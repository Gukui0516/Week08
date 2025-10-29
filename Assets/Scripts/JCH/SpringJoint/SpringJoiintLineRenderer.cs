// SpringJointLineRenderer.cs
using UnityEngine;

/// <summary>
/// SpringJoint를 시각화하는 LineRenderer 컴포넌트
/// </summary>
public class SpringJointLineRenderer : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private SpringJoint _springJoint;
    [SerializeField] private LineRenderer _lineRenderer;

    [Header("Line Rendering")]
    [SerializeField] private float _lineWidthWorld = 0.05f;

    [Header("Color Mode")]
    [SerializeField] private bool _useGradient = true;
    [SerializeField] private Color _solidColor = Color.cyan;

    [Header("Gradient Settings")]
    [SerializeField] private Gradient _stretchGradient;
    [SerializeField] private float _maxStretchRatio = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Rigidbody _rigidbody;
    private float _restDistance;
    #endregion

    #region Properties
    public bool IsRendering => _lineRenderer != null && _lineRenderer.enabled;
    public SpringJoint TargetSpringJoint => _springJoint;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LateInitialize(_springJoint);
    }

    private void Update()
    {
        if (!IsSpringJointValid())
        {
            if (_lineRenderer != null && _lineRenderer.enabled)
            {
                _lineRenderer.enabled = false;
                LogWarning("SpringJoint invalid - LineRenderer disabled", true);
            }
            return;
        }

        if (_lineRenderer != null && !_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
        }

        UpdateLinePositions();
        UpdateLineColor();
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
        _rigidbody = GetComponent<Rigidbody>();

        SetupLineRenderer();

        if (_useGradient && _stretchGradient == null)
        {
            InitializeDefaultGradient();
        }

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }

        Log("SpringJointLineRenderer initialized");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    /// <param name="springJoint">시각화할 SpringJoint</param>
    public void LateInitialize(SpringJoint springJoint)
    {
        if (springJoint == null)
        {
            LogError("SpringJoint is null - cannot initialize");
            return;
        }

        _springJoint = springJoint;

        if (_rigidbody == null)
        {
            LogError("Rigidbody not found - SpringJoint requires Rigidbody");
            return;
        }

        if (_springJoint.connectedBody == null)
        {
            LogError("SpringJoint.connectedBody is null - cannot visualize");
            return;
        }

        Vector3 startPos = CalculateStartWorldPosition();
        Vector3 endPos = CalculateEndWorldPosition();
        _restDistance = Vector3.Distance(startPos, endPos);

        Log($"Rest distance: {_restDistance:F3}");
        Log($"Start: {startPos}, End: {endPos}");

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
        }

        Log($"LateInitialize completed for SpringJoint on {_springJoint.gameObject.name}");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }

        Log("SpringJointLineRenderer cleaned up");
    }
    #endregion

    #region Public Methods - Rendering Control
    /// <summary>렌더링 활성화/비활성화</summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetRenderingActive(bool isActive)
    {
        if (_lineRenderer == null)
        {
            LogWarning("LineRenderer is null - cannot set rendering active");
            return;
        }

        _lineRenderer.enabled = isActive;
        Log($"Rendering set to {(isActive ? "active" : "inactive")}");
    }
    #endregion

    #region Private Methods - Line Update
    /// <summary>LineRenderer 위치 업데이트</summary>
    private void UpdateLinePositions()
    {
        if (!IsSpringJointValid() || _lineRenderer == null)
        {
            return;
        }

        Vector3 startPos = CalculateStartWorldPosition();
        Vector3 endPos = CalculateEndWorldPosition();

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    /// <summary>LineRenderer 색상 업데이트</summary>
    private void UpdateLineColor()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        if (_useGradient && _stretchGradient != null)
        {
            float stretchRatio = CalculateStretchRatio();
            float normalizedRatio = Mathf.Clamp01(stretchRatio / _maxStretchRatio);
            Color evaluatedColor = _stretchGradient.Evaluate(normalizedRatio);

            _lineRenderer.startColor = evaluatedColor;
            _lineRenderer.endColor = evaluatedColor;

            // 디버깅
            if (_isDebugLogging)
            {
                Log($"StretchRatio={stretchRatio:F2}, Normalized={normalizedRatio:F2}, Color={evaluatedColor}");
            }
        }
        else
        {
            _lineRenderer.startColor = _solidColor;
            _lineRenderer.endColor = _solidColor;

            // 디버깅
            if (_isDebugLogging && _useGradient && _stretchGradient == null)
            {
                LogWarning("Gradient is null - using solid color");
            }
        }
    }
    #endregion

    #region Private Methods - Position Calculation
    /// <summary>시작점 월드 위치 계산 (SpringJoint의 Rigidbody 기준)</summary>
    /// <returns>시작점 월드 좌표</returns>
    private Vector3 CalculateStartWorldPosition()
    {
        if (_springJoint == null || _rigidbody == null)
        {
            return transform.position;
        }

        return _rigidbody.transform.TransformPoint(_springJoint.anchor);
    }

    /// <summary>끝점 월드 위치 계산 (ConnectedBody 기준)</summary>
    /// <returns>끝점 월드 좌표</returns>
    private Vector3 CalculateEndWorldPosition()
    {
        if (_springJoint == null || _springJoint.connectedBody == null)
        {
            return transform.position;
        }

        return _springJoint.connectedBody.transform.TransformPoint(_springJoint.connectedAnchor);
    }

    /// <summary>Spring 늘어남 비율 계산</summary>
    /// <returns>늘어남 비율</returns>
    private float CalculateStretchRatio()
    {
        if (_springJoint == null || _restDistance <= 0.0f)
        {
            return 1.0f;
        }

        Vector3 startPos = CalculateStartWorldPosition();
        Vector3 endPos = CalculateEndWorldPosition();
        float currentDistance = Vector3.Distance(startPos, endPos);

        return currentDistance / _restDistance;
    }
    #endregion

    #region Private Methods - Initialization Helpers
    /// <summary>LineRenderer 컴포넌트 설정</summary>
    private void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            Log("LineRenderer component added");
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = _lineWidthWorld;
        _lineRenderer.endWidth = _lineWidthWorld;

        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        if (!_useGradient)
        {
            _lineRenderer.startColor = _solidColor;
            _lineRenderer.endColor = _solidColor;
        }

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.alignment = LineAlignment.View;

        Log("LineRenderer setup completed");
    }

    /// <summary>기본 Gradient 초기화</summary>
    private void InitializeDefaultGradient()
    {
        _stretchGradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(Color.blue, 0.0f);
        colorKeys[1] = new GradientColorKey(Color.green, 0.5f);
        colorKeys[2] = new GradientColorKey(Color.red, 1.0f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

        _stretchGradient.SetKeys(colorKeys, alphaKeys);

        Log("Default gradient initialized (Blue → Green → Red)");
    }
    #endregion

    #region Private Methods - Validation
    /// <summary>SpringJoint 유효성 검사</summary>
    /// <returns>유효 여부</returns>
    private bool IsSpringJointValid()
    {
        return _springJoint != null
            && _rigidbody != null
            && _springJoint.connectedBody != null;
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            LogSystem.DebugLog(message, null, this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            LogSystem.PushLog(LogLevel.WARNING, GetType().Name, message, true);
    }

    /// <summary>에러 로그 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        LogSystem.PushLog(LogLevel.ERROR, GetType().Name, message, true);
    }
    #endregion
}