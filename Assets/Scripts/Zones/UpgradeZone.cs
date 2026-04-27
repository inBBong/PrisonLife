// UpgradeZone.cs
// 돈을 투입해 업그레이드하는 존
// 채석도구 업그레이드, 인부 고용, 수용소 확장 등에 사용

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UpgradeZone : MonoBehaviour
{
    public enum UpgradeType
    {
        MiningTool,         // 채석도구 업그레이드
        HireQuarryWorker,   // 채석 인부 고용
        HireDeliveryWorker, // 배달 직원 고용
        ExpandPrison        // 수용소 확장
    }

    [Header("Upgrade Info")]
    public UpgradeType upgradeType;
    public int cost = 50;
    public string upgradeName = "Upgrade";

    [Header("UI References")]
    public GameObject upgradePanel;
    public Image progressBar;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI nameText;

    [Header("Workers (for hire type)")]
    public GameObject workerPrefab;
    public Transform workerSpawnPoint;

    // 현재까지 투입된 돈
    private float moneyInvested = 0f;
    private bool isCompleted = false;
    private bool playerInZone = false;
    private PlayerInventory playerInventory;
    private Coroutine moneyDrainCoroutine;

    void Start()
    {
        UpdateUI();
        ShowPanel(true);
    }

    public void Show(bool visible) => ShowPanel(visible);

    void ShowPanel(bool v)
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(v);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        if (!other.CompareTag("Player")) return;
        playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;
        playerInZone = true;
        moneyDrainCoroutine = StartCoroutine(DrainMoneyLoop());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = false;
        if (moneyDrainCoroutine != null)
            StopCoroutine(moneyDrainCoroutine);
        playerInventory = null;
    }

    IEnumerator DrainMoneyLoop()
    {
        while (playerInZone && !isCompleted)
        {
            if (playerInventory != null && playerInventory.HasMoney)
            {
                playerInventory.RemoveMoney();
                moneyInvested++;
                UpdateProgress();

                if (moneyInvested >= cost)
                {
                    CompleteUpgrade();
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.08f);
        }
    }

    void CompleteUpgrade()
    {
        isCompleted = true;
        Debug.Log($"[UpgradeZone] Upgrade complete: {upgradeType}");

        switch (upgradeType)
        {
            case UpgradeType.MiningTool:
                GameManager.Instance.UpgradeMiningTool();
                break;
            case UpgradeType.HireQuarryWorker:
                SpawnWorker(Worker.WorkerType.Quarry);
                break;
            case UpgradeType.HireDeliveryWorker:
                SpawnWorker(Worker.WorkerType.Delivery);
                break;
            case UpgradeType.ExpandPrison:
                PrisonManager.Instance?.ExpandCapacity();
                break;
        }

        // 완료 후 패널 숨김
        if (upgradePanel != null)
        {
            StartCoroutine(HideAfterDelay(0.5f));
        }

        // 플로팅 텍스트
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFloatingText(
                $"{upgradeName} Complete!", transform.position + Vector3.up * 2f, Color.yellow);
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPanel(false);
        gameObject.SetActive(false);
    }

    void SpawnWorker(Worker.WorkerType type)
    {
        if (workerPrefab == null || workerSpawnPoint == null) return;
        GameObject obj = Instantiate(workerPrefab, workerSpawnPoint.position, Quaternion.identity);
        Worker w = obj.GetComponent<Worker>();
        if (w != null) w.workerType = type;
    }

    void UpdateProgress()
    {
        if (progressBar != null)
            progressBar.fillAmount = moneyInvested / cost;
    }

    void UpdateUI()
    {
        if (costText != null)
            costText.text = $"${cost}";
        if (nameText != null)
            nameText.text = upgradeName;
        if (progressBar != null)
            progressBar.fillAmount = 0f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
