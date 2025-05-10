using System.Collections.Generic;
using UnityEngine;

public class InventoryHandler : MonoBehaviour
{
    public static InventoryHandler instance;

    [SerializeField] private List<InventorySlot> inventorySlots;
    [SerializeField] private int maxInventorySize = 20;

    [Header("Selection Settings")]
    [SerializeField] private int selectedSlotIndex = -1; // -1 means no selection
    [SerializeField] private Color selectionOutlineColor = new Color(1f, 0.8f, 0f, 1f); // Gold outline
    [SerializeField] private float selectionScale = 1.1f;
    [SerializeField] private bool enableNumberKeySelection = true;
    [SerializeField] private bool enableScrollSelection = true;

    [Header("Drop Item Settings")]
    [SerializeField] private GameObject groundItemPrefab; // Prefab for dropped items
    [SerializeField] private float dropDistance = 1.0f;   // How far in front of player to drop
    [SerializeField] private AudioClip dropSound;         // Sound when dropping items
    [SerializeField] private float dropCooldown = 0.2f;   // Minimum time between drops

    private float lastDropTime = 0f;

    // Reference to currently selected slot
    private InventorySlot selectedSlot = null;

    // Event that other systems can subscribe to when selection changes
    public delegate void SelectionChangedHandler(InventoryItem item, int amount);
    public event SelectionChangedHandler OnSelectionChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize selection
        UpdateSelectedSlot(-1);

