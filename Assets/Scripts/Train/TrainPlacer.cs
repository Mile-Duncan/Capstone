using System.Collections;
using UnityEngine;

public class TrainPlacer
{
    private static uint NextTrainNumber = 0;
    public static bool IsPlacing{get; private set;}

    private static GameObject TrainCarPrefab = Resources.Load<GameObject>("Prefabs/TrainCar");

    private static TrainCar CurrentPlacingTrain;
    public static void IsPlacementSequenceActive(bool isPlacing = false)
    {
        IsPlacing = isPlacing;

        if (!IsPlacing)
        {
            if(CurrentPlacingTrain.gameObject!=null)Object.Destroy(CurrentPlacingTrain.gameObject);
            return;
        }
        
        CurrentPlacingTrain = Object.Instantiate(TrainCarPrefab).GetComponent<TrainCar>();

        CurrentPlacingTrain.StartCoroutine(UpdatePlaceingTrainCar());


    }
    
    private static IEnumerator UpdatePlaceingTrainCar()
    {
        while (IsPlacing)
        {
            yield return null;
            if(CurrentPlacingTrain == null)break;
            Vector3 placePosition = PlayerMovment.Instance.GetMousePositionInWorld().point;
            CurrentPlacingTrain.transform.position = placePosition;
            CheckValidity();
        }
    }
    
    public static void PlaceTrain()
    {
        if (!CheckValidity()) return;

        CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color = Color.white;
        CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color *= new Vector4(1f, 1f, 1f, 0f);
        TrainSet trainSet = new GameObject("Train "+NextTrainNumber).AddComponent<TrainSet>();
        trainSet.Cars.Add(CurrentPlacingTrain);
        NextTrainNumber++;
        CurrentPlacingTrain = Object.Instantiate(TrainCarPrefab).GetComponent<TrainCar>();
    }

    public static void RemoveTrain()
    {
        Object.Destroy(CurrentPlacingTrain);
    }

    private static bool CheckValidity()
    {
        if(CurrentPlacingTrain.BogieA.currentSegment == null)return false;
        if (CurrentPlacingTrain.BogieA.currentSegment.trackCircuit != Semaphore.State.Stop &&
            CurrentPlacingTrain.BogieA.currentSegment.trackCircuit != Semaphore.State.Stop)
        {
            CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color = Color.cyan;
            CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color *= new Vector4(1f, 1f, 1f, .4f);
            return true;
        }
        
        CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color = Color.red;
        CurrentPlacingTrain.carCollider.GetComponent<MeshRenderer>().material.color *= new Vector4(1f, 1f, 1f, .4f);

        return false;
    }
}
