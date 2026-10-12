using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
  [Header("Hitbox Settings")]
  [Tooltip("Chỉ gây sát thương lên các Layer được chọn (ví dụ: Enemy, Breakable)")]
  [SerializeField] private LayerMask targetLayers;

  private Collider hitCollider;
  private float currentDamage = 10f;
  private Transform attacker;

  // Danh sách lưu các mục tiêu đã trúng đòn trong 1 lần vung kiếm (chống lặp dame)
  private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

  private void Awake()
  {
    hitCollider = GetComponent<Collider>();
    hitCollider.isTrigger = true;
    hitCollider.enabled = false; // Mặc định tắt khi chưa chém
  }

  public void Initialize(Transform attackerTransform)
  {
    attacker = attackerTransform;
  }

  /// <summary>
  /// Bật hitbox khi kiếm bắt đầu vung trúng đích
  /// </summary>
  public void EnableHitbox(float damage)
  {
    currentDamage = damage;
    hitTargets.Clear(); // Xóa bộ nhớ trúng đòn của nhát trước
    if (hitCollider != null) hitCollider.enabled = true;
  }

  /// <summary>
  /// Tắt hitbox khi kiếm đã chém qua đích
  /// </summary>
  public void DisableHitbox()
  {
    if (hitCollider != null) hitCollider.enabled = false;
    hitTargets.Clear();
  }

  private void OnTriggerEnter(Collider other)
  {
    // 1. Kiểm tra layer có nằm trong danh sách được phép chém không
    if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

    // 2. Tìm IDamageable trên đối tượng va chạm (hoặc cha của nó)
    IDamageable damageable = other.GetComponentInParent<IDamageable>();

    if (damageable != null && !hitTargets.Contains(damageable))
    {
      hitTargets.Add(damageable); // Đánh dấu đã chém trúng, không chém lần 2 trong nhát này

      // Lấy điểm tiếp xúc gần nhất và hướng đánh
      Vector3 hitPoint = other.ClosestPoint(transform.position);
      Vector3 hitDirection = attacker != null ? attacker.forward : transform.forward;

      // Gây sát thương
      damageable.TakeDamage(currentDamage, hitPoint, hitDirection);
    }
  }
}