using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 타입별 가중치(비율) 설정
/// </summary>
[Serializable]
public struct TypeWeightEntry
{
    public SpawnObjectType type;
    [Range(0f, 1f), Tooltip("0~1 비율. 자동 정규화됨")]
    public float weight;
}

/// <summary>
/// 스폰 모드
/// </summary>
public enum SpawnMode
{
    Immediate,
    Sequential
}

/// <summary>
/// 게임 시작 시 RandomSpawner에게 자동 스폰 명령
/// </summary>
public class AutoSpawnCaller : MonoBehaviour
{
    #region Serialized Fields
    [Header("Spawner Reference")]
    [SerializeField] private RandomSpawner _randomSpawner;

    [Header("Type Settings")]
    [SerializeField] private bool _useMixedTypes = false;
    [SerializeField] private SpawnObjectType _singleType = SpawnObjectType.Default;
    [SerializeField] private TypeWeightEntry[] _typeWeights;

    [Header("Spawn Settings")]
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.Sequential;
    [SerializeField] private int _spawnCount = 10;
    [SerializeField] private float _sequentialDurationSeconds = 2.0f;

    [Header("Timing Settings")]
    [SerializeField] private float _startDelaySeconds = 0f;

    [Header("Debug Settings")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Properties
    /// <summary>초기화 완료 여부</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>현재 사용 중인 스폰 모드</summary>
    public SpawnMode CurrentSpawnMode => _spawnMode;

    /// <summary>설정된 총 스폰 개수</summary>
    public int SpawnCount => _spawnCount;

    /// <summary>혼합 타입 사용 여부</summary>
    public bool UseMixedTypes => _useMixedTypes;
    #endregion

    #region Private Fields
    private Dictionary<SpawnObjectType, float> _normalizedWeights;
    private Coroutine _spawnCoroutine;
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
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _normalizedWeights = new Dictionary<SpawnObjectType, float>();
        _spawnCoroutine = null;
        IsInitialized = false;

        Log("AutoSpawnCaller initialized");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        if (!ValidateSpawner())
        {
            LogError("Spawner validation failed. AutoSpawnCaller will not execute.");
            return;
        }

        if (_useMixedTypes)
        {
            if (!ValidateWeights())
            {
                LogError("Weight validation failed. AutoSpawnCaller will not execute.");
                return;
            }

            NormalizeWeights();
            Log($"Mixed types mode: {_typeWeights.Length} types configured", forcely: true);
        }
        else
        {
            Log($"Single type mode: {_singleType}", forcely: true);
        }

        IsInitialized = true;

        // 지연 후 스폰 실행
        _spawnCoroutine = StartCoroutine(ExecuteSpawnWithDelay());
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        if (_normalizedWeights != null)
        {
            _normalizedWeights.Clear();
        }

        IsInitialized = false;

        Log("AutoSpawnCaller cleanup completed");
    }
    #endregion

    #region Private Methods - Spawn Execution
    /// <summary>
    /// 지연 후 스폰 실행
    /// </summary>
    private IEnumerator ExecuteSpawnWithDelay()
    {
        if (_startDelaySeconds > 0f)
        {
            Log($"Waiting {_startDelaySeconds:F2} seconds before spawn...", forcely: true);
            yield return new WaitForSeconds(_startDelaySeconds);
        }

        ExecuteSpawn();
    }

    /// <summary>
    /// 스폰 모드에 따라 명령 실행
    /// </summary>
    private void ExecuteSpawn()
    {
        if (_spawnCount <= 0)
        {
            LogError($"Invalid spawn count: {_spawnCount}");
            return;
        }

        if (_useMixedTypes)
        {
            // 혼합 타입 모드
            DistributeCountsByWeight(_spawnCount, out SpawnObjectType[] types, out int[] counts);

            if (types == null || counts == null)
            {
                LogError("Failed to distribute counts by weight");
                return;
            }

            if (_spawnMode == SpawnMode.Immediate)
            {
                _randomSpawner.SpawnMixedImmediate(types, counts);
                Log($"Executed mixed immediate spawn: {_spawnCount} objects", forcely: true);
            }
            else
            {
                _randomSpawner.SpawnMixedSequential(types, counts, _sequentialDurationSeconds);
                Log($"Executed mixed sequential spawn: {_spawnCount} objects over {_sequentialDurationSeconds:F2}s", forcely: true);
            }
        }
        else
        {
            // 단일 타입 모드
            if (_singleType == SpawnObjectType.None)
            {
                LogError("Single type mode enabled but type is set to None");
                return;
            }

            if (_spawnMode == SpawnMode.Immediate)
            {
                _randomSpawner.SpawnImmediate(_singleType, _spawnCount);
                Log($"Executed single immediate spawn: {_spawnCount} x {_singleType}", forcely: true);
            }
            else
            {
                _randomSpawner.SpawnSequential(_singleType, _spawnCount, _sequentialDurationSeconds);
                Log($"Executed single sequential spawn: {_spawnCount} x {_singleType} over {_sequentialDurationSeconds:F2}s", forcely: true);
            }
        }
    }
    #endregion

