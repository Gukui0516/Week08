using UnityEngine;
using UnityEditor;

public class PolygonGenerator : EditorWindow
{
    #region Editor Fields
    private int _sideCount = 6;
    private float _edgeLength = 2.0f;
    private float _edgeThickness = 0.2f;
    #endregion

    #region Unity Editor Methods
    [MenuItem("Tools/Polygon Generator")]
    public static void ShowWindow()
    {
        GetWindow<PolygonGenerator>("Polygon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Polygon Settings", EditorStyles.boldLabel);

        _sideCount = EditorGUILayout.IntField("Side Count", _sideCount);
        _edgeLength = EditorGUILayout.FloatField("Edge Length", _edgeLength);
        _edgeThickness = EditorGUILayout.FloatField("Edge Thickness", _edgeThickness);

        if (GUILayout.Button("Generate Polygon"))
        {
            GeneratePolygon();
        }
    }
    #endregion

    #region Private Methods - Polygon Generation
    /// <summary>정 n각형 생성</summary>
    private void GeneratePolygon()
    {
        if (!ValidateInput())
            return;

        GameObject parent = CreateParentObject();

        // 모서리 길이로부터 외접원 반지름 계산
        float radius = _edgeLength / (2f * Mathf.Sin(Mathf.PI / _sideCount));
        Vector3[] vertices = CalculatePolygonVertices(_sideCount, radius);

        // 모서리 박스 생성
        for (int i = 0; i < _sideCount; i++)
        {
            Vector3 startPos = vertices[i];
            Vector3 endPos = vertices[(i + 1) % _sideCount];
            CreateEdgeBox(parent.transform, startPos, endPos, i);
        }

        CreateCenterObject(parent.transform);

        Debug.Log($"Polygon (n={_sideCount}) generated at: {parent.transform.position}");
        Selection.activeGameObject = parent;
    }

    /// <summary>부모 GameObject 생성</summary>
    /// <returns>부모 GameObject</returns>
    private GameObject CreateParentObject()
    {
        GameObject parent = new GameObject($"Polygon_{_sideCount}");
        parent.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(parent, "Create Polygon");
        return parent;
    }

    /// <summary>중심 빈 GameObject 생성</summary>
    /// <param name="parent">부모 Transform</param>
    private void CreateCenterObject(Transform parent)
    {
        GameObject center = new GameObject("Center");
        center.transform.SetParent(parent);
        center.transform.localPosition = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(center, "Create Polygon Center");
    }

    /// <summary>모서리 박스 생성</summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="startPos">시작 위치</param>
    /// <param name="endPos">끝 위치</param>
    /// <param name="index">모서리 인덱스</param>
    private void CreateEdgeBox(Transform parent, Vector3 startPos, Vector3 endPos, int index)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(parent);

        // 위치: 두 꼭짓점의 중점
        cube.transform.localPosition = (startPos + endPos) / 2f;

        // 회전: 로컬 X축이 모서리 방향을 향하도록
        Vector3 direction = (endPos - startPos).normalized;
        cube.transform.rotation = Quaternion.LookRotation(Vector3.up, direction) * Quaternion.Euler(0, 0, -90);

        // 크기: X=모서리 길이, Y=두께, Z=두께
        cube.transform.localScale = new Vector3(_edgeLength, _edgeThickness, _edgeThickness);

        cube.name = $"Edge_{index + 1}";
        Undo.RegisterCreatedObjectUndo(cube, "Create Polygon Edge");
    }
    #endregion

    #region Private Methods - Math Helpers
    /// <summary>XZ 평면에서 정 n각형 꼭짓점 계산</summary>
    /// <param name="sideCount">각 개수</param>
    /// <param name="radius">반지름</param>
    /// <returns>꼭짓점 배열</returns>
    private Vector3[] CalculatePolygonVertices(int sideCount, float radius)
    {
        Vector3[] vertices = new Vector3[sideCount];
        float angleStep = 360f / sideCount;

        for (int i = 0; i < sideCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i] = new Vector3(x, 0, z);
        }

        return vertices;
    }

    /// <summary>입력 검증</summary>
    /// <returns>유효한 입력인지 여부</returns>
    private bool ValidateInput()
    {
        if (_sideCount < 3)
        {
            EditorUtility.DisplayDialog("Invalid Input", "Side Count must be at least 3.", "OK");
            return false;
        }

        if (_edgeLength <= 0 || _edgeThickness <= 0)
        {
            EditorUtility.DisplayDialog("Invalid Input", "Edge Length and Thickness must be positive.", "OK");
            return false;
        }

        return true;
    }
    #endregion
}