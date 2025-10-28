using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NextStage 부모 오브젝트에 붙여서 사용합니다.
/// 자식 오브젝트들의 Collider를 인스펙터에 등록하고,
/// Bomb가 충돌하면 폭발시킨 후 다음 스테이지로 전환합니다.
/// STAGE ↔ ReverseStage 사이를 자동으로 전환합니다.
/// </summary>
public class NextStageTrigger : MonoBehaviour
{
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

    private bool isTriggered = false;
    private Dictionary<Collider, TriggerDetector> detectors = new Dictionary<Collider, TriggerDetector>();

    private void Start()
    {
        // 자동으로 자식 Collider 찾기
        if (autoFindChildColliders)
        {
            childColliders = GetComponentsInChildren<Collider>();
            Debug.Log($"[NextStageTrigger] {childColliders.Length}개의 자식 Collider를 자동으로 찾았습니다.");
        }

        // 각 자식 Collider에 TriggerDetector 추가
        SetupChildColliders();
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
    }

    /// <summary>
    /// 자식 Collider들에 TriggerDetector 컴포넌트를 동적으로 추가합니다.
    /// </summary>
    private void SetupChildColliders()
    {
        if (childColliders == null || childColliders.Length == 0)
        {
            Debug.LogWarning("[NextStageTrigger] 자식 Collider가 등록되지 않았습니다!");
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
                Debug.Log($"[NextStageTrigger] {col.gameObject.name}에 TriggerDetector 추가");
            }

            // 이벤트 연결
            detector.bombTag = bombTag;
            detector.OnTriggerDetected += OnBombDetected;

            detectors[col] = detector;
        }

        Debug.Log($"[NextStageTrigger] {detectors.Count}개의 자식 Collider 설정 완료");
    }

    /// <summary>
    /// 자식 오브젝트의 TriggerDetector에서 호출됩니다.
    /// </summary>
    public void OnBombDetected(GameObject bomb)
    {
        // 이미 트리거된 경우 중복 실행 방지
        if (isTriggered)
            return;

        if (bomb == null)
            return;

        Debug.Log($"[NextStageTrigger] Bomb '{bomb.name}'이(가) NextStage에 감지되었습니다!");

        isTriggered = true;
        StartCoroutine(ExplodeAndLoadNextStage(bomb));
    }

    /// <summary>
    /// 현재 씬에 따라 다음 씬 이름을 결정합니다.
    /// STAGE → ReverseStage
    /// ReverseStage → STAGE
    /// </summary>
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
            Debug.LogWarning($"[NextStageTrigger] 현재 씬 '{currentSceneName}'이(가) 예상하지 못한 씬입니다. STAGE로 이동합니다.");
            return stageSceneName;
        }
    }

    private IEnumerator ExplodeAndLoadNextStage(GameObject bomb)
    {
        Debug.Log("[NextStageTrigger] 모든 폭탄을 터뜨립니다! 💥💥💥");

        // 1. 씬에 있는 모든 Bomb 찾기
        GameObject[] allBombs = GameObject.FindGameObjectsWithTag(bombTag);

        if (allBombs.Length > 0)
        {
            Debug.Log($"[NextStageTrigger] {allBombs.Length}개의 폭탄을 발견했습니다!");

            if (explodeAllAtOnce)
            {
                // 2-1. 모든 폭탄 동시에 폭발
                Debug.Log("[NextStageTrigger] 동시 폭발 모드!");
                foreach (GameObject b in allBombs)
                {
                    ExplodeBomb(b);
                }
            }
            else
            {
                // 2-2. 순차적으로 폭발
                Debug.Log($"[NextStageTrigger] 순차 폭발 모드! (간격: {delayBetweenExplosions}초)");
                foreach (GameObject b in allBombs)
                {
                    ExplodeBomb(b);
                    yield return new WaitForSeconds(delayBetweenExplosions);
                }
            }
        }
        else
        {
            Debug.LogWarning("[NextStageTrigger] 씬에 폭탄이 없습니다!");
        }

        // 3. 대기 (폭발 연출 시간)
        yield return new WaitForSeconds(delayBeforeLoadScene);

        // 4. 다음 스테이지로 이동
        string nextScene = GetNextSceneName();
        string currentScene = SceneManager.GetActiveScene().name;

        Debug.Log($"[NextStageTrigger] '{currentScene}' → '{nextScene}' 씬으로 이동합니다.");
        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// 개별 폭탄을 폭발시킵니다.
    /// </summary>
    private void ExplodeBomb(GameObject bomb)
    {
        if (bomb == null || !bomb.activeInHierarchy) return;

        BombC bombC = bomb.GetComponent<BombC>();
        if (bombC != null)
        {
            bombC.Explode();
            Debug.Log($"[NextStageTrigger] {bomb.name} 폭발!");
        }

        // BombManager에 폭발 알림
        if (BombManager.Instance != null)
        {
            BombManager.Instance.NotifyBombExploded(bomb);
        }
    }

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
}

/// <summary>
/// 자식 Collider에 동적으로 추가되는 헬퍼 클래스입니다.
/// 충돌을 감지하고 이벤트를 발생시킵니다.
/// </summary>
public class TriggerDetector : MonoBehaviour
{
    public string bombTag = "Bomb";
    public System.Action<GameObject> OnTriggerDetected;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(bombTag))
        {
            OnTriggerDetected?.Invoke(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(bombTag))
        {
            OnTriggerDetected?.Invoke(collision.gameObject);
        }
    }
}