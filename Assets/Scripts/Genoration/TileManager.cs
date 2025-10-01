
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class TileManager : MonoBehaviour
{
    public const int MapSize = 2560;
    public const int DeffultMeshSize = 64;
    public const int DeffultMeshPolygonSize = 8;

    public static SaveData Save;
    
    public int publicSeed;
    public static int PerlinOffsetX;
    public static int PerlinOffsetZ;
    public static int Seed;

    public static GameObject TileGrid;
    
    public static GameObject Water;
    
    private Random rnd;
    public static Dictionary<Vector2, GameObject> loadedTiles  = new Dictionary<Vector2, GameObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        Save = GetComponent<SaveData>();
        
        TileGrid = new GameObject("TileGrid");
        
        rnd = new Random(publicSeed);
        PerlinOffsetX = rnd.Next(int.MinValue,int.MaxValue)%100000;
        PerlinOffsetZ = rnd.Next(int.MinValue,int.MaxValue)%100000;
        //TestGrid();
        Seed = publicSeed;
        
        CreateMap();
    }


    private void TestGrid()
    {
        for (int i = 0; i < 50; i++)for(int j = 0;j < 50; j++)
        {
            new Tile().NewTile(i * DeffultMeshSize, j * DeffultMeshSize, DeffultMeshSize, DeffultMeshPolygonSize);
        }
    }
    
    private void CreateMap()
    {
        for (int i = -DeffultMeshSize; i <= MapSize; i+=DeffultMeshSize)for(int j = -DeffultMeshSize;j <= MapSize; j+=DeffultMeshSize)
        {
            new Tile().NewTile(i, j, DeffultMeshSize, DeffultMeshPolygonSize);
        }
        
        Water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Water.transform.localScale = new Vector3(MapSize, 1, MapSize);
        Water.GetComponent<Renderer>().material = Resorces.Materials["Water"];
        Water.GetComponent<Collider>().enabled = false;
    }

    public static void ConnectObjectToTile(GameObject obj)
    {
        GameObject ClosestTile = GetClosestTile(obj);
        for (int i = 0; i < ClosestTile.transform.childCount; i++)
        {
            if (ClosestTile.transform.GetChild(i).name == "Connected Objects")
            {
                obj.transform.parent = ClosestTile.transform.GetChild(i).transform;

                GameObject ConectedObjects = ClosestTile.transform.GetChild(i).gameObject;
                ObjectData ConectedRootData = ConectedObjects.GetComponent<ObjectData>();
                //ObjectData AddedData = obj.GetComponent<ObjectData>();

                ConectedRootData.SetParentTile(ClosestTile);
                
                //if (ConectedRootData.getDespawnTime() > AddedData.getDespawnTime()) ConectedRootData.setDespawnTime(AddedData.getDespawnTime());
                //Save.AddObjects(ConectedObjects);
                return;
            }
        }
        
    }

    public static GameObject GetClosestTile(GameObject obj)
    {
        Vector3 objPos = obj.GetComponent<ObjectData>().getOriginalPosition();
        Vector2 tilePos = new Vector2((int)(objPos[0]/DeffultMeshSize)*DeffultMeshSize,(int)(objPos[2]/DeffultMeshSize)*DeffultMeshSize);
        return loadedTiles[tilePos];
    }

    public void UpdateLoadedTiles(Vector2 PlayerPos, int ViewDistance)
    {
        PlayerPos[0] = (int)PlayerPos[0] - ViewDistance / 2;
        PlayerPos[0] -= PlayerPos[0] % DeffultMeshSize;
        
        PlayerPos[1] = (int)PlayerPos[1] - ViewDistance / 2;
        PlayerPos[1] -= PlayerPos[1] % DeffultMeshSize;

        List<Vector2> keys = new List<Vector2>(loadedTiles.Keys);
        for(int i =0; i < keys.Count; i++)
        {
            Vector2 key = keys[i];
            if ((ViewDistance + PlayerPos[0] < key[0]||PlayerPos[0] > key[0])||(ViewDistance + PlayerPos[1] < key[1]||PlayerPos[1] > key[1]))
            {
                RemoveTile(key);
            }
        }

        Vector2[] TileBuffer = new Vector2[ViewDistance/DeffultMeshSize];
        int TileBufferIndex = 0;
        
        for(int i = 0; i < ViewDistance; i+=DeffultMeshSize )for(int j = 0; j < ViewDistance; j+=DeffultMeshSize){
                Vector2 NewTilePos = new Vector2(i, j) + PlayerPos;
                if (!loadedTiles.ContainsKey(NewTilePos))
                {
                    TileBuffer[TileBufferIndex] = NewTilePos;
                    TileBufferIndex++;
                    if (TileBufferIndex >= TileBuffer.Length)
                    {
                        StartCoroutine(CreateBufferedTiles(TileBuffer));
                        
                        TileBufferIndex = 0;
                        TileBuffer = new Vector2[TileBuffer.Length];
                    }
                }
        }
        
        if (TileBufferIndex >0)
        {
            Vector2[] MiniBuffer = new Vector2[TileBufferIndex];
            
            for(int i = 0; i < TileBufferIndex; i++)
            {
                MiniBuffer[i] = TileBuffer[i];
            }
            StartCoroutine(CreateBufferedTiles(MiniBuffer));
        }
    }
    
    

    IEnumerator CreateBufferedTiles(Vector2[] TileBuffer)
    {
        foreach (var TilePos in TileBuffer)
        {
            if (TilePos != null)
            {
                if (TilePos != null) CreateTile(TilePos);
                yield return new WaitForSeconds(0.01f*DeffultMeshSize);
            }
    
        }
    }


    private static void CreateTile(Vector2 pos)
    {
        if (!loadedTiles.ContainsKey(pos))
        {
            GameObject dummy = new GameObject();
            loadedTiles.Add(pos, dummy);
            loadedTiles[pos] = new Tile().NewTile((int)pos[0], (int)pos[1], DeffultMeshSize, DeffultMeshPolygonSize);
            DestroyImmediate(dummy);
            //loadedTiles.Add(pos, new Tile().NewTile((int)pos[0], (int)pos[1], DeffultMeshSize));
        }
    }
    
    private void RemoveTile(Vector2 pos)
    {
        Save.SerializeTile(loadedTiles[pos]);
        Destroy(loadedTiles[pos]);
        loadedTiles.Remove(pos);
    }
    
    public int GetSeed()
    {
        return publicSeed;
    }

    public static int GetRandom(int seed)
    {
        return new Random(seed).Next();
    }
    
    public static int GetRandom(int seed, int lower, int upper)
    {
        Random rnd = new Random(seed+Seed);
        return rnd.Next(lower, upper);
    }

    public static GameObject GetTileAt(Vector3 pos)
    {
        Vector2 tilePos = new Vector2(((int)(pos[0] / DeffultMeshSize))*DeffultMeshSize, ((int)(pos[2] / DeffultMeshSize))*DeffultMeshSize);
        if (loadedTiles.ContainsKey(tilePos)) return loadedTiles[tilePos];
        return null;
    }
}
