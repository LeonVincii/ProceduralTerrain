using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class NoiseTexture
{
    struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    static public Texture2D Generate(NativeArray<float> noise, int size)
    {
        var texture = new Texture2D(size, size)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var colors = new NativeArray<Color32>(size * size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        Job.Run(noise, colors);

        texture.SetPixelData(colors.Reinterpret<byte>(sizeof(byte) * 4), 0);
        texture.Apply();

        colors.Dispose();

        return texture;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct Job : IJob
    {
        [ReadOnly]
        NativeArray<float> _noise;

        [WriteOnly]
        NativeArray<Color32> _colors;

        public void Execute()
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i] = new Color32
                {
                    r = (byte)(_noise[i] * 255),
                    g = (byte)(_noise[i] * 255),
                    b = (byte)(_noise[i] * 255),
                    a = 255,
                };
            }
        }

        public static void Run(NativeArray<float> noise, NativeArray<Color32> colors)
        {
            new Job
            {
                _noise = noise,
                _colors = colors,
            }
            .Run();
        }
    }
}
