// StackItem.cs
// 아이템이 플레이어에게 날아가는 아크 애니메이션
// Getter존에서 아이템 획득 시 시각적 효과

using UnityEngine;
using System.Collections;

public class StackItem : MonoBehaviour
{
    [Header("Arc Settings")]
    public float arcHeight = 1.5f;
    public float flyDuration = 0.3f;
    public AnimationCurve arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // 아이템을 특정 위치로 아크를 그리며 이동
    public IEnumerator FlyTo(Vector3 startPos, Vector3 endPos, System.Action onComplete)
    {
        transform.position = startPos;
        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            float t = elapsed / flyDuration;
            float curvedT = arcCurve.Evaluate(t);

            // 선형 보간 + 아크 높이
            Vector3 linearPos = Vector3.Lerp(startPos, endPos, curvedT);
            float arcY = arcHeight * Mathf.Sin(t * Mathf.PI);
            transform.position = linearPos + Vector3.up * arcY;

            // 크기 애니메이션 (끝으로 갈수록 작아짐)
            float scale = Mathf.Lerp(1f, 0.3f, t);
            transform.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        onComplete?.Invoke();
        Destroy(gameObject);
    }

    // 정적 팩토리 메서드
    public static void Launch(GameObject prefab, Vector3 from, Vector3 to,
                               float arcH = 1.5f, float duration = 0.3f,
                               System.Action onArrival = null)
    {
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, from, Quaternion.identity);
        StackItem si = obj.AddComponent<StackItem>();
        si.arcHeight = arcH;
        si.flyDuration = duration;
        si.StartCoroutine(si.FlyTo(from, to, onArrival));
    }
}
