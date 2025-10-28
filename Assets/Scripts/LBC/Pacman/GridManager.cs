using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 팩맨 게임을 위한 격자 기반 경로 찾기 시스템입니다.
/// 세로로 세운 판(Y-Z 평면) 위에서 29x32 그리드를 관리하며, A* 알고리즘으로 경로를 찾습니다.
/// 
/// [개선 사항]
/// - 4방향(상하좌우)만 이동 가능하도록 강제
/// - 경로 스무딩 기본 비활성화로 벽 뚫기 방지
/// - 벽 감지 정확도 향상
/// </summary>
public class GridManager : SingletonObject<GridManager>
{
    [Header("그리드 설정")]
    [Tooltip("그리드의 가로 크기 (X축)")]
    [SerializeField] private int gridWidth = 29;

    [Tooltip("그리드의 세로 크기 (Z축)")]
    [SerializeField] private int gridHeight = 32;

    [Tooltip("각 그리드 셀의 실제 크기")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("그리드가 위치할 판의 Transform (세로로 세운 판)")]
    [SerializeField] private Transform boardTransform;

    [Header("그리드 위치 조정")]
    [Tooltip("그리드의 X축 오프셋 (좌우 이동)")]
    [SerializeField] private float gridOffsetX = 0f;

    [Tooltip("그리드의 Y축 오프셋 (상하 이동)")]
    [SerializeField] private float gridOffsetY = 0f;

    [Tooltip("그리드의 Z축 오프셋 (앞뒤 이동)")]
    [SerializeField] private float gridOffsetZ = 0f;

    [Header("경로 찾기 설정")]
    [Tooltip("경로 스무딩 활성화 (벽 뚫기 문제가 있으므로 비활성화 권장)")]
    [SerializeField] private bool enablePathSmoothing = false;

    [Tooltip("경로 단순화 거리 (높을수록 더 단순한 경로)")]
    [SerializeField] private float simplificationTolerance = 0.3f;

    [Header("시각화 설정")]
    [Tooltip("Scene 뷰에서 그리드를 표시할지 여부")]
    [SerializeField] private bool showGridGizmos = true;

    [Tooltip("통로 색상")]
    [SerializeField] private Color walkableColor = new Color(0, 1, 0, 0.3f);

    [Tooltip("벽 색상")]
    [SerializeField] private Color wallColor = new Color(1, 0, 0, 0.3f);

    [Header("벽 감지 설정")]
    [Tooltip("벽을 감지할 레이어")]
    [SerializeField] private LayerMask wallLayer;

    [Tooltip("벽 감지 반경 (셀 크기의 비율, 0.4~0.5 권장)")]
    [SerializeField] private float wallCheckRadius = 0.45f;

    [Header("디버그 설정")]
    [Tooltip("그리드 스캔 결과를 콘솔에 출력")]
    [SerializeField] private bool showScanDebugInfo = false;

    [Tooltip("경로 찾기 실패 시 디버그 정보 출력")]
    [SerializeField] private bool showPathfindingDebug = true;

    // 그리드 데이터: true = 통행 가능, false = 벽
    private bool[,] gridData;

    // 그리드의 중심 위치 (월드 좌표)
    private Vector3 gridCenter;

    // 경로 찾기 통계 (디버깅용)
    private int totalPathRequests = 0;
    private int successfulPaths = 0;
    private int failedPaths = 0;

    protected override void Awake()
    {
        base.Awake();
        InitializeGrid();
    }

    /// <summary>
    /// 그리드를 초기화하고 벽 정보를 스캔합니다.
    /// 게임 시작 시 한 번 호출되어 전체 맵의 격자 데이터를 구축합니다.
    /// </summary>
    private void InitializeGrid()
    {
        gridData = new bool[gridWidth, gridHeight];

        // 판의 중심점 계산 (세로로 세운 판이므로 Y-Z 평면)
        if (boardTransform != null)
        {
            gridCenter = boardTransform.position;
        }
        else
        {
            gridCenter = Vector3.zero;
        }

        // 오프셋 적용
        gridCenter += new Vector3(gridOffsetX, gridOffsetY, gridOffsetZ);

        // 모든 셀을 스캔하여 벽인지 통로인지 판단
        ScanGrid();

        Debug.Log($"GridManager 초기화 완료: {gridWidth}x{gridHeight} 그리드, 중심: {gridCenter}");
    }

