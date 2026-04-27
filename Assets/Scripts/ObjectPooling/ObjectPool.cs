// ObjectPool.cs
// 범용 오브젝트 풀링 시스템
// 생성/삭제 대신 활성화/비활성화로 성능 최적화

using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        [Tooltip("이 풀 아이템들의 부모 (없으면 기본 itemRoot 사용)")]
        public Transform itemRoot;
    }

    [Header("Pools")]
    public List<Pool> pools;

    [Header("Default Item Root")]
    [Tooltip("Pool별 itemRoot가 없을 때 사용할 기본 부모")]
    public Transform defaultItemRoot;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;
    private Dictionary<string, Transform> rootDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();
        rootDictionary = new Dictionary<string, Transform>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();
            prefabDictionary[pool.tag] = pool.prefab;

            // Pool별 itemRoot 또는 defaultItemRoot 저장
            Transform root = pool.itemRoot != null ? pool.itemRoot : defaultItemRoot;
            rootDictionary[pool.tag] = root;

            // 초기 오브젝트 생성
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab, pool.tag, root);
                objectQueue.Enqueue(obj);
            }

            poolDictionary[pool.tag] = objectQueue;
        }
    }

    GameObject CreateNewObject(GameObject prefab, string tag, Transform root = null)
    {
        Transform parent = root != null ? root : transform;
        GameObject obj = Instantiate(prefab, parent);
        obj.name = $"{tag}_Pooled";
        obj.SetActive(false);
        return obj;
    }

    // ─── 풀에서 오브젝트 가져오기 ────────────────────────
    public GameObject Get(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] '{tag}' 풀이 없습니다!");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[tag];

        // 풀이 비어있으면 새로 생성
        if (queue.Count == 0)
        {
            if (prefabDictionary.ContainsKey(tag))
            {
                GameObject newObj = CreateNewObject(prefabDictionary[tag], tag);
                queue.Enqueue(newObj);
            }
        }

        GameObject obj = queue.Dequeue();
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        // Pool별 지정된 root로 부모 설정
        Transform root = rootDictionary.ContainsKey(tag) ? rootDictionary[tag] : defaultItemRoot;
        obj.transform.SetParent(root);
        obj.SetActive(true);

        // IPoolable 인터페이스 구현 시 초기화 호출
        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnSpawn();

        return obj;
    }

    // 부모 지정 버전
    public GameObject Get(string tag, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject obj = Get(tag, position, rotation);
        if (obj != null) obj.transform.SetParent(parent);
        return obj;
    }

    // ─── 풀에 오브젝트 반환 ───────────────────────────────
    public void Return(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Destroy(obj);
            return;
        }

        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnDespawn();

        obj.SetActive(false);
        // 반환 시 원래 root로 복귀
        Transform root = rootDictionary.ContainsKey(tag) ? rootDictionary[tag] : transform;
        obj.transform.SetParent(root);
        poolDictionary[tag].Enqueue(obj);
    }

    // 태그 자동 반환 (PooledObject 컴포넌트 사용)
    public void Return(GameObject obj)
    {
        PooledObject po = obj.GetComponent<PooledObject>();
        if (po != null) Return(po.poolTag, obj);
        else Destroy(obj);
    }
}

// ─── 풀링 가능 오브젝트 인터페이스 ───────────────────────
public interface IPoolable
{
    void OnSpawn();     // 풀에서 꺼낼 때
    void OnDespawn();   // 풀에 반환할 때
}