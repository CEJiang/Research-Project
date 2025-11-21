using UnityEngine.InputSystem;
public class CameraInput
{
    public InputVector2 MouseDelta = new();
    public InputVector2 MousePosition = new();

    public void SetMouseDelta()
    {
        MouseDelta.Set(Mouse.current.delta.ReadValue());
    }

    public void SetMousePosition()
    {
        MousePosition.Set(Mouse.current.position.ReadValue());
    }

    public void ResetFrameFlags()
    {
        MouseDelta.ResetFrameFlags();
        MousePosition.ResetFrameFlags();
    }
}
