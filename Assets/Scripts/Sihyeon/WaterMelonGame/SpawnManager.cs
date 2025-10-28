using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 수박 게임의 과일 스폰 전담 매니저입니다.
/// 스폰 위치 계산, 검증, 과일 생성을 담당합니다.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("과일이 생성될 위치입니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("스폰 지점의 기본 높이입니다. (Y축)")]
    [SerializeField] private float spawnHeight = 10f;

    [Tooltip("스폰 가능한 X축 범위입니다. (-range ~ +range)")]
    [SerializeField] private float spawnRangeX = 3f;

    [Tooltip("스폰 가능한 Z축 범위입니다. (-range ~ +range)")]
    [SerializeField] private float spawnRangeZ = 3f;

    [Tooltip("생성할 과일 타입 범위입니다. (Grape ~ 설정한 타입까지만 생성)")]
    [SerializeField] private FruitMergeData.FruitType maxSpawnFruitType = FruitMergeData.FruitType.Lemon;

    [Header("Spawn Validation Settings")]
    [Tooltip("스폰 위치 검증 시 과일 간 최소 여유 거리입니다.")]
    [SerializeField] private float spawnMargin = 0.2f;

    [Tooltip("스폰 위치 찾기 최대 시도 횟수입니다.")]
    [SerializeField] private int maxSpawnAttempts = 10;

    [Tooltip("스폰 위치 검증을 수행합니다. (false면 검증 없이 즉시 스폰)")]
    [SerializeField] private bool enableSpawnValidation = true;

    [Header("Spawn Frequency Settings")]
    [Tooltip("스폰 단계별 설정 (spawnCount 범위에 따라 확률 조정)")]
    [SerializeField]
    private List<SpawnStage> spawnStages = new List<SpawnStage>
    {
        new SpawnStage { minCount = 0, maxCount = 15, fruitProbabilities = new List<FruitProbability>
            { new FruitProbability { type = FruitMergeData.FruitType.Grape, probability = 0.7f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.25f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.25f },
              new FruitProbability { type = FruitMergeData.FruitType.Orange, probability = 0.05f },
              new FruitProbability { type = FruitMergeData.FruitType.Orange, probability = 0.05f } } },
        new SpawnStage { minCount = 15, maxCount = 30, fruitProbabilities = new List<FruitProbability>
            { new FruitProbability { type = FruitMergeData.FruitType.Grape, probability = 0.5f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Orange, probability = 0.2f },
              new FruitProbability { type = FruitMergeData.FruitType.Orange, probability = 0.05f } } },
        new SpawnStage { minCount = 30, maxCount = int.MaxValue, fruitProbabilities = new List<FruitProbability>
            { new FruitProbability { type = FruitMergeData.FruitType.Grape, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Apple, probability = 0.3f },
              new FruitProbability { type = FruitMergeData.FruitType.Orange, probability = 0.4f } } }
    };

    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;

    [Tooltip("스폰 지점과 범위를 Gizmo로 표시합니다.")]
    [SerializeField] private bool showGizmos = true;

    // 마지막으로 로그를 출력한 마일스톤 값 (중복 출력 방지용)
    private int lastLoggedMilestone = 0;

    // 이전에 생성한 과일 타입 (연속 생성 방지용)
    private FruitMergeData.FruitType? lastSpawnedFruitType;

    // 이전 스폰 단계 (스테이지 변경 감지용)
    private SpawnStage previousStage;

    // 참조
    private WatermelonObjectPool objectPool;

    public void Initialize(WatermelonObjectPool pool)
    {
        objectPool = pool;
        lastSpawnedFruitType = null;  // 초기화
        previousStage = null;  // 초기화

        if (spawnPoint == null)
        {
            Debug.LogWarning("[SpawnManager] SpawnPoint가 설정되지 않았습니다. 매니저 위치를 사용합니다.");
            spawnPoint = transform;
        }

        Debug.Log("[SpawnManager] 스폰 매니저 초기화 완료");
    }

    /// <summary>
    /// 현재 스폰된 총 과일 개수를 반환합니다.
    /// </summary>
    public int SpawnCount
    {
        get
        {
            if (objectPool == null) return 0;

            int total = 0;
            foreach (FruitMergeData.FruitType type in System.Enum.GetValues(typeof(FruitMergeData.FruitType)))
            {
                total += objectPool.GetActiveFruitCount(type);
            }

            // SpawnCount가 증가하여 5의 배수가 되었을 때 로그 출력
            LogSpawnCountMilestone(total);

            return total;
        }
    }

    /// <summary>
    /// 랜덤한 위치에 랜덤한 과일을 생성합니다.
    /// spawnCount에 따라 낮은 단계 과일 빈도 조정.
    /// 이전에 생성한 과일과 동일한 타입이면 최대 10번까지 재시도합니다.
    /// </summary>
    public GameObject SpawnRandomFruit()
    {
        if (objectPool == null)
        {
            Debug.LogError("[SpawnManager] ObjectPool이 null입니다!");
            return null;
        }

        FruitMergeData.FruitType selectedType;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            selectedType = GetWeightedRandomFruitType();
            attempts++;
        } while (selectedType == lastSpawnedFruitType && attempts < maxAttempts);

        lastSpawnedFruitType = selectedType;

        return SpawnFruit(selectedType);
    }

    /// <summary>
    /// spawnCount에 따라 가중치가 적용된 랜덤 과일 타입을 반환합니다.
    /// maxSpawnFruitType을 고려하여 필터링합니다.
    /// </summary>
    private FruitMergeData.FruitType GetWeightedRandomFruitType()
    {
        // spawnCount에 맞는 단계 찾기
        SpawnStage currentStage = null;
        foreach (var stage in spawnStages)
        {
            if (SpawnCount >= stage.minCount && SpawnCount < stage.maxCount)
            {
                currentStage = stage;
                Debug.Log($"[SpawnManager] 현재 스폰 단계: {stage.minCount} ~ {stage.maxCount} (spawnCount: {SpawnCount})");
                break;
            }
        }

        // 스테이지 변경 감지 및 경고 로그
        if (previousStage != currentStage)
        {
            if (previousStage != null)
            {
                Debug.LogWarning($"[SpawnManager] 스폰 단계가 변경되었습니다! 이전: {previousStage.minCount} ~ {previousStage.maxCount}, 현재: {currentStage?.minCount ?? 0} ~ {currentStage?.maxCount ?? 0} (spawnCount: {SpawnCount})");
            }
            previousStage = currentStage;
        }

        if (currentStage == null || currentStage.fruitProbabilities.Count == 0)
        {
            // fallback (균등)
            return FruitMergeData.FruitType.Apple;
        }

        // maxSpawnFruitType 이하의 확률만 필터링
        List<FruitProbability> filteredProbabilities = new List<FruitProbability>();
        foreach (var prob in currentStage.fruitProbabilities)
        {
            if ((int)prob.type <= (int)maxSpawnFruitType)
            {
                filteredProbabilities.Add(prob);
            }
        }

        if (filteredProbabilities.Count == 0)
        {
            // 필터링 후 확률이 없으면 fallback
            return FruitMergeData.FruitType.Apple;
        }

        // 확률 합산
        float totalWeight = 0f;
        foreach (var prob in filteredProbabilities)
        {
            totalWeight += prob.probability;
        }

        // 랜덤 선택
        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var prob in filteredProbabilities)
        {
            cumulative += prob.probability;
            if (randomValue <= cumulative)
            {
                return prob.type;
            }
        }

        // fallback
        return FruitMergeData.FruitType.Apple;
    }

    /// <summary>
    /// 특정 타입의 과일을 생성합니다.
    /// </summary>
    public GameObject SpawnFruit(FruitMergeData.FruitType fruitType)
    {
        if (objectPool == null)
        {
            Debug.LogError("[SpawnManager] ObjectPool이 null입니다!");
            return null;
        }

        Vector3 spawnPos = Vector3.zero;
        bool foundValidPosition = false;

        if (enableSpawnValidation)
        {
            float fruitRadius = GetFruitRadius(fruitType);

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 candidatePos = GetRandomSpawnPosition();

                if (IsValidSpawnPosition(candidatePos, fruitRadius))
                {
                    spawnPos = candidatePos;
                    foundValidPosition = true;

                    if (showDebugLogs)
                    {
                        Debug.Log($"[SpawnManager] 유효한 스폰 위치 찾음 (시도 {attempt + 1}회): {spawnPos}");
                    }
                    break;
                }
            }

            if (!foundValidPosition)
            {
                // 검증 실패 시에도 랜덤 위치에 생성
                spawnPos = GetRandomSpawnPosition();
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[SpawnManager] {maxSpawnAttempts}회 시도 후에도 유효한 스폰 위치를 찾지 않았습니다. 랜덤 위치에 생성: {spawnPos}");
                }
            }
        }
        else
        {
            spawnPos = GetRandomSpawnPosition();
            foundValidPosition = true;
        }

        GameObject fruit = objectPool.GetFruit(fruitType, spawnPos);

        // 스폰된 과일에 면역 설정 (처음 생성 시에만 적용)
        if (fruit != null)
        {
            FruitMergeData fruitData = fruit.GetComponent<FruitMergeData>();
            if (fruitData != null)
            {
                fruitData.SetMergeImmunity(0.5f);  // 0.5초 면역 적용
            }
        }

        if (fruit != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {fruitType} 생성 완료 at {spawnPos}");
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnManager] {fruitType} 생성 실패!");
        }

        return fruit;
    }

    /// <summary>
    /// 특정 과일 타입의 반경을 반환합니다.
    /// </summary>
    private float GetFruitRadius(FruitMergeData.FruitType type)
    {
        if (objectPool == null)
        {
            Debug.LogWarning("[SpawnManager] ObjectPool이 null입니다. 기본 반경(0.5f) 사용.");
            return 0.5f;
        }

        GameObject prefab = objectPool.GetFruitPrefab(type);

        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnManager] {type} 프리팹을 찾을 수 없습니다. 기본 반경(0.5f) 사용.");
            return 0.5f;
        }

        SphereCollider sphereCollider = prefab.GetComponent<SphereCollider>();

        if (sphereCollider != null)
        {
            float maxScale = Mathf.Max(prefab.transform.localScale.x,
                                       prefab.transform.localScale.y,
                                       prefab.transform.localScale.z);
            float actualRadius = sphereCollider.radius * maxScale;

            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {type} 반경: {actualRadius:F2}");
            }

            return actualRadius;
        }

        Collider collider = prefab.GetComponent<Collider>();

        if (collider != null)
        {
            float radius = Mathf.Max(collider.bounds.extents.x,
                                     collider.bounds.extents.y,
                                     collider.bounds.extents.z);

            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {type} 반경 (Bounds): {radius:F2}");
            }

            return radius;
        }

        Debug.LogWarning($"[SpawnManager] {type} 프리팹에 Collider가 없습니다. 기본 반경(0.5f) 사용.");
        return 0.5f;
    }

    /// <summary>
    /// 지정된 위치가 스폰하기에 유효한지 검증합니다.
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 position, float fruitRadius)
    {
        FruitMergeData[] activeFruits = FindObjectsByType<FruitMergeData>(FindObjectsSortMode.None);

        foreach (var fruit in activeFruits)
        {
            if (!fruit.gameObject.activeSelf) continue;

            Vector3 fruitPos = fruit.transform.position;
            float distanceXZ = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(fruitPos.x, fruitPos.z)
            );

            float otherRadius = GetFruitRadius(fruit.CurrentFruitType);
            float minDistance = fruitRadius + otherRadius + spawnMargin;

            if (distanceXZ < minDistance)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[SpawnManager] 스폰 위치 검증 실패: {fruit.CurrentFruitType}와 거리 {distanceXZ:F2} (최소 {minDistance:F2} 필요)");
                }
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 랜덤한 스폰 위치를 반환합니다.
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        return new Vector3(basePos.x + randomX, spawnHeight, basePos.z + randomZ);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnHeight <= 0f)
        {
            spawnHeight = 10f;
            Debug.LogWarning("[SpawnManager] spawnHeight는 0보다 커야 합니다.");
        }

        if (spawnRangeX < 0f) spawnRangeX = 3f;
        if (spawnRangeZ < 0f) spawnRangeZ = 3f;
        if (spawnMargin < 0f) spawnMargin = 0.2f;
        if (maxSpawnAttempts < 1) maxSpawnAttempts = 20;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        Gizmos.color = Color.green;
        Vector3 spawnPos = new Vector3(basePos.x, spawnHeight, basePos.z);
        Gizmos.DrawWireSphere(spawnPos, 0.5f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 boxSize = new Vector3(spawnRangeX * 2, 0.1f, spawnRangeZ * 2);
        Gizmos.DrawCube(spawnPos, boxSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnPos, boxSize);
    }
#endif

    /// <summary>
    /// 스폰 단계 설정 구조체
    /// </summary>
    [System.Serializable]
    public class SpawnStage
    {
        [Tooltip("최소 spawnCount")]
        public int minCount;
        [Tooltip("최대 spawnCount (미포함)")]
        public int maxCount;
        [Tooltip("과일 타입별 확률 (합계 1.0 권장)")]
        public List<FruitProbability> fruitProbabilities;
    }

    /// <summary>
    /// 과일 타입별 확률 설정 클래스
    /// </summary>
    [System.Serializable]
    public class FruitProbability
    {
        [Tooltip("과일 타입")]
        public FruitMergeData.FruitType type;
        [Tooltip("확률 값")]
        public float probability;
    }

    /// <summary>
    /// SpawnCount가 증가하여 5의 배수가 되었을 때 디버그 로그를 출력합니다.
    /// </summary>
    private void LogSpawnCountMilestone(int count)
    {
        if (count > lastLoggedMilestone && count % 5 == 0)
        {
            Debug.Log($"[SpawnManager] SpawnCount 마일스톤 도달: {count}개 (5의 배수)");
            lastLoggedMilestone = count;
        }
    }
}