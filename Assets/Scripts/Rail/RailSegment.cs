using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class RailSegment : MonoBehaviour
{
    public enum SegmentType
    {
        Fucked = -1,
        Detached = 0,
        Buffer = 1,
        Standard = 2,
        Switch = 3,
        Crossover = 4,
        Holographic = 100
    }

    public PlaceableSplineSegment SplineSegment;
    public List<RailSegment> connections;
    public SegmentType segmentType;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private SplineExtrude splineExtrude;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RailNetwork.Track.Add(this);
        splineContainer = gameObject.GetOrAddComponent<SplineContainer>();
        splineExtrude = gameObject.GetOrAddComponent<SplineExtrude>();
        splineExtrude.targetMesh = CreateRailMesh();
        splineExtrude.RebuildFrequency = 1;
    }
    void Start()
    {
        splineContainer.Spline = SplineSegment.PlaceableSpline;
        splineExtrude.Container = splineContainer;
        
        new GameObject("test").AddComponent<MeshFilter>().mesh = CreateRailMesh();
    }

    private Mesh CreateRailMesh()
    {
        Mesh RailMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            // Front face
            new Vector3(-1.25f, -0.5f, -1.0f),  // 0: front bottom-left
            new Vector3(-1.0f,   0.5f, -1.0f),  // 1: front top-left
            new Vector3( 1.0f,   0.5f, -1.0f),  // 2: front top-right
            new Vector3( 1.25f, -0.5f, -1.0f),  // 3: front bottom-right
            
            // Back face
            new Vector3(-1.25f, -0.5f,  1.0f),  // 4: back bottom-left
            new Vector3(-1.0f,   0.5f,  1.0f),  // 5: back top-left
            new Vector3( 1.0f,   0.5f,  1.0f),  // 6: back top-right
            new Vector3( 1.25f, -0.5f,  1.0f)   // 7: back bottom-right
        };

        // Triangles to form the top and side faces
        // The ends and bottom are left open as requested
        int[] triangles = new int[]
        {
            // Top face (trapezoid)
            1, 5, 6,    // First triangle
            1, 6, 2,    // Second triangle

            // Left side face
            0, 4, 5,    // First triangle
            0, 5, 1,    // Second triangle

            // Right side face
            2, 6, 7,    // First triangle
            2, 7, 3     // Second triangle
        };

        RailMesh.vertices = vertices;
        RailMesh.triangles = triangles;
        RailMesh.RecalculateNormals();
        return RailMesh;
    }

    // Update is called once per frame
    void Update()
    {
        splineExtrude.targetMesh = CreateRailMesh();

    }
}
