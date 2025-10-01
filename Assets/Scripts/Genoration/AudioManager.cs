using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public List<AudioClip> Files = new List<AudioClip>();
    public List<string> Names = new List<string>();
    public static Dictionary<string, AudioClip> Audio = new Dictionary<string, AudioClip>();
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Files.Count; i++)
        {
            Audio.Add(Names[i],Files[i]);
        }
    }

}
