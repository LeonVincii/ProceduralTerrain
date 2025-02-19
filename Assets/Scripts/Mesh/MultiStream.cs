using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public struct MultiStream : IVertexStream
{
    [NativeDisableContainerSafetyRestriction]
    NativeArray<float3> _stream0;

    [NativeDisableContainerSafetyRestriction]
    NativeArray<float3> _stream1;

    [NativeDisableContainerSafetyRestriction]
    NativeArray<float4> _stream2;

    [NativeDisableContainerSafetyRestriction]
    NativeArray<float2> _stream3;

    [NativeDisableContainerSafetyRestriction]
    NativeArray<UShort3> _triangles;

    public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
    {
        var descriptor =
            new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        descriptor[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);
        descriptor[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 2);
        descriptor[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 3);

        meshData.SetVertexBufferParams(vertexCount, descriptor);

        descriptor.Dispose();

        meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount)
        {
            vertexCount = vertexCount,
            bounds = bounds,
        },
        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

        _stream0 = meshData.GetVertexData<float3>(0);
        _stream1 = meshData.GetVertexData<float3>(1);
        _stream2 = meshData.GetVertexData<float4>(2);
        _stream3 = meshData.GetVertexData<float2>(3);

        _triangles = meshData.GetIndexData<ushort>().Reinterpret<UShort3>(sizeof(ushort));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertex(int index, Vertex vertex)
    {
        _stream0[index] = new float3(vertex.position);
        _stream1[index] = new float3(vertex.normal);
        _stream2[index] = new float4(vertex.tangent);
        _stream3[index] = new float2(vertex.uv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTriangle(int index, int3 triangle)
    {
        _triangles[index] = triangle;
    }
}
