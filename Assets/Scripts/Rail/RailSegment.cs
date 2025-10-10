using System;
using System.Collections.Generic;
using Unity.Mathematics;
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
    [SerializeField] public SplineMeshExtrude splineExtrude;
    [SerializeField] private MeshFilter splineMeshFilter;
    [SerializeField] public MeshRenderer splineMeshRenderer;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RailNetwork.Track.Add(this);

        connections = new List<RailSegment>();
        
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
        splineExtrude.extrusionInterval = 0.1f;
        splineMeshRenderer.material = Resorces.Materials["Gravel"];

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere((Vector3)SplineSegment.ControlPoint, 1);
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
        
        const float UV_LENGTH_SCALE = 20.0f; // Controls how many times the texture repeats along the rail length (Z axis). Set to 1.0f for no repetition.
        Vector2[] uv = new Vector2[]
        {
            // Front face (V = 0.0 * scale)
            // U = 0.0 (( -1.25 + 1.25) / 2.5)
            new Vector2(0.0f, 0.0f * UV_LENGTH_SCALE),    // 0
            // U = 0.1 (( -1.0 + 1.25) / 2.5)
            new Vector2(0.1f, 0.0f * UV_LENGTH_SCALE),    // 1
            // U = 0.9 (( 1.0 + 1.25) / 2.5)
            new Vector2(0.9f, 0.0f * UV_LENGTH_SCALE),    // 2
            // U = 1.0 (( 1.25 + 1.25) / 2.5)
            new Vector2(1.0f, 0.0f * UV_LENGTH_SCALE),    // 3

            // Back face (V = 1.0 * scale)
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
    
    private Mesh CreateRailMesh2()
    {
        Mesh RailMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            // Front face
            new Vector3(-1.25f, -0.5f, -1.0f),  // 0: front bottom-left
            new Vector3(-1.0f,   0.5f, -1.0f),  // 1: front top-left
            new Vector3( 1.0f,   0.5f, -1.0f),  // 2: front top-right
            new Vector3( 1.25f, -0.5f, -1.0f),  // 3: front bottom-right
        };

        RailMesh.vertices = vertices;
        RailMesh.RecalculateNormals();
        return RailMesh;
    }

}
