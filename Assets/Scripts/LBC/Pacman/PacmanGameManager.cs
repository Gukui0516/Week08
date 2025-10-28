using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// 팩맨 게임의 전체 흐름을 관리합니다.
/// 점수, 코인 수집 상태, 파워 모드, 게임오버, 클리어 조건 등을 관리합니다.
/// </summary>
public class PacmanGameManager : SingletonObject<PacmanGameManager>
{
    [Header("게임 상태")]
    [Tooltip("현재 점수")]
    [SerializeField] private int currentScore = 0;

    [Tooltip("수집한 코인 개수")]
    [SerializeField] private int coinsCollected = 0;

    [Tooltip("맵에 있는 전체 코인 개수 (자동 감지 또는 수동 설정)")]
    [SerializeField] private int totalCoins = 0;

    [Tooltip("게임이 시작되었는지 여부")]
    [SerializeField] private bool isGameStarted = false;

    [Tooltip("게임이 끝났는지 여부")]
    [SerializeField] private bool isGameOver = false;

    [Header("파워 모드 설정")]
    [Tooltip("파워 모드 지속 시간 (초)")]
    [SerializeField] private float powerModeDuration = 10f;

    [Tooltip("파워 모드가 활성화되어 있는지 여부")]
    [SerializeField] private bool isPowerModeActive = false;

    [Tooltip("파워 모드 남은 시간")]
    [SerializeField] private float powerModeTimer = 0f;

    [Header("고스트 설정")]
    [Tooltip("파워 모드 시 고스트를 먹었을 때 얻는 점수")]
    [SerializeField] private int ghostScoreValue = 200;

    [Tooltip("고스트를 연속으로 먹을수록 점수 배율 증가 여부")]
    [SerializeField] private bool useGhostScoreMultiplier = true;

    [Tooltip("현재 파워 모드에서 먹은 고스트 수 (점수 배율 계산용)")]
    private int ghostsEatenInPowerMode = 0;

    [Header("게임오버 설정")]
    [Tooltip("게임오버 시 자동으로 씬을 재시작할지 여부")]
    [SerializeField] private bool autoRestartOnGameOver = false;

    [Tooltip("자동 재시작 대기 시간 (초)")]
    [SerializeField] private float restartDelay = 3f;

    [Header("코인 자동 감지 설정")]
    [Tooltip("시작 시 씬에 있는 모든 코인을 자동으로 카운트할지 여부")]
    [SerializeField] private bool autoCountCoins = true;

    [Header("이벤트")]
    [Tooltip("점수가 변경될 때 호출됩니다 (UI 업데이트용)")]
    public UnityEvent<int> OnScoreChanged;

    [Tooltip("파워 모드가 시작될 때 호출됩니다")]
    public UnityEvent OnPowerModeStarted;

    [Tooltip("파워 모드가 종료될 때 호출됩니다")]
    public UnityEvent OnPowerModeEnded;

    [Tooltip("게임 클리어 시 호출됩니다")]
    public UnityEvent OnGameCleared;

    [Tooltip("게임오버 시 호출됩니다")]
    public UnityEvent OnGameOverEvent;

