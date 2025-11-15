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
    [SerializeField] public SplineMeshExtrude splineExtrude;
    [SerializeField] private MeshFilter splineMeshFilter;
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

        StartCoroutine(UpdateSemaphore());
    }

    private void FixedUpdate()
    {
        
    }

    private IEnumerator UpdateSemaphore()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (isOccupied)
            {
                Semaphore.UpdatePositions(this);
            }
        }
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
    
    // NEW STATIC HELPER FUNCTION (Used by Switch.cs and GetStandPosition)
    public static int GetJunctionIndex(RailSegment segment, RailSegment junctionTrack)
    {
        // We find the index where the segment is connected to the junctionTrack.
        // This is necessary because connections[0] or [1] might be null after a break.
        // We rely on the initial structural placement of the track.
        
        // NOTE: If the connection array is [null, neighbour], the junction is at 0.
        // If it's [neighbour, null], the junction is at 1.
        
        // This relies on the convention that the other end (the non-junction end) 
        // will either be null (if detached) or point to a non-junction track.
        
        if (segment.connections[0] != null && segment.connections[0] != junctionTrack && !segment.connections.Contains(junctionTrack))
        {
            // If connections[0] is set and is NOT the junction track, then connections[1] must be the junction end.
            return 1;
        }
        // Otherwise, connection[0] must be the junction end.
        return 0;
    }

    // NEW FUNCTION: Calculates the position for the switch stand visual element
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

                    // --- Scenario 1: Simple Connection (Standard Track) ---
                    if (existingCount == 0)
                    {
                        segmentA.connections[a] = segmentB;
                        segmentB.connections[b] = segmentA;
                        segmentA.trackUpdateEvent.Invoke();
                        segmentB.trackUpdateEvent.Invoke();
                        return true;
                    }
                    
                    // --- Scenario 2: Switch Creation (One existing track) ---
                    else if (existingCount == 1)
                    {
                        RailSegment trackIn;    
                        RailSegment trackOut1;  
                        RailSegment trackOut2;  
                        
                        if (connA != null) 
                        {
                            trackIn = connA;
                            trackOut1 = segmentA; 
                            trackOut2 = segmentB;
                        }
                        else 
                        {
                            trackIn = connB;
                            trackOut1 = segmentB; 
                            trackOut2 = segmentA;
                        }

                        // 1. Calculate all necessary junction indices
                        int indexOnTrackIn = Array.IndexOf(trackIn.connections, trackOut1); 
                        int trackOut1JunctionIndex = (trackOut1 == segmentA) ? a : b; 
                        int trackOut2JunctionIndex = (trackOut2 == segmentA) ? a : b;
                        
                        // 2. Check for valid index on trackIn and a diverging angle
                        if (indexOnTrackIn != -1 && CheckForConvergingTracks(trackOut1, trackOut1JunctionIndex, trackOut2, trackOut2JunctionIndex))
                        {
                            // 3. FIX: Temporarily set the reciprocal links BEFORE initialization.
                            // This guarantees Switch.InitSwitch can find the indices.
                            trackIn.connections[indexOnTrackIn] = trackOut1;
                            trackOut1.connections[trackOut1JunctionIndex] = trackIn;
                            trackOut2.connections[trackOut2JunctionIndex] = trackIn;

                            // 4. Break the existing direct link on TrackIn
                            trackIn.connections[indexOnTrackIn] = null;

                            // 5. Calculate the stand position on trackOut2
                            Vector3 standPosition = trackOut2.GetStandPosition(trackOut2JunctionIndex);

                            // 6. Create the Switch component using the three required arguments.
                            CreateNewSwitch(trackIn, new []{trackOut1, trackOut2}, indexOnTrackIn, standPosition); 

                            // 7. Final assignment to ensure all three segments are updated
                            trackIn.trackUpdateEvent.Invoke();
                            trackOut1.trackUpdateEvent.Invoke();
                            trackOut2.trackUpdateEvent.Invoke();
                            return true;
                        }
                        return false;
                    }
                    
                    // --- Scenario 3: Already at Max Connections ---
                    else
                    {
                        return false; 
                    }
                }
            }
        }
        return false;
    }

    public static bool CheckForConvergingTracks(RailSegment segmentA, int endIndexA, RailSegment segmentB, int endIndexB)
    {
        const float EPSILON = 0.05f;
        
        float tA = (endIndexA == 0) ? EPSILON : (1.0f - EPSILON); 
        float tB = (endIndexB == 0) ? EPSILON : (1.0f - EPSILON); 
        
        Vector3 tangentA = (Vector3)segmentA.splineContainer.Spline.EvaluateTangent(tA);
        Vector3 tangentB = (Vector3)segmentB.splineContainer.Spline.EvaluateTangent(tB);

        if (tangentA.sqrMagnitude < 0.0001f || tangentB.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float angle = Vector3.Angle(tangentA.normalized, tangentB.normalized);

        const float MIN_CONVERGENCE_ANGLE = 5.0f;  
        const float MAX_CONVERGENCE_ANGLE = 175.0f; 

        return angle > MIN_CONVERGENCE_ANGLE && angle < MAX_CONVERGENCE_ANGLE;
    }
    
    // UPDATED SIGNATURE
    public static void CreateNewSwitch(RailSegment trackIn, RailSegment[] tracksOut, int indexOnTrackIn, Vector3 standPosition)
    {
        Switch trackSwitch = trackIn.gameObject.AddComponent<Switch>(); 
    
        // 1. Set the switch reference
        if (trackIn.switches[indexOnTrackIn] != null)
        {
            MonoBehaviour.Destroy(trackIn.switches[indexOnTrackIn]);
        }
        trackIn.switches[indexOnTrackIn] = trackSwitch;
        
        // 2. Set the position of the switch stand (visual element)
        // Note: The Switch component itself is placed on the TrackIn object.
        
        GameObject switchStand = MonoBehaviour.Instantiate(Resources.Load<GameObject>("Prefabs/SwitchBox"), trackSwitch.transform);
        switchStand.transform.position = standPosition;
        switchStand.transform.localScale *= 3;
        switchStand.transform.rotation = Quaternion.Euler(SplineUtility.EvaluateTangent(trackIn.SplineSegment.PlaceableSpline,indexOnTrackIn));

        // 3. Initialize the switch
        trackSwitch.InitSwitch(trackIn, tracksOut);
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
}