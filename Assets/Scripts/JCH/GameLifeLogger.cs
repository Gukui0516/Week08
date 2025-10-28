using UnityEngine;

/// <summary>
/// 게임 생명주기 로깅 및 이벤트 관리 (DontDestroy 싱글톤)
/// </summary>
public class GameLifeLogger : MonoBehaviour
{
    #region Singleton
    private static GameLifeLogger _instance;
    public static GameLifeLogger Instance => _instance;
    #endregion

    #region Serialized Fields
    [SerializeField] private bool _isDebugLogging = true;
    #endregion

    #region Private Fields
    private bool _isGameStartLogged;
    private bool _isGameClearLogged;
    #endregion

    #region Properties
    public bool IsGameStarted => _isGameStartLogged;
    public bool IsGameCleared => _isGameClearLogged;
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

    private void OnApplicationQuit()
    {
        if (_isGameStartLogged)
        {
            LogSystem.PushLog(LogLevel.INFO, "GameQuit", 1, true);
        }
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
        // 싱글톤 중복 체크
        if (_instance != null && _instance != this)
        {
            LogWarning("Duplicate GameLifeLogger detected. Destroying this instance.", true);
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _isGameStartLogged = false;
        _isGameClearLogged = false;

        Log("GameLifeLogger initialized.");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        if (!_isGameStartLogged)
        {
            LogSystem.PushLog(LogLevel.INFO, "GameStart", 1);
            _isGameStartLogged = true;
            Log("Game started. Log pushed to LogSystem.");
        }
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Private Methods - Logging
    /// <summary>
    /// 일반 로그 출력
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="forcely">강제 출력 여부</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=#00CED1>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>
    /// 경고 로그 출력
    /// </summary>
    /// <param name="message">경고 메시지</param>
    /// <param name="forcely">강제 출력 여부</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=#00CED1>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>
    /// 에러 로그 출력 - 항상 강제 출력
    /// </summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=#00CED1>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}