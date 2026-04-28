using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class HandcuffDeliveryWorker : MonoBehaviour
{
    [Header("Settings")]
    public int carryCapacity = 15;
    public float moveSpeed = 3f;

    [Header("References")]
    public GetterZone handcuffGetterZone;   // 컨베이어 수갑 Getter존
    public SetterZone handcuffSetterZone;   // 수감자 입구 수갑 Setter존

    [Header("Stack Visual")]
    public Transform handcuffStackAnchor;   // 몸통 앞쪽 빈 오브젝트
    public GameObject handcuffPrefab;
    public string handcuffPoolTag = "Handcuff";
    public float itemSpacingY = 0.12f;
    public float stackAnimSpeed = 8f;

    [Header("Animation")]
    public Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private NavMeshAgent agent;
    private int carriedHandcuffs = 0;
    private List<Transform> handcuffVisuals = new List<Transform>();
    private Vector3 handcuffPrefabScale = Vector3.one;

    private DeliveryState state = DeliveryState.CheckingSetterZone;

    enum DeliveryState
    {
        CheckingSetterZone, // 수갑 Setter존 확인
        GoingToGetter,      // Getter존으로 이동
        PickingUp,          // 수갑 집기
        GoingToSetter,      // Setter존으로 이동
        Delivering,         // 수갑 내려놓기
        WaitingAtSetter     // Setter존 앞 대기
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;
        if (handcuffPrefab != null)
            handcuffPrefabScale = handcuffPrefab.transform.localScale;
    }

    void Start()
    {
        StartCoroutine(DeliveryLoop());
    }

    void Update()
    {
        if (animator != null && agent != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);

        UpdateStackPositions();
    }

    IEnumerator DeliveryLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        while (true)
        {
            // ─── Setter존에 수갑이 있으면 대기 ──────────────
            if (handcuffSetterZone != null &&
                handcuffSetterZone.StoredCount > 0)
            {
                state = DeliveryState.WaitingAtSetter;
                yield return StartCoroutine(WaitNearSetter());
                continue;
            }

            // ─── Getter존에 수갑이 없으면 대기 ──────────────
            if (handcuffGetterZone == null ||
                handcuffGetterZone.StoredCount <= 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // ─── Getter존으로 이동 ───────────────────────────
            state = DeliveryState.GoingToGetter;
            yield return StartCoroutine(MoveTo(handcuffGetterZone.transform.position));

            // ─── 수갑 집기 ───────────────────────────────────
            state = DeliveryState.PickingUp;
            yield return StartCoroutine(PickUpHandcuffs());

            // ─── Setter존으로 이동 ───────────────────────────
            state = DeliveryState.GoingToSetter;
            yield return StartCoroutine(MoveTo(handcuffSetterZone.transform.position));

            // ─── 수갑 내려놓기 ───────────────────────────────
            state = DeliveryState.Delivering;
            yield return StartCoroutine(DeliverHandcuffs());
        }
    }

    // ─── Setter존 근처에서 대기 ───────────────────────────
    IEnumerator WaitNearSetter()
    {
        if (agent != null && handcuffSetterZone != null)
        {
            // Setter존 바로 앞에서 대기
            Vector3 waitPos = handcuffSetterZone.transform.position +
                handcuffSetterZone.transform.forward * 1.5f;
            agent.SetDestination(waitPos);
        }

        // Setter존 수갑이 다 없어질 때까지 대기
        yield return new WaitUntil(() =>
            handcuffSetterZone == null ||
            handcuffSetterZone.StoredCount <= 0);
    }

    // ─── 이동 ─────────────────────────────────────────────
    IEnumerator MoveTo(Vector3 destination)
    {
        if (agent == null) yield break;
        agent.SetDestination(destination);
        yield return new WaitUntil(() =>
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.3f);
    }

    // ─── 수갑 집기 ────────────────────────────────────────
    IEnumerator PickUpHandcuffs()
    {
        if (agent != null) agent.ResetPath();
        carriedHandcuffs = 0;

        int toPickUp = Mathf.Min(carryCapacity, handcuffGetterZone.StoredCount);

        for (int i = 0; i < toPickUp; i++)
        {
            if (handcuffGetterZone.TakeItem())
            {
                carriedHandcuffs++;
                SpawnHandcuffVisual();

                // 수갑 줍는 소리
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayHandcuffPickup();

                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    // ─── 수갑 내려놓기 ────────────────────────────────────
    IEnumerator DeliverHandcuffs()
    {
        if (agent != null) agent.ResetPath();

        for (int i = 0; i < carriedHandcuffs; i++)
        {
            if (handcuffSetterZone.StoredCount < handcuffSetterZone.maxCapacity)
                handcuffSetterZone.AddItemDirectly();

            RemoveTopHandcuffVisual();

            // 수갑 놓는 소리
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayHandcuffDrop();

            yield return new WaitForSeconds(0.08f);
        }

        carriedHandcuffs = 0;
    }

    // ─── 수갑 스택 비주얼 ────────────────────────────────
    void SpawnHandcuffVisual()
    {
        if (handcuffStackAnchor == null) return;

        Vector3 pos = handcuffStackAnchor.position +
            Vector3.up * handcuffVisuals.Count * itemSpacingY;

        GameObject obj = null;
        if (ObjectPool.Instance != null)
            obj = ObjectPool.Instance.Get(handcuffPoolTag, pos, Quaternion.identity);
        if (obj == null && handcuffPrefab != null)
            obj = Instantiate(handcuffPrefab, pos, Quaternion.identity);
        if (obj == null) return;

        obj.transform.localScale = handcuffPrefabScale;
        handcuffVisuals.Add(obj.transform);
    }

    void RemoveTopHandcuffVisual()
    {
        if (handcuffVisuals.Count == 0) return;
        int last = handcuffVisuals.Count - 1;
        Transform top = handcuffVisuals[last];
        handcuffVisuals.RemoveAt(last);

        if (top == null) return;
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(handcuffPoolTag, top.gameObject);
        else
            Destroy(top.gameObject);
    }

    void UpdateStackPositions()
    {
        if (handcuffStackAnchor == null) return;
        for (int i = 0; i < handcuffVisuals.Count; i++)
        {
            if (handcuffVisuals[i] == null) continue;

            float lagFactor = 1f / (1f + i * 0.3f);
            float speed = stackAnimSpeed * lagFactor;

            Vector3 targetPos = i == 0
                ? handcuffStackAnchor.position
                : handcuffVisuals[i - 1].position + Vector3.up * itemSpacingY;

            handcuffVisuals[i].position = Vector3.Lerp(
                handcuffVisuals[i].position, targetPos, speed * Time.deltaTime);
            handcuffVisuals[i].rotation = Quaternion.Lerp(
                handcuffVisuals[i].rotation, handcuffStackAnchor.rotation,
                speed * Time.deltaTime);
        }
    }
}