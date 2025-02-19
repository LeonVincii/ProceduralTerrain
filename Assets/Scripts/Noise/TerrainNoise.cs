using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

using Random = Unity.Mathematics.Random;

public static class TerrainNoise
{
    [Serializable]
    public struct Config
    {
        public uint seed;
        public float scale;
        public float2 offset;
        public int octaves;
        public float persistence;
        public float lacunarity;

        public void Validate()
        {
            if (seed == 0)
                seed = 1;

            if (octaves < 1)
                octaves = 1;
            else if (octaves > 10)
                octaves = 10;

            if (scale < .1f)
                scale = .1f;
            else if (scale > 10f)
                scale = 10f;

            if (persistence < 0f)
                persistence = 0f;
            else if (persistence > 1f)
                persistence = 1f;

            if (lacunarity < 1f)
                lacunarity = 1f;
        }
    }

    static public JobHandle GenerateParallel(
        Config config, int size, NativeArray<float3> positions, NativeArray<float> noise,
        JobHandle dependency = default)
    {
        Assert.AreEqual(positions.Length, noise.Length);

        CalculateOctaveValues(
            config, out float maxValue, out NativeArray<float> amplitudes, out NativeArray<float> frequencies,
            out NativeArray<float2> octaveOffsets);

        JobHandle handle = Job.ScheduleParallel(
            config, size, maxValue, amplitudes, frequencies, octaveOffsets, positions, noise, dependency);

        amplitudes.Dispose(handle);
        frequencies.Dispose(handle);
        octaveOffsets.Dispose(handle);

        return handle;
    }

    static public void CalculateOctaveValues(
        Config config, out float maxValue, out NativeArray<float> amplitudes, out NativeArray<float> frequencies,
        out NativeArray<float2> offsets)
    {
        maxValue = 0f;

        amplitudes = new NativeArray<float>(
            config.octaves, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        frequencies = new NativeArray<float>(
            config.octaves, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        offsets = new NativeArray<float2>(
            config.octaves, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        float amp = 1f, freq = 1f;

        var rand = new Random(config.seed);

        for (int i = 0; i < config.octaves; ++i)
        {
            maxValue += amp;

            amplitudes[i] = amp;
            frequencies[i] = freq;
            offsets[i] = rand.NextFloat2(-100000f, 100000f);

            amp *= config.persistence;
            freq *= config.lacunarity;
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job : IJobFor
    {
        Config _config;

        int _size;

        float _maxValue;

        [ReadOnly]
        NativeArray<float> _amplitudes;

        [ReadOnly]
        NativeArray<float> _frequencies;

        [ReadOnly]
        NativeArray<float2> _octaveOffsets;

        [ReadOnly]
        NativeArray<float3> _positions;

        [WriteOnly]
        NativeArray<float> _noise;

        public void Execute(int index)
        {
            float noise = 0;

            for (int i = 0; i < _config.octaves; ++i)
            {
                float2 offset = _octaveOffsets[i] + _positions[index].xz + _config.offset;
                float2 sample = offset / _size / _config.scale * _frequencies[i];

                noise += cnoise(sample) * _amplitudes[i];
            }

            _noise[index] = unlerp(-_maxValue, _maxValue, noise) * 2f - 1f;
        }

        public static JobHandle ScheduleParallel(
            Config config, int size, float maxValue, NativeArray<float> amplitudes, NativeArray<float> frequencies,
            NativeArray<float2> octaveOffsets, NativeArray<float3> positions, NativeArray<float> noise,
            JobHandle dependency = default)
        {
            return new Job
            {
                _config = config,
                _size = size,
                _maxValue = maxValue,
                _amplitudes = amplitudes,
                _frequencies = frequencies,
                _octaveOffsets = octaveOffsets,
                _positions = positions,
                _noise = noise,
            }
            .ScheduleParallel(noise.Length, 32, dependency);
        }
    }
}
