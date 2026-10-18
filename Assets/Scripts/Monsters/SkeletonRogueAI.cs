using UnityEngine;
using UnityEngine.AI;

public class SkeletonRogueAI : EnemyAIBase
{
  [Header("Rogue Combat Settings (Tầm Xa / Bắn Cung)")]
  [Tooltip("Prefab mũi tên phóng ra")]
  [SerializeField] private GameObject arrowPrefab;
  [Tooltip("Điểm xuất phát mũi tên (ví dụ: tay cầm cung/nỏ). Nếu để trống code tự lấy trước ngực quái.")]
  [SerializeField] private Transform arrowSpawnPoint;
  [SerializeField] private float arrowSpeed = 22f;
  [SerializeField] private float arrowDamage = 15f;
  [Tooltip("Layer vật cản (tường, cột) để kiểm tra đường ngắm thẳng")]
  [SerializeField] private LayerMask obstacleLayerMask = 1; // Default

  [Header("Rogue Kiting Settings (Thả Diều / Giữ Khoảng Cách)")]
  [Tooltip("Cự ly tối thiểu: Nếu Player áp sát gần hơn mức này, Rogue sẽ lập tức lùi lại giữ khoảng cách")]
  [SerializeField] private float minKiteDistance = 4.0f;
  [Tooltip("Tốc độ chạy lùi thả diều (m/s)")]
  [SerializeField] private float kiteSpeed = 2.8f;

  private bool isAttacking = false;

  public override EnemyCombatRole CombatRole => EnemyCombatRole.Ranged;
  public override bool IsAttacking => isAttacking;

  protected override void Awake()
  {
    base.Awake();

    // Thiết lập mặc định cự ly phù hợp với xạ thủ nếu chưa chỉnh trong Inspector
    if (preferredDistance < 5f) preferredDistance = 8.5f;
    if (attackRange < 6f) attackRange = 12.0f;
    if (combatStanceRange < 8f) combatStanceRange = 14.0f;
    if (chaseRange < 12f) chaseRange = 18.0f;
    if (attackCooldown < 2.0f) attackCooldown = 2.8f;
  }

  protected override void DetermineState(float distanceToPlayer)
  {
    if (hasAttackToken)
    {
      if (distanceToPlayer <= attackRange && HasLineOfSightToPlayer())
      {
        currentState = AIState.Attack;
      }
      else
      {
        currentState = AIState.Chase;
      }
    }
    else if (distanceToPlayer <= combatStanceRange)
    {
      currentState = AIState.Circling;
    }
    else if (distanceToPlayer <= chaseRange)
    {
      currentState = AIState.Chase;
    }
    else
    {
      currentState = AIState.Patrol;
    }
  }

  protected override void HandleCirclingState(float distanceToPlayer)
  {
    RotateTowards(playerTransform.position);

    // 1. CƠ CHẾ THẢ DIỀU (KITING): Player áp sát quá gần -> Chạy lùi lại ngay
    if (distanceToPlayer <= minKiteDistance)
    {
      agent.isStopped = false;
      agent.speed = kiteSpeed;
      Vector3 awayDir = (transform.position - playerTransform.position).normalized;
      Vector3 targetPos = transform.position + awayDir * 3.5f;

      if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
      {
        agent.SetDestination(hit.position);
      }
      return;
    }

    // 2. ĐANG CẦM VÉ TẦM XA:
    if (hasAttackToken)
    {
      if (distanceToPlayer <= attackRange && HasLineOfSightToPlayer())
      {
        agent.isStopped = true;
        ExecuteAttack();
        circleTimer = 0f;
      }
      else
      {
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(playerTransform.position);
      }
      return;
    }

    // 3. ĐỊNH KỲ ĐỔI HƯỚNG BƯỚC NGANG & CỰ LY
    directionTimer += Time.deltaTime;
    if (directionTimer >= directionChangeInterval)
    {
      strafeDirection = Random.value > 0.5f ? 1 : -1;
      directionChangeInterval = Random.Range(2.5f, 4.5f);
      directionTimer = 0f;
    }

    distanceReshuffleTimer += Time.deltaTime;
    if (distanceReshuffleTimer >= 5.0f)
    {
      individualPreferredDistance = preferredDistance + Random.Range(-distanceVariance, distanceVariance);
      distanceReshuffleTimer = 0f;
    }

    // 4. VỜN QUANH Ở CỰ LY XA (Tầm ngắm bắn)
    agent.isStopped = false;
    Vector3 directionToMonster = (transform.position - playerTransform.position).normalized;
    Vector3 tangentDirection = Vector3.Cross(Vector3.up, directionToMonster) * strafeDirection;
    Vector3 separationOffset = CalculateSeparationOffset(directionToMonster, ref tangentDirection);

    Vector3 targetOrbitPosition;
    if (distanceToPlayer < individualPreferredDistance - 0.5f)
    {
      agent.speed = kiteSpeed;
      targetOrbitPosition = playerTransform.position + (directionToMonster + separationOffset * 0.5f).normalized * individualPreferredDistance;
    }
    else
    {
      agent.speed = strafeSpeed;
      Vector3 blendedDirection = (directionToMonster + tangentDirection * 0.7f + separationOffset).normalized;
      targetOrbitPosition = playerTransform.position + blendedDirection * individualPreferredDistance;
    }

    if (NavMesh.SamplePosition(targetOrbitPosition, out NavMeshHit orbitHit, 2f, NavMesh.AllAreas))
    {
      agent.SetDestination(orbitHit.position);
    }

    // 5. Xin vé Tầm xa khi hết thời gian vờn
    circleTimer += Time.deltaTime;
    if (circleTimer >= circleDuration && Time.time >= lastAttackTime + attackCooldown)
    {
      if (AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.RequestAttackToken(this))
      {
        hasAttackToken = true;
      }
    }
  }

