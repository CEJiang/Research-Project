using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayInput : PlayerControls.IGameplayActions
{
    private PlayerControls playerControls;

    public readonly InputButton Interact = new(KeyCode.E);

    public GameplayInput()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.SetCallbacks(this);
        playerControls.Enable();
    }

    public void ResetFrameFlags()
    {
        Interact.ResetFrameFlags();
    }

#region Input Actions
    public void OnInteract(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();

        if (isPressed) Interact.Press(); else Interact.Release();
    }
#endregion
}
