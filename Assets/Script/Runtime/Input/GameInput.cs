
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GameInput
{
    public static CameraInput CameraInput { get; private set; }
    public static PlayerInput PlayerInput { get; private set; }
    public static GameplayInput GameplayInput { get; private set; }
    public static MouseInput MouseInput { get; private set; }
    public static UIInput UIInput { get; private set; }
    public static List<InputButton> inputButtons = new();

    static GameInput()
    {
        CameraInput = new CameraInput();
        PlayerInput = new PlayerInput();
        GameplayInput = new GameplayInput();
        MouseInput = new MouseInput();
        UIInput = new UIInput();

        inputButtons.Add(PlayerInput.MoveUp);
        inputButtons.Add(PlayerInput.MoveDown);
        inputButtons.Add(PlayerInput.MoveLeft);
        inputButtons.Add(PlayerInput.MoveRight);
        inputButtons.Add(UIInput.Escape);
        inputButtons.Add(GameplayInput.Interact);

        InputSystem.onBeforeUpdate += OnBeforeUpdate;
        InputSystem.onAfterUpdate += OnAfterUpdate;
    }

    static void OnBeforeUpdate()
    {
        CameraInput.ResetFrameFlags();
        PlayerInput.ResetFrameFlags();
        GameplayInput.ResetFrameFlags();
        MouseInput.ResetFrameFlags();
        UIInput.ResetFrameFlags();
    }
    static void OnAfterUpdate()
    {
        CameraInput.SetMouseDelta();
        CameraInput.SetMousePosition();
    }
    public static List<InputButton> GetInputButtons => inputButtons;
}
