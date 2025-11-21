using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Setup
{
    #region General Settings

    /// <summary>
    /// Data folder name.
    /// </summary>
    public const string DataFolderName = "Jiang";

    #endregion

    #region Mouse Spatial Data Collection Settings

    /// <summary>
    /// Mouse Distance Sampling Count.
    /// </summary>
    public static readonly int MouseSpatialSamplingCount = 10;

    /// <summary>
    /// Mouse spatial sampling distance start index.
    /// </summary>
    public static readonly int MouseSpatialSamplingDistStart = 3;
    /// <summary>
    /// Mouse spatial sampling distance stride.
    /// </summary>
    public static readonly int MouseSpatialSamplingDistStride = 2;

    /// <summary>
    /// Mouse spatial sampling arc length threshold.
    /// </summary>
    public static readonly float MouseSpatialSamplingArcLength = 0.25f;

    /// <summary>
    /// Mouse spatial sampling radius.
    /// </summary>
    public static readonly float MouseSpatialSamplingRadius = 2f;
    
    /// <summary>
    /// Mouse spatial sample buffer size.
    /// </summary>
    public static readonly int MouseSpatialSampleBufferSize = 50;

    /// <summary>
    /// Number of initial Mouse spatial samples to ignore.
    /// </summary>
    public static readonly int MouseSpatialSampleIgnoreCount = 10;
    /// <summary>
    /// Mouse spatial data file name.
    /// </summary>
    public static readonly string MouseSpatialDataFileName = "MouseSpatialData.json";

    /// <summary>
    /// The timeout in seconds for a Mouse spatial segment.
    /// </summary>
    public static readonly float MouseSpatialSegmentTimeout = 2.0f;

    #endregion

    #region Mouse Spatial Feature Extraction Settings
    public static readonly string MouseSpatialFeatureDataFileName = "MouseSpatialFeatureData.json";
    
    #endregion

    #region Mouse Temporal Data Collection Settings
    public static readonly int MouseTemporalSampleBufferSize = 60;
    public static readonly string MouseTemporalDataFileName = "MouseTemporalData.json";
    #endregion

    #region Mouse Temporal Feature Extraction Settings
    public static readonly string MouseTemporalFeatureDataFileName = "MouseTemporalFeatureData.json";
    #endregion

    #region  Keyboard Spatial Data Collection Settings
    /// <summary>
    /// Mouse spatial sampling arc length threshold.
    /// </summary>
    public static readonly float KeyboardSpatialSamplingArcLength = 0.1f;
    public static readonly string KeyboardSpatialDataFileName = "KeyboardSpatialData.json";

    #endregion

    # region Keyboard Spatial Feature Extraction Settings
    public static readonly string KeyboardSpatialFeatureDataFileName = "KeyboardSpatialFeatureData.json";
    #endregion

    #region Keyboard Temporal Data Collection Settings
    public static readonly int KeyboardTemporalSampleBufferSize = 10;
    public static readonly int KeyboardTemporalWindowSize = 10;
    public static readonly string KeyboardTemporalDataFileName = "KeyboardTemporalData.json";
    #endregion

    #region Keyboard Temporal Feature Extraction Settings
    public static readonly string KeyboardTemporalFeatureDataFileName = "KeyboardTemporalFeatureData.json";
    #endregion

    #region Logging Settings
    /// <summary>
    /// Log folder name.
    /// </summary>
    public static readonly string LogFolder = "Logs";

    /// <summary>
    /// Data logging interval in seconds.
    /// </summary>
    public static readonly float DataLoggingInterval = 2.0f;

    /// <summary>
    /// Max number of data buffers.
    /// </summary>
    public static readonly int MaxDataBuffers = 20;
    public static readonly string KeyboardSegmentSpatialDataFileName = "KeyboardSegmentSpatialData.json";

    #endregion
}
