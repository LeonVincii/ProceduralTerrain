using Unity.Mathematics;
using UnityEngine;

public struct UShort3
{
    public ushort x, y, z;

    public static implicit operator UShort3(int3 value)
    {
        return new UShort3
        {
            x = (ushort)value.x,
            y = (ushort)value.y,
            z = (ushort)value.z,
        };
    }
}

public interface IVertexStream
{
    void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);
    void SetVertex(int index, Vertex vertex);
    void SetTriangle(int index, int3 triangle);
}
