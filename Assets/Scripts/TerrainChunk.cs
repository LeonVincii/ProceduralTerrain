using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public static class TerrainChunk
{
    [Serializable]
    public struct Config
    {
        public int size;
        public float scale;
        public int terrainResolution;
        public int waterResolution;

        public bool showTerrain;
        public Gradient heightColors;
        public AnimationCurve heightCurve;
        public int heightCurveResolution;

        public TerrainMesh.Config terrainMesh;
        public TerrainNoise.Config noise;

        public void Validate()
        {
            size = Mathf.Clamp(size, 1, 255);
            scale = Mathf.Clamp(scale, .1f, 10f);
            terrainResolution = Mathf.Clamp(terrainResolution, 1, 255);
            waterResolution = Mathf.Clamp(waterResolution, 1, 255);
            heightCurveResolution = Mathf.Clamp(heightCurveResolution, 2, 100);

            terrainMesh.Validate();
            noise.Validate();
        }
    }

    public static Material terrainMaterial;
    public static Material waterMaterial;

    static bool _allocated = false;

    static Config _config;

    static NativeArray<float> _heightCurveSamples;

    static int _terrainNoiseTextureID = Shader.PropertyToID("_TerrainNoiseTexture");
    static int _showTerrainID = Shader.PropertyToID("_ShowTerrain");
    static int _minHeightID = Shader.PropertyToID("_MinHeight");
    static int _maxHeightID = Shader.PropertyToID("_MaxHeight");
    static int _colorCountID = Shader.PropertyToID("_ColorCount");
    static int _heightsID = Shader.PropertyToID("_Heights");
    static int _heightColorsID = Shader.PropertyToID("_HeightColors");

    static int _waveNormalFrequencyID = Shader.PropertyToID("_WaveNormalFrequency");
    static int _waveNormalMoveSpeedID = Shader.PropertyToID("_WaveNormalMoveSpeed");

    static public void Allocate(Config config)
    {
        Assert.IsFalse(_allocated);

        _config = config;

        _heightCurveSamples = new NativeArray<float>(
            _config.heightCurveResolution, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        SampleCurve(_config.heightCurve, _config.heightCurveResolution, ref _heightCurveSamples);

        _allocated = true;
    }

    static public void Dispose()
    {
        if (!_allocated)
            return;

        _heightCurveSamples.Dispose();
    }

    static public GameObject Generate(float2 offset)
    {
        Assert.IsTrue(_allocated);

        var chunk = new GameObject();

        var terrain = new GameObject("Terrain");
        terrain.transform.SetParent(chunk.transform);
        var terrainMesh = new Mesh
        {
            name = "Terrain",
        };
        terrain.AddComponent<MeshFilter>().mesh = terrainMesh;
        var terrainMeshRenderer = terrain.AddComponent<MeshRenderer>();
        terrainMeshRenderer.material = terrainMaterial;

        var water = new GameObject("Water");
        water.transform.SetParent(chunk.transform);
        var waterMesh = new Mesh
        {
            name = "Water",
        };
        water.AddComponent<MeshFilter>().mesh = waterMesh;
        var waterMeshRenderer = water.AddComponent<MeshRenderer>();
        waterMeshRenderer.material = waterMaterial;

        var terrainNoiseTexture = new Texture2D(_config.terrainResolution + 1, _config.terrainResolution + 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(2);
        Mesh.MeshData terrainMeshData = meshDataArray[0];
        Mesh.MeshData waterMeshData = meshDataArray[1];

        JobHandle terrainMeshJob = TerrainMesh.GenerateParallel(
            terrainMesh, terrainMeshData, _config.size, _config.terrainResolution,
            Vector3.up * _config.terrainMesh.heightMultiplier * _config.scale);

        NativeArray<float3> positions = terrainMeshData.GetVertexData<float3>(0);

        var noise = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle noiseJob = TerrainNoise.GenerateParallel(
            _config.noise, _config.size, _config.scale, offset, positions, noise, terrainMeshJob);

        SampleCurve(_config.heightCurve, _config.heightCurveResolution, ref _heightCurveSamples);

        TerrainMesh.DisplaceParallel(
            _config.terrainMesh, _config.scale, _heightCurveSamples, noise, positions, noiseJob);

        NoiseTexture.GenerateParallel(terrainNoiseTexture, noise, noiseJob);

        terrainMeshRenderer.material.SetTexture(_terrainNoiseTextureID, terrainNoiseTexture);
        terrainMeshRenderer.material.SetInt(_showTerrainID, _config.showTerrain ? 1 : 0);
        terrainMeshRenderer.material.SetFloat(
            _minHeightID, -_config.terrainMesh.heightMultiplier * _config.scale);
        terrainMeshRenderer.material.SetFloat(
            _maxHeightID, _config.terrainMesh.heightMultiplier * _config.scale);

        GradientColorKey[] colorKeys = _config.heightColors.colorKeys;
        float[] heights = new float[colorKeys.Length];
        Color[] colors = new Color[colorKeys.Length];

        for (int i = 0; i < colorKeys.Length; ++i)
        {
            heights[i] = colorKeys[i].time;
            colors[i] = colorKeys[i].color;
        }

        terrainMeshRenderer.material.SetInt(_colorCountID, colorKeys.Length);
        terrainMeshRenderer.material.SetFloatArray(_heightsID, heights);
        terrainMeshRenderer.material.SetColorArray(_heightColorsID, colors);

        noise.Dispose();

        TerrainMesh.GenerateParallel(
            waterMesh, waterMeshData, _config.size, _config.waterResolution, Vector3.zero).Complete();

        waterMeshRenderer.material.SetTexture(_terrainNoiseTextureID, terrainNoiseTexture);
        waterMeshRenderer.material.SetFloat(_waveNormalFrequencyID, 27.3f / _config.scale);
        waterMeshRenderer.material.SetVector(_waveNormalMoveSpeedID, new Vector2(.05f, .1f) * _config.scale);

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, new[] { terrainMesh, waterMesh });

        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateTangents();

        return chunk;
    }

    static void SampleCurve(AnimationCurve curve, int resolution, ref NativeArray<float> samples)
    {
        for (int i = 0; i < resolution; ++i)
        {
            float t = (float)i / (resolution - 1);
            samples[i] = curve.Evaluate(t);
        }
    }
}
