using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
  private enum AIState { Idle, Patrol, Chase, Circling, Attack }

  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private WeaponHitbox weaponHitbox;

  [Header("Combat Ranges")]
  [SerializeField] private float chaseRange = 9f;           // Tầm phát hiện
  [SerializeField] private float combatStanceRange = 5f;    // Cự ly chuyển sang thủ thế vờn quanh
  [SerializeField] private float preferredDistance = 3.2f;  // Cự ly lý tưởng quái muốn giữ với Player
  [SerializeField] private float attackRange = 2.0f;        // Tầm vung kiếm
  [SerializeField] private float attackCooldown = 2.2f;     // Thời gian nghỉ giữa các đợt tấn công
  [SerializeField] private float attackDamage = 15f;
  [SerializeField] private float attackKnockback = 0.5f;

  [Header("Circling Behavior")]
  [SerializeField] private float circleDuration = 3f;       // Thời gian vờn quanh trước khi cân nhắc tấn công
  [SerializeField] private float directionChangeInterval = 2.5f; // Thời gian đổi hướng bước ngang (trái <-> phải)

  [Header("Movement Speeds")]
  [SerializeField] private float walkSpeed = 1.8f;
  [SerializeField] private float strafeSpeed = 2.2f;
  [SerializeField] private float runSpeed = 4.2f;
  [SerializeField] private float rotationSpeed = 9f;

  [Header("Patrol Settings")]
  [SerializeField] private float patrolRadius = 4f;
  [SerializeField] private float patrolWaitTime = 2.5f;

  private NavMeshAgent agent;
  private Health health;
  private Transform playerTransform;

  private AIState currentState = AIState.Idle;
  private Vector3 spawnPoint;
  private float patrolTimer = 0f;
  private float lastAttackTime = -999f;
  private float circleTimer = 0f;
  private float directionTimer = 0f;
  private int strafeDirection = 1; // 1: Dạt sang phải, -1: Dạt sang trái
  private bool isAttacking = false;
  private bool hasInitializedOnNavMesh = false;

  // Animator Hashes
  private static readonly int SpeedHash = Animator.StringToHash("Speed");
  private static readonly int InCombatHash = Animator.StringToHash("InCombat");
  private static readonly int StrafeXHash = Animator.StringToHash("StrafeX");
  private static readonly int StrafeZHash = Animator.StringToHash("StrafeZ");
  private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

  public bool IsAttacking => isAttacking;

  private void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    health = GetComponent<Health>();
    if (animator == null) animator = GetComponentInChildren<Animator>();
  }

  private void Start()
  {
    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
    {
      transform.position = hit.position;
      agent.Warp(hit.position);
    }

    spawnPoint = transform.position;

    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
    }

    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj != null)
    {
      playerTransform = playerObj.transform;
    }

    currentState = AIState.Patrol;
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(OnTakeDamageAggro);
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(OnTakeDamageAggro);
  }

  private void Update()
  {
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

    // Đang trong animation chém: Dừng chân và xoay mặt theo Player
    if (isAttacking)
    {
      agent.isStopped = true;
      RotateTowards(playerTransform.position);
      UpdateAnimator();
      return;
    }

    float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

    // =====================================================================
    // PHÂN PHỐI TRẠNG THÁI AI SOULS-LIKE
    // =====================================================================
    if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
    {
      currentState = AIState.Attack;
    }
    else if (distanceToPlayer <= combatStanceRange)
    {
      // Vào cự ly gần -> Bật tư thế chiến đấu (Vờn quanh / Giữ cự ly)
      currentState = AIState.Circling;
    }
    else if (distanceToPlayer <= chaseRange)
    {
      // Ở xa -> Chạy tiếp cận
      currentState = AIState.Chase;
    }
    else
    {
      // Quá xa -> Quay về đi tuần
      currentState = AIState.Patrol;
    }

    // Thực thi hành vi
    switch (currentState)
    {
      case AIState.Idle:
      case AIState.Patrol:
        agent.updateRotation = true; // Cho phép NavMeshAgent tự xoay khi đi tuần
        HandlePatrol();
        break;

      case AIState.Chase:
        agent.updateRotation = true;
        HandleChase();
        break;

      case AIState.Circling:
        agent.updateRotation = false; // TẮT TỰ XOAY: Để code tự khóa mắt vào Player
        HandleCircling(distanceToPlayer);
        break;

      case AIState.Attack:
        agent.updateRotation = false;
        HandleAttack(distanceToPlayer);
        break;
    }

    UpdateAnimator();
  }

  // =========================================================================
  // CÁC HÀNH VI CHI TIẾT
  // =========================================================================

  private void HandlePatrol()
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

  private void HandleChase()
  {
    agent.isStopped = false;
    agent.speed = runSpeed;
    agent.SetDestination(playerTransform.position);
  }

  private void HandleCircling(float distanceToPlayer)
  {
    agent.isStopped = false;
    agent.speed = strafeSpeed;

    // 1. Luôn nhìn thẳng vào người chơi
    RotateTowards(playerTransform.position);

    // 2. Định kỳ đổi hướng bước sang trái hoặc phải
    directionTimer += Time.deltaTime;
    if (directionTimer >= directionChangeInterval)
    {
      strafeDirection = Random.value > 0.5f ? 1 : -1;
      directionTimer = 0f;
    }

    // 3. Tính toán vị trí vờn quanh (Orbit Target)
    Vector3 directionToMonster = (transform.position - playerTransform.position).normalized;
    Vector3 tangentDirection = Vector3.Cross(Vector3.up, directionToMonster) * strafeDirection;

    Vector3 targetOrbitPosition;

    if (distanceToPlayer < preferredDistance - 0.5f)
    {
      // Player áp sát quá gần -> Quái chủ động bước lùi về sau
      targetOrbitPosition = playerTransform.position + directionToMonster * preferredDistance;
    }
    else
    {
      // Giữ cự ly và bước vòng tròn quanh Player
      targetOrbitPosition = playerTransform.position + (directionToMonster + tangentDirection * 0.7f).normalized * preferredDistance;
    }

    // Đặt điểm đến trên NavMesh
    if (NavMesh.SamplePosition(targetOrbitPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
    {
      agent.SetDestination(hit.position);
    }

    // 4. Nếu đã vờn đủ lâu và hết hồi chiêu -> Chủ động lao vào áp sát để tấn công!
    circleTimer += Time.deltaTime;
    if (circleTimer >= circleDuration && Time.time >= lastAttackTime + attackCooldown)
    {
      // Lao thẳng vào cự ly chém
      agent.speed = runSpeed;
      agent.SetDestination(playerTransform.position);

      if (distanceToPlayer <= attackRange)
      {
        ExecuteAttack();
        circleTimer = 0f;
      }
    }
  }

  private void HandleAttack(float distanceToPlayer)
  {
    agent.isStopped = true;
    RotateTowards(playerTransform.position);

    if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
    {
      ExecuteAttack();
    }
  }

  private void ExecuteAttack()
  {
    isAttacking = true;
    lastAttackTime = Time.time;

    int randomAttack = Random.Range(0, 2);
    animator.SetInteger(AttackTypeHash, randomAttack);
    animator.SetTrigger(AttackTriggerHash);
  }

  public void InterruptAttack()
  {
    isAttacking = false;
    if (weaponHitbox != null)
    {
      weaponHitbox.DisableHitbox();
    }
  }

  private void RotateTowards(Vector3 targetPosition)
  {
    Vector3 direction = (targetPosition - transform.position);
    direction.y = 0f;

    if (direction.sqrMagnitude > 0.001f)
    {
      Quaternion targetRotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
  }

  // =========================================================================
  // ĐIỀU KHIỂN ANIMATOR TỰ ĐỘNG THEO VẬN TỐC THỰC TẾ
  // =========================================================================
  private void UpdateAnimator()
  {
    if (animator == null) return;

    bool inCombat = (currentState == AIState.Circling || currentState == AIState.Attack);
    animator.SetBool(InCombatHash, inCombat);

    if (inCombat)
    {
      // Chuyển đổi vận tốc thế giới sang không gian cục bộ (Local Space) của Quái
      Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

      float targetX = localVelocity.x / strafeSpeed;
      float targetZ = localVelocity.z / strafeSpeed;

      animator.SetFloat(StrafeXHash, targetX, 0.1f, Time.deltaTime);
      animator.SetFloat(StrafeZHash, targetZ, 0.1f, Time.deltaTime);
    }
    else
    {
      // Khi ở ngoài, dùng biến Speed đơn thuần cho Locomotion
      float normalizedSpeed = agent.velocity.magnitude / runSpeed;
      animator.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);
    }
  }

  private Vector3 GetRandomNavMeshLocation(Vector3 center, float radius)
  {
    Vector3 randomDirection = Random.insideUnitSphere * radius;
    randomDirection += center;

    if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
    {
      return hit.position;
    }

    return center;
  }

  private void OnTakeDamageAggro(Vector3 hitPoint, Vector3 hitDirection, float knockback)
  {
    if (knockback > 0f)
    {
      InterruptAttack();
    }

    if (!health.IsDead && playerTransform != null)
    {
      currentState = AIState.Circling;
    }
  }

  // Animation Events
  public void EnableEnemyAttackHitbox()
  {
    if (weaponHitbox != null) weaponHitbox.EnableHitbox(attackDamage, attackKnockback);
  }

  public void DisableEnemyAttackHitbox()
  {
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
  }

  public void OnEnemyAttackEnd()
  {
    isAttacking = false;
    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
  }
}