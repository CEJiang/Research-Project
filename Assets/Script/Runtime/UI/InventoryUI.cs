using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    List<InventorySlot> inventorySlots = new();

    [Header("Detail Elements")]
    public GameObject DetailPanel;
    public RawImage previewImage;
    public Text ItemNameText;
    public Text ItemDescriptionText;

    GameObject itemPreviewRoot;
    void Awake()
    {
        Transform inventoryTransform = transform.Find("Content/Body/Inventory Panel/Inventory");
        foreach (Transform slotTransform in inventoryTransform)
        {
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            inventorySlots.Add(slot);
        }

        DetailPanel = transform.Find("Content/Body/Detail Panel").gameObject;
        previewImage = DetailPanel.transform.Find("Preview").GetComponent<RawImage>();
        ItemNameText = DetailPanel.transform.Find("ItemName Text").GetComponent<Text>();
        ItemDescriptionText = DetailPanel.transform.Find("Description Text").GetComponent<Text>();

        previewImage.color = new Color(1, 1, 1, 0);
        ItemNameText.text = "";
        ItemDescriptionText.text = "";

        itemPreviewRoot = GameObject.Find("ItemPreviewRoot");
    }

    public void AddItemToUI(SemanticActionObject item)
    {
        foreach (var slot in inventorySlots)
        {
            if (!slot.isOccupied)
            {
                slot.SetItem(item);
                break;
            }
        }
    }

    public void ShowItemDetails(SemanticActionObject item)
    {
        previewImage.color = new Color(1, 1, 1, 1);
        previewImage.texture = SnapshotUtility.GetSnapshot(item.displayNameEn);

        ItemNameText.text = item.displayNameEn;
        // ItemDescriptionText.text = item.facts.Count > 0 ? string.Join("\n- ", item.facts) : "No description available.";
        ItemDescriptionText.text = "";
        item.gameObject.transform.SetParent(itemPreviewRoot.transform);
        item.gameObject.SetActive(true);

        FrameObject(item.gameObject);
    }
    void FrameObject(GameObject obj)
    {
        Camera snapshotCamera = itemPreviewRoot.GetComponentInChildren<Camera>();
        Bounds bounds = GetBounds(obj);
        float size = bounds.extents.magnitude;
        float distance = size / Mathf.Tan(snapshotCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);

        snapshotCamera.transform.position = bounds.center - snapshotCamera.transform.forward * distance * 1.2f;
    }
    Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = new(obj.transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
}
