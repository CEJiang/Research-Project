using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MouseDataCollector))]
[RequireComponent(typeof(KeyboardDataCollector))]
public class AnalysisManager : Singleton<AnalysisManager>
{
    private MouseDataCollector mouseDataCollector;
    private KeyboardDataCollector keyboardDataCollector;
    private float analysisInterval = 1.0f; 
    private float lastAnalysisTime;

    void Start()
    {
        mouseDataCollector = GetComponent<MouseDataCollector>();
        keyboardDataCollector = GetComponent<KeyboardDataCollector>();
        lastAnalysisTime = Time.time;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DataLogger<MouseSpatialFeature>.FlushToDisk();
        DataLogger<MouseTemporalFeature>.FlushToDisk();
        DataLogger<KeyboardSegmentSpatialFeature>.FlushToDisk();
        DataLogger<KeyboardTemporalFeature>.FlushToDisk();
    }

    void LateUpdate()
    {
        if(Time.time - lastAnalysisTime >= analysisInterval)
        {
            PerformAnalysis();
            lastAnalysisTime = Time.time;
        }
    }
    private void PerformAnalysis()
    {
        var mouseTemporalFeatures = mouseDataCollector.GetTemporalFeatures();
        var mouseSpatialFeatures = mouseDataCollector.GetSpatialFeatures();
        var keyboardTemporalFeatures = keyboardDataCollector.GetTemporalFeatures();
        var keyboardSpatialFeatures = keyboardDataCollector.GetSpatialFeatures();
        mouseDataCollector.Reset();
        keyboardDataCollector.Reset();

    }
}
