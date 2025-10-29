using UnityEngine;

/// <summary>
/// 벽돌깨기 게임의 공 컴포넌트입니다.
/// 일정한 속도를 유지하며 반사 메커니즘으로 동작합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BrickBreakerBall : MonoBehaviour
{
    [Header("Ball Settings")]
    [Tooltip("공의 일정한 속도입니다. 항상 이 속도를 유지합니다.")]
    [SerializeField] private float ballSpeed = 10f;
    
    [Header("Collision Settings")]
    [Tooltip("동일한 오브젝트와의 충돌 쿨다운 시간(초)입니다. 코너에 끼는 문제를 방지합니다.")]
    [SerializeField] private float collisionCooldown = 0.1f;
    
    [Tooltip("코너 감지를 위한 최소 각도(도)입니다. 이 각도보다 작으면 코너로 판단합니다.")]
    [SerializeField] private float cornerDetectionAngle = 30f;
    
    [Header("Visual Effects")]
    [Tooltip("충돌 시 생성할 이펙트 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject collisionVFXPrefab;
    [Tooltip("VFX 자동 소멸 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 1.0f;
    
    [Header("Collision Layers")]
    [Tooltip("충돌 감지할 레이어 마스크입니다.")]
    [SerializeField] private LayerMask collisionMask = -1;
    
    [Header("Debug Settings")]
    [Tooltip("충돌 디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    private Rigidbody rb;
    private bool isActive = false;
    private Vector3 lastVelocity; // 충돌 직전 속도
    private GameObject lastCollisionObject; // 마지막 충돌 오브젝트
    private float lastCollisionTime; // 마지막 충돌 시간
    
    /// <summary>
    /// 공의 현재 속도를 반환합니다.
    /// </summary>
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    
    /// <summary>
    /// 공의 활성화 상태를 반환합니다.
    /// </summary>
    public bool IsActive => isActive;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        ConfigureCollider();
    }
    
    /// <summary>
    /// Rigidbody를 벽돌깨기 공에 맞게 설정합니다.
    /// </summary>
    private void ConfigureRigidbody()
    {
        if (rb == null) return;
        
        rb.mass = 1f;
        rb.linearDamping = 0f; // 공기 저항 없음
        rb.angularDamping = 0f; // 회전 저항 없음
        rb.useGravity = false; // 중력 사용 안 함
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation; // Z축 고정 (2D처럼)
    }
    
    /// <summary>
    /// SphereCollider를 설정합니다.
    /// Physics Material에서 Bounciness를 0으로 설정하여 수동 반사만 사용합니다.
    /// </summary>
    private void ConfigureCollider()
    {
        SphereCollider sphereCol = GetComponent<SphereCollider>();
        if (sphereCol != null)
        {
            // Physics Material 생성 (반사 없음, 마찰 없음)
            PhysicsMaterial nonBouncyMaterial = new PhysicsMaterial("BallMaterial_NoBounciness");
            nonBouncyMaterial.bounciness = 0f; // ✅ 자동 반사 제거
            nonBouncyMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            nonBouncyMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            nonBouncyMaterial.dynamicFriction = 0f;
            nonBouncyMaterial.staticFriction = 0f;
            
            sphereCol.material = nonBouncyMaterial;
        }
    }
    
    private void FixedUpdate()
    {
        if (!isActive) return;
        
        // 충돌 직전 속도 저장
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            lastVelocity = rb.linearVelocity;
        }
        
        // 속도를 일정하게 유지
        MaintainConstantSpeed();
    }
    
    /// <summary>
    /// 공의 속도를 항상 일정하게 유지합니다.
    /// </summary>
    private void MaintainConstantSpeed()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;
        
        if (currentSpeed > 0.1f && Mathf.Abs(currentSpeed - ballSpeed) > 0.1f)
        {
            // 현재 방향을 유지하면서 속도만 조정
            rb.linearVelocity = currentVelocity.normalized * ballSpeed;
        }
    }
    
    /// <summary>
    /// 공을 특정 방향으로 발사합니다.
    /// </summary>
    /// <param name="direction">발사 방향 (정규화됨)</param>
    public void Launch(Vector3 direction)
    {
        if (rb == null) return;
        
        isActive = true;
        
        // Z축 방향 제거 (2D 동작)
        direction.z = 0f;
        direction.Normalize();
        
        rb.linearVelocity = direction * ballSpeed;
        lastVelocity = rb.linearVelocity;
        lastCollisionObject = null;
        lastCollisionTime = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BrickBreakerBall] 공 발사: {gameObject.name}, 방향: {direction}, 속도: {ballSpeed}");
        }
    }
    
    /// <summary>
    /// 공을 정지시킵니다.
    /// </summary>
    public void Stop()
    {
        isActive = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        lastVelocity = Vector3.zero;
        lastCollisionObject = null;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BrickBreakerBall] 공 정지: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 공의 속도를 변경합니다. (아이템 등에 사용)
    /// </summary>
    /// <param name="newSpeed">새로운 속도</param>
    public void SetSpeed(float newSpeed)
    {
        ballSpeed = Mathf.Max(0f, newSpeed);
        
        if (isActive && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * ballSpeed;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[BrickBreakerBall] 속도 변경: {ballSpeed}");
        }
    }
    
    /// <summary>
    /// 공을 오브젝트 풀로 반환합니다.
    /// </summary>
    public void ReturnToPool()
    {
        Stop();
        
        if (BrickBreakerBallPool.Instance != null)
        {
            BrickBreakerBallPool.Instance.ReturnBall(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 충돌 레이어 확인
        if (((1 << collision.gameObject.layer) & collisionMask) == 0)
        {
            return;
        }
        
        // 동일한 오브젝트와의 연속 충돌 방지 (쿨다운)
        float timeSinceLastCollision = Time.time - lastCollisionTime;
        if (collision.gameObject == lastCollisionObject && timeSinceLastCollision < collisionCooldown)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[BrickBreakerBall] 충돌 쿨다운: {collision.gameObject.name} (경과 시간: {timeSinceLastCollision:F3}초)");
            }
            return;
        }
        
        // ✅ 추가: Breakable 태그 체크
        if (collision.gameObject.CompareTag("Breakable"))
        {
            BreakableBrick brick = collision.gameObject.GetComponent<BreakableBrick>();
            if (brick != null && collision.contacts.Length > 0)
            {
                Vector3 hitPoint = collision.contacts[0].point;
                brick.OnHit(hitPoint);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[BrickBreakerBall] Breakable 충돌: {collision.gameObject.name} (HP: {brick.CurrentHP}/{brick.MaxHP})");
                }
            }
            else if (brick == null)
            {
                Debug.LogError($"[BrickBreakerBall] {collision.gameObject.name}에 BreakableBrick 컴포넌트가 없습니다!");
            }
        }
        
        // VFX 생성
        if (collisionVFXPrefab != null && collision.contacts.Length > 0)
        {
            Vector3 contactPoint = collision.contacts[0].point;
            GameObject vfxInstance = Instantiate(collisionVFXPrefab, contactPoint, Quaternion.identity);
            
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
        }
        
        // 반사 처리
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            
            // 충돌 직전의 속도 사용
            Vector3 incomingVelocity = lastVelocity.magnitude > 0.5f ? lastVelocity : rb.linearVelocity;
            
            // 입사각 계산 (코너 감지용)
            float incidentAngle = Vector3.Angle(-incomingVelocity, normal);
            
            // 코너 감지: 입사각이 너무 작으면 (거의 수직 충돌) 약간의 랜덤성 추가
            if (incidentAngle < cornerDetectionAngle)
            {
                // 약간의 각도 추가 (±5~15도)
                float randomAngle = Random.Range(5f, 15f) * (Random.value > 0.5f ? 1f : -1f);
                normal = Quaternion.Euler(0f, 0f, randomAngle) * normal;
                normal.Normalize();
                
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[BrickBreakerBall] 코너 감지! 각도 조정: {randomAngle:F1}도");
                }
            }
            
            // 반사 벡터 계산
            Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);
            
            // Z축 고정
            reflectedVelocity.z = 0f;
            
            // 일정한 속도 유지하며 반사
            Vector3 newVelocity = reflectedVelocity.normalized * ballSpeed;
            rb.linearVelocity = newVelocity;
            lastVelocity = newVelocity;
            
            // 충돌 기록
            lastCollisionObject = collision.gameObject;
            lastCollisionTime = Time.time;
            
            if (showDebugLogs)
            {
                Debug.Log($"[BrickBreakerBall] 충돌: {collision.gameObject.name}\n" +
                          $"법선: {normal}\n" +
                          $"입사 속도: {incomingVelocity}\n" +
                          $"반사 속도: {newVelocity}\n" +
                          $"입사각: {incidentAngle:F1}도");
            }
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 속도 검증
        if (ballSpeed < 0f)
        {
            ballSpeed = 0f;
            Debug.LogWarning("[BrickBreakerBall] 공의 속도는 0 이상이어야 합니다.");
        }
        
        if (collisionCooldown < 0f)
        {
            collisionCooldown = 0f;
        }
        
        if (cornerDetectionAngle < 0f || cornerDetectionAngle > 90f)
        {
            cornerDetectionAngle = Mathf.Clamp(cornerDetectionAngle, 0f, 90f);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!isActive || rb == null) return;
        
        // 현재 속도 벡터 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        
        // 마지막 속도 벡터 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, lastVelocity.normalized * 1.5f);
        
        // 속도 텍스트
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
            $"속도: {CurrentSpeed:F1}\n마지막: {lastVelocity.magnitude:F1}");
    }
#endif
}