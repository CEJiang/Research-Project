using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayInput : PlayerControls.IGameplayActions
{
    private PlayerControls playerControls;

    public readonly InputButton Interact = new();
    public readonly InputButton Photograph = new();

    public GameplayInput()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.SetCallbacks(this);
        playerControls.Enable();
    }

    public void ResetFrameFlags()
    {
        Interact.ResetFrameFlags();
        Photograph.ResetFrameFlags();
    }

#region Input Actions
    public void OnInteract(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Interact.Press(); else Interact.Release();
    }

    public void OnPhotograph(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Photograph.Press(); else Photograph.Release();
    }
#endregion
}
