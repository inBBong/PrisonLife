// MaxIndicatorUI.cs
// 플레이어가 최대 소지량에 도달했을 때 머리 위에 "MAX" 텍스트 표시

using UnityEngine;
using TMPro;
using System.Collections;

public class MaxIndicatorUI : MonoBehaviour
{
    [Header("Settings")]
    public TextMeshProUGUI maxText;
    public float riseSpeed = 2f;
    public float duration = 1.0f;
    public float cooldown = 0.5f;  // 연속 표시 방지

    private bool isShowing = false;
    private bool onCooldown = false;
    private Vector3 startLocalPos;
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        if (maxText != null)
        {
            maxText.text = "MAX";
            maxText.color = Color.red;
            maxText.gameObject.SetActive(false);
        }
        startLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        // 항상 카메라를 향하도록 빌보드
        if (mainCamera != null)
        {
            transform.LookAt(
                transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void ShowMax()
    {
        if (isShowing || onCooldown) return;
        StartCoroutine(ShowAnimation());
    }

    IEnumerator ShowAnimation()
    {
        isShowing = true;

        if (maxText != null)
            maxText.gameObject.SetActive(true);

        // 시작 위치 초기화
        transform.localPosition = startLocalPos;

        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * riseSpeed;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // 위로 올라가기
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            // 페이드 아웃
            if (maxText != null)
            {
                Color c = maxText.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                maxText.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (maxText != null)
        {
            maxText.gameObject.SetActive(false);
            Color c = maxText.color;
            c.a = 1f;
            maxText.color = c;
        }

        transform.localPosition = startLocalPos;
        isShowing = false;

        // 쿨다운
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
