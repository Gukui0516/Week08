
/// <summary>
/// 복잡한 사용자 정의 타입 로깅 인터페이스 (선택적)
/// </summary>
public interface ILoggable
{
    /// <summary>
    /// 로그 문자열로 변환
    /// </summary>
    string ToLogString();
}