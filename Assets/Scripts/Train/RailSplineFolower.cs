using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Splines;

public class RailSplineFolower : MonoBehaviour
{
    public enum Direction { Positive = 1, Negative = -1, Stopped = 0}
    
    public RailSegment currentSegment;
    public RailSegment nextSegment;
    public float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (speed == 0)
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        else
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
            gameObject.GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;

        }
        ConnectToRail();
    }

    private void ConnectToRail()
    {
        List<RailSegment> segmentsToCheck;
        if (currentSegment == null) segmentsToCheck = RailNetwork.Track;
        else
        {
            segmentsToCheck = new List<RailSegment>{currentSegment,currentSegment.connections[0],currentSegment.connections[1]};
            currentSegment.isOccupied = false;
        }

        
        float minDistance = float.MaxValue;
        float minAmount = 0;
        RailSegment minSegment = null;

        float3 point;
        float amount;
        foreach (RailSegment segment in segmentsToCheck)
        {
            if(segment==null|| segment.segmentType < 0) continue;
            SplineUtility.GetNearestPoint(segment.SplineSegment.PlaceableSpline, transform.position, out point, out amount,6,3);
            float dist = Vector3.SqrMagnitude(transform.position - (Vector3)point);
            if (dist < minDistance)
            {
                minDistance = dist;
                minAmount = amount;
                minSegment = segment;
            }
        }
        
        if (minDistance > 10000000) return;

        currentSegment = minSegment;
        Vector3 currentVelocity = gameObject.GetComponent<Rigidbody>().linearVelocity;
        float3 tangent = currentSegment.splineContainer.EvaluateTangent(minAmount);
        
        if(currentSegment== null)return;
        
        segmentsToCheck = new List<RailSegment>{currentSegment.connections[0],currentSegment,currentSegment.connections[1]};
        
        if(nextSegment==null) nextSegment = segmentsToCheck[0];;

        if (currentVelocity.sqrMagnitude < 0.001f)
        {
            currentSegment.direction = Direction.Stopped;
        }
        else
        {
            float dotProduct = Vector3.Dot(currentVelocity.normalized, ((Vector3)tangent).normalized);

            if (dotProduct > 0)
            {
                currentSegment.direction = Direction.Positive;
                nextSegment = segmentsToCheck[2];

            }
            else
            {
                currentSegment.direction = Direction.Negative;
                nextSegment = segmentsToCheck[0];
            }
            
        }

        currentSegment.isOccupied = true;
        
        if(!RailNetwork.UnsetOccupiedTrack.Contains(currentSegment))RailNetwork.UnsetOccupiedTrack.Add(currentSegment);
        
        transform.position = currentSegment.splineContainer.EvaluatePosition(minAmount);
        if(Vector3.Angle(transform.forward, tangent)<90) transform.forward = tangent;
        else transform.forward = -tangent;

    }
}
