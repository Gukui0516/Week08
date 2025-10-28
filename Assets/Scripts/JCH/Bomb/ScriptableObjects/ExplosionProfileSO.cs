using UnityEngine;

/// <summary>
/// 폭발 설정 데이터를 담는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "ExplosionProfile", menuName = "Bomb System/Explosion Profile")]
public class ExplosionProfileSO : ScriptableObject
{
    #region Serialized Fields
    [Header("Physics Settings")]
    [Tooltip("폭발의 기본 힘입니다.")]
    [SerializeField] private float _explosionForce = 500f;

    [Tooltip("폭발 충격파가 미치는 반경입니다.")]
    [SerializeField] private float _explosionRadius = 15f;

    [Tooltip("폭발 시 오브젝트를 위로 띄워 올리는 힘을 추가합니다.")]
    [SerializeField] private float _upwardModifier = 2.0f;

    [Tooltip("물리력 적용 시 사용할 LayerMask입니다.")]
    [SerializeField] private LayerMask _explosionLayerMask = -1;

    [Header("Visual Effects")]
    [Tooltip("폭발 시 사용할 VFX 타입입니다.")]
    [SerializeField] private ExplosionVfxType _vfxType = ExplosionVfxType.Default;

    [Tooltip("점멸 효과에 사용할 색상입니다.")]
    [SerializeField] private Color _tickingColor = Color.red;

    [Header("Camera Shake")]
    [Tooltip("카메라 쉐이크 강도입니다.")]
    [SerializeField] private float _cameraShakeIntensity = 1.0f;
    #endregion

    #region Properties
    public float ExplosionForce => _explosionForce;
    public float ExplosionRadius => _explosionRadius;
    public float UpwardModifier => _upwardModifier;
    public LayerMask ExplosionLayerMask => _explosionLayerMask;
    public ExplosionVfxType VfxType => _vfxType;
    public Color TickingColor => _tickingColor;
    public float CameraShakeIntensity => _cameraShakeIntensity;
    #endregion

    #region Public Methods - Validation
    /// <summary>
    /// 프로필 설정이 유효한지 검증합니다.
    /// </summary>
    public bool IsValid()
    {
        // 빈 구현
        return true;
    }
    #endregion
}