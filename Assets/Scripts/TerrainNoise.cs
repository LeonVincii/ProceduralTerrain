using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public static class TerrainNoise
{
    [Serializable]
    public struct Config
    {
        public int size;
    }

    static public JobHandle Generate(Config config, NativeArray<float> noise)
    {
        return Job.ScheduleParallel(config, noise);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job : IJobFor
    {
        [WriteOnly]
        NativeArray<float> _noise;

        public void Execute(int i)
        {
            _noise[i] = (float)i / _noise.Length;
        }

        public static JobHandle ScheduleParallel(Config config, NativeArray<float> noise, JobHandle dependency = default)
        {
            return new Job
            {
                _noise = noise,
            }
            .ScheduleParallel(config.size * config.size, 64, dependency);
        }
    }
}
