using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;

public partial class CursorController : MonoBehaviour {

    private WorldHandler worldHandler;
    // public audio for placing and breaking tiles
    public AudioSource tileAudio;
    public AudioClip breakSound; // Add sound for breaking decorations
    public DualGridRuleTile tile;
    
    // Sound rate limiting (10 sounds per second = 0.1f cooldown)
    [Tooltip("Minimum time between tile placements in seconds (0.1 = 10 tiles/sec)")]
    public float placementCooldown = 0.1f;
    private float lastPlacementTime = 0f;
    
    // Track the last position where we placed a tile
    private Vector3Int lastTilePos = new Vector3Int(int.MinValue, int.MinValue, 0);

    // Decoration references
    private Tilemap decorationTilemap;
    [Tooltip("Damage dealt to decorations on each hit")]
    public int damagePerHit = 1;
    
    // Cache for decoration objects by position
    private Dictionary<Vector3Int, Decoration> decorationCache = new Dictionary<Vector3Int, Decoration>();

    // Inventory references
    private InventoryHandler playerInventory;
    [Tooltip("The Rock item required for building walls")]
    public InventoryItem rockItem;
    [Tooltip("Number of rocks required to build a wall")]
    public int rocksRequired = 2;

    void Start()
    {
        worldHandler = GameObject.Find("WorldHandler").GetComponent<WorldHandler>();
        
        // Find the decoration tilemap from the WorldGenerator
        var worldGenerator = FindObjectOfType<WorldGenerator>();
        if (worldGenerator != null)
        {
            decorationTilemap = worldGenerator.decorationTilemap;
            Debug.Log("Found decoration tilemap: " + (decorationTilemap != null));
        }
        else
        {
            Debug.LogError("No WorldGenerator found in the scene!");
        }

        // Find player inventory
        playerInventory = FindObjectOfType<InventoryHandler>();
        if (playerInventory == null)
        {
            Debug.LogError("No InventoryHandler found in the scene!");
        }
    }

    void Update() {
        var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int tilePos = GetWorldPosTile(mouseWorldPos);
        transform.position = Vector3.Lerp(transform.position, tilePos + new Vector3(0.5f, 0.5f, -1), 0.1f);

        bool canPlaceTile = (Time.time - lastPlacementTime >= placementCooldown);

        if (Input.GetMouseButton(1)) {
            // Only set cell and play audio if we're at a new position AND cooldown has passed
            if (!tilePos.Equals(lastTilePos) && canPlaceTile) {
                // Check if player has enough rocks
                if (playerInventory != null && rockItem != null && HasEnoughRocks()) {
                    bool tileChanged = worldHandler.SetTile(tilePos, tile);
                    
                    // Play sound and update timestamps
                    if (tileChanged) {
                        // Consume the rocks from inventory
                        playerInventory.RemoveItem(rockItem, rocksRequired);
                        
                        tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                        tileAudio.PlayOneShot(tileAudio.clip);
                        lastPlacementTime = Time.time;
                        lastTilePos = tilePos;
                    }
                } else {
                    // Visual or audio feedback that player needs more rocks could be added here
                    string rockCount = (playerInventory != null && rockItem != null) ? playerInventory.GetItemCount(rockItem).ToString() : "0";
                    Debug.Log("Not enough rocks to build a wall! Need " + rocksRequired + " rocks. You have: " + rockCount);
                }
            }
        } else if (Input.GetMouseButton(0)) {
            // if lmb pressed, damage decoration tiles
            if (canPlaceTile) {
                if (decorationTilemap == null) {
                    Debug.LogError("No decoration tilemap assigned!");
                    return;
                }
                
                // Check if there's a decoration tile at the cursor position
                if (decorationTilemap.HasTile(tilePos)) {
                    Debug.Log($"Found decoration tile at {tilePos}");
                    
                    // Try to get or find the decoration at this position
                    Decoration decoration = GetDecorationAtPosition(tilePos);
                    
                    if (decoration != null) {
                        // Damage the decoration
                        decoration.TakeDamage(damagePerHit);
                        Debug.Log($"Damaged {decoration.decorationName} at {tilePos}, health now {decoration.health}/{decoration.maxHealth}");
                        
                        // Play break sound if available
                        if (breakSound != null) {
                            tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                            tileAudio.PlayOneShot(breakSound);
                        } else {
                            // Use default sound if no break sound is set
                            tileAudio.pitch = UnityEngine.Random.Range(0.7f, 0.9f); // Lower pitch for breaking
                            tileAudio.PlayOneShot(tileAudio.clip);
                        }
                        
                        lastPlacementTime = Time.time;
                        lastTilePos = tilePos;
                    } else {
                        Debug.LogWarning($"No decoration component found at {tilePos} despite tile being present!");
                        // Force-remove the tile if no component was found
                        decorationTilemap.SetTile(tilePos, null);
                    }
                }
            }
        } else {
            // Reset the last position when no mouse button is pressed
            lastTilePos = new Vector3Int(int.MinValue, int.MinValue, 0);
        }
    }

    // Check if player has enough rocks to build a wall
    private bool HasEnoughRocks() {
        if (playerInventory == null || rockItem == null) return false;
        
        int rockCount = playerInventory.GetItemCount(rockItem);
        return rockCount >= rocksRequired;
    }

    private Decoration GetDecorationAtPosition(Vector3Int tilePos) {
        // First check cache
        if (decorationCache.TryGetValue(tilePos, out Decoration cachedDecoration)) {
            if (cachedDecoration != null && cachedDecoration.gameObject != null) {
                return cachedDecoration;
            } else {
                // Remove invalid cache entries
                decorationCache.Remove(tilePos);
            }
        }
        
        // Look for a Decoration component at this position
        Vector3 worldPos = decorationTilemap.GetCellCenterWorld(tilePos);
        
        // Method 1: Try using physics
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.5f);
        Debug.Log($"Found {colliders.Length} colliders at {worldPos}");
        
        foreach (Collider2D collider in colliders) {
            Decoration decoration = collider.GetComponent<Decoration>();
            if (decoration != null) {
                // Cache it for later
                decorationCache[tilePos] = decoration;
                return decoration;
            }
        }
        
        // Method 2: Try direct GameObject search
        // Find all decoration objects in the scene
        Decoration[] allDecorations = FindObjectsOfType<Decoration>();
        foreach (Decoration decoration in allDecorations) {
            Vector3Int decorPos = decorationTilemap.WorldToCell(decoration.transform.position);
            if (decorPos == tilePos) {
                // Cache it for later
                decorationCache[tilePos] = decoration;
                return decoration;
            }
        }
        
        return null;
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos) {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt, 0);
    }
}