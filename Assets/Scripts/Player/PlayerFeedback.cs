using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private TargetLockSystem targetLockSystem;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 35f;

  [Header("Stagger / Knockback")]
  [SerializeField] private float staggerDuration = 0.5f; // Thời gian đơ người khi bị vỡ thế
  [SerializeField] private float knockbackDuration = 0.15f;

  // Các cổng kết nối cho Combat nâng cao (Roll, Hyper Armor, Parry, Skills)
  public bool HasHyperArmor { get; set; } = false;   // Bật khi gồng Skill 1, Skill 2, Jump Attack
  public bool IsParrying { get; set; } = false;       // Bật trong khung cửa sổ Parry (sắp tới)

  private Health health;
  private PlayerController playerController;
  private CharacterController characterController;

  private float currentPoise;
  private float lastHitTime;
  private Coroutine staggerCoroutine;
  private Coroutine knockbackCoroutine;

  // Animator Hashes
  private static readonly int GetHitHash = Animator.StringToHash("GetHit");
  private static readonly int DieHash = Animator.StringToHash("Die");

  private void Awake()
  {
    health = GetComponent<Health>();
    playerController = GetComponent<PlayerController>();
    characterController = GetComponent<CharacterController>();

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (targetLockSystem == null) targetLockSystem = GetComponent<TargetLockSystem>();

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
    // Tự động hồi phục thăng bằng sau khi không bị quái đánh một khoảng thời gian
    if (currentPoise < maxPoise && Time.time >= lastHitTime + poiseRecoveryDelay)
    {
      currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
    }
  }

  // =========================================================================
  // XỬ LÝ KHI BỊ QUÁI CHÉM TRÚNG
  // =========================================================================
  private void HandleTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    if (health.IsDead) return;

    // 1. I-FRAME ROLL: Nếu người chơi đang lăn né đòn -> BỎ QUA HOÀN TOÀN SÁT THƯƠNG
    if (playerController.IsRolling)
    {
      // Trả lại máu vì đã né thành công nhát chém
      // (Hoặc trong WeaponHitbox kiểm tra trước khi ApplyDamage)
      return;
    }

    // 2. PARRY CHECK (Cổng chờ cho cơ chế Parry):
    if (IsParrying)
    {
      // Đỡ gạt thành công! Bỏ qua sát thương, không khựng đòn
      Debug.Log("<color=yellow>[Parry]</color> Đã kích hoạt Parry thành công!");
      return;
    }

    lastHitTime = Time.time;

    // 3. TÍNH LƯỢNG POISE BỊ TRỪ
    // Đòn cào nhẹ trừ 30 Poise, đòn búa tạ/nặng trừ 70 - 100 Poise
    float poiseDamageTaken = knockbackDistance > 0.8f ? 70f : 35f;
    currentPoise -= poiseDamageTaken;

    // 4. KIỂM TRA HYPER ARMOR (Khi đang gồng Skill 1, Skill 2, Jump Attack)
    if (HasHyperArmor)
    {
      // Chấp nhận đổi máu, tiếp tục vung kiếm/tung chiêu, không chạy animation GetHit
      return;
    }

    // 5. NẾU BỊ PHÁ THẾ (VỠ POISE) HOẶC DÍNH ĐÒN NẶNG -> KÍCH HOẠT GETHIT
    if (currentPoise <= 0f || knockbackDistance >= 1.2f)
    {
      currentPoise = maxPoise; // Reset lại thăng bằng sau khi vỡ thế

      // Kích hoạt animation giật mình
      animator.SetTrigger(GetHitHash);

      // Khóa chân người chơi trong chốc lát
      if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
      staggerCoroutine = StartCoroutine(StaggerRoutine());

      // Đẩy lùi lùi nhẹ về sau theo hướng nhát kiếm của quái
      if (knockbackDistance > 0f)
      {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
      }
    }
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

  // =========================================================================
  // XỬ LÝ KHI NGƯỜI CHƠI CHẾT (DEATH)
  // =========================================================================
  private void HandleDeath()
  {
    // 1. Dừng các hiệu ứng khựng / đẩy lùi
    if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
    if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

    // 2. Kích hoạt animation ngã gục
    animator.SetTrigger(DieHash);

    // Tắt hẳn script PlayerController để dừng Update() hoàn toàn
    playerController.enabled = false;

    // Tắt CharacterController
    characterController.enabled = false;

    // 4. Bung khóa mục tiêu nếu đang Lock-on
    if (targetLockSystem != null)
    {
      targetLockSystem.UnlockTarget();
    }

    // 5. Tắt va chạm để quái không húc đẩy xác
    characterController.enabled = false;

    Debug.Log("<color=red>[YOU DIED]</color> Người chơi đã ngã xuống.");
  }
}