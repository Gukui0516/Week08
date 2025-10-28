using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 충돌 시 Contact Normal 방향으로 직렬화된 임펄스를 상대 Rigidbody에 전달하는 컴포넌트
/// </summary>
public class CollisionImpulseApplicator : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Impulse")]
    [SerializeField, Tooltip("임펄스 크기")]
    private float _impulseMagnitude = 10f;

    [TabGroup("Impulse")]
    [SerializeField, Tooltip("힘 적용 모드")]
    private ForceMode _forceMode = ForceMode.Impulse;

    [TabGroup("Gizmo")]
    [SerializeField, Tooltip("기즈모 표시 지속 시간 (초)")]
    private float _gizmoDuration = 0.5f;

    [TabGroup("Gizmo")]
    [SerializeField, Tooltip("기즈모 화살표 길이 배율")]
    private float _gizmoArrowLength = 1f;

    [TabGroup("Debug")]
    [SerializeField]
    private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Vector3 _lastCollisionWorldPoint;
    private Vector3 _lastImpulseWorldDirection;
    private float _lastCollisionTime;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastCollisionWorldPoint => _lastCollisionWorldPoint;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastImpulseWorldDirection => _lastImpulseWorldDirection;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasRecentCollision => (Time.time - _lastCollisionTime) < _gizmoDuration;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0)
        {
            LogWarning("충돌 Contact 정보 없음");
            return;
        }

        Rigidbody targetRigidbody = collision.rigidbody;
        if (targetRigidbody == null)
        {
            Log("상대 오브젝트에 Rigidbody 없음 - 임펄스 적용 불가");
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 contactNormal = contact.normal;

        _lastCollisionWorldPoint = contact.point;
        _lastImpulseWorldDirection = -contactNormal;
        _lastCollisionTime = Time.time;

        ApplyImpulseToRigidbody(targetRigidbody, contactNormal);

        Log($"충돌 감지: {collision.gameObject.name} | 임펄스 방향: {contactNormal}");
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnDrawGizmos()
    {
        DrawCollisionGizmos();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _lastCollisionWorldPoint = Vector3.zero;
        _lastImpulseWorldDirection = Vector3.zero;
        _lastCollisionTime = -_gizmoDuration;

        Log("초기화 완료: 충돌 정보 초기화");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        Log("Cleanup 완료");
    }
    #endregion

    #region Private Methods - Collision Logic
    /// <summary>상대 Rigidbody에 임펄스 적용</summary>
    /// <param name="targetRigidbody">대상 Rigidbody</param>
    /// <param name="impulseWorldDirection">월드 좌표계 임펄스 방향</param>
    private void ApplyImpulseToRigidbody(Rigidbody targetRigidbody, Vector3 impulseWorldDirection)
    {
        Vector3 force = impulseWorldDirection * _impulseMagnitude;
        targetRigidbody.AddForce(force, _forceMode);

        Log($"임펄스 적용: {targetRigidbody.gameObject.name} | 크기: {_impulseMagnitude} | 모드: {_forceMode}");
    }
    #endregion

    #region Private Methods - Gizmos
    /// <summary>충돌 지점과 반발력 화살표 기즈모 그리기</summary>
    private void DrawCollisionGizmos()
    {
        if (!HasRecentCollision)
            return;

        // 충돌 지점 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_lastCollisionWorldPoint, 0.1f);

        // 반발력 화살표 표시
        Gizmos.color = Color.yellow;
        DrawArrowGizmo(_lastCollisionWorldPoint, _lastImpulseWorldDirection, _gizmoArrowLength);
    }

    /// <summary>화살표 기즈모 그리기 헬퍼</summary>
    /// <param name="startWorldPosition">화살표 시작 월드 위치</param>
    /// <param name="directionWorld">화살표 방향 (월드)</param>
    /// <param name="length">화살표 길이</param>
    private void DrawArrowGizmo(Vector3 startWorldPosition, Vector3 directionWorld, float length)
    {
        Vector3 endWorldPosition = startWorldPosition + directionWorld * length;

        // 화살표 몸통
        Gizmos.DrawLine(startWorldPosition, endWorldPosition);

        // 화살촉 (양옆 두 선)
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
            Debug.Log($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}