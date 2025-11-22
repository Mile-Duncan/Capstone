using System;
using UnityEngine;

public class TestFactory : POI
{ 
    private new void Start()
    {
        CargoPrefab = Resources.Load<CarCargo>("Prefabs/CarModels/TestCargo");
        
        MaxUnits = 10;
        MinUnits = 2;
        MaxReward = 1000;
        MinReward = 500;
        MaxTime = 200;
        MinTime = 50;
        ContractText = "The Blue Cube Factory would like to offer you one of the following contracts: ";

        base.Start();
    }

    protected override void LoadLoop(TrainCar car)
    {
        if(car.cargoModel== null)return;
        if(car.cargoModel.cargoType != CarCargo.CargoType.TestCube)return;
        if (Vector3.Distance(car.carBodyDummy.transform.position, transform.position) < Range)
        {
            Destroy(car.cargoModel.gameObject);
            UnitsStored--;
        }
    }
}
