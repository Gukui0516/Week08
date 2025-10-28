using UnityEngine;

/// <summary>
/// 팩맨이 먹으면 일정 시간 동안 고스트를 먹을 수 있게 하는 파워 펠렛입니다.
/// 기본 코인보다 크고, 점멸 효과가 있으며, 파워 모드를 활성화합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerPellet : MonoBehaviour
{
    [Header("파워 펠렛 설정")]
    [Tooltip("이 파워 펠렛을 먹었을 때 얻는 점수")]
    [SerializeField] private int scoreValue = 50;

    [Header("시각 효과")]
    [Tooltip("수집 시 재생할 파티클 효과 (선택 사항)")]
    [SerializeField] private GameObject collectEffectPrefab;

    [Tooltip("수집 시 재생할 오디오 클립 (선택 사항)")]
    [SerializeField] private AudioClip collectSound;

    [Header("점멸 효과")]
    [Tooltip("파워 펠렛을 점멸시킬지 여부")]
    [SerializeField] private bool enableBlinking = true;

    [Tooltip("점멸 속도 (초당 깜빡임 횟수)")]
    [SerializeField] private float blinkSpeed = 2f;

    [Tooltip("점멸 효과를 적용할 렌더러 (비어있으면 자동으로 찾음)")]
    [SerializeField] private Renderer targetRenderer;

    [Header("회전 효과")]
    [Tooltip("파워 펠렛을 회전시킬지 여부")]
    [SerializeField] private bool rotateConstantly = true;

    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 50, 0);

    [Header("크기 애니메이션")]
    [Tooltip("크기를 변화시킬지 여부 (펄스 효과)")]
    [SerializeField] private bool enableScalePulse = true;

    [Tooltip("크기 변화 속도")]
    [SerializeField] private float pulseSpeed = 3f;

    [Tooltip("크기 변화 범위 (1.0 기준)")]
    [SerializeField] private float pulseAmount = 0.2f;

    private bool isCollected = false;
    private Collider pelletCollider;
    private float blinkTimer = 0f;
    private Vector3 originalScale;
    private Material materialInstance; // 머티리얼 인스턴스 (점멸 효과용)

    void Awake()
    {
        // Collider가 Trigger로 설정되어 있는지 확인
        pelletCollider = GetComponent<Collider>();
        if (!pelletCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}의 Collider가 Trigger로 설정되지 않았습니다. 자동으로 Trigger로 변경합니다.");
            pelletCollider.isTrigger = true;
        }

        // 렌더러가 할당되지 않았으면 자동으로 찾기
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        // 점멸 효과를 위한 머티리얼 인스턴스 생성
        if (enableBlinking && targetRenderer != null)
        {
            // 원본 머티리얼을 복사하여 인스턴스 생성 (다른 오브젝트에 영향 안 줌)
            materialInstance = targetRenderer.material;
        }

        // 원래 크기 저장
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isCollected)
            return;

        // 회전 효과
        if (rotateConstantly)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        // 점멸 효과
        if (enableBlinking && targetRenderer != null && materialInstance != null)
        {
            UpdateBlinkEffect();
        }

        // 크기 펄스 효과
        if (enableScalePulse)
        {
            UpdateScalePulse();
        }
    }

    /// <summary>
    /// 점멸 효과를 업데이트합니다.
    /// 알파 값을 변화시켜 깜빡이는 효과를 만듭니다.
    /// </summary>
    private void UpdateBlinkEffect()
    {
        blinkTimer += Time.deltaTime * blinkSpeed;

        // 0과 1 사이를 왔다갔다 하는 값 계산 (사인파 사용)
        float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(blinkTimer * Mathf.PI * 2f) + 1f) * 0.5f);

        // 머티리얼의 알파 값 변경
        Color color = materialInstance.color;
        color.a = alpha;
        materialInstance.color = color;
    }

    /// <summary>
    /// 크기 펄스 효과를 업데이트합니다.
    /// 크기를 주기적으로 변화시켜 호흡하는 듯한 효과를 만듭니다.
    /// </summary>
    private void UpdateScalePulse()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * scale;
    }

    /// <summary>
    /// 팩맨과 충돌했을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 이미 수집된 경우 무시
        if (isCollected)
            return;

        // 팩맨 태그를 가진 오브젝트와 충돌했는지 확인
        if (other.CompareTag("Draggable"))
        {
            CollectPowerPellet();
        }
    }

    /// <summary>
    /// 파워 펠렛을 수집하는 메인 로직입니다.
    /// 점수를 증가시키고, 파워 모드를 활성화하며, 효과를 재생하고, 자신을 제거합니다.
    /// </summary>
    private void CollectPowerPellet()
    {
        // 중복 수집 방지
        isCollected = true;

        // 게임 매니저에 점수 추가
        if (PacmanGameManager.Instance != null)
        {
            PacmanGameManager.Instance.AddScore(scoreValue);

            // 파워 모드 활성화 (가장 중요!)
            PacmanGameManager.Instance.ActivatePowerMode();

            // 코인으로도 카운트 (클리어 조건에 포함)
            PacmanGameManager.Instance.OnCoinCollected();
        }
        else
        {
            Debug.LogWarning("PacmanGameManager를 찾을 수 없어 파워 모드를 활성화할 수 없습니다.");
        }

        // 수집 효과 재생
        PlayCollectEffects();

        // 파워 펠렛 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 수집 시 시각/청각 효과를 재생합니다.
    /// </summary>
    private void PlayCollectEffects()
    {
        // 파티클 효과 생성
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }

        // 사운드 재생
        if (collectSound != null)
        {
            // AudioSource.PlayClipAtPoint를 사용하여 일회성 사운드 재생
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }

    /// <summary>
    /// 파워 펠렛의 점수 값을 동적으로 설정합니다.
    /// 런타임에서 파워 펠렛을 생성할 때 유용합니다.
    /// </summary>
    public void SetScoreValue(int value)
    {
        scoreValue = value;
    }

    /// <summary>
    /// 파워 펠렛의 점수 값을 반환합니다.
    /// </summary>
    public int GetScoreValue()
    {
        return scoreValue;
    }

    /// <summary>
    /// Scene 뷰에서 파워 펠렛의 수집 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.cyan;

            // SphereCollider인 경우
            if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position, sphereCol.radius * transform.localScale.x);
            }
            // BoxCollider인 경우
            else if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
        }
    }

    void OnDestroy()
    {
        // 머티리얼 인스턴스 메모리 해제 (메모리 누수 방지)
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}