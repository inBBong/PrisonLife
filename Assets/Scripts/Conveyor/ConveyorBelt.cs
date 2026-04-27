// ConveyorBelt.cs
// 가공 컨베이어벨트
// 돌 Setter존에서 돌을 가져와 처리 시간 후 수갑 Getter존에 수갑 추가

using UnityEngine;
using System.Collections;

public class ConveyorBelt : MonoBehaviour
{
    [Header("References")]
    public SetterZone stoneInput;           // 돌 투입 존
    public GetterZone handcuffOutput;       // 수갑 배출 존

    [Header("Processing")]
    [Tooltip("돌 1개를 수갑으로 가공하는 데 걸리는 시간 (초)")]
    public float processTimePerStone = 1.5f;

    [Header("Conveyor Animation")]
    public Transform[] conveyorItems;       // 컨베이어 위 이동 아이템들 (선택)
    public float beltSpeed = 1f;

    [Header("Processing Visual")]
    public GameObject processingIndicator;  // 가공 중 표시 이펙트
    public TMPro.TextMeshProUGUI statusText;

    private bool isProcessing = false;
    private float processProgress = 0f;
    private Coroutine processCoroutine;

    void Start()
    {
        if (stoneInput == null || handcuffOutput == null)
        {
            Debug.LogError("[ConveyorBelt] stoneInput 또는 handcuffOutput이 할당되지 않았습니다!");
            return;
        }

        stoneInput.OnCountChanged += OnStoneInputChanged;
        processCoroutine = StartCoroutine(ProcessLoop());
    }

    void OnDestroy()
    {
        if (stoneInput != null)
            stoneInput.OnCountChanged -= OnStoneInputChanged;
    }

    void OnStoneInputChanged(int count)
    {
        // 돌이 들어오면 처리 루프가 자동으로 감지
    }

    IEnumerator ProcessLoop()
    {
        while (true)
        {
            // 돌이 있으면 처리
            if (stoneInput != null && stoneInput.StoredCount > 0)
            {
                // 돌 1개 꺼내기
                stoneInput.TakeItem();
                isProcessing = true;
                processProgress = 0f;
                SetProcessingVisual(true);

                // 가공 시간 대기
                float elapsed = 0f;
                while (elapsed < processTimePerStone)
                {
                    elapsed += Time.deltaTime;
                    processProgress = elapsed / processTimePerStone;
                    UpdateStatusText();
                    yield return null;
                }

                // 수갑 완성
                isProcessing = false;
                processProgress = 0f;
                SetProcessingVisual(false);

                if (handcuffOutput != null)
                    handcuffOutput.AddItem();
            }
            else
            {
                UpdateStatusText();
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    void SetProcessingVisual(bool active)
    {
        if (processingIndicator != null)
            processingIndicator.SetActive(active);
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        if (isProcessing)
            statusText.text = $"Processing... {(int)(processProgress * 100)}%";
        else if (stoneInput != null && stoneInput.StoredCount == 0)
            statusText.text = "Idle";
        else
            statusText.text = "Ready";
    }

    void Update()
    {
        // 컨베이어 벨트 오브젝트 이동 애니메이션 (선택)
        if (conveyorItems != null)
        {
            foreach (var item in conveyorItems)
            {
                if (item == null) continue;
                item.localPosition += Vector3.right * beltSpeed * Time.deltaTime;
                if (item.localPosition.x > 1.5f)
                    item.localPosition = new Vector3(-1.5f, item.localPosition.y, item.localPosition.z);
            }
        }
    }
}
