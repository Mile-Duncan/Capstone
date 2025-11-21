using System;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEngine.Splines; // Keep if using the CustomEditor

public class Switch : MonoBehaviour
{
    private RailSegment TrackIn;
    private RailSegment[] TracksOut;
    [SerializeField] private bool CurrentTrack; // false=TracksOut[0], true=TracksOut[1]

    private GameObject _lever;

    // --- CACHED INDICES ---
    private int _trackInJunctionIndex = -1;
    private int _trackOut0JunctionIndex = -1;
    private int _trackOut1JunctionIndex = -1;
    // ----------------------
    
    public struct SwitchRoles
    {
        public RailSegment TrackIn;
        public int TrackInIndex;
        public RailSegment TrackOut1;
        public int TrackOut1Index;
        public RailSegment TrackOut2;
        public int TrackOut2Index;
    }
    
    public void SwitchTrack()
    {
        // Safety check for cached indices
        if (_trackInJunctionIndex == -1 || _trackOut0JunctionIndex == -1 || _trackOut1JunctionIndex == -1)
        {
            Debug.LogError("Switch not properly initialized. Cannot switch tracks.");
            return;
        }

        // 1. Determine current and new tracks and their cached indices
        int previousIdx = CurrentTrack ? 1 : 0;
        RailSegment trackToDisconnect = TracksOut[previousIdx];
        int disconnectIndex = (previousIdx == 0) ? _trackOut0JunctionIndex : _trackOut1JunctionIndex;
    
        CurrentTrack = !CurrentTrack;

        _lever.transform.localRotation = Quaternion.Euler(0, 0, CurrentTrack ? -45 : 45);

        int currentIdx = CurrentTrack ? 1 : 0;
        RailSegment trackToConnect = TracksOut[currentIdx];
        int connectIndex = (currentIdx == 0) ? _trackOut0JunctionIndex : _trackOut1JunctionIndex;
        
        // 2. Execute Switching Logic (Using cached indices)
    
        // A. ESTABLISH NEW CONNECTION: TrackIn -> TrackToConnect
        TrackIn.connections[_trackInJunctionIndex] = trackToConnect;
    
        // B. RESTORE RECIPROCAL LINK: TrackToConnect -> TrackIn
        trackToConnect.connections[connectIndex] = TrackIn;

        // C. BREAK RECIPROCAL LINK: TrackToDisconnect -> null
        trackToDisconnect.connections[disconnectIndex] = null;

        // 3. Notify all affected tracks
        TrackIn.trackUpdateEvent.Invoke();
        trackToConnect.trackUpdateEvent.Invoke();
        trackToDisconnect.trackUpdateEvent.Invoke();
    }

    public static bool CreateSwitchFromSegments(RailSegment t1, RailSegment t2, RailSegment t3)
    {
        // 1. Determine roles using the geometric function (from the previous response)
        SwitchRoles roles = DetermineSwitchRoles(t1, t2, t3);

        // 2. Break the link managed by the switch before creating the component
        // We break the existing link on the T_in segment.
        roles.TrackIn.connections[roles.TrackInIndex] = null; 
    
        // 3. Calculate Stand Position
        // We use the T_out1 segment and its index for stand placement reference.
        Vector3 standPosition = roles.TrackOut1.GetStandPosition(roles.TrackOut1Index);

        // 4. Instantiate the component and assign the T_in/T_out roles
        GameObject switchStand = MonoBehaviour.Instantiate(Resources.Load<GameObject>("Prefabs/SwitchBox"), roles.TrackIn.transform);
        switchStand.transform.position = standPosition;
        switchStand.transform.localScale *= 3;
    
        // 5. Set Stand Rotation (using T_in's tangent)
        Vector3 tan = SplineUtility.EvaluateTangent(roles.TrackIn.SplineSegment.PlaceableSpline, roles.TrackInIndex == 0 ? 0.01f : 0.99f);
        switchStand.transform.rotation = Quaternion.LookRotation(tan , Vector3.up);

        Switch trackSwitch = switchStand.AddComponent<Switch>(); 

        // 6. Assign the switch reference back to TrackIn
        if (roles.TrackIn.switches[roles.TrackInIndex] != null) return false;
        roles.TrackIn.switches[roles.TrackInIndex] = trackSwitch;

        // 7. Initialize the switch. InitSwitch will handle setting the reciprocal links.
        trackSwitch.InitSwitch(roles.TrackIn, new[] { roles.TrackOut1, roles.TrackOut2 });
    
        return true;
    }
  
