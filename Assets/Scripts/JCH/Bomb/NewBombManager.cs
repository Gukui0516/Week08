using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// IExplodable 객체들의 등록 및 상태 조회를 담당하는 싱글톤 매니저입니다.
/// 폭발 실행은 하지 않으며, 데이터 제공만 수행합니다.
/// </summary>
public class NewBombManager : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private static NewBombManager _instance;
    private static bool _isQuitting = false;

    private List<IExplodable> _registeredExplodables;
    private HashSet<IExplodable> _explodedSet;
    #endregion

    #region Properties
    public static NewBombManager Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NewBombManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("NewBombManager");
                    _instance = obj.AddComponent<NewBombManager>();
                }
            }
            return _instance;
        }
    }

    /// <summary>등록된 모든 IExplodable 객체 (읽기 전용)</summary>
    public IReadOnlyList<IExplodable> RegisteredExplodables => _registeredExplodables;

    /// <summary>디버그 로깅 활성화 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;
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

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        if (_instance == null)
        {
            _instance = this;
            _isQuitting = false;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _registeredExplodables = new List<IExplodable>();
        _explodedSet = new HashSet<IExplodable>();

        Log("초기화 완료: 폭발 객체 관리 시스템 준비됨");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_instance == this)
        {
            _isQuitting = true;
        }

        _registeredExplodables?.Clear();
        _explodedSet?.Clear();

        Log("Cleanup: 폭발 객체 관리 시스템 정리 완료");
    }
    #endregion

    #region Public Methods - Registration
    /// <summary>
    /// IExplodable 객체를 등록합니다.
    /// </summary>
    /// <param name="explodable">등록할 IExplodable 객체</param>
    public void RegisterExplodable(IExplodable explodable)
    {
        if (explodable == null)
        {
            LogWarning("null 객체를 등록하려고 시도했습니다.");
            return;
        }

        if (_registeredExplodables.Contains(explodable))
        {
            MonoBehaviour explodableMono = explodable as MonoBehaviour;
            string objectName = explodableMono != null ? explodableMono.name : "Unknown";
            LogWarning($"{objectName}은(는) 이미 등록되어 있습니다.");
            return;
        }

        _registeredExplodables.Add(explodable);

        MonoBehaviour mono = explodable as MonoBehaviour;
        string name = mono != null ? mono.name : "Unknown";
        Log($"IExplodable 등록: {name}");
    }

    /// <summary>
    /// IExplodable 객체를 등록 해제합니다.
    /// </summary>
    /// <param name="explodable">해제할 IExplodable 객체</param>
    public void UnregisterExplodable(IExplodable explodable)
    {
        if (explodable == null)
        {
            LogWarning("null 객체를 등록 해제하려고 시도했습니다.");
            return;
        }

        if (!_registeredExplodables.Contains(explodable))
        {
            MonoBehaviour explodableMono = explodable as MonoBehaviour;
            string objectName = explodableMono != null ? explodableMono.name : "Unknown";
            LogWarning($"{objectName}은(는) 등록되어 있지 않습니다.");
            return;
        }

        _registeredExplodables.Remove(explodable);
        _explodedSet.Remove(explodable);

        MonoBehaviour mono = explodable as MonoBehaviour;
        string name = mono != null ? mono.name : "Unknown";
        Log($"IExplodable 등록 해제: {name}");
    }
    #endregion

    #region Public Methods - Query
    /// <summary>
    /// 현재 활성화된 IExplodable 객체의 개수를 반환합니다.
    /// </summary>
    /// <returns>활성 객체 개수</returns>
    public int GetActiveCount()
    {
        if (_registeredExplodables == null || _registeredExplodables.Count == 0)
            return 0;

        int activeCount = 0;
        foreach (var explodable in _registeredExplodables)
        {
            if (explodable == null) continue;

            MonoBehaviour explodableMono = explodable as MonoBehaviour;
            if (explodableMono != null && explodableMono.gameObject.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    /// <summary>
    /// 특정 IExplodable 객체가 이미 폭발했는지 확인합니다.
    /// </summary>
    /// <param name="explodable">확인할 IExplodable 객체</param>
    /// <returns>폭발 여부</returns>
    public bool HasExploded(IExplodable explodable)
    {
        if (explodable == null)
            return false;

        return _explodedSet.Contains(explodable);
    }
    #endregion

    #region Public Methods - Explosion Tracking
    /// <summary>
    /// 특정 IExplodable 객체를 폭발한 것으로 기록합니다.
    /// </summary>
    /// <param name="explodable">기록할 IExplodable 객체</param>
    public void MarkAsExploded(IExplodable explodable)
    {
        if (explodable == null)
        {
            LogWarning("null 객체를 폭발 기록하려고 시도했습니다.");
            return;
        }

        if (_explodedSet.Contains(explodable))
        {
            MonoBehaviour explodableMono = explodable as MonoBehaviour;
            string objectName = explodableMono != null ? explodableMono.name : "Unknown";
            Log($"{objectName}은(는) 이미 폭발 기록되어 있습니다.");
            return;
        }

        _explodedSet.Add(explodable);

        MonoBehaviour mono = explodable as MonoBehaviour;
        string name = mono != null ? mono.name : "Unknown";
        Log($"폭발 기록: {name}");
    }

    /// <summary>
    /// 모든 폭발 기록을 초기화합니다.
    /// </summary>
    public void ResetExplosionRecords()
    {
        _explodedSet.Clear();
        Log("모든 폭발 기록 초기화");
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=lime>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=lime>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=lime>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}