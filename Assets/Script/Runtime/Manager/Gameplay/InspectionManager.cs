using UnityEngine;

public class InspectionManager : Singleton<InspectionManager>
{
    [Header("Inspect Setup")]
    [SerializeField] private Transform inspectAnchor;

    [Header("Optional")]
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.15f;
    [SerializeField] private float minZoomZ = 0.2f;
    [SerializeField] private float maxZoomZ = 1.2f;

    [Header("Pan")]
    [SerializeField] private float panSpeed = 0.0025f;
    [SerializeField] private float maxPanX = 0.4f;
    [SerializeField] private float maxPanY = 0.3f;

    private GameObject currentInspectedObject;
    private bool isInspecting;

    private Vector3 initialLocalPosition;
    private Vector3 initialLocalEulerAngles;
    private Vector3 initialLocalScale;

    public bool IsInspecting => isInspecting;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (!isInspecting || currentInspectedObject == null) return;

        if (GameInput.UIInput.Escape.WasPressedThisFrame)
        {
            CloseInspect();
            return;
        }

        HandleZoom();
        HandlePan();
    }

    public void ShowInspectObject(GameObject prefabOrSource, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        if (inspectAnchor == null)
        {
            Debug.LogError("InspectionManager: inspectAnchor is not assigned.");
            return;
        }

        if (prefabOrSource == null)
        {
            Debug.LogWarning("InspectionManager: prefabOrSource is null.");
            return;
        }

        if (isInspecting)
        {
            CloseInspect();
        }

        currentInspectedObject = Instantiate(prefabOrSource, inspectAnchor);
        currentInspectedObject.transform.localPosition = localPosition;
        currentInspectedObject.transform.localRotation = Quaternion.Euler(localEulerAngles);
        currentInspectedObject.transform.localScale = localScale;

        initialLocalPosition = localPosition;
        initialLocalEulerAngles = localEulerAngles;
        initialLocalScale = localScale;

        isInspecting = true;
        CursorManager.Instance.SetCursorState(true);
    }

    public void CloseInspect()
    {
        if (currentInspectedObject != null)
        {
            Destroy(currentInspectedObject);
            currentInspectedObject = null;
        }

        isInspecting = false;
        CursorManager.Instance.SetCursorState(false);
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        Transform t = currentInspectedObject.transform;
        Vector3 pos = t.localPosition;

        pos.z += scroll * zoomSpeed * -1;
        pos.z = Mathf.Clamp(pos.z, minZoomZ, maxZoomZ);

        t.localPosition = pos;
    }

    private void HandlePan()
    {
        if (!Input.GetMouseButton(0)) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Transform t = currentInspectedObject.transform;
        Vector3 pos = t.localPosition;

        pos.x += mouseX * panSpeed;
        pos.y += mouseY * panSpeed;

        float minX = initialLocalPosition.x - maxPanX;
        float maxX = initialLocalPosition.x + maxPanX;
        float minY = initialLocalPosition.y - maxPanY;
        float maxY = initialLocalPosition.y + maxPanY;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        t.localPosition = pos;
    }
}