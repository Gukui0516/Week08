using UnityEngine;

/// <summary>
/// Hinge Joint의 각도가 지정된 값 이상이 되면 과일 반복 생성을 트리거하는 컴포넌트입니다.
/// </summary>
public class HingeTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("트리거를 발동할 힌지 각도 (도)")]
    [SerializeField] private float triggerAngle = 80f;

    [Tooltip("한 번 트리거된 후 다시 발동할 수 있도록 리셋할지 여부")]
    [SerializeField] private bool resetOnAngleDecrease = true;

    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false; // [참고] 이 변수는 DEBUG 로그에서 더 이상 사용되지 않습니다.

    // 컴포넌트 참조
    private HingeJoint hingeJointComponent;

    // 트리거 상태
    private bool hasTriggered = false;

    private void Awake()
    {
        // HingeJoint 컴포넌트 가져오기
        hingeJointComponent = GetComponent<HingeJoint>();

        if (hingeJointComponent == null)
        {
            // [변경] LogLevel.ERROR
            LogSystem.PushLog(LogLevel.ERROR, "MissingComponent",
                $"[HingeTrigger] {gameObject.name}에 HingeJoint 컴포넌트가 없습니다!", useUnityDebug: true);
            enabled = false;
            return;
        }

        // [변경] DEBUG 로그 수정
        LogSystem.PushLog(LogLevel.DEBUG, "HingeTriggerInit",
            $"[HingeTrigger] {gameObject.name} 초기화 완료. 트리거 각도: {triggerAngle}도");
    }

    private void Update()
    {
        if (hingeJointComponent == null) return;

        float currentAngle = hingeJointComponent.angle;

        // [변경] DEBUG 로그 수정 (매 프레임 호출 주의)
        // 이 로그는 프레임마다 호출되므로 DEBUG 레벨로 유지합니다.
        LogSystem.PushLog(LogLevel.DEBUG, "HingeAngle", $"현재 힌지 각도: {currentAngle:F1}도");

        // 트리거 조건 확인
        if (currentAngle >= triggerAngle)
        {
            if (!hasTriggered)
            {
                hasTriggered = true;

                // [추가] f. 레버 당기기 전 Draggable 객체 로깅
                if (WatermelonGameManager.Instance != null)
                {
                    WatermelonGameManager.Instance.LogDraggableObjects("BeforeLeverPull");
                }
                else
                {
                    // [추가] f. 오류 로그
                    LogSystem.PushLog(LogLevel.WARNING, "MissingManager",
                        "[HingeTrigger] GameManager가 없어 BeforeLeverPull 로깅 실패", useUnityDebug: true);
                }

                // [추가] a. 레버 당김 이벤트 로그
                LogSystem.PushLog(LogLevel.INFO, "LeverPulled", $"Triggered at {currentAngle:F1}도 (Threshold: {triggerAngle}도)");

                // [변경] DEBUG 로그 수정
                LogSystem.PushLog(LogLevel.DEBUG, "HingeTriggered",
                    $"[HingeTrigger] 트리거 발동! 각도: {currentAngle:F1}도 >= {triggerAngle}도");

                // 과일 반복 생성 호출
                if (WatermelonGameManager.Instance != null)
                {
                    WatermelonGameManager.Instance.SpawnRandomFruitsRepeatedly();
                }
                else
                {
                    // [변경] LogLevel.ERROR
                    LogSystem.PushLog(LogLevel.ERROR, "MissingManager",
                        "[HingeTrigger] WatermelonGameManager 인스턴스를 찾을 수 없습니다!", useUnityDebug: true);
                }
            }
        }
        else if (resetOnAngleDecrease && hasTriggered)
        {
            // 각도가 낮아지면 리셋
            hasTriggered = false;

            // [변경] DEBUG 로그 수정
            LogSystem.PushLog(LogLevel.DEBUG, "HingeReset",
                $"[HingeTrigger] 트리거 리셋. 각도: {currentAngle:F1}도 < {triggerAngle}도");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (triggerAngle < 0f)
        {
            triggerAngle = 0f;
            Debug.LogWarning("[HingeTrigger] triggerAngle은 0 이상이어야 합니다.");
        }

        if (triggerAngle > 180f)
        {
            triggerAngle = 180f;
            Debug.LogWarning("[HingeTrigger] triggerAngle은 180 이하가 권장됩니다.");
        }
    }
#endif
}