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

        if (rockRenderer != null)
            originalColor = rockRenderer.material.color;
    }

    public void SetParentArea(QuarryArea area)
    {
        parentArea = area;
    }

    // ─── IPoolable ────────────────────────────────────────
    public void OnSpawn()
    {
        isCollected = false;
        isOnCooldown = false;
        transform.localScale = Vector3.one;

        if (rockRenderer != null)
            rockRenderer.material.color = originalColor;

        if (parentArea == null)
            parentArea = GetComponentInParent<QuarryArea>();

        StartCoroutine(PopIn());
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        isCollected = false;
        isOnCooldown = false;

        if (rockRenderer != null)
            rockRenderer.material.color = originalColor;
    }

    // ─── 플레이어 충돌 ────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (isCollected || isOnCooldown) return;
        if (!other.CompareTag("Player")) return;

        PlayerInventory inv = other.GetComponent<PlayerInventory>();
        if (inv == null || !inv.IsInQuarry) return;
        if (inv.HasHandcuffs) return;

        StartCoroutine(MineWithCooldown(inv));
    }

    // MiningRangeCollider에서 호출
    public void CollectByPlayer(PlayerInventory inv)
    {
        if (isCollected || isOnCooldown) return;
        if (inv == null || !inv.IsInQuarry) return;
        if (inv.HasHandcuffs) return;
        StartCoroutine(MineWithCooldown(inv));
    }

    // ─── 인부 충돌 ────────────────────────────────────────
    public void CollectByWorker(QuarryWorker worker)
    {
        if (isCollected || isOnCooldown) return;
        StartCoroutine(WorkerMineCoroutine(worker));
    }

    // ─── 채굴 코루틴 (플레이어) ───────────────────────────
    IEnumerator MineWithCooldown(PlayerInventory inv)
    {
        isOnCooldown = true;

        float cooldown = GameManager.Instance != null
            ? GameManager.Instance.MiningCooldown : 1.0f;

        if (rockRenderer != null)
            rockRenderer.material.color = Color.gray;

        // 채굴 소리
        if (SoundManager.Instance != null && GameManager.Instance != null)
            SoundManager.Instance.PlayMining(GameManager.Instance.miningToolLevel);

        yield return new WaitForSeconds(cooldown);

        if (inv == null || !inv.IsInQuarry)
        {
            isOnCooldown = false;
            if (rockRenderer != null)
                rockRenderer.material.color = originalColor;
            yield break;
        }

        if (!inv.StoneFull)
            inv.AddStone();

        if (inv.StoneFull)
        {
            MaxIndicatorUI maxUI = inv.GetComponentInChildren<MaxIndicatorUI>();
            if (maxUI != null) maxUI.ShowMax();
        }

        isCollected = true;
        parentArea?.OnRockCollected(this);
        StartCoroutine(CollectAnimation());
    }

    // ─── 채굴 코루틴 (인부) ───────────────────────────────
    IEnumerator WorkerMineCoroutine(QuarryWorker worker)
    {
        isOnCooldown = true;

        if (rockRenderer != null)
            rockRenderer.material.color = Color.gray;

        float cooldown = GameManager.Instance != null
            ? GameManager.Instance.MiningCooldown : 1.0f;

        // 인부도 동일한 채굴 소리
        if (SoundManager.Instance != null && GameManager.Instance != null)
            SoundManager.Instance.PlayMining(1);

        yield return new WaitForSeconds(cooldown);

        bool added = worker.TryAddStone();
        if (!added)
        {
            isOnCooldown = false;
            if (rockRenderer != null)
                rockRenderer.material.color = originalColor;
            yield break;
        }

        isCollected = true;
        parentArea?.OnRockCollected(this);
        StartCoroutine(CollectAnimation());
    }

    // ─── 애니메이션 ───────────────────────────────────────
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

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return("Rock", gameObject);
        else
            gameObject.SetActive(false);
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