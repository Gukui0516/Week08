using UnityEngine;

/// <summary>
/// 플레이어(Draggable)에 부착하는 스크립트입니다.
/// Bomb 태그를 가진 오브젝트와 충돌하면 해당 Bomb의 중력을 활성화하고 모든 Freeze를 해제합니다.
/// 
/// [사용법]
/// 1. 플레이어(Draggable 태그) 오브젝트에 이 스크립트 부착
/// 2. Bomb 오브젝트 설정:
///    - Tag를 "Bomb"로 설정
///    - Collider 추가 (Is Trigger 체크)
///    - Rigidbody 추가
///    - 초기 Rigidbody 설정: Use Gravity OFF, Constraints Freeze 설정
/// </summary>
public class BombActivationTrigger : MonoBehaviour
{
    [Header("디버그")]
    [Tooltip("활성화 정보를 콘솔에 출력")]
    [SerializeField] private bool showDebugInfo = true;

    /// <summary>
    /// Bomb 태그를 가진 오브젝트와 충돌했을 때 호출됩니다.
    /// 해당 Bomb의 Rigidbody를 가져와서 중력을 활성화하고 모든 Freeze를 해제합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Bomb 태그 확인
        if (!other.CompareTag("Bomb"))
            return;

        // Bomb의 Rigidbody 가져오기
        Rigidbody bombRb = other.GetComponent<Rigidbody>();

        if (bombRb == null)
        {
            Debug.LogWarning($"{other.gameObject.name}: Rigidbody 컴포넌트가 없습니다.");
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"{other.gameObject.name}: 활성화 전 - Gravity: {bombRb.useGravity}, Constraints: {bombRb.constraints}");
        }

        // 중력 활성화
        bombRb.useGravity = true;

        // 모든 Freeze 해제
        bombRb.constraints = RigidbodyConstraints.None;

        if (showDebugInfo)
        {
            Debug.Log($"{other.gameObject.name}: 활성화 완료 - Gravity: {bombRb.useGravity}, Constraints: {bombRb.constraints}");
        }
    }
}