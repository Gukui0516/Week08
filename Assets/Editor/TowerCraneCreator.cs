using UnityEditor;
using UnityEngine;

public class TowerCraneCreator_Detailed
{
    // 재질을 쉽게 참조하기 위한 변수
    private static Material craneRedMaterial; // -> 'Movable' 머티리얼을 담을 변수
    private static Material darkGrayMaterial;
    private static Material cableMaterial;

    [MenuItem("GameObject/3D Object/Tower Crane (Detailed)")]
    public static void CreateTowerCrane()
    {
        // --- 재질 생성 ---

        // 1. 'Movable' 머티리얼 로드 (Resources 폴더 기준 경로)
        string movablePath = "Materials/LBC/Old/Movable";
        craneRedMaterial = Resources.Load<Material>(movablePath);

        // 2. 머티리얼 로드 실패 시 예외 처리
        if (craneRedMaterial == null)
        {
            Debug.LogError($"[TowerCraneCreator] 머티리얼을 찾을 수 없습니다: 'Resources/{movablePath}'. 기본 빨간색 머티리얼을 사용합니다.");
            craneRedMaterial = new Material(Shader.Find("Standard"));
            craneRedMaterial.color = new Color(0.8f, 0.1f, 0.1f);
        }

        // 3. 나머지 머티리얼 생성
        darkGrayMaterial = new Material(Shader.Find("Standard"));
        darkGrayMaterial.color = Color.gray * 0.5f;

        cableMaterial = new Material(Shader.Find("Standard"));
        cableMaterial.color = Color.gray * 0.8f;

        // --- 기본 설정 ---
        int mastSections = 15;
        float mastSectionHeight = 2.0f;
        float mastWidth = 2.0f;
        float pillarSize = 0.2f; // 마스트 기둥 두께

        float jibLength = 20.0f;
        float counterJibLength = 8.0f;
        float jibTrussHeight = 1.2f;

        float kingPostHeight = 5.0f; // A-프레임(지지삭) 높이
        float ropeLength = 10.0f;

        // --- 루트 오브젝트 ---
        GameObject craneRoot = new GameObject("TowerCrane (Detailed)");
        Undo.RegisterCreatedObjectUndo(craneRoot, "Create Detailed Tower Crane");

        // --- 1. 베이스 (기초) ---
        CreatePrimitive(PrimitiveType.Cube, "Base", craneRoot.transform,
            new Vector3(0, 0.5f, 0), Vector3.zero,
            new Vector3(mastWidth * 2.5f, 1, mastWidth * 2.5f),
            darkGrayMaterial);

        // --- 2. 타워 마스트 (격자 구조) ---
        GameObject mastRoot = new GameObject("Mast");
        mastRoot.transform.SetParent(craneRoot.transform);
        mastRoot.transform.localPosition = new Vector3(0, 1, 0);

        for (int i = 0; i < mastSections; i++)
        {
            Vector3 sectionPos = new Vector3(0, i * mastSectionHeight, 0);
            CreateMastSection(mastRoot.transform, sectionPos, mastWidth, mastSectionHeight, pillarSize);
        }

        // --- 3. 상부 선회체 (회전부) ---
        float topHeight = 1 + (mastSections * mastSectionHeight);
        GameObject slewingUnit = new GameObject("SlewingUnit");
        slewingUnit.transform.SetParent(craneRoot.transform);
        slewingUnit.transform.localPosition = new Vector3(0, topHeight, 0);

        // 턴테이블
        CreatePrimitive(PrimitiveType.Cylinder, "Turntable", slewingUnit.transform,
            new Vector3(0, 0.25f, 0), Vector3.zero,
            new Vector3(mastWidth, 0.5f, mastWidth), darkGrayMaterial);

        // --- 4. A-프레임 (King Post / 지브 지지삭) ---
        GameObject aFrame = new GameObject("A-Frame");
        aFrame.transform.SetParent(slewingUnit.transform);
        aFrame.transform.localPosition = Vector3.zero;

        CreatePrimitive(PrimitiveType.Cube, "KingPost", aFrame.transform,
            new Vector3(0, kingPostHeight / 2, 0), Vector3.zero,
            new Vector3(0.5f, kingPostHeight, 0.5f), craneRedMaterial);

        // --- 5. 지브 (Jib, 메인 팔) ---
        GameObject jib = new GameObject("Jib");
        jib.transform.SetParent(slewingUnit.transform);
        jib.transform.localPosition = new Vector3(0, 0, 0);
        CreateTrussBoom(jib.transform, jibLength, jibTrussHeight, mastWidth, true);

        // --- 6. 카운터 지브 (Counter Jib) ---
        GameObject counterJib = new GameObject("CounterJib");
        counterJib.transform.SetParent(slewingUnit.transform);
        counterJib.transform.localPosition = new Vector3(0, 0, 0);
        CreateTrussBoom(counterJib.transform, -counterJibLength, jibTrussHeight, mastWidth, false);

        // --- 7. 지지 케이블 (Jib Ties) ---
        Vector3 kingPostTop = new Vector3(0, kingPostHeight, 0);
        // 지브 지지 케이블
        CreateCable(aFrame.transform, "JibTie_1", kingPostTop, new Vector3(0.5f, 0, jibLength * 0.7f));
        CreateCable(aFrame.transform, "JibTie_2", kingPostTop, new Vector3(-0.5f, 0, jibLength * 0.7f));
        // 카운터 지브 지지 케이블
        CreateCable(aFrame.transform, "CounterTie_1", kingPostTop, new Vector3(0.5f, 0, -counterJibLength));
        CreateCable(aFrame.transform, "CounterTie_2", kingPostTop, new Vector3(-0.5f, 0, -counterJibLength));

        // --- 8. 평형추 (Counterweight) ---
        CreatePrimitive(PrimitiveType.Cube, "Counterweight", counterJib.transform,
            new Vector3(0, -0.5f, -counterJibLength + 1f), Vector3.zero,
            new Vector3(2.5f, 2.5f, 2.5f), darkGrayMaterial);

        // --- 9. 운전실 (Cabin) ---
        CreatePrimitive(PrimitiveType.Cube, "Cabin", slewingUnit.transform,
             new Vector3(mastWidth / 2 + 0.75f, -1.0f, 1.5f), Vector3.zero,
             new Vector3(1.5f, 1.5f, 1.5f), darkGrayMaterial);

        // --- 10. 트롤리 & 후크 (Trolley & Hook) ---
        GameObject trolley = CreatePrimitive(PrimitiveType.Cube, "Trolley", jib.transform,
            new Vector3(0, -jibTrussHeight + 0.1f, 15f), // 지브 하단에 위치
            Vector3.zero, new Vector3(0.5f, 0.3f, 0.5f), darkGrayMaterial);

        GameObject hookAssembly = new GameObject("HookAssembly");
        hookAssembly.transform.SetParent(trolley.transform);
        hookAssembly.transform.localPosition = Vector3.zero;

        // 로프
        CreatePrimitive(PrimitiveType.Cylinder, "HoistingRope", hookAssembly.transform,
            new Vector3(0, -ropeLength / 2, 0), Vector3.zero,
            new Vector3(0.1f, ropeLength, 0.1f), cableMaterial);

        // 호이스팅 블록
        GameObject hookBlock = CreatePrimitive(PrimitiveType.Cube, "HookBlock", hookAssembly.transform,
            new Vector3(0, -ropeLength - 0.25f, 0), Vector3.zero,
            new Vector3(0.7f, 0.5f, 0.7f), darkGrayMaterial);

        // 갈고리 (디테일)
        CreatePrimitive(PrimitiveType.Cube, "Hook_1", hookBlock.transform, new Vector3(0, -0.5f, 0.3f), Vector3.zero, new Vector3(0.2f, 0.6f, 0.2f), darkGrayMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Hook_2", hookBlock.transform, new Vector3(0, -0.8f, 0.1f), Vector3.zero, new Vector3(0.2f, 0.2f, 0.6f), darkGrayMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Hook_3", hookBlock.transform, new Vector3(0, -0.5f, -0.1f), Vector3.zero, new Vector3(0.2f, 0.6f, 0.2f), darkGrayMaterial);

        Selection.activeObject = craneRoot;
    }

