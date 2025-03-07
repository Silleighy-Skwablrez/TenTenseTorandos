using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using skner.DualGrid;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UI;

public class WorldGenerator : MonoBehaviour
{
    [Header("Dependencies")]
    public WorldHandler worldHandler;
    
    [Header("Tile Materials")]
    public DualGridRuleTile Water;
    public DualGridRuleTile Sand;
    public DualGridRuleTile Grass;
    
    [Header("Island Parameters")]
    public int mapSize = 100;
    public float noiseScale = 0.05f;
    public Vector2 noiseOffset;
    public float islandRadius = 40f;
    public float falloffStrength = 1.5f;
    
    [Header("Biome Thresholds")]
    [Range(0f, 1f)] public float waterLevel = 0.4f;
    [Range(0f, 1f)] public float sandLevel = 0.5f;

    [Header("Other")]
    public Slider progressSlider;
    public Text progressText;
    public Camera mainCamera;

    [Header("Generation Settings")]
    [Tooltip("How many tiles to process per frame")]
    public int tilesPerFrame = 250;
    
    void Start()
    {
        if (worldHandler == null)
            worldHandler = FindObjectOfType<WorldHandler>();
            
        // are you alive?
        if (!worldHandler.IsInitialized)
            worldHandler.Initialize();
        
        // register tile rules
        worldHandler.RegisterTileRules(new List<DualGridRuleTile> { Water, Sand, Grass });
            
        // doesnt the seed do this already?
        // noiseOffset = new Vector2(UnityEngine.Random.Range(0, 1000f), UnityEngine.Random.Range(0, 1000f));
        
        // set random seed
        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
        progressText.text = "Generating island...";

        // generate island
        StartCoroutine(GenerateIslandCoroutine());
    }
    
    public void GenerateIsland()
    {
        StartCoroutine(GenerateIslandCoroutine());
    }
    
    private IEnumerator GenerateIslandCoroutine()
    {
        if (progressSlider != null)
            progressSlider.value = 0;
            
        yield return null; // chill for a frame
        
        progressText.text = "Generating island...";
        
        // clear map, probably not needed but just in case
        ClearMap();
        
        if (progressSlider != null)
            progressSlider.value = 0.1f;
            
        yield return null; // chill for a frame
        
        // do the thing
        progressText.text = "Calculating terrain...";
        int[] terrainTypes = GenerateTerrainData();
        
        if (progressSlider != null)
            progressSlider.value = 0.2f;
            
        yield return null; // chill for a frame
        
        progressText.text = "Loading terrain...";
        
        // place tiles
        Dictionary<DualGridRuleTile, List<Vector3Int>> allTiles = new Dictionary<DualGridRuleTile, List<Vector3Int>>
        {
            { Water, new List<Vector3Int>() },
            { Sand, new List<Vector3Int>() },
            { Grass, new List<Vector3Int>() }
        };
        
        // classify tiles
        int index = 0;
        int processedTiles = 0;
        float progressStart = 0.2f;
        float progressEnd = 0.6f;
        
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x - mapSize/2, y - mapSize/2, 0);
                
                // group by terrain type
                switch (terrainTypes[index])
                {
                    case 0:
                        allTiles[Water].Add(tilePosition);
                        break;
                    case 1:
                        allTiles[Sand].Add(tilePosition);
                        break;
                    default:
                        allTiles[Grass].Add(tilePosition);
                        break;
                }
                
                index++;
                processedTiles++;
                
