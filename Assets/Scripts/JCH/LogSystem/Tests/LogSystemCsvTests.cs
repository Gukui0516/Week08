
#if UNITY_EDITOR
// LogSystemCsvTests.cs
using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// CSV 파일 생성 및 포맷 검증 테스트
/// </summary>
public class LogSystemCsvTests
{
    #region Private Fields
    private GameObject _runtimeObject;
    private LogRuntime _runtime;
    private string _logDirectoryPath;
    #endregion

    #region Setup and Teardown
    [SetUp]
    public void Setup()
    {
        _runtimeObject = new GameObject("TestLogRuntime");
        _runtime = _runtimeObject.AddComponent<LogRuntime>();
        _runtime.Initialize();
        _logDirectoryPath = GetLogDirectoryPath();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupRuntime();
        CleanupLogFiles();
    }
    #endregion

    #region Test Methods - File Creation
    /// <summary>
    /// 시나리오 4-1: 파일 생성 및 경로 확인
    /// </summary>
    [Test]
    public void Test_File_Creation_And_Path()
    {
        // Given: 로그 시스템 초기화 완료 (Setup에서)
        _runtime.ClearBufferForTest();

        // When: 첫 로그 기록 후 FlushLogs
        LogSystem.PushLog(LogLevel.INFO, "Test", 1);
        LogSystem.FlushLogs();

        // Then: 파일 존재 확인
        string csvPath = GetLatestCsvFilePath();
        Assert.IsNotNull(csvPath, "CSV 파일이 생성되어야 합니다");
        Assert.IsTrue(File.Exists(csvPath), $"CSV 파일이 존재해야 합니다: {csvPath}");

        // 파일명 패턴 확인 "GameLog_yyyyMMdd_HHmmss.csv"
        string fileName = Path.GetFileName(csvPath);
        Assert.IsTrue(fileName.StartsWith("GameLog_"), "파일명은 GameLog_로 시작해야 합니다");
        Assert.IsTrue(fileName.EndsWith(".csv"), "파일 확장자는 .csv여야 합니다");
    }
    #endregion

    #region Test Methods - CSV Header
    /// <summary>
    /// 시나리오 4-2: CSV 헤더 검증
    /// </summary>
    [Test]
    public void Test_CSV_Header_Format()
    {
        // Given: 로그 기록 및 플러시
        _runtime.ClearBufferForTest();
        LogSystem.PushLog(LogLevel.INFO, "Test", 1);
        LogSystem.FlushLogs();

        // When: 파일 첫 줄 읽기
        string csvPath = GetLatestCsvFilePath();
        string firstLine = File.ReadLines(csvPath).First();

        // Then: 헤더 포맷 확인
        Assert.AreEqual("LogType,Timestamp,Key,Value", firstLine,
            "CSV 헤더는 'LogType,Timestamp,Key,Value' 형식이어야 합니다");
    }
    #endregion

    #region Test Methods - Data Row Format
    /// <summary>
    /// 시나리오 4-3: 데이터 행 포맷 검증
    /// </summary>
    [Test]
    public void Test_Data_Row_Format()
    {
        // Given: 특정 로그 기록
        _runtime.ClearBufferForTest();
        LogSystem.PushLog(LogLevel.INFO, "Health", 85.567f);
        LogSystem.FlushLogs();

        // When: 두 번째 줄 읽기 (첫 줄은 헤더)
        string csvPath = GetLatestCsvFilePath();
        string dataLine = File.ReadLines(csvPath).Skip(1).First();

        // Then: 포맷 확인 "INFO,2025-10-24T14:30:45.123,Health,85.567"
        Assert.IsTrue(dataLine.StartsWith("INFO,"), "데이터 행은 LogType으로 시작해야 합니다");
        Assert.IsTrue(dataLine.Contains(",Health,"), "Key가 포함되어야 합니다");
        Assert.IsTrue(dataLine.EndsWith(",85.567"), "Value가 마지막에 위치해야 합니다");

        // 필드 개수 확인 (4개: LogType, Timestamp, Key, Value)
        int fieldCount = dataLine.Split(',').Length;
        Assert.AreEqual(4, fieldCount, "데이터 행은 4개 필드를 가져야 합니다");
    }
    #endregion

