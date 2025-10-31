using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 스폰 영역 모드
/// </summary>
public enum SpawnAreaMode
{
    Surface,  // Collider 표면
    Volume    // Collider 내부 볼륨
}




/// <summary>
/// TypedObjectPool 기반 랜덤 프리팹 스포너
/// </summary>
public class RandomSpawner : MonoBehaviour
{
    /// <summary>
    /// 스폰 명령 데이터
    /// </summary>
    public struct SpawnCommand
    {
        public bool IsImmediate;
        public int Count;
        public float DurationSeconds;
    }

    #region Serialized Fields
    [TabGroup("Pool Settings")]
    [Required, InfoBox("스폰에 사용할 오브젝트 풀")]
    [SerializeField] private TypedObjectPool<SpawnObjectType> _objectPool;

    [TabGroup("Pool Settings")]
    [SerializeField] private SpawnObjectType _spawnType = SpawnObjectType.None;

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

    [TabGroup("Timing Settings")]
    [Min(0.1f), InfoBox("순차 생성 시 전체 소요 시간(초)")]
    [SerializeField] private float _sequentialSpawnDurationSeconds = 2.0f;

    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Debug Methods - Odin Buttons
    [Button("즉시 1개 생성", ButtonSizes.Medium)]
    private void DebugSpawnOne()
    {
        SpawnImmediate(1);
    }

    [Button("즉시 30개 생성", ButtonSizes.Medium)]
    private void DebugSpawnThirty()
    {
        SpawnImmediate(30);
    }

    [Button("30개 순차 생성 (2초)", ButtonSizes.Medium)]
    private void DebugSpawnThirtySequential()
    {
        SpawnSequential(30, 2.0f);
    }

    [Button("100개 순차 생성 (2초)", ButtonSizes.Medium)]
    private void DebugSpawnHundredSequential()
    {
        SpawnSequential(100, 2.0f);
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
    private bool _isSequentialSpawning;
    private int _sequentialTargetCount;
    private float _sequentialElapsedTime;
    private float _sequentialSpawnInterval;
    private int _sequentialSpawnedCount;
    private Queue<SpawnCommand> _spawnCommandQueue;
    private bool _isExecutingCommand;

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
    /// 즉시 지정 개수만큼 스폰
    /// </summary>
    /// <param name="count">스폰할 개수</param>
    public void SpawnImmediate(int count)
    {
        if (!ValidateSpawnRequest(count))
            return;

        SpawnCommand command = new SpawnCommand
        {
            IsImmediate = true,
            Count = count,
            DurationSeconds = 0f
        };

        _spawnCommandQueue.Enqueue(command);
        Log($"Queued immediate spawn: {count} objects (Queue size: {_spawnCommandQueue.Count})");
    }

    /// <summary>
    /// 지정 시간 동안 균등하게 순차 스폰
    /// </summary>
    /// <param name="count">스폰할 총 개수</param>
    /// <param name="durationSeconds">전체 스폰 소요 시간(초)</param>
    public void SpawnSequential(int count, float durationSeconds)
    {
        if (!ValidateSpawnRequest(count))
            return;

        SpawnCommand command = new SpawnCommand
        {
            IsImmediate = false,
            Count = count,
            DurationSeconds = durationSeconds
        };

        _spawnCommandQueue.Enqueue(command);
        Log($"Queued sequential spawn: {count} objects over {durationSeconds:F2}s (Queue size: {_spawnCommandQueue.Count})");
    }

    /// <summary>
    /// 순차 스폰 중단
    /// </summary>
    public void StopSequentialSpawn()
    {
        if (!_isSequentialSpawning)
            return;

        Log($"Sequential spawn stopped. Spawned {_sequentialSpawnedCount}/{_sequentialTargetCount}", forcely: true);

        _isSequentialSpawning = false;
        _sequentialTargetCount = 0;
        _sequentialElapsedTime = 0f;
        _sequentialSpawnedCount = 0;
        _isExecutingCommand = false; // 큐 재개
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

        // 역순 순회로 안전하게 제거
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

        // 리스트 끝에서부터 회수
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

    #region Private Methods - Spawn Logic
    /// <summary>
    /// 단일 오브젝트 스폰
    /// </summary>
    /// <returns>스폰된 GameObject</returns>
    private GameObject SpawnSingle()
    {
        Vector3 spawnPosition = _randomizePosition ? GetRandomPosition() : transform.position;
        Quaternion spawnRotation = _randomizeRotation ? GetRandomRotation() : Quaternion.identity;

        GameObject spawnedObject = _objectPool.SpawnObject(_spawnType, spawnPosition, spawnRotation, isForcely: false);

        if (spawnedObject != null)
        {
            _spawnedObjects.Add(spawnedObject);
            Log($"Spawned object at {spawnPosition}");
        }
        else
        {
            LogError("Failed to spawn object from pool");
        }

        return spawnedObject;
    }

    /// <summary>
    /// 순차 스폰 업데이트 (Update에서 호출)
    /// </summary>
    private void UpdateSequentialSpawn()
    {
        _sequentialElapsedTime += Time.deltaTime;

        // 현재 시간까지 스폰해야 할 개수 계산
        int targetSpawnCount = Mathf.FloorToInt(_sequentialElapsedTime / _sequentialSpawnInterval);
        targetSpawnCount = Mathf.Min(targetSpawnCount, _sequentialTargetCount);

        // 목표 개수만큼 스폰
        int spawnThisFrame = targetSpawnCount - _sequentialSpawnedCount;
        for (int i = 0; i < spawnThisFrame; i++)
        {
            SpawnSingle();
            _sequentialSpawnedCount++;
        }

        // 완료 체크
        if (_sequentialSpawnedCount >= _sequentialTargetCount)
        {
            Log($"Sequential spawn completed: {_sequentialSpawnedCount} objects", forcely: true);
            StopSequentialSpawn();
            _isExecutingCommand = false;
        }
    }

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
            ExecuteImmediateCommand(command.Count);
        }
        else
        {
            ExecuteSequentialCommand(command.Count, command.DurationSeconds);
        }
    }

