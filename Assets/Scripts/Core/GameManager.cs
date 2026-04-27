using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Economy")]
    public int totalMoney = 0;

    [Header("Mining Tool Levels")]
    public int miningToolLevel = 1;
    // Lv1: 10개, Lv2: 15개, Lv3: 40개
    public int[] maxStonesByLevel = { 10, 15, 40 };
    public int[] toolUpgradeCost = { 50, 150, 400 };
    // Lv1: 1.0초, Lv2: 0.5초, Lv3: 0.1초
    public float[] miningCooldownByLevel = { 1.0f, 0.5f, 0.1f };

    public float MiningCooldown =>
        miningCooldownByLevel[Mathf.Clamp(miningToolLevel - 1, 0, miningCooldownByLevel.Length - 1)];

    [Header("Workers")]
    public int quarryWorkerCount = 0;
    public int deliveryWorkerCount = 0;
    public int workerCost = 100;

    public int MaxStonesCarry =>
        maxStonesByLevel[Mathf.Clamp(miningToolLevel - 1, 0, maxStonesByLevel.Length - 1)];

    public bool CanUpgradeTool => miningToolLevel <= toolUpgradeCost.Length;
    public int NextToolUpgradeCost =>
        miningToolLevel <= toolUpgradeCost.Length ? toolUpgradeCost[miningToolLevel - 1] : int.MaxValue;

    public event Action<int> OnMoneyChanged;
    public event Action<int> OnToolLevelChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void AddMoney(int amount)
    {
        totalMoney += amount * 10;
        OnMoneyChanged?.Invoke(totalMoney);
    }

    public bool SpendMoney(int amount)
    {
        if (totalMoney < amount) return false;
        totalMoney -= amount;
        OnMoneyChanged?.Invoke(totalMoney);
        return true;
    }

    public bool CanAfford(int amount) => totalMoney >= amount;

    // ─── 돈 차감 없이 레벨만 올림 ────────────────────────
    // MoneyZoneUpgrade에서 이미 돈을 차감했으므로
    // 여기서는 레벨 증가만 처리
    public void UpgradeMiningToolLevel()
    {
        if (!CanUpgradeTool) return;
        miningToolLevel++;
        OnToolLevelChanged?.Invoke(miningToolLevel);
        Debug.Log($"[GameManager] 채석 도구 Lv{miningToolLevel} 업그레이드 완료!");
    }

    // 돈 차감 포함 업그레이드 (기존 방식, 호환성 유지)
    public bool UpgradeMiningTool()
    {
        if (!CanUpgradeTool) return false;
        int cost = NextToolUpgradeCost;
        if (!SpendMoney(cost)) return false;
        miningToolLevel++;
        OnToolLevelChanged?.Invoke(miningToolLevel);
        return true;
    }

    public bool HireQuarryWorker()
    {
        quarryWorkerCount++;
        Debug.Log($"[GameManager] 채석 인부 고용! 총 {quarryWorkerCount}명");
        return true;
    }

    public bool HireDeliveryWorker()
    {
        deliveryWorkerCount++;
        return true;
    }
}