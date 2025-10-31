using UnityEngine;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 스테이지의 총 시간을 카운트다운하고, 남은 시간에 따라 TMP 텍스트의 색상을 변경하여 긴박감을 연출합니다.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("타이머 텍스트를 표시할 TextMeshPro 컴포넌트를 연결하십시오.")]
    [SerializeField] private TextMeshProUGUI timerText;

    private bool isRunning = false;
    private float currentTime;

    // === 1. 시간 설정 (인스펙터 조절 가능) ===
    [Header("Time & Threshold Settings")]
    [Tooltip("스테이지 시작 시 카운트다운 할 총 시간 (초)")]
    public float totalTime = 120.0f;

    [Tooltip("경고 상태로 전환될 남은 시간 임계값")]
    public float warningThreshold = 15.0f;

    [Tooltip("위험 상태로 전환될 남은 시간 임계값")]
    public float dangerThreshold = 5.0f;

    // === 2. 시각 효과 설정 (인스펙터 조절 가능) ===
    [Header("Visual Effects")]
    public Color initialColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Tooltip("경고/위험 상태에서 색상이 깜빡이는 속도 (높을수록 빠름)")]
    public float pulseSpeed = 5.0f;

    private Coroutine pulseCoroutine;
    private Color currentBaseColor = Color.black; // 현재 깜빡임의 기준 색상을 추적

    void Start()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TextMeshProUGUI>();
        }

        if (timerText == null)
        {
            Debug.LogError("[GameTimer] TextMeshProUGUI 컴포넌트가 할당되지 않았습니다. 스크립트를 비활성화합니다.");
            enabled = false;
            return;
        }

        currentTime = totalTime;
        UpdateTimerDisplay(currentTime);
        timerText.color = initialColor;
        currentBaseColor = initialColor; // 초기 색상 설정

        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;
        UpdateTimerDisplay(currentTime);

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimerDisplay(0f);
            HandleTimeOut();
        }
        else if (currentTime <= dangerThreshold)
        {
            ChangeState(dangerColor, true); // 5초 이하: 빨간색, 깜빡임 ON
        }
        else if (currentTime <= warningThreshold)
        {
            ChangeState(warningColor, true); // 15초 이하: 노란색, 깜빡임 ON
        }
        else
        {
            ChangeState(initialColor, false); // 초기 상태: 흰색, 깜빡임 OFF
        }
    }

    /// <summary>
    /// 시간을 SS.ss 포맷으로 변환하여 TMP에 표시합니다.
    /// </summary>
    private void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timeToDisplay < 0) timeToDisplay = 0;
        timerText.text = timeToDisplay.ToString("F2");
    }

    /// <summary>
    /// 타이머의 상태(색상 및 깜빡임)를 변경합니다.
    /// </summary>
    private void ChangeState(Color targetColor, bool shouldPulse)
    {
        // 색상이 바뀌어야 하는지 확인 (노란색 -> 빨간색 전환 시)
        bool colorNeedsRestart = (targetColor != currentBaseColor) && shouldPulse;

        // 1. 펄스 상태에서 이탈하는 경우 (일반 상태로 복귀)
        if (!shouldPulse && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            timerText.color = targetColor;
            currentBaseColor = targetColor;
            return;
        }

        // 2. 펄스 상태로 진입하거나 색상 변경이 필요한 경우
        if (shouldPulse)
        {
            if (pulseCoroutine == null || colorNeedsRestart)
            {
                // 실행 중이던 코루틴이 있다면 멈추고 새 색상으로 시작
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }

                // 새로운 베이스 색상 설정 및 코루틴 시작
                currentBaseColor = targetColor;
                pulseCoroutine = StartCoroutine(PulseColorCoroutine(targetColor));
            }
        }
        else // shouldPulse == false (초기 상태)
        {
            timerText.color = targetColor;
            currentBaseColor = targetColor;
        }
    }

    /// <summary>
    /// 색상 깜빡임 효과를 구현하는 코루틴입니다.
    /// </summary>
    private IEnumerator PulseColorCoroutine(Color baseColor)
    {
        float t = 0;
        Color invisibleColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f); // 투명한 색상

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float alpha = (Mathf.Sin(t) + 1f) / 2f; // 0.0 ~ 1.0 사이의 값

            timerText.color = Color.Lerp(invisibleColor, baseColor, alpha);

            yield return null;
        }
    }

    /// <summary>
    /// 게임 클리어 시 외부에서 호출되어 타이머를 멈춥니다.
    /// </summary>
    public void StopTimer(bool success)
    {
        isRunning = false;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (success)
        {
            // 성공 시 최종 시간 표시
            timerText.color = Color.green;
        }
        else
        {
            // 실패 시 빨간색 고정
            timerText.color = dangerColor;
        }
    }

    /// <summary>
    /// 시간이 0이 되었을 때 호출되는 이벤트 처리 함수입니다.
    /// </summary>
    private void HandleTimeOut()
    {
        Debug.LogWarning("타이머 시간 초과! 스테이지 실패 이벤트를 호출합니다.");

        StopTimer(false);

        ClimaxController_Advanced climaxController = FindAnyObjectByType<ClimaxController_Advanced>();

        if (climaxController != null)
        {
            climaxController.StartClimaxSequence();
            Debug.Log("[GameTimer] ClimaxController의 최종 폭발 시퀀스를 호출했습니다.");
        }
        else
        {
            Debug.LogError("[GameTimer] 씬에서 ClimaxController_Advanced를 찾을 수 없습니다. 폭발 시퀀스 실패.");
        }
    }
}