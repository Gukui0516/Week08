using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 사인파 기반으로 Transform을 로컬 좌표계에서 진동시키는 컨트롤러
/// </summary>
public class SineVibrationImpulseApplicator : MonoBehaviour
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

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("Impulse 활성화")]
    private bool _enableCast = true;

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("Impulse 대상 레이어")]
    private LayerMask _castLayerMask = ~0;

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("임펄스 크기")]
    private float _impulseMagnitude = 10f;

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("힘 적용 모드")]
    private ForceMode _forceMode = ForceMode.Impulse;

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("Impulse 기즈모 표시 지속 시간 (초)")]
    private float _castGizmoDuration = 0.5f;

    [TabGroup("Debug")]
    [SerializeField]
    private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Vector3 _initialLocalPosition;
    private float _currentPhaseRadians;
    private bool _isVibrating;
    private bool _isOneShotMode;

    private Collider _colliderForCast;
    private Vector3 _lastCastHitWorldPoint;
    private Vector3 _lastCastHitNormal;
    private float _lastCastHitTime;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsVibrating => _isVibrating;
    [TabGroup("Debug")]
    [ShowInInspector,ReadOnly]
    public float CurrentPhase => _currentPhaseRadians;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasRecentCastHit => (Time.time - _lastCastHitTime) < _castGizmoDuration;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastCastHitWorldPoint => _lastCastHitWorldPoint;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastCastHitNormal => _lastCastHitNormal;
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

        // Collider 캐싱
        _colliderForCast = GetComponent<Collider>();
        if (_colliderForCast == null)
        {
            LogWarning("Cast를 위한 Collider 컴포넌트가 없습니다", true);
        }

        // Cast 히트 정보 초기화
        _lastCastHitWorldPoint = Vector3.zero;
        _lastCastHitNormal = Vector3.zero;
        _lastCastHitTime = -_castGizmoDuration;

        Log("초기화 완료: 초기 로컬 위치 저장 및 Collider 캐싱");
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

        // 다음 위치 계산
        Vector3 nextLocalPosition = CalculateSinePosition();

        // Cast 수행
        if (_enableCast && _colliderForCast != null)
        {
            PerformCastAndApplyImpulse(transform.localPosition, nextLocalPosition);
        }

        // One-Shot 모드 처리
        if (_isOneShotMode && _currentPhaseRadians >= 2f * Mathf.PI)
        {
            _currentPhaseRadians = 2f * Mathf.PI;
            transform.localPosition = nextLocalPosition;
            StopVibration();
            RestoreInitialPosition();
            Log("1회 진동 완료");
            return;
        }

        // 루핑 처리
        if (_isLooping && _currentPhaseRadians >= 2f * Mathf.PI)
        {
            _currentPhaseRadians -= 2f * Mathf.PI;
        }

        // 위치 업데이트
        transform.localPosition = nextLocalPosition;
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

    #region Private Methods - Cast and Impulse
    /// <summary>현재 위치에서 다음 위치로 Cast 수행 및 임펄스 적용</summary>
    private void PerformCastAndApplyImpulse(Vector3 currentLocalPosition, Vector3 nextLocalPosition)
    {
        Vector3 currentWorldPosition = transform.parent != null
            ? transform.parent.TransformPoint(currentLocalPosition)
            : currentLocalPosition;

        Vector3 nextWorldPosition = transform.parent != null
            ? transform.parent.TransformPoint(nextLocalPosition)
            : nextLocalPosition;

        Vector3 castDirection = nextWorldPosition - currentWorldPosition;
        float castDistance = castDirection.magnitude;

        if (castDistance < 0.0001f)
            return;

        castDirection.Normalize();

        RaycastHit hitInfo;
        if (PerformColliderCast(currentWorldPosition, castDirection, castDistance, out hitInfo))
        {
            _lastCastHitWorldPoint = hitInfo.point;
            _lastCastHitNormal = hitInfo.normal;
            _lastCastHitTime = Time.time;

            ApplyImpulseToHitRigidbody(hitInfo);
        }
    }

    /// <summary>Collider 타입에 따른 Cast 수행</summary>
    private bool PerformColliderCast(Vector3 worldPosition, Vector3 direction, float distance, out RaycastHit hitInfo)
    {
        BoxCollider boxCollider = _colliderForCast as BoxCollider;
        if (boxCollider != null)
        {
            Vector3 center = worldPosition + transform.TransformVector(boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, transform.lossyScale);
            return Physics.BoxCast(center, halfExtents, direction, out hitInfo, transform.rotation, distance, _castLayerMask, QueryTriggerInteraction.Ignore);
        }

        SphereCollider sphereCollider = _colliderForCast as SphereCollider;
        if (sphereCollider != null)
        {
            Vector3 center = worldPosition + transform.TransformVector(sphereCollider.center);
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            return Physics.SphereCast(center, radius, direction, out hitInfo, distance, _castLayerMask, QueryTriggerInteraction.Ignore);
        }

        CapsuleCollider capsuleCollider = _colliderForCast as CapsuleCollider;
        if (capsuleCollider != null)
        {
            Vector3 center = worldPosition + transform.TransformVector(capsuleCollider.center);
            float radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float height = capsuleCollider.height * transform.lossyScale.y;

            Vector3 point1 = center + Vector3.up * (height * 0.5f - radius);
            Vector3 point2 = center - Vector3.up * (height * 0.5f - radius);

            return Physics.CapsuleCast(point1, point2, radius, direction, out hitInfo, distance, _castLayerMask, QueryTriggerInteraction.Ignore);
        }

        hitInfo = default(RaycastHit);
        return false;
    }

    /// <summary>히트한 Rigidbody에 임펄스 적용</summary>
    private void ApplyImpulseToHitRigidbody(RaycastHit hitInfo)
    {
        Rigidbody targetRigidbody = hitInfo.rigidbody;
        if (targetRigidbody == null)
        {
            Log("히트한 오브젝트에 Rigidbody 없음 - 임펄스 적용 불가");
            return;
        }

        Vector3 impulseDirection = -hitInfo.normal;
        Vector3 force = impulseDirection * _impulseMagnitude;
        targetRigidbody.AddForce(force, _forceMode);

        Log($"Cast 히트 임펄스 적용: {targetRigidbody.gameObject.name} | 크기: {_impulseMagnitude} | 방향: {impulseDirection}");
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

        // Cast 히트 정보 시각화
        if (HasRecentCastHit)
        {
            // 히트 지점 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_lastCastHitWorldPoint, 0.1f);

            // 노말 방향 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_lastCastHitWorldPoint, _lastCastHitWorldPoint + _lastCastHitNormal * 0.5f);

            // 임펄스 방향 표시 (노말 반대)
            Gizmos.color = Color.magenta;
            Vector3 impulseDirection = -_lastCastHitNormal;
            DrawArrowGizmo(_lastCastHitWorldPoint, impulseDirection, 0.5f);
        }
    }

    /// <summary>화살표 기즈모 그리기 헬퍼</summary>
    private void DrawArrowGizmo(Vector3 startWorldPosition, Vector3 directionWorld, float length)
    {
        Vector3 endWorldPosition = startWorldPosition + directionWorld * length;

        // 화살표 몸통
        Gizmos.DrawLine(startWorldPosition, endWorldPosition);

        // 화살촉
        Vector3 right = Vector3.Cross(directionWorld, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.Cross(directionWorld, Vector3.forward).normalized;

        float arrowSize = length * 0.2f;
        Vector3 arrowTip1 = endWorldPosition - directionWorld * arrowSize + right * arrowSize * 0.5f;
        Vector3 arrowTip2 = endWorldPosition - directionWorld * arrowSize - right * arrowSize * 0.5f;

        Gizmos.DrawLine(endWorldPosition, arrowTip1);
        Gizmos.DrawLine(endWorldPosition, arrowTip2);
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