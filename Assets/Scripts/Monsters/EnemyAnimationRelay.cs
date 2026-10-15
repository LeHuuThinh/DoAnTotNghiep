using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
  private EnemyAI enemyAI;

  private void Awake()
  {
    // Tự động tìm script EnemyAI ở GameObject cha
    enemyAI = GetComponentInParent<EnemyAI>();
  }

  // Các hàm chuyển tiếp sự kiện lên EnemyAI
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
}