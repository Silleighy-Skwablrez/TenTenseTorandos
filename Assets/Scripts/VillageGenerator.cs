using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using skner.DualGrid;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.Tilemaps;


public class VillageGenerator : MonoBehaviour
{
    private WorldHandler worldhandle = new WorldHandler();
    public GameObject house;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //make village after checking if grass at a point
    public void makeVillage()
    {
        Vector3Int startPos = checkPositions();
        house.Instantiate(startPos);
        house.Instantiate(startPos);
        

    }
    //Check Positions for if they're grass, returns position that has grass
    public Vector3Int checkPositions(){
        int i = 0;
        Vector3Int genPos = new Vector3Int(0, 0, 0);
        while(worldhandle.getTile(genPos).name != "grass"){
            i+=10;
            genPos = new Vector3Int(i, 0, 0);
        }
        return genPos;
    }
}
