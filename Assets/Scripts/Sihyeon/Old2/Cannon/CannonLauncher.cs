using UnityEngine;
using System.Collections;

/// <summary>
/// 대포 발사 시스템입니다.
/// [추가] 자동 연속 발사 모드 지원
/// </summary>
public class CannonLauncher : MonoBehaviour
{
    [Header("Cannon Settings")]
    [Tooltip("대포의 포신(발사 방향 기준점)입니다. 비어있으면 자신의 Transform을 사용합니다.")]
    [SerializeField] private Transform cannonBarrel;
    
    [Header("Projectile Settings")]
    [Tooltip("발사할 포탄 프리팹입니다. Rigidbody와 Collider가 필요합니다.")]
    [SerializeField] private GameObject cannonBallPrefab;
    
    [Header("Launch Settings")]
    [Tooltip("포탄에 가해질 발사 힘입니다.")]
    [SerializeField] private float launchForce = 500f;
    
    [Header("Auto Fire Settings")]
    [Tooltip("자동 발사 간격(초)입니다.")]
    [SerializeField] private float autoFireInterval = 2.0f;
    
    [Header("Spawn Settings")]
    [Tooltip("생성된 포탄의 초기 위치 오프셋(로컬 좌표)입니다.")]
    [SerializeField] private Vector3 spawnPositionOffset = Vector3.zero;
    [Tooltip("생성된 포탄의 Rigidbody 설정입니다.")]
    [SerializeField] private RigidbodySettings projectileSettings = new RigidbodySettings
    {
        mass = 10f,
        linearDamping = 0.1f,
        angularDamping = 0.1f,
        interpolation = RigidbodyInterpolation.Interpolate,
        collisionDetectionMode = CollisionDetectionMode.Continuous
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
    
    [Header("Visual Feedback")]
    [Tooltip("발사 시 생성할 VFX 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject launchVFXPrefab;
    [Tooltip("VFX가 자동으로 소멸되는 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 2.0f;
    
    [Header("Collision Settings")]
    [Tooltip("포탄과 대포의 충돌을 영구적으로 무시합니다.")]
    [SerializeField] private bool ignoreSelfCollision = true;
    [Tooltip("충돌 무시 시간(초)입니다. 0이면 영구적으로 무시합니다.")]
    [SerializeField] private float ignoreCollisionDuration = 0f;
    
    [Header("Trajectory Visualization")]
    [Tooltip("궤적을 그릴 때 사용할 점의 개수입니다.")]
    [SerializeField] private int trajectoryPointCount = 30;
    [Tooltip("궤적을 그릴 시간 간격(초)입니다.")]
    [SerializeField] private float trajectoryTimeStep = 0.1f;
    
    private Coroutine autoFireCoroutine;
    private Collider[] cannonColliders;
    
    private void Awake()
    {
        // cannonBarrel이 설정되지 않았다면 자신을 사용
        if (cannonBarrel == null)
        {
            cannonBarrel = transform;
        }
        
        // 프리팹 검증
        if (cannonBallPrefab == null)
        {
            Debug.LogError($"[CannonLauncher] {gameObject.name}에 포탄 프리팹이 할당되지 않았습니다!");
        }
        
        // 대포의 모든 Collider 캐싱 (자신 + 자식 오브젝트)
        cannonColliders = GetComponentsInChildren<Collider>();
        Debug.Log($"[CannonLauncher] 대포 Collider {cannonColliders.Length}개 감지됨");
    }
    
    private void Start()
    {
        // 무조건 자동 발사 시작
        StartAutoFire();
    }
    
    /// <summary>
    /// 자동 연속 발사를 시작합니다. (외부 호출 가능)
    /// </summary>
    public void StartAutoFire()
    {
        if (autoFireCoroutine != null)
        {
            Debug.LogWarning("[CannonLauncher] 자동 발사가 이미 실행 중입니다.");
            return;
        }
        
        autoFireCoroutine = StartCoroutine(AutoFireCoroutine());
        Debug.Log($"[CannonLauncher] 자동 발사 시작! 발사 간격: {autoFireInterval}초");
    }
    
    /// <summary>
    /// 자동 연속 발사를 중지합니다. (외부 호출 가능)
    /// </summary>
    public void StopAutoFire()
    {
        if (autoFireCoroutine != null)
        {
            StopCoroutine(autoFireCoroutine);
            autoFireCoroutine = null;
            Debug.Log("[CannonLauncher] 자동 발사 중지");
        }
    }
    
    /// <summary>
    /// 자동 연속 발사 코루틴입니다.
    /// </summary>
    private IEnumerator AutoFireCoroutine()
    {
        while (true)  // 무한 반복
        {
            // 포탄 즉시 발사
            LaunchProjectile();
            
            // 다음 발사까지 대기
            yield return new WaitForSeconds(autoFireInterval);
        }
    }
    
    /// <summary>
    /// 포탄을 생성하고 발사합니다.
    /// </summary>
    private void LaunchProjectile()
    {
        if (cannonBallPrefab == null)
        {
            Debug.LogError("[CannonLauncher] 포탄 프리팹이 없습니다!");
            return;
        }
        
        // 생성 위치 계산 (대포 포신 위치 + 오프셋)
        Vector3 spawnPosition = cannonBarrel.position + cannonBarrel.TransformDirection(spawnPositionOffset);
        
        // 포탄 생성
        GameObject projectile = Instantiate(cannonBallPrefab, spawnPosition, cannonBarrel.rotation);
        
        // Rigidbody 설정
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
            Debug.LogWarning($"[CannonLauncher] {cannonBallPrefab.name}에 Rigidbody가 없어 자동으로 추가했습니다.");
        }
        
        // ✅ 중요: Kinematic 해제 및 중력 활성화
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Rigidbody 속성 적용
        rb.mass = projectileSettings.mass;
        rb.linearDamping = projectileSettings.linearDamping;
        rb.angularDamping = projectileSettings.angularDamping;
        rb.interpolation = projectileSettings.interpolation;
        rb.collisionDetectionMode = projectileSettings.collisionDetectionMode;
        
        // ✅ 대포와의 충돌 무시 설정
        if (ignoreSelfCollision)
        {
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null && cannonColliders != null)
            {
                foreach (Collider cannonCollider in cannonColliders)
                {
                    if (cannonCollider != null && cannonCollider.enabled)
                    {
                        Physics.IgnoreCollision(projectileCollider, cannonCollider, true);
                    }
                }
                
                // 일정 시간 후 충돌 다시 활성화 (옵션)
                if (ignoreCollisionDuration > 0f)
                {
                    StartCoroutine(ReEnableCollisionAfterDelay(projectileCollider, ignoreCollisionDuration));
                }
                
                Debug.Log($"[CannonLauncher] 포탄-대포 충돌 무시 설정 완료 (영구: {ignoreCollisionDuration == 0})");
            }
        }
        
        // 발사 방향 계산 (대포 포신이 바라보는 방향)
        Vector3 launchDirection = cannonBarrel.forward;
        
        // ✅ 해결책: velocity를 직접 설정
        float velocityMagnitude = launchForce / rb.mass; // F = ma → v = F/m
        rb.linearVelocity = launchDirection * velocityMagnitude;
        
        Debug.Log($"[CannonLauncher] 포탄 발사! 위치: {spawnPosition}, 방향: {launchDirection}, 속도: {rb.linearVelocity.magnitude:F2} m/s");
        
        // ✅ VFX 생성 (발사 시에만)
        if (launchVFXPrefab != null)
        {
            GameObject vfx = Instantiate(launchVFXPrefab, spawnPosition, cannonBarrel.rotation);
            
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
    
    /// <summary>
    /// 일정 시간 후 충돌을 다시 활성화합니다.
    /// </summary>
    private IEnumerator ReEnableCollisionAfterDelay(Collider projectileCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (projectileCollider != null && cannonColliders != null)
        {
            foreach (Collider cannonCollider in cannonColliders)
            {
                if (cannonCollider != null && cannonCollider.enabled)
                {
                    Physics.IgnoreCollision(projectileCollider, cannonCollider, false);
                }
            }
            Debug.Log("[CannonLauncher] 포탄-대포 충돌 다시 활성화");
        }
    }
    
    private void OnDisable()
    {
        // 컴포넌트 비활성화 시 자동 발사 중지
        StopAutoFire();
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (cannonBarrel == null)
            return;
        
        Vector3 startPos = cannonBarrel.position + cannonBarrel.TransformDirection(spawnPositionOffset);
        Vector3 direction = cannonBarrel.forward;
        
        // 발사 방향 직선 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cannonBarrel.position, direction * 3f);
        
        // 포탄 생성 위치 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPos, 0.2f);
        
        // 예상 궤적 그리기 (초록색)
        DrawTrajectory(startPos, direction);
    }
    
    /// <summary>
    /// 포물선 궤적을 Gizmo로 그립니다.
    /// </summary>
    private void DrawTrajectory(Vector3 startPos, Vector3 direction)
    {
        // 초기 속도 계산
        float velocityMagnitude = launchForce / projectileSettings.mass;
        Vector3 velocity = direction * velocityMagnitude;
        
        // 중력 가속도
        Vector3 gravity = Physics.gravity;
        
        // 궤적 색상
        Gizmos.color = Color.green;
        
        Vector3 previousPoint = startPos;
        
        for (int i = 1; i <= trajectoryPointCount; i++)
        {
            float t = i * trajectoryTimeStep;
            
            // 포물선 운동 공식: p(t) = p0 + v0*t + 0.5*g*t^2
            Vector3 currentPoint = startPos + velocity * t + 0.5f * gravity * t * t;
            
            // 선 그리기
            Gizmos.DrawLine(previousPoint, currentPoint);
            
            // 점 그리기 (5번째마다)
            if (i % 5 == 0)
            {
                Gizmos.DrawWireSphere(currentPoint, 0.15f);
            }
            
            previousPoint = currentPoint;
            
            // 땅에 닿으면 중단 (Y < 0)
            if (currentPoint.y < 0)
            {
                // 착지 지점 표시 (빨간색)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(currentPoint, 0.3f);
                break;
            }
        }
    }
#endif
}