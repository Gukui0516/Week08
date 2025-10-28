using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class StageSaveManager
{
    private static string savePath => Path.Combine(Application.persistentDataPath, "StageSaveData.json");

    // 저장
    public static void SaveSingleStage(StageDataSO stage)
    {
        Wrapper wrapper;

        // 1기존 세이브 로드
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            wrapper = JsonUtility.FromJson<Wrapper>(json);

            if (wrapper == null || wrapper.list == null)
                wrapper = new Wrapper { list = new List<StageSaveData>() };
        }
        else
        {
            wrapper = new Wrapper { list = new List<StageSaveData>() };
        }

        // 클릭한 스테이지 항목 탐색
        var existing = wrapper.list.Find(s => s.sceneName == stage.SceneName);
        if (existing != null)
        {
            // 다른 데이터는 그대로 두고 isTried만 true로 변경
            if (!existing.isTried)
            {
                existing.isTried = true;
                Debug.Log($"[StageSaveManager] '{stage.SceneName}' isTried 업데이트");
            }
        }
        else
        {
            // 세이브에 없던 신규 스테이지라면 최소 정보만 추가
            wrapper.list.Add(new StageSaveData
            {
                sceneName = stage.SceneName,
                isTried = true,
                clearStar = 0,              // 초기값
                stageImagePath = ""         // 아직 없음
            });

            Debug.Log($"[StageSaveManager] '{stage.SceneName}' 신규 항목 추가 (isTried=true)");
        }

        // 파일 저장 (기존 데이터 유지)
        string newJson = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, newJson);
    }


    // 불러오기
    public static void Load(List<StageDataSO> stages)
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("처음 실행이므로 세이브 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(savePath);
        var wrapper = JsonUtility.FromJson<Wrapper>(json);

        foreach (var data in wrapper.list)
        {
            StageDataSO so = stages.Find(s => s.SceneName == data.sceneName);
            if (so != null)
            {
                so.IsTried = data.isTried;
                so.ClearStar = data.clearStar;
                so.StageImagePath = data.stageImagePath;
            }
        }

        Debug.Log($"세이브 데이터 로드 완료 ({wrapper.list.Count}개)");
    }

    // 스테이지 클리어 별 업데이트
    public static void UpdateStageData(StageDataSO stage, int earnedStars, string snapShotPath)
    {
        Wrapper wrapper = null;

        // 경로 정리
        snapShotPath = snapShotPath.Replace("\\", "/");

        // 기존 데이터 읽기 시도
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            wrapper = JsonUtility.FromJson<Wrapper>(json);

            // wrapper 또는 list가 null이면 새로 만듦 (파일은 그대로 둠)
            if (wrapper == null)
            {
                Debug.LogWarning($"[StageSaveManager] 기존 JSON 파싱 실패 → 새 wrapper 생성 (파일은 유지): {savePath}");
                wrapper = new Wrapper { list = new List<StageSaveData>() };
            }
            else if (wrapper.list == null)
            {
                Debug.LogWarning($"[StageSaveManager] wrapper.list null → 새 리스트 생성");
                wrapper.list = new List<StageSaveData>();
            }
        }
        else
        {
            // 파일 자체가 없을 때만 새로 생성
            wrapper = new Wrapper { list = new List<StageSaveData>() };
        }

        // 스테이지 데이터 업데이트
        var existing = wrapper.list.Find(s => s.sceneName == stage.SceneName);
        if (existing != null)
        {
            existing.isTried = true;
            existing.clearStar = Mathf.Max(existing.clearStar, earnedStars);
            existing.stageImagePath = snapShotPath;
        }
        else
        {
            wrapper.list.Add(new StageSaveData
            {
                sceneName = stage.SceneName,
                isTried = stage.IsTried,
                clearStar = earnedStars,
                stageImagePath = snapShotPath
            });
        }

        // 덮어쓰기 (wrapper만 갱신)
        string newJson = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, newJson);

        Debug.Log($"StageSaveManager: {stage.SceneName} 저장 완료 ({earnedStars}, 이미지: {snapShotPath})");
    }



    [System.Serializable]
    private class Wrapper
    {
        public List<StageSaveData> list;
    }
}
