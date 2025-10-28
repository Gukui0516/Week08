using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 고스트의 상태를 관리합니다.
/// Normal: 일반 상태 (팩맨 추격)
/// Frightened: 겁먹은 상태 (파워 모드 - 도망, 먹힐 수 있음)
/// Eaten: 먹힌 상태 (홈으로 복귀)
/// </summary>
public class GhostState : MonoBehaviour
{
    /// <summary>
    /// 고스트의 가능한 상태들입니다.
    /// </summary>
    public enum State
    {
        Normal,      // 일반 상태: 팩맨 추격
        Frightened,  // 겁먹은 상태: 도망, 먹힐 수 있음
        Eaten        // 먹힌 상태: 홈으로 복귀
    }

    [Header("현재 상태")]
    [Tooltip("고스트의 현재 상태")]
    [SerializeField] private State currentState = State.Normal;

    [Header("홈 위치 설정")]
    [Tooltip("고스트가 부활할 홈 위치")]
    [SerializeField] private Transform homePosition;

    [Tooltip("홈 위치를 자동으로 찾을지 여부 (GhostHome 태그 사용)")]
    [SerializeField] private bool autoFindHome = true;

    [Header("Frightened 상태 설정")]
    [Tooltip("Frightened 상태에서 깜빡이기 시작하는 남은 시간 (초)")]
    [SerializeField] private float blinkStartTime = 3f;

    [Tooltip("깜빡임 속도")]
    [SerializeField] private float blinkSpeed = 10f;

    [Header("Eaten 상태 설정")]
    [Tooltip("먹힌 후 홈으로 돌아가는 속도 배율")]
    [SerializeField] private float eatenSpeedMultiplier = 2f;

    [Tooltip("홈에 도착했다고 판단하는 거리")]
    [SerializeField] private float homeArrivalDistance = 0.5f;

    [Header("시각적 표현")]
    [Tooltip("Normal 상태 머티리얼")]
    [SerializeField] private Material normalMaterial;

    [Tooltip("Frightened 상태 머티리얼 (파란색)")]
    [SerializeField] private Material frightenedMaterial;

    [Tooltip("Eaten 상태 머티리얼 (눈만 보이는 상태)")]
    [SerializeField] private Material eatenMaterial;

    [Tooltip("고스트의 렌더러 (비어있으면 자동으로 찾음)")]
    [SerializeField] private Renderer ghostRenderer;

    [Header("이벤트")]
    [Tooltip("상태가 변경될 때 호출됩니다")]
    public UnityEvent<State> OnStateChanged;

    [Tooltip("먹혔을 때 호출됩니다")]
    public UnityEvent OnGhostEaten;

    [Tooltip("홈에 도착하여 부활했을 때 호출됩니다")]
    public UnityEvent OnRespawned;

    [Header("디버그")]
    [Tooltip("상태 변경 정보를 콘솔에 출력할지 여부")]
    [SerializeField] private bool showDebugInfo = true;

    // 내부 변수
    private float frightenedTimer = 0f;
    private bool isBlinking = false;
    private Material materialInstance; // 머티리얼 인스턴스
    private Vector3 originalScale;

    void Awake()
    {
        // 렌더러 자동 찾기
        if (ghostRenderer == null)
        {
            ghostRenderer = GetComponent<Renderer>();

            if (ghostRenderer == null)
            {
                ghostRenderer = GetComponentInChildren<Renderer>();
            }
        }

        // 머티리얼 인스턴스 생성
        if (ghostRenderer != null)
        {
            materialInstance = ghostRenderer.material;
        }

        // 홈 위치 자동 찾기
        if (autoFindHome && homePosition == null)
        {
            GameObject homeObject = GameObject.FindGameObjectWithTag("GhostHome");
            if (homeObject != null)
            {
                homePosition = homeObject.transform;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: GhostHome 태그를 가진 오브젝트를 찾을 수 없습니다. 현재 위치를 홈으로 설정합니다.");
                homePosition = transform;
            }
        }

        originalScale = transform.localScale;
    }

    void Start()
    {
        // PacmanGameManager의 파워 모드 이벤트 구독
        if (PacmanGameManager.Instance != null)
        {
            PacmanGameManager.Instance.OnPowerModeStarted.AddListener(OnPowerModeStarted);
            PacmanGameManager.Instance.OnPowerModeEnded.AddListener(OnPowerModeEnded);
        }

        // 초기 상태 설정
        ChangeState(currentState);
    }

    void Update()
    {
        // Frightened 상태에서 타이머 및 깜빡임 처리
        if (currentState == State.Frightened)
        {
            UpdateFrightenedState();
        }
    }

    /// <summary>
    /// Frightened 상태의 타이머와 깜빡임을 업데이트합니다.
    /// </summary>
    private void UpdateFrightenedState()
    {
        if (PacmanGameManager.Instance == null)
            return;

        frightenedTimer = PacmanGameManager.Instance.GetPowerModeTimeRemaining();

        // 남은 시간이 적으면 깜빡이기 시작
        if (frightenedTimer <= blinkStartTime && frightenedTimer > 0)
        {
            if (!isBlinking)
            {
                isBlinking = true;
            }

            // 깜빡임 효과
            float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            if (ghostRenderer != null && materialInstance != null)
            {
                // Normal과 Frightened 머티리얼 사이를 왔다갔다
                if (blinkValue > 0.5f)
                {
                    ghostRenderer.material = frightenedMaterial;
                }
                else
                {
                    ghostRenderer.material = normalMaterial;
                }
            }
        }
    }

