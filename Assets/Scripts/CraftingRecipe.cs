using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe : MonoBehaviour
{
    public InventoryItem[][] ingredients; // Required grid input for recipe
    public InventoryItem result; // What the recipe is for
    public bool isRotatable; // Allows the recipe to match even if the grid is rotated by a multiple of 90 degrees
    public bool isReversable; // This doesn't entail "uncrafting", but the recipe grid itself being able to be mirrored

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
