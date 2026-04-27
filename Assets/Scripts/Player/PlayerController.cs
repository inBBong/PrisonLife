// PlayerController.cs
// 플레이어 이동 컨트롤러
// - 버추얼 조이스틱 (모바일) + WASD/화살표 키 (에디터 테스트)
// - 카메라 기준 아이소메트릭 이동

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;
    public float gravity = -15f;

    [Header("References")]
    public Joystick joystick;
    public PlayerInventory inventory;
    public Animator animator;

    [Header("Tool Visual")]
    public GameObject pickaxeVisual;  // 채석장 진입 시 보여질 도구

    private CharacterController cc;
    private Vector3 verticalVelocity;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MiningHash = Animator.StringToHash("IsMining");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        
        // UIManager가 게임 시작 전까지 비활성화할 수 있음
        // 기본은 활성화 상태로 시작 (에디터 테스트 편의)
    }

    void Update()
    {
        HandleMovement();
        HandlePickaxeVisual();
    }

    void HandleMovement()
    {
        // ─── 입력 수집 ─────────────────────────────────
        float h = 0f, v = 0f;

        // 키보드 입력 (에디터 테스트용)
        h += Input.GetAxis("Horizontal");
        v += Input.GetAxis("Vertical");

        // 조이스틱 입력
        if (joystick != null && joystick.IsActive)
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        // ─── 카메라 기준 방향 계산 ──────────────────────
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * v + camRight * h;

        // ─── 이동 적용 ──────────────────────────────────
        if (moveDir.magnitude > 0.1f)
        {
            moveDir.Normalize();

            // 회전
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // 이동
            cc.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // 중력
        if (cc.isGrounded)
            verticalVelocity.y = -1f;
        else
            verticalVelocity.y += gravity * Time.deltaTime;

        cc.Move(verticalVelocity * Time.deltaTime);

        // ─── 애니메이션 ─────────────────────────────────
        if (animator != null)
        {
            float speed = moveDir.magnitude;
            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        }
    }

    void HandlePickaxeVisual()
    {
        if (pickaxeVisual == null || inventory == null) return;
        // 채석장 안에서 손에 곡괭이 표시 (수갑 없을 때만)
        pickaxeVisual.SetActive(inventory.IsInQuarry && !inventory.HasHandcuffs);
    }

    // 외부에서 속도 수정 (업그레이드 등)
    public void SetMoveSpeed(float speed) => moveSpeed = speed;
}
