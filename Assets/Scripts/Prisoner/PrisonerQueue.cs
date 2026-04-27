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

    private List<Prisoner> queue = new List<Prisoner>();
    private float spawnTimer = 0f;
    private bool initialized = false;

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

        // waypoints[0] 방향을 바라보며 스폰
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

        // 스폰 시 이미 수갑이 있으면 즉시 분배 시작
        if (handcuffSetterZone != null && handcuffSetterZone.StoredCount > 0)
            StartCoroutine(DistributeHandcuffs());
    }

    // 수갑 Setter존 수갑 수 변경 시 호출
    void OnHandcuffCountChanged(int handcuffCount)
    {
        if (handcuffCount <= 0) return;
        StartCoroutine(DistributeHandcuffs());
    }

    // ─── 핵심: 수갑을 1개씩 수감자에게 분배 ──────────────
    IEnumerator DistributeHandcuffs()
    {
        // 짧은 딜레이로 중복 호출 방지
        yield return new WaitForSeconds(0.05f);

        while (handcuffSetterZone.StoredCount > 0 && queue.Count > 0)
        {
            Prisoner frontPrisoner = queue[0];

            // 맨 앞 수감자에게 수갑 1개 전달
            if (frontPrisoner != null && frontPrisoner.IsWaiting)
            {
                handcuffSetterZone.TakeItem();
                frontPrisoner.ReceiveHandcuff();

                // 수갑 요구량이 다 충족됐으면 수감자 처리
                if (frontPrisoner.HandcuffsRemaining <= 0)
                {
                    frontPrisoner.Handcuff();
                    queue.RemoveAt(0);
                    UpdateQueuePositions();
                }
            }
            else
            {
                break;
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < queue.Count; i++)
            if (queueSlots.Length > i)
                queue[i].UpdateWaitPosition(queueSlots[i].position);
    }

    public void OnPrisonerEnteredPrison(Prisoner p)
    {
        PrisonManager.Instance?.AddPrisoner();
        // 수용소 안 수감자 목록에 등록 (확장 시 재배치용)
        PrisonManager.Instance?.RegisterPrisoner(p);
    }
}