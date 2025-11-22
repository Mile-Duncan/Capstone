using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainSet : MonoBehaviour
{
    public int BreakingForce { get; private set; }
    public int Mass { get; private set; }
    public bool Forward;

    [SerializeField] public static List<TrainSet> TrainSets = new List<TrainSet>();

    [SerializeField] private Semaphore.State currentTrackState;

    public float Speed
    {
        get
        {
            return Cars[0].Speed;
        }
        set
        {
            foreach (TrainCar car in Cars)
            {
                car.Speed = value;
            }
        }
    }

    public List<TrainCar> Cars = new List<TrainCar>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerMovment.UseEvent.AddListener(OnTrainClick);
        TrainSets.Add(this);
    }

    private void OnTrainClick()
    {
        RaycastHit raycastHit = PlayerMovment.Instance.GetMousePositionInWorld();
        foreach (TrainCar car in Cars)
        {
            if (raycastHit.collider == car.carCollider)
            {
                Forward = !Forward;
                car.BogieA.nextSegment.trackCircuit = Semaphore.State.Clear;
                return;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        currentTrackState = Semaphore.State.None;
        foreach (TrainCar car in Cars)
        {
            if (car.currentTrackState > currentTrackState) currentTrackState = car.currentTrackState;
        }

        switch (currentTrackState)
        {
            case (Semaphore.State.Clear):
            {
                Speed = 15;
                break;
            }
            case (Semaphore.State.Approach):
            {
                Speed = 5;
                break;
            }
            default:
            {
                Speed = 0;
                break;
            }
        }

        if (!Forward) Speed *= -1;

    }

    private void OnDestroy()
    {
        TrainSets.Remove(this);
    }
}
