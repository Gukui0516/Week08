 using UnityEngine;
using UnityEditor; // 에디터 스크립트 작성을 위해 필수
using System.IO;   // 디렉토리(폴더) 관리를 위해 사용

/// <summary>
/// 유니티 에디터의 메뉴를 통해 프리미티브 도형을 조합한 건축물 프리팹을 생성합니다.
/// 이 스크립트는 반드시 "Assets/Editor" 폴더 내에 위치해야 합니다.
/// </summary>
public class MakeBuildPrefab
{
    // --- 피사의 사탑 생성 ---

    [MenuItem("Tools/Build Prefabs/Create Leaning Tower of Pisa")]
    private static void CreateLeaningTowerOfPisa()
    {
        // --- 1. 건축물 파라미터 설정 ---
        int numFloors = 8;        // 탑의 층수
        float floorHeight = 1.0f; // 각 층의 높이
        float radius = 4.0f;      // 탑의 반지름
        float leanPerFloor = 0.2f;  // 각 층마다 기울어지는 정도

        // --- 2. 루트 게임 오브젝트 생성 ---
        GameObject towerRoot = new GameObject("LeaningTowerOfPisa");
        towerRoot.transform.position = Vector3.zero;

        float currentY = 0.0f;     // 현재 쌓이는 높이
        float currentLean = 0.0f;  // 현재 누적된 기울기

        // --- 3. 프리미티브 도형으로 건축물 조립 ---
        Debug.Log($"Building Leaning Tower of Pisa with {numFloors} floors...");

        // 기본 베이스 (정육면체 Cube 사용)
        GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.name = "Base";
        baseBlock.transform.SetParent(towerRoot.transform);
        baseBlock.transform.localScale = new Vector3(radius * 2.2f, floorHeight, radius * 2.2f);
        baseBlock.transform.localPosition = new Vector3(0, floorHeight / 2.0f, 0);

        currentY = floorHeight; // 첫 층의 높이 업데이트

        // 층수만큼 반복하여 실린더(Cylinder)를 쌓습니다.
        for (int i = 0; i < numFloors; i++)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = $"Floor_{i + 1}";
            floor.transform.SetParent(towerRoot.transform);
            floor.transform.localScale = new Vector3(radius * 2, floorHeight / 2.0f, radius * 2);
            float yPos = currentY + (floorHeight / 2.0f);
            floor.transform.localPosition = new Vector3(currentLean, yPos, 0);

            currentY += floorHeight;
            currentLean += leanPerFloor;
        }

