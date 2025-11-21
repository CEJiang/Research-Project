using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIInput : PlayerControls.IUIActions
{
    private PlayerControls playerControls;
    public InputButton Escape = new(KeyCode.Escape);

    public UIInput()
    {
        playerControls = new PlayerControls();
        playerControls.UI.SetCallbacks(this);
        playerControls.Enable();
    }

    public void ResetFrameFlags()
    {
        Escape.ResetFrameFlags();
    }

    #region Escape Button

    public void OnEsc(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Escape.Press(); else Escape.Release();
    }
    #endregion
}
