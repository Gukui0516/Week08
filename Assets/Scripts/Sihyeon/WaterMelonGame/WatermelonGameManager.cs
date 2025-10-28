using UnityEngine;
using System.Collections;
using System.Collections.Generic; // [추가] b. 병합 카운트
using System.Linq; // [추가] e, f. Draggable 로깅

/// <summary>
/// 수박 게임의 전체 흐름을 관리하는 게임 매니저입니다.
/// 싱글톤 패턴으로 구현되었습니다.
/// </summary>
public class WatermelonGameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("스폰 관리를 담당하는 SpawnManager입니다.")]
    [SerializeField] private SpawnManager spawnManager;

    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false; // [참고] 이 변수는 DEBUG 로그에서 더 이상 사용되지 않습니다.

    [Header("Spawn Repeat Settings")]
    [Tooltip("과일 생성 간격 (초)")]
    [SerializeField] private float spawnInterval = 0.2f;
    [Tooltip("과일 생성 횟수")]
    [SerializeField] private int spawnCount = 6;

    // 싱글톤 인스턴스
    private static WatermelonGameManager instance;
    public static WatermelonGameManager Instance => instance;

    // 참조
    private WatermelonObjectPool objectPool;

    // SpawnManager 공개 프로퍼티 추가
    public SpawnManager SpawnManager => spawnManager;

    // [추가] b. 과일 병합 카운트용 변수
    private int _totalMergeCount = 0;
    private Dictionary<FruitMergeData.FruitType, int> _mergeCountByType = new Dictionary<FruitMergeData.FruitType, int>();

    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            // [변경] LogLevel.WARNING
            LogSystem.PushLog(LogLevel.WARNING, "SingletonDuplicate",
                $"[WatermelonGameManager] 중복된 인스턴스 감지! {gameObject.name}를 파괴합니다.", useUnityDebug: true);
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        // ObjectPool 참조 가져오기
        objectPool = WatermelonObjectPool.Instance;

        if (objectPool == null)
        {
            // [변경] LogLevel.ERROR
            LogSystem.PushLog(LogLevel.ERROR, "MissingReference",
                "[WatermelonGameManager] WatermelonObjectPool을 찾을 수 없습니다!", useUnityDebug: true);
            return;
        }

        // SpawnManager 초기화
        if (spawnManager == null)
        {
            spawnManager = GetComponent<SpawnManager>();

            if (spawnManager == null)
            {
                // [변경] LogLevel.ERROR
                LogSystem.PushLog(LogLevel.ERROR, "MissingReference",
                    "[WatermelonGameManager] SpawnManager를 찾을 수 없습니다!", useUnityDebug: true);
                return;
            }
        }

        spawnManager.Initialize(objectPool);

        // [변경] 신규 DEBUG 로그 가이드 적용
        LogSystem.PushLog(LogLevel.DEBUG, "GameManagerInit",
            "[WatermelonGameManager] 게임 매니저 초기화 완료 (3D 모드, 8단계)");
    }

    private void Update()
    {
        // 테스트 입력
        HandleTestInput();
    }

    /// <summary>
    /// 테스트용 입력을 처리합니다.
    /// </summary>
    private void HandleTestInput()
    {
        // 스페이스바: 랜덤 과일 반복 생성
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnRandomFruitsRepeatedly();
        }
    }

    /// <summary>
    /// 랜덤한 위치에 랜덤한 과일을 생성합니다.
    /// </summary>
    public void SpawnRandomFruit()
    {
        if (spawnManager == null)
        {
            // [변경] LogLevel.ERROR
            LogSystem.PushLog(LogLevel.ERROR, "NullReference",
                "[WatermelonGameManager] SpawnManager가 null입니다!", useUnityDebug: true);
            return;
        }

        GameObject fruit = spawnManager.SpawnRandomFruit();

        if (fruit != null)
        {
            // [변경] 신규 DEBUG 로그 가이드 적용 (showDebugLogs 제거)
            LogSystem.PushLog(LogLevel.DEBUG, "SpawnTest",
                $"[WatermelonGameManager] 과일 생성 성공: {fruit.name}");
        }
    }

    /// <summary>
    /// 지정된 간격으로 지정된 횟수만큼 랜덤 과일을 반복 생성합니다.
    /// </summary>
    public void SpawnRandomFruitsRepeatedly()
    {
        StartCoroutine(SpawnFruitsCoroutine());
    }

    /// <summary>
    /// 과일 반복 생성을 위한 코루틴입니다.
    /// </summary>
    private IEnumerator SpawnFruitsCoroutine()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnRandomFruit();
            if (i < spawnCount - 1) // 마지막 반복에서는 대기하지 않음
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // [추가] b. 병합 카운트를 증가시키고 반환하는 메서드
    public (int total, int byType) IncrementMergeCount(FruitMergeData.FruitType mergedType)
    {
        // 1. 전체 카운트 증가
        _totalMergeCount++;

        // 2. 타입별 카운트 증가
        if (!_mergeCountByType.ContainsKey(mergedType))
        {
            _mergeCountByType[mergedType] = 0;
        }
        _mergeCountByType[mergedType]++;

        return (_totalMergeCount, _mergeCountByType[mergedType]);
    }

    // [추가] e, f 항목을 처리하기 위한 공용 헬퍼
    public void LogDraggableObjects(string eventKey)
    {
        var draggables = GameObject.FindGameObjectsWithTag("Draggable");

        // LINQ를 사용하여 'Handle'이 아닌 객체의 이름만 추출
        var objectNames = draggables
            .Where(obj => obj.name != "Handle")
            .Select(obj => obj.name);

        // 쉼표로 구분된 문자열 생성
        string items = string.Join(", ", objectNames);
        int count = objectNames.Count();

        string logValue = $"Count: {count}, Items: [{items}]";

        // "GameClear" 또는 "BeforeLeverPull" 등 전달받은 Key로 로그 기록
        LogSystem.PushLog(LogLevel.INFO, eventKey, logValue);
    }
}