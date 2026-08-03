using UnityEngine;
using UnityEngine.UI;

public class ObjectPreviewManager : Singleton<ObjectPreviewManager>
{
    [Header("Preview References")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewRawImage;

    [Header("Layer")]
    [SerializeField] private string previewLayerName = "Preview";

    [Header("Input")]
    [SerializeField] private int orbitMouseButton = 0; // 左鍵旋轉
    [SerializeField] private int panMouseButton = 1;   // 右鍵平移

    [SerializeField] private float rotateSpeed = 4f;
    [SerializeField] private float zoomSpeed = 0.4f;
    [SerializeField] private float panSpeed = 0.02f;

    private GameObject currentPreviewObject;

    private ObjectPreviewRotationMode rotationMode;

    private Vector3 orbitCenterWorld;

    private float yaw;
    private float pitch;
    private float defaultPitch;

    private float minPitch;
    private float maxPitch;

    private float radius;
    private float minRadius;
    private float maxRadius;

    private bool allowZoom;
    private bool useCameraZoom;

    private float orthographicSize;
    private float minOrthographicSize;
    private float maxOrthographicSize;

    private bool allowPan;
    private float panX;
    private float panY;
    private float maxPanX;
    private float maxPanY;

    private bool isPreviewing;

    public bool IsPreviewing => isPreviewing;

    protected override void Awake()
    {
        base.Awake();

        if (previewCamera != null)
        {
            previewCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPreviewing || currentPreviewObject == null)
            return;

        HandleOrbitRotation();
        HandlePan();
        HandleZoom();
    }

    public void OpenPreview(SemanticActionObject actionObject)
    {
        if (actionObject == null)
        {
            Debug.LogWarning("[ObjectPreviewManager] actionObject is null.");
            return;
        }

        if (previewAnchor == null || previewCamera == null)
        {
            Debug.LogError("[ObjectPreviewManager] previewAnchor or previewCamera is not assigned.");
            return;
        }

        ClosePreview();

        ObjectPreviewConfig config = GetPreviewConfig(actionObject);

        GameObject prefabToUse = actionObject.gameObject;

        Vector3 localPosition = Vector3.zero;
        Vector3 localEulerAngles = Vector3.zero;
        Vector3 localScale = Vector3.one;
        Vector3 centerOffset = Vector3.zero;

        // Default settings
        rotationMode = ObjectPreviewRotationMode.YawOnly;

        yaw = 0f;
        pitch = 20f;
        defaultPitch = 20f;

        minPitch = 5f;
        maxPitch = 60f;

        allowZoom = true;
        useCameraZoom = true;

        radius = 3f;
        minRadius = 1.2f;
        maxRadius = 6f;

        orthographicSize = 2.5f;
        minOrthographicSize = 0.8f;
        maxOrthographicSize = 5f;

        allowPan = true;

        panX = 0f;
        panY = 0f;

        maxPanX = 1.0f;
        maxPanY = 1.0f;

        if (config != null)
        {
            if (config.previewPrefab != null)
                prefabToUse = config.previewPrefab;

            localPosition = config.previewLocalPosition;
            localEulerAngles = config.previewLocalEulerAngles;
            localScale = config.previewLocalScale;
            centerOffset = config.centerOffset;

            rotationMode = config.rotationMode;

            yaw = config.startYaw;
            pitch = config.startPitch;
            defaultPitch = config.startPitch;

            minPitch = config.minPitch;
            maxPitch = config.maxPitch;

            allowZoom = config.allowZoom;

            orthographicSize = config.startOrthographicSize;
            minOrthographicSize = config.minOrthographicSize;
            maxOrthographicSize = config.maxOrthographicSize;

            allowPan = config.allowPan;
            maxPanX = config.maxPanX;
            maxPanY = config.maxPanY;
        }

        radius = Mathf.Clamp(radius, minRadius, maxRadius);
        orthographicSize = Mathf.Clamp(orthographicSize, minOrthographicSize, maxOrthographicSize);

        MeshFilter[] allMeshFilters = prefabToUse.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in allMeshFilters)
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                // 第二個參數傳進 mf.gameObject，點擊 Console 視窗可以直接在 Hierarchy 高亮該物件！
                Debug.LogError(
                    $"[Preview Diagnostics] 抓到了！物件 '{GetGameObjectPath(mf.gameObject)}' " +
                    $"採用的 Mesh ('{mf.sharedMesh.name}') 沒有開啟 Read/Write 或是被 Static Batching 合併了！", 
                    mf.gameObject
                );
            }
        }
        
        currentPreviewObject = Instantiate(prefabToUse, previewAnchor);

        currentPreviewObject.transform.localPosition = localPosition;
        currentPreviewObject.transform.localRotation = Quaternion.Euler(localEulerAngles);
        currentPreviewObject.transform.localScale = localScale;

        DisablePreviewInteraction(currentPreviewObject);
        DisablePreviewBehaviours(currentPreviewObject);
        SetLayerRecursively(currentPreviewObject, LayerMask.NameToLayer(previewLayerName));

        orbitCenterWorld = CalculateModelCenter(currentPreviewObject) + centerOffset;

        isPreviewing = true;

        previewCamera.gameObject.SetActive(true);

        ApplyCameraZoom();
        UpdateCameraOrbit();

        Debug.Log(
            $"[ObjectPreview] Opened. Config={(config != null)}, " +
            $"Radius={radius}, OrthoSize={orthographicSize}, " +
            $"CameraDistance={Vector3.Distance(previewCamera.transform.position, orbitCenterWorld)}"
        );
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    public void ClosePreview()
    {
        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
            currentPreviewObject = null;
        }

        isPreviewing = false;

        panX = 0f;
        panY = 0f;

        if (previewCamera != null)
        {
            previewCamera.gameObject.SetActive(false);
        }
    }

    private ObjectPreviewConfig GetPreviewConfig(SemanticActionObject actionObject)
    {
        ObjectPreviewConfig config = actionObject.GetComponent<ObjectPreviewConfig>();

        if (config != null)
            return config;

        config = actionObject.GetComponentInChildren<ObjectPreviewConfig>(true);

        if (config != null)
            return config;

        return actionObject.GetComponentInParent<ObjectPreviewConfig>();
    }

    private void HandleOrbitRotation()
    {
        if (!Input.GetMouseButton(orbitMouseButton))
            return;

        if (!IsMouseOverPreview())
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * rotateSpeed;

        if (rotationMode == ObjectPreviewRotationMode.YawPitchLimited)
        {
            pitch -= mouseY * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            pitch = defaultPitch;
        }

        UpdateCameraOrbit();
    }

    private void HandlePan()
    {
        if (!allowPan)
            return;

        if (!Input.GetMouseButton(panMouseButton))
            return;

        if (!IsMouseOverPreview())
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        panX -= mouseX * panSpeed;
        panY -= mouseY * panSpeed;

        panX = Mathf.Clamp(panX, -maxPanX, maxPanX);
        panY = Mathf.Clamp(panY, -maxPanY, maxPanY);

        UpdateCameraOrbit();
    }

    private void HandleZoom()
    {
        if (!allowZoom)
            return;

        if (!IsMouseOverPreview())
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        radius -= scroll * zoomSpeed;
        radius = Mathf.Clamp(radius, minRadius, maxRadius);

        if (useCameraZoom && previewCamera != null)
        {
            if (previewCamera.orthographic)
            {
                orthographicSize -= scroll * zoomSpeed;
                orthographicSize = Mathf.Clamp(
                    orthographicSize,
                    minOrthographicSize,
                    maxOrthographicSize
                );
            }

            ApplyCameraZoom();
        }

        UpdateCameraOrbit();
    }

    private void UpdateCameraOrbit()
    {
        if (previewCamera == null)
            return;

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset = orbitRotation * new Vector3(0f, 0f, -radius);

        Vector3 basePosition = orbitCenterWorld + offset;

        previewCamera.transform.position = basePosition;
        previewCamera.transform.LookAt(orbitCenterWorld);

        Vector3 screenRight = previewCamera.transform.right;
        Vector3 screenUp = previewCamera.transform.up;

        Vector3 target =
            orbitCenterWorld
            + screenRight * panX
            + screenUp * panY;

        previewCamera.transform.position = target + offset;
        previewCamera.transform.LookAt(target);
    }

    private void ApplyCameraZoom()
    {
        if (previewCamera == null)
            return;

        if (previewCamera.orthographic)
        {
            previewCamera.orthographicSize = orthographicSize;
        }
    }

    private Vector3 CalculateModelCenter(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[ObjectPreviewManager] No Renderer found. Use previewAnchor position as orbit center.");
            return previewAnchor.position;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    private bool IsMouseOverPreview()
    {
        if (previewRawImage == null)
            return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            previewRawImage.rectTransform,
            Input.mousePosition
        );
    }

    private void DisablePreviewInteraction(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);

        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void DisablePreviewBehaviours(GameObject obj)
    {
        MonoBehaviour[] behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var behaviour in behaviours)
        {
            behaviour.enabled = false;
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0)
        {
            Debug.LogWarning($"[ObjectPreviewManager] Layer '{previewLayerName}' does not exist.");
            return;
        }

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}