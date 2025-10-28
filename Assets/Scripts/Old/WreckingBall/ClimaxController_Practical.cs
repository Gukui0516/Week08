using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Tag 기반으로 씬에 배치된 폭탄들을 순차적으로 폭발시키는 컨트롤러입니다.
/// Inspector 버튼으로 자동 검색 후 수동 순서 조정이 가능합니다.
/// [개선] 개별 폭탄 충돌 폭발 지원 (이벤트 기반)
/// </summary>
public class ClimaxController_Advanced : MonoBehaviour
{
    [Header("Target Containers")]
    [Tooltip("파괴할 모든 블록을 포함하는 최상위 부모 오브젝트입니다.")]
    [SerializeField] private Transform jengaBlocksContainer;
    [Tooltip("파괴할 모든 바닥 블록을 포함하는 최상위 부모 오브젝트입니다.")]
    [SerializeField] private Transform floorBlocksContainer;

    [Header("Bomb Management")]
    [Tooltip("폭탄으로 인식할 GameObject의 태그입니다.")]
    [SerializeField] private string bombTag = "Bomb";
    [Tooltip("자동 검색된 폭탄 목록입니다. 순서를 드래그로 조정할 수 있습니다.")]
    [SerializeField] private List<GameObject> bombObjects = new List<GameObject>();
    [Tooltip("모든 폭탄의 점등 시간(초)입니다.")]
    [SerializeField] private float tickingDuration = 3.0f;

    [Header("Explosion Settings")]
    [Tooltip("폭발의 기본 힘입니다.")]
    [SerializeField] private float explosionForce = 500f;
    [Tooltip("폭발 충격파가 미치는 반경입니다.")]
    [SerializeField] private float explosionRadius = 15f;
    [Tooltip("폭발 시 오브젝트를 위로 띄워 올리는 힘을 추가합니다.")]
    [SerializeField] private float upwardModifier = 2.0f;

    [Header("Explosion Timing")]
    [Tooltip("각 폭탄별 폭발 지연 프레임 수입니다. 폭탄 개수와 일치해야 합니다.")]
    [SerializeField] private List<int> explosionDelayFrames = new List<int> { 0, 30, 60 };

