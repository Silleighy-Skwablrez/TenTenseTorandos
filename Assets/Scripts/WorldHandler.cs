using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public class WorldHandler : MonoBehaviour
{
    public Tilemap[] dualGridTilemaps;

    // for mapping tile rule assets to their respective tilemaps
    public Dictionary<DualGridRuleTile, Tilemap> tileRuleToTilemap = new Dictionary<DualGridRuleTile, Tilemap>();

    // for tracking which coordinates are occupied by which tile rule
    private Dictionary<Vector3Int, DualGridRuleTile> coordsToTileRule = new Dictionary<Vector3Int, DualGridRuleTile>();

    // track initialization
    public bool IsInitialized { get; private set; }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (IsInitialized) return;

        // initialize the tile rule to tilemap dictionary
        foreach (var tilemap in dualGridTilemaps)
        {
            if (tilemap.TryGetComponent<DualGridTilemapModule>(out var module))
            {
                // get the tile rule that the module wants
                DualGridRuleTile tileRule = module.RenderTile;
                if (tileRule != null)
                {
                    tileRuleToTilemap[tileRule] = tilemap;
                    Debug.Log($"Registered tilemap with rule: {tileRule.name}");
                }
                else
                {
                    Debug.LogWarning($"Tilemap {tilemap.name} has no tile rule assigned");
                }
            }
        }

        IsInitialized = true;
    }

    // register tile rules with the world handler
    public void RegisterTileRules(List<DualGridRuleTile> tileRules)
    {
        foreach (var tileRule in tileRules)
        {
            // skip if its already here
            if (tileRuleToTilemap.ContainsKey(tileRule))
                continue;

            // look for a tilemap with the same rule
            foreach (var tilemap in dualGridTilemaps)
            {
                if (tilemap.TryGetComponent<DualGridTilemapModule>(out var module) &&
                    module.RenderTile != null &&
                    module.RenderTile.name == tileRule.name)
                {
                    tileRuleToTilemap[tileRule] = tilemap;
                    Debug.Log($"Added tilemap mapping for rule: {tileRule.name}");
                    break;
                }
            }

            // ok then, use the first one
            if (!tileRuleToTilemap.ContainsKey(tileRule) && dualGridTilemaps.Length > 0)
            {
                tileRuleToTilemap[tileRule] = dualGridTilemaps[0];
                Debug.LogWarning($"Using fallback tilemap for rule: {tileRule.name}");
            }
        }
    }

    public bool SetTile(Vector3Int position, DualGridRuleTile tile)
    {
        // yeah, lets not allow removal of the map lol
        if (tile == null)
        {
            return false;
        }

        // Not using this for now because tiles of different materials dont match up perfectly
        // if (coordsToTileRule.ContainsKey(position))
        // {
        //     // Get existing tile and clear it from its tilemap
        //     DualGridRuleTile existingTile = coordsToTileRule[position];
        //     if (tileRuleToTilemap.TryGetValue(existingTile, out Tilemap existingTilemap))
        //     {
        //         existingTilemap.SetTile(position, null);
        //     }
        // }

        if (!tileRuleToTilemap.TryGetValue(tile, out Tilemap targetTilemap))
        {
            Debug.LogWarning($"No tilemap found for tile rule {tile.name}, trying to find by name");

            foreach (var pair in tileRuleToTilemap)
            {
                if (pair.Key.name == tile.name)
                {
                    targetTilemap = pair.Value;
                    break;
                }
            }

            // weird ok
            if (targetTilemap == null && dualGridTilemaps.Length > 0)
            {
                Debug.LogWarning($"Using default tilemap for {tile.name}");
                targetTilemap = dualGridTilemaps[0];
            }
        }

        if (targetTilemap == null)
        {
            Debug.LogError($"Could not find a tilemap for tile {tile.name}");
            return false;
        }

        targetTilemap.SetTile(position, tile);
        coordsToTileRule[position] = tile;
        return true;
    }

    // for loading generated worlds
    public void SetTilesBulk(List<Vector3Int> positions, DualGridRuleTile tile)
    {
        if (positions.Count == 0) return;

        // find the tilemap for the tile
        if (!tileRuleToTilemap.TryGetValue(tile, out Tilemap targetTilemap))
        {
            Debug.LogWarning($"No tilemap found for bulk placement of {tile.name}");
            return;
        }

        // create native arrays for the job system
        NativeArray<int3> positionsArray = new NativeArray<int3>(positions.Count, Allocator.TempJob);

        // convert Vector3Int to int3 for the job
        for (int i = 0; i < positions.Count; i++)
        {
            positionsArray[i] = new int3(positions[i].x, positions[i].y, positions[i].z);
        }

        // schedule the job
        var processingJob = new ProcessTilesJob
        {
            positions = positionsArray
        }.Schedule(positions.Count, 64);

        processingJob.Complete();

        // set the tiles in the main thread
        for (int i = 0; i < positions.Count; i++)
        {
            var position = positions[i];

            // clear any existing tiles first (probably dont need this but just in case)
            if (coordsToTileRule.TryGetValue(position, out DualGridRuleTile existingTile))
            {
                if (tileRuleToTilemap.TryGetValue(existingTile, out Tilemap existingTilemap))
                {
                    existingTilemap.SetTile(position, null);
                }
            }

            // set the new tile
            targetTilemap.SetTile(position, tile);
            coordsToTileRule[position] = tile;
        }

        // bye bye
        positionsArray.Dispose();
    }

    // process tiles in parallel using Burst
    [BurstCompile]
    private struct ProcessTilesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int3> positions;

        public void Execute(int index)
        {
            // probably never gonna use this, maybe remove it
            float3 pos = new float3(positions[index].x, positions[index].y, positions[index].z);
        }
    }

    // Burst compiled bulk terrain data preparation
    [BurstCompile]
    private struct TerrainProcessingJob : IJobParallelFor
    {
        [ReadOnly] public float2 noiseOffset;
        [ReadOnly] public float noiseScale;
        [ReadOnly] public float2 centerOffset;
        [ReadOnly] public float islandRadius;
        [ReadOnly] public float falloffStrength;
        [ReadOnly] public float waterLevel;
        [ReadOnly] public float sandLevel;

        [WriteOnly] public NativeArray<int> terrainTypes;
        [ReadOnly] public NativeArray<int2> positions;

        public void Execute(int i)
        {
            int2 pos = positions[i];

            // Calculate normalized distance from center (0 to 1+)
            float2 position = new float2(pos.x, pos.y);
            float distanceFromCenter = math.distance(position, centerOffset) / islandRadius;

            // Apply circular falloff so the island is round and not taking up the whole map
            float circularFalloff = math.clamp(distanceFromCenter * falloffStrength, 0, 1);

            // generate noise value
            float noiseValue = noise.cnoise(
                new float2((pos.x + noiseOffset.x) * noiseScale,
                         (pos.y + noiseOffset.y) * noiseScale)
            ) * 0.5f + 0.5f;

            // subtract the thingy to make the thingy round
            float heightValue = noiseValue - circularFalloff;

            // put things where they belong
            int terrainType;

            if (heightValue < waterLevel)
                terrainType = 0; // Water
            else if (heightValue < sandLevel)
                terrainType = 1; // Sand
            else
                terrainType = 2; // Grass

            terrainTypes[i] = terrainType;
        }
    }

    public int[] ProcessTerrainData(int mapSize, float noiseScale, Vector2 noiseOffset,
                                   float islandRadius, float falloffStrength,
                                   float waterLevel, float sandLevel)
    {
        int totalTiles = mapSize * mapSize;

        // native arrays for job
        var terrainTypes = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var positions = new NativeArray<int2>(totalTiles, Allocator.TempJob);

        // fill positions array
        int index = 0;
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                positions[index] = new int2(x, y);
                index++;
            }
        }

        // scheduule it
        float2 centerOffset = new float2(mapSize / 2, mapSize / 2);
        TerrainProcessingJob job = new TerrainProcessingJob
        {
            noiseOffset = noiseOffset,
            noiseScale = noiseScale,
            centerOffset = centerOffset,
            islandRadius = islandRadius,
            falloffStrength = falloffStrength,
            waterLevel = waterLevel,
            sandLevel = sandLevel,
            terrainTypes = terrainTypes,
            positions = positions
        };

        // execute the job
        JobHandle handle = job.Schedule(totalTiles, 64);
        handle.Complete();

        // back to normal person arrays
        int[] result = new int[totalTiles];
        terrainTypes.CopyTo(result);

        // bye bye
        terrainTypes.Dispose();
        positions.Dispose();

        return result;
    }

    public void SetAllTilesAtOnce(Dictionary<DualGridRuleTile, List<Vector3Int>> tileMap)
    {
        // dictionary to batch tiles by tilemap because i can only have one tilemap per tile rule
        Dictionary<Tilemap, Dictionary<Vector3Int, TileBase>> tilemapBatches = new Dictionary<Tilemap, Dictionary<Vector3Int, TileBase>>();

        foreach (var entry in tileMap)
        {
            DualGridRuleTile tile = entry.Key;
            List<Vector3Int> positions = entry.Value;

            // skip if theres no positions
            if (positions == null || positions.Count == 0)
                continue;

            // look for the tilemap
            if (!tileRuleToTilemap.TryGetValue(tile, out Tilemap targetTilemap))
            {
                Debug.LogWarning($"No tilemap found for bulk placement of {tile.name}, attempting fallback");

                // by name?
                foreach (var pair in tileRuleToTilemap)
                {
                    if (pair.Key.name == tile.name)
                    {
                        targetTilemap = pair.Value;
                        break;
                    }
                }

                // ok then, use the first one
                if (targetTilemap == null && dualGridTilemaps.Length > 0)
                {
                    targetTilemap = dualGridTilemaps[0];
                    Debug.LogWarning($"Using default tilemap for {tile.name}");
                }
            }

            if (targetTilemap == null)
            {
                Debug.LogError($"Could not find a tilemap for tile {tile.name}");
                continue;
            }

            // batch the tiles by tilemap
            if (!tilemapBatches.TryGetValue(targetTilemap, out var batch))
            {
                batch = new Dictionary<Vector3Int, TileBase>();
                tilemapBatches[targetTilemap] = batch;
            }

            // add the tiles to the batch
            foreach (var position in positions)
            {
                // clear any existing tiles first
                if (coordsToTileRule.TryGetValue(position, out var existingTile))
                {
                    // we just overwrite it
                    coordsToTileRule.Remove(position);
                }

                // add the tile to the batch
                batch[position] = tile;

                // update the map thing
                coordsToTileRule[position] = tile;
            }
        }

        // now we set the tiles for each tilemap
        foreach (var batch in tilemapBatches)
        {
            Tilemap tilemap = batch.Key;
            Dictionary<Vector3Int, TileBase> tiles = batch.Value;

            var positions = new Vector3Int[tiles.Count];
            var tileArray = new TileBase[tiles.Count];

            int i = 0;
            foreach (var pair in tiles)
            {
                positions[i] = pair.Key;
                tileArray[i] = pair.Value;
                i++;
            }

            // this kills the frames for a couple seconds lol
            tilemap.SetTiles(positions, tileArray);
        }

        // do the thing so the tiles show up
        foreach (var tilemap in dualGridTilemaps)
        {
            tilemap.RefreshAllTiles();
        }
    }

    // clear all tiles
    public void ClearAllTiles()
    {
        foreach (var tilemap in dualGridTilemaps)
        {
            tilemap.ClearAllTiles();
        }

        coordsToTileRule.Clear();
    }
}