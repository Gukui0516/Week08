
#if UNITY_EDITOR
// LogSystemBasicTypeTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// 기본 타입 로깅 테스트
/// </summary>
public class LogSystemBasicTypeTests
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

    #region Test Methods - Type Conversion
    /// <summary>
    /// 시나리오 1-1: float/int/string 타입 변환 검증
    /// </summary>
    [Test]
    public void Test_BasicType_Conversion()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 각 타입별 PushLog 호출
        LogSystem.PushLog(LogLevel.INFO, "Health", 85.567f);
        LogSystem.PushLog(LogLevel.DEBUG, "Score", 1000);
        LogSystem.PushLog(LogLevel.WARNING, "Message", "Test");

        // Then: Value 포맷 확인
        LogEntry entry0 = _runtime.GetEntryAt(0);
        LogEntry entry1 = _runtime.GetEntryAt(1);
        LogEntry entry2 = _runtime.GetEntryAt(2);

        Assert.AreEqual("85.567", entry0.Value, "float는 F3 포맷이어야 합니다");
        Assert.AreEqual("1000", entry1.Value, "int는 ToString이어야 합니다");
        Assert.AreEqual("Test", entry2.Value, "string은 원본 유지되어야 합니다");

        Assert.AreEqual(LogLevel.INFO, entry0.Type);
        Assert.AreEqual(LogLevel.DEBUG, entry1.Type);
        Assert.AreEqual(LogLevel.WARNING, entry2.Type);
    }
    #endregion

    #region Test Methods - Caller Info
    /// <summary>
    /// 시나리오 1-2: CallerAttributes 정보 추적 확인
    /// </summary>
    [Test]
    public void Test_CallerInfo_Tracking()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 특정 라인에서 호출
        LogSystem.PushLog(LogLevel.INFO, "Test", 123); // 이 라인 번호 기억

        // Then: CallerAttributes 캡처 확인
        LogEntry entry = _runtime.GetEntryAt(0);

        Assert.IsTrue(entry.FilePath.Contains("LogSystemBasicTypeTests.cs"),
            $"FilePath에 테스트 파일명이 포함되어야 합니다. Actual: {entry.FilePath}");
        Assert.AreEqual("Test_CallerInfo_Tracking", entry.MemberName,
            "MemberName은 호출한 메서드명이어야 합니다");
        Assert.Greater(entry.LineNumber, 0, "LineNumber는 0보다 커야 합니다");
    }
    #endregion

    #region Test Methods - Null Safety
    /// <summary>
    /// 시나리오 1-3: null 값 안전성 테스트
    /// </summary>
    [Test]
    public void Test_Null_Safety()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: null 값 로깅
        LogSystem.PushLog<string>(LogLevel.ERROR, "NullTest", null);

        // Then: 예외 없이 "null" 문자열 저장
        LogEntry entry = _runtime.GetEntryAt(0);

        Assert.AreEqual("null", entry.Value, "null 값은 'null' 문자열로 변환되어야 합니다");
        Assert.AreEqual(LogLevel.ERROR, entry.Type);
        Assert.AreEqual("NullTest", entry.Key);
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