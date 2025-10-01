using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Resorces : MonoBehaviour
{
    public Sprite[] SetSprites;
    public string[] NameSprites;

    public static Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    
    public string[] SetMaterials;
    public Material[] NameMaterials;
    
    public static Dictionary<string,Material> Materials = new Dictionary<string, Material>();

    public static string LoggedDataPath = "./Assets/Saved";

    
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < SetSprites.Length; i++)Sprites.Add(NameSprites[i],SetSprites[i]);
        
        for(int i=0; i < NameMaterials.Length && i < SetMaterials.Length ;i++)Materials.Add(SetMaterials[i],NameMaterials[i]);

        gameObject.AddComponent<RailNetwork>();
    }

}
