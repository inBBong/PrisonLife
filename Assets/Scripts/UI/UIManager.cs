// UIManager.cs
// HUD UI 관리 (돈 표시, 수용소 인원 등)

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI prisonCapacityText;

    [Header("Inventory Display")]
    public TextMeshProUGUI stoneCountText;
    public TextMeshProUGUI handcuffCountText;

    [Header("Title / Play Button")]
    public GameObject titlePanel;
    public Button playButton;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Canvas worldCanvas;

    private bool gameStarted = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoneyUI;
            UpdateMoneyUI(GameManager.Instance.totalMoney);
        }

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (titlePanel != null)
            titlePanel.SetActive(true);
    }

    void OnPlayButtonClicked()
    {
        gameStarted = true;
        if (titlePanel != null)
            titlePanel.SetActive(false);
        
        // 플레이어 이동 활성화
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.enabled = true;
    }

    public void UpdateMoneyUI(int amount)
    {
        if (moneyText != null)
            moneyText.text = amount.ToString();
    }

    public void UpdatePrisonCapacity(int current, int max)
    {
        if (prisonCapacityText != null)
            prisonCapacityText.text = $"{current}/{max}";
    }

    public void UpdateInventoryDisplay(int stones, int handcuffs)
    {
        if (stoneCountText != null)
            stoneCountText.text = stones > 0 ? $"🪨 {stones}" : "";
        if (handcuffCountText != null)
            handcuffCountText.text = handcuffs > 0 ? $"⛓ {handcuffs}" : "";
    }

    // 화면 위치에 플로팅 텍스트 생성 (e.g., "+$10")
    public void ShowFloatingText(string text, Vector3 worldPosition, Color color)
    {
        if (floatingTextPrefab == null || worldCanvas == null) return;
        GameObject obj = Instantiate(floatingTextPrefab, worldCanvas.transform);
        FloatingText ft = obj.GetComponent<FloatingText>();
        if (ft != null) ft.Initialize(text, worldPosition, color);
    }

    public bool IsGameStarted => gameStarted;
}
