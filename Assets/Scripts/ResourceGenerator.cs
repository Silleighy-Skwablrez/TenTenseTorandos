using System.Collections;
using System.Collections.Generic;
using System.Linq; // Add this line to import LINQ functionality
using UnityEngine;
using skner.DualGrid;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class ResourceGenerator : MonoBehaviour
{
    public GameObject DriftWood;
    public GameObject SandPile;

    private HashSet<Vector3Int> generatedItems = new HashSet<Vector3Int>();
    private Tilemap tilemap; // Tilemap reference will be fetched dynamically
    private WorldHandler worldHandler; // Reference to the WorldHandler
    // Start is called before the first frame update
    void Start()
    {
        // Find the WorldHandler in the scene
        worldHandler = FindObjectOfType<WorldHandler>();

        if (worldHandler == null)
        {
            Debug.LogError("WorldHandler not found in the scene!");
            return;
        }

        // Use the first tilemap from dualGridTilemaps for positioning houses
        if (worldHandler.dualGridTilemaps.Length > 0)
        {
            tilemap = worldHandler.dualGridTilemaps[0]; // Select the appropriate tilemap
        }
        else
        {
            Debug.LogError("No tilemaps found in WorldHandler's dualGridTilemaps!");
        }
    }

    public void GenerateWood(List<Vector3Int> sandPositions, int woodCount)
    {
        if (sandPositions == null || sandPositions.Count == 0)
        {
            Debug.LogError("No valid sand positions available for wood generation!");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned or found!");
            return;
        }

        List<Vector3Int> randomPositions = RandomPositions(sandPositions, woodCount);
        int count = 0;
        foreach (Vector3Int tilePosition in randomPositions)
        {
            if (count >= woodCount) break;

            Place(tilePosition, DriftWood);
        }
    }

    private List<Vector3Int> RandomPositions(List<Vector3Int> positions, int count)
    {
        List<Vector3Int> randomized = new List<Vector3Int>();
        List<int> pastNums = new List<int>();
        int i = 0;
        while (i < count)
        {
            int randomNum = UnityEngine.Random.Range(0, positions.Count);
            if (!pastNums.Contains(randomNum))
            {
                randomized.Add(positions[randomNum]);
                pastNums.Add(randomNum);
                i++;
            }
        }
        return randomized;
    }

    void Place(Vector3Int tilePosition, GameObject placeable)
    {
        if (placeable != null)
        {
            Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);
            worldPosition.z = -1;
            GameObject placing = Instantiate(placeable, worldPosition, Quaternion.identity);
            Debug.Log($"Resource placed at tile position: {tilePosition}, world position: {worldPosition}");

            // Adjust visibility
            SpriteRenderer renderer = placing.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10; // Ensure sprite is rendered above the tilemap
            }

            // Add or configure Decoration component for harvesting
            Decoration decoration = placing.GetComponent<Decoration>();
            if (decoration == null)
            {
                decoration = placing.AddComponent<Decoration>();
            }

            // Configure the decoration for driftwood
            if (placeable == DriftWood)
            {
                decoration.decorationName = "Driftwood";
                decoration.breakable = true;
                decoration.maxHealth = 3;
                decoration.health = 3;

                // Make sure to set up the drop item - you'll need to assign this in the Inspector
                // or find a reference to a wood/driftwood item in your inventory system
                InventoryItem woodItem = Resources.FindObjectsOfTypeAll<InventoryItem>().FirstOrDefault(item => item.name.ToLower().Contains("wood"));

                if (woodItem != null)
                {
                    decoration.dropItem = woodItem;
                }
                else
                {
                    Debug.LogWarning("Could not find wood item for driftwood drops!");
                }

                // Find and assign the ground item prefab
                GameObject groundItemPrefab = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(go => go.GetComponent<GroundItem>() != null);

                if (groundItemPrefab != null)
                {
                    decoration.groundItemPrefab = groundItemPrefab;
                }
                else
                {
                    Debug.LogWarning("Could not find ground item prefab for dropping items!");
                }
            }

            // Track the generated item to prevent duplicates
            generatedItems.Add(tilePosition);
        }
        else
        {
            Debug.LogError("Item prefab is not assigned!");
        }
    }
}