    public void InitSwitch(RailSegment trackIn, RailSegment[] tracksOut)
    {
        _lever = gameObject.transform.GetChild(0).gameObject;
        PlayerMovment.UseEvent.AddListener(CheckSwitchTrackClick);
        TrackIn = trackIn;
        TracksOut = tracksOut;
        CurrentTrack = false; // Default: TracksOut[0] is the active path.

        // 1. Determine the junction point position from TrackIn
        // Rely on the fact that TrackIn.switches[idx] is already set to 'this'.
        int trackInJunctionIndexCheck = (trackIn.switches[0] == this) ? 0 : 1;
        
        // Find the position of the common junction point J
        Vector3 junctionPointPosition = (trackInJunctionIndexCheck == 0) 
            ? (Vector3)TrackIn.SplineSegment.AEnd.Position 
            : (Vector3)TrackIn.SplineSegment.BEnd.Position;

        // 2. CACHE ALL NECESSARY INDICES
        _trackInJunctionIndex = trackInJunctionIndexCheck;
        
        // Find and cache the index for TrackOut[0]
        sbyte out0Sbyte = TracksOut[0].SplineSegment.GetKnotAt((float3)junctionPointPosition);
        if (out0Sbyte == -1) { Debug.LogError("InitSwitch failed: Out0 knot not found."); return; }
        _trackOut0JunctionIndex = (int)out0Sbyte;
        
        // Find and cache the index for TrackOut[1]
        sbyte out1Sbyte = TracksOut[1].SplineSegment.GetKnotAt((float3)junctionPointPosition);
        if (out1Sbyte == -1) { Debug.LogError("InitSwitch failed: Out1 knot not found."); return; }
        _trackOut1JunctionIndex = (int)out1Sbyte;
        
        // 3. ESTABLISH THE FINAL STARTING STATE (TrackOut[0] active)
        
        // Connect the active path: TrackIn -> TracksOut[0]
        TrackIn.connections[_trackInJunctionIndex] = TracksOut[0];
        
        // Connect the reciprocal link for the active path: TracksOut[0] -> TrackIn
        TracksOut[0].connections[_trackOut0JunctionIndex] = trackIn; 
        
        // BREAK the reciprocal link for the blocked path: TracksOut[1] -> null
        TracksOut[1].connections[_trackOut1JunctionIndex] = null; 
        
        // 4. Call SwitchTrack twice to ensure the lever and state are correct if the default is changed.
        SwitchTrack();
        SwitchTrack();
    }

    private void CheckSwitchTrackClick()
    {
        RaycastHit raycastHit = PlayerMovment.Instance.GetMousePositionInWorld();
        if (raycastHit.collider == gameObject.GetComponent<Collider>())
        {
            SwitchTrack();
        }
    }

