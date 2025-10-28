using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고스트 간 충돌 회피를 전문적으로 처리하는 컴포넌트입니다.
/// GhostController와 함께 사용하여 더욱 정교한 회피 동작을 구현할 수 있습니다.
/// 
/// [사용법]
/// 1. GhostController가 이미 기본 회피 기능을 제공하므로, 이 컴포넌트는 선택사항입니다.
/// 2. 더 정교한 회피가 필요한 경우에만 추가하세요.
/// 3. GhostController의 enableAvoidance를 false로 설정하고 이 컴포넌트를 사용하세요.
/// 
/// [특징]
/// - Flocking 알고리즘 기반 (Separation, Alignment, Cohesion)
/// - 동적 회피 강도 조절
/// - 장애물 회피 지원
/// </summary>
[RequireComponent(typeof(GhostController))]
public class GhostAvoidance : MonoBehaviour
{
    [Header("회피 설정")]
    [Tooltip("Separation: 다른 고스트와 떨어지려는 힘")]
    [SerializeField] private float separationWeight = 2.0f;

    [Tooltip("Alignment: 다른 고스트와 같은 방향으로 움직이려는 힘")]
    [SerializeField] private float alignmentWeight = 0.5f;

    [Tooltip("Cohesion: 다른 고스트들의 중심으로 모이려는 힘")]
    [SerializeField] private float cohesionWeight = 0.3f;

    [Header("감지 범위")]
    [Tooltip("Separation을 적용할 감지 범위")]
    [SerializeField] private float separationRadius = 1.5f;

    [Tooltip("Alignment와 Cohesion을 적용할 감지 범위")]
    [SerializeField] private float neighborRadius = 3f;

    [Tooltip("고스트를 감지할 레이어")]
    [SerializeField] private LayerMask ghostLayer;

    [Header("장애물 회피")]
    [Tooltip("장애물 회피 활성화")]
    [SerializeField] private bool enableObstacleAvoidance = true;

    [Tooltip("장애물을 감지할 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("장애물 감지 거리")]
    [SerializeField] private float obstacleDetectionDistance = 2f;

    [Tooltip("장애물 회피 힘")]
    [SerializeField] private float obstacleAvoidanceForce = 3f;

    [Header("디버그")]
    [Tooltip("회피 벡터를 Scene 뷰에 표시")]
    [SerializeField] private bool showDebugGizmos = false;

    // 컴포넌트 참조
    private GhostController ghostController;

    // 계산된 회피 벡터들
    private Vector3 separationVector;
    private Vector3 alignmentVector;
    private Vector3 cohesionVector;
    private Vector3 obstacleAvoidanceVector;

    // 최종 회피 방향
    public Vector3 AvoidanceDirection { get; private set; }

    void Awake()
    {
        ghostController = GetComponent<GhostController>();
    }

    void Update()
    {
        // 모든 회피 힘 계산
        CalculateFlockingBehaviors();

        if (enableObstacleAvoidance)
        {
            CalculateObstacleAvoidance();
        }

        // 최종 회피 방향 합성
        CombineAvoidanceVectors();
    }

    /// <summary>
    /// Flocking 알고리즘의 3가지 행동을 계산합니다.
    /// Separation: 가까운 이웃으로부터 멀어지기
    /// Alignment: 이웃들과 같은 방향으로 이동
    /// Cohesion: 이웃들의 평균 위치로 모이기
    /// </summary>
    private void CalculateFlockingBehaviors()
    {
        separationVector = Vector3.zero;
        alignmentVector = Vector3.zero;
        cohesionVector = Vector3.zero;

        // Separation 계산 (가까운 고스트 감지)
        Collider[] nearbyGhosts = Physics.OverlapSphere(transform.position, separationRadius, ghostLayer);
        int separationCount = 0;

        foreach (Collider other in nearbyGhosts)
        {
            if (other.transform == transform)
                continue;

            Vector3 awayVector = transform.position - other.transform.position;
            float distance = awayVector.magnitude;

            if (distance < 0.01f)
            {
                awayVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            }
            else
            {
                // 거리에 반비례하는 힘 (가까울수록 강하게 밀어냄)
                float force = 1f - (distance / separationRadius);
                awayVector = awayVector.normalized * force;
            }

            separationVector += awayVector;
            separationCount++;
        }

        if (separationCount > 0)
        {
            separationVector /= separationCount;
            separationVector = separationVector.normalized * separationWeight;
        }

        // Alignment & Cohesion 계산 (이웃 고스트 감지)
        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius, ghostLayer);
        int neighborCount = 0;
        Vector3 avgVelocity = Vector3.zero;
        Vector3 avgPosition = Vector3.zero;

        foreach (Collider other in neighbors)
        {
            if (other.transform == transform)
                continue;

            // Alignment: 이웃의 속도 평균
            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                avgVelocity += otherRb.linearVelocity;
            }

            // Cohesion: 이웃의 위치 평균
            avgPosition += other.transform.position;
            neighborCount++;
        }

        if (neighborCount > 0)
        {
            // Alignment
            avgVelocity /= neighborCount;
            alignmentVector = avgVelocity.normalized * alignmentWeight;

            // Cohesion
            avgPosition /= neighborCount;
            cohesionVector = (avgPosition - transform.position).normalized * cohesionWeight;
        }
    }

