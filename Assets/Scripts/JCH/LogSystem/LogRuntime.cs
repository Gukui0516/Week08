// LogRuntime.cs
using System;
using UnityEngine;

/// <summary>
/// 로그 런타임 버퍼 관리
/// </summary>
public class LogRuntime : MonoBehaviour
{
    #region Private Fields
    private const int BUFFER_SIZE = 1000;
    private const float FLUSH_INTERVAL_SECONDS = 5.0f;
    private const float BUFFER_THRESHOLD = 0.8f;

    private LogEntry[] _ringBuffer;
    private int _writeIndex;
    private float _lastFlushTime;
    private ILogSaver _logSaver;

    // 캐싱 필드
    private LogEntry[] _cachedSnapshot;
    private int _cachedCount;
    private bool _isSnapshotDirty = true;
    #endregion

    #region Properties
    /// <summary>현재 버퍼에 저장된 로그 개수</summary>
    public int CurrentLogCount => _writeIndex;

    /// <summary>버퍼 용량</summary>
    public int BufferCapacity => BUFFER_SIZE;

    /// <summary>플러시 인터벌 (초)</summary>
    public float FlushIntervalSeconds => FLUSH_INTERVAL_SECONDS;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (ShouldFlush())
        {
            FlushBuffer();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            FlushBuffer();
        }
    }

    private void OnApplicationQuit()
    {
        FlushBuffer();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _ringBuffer = new LogEntry[BUFFER_SIZE];
        _writeIndex = 0;
        _lastFlushTime = Time.realtimeSinceStartup;
        _logSaver = new LogSave();
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        FlushBuffer();
    }
    #endregion

    #region Public Methods - Buffer Management
    public void AddEntry(LogEntry entry)
    {
        _ringBuffer[_writeIndex] = entry;
        _writeIndex++;
        _isSnapshotDirty = true; // dirty 설정

        if (_writeIndex >= BUFFER_SIZE)
        {
            _writeIndex = 0;
        }
    }

    public void FlushBuffer()
    {
        if (_writeIndex == 0)
            return;

        _logSaver.SaveAsync(_ringBuffer, _writeIndex);
        ClearBuffer();
    }

    public LogEntry[] GetBufferSnapshot(out int count)
    {
        RefreshSnapshotIfNeeded();
        count = _cachedCount;
        LogEntry[] result = new LogEntry[count];
        Array.Copy(_cachedSnapshot, result, count);
        return result;
    }

    public LogEntry GetEntryAt(int index)
    {
        RefreshSnapshotIfNeeded();

        if (index < 0 || index >= _cachedCount)
            throw new System.ArgumentOutOfRangeException(nameof(index),
                $"Index {index} is out of range [0, {_cachedCount})");

        return _cachedSnapshot[index];
    }

    public void ClearBufferForTest()
    {
        _writeIndex = 0;
        _lastFlushTime = Time.realtimeSinceStartup;
        _isSnapshotDirty = true;
    }
    #endregion

    #region Private Methods - Snapshot Management
    /// <summary>
    /// Dirty 플래그 확인 후 스냅샷 갱신
    /// </summary>
    private void RefreshSnapshotIfNeeded()
    {
        if (!_isSnapshotDirty)
            return;

        _cachedCount = _writeIndex;

        // 배열 재사용 (크기 충분하면)
        if (_cachedSnapshot == null || _cachedSnapshot.Length < _cachedCount)
        {
            _cachedSnapshot = new LogEntry[BUFFER_SIZE];
        }

        Array.Copy(_ringBuffer, _cachedSnapshot, _cachedCount);
        _isSnapshotDirty = false;
    }
    #endregion

    #region Private Methods - Flush Logic
    /// <summary>
    /// 플러시 조건 확인
    /// </summary>
    private bool ShouldFlush()
    {
        // 버퍼 80% 도달
        int thresholdCount = (int)(BUFFER_SIZE * BUFFER_THRESHOLD);
        if (_writeIndex >= thresholdCount)
            return true;

        // 30초 경과
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - _lastFlushTime >= FLUSH_INTERVAL_SECONDS)
            return true;

        return false;
    }

    /// <summary>
    /// 버퍼 초기화
    /// </summary>
    private void ClearBuffer()
    {
        _writeIndex = 0;
        _lastFlushTime = Time.realtimeSinceStartup;
    }
    #endregion
}