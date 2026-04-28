// ExcavatorController.cs
// Lv3 채석장 진입 시 포크레인을 직접 조종
// 플레이어 입력을 받아 포크레인 이동
// 카메라 타겟도 포크레인으로 전환

using UnityEngine;

public class ExcavatorController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 8f;

    [Header("Spawn Offset")]
    [Tooltip("플레이어 위치에서 포크레인 생성 시 Y 오프셋")]
    public float spawnYOffset = 0f;

    [Header("References")]
    public Joystick joystick;               // 기존 조이스틱
    public CameraFollow cameraFollow;       // 카메라 팔로우
    public Transform playerTransform;       // 플레이어 원래 위치 추적용

    [Header("Collider")]
    public BoxCollider boxCollider;         // 포크레인 전용 BoxCollider

    private bool isActive = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // 처음엔 비활성화
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;
        HandleMovement();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (joystick != null && joystick.IsActive)
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * v + camRight * h;

        if (moveDir.magnitude > 0.1f)
        {
            moveDir.Normalize();

            // 회전
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // 이동
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }

    // 플레이어 위치에 포크레인 스폰하고 활성화
    public void Activate(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // Y 오프셋 적용
        spawnPosition.y += spawnYOffset;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);
        isActive = true;

        // 카메라 타겟을 포크레인으로 변경
        if (cameraFollow != null)
            cameraFollow.target = transform;
    }

    // 비활성화 및 카메라 원래 타겟 복원
    public void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);

        // 카메라 타겟을 플레이어로 복원
        if (cameraFollow != null && playerTransform != null)
            cameraFollow.target = playerTransform;
    }

    // 포크레인 위치를 플레이어에게 전달 (채석장 나갈 때)
    public Vector3 GetPosition() => transform.position;
}