using System.Collections;
using UnityEngine;
using TMPro;

public class PrisonManager : MonoBehaviour
{
    public static PrisonManager Instance { get; private set; }

    [Header("Capacity")]
    public int initialCapacity = 20;
    public int expandedCapacity = 40;

    [Header("UI")]
    public TextMeshProUGUI capacityText;
    public GameObject capacityPanel;

    [Header("Upgrade Zone")]
    public MoneyZoneUpgrade expandUpgradeZone;  // 수용소 확장 존
    public int expandUnlockThreshold = 10;       // 몇 명이 들어오면 존 활성화

    [Header("Prison Expand Transform")]
    public Transform prisonObject;          // 확장할 감옥 오브젝트
    public Vector3 expandedPosition;        // 확장 후 Position
    public Vector3 expandedRotation;        // 확장 후 Rotation
    public Vector3 expandedScale;           // 확장 후 Scale
    public float expandAnimDuration = 0.5f; // 확장 애니메이션 시간

    public int CurrentCount { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsFull => CurrentCount >= MaxCapacity;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        MaxCapacity = initialCapacity;
    }

    void Start()
    {
        UpdateUI();

        // 확장 존은 처음엔 숨김
        if (expandUpgradeZone != null)
            expandUpgradeZone.gameObject.SetActive(false);
    }

    public bool AddPrisoner()
    {
        if (IsFull)
        {
            Debug.Log("[PrisonManager] 수용소가 가득 찼습니다!");
            return false;
        }

        CurrentCount++;
        UpdateUI();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowFloatingText(
                "+1 Prisoner",
                transform.position + Vector3.up * 3f,
                Color.white);

        // 수감자 10명 달성 시 확장 존 활성화
        if (CurrentCount >= expandUnlockThreshold && expandUpgradeZone != null
            && !expandUpgradeZone.gameObject.activeSelf)
        {
            expandUpgradeZone.Unlock();
            Debug.Log("[PrisonManager] 수용소 확장 존 활성화!");
        }

        return true;
    }

    public void ExpandCapacity()
    {
        if (MaxCapacity >= expandedCapacity) return;
        MaxCapacity = expandedCapacity;
        UpdateUI();
        Debug.Log($"[PrisonManager] 수용소 확장 완료! {CurrentCount}/{MaxCapacity}");

        // 감옥 트랜스폼 애니메이션
        if (prisonObject != null)
            StartCoroutine(ExpandPrisonAnimation());
    }

    System.Collections.IEnumerator ExpandPrisonAnimation()
    {
        Vector3 startPos = prisonObject.position;
        Vector3 startRot = prisonObject.eulerAngles;
        Vector3 startScale = prisonObject.localScale;

        float elapsed = 0f;
        while (elapsed < expandAnimDuration)
        {
            float t = elapsed / expandAnimDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            prisonObject.position = Vector3.Lerp(startPos, expandedPosition, smooth);
            prisonObject.eulerAngles = Vector3.Lerp(startRot, expandedRotation, smooth);
            prisonObject.localScale = Vector3.Lerp(startScale, expandedScale, smooth);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 정확히 적용
        prisonObject.position = expandedPosition;
        prisonObject.eulerAngles = expandedRotation;
        prisonObject.localScale = expandedScale;
    }

    void UpdateUI()
    {
        if (capacityText != null)
            capacityText.text = $"{CurrentCount}/{MaxCapacity}";

        if (UIManager.Instance != null)
            UIManager.Instance.UpdatePrisonCapacity(CurrentCount, MaxCapacity);
    }
}