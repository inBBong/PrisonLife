// SceneSetupHelper.cs
// 에디터 전용 씬 자동 구성 헬퍼
// Menu: PrisonLife > Setup Scene

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SceneSetupHelper : EditorWindow
{
    [MenuItem("PrisonLife/Setup Scene Layout")]
    static void SetupScene()
    {
        // ─── 레이아웃 참고용 오브젝트 생성 ─────────────
        // (실제 씬 구성 시 이 위치들을 참고해서 오브젝트 배치)

        Debug.Log("=== Prison Life 씬 구성 가이드 ===");
        Debug.Log("1. Quarry Area: (0, 0, 8) - 채석장");
        Debug.Log("2. Conveyor Belt: (0, 0, 0) - 컨베이어벨트");
        Debug.Log("3. Prisoner Entrance: (0, 0, -8) - 수감자 입구");
        Debug.Log("4. Prison: (6, 0, -8) - 수용소");
        Debug.Log("5. Player Start: (0, 0.5, 2) - 플레이어 시작 위치");
        Debug.Log("6. Camera: (0, 12, -6) Rotation(55, 0, 0) - 카메라");
        Debug.Log("===================================");
    }

    [MenuItem("PrisonLife/Create Game Objects")]
    static void CreateGameObjects()
    {
        // GameManager
        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            Debug.Log("GameManager 생성됨");
        }

        // PrisonManager
        if (FindObjectOfType<PrisonManager>() == null)
        {
            GameObject pm = new GameObject("PrisonManager");
            pm.AddComponent<PrisonManager>();
            Debug.Log("PrisonManager 생성됨");
        }

        // UIManager
        if (FindObjectOfType<UIManager>() == null)
        {
            GameObject um = new GameObject("UIManager");
            um.AddComponent<UIManager>();
            Debug.Log("UIManager 생성됨");
        }

        Debug.Log("기본 매니저 오브젝트 생성 완료! Inspector에서 참조를 연결해주세요.");
    }
}
#endif
