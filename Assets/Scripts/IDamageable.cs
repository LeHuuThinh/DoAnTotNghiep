using UnityEngine;

public interface IDamageable
{
  void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance = 0f, Transform attacker = null);
}