using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : PlayerControls.IPlayerActions
{
    public PlayerControls playerControls;
    public InputButton MoveUp = new(KeyCode.W);
    public InputButton MoveDown = new(KeyCode.S);
    public InputButton MoveLeft = new(KeyCode.A);
    public InputButton MoveRight = new(KeyCode.D);
    public InputButton Move = new(KeyCode.None);
    public InputVector2 MoveVector2 = new();

    public PlayerInput()
    {
        playerControls = new PlayerControls();
        playerControls.Player.SetCallbacks(this);
        playerControls.Enable();
    }
    public void ResetFrameFlags()
    {
        MoveUp.ResetFrameFlags();
        MoveDown.ResetFrameFlags();
        MoveLeft.ResetFrameFlags();
        MoveRight.ResetFrameFlags();
        
        Move.ResetFrameFlags();
        MoveVector2.ResetFrameFlags();
    }

    #region Movement
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        MoveVector2.Set(input);

        if (input != Vector2.zero) Move.Press(); else Move.Release();
        if (input.y > 0) MoveUp.Press(); else MoveUp.Release();
        if (input.y < 0) MoveDown.Press(); else MoveDown.Release();
        if (input.x < 0) MoveLeft.Press(); else MoveLeft.Release();
        if (input.x > 0) MoveRight.Press(); else MoveRight.Release();
    }
    #endregion
}
