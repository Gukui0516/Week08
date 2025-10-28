
/// <summary>
/// 로그 레벨 타입
/// </summary>
public enum LogLevel
{
    DEBUG = 0,
    INFO = 1,
    WARNING = 2,
    ERROR = 3,
    CRITICAL = 4
}

/// <summary>
/// 로그 엔트리 데이터 구조
/// </summary>
public struct LogEntry
{
    /// <summary>로그 타입</summary>
    public LogLevel Type;

    /// <summary>Time.realtimeSinceStartup 기준 시간(초)</summary>
    public float RealtimeSeconds;

    /// <summary>로그 키</summary>
    public string Key;

    /// <summary>변환된 값 문자열</summary>
    public string Value;

    /// <summary>호출 파일 경로</summary>
    public string FilePath;

    /// <summary>호출 메서드명</summary>
    public string MemberName;

    /// <summary>호출 라인 번호</summary>
    public int LineNumber;
}

