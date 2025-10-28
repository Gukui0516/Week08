using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 전용 초기화 
public class Stage : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private Image _stageImg;
    [SerializeField] private GameObject _clearStars;
    [SerializeField] private Sprite _filledStar;
    [SerializeField] private Sprite _emptyStar;
    [SerializeField] private int _maxStars = 3;
    [SerializeField] private TextMeshProUGUI _stageNameText;
    #endregion

    private StageDataSO _stageDataSO;

    public void Init(StageDataSO stageSO)
    {
        // 원본 SO를 직접 수정하지 않도록, 런타임용 복제본 생성
#if UNITY_EDITOR
        _stageDataSO = UnityEditor.EditorApplication.isPlaying
            ? Instantiate(stageSO)
            : stageSO;
#else
        _stageDataSO = Instantiate(stageSO);
#endif

        _stageNameText.text = _stageDataSO.StageName;

        // StageImagePath가 유효하면 이미지 로드 후 반영
        if (!string.IsNullOrEmpty(_stageDataSO.StageImagePath) && File.Exists(_stageDataSO.StageImagePath))
        {
            byte[] bytes = File.ReadAllBytes(_stageDataSO.StageImagePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            _stageDataSO.StageImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

            // UI에 즉시 반영
            _stageImg.sprite = _stageDataSO.StageImage;

            Debug.Log($"[Init] StageImage 로드 완료: {_stageDataSO.SceneName} ({_stageDataSO.StageImagePath})");
        }
        else
        {
            _stageImg.sprite = _stageDataSO.StageImage;
        }

        DrawClearStar(_stageDataSO.ClearStar);
    }

    private void DrawClearStar(int earnedStar)
    {
        if (!_stageDataSO.IsTried)
        {
            _clearStars.SetActive(false);
            return;
        }

        _clearStars.SetActive(true);

        // 별 자식 Image들을 순회하며 sprite 변경
        int clearCount = Mathf.Clamp(_stageDataSO.ClearStar, 0, _maxStars);

        for (int i = 0; i < _maxStars; i++)
        {
            Image starImg = _clearStars.transform.GetChild(i).GetComponent<Image>();
            if (i < clearCount)
                starImg.sprite = _filledStar;
            else
                starImg.sprite = _emptyStar;
        }
    }

}