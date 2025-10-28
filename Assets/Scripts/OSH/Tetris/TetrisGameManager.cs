using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 테트리스 게임 로직 관리자
/// - 라인 제거 카운트
/// - 모드 전환 결정
/// - 블록 제거 처리
/// 
/// 실제 블록 스폰은 TetrisBlockSpawner가 담당
/// </summary>
public class TetrisGameManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [Tooltip("테트리스 블록 스포너 참조")]
    [SerializeField] private TetrisBlockSpawner blockSpawner;

    [Tooltip("테트리스 라인 체커 참조")]
    [SerializeField] private TetrisLineChecker lineChecker;

    [Header("Game Settings")]
    [Tooltip("라인이 제거될 때마다 폭탄 블록 소환")]
    [SerializeField] private bool spawnBombOnLineClear = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region DebugButtons
    [Button("Forcely Trgger LineBomb Explosion", ButtonSizes.Large)]
    public void DebugTriggerLineBombExplosion()
    {
        TriggerBombExplosions(2.0f);
    }
    #endregion

    #region Private Fields

    private int totalLinesCleared = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateSettings();
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    #endregion

    #region Validation

    private void ValidateSettings()
    {
        if (blockSpawner == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "GameManager_MissingReference", "TetrisBlockSpawner", true);
            enabled = false;
            return;
        }

        if (lineChecker == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "GameManager_MissingReference", "TetrisLineChecker", true);
            enabled = false;
            return;
        }
    }

    #endregion

    #region Event Registration

    private void RegisterEvents()
    {
        if (lineChecker != null)
        {
            lineChecker.onLineRemoved.AddListener(OnLineRemoved);
            if (showDebugLogs)
            {
                LogSystem.DebugLog("[GameManager] 라인 제거 이벤트 등록 완료");
            }
        }
    }

    private void UnregisterEvents()
    {
        if (lineChecker != null)
        {
            lineChecker.onLineRemoved.RemoveListener(OnLineRemoved);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 폭탄 블록의 폭발 로직을 호출합니다.
    /// </summary>
    private void TriggerBombExplosions(float height)
    {
        // 폭탄 블록 탐색
        Collider[] colliders = Physics.OverlapBox(new Vector3(0, height, 0), new Vector3(5.5f, 0.5f, 5.5f));

        int bombCount = 0;
        int explodedCount = 0;

        // 폭탄 개수 세기
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Bomb"))
                bombCount++;
        }

        // 로그: 폭탄 라인 정보
        if (bombCount > 0)
        {
            LogSystem.PushLog(LogLevel.INFO, "BombLine_Height", height);
            LogSystem.PushLog(LogLevel.INFO, "BombLine_BombCount", bombCount);
        }

        // 각 폭탄 폭발 처리
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Bomb"))
            {
                var bomb = collider.GetComponent<BombC>();
                if (bomb != null)
                {
                    explodedCount++;

                    // 핵심 로그: 폭탄 제거(라인) 이벤트 (INFO 레벨)
                    LogSystem.PushLog(LogLevel.INFO, "Bomb_LineCleared", collider.gameObject.name, useUnityDebug: true);

                    // 로그: 개별 폭탄 폭발
                    LogSystem.PushLog(LogLevel.WARNING, "Bomb_ExplodeMethod", "LineClear");
                    LogSystem.PushLog(LogLevel.WARNING, "Bomb_ExplodePosition", collider.transform.position);
                    LogSystem.PushLog(LogLevel.DEBUG, "Bomb_ExplodeIndex", explodedCount);

                    bomb.Explode();
                    BombManager.Instance.NotifyBombExploded(bomb.gameObject);

                }
                else
                {
                    // 로그: 에러 - BombC 컴포넌트 없음
                    LogSystem.PushLog(LogLevel.ERROR, "Bomb_MissingComponent", collider.name, true);
                }
            }
        }

        // 로그: 통합 결과
        if (explodedCount > 0)
        {
            LogSystem.PushLog(LogLevel.WARNING, "BombLine_TotalExploded", explodedCount, true);

            int remainingBombs = blockSpawner.GetSpawnedBombBlocks().Count - explodedCount;
            LogSystem.PushLog(LogLevel.INFO, "BombLine_RemainingBombs", remainingBombs);
        }
    }

    /// <summary>
    /// 라인 제거 이벤트 핸들러
    /// </summary>
    private void OnLineRemoved(float height, bool isBombLine)
    {
        totalLinesCleared++;

        // 로그: 라인 제거 기본 정보
        LogSystem.PushLog(LogLevel.INFO, "Line_Height", height);
        LogSystem.PushLog(LogLevel.INFO, "Line_TotalCount", totalLinesCleared);
        LogSystem.PushLog(LogLevel.INFO, "Line_IsBombLine", isBombLine);

        if (showDebugLogs)
        {
            LogSystem.DebugLog($"[GameManager] 라인 제거! 총 {totalLinesCleared}줄 | 높이: {height}");
        }

        // 폭탄 블록 폭발 처리
        if (isBombLine)
        {
            // 로그: 폭탄 라인 트리거 (중요 이벤트 - Unity 콘솔 출력)
            LogSystem.PushLog(LogLevel.WARNING, "Line_BombTrigger", "BombLineCleared", true);

            TriggerBombExplosions(height);
        }

        // 라인 제거할 때마다 폭탄 블록 1개 소환
        if (spawnBombOnLineClear)
        {
            blockSpawner.QueueBombBlock();

            // 로그: 폭탄 블록 큐 추가
            LogSystem.PushLog(LogLevel.INFO, "Line_BombQueued", true);
        }

        // 로그: 마일스톤 체크 (5줄 단위 - 중요 이정표)
        if (totalLinesCleared % 5 == 0)
        {
            LogSystem.PushLog(LogLevel.INFO, "Line_Milestone", $"{totalLinesCleared}_lines", true);
        }

    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 게임 리셋
    /// </summary>
    public void ResetGame()
    {
        totalLinesCleared = 0;
        blockSpawner.EnableSpawning();

        if (showDebugLogs)
        {
            LogSystem.DebugLog("[GameManager] 게임 리셋!");
        }
    }

    /// <summary>
    /// 현재 제거된 라인 수 반환
    /// </summary>
    public int GetTotalLinesCleared()
    {
        return totalLinesCleared;
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Test: Reset Game")]
    private void DebugResetGame()
    {
        if (Application.isPlaying)
        {
            ResetGame();
        }
    }
#endif
}