    [Header("Advanced FX Settings")]
    [Tooltip("카메라 셰이크를 위한 Cinemachine Impulse Source입니다.")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [Tooltip("폭발 시 생성할 VFX 프리팹입니다.")]
    [SerializeField] private GameObject explosionVFXPrefab;
    [Tooltip("VFX가 자동으로 소멸되는 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 3.0f;
    [Tooltip("히트스탑이 시작되기 전까지 대기할 프레임 수입니다.")]
    [SerializeField] private int hitstopDelayFrames = 50;
    [Tooltip("히트스탑(시간 정지) 지속 시간(초)입니다.")]
    [SerializeField] private float hitstopDuration = 0.2f;
    [Tooltip("히트스탑 종료 후, 원래 시간 속도로 돌아오는 데 걸리는 시간(초)입니다.")]
    [SerializeField] private float timeScaleRecoveryDuration = 1.0f;
    [Tooltip("시간 속도 복구 애니메이션의 형태를 정의하는 커브입니다.")]
    [SerializeField] private AnimationCurve timeScaleRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rigidbody Settings for Floor")]
    [SerializeField]
    private RigidbodySettings floorRigidbodySettings = new RigidbodySettings
    {
        mass = 1f,
        linearDamping = 0.05f,
        angularDamping = 0.05f,
        interpolation = RigidbodyInterpolation.None,
        collisionDetectionMode = CollisionDetectionMode.Discrete
    };

    [System.Serializable]
    public struct RigidbodySettings
    {
        public float mass;
        public float linearDamping;
        public float angularDamping;
        public RigidbodyInterpolation interpolation;
        public CollisionDetectionMode collisionDetectionMode;
    }

    // 폭발 예약 정보
    private class ScheduledExplosion
    {
        public GameObject bombObject;
        public int frameCount;
    }
    public enum ExplosionMode
    {
        FullExplosion,      // floor Rigidbody 추가 O
        CollisionExplosion  // floor 처리 스킵 X
    }
    private HashSet<Rigidbody> processedRigidbodies = new HashSet<Rigidbody>();
    private HashSet<GameObject> explodedBombs = new HashSet<GameObject>();
    private bool isSequenceRunning = false;

    private void OnEnable()
    {
        // [추가] 이벤트 구독
        BombCollisionDetector.OnBombCollisionDetected += HandleBombCollision;
        Debug.Log("[ClimaxController] 폭탄 충돌 이벤트 구독 완료");
    }

    private void OnDisable()
    {
        // [추가] 이벤트 구독 해제
        BombCollisionDetector.OnBombCollisionDetected -= HandleBombCollision;
        Debug.Log("[ClimaxController] 폭탄 충돌 이벤트 구독 해제");
    }

    /// <summary>
    /// [개선] 충돌 감지 시 호출되는 이벤트 핸들러 (CollisionExplosion 모드 사용)
    /// </summary>
    private void HandleBombCollision(GameObject bomb)
    {
        if (bomb == null)
        {
            Debug.LogWarning("[ClimaxController] 폭발 요청된 폭탄이 null입니다.");
            return;
        }

        // 이미 폭발한 폭탄인지 확인
        if (explodedBombs.Contains(bomb))
        {
            Debug.Log($"[ClimaxController] {bomb.name}은(는) 이미 폭발했습니다. 무시합니다.");
            return;
        }

        // 활성화 상태 확인
        if (!bomb.activeInHierarchy)
        {
            Debug.LogWarning($"[클라이맥스컨트롤러] {bomb.name}이(가) 비활성화 상태입니다.");
            return;
        }

        Debug.Log($"[ClimaxController] 충돌 감지: {bomb.name} 즉시 폭발 처리 (CollisionExplosion 모드)");
        ExplosionIndividualBomb(bomb, ExplosionMode.CollisionExplosion);
    }

    /// <summary>
    /// [개선] 개별 폭탄을 즉시 폭발시킵니다. (외부 호출 가능)
    /// </summary>
    /// <param name="bomb">폭발시킬 폭탄 GameObject</param>
    /// <param name="mode">폭발 모드 (기본값: FullExplosion)</param>
    public void ExplosionIndividualBomb(GameObject bomb, ExplosionMode mode = ExplosionMode.FullExplosion)
    {
        if (bomb == null || explodedBombs.Contains(bomb))
        {
            return;
        }

        // 폭발 처리
        explodedBombs.Add(bomb);
        TriggerExplosion(bomb, mode);
    }

    /// <summary>
    /// [Editor 전용] 씬에서 폭탄을 자동 검색하여 이름 순서로 정렬합니다.
    /// </summary>
    public void SearchAndAssignBombs()
    {
#if UNITY_EDITOR
        bombObjects.Clear();
        GameObject[] foundBombs = GameObject.FindGameObjectsWithTag(bombTag);

        if (foundBombs == null || foundBombs.Length == 0)
        {
            Debug.LogWarning($"[ClimaxController] '{bombTag}' 태그를 가진 폭탄이 씬에 없습니다.");
            return;
        }

        // 이름 순서로 정렬
        var sortedBombs = foundBombs.OrderBy(b => b.name).ToList();
        bombObjects.AddRange(sortedBombs);

        Debug.Log($"[ClimaxController] {bombObjects.Count}개의 폭탄을 검색하여 할당했습니다.");

        // Inspector 업데이트
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void StartClimaxSequence()
    {
        if (isSequenceRunning)
        {
            Debug.LogWarning("[ClimaxController] 클라이맥스 시퀀스가 이미 실행 중입니다.");
            return;
        }

        StopAllCoroutines();
        processedRigidbodies.Clear();
        explodedBombs.Clear(); // [추가] 폭발 기록 초기화
        StartCoroutine(ClimaxCoroutine());
    }

    private IEnumerator ClimaxCoroutine()
    {
        isSequenceRunning = true;

        // 유효한 폭탄만 필터링
        List<GameObject> validBombs = new List<GameObject>();
        foreach (var bomb in bombObjects)
        {
            if (bomb != null && bomb.activeInHierarchy && !explodedBombs.Contains(bomb))
            {
                validBombs.Add(bomb);
            }
        }

        if (validBombs.Count == 0)
        {
            Debug.LogError("[ClimaxController] 활성화된 폭탄이 없습니다.");
            isSequenceRunning = false;
            yield break;
        }

        Debug.Log($"[ClimaxController] {validBombs.Count}개의 폭탄을 발견했습니다.");

        // 폭탄에 BombController가 있다면 점등 시작
        foreach (var bomb in validBombs)
        {
            BombController controller = bomb.GetComponent<BombController>();
            if (controller != null)
            {
                controller.StartTicking(tickingDuration);
            }
        }

        // 폭발 스케줄 생성 (bombObjects 순서대로)
        List<ScheduledExplosion> explosionSchedule = new List<ScheduledExplosion>();

        for (int i = 0; i < validBombs.Count; i++)
        {
            int delayFrames = 0;

            // explosionDelayFrames 리스트에서 값 가져오기
            if (i < explosionDelayFrames.Count)
            {
                delayFrames = explosionDelayFrames[i];
            }
            else if (explosionDelayFrames.Count > 0)
            {
                // 리스트보다 폭탄이 많으면 마지막 값 사용
                delayFrames = explosionDelayFrames[explosionDelayFrames.Count - 1] + (i - explosionDelayFrames.Count + 1) * 30;
            }

            explosionSchedule.Add(new ScheduledExplosion
            {
                bombObject = validBombs[i],
                frameCount = delayFrames
            });
        }

        explosionSchedule.Sort((a, b) => a.frameCount.CompareTo(b.frameCount));

        // 점등 대기
        yield return new WaitForSeconds(tickingDuration);

        // 프레임 기반 폭발 처리
        int currentFrame = 0;
        int scheduleIndex = 0;

        while (scheduleIndex < explosionSchedule.Count)
        {
            // 현재 프레임에 폭발할 폭탄들 처리
            while (scheduleIndex < explosionSchedule.Count &&
                   explosionSchedule[scheduleIndex].frameCount == currentFrame)
            {
                var scheduled = explosionSchedule[scheduleIndex];

                // [개선] FullExplosion 모드로 폭발
                ExplosionIndividualBomb(scheduled.bombObject, ExplosionMode.FullExplosion);

                scheduleIndex++;
            }

            // 다음 프레임으로
            currentFrame++;
            yield return null;
        }

        // 히트스탑 효과
        StartCoroutine(HitStopCoroutine());

        isSequenceRunning = false;
    }

    /// <summary>
    /// [개선] 폭발을 실행합니다. ExplosionMode에 따라 처리가 달라집니다.
    /// </summary>
    /// <param name="bombObject">폭발시킬 폭탄</param>
    /// <param name="mode">폭발 모드</param>
    private void TriggerExplosion(GameObject bombObject, ExplosionMode mode)
    {
        if (bombObject == null) return;

        // 폭발 위치는 폭탄 오브젝트의 위치
        Vector3 explosionPos = bombObject.transform.position;

        // VFX 생성 (추가)
        if (explosionVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(explosionVFXPrefab, explosionPos, Quaternion.identity);

            // ParticleSystem 찾아서 재생
            ParticleSystem particleSystem = vfxInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                // 시뮬레이션 공간 확인 및 수정
                var main = particleSystem.main;
                if (main.simulationSpace == ParticleSystemSimulationSpace.Local)
                {
                    var mainModule = particleSystem.main;
                    mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
                }

                particleSystem.Play();
                Debug.Log($"[ClimaxController] ParticleSystem 재생: {explosionPos}");
            }
            else
            {
                // 자식 오브젝트에 ParticleSystem이 있을 수 있음
                ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
                if (particleSystems.Length > 0)
                {
                    foreach (var ps in particleSystems)
                    {
                        var main = ps.main;
                        if (main.simulationSpace == ParticleSystemSimulationSpace.Local)
                        {
                            var mainModule = ps.main;
                            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
                        }

                        ps.Play();
                    }
                    Debug.Log($"[ClimaxController] {particleSystems.Length}개의 ParticleSystem 재생: {explosionPos}");
                }
            }

            // 자동 소멸
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
        }

        // 카메라 셰이크
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        // 모드별 처리
        switch (mode)
        {
            case ExplosionMode.FullExplosion:
                // 스케줄 폭발: 모든 컨테이너 처리 + floor에 Rigidbody 추가
                Debug.Log($"[ClimaxController] FullExplosion 모드: {bombObject.name}");

                if (jengaBlocksContainer != null)
                {
                    ApplyExplosionOptimized(jengaBlocksContainer, explosionPos, explosionForce, explosionRadius, upwardModifier, false);
                }

                if (floorBlocksContainer != null)
                {
                    ApplyExplosionOptimized(floorBlocksContainer, explosionPos, explosionForce, explosionRadius, upwardModifier, true);
                }
                break;

            case ExplosionMode.CollisionExplosion:
                // 충돌 폭발: 젠가 블록만 처리 (floor Rigidbody 추가 스킵)
                Debug.Log($"[ClimaxController] CollisionExplosion 모드: {bombObject.name}");

                if (jengaBlocksContainer != null)
                {
                    ApplyExplosionOptimized(jengaBlocksContainer, explosionPos, explosionForce, explosionRadius, upwardModifier, false);
                }
                // floor 처리 생략 ✅
                break;
        }

        // 폭탄 비활성화 전에 알림!
        BombManager.Instance.NotifyBombExploded(bombObject);

        // 폭탄 비활성화
        BombController controller = bombObject.GetComponent<BombController>();
        if (controller != null)
        {
            controller.Explode();
        }
        else
        {
            // BombController가 없으면 그냥 비활성화
            bombObject.SetActive(false);
        }

        Debug.Log($"[ClimaxController] 폭탄 폭발: {bombObject.name} at {explosionPos}");
    }

    private IEnumerator HitStopCoroutine()
    {
        // 히트스탑 대기
        for (int i = 0; i < hitstopDelayFrames; i++)
        {
            yield return null;
        }

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 히트스탑 유지
        float waitTimer = 0f;
        while (waitTimer < hitstopDuration)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 시간 속도 복구
        float elapsedTime = 0f;
        while (elapsedTime < timeScaleRecoveryDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float curveSamplePoint = Mathf.Clamp01(elapsedTime / timeScaleRecoveryDuration);
            float curveValue = timeScaleRecoveryCurve.Evaluate(curveSamplePoint);

            Time.timeScale = Mathf.Lerp(0f, originalTimeScale, curveValue);

            yield return null;
        }

        Time.timeScale = originalTimeScale;
    }

    /// <summary>
    /// 최적화된 폭발 처리 (큐 기반)
    /// </summary>
    private void ApplyExplosionOptimized(Transform root, Vector3 explosionPos, float force, float radius, float upward, bool addRigidbodyToFloor)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            // Rigidbody 처리
            Rigidbody rb = current.GetComponent<Rigidbody>();

            if (rb == null && addRigidbodyToFloor && current.GetComponent<Collider>() != null)
            {
                rb = current.gameObject.AddComponent<Rigidbody>();
                rb.mass = floorRigidbodySettings.mass;
                rb.linearDamping = floorRigidbodySettings.linearDamping;
                rb.angularDamping = floorRigidbodySettings.angularDamping;
                rb.interpolation = floorRigidbodySettings.interpolation;
                rb.collisionDetectionMode = floorRigidbodySettings.collisionDetectionMode;
            }

            // 중복 처리 방지 및 범위 체크
            if (rb != null && !processedRigidbodies.Contains(rb))
            {
                float distance = Vector3.Distance(explosionPos, rb.position);
                if (distance <= radius)
                {
                    rb.AddExplosionForce(force, explosionPos, radius, upward);
                    processedRigidbodies.Add(rb);
                }
            }

            // 자식들을 큐에 추가
            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 경고: Tag 존재 여부 확인
        if (!IsTagValid(bombTag))
        {
            Debug.LogWarning($"[ClimaxController] '{bombTag}' 태그가 Tag Manager에 등록되어 있지 않습니다.");
        }
    }

    private bool IsTagValid(string tag)
    {
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        // bombObjects 리스트 기반으로 시각화
        if (bombObjects == null || bombObjects.Count == 0) return;

        for (int i = 0; i < bombObjects.Count; i++)
        {
            if (bombObjects[i] == null) continue;

            Vector3 pos = bombObjects[i].transform.position;

            // 폭발 범위 그리기
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(pos, explosionRadius);

            // 폭발 중심점
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos, 0.5f);

            // 폭발 순서 레이블
            int delayFrames = i < explosionDelayFrames.Count ? explosionDelayFrames[i] : 0;
            UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"폭탄 #{i + 1}\n프레임: {delayFrames}\n{bombObjects[i].name}");
        }
    }
#endif
}

