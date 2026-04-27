using System.Collections.Generic;
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

    [Header("Prisoner Spread Range")]
    public float normalSpreadRange = 2.5f;   // 기본 퍼짐 범위 (6x6 기준)
    public float expandedSpreadRange = 5f;   // 확장 후 퍼짐 범위 (12x12 기준)

    public float CurrentSpreadRange { get; private set; }

    public int CurrentCount { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsFull => CurrentCount >= MaxCapacity;
    private bool expandZoneOff=false;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        MaxCapacity = initialCapacity;
    }

    void Start()
    {
        UpdateUI();
        CurrentSpreadRange = normalSpreadRange;

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
            && !expandUpgradeZone.gameObject.activeSelf&&!expandZoneOff)
        {
            expandUpgradeZone.Unlock();
            Debug.Log("[PrisonManager] 수용소 확장 존 활성화!");
            expandZoneOff=true;
        }

        return true;
    }

    // 수용소 안 수감자 목록 관리
    private List<Prisoner> prisonersInside = new List<Prisoner>();

    public void RegisterPrisoner(Prisoner p)
    {
        prisonersInside.Add(p);
    }

    public void ExpandCapacity()
    {
        if (MaxCapacity >= expandedCapacity) return;
        MaxCapacity = expandedCapacity;
        UpdateUI();
        Debug.Log($"[PrisonManager] 수용소 확장 완료! {CurrentCount}/{MaxCapacity}");

        // 퍼짐 범위 확장
        CurrentSpreadRange = expandedSpreadRange;

        // 감옥 트랜스폼 애니메이션
        if (prisonObject != null)
            StartCoroutine(ExpandPrisonAnimation());

        // 기존 수감자들도 넓게 재배치
        StartCoroutine(RedistributePrisoners());
    }

    IEnumerator RedistributePrisoners()
    {
        // 애니메이션 완료 후 재배치
        yield return new WaitForSeconds(expandAnimDuration + 0.2f);

        foreach (Prisoner p in prisonersInside)
        {
            if (p == null) continue;
            p.MoveToRandomPosition(CurrentSpreadRange);
            yield return new WaitForSeconds(0.1f); // 순차적으로 이동
        }

        // null 제거
        prisonersInside.RemoveAll(p => p == null);
    }

    System.Collections.IEnumerator ExpandPrisonAnimation()
    {
        // 로컬 기준으로 읽고 적용 (부모 영향 제거)
        Vector3 startPos = prisonObject.localPosition;
        Vector3 startRot = prisonObject.localEulerAngles;
        Vector3 startScale = prisonObject.localScale;

        float elapsed = 0f;
        while (elapsed < expandAnimDuration)
        {
            float t = elapsed / expandAnimDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            prisonObject.localPosition = Vector3.Lerp(startPos, expandedPosition, smooth);
            prisonObject.localEulerAngles = Vector3.Lerp(startRot, expandedRotation, smooth);
            prisonObject.localScale = Vector3.Lerp(startScale, expandedScale, smooth);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 정확히 적용
        prisonObject.localPosition = expandedPosition;
        prisonObject.localEulerAngles = expandedRotation;
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