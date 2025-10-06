using System.Collections;
using System.Collections.Generic;
// Removed unused 'UnityEditor.PackageManager'
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics; // Used for float3 and distances

public static class RailPlacer
{
    private const float SnapRange = 1.5f;
    private static bool _active = false;
    
    private static Vector3 _firstPosition;
    private static RailSegment _placingRailSegment;
    private static Vector3 _controlPointPosition;
    
    public static Vector3 GetTrackPlacementPosition(Vector3 originalPlacePosition)
    {
        foreach (RailSegment segment in RailNetwork.Track)
        {
            if (Vector3.Distance(segment.SplineSegment.GetKnots()[0], originalPlacePosition) <= SnapRange)
            {
                return segment.SplineSegment.GetKnots()[0];
            };
            if (Vector3.Distance(segment.SplineSegment.GetKnots()[1], originalPlacePosition) <= SnapRange)
            {
                return segment.SplineSegment.GetKnots()[1];
            };
        }
        return originalPlacePosition;
    }

    public static void TogglePlacementSequence(Vector3 clickPosition)
    {
        if (_active)
        {
            _active = false;
            return;
        }
        _active = true;
        _firstPosition = clickPosition;
        
        _controlPointPosition = new Vector3(clickPosition.x+10, clickPosition.y, clickPosition.z+10);

        _placingRailSegment = new GameObject("Rail Segment: " + RailNetwork.Track.Count).AddComponent<RailSegment>();

        _placingRailSegment.SplineSegment = new PlaceableSplineSegment(clickPosition, clickPosition, _controlPointPosition);
        _placingRailSegment.segmentType = RailSegment.SegmentType.Holographic;
        
        _placingRailSegment.StartCoroutine(UpdatePlaceingRailSegment());
    }

    private static IEnumerator UpdatePlaceingRailSegment()
    {
        while (_active)
        {
            yield return null;
            _placingRailSegment.SplineSegment.Modify(null, PlayerMovment.Instance.GetMousePositionInWorld().point, null);
            
            MonoBehaviour.print("tick");
            

        }
    }

}
