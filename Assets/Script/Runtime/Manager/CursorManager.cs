using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;

public class CursorManager : Singleton<CursorManager>
{
    private bool isPaused = true;
    private FirstPersonController firstPersonController;
    
    void Start()
    {
        firstPersonController = FindObjectOfType<FirstPersonController>();

        isPaused = true;
        SetCursorState(isPaused);
    }

    void Update()
    {
        HandleEscape();
    }

    private void HandleEscape()
    {
        if (GameInput.UIInput.Escape.WasPressedThisFrame)
        {
            isPaused = true;
            SetCursorState(isPaused);
        }
        
        if (isPaused && GameInput.MouseInput.LeftButton.WasPressedThisFrame)
        {
            isPaused = false;
            SetCursorState(isPaused);
        }
    }
    
    private void SetCursorState(bool isPaused)
    {
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        firstPersonController.SetLookable(!isPaused);
        firstPersonController.SetMovable(!isPaused);
        MouseDataCollector.Instance.SetIsRecording(!isPaused);
        KeyboardDataCollector.Instance.SetIsRecording(!isPaused);

    }
}
