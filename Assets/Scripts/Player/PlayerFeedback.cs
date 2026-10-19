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
  [SerializeField] private ThirdPersonCamera thirdPersonCamera;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 35f;

  [Header("Parry Feedback Settings")]
  [Tooltip("Thời gian hitlag khựng hình khi Parry thành công (giây thực tế)")]
  [SerializeField] private float parryHitlagDuration = 0.2f;
  [Tooltip("Tốc độ thời gian khi khựng hitlag (0.05 = làm chậm 95%)")]
  [SerializeField] private float parryTimeScale = 0.05f;

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
  private Coroutine hitStopCoroutine;

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

    if (thirdPersonCamera == null)
    {
      if (playerController != null && playerController.ThirdPersonCamera != null)
      {
        thirdPersonCamera = playerController.ThirdPersonCamera;
      }
      else
      {
        thirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();
      }
    }

    currentPoise = maxPoise;
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(HandleTakeDamage);
    health.onDeath.AddListener(HandleDeath);
    if (health != null) health.onCheckParryOrBlock = TryParry;
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(HandleTakeDamage);
    health.onDeath.RemoveListener(HandleDeath);
    if (health != null && health.onCheckParryOrBlock == TryParry) health.onCheckParryOrBlock = null;

    if (hitStopCoroutine != null)
    {
      Time.timeScale = 1f;
      hitStopCoroutine = null;
    }
  }

  /// <summary>
  /// Can thiệp ngay TRƯỚC KHI Health bị trừ máu: Nếu người chơi đang trong thế Parry thì gạt đòn thành công!
  /// </summary>
  private bool TryParry(Transform attacker, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    if (IsParrying)
    {
      ExecuteParrySuccess(attacker);
      return true; // Gạt đòn thành công -> Bỏ qua 100% sát thương, KHÔNG TRỪ MÁU!
    }
    return false;
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

    // 1. TÍNH POISE DAMAGE BÌNH THƯỜNG
    float poiseDamageTaken = knockbackDistance > 0.8f ? 70f : 35f;
    currentPoise -= poiseDamageTaken;

    if (HasHyperArmor) return;

    // 2. BỊ VỠ THẾ
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

  public void ExecuteParrySuccess(Transform incomingAttacker = null)
  {
    Debug.Log("<color=yellow>[PARRY THÀNH CÔNG!] Keng!</color>");

    // 1. Khựng hình hitlag 0.2s tạo độ đanh và cảm giác lực
    TriggerHitlag(parryHitlagDuration, parryTimeScale);

    // 2. Kích hoạt camera zoom tới và trả về mượt mà
    if (thirdPersonCamera != null)
    {
      thirdPersonCamera.TriggerParryZoom();
    }
    else
    {
      thirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();
      if (thirdPersonCamera != null) thirdPersonCamera.TriggerParryZoom();
    }

    // 3. XÁC ĐỊNH CHÍNH XÁC CON QUÁI THỰC SỰ VUNG KIẾM BỊ PARRY
    // Ưu tiên cao nhất: Lấy trực tiếp attacker từ đòn đánh vừa bị gạt phăng
    Transform parriedEnemy = incomingAttacker;

    // Ưu tiên 2: Lấy từ LastAttacker trên Health
    if (parriedEnemy == null && health != null && health.LastAttacker != null)
    {
      parriedEnemy = health.LastAttacker;
    }

    // Ưu tiên 2 (Failsafe): Quét các quái xung quanh NHƯNG BẮT BUỘC ĐANG TẤN CÔNG (IsAttacking == true)
    if (parriedEnemy == null)
    {
      Collider[] cols = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 3.0f);
      foreach (Collider c in cols)
      {
        EnemyAIBase ai = c.GetComponentInParent<EnemyAIBase>();
        if (ai != null && ai.IsAttacking)
        {
          parriedEnemy = ai.transform.root;
          break;
        }
      }
    }

    // Ưu tiên 3: Nếu vẫn chưa thấy, kiểm tra quái đang lock-on (nhưng cũng phải đang vung kiếm IsAttacking)
    if (parriedEnemy == null && targetLockSystem != null && targetLockSystem.IsLockedOn)
    {
      EnemyAIBase lockAI = targetLockSystem.CurrentTarget.GetComponentInParent<EnemyAIBase>();
      if (lockAI != null && lockAI.IsAttacking)
      {
        parriedEnemy = lockAI.transform.root;
      }
    }

    // Ép CHỈ CON QUÁI BỊ PARRY bị ngắt chiêu và vỡ thế hoàn toàn
    if (parriedEnemy != null)
    {
      EnemyAIBase enemyAI = parriedEnemy.GetComponentInChildren<EnemyAIBase>();
      if (enemyAI != null)
      {
        enemyAI.InterruptAttack();
      }

      Animator enemyAnim = parriedEnemy.GetComponentInChildren<Animator>();
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

  public void TriggerHitlag(float duration, float scale = 0.05f)
  {
    if (hitStopCoroutine != null)
    {
      StopCoroutine(hitStopCoroutine);
      Time.timeScale = 1f;
    }
    hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, scale));
  }

  private IEnumerator HitStopRoutine(float duration, float scale)
  {
    Time.timeScale = scale;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1f;
    hitStopCoroutine = null;
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