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
  public UnityEvent<Vector3, Vector3> onTakeDamage; // Gửi (hitPoint, hitDirection) để sinh VFX trúng đòn
  public UnityEvent onDeath;

  private bool isDead = false;

  public float CurrentHealth => currentHealth;
  public float MaxHealth => maxHealth;
  public bool IsDead => isDead;

  private void Awake()
  {
    currentHealth = maxHealth;
  }

  public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
  {
    if (isDead || isInvulnerable || damage <= 0f) return;

    currentHealth = Mathf.Max(0f, currentHealth - damage);
    Debug.Log($"[Trúng đòn!] Mục tiêu dính {damage} sát thương. Máu còn lại: {currentHealth}/{maxHealth}");

    // Kích hoạt sự kiện nhận đòn
    onHealthChanged?.Invoke(currentHealth, maxHealth);
    onTakeDamage?.Invoke(hitPoint, hitDirection);

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