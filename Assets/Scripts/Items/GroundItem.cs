using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundItem : MonoBehaviour
{
    public InventoryItem item;
    public SpriteRenderer spriteRenderer;
    
    // Animation parameters
    public float bobHeight = 0.1f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 45f;
    
    // Player attraction parameters
    public float attractionRadius = 3f;      // Distance at which item starts moving toward player
    public float minAttractionSpeed = 1.5f;  // Minimum speed when attraction begins
    public float maxAttractionSpeed = 5f;    // Maximum speed when very close to player
    public float pickupDistance = 0.8f;      // Distance for automatic pickup
    
    // Pickup delay parameters
    [Header("Pickup Delay Settings")]
    [Tooltip("Time in seconds before this item can be picked up after being dropped")]
    public float pickupDelay = 1.0f;
    [Tooltip("Can the item be attracted to the player during pickup delay?")]
    public bool attractDuringDelay = false;
    private float spawnTime;
    
    // Sound parameters
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    
    private Vector3 bobOrigin;
    private float timeOffset;
    private Transform playerTransform;
    private bool isAttracting = false;
    private bool canAnimate = true;
    private InventoryHandler inventoryHandler;
    
    // Initial "pop" effect
    public float popForce = 5.0f;
    public float popDuration = 0.7f;
    private Vector2 popDirection;
    private float popTimer = 0f;
    private bool hasPopped = false;
    
    // Debug variables
    private bool debugMode = true;
    private float debugTimer = 0f;
    private float pickupAttemptTimer = 0f;
    
    private void Awake()
    {
        // Initialize default values
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        bobOrigin = transform.position;
        timeOffset = Random.value * 6.28f;
        
        // Generate random direction for pop with strong upward bias
        popDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(1.0f, 2.0f)
        ).normalized;
        
        // Force ALL colliders to be triggers
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0)
        {
            // No collider at all, add one
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            Debug.Log("Added collider to GroundItem");
        }
        else
        {
            // Ensure all colliders are triggers
            foreach (Collider2D col in colliders)
            {
                col.isTrigger = true;
                Debug.Log($"Set collider {col.name} to trigger");
            }
        }
        
        // Record spawn time for pickup delay
        spawnTime = Time.time;
        
        // Find player reference and inventory
        FindPlayer();
        FindInventoryHandler();
    }
    
    private void Start()
    {
        ForcePopEffect();
    }
    
    private void OnEnable()
    {
        if (playerTransform == null)
            FindPlayer();
            
        if (inventoryHandler == null)
            FindInventoryHandler();
    }
    
    public void ForcePopEffect()
    {
        popTimer = popDuration;
        hasPopped = false;
        
        popDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(1.0f, 2.0f)
        ).normalized;
    }
    
    // Find player for movement/following
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            if (debugMode) Debug.Log($"Found player: {player.name} at {player.transform.position}");
        }
        else
        {
            Debug.LogWarning("Ground item couldn't find player with tag 'Player'");
        }
    }
    
    // Find inventory handler separately (may be on UI)
    private void FindInventoryHandler()
    {
        // Try to find the inventory handler through the static instance first
        if (InventoryHandler.instance != null)
        {
            inventoryHandler = InventoryHandler.instance;
            Debug.Log("Found inventory handler through singleton instance");
            return;
        }
        
        // Fallback: try to find any inventory handler in the scene
        inventoryHandler = FindObjectOfType<InventoryHandler>();
        
        if (inventoryHandler != null)
        {
            Debug.Log("Found inventory handler in scene");
        }
        else 
        {
            Debug.LogError("Could not find any InventoryHandler in the scene!");
        }
    }

    public void Setup(InventoryItem newItem)
    {
        if (newItem == null) return;
        
        item = newItem;
        
        if (spriteRenderer != null && item.itemSprite != null)
        {
            spriteRenderer.sprite = item.itemSprite;
        }
        
        bobOrigin = transform.position;
        ForcePopEffect();
        
        if (playerTransform == null)
            FindPlayer();
            
        if (inventoryHandler == null)
            FindInventoryHandler();
        
        // Reset the spawn time when item is set up
        spawnTime = Time.time;
        
        Debug.Log($"Ground item set up with {newItem.itemName}, position: {transform.position}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (item == null)
        {
            Debug.LogWarning("OnTriggerEnter2D - Item is null!");
            return;
        }
        
        // Check pickup delay
        if (!CanPickup())
        {
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player collision detected with {item.itemName}");
            
            // Make sure we have the inventory handler
            if (inventoryHandler == null)
                FindInventoryHandler();
                
            if (inventoryHandler != null)
            {
                bool added = inventoryHandler.AddItem(item, 1);
                Debug.Log($"AddItem result: {added}");
                
                if (added)
                {
                    AudioSource playerAudio = other.GetComponent<AudioSource>();
                    if (playerAudio != null && playerAudio.clip != null)
                    {
                        playerAudio.pitch = Random.Range(minPitch, maxPitch);
                        playerAudio.PlayOneShot(playerAudio.clip);
                    }
                    
                    Debug.Log($"Collected via trigger: {item.itemName}");
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogError("No InventoryHandler found in the scene!");
            }
        }
    }

    private void Update()
    {
        // Debug periodic logging
        if (debugMode)
        {
            debugTimer -= Time.deltaTime;
            if (debugTimer <= 0)
            {
                debugTimer = 5f;
                float timeUntilPickup = pickupDelay - (Time.time - spawnTime);
                string pickupStatus = timeUntilPickup > 0 ? 
                    $"Pickup in: {timeUntilPickup:F1}s (no attraction)" : 
                    "Ready for pickup & attraction";
                
                Debug.Log($"GroundItem {name}: {pickupStatus}, item={item?.itemName}, " +
                          $"player={playerTransform != null}, pos={transform.position}");
            }
        }
        
        // Apply pop effect
        if (popTimer > 0)
        {
            hasPopped = true;
            popTimer -= Time.deltaTime;
            
            float popStrength = Mathf.Clamp01(popTimer / popDuration) * popForce;
            Vector3 popMovement = new Vector3(
                popDirection.x * popStrength * Time.deltaTime,
                popDirection.y * popStrength * Time.deltaTime,
                0
            );
            
            transform.position += popMovement;
            bobOrigin = new Vector3(transform.position.x, transform.position.y, bobOrigin.z);
            return;
        }
        
        if (!hasPopped)
        {
            ForcePopEffect();
            return;
        }
        
        // Find player if needed
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }
        
        if (item == null) return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(playerTransform.position.x, playerTransform.position.y)
        );
        
        // Only try pickup if the delay has passed
        if (CanPickup())
        {
            // Try pickup more frequently when very close
            pickupAttemptTimer -= Time.deltaTime;
            if (pickupAttemptTimer <= 0f && distanceToPlayer <= pickupDistance * 1.5f)
            {
                pickupAttemptTimer = 0.1f;  // Try pickup every 0.1 seconds when close
                TryPickup(distanceToPlayer);
            }
            
            // Only allow attraction to player after the pickup delay has passed
            // unless attractDuringDelay is set to true
            if (distanceToPlayer <= attractionRadius)
            {
                isAttracting = true;
                
                // Calculate attraction speed based on distance
                float attractionPercent = 1f - (distanceToPlayer / attractionRadius);
                float currentSpeed = Mathf.Lerp(minAttractionSpeed, maxAttractionSpeed, attractionPercent);
                
                // Move toward player
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                Vector3 movement = directionToPlayer * currentSpeed * Time.deltaTime;
                transform.position += movement;
                
                // Update bobbing origin to follow the item
                bobOrigin = new Vector3(transform.position.x, transform.position.y, bobOrigin.z);
            }
            else
            {
                isAttracting = false;
            }
        }
        else if (attractDuringDelay && distanceToPlayer <= attractionRadius)
        {
            // Optional: Allow attraction during delay if the flag is set
            isAttracting = true;
            
            // Use slower attraction during delay
            float attractionPercent = 0.5f * (1f - (distanceToPlayer / attractionRadius));
            float currentSpeed = Mathf.Lerp(minAttractionSpeed * 0.5f, maxAttractionSpeed * 0.5f, attractionPercent);
            
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            Vector3 movement = directionToPlayer * currentSpeed * Time.deltaTime;
            transform.position += movement;
            
            bobOrigin = new Vector3(transform.position.x, transform.position.y, bobOrigin.z);
        }
        else
        {
            isAttracting = false;
        }
        
        // Apply bobbing and rotation
        if (canAnimate)
        {
            // Apply bobbing motion
            float bobOffset = Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobHeight;
            transform.position = new Vector3(
                bobOrigin.x, 
                bobOrigin.y, 
                bobOrigin.z + bobOffset
            );
            
            // Apply rotation
            if (!isAttracting)
            {
                transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
            }
        }
        
        // Optional: Visual cue that item is not ready for pickup
        if (!CanPickup() && spriteRenderer != null)
        {
            // Make item slightly transparent during delay
            Color currentColor = spriteRenderer.color;
            spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.7f);
        }
        else if (spriteRenderer != null)
        {
            // Make fully opaque when ready
            Color currentColor = spriteRenderer.color;
            spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1.0f);
        }
    }
    
    // Check if enough time has passed to allow pickup
    private bool CanPickup()
    {
        return (Time.time - spawnTime) >= pickupDelay;
    }
    
    private void TryPickup(float distance)
    {
        // Don't try pickup if we're in delay period
        if (!CanPickup()) return;
        
        if (distance <= pickupDistance)
        {
            // Make sure we have the inventory handler
            if (inventoryHandler == null)
                FindInventoryHandler();
            
            if (inventoryHandler != null && item != null)
            {
                bool added = inventoryHandler.AddItem(item, 1);
                if (debugMode) Debug.Log($"Proximity pickup attempt: {added}, distance={distance}");
                
                if (added)
                {
                    // Play pickup sound if player has an audio source
                    if (playerTransform != null)
                    {
                        AudioSource playerAudio = playerTransform.GetComponent<AudioSource>();
                        if (playerAudio != null && playerAudio.clip != null)
                        {
                            playerAudio.pitch = Random.Range(minPitch, maxPitch);
                            playerAudio.PlayOneShot(playerAudio.clip);
                        }
                    }
                    
                    Debug.Log($"Picked up {item.itemName} in Update");
                    Destroy(gameObject);
                }
            }
        }
    }
}