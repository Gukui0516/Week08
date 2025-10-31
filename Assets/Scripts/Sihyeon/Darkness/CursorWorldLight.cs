using UnityEngine;

// CursorManager가 제공하는 스크린 좌표를 따라 월드 스페이스에 두 개의 PointLight를 배치합니다.
// 배치 위치는 커서 아래의 3D 오브젝트 표면을 SphereCasting하여 결정됩니다. (더 넓은 탐지 범위)
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
    [Tooltip("카메라에서 Light가 떨어져 배치될 월드 스페이스 거리 (깊이 / SphereCast 최대 거리)")]
    public float lightDistance = 5f;

    [Tooltip("Raycast 충돌 표면에서 카메라 방향으로 라이트가 후퇴할 거리")]
    public float surfaceOffset = 0.25f;

    [Header("SphereCast Setting")]
    [Tooltip("SphereCast의 반경. 이 값이 클수록 커서 주변의 물체도 잘 감지합니다.")]
    public float sphereRadius = 0.1f; // SphereCast에 사용할 월드 스페이스 반경

    private Camera mainCamera;

    void Start()
    {
        // ... (Start 함수 내용은 이전과 동일)
        mainCamera = Camera.main;

        if (cursorManager == null || mainBlockLight == null || ambientSceneLight == null || mainCamera == null)
        {
            // 에러 처리 및 초기화 로그는 이전 코드를 참조하여 유지하십시오.
            // 생략: 에러 처리 및 초기 위치 설정
            if (cursorManager == null) Debug.LogError("[LGT_FAIL] Cursor Manager reference is MISSING.");
            if (mainBlockLight == null || ambientSceneLight == null) Debug.LogError("[LGT_FAIL] One or both Cursor Light components are MISSING.");
            if (mainCamera == null) Debug.LogError("[LGT_FAIL] Main Camera is MISSING or NOT TAGGED.");
            if (cursorManager == null || mainBlockLight == null || ambientSceneLight == null || mainCamera == null)
            {
                enabled = false;
                return;
            }

            Vector3 initialScreenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, lightDistance);
            transform.position = mainCamera.ScreenToWorldPoint(initialScreenCenter);
            Debug.LogWarning($"[LGT_INIT] Lights initialized and positioned at: {transform.position}");
        }
    }

    void Update()
    {
        UpdateCursorPositionLightWithSphereCast(); // 함수명 변경
    }

    private void UpdateCursorPositionLightWithSphereCast()
    {
        Vector3 screenPos = cursorManager.CursorPosition;

        // 스크린 좌표에서 Ray를 생성합니다. (Ray의 원점과 방향 사용)
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // --- [디버그 시작] ---
        // Scene 뷰에서 Ray의 경로를 빨간색 선으로 시각화합니다.
        // SphereCast 경로를 시각화하는 것은 복잡하므로, 중심선만 표시합니다.
        Debug.DrawRay(ray.origin, ray.direction * lightDistance, Color.red);
        // --- [디버그 끝] ---

        RaycastHit hit;

        // Raycast 대신 Physics.SphereCast를 사용하여 탐지합니다.
        // SphereCast(Ray의 원점, 반경, Ray의 방향, 충돌 정보, 최대 거리)
        if (Physics.SphereCast(ray.origin, sphereRadius, ray.direction, out hit, lightDistance))
        {
            // SphereCast가 오브젝트와 충돌했을 때:

            // --- [디버그 시작] ---
            // 충돌이 발생한 Ray 부분을 녹색 선으로 표시합니다.
            Debug.DrawLine(ray.origin, hit.point, Color.green);
            Debug.Log($"[LGT_HIT] SphereCast Hit Object: {hit.collider.name} at {hit.point}. Light Position calculated.");
            // --- [디버그 끝] ---

            // 충돌 지점에서 카메라 방향으로 surfaceOffset 만큼 후퇴하여 라이트 위치를 결정합니다.
            Vector3 lightPosition = hit.point + (-ray.direction * surfaceOffset);

            transform.position = lightPosition;
        }
        else
        {
            // SphereCast가 오브젝트와 충돌하지 않았을 때:

            // --- [디버그 시작] ---
            Debug.Log("[LGT_FAIL] SphereCast Missed. Falling back to fixed distance.");
            // --- [디버그 끝] ---

            // Fallback 로직: 카메라로부터 lightDistance 만큼 떨어진 고정 위치에 배치합니다.
            screenPos.z = lightDistance;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

            transform.position = worldPos;
        }
    }
}