    // (이하 헬퍼 함수들은 이전과 동일합니다)

    private static void CreateMastSection(Transform parent, Vector3 position, float width, float height, float pillarSize)
    {
        GameObject sectionRoot = new GameObject("MastSection");
        sectionRoot.transform.SetParent(parent);
        sectionRoot.transform.localPosition = position;

        float w = width / 2;
        float h = height;

        CreatePrimitive(PrimitiveType.Cube, "Pillar_FL", sectionRoot.transform, new Vector3(-w, h / 2, -w), Vector3.zero, new Vector3(pillarSize, h, pillarSize), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Pillar_FR", sectionRoot.transform, new Vector3(w, h / 2, -w), Vector3.zero, new Vector3(pillarSize, h, pillarSize), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Pillar_BL", sectionRoot.transform, new Vector3(-w, h / 2, w), Vector3.zero, new Vector3(pillarSize, h, pillarSize), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Pillar_BR", sectionRoot.transform, new Vector3(w, h / 2, w), Vector3.zero, new Vector3(pillarSize, h, pillarSize), craneRedMaterial);

        CreatePrimitive(PrimitiveType.Cube, "Brace_Top_F", sectionRoot.transform, new Vector3(0, h - pillarSize / 2, -w), Vector3.zero, new Vector3(width, pillarSize, pillarSize), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Top_B", sectionRoot.transform, new Vector3(0, h - pillarSize / 2, w), Vector3.zero, new Vector3(width, pillarSize, pillarSize), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Top_L", sectionRoot.transform, new Vector3(-w, h - pillarSize / 2, 0), Vector3.zero, new Vector3(pillarSize, pillarSize, width), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Top_R", sectionRoot.transform, new Vector3(w, h - pillarSize / 2, 0), Vector3.zero, new Vector3(pillarSize, pillarSize, width), craneRedMaterial);

        float braceLen = Mathf.Sqrt(width * width + height * height);
        float angle = Mathf.Atan(height / width) * Mathf.Rad2Deg;
        CreatePrimitive(PrimitiveType.Cube, "Brace_Diag_F1", sectionRoot.transform, new Vector3(0, h / 2, -w), new Vector3(0, 0, angle), new Vector3(pillarSize * 0.7f, braceLen, pillarSize * 0.7f), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Diag_F2", sectionRoot.transform, new Vector3(0, h / 2, -w), new Vector3(0, 0, -angle), new Vector3(pillarSize * 0.7f, braceLen, pillarSize * 0.7f), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Diag_L1", sectionRoot.transform, new Vector3(-w, h / 2, 0), new Vector3(angle, 0, 0), new Vector3(pillarSize * 0.7f, pillarSize * 0.7f, braceLen), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Brace_Diag_L2", sectionRoot.transform, new Vector3(-w, h / 2, 0), new Vector3(-angle, 0, 0), new Vector3(pillarSize * 0.7f, pillarSize * 0.7f, braceLen), craneRedMaterial);
    }

    private static void CreateTrussBoom(Transform parent, float length, float height, float width, bool isJib)
    {
        float dir = Mathf.Sign(length);
        float boomPillarSize = 0.3f;
        float braceSize = 0.1f;
        float segmentLength = 2.5f;
        int segments = (int)(Mathf.Abs(length) / segmentLength);

        CreatePrimitive(PrimitiveType.Cube, "TopBoom", parent, new Vector3(0, 0, length / 2), Vector3.zero, new Vector3(boomPillarSize * 1.5f, boomPillarSize * 1.5f, Mathf.Abs(length)), craneRedMaterial);

        float bottomWidth = width / 2;
        CreatePrimitive(PrimitiveType.Cube, "BottomBoom_L", parent, new Vector3(-bottomWidth, -height, length / 2), Vector3.zero, new Vector3(boomPillarSize, boomPillarSize, Mathf.Abs(length)), craneRedMaterial);
        CreatePrimitive(PrimitiveType.Cube, "BottomBoom_R", parent, new Vector3(bottomWidth, -height, length / 2), Vector3.zero, new Vector3(boomPillarSize, boomPillarSize, Mathf.Abs(length)), craneRedMaterial);

        for (int i = 0; i < segments; i++)
        {
            float z = (i + 0.5f) * segmentLength * dir;
            CreatePrimitive(PrimitiveType.Cube, $"V_Brace_{i}_L", parent, new Vector3(-bottomWidth, -height / 2, z), Vector3.zero, new Vector3(braceSize, height, braceSize), craneRedMaterial);
            CreatePrimitive(PrimitiveType.Cube, $"V_Brace_{i}_R", parent, new Vector3(bottomWidth, -height / 2, z), Vector3.zero, new Vector3(braceSize, height, braceSize), craneRedMaterial);
            float diagLen = Mathf.Sqrt(segmentLength * segmentLength + height * height);
            float angle = Mathf.Atan(height / segmentLength) * Mathf.Rad2Deg;
            CreatePrimitive(PrimitiveType.Cube, $"D_Brace_T_{i}", parent, new Vector3(0, -height / 2, z), new Vector3(angle, 0, 0), new Vector3(braceSize, braceSize, diagLen), craneRedMaterial);
        }
    }

    private static void CreateCable(Transform parent, string name, Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) / 2;
        float length = Vector3.Distance(start, end);
        Quaternion rotation = Quaternion.LookRotation(end - start);

        GameObject cable = CreatePrimitive(PrimitiveType.Cylinder, name, parent,
            midPoint, rotation.eulerAngles + new Vector3(90, 0, 0),
            new Vector3(0.1f, length / 2, 0.1f),
            cableMaterial);
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localEulerAngles = rotation;
        obj.transform.localScale = scale;

        Renderer rend = obj.GetComponent<Renderer>();
        rend.material = material;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Object.DestroyImmediate(obj.GetComponent<Collider>());

        return obj;
    }
}