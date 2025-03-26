using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Decoration", menuName = "Game/Decoration Data")]
public class DecorationData : ScriptableObject
{
    public string decorationName;
    public TileBase tile;
    public GameObject decorationPrefab;
    public bool breakable = true;
    public int maxHealth = 1;
    public InventoryItem dropItem;
}