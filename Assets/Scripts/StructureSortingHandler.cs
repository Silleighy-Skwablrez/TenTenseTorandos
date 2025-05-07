using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class StructureSortingHandler : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private SpriteRenderer playerRenderer;
    
    // Offset to ensure proper layering
    public int sortingOrderOffset = 1;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRenderer = player.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;
        
        // Check if player is south of (in front of) the structure
        if (playerTransform.position.y < transform.position.y)
        {
            // Structure should render behind player
            spriteRenderer.sortingOrder = playerRenderer.sortingOrder - sortingOrderOffset;
        }
        else
        {
            // Structure should render in front of player
            spriteRenderer.sortingOrder = playerRenderer.sortingOrder + sortingOrderOffset;
        }
    }
}