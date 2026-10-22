using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyCombatRole
{
  Melee,  // Quái cận chiến (SkeletonWarrior, SkeletonMinion)
  Ranged  // Quái tầm xa (SkeletonRogue, SkeletonMage)
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public abstract class EnemyAIBase : MonoBehaviour
{
  public enum AIState { Idle, Patrol, Chase, Circling, Attack }

  [Header("Base References")]
  [SerializeField] protected Animator animator;

  [Header("Detection & Stance Ranges")]
  [SerializeField] protected float chaseRange = 10f;          // Tầm phát hiện Player
  [SerializeField] protected float combatStanceRange = 6f;   // Cự ly chuyển sang thủ thế vờn quanh
  [SerializeField] protected float preferredDistance = 3.5f; // Cự ly lý tưởng quái muốn giữ
  [SerializeField] protected float attackRange = 2.0f;       // Cự ly có thể tấn công
  [SerializeField] protected float attackCooldown = 2.2f;    // Thời gian nghỉ giữa các đợt tấn công

  [Header("Movement Speeds")]
  [Tooltip("Tốc độ đi bộ khi đi tuần (m/s)")]
  [SerializeField] protected float walkSpeed = 1.8f;
  [Tooltip("Tốc độ bước ngang vờn quanh (m/s)")]
  [SerializeField] protected float strafeSpeed = 2.0f;
  [Tooltip("Tốc độ chạy đuổi theo hoặc áp sát (m/s)")]
  [SerializeField] protected float runSpeed = 4.2f;
  [Tooltip("Tốc độ xoay mặt theo Player")]
  [SerializeField] protected float rotationSpeed = 9f;

  [Header("Circling Behavior & Spacing")]
  [SerializeField] protected float circleDuration = 3f;       // Thời gian vờn quanh trước khi xin vé tấn công
  [SerializeField] protected float directionChangeInterval = 2.5f; // Thời gian đổi hướng bước ngang
  [SerializeField] protected float distanceVariance = 0.8f;   // Độ lệch khoảng cách vờn ngẫu nhiên
  [SerializeField] protected float crowdRepulsionRadius = 2.4f; // Bán kính né quái đồng minh
  [SerializeField] protected float crowdRepulsionForce = 1.5f;  // Lực đẩy dạt sang hai bên

  [Header("Patrol Settings")]
  [SerializeField] protected float patrolRadius = 8f;
  [SerializeField] protected float patrolWaitTime = 3f;

  [Header("Animation Tuning")]
  [Tooltip("Tốc độ animation đòn đánh (kết hợp với Multiplier 'AttackSpeedMultiplier' trong Animator)")]
  [SerializeField] protected float attackAnimationSpeed = 1.0f;

  // Runtime State
  protected NavMeshAgent agent;
  protected Health health;
  protected Transform playerTransform;
  protected Vector3 spawnPoint;
  protected bool hasInitializedOnNavMesh = false;

  protected AIState currentState = AIState.Idle;
  protected bool hasAttackToken = false;
  protected float lastAttackTime = -99f;
  protected float circleTimer = 0f;
  protected int strafeDirection = 1;
  protected float directionTimer = 0f;
  protected float patrolTimer = 0f;
  protected float individualPreferredDistance;
  protected float distanceReshuffleTimer = 0f;
  protected float attackStateTimer = 0f;

  // Animator Hashes
  protected static readonly int SpeedHash = Animator.StringToHash("Speed");
  protected static readonly int InCombatHash = Animator.StringToHash("InCombat");
  protected static readonly int StrafeXHash = Animator.StringToHash("StrafeX");
  protected static readonly int StrafeZHash = Animator.StringToHash("StrafeZ");
  protected static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  protected static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
  protected static readonly int AttackSpeedMultiplierHash = Animator.StringToHash("AttackSpeedMultiplier");
  protected bool hasAttackSpeedMultiplierParam = false;

  // Optimization & Hibernation State
  protected bool isSleeping = false;
  public bool IsSleeping => isSleeping;
  protected Renderer[] cachedRenderers;
  protected ParticleSystem[] cachedParticles;
  protected Light[] cachedLights;

  // Public Properties
  public abstract EnemyCombatRole CombatRole { get; }
  public abstract bool IsAttacking { get; }
  public AIState CurrentState => currentState;

  protected virtual void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    health = GetComponent<Health>();
    if (animator == null) animator = GetComponentInChildren<Animator>();

    // Cache components phục vụ tối ưu hóa LOD / Hibernation
    cachedRenderers = GetComponentsInChildren<Renderer>(true);
    cachedParticles = GetComponentsInChildren<ParticleSystem>(true);
    cachedLights = GetComponentsInChildren<Light>(true);

    if (animator != null)
    {
      // Tối ưu hóa ma trận xương khi ngoài camera (tiết kiệm 75% GPU/CPU rigging)
      animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

      foreach (var param in animator.parameters)
      {
        if (param.nameHash == AttackSpeedMultiplierHash)
        {
          hasAttackSpeedMultiplierParam = true;
          break;
        }
      }
    }
  }

  protected virtual void Start()
  {
    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
    {
      transform.position = hit.position;
      agent.Warp(hit.position);
    }

    spawnPoint = transform.position;

    if (playerTransform == null)
    {
      GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj != null)
      {
        playerTransform = playerObj.transform;
      }
      else
      {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
      }
    }

    strafeDirection = Random.value > 0.5f ? 1 : -1;
    individualPreferredDistance = preferredDistance + Random.Range(-distanceVariance, distanceVariance);

    if (DungeonEnemyOptimizer.Instance != null)
    {
      DungeonEnemyOptimizer.Instance.RegisterEnemy(this);
    }
  }

  protected virtual void OnEnable()
  {
    if (health != null)
    {
      health.onTakeDamage.AddListener(OnTakeDamageAggro);
      health.onDeath.AddListener(OnDeath);
    }
    if (AttackTokenDirector.Instance != null)
    {
      AttackTokenDirector.Instance.RegisterEnemy(this);
    }
  }

  protected virtual void OnDisable()
  {
    if (health != null)
    {
      health.onTakeDamage.RemoveListener(OnTakeDamageAggro);
      health.onDeath.RemoveListener(OnDeath);
    }
    ReleaseToken();
    if (AttackTokenDirector.HasInstance)
    {
      AttackTokenDirector.Instance.UnregisterEnemy(this);
    }
    if (DungeonEnemyOptimizer.Instance != null)
    {
      DungeonEnemyOptimizer.Instance.UnregisterEnemy(this);
    }
  }

  protected virtual void OnDeath()
  {
    if (isSleeping)
    {
      SetHibernation(false);
    }
    ReleaseToken();
    if (AttackTokenDirector.HasInstance)
    {
      AttackTokenDirector.Instance.UnregisterEnemy(this);
    }
    if (DungeonEnemyOptimizer.Instance != null)
    {
      DungeonEnemyOptimizer.Instance.UnregisterEnemy(this);
    }
  }

  protected virtual void Update()
  {
    if (isSleeping) return; // Ngủ đông khi ở xa người chơi: 0% CPU!

    if (agent == null || !agent.isOnNavMesh) return;

    if (!hasInitializedOnNavMesh)
    {
      spawnPoint = transform.position;
      hasInitializedOnNavMesh = true;
    }

    if (health.IsDead || playerTransform == null)
    {
      InterruptAttack();
      agent.isStopped = true;
      return;
    }

    if (IsAttacking)
    {
      attackStateTimer += Time.deltaTime;

      // Failsafe 1: Nếu Animator đã chuyển về Locomotion hoặc CombatLocomotion
      if (attackStateTimer > 0.25f && animator != null)
      {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!animator.IsInTransition(0) &&
           (stateInfo.IsName("Locomotion") || stateInfo.IsName("CombatLocomotion") || stateInfo.IsName("Idle")))
        {
          OnEnemyAttackEnd();
          attackStateTimer = 0f;
          return;
        }
      }

      // Failsafe 2: Timeout tối đa cho đòn đánh (tránh animation event bị lỡ hoặc bị cắt bỏ)
      if (attackStateTimer >= 2.5f)
      {
        OnEnemyAttackEnd();
        attackStateTimer = 0f;
        return;
      }

      agent.isStopped = true;
      RotateTowards(playerTransform.position);
      UpdateAnimator();
      return;
    }
    else
    {
      attackStateTimer = 0f;
    }

    float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

    // Phân phối trạng thái cơ bản
    DetermineState(distanceToPlayer);

    // Thực thi hành vi
    switch (currentState)
    {
      case AIState.Idle:
      case AIState.Patrol:
        agent.updateRotation = true;
        HandlePatrol();
        break;

      case AIState.Chase:
        agent.updateRotation = true;
        HandleChase();
        break;

      case AIState.Circling:
        agent.updateRotation = false;
        HandleCirclingState(distanceToPlayer);
        break;

      case AIState.Attack:
        agent.updateRotation = false;
        HandleAttackState(distanceToPlayer);
        break;
    }

    UpdateAnimator();
  }

  protected virtual void DetermineState(float distanceToPlayer)
  {
    if (hasAttackToken && distanceToPlayer <= attackRange)
    {
      currentState = AIState.Attack;
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

  protected virtual void HandlePatrol()
  {
    agent.isStopped = false;
    agent.speed = walkSpeed;
    patrolTimer += Time.deltaTime;

    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
    {
      if (patrolTimer >= patrolWaitTime)
      {
        Vector3 newPatrolPoint = GetRandomNavMeshLocation(spawnPoint, patrolRadius);
        agent.SetDestination(newPatrolPoint);
        patrolTimer = 0f;
      }
    }
  }

  protected virtual void HandleChase()
  {
    agent.isStopped = false;
    agent.speed = runSpeed;
    agent.SetDestination(playerTransform.position);
  }

  protected abstract void HandleCirclingState(float distanceToPlayer);
  protected abstract void HandleAttackState(float distanceToPlayer);
  protected abstract void ExecuteAttack();
  public abstract void InterruptAttack();

  public virtual void RevokeAttackToken()
  {
    hasAttackToken = false;
  }

  protected virtual void ReleaseToken()
  {
    if (hasAttackToken)
    {
      hasAttackToken = false;
      if (AttackTokenDirector.HasInstance)
      {
        AttackTokenDirector.Instance.ReleaseAttackToken(this);
      }
    }
  }

  protected void RotateTowards(Vector3 targetPosition)
  {
    Vector3 direction = (targetPosition - transform.position);
    direction.y = 0f;

    if (direction.sqrMagnitude > 0.001f)
    {
      Quaternion targetRotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
  }

  protected Vector3 CalculateSeparationOffset(Vector3 directionToMonster, ref Vector3 tangentDirection)
  {
    Vector3 separationOffset = Vector3.zero;
    if (AttackTokenDirector.Instance != null)
    {
      var allEnemies = AttackTokenDirector.Instance.RegisteredEnemies;
      for (int i = 0; i < allEnemies.Count; i++)
      {
        EnemyAIBase other = allEnemies[i];
        if (other == null || other == this || !other.gameObject.activeInHierarchy) continue;

        Vector3 toOther = other.transform.position - transform.position;
        toOther.y = 0f;
        float dist = toOther.magnitude;

        if (dist < crowdRepulsionRadius && dist > 0.01f)
        {
          float pushStrength = (1f - (dist / crowdRepulsionRadius)) * crowdRepulsionForce;
          separationOffset += (-toOther.normalized) * pushStrength;

          float alignment = Vector3.Dot(tangentDirection, toOther.normalized);
          if (alignment > 0.35f)
          {
            strafeDirection = -strafeDirection;
            directionChangeInterval = Random.Range(2.5f, 4.5f);
            directionTimer = 0f;
            tangentDirection = Vector3.Cross(Vector3.up, directionToMonster) * strafeDirection;
          }
        }
      }
    }
    return separationOffset;
  }

  protected Vector3 GetRandomNavMeshLocation(Vector3 center, float radius)
  {
    Vector3 randomDirection = Random.insideUnitSphere * radius;
    randomDirection += center;

    if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
    {
      return hit.position;
    }
    return center;
  }

  /// <summary>
  /// Bật/Tắt chế độ ngủ đông khi ở xa người chơi để giải phóng tối đa tài nguyên CPU/GPU.
  /// Ngủ đông bao gồm:
  /// 1. Tắt Update() AI và dừng NavMeshAgent (0% CPU Logic & Vật lý).
  /// 2. Tắt Animator (dừng 100% chi phí tính toán chuyển động xương, State Machine).
  /// 3. Tắt Renderers (loại bỏ 100% SkinnedMeshRenderer, Draw Calls và Shadow Caster Passes).
  /// 4. Tắt Particle Systems và Point Lights (nếu có).
  /// </summary>
  public virtual void SetHibernation(bool sleep)
  {
    if (isSleeping == sleep) return;

    // Failsafe: Nếu đang trong chuỗi ra đòn hoặc đang giữ Token tấn công, hoãn ngủ đông
    if (sleep && (currentState == AIState.Attack || hasAttackToken || IsAttacking)) return;

    isSleeping = sleep;

    if (isSleeping)
    {
      // 1. Dừng NavMeshAgent và tắt né tránh va chạm (0% CPU NavMesh)
      if (agent != null && agent.isOnNavMesh)
      {
        agent.isStopped = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
      }

      // 2. Tắt hoàn toàn Animator (dừng 100% tính toán chuyển động xương)
      if (animator != null)
      {
        animator.enabled = false;
      }

      // 3. Tắt Renderers (loại bỏ 100% Draw Calls và Shadow Passes)
      if (cachedRenderers != null)
      {
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
          if (cachedRenderers[i] != null) cachedRenderers[i].enabled = false;
        }
      }

      // 4. Dừng các hiệu ứng Particle nếu có
      if (cachedParticles != null)
      {
        for (int i = 0; i < cachedParticles.Length; i++)
        {
          if (cachedParticles[i] != null) cachedParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
      }

      // 5. Tắt nguồn sáng nếu có
      if (cachedLights != null)
      {
        for (int i = 0; i < cachedLights.Length; i++)
        {
          if (cachedLights[i] != null) cachedLights[i].enabled = false;
        }
      }
    }
    else
    {
      // 1. Phục hồi NavMeshAgent
      if (agent != null && agent.isOnNavMesh)
      {
        agent.isStopped = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
      }

      // 2. Bật lại Animator
      if (animator != null)
      {
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
      }

      // 3. Bật lại Renderers
      if (cachedRenderers != null)
      {
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
          if (cachedRenderers[i] != null) cachedRenderers[i].enabled = true;
        }
      }

      // 4. Phát lại hiệu ứng Particle
      if (cachedParticles != null)
      {
        for (int i = 0; i < cachedParticles.Length; i++)
        {
          if (cachedParticles[i] != null) cachedParticles[i].Play();
        }
      }

      // 5. Bật lại nguồn sáng
      if (cachedLights != null)
      {
        for (int i = 0; i < cachedLights.Length; i++)
        {
          if (cachedLights[i] != null) cachedLights[i].enabled = true;
        }
      }
    }
  }

  protected virtual void OnTakeDamageAggro(Vector3 hitPoint, Vector3 hitDirection, float knockback)
  {
    // Bị người chơi tấn công (kể cả từ xa bằng cung/phép) -> Lập tức thức tỉnh!
    if (isSleeping)
    {
      SetHibernation(false);
    }

    if (knockback > 0f)
    {
      InterruptAttack();
    }

    if (!health.IsDead && playerTransform != null)
    {
      currentState = AIState.Circling;
    }
  }

  protected virtual void UpdateAnimator()
  {
    if (animator == null) return;

    bool inCombat = (currentState == AIState.Circling || currentState == AIState.Attack);
    animator.SetBool(InCombatHash, inCombat);

    if (inCombat)
    {
      Vector3 worldVelocity = agent.velocity;
      Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

      float targetStrafeX = Mathf.Clamp(localVelocity.x / strafeSpeed, -1f, 1f);
      float targetStrafeZ = Mathf.Clamp(localVelocity.z / strafeSpeed, -1f, 1f);

      float currentX = animator.GetFloat(StrafeXHash);
      float currentZ = animator.GetFloat(StrafeZHash);

      animator.SetFloat(StrafeXHash, Mathf.Lerp(currentX, targetStrafeX, Time.deltaTime * 10f));
      animator.SetFloat(StrafeZHash, Mathf.Lerp(currentZ, targetStrafeZ, Time.deltaTime * 10f));
      animator.SetFloat(SpeedHash, 0f);
    }
    else
    {
      float targetSpeed = agent.velocity.magnitude;
      float currentSpeed = animator.GetFloat(SpeedHash);
      animator.SetFloat(SpeedHash, Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 8f));
      animator.SetFloat(StrafeXHash, 0f);
      animator.SetFloat(StrafeZHash, 0f);
    }
  }

  // Animation Event Virtual Hooks (Overridden by specific subclasses)
  public virtual void EnableEnemyAttackHitbox() {}
  public virtual void DisableEnemyAttackHitbox() {}
  public virtual void ShootProjectileEvent() {}
  public virtual void OnEnemyAttackEnd() {}
  public virtual void EnableStaffAura() {}
  public virtual void DisableStaffAura() {}
  public virtual void SpawnMageSpellEvent() {}
}
