using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardDataCollector : Singleton<KeyboardDataCollector>
{
    private FirstPersonController playerController;
    private List<KeyboardTemporalSample> temporalSamples = new();
    private List<KeyboardSpatialSample> spatialSamples = new();
    private bool isRecording = true;
    public bool isShowSpatialSampleEnabled = false;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;
    private int segmentID = 0;

    private List<KeyboardSegmentSpatialFeature> spatialFeatures = new();
    private KeyboardTemporalFeature temporalFeatures = new();
    

    void Start()
    {
        Application.targetFrameRate = 60;
        playerController = FindObjectOfType<FirstPersonController>();

        temporalSamples.Clear();

        lastPosition = playerController.transform.position;
        ResetSpatialSegment();
        
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        FlushTemporalSegmentToLog();
    }
    
    void LateUpdate()
    {
        if (isRecording)
        {
            PerformTemporalSampling();
            PerformSpatialSampling();
        }
    }

    public void SetIsRecording(bool isRecording)
    {
        this.isRecording = isRecording;
    }

    #region Temporal Sampling
    private float prevKeyDownTime = 0f;
    private float prev2KeyDownTime = 0f;
    private float prevKeyUpTime = 0f;

    private bool hasPrevKeyDown = false;
    private bool hasPrev2KeyDown = false;

    private void PerformTemporalSampling()
    {
        foreach (var button in GameInput.GetInputButtons)
        {
            if (button.WasPressedThisFrame)
                RecordKeyboardPressedEvent(button);
            else if (button.WasReleasedThisFrame)
                RecordKeyboardReleasedEvent(button);
        }
    }

    private void RecordKeyboardPressedEvent(InputButton button)
    {
        float now = Time.time;

        float seekTime = hasPrevKeyDown ? (now - prevKeyUpTime) : 0f;

        KeyboardTemporalSample sample = new()
        {
            timestamp = now,
            keyCode = button.keyCode,
            holdTime = 0f,
            seekTime = seekTime,
            keyLatency2 = 0f,
            keyLatency3 = 0f
        };

        temporalSamples.Add(sample);

        prev2KeyDownTime = prevKeyDownTime;
        prevKeyDownTime = now;

        hasPrev2KeyDown = hasPrevKeyDown;
        hasPrevKeyDown = true;
    }

    private void RecordKeyboardReleasedEvent(InputButton button)
    {
        float now = Time.time;

        float latency2 = hasPrevKeyDown ? (now - prevKeyDownTime) : 0f;
        float latency3 = hasPrev2KeyDown ? (now - prev2KeyDownTime) : 0f;

        KeyboardTemporalSample sample = new()
        {
            timestamp = now,
            keyCode = button.keyCode,
            holdTime = button.PressedDuration,
            seekTime = 0f,
            keyLatency2 = latency2,
            keyLatency3 = latency3
        };

        temporalSamples.Add(sample);

        prevKeyUpTime = now;
    }

    private void FlushTemporalSegmentToLog()
    {
        while (temporalSamples.Count > 0)
        {
            DataLogger<KeyboardTemporalSample>.LogData(temporalSamples[0], Setup.KeyboardTemporalDataFileName);
            temporalSamples.RemoveAt(0);    
        }
    }
    #endregion

    #region Spatial Sampling
    private void PerformSpatialSampling()
    {
        Vector3 currentPosition = playerController.transform.position;

        float delta = Vector3.Distance(currentPosition, lastPosition);
        float targetDistance = Setup.KeyboardSpatialSamplingArcLength;

        if (delta < 0.001f)
            return;

        while (accumulatedDistance + delta >= targetDistance)
        {
            float remaining = targetDistance - accumulatedDistance;
            float ratio = remaining / delta;

            Vector3 samplePosition = Vector3.Lerp(lastPosition, currentPosition, ratio);

            KeyboardSpatialSample sample = new()
            {
                timestamp = Time.time,
                position = samplePosition
            };

            spatialSamples.Add(sample);
            
            if (isShowSpatialSampleEnabled)
                SamplePoint.Instance.ShowSpatialSample(sample.position, Color.yellow);

            accumulatedDistance = 0f;
            lastPosition = samplePosition;
            delta -= remaining;
        }

        accumulatedDistance += delta;
        lastPosition = currentPosition;
    }

    // Call this method to finalize the current spatial segment
    // If touch Interactable object, call this method to finalize the current spatial segment
    public void FlushSpatialSegment()
    {
        KeyboardSegmentSpatialSample keyboardSegmentSpatialSample = new KeyboardSegmentSpatialSample()
        {
            segmentID = segmentID,
            startPosition = spatialSamples.Count > 0 ? spatialSamples[0].position : Vector3.zero,
            endPosition = spatialSamples.Count > 0 ? spatialSamples[^1].position : Vector3.zero,
            startTime = spatialSamples.Count > 0 ? spatialSamples[0].timestamp : 0f,
            endTime = spatialSamples.Count > 0 ? spatialSamples[^1].timestamp : 0f,
            spatialSamples = new List<KeyboardSpatialSample>(spatialSamples)
        };

        DataLogger<KeyboardSegmentSpatialSample>.LogData(keyboardSegmentSpatialSample, Setup.KeyboardSpatialDataFileName);
        spatialFeatures.Add(KeyboardFeatureExtractor.ExtractSpatialFeatures(keyboardSegmentSpatialSample));
        ResetSpatialSegment();
        segmentID++;
    }

    private void ResetSpatialSegment()
    {
        spatialSamples.Clear();
        accumulatedDistance = 0f;

        spatialSamples.Add(new KeyboardSpatialSample()
        {
            timestamp = Time.time,
            position = lastPosition
        }); 
    }

    #endregion

    #region Getters
    public List<KeyboardSegmentSpatialFeature> GetSpatialFeatures() => spatialFeatures;
    public KeyboardTemporalFeature GetTemporalFeatures()
    {
        if(temporalSamples.Count > 0)
            temporalFeatures = KeyboardFeatureExtractor.ExtractTemporalFeatures(temporalSamples);
        
        FlushTemporalSegmentToLog();
        temporalSamples.Clear();
            
        return temporalFeatures;   
    }
    #endregion

    #region Utility
    public void Reset()
    {
        spatialFeatures.Clear();
        temporalFeatures = null;
    }
    #endregion
}
