using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// LimitLine에 부착되어 폭탄 및 Draggable 오브젝트의 충돌을 감지하고 이벤트를 발생시킵니다.
/// OnTriggerEnter를 사용하여 Bomb 및 Draggable 태그를 가진 오브젝트를 감지합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BombC : MonoBehaviour
{
    [Header("Collision Detection")]
    [Tooltip("폭발을 트리거할 오브젝트의 태그입니다.")]
    [SerializeField] private string bombTag = "Bomb";
    [Tooltip("낙하 감지할 Draggable 오브젝트의 태그입니다.")]
    [SerializeField] private string draggableTag = "Draggable";

    [Header("Events")]
    [Tooltip("충돌 시 발생하는 UnityEvent입니다. Inspector에서 연결할 수 있습니다.")]
    [SerializeField] private UnityEvent<GameObject> onBombCollision;

    [Header("Visual Effects")]
    [Tooltip("충돌 지점에 생성할 폭발 이펙트 프리팹입니다.")]
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

    private void OnTriggerEnter(Collider other)
    {
        // Bomb 태그 체크
        if (other.CompareTag(bombTag))
        {
            GameObject bomb = other.gameObject;
            HandleBombTrigger(bomb, other.ClosestPoint(transform.position));
        }
        // Draggable 태그 체크
        else if (other.CompareTag(draggableTag))
        {
            GameObject draggable = other.gameObject;
            HandleDraggableTrigger(draggable, other.ClosestPoint(transform.position));
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
                Debug.Log($"[BombCollisionDetector] ParticleSystem 재생: {contactPoint}");
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
                    Debug.Log($"[BombCollisionDetector] {particleSystems.Length}개의 ParticleSystem 재생: {contactPoint}");
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

    private void HandleDraggableTrigger(GameObject draggable, Vector3 contactPoint)
    {
        // 이미 트리거된 오브젝트인지 확인
        if (triggeredDraggables.Contains(draggable))
        {
            return;
        }

        // 트리거된 Draggable 기록
        triggeredDraggables.Add(draggable);

        Debug.Log($"[BombCollisionDetector] Draggable 감지: {draggable.name} | 총 트리거된 개수: {triggeredDraggables.Count}");

        // BombManager에 알림
        if (BombManager.Instance != null)
        {
            BombManager.Instance.NotifyDraggableTriggered(draggable);
        }

        // 지연 후 파괴
        Destroy(draggable, draggableDestroyDelay);
    }

    /// <summary>
    /// 트리거된 Draggable 개수를 초기화합니다.
    /// </summary>
    public void ResetDraggableCount()
    {
        triggeredDraggables.Clear();
        Debug.Log($"[BombCollisionDetector] Draggable 카운트 초기화됨.");
    }

    /// <summary>
    /// 폭탄이 터질 때 호출되는 메서드입니다.
    /// </summary>
    public void Explode()
    {
        Debug.Log($"[BombC] 폭탄 {gameObject.name} 폭발!");

        // 폭발 VFX 생성
        if (explosionVFX != null)
        {
            GameObject vfxInstance = Instantiate(explosionVFX, transform.position, Quaternion.identity);

            // VFX 자동 소멸 처리
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
        }

        // 폭발 물리 효과 추가 (예: 주변 오브젝트 밀어내기)
        Collider[] colliders = Physics.OverlapSphere(transform.position, 20.0f); // 폭발 반경 20.0
        foreach (var collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(1000.0f, transform.position, 5.0f, 1.0f, ForceMode.Impulse);
            }
        }

        // 폭탄 오브젝트 제거 -> 따로 처리함
        //Destroy(gameObject);
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
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
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
            }
            // Sphere Collider
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
            }
            // Capsule Collider
            else if (col is CapsuleCollider capsuleCol)
            {
                Gizmos.DrawWireSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
            }
            // 기타 Collider
            else
            {
                Gizmos.DrawWireSphere(transform.position, col.bounds.extents.magnitude);
            }
        }
    }
#endif
}