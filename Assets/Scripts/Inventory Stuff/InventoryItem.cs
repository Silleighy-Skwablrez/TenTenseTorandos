using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Inventory Item")]
public class InventoryItem : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;  // Changed from 'icon' to 'itemSprite' to match InventorySlot
    [TextArea(3, 10)]
    public string description;
    
    // Add additional properties as needed
    public int maxStackSize = 99;
    public bool isConsumable = false;
    public int value = 1;
    
    // If you need custom item behaviors
    public virtual void UseItem()
    {
        // Default implementation does nothing
        Debug.Log($"Used item: {itemName}");
    }
}