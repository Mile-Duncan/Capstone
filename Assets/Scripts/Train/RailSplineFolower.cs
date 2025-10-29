using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Splines;

public class RailSplineFolower : MonoBehaviour
{
    public RailSegment currentSegment;
    public float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (speed != 0) gameObject.GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;
        ConnectToRail();
    }

    private void ConnectToRail()
    {
        List<RailSegment> segmentsToCheck;
        if (currentSegment == null) segmentsToCheck = RailNetwork.Track;
        else segmentsToCheck = new List<RailSegment>{currentSegment,currentSegment.connections[0],currentSegment.connections[1]};

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
        transform.position = currentSegment.splineContainer.EvaluatePosition(minAmount);
        float3 tangent = currentSegment.splineContainer.EvaluateTangent(minAmount);
        if(Vector3.Angle(transform.forward, tangent)<90) transform.forward = tangent;
        else transform.forward = -tangent;

    }
}
