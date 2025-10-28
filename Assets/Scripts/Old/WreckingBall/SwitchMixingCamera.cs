using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

/// <summary>
/// Cinemachine Mixing Camera의 Weight 값을 제어하여 폭탄 폭발 시 카메라를 순차적으로 전환합니다.
/// Mixing Camera GameObject에 직접 추가하여 사용하세요.
/// Cinemachine 3.1.x 버전용입니다.
/// </summary>
public class SwitchMixingCamera : MonoBehaviour
{
    [Header("Mixing Camera Settings")]
    [Tooltip("제어할 Cinemachine Mixing Camera입니다. 비어있으면 자동으로 같은 GameObject에서 찾습니다.")]
    [SerializeField] private CinemachineMixingCamera mixingCamera;

    [Header("Transition Settings")]
    [Tooltip("Weight 전환에 걸리는 시간(초)입니다.")]
    [SerializeField] private float transitionDuration = 2.0f;
    [Tooltip("Weight 전환 애니메이션 커브입니다.")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private OrbitCamera[] orbitCamera;

    private int currentCameraIndex = 0;
    private Coroutine currentTransition;

    private void OnEnable()
    {
        // BombCollisionDetector의 폭탄 충돌 이벤트 구독
        BombCollisionDetector.OnBombCollisionDetected += OnBombExploded;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        BombCollisionDetector.OnBombCollisionDetected -= OnBombExploded;
    }

    private void Start()
    {
        // Mixing Camera 자동 참조 (비어있을 경우)
        if (mixingCamera == null)
        {
            mixingCamera = GetComponent<CinemachineMixingCamera>();
        }

        // Mixing Camera 검증
        if (mixingCamera == null)
        {
            Debug.LogError("[SwitchMixingCamera] Mixing Camera를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        for (int i = 1; i < orbitCamera.Length; i++)
        {
            orbitCamera[i].enabled = false;
        }

        // 초기 Weight 설정
        InitializeCameraWeights();
    }

    /// <summary>
    /// 카메라 Weight를 초기 상태로 설정합니다.
    /// </summary>
    private void InitializeCameraWeights()
    {
        if (mixingCamera == null) return;

        // 모든 채널의 Weight을 0으로 설정
        for (int i = 0; i < mixingCamera.ChildCameras.Count; i++)
        {
            mixingCamera.SetWeight(i, 0f);
        }

        // 기본 카메라만 활성화
        if (currentCameraIndex < mixingCamera.ChildCameras.Count)
        {
            mixingCamera.SetWeight(currentCameraIndex, 1f);
        }
    }

    /// <summary>
    /// 폭탄 폭발 시 호출되는 이벤트 핸들러입니다.
    /// </summary>
    /// <param name="bomb">폭발한 폭탄 GameObject</param>
    private void OnBombExploded(GameObject bomb)
    {
        int nextCameraIndex = currentCameraIndex + 1;

        if (nextCameraIndex >= mixingCamera.ChildCameras.Count)
        {
            return;
        }

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(TransitionToNextCamera(nextCameraIndex));
    }

    /// <summary>
    /// 다음 카메라로 전환하는 코루틴입니다.
    /// </summary>
    /// <param name="targetIndex">전환할 타겟 카메라 인덱스</param>
    private IEnumerator TransitionToNextCamera(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= mixingCamera.ChildCameras.Count)
        {
            yield break;
        }

        int fromIndex = currentCameraIndex;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);
            float curveValue = transitionCurve.Evaluate(t);
            for (int i = 0; i < orbitCamera.Length; i++)
            {
                orbitCamera[i].enabled = (i == targetIndex);
            }
            // Weight 값 보간
            mixingCamera.SetWeight(fromIndex, Mathf.Lerp(1f, 0f, curveValue));
            mixingCamera.SetWeight(targetIndex, Mathf.Lerp(0f, 1f, curveValue));

            yield return null;
        }

        // 최종 값 확정
        mixingCamera.SetWeight(fromIndex, 0f);
        mixingCamera.SetWeight(targetIndex, 1f);

        currentCameraIndex = targetIndex;
        currentTransition = null;
    }

    /// <summary>
    /// 카메라를 초기 상태로 리셋합니다.
    /// </summary>
    public void ResetCamera()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }

        currentCameraIndex = 0;
        InitializeCameraWeights();
    }

#if UNITY_EDITOR
    [ContextMenu("다음 카메라로 전환")]
    private void TestNextCamera()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SwitchMixingCamera] Play Mode에서만 테스트할 수 있습니다.");
            return;
        }

        OnBombExploded(null);
    }

    [ContextMenu("카메라 리셋")]
    private void TestCameraReset()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SwitchMixingCamera] Play Mode에서만 리셋할 수 있습니다.");
            return;
        }

        ResetCamera();
    }

    private void OnValidate()
    {
        // Mixing Camera 자동 참조 시도
        if (mixingCamera == null)
        {
            mixingCamera = GetComponent<CinemachineMixingCamera>();
        }

        if (transitionDuration <= 0)
        {
            transitionDuration = 0.1f;
        }
    }
#endif
}