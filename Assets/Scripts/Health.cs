using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
  [Header("Health Settings")]
  [SerializeField] private float maxHealth = 100f;
  [SerializeField] private float currentHealth;
  [SerializeField] private bool isInvulnerable = false; // Bất tử (dùng khi Player lộn nhào né đòn - I-Frames)

  [Header("Events (Tùy chọn gắn VFX / SFX qua Inspector)")]
  public UnityEvent<float, float> onHealthChanged; // Gửi (currentHealth, maxHealth) để cập nhật thanh máu UI
  public UnityEvent<Vector3, Vector3, float> onTakeDamage; // Gửi (hitPoint, hitDirection, knockbackDistance) để sinh VFX trúng đòn
  public UnityEvent onDeath;

  private bool isDead = false;

  public float CurrentHealth => currentHealth;
  public float MaxHealth => maxHealth;
  public float BaseMaxHealth => baseMaxHealth > 0f ? baseMaxHealth : maxHealth;
  public bool IsDead => isDead;
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
  /// </summary>
  public void SetBonusMaxHealth(float progressionBonus, float equipmentBonus = 0f)
  {
    if (baseMaxHealth < 0f)
    {
      baseMaxHealth = maxHealth;
    }

    float oldMax = maxHealth;
    maxHealth = Mathf.Max(1f, baseMaxHealth + progressionBonus + equipmentBonus);

    if (maxHealth > oldMax)
    {
      currentHealth += (maxHealth - oldMax);
    }
    else
    {
      currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance = 0f, Transform attacker = null)
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

    // 2. Tính giảm trừ sát thương từ Giáp trang bị (nếu là Player)
    float actualDamage = damage;
    if (CompareTag("Player") && InfiniteLabyrinth.Inventory.InventoryManager.HasInstance)
    {
      float defense = InfiniteLabyrinth.Inventory.InventoryManager.Instance.GetTotalDefenseBonus();
      if (defense > 0f)
      {
        actualDamage = Mathf.Max(1f, actualDamage - defense);
      }
    }

    // 3. Nếu không parry thì trừ máu bình thường
    currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
    if (CompareTag("Player"))
    {
      s_PersistentPlayerHealth = currentHealth;
      if (GameManager.HasInstance) GameManager.Instance.SavePlayerHealth(currentHealth);
    }
    Debug.Log($"[Trúng đòn!] Mục tiêu dính {actualDamage} (gốc: {damage}) sát thương. Máu còn lại: {currentHealth}/{maxHealth}");

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
    currentHealth = (targetHealth > 0f) ? Mathf.Min(targetHealth, maxHealth) : maxHealth;
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  private void Die()
  {
    if (isDead) return;
    isDead = true;

    onDeath?.Invoke();
  }
}