using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 사인파 기반으로 Transform을 로컬 좌표계에서 진동시키는 컨트롤러
/// </summary>
public class SineVibrationController : MonoBehaviour
{
    #region Serialized Fields

    [TabGroup("Vibration")]
    [SerializeField, LabelText("Vibrate X Axis")]
    private bool _vibrateXAxis = true;

    [TabGroup("Vibration")]
    [SerializeField, LabelText("Vibrate Y Axis")]
    private bool _vibrateYAxis = false;

    [TabGroup("Vibration")]
    [SerializeField, LabelText("Vibrate Z Axis")]
    private bool _vibrateZAxis = false;

    [TabGroup("Vibration")]
    [SerializeField, Tooltip("진동 거리 (음수 가능)")]
    private float _amplitudeDistance = 1f;

    [TabGroup("Vibration")]
    [SerializeField, Tooltip("진동 주파수 (Hz)"), Min(0.01f)]
    private float _frequencyHz = 1f;

    [TabGroup("Vibration")]
    [SerializeField, Tooltip("자동으로 무한 반복")]
    private bool _isLooping = true;

    [TabGroup("Debug")]
    [SerializeField]
    private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Vector3 _initialLocalPosition;
    private float _currentPhaseRadians;
    private bool _isVibrating;
    private bool _isOneShotMode;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsVibrating => _isVibrating;
    [TabGroup("Debug")]
    [ShowInInspector,ReadOnly]
    public float CurrentPhase => _currentPhaseRadians;
    #endregion

    #region Odin Debug Buttons
    [Button("1회 진동 트리거", ButtonSizes.Medium)]
    [PropertyOrder(100)]
    private void DebugTriggerOneShot()
    {
        if (!Application.isPlaying)
        {
            LogWarning("플레이 모드에서만 실행 가능합니다.", true);
            return;
        }

        TriggerOneShotVibration();
    }

    [Button("연속 진동 토글", ButtonSizes.Medium)]
    [PropertyOrder(101)]
    private void DebugToggleContinuous()
    {
        if (!Application.isPlaying)
        {
            LogWarning("플레이 모드에서만 실행 가능합니다.", true);
            return;
        }

        if (_isVibrating && !_isOneShotMode)
        {
            StopVibration();
        }
        else
        {
            StartVibration();
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LateInitialize();
    }

    private void Update()
    {
        if (_isVibrating)
        {
            UpdateVibration();
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnDrawGizmos()
    {
        DrawVibrationGizmos();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _currentPhaseRadians = 0f;
        _isVibrating = false;
        _isOneShotMode = false;
        _initialLocalPosition = transform.localPosition;

        Log("초기화 완료: 초기 로컬 위치 저장");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        Log("LateInitialize 완료");

        if (_isLooping)
        {
            StartVibration();
        }
    }

    /// <summary>소멸 시 초기 위치 복원</summary>
    public void Cleanup()
    {
        RestoreInitialPosition();
        Log("Cleanup: 초기 위치 복원 완료");
    }
    #endregion

    #region Public Methods - Vibration Control
    /// <summary>연속 진동 시작</summary>
    public void StartVibration()
    {
        _currentPhaseRadians = 0f;
        _isVibrating = true;
        _isOneShotMode = false;

        Log("연속 진동 시작");
    }

    /// <summary>진동 즉시 정지</summary>
    public void StopVibration()
    {
        _isVibrating = false;
        _isOneShotMode = false;

        Log("진동 정지");
    }

    /// <summary>1회 진동 트리거 (0 ~ 2π)</summary>
    public void TriggerOneShotVibration()
    {
        _currentPhaseRadians = 0f;
        _isVibrating = true;
        _isOneShotMode = true;

        Log("1회 진동 트리거");
    }
    #endregion

    #region Private Methods - Vibration Logic
    /// <summary>진동 업데이트 로직</summary>
    private void UpdateVibration()
    {
        float phaseIncrement = 2f * Mathf.PI * _frequencyHz * Time.deltaTime;
        _currentPhaseRadians += phaseIncrement;

        if (_isOneShotMode && _currentPhaseRadians >= 2f * Mathf.PI)
        {
            _currentPhaseRadians = 2f * Mathf.PI;
            transform.localPosition = CalculateSinePosition();
            StopVibration();
            RestoreInitialPosition();
            Log("1회 진동 완료");
            return;
        }

        if (_isLooping && _currentPhaseRadians >= 2f * Mathf.PI)
        {
            _currentPhaseRadians -= 2f * Mathf.PI;
        }

        transform.localPosition = CalculateSinePosition();
    }

    /// <summary>사인파 계산하여 새 로컬 위치 반환</summary>
    /// <returns>계산된 로컬 위치</returns>
    private Vector3 CalculateSinePosition()
    {
        float sineValue = Mathf.Sin(_currentPhaseRadians);
        float worldDisplacement = sineValue * _amplitudeDistance;

        Vector3 localOffset = Vector3.zero;
        Vector3 lossyScale = transform.lossyScale;

        if (_vibrateXAxis)
            localOffset.x += worldDisplacement / lossyScale.x;

        if (_vibrateYAxis)
            localOffset.y += worldDisplacement / lossyScale.y;

        if (_vibrateZAxis)
            localOffset.z += worldDisplacement / lossyScale.z;

        return _initialLocalPosition + localOffset;
    }


    /// <summary>타겟을 초기 로컬 위치로 복원</summary>
    private void RestoreInitialPosition()
    {
        transform.localPosition = _initialLocalPosition;
    }
    #endregion

    #region Private Methods - Gizmos
    /// <summary>진동 궤적 기즈모 그리기</summary>
    private void DrawVibrationGizmos()
    {
        // 진동 원점 계산 (로컬 → 월드 변환)
        Vector3 localOrigin = Application.isPlaying ? _initialLocalPosition : transform.localPosition;
        Vector3 worldOrigin = transform.parent != null
            ? transform.parent.TransformPoint(localOrigin)
            : localOrigin;

        Gizmos.color = Color.yellow;

        // X축 진동 궤적
        if (_vibrateXAxis)
        {
            Vector3 positiveEnd = worldOrigin + transform.right * _amplitudeDistance;
            Vector3 negativeEnd = worldOrigin - transform.right * _amplitudeDistance;

            Gizmos.DrawLine(negativeEnd, positiveEnd);
            Gizmos.DrawWireSphere(positiveEnd, 0.05f);
            Gizmos.DrawWireSphere(negativeEnd, 0.05f);
        }

        // Y축 진동 궤적
        if (_vibrateYAxis)
        {
            Vector3 positiveEnd = worldOrigin + transform.up * _amplitudeDistance;
            Vector3 negativeEnd = worldOrigin - transform.up * _amplitudeDistance;

            Gizmos.DrawLine(negativeEnd, positiveEnd);
            Gizmos.DrawWireSphere(positiveEnd, 0.05f);
            Gizmos.DrawWireSphere(negativeEnd, 0.05f);
        }

        // Z축 진동 궤적
        if (_vibrateZAxis)
        {
            Vector3 positiveEnd = worldOrigin + transform.forward * _amplitudeDistance;
            Vector3 negativeEnd = worldOrigin - transform.forward * _amplitudeDistance;

            Gizmos.DrawLine(negativeEnd, positiveEnd);
            Gizmos.DrawWireSphere(positiveEnd, 0.05f);
            Gizmos.DrawWireSphere(negativeEnd, 0.05f);
        }

        // 진동 원점 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(worldOrigin, 0.08f);
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }
    #endregion

}