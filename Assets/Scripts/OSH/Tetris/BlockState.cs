using UnityEngine;

public class BlockState : MonoBehaviour
{
    [SerializeField]
    private bool isProcessed = false; // 임시 중복 방지용 (짧은 시간)

    [SerializeField]
    private bool hasTriggeredSpawn = false; // 영구적: 이 블록이 이미 스폰을 트리거했는지

    /// <summary>
    /// 임시 중복 방지 플래그 (debounceTime 후 리셋됨)
    /// </summary>
    public bool IsProcessed
    {
        get => isProcessed;
        set => isProcessed = value;
    }

    /// <summary>
    /// 영구적 스폰 트리거 플래그 (절대 리셋 안 됨)
    /// 이 블록이 이미 스폰을 트리거했으면 true, 다시는 스폰 안 함
    /// </summary>
    public bool HasTriggeredSpawn
    {
        get => hasTriggeredSpawn;
        set => hasTriggeredSpawn = value;
    }
}