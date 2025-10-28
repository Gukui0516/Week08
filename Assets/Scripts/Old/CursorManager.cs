using UnityEngine;
using UnityEngine.UI;

// Manages the visual custom cursor, including a manual positioning mode.
public class CursorManager : SingletonObject<CursorManager>
{
    [SerializeField] private Sprite cursorSprite;
    [SerializeField] private Sprite grabSprite;
    [SerializeField] private Sprite forwardWheelSprite;
    [SerializeField] private Sprite backWheelSprite;
    [SerializeField] private Gradient stressGradient;

    [SerializeField] private RectTransform cursorUITransform;
    [SerializeField] private Image cursorUIImage;
    private Vector2 cursorUIImageOriginalPosition;
    public float manualMoveSpeed = 15f;
    [SerializeField] private float stressDecayRate = 1f;
    public Vector2 CursorPosition;

    private float currentStress = 0f;
    public bool isGrabbed = false;

    // --- 추가된 변수들 ---
    private Vector3 trackedWorldPoint;
    private Camera mainCamera;
    // ---

    protected override void Awake()
    {
        base.Awake();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = Camera.main; // <<< 추가: Camera.main을 캐싱하여 성능 향상
        cursorUIImageOriginalPosition = cursorUIImage.transform.localPosition;
        SetCursorToDefault();
        cursorUIImage.color = Color.white;
        currentStress = 0f;
        isGrabbed = false;
    }

    void Update()
    {
        if (!cursorUITransform.gameObject.activeInHierarchy) return;

        if (isGrabbed)
        {
            trackedWorldPoint = PhysicsDrag.Instance.currentGrabPoint;
            PhysicsDrag.Instance.UpdateGrabDistance();

            // 월드 좌표 추적 모드: 3D 포인트를 화면 좌표로 변환하여 커서 위치를 업데이트합니다.
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(trackedWorldPoint);

            // 오브젝트가 카메라 뒤로 가면 z값이 음수가 되어 좌표가 뒤집히는 현상 방지
            if (screenPoint.z > 0)
            {
                cursorUITransform.position = screenPoint;
            }


        }


        if (!isGrabbed || !Input.GetMouseButton(1))
        {
            // 기존의 수동 조작 모드: 마우스 움직임으로 커서를 이동시킵니다.
            Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * manualMoveSpeed;
            cursorUITransform.position += new Vector3(delta.x, delta.y, 0);
        }

        ClampCursorToScreen();
        CursorPosition = cursorUITransform.position;

        currentStress -= stressDecayRate * Time.deltaTime;
        if (currentStress < 0f)
            currentStress = 0f;
        Color stressColor = stressGradient.Evaluate(currentStress);
        cursorUIImage.color = stressColor;

        if (isGrabbed)
        {
            // if (!Input.GetMouseButton(1))
            // {
            PhysicsDrag.Instance.UpdateDistance();
            PhysicsDrag.Instance.Drag();
            // }
        }


    }

    // --- Public Methods for other scripts to call ---

    public void ShowCursor()
    {

        cursorUITransform.gameObject.SetActive(true);
    }

    public void HideCursor()
    {
        cursorUITransform.gameObject.SetActive(false);
    }

    public void SetCursorToDefault()
    {
        isGrabbed = false;
        cursorUIImage.sprite = cursorSprite;
        cursorUIImage.transform.localPosition = cursorUIImageOriginalPosition;
    }
    public void SetCursorToGrab()
    {
        isGrabbed = true;
        cursorUIImage.sprite = grabSprite;
    }

    public void SetCursorColor(float stress)
    {
        if (stress > currentStress)
            currentStress = stress;
    }

    public void SetCursorToForwardWheel()
    {
        cursorUIImage.sprite = forwardWheelSprite;
    }

    public void SetCursorToBackWheel()
    {
        cursorUIImage.sprite = backWheelSprite;
    }

    public void SetCursorUIImagePosition(float speed)
    {
        float modifiedSpeed = speed * 2f;
        cursorUIImage.transform.localPosition = new Vector2(cursorUIImageOriginalPosition.x, cursorUIImageOriginalPosition.y + modifiedSpeed);
    }

    // --- Helper Method ---

    private void ClampCursorToScreen()
    {
        Vector3 pos = cursorUITransform.position;
        pos.x = Mathf.Clamp(pos.x, 0, Screen.width);
        pos.y = Mathf.Clamp(pos.y, 0, Screen.height);
        cursorUITransform.position = pos;
    }

    public bool CheckAndClampCursorPosition()
    {
        Vector3 originalPos = cursorUITransform.position;
        Vector3 clampedPos = originalPos;
        clampedPos.x = Mathf.Clamp(originalPos.x, 0, Screen.width);
        clampedPos.y = Mathf.Clamp(originalPos.y, 0, Screen.height);

        bool wasClamped = (originalPos.x != clampedPos.x || originalPos.y != clampedPos.y);

        return wasClamped;
    }

    // --- 추가된 Public 메서드 ---

    /// <summary>
    /// 지정된 월드 좌표를 커서가 추적하도록 시작합니다.
    /// </summary>
    // public void StartTrackingWorldPoint(Vector3 worldPoint)
    // {
    //     trackedWorldPoint = worldPoint;
    //     ShowCursor(); // 추적 중에는 커서가 항상 보이도록 합니다.
    // }

    /// <summary>
    /// 월드 좌표 추적을 멈추고 기본 수동 조작 모드로 돌아갑니다.
    /// </summary>
    public void StopTracking()
    {
    }
}