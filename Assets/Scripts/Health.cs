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
  public bool IsDead => isDead;
  public Transform LastAttacker { get; private set; }

  // Callback kiểm tra Parry hoặc Block trước khi trừ máu (trả về true nếu chặn/parry thành công -> không trừ máu)
  public Func<Transform, Vector3, Vector3, float, bool> onCheckParryOrBlock;

  private void Awake()
  {
    currentHealth = maxHealth;
  }

  public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance = 0f, Transform attacker = null)
  {
    if (isDead || isInvulnerable || damage <= 0f) return;

    LastAttacker = attacker;

    // 1. Kiểm tra nếu đòn đánh bị Parry hoặc Chặn đứng thành công -> MIỄN NHIỄM 100% SÁT THƯƠNG, KHÔNG TRỪ MÁU!
    if (onCheckParryOrBlock != null && onCheckParryOrBlock.Invoke(attacker, hitPoint, hitDirection, knockbackDistance))
    {
      return;
    }

    // 2. Nếu không parry thì trừ máu bình thường
    currentHealth = Mathf.Max(0f, currentHealth - damage);
    Debug.Log($"[Trúng đòn!] Mục tiêu dính {damage} sát thương. Máu còn lại: {currentHealth}/{maxHealth}");

    // Kích hoạt sự kiện nhận đòn
    onHealthChanged?.Invoke(currentHealth, maxHealth);
    onTakeDamage?.Invoke(hitPoint, hitDirection, knockbackDistance);

    if (currentHealth <= 0f)
    {
      Die();
    }
  }

  public void Heal(float amount)
  {
    if (isDead || amount <= 0f) return;

    currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    onHealthChanged?.Invoke(currentHealth, maxHealth);
  }

  public void SetInvulnerable(bool state)
  {
    isInvulnerable = state;
  }

  private void Die()
  {
    if (isDead) return;
    isDead = true;

    onDeath?.Invoke();
  }
}