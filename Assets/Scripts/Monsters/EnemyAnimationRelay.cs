using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
  private EnemyAIBase enemyAI;

  private void Awake()
  {
    // Tự động tìm script AI (SkeletonWarriorAI, SkeletonRogueAI, v.v.) ở GameObject cha
    enemyAI = GetComponentInParent<EnemyAIBase>();
  }

  // Các hàm chuyển tiếp sự kiện từ Animation Event lên EnemyAIBase
  public void EnableEnemyAttackHitbox()
  {
    if (enemyAI != null) enemyAI.EnableEnemyAttackHitbox();
  }

  public void DisableEnemyAttackHitbox()
  {
    if (enemyAI != null) enemyAI.DisableEnemyAttackHitbox();
  }

  public void OnEnemyAttackEnd()
  {
    if (enemyAI != null) enemyAI.OnEnemyAttackEnd();
  }

  /// <summary>
  /// Chuyển tiếp sự kiện bắn tên / phóng đạn phép từ Animation Event
  /// </summary>
  public void ShootProjectileEvent()
  {
    if (enemyAI != null) enemyAI.ShootProjectileEvent();
  }
}