    #region Private Methods - Weight Processing
    /// <summary>
    /// 가중치 배열을 정규화하여 합계가 1.0이 되도록 함
    /// </summary>
    private void NormalizeWeights()
    {
        if (_typeWeights == null || _typeWeights.Length == 0)
        {
            LogError("Cannot normalize: type weights array is null or empty");
            return;
        }

        _normalizedWeights.Clear();

        float totalWeight = 0f;
        foreach (var entry in _typeWeights)
        {
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
        {
            LogError($"Cannot normalize: total weight is {totalWeight}");
            return;
        }

        foreach (var entry in _typeWeights)
        {
            float normalizedWeight = entry.weight / totalWeight;
            _normalizedWeights[entry.type] = normalizedWeight;
            Log($"Normalized weight [{entry.type}]: {entry.weight:F3} -> {normalizedWeight:F3}");
        }

        Log($"Weight normalization completed. {_normalizedWeights.Count} types processed", forcely: true);
    }

    /// <summary>
    /// 총 개수를 가중치에 따라 타입별로 배분
    /// </summary>
    /// <param name="totalCount">총 스폰 개수</param>
    /// <param name="types">배분된 타입 배열 (출력)</param>
    /// <param name="counts">타입별 개수 배열 (출력)</param>
    private void DistributeCountsByWeight(int totalCount, out SpawnObjectType[] types, out int[] counts)
    {
        if (_normalizedWeights == null || _normalizedWeights.Count == 0)
        {
            LogError("Cannot distribute: normalized weights not initialized");
            types = null;
            counts = null;
            return;
        }

        int typeCount = _normalizedWeights.Count;
        types = new SpawnObjectType[typeCount];
        counts = new int[typeCount];

        int distributedTotal = 0;
        int index = 0;

        // 각 타입별 개수 계산 (반올림)
        foreach (var kvp in _normalizedWeights)
        {
            types[index] = kvp.Key;
            float exactCount = totalCount * kvp.Value;
            counts[index] = Mathf.RoundToInt(exactCount);
            distributedTotal += counts[index];
            index++;
        }

        // 반올림 오차 보정
        int difference = totalCount - distributedTotal;

        if (difference != 0)
        {
            // 가장 큰 가중치를 가진 타입에 오차 추가/제거
            int maxWeightIndex = 0;
            float maxWeight = 0f;

            for (int i = 0; i < typeCount; i++)
            {
                if (_normalizedWeights[types[i]] > maxWeight)
                {
                    maxWeight = _normalizedWeights[types[i]];
                    maxWeightIndex = i;
                }
            }

            counts[maxWeightIndex] += difference;
            Log($"Adjusted count for {types[maxWeightIndex]} by {difference} to match total");
        }

        // 로그 출력
        for (int i = 0; i < typeCount; i++)
        {
            Log($"Distributed [{types[i]}]: {counts[i]} objects ({(counts[i] / (float)totalCount * 100f):F1}%)");
        }
    }
    #endregion

    #region Private Methods - Validation
    /// <summary>
    /// 가중치 설정 유효성 검증
    /// </summary>
    /// <returns>유효 여부</returns>
    private bool ValidateWeights()
    {
        if (_typeWeights == null || _typeWeights.Length == 0)
        {
            LogError("Type weights array is null or empty");
            return false;
        }

        float totalWeight = 0f;
        HashSet<SpawnObjectType> uniqueTypes = new HashSet<SpawnObjectType>();

        for (int i = 0; i < _typeWeights.Length; i++)
        {
            TypeWeightEntry entry = _typeWeights[i];

            if (entry.weight <= 0f)
            {
                LogError($"Invalid weight at index {i}: {entry.weight}. Weight must be greater than 0");
                return false;
            }

            if (entry.type == SpawnObjectType.None)
            {
                LogError($"Invalid type at index {i}: SpawnObjectType.None is not allowed");
                return false;
            }

            if (!uniqueTypes.Add(entry.type))
            {
                LogError($"Duplicate type found: {entry.type}");
                return false;
            }

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
        {
            LogError($"Total weight is {totalWeight}. Must be greater than 0");
            return false;
        }

        Log($"Weight validation passed. Total weight: {totalWeight:F3}");
        return true;
    }

    /// <summary>
    /// Spawner 참조 유효성 검증
    /// </summary>
    /// <returns>유효 여부</returns>
    private bool ValidateSpawner()
    {
        if (_randomSpawner == null)
        {
            LogError("RandomSpawner reference is null");
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