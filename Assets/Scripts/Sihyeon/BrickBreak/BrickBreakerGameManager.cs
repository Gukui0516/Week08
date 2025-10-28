using UnityEngine;

/// <summary>
/// 벽돌깨기 게임의 전체 흐름을 관리합니다.
/// 테스트를 위한 간단한 발사 기능을 포함합니다.
/// </summary>
public class BrickBreakerGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [Tooltip("공의 시작 위치입니다.")]
    [SerializeField] private Transform ballSpawnPoint;
    
    [Tooltip("공의 초기 발사 방향입니다. (정규화됨)")]
    [SerializeField] private Vector3 initialLaunchDirection = new Vector3(1f, 1f, 0f);
    
    [Header("Test Settings")]
    [Tooltip("게임 시작 시 자동으로 공을 발사합니다.")]
    [SerializeField] private bool autoLaunchOnStart = false;
    
    [Tooltip("자동 발사 지연 시간(초)입니다.")]
    [SerializeField] private float autoLaunchDelay = 1f;
    
    private BrickBreakerBallPool ballPool;
    private BrickBreakerBall currentBall;
    
    private void Start()
    {
        // BallPool 참조 가져오기
        ballPool = BrickBreakerBallPool.Instance;
        
        if (ballPool == null)
        {
            Debug.LogError("[BrickBreakerGameManager] BrickBreakerBallPool을 찾을 수 없습니다!");
            return;
        }
        
        // 자동 발사
        if (autoLaunchOnStart)
        {
            Invoke(nameof(LaunchBall), autoLaunchDelay);
        }
    }
    
    private void Update()
    {
        // 스페이스바로 공 발사 (테스트용)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LaunchBall();
        }
        
        // R키로 모든 공 회수 (테스트용)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReturnAllBalls();
        }
    }
    
    /// <summary>
    /// 공을 발사합니다.
    /// </summary>
    public void LaunchBall()
    {
        if (ballPool == null)
        {
            Debug.LogError("[BrickBreakerGameManager] BallPool이 null입니다!");
            return;
        }
        
        Vector3 spawnPosition = ballSpawnPoint != null ? ballSpawnPoint.position : transform.position;
        
        // Z축 방향 제거 (2D)
        Vector3 launchDir = initialLaunchDirection;
        launchDir.z = 0f;
        launchDir.Normalize();
        
        BrickBreakerBall ball = ballPool.GetBall(spawnPosition, launchDir);
        
        if (ball != null)
        {
            currentBall = ball;
            Debug.Log($"[BrickBreakerGameManager] 공 발사 완료: {ball.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[BrickBreakerGameManager] 공을 가져올 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 모든 공을 회수합니다.
    /// </summary>
    public void ReturnAllBalls()
    {
        if (ballPool != null)
        {
            ballPool.ReturnAllBalls();
            currentBall = null;
            Debug.Log("[BrickBreakerGameManager] 모든 공 회수 완료");
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoLaunchDelay < 0f)
        {
            autoLaunchDelay = 0f;
        }
    }
    
    private void OnDrawGizmos()
    {
        // 발사 시작점 시각화
        if (ballSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ballSpawnPoint.position, 0.5f);
            
            // 발사 방향 시각화
            Vector3 launchDir = initialLaunchDirection;
            launchDir.z = 0f;
            launchDir.Normalize();
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ballSpawnPoint.position, launchDir * 3f);
        }
    }
#endif
}