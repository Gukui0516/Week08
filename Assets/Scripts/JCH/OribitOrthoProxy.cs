using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// OrbitCamera의 distance를 읽어 Orthographic Size로 변환하는 Proxy
/// </summary>
public class OrbitOrthoProxy : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [SerializeField] private OrbitCamera _orbitCamera;

    [TabGroup("Settings")]
    [Tooltip("Orthographic Size 최소값")]
    [SerializeField] private float _minSize = 5f;

    [TabGroup("Settings")]
    [Tooltip("Orthographic Size 최대값")]
    [SerializeField] private float _maxSize = 20f;

    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Fields
    private Camera _camera;
    private CinemachineCamera _cinemachineCamera;
    private bool _isOrthographic;
    #endregion

    #region Properties
    /// <summary>Orthographic 카메라 여부</summary>
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsOrthographic => _isOrthographic;

    /// <summary>디버그 로그 출력 여부</summary>
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

    private void LateUpdate()
    {
        UpdateOrthographicSize();
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
        _camera = GetComponent<Camera>();
        _cinemachineCamera = GetComponent<CinemachineCamera>();

        // Orthographic 여부 확인
        if (_cinemachineCamera != null)
        {
            _isOrthographic = _cinemachineCamera.Lens.Orthographic;
            Log($"Cinemachine Orthographic: {_isOrthographic}");
        }
        else if (_camera != null)
        {
            _isOrthographic = _camera.orthographic;
            Log($"Camera Orthographic: {_isOrthographic}");
        }
        else
        {
            LogError("Camera 또는 CinemachineCamera 컴포넌트를 찾을 수 없습니다.");
        }
    }

    //// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
      
        if (_orbitCamera == null)
        {
            LogWarning("OrbitCamera가 할당되지 않았습니다.", true);
        }

        if (!_isOrthographic)
        {
            LogWarning("Orthographic 카메라가 아닙니다. 이 Proxy는 동작하지 않습니다.", true);
        }
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        _camera = null;
        _cinemachineCamera = null;
    }
    #endregion

    #region Private Methods - Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.Log($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.LogWarning($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=magenta>[{GetType().Name}]</color> {message}", this);
    }
    #endregion

    #region Private Methods - Size Update
    /// <summary>OrbitCamera의 distance를 읽어 ortho size 업데이트</summary>
    private void UpdateOrthographicSize()
    {
        if (_orbitCamera == null || !_isOrthographic)
            return;

        float distance = _orbitCamera.Distance;
        float distMin = _orbitCamera.DistanceMin;
        float distMax = _orbitCamera.DistanceMax;

        // 비율 계산: 0 ~ 1 범위
        float ratio = Mathf.InverseLerp(distMin, distMax, distance);

        // 비율을 ortho size 범위로 매핑
        float targetSize = Mathf.Lerp(_minSize, _maxSize, ratio);

        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Lens.OrthographicSize = targetSize;
        }
        else if (_camera != null)
        {
            _camera.orthographicSize = targetSize;
        }
    }
    #endregion
}