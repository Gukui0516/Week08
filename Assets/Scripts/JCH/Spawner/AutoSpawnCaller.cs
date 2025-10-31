using UnityEngine;
using System.Collections;

/// <summary>
/// 게임 시작 시 RandomSpawner에게 자동 스폰 명령
/// </summary>
public class AutoSpawnCaller : MonoBehaviour
{
    #region Serialized Fields
    [Header("Spawner Reference")]
    [SerializeField] private RandomSpawner _randomSpawner;

    [Header("Spawn Settings")]
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.Sequential;
    [SerializeField] private int _spawnCount = 10;
    [SerializeField] private float _sequentialDurationSeconds = 2.0f;

    [Header("Timing Settings")]
    [SerializeField] private float _startDelaySeconds = 0f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_randomSpawner == null)
        {
            Debug.LogError("[AutoSpawner] RandomSpawner is not assigned!");
            return;
        }

        if (_startDelaySeconds > 0f)
        {
            StartCoroutine(ExecuteSpawnWithDelay());
        }
        else
        {
            ExecuteSpawn();
        }
    }
    #endregion

    #region Private Methods - Spawn Execution
    /// <summary>
    /// 지연 후 스폰 실행
    /// </summary>
    private IEnumerator ExecuteSpawnWithDelay()
    {
        yield return new WaitForSeconds(_startDelaySeconds);
        ExecuteSpawn();
    }

    /// <summary>
    /// 스폰 모드에 따라 명령 실행
    /// </summary>
    private void ExecuteSpawn()
    {
        switch (_spawnMode)
        {
            case SpawnMode.Immediate:
                _randomSpawner.SpawnImmediate(_spawnCount);
                break;

            case SpawnMode.Sequential:
                _randomSpawner.SpawnSequential(_spawnCount, _sequentialDurationSeconds);
                break;
        }
    }
    #endregion
}

/// <summary>
/// 스폰 모드
/// </summary>
public enum SpawnMode
{
    Immediate,  // 즉시 스폰
    Sequential  // 순차 스폰
}