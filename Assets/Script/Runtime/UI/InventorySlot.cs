using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    private SemanticActionObject item;
    private RawImage previewTexture;
    private GameObject indicatorIcon;
    private bool isFirstTimeShowIndicator = false;
    public bool isOccupied => item != null;
    void Awake()
    {
        string name = gameObject.name;
        previewTexture = transform.Find("Preview")?.GetComponent<RawImage>();
        indicatorIcon  = transform.Find("Indicator Icon")?.gameObject;
        indicatorIcon.SetActive(false);
    }

    void OnEnable()
    {
        if (isFirstTimeShowIndicator)
        {
            indicatorIcon.SetActive(true);
        }
    }

    public void SetItem(SemanticActionObject newItem)
    {
        item = newItem;
        ShowItemIcon();
        isFirstTimeShowIndicator = true;
        
    }
    private void ShowItemIcon()
    {
        if (!previewTexture || item == null) return;

        var tex = SnapshotUtility.GetSnapshot(item.displayNameEn);
        if (tex != null) previewTexture.texture = tex;
        else previewTexture.texture = null; // 或顯示預設圖
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (item != null)
                UIManager.Instance.inventoryUI.ShowItemDetails(item);

        if (isFirstTimeShowIndicator)
        {
            indicatorIcon.SetActive(false);
            isFirstTimeShowIndicator = false;
        }
    }
}
