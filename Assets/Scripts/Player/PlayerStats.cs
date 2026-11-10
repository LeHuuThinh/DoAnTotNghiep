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
  [Tooltip("Năng lượng kỹ năng tối đa (Cap = 200)")]
  [SerializeField] private float maxSpirit = 200f;
  [Tooltip("Năng lượng kỹ năng hiện tại (theo dõi thời gian thực)")]
  [SerializeField] private float currentSpirit = 0f;

  [Header("Spirit Costs (Tiêu Hao Cho Kỹ Năng)")]
  [Tooltip("Năng lượng tiêu hao khi dùng Kỹ năng E (Slash Projectile)")]
  [SerializeField] private float skillECost = 60f;
  [Tooltip("Năng lượng tiêu hao khi dùng Kỹ năng Q (AOE Spell)")]
  [SerializeField] private float skillQCost = 90f;
  [Tooltip("Năng lượng tiêu hao khi dùng Kỹ năng R (Ultimate Colossal Slash)")]
  [SerializeField] private float skillRCost = 175f;

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
  public float BaseMaxStamina => baseMaxStamina > 0f ? baseMaxStamina : maxStamina;
  public float CurrentStamina => currentStamina;
  public float MaxSpirit => maxSpirit;
  public float CurrentSpirit => currentSpirit;

  // Multipliers từ trang bị
  private float SpiritCostReductionMultiplier => 
    InfiniteLabyrinth.Inventory.InventoryManager.HasInstance 
      ? Mathf.Clamp01(1f - InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalSpiritCostReductionBonus() / 100f) 
      : 1f;

  private float StaminaCostReductionMultiplier => 
    InfiniteLabyrinth.Inventory.InventoryManager.HasInstance 
      ? Mathf.Clamp01(1f - InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalStaminaCostReductionBonus() / 100f) 
      : 1f;

  public float SprintCostPerSecond => sprintCostPerSecond * StaminaCostReductionMultiplier;
  public float RollCost => rollCost * StaminaCostReductionMultiplier;
  public float LightAttackCost => lightAttackCost * StaminaCostReductionMultiplier;
  public float ChargeAttackCost => chargeAttackCost * StaminaCostReductionMultiplier;
  public float ParryWhiffCost => parryWhiffCost * StaminaCostReductionMultiplier;

  public float SkillECost
  {
    get
    {
      float cost = skillECost * SpiritCostReductionMultiplier;
      if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
          InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.SkillECostReduction))
      {
        cost *= 0.7f; // Giảm thêm 30% cho Skill E
      }
      return Mathf.Max(1f, cost);
    }
  }

  public float SkillQCost
  {
    get
    {
      float cost = skillQCost * SpiritCostReductionMultiplier;
      if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
          InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.SkillQCostReduction))
      {
        cost *= 0.7f; // Giảm thêm 30% cho Skill Q
      }
      return Mathf.Max(1f, cost);
    }
  }

  public float SkillRCost
  {
    get
    {
      float cost = skillRCost * SpiritCostReductionMultiplier;
      if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
          InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.SkillRCostReduction))
      {
        cost *= 0.75f; // Giảm thêm 25% cho Skill R
      }
      return Mathf.Max(1f, cost);
    }
  }

  public float SpiritGainOnLightHit => spiritGainOnLightHit;
  public float SpiritGainOnChargeHit => spiritGainOnChargeHit;
  public float SpiritGainOnCounterHit => spiritGainOnCounterHit;
  public float SpiritGainOnParrySuccess => spiritGainOnParrySuccess;

  private float baseMaxStamina = -1f;

  private static float? s_PersistentSpirit = null;

  public static void ResetPersistentSpirit()
  {
    s_PersistentSpirit = 0f;
    if (GameManager.HasInstance)
    {
      GameManager.Instance.ResetPlayerSpirit();
    }
  }

  public static void SetPersistentSpirit(float spirit)
  {
    s_PersistentSpirit = spirit;
    if (GameManager.HasInstance)
    {
      GameManager.Instance.SavePlayerSpirit(spirit);
    }
  }

  /// <summary>
  /// Hồi đầy 100% Thần lực (Spirit) - Dành cho Chuông Làng hoặc Hồi sinh
  /// </summary>
  public void RefillSpiritToFull()
  {
    currentSpirit = maxSpirit;
    s_PersistentSpirit = maxSpirit;
    if (GameManager.HasInstance)
    {
      GameManager.Instance.SavePlayerSpirit(maxSpirit);
    }
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
  }

  private void Awake()
  {
    baseMaxStamina = maxStamina;
    currentStamina = maxStamina;

    // Khôi phục Năng lượng Kỹ năng (Spirit) xuyên Scene
    if (s_PersistentSpirit.HasValue)
    {
      currentSpirit = Mathf.Clamp(s_PersistentSpirit.Value, 0f, maxSpirit);
    }
    else if (GameManager.HasInstance && GameManager.Instance.HasSavedSpirit)
    {
      currentSpirit = Mathf.Clamp(GameManager.Instance.SavedSpirit, 0f, maxSpirit);
      s_PersistentSpirit = currentSpirit;
    }
    else
    {
      currentSpirit = Mathf.Clamp(currentSpirit, 0f, maxSpirit);
      s_PersistentSpirit = currentSpirit;
    }
  }

  private void OnDisable()
  {
    s_PersistentSpirit = currentSpirit;
    if (GameManager.HasInstance && !GameManager.IsApplicationQuitting)
    {
      GameManager.Instance.SavePlayerSpirit(currentSpirit);
    }
  }

  private void OnDestroy()
  {
    s_PersistentSpirit = currentSpirit;
    if (GameManager.HasInstance && !GameManager.IsApplicationQuitting)
    {
      GameManager.Instance.SavePlayerSpirit(currentSpirit);
    }
  }

  /// <summary>
  /// Cập nhật Thể lực tối đa kết hợp từ Điểm tiềm năng (Progression) và Trang bị (Equipment).
  /// </summary>
  public void SetBonusMaxStamina(float progressionBonus, float equipmentBonus = 0f)
  {
    if (baseMaxStamina < 0f)
    {
      baseMaxStamina = maxStamina;
    }

    float oldMax = maxStamina;
    maxStamina = Mathf.Max(10f, baseMaxStamina + progressionBonus + equipmentBonus);

    if (maxStamina > oldMax)
    {
      currentStamina += (maxStamina - oldMax);
    }
    else
    {
      currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    onStaminaChanged?.Invoke(currentStamina, maxStamina);
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
    if (AdminCheatManager.IsInfiniteStamina) return true;
    return currentStamina >= amount;
  }

  public bool ConsumeStamina(float amount)
  {
    if (AdminCheatManager.IsInfiniteStamina) return true;
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
    if (AdminCheatManager.IsInfiniteStamina) return true;
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
    if (AdminCheatManager.IsInfiniteSpirit) return true;
    return currentSpirit >= amount;
  }

  public bool ConsumeSpirit(float amount)
  {
    if (AdminCheatManager.IsInfiniteSpirit) return true;
    if (currentSpirit < amount)
    {
      return false;
    }

    currentSpirit = Mathf.Max(0f, currentSpirit - amount);
    s_PersistentSpirit = currentSpirit;
    if (GameManager.HasInstance) GameManager.Instance.SavePlayerSpirit(currentSpirit);
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
    return true;
  }

  public void GainSpirit(float amount)
  {
    if (amount <= 0f) return;
    currentSpirit = Mathf.Min(maxSpirit, currentSpirit + amount);
    s_PersistentSpirit = currentSpirit;
    if (GameManager.HasInstance) GameManager.Instance.SavePlayerSpirit(currentSpirit);
    onSpiritChanged?.Invoke(currentSpirit, maxSpirit);
  }
}
