using UnityEngine;

public class TrainPlaceUI : GameUIControler
{
    
    
    void OnEnable()
    {
        DisableButtons();
        CurrentState = UIState.Train;
        
        PlayerMovment.UseEvent.AddListener(OnUseEvent);
        PlayerMovment.BreakEvent.AddListener(OnBreakEvent);
        TrainPlacer.IsPlacementSequenceActive(true);

    }

    void OnUseEvent()
    {
        TrainPlacer.PlaceTrain();
    }
    
    void OnBreakEvent()
    {

        TrainPlacer.RemoveTrain();
    }

    void OnDisable()
    {
        PlayerMovment.UseEvent.RemoveListener(OnUseEvent);
        PlayerMovment.BreakEvent.RemoveListener(OnBreakEvent);
        TrainPlacer.IsPlacementSequenceActive(false);

    }
}
