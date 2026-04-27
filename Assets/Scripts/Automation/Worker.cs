// Worker.cs
// 자동화 인부
// - Quarry 타입: 채석 자동화 (채석장 → 컨베이어 돌 Setter존)
// - Delivery 타입: 배달 자동화 (수갑 Getter존 → 수감자 입구 Setter존)

using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Worker : MonoBehaviour
{
    public enum WorkerType { Quarry, Delivery }

    [Header("Worker Type")]
    public WorkerType workerType;

    [Header("Work Settings")]
    public int carryCapacity = 5;
    public float moveSpeed = 3f;

    [Header("Quarry Worker References")]
    public QuarryArea quarryArea;
    public SetterZone stoneSetterZone;      // 컨베이어 앞 돌 Setter존

    [Header("Delivery Worker References")]
    public GetterZone handcuffGetterZone;   // 컨베이어 뒤 수갑 Getter존
    public SetterZone handcuffSetterZone;   // 수감자 입구 수갑 Setter존

    [Header("Visual")]
    public Renderer workerRenderer;
    public Color quarryColor = new Color(0.8f, 0.6f, 0.2f);
    public Color deliveryColor = new Color(0.2f, 0.6f, 0.8f);

    private NavMeshAgent agent;
    private int carrying = 0;
    private WorkerState state = WorkerState.Idle;

    enum WorkerState { Idle, GoingToSource, AtSource, GoingToDestination, AtDestination }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;
    }

    void Start()
    {
        // 색상 설정
        if (workerRenderer != null)
            workerRenderer.material.color = workerType == WorkerType.Quarry ? quarryColor : deliveryColor;

        StartCoroutine(WorkLoop());
    }

    IEnumerator WorkLoop()
    {
        // 짧은 초기 대기
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            if (workerType == WorkerType.Quarry)
                yield return StartCoroutine(QuarryWorkCycle());
            else
                yield return StartCoroutine(DeliveryWorkCycle());

            yield return new WaitForSeconds(0.3f);
        }
    }

    // ─── 채석 인부 사이클 ─────────────────────────────────
    IEnumerator QuarryWorkCycle()
    {
        // 돌 Setter존이 가득 차면 대기
        if (stoneSetterZone != null && stoneSetterZone.StoredCount >= stoneSetterZone.maxCapacity)
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // 채석 (일정 시간마다 돌 하나씩 추가)
        yield return new WaitForSeconds(2f + Random.Range(0f, 1f));

        if (stoneSetterZone != null)
            stoneSetterZone.AddItemDirectly();
    }

    // ─── 배달 직원 사이클 ─────────────────────────────────
    IEnumerator DeliveryWorkCycle()
    {
        if (handcuffGetterZone == null || handcuffSetterZone == null)
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // 수갑 Getter존에서 수갑을 꺼내 Setter존으로 옮김
        if (handcuffGetterZone.StoredCount <= 0)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // Getter존으로 이동
        if (agent != null)
        {
            agent.SetDestination(handcuffGetterZone.transform.position);
            yield return new WaitUntil(() =>
                agent.remainingDistance <= agent.stoppingDistance + 0.5f);
        }

        yield return new WaitForSeconds(0.3f);

        // 수갑 집기
        int toCarry = Mathf.Min(carryCapacity, handcuffGetterZone.StoredCount);
        for (int i = 0; i < toCarry; i++)
            handcuffGetterZone.TakeItem();

        carrying = toCarry;

        // Setter존으로 이동
        if (agent != null)
        {
            agent.SetDestination(handcuffSetterZone.transform.position);
            yield return new WaitUntil(() =>
                agent.remainingDistance <= agent.stoppingDistance + 0.5f);
        }

        yield return new WaitForSeconds(0.3f);

        // 수갑 놓기
        for (int i = 0; i < carrying; i++)
            handcuffSetterZone.AddItemDirectly();

        carrying = 0;
    }
}
