using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using TMPro;

public class Prisoner : MonoBehaviour
{
    public enum PrisonerState { Waiting, BeingHandcuffed, MovingToPrison, InPrison }

    [Header("Requirements")]
    public int handcuffsRequired;
    public int moneyReward;

    [Header("Visual")]
    public TextMeshProUGUI requirementText;
    public GameObject handcuffBadge;
    public Renderer prisonerRenderer;
    public Color normalColor = new Color(0.6f, 0.4f, 0.2f);
    public Color satisfiedColor = Color.green;

    [Header("UI Billboard")]
    public Transform requirementCanvas;  // 머리 위 캔버스
    private Camera mainCamera;

    [Header("Movement")]
    public float walkSpeed = 2f;

    [Header("Door")]
    [Tooltip("문 앞 마지막 웨이포인트에서 문이 열릴 때까지 대기 시간")]
    public float doorOpenWaitTime = 0.8f;

    [Header("Animation")]
    public Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Handcuff Visual")]
    public GameObject handcuffPrefab;       // 수갑 프리팹
    public Transform leftWristBone;         // 왼쪽 손목 뼈
    public Transform rightWristBone;        // 오른쪽 손목 뼈
    public Vector3 handcuffOffset = Vector3.zero;
    public Vector3 handcuffRotation = Vector3.zero;

    public PrisonerState State { get; private set; } = PrisonerState.Waiting;
    public int HandcuffsRequired => handcuffsRequired;
    public int HandcuffsRemaining { get; private set; }  // 남은 수갑 요구량
    public bool IsWaiting => State == PrisonerState.Waiting;

    private NavMeshAgent agent;
    private PrisonerQueue ownerQueue;
    private Vector3 waitPosition;

