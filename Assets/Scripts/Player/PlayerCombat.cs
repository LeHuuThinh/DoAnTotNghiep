using System.Collections;
using System.Collections.Generic;
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
  [SerializeField] private PlayerSlashVFX playerSlashVFX;

  [Header("Weapon & Damage Settings")]
  [SerializeField] private WeaponHitbox weaponHitbox;
  public WeaponHitbox WeaponHitbox => weaponHitbox;
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
  [Tooltip("Aura kiếm màu đỏ / vàng kim cho Tuyệt kỹ Ult R")]
  [SerializeField] private GameObject swordAuraUltVfx;

  [Header("Ultimate Skill (R) Settings")]
  [Tooltip("Sát thương cơ bản của Tuyệt kỹ Ult R")]
  [SerializeField] private float ultDamage = 140f;
  [Tooltip("Lực đẩy lùi (Knockback) của Tuyệt kỹ Ult R")]
  [SerializeField] private float ultKnockback = 3.5f;
  [Tooltip("Sát thương Poise (phá thế quái vật) của Tuyệt kỹ Ult R")]
  [SerializeField] private float ultPoiseDamage = 180f;
  [Tooltip("Bán kính quét diện rộng (m) của Hitbox chém Ult R")]
  [SerializeField] private float ultHitboxRadius = 5.5f;
  [Tooltip("Độ lệch tâm quét về phía trước mặt nhân vật (m)")]
  [SerializeField] private float ultHitboxForwardOffset = 2.2f;
  [Tooltip("Độ cao tâm quét sát thương so với chân nhân vật (m). 1.15m khớp chuẩn vị trí vệt chém Slash VFX")]
  [SerializeField] private float ultHitboxHeightOffset = 1.15f;
  [Tooltip("Góc quét hình quạt phía trước mặt (độ). 240 = quét rộng trước mặt, 360 = trọn vẹn toàn bộ 360 độ")]
  [Range(60f, 360f)]
  [SerializeField] private float ultHitboxAngle = 240f;
  [Header("Skill Ult (R) Hitbox Visualizer (Gizmos)")]
  [Tooltip("Bật hiển thị khung Hitbox Ult trong cửa sổ Scene View để dễ dàng căn chỉnh khớp vệt chém")]
  [SerializeField] private bool showUltHitboxGizmos = true;
  [SerializeField] private Color ultHitboxGizmoColor = new Color(1f, 0.22f, 0.22f, 0.35f);
  [Header("Skill Ult (R) Cinematic & Slow Motion")]
  [Tooltip("Thời gian đơ game (freeze frame / hitlag) ngay khoảnh khắc chuẩn bị chém (giây)")]
  [SerializeField] private float ultFreezeDuration = 0.1f;
  [Tooltip("Thời gian làm chậm thực tế khi vung nhát chém Ult (giây thực tế). 0.8s - 1.2s tạo cảm giác chém ngang đầm chắc, mãn nhãn")]
  [SerializeField] private float ultSlowMoDuration = 0.95f;
  [Tooltip("Thời gian khựng đơ game (giây) ngay khoảnh khắc nhát chém Ult chạm trúng quái vật (0.2s - 0.25s tạo độ đanh uy lực cực đại)")]
  [SerializeField] private float ultImpactHitlag = 0.22f;
  [Tooltip("Tốc độ thời gian khi slow motion (0.28 = làm chậm ~72%)")]
  [Range(0.05f, 0.5f)]
  [SerializeField] private float ultSlowMoTimeScale = 0.28f;
  [Tooltip("VFX chớp sáng ngang trên lưỡi kiếm ngay trước khi chém")]
  [SerializeField] private GameObject swordGleamVfxPrefab;
  [Tooltip("Độ lệch vị trí chớp sáng dọc theo lưỡi kiếm (m). Dương = tiến về mũi kiếm, Âm = lùi về chuôi")]
  [SerializeField] private float ultGleamBladeOffset = 0.15f;
  [Tooltip("Góc xoay điều chỉnh thêm của VFX chớp sáng Gleam (Euler Angles)")]
  [SerializeField] private Vector3 ultGleamRotationOffset = Vector3.zero;
  [Tooltip("Tỷ lệ phóng to/thu nhỏ của VFX chớp sáng Gleam")]
  [SerializeField] private float ultGleamScale = 1.0f;
  [Tooltip("Điểm neo thân kiếm / lưỡi kiếm để phát sinh chớp sáng")]
  [SerializeField] private Transform swordTransform;

  private enum ActiveSkillType { None, SkillE, SkillQ, SkillR }
  private ActiveSkillType currentActiveSkill = ActiveSkillType.None;

  [Header("Skill Cooldowns")]
  [SerializeField] private float skillECooldown = 6.0f;
  [SerializeField] private float skillQCooldown = 10.0f;
  [SerializeField] private float skillRCooldown = 20.0f;

  [Header("Knockback Settings")]
  [SerializeField] private float attack1Knockback = 0f;
  [SerializeField] private float attack2Knockback = 0f;
  [SerializeField] private float attack3Knockback = 0.8f;
  [SerializeField] private float chargeAttackKnockback = 1.2f;
  [SerializeField] private float counterAttackKnockback = 2.0f; // Đẩy bay quái khi Counter

  [Header("Hitlag Settings")]
  [Tooltip("Thời gian khựng (giây) khi đòn chém thường 1 trúng mục tiêu")]
  [SerializeField] private float attack1Hitlag = 0.035f;

  [Tooltip("Thời gian khựng (giây) khi đòn chém thường 2 trúng mục tiêu")]
  [SerializeField] private float attack2Hitlag = 0.045f;

  [Tooltip("Thời gian khựng (giây) khi đòn chém thường 3 (đòn kết chuỗi) trúng mục tiêu")]
  [SerializeField] private float attack3Hitlag = 0.08f;

  [Tooltip("Thời gian khựng (giây) khi đòn Gồng Lực (Charge) nhát 1 trúng mục tiêu")]
  [SerializeField] private float chargeHit1Hitlag = 0.05f;

  [Tooltip("Thời gian khựng (giây) khi đòn Gồng Lực (Charge) nhát 2 trúng mục tiêu")]
  [SerializeField] private float chargeHit2Hitlag = 0.095f;

  [Tooltip("Thời gian khựng (giây) khi đòn Phản Đòn (Counter Attack) trúng mục tiêu")]
  [SerializeField] private float counterAttackHitlag = 0.11f;

  [Tooltip("Thời gian khựng (giây) khi đòn đánh KẾT LIỄU hạ gục quái vật (0.15s - 0.2s tạo độ đanh và điểm nhấn dứt điểm cực đã tay)")]
  [SerializeField] private float lethalHitlag = 0.18f;

  [Header("Poise Damage Settings")]
  [Tooltip("Sát thương làm mất thăng bằng của đòn chém 1 (Đòn nhẹ, quái đứng vững)")]
  [SerializeField] private float attack1PoiseDamage = 25f;

  [Tooltip("Sát thương làm mất thăng bằng của đòn chém 2 (Đòn nhẹ, quái đứng vững)")]
  [SerializeField] private float attack2PoiseDamage = 25f;

  [Tooltip("Sát thương làm mất thăng bằng của đòn chém 3 (Đòn kết chuỗi, vỡ thế quái)")]
  [SerializeField] private float attack3PoiseDamage = 55f;

  [Tooltip("Sát thương Poise đòn Gồng nhát 1")]
  [SerializeField] private float chargeHit1PoiseDamage = 35f;

  [Tooltip("Sát thương Poise đòn Gồng nhát 2 (Bổ mạnh phá thế)")]
  [SerializeField] private float chargeHit2PoiseDamage = 80f;

  [Tooltip("Sát thương Poise đòn Phản công Counter (Lập tức phá vỡ thế mọi quái)")]
  [SerializeField] private float counterAttackPoiseDamage = 120f;

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
  private float skillRTimer = 0f;
  private Coroutine ultTimeDilationCoroutine;
  private bool isUltSlowMoActive = false;

  /// <summary>
  /// Trạng thái toàn cục báo hiệu chiêu Ult đang trong pha Slow Motion để các hệ thống Hitlag không đè mất nhịp làm chậm
  /// </summary>
  public static bool IsAnyUltSlowMoActive { get; private set; }
  public static float ActiveUltSlowMoScale { get; private set; } = 0.28f;

  /// <summary>
  /// Lấy tốc độ thời gian chuẩn sau khi kết thúc một hiệu ứng Hitlag.
  /// Nếu người chơi đang trong pha Ult chém ngang, trả về tốc độ slow-mo của Ult thay vì ép về 1.0f!
  /// </summary>
  public static float GetActiveCombatTimeScale()
  {
    if (IsAnyUltSlowMoActive)
    {
      return ActiveUltSlowMoScale;
    }
    return PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f;
  }

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
  private static readonly int SkillRTriggerHash = Animator.StringToHash("Skill_R");

  private void Awake()
  {
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (playerController == null) playerController = GetComponentInParent<PlayerController>();
    if (characterController == null) characterController = GetComponentInParent<CharacterController>();
    if (playerFeedback == null) playerFeedback = GetComponentInParent<PlayerFeedback>();
    if (playerSlashVFX == null) playerSlashVFX = GetComponentInParent<PlayerSlashVFX>();
    if (playerSlashVFX == null) playerSlashVFX = GetComponent<PlayerSlashVFX>();
    if (playerSlashVFX == null) playerSlashVFX = GetComponentInChildren<PlayerSlashVFX>();

    if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
    if (playerStats == null) playerStats = GetComponent<PlayerStats>();
    if (playerStats == null) playerStats = GetComponentInChildren<PlayerStats>();

    health = GetComponent<Health>();
    if (health == null) health = GetComponentInParent<Health>();

    targetLayerWeight = 1f;

    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
      weaponHitbox.OnHitTarget += HandleWeaponHitTarget;
      if (swordTransform == null) swordTransform = weaponHitbox.transform;
    }
  }

  private void OnDisable()
  {
    SetCounterAttackActive(false);
    if (isUltSlowMoActive)
    {
      if (ultTimeDilationCoroutine != null)
      {
        StopCoroutine(ultTimeDilationCoroutine);
        ultTimeDilationCoroutine = null;
      }
      isUltSlowMoActive = false;
      IsAnyUltSlowMoActive = false;
      float restoreScale = PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f;
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
    }
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

      float maxAttackTimeout = (currentActiveSkill == ActiveSkillType.SkillR) ? 4.5f : 2.0f;
      if (attackTimer >= maxAttackTimeout)
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
    if (skillRTimer > 0f) skillRTimer -= Time.deltaTime;

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

      // Phím R: Tuyệt kỹ Tối thượng Ultimate - Chém Ngang Càn Quét (Tiêu hao 200 Spirit - Max Thanh)
      if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && skillRTimer <= 0f)
      {
        float rCost = playerStats != null ? playerStats.SkillRCost : 200f;
        if (playerStats != null && !playerStats.HasSpirit(rCost))
        {
          Debug.Log("<color=red>[Skill Ult]</color> Chưa đủ Spirit để tung Tuyệt Kỹ! Cần: " + rCost + " (Hiện có: " + playerStats.CurrentSpirit + ")");
          return;
        }

        if (playerStats != null)
        {
          playerStats.ConsumeSpirit(rCost);
        }

        ExecuteSkillR();
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

    // Chỉ dập tắt Time.timeScale nếu KHÔNG đang trong pha Ult Slow-mo tự nhiên
    if (!isUltSlowMoActive)
    {
      if (ultTimeDilationCoroutine != null)
      {
        StopCoroutine(ultTimeDilationCoroutine);
        ultTimeDilationCoroutine = null;
      }
      float restoreScale = PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f;
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
    }

    if (health != null) health.SetInvulnerable(false);
    if (playerFeedback != null)
    {
      playerFeedback.IsParrying = false;
      playerFeedback.DisableHyperArmor();
    }
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

  private void ExecuteSkillR()
  {
    isAttacking = true;
    currentActiveSkill = ActiveSkillType.SkillR;
    skillRTimer = skillRCooldown;
    targetLayerWeight = 0f;

    if (playerController != null) playerController.LockMovement();

    // Tuyệt kỹ Tối thượng 200 Spirit: Bật Hyper Armor và Invulnerable để quái không ngắt được chiêu
    if (playerFeedback != null) playerFeedback.EnableHyperArmor();
    if (health != null) health.SetInvulnerable(true);

    EnableSwordAura();

    animator.SetTrigger(SkillRTriggerHash);
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
    else if (currentActiveSkill == ActiveSkillType.SkillR)
    {
      if (swordAuraUltVfx != null) swordAuraUltVfx.SetActive(true);
      else if (swordAuraOrangeVfx != null) swordAuraOrangeVfx.SetActive(true);
      else if (swordAuraVfx != null) swordAuraVfx.SetActive(true);
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
    if (swordAuraUltVfx != null) swordAuraUltVfx.SetActive(false);
    if (swordAuraVfx != null) swordAuraVfx.SetActive(false);
    currentActiveSkill = ActiveSkillType.None;
  }

  /// <summary>
  /// Gắn Event này đúng thời điểm vung kiếm trong clip Skill E (Chỉ lo việc phóng đạn kiếm khí)
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
  /// Gắn Event này đúng thời điểm cắm kiếm xuống đất / bộc phát chiêu trong clip Skill Q (Chỉ lo việc sinh phép nổ AOE)
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
  /// Animation Event gọi vệt chém Skill E độc lập nếu muốn căn nhịp riêng biệt với việc phóng đạn
  /// </summary>
  public void PlaySkillESlash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.SkillE);

  /// <summary>
  /// Animation Event gọi vệt chém Skill Q độc lập nếu muốn căn nhịp riêng biệt với việc nổ AOE
  /// </summary>
  public void PlaySkillQSlash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.SkillQ);

  /// <summary>
  /// Animation Event: Khoảnh khắc chuẩn bị chém ngang của Skill Ult
  /// Kích hoạt chớp sáng ngang trên kiếm + Đơ game (freeze frame / hitlag) 0.1s + Camera Zoom Punch
  /// </summary>
  public void OnUltPreSlashGleam()
  {
    SpawnSwordGleamVfx();

    // Giật camera Zoom Punch nén lực
    if (playerController != null && playerController.ThirdPersonCamera != null)
    {
      playerController.ThirdPersonCamera.TriggerParryZoom();
    }

    // Đơ game 0.1s (Freeze Frame unscaled)
    if (ultTimeDilationCoroutine != null) StopCoroutine(ultTimeDilationCoroutine);
    ultTimeDilationCoroutine = StartCoroutine(UltPreSlashFreezeRoutine());
  }

  /// <summary>
  /// Alias ngắn gọn cho Animation Event chớp sáng kiếm Ult
  /// </summary>
  public void OnUltGleam() => OnUltPreSlashGleam();

  private IEnumerator UltPreSlashFreezeRoutine()
  {
    float originalScale = Time.timeScale > 0.01f ? Time.timeScale : 1.0f;
    Time.timeScale = 0.02f; // Khựng đơ thế giới
    Time.fixedDeltaTime = 0.02f * Time.timeScale;
    yield return new WaitForSecondsRealtime(ultFreezeDuration);
    float restoreScale = originalScale > 0.05f ? originalScale : (PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f);
    Time.timeScale = restoreScale;
    Time.fixedDeltaTime = 0.02f * restoreScale;
  }

  private void SpawnSwordGleamVfx()
  {
    GameObject gleamPrefab = swordGleamVfxPrefab;
    if (gleamPrefab == null)
    {
#if UNITY_EDITOR
      gleamPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Skill/Ult Gleam VFX.prefab");
#endif
      if (gleamPrefab == null)
      {
        gleamPrefab = Resources.Load<GameObject>("Ult Gleam VFX");
      }
      if (gleamPrefab == null)
      {
        gleamPrefab = Resources.Load<GameObject>("Light Flare");
      }
#if UNITY_EDITOR
      if (gleamPrefab == null)
      {
        gleamPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Other/Light Flare.prefab");
      }
      if (gleamPrefab == null)
      {
        gleamPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab");
      }
#endif
    }

    Transform root = transform.root;
    Transform parentTarget = swordTransform != null ? swordTransform : (weaponHitbox != null ? weaponHitbox.transform : null);

    Vector3 gleamPos;
    Quaternion gleamRot;

    BoxCollider box = weaponHitbox != null ? weaponHitbox.GetComponent<BoxCollider>() : null;
    if (parentTarget != null && box != null)
    {
      // Lấy đúng tâm lưỡi kiếm (local center Y ~ 0.81m) kèm độ lệch tiến về phía mũi kiếm
      gleamPos = parentTarget.TransformPoint(box.center + Vector3.up * ultGleamBladeOffset);
      gleamRot = parentTarget.rotation * Quaternion.Euler(ultGleamRotationOffset);
    }
    else if (parentTarget != null)
    {
      gleamPos = parentTarget.position + parentTarget.up * (0.8f + ultGleamBladeOffset);
      gleamRot = parentTarget.rotation * Quaternion.Euler(ultGleamRotationOffset);
    }
    else
    {
      gleamPos = root.position + root.forward * 0.9f + Vector3.up * 1.15f;
      gleamRot = root.rotation * Quaternion.Euler(ultGleamRotationOffset);
    }

    if (gleamPrefab != null)
    {
      // Gắn làm con của parentTarget (thanh kiếm) để khi nhân vật cử động trong 0.1s, chớp sáng bám chặt theo kiếm
      GameObject gleamInst = parentTarget != null
        ? Instantiate(gleamPrefab, gleamPos, gleamRot, parentTarget)
        : Instantiate(gleamPrefab, gleamPos, gleamRot);

      // Giữ nguyên tỷ lệ chuẩn của prefab, nhân theo thanh trượt scale
      Vector3 origScale = gleamPrefab.transform.localScale;
      gleamInst.transform.localScale = origScale * ultGleamScale;

      // Kích hoạt tất cả ParticleSystem con và ép useUnscaledTime = true để không bị đơ theo TimeScale
      ParticleSystem[] allPS = gleamInst.GetComponentsInChildren<ParticleSystem>(true);
      foreach (ParticleSystem ps in allPS)
      {
        var main = ps.main;
        main.useUnscaledTime = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ps.Play(true);
      }

      Destroy(gleamInst, 1.2f);
    }
  }

  /// <summary>
  /// Animation Event: Khoảnh khắc vung chém ngang của Skill Ult
  /// Kích hoạt Slow motion 0.5s + Vệt kiếm khổng lồ + Quét Hitbox siêu to
  /// </summary>
  public void OnUltSlashRelease()
  {
    // 1. Slow motion trong lúc chém ngang
    TriggerUltSlowMo();

    // 2. Kích hoạt vệt chém ngang khổng lồ
    PlaySkillUltSlash();

    // 3. Quét Hitbox siêu to
    TriggerUltHitbox();
  }

  /// <summary>
  /// Alias ngắn gọn cho Animation Event chém ngang của Ult (tương ứng tên event OnUltSlash trên timeline)
  /// </summary>
  public void OnUltSlash() => OnUltSlashRelease();

  /// <summary>
  /// Animation Event độc lập: Kích hoạt vệt chém khổng lồ cho Skill Ult
  /// </summary>
  public void PlaySkillUltSlash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.SkillUlt);
  public void PlayUltSlash() => PlaySkillUltSlash();

  /// <summary>
  /// Animation Event độc lập: Kích hoạt Slow Motion 0.5s cho Skill Ult
  /// </summary>
  public void TriggerUltSlowMo()
  {
    if (ultTimeDilationCoroutine != null) StopCoroutine(ultTimeDilationCoroutine);
    ultTimeDilationCoroutine = StartCoroutine(UltSlashSlowMoRoutine());
  }
  public void OnUltSlowMo() => TriggerUltSlowMo();

  /// <summary>
  /// Animation Event độc lập: Kích hoạt quét Hitbox sát thương diện rộng của Ult
  /// </summary>
  public void TriggerUltHitbox() => ExecuteUltGiganticHitbox();
  public void OnUltSlashHitbox() => TriggerUltHitbox();

  private IEnumerator UltSlashSlowMoRoutine()
  {
    isUltSlowMoActive = true;
    IsAnyUltSlowMoActive = true;
    ActiveUltSlowMoScale = ultSlowMoTimeScale;

    // Nếu hiện tại không có hitlag nào đang khựng game (ở 0.05f) thì áp dụng ngay tốc độ slow-mo
    if (!PlayerFeedback.IsAnyHitlagActive && !WeaponHitbox.IsHitlagActive)
    {
      Time.timeScale = ultSlowMoTimeScale; // 0.28x
      Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    // Đếm thời lượng slow-mo bằng unscaled time, nhưng TẠM DỪNG đếm khi hitlag đang diễn ra
    // để sau khi khựng đơ 0.22s xong, người chơi vẫn được chiêm ngưỡng trọn vẹn slow-mo!
    float elapsed = 0f;
    while (elapsed < ultSlowMoDuration)
    {
      if (!PlayerFeedback.IsAnyHitlagActive && !WeaponHitbox.IsHitlagActive)
      {
        elapsed += Time.unscaledDeltaTime;
      }
      yield return null;
    }

    isUltSlowMoActive = false;
    IsAnyUltSlowMoActive = false;

    float restoreScale = PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f;
    Time.timeScale = restoreScale;
    Time.fixedDeltaTime = 0.02f * restoreScale;
    ultTimeDilationCoroutine = null;
  }

  /// <summary>
  /// Quét sát thương hình quạt diện rộng cực đại phía trước người chơi
  /// </summary>
  private void ExecuteUltGiganticHitbox()
  {
    Transform root = transform.root;
    Vector3 origin = root.position;
    Vector3 center = origin + root.forward * ultHitboxForwardOffset + Vector3.up * ultHitboxHeightOffset;

    LayerMask targetLayers = (weaponHitbox != null && weaponHitbox.TargetLayers.value != 0)
      ? weaponHitbox.TargetLayers
      : ((1 << 9) | (1 << 8)); // Enemy (9), Damageable (8)

    Collider[] colliders = Physics.OverlapSphere(center, ultHitboxRadius, targetLayers);
    HashSet<IDamageable> damagedEntities = new HashSet<IDamageable>();

    float totalDamage = ultDamage + TotalAttackBonus * 2.5f;

    foreach (Collider col in colliders)
    {
      if (col == null || col.transform.root == root) continue;

      // Kiểm tra góc quét hình quạt phía trước nếu ultHitboxAngle < 360
      if (ultHitboxAngle < 359f)
      {
        Vector3 dirToTarget = col.bounds.center - (origin + Vector3.up * ultHitboxHeightOffset);
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude > 0.001f)
        {
          float angle = Vector3.Angle(root.forward, dirToTarget.normalized);
          if (angle > ultHitboxAngle * 0.5f) continue;
        }
      }

      IDamageable damageable = col.GetComponentInParent<IDamageable>();
      if (damageable != null && !damagedEntities.Contains(damageable))
      {
        damagedEntities.Add(damageable);

        Vector3 hitPoint = col.ClosestPoint(center);
        Vector3 hitDirection = (col.transform.position - origin);
        hitDirection.y = 0.08f;
        hitDirection.Normalize();

        // 1. Gây sát thương Poise cực mạnh (Lập tức phá vỡ thế mọi quái thường)
        Health targetHealth = damageable as Health ?? (damageable as Component)?.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
          targetHealth.SetIncomingPoiseDamage(ultPoiseDamage);
        }

        // 2. Kích hoạt Impact VFX tại điểm chạm
        if (weaponHitbox != null)
        {
          weaponHitbox.SpawnHitImpactVfx(hitPoint, hitDirection);
        }

        // 3. Gây sát thương và đẩy văng (Knockback mạnh)
        damageable.TakeDamage(totalDamage, hitPoint, hitDirection, ultKnockback, root);
      }
    }

    // Kích hoạt Impact Hitlag 0.22s khi nhát chém trúng ít nhất 1 mục tiêu
    if (damagedEntities.Count > 0 && ultImpactHitlag > 0f)
    {
      playerFeedback?.TriggerHitlag(ultImpactHitlag, 0.05f);
    }
  }

  /// <summary>
  /// Gắn ở cuối các clip Skill để trả lại quyền điều khiển
  /// </summary>
  public void OnSkillAnimationEnd()
  {
    DisableSwordAura();
    // Chỉ dập tắt Time.timeScale nếu KHÔNG đang trong pha Ult Slow-mo tự nhiên
    if (!isUltSlowMoActive)
    {
      if (ultTimeDilationCoroutine != null)
      {
        StopCoroutine(ultTimeDilationCoroutine);
        ultTimeDilationCoroutine = null;
      }
      float restoreScale = PlayerSlashVFX.ActiveSlowMoScale > 0f ? PlayerSlashVFX.ActiveSlowMoScale : 1.0f;
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
    }
    if (health != null) health.SetInvulnerable(false);
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

  // Bonus sát thương từ trang bị và điểm tiềm năng
  public float BaseAttackDamage => attack1Damage;
  public float CurrentAttackBonus => TotalAttackBonus;
  public float TotalAttackDamage => attack1Damage + TotalAttackBonus;

  private float AttackBonus => InfiniteLabyrinth.Inventory.InventoryManager.HasInstance 
    ? InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalAttackBonus() 
    : 0f;

  private float ProgressionAttackBonus => InfiniteLabyrinth.Progression.PlayerProgression.HasInstance
    ? InfiniteLabyrinth.Progression.PlayerProgression.Instance.BonusAttack
    : 0f;

  private float TotalAttackBonus => AttackBonus + ProgressionAttackBonus;

  // =========================================================================
  // CÁC ANIMATION EVENTS ĐƯỢC CHIA TỶ LỆ HITLAG & SLASH TRAIL VFX
  // =========================================================================

  // Đòn 1: Nhanh, mượt
  public void EnableAttack1Hitbox()
  {
    weaponHitbox?.EnableHitbox(attack1Damage + TotalAttackBonus, attack1Knockback, attack1Hitlag, lethalHitlag, attack1PoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack1);
  }

  // Đòn 2: Nhanh, dứt khoát
  public void EnableAttack2Hitbox()
  {
    weaponHitbox?.EnableHitbox(attack2Damage + TotalAttackBonus, attack2Knockback, attack2Hitlag, lethalHitlag, attack2PoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack2);
  }

  // Đòn 3: Đòn kết đầm tay
  public void EnableAttack3Hitbox()
  {
    weaponHitbox?.EnableHitbox(attack3Damage + TotalAttackBonus * 1.5f, attack3Knockback, attack3Hitlag, lethalHitlag, attack3PoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack3);
  }

  // Charge Attack - Hit 1: Chém mở màn
  public void EnableChargeHit1Hitbox()
  {
    weaponHitbox?.EnableHitbox((chargeAttackDamage + TotalAttackBonus) * 0.4f, 0f, chargeHit1Hitlag, lethalHitlag, chargeHit1PoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Charge1);
  }

  // Charge Attack - Hit 2: Nhát bổ nặng dứt điểm
  public void EnableChargeHit2Hitbox()
  {
    weaponHitbox?.EnableHitbox((chargeAttackDamage + TotalAttackBonus) * 0.6f, chargeAttackKnockback, chargeHit2Hitlag, lethalHitlag, chargeHit2PoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Charge2);
  }

  // Counter Attack: Nhát chém trừng phạt cực nặng
  public void EnableCounterAttackHitbox()
  {
    weaponHitbox?.EnableHitbox(counterAttackDamage + TotalAttackBonus * 2.0f, counterAttackKnockback, counterAttackHitlag, lethalHitlag, counterAttackPoiseDamage);
    playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Counter);
  }

  public void DisableWeaponHitbox() => weaponHitbox?.DisableHitbox();

  // =========================================================================
  // ANIMATION EVENTS PHÁT VỆT CHÉM SLASH VFX ĐỘC LẬP CHO TẤT CẢ CÁC ĐÒN ĐÁNH
  // (Cho phép kéo chỉnh Animation Event trên timeline tách rời với Hitbox va chạm)
  // =========================================================================
  public void PlayAttack1Slash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack1);
  public void PlayAttack2Slash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack2);
  public void PlayAttack3Slash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Attack3);
  public void PlayChargeHit1Slash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Charge1);
  public void PlayChargeHit2Slash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Charge2);
  public void PlayCounterSlash() => playerSlashVFX?.PlaySlash(PlayerSlashVFX.SlashAttackType.Counter);

  // Thêm vào PlayerCombat để PlayerController có thể đọc được
  public bool IsBlockingOrParrying => isParryActive;
  public bool IsAttacking => isAttacking;
  public PlayerSlashVFX SlashVFX => playerSlashVFX;

  // Skill Cooldown Getters (Dành cho UI hiển thị thời gian hồi chiêu)
  public float SkillECooldown => skillECooldown;
  public float SkillQCooldown => skillQCooldown;
  public float SkillRCooldown => skillRCooldown;

  public float SkillETimer => skillETimer;
  public float SkillQTimer => skillQTimer;
  public float SkillRTimer => skillRTimer;

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    if (!showUltHitboxGizmos) return;
    DrawUltHitboxGizmo(true);
  }

  /// <summary>
  /// Vẽ trực quan Hitbox chém Ult (R) trong Scene View để dễ dàng căn chỉnh kích thước, bán kính, góc chém khớp với VFX
  /// </summary>
  private void DrawUltHitboxGizmo(bool isSelected)
  {
    Transform root = transform.root != null ? transform.root : transform;
    Vector3 origin = root.position;
    Vector3 waistCenter = origin + Vector3.up * ultHitboxHeightOffset;
    Vector3 hitboxCenter = waistCenter + root.forward * ultHitboxForwardOffset;

    Color baseColor = ultHitboxGizmoColor;
    if (!isSelected) baseColor.a *= 0.5f;

    // 1. Trục tâm từ nhân vật tới tâm Hitbox
    Gizmos.color = isSelected ? Color.yellow : new Color(1f, 0.9f, 0.2f, 0.4f);
    Gizmos.DrawLine(waistCenter, hitboxCenter);
    Gizmos.DrawWireSphere(hitboxCenter, 0.15f);

    // 2. Mũi tên chỉ hướng chém phía trước
    Gizmos.color = Color.cyan;
    Vector3 fwdEnd = hitboxCenter + root.forward * (ultHitboxRadius * 0.45f);
    Gizmos.DrawLine(hitboxCenter, fwdEnd);

    // 3. Khung cầu Wireframe quét va chạm Physics.OverlapSphere
    Gizmos.color = baseColor;
    Gizmos.DrawWireSphere(hitboxCenter, ultHitboxRadius);

    // 4. Handles vẽ lát cắt đĩa chém ngang (Horizontal Cleave Disc) & Hình quạt góc quét
    UnityEditor.Handles.color = baseColor;
    UnityEditor.Handles.DrawWireDisc(hitboxCenter, Vector3.up, ultHitboxRadius);

    if (ultHitboxAngle < 359f)
    {
      float halfAngle = ultHitboxAngle * 0.5f;
      Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * root.forward;
      Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * root.forward;
      float reachDistance = ultHitboxForwardOffset + ultHitboxRadius;

      UnityEditor.Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, isSelected ? 0.22f : 0.10f);
      UnityEditor.Handles.DrawSolidArc(waistCenter, Vector3.up, leftDir, ultHitboxAngle, reachDistance);

      UnityEditor.Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, isSelected ? 0.9f : 0.5f);
      UnityEditor.Handles.DrawLine(waistCenter, waistCenter + leftDir * reachDistance);
      UnityEditor.Handles.DrawLine(waistCenter, waistCenter + rightDir * reachDistance);
      UnityEditor.Handles.DrawWireArc(waistCenter, Vector3.up, leftDir, ultHitboxAngle, reachDistance);
    }
    else
    {
      UnityEditor.Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, isSelected ? 0.2f : 0.08f);
      UnityEditor.Handles.DrawSolidDisc(hitboxCenter, Vector3.up, ultHitboxRadius);
    }

    if (isSelected)
    {
      GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
      {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 11,
        fontStyle = FontStyle.Bold
      };
      labelStyle.normal.textColor = Color.white;

      string info = $"⚔️ Skill Ult Hitbox (R)\nBán kính: {ultHitboxRadius:F1}m | Đẩy trước: {ultHitboxForwardOffset:F1}m\nCao: {ultHitboxHeightOffset:F2}m | Góc: {ultHitboxAngle}°";
      UnityEditor.Handles.Label(hitboxCenter + Vector3.up * (ultHitboxRadius + 0.35f), info, labelStyle);
    }
  }
#endif
}