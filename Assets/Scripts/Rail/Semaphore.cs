using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Semaphore : MonoBehaviour
{
    [SerializeField] private GameObject greenLight;
    [SerializeField] private GameObject yellowLight;
    [SerializeField] private GameObject redLight;

    private static Material _greenMaterial;
    private static Material _yellowMaterial;
    private static Material _redMaterial;
    private static Material _offMaterial;
    
    private static List<Semaphore> _allSemaphores = new List<Semaphore>();


    private static int _currentUpdateTick;
    private int _lastUpdateTick;
    
    public State SemaphoreState { get; private set; } = State.None;

    //public Semaphore UplineConection;
    //public Semaphore DownlineConection;
    //public Semaphore OpposingConection;
    private RailSegment AttachedRailSegment;
    private bool IsAt1;
    public enum State
    {
        Clear = 2,
        Approach = 1,
        Stop = 0,
        None = -1,
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _greenMaterial = Resources.Load<Material>("Materials/lights/Green");
        _yellowMaterial = Resources.Load<Material>("Materials/lights/Yellow");
        _redMaterial = Resources.Load<Material>("Materials/lights/Red");
        _offMaterial = Resources.Load<Material>("Materials/lights/Off");

        if (_currentUpdateTick == 0)
        {
            _currentUpdateTick++;
            PlayerMovment.Instance.StartCoroutine(UpdateSemaphores());
        }
    }
    
    private static IEnumerator UpdateSemaphores()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            
            foreach (Semaphore semaphore in _allSemaphores)
            {
                semaphore.SetState(State.Clear);
            }

            foreach (RailSegment segment in RailNetwork.Track)
            {
                segment.trackCircuit = State.Clear;
                if (segment.connections[0] is null)
                {
                    segment.isOccupied = true;
                    segment.trackCircuit = State.Stop;
                    if(!RailNetwork.UnsetOccupiedTrack.Contains(segment))RailNetwork.UnsetOccupiedTrack.Add(segment);
                }else if (segment.connections[1] is null)
                {
                    if(!RailNetwork.UnsetOccupiedTrack.Contains(segment))RailNetwork.UnsetOccupiedTrack.Add(segment);
                    segment.isOccupied = true;
                    segment.trackCircuit = State.Stop;
                }
            }
            
            _currentUpdateTick++;
            while(RailNetwork.UnsetOccupiedTrack.Count>0)
            {
                UpdatePositions(RailNetwork.UnsetOccupiedTrack[0]);
                RailNetwork.UnsetOccupiedTrack.Remove(RailNetwork.UnsetOccupiedTrack[0]);

            }
            
            foreach (Semaphore semaphore in _allSemaphores)
            {
                semaphore.SetVisualPosition();
            }

        }
    }

    public void Init(RailSegment segment, bool at1)
    {
        AttachedRailSegment = segment;
        IsAt1 = at1;
        
        _allSemaphores.Add(this);
        
        transform.parent = segment.transform;
        
    }

    public static void UpdatePositions(RailSegment segment)
    {
        RailSegment nextSegment = segment;

        bool nextAt1 = (segment.direction == RailSplineFolower.Direction.Positive);
            
        State propagatingState = State.Stop;
        segment.trackSemaphores[0]?.SetState(propagatingState);
        segment.trackSemaphores[1]?.SetState(propagatingState);
        segment.trackCircuit = State.Stop;
        
        if(segment.direction == RailSplineFolower.Direction.Stopped) return; 
        propagatingState++;

        int iterations = 100;
        while (true)
        {
            iterations--;
            if (iterations <= 0)
            {
                throw new WarningException("Semaphores are in a loop! Aborting!");
            }
            
            if(!PrepNextSegment(true))break;

            if(propagatingState!=State.Stop && nextSegment.isOccupied)break;
            if (nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)] != null && !nextSegment.isOccupied && propagatingState < State.Clear)
            {
                nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)].SetState(propagatingState);
                propagatingState++;
                if(propagatingState>=State.Clear)propagatingState = State.Clear;
            }
            
            
            if (nextSegment.trackSemaphores[Convert.ToByte(nextAt1)] != null)nextSegment.trackSemaphores[Convert.ToByte(nextAt1)].SetState(State.Stop);
        }
        
        nextSegment = segment;
        
        nextAt1 = (segment.direction != RailSplineFolower.Direction.Positive);
        
        iterations = 100;
        while (true)
        {
            iterations--;
            if (iterations <= 0)
            {
                throw new WarningException("Semaphores are in a loop! Aborting!");
            }
            
            if(!PrepNextSegment())break;

            if (nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)] != null)nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)].SetState(State.Stop);
        }
        
        bool PrepNextSegment(bool propagete=false)
        {
            bool lastAt1 = nextAt1;
            RailSegment lastSegment = nextSegment;
            lastSegment.GetConnection(!lastAt1, out nextSegment, out nextAt1);
            
            if (nextSegment == null || nextSegment == segment) return false;
            
            if(propagete)nextSegment.trackCircuit = propagatingState;
            
            if(nextAt1==lastAt1) nextSegment.direction = lastSegment.direction;
            else
            {
                if (lastSegment.direction == RailSplineFolower.Direction.Negative) nextSegment.direction = RailSplineFolower.Direction.Positive;
                else if (lastSegment.direction == RailSplineFolower.Direction.Positive) nextSegment.direction = RailSplineFolower.Direction.Negative;

            }
            return true;
        }
    }


    public void SetState(State state)
    {
        if(_lastUpdateTick>= _currentUpdateTick && state >= SemaphoreState)return;
        _lastUpdateTick = _currentUpdateTick;
        SemaphoreState = state;
    }

    private void SetVisualPosition()
    {
        switch (SemaphoreState)
        {
            case State.Clear:
            {
                Green(true);
                Yellow(false);
                Red(false);
                break;
            }
            case State.Approach:
            {
                Green(false);
                Yellow(true);
                Red(false);
                break;
            }
            case State.Stop:
            {
                Green(false);
                Yellow(false);
                Red(true);
                break;
            }
        }
    }
    
    private void Green(bool on)
    {
        if(on)greenLight.GetComponent<MeshRenderer>().material = _greenMaterial;
        else greenLight.gameObject.GetComponent<MeshRenderer>().material = _offMaterial;
    }
    
    private void Yellow(bool on)
    {
        if(on)yellowLight.GetComponent<MeshRenderer>().material = _yellowMaterial;
        else yellowLight.gameObject.GetComponent<MeshRenderer>().material = _offMaterial;
    }

    private void Red(bool on)
    {
        if(on)redLight.GetComponent<MeshRenderer>().material = _redMaterial;
        else redLight.gameObject.GetComponent<MeshRenderer>().material = _offMaterial;
    }

    private void OnDestroy()
    {
        _allSemaphores.Remove(this);
    }
}
