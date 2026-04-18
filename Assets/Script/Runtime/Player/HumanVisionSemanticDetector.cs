using System.Collections.Generic;
using UnityEngine;

public class HumanVisionSemanticDetector : MonoBehaviour
{
    public Camera playerCamera;
    public float scanRadius = 6f;
    public LayerMask semanticMask;
    public LayerMask ignoreMask;

    private float fovealAngle = 5f;   // 固定，符合生理特徵
    public float paraFovealAngle;    // 動態依照 FOV 設定
    public float peripheralAngle;    // 動態依照 FOV 設定

    HashSet<SemanticActionObject> previousDetectedObjects = new();
    Dictionary<SemanticActionObject, VisionState> objectVisionStates = new();
    Dictionary<SemanticActionObject, float?> objectDetectedTime = new();
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        float verticalFOV = playerCamera.fieldOfView;
        float horizontalFOV = 2f * Mathf.Atan(
            Mathf.Tan(verticalFOV * 0.5f * Mathf.Deg2Rad) * playerCamera.aspect
        ) * Mathf.Rad2Deg;

        paraFovealAngle = verticalFOV * 0.25f;     // 15°
        peripheralAngle = horizontalFOV * 0.45f;   // 27° for 60° FOV
    }

    void Update()
    {
        DetectVision();
    }

    void DetectVision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, semanticMask);
        HashSet<SemanticActionObject> detectedThisFrame = new();
        
        foreach (var col in hits)
        {
            SemanticActionObject obj = col.GetComponent<SemanticActionObject>();
            if (obj == null || obj.isChecked) continue;

            Vector3 closestPoint = col.ClosestPoint(playerCamera.transform.position);
            Vector3 dir = (closestPoint - playerCamera.transform.position).normalized;

            Vector3 vp = playerCamera.WorldToViewportPoint(closestPoint);

            if (vp.z <= 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1)
                continue;

            if (!Physics.Raycast(playerCamera.transform.position, dir, out RaycastHit hit, scanRadius, ~ignoreMask))
                continue;

            if (hit.collider != col)
                continue;

            detectedThisFrame.Add(obj);
            obj.memoryStrength += Time.deltaTime * 0.1f;
        }

        foreach (var obj in detectedThisFrame)
        {
            Vector3 closestPoint = obj.GetComponent<Collider>().ClosestPoint(playerCamera.transform.position);
            VisionState currentState = GetVisionState(
                Vector3.Angle(
                    playerCamera.transform.forward,
                    (closestPoint - playerCamera.transform.position).normalized
                )
            );
            
            if (!previousDetectedObjects.Contains(obj))
            {
                if (currentState == VisionState.Focus)
                    objectDetectedTime[obj] = Time.time;

                RecordVisionAction(currentState, obj);
            }
            else if (objectVisionStates.ContainsKey(obj))
            {
                VisionState previousState = objectVisionStates[obj];

                // Peripheral -> Notice -> Focus
                if (currentState > previousState)
                {
                    RecordVisionAction(currentState, obj);
                }

                if (previousState == VisionState.Focus && currentState != VisionState.Focus)
                {
                    if (objectDetectedTime[obj] != null)
                        LogSemantic(VisionState.FocusToLost, obj);
                        
                    objectDetectedTime[obj] = null;
                }
                else if (previousState != VisionState.Focus && currentState == VisionState.Focus)
                {
                    objectDetectedTime[obj] = Time.time;
                }
            }
        }

        foreach (var obj in previousDetectedObjects)
        {
            if (!detectedThisFrame.Contains(obj))
            {
                objectVisionStates[obj] = VisionState.Lost;
                objectDetectedTime[obj] = null;
            }
        }

        previousDetectedObjects = detectedThisFrame;
    }

    void RecordVisionAction(VisionState visionState, SemanticActionObject obj)
    {
        if (visionState != VisionState.Lost)
        {
            LogSemantic(visionState, obj);
            objectVisionStates[obj] = visionState;
        }
    }

    VisionState GetVisionState(float angle)
    {
        if (angle <= fovealAngle)
            return VisionState.Focus;
        else if (angle <= paraFovealAngle)
            return VisionState.Notice;
        else if (angle <= peripheralAngle)
            return VisionState.Peripheral;
        else
            return VisionState.Lost;
    }

    readonly Dictionary<ObjectSignificance, string> significanceDescriptions = new()
    {
        { ObjectSignificance.Critical, "This object holds high narrative relevance." },
        { ObjectSignificance.Supportive, "This object provides supplementary narrative context." },
        { ObjectSignificance.Ambient, "This object primarily contributes to environmental atmosphere." }
    };

    string GetMemoryTag(float value)
    {
        return value switch
        {
            < 0.3f => "Its accumulated visual presence remains minimal.",
            < 0.7f => "Its repeated on-screen appearance has established moderate latent salience.",
            _      => "Its persistent on-screen presence has resulted in high latent salience.",
        };
    }

    void LogSemantic(VisionState state, SemanticActionObject obj)
    {
        string message = state switch
        {
            VisionState.FocusToLost =>
                $"{obj.displayNameEn} was no longer present near the center of the screen after remaining prominently visible for {(Time.time - objectDetectedTime[obj]):F2} seconds.",

            VisionState.Focus =>
                $"{obj.displayNameEn} remained prominently positioned near the center of the screen, indicating sustained on-screen presence.",

            VisionState.Notice =>
                $"{obj.displayNameEn} briefly entered the central area of the screen without prolonged on-screen exposure.",

            VisionState.Peripheral =>
                $"{obj.displayNameEn} appeared within the peripheral region of the screen, without occupying the central view.",

            _ => ""
        };
        message += $" {significanceDescriptions[obj.significance]} {GetMemoryTag(obj.memoryStrength)}";

        _ = SemanticActionManager.Instance.LogSemanticAction(
            new SemanticAction(Time.time, ActionType.PlayerObject, message)
        );

        Debug.Log(message);
    }
}
