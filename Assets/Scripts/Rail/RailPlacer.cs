using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public static class RailPlacer
{
    private const float SnapRange = 3f;
    private const float MinLength = 5f;
    private const float MaxLength = 50f;

    
    private static bool _active = false;
    private static bool _isValid = false;
    
    private static Vector3 _firstPosition;
    private static RailSegment _placingRailSegment;
    private static Vector3 _controlPointPosition;
    private static Vector3 _currentAnchorTangent;
    
    public static Vector3 GetTrackPlacementPosition(Vector3 originalPlacePosition, bool firstClick = false)
    {
        foreach (RailSegment segment in RailNetwork.Track)
        {
            if (segment == _placingRailSegment) continue;
            if (Vector3.Distance(segment.SplineSegment.GetKnots()[0], originalPlacePosition) <= SnapRange)
            {
                Vector3 clickPosition = segment.SplineSegment.GetKnots()[0];
                if (firstClick)
                {
                    _currentAnchorTangent = segment.SplineSegment.ControlPoint;
                    _placingRailSegment.SplineSegment = new PlaceableSplineSegment(clickPosition, clickPosition, _controlPointPosition);
                }
                ConectSegment(segment);

                return clickPosition;
            };
            if (Vector3.Distance(segment.SplineSegment.GetKnots()[1], originalPlacePosition) <= SnapRange)
            {
                Vector3 clickPosition = segment.SplineSegment.GetKnots()[1];
                if (firstClick)
                {
                    _currentAnchorTangent = segment.SplineSegment.ControlPoint;
                    _placingRailSegment.SplineSegment = new PlaceableSplineSegment(clickPosition, clickPosition, _controlPointPosition);
                }
                ConectSegment(segment);

                return clickPosition;
            };
        }

        if (firstClick)
        {
            _currentAnchorTangent = new Vector3(originalPlacePosition.x, originalPlacePosition.y, originalPlacePosition.z);
            _placingRailSegment.SplineSegment = new PlaceableSplineSegment(originalPlacePosition, originalPlacePosition, _controlPointPosition);
        }
        
        return originalPlacePosition;
    }

    private static void ConectSegment(RailSegment segment)
    {
        if (segment.SplineSegment.AEnd.Position.Equals(_placingRailSegment.SplineSegment.AEnd.Position))
        {
            _placingRailSegment.connections[0] = segment;
            segment.connections[0] = _placingRailSegment;
        }else if (segment.SplineSegment.AEnd.Position.Equals(_placingRailSegment.SplineSegment.BEnd.Position))
        {
            _placingRailSegment.connections[1] = segment;
            segment.connections[0] = _placingRailSegment;
        }else if (segment.SplineSegment.BEnd.Position.Equals(_placingRailSegment.SplineSegment.AEnd.Position))
        {
            _placingRailSegment.connections[0] = segment;
            segment.connections[1] = _placingRailSegment;
        }else if (segment.SplineSegment.BEnd.Position.Equals(_placingRailSegment.SplineSegment.BEnd.Position))
        {
            _placingRailSegment.connections[1] = segment;
            segment.connections[1] = _placingRailSegment;
        }
    }

    public static void TogglePlacementSequence(Vector3 clickPosition)
    {
        
        if (_active)
        {
            if(!_isValid)return;
            
            _placingRailSegment.splineMeshRenderer.material.SetColor("_Color", Color.white);
            
            clickPosition = GetTrackPlacementPosition(clickPosition);

            _active = false;
            if (_firstPosition == _controlPointPosition || clickPosition == _controlPointPosition) _controlPointPosition = (clickPosition + _firstPosition) / 2f;
            _placingRailSegment = null;
            return;
        }

        _isValid = false;

        _placingRailSegment = new GameObject("Rail Segment: " + RailNetwork.Track.Count).AddComponent<RailSegment>();

        clickPosition = GetTrackPlacementPosition(clickPosition, true);
        
        _active = true;
        _firstPosition = clickPosition;
        
        SetControlPointPosition(clickPosition,clickPosition);

        _placingRailSegment.segmentType = RailSegment.SegmentType.Holographic;
        
        _placingRailSegment.StartCoroutine(UpdatePlaceingRailSegment());
    }

    private static IEnumerator UpdatePlaceingRailSegment()
    {
        while (_active)
        {
            Vector3 mousePosition = GetTrackPlacementPosition(PlayerMovment.Instance.GetMousePositionInWorld().point);
            SetControlPointPosition(mousePosition, _firstPosition);
            _placingRailSegment.SplineSegment.Modify(null, mousePosition, _controlPointPosition);
            _placingRailSegment.splineExtrude.UpdateMesh();
            _isValid = ValidatePlacement();
            if (_isValid) _placingRailSegment.splineMeshRenderer.material.SetColor("_Color", Color.cyan);
            else _placingRailSegment.splineMeshRenderer.material.SetColor("_Color", Color.red);
            yield return null;
        }
    }

    private static void SetControlPointPosition(Vector3 mousePosition, Vector3 anchorPosition)
    {
        TryFindEquidistantPoint(_currentAnchorTangent, mousePosition, anchorPosition, out _controlPointPosition);
        _controlPointPosition.y = (_controlPointPosition.y+mousePosition.y)/2;
    }
    
    
    //Math
    public static bool TryFindEquidistantPoint(Vector3 A, Vector3 C, Vector3 B, out Vector3 D)
    {
        const float Epsilon = 1e-6f;
        
        Vector3 V = A - B;
        Vector3 R = C - B;
        
        float V_dot_R = Vector3.Dot(V, R);
        
        if (V.sqrMagnitude < Epsilon)
        {
            D = B;
            //return true;
        }
        
        if (Mathf.Abs(V_dot_R) < Epsilon)
        {
            if (R.sqrMagnitude < Epsilon)
            {
                D = B;
                return true;
            }
            D = (B+C)/2f;
            return false;
        }

       
        float numerator = R.sqrMagnitude;
        float denominator = 2.0f * V_dot_R;

        float t = numerator / denominator;
        
        D = B + t * V;
        
        return true;
    }
    
    private static bool ValidatePlacement()
    {
        if (_placingRailSegment.SplineSegment.PlaceableSpline.GetLength() < MinLength || _placingRailSegment.SplineSegment.PlaceableSpline.GetLength() > MaxLength) return false;
        //if(Quaternion.Angle(_placingRailSegment.SplineSegment.AEnd.Rotation,_placingRailSegment.SplineSegment.BEnd.Rotation) > 90f) return false;
        return true;
    }

}
