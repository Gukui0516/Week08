using UnityEngine;

/// <summary>
/// 대포 포탄 스크립트입니다.
/// 현재는 식별용 태그 확인 및 선택적 충돌 이펙트만 지원합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CannonBall : MonoBehaviour
{
    [Header("Collision Effects")]
    [Tooltip("충돌 시 생성할 이펙트 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject collisionVFXPrefab;
    [Tooltip("VFX가 자동으로 소멸되는 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 2.0f;
    [Tooltip("충돌 시 로그를 출력합니다.")]
    [SerializeField] private bool enableDebugLog = false;
    
    [Header("Collision Settings")]
    [Tooltip("첫 충돌만 이펙트를 생성합니다.")]
    [SerializeField] private bool onlyFirstCollision = true;
    
    private bool hasCollided = false;
    
    private void OnCollisionEnter(Collision collision)
    {
        // 첫 충돌만 처리하는 옵션
        if (onlyFirstCollision && hasCollided)
            return;
        
        hasCollided = true;
        
        // 디버그 로그
        if (enableDebugLog)
        {
            Debug.Log($"[CannonBall] {gameObject.name}이(가) {collision.gameObject.name}에 충돌했습니다. " +
                      $"충돌 지점: {collision.contacts[0].point}, 속도: {GetComponent<Rigidbody>().linearVelocity.magnitude:F2} m/s");
        }
        
        // VFX 생성
        if (collisionVFXPrefab != null && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            GameObject vfx = Instantiate(collisionVFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            
            // ParticleSystem 자동 재생
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            
            // 자동 소멸
            if (vfxLifetime > 0)
            {
                Destroy(vfx, vfxLifetime);
            }
        }
    }
}