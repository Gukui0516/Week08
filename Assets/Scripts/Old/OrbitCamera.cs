using UnityEngine;

/// <summary>
/// 마우스와 키보드(WASD/QE) 입력을 사용해 타겟 주위를 공전하고 줌하는 카메라 스크립트입니다.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
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

    // 현재 카메라의 회전 각도를 저장하는 변수
    private float x = 0.0f;
    private float y = 0.0f;

    // 타겟 오프셋 (패닝으로 이동한 위치)
    private Vector3 targetOffset = Vector3.zero;

    /// <summary>타겟으로부터의 현재 거리</summary>
    public float Distance => distance;
    /// <summary>최소 거리</summary>
    public float DistanceMin => distanceMin;
    /// <summary>최대 거리</summary>
    public float DistanceMax => distanceMax;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if (target)
        {
            // --- 1. 마우스 궤도 회전 (우클릭) ---
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed;
                y -= Input.GetAxis("Mouse Y") * ySpeed;
            }

            // --- 2. 휠 버튼 패닝 (카메라 이동) ---
            if (Input.GetMouseButton(2))
            {
                float panX = -Input.GetAxis("Mouse X") * panSpeed;
                float panY = -Input.GetAxis("Mouse Y") * panSpeed;

                // 현재 카메라의 right와 up 벡터를 기준으로 이동
                targetOffset += transform.right * panX;
                targetOffset += transform.up * panY;
            }

            // --- 3. 키보드 궤도 회전 (WASD) ---
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

            // --- 4. 줌 (휠 & QE) ---
            if (!CursorManager.Instance.isGrabbed || (CursorManager.Instance.isGrabbed && Input.GetMouseButton(1)))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                distance -= scroll * zoomSpeed;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                distance += keyZoomSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                distance -= keyZoomSpeed * Time.deltaTime;
            }

            // --- 5. 값 제한 (Clamping) ---
            y = ClampAngle(y, yMinLimit, yMaxLimit);
            distance = Mathf.Clamp(distance, distanceMin, distanceMax);

            // --- 6. 카메라 위치/회전 최종 적용 ---
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);

            // 타겟 위치에 오프셋을 더해서 패닝 효과 적용
            Vector3 position = rotation * negDistance + target.position + targetOffset;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

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
}