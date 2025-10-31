using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 숫자 블록(0~9)을 순차적으로 폭탄으로 변환하는 스크립트입니다.
/// 이전 폭탄이 터지면 다음 블록이 자동으로 폭탄으로 변환됩니다.
/// StageConfig를 Manual 모드로 설정하여 동적 폭탄 생성을 지원합니다.
/// </summary>
public class SequentialBombConverter : MonoBehaviour
{
    [Header("Number Blocks Settings")]
    [Tooltip("0부터 9까지 순서대로 변환할 블록들입니다. Inspector에서 순서대로 할당하세요.")]
    [SerializeField] private List<GameObject> numberBlocks = new List<GameObject>();

    [Header("Bomb Settings")]
    [Tooltip("폭탄으로 변환할 때 적용할 Tag입니다.")]
    [SerializeField] private string bombTag = "Bomb";

    [Tooltip("폭탄으로 변환할 때 적용할 Material입니다.")]
    [SerializeField] private Material bombMaterial;

    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool enableDebugLog = true;

    // 현재 변환할 블록의 인덱스
    private int currentBlockIndex = 0;

    // 초기화 완료 여부
    private bool isInitialized = false;

    // 이전 프레임의 활성 폭탄 개수 (감소 감지용)
    private int previousActiveBombs = 0;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // BombManager의 폭탄 개수 변경 이벤트 구독
        if (BombManager.Instance != null)
        {
            BombManager.Instance.OnBombCountChanged += OnBombCountChanged;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (BombManager.Instance != null)
        {
            BombManager.Instance.OnBombCountChanged -= OnBombCountChanged;
        }
    }

    /// <summary>
    /// 초기화 메서드입니다.
    /// StageConfig를 Manual 모드로 설정하고 첫 번째 블록을 폭탄으로 변환합니다.
    /// </summary>
    private void Initialize()
    {
        // 유효성 검사
        if (!ValidateSettings())
        {
            return;
        }

        // StageConfig를 Manual 모드로 설정
        SetupStageConfig();

        // 첫 번째 블록을 폭탄으로 변환
        ConvertNextBlockToBomb();

        // 초기 활성 폭탄 개수 저장
        previousActiveBombs = BombManager.Instance.GetActiveBombCount();

        isInitialized = true;

        if (enableDebugLog)
        {
            LogSystem.PushLog(LogLevel.INFO, "SequentialBombInit",
                $"초기화 완료 | 총 블록 개수: {numberBlocks.Count}개");
        }
    }

    /// <summary>
    /// 설정값의 유효성을 검사합니다.
    /// </summary>
    /// <returns>유효하면 true, 그렇지 않으면 false</returns>
    private bool ValidateSettings()
    {
        // 블록 리스트 검사
        if (numberBlocks == null || numberBlocks.Count == 0)
        {
            LogSystem.PushLog(LogLevel.ERROR, "ValidationError",
                "[SequentialBombConverter] numberBlocks가 비어있습니다. Inspector에서 블록들을 할당하세요.", useUnityDebug: true);
            enabled = false;
            return false;
        }

        // null 블록 검사
        for (int i = 0; i < numberBlocks.Count; i++)
        {
            if (numberBlocks[i] == null)
            {
                LogSystem.PushLog(LogLevel.ERROR, "ValidationError",
                    $"[SequentialBombConverter] numberBlocks[{i}]이(가) null입니다.", useUnityDebug: true);
                enabled = false;
                return false;
            }
        }

        // Material 검사
        if (bombMaterial == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "ValidationError",
                "[SequentialBombConverter] bombMaterial이 할당되지 않았습니다.", useUnityDebug: true);
            enabled = false;
            return false;
        }