    protected override void Awake()
    {
        base.Awake();

        // 코인 자동 카운트
        if (autoCountCoins)
        {
            CountTotalCoins();
        }
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        // 파워 모드 타이머 업데이트
        if (isPowerModeActive)
        {
            powerModeTimer -= Time.deltaTime;

            if (powerModeTimer <= 0f)
            {
                EndPowerMode();
            }
        }
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    public void StartGame()
    {
        isGameStarted = true;
        isGameOver = false;
        currentScore = 0;
        coinsCollected = 0;
        isPowerModeActive = false;
        powerModeTimer = 0f;
        ghostsEatenInPowerMode = 0;

        OnScoreChanged?.Invoke(currentScore);

        Debug.Log("팩맨 게임 시작!");
    }

    /// <summary>
    /// 씬에 있는 모든 코인을 카운트합니다.
    /// </summary>
    private void CountTotalCoins()
    {
        Coin[] coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
        totalCoins = coins.Length;

        Debug.Log($"전체 코인 개수: {totalCoins}");
    }

    /// <summary>
    /// 점수를 추가합니다.
    /// </summary>
    /// <param name="score">추가할 점수</param>
    public void AddScore(int score)
    {
        if (isGameOver)
            return;

        currentScore += score;
        OnScoreChanged?.Invoke(currentScore);

        Debug.Log($"점수 추가: +{score} (현재 점수: {currentScore})");
    }

    /// <summary>
    /// 코인이 수집되었을 때 호출됩니다.
    /// 모든 코인을 수집하면 게임 클리어 처리합니다.
    /// </summary>
    public void OnCoinCollected()
    {
        if (isGameOver)
            return;

        coinsCollected++;

        Debug.Log($"코인 수집: {coinsCollected}/{totalCoins}");

        // 모든 코인을 수집하면 게임 클리어
        if (coinsCollected >= totalCoins)
        {
            GameClear();
        }
    }

    /// <summary>
    /// 파워 펠렛을 먹었을 때 호출됩니다.
    /// 파워 모드를 활성화합니다.
    /// </summary>
    public void ActivatePowerMode()
    {
        if (isGameOver)
            return;

        // 이미 파워 모드인 경우 타이머만 리셋
        if (isPowerModeActive)
        {
            powerModeTimer = powerModeDuration;
            Debug.Log("파워 모드 타이머 리셋!");
            return;
        }

        isPowerModeActive = true;
        powerModeTimer = powerModeDuration;
        ghostsEatenInPowerMode = 0;

        OnPowerModeStarted?.Invoke();

        Debug.Log($"파워 모드 시작! ({powerModeDuration}초)");
    }

    /// <summary>
    /// 파워 모드를 종료합니다.
    /// </summary>
    private void EndPowerMode()
    {
        if (!isPowerModeActive)
            return;

        isPowerModeActive = false;
        powerModeTimer = 0f;
        ghostsEatenInPowerMode = 0;

        OnPowerModeEnded?.Invoke();

        Debug.Log("파워 모드 종료!");
    }

    /// <summary>
    /// 파워 모드 중 고스트를 먹었을 때 호출됩니다.
    /// </summary>
    public void OnGhostEaten()
    {
        if (isGameOver || !isPowerModeActive)
            return;

        ghostsEatenInPowerMode++;

        // 점수 배율 계산 (고스트를 연속으로 먹을수록 점수 증가)
        int score = ghostScoreValue;

        if (useGhostScoreMultiplier)
        {
            // 200 -> 400 -> 800 -> 1600 형태로 증가
            score = ghostScoreValue * (int)Mathf.Pow(2, ghostsEatenInPowerMode - 1);
        }

        AddScore(score);

        Debug.Log($"고스트 처치! +{score}점 (연속 {ghostsEatenInPowerMode}마리)");
    }

    /// <summary>
    /// 팩맨이 고스트에게 잡혔을 때 호출됩니다.
    /// </summary>
    public void OnPacmanCaught()
    {
        if (isGameOver)
            return;

        GameOver();
    }

    /// <summary>
    /// 게임 클리어 처리를 합니다.
    /// </summary>
    private void GameClear()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        OnGameCleared?.Invoke();

        Debug.Log($"게임 클리어! 최종 점수: {currentScore}");

        // 추가 처리 (UI 표시, 다음 레벨 로드 등)
    }

    /// <summary>
    /// 게임오버 처리를 합니다.
    /// </summary>
    private void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        OnGameOverEvent?.Invoke();

        Debug.Log($"게임오버! 최종 점수: {currentScore}");

        // 자동 재시작
        if (autoRestartOnGameOver)
        {
            Invoke(nameof(RestartGame), restartDelay);
        }
    }

    /// <summary>
    /// 게임을 재시작합니다.
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===== Getter 메서드들 =====

    /// <summary>
    /// 현재 점수를 반환합니다.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// 파워 모드가 활성화되어 있는지 반환합니다.
    /// </summary>
    public bool IsPowerModeActive()
    {
        return isPowerModeActive;
    }

    /// <summary>
    /// 파워 모드 남은 시간을 반환합니다.
    /// </summary>
    public float GetPowerModeTimeRemaining()
    {
        return isPowerModeActive ? powerModeTimer : 0f;
    }

    /// <summary>
    /// 게임이 종료되었는지 반환합니다.
    /// </summary>
    public bool IsGameOver()
    {
        return isGameOver;
    }

    /// <summary>
    /// 수집한 코인 개수를 반환합니다.
    /// </summary>
    public int GetCoinsCollected()
    {
        return coinsCollected;
    }

    /// <summary>
    /// 전체 코인 개수를 반환합니다.
    /// </summary>
    public int GetTotalCoins()
    {
        return totalCoins;
    }

    /// <summary>
    /// 전체 코인 개수를 수동으로 설정합니다.
    /// (autoCountCoins를 사용하지 않을 때)
    /// </summary>
    public void SetTotalCoins(int count)
    {
        totalCoins = count;
    }
}