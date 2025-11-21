using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyboardFeatureExtractor
{

    #region Spatial Feature Extraction
    public static KeyboardSegmentSpatialFeature ExtractSpatialFeatures(KeyboardSegmentSpatialSample segmentSpatialSample)
    {
        float efficiency = CalculateEfficiency(segmentSpatialSample.spatialSamples, out float totalDistance, out float displacement);
        CalculateCurvature(segmentSpatialSample.spatialSamples, out float curvatureStdDev, out float curvatureMean);
        CalculateSpeed(segmentSpatialSample.spatialSamples, out float speedMean, out float speedStdDev);

        KeyboardSegmentSpatialFeature keyboardSpatialFeature = new()
        {
            segmentID = segmentSpatialSample.segmentID,
            startTime = segmentSpatialSample.spatialSamples[0].timestamp,
            endTime = segmentSpatialSample.spatialSamples[^1].timestamp,
            totalDistance = totalDistance,
            displacement = displacement,
            efficiency = efficiency,
            curvatureStdDev = curvatureStdDev,
            curvatureMean = curvatureMean,
            speedMean = speedMean,
            speedStdDev = speedStdDev
        };

        DataLogger<KeyboardSegmentSpatialFeature>.LogData(keyboardSpatialFeature, Setup.KeyboardSpatialFeatureDataFileName);

        return keyboardSpatialFeature;
    }
    private static float CalculateEfficiency(List<KeyboardSpatialSample> samples, out float totalDistance, out float displacement)
    {
        totalDistance = 0f;
        for (int i = 1; i < samples.Count; i++)
            totalDistance += Vector3.Distance(samples[i - 1].position, samples[i].position);

        displacement = Vector3.Distance(samples[0].position, samples[^1].position);

        return (totalDistance > 0f) ? displacement / totalDistance : 0f;
    }

    private static void CalculateCurvature(List<KeyboardSpatialSample> samples, out float curvatureStdDev, out float curvatureMean)
    {
        if (samples == null || samples.Count < 3)
        {
            curvatureMean = 0f;
            curvatureStdDev = 0f;
            return;
        }

        List<float> angles = new(); 
        float angleSum = 0f;

        for (int i = 1; i < samples.Count - 1; i++)
        {
            Vector3 v1 = (samples[i].position - samples[i - 1].position).normalized;
            Vector3 v2 = (samples[i + 1].position - samples[i].position).normalized;

            float dot = Mathf.Clamp(Vector3.Dot(v1, v2), -1f, 1f);
            float angle = Mathf.Acos(dot);

            if (float.IsNaN(angle)) continue; 
            
            angles.Add(angle);
            angleSum += angle;
        }
        if (angles.Count == 0)
        {
            curvatureMean = 0f;
            curvatureStdDev = 0f;
            return;
        }

        curvatureMean = angleSum / angles.Count;

        float sumSquaredDiffs = 0f;
        foreach (float a in angles)
        {
            float diff = a - curvatureMean;
            sumSquaredDiffs += diff * diff;
        }
        
        if (angles.Count > 1)
        {
            curvatureStdDev = Mathf.Sqrt(sumSquaredDiffs / (angles.Count - 1));
        }
        else
        {
            curvatureStdDev = 0f;
        }
    }
    private static void CalculateSpeed(List<KeyboardSpatialSample> samples, out float speedMean, out float speedStdDev)
    {
        if (samples == null || samples.Count < 2)
        {
            speedMean = 0f;
            speedStdDev = 0f;
            return;
        }
        
        List<float> speeds = new(); 
        float speedSum = 0f;

        for (int i = 1; i < samples.Count; i++)
        {
            float distance = Vector3.Distance(samples[i - 1].position, samples[i].position);
            float timeDelta = samples[i].timestamp - samples[i - 1].timestamp;

            float speed = (timeDelta > 0f) ? distance / timeDelta : 0f;
            speeds.Add(speed);
            speedSum += speed;
        }

        speedMean = speedSum / speeds.Count;

        float sumSquaredDiffs = 0f;
        foreach (float s in speeds)
        {
            float diff = s - speedMean;
            sumSquaredDiffs += diff * diff;
        }

        if (speeds.Count > 1)
        {
            speedStdDev = Mathf.Sqrt(sumSquaredDiffs / (speeds.Count - 1));
        }
        else
        {
            speedStdDev = 0f;
        }
    }
    
    #endregion

    #region Temporal Feature Extraction
    public static KeyboardTemporalFeature ExtractTemporalFeatures(List<KeyboardTemporalSample> temporalSamples)
    {
        if (temporalSamples == null || temporalSamples.Count == 0)
        {
            return null;
        }
        
        (float meanHold, float stdDevHold) = CalculateHoldTimeStats(temporalSamples);
        
        KeyboardTemporalFeature temporalFeature = new()
        {   
            startTime = temporalSamples[0].timestamp,
            endTime = temporalSamples[^1].timestamp,
            
            meanHoldTime = meanHold, 
            stdDevHoldTime = stdDevHold,
            
            meanSeekTime = CalculateMeanSeekTime(temporalSamples),
            meanLatency2 = CalculateMeanLatency2(temporalSamples),
            pressDensity = CalculatePressDensity(temporalSamples),
            directionalKeyBins = CalculateDirectionalKeyBins(temporalSamples)
        };
        DataLogger<KeyboardTemporalFeature>.LogData(temporalFeature, Setup.KeyboardTemporalFeatureDataFileName);
        return temporalFeature;
    }

    private static (float meanHoldTime, float stdDevHoldTime) CalculateHoldTimeStats(List<KeyboardTemporalSample> samples)
    {
        List<float> holdTimes = new();
        foreach (var sample in samples)
        {
            if (sample.holdTime > 0f)
            {
                holdTimes.Add(sample.holdTime);
            }
        }

        if (holdTimes.Count == 0)
        {
            return (0f, 0f);
        }
        float mean = 0f;
        foreach (float t in holdTimes) mean += t;
        mean /= holdTimes.Count;

        if (holdTimes.Count < 2)
        {
            return (mean, 0f);
        }
        
        float sumSquaredDiffs = 0f;
        foreach (float t in holdTimes)
        {
            float diff = t - mean;
            sumSquaredDiffs += diff * diff;
        }

        float stdDev = Mathf.Sqrt(sumSquaredDiffs / (holdTimes.Count - 1));
        
        return (mean, stdDev);
    }

    private static float CalculateMeanSeekTime(List<KeyboardTemporalSample> samples)
    {
        float totalSeekTime = 0f;
        int count = 0;
        foreach (var sample in samples)
        {
            if (sample.seekTime > 0f)
            {
                totalSeekTime += sample.seekTime;
                count++;
            }
        }
        return (count > 0) ? totalSeekTime / count : 0f;
    }

    private static float CalculateMeanLatency2(List<KeyboardTemporalSample> samples)
    {
        float totalLatency2 = 0f;
        int count = 0;
        foreach (var sample in samples)
        {
            if (sample.keyLatency2 > 0f)
            {
                totalLatency2 += sample.keyLatency2;
                count++;
            }
        }
        return (count > 0) ? totalLatency2 / count : 0f;
    }

    private static float CalculatePressDensity(List<KeyboardTemporalSample> samples)
    {
        if (samples.Count < 2)
            return 0f;

        float duration = samples[^1].timestamp - samples[0].timestamp;
        return (duration > 0f) ? samples.Count / duration : 0f;
    }

    private static List<float> CalculateDirectionalKeyBins(List<KeyboardTemporalSample> samples)
    {
        List<KeyCode> directionalKeys = new()
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        Dictionary<KeyCode, int> keyCounts = new();
        foreach (var key in directionalKeys)
        {
            keyCounts[key] = 0;
        }

        foreach (var sample in samples)
        {
            if (keyCounts.ContainsKey(sample.keyCode))
            {
                keyCounts[sample.keyCode]++;
            }
        }

        List<float> bins = new();
        int totalDirectionalKeyPresses = 0;
        foreach (var count in keyCounts.Values)
        {
            totalDirectionalKeyPresses += count;
        }

        foreach (var key in directionalKeys)
        {
            float frequency = (totalDirectionalKeyPresses > 0) ? (float)keyCounts[key] / totalDirectionalKeyPresses : 0f;
            bins.Add(frequency);
        }

        return bins;
    }
    #endregion
}
