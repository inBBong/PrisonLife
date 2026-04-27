// Joystick.cs
// 모바일용 버추얼 조이스틱 (화면 어디서나 터치 가능)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float maxRadius = 80f;

    [Header("References")]
    public RectTransform background;    // 조이스틱 배경 원
    public RectTransform handle;        // 조이스틱 핸들

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool IsActive { get; private set; }
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
    public float Magnitude => Direction.magnitude;

    private Vector2 touchStartPos;
    private Canvas parentCanvas;

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        
        // 처음엔 배경 숨김 (다이나믹 조이스틱)
        if (background != null)
            background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;

        // 터치 위치로 배경 이동
        if (background != null)
        {
            background.gameObject.SetActive(true);
            background.position = eventData.position;
        }

        touchStartPos = eventData.position;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsActive) return;

        Vector2 delta = eventData.position - touchStartPos;
        
        // 스케일 보정 (Canvas 스케일에 따라)
        if (parentCanvas != null)
            delta /= parentCanvas.scaleFactor;

        float magnitude = delta.magnitude;

        if (magnitude > maxRadius)
            delta = delta.normalized * maxRadius;

        if (handle != null)
            handle.anchoredPosition = delta;

        Horizontal = delta.x / maxRadius;
        Vertical = delta.y / maxRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsActive = false;
        Horizontal = 0f;
        Vertical = 0f;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (background != null)
            background.gameObject.SetActive(false);
    }
}
