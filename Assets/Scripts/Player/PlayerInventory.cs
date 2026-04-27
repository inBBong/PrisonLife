using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Carry Limits")]
    public int maxHandcuffs = 15;
    public int maxMoney = 50;

    [Header("Stack Anchors")]
    public Transform stoneStackAnchor;
    public Transform handcuffStackAnchor;
    public Transform moneyStackAnchor;

    [Header("Item Visual Prefabs (풀 미사용 시 폴백)")]
    public GameObject stonePrefab;
    public GameObject handcuffPrefab;
    public GameObject moneyBillPrefab;

    [Header("Object Pool Tags")]
    public string stonePoolTag = "RockStack";
    public string handcuffPoolTag = "Handcuff";
    public string moneyPoolTag = "Money";

    [Header("Stack Settings")]
    public float itemSpacingY = 0.18f;
    public float stackAnimSpeed = 8f;

    public int Stones { get; private set; }
    public int Handcuffs { get; private set; }
    public int Money { get; private set; }

    public bool IsInQuarry { get; set; }

    public int MaxStones => GameManager.Instance != null ? GameManager.Instance.MaxStonesCarry : 10;
    public bool StoneFull => Stones >= MaxStones;
    public bool HasHandcuffs => Handcuffs > 0;
    public bool HasMoney => Money > 0;
    public bool HasStones => Stones > 0;
    public bool CanMine => !HasHandcuffs && !StoneFull;

    private List<Transform> stoneVisuals = new List<Transform>();
    private List<Transform> handcuffVisuals = new List<Transform>();
    private List<Transform> moneyVisuals = new List<Transform>();

    private Vector3 stonePrefabScale = Vector3.one;
    private Vector3 handcuffPrefabScale = Vector3.one;
    private Vector3 moneyPrefabScale = Vector3.one;

    void Awake()
    {
        if (stonePrefab != null) stonePrefabScale = stonePrefab.transform.localScale;
        if (handcuffPrefab != null) handcuffPrefabScale = handcuffPrefab.transform.localScale;
        if (moneyBillPrefab != null) moneyPrefabScale = moneyBillPrefab.transform.localScale;
    }

    void Update()
    {
        UpdateStackPositions(stoneVisuals, stoneStackAnchor);
        UpdateStackPositions(handcuffVisuals, handcuffStackAnchor);
        UpdateStackPositions(moneyVisuals, moneyStackAnchor);
    }

    void UpdateStackPositions(List<Transform> visuals, Transform anchor)
    {
        if (anchor == null) return;
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] == null) continue;

            float lagFactor = 1f / (1f + i * 0.35f);
            float speed = stackAnimSpeed * lagFactor;

            // 바로 아래 아이템 체인 추적
            Vector3 targetPos;
            Quaternion targetRot;
            if (i == 0)
            {
                targetPos = anchor.position;
                targetRot = anchor.rotation;
            }
            else
            {
                targetPos = visuals[i - 1].position + Vector3.up * itemSpacingY;
                targetRot = visuals[i - 1].rotation;
            }

            visuals[i].position = Vector3.Lerp(visuals[i].position, targetPos, speed * Time.deltaTime);
            visuals[i].rotation = Quaternion.Lerp(visuals[i].rotation, targetRot, speed * Time.deltaTime);
        }
    }

    // ─── 돌 ───────────────────────────────────────────────
    public bool AddStone()
    {
        if (StoneFull) return false;
        if (Handcuffs > 0) return false;
        Stones++;
        SpawnVisual(stonePrefab, stonePrefabScale, stonePoolTag, stoneStackAnchor, stoneVisuals);
        OnInventoryChanged();
        return true;
    }

    public bool RemoveStone()
    {
        if (Stones <= 0) return false;
        Stones--;
        ReturnTopVisual(stoneVisuals, stonePoolTag);
        OnInventoryChanged();
        return true;
    }

    // ─── 수갑 ──────────────────────────────────────────────
    public bool AddHandcuff()
    {
        if (Handcuffs >= maxHandcuffs) return false;
        Handcuffs++;
        SpawnVisual(handcuffPrefab, handcuffPrefabScale, handcuffPoolTag, handcuffStackAnchor, handcuffVisuals);
        OnInventoryChanged();
        return true;
    }

    public bool RemoveHandcuff()
    {
        if (Handcuffs <= 0) return false;
        Handcuffs--;
        ReturnTopVisual(handcuffVisuals, handcuffPoolTag);
        OnInventoryChanged();
        return true;
    }

    // ─── 돈 ───────────────────────────────────────────────
    public bool AddMoney()
    {
        if (Money >= maxMoney) return false;
        Money++;
        SpawnVisual(moneyBillPrefab, moneyPrefabScale, moneyPoolTag, moneyStackAnchor, moneyVisuals);

        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(1);

        OnInventoryChanged();
        return true;
    }

    public bool RemoveMoney()
    {
        if (Money <= 0) return false;
        Money--;
        ReturnTopVisual(moneyVisuals, moneyPoolTag);
        OnInventoryChanged();
        return true;
    }

    // ─── 풀에서 꺼내기 ────────────────────────────────────
    void SpawnVisual(GameObject prefab, Vector3 originalScale,
                     string poolTag, Transform anchor, List<Transform> list)
    {
        if (anchor == null) return;

        float yOffset = list.Count * itemSpacingY;
        Vector3 spawnPos = anchor.position + Vector3.up * yOffset;

        GameObject obj = null;

        // 풀에서 꺼내기 (ObjectPool이 itemRoot 아래에 생성)
        if (ObjectPool.Instance != null)
            obj = ObjectPool.Instance.Get(poolTag, spawnPos, anchor.rotation);

        // 풀 없으면 직접 생성
        if (obj == null && prefab != null)
            obj = Instantiate(prefab, spawnPos, anchor.rotation);

        if (obj == null) return;

        // 스케일 적용
        obj.transform.localScale = originalScale;
        list.Add(obj.transform);
    }

    // ─── 풀에 반환 ────────────────────────────────────────
    void ReturnTopVisual(List<Transform> list, string poolTag)
    {
        if (list.Count == 0) return;
        Transform top = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);

        if (top == null) return;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(poolTag, top.gameObject);
        else
            Destroy(top.gameObject);
    }

    void OnInventoryChanged()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateInventoryDisplay(Stones, Handcuffs);
    }
}