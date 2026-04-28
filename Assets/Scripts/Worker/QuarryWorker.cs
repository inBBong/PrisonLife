using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class QuarryWorker : MonoBehaviour
{
    [Header("Settings")]
    public int carryCapacity = 5;
    public float restDuration = 10f;
    public float moveSpeed = 2.5f;

    [Header("References")]
    public QuarryArea quarryArea;
    public SetterZone stoneSetterZone;

    [Header("Stack Visual")]
    public Transform stoneStackAnchor;      // 등 뒤 빈 오브젝트
    public GameObject rockStackPrefab;      // RockStack 프리팹
    public string rockStackPoolTag = "RockStack";
    public float itemSpacingY = 0.18f;
    public float stackAnimSpeed = 8f;

    [Header("Animation")]
    public Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private NavMeshAgent agent;
    private int carriedStones = 0;
    private List<Transform> stoneVisuals = new List<Transform>();
    private WorkerState state = WorkerState.MovingToQuarry;

    // RockStack 프리팹 원본 스케일
    private Vector3 rockStackScale = Vector3.one;

    enum WorkerState { MovingToQuarry, Mining, Delivering, Resting }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;
        if (rockStackPrefab != null)
            rockStackScale = rockStackPrefab.transform.localScale;
    }

    void Start()
    {
        // 인부마다 시작 시간을 더 많이 분산
        StartCoroutine(WorkLoop());
    }

    void Update()
    {
        if (animator != null && agent != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);

        UpdateStackPositions();
    }

    // ─── 돌과 충돌 시 수집 ────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (state != WorkerState.Mining) return;
        if (carriedStones >= carryCapacity) return;

        QuarryRock rock = other.GetComponent<QuarryRock>();
        if (rock == null) return;

        // 바위 수집
        rock.CollectByWorker(this);
    }

    // QuarryRock에서 호출
    public bool TryAddStone()
    {
        if (carriedStones >= carryCapacity) return false;
        carriedStones++;
        SpawnStackVisual();
        return true;
    }

    // ─── 스택 비주얼 ──────────────────────────────────────
    void SpawnStackVisual()
    {
        if (stoneStackAnchor == null) return;

        Vector3 pos = stoneStackAnchor.position + Vector3.up * stoneVisuals.Count * itemSpacingY;
        GameObject obj = null;

        if (ObjectPool.Instance != null)
            obj = ObjectPool.Instance.Get(rockStackPoolTag, pos, Quaternion.identity);

        if (obj == null && rockStackPrefab != null)
            obj = Instantiate(rockStackPrefab, pos, Quaternion.identity);

        if (obj == null) return;
        obj.transform.localScale = rockStackScale;
        stoneVisuals.Add(obj.transform);
    }

    void ClearStackVisuals()
    {
        foreach (Transform t in stoneVisuals)
        {
            if (t == null) continue;
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Return(rockStackPoolTag, t.gameObject);
            else
                Destroy(t.gameObject);
        }
        stoneVisuals.Clear();
    }

    void UpdateStackPositions()
    {
        if (stoneStackAnchor == null) return;
        for (int i = 0; i < stoneVisuals.Count; i++)
        {
            if (stoneVisuals[i] == null) continue;
            float lagFactor = 1f / (1f + i * 0.35f);
            float speed = stackAnimSpeed * lagFactor;

            Vector3 targetPos = i == 0
                ? stoneStackAnchor.position
                : stoneVisuals[i - 1].position + Vector3.up * itemSpacingY;

            Quaternion targetRot = stoneStackAnchor.rotation;

            stoneVisuals[i].position = Vector3.Lerp(stoneVisuals[i].position, targetPos, speed * Time.deltaTime);
            stoneVisuals[i].rotation = Quaternion.Lerp(stoneVisuals[i].rotation, targetRot, speed * Time.deltaTime);
        }
    }

    // ─── 워크 루프 ────────────────────────────────────────
    IEnumerator WorkLoop()
    {
        // 인부마다 시작 시간 크게 분산 (0~5초)
        yield return new WaitForSeconds(Random.Range(0f, 5f));

        while (true)
        {
            // 1. 채석장으로 이동
            state = WorkerState.MovingToQuarry;
            yield return StartCoroutine(MoveToQuarry());

            // 2. 채석 (돌과 충돌로 자동 수집)
            state = WorkerState.Mining;
            yield return StartCoroutine(MineStones());

            // 3. 자동 전달
            state = WorkerState.Delivering;
            yield return StartCoroutine(DeliverStones());

            // 4. 휴식
            state = WorkerState.Resting;
            yield return StartCoroutine(Rest());
        }
    }

    IEnumerator MoveToQuarry()
    {
        if (quarryArea == null || agent == null) yield break;
        agent.SetDestination(quarryArea.transform.position);
        yield return new WaitUntil(() =>
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.5f);
    }

    IEnumerator MineStones()
    {
        carriedStones = 0;

        // 채석장 안에서 돌아다니며 돌과 충돌로 수집
        float timeout = 30f; // 최대 30초
        float elapsed = 0f;

        while (carriedStones < carryCapacity && elapsed < timeout)
        {
            // 채석장 내 랜덤 위치로 이동 (범위 넓게)
            Vector3 randomPos = quarryArea.transform.position +
                new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));

            if (agent != null)
                agent.SetDestination(randomPos);

            // 이동 시간도 랜덤하게
            yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
            elapsed += 1f;
        }

        if (agent != null) agent.ResetPath();
    }

    IEnumerator DeliverStones()
    {
        if (stoneSetterZone == null) yield break;

        // 스택 비주얼 제거하면서 SetterZone에 추가
        for (int i = 0; i < carriedStones; i++)
        {
            if (stoneSetterZone.StoredCount < stoneSetterZone.maxCapacity)
                stoneSetterZone.AddItemDirectly();
            yield return new WaitForSeconds(0.1f);
        }

        carriedStones = 0;
        ClearStackVisuals();
    }

    IEnumerator Rest()
    {
        if (agent != null && quarryArea != null)
        {
            // 휴식 위치도 더 넓게 분산
            Vector3 restPos = quarryArea.transform.position +
                new Vector3(Random.Range(-4f, 4f), 0, Random.Range(2f, 5f));
            agent.SetDestination(restPos);
        }
        // 휴식 시간도 약간 랜덤하게
        yield return new WaitForSeconds(restDuration + Random.Range(-2f, 2f));
    }
}