using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class SetterZone : InteractionZone
{
    [Header("Zone Type")]
    public ItemType acceptedItemType;

    [Header("Storage")]
    public int maxCapacity = 60;
    public Transform stackAnchor;
    public GameObject storedItemPrefab;
    public float stackSpacingY = 0.12f;

    [Header("Pool Tag")]
    public string poolTag = "";  // 비어있으면 Instantiate 사용

    [Header("Capacity UI")]
    public TextMeshProUGUI capacityText;

    public int StoredCount { get; private set; }
    public event Action<int> OnCountChanged;

    private List<GameObject> storedVisuals = new List<GameObject>();

    void Start() => UpdateCapacityUI();

    protected override bool DoInteraction()
    {
        if (StoredCount >= maxCapacity) return false;
        if (playerInventory == null) return false;

        bool took = false;
        switch (acceptedItemType)
        {
            case ItemType.Stone: took = playerInventory.RemoveStone(); break;
            case ItemType.Handcuff: took = playerInventory.RemoveHandcuff(); break;
            case ItemType.Money: took = playerInventory.RemoveMoney(); break;
        }

        if (took) AddItemInternal();
        return took;
    }

    public void AddItemDirectly()
    {
        if (StoredCount >= maxCapacity) return;
        AddItemInternal();
    }

    void AddItemInternal()
    {
        StoredCount++;
        OnCountChanged?.Invoke(StoredCount);
        UpdateCapacityUI();
        SpawnVisual();
    }

    public bool TakeItem()
    {
        if (StoredCount <= 0) return false;
        StoredCount--;
        OnCountChanged?.Invoke(StoredCount);
        UpdateCapacityUI();
        RemoveTopVisual();
        return true;
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

    void UpdateCapacityUI()
    {
        if (capacityText != null)
            capacityText.text = $"{StoredCount}/{maxCapacity}";
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}