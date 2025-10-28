using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 유니티 에디터의 메뉴를 통해 표준 키보드 키캡 프리팹을 생성합니다.
/// 이 스크립트는 반드시 "Assets/Editor" 폴더 내에 위치해야 합니다.
/// </summary>
public class MakeKeycapPrefab
{
    #region Constants
    private const float BASE_UNIT_SIZE = 1.9f;      // 1u 기본 크기 (Unity 단위)
    private const float KEYCAP_HEIGHT = 0.8f;       // 키캡 전체 높이
    private const float TOP_SURFACE_HEIGHT = 0.1f;  // 상단면 두께
    private const float WALL_THICKNESS = 0.15f;     // 측면 벽 두께
    private const float TOP_SURFACE_INSET = 0.1f;   // 상단면 오목한 정도
    private const float STEM_HEIGHT = 0.4f;         // 스템 높이
    private const float STEM_RADIUS = 0.2f;         // 스템 반지름
    #endregion

    #region Menu Items - Keycap Creation
    [MenuItem("Tools/Keycap Prefabs/Create 1.0u Keycap")]
    private static void Create1uKeycap()
    {
        CreateKeycap(1.0f, "Keycap_1.0u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 1.25u Keycap")]
    private static void Create125uKeycap()
    {
        CreateKeycap(1.25f, "Keycap_1.25u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 1.5u Keycap")]
    private static void Create15uKeycap()
    {
        CreateKeycap(1.5f, "Keycap_1.5u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 1.75u Keycap")]
    private static void Create175uKeycap()
    {
        CreateKeycap(1.75f, "Keycap_1.75u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 2.0u Keycap")]
    private static void Create2uKeycap()
    {
        CreateKeycap(2.0f, "Keycap_2.0u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 2.25u Keycap")]
    private static void Create225uKeycap()
    {
        CreateKeycap(2.25f, "Keycap_2.25u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 2.75u Keycap")]
    private static void Create275uKeycap()
    {
        CreateKeycap(2.75f, "Keycap_2.75u");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 6.25u Spacebar")]
    private static void Create625uSpacebar()
    {
        CreateKeycap(6.25f, "Keycap_6.25u_Spacebar");
    }

    [MenuItem("Tools/Keycap Prefabs/Create 75% Keyboard Layout")]
    private static void CreateKeyboard75Layout()
    {
        GameObject keyboardRoot = new GameObject("Keyboard_75_Layout");
        GameObject normalParent = new GameObject("Normal");
        normalParent.transform.SetParent(keyboardRoot.transform);
        normalParent.transform.localPosition = Vector3.zero;

        float rowSpacing = BASE_UNIT_SIZE;
        float currentZ = 0f;

        CreateRow1_ESC(keyboardRoot.transform, normalParent.transform, currentZ);
        currentZ -= rowSpacing;

        CreateRow2_Numbers(keyboardRoot.transform, normalParent.transform, currentZ);
        currentZ -= rowSpacing;

        CreateRow3_Tab(keyboardRoot.transform, normalParent.transform, currentZ);
        currentZ -= rowSpacing;

        CreateRow4_Caps(keyboardRoot.transform, normalParent.transform, currentZ);
        currentZ -= rowSpacing;

        CreateRow5_Shift(keyboardRoot.transform, normalParent.transform, currentZ);
        currentZ -= rowSpacing;

        CreateRow6_Bottom(keyboardRoot.transform, normalParent.transform, currentZ);

        Debug.Log("<color=green>75% Keyboard layout created successfully!</color>");
    }

    private static GameObject InstantiateKeycap(float sizeU, string keyName, Transform parent, Transform normalParent, Vector3 localPosition)
    {
        // 1.0u는 "1.0u"로, 나머지는 소수점 형식 유지
        string sizeString = (sizeU == 1.0f) ? "1.0u" :
                            (sizeU == 6.25f) ? "6.25u_Spacebar" :
                            $"{sizeU:0.##}u";

        string prefabPath = $"Assets/Prefabs/Keycaps/Keycap_{sizeString}.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Keycap prefab not found: {prefabPath}");
            return null;
        }

        GameObject keycap = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        keycap.name = keyName;

