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
    
    // Perspective camera settings
    [Tooltip("Layer mask for raycast to detect ground")]
    public LayerMask groundLayerMask;
    [Tooltip("Maximum distance for raycast")]
    public float maxRaycastDistance = 100f;
    [Tooltip("Y offset for cursor display")]
    public float cursorHeightOffset = 0.05f;
    
    // Ground plane for raycast hit detection
    private Plane groundPlane;

    private Decoration[] allDecorations;
    private float decorationRefreshTimer = 0f;
    private const float DECORATION_REFRESH_INTERVAL = 2f;

    void Start()
    {
        worldHandler = GameObject.Find("WorldHandler").GetComponent<WorldHandler>();
        
        // Find the decoration tilemap from the WorldGenerator
        var worldGenerator = FindObjectOfType<WorldGenerator>();
        if (worldGenerator != null)
        {
            decorationTilemap = worldGenerator.decorationTilemap;
            Debug.Log("Found decoration tilemap: " + (decorationTilemap != null));
            
            // Initialize the ground plane - assumes tilemap is on XY plane
            groundPlane = new Plane(Vector3.forward, Vector3.zero);
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

        // Cache all decorations
        RefreshAllDecorations();
    }

    private void RefreshAllDecorations()
    {
        allDecorations = FindObjectsOfType<Decoration>();
        decorationRefreshTimer = DECORATION_REFRESH_INTERVAL;
    }

    void Update() {
        Vector3Int tilePos;
        
        // Use raycast to find the tile position under cursor
        if (GetTilePositionUnderCursor(out tilePos, out Vector3 hitPoint)) {
            // Convert tile grid position to world position (centered on the tile)
            Vector3 gridSnappedPosition;
            
            // If we have access to the tilemap, use its conversion method
            if (decorationTilemap != null) {
                // This will get the center of the tile in world coordinates
                gridSnappedPosition = decorationTilemap.GetCellCenterWorld(tilePos);
            } else {
                // Fallback: manual calculation (assuming 1 unit = 1 tile)
                gridSnappedPosition = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0);
            }
            
            // Apply height offset to prevent z-fighting
            gridSnappedPosition += new Vector3(0, 0, -cursorHeightOffset);
            
            // Move cursor to the grid-snapped position
            transform.position = Vector3.Lerp(transform.position, gridSnappedPosition, 0.1f);
            
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

        decorationRefreshTimer -= Time.deltaTime;
        if (decorationRefreshTimer <= 0f)
        {
            RefreshAllDecorations();
        }
    }
    
    // Get the tile position under cursor using raycasting for perspective camera
    private bool GetTilePositionUnderCursor(out Vector3Int tilePos, out Vector3 hitPoint) {
        tilePos = new Vector3Int(int.MinValue, int.MinValue, 0);
        hitPoint = Vector3.zero;
        
        // Create ray from mouse position through camera
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // Method 1: Try mathematical plane-ray intersection
        float enter = 0.0f;
        if (groundPlane.Raycast(ray, out enter)) {
            hitPoint = ray.GetPoint(enter);
            tilePos = GetWorldPosTile(hitPoint);
            return true;
        }
        
        // Method 2: Physics raycast as fallback
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, groundLayerMask)) {
            hitPoint = hit.point;
            tilePos = GetWorldPosTile(hitPoint);
            return true;
        }
        
        return false;
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
        
        // Get world position for this tile
        Vector3 worldPos = decorationTilemap.GetCellCenterWorld(tilePos);
        
        // Method 1: Try using physics (more precise but potentially expensive)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.5f);
        foreach (Collider2D collider in colliders) {
            Decoration decoration = collider.GetComponent<Decoration>();
            if (decoration != null) {
                decorationCache[tilePos] = decoration;
                return decoration;
            }
        }
        
        // Method 2: Use the pre-cached decorations list
        if (allDecorations != null) {
            foreach (Decoration decoration in allDecorations) {
                if (decoration == null) continue;
                
                Vector3Int decorPos = decorationTilemap.WorldToCell(decoration.transform.position);
                if (decorPos == tilePos) {
                    decorationCache[tilePos] = decoration;
                    return decoration;
                }
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