using UnityEngine;

public class SemaphorePlaceUI : GameUIControler
{
    void OnEnable()
    {
        DisableButtons();
        CurrentState = UIState.Track;
        
        PlayerMovment.UseEvent.AddListener(OnUseEvent);
        PlayerMovment.BreakEvent.AddListener(OnBreakEvent);
        SemaphorePlacer.IsPlacementSequenceActive(true);

    }

    void OnUseEvent()
    {
        SemaphorePlacer.PlaceSemaphore();
    }
    
    void OnBreakEvent()
    {

        SemaphorePlacer.RemoveSemaphore();
    }

    void OnDisable()
    {
        PlayerMovment.UseEvent.RemoveListener(OnUseEvent);
        PlayerMovment.BreakEvent.RemoveListener(OnBreakEvent);
        SemaphorePlacer.IsPlacementSequenceActive(false);

    }
}
