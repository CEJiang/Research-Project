

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MouseSpatialSample
{
    public float timestamp;
    public Vector3 position;
    public float yaw;
    public float pitch;
    public Vector3 direction;
}

public class MouseSpatialFeature
{
    public int segmentID;
    public float startTime;
    public float endTime;

    public List<float> arcRatios;

    public float totalArcLength;
    public float displacement;
    public float efficiency;

    public float curvatureMean;
    public float curvatureStdDev;

    public float angularSpeedMean;
    public float angularSpeedStdDev;
    
    public List<float> turnBins;
}