using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public static class TerrainNoise
{
    public struct Config
    {
        public int size;
        public float scale;
        public float2 offset;
    }

    static public JobHandle GenerateParallel(
        Config config, NativeArray<float3> positions, NativeArray<float> noise, JobHandle dependency = default)
    {
        Assert.AreEqual(positions.Length, noise.Length);
        return Job.ScheduleParallel(config, positions, noise, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job : IJobFor
    {
        Config _config;

        [ReadOnly]
        NativeArray<float3> _positions;

        [WriteOnly]
        NativeArray<float> _noise;

        public void Execute(int i)
        {
            float2 sample = (_positions[i].xz + _config.offset) / _config.size / _config.scale;
            _noise[i] = unlerp(-1, 1, cnoise(sample));
        }

        public static JobHandle ScheduleParallel(
            Config config, NativeArray<float3> positions, NativeArray<float> noise, JobHandle dependency = default)
        {
            return new Job
            {
                _config = config,
                _positions = positions,
                _noise = noise,
            }
            .ScheduleParallel(noise.Length, 32, dependency);
        }
    }
}
