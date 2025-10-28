using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고스트의 AI 이동을 관리합니다.
/// GridManager를 기반으로 경로를 찾아 이동하며, 상태에 따라 다른 행동을 수행합니다.
/// 격자 기반 이동 시스템으로 대각선 이동을 방지하고 부드러운 이동을 보장합니다.
/// </summary>
[RequireComponent(typeof(GhostState))]
[RequireComponent(typeof(Rigidbody))]
public class GhostController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("Normal 상태에서의 이동 속도")]
    [SerializeField] private float normalSpeed = 3f;

    [Tooltip("Frightened 상태에서의 이동 속도 (느려짐)")]
    [SerializeField] private float frightenedSpeed = 1.5f;

    [Tooltip("Eaten 상태에서의 이동 속도 (빨라짐)")]
    [SerializeField] private float eatenSpeed = 6f;

    [Tooltip("격자 중심에 도착했다고 판단하는 거리 (작을수록 정확)")]
    [SerializeField] private float gridSnapDistance = 0.05f;

    [Header("AI 행동 설정")]
    [Tooltip("경로를 재계산하는 주기 (초)")]
    [SerializeField] private float pathUpdateInterval = 0.5f;

    [Tooltip("팩맨을 감지할 수 있는 최대 거리 (0이면 무제한)")]
    [SerializeField] private float detectionRange = 0f;

    [Tooltip("Frightened 상태에서 랜덤 이동 확률 (0~1)")]
    [SerializeField] private float frightenedRandomChance = 0.3f;

    [Header("AI 개성 설정")]
    [Tooltip("고스트의 AI 타입 (각각 다른 추격 방식)")]
    [SerializeField] private GhostAIType aiType = GhostAIType.Chaser;

    [Tooltip("목표 위치 오프셋 (AI 타입별 개성)")]
    [SerializeField] private Vector2Int targetOffset = Vector2Int.zero;

    [Header("참조")]
    [Tooltip("추격할 대상 (팩맨)")]
    [SerializeField] private Transform targetPlayer;

    [Tooltip("플레이어를 자동으로 찾을지 여부 (Player 태그 사용)")]
    [SerializeField] private bool autoFindPlayer = true;

    [Header("디버그")]
    [Tooltip("경로를 Scene 뷰에 표시할지 여부")]
    [SerializeField] private bool showPathGizmos = true;

    [Tooltip("디버그 정보를 콘솔에 출력할지 여부")]
    [SerializeField] private bool showDebugInfo = false;

    [Tooltip("현재 격자 위치를 Scene 뷰에 표시할지 여부")]
    [SerializeField] private bool showCurrentGridPosition = true;

    [Tooltip("경로의 각 격자가 통행 가능한지 검증")]
    [SerializeField] private bool validatePathWalkability = true;

    [Tooltip("벽 격자를 특별한 색으로 표시")]
    [SerializeField] private bool highlightWallsInPath = true;

    // 컴포넌트 참조
    private GhostState ghostState;
    private Rigidbody rb;

    // 격자 기반 이동 관련
    private Vector2Int currentGridPosition;      // 현재 격자 좌표
    private Vector2Int targetGridPosition;       // 목표 격자 좌표
    private Vector3 targetWorldPosition;         // 목표 월드 좌표
    private bool isMovingToTarget = false;       // 현재 이동 중인지 여부

    // 경로 찾기 관련
    private List<Vector2Int> currentPath = new List<Vector2Int>(); // 격자 좌표 경로
    private int currentPathIndex = 0;
    private float pathUpdateTimer = 0f;

    // 디버그용 - 경로의 통행 가능 여부 저장
    private List<bool> pathWalkabilityStatus = new List<bool>();

    // 현재 이동 상태
    private Vector3 moveDirection;
    private float currentSpeed;

    /// <summary>
    /// 고스트의 AI 타입을 정의합니다.
    /// 각 타입마다 다른 추격 전략을 사용합니다.
    /// </summary>
    public enum GhostAIType
    {
        Chaser,     // 직접 추격 (빨강 - Blinky)
        Ambusher,   // 앞을 막음 (분홍 - Pinky)
        Patroller,  // 순찰하다가 추격 (파랑 - Inky)
        Random      // 랜덤 이동 (주황 - Clyde)
    }

    void Awake()
    {
        ghostState = GetComponent<GhostState>();
        rb = GetComponent<Rigidbody>();

        // Rigidbody 설정: 중력 없음, Kinematic 모드
        rb.useGravity = false;
        rb.isKinematic = true;

        // 회전 고정 (2D 이동)
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        // 플레이어 자동 찾기
        if (autoFindPlayer && targetPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Draggable");
            if (playerObject != null)
            {
                targetPlayer = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Draggable 태그를 가진 오브젝트를 찾을 수 없습니다.");
            }
        }
    }

    void Start()
    {
        // 현재 위치를 가장 가까운 격자 중심으로 스냅
        SnapToNearestGrid();

        // 초기 경로 계산
        UpdatePath();
    }

    void Update()
    {
        // 게임오버 상태면 이동 중지
        if (PacmanGameManager.Instance != null && PacmanGameManager.Instance.IsGameOver())
        {
            return;
        }

        // 상태에 따른 속도 설정
        UpdateSpeed();

        // 경로 업데이트 타이머 (이동 중일 때만 증가)
        if (isMovingToTarget)
        {
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateInterval)
            {
                pathUpdateTimer = 0f;
                UpdatePath();
            }
        }
    }

    /// <summary>
    /// 현재 위치를 가장 가까운 격자 중심으로 스냅합니다.
    /// 게임 시작 시 고스트가 정확한 격자 위치에서 시작하도록 보장합니다.
    /// </summary>
    private void SnapToNearestGrid()
    {
        if (GridManager.Instance == null)
            return;

        currentGridPosition = GridManager.Instance.WorldToGridPosition(transform.position);
        Vector3 gridCenter = GridManager.Instance.GridToWorldPosition(currentGridPosition.x, currentGridPosition.y);
        transform.position = gridCenter;

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 격자 스냅 완료 - Grid({currentGridPosition.x}, {currentGridPosition.y})");
        }
    }

    /// <summary>
    /// 현재 상태에 따라 이동 속도를 업데이트합니다.
    /// Normal/Frightened/Eaten 상태마다 다른 속도를 적용합니다.
    /// </summary>
    private void UpdateSpeed()
    {
        switch (ghostState.GetCurrentState())
        {
            case GhostState.State.Normal:
                currentSpeed = normalSpeed;
                break;

            case GhostState.State.Frightened:
                currentSpeed = frightenedSpeed;
                break;

            case GhostState.State.Eaten:
                currentSpeed = eatenSpeed;
                break;
        }
    }

    /// <summary>
    /// 경로를 업데이트합니다. 상태에 따라 다른 목표를 설정합니다.
    /// 이동 중에도 자연스럽게 경로를 재계산하여 멈칫거림을 방지합니다.
    /// </summary>
    private void UpdatePath()
    {
        if (GridManager.Instance == null)
            return;

        Vector3 targetPosition;

        // 상태에 따라 목표 위치 결정
        switch (ghostState.GetCurrentState())
        {
            case GhostState.State.Normal:
                targetPosition = GetChaseTarget();
                break;

            case GhostState.State.Frightened:
                targetPosition = GetFleeTarget();
                break;

            case GhostState.State.Eaten:
                targetPosition = ghostState.GetHomePosition();
                break;

            default:
                return;
        }

        // 목표 격자 좌표 계산
        Vector2Int targetGrid = GridManager.Instance.WorldToGridPosition(targetPosition);

        // 현재 격자 위치에서 목표까지의 경로 계산 (격자 좌표 기반)
        List<Vector3> worldPath = GridManager.Instance.FindPath(
            GridManager.Instance.GridToWorldPosition(currentGridPosition.x, currentGridPosition.y),
            targetPosition
        );

        // 월드 좌표 경로를 격자 좌표 경로로 변환
        currentPath.Clear();
        pathWalkabilityStatus.Clear();

        foreach (Vector3 worldPos in worldPath)
        {
            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPos);
            currentPath.Add(gridPos);

            // 경로 검증: 이 격자가 실제로 통행 가능한지 확인
            if (validatePathWalkability)
            {
                bool isWalkable = GridManager.Instance.IsWalkable(gridPos.x, gridPos.y);
                pathWalkabilityStatus.Add(isWalkable);

                // 벽을 포함한 경로 발견 시 경고
                if (!isWalkable && showDebugInfo)
                {
                    Debug.LogError($"{gameObject.name}: 경로에 벽이 포함됨! Grid({gridPos.x}, {gridPos.y})");
                    Debug.LogError($"이 위치의 월드 좌표: {worldPos}");
                }
            }
        }

        // 경로가 비어있지 않으면 첫 번째 목표 설정
        if (currentPath.Count > 0)
        {
            currentPathIndex = 0;

            // 현재 이동 중이 아닐 때만 새로운 목표 설정
            if (!isMovingToTarget)
            {
                SetNextTarget();
            }
            // 이동 중일 때는 현재 목표를 유지하여 멈칫거림 방지

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name}: 경로 업데이트 (웨이포인트 {currentPath.Count}개)");

                // 경로의 각 격자 좌표 출력
                string pathString = "경로: ";
                for (int i = 0; i < currentPath.Count; i++)
                {
                    pathString += $"({currentPath[i].x}, {currentPath[i].y})";
                    if (validatePathWalkability)
                    {
                        pathString += pathWalkabilityStatus[i] ? "[O]" : "[X-벽!]";
                    }
                    if (i < currentPath.Count - 1)
                        pathString += " -> ";
                }
                Debug.Log(pathString);
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"{gameObject.name}: 경로를 찾을 수 없습니다!");
            Debug.LogWarning($"현재 위치: Grid({currentGridPosition.x}, {currentGridPosition.y}), 목표: Grid({targetGrid.x}, {targetGrid.y})");
        }
    }

    /// <summary>
    /// 다음 목표 격자 위치를 설정합니다.
    /// 경로에서 다음 웨이포인트를 가져와 목표로 설정합니다.
    /// </summary>
    private void SetNextTarget()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            isMovingToTarget = false;
            return;
        }

        targetGridPosition = currentPath[currentPathIndex];
        targetWorldPosition = GridManager.Instance.GridToWorldPosition(
            targetGridPosition.x,
            targetGridPosition.y
        );

        isMovingToTarget = true;
        currentPathIndex++;

        // 목표 격자가 벽인지 확인
        if (validatePathWalkability && !GridManager.Instance.IsWalkable(targetGridPosition.x, targetGridPosition.y))
        {
            Debug.LogError($"{gameObject.name}: 벽으로 이동하려고 시도! Grid({targetGridPosition.x}, {targetGridPosition.y})");
            Debug.LogError($"이동 중단! 경로 재계산 필요");

            // 벽으로 이동하려는 경우 즉시 경로 재계산
            isMovingToTarget = false;
            UpdatePath();
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 다음 목표 설정 - Grid({targetGridPosition.x}, {targetGridPosition.y})");
        }
    }

    /// <summary>
    /// 격자 기반으로 이동합니다.
    /// 4방향(상하좌우)만 이동하여 대각선 이동을 완전히 방지합니다.
    /// X축 또는 Y축 중 하나의 축으로만 이동하여 정통 팩맨 스타일의 격자 이동을 구현합니다.
    /// 
    /// 동작 원리:
    /// 1. 현재 위치와 목표 위치의 X축, Y축 차이를 계산
    /// 2. 차이가 더 큰 축을 선택하여 그 방향으로만 이동
    /// 3. 한 번에 한 축으로만 이동하므로 대각선 이동이 발생하지 않음
    /// 4. 격자 중심에 정확히 도착하면 다음 격자로 이동
    /// </summary>
    private void MoveAlongGrid()
    {
        if (!isMovingToTarget)
        {
            // 이동할 목표가 없으면 다음 목표 설정
            if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
            {
                SetNextTarget();
            }
            else
            {
                // 경로가 끝났으면 새로운 경로 계산
                UpdatePath();
            }
            return;
        }

        // 목표 위치까지의 방향과 거리 계산
        Vector3 directionToTarget = targetWorldPosition - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // 격자 중심에 도착했는지 확인
        if (distanceToTarget <= gridSnapDistance)
        {
            // 정확히 격자 중심으로 스냅
            transform.position = targetWorldPosition;
            currentGridPosition = targetGridPosition;

            // 다음 목표 설정
            isMovingToTarget = false;

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name}: 격자 도착 - Grid({currentGridPosition.x}, {currentGridPosition.y})");
            }

            return;
        }

        // *** 핵심 로직: 4방향 이동만 허용 ***
        // X축과 Y축 중 더 큰 차이가 있는 축으로만 이동
        // 이렇게 하면 한 번에 한 방향으로만 이동하여 대각선 이동을 완전히 차단합니다
        Vector3 movementDirection = Vector3.zero;

        float absX = Mathf.Abs(directionToTarget.x);
        float absY = Mathf.Abs(directionToTarget.y);

        if (absX > absY)
        {
            // X축으로만 이동 (좌우)
            movementDirection = new Vector3(Mathf.Sign(directionToTarget.x), 0, 0);
        }
        else
        {
            // Y축으로만 이동 (상하)
            movementDirection = new Vector3(0, Mathf.Sign(directionToTarget.y), 0);
        }

        // 이동 거리 계산 (목표를 넘어가지 않도록 제한)
        float moveDistance = currentSpeed * Time.deltaTime;

        // 한 축으로만 이동할 때의 실제 남은 거리 계산
        float remainingDistance = 0f;
        if (movementDirection.x != 0)
        {
            remainingDistance = Mathf.Abs(directionToTarget.x);
        }
        else if (movementDirection.y != 0)
        {
            remainingDistance = Mathf.Abs(directionToTarget.y);
        }

        // 목표를 넘어가지 않도록 제한
        if (moveDistance > remainingDistance)
        {
            moveDistance = remainingDistance;
        }

        // Rigidbody를 사용한 부드러운 이동 (4방향만)
        Vector3 newPosition = transform.position + movementDirection * moveDistance;
        rb.MovePosition(newPosition);

        // 디버그: 현재 이동 방향 저장 (Gizmo 표시용)
        moveDirection = movementDirection;
    }

    /// <summary>
    /// Normal 상태에서 추격할 목표 위치를 반환합니다.
    /// AI 타입에 따라 다른 전략을 사용합니다.
    /// </summary>
    private Vector3 GetChaseTarget()
    {
        if (targetPlayer == null)
            return transform.position;

        Vector3 playerPos = targetPlayer.position;
        Vector2Int playerGrid = GridManager.Instance.WorldToGridPosition(playerPos);

        // AI 타입별 목표 계산
        switch (aiType)
        {
            case GhostAIType.Chaser:
                // 직접 추격: 팩맨의 현재 위치
                return playerPos;

            case GhostAIType.Ambusher:
                // 앞을 막음: 팩맨의 이동 방향 4칸 앞
                Vector2Int offset = GetPlayerMovementDirection();
                Vector2Int targetGrid = playerGrid + offset * 4;
                return GridManager.Instance.GridToWorldPosition(targetGrid.x, targetGrid.y);

            case GhostAIType.Patroller:
                // 순찰: 팩맨과의 거리에 따라 다른 행동
                float distance = Vector3.Distance(transform.position, playerPos);
                if (distance > 8f)
                {
                    // 멀면 추격
                    return playerPos;
                }
                else
                {
                    // 가까우면 분산 (맵 모퉁이로)
                    return GetScatterTarget();
                }

            case GhostAIType.Random:
                // 랜덤: 팩맨과 가까우면 추격, 멀면 랜덤
                float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
                if (distanceToPlayer < 8f)
                {
                    return playerPos;
                }
                else
                {
                    return GetRandomTarget();
                }

            default:
                return playerPos;
        }
    }

    /// <summary>
    /// Frightened 상태에서 도망갈 목표 위치를 반환합니다.
    /// 일정 확률로 랜덤 이동을 하거나 팩맨 반대 방향으로 도망갑니다.
    /// </summary>
    private Vector3 GetFleeTarget()
    {
        if (targetPlayer == null)
            return GetRandomTarget();

        // 일정 확률로 랜덤 이동
        if (Random.value < frightenedRandomChance)
        {
            return GetRandomTarget();
        }

        // 팩맨 반대 방향으로 도망
        Vector3 fleeDirection = (transform.position - targetPlayer.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 10f;

        return fleeTarget;
    }

    /// <summary>
    /// 랜덤한 목표 위치를 반환합니다.
    /// Frightened 상태나 Random AI 타입에서 사용됩니다.
    /// </summary>
    private Vector3 GetRandomTarget()
    {
        if (GridManager.Instance == null)
            return transform.position;

        int randomX = Random.Range(0, 29);
        int randomZ = Random.Range(0, 32);

        return GridManager.Instance.GridToWorldPosition(randomX, randomZ);
    }

    /// <summary>
    /// 맵 모퉁이로 가는 목표를 반환합니다.
    /// Patroller AI 타입의 분산(Scatter) 모드에서 사용됩니다.
    /// </summary>
    private Vector3 GetScatterTarget()
    {
        if (GridManager.Instance == null)
            return transform.position;

        // 4개 모퉁이 중 랜덤 선택
        int corner = Random.Range(0, 4);
        int x, z;

        switch (corner)
        {
            case 0: x = 0; z = 0; break;           // 왼쪽 아래
            case 1: x = 28; z = 0; break;          // 오른쪽 아래
            case 2: x = 0; z = 31; break;          // 왼쪽 위
            case 3: x = 28; z = 31; break;         // 오른쪽 위
            default: x = 0; z = 0; break;
        }

        return GridManager.Instance.GridToWorldPosition(x, z);
    }

    /// <summary>
    /// 플레이어의 이동 방향을 추정합니다.
    /// Ambusher AI가 팩맨의 앞을 막기 위해 사용합니다.
    /// </summary>
    private Vector2Int GetPlayerMovementDirection()
    {
        if (targetPlayer == null)
            return Vector2Int.zero;

        // 플레이어의 Rigidbody 속도를 기반으로 방향 추정
        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();
        if (playerRb != null && playerRb.linearVelocity.magnitude > 0.1f)
        {
            Vector3 velocity = playerRb.linearVelocity;

            // X-Y 평면 속도를 그리드 방향으로 변환
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                return velocity.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                return velocity.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// 팩맨과 충돌했을 때 호출됩니다.
    /// 현재 상태에 따라 다른 처리를 수행합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Draggable"))
        {
            HandlePlayerCollision();
        }
    }

    /// <summary>
    /// 플레이어와의 충돌을 처리합니다.
    /// Normal 상태: 팩맨 잡음 (게임오버)
    /// Frightened 상태: 고스트가 먹힘
    /// Eaten 상태: 충돌 무시
    /// </summary>
    private void HandlePlayerCollision()
    {
        switch (ghostState.GetCurrentState())
        {
            case GhostState.State.Normal:
                // 팩맨이 잡힘 → 게임오버
                if (PacmanGameManager.Instance != null)
                {
                    PacmanGameManager.Instance.OnPacmanCaught();
                }
                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name}: 팩맨을 잡았습니다!");
                }
                break;

            case GhostState.State.Frightened:
                // 고스트가 먹힘
                ghostState.GetEaten();
                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name}: 먹혔습니다!");
                }
                break;

            case GhostState.State.Eaten:
                // 이미 먹힌 상태이므로 무시
                break;
        }
    }

    void FixedUpdate()
    {
        // 격자 기반 이동 처리
        MoveAlongGrid();

        // Eaten 상태에서 홈에 도착했는지 체크
        if (ghostState.IsEaten() && ghostState.HasReachedHome())
        {
            ghostState.Respawn();

            // 부활 후 현재 격자 위치 업데이트
            SnapToNearestGrid();
        }
    }

    // ===== Public 메서드들 =====

    /// <summary>
    /// 현재 이동 속도를 반환합니다.
    /// 다른 스크립트에서 고스트의 속도를 확인할 때 사용합니다.
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// AI 타입을 설정합니다.
    /// 런타임 중 AI 타입을 변경할 때 사용합니다.
    /// </summary>
    public void SetAIType(GhostAIType type)
    {
        aiType = type;
        UpdatePath(); // 타입 변경 시 경로 재계산
    }

    /// <summary>
    /// 경로를 강제로 재계산합니다.
    /// 외부에서 경로 업데이트가 필요할 때 호출합니다.
    /// </summary>
    public void ForceUpdatePath()
    {
        UpdatePath();
    }

    /// <summary>
    /// 현재 격자 위치를 반환합니다.
    /// 디버그나 다른 시스템에서 고스트의 격자 위치를 확인할 때 사용합니다.
    /// </summary>
    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }

    /// <summary>
    /// Scene 뷰에서 경로와 현재 상태를 시각화합니다.
    /// 디버깅 시 고스트의 이동 경로와 목표를 확인할 수 있습니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showPathGizmos || currentPath == null || currentPath.Count == 0)
            return;

        if (GridManager.Instance == null)
            return;

        // 현재 격자 위치 표시
        if (showCurrentGridPosition)
        {
            Vector3 currentGridWorldPos = GridManager.Instance.GridToWorldPosition(
                currentGridPosition.x,
                currentGridPosition.y
            );

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(currentGridWorldPos, Vector3.one * 0.3f);
        }

        // 현재 목표 위치 표시 (파란색)
        if (isMovingToTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetWorldPosition);
            Gizmos.DrawWireSphere(targetWorldPosition, 0.2f);

            // 현재 이동 방향 표시 (흰색 화살표) - 4방향만 표시됨
            if (Application.isPlaying && moveDirection != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Vector3 arrowEnd = transform.position + moveDirection * 0.5f;
                Gizmos.DrawLine(transform.position, arrowEnd);

                // 화살표 끝 표시
                Gizmos.DrawSphere(arrowEnd, 0.1f);
            }
        }

        // 경로 전체 표시
        for (int i = currentPathIndex; i < currentPath.Count - 1; i++)
        {
            Vector3 start = GridManager.Instance.GridToWorldPosition(
                currentPath[i].x,
                currentPath[i].y
            );
            Vector3 end = GridManager.Instance.GridToWorldPosition(
                currentPath[i + 1].x,
                currentPath[i + 1].y
            );

            // 벽인 경로는 빨간색으로, 통로는 노란색으로 표시
            if (validatePathWalkability && highlightWallsInPath && i < pathWalkabilityStatus.Count)
            {
                Gizmos.color = pathWalkabilityStatus[i] ? Color.yellow : Color.red;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.1f);
        }

        // 최종 목표 표시
        if (currentPath.Count > 0)
        {
            Vector3 finalGoal = GridManager.Instance.GridToWorldPosition(
                currentPath[currentPath.Count - 1].x,
                currentPath[currentPath.Count - 1].y
            );

            // 마지막 목표도 벽인지 확인
            if (validatePathWalkability && highlightWallsInPath && pathWalkabilityStatus.Count > 0)
            {
                Gizmos.color = pathWalkabilityStatus[pathWalkabilityStatus.Count - 1] ? Color.red : Color.magenta;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawWireSphere(finalGoal, 0.25f);
        }
    }
}