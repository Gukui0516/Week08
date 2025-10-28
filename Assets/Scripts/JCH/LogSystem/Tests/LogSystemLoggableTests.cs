#if UNITY_EDITOR
// LogSystemLoggableTests.cs
using NUnit.Framework;
using System.Linq;
using UnityEngine;

/// <summary>
/// ILoggable 커스텀 타입 테스트
/// </summary>
public class LogSystemLoggableTests
{
    #region Private Fields
    private GameObject _runtimeObject;
    private LogRuntime _runtime;
    #endregion

    #region Test Helper Types - ILoggable Implementations
    /// <summary>
    /// 테스트용 단순 ILoggable 구조체
    /// </summary>
    private struct SimpleLoggable : ILoggable
    {
        public int id;
        public string name;

        public string ToLogString()
        {
            return $"Item[{id}:{name}]";
        }
    }

    /// <summary>
    /// 테스트용 중첩 데이터 ILoggable 구조체
    /// </summary>
    private struct InventoryLoggable : ILoggable
    {
        public int[] itemIds;
        public int[] counts;

        public string ToLogString()
        {
            if (itemIds == null || counts == null || itemIds.Length != counts.Length)
                return "Invalid";

            // struct 내부 람다 제약 회피: 로컬 변수로 복사
            var ids = itemIds;
            var cnts = counts;

            return string.Join(",", System.Linq.Enumerable.Range(0, ids.Length)
                .Select(i => $"{ids[i]}x{cnts[i]}"));
        }
    }

    /// <summary>
    /// 테스트용 민감 정보 필터링 ILoggable 구조체
    /// </summary>
    private struct UserDataLoggable : ILoggable
    {
        public string userId;
        public string password;

        public string ToLogString()
        {
            return $"User[{userId}]"; // password 제외
        }
    }

    /// <summary>
    /// 테스트용 ILoggable + 기본 타입 구조체 (우선순위 테스트)
    /// </summary>
    private struct CustomInt : ILoggable
    {
        public int value;

        public string ToLogString()
        {
            return $"Custom:{value}";
        }
    }
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

    #region Test Methods - Simple Structure
    /// <summary>
    /// 시나리오 6-1: 단순 구조체 로깅
    /// </summary>
    [Test]
    public void Test_SimpleLoggable_Conversion()
    {
        // Given: ILoggable 구현 구조체
        _runtime.ClearBufferForTest();
        var item = new SimpleLoggable { id = 100, name = "Sword" };

        // When: 로깅
        LogSystem.PushLog(LogLevel.INFO, "Item", item);

        // Then: ToLogString 결과 저장
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("Item[100:Sword]", entry.Value,
            "ILoggable의 ToLogString() 결과가 저장되어야 합니다");
        Assert.AreEqual("Item", entry.Key);
        Assert.AreEqual(LogLevel.INFO, entry.Type);
    }
    #endregion

    #region Test Methods - Nested Data
    /// <summary>
    /// 시나리오 6-2: 중첩 데이터 구조
    /// </summary>
    [Test]
    public void Test_NestedData_Conversion()
    {
        // Given: 복잡한 중첩 구조
        _runtime.ClearBufferForTest();
        var inv = new InventoryLoggable
        {
            itemIds = new[] { 1, 2, 3 },
            counts = new[] { 5, 10, 2 }
        };

        // When: 로깅
        LogSystem.PushLog(LogLevel.DEBUG, "Inventory", inv);

        // Then: 커스텀 포맷 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("1x5,2x10,3x2", entry.Value,
            "중첩 데이터 구조가 올바르게 변환되어야 합니다");
        Assert.AreEqual("Inventory", entry.Key);
        Assert.AreEqual(LogLevel.DEBUG, entry.Type);
    }
    #endregion

    #region Test Methods - Sensitive Info Filter
    /// <summary>
    /// 시나리오 6-3: 민감 정보 필터링
    /// </summary>
    [Test]
    public void Test_SensitiveInfo_Filtering()
    {
        // Given: 민감 정보 포함
        _runtime.ClearBufferForTest();
        var user = new UserDataLoggable { userId = "user123", password = "secret" };

        // When: 로깅
        LogSystem.PushLog(LogLevel.INFO, "Login", user);

        // Then: 필터링 확인
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("User[user123]", entry.Value,
            "민감 정보가 제외되어야 합니다");
        Assert.IsFalse(entry.Value.Contains("secret"),
            "password는 로그에 포함되지 않아야 합니다");
        Assert.IsFalse(entry.Value.Contains("password"),
            "password 키워드도 포함되지 않아야 합니다");
    }
    #endregion

    #region Test Methods - Priority
    /// <summary>
    /// 시나리오 6-4: ILoggable vs 기본 타입 우선순위
    /// </summary>
    [Test]
    public void Test_ILoggable_Priority_Over_BasicType()
    {
        // Given: ILoggable 구현 + 기본 타입
        _runtime.ClearBufferForTest();
        var custom = new CustomInt { value = 42 };

        // When: 로깅
        LogSystem.PushLog(LogLevel.INFO, "Test", custom);

        // Then: ILoggable 우선 적용
        LogEntry entry = _runtime.GetEntryAt(0);
        Assert.AreEqual("Custom:42", entry.Value,
            "ILoggable이 기본 타입보다 우선 적용되어야 합니다");
        Assert.AreNotEqual("42", entry.Value,
            "기본 int ToString()이 적용되면 안 됩니다");
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