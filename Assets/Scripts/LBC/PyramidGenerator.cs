using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// (1, 1, 2) 크기의 큐브 블록으로 피라미드를 자동 생성하는 스크립트입니다.
/// Inspector에서 다양한 설정을 조정하여 원하는 형태의 피라미드를 만들 수 있습니다.
/// </summary>
public class PyramidGenerator : MonoBehaviour
{
    #region Serialized Fields

    [Header("Pyramid Structure Settings")]
    [Tooltip("피라미드의 층수입니다. 높을수록 더 큰 피라미드가 생성됩니다.")]
    [SerializeField] private int pyramidHeight = 5;

    [Tooltip("각 층의 기본 블록 개수입니다. (가장 아래층 기준)")]
    [SerializeField] private int baseBlockCount = 5;

    [Header("Block Settings")]
    [Tooltip("블록의 크기입니다. 기본값 (1, 1, 2)")]
    [SerializeField] private Vector3 blockSize = new Vector3(1f, 1f, 2f);

    [Tooltip("블록 간의 간격입니다. 0이면 딱 붙어서 생성됩니다.")]
    [SerializeField] private float blockSpacing = 0.05f;

    [Tooltip("블록에 적용할 머티리얼입니다. 비어있으면 기본 머티리얼을 사용합니다.")]
    [SerializeField] private Material blockMaterial;

    [Header("Block Rotation")]
    [Tooltip("블록의 회전 방향을 층마다 교차시킬지 여부입니다.")]
    [SerializeField] private bool alternateRotation = true;

    [Tooltip("첫 번째 층의 회전 각도입니다. (Y축 기준)")]
    [SerializeField] private float initialRotation = 0f;

    [Header("Physics Settings")]
    [Tooltip("생성된 블록에 Rigidbody를 추가합니다.")]
    [SerializeField] private bool addRigidbody = true;

    [Tooltip("Rigidbody의 질량입니다.")]
    [SerializeField] private float blockMass = 1f;

    [Tooltip("Rigidbody의 선형 감쇠(Linear Damping) 값입니다.")]
    [SerializeField] private float linearDamping = 0.05f;

    [Tooltip("Rigidbody의 각운동 감쇠(Angular Damping) 값입니다.")]
    [SerializeField] private float angularDamping = 0.05f;

    [Header("Tag and Layer Settings")]
    [Tooltip("생성된 블록에 할당할 태그입니다.")]
    [SerializeField] private string blockTag = "Draggable";

    [Tooltip("생성된 블록에 할당할 레이어입니다.")]
    [SerializeField] private int blockLayer = 0;

    [Header("Container Settings")]
    [Tooltip("생성된 블록들을 담을 부모 오브젝트입니다. 비어있으면 자동으로 생성합니다.")]
    [SerializeField] private Transform blockContainer;

    [Tooltip("자동 생성할 컨테이너의 이름입니다.")]
    [SerializeField] private string containerName = "PyramidBlocks";

    [Header("Auto Generation")]
    [Tooltip("게임 시작 시 자동으로 피라미드를 생성합니다.")]
    [SerializeField] private bool generateOnStart = false;

    #endregion

    #region Private Fields

