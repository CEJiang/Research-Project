using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NestedActionAutoRegister : MonoBehaviour
{
    public string displayNameZh;
    public string displayNameEn;
    public List<string> factBulletsZh = new();
    public List<string> factBulletsEn = new();
    void Start()
    {
        // 取得所有子物件 collider
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (var col in colliders)
        {
            if (col.GetComponent<SemanticActionObject>() == null)
            {
                col.gameObject.AddComponent<InteractableObject>();
                var sao = col.gameObject.AddComponent<SemanticActionObject>();
                sao.displayNameZh = displayNameZh;
                sao.displayNameEn = displayNameEn;
                sao.factBulletsZh = factBulletsZh;
                sao.factBulletsEn = factBulletsEn;
                Debug.Log($"[NestedActionAutoRegister] Added SemanticActionObject to: {col.gameObject.name}");
            }
        }

        Debug.Log($"[NestedActionAutoRegister] Total footprints updated: {colliders.Length}");
    }
}
