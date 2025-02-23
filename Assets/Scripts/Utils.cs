using UnityEngine;

public static class Utils
{
    public static Vector2Int ChunkCoord(Vector3 position, int chunkSize)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));
    }

    public static Vector2 ChunkOffset(Vector2Int coord, int chunkSize)
    {
        return new Vector2(coord.x * chunkSize, coord.y * chunkSize);
    }
}
