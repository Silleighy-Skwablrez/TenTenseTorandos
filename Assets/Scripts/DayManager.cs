using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DayManager : MonoBehaviour
{
    [Header("Day Settings")]
    public int maxDays = 10;
    public Text dayCounterText;
    public Text timerText;
    public float dayDuration = 180f;
    public float afterTypingDelay = 1f;
    public float fadeDuration = 1f;
    
    [Header("Storm Settings")]
    public StormGenerator stormGenerator;
    public float gameOverDestructionThreshold = 50f;
    
    [Header("UI Elements")]
    public CanvasGroup gameplayUI;
    public CanvasGroup stormAftermathUI;
    public TypeWrite stormSummaryTypeWrite;
    public TypeWrite destructionPercentageTypeWrite;
    public Text continuePromptText;
    public Image blackPanel;
    
    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip windSound;
    public AudioClip rainSound;
    public AudioClip thunderSound;
    public AudioClip electricalSound;
    
    [Header("Scene Transition")]
    public string endGameSceneName = "EndGame";
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // States
    private enum GameState { Day, Night, StormPrep, StormDamage, Aftermath, Transition }
    private GameState currentState;
    
    // Private variables
    private int currentDay = 1;
    private float timeRemaining;
    private bool isGameOver = false;
    private List<AudioSource> stormAudioSources = new List<AudioSource>();
    
    // Tracking variables
    private StormDamageStatistics currentStormStats;
    private StormData currentStorm;
    
    void Start()
    {
        InitializeComponents();
        CreateStormAudioSources();
        StartDay();
    }
    
    void InitializeComponents()
    {
        // Check for storm generator
        if (stormGenerator == null)
            stormGenerator = FindObjectOfType<StormGenerator>();
        
        // Setup black panel
        if (blackPanel != null)
        {
            blackPanel.color = new Color(0, 0, 0, 1); // Start fully black
            blackPanel.gameObject.SetActive(true);
            
            Canvas panelCanvas = blackPanel.GetComponentInParent<Canvas>();
            if (panelCanvas != null)
                panelCanvas.sortingOrder = 9999;
        }
        
        // Initialize UI states
        if (gameplayUI != null)
        {
            gameplayUI.alpha = 0;
            gameplayUI.interactable = false;
            gameplayUI.blocksRaycasts = false;
        }
        
        if (stormAftermathUI != null)
        {
            stormAftermathUI.alpha = 0;
            stormAftermathUI.interactable = false;
            stormAftermathUI.blocksRaycasts = false;
            stormAftermathUI.gameObject.SetActive(false);
        }
    }
    
    void CreateStormAudioSources()
    {
        // Clear existing sources
        foreach (AudioSource source in stormAudioSources)
        {
            if (source != null)
                Destroy(source.gameObject);
        }
        stormAudioSources.Clear();
        
        // Create sources with zero volume
        CreateAudioSource("WindAudio", windSound);
        CreateAudioSource("RainAudio", rainSound);
        CreateAudioSource("ThunderAudio", thunderSound);
        CreateAudioSource("ElectricalAudio", electricalSound);
    }
    
    AudioSource CreateAudioSource(string name, AudioClip clip)
    {
        if (clip == null)
            return null;
            
        GameObject audioObj = new GameObject(name);
        audioObj.transform.parent = transform;
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0;
        stormAudioSources.Add(source);
        return source;
    }
    
    void Update()
    {
        // Day state - check for timer
        if (currentState == GameState.Day)
        {
            // Update timer
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
                
                // Auto-end day when timer expires
                if (timeRemaining <= 0)
                {
                    DebugLog("Day timer expired");
                    EndDay();
                }
            }
        }
        
        // Check for input in aftermath state
        if (currentState == GameState.Aftermath)
        {
            if (Input.anyKeyDown && continuePromptText != null && 
                continuePromptText.gameObject.activeInHierarchy)
            {
                StartCoroutine(TransitionToNextDay());
                currentState = GameState.Transition; // Prevent multiple inputs
            }
        }
    }
    
    /* DAY STATE */
    
    void StartDay()
    {
        DebugLog($"Starting day {currentDay}/{maxDays}");
        currentState = GameState.Day;
        
        // Reset timer
        timeRemaining = dayDuration;
        
        // Update UI
        UpdateDayCounter();
        
        // Show gameplay UI
        StartCoroutine(ShowGameplayUI());
        
        // Generate storm for this day
        if (stormGenerator != null)
        {
            currentStorm = stormGenerator.GenerateStorm(currentDay);
            DebugLog($"Generated storm for day {currentDay}");
        }
    }
    
    IEnumerator ShowGameplayUI()
    {
        // Fade out black panel
        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);
            StartCoroutine(FadeImage(blackPanel, 1f, 0f, fadeDuration));
        }
        
        // Wait a bit before showing UI
        yield return new WaitForSeconds(fadeDuration * 0.5f);
        
        // Fade in gameplay UI
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(gameplayUI, 0f, 1f, fadeDuration));
            gameplayUI.interactable = true;
            gameplayUI.blocksRaycasts = true;
        }
        
        // Start music with fade
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.volume = 0f;
            musicSource.Play();
            StartCoroutine(FadeAudioSource(musicSource, 0f, 1f, fadeDuration));
        }
    }
    
    /* END OF DAY */
    
    void EndDay()
    {
        DebugLog("Ending day");
        
        // Critical: First prepare storm calculation BEFORE any visual changes
        if (stormGenerator != null)
        {
            DebugLog("Preparing storm calculations first (NO DAMAGE YET)");
            stormGenerator.PrepareStormOnly();
        }
        
        // Now fade out UI
        StartCoroutine(FadeOutDayUI());
    }
    
    IEnumerator FadeOutDayUI()
    {
        // First fade out UI elements
        if (gameplayUI != null)
        {
            gameplayUI.interactable = false;
            gameplayUI.blocksRaycasts = false;
            StartCoroutine(FadeCanvasGroup(gameplayUI, gameplayUI.alpha, 0f, fadeDuration));
        }
        
        // Also fade out music
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeAudioSource(musicSource, musicSource.volume, 0f, fadeDuration));
        }
        
        // Wait for UI to fade out completely
        yield return new WaitForSeconds(fadeDuration);
        
        // Disable UI gameobjects
        if (gameplayUI != null)
            gameplayUI.gameObject.SetActive(false);
        
        if (musicSource != null)
            musicSource.Stop();
            
        // Now transition to night (black screen)
        StartBlackScreen();
    }
    
    /* BLACK SCREEN SEQUENCE */
    
    void StartBlackScreen()
    {
        DebugLog("Making screen black");
        currentState = GameState.Night;
        
        // Force black panel immediately
        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);
            blackPanel.color = new Color(0, 0, 0, 1f); // Force completely black
            
            Canvas panelCanvas = blackPanel.GetComponentInParent<Canvas>();
            if (panelCanvas != null)
                panelCanvas.sortingOrder = 9999;
            
            // Force Unity to update the canvas
            Canvas.ForceUpdateCanvases();
        }
        
        // Start storm audio here
        SetStormSounds(currentStorm);
        
        // Switch to storm damage state
        DebugLog("Now calling ApplyStormDamage()");
        Invoke("ApplyStormDamage", 0.5f); // Use Invoke to ensure screen is rendered before damage
    }
    
    /* STORM DAMAGE */
    
    void ApplyStormDamage()
    {
        // At this point screen should be completely black
        DebugLog("APPLYING STORM DAMAGE NOW (screen is black)");
        currentState = GameState.StormDamage;
        
        // Safety check - make sure black panel is visible
        if (blackPanel != null && (!blackPanel.gameObject.activeInHierarchy || blackPanel.color.a < 0.99f))
        {
            DebugLog("WARNING: Black panel not fully visible when applying damage!");
            blackPanel.gameObject.SetActive(true);
            blackPanel.color = new Color(0, 0, 0, 1f);
            Canvas.ForceUpdateCanvases();
        }
        
        // Apply damage
        if (stormGenerator != null)
        {
            currentStormStats = stormGenerator.ApplyPreparedStormDamage();
            DebugLog($"Storm damage applied: {currentStormStats.percentageDestroyed}% destroyed");
        }
        
        // Move to aftermath after a short delay
        Invoke("ShowAftermath", 1.0f);
    }
    
    /* AFTERMATH STATE */
    
    void ShowAftermath()
    {
        DebugLog("Showing storm aftermath");
        currentState = GameState.Aftermath;
        
        // Keep screen black
        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);
            blackPanel.color = new Color(0, 0, 0, 1);
        }
        
        // Show aftermath UI
        StartCoroutine(FadeInAftermathUI());
    }
    
    IEnumerator FadeInAftermathUI()
    {
        // Show aftermath UI
        if (stormAftermathUI != null)
        {
            stormAftermathUI.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(stormAftermathUI, 0f, 1f, fadeDuration));
            stormAftermathUI.interactable = true;
            stormAftermathUI.blocksRaycasts = true;
        }
        
        // Show storm summary
        StartCoroutine(DisplayStormInfo());
    }
    
    IEnumerator DisplayStormInfo()
    {
        // Show storm summary
        if (stormSummaryTypeWrite != null && currentStormStats != null)
        {
            stormSummaryTypeWrite.text = currentStormStats.GetSummary();
            stormSummaryTypeWrite.TypewriteText();
        }
        
        yield return new WaitForSeconds(1f);
        
        // Show destruction percentage
        if (destructionPercentageTypeWrite != null && currentStormStats != null)
        {
            destructionPercentageTypeWrite.text = $"{currentStormStats.percentageDestroyed:F1}% Destroyed";
            destructionPercentageTypeWrite.TypewriteText();
        }
        
        // Wait for typing to complete
        bool summaryComplete = false;
        bool percentageComplete = false;
        
        while (!summaryComplete || !percentageComplete)
        {
            if (stormSummaryTypeWrite == null || 
                stormSummaryTypeWrite.text.Length == 0 || 
                stormSummaryTypeWrite.GetComponent<Text>().text.Length >= stormSummaryTypeWrite.text.Length)
            {
                summaryComplete = true;
            }
            
            if (destructionPercentageTypeWrite == null || 
                destructionPercentageTypeWrite.text.Length == 0 || 
                destructionPercentageTypeWrite.GetComponent<Text>().text.Length >= destructionPercentageTypeWrite.text.Length)
            {
                percentageComplete = true;
            }
            
            yield return null;
        }
        
        // Wait an extra moment
        yield return new WaitForSeconds(afterTypingDelay);
        
        // Show continue prompt
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
            continuePromptText.text = "Press any key to continue...";
        }
    }
    
    /* TRANSITION TO NEXT DAY */
    
    IEnumerator TransitionToNextDay()
    {
        DebugLog("Transitioning to next day or end");
        
        // Fade out storm sounds
        foreach (AudioSource source in stormAudioSources)
        {
            if (source != null && source.isPlaying)
            {
                StartCoroutine(FadeAudioSource(source, source.volume, 0f, fadeDuration * 0.5f));
            }
        }
        
        // Fade out aftermath UI
        if (stormAftermathUI != null)
        {
            stormAftermathUI.interactable = false;
            stormAftermathUI.blocksRaycasts = false;
            yield return StartCoroutine(FadeCanvasGroup(stormAftermathUI, stormAftermathUI.alpha, 0f, fadeDuration));
            stormAftermathUI.gameObject.SetActive(false);
        }
        
        // Check if game should end
        bool maxDaysReached = currentDay >= maxDays;
        bool tooMuchDestruction = currentStormStats != null && 
            currentStormStats.percentageDestroyed >= gameOverDestructionThreshold;
        
        if (maxDaysReached || tooMuchDestruction)
        {
            // Game over
            isGameOver = true;
            
            // Make sure we have a black screen
            if (blackPanel != null)
            {
                blackPanel.gameObject.SetActive(true);
                blackPanel.color = new Color(0, 0, 0, 1f);
            }
            
            // Load end scene after a small delay
            yield return new WaitForSeconds(0.5f);
            
            if (!string.IsNullOrEmpty(endGameSceneName))
            {
                DebugLog("Loading end game scene");
                SceneManager.LoadScene(endGameSceneName);
            }
        }
        else
        {
            // Continue to next day
            currentDay++;
            DebugLog($"Advancing to day {currentDay}");
            
            // Ensure black screen fully visible
            if (blackPanel != null)
            {
                blackPanel.gameObject.SetActive(true);
                blackPanel.color = new Color(0, 0, 0, 1f);
            }
            
            // Wait a moment before starting new day
            yield return new WaitForSeconds(0.5f);
            
            // Start the next day
            StartDay();
        }
    }
    
    /* UTILITY METHODS */
    
    // Fade a canvas group
    IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        group.alpha = startAlpha;
        
        while (elapsedTime < duration)
        {
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        group.alpha = endAlpha;
    }
    
    // Fade an image
    IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        Color startColor = image.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, endAlpha);
        startColor.a = startAlpha;
        image.color = startColor;
        
        while (elapsedTime < duration)
        {
            image.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        image.color = endColor;
    }
    
    // Fade audio
    IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration)
    {
        float elapsedTime = 0;
        source.volume = startVolume;
        
        while (elapsedTime < duration)
        {
            source.volume = Mathf.Lerp(startVolume, endVolume, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        source.volume = endVolume;
    }
    
    void SetStormSounds(StormData storm)
    {
        if (storm == null)
            return;
            
        DebugLog("Setting storm sounds");
        
        // Calculate intensities
        float windIntensity = Mathf.Clamp01(storm.GetWindMetric() / 10f);
        float rainIntensity = Mathf.Clamp01(storm.GetFloodingMetric() / 10f);
        float thunderIntensity = Mathf.Clamp01(storm.GetLightningMetric() / 10f);
        float electricalIntensity = Mathf.Clamp01(storm.GetElectricalMetric() / 10f);
        
        // Apply audio balance curves
        windIntensity = Mathf.Pow(windIntensity, 0.7f);
        rainIntensity = rainIntensity * rainIntensity;
        thunderIntensity = Mathf.Pow(thunderIntensity, 0.8f);
        electricalIntensity = Mathf.Pow(electricalIntensity, 0.9f);
        
        // Set each sound to appropriate volume immediately (no fade)
        foreach (AudioSource source in stormAudioSources)
        {
            if (source == null || source.clip == null)
                continue;
                
            float targetVolume = 0f;
            
            if (source.clip == windSound)
                targetVolume = windIntensity;
            else if (source.clip == rainSound)
                targetVolume = rainIntensity;
            else if (source.clip == thunderSound)
                targetVolume = thunderIntensity;
            else if (source.clip == electricalSound)
                targetVolume = electricalIntensity;
                
            // Only play if significant
            if (targetVolume > 0.05f)
            {
                source.volume = targetVolume;
                source.Play();
            }
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    void UpdateDayCounter()
    {
        if (dayCounterText != null)
            dayCounterText.text = $"Day {currentDay} of {maxDays}";
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[DayManager] {message}");
    }
}