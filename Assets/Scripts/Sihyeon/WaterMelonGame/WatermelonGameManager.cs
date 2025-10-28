using UnityEngine;
using System.Collections;

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
    [SerializeField] private bool showDebugLogs = false;
    
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
    
    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[WatermelonGameManager] 중복된 인스턴스 감지! {gameObject.name}를 파괴합니다.");
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
            Debug.LogError("[WatermelonGameManager] WatermelonObjectPool을 찾을 수 없습니다!");
            return;
        }
        
        // SpawnManager 초기화
        if (spawnManager == null)
        {
            spawnManager = GetComponent<SpawnManager>();
            
            if (spawnManager == null)
            {
                Debug.LogError("[WatermelonGameManager] SpawnManager를 찾을 수 없습니다!");
                return;
            }
        }
        
        spawnManager.Initialize(objectPool);
        
        Debug.Log("[WatermelonGameManager] 게임 매니저 초기화 완료 (3D 모드, 8단계)");
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
            Debug.LogError("[WatermelonGameManager] SpawnManager가 null입니다!");
            return;
        }
        
        GameObject fruit = spawnManager.SpawnRandomFruit();
        
        if (fruit != null && showDebugLogs)
        {
            Debug.Log($"[WatermelonGameManager] 과일 생성 성공: {fruit.name}");
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
}