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


public class VillageGenerator : MonoBehaviour
{
    public GameObject housePrefab;
    public GameObject housePrefabBack;

    private HashSet<Vector3Int> generatedHouses = new HashSet<Vector3Int>();
    private Tilemap tilemap; // Tilemap reference will be fetched dynamically
    private WorldHandler worldHandler; // Reference to the WorldHandler
    public int spacing;

    // Generate a village using Grass tile positions from WorldGenerator
     private void Start()
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

   public void GenerateVillage(List<Vector3Int> grassPositions, int houseCount)
    {
        if (grassPositions == null || grassPositions.Count == 0)
        {
            Debug.LogError("No valid grass positions available for village generation!");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned or found!");
            return;
        }
        // Calculate the midpoint of all grass positions
        Vector3Int startingPosition = CalculateMidpoint(grassPositions);
        Debug.Log($"Starting position calculated as: {startingPosition}");

        Debug.Log($"Generating {houseCount} houses in a grid-like pattern...");

        // Sort grass positions into a uniform grid
        List<bool> houseSide = new List<bool>();
        List<Vector3Int> gridPositions = GetGridPositions(grassPositions, startingPosition, houseSide);
        
        int count = 0;
        foreach (Vector3Int tilePosition in gridPositions)
        {
            if (count >= houseCount) break;

            // Place a house at the tile position
            PlaceHouse(tilePosition, houseSide[count]);
            count++;
        }
    }

    private Vector3Int CalculateMidpoint(List<Vector3Int> grassPositions)
    {
        int totalX = 50;
        int totalY = 50;

        foreach (Vector3Int position in grassPositions)
        {
            totalX += position.x;
            totalY += position.y;
        }

        int midpointX = totalX / grassPositions.Count;
        int midpointY = totalY / grassPositions.Count;

        // Return the midpoint as a Vector3Int
        return new Vector3Int(midpointX, midpointY, 0);
    }

    private List<Vector3Int> GetGridPositions(List<Vector3Int> grassPositions, Vector3Int startingPositionVillage, List<bool> whichHouse)
    {
        List<Vector3Int> gridPositions = new List<Vector3Int>();

        // Starting position (bottom-left corner of the grid)
        Vector3Int startingPosition = startingPositionVillage;

        foreach (Vector3Int position in grassPositions)
        {
            // Check if the position fits a grid pattern
            if (((position.x - startingPosition.x) % spacing == 0 && (position.y - startingPosition.y) % spacing == 0) && (Mathf.Abs(Mathf.Abs(position.x)+Mathf.Abs(position.y) - (startingPosition.x+startingPosition.y)) < 12.5))
            {
                gridPositions.Add(position); 
                if((((position.y - startingPosition.y)/spacing) % 2) == 0)
                {
                    whichHouse.Add(false);
                }
                else
                {
                    whichHouse.Add(true);
                }
            }
           
        }


        return gridPositions;
    }

    private void PlaceHouse(Vector3Int tilePosition, bool whichSide)
    {
        if (housePrefab != null)
        {
            // Convert tilemap position to world position
            Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);

            // Adjust Z-position to ensure the house is above the tilemap
            worldPosition.z = -1; // Set Z to -1 or any value above the tilemap layer

            // Instantiate the house prefab at the adjusted world position
            if(whichSide)
            {
                GameObject houseInstance = Instantiate(housePrefab, worldPosition, Quaternion.identity);
                Debug.Log($"House placed at tile position: {tilePosition}, world position: {worldPosition}");

                // Adjust house visibility
                SpriteRenderer renderer = houseInstance.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 10; // Ensure house sprite is rendered above the tilemap
                    Debug.Log($"Set house sprite sorting order to {renderer.sortingOrder}");
                }

                // Track the generated house to prevent duplicates
                generatedHouses.Add(tilePosition);
                }
            else
            {
                GameObject houseInstance = Instantiate(housePrefabBack, worldPosition, Quaternion.identity);
                Debug.Log($"House placed at tile position: {tilePosition}, world position: {worldPosition}");

                // Adjust house visibility
                SpriteRenderer renderer = houseInstance.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 10; // Ensure house sprite is rendered above the tilemap
                    Debug.Log($"Set house sprite sorting order to {renderer.sortingOrder}");
                }

                // Track the generated house to prevent duplicates
                generatedHouses.Add(tilePosition);
            }
        }
        else
        {
            Debug.LogError("House prefab is not assigned!");
        }
    }
}