using UnityEngine;

public class WorkerSpawner : MonoBehaviour
{
    [Header("Quarry Worker")]
    public GameObject quarryWorkerPrefab;
    public int quarryWorkerCount = 3;
    public Transform[] quarrySpawnPoints;
    public QuarryArea quarryArea;
    public SetterZone stoneSetterZone;

    [Header("Delivery Worker")]
    public GameObject deliveryWorkerPrefab;
    public int deliveryWorkerCount = 1;
    public Transform[] deliverySpawnPoints;
    public GetterZone handcuffGetterZone;
    public SetterZone handcuffSetterZone;

    // MoneyZoneUpgrade → OnUpgradeComplete에 연결
    public void SpawnQuarryWorkers()
    {
        Debug.Log($"[WorkerSpawner] SpawnQuarryWorkers 호출! 인부 수: {quarryWorkerCount}");
        Debug.Log($"[WorkerSpawner] prefab: {(quarryWorkerPrefab != null ? quarryWorkerPrefab.name : "NULL")}");
        Debug.Log($"[WorkerSpawner] quarryArea: {(quarryArea != null ? quarryArea.name : "NULL")}");

        for (int i = 0; i < quarryWorkerCount; i++)
        {
            Vector3 pos = quarrySpawnPoints != null && i < quarrySpawnPoints.Length
                ? quarrySpawnPoints[i].position
                : transform.position + new Vector3(i * 1.2f, 0, 0);

            GameObject obj = Instantiate(quarryWorkerPrefab, pos, Quaternion.identity);
            QuarryWorker worker = obj.GetComponent<QuarryWorker>();
            if (worker != null)
            {
                worker.quarryArea = quarryArea;
                worker.stoneSetterZone = stoneSetterZone;
                int level = GameManager.Instance != null
                    ? GameManager.Instance.miningToolLevel : 1;
                worker.carryCapacity = level == 1 ? 5 : level == 2 ? 8 : 15;
            }
            Debug.Log($"[WorkerSpawner] 채석 인부 {i + 1} 스폰");
        }

        // 채석 인부 고용 후 배달부 존 활성화
        if (deliveryWorkerUnlockZone != null)
            deliveryWorkerUnlockZone.Unlock();
    }

    // 배달부 고용 존 (채석 인부 고용 후 활성화)
    public MoneyZoneUpgrade deliveryWorkerUnlockZone;

    public void SpawnDeliveryWorkers()
    {
        for (int i = 0; i < deliveryWorkerCount; i++)
        {
            Vector3 pos = deliverySpawnPoints != null && i < deliverySpawnPoints.Length
                ? deliverySpawnPoints[i].position
                : transform.position + new Vector3(i * 1.2f, 0, 2f);

            GameObject obj = Instantiate(deliveryWorkerPrefab, pos, Quaternion.identity);
            HandcuffDeliveryWorker worker = obj.GetComponent<HandcuffDeliveryWorker>();
            if (worker != null)
            {
                worker.handcuffGetterZone = handcuffGetterZone;
                worker.handcuffSetterZone = handcuffSetterZone;
            }
            Debug.Log($"[WorkerSpawner] 배달 직원 {i + 1} 스폰");
        }
    }
}