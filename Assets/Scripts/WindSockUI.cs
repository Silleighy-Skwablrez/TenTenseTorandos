using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindSockUI : MonoBehaviour
{
     [Header("UI Images")]
    public RectTransform imageToRotate;//Rotating the image by direction

    [Header("Direction Settings")]
    public int direction; // Direction (0 = Up, 1 = Right, 2 = Down, 3 = Left)
    private readonly float[] rotationAngles = { 0f, 90f, 180f, 270f };
    public StormData stormData;
    private void Start()
    {
        int direction = stormData.getDirection();

        // Update the UI to match the initial direction
        UpdateDirectionUI();
    }

    private void Update(){
        UpdateDirectionUI();
    }

    private void UpdateDirectionUI()
    {
        //Rotate to direction
        if (imageToRotate != null)
        {
            imageToRotate.rotation = Quaternion.Euler(0f, 0f, -rotationAngles[direction]);
        }
    }
}
