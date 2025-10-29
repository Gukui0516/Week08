using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 벽돌깨기 공의 오브젝트 풀링 시스템입니다.
/// 싱글톤 패턴으로 구현되었습니다.
/// </summary>
public class BrickBreakerBallPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("공 프리팹입니다. BrickBreakerBall 컴포넌트가 필요합니다.")]
    [SerializeField] private GameObject ballPrefab;
    
    [Tooltip("초기 생성할 공의 개수입니다.")]
    [SerializeField] private int initialPoolSize = 5;
    
    [Tooltip("풀 크기 자동 확장 여부입니다.")]
    [SerializeField] private bool autoExpand = true;
    
    [Tooltip("최대 풀 크기입니다. (0이면 무제한)")]
    [SerializeField] private int maxPoolSize = 20;
    
    [Header("Organization")]
    [Tooltip("풀링된 오브젝트들의 부모 Transform입니다.")]
    [SerializeField] private Transform poolParent;
    
    // 싱글톤 인스턴스
    private static BrickBreakerBallPool instance;
    public static BrickBreakerBallPool Instance => instance;
    
    // 풀 컨테이너
    private Queue<BrickBreakerBall> availableBalls = new Queue<BrickBreakerBall>();
    private HashSet<BrickBreakerBall> activeBalls = new HashSet<BrickBreakerBall>();
    
    /// <summary>
    /// 현재 활성화된 공의 개수를 반환합니다.
    /// </summary>
    public int ActiveBallCount => activeBalls.Count;
    
    /// <summary>
    /// 사용 가능한 공의 개수를 반환합니다.
    /// </summary>
    public int AvailableBallCount => availableBalls.Count;
    
    /// <summary>
    /// 전체 풀 크기를 반환합니다.
    /// </summary>
    public int TotalPoolSize => availableBalls.Count + activeBalls.Count;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[BrickBreakerBallPool] 중복된 인스턴스 감지! {gameObject.name}를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        // 풀 부모 설정
        if (poolParent == null)
        {
            GameObject poolParentObj = new GameObject("BallPool");
            poolParent = poolParentObj.transform;
            poolParent.SetParent(transform);
        }
        
        // 초기 풀 생성
        InitializePool();
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    /// <summary>
    /// 풀을 초기화하고 공들을 미리 생성합니다.
    /// </summary>
    private void InitializePool()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("[BrickBreakerBallPool] 공 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        // BrickBreakerBall 컴포넌트 확인
        if (ballPrefab.GetComponent<BrickBreakerBall>() == null)
        {
            Debug.LogError("[BrickBreakerBallPool] 공 프리팹에 BrickBreakerBall 컴포넌트가 없습니다!");
            return;
        }
        
        // 초기 공 생성
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBall();
        }
        
        Debug.Log($"[BrickBreakerBallPool] 풀 초기화 완료: {initialPoolSize}개의 공 생성");
    }
    
    /// <summary>
    /// 새로운 공을 생성하고 풀에 추가합니다.
    /// </summary>
    /// <returns>생성된 BrickBreakerBall 컴포넌트</returns>
    private BrickBreakerBall CreateNewBall()
    {
        GameObject ballObj = Instantiate(ballPrefab, poolParent);
        ballObj.SetActive(false);
        
        BrickBreakerBall ball = ballObj.GetComponent<BrickBreakerBall>();
        availableBalls.Enqueue(ball);
        
        return ball;
    }
    
    /// <summary>
    /// 풀에서 공을 가져옵니다.
    /// </summary>
    /// <param name="position">공의 초기 위치 (월드 좌표)</param>
    /// <param name="launchDirection">발사 방향 (정규화됨)</param>
    /// <returns>BrickBreakerBall 컴포넌트 (실패 시 null)</returns>
    public BrickBreakerBall GetBall(Vector3 position, Vector3 launchDirection)
    {
        BrickBreakerBall ball = null;
        
        // 사용 가능한 공이 있으면 가져오기
        if (availableBalls.Count > 0)
        {
            ball = availableBalls.Dequeue();
        }
        // 자동 확장이 가능하면 새로 생성
        else if (autoExpand && (maxPoolSize == 0 || TotalPoolSize < maxPoolSize))
        {
            ball = CreateNewBall();
            Debug.Log($"[BrickBreakerBallPool] 풀 확장: 새 공 생성 (현재 크기: {TotalPoolSize})");
        }
        else
        {
            Debug.LogWarning("[BrickBreakerBallPool] 사용 가능한 공이 없습니다!");
            return null;
        }
        
        // ✅ 수정: 부모를 null로 설정하여 월드 좌표계 사용
        ball.transform.SetParent(null);
        
        // 공 활성화 및 설정
        ball.gameObject.SetActive(true);
        ball.transform.position = position; // 이제 월드 좌표로 정상 작동
        ball.transform.rotation = Quaternion.identity;
        
        activeBalls.Add(ball);
        
        // 발사
        ball.Launch(launchDirection);
        
        Debug.Log($"[BrickBreakerBallPool] 공 가져오기: {ball.gameObject.name} (위치: {position}) | 활성: {ActiveBallCount}, 대기: {AvailableBallCount}");
        
        return ball;
    }
    
    /// <summary>
    /// 공을 풀로 반환합니다.
    /// </summary>
    /// <param name="ball">반환할 BrickBreakerBall 컴포넌트</param>
    public void ReturnBall(BrickBreakerBall ball)
    {
        if (ball == null)
        {
            Debug.LogWarning("[BrickBreakerBallPool] null 공을 반환하려고 했습니다.");
            return;
        }
        
        if (!activeBalls.Contains(ball))
        {
            Debug.LogWarning($"[BrickBreakerBallPool] {ball.gameObject.name}은(는) 활성 공 목록에 없습니다.");
            return;
        }
        
        // 공 정지 및 비활성화
        ball.Stop();
        ball.gameObject.SetActive(false);
        ball.transform.SetParent(poolParent);
        
        // 풀로 이동
        activeBalls.Remove(ball);
        availableBalls.Enqueue(ball);
        
        Debug.Log($"[BrickBreakerBallPool] 공 반환: {ball.gameObject.name} | 활성: {ActiveBallCount}, 대기: {AvailableBallCount}");
    }
    
    /// <summary>
    /// 모든 활성 공을 풀로 반환합니다.
    /// </summary>
    public void ReturnAllBalls()
    {
        // 복사본으로 순회 (컬렉션 수정 방지)
        List<BrickBreakerBall> ballsToReturn = new List<BrickBreakerBall>(activeBalls);
        
        foreach (var ball in ballsToReturn)
        {
            ReturnBall(ball);
        }
        
        Debug.Log($"[BrickBreakerBallPool] 모든 공 반환 완료: {ballsToReturn.Count}개");
    }
    
    /// <summary>
    /// 풀을 완전히 초기화합니다.
    /// </summary>
    public void ClearPool()
    {
        // 모든 공 반환
        ReturnAllBalls();
        
        // 모든 공 파괴
        while (availableBalls.Count > 0)
        {
            BrickBreakerBall ball = availableBalls.Dequeue();
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }
        
        availableBalls.Clear();
        activeBalls.Clear();
        
        Debug.Log("[BrickBreakerBallPool] 풀 초기화 완료");
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (initialPoolSize < 0)
        {
            initialPoolSize = 0;
            Debug.LogWarning("[BrickBreakerBallPool] 초기 풀 크기는 0 이상이어야 합니다.");
        }
        
        if (maxPoolSize < 0)
        {
            maxPoolSize = 0;
        }
        
        if (maxPoolSize > 0 && initialPoolSize > maxPoolSize)
        {
            initialPoolSize = maxPoolSize;
            Debug.LogWarning("[BrickBreakerBallPool] 초기 풀 크기가 최대 풀 크기보다 큽니다. 조정되었습니다.");
        }
    }
#endif
}