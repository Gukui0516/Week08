using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClimaxController_Advanced))]
public class ClimaxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ClimaxController_Advanced controller = (ClimaxController_Advanced)target;

        // 기본 Inspector 그리기
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("폭탄 관리 도구", EditorStyles.boldLabel);
        
        // 자동 검색 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🔍 씬에서 폭탄 자동 검색 (이름 순서)", GUILayout.Height(40)))
        {
            controller.SearchAndAssignBombs();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            "1. '🔍 씬에서 폭탄 자동 검색' 버튼 클릭\n" +
            "2. 리스트에서 드래그로 순서 조정\n" +
            "3. Scene 뷰에서 Gizmos로 순서 확인",
            MessageType.Info
        );
    }
}