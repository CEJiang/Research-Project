using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class MouseFeatureExtractor
{

    #region Spatial Feature Extraction
    public static MouseSpatialFeature ExtractSpatialFeatures(List<MouseSpatialSample> spatialSamples, int segmentID)
    {
        List<float> arcRatios = new();
        for (int i = Setup.MouseSpatialSamplingDistStart - 1; i < Setup.MouseSpatialSamplingCount; i += Setup.MouseSpatialSamplingDistStride)
        {
            arcRatios.Add(CalculateSphericalArcRatio(spatialSamples, i));
        }

        float efficiency = CalculateSphericalEfficiency(spatialSamples, out float totalArcLength, out float displacement);
        CalculateSphericalCurvature(spatialSamples, out float curvatureMean, out float curvatureStdDev);
        CalculateAngularSpeed(spatialSamples, out float angularSpeedMean, out float angularSpeedStdDev);
    
        MouseSpatialFeature featureData = new()
        {
            segmentID = segmentID,
            startTime = spatialSamples[0].timestamp,
            endTime = spatialSamples[^1].timestamp,
            arcRatios = arcRatios,
            turnBins = CalculateTurnBins(spatialSamples),

            efficiency = efficiency,
            totalArcLength = totalArcLength,
            displacement = displacement,

            curvatureMean = curvatureMean,
            curvatureStdDev = curvatureStdDev,

            angularSpeedMean = angularSpeedMean,
            angularSpeedStdDev = angularSpeedStdDev
        };
        FeatureVisualizer.Instance.AddFeature("efficiency", featureData.efficiency);

        DataLogger<MouseSpatialFeature>.LogData(featureData, Setup.MouseSpatialFeatureDataFileName);
        return featureData;
    }

    private static float CalculateSphericalArcRatio(List<MouseSpatialSample> samples, int n)
    {
        
        float arcSum = 0f;
        for (int i = 1; i < n; ++i)
        {
            Vector3 a = samples[i - 1].direction.normalized;
            Vector3 b = samples[i].direction.normalized;
            
            arcSum += CalculateArc(a, b);
        }

        Vector3 start = samples[0].direction.normalized;
        Vector3 end = samples[n - 1].direction.normalized;

        float straightArc = CalculateArc(start, end);

        return (straightArc > 0f) ? arcSum / straightArc : 0f;
    }
    

    private static float CalculateSphericalEfficiency(List<MouseSpatialSample> samples, out float totalArcLength, out float displacement)
    {
        float arcSum = 0f;
        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 a = samples[i - 1].direction.normalized;
            Vector3 b = samples[i].direction.normalized;

            arcSum += CalculateArc(a, b);
        }

        Vector3 start = samples[0].direction.normalized;
        Vector3 end = samples[^1].direction.normalized;

        float straightArc = CalculateArc(start, end);

        totalArcLength = arcSum;
        displacement = straightArc;

        return (arcSum > 0f) ? straightArc / arcSum : 0f;
    }

    private static float CalculateArc(Vector3 a, Vector3 b)
    {
        float radius = Setup.MouseSpatialSamplingRadius;
        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f));
        float arc = radius * angle;
        return arc;
    }

    private static void CalculateSphericalCurvature(List<MouseSpatialSample> samples, out float curvatureMean, out float curvatureStdDev)
    {
        if (samples == null || samples.Count < 3)
        {
            curvatureMean = 0f;
            curvatureStdDev = 0f;
            return;
        }
        
        List<float> curvatures = new(); 
        float angleSum = 0f;

        for (int i = 1; i < samples.Count - 1; i++)
        {
            Vector3 a = samples[i - 1].direction.normalized;
            Vector3 b = samples[i].direction.normalized;
            Vector3 c = samples[i + 1].direction.normalized;
            Vector3 ab = Vector3.Cross(a, b).normalized;
            Vector3 bc = Vector3.Cross(b, c).normalized;

            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(ab, bc), -1f, 1f));
            
            if (float.IsNaN(angle)) continue;
            
            curvatures.Add(angle);
            angleSum += angle;
        }

        if (curvatures.Count == 0)
        {
            curvatureMean = 0f;
            curvatureStdDev = 0f;
            return;
        }

        curvatureMean = angleSum / curvatures.Count;

        float sumSquaredDiffs = 0f;
        foreach (var k in curvatures)
        {
            float diff = k - curvatureMean;
            sumSquaredDiffs += diff * diff;
        }
        
        if (curvatures.Count > 1)
        {
            curvatureStdDev = Mathf.Sqrt(sumSquaredDiffs / (curvatures.Count - 1));
        }
        else
        {
            curvatureStdDev = 0f;
        }
    }
    
    private static List<float> CalculateTurnBins(List<MouseSpatialSample> samples, int binSizeDeg = 10, int[] strides = null)
    {
        strides ??= new[] { 1, 3, 5, 9 };

        int binCount = 180 / binSizeDeg;
        int[] bins = new int[binCount];
        int total = 0;

        foreach (int k in strides)
        {
            for (int i = k; i < samples.Count; i++)
            {
                Vector3 a = samples[i - k].direction.normalized;
                Vector3 b = samples[i].direction.normalized;

                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f)) * Mathf.Rad2Deg;
                int idx = Mathf.Min(Mathf.FloorToInt(angle / binSizeDeg), binCount - 1);
                bins[idx]++;
                total++;
            }
        }

        List<float> normalized = new(binCount);
        for (int i = 0; i < binCount; i++)
            normalized.Add(total > 0 ? (float)bins[i] / total : 0f);

        return normalized;
    }

    public static void CalculateAngularSpeed(List<MouseSpatialSample> samples, out float angularSpeedMean, out float angularSpeedStdDev)
    {
        if (samples == null || samples.Count < 2)
        {
            angularSpeedMean = 0f;
            angularSpeedStdDev = 0f;
            return;
        }
        
        List<float> angularSpeeds = new List<float>();
        float sum = 0f;
        
        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 a = samples[i - 1].direction.normalized;
            Vector3 b = samples[i].direction.normalized;

            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f));
            float deltaTime = samples[i].timestamp - samples[i - 1].timestamp;

            if (deltaTime > 0f)
            {
                float angularSpeed = angle / deltaTime;
                angularSpeeds.Add(angularSpeed);
                sum += angularSpeed;
            }
        }

        if (angularSpeeds.Count == 0)
        {
            angularSpeedMean = 0f;
            angularSpeedStdDev = 0f;
            return;
        }

        angularSpeedMean = sum / angularSpeeds.Count;

        float sumSquaredDiffs = 0f;
        foreach (var s in angularSpeeds)
        {
            float diff = s - angularSpeedMean;
            sumSquaredDiffs += diff * diff;
        }
        
        if (angularSpeeds.Count > 1)
        {
            angularSpeedStdDev = Mathf.Sqrt(sumSquaredDiffs / (angularSpeeds.Count - 1));
        }
        else
        {
            angularSpeedStdDev = 0f;
        }
    }   

    #endregion

    #region Temporal Feature Extraction
    public static MouseTemporalFeature ExtractTemporalFeatures(List<MouseTemporalSample> temporalSamples)
    {
        if (temporalSamples.Count == 0)
        {
            return null; // No data in the window
        }

        MouseTemporalFeature featureData = new()
        {
            startTime = temporalSamples[0].timestamp,
            endTime = temporalSamples[^1].timestamp,
            angularSpeedMean = CalculateMeanAngularSpeed(temporalSamples),
            angularSpeedStd = CalculateAngularSpeedStdDev(temporalSamples),
            angleStd = CalculateAngleStdDev(temporalSamples),
            yawRange = CalculateYawRange(temporalSamples),
            pitchRange = CalculatePitchRange(temporalSamples),
            fixationTime = CalculateFixationTime(temporalSamples), 
            fixationCount = CalculateFixationCount(temporalSamples),
            clickDensity = CalculateClickDensity(temporalSamples),
            gazeDispersion = CalculateGazeDispersion(temporalSamples)
        };
        DataLogger<MouseTemporalFeature>.LogData(featureData, Setup.MouseTemporalFeatureDataFileName);

        return featureData;
    }

    private static float CalculateMeanAngularSpeed(List<MouseTemporalSample> samples)
    {
        float speedSum = 0f;
        foreach (var sample in samples)
        {
            speedSum += sample.angularSpeed;
        }
        return speedSum / samples.Count;
    }
    private static float CalculateAngularSpeedStdDev(List<MouseTemporalSample> samples)
    {
        if (samples.Count < 2) return 0f;
        
        float speedMean = CalculateMeanAngularSpeed(samples);
        float sumSquaredDiffs = 0f;
        foreach (var sample in samples)
        {
            float diff = sample.angularSpeed - speedMean;
            sumSquaredDiffs += diff * diff;
        }
        return Mathf.Sqrt(sumSquaredDiffs / (samples.Count - 1)); 
    }

    public static float CalculateAngleStdDev(List<MouseTemporalSample> samples)
    {
        if (samples == null || samples.Count < 2) return 0f;
        
        float angleMean = 0f;
        foreach (var sample in samples)
            angleMean += sample.angle;
        
        angleMean /= samples.Count;

        float sumSquaredDiffs = 0f;
        foreach (var sample in samples)
        {
            float diff = sample.angle - angleMean;
            sumSquaredDiffs += diff * diff;
        }
        return Mathf.Sqrt(sumSquaredDiffs / (samples.Count - 1));
    }

    private static float CalculateYawRange(List<MouseTemporalSample> samples)
    {
        float minYaw = float.MaxValue;
        float maxYaw = float.MinValue;
        foreach (var sample in samples)
        {
            if (sample.yaw < minYaw) minYaw = sample.yaw;
            if (sample.yaw > maxYaw) maxYaw = sample.yaw;
        }
        return maxYaw - minYaw;
    }
    private static float CalculatePitchRange(List<MouseTemporalSample> samples)
    {
        float minPitch = float.MaxValue;
        float maxPitch = float.MinValue;
        foreach (var sample in samples)
        {
            if (sample.pitch < minPitch) minPitch = sample.pitch;
            if (sample.pitch > maxPitch) maxPitch = sample.pitch;
        }
        return maxPitch - minPitch;
    }
    private static float CalculateClickDensity(List<MouseTemporalSample> samples)
    {
        if (samples.Count < 2) return 0f;

        int clickCount = 0;
        foreach (var sample in samples)
        {
            if (sample.leftButtonClickDuration > 0f || 
                sample.middleButtonClickDuration > 0f || 
                sample.rightButtonClickDuration > 0f)
            {
                clickCount++;
            }
        }

        float duration = samples[^1].timestamp - samples[0].timestamp;

        return (duration > 0f) ? clickCount / duration : 0f;
    }
    private static float CalculateGazeDispersion(List<MouseTemporalSample> samples)
    {
        if (samples.Count == 0) return 0f;

        Vector3 meanDir = Vector3.zero;
        foreach (var s in samples)
            meanDir += s.position.normalized;
        meanDir.Normalize();

        float maxAngle = 0f;
        foreach (var s in samples)
        {
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(meanDir, s.position.normalized), -1f, 1f)) * Mathf.Rad2Deg;
            if (angle > maxAngle) maxAngle = angle;
        }
        return maxAngle;
    }

    private static (float totalTime, int count) FindFixations(List<MouseTemporalSample> samples, float angularSpeedThreshold = 5.0f, float minDuration = 0.1f)
    {
        float totalFixationTime = 0f;
        int fixationCount = 0;
        float currentFixationStartTime = -1f;

        for (int i = 0; i < samples.Count; i++)
        {
            bool isFixating = samples[i].angularSpeed <= angularSpeedThreshold;

            if (isFixating)
            {
                if (currentFixationStartTime < 0f)
                    currentFixationStartTime = samples[i].timestamp;
            }
            else
            {
                if (currentFixationStartTime >= 0f)
                {
                    float duration = samples[i].timestamp - currentFixationStartTime; 
                    
                    if (duration >= minDuration)
                    {
                        totalFixationTime += duration;
                        fixationCount++;
                    }
                    currentFixationStartTime = -1f;
                }
            }
        }
        if (currentFixationStartTime >= 0f)
        {
            float duration = samples[^1].timestamp - currentFixationStartTime; 
            if (duration >= minDuration)
            {
                totalFixationTime += duration;
                fixationCount++;
            }
        }
        
        return (totalFixationTime, fixationCount);
    }

    private static int CalculateFixationCount(List<MouseTemporalSample> samples)
    {
        return FindFixations(samples).count;
    }

    private static float CalculateFixationTime(List<MouseTemporalSample> samples)
    {
        return FindFixations(samples).totalTime;
    }
    #endregion
}
