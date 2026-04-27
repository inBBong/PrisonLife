// ProgressBarZone.cs
// 존 위에 표시되는 초록색 점선 박스 + 진행 표시
// Prison Life의 녹색 점선 영역 UI 재현

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBarZone : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public Image fillBar;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;

    [Header("Dashed Border")]
    public Image dashedBorder;          // 점선 테두리 이미지
    public Color borderColor = Color.green;
    public float blinkSpeed = 2f;       // 테두리 깜빡임 속도

    private bool playerInZone = false;
    private float blinkTimer = 0f;

    void Start()
    {
        ShowPanel(false);
    }

    void Update()
    {
        if (!playerInZone) return;

        // 테두리 깜빡임
        if (dashedBorder != null)
        {
            blinkTimer += Time.deltaTime * blinkSpeed;
            float alpha = (Mathf.Sin(blinkTimer * Mathf.PI) + 1f) * 0.5f;
            Color c = borderColor;
            c.a = Mathf.Lerp(0.4f, 1f, alpha);
            dashedBorder.color = c;
        }
    }

    public void ShowPanel(bool show)
    {
        playerInZone = show;
        if (panel != null) panel.SetActive(show);
    }

    public void SetFill(float t)
    {
        if (fillBar != null) fillBar.fillAmount = t;
    }

    public void SetLabel(string label, string value = "")
    {
        if (labelText != null) labelText.text = label;
        if (valueText != null) valueText.text = value;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            ShowPanel(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            ShowPanel(false);
    }
}
