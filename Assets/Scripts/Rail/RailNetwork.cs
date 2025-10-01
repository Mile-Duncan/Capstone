using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Splines.ExtrusionShapes;

public class RailNetwork : MonoBehaviour
{
    public SplineContainer railSplines;
    public static RailNetwork Instance;
    private static SplineExtrude railRoad;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
        railSplines = gameObject.AddComponent<SplineContainer>();
        railRoad = gameObject.AddComponent<SplineExtrude>();
        railRoad.Container = railSplines;
        railRoad.Radius = 0.5f;
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
