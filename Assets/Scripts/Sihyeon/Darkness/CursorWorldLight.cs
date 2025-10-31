using UnityEngine;

// CursorManager가 제공하는 스크린 좌표를 따라 월드 스페이스에 두 개의 PointLight를 배치합니다.
public class CursorWorldLight : MonoBehaviour
{
    // === Dependencies (참조만 유지) ===
    [Header("Dependencies")]
    [Tooltip("커서의 스크린 좌표를 제공하는 CursorManager 스크립트를 연결하십시오.")]
    [SerializeField]
    private CursorManager cursorManager;

    [Header("Cursor Lights")]
    [Tooltip("젠가 블록만 비추는 Light (Rendering Layer Mask 설정 필요)")]
    [SerializeField]
    private Light mainBlockLight;

    [Tooltip("씬 전체의 일반 오브젝트를 비추는 Light (Rendering Layer Mask 설정 필요)")]
    [SerializeField]
    private Light ambientSceneLight;

    [Header("Position Setting")]
    [Tooltip("카메라에서 Light가 떨어져 배치될 월드 스페이스 거리 (깊이)")]
    public float lightDistance = 5f; // 위치 설정은 코드가 담당해야 하므로 유지

    private Camera mainCamera;

    void Start()
    {
        // 1. 초기 검증 및 캐싱
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cursorManager == null)
        {
            Debug.LogError("[LGT_FAIL] Cursor Manager reference is MISSING.");
            enabled = false;
            return;
        }

        // 두 Light 모두 할당되었는지 확인
        if (mainBlockLight == null || ambientSceneLight == null)
        {
            Debug.LogError("[LGT_FAIL] One or both Cursor Light components are MISSING in Inspector.");
            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[LGT_FAIL] Main Camera is MISSING or NOT TAGGED. WorldToScreen conversion will fail.");
            enabled = false;
            return;
        }

        // **2. 라이트 설정 로직 제거:** // Light의 Intensity, Range, Color 등 모든 세팅은 이제 인스펙터의 디폴트 값을 사용합니다.

        // 3. 초기 위치 강제 설정
        Vector3 initialScreenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, lightDistance);
        transform.position = mainCamera.ScreenToWorldPoint(initialScreenCenter);
        Debug.LogWarning($"[LGT_INIT] Lights initialized and positioned at: {transform.position}");
    }

    void Update()
    {
        UpdateCursorPositionLight();
    }

    private void UpdateCursorPositionLight()
    {
        // 커서 위치를 CursorManager에서 가져옵니다.
        Vector3 screenPos = cursorManager.CursorPosition;

        // Z 값을 카메라로부터의 거리(depth)로 설정합니다.
        screenPos.z = lightDistance;

        // 스크린 좌표를 월드 좌표로 변환
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

        // 라이트 위치 업데이트 (Light의 디폴트 세팅은 유지된 채 위치만 이동)
        transform.position = worldPos;
    }
}