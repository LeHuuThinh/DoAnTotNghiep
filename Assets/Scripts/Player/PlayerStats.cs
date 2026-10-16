using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý tài nguyên của Player:
/// 1. THỂ LỰC (STAMINA): Tiêu hao khi Chạy nhanh, Lăn né, Chém thường, Chém gồng, Giơ thế Parry. Tự hồi phục sau khoảng trễ.
/// 2. NĂNG LƯỢNG KỸ NĂNG (SPIRIT/FOCUS): Max 150. Tiêu hao khi dùng Skill E (50) và Q (100). Chỉ hồi phục khi chém trúng quái và Parry thành công.
/// </summary>
public class PlayerStats : MonoBehaviour
{
  [Header("=== STAMINA (THỂ LỰC) ===")]
  [Tooltip("Thể lực tối đa")]
  [SerializeField] private float maxStamina = 100f;
  [Tooltip("Thể lực hiện tại (theo dõi thời gian thực)")]
  [SerializeField] private float currentStamina = 100f;

  [Header("Stamina Regeneration")]
  [Tooltip("Tốc độ hồi thể lực (điểm/giây)")]
  [SerializeField] private float staminaRegenRate = 35f;
  [Tooltip("Khoảng thời gian trễ trước khi bắt đầu hồi thể lực sau hành động cuối cùng (giây)")]
  [SerializeField] private float staminaRegenDelay = 0.85f;

  [Header("Stamina Costs (Tiêu Hao Thể Lực)")]
  [Tooltip("Thể lực tiêu hao mỗi giây khi chạy nhanh (Sprint)")]
  [SerializeField] private float sprintCostPerSecond = 15f;
  [Tooltip("Thể lực tiêu hao khi lăn né (Roll)")]
  [SerializeField] private float rollCost = 20f;
  [Tooltip("Thể lực tiêu hao cho mỗi nhát chém thường")]
  [SerializeField] private float lightAttackCost = 15f;
  [Tooltip("Thể lực tiêu hao cho đòn chém gồng (Charge Attack)")]
  [SerializeField] private float chargeAttackCost = 30f;
  [Tooltip("Thể lực tiêu hao khi vào thế Parry (nếu hụt sẽ bị phạt, nếu parry chuẩn sẽ được hoàn trả)")]
  [SerializeField] private float parryWhiffCost = 20f;

  [Header("=== SPIRIT (NĂNG LƯỢNG KỸ NĂNG) ===")]
  [Tooltip("Năng lượng kỹ năng tối đa (Cap = 150)")]
  [SerializeField] private float maxSpirit = 150f;
  [Tooltip("Năng lượng kỹ năng hiện tại (theo dõi thời gian thực)")]
  [SerializeField] private float currentSpirit = 0f;

  [Header("Spirit Costs (Tiêu Hao Cho Kỹ Năng)")]
  [Tooltip("Năng lượng tiêu hao khi dùng Kỹ năng E (Slash Projectile)")]
  [SerializeField] private float skillECost = 50f;
  [Tooltip("Năng lượng tiêu hao khi dùng Kỹ năng Q (AOE Spell)")]
  [SerializeField] private float skillQCost = 100f;

  [Header("Spirit Gains (Tích Lũy Từ Combat)")]
  [Tooltip("Năng lượng nhận được khi chém thường trúng quái")]
  [SerializeField] private float spiritGainOnLightHit = 12f;
  [Tooltip("Năng lượng nhận được khi đòn Charge Attack trúng quái")]
  [SerializeField] private float spiritGainOnChargeHit = 30f;
  [Tooltip("Năng lượng nhận được khi đòn Counter Attack trúng quái")]
  [SerializeField] private float spiritGainOnCounterHit = 35f;
  [Tooltip("Năng lượng nhận được khi Parry thành công đòn đánh của quái")]
  [SerializeField] private float spiritGainOnParrySuccess = 40f;

  [Header("Events (Dành Cho Kết Nối UI / HUD)")]
  public UnityEvent<float, float> onStaminaChanged; // (current, max)
  public UnityEvent<float, float> onSpiritChanged;   // (current, max)

  private float staminaRegenTimer = 0f;

  // Public Properties
  public float MaxStamina => maxStamina;
  public float CurrentStamina => currentStamina;
  public float MaxSpirit => maxSpirit;
  public float CurrentSpirit => currentSpirit;

  public float SprintCostPerSecond => sprintCostPerSecond;
  public float RollCost => rollCost;
  public float LightAttackCost => lightAttackCost;
  public float ChargeAttackCost => chargeAttackCost;
  public float ParryWhiffCost => parryWhiffCost;

  public float SkillECost => skillECost;
  public float SkillQCost => skillQCost;

  public float SpiritGainOnLightHit => spiritGainOnLightHit;
  public float SpiritGainOnChargeHit => spiritGainOnChargeHit;
  public float SpiritGainOnCounterHit => spiritGainOnCounterHit;
  public float SpiritGainOnParrySuccess => spiritGainOnParrySuccess;

  private void Awake()
  {
    currentStamina = maxStamina;
    currentSpirit = Mathf.Clamp(currentSpirit, 0f, maxSpirit);
  }

  private void Start()
  {
    onStaminaChanged?.Invoke(currentStamina, maxStamina);
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
  }

  private void Update()
  {
    RegenerateStamina();
  }

  private void RegenerateStamina()
  {
    if (staminaRegenTimer > 0f)
    {
      staminaRegenTimer -= Time.deltaTime;
      return;
    }

    if (currentStamina < maxStamina)
    {
      currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
      onStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
  }

  // =========================================================================
  // XỬ LÝ THỂ LỰC (STAMINA)
  // =========================================================================

  public bool HasStamina(float amount)
  {
    return currentStamina >= amount;
  }

  public bool ConsumeStamina(float amount)
  {
    if (currentStamina < amount)
    {
      return false;
    }

    currentStamina = Mathf.Max(0f, currentStamina - amount);
    staminaRegenTimer = staminaRegenDelay;
    onStaminaChanged?.Invoke(currentStamina, maxStamina);
    return true;
  }

  public bool ConsumeStaminaContinuous(float amount)
  {
    if (currentStamina <= 0f)
    {
      return false;
    }

    currentStamina = Mathf.Max(0f, currentStamina - amount);
    staminaRegenTimer = staminaRegenDelay;
    onStaminaChanged?.Invoke(currentStamina, maxStamina);
    return currentStamina > 0f;
  }

  public void RestoreStamina(float amount)
  {
    if (amount <= 0f) return;
    currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    onStaminaChanged?.Invoke(currentStamina, maxStamina);
  }

  // =========================================================================
  // XỬ LÝ NĂNG LƯỢNG KỸ NĂNG (SPIRIT)
  // =========================================================================

  public bool HasSpirit(float amount)
  {
    return currentSpirit >= amount;
  }

  public bool ConsumeSpirit(float amount)
  {
    if (currentSpirit < amount)
    {
      return false;
    }

    currentSpirit = Mathf.Max(0f, currentSpirit - amount);
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
    return true;
  }

  public void GainSpirit(float amount)
  {
    if (amount <= 0f) return;
    currentSpirit = Mathf.Min(maxSpirit, currentSpirit + amount);
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
  }
}
