using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMeshExtrude : MonoBehaviour
{
    public enum Axis
    {
        X, Y, Z
    }

    [SerializeField]
    public Mesh extrusionTemplateMesh;
    [SerializeField]
    public Axis extrusionAxis;
    [SerializeField]
    public float extrusionInterval = 10f;
    [SerializeField]
    public float TilingDistance = 1.0f; // Real-world distance (units) for one texture repeat (V=1).
    [SerializeField]
    private bool smoothFaces = true;
    [SerializeField]
    private bool useWorldUp = true;

    private MeshFilter meshFilter;
    private SplineContainer splineContainer;
    private Spline spline;

    private Vector3[] templateVertices;
    
    // A safe upper limit for UV coordinates to prevent rendering API/GPU overflow or precision errors.
    // Coordinates exceeding this value can cause rendering glitches on long meshes.
    private const float MAX_SAFE_UV_MAGNITUDE = 1000.0f; 

    private void Awake()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (!meshFilter)
            Debug.LogError($"SplineMeshExtrude: Awake: Gameobject {gameObject.name} does not have an attached mesh filter.");

        splineContainer = gameObject.GetOrAddComponent<SplineContainer>();
        spline = splineContainer.Spline;

        Mesh generatedMesh = GenerateMesh(new Mesh());
        meshFilter.mesh = generatedMesh;
    }

    public void UpdateMesh()
    {
        spline = splineContainer.Spline;
        GenerateMesh(meshFilter.mesh);
    }

    private Mesh GenerateMesh(Mesh mesh)
    {
        mesh.Clear();
        bool success = SplineUtil.SampleSplineInterval(spline, transform, extrusionInterval, 
                                                       out Vector3[] positions, out Vector3[] tangents, out Vector3[] upVectors);
        if (!success)
        {
            //Debug.LogError("SplineMeshExtrude: GenerateMesh: Error encountered when sampling spline. Aborting");
            return mesh;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        // distinguish verticies from first and second edges
        (int[] firstEdge, int[] secondEdge) = GetEdgeIndicies(extrusionTemplateMesh.vertices, extrusionAxis);

        templateVertices = CollapsePointsOnAxis(extrusionTemplateMesh.vertices, extrusionAxis);

        for (int i = 0; i < positions.Length - 1; i++)
        {
            // PASS THE SEGMENT INDEX (i) TO APPENDMESHSEGMENT
            AppendMeshSegment(vertices, triangles, normals, uvs,
                              positions[i], tangents[i], upVectors[i], positions[i + 1], tangents[i + 1], upVectors[i + 1],
                              firstEdge, secondEdge, i); // <-- i is the segment index
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();

        return mesh;
    }

    private void AppendMeshSegment(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs,
        Vector3 firstPos, Vector3 firstTangent, Vector3 firstUp, Vector3 secondPos, Vector3 secondTangent, Vector3 secondUp, 
        int[] firstEdgeIndicies, int[] secondEdgeIndicies, int segmentIndex) // <-- ADD segmentIndex
    {
        Vector3[] newVertices = new Vector3[templateVertices.Length];
        Vector3[] newNormals = new Vector3[extrusionTemplateMesh.normals.Length];

        // --- Rotation Calculation for first end of segment (firstPos) ---
        Vector3 safeTangent1 = firstTangent;
        
        // Fallback logic: If the sampled tangent is near zero, use the segment direction as a reliable tangent.
        if (safeTangent1.sqrMagnitude < Mathf.Epsilon)
        {
            Vector3 fallbackDirection = (secondPos - firstPos).normalized;
            if (fallbackDirection.sqrMagnitude > Mathf.Epsilon)
            {
                safeTangent1 = fallbackDirection;
            }
        }

        Vector3 forwardVec1 = useWorldUp ? new Vector3(safeTangent1.x, 0, safeTangent1.z) : safeTangent1;
        Quaternion rotation1 = Quaternion.identity;

        if (forwardVec1.sqrMagnitude > Mathf.Epsilon)
        {
            rotation1 = useWorldUp ? Quaternion.LookRotation(forwardVec1, Vector3.up) : 
                                       Quaternion.LookRotation(safeTangent1, firstUp);
        }
        else
        {
            // If we still can't find a rotation (e.g., points overlap or highly vertical spline + worldUp), warn.
            Debug.LogWarning($"SplineMeshExtrude: Zero or near-zero forward vector detected for segment {segmentIndex} (Start). Using Quaternion.identity.");
        }
        
        // Use rotation1 for the first end's vertices and normal calculation
        Quaternion rotation = rotation1;

        Quaternion flatRotation = Quaternion.identity;
        if (!smoothFaces)
        {
            // --- Flat Rotation Calculation ---
            Vector3 avgTangentDir = Vector3.zero;
            Vector3 avgUpDir = Vector3.up;

            if (useWorldUp)
            {
                // Use original tangents for averaging the forward direction
                avgTangentDir = new Vector3(firstTangent.x + secondTangent.x, 0, firstTangent.z + secondTangent.z);
            }
            else
            {
                avgTangentDir = firstTangent + secondTangent;
                avgUpDir = firstUp + secondUp;
            }

            if (avgTangentDir.sqrMagnitude > Mathf.Epsilon)
            {
                flatRotation = Quaternion.LookRotation(avgTangentDir, avgUpDir.normalized);
            }
            else
            {
                Debug.LogWarning($"SplineMeshExtrude: Zero or near-zero average forward vector detected for segment {segmentIndex}. Flat rotation failed. Using Quaternion.identity.");
            }
        }
        Quaternion normalRotation = smoothFaces ? rotation : flatRotation;

        foreach (int index in firstEdgeIndicies)
        {
            // transform verticies and normals to match firstPos
            newVertices[index] = (rotation1 * templateVertices[index]) + firstPos;
            newNormals[index] = normalRotation * extrusionTemplateMesh.normals[index];
        }

        // --- Rotation Calculation for second end of segment (secondPos) ---
        Vector3 safeTangent2 = secondTangent;
        
        // Fallback logic for the end of the segment as well
        if (safeTangent2.sqrMagnitude < Mathf.Epsilon)
        {
            Vector3 fallbackDirection = (secondPos - firstPos).normalized;
            if (fallbackDirection.sqrMagnitude > Mathf.Epsilon)
            {
                // Using the segment direction ensures a valid rotation even if the sampled tangent is zero.
                safeTangent2 = fallbackDirection; 
            }
        }

        Vector3 forwardVec2 = useWorldUp ? new Vector3(safeTangent2.x, 0, safeTangent2.z) : safeTangent2;
        Quaternion rotation2 = Quaternion.identity;
        
        if (forwardVec2.sqrMagnitude > Mathf.Epsilon)
        {
            rotation2 = useWorldUp ? Quaternion.LookRotation(forwardVec2, Vector3.up) :
                                Quaternion.LookRotation(safeTangent2, secondUp);
        }
        else
        {
            Debug.LogWarning($"SplineMeshExtrude: Zero or near-zero forward vector detected for segment {segmentIndex} (End). Using Quaternion.identity.");
        }
        
        rotation = rotation2; 
        normalRotation = smoothFaces ? rotation2 : flatRotation;
        
        foreach (int index in secondEdgeIndicies)
        {
            // transform verticies and normals to match secondPos
            newVertices[index] = (rotation2 * templateVertices[index]) + secondPos;
            newNormals[index] = normalRotation * extrusionTemplateMesh.normals[index];
        }

        int prevVerticiesLength = vertices.Count;

        vertices.AddRange(newVertices);
        triangles.AddRange(extrusionTemplateMesh.triangles.Select(index => index + prevVerticiesLength));
        normals.AddRange(newNormals);
        
        // --- UV Connection Fix: Offset the V coordinate based on distance and TilingDistance ---
        
        // 1. Determine the total V-span of the single template mesh 
        float vMaxTemplate = extrusionTemplateMesh.uv.Length > 0 ? extrusionTemplateMesh.uv.Max(uv => uv.y) : 0f;
        
        // 2. Calculate the segment's V span in texture repeats
        float segmentVRepeat = extrusionInterval / TilingDistance;

        // 3. Calculate cumulative V offset based on full distance
        float startDistance = segmentIndex * extrusionInterval;
        float vCumulativeStartOffset = startDistance / TilingDistance;

        // 4. Calculate the scaling factor for the template V range
        float vScaleFactor = (vMaxTemplate > Mathf.Epsilon) ? segmentVRepeat / vMaxTemplate : 0f;

        // 5. Determine the multiple of MAX_SAFE_UV_MAGNITUDE passed at the start of the segment.
        float wrapMultiple = Mathf.Floor(vCumulativeStartOffset / MAX_SAFE_UV_MAGNITUDE);
        
        // 6. Apply the offset and scaling to the template UVs and apply the safe wrap.
        List<Vector2> segmentUvs = new List<Vector2>(extrusionTemplateMesh.uv.Length);
        foreach (Vector2 templateUV in extrusionTemplateMesh.uv)
        {
            // Calculate the local V offset within the segment (0 to segmentVRepeat)
            float vLocalOffset = templateUV.y * vScaleFactor;

            // Calculate the full cumulative V-coordinate (This can be very large)
            float vCumulative = vCumulativeStartOffset + vLocalOffset;

            // Subtract the wrap multiple to bring the V coordinate back into the safe range (0 to ~1000)
            float vFinalSafe = vCumulative - (wrapMultiple * MAX_SAFE_UV_MAGNITUDE);

            segmentUvs.Add(new Vector2(templateUV.x, vFinalSafe));
        }

        // 7. Add the correctly offset and safely magnitude-wrapped UVs
        uvs.AddRange(segmentUvs);
    }

    private (int[] first, int[] second) GetEdgeIndicies(Vector3[] templateVertices, Axis axis)
    {
        List<int> firstIndicies = new List<int>();
        List<int> secondIndicies = new List<int>();

        int vectorIndex = axis == Axis.X ? 0 : axis == Axis.Y ? 1 : 2;

        for (int i = 0; i < templateVertices.Length; i++)
        {
            if (templateVertices[i][vectorIndex] < 0)
                firstIndicies.Add(i);
            else
                secondIndicies.Add(i);
        }

        return (firstIndicies.ToArray(), secondIndicies.ToArray());
    }

    // set the specified axis to zero for each point
    // returns a new array, and does not modify the input array
    private Vector3[] CollapsePointsOnAxis(Vector3[] points, Axis axis)
    {
        Vector3[] collapsedPoints = new Vector3[points.Length];
        Vector3 axisCollapseVector = axis == Axis.X ? new Vector3(0, 1, 1) :
                                     axis == Axis.Y ? new Vector3(1, 0, 1) : 
                                                      new Vector3(1, 1, 0);

        for (int i = 0; i < points.Length; i++)
        {
            // element wise multiplication
            collapsedPoints[i] = Vector3.Scale(points[i], axisCollapseVector);
        }
        return collapsedPoints;
    }
}
