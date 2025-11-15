using System;
using System.ComponentModel;
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
    }

    public void Init(RailSegment segment, bool at1)
    {
        AttachedRailSegment = segment;
        IsAt1 = at1;
        
        

        /*UplineConection = GetNext(true);
        if (UplineConection != null)
        {
            UplineConection.DownlineConection = this;
            UplineConection.SetOpposing();
        }
        DownlineConection = GetNext(false);
        if(DownlineConection != null) DownlineConection.UplineConection = this;
        SetOpposing();*/
        
    }

    public static void UpdatePositions(RailSegment segment)
    {
        RailSegment nextSegment = segment;

        bool nextAt1 = (segment.direction == RailSplineFolower.Direction.Positive);
            
        State propagatingState = State.Stop;
        segment.trackSemaphores[0]?.SetState(propagatingState);
        segment.trackSemaphores[1]?.SetState(propagatingState);
        
        if(segment.direction == RailSplineFolower.Direction.None) return; 
        //propagatingState++;

        int iterations = 100;
        while (true)
        {
            iterations--;
            if (iterations <= 0)
            {
                throw new WarningException("Semaphores are in a loop! Aborting!");
            }
            nextSegment.GetConnection(!nextAt1, out nextSegment, out nextAt1);
            
            if (nextSegment == null || nextSegment == segment) break;
            nextSegment.direction = segment.direction;

            if(propagatingState!=State.Stop && nextSegment.isOccupied)break;
            if (nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)] != null && !nextSegment.isOccupied)
            {
                nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)].SetState(propagatingState);
                propagatingState++;
                if(propagatingState>=State.Clear)propagatingState = State.Clear;
            }
            
            
            if (nextSegment.trackSemaphores[Convert.ToByte(nextAt1)] != null)nextSegment.trackSemaphores[Convert.ToByte(nextAt1)].SetState(State.Stop);
        }
        
        nextSegment = segment;
        
        nextAt1 = (segment.direction != RailSplineFolower.Direction.Positive);
        while (true)
        {
            nextSegment.GetConnection(!nextAt1, out nextSegment, out nextAt1);
            
            if (nextSegment == null || nextSegment == segment) return;
            nextSegment.direction = segment.direction;

            if (nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)] != null)nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)].SetState(State.Stop);
        }
    }

    /*void OnDestroy()
    {
        if(UplineConection != null) UplineConection.DownlineConection = DownlineConection;
        if(DownlineConection != null) DownlineConection.UplineConection = UplineConection;
    }

    private Semaphore GetNext(bool upline)
    {
        RailSegment nextSegment = AttachedRailSegment;
        bool nextAt1 = IsAt1 ^ upline;

        while (true)
        {
            nextSegment.GetConnection(!nextAt1, out nextSegment, out nextAt1);

            if (nextSegment == null || nextSegment == AttachedRailSegment) return null;
            if (nextSegment.trackSemaphores[Convert.ToByte(nextAt1 ^ upline)] != null) return nextSegment.trackSemaphores[Convert.ToByte(nextAt1 ^ upline)];
        }
    }

    private Semaphore GetNext(bool upline)
    {
        RailSegment nextSegment = AttachedRailSegment;
        bool nextAt1 = IsAt1 ^ upline;

        while (true)
        {
            nextSegment.GetConnection(!nextAt1, out nextSegment, out nextAt1);

            if (nextSegment == null || nextSegment == AttachedRailSegment) return null;
            if (nextSegment.trackSemaphores[Convert.ToByte(nextAt1 ^ upline)] != null) return nextSegment.trackSemaphores[Convert.ToByte(nextAt1 ^ upline)];
        }
    }

    private void SetOpposing()
    {
        RailSegment nextSegment = AttachedRailSegment;
        bool nextAt1 = IsAt1;

        while (true)
        {
            if (nextSegment.trackSemaphores[Convert.ToByte(nextAt1)] != null)
            {
                OpposingConection = null;
                return;
            }

            if (nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)] != null)
            {
                OpposingConection = nextSegment.trackSemaphores[Convert.ToByte(!nextAt1)];
                return;
            }

            nextSegment.GetConnection(!nextAt1, out nextSegment, out nextAt1);

            if (nextSegment == null || nextSegment == AttachedRailSegment)
            {
                OpposingConection = null;
                return;
            }
        }
    }*/

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    public void SetState(State state)
    {
        if(SemaphoreState == state)return;
        SemaphoreState = state;
        switch (state)
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
}
