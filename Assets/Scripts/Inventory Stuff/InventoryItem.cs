using UnityEngine;
using skner.DualGrid;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    [Header("Basic Properties")]
    public string itemName;
    public Sprite itemSprite;
    [TextArea(3, 10)]
    public string description;
    
    [Header("Inventory Settings")]
    public int maxStackSize = 99;
    public bool isConsumable = false;
    
    [Header("Economy")]
    public int value = 1;
    
    [Header("Item Category")]
    public ItemCategory category = ItemCategory.Misc;
    
    [Header("Placement Options")]
    public bool isPlaceable = false;
    public DualGridRuleTile placeTile;  // The tile to place when using this item
    [Tooltip("Resources required for placement (empty = none)")]
    public PlacementResource[] placementResources;
    
    // Optional additional properties
    public GameObject worldPrefab;  // The 3D object to spawn in the world
    public AudioClip useSound;      // Sound to play when used
    
    // Property to provide compatibility with code that expects "icon"
    public Sprite icon { get { return itemSprite; } }
    
    // If you need custom item behaviors
    public virtual void UseItem()
    {
        // Default implementation does nothing
        Debug.Log($"Used item: {itemName}");
    }
    
    // Optional: Additional methods based on item type
    public virtual bool CanUse()
    {
        return isConsumable;
    }
    
    // Check if all required resources are available for placement
    public bool HasRequiredResources(InventoryHandler inventory)
    {
        if (inventory == null || placementResources == null || placementResources.Length == 0)
            return true;
            
        foreach (var resource in placementResources)
        {
            if (resource.item == null)
                continue;
                
            int availableCount = inventory.GetItemCount(resource.item);
            if (availableCount < resource.amount)
                return false;
        }
        
        return true;
    }
    
    // Consume required resources for placement
    public void ConsumeResources(InventoryHandler inventory)
    {
        if (inventory == null || placementResources == null)
            return;
            
        foreach (var resource in placementResources)
        {
            if (resource.item != null && resource.amount > 0)
                inventory.RemoveItem(resource.item, resource.amount);
        }
    }
}

// Enum to categorize items
public enum ItemCategory
{
    Weapon,
    Tool,
    Material,
    Consumable,
    Quest,
    Misc
}

// Class to define resources needed for placement
[System.Serializable]
public class PlacementResource
{
    public InventoryItem item;
    public int amount = 1;
}