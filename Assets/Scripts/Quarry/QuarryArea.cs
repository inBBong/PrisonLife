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

    [Header("Respawn")]
    public float respawnDelay = 4f;

    [Header("Quarry Boundary")]
    public BoxCollider quarryBoundary;

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
        Vector3 origin = transform.position;
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
            //Debug.Log($"[QuarryArea] 풀에서 꺼냄: {(rock != null ? rock.name : "NULL")}");
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
        rock.transform.localScale = Vector3.one * scale;
        rock.transform.SetParent(transform);
        rock.name = $"Rock_{index}";

        // QuarryArea 참조 업데이트
        QuarryRock qr = rock.GetComponent<QuarryRock>();
        if (qr != null) qr.OnSpawn();

        emptySlots.Remove(index);
        //Debug.Log($"[QuarryArea] Rock_{index} 스폰 완료");
    }

    public void OnRockCollected(QuarryRock rock)
    {
        string[] parts = rock.gameObject.name.Split('_');
        //Debug.Log($"[QuarryArea] 바위 수집: {rock.gameObject.name}, parts: {parts.Length}");

        if (parts.Length >= 2 && int.TryParse(parts[1], out int idx))
        {
            //Debug.Log($"[QuarryArea] 슬롯 {idx} 리스폰 예약");
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
        // 전체 슬롯 중 비어있는 곳 찾아서 리스폰
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            // 해당 슬롯에 바위가 없으면 리스폰
            bool hasRock = false;
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == $"Rock_{i}")
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