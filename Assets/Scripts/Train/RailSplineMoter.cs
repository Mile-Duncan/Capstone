using System;
using UnityEngine;

public class RailSplineMoter : MonoBehaviour
{
    public enum Direction
    {
        Forward = 1,
        Stuck = 0,
        Reverse = -1
    }
    
    public RailSegment currentSegment;

    [SerializeField] private bool InverseDirection;
    [SerializeField] public float Speed;
    
    private float DistanceThoughCurrentSegment;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Move(Speed);
    }

    private void Move(float amount)
    {
        if(currentSegment == null) return;
        amount += DistanceThoughCurrentSegment;

        while (amount < 0 || amount > currentSegment.splineContainer.CalculateLength())
        {
            if (amount > currentSegment.splineContainer.CalculateLength())
            {
                amount -= currentSegment.splineContainer.CalculateLength();

                if (currentSegment.connections[1].SplineSegment.GetKnots()[1].Equals(currentSegment.SplineSegment.GetKnots()[1]))
                {
                    Speed = -Speed;
                    amount = -amount;
                    currentSegment = currentSegment.connections[1];
                    amount += currentSegment.splineContainer.CalculateLength();
                }
                else
                {
                    currentSegment = currentSegment.connections[1];
                }

            }

            if (amount < 0)
            {
                if (currentSegment.connections[0].SplineSegment.GetKnots()[0].Equals(currentSegment.SplineSegment.GetKnots()[0]))
                {
                    Speed = -Speed;
                    amount = -amount;
                    currentSegment = currentSegment.connections[0];
                    //amount -= currentSegment.splineContainer.CalculateLength();
                }
                else
                {
                    currentSegment = currentSegment.connections[0];
                    amount += currentSegment.splineContainer.CalculateLength();
                }

            }
        }

        transform.position = currentSegment.splineContainer.EvaluatePosition(amount / currentSegment.splineContainer.CalculateLength());
        if(Speed>0)transform.forward = currentSegment.splineContainer.EvaluateTangent(amount / currentSegment.splineContainer.CalculateLength());
        else transform.forward = -currentSegment.splineContainer.EvaluateTangent(amount / currentSegment.splineContainer.CalculateLength());
        DistanceThoughCurrentSegment = amount;
        
    }

    private void MoveTo(RailSegment segment, float percentage)
    {
        segment.splineContainer.EvaluatePosition(percentage);
    }
}
