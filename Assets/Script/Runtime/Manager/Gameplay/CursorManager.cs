using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;

public class CursorManager : Singleton<CursorManager>
{
    private bool isPaused = true;
    private FirstPersonController firstPersonController;
    
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        firstPersonController = FindObjectOfType<FirstPersonController>();

        isPaused = true;
        SetCursorState(isPaused);
        firstPersonController.SetOpenableUI(!isPaused);
    }

    void Update()
    {
        HandleEscape();
    }

    public void HandleEscape()
    {
        if (GameInput.UIInput.Escape.WasPressedThisFrame)
        {
            if (UIManager.Instance.GetCurrentOpenUI() == UIManager.OpenUIType.PlayerReasoning && RelationGraphManager.Instance.firstNode != null)
            {
                RelationGraphManager.Instance.firstNode = null;
                RelationGraphManager.Instance.relationGraphPreviewEdge.EndPreview();

                return;
            }

            if (UIManager.Instance.GetCurrentOpenUI() == UIManager.OpenUIType.PlayerReasoning && RelationGraphManager.Instance.currentRelationGraphType != RelationGraphType.NONE)
            {
                RelationGraphManager.Instance.SetCurrentRelationGraphType(RelationGraphType.NONE);
                return;
            }

            if (UIManager.Instance.uiStack.Count > 0)
            {
                UIManager.Instance.CloseTopUI();
                return;
            }

            isPaused = true;
            SetCursorState(isPaused);
            firstPersonController.SetOpenableUI(!isPaused);
        }
        
        if (isPaused && GameInput.MouseInput.LeftButton.WasPressedThisFrame)
        {
            isPaused = false;
            SetCursorState(isPaused);
            firstPersonController.SetOpenableUI(!isPaused);
        }

        
    }
    
    public void SetCursorState(bool isPaused)
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
        firstPersonController.SetInteractable(!isPaused);
    }
}
