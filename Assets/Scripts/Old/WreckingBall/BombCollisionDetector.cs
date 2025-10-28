using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// LimitLine에 부착되어 폭탄 및 Draggable 오브젝트의 충돌을 감지하고 이벤트를 발생시킵니다.
/// 충돌한 콜라이더의 '부모 루트' 태그를 기준으로 Draggable 여부를 판단합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BombCollisionDetector : MonoBehaviour
{
    [Header("Collision Detection")]
    [SerializeField] private string bombTag = "Bomb";
    [SerializeField] private string draggableTag = "Draggable";

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onBombCollision;

    [Header("Visual Effects")]
    [SerializeField] private GameObject explosionVFX;
    [Tooltip("VFX가 자동으로 소멸되는 시간(초)입니다. 0이면 자동 소멸 안 함.")]
    [SerializeField] private float vfxLifetime = 3.0f;

    [Header("Draggable Settings")]
    [Tooltip("Draggable 오브젝트 파괴 지연 시간(초)입니다.")]
    [SerializeField] private float draggableDestroyDelay = 0.5f;

    // C# Event (코드에서 구독용)
    public static event Action<GameObject> OnBombCollisionDetected;

    // Draggable 트리거 추적
    private HashSet<GameObject> triggeredDraggables = new HashSet<GameObject>();
    private Collider triggerCollider;

    /// <summary>
    /// 트리거된 Draggable 오브젝트의 개수를 반환합니다.
    /// </summary>
    public int TriggeredDraggableCount => triggeredDraggables.Count;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        // Collider 검증 및 Trigger 설정 확인
        if (triggerCollider == null)
        {
            Debug.LogError($"[BombCollisionDetector] {gameObject.name}에 Collider가 없습니다!");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[BombCollisionDetector] {gameObject.name}의 Collider가 Trigger로 설정되어 있지 않습니다. 자동으로 Trigger를 활성화합니다.");
            triggerCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// 충돌체의 '루트 오브젝트' 태그를 검사합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // [디버그 1] (주석 처리)
        // Debug.LogWarning($"[DEBUG] OnTriggerEnter: 충돌 감지! 충돌체: {other.name}, 충돌체 태그: {other.tag}");

        // 1. 충돌체의 Rigidbody 루트를 찾습니다.
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        GameObject rootObject = (rb != null) ? rb.gameObject : other.gameObject;

        if (rootObject == null)
        {
            // Debug.LogError("[DEBUG] 루트 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // [디버그 2] (주석 처리)
        // Debug.LogWarning($"[DEBUG] 루트 오브젝트 확인: {rootObject.name}, 루트 태그: {rootObject.tag}");

        // 2. '루트 오브젝트'의 태그를 기준으로 분기합니다.
        if (rootObject.CompareTag(bombTag))
        {
            // Debug.LogWarning($"[DEBUG] 'Bomb' 루트 태그 감지됨: {rootObject.name}");
            HandleBombTrigger(rootObject, other.ClosestPoint(transform.position));
        }
        else if (rootObject.CompareTag(draggableTag))
        {
            // [디버그 3] (주석 처리)
            // Debug.LogWarning($"[DEBUG] 'Draggable' 루트 태그 확인! (충돌체: {other.name})");
            HandleDraggableTrigger(rootObject, other.ClosestPoint(transform.position));
        }
        else
        {
            // [디버그 4] (주석 처리)
            // Debug.LogWarning($"[DEBUG] 설정된 태그({bombTag}, {draggableTag})가 아닙니다. 무시. (감지된 루트 태그: {rootObject.tag})");
        }
    }

    private void HandleBombTrigger(GameObject bomb, Vector3 contactPoint)
    {
        Debug.Log($"[BombCollisionDetector] {gameObject.name}이(가) 폭탄 {bomb.name}을(를) 감지! 폭발 요청 전송.");

        // VFX 생성 (접촉 지점)
        if (explosionVFX != null)
        {
            GameObject vfxInstance = Instantiate(explosionVFX, contactPoint, Quaternion.identity);

            // ParticleSystem 컴포넌트 찾아서 재생
            ParticleSystem particleSystem = vfxInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                // 시뮬레이션 공간 확인 및 수정
                var main = particleSystem.main;
                if (main.simulationSpace == ParticleSystemSimulationSpace.Local)
                {
                    Debug.LogWarning($"[BombCollisionDetector] ParticleSystem이 Local 시뮬레이션 공간을 사용 중입니다. World로 변경합니다.");
                    var mainModule = particleSystem.main;
                    mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
                }

                particleSystem.Play();
            }
            else
            {
                // 자식 오브젝트에 ParticleSystem이 있을 수 있음
                ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
                if (particleSystems.Length > 0)
                {
                    foreach (var ps in particleSystems)
                    {
                        // 각 파티클 시스템의 시뮬레이션 공간 확인
                        var main = ps.main;
                        if (main.simulationSpace == ParticleSystemSimulationSpace.Local)
                        {
                            var mainModule = ps.main;
                            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
                        }

                        ps.Play();
                    }
                }
                else
                {
                    Debug.LogWarning($"[BombCollisionDetector] VFX 프리팹에 ParticleSystem 컴포넌트가 없습니다!");
                }
            }

            // 자동 소멸
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
        }

        // UnityEvent 호출 (Inspector 연결용)
        onBombCollision?.Invoke(bomb);

        // C# Event 호출 (코드 구독용)
        OnBombCollisionDetected?.Invoke(bomb);
    }

    private void HandleDraggableTrigger(GameObject draggableRootObject, Vector3 contactPoint)
    {
        // [디버그 6] (주석 처리)
        // Debug.LogError($"[DEBUG] HandleDraggableTrigger: '{draggableRootObject.name}' 처리 시작.");

        // 이미 트리거된 오브젝트(루트 기준)인지 확인
        if (triggeredDraggables.Contains(draggableRootObject))
        {
            // [디버그 7] (주석 처리)
            // Debug.LogWarning($"[DEBUG] {draggableRootObject.name}은(는) 이미 처리 목록에 있습니다. (현재 카운트: {triggeredDraggables.Count}). 중복 실행 방지.");
            return;
        }

        // 트리거된 Draggable 기록 (루트 오브젝트 기준)
        triggeredDraggables.Add(draggableRootObject);

        // [디버그 8] (주석 처리)
        // Debug.LogError($"[DEBUG] {draggableRootObject.name}을(를) 처리 목록에 추가. (새 카운트: {triggeredDraggables.Count})");

        // BombManager에 알림
        if (BombManager.Instance != null)
        {
            BombManager.Instance.NotifyDraggableTriggered(draggableRootObject);
        }

        // [디버그 9] (주석 처리)
        // Debug.LogError($"[DEBUG] {draggableRootObject.name}을(를) {draggableDestroyDelay}초 후 'Destroy' 하도록 예약합니다.");

        // 지연 후 파괴 (루트 오브젝트 기준)
        Destroy(draggableRootObject, draggableDestroyDelay);
    }

    /// <summary>
    /// 트리거된 Draggable 개수를 초기화합니다.
    /// </summary>
    public void ResetDraggableCount()
    {
        triggeredDraggables.Clear();
        // Debug.LogWarning($"[BombCollisionDetector] Draggable 카운트 초기화됨.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 태그 존재 여부 확인
        if (!IsTagValid(bombTag))
        {
            Debug.LogWarning($"[BombCollisionDetector] '{bombTag}' 태그가 Tag Manager에 등록되어 있지 않습니다.");
        }

        if (!IsTagValid(draggableTag))
        {
            Debug.LogWarning($"[BombCollisionDetector] '{draggableTag}' 태그가 Tag Manager에 등록되어 있지 않습니다.");
        }

        // Collider가 Trigger인지 확인
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[BombCollisionDetector] Collider의 'Is Trigger'를 활성화해야 합니다!");
        }
    }

    private bool IsTagValid(string tag)
    {
        try
        {
            // 태그가 존재하는지 테스트
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch (UnityException) // 태그가 없으면 UnityException 발생
        {
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 트리거 범위 시각화 (Collider 기준)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

            // Box Collider
            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
                Gizmos.matrix = Matrix4x4.identity; // Gizmos.matrix 복원
            }
            // Sphere Collider
            else if (col is SphereCollider sphereCol)
            {
                // 스케일을 고려한 반지름 계산
                float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Vector3 worldCenter = transform.TransformPoint(sphereCol.center);
                Gizmos.DrawWireSphere(worldCenter, sphereCol.radius * maxScale);
            }
            // Capsule Collider
            else if (col is CapsuleCollider capsuleCol)
            {
                // 캡슐은 복잡하므로 여기서는 간단한 구로 대체하거나 생략합니다.
                // 여기서는 간단히 bounds를 사용합니다.
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            // 기타 Collider
            else
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
#endif
}