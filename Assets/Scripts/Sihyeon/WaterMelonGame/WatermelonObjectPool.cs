using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 수박 게임의 과일 오브젝트 풀링 시스템입니다.
/// 8종류의 과일을 각각 풀링하여 효율적으로 관리합니다.
/// 싱글톤 패턴으로 구현되었습니다.
/// </summary>
public class WatermelonObjectPool : MonoBehaviour
{
    [Header("Fruit Prefabs")]
    [Tooltip("포도 프리팹 (0단계)")]
    [SerializeField] private GameObject grapePrefab;

    [Tooltip("사과 프리팹 (1단계)")]
    [SerializeField] private GameObject applePrefab;

    [Tooltip("오렌지 프리팹 (2단계)")]
    [SerializeField] private GameObject orangePrefab;

    [Tooltip("레몬 프리팹 (3단계)")]
    [SerializeField] private GameObject lemonPrefab;

    [Tooltip("멜론 프리팹 (4단계)")]
    [SerializeField] private GameObject melonPrefab;

    [Tooltip("두리안 프리팹 (5단계)")]
    [SerializeField] private GameObject durianPrefab;

    [Tooltip("수박 프리팹 (6단계)")]
    [SerializeField] private GameObject watermelonPrefab;

    [Tooltip("폭탄 프리팹 (7단계, 최종)")]
    [SerializeField] private GameObject bombPrefab;

    [Header("Pool Settings")]
    [Tooltip("각 과일 타입별 초기 생성 개수입니다.")]
    [SerializeField] private int initialPoolSizePerType = 5;

    [Tooltip("풀 크기 자동 확장 여부입니다.")]
    [SerializeField] private bool autoExpand = true;

    [Tooltip("각 타입별 최대 풀 크기입니다. (0이면 무제한)")]
    [SerializeField] private int maxPoolSizePerType = 20;

    [Header("Organization")]
    [Tooltip("풀링된 오브젝트들의 부모 Transform입니다.")]
    [SerializeField] private Transform poolParent;

