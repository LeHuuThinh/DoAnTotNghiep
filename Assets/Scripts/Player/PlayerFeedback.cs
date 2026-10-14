using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private TargetLockSystem targetLockSystem;
  [SerializeField] private PlayerCombat playerCombat;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 35f;

  [Header("Stagger / Knockback")]
  [SerializeField] private float staggerDuration = 0.5f;
  [SerializeField] private float knockbackDuration = 0.15f;

  public bool HasHyperArmor { get; set; } = false;
  public bool IsParrying { get; set; } = false;

  // Cổng kiểm tra xem người chơi có đang bị choáng/vỡ thế hay không
  public bool IsStaggered => staggerCoroutine != null;

  private Health health;
  private PlayerController playerController;
  private CharacterController characterController;

  private float currentPoise;
  private float lastHitTime;
  private Coroutine staggerCoroutine;
  private Coroutine knockbackCoroutine;

  private static readonly int GetHitHash = Animator.StringToHash("GetHit");
  private static readonly int DieHash = Animator.StringToHash("Die");

  private void Awake()
  {
    health = GetComponent<Health>();
    playerController = GetComponent<PlayerController>();
    characterController = GetComponent<CharacterController>();

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (targetLockSystem == null) targetLockSystem = GetComponent<TargetLockSystem>();

    // Tự động tìm kiếm linh hoạt ở cả Object cha lẫn Object con
    if (playerCombat == null)
    {
      playerCombat = GetComponent<PlayerCombat>();
      if (playerCombat == null) playerCombat = GetComponentInChildren<PlayerCombat>();
      if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
    }

    currentPoise = maxPoise;
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

    // 1. I-FRAME ROLL: Đang lăn né đòn thì bỏ qua sát thương
    if (playerController.IsRolling) return;

    // 2. =================================================================
    // PARRY THÀNH CÔNG!
    // =================================================================
    if (IsParrying)
    {
      ExecuteParrySuccess();
      return; // Miễn nhiễm 100% sát thương và không bị khựng đòn
    }

    lastHitTime = Time.time;

    // 3. TÍNH POISE DAMAGE BÌNH THƯỜNG
    float poiseDamageTaken = knockbackDistance > 0.8f ? 70f : 35f;
    currentPoise -= poiseDamageTaken;

    if (HasHyperArmor) return;

    // 4. BỊ VỠ THẾ
    if (currentPoise <= 0f || knockbackDistance >= 1.2f)
    {
      currentPoise = maxPoise;
      animator.SetTrigger(GetHitHash);

      if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
      staggerCoroutine = StartCoroutine(StaggerRoutine());

      if (knockbackDistance > 0f)
      {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
      }
    }
  }

  private void ExecuteParrySuccess()
  {
    Debug.Log("<color=yellow>[PARRY THÀNH CÔNG!] Keng!</color>");

    // Khựng hình nhẹ 0.06s tạo độ "đanh"
    StartCoroutine(HitStopRoutine(0.06f));

    // Tìm quái tấn công (ưu tiên quái đang lock-on hoặc ở gần trước mặt)
    Transform enemy = targetLockSystem != null && targetLockSystem.IsLockedOn ? targetLockSystem.CurrentTarget : null;

    if (enemy == null)
    {
      Collider[] cols = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 2.5f);
      foreach (Collider c in cols)
      {
        if (c.GetComponentInParent<EnemyAI>() != null)
        {
          enemy = c.transform.root;
          break;
        }
      }
    }

    // Ép quái bị ngắt chiêu và vỡ thế hoàn toàn
    if (enemy != null)
    {
      EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
      if (enemyAI != null) enemyAI.InterruptAttack();

      Animator enemyAnim = enemy.GetComponentInChildren<Animator>();
      if (enemyAnim != null)
      {
        enemyAnim.SetInteger("HitType", 1);
        enemyAnim.SetTrigger("GetHit");
      }
    }

    // Báo cho PlayerCombat mở cửa sổ Counter Attack
    if (playerCombat != null)
    {
      playerCombat.OnParrySuccess();
    }
    else
    {
      Debug.LogError("Chưa tìm thấy PlayerCombat trên Player!");
    }
  }

  private IEnumerator HitStopRoutine(float duration)
  {
    Time.timeScale = 0.05f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1f;
  }

  private IEnumerator StaggerRoutine()
  {
    playerController.LockMovement();
    yield return new WaitForSeconds(staggerDuration);
    playerController.UnlockMovement();
    staggerCoroutine = null;
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
      characterController.Move(direction * (speed * Time.deltaTime));
      yield return null;
    }

    knockbackCoroutine = null;
  }

  private void HandleDeath()
  {
    if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
    if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

    animator.SetTrigger(DieHash);
    playerController.LockMovement();
    playerController.enabled = false;
    characterController.enabled = false;

    if (targetLockSystem != null) targetLockSystem.UnlockTarget();

    Debug.Log("<color=red>[YOU DIED]</color>");
  }

  public void EnableHyperArmor() => HasHyperArmor = true;
  public void DisableHyperArmor() => HasHyperArmor = false;
}