        Transform targetParent = (sizeU == 1.0f) ? normalParent : parent;
        keycap.transform.SetParent(targetParent);
        keycap.transform.localPosition = localPosition;
        return keycap;
    }

    private static void CreateRow1_ESC(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        InstantiateKeycap(1.0f, "ESC", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        for (int i = 1; i <= 4; i++)
        {
            InstantiateKeycap(1.0f, $"F{i}", parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }
        x += BASE_UNIT_SIZE * 0.25f;

        for (int i = 5; i <= 8; i++)
        {
            InstantiateKeycap(1.0f, $"F{i}", parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }
        x += BASE_UNIT_SIZE * 0.25f;

        for (int i = 9; i <= 12; i++)
        {
            InstantiateKeycap(1.0f, $"F{i}", parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }
        x += BASE_UNIT_SIZE * 0.25f;

        InstantiateKeycap(1.0f, "Insert", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE;
        InstantiateKeycap(1.0f, "Home", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE;
        InstantiateKeycap(1.0f, "Delete", parent, normalParent, new Vector3(x, 0, zPos));
    }

    private static void CreateRow2_Numbers(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        string[] keys = { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

        foreach (string key in keys)
        {
            InstantiateKeycap(1.0f, key, parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }

        InstantiateKeycap(2.0f, "Backspace", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.5f, 0, zPos));
        x += BASE_UNIT_SIZE * 2;

        InstantiateKeycap(1.0f, "PgUp", parent, normalParent, new Vector3(x, 0, zPos));
    }

    private static void CreateRow3_Tab(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        InstantiateKeycap(1.5f, "Tab", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.25f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.5f;

        string[] keys = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]" };
        foreach (string key in keys)
        {
            InstantiateKeycap(1.0f, key, parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }

        InstantiateKeycap(1.5f, "\\", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.25f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.5f;

        InstantiateKeycap(1.0f, "PgDn", parent, normalParent, new Vector3(x, 0, zPos));
    }

    private static void CreateRow4_Caps(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        InstantiateKeycap(1.75f, "CapsLock", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.375f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.75f;

        string[] keys = { "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'" };
        foreach (string key in keys)
        {
            InstantiateKeycap(1.0f, key, parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }

        InstantiateKeycap(2.25f, "Enter", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 2.25f;

        InstantiateKeycap(1.0f, "End", parent, normalParent, new Vector3(x, 0, zPos));
    }

    private static void CreateRow5_Shift(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        InstantiateKeycap(2.25f, "Shift_L", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 2.25f;

        string[] keys = { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" };
        foreach (string key in keys)
        {
            InstantiateKeycap(1.0f, key, parent, normalParent, new Vector3(x, 0, zPos));
            x += BASE_UNIT_SIZE;
        }

        InstantiateKeycap(1.75f, "Shift_R", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.375f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.75f;

        InstantiateKeycap(1.0f, "Up", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE;
    }

    private static void CreateRow6_Bottom(Transform parent, Transform normalParent, float zPos)
    {
        float x = 0f;
        InstantiateKeycap(1.25f, "Ctrl_L", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        InstantiateKeycap(1.25f, "Win", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        InstantiateKeycap(1.25f, "Alt_L", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        InstantiateKeycap(6.25f, "Space", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 3.125f, 0, zPos));
        x += BASE_UNIT_SIZE * 6.25f;

        InstantiateKeycap(1.25f, "Alt_R", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        InstantiateKeycap(1.25f, "Ctrl_R", parent, normalParent, new Vector3(x + BASE_UNIT_SIZE * 0.625f, 0, zPos));
        x += BASE_UNIT_SIZE * 1.25f;

        InstantiateKeycap(1.0f, "Left", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE;
        InstantiateKeycap(1.0f, "Down", parent, normalParent, new Vector3(x, 0, zPos));
        x += BASE_UNIT_SIZE;
        InstantiateKeycap(1.0f, "Right", parent, normalParent, new Vector3(x, 0, zPos));
    }
    #endregion

    #region Private Methods - Keycap Construction
    /// <summary>
    /// 지정된 사이즈의 키캡을 생성합니다.
    /// </summary>
    /// <param name="sizeMultiplier">키캡 사이즈 배수 (1.0, 1.25, 2.0 등)</param>
    /// <param name="keycapName">키캡 이름</param>
    private static void CreateKeycap(float sizeMultiplier, string keycapName)
    {
        Debug.Log($"Creating keycap: {keycapName} (size: {sizeMultiplier}u)");

        // 루트 게임 오브젝트 생성
        GameObject keycapRoot = new GameObject(keycapName);
        keycapRoot.transform.position = Vector3.zero;

        // 키캡 너비 계산
        float keycapWidthWorldUnits = BASE_UNIT_SIZE * sizeMultiplier;

        // 키캡 구성 요소 생성
        CreateTopSurface(keycapRoot.transform, keycapWidthWorldUnits);
        CreateSideWalls(keycapRoot.transform, keycapWidthWorldUnits);
        CreateStem(keycapRoot.transform);

        // 프리팹으로 저장
        SavePrefab(keycapRoot, keycapName);
    }

    /// <summary>
    /// 키캡 상단면을 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="widthWorldUnits">키캡 너비 (월드 단위)</param>
    private static void CreateTopSurface(Transform parent, float widthWorldUnits)
    {
        GameObject topSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topSurface.name = "TopSurface";
        topSurface.transform.SetParent(parent);

        // 상단면 크기 (Wall 안쪽 빈 공간에 위치)
        float topWidthWorldUnits = widthWorldUnits - (TOP_SURFACE_INSET * 2);
        float topDepthWorldUnits = BASE_UNIT_SIZE - (TOP_SURFACE_INSET * 2);

        // Wall 높이 계산
        float wallHeight = KEYCAP_HEIGHT - TOP_SURFACE_HEIGHT;

        // Top 높이: Stem 윗면부터 Wall 윗면 + TOP_SURFACE_HEIGHT까지
        float topHeight = (wallHeight + TOP_SURFACE_HEIGHT) - STEM_HEIGHT;

        topSurface.transform.localScale = new Vector3(
            topWidthWorldUnits,
            topHeight,
            topDepthWorldUnits
        );

        // Top 중심 Y 위치
        float yPosition = STEM_HEIGHT + (topHeight / 2.0f);
        topSurface.transform.localPosition = new Vector3(0, yPosition, 0);
    }

    /// <summary>
    /// 키캡 측면 벽을 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="widthWorldUnits">키캡 너비 (월드 단위)</param>
    private static void CreateSideWalls(Transform parent, float widthWorldUnits)
    {
        float wallHeightWorldUnits = KEYCAP_HEIGHT - TOP_SURFACE_HEIGHT;
        float wallYPosition = wallHeightWorldUnits / 2.0f;

        // 앞쪽 벽 (Z+)
        GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontWall.name = "Wall_Front";
        frontWall.transform.SetParent(parent);
        frontWall.transform.localScale = new Vector3(
            widthWorldUnits,
            wallHeightWorldUnits,
            WALL_THICKNESS
        );
        frontWall.transform.localPosition = new Vector3(
            0,
            wallYPosition,
            (BASE_UNIT_SIZE / 2.0f) - (WALL_THICKNESS / 2.0f)
        );

        // 뒤쪽 벽 (Z-)
        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Wall_Back";
        backWall.transform.SetParent(parent);
        backWall.transform.localScale = new Vector3(
            widthWorldUnits,
            wallHeightWorldUnits,
            WALL_THICKNESS
        );
        backWall.transform.localPosition = new Vector3(
            0,
            wallYPosition,
            -(BASE_UNIT_SIZE / 2.0f) + (WALL_THICKNESS / 2.0f)
        );

        // 왼쪽 벽 (X-)
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Wall_Left";
        leftWall.transform.SetParent(parent);
        leftWall.transform.localScale = new Vector3(
            WALL_THICKNESS,
            wallHeightWorldUnits,
            BASE_UNIT_SIZE - (WALL_THICKNESS * 2)
        );
        leftWall.transform.localPosition = new Vector3(
            -(widthWorldUnits / 2.0f) + (WALL_THICKNESS / 2.0f),
            wallYPosition,
            0
        );

        // 오른쪽 벽 (X+)
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Wall_Right";
        rightWall.transform.SetParent(parent);
        rightWall.transform.localScale = new Vector3(
            WALL_THICKNESS,
            wallHeightWorldUnits,
            BASE_UNIT_SIZE - (WALL_THICKNESS * 2)
        );
        rightWall.transform.localPosition = new Vector3(
            (widthWorldUnits / 2.0f) - (WALL_THICKNESS / 2.0f),
            wallYPosition,
            0
        );
    }

    /// <summary>
    /// 키캡 하단 스템을 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    private static void CreateStem(Transform parent)
    {
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(parent);

        // 기존 CapsuleCollider 제거하고 MeshCollider 추가
        Object.DestroyImmediate(stem.GetComponent<Collider>());
        var mesh = stem.AddComponent<MeshCollider>();
        mesh.convex = true;

        stem.transform.localScale = new Vector3(
            STEM_RADIUS * 2,
            STEM_HEIGHT / 2.0f,
            STEM_RADIUS * 2
        );

        // 스템 위치 (키캡 내부 바닥부터 위로 올라가는 형태)
        float yPosition = STEM_HEIGHT / 2.0f;
        stem.transform.localPosition = new Vector3(0, yPosition, 0);
    }
    #endregion

    #region Private Methods - Utility
    /// <summary>
    /// 씬에 생성된 게임 오브젝트를 지정된 이름의 프리팹으로 저장합니다.
    /// </summary>
    /// <param name="rootObject">프리팹으로 만들 루트 게임 오브젝트</param>
    /// <param name="prefabName">저장할 프리팹 파일 이름 (확장자 제외)</param>
    private static void SavePrefab(GameObject rootObject, string prefabName)
    {
        // 프리팹 저장 경로 설정
        string prefabDir = "Assets/Prefabs/Keycaps";
        string prefabPath = $"{prefabDir}/{prefabName}.prefab";

        // "Assets/Prefabs" 폴더가 없으면 생성
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            Debug.Log("Created 'Assets/Prefabs' directory.");
        }

        // "Assets/Prefabs/Keycaps" 폴더가 없으면 생성
        if (!Directory.Exists(Application.dataPath + "/Prefabs/Keycaps"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Keycaps");
            Debug.Log("Created 'Assets/Prefabs/Keycaps' directory.");
        }

        // 경로가 겹치지 않도록 고유한 경로 생성
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        // 씬에 생성된 게임 오브젝트를 프리팹으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObject, uniquePath);

        if (prefab != null)
        {
            Debug.Log($"<color=green>Successfully created keycap prefab at: {uniquePath}</color>");
        }
        else
        {
            Debug.LogError($"Failed to create keycap prefab: {prefabName}");
        }

        // 씬에 임시로 생성했던 게임 오브젝트 삭제
        GameObject.DestroyImmediate(rootObject);
    }
    #endregion
}