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
    
    // Sound rate limiting (10 sounds per second = 0.1f cooldown)
    [Tooltip("Minimum time between tile placements in seconds (0.1 = 10 tiles/sec)")]
    public float placementCooldown = 0.1f;
    private float lastPlacementTime = 0f;
    
    // Track the last position where we placed a tile
    private Vector3Int lastTilePos = new Vector3Int(int.MinValue, int.MinValue, 0);
    // Track the last position where we already showed a placement error
    private Vector3Int lastErrorPos = new Vector3Int(int.MinValue, int.MinValue, 0);

    // Decoration references
    private Tilemap decorationTilemap;
    [Tooltip("Damage dealt to decorations on each hit")]
    public int damagePerHit = 1;
    
    // Cache for decoration objects by position
    private Dictionary<Vector3Int, Decoration> decorationCache = new Dictionary<Vector3Int, Decoration>();

    // Inventory references
    private InventoryHandler playerInventory;
    
    // Perspective camera settings
    [Tooltip("Layer mask for raycast to detect ground")]
    public LayerMask groundLayerMask;
    [Tooltip("Maximum distance for raycast")]
    public float maxRaycastDistance = 100f;
    [Tooltip("Y offset for cursor display")]
    public float cursorHeightOffset = 0.05f;
    
    [Tooltip("Size of collision check for placement")]
    public float collisionCheckRadius = 0.4f;
    [Tooltip("Layer mask for collision checking")]
    public LayerMask placementCollisionMask;
    
    // Ground plane for raycast hit detection
    private Plane groundPlane;

    private Decoration[] allDecorations;
    private float decorationRefreshTimer = 0f;
    private const float DECORATION_REFRESH_INTERVAL = 2f;

    void Start()
    {
        worldHandler = GameObject.Find("WorldHandler")?.GetComponent<WorldHandler>();
        if (worldHandler == null)
        {
            Debug.LogError("No WorldHandler found!");
        }
        
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

        // If placement collision mask isn't set, default to everything except player layer
        if (placementCollisionMask.value == 0)
        {
            placementCollisionMask = ~(1 << LayerMask.NameToLayer("Player"));
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
                if (!tilePos.Equals(lastTilePos) && canPlaceTile && playerInventory != null) {
                    // Get the currently selected item
                    InventoryItem selectedItem = playerInventory.GetSelectedItem();
                    
                    // Check if selected item is placeable and has a tile
                    if (selectedItem != null && selectedItem.isPlaceable && selectedItem.placeTile != null) {
                        // Check if player has the required resources
                        if (selectedItem.HasRequiredResources(playerInventory)) {
                            // Check if there's a collider at the placement position
                            if (!HasColliderAtPosition(gridSnappedPosition)) {
                                // Try to place the tile
                                bool tileChanged = worldHandler.SetTile(tilePos, selectedItem.placeTile);
                                
                                // If placement successful, consume resources and play sound
                                if (tileChanged) {
                                    
                                    // Consume the item itself (or required resources if defined)
                                    if (selectedItem.placementResources != null && selectedItem.placementResources.Length > 0) {
                                        // Use custom resources
                                        selectedItem.ConsumeResources(playerInventory);
                                    } else {
                                        // Use the item itself (1 item per placement)
                                        playerInventory.RemoveItem(selectedItem, 1);
                                    }
                                    // if name of tile is "wall", increment levee stat
                                    if (selectedItem.placeTile.name == "wall") {
                                        GameStats.Instance.IncrementStat("Levees Built", 1);
                                    }
                                    // Play sound for tile placement
                                    if (selectedItem.useSound != null && tileAudio != null) {
                                        tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                                        tileAudio.PlayOneShot(selectedItem.useSound);
                                    } else if (tileAudio != null) {
                                        tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                                        tileAudio.PlayOneShot(tileAudio.clip);
                                    }
                                    
                                    lastPlacementTime = Time.time;
                                    lastTilePos = tilePos;
                                    
                                    // Reset error position when we successfully place
                                    lastErrorPos = new Vector3Int(int.MinValue, int.MinValue, 0);
                                }
                            } else {
                                // Only provide feedback if this is a new position we're trying to build on
                                if (!tilePos.Equals(lastErrorPos)) {
                                    Debug.Log("Cannot place: Location is blocked by another object");
                                    lastErrorPos = tilePos;
                                }
                            }
                        } else {
                            // Only provide feedback once per position
                            if (!tilePos.Equals(lastErrorPos)) {
                                Debug.Log($"Not enough resources to place {selectedItem.itemName}!");
                                lastErrorPos = tilePos;
                            }
                        }
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
                // Also reset the error position when mouse is released
                lastErrorPos = new Vector3Int(int.MinValue, int.MinValue, 0);
            }
        }

        decorationRefreshTimer -= Time.deltaTime;
        if (decorationRefreshTimer <= 0f)
        {
            RefreshAllDecorations();
        }
    }
    
    // Check if there's a collider at the specified position
    private bool HasColliderAtPosition(Vector3 position)
    {
        // Use OverlapCircle to check for any colliders at the position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, collisionCheckRadius, placementCollisionMask);
        
        // Filter out any unwanted colliders (e.g., player, triggers)
        foreach (Collider2D collider in colliders)
        {
            // Skip triggers
            if (collider.isTrigger)
                continue;
                
            // Skip player collider
            if (collider.CompareTag("Player"))
                continue;
                
            // Found a valid blocking collider
            return true;
        }
        
        return false;
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