using UnityEngine;

/// <summary>
/// 스테이지별 설정을 관리하는 스크립트입니다.
/// 각 스테이지 씬마다 1개씩 배치하여 사용합니다.
/// 목표 폭탄 개수, 자동 카운팅 모드 등을 설정할 수 있습니다.
/// 
/// 폭탄 개수 구분:
/// - 목표 폭탄: 클리어를 위해 터트려야 하는 폭탄 개수
/// - 생성된 폭탄: 실제로 씬에 생성된 폭탄 개수
/// - 터진 폭탄: 생성된 폭탄 중 폭발한 개수
/// - 남은 폭탄: 생성된 폭탄 - 터진 폭탄
/// </summary>
public class StageConfig : MonoBehaviour
{
    private static StageConfig instance;
    private static bool isQuitting = false;

    /// <summary>
    /// StageConfig의 싱글톤 인스턴스입니다.
    /// 씬마다 1개만 존재해야 합니다.
    /// </summary>
    public static StageConfig Instance
    {
        get
        {
            if (isQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<StageConfig>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("StageConfig");
                    instance = obj.AddComponent<StageConfig>();
                }
            }
            return instance;
        }
    }

    [Header("Goal Bomb Settings")]
    [Tooltip("목표 폭탄 개수 설정 모드를 선택합니다.")]
    [SerializeField] private GoalBombMode goalBombMode = GoalBombMode.AutoCount;

    [Tooltip("Manual 모드일 때 목표로 삼을 폭탄 개수입니다.")]
    [SerializeField] private int manualGoalBombCount = 5;

    [Header("Debug Settings")]
    [Tooltip("스테이지 설정 관련 로그를 출력합니다.")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// 목표 폭탄 개수 설정 모드
    /// </summary>
    public enum GoalBombMode
    {
        [Tooltip("씬에 있는 모든 Bomb 태그 오브젝트를 자동으로 목표로 설정합니다.")]
        AutoCount,

        [Tooltip("Inspector에서 설정한 개수를 목표로 사용합니다. 동적 생성 스테이지에 유용합니다.")]
        Manual,

        [Tooltip("BombManager에 등록된 폭탄만 목표로 삼습니다. RegisterGoalBomb()로 등록 필요.")]
        RegisterOnly
    }

    /// <summary>
    /// 현재 설정된 목표 폭탄 개수 모드를 반환합니다.
    /// </summary>
    public GoalBombMode CurrentGoalBombMode => goalBombMode;

    /// <summary>
    /// Manual 모드일 때 설정된 목표 폭탄 개수를 반환합니다.
    /// </summary>
    public int ManualGoalBombCount => manualGoalBombCount;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            isQuitting = false;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"<color=green>[StageConfig]</color> 스테이지 설정 초기화 완료\n" +
                      $"모드: {goalBombMode} | 수동 목표: {manualGoalBombCount}");
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            isQuitting = true;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>
    /// 현재 스테이지의 목표 폭탄 개수를 반환합니다.
    /// 클리어하기 위해 터트려야 하는 폭탄의 개수입니다.
    /// </summary>
    /// <returns>목표 폭탄 개수</returns>
    public int GetGoalBombCount()
    {
        if (BombManager.Instance == null)
        {
            Debug.LogWarning("[StageConfig] BombManager를 찾을 수 없습니다.");
            return 0;
        }

        int goalCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
                // 씬에 있는 모든 Bomb 태그 오브젝트를 목표로 설정
                goalCount = BombManager.Instance.GetTotalBombCount();
                break;

            case GoalBombMode.Manual:
                // Inspector에서 설정한 개수 사용
                goalCount = manualGoalBombCount;
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄만 카운트
                goalCount = BombManager.Instance.GetRegisteredGoalBombCount();
                break;
        }

        if (enableDebugLog)
        {
            Debug.Log($"<color=green>[StageConfig]</color> 목표 폭탄 개수: {goalCount} (모드: {goalBombMode})");
        }

        return goalCount;
    }

    /// <summary>
    /// 게임 시작 후 생성된 총 폭탄 개수를 반환합니다.
    /// </summary>
    /// <returns>생성된 총 폭탄 개수</returns>
    public int GetSpawnedBombCount()
    {
        if (BombManager.Instance == null)
        {
            return 0;
        }

        int spawnedCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
            case GoalBombMode.Manual:
                // 생성 추적 시스템 사용
                spawnedCount = BombManager.Instance.GetTotalSpawnedBombs();
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄 개수 (등록 = 생성으로 간주)
                spawnedCount = BombManager.Instance.GetRegisteredGoalBombCount();
                break;
        }

        return spawnedCount;
    }

    /// <summary>
    /// 게임 시작 후 폭발한 총 폭탄 개수를 반환합니다.
    /// </summary>
    /// <returns>폭발한 총 폭탄 개수</returns>
    public int GetExplodedBombCount()
    {
        if (BombManager.Instance == null)
        {
            return 0;
        }

        int explodedCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
            case GoalBombMode.Manual:
                // 생성 추적 시스템 사용
                explodedCount = BombManager.Instance.GetTotalExplodedBombs();
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄 중 비활성화된 개수
                int totalRegistered = BombManager.Instance.GetRegisteredGoalBombCount();
                int activeRegistered = BombManager.Instance.GetActiveRegisteredGoalBombCount();
                explodedCount = totalRegistered - activeRegistered;
                break;
        }

        return explodedCount;
    }

    /// <summary>
    /// 현재 남은 폭탄 개수를 반환합니다.
    /// 생성된 폭탄 - 터진 폭탄 = 남은 폭탄
    /// </summary>
    /// <returns>남은 폭탄 개수</returns>
    public int GetRemainingBombCount()
    {
        if (BombManager.Instance == null)
        {
            return 0;
        }

        int remainingCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
            case GoalBombMode.Manual:
                // 현재 씬에 활성화된 폭탄 개수
                remainingCount = BombManager.Instance.GetActiveBombCount();
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄 중 활성화된 것만
                remainingCount = BombManager.Instance.GetActiveRegisteredGoalBombCount();
                break;
        }

        return remainingCount;
    }

    /// <summary>
    /// 스테이지 클리어 조건을 만족하는지 확인합니다.
    /// 조건: 목표 개수만큼 폭탄이 터졌는지 확인
    /// </summary>
    /// <returns>클리어 조건 만족 여부</returns>
    public bool IsClearConditionMet()
    {
        int goalCount = GetGoalBombCount();
        int explodedCount = GetExplodedBombCount();
        int remainingCount = GetRemainingBombCount();

        // 클리어 조건:
        // 1. 목표 개수만큼 폭탄이 터졌음
        // 2. 남은 폭탄이 0개 (모든 생성된 폭탄이 처리됨)
        bool isClear = (explodedCount >= goalCount) && (remainingCount <= 0);

        if (enableDebugLog && isClear)
        {
            Debug.Log($"<color=green>[StageConfig]</color> 클리어 조건 만족!\n" +
                      $"목표: {goalCount} | 터진 폭탄: {explodedCount} | 남은 폭탄: {remainingCount}");
        }

        return isClear;
    }

#if UNITY_EDITOR
    [ContextMenu("목표 폭탄 개수 확인")]
    private void DebugGoalBombCount()
    {
        int goalCount = GetGoalBombCount();
        int spawnedCount = GetSpawnedBombCount();
        int explodedCount = GetExplodedBombCount();
        int remainingCount = GetRemainingBombCount();

        Debug.Log($"<color=green>[StageConfig]</color> 폭탄 현황\n" +
                  $"목표: {goalCount}\n" +
                  $"생성: {spawnedCount}\n" +
                  $"터짐: {explodedCount}\n" +
                  $"남음: {remainingCount}\n" +
                  $"클리어 가능: {IsClearConditionMet()}");
    }

    private void OnValidate()
    {
        // Manual 모드일 때 목표 개수가 0 이하면 경고
        if (goalBombMode == GoalBombMode.Manual && manualGoalBombCount <= 0)
        {
            Debug.LogWarning("[StageConfig] Manual 모드에서 목표 폭탄 개수는 1 이상이어야 합니다.");
        }
    }
#endif
}