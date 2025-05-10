using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

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
    public CanvasGroup gameOverUI;  // New canvas group for game over screen
    public TypeWrite stormSummaryTypeWrite;
    public TypeWrite destructionPercentageTypeWrite;
    public TypeWrite gameOverMessageTypeWrite;  // New typewrite for game over messages
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
    
    [Header("Game Over Messages")]
    public string winMessage = "Congratulations! You've survived all 10 days. You've proven yourself as a capable meteorologist.";
    public string lossMessage = "Your town has sustained critical damage. With over 50% destruction, recovery is impossible. The remaining residents have decided to evacuate.";
    public string restartPrompt = "Press 'ESC' to return to the main menu.";
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // States
    private enum GameState { Day, Night, StormPrep, StormDamage, Aftermath, GameOver, Transition }
    private GameState currentState;
    
    // Events for better flow control
    private event Action OnTypewriteComplete;
    
    // Private variables
    private int currentDay = 1;
    private float timeRemaining;
    private bool isGameOver = false;
    private bool isWin = false;
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
        
        // Initialize game over UI if it exists
        if (gameOverUI != null)
        {
            gameOverUI.alpha = 0;
            gameOverUI.interactable = false;
            gameOverUI.blocksRaycasts = false;
            gameOverUI.gameObject.SetActive(false);
        }
        
        // Initialize typewrite components with completion callbacks
        if (stormSummaryTypeWrite != null)
        {
            stormSummaryTypeWrite.OnTypewriteComplete += () => {
                DebugLog("Storm summary typewrite complete");
                isSummaryComplete = true;
                CheckTypewriteCompletions();
            };
        }
        
        if (destructionPercentageTypeWrite != null)
        {
            destructionPercentageTypeWrite.OnTypewriteComplete += () => {
                DebugLog("Destruction percentage typewrite complete");
                isPercentageComplete = true;
                CheckTypewriteCompletions();
            };
        }
        
        if (gameOverMessageTypeWrite != null)
        {
            gameOverMessageTypeWrite.OnTypewriteComplete += () => {
                DebugLog("Game over message typewrite complete");
                ShowRestartPrompt();
            };
        }
    }
    
    // Use this to handle when both typewrite components are done
    private bool isSummaryComplete = false;
    private bool isPercentageComplete = false;
    
    private void CheckTypewriteCompletions()
    {
        // Only invoke if both are complete
        if (isSummaryComplete && isPercentageComplete)
        {
            OnTypewriteComplete?.Invoke();
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
        
        // Check for restart or quit in game over state
        if (currentState == GameState.GameOver)
        {
            if (Input.anyKeyDown && continuePromptText != null && 
                continuePromptText.gameObject.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    // Restart the game
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // Return to menu
                    SceneManager.LoadScene("Menu");
                }
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
        
        // Reset typewrite completion flags
        isSummaryComplete = false;
        isPercentageComplete = false;
        
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
        StartCoroutine(EndDaySequence());
    }
    
    IEnumerator EndDaySequence()
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
            
        // Now transition to night with storm
        yield return StartCoroutine(StormSequence());
    }
    
    /* STORM SEQUENCE */
    
    IEnumerator StormSequence()
    {
        // Make screen black
        DebugLog("Making screen black");
        currentState = GameState.Night;
        
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
        
        // Start storm audio
        StartCoroutine(FadeInStormSounds(currentStorm));
        
        // Ensure screen is fully black before damage calculation
        yield return new WaitForSeconds(0.5f);
        
        // Apply storm damage
        DebugLog("APPLYING STORM DAMAGE NOW (screen is black)");
        currentState = GameState.StormDamage;
        
        // Double-check black screen is active
        if (blackPanel != null)
        {
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
        
        // Delay for dramatic effect
        yield return new WaitForSeconds(1.0f);
        
        // Show aftermath
        yield return StartCoroutine(ShowAftermathSequence());
    }
    
    /* AFTERMATH STATE */
    
    IEnumerator ShowAftermathSequence()
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
        if (stormAftermathUI != null)
        {
            stormAftermathUI.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(stormAftermathUI, 0f, 1f, fadeDuration));
            stormAftermathUI.interactable = true;
            stormAftermathUI.blocksRaycasts = true;
        }
        
        // Setup completion callback
        OnTypewriteComplete = null;
        OnTypewriteComplete += ShowContinuePrompt;
        
        // Start typing effects
        yield return StartCoroutine(DisplayStormInfo());
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
        
        // If TypeWrite components don't have callbacks, fall back to timing
        if (stormSummaryTypeWrite == null || destructionPercentageTypeWrite == null)
        {
            // Fallback timing if components are missing
            float timeToWait = Mathf.Max(3f, afterTypingDelay * 2);
            yield return new WaitForSeconds(timeToWait);
            ShowContinuePrompt();
        }
    }
    
    private void ShowContinuePrompt()
    {
        // Only show once
        OnTypewriteComplete = null;
        
        DebugLog("Showing continue prompt");
        
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
        
        // Check if game should end
        bool maxDaysReached = currentDay >= maxDays;
        bool tooMuchDestruction = currentStormStats != null && 
            currentStormStats.percentageDestroyed >= gameOverDestructionThreshold;
        
        if (maxDaysReached || tooMuchDestruction)
        {
            // Game over - keep the aftermath UI visible
            isGameOver = true;
            isWin = maxDaysReached && !tooMuchDestruction;
            
            // Show game over message using the EXISTING UI 
            // (no need to fade out/in, just update the text)
            yield return new WaitForSeconds(0.5f);
            ShowGameOverScreen();
        }
        else
        {
            // Only fade out aftermath UI if continuing to next day
            if (stormAftermathUI != null)
            {
                stormAftermathUI.interactable = false;
                stormAftermathUI.blocksRaycasts = false;
                yield return StartCoroutine(FadeCanvasGroup(stormAftermathUI, stormAftermathUI.alpha, 0f, fadeDuration));
                stormAftermathUI.gameObject.SetActive(false);
            }
            
            // Continue to next day
            currentDay++;
            DebugLog($"Advancing to day {currentDay}");
            if (GameStats.Instance != null)
            {
                GameStats.Instance.IncrementStat("Total Days Survived");
            }
            
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
    
    /* GAME OVER SCREEN */
    
    void ShowGameOverScreen()
    {
        currentState = GameState.GameOver;
        DebugLog($"Game over: {(isWin ? "Win" : "Loss")}");
        
        // Reset typewrite completion flags since we're reusing the typewriters
        isSummaryComplete = false;
        isPercentageComplete = false;
        
        // Update the text content directly in the existing UI elements
        if (stormSummaryTypeWrite != null)
        {
            stormSummaryTypeWrite.text = isWin ? winMessage : lossMessage;
            stormSummaryTypeWrite.TypewriteText();
        }
        
        // Clear the second typewrite or use it for additional stats
        if (destructionPercentageTypeWrite != null)
        {
            destructionPercentageTypeWrite.text = "";  
            destructionPercentageTypeWrite.TypewriteText();
        }
        
        // Setup completion callback
        OnTypewriteComplete = null;
        OnTypewriteComplete += ShowRestartPrompt;
    }

    private IEnumerator ShowGameOverMessage()
    {
        // Wait a frame to ensure UI is properly activated
        yield return null;
        
        // Reset typewrite completion flags since we're reusing the typewriters
        isSummaryComplete = false;
        isPercentageComplete = false;
        
        // Make sure the parent objects of typewrite components are active
        if (stormSummaryTypeWrite != null)
        {
            // Ensure the gameObject is active
            stormSummaryTypeWrite.gameObject.SetActive(true);
            stormSummaryTypeWrite.text = isWin ? winMessage : lossMessage;
            stormSummaryTypeWrite.TypewriteText();
        }
        
        // Clear the second typewrite or use it for additional info
        if (destructionPercentageTypeWrite != null)
        {
            // Ensure the gameObject is active
            destructionPercentageTypeWrite.gameObject.SetActive(true);
            destructionPercentageTypeWrite.text = "";
            destructionPercentageTypeWrite.TypewriteText();
        }
        
        // Setup completion callback
        OnTypewriteComplete = null;
        OnTypewriteComplete += ShowRestartPrompt;
    }

    private void ShowRestartPrompt()
    {
        // Only show once
        OnTypewriteComplete = null;
        
        DebugLog("Showing restart prompt");
        
        // Show continue prompt
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
            
            // Use different prompts depending on the state
            if (currentState == GameState.GameOver)
            {
                continuePromptText.text = restartPrompt;
            }
            else
            {
                continuePromptText.text = "Press any key to continue...";
            }
        }
    }
    
    /* IMPROVED STORM AUDIO */
    
    IEnumerator FadeInStormSounds(StormData storm)
    {
        if (storm == null)
            yield break;
            
        DebugLog("Setting storm sounds with fade");
        
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
        
        // Set each sound with proper fade
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
                
            // Only play if significant volume
            if (targetVolume > 0.05f)
            {
                source.volume = 0f;
                source.Play();
                StartCoroutine(FadeAudioSource(source, 0f, targetVolume, fadeDuration));
            }
        }
        
        yield return null;
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
            dayCounterText.text = $"Day {currentDay}";
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[DayManager] {message}");
    }
}