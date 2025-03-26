using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Countdown : MonoBehaviour
{
    public Text timerText;
    public string sceneToLoad;
    
    private float timeRemaining = 180f; // 3 minutes in seconds
    
    void Start()
    {
        // Verify Text component is assigned
        if (timerText == null)
        {
            Debug.LogError("Timer Text component not assigned!");
        }
        
        UpdateTimerDisplay();
    }
    
    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        else
        {
            timeRemaining = 0;
            UpdateTimerDisplay();
            LoadNextScene();
        }
    }
    
    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
