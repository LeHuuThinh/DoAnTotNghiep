using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private Collider enemyCollider;

  [Header("Knockback Settings")]
  [SerializeField] private float knockbackDuration = 0.15f; // Thời gian trượt lùi (càng nhỏ trượt càng gắt)

  private Health health;
  private Coroutine knockbackCoroutine;

  private static readonly int GetHitTriggerHash = Animator.StringToHash("GetHit");
  private static readonly int HitTypeHash = Animator.StringToHash("HitType");
  private static readonly int DieTriggerHash = Animator.StringToHash("Die");
  private static readonly int DeathTypeHash = Animator.StringToHash("DeathType");

  private void Awake()
  {
    health = GetComponent<Health>();
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (enemyCollider == null) enemyCollider = GetComponent<Collider>();
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
  }

  // =========================================================================
  // XỬ LÝ GET HIT (Random A/B & Chỉ Knockback khi có lực đẩy)
  // =========================================================================
  private void HandleTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    if (health.IsDead) return;

    // 1. RANDOM 1 TRONG 2 THẾ GIẬT NGƯỜI (0: Hit_A, 1: Hit_B)
    int randomHit = Random.Range(0, 2);
    animator.SetInteger(HitTypeHash, randomHit);
    animator.SetTrigger(GetHitTriggerHash);

    // 2. CHỈ ĐẨY LÙI KHI ĐÒN ĐÁNH CÓ YÊU CẦU KNOCKBACK (> 0)
    if (knockbackDistance > 0f)
    {
      if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
      knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
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
      // Trượt nhẹ quái về sau theo hướng vung kiếm
      transform.position += direction * (speed * Time.deltaTime);
      yield return null;
    }

    knockbackCoroutine = null;
  }

  // =========================================================================
  // XỬ LÝ DEATH (Random A/B & Tắt va chạm)
  // =========================================================================
  private void HandleDeath()
  {
    if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

    // 1. RANDOM 1 TRONG 2 THẾ NGÃ CHẾT (0: Death_A, 1: Death_B)
    int randomDeath = Random.Range(0, 2);
    animator.SetInteger(DeathTypeHash, randomDeath);
    animator.SetTrigger(DieTriggerHash);

    // 2. TẮT COLLIDER NGAY: Để người chơi không bị cản đường, bước qua xác được
    if (enemyCollider != null)
    {
      enemyCollider.enabled = false;
    }

    // Tự hủy GameObject xác sau 5 giây để dọn dẹp bộ nhớ hầm ngục
    Destroy(gameObject, 5f);
  }
}