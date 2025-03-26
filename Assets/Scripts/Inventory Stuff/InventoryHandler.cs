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

            // drop items on q press
            
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