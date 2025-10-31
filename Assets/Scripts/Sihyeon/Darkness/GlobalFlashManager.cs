using UnityEngine;
using System.Collections;

/// <summary>
/// Directional Light를 껐다 켜서 씬 전체에 주기적인 암전 및 섬광 효과를 구현합니다.
/// (CursorWorldLight의 미약 조명은 이 영향과 관계없이 항상 켜져 있어야 합니다.)
/// </summary>
public class GlobalFlashManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("씬에 있는 Directional Light (태양) 컴포넌트를 연결하십시오.")]
    [SerializeField] private Light directionalLight;

    [Header("Flashing Timing")]
    [Tooltip("섬광을 터뜨리기 전 암전 상태로 대기할 시간 (n초)")]
    public float blackoutDuration = 3.0f;

    [Tooltip("Directional Light가 켜져 섬광이 지속될 시간 (m초)")]
    public float flashDuration = 0.5f;

    // === 내부 변수 ===
    private float initialIntensity;

    void Start()
    {
        if (directionalLight == null)
        {
            // 씬에서 태그를 통해 Directional Light를 자동 탐색 시도
            if (GameObject.FindGameObjectWithTag("Light") != null)
            {
                directionalLight = GameObject.FindGameObjectWithTag("Light").GetComponent<Light>();
            }

            if (directionalLight == null)
            {
                Debug.LogError("[FLASH_FAIL] Directional Light 컴포넌트가 할당되지 않았습니다. 씬에 Light가 있는지 확인하십시오.");
                enabled = false;
                return;
            }
        }

        // Directional Light의 초기 강도를 저장합니다.
        initialIntensity = directionalLight.intensity;

        // 시작 시 암전 상태로 만듭니다.
        SetLightState(false);

        // 섬광 코루틴 시작
        StartCoroutine(FlashCycleCoroutine());
    }

    /// <summary>
    /// Directional Light의 상태를 제어합니다. (켜기 = 섬광 / 끄기 = 암전)
    /// </summary>
    private void SetLightState(bool enable)
    {
        if (enable)
        {
            // 섬광: 저장된 원래 강도로 설정
            directionalLight.intensity = initialIntensity;
            Debug.LogWarning($"[GLOBAL_FLASH] Flash ON! Duration: {flashDuration:F1}s");
        }
        else
        {
            // 암전: 강도를 0으로 설정
            directionalLight.intensity = 0f;
        }
    }

    private IEnumerator FlashCycleCoroutine()
    {
        while (true)
        {
            // 1. 암전 상태 유지 (n초)
            SetLightState(false);
            yield return new WaitForSeconds(blackoutDuration);

            // 2. 섬광 시작 (m초)
            SetLightState(true);
            yield return new WaitForSeconds(flashDuration);

            // 3. 다시 암전으로 돌아감
        }
    }
}