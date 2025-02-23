using System;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class World : MonoBehaviour
{
    [Serializable]
    public struct Config
    {
        public int drawDistance;

        public TerrainChunk.Config chunk;

        public void Validate()
        {
            drawDistance = Mathf.Clamp(drawDistance, 0, 10);

            chunk.Validate();
        }
    }

    [SerializeField]
    Config _config;

    [SerializeField]
    Transform _player;

    Vector2Int _playerChunkCoord;

    Dictionary<Vector2Int, GameObject> _chunks = new();

    [SerializeField]
    Material _terrainMaterial;

    [SerializeField]
    Material _waterMaterial;

    void OnValidate()
    {
        _config.Validate();
    }

    void OnDestroy()
    {
        TerrainChunk.Dispose();
    }

    void Start()
    {
        TerrainChunk.Allocate(_config.chunk);
        TerrainChunk.terrainMaterial = _terrainMaterial;
        TerrainChunk.waterMaterial = _waterMaterial;
    }

    void Update()
    {
        Vector2Int playerChunkCoord = Utils.ChunkCoord(_player.position, _config.chunk.size);

        if (playerChunkCoord != _playerChunkCoord)
            _playerChunkCoord = playerChunkCoord;

        DestroyFarChunks();

        for (int x = -_config.drawDistance; x <= _config.drawDistance; ++x)
        {
            for (int y = -_config.drawDistance; y <= _config.drawDistance; ++y)
            {
                var chunkCoord = new Vector2Int(_playerChunkCoord.x + x, _playerChunkCoord.y + y);

                if (_chunks.ContainsKey(chunkCoord))
                    continue;

                if (Vector2Int.Distance(_playerChunkCoord, chunkCoord) > _config.drawDistance)
                    continue;

                Vector2 offset = Utils.ChunkOffset(chunkCoord, _config.chunk.size);

                var chunk = TerrainChunk.Generate(float2(offset.x, offset.y));

                chunk.name = $"Chunk {chunkCoord}";
                chunk.transform.parent = transform;
                chunk.transform.position = new Vector3(offset.x, 0, offset.y);

                _chunks.Add(chunkCoord, chunk);
            }
        }
    }

    void DestroyFarChunks()
    {
        var chunksToRemove = new List<Vector2Int>();

        foreach (var kv in _chunks)
        {
            if (Vector2Int.Distance(_playerChunkCoord, kv.Key) <= _config.drawDistance)
                continue;

            chunksToRemove.Add(kv.Key);
        }

        foreach (var chunk in chunksToRemove)
        {
            DestroyChunk(_chunks[chunk]);
            _chunks.Remove(chunk);
        }
    }

    void DestroyChunk(GameObject chunk)
    {
        var terrain = chunk.transform.GetChild(0).gameObject;
        Destroy(terrain);

        var water = chunk.transform.GetChild(1).gameObject;
        Destroy(water);

        Destroy(chunk);
    }
}
