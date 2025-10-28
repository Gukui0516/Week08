using UnityEngine;
using System.Collections;

/// <summary>
/// 수박 게임의 과일 합치기 로직을 담당하는 컨트롤러입니다.
/// 같은 종류의 과일이 충돌하면 합치기 애니메이션을 재생하고 다음 단계 과일을 생성합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(FruitMergeData))]
public class FruitMergeController : MonoBehaviour
{
    [Header("Merge Animation Settings")]
    [Tooltip("합치기 애니메이션 지속 시간(초)입니다.")]
    [SerializeField] private float mergeDuration = 0.3f;
    
    [Tooltip("합치기 애니메이션 커브입니다.")]
    [SerializeField] private AnimationCurve mergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Collision Settings")]
    [Tooltip("같은 과일과의 충돌 쿨다운 시간(초)입니다. 중복 충돌 방지용입니다.")]
    [SerializeField] private float collisionCooldown = 0.1f;
    
    [Header("Merge Spawn Settings")]
    [Tooltip("생성 오프셋 배율입니다. (과일 반지름 × 이 값)")]
    [SerializeField] private float spawnOffsetMultiplier = 1.2f;
    
    [Header("Visual Effects")]
    [Tooltip("충돌 시 생성할 VFX 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject collisionVFXPrefab;
    
    [Tooltip("VFX 자동 소멸 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 2.0f;
    
    [Header("Debug Settings")]
    [Tooltip("충돌 및 합치기 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 캐시된 컴포넌트
    private Rigidbody rb;
    private Collider fruitCollider;
    private FruitMergeData fruitData;
    
    // 충돌 쿨다운 관리
    private GameObject lastCollisionObject;
    private float lastCollisionTime;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fruitCollider = GetComponent<Collider>();
        fruitData = GetComponent<FruitMergeData>();
        
        if (fruitData == null)
        {
            Debug.LogError($"[FruitMergeController] {gameObject.name}에 FruitMergeData 컴포넌트가 없습니다!");
            enabled = false;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 병합 진행 중이거나 쿨다운 중이면 무시
        if (fruitData.IsMerging || IsInCooldown(collision.gameObject))
            return;
        
        // 상대방 검증
        if (!TryGetOtherController(collision, out FruitMergeController otherController))
        {
            HandleNonFruitCollision(collision);
            return;
        }
        
        // 병합 조건 검증
        if (!CanMergeWith(otherController))
            return;
        
        // 인스턴스 ID 비교 (작은 쪽만 처리)
        if (GetInstanceID() > otherController.GetInstanceID())
            return;
        
        // 병합 시작
        LogDebug($"⚡ 병합 시작: {gameObject.name} + {collision.gameObject.name} " +
                $"({fruitData.CurrentFruitType} → {fruitData.NextFruitType})");
        
        // 즉시 상태 잠금 (Race Condition 방지)
        LockMergeState(otherController);
        
        Vector3 contactPoint = collision.contacts.Length > 0 
            ? collision.contacts[0].point 
            : (transform.position + collision.transform.position) / 2f;
        
        StartCoroutine(MergeCoroutine(otherController, contactPoint));
    }
    
    /// <summary>
    /// 쿨다운 체크
    /// </summary>
    private bool IsInCooldown(GameObject target)
    {
        if (target != lastCollisionObject)
        {
            lastCollisionObject = target;
            lastCollisionTime = Time.time;
            return false;
        }
        
        return Time.time - lastCollisionTime < collisionCooldown;
    }
    
    /// <summary>
    /// 상대방 컨트롤러 가져오기
    /// </summary>
    private bool TryGetOtherController(Collision collision, out FruitMergeController controller)
    {
        controller = collision.gameObject.GetComponent<FruitMergeController>();
        return controller != null;
    }
    
    /// <summary>
    /// 과일이 아닌 오브젝트와 충돌 처리
    /// </summary>
    private void HandleNonFruitCollision(Collision collision)
    {
        if (collisionVFXPrefab != null && collision.contacts.Length > 0)
        {
            SpawnVFX(collision.contacts[0].point);
        }
    }
    
    /// <summary>
    /// 병합 가능 여부 확인
    /// </summary>
    private bool CanMergeWith(FruitMergeController other)
    {
        FruitMergeData otherData = other.fruitData;
        
        // 같은 종류인지
        if (fruitData.CurrentFruitType != otherData.CurrentFruitType)
        {
            LogDebug($"종류 다름: {fruitData.CurrentFruitType} vs {otherData.CurrentFruitType}");
            return false;
        }
        
        // 🔥 면역 시간 체크 (공중 병합 방지)
        if (!fruitData.CanMergeNow || !otherData.CanMergeNow)
        {
            LogDebug($"병합 면역 시간 중: {gameObject.name} 또는 {other.gameObject.name}");
            return false;
        }
        
        // 병합 가능한지
        if (!fruitData.CanMerge || !otherData.CanMerge)
        {
            LogDebug($"{gameObject.name} 또는 {other.gameObject.name}은(는) 병합 불가");
            return false;
        }
        
        // 상대방이 이미 병합 중인지
        if (otherData.IsMerging)
        {
            LogDebug($"{other.gameObject.name}은(는) 이미 병합 중");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 병합 상태 즉시 잠금
    /// </summary>
    private void LockMergeState(FruitMergeController other)
    {
        // 플래그 설정
        fruitData.SetMerging(true);
        other.fruitData.SetMerging(true);
        
        // 콜라이더 비활성화
        fruitCollider.enabled = false;
        if (other.fruitCollider != null)
            other.fruitCollider.enabled = false;
        
        // 물리 정지
        rb.isKinematic = true;
        if (other.rb != null)
            other.rb.isKinematic = true;
        
        LogDebug("✅ 병합 상태 잠금 완료");
    }
    
    /// <summary>
    /// 병합 코루틴
    /// </summary>
    private IEnumerator MergeCoroutine(FruitMergeController other, Vector3 contactPoint)
    {
        // Null 체크 및 상태 복원
        if (!ValidateOther(other))
        {
            RestoreSelfState();
            yield break;
        }
        
        // 원래 타입 저장
        FruitMergeData.FruitType originalType1 = fruitData.CurrentFruitType;
        FruitMergeData.FruitType originalType2 = other.fruitData.CurrentFruitType;
        
        LogDebug($"🔒 원래 타입: {gameObject.name}({originalType1}), {other.gameObject.name}({originalType2})");
        
        // 병합 전 SpawnCount 로그
        if (WatermelonGameManager.Instance != null && WatermelonGameManager.Instance.SpawnManager != null)
        {
            LogDebug($"📊 병합 전 SpawnCount: {WatermelonGameManager.Instance.SpawnManager.SpawnCount}");
        }
        
        // 병합 애니메이션
        yield return StartCoroutine(PlayMergeAnimation(other));
        
        // 이펙트 생성
        Vector3 midPoint = (transform.position + other.transform.position) / 2f;
        SpawnMergeFX(midPoint);
        
        // 다음 과일 생성
        SpawnNextFruit(midPoint);
        
        // 병합 중간 SpawnCount 로그 (새 과일 생성 후)
        if (WatermelonGameManager.Instance != null && WatermelonGameManager.Instance.SpawnManager != null)
        {
            LogDebug($"📊 새 과일 생성 후 SpawnCount: {WatermelonGameManager.Instance.SpawnManager.SpawnCount}");
        }
        
        // 풀로 반환
        ReturnToPool(other, originalType1, originalType2);
        
        // 병합 후 SpawnCount 로그
        if (WatermelonGameManager.Instance != null && WatermelonGameManager.Instance.SpawnManager != null)
        {
            LogDebug($"📊 병합 후 SpawnCount: {WatermelonGameManager.Instance.SpawnManager.SpawnCount}");
        }
    }
    
    /// <summary>
    /// 상대방 유효성 검증
    /// </summary>
    private bool ValidateOther(FruitMergeController other)
    {
        if (other != null && other.gameObject != null)
            return true;
        
        Debug.LogWarning("[FruitMergeController] 상대 과일이 이미 파괴됨. 병합 중단.");
        return false;
    }
    
    /// <summary>
    /// 자신의 상태 복원
    /// </summary>
    private void RestoreSelfState()
    {
        if (fruitData != null) fruitData.SetMerging(false);
        if (fruitCollider != null) fruitCollider.enabled = true;
        if (rb != null) rb.isKinematic = false;
    }
    
    /// <summary>
    /// 병합 애니메이션 재생
    /// </summary>
    private IEnumerator PlayMergeAnimation(FruitMergeController other)
    {
        Vector3 startPos1 = transform.position;
        Vector3 startPos2 = other.transform.position;
        Vector3 midPoint = (startPos1 + startPos2) / 2f;
        
        LogDebug($"애니메이션 시작: {mergeDuration}초 동안 {midPoint}로 이동");
        
        float elapsed = 0f;
        while (elapsed < mergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = mergeCurve.Evaluate(elapsed / mergeDuration);
            
            if (this != null && gameObject.activeSelf)
                transform.position = Vector3.Lerp(startPos1, midPoint, t);
            
            if (other != null && other.gameObject != null && other.gameObject.activeSelf)
                other.transform.position = Vector3.Lerp(startPos2, midPoint, t);
            
            yield return null;
        }
        
        // 최종 위치 보정
        if (this != null && gameObject.activeSelf)
            transform.position = midPoint;
        
        if (other != null && other.gameObject != null && other.gameObject.activeSelf)
            other.transform.position = midPoint;
    }
    
    /// <summary>
    /// 병합 이펙트 생성
    /// </summary>
    private void SpawnMergeFX(Vector3 position)
    {
        GameObject mergeFX = fruitData.MergeFXPrefab;
        if (mergeFX == null) return;
        
        GameObject vfxInstance = Instantiate(mergeFX, position, Quaternion.identity);
        
        // ParticleSystem 재생
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        else
        {
            foreach (var system in vfxInstance.GetComponentsInChildren<ParticleSystem>())
                system.Play();
        }
        
        if (vfxLifetime > 0)
            Destroy(vfxInstance, vfxLifetime);
        
        LogDebug($"이펙트 생성: {position}");
    }
    
    /// <summary>
    /// 🔥 과일 타입별 생성 오프셋을 계산합니다.
    /// </summary>
    private float GetDynamicSpawnOffset(FruitMergeData.FruitType fruitType)
    {
        float radius = fruitType switch
        {
            FruitMergeData.FruitType.Grape => 0.35f,
            FruitMergeData.FruitType.Apple => 0.45f,
            FruitMergeData.FruitType.Orange => 0.55f,
            FruitMergeData.FruitType.Lemon => 0.65f,
            FruitMergeData.FruitType.Melon => 0.8f,
            FruitMergeData.FruitType.Durian => 1.0f,
            FruitMergeData.FruitType.Watermelon => 1.25f,
            FruitMergeData.FruitType.Bomb => 1.35f,
            _ => 0.5f
        };
        
        return radius * spawnOffsetMultiplier;
    }
    
    /// <summary>
    /// 다음 단계 과일 생성
    /// </summary>
    private void SpawnNextFruit(Vector3 position)
    {
        if (fruitData.CurrentFruitType == FruitMergeData.FruitType.Bomb)
        {
            LogDebug("폭탄은 최종 단계");
            return;
        }
        
        if (WatermelonObjectPool.Instance == null)
        {
            Debug.LogError("[FruitMergeController] WatermelonObjectPool이 없습니다!");
            return;
        }
        
        // 🔥 동적 오프셋 계산
        float dynamicOffset = GetDynamicSpawnOffset(fruitData.NextFruitType);
        Vector3 spawnPosition = position + Vector3.up * dynamicOffset;
        
        GameObject newFruit = WatermelonObjectPool.Instance.GetFruit(
            fruitData.NextFruitType, 
            spawnPosition
        );
        
        // 병합으로 생성된 과일에는 면역 적용하지 않음 (요청에 따라 제거)
        
        LogDebug($"다음 과일 생성: {fruitData.NextFruitType} at {spawnPosition} (오프셋: +{dynamicOffset:F2}Y)");
    }
    
    /// <summary>
    /// 풀로 반환
    /// </summary>
    private void ReturnToPool(FruitMergeController other, FruitMergeData.FruitType type1, FruitMergeData.FruitType type2)
    {
        if (WatermelonObjectPool.Instance == null)
        {
            // 풀이 없으면 파괴
            Destroy(gameObject);
            if (other != null && other.gameObject != null)
                Destroy(other.gameObject);
            
            LogDebug("풀 없음. 파괴 처리");
            return;
        }
        
        // 타입 변경 감지
        if (fruitData.CurrentFruitType != type1)
            LogDebug($"⚠️ 타입 변경: {gameObject.name} {type1} → {fruitData.CurrentFruitType}");
        
        if (other != null && other.fruitData != null && other.fruitData.CurrentFruitType != type2)
            LogDebug($"⚠️ 타입 변경: {other.gameObject.name} {type2} → {other.fruitData.CurrentFruitType}");
        
        // 원래 타입으로 반환
        WatermelonObjectPool.Instance.ReturnFruitByOriginalType(gameObject, type1);
        
        if (other != null && other.gameObject != null)
            WatermelonObjectPool.Instance.ReturnFruitByOriginalType(other.gameObject, type2);
        
        LogDebug($"✅ 풀 반환 완료 (타입: {type1}, {type2})");
    }
    
    /// <summary>
    /// VFX 생성 (충돌용)
    /// </summary>
    private void SpawnVFX(Vector3 position)
    {
        GameObject vfxInstance = Instantiate(collisionVFXPrefab, position, Quaternion.identity);
        
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();
        
        if (vfxLifetime > 0)
            Destroy(vfxInstance, vfxLifetime);
    }
    
    /// <summary>
    /// 디버그 로그 헬퍼
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[FruitMergeController] {message}");
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mergeDuration <= 0f)
        {
            mergeDuration = 0.3f;
            Debug.LogWarning($"[FruitMergeController] mergeDuration은 0보다 커야 합니다. 기본값 설정.");
        }
        
        if (collisionCooldown < 0f)
        {
            collisionCooldown = 0.1f;
            Debug.LogWarning($"[FruitMergeController] collisionCooldown은 음수일 수 없습니다. 기본값 설정.");
        }
        
        if (spawnOffsetMultiplier < 1f)
        {
            spawnOffsetMultiplier = 1.2f;
            Debug.LogWarning($"[FruitMergeController] spawnOffsetMultiplier는 1 이상이어야 합니다. 기본값 설정.");
        }
    }
#endif
}