    /// <summary>
    /// 앞쪽의 장애물을 감지하여 회피 방향을 계산합니다.
    /// Raycast를 사용하여 전방의 장애물을 미리 감지합니다.
    /// </summary>
    private void CalculateObstacleAvoidance()
    {
        obstacleAvoidanceVector = Vector3.zero;

        // 현재 이동 방향
        Vector3 forwardDirection = transform.up; // 2D에서는 up이 전방

        // 전방 3방향 Raycast (중앙, 좌, 우)
        Vector3[] directions = new Vector3[]
        {
            forwardDirection,                                    // 중앙
            Quaternion.Euler(0, 0, 30) * forwardDirection,      // 우측 30도
            Quaternion.Euler(0, 0, -30) * forwardDirection      // 좌측 30도
        };

        foreach (Vector3 direction in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, obstacleDetectionDistance, obstacleLayer))
            {
                // 장애물을 발견하면 반대 방향으로 회피
                Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.forward);
                float avoidForce = 1f - (hit.distance / obstacleDetectionDistance);

                obstacleAvoidanceVector += avoidDirection.normalized * avoidForce;
            }
        }

        if (obstacleAvoidanceVector.magnitude > 0.01f)
        {
            obstacleAvoidanceVector = obstacleAvoidanceVector.normalized * obstacleAvoidanceForce;
        }
    }

    /// <summary>
    /// 모든 회피 벡터를 합성하여 최종 회피 방향을 계산합니다.
    /// </summary>
    private void CombineAvoidanceVectors()
    {
        AvoidanceDirection = separationVector + alignmentVector + cohesionVector + obstacleAvoidanceVector;

        // 최대 크기 제한
        if (AvoidanceDirection.magnitude > 1f)
        {
            AvoidanceDirection = AvoidanceDirection.normalized;
        }
    }

    /// <summary>
    /// 외부에서 호출하여 현재 회피 방향을 가져옵니다.
    /// GhostController에서 사용할 수 있습니다.
    /// </summary>
    public Vector3 GetAvoidanceDirection()
    {
        return AvoidanceDirection;
    }

    /// <summary>
    /// 회피 강도를 동적으로 조절합니다.
    /// 예: 좁은 복도에서는 회피를 약하게, 넓은 공간에서는 강하게
    /// </summary>
    public void SetAvoidanceStrength(float separation, float alignment, float cohesion)
    {
        separationWeight = separation;
        alignmentWeight = alignment;
        cohesionWeight = cohesion;
    }

    /// <summary>
    /// Scene 뷰에서 회피 벡터와 감지 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;

        // Separation 범위 (빨강)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Neighbor 범위 (초록)
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        // 회피 벡터들
        if (Application.isPlaying)
        {
            // Separation (빨강)
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, separationVector);

            // Alignment (파랑)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, alignmentVector);

            // Cohesion (초록)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, cohesionVector);

            // Obstacle Avoidance (노랑)
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, obstacleAvoidanceVector);

            // 최종 회피 방향 (흰색)
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position, AvoidanceDirection * 2f);
        }

        // 장애물 감지 Raycast 시각화
        if (enableObstacleAvoidance)
        {
            Gizmos.color = Color.cyan;
            Vector3 forward = transform.up;
            Gizmos.DrawRay(transform.position, forward * obstacleDetectionDistance);
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, 0, 30) * forward * obstacleDetectionDistance);
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, 0, -30) * forward * obstacleDetectionDistance);
        }
    }
}