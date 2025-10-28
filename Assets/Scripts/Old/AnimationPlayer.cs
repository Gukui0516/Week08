using UnityEngine;
using System.Collections; // 코루틴을 사용하기 위해 필요합니다.

/// <summary>
/// 지정된 Animator Trigger를 일정 시간 간격으로 반복 호출합니다.
/// </summary>
public class AnimationPlayer : MonoBehaviour
{
    [Tooltip("애니메이션을 제어할 Animator 컴포넌트입니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("반복해서 호출할 Trigger의 이름입니다.")]
    [SerializeField] private string triggerName = "Play";

    [Tooltip("Trigger를 다시 호출할 시간 간격(초)입니다.")]
    [SerializeField] private float interval = 5.0f;

    [Tooltip("게임 시작 후 첫 Trigger가 호출되기까지의 대기 시간(초)입니다.")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("체크하면 게임이 시작될 때 자동으로 반복을 시작합니다.")]
    [SerializeField] private bool playOnStart = true;

    private Coroutine repeatCoroutine; // 실행 중인 코루틴을 제어하기 위한 변수

    void Start()
    {
        // Inspector 창에서 Animator가 할당되지 않았다면,
        // 이 게임 오브젝트에 붙어있는 컴포넌트를 자동으로 찾아옵니다.
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // playOnStart가 체크되어 있으면 자동으로 반복을 시작합니다.
        if (playOnStart)
        {
            StartRepeating();
        }
    }

    /// <summary>
    /// Trigger 반복을 시작합니다. (ON 기능)
    /// </summary>
    public void StartRepeating()
    {
        // 이미 실행 중인 코루틴이 있다면 중복 실행을 막기 위해 먼저 중지합니다.
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
        }

        // 코루틴을 시작하고, 나중에 중지할 수 있도록 변수에 저장합니다.
        repeatCoroutine = StartCoroutine(ActivateTriggerRoutine());
    }

    /// <summary>
    /// Trigger 반복을 중지합니다. (OFF 기능)
    /// </summary>
    public void StopRepeating()
    {
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
            repeatCoroutine = null; // 변수 초기화
        }
    }

    // Trigger를 반복 호출하는 실제 로직이 담긴 코루틴 함수
    private IEnumerator ActivateTriggerRoutine()
    {
        // 설정된 시작 딜레이만큼 기다립니다.
        yield return new WaitForSeconds(startDelay);

        // 무한 루프를 돌면서 Trigger를 반복 호출합니다.
        while (true)
        {
            // Animator의 Trigger를 활성화합니다.
            animator.SetTrigger(triggerName);

            // 설정된 간격만큼 기다립니다.
            yield return new WaitForSeconds(interval);
        }
    }
}