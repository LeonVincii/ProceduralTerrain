using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;

public static class NoiseTexture
{
    struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    static public void GenerateParallel(
        Texture2D texture, NativeArray<float> noise, JobHandle dependency = default)
    {
        var colors = new NativeArray<Color32>(noise.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        Job.ScheduleParallel(noise, colors, dependency).Complete();

        texture.SetPixelData(colors.Reinterpret<byte>(sizeof(byte) * 4), 0);
        texture.Apply();

        colors.Dispose();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job : IJobFor
    {
        [ReadOnly]
        NativeArray<float> _noise;

        [WriteOnly]
        NativeArray<Color32> _colors;

        public void Execute(int index)
        {
            _colors[index] = new Color32
            {
                a = (byte)(_noise[index] * 255),
            };
        }

        public static JobHandle ScheduleParallel(
            NativeArray<float> noise, NativeArray<Color32> colors, JobHandle dependency = default)
        {
            return new Job
            {
                _noise = noise,
                _colors = colors,
            }
            .ScheduleParallel(noise.Length, 32, dependency);
        }
    }
}
