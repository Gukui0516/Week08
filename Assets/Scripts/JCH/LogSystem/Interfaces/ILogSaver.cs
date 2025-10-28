using System.Threading.Tasks;

/// <summary>
/// 로그 저장 전략 인터페이스
/// </summary>
public interface ILogSaver
{
    /// <summary>
    /// 로그 엔트리 배열을 비동기로 저장
    /// </summary>
    /// <param name="entries">로그 엔트리 배열</param>
    /// <param name="count">저장할 엔트리 개수</param>
    Task SaveAsync(LogEntry[] entries, int count);
}