// LogConverter.cs
using System;
using System.IO;
using UnityEngine;

/// <summary>
/// LogEntry를 Unity Debug Console 형식으로 변환
/// </summary>
public static class LogConverter
{
    #region Private Constants
    private const string COLOR_DEBUG = "#888888";
    private const string COLOR_INFO = "#FFFFFF";
    private const string COLOR_WARNING = "#FFFF00";
    private const string COLOR_ERROR = "#FF0000";
    private const string CRITICAL_PREFIX = "⚠ ";
    #endregion

    #region Public Methods - Unity Debug Integration
    /// <summary>
    /// LogEntry를 Unity Debug Console에 출력
    /// </summary>
    /// <param name="entry">로그 엔트리</param>
    public static void ToUnityLog(LogEntry entry)
    {
        string className = GetCallerClassName(entry.FilePath);
        string classColorHex = GetClassColorHex(className);
        string typeColorHex = GetLogTypeColorHex(entry.Type);

        string formattedMessage = FormatMessage(entry, classColorHex, typeColorHex);

        CallUnityDebugMethod(entry.Type, formattedMessage);
    }
    #endregion

    #region Public Methods - Test Support
    /// <summary>
    /// 포맷된 메시지 반환 (Unity Debug 호출 없이)
    /// </summary>
    /// <param name="entry">로그 엔트리</param>
    /// <returns>color 태그 포함 포맷된 메시지</returns>
    public static string GetFormattedMessage(LogEntry entry)
    {
        string className = GetCallerClassName(entry.FilePath);
        string classColorHex = GetClassColorHex(className);
        string typeColorHex = GetLogTypeColorHex(entry.Type);
        return FormatMessage(entry, classColorHex, typeColorHex);
    }
    #endregion

    #region Private Methods - Color Generation
    /// <summary>
    /// 클래스명에서 색상 해시 생성 (HSL 기반)
    /// </summary>
    /// <param name="className">클래스명</param>
    /// <returns>16진수 색상 코드 (#RRGGBB)</returns>
    private static string GetClassColorHex(string className)
    {
        if (string.IsNullOrEmpty(className))
            return "#AAAAAA";

        int hash = className.GetHashCode();

        // Hue만 해시로 결정, Saturation/Lightness 고정
        float h = (Math.Abs(hash) % 360) / 360f;
        float s = 0.70f; // 채도 70%
        float l = 0.60f; // 명도 60% (검은 배경 최적)

        Color color = HSLToRGB(h, s, l);

        int r = Mathf.Clamp((int)(color.r * 255), 0, 255);
        int g = Mathf.Clamp((int)(color.g * 255), 0, 255);
        int b = Mathf.Clamp((int)(color.b * 255), 0, 255);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// LogType에 따른 색상 반환
    /// </summary>
    /// <param name="type">로그 타입</param>
    /// <returns>16진수 색상 코드 (#RRGGBB)</returns>
    private static string GetLogTypeColorHex(LogLevel type)
    {
        return type switch
        {
            LogLevel.DEBUG => COLOR_DEBUG,
            LogLevel.INFO => COLOR_INFO,
            LogLevel.WARNING => COLOR_WARNING,
            LogLevel.ERROR => COLOR_ERROR,
            LogLevel.CRITICAL => COLOR_ERROR,
            _ => COLOR_INFO
        };
    }

    /// <summary>
    /// HSL을 RGB로 변환
    /// </summary>
    /// <param name="h">Hue (0~1)</param>
    /// <param name="s">Saturation (0~1)</param>
    /// <param name="l">Lightness (0~1)</param>
    /// <returns>RGB Color</returns>
    private static Color HSLToRGB(float h, float s, float l)
    {
        float c = (1f - Mathf.Abs(2f * l - 1f)) * s;
        float x = c * (1f - Mathf.Abs((h * 6f) % 2f - 1f));
        float m = l - c / 2f;

        float r = 0f, g = 0f, b = 0f;

        if (h < 1f / 6f) { r = c; g = x; b = 0; }
        else if (h < 2f / 6f) { r = x; g = c; b = 0; }
        else if (h < 3f / 6f) { r = 0; g = c; b = x; }
        else if (h < 4f / 6f) { r = 0; g = x; b = c; }
        else if (h < 5f / 6f) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return new Color(r + m, g + m, b + m);
    }
    #endregion

    #region Private Methods - Formatting
    /// <summary>
    /// 포맷된 메시지 생성
    /// </summary>
    /// <param name="entry">로그 엔트리</param>
    /// <param name="classColorHex">클래스 태그 색상</param>
    /// <param name="typeColorHex">타입 태그 및 내용 색상</param>
    /// <returns>포맷된 메시지</returns>
    private static string FormatMessage(LogEntry entry, string classColorHex, string typeColorHex)
    {
        string className = GetCallerClassName(entry.FilePath);
        string classTag = $"<color={classColorHex}>[{className}]</color>";

        string typePrefix = entry.Type == LogLevel.CRITICAL ? CRITICAL_PREFIX : "";
        string typeTag = $"[{entry.Type}]";
        string mainContent = $"{typeTag} {entry.Key}={entry.Value}";

        string callerInfo = $"\n  at {entry.FilePath}:{entry.MemberName}({entry.LineNumber})";

        string coloredContent = $"<color={typeColorHex}>{typePrefix}{mainContent}{callerInfo}</color>";

        return $"{classTag} {coloredContent}";
    }

    /// <summary>
    /// Unity Debug 메서드 호출
    /// </summary>
    /// <param name="type">로그 타입</param>
    /// <param name="message">포맷된 메시지</param>
    private static void CallUnityDebugMethod(LogLevel type, string message)
    {
        switch (type)
        {
            case LogLevel.DEBUG:
            case LogLevel.INFO:
                Debug.Log(message);
                break;
            case LogLevel.WARNING:
                Debug.LogWarning(message);
                break;
            case LogLevel.ERROR:
            case LogLevel.CRITICAL:
                Debug.LogError(message);
                break;
        }
    }
    #endregion

    #region Private Methods - File Path Processing
    /// <summary>
    /// 파일 경로에서 클래스명 추출
    /// </summary>
    /// <param name="filePath">파일 경로 (예: "Assets/Scripts/Player.cs")</param>
    /// <returns>클래스명 (예: "Player")</returns>
    private static string GetCallerClassName(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "Unknown";

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return string.IsNullOrEmpty(fileName) ? "Unknown" : fileName;
    }
    #endregion

}