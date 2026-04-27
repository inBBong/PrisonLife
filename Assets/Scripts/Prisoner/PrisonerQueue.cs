using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrisonerQueue : MonoBehaviour
{
    [Header("Prisoner Spawning")]
    public GameObject prisonerPrefab;
    public int maxQueueLength = 5;
    public float spawnInterval = 6f;
    public float initialSpawnDelay = 1f;

    [Header("Queue Positions")]
    public Transform[] queueSlots;

    [Header("References")]
    public Transform prisonDoor;
    public SetterZone handcuffSetterZone;
    public GetterZone moneyGetterZone;

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Handcuff Distribution")]
    public float handcuffGiveInterval = 0.4f;

    private List<Prisoner> queue = new List<Prisoner>();
    private float spawnTimer = 0f;
    private bool initialized = false;
    private bool isProcessingDeparture = false;
    private bool isDistributing = false;

    void Start()
    {
        if (handcuffSetterZone != null)
            handcuffSetterZone.OnCountChanged += OnHandcuffCountChanged;
        StartCoroutine(InitialSpawn());
    }

    void OnDestroy()
    {
        if (handcuffSetterZone != null)
            handcuffSetterZone.OnCountChanged -= OnHandcuffCountChanged;
    }

    IEnumerator InitialSpawn()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        SpawnPrisoner();
        yield return new WaitForSeconds(1f);
        SpawnPrisoner();
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (isProcessingDeparture) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            if (queue.Count < maxQueueLength)
                SpawnPrisoner();
        }
    }

    void SpawnPrisoner()
    {
        if (prisonerPrefab == null) return;
        if (queue.Count >= maxQueueLength) return;
        if (queue.Count >= queueSlots.Length) return;

        int slotIndex = queue.Count;
        Vector3 spawnPos = queueSlots[slotIndex].position;

        Quaternion spawnRot = Quaternion.identity;
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            Vector3 dir = waypoints[0].position - spawnPos;
            dir.y = 0f;
            if (dir != Vector3.zero)
                spawnRot = Quaternion.LookRotation(dir.normalized);
        }

        GameObject obj = Instantiate(prisonerPrefab, spawnPos, spawnRot);
        Prisoner p = obj.GetComponent<Prisoner>();
        if (p == null) { Destroy(obj); return; }

        p.Initialize(this, spawnPos, waypoints, prisonDoor, moneyGetterZone);
        queue.Add(p);
        UpdateQueuePriorities();

        if (handcuffSetterZone != null && handcuffSetterZone.StoredCount > 0 && !isDistributing)
            StartCoroutine(DistributeHandcuffs());
    }

    void UpdateQueuePriorities()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null) continue;
            var agent = queue[i].GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
                agent.avoidancePriority = 20 + i * 10;
        }
    }

    void OnHandcuffCountChanged(int handcuffCount)
    {
        if (handcuffCount <= 0) return;
        if (isDistributing) return;
        StartCoroutine(DistributeHandcuffs());
    }

    // ─── 수갑 분배 ────────────────────────────────────────
    IEnumerator DistributeHandcuffs()
    {
        if (isDistributing) yield break;
        isDistributing = true;

    try_distribute:

        // 수감자 없으면 종료
        if (queue.Count == 0)
        {
            isDistributing = false;
            yield break;
        }

        Prisoner frontPrisoner = queue[0];
        if (frontPrisoner == null || !frontPrisoner.IsWaiting)
        {
            isDistributing = false;
            yield break;
        }

        // ① 맨 앞 수감자가 Idle 될 때까지 대기 (최초 1회)
        yield return new WaitUntil(() =>
            frontPrisoner == null ||
            !frontPrisoner.IsWaiting ||
            IsPrisonerIdle(frontPrisoner));

        if (frontPrisoner == null || !frontPrisoner.IsWaiting)
        {
            isDistributing = false;
            yield break;
        }

        // ② 수감자 요구량 충족될 때까지 수갑 1개씩 전달
        while (frontPrisoner != null &&
               frontPrisoner.IsWaiting &&
               frontPrisoner.HandcuffsRemaining > 0)
        {
            // 수갑이 없으면 폴링으로 대기
            while (handcuffSetterZone.StoredCount <= 0)
            {
                yield return new WaitForSeconds(0.1f);

                // 대기 중 수감자 상태 변경 확인
                if (frontPrisoner == null || !frontPrisoner.IsWaiting)
                {
                    isDistributing = false;
                    yield break;
                }
            }

            // 수갑 1개 전달
            handcuffSetterZone.TakeItem();
            frontPrisoner.ReceiveHandcuff();
            yield return new WaitForSeconds(handcuffGiveInterval);
        }

        // ③ 요구량 충족 → 수감자 출발
        if (frontPrisoner != null && frontPrisoner.IsWaiting &&
            frontPrisoner.HandcuffsRemaining <= 0)
        {
            isProcessingDeparture = true;
            frontPrisoner.Handcuff();
            queue.RemoveAt(0);

            yield return StartCoroutine(WaitAndMoveQueue(frontPrisoner));
            isProcessingDeparture = false;
        }

        // ④ 다음 수감자 처리
        goto try_distribute;
    }

    IEnumerator WaitAndMoveQueue(Prisoner departedPrisoner)
    {
        if (queueSlots.Length > 0)
        {
            yield return new WaitUntil(() =>
                departedPrisoner == null ||
                Vector3.Distance(departedPrisoner.transform.position,
                    queueSlots[0].position) >= 2f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        UpdateQueuePositions();

        if (queue.Count > 0)
        {
            Prisoner nextPrisoner = queue[0];
            yield return new WaitUntil(() =>
                nextPrisoner == null ||
                !nextPrisoner.IsWaiting ||
                IsAtSlot0(nextPrisoner));
        }
    }

    bool IsAtSlot0(Prisoner p)
    {
        if (p == null || queueSlots.Length == 0) return true;
        return Vector3.Distance(p.transform.position, queueSlots[0].position) <= 0.5f;
    }

    bool IsPrisonerIdle(Prisoner p)
    {
        if (p == null) return true;
        Animator anim = p.GetComponent<Animator>();
        if (anim == null) return true;
        return anim.GetFloat("Speed") < 0.1f;
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < queue.Count; i++)
            if (queueSlots.Length > i)
                queue[i].UpdateWaitPosition(queueSlots[i].position);
        UpdateQueuePriorities();
    }

    public void OnPrisonerEnteredPrison(Prisoner p)
    {
        PrisonManager.Instance?.AddPrisoner();
        PrisonManager.Instance?.RegisterPrisoner(p);
    }
}