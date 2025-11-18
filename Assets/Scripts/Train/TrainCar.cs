using UnityEngine;

public class TrainCar : MonoBehaviour
{
    public int BreakingForce { get; private set; }
    public int Mass { get; private set; }
    public Semaphore.State currentTrackState {get; private set;}

    public float Speed
    {
        get
        {
            return BogieA.speed;
        }
        set
        {
            BogieA.speed = value;
            BogieB.speed = value;
        }
    }

    public RailSplineFolower BogieA;
    public RailSplineFolower BogieB;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));

        Semaphore.State? AState = BogieA.nextSegment?.trackCircuit;
        Semaphore.State? BState = BogieB.nextSegment?.trackCircuit;

        AState ??= Semaphore.State.None;
        BState ??= Semaphore.State.None;
        
        currentTrackState = AState.Value > BState.Value ?  AState.Value : BState.Value;
    }
}
