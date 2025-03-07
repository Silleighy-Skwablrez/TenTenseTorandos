using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;

public partial class CursorController : MonoBehaviour {

    private WorldHandler worldHandler;
    // public audio for placing and breaking tiles
    public AudioSource tileAudio;
    public DualGridRuleTile tile;
    
    // Sound rate limiting (10 sounds per second = 0.1f cooldown)
    [Tooltip("Minimum time between tile placements in seconds (0.1 = 10 tiles/sec)")]
    public float placementCooldown = 0.1f;
    private float lastPlacementTime = 0f;
    
    // Track the last position where we placed a tile
    private Vector3Int lastTilePos = new Vector3Int(int.MinValue, int.MinValue, 0);


    void Start()
    {
        worldHandler = GameObject.Find("WorldHandler").GetComponent<WorldHandler>();
    }

    void Update() {
        var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int tilePos = GetWorldPosTile(mouseWorldPos);
        //transform.position = tilePos + new Vector3(0.5f, 0.5f, -1);
        // lerp instead
        transform.position = Vector3.Lerp(transform.position, tilePos + new Vector3(0.5f, 0.5f, -1), 0.1f);

        bool canPlaceTile = (Time.time - lastPlacementTime >= placementCooldown);

        if (Input.GetMouseButton(0)) {
            // Only set cell and play audio if we're at a new position AND cooldown has passed
            if (!tilePos.Equals(lastTilePos) && canPlaceTile) {
                bool tileChanged = worldHandler.SetTile(tilePos, tile);
                
                // Play sound and update timestamps
                if (tileChanged) {
                    tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    tileAudio.PlayOneShot(tileAudio.clip);
                    lastPlacementTime = Time.time;
                    lastTilePos = tilePos;
                }
            }
        // } else if (Input.GetMouseButton(1)) {
        //     // Only set cell and play audio if we're at a new position AND cooldown has passed
        //     if (!tilePos.Equals(lastTilePos) && canPlaceTile) {
        //         bool tileChanged = worldHandler.SetTile(tilePos, null);
                
        //         // Play sound and update timestamps
        //         if (tileChanged) {
        //             tileAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        //             tileAudio.PlayOneShot(tileAudio.clip);
        //             lastPlacementTime = Time.time;
        //             lastTilePos = tilePos;
        //         }
        //     }

           
        } else {
            // Reset the last position when no mouse button is pressed
            lastTilePos = new Vector3Int(int.MinValue, int.MinValue, 0);
        }
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos) {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt, 0);
    }
}