using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectClickHandler : MonoBehaviour
{
    [Header("Object Settings")]
    public GameObject objectToInstantiate; // The object to instantiate when this one is clicked

    public Vector3 spawnOffset; // Optional offset for the new object's spawn position

    private void OnMouseDown()
    {
        // Check if there is an object to instantiate
        if (objectToInstantiate != null)
        {
            // Instantiate the new object at this object's position + the offset
            Instantiate(objectToInstantiate, transform.position + spawnOffset, Quaternion.identity);
        }

        // Destroy this object
        Destroy(gameObject);
    }
}
