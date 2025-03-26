using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Decoration : MonoBehaviour
{
    public string decorationName;
    public bool breakable;
    public int health;
    public int maxHealth;
    public InventoryItem dropItem;
    public TileBase tile;
    
    [Header("Drop Settings")]
    public GameObject groundItemPrefab;  // Assign a prefab with GroundItem component
    public float dropChance = 1.0f;      // Default 100% chance to drop
    
    private static Tilemap decorationTilemap;
    private static InventoryHandler inventoryHandler;

    public void TakeDamage(int damage)
    {
        if (!breakable)
            return;
            
        health -= damage;
        Debug.Log($"Decoration '{decorationName}' took {damage} damage. Health: {health}/{maxHealth}");
        
        if (health <= 0)
        {
            Debug.Log($"Decoration '{decorationName}' health depleted. Calling Die()");
            Die();
        }
    }

    public void Die()
    {
        Debug.Log($"Decoration '{decorationName}' Die() method called");
        
        // Find the tilemap if not already found
        if (decorationTilemap == null)
            decorationTilemap = FindObjectOfType<WorldGenerator>()?.decorationTilemap;
            
        // Remove the tile from the tilemap
        if (decorationTilemap != null)
        {
            // Convert world position to cell position
            Vector3Int cellPosition = decorationTilemap.WorldToCell(transform.position);
            decorationTilemap.SetTile(cellPosition, null);
            Debug.Log($"Removed decoration tile at position {cellPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find decoration tilemap");
        }

        // Spawn a ground item if we have a drop item and prefab
        if (dropItem != null && groundItemPrefab != null)
        {
            Debug.Log($"Attempting to drop item: {dropItem.name}, using prefab: {groundItemPrefab.name}");
            
            // Apply drop chance
            float roll = Random.value;
            Debug.Log($"Drop chance roll: {roll} vs required: {dropChance}");
            
            if (roll <= dropChance)
            {
                // Spawn the ground item
                Vector3 dropPosition = transform.position;
                Debug.Log($"Spawning ground item at position {dropPosition}");
                
                GameObject groundItem = Instantiate(groundItemPrefab, dropPosition, Quaternion.identity);
                GroundItem itemComponent = groundItem.GetComponent<GroundItem>();
                
                if (itemComponent != null)
                {
                    Debug.Log("GroundItem component found, setting up item");
                    itemComponent.Setup(dropItem);
                    
                    // Ensure it's at the right position (might be unnecessary but just to be sure)
                    groundItem.transform.position = dropPosition;
                    
                    // Make sure it's visible in the scene
                    SpriteRenderer renderer = groundItem.GetComponentInChildren<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 10; // Ensure it's drawn above other elements
                        Debug.Log($"Set sprite sorting order to {renderer.sortingOrder}");
                    }
                }
                else
                {
                    Debug.LogError("Ground item prefab missing GroundItem component!");
                }
            }
            else
            {
                Debug.Log("Item didn't drop due to drop chance calculation");
            }
        }
        else
        {
            if (dropItem == null)
                Debug.LogWarning("No drop item assigned to decoration");
            if (groundItemPrefab == null)
                Debug.LogWarning("No ground item prefab assigned to decoration");
        }
        
        Debug.Log($"Destroying decoration GameObject: {gameObject.name}");
        Destroy(gameObject);
    }


    public void Start()
    {
        // Ensure health is properly initialized
        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Decoration '{decorationName}' has invalid maxHealth ({maxHealth}). Setting to 1.");
            maxHealth = 1;
        }
        
        health = maxHealth;
        
        // Validate other critical components
        if (breakable && dropItem != null && groundItemPrefab == null)
        {
            Debug.LogWarning($"Decoration '{decorationName}' has a drop item but no ground item prefab!");
        }
        
        // Add a collider if missing (for player interaction)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"Decoration '{decorationName}' has no Collider2D. Adding one.");
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Make sure the collider is a trigger so it doesn't block player movement
        collider.isTrigger = true;
    }
}