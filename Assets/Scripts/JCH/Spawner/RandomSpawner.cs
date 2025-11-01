using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰 영역 모드
/// </summary>
public enum SpawnAreaMode
{
    Surface,
    Volume
}

/// <summary>
/// TypedObjectPool 기반 랜덤 프리팹 스포너 (통합 혼합 타입 지원)
/// </summary>
public class RandomSpawner : MonoBehaviour
{
    /// <summary>
    /// 스폰 명령 데이터 (단일/혼합 타입 통합)
    /// </summary>
    public struct SpawnCommand
    {
        public bool IsImmediate;
        public SpawnObjectType[] Types;  // Length 1 = 단일, 2+ = 혼합
        public int[] Counts;
        public float DurationSeconds;
    }

    #region Serialized Fields
    [TabGroup("Pool Settings")]
    [Required, InfoBox("스폰에 사용할 오브젝트 풀")]
    [SerializeField] private TypedObjectPool<SpawnObjectType> _objectPool;

    [TabGroup("Spawn Settings")]
    [SerializeField] private bool _randomizePosition = true;

    [TabGroup("Spawn Settings")]
    [SerializeField] private bool _randomizeRotation = true;

    [TabGroup("Spawn Settings")]
    [ShowIf("_randomizeRotation")]
    [SerializeField] private bool _lockXAxis = false;

    [TabGroup("Spawn Settings")]
    [ShowIf("_randomizeRotation")]
    [SerializeField] private bool _lockYAxis = false;

    [TabGroup("Spawn Settings")]
    [ShowIf("_randomizeRotation")]
    [SerializeField] private bool _lockZAxis = false;

    [TabGroup("Area Settings")]
    [Required, InfoBox("스폰 영역으로 사용할 Collider")]
    [SerializeField] private Collider _spawnAreaCollider;

    [TabGroup("Area Settings")]
    [SerializeField] private SpawnAreaMode _areaMode = SpawnAreaMode.Volume;

    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Debug Methods - Odin Buttons
    [Button("즉시 1개 생성", ButtonSizes.Medium)]
    private void DebugSpawnOne()
    {
        SpawnImmediate(SpawnObjectType.Default, 1);
    }

    [Button("즉시 30개 생성", ButtonSizes.Medium)]
    private void DebugSpawnThirty()
    {
        SpawnImmediate(SpawnObjectType.Default, 30);
    }

    [Button("30개 순차 생성 (2초)", ButtonSizes.Medium)]
    private void DebugSpawnThirtySequential()
    {
        SpawnSequential(SpawnObjectType.Default, 30, 2.0f);
    }

    [Button("100개 순차 생성 (2초)", ButtonSizes.Medium)]
    private void DebugSpawnHundredSequential()
    {
        SpawnSequential(SpawnObjectType.Default, 100, 2.0f);
    }

    [Button("일괄 회수", ButtonSizes.Medium)]
    private void DebugRecallAll()
    {
        RecallAll();
    }

    [Button("최근 1개 회수", ButtonSizes.Medium)]
    private void DebugRecallLatest()
    {
        RecallLatest(1);
    }
    #endregion

    #region Properties
    /// <summary>현재 순차 스폰 진행 중 여부</summary>
    public bool IsSpawning => _isSequentialSpawning;

    /// <summary>현재 스폰된 오브젝트 개수</summary>
    public int SpawnedCount => _spawnedObjects.Count;

    /// <summary>스폰된 오브젝트 목록 (읽기 전용)</summary>
    public IReadOnlyList<GameObject> SpawnedObjects => _spawnedObjects;
    #endregion

    #region Private Fields
    private List<GameObject> _spawnedObjects;
    private Queue<SpawnCommand> _spawnCommandQueue;
    private bool _isExecutingCommand;

    // 순차 스폰 상태
    private bool _isSequentialSpawning;
    private SpawnObjectType[] _sequentialTypes;
    private int[] _sequentialCounts;
    private List<(SpawnObjectType type, int index)> _sequentialShuffledOrder;
    private int _sequentialCurrentIndex;
    private float _sequentialElapsedTime;
    private float _sequentialSpawnInterval;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        // 큐 처리
        if (!_isExecutingCommand && _spawnCommandQueue.Count > 0)
        {
            ExecuteNextCommand();
        }

        // 순차 스폰 업데이트
        if (_isSequentialSpawning)
        {
            UpdateSequentialSpawn();
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
        _spawnedObjects = new List<GameObject>();
        _spawnCommandQueue = new Queue<SpawnCommand>();
        _isSequentialSpawning = false;
        _isExecutingCommand = false;

        if (_objectPool != null)
        {
            _objectPool.Initialize(transform);
        }

        ValidateComponents();

        Log("RandomSpawner initialized");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        StopSequentialSpawn();
        RecallAll();

        Log("RandomSpawner cleanup completed");
    }
    #endregion

