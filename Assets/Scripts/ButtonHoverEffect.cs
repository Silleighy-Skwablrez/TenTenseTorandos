using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Effects")]
    [Tooltip("How much the button should grow when hovered (1.0 = normal size)")]
    public float hoverScale = 1.1f;
    
    [Tooltip("How much the button should squish vertically when pressed (1.0 = normal height)")]
    public float pressedYScale = 0.9f;
    
    [Tooltip("How fast the button grows/shrinks")]
    public float transitionSpeed = 8f;
    
    [Header("Sound Effects")]
    [Tooltip("Sound played when hovering over the button")]
    public AudioClip hoverSound;
    
    [Tooltip("Sound played when button is pressed down")]
    public AudioClip clickDownSound;
    
    [Tooltip("Sound played when button is released")]
    public AudioClip clickUpSound;
    
    [Range(0f, 1f)]
    [Tooltip("Volume for button sounds")]
    public float soundVolume = 1f;
    
    [Range(0f, 0.5f)]
    [Tooltip("Random pitch variation range (+/-)")]
    public float randomPitchRange = 0.1f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Button button;
    private AudioSource audioSource;
    private bool isHovering = false;
    
    void Start()
    {
        // Store the original scale
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // Get button component (if exists)
        button = GetComponent<Button>();
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
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
            isHovering = true;
            targetScale = originalScale * hoverScale;
            
            // Play hover sound with random pitch
            if (hoverSound != null && audioSource != null)
            {
                PlaySoundWithRandomPitch(hoverSound);
            }
        }
    }
    
    // Called when pointer exits button area
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetScale = originalScale;
    }
    
    // Called when button is pressed
    public void OnPointerDown(PointerEventData eventData)
    {
        // Only apply effects if button is interactable
        if (button == null || button.interactable)
        {
            // Squish on Y axis while maintaining hover scale on X and Z
            targetScale = new Vector3(
                originalScale.x * hoverScale,
                originalScale.y * pressedYScale,
                originalScale.z * hoverScale
            );
            
            // Play sound with random pitch
            if (clickDownSound != null)
            {
                PlaySoundWithRandomPitch(clickDownSound);
            }
        }
    }
    
    // Called when button is released
    public void OnPointerUp(PointerEventData eventData)
    {
        // Only apply effects if button is interactable
        if (button == null || button.interactable)
        {
            // Return to hover scale if still hovering, otherwise to original scale
            targetScale = isHovering ? originalScale * hoverScale : originalScale;
            
            // Play sound with random pitch
            if (clickUpSound != null)
            {
                PlaySoundWithRandomPitch(clickUpSound);
            }
        }
    }
    
    private void PlaySoundWithRandomPitch(AudioClip clip)
    {
        // Apply random pitch variation
        float randomPitch = 1f + Random.Range(-randomPitchRange, randomPitchRange);
        audioSource.pitch = randomPitch;
        audioSource.PlayOneShot(clip, soundVolume);
    }
}