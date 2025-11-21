using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInput : PlayerControls.IMouseActions
{
    public InputButton LeftButton = new(KeyCode.None);
    public InputButton RightButton = new(KeyCode.None);
    public InputButton MiddleButton = new(KeyCode.None);
    private PlayerControls playerControls;
    public MouseInput()
    {
        playerControls = new PlayerControls();
        playerControls.Mouse.SetCallbacks(this);
        playerControls.Enable();
    }

    public void ResetFrameFlags()
    {
        LeftButton.ResetFrameFlags();
        RightButton.ResetFrameFlags();
        MiddleButton.ResetFrameFlags();
    }  

    #region Left Button
    public void OnLeftMouseButton(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) LeftButton.Press(); else LeftButton.Release();
    }
    #endregion

    #region Right Button
    public void OnRightMouseButton(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) RightButton.Press(); else RightButton.Release();
    }
    #endregion

    #region Middle Button
    public void OnMiddleMouseButton(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) MiddleButton.Press(); else MiddleButton.Release();
    }
    #endregion
}
