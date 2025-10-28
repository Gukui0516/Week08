using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 목표 텍스트 UI를 관리하는 클래스
/// 스테이지 시작 시 중앙에 목표를 표시하고, 일정 시간 후 지정된 위치로 이동시킵니다.
/// </summary>
public class StageGoalUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [Tooltip("목표 텍스트를 표시할 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI goalText;

    [Tooltip("페이드 효과를 위한 CanvasGroup (선택사항)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [Tooltip("중앙에 목표가 표시되는 시간 (초)")]
    [SerializeField] private float displayDuration = 1f;

    [Tooltip("목표 위치로 이동하는 애니메이션 시간 (초)")]
    [SerializeField] private float moveDuration = 0.5f;

    [Tooltip("페이드 인 시간 (초)")]
    [SerializeField] private float fadeInDuration = 0.3f;

    [Header("Target Position")]
    [Tooltip("목표가 최종적으로 위치할 앵커 위치 (0~1 범위, 0.5는 중앙)")]
    [SerializeField] private Vector2 targetAnchorMin = new Vector2(0.7f, 0.85f);

    [Tooltip("목표가 최종적으로 위치할 앵커 위치 (0~1 범위, 0.5는 중앙)")]
    [SerializeField] private Vector2 targetAnchorMax = new Vector2(0.95f, 0.95f);

    [Tooltip("최종 위치에서의 텍스트 크기 배율 (1이 원본 크기)")]
    [SerializeField] private float targetScale = 0.5f;

    [Header("Animation Curve")]
    [Tooltip("이동 애니메이션의 easing 커브")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Private Fields

    private RectTransform rectTransform;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector3 originalScale;
    private bool isAnimating = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 컴포넌트 초기화
    /// RectTransform과 CanvasGroup 컴포넌트를 가져옵니다.
    /// </summary>
    private void Awake()
    {
        Debug.Log("StageGoalUI Awake");

        rectTransform = GetComponent<RectTransform>();

        // CanvasGroup이 없으면 자동으로 추가
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 원본 값 저장
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalScale = rectTransform.localScale;

        gameObject.SetActive(false);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 목표 텍스트를 설정하고 애니메이션을 시작합니다.
    /// </summary>
    /// <param name="goalTextContent">표시할 목표 텍스트 내용</param>
    public void ShowGoal()
    {
        Debug.Log("StageGoalUI ShowGoalUI");

        if (isAnimating)
        {
            Debug.LogWarning("[StageGoalUI] 이미 애니메이션이 진행 중입니다.");
            return;
        }

        gameObject.SetActive(true);

        StartCoroutine(GoalAnimationSequence());
    }

    /// <summary>
    /// 진행 중인 애니메이션을 즉시 중단하고 최종 상태로 이동합니다.
    /// </summary>
    public void SkipAnimation()
    {
        if (!isAnimating) return;

        StopAllCoroutines();
        isAnimating = false;

        // 최종 상태로 즉시 이동
        rectTransform.anchorMin = targetAnchorMin;
        rectTransform.anchorMax = targetAnchorMax;
        rectTransform.localScale = originalScale * targetScale;
        canvasGroup.alpha = 1f;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 목표 UI 애니메이션 시퀀스를 실행합니다.
    /// 1. 페이드 인
    /// 2. 중앙에 일정 시간 표시
    /// 3. 목표 위치로 이동 및 스케일 조정
    /// </summary>
    private IEnumerator GoalAnimationSequence()
    {
        Debug.Log("StageGoalUI GoalAnimationSequence");

        isAnimating = true;

        // 초기 위치 설정 (화면 중앙)
        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.localScale = originalScale;

        // 2단계: 중앙에 표시
        yield return new WaitForSeconds(displayDuration);

        // 3단계: 목표 위치로 이동 및 축소
        yield return StartCoroutine(MoveToTarget());

        isAnimating = false;
    }

    /// <summary>
    /// UI를 서서히 나타나게 하는 페이드 인 효과
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// UI를 목표 위치로 이동시키고 크기를 조정합니다.
    /// AnimationCurve를 사용하여 부드러운 이동 효과를 구현합니다.
    /// </summary>
    private IEnumerator MoveToTarget()
    {
        float elapsed = 0f;

        Vector2 startAnchorMin = rectTransform.anchorMin;
        Vector2 startAnchorMax = rectTransform.anchorMax;
        Vector3 startScale = rectTransform.localScale;
        Vector3 endScale = originalScale * targetScale;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float rawProgress = elapsed / moveDuration;

            // AnimationCurve를 적용하여 easing 효과
            float curvedProgress = movementCurve.Evaluate(rawProgress);

            // 앵커 위치 보간
            rectTransform.anchorMin = Vector2.Lerp(startAnchorMin, targetAnchorMin, curvedProgress);
            rectTransform.anchorMax = Vector2.Lerp(startAnchorMax, targetAnchorMax, curvedProgress);

            // 스케일 보간
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, curvedProgress);

            yield return null;
        }

        // 최종 값 정확히 설정
        rectTransform.anchorMin = targetAnchorMin;
        rectTransform.anchorMax = targetAnchorMax;
        rectTransform.localScale = endScale;
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 값 변경 시 시각적으로 확인할 수 있도록 도와주는 메서드
    /// </summary>
    private void OnValidate()
    {
        // 음수 값 방지
        displayDuration = Mathf.Max(0f, displayDuration);
        moveDuration = Mathf.Max(0.0f, moveDuration);
        fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
        targetScale = Mathf.Max(0.0f, targetScale);

        // 앵커 값 범위 제한 (0~1)
        targetAnchorMin = new Vector2(
            Mathf.Clamp01(targetAnchorMin.x),
            Mathf.Clamp01(targetAnchorMin.y)
        );
        targetAnchorMax = new Vector2(
            Mathf.Clamp01(targetAnchorMax.x),
            Mathf.Clamp01(targetAnchorMax.y)
        );
    }
#endif

    #endregion
}