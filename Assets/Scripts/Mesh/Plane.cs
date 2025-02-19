using UnityEngine;
using static Unity.Mathematics.math;

public struct Plane : IMeshGenerator
{
    public int Size { get; set; }
    public int Resolution { get; set; }
    public readonly Bounds Bounds => new(float3(0f), float3(1f) * Size);
    public readonly int VertexCount => (Resolution + 1) * (Resolution + 1);
    public readonly int IndexCount => Resolution * Resolution * 6;
    public readonly int JobLength => Resolution + 1;

    public void Generate<Stream_T>(int row, Stream_T stream) where Stream_T : struct, IVertexStream
    {
        int vi = row * (Resolution + 1), ti = (row - 1) * Resolution * 2;

        var vert = new Vertex();
        vert.position.z = ((float)row / Resolution - .5f) * Size;
        vert.normal.y = 1f;
        vert.tangent.xw = float2(1f, -1f);
        vert.uv.y = (float)row / Resolution;

        vert.position.x = -.5f * Size;
        vert.uv.x = 0f;

        stream.SetVertex(vi, vert);
        ++vi;

        for (int col = 1; col <= Resolution; ++col, ++vi)
        {
            vert.position.x = ((float)col / Resolution - .5f) * Size;
            vert.uv.x = (float)col / Resolution;

            stream.SetVertex(vi, vert);

            if (row > 0)
            {
                stream.SetTriangle(ti++, vi + int3(-Resolution - 2, -1, -Resolution - 1));
                stream.SetTriangle(ti++, vi + int3(-Resolution - 1, -1, 0));
            }
        }
    }
}
