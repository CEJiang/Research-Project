using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header("Inventory UI References")]
    public InventoryUI inventoryUI;

    [Header("Camera UI References")]
    public CameraUI cameraUI;   

    [Header("Guide UI References")] 
    public GuideUI guideUI;

    [Header("Player Reasoning UI References")]
    public PlayerReasoningUI playerReasoningUI;

    [Header("Fact Selection UI References")]
    public FactSelectionUI factSelectionUI;
    
    public enum OpenUIType
    {
        None,
        PlayerReasoning,
        Inventory,
        Camera,
        FactSelection
    }
    public List<OpenUIType> uiStack = new();
    public Dictionary<OpenUIType, GameObject> uiDictionary = new();

    void Start()
    {
        inventoryUI = FindObjectOfType<InventoryUI>();
        inventoryUI.gameObject.SetActive(false);
        uiDictionary[OpenUIType.Inventory] = inventoryUI.gameObject;

        cameraUI = FindObjectOfType<CameraUI>();
        cameraUI.gameObject.SetActive(false);
        uiDictionary[OpenUIType.Camera] = cameraUI.gameObject;

        guideUI = FindObjectOfType<GuideUI>();

        playerReasoningUI = FindObjectOfType<PlayerReasoningUI>();
        playerReasoningUI.gameObject.SetActive(false);
        uiDictionary[OpenUIType.PlayerReasoning] = playerReasoningUI.gameObject;
        
        factSelectionUI = FindObjectOfType<FactSelectionUI>();
        factSelectionUI.gameObject.SetActive(false);
        uiDictionary[OpenUIType.FactSelection] = factSelectionUI.gameObject;
    }

    public void TogglePlayerReasoningUI()
    {
        if (uiStack.Count > 0 && uiStack.Last() != OpenUIType.PlayerReasoning) return;

        playerReasoningUI.gameObject.SetActive(!playerReasoningUI.gameObject.activeSelf);
        CursorManager.Instance.SetCursorState(playerReasoningUI.gameObject.activeSelf);
        if (playerReasoningUI.gameObject.activeSelf)
        {
            guideUI.ShowPlayerReasoningInputGuide();
            uiStack.Add(OpenUIType.PlayerReasoning);
        }
        else
        {
            guideUI.ShowDefaultInputGuide();
            UpdateUIStackAfterClosingUI();
        }
    }

    public void ToggleInventoryUI()
    {
        if (uiStack.Count > 0 && uiStack.Last() != OpenUIType.Inventory) return;

        inventoryUI.gameObject.SetActive(!inventoryUI.gameObject.activeSelf);
        CursorManager.Instance.SetCursorState(inventoryUI.gameObject.activeSelf);
        if (inventoryUI.gameObject.activeSelf)
        {
            // guideUI.ShowInventoryInputGuide();
            uiStack.Add(OpenUIType.Inventory);
        }
        else
        {
            guideUI.ShowDefaultInputGuide();
            UpdateUIStackAfterClosingUI();
        }
    }
    public void ToggleCameraUI()
    {
        if (uiStack.Count > 0 && uiStack.Last() != OpenUIType.Camera) return;

        cameraUI.gameObject.SetActive(!cameraUI.gameObject.activeSelf);
        if (cameraUI.gameObject.activeSelf)
        {
            guideUI.ShowCameraInputGuide();
            uiStack.Add(OpenUIType.Camera);
        }
        else
        {
            guideUI.ShowDefaultInputGuide();
            UpdateUIStackAfterClosingUI();
        }
    }

    public void ToggleFactSelectionUI()
    {
        factSelectionUI.gameObject.SetActive(!factSelectionUI.gameObject.activeSelf);
        CursorManager.Instance.SetCursorState(factSelectionUI.gameObject.activeSelf);

        if (factSelectionUI.gameObject.activeSelf)
        {
            // guideUI.ShowFactSelectionInputGuide();
            FactSelectionManager.Instance.SetFactSelectionMode(FactSelectionManager.FactSelectionMode.Normal);
            uiStack.Add(OpenUIType.FactSelection);

            // Enter fact selection mode, we should close camera UI
            // Teporarily close camera UI when entering fact selection mode
            cameraUI.gameObject.SetActive(false);
            PlayerControlManager.Instance.SetPlayerControlState(false, false, false, false);
        }
        else
        {
            FactSelectionManager.Instance.SetFactSelectionMode(FactSelectionManager.FactSelectionMode.None);
            UpdateUIStackAfterClosingUI();

            // Exit fact selection mode, we should return to previous camera UI
            // When exiting fact selection mode, we should recover camera UI state based on whether player has opened camera UI before entering fact selection mode
            cameraUI.gameObject.SetActive(true);
            PlayerControlManager.Instance.SetPlayerControlState(true, true, true, true);
        }
    }

    public void ChangeFactSelectionFade(bool faded)
    {
        factSelectionUI.SetFactUIFade(faded);
        CursorManager.Instance.SetCursorState(!faded);
    }

    public void CloseTopUI()
    {
        if (uiStack.Count == 0) return;

        OpenUIType topUI = uiStack.Last();
        switch (topUI)
        {
            case OpenUIType.PlayerReasoning:
                TogglePlayerReasoningUI();
                break;
            case OpenUIType.Inventory:
                ToggleInventoryUI();
                break;
            case OpenUIType.Camera:
                ToggleCameraUI();
                break;
            default:
                break;
        }
    }
    public OpenUIType GetCurrentOpenUI()
    {
        return uiStack.Count > 0 ? uiStack.Last() : OpenUIType.None;
    }

    public void UpdateUIStackAfterClosingUI()
    {
        uiStack.RemoveAt(uiStack.Count - 1);
        if (uiStack.Count == 0)
        {
            CursorManager.Instance.SetCursorState(false);
            guideUI.ShowDefaultInputGuide();
        }
        else
        {
            OpenUIType topUI = uiStack.Last();
            switch (topUI)
            {
                case OpenUIType.PlayerReasoning:
                    guideUI.ShowPlayerReasoningInputGuide();
                    break;
                case OpenUIType.Inventory:
                    // guideUI.ShowInventoryInputGuide();
                    break;
                case OpenUIType.Camera:
                    guideUI.ShowCameraInputGuide();
                    break;
                default:
                    guideUI.ShowDefaultInputGuide();
                    break;
            }
        }
    }

    public void SetCameraDisplayName(string displayName)
    {
        if (displayName == "") cameraUI.displayNameText.transform.parent.gameObject.SetActive(false);
        else cameraUI.displayNameText.transform.parent.gameObject.SetActive(true);
        cameraUI.displayNameText.text = displayName;
    }

    public void CloseAllUI()
    {
        // evidenceBookUI.gameObject.SetActive(false);
        inventoryUI.gameObject.SetActive(false);
        cameraUI.gameObject.SetActive(false);
        playerReasoningUI.gameObject.SetActive(false);
        CursorManager.Instance.SetCursorState(true);
        uiStack.Clear();
    }
}
