using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class NoiseVisualizer : MonoBehaviour
{
    [Serializable]
    public struct Config
    {
        public int size;
        public int resolution;

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

            noise.Validate();
        }
    }

    [SerializeField]
    Config _config;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    Mesh _mesh;

    Texture2D _texture;

    public void OnValidate()
    {
        _config.Validate();

        enabled = true;
    }

    public void Awake()
    {
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
            _mesh, meshData, _config.size, _config.resolution, Vector3.up);

        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);

        var noise = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle noiseJob = TerrainNoise.GenerateParallel(_config.noise, _config.size, positions, noise, meshJob);

        NoiseTexture.GenerateParallel(_texture, noise, noiseJob);

        _meshRenderer.material.mainTexture = _texture;

        noise.Dispose();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);

        enabled = false;
    }
}
