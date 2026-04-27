// FloatingText.cs
// 화면에 떠오르는 텍스트 이펙트 (예: +$10)

using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 1.2f;
    public float riseSpeed = 60f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private Camera mainCamera;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    public void Initialize(string text, Vector3 worldPos, Color color)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
        }

        // World → Screen 좌표 변환
        if (mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            if (rectTransform != null)
                rectTransform.position = screenPos;
        }

        StartCoroutine(AnimateAndDestroy());
    }

    IEnumerator AnimateAndDestroy()
    {
        float elapsed = 0f;
        Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

        while (elapsed < lifetime)
        {
            float t = elapsed / lifetime;
            
            if (rectTransform != null)
                rectTransform.anchoredPosition = startPos + Vector2.up * riseSpeed * elapsed;
            
            if (textComponent != null)
            {
                Color c = textComponent.color;
                c.a = fadeCurve.Evaluate(t);
                textComponent.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
