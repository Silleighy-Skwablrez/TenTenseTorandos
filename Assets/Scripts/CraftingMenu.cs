using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenu : MonoBehaviour
{
    [Header("Recipe Settings")]
    [SerializeField] private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
    
    [Header("UI References")]
    [SerializeField] private GameObject craftingMenuPanel;
    [SerializeField] private Transform recipeListContent;
    [SerializeField] private GameObject recipeButtonPrefab;
    
    [Header("Recipe Details")]
    [SerializeField] private GameObject recipeDetailsPanel;
    [SerializeField] private Text recipeNameText;
    [SerializeField] private Text recipeDescriptionText;
    [SerializeField] private Image resultItemImage;
    [SerializeField] private Text resultItemNameText;
    [SerializeField] private Text resultAmountText;
    
    [Header("Ingredients Panel")]
    [SerializeField] private Transform ingredientListContent;
    [SerializeField] private GameObject ingredientEntryPrefab;
    
    [Header("Crafting Controls")]
    [SerializeField] private Button craftButton;
    [SerializeField] private Text craftButtonText;
    [SerializeField] private KeyCode menuToggleKey = KeyCode.C;
    
    // Runtime variables
    private CraftingRecipe selectedRecipe;
    private List<GameObject> recipeButtons = new List<GameObject>();
    private List<GameObject> ingredientEntries = new List<GameObject>();
    
    private void Start()
    {
        // Initial setup
        SetupRecipeList();
        HideCraftingMenu();
    }
    
    private void Update()
    {
        // Toggle menu with key press
        if (Input.GetKeyDown(menuToggleKey))
        {
            ToggleCraftingMenu();
        }
        
        // Update craftability status if menu is open
        if (craftingMenuPanel.activeSelf)
        {
            UpdateRecipeStatus();
        }
    }
    
    public void ToggleCraftingMenu()
    {
        if (craftingMenuPanel.activeSelf)
        {
            HideCraftingMenu();
        }
        else
        {
            ShowCraftingMenu();
        }
    }
    
    public void ShowCraftingMenu()
    {
        craftingMenuPanel.SetActive(true);
        UpdateRecipeStatus();
    }
    
    public void HideCraftingMenu()
    {
        craftingMenuPanel.SetActive(false);
    }
    
    private void SetupRecipeList()
    {
        // Clear existing buttons
        foreach (GameObject button in recipeButtons)
        {
            Destroy(button);
        }
        recipeButtons.Clear();
        
        // Create a button for each recipe
        foreach (CraftingRecipe recipe in availableRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipeListContent);
            Button button = buttonObj.GetComponent<Button>();
            
            // Setup button visuals
            Image buttonImage = buttonObj.transform.Find("RecipeIcon")?.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = recipe.recipeIcon != null ? recipe.recipeIcon : recipe.resultItem.icon;
            }
            
            Text buttonText = buttonObj.transform.Find("RecipeName")?.GetComponent<Text>();
            if (buttonText != null)
            {
                buttonText.text = recipe.recipeName;
            }
            
            // Setup button action
            button.onClick.AddListener(() => SelectRecipe(recipe));
            
            recipeButtons.Add(buttonObj);
        }
        
        // Select first recipe by default
        if (availableRecipes.Count > 0)
        {
            SelectRecipe(availableRecipes[0]);
        }
    }

    private void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        UpdateRecipeDetails();
    }
    
    private void UpdateRecipeDetails()
    {
        if (selectedRecipe == null)
        {
            recipeDetailsPanel.SetActive(false);
            return;
        }
        
        recipeDetailsPanel.SetActive(true);
        
        // Update recipe info
        recipeNameText.text = selectedRecipe.recipeName;
        recipeDescriptionText.text = selectedRecipe.recipeDescription;
        
        // Update result item display
        resultItemImage.sprite = selectedRecipe.resultItem.icon;
        resultItemNameText.text = selectedRecipe.resultItem.itemName;
        resultAmountText.text = "x" + selectedRecipe.resultAmount.ToString();
        
        // Update ingredients list
        UpdateIngredientsList();
        
        // Update craft button
        UpdateCraftButton();
    }
    
    private void UpdateIngredientsList()
    {
        // Clear existing entries
        foreach (GameObject entry in ingredientEntries)
        {
            Destroy(entry);
        }
        ingredientEntries.Clear();
        
        // Create entries for each ingredient
        foreach (RecipeIngredient ingredient in selectedRecipe.ingredients)
        {
            GameObject entryObj = Instantiate(ingredientEntryPrefab, ingredientListContent);
            
            Image itemIcon = entryObj.transform.Find("ItemIcon")?.GetComponent<Image>();
            if (itemIcon != null)
            {
                itemIcon.sprite = ingredient.item.icon;
            }
            
            Text itemName = entryObj.transform.Find("ItemName")?.GetComponent<Text>();
            if (itemName != null)
            {
                itemName.text = ingredient.item.itemName;
            }
            
            // Get current amount in inventory
            int currentAmount = InventoryHandler.instance.GetItemCount(ingredient.item);
            
            Text amountText = entryObj.transform.Find("AmountText")?.GetComponent<Text>();
            if (amountText != null)
            {
                amountText.text = currentAmount + "/" + ingredient.amount;
                
                // Color red if not enough
                if (currentAmount < ingredient.amount)
                {
                    amountText.color = Color.red;
                }
                else
                {
                    amountText.color = Color.white;
                }
            }
            
            ingredientEntries.Add(entryObj);
        }
    }
    
    private void UpdateCraftButton()
    {
        bool canCraft = selectedRecipe.CanCraft();
        craftButton.interactable = canCraft;
        craftButtonText.text = canCraft ? "Craft" : "Missing Materials";
    }
    
    private void UpdateRecipeStatus()
    {
        // Update selected recipe details (ingredient counts may have changed)
        if (selectedRecipe != null)
        {
            UpdateIngredientsList();
            UpdateCraftButton();
        }
        
        // Update all recipe buttons to show available/unavailable status
        for (int i = 0; i < availableRecipes.Count && i < recipeButtons.Count; i++)
        {
            bool canCraft = availableRecipes[i].CanCraft();
            
            // Visual indication of craftability
            Image buttonBg = recipeButtons[i].GetComponent<Image>();
            if (buttonBg != null)
            {
                buttonBg.color = canCraft ? Color.green : new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
    
    public void CraftSelectedRecipe()
    {
        if (selectedRecipe == null) return;
        
        if (selectedRecipe.Craft())
        {
            // Update UI after successful crafting
            UpdateRecipeStatus();
            
            // Play sounds or effects here
        }
    }
}