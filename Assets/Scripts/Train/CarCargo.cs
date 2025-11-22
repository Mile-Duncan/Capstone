using UnityEngine;

public class CarCargo : MonoBehaviour
{
    public enum CargoType
    {
        TestCube,
        None,
    }

    public string NameFromType(CargoType type)
    {
        switch (type)
        {
            case CargoType.TestCube:
            {
                return "Test Cube";
            }
            default:
            {
                return "None";
            }
        }
    }
    
     public CargoType cargoType;
}
