using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectClickHandler : MonoBehaviour
{
    [Header("Object Settings")]
    public GameObject objectToInstantiate; // The object to instantiate when this one is clicked
    public Vector3 spawnOffset; // Optional offset for the new object's spawn position
    
    [Header("Item Settings")]
    public InventoryItem itemToGive; // The item to give when clicked
    
    private void OnMouseDown()
    {
        // Check if there is an object to instantiate
        if (objectToInstantiate != null)
        {
            // Instantiate the new object at this object's position + the offset
            GameObject spawnedObject = Instantiate(objectToInstantiate, transform.position + spawnOffset, Quaternion.identity);
            
            // Get the GroundItem component from the INSTANTIATED object (not the prefab)
            GroundItem groundItem = spawnedObject.GetComponent<GroundItem>();
            
            // If we found the GroundItem component and have an item to give
            if (groundItem != null && itemToGive != null)
            {
                // Initialize the ground item with our item
                groundItem.Setup(itemToGive);
            }
        }

        // Destroy this object
        Destroy(gameObject);
    }
}