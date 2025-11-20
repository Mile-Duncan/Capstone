using System.Collections;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Object = UnityEngine.Object;

public static class SemaphorePlacer
{
    private const float OFFSET = 2f;
    public static bool IsPlacing{get; private set;}
    
    
    private static GameObject SemaphorePrefab = Resources.Load<GameObject>("Prefabs/Semaphore");
    private static Semaphore CurrentPlacingSemaphore;
    private static RailSegment CurrentTrack;
    private static bool IsAt1;
    public static Vector3? GetSemaphorePlacementPosition(Vector3 mousePosition, out bool at1, out Quaternion rotation)
    {
        float minRange = float.MaxValue;
        at1 = false;
        rotation = Quaternion.identity;

        Vector3? bestPosition = null;
        foreach (RailSegment segment in RailNetwork.Track)
        {

            float3 up;
            float3 tan0;
            float3 tan1;
            float3 pos;
            
            SplineUtility.Evaluate(segment.SplineSegment.PlaceableSpline, 0.01f, out pos, out tan0, out up);
            Vector3 placementPosition0 = (Vector3)pos + Vector3.Cross(tan0, up).normalized * -OFFSET;
            
            SplineUtility.Evaluate(segment.SplineSegment.PlaceableSpline, 0.99f, out pos, out tan1, out up);
            Vector3 placementPosition1 = (Vector3)pos + Vector3.Cross(tan1, up).normalized * OFFSET;
            
            
            if (Vector3.Distance(placementPosition0, mousePosition) <= minRange)
            { 
                bestPosition = placementPosition0;
                minRange = Vector3.Distance(placementPosition0, mousePosition);
                at1 = false;
                rotation = Quaternion.LookRotation(tan0, Vector3.up);
                CurrentTrack = segment;
                IsAt1 = false;
            };
            if (Vector3.Distance(placementPosition1, mousePosition) <= minRange)
            { 
                bestPosition = placementPosition1;
                minRange = Vector3.Distance(placementPosition1, mousePosition);
                at1 = true;
                rotation = Quaternion.LookRotation(-tan1, Vector3.up);
                CurrentTrack = segment;
                IsAt1 = true;
            };
        }
        return bestPosition;
    }

    public static void IsPlacementSequenceActive(bool isPlacing = false)
    {
        IsPlacing = isPlacing;

        if (!IsPlacing)
        {
            Object.Destroy(CurrentPlacingSemaphore.gameObject);
            return;
        }
        
        CurrentPlacingSemaphore = Object.Instantiate(SemaphorePrefab).GetComponent<Semaphore>();

        CurrentPlacingSemaphore.StartCoroutine(UpdatePlaceingSemaphore());


    }
    
    private static IEnumerator UpdatePlaceingSemaphore()
    {
        bool placeAt1;
        Quaternion rotation;
        while (IsPlacing)
        {
            yield return null;
            Vector3? placePosition = GetSemaphorePlacementPosition(PlayerMovment.Instance.GetMousePositionInWorld().point,out placeAt1, out rotation);
            if(placePosition == null || CurrentPlacingSemaphore == null)continue;
            CurrentPlacingSemaphore.transform.position = placePosition.Value;
            CurrentPlacingSemaphore.transform.position += Vector3.up * 2;
            CurrentPlacingSemaphore.transform.rotation = rotation;
            CurrentPlacingSemaphore.transform.Rotate(0,90,0);
            CheckValidity();
        }
    }

    public static void PlaceSemaphore()
    {
        if (!CheckValidity()) return;
        
        CurrentPlacingSemaphore.GetComponent<MeshRenderer>().material.color = Color.white;
        CurrentPlacingSemaphore.gameObject.transform.localScale *= 0.95f;
        CurrentTrack.trackSemaphores[Convert.ToByte(IsAt1)] = CurrentPlacingSemaphore;
        CurrentPlacingSemaphore.Init(CurrentTrack,IsAt1);
        CurrentPlacingSemaphore = Object.Instantiate(SemaphorePrefab).GetComponent<Semaphore>();


        
    }

    public static void RemoveSemaphore()
    {
        if(CurrentTrack.trackSemaphores[Convert.ToByte(IsAt1)] != null) Object.Destroy(CurrentTrack.trackSemaphores[Convert.ToByte(IsAt1)].gameObject);
    }

    private static bool CheckValidity()
    {
        
        CurrentPlacingSemaphore.GetComponent<MeshRenderer>().material.color = Color.red;
        if(CurrentTrack == null)return false;
        if(CurrentTrack.trackSemaphores[Convert.ToByte(IsAt1)] != null)return false;
        
        CurrentPlacingSemaphore.GetComponent<MeshRenderer>().material.color = Color.cyan;

        return true;
    }
}
