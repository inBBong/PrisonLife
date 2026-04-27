// QuarryRock.cs - 오브젝트 풀링 적용 버전

using UnityEngine;
using System.Collections;

public class QuarryRock : MonoBehaviour, IPoolable
{
    [Header("Visual")]
    public float collectAnimDuration = 0.15f;

    private bool isCollected = false;
    private bool isOnCooldown = false;
    private QuarryArea parentArea;
    private Renderer rockRenderer;
    private Color originalColor;
    private PooledObject pooledObject;

    void Awake()
    {
        rockRenderer = GetComponent<Renderer>();
        pooledObject = GetComponent<PooledObject>();
        if (pooledObject == null)
            pooledObject = gameObject.AddComponent<PooledObject>();
        pooledObject.poolTag = "Rock";

        // Awake에서 원본 색상 저장 (최초 1회)
        if (rockRenderer != null)
            originalColor = rockRenderer.material.color;
    }

    // QuarryArea가 자식이 아닌 경우 직접 주입
    public void SetParentArea(QuarryArea area)
    {
        parentArea = area;
    }

    // ─── IPoolable 구현 ───────────────────────────────────
    public void OnSpawn()
    {
        isCollected = false;
        isOnCooldown = false;
        transform.localScale = Vector3.one;

        // 저장된 원본 색상으로 복원
        if (rockRenderer != null)
            rockRenderer.material.color = originalColor;

        // parentArea가 없을 때만 자동 탐색 (SetParentArea로 주입된 경우 유지)
        if (parentArea == null)
            parentArea = GetComponentInParent<QuarryArea>();

        StartCoroutine(PopIn());
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        isCollected = false;
        isOnCooldown = false;

        // 반환 전 색상 원복 (다음 스폰을 위해)
        if (rockRenderer != null)
            rockRenderer.material.color = originalColor;
    }

    // ─── 채굴 로직 ────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (isCollected || isOnCooldown) return;
        if (!other.CompareTag("Player")) return;

        PlayerInventory inv = other.GetComponent<PlayerInventory>();
        if (inv == null || !inv.IsInQuarry) return;

        // 수갑 소지 중이면 채굴 불가
        if (inv.HasHandcuffs) return;

        StartCoroutine(MineWithCooldown(inv));
    }

    // MiningRangeCollider에서 호출
    public void CollectByPlayer(PlayerInventory inv)
    {
        if (isCollected || isOnCooldown) return;
        if (inv == null || !inv.IsInQuarry) return;

        // 수갑 소지 중이면 채굴 불가
        if (inv.HasHandcuffs) return;

        // Max여도 채굴 모션은 실행 (MineWithCooldown 내부에서 처리)
        StartCoroutine(MineWithCooldown(inv));
    }

    IEnumerator MineWithCooldown(PlayerInventory inv)
    {
        isOnCooldown = true;

        float cooldown = GameManager.Instance != null
            ? GameManager.Instance.MiningCooldown
            : 1.0f;

        if (rockRenderer != null)
            rockRenderer.material.color = Color.gray;

        yield return new WaitForSeconds(cooldown);

        if (inv == null || !inv.IsInQuarry)
        {
            isOnCooldown = false;
            if (rockRenderer != null)
                rockRenderer.material.color = originalColor;
            yield break;
        }

        // 돌 캐는 모션은 항상 실행
        // Max일 경우 인벤토리에 추가만 안 함
        if (!inv.StoneFull)
            inv.AddStone();

        // Max 표시
        if (inv.StoneFull)
        {
            MaxIndicatorUI maxUI = inv.GetComponentInChildren<MaxIndicatorUI>();
            if (maxUI != null) maxUI.ShowMax();
        }

        // 돌은 항상 사라짐
        isCollected = true;
        parentArea?.OnRockCollected(this);
        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < collectAnimDuration)
        {
            float t = elapsed / collectAnimDuration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy 대신 풀에 반환
        ReturnToPool();
    }

    // ─── 인부가 수집할 때 호출 ───────────────────────────
    public void CollectByWorker(QuarryWorker worker)
    {
        if (isCollected || isOnCooldown) return;
        StartCoroutine(WorkerMineCoroutine(worker));
    }

    IEnumerator WorkerMineCoroutine(QuarryWorker worker)
    {
        isOnCooldown = true;

        // 플레이어처럼 회색으로 변함
        if (rockRenderer != null)
            rockRenderer.material.color = Color.gray;

        // 툴 레벨 기반 쿨다운 적용
        float cooldown = GameManager.Instance != null
            ? GameManager.Instance.MiningCooldown
            : 1.0f;

        yield return new WaitForSeconds(cooldown);

        // 인부가 아직 돌 더 필요한지 확인
        bool added = worker.TryAddStone();
        if (!added)
        {
            // 인부가 꽉 찼으면 원래 색으로 복구
            isOnCooldown = false;
            if (rockRenderer != null)
                rockRenderer.material.color = originalColor;
            yield break;
        }

        isCollected = true;
        parentArea?.OnRockCollected(this);
        StartCoroutine(CollectAnimation());
    }

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
        {
            Debug.Log($"[QuarryRock] 풀 반환: {gameObject.name}");
            ObjectPool.Instance.Return("Rock", gameObject);
        }
        else
        {
            Debug.LogWarning("[QuarryRock] ObjectPool 없음 → 비활성화");
            gameObject.SetActive(false);
        }
    }

    IEnumerator PopIn()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0f, duration = 0.25f;
        while (elapsed < duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}