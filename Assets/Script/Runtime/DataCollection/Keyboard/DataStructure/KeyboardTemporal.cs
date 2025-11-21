using System.Collections.Generic;
using UnityEngine;

public class KeyboardTemporalSample
{
    /// <summary>
    /// Timestamp of the sample.
    /// </summary>
    public float timestamp;
    public KeyCode keyCode;
    public float holdTime;
    public float seekTime;
    public float keyLatency2;
    public float keyLatency3;
}

public class KeyboardTemporalFeature
{
    public float startTime;
    public float endTime;

    // --- 按鍵時長統計 (Hold Time) ---
    /// <summary>
    /// 平均按鍵按住時長 (Mean Key Hold Time)
    /// </summary>
    public float meanHoldTime;
    
    /// <summary>
    /// 按鍵時長標準差 (Std Dev of Key Hold Time)
    /// </summary>
    public float stdDevHoldTime;

    // --- 按鍵間隔統計 (Seek Time & Latency) ---
    /// <summary>
    /// 平均按鍵間隔時間 (Mean Seek Time)
    /// </summary>
    public float meanSeekTime;

    /// <summary>
    /// 平均按鍵延遲 (Mean Key Latency 2)
    /// </summary>
    public float meanLatency2;

    // --- 按鍵頻率統計 ---
    /// <summary>
    /// 按鍵總次數 / 時間窗長度 (Key Press Count Rate)
    /// </summary>
    public float pressDensity;

    // --- 特定按鍵分佈 (Directional Key Usage) ---
    /// <summary>
    /// WASD (或上下左右) 的使用頻率分佈 (向量)
    /// </summary>
    public List<float> directionalKeyBins;
}
