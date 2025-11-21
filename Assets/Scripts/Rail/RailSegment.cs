using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Added for Array.IndexOf
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Object = System.Object;

public class RailSegment : MonoBehaviour
{
    public enum SegmentType
    {
        Fucked = -100,
        Detached = 0,
        Buffer = 1,
        Standard = 2,
        Switch = 3,
        Crossover = 4,
        Holographic = -1
    }

    public PlaceableSplineSegment SplineSegment;
    public RailSegment[] connections = new RailSegment[2];
    public Switch[] switches = new Switch[2];
    public SegmentType segmentType;
    
    public Semaphore[] trackSemaphores = new Semaphore[2];
    public RailSplineFolower.Direction direction;
    public bool isOccupied;
    public Semaphore.State trackCircuit;
    [SerializeField] public SplineContainer splineContainer { get; private set; }
    [SerializeField] public MeshFilter splineMeshFilter;
    [SerializeField] public SplineMeshExtrude splineExtrude;
    [SerializeField] public MeshRenderer splineMeshRenderer;

    public UnityEvent trackUpdateEvent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RailNetwork.RegisterNewTrack(this);
        
        splineContainer = gameObject.GetOrAddComponent<SplineContainer>();
        splineMeshFilter = gameObject.GetOrAddComponent<MeshFilter>();
        splineExtrude = gameObject.GetOrAddComponent<SplineMeshExtrude>();
        splineMeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();

    }
    void Start()
    {
        splineContainer.Spline = SplineSegment.PlaceableSpline;
        splineExtrude.extrusionTemplateMesh = CreateRailMesh();
        splineExtrude.extrusionAxis = SplineMeshExtrude.Axis.Z;
        splineExtrude.extrusionInterval = 1f;
        splineMeshRenderer.material = Resorces.Materials["Gravel"];
        
        trackUpdateEvent = new UnityEvent();
        trackUpdateEvent.AddListener(OnTrackUpdateEvent);

    }
    

    private void OnTrackUpdateEvent()
    {
        UpdateSegmentType();
        
    }

    public SegmentType UpdateSegmentType()
    {
        byte cons = 0;
        if(connections[0] != null) cons++;
        if(connections[1] != null) cons++;
        
        // Check for Switch component presence
        bool hasSwitch = (switches[0] != null || switches[1] != null);

        if (hasSwitch)
        {
            if (switches[0] != null && switches[1] != null)
            {
                segmentType = SegmentType.Crossover;
            }
            else
            {
                segmentType = SegmentType.Switch;
            }
        }
        else if(cons == 0) 
        {
            segmentType = SegmentType.Detached;
        }
        else if(cons == 1) 
        {
            segmentType = SegmentType.Buffer;
        }
        else if(cons == 2) 
        {
            segmentType = SegmentType.Standard;
        }
        else 
        {
            segmentType = SegmentType.Fucked; 
        }
        
        return segmentType;
    }

    public void SetConnections()
    {
        List<RailSegment> segments = RailNetwork.GetSegmentsWithNodeAt(SplineSegment.AEnd.Position, new []{this});
        if (segments.Count>0) ConectSegments(this,segments[0]);
        segments = RailNetwork.GetSegmentsWithNodeAt(SplineSegment.BEnd.Position, new []{this});
        if (segments.Count>0) ConectSegments(this,segments[0]);

    }
    
    public static int GetJunctionIndex(RailSegment segment, RailSegment junctionTrack)
    {
        RailSegment conectedTrack;
        segment.GetConnection(false, out conectedTrack, out _);
            
        if (junctionTrack.SplineSegment.GetKnots().Contains(segment.SplineSegment.AEnd.Position))
        {
            return 0;
        }else if (junctionTrack.SplineSegment.GetKnots().Contains(segment.SplineSegment.BEnd.Position))
        {
            return 1;
        }
        else
        {
            throw new Exception("Invalid junction state");
        }
    }


    
    public Vector3 GetStandPosition(int junctionIndex)
    {
        // Distance from the spline to place the switch stand
        const float OFFSET_DISTANCE = 3.0f; 
        // Small offset for sampling tangent near knot
        const float EPSILON = 0.05f;

        // 1. Determine t-value for position and tangent calculation
        float t_pos = (junctionIndex == 0) ? 0f : 1f;
        float t_sample = (junctionIndex == 0) ? EPSILON : (1.0f - EPSILON);

        // 2. Get the position of the junction
        Vector3 junctionPosition = (Vector3)splineContainer.Spline.EvaluatePosition(t_pos);

        // 3. Get the direction vector (tangent) pointing *away* from the junction
        Vector3 tangent = ((Vector3)splineContainer.Spline.EvaluateTangent(t_sample)).normalized;

        // 4. Calculate the perpendicular offset direction (90 degrees rotation around Vector3.up)
        // This gives us the direction to place the stand beside the track.
        Vector3 offsetDirection = Vector3.Cross(tangent, Vector3.up).normalized;

        // 5. Return the final position, offset and lifted slightly
        return junctionPosition + offsetDirection * OFFSET_DISTANCE + Vector3.up * 0.5f;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(SplineSegment.ControlPoint, 1);
    }

    private Mesh CreateRailMesh()
    {
        float baseWidth = 2f;
        float height = 0.75f;
        Mesh RailMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            // Front face
            new Vector3(-baseWidth, -height, -1.0f),  // 0: front bottom-left
            new Vector3(-1.0f,   height, -1.0f),  // 1: front top-left
            new Vector3( 1.0f,   height, -1.0f),  // 2: front top-right
            new Vector3( baseWidth, -height, -1.0f),  // 3: front bottom-right
            
            // Back face
            new Vector3(-baseWidth, -height,  1.0f),  // 4: back bottom-left
            new Vector3(-1.0f,   height,  1.0f),  // 5: back top-left
            new Vector3( 1.0f,   height,  1.0f),  // 6: back top-right
            new Vector3( baseWidth, -height,  1.0f)   // 7: back bottom-right
        };

        // Triangles to form the top and side faces
        int[] triangles = new int[]
        {
            // Top face (trapezoid)
            1, 5, 6,    
            1, 6, 2,    

            // Left side face
            0, 4, 5,    
            0, 5, 1,    

            // Right side face
            2, 6, 7,    
            2, 7, 3     
        };
        
        const float UV_LENGTH_SCALE = 20.0f; 
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0.0f, 0.0f * UV_LENGTH_SCALE),    // 0
            new Vector2(0.1f, 0.0f * UV_LENGTH_SCALE),    // 1
            new Vector2(0.9f, 0.0f * UV_LENGTH_SCALE),    // 2
            new Vector2(1.0f, 0.0f * UV_LENGTH_SCALE),    // 3

            new Vector2(0.0f, 1.0f * UV_LENGTH_SCALE),    // 4
            new Vector2(0.1f, 1.0f * UV_LENGTH_SCALE),    // 5
            new Vector2(0.9f, 1.0f * UV_LENGTH_SCALE),    // 6
            new Vector2(1.0f, 1.0f * UV_LENGTH_SCALE)     // 7
        };

        RailMesh.vertices = vertices;
        RailMesh.triangles = triangles;
        RailMesh.uv = uv;
        RailMesh.RecalculateNormals();
        return RailMesh;
    }
    
    public static bool ConectSegments(RailSegment segmentA, RailSegment segmentB)
{
    Vector3[] Aends = {segmentA.SplineSegment.AEnd.Position, segmentA.SplineSegment.BEnd.Position};
    Vector3[] Bends = {segmentB.SplineSegment.AEnd.Position, segmentB.SplineSegment.BEnd.Position};
    
    for(int a = 0; a < Aends.Length; a++) 
    {
        for (int b = 0; b < Bends.Length; b++)
        {
            if (Aends[a].Equals(Bends[b]))
            {
                RailSegment connA = segmentA.connections[a];
                RailSegment connB = segmentB.connections[b];
                int existingCount = (connA != null ? 1 : 0) + (connB != null ? 1 : 0);

                // --- Scenario 1: Simple Connection (Unchanged) ---
                if (existingCount == 0)
                {
                    segmentA.connections[a] = segmentB;
                    segmentB.connections[b] = segmentA;
                    segmentA.trackUpdateEvent.Invoke();
                    segmentB.trackUpdateEvent.Invoke();
                    return true;
                }
                
                // --- Scenario 2: Switch Creation (Three segments meet) ---
                if (existingCount == 1)
                {
                    // 1. Identify the three segments meeting at the junction
                    RailSegment T_junc;       // segmentA or segmentB (the one at the junction)
                    RailSegment T_exist;      // connA or connB (the existing segment)
                    RailSegment T_new;        // the other of segmentA/segmentB (the new segment)
                    
                    int T_junc_Index; // Index (a or b) on T_junc

                    if (connA != null) 
                    {
                        T_junc = segmentA; T_junc_Index = a; T_exist = connA; T_new = segmentB;
                    }
                    else 
                    {
                        T_junc = segmentB; T_junc_Index = b; T_exist = connB; T_new = segmentA;
                    }

                    // 2. Perform safety check on T_junc and T_new using the original connection indices
                    // We must ensure the angle is wide enough to be a switch.
                    //if (!CheckForConvergingTracks(T_junc, T_junc_Index, T_new, (T_new == segmentA ? a : b))) return false;

                    // 3. CREATE THE SWITCH: Pass all three segments to the static helper.
                    // The Switch class will handle role determination and initialization.
                    if (!Switch.CreateSwitchFromSegments(T_exist, T_junc, T_new)) return false;

                    // 4. Update events (The Switch class should handle its own updates, 
                    // but these ensure surrounding tracks update their type/state).
                    T_exist.trackUpdateEvent.Invoke();
                    T_junc.trackUpdateEvent.Invoke();
                    T_new.trackUpdateEvent.Invoke();
                    return true;
                }
                return false;
            }
        }
    }
    return false;
}
    

    public static bool CheckForConvergingTracks(RailSegment segmentA, int endIndexA, RailSegment segmentB, int endIndexB)
    {
        // The previous implementation likely failed because it relied on Spline.EvaluateTangent(t)
        // without knowing if t was closer to 0 or 1, resulting in reversed or inconsistent vectors.
        // We now use the reliable GetOutwardTangent method.
    
        // 1. Get the Outward Tangent for both segments.
        // These vectors (V_A and V_B) start at the junction and point away from it.
        Vector3 V_A = segmentA.GetOutwardTangent(endIndexA);
        Vector3 V_B = segmentB.GetOutwardTangent(endIndexB);

        // 2. Calculate the angle between the two outward vectors.
        float angle = Vector3.Angle(V_A, V_B);

        // 3. Define acceptable angle bounds for a switch.
        // If the angle is too small (near 0), it's a straight line/simple connection.
        // If the angle is too large (near 180), it's likely a simple connection or poorly formed tracks.
    
        // Tweak these constants based on how sharp you allow your switches to be.
        const float MIN_DIVERGENCE_ANGLE = 5.0f;  // Minimum angle for a switch to be valid (e.g., 5 degrees)
        const float MAX_DIVERGENCE_ANGLE = 175.0f; // Maximum angle (180 is a straight line)

        // Check if the angle is within the valid range for a diverging switch.
        return angle > MIN_DIVERGENCE_ANGLE && angle < MAX_DIVERGENCE_ANGLE;
    }
    
    

    private void OnDestroy()
    {
        if (connections[0] != null)connections[0].connections[Array.IndexOf(connections[0].connections, this)] = null;
        if (connections[1] != null)connections[1].connections[Array.IndexOf(connections[1].connections, this)] = null;
        
        if(switches[0] != null)MonoBehaviour.Destroy(switches[0]);
        if(switches[1] != null)MonoBehaviour.Destroy(switches[1]);
        


    }

    public void GetConnection(bool atEnd1, out RailSegment conectedTrack, out bool isConectedAtEnd1)
    {
        conectedTrack = connections[Convert.ToByte(atEnd1)];

        if (connections[Convert.ToByte(atEnd1)]?.connections[1] == this)
        {
            isConectedAtEnd1 = true;
            return;
        }
        isConectedAtEnd1 = false;
    }
    
    // In RailSegment.cs: Ensure this method is public

    public Vector3 GetOutwardTangent(int junctionIndex, float sampleOffset = 0.01f)
    {
        // Determine the sample t-value: 0.01 if index=0, 0.99 if index=1.
        float t_sample = (junctionIndex == 0) ? sampleOffset : (1.0f - sampleOffset);
    
        // Evaluate the spline's default tangent (T, direction 0 -> 1)
        Vector3 tangent_raw = (Vector3)splineContainer.Spline.EvaluateTangent(t_sample);

        // If junctionIndex is 1, the raw tangent points INWARD. We must reverse it.
        if (junctionIndex == 1)
        {
            return -tangent_raw.normalized;
        }
        // If junctionIndex is 0, the raw tangent points OUTWARD.
        return tangent_raw.normalized;
    }
}