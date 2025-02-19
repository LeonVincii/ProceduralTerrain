using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class TerrainMesh
{
    public static JobHandle GenerateParallel(
        Mesh mesh, Mesh.MeshData data, int size, int resolution, Vector3 extraBounds, JobHandle dependency = default)
    {
        return Job<Plane, MultiStream>.ScheduleParallel(mesh, data, size, resolution, extraBounds, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job<MeshGenerator_T, VertexStream_T> : IJobFor
        where MeshGenerator_T : struct, IMeshGenerator
        where VertexStream_T : struct, IVertexStream
    {
        MeshGenerator_T _generator;

        [WriteOnly]
        VertexStream_T _stream;

        public void Execute(int i)
        {
            _generator.Generate(i, _stream);
        }

        public static JobHandle ScheduleParallel(
            Mesh mesh, Mesh.MeshData data, int size, int resolution, Vector3 extraBounds,
            JobHandle dependency = default)
        {
            var job = new Job<MeshGenerator_T, VertexStream_T>();

            job._generator.Size = size;
            job._generator.Resolution = resolution;

            Bounds bounds = job._generator.Bounds;
            bounds.extents += extraBounds;
            mesh.bounds = bounds;

            job._stream.Setup(data, job._generator.Bounds, job._generator.VertexCount, job._generator.IndexCount);

            return job.ScheduleParallel(job._generator.JobLength, 8, dependency);
        }
    }
}
