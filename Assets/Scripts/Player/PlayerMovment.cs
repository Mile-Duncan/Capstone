using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovment : MonoBehaviour
{
    public const int ViewDistance = 200;
    public static PlayerMovment Instance;
    private static GameObject Player;
    private static GameObject Water;
    private GameObject ViewPointCenter;
    public GameObject TileManagerObject;
    public static List<GameObject> ClosestInteractables;

    public static UnityEvent UseEvent = new();
    public static UnityEvent BreakEvent = new();


    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        Player = new GameObject();
        ViewPointCenter = new GameObject("ViewPointCenter");
        Player.name = "Player";
        Player.tag = "Player";
        Player.transform.position = new Vector3(TileManager.MapSize/2, 50, TileManager.MapSize/2);
        Player.transform.Rotate(0,45f,0);

    }

    // Update is called once per frame
    void Update()
    {
        if (Player == null)
        {
            Player = GameObject.FindWithTag("Player");
        }
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
            BreakEvent.Invoke();
            print("b");
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            UseEvent.Invoke();
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
    
    /*IEnumerator UpdateLoadedTiles()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            TileManagerObject.GetComponent<TileManager>().UpdateLoadedTiles(new Vector2(ViewPointCenter.transform.position[0],ViewPointCenter.transform.position[2]), ViewDistance);
        }
    }*/

    public static Vector3 getPlayerPos()
    {
        return Player.transform.position;
    }
    

    public RaycastHit GetMousePositionInWorld()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit);
        return hit;
    }
}
