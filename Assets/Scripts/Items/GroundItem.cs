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
    public float pickupDistance = 0.5f;      // Distance at which item is collected
    
    // Sound parameters
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    
    private Vector3 bobOrigin;  // Origin point for bobbing animation
    private float timeOffset;
    private Transform playerTransform;
    private bool isAttracting = false;
    private bool canAnimate = true;
    private InventoryHandler inventoryHandler;
    
    // Initial "pop" effect
    public float popForce = 1.5f;
    public float popDuration = 0.3f;
    private Vector2 popDirection;
    private float popTimer = 0f;
    
    // Cache these values to avoid recalculation
    private float bobTime;
    private float lastUpdateTime;
    
    void Awake()
    {
        // Try to find a sprite renderer if not already assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        // Random time offset for bob animation
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Find inventory handler
        inventoryHandler = FindObjectOfType<InventoryHandler>();
        
        // Initialize pop effect
        popDirection = Random.insideUnitCircle.normalized;
        popTimer = popDuration;
    }
    
    void Start()
    {
        // Store starting position
        bobOrigin = transform.position;
        UpdateSprite();
        
        // Add a collider if missing
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
        }
    }
    
    void Update()
    {
        // Skip update if not visible
        if (!spriteRenderer.isVisible) return;
        
        // Handle initial "pop" animation
        if (popTimer > 0)
        {
            float popProgress = 1 - (popTimer / popDuration); // 0 to 1
            float currentForce = Mathf.Lerp(popForce, 0, popProgress); // Gradually reduce force
            
            // Move with reduced force
            transform.position += (Vector3)(popDirection * currentForce * Time.deltaTime);
            
            // Reduce timer
            popTimer -= Time.deltaTime;
            
            // When pop is complete, store the new position as bobbing origin
            if (popTimer <= 0)
            {
                bobOrigin = transform.position;
            }
            
            return; // Skip other animations during pop
        }
        
        // Variable to store the current position excluding bobbing
        Vector3 currentBasePosition = isAttracting ? transform.position : bobOrigin;
        
        // Check if player is within attraction radius and inventory isn't full
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Handle attraction to player - only if inventory has space
            if (distanceToPlayer <= attractionRadius && !IsInventoryFull())
            {
                if (!isAttracting)
                {
                    // Transitioning from bobbing to attraction - save the real position
                    bobOrigin = new Vector3(
                        transform.position.x,
                        bobOrigin.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight,
                        transform.position.z
                    );
                }
                
                isAttracting = true;
                canAnimate = false; // Stop bobbing when attracting
                
                // Calculate speed based on distance (closer = faster)
                float speedFactor = 1f - (distanceToPlayer / attractionRadius); // 0 to 1
                float currentSpeed = Mathf.Lerp(minAttractionSpeed, maxAttractionSpeed, speedFactor);
                
                // Move toward player
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                
                // Auto-pickup when very close
                if (distanceToPlayer <= pickupDistance)
                {
                    PickupItem();
                }
            }
            else
            {
                // If we were attracting but player moved out of range or inventory full
                if (isAttracting)
                {
                    // Update bobbing origin to current position
                    bobOrigin = transform.position;
                }
                
                isAttracting = false;
                canAnimate = true;
            }
        }
        
        // Simple bobbing and rotating animation when not being attracted
        if (canAnimate)
        {
            // Bobbing up and down
            float yOffset = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(bobOrigin.x, bobOrigin.y + yOffset, bobOrigin.z);
        }
        
        // Always rotate, regardless of attraction state
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    
    public void Setup(InventoryItem newItem)
    {
        item = newItem;
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer != null && item != null && item.itemSprite != null)
        {
            spriteRenderer.sprite = item.itemSprite;
            // Ensure sprite is visible
            spriteRenderer.sortingOrder = 10;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PickupItem();
        }
    }
    
    private bool IsInventoryFull()
    {
        if (inventoryHandler == null)
        {
            inventoryHandler = FindObjectOfType<InventoryHandler>();
        }
        
        if (inventoryHandler != null)
        {
            return !inventoryHandler.canAcceptItem(item, 1);
        }
        
        return false; // If we can't find inventory handler, assume not full
    }
    
    private void PickupItem()
    {
        if (item == null) return;
        
        // Find inventory handler and add item
        if (inventoryHandler == null)
        {
            inventoryHandler = FindObjectOfType<InventoryHandler>();
        }
        
        if (inventoryHandler != null)
        {
            bool added = inventoryHandler.AddItem(item, 1);
            
            if (added)
            {
                // Play pickup sound with randomized pitch
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    // Create a temporary audio source to play with random pitch
                    AudioSource.PlayClipAtPoint(audioSource.clip, transform.position);
                    
                    // Find the temporary audio source that was just created
                    AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
                    foreach (AudioSource source in allAudioSources)
                    {
                        if (source.clip == audioSource.clip && source != audioSource)
                        {
                            // Randomize the pitch
                            source.pitch = Random.Range(minPitch, maxPitch);
                            break;
                        }
                    }
                }
                
                Destroy(gameObject);
            }
        }
    }
    
    // Visualize attraction radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }
}