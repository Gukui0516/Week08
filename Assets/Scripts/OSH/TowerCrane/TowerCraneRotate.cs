using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    // 인스펙터에서 회전 방향을 선택할 수 있는 옵션
    public enum RotationDirection
    {
        Right, // 오른쪽 (시계 방향)
        Left   // 왼쪽 (반시계 방향)
    }

    [Header("회전 설정")]
    [Tooltip("초당 회전하는 각도입니다.")]
    public float rotationSpeed = 1f; // 초당 1도

    [Tooltip("회전할 방향을 선택합니다.")]
    public RotationDirection direction = RotationDirection.Right;

    // Update는 매 프레임마다 호출됩니다.
    void Update()
    {
        // 1. 방향 벡터 결정
        // Y축(Vector3.up)을 기준으로 회전합니다.
        Vector3 axis = (direction == RotationDirection.Right) ? Vector3.up : Vector3.down;

        // 2. 회전 적용
        // Time.deltaTime을 곱해 초당 'rotationSpeed'만큼 일정하게 회전시킵니다.
        transform.Rotate(axis * rotationSpeed * Time.deltaTime);
    }
}