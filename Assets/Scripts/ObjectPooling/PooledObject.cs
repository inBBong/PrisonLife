// PooledObject.cs
// 풀링된 오브젝트에 붙이는 태그 컴포넌트
// 어느 풀에 속하는지 기억하고 자동 반환

using UnityEngine;
using System.Collections;

public class PooledObject : MonoBehaviour
{
    public string poolTag;

    // 일정 시간 후 자동 반환
    public void ReturnAfterDelay(float delay)
    {
        StartCoroutine(ReturnCoroutine(delay));
    }

    IEnumerator ReturnCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(poolTag, gameObject);
        else
            gameObject.SetActive(false);
    }
}