    /// <summary>
    /// 그리드를 스캔하여 각 셀이 벽인지 통로인지 판단합니다.
    /// Physics.OverlapSphere를 사용하여 벽 Collider만 정확하게 감지합니다.
    /// 팩맨이나 고스트는 무시합니다.
    /// </summary>
    private void ScanGrid()
    {
        int walkableCount = 0;
        int wallCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = GridToWorldPosition(x, z);

                // 해당 위치의 모든 Collider를 감지
                Collider[] colliders = Physics.OverlapSphere(worldPos, cellSize * wallCheckRadius, wallLayer);

                // 벽인지 판단 (Draggable, Ghost 태그는 제외)
                bool hasWall = false;
                foreach (Collider col in colliders)
                {
                    // 팩맨, 고스트, 코인 등은 벽이 아니므로 무시
                    if (col.CompareTag("Draggable") ||
                        col.CompareTag("Ghost") ||
                        col.CompareTag("Coin"))
                        //col.CompareTag("PowerPellet"))
                    {
                        continue;
                    }

                    // 실제 벽 발견
                    hasWall = true;
                    break;
                }

                // 벽이 없으면 통행 가능
                gridData[x, z] = !hasWall;

                if (gridData[x, z])
                    walkableCount++;
                else
                    wallCount++;

                // 디버그 정보 출력 (옵션)
                if (showScanDebugInfo && hasWall)
                {
                    Debug.Log($"벽 감지: Grid({x}, {z}) at {worldPos}");
                }
            }
        }

        Debug.Log($"그리드 스캔 완료 - 통로: {walkableCount}개, 벽: {wallCount}개");
    }

    /// <summary>
    /// 그리드를 재스캔합니다. (런타임 중 벽이 변경될 때 호출)
    /// 동적으로 맵이 변경되는 경우에만 사용하세요.
    /// </summary>
    public void RescanGrid()
    {
        ScanGrid();
        Debug.Log("그리드 재스캔 완료");
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다.
    /// 세로로 세운 판이므로 X-Z 그리드를 X-Y 월드 좌표로 매핑합니다.
    /// </summary>
    public Vector3 GridToWorldPosition(int gridX, int gridZ)
    {
        float worldX = gridCenter.x + (gridX - gridWidth / 2f) * cellSize;
        float worldY = gridCenter.y + (gridZ - gridHeight / 2f) * cellSize;

        return new Vector3(worldX, worldY, gridCenter.z);
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// 세로로 세운 판이므로 X-Y 월드 좌표를 X-Z 그리드로 매핑합니다.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int gridX = Mathf.RoundToInt((worldPosition.x - gridCenter.x) / cellSize + gridWidth / 2f);
        int gridZ = Mathf.RoundToInt((worldPosition.y - gridCenter.y) / cellSize + gridHeight / 2f);

        return new Vector2Int(gridX, gridZ);
    }

    /// <summary>
    /// 해당 그리드 좌표가 유효한 범위 내에 있는지 확인합니다.
    /// 경로 찾기 및 이동 시 맵 경계를 벗어나지 않도록 검사합니다.
    /// </summary>
    public bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    /// <summary>
    /// 해당 그리드 좌표가 통행 가능한지 확인합니다.
    /// 범위를 벗어나거나 벽이 있으면 false를 반환합니다.
    /// </summary>
    public bool IsWalkable(int x, int z)
    {
        if (!IsValidGridPosition(x, z))
            return false;

        return gridData[x, z];
    }

    /// <summary>
    /// 특정 그리드 셀을 벽으로 설정합니다.
    /// 동적으로 맵을 수정할 때 사용합니다.
    /// </summary>
    public void SetWall(int x, int z, bool isWall)
    {
        if (IsValidGridPosition(x, z))
        {
            gridData[x, z] = !isWall;
        }
    }

    /// <summary>
    /// A* 알고리즘을 사용하여 시작점에서 목표점까지의 최단 경로를 찾습니다.
    /// 월드 좌표를 입력받아 격자 좌표로 변환한 후 경로를 탐색합니다.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        totalPathRequests++;

        Vector2Int startGrid = WorldToGridPosition(startPos);
        Vector2Int targetGrid = WorldToGridPosition(targetPos);

        // 목표점이 벽이면 가장 가까운 통로 찾기
        if (!IsWalkable(targetGrid.x, targetGrid.y))
        {
            Vector2Int nearestWalkable = FindNearestWalkableGrid(targetGrid);

            if (nearestWalkable != targetGrid)
            {
                if (showPathfindingDebug)
                {
                    Debug.Log($"목표점 보정: Grid({targetGrid.x}, {targetGrid.y}) -> Grid({nearestWalkable.x}, {nearestWalkable.y})");
                }
                targetGrid = nearestWalkable;
            }
            else if (showPathfindingDebug)
            {
                Debug.LogWarning($"경로 찾기 실패: 목표점 근처에 통로가 없습니다. Grid({targetGrid.x}, {targetGrid.y}) at {targetPos}");
            }
        }

        // 시작점이 벽인 경우에도 보정
        if (!IsWalkable(startGrid.x, startGrid.y))
        {
            Vector2Int nearestWalkable = FindNearestWalkableGrid(startGrid);

            if (nearestWalkable != startGrid)
            {
                if (showPathfindingDebug)
                {
                    Debug.Log($"시작점 보정: Grid({startGrid.x}, {startGrid.y}) -> Grid({nearestWalkable.x}, {nearestWalkable.y})");
                }
                startGrid = nearestWalkable;
            }
            else if (showPathfindingDebug)
            {
                Debug.LogWarning($"경로 찾기 실패: 시작점 근처에 통로가 없습니다. Grid({startGrid.x}, {startGrid.y}) at {startPos}");
            }
        }

        List<Vector3> rawPath = FindPath(startGrid.x, startGrid.y, targetGrid.x, targetGrid.y);

        // 경로 찾기 통계 업데이트
        if (rawPath.Count > 0)
        {
            successfulPaths++;
        }
        else
        {
            failedPaths++;

            if (showPathfindingDebug)
            {
                Debug.LogWarning($"경로 찾기 실패: Grid({startGrid.x}, {startGrid.y}) -> Grid({targetGrid.x}, {targetGrid.y})");
                Debug.LogWarning($"경로 찾기 통계 - 총: {totalPathRequests}, 성공: {successfulPaths}, 실패: {failedPaths}");
            }
        }

        // 경로 스무딩 적용 (기본적으로 비활성화 권장)
        if (enablePathSmoothing && rawPath != null && rawPath.Count > 2)
        {
            return SmoothPath(rawPath);
        }

        return rawPath;
    }

    /// <summary>
    /// 주어진 격자 위치에서 가장 가까운 통행 가능한 격자를 찾습니다.
    /// BFS(너비 우선 탐색)를 사용하여 효율적으로 탐색합니다.
    /// </summary>
    private Vector2Int FindNearestWalkableGrid(Vector2Int center)
    {
        // 이미 통행 가능하면 그대로 반환
        if (IsWalkable(center.x, center.y))
            return center;

        // BFS를 위한 큐와 방문 체크
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(center);
        visited.Add(center);

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // 위
            new Vector2Int(0, -1),  // 아래
            new Vector2Int(-1, 0),  // 왼쪽
            new Vector2Int(1, 0)    // 오른쪽
        };

        // 최대 탐색 범위 제한 (성능 보호)
        int maxSearchRadius = 10;
        int searchCount = 0;
        int maxSearchCount = maxSearchRadius * maxSearchRadius;

        while (queue.Count > 0 && searchCount < maxSearchCount)
        {
            searchCount++;
            Vector2Int current = queue.Dequeue();

            // 통행 가능한 격자를 찾으면 반환
            if (IsWalkable(current.x, current.y))
            {
                return current;
            }

            // 인접한 4방향 탐색
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;

                // 유효한 범위이고 아직 방문하지 않았으면 큐에 추가
                if (IsValidGridPosition(neighbor.x, neighbor.y) && !visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        // 통로를 찾지 못하면 원래 위치 반환
        return center;
    }

    /// <summary>
    /// A* 알고리즘을 사용하여 경로를 찾습니다. (그리드 좌표 기반)
    /// 4방향(상하좌우)만 탐색하여 대각선 이동을 방지합니다.
    /// </summary>
    public List<Vector3> FindPath(int startX, int startZ, int targetX, int targetZ)
    {
        List<Vector3> path = new List<Vector3>();

        // 시작점이나 목표점이 유효하지 않으면 빈 경로 반환
        if (!IsWalkable(startX, startZ) || !IsWalkable(targetX, targetZ))
        {
            return path;
        }

        // A* 알고리즘에 사용할 자료구조
        Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>();
        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Vector2Int startNode = new Vector2Int(startX, startZ);
        Vector2Int targetNode = new Vector2Int(targetX, targetZ);

        // 시작 노드 생성
        PathNode startPathNode = new PathNode
        {
            position = startNode,
            gCost = 0,
            hCost = GetHeuristic(startNode, targetNode),
            parent = null
        };

        allNodes[startNode] = startPathNode;
        openList.Add(startPathNode);

        // A* 메인 루프
        int iterations = 0;
        int maxIterations = gridWidth * gridHeight; // 무한 루프 방지

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // fCost가 가장 낮은 노드 선택
            PathNode currentNode = GetLowestFCostNode(openList);

            // 목표 도달 시 경로 재구성
            if (currentNode.position == targetNode)
            {
                return ReconstructPath(currentNode);
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);

            // *** 중요: 4방향(상하좌우)만 탐색 - 대각선 이동 완전 방지 ***
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 위
                new Vector2Int(0, -1),  // 아래
                new Vector2Int(-1, 0),  // 왼쪽
                new Vector2Int(1, 0)    // 오른쪽
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentNode.position + dir;

                // 유효하지 않거나 벽이거나 이미 탐색한 노드는 스킵
                if (!IsWalkable(neighborPos.x, neighborPos.y) || closedSet.Contains(neighborPos))
                {
                    continue;
                }

                float tentativeGCost = currentNode.gCost + 1;

                // 새로운 노드이거나 더 나은 경로를 찾은 경우
                if (!allNodes.ContainsKey(neighborPos))
                {
                    PathNode neighborNode = new PathNode
                    {
                        position = neighborPos,
                        gCost = tentativeGCost,
                        hCost = GetHeuristic(neighborPos, targetNode),
                        parent = currentNode
                    };

                    allNodes[neighborPos] = neighborNode;
                    openList.Add(neighborNode);
                }
                else
                {
                    PathNode neighborNode = allNodes[neighborPos];

                    if (tentativeGCost < neighborNode.gCost)
                    {
                        neighborNode.gCost = tentativeGCost;
                        neighborNode.parent = currentNode;
                    }
                }
            }
        }

        // 최대 반복 횟수 초과 경고
        if (iterations >= maxIterations && showPathfindingDebug)
        {
            Debug.LogWarning($"A* 알고리즘이 최대 반복 횟수({maxIterations})에 도달했습니다.");
        }

        // 경로를 찾지 못한 경우 빈 리스트 반환
        return path;
    }

    /// <summary>
    /// 경로를 스무딩하여 불필요한 웨이포인트를 제거합니다.
    /// Line of Sight 방식을 사용하지만, 벽 체크를 엄격하게 수행합니다.
    /// 주의: 이 기능은 벽 뚫기를 유발할 수 있으므로 기본적으로 비활성화하는 것을 권장합니다.
    /// </summary>
    private List<Vector3> SmoothPath(List<Vector3> originalPath)
    {
        if (originalPath == null || originalPath.Count < 3)
            return originalPath;

        List<Vector3> smoothedPath = new List<Vector3>();

        // 시작점은 항상 포함
        smoothedPath.Add(originalPath[0]);

        int currentIndex = 0;

        // Line of Sight 방식: 현재 위치에서 볼 수 있는 가장 먼 지점까지 직선으로 연결
        while (currentIndex < originalPath.Count - 1)
        {
            int farthestVisible = currentIndex;

            // 현재 위치에서 가장 먼 보이는 지점 찾기
            for (int i = originalPath.Count - 1; i > currentIndex; i--)
            {
                if (HasLineOfSight(originalPath[currentIndex], originalPath[i]))
                {
                    farthestVisible = i;
                    break;
                }
            }

            // 다음 지점으로 이동
            currentIndex = farthestVisible;

            // 목표 지점이 아니면 경로에 추가
            if (currentIndex < originalPath.Count - 1)
            {
                smoothedPath.Add(originalPath[currentIndex]);
            }
        }

        // 목표점은 항상 포함
        smoothedPath.Add(originalPath[originalPath.Count - 1]);

        return smoothedPath;
    }

    /// <summary>
    /// 두 지점 사이에 시야가 확보되는지 확인합니다. (벽이 없는지)
    /// 시작점과 끝점 사이의 모든 격자를 체크하여 벽 뚫기를 방지합니다.
    /// </summary>
    private bool HasLineOfSight(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // 시작점과 끝점 사이를 충분히 촘촘하게 체크 (셀 크기의 절반 간격)
        int checkPoints = Mathf.CeilToInt(distance / (cellSize * 0.3f));

        for (int i = 0; i <= checkPoints; i++)
        {
            float t = i / (float)checkPoints;
            Vector3 checkPos = Vector3.Lerp(start, end, t);

            Vector2Int gridPos = WorldToGridPosition(checkPos);

            // 벽이 있으면 시야가 막힘
            if (!IsWalkable(gridPos.x, gridPos.y))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 휴리스틱 함수: 맨해튼 거리를 사용합니다.
    /// A* 알고리즘에서 목표까지의 예상 거리를 계산할 때 사용합니다.
    /// 4방향 이동이므로 맨해튼 거리가 가장 적합합니다.
    /// </summary>
    private float GetHeuristic(Vector2Int from, Vector2Int to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    /// <summary>
    /// OpenList에서 fCost가 가장 낮은 노드를 찾습니다.
    /// fCost = gCost(시작점에서의 거리) + hCost(목표까지의 예상 거리)
    /// 실무에서는 Priority Queue(MinHeap)를 사용하여 성능을 최적화합니다.
    /// </summary>
    private PathNode GetLowestFCostNode(List<PathNode> openList)
    {
        PathNode lowest = openList[0];

        for (int i = 1; i < openList.Count; i++)
        {
            if (openList[i].fCost < lowest.fCost)
            {
                lowest = openList[i];
            }
            // fCost가 같으면 hCost가 낮은 것을 선택 (목표에 더 가까운 것)
            else if (openList[i].fCost == lowest.fCost && openList[i].hCost < lowest.hCost)
            {
                lowest = openList[i];
            }
        }

        return lowest;
    }

    /// <summary>
    /// 목표 노드에서 시작 노드까지 역추적하여 경로를 재구성합니다.
    /// parent 포인터를 따라가면서 경로를 만들고, 뒤집어서 시작->목표 순서로 반환합니다.
    /// </summary>
    private List<Vector3> ReconstructPath(PathNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        while (currentNode != null)
        {
            Vector3 worldPos = GridToWorldPosition(currentNode.position.x, currentNode.position.y);
            path.Add(worldPos);
            currentNode = currentNode.parent;
        }

        // 경로를 뒤집어서 시작점 -> 목표점 순서로 만듦
        path.Reverse();

        // 시작점은 제거 (현재 위치이므로 불필요)
        if (path.Count > 0)
        {
            path.RemoveAt(0);
        }

        return path;
    }

    /// <summary>
    /// Inspector 값이 변경될 때 호출되어 그리드를 실시간으로 업데이트합니다.
    /// 에디터에서 오프셋 값을 조정할 때 실시간으로 그리드 위치를 확인할 수 있습니다.
    /// </summary>
    private void OnValidate()
    {
        // Play 모드가 아닐 때만 (에디터 모드)
        if (!Application.isPlaying)
        {
            // gridCenter 재계산
            if (boardTransform != null)
            {
                gridCenter = boardTransform.position;
            }
            else
            {
                gridCenter = Vector3.zero;
            }

            // 오프셋 적용
            gridCenter += new Vector3(gridOffsetX, gridOffsetY, gridOffsetZ);
        }
    }

    /// <summary>
    /// Scene 뷰에서 그리드를 시각화합니다.
    /// Play 모드에서는 실제 벽/통로 데이터를 색상으로 표시합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGridGizmos)
            return;

        // Play 모드가 아닐 때는 gridCenter를 실시간으로 계산
        Vector3 drawCenter = gridCenter;
        if (!Application.isPlaying)
        {
            if (boardTransform != null)
            {
                drawCenter = boardTransform.position;
            }
            else
            {
                drawCenter = Vector3.zero;
            }
            drawCenter += new Vector3(gridOffsetX, gridOffsetY, gridOffsetZ);
        }

        // gridData가 있으면 실제 데이터 기반, 없으면 미리보기
        if (gridData != null && Application.isPlaying)
        {
            // Play 모드: 실제 그리드 데이터 표시
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 worldPos = GridToWorldPositionWithCenter(x, z, drawCenter);

                    // 통로는 초록색, 벽은 빨간색
                    Gizmos.color = gridData[x, z] ? walkableColor : wallColor;
                    Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.9f);
                }
            }
        }
        else
        {
            // 에디터 모드: 그리드 미리보기
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 worldPos = GridToWorldPositionWithCenter(x, z, drawCenter);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                }
            }
        }
    }

    /// <summary>
    /// 특정 중심점을 기준으로 그리드 좌표를 월드 좌표로 변환합니다.
    /// OnDrawGizmos에서 에디터/플레이 모드에 관계없이 그리드를 그리기 위해 사용합니다.
    /// </summary>
    private Vector3 GridToWorldPositionWithCenter(int gridX, int gridZ, Vector3 center)
    {
        float worldX = center.x + (gridX - gridWidth / 2f) * cellSize;
        float worldY = center.y + (gridZ - gridHeight / 2f) * cellSize;

        return new Vector3(worldX, worldY, center.z);
    }

    // ===== Public 유틸리티 메서드 =====

    /// <summary>
    /// 경로 찾기 통계를 콘솔에 출력합니다.
    /// 디버깅 시 경로 찾기 성능을 확인할 때 사용합니다.
    /// </summary>
    public void PrintPathfindingStats()
    {
        float successRate = totalPathRequests > 0 ? (successfulPaths / (float)totalPathRequests) * 100f : 0f;
        Debug.Log($"=== 경로 찾기 통계 ===");
        Debug.Log($"총 요청: {totalPathRequests}");
        Debug.Log($"성공: {successfulPaths} ({successRate:F1}%)");
        Debug.Log($"실패: {failedPaths}");
    }

    /// <summary>
    /// 통계를 초기화합니다.
    /// </summary>
    public void ResetStats()
    {
        totalPathRequests = 0;
        successfulPaths = 0;
        failedPaths = 0;
    }

    /// <summary>
    /// A* 알고리즘에 사용되는 경로 노드 클래스입니다.
    /// 각 격자 위치의 비용 정보와 부모 노드를 저장합니다.
    /// </summary>
    private class PathNode
    {
        public Vector2Int position;    // 격자 좌표
        public float gCost;            // 시작점에서 이 노드까지의 실제 비용
        public float hCost;            // 이 노드에서 목표까지의 예상 비용
        public float fCost => gCost + hCost;  // 총 비용 (gCost + hCost)
        public PathNode parent;        // 경로 역추적을 위한 부모 노드
    }
}