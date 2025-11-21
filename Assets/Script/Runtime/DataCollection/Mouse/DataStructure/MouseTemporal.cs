using UnityEngine;

public class MouseTemporalSample
{
    /// <summary>
    /// Timestamp of the sample.
    /// </summary>
    public float timestamp;

    /// <summary>
    /// The change in mouse position.
    /// </summary>
    public Vector2 delta;

    /// <summary>
    /// The world position of the reference point.
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// The yaw of the player at the time of the sample.
    /// </summary>
    public float yaw;

    /// <summary>
    /// The pitch of the player at the time of the sample.
    /// </summary>
    public float pitch;

    public Vector3 direction;

    /// <summary>
    /// The angular speed of the mouse movement.
    /// </summary>
    public float angularSpeed;

    /// <summary>
    /// The angle of the mouse movement.
    /// </summary>
    public float angle;
    public int clickCount;
    public bool isFixating;

    /// <summary>
    /// Duration the left mouse button was held down.
    /// </summary>
    public float leftButtonClickDuration;

    /// <summary>
    /// Duration the middle mouse button was held down.
    /// </summary>
    public float middleButtonClickDuration;

    /// <summary>
    /// Duration the right mouse button was held down.
    /// </summary>
    public float rightButtonClickDuration;
}

public class MouseTemporalFeature
{
    public float startTime;
    public float endTime;
    public float angularSpeedMean;
    public float angularSpeedStd;
    public float angleStd;
    public float yawRange;
    public float pitchRange;
    public float fixationTime;
    public int fixationCount;
    public float clickDensity;
    public float gazeDispersion;
}
