using UnityEngine;

public enum ObjectPreviewRotationMode
{
    YawOnly,
    YawPitchLimited
}

public class ObjectPreviewConfig : MonoBehaviour
{
    [Header("Preview Source")]
    public GameObject previewPrefab;

    [Header("Preview Transform")]
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEulerAngles = Vector3.zero;
    public Vector3 previewLocalScale = Vector3.one;

    [Header("Orbit Center")]
    public Vector3 centerOffset = Vector3.zero;

    [Header("Camera Orbit")]
    public ObjectPreviewRotationMode rotationMode = ObjectPreviewRotationMode.YawOnly;

    public float startYaw = 0f;
    public float startPitch = 20f;

    public float minPitch = 5f;
    public float maxPitch = 60f;

    [Header("Camera Distance")]
    public float orbitDistance = 3f;

    [Header("Zoom")]
    public bool allowZoom = true;
    public float startOrthographicSize = 2.5f;
    public float minOrthographicSize = 0.8f;
    public float maxOrthographicSize = 5f;

    [Header("Pan")]
    public bool allowPan = true;
    public float maxPanX = 1.0f;
    public float maxPanY = 1.0f;
}