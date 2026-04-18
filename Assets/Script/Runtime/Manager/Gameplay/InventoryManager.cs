using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : Singleton<InventoryManager>
{
    public List<SemanticActionObject> items = new();

    public void AddItem(SemanticActionObject item)
    {
        if (!items.Contains(item))
        {
            UIManager.Instance.inventoryUI.AddItemToUI(item);
            items.Add(item);
            item.gameObject.SetActive(false);  
        }
    }
    
}