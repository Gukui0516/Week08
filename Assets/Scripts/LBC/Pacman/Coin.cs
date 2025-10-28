using UnityEngine;

/// <summary>
/// 팩맨이 수집할 수 있는 기본 코인입니다.
/// 팩맨과 충돌하면 점수를 증가시키고 자신을 제거합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("코인 설정")]
    [Tooltip("이 코인을 먹었을 때 얻는 점수")]
    [SerializeField] private int scoreValue = 10;

    [Header("시각 효과")]
    [Tooltip("수집 시 재생할 파티클 효과 (선택 사항)")]
    [SerializeField] private GameObject collectEffectPrefab;

    [Tooltip("수집 시 재생할 오디오 클립 (선택 사항)")]
    [SerializeField] private AudioClip collectSound;

    [Header("회전 효과")]
    [Tooltip("코인을 회전시킬지 여부")]
    [SerializeField] private bool rotateConstantly = true;

    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 100, 0);

    private bool isCollected = false;
    private Collider coinCollider;

    void Awake()
    {
        // Collider가 Trigger로 설정되어 있는지 확인
        coinCollider = GetComponent<Collider>();
        if (!coinCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}의 Collider가 Trigger로 설정되지 않았습니다. 자동으로 Trigger로 변경합니다.");
            coinCollider.isTrigger = true;
        }
    }

    void Update()
    {
        // 코인을 계속 회전시켜서 시각적 효과 추가
        if (rotateConstantly && !isCollected)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
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
            CollectCoin();
        }
    }

    /// <summary>
    /// 코인을 수집하는 메인 로직입니다.
    /// 점수를 증가시키고, 효과를 재생하며, 자신을 제거합니다.
    /// </summary>
    private void CollectCoin()
    {
        // 중복 수집 방지
        isCollected = true;

        // 게임 매니저에 점수 추가
        if (PacmanGameManager.Instance != null)
        {
            PacmanGameManager.Instance.AddScore(scoreValue);
            PacmanGameManager.Instance.OnCoinCollected();
        }
        else
        {
            Debug.LogWarning("PacmanGameManager를 찾을 수 없어 점수를 추가할 수 없습니다.");
        }

        // 수집 효과 재생
        PlayCollectEffects();

        // 코인 제거
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
    /// 코인의 점수 값을 동적으로 설정합니다.
    /// 런타임에서 코인을 생성할 때 유용합니다.
    /// </summary>
    public void SetScoreValue(int value)
    {
        scoreValue = value;
    }

    /// <summary>
    /// 코인의 점수 값을 반환합니다.
    /// </summary>
    public int GetScoreValue()
    {
        return scoreValue;
    }

    /// <summary>
    /// Scene 뷰에서 코인의 수집 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;

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
}