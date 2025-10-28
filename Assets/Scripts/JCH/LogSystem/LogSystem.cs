// LogSystem.cs
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 전역 로깅 시스템 인터페이스
/// </summary>
public static class LogSystem
{
    #region Private Fields
    private static DateTime _sessionStartTime;
    private static LogRuntime _runtimeInstance;
    #endregion

    #region Properties
    /// <summary>세션 시작 시간</summary>
    public static DateTime SessionStartTime => _sessionStartTime;
    #endregion

    #region Static Constructor
    static LogSystem()
    {
        _sessionStartTime = DateTime.Now;
        EnsureRuntimeInstance();
    }
    #endregion

    #region Public Methods - Logging Interface
    /// <summary>
    /// 로그 기록
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="type">로그 타입</param>
    /// <param name="key">로그 키</param>
    /// <param name="value">로그 값</param>
    /// <param name="useUnityDebug">Unity Console 출력 여부</param>
    /// <param name="filePath">호출 파일 경로</param>
    /// <param name="memberName">호출 메서드명</param>
    /// <param name="lineNumber">호출 라인 번호</param>
    public static void PushLog<T>(
        LogLevel type,
        string key,
        T value,
        bool useUnityDebug = false,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        EnsureRuntimeInstance();

        string convertedValue = ConvertToString(value);

        LogEntry entry = new LogEntry
        {
            Type = type,
            RealtimeSeconds = Time.realtimeSinceStartup,
            Key = key,
            Value = convertedValue,
            FilePath = filePath,
            MemberName = memberName,
            LineNumber = lineNumber
        };

        _runtimeInstance.AddEntry(entry);

        if (useUnityDebug)
        {
            LogConverter.ToUnityLog(entry);
        }
    }

    /// <summary>
    /// 버퍼의 로그를 즉시 파일에 저장
    /// </summary>
    public static void FlushLogs()
    {
        if (_runtimeInstance != null)
        {
            _runtimeInstance.FlushBuffer();
        }
    }
    #endregion

    #region Private Methods - Type Conversion
    /// <summary>
    /// 제네릭 값을 문자열로 변환
    /// </summary>
    private static string ConvertToString<T>(T value)
    {
        return value switch
        {
            // 사용자 확장
            ILoggable loggable => loggable.ToLogString(),

            // 기본 타입
            float f => f.ToString("F3"),
            int i => i.ToString(),
            bool b => b.ToString(),
            string s => s,

            // Unity 타입 감지 → LogUnityTypeConverter 위임
            _ when value?.GetType().Namespace?.StartsWith("UnityEngine") == true
                => LogUnityTypeConverter.Convert(value),

            // 폴백
            null => "null",
            _ => value.ToString()
        };
    }
    #endregion

    #region Private Methods - Runtime Management
    /// <summary>
    /// LogRuntime 인스턴스 확보
    /// </summary>
    private static void EnsureRuntimeInstance()
    {
        if (_runtimeInstance != null)
            return;

        _runtimeInstance = UnityEngine.Object.FindObjectOfType<LogRuntime>();

        if (_runtimeInstance == null)
        {
            GameObject runtimeObject = new GameObject("LogRuntime");
            _runtimeInstance = runtimeObject.AddComponent<LogRuntime>();
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);
        }
    }
    #endregion
}
