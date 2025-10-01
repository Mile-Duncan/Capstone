using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class RailSegment : MonoBehaviour
{
    public enum SegmentType
    {
        Fucked = -1,
        Detached = 0,
        Buffer = 1,
        Standard = 2,
        Switch = 3,
        Crossover = 4,
        
    }
    
    public Spline spline;
    public List<RailSegment> conections;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
