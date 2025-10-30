using UnityEngine;

public class CursorWorldLight : MonoBehaviour
{
    // === 인스펙터 설정 ===
    [Header("Dependencies")]
    [SerializeField]
    private CursorManager cursorManager;

    [SerializeField]
    private Light cursorPointLight;

    [Header("Light Settings")]
    [Tooltip("월드 스페이스에서 PointLight의 거리 (카메라와의 거리)")]
    public float lightDistance = 5f;

    [Tooltip("조명이 닿는 최대 거리")]
    public float lightRange = 10f;

    // === 내부 변수 ===
    private Camera mainCamera;

    // 이 변수를 이용해 '미약한 조명'의 강도를 설정합니다.
    [Header("Base Light Intensity")]
    [Tooltip("미약한 조명의 기본 강도")]
    public float baseIntensity = 0.5f;

    void Start()
    {
        mainCamera = Camera.main;

        if (cursorManager == null || cursorPointLight == null || mainCamera == null)
        {
            Debug.LogError("CursorWorldLight requires CursorManager, a Light component, and Camera.main to be present.");
            enabled = false;
            return;
        }

        // --- 수정된 부분 ---
        // 조명은 항상 켜져 있으며, baseIntensity로 강도가 설정됩니다.
        cursorPointLight.intensity = baseIntensity;
        cursorPointLight.range = lightRange;
        // ---
    }

    void Update()
    {
        // 1. 마우스 포인터 위치 조명 기능 (월드 스페이스)은 그대로 유지하여 커서를 따라다닙니다.
        UpdateCursorPositionLight();

        // 2. 주기적 발광 기능 (UpdateFlashMechanic) 호출을 제거합니다.
    }

    // 마우스의 스크린 좌표를 월드 좌표로 변환하여 라이트 위치를 업데이트합니다. (로직 변경 없음)
    private void UpdateCursorPositionLight()
    {
        Vector3 screenPos = cursorManager.CursorPosition;
        screenPos.z = lightDistance;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        transform.position = worldPos;
    }

    // UpdateFlashMechanic 함수는 이제 필요 없으므로 이 스크립트에서 제거합니다.
}