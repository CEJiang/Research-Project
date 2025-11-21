using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplePoint : Singleton<SamplePoint>
{
    #region Show Spatial Sample
    private float sphereRadius = 0.05f;
    public void ShowSpatialSample(Vector3 position, Color startColor)
    {

        StartCoroutine(ShowSpatialSamplePoint(position, startColor));
    }
    private float showTimer = 3.0f;
    IEnumerator ShowSpatialSamplePoint(Vector3 position, Color startColor)
    {
        float startTime = Time.time;
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * sphereRadius;

        // ✅ 用透明材質
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);  // 3 = Transparent 模式
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        sphere.GetComponent<Renderer>().material = mat;

        Color endColor = new Color(1f, 1f, 1f, 0f); // 透明白

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // ✅ 在 showTimer 內漸變顏色
        while (Time.time - startTime < showTimer)
        {
            float t = (Time.time - startTime) / showTimer;
            sphere.GetComponent<Renderer>().material.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        Destroy(sphere);
    }

    #endregion
}