    #region Public Methods - Spawn Control
    /// <summary>
    /// 단일 타입 즉시 스폰
    /// </summary>
    /// <param name="type">스폰할 타입</param>
    /// <param name="count">스폰할 개수</param>
    public void SpawnImmediate(SpawnObjectType type, int count)
    {
        SpawnObjectType[] types = new SpawnObjectType[] { type };
        int[] counts = new int[] { count };
        SpawnMixedImmediate(types, counts);
    }

    /// <summary>
    /// 단일 타입 순차 스폰
    /// </summary>
    /// <param name="type">스폰할 타입</param>
    /// <param name="count">스폰할 총 개수</param>
    /// <param name="durationSeconds">전체 스폰 소요 시간(초)</param>
    public void SpawnSequential(SpawnObjectType type, int count, float durationSeconds)
    {
        SpawnObjectType[] types = new SpawnObjectType[] { type };
        int[] counts = new int[] { count };
        SpawnMixedSequential(types, counts, durationSeconds);
    }

    /// <summary>
    /// 혼합 타입 즉시 스폰
    /// </summary>
    /// <param name="types">스폰할 타입 배열</param>
    /// <param name="counts">각 타입의 개수 배열</param>
    public void SpawnMixedImmediate(SpawnObjectType[] types, int[] counts)
    {
        if (!ValidateSpawnRequest(types, counts))
            return;

        SpawnCommand command = new SpawnCommand
        {
            IsImmediate = true,
            Types = types,
            Counts = counts,
            DurationSeconds = 0f
        };

        _spawnCommandQueue.Enqueue(command);

        int totalCount = 0;
        for (int i = 0; i < counts.Length; i++)
            totalCount += counts[i];

        Log($"Queued immediate spawn: {totalCount} objects ({types.Length} types)");
    }

    /// <summary>
    /// 혼합 타입 순차 스폰
    /// </summary>
    /// <param name="types">스폰할 타입 배열</param>
    /// <param name="counts">각 타입의 개수 배열</param>
    /// <param name="durationSeconds">전체 스폰 소요 시간(초)</param>
    public void SpawnMixedSequential(SpawnObjectType[] types, int[] counts, float durationSeconds)
    {
        if (!ValidateSpawnRequest(types, counts))
            return;

        SpawnCommand command = new SpawnCommand
        {
            IsImmediate = false,
            Types = types,
            Counts = counts,
            DurationSeconds = durationSeconds
        };

        _spawnCommandQueue.Enqueue(command);

        int totalCount = 0;
        for (int i = 0; i < counts.Length; i++)
            totalCount += counts[i];

        Log($"Queued sequential spawn: {totalCount} objects ({types.Length} types) over {durationSeconds:F2}s");
    }

    /// <summary>
    /// 순차 스폰 중단
    /// </summary>
    public void StopSequentialSpawn()
    {
        if (!_isSequentialSpawning)
            return;

        int totalTarget = 0;
        for (int i = 0; i < _sequentialCounts.Length; i++)
            totalTarget += _sequentialCounts[i];

        Log($"Sequential spawn stopped. Spawned {_sequentialCurrentIndex}/{totalTarget}", forcely: true);

        _isSequentialSpawning = false;
        _sequentialTypes = null;
        _sequentialCounts = null;
        _sequentialShuffledOrder = null;
        _sequentialCurrentIndex = 0;
        _sequentialElapsedTime = 0f;
        _isExecutingCommand = false;
    }
    #endregion

    #region Public Methods - Recall Control
    /// <summary>
    /// 모든 스폰된 오브젝트 회수
    /// </summary>
    /// <returns>회수된 개수</returns>
    public int RecallAll()
    {
        if (_spawnedObjects.Count == 0)
        {
            Log("No objects to recall");
            return 0;
        }

        int recalledCount = 0;

        for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = _spawnedObjects[i];

            if (obj != null)
            {
                RecallObject(obj);
                recalledCount++;
            }

            _spawnedObjects.RemoveAt(i);
        }

