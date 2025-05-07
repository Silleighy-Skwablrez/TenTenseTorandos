using UnityEngine;
using UnityEngine.UI;

public class QuitGame : MonoBehaviour
{
    private Button quitButton;

    private void Start()
    {
        // Get the Button component
        quitButton = GetComponent<Button>();
        
        // Add listener to the button click event
        if (quitButton != null)
        {
            Debug.Log("QuitGame script attached to a Button component");
            quitButton.onClick.AddListener(ExitGame);
        }
        else
        {
            Debug.LogWarning("QuitGame script attached to an object without a Button component");
        }
    }

    // This method can be called directly from the Button's onClick event in the Inspector
    public void ExitGame()
    {
        Debug.Log("Quitting application");
        
        #if UNITY_EDITOR
        // If we're in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // If we're in a build
        Application.Quit();
        #endif
    }
}