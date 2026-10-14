using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private PlayerController playerController;
  [SerializeField] private CharacterController characterController;
  [SerializeField] private PlayerFeedback playerFeedback;

  [Header("Weapon & Damage Settings")]
  [SerializeField] private WeaponHitbox weaponHitbox;
  [SerializeField] private float attack1Damage = 15f;
  [SerializeField] private float attack2Damage = 20f;
  [SerializeField] private float attack3Damage = 35f;
  [SerializeField] private float chargeAttackDamage = 50f;
  [SerializeField] private float counterAttackDamage = 65f;   // Sát thương phản đòn cực lớn!

  [Header("Knockback Settings")]
  [SerializeField] private float attack1Knockback = 0f;
  [SerializeField] private float attack2Knockback = 0f;
  [SerializeField] private float attack3Knockback = 0.8f;
  [SerializeField] private float chargeAttackKnockback = 1.2f;
  [SerializeField] private float counterAttackKnockback = 2.0f; // Đẩy bay quái khi Counter

  [Header("Parry & Counter Settings")]
  [Tooltip("Khung thời gian vàng (giây) tính từ khi bấm Chuột Phải mà đòn chém của quái bị gạt")]
  [SerializeField] private float parryWindowDuration = 0.28f;
  [Tooltip("Khung thời gian (giây) cho phép người chơi bấm Chuột Trái để tung Counter Attack")]
  [SerializeField] private float counterWindowDuration = 1.2f;

  [Header("Layer Settings")]
  [SerializeField] private int swordLayerIndex = 1;
  [SerializeField] private float layerFadeSpeed = 6f;

  [Header("Charge Attack Settings")]
  [SerializeField] private float chargeThreshold = 0.35f;

  private Health health;
  private int comboStep = 0;
  private bool isAttacking = false;
  private bool canCombo = false;
  private bool comboBuffered = false;
  private float attackTimer = 0f;
  private float targetLayerWeight = 1f;

  // Biến Charge Attack
  private float holdTimer = 0f;
  private bool isHoldingMouse = false;

  // Biến Parry & Counter
  private bool isParryActive = false;
  private bool canCounterAttack = false;
  private float counterTimer = 0f;
  private float parryTimer = 0f;
  private Coroutine parryWindowCoroutine;

  // Animator Hashes
  private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
  private static readonly int JumpAttackTriggerHash = Animator.StringToHash("JumpAttack");
  private static readonly int ParryTriggerHash = Animator.StringToHash("Parry");
  private static readonly int CounterAttackTriggerHash = Animator.StringToHash("CounterAttack");

  private void Awake()
  {
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (playerController == null) playerController = GetComponentInParent<PlayerController>();
    if (characterController == null) characterController = GetComponentInParent<CharacterController>();
    if (playerFeedback == null) playerFeedback = GetComponentInParent<PlayerFeedback>();

    health = GetComponent<Health>();
    if (health == null) health = GetComponentInParent<Health>();

    targetLayerWeight = 1f;

    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
    }
  }

  private void Update()
  {
    HandleCombatInput();
    UpdateCounterWindow();

    // =========================================================================
    // XỬ LÝ THOÁT KẸT TỰ ĐỘNG
    // =========================================================================
    if (isAttacking)
    {
      attackTimer += Time.deltaTime;

      if (attackTimer > 0.25f && animator != null)
      {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!animator.IsInTransition(0) &&
           (stateInfo.IsName("Locomotion") || stateInfo.IsName("CombatLocomotion") || stateInfo.IsName("FreeLocomotion")))
        {
          ForceResetCombat();
        }
      }

      if (attackTimer >= 2.0f)
      {
        ForceResetCombat();
      }
    }

    // Failsafe nếu animation Block bị lỡ Event kết thúc
    if (isParryActive)
    {
      parryTimer += Time.deltaTime;
      if (parryTimer >= 1.2f)
      {
        OnParryAnimationEnd();
      }
    }
    else
    {
      parryTimer = 0f;
    }

    // Quản lý Layer tay phải
    if (animator != null && swordLayerIndex < animator.layerCount)
    {
      float currentWeight = animator.GetLayerWeight(swordLayerIndex);
      if (!Mathf.Approximately(currentWeight, targetLayerWeight))
      {
        float newWeight = Mathf.MoveTowards(currentWeight, targetLayerWeight, layerFadeSpeed * Time.deltaTime);
        animator.SetLayerWeight(swordLayerIndex, newWeight);
      }
    }
  }

  private void HandleCombatInput()
  {
    if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

    // Chặn input nếu đang lăn, đang bị quái đánh choáng (stagger), hoặc đã chết
    if (playerController != null && playerController.IsRolling) return;
    if (playerFeedback != null && playerFeedback.IsStaggered) return;
    if (health != null && health.IsDead) return;

    // CHỈ CHẶN canMove NẾU ĐÓ LÀ LỆNH KHÓA TỪ BÊN NGOÀI (Ví dụ: đang khom người lấy đà nhảy)
    // TUYỆT ĐỐI KHÔNG CHẶN nếu canMove bị khóa do chính đòn chém hoặc đỡ đòn đang chạy!
    if (playerController != null && !playerController.canMove && !isAttacking && !isParryActive) return;

    // =====================================================================
    // 1. BẤM CHUỘT PHẢI (RMB) -> KÍCH HOẠT PARRY / BLOCK
    // =====================================================================
    if (Mouse.current.rightButton.wasPressedThisFrame)
    {
      if (!isAttacking && !isParryActive)
      {
        ExecuteParry();
        return;
      }
    }

    // =====================================================================
    // 2. BẤM CHUỘT TRÁI (LMB)
    // =====================================================================
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
      // ƯU TIÊN SỐ 1: NẾU ĐANG TRONG CỬA SỔ PARRY THÀNH CÔNG -> TUNG COUNTER ATTACK!
      if (canCounterAttack)
      {
        ExecuteCounterAttack();
        return;
      }

      // Đánh thường / Nối Combo
      if (!isAttacking)
      {
        isHoldingMouse = true;
        holdTimer = 0f;
      }
      else
      {
        // Khi đang chém đòn 1 hoặc 2: nhận nhấp chuột để nối tiếp đòn sau
        if (comboStep > 0 && comboStep < 3)
        {
          comboBuffered = true;
          if (canCombo) AdvanceCombo();
        }
      }
    }

    // Giữ chuột đếm Charge Attack
    if (isHoldingMouse && Mouse.current.leftButton.isPressed)
    {
      holdTimer += Time.deltaTime;
      if (holdTimer >= chargeThreshold)
      {
        isHoldingMouse = false;
        ExecuteChargeAttack();
      }
    }

    // Nhả chuột nhanh (< 0.35s) -> Tung đòn chém thường 1
    if (isHoldingMouse && Mouse.current.leftButton.wasReleasedThisFrame)
    {
      isHoldingMouse = false;
      comboStep = 1;
      ExecuteNormalAttack();
    }
  }

  // =========================================================================
  // HỆ THỐNG PARRY & COUNTER
  // =========================================================================

  private void ExecuteParry()
  {
    isParryActive = true;
    parryTimer = 0f;
    isHoldingMouse = false;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetTrigger(ParryTriggerHash);

    if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
    parryWindowCoroutine = StartCoroutine(ParryWindowRoutine());
  }

  private IEnumerator ParryWindowRoutine()
  {
    yield return new WaitForSeconds(0.04f); // Chờ 0.04s vung thế đón đòn

    if (playerFeedback != null) playerFeedback.IsParrying = true;

    yield return new WaitForSeconds(parryWindowDuration);

    if (playerFeedback != null) playerFeedback.IsParrying = false;
  }

  public void OnParrySuccess()
  {
    if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
    if (playerFeedback != null) playerFeedback.IsParrying = false;

    // Mở cửa sổ phản đòn
    canCounterAttack = true;
    counterTimer = counterWindowDuration;
  }

  private void UpdateCounterWindow()
  {
    if (canCounterAttack)
    {
      counterTimer -= Time.deltaTime;
      if (counterTimer <= 0f)
      {
        canCounterAttack = false;
      }
    }
  }

  private void ExecuteCounterAttack()
  {
    canCounterAttack = false;
    isAttacking = true;
    isParryActive = false;
    isHoldingMouse = false;
    attackTimer = 0f;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetTrigger(CounterAttackTriggerHash);
  }

  // =========================================================================
  // CÁC ĐÒN TẤN CÔNG KHÁC
  // =========================================================================

  private void ExecuteNormalAttack()
  {
    isAttacking = true;
    canCombo = false;
    comboBuffered = false;
    attackTimer = 0f;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetInteger(ComboStepHash, comboStep);
    animator.SetTrigger(AttackTriggerHash);
  }

  private void AdvanceCombo()
  {
    canCombo = false;
    comboBuffered = false;
    comboStep++;
    ExecuteNormalAttack();
  }

  private void ExecuteChargeAttack()
  {
    isAttacking = true;
    comboStep = 0;
    canCombo = false;
    comboBuffered = false;
    attackTimer = 0f;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetTrigger(JumpAttackTriggerHash);
  }

  // =========================================================================
  // ANIMATION EVENTS
  // =========================================================================

  public void EnableComboWindow()
  {
    canCombo = true;
    if (comboBuffered && comboStep < 3)
    {
      AdvanceCombo();
    }
  }

  public void DisableComboWindow() => canCombo = false;

  public void OnAttackEnd() => ForceResetCombat();

  public void OnParryAnimationEnd()
  {
    isParryActive = false;
    if (playerFeedback != null) playerFeedback.IsParrying = false;
    targetLayerWeight = 1f;

    // Nếu không tung Counter thì mở khóa chân di chuyển lại bình thường
    if (!isAttacking && playerController != null)
    {
      playerController.UnlockMovement();
    }
  }

  public void ForceResetCombat()
  {
    // Ngắt Coroutine Parry nếu đang đếm dở
    if (parryWindowCoroutine != null)
    {
      StopCoroutine(parryWindowCoroutine);
      parryWindowCoroutine = null;
    }

    comboStep = 0;
    isAttacking = false;
    isParryActive = false;
    canCombo = false;
    comboBuffered = false;
    isHoldingMouse = false;
    holdTimer = 0f;
    attackTimer = 0f;
    targetLayerWeight = 1f;

    if (playerFeedback != null) playerFeedback.IsParrying = false;
    if (animator != null) animator.SetInteger(ComboStepHash, 0);
    if (playerController != null) playerController.UnlockMovement();
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
  }

  // =========================================================================
  // CÁC ANIMATION EVENTS ĐƯỢC CHIA TỶ LỆ HITLAG CHUẨN
  // =========================================================================

  // Đòn 1: Nhanh, mượt (Hitlag = 0.035s)
  public void EnableAttack1Hitbox() => weaponHitbox?.EnableHitbox(attack1Damage, attack1Knockback, 0.035f);

  // Đòn 2: Nhanh, dứt khoát (Hitlag = 0.045s)
  public void EnableAttack2Hitbox() => weaponHitbox?.EnableHitbox(attack2Damage, attack2Knockback, 0.045f);

  // Đòn 3: Đòn kết đầm tay (Hitlag = 0.08s)
  public void EnableAttack3Hitbox() => weaponHitbox?.EnableHitbox(attack3Damage, attack3Knockback, 0.08f);

  // Charge Attack - Hit 1: Chém mở màn (Hitlag = 0.05s)
  public void EnableChargeHit1Hitbox() => weaponHitbox?.EnableHitbox(chargeAttackDamage * 0.4f, 0f, 0.05f);

  // Charge Attack - Hit 2: Nhát bổ nặng dứt điểm (Hitlag = 0.095s)
  public void EnableChargeHit2Hitbox() => weaponHitbox?.EnableHitbox(chargeAttackDamage * 0.6f, chargeAttackKnockback, 0.095f);

  // Counter Attack: Nhát chém trừng phạt cực nặng (Hitlag = 0.11s)
  public void EnableCounterAttackHitbox() => weaponHitbox?.EnableHitbox(counterAttackDamage, counterAttackKnockback, 0.11f);

  public void DisableWeaponHitbox() => weaponHitbox?.DisableHitbox();
  // Thêm vào PlayerCombat để PlayerController có thể đọc được
  public bool IsBlockingOrParrying => isParryActive;
}