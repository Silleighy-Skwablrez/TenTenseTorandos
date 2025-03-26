using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TypeWrite : MonoBehaviour
{
    [TextArea(3, 10)]
    public string text;
    
    private string _previousText;
    private Text _textComponent;
    private Coroutine _typewriteCoroutine;
    
    [Header("Sound Effects")]
    public AudioClip typingSound;
    [Range(0f, 1f)]
    public float typingSoundVolume = 0.5f;
    
    private AudioSource _audioSource;

    [Header("Typing Parameters")]
    [Range(1f, 30f)]
    public float baseTypingSpeed = 15f;
    
    [Range(0.3f, 1f)]
    public float minSpeedMultiplier = 0.5f;
    
    [Range(1f, 3f)]
    public float maxSpeedMultiplier = 1.8f;
    
    [Range(0f, 0.15f)]
    public float thinkPauseChance = 0.05f;
    
    [Range(0.1f, 2f)]
    public float minThinkDuration = 0.3f;
    
    [Range(0.2f, 3f)]
    public float maxThinkDuration = 1.2f;
    
    void Start()
    {
        _textComponent = GetComponent<Text>();
        if (_textComponent == null)
        {
            Debug.LogError("TypeWrite requires a UI.Text component attached to the same GameObject");
            return;
        }
        
        // Set up dedicated audio source for typing sounds
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source for UI sounds (non-positional)
        _audioSource.spatialBlend = 0f; // 2D sound (no stereo positioning)
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        
        _previousText = text;
        if (!string.IsNullOrEmpty(text))
        {
            TypewriteText();
        }
    }

    void Update()
    {
        // Automatically trigger effect when text changes
        if (text != _previousText)
        {
            _previousText = text;
            TypewriteText();
        }
    }
    
    public void TypewriteText()
    {
        if (_textComponent == null) return;
        
        if (_typewriteCoroutine != null)
        {
            StopCoroutine(_typewriteCoroutine);
        }
        
        _typewriteCoroutine = StartCoroutine(TypewriteRoutine());
    }
    
    private IEnumerator TypewriteRoutine()
    {
        _textComponent.text = "";
        
        if (string.IsNullOrEmpty(text)) yield break;
        
        int totalLength = text.Length;
        int rampUpEnd = Mathf.Min(totalLength / 5, 12);
        int rampDownStart = Mathf.Max(totalLength - totalLength / 4, totalLength - 10);
        
        float burstCounter = Random.Range(3f, 7f);
        bool inBurst = false;
        float burstDuration = 0f;
        
        for (int i = 0; i < totalLength; i++)
        {
            // Add the next character
            _textComponent.text += text[i];

            // Play typing sound with random pitch
            if (typingSound != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.8f, 1.1f);
                _audioSource.PlayOneShot(typingSound, typingSoundVolume);
            }
            
            // Check for bursts of typing
            burstCounter -= Time.deltaTime;
            if (burstCounter <= 0 && !inBurst)
            {
                inBurst = true;
                burstDuration = Random.Range(1.5f, 3.5f);
            }
            
            if (inBurst)
            {
                burstDuration -= Time.deltaTime;
                if (burstDuration <= 0)
                {
                    inBurst = false;
                    burstCounter = Random.Range(3f, 8f);
                }
            }
            
            // Calculate typing speed multiplier based on position and bursts
            float speedMultiplier;
            
            if (i < rampUpEnd)
            {
                // Ramp up speed at the beginning
                speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, (float)i / rampUpEnd);
            }
            else if (i > rampDownStart)
            {
                // Ramp down speed at the end
                speedMultiplier = Mathf.Lerp(1f, minSpeedMultiplier * 1.2f, 
                    (float)(i - rampDownStart) / (totalLength - rampDownStart));
            }
            else
            {
                // Middle section - normal or burst speed
                speedMultiplier = inBurst ? maxSpeedMultiplier : 1f;
            }
            
            // Add random variation to typing speed
            speedMultiplier *= Random.Range(0.85f, 1.15f);
            
            // Calculate delay before next character
            float charDelay = 1f / (baseTypingSpeed * speedMultiplier);
            
            // Add thinking pauses
            if (Random.value < thinkPauseChance && (text[i] == ' ' || text[i] == '.' || text[i] == ','))
            {
                yield return new WaitForSeconds(Random.Range(minThinkDuration, maxThinkDuration));
            }
            // Pause longer after punctuation
            else if (IsPunctuation(text[i]))
            {
                yield return new WaitForSeconds(charDelay * 2.5f);
            }
            else
            {
                yield return new WaitForSeconds(charDelay);
            }
        }
    }

    [Header("Backspace Parameters")]
    [Range(0.05f, 0.5f)]
    public float initialBackspaceDelay = 0.1f;

    [Range(0.01f, 0.1f)]
    public float minBackspaceDelay = 0.02f;

    [Range(0.7f, 0.99f)]
    public float backspaceAcceleration = 0.92f;

    [Range(0.1f, 1f)]
    public float backspaceStartDelay = 0.3f;

    private Coroutine _backspaceCoroutine;

    public void ClearText()
    {
        if (_textComponent == null) return;
        
        // Stop typing if it's in progress
        if (_typewriteCoroutine != null)
        {
            StopCoroutine(_typewriteCoroutine);
            _typewriteCoroutine = null;
        }
        
        // Stop existing backspace if it's already running
        if (_backspaceCoroutine != null)
        {
            StopCoroutine(_backspaceCoroutine);
        }
        
        _backspaceCoroutine = StartCoroutine(BackspaceRoutine());
    }

    public void StopBackspace()
    {
        if (_backspaceCoroutine != null)
        {
            StopCoroutine(_backspaceCoroutine);
            _backspaceCoroutine = null;
        }
    }

    private IEnumerator BackspaceRoutine()
    {
        if (_textComponent == null || string.IsNullOrEmpty(_textComponent.text)) yield break;
        
        string currentText = _textComponent.text;
        float backspaceDelay = initialBackspaceDelay;
        
        // Small initial delay before backspacing starts
        yield return new WaitForSeconds(backspaceStartDelay);
        
        while (currentText.Length > 0)
        {
            // Remove the last character
            currentText = currentText.Substring(0, currentText.Length - 1);
            _textComponent.text = currentText;
            
            // Slightly different delays for different characters
            if (currentText.Length > 0 && IsPunctuation(currentText[currentText.Length - 1]))
            {
                yield return new WaitForSeconds(backspaceDelay * 1.2f);
            }
            else
            {
                yield return new WaitForSeconds(backspaceDelay);
            }
            
            // Accelerate backspace speed (but not below minimum)
            backspaceDelay = Mathf.Max(backspaceDelay * backspaceAcceleration, minBackspaceDelay);
        }
        
        // Update the stored text
        text = "";
        _previousText = "";
        _backspaceCoroutine = null;
    }
    
    
    private bool IsPunctuation(char c)
    {
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':';
    }
}