                // update progress
                if (processedTiles % tilesPerFrame == 0)
                {
                    if (progressSlider != null)
                    {
                        float progress = progressStart + (progressEnd - progressStart) * ((float)processedTiles / (mapSize * mapSize));
                        progressSlider.value = progress;
                    }
                    yield return null; // chill for a frame
                }
            }
        }
        
        if (progressSlider != null)
            progressSlider.value = 0.6f;
        
        yield return null; // chill for a frame
        
        // load map
        progressText.text = "Placing tiles...";
        worldHandler.SetAllTilesAtOnce(allTiles);
        
        if (progressSlider != null)
            progressSlider.value = 0.9f;
        
        yield return null;
        
        progressText.text = "Finalizing...";
        
        // done
        if (progressSlider != null)
            progressSlider.value = 1.0f;
        
        progressText.text = "Done!";    
        yield return new WaitForSeconds(0.5f);

        // gta camera transition type beat
        float cameraSize = mainCamera.orthographicSize;
        while (cameraSize > 6.1f)
        {
            cameraSize = Mathf.Lerp(cameraSize, 6, 0.025f);
            mainCamera.orthographicSize = cameraSize;
            yield return null;
        }

        // hide progress bar and slider
        progressSlider.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        
        if (progressSlider != null)
            progressSlider.value = 0;
    }
    
    private IEnumerator PlaceTilesInChunks(List<Vector3Int> positions, DualGridRuleTile tile, float progressStart, float progressEnd)
    {
        int processed = 0;
        int totalPositions = positions.Count;
        
        progressText.text = $"Placing {tile.name} tiles...";
        // i want my awesome loading screen to actually be visible instead of the game freezing, so we'll do this in chunks
        for (int i = 0; i < totalPositions; i += tilesPerFrame)
        {
            // this chunk size
            int chunkSize = Mathf.Min(tilesPerFrame, totalPositions - i);
            
            // subset for this chunk
            List<Vector3Int> chunk = positions.GetRange(i, chunkSize);
            
            // process the chunk
            worldHandler.SetTilesBulk(chunk, tile);
            
            // update progress stuff
            processed += chunkSize;
            if (progressSlider != null)
            {
                float progress = progressStart + (progressEnd - progressStart) * ((float)processed / totalPositions);
                progressSlider.value = progress;
            }
            
            yield return null; // chill for a frame
        }
    }
    
    private int[] GenerateTerrainData()
    {
        // check if WorldHandler is available to generate terrain data for us
        if (worldHandler != null)
        {
            return worldHandler.ProcessTerrainData(
                mapSize, 
                noiseScale, 
                noiseOffset, 
                islandRadius, 
                falloffStrength, 
                waterLevel, 
                sandLevel
            );
        }
        
        // fallback to slower method if WorldHandler is not available D:
        int totalTiles = mapSize * mapSize;
        
        // make these arrays because we need to pass them to the job
        var terrainTypes = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var positions = new NativeArray<int2>(totalTiles, Allocator.TempJob);
        
        // fill the positions array
        int index = 0;
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (progressSlider != null)
                    progressSlider.value = 0.1f + ((float)index / totalTiles * 0.1f); 
                positions[index] = new int2(x, y);
                index++;
            }
        }
        
        // schedule the job
        float2 centerOffset = new float2(mapSize / 2, mapSize / 2);
        var job = new IslandGenerationJob
        {
            noiseOffset = new float2(noiseOffset.x, noiseOffset.y),
            noiseScale = noiseScale,
            centerOffset = centerOffset,
            islandRadius = islandRadius,
            falloffStrength = falloffStrength,
            waterLevel = waterLevel,
            sandLevel = sandLevel,
            terrainTypes = terrainTypes,
            positions = positions
        };
        
        // run the job
        JobHandle handle = job.Schedule(totalTiles, 64);
        handle.Complete();
        
        // normal array to return
        int[] result = new int[totalTiles];
        terrainTypes.CopyTo(result);
        
        // bye bye
        terrainTypes.Dispose();
        positions.Dispose();
        
        return result;
    }
    
    [BurstCompile]
    private struct IslandGenerationJob : IJobParallelFor
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
            
            // distance from center
            float2 position = new float2(pos.x, pos.y);
            float distanceFromCenter = math.distance(position, centerOffset) / islandRadius;
            
            // make circle
            float circularFalloff = math.clamp(distanceFromCenter * falloffStrength, 0, 1);
            
            // get the value
            float noiseValue = noise.cnoise(
                new float2((pos.x + noiseOffset.x) * noiseScale,
                         (pos.y + noiseOffset.y) * noiseScale)
            ) * 0.5f + 0.5f;
            
            // use circle to make circle island, wow!
            float heightValue = noiseValue - circularFalloff;
            
            // make things the thing they should be
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
    public void ClearMap()
    {
        worldHandler.ClearAllTiles();
    }
}