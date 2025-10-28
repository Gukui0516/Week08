using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NextStage 씬 전환 트리거 시스템
/// - Bomb가 지정된 영역에 충돌하면 모든 폭탄을 폭발시키고 다음 스테이지로 전환
/// - STAGE ↔ ReverseStage 사이를 자동으로 순환
/// - 로그 시스템을 통해 모든 이벤트 추적
/// </summary>
public class NextStageTrigger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Child Colliders")]
    [Tooltip("충돌을 감지할 자식 오브젝트들의 Collider (Box, Box (1), Box (2), Box (3) 등)")]
    [SerializeField] private Collider[] childColliders;

    [Header("Scene Settings")]
    [Tooltip("STAGE 씬 이름")]
    [SerializeField] private string stageSceneName = "STAGE";

    [Tooltip("ReverseStage 씬 이름")]
    [SerializeField] private string reverseStageSceneName = "ReverseStage";

    [Header("Trigger Settings")]
    [Tooltip("폭발 후 씬 전환까지 대기 시간")]
    [SerializeField] private float delayBeforeLoadScene = 1.0f;

    [Tooltip("Bomb 태그")]
    [SerializeField] private string bombTag = "Bomb";

    [Header("Explosion Settings")]
    [Tooltip("true: 모든 폭탄 동시 폭발, false: 순차적으로 폭발")]
    [SerializeField] private bool explodeAllAtOnce = true;

    [Tooltip("순차 폭발 시 폭탄 사이의 간격 (초)")]
    [SerializeField] private float delayBetweenExplosions = 0.1f;

    [Header("Auto Setup")]
    [Tooltip("체크하면 Start 시 자동으로 자식 Collider를 찾습니다")]
    [SerializeField] private bool autoFindChildColliders = true;

    [Header("Debug")]
    [Tooltip("Unity 콘솔에도 로그 출력 여부")]
    [SerializeField] private bool useUnityDebugLog = true;

    #endregion

    #region Private Fields

    /// <summary>
    /// 씬 전환 트리거 중복 방지 플래그
    /// </summary>
    private bool isTriggered = false;

    /// <summary>
    /// 자식 Collider와 TriggerDetector 매핑 딕셔너리
    /// </summary>
    private Dictionary<Collider, TriggerDetector> detectors = new Dictionary<Collider, TriggerDetector>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // 로그: NextStageTrigger 초기화 시작
        LogSystem.PushLog(LogLevel.INFO, "NextStage_Init", "Started");

        // 자동으로 자식 Collider 찾기
        if (autoFindChildColliders)
        {
            childColliders = GetComponentsInChildren<Collider>();

            // 로그: 자동 탐색 결과
            LogSystem.PushLog(LogLevel.INFO, "NextStage_ColliderFound", childColliders.Length);

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog($"[NextStageTrigger] {childColliders.Length}개의 자식 Collider를 자동으로 찾았습니다.");
            }
        }

        // 각 자식 Collider에 TriggerDetector 추가
        SetupChildColliders();

        // 로그: 초기화 완료
        LogSystem.PushLog(LogLevel.INFO, "NextStage_InitComplete", detectors.Count);
    }

    private void OnDestroy()
    {
        // TriggerDetector 정리
        foreach (var detector in detectors.Values)
        {
            if (detector != null)
            {
                detector.OnTriggerDetected -= OnBombDetected;
            }
        }

        // 로그: NextStageTrigger 정리
        LogSystem.PushLog(LogLevel.DEBUG, "NextStage_Destroyed", gameObject.name);
    }

    #endregion

    #region Setup

    /// <summary>
    /// 자식 Collider들에 TriggerDetector 컴포넌트를 동적으로 추가
    /// 이미 존재하는 경우 재사용하며, 이벤트 연결을 설정
    /// </summary>
    private void SetupChildColliders()
    {
        if (childColliders == null || childColliders.Length == 0)
        {
            // 로그: 에러 - 자식 Collider 없음
            LogSystem.PushLog(LogLevel.ERROR, "NextStage_SetupError", "NoColliders", true);
            return;
        }

        foreach (var col in childColliders)
        {
            if (col == null) continue;

            // 이미 TriggerDetector가 있는지 확인
            TriggerDetector detector = col.GetComponent<TriggerDetector>();

            if (detector == null)
            {
                // 없으면 동적으로 추가
                detector = col.gameObject.AddComponent<TriggerDetector>();

                // 로그: TriggerDetector 추가
                LogSystem.PushLog(LogLevel.DEBUG, "NextStage_DetectorAdded", col.gameObject.name);

                if (useUnityDebugLog)
                {
                    LogSystem.DebugLog($"[NextStageTrigger] {col.gameObject.name}에 TriggerDetector 추가");
                }
            }

            // 이벤트 연결
            detector.bombTag = bombTag;
            detector.OnTriggerDetected += OnBombDetected;

            detectors[col] = detector;
        }

        // 로그: 설정 완료
        LogSystem.PushLog(LogLevel.INFO, "NextStage_DetectorsSetup", detectors.Count);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog($"[NextStageTrigger] {detectors.Count}개의 자식 Collider 설정 완료");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 자식 오브젝트의 TriggerDetector에서 호출되는 이벤트 핸들러
    /// Bomb가 감지되면 씬 전환 프로세스를 시작
    /// </summary>
    /// <param name="bomb">감지된 Bomb 오브젝트</param>
    public void OnBombDetected(GameObject bomb)
    {
        // 이미 트리거된 경우 중복 실행 방지
        if (isTriggered)
        {
            // 로그: 중복 트리거 방지
            LogSystem.PushLog(LogLevel.DEBUG, "NextStage_DuplicateIgnored", bomb != null ? bomb.name : "null");
            return;
        }

        if (bomb == null)
        {
            // 로그: 에러 - null 오브젝트
            LogSystem.PushLog(LogLevel.ERROR, "NextStage_NullBomb", "BombIsNull", true);
            return;
        }

        // 로그: Bomb 감지 (중요 이벤트 - Unity 콘솔 출력)
        LogSystem.PushLog(LogLevel.WARNING, "NextStage_BombDetected", bomb.name, true);
        LogSystem.PushLog(LogLevel.WARNING, "NextStage_BombPosition", bomb.transform.position);
        LogSystem.PushLog(LogLevel.INFO, "NextStage_CurrentScene", SceneManager.GetActiveScene().name);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog($"[NextStageTrigger] Bomb '{bomb.name}'이(가) NextStage에 감지되었습니다!");
        }

        isTriggered = true;
        StartCoroutine(ExplodeAndLoadNextStage(bomb));
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// 현재 씬에 따라 다음 씬 이름을 결정
    /// STAGE → ReverseStage
    /// ReverseStage → STAGE
    /// 그 외 → STAGE (기본값)
    /// </summary>
    /// <returns>다음 씬 이름</returns>
    private string GetNextSceneName()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == stageSceneName)
        {
            return reverseStageSceneName;
        }
        else if (currentSceneName == reverseStageSceneName)
        {
            return stageSceneName;
        }
        else
        {
            // 현재 씬이 STAGE도 ReverseStage도 아닌 경우 STAGE로 이동
            // 로그: 경고 - 예상치 못한 씬
            LogSystem.PushLog(LogLevel.WARNING, "NextStage_UnexpectedScene", currentSceneName, true);

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog($"[NextStageTrigger] 현재 씬 '{currentSceneName}'이(가) 예상하지 못한 씬입니다. STAGE로 이동합니다.");
            }

            return stageSceneName;
        }
    }

    #endregion

    #region Explosion & Scene Transition

    /// <summary>
    /// 모든 폭탄을 폭발시키고 다음 스테이지로 이동하는 코루틴
    /// 
    /// [처리 순서]
    /// 1. 씬에 있는 모든 Bomb 찾기
    /// 2. 설정에 따라 동시/순차 폭발
    /// 3. 대기 시간 후 다음 씬 로드
    /// </summary>
    /// <param name="triggerBomb">트리거를 발동시킨 Bomb (로그용)</param>
    private IEnumerator ExplodeAndLoadNextStage(GameObject triggerBomb)
    {
        // 로그: 폭발 프로세스 시작 (중요 이벤트 - Unity 콘솔 출력)
        LogSystem.PushLog(LogLevel.WARNING, "NextStage_ExplosionStart", "AllBombs", true);
        LogSystem.PushLog(LogLevel.INFO, "NextStage_TriggerBomb", triggerBomb.name);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog("[NextStageTrigger] 모든 폭탄을 터뜨립니다! 💥💥💥");
        }

        // 1. 씬에 있는 모든 Bomb 찾기
        GameObject[] allBombs = GameObject.FindGameObjectsWithTag(bombTag);

        // 로그: 발견된 폭탄 개수
        LogSystem.PushLog(LogLevel.INFO, "NextStage_BombsFound", allBombs.Length);

        if (allBombs.Length > 0)
        {
            if (useUnityDebugLog)
            {
                LogSystem.DebugLog($"[NextStageTrigger] {allBombs.Length}개의 폭탄을 발견했습니다!");
            }

            if (explodeAllAtOnce)
            {
                // 2-1. 모든 폭탄 동시에 폭발
                // 로그: 동시 폭발 모드
                LogSystem.PushLog(LogLevel.INFO, "NextStage_ExplosionMode", "Simultaneous");

                if (useUnityDebugLog)
                {
                    LogSystem.DebugLog("[NextStageTrigger] 동시 폭발 모드!");
                }

                int explodedCount = 0;
                foreach (GameObject b in allBombs)
                {
                    if (ExplodeBomb(b))
                    {
                        explodedCount++;
                    }
                }

                // 로그: 폭발 완료 개수
                LogSystem.PushLog(LogLevel.WARNING, "NextStage_ExplodedCount", explodedCount, true);
            }
            else
            {
                // 2-2. 순차적으로 폭발
                // 로그: 순차 폭발 모드
                LogSystem.PushLog(LogLevel.INFO, "NextStage_ExplosionMode", "Sequential");
                LogSystem.PushLog(LogLevel.INFO, "NextStage_ExplosionDelay", delayBetweenExplosions);

                if (useUnityDebugLog)
                {
                    LogSystem.DebugLog($"[NextStageTrigger] 순차 폭발 모드! (간격: {delayBetweenExplosions}초)");
                }

                int explodedCount = 0;
                foreach (GameObject b in allBombs)
                {
                    if (ExplodeBomb(b))
                    {
                        explodedCount++;
                    }
                    yield return new WaitForSeconds(delayBetweenExplosions);
                }

                // 로그: 폭발 완료 개수
                LogSystem.PushLog(LogLevel.WARNING, "NextStage_ExplodedCount", explodedCount, true);
            }
        }
        else
        {
            // 로그: 경고 - 폭탄 없음
            LogSystem.PushLog(LogLevel.WARNING, "NextStage_NoBombs", "ZeroBombs", true);

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog("[NextStageTrigger] 씬에 폭탄이 없습니다!");
            }
        }

        // 3. 대기 (폭발 연출 시간)
        // 로그: 씬 전환 대기 시작
        LogSystem.PushLog(LogLevel.INFO, "NextStage_TransitionWait", delayBeforeLoadScene);

        yield return new WaitForSeconds(delayBeforeLoadScene);

        // 4. 다음 스테이지로 이동
        string nextScene = GetNextSceneName();
        string currentScene = SceneManager.GetActiveScene().name;

        // 로그: 씬 전환 (중요 이벤트 - Unity 콘솔 출력)
        LogSystem.PushLog(LogLevel.WARNING, "NextStage_SceneTransition", $"{currentScene}->{nextScene}", true);
        LogSystem.PushLog(LogLevel.INFO, "NextStage_FromScene", currentScene);
        LogSystem.PushLog(LogLevel.INFO, "NextStage_ToScene", nextScene);

        if (useUnityDebugLog)
        {
            LogSystem.DebugLog($"[NextStageTrigger] '{currentScene}' → '{nextScene}' 씬으로 이동합니다.");
        }

        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// 개별 폭탄을 폭발시키고 BombManager에 알림
    /// </summary>
    /// <param name="bomb">폭발시킬 Bomb 오브젝트</param>
    /// <returns>성공 여부</returns>
    private bool ExplodeBomb(GameObject bomb)
    {
        if (bomb == null || !bomb.activeInHierarchy)
        {
            // 로그: 폭발 실패 - null 또는 비활성
            LogSystem.PushLog(LogLevel.DEBUG, "NextStage_BombSkipped", bomb != null ? bomb.name : "null");
            return false;
        }

        BombC bombC = bomb.GetComponent<BombC>();
        if (bombC != null)
        {
            // 로그: 개별 폭탄 폭발
            LogSystem.PushLog(LogLevel.WARNING, "NextStage_BombExploded", bomb.name);
            LogSystem.PushLog(LogLevel.DEBUG, "NextStage_BombPosition", bomb.transform.position);
            LogSystem.PushLog(LogLevel.DEBUG, "NextStage_ExplosionMethod", "NextStageTrigger");

            bombC.Explode();

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog($"[NextStageTrigger] {bomb.name} 폭발!");
            }

            // BombManager에 폭발 알림
            if (BombManager.Instance != null)
            {
                BombManager.Instance.NotifyBombExploded(bomb);
            }

            return true;
        }
        else
        {
            // 로그: 에러 - BombC 컴포넌트 없음
            LogSystem.PushLog(LogLevel.ERROR, "NextStage_MissingBombC", bomb.name, true);

            if (useUnityDebugLog)
            {
                LogSystem.DebugLog($"[NextStageTrigger] {bomb.name}에 BombC 컴포넌트가 없습니다!");
            }

            return false;
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // 시각적으로 NextStage 영역 표시
        if (childColliders != null)
        {
            Gizmos.color = Color.green;
            foreach (var col in childColliders)
            {
                if (col != null)
                {
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// 자식 Collider에 동적으로 추가되는 헬퍼 클래스
/// 충돌을 감지하고 이벤트를 발생시킴
/// 
/// [동작 원리]
/// - OnTriggerEnter 또는 OnCollisionEnter로 충돌 감지
/// - bombTag와 일치하는 오브젝트만 처리
/// - 이벤트를 통해 부모 NextStageTrigger에 알림
/// </summary>
public class TriggerDetector : MonoBehaviour
{
    /// <summary>
    /// 감지할 폭탄 태그
    /// </summary>
    public string bombTag = "Bomb";

    /// <summary>
    /// 폭탄 감지 시 발생하는 이벤트
    /// GameObject: 감지된 폭탄 오브젝트
    /// </summary>
    public System.Action<GameObject> OnTriggerDetected;

    /// <summary>
    /// 트리거 충돌 감지 (isTrigger = true인 Collider)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(bombTag))
        {
            OnTriggerDetected?.Invoke(other.gameObject);
        }
    }

    /// <summary>
    /// 일반 충돌 감지 (isTrigger = false인 Collider)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(bombTag))
        {
            OnTriggerDetected?.Invoke(collision.gameObject);
        }
    }
}