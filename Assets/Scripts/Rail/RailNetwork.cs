using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Splines.ExtrusionShapes;

public static class RailNetwork
{
    public static List<RailSegment> Track { get; private set; } = new List<RailSegment>();
    public static List<RailSegment> UnsetOccupiedTrack { get; private set; } = new List<RailSegment>();

    private static GameObject RailNetworkObject = new GameObject("RailNetwork");
    public static void RegisterNewTrack(RailSegment segment)
    {
        Track.Add(segment);
        segment.transform.parent = RailNetworkObject.transform;
        
    }

    public static List<RailSegment> GetSegmentsWithNodeAt(Vector3 position, RailSegment[] ignore = null)
    {
        List<RailSegment> segments = new List<RailSegment>();
        foreach (RailSegment segment in RailNetwork.Track)
        {
            if (ignore != null && ignore.Contains(segment)) continue;
            if(segment.SplineSegment.GetKnots().Contains(position))segments.Insert(0,segment);
        }
        return segments;
    }

     
}
