using UnityEngine;
using System.Collections;

public class PrisonDoor : MonoBehaviour
{
    [Header("Door Objects")]
    [Tooltip("열릴 문 오브젝트 (좌/우 각각 or 하나)")]
    public Transform doorLeft;
    public Transform doorRight;

    [Header("Open/Close Settings")]
    public float openAngle = 90f;       // 열릴 각도
    public float openSpeed = 3f;        // 열리는 속도
    public float closeDelay = 1.5f;     // 닫히기까지 대기 시간

    [Header("Trigger")]
    [Tooltip("수감자 감지 트리거 콜라이더 (이 오브젝트에 붙임)")]
    public float triggerRadius = 2f;

    private Quaternion doorLeftClosed;
    private Quaternion doorRightClosed;
    private Quaternion doorLeftOpen;
    private Quaternion doorRightOpen;

    private bool isOpen = false;
    private int prisonersInTrigger = 0;
    private Coroutine closeCoroutine;

    void Start()
    {
        // 초기(닫힌) 회전값 저장
        if (doorLeft != null)
        {
            doorLeftClosed = doorLeft.localRotation;
            doorLeftOpen = Quaternion.Euler(
                doorLeft.localEulerAngles + new Vector3(0, -openAngle, 0));
        }
        if (doorRight != null)
        {
            doorRightClosed = doorRight.localRotation;
            doorRightOpen = Quaternion.Euler(
                doorRight.localEulerAngles + new Vector3(0, openAngle, 0));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 수감자 또는 플레이어 감지
        if (!other.CompareTag("Prisoner")) return;

        prisonersInTrigger++;

        // 닫히기 예약 취소
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        // 문 열기
        if (!isOpen)
        {
            isOpen = true;
            StartCoroutine(AnimateDoor(true));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Prisoner") && !other.CompareTag("Player")) return;

        prisonersInTrigger = Mathf.Max(0, prisonersInTrigger - 1);

        // 트리거 안에 아무도 없으면 닫기 예약
        if (prisonersInTrigger <= 0)
            closeCoroutine = StartCoroutine(CloseAfterDelay());
    }

    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        isOpen = false;
        StartCoroutine(AnimateDoor(false));
    }

    IEnumerator AnimateDoor(bool opening)
    {
        Quaternion leftTarget = opening ? doorLeftOpen : doorLeftClosed;
        Quaternion rightTarget = opening ? doorRightOpen : doorRightClosed;

        while (true)
        {
            bool leftDone = true;
            bool rightDone = true;

            if (doorLeft != null)
            {
                doorLeft.localRotation = Quaternion.Lerp(
                    doorLeft.localRotation, leftTarget, openSpeed * Time.deltaTime);
                leftDone = Quaternion.Angle(doorLeft.localRotation, leftTarget) < 0.5f;
            }

            if (doorRight != null)
            {
                doorRight.localRotation = Quaternion.Lerp(
                    doorRight.localRotation, rightTarget, openSpeed * Time.deltaTime);
                rightDone = Quaternion.Angle(doorRight.localRotation, rightTarget) < 0.5f;
            }

            if (leftDone && rightDone)
            {
                if (doorLeft != null) doorLeft.localRotation = leftTarget;
                if (doorRight != null) doorRight.localRotation = rightTarget;
                yield break;
            }

            yield return null;
        }
    }

    // 외부에서 강제로 열기/닫기
    public void ForceOpen() => StartCoroutine(AnimateDoor(true));
    public void ForceClose() => StartCoroutine(AnimateDoor(false));

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}