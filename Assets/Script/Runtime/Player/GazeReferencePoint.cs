using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeReferencePoint : MonoBehaviour
{
    [SerializeField] Camera targetCamera;

    void Awake()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * Setup.MouseSpatialSamplingRadius;
        transform.rotation = targetCamera.transform.rotation;
    }
}
