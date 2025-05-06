using UnityEngine;

public class UtilitiesUIOpen : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject uiPanel; // The UI panel to activate
    public KeyCode activationKey = KeyCode.E; // The key to open the UI

    [Header("Player Detection Settings")]
    public Transform player; // Reference to the player's transform
    public float activationDistance = 5f; // Distance within which the UI can be activated

    private bool isPlayerInRange = false;

    private void Start()
    {
        // Ensure the UI panel is initially hidden
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Check the distance between the player and this object
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool shouldShowUI = distanceToPlayer <= activationDistance;

            // Show or hide the UI based on the player's distance
            if (shouldShowUI && !isPlayerInRange)
            {
                ShowUI();
            }
            else if (!shouldShowUI && isPlayerInRange)
            {
                HideUI();
            }
        }
    }

    private void ShowUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
            isPlayerInRange = true;
        }
    }

    private void HideUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
            isPlayerInRange = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a sphere in the editor to visualize the activation distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}