using UnityEngine;

public class CameraOptimizer : MonoBehaviour
{
    [Header("Resolution")]
    public int screenWidth = 720;
    public int screenHeight = 1280;

    [Header("Performance")]
    public int targetFrameRate = 60;

    void Awake()
    {
        // 720x1280 창 모드로 강제 설정
        Screen.SetResolution(screenWidth, screenHeight, false);

        // 프레임 설정
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
    }
}
