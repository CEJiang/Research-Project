using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoResizeDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private float collapsedWidth = 160f; // 關閉時寬度
    private float posXOffset = 0f;   // origin X 偏移量
    public float expandedWidth = 650f;  // 展開時寬度
    private float expandedPosXOffset = 0f; // 展開後 X 偏移量
    private bool isExpanded = false;

    void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        posXOffset = dropdown.GetComponent<RectTransform>().anchoredPosition.x;
        collapsedWidth = dropdown.GetComponent<RectTransform>().rect.width;

        // 強制讓關閉時回到預設寬度
        SetSelfWidth(collapsedWidth);

        // 監聽點擊事件（Dropdown 展開的時候）
        dropdown.onValueChanged.AddListener(delegate { CollapseBack(); });
    }

    void Update()
    {
        // Dropdown 展開時會生成一個 clone "Dropdown List"
        Transform list = dropdown.transform.Find("Dropdown List");

        if (list != null && !isExpanded)
        {
            ExpandDropdown(list);
        }
        else if (list == null && isExpanded)
        {
            // 一旦關閉自動回縮
            SetSelfWidth(collapsedWidth);
            isExpanded = false;
        }
    }

    void ExpandDropdown(Transform list)
    {
        isExpanded = true;
        RectTransform rt = list.GetComponent<RectTransform>();

        // 1. 強制設定軸心點(Pivot)為左上角 (0, 1)
        // 這樣調整寬度時，選單的左側位置就不會變動
        rt.pivot = new Vector2(0, 1);

        // 2. 套用你指定的初始位置與寬度
        float currentWidth = expandedWidth;
        float currentPosX = expandedPosXOffset;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
        rt.anchoredPosition = new Vector2(currentPosX, rt.anchoredPosition.y);

        // 3. 獲取世界座標以判斷螢幕邊界
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        
        float leftEdgeWorldX = corners[0].x; // 選單左緣在螢幕上的位置
        float rightEdgeWorldX = corners[2].x; // 選單右緣在螢幕上的位置

        // 4. 如果右邊超出了螢幕
        if (rightEdgeWorldX > Screen.width)
        {
            // 直接計算剩餘可用的空間 (螢幕寬度 - 左緣位置 - 右邊留白)
            float availableWidth = Screen.width - leftEdgeWorldX - 20f;
            
            // 寬度縮小到可用空間，但不要小於按鈕本身的寬度
            currentWidth = Mathf.Max(collapsedWidth, availableWidth);
            
            // 重新套用寬度
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
        }

        // 5. 同步更新 Viewport 與 Content，讓文字正確換行
        RectTransform viewport = list.Find("Viewport").GetComponent<RectTransform>();
        RectTransform content = viewport.Find("Content").GetComponent<RectTransform>();
        
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);

        // 6. 強制排版引擎重算 (解決文字疊在一起的問題)
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    void CollapseBack()
    {
        // 關閉後 Dropdown 本體回縮
        SetSelfWidth(collapsedWidth);
        isExpanded = false;
    }

    void SetSelfWidth(float w)
    {
        RectTransform rt = dropdown.GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.anchoredPosition = new Vector2(posXOffset, rt.anchoredPosition.y);
    }
}
