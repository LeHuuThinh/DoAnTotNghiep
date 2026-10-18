using UnityEngine;
using UnityEngine.AI;

public class SkeletonWarriorAI : EnemyAIBase
{
  [Header("Warrior Combat Settings (Cận Chiến)")]
  [SerializeField] private WeaponHitbox weaponHitbox;
  [SerializeField] private float attackDamage = 15f;
  [SerializeField] private float attackKnockback = 0.5f;

  [Header("Warrior Movement Speeds")]
  [Tooltip("Tốc độ khi quái bước lùi lại 1 bước trong cận chiến (m/s)")]
  [SerializeField] private float retreatSpeed = 1.1f;
  [Tooltip("Tốc độ quái chủ động lùi về lại cự ly preferredDistance sau khi chém (m/s)")]
  [SerializeField] private float postAttackRetreatSpeed = 1.8f;
  [Tooltip("Nếu người chơi áp sát vào tầm đánh và quái đã hồi chiêu -> Chém trả đũa ngay")]
  [SerializeField] private bool counterAttackIfReady = true;

  [Header("Close-Combat Reactions (Phản Ứng Khi Player Áp Sát)")]
  [Tooltip("Cự ly kích hoạt phản ứng khi Player chủ động áp sát (mét, khuyên dùng 2.5 - 2.8m)")]
  [SerializeField] private float closeCombatTriggerRange = 2.8f;
  [Tooltip("Khoảng cách của một bước lùi ngắn trước khi dừng lại thủ thế (mét, khuyên dùng 0.8 - 1.2m)")]
  [SerializeField] private float stepBackDistance = 1.0f;
  [Tooltip("Thời gian duy trì một phản ứng trước khi đổi hành động (giây, khuyên dùng 1.2 - 2.0s)")]
  [SerializeField] private float reactionDuration = 1.5f;
  [Tooltip("Tỷ lệ % đứng thủ thế khi Player áp sát (0 - 100)")]
  [Range(0, 100)] [SerializeField] private int standGuardChance = 35;
  [Tooltip("Tỷ lệ % dạt ngang né đòn khi Player áp sát (0 - 100)")]
  [Range(0, 100)] [SerializeField] private int sidestepChance = 35;
  [Tooltip("Tỷ lệ % lùi lại 1 bước rồi dừng thủ thế khi Player áp sát (0 - 100)")]
  [Range(0, 100)] [SerializeField] private int stepBackChance = 30;

  private bool isAttacking = false;
  private bool isPostAttackRetreating = false;
  private float postAttackRetreatTimeout = 0f;

  private enum CloseReaction { None, StandGuard, Sidestep, StepBackOnce }
  private CloseReaction currentReaction = CloseReaction.None;
  private float reactionTimer = 0f;
  private float reactionCooldownTimer = 0f;
  private Vector3 stepBackTargetPosition;
  private bool isStepBackDone = false;

  public override EnemyCombatRole CombatRole => EnemyCombatRole.Melee;
  public override bool IsAttacking => isAttacking;

