using UnityEngine;
using System.Collections;

/// <summary>
/// 벽돌깨기 게임의 파괴 가능한 벽돌 컴포넌트입니다.
/// HP 시스템과 Material 변경, 파괴 애니메이션을 관리합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BreakableBrick : MonoBehaviour
{
    [Header("HP Settings")]
    [Tooltip("벽돌의 최대 HP입니다.")]
    [SerializeField] private int maxHP = 3;
    
    [Header("Material Settings")]
    [Tooltip("HP별 Material 배열입니다. 인덱스 0 = HP 1, 인덱스 1 = HP 2, ...")]
    [SerializeField] private Material[] hpMaterials;
    
    [Header("Destruction Settings")]
    [Tooltip("벽돌이 파괴되는 데 걸리는 시간(초)입니다.")]
    [SerializeField] private float destructionDuration = 0.5f;
    
    [Tooltip("파괴 애니메이션의 Scale 변화 커브입니다.")]
    [SerializeField] private AnimationCurve destructionCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    
    [Header("Visual Effects")]
    [Tooltip("파괴 시 생성할 VFX 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject destructionVFXPrefab;
    
    [Tooltip("VFX 자동 소멸 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 2.0f;
    
    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    private int currentHP;
    private Renderer brickRenderer;
    private Collider brickCollider;
    private Vector3 originalScale;
    private bool isDestroying = false;
    
    /// <summary>
    /// 현재 HP를 반환합니다.
    /// </summary>
    public int CurrentHP => currentHP;
    
    /// <summary>
    /// 최대 HP를 반환합니다.
    /// </summary>
    public int MaxHP => maxHP;
    
    /// <summary>
    /// 파괴 중인지 여부를 반환합니다.
    /// </summary>
    public bool IsDestroying => isDestroying;
    
    private void Awake()
    {
        brickRenderer = GetComponent<Renderer>();
        brickCollider = GetComponent<Collider>();
        originalScale = transform.localScale;
        
        // 초기화
        ResetBrick();
    }
    
    /// <summary>
    /// 벽돌을 초기 상태로 리셋합니다.
    /// </summary>
    public void ResetBrick()
    {
        currentHP = maxHP;
        transform.localScale = originalScale;
        isDestroying = false;
        
        if (brickCollider != null)
        {
            brickCollider.enabled = true;
        }
        
        UpdateMaterial();
        
        if (showDebugLogs)
        {
            Debug.Log($"[BreakableBrick] {gameObject.name} 초기화: HP {currentHP}/{maxHP}");
        }
    }
    
    /// <summary>
    /// 공과 충돌 시 호출됩니다.
    /// </summary>
    /// <param name="hitPoint">충돌 지점</param>
    public void OnHit(Vector3 hitPoint)
    {
        // 이미 파괴 중이면 무시
        if (isDestroying)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[BreakableBrick] {gameObject.name}은(는) 이미 파괴 중입니다.");
            }
            return;
        }
        
        // HP 감소
        currentHP--;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BreakableBrick] {gameObject.name} 피격! HP: {currentHP}/{maxHP}");
        }
        
        if (currentHP <= 0)
        {
            // 파괴 시작
            StartDestruction(hitPoint);
        }
        else
        {
            // Material 업데이트
            UpdateMaterial();
        }
    }
    
    /// <summary>
    /// 현재 HP에 맞는 Material로 변경합니다.
    /// </summary>
    private void UpdateMaterial()
    {
        if (brickRenderer == null || hpMaterials == null || hpMaterials.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[BreakableBrick] {gameObject.name}: Renderer 또는 Material 배열이 없습니다.");
            }
            return;
        }
        
        // Material 배열 인덱스 계산 (HP 1 = 인덱스 0, HP 2 = 인덱스 1, ...)
        int materialIndex = currentHP - 1;
        
        // 유효성 검사
        if (materialIndex < 0 || materialIndex >= hpMaterials.Length)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[BreakableBrick] {gameObject.name}: Material 인덱스 {materialIndex}가 범위를 벗어났습니다. (배열 크기: {hpMaterials.Length})");
            }
            return;
        }
        
        Material targetMaterial = hpMaterials[materialIndex];
        
        if (targetMaterial == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[BreakableBrick] {gameObject.name}: Material 인덱스 {materialIndex}가 null입니다.");
            }
            return;
        }
        
        // Material 적용
        brickRenderer.material = targetMaterial;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BreakableBrick] {gameObject.name} Material 변경: HP {currentHP} → {targetMaterial.name}");
        }
    }
    
    /// <summary>
    /// 벽돌 파괴를 시작합니다.
    /// </summary>
    /// <param name="hitPoint">충돌 지점 (VFX 생성 위치)</param>
    private void StartDestruction(Vector3 hitPoint)
    {
        if (isDestroying) return;
        
        isDestroying = true;
        
        // Collider 비활성화 (추가 충돌 방지)
        if (brickCollider != null)
        {
            brickCollider.enabled = false;
        }
        
        // VFX 생성
        if (destructionVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(destructionVFXPrefab, hitPoint, Quaternion.identity);
            
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[BreakableBrick] {gameObject.name} VFX 생성: {hitPoint}");
            }
        }
        
        // 파괴 애니메이션 시작
        StartCoroutine(DestructionCoroutine());
    }
    
    /// <summary>
    /// 파괴 애니메이션 코루틴입니다.
    /// </summary>
    private IEnumerator DestructionCoroutine()
    {
        float elapsedTime = 0f;
        Vector3 targetScale = Vector3.zero;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BreakableBrick] {gameObject.name} 파괴 애니메이션 시작 ({destructionDuration}초)");
        }
        
        while (elapsedTime < destructionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / destructionDuration;
            
            // AnimationCurve를 사용한 보간
            float curveValue = destructionCurve.Evaluate(progress);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, 1f - curveValue);
            
            yield return null;
        }
        
        // 최종 Scale 설정
        transform.localScale = targetScale;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BreakableBrick] {gameObject.name} 파괴 완료 → SetActive(false)");
        }
        
        // 비활성화
        gameObject.SetActive(false);
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // maxHP 검증
        if (maxHP < 1)
        {
            maxHP = 1;
            Debug.LogWarning($"[BreakableBrick] {gameObject.name}: maxHP는 1 이상이어야 합니다.");
        }
        
        // Material 배열 크기 검증
        if (hpMaterials != null && hpMaterials.Length != maxHP)
        {
            Debug.LogWarning($"[BreakableBrick] {gameObject.name}: Material 배열 크기({hpMaterials.Length})가 maxHP({maxHP})와 일치하지 않습니다.");
        }
        
        // Destruction Duration 검증
        if (destructionDuration <= 0f)
        {
            destructionDuration = 0.5f;
            Debug.LogWarning($"[BreakableBrick] {gameObject.name}: destructionDuration은 0보다 커야 합니다. 기본값(0.5초)로 설정합니다.");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 벽돌의 현재 상태 시각화
        if (Application.isPlaying)
        {
            Gizmos.color = isDestroying ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            
            // HP 텍스트
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                $"HP: {currentHP}/{maxHP}\n파괴 중: {isDestroying}");
        }
    }
#endif
}