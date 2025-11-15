using UnityEngine;

public class TrackPlaceUI : GameUIControler
{
    void OnEnable()
    {
        DisableButtons();
        CurrentState = UIState.Track;
        
        PlayerMovment.UseEvent.AddListener(OnUseEvent);
        PlayerMovment.BreakEvent.AddListener(OnBreakEvent);
    }

    void OnUseEvent()
    {
        RaycastHit hit = PlayerMovment.Instance.GetMousePositionInWorld();
        
        RailPlacer.TogglePlacementSequence(hit.point);
    }
    
    void OnBreakEvent()
    {
        RaycastHit hit = PlayerMovment.Instance.GetMousePositionInWorld();
        
        RailPlacer.CancelPlacement();
        RailPlacer.RemoveRailSegmentAt(hit.point);
    }

    void OnDisable()
    {
        PlayerMovment.UseEvent.RemoveListener(OnUseEvent);
        PlayerMovment.BreakEvent.RemoveListener(OnBreakEvent);
        RailPlacer.CancelPlacement();
    }
}