  protected override void Start()
  {
    base.Start();

    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
    }
  }

  protected override void HandleCirclingState(float distanceToPlayer)
  {
    RotateTowards(playerTransform.position);

    // 1. GIAI ĐOẠN LÙI TÁI LẬP CỰ LY SAU KHI CHÉM (Post-Attack Retreat)
    if (isPostAttackRetreating)
    {
      postAttackRetreatTimeout -= Time.deltaTime;
      bool isAllyAttackingNow = AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.ActiveMeleeTokens > 0;
      float targetPreferredDist = isAllyAttackingNow ? individualPreferredDistance + 0.6f : individualPreferredDistance;

      bool reachedRetreatDist = distanceToPlayer >= targetPreferredDist - 0.3f;
      bool isStuckAgainstWall = !agent.pathPending && agent.hasPath && agent.remainingDistance <= 0.3f && postAttackRetreatTimeout < 2.0f;

      if (reachedRetreatDist || postAttackRetreatTimeout <= 0f || isStuckAgainstWall)
      {
        isPostAttackRetreating = false;
        currentReaction = CloseReaction.None;
        reactionTimer = 0f;
      }
      else
      {
        agent.isStopped = false;
        agent.speed = postAttackRetreatSpeed;
        Vector3 awayDir = (transform.position - playerTransform.position).normalized;
        Vector3 tangentDir = Vector3.Cross(Vector3.up, awayDir) * strafeDirection;
        Vector3 retreatSeparation = CalculateSeparationOffset(awayDir, ref tangentDir);
        Vector3 targetRetreatPos = playerTransform.position + (awayDir + retreatSeparation * 0.4f).normalized * targetPreferredDist;

        if (NavMesh.SamplePosition(targetRetreatPos, out NavMeshHit retreatHit, 2f, NavMesh.AllAreas))
        {
          agent.SetDestination(retreatHit.position);
        }
        return;
      }
    }

    // 2. PHẢN CÔNG TỨC THÌ: Nếu Player áp sát vào tầm chém và quái đã hồi chiêu -> Chém ngay!
    if (counterAttackIfReady && distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
    {
      if (AttackTokenDirector.Instance == null || AttackTokenDirector.Instance.RequestAttackToken(this))
      {
        hasAttackToken = true;
        currentState = AIState.Attack;
        currentReaction = CloseReaction.None;
        circleTimer = 0f;
        ExecuteAttack();
        return;
      }
    }

    // 3. ĐANG CẦM VÉ CẬN CHIẾN: Lao thẳng vào cự ly chém!
    if (hasAttackToken)
    {
      agent.isStopped = false;
      agent.speed = runSpeed;
      agent.SetDestination(playerTransform.position);

      if (distanceToPlayer <= attackRange)
      {
        ExecuteAttack();
        circleTimer = 0f;
        currentReaction = CloseReaction.None;
      }
      return;
    }

    // 4. ĐỊNH KỲ ĐỔI HƯỚNG BƯỚC NGANG & CỰ LY
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

    // 5. PHẢN ỨNG NGẪU NHIÊN KHI PLAYER CHỦ ĐỘNG ÁP SÁT
    if (distanceToPlayer <= closeCombatTriggerRange)
    {
      HandleCloseCombatReaction(distanceToPlayer);
      return;
    }

    currentReaction = CloseReaction.None;
    reactionTimer = 0f;

    // 6. VỜN QUANH BÌNH THƯỜNG Ở CỰ LY LÝ TƯỞNG
    agent.isStopped = false;
    bool isAllyAttacking = AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.ActiveMeleeTokens > 0;
    float currentPreferredDist = isAllyAttacking ? individualPreferredDistance + 0.6f : individualPreferredDistance;

    Vector3 directionToMonster = (transform.position - playerTransform.position).normalized;
    Vector3 tangentDirection = Vector3.Cross(Vector3.up, directionToMonster) * strafeDirection;
    Vector3 separationOffset = CalculateSeparationOffset(directionToMonster, ref tangentDirection);

    Vector3 targetOrbitPosition;
    if (distanceToPlayer < currentPreferredDist - 0.4f)
    {
      agent.speed = retreatSpeed;
      targetOrbitPosition = playerTransform.position + (directionToMonster + separationOffset * 0.5f).normalized * currentPreferredDist;
    }
    else
    {
      agent.speed = strafeSpeed;
      Vector3 blendedDirection = (directionToMonster + tangentDirection * 0.7f + separationOffset).normalized;
      targetOrbitPosition = playerTransform.position + blendedDirection * currentPreferredDist;
    }

    if (NavMesh.SamplePosition(targetOrbitPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
    {
      agent.SetDestination(hit.position);
    }

    // 7. Vờn đủ thời gian -> Xin vé Cận chiến
    circleTimer += Time.deltaTime;
    if (circleTimer >= circleDuration && Time.time >= lastAttackTime + attackCooldown)
    {
      if (AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.RequestAttackToken(this))
      {
        hasAttackToken = true;
      }
    }
  }

  private void HandleCloseCombatReaction(float distanceToPlayer)
  {
    reactionCooldownTimer -= Time.deltaTime;

    if (currentReaction == CloseReaction.None)
    {
      if (reactionCooldownTimer <= 0f)
      {
        int totalWeight = Mathf.Max(1, standGuardChance + sidestepChance + stepBackChance);
        int roll = Random.Range(0, totalWeight);

        if (roll < standGuardChance)
        {
          currentReaction = CloseReaction.StandGuard;
        }
        else if (roll < standGuardChance + sidestepChance)
        {
          currentReaction = CloseReaction.Sidestep;
          strafeDirection = Random.value > 0.5f ? 1 : -1;
        }
        else
        {
          currentReaction = CloseReaction.StepBackOnce;
          isStepBackDone = false;
          Vector3 awayDir = (transform.position - playerTransform.position).normalized;
          Vector3 desiredStepPos = transform.position + awayDir * stepBackDistance;
          if (NavMesh.SamplePosition(desiredStepPos, out NavMeshHit stepHit, 1.5f, NavMesh.AllAreas))
          {
            stepBackTargetPosition = stepHit.position;
          }
          else
          {
            stepBackTargetPosition = transform.position;
            isStepBackDone = true;
          }
        }

        reactionTimer = reactionDuration;
      }
      else
      {
        agent.isStopped = true;
        return;
      }
    }

    switch (currentReaction)
    {
      case CloseReaction.StandGuard:
        agent.isStopped = true;
        break;

      case CloseReaction.Sidestep:
        agent.isStopped = false;
        agent.speed = strafeSpeed * 1.15f;
        Vector3 dirToMe = (transform.position - playerTransform.position).normalized;
        Vector3 lateralDir = Vector3.Cross(Vector3.up, dirToMe) * strafeDirection;
        Vector3 sideTarget = playerTransform.position + (dirToMe + lateralDir * 0.8f).normalized * distanceToPlayer;
        if (NavMesh.SamplePosition(sideTarget, out NavMeshHit sideHit, 1.5f, NavMesh.AllAreas))
        {
          agent.SetDestination(sideHit.position);
        }
        break;

      case CloseReaction.StepBackOnce:
        if (!isStepBackDone)
        {
          agent.isStopped = false;
          agent.speed = retreatSpeed * 1.3f;
          agent.SetDestination(stepBackTargetPosition);

          if (!agent.pathPending && agent.remainingDistance <= 0.25f)
          {
            isStepBackDone = true;
            agent.isStopped = true;
          }
        }
        else
        {
          agent.isStopped = true;
        }
        break;
    }

    reactionTimer -= Time.deltaTime;
    if (reactionTimer <= 0f)
    {
      currentReaction = CloseReaction.None;
      reactionCooldownTimer = 0.5f;
    }
  }

  protected override void HandleAttackState(float distanceToPlayer)
  {
    agent.isStopped = true;
    RotateTowards(playerTransform.position);

    if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
    {
      ExecuteAttack();
    }
    else if (distanceToPlayer > attackRange)
    {
      currentState = AIState.Circling;
    }
  }

  protected override void ExecuteAttack()
  {
    isAttacking = true;
    attackStateTimer = 0f;
    lastAttackTime = Time.time;

    int randomAttack = Random.Range(0, 2);
    if (hasAttackSpeedMultiplierParam)
    {
      animator.SetFloat(AttackSpeedMultiplierHash, attackAnimationSpeed);
    }
    animator.SetInteger(AttackTypeHash, randomAttack);
    animator.SetTrigger(AttackTriggerHash);
  }

  public override void InterruptAttack()
  {
    isAttacking = false;
    attackStateTimer = 0f;
    isPostAttackRetreating = false;
    currentReaction = CloseReaction.None;
    reactionTimer = 0f;
    ReleaseToken();
    if (weaponHitbox != null)
    {
      weaponHitbox.DisableHitbox();
    }
  }

  public override void EnableEnemyAttackHitbox()
  {
    if (weaponHitbox != null) weaponHitbox.EnableHitbox(attackDamage, attackKnockback);
  }

  public override void DisableEnemyAttackHitbox()
  {
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
  }

  public override void OnEnemyAttackEnd()
  {
    isAttacking = false;
    attackStateTimer = 0f;
    isPostAttackRetreating = true;
    postAttackRetreatTimeout = 2.5f;
    currentState = AIState.Circling;
    currentReaction = CloseReaction.None;
    reactionTimer = 0f;
    ReleaseToken();
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
    Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
    Gizmos.DrawWireSphere(transform.position, closeCombatTriggerRange);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, combatStanceRange);
  }
#endif
}
