using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    [Tooltip("Name of the scene to load when button is pressed")]
    public string sceneToLoad = "SampleScene";
    
    private Button playButton;

    private void Start()
    {
        // Get the Button component
        playButton = GetComponent<Button>();
        
        // Add listener to the button click event
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogWarning("PlayButton script attached to an object without a Button component");
        }
    }

    // This method can be called directly from the Button's onClick event in the Inspector
    public void StartGame()
    {
        Debug.Log("Loading scene: " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }
}