using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class NoiseVisualizer : MonoBehaviour
{
    [SerializeField, Range(1, 255)]
    int _size = 64;

    [SerializeField, Range(1, 255)]
    int _resolution = 64;

    [SerializeField, Range(.1f, 10f)]
    float _noiseScale = 1.0f;

    [SerializeField]
    float2 _offset = float2.zero;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    Mesh _mesh;

    Texture2D _texture;

    public void OnValidate()
    {
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

        _texture = new Texture2D(_resolution + 1, _resolution + 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        JobHandle meshJob = TerrainMesh.GenerateParallel(_mesh, meshData, _size, _resolution, Vector3.zero);

        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);

        var noise = new NativeArray<float>(positions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var noiseConfig = new TerrainNoise.Config
        {
            size = _size,
            scale = _noiseScale,
            offset = _offset,
        };

        JobHandle noiseJob = TerrainNoise.GenerateParallel(noiseConfig, positions, noise, meshJob);

        NoiseTexture.GenerateParallel(_texture, noise, noiseJob);

        _meshRenderer.material.mainTexture = _texture;

        noise.Dispose();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);

        enabled = false;
    }
}
