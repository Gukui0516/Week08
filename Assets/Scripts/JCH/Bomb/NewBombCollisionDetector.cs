using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// IExplodable 인터페이스를 가진 객체의 충돌을 감지하고 폭발을 트리거합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NewBombCollisionDetector : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Collision")]
    [Tooltip("true: OnTriggerEnter 사용, false: OnCollisionEnter 사용")]
    [SerializeField] private bool _useTriggerMode = true;

    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Collider _triggerCollider;
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

    private void OnTriggerEnter(Collider other)
    {
        if (!_useTriggerMode) return;
        HandleCollision(other.gameObject, other.ClosestPoint(transform.position));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_useTriggerMode) return;
        Vector3 contactWorldPosition = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : collision.transform.position;
        HandleCollision(collision.gameObject, contactWorldPosition);
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _triggerCollider = GetComponent<Collider>();

        if (_triggerCollider == null)
        {
            LogError($"{gameObject.name}에 Collider가 없습니다!");
        }

        Log("초기화 완료: 충돌 감지 시스템 준비됨");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        Log("Cleanup: 충돌 감지 시스템 정리 완료");
    }
    #endregion

    #region Private Methods - Trigger Handling
    /// <summary>IExplodable 트리거 처리</summary>
    /// <param name="explodable">감지된 IExplodable 객체</param>
    /// <param name="contactWorldPosition">충돌 지점 월드 좌표</param>
    private void HandleExplodableTrigger(IExplodable explodable, Vector3 contactWorldPosition)
    {
        if (explodable == null)
        {
            LogWarning("감지된 explodable이 null입니다.");
            return;
        }

        MonoBehaviour explodableMono = explodable as MonoBehaviour;
        if (explodableMono == null)
        {
            LogError("IExplodable이 MonoBehaviour가 아닙니다!");
            return;
        }

        Log($"{gameObject.name}이(가) {explodableMono.name}을(를) 감지! 폭발 트리거.");

        
        NewBombExplodeSystem.Instance.Execute(explodable);
    }
    #endregion

    #region Private Methods - Collision Handling
    /// <summary>충돌/트리거 공통 처리</summary>
    /// <param name="gameObject">충돌한 GameObject</param>
    /// <param name="contactWorldPosition">접촉 지점 월드 좌표</param>
    private void HandleCollision(GameObject gameObject, Vector3 contactWorldPosition)
    {
        IExplodable explodable = gameObject.GetComponent<IExplodable>();
        if (explodable != null)
        {
            HandleExplodableTrigger(explodable, contactWorldPosition);
        }
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }
    #endregion

#if UNITY_EDITOR
    #region Editor Validation
    private void OnValidate()
    {
        // Collider 존재 여부 확인
        Collider col = gameObject.GetComponent<Collider>();
        if (col == null)
        {
            LogWarning("Collider 컴포넌트가 필요합니다!", true);
            return;
        }

        // Trigger 모드 검증
        if (_useTriggerMode && !col.isTrigger)
        {
            LogWarning("Trigger 모드 사용 시 Collider의 'Is Trigger'를 활성화해야 합니다!", true);
        }
        else if (!_useTriggerMode && col.isTrigger)
        {
            LogWarning("Collision 모드 사용 시 Collider의 'Is Trigger'를 비활성화해야 합니다!", true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = _useTriggerMode
                ? new Color(1f, 0.5f, 0f, 0.5f)  // 주황색 (Trigger)
                : new Color(0f, 1f, 0.5f, 0.5f); // 청록색 (Collision)

            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                Gizmos.DrawWireSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, col.bounds.extents.magnitude);
            }
        }
    }
    #endregion
#endif
}