        // --- 4. 프리팹으로 저장 ---
        // 공용 저장 함수 호출
        SavePrefab(towerRoot, "LeaningTowerOfPisa");
    }

    // --- 콜로세움 생성 (새로 추가된 부분) ---

    [MenuItem("Tools/Build Prefabs/Create Colosseum")]
    private static void CreateColosseum()
    {
        // --- 1. 건축물 파라미터 설정 ---
        float arenaRadius = 15f;         // 내부 원형 경기장 반지름
        float wallThickness = 6f;          // 외벽 두께
        int numTiers = 3;                  // 층수
        int pillarsPerTier = 40;         // 층별 기둥 개수
        float pillarHeight = 3.5f;       // 기둥 높이
        float pillarRadius = 0.5f;       // 기둥 반지름
        float lintelHeight = 1.0f;       // 기둥 위 아치/상판(직육면체) 높이
        float lintelWidthMultiplier = 1.2f; // 기둥보다 살짝 넓게

        // --- 2. 루트 게임 오브젝트 생성 ---
        GameObject colosseumRoot = new GameObject("Colosseum");
        colosseumRoot.transform.position = Vector3.zero;

        Debug.Log($"Building Colosseum with {numTiers} tiers...");

        // --- 3. 프리미티브 도형으로 건축물 조립 ---

        // 3-1. 바닥 (Arena Floor) 생성 (Cylinder)
        GameObject arenaFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arenaFloor.name = "ArenaFloor";
        arenaFloor.transform.SetParent(colosseumRoot.transform);
        // (경기장 반지름 + 벽 두께) * 2 = 전체 지름
        float totalDiameter = (arenaRadius + wallThickness) * 2;
        arenaFloor.transform.localScale = new Vector3(totalDiameter, 0.2f, totalDiameter);
        arenaFloor.transform.localPosition = Vector3.zero; // Y=0 (바닥)

        // 3-2. 층(Tier)별 기둥(Pillar) 및 상판(Lintel) 생성
        float currentY = 0.1f; // 바닥(0.2f 높이)의 절반부터 시작
        Vector3 pillarScale = new Vector3(pillarRadius * 2, pillarHeight / 2.0f, pillarRadius * 2);

        for (int tier = 0; tier < numTiers; tier++)
        {
            // 각 층을 담을 빈 오브젝트 생성 (계층 구조 정리용)
            GameObject tierRoot = new GameObject($"Tier_{tier + 1}");
            tierRoot.transform.SetParent(colosseumRoot.transform);

            // 층의 Y 위치 설정 (기둥 높이의 절반)
            float tierYPos = currentY + (pillarHeight / 2.0f);
            tierRoot.transform.localPosition = new Vector3(0, tierYPos, 0);

            // 층별 반지름 (위로 갈수록 살짝 넓어지는 효과)
            float currentRadius = arenaRadius + (tier * 0.5f);

            Vector3 lastPillarPos = Vector3.zero;
            Vector3 firstPillarPos = Vector3.zero;

            // 3-3. 원형으로 기둥 배치 (Cylinder)
            for (int i = 0; i < pillarsPerTier; i++)
            {
                float angle = (float)i / pillarsPerTier * 360f;
                float radAngle = angle * Mathf.Deg2Rad;

                // 원형 위치 계산 (X, Z 평면)
                float x = currentRadius * Mathf.Cos(radAngle);
                float z = currentRadius * Mathf.Sin(radAngle);

                // 기둥의 로컬 위치 (Y=0, 부모인 tierRoot가 높이를 갖고 있음)
                Vector3 pillarPos = new Vector3(x, 0, z);

                // 기둥 생성
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i}";
                pillar.transform.SetParent(tierRoot.transform);
                pillar.transform.localPosition = pillarPos;
                pillar.transform.localScale = pillarScale;

                // 3-4. 기둥 위에 상판(Lintel) 배치 (Cube)
                if (i > 0)
                {
                    // 이전 기둥과 현재 기둥을 연결하는 상판 생성
                    CreateLintel(tierRoot.transform, lastPillarPos, pillarPos, pillarHeight, lintelHeight, pillarRadius * lintelWidthMultiplier);
                }

                lastPillarPos = pillarPos;
                if (i == 0) firstPillarPos = pillarPos;
            }

            // 마지막 기둥과 첫 번째 기둥을 연결
            CreateLintel(tierRoot.transform, lastPillarPos, firstPillarPos, pillarHeight, lintelHeight, pillarRadius * lintelWidthMultiplier);

            // 다음 층의 Y 위치 업데이트
            currentY += pillarHeight + lintelHeight;
        }

        // --- 4. 프리팹으로 저장 ---
        SavePrefab(colosseumRoot, "Colosseum");
    }

    /// <summary>
    /// 두 기둥(위치 A, B) 사이에 상판(Lintel)을 생성하는 도우미 함수
    /// </summary>
    /// <param name="parent">상판이 속할 부모 Transform</param>
    /// <param name="posA">첫 번째 기둥 위치</param>
    /// <param name="posB">두 번째 기둥 위치</param>
    /// <param name="pillarHeight">기둥의 실제 높이</param>
    /// <param name="lintelHeight">상판의 높이</param>
    /// <param name="lintelWidth">상판의 폭(두께)</param>
    private static void CreateLintel(Transform parent, Vector3 posA, Vector3 posB, float pillarHeight, float lintelHeight, float lintelWidth)
    {
        // PrimitiveType.Cube 사용
        GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "Lintel";
        lintel.transform.SetParent(parent);

        // 두 기둥 사이의 거리와 중심점 계산
        float distance = Vector3.Distance(posA, posB);
        Vector3 midpoint = (posA + posB) / 2.0f;

        // 상판 위치 설정 (기둥 위)
        // Y 위치 = 기둥 높이의 절반 + 상판 높이의 절반
        lintel.transform.localPosition = new Vector3(midpoint.x, (pillarHeight / 2.0f) + (lintelHeight / 2.0f), midpoint.z);

        // 상판 크기 설정
        // Z 스케일이 길이 방향이 되도록 LookRotation을 사용할 것임
        lintel.transform.localScale = new Vector3(lintelWidth, lintelHeight, distance);

        // 상판 회전 설정 (posA에서 posB를 바라보도록)
        lintel.transform.rotation = Quaternion.LookRotation(posB - posA);
    }


    // --- 공용 프리팹 저장 함수 ---

    /// <summary>
    /// 씬에 생성된 게임 오브젝트를 지정된 이름의 프리팹으로 저장합니다.
    /// (코드 중복을 방지하기 위한 공용 함수)
    /// </summary>
    /// <param name="rootObject">프리팹으로 만들 루트 게임 오브젝트</param>
    /// <param name="prefabName">저장할 프리팹 파일 이름 (확장자 제외)</param>
    private static void SavePrefab(GameObject rootObject, string prefabName)
    {
        // 프리팹을 저장할 경로 설정
        string prefabDir = "Assets/Prefabs";
        string prefabPath = $"{prefabDir}/{prefabName}.prefab";

        // "Assets/Prefabs" 폴더가 없으면 생성
        // (Directory.Exists는 전체 경로가 필요)
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            // AssetDatabase.CreateFolder는 "Assets" 기준 경로를 받습니다.
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            Debug.Log("Created 'Assets/Prefabs' directory.");
        }

        // 경로가 겹치지 않도록 고유한 경로 생성 (예: "Colosseum 1.prefab")
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        // 씬에 생성된 towerRoot 게임 오브젝트를 프리팹으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObject, uniquePath);

        if (prefab != null)
        {
            Debug.Log($"<color=green>Successfully created prefab at: {uniquePath}</color>");
        }
        else
        {
            Debug.LogError($"Failed to create prefab: {prefabName}");
        }

        // 씬에 임시로 생성했던 게임 오브젝트 삭제
        GameObject.DestroyImmediate(rootObject);
    }
}