    /// <summary>
    /// 즉시 스폰 명령 실행
    /// </summary>
    /// <param name="count">스폰할 개수</param>
    private void ExecuteImmediateCommand(int count)
    {
        Log($"Executing immediate spawn: {count} objects", forcely: true);

        for (int i = 0; i < count; i++)
        {
            SpawnSingle();
        }

        Log($"Immediate spawn completed. Total spawned: {_spawnedObjects.Count}", forcely: true);
        _isExecutingCommand = false;
    }

    /// <summary>
    /// 순차 스폰 명령 실행
    /// </summary>
    /// <param name="count">스폰할 총 개수</param>
    /// <param name="durationSeconds">전체 스폰 소요 시간(초)</param>
    private void ExecuteSequentialCommand(int count, float durationSeconds)
    {
        _isSequentialSpawning = true;
        _sequentialTargetCount = count;
        _sequentialElapsedTime = 0f;
        _sequentialSpawnedCount = 0;
        _sequentialSpawnInterval = durationSeconds / count;

        Log($"Executing sequential spawn: {count} objects over {durationSeconds:F2} seconds (interval: {_sequentialSpawnInterval:F3}s)", forcely: true);
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
        float x = _lockXAxis ? 0f : Random.Range(0f, 360f);
        float y = _lockYAxis ? 0f : Random.Range(0f, 360f);
        float z = _lockZAxis ? 0f : Random.Range(0f, 360f);

        return Quaternion.Euler(x, y, z);
    }

    /// <summary>
    /// Collider 표면의 랜덤 위치 생성
    /// </summary>
    /// <returns>표면 월드 좌표</returns>
    private Vector3 GetRandomSurfacePosition()
    {
        Bounds bounds = _spawnAreaCollider.bounds;

        // 볼륨 내 랜덤 위치 생성 후 표면으로 투영
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        // ClosestPoint로 표면에 투영
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
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
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

        if (_spawnType == SpawnObjectType.None)
        {
            LogWarning("Spawn type is set to None");
        }
    }

    /// <summary>
    /// 스폰 요청 유효성 검증
    /// </summary>
    /// <param name="count">스폰 개수</param>
    /// <returns>유효 여부</returns>
    private bool ValidateSpawnRequest(int count)
    {
        if (count <= 0)
        {
            LogWarning("Spawn count must be positive");
            return false;
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

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        LogSystem.PushLog(LogLevel.ERROR, GetType().Name, message, true);
    }
    #endregion
}