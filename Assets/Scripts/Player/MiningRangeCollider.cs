// MiningRangeCollider.cs
// 플레이어의 채굴 범위 콜라이더
// 툴 레벨에 따라 크기가 달라짐
// 이 오브젝트를 플레이어 자식으로 추가

using UnityEngine;

public class MiningRangeCollider : MonoBehaviour
{
    [Header("Range Settings")]
    public float lv1Range = 0.5f;   // 기본 범위
    public float lv2Range = 0.7f;   // Lv2 범위
    public float lv3Range = 2.5f;   // Lv3 포크레인 범위

    [Header("References")]
    public PlayerInventory inventory;

    private SphereCollider miningCollider;
    private int lastToolLevel = -1;

    void Awake()
    {
        // Rigidbody 추가 (OnTriggerEnter 작동에 필요)
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;  // 물리 영향 없이 트리거만 감지
            rb.useGravity = false;
        }

        // SphereCollider 추가 (Is Trigger)
        miningCollider = gameObject.AddComponent<SphereCollider>();
        miningCollider.isTrigger = true;
        miningCollider.radius = lv1Range;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        int currentLevel = GameManager.Instance.miningToolLevel;
        if (currentLevel == lastToolLevel) return;

        lastToolLevel = currentLevel;
        UpdateRange(currentLevel);
    }

    void UpdateRange(int level)
    {
        if (miningCollider == null) return;

        float range = level == 1 ? lv1Range :
                      level == 2 ? lv2Range : lv3Range;

        miningCollider.radius = range;
        Debug.Log($"[MiningRange] Lv{level} 채굴 범위: {range}");
    }

    // QuarryRock이 이 콜라이더와 충돌 감지
    void OnTriggerEnter(Collider other)
    {
        if (inventory == null || !inventory.IsInQuarry) return;

        // 수갑 소지 중이면 채굴 불가
        if (inventory.HasHandcuffs) return;

        QuarryRock rock = other.GetComponent<QuarryRock>();
        if (rock == null) return;

        // Max여도 채굴 모션은 실행 (QuarryRock 내부에서 인벤토리 추가만 스킵)
        rock.CollectByPlayer(inventory);
    }

    void OnDrawGizmos()
    {
        if (miningCollider == null) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, miningCollider.radius);
    }
}