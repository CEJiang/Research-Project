using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlManager : Singleton<PlayerControlManager>
{
    public FirstPersonController firstPersonController;

    public void SetPlayerControlState(bool canMove, bool canLook, bool canInteract, bool canOpenUI)
    {
        firstPersonController.SetMovable(canMove);
        firstPersonController.SetLookable(canLook);
        firstPersonController.SetInteractable(canInteract);
        firstPersonController.SetOpenableUI(canOpenUI);
    }
}
