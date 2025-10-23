using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Splines.ExtrusionShapes;

public static class RailNetwork
{
    public static List<RailSegment> Track { get; private set; } = new List<RailSegment>();
    private static GameObject RailNetworkObject = new GameObject("RailNetwork");
    public static void RegisterNewTrack(RailSegment segment)
    {
        Track.Add(segment);
        segment.transform.parent = RailNetworkObject.transform;
        
    }

    

     
}
