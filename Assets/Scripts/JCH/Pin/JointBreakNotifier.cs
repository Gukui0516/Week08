using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Joint 연결 파괴를 감지하고 통지하는 컴포넌트
/// </summary>
public class JointBreakNotifier : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = true;

    [TabGroup("Settings")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("파괴 비율 임계값 (0~1). 이 비율을 초과하면 남은 모든 Joint 파괴")]
    private float _autoBreakThresholdRatio = 0.8f;
    #endregion

    #region Fields
    [TabGroup("Debug")]
    [ShowInInspector,ReadOnly]
    private List<Joint> _jointList;

    private int _initialJointCount;
    private int _lastValidJointCount;
    private bool _isDirty;
    #endregion

    #region Event
    public event System.Action<int, int> OnJointBreakEvent;
    public event System.Action OnAllJointsDestroyedEvent;
    #endregion

    #region Properties
    /// <summary>디버그 로그 출력 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;

    /// <summary>초기 Joint 개수</summary>
    public int InitialJointCount => _initialJointCount;

    /// <summary>현재 유효한 Joint 개수</summary>
    public int ValidJointCount => _lastValidJointCount;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Update()
    {
        if (_isDirty)
        {
            CheckValidJointCount();
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Log($"OnJointBreak 호출 - 파괴력: {breakForce:F2}");
        _isDirty = true;
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _jointList = new List<Joint>();
        _isDirty = false;

        // 자신에게 붙은 모든 Joint 컴포넌트 수집
        GetComponents(_jointList);
        _initialJointCount = _jointList.Count;
        _lastValidJointCount = _initialJointCount;

        Log($"초기화 완료 - 총 Joint 개수: {_initialJointCount}");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_jointList != null)
        {
            _jointList.Clear();
            _jointList = null;
        }

        Log("정리 완료");
    }
    #endregion

    #region Protected Methods - Notification
    /// <summary>Joint 파괴 통지</summary>
    /// <param name="nowCount">현재 남은 Joint 개수</param>
    /// <param name="maxCount">최대 Joint 개수</param>
    protected virtual void NotifyJointBreak(int nowCount, int maxCount)
    {
        Log($"NotifyJointBreak 호출 - 남은 개수: {nowCount}, 최대 개수: {maxCount}");
        OnJointBreakEvent?.Invoke(nowCount, maxCount);
    }
    #endregion

    #region Private Methods - Joint Validation
    /// <summary>유효한 Joint 개수 확인 및 이벤트 발행</summary>
    private void CheckValidJointCount()
    {
        _isDirty = false;

        int currentValidCount = 0;
        for (int i = 0; i < _jointList.Count; i++)
        {
            if (_jointList[i] != null)
                currentValidCount++;
        }

        // 실제로 개수가 변경된 경우만 이벤트 발행
        if (currentValidCount != _lastValidJointCount)
        {
            Log($"Joint 개수 변경 감지 - 이전: {_lastValidJointCount}, 현재: {currentValidCount}");

            _lastValidJointCount = currentValidCount;
            NotifyJointBreak(currentValidCount, _initialJointCount);

            // 파괴 비율 체크
            CheckAutoBreakThreshold(currentValidCount);
        }
    }

    /// <summary>파괴 비율 임계값 체크 및 전체 파괴</summary>
    /// <param name="currentValidCount">현재 유효한 Joint 개수</param>
    private void CheckAutoBreakThreshold(int currentValidCount)
    {
        if (_initialJointCount <= 0) return;

        float destroyedRatio = (float)(_initialJointCount - currentValidCount) / _initialJointCount;

        if (destroyedRatio >= _autoBreakThresholdRatio && currentValidCount > 0)
        {
            Log($"파괴 임계값 도달 - 비율: {destroyedRatio:P1} (임계값: {_autoBreakThresholdRatio:P1}), 남은 Joint 전체 파괴 시작", true);
            DestroyAllRemainingJoints();
        }
    }

    /// <summary>남은 모든 Joint 강제 파괴</summary>
    private void DestroyAllRemainingJoints()
    {
        int destroyedCount = 0;

        for (int i = 0; i < _jointList.Count; i++)
        {
            if (_jointList[i] != null)
            {
                Destroy(_jointList[i]);
                destroyedCount++;
            }
        }

        _lastValidJointCount = 0;

        Log($"전체 Joint 파괴 완료 - 파괴된 개수: {destroyedCount}", true);
        // 일관성 있게 OnJointBreakEvent 호출
        NotifyJointBreak(0, _initialJointCount);
        OnAllJointsDestroyedEvent?.Invoke();
    }
    #endregion

    #region Private Methods - Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.Log($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.LogWarning($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}