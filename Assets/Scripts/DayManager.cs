using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    [Header("Day Settings")]
    public int maxDays = 10;
    public float dayDuration = 180f; // 3 minutes per day
    public Text dayCounterText;
    public Text timerText;
    
    [Header("Storm Settings")]
    public StormGenerator stormGenerator;
    
    [Header("UI Elements")]
    public CanvasGroup gameplayUI;
    public CanvasGroup stormAftermathUI;
    public Text stormSummaryText;
    public Text destructionPercentageText;
    
    [Header("Audio")]
    public AudioClip windSound;
    public AudioClip rainSound;
    public AudioClip thunderSound;
    public AudioClip electricalSound;
    public AudioSource audioSource;
    
    [Header("Scene Transition")]
    public string endGameSceneName;
    
    private int currentDay = 1;
    private float timeRemaining;
    private bool isDayActive = true;
    private bool isGameOver = false;
    
    void Start()
    {
        if (stormGenerator == null)
            stormGenerator = FindObjectOfType<StormGenerator>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        InitializeDay();
    }
    
    void Update()
    {
        if (!isDayActive || isGameOver)
            return;
            
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        else
        {
            EndDay();
        }
    }
    
    void InitializeDay()
    {
        timeRemaining = dayDuration;
        UpdateDayCounter();
        UpdateTimerDisplay();
        
        // Show gameplay UI
        SetUIVisibility(gameplayUI, true);
        SetUIVisibility(stormAftermathUI, false);
        
        // Generate storm for this day
        stormGenerator.GenerateStorm(currentDay);
        
        isDayActive = true;
    }
    
    void EndDay()
    {
        isDayActive = false;
        
        // Start storm sequence
        StartCoroutine(PlayStormSequence());
    }
    
    IEnumerator PlayStormSequence()
    {
        // Hide game UI
        SetUIVisibility(gameplayUI, false);
        
        // Fade to black
        yield return StartCoroutine(FadeToBlack());
        
        // Apply storm damage
        stormGenerator.ApplyStormDamage();
        StormDamageStatistics stats = stormGenerator.GetStormDamageStatistics();
        
        // Play storm sounds based on storm intensity
        yield return StartCoroutine(PlayStormSounds(stormGenerator.GetCurrentStorm()));
        
        // Display storm aftermath UI
        SetUIVisibility(stormAftermathUI, true);
        stormSummaryText.text = stats.GetSummary();
        destructionPercentageText.text = $"{stats.percentageDestroyed:F1}% Destroyed";
        
        // Wait for player to continue
        yield return new WaitForSeconds(5f);
        yield return StartCoroutine(WaitForPlayerInput());
        
        // Check if game is over
        if (currentDay >= maxDays)
        {
            isGameOver = true;
            // Load end game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(endGameSceneName);
        }
        else
        {
            // Start next day
            currentDay++;
            InitializeDay();
        }
    }
    
    IEnumerator FadeToBlack()
    {
        // Add a black overlay and fade it in
        // This could be done with a UI Image that's controlled here
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator PlayStormSounds(StormData storm)
    {
        float windIntensity = storm.GetWindMetric() / 10f;
        float rainIntensity = storm.GetFloodingMetric() / 10f;
        float thunderIntensity = storm.GetLightningMetric() / 10f;
        float electricalIntensity = storm.GetElectricalMetric() / 10f;
        
        // Play wind sound if significant
        if (windIntensity > 0.1f && windSound != null)
        {
            audioSource.clip = windSound;
            audioSource.volume = windIntensity;
            audioSource.Play();
            yield return new WaitForSeconds(Mathf.Lerp(2f, 5f, windIntensity));
        }
        
        // Play rain sound if significant
        if (rainIntensity > 0.1f && rainSound != null)
        {
            audioSource.clip = rainSound;
            audioSource.volume = rainIntensity;
            audioSource.Play();
            yield return new WaitForSeconds(Mathf.Lerp(2f, 5f, rainIntensity));
        }
        
        // Play thunder sound if significant
        if (thunderIntensity > 0.1f && thunderSound != null)
        {
            audioSource.clip = thunderSound;
            audioSource.volume = thunderIntensity;
            audioSource.Play();
            yield return new WaitForSeconds(Mathf.Lerp(2f, 5f, thunderIntensity));
        }
        
        // Play electrical sound if significant
        if (electricalIntensity > 0.1f && electricalSound != null)
        {
            audioSource.clip = electricalSound;
            audioSource.volume = electricalIntensity;
            audioSource.Play();
            yield return new WaitForSeconds(Mathf.Lerp(2f, 5f, electricalIntensity));
        }
    }
    
    IEnumerator WaitForPlayerInput()
    {
        bool inputReceived = false;
        
        // Add an instruction text telling the player to press any key
        // stormAftermathUI should include this text element
        
        while (!inputReceived)
        {
            if (Input.anyKeyDown)
                inputReceived = true;
                
            yield return null;
        }
    }
    
    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    void UpdateDayCounter()
    {
        if (dayCounterText != null)
            dayCounterText.text = $"Day {currentDay} of {maxDays}";
    }
    
    void SetUIVisibility(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
            
        group.alpha = visible ? 1 : 0;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}