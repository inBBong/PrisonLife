// QuarryArea.cs - 오브젝트 풀링 적용 버전

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuarryArea : MonoBehaviour
{
    [Header("Rock Spawning")]
    public GameObject rockPrefab;
    public int gridWidth = 6;
    public int gridHeight = 5;
    public float gridSpacingX = 1.2f;
    public float gridSpacingZ = 1.0f;
    public float heightVariance = 0.1f;
    public float scaleVariance = 0.2f;

    [Header("Grid Offset")]
    [Tooltip("QuarryArea 위치는 고정하고 돌 그리드만 이동")]
    public Vector3 gridOffset = Vector3.zero;

    [Header("Respawn")]
    public float respawnDelay = 4f;

    [Header("Quarry Boundary")]
    public BoxCollider quarryBoundary;

    [Header("Rock Container")]
    [Tooltip("돌들의 부모 오브젝트 (QuarryArea 스케일 영향 안 받음)")]
    public Transform rockContainer;

    private List<Vector3> spawnPositions = new List<Vector3>();
    private HashSet<int> emptySlots = new HashSet<int>();

    // 풀 태그
    private const string ROCK_TAG = "Rock";

    void Start()
    {
        // ObjectPool에 Rock 풀이 없으면 경고
        if (ObjectPool.Instance == null)
            Debug.LogWarning("[QuarryArea] ObjectPool이 없습니다! 씬에 ObjectPool을 추가하세요.");

        GenerateSpawnGrid();
        SpawnAllRocks();
    }

    void GenerateSpawnGrid()
    {
        spawnPositions.Clear();
        Vector3 origin = transform.position + gridOffset;
        float startX = -(gridWidth - 1) * gridSpacingX * 0.5f;
        float startZ = -(gridHeight - 1) * gridSpacingZ * 0.5f;

        for (int z = 0; z < gridHeight; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                float px = origin.x + startX + x * gridSpacingX + Random.Range(-0.2f, 0.2f);
                float pz = origin.z + startZ + z * gridSpacingZ + Random.Range(-0.2f, 0.2f);
                float py = origin.y + Random.Range(-heightVariance, heightVariance);
                spawnPositions.Add(new Vector3(px, py, pz));
            }
        }
    }

    void SpawnAllRocks()
    {
        for (int i = 0; i < spawnPositions.Count; i++)
            SpawnRockAt(i);
    }

    void SpawnRockAt(int index)
    {
        Vector3 pos = spawnPositions[index];
        float scale = 1f + Random.Range(-scaleVariance, scaleVariance);
        Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

        GameObject rock = null;

        if (ObjectPool.Instance != null)
        {
            rock = ObjectPool.Instance.Get(ROCK_TAG, pos, rot);
            Debug.Log($"[QuarryArea] 풀에서 꺼냄: {(rock != null ? rock.name : "NULL")}");
        }

        // 풀에서 못 가져왔으면 직접 생성
        if (rock == null)
        {
            Debug.LogWarning("[QuarryArea] 풀에서 꺼내기 실패 → Instantiate");
            if (rockPrefab == null) return;
            rock = Instantiate(rockPrefab, pos, rot);
        }

        rock.transform.position = pos;
        rock.transform.rotation = rot;
        rock.name = $"Rock_{index}";

        // QuarryArea 스케일 영향 받지 않도록 부모 설정 안 함
        // 대신 씬 루트에 두거나 별도 컨테이너에 넣기
        if (rockContainer != null)
            rock.transform.SetParent(rockContainer);
        else
            rock.transform.SetParent(null); // 씬 루트

        // 스케일은 항상 원본 유지
        rock.transform.localScale = Vector3.one * scale;

        // QuarryRock에 QuarryArea 직접 주입 (부모가 아니므로 GetComponentInParent 불가)
        QuarryRock qr = rock.GetComponent<QuarryRock>();
        if (qr != null)
        {
            qr.SetParentArea(this);
            qr.OnSpawn();
        }

        emptySlots.Remove(index);
        Debug.Log($"[QuarryArea] Rock_{index} 스폰 완료");
    }

    public void OnRockCollected(QuarryRock rock)
    {
        string[] parts = rock.gameObject.name.Split('_');
        Debug.Log($"[QuarryArea] 바위 수집: {rock.gameObject.name}, parts: {parts.Length}");

        if (parts.Length >= 2 && int.TryParse(parts[1], out int idx))
        {
            Debug.Log($"[QuarryArea] 슬롯 {idx} 리스폰 예약");
            emptySlots.Add(idx);
            StartCoroutine(RespawnAfterDelay(idx));
        }
        else
        {
            // 이름 파싱 실패 시 빈 슬롯 아무거나 리스폰
            Debug.LogWarning($"[QuarryArea] 이름 파싱 실패: {rock.gameObject.name} → 랜덤 슬롯 리스폰");
            StartCoroutine(RespawnAnySlot());
        }
    }

    IEnumerator RespawnAnySlot()
    {
        yield return new WaitForSeconds(respawnDelay);
        Transform container = rockContainer != null ? rockContainer : transform;
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            bool hasRock = false;
            foreach (Transform child in container)
            {
                if (child.gameObject.name == $"Rock_{i}" && child.gameObject.activeSelf)
                {
                    hasRock = true;
                    break;
                }
            }
            if (!hasRock)
            {
                SpawnRockAt(i);
                break;
            }
        }
    }

    IEnumerator RespawnAfterDelay(int slotIndex)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (emptySlots.Contains(slotIndex))
            SpawnRockAt(slotIndex);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInventory inv = other.GetComponent<PlayerInventory>();
        if (inv != null) inv.IsInQuarry = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInventory inv = other.GetComponent<PlayerInventory>();
        if (inv != null) inv.IsInQuarry = false;
    }
}