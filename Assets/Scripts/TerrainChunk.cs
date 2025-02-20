using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    [Serializable]
    public struct Config
    {
        public int size;
        public int resolution;

        public bool showTerrain;
        public Gradient heightColors;
        public AnimationCurve heightCurve;

        public TerrainMesh.Config mesh;
        public TerrainNoise.Config noise;

        public void Validate()
        {
            if (size < 1)
                size = 1;
            else if (size > 255)
                size = 255;

            if (resolution < 1)
                resolution = 1;
            else if (resolution > 255)
                resolution = 255;

            mesh.Validate();
            noise.Validate();
        }
    }

    [SerializeField]
    Config _config;

    NativeArray<float> _heightCurveSamples;

    [SerializeField, Range(2, 100)]
    int _heightCurveSamplingResolution = 100;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    Mesh _mesh;

    Texture2D _texture;

    int _noiseTextureID = Shader.PropertyToID("_NoiseTexture");
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
        _heightCurveSamples.Dispose();
    }

    public void Awake()
    {
        _heightCurveSamples = new NativeArray<float>(
            _heightCurveSamplingResolution, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh
        {
            name = "Noise Visualizer",
        };

        _meshFilter.mesh = _mesh;
    }

    public void Update()
    {
        if (_texture != null)
            Destroy(_texture);

        _texture = new Texture2D(_config.resolution + 1, _config.resolution + 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        JobHandle meshJob = TerrainMesh.GenerateParallel(
            _mesh, meshData, _config.size, _config.resolution, Vector3.up * _config.mesh.heightMultiplier);

        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);

        var noise = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle noiseJob = TerrainNoise.GenerateParallel(_config.noise, _config.size, positions, noise, meshJob);

        SampleCurve(_config.heightCurve, _heightCurveSamplingResolution, ref _heightCurveSamples);

        TerrainMesh.DisplaceParallel(_config.mesh, _heightCurveSamples, noise, positions, noiseJob);

        NoiseTexture.GenerateParallel(_texture, noise, noiseJob);

        _meshRenderer.material.SetTexture(_noiseTextureID, _texture);
        _meshRenderer.material.SetInt(_showTerrainID, _config.showTerrain ? 1 : 0);
        _meshRenderer.material.SetFloat(_minHeightID, -_config.mesh.heightMultiplier);
        _meshRenderer.material.SetFloat(_maxHeightID, _config.mesh.heightMultiplier);

        GradientColorKey[] colorKeys = _config.heightColors.colorKeys;
        float[] heights = new float[colorKeys.Length];
        Color[] colors = new Color[colorKeys.Length];

        for (int i = 0; i < colorKeys.Length; ++i)
        {
            heights[i] = colorKeys[i].time;
            colors[i] = colorKeys[i].color;
        }

        _meshRenderer.material.SetInt(_colorCountID, colorKeys.Length);
        _meshRenderer.material.SetFloatArray(_heightsID, heights);
        _meshRenderer.material.SetColorArray(_heightColorsID, colors);

        noise.Dispose();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);

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
