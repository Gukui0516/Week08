using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject 프리팹 기반 범용 풀링 시스템 (제네릭)
/// </summary>
[Serializable]
public class TypedObjectPool<TEnum> where TEnum : Enum
{
    [Serializable]
    public struct PrefabMapEntry
    {
        public TEnum type;
        public GameObject prefab;

        [Header("Pool Settings")]
        [InfoBox("이 타입의 개별 풀 사이즈. 0이면 기본값 사용")]
        public int individualPoolSize;
    }

    #region Serialized Fields
    [Header("Pool Settings")]
    [SerializeField] private int _defaultPoolSize = 50;

    [Header("Prefab Mapping")]
    [Required]
    [InfoBox("각 타입에 해당하는 프리팹과 개별 풀 사이즈를 설정하세요")]
    [SerializeField] private PrefabMapEntry[] _prefabEntries;

    [Header("Debug Settings")]
    [SerializeField] private bool _isDebugLog = false;
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }

    [ShowInInspector, ReadOnly]
    public Dictionary<TEnum, int> PoolCounts => GetPoolCounts();

    [ShowInInspector, ReadOnly]
    public Dictionary<TEnum, int> ActiveObjectCounts => _activeObjects;

    [ShowInInspector, ReadOnly]
    public int TotalActiveObjectCounts => _activeObjectsWithTime?.Count ?? 0;

    [ShowInInspector, ReadOnly]
    public int RegisteredPrefabCount => _prefabMap?.Count ?? 0;
    #endregion

    #region Private Fields
    private Dictionary<TEnum, GameObject> _prefabMap;
    private Dictionary<TEnum, Queue<GameObject>> _prefabPools;
    private Transform _poolParentTransform;
    private Dictionary<TEnum, int> _poolCreations = new Dictionary<TEnum, int>();
    private Dictionary<TEnum, Transform> _typeParentTransforms = new Dictionary<TEnum, Transform>();
    private Dictionary<TEnum, int> _activeObjects = new Dictionary<TEnum, int>();
    private Dictionary<GameObject, float> _activeObjectsWithTime = new Dictionary<GameObject, float>();
    #endregion

    #region Public Methods - Pool Management
    /// <summary>
    /// 풀 초기화
    /// </summary>
    /// <param name="poolParent">풀링된 오브젝트들의 부모 Transform</param>
    public void Initialize(Transform poolParent = null)
    {
        if (IsInitialized)
        {
            LogWarning("[TypedObjectPool] Already initialized");
            return;
        }

        if (poolParent == null)
        {
            GameObject rootObject = new GameObject($"PooledObjects_{typeof(TEnum).Name}");
            _poolParentTransform = rootObject?.transform;
        }
        else
        {
            _poolParentTransform = poolParent;
        }

        InitializeDictionaries();
        InitializeFromPrefabMap();
        CreateTypeParentTransforms();
        IsInitialized = true;

        foreach (var pair in _prefabMap)
        {
            _poolCreations.Add(pair.Key, 0);
            _activeObjects.Add(pair.Key, 0);

            int poolSize = GetPoolSizeForType(pair.Key);
            PrewarmPool(pair.Key, poolSize);
        }
        Log($"[TypedObjectPool<{typeof(TEnum).Name}>] Initialization completed");
    }

    /// <summary>
    /// 모든 활성 오브젝트를 강제로 풀에 반환
    /// </summary>
    /// <returns>반환된 오브젝트 개수</returns>
    public int ReturnAllActiveObjects()
    {
        if (_activeObjectsWithTime == null || _activeObjectsWithTime.Count == 0)
        {
            Log("No active objects to return");
            return 0;
        }

        List<GameObject> activeObjects = new List<GameObject>(_activeObjectsWithTime.Keys);
        int returnedCount = activeObjects.Count;

        foreach (var obj in activeObjects)
        {
            if (obj != null)
            {
                ReturnObject(obj);
            }
        }

        Log($"Returned {returnedCount} active objects to pool", forcely: true);
        return returnedCount;
    }

    /// <summary>
    /// 지정된 타입의 오브젝트를 풀에서 가져옵니다
    /// </summary>
    /// <param name="prefabType">타입</param>
    /// <returns>활성화된 GameObject 인스턴스</returns>
    public GameObject GetObject(TEnum prefabType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[TypedObjectPool] Pool not initialized");
            return null;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        LogWarning($"[TypedObjectPool] Pool empty for type: {prefabType}. Expanding pool dynamically.");
        ExpandPool(prefabType);

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        Debug.LogError($"[TypedObjectPool] Failed to expand pool for type: {prefabType}. Creating single object.");
        return CreateNewObject(prefabType);
    }

    /// <summary>
    /// 지정된 타입의 오브젝트를 풀에서 가져옵니다
    /// 풀이 비어있으면 가장 오래된 활성 오브젝트를 회수합니다
    /// </summary>
    /// <param name="prefabType">타입</param>
    /// <returns>활성화된 GameObject 인스턴스</returns>
    public GameObject GetObjectForcely(TEnum prefabType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[TypedObjectPool] Pool not initialized");
            return null;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        GameObject recycledObject = RecycleOldestObject(prefabType);
        if (recycledObject != null)
        {
            LogWarning($"[TypedObjectPool] Pool empty for type: {prefabType}. Reused oldest object.");
            return recycledObject;
        }

        LogWarning($"[TypedObjectPool] Pool empty for type: {prefabType}. Expanding pool dynamically.", forcely:true);
        ExpandPool(prefabType);

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        Debug.LogError($"[TypedObjectPool] Failed to expand pool for type: {prefabType}. Creating single object.");
        return CreateNewObject(prefabType);
    }

    /// <summary>
    /// 오브젝트를 풀로 반환합니다
    /// </summary>
    /// <param name="obj">반환할 GameObject</param>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        TEnum prefabType = GetPrefabTypeFromGameObject(obj);

        _activeObjectsWithTime.Remove(obj);
        if (_activeObjects.ContainsKey(prefabType))
        {
            _activeObjects[prefabType] = Mathf.Max(0, _activeObjects[prefabType] - 1);
        }

        obj.SetActive(false);

        Transform targetParent = null;
        if (_typeParentTransforms.ContainsKey(prefabType) && _typeParentTransforms[prefabType] != null)
        {
            targetParent = _typeParentTransforms[prefabType];
        }
        else if (_poolParentTransform != null)
        {
            targetParent = _poolParentTransform;
            LogWarning($"[TypedObjectPool] Type parent not found for {prefabType}, using default parent");
        }

        if (targetParent != null && obj.transform.parent != targetParent)
        {
            obj.transform.SetParent(targetParent);
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);
        pool.Enqueue(obj);
    }

    /// <summary>
    /// 오브젝트를 지정된 위치와 방향으로 생성합니다
    /// </summary>
    /// <param name="prefabType">타입</param>
    /// <param name="worldPosition">생성 위치</param>
    /// <param name="worldRotation">생성 방향</param>
    /// <param name="isForcely">강제 생성 여부</param>
    /// <returns>생성된 GameObject 인스턴스</returns>
    public GameObject SpawnObject(TEnum prefabType, Vector3 worldPosition, Quaternion worldRotation, bool isForcely = false)
    {
        GameObject obj = isForcely ?
            GetObjectForcely(prefabType) : GetObject(prefabType);

        if (obj != null)
        {
            obj.transform.position = worldPosition;
            obj.transform.rotation = worldRotation;
            obj.SetActive(true);

            _activeObjectsWithTime[obj] = Time.time;
            if (!_activeObjects.ContainsKey(prefabType))
            {
                _activeObjects[prefabType] = 0;
            }
            _activeObjects[prefabType]++;
        }
        else
        {
            Debug.LogError($"[TypedObjectPool] Failed to Spawn Object!");
        }

        return obj;
    }

    /// <summary>
    /// 지정된 타입의 풀을 미리 생성합니다
    /// </summary>
    /// <param name="prefabType">타입</param>
    /// <param name="count">생성할 개수</param>
    public void PrewarmPool(TEnum prefabType, int count)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[TypedObjectPool] Pool not initialized");
            return;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        for (int i = 0; i < count; i++)
        {
            GameObject newObject = CreateNewObject(prefabType);
            if (newObject != null)
            {
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }

        Log($"[TypedObjectPool] Prewarmed {count} instances of {prefabType}");
    }
    #endregion

    #region Public Methods - Query
    /// <summary>
    /// 현재 활성화된 오브젝트 개수 반환
    /// </summary>
    /// <returns>활성 오브젝트 개수</returns>
    public int GetActiveObjectCount()
    {
        return _activeObjectsWithTime.Count;
    }

    /// <summary>
    /// 현재 활성화된 모든 오브젝트 목록 반환
    /// </summary>
    /// <returns>활성 오브젝트 목록 (읽기 전용)</returns>
    public IEnumerable<GameObject> GetActiveObjects()
    {
        return _activeObjectsWithTime.Keys;
    }

    /// <summary>
    /// 특정 타입의 활성화된 오브젝트 개수 반환
    /// </summary>
    /// <param name="prefabType">조회할 타입</param>
    /// <returns>해당 타입의 활성 오브젝트 개수</returns>
    public int GetActiveObjectCount(TEnum prefabType)
    {
        return _activeObjects.ContainsKey(prefabType) ? _activeObjects[prefabType] : 0;
    }

    /// <summary>
    /// 모든 풀 정리
    /// </summary>
    public void ClearAllPools()
    {
        if (_prefabPools == null) return;

        foreach (var pool in _prefabPools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        _prefabPools.Clear();
        _activeObjects.Clear();
        _activeObjectsWithTime.Clear();

        Log("All pools cleared");
    }
    #endregion

    #region Private Methods - Initialization
    /// <summary>
    /// Dictionary 초기화
    /// </summary>
    private void InitializeDictionaries()
    {
        _prefabPools = new Dictionary<TEnum, Queue<GameObject>>();
    }

    /// <summary>
    /// PrefabMapEntry 배열에서 Dictionary로 변환
    /// </summary>
    private void InitializeFromPrefabMap()
    {
        if (_prefabEntries == null)
        {
            LogWarning("[TypedObjectPool] Prefab entries array is null");
            return;
        }

        if (_prefabMap != null)
        {
            _prefabMap.Clear();
        }
        else
        {
            _prefabMap = new Dictionary<TEnum, GameObject>();
        }

        foreach (var entry in _prefabEntries)
        {
            if (entry.prefab == null)
            {
                LogWarning($"[TypedObjectPool] Null prefab found for type: {entry.type}");
                continue;
            }

            if (_prefabMap.ContainsKey(entry.type))
            {
                LogWarning($"[TypedObjectPool] Duplicate prefab type: {entry.type}. Overwriting.");
            }

            _prefabMap[entry.type] = entry.prefab;
            Log($"[TypedObjectPool] Registered {entry.type}: {entry.prefab.name}");
        }
    }

    /// <summary>
    /// 타입별 부모 Transform 생성
    /// </summary>
    private void CreateTypeParentTransforms()
    {
        if (_prefabMap == null || _poolParentTransform == null)
            return;

        foreach (var kvp in _prefabMap)
        {
            TEnum prefabType = kvp.Key;

            GameObject typeParent = new GameObject(prefabType.ToString());
            typeParent.transform.SetParent(_poolParentTransform);

            _typeParentTransforms[prefabType] = typeParent.transform;

            Log($"[TypedObjectPool] Created parent transform for {prefabType}");
        }
    }

    /// <summary>
    /// 특정 타입의 풀 사이즈 반환
    /// </summary>
    /// <param name="prefabType">조회할 타입</param>
    /// <returns>해당 타입의 풀 사이즈</returns>
    private int GetPoolSizeForType(TEnum prefabType)
    {
        if (_prefabEntries == null)
            return _defaultPoolSize;

        foreach (var entry in _prefabEntries)
        {
            if (EqualityComparer<TEnum>.Default.Equals(entry.type, prefabType))
            {
                return entry.individualPoolSize > 0 ? entry.individualPoolSize : _defaultPoolSize;
            }
        }

        return _defaultPoolSize;
    }
    #endregion

    #region Private Methods - Pool Management
    /// <summary>
    /// 동적 풀 확장 - 현재 풀 크기의 1.5배로 확장
    /// </summary>
    /// <param name="prefabType">확장할 타입</param>
    private void ExpandPool(TEnum prefabType)
    {
        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        int currentPoolSize = _poolCreations.ContainsKey(prefabType) ? _poolCreations[prefabType] : _defaultPoolSize;
        int expansionSize = Mathf.Max(5, Mathf.RoundToInt(currentPoolSize * 0.5f));

        Log($"Expanding pool for {prefabType}: +{expansionSize} objects (Current: {currentPoolSize})");

        for (int i = 0; i < expansionSize; i++)
        {
            GameObject newObject = CreateNewObject(prefabType);
            if (newObject != null)
            {
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }

        Log($"Pool expansion completed for {prefabType}. New pool size: {pool.Count}");
    }

    /// <summary>
    /// 지정된 타입의 풀을 가져오거나 새로 생성
    /// </summary>
    /// <param name="prefabType">타입</param>
    /// <returns>해당 타입의 풀</returns>
    private Queue<GameObject> GetOrCreatePool(TEnum prefabType)
    {
        if (!_prefabPools.ContainsKey(prefabType))
        {
            _prefabPools[prefabType] = new Queue<GameObject>();
        }
        return _prefabPools[prefabType];
    }

    /// <summary>
    /// 새 오브젝트 생성
    /// </summary>
    /// <param name="prefabType">생성할 타입</param>
    /// <returns>생성된 GameObject</returns>
    private GameObject CreateNewObject(TEnum prefabType)
    {
        if (!_prefabMap.ContainsKey(prefabType) || _prefabMap[prefabType] == null)
        {
            LogWarning($"No prefab registered for type: {prefabType}");
            return null;
        }

        GameObject prefab = _prefabMap[prefabType];
        GameObject newObject = UnityEngine.Object.Instantiate(prefab);

        if (!_poolCreations.ContainsKey(prefabType))
        {
            _poolCreations[prefabType] = 0;
        }
        newObject.name = prefab.name + ":" + (++_poolCreations[prefabType]).ToString();

        if (_typeParentTransforms.ContainsKey(prefabType) && _typeParentTransforms[prefabType] != null)
        {
            newObject.transform.SetParent(_typeParentTransforms[prefabType]);
        }
        else if (_poolParentTransform != null)
        {
            newObject.transform.SetParent(_poolParentTransform);
            LogWarning($"Type parent not found for {prefabType}, using default parent");
        }

        newObject.SetActive(false);

        return newObject;
    }

    /// <summary>
    /// 풀별 개체 수 반환
    /// </summary>
    /// <returns>타입별 풀 개체 수 딕셔너리</returns>
    private Dictionary<TEnum, int> GetPoolCounts()
    {
        var counts = new Dictionary<TEnum, int>();
        if (_prefabPools != null)
        {
            foreach (var kvp in _prefabPools)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
        }
        return counts;
    }

    /// <summary>
    /// GameObject로부터 타입 추론
    /// </summary>
    /// <param name="obj">분석할 GameObject</param>
    /// <returns>추론된 타입</returns>
    private TEnum GetPrefabTypeFromGameObject(GameObject obj)
    {
        if (obj == null || _prefabMap == null)
            return default(TEnum);

        string objName = obj.name;
        string prefabName = objName.Contains(":") ? objName.Split(':')[0] : objName;

        foreach (var kvp in _prefabMap)
        {
            if (kvp.Value != null && kvp.Value.name == prefabName)
            {
                return kvp.Key;
            }
        }

        LogWarning($"Could not determine type for {obj.name}. Defaulting to None.",true);
        return default(TEnum);
    }
    #endregion

    #region Private Methods - Pool Reuse
    /// <summary>
    /// 가장 오래된 활성 오브젝트를 강제 회수합니다
    /// </summary>
    /// <param name="prefabType">회수할 오브젝트 타입 (같은 타입 우선)</param>
    /// <returns>회수된 오브젝트, 없으면 null</returns>
    private GameObject RecycleOldestObject(TEnum prefabType)
    {
        if (_activeObjectsWithTime.Count == 0)
            return null;

        GameObject oldestSameType = null;
        float oldestSameTypeTime = float.MaxValue;

        GameObject oldestOverall = null;
        float oldestOverallTime = float.MaxValue;

        foreach (var kvp in _activeObjectsWithTime)
        {
            GameObject obj = kvp.Key;
            float spawnTime = kvp.Value;

            TEnum objType = GetPrefabTypeFromGameObject(obj);

            if (EqualityComparer<TEnum>.Default.Equals(objType, prefabType) && spawnTime < oldestSameTypeTime)
            {
                oldestSameType = obj;
                oldestSameTypeTime = spawnTime;
            }

            if (spawnTime < oldestOverallTime)
            {
                oldestOverall = obj;
                oldestOverallTime = spawnTime;
            }
        }

        GameObject targetObject = oldestSameType ?? oldestOverall;

        if (targetObject != null)
        {
            ReturnObject(targetObject);

            TEnum targetType = GetPrefabTypeFromGameObject(targetObject);
            Queue<GameObject> pool = GetOrCreatePool(targetType);
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return null;
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>
    /// 일반 로그 출력 (라임 색상)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if(forcely || _isDebugLog)
            Debug.Log($"<color=lime>[TypedObjectPool<{typeof(TEnum).Name}>] {message}</color>");
    }

    /// <summary>
    /// 경고 로그 출력 (라임 색상)
    /// </summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (forcely || _isDebugLog)
            Debug.LogWarning($"<color=lime>[TypedObjectPool<{typeof(TEnum).Name}>] {message}</color>");
    }
    #endregion
}