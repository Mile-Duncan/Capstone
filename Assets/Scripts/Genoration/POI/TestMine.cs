using System;
using UnityEngine;

public class TestMine : MonoBehaviour
{
    GameObject prefab;
    public int SpawnRate;
    public float range = 10;

    private void Start()
    {
        prefab = Resources.Load<GameObject>("Prefabs/CarModels/LoadedTestCar");
    }

    private void LoadLoop()
    {
        foreach (TrainSet train in TrainSet.TrainSets) foreach (TrainCar car in train.Cars)
        {
            if (Vector3.Angle(car.transform.position, transform.position) < range)
            {
                Destroy(car.carModel);
                car.carModel = Instantiate(prefab, car.carBodyDummy.transform.position, car.carBodyDummy.transform.rotation);
            }
        }
    }
}