    /// <summary>생성된 블록들의 개수</summary>
    private int generatedBlockCount = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // 게임 시작 시 자동 생성 옵션이 활성화되어 있으면 피라미드 생성
        if (generateOnStart)
        {
            GeneratePyramid();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 피라미드를 생성하는 메인 메서드입니다.
    /// Inspector 버튼이나 외부 스크립트에서 호출할 수 있습니다.
    /// </summary>
    public void GeneratePyramid()
    {
        // 이전에 생성된 피라미드 제거
        ClearPyramid();

        // 컨테이너 설정
        SetupContainer();

        // 블록 카운트 초기화
        generatedBlockCount = 0;

        // 각 층을 아래부터 위로 생성
        for (int layer = 0; layer < pyramidHeight; layer++)
        {
            GenerateLayer(layer);
        }

        Debug.Log($"[PyramidGenerator] 피라미드 생성 완료! 총 {generatedBlockCount}개의 블록이 생성되었습니다.");
    }

    /// <summary>
    /// 현재 생성된 피라미드를 제거하는 메서드입니다.
    /// </summary>
    public void ClearPyramid()
    {
        if (blockContainer != null)
        {
            // 에디터 모드와 플레이 모드에 따라 다르게 처리
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(blockContainer.gameObject);
            }
            else
            {
                DestroyImmediate(blockContainer.gameObject);
            }
#else
            Destroy(blockContainer.gameObject);
#endif
            blockContainer = null;
        }

        generatedBlockCount = 0;
        Debug.Log("[PyramidGenerator] 피라미드가 제거되었습니다.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 블록 컨테이너를 설정합니다.
    /// 컨테이너가 없으면 새로 생성합니다.
    /// </summary>
    private void SetupContainer()
    {
        if (blockContainer == null)
        {
            GameObject containerObj = new GameObject(containerName);
            blockContainer = containerObj.transform;
            blockContainer.position = transform.position;
        }
    }

    /// <summary>
    /// 특정 층의 블록들을 생성합니다.
    /// 사각 피라미드 형태로 각 층을 사방으로 배치합니다.
    /// </summary>
    /// <param name="layer">생성할 층의 인덱스 (0부터 시작)</param>
    private void GenerateLayer(int layer)
    {
        // 현재 층의 한 변당 블록 개수 (위로 갈수록 감소)
        int blocksPerSide = baseBlockCount - layer;

        // 블록이 0개 이하면 생성하지 않음
        if (blocksPerSide <= 0)
        {
            return;
        }

        // 현재 층의 Y 위치 계산
        float yPosition = layer * (blockSize.y + blockSpacing);

        // 현재 층의 회전 각도 계산
        float currentRotation = initialRotation;
        if (alternateRotation && layer % 2 == 1)
        {
            currentRotation += 90f;
        }

        // 2차원 그리드로 블록 배치 (정사각형 형태)
        for (int x = 0; x < blocksPerSide; x++)
        {
            for (int z = 0; z < blocksPerSide; z++)
            {
                Vector3 blockPosition = CalculateBlockPosition(layer, x, z, blocksPerSide, currentRotation, yPosition);
                Quaternion blockRotation = Quaternion.Euler(0f, currentRotation, 0f);

                CreateBlock(blockPosition, blockRotation, layer, x * blocksPerSide + z);
            }
        }
    }

    /// <summary>
    /// 개별 블록의 위치를 계산합니다.
    /// 2차원 그리드 상의 위치를 기반으로 계산합니다.
    /// </summary>
    /// <param name="layer">층 인덱스</param>
    /// <param name="xIndex">X축 인덱스</param>
    /// <param name="zIndex">Z축 인덱스</param>
    /// <param name="blocksPerSide">한 변당 블록 개수</param>
    /// <param name="rotation">회전 각도</param>
    /// <param name="yPosition">Y 위치</param>
    /// <returns>블록의 월드 위치</returns>
    private Vector3 CalculateBlockPosition(int layer, int xIndex, int zIndex, int blocksPerSide,
        float rotation, float yPosition)
    {
        // 회전 각도에 따라 블록의 너비와 깊이 결정
        bool isRotated = Mathf.Abs(Mathf.Sin(rotation * Mathf.Deg2Rad)) > 0.5f;
        float blockWidth = isRotated ? blockSize.z : blockSize.x;
        float blockDepth = isRotated ? blockSize.x : blockSize.z;

        // 그리드의 전체 크기 계산
        float totalWidth = blocksPerSide * blockWidth + (blocksPerSide - 1) * blockSpacing;
        float totalDepth = blocksPerSide * blockDepth + (blocksPerSide - 1) * blockSpacing;

        // 중심을 기준으로 오프셋 계산
        float xOffset = -totalWidth / 2f + xIndex * (blockWidth + blockSpacing) + blockWidth / 2f;
        float zOffset = -totalDepth / 2f + zIndex * (blockDepth + blockSpacing) + blockDepth / 2f;

        // 로컬 위치 설정
        Vector3 localPosition = new Vector3(xOffset, yPosition, zOffset);

        // 회전 적용
        Vector3 rotatedPosition = Quaternion.Euler(0f, rotation, 0f) * localPosition;

        // 피라미드 생성기의 위치를 기준으로 최종 위치 결정
        return transform.position + rotatedPosition;
    }

    /// <summary>
    /// 개별 블록을 생성하고 설정을 적용합니다.
    /// </summary>
    /// <param name="position">블록의 위치</param>
    /// <param name="rotation">블록의 회전</param>
    /// <param name="layer">층 인덱스</param>
    /// <param name="blockIndex">층 내 블록 인덱스</param>
    private void CreateBlock(Vector3 position, Quaternion rotation, int layer, int blockIndex)
    {
        // 프리미티브 큐브 생성
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = $"Block_L{layer}_B{blockIndex}";
        block.transform.position = position;
        block.transform.rotation = rotation;
        block.transform.localScale = blockSize;
        block.transform.SetParent(blockContainer);

        // 머티리얼 적용
        if (blockMaterial != null)
        {
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = blockMaterial;
            }
        }

        // Rigidbody 추가
        if (addRigidbody)
        {
            Rigidbody rb = block.AddComponent<Rigidbody>();
            rb.mass = blockMass;
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // 태그 설정 (태그가 존재하는 경우만)
        if (!string.IsNullOrEmpty(blockTag) && IsTagValid(blockTag))
        {
            block.tag = blockTag;
        }

        // 레이어 설정
        block.layer = blockLayer;

        // 생성된 블록 카운트 증가
        generatedBlockCount++;
    }

    /// <summary>
    /// 태그가 유효한지 확인합니다.
    /// </summary>
    /// <param name="tag">확인할 태그</param>
    /// <returns>태그가 존재하면 true, 아니면 false</returns>
    private bool IsTagValid(string tag)
    {
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            Debug.LogWarning($"[PyramidGenerator] '{tag}' 태그가 Tag Manager에 등록되어 있지 않습니다.");
            return false;
        }
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 유효성 검사를 수행합니다.
    /// </summary>
    private void OnValidate()
    {
        // 음수 값 방지
        if (pyramidHeight < 1)
            pyramidHeight = 1;

        if (baseBlockCount < 1)
            baseBlockCount = 1;

        if (blockMass < 0.01f)
            blockMass = 0.01f;

        if (linearDamping < 0f)
            linearDamping = 0f;

        if (angularDamping < 0f)
            angularDamping = 0f;

        // 블록 크기가 너무 작지 않도록
        if (blockSize.x < 0.1f) blockSize.x = 0.1f;
        if (blockSize.y < 0.1f) blockSize.y = 0.1f;
        if (blockSize.z < 0.1f) blockSize.z = 0.1f;
    }

    /// <summary>
    /// Gizmo를 그려서 피라미드가 생성될 위치를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

        // 피라미드 중심점 표시
        Gizmos.DrawSphere(transform.position, 0.5f);

        // 피라미드 대략적인 범위 표시
        float maxWidth = baseBlockCount * Mathf.Max(blockSize.x, blockSize.z);
        float height = pyramidHeight * blockSize.y;

        Gizmos.DrawWireCube(
            transform.position + Vector3.up * height / 2f,
            new Vector3(maxWidth, height, maxWidth)
        );
    }
#endif

    #endregion
}

#region Custom Editor

#if UNITY_EDITOR
/// <summary>
/// PyramidGenerator의 커스텀 에디터입니다.
/// Inspector에 버튼을 추가하여 쉽게 피라미드를 생성/제거할 수 있습니다.
/// </summary>
[CustomEditor(typeof(PyramidGenerator))]
public class PyramidGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 그리기
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        PyramidGenerator generator = (PyramidGenerator)target;

        // 피라미드 생성 버튼
        if (GUILayout.Button("Generate Pyramid", GUILayout.Height(40)))
        {
            generator.GeneratePyramid();
        }

        EditorGUILayout.Space(5);

        // 피라미드 제거 버튼
        if (GUILayout.Button("Clear Pyramid", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Pyramid",
                "현재 생성된 피라미드를 제거하시겠습니까?",
                "Yes",
                "No"))
            {
                generator.ClearPyramid();
            }
        }

        EditorGUILayout.Space(10);

        // 도움말 표시
        EditorGUILayout.HelpBox(
            "Generate Pyramid: 설정에 따라 피라미드를 생성합니다.\n" +
            "Clear Pyramid: 현재 생성된 피라미드를 제거합니다.\n\n" +
            "TIP: Alternate Rotation을 활성화하면 층마다 블록 방향이 교차됩니다.",
            MessageType.Info);
    }
}
#endif

#endregion