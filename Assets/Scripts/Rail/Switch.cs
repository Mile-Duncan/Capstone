using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
public class Switch : MonoBehaviour
{
    private RailSegment TrackIn;

    private GameObject _lever;
    
    private RailSegment[] TracksOut;
    [SerializeField]private bool CurrentTrack;
    
    public void SwitchTrack()
    {
        // 1. Determine current and new tracks
        int previousIdx = CurrentTrack ? 1 : 0;
        RailSegment trackToDisconnect = TracksOut[previousIdx];
    
        CurrentTrack = !CurrentTrack;

        _lever.transform.localRotation = Quaternion.Euler(0, 0, CurrentTrack ? -45 : 45);

        
        int currentIdx = CurrentTrack ? 1 : 0;
        RailSegment trackToConnect = TracksOut[currentIdx];

        // 2. Find all necessary indices using the helper method.
        int junctionIndex = (TrackIn.switches[0] == this) ? 0 : 1;
        int connectIndex = RailSegment.GetJunctionIndex(trackToConnect, TrackIn);
        int disconnectIndex = RailSegment.GetJunctionIndex(trackToDisconnect, TrackIn);
    
        // 3. Execute Switching Logic (Now guaranteed to use the correct indices)
    
        // A. ESTABLISH NEW CONNECTION: TrackIn -> TrackToConnect
        TrackIn.connections[junctionIndex] = trackToConnect;
    
        // B. RESTORE RECIPROCAL LINK: TrackToConnect -> TrackIn (This is the line that now works!)
        trackToConnect.connections[connectIndex] = TrackIn;

        // C. BREAK RECIPROCAL LINK: TrackToDisconnect -> null
        trackToDisconnect.connections[disconnectIndex] = null;

        // 4. Notify all affected tracks
        TrackIn.trackUpdateEvent.Invoke();
        trackToConnect.trackUpdateEvent.Invoke();
        trackToDisconnect.trackUpdateEvent.Invoke();
    }

  
    public void InitSwitch(RailSegment trackIn, RailSegment[] tracksOut)
    {
        _lever = gameObject.transform.GetChild(0).gameObject;
        PlayerMovment.UseEvent.AddListener(CheckSwitchTrackClick);
        TrackIn = trackIn;
        TracksOut = tracksOut;
        CurrentTrack = false; // Default: TracksOut[0] is the active path.

        // 1. Find the TrackIn junction index (Must rely on the 'switches' array being set)
        int trackInJunctionIndex = -1;
        if (trackIn.switches[0] == this) trackInJunctionIndex = 0;
        else if (trackIn.switches[1] == this) trackInJunctionIndex = 1;

        // 2. Find the junction indices for the outgoing tracks using the helper.
        // This finds the index based on the placement, regardless of the 'null' state.
        int trackOut0JunctionIndex = RailSegment.GetJunctionIndex(TracksOut[0], TrackIn);
        int trackOut1JunctionIndex = RailSegment.GetJunctionIndex(TracksOut[1], TrackIn);

        if (trackInJunctionIndex != -1) // Simplify error check as helper should prevent index issues
        {
            // 3. ESTABLISH THE FINAL STATE DIRECTLY
        
            // Connect the active path: TrackIn -> TracksOut[0]
            TrackIn.connections[trackInJunctionIndex] = TracksOut[0];
        
            // Connect the reciprocal link for the active path: TracksOut[0] -> TrackIn (THE FIX!)
            TracksOut[0].connections[trackOut0JunctionIndex] = trackIn; 
        
            // BREAK the reciprocal link for the blocked path: TracksOut[1] -> null
            TracksOut[1].connections[trackOut1JunctionIndex] = null; 
        }
        else
        {
            Debug.LogError("Switch initialization failed: Could not determine TrackIn index.");
        }
        
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
        if(TracksOut[1]==null&&CurrentTrack)SwitchTrack();
        if(TracksOut[0]==null&&!CurrentTrack)SwitchTrack();

    }
}

[CustomEditor(typeof(Switch))]
public class MyScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Switch myScript = (Switch)target;
        if (GUILayout.Button("Click Me"))
        {
            myScript.SwitchTrack();
        }
    }
}