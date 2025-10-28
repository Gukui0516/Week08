using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 키보드 하위 모든 키캡의 매터리얼을 일괄 적용합니다.
/// </summary>
public class KeyboardMaterialApplier : MonoBehaviour
{
    #region Serialized Fields
    [Header("Keycap Materials")]
    [SerializeField] private Material _wallMaterial;
    [SerializeField] private Material _stemMaterial;
    [SerializeField] private Material _surfaceMaterial;

    [Button("Apply Materials", ButtonSizes.Large), GUIColor("cyan")]
    private void ApplyMaterialsButton() => ApplyMaterials();
    #endregion

    #region Properties
    public Material WallMaterial => _wallMaterial;
    public Material StemMaterial => _stemMaterial;
    public Material SurfaceMaterial => _surfaceMaterial;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }
    #endregion

    #region Initialization and Clear
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        // 초기화 시 자동 적용하지 않음 (사용자가 수동으로 호출)
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        // 필요 시 정리 작업
    }
    #endregion

    #region Public Methods - Material Application
    /// <summary>
    /// 하위 모든 키캡에 매터리얼을 적용합니다.
    /// </summary>
    public void ApplyMaterials()
    {
        if (_wallMaterial == null || _stemMaterial == null || _surfaceMaterial == null)
        {
            Debug.LogWarning($"<color=yellow>[{GetType().Name}]</color> Some materials are not assigned.", this);
            return;
        }

        ApplyMaterialsRecursive(transform);
        Debug.Log($"<color=cyan>[{GetType().Name}]</color> Materials applied to all keycaps.", this);
    }
    #endregion

    #region Private Methods - Recursive Search
    /// <summary>
    /// 재귀적으로 자식 오브젝트를 탐색하여 매터리얼을 적용합니다.
    /// </summary>
    /// <param name="targetTransform">탐색할 Transform</param>
    private void ApplyMaterialsRecursive(Transform targetTransform)
    {
        MeshRenderer renderer = targetTransform.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            string objectName = targetTransform.name;

            if (objectName.StartsWith("Wall_"))
            {
                renderer.sharedMaterial = _wallMaterial;
            }
            else if (objectName == "Stem")
            {
                renderer.sharedMaterial = _stemMaterial;
            }
            else if (objectName == "TopSurface")
            {
                renderer.sharedMaterial = _surfaceMaterial;
            }
        }

        // 자식 오브젝트 재귀 탐색
        foreach (Transform child in targetTransform)
        {
            ApplyMaterialsRecursive(child);
        }
    }
    #endregion
}