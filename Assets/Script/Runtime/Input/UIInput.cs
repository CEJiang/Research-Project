using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIInput : PlayerControls.IUIActions
{
    private PlayerControls playerControls;
    public InputButton Escape = new();
    public InputButton Inventory = new();
    public readonly InputButton PlayerReasoning = new();
    public readonly InputButton RelationGraphDelete = new();
    public readonly InputButton Camera = new();

    public UIInput()
    {
        playerControls = new PlayerControls();
        playerControls.UI.SetCallbacks(this);
        playerControls.Enable();
    }

    public void ResetFrameFlags()
    {
        Escape.ResetFrameFlags();
        Inventory.ResetFrameFlags();
        PlayerReasoning.ResetFrameFlags();
        RelationGraphDelete.ResetFrameFlags();
        Camera.ResetFrameFlags();
    }

    #region Escape Button

    public void OnEsc(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Escape.Press(); else Escape.Release();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Inventory.Press(); else Inventory.Release();
    }

    public void OnPlayerReasoning(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) PlayerReasoning.Press(); else PlayerReasoning.Release();
    }

    public void OnRelationGraph(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) RelationGraphDelete.Press(); else RelationGraphDelete.Release();
    }

    public void OnCamera(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        
        if (isPressed) Camera.Press(); else Camera.Release();
    }


    #endregion
}
