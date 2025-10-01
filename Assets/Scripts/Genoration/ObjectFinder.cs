using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectFinder : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetClosestObject());
    }
    
    IEnumerator GetClosestObject()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            Vector3 PlayerPos = PlayerMovment.getPlayerPos();
            List<GameObject> objects = new List<GameObject>();

            for (int i = -2; i <= 2; i++)
            {
                for (int h = -2; h <= 2; h++)
                {
                    GameObject tile = TileManager.GetTileAt(new Vector3(PlayerPos[0] + (h * 4), PlayerPos[1], PlayerPos[2] + (i * 4)));
                    if (tile != null)
                    {
                        for (int j = 0; j < tile.transform.childCount; j++)
                        {
                            if (tile.transform.GetChild(j).gameObject.GetComponent<ObjectData>().isInteractable())
                            {
                                GameObject TileObjectSet = tile.transform.GetChild(j).gameObject;
                                for (int k = 0; k < TileObjectSet.transform.childCount; k++)
                                {
                                    GameObject child = TileObjectSet.transform.GetChild(k).gameObject;
                                    if (child.GetComponent<ObjectData>().isInteractable()) objects.Add(child);
                                }
                            }
                        }
                    }
                }
            }
            
            PlayerMovment.ClosestInteractables = objects;
        }
    }
    

    public static void RemoveObjectFromTile(GameObject obj)
    {
        Destroy(obj);
    }
}
