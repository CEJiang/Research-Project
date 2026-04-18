using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CancelButtonInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Coroutine breathingCoroutine;
    private Vector3 initialScale;
    
    [Header("Breathing Effect Settings")]
    public float breatheSpeed = 2f;      // Breathing speed
    public float breatheAmount = 0.05f;   // Breathing amplitude (5%)
    public float hoverScale = 1.2f;      // Scale when mouse hovers

    private bool isHovering = false;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    void OnEnable()
    {
        breathingCoroutine = StartCoroutine(StartBreathingEffect());
    }

    void OnDisable()
    {
        if (breathingCoroutine != null) StopCoroutine(breathingCoroutine);
        transform.localScale = initialScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        // Playing a subtle paper rustling sound effect would be great here
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    IEnumerator StartBreathingEffect()
    {
        while (true)
        {
            // Calculate breathing scale value
            float targetBreathe = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            
            // If the mouse is hovering, scale based on the breathing value
            float finalScaleMultiplier = isHovering ? hoverScale : 1f;
            
            // Use Lerp for smoother transitions
            Vector3 targetScale = initialScale * targetBreathe * finalScaleMultiplier;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
            
            yield return null;
        }
    }
}