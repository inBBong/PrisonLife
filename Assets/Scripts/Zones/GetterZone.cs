using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GetterZone : InteractionZone
{
    [Header("Zone Type")]
    public ItemType providedItemType;

    [Header("Storage")]
    public Transform stackAnchor;
    public GameObject storedItemPrefab;
    public float stackSpacingY = 0.12f;

    [Header("Pool Tag")]
    public string poolTag = "";  // 비어있으면 Instantiate 사용

    [Header("Count UI")]
    public TextMeshProUGUI countText;

    public int StoredCount { get; private set; }

    private List<GameObject> storedVisuals = new List<GameObject>();

    public void AddItem()
    {
        StoredCount++;
        UpdateCountUI();
        SpawnVisual();
    }

    protected override bool DoInteraction()
    {
        if (StoredCount <= 0) return false;
        if (playerInventory == null) return false;

        bool success = false;
        switch (providedItemType)
        {
            case ItemType.Handcuff: success = playerInventory.AddHandcuff(); break;
            case ItemType.Money: success = playerInventory.AddMoney(); break;
            case ItemType.Stone: success = playerInventory.AddStone(); break;
        }

        if (success) TakeItemInternal();
        return success;
    }

    public bool TakeItem()
    {
        if (StoredCount <= 0) return false;
        TakeItemInternal();
        return true;
    }

    void TakeItemInternal()
    {
        StoredCount--;
        UpdateCountUI();
        RemoveTopVisual();
    }

    // ─── 풀링 적용 SpawnVisual ────────────────────────────
    void SpawnVisual()
    {
        if (stackAnchor == null) return;

        float y = storedVisuals.Count * stackSpacingY;
        Vector3 pos = stackAnchor.position + Vector3.up * y;
        GameObject obj = null;

        // 풀에서 꺼내기
        if (!string.IsNullOrEmpty(poolTag) && ObjectPool.Instance != null)
            obj = ObjectPool.Instance.Get(poolTag, pos, stackAnchor.rotation);

        // 풀 없으면 직접 생성
        if (obj == null && storedItemPrefab != null)
            obj = Instantiate(storedItemPrefab, pos, Quaternion.identity);

        if (obj == null) return;

        obj.transform.SetParent(stackAnchor);
        obj.transform.localPosition = Vector3.up * y;
        storedVisuals.Add(obj);
    }

    // ─── 풀링 적용 RemoveTopVisual ────────────────────────
    void RemoveTopVisual()
    {
        if (storedVisuals.Count == 0) return;
        int last = storedVisuals.Count - 1;
        GameObject obj = storedVisuals[last];
        storedVisuals.RemoveAt(last);

        if (obj == null) return;

        // 풀에 반환
        if (!string.IsNullOrEmpty(poolTag) && ObjectPool.Instance != null)
            ObjectPool.Instance.Return(poolTag, obj);
        else
            Destroy(obj);
    }

    void UpdateCountUI()
    {
        if (countText != null)
            countText.text = StoredCount > 0 ? StoredCount.ToString() : "";
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}