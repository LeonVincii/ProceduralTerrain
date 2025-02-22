using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    [Serializable]
    public struct Config
    {
        public int size;
        public int terrainResolution;
        public int waterResolution;

        public bool showTerrain;
        public Gradient heightColors;
        public AnimationCurve heightCurve;

        public TerrainMesh.Config terrainMesh;
        public TerrainNoise.Config noise;

        public void Validate()
        {
            if (size < 1)
                size = 1;
            else if (size > 255)
                size = 255;

            if (terrainResolution < 1)
                terrainResolution = 1;
            else if (terrainResolution > 255)
                terrainResolution = 255;

            if (waterResolution < 1)
                waterResolution = 1;
            else if (waterResolution > 255)
                waterResolution = 255;

            terrainMesh.Validate();
            noise.Validate();
        }
    }

    [SerializeField]
    Config _config;

    NativeArray<float> _heightCurveSamples;

    [SerializeField, Range(2, 100)]
    int _heightCurveSamplingResolution = 100;

    [SerializeField]
    Material _terrainMaterial;
    [SerializeField]
    Material _waterMaterial;

    GameObject _terrain;
    Mesh _terrainMesh;
    Texture2D _terrainNoiseTexture;
    MeshFilter _terrainMeshFilter;
    MeshRenderer _terrainMeshRenderer;

    GameObject _water;
    Mesh _waterMesh;
    MeshFilter _waterMeshFilter;
    MeshRenderer _waterMeshRenderer;

    int _terrainNoiseTextureID = Shader.PropertyToID("_TerrainNoiseTexture");
    int _showTerrainID = Shader.PropertyToID("_ShowTerrain");
    int _minHeightID = Shader.PropertyToID("_MinHeight");
    int _maxHeightID = Shader.PropertyToID("_MaxHeight");
    int _colorCountID = Shader.PropertyToID("_ColorCount");
    int _heightsID = Shader.PropertyToID("_Heights");
    int _heightColorsID = Shader.PropertyToID("_HeightColors");

    public void OnValidate()
    {
        _config.Validate();

        enabled = true;
    }

    public void OnDestroy()
    {
        Destroy(_terrainMesh);
        Destroy(_terrainNoiseTexture);
        Destroy(_terrain);

        Destroy(_waterMesh);
        Destroy(_water);

        _heightCurveSamples.Dispose();
    }

    public void Awake()
    {
        _heightCurveSamples = new NativeArray<float>(
            _heightCurveSamplingResolution, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        _terrain = new GameObject("Land");
        _terrain.transform.SetParent(transform);
        _terrain.AddComponent<MeshFilter>();
        _terrain.AddComponent<MeshRenderer>();
        _terrainMeshFilter = _terrain.GetComponent<MeshFilter>();
        _terrainMesh = new Mesh
        {
            name = "Terrain",
        };
        _terrainMeshFilter.mesh = _terrainMesh;
        _terrainMeshRenderer = _terrain.GetComponent<MeshRenderer>();
        _terrainMeshRenderer.material = _terrainMaterial;

        _water = new GameObject("Water");
        _water.transform.SetParent(transform);
        _water.AddComponent<MeshFilter>();
        _water.AddComponent<MeshRenderer>();
        _waterMeshFilter = _water.GetComponent<MeshFilter>();
        _waterMesh = new Mesh
        {
            name = "Water",
        };
        _waterMeshFilter.mesh = _waterMesh;
        _waterMeshRenderer = _water.GetComponent<MeshRenderer>();
        _waterMeshRenderer.material = _waterMaterial;
    }

    public void Update()
    {
        if (_terrainNoiseTexture != null)
            Destroy(_terrainNoiseTexture);

        _terrainNoiseTexture = new Texture2D(_config.terrainResolution + 1, _config.terrainResolution + 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(2);
        Mesh.MeshData terrainMeshData = meshDataArray[0];
        Mesh.MeshData waterMeshData = meshDataArray[1];

        JobHandle terrainMeshJob = TerrainMesh.GenerateParallel(
            _terrainMesh, terrainMeshData, _config.size, _config.terrainResolution,
            Vector3.up * _config.terrainMesh.heightMultiplier);

        NativeArray<float3> positions = terrainMeshData.GetVertexData<float3>(0);

        var noise = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle noiseJob = TerrainNoise.GenerateParallel(
            _config.noise, _config.size, positions, noise, terrainMeshJob);

        SampleCurve(_config.heightCurve, _heightCurveSamplingResolution, ref _heightCurveSamples);

        TerrainMesh.DisplaceParallel(_config.terrainMesh, _heightCurveSamples, noise, positions, noiseJob);

        NoiseTexture.GenerateParallel(_terrainNoiseTexture, noise, noiseJob);

        _terrainMeshRenderer.material.SetTexture(_terrainNoiseTextureID, _terrainNoiseTexture);
        _terrainMeshRenderer.material.SetInt(_showTerrainID, _config.showTerrain ? 1 : 0);
        _terrainMeshRenderer.material.SetFloat(_minHeightID, -_config.terrainMesh.heightMultiplier);
        _terrainMeshRenderer.material.SetFloat(_maxHeightID, _config.terrainMesh.heightMultiplier);

        GradientColorKey[] colorKeys = _config.heightColors.colorKeys;
        float[] heights = new float[colorKeys.Length];
        Color[] colors = new Color[colorKeys.Length];

        for (int i = 0; i < colorKeys.Length; ++i)
        {
            heights[i] = colorKeys[i].time;
            colors[i] = colorKeys[i].color;
        }

        _terrainMeshRenderer.material.SetInt(_colorCountID, colorKeys.Length);
        _terrainMeshRenderer.material.SetFloatArray(_heightsID, heights);
        _terrainMeshRenderer.material.SetColorArray(_heightColorsID, colors);

        noise.Dispose();

        TerrainMesh.GenerateParallel(
            _waterMesh, waterMeshData, _config.size, _config.waterResolution, Vector3.zero).Complete();

        _waterMeshRenderer.material.SetTexture(_terrainNoiseTextureID, _terrainNoiseTexture);

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, new[] { _terrainMesh, _waterMesh });

        _terrainMesh.RecalculateNormals();
        _terrainMesh.RecalculateTangents();

        enabled = false;
    }

    void SampleCurve(AnimationCurve curve, int resolution, ref NativeArray<float> samples)
    {
        for (int i = 0; i < resolution; ++i)
        {
            float t = (float)i / (resolution - 1);
            samples[i] = curve.Evaluate(t);
        }
    }
}
