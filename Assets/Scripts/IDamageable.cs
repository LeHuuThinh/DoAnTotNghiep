using UnityEngine;

public interface IDamageable
{
  /// <summary>
  /// Nhận sát thương kèm thông tin va chạm để sinh hiệu ứng và lực đẩy lùi
  /// </summary>
  /// <param name="damage">Lượng máu bị trừ</param>
  /// <param name="hitPoint">Tọa độ tiếp xúc (để spawn tia lửa, vệt máu, số nhảy dame)</param>
  /// <param name="hitDirection">Hướng vung kiếm (để tính lực đẩy giật lùi - Knockback)</param>
  void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
}