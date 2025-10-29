// WorldStageObject.cs
using UnityEngine;
using UnityEngine.SceneManagement;

// 3D 오브젝트(Collider 포함)에 이 스크립트를 붙여야 합니다.
[RequireComponent(typeof(Collider))]
public class WorldStageObject : MonoBehaviour
{
    [Header("1. 연결할 데이터")]
    [SerializeField]
    private StageDataSO _stageData; // 인스펙터에서 할당할 스테이지 데이터

    [Header("2. 연결할 UI")]
    [SerializeField]
    private Stage _stageUI; // 자식 World Space Canvas에 있는 Stage.cs 컴포넌트

    void Start()
    {
        // 3D 오브젝트에 연결된 UI를 스테이지 데이터로 초기화합니다.
        if (_stageUI != null && _stageData != null)
        {
            _stageUI.Init(_stageData);
        }
        else
        {
            Debug.LogWarning($"[WorldStageObject] {gameObject.name}에 StageData 또는 StageUI가 연결되지 않았습니다.");
        }
    }

    /// <summary>
    /// StageUIManager가 Raycast로 이 오브젝트를 맞췄을 때 호출할 함수
    /// </summary>
    public void SelectStage()
    {
        if (_stageData == null)
        {
            Debug.LogError($"[WorldStageObject] {gameObject.name}에 StageData가 없습니다.");
            return;
        }

        // 현재 씬 이름을 StageManager에 저장 (클리어 후 돌아올 씬)
        string currentSceneName = SceneManager.GetActiveScene().name;
        StageManager.Instance.SetPreviousScene(currentSceneName);

        // StageManager에 현재 스테이지 정보 등록
        StageManager.Instance.SetStageData(_stageData, null);

        // 씬 로드
        SceneManager.LoadScene(_stageData.SceneName);
    }
}