// C# Script: ObjectArrangerTool.cs
using UnityEngine;
using UnityEditor;

public class ObjectArrangerTool : EditorWindow
{
    // 좌표계 선택을 위한 Enum
    public enum CoordinateSpace
    {
        World, // 월드 좌표계
        Local  // 로컬 좌표계
    }

    private Vector3 centerPoint = Vector3.zero;
    private float radius = 5.0f;
    private float totalArc = 360.0f;
    private bool useSpacedArc = false;
    private CoordinateSpace coordinateSpace = CoordinateSpace.World; // 좌표계 선택 변수

    /// <summary>
    /// "Tools/Object Arranger" 메뉴를 통해 에디터 창을 엽니다.
    /// </summary>
    [MenuItem("Tools/Object Arranger")]
    public static void ShowWindow()
    {
        GetWindow<ObjectArrangerTool>("Object Arranger");
    }

    /// <summary>
    /// 에디터 창의 GUI를 그립니다.
    /// </summary>
    void OnGUI()
    {
        GUILayout.Label("오브젝트 배치 설정", EditorStyles.boldLabel);

        // UI 필드: 중심점, 반지름, 호 각도, 좌표계 선택
        coordinateSpace = (CoordinateSpace)EditorGUILayout.EnumPopup("1. 기준 좌표계", coordinateSpace);
        centerPoint = EditorGUILayout.Vector3Field("2. 중심점 (위치/높이)", centerPoint);
        radius = EditorGUILayout.FloatField("3. 반지름 (거리)", radius);

        useSpacedArc = EditorGUILayout.Toggle("4. 특정 호(Arc) 사용", useSpacedArc);

        if (useSpacedArc)
        {
            totalArc = EditorGUILayout.Slider("배치할 호 각도", totalArc, 0.0f, 360.0f);
        }
        else
        {
            totalArc = 360.0f;
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("선택한 오브젝트 배치 실행"))
        {
            ArrangeObjects();
        }

        // 도움말 박스 내용 업데이트
        EditorGUILayout.HelpBox("사용법:\n1. 씬(Scene)에서 배치할 오브젝트들을 모두 선택하세요.\n2. 기준 좌표계(World/Local) 및 기타 옵션을 설정하세요.\n3. '배치 실행' 버튼을 누르세요.\n\n※ 'Local' 좌표계는 부모 오브젝트 기준입니다. 정확한 원형 배치를 위해선 선택한 오브젝트들이 동일한 부모를 가져야 합니다.", MessageType.Info);
    }

    /// <summary>
    /// 선택된 오브젝트들을 계산된 위치에 배치하는 핵심 로직입니다.
    /// </summary>
    void ArrangeObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int objectCount = selectedObjects.Length;

        if (objectCount == 0)
        {
            Debug.LogWarning("배치할 오브젝트가 선택되지 않았습니다.");
            return;
        }

        // 로컬 좌표계 사용 시, 선택된 오브젝트들이 동일한 부모를 가졌는지 확인
        if (coordinateSpace == CoordinateSpace.Local)
        {
            Transform firstParent = selectedObjects[0].transform.parent;
            for (int i = 1; i < objectCount; i++)
            {
                if (selectedObjects[i].transform.parent != firstParent)
                {
                    Debug.LogWarning("로컬 좌표계 배치는 모든 오브젝트가 동일한 부모를 가질 때 가장 정확하게 동작합니다. 일부 오브젝트의 부모가 다릅니다.");
                    break; // 경고는 한 번만 출력
                }
            }
        }

        Undo.RecordObjects(Selection.transforms, "Arrange Objects");

        float angleStep;
        if (useSpacedArc && objectCount > 1 && totalArc < 360.0f)
        {
            angleStep = totalArc / (objectCount - 1);
        }
        else
        {
            angleStep = totalArc / objectCount;
        }

        for (int i = 0; i < objectCount; i++)
        {
            float angleInDegrees = i * angleStep;
            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

            float x = centerPoint.x + radius * Mathf.Cos(angleInRadians);
            float z = centerPoint.z + radius * Mathf.Sin(angleInRadians);
            float y = centerPoint.y;

            Vector3 newPosition = new Vector3(x, y, z);

            // 선택된 좌표계에 따라 position 또는 localPosition을 설정합니다.
            if (coordinateSpace == CoordinateSpace.World)
            {
                selectedObjects[i].transform.position = newPosition;
            }
            else // coordinateSpace == CoordinateSpace.Local
            {
                selectedObjects[i].transform.localPosition = newPosition;
            }
        }

        Debug.Log(objectCount + "개의 오브젝트를 성공적으로 배치했습니다.");
    }
}