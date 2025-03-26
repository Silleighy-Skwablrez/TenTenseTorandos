using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public InventoryItem item;
    public int amount;
    public Text countLabel;
    public GameObject contentImageObject;
    private Image contentImage;
    
    [Header("Selection Visuals")]
    public Outline selectionOutline; // Assign Unity's built-in Outline component
    private bool isSelected = false;
    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    
    // How fast to animate scale changes
    [SerializeField] private float scaleSpeed = 10f;
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.8f, 0f, 1f); // Gold outline for selected
    [SerializeField] private Color normalOutlineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Subtle outline for unselected
    [SerializeField] private float selectedOutlineWidth = 3f;
    [SerializeField] private float normalOutlineWidth = 1f;
    
    void Start()
    {
        countLabel = transform.GetChild(0).GetComponent<Text>();
        countLabel.gameObject.SetActive(false); // just in case its left enabled in the editor
        contentImageObject = transform.GetChild(1).gameObject;
        contentImage = contentImageObject.GetComponent<Image>();
        contentImage.sprite = null;
        contentImageObject.SetActive(false); // just in case its left enabled in the editor
        
        // Find or create selection outline if not assigned
        if (selectionOutline == null)
        {
            // Try to find existing outline component on this GameObject or its children
            selectionOutline = GetComponent<Outline>();
            if (selectionOutline == null)
                selectionOutline = GetComponentInChildren<Outline>();
            
            // If still null, add one to the current GameObject
            if (selectionOutline == null)
            {
                selectionOutline = gameObject.AddComponent<Outline>();
            }
        }
        
        // Initialize outline state to normal (unselected)
        if (selectionOutline != null)
        {
            selectionOutline.effectColor = normalOutlineColor;
            selectionOutline.effectDistance = new Vector2(normalOutlineWidth, normalOutlineWidth);
        }
    }

    public void SetItem(InventoryItem item, int amount)
    {
        if (item == null)
        {
            this.item = null;
            this.amount = 0;
            contentImage.sprite = null;
            contentImageObject.SetActive(false);
            countLabel.gameObject.SetActive(false);
            return;
        }

        this.item = item;
        this.amount = amount;
        contentImage.sprite = item.itemSprite;
        contentImageObject.SetActive(true);
        
        if (amount <= 1)
        {
            countLabel.gameObject.SetActive(false);
        }
        else
        {
            countLabel.text = amount.ToString();
            countLabel.gameObject.SetActive(true);
        }
    }

    public void AddItem(int amount)
    {
        if (item == null)
        {
            Debug.LogWarning("Trying to add to a null item in inventory slot.");
            return;
        }
        
        this.amount += amount;
        if (this.amount <= 1)
        {
            countLabel.gameObject.SetActive(false);
        }
        else
        {
            countLabel.text = this.amount.ToString();
            countLabel.gameObject.SetActive(true);
        }
    }
    
    // Method to handle selection state
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Update outline appearance
        if (selectionOutline != null)
        {
            // Change color and width based on selection state
            if (selected)
            {
                selectionOutline.effectColor = selectedOutlineColor;
                selectionOutline.effectDistance = new Vector2(selectedOutlineWidth, selectedOutlineWidth);
            }
            else
            {
                selectionOutline.effectColor = normalOutlineColor;
                selectionOutline.effectDistance = new Vector2(normalOutlineWidth, normalOutlineWidth);
            }
        }
    }

    void Update()
    {
        // Target scale based on selection state
        Vector3 targetScale = isSelected ? selectedScale : normalScale;
        
        // Lerp current scale to target
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }
}