    // PrisonerQueue에서 주입받는 값들
    private Transform[] waypoints;
    private Transform prisonDoor;
    private GetterZone moneyGetter;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        handcuffsRequired = Random.Range(1, 5);
        moneyReward = handcuffsRequired;
        if (agent != null) agent.speed = walkSpeed;
        mainCamera = Camera.main;
    }

    private Vector3 lastPosition;

    void Update()
    {
        // 캔버스가 항상 카메라를 향하도록 (빌보드)
        if (requirementCanvas != null && mainCamera != null)
        {
            requirementCanvas.LookAt(
                requirementCanvas.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }

        if (animator == null || agent == null) return;

        // velocity가 0을 반환하는 경우를 대비해
        // desiredVelocity와 실제 위치 변화량 중 큰 값 사용
        float agentSpeed = agent.desiredVelocity.magnitude;

        if (agentSpeed < 0.1f)
        {
            // 위치 변화량으로 보조 확인
            float movedDistance = Vector3.Distance(transform.position, lastPosition);
            agentSpeed = movedDistance / Time.deltaTime;
        }

        lastPosition = transform.position;
        animator.SetFloat(SpeedHash, agentSpeed, 0.1f, Time.deltaTime);
    }

    // ← Initialize에서 waypoints, prisonDoor, moneyGetter 모두 받음
    public void Initialize(PrisonerQueue queue, Vector3 waitPos,
                           Transform[] wps, Transform door, GetterZone money)
    {
        ownerQueue = queue;
        waitPosition = waitPos;
        waypoints = wps;
        prisonDoor = door;
        moneyGetter = money;

        HandcuffsRemaining = handcuffsRequired;
        State = PrisonerState.Waiting;
        if (agent != null) agent.SetDestination(waitPos);
        UpdateRequirementUI();
        SetColor(normalColor);
    }

    public void UpdateWaitPosition(Vector3 newPos)
    {
        waitPosition = newPos;
        if (State == PrisonerState.Waiting && agent != null)
            agent.SetDestination(newPos);
    }

    // 수갑 1개 수령 → UI 업데이트
    public void ReceiveHandcuff()
    {
        if (HandcuffsRemaining <= 0) return;
        HandcuffsRemaining--;
        UpdateRequirementUI();
    }

    public void Handcuff()
    {
        if (State != PrisonerState.Waiting) return;
        State = PrisonerState.BeingHandcuffed;
        StartCoroutine(HandcuffSequence());
    }

    IEnumerator HandcuffSequence()
    {
        SetColor(satisfiedColor);
        UpdateRequirementUI(true);

        // 수갑 비주얼 장착
        AttachHandcuffs();

        yield return new WaitForSeconds(0.4f);

        // 돈 지급
        if (moneyGetter != null)
            for (int i = 0; i < moneyReward; i++)
                moneyGetter.AddItem();

        State = PrisonerState.MovingToPrison;

        // ─── Waypoint 순서대로 이동 ───────────────────────
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform wp = waypoints[i];
                if (wp == null) continue;

                agent.SetDestination(wp.position);
                yield return new WaitUntil(() =>
                    !agent.pathPending &&
                    agent.remainingDistance <= agent.stoppingDistance + 0.2f);

                // 마지막 웨이포인트(문 앞)에서 문이 열릴 때까지 대기
                if (i == waypoints.Length - 1)
                {
                    yield return new WaitForSeconds(doorOpenWaitTime);
                }
            }
        }

        // ─── 수용소 내부로 이동 ───────────────────────────
        if (prisonDoor != null)
        {
            agent.SetDestination(prisonDoor.position);
            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.2f);
        }

        // 수용소 입장 완료
        State = PrisonerState.InPrison;
        ownerQueue?.OnPrisonerEnteredPrison(this);

        // NavMeshAgent 비활성화 (더 이상 이동 불필요)
        if (agent != null) agent.enabled = false;

        // 수용소 안에서 제자리에 머물기
        // (Destroy 하지 않음 - 수용소 안에 수감자가 보임)
    }

    void AttachHandcuffs()
    {
        Debug.Log($"[Prisoner] AttachHandcuffs 호출됨");
        Debug.Log($"[Prisoner] handcuffPrefab: {(handcuffPrefab != null ? handcuffPrefab.name : "NULL")}");
        Debug.Log($"[Prisoner] leftWristBone: {(leftWristBone != null ? leftWristBone.name : "NULL")}");
        Debug.Log($"[Prisoner] rightWristBone: {(rightWristBone != null ? rightWristBone.name : "NULL")}");

        if (handcuffPrefab == null)
        {
            Debug.LogError("[Prisoner] handcuffPrefab이 없습니다! Inspector에서 연결해주세요.");
            return;
        }

        // 왼쪽 손목에 수갑 부착
        if (leftWristBone != null)
        {
            GameObject leftCuff = Instantiate(handcuffPrefab, leftWristBone);
            leftCuff.transform.localPosition = handcuffOffset;
            leftCuff.transform.localEulerAngles = handcuffRotation;
            Debug.Log("[Prisoner] 왼쪽 수갑 생성 완료");
        }
        else
        {
            Debug.LogWarning("[Prisoner] leftWristBone이 없습니다!");
        }

        // 오른쪽 손목에 수갑 부착
        if (rightWristBone != null)
        {
            GameObject rightCuff = Instantiate(handcuffPrefab, rightWristBone);
            rightCuff.transform.localPosition = handcuffOffset;
            rightCuff.transform.localEulerAngles = handcuffRotation;
            Debug.Log("[Prisoner] 오른쪽 수갑 생성 완료");
        }
        else
        {
            Debug.LogWarning("[Prisoner] rightWristBone이 없습니다!");
        }
    }

    void SetColor(Color color)
    {
        if (prisonerRenderer != null)
            prisonerRenderer.material.color = color;
    }

    void UpdateRequirementUI(bool satisfied = false)
    {
        if (requirementText != null)
        {
            if (satisfied)
                requirementText.text = "complete!";
            else
                requirementText.text = $" {HandcuffsRemaining}";  // 남은 수갑 수 표시
        }

        if (handcuffBadge != null)
            handcuffBadge.SetActive(!satisfied);

        // 수감자가 수용소로 이동 시작하면 UI 숨김
        if (satisfied && requirementCanvas != null)
            StartCoroutine(HideUIAfterDelay(0.5f));
    }

    System.Collections.IEnumerator HideUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (requirementCanvas != null)
            requirementCanvas.gameObject.SetActive(false);
    }
}