        // BombManager 검사
        if (BombManager.Instance == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "ValidationError",
                "[SequentialBombConverter] BombManager를 찾을 수 없습니다.", useUnityDebug: true);
            enabled = false;
            return false;
        }

        // StageConfig 검사
        if (StageConfig.Instance == null)
        {
            LogSystem.PushLog(LogLevel.ERROR, "ValidationError",
                "[SequentialBombConverter] StageConfig를 찾을 수 없습니다.", useUnityDebug: true);
            enabled = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// StageConfig를 Manual 모드로 설정합니다.
    /// 목표 폭탄 개수는 numberBlocks의 개수로 자동 설정됩니다.
    /// </summary>
    private void SetupStageConfig()
    {
        if (StageConfig.Instance == null)
        {
            return;
        }

        // StageConfig의 목표 개수를 numberBlocks 개수로 설정
        // (StageConfig.cs의 manualGoalBombCount는 SerializeField이므로 
        // 런타임에서 직접 변경할 수 없습니다. Inspector에서 수동으로 설정해야 합니다.)

        if (enableDebugLog)
        {
            LogSystem.PushLog(LogLevel.INFO, "StageConfigSetup",
                $"StageConfig를 Manual 모드로 설정하세요. 목표 폭탄 개수: {numberBlocks.Count}개");
        }
    }

    /// <summary>
    /// 폭탄 개수가 변경될 때 호출되는 이벤트 핸들러입니다.
    /// 폭탄이 터져서 개수가 감소했을 때만 다음 블록을 폭탄으로 변환합니다.
    /// </summary>
    /// <param name="currentActiveBombs">현재 활성화된 폭탄 개수</param>
    private void OnBombCountChanged(int currentActiveBombs)
    {
        if (enableDebugLog)
        {
            Debug.Log($"<color=cyan>[SequentialBomb]</color> OnBombCountChanged 호출 | " +
                      $"초기화: {isInitialized} | 이전: {previousActiveBombs} → 현재: {currentActiveBombs} | " +
                      $"현재 인덱스: {currentBlockIndex}/{numberBlocks.Count}");
        }

        if (!isInitialized)
        {
            if (enableDebugLog)
            {
                Debug.Log("<color=yellow>[SequentialBomb]</color> 아직 초기화 안 됨 - 무시");
            }
            return;
        }

        // 폭탄이 감소했는지 확인 (폭발이 발생했는지)
        if (currentActiveBombs < previousActiveBombs)
        {
            if (enableDebugLog)
            {
                Debug.Log($"<color=green>[SequentialBomb]</color> 폭탄 폭발 감지! " +
                          $"이전: {previousActiveBombs} → 현재: {currentActiveBombs}");
            }

            // 다음 블록이 있는지 확인
            if (currentBlockIndex >= numberBlocks.Count)
            {
                // 모든 블록이 변환됨
                if (enableDebugLog)
                {
                    Debug.Log("<color=magenta>[SequentialBomb]</color> 모든 블록이 변환 완료!");
                }
                previousActiveBombs = currentActiveBombs;
                return;
            }

            // 다음 블록을 폭탄으로 변환
            Debug.Log($"<color=orange>[SequentialBomb]</color> 다음 블록 변환 시도 - Index: {currentBlockIndex}");
            ConvertNextBlockToBomb();

            // previousActiveBombs 업데이트를 다음 프레임으로 지연
            StartCoroutine(UpdatePreviousBombCountNextFrame());
            return;
        }
        else if (currentActiveBombs > previousActiveBombs)
        {
            if (enableDebugLog)
            {
                Debug.Log($"<color=yellow>[SequentialBomb]</color> 폭탄 증가 (생성) 감지 - 무시 | " +
                          $"이전: {previousActiveBombs} → 현재: {currentActiveBombs}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"<color=gray>[SequentialBomb]</color> 폭탄 개수 변화 없음 - 무시");
            }
        }

        // 현재 개수를 저장 (중요!)
        previousActiveBombs = currentActiveBombs;
    }

    /// <summary>
    /// 다음 프레임에 previousActiveBombs를 업데이트합니다.
    /// 폭발 이벤트가 완전히 끝난 후 카운트를 동기화하기 위함입니다.
    /// </summary>
    private System.Collections.IEnumerator UpdatePreviousBombCountNextFrame()
    {
        yield return null; // 다음 프레임까지 대기
        previousActiveBombs = BombManager.Instance.GetActiveBombCount();

        if (enableDebugLog)
        {
            Debug.Log($"<color=blue>[SequentialBomb]</color> previousActiveBombs 업데이트 → {previousActiveBombs}");
        }
    }

    /// <summary>
    /// 다음 블록을 폭탄으로 변환합니다.
    /// Tag 변경, Material 교체, BombManager 알림을 처리합니다.
    /// </summary>
    private void ConvertNextBlockToBomb()
    {
        Debug.Log($"<color=orange>[SequentialBomb]</color> === ConvertNextBlockToBomb 시작 === Index: {currentBlockIndex}");

        // 인덱스 범위 체크
        if (currentBlockIndex >= numberBlocks.Count)
        {
            Debug.Log($"<color=red>[SequentialBomb]</color> 인덱스 범위 초과! {currentBlockIndex} >= {numberBlocks.Count}");
            return;
        }

        GameObject targetBlock = numberBlocks[currentBlockIndex];

        if (targetBlock == null)
        {
            Debug.LogError($"<color=red>[SequentialBomb]</color> Block[{currentBlockIndex}]이(가) null입니다!");
            currentBlockIndex++;
            return;
        }

        Debug.Log($"<color=cyan>[SequentialBomb]</color> 대상 블록: '{targetBlock.name}' (Index: {currentBlockIndex})");

        // 1. Tag 변경
        string oldTag = targetBlock.tag;
        targetBlock.tag = bombTag;
        Debug.Log($"<color=green>[SequentialBomb]</color> 1. Tag 변경: '{oldTag}' → '{bombTag}'");

        // 2. Material 교체 (본인 + 모든 자식 오브젝트)
        ChangeMaterialRecursively(targetBlock.transform);
        Debug.Log($"<color=green>[SequentialBomb]</color> 2. Material 교체 완료");

        // 3. BombController가 있다면 활성화 (선택사항)
        BombController bombController = targetBlock.GetComponent<BombController>();
        if (bombController != null)
        {
            bombController.enabled = true;
            Debug.Log($"<color=green>[SequentialBomb]</color> 3. BombController 활성화");
        }
        else
        {
            Debug.Log($"<color=yellow>[SequentialBomb]</color> 3. BombController 없음 (스킵)");
        }

        // 4. BombManager에 폭탄 생성 알림
        int beforeCount = BombManager.Instance.GetActiveBombCount();
        BombManager.Instance.NotifyBombSpawned(targetBlock);
        int afterCount = BombManager.Instance.GetActiveBombCount();
        Debug.Log($"<color=green>[SequentialBomb]</color> 4. BombManager 알림 완료 | 폭탄 개수: {beforeCount} → {afterCount}");

        // 5. BombManager에 목표 폭탄으로 등록 (RegisterOnly 모드 대비)
        BombManager.Instance.RegisterGoalBomb(targetBlock);
        Debug.Log($"<color=green>[SequentialBomb]</color> 5. 목표 폭탄 등록 완료");

        // 6. 다음 블록 인덱스로 이동
        currentBlockIndex++;
        Debug.Log($"<color=magenta>[SequentialBomb]</color> === ConvertNextBlockToBomb 완료 === 다음 Index: {currentBlockIndex}");
    }

    /// <summary>
    /// 지정된 Transform과 모든 자식 오브젝트의 Renderer Material을 변경합니다.
    /// </summary>
    /// <param name="root">Material을 변경할 최상위 Transform</param>
    private void ChangeMaterialRecursively(Transform root)
    {
        // 현재 오브젝트의 Renderer 처리
        Renderer renderer = root.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Material 배열 생성 (모든 Material을 bombMaterial로 교체)
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = bombMaterial;
            }
            renderer.materials = materials;
        }

        // 모든 자식 오브젝트 재귀 처리
        foreach (Transform child in root)
        {
            ChangeMaterialRecursively(child);
        }
    }

    /// <summary>
    /// 특정 인덱스의 블록을 강제로 폭탄으로 변환합니다. (테스트용)
    /// </summary>
    /// <param name="index">변환할 블록의 인덱스</param>
    public void ConvertBlockAtIndex(int index)
    {
        if (index < 0 || index >= numberBlocks.Count)
        {
            LogSystem.PushLog(LogLevel.WARNING, "ConversionWarning",
                $"[SequentialBombConverter] 인덱스 {index}은(는) 범위를 벗어났습니다.", useUnityDebug: true);
            return;
        }

        currentBlockIndex = index;
        ConvertNextBlockToBomb();
    }

    /// <summary>
    /// 현재 변환 상태를 초기화합니다.
    /// </summary>
    public void ResetConverter()
    {
        currentBlockIndex = 0;
        isInitialized = false;
        previousActiveBombs = 0;

        // 모든 블록을 원래 상태로 복원 (선택사항)
        // 필요시 구현

        if (enableDebugLog)
        {
            LogSystem.PushLog(LogLevel.INFO, "SequentialBombReset",
                "SequentialBombConverter가 초기화되었습니다.");
        }
    }

    /// <summary>
    /// 현재 변환 진행 상황을 반환합니다.
    /// </summary>
    /// <returns>변환된 블록 개수 / 전체 블록 개수</returns>
    public string GetConversionProgress()
    {
        return $"{currentBlockIndex} / {numberBlocks.Count}";
    }

