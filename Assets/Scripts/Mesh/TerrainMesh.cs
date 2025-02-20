using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public static class TerrainMesh
{
    [Serializable]
    public struct Config
    {
        public float heightMultiplier;

        public void Validate()
        {
            if (heightMultiplier < 0f)
                heightMultiplier = 0f;
        }
    }

    public static JobHandle GenerateParallel(
        Mesh mesh, Mesh.MeshData data, int size, int resolution, Vector3 extraBounds, JobHandle dependency = default)
    {
        return GenerateJob<Plane, MultiStream>.ScheduleParallel(mesh, data, size, resolution, extraBounds, dependency);
    }

    public static void DisplaceParallel(
        Config config, NativeArray<float> heightCurve, NativeArray<float> noise, NativeArray<float3> positions,
        JobHandle dependency = default)
    {
        DisplaceJob.ScheduleParallel(config, heightCurve, noise, positions, dependency).Complete();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct GenerateJob<MeshGenerator_T, VertexStream_T> : IJobFor
        where MeshGenerator_T : struct, IMeshGenerator
        where VertexStream_T : struct, IVertexStream
    {
        MeshGenerator_T _generator;

        [WriteOnly]
        VertexStream_T _stream;

        public void Execute(int index)
        {
            _generator.Generate(index, _stream);
        }

        public static JobHandle ScheduleParallel(
            Mesh mesh, Mesh.MeshData data, int size, int resolution, Vector3 extraBounds,
            JobHandle dependency = default)
        {
            var job = new GenerateJob<MeshGenerator_T, VertexStream_T>();

            job._generator.Size = size;
            job._generator.Resolution = resolution;

            Bounds bounds = job._generator.Bounds;
            bounds.extents += extraBounds;
            mesh.bounds = bounds;

            job._stream.Setup(data, job._generator.Bounds, job._generator.VertexCount, job._generator.IndexCount);

            return job.ScheduleParallel(job._generator.JobLength, 8, dependency);
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct DisplaceJob : IJobFor
    {
        Config _config;

        [ReadOnly]
        NativeArray<float> _curve;

        [ReadOnly]
        NativeArray<float> _noise;

        NativeArray<float3> _positions;

        public void Execute(int index)
        {
            float3 position = _positions[index];
            position.y = EvaluateCurve(_noise[index]) * _config.heightMultiplier;
            _positions[index] = position;
        }

        public static JobHandle ScheduleParallel(
            Config config, NativeArray<float> curve, NativeArray<float> noise, NativeArray<float3> positions,
            JobHandle dependency = default)
        {
            return new DisplaceJob()
            {
                _config = config,
                _curve = curve,
                _noise = noise,
                _positions = positions,
            }
            .ScheduleParallel(positions.Length, 8, dependency);
        }

        float EvaluateCurve(float key)
        {
            int keyCount = _curve.Length;
            int lo = (int)ceil(key * keyCount);
            int hi = lo + 1;

            if (lo == hi)
                return _curve[lo];

            return lerp(_curve[lo], _curve[hi], unlerp(key, lo, hi));
        }
    }
}