    void OnDestroy()
    {
        if(TrackIn == null) return;
        if(TracksOut[0]==null&&TracksOut[1]==null) return;
        // The logic below ensures that whichever track is currently blocked is restored 
        // to a 'null' connection state on TrackIn before TrackIn is potentially destroyed.
        
        // If TrackOut[1] is active but null, switch to TrackOut[0] before destroying
        if(TracksOut[1]==null && CurrentTrack) SwitchTrack();
        // If TrackOut[0] is active but null, switch to TrackOut[1] before destroying
        if(TracksOut[0]==null && !CurrentTrack) SwitchTrack();
    }
    public static SwitchRoles DetermineSwitchRoles(RailSegment t1, RailSegment t2, RailSegment t3)
    {
        // --- Step 1: FIND THE COMMON JUNCTION POINT POSITION ROBUSTLY ---
        
        // We check T1's ends against T2's knots to find the shared position (J).
        Vector3 junctionPointPosition;
        if (t2.SplineSegment.GetKnotAt(t1.SplineSegment.AEnd.Position) != -1)
        {
            junctionPointPosition = (Vector3)t1.SplineSegment.AEnd.Position;
        }
        else if (t2.SplineSegment.GetKnotAt(t1.SplineSegment.BEnd.Position) != -1)
        {
            junctionPointPosition = (Vector3)t1.SplineSegment.BEnd.Position;
        }
        else
        {
            // This indicates a setup error, as T1, T2, and T3 should be connected here.
            throw new InvalidOperationException("Segments T1 and T2 do not share a common knot position.");
        }


        // --- Step 2: GET JUNCTION INDEX FOR ALL THREE TRACKS ---
        sbyte i1_sbyte = t1.SplineSegment.GetKnotAt((float3)junctionPointPosition);
        sbyte i2_sbyte = t2.SplineSegment.GetKnotAt((float3)junctionPointPosition);
        sbyte i3_sbyte = t3.SplineSegment.GetKnotAt((float3)junctionPointPosition);

        if (i1_sbyte == -1 || i2_sbyte == -1 || i3_sbyte == -1)
        {
            throw new InvalidOperationException("One or more segments do not share the specified junction point.");
        }
        
        int i1 = (int)i1_sbyte;
        int i2 = (int)i2_sbyte;
        int i3 = (int)i3_sbyte;

        // --- Step 3: GET OUTWARD TANGENT VECTORS ---
        // These vectors (V1, V2, V3) all point AWAY from the junction.
        Vector3 v1 = t1.GetOutwardTangent(i1); 
        Vector3 v2 = t2.GetOutwardTangent(i2);
        Vector3 v3 = t3.GetOutwardTangent(i3);

        // --- Step 4: PERFORM DOT PRODUCT COMPARISON TO FIND THE T_in (SPLIT TRACK) ---

        // Calculate the average direction of two segments and compare it to the third.
        // The track with the most negative dot product is the T_in track.

        // Case 1: T1 vs (Avg T2, T3)
        Vector3 avgV2V3 = (v2 + v3).normalized;
        float dot1_avg23 = Vector3.Dot(v1, avgV2V3);

        // Case 2: T2 vs (Avg T1, T3)
        Vector3 avgV1V3 = (v1 + v3).normalized;
        float dot2_avg13 = Vector3.Dot(v2, avgV1V3);

        // Case 3: T3 vs (Avg T1, T2)
        Vector3 avgV1V2 = (v1 + v2).normalized;
        float dot3_avg12 = Vector3.Dot(v3, avgV1V2);

        // Find the minimum dot product (maximum opposition)
        float minDot = Mathf.Min(dot1_avg23, dot2_avg13, dot3_avg12);

        // --- Step 5: ASSIGN ROLES ---
        RailSegment trackIn;
        int trackInIndex;
        RailSegment trackOut1;
        RailSegment trackOut2;

        if (minDot == dot1_avg23) // T1 is the odd one out (Trunk)
        {
            trackIn = t1;
            trackInIndex = i1;
            trackOut1 = t2;
            trackOut2 = t3;
        }
        else if (minDot == dot2_avg13) // T2 is the odd one out (Trunk)
        {
            trackIn = t2;
            trackInIndex = i2;
            trackOut1 = t1;
            trackOut2 = t3;
        }
        else // T3 is the odd one out (Trunk)
        {
            trackIn = t3;
            trackInIndex = i3;
            trackOut1 = t1;
            trackOut2 = t2;
        }

        // --- Step 6: Return the final, geometrically-determined roles ---
        return new SwitchRoles
        {
            TrackIn = trackIn,
            TrackInIndex = trackInIndex,
            TrackOut1 = trackOut1,
            // Assign the correct junction index for the track out segments
            TrackOut1Index = (trackOut1 == t1) ? i1 : (trackOut1 == t2 ? i2 : i3),
            TrackOut2 = trackOut2,
            TrackOut2Index = (trackOut2 == t1) ? i1 : (trackOut2 == t2 ? i2 : i3)
        };
    }
}