    /// <summary>
    /// 고스트의 상태를 변경합니다.
    /// </summary>
    /// <param name="newState">새로운 상태</param>
    public void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        State previousState = currentState;
        currentState = newState;

        // 상태별 처리
        switch (currentState)
        {
            case State.Normal:
                OnEnterNormalState();
                break;

            case State.Frightened:
                OnEnterFrightenedState();
                break;

            case State.Eaten:
                OnEnterEatenState();
                break;
        }

        // 이벤트 호출
        OnStateChanged?.Invoke(currentState);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 상태 변경 {previousState} → {currentState}");
        }
    }

    /// <summary>
    /// Normal 상태로 진입할 때 호출됩니다.
    /// </summary>
    private void OnEnterNormalState()
    {
        isBlinking = false;

        // Normal 머티리얼 적용
        if (ghostRenderer != null && normalMaterial != null)
        {
            ghostRenderer.material = normalMaterial;
        }

        // 원래 크기로 복구
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Frightened 상태로 진입할 때 호출됩니다.
    /// </summary>
    private void OnEnterFrightenedState()
    {
        isBlinking = false;

        // Frightened 머티리얼 적용 (파란색)
        if (ghostRenderer != null && frightenedMaterial != null)
        {
            ghostRenderer.material = frightenedMaterial;
        }

        if (PacmanGameManager.Instance != null)
        {
            frightenedTimer = PacmanGameManager.Instance.GetPowerModeTimeRemaining();
        }
    }

    /// <summary>
    /// Eaten 상태로 진입할 때 호출됩니다.
    /// </summary>
    private void OnEnterEatenState()
    {
        isBlinking = false;

        // Eaten 머티리얼 적용 (눈만 보이는 상태)
        if (ghostRenderer != null && eatenMaterial != null)
        {
            ghostRenderer.material = eatenMaterial;
        }

        // 크기를 약간 줄임 (선택 사항)
        transform.localScale = originalScale * 0.7f;

        // 게임 매니저에 고스트가 먹혔다고 알림
        if (PacmanGameManager.Instance != null)
        {
            PacmanGameManager.Instance.OnGhostEaten();
        }

        OnGhostEaten?.Invoke();
    }

    /// <summary>
    /// 파워 모드가 시작되었을 때 호출됩니다.
    /// </summary>
    private void OnPowerModeStarted()
    {
        // Eaten 상태가 아니면 Frightened로 변경
        if (currentState != State.Eaten)
        {
            ChangeState(State.Frightened);
        }
    }

    /// <summary>
    /// 파워 모드가 종료되었을 때 호출됩니다.
    /// </summary>
    private void OnPowerModeEnded()
    {
        // Frightened 상태였으면 Normal로 복귀
        if (currentState == State.Frightened)
        {
            ChangeState(State.Normal);
        }
    }

    /// <summary>
    /// 팩맨에게 먹혔을 때 호출됩니다.
    /// </summary>
    public void GetEaten()
    {
        if (currentState == State.Frightened)
        {
            ChangeState(State.Eaten);
        }
    }

    /// <summary>
    /// 홈에 도착하여 부활합니다.
    /// </summary>
    public void Respawn()
    {
        ChangeState(State.Normal);

        // 홈 위치로 이동
        if (homePosition != null)
        {
            transform.position = homePosition.position;
        }

        OnRespawned?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 부활!");
        }
    }

    /// <summary>
    /// 홈에 도착했는지 확인합니다.
    /// </summary>
    /// <returns>홈에 도착했으면 true</returns>
    public bool HasReachedHome()
    {
        if (homePosition == null)
            return false;

        float distance = Vector3.Distance(transform.position, homePosition.position);
        return distance <= homeArrivalDistance;
    }

    // ===== Getter 메서드들 =====

    /// <summary>
    /// 현재 상태를 반환합니다.
    /// </summary>
    public State GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Normal 상태인지 확인합니다.
    /// </summary>
    public bool IsNormal()
    {
        return currentState == State.Normal;
    }

    /// <summary>
    /// Frightened 상태인지 확인합니다.
    /// </summary>
    public bool IsFrightened()
    {
        return currentState == State.Frightened;
    }

    /// <summary>
    /// Eaten 상태인지 확인합니다.
    /// </summary>
    public bool IsEaten()
    {
        return currentState == State.Eaten;
    }

    /// <summary>
    /// Eaten 상태일 때의 속도 배율을 반환합니다.
    /// </summary>
    public float GetEatenSpeedMultiplier()
    {
        return eatenSpeedMultiplier;
    }

    /// <summary>
    /// 홈 위치를 반환합니다.
    /// </summary>
    public Vector3 GetHomePosition()
    {
        return homePosition != null ? homePosition.position : transform.position;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PacmanGameManager.Instance != null)
        {
            PacmanGameManager.Instance.OnPowerModeStarted.RemoveListener(OnPowerModeStarted);
            PacmanGameManager.Instance.OnPowerModeEnded.RemoveListener(OnPowerModeEnded);
        }

        // 머티리얼 인스턴스 메모리 해제
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    /// <summary>
    /// Scene 뷰에서 홈 위치와의 연결선을 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (homePosition != null)
        {
            // 고스트 → 홈 위치 연결선
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, homePosition.position);

            // 홈 위치 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(homePosition.position, homeArrivalDistance);
        }
    }
}