using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

/// <summary>
/// IExplodable 인터페이스를 구현하는 기본 폭탄 엔티티입니다.
/// MaterialPropertyBlock 기반 점멸 효과와 폭발 위임을 처리합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class NewBombEntityDefault : MonoBehaviour, IExplodable
{
    #region Serialized Fields
    [TabGroup("Explosion")]
    [Tooltip("이 폭탄의 폭발 설정 프로필입니다.")]
    [SerializeField] private ExplosionProfileSO _explosionProfile;

    [TabGroup("Explosion")]
    [SerializeField] private bool _destroyAfterExplosion = true;

    [TabGroup("Ticking")]
    [Tooltip("점멸 효과 사용 여부입니다.")]
    [SerializeField] private bool _useTickingEffect = true;

    [TabGroup("Ticking")]
    [Tooltip("점멸 주기(초)입니다.")]
    [SerializeField] private float _tickingInterval = 1.0f;

    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Renderer _cachedRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    private Color _originalColor;

    // Ticking 상태 관리
    private bool _isTickingActive;
    private float _tickingElapsedTime;
    private float _tickingTargetDuration;
    private float _tickingNextToggleTime;
    private bool _isCurrentlyBright;

    private static readonly int ColorPropertyID = Shader.PropertyToID("_BaseColor");
    #endregion

    #region Properties
    /// <summary>디버그 로깅 활성화 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LateInitialize();
    }

    private void Update()
    {
        if (_isTickingActive && _useTickingEffect)
        {
            UpdateTicking();
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _cachedRenderer = GetComponent<Renderer>();

        if (_cachedRenderer == null)
        {
            LogError("Renderer 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        _materialPropertyBlock = new MaterialPropertyBlock();
        _cachedRenderer.GetPropertyBlock(_materialPropertyBlock);

        // 원본 색상 저장
        if (_materialPropertyBlock.isEmpty)
        {
            _originalColor = _cachedRenderer.sharedMaterial.GetColor(ColorPropertyID);
        }
        else
        {
            _originalColor = _materialPropertyBlock.GetColor(ColorPropertyID);
        }

        // Ticking 상태 초기화
        _isTickingActive = false;
        _tickingElapsedTime = 0f;
        _tickingTargetDuration = 0f;
        _tickingNextToggleTime = 0f;
        _isCurrentlyBright = false;

        Log("초기화 완료: Renderer 및 MaterialPropertyBlock 준비됨");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        if (_explosionProfile == null)
        {
            LogWarning("ExplosionProfile이 할당되지 않았습니다!");
        }

        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        StopTicking();

        if (_cachedRenderer != null && _materialPropertyBlock != null)
        {
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        Log("Cleanup: 원본 색상 복원 완료");
    }
    #endregion

    #region IExplodable Implementation
    /// <summary>
    /// 이 객체의 폭발 설정 프로필을 반환합니다.
    /// </summary>
    public ExplosionProfileSO GetExplosionProfile()
    {
        return _explosionProfile;
    }

    /// <summary>
    /// 점멸 효과를 위한 Renderer를 반환합니다.
    /// </summary>
    public Renderer GetRenderer()
    {
        return _cachedRenderer;
    }

    /// <summary>
    /// 폭발 후 호출되는 콜백입니다.
    /// </summary>
    public void AfterExploded()
    {
        if (_destroyAfterExplosion)
        {
            Log($"{gameObject.name} 파괴");
            gameObject.SetActive(false);
            GameObject.Destroy(gameObject);
        }
        else
        {
            Log($"{gameObject.name} 재사용 가능 상태로 복원");
            StopTicking();
        }
    }

    /// <summary>
    /// 점멸 효과를 시작합니다.
    /// </summary>
    /// <param name="duration">점멸 지속 시간(초)</param>
    public void StartTicking(float duration)
    {
        if (!_useTickingEffect)
        {
            Log("점멸 효과가 비활성화되어 있습니다.");
            return;
        }

        _isTickingActive = true;
        _tickingElapsedTime = 0f;
        _tickingTargetDuration = duration;
        _tickingNextToggleTime = _tickingInterval * 0.5f;
        _isCurrentlyBright = false;

        // 초기 색상을 원본으로 설정
        if (_cachedRenderer != null && _materialPropertyBlock != null)
        {
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        Log($"점멸 시작: {duration}초");
    }

    /// <summary>
    /// 점멸 효과를 중지합니다.
    /// </summary>
    public void StopTicking()
    {
        _isTickingActive = false;
        _tickingElapsedTime = 0f;

        if (_cachedRenderer != null && _materialPropertyBlock != null)
        {
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        Log("점멸 중지");
    }
    #endregion

    #region Private Methods - Update Logic
    /// <summary>
    /// Update에서 호출되는 점멸 로직입니다.
    /// </summary>
    private void UpdateTicking()
    {
        if (_cachedRenderer == null || _materialPropertyBlock == null || _explosionProfile == null)
        {
            LogError("점멸 효과에 필요한 컴포넌트가 누락되었습니다!");
            _isTickingActive = false;
            return;
        }

        _tickingElapsedTime += Time.deltaTime;

        // 목표 시간 도달 시 종료
        if (_tickingElapsedTime >= _tickingTargetDuration)
        {
            _isTickingActive = false;
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
            Log("점멸 완료");
            return;
        }

        // 색상 전환 타이밍 체크
        if (_tickingElapsedTime >= _tickingNextToggleTime)
        {
            _isCurrentlyBright = !_isCurrentlyBright;
            _tickingNextToggleTime += _tickingInterval * 0.5f;

            Color targetColor = _isCurrentlyBright ? _explosionProfile.TickingColor : _originalColor;
            _materialPropertyBlock.SetColor(ColorPropertyID, targetColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }
    #endregion

#if UNITY_EDITOR
    #region Editor Gizmos
    private void OnDrawGizmosSelected()
    {
        if (_explosionProfile == null) return;

        // 폭발 범위 표시
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, _explosionProfile.ExplosionRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _explosionProfile.ExplosionRadius);
    }
    #endregion
#endif
}