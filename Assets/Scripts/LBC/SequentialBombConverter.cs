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

    // 게임 종료 체크 관련
    [Header("Game End Settings")]
    [Tooltip("모든 블록이 사라졌는지 체크하는 간격(초)입니다.")]
    [SerializeField] private float gameEndCheckInterval = 0.5f;

    private float gameEndCheckTimer = 0f;
    private bool isGameEnded = false;

    private void Start()
    {
        // 빌드 환경에서 초기화 지연 (다른 매니저들이 먼저 초기화되도록)
        StartCoroutine(DelayedInitialize());
    }

    /// <summary>
    /// 빌드 환경을 위한 지연 초기화
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialize()
    {
        // 한 프레임 대기 (모든 매니저가 Awake/Start 완료되도록)
        yield return null;

        Initialize();
    }

    private void Update()
    {
        // 게임 종료 체크
        if (!isGameEnded && isInitialized)
        {
            gameEndCheckTimer += Time.deltaTime;
            if (gameEndCheckTimer >= gameEndCheckInterval)
            {
                gameEndCheckTimer = 0f;
                CheckGameEnd();
            }
        }
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
        if (BombManager.Instance != null)
        {
            previousActiveBombs = BombManager.Instance.GetActiveBombCount();
        }

        isInitialized = true;

        if (enableDebugLog)
        {
            LogSystem.PushLog(LogLevel.INFO, "SequentialBombInit",
                $"초기화 완료 | 총 블록 개수: {numberBlocks.Count}개", useUnityDebug: true);
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
        if (!isInitialized)
        {
            return;
        }

        // 폭탄이 감소했는지 확인 (폭발이 발생했는지)
        if (currentActiveBombs < previousActiveBombs)
        {
            // 다음 블록이 있는지 확인
            if (currentBlockIndex >= numberBlocks.Count)
            {
                // 모든 블록이 변환됨
                previousActiveBombs = currentActiveBombs;
                return;
            }

            // 다음 블록을 폭탄으로 변환
            ConvertNextBlockToBomb();

            // previousActiveBombs 업데이트를 다음 프레임으로 지연
            StartCoroutine(UpdatePreviousBombCountNextFrame());
            return;
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

        // 빌드 환경에서 BombManager가 null이 될 수 있으므로 체크
        if (BombManager.Instance != null)
        {
            previousActiveBombs = BombManager.Instance.GetActiveBombCount();
        }
    }

    /// <summary>
    /// 다음 블록을 폭탄으로 변환합니다.
    /// Tag 변경, Material 교체, BombManager 알림을 처리합니다.
    /// </summary>
    private void ConvertNextBlockToBomb()
    {
        // 활성화된 블록을 찾을 때까지 반복
        while (currentBlockIndex < numberBlocks.Count)
        {
            GameObject targetBlock = numberBlocks[currentBlockIndex];

            // null 체크
            if (targetBlock == null)
            {
                currentBlockIndex++;
                continue; // 다음 블록 확인
            }

            // 비활성화 체크
            if (!targetBlock.activeInHierarchy)
            {
                currentBlockIndex++;
                continue; // 다음 블록 확인
            }

            // 활성화된 블록을 찾음!
            // 1. 루트 오브젝트의 Tag만 변경
            targetBlock.tag = bombTag;

            // 2. 자식 오브젝트들의 Material만 교체 (루트는 제외)
            ChangeChildrenMaterialRecursively(targetBlock.transform);

            // 3. BombController가 있다면 활성화 (선택사항)
            BombController bombController = targetBlock.GetComponent<BombController>();
            if (bombController != null)
            {
                bombController.enabled = true;
            }

            // 4. BombManager에 폭탄 생성 알림
            if (BombManager.Instance != null)
            {
                BombManager.Instance.NotifyBombSpawned(targetBlock);
                BombManager.Instance.RegisterGoalBomb(targetBlock);
            }

            // 로그 기록 (중요한 이벤트만)
            if (enableDebugLog)
            {
                LogSystem.PushLog(LogLevel.INFO, "BlockToBomb",
                    $"{targetBlock.name} (Index: {currentBlockIndex})", useUnityDebug: true);
            }

            // 6. 다음 블록 인덱스로 이동
            currentBlockIndex++;

            return; // 폭탄 변환 성공, 메서드 종료
        }

        // 모든 블록을 확인했지만 활성화된 블록이 없음
        if (enableDebugLog)
        {
            LogSystem.PushLog(LogLevel.INFO, "AllBlocksConverted",
                "모든 블록 변환 완료", useUnityDebug: true);
        }
    }

    /// <summary>
    /// 지정된 Transform의 자식 오브젝트들만 Renderer Material을 변경합니다.
    /// 루트 오브젝트 자체는 제외합니다.
    /// </summary>
    /// <param name="root">Material을 변경할 최상위 Transform</param>
    /// <returns>처리된 Renderer 개수</returns>
    private int ChangeChildrenMaterialRecursively(Transform root)
    {
        int changedCount = 0;

        // 자식 오브젝트들만 재귀 처리 (루트는 제외)
        foreach (Transform child in root)
        {
            changedCount += ChangeMaterialRecursively(child);
        }

        return changedCount;
    }

    /// <summary>
    /// 지정된 Transform과 모든 자식 오브젝트의 Renderer Material을 변경합니다.
    /// </summary>
    /// <param name="target">Material을 변경할 Transform</param>
    /// <returns>처리된 Renderer 개수</returns>
    private int ChangeMaterialRecursively(Transform target)
    {
        int changedCount = 0;

        // 현재 오브젝트의 Renderer 처리
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Material 배열 생성 (모든 Material을 bombMaterial로 교체)
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = bombMaterial;
            }
            renderer.materials = materials;
            changedCount++;
        }

        // 모든 자식 오브젝트 재귀 처리
        foreach (Transform child in target)
        {
            changedCount += ChangeMaterialRecursively(child);
        }

        return changedCount;
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

    /// <summary>
    /// 모든 블록이 사라졌는지(비활성화 또는 파괴) 체크하여 게임 종료 여부를 판단합니다.
    /// </summary>
    private void CheckGameEnd()
    {
        int activeBlockCount = 0;

        // 모든 블록의 활성화 상태 확인
        foreach (GameObject block in numberBlocks)
        {
            if (block != null && block.activeInHierarchy)
            {
                activeBlockCount++;
            }
        }

        // 모든 블록이 비활성화되거나 파괴됨
        if (activeBlockCount == 0)
        {
            isGameEnded = true;
            OnGameEnd();
        }
    }

    /// <summary>
    /// 게임이 종료되었을 때 호출되는 메서드입니다.
    /// ClearManager의 클리어 로직을 실행합니다.
    /// </summary>
    private void OnGameEnd()
    {
        LogSystem.PushLog(LogLevel.INFO, "GameEnd", "SequentialBombGame", useUnityDebug: true);

        // ClearManager의 클리어 로직 실행
        ClearManager clearManager = FindFirstObjectByType<ClearManager>();
        if (clearManager != null)
        {
            // ClearManager의 private ClearChecker 메서드를 리플렉션으로 호출
            var method = clearManager.GetType().GetMethod("ClearChecker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(clearManager, null);
            }
        }
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
                // Tag가 없음
            }
        }
    }
#endif
}