using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Create SO/StageData", order = 1)]
public class StageDataSO : ScriptableObject
{
    #region Serialized Fields
    [SerializeField] private Sprite _stageImage;
    [SerializeField] private string _stageName;
    [SerializeField] private string _sceneName;
    [SerializeField] private int _clearStar = 0;
    [SerializeField] private bool _isTried = false;
    [SerializeField, HideInInspector] private string _stageImagePath;
    [SerializeField, ReadOnly] private string _scenePath;
    #endregion

    #region 외부 전용 반환 메소드
    public Sprite StageImage
    {
        get => _stageImage;
        set => _stageImage = value;
    }

    public string StageName
    {
        get => _stageName;
        set => _stageName = value;
    }

    public int ClearStar
    {
        get => _clearStar;
        set => _clearStar = value;
    }

    public bool IsTried
    {
        get => _isTried;
        set => _isTried = value;
    }

    public string SceneName => _sceneName;

    public string StageImagePath
    {
        get => _stageImagePath;
        set => _stageImagePath = value;
    }

    #endregion

    #region Editor 전용 외부 메소드
#if UNITY_EDITOR
    public void SetSceneInfo(string name, string path)
    {
        _sceneName = name;
        _scenePath = path;
    }
#endif

    #endregion

}