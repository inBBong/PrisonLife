// InteractionZone.cs
// Getter/Setter 존 베이스 클래스
// 플레이어가 존에 들어가면 빠른 속도로 반복 상호작용 실행

using UnityEngine;
using System.Collections;

public abstract class InteractionZone : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("아이템 하나 처리에 걸리는 시간 (초)")]
    public float interactionRate = 0.12f;

    [Header("Zone Indicator")]
    public GameObject zoneIndicator;  // 초록 점선 표시 등

    protected bool playerInZone = false;
    protected PlayerInventory playerInventory;
    private Coroutine interactionCoroutine;

    protected virtual void OnEnable() { }
    protected virtual void OnDisable()
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        playerInZone = true;
        ShowIndicator(true);
        OnPlayerEnter(playerInventory);

        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(InteractionLoop());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;
        ShowIndicator(false);
        OnPlayerExit(playerInventory);

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
        playerInventory = null;
    }

    IEnumerator InteractionLoop()
    {
        // 첫 상호작용은 즉시 실행
        while (playerInZone)
        {
            bool result = DoInteraction();
            if (!result)
            {
                // 처리할 아이템 없으면 잠시 대기 후 재시도
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return new WaitForSeconds(interactionRate);
            }
        }
    }

    void ShowIndicator(bool show)
    {
        if (zoneIndicator != null)
            zoneIndicator.SetActive(show);
    }

    // 하위 클래스에서 구현: 실제 아이템 처리. true=성공, false=처리 불가
    protected abstract bool DoInteraction();

    // 선택적 오버라이드
    protected virtual void OnPlayerEnter(PlayerInventory inv) { }
    protected virtual void OnPlayerExit(PlayerInventory inv) { }
}