  protected override void HandleAttackState(float distanceToPlayer)
  {
    agent.isStopped = true;
    RotateTowards(playerTransform.position);

    if (distanceToPlayer <= attackRange && HasLineOfSightToPlayer() && Time.time >= lastAttackTime + attackCooldown)
    {
      ExecuteAttack();
    }
    else if (distanceToPlayer > attackRange || !HasLineOfSightToPlayer())
    {
      currentState = AIState.Circling;
    }
  }

  protected override void ExecuteAttack()
  {
    isAttacking = true;
    attackStateTimer = 0f;
    lastAttackTime = Time.time;

    // Cung thủ chỉ có 1 animation bắn tên (AttackType = 0)
    animator.SetInteger(AttackTypeHash, 0);

    if (hasAttackSpeedMultiplierParam)
    {
      animator.SetFloat(AttackSpeedMultiplierHash, attackAnimationSpeed);
    }

    animator.SetTrigger(AttackTriggerHash);
  }

  public override void InterruptAttack()
  {
    isAttacking = false;
    attackStateTimer = 0f;
    ReleaseToken();
  }

  /// <summary>
  /// Kiểm tra xem có đường ngắm thẳng (không bị tường/vật cản che) tới Player hay không
  /// </summary>
  public bool HasLineOfSightToPlayer()
  {
    if (playerTransform == null) return false;

    Vector3 eyePos = transform.position + Vector3.up * 1.2f;
    Vector3 targetPos = playerTransform.position + Vector3.up * 1.0f;
    Vector3 toTarget = targetPos - eyePos;
    float dist = toTarget.magnitude;

    if (Physics.Raycast(eyePos, toTarget.normalized, out RaycastHit hit, dist, obstacleLayerMask))
    {
      if (hit.transform.root != playerTransform.root)
      {
        return false; // Bị tường che khuất
      }
    }
    return true;
  }

  /// <summary>
  /// Gọi từ Animation Event tại đúng frame buông dây cung
  /// </summary>
  public override void ShootProjectileEvent()
  {
    if (arrowPrefab == null) return;

    Vector3 spawnPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : (transform.position + Vector3.up * 1.2f + transform.forward * 0.5f);
    Vector3 targetPos = playerTransform != null ? (playerTransform.position + Vector3.up * 1.0f) : (spawnPos + transform.forward * 10f);
    Vector3 shootDir = (targetPos - spawnPos).normalized;

    GameObject proj = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(shootDir));
    EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();
    if (projectile != null)
    {
      projectile.Launch(transform.root, shootDir, arrowDamage, arrowSpeed);
    }
  }

  public override void OnEnemyAttackEnd()
  {
    isAttacking = false;
    attackStateTimer = 0f;
    currentState = AIState.Circling;
    ReleaseToken();
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
    Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Vùng kích hoạt Kite
    Gizmos.DrawWireSphere(transform.position, minKiteDistance);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, combatStanceRange);
  }
#endif
}
