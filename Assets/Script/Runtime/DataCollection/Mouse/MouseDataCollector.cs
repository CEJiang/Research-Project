using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDataCollector : Singleton<MouseDataCollector>
{
    private FirstPersonController playerController;
    private GameObject referencePoint;
    private List<MouseTemporalSample> temporalSamples = new();
    private List<MouseSpatialSample> spatialSamples = new();
    public bool isShowSpatialSampleEnabled = false;
    private Vector3 globalCenter;
    private bool isRecording = true;

    private List<MouseSpatialFeature> spatialFeatures = new();
    private List<MouseTemporalFeature> temporalFeatures = new();
    public List<MouseSpatialFeature> GetSpatialFeatures() => spatialFeatures;
    public List<MouseTemporalFeature> GetTemporalFeatures() => temporalFeatures;

    void Start()
    {
        Application.targetFrameRate = 60;
        playerController = FindObjectOfType<FirstPersonController>();
        referencePoint = FindObjectOfType<GazeReferencePoint>().gameObject;
        globalCenter = playerController.GetCameraPosition();

        spatialSamples.Clear();
        temporalSamples.Clear();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        FlushSpatialSegmentToLog();
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
    private float lastYaw, lastPitch;

    private void PerformTemporalSampling()
    {
        Vector2 delta = GameInput.CameraInput.MouseDelta.input;
        bool leftButtonReleased = GameInput.MouseInput.LeftButton.WasReleasedThisFrame;
        bool middleButtonReleased = GameInput.MouseInput.MiddleButton.WasReleasedThisFrame;
        bool rightButtonReleased = GameInput.MouseInput.RightButton.WasReleasedThisFrame;

        float yaw = playerController.GetYaw();
        float pitch = playerController.GetPitch();

        float deltaYaw = yaw - lastYaw;
        float deltaPitch = pitch - lastPitch;
        
        float angularSpeed = Mathf.Sqrt(
            Mathf.Pow(deltaYaw, 2) +
            Mathf.Pow(deltaPitch, 2)
        ) / Time.deltaTime;

        lastYaw = yaw;
        lastPitch = pitch;

        Vector3 direction = playerController.GetCameraForward().normalized;
        
        MouseTemporalSample mouseRawData = new()
        {
            timestamp = Time.time,
            delta = delta,
            position = referencePoint.transform.position,
            
            direction = direction,
            angularSpeed = angularSpeed,
            angle = Mathf.Atan2(deltaPitch, deltaYaw) * Mathf.Rad2Deg,

            yaw = yaw,
            pitch = pitch,
            
            leftButtonClickDuration = (leftButtonReleased ? GameInput.MouseInput.LeftButton.PressedDuration : 0f),
            middleButtonClickDuration = (middleButtonReleased ? GameInput.MouseInput.MiddleButton.PressedDuration : 0f),
            rightButtonClickDuration = (rightButtonReleased ? GameInput.MouseInput.RightButton.PressedDuration : 0f)
        };

        temporalSamples.Add(mouseRawData);
        UpdateTemporalWindow();
    }

    private void UpdateTemporalWindow()
    {
        if (temporalSamples.Count > Setup.MouseTemporalSampleBufferSize)
        {
            DataLogger<MouseTemporalSample>.LogData(temporalSamples[0], Setup.MouseTemporalDataFileName);
            temporalSamples.RemoveAt(0);
        }

        if (temporalSamples.Count >= Setup.MouseTemporalSampleBufferSize)
        {
            temporalFeatures.Add(MouseFeatureExtractor.ExtractTemporalFeatures(temporalSamples));
        }
    }

    private void FlushTemporalSegmentToLog()
    {
        while (temporalSamples.Count > 0)
        {
            DataLogger<MouseTemporalSample>.LogData(temporalSamples[0], Setup.MouseTemporalDataFileName);
            temporalSamples.RemoveAt(0);
        }
    }
    #endregion

    #region Spatial Sampling
    private Vector3 lastForward;
    private float accumulatedArc = 0f;
    [SerializeField] private float minAngleThreshold = 0.00087f; // ~0.05 degrees in radians
    private int sampleCount = 0;
    private int segmentID = 0;
    private float lastSegmentTimestamp = 0f;

    private void PerformSpatialSampling()
    {
        Vector3 dir = playerController.GetCameraForward().normalized;
        float dot = Mathf.Clamp(Vector3.Dot(lastForward, dir), -1f, 1f);
        float theta = Mathf.Acos(dot);

        if (theta < minAngleThreshold)
            return;

        float deltaArc = Setup.MouseSpatialSamplingRadius * theta;
        float targetArc = Setup.MouseSpatialSamplingArcLength;

        CheckSpatialSegmentTimeout();

        while (accumulatedArc + deltaArc >= targetArc)
        {
            float remaining = targetArc - accumulatedArc;
            float ratio = remaining / deltaArc;

            Vector3 sampleDir = Vector3.Slerp(lastForward, dir, ratio).normalized;

            Vector3 samplePoint = globalCenter + sampleDir * Setup.MouseSpatialSamplingRadius;

            MouseSpatialSample spatialSample = new()
            {
                timestamp = Time.time,
                position = samplePoint,
                yaw = playerController.GetYaw(),
                pitch = playerController.GetPitch(),
                direction = sampleDir
            };

            sampleCount++;

            if (sampleCount >= Setup.MouseSpatialSampleIgnoreCount)
            {
                spatialSamples.Add(spatialSample);
                
                if (isShowSpatialSampleEnabled)
                    SamplePoint.Instance.ShowSpatialSample(spatialSample.position, Color.blue);

                UpdateSpatialWindow();
            }

            lastForward = sampleDir;
            deltaArc -= remaining;
            accumulatedArc = 0f;

            lastSegmentTimestamp = Time.time;
        }

        accumulatedArc += deltaArc;
        lastForward = dir;
    }


    private void UpdateSpatialWindow()
    {
        if (spatialSamples.Count > Setup.MouseSpatialSampleBufferSize)
        {
            DataLogger<MouseSpatialSample>.LogData(spatialSamples[0], Setup.MouseSpatialDataFileName);
            spatialSamples.RemoveAt(0);
        }
        if (spatialSamples.Count > Setup.MouseSpatialSamplingCount)
        {
            int start = Mathf.Max(0, spatialSamples.Count - Setup.MouseSpatialSamplingCount);
            spatialFeatures.Add(MouseFeatureExtractor.ExtractSpatialFeatures(spatialSamples.GetRange(start, spatialSamples.Count - start), segmentID));
        }
    }

    private void CheckSpatialSegmentTimeout()
    {
        if (Time.time - lastSegmentTimestamp >= Setup.MouseSpatialSegmentTimeout)
        {
            FlushSpatialSegmentToLog();

            Vector3 dir = playerController.GetCameraForward().normalized;

            spatialSamples.Clear();
            accumulatedArc = 0f;
            lastForward = dir;
            lastSegmentTimestamp = Time.time;

            segmentID++;
        }
    }
    
    private void FlushSpatialSegmentToLog()
    {
        while (spatialSamples.Count > 0)
        {
            DataLogger<MouseSpatialSample>.LogData(spatialSamples[0], Setup.MouseSpatialDataFileName);
            spatialSamples.RemoveAt(0);
        }
    }

    #endregion

    #region Utility
    public void Reset()
    {
        spatialFeatures.Clear();
        temporalFeatures.Clear();
    }
    #endregion
}
