using UnityEngine;

public class FpsLimit : MonoBehaviour
{
    public int targetFrameRate = 30;

    private void Start()
    {
        // QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}