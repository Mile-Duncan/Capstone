using System.Collections.Generic;
using UnityEngine;

public class TrainSet : MonoBehaviour
{
    public int BreakingForce { get; private set; }
    public int Mass { get; private set; }

    [SerializeField]private float _speed;

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
        
    }

    // Update is called once per frame
    void Update()
    {
        Speed = _speed;
    }
}
