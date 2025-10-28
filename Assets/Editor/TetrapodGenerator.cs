using UnityEngine;
using UnityEditor;

public class TetrapodGenerator : EditorWindow
{
    // 테트라포드 다리(원통)의 길이와 두께 설정
    private float legLength = 3.0f;
    private float legRadius = 0.5f;

    [MenuItem("Tools/S_Tetrapod Generator")] // 유니티 메뉴에 추가
    public static void ShowWindow()
    {
        GetWindow<TetrapodGenerator>("Tetrapod Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tetrapod Settings", EditorStyles.boldLabel);

        // GUI를 통해 설정값을 변경할 수 있도록 합니다.
        legLength = EditorGUILayout.FloatField("Leg Length", legLength);
        legRadius = EditorGUILayout.FloatField("Leg Radius", legRadius);

        if (GUILayout.Button("Generate Tetrapod"))
        {
            GenerateTetrapod();
        }
    }

    private void GenerateTetrapod()
    {
        // 1. 테트라포드의 중심이 될 부모 오브젝트 생성
        GameObject parent = new GameObject("Tetrapod");
        parent.transform.position = Vector3.zero; // 월드 중앙에 배치

        // 생성된 오브젝트를 Undo 시스템에 등록
        Undo.RegisterCreatedObjectUndo(parent, "Create Tetrapod");

        // 2. 정사면체 꼭짓점 방향 벡터 (중심각 약 109.5도)
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 1, 1).normalized,
            new Vector3(1, -1, -1).normalized,
            new Vector3(-1, 1, -1).normalized,
            new Vector3(-1, -1, 1).normalized
        };

        // 3. 네 개의 다리(원통) 생성
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];

            // Unity의 기본 원통은 Y축 방향이 위쪽(height)이므로,
            // 길이는 원통의 높이(scale.y)를 조정하여 설정합니다.

            // a. 원통 오브젝트 생성 (기본 3D 오브젝트)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            // b. 부모 설정
            cylinder.transform.SetParent(parent.transform);

            // c. 크기 설정: 반지름(scale.x/z) 및 길이(scale.y)
            cylinder.transform.localScale = new Vector3(legRadius * 2, legLength / 2, legRadius * 2);

            // d. 위치 설정: 중심에서 절반 길이만큼 해당 방향으로 이동
            cylinder.transform.localPosition = direction * (legLength / 2);

            // e. 회전 설정: 원통의 Y축(높이)이 direction을 바라보도록 회전
            cylinder.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            // f. 이름을 지정하고 Undo 시스템에 등록
            cylinder.name = "Leg " + (i + 1);
            Undo.RegisterCreatedObjectUndo(cylinder, "Create Tetrapod Leg");

            // *선택 사항: 다리 끝에 구(Sphere) 추가 (부드러운 마감)
            GameObject sphereCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereCap.transform.SetParent(parent.transform);
            sphereCap.transform.localPosition = direction * legLength;
            sphereCap.transform.localScale = Vector3.one * legRadius * 2;
            sphereCap.name = "Cap " + (i + 1);
            Undo.RegisterCreatedObjectUndo(sphereCap, "Create Tetrapod Cap");
        }

        // 4. 중심부에 구(Sphere) 추가 (선택 사항: 결합 부위 마감)
        GameObject centerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        centerSphere.transform.SetParent(parent.transform);
        centerSphere.transform.localPosition = Vector3.zero;
        centerSphere.transform.localScale = Vector3.one * legRadius * 2;
        centerSphere.name = "Center";
        Undo.RegisterCreatedObjectUndo(centerSphere, "Create Tetrapod Center");

        // 생성 완료 후 알림
        Debug.Log("Tetrapod generated successfully at: " + parent.transform.position);

        // 생성된 오브젝트를 선택하도록 설정
        Selection.activeGameObject = parent;
    }
}

// Editor 폴더에 넣으면 Unity가 자동으로 인식하여 메뉴에 'Tools/S_Tetrapod Generator'가 생성됩니다.