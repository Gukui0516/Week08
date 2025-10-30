using UnityEngine;

/// <summary>
/// 시소(Plate)의 각도가 0.5도 변할 때마다
/// 양 끝(LeftSide, RightSide)의 위치를 로깅합니다.
/// </summary>
public class SeesawLogger : MonoBehaviour
{
    [Header("참조 (필수)")]
    [Tooltip("각도를 추적할 시소 판의 Transform")]
    [SerializeField] private Transform plateTransform;

    [Tooltip("위치를 기록할 왼쪽 끝 Transform")]
    [SerializeField] private Transform leftSideTransform;

    [Tooltip("위치를 기록할 오른쪽 끝 Transform")]
    [SerializeField] private Transform rightSideTransform;

    [Header("설정")]
    [Tooltip("로그를 기록할 최소 각도 변화 (도)")]
    [SerializeField] private float angleThreshold = 0.5f;

    [Tooltip("시소가 회전하는 축 (X축: Pitch, Y축: Yaw, Z축: Roll)")]
    [SerializeField] private TiltAxis tiltAxis = TiltAxis.Z;

    // 마지막으로 로그를 기록했던 시점의 각도
    private float lastLoggedAngle = 0f;

    public enum TiltAxis { X, Y, Z }

    void Start()
    {
        if (plateTransform == null || leftSideTransform == null || rightSideTransform == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "MissingReference", "SeesawLogger에 참조가 할당되지 않았습니다.", useUnityDebug: true);
            enabled = false;
            return;
        }

        // 시작 시점의 각도를 초기값으로 설정
        lastLoggedAngle = GetCurrentTiltAngle();
    }

    void Update()
    {
        // 1. 현재 각도 가져오기
        float currentAngle = GetCurrentTiltAngle();

        // 2. 마지막 로그 각도와의 차이 계산 (DeltaAngle 사용)
        // Mathf.DeltaAngle은 0~360도 순환을 안전하게 처리합니다.
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, lastLoggedAngle));

        // 3. 임계값(0.5도)을 넘었는지 확인
        if (angleDifference >= angleThreshold)
        {
            // 4. 위치 정보 가져오기
            Vector3 leftPos = leftSideTransform.position;
            Vector3 rightPos = rightSideTransform.position;

            // 5. 로그 시스템으로 호출
            string logValue = $"Angle:{currentAngle:F2}, Left:{leftPos}, Right:{rightPos}";
            LogSystem.PushLog(LogLevel.INFO, "SeesawTilt", logValue, true);

            // 6. 마지막 로그 각도를 현재 각도로 갱신 (중요)
            lastLoggedAngle = currentAngle;
        }
    }

    /// <summary>
    /// 설정된 축(Axis)에 따라 현재 각도를 반환합니다.
    /// </summary>
    private float GetCurrentTiltAngle()
    {
        switch (tiltAxis)
        {
            case TiltAxis.X:
                return plateTransform.eulerAngles.x;
            case TiltAxis.Y:
                return plateTransform.eulerAngles.y;
            case TiltAxis.Z:
                return plateTransform.eulerAngles.z;
            default:
                return 0f;
        }
    }
}