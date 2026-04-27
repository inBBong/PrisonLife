// PlayerToolController.cs
// 채석장 진입 시 레벨에 맞는 툴 표시
// 채석장 밖이거나 수갑 소지 중이면 툴 숨김

using UnityEngine;

public class PlayerToolController : MonoBehaviour
{
    [Header("Tool Prefabs")]
    [Tooltip("0=Lv1(곡괭이), 1=Lv2(오른손곡괭이), 2=Lv3(포크레인)")]
    public GameObject[] toolPrefabs;

    [Tooltip("Lv2 왼손 전용 곡괭이 프리팹 (오른손과 별도)")]
    public GameObject lv2LeftToolPrefab;

    [Header("Tool Anchors")]
    [Tooltip("오른손 위치")]
    public Transform rightToolAnchor;
    [Tooltip("왼손 위치 (Lv2 양손 곡괭이용)")]
    public Transform leftToolAnchor;

    [Header("References")]
    public PlayerInventory inventory;

    [Header("Lv3 Excavator Settings")]
    [Tooltip("Lv3일 때 숨길 플레이어 메시 오브젝트들")]
    public GameObject[] playerMeshObjects;  // Group1 등 플레이어 비주얼
    [Tooltip("포크레인이 붙을 앵커 (플레이어 발 위치)")]
    public Transform excavatorAnchor;

    private GameObject rightToolInstance;
    private GameObject leftToolInstance;
    private int currentToolLevel = -1;
    private bool isExcavatorMode = false;

    void Update()
    {
        if (inventory == null || GameManager.Instance == null) return;

        // 채석장 안에 있고 수갑 없을 때만 툴 표시
        bool shouldShowTool = inventory.IsInQuarry && !inventory.HasHandcuffs;
        int toolLevel = GameManager.Instance.miningToolLevel - 1; // 0-based

        if (shouldShowTool)
        {
            // 레벨이 바뀌었으면 툴 교체
            if (currentToolLevel != toolLevel)
                UpdateTool(toolLevel);

            if (rightToolInstance != null) rightToolInstance.SetActive(true);
            if (leftToolInstance != null) leftToolInstance.SetActive(true);
        }
        else
        {
            // 채석장 밖이거나 수갑 소지 중 → 툴 숨김
            if (rightToolInstance != null) rightToolInstance.SetActive(false);
            if (leftToolInstance != null) leftToolInstance.SetActive(false);
        }
    }

    void UpdateTool(int levelIndex)
    {
        // 기존 툴 모두 제거
        if (rightToolInstance != null) Destroy(rightToolInstance);
        if (leftToolInstance != null) Destroy(leftToolInstance);

        currentToolLevel = levelIndex;

        if (toolPrefabs == null || levelIndex >= toolPrefabs.Length) return;
        if (toolPrefabs[levelIndex] == null) return;

        if (levelIndex == 2) // Lv3: 포크레인 탑승 모드
        {
            EnterExcavatorMode(levelIndex);
        }
        else
        {
            // 포크레인 모드 해제 (혹시 다운그레이드 시)
            ExitExcavatorMode();

            if (levelIndex == 1) // Lv2: 양손 곡괭이
            {
                if (rightToolAnchor != null)
                {
                    // 오른손: 기본 곡괭이 프리팹
                    rightToolInstance = Instantiate(toolPrefabs[levelIndex], rightToolAnchor);
                }
                if (leftToolAnchor != null)
                {
                    // 왼손: 왼손 전용 프리팹 (없으면 기본 프리팹 사용)
                    GameObject leftPrefab = lv2LeftToolPrefab != null
                        ? lv2LeftToolPrefab
                        : toolPrefabs[levelIndex];
                    leftToolInstance = Instantiate(leftPrefab, leftToolAnchor);
                }
            }
            else // Lv1: 오른손 곡괭이
            {
                Transform anchor = rightToolAnchor != null ? rightToolAnchor : transform;
                rightToolInstance = Instantiate(toolPrefabs[levelIndex], anchor);
                // 프리팹 로컬 트랜스폼 그대로 유지
            }
        }

        Debug.Log($"[PlayerToolController] Lv{levelIndex + 1} 툴 장착!");
    }

    void EnterExcavatorMode(int levelIndex)
    {
        isExcavatorMode = true;

        // 플레이어 메시 숨기기
        foreach (var mesh in playerMeshObjects)
            if (mesh != null) mesh.SetActive(false);

        // 포크레인 생성 (플레이어 발 위치에 부착)
        Transform anchor = excavatorAnchor != null ? excavatorAnchor : transform;
        rightToolInstance = Instantiate(toolPrefabs[levelIndex], anchor);
        // 프리팹 로컬 트랜스폼 그대로 유지
    }

    void ExitExcavatorMode()
    {
        if (!isExcavatorMode) return;
        isExcavatorMode = false;

        // 플레이어 메시 다시 보이기
        foreach (var mesh in playerMeshObjects)
            if (mesh != null) mesh.SetActive(true);
    }

    // 채석장 나갈 때 포크레인 모드도 해제
    void OnDisable()
    {
        ExitExcavatorMode();
    }
}