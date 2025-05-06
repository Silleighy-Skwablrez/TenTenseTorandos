using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using skner.DualGrid;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

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

    [Header("Decorations")]
    public Tilemap decorationTilemap; // Regular Unity tilemap for decorations
    public List<DecorationData> decorations; // List of decoration data
    [Range(0f, 1f)] public float decorationDensity = 0.1f; // Percentage of grass tiles that get decorations
    public bool placeDecorations = true; // Toggle to enable/disable decorations

    [Header("Other")]
        public Slider progressSlider;
        public Text progressText;
        public GameObject gameUi;
        public Camera mainCamera;

    [Header("Generation Settings")]
    [Tooltip("How many tiles to process per frame")]
    public int tilesPerFrame = 250;

    // List different materials tile positions
    private List<Vector3Int> grassTilePositions;
    private List<Vector3Int> sandTilePositions;

    void Start()
    {
        VillageGenerator villageGenerator = FindObjectOfType<VillageGenerator>();
        ResourceGenerator resourceGenerator = FindObjectOfType<ResourceGenerator>();
        RectTransform gameUiRect = gameUi.GetComponent<RectTransform>();
        if (gameUiRect != null)
        {
            Vector2 offsetMin = gameUiRect.offsetMin;
            gameUiRect.offsetMin = new Vector2(offsetMin.x, -10000);
        }

        if (worldHandler == null)
            worldHandler = FindObjectOfType<WorldHandler>();

        // are you alive?
        if (!worldHandler.IsInitialized)
            worldHandler.Initialize();

        // register tile rules
        worldHandler.RegisterTileRules(new List<DualGridRuleTile> { Water, Sand, Grass });

        // doesnt the seed do this already?
        noiseOffset = new Vector2(UnityEngine.Random.Range(0, 1000f), UnityEngine.Random.Range(0, 1000f));

        // set random seed
        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
        progressText.text = "Generating island...";

        // generate island
        StartCoroutine(GenerateIslandCoroutine());
        // Wait for island generation to complete, then generate the village and resources
        StartCoroutine(GenerateVillageAfterIsland(villageGenerator));
        StartCoroutine(GenerateResourcesAfterIsland(resourceGenerator));
    }

    public List<Vector3Int> GetGrassTilePositions()
    {
        return grassTilePositions;
    }

    public List<Vector3Int> GetSandTilePositions()
    {
        return sandTilePositions;
    }

    private IEnumerator GenerateVillageAfterIsland(VillageGenerator villageGenerator)
    {
    // Wait until island generation is done
    yield return new WaitUntil(() => grassTilePositions != null);

    // Pass Grass tile positions to the VillageGenerator
    villageGenerator.GenerateVillage(grassTilePositions, 30);
    }

    private IEnumerator GenerateResourcesAfterIsland(ResourceGenerator resourceGenerator)
    {
    //Wait until island generation is done
    yield return new WaitUntil(() => sandTilePositions != null);

    // Pass Sand tile positions to the ResourceGenerator
    resourceGenerator.GenerateWood(sandTilePositions, 20);
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
                Vector3Int tilePosition = new Vector3Int(x - mapSize / 2, y - mapSize / 2, 0);

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

        // Assign tile positions after classification
        grassTilePositions = allTiles[Grass];
        sandTilePositions = allTiles[Sand];
        Debug.Log($"Total Grass Tiles: {grassTilePositions.Count}"); // Debug log to verify grass tiles
        Debug.Log($"Total Sand Tiles: {sandTilePositions.Count}"); // Debug log to verify sand tiles
        
        progressText.text = "Finalizing...";

        // done
        if (progressSlider != null)
            progressSlider.value = 1.0f;

        progressText.text = "Done!";
        yield return new WaitForSeconds(1.5f);

        // hide progress bar and slider
        progressSlider.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);

        //set gameui rect transform bottom to 25
        // Show the game UI and set its position

        RectTransform gameUiRect = gameUi.GetComponent<RectTransform>();
        if (gameUiRect != null)
        {
            Vector2 offsetMin = gameUiRect.offsetMin;
            gameUiRect.offsetMin = new Vector2(offsetMin.x, 25);
        }

        // gta camera transition type beat
        float cameraSize = mainCamera.orthographicSize;
        while (cameraSize > 6.1f)
        {
            cameraSize = Mathf.Lerp(cameraSize, 6, 0.025f);
            mainCamera.orthographicSize = cameraSize;
            yield return null;
        }

        mainCamera.orthographicSize = cameraSize;
        yield return null;
        
        // Place decorations if enabled
        if (placeDecorations && decorationTilemap != null && decorations != null && decorations.Count > 0)
        {
            yield return PlaceDecorationsCoroutine(allTiles[Grass]);
        }

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

    private IEnumerator PlaceDecorationsCoroutine(List<Vector3Int> grassPositions)
    {
        progressText.gameObject.SetActive(true);
        progressSlider.gameObject.SetActive(true);
        progressText.text = "Adding decorations...";
        progressSlider.value = 0f;
        
        // Calculate how many decorations to place
        int decorationCount = Mathf.RoundToInt(grassPositions.Count * decorationDensity);
        
        // Create a copy of grass positions to randomly select from
        List<Vector3Int> availablePositions = new List<Vector3Int>(grassPositions);
        
        // Place decorations
        int processed = 0;
        
        if (decorationTilemap != null)
            decorationTilemap.ClearAllTiles(); // Clear existing decorations
        
        for (int i = 0; i < decorationCount; i++)
        {
            if (availablePositions.Count == 0 || decorations.Count == 0)
                break;
                
            // Select a random position from available grass tiles
            int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
            Vector3Int cellPos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex); // Ensure no duplicates
            
            // Select a random decoration
            DecorationData decorData = decorations[UnityEngine.Random.Range(0, decorations.Count)];
            
            // Place the tile on the decoration tilemap
            if (decorationTilemap != null && decorData.tile != null)
                decorationTilemap.SetTile(cellPos, decorData.tile);
                
            // Instantiate the decoration game object if a prefab exists
            if (decorData.decorationPrefab != null)
            {
                // Convert tilemap position to world position
                Vector3 worldPos = decorationTilemap.GetCellCenterWorld(cellPos);
                
                // Instantiate the decoration
                GameObject decorObj = Instantiate(decorData.decorationPrefab, worldPos, Quaternion.identity);
                
                // Set up the Decoration component
                Decoration decoration = decorObj.GetComponent<Decoration>();
                if (decoration != null)
                {
                    decoration.decorationName = decorData.decorationName;
                    decoration.breakable = decorData.breakable;
                    decoration.maxHealth = decorData.maxHealth;
                    decoration.health = decorData.maxHealth;
                    decoration.dropItem = decorData.dropItem;
                    decoration.tile = decorData.tile;
                }
            }
            
            processed++;
            
            // Update progress in chunks to avoid freezing
            if (processed % tilesPerFrame == 0)
            {
                progressText.text = $"Adding decorations... {processed}/{decorationCount}";
                progressSlider.value = (float)processed / decorationCount;
                yield return null; // Allow frame to render
            }
        }
        
        progressText.text = "Decorations complete!";
        progressSlider.value = 1f;
        yield return new WaitForSeconds(0.5f);
        
        progressText.gameObject.SetActive(false);
        progressSlider.gameObject.SetActive(false);
    }
}