using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class PlaceableSplineSegment
{
    private const float ControlPointPrecent = 1f;
    public Spline PlaceableSpline { get; private set; }
    public BezierKnot AEnd{ get; private set; }
    public BezierKnot BEnd{ get; private set; }
    public float3 ControlPoint { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public PlaceableSplineSegment(float3 knot1Pos, float3 knot2Pos, Vector3 controlPoint)
    {
        ControlPoint = controlPoint;
        PlaceableSpline = new Spline();
        
        float3 controlPoint1 = GetControlPoint(controlPoint, knot1Pos);
        float3 controlPoint2 = GetControlPoint(controlPoint, knot2Pos);

        
        AEnd = new BezierKnot(knot1Pos, controlPoint1, 0);
        BEnd = new BezierKnot(knot2Pos, controlPoint2, 0);
        PlaceableSpline.Add(AEnd);
        PlaceableSpline.Add(BEnd);
    }
    
    public void Modify(float3? Knot1Position, float3? Knot2Position, float3? ControlPoint)
    {
        if (ControlPoint != null) this.ControlPoint = ControlPoint.Value;
        Knot1Position ??= AEnd.Position;
        Knot2Position ??= BEnd.Position;
        
        float3 controlPoint = (float3)ControlPoint;
        float3 knot1Pos = (float3)Knot1Position;
        float3 knot2Pos = (float3)Knot2Position;
        
        float3 controlPoint1 = GetControlPoint(controlPoint, knot1Pos);
        float3 controlPoint2 = GetControlPoint(controlPoint, knot2Pos);

        Vector3 AEndFacePoint = (this.ControlPoint - knot1Pos);
        
        AEnd = new BezierKnot(knot1Pos, controlPoint1, 0,rotation:Quaternion.LookRotation(AEndFacePoint));
        BEnd = new BezierKnot(knot2Pos, controlPoint2, 0);
        PlaceableSpline[0] = AEnd;
        PlaceableSpline[1] = BEnd;
    }

    private float3 GetControlPoint(float3 controlPoint, float3 knot)
    {
        controlPoint -= knot; //get relative cords
        controlPoint *= ControlPointPrecent; //scale
        return controlPoint; //fuck ya
    }

    public float3[] GetKnots()
    {
        return new float3[2]
        {
            new float3(AEnd.Position.x, AEnd.Position.y, AEnd.Position.z),
            new float3(BEnd.Position.x, BEnd.Position.y, BEnd.Position.z)
        };
    }
}