#if UNITY_EDITOR
    [ContextMenu("다음 블록을 폭탄으로 변환 (테스트)")]
    private void TestConvertNextBlock()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SequentialBombConverter] Play Mode에서만 테스트할 수 있습니다.");
            return;
        }

        ConvertNextBlockToBomb();
    }

    [ContextMenu("변환 상태 초기화")]
    private void TestResetConverter()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SequentialBombConverter] Play Mode에서만 테스트할 수 있습니다.");
            return;
        }

        ResetConverter();
    }

    [ContextMenu("진행 상황 확인")]
    private void TestGetProgress()
    {
        Debug.Log($"[SequentialBombConverter] 진행 상황: {GetConversionProgress()}");
    }

    private void OnValidate()
    {
        // Tag 유효성 검사
        if (!string.IsNullOrEmpty(bombTag))
        {
            try
            {
                GameObject.FindGameObjectWithTag(bombTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[SequentialBombConverter] '{bombTag}' 태그가 Tag Manager에 등록되어 있지 않습니다.");
            }
        }

        // Material 검사
        if (bombMaterial == null)
        {
            Debug.LogWarning("[SequentialBombConverter] bombMaterial이 할당되지 않았습니다.");
        }

        // 블록 리스트 검사
        if (numberBlocks != null && numberBlocks.Count > 0)
        {
            for (int i = 0; i < numberBlocks.Count; i++)
            {
                if (numberBlocks[i] == null)
                {
                    Debug.LogWarning($"[SequentialBombConverter] numberBlocks[{i}]이(가) null입니다.");
                }
            }
        }
    }
#endif
}