using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovment : MonoBehaviour
{
    public const int ViewDistance = 200;
    private static GameObject Player;
    private static GameObject Water;
    private GameObject ViewPointCenter;
    public GameObject TileManagerObject;
    public static List<GameObject> ClosestInteractables;
    
    // Start is called before the first frame update
    void Start()
    {
        Player = new GameObject();
        ViewPointCenter = new GameObject("ViewPointCenter");
        Player.name = "Player";
        Player.transform.position = new Vector3(TileManager.MapSize/2, 50, TileManager.MapSize/2);
        Player.transform.Rotate(0,45f,0);


        return;
        StartCoroutine(UpdateLoadedTiles());
    }

    // Update is called once per frame
    void Update()
    {
        int MoveX = 0;
        int MoveY = 0;
        int MoveZ = 0;
        if (Input.GetKey(KeyCode.P))
        {
            MoveX = 10;
        }

        if (Input.GetKey(KeyCode.Semicolon))
        {
            MoveX = -10;
        }
        
        if (Input.GetKey(KeyCode.L))
        {
            MoveZ = 10;
        }

        if (Input.GetKey(KeyCode.Quote))
        {
            MoveZ = -10;

        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Break();
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            Place();
        }
        
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
        
        Player.transform.position += new Vector3(MoveX,MoveY,MoveZ);
        
        MoveConectedObjects(Player.transform.position);
        

        if (Player.transform.position.y <= -10) Player.transform.position += new Vector3(0,10,0);
    }

    private void MoveConectedObjects(Vector3 pos)
    {
        ViewPointCenter.transform.position = pos + new Vector3(35,0,35);
        Camera.main.transform.position = pos + new Vector3(-20,30,-20);

        //Water.transform.position = new Vector3(pos.x, 0, pos.z);
    }
    
    IEnumerator UpdateLoadedTiles()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            TileManagerObject.GetComponent<TileManager>().UpdateLoadedTiles(new Vector2(ViewPointCenter.transform.position[0],ViewPointCenter.transform.position[2]), ViewDistance);
        }
    }

    public static Vector3 getPlayerPos()
    {
        return Player.transform.position;
    }

    private void Break()
    {
        if (ClosestInteractables.Count > 0)
        {
            float minDistance = float.PositiveInfinity;
            GameObject interactWith = null;
            foreach (var item in ClosestInteractables)
            {
                float currentDistance = Vector3.Distance(Player.transform.position, item.transform.position);
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    interactWith = item;
                }
            }
            if(minDistance>5)return;
            ObjectFinder.RemoveObjectFromTile(interactWith);
            ClosestInteractables.Remove(interactWith);
        }
    }

    private void Place()
    {

        print("click");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            RailPlacer.HandlePlacementInput(hit.point); // Now static
        }

}
}
