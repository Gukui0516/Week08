using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 모든 폭발 로직을 실행하는 싱글톤 시스템입니다.
/// IExplodable 기반으로 VFX, 물리력, 카메라 셰이크, 히트스탑을 처리합니다.
/// </summary>
public class NewBombExplodeSystem : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Camera Shake")]
    [Tooltip("카메라 셰이크를 위한 Cinemachine Impulse Source입니다.")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    [TabGroup("VFX Pool Settings")]
    [Tooltip("VFX 풀링 시스템입니다.")]
    [SerializeField] private TypedObjectPool<ExplosionVfxType> _vfxPool;

    [TabGroup("VFX Settings")]
    [Tooltip("VFX가 자동으로 풀에 반환되는 시간(초)입니다.")]
    [SerializeField] private float _vfxLifetime = 3.0f;

    [TabGroup("Hitstop Settings")]
    [Tooltip("히트스탑이 시작되기 전까지 대기 시간")]
    [SerializeField] private float _hitstopDelaySeconds = 0.5f;

    [Tooltip("히트스탑(시간 정지) 지속 시간(초)입니다.")]
    [SerializeField] private float _hitstopDuration = 0.2f;

    [Tooltip("히트스탑 종료 후, 원래 시간 속도로 돌아오는 데 걸리는 시간(초)입니다.")]
    [SerializeField] private float _timeScaleRecoveryDuration = 1.0f;

    [Tooltip("시간 속도 복구 애니메이션의 형태를 정의하는 커브입니다.")]
    [SerializeField] private AnimationCurve _timeScaleRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [TabGroup("Debug Settings")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private static NewBombExplodeSystem _instance;
    private static bool _isQuitting = false;

    private Dictionary<GameObject, float> _activeVfxInstances;
    #endregion

    #region Properties
    public static NewBombExplodeSystem Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NewBombExplodeSystem>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("NewBombExplodeSystem");
                    _instance = obj.AddComponent<NewBombExplodeSystem>();
                }
            }
            return _instance;
        }
    }

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
        UpdateVfxLifetime();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        if (_instance == null)
        {
            _instance = this;
            _isQuitting = false;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _activeVfxInstances = new Dictionary<GameObject, float>();
        if (_vfxPool == null)
        {
            _vfxPool = new TypedObjectPool<ExplosionVfxType>();
        }

        if (_vfxPool != null)
        {
            _vfxPool.Initialize();
            Log("VFX 풀 초기화 완료");
        }
        else
        {
            LogWarning("VFX Pool이 할당되지 않았습니다!");
        }

        Log("초기화 완료: 폭발 시스템 준비됨");
    }
    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        if (_impulseSource == null)
        {
            LogWarning("Cinemachine Impulse Source가 할당되지 않았습니다.");
        }

        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_instance == this)
        {
            _isQuitting = true;
        }

        StopAllCoroutines();

        if (_vfxPool != null && _vfxPool.IsInitialized)
        {
            _vfxPool.ReturnAllActiveObjects();
            Log("VFX 풀 정리 완료");
        }

        _activeVfxInstances?.Clear();

        Log("Cleanup: 폭발 시스템 정리 완료");
    }
    #endregion

    #region Public Methods - Explosion Control
    public void Execute(IExplodable target)
    {
        if (target == null)
        {
            LogError("폭발 대상이 null입니다.");
            return;
        }

        ExplosionProfileSO profile = target.GetExplosionProfile();
        if (profile == null)
        {
            LogError("ExplosionProfile이 null입니다.");
            return;
        }

        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null || !targetMono.gameObject.activeInHierarchy)
        {
            LogWarning("대상이 비활성화 상태입니다.");
            return;
        }

        Vector3 explosionWorldPosition = targetMono.transform.position;

        Log($"폭발 실행: {targetMono.name} at {explosionWorldPosition}");

        CreateExplosionVFX(explosionWorldPosition, profile.VfxType);
        TriggerCameraShake(profile.CameraShakeIntensity);
        ApplyExplosionForceInRadius(profile, explosionWorldPosition);

        target.StopTicking();
        target.AfterExploded();

        Log($"폭발 완료: {targetMono.name}");
    }

    /// <summary>
    /// 여러 객체를 순차적으로 폭발시키는 시퀀스를 실행합니다.
    /// </summary>
    /// <param name="targets">폭발시킬 IExplodable 리스트</param>
    /// <param name="delayInterval">각 폭발 사이의 지연 시간(초)</param>
    /// <param name="tickingDuration">점멸 지속 시간(초)</param>
    public void ExecuteSequence(List<IExplodable> targets, float delayInterval, float tickingDuration)
    {
        if (targets == null || targets.Count == 0)
        {
            LogWarning("폭발시킬 대상이 없습니다.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SequentialExplosionCoroutine(targets, delayInterval, tickingDuration));
        Log($"순차 폭발 시퀀스 시작: {targets.Count}개 대상");
    }
    #endregion

    #region Private Methods - VFX
    /// <summary>
    /// 폭발 VFX를 풀에서 가져와 생성합니다.
    /// </summary>
    /// <param name="explosionWorldPosition">폭발 위치 (월드 좌표)</param>
    /// <param name="vfxType">생성할 VFX 타입</param>
    private void CreateExplosionVFX(Vector3 explosionWorldPosition, ExplosionVfxType vfxType)
    {
        if (vfxType == ExplosionVfxType.None)
        {
            Log("VFX 타입이 None입니다.");
            return;
        }

        if (_vfxPool == null || !_vfxPool.IsInitialized)
        {
            LogError("VFX 풀이 초기화되지 않았습니다!");
            return;
        }

        GameObject vfxInstance = _vfxPool.SpawnObject(vfxType, explosionWorldPosition, Quaternion.identity);

        if (vfxInstance == null)
        {
            LogError($"VFX 생성 실패: {vfxType}");
            return;
        }

        ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
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
            Log($"{particleSystems.Length}개의 ParticleSystem 재생: {explosionWorldPosition}");
        }
        else
        {
            LogWarning($"VFX에 ParticleSystem이 없습니다: {vfxType}");
        }

        _activeVfxInstances[vfxInstance] = Time.time;
    }

    /// <summary>
    /// Update에서 VFX 수명을 체크하고 만료된 VFX를 풀로 반환합니다.
    /// </summary>
    private void UpdateVfxLifetime()
    {
        if (_activeVfxInstances.Count == 0) return;

        float currentTime = Time.time;
        List<GameObject> expiredVfx = new List<GameObject>();

        foreach (var kvp in _activeVfxInstances)
        {
            if (kvp.Key == null)
            {
                expiredVfx.Add(kvp.Key);
                continue;
            }

            float elapsedTime = currentTime - kvp.Value;
            if (elapsedTime >= _vfxLifetime)
            {
                expiredVfx.Add(kvp.Key);
            }
        }

        foreach (var vfx in expiredVfx)
        {
            if (vfx != null && _vfxPool != null)
            {
                _vfxPool.ReturnObject(vfx);
                Log($"VFX 풀 반환: {vfx.name}");
            }
            _activeVfxInstances.Remove(vfx);
        }
    }
    #endregion

    #region Private Methods - Camera Shake
    /// <summary>
    /// 카메라 셰이크를 트리거합니다.
    /// </summary>
    /// <param name="intensity">셰이크 강도</param>
    private void TriggerCameraShake(float intensity)
    {
        if (_impulseSource == null)
        {
            Log("Impulse Source가 할당되지 않았습니다.");
            return;
        }

        _impulseSource.GenerateImpulse(intensity);
        Log($"카메라 셰이크 발생 (강도: {intensity})");
    }
    #endregion

    #region Private Methods - Physics
    private void ApplyExplosionForceInRadius(ExplosionProfileSO profile, Vector3 explosionWorldPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(
            explosionWorldPosition,
            profile.ExplosionRadius,
            profile.ExplosionLayerMask);

        HashSet<Rigidbody> processedInThisExplosion = new HashSet<Rigidbody>();
        int processedCount = 0;

        foreach (var col in colliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();

            if (rb != null && !processedInThisExplosion.Contains(rb))
            {
                rb.AddExplosionForce(
                    profile.ExplosionForce,
                    explosionWorldPosition,
                    profile.ExplosionRadius,
                    profile.UpwardModifier);

                processedInThisExplosion.Add(rb);
                processedCount++;
            }
        }

        Log($"물리력 적용 완료: 처리된 Rigidbody {processedCount}개");
    }
    #endregion

    #region Private Methods - Sequential Explosion
    private IEnumerator SequentialExplosionCoroutine(List<IExplodable> targets, float delayInterval, float tickingDuration)
    {
        List<IExplodable> validTargets = new List<IExplodable>();

        foreach (var target in targets)
        {
            if (target == null) continue;

            MonoBehaviour targetMono = target as MonoBehaviour;
            if (targetMono != null && targetMono.gameObject.activeInHierarchy)
            {
                validTargets.Add(target);
            }
        }

        if (validTargets.Count == 0)
        {
            LogError("활성화된 폭발 대상이 없습니다.");
            yield break;
        }

        Log($"{validTargets.Count}개의 대상 순차 폭발 준비");

        foreach (var target in validTargets)
        {
            target.StartTicking(tickingDuration);
        }

        if (tickingDuration > 0.001f)
        {
            yield return new WaitForSeconds(tickingDuration);
        }

        foreach (var target in validTargets)
        {
            Execute(target);

            if (delayInterval > 0)
            {
                yield return new WaitForSeconds(delayInterval);
            }
        }

        yield return StartCoroutine(HitStopCoroutine());

        Log("순차 폭발 완료");
    }
    #endregion

    #region Private Methods - Hitstop
    /// <summary>
    /// 히트스탑 연출 코루틴
    /// </summary>
    private IEnumerator HitStopCoroutine()
    {
        // 히트스탑 대기
        yield return new WaitForSeconds(_hitstopDelaySeconds);

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Log("히트스탑 시작");

        // 히트스탑 유지
        float waitTimer = 0f;
        while (waitTimer < _hitstopDuration)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 시간 속도 복구
        float elapsedTime = 0f;
        while (elapsedTime < _timeScaleRecoveryDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float curveSamplePoint = Mathf.Clamp01(elapsedTime / _timeScaleRecoveryDuration);
            float curveValue = _timeScaleRecoveryCurve.Evaluate(curveSamplePoint);

            Time.timeScale = Mathf.Lerp(0f, originalTimeScale, curveValue);

            yield return null;
        }

        Time.timeScale = originalTimeScale;
        Log("히트스탑 종료");
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}