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
  [SerializeField] private PlayerStats playerStats;

  [Header("Weapon & Damage Settings")]
  [SerializeField] private WeaponHitbox weaponHitbox;
  [SerializeField] private float attack1Damage = 15f;
  [SerializeField] private float attack2Damage = 20f;
  [SerializeField] private float attack3Damage = 35f;
  [SerializeField] private float chargeAttackDamage = 50f;
  [SerializeField] private float counterAttackDamage = 65f;   // Sát thương phản đòn cực lớn!

  [Header("Skill Settings")]
  [SerializeField] private GameObject slashProjectilePrefab; // Prefab dải năng lượng (E)
  [SerializeField] private Transform projectileSpawnPoint;    // Điểm phóng đạn (đầu mũi kiếm)
  [SerializeField] private GameObject aoeSpellPrefab;         // Prefab AoeSpell (Q)
  [Tooltip("Aura kiếm mặc định / Fallback")]
  [SerializeField] private GameObject swordAuraVfx;
  [Tooltip("Aura kiếm màu xanh cho Skill E (Chém kiếm khí tầm xa)")]
  [SerializeField] private GameObject swordAuraBlueVfx;
  [Tooltip("Aura kiếm màu vàng cam cho Skill Q (Gồng nổ AOE chấn địa)")]
  [SerializeField] private GameObject swordAuraOrangeVfx;

  private enum ActiveSkillType { None, SkillE, SkillQ }
  private ActiveSkillType currentActiveSkill = ActiveSkillType.None;

  [Header("Skill Cooldowns")]
  [SerializeField] private float skillECooldown = 4.0f;
  [SerializeField] private float skillQCooldown = 8.0f;

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

  // Biến CD SKill
  private float skillETimer = 0f;
  private float skillQTimer = 0f;

  // Biến Charge Attack
  private float holdTimer = 0f;
  private bool isHoldingMouse = false;

  // Biến Parry & Counter
  private bool isParryActive = false;
  private bool canCounterAttack = false;
  private float counterTimer = 0f;
  private float parryTimer = 0f;
  private Coroutine parryWindowCoroutine;

  // Loại đòn tấn công đang thi triển (dùng cho tính lượng Spirit nhận được)
  private bool isChargeAttackActive = false;
  private bool isCounterAttackActive = false;

  // Animator Hashes
  private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
  private static readonly int JumpAttackTriggerHash = Animator.StringToHash("JumpAttack");
  private static readonly int ParryTriggerHash = Animator.StringToHash("Parry");
  private static readonly int CounterAttackTriggerHash = Animator.StringToHash("CounterAttack");
  private static readonly int SkillETriggerHash = Animator.StringToHash("Skill_E");
  private static readonly int SkillQTriggerHash = Animator.StringToHash("Skill_Q");

  private void Awake()
  {
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (playerController == null) playerController = GetComponentInParent<PlayerController>();
    if (characterController == null) characterController = GetComponentInParent<CharacterController>();
    if (playerFeedback == null) playerFeedback = GetComponentInParent<PlayerFeedback>();

    if (playerStats == null) playerStats = GetComponent<PlayerStats>();
    if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
    if (playerStats == null) playerStats = gameObject.AddComponent<PlayerStats>();

    health = GetComponent<Health>();
    if (health == null) health = GetComponentInParent<Health>();

    targetLayerWeight = 1f;

    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
      weaponHitbox.OnHitTarget += HandleWeaponHitTarget;
    }
  }

  private void OnDisable()
  {
    SetCounterAttackActive(false);
  }

  private void OnDestroy()
  {
    if (weaponHitbox != null)
    {
      weaponHitbox.OnHitTarget -= HandleWeaponHitTarget;
    }
  }

  /// <summary>
  /// Bật/Tắt trạng thái Counter Attack và đồng bộ I-Frames bảo vệ người chơi
  /// </summary>
  private void SetCounterAttackActive(bool active)
  {
    if (isCounterAttackActive == active) return;
    isCounterAttackActive = active;

    if (health != null)
    {
      health.SetInvulnerable(active);
    }
  }

  /// <summary>
  /// Nhận thông báo khi kiếm chém trúng quái vật -> Tích lũy Năng lượng Kỹ năng (Spirit)
  /// </summary>
  private void HandleWeaponHitTarget(IDamageable target, float damage)
  {
    if (playerStats == null) return;

    if (isCounterAttackActive)
    {
      playerStats.GainSpirit(playerStats.SpiritGainOnCounterHit);
    }
    else if (isChargeAttackActive)
    {
      playerStats.GainSpirit(playerStats.SpiritGainOnChargeHit);
    }
    else
    {
      playerStats.GainSpirit(playerStats.SpiritGainOnLightHit);
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

    // Đếm ngược Cooldown của Kỹ năng
    if (skillETimer > 0f) skillETimer -= Time.deltaTime;
    if (skillQTimer > 0f) skillQTimer -= Time.deltaTime;

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
    // =====================================================================
    // 3. THI TRIỂN KỸ NĂNG (E: SLASHRANGE, Q: AOE CHARGE)
    // =====================================================================
    if (!isAttacking && !isParryActive)
    {
      // Phím E: Phóng dải năng lượng tầm xa (Tiêu hao 50 Spirit)
      if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && skillETimer <= 0f)
      {
        if (playerStats != null && !playerStats.HasSpirit(playerStats.SkillECost))
        {
          Debug.Log("<color=cyan>[Skill E]</color> Không đủ Spirit! Cần: " + playerStats.SkillECost + " (Hiện có: " + playerStats.CurrentSpirit + ")");
          return;
        }

        if (playerStats != null)
        {
          playerStats.ConsumeSpirit(playerStats.SkillECost);
        }

        ExecuteSkillE();
        return;
      }

      // Phím Q: Gồng nổ AOE xung quanh (Tiêu hao 100 Spirit)
      if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && skillQTimer <= 0f)
      {
        if (playerStats != null && !playerStats.HasSpirit(playerStats.SkillQCost))
        {
          Debug.Log("<color=cyan>[Skill Q]</color> Không đủ Spirit! Cần: " + playerStats.SkillQCost + " (Hiện có: " + playerStats.CurrentSpirit + ")");
          return;
        }

        if (playerStats != null)
        {
          playerStats.ConsumeSpirit(playerStats.SkillQCost);
        }

        ExecuteSkillQ();
        return;
      }
    }
  }

  // =========================================================================
  // HỆ THỐNG PARRY & COUNTER
  // =========================================================================

  private void ExecuteParry()
  {
    if (playerStats != null && !playerStats.HasStamina(playerStats.ParryWhiffCost))
    {
      Debug.Log("<color=yellow>[Parry]</color> Không đủ thể lực để giơ kiếm đón đòn!");
      return;
    }

    if (playerStats != null)
    {
      playerStats.ConsumeStamina(playerStats.ParryWhiffCost);
    }

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

    // Hoàn trả lại thể lực do Parry chuẩn xác, đồng thời thưởng Spirit!
    if (playerStats != null)
    {
      playerStats.RestoreStamina(playerStats.ParryWhiffCost);
      playerStats.GainSpirit(playerStats.SpiritGainOnParrySuccess);
    }

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

    isChargeAttackActive = false;
    SetCounterAttackActive(true);

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
    if (playerStats != null && !playerStats.ConsumeStamina(playerStats.LightAttackCost))
    {
      comboStep = 0;
      canCombo = false;
      comboBuffered = false;
      isAttacking = false;
      return;
    }

    isChargeAttackActive = false;
    SetCounterAttackActive(false);

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
    if (playerStats != null && !playerStats.ConsumeStamina(playerStats.ChargeAttackCost))
    {
      isAttacking = false;
      return;
    }

    isChargeAttackActive = true;
    SetCounterAttackActive(false);

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

    isChargeAttackActive = false;
    SetCounterAttackActive(false);

    if (playerFeedback != null) playerFeedback.IsParrying = false;
    if (animator != null)
    {
      animator.SetInteger(ComboStepHash, 0);
      animator.speed = 1f;
    }
    if (playerController != null) playerController.UnlockMovement();
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
    DisableSwordAura();
  }

  private void ExecuteSkillE()
  {
    isAttacking = true;
    currentActiveSkill = ActiveSkillType.SkillE;
    skillETimer = skillECooldown;
    targetLayerWeight = 0f;

    if (playerController != null) playerController.LockMovement();
    animator.SetTrigger(SkillETriggerHash);
  }

  private void ExecuteSkillQ()
  {
    isAttacking = true;
    currentActiveSkill = ActiveSkillType.SkillQ;
    skillQTimer = skillQCooldown;
    targetLayerWeight = 0f;

    if (playerController != null) playerController.LockMovement();

    // Gồng chiêu lớn: BẬT HYPER ARMOR để quái đánh nhẹ không ngắt được chiêu!
    if (playerFeedback != null) playerFeedback.EnableHyperArmor();

    animator.SetTrigger(SkillQTriggerHash);
  }

  // =========================================================================
  // ANIMATION EVENTS CHO SKILLS
  // =========================================================================

  public void EnableSwordAura()
  {
    if (currentActiveSkill == ActiveSkillType.SkillE && swordAuraBlueVfx != null)
    {
      swordAuraBlueVfx.SetActive(true);
    }
    else if (currentActiveSkill == ActiveSkillType.SkillQ && swordAuraOrangeVfx != null)
    {
      swordAuraOrangeVfx.SetActive(true);
    }
    else if (swordAuraVfx != null)
    {
      swordAuraVfx.SetActive(true);
    }
  }

  public void DisableSwordAura()
  {
    if (swordAuraBlueVfx != null) swordAuraBlueVfx.SetActive(false);
    if (swordAuraOrangeVfx != null) swordAuraOrangeVfx.SetActive(false);
    if (swordAuraVfx != null) swordAuraVfx.SetActive(false);
    currentActiveSkill = ActiveSkillType.None;
  }

  /// <summary>
  /// Gắn Event này đúng thời điểm vung kiếm trong clip Skill E
  /// </summary>
  public void SpawnSlashProjectileEvent()
  {
    if (slashProjectilePrefab == null) return;

    Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : (transform.position + transform.forward + Vector3.up * 1.2f);
    Vector3 launchDir = transform.root.forward; // Lấy đúng hướng nhân vật đang nhìn tại thời điểm chém

    GameObject proj = Instantiate(slashProjectilePrefab, spawnPos, Quaternion.LookRotation(launchDir));
    ProjectileSlash slash = proj.GetComponent<ProjectileSlash>();
    if (slash != null)
    {
      slash.Launch(transform.root, launchDir);
    }
  }

  /// <summary>
  /// Gắn Event này đúng thời điểm cắm kiếm xuống đất / bộc phát chiêu trong clip Skill Q
  /// </summary>
  public void SpawnAoeSpellEvent()
  {
    if (aoeSpellPrefab == null) return;

    Vector3 spawnPos = transform.root.position;
    GameObject aoe = Instantiate(aoeSpellPrefab, spawnPos, Quaternion.identity);
    AoeSpell spell = aoe.GetComponent<AoeSpell>();
    if (spell != null)
    {
      spell.Initialize(transform.root);
    }
  }

  /// <summary>
  /// Gắn ở cuối 2 clip Skill để trả lại quyền điều khiển
  /// </summary>
  public void OnSkillAnimationEnd()
  {
    DisableSwordAura();
    if (playerFeedback != null) playerFeedback.DisableHyperArmor();
    if (animator != null) animator.speed = 1f;
    ForceResetCombat();
  }

  /// <summary>
  /// Thay đổi tốc độ animation linh hoạt theo từng frame (gọi qua Animation Event trên timeline clip).
  /// Ví dụ Skill Q: Đoạn đầu gồng đặt SetAnimatorSpeed(0.6f), đoạn vung kiếm đập xuống đất đặt SetAnimatorSpeed(2.2f).
  /// </summary>
  public void SetAnimatorSpeed(float speed)
  {
    if (animator != null)
    {
      animator.speed = speed;
    }
  }

  // Bonus sát thương từ trang bị trong Kho đồ
  private float AttackBonus => InfiniteLabyrinth.Inventory.InventoryManager.HasInstance 
    ? InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalAttackBonus() 
    : 0f;

  // =========================================================================
  // CÁC ANIMATION EVENTS ĐƯỢC CHIA TỶ LỆ HITLAG CHUẨN
  // =========================================================================

  // Đòn 1: Nhanh, mượt (Hitlag = 0.035s)
  public void EnableAttack1Hitbox() => weaponHitbox?.EnableHitbox(attack1Damage + AttackBonus, attack1Knockback, 0.035f);

  // Đòn 2: Nhanh, dứt khoát (Hitlag = 0.045s)
  public void EnableAttack2Hitbox() => weaponHitbox?.EnableHitbox(attack2Damage + AttackBonus, attack2Knockback, 0.045f);

  // Đòn 3: Đòn kết đầm tay (Hitlag = 0.08s)
  public void EnableAttack3Hitbox() => weaponHitbox?.EnableHitbox(attack3Damage + AttackBonus * 1.5f, attack3Knockback, 0.08f);

  // Charge Attack - Hit 1: Chém mở màn (Hitlag = 0.05s)
  public void EnableChargeHit1Hitbox() => weaponHitbox?.EnableHitbox((chargeAttackDamage + AttackBonus) * 0.4f, 0f, 0.05f);

  // Charge Attack - Hit 2: Nhát bổ nặng dứt điểm (Hitlag = 0.095s)
  public void EnableChargeHit2Hitbox() => weaponHitbox?.EnableHitbox((chargeAttackDamage + AttackBonus) * 0.6f, chargeAttackKnockback, 0.095f);

  // Counter Attack: Nhát chém trừng phạt cực nặng (Hitlag = 0.11s)
  public void EnableCounterAttackHitbox() => weaponHitbox?.EnableHitbox(counterAttackDamage + AttackBonus * 2.0f, counterAttackKnockback, 0.11f);

  public void DisableWeaponHitbox() => weaponHitbox?.DisableHitbox();
  // Thêm vào PlayerCombat để PlayerController có thể đọc được
  public bool IsBlockingOrParrying => isParryActive;
  public bool IsAttacking => isAttacking;
}