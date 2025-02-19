using UnityEngine;

public interface IMeshGenerator
{
    int Size { get; set; }
    int Resolution { get; set; }
    Bounds Bounds { get; }
    int VertexCount { get; }
    int IndexCount { get; }
    int JobLength { get; }

    public void Generate<Stream_T>(int i, Stream_T stream) where Stream_T : struct, IVertexStream;
}
