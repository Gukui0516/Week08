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
            Debug.LogError("[GameManager] TetrisBlockSpawner 참조가 없습니다!");
            enabled = false;
            return;
        }

        if (lineChecker == null)
        {
            Debug.LogError("[GameManager] TetrisLineChecker 참조가 없습니다!");
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
                Debug.Log("[GameManager] 라인 제거 이벤트 등록 완료");
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
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Bomb"))
            {
                var bomb = collider.GetComponent<BombC>();
                if (bomb != null)
                {
                    bomb.Explode();
                    BombManager.Instance.NotifyBombExploded(bomb.gameObject);

                    //// 목표 폭탄 개수 감소
                    //ClearManager clearManager = Object.FindAnyObjectByType<ClearManager>();
                    //if (clearManager != null)
                    //{
                    //    clearManager.DecreaseGoalBombCount();
                    //}
                }
                else
                {
                    Debug.LogWarning($"[GameManager] 폭탄 블록 {collider.name}에 BombC 컴포넌트가 없습니다.");
                }
            }
        }
    }

    /// <summary>
    /// 라인 제거 이벤트 핸들러
    /// </summary>
    private void OnLineRemoved(float height, bool isBombLine)
    {
        totalLinesCleared++;

        if (showDebugLogs)
        {
            Debug.Log($"[GameManager] 라인 제거! 총 {totalLinesCleared}줄 | 높이: {height}");
        }

        // 폭탄 블록 폭발 처리
        if (isBombLine)
        {
            TriggerBombExplosions(height);
        }

        // 라인 제거할 때마다 폭탄 블록 1개 소환
        if (spawnBombOnLineClear)
        {
            blockSpawner.QueueBombBlock();
        }

        // 설정한 라인 수 이상 지우면 일반 블록 스폰 중지
        // if (totalLinesCleared >= linesToStopNormalSpawn)
        // {
        //     blockSpawner.DisableSpawning();

        //     if (showDebugLogs)
        //     {
        //         Debug.Log($"[GameManager] ⚠️ {linesToStopNormalSpawn}줄 달성! 일반 블록 생성 중지");
        //     }
        // }

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
            Debug.Log("[GameManager] 게임 리셋!");
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