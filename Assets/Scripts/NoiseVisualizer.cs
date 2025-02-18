using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class NoiseVisualizer : MonoBehaviour
{
    [SerializeField, Range(1, 255)]
    int _size = 64;

    MeshRenderer _meshRenderer;

    public void OnValidate()
    {
        enabled = true;
    }

    public void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Update()
    {
        if (_meshRenderer.sharedMaterial.mainTexture != null)
            Destroy(_meshRenderer.sharedMaterial.mainTexture);

        var noiseConfig = new TerrainNoise.Config
        {
            size = _size,
        };

        var noise = new NativeArray<float>(_size * _size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        TerrainNoise.Generate(noiseConfig, noise).Complete();

        _meshRenderer.material.mainTexture = NoiseTexture.Generate(noise, _size);

        noise.Dispose();

        enabled = false;
    }
}
