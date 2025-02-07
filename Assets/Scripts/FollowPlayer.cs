using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;  // Assign the player GameObject here
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Keeps the camera at a fixed Z position

    void FixedUpdate()
    {
        if (player == null) return;

        // Target position with offset
        Vector3 targetPosition = player.position + offset;

        // Smoothly interpolate between current position and target
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
