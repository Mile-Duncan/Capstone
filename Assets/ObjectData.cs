using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ObjectData : MonoBehaviour
{
    private static long NextID;

    [SerializeField]private string Name;
    [SerializeField]private string ObjectType;
    [SerializeField]private string DropItem;
    [SerializeField]private string BreakAnimation;
    [SerializeField]private Vector3 OriginalPosition = new Vector3(float.NaN,float.NaN,float.NaN);
    [SerializeField]private long DespawnTime;
    [SerializeField]private bool Genorated;
    [SerializeField]private bool Interactable;
    [SerializeField]private long ID;
    [SerializeField]private Vector2 TilePos;

    void Start()
    {
        Name = name;
        if(float.IsNaN(OriginalPosition[0])) OriginalPosition = gameObject.transform.position;
    }
    
    public ObjectData()
    {
        ObjectType = "None";
        Genorated = false;
        DespawnTime = long.MaxValue;
        Interactable = false;
        ID = GetNextID();
    }

    public void CopyFrom(ObjectData od)
    {
        Name = od.Name;
        ObjectType = od.ObjectType;
        OriginalPosition = od.OriginalPosition;
        BreakAnimation = od.BreakAnimation;
        DropItem = od.DropItem;
        DespawnTime = od.DespawnTime;
        Genorated = od.Genorated;
        Interactable = od.Interactable;
        TilePos = od.TilePos;
        ID = GetNextID();
    }

    public string GetObjJson()
    {
        Name = name;
        return JsonUtility.ToJson(this);
    }

    public bool CopyFrom(string json)
    {
        JsonUtility.FromJsonOverwrite(json,this);
        
        //Get current Time
        DateTime Time = DateTime.Now;
        long UnixTime = ((DateTimeOffset)Time).ToUnixTimeMilliseconds();

        if (DespawnTime <= UnixTime)
        {
            DestroyImmediate(gameObject);
            return false;
        }

        name = Name;
        return true;
    }

    private long GetNextID()
    {
        return NextID++;
    }
    
    public bool Equals(ObjectData other)
    {
        return (other.OriginalPosition == OriginalPosition && other.ObjectType == ObjectType);
    }
    
    public string getType()
    {
        return ObjectType;
    }
    
    public void setType(string type)
    {
        ObjectType = type;
    }
    
    public string getItemDrop()
    {
        return DropItem;
    }
    
    public void setItemDrop(string item)
    {
        DropItem = item;
    }

    public string getBreakAnimation()
    {
        return BreakAnimation;
    }

    public void setBreakAnimation(string animation)
    {
        BreakAnimation = animation;
    }
    
    public Vector3 getOriginalPosition()
    {
        return OriginalPosition;
    }
    
    public Vector2 getOriginalXZPosition()
    {
        return new Vector2(OriginalPosition[0], OriginalPosition[2]);
    }
    
    public void setOriginalPosition(Vector3 pos)
    {
        OriginalPosition = pos;
    }

    public void setDespawnTime(long t)
    {
        DateTime Time = DateTime.Now;
        long UnixTime = ((DateTimeOffset)Time).ToUnixTimeMilliseconds();
        DespawnTime = UnixTime + t;
    }

    public long getDespawnTime()
    {
        return DespawnTime;
    }

    public void setGenorated(bool b)
    {
        Genorated = b;
    }

    public bool isGenorated()
    {
        return Genorated;
    }

    public void setInteractable(bool b)
    {
        Interactable = b;
    }

    public bool isInteractable()
    {
        return Interactable;
    }

    public void SetParentTile(GameObject tile)
    {
        TilePos = new Vector2(tile.transform.position.x,tile.transform.position.z);
    }
    
    public Vector2 GetParentTile()
    {
        return TilePos;
    }
}

