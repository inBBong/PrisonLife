// ZoneProgressUI.cs
// 업그레이드 존 위에 표시되는 진행도 UI
// 월드 스페이스 캔버스에 붙여서 사용

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneProgressUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image progressFill;          // 원형 또는 바 형태 progress
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI progressText;
    public GameObject lockIcon;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f);
    public Color fullColor = Color.yellow;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // 항상 카메라를 향하도록 빌보드
        if (mainCamera != null)
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
    }

    public void SetProgress(float t)
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = t;
            progressFill.color = Color.Lerp(normalColor, fullColor, t);
        }
        if (progressText != null)
            progressText.text = $"{(int)(t * 100)}%";
    }

    public void SetInfo(string title, int cost)
    {
        if (titleText != null) titleText.text = title;
        if (costText != null) costText.text = $"${cost}";
    }

    public void SetLocked(bool locked)
    {
        if (lockIcon != null) lockIcon.SetActive(locked);
    }
}
