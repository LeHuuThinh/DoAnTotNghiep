using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private Collider enemyCollider;
  [Tooltip("Để trống thì code tự tìm tất cả bộ phận con. Hoặc bạn có thể tự kéo các bộ phận vào đây.")]
  [SerializeField] private Renderer[] bodyRenderers;

  [Header("Flash Feedback")]
  [SerializeField] private float flashDuration = 0.1f;
  [SerializeField] private Color flashColor = Color.red;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 40f;

  [Header("Knockback Settings")]
  [SerializeField] private float knockbackDuration = 0.15f;

  // Lưu từng material và màu gốc của nó
  private class MaterialCache
  {
    public Material mat;
    public Color originalColor;
    public string propName;
  }

  private Health health;
  private EnemyAIBase enemyAI;
  private float currentPoise;
  private float lastHitTime;
  private Coroutine knockbackCoroutine;
  private Coroutine flashCoroutine;
  private readonly List<MaterialCache> cachedMaterials = new List<MaterialCache>();

  private static readonly int GetHitTriggerHash = Animator.StringToHash("GetHit");
  private static readonly int HitTypeHash = Animator.StringToHash("HitType");
  private static readonly int DieTriggerHash = Animator.StringToHash("Die");
  private static readonly int DeathTypeHash = Animator.StringToHash("DeathType");

  private void Awake()
  {
    health = GetComponent<Health>();
    enemyAI = GetComponent<EnemyAIBase>();

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (enemyCollider == null) enemyCollider = GetComponent<Collider>();

    currentPoise = maxPoise;

    // 1. Tự tìm tất cả SkinnedMeshRenderer và MeshRenderer ở mọi object con nếu chưa kéo tay
    if (bodyRenderers == null || bodyRenderers.Length == 0)
    {
      bodyRenderers = GetComponentsInChildren<Renderer>(true);
    }

    // 2. Lưu lại toàn bộ Material và màu gốc của từng bộ phận
    foreach (Renderer rend in bodyRenderers)
    {
      if (rend == null || rend is ParticleSystemRenderer || rend is TrailRenderer) continue;

      foreach (Material m in rend.materials)
      {
        if (m == null) continue;

        string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (m.HasProperty(prop))
        {
          cachedMaterials.Add(new MaterialCache
          {
            mat = m,
            originalColor = m.GetColor(prop),
            propName = prop
          });
        }
      }
    }
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(HandleTakeDamage);
    health.onDeath.AddListener(HandleDeath);
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(HandleTakeDamage);
    health.onDeath.RemoveListener(HandleDeath);
    ResetColors();
  }

  private void Update()
  {
    if (currentPoise < maxPoise && Time.time >= lastHitTime + poiseRecoveryDelay)
    {
      currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
    }
  }

  private void HandleTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    if (health.IsDead) return;

    lastHitTime = Time.time;

    // Tính Poise
    float poiseDamageTaken = 30f;
    if (knockbackDistance >= 1.5f) poiseDamageTaken = 100f;
    else if (knockbackDistance > 0f) poiseDamageTaken = 60f;

    currentPoise -= poiseDamageTaken;

    // Luôn chớp đỏ tất cả bộ phận khi trúng kiếm
    FlashDamageFeedback();

    // Hyper Armor check
    bool isAttacking = enemyAI != null && enemyAI.IsAttacking;
    if (isAttacking && currentPoise > 0f) return;

    // Stagger
    if (currentPoise <= 0f || knockbackDistance > 0f)
    {
      currentPoise = maxPoise;

      int randomHit = Random.Range(0, 2);
      animator.SetInteger(HitTypeHash, randomHit);
      animator.SetTrigger(GetHitTriggerHash);

      if (knockbackDistance > 0f)
      {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
      }
    }
  }

  // =========================================================================
  // XỬ LÝ NHÁY ĐỎ TRỰC TIẾP TRÊN MATERIAL
  // =========================================================================

  private void FlashDamageFeedback()
  {
    if (cachedMaterials.Count == 0) return;

    if (flashCoroutine != null) StopCoroutine(flashCoroutine);
    flashCoroutine = StartCoroutine(FlashRedRoutine());
  }

  private IEnumerator FlashRedRoutine()
  {
    // Đổi màu tất cả bộ phận sang đỏ
    for (int i = 0; i < cachedMaterials.Count; i++)
    {
      if (cachedMaterials[i].mat != null)
        cachedMaterials[i].mat.SetColor(cachedMaterials[i].propName, flashColor);
    }

    yield return new WaitForSeconds(flashDuration);

    // Trả lại màu gốc cho tất cả bộ phận
    ResetColors();
    flashCoroutine = null;
  }

  private void ResetColors()
  {
    for (int i = 0; i < cachedMaterials.Count; i++)
    {
      if (cachedMaterials[i].mat != null)
        cachedMaterials[i].mat.SetColor(cachedMaterials[i].propName, cachedMaterials[i].originalColor);
    }
  }

  private IEnumerator ApplyKnockback(Vector3 direction, float distance)
  {
    direction.y = 0f;
    direction.Normalize();

    float timer = 0f;
    float speed = distance / knockbackDuration;

    while (timer < knockbackDuration)
    {
      timer += Time.deltaTime;
      transform.position += direction * (speed * Time.deltaTime);
      yield return null;
    }

    knockbackCoroutine = null;
  }

  // =========================================================================
  // XỬ LÝ CHẾT (Đảm bảo nháy đỏ xong tự nhả lại màu gốc)
  // =========================================================================

  private void HandleDeath()
  {
    if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

    // Phát chém kết liễu: Không ngắt flashCoroutine giữa chừng để nó tự chạy nốt
    // 0.1 giây rồi trả lại màu gốc bình thường cho cái xác.

    int randomDeath = Random.Range(0, 2);
    animator.SetInteger(DeathTypeHash, randomDeath);
    animator.SetTrigger(DieTriggerHash);

    if (enemyCollider != null) enemyCollider.enabled = false;

    Destroy(gameObject, 5f);
  }
}