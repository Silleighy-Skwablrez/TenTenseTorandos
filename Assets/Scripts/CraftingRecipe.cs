using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecipeIngredient
{
    public InventoryItem item;
    public int amount = 1;
}

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Sprite recipeIcon; // Optional icon to display in the crafting menu
    [TextArea(2, 4)]
    public string recipeDescription;
    
    [Header("Ingredients")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
    
    [Header("Result")]
    public InventoryItem resultItem;
    public int resultAmount = 1;

    // Check if player has all required ingredients
    public bool CanCraft()
    {
        if (InventoryHandler.instance == null) return false;
        
        foreach (RecipeIngredient ingredient in ingredients)
        {
            if (InventoryHandler.instance.GetItemCount(ingredient.item) < ingredient.amount)
            {
                return false;
            }
        }
        
        // Also check if inventory has room for the result
        return InventoryHandler.instance.canAcceptItem(resultItem, resultAmount);
    }

    // Perform crafting - consume ingredients and add result
    public bool Craft()
    {
        if (!CanCraft()) return false;
        
        // Remove ingredients
        foreach (RecipeIngredient ingredient in ingredients)
        {
            InventoryHandler.instance.RemoveItem(ingredient.item, ingredient.amount);
        }
        
        // Add result
        InventoryHandler.instance.AddItem(resultItem, resultAmount);
        
        return true;
    }
}