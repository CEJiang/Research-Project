using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -9.81f;
    public bool isMovable = true;

    [Header("Look")]
    [SerializeField] float sensitivity = 0.5f;
    [SerializeField] float verticalClamp = 80f;
    public bool isLookable = true;

    [Header("Interaction")]
    [SerializeField] float rayDistance = 3f;
    InteractableObject currentTarget;

    [Header("Camera Reference")]
    [SerializeField] Camera targetCamera;

    CharacterController controller;
    float verticalVelocity = 0f;
    float pitch = 0f;
    float yaw = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main;

        GameInput.CameraInput.MouseDelta.Set(Vector3.zero);
    }

    void Start()
    {
        // Initialize rotation
        Vector3 eulerAngles = targetCamera.transform.eulerAngles;
        pitch = eulerAngles.x;
        yaw = eulerAngles.y;
    }

    void FixedUpdate()
    {
        if (isMovable) HandleMovement();
    }

    void Update()
    {
        if (isLookable) HandleLook();
        HandleRaycast();
        HandleInteraction();
    }

    #region Movement
    void HandleMovement()
    {
        Vector2 input = GameInput.PlayerInput.MoveVector2.input;

        // Calculate move direction
        Vector3 moveDirection = Vector3.zero;
        Vector3 orientation = new Vector3(targetCamera.transform.forward.x, 0, targetCamera.transform.forward.z).normalized;

        moveDirection.x = input.x;
        moveDirection.z = input.y;
        moveDirection = Quaternion.LookRotation(orientation) * moveDirection;

        // Calculate vertical velocity
        verticalVelocity += gravity * Time.deltaTime;
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        // Apply movement
        Vector3 velocity = moveDirection * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    public void SetMovable(bool value)
    {
        isMovable = value;
    }
    #endregion

    #region Look
    void HandleLook()
    {
        Vector2 mouseDelta = GameInput.CameraInput.MouseDelta.input * sensitivity;

        // Calculate the new vertical and horizontal rotations
        yaw += mouseDelta.x;
        pitch -= mouseDelta.y;


        // Clamp the vertical rotation
        pitch = Mathf.Clamp(pitch, -verticalClamp, verticalClamp);

        // Apply the rotations
        targetCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void SetLookable(bool value)
    {
        isLookable = value;
    }
    #endregion

    #region Interaction
    void HandleRaycast()
    {
        Ray ray = targetCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            var target = hit.transform.GetComponent<InteractableObject>();
            SetCurrentTarget(target);
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    void HandleInteraction()
    {
        if (currentTarget != null && GameInput.GameplayInput.Interact.WasPressedThisFrame)
        {
            var actions = currentTarget.GetAvailableActions();
            if (actions.Count > 0)
                currentTarget.ExecuteAction(actions[0]); // TODO: Handle multiple actions
        }
    }

    void SetCurrentTarget(InteractableObject target)
    {
        if (currentTarget == target) return;

        ClearCurrentTarget();

        if (target == null) return;

        currentTarget = target;
        currentTarget.Highlight(true);
    }

    void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.Highlight(false);
            currentTarget = null;
        }
    }
    #endregion

    #region Getters
    public InteractableObject GetCurrentTarget()
    {
        return currentTarget;
    }
    public float GetYaw()
    {
        return yaw;
    }
    public float GetPitch()
    {
        return pitch;
    }
    public Vector3 GetCameraForward()
    {
        return targetCamera.transform.forward;
    }
    public Vector3 GetCameraPosition()
    {
        return targetCamera.transform.position;
    }
    #endregion

}