    /// <summary>
    /// 시나리오 4-4: CSV 이스케이프 처리
    /// </summary>
    [Test]
    public void Test_CSV_Escape_SpecialCharacters()
    {
        // Given: 특수문자 포함 값
        _runtime.ClearBufferForTest();
        string testValue = "Error, \"quoted\", text";
        LogSystem.PushLog(LogLevel.ERROR, "Message", testValue);
        LogSystem.FlushLogs();

        // When: CSV 전체 내용 읽기
        string csvPath = GetLatestCsvFilePath();
        string csvContent = File.ReadAllText(csvPath);

        // Then: 이스케이프 확인 - "Error, ""quoted"", text"
        Assert.IsTrue(csvContent.Contains("\"Error, \"\"quoted\"\", text\""),
            "특수문자가 포함된 값은 이중 따옴표로 래핑되고 내부 따옴표는 이스케이프되어야 합니다");

        // 추가 테스트: 개행 문자
        _runtime.ClearBufferForTest();
        LogSystem.PushLog(LogLevel.WARNING, "MultiLine", "Line1\nLine2");
        LogSystem.FlushLogs();

        csvPath = GetLatestCsvFilePath();
        csvContent = File.ReadAllText(csvPath);
        Assert.IsTrue(csvContent.Contains("\"Line1\nLine2\""),
            "개행 문자가 포함된 값은 따옴표로 래핑되어야 합니다");
    }

    #region Test Methods - Timestamp Format
    /// <summary>
    /// 시나리오 4-5: 타임스탬프 ISO 8601 포맷
    /// </summary>
    [Test]
    public void Test_Timestamp_ISO8601_Format()
    {
        // Given: 로그 기록
        _runtime.ClearBufferForTest();
        LogSystem.PushLog(LogLevel.DEBUG, "Test", 1);
        LogSystem.FlushLogs();

        // When: 데이터 행 읽기
        string csvPath = GetLatestCsvFilePath();
        string dataLine = File.ReadLines(csvPath).Skip(1).First();

        // Then: ISO 8601 형식 확인 "yyyy-MM-ddTHH:mm:ss.fff"
        // 필드 분리: LogType,Timestamp,Key,Value
        string[] fields = dataLine.Split(',');
        Assert.AreEqual(4, fields.Length, "4개 필드가 있어야 합니다");

        string timestamp = fields[1];
        string iso8601Pattern = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}$";
        Assert.IsTrue(Regex.IsMatch(timestamp, iso8601Pattern),
            $"타임스탬프는 ISO 8601 형식(yyyy-MM-ddTHH:mm:ss.fff)이어야 합니다. Actual: {timestamp}");
    }
    #endregion

    #region Private Methods - Helper
    /// <summary>
    /// LogRuntime 인스턴스 정리
    /// </summary>
    private void CleanupRuntime()
    {
        if (_runtime != null)
        {
            _runtime.ClearBufferForTest();
            _runtime.Cleanup();
        }

        if (_runtimeObject != null)
        {
            Object.DestroyImmediate(_runtimeObject);
            _runtimeObject = null;
            _runtime = null;
        }
    }

    /// <summary>
    /// 테스트용 CSV 파일 삭제
    /// </summary>
    private void CleanupLogFiles()
    {
        if (Directory.Exists(_logDirectoryPath))
        {
            string[] csvFiles = Directory.GetFiles(_logDirectoryPath, "*.csv");
            foreach (string file in csvFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to delete test log file: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 로그 디렉토리 경로 반환
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
    /// 가장 최근 생성된 CSV 파일 경로 반환
    /// </summary>
    private string GetLatestCsvFilePath()
    {
        if (!Directory.Exists(_logDirectoryPath))
            return null;

        string[] csvFiles = Directory.GetFiles(_logDirectoryPath, "GameLog_*.csv");
        if (csvFiles.Length == 0)
            return null;

        // 최신 파일 반환 (파일명에 타임스탬프 포함되어 있으므로 정렬)
        return csvFiles.OrderByDescending(f => f).First();
    }
    #endregion
}
#endif