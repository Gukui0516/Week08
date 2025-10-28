using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    #region Public Fields
    public static StageManager Instance { get; private set; }
    public StageDataSO CurrentStageData { get; private set; }
    public string PreviousSceneName { get; private set; } = "STAGE"; // 기본값은 STAGE
    #endregion

    #region Public Methods
    /// <summary>
    /// 스테이지 선택 화면의 씬 이름을 저장합니다.
    /// 클리어나 뒤로가기 시 이 씬으로 돌아갑니다.
    /// </summary>
    public void SetPreviousScene(string sceneName)
    {
        PreviousSceneName = sceneName;
        Debug.Log($"[StageManager] 이전 씬 저장: {PreviousSceneName}");
    }

    public void SetStageData(StageDataSO data, List<StageDataSO> datum)
    {
        LogSystem.PushLog(LogLevel.INFO, "StageBegin", data.StageName);
        CurrentStageData = data;
        data.IsTried = true;

        // 전체 Save가 아니라, isTried만 갱신
        StageSaveManager.SaveSingleStage(data);
    }

    // 스테이지 이미지 및 별 업데이트
    public void UpdateClearData(int starCount, string snapShotPath)
    {
        StageSaveManager.UpdateStageData(CurrentStageData, starCount, snapShotPath);

        LogSystem.PushLog(LogLevel.INFO, "StageStar", starCount);
        LogSystem.PushLog(LogLevel.INFO, "StageClear", CurrentStageData.StageName);
    }
    #endregion

    #region Private Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion
}