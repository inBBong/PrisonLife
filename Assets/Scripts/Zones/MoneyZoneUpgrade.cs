// MoneyZoneUpgrade.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class MoneyZoneUpgrade : MonoBehaviour
{
    [Header("Upgrade Settings")]
    public string upgradeName = "Tool Upgrade";
    public int upgradeCost = 50;

    [Header("Unlock Condition")]
    public bool startsHidden = true;        // 처음엔 숨겨져 있음
    public bool unlockOnFirstMoney = false; // 돈 첫 획득 시 열림 (체인X)

    [Header("Next Zones (완료 시 활성화)")]
    [Tooltip("업그레이드 완료 시 활성화할 다음 존들")]
    public MoneyZoneUpgrade[] nextZones;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image progressBar;

    [Header("Visual")]
    public GameObject zoneVisual;       // 반투명 큐브

    [Header("Drain Settings")]
    [Tooltip("한 번에 차감할 금액")]
    public int drainAmountPerTick = 5;
    [Tooltip("차감 주기 (초)")]
    public float drainInterval = 0.05f;

    [Header("Events")]
    public UnityEvent onUpgradeComplete;  // 완료 시 호출

    private float moneyInvested = 0f;
    private bool isCompleted = false;
    private PlayerInventory playerInZone = null;
    private Coroutine drainCoroutine;

    void Start()
    {
        if (startsHidden)
            SetVisible(false);
        else
            SetVisible(true);

        UpdateUI();

        // unlockOnFirstMoney가 true인 존만 돈 이벤트 구독
        // 나머지는 체인(nextZones)으로만 열림
        if (startsHidden && unlockOnFirstMoney && GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    // 돈이 처음 생기면 나타남
    void OnMoneyChanged(int amount)
    {
        if (amount > 0 && startsHidden)
        {
            Unlock();
            GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }
    }

    // 외부에서 직접 활성화 (다음 존 체인용)
    public void Unlock()
    {
        startsHidden = false;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[UpgradeZone] 충돌 감지: {other.gameObject.name}, Tag: {other.tag}");
        if (isCompleted) return;
        if (!other.CompareTag("Player"))
        {
            Debug.Log("[UpgradeZone] Player 태그 아님 → 무시");
            return;
        }
        playerInZone = other.GetComponent<PlayerInventory>();
        if (playerInZone == null)
        {
            Debug.LogError("[UpgradeZone] PlayerInventory 없음!");
            return;
        }
        Debug.Log($"[UpgradeZone] 플레이어 진입! 돈: {playerInZone.Money}");
        drainCoroutine = StartCoroutine(DrainMoneyLoop());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = null;
        if (drainCoroutine != null) StopCoroutine(drainCoroutine);
    }

    // 돈 블록 1개 = 10원
    private const int moneyPerBlock = 10;
    private int pendingDrain = 0;

    IEnumerator DrainMoneyLoop()
    {
        while (playerInZone != null && !isCompleted)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanAfford(1))
            {
                // 한 번에 drainAmountPerTick원 차감
                int actualDrain = Mathf.Min(
                    drainAmountPerTick,
                    upgradeCost - (int)moneyInvested);  // 초과 차감 방지
                actualDrain = Mathf.Min(
                    actualDrain,
                    GameManager.Instance.totalMoney);    // 보유금 초과 방지

                if (actualDrain <= 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                GameManager.Instance.SpendMoney(actualDrain);
                moneyInvested += actualDrain;
                pendingDrain += actualDrain;

                // 10원마다 블록 1개 제거
                while (pendingDrain >= moneyPerBlock)
                {
                    pendingDrain -= moneyPerBlock;
                    if (playerInZone != null && playerInZone.HasMoney)
                        playerInZone.RemoveMoney();
                }

                // 프로그레스바 업데이트
                float progress = moneyInvested / upgradeCost;
                if (progressBar != null)
                    progressBar.fillAmount = progress;

                // 남은 금액 표시
                int remaining = upgradeCost - (int)moneyInvested;
                if (costText != null)
                    costText.text = remaining > 0 ? $"${remaining}" : "Complete!";

                if (moneyInvested >= upgradeCost)
                {
                    // 남은 블록 모두 제거
                    while (playerInZone != null && playerInZone.HasMoney)
                        playerInZone.RemoveMoney();
                    CompleteUpgrade();
                    yield break;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            yield return new WaitForSeconds(drainInterval);
        }
    }

    void CompleteUpgrade()
    {
        isCompleted = true;

        // 업그레이드 실행
        ApplyUpgrade();

        // 이벤트 호출 (Inspector에서 연결한 함수 실행)
        onUpgradeComplete?.Invoke();

        // 플로팅 텍스트
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFloatingText(
                $"✓ {upgradeName}!",
                transform.position + Vector3.up * 2f,
                Color.yellow);

        // 다음 존들 활성화
        if (nextZones != null)
            foreach (var zone in nextZones)
                if (zone != null) zone.Unlock();

        // 이 존 비활성화
        StartCoroutine(HideAfterDelay(0.5f));
    }

    void ApplyUpgrade()
    {
        if (GameManager.Instance == null) return;

        // 툴 업그레이드만 자동 처리
        // 나머지(인부 고용, 수용소 확장 등)는
        // OnUpgradeComplete 이벤트로 외부에서 처리
        if (upgradeName.Contains("Tool") || upgradeName.Contains("tool"))
            GameManager.Instance.UpgradeMiningToolLevel();

        Debug.Log($"[UpgradeZone] {upgradeName} 완료!");
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    void UpdateUI()
    {
        if (nameText != null) nameText.text = upgradeName;
        if (costText != null) costText.text = $"${upgradeCost}";
        if (progressBar != null) progressBar.fillAmount = 0f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one * 1.5f);
    }
}