using UnityEngine;
using UnityEditor;

public class XBlockGenerator : EditorWindow
{
    // 블록의 길이와 폭(두께) 설정
    private float blockLength = 4.0f;
    private float blockWidth = 1.0f;

    [MenuItem("Tools/S_X-Block Generator")] // 유니티 메뉴에 추가
    public static void ShowWindow()
    {
        GetWindow<XBlockGenerator>("X-Block Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("X-Block Settings", EditorStyles.boldLabel);

        // GUI를 통해 설정값을 변경할 수 있도록 합니다.
        blockLength = EditorGUILayout.FloatField("Block Length", blockLength);
        blockWidth = EditorGUILayout.FloatField("Block Width (Thickness)", blockWidth);

        if (GUILayout.Button("Generate X-Block"))
        {
            GenerateXBlock();
        }
    }

    private void GenerateXBlock()
    {
        // 1. 엑스 블록의 중심이 될 부모 오브젝트 생성
        GameObject parent = new GameObject("X-Block");
        parent.transform.position = Vector3.zero;

        // 생성된 오브젝트를 Undo 시스템에 등록
        Undo.RegisterCreatedObjectUndo(parent, "Create X-Block");

        // 2. 서로 직교하는 세 축(X, Y, Z) 방향
        Vector3[] axes = new Vector3[]
        {
            Vector3.right, // (1, 0, 0)
            Vector3.up,    // (0, 1, 0)
            Vector3.forward // (0, 0, 1)
        };

        // 3. 세 개의 직육면체(큐브) 생성 및 배치
        for (int i = 0; i < axes.Length; i++)
        {
            // a. 큐브 오브젝트 생성 (기본 3D 오브젝트)
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // b. 부모 설정
            cube.transform.SetParent(parent.transform);

            // c. 크기 설정:
            // 큐브의 기본 크기는 1입니다. 길이는 blockLength, 나머지 두께는 blockWidth로 설정합니다.
            Vector3 scale = Vector3.one;

            if (axes[i] == Vector3.right) // X축 방향 블록
            {
                scale = new Vector3(blockLength, blockWidth, blockWidth);
            }
            else if (axes[i] == Vector3.up) // Y축 방향 블록
            {
                scale = new Vector3(blockWidth, blockLength, blockWidth);
            }
            else if (axes[i] == Vector3.forward) // Z축 방향 블록
            {
                scale = new Vector3(blockWidth, blockWidth, blockLength);
            }

            cube.transform.localScale = scale;

            // d. 위치 및 회전 설정:
            // 중심을 기준으로 스케일링을 했기 때문에 위치(localPosition)는 (0, 0, 0) 그대로 두고
            // 회전(rotation) 역시 필요하지 않습니다. (이미 축에 맞춰 정렬됨)
            cube.transform.localPosition = Vector3.zero;

            // e. 이름을 지정하고 Undo 시스템에 등록
            cube.name = "Arm " + axes[i].ToString();
            Undo.RegisterCreatedObjectUndo(cube, "Create X-Block Arm");
        }

        // 생성 완료 후 알림
        Debug.Log("X-Block generated successfully at: " + parent.transform.position);

        // 생성된 오브젝트를 선택하도록 설정
        Selection.activeGameObject = parent;
    }
}