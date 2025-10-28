#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;

/// <summary>
/// 버퍼 플러시 조건 테스트
/// </summary>
public class LogSystemFlushTests
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

    #region Test Methods - 80% Threshold
    /// <summary>
    /// 시나리오 3-1: 80% 임계값 플러시
    /// </summary>
    [UnityTest]
    public IEnumerator Test_Flush_80Percent_Threshold()
    {
        // Given: 빈 버퍼
        _runtime.ClearBufferForTest();

        // When: 800개 로그 추가
        for (int i = 0; i < 800; i++)
        {
            LogSystem.PushLog(LogLevel.INFO, "Test", i);
        }

        int countBefore = _runtime.CurrentLogCount;
        Assert.AreEqual(800, countBefore, "800개 로그가 추가되어야 합니다");

        // Then: 다음 Update에서 자동 플러시
        yield return null; // Wait for Update

        int countAfter = _runtime.CurrentLogCount;
        Assert.AreEqual(0, countAfter, "80% 임계값 도달 시 자동 플러시되어야 합니다");

        // CSV 파일 생성 확인
        bool csvExists = Directory.Exists(_logDirectoryPath) &&
                         Directory.GetFiles(_logDirectoryPath, "*.csv").Length > 0;
        Assert.IsTrue(csvExists, "플러시 후 CSV 파일이 생성되어야 합니다");
    }
    #endregion

    #region Test Methods - Flush Timer
    /// <summary>
    /// 시나리오 3-2: 플러시 인터벌 타이머 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator Test_Flush_Timer()
    {
        // Given: 100개 로그 (임계값 미만)
        _runtime.ClearBufferForTest();
        for (int i = 0; i < 100; i++)
        {
            LogSystem.PushLog(LogLevel.INFO, "Test", i);
        }
        Assert.AreEqual(100, _runtime.CurrentLogCount);

        // When: (인터벌 + 1초) 대기
        float waitTime = _runtime.FlushIntervalSeconds + 1f;
        yield return new WaitForSeconds(waitTime);

        // Then: 자동 플러시 발생
        Assert.AreEqual(0, _runtime.CurrentLogCount,
            $"{_runtime.FlushIntervalSeconds}초 경과 시 자동 플러시되어야 합니다");
    }
    #endregion

    #region Test Methods - Manual Flush
    /// <summary>
    /// 시나리오 3-3: 수동 플러시
    /// </summary>
    [Test]
    public void Test_Manual_Flush()
    {
        // Given: 10개 로그 (임계값 미만, 타이머 미만)
        _runtime.ClearBufferForTest();

        for (int i = 0; i < 10; i++)
        {
            LogSystem.PushLog(LogLevel.INFO, "Test", i);
        }

        Assert.AreEqual(10, _runtime.CurrentLogCount);

        // When: FlushLogs 호출
        LogSystem.FlushLogs();

        // Then: 즉시 플러시
        Assert.AreEqual(0, _runtime.CurrentLogCount, "수동 FlushLogs 호출 시 즉시 플러시되어야 합니다");
    }
    #endregion

    #region Test Methods - Pause/Quit Flush
    /// <summary>
    /// 시나리오 3-4: Pause/Quit 플러시
    /// </summary>
    [Test]
    public void Test_PauseQuit_Flush()
    {
        // Given: 50개 로그
        _runtime.ClearBufferForTest();

        for (int i = 0; i < 50; i++)
        {
            LogSystem.PushLog(LogLevel.INFO, "Test", i);
        }

        Assert.AreEqual(50, _runtime.CurrentLogCount);

        // When: OnApplicationPause(true) 시뮬레이션
        _runtime.SendMessage("OnApplicationPause", true);

        // Then: 자동 플러시
        Assert.AreEqual(0, _runtime.CurrentLogCount, "OnApplicationPause 시 자동 플러시되어야 합니다");
    }
    #endregion

    #region Test Methods - Ring Buffer
    /// <summary>
    /// 시나리오 3-5: 순환 버퍼 동작
    /// </summary>
    [Test]
    public void Test_RingBuffer_Wrap()
    {
        // Given: 플러시 비활성화를 위해 직접 AddEntry 호출
        _runtime.ClearBufferForTest();

        // When: 1000개 로그 추가 (버퍼 full)
        for (int i = 0; i < 1000; i++)
        {
            LogEntry entry = new LogEntry
            {
                Type = LogLevel.INFO,
                RealtimeSeconds = Time.realtimeSinceStartup,
                Key = "Test",
                Value = i.ToString(),
                FilePath = "",
                MemberName = "",
                LineNumber = 0
            };
            _runtime.AddEntry(entry);
        }

        // Then: _writeIndex가 0으로 순환
        Assert.AreEqual(0, _runtime.CurrentLogCount, "버퍼가 가득 차면 _writeIndex가 0으로 순환되어야 합니다");

        // 추가로 1개 더 추가
        LogEntry newEntry = new LogEntry
        {
            Type = LogLevel.DEBUG,
            RealtimeSeconds = Time.realtimeSinceStartup,
            Key = "NewTest",
            Value = "1000",
            FilePath = "",
            MemberName = "",
            LineNumber = 0
        };
        _runtime.AddEntry(newEntry);

        Assert.AreEqual(1, _runtime.CurrentLogCount, "순환 후 다시 카운트가 증가해야 합니다");

        // 첫 번째 엔트리가 덮어씌워졌는지 확인
        LogEntry firstEntry = _runtime.GetEntryAt(0);
        Assert.AreEqual("NewTest", firstEntry.Key, "순환 버퍼는 가장 오래된 데이터를 덮어써야 합니다");
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
    #endregion
}
#endif