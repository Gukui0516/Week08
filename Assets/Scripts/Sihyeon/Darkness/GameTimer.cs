using UnityEngine;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 스테이지의 총 시간을 카운트다운하고, 남은 시간에 따라 TMP 텍스트의 색상을 변경하여 긴박감을 연출합니다.
/// BombCollisionDetector의 이벤트를 구독하여 폭탄 폭발 시 시간을 추가합니다.
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

    // === 3. 추가 시간 설정 (추가됨) ===
    [Header("Bonus Time")]
    [Tooltip("폭탄 폭발 시 타이머에 추가할 시간 (초)")]
    public float bonusTimeOnExplosion = 5.0f;

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

        // BombCollisionDetector 이벤트 구독 (수정된 로직)
        BombCollisionDetector.OnBombCollisionDetected += OnBombCollisionDetectedHandler;

        currentTime = totalTime;
        UpdateTimerDisplay(currentTime);
        timerText.color = initialColor;
        currentBaseColor = initialColor; // 초기 색상 설정

        isRunning = true;
    }

    private void OnDestroy()
    {
        // BombCollisionDetector 이벤트 구독 해제 (수정된 로직)
        BombCollisionDetector.OnBombCollisionDetected -= OnBombCollisionDetectedHandler;
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
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }

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

    // ==========================================================
    // 폭탄 충돌 이벤트 처리 및 시간 추가 로직 (추가/수정됨)
    // ==========================================================

    /// <summary>
    /// BombCollisionDetector 이벤트 핸들러: 폭탄 충돌 감지 시 시간을 추가합니다.
    /// 이 시점은 폭탄이 '폭발 라인'에 닿았을 때입니다.
    /// </summary>
    /// <param name="bomb">충돌한 폭탄 GameObject (인수 사용 안 함)</param>
    private void OnBombCollisionDetectedHandler(GameObject bomb)
    {
        // BombCollisionDetector는 충돌 감지 후 NotifyBombExploded를 호출할 책임이 있으므로, 
        // 여기서 시간을 추가하는 것이 논리적으로 맞습니다.
        AddTime(bonusTimeOnExplosion);
        Debug.Log($"[GameTimer] 폭탄 충돌 감지! 타이머에 +{bonusTimeOnExplosion}초 추가. (현재 시간: {currentTime:F2})");
    }

    /// <summary>
    /// 현재 타이머에 지정된 시간만큼 추가합니다.
    /// </summary>
    /// <param name="amount">추가할 시간 (초)</param>
    public void AddTime(float amount)
    {
        if (amount <= 0 || !isRunning) return;

        currentTime += amount;

        // 시간이 추가되어 경고 상태에서 벗어날 경우 시각 효과 즉시 갱신
        if (currentTime > warningThreshold && pulseCoroutine != null)
        {
            ChangeState(initialColor, false);
        }

        UpdateTimerDisplay(currentTime);
    }

    // ==========================================================

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

        // 이벤트 구독 해제
        BombCollisionDetector.OnBombCollisionDetected -= OnBombCollisionDetectedHandler;

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