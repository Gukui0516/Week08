#if UNITY_EDITOR
// LogSystemUnityConsoleTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unity Console 출력 포맷 검증 테스트
/// </summary>
public class LogSystemUnityConsoleTests
{
    #region Private Fields
    private GameObject _runtimeObject;
    private LogRuntime _runtime;
    #endregion

    #region Setup and Teardown
    [SetUp]
    public void Setup()
    {
        _runtimeObject = new GameObject("TestLogRuntime");
        _runtime = _runtimeObject.AddComponent<LogRuntime>();
        _runtime.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupRuntime();
    }
    #endregion

    #region Test Methods - Class Color Hash
    /// <summary>
    /// 시나리오 5-1: 클래스명 색상 해시 일관성
    /// </summary>
    [Test]
    public void Test_ClassColor_Hash_Consistency()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 동일 클래스(파일)에서 여러 로그 생성
        LogSystem.PushLog(LogLevel.INFO, "Test1", 1, true);
        LogSystem.PushLog(LogLevel.DEBUG, "Test2", 2, true);
        LogSystem.PushLog(LogLevel.WARNING, "Test3", 3, true);

        // Then: 모든 로그의 FilePath가 동일함 확인
        LogEntry entry1 = _runtime.GetEntryAt(0);
        LogEntry entry2 = _runtime.GetEntryAt(1);
        LogEntry entry3 = _runtime.GetEntryAt(2);

        Assert.AreEqual(entry1.FilePath, entry2.FilePath,
            "동일 클래스에서 호출된 로그는 같은 FilePath를 가져야 합니다");
        Assert.AreEqual(entry1.FilePath, entry3.FilePath,
            "동일 클래스에서 호출된 로그는 같은 FilePath를 가져야 합니다");

        // Note: 실제 색상 일관성은 Unity Console에서 육안 확인 필요
        LogAssert.Expect(UnityEngine.LogType.Log, new System.Text.RegularExpressions.Regex(@".*\[LogSystemUnityConsoleTests\].*"));
        LogAssert.Expect(UnityEngine.LogType.Log, new System.Text.RegularExpressions.Regex(@".*\[LogSystemUnityConsoleTests\].*"));
        LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex(@".*\[LogSystemUnityConsoleTests\].*"));
    }
    #endregion

    #region Test Methods - LogType Colors
    /// <summary>
    /// 시나리오 5-2: 로그 타입별 색상 매핑
    /// </summary>
    [Test]
    public void Test_LogType_Color_Mapping()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 각 타입별 로그 출력
        LogSystem.PushLog(LogLevel.DEBUG, "DebugTest", 1, true);
        LogSystem.PushLog(LogLevel.INFO, "InfoTest", 2, true);
        LogSystem.PushLog(LogLevel.WARNING, "WarningTest", 3, true);
        LogSystem.PushLog(LogLevel.ERROR, "ErrorTest", 4, true);
        LogSystem.PushLog(LogLevel.CRITICAL, "CriticalTest", 5, true);

        // Then: Unity Console에 출력되었는지 확인
        // DEBUG, INFO -> Debug.Log
        LogAssert.Expect(UnityEngine.LogType.Log, new System.Text.RegularExpressions.Regex(@".*\[DEBUG\].*DebugTest.*"));
        LogAssert.Expect(UnityEngine.LogType.Log, new System.Text.RegularExpressions.Regex(@".*\[INFO\].*InfoTest.*"));

        // WARNING -> Debug.LogWarning
        LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex(@".*\[WARNING\].*WarningTest.*"));

        // ERROR, CRITICAL -> Debug.LogError
        LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(@".*\[ERROR\].*ErrorTest.*"));
        LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(@".*\[CRITICAL\].*CriticalTest.*"));

        // Note: 실제 색상(#888888, #FFFFFF, #FFFF00, #FF0000)은 Console에서 육안 확인
    }
    #endregion

    #region Test Methods - Message Format
    /// <summary>
    /// 시나리오 5-3: 메시지 포맷 구조
    /// </summary>
    [Test]
    public void Test_Message_Format_Structure()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 로그 생성 (useUnityDebug=false)
        LogSystem.PushLog(LogLevel.INFO, "Health", 85, false);

        // Then: 엔트리 데이터 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("Health", entry.Key);
        Assert.AreEqual("85", entry.Value);
        Assert.AreEqual(LogLevel.INFO, entry.Type);

        // 포맷된 메시지 검증
        string formatted = LogConverter.GetFormattedMessage(entry);

        // 클래스 태그 확인
        Assert.IsTrue(formatted.Contains("<color="), "클래스 태그에 color가 있어야 합니다");
        Assert.IsTrue(formatted.Contains("[LogSystemUnityConsoleTests]"), "클래스명이 포함되어야 합니다");
        Assert.IsTrue(formatted.Contains("</color>"), "color 태그가 닫혀야 합니다");

        // 로그 타입 및 내용 확인
        Assert.IsTrue(formatted.Contains("[INFO]"), "로그 타입이 포함되어야 합니다");
        Assert.IsTrue(formatted.Contains("Health=85"), "Key=Value 형식이어야 합니다");

        // 호출 위치 확인
        Assert.IsTrue(formatted.Contains("at"), "호출 위치 정보가 있어야 합니다");
        Assert.IsTrue(formatted.Contains("Test_Message_Format_Structure"), "메서드명이 포함되어야 합니다");
    }
    #endregion

    #region Test Methods - Critical Prefix
    /// <summary>
    /// 시나리오 5-4: CRITICAL 특수 표시
    /// </summary>
    [Test]
    public void Test_Critical_Warning_Prefix()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: CRITICAL 로그 출력
        LogSystem.PushLog(LogLevel.CRITICAL, "FatalError", "msg", true);

        // Then: ⚠ 접두사 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual(LogLevel.CRITICAL, entry.Type);

        // 콘솔 출력에 ⚠ 포함 확인
        LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(@".*⚠.*\[CRITICAL\].*FatalError.*"));
    }
    #endregion

    #region Test Methods - Caller Location
    /// <summary>
    /// 시나리오 5-5: 호출 위치 표시
    /// </summary>
    [Test]
    public void Test_Caller_Location_Display()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 특정 라인에서 로그 출력
        LogSystem.PushLog(LogLevel.INFO, "Test", 1, true); // 이 라인 번호 기억

        // Then: 호출 정보 확인
        LogEntry entry = _runtime.GetEntryAt(0);

        Assert.IsTrue(entry.FilePath.Contains("LogSystemUnityConsoleTests.cs"),
            "FilePath에 테스트 파일명이 포함되어야 합니다");
        Assert.AreEqual("Test_Caller_Location_Display", entry.MemberName,
            "MemberName은 호출한 메서드명이어야 합니다");
        Assert.Greater(entry.LineNumber, 0, "LineNumber는 0보다 커야 합니다");

        // 콘솔 출력 형식 검증: "at FilePath:MethodName(LineNumber)"
        string expectedPattern = $@".*at.*LogSystemUnityConsoleTests\.cs:Test_Caller_Location_Display\({entry.LineNumber}\).*";
        LogAssert.Expect(UnityEngine.LogType.Log, new System.Text.RegularExpressions.Regex(expectedPattern));
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
    #endregion
}
#endif