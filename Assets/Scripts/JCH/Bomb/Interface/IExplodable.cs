using UnityEngine;

/// <summary>
/// 폭발 가능한 객체의 규약을 정의하는 인터페이스입니다.
/// </summary>
public interface IExplodable
{
    /// <summary>
    /// 이 객체의 폭발 설정 프로필을 반환합니다.
    /// </summary>
    ExplosionProfileSO GetExplosionProfile();

    /// <summary>
    /// 점멸 효과를 위한 Renderer를 반환합니다.
    /// </summary>
    Renderer GetRenderer();

    /// <summary>
    /// 폭발 후 호출되는 콜백입니다. 구현체에서 자유롭게 처리합니다.
    /// </summary>
    void AfterExploded();


    void StartTicking(float duration);  // Entity가 자체 코루틴 관리
    void StopTicking();                 // Entity가 자체 MPB 관리
}