using UnityEngine;

/// <summary>
/// 마우스와 키보드(WASD/QE) 입력을 사용해 타겟 주위를 공전하고 줌하는 카메라 스크립트입니다.
/// 상태 기반으로 회전/패닝/줌의 시작·종료 시점을 추적하고 로그를 기록합니다.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    #region Serialized Fields
    [Header("타겟 설정")]
    [Tooltip("카메라가 바라볼 타겟 오브젝트입니다.")]
    [SerializeField] private Transform target;

    [Header("궤도 설정")]
    [Tooltip("타겟으로부터의 초기 거리입니다.")]
    [SerializeField] private float distance = 5.0f;
    [Tooltip("마우스를 사용한 수평/수직 회전 속도입니다.")]
    [SerializeField] private float xSpeed = 120.0f;
    [SerializeField] private float ySpeed = 120.0f;

    [Header("키보드 설정")]
    [Tooltip("WASD 키를 사용한 수평/수직 회전 속도입니다.")]
    [SerializeField] private float keyOrbitSpeed = 60.0f;

    [Header("패닝 설정")]
    [Tooltip("휠 버튼을 사용한 카메라 이동 속도입니다.")]
    [SerializeField] private float panSpeed = 0.5f;

    [Header("줌 설정")]
    [Tooltip("마우스 휠 및 QE 키를 사용한 줌 속도입니다.")]
    [SerializeField] private float zoomSpeed = 5.0f;
    [SerializeField] private float keyZoomSpeed = 20.0f;

    [Header("제한 값")]
    [Tooltip("카메라의 최소/최대 고도(수직 각도)입니다.")]
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;
    [Tooltip("카메라의 최소/최대 줌 거리입니다.")]
    [SerializeField] private float distanceMin = .5f;
    [SerializeField] private float distanceMax = 15f;
    #endregion

    #region Private Fields - Camera State
    // 현재 카메라의 회전 각도
    private float x = 0.0f;
    private float y = 0.0f;

    // 타겟 오프셋 (패닝으로 이동한 위치)
    private Vector3 targetOffset = Vector3.zero;
    #endregion

    #region Private Fields - Operation States
    // 조작 상태 플래그
    private bool _isRotating = false;
    private bool _isPanning = false;
    private bool _isZooming = false;
    #endregion

    #region Private Fields - Cached Start Values
    // 회전 시작 시점 캐싱
    private float _rotationStartX = 0.0f;
    private float _rotationStartY = 0.0f;

    // 패닝 시작 시점 캐싱
    private Vector3 _panningStartOffset = Vector3.zero;

    // 줌 시작 시점 캐싱
    private float _zoomStartDistance = 0.0f;
    #endregion

    #region Properties
    /// <summary>타겟으로부터의 현재 거리</summary>
    public float Distance => distance;

    /// <summary>최소 거리</summary>
    public float DistanceMin => distanceMin;

    /// <summary>최대 거리</summary>
    public float DistanceMax => distanceMax;

    /// <summary>현재 회전 중인지 여부</summary>
    public bool IsRotating => _isRotating;

    /// <summary>현재 패닝 중인지 여부</summary>
    public bool IsPanning => _isPanning;

    /// <summary>현재 줌 중인지 여부</summary>
    public bool IsZooming => _isZooming;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        Initialize();
        LateInitialize();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 입력 처리 (상태 관리 포함)
        ProcessRotationInput();
        ProcessPanningInput();
        ProcessZoomInput();

        // 카메라 최종 트랜스폼 적용
        ApplyCameraTransform();
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
        // 상태 플래그 초기화
        _isRotating = false;
        _isPanning = false;
        _isZooming = false;

        // 캐싱 변수 초기화
        _rotationStartX = 0.0f;
        _rotationStartY = 0.0f;
        _panningStartOffset = Vector3.zero;
        _zoomStartDistance = 0.0f;
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        // 초기 회전 각도 설정
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // target 유효성 검증
        if (target == null)
        {
            LogSystem.PushLog(LogLevel.WARNING, "OrbitCamera", "Target is not assigned", true);
        }
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        // 진행 중인 상태가 있다면 강제 종료 처리
        if (_isRotating)
        {
            EndRotation();
        }

        if (_isPanning)
        {
            EndPanning();
        }

        if (_isZooming)
        {
            EndZoom();
        }
    }
    #endregion

    #region Private Methods - State Management
    /// <summary>회전 상태 시작 처리</summary>
    private void StartRotation()
    {
        _isRotating = true;
        _rotationStartX = x;
        _rotationStartY = y;

        //LogSystem.PushLog(LogLevel.DEBUG, "CameraRotateStart",
        //    $"Start(X:{x:F2}, Y:{y:F2})", true);
    }

    /// <summary>회전 상태 종료 처리</summary>
    private void EndRotation()
    {
        _isRotating = false;

        float deltaX = x - _rotationStartX;
        float deltaY = y - _rotationStartY;
        Vector2 totalDelta = new Vector2(deltaX, deltaY);

        LogSystem.PushLog(LogLevel.INFO, "CameraRotate", totalDelta);
    }

    /// <summary>패닝 상태 시작 처리</summary>
    private void StartPanning()
    {
        _isPanning = true;
        _panningStartOffset = targetOffset;

        //LogSystem.PushLog(LogLevel.INFO, "CameraPanningStart",
        //    $"Start({_panningStartOffset})", true);
    }

    /// <summary>패닝 상태 종료 처리</summary>
    private void EndPanning()
    {
        _isPanning = false;

        Vector3 totalDelta = targetOffset - _panningStartOffset;
        Vector2 recordedDelta = new Vector2(totalDelta.x, totalDelta.y);

        LogSystem.PushLog(LogLevel.INFO, "CameraPanning", recordedDelta);
    }

    /// <summary>줌 상태 시작 처리</summary>
    private void StartZoom()
    {
        _isZooming = true;
        _zoomStartDistance = distance;

        //LogSystem.PushLog(LogLevel.INFO, "CameraZoomStart",
        //    $"Start(Distance:{distance:F2})", true);
    }

    /// <summary>줌 상태 종료 처리</summary>
    private void EndZoom()
    {
        _isZooming = false;

        float totalDelta = distance - _zoomStartDistance;

        LogSystem.PushLog(LogLevel.INFO, "CameraZoom", totalDelta);
    }
    #endregion

    #region Private Methods - Input Processing
    /// <summary>회전 입력 감지 및 상태 관리</summary>
    private void ProcessRotationInput()
    {
        bool isInputActive = Input.GetMouseButton(1) ||
                             Input.GetKey(KeyCode.W) ||
                             Input.GetKey(KeyCode.S) ||
                             Input.GetKey(KeyCode.A) ||
                             Input.GetKey(KeyCode.D);

        // 상태 시작 감지
        if (isInputActive && !_isRotating)
        {
            StartRotation();
        }

        // 상태 종료 감지
        if (!isInputActive && _isRotating)
        {
            EndRotation();
        }

        // 입력 처리 (회전 중일 때만)
        if (_isRotating)
        {
            // 마우스 우클릭
            if (Input.GetMouseButton(1))
            {
                float deltaX = Input.GetAxis("Mouse X") * xSpeed;
                float deltaY = Input.GetAxis("Mouse Y") * ySpeed;

                x += deltaX;
                y -= deltaY;
            }

            // 키보드 WASD
            if (Input.GetKey(KeyCode.W))
            {
                y += keyOrbitSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                y -= keyOrbitSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                x += keyOrbitSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                x -= keyOrbitSpeed * Time.deltaTime;
            }
        }
    }

    /// <summary>패닝 입력 감지 및 상태 관리</summary>
    private void ProcessPanningInput()
    {
        bool isInputActive = Input.GetMouseButton(2);

        // 상태 시작 감지
        if (isInputActive && !_isPanning)
        {
            StartPanning();
        }

        // 상태 종료 감지
        if (!isInputActive && _isPanning)
        {
            EndPanning();
        }

        // 입력 처리 (패닝 중일 때만)
        if (_isPanning)
        {
            float panX = -Input.GetAxis("Mouse X") * panSpeed;
            float panY = -Input.GetAxis("Mouse Y") * panSpeed;

            targetOffset += transform.right * panX;
            targetOffset += transform.up * panY;
        }
    }

    /// <summary>줌 입력 감지 및 상태 관리</summary>
    private void ProcessZoomInput()
    {
        bool isScrollActive = false;
        bool isKeyActive = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);

        // 마우스 휠 입력 체크
        if (!CursorManager.Instance.isGrabbed || (CursorManager.Instance.isGrabbed && Input.GetMouseButton(1)))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                isScrollActive = true;
            }
        }

        bool isInputActive = isScrollActive || isKeyActive;

        // 상태 시작 감지
        if (isInputActive && !_isZooming)
        {
            StartZoom();
        }

        // 상태 종료 감지
        if (!isInputActive && _isZooming)
        {
            EndZoom();
        }

        // 입력 처리 (줌 중일 때만)
        if (_isZooming)
        {
            // 마우스 휠
            if (!CursorManager.Instance.isGrabbed || (CursorManager.Instance.isGrabbed && Input.GetMouseButton(1)))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0f)
                {
                    distance += -scroll * zoomSpeed;
                }
            }

            // Q/E 키
            if (Input.GetKey(KeyCode.Q))
            {
                distance += keyZoomSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                distance -= keyZoomSpeed * Time.deltaTime;
            }
        }
    }

    /// <summary>카메라 최종 위치 및 회전 적용</summary>
    private void ApplyCameraTransform()
    {
        if (target == null)
            return;

        // 값 제한
        y = ClampAngle(y, yMinLimit, yMaxLimit);
        distance = Mathf.Clamp(distance, distanceMin, distanceMax);

        // 카메라 위치/회전 최종 적용
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);

        Vector3 position = rotation * negDistance + target.position + targetOffset;

        transform.rotation = rotation;
        transform.position = position;
    }
    #endregion

    #region Private Methods - Helpers
    /// <summary>
    /// 각도를 주어진 최소값과 최대값 사이로 제한하는 헬퍼 함수입니다.
    /// </summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
    #endregion
}