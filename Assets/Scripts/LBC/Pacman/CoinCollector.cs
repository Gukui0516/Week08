using UnityEngine;

/// <summary>
/// 팩맨(Player)에 부착하여 코인/파워펠렛 수집을 보조합니다.
/// Coin과 PowerPellet이 자체적으로 충돌을 감지하지만, 
/// 이 스크립트에서 추가적인 수집 로직이나 효과를 넣을 수 있습니다.
/// </summary>
public class CoinCollector : MonoBehaviour
{
    [Header("수집 설정")]
    [Tooltip("수집 가능한 거리 (0이면 Collider 기반으로만 작동)")]
    [SerializeField] private float collectionRadius = 0f;

    [Tooltip("자석 효과: 일정 거리 내의 코인을 끌어당길지 여부")]
    [SerializeField] private bool enableMagnetEffect = false;

    [Tooltip("자석 효과 반경")]
    [SerializeField] private float magnetRadius = 2f;

    [Tooltip("자석 효과로 끌어당기는 속도")]
    [SerializeField] private float magnetPullSpeed = 5f;

    [Header("시각 효과")]
    [Tooltip("코인 수집 시 재생할 파티클 효과 (팩맨 위치에서)")]
    [SerializeField] private GameObject collectParticleEffect;

    [Tooltip("파워 펠렛 수집 시 재생할 파티클 효과")]
    [SerializeField] private GameObject powerPelletParticleEffect;

    [Header("사운드")]
    [Tooltip("코인 수집 사운드 (AudioSource에서 재생)")]
    [SerializeField] private AudioClip coinCollectSound;

    [Tooltip("파워 펠렛 수집 사운드")]
    [SerializeField] private AudioClip powerPelletCollectSound;

    [Tooltip("사운드를 재생할 AudioSource (없으면 자동 생성)")]
    [SerializeField] private AudioSource audioSource;

    [Header("디버그")]
    [Tooltip("수집 정보를 콘솔에 출력할지 여부")]
    [SerializeField] private bool showDebugInfo = true;

    // 이번 프레임에 이미 수집한 오브젝트 추적 (중복 수집 방지)
    private int coinsCollectedThisFrame = 0;

    void Awake()
    {
        // AudioSource가 없으면 자동으로 추가
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // 2D와 3D 사운드의 중간
            }
        }
    }

    void Update()
    {
        coinsCollectedThisFrame = 0;

        // 자석 효과가 활성화되어 있으면 주변 코인을 끌어당김
        if (enableMagnetEffect)
        {
            ApplyMagnetEffect();
        }
    }

    /// <summary>
    /// 자석 효과: 주변의 코인/파워펠렛을 팩맨 쪽으로 끌어당깁니다.
    /// </summary>
    private void ApplyMagnetEffect()
    {
        // 주변의 모든 Collider를 검색
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, magnetRadius);

        foreach (Collider col in nearbyColliders)
        {
            // 코인이나 파워펠렛인지 확인
            Coin coin = col.GetComponent<Coin>();
            PowerPellet powerPellet = col.GetComponent<PowerPellet>();

            if (coin != null || powerPellet != null)
            {
                // 팩맨 방향으로 이동
                Vector3 direction = (transform.position - col.transform.position).normalized;
                col.transform.position += direction * magnetPullSpeed * Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 코인이나 파워펠렛과 충돌했을 때 호출됩니다.
    /// Coin/PowerPellet 스크립트가 자체적으로 수집을 처리하지만,
    /// 여기서 추가적인 효과를 넣을 수 있습니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 코인 수집
        if (other.CompareTag("Coin"))
        {
            OnCoinCollected(other.gameObject);
        }
        /*// 파워 펠렛 수집
        else if (other.CompareTag("PowerPellet"))
        {
            OnPowerPelletCollected(other.gameObject);
        }*/
    }

    /// <summary>
    /// 코인을 수집했을 때 호출되는 메서드입니다.
    /// 추가적인 효과나 로직을 여기에 구현할 수 있습니다.
    /// </summary>
    /// <param name="coinObject">수집한 코인 GameObject</param>
    private void OnCoinCollected(GameObject coinObject)
    {
        coinsCollectedThisFrame++;

        // 파티클 효과 재생
        if (collectParticleEffect != null)
        {
            Instantiate(collectParticleEffect, transform.position, Quaternion.identity);
        }

        // 사운드 재생
        if (coinCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(coinCollectSound);
        }

        // 디버그 정보 출력
        if (showDebugInfo)
        {
            Debug.Log($"코인 수집! (이번 프레임: {coinsCollectedThisFrame})");
        }
    }

    /// <summary>
    /// 파워 펠렛을 수집했을 때 호출되는 메서드입니다.
    /// 추가적인 효과나 로직을 여기에 구현할 수 있습니다.
    /// </summary>
    /// <param name="pelletObject">수집한 파워 펠렛 GameObject</param>
    private void OnPowerPelletCollected(GameObject pelletObject)
    {
        // 파티클 효과 재생 (파워 펠렛은 더 화려한 효과)
        if (powerPelletParticleEffect != null)
        {
            Instantiate(powerPelletParticleEffect, transform.position, Quaternion.identity);
        }

        // 사운드 재생
        if (powerPelletCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(powerPelletCollectSound);
        }

        // 디버그 정보 출력
        if (showDebugInfo)
        {
            Debug.Log("파워 펠렛 수집! 파워 모드 활성화!");
        }
    }

    /// <summary>
    /// 자석 효과를 일시적으로 활성화합니다.
    /// 다른 스크립트에서 호출할 수 있습니다.
    /// </summary>
    /// <param name="duration">지속 시간 (초, 0이면 무한)</param>
    public void ActivateMagnetEffect(float duration = 0f)
    {
        enableMagnetEffect = true;

        if (duration > 0f)
        {
            Invoke(nameof(DeactivateMagnetEffect), duration);
        }

        if (showDebugInfo)
        {
            Debug.Log($"자석 효과 활성화! (지속: {(duration > 0 ? duration + "초" : "무한")})");
        }
    }

    /// <summary>
    /// 자석 효과를 비활성화합니다.
    /// </summary>
    public void DeactivateMagnetEffect()
    {
        enableMagnetEffect = false;

        if (showDebugInfo)
        {
            Debug.Log("자석 효과 비활성화!");
        }
    }

    /// <summary>
    /// 자석 효과의 반경을 동적으로 변경합니다.
    /// </summary>
    public void SetMagnetRadius(float radius)
    {
        magnetRadius = radius;
    }

    /// <summary>
    /// Scene 뷰에서 자석 효과 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (enableMagnetEffect)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f); // 반투명 노란색
            Gizmos.DrawWireSphere(transform.position, magnetRadius);
        }

        if (collectionRadius > 0f)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 반투명 초록색
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}