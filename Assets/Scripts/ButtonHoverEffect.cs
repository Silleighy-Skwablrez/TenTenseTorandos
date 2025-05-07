using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("How much the button should grow when hovered (1.0 = normal size)")]
    public float hoverScale = 1.1f;
    
    [Tooltip("How fast the button grows/shrinks")]
    public float transitionSpeed = 8f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Button button;
    
    void Start()
    {
        // Store the original scale
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // Get button component (if exists)
        button = GetComponent<Button>();
    }
    
    void Update()
    {
        // Smoothly animate to target scale
        transform.localScale = Vector3.Lerp(
            transform.localScale, 
            targetScale, 
            Time.deltaTime * transitionSpeed
        );
    }
    
    // Called when pointer enters button area
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only grow if button is interactable
        if (button == null || button.interactable)
        {
            targetScale = originalScale * hoverScale;
        }
    }
    
    // Called when pointer exits button area
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}