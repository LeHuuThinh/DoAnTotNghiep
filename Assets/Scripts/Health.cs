using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
  [Header("Health Settings")]
  [SerializeField] private float maxHealth = 100f;
  [SerializeField] private float currentHealth;
  [SerializeField] private bool isInvulnerable = false; // Bất tử (dùng khi Player lộn nhào né đòn - I-Frames)

  [Header("Defense Settings (ARPG Diminishing Returns)")]
  [Tooltip("Chỉ số Giáp/Phòng thủ cơ bản của thực thể (áp dụng công thức giảm sát thương % chuẩn ARPG: Damage * 100 / (100 + Def))")]
  [SerializeField] private float baseDefense = 0f;

  [Header("Events (Tùy chọn gắn VFX / SFX qua Inspector)")]
  public UnityEvent<float, float> onHealthChanged; // Gửi (currentHealth, maxHealth) để cập nhật thanh máu UI
  public UnityEvent<Vector3, Vector3, float> onTakeDamage; // Gửi (hitPoint, hitDirection, knockbackDistance) để sinh VFX trúng đòn
  public UnityEvent onDeath;
  public UnityEvent<bool> onBleedStateChanged = new UnityEvent<bool>(); // Báo hiệu bật/tắt icon xuất huyết trên HUD

  [Header("Status Effects Settings")]
  [SerializeField] private GameObject bleedVfxPrefab;
  private Coroutine activeBleedRoutine = null;
  public bool IsBleeding { get; private set; } = false;

  private bool isDead = false;

  public float CurrentHealth => currentHealth;
  public float MaxHealth => maxHealth;
  public float BaseMaxHealth => baseMaxHealth > 0f ? baseMaxHealth : maxHealth;
  public bool IsDead => isDead;
  public float BaseDefense => baseDefense;
  private float temporaryDefenseBonus = 0f;
  private float temporaryDefenseTimer = 0f;
  private float temporaryDefenseDuration = 30f;

  public bool HasTemporaryDefenseBoost => temporaryDefenseTimer > 0f;
  public float TemporaryDefenseTimer => temporaryDefenseTimer;
  public float TemporaryDefenseDuration => temporaryDefenseDuration > 0f ? temporaryDefenseDuration : 30f;
  public float TemporaryDefenseBonus => temporaryDefenseBonus;

  public void TriggerTemporaryDefenseBoost(float bonus, float duration)
  {
    temporaryDefenseBonus = Mathf.Max(temporaryDefenseBonus, bonus);
    temporaryDefenseTimer = Mathf.Max(temporaryDefenseTimer, duration);
    temporaryDefenseDuration = Mathf.Max(temporaryDefenseDuration, duration);
  }

  private void Update()
  {
    if (temporaryDefenseTimer > 0f)
    {
      temporaryDefenseTimer -= Time.deltaTime;
      if (temporaryDefenseTimer <= 0f)
      {
        temporaryDefenseTimer = 0f;
        temporaryDefenseBonus = 0f;
      }
    }
  }

  public float TotalDefense
  {
    get
    {
      float total = Mathf.Max(0f, baseDefense) + (temporaryDefenseTimer > 0f ? temporaryDefenseBonus : 0f);
      if (CompareTag("Player") && InfiniteLabyrinth.Inventory.InventoryManager.HasInstance)
      {
        total += InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalDefenseBonus();
      }
      return total;
    }
  }

  public void SetBaseDefense(float defense)
  {
    baseDefense = Mathf.Max(0f, defense);
  }

  public Transform LastAttacker { get; private set; }
  public float LastPoiseDamage { get; private set; } = -1f;

  public void SetIncomingPoiseDamage(float poiseDamage)
  {
    LastPoiseDamage = poiseDamage;
  }

  // Callback kiểm tra Parry hoặc Block trước khi trừ máu (trả về true nếu chặn/parry thành công -> không trừ máu)
  public Func<Transform, Vector3, Vector3, float, bool> onCheckParryOrBlock;

  private float baseMaxHealth = -1f;

  private static float? s_PersistentPlayerHealth = null;

  public static void ResetPersistentPlayerHealth()
  {
    s_PersistentPlayerHealth = null;
    if (GameManager.HasInstance)
    {
      GameManager.Instance.ClearSavedHealth();
    }
  }

  public static void SetPersistentPlayerHealth(float hp)
  {
    s_PersistentPlayerHealth = hp;
    if (GameManager.HasInstance)
    {
      GameManager.Instance.SavePlayerHealth(hp);
    }
  }

  /// <summary>
  /// Hồi đầy 100% máu (Dành cho Chuông Làng hoặc Hồi sinh sau tử trận)
  /// </summary>
  public void RefillToFull()
  {
    isDead = false;
    currentHealth = maxHealth;
    if (CompareTag("Player"))
    {
      s_PersistentPlayerHealth = maxHealth;
      if (GameManager.HasInstance)
      {
        GameManager.Instance.SavePlayerHealth(maxHealth);
      }
    }
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  /// <summary>
  /// Cài đặt lượng máu tối đa và hiện tại theo giá trị tùy chỉnh (Dành cho Dummy bia tập đánh hoặc Admin)
  /// </summary>
  public void SetCustomHealth(float newMaxHealth)
  {
    baseMaxHealth = newMaxHealth;
    maxHealth = newMaxHealth;
    currentHealth = newMaxHealth;
    isDead = false;
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  /// <summary>
  /// Cài đặt lượng máu tối đa và hiện tại theo giá trị mới (Alias của SetCustomHealth)
  /// </summary>
  public void SetMaxHealth(float newMaxHealth) => SetCustomHealth(newMaxHealth);

  private void Awake()
  {
    baseMaxHealth = maxHealth;
    currentHealth = maxHealth;

    // Item 1: Bảo lưu lượng máu còn lại của Player xuyên TẤT CẢ các Scene (Dù xuống tầng hay về Làng)
    if (CompareTag("Player"))
    {
      if (s_PersistentPlayerHealth.HasValue)
      {
        currentHealth = Mathf.Clamp(s_PersistentPlayerHealth.Value, 1f, maxHealth);
        Debug.Log($"<color=cyan>[Health]</color> 🩸 Đã khôi phục Máu từ scene trước (static cache): {currentHealth:F0}/{maxHealth:F0}");
      }
      else if (GameManager.HasInstance && GameManager.Instance.HasSavedHealth)
      {
        currentHealth = Mathf.Clamp(GameManager.Instance.SavedHealth, 1f, maxHealth);
        s_PersistentPlayerHealth = currentHealth;
        Debug.Log($"<color=cyan>[Health]</color> 🩸 Đã khôi phục Máu từ scene trước (GameManager): {currentHealth:F0}/{maxHealth:F0}");
      }
      else
      {
        currentHealth = maxHealth;
        s_PersistentPlayerHealth = currentHealth;
      }
    }
  }

  private void OnDisable()
  {
    if (CompareTag("Player") && !isDead)
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance && !GameManager.IsApplicationQuitting)
      {
        GameManager.Instance.SavePlayerHealth(currentHealth);
      }
    }
  }

  private void OnDestroy()
  {
    if (CompareTag("Player") && !isDead)
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance && !GameManager.IsApplicationQuitting)
      {
        GameManager.Instance.SavePlayerHealth(currentHealth);
      }
    }
  }

  /// <summary>
  /// Cập nhật Máu tối đa kết hợp từ Điểm tiềm năng (Progression) và Trang bị (Equipment).
  /// Luôn giữ nguyên lượng máu hiện tại của người chơi (chỉ clamp nếu vượt quá Max Health mới),
  /// triệt tiêu hoàn toàn lỗi tháo/gắn trang bị nhiều lần làm đầy cây máu (infinite heal exploit).
  /// </summary>
  public void SetBonusMaxHealth(float progressionBonus, float equipmentBonus = 0f)
  {
    if (baseMaxHealth < 0f)
    {
      baseMaxHealth = maxHealth;
    }

    maxHealth = Mathf.Max(1f, baseMaxHealth + progressionBonus + equipmentBonus);
    currentHealth = Mathf.Min(currentHealth, maxHealth);

    if (CompareTag("Player"))
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance && !GameManager.IsApplicationQuitting)
      {
        GameManager.Instance.SavePlayerHealth(currentHealth);
      }
    }

    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance = 0f, Transform attacker = null, InfiniteLabyrinth.UI.Combat.DamageNumberType damageType = InfiniteLabyrinth.UI.Combat.DamageNumberType.Normal, bool isCrit = false)
  {
    if (isDead || isInvulnerable || damage <= 0f) return;

    // Admin Cheat: God Mode cho Player
    if (CompareTag("Player") && AdminCheatManager.IsGodMode) return;

    LastAttacker = attacker;

    // 1. Kiểm tra nếu đòn đánh bị Parry hoặc Chặn đứng thành công -> MIỄN NHIỄM 100% SÁT THƯƠNG, KHÔNG TRỪ MÁU!
    if (onCheckParryOrBlock != null && onCheckParryOrBlock.Invoke(attacker, hitPoint, hitDirection, knockbackDistance))
    {
      return;
    }

    // 2. Tính giảm trừ sát thương theo công thức Giảm % chuẩn ARPG / Souls-like
    // Damage Taken = Raw Damage * (K / (K + Defense)) với K = 100f
    float totalDefense = TotalDefense;
    float actualDamage = damage;
    if (totalDefense > 0f)
    {
      const float armorConstant = 100f;
      float damageReductionFactor = armorConstant / (armorConstant + totalDefense);
      actualDamage = damage * damageReductionFactor;
    }
    actualDamage = Mathf.Max(1f, Mathf.Round(actualDamage * 10f) / 10f); // Giữ 1 chữ số thập phân, tối thiểu 1 sát thương

    // 3. Nếu không parry thì trừ máu bình thường
    currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
    if (CompareTag("Player"))
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance) GameManager.Instance.SavePlayerHealth(currentHealth);
    }
    Debug.Log($"[Trúng đòn!] Mục tiêu dính {actualDamage} (gốc: {damage}) sát thương{(isCrit ? " [CRITICAL!]" : "")}. Máu còn lại: {currentHealth}/{maxHealth}");

    // 4. Kích hoạt số sát thương bay lên (Floating Damage Number)
    Vector3 spawnPos = (hitPoint != Vector3.zero) ? hitPoint : (transform.position + Vector3.up * 1.1f);
    InfiniteLabyrinth.UI.Combat.DamageNumberType effectiveType = CompareTag("Player") 
      ? InfiniteLabyrinth.UI.Combat.DamageNumberType.PlayerHit 
      : damageType;
    InfiniteLabyrinth.UI.Combat.DamageNumberManager.Instance.SpawnDamage(actualDamage, spawnPos, effectiveType, isCrit);

    // Kích hoạt sự kiện nhận đòn
    onHealthChanged?.Invoke(currentHealth, maxHealth);

    if (currentHealth <= 0f)
    {
      Die();
    }

    onTakeDamage?.Invoke(hitPoint, hitDirection, knockbackDistance);
  }

  public void Heal(float amount)
  {
    if (isDead || amount <= 0f) return;

    currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    if (CompareTag("Player"))
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance) GameManager.Instance.SavePlayerHealth(currentHealth);
    }
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  public void SetInvulnerable(bool state)
  {
    isInvulnerable = state;
  }

  /// <summary>
  /// Hồi sinh đối tượng (reset cờ isDead và nạp đầy máu) - Dùng khi bắt đầu hiệp đấu / Episode mới trong Arena hoặc ML-Agents
  /// </summary>
  public void Revive(float targetHealth = -1f)
  {
    isDead = false;
    if (activeBleedRoutine != null)
    {
      StopCoroutine(activeBleedRoutine);
      activeBleedRoutine = null;
    }
    if (IsBleeding)
    {
      IsBleeding = false;
      onBleedStateChanged?.Invoke(false);
    }
    currentHealth = (targetHealth > 0f) ? Mathf.Min(targetHealth, maxHealth) : maxHealth;
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  public void ApplyBleed(float totalDamage, float duration = 3f, int ticks = 3, Transform attacker = null)
  {
    if (isDead || totalDamage <= 0f) return;

    if (activeBleedRoutine != null)
    {
      StopCoroutine(activeBleedRoutine);
      activeBleedRoutine = null;
    }

    activeBleedRoutine = StartCoroutine(BleedRoutine(totalDamage, duration, ticks, attacker));
  }

  private System.Collections.IEnumerator BleedRoutine(float totalDamage, float duration, int ticks, Transform attacker)
  {
    IsBleeding = true;
    onBleedStateChanged?.Invoke(true);

    if (bleedVfxPrefab == null)
    {
      bleedVfxPrefab = Resources.Load<GameObject>("BleedVFX");
    }

    float tickInterval = duration / Mathf.Max(1, ticks);
    float tickDamage = totalDamage / Mathf.Max(1, ticks);

    for (int i = 0; i < ticks; i++)
    {
      yield return new WaitForSeconds(tickInterval);
      if (isDead) break;

      Vector3 spawnPos = transform.position + Vector3.up * 0.65f;

      // 1. Kích hoạt hiệu ứng tóe máu BleedVFX mỗi nhịp rút máu
      if (bleedVfxPrefab != null)
      {
        Instantiate(bleedVfxPrefab, spawnPos, Quaternion.identity);
      }

      // 2. Rút máu với kiểu sát thương Bleed (Số đỏ thẫm + Icon Giọt Máu)
      TakeDamage(tickDamage, spawnPos, Vector3.up, 0f, attacker, InfiniteLabyrinth.UI.Combat.DamageNumberType.Bleed, false);
    }

    IsBleeding = false;
    activeBleedRoutine = null;
    onBleedStateChanged?.Invoke(false);
  }

  private void Die()
  {
    if (isDead) return;
    isDead = true;

    if (activeBleedRoutine != null)
    {
      StopCoroutine(activeBleedRoutine);
      activeBleedRoutine = null;
    }
    if (IsBleeding)
    {
      IsBleeding = false;
      onBleedStateChanged?.Invoke(false);
    }

    // Kích hoạt Phù Phép Soul Siphon (Hồi phục HP & Spirit cho Player khi tiêu diệt mục tiêu)
    if (LastAttacker != null && LastAttacker.CompareTag("Player") && !CompareTag("Player"))
    {
      if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
          InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.SoulSiphon))
      {
        var pHealth = LastAttacker.GetComponent<Health>();
        if (pHealth != null && !pHealth.IsDead) pHealth.Heal(15f);

        var pStats = LastAttacker.GetComponent<PlayerStats>();
        if (pStats != null) pStats.GainSpirit(15f);
      }
    }

    onDeath?.Invoke();
  }
}