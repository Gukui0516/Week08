using UnityEngine;
using System.Collections;

/// <summary>
/// 씬에 배치된 폭탄의 시각적 효과와 폭발 타이밍을 관리합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class BombController : MonoBehaviour
{
    [Header("Blinking Effect")]
    [Tooltip("점등 시 사용할 빨간색 머티리얼입니다.")]
    [SerializeField] private Material blinkingMaterial;
    [Tooltip("점등 주기(초)입니다. 1초에 한 번씩 깜빡입니다.")]
    [SerializeField] private float blinkInterval = 1.0f;

    [Header("Explosion Timing")]
    [Tooltip("트리거 후 몇 프레임 뒤에 폭발할지 설정합니다.")]
    [SerializeField] private int explosionDelayFrames = 0;

    [Header("Explosion Settings")]
    [Tooltip("이 폭탄의 폭발 중심점입니다. 비어있으면 자신의 위치를 사용합니다.")]
    [SerializeField] private Transform explosionCenter;
    [Tooltip("폭발의 기본 힘입니다.")]
    [SerializeField] private float explosionForce = 500f;
    [Tooltip("폭발 충격파가 미치는 반경입니다.")]
    [SerializeField] private float explosionRadius = 15f;
    [Tooltip("폭발 시 오브젝트를 위로 띄워 올리는 힘을 추가합니다.")]
    [SerializeField] private float upwardModifier = 2.0f;

    private Renderer objectRenderer;
    private Material originalMaterial;
    private Coroutine tickingCoroutine;

    // 프로퍼티
    public int ExplosionDelayFrames => explosionDelayFrames;
    public Vector3 ExplosionPosition => explosionCenter != null ? explosionCenter.position : transform.position;
    public float ExplosionForce => explosionForce;
    public float ExplosionRadius => explosionRadius;
    public float UpwardModifier => upwardModifier;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    /// <summary>
    /// 폭탄의 점등 효과를 시작합니다.
    /// </summary>
    /// <param name="duration">폭발까지 걸리는 시간(초)</param>
    public void StartTicking(float duration)
    {
        if (tickingCoroutine != null)
        {
            StopCoroutine(tickingCoroutine);
        }
        tickingCoroutine = StartCoroutine(TickingCoroutine(duration));
    }

    /// <summary>
    /// 즉시 점등 효과를 중지합니다.
    /// </summary>
    public void StopTicking()
    {
        if (tickingCoroutine != null)
        {
            StopCoroutine(tickingCoroutine);
            tickingCoroutine = null;
        }

        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }

    /// <summary>
    /// 폭탄을 폭발시키고 비활성화합니다.
    /// </summary>
    public void Explode()
    {
        StopTicking();

        // 폭발 알림 추가!
        BombManager.Instance.NotifyBombExploded(gameObject);

        gameObject.SetActive(false);
    }

    private IEnumerator TickingCoroutine(float duration)
    {
        if (objectRenderer == null || blinkingMaterial == null || originalMaterial == null)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            objectRenderer.material = blinkingMaterial;
            yield return new WaitForSeconds(blinkInterval / 2);

            objectRenderer.material = originalMaterial;
            yield return new WaitForSeconds(blinkInterval / 2);

            elapsedTime += blinkInterval;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = explosionCenter != null ? explosionCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, explosionRadius);
        
        // 폭발 중심점 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.3f);
    }
#endif
}

