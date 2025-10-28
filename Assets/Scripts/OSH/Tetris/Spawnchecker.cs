using UnityEngine;
using System.Collections;

/// <summary>
/// 블록 스폰 트리거 체커 (로그 시스템 통합 버전)
/// 
/// [주요 기능]
/// - 블록이 스포너 영역을 완전히 벗어났을 때 다음 블록 생성
/// - EXIT 기반: OnTriggerExit 이벤트 사용
/// - 각 블록은 생애 동안 단 한 번만 스폰 트리거
/// - 일반 블록(Cube)과 폭탄 블록(Bomb) 모두 지원
/// 
/// [로그 시스템 통합]
/// - 블록 감지 이벤트 추적
/// - 스폰 트리거 이벤트 기록
/// - 중복 방지 동작 로깅
/// </summary>
public class SpawnChecker : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [Tooltip("테트리스 블록 스포너 참조")]
    [SerializeField] private TetrisBlockSpawner blockSpawner;

    [Header("Settings")]
    [Tooltip("중복 생성 방지 시간 (초)")]
    [SerializeField] private float debounceTime = 2f;

    [Tooltip("게임 시작 후 체커 활성화 대기 시간 (초) - 첫 블록 안정화")]
    [SerializeField] private float startDelay = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool useUnityDebugLog = true;

    #endregion

    #region Private Fields

    /// <summary>
    /// 체커 활성화 여부
    /// startDelay 후 true로 변경됨
    /// </summary>
    private bool isActive = false;

    /// <summary>
    /// 통계: 총 트리거된 스폰 횟수
    /// </summary>
    private int totalSpawnsTriggered = 0;

    /// <summary>
    /// 통계: 무시된 중복 트리거 횟수
    /// </summary>
    private int duplicateTriggersIgnored = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // 로그: SpawnChecker 초기화
        LogSystem.PushLog(LogLevel.INFO, "SpawnChecker_Init", "Started");
        LogSystem.PushLog(LogLevel.INFO, "SpawnChecker_StartDelay", startDelay);
        LogSystem.PushLog(LogLevel.INFO, "SpawnChecker_DebounceTime", debounceTime);

        ValidateComponents();

        // 일정 시간 후 체커 활성화
        Invoke(nameof(ActivateChecker), startDelay);

        if (showDebugLogs && useUnityDebugLog)
        {
            LogSystem.DebugLog($"[SpawnChecker] {startDelay}초 후 활성화 예정");
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// 필수 컴포넌트 유효성 검사
    /// 필수 참조가 없거나 설정이 잘못된 경우 에러 로그 및 비활성화
    /// </summary>
    private void ValidateComponents()
    {
        bool isValid = true;

        if (blockSpawner == null)
        {
            // 로그: 에러 - Spawner 참조 없음
            LogSystem.PushLog(LogLevel.ERROR, "SpawnChecker_ValidationError", "NoBlockSpawner", true);
            isValid = false;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            // 로그: 에러 - BoxCollider 없음
            LogSystem.PushLog(LogLevel.ERROR, "SpawnChecker_ValidationError", "NoBoxCollider", true);
            isValid = false;
        }
        else if (!boxCollider.isTrigger)
        {
            // 로그: 경고 - isTrigger 비활성화
            LogSystem.PushLog(LogLevel.WARNING, "SpawnChecker_ValidationWarning", "IsTriggerDisabled", true);

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog("[SpawnChecker] BoxCollider의 'Is Trigger'를 활성화해주세요!");
            }
        }

        if (!isValid)
        {
            enabled = false;
            LogSystem.PushLog(LogLevel.ERROR, "SpawnChecker_Disabled", "ValidationFailed", true);
        }
        else
        {
            // 로그: 검증 성공
            LogSystem.PushLog(LogLevel.INFO, "SpawnChecker_ValidationSuccess", "AllComponentsValid");
        }
    }

    #endregion

    #region Activation

    /// <summary>
    /// 체커 활성화
    /// startDelay 후 자동으로 호출되어 블록 감지를 시작
    /// </summary>
    private void ActivateChecker()
    {
        isActive = true;

        // 로그: 체커 활성화 (중요 이벤트 - Unity 콘솔 출력)
        LogSystem.PushLog(LogLevel.INFO, "SpawnChecker_Activated", "CheckerReady", true);

        if (showDebugLogs && useUnityDebugLog)
        {
            LogSystem.DebugLog("[SpawnChecker] ✓ 체커 활성화!");
        }
    }

    #endregion

    #region Trigger Detection

    /// <summary>
    /// 블록이 트리거 영역을 완전히 벗어났을 때 호출
    /// 
    /// [처리 단계]
    /// 1. 체커 활성화 확인
    /// 2. 태그 확인 (Cube 또는 Bomb)
    /// 3. 부모 블록 찾기 (일반 블록만)
    /// 4. BlockState 확인
    /// 5. 중복 트리거 방지 체크 (영구적 + 임시적)
    /// 6. 스폰 트리거 및 플래그 설정
    /// 7. 임시 플래그 리셋 예약
    /// </summary>
    /// <param name="other">영역을 벗어난 Collider</param>
    private void OnTriggerExit(Collider other)
    {
        // 체커가 활성화되지 않았으면 무시
        if (!isActive)
        {
            return;
        }

        GameObject parentBlock = null;
        string blockType = "";

        // 1단계: "Cube" 태그인지 확인 (일반 테트리스 블록)
        if (other.CompareTag("Cube"))
        {
            blockType = "Normal";

            // 2단계: 부모 블록 찾기
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                return;
            }

            // 3단계: 부모가 "Draggable" 태그인지 확인
            if (!rb.CompareTag("Draggable"))
            {
                return;
            }

            parentBlock = rb.gameObject;
        }
        // 1-B단계: "Bomb" 태그인지 확인 (3x3 폭탄 블록 - 단일 오브젝트)
        else if (other.CompareTag("Bomb"))
        {
            blockType = "Bomb";
            // 폭탄은 자식이 없는 단일 오브젝트
            parentBlock = other.gameObject;
        }
        else
        {
            // 태그가 "Cube"도 "Bomb"도 아니면 무시
            return;
        }

        // 로그: 블록 감지
        if (showDebugLogs)
        {
            LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_BlockDetected", parentBlock.name);
            LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_BlockType", blockType);
            LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_BlockPosition", parentBlock.transform.position);
        }

        // 4단계: BlockState 확인
        BlockState blockState = parentBlock.GetComponent<BlockState>();
        if (blockState == null)
        {
            // 로그: 경고 - BlockState 없음
            LogSystem.PushLog(LogLevel.WARNING, "SpawnTrigger_MissingBlockState", parentBlock.name, true);

            if (showDebugLogs && useUnityDebugLog)
            {
                LogSystem.DebugLog($"[SpawnChecker] {parentBlock.name}에 BlockState가 없습니다!");
            }
            return;
        }

        // 5단계: 이미 스폰을 트리거한 블록인지 확인 (영구적 체크)
        if (blockState.HasTriggeredSpawn)
        {
            duplicateTriggersIgnored++;

            // 로그: 중복 무시
            if (showDebugLogs)
            {
                LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_DuplicateIgnored", parentBlock.name);
                LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_TotalDuplicates", duplicateTriggersIgnored);

                if (useUnityDebugLog)
                {
                    LogSystem.DebugLog($"[SpawnChecker] ⚠️ {parentBlock.name}은 이미 스폰을 트리거했습니다. 무시.");
                }
            }
            return;
        }

        // 6단계: 임시 중복 방지 (짧은 시간 내 여러 번 호출 방지)
        if (blockState.IsProcessed)
        {
            return;
        }

        // 7단계: 영구적 플래그 설정 (다시는 스폰 안 함)
        blockState.HasTriggeredSpawn = true;
        blockState.IsProcessed = true;

        // 로그: 스폰 트리거 (중요 이벤트 - Unity 콘솔 출력)
        totalSpawnsTriggered++;

        LogSystem.PushLog(LogLevel.WARNING, "SpawnTrigger_Triggered", parentBlock.name, true);
        LogSystem.PushLog(LogLevel.INFO, "SpawnTrigger_BlockType", blockType);
        LogSystem.PushLog(LogLevel.INFO, "SpawnTrigger_BlockPosition", parentBlock.transform.position);
        LogSystem.PushLog(LogLevel.INFO, "SpawnTrigger_TotalCount", totalSpawnsTriggered);

        if (showDebugLogs && useUnityDebugLog)
        {
            LogSystem.DebugLog($"[SpawnChecker] ✓ {blockType} 블록이 영역을 벗어남: {parentBlock.name} → 다음 블록 생성!");
        }

        // 8단계: 다음 블록 생성
        if (blockSpawner != null)
        {
            blockSpawner.SpawnBlockManually();

            // 로그: 스포너 호출
            LogSystem.PushLog(LogLevel.INFO, "SpawnTrigger_SpawnerCalled", "SpawnBlockManually");
        }
        else
        {
            // 로그: 에러 - Spawner 없음
            LogSystem.PushLog(LogLevel.ERROR, "SpawnTrigger_NoSpawner", "BlockSpawnerNull", true);
        }

        // 9단계: 임시 플래그만 리셋 (HasTriggeredSpawn은 유지)
        StartCoroutine(ResetTemporaryFlag(blockState, debounceTime));
    }

    /// <summary>
    /// 임시 디바운스 플래그만 리셋 (영구 플래그는 유지)
    /// debounceTime 후 IsProcessed를 false로 되돌림
    /// </summary>
    /// <param name="blockState">리셋할 BlockState</param>
    /// <param name="delay">대기 시간</param>
    private IEnumerator ResetTemporaryFlag(BlockState blockState, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (blockState != null)
        {
            blockState.IsProcessed = false; // 임시 플래그만 리셋

            // 로그: 디바운스 해제
            if (showDebugLogs)
            {
                LogSystem.PushLog(LogLevel.DEBUG, "SpawnTrigger_DebounceReset", blockState.gameObject.name);

                if (useUnityDebugLog)
                {
                    LogSystem.DebugLog("[SpawnChecker] 임시 디바운스 해제 (HasTriggeredSpawn은 영구 유지)");
                }
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 체커 즉시 활성화 (디버그용)
    /// Invoke를 취소하고 즉시 활성화
    /// </summary>
    public void ForceActivate()
    {
        CancelInvoke(nameof(ActivateChecker));
        isActive = true;

        // 로그: 강제 활성화
        LogSystem.PushLog(LogLevel.DEBUG, "SpawnChecker_ForceActivated", "Manual", true);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog("[SpawnChecker] 강제 활성화!");
        }
    }

    /// <summary>
    /// 체커 비활성화 (디버그용)
    /// 블록 감지를 일시적으로 중단
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        // 로그: 비활성화
        LogSystem.PushLog(LogLevel.DEBUG, "SpawnChecker_Deactivated", "Manual", true);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog("[SpawnChecker] 비활성화!");
        }
    }

    /// <summary>
    /// 통계 정보 반환
    /// </summary>
    /// <returns>트리거된 스폰 수와 무시된 중복 수</returns>
    public string GetStatistics()
    {
        return $"Spawns: {totalSpawnsTriggered} | Duplicates Ignored: {duplicateTriggersIgnored}";
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // 에디터에서 트리거 영역 시각화
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            // 반투명 녹색 박스
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            // 녹색 와이어프레임
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }

    #endregion

    #region Context Menu (Editor Only)

#if UNITY_EDITOR
    [ContextMenu("Force Activate Checker")]
    private void DebugForceActivate()
    {
        ForceActivate();
    }

    [ContextMenu("Deactivate Checker")]
    private void DebugDeactivate()
    {
        Deactivate();
    }

    [ContextMenu("Print Statistics")]
    private void DebugPrintStatistics()
    {
        if (Application.isPlaying)
        {
            LogSystem.DebugLog(GetStatistics());
        }
        else
        {
            Debug.Log("통계는 플레이 모드에서만 확인 가능합니다.");
        }
    }
#endif

    #endregion
}