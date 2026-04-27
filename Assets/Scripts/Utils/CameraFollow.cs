// CameraFollow.cs
// 아이소메트릭 스타일 카메라 팔로우
// Prison Life의 비스듬한 탑뷰 시점 구현

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset & Angle")]
    public Vector3 offset = new Vector3(0f, 10f, -7f);
    public float lookAtHeightOffset = 1f;   // 플레이어 머리 위를 바라봄

    [Header("Smoothing")]
    public float positionSmoothSpeed = 8f;
    public float rotationSmoothSpeed = 5f;

    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public Bounds levelBounds;

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPos = target.position + offset;

        // 경계 클램프 (선택사항)
        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, levelBounds.min.x, levelBounds.max.x);
            desiredPos.z = Mathf.Clamp(desiredPos.z, levelBounds.min.z, levelBounds.max.z);
        }

        // 부드러운 위치 이동
        transform.position = Vector3.Lerp(
            transform.position, desiredPos, positionSmoothSpeed * Time.deltaTime);

        // 플레이어 바라보기
        Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, rotationSmoothSpeed * Time.deltaTime);
    }

    // 에디터에서 카메라 오프셋 시각화
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.3f);
    }
}