        // If we don't have a ground item prefab, try to load one
        if (groundItemPrefab == null)
        {
            groundItemPrefab = Resources.Load<GameObject>("Prefabs/GroundItem");
            if (groundItemPrefab == null)
                Debug.LogWarning("No GroundItem prefab found. Dropping items will not work.");
        }
    }

    private void Update()
    {
        // Handle scroll wheel selection
        if (enableScrollSelection)
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                // Scroll up (positive delta) = previous slot
                // Scroll down (negative delta) = next slot
                int direction = scrollDelta > 0 ? -1 : 1;
                CycleSelection(direction);
            }

            // Handle item dropping - make sure a cooldown has passed
            if (Time.time >= lastDropTime + dropCooldown)
            {
                // Drop one item with Q
                if (Input.GetKeyDown(KeyCode.Q) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                {
                    DropSelectedItem(1);
                }
                // Drop entire stack with Ctrl+Q
                else if (Input.GetKeyDown(KeyCode.Q) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    DropSelectedItem(); // No amount parameter = drop all
                }
            }
        }

        // Handle number key selection (1-9 for first 9 slots)
        if (enableNumberKeySelection)
        {
            for (int i = 1; i <= 9 && i <= inventorySlots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    UpdateSelectedSlot(i - 1); // Convert to 0-based index
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Drops the currently selected item(s)
    /// </summary>
    /// <param name="amount">Number of items to drop, or all if not specified</param>
    public void DropSelectedItem(int amount = 0)
    {
        // Make sure we have a selected slot with an item
        if (selectedSlot == null || selectedSlot.item == null || selectedSlot.amount <= 0)
            return;

        InventoryItem itemToDrop = selectedSlot.item;
        int amountToDrop = (amount <= 0) ? selectedSlot.amount : Mathf.Min(amount, selectedSlot.amount);

        // Remove from inventory
        if (selectedSlot.amount <= amountToDrop)
        {
            // Dropping the whole stack
            selectedSlot.SetItem(null, 0);
        }
        else
        {
            // Dropping part of the stack
            selectedSlot.AddItem(-amountToDrop);
        }

        // Spawn ground item
        SpawnGroundItem(itemToDrop, amountToDrop);

        // Record the time of drop for cooldown
        lastDropTime = Time.time;
    }

    /// <summary>
    /// Spawns a ground item in front of the player
    /// </summary>
    private void SpawnGroundItem(InventoryItem item, int amount)
    {
        if (groundItemPrefab == null || item == null || amount <= 0)
            return;

        // Find player position
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No player found to drop item in front of");
            return;
        }

        // Calculate drop position in front of player
        Vector3 dropPosition = player.transform.position;

        // If player has a direction facing, use that
        var playerMovement = player.GetComponent<PlayerController>();

        dropPosition += new Vector3(
            Random.Range(-0.5f, 0.5f) * dropDistance,
            Random.Range(-0.5f, 0.5f) * dropDistance,
            0
        );


        // Spawn the item
        GameObject groundItemObj = Instantiate(groundItemPrefab, dropPosition, Quaternion.identity);
        GroundItem groundItem = groundItemObj.GetComponent<GroundItem>();

        if (groundItem != null)
        {
            groundItem.Setup(item);

            // If we have multiple items, create multiple ground items for better visual feedback
            if (amount > 1)
            {
                // Create individual items for satisfying visuals
                int individualDrops = Mathf.Min(amount, 5); // Cap at 5 individual drops for performance

                for (int i = 1; i < individualDrops; i++)
                {
                    // Random position near the original drop point
                    Vector3 offsetPos = dropPosition + new Vector3(
                        Random.Range(-0.3f, 0.3f),
                        Random.Range(-0.3f, 0.3f),
                        0
                    );

                    GameObject additionalItemObj = Instantiate(groundItemPrefab, offsetPos, Quaternion.identity);
                    GroundItem additionalItem = additionalItemObj.GetComponent<GroundItem>();
                    if (additionalItem != null)
                    {
                        additionalItem.Setup(item);
                    }
                }
            }
        }

        // Play drop sound if available
        if (dropSound != null)
        {
            AudioSource playerAudio = player.GetComponent<AudioSource>();
            if (playerAudio != null)
            {
                playerAudio.pitch = Random.Range(0.9f, 1.1f);
                playerAudio.PlayOneShot(dropSound);
            }
            else
            {
                // Fallback to playing at point if player has no audio source
                AudioSource.PlayClipAtPoint(dropSound, dropPosition);
            }
        }
    }

    private void CycleSelection(int direction)
    {
        if (inventorySlots.Count == 0)
            return;

        int newIndex;

        // If nothing is selected, select the first/last slot based on direction
        if (selectedSlotIndex == -1)
        {
            newIndex = direction > 0 ? 0 : inventorySlots.Count - 1;
        }
        else
        {
            // Calculate new index with wraparound
            newIndex = (selectedSlotIndex + direction) % inventorySlots.Count;
            if (newIndex < 0) newIndex += inventorySlots.Count; // Handle negative wraparound
        }

        UpdateSelectedSlot(newIndex);
    }

    private void UpdateSelectedSlot(int newIndex)
    {
        // Only proceed if there's an actual change
        if (newIndex == selectedSlotIndex)
            return;

        // Clear previous selection
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Update the selected index
        selectedSlotIndex = newIndex;

        // Set new selection
        if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
        {
            selectedSlot = inventorySlots[selectedSlotIndex];
            selectedSlot.SetSelected(true);

            // Trigger event for systems that need to know about selection change
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged(selectedSlot.item, selectedSlot.amount);
            }
        }
        else
        {
            selectedSlot = null;

            // Trigger event with null item
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged(null, 0);
            }
        }
    }

    public int GetItemCount(InventoryItem item)
    {
        int count = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == item)
            {
                count += slot.amount;
            }
        }
        return count;
    }

    public bool canAcceptItem(InventoryItem item, int amount)
    {
        if (item == null) return false;

        // First try to stack with existing items
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == item && slot.amount < item.maxStackSize)
            {
                amount -= Mathf.Min(amount, item.maxStackSize - slot.amount);

                if (amount <= 0)
                    return true;
            }
        }

        // If we still have items to add, find empty slots
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == null)
            {
                amount -= Mathf.Min(amount, item.maxStackSize);

                if (amount <= 0)
                    return true;
            }
        }

        // If we reach here, there wasn't enough inventory space
        return amount < 1;
    }

    public InventoryItem GetSelectedItem()
    {
        return selectedSlot != null ? selectedSlot.item : null;
    }

    public int GetSelectedAmount()
    {
        return selectedSlot != null ? selectedSlot.amount : 0;
    }

    public bool AddItem(InventoryItem item, int amount)
    {
        if (item == null) return false;

        // First try to stack with existing items
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == item && slot.amount < item.maxStackSize)
            {
                int amountToAdd = Mathf.Min(amount, item.maxStackSize - slot.amount);
                slot.AddItem(amountToAdd);
                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

        // If we still have items to add, find empty slots
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == null)
            {
                int amountToAdd = Mathf.Min(amount, item.maxStackSize);
                slot.SetItem(item, amountToAdd);
                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

        // If we reach here, there wasn't enough inventory space
        Debug.Log("Inventory full, couldn't add all items");
        return amount < 1; // Return true if we managed to add at least something
    }

    public void RemoveItem(InventoryItem item, int amount)
    {
        if (item == null) return;

        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = inventorySlots[i];
            if (slot.item == item)
            {
                if (slot.amount <= amount)
                {
                    amount -= slot.amount;
                    slot.SetItem(null, 0);
                }
                else
                {
                    slot.AddItem(-amount);
                    return;
                }

                if (amount <= 0)
                    return;
            }
        }
    }
}