    [Header("Debug")]
    [Tooltip("풀링 관련 디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;

    // 싱글톤 인스턴스
    private static WatermelonObjectPool instance;
    public static WatermelonObjectPool Instance => instance;

    // 과일 타입별 풀 컨테이너
    private Dictionary<FruitMergeData.FruitType, Queue<GameObject>> availableFruits;
    private Dictionary<FruitMergeData.FruitType, HashSet<GameObject>> activeFruits;

    // 프리팹 매핑
    private Dictionary<FruitMergeData.FruitType, GameObject> fruitPrefabs;

    /// <summary>
    /// 특정 타입의 활성 과일 개수를 반환합니다.
    /// </summary>
    public int GetActiveFruitCount(FruitMergeData.FruitType type)
    {
        return activeFruits != null && activeFruits.ContainsKey(type) ? activeFruits[type].Count : 0;
    }

    /// <summary>
    /// 특정 타입의 대기 중인 과일 개수를 반환합니다.
    /// </summary>
    public int GetAvailableFruitCount(FruitMergeData.FruitType type)
    {
        return availableFruits != null && availableFruits.ContainsKey(type) ? availableFruits[type].Count : 0;
    }

    /// <summary>
    /// 특정 타입의 전체 풀 크기를 반환합니다.
    /// </summary>
    public int GetTotalPoolSize(FruitMergeData.FruitType type)
    {
        return GetActiveFruitCount(type) + GetAvailableFruitCount(type);
    }

    /// <summary>
    /// 특정 타입의 프리팹을 반환합니다.
    /// </summary>
    public GameObject GetFruitPrefab(FruitMergeData.FruitType type)
    {
        if (fruitPrefabs != null && fruitPrefabs.ContainsKey(type))
        {
            return fruitPrefabs[type];
        }

        Debug.LogWarning($"[WatermelonObjectPool] {type} 프리팹을 찾을 수 없습니다. Dictionary가 초기화되지 않았거나 해당 키가 없습니다.");
        return null;
    }

    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[WatermelonObjectPool] 중복된 인스턴스 감지! {gameObject.name}를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 풀 부모 설정
        if (poolParent == null)
        {
            GameObject poolParentObj = new GameObject("FruitPool");
            poolParent = poolParentObj.transform;
            poolParent.SetParent(transform);
        }

        // 초기화
        InitializePools();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 과일 풀을 초기화합니다.
    /// </summary>
    private void InitializePools()
    {
        // 컨테이너 초기화
        availableFruits = new Dictionary<FruitMergeData.FruitType, Queue<GameObject>>();
        activeFruits = new Dictionary<FruitMergeData.FruitType, HashSet<GameObject>>();

        // 프리팹 매핑
        fruitPrefabs = new Dictionary<FruitMergeData.FruitType, GameObject>
        {
            { FruitMergeData.FruitType.Grape, grapePrefab },
            { FruitMergeData.FruitType.Apple, applePrefab },
            { FruitMergeData.FruitType.Orange, orangePrefab },
            { FruitMergeData.FruitType.Lemon, lemonPrefab },
            { FruitMergeData.FruitType.Melon, melonPrefab },
            { FruitMergeData.FruitType.Durian, durianPrefab },
            { FruitMergeData.FruitType.Watermelon, watermelonPrefab },
            { FruitMergeData.FruitType.Bomb, bombPrefab }
        };

        // 각 타입별 풀 초기화
        foreach (var kvp in fruitPrefabs)
        {
            FruitMergeData.FruitType type = kvp.Key;
            GameObject prefab = kvp.Value;

            // 프리팹 검증
            if (prefab == null)
            {
                Debug.LogError($"[WatermelonObjectPool] {type} 프리팹이 할당되지 않았습니다!");
                continue;
            }

            // FruitMergeData 컴포넌트 확인
            if (prefab.GetComponent<FruitMergeData>() == null)
            {
                Debug.LogError($"[WatermelonObjectPool] {type} 프리팹에 FruitMergeData 컴포넌트가 없습니다!");
                continue;
            }

            // 풀 생성
            availableFruits[type] = new Queue<GameObject>();
            activeFruits[type] = new HashSet<GameObject>();

            // 초기 과일 생성
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                CreateNewFruit(type);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[WatermelonObjectPool] {type} 풀 초기화 완료: {initialPoolSizePerType}개 생성");
            }
        }

        Debug.Log($"[WatermelonObjectPool] 전체 풀 초기화 완료: 총 {fruitPrefabs.Count}종류, 각 {initialPoolSizePerType}개씩 생성");
    }

    /// <summary>
    /// 새로운 과일을 생성하고 풀에 추가합니다.
    /// </summary>
    private GameObject CreateNewFruit(FruitMergeData.FruitType type)
    {
        if (!fruitPrefabs.ContainsKey(type) || fruitPrefabs[type] == null)
        {
            Debug.LogError($"[WatermelonObjectPool] {type} 프리팹을 찾을 수 없습니다!");
            return null;
        }

        GameObject fruitObj = Instantiate(fruitPrefabs[type], poolParent);
        fruitObj.name = $"{type}_{GetTotalPoolSize(type)}";
        fruitObj.SetActive(false);

        availableFruits[type].Enqueue(fruitObj);

        return fruitObj;
    }

    /// <summary>
    /// 풀에서 과일을 가져옵니다.
    /// </summary>
    /// <param name="type">과일 타입</param>
    /// <param name="position">생성 위치 (월드 좌표)</param>
    /// <returns>생성된 과일 GameObject (실패 시 null)</returns>
    public GameObject GetFruit(FruitMergeData.FruitType type, Vector3 position)
    {
        if (!availableFruits.ContainsKey(type))
        {
            Debug.LogError($"[WatermelonObjectPool] {type} 풀이 초기화되지 않았습니다!");
            return null;
        }

        GameObject fruitObj = null;

        // 사용 가능한 과일이 있으면 가져오기
        if (availableFruits[type].Count > 0)
        {
            fruitObj = availableFruits[type].Dequeue();
        }
        // 자동 확장이 가능하면 새로 생성
        else if (autoExpand && (maxPoolSizePerType == 0 || GetTotalPoolSize(type) < maxPoolSizePerType))
        {
            fruitObj = CreateNewFruit(type);

            if (showDebugLogs)
            {
                Debug.Log($"[WatermelonObjectPool] {type} 풀 확장: 새 과일 생성 (현재 크기: {GetTotalPoolSize(type)})");
            }
        }
        else
        {
            Debug.LogWarning($"[WatermelonObjectPool] {type} 풀에 사용 가능한 과일이 없습니다!");
            return null;
        }

        if (fruitObj == null)
        {
            Debug.LogError($"[WatermelonObjectPool] {type} 과일 생성에 실패했습니다!");
            return null;
        }

        // 부모를 null로 설정하여 월드 좌표계 사용
        fruitObj.transform.SetParent(null);

        // FruitMergeData를 통한 활성화
        FruitMergeData fruitData = fruitObj.GetComponent<FruitMergeData>();
        if (fruitData != null)
        {
            fruitData.Activate(position);
        }
        else
        {
            // FruitMergeData가 없으면 수동 활성화
            fruitObj.transform.position = position;
            fruitObj.SetActive(true);

            Debug.LogWarning($"[WatermelonObjectPool] {fruitObj.name}에 FruitMergeData 컴포넌트가 없습니다!");
        }

        // 활성 풀에 추가
        activeFruits[type].Add(fruitObj);

        if (showDebugLogs)
        {
            Debug.Log($"[WatermelonObjectPool] {type} 가져오기: {fruitObj.name} (위치: {position}) | " +
                     $"활성: {GetActiveFruitCount(type)}, 대기: {GetAvailableFruitCount(type)}");
        }

        return fruitObj;
    }

    /// <summary>
    /// 과일을 풀로 반환합니다.
    /// </summary>
    /// <param name="fruitObj">반환할 과일 GameObject</param>
    public void ReturnFruit(GameObject fruitObj)
    {
        if (fruitObj == null)
        {
            Debug.LogWarning("[WatermelonObjectPool] null 과일을 반환하려고 했습니다.");
            return;
        }

        // FruitMergeData로 타입 확인
        FruitMergeData fruitData = fruitObj.GetComponent<FruitMergeData>();
        if (fruitData == null)
        {
            Debug.LogWarning($"[WatermelonObjectPool] {fruitObj.name}에 FruitMergeData 컴포넌트가 없습니다!");
            Destroy(fruitObj);
            return;
        }

        FruitMergeData.FruitType type = fruitData.CurrentFruitType;

        if (!activeFruits.ContainsKey(type) || !activeFruits[type].Contains(fruitObj))
        {
            Debug.LogWarning($"[WatermelonObjectPool] {fruitObj.name}은(는) {type} 활성 풀에 없습니다.");
            return;
        }

        // 과일 비활성화
        fruitData.Deactivate();
        fruitObj.transform.SetParent(poolParent);

        // 풀로 이동
        activeFruits[type].Remove(fruitObj);
        availableFruits[type].Enqueue(fruitObj);

        if (showDebugLogs)
        {
            Debug.Log($"[WatermelonObjectPool] {type} 반환: {fruitObj.name} | " +
                     $"활성: {GetActiveFruitCount(type)}, 대기: {GetAvailableFruitCount(type)}");
        }
    }

    /// <summary>
    /// 특정 타입의 모든 활성 과일을 풀로 반환합니다.
    /// </summary>
    public void ReturnAllFruits(FruitMergeData.FruitType type)
    {
        if (!activeFruits.ContainsKey(type))
        {
            Debug.LogWarning($"[WatermelonObjectPool] {type} 풀이 존재하지 않습니다.");
            return;
        }

        // 복사본으로 순회 (컬렉션 수정 방지)
        List<GameObject> fruitsToReturn = new List<GameObject>(activeFruits[type]);

        foreach (var fruit in fruitsToReturn)
        {
            ReturnFruit(fruit);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WatermelonObjectPool] {type} 모든 과일 반환 완료: {fruitsToReturn.Count}개");
        }
    }

