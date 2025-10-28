using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// CSV 파일 로그 저장 구현
/// </summary>
///
public class LogSave : ILogSaver
{
    #region Private Fields
    private string _logDirectoryPath;
    private string _sessionFilePath;
    private DateTime _sessionStartTime;
    #endregion

    #region Constructor
    /// <summary>생성자에서 초기화 완료</summary>
    public LogSave()
    {
        _sessionStartTime = LogSystem.SessionStartTime;
        _logDirectoryPath = GetLogDirectoryPath();
        _sessionFilePath = CreateSessionFilePath();

        // 디렉토리 생성
        if (!Directory.Exists(_logDirectoryPath))
        {
            Directory.CreateDirectory(_logDirectoryPath);
        }

        // CSV 헤더 작성
        WriteHeader();
    }
    #endregion

    #region Public Methods - ILogSaver Implementation
    /// <summary>
    /// 로그 엔트리 배열을 비동기로 CSV 파일에 저장 (UTF-8 with BOM)
    /// </summary>
    /// <param name="entries">로그 엔트리 배열</param>
    /// <param name="count">저장할 엔트리 개수</param>
    public async Task SaveAsync(LogEntry[] entries, int count)
    {
        if (count <= 0)
            return;

        try
        {
            StringBuilder sb = new StringBuilder(count * 100);

            for (int i = 0; i < count; i++)
            {
                string csvLine = EntryToCsvLine(entries[i]);
                sb.AppendLine(csvLine);
            }

            await File.AppendAllTextAsync(_sessionFilePath, sb.ToString(), System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogSave] Failed to save logs: {ex.Message}");
        }
    }
    #endregion

    #region Private Methods - File Operations
    /// <summary>
    /// 로그 디렉토리 경로 생성
    /// </summary>
    private string GetLogDirectoryPath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "..", "Logs");
#else
    return Path.Combine(Application.persistentDataPath, "Logs");
#endif
    }

    /// <summary>
    /// 세션 파일 경로 생성
    /// </summary>
    private string CreateSessionFilePath()
    {
        string fileName = $"GameLog_{_sessionStartTime:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(_logDirectoryPath, fileName);
    }

    /// <summary>
    /// CSV 헤더 작성 (UTF-8 with BOM)
    /// </summary>
    private void WriteHeader()
    {
        try
        {
            string header = "LogType,Timestamp,Key,Value";
            File.WriteAllText(_sessionFilePath, header + Environment.NewLine, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogSave] Failed to write header: {ex.Message}");
        }
    }
    #endregion

        #region Private Methods - CSV Formatting
        /// <summary>
        /// LogEntry를 CSV 행으로 변환
        /// </summary>
    private string EntryToCsvLine(LogEntry entry)
    {
        string timestamp = ConvertTimestamp(entry.RealtimeSeconds);
        string escapedValue = EscapeCsvValue(entry.Value);

        return $"{entry.Type},{timestamp},{entry.Key},{escapedValue}";
    }

    /// <summary>
    /// 타임스탬프 변환 (RealtimeSeconds → ISO 8601)
    /// </summary>
    private string ConvertTimestamp(float realtimeSeconds)
    {
        DateTime logTime = _sessionStartTime.AddSeconds(realtimeSeconds);
        return logTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
    }

    /// <summary>
    /// CSV 이스케이프 처리
    /// </summary>
    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // 쉼표, 따옴표, 개행 포함 시 이스케이프
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            // 따옴표를 두 개로 이스케이프
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
    #endregion
}