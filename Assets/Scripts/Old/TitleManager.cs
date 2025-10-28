using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    bool isStart = false;
    private void OnEnable()
    {
        // BombManager가 존재하는지 확인
        if (BombManager.Instance != null)
        {
            BombManager.Instance.OnBombCountChanged += RemainBombUpdate;
        }
    }

    private void OnDisable()
    {
        // BombManager가 이미 파괴되었을 수 있으므로 null 체크
        if (BombManager.Instance != null)
        {
            BombManager.Instance.OnBombCountChanged -= RemainBombUpdate;
        }
    }

    void RemainBombUpdate(int remainBomb)
    {
        if (remainBomb <= 0)
        {
            if (isStart) return;
            StartCoroutine(StartGame());
        }
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("STAGE");
    }
}
