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
    [SerializeField] private bool showDebugLogs = false;
    
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
            Debug.LogError($"[HingeTrigger] {gameObject.name}에 HingeJoint 컴포넌트가 없습니다!");
            enabled = false;
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[HingeTrigger] {gameObject.name} 초기화 완료. 트리거 각도: {triggerAngle}도");
        }
    }
    
    private void Update()
    {
        if (hingeJointComponent == null) return;
        
        float currentAngle = hingeJointComponent.angle;
        
        if (showDebugLogs)
        {
            Debug.Log($"[HingeTrigger] 현재 힌지 각도: {currentAngle:F1}도");
        }
        
        // 트리거 조건 확인
        if (currentAngle >= triggerAngle)
        {
            if (!hasTriggered)
            {
                hasTriggered = true;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[HingeTrigger] 트리거 발동! 각도: {currentAngle:F1}도 >= {triggerAngle}도");
                }
                
                // 과일 반복 생성 호출
                if (WatermelonGameManager.Instance != null)
                {
                    WatermelonGameManager.Instance.SpawnRandomFruitsRepeatedly();
                }
                else
                {
                    Debug.LogError("[HingeTrigger] WatermelonGameManager 인스턴스를 찾을 수 없습니다!");
                }
            }
        }
        else if (resetOnAngleDecrease && hasTriggered)
        {
            // 각도가 낮아지면 리셋
            hasTriggered = false;
            
            if (showDebugLogs)
            {
                Debug.Log($"[HingeTrigger] 트리거 리셋. 각도: {currentAngle:F1}도 < {triggerAngle}도");
            }
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