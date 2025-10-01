using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DateTime = System.DateTime;
using StreamWriter = System.IO.StreamWriter;

public class SaveData : MonoBehaviour
{
    private DateTime Time;
    
    public Dictionary<Vector2,List<ObjectData>> SavedTileObjects;
    public List<Vector2> SavedTileObjectKeys;
    
    private const string SaveTarget = "/Tiles/";    
   // private long lastUpdate;
    
    // Start is called before the first frame update
    public SaveData()
    {
        Time = DateTime.Now;
     //   lastUpdate = 0;
        SavedTileObjects = new Dictionary<Vector2, List<ObjectData>>();
    }
    
    
    public List<ObjectData> DeserializeTile(Vector3 tilePos)
    {
        List<ObjectData> objectList = new List<ObjectData>(); 
        
        if (SavedTileObjectKeys.Contains(tilePos)) return SavedTileObjects[tilePos];
        
        float x = tilePos.x;
        float z = tilePos.z;
        if (tilePos.y != 0) z = tilePos.y;
        
        string address = Resorces.LoggedDataPath+SaveTarget + "(" + x + "," + z + ")";

        if (File.Exists(address))
        {
            try
            {
                StreamReader stream = new StreamReader(address);
                while (!stream.EndOfStream)
                {
                    GameObject obj = new GameObject("Unknown", typeof(ObjectData));
                    ObjectData objdata = obj.GetComponent<ObjectData>();
                    if(objdata.CopyFrom(stream.ReadLine())) objectList.Add(objdata);
                }

                SavedTileObjectKeys.Add(new Vector2(x, z));
                SavedTileObjects.Add(new Vector2(x, z), objectList);
                stream.Close();

                return objectList;
            }
            catch (Exception e)
            {
                print(e);
                return objectList;
            }
        }
        return objectList;
    }

    public bool SerializeTile(GameObject tile)
    {
        float x = tile.transform.position.x;
        float z = tile.transform.position.z;

        string address = Resorces.LoggedDataPath+SaveTarget + "(" + x + "," + z + ")";


        try
        {
            File.Delete(address);
        
            for (int i = 0; i < tile.transform.childCount; i++)
            {
                if (tile.transform.GetChild(i).name == "Connected Objects")
                {
                    GameObject ConectedObjects = tile.transform.GetChild(i).gameObject;

                    if(ConectedObjects.transform.childCount!=0){
                        StreamWriter stream = new StreamWriter(address);
                        for(int j = 0; j < ConectedObjects.transform.childCount; j++)
                        {
                            GameObject ConectedObject = ConectedObjects.transform.GetChild(j).gameObject;
                            ObjectData ConectedRootData = ConectedObject.GetComponent<ObjectData>();
                            
                            try
                            {
                                stream.WriteLine(ConectedRootData.GetObjJson());
                                Destroy(ConectedObject);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                        stream.Close();
                    }
                    break;
                }
            }
            Destroy(tile);
            SavedTileObjects.Remove(new Vector2(x, z));
            SavedTileObjectKeys.Remove(new Vector2(x, z));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        return true;
    }
}