        Log($"Recalled all objects: {recalledCount}", forcely: true);
        return recalledCount;
    }

    /// <summary>
    /// 가장 최근에 스폰된 N개 회수
    /// </summary>
    /// <param name="count">회수할 개수</param>
    /// <returns>실제 회수된 개수</returns>
    public int RecallLatest(int count)
    {
        if (count <= 0)
        {
            LogWarning("Recall count must be positive");
            return 0;
        }

        if (_spawnedObjects.Count == 0)
        {
            Log("No objects to recall");
            return 0;
        }

        int recallCount = Mathf.Min(count, _spawnedObjects.Count);
        int recalledCount = 0;

        for (int i = 0; i < recallCount; i++)
        {
            int lastIndex = _spawnedObjects.Count - 1;
            GameObject obj = _spawnedObjects[lastIndex];

            if (obj != null)
            {
                RecallObject(obj);
                recalledCount++;
            }

            _spawnedObjects.RemoveAt(lastIndex);
        }

        Log($"Recalled latest {recalledCount} objects", forcely: true);
        return recalledCount;
    }
    #endregion

    #region Private Methods - Command Queue
    /// <summary>
    /// 큐에서 다음 명령 실행
    /// </summary>
    private void ExecuteNextCommand()
    {
        if (_spawnCommandQueue.Count == 0)
            return;

        SpawnCommand command = _spawnCommandQueue.Dequeue();
        _isExecutingCommand = true;

        if (command.IsImmediate)
        {
            ExecuteImmediateCommand(command.Types, command.Counts);
        }
        else
        {
            ExecuteSequentialCommand(command.Types, command.Counts, command.DurationSeconds);
        }
    }

    /// <summary>
    /// 즉시 스폰 명령 실행
    /// </summary>
    private void ExecuteImmediateCommand(SpawnObjectType[] types, int[] counts)
    {
        int totalCount = 0;
        for (int i = 0; i < counts.Length; i++)
            totalCount += counts[i];

        Log($"Executing immediate spawn: {totalCount} objects ({types.Length} types)", forcely: true);

        List<(SpawnObjectType type, int index)> shuffledOrder = CreateShuffledSpawnOrder(types, counts);

        foreach (var pair in shuffledOrder)
        {
            SpawnSingle(pair.type);
        }

        Log($"Immediate spawn completed. Total spawned: {_spawnedObjects.Count}", forcely: true);
        _isExecutingCommand = false;
    }

    /// <summary>
    /// 순차 스폰 명령 실행
    /// </summary>
    private void ExecuteSequentialCommand(SpawnObjectType[] types, int[] counts, float durationSeconds)
    {
        int totalCount = 0;
        for (int i = 0; i < counts.Length; i++)
            totalCount += counts[i];

        _isSequentialSpawning = true;
        _sequentialTypes = types;
        _sequentialCounts = counts;
        _sequentialShuffledOrder = CreateShuffledSpawnOrder(types, counts);
        _sequentialCurrentIndex = 0;
        _sequentialElapsedTime = 0f;
        _sequentialSpawnInterval = durationSeconds / totalCount;

        Log($"Executing sequential spawn: {totalCount} objects ({types.Length} types) over {durationSeconds:F2}s (interval: {_sequentialSpawnInterval:F3}s)", forcely: true);
    }
    #endregion

    #region Private Methods - Spawn Logic
    /// <summary>
    /// 단일 오브젝트 스폰
    /// </summary>
    /// <param name="type">스폰할 타입</param>
    /// <returns>스폰된 GameObject</returns>
    private GameObject SpawnSingle(SpawnObjectType type)
    {
        Vector3 spawnPosition = _randomizePosition ? GetRandomPosition() : transform.position;
        Quaternion spawnRotation = _randomizeRotation ? GetRandomRotation() : Quaternion.identity;

        GameObject spawnedObject = _objectPool.SpawnObject(type, spawnPosition, spawnRotation, isForcely: false);

        if (spawnedObject != null)
        {
            _spawnedObjects.Add(spawnedObject);
            Log($"Spawned {type} at {spawnPosition}");
        }
        else
        {
            LogError($"Failed to spawn {type} from pool");
        }

        return spawnedObject;
    }

    /// <summary>
    /// 순차 스폰 업데이트 (Update에서 호출)
    /// </summary>
    private void UpdateSequentialSpawn()
    {
        _sequentialElapsedTime += Time.deltaTime;

        int targetSpawnIndex = Mathf.FloorToInt(_sequentialElapsedTime / _sequentialSpawnInterval);
        targetSpawnIndex = Mathf.Min(targetSpawnIndex, _sequentialShuffledOrder.Count);

        while (_sequentialCurrentIndex < targetSpawnIndex)
        {
            var pair = _sequentialShuffledOrder[_sequentialCurrentIndex];
            SpawnSingle(pair.type);
            _sequentialCurrentIndex++;
        }

        if (_sequentialCurrentIndex >= _sequentialShuffledOrder.Count)
        {
            Log($"Sequential spawn completed: {_sequentialCurrentIndex} objects", forcely: true);
            StopSequentialSpawn();
            _isExecutingCommand = false;
        }
    }

    /// <summary>
    /// 타입-인덱스 쌍을 셔플된 순서로 생성
    /// </summary>
    private List<(SpawnObjectType type, int index)> CreateShuffledSpawnOrder(SpawnObjectType[] types, int[] counts)
    {
        List<(SpawnObjectType type, int index)> order = new List<(SpawnObjectType, int)>();

        for (int i = 0; i < types.Length; i++)
        {
            for (int j = 0; j < counts[i]; j++)
            {
                order.Add((types[i], j));
            }
        }

        // Fisher-Yates Shuffle
        for (int i = order.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = order[i];
            order[i] = order[randomIndex];
            order[randomIndex] = temp;
        }

        return order;
    }
    #endregion

    #region Private Methods - Position and Rotation
    /// <summary>
    /// 스폰 영역 내 랜덤 위치 생성
    /// </summary>
    /// <returns>월드 좌표 위치</returns>
    private Vector3 GetRandomPosition()
    {
        if (_spawnAreaCollider == null)
        {
            LogError("Spawn area collider is null");
            return transform.position;
        }

        return _areaMode switch
        {
            SpawnAreaMode.Surface => GetRandomSurfacePosition(),
            SpawnAreaMode.Volume => GetRandomVolumePosition(),
            _ => transform.position
        };
    }

    /// <summary>
    /// 랜덤 회전 생성 (축 잠금 고려)
    /// </summary>
    /// <returns>회전 값</returns>
    private Quaternion GetRandomRotation()
    {
        float x = _lockXAxis ? 0f : UnityEngine.Random.Range(0f, 360f);
        float y = _lockYAxis ? 0f : UnityEngine.Random.Range(0f, 360f);
        float z = _lockZAxis ? 0f : UnityEngine.Random.Range(0f, 360f);

        return Quaternion.Euler(x, y, z);
    }

    /// <summary>
    /// Collider 표면의 랜덤 위치 생성
    /// </summary>
    /// <returns>표면 월드 좌표</returns>
    private Vector3 GetRandomSurfacePosition()
    {
        Bounds bounds = _spawnAreaCollider.bounds;

        Vector3 randomPoint = new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );

        Vector3 surfacePoint = _spawnAreaCollider.ClosestPoint(randomPoint);

        return surfacePoint;
    }

    /// <summary>
    /// Collider 볼륨 내부 랜덤 위치 생성
    /// </summary>
    /// <returns>내부 월드 좌표</returns>
    private Vector3 GetRandomVolumePosition()
    {
        Bounds bounds = _spawnAreaCollider.bounds;

        Vector3 randomPoint = new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );

        return randomPoint;
    }
    #endregion

    #region Private Methods - Recall Logic
    /// <summary>
    /// 단일 오브젝트 회수
    /// </summary>
    /// <param name="obj">회수할 GameObject</param>
    private void RecallObject(GameObject obj)
    {
        if (obj == null)
        {
            LogWarning("Attempted to recall null object");
            return;
        }

        _objectPool.ReturnObject(obj);
        Log($"Recalled object: {obj.name}");
    }
    #endregion

    #region Private Methods - Validation
    /// <summary>
    /// 컴포넌트 유효성 검증
    /// </summary>
    private void ValidateComponents()
    {
        if (_objectPool == null)
        {
            LogError("TypedObjectPool is not assigned!");
        }

        if (_spawnAreaCollider == null)
        {
            LogError("Spawn area collider is not assigned!");
        }
    }

    /// <summary>
    /// 스폰 요청 유효성 검증
    /// </summary>
    /// <param name="types">타입 배열</param>
    /// <param name="counts">개수 배열</param>
    /// <returns>유효 여부</returns>
    private bool ValidateSpawnRequest(SpawnObjectType[] types, int[] counts)
    {
        if (types == null || counts == null)
        {
            LogError("Types or counts array is null");
            return false;
        }

        if (types.Length == 0 || counts.Length == 0)
        {
            LogError("Types or counts array is empty");
            return false;
        }

        if (types.Length != counts.Length)
        {
            LogError($"Array length mismatch: types={types.Length}, counts={counts.Length}");
            return false;
        }

        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] <= 0)
            {
                LogError($"Invalid count at index {i}: {counts[i]}");
                return false;
            }
        }

        if (_objectPool == null)
        {
            LogError("Cannot spawn: TypedObjectPool is null");
            return false;
        }

        if (_spawnAreaCollider == null)
        {
            LogError("Cannot spawn: Spawn area collider is null");
            return false;
        }

        return true;
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            LogSystem.DebugLog(message, null, this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            LogSystem.PushLog(LogLevel.WARNING, GetType().Name, message, true);
    }

    /// <summary>에러 로그 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        LogSystem.PushLog(LogLevel.ERROR, GetType().Name, message, true);
    }
    #endregion
}