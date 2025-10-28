using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonObject<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            var stageName = StageManager.Instance.CurrentStageData != null
                ? StageManager.Instance.CurrentStageData.StageName
                : "UnknownStage";
            LogSystem.PushLog(LogLevel.INFO, "StageRetry", stageName);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
