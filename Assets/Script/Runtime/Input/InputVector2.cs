using UnityEngine;

public class InputVector2
{
    public Vector2 input { get; private set; }
    public bool WasChangedThisFrame { get; private set; }
    public void Set(Vector2 _input)
    {
        if (_input != input)
        {
            input = _input;
            WasChangedThisFrame = true;
        }
    }
    public void ResetFrameFlags()
    {
        WasChangedThisFrame = false;
    }
}