    /// <summary>
    /// 모든 타입의 활성 과일을 풀로 반환합니다.
    /// </summary>
    public void ReturnAllFruits()
    {
        int totalReturned = 0;

        foreach (var type in System.Enum.GetValues(typeof(FruitMergeData.FruitType)))
        {
            FruitMergeData.FruitType fruitType = (FruitMergeData.FruitType)type;
            int count = GetActiveFruitCount(fruitType);
            ReturnAllFruits(fruitType);
            totalReturned += count;
        }

        Debug.Log($"[WatermelonObjectPool] 모든 과일 반환 완료: 총 {totalReturned}개");
    }

    /// <summary>
    /// 풀을 완전히 초기화합니다.
    /// </summary>
    public void ClearPool()
    {
        // 모든 과일 반환
        ReturnAllFruits();

        // 모든 과일 파괴
        foreach (var kvp in availableFruits)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject fruit = kvp.Value.Dequeue();
                if (fruit != null)
                {
                    Destroy(fruit);
                }
            }
        }

        availableFruits.Clear();
        activeFruits.Clear();

        Debug.Log("[WatermelonObjectPool] 풀 초기화 완료");
    }

    /// <summary>
    /// 원래 타입을 명시적으로 지정하여 과일을 풀로 반환합니다.
    /// (병합 중 타입이 변경된 경우 사용)
    /// </summary>
    /// <param name="fruitObj">반환할 과일 GameObject</param>
    /// <param name="originalType">오브젝트가 원래 속했던 타입</param>
    public void ReturnFruitByOriginalType(GameObject fruitObj, FruitMergeData.FruitType originalType)
    {
        if (fruitObj == null)
        {
            Debug.LogWarning("[WatermelonObjectPool] null 과일을 반환하려고 했습니다.");
            return;
        }

        // FruitMergeData 확인
        FruitMergeData fruitData = fruitObj.GetComponent<FruitMergeData>();
        if (fruitData == null)
        {
            Debug.LogWarning($"[WatermelonObjectPool] {fruitObj.name}에 FruitMergeData 컴포넌트가 없습니다!");
            Destroy(fruitObj);
            return;
        }

        // 현재 타입과 원래 타입 비교
        FruitMergeData.FruitType currentType = fruitData.CurrentFruitType;

        if (currentType != originalType)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[WatermelonObjectPool] 타입 불일치 감지: {fruitObj.name} " +
                               $"(현재: {currentType}, 원래: {originalType}) - 원래 타입으로 반환");
            }
        }

        // 원래 타입의 활성 풀에서 제거
        if (!activeFruits.ContainsKey(originalType))
        {
            Debug.LogError($"[WatermelonObjectPool] {originalType} 풀이 존재하지 않습니다!");
            return;
        }

        if (!activeFruits[originalType].Contains(fruitObj))
        {
            Debug.LogWarning($"[WatermelonObjectPool] {fruitObj.name}은(는) {originalType} 활성 풀에 없습니다. " +
                            $"(현재 타입: {currentType})");

            // 현재 타입 풀에서도 확인
            if (currentType != originalType && activeFruits.ContainsKey(currentType) &&
                activeFruits[currentType].Contains(fruitObj))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[WatermelonObjectPool] {currentType} 풀에서 발견됨. 해당 풀에서 제거합니다.");
                }
                activeFruits[currentType].Remove(fruitObj);
            }
            else
            {
                return;
            }
        }
        else
        {
            // 원래 타입 풀에서 제거
            activeFruits[originalType].Remove(fruitObj);
        }

        // 과일 비활성화
        fruitData.Deactivate();
        fruitObj.transform.SetParent(poolParent);

        // 원래 타입의 대기 풀에 추가
        availableFruits[originalType].Enqueue(fruitObj);

        if (showDebugLogs)
        {
            Debug.Log($"[WatermelonObjectPool] {originalType} 반환: {fruitObj.name} | " +
                     $"활성: {GetActiveFruitCount(originalType)}, 대기: {GetAvailableFruitCount(originalType)}");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (initialPoolSizePerType < 0)
        {
            initialPoolSizePerType = 0;
            Debug.LogWarning("[WatermelonObjectPool] 초기 풀 크기는 0 이상이어야 합니다.");
        }

        if (maxPoolSizePerType < 0)
        {
            maxPoolSizePerType = 0;
        }

        if (maxPoolSizePerType > 0 && initialPoolSizePerType > maxPoolSizePerType)
        {
            initialPoolSizePerType = maxPoolSizePerType;
            Debug.LogWarning("[WatermelonObjectPool] 초기 풀 크기가 최대 풀 크기보다 큽니다. 조정되었습니다.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || availableFruits == null) return;

        // 풀 상태 시각화
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        string poolInfo = "=== Fruit Pool Status ===\n";

        foreach (var type in System.Enum.GetValues(typeof(FruitMergeData.FruitType)))
        {
            FruitMergeData.FruitType fruitType = (FruitMergeData.FruitType)type;
            int active = GetActiveFruitCount(fruitType);
            int available = GetAvailableFruitCount(fruitType);
            poolInfo += $"{fruitType}: {active} active, {available} available\n";
        }

        UnityEditor.Handles.Label(labelPos, poolInfo);
    }
#endif
}