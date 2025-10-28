// StageUIManager.cs (Step 3 리팩터링 완료 + 주석 코드 복원)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageUIManager : MonoBehaviour
{
    #region Serialized Fields
    // --- [사용되지 않음] 기존 2D UI 필드 ---
    [Header("!! [사용되지 않음] !!")]
    [SerializeField] private GameObject _stagePrefab; // (더 이상 사용되지 않음)
    [SerializeField] private GameObject _stageTarget; // (더 이상 사용되지 않음)
    // ------------------------------------

    [Header("스테이지 데이터")]
    [SerializeField] private StageGroupSO _stageGroupSO;
    [SerializeField] private Button _exitBtn;

    [Header("Hidden Stage Lock")]
    [SerializeField] private GameObject _hiddenStageLock;  // Scene의 3D 가림막 오브젝트
    [SerializeField] private string _hiddenStageName = "JMKey";
    [SerializeField] private GameObject _unlockObject1;  // 해금 시 활성화할 오브젝트 1
    [SerializeField] private GameObject _unlockObject2;  // 해금 시 활성화할 오브젝트 2

    [Header("Hidden Object Monitoring")]
    [SerializeField] private GameObject _hiddenObj2;  // HiddenObj_2 오브젝트
    [SerializeField] private float positionChangeThreshold = 1.0f;  // 위치 변화 감지 임계값 (1cm)

    [Header("Debug Settings")]
    [SerializeField] private float starFillDelay = 0.3f;  // 별 채우기 간격 (초)
    #endregion

    #region Private Fields
    private List<StageDataSO> stageDataSOs = new();

    // --- 3D 상호작용용 ---
    private Camera _mainCamera; // Raycast를 위한 카메라

    #endregion

    #region Initialize Methods

    // StageGroupSO 호출
    void OnEnable()
    {
        stageDataSOs = _stageGroupSO.stages;
        StageSaveManager.Load(stageDataSOs);
        _exitBtn.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        _exitBtn.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        // 3D 클릭(Raycast)을 위해 메인 카메라 할당
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[StageUIManager] 씬에 'MainCamera' 태그가 달린 카메라가 없습니다!");
        }
    }

    private void Update()
    {
        // 매 프레임 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }
    #endregion

    #region Private Methods

    /// <summary>
    /// ★ [신규] 3D 오브젝트 클릭을 감지하는 Raycast 메서드
    /// </summary>
    private void HandleClick()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(CursorManager.Instance.CursorPosition);

        // 2D UI (예: Exit 버튼)를 클릭했다면 3D Raycast 무시
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[StageUIManager] 2D UI 클릭 감지 - 3D Raycast 무시");
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 레이캐스트 히트 결과 디버깅용 그리기
            Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.blue, 2.0f); // 히트 포인트에서 노멀 방향으로 파란색 레이
            Debug.DrawRay(hit.point, Vector3.up * 0.1f, Color.green, 2.0f); // 히트 포인트에 녹색 마커
            Debug.DrawRay(ray.origin, hit.point - ray.origin, Color.red, 2.0f); // 레이캐스트 레이 라인 (시작점에서 히트 포인트까지)

            // Raycast에 맞은 오브젝트와 그 자식들에서 WorldStageObject 컴포넌트 재귀적으로 탐색
            CheckForWorldStageObject(hit.collider.gameObject);
        }
    }

    /// <summary>
    /// 재귀적으로 오브젝트와 그 자식들에서 WorldStageObject 컴포넌트를 검사하고 스테이지 선택 실행
    /// </summary>
    /// <param name="obj">검사할 오브젝트</param>
    private void CheckForWorldStageObject(GameObject obj)
    {
        // 현재 오브젝트에 WorldStageObject 컴포넌트가 있는지 검사
        WorldStageObject stageObject = obj.GetComponent<WorldStageObject>();
        if (stageObject != null)
        {
            Debug.Log($"[StageUIManager] WorldStageObject 컴포넌트 발견 - 스테이지 선택 실행: {obj.name}");
            stageObject.SelectStage();
            return; // 하나 찾으면 중지
        }

        // 자식 오브젝트들을 재귀적으로 검사
        foreach (Transform child in obj.transform)
        {
            CheckForWorldStageObject(child.gameObject);
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

}