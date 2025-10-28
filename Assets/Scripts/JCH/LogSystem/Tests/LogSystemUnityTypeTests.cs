
#if UNITY_EDITOR
// LogSystemUnityTypeTests.cs
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unity 타입 로깅 테스트
/// </summary>
public class LogSystemUnityTypeTests
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

    #region Test Methods - Vector3
    /// <summary>
    /// 시나리오 2-1: Vector3 변환 테스트
    /// </summary>
    [Test]
    public void Test_Vector3_Conversion()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: Vector3 로깅
        Vector3 pos = new Vector3(10.123f, 5.678f, -3.456f);
        LogSystem.PushLog(LogLevel.INFO, "Position", pos);

        // Then: F2 포맷 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("(10.12,5.68,-3.46)", entry.Value);
        Assert.AreEqual("Position", entry.Key);
        Assert.AreEqual(LogLevel.INFO, entry.Type);
    }
    #endregion

    #region Test Methods - Quaternion
    /// <summary>
    /// 시나리오 2-2: Quaternion 변환 테스트
    /// </summary>
    [Test]
    public void Test_Quaternion_Conversion()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: Quaternion 로깅
        Quaternion rot = Quaternion.Euler(45, 90, 0);
        LogSystem.PushLog(LogLevel.DEBUG, "Rotation", rot);

        // Then: 4개 성분 F2 포맷
        LogEntry entry = _runtime.GetEntryAt(0);

        Assert.IsTrue(entry.Value.StartsWith("("), "Quaternion은 괄호로 시작해야 합니다");
        Assert.IsTrue(entry.Value.Contains(","), "Quaternion은 쉼표를 포함해야 합니다");
        Assert.IsTrue(entry.Value.EndsWith(")"), "Quaternion은 괄호로 끝나야 합니다");

        // 4개 성분 존재 확인
        int commaCount = entry.Value.Split(',').Length - 1;
        Assert.AreEqual(3, commaCount, "Quaternion은 3개의 쉼표(4개 성분)를 가져야 합니다");
    }
    #endregion

    #region Test Methods - Color
    /// <summary>
    /// 시나리오 2-3: Color 변환 테스트
    /// </summary>
    [Test]
    public void Test_Color_Conversion()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: Color 로깅
        Color color = new Color(1f, 0.5f, 0.25f, 1f);
        LogSystem.PushLog(LogLevel.INFO, "TintColor", color);

        // Then: RGBA 포맷 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("RGBA(1.00,0.50,0.25,1.00)", entry.Value);
        Assert.AreEqual("TintColor", entry.Key);
    }
    #endregion

    #region Test Methods - Bounds
    /// <summary>
    /// 시나리오 2-4: Bounds 중첩 변환 테스트
    /// </summary>
    [Test]
    public void Test_Bounds_NestedConversion()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: Bounds 로깅 (Vector3 포함)
        Bounds bounds = new Bounds(Vector3.one, Vector3.one * 2);
        LogSystem.PushLog(LogLevel.DEBUG, "ColliderBounds", bounds);

        // Then: 중첩 포맷 확인
        LogEntry entry = _runtime.GetEntryAt(0);

        Assert.IsTrue(entry.Value.Contains("center:"), "Bounds는 center를 포함해야 합니다");
        Assert.IsTrue(entry.Value.Contains("size:"), "Bounds는 size를 포함해야 합니다");
        Assert.IsTrue(entry.Value.Contains("("), "Bounds 내부 Vector3는 괄호를 포함해야 합니다");
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