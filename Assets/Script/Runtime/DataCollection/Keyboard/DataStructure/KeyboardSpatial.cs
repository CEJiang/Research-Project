

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class KeyboardSpatialSample
{
    public float timestamp;
    public Vector3 position;
    
}
public class KeyboardSegmentSpatialSample
{
    public int segmentID;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public float startTime;
    public float endTime;
    public List<KeyboardSpatialSample> spatialSamples;
}

public class KeyboardSegmentSpatialFeature
{
    public int segmentID;
    public float startTime;
    public float endTime;

    /// <summary>
    /// total distance calculated along the trajectory
    /// </summary>
    public float totalDistance;

    /// <summary>
    /// displacement between start and end sample points
    /// </summary>
    public float displacement;    

    /// <summary>
    /// efficiency calculated as displacement divided by total distance
    /// </summary>
    public float efficiency;

    /// <summary>
    /// standard deviation of curvature of the trajectory calculated at each sample point
    /// </summary>
    public float curvatureStdDev;

    /// <summary>
    /// mean curvature of the trajectory calculated at each sample point
    /// </summary>
    public float curvatureMean;

    /// <summary>
    /// mean speed of movement along the trajectory 
    /// </summary>
    public float speedMean;

    /// <summary>
    /// Standard deviation of speed
    /// </summary>
    public float speedStdDev;
    
}
