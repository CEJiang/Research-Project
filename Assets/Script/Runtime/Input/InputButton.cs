using UnityEngine;

public class InputButton
{
    public KeyCode keyCode;
    public bool IsPressed { get; private set; }
    public bool WasPressedThisFrame { get; private set; }
    public bool WasReleasedThisFrame { get; private set; }
    public float PressedDuration { get; private set; }
    private float startTime;
    public InputButton(KeyCode keyCode)
    {
        this.keyCode = keyCode;
    }
    public void Press()
    {
        if (IsPressed) return;
        IsPressed = true;
        WasPressedThisFrame = true;
        startTime = Time.time;
    }
    public void Release()
    {
        if (!IsPressed) return;
        IsPressed = false;
        WasReleasedThisFrame = true;
        PressedDuration = Time.time - startTime;
    }
    public void ResetFrameFlags()
    {
        WasPressedThisFrame = false;
        WasReleasedThisFrame = false;
    }
}
