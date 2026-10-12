using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
  private enum AIState { Idle, Patrol, Chase, Attack }

  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private WeaponHitbox weaponHitbox;

  [Header("Combat Settings")]
  [SerializeField] private float chaseRange = 8f;        // Bán kính phát hiện Player
  [SerializeField] private float attackRange = 1.8f;      // Cự ly vung kiếm chém
  [SerializeField] private float attackCooldown = 1.8f;   // Thời gian nghỉ giữa các đòn đánh (giây)
  [SerializeField] private float attackDamage = 15f;      // Sát thương đòn đánh của quái
  [SerializeField] private float attackKnockback = 0.5f;   // Lực đẩy lùi tác động lên Player

  [Header("Patrol Settings")]
  [SerializeField] private float patrolRadius = 4f;       // Bán kính đi dạo quanh vị trí xuất phát
  [SerializeField] private float patrolWaitTime = 2.5f;   // Thời gian đứng ngắm cảnh trước khi đổi chỗ

  [Header("Movement Speeds")]
  [SerializeField] private float walkSpeed = 1.8f;        // Tốc độ khi đi dạo
  [SerializeField] private float runSpeed = 4.0f;         // Tốc độ khi phi tới cắn Player
  [SerializeField] private float rotationSpeed = 8f;

  private NavMeshAgent agent;
  private Health health;
  private Transform playerTransform;

  private AIState currentState = AIState.Idle;
  private Vector3 spawnPoint;
  private float patrolTimer = 0f;
  private float lastAttackTime = -999f;
  private bool isAttacking = false;

  // Animator Hashes
  private static readonly int SpeedHash = Animator.StringToHash("Speed");
  private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

  private void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    health = GetComponent<Health>();

    if (animator == null) animator = GetComponentInChildren<Animator>();

    spawnPoint = transform.position;
  }

  private void Start()
  {
    // 1. Khởi tạo vũ khí cho quái
    if (weaponHitbox != null)
    {
      weaponHitbox.Initialize(transform.root);
    }

    // 2. Tự động tìm Player theo Tag "Player" trong Scene
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj != null)
    {
      playerTransform = playerObj.transform;
    }

    currentState = AIState.Patrol;
  }

  private void OnEnable()
  {
    // Đăng ký nhận đòn: nếu bị Player chém từ sau lưng ngoài tầm nhìn -> lập tức quay lại rượt
    health.onTakeDamage.AddListener(OnTakeDamageAggro);
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(OnTakeDamageAggro);
  }

  private void Update()
  {
    // Nếu quái đã chết hoặc không có Player, dừng toàn bộ hành vi
    if (health.IsDead || playerTransform == null)
    {
      if (agent.enabled) agent.isStopped = true;
      return;
    }

    // Nếu đang trong hoạt ảnh vung kiếm, đứng yên và xoay nhẹ mặt về Player
    if (isAttacking)
    {
      agent.isStopped = true;
      RotateTowards(playerTransform.position);
      UpdateAnimatorSpeed();
      return;
    }

    float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

    // Quyết định trạng thái AI
    if (distanceToPlayer <= attackRange)
    {
      currentState = AIState.Attack;
    }
    else if (distanceToPlayer <= chaseRange)
    {
      currentState = AIState.Chase;
    }
    else if (currentState == AIState.Chase)
    {
      // Player chạy thoát khỏi tầm rượt -> quay về đi tuần
      currentState = AIState.Patrol;
    }

    // Thực thi trạng thái
    switch (currentState)
    {
      case AIState.Idle:
      case AIState.Patrol:
        HandlePatrol();
        break;

      case AIState.Chase:
        HandleChase();
        break;

      case AIState.Attack:
        HandleAttack();
        break;
    }

    UpdateAnimatorSpeed();
  }

  // =========================================================================
  // HÀNH VI CHI TIẾT
  // =========================================================================

  private void HandlePatrol()
  {
    agent.isStopped = false;
    agent.speed = walkSpeed;

    patrolTimer += Time.deltaTime;

    // Nếu đã đến điểm đi dạo hoặc hết giờ chờ -> tìm điểm ngẫu nhiên mới
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

  private void HandleAttack()
  {
    agent.isStopped = true;
    RotateTowards(playerTransform.position);

    // Kiểm tra hồi chiêu đòn đánh
    if (Time.time >= lastAttackTime + attackCooldown)
    {
      ExecuteAttack();
    }
  }

  private void ExecuteAttack()
  {
    isAttacking = true;
    lastAttackTime = Time.time;

    // Random giữa 0 (Attack_A) và 1 (Attack_B)
    int randomAttack = Random.Range(0, 2);
    animator.SetInteger(AttackTypeHash, randomAttack);
    animator.SetTrigger(AttackTriggerHash);
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

  private void UpdateAnimatorSpeed()
  {
    if (animator == null) return;

    // Chuẩn hóa vận tốc: 0 = đứng yên, ~0.4 = đi bộ, ~1.0 = chạy
    float currentSpeed = agent.velocity.magnitude / runSpeed;
    animator.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);
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
    // Khi bị chém bất ngờ, lập tức aggro và rượt theo Player
    if (!health.IsDead && playerTransform != null)
    {
      currentState = AIState.Chase;
    }
  }

  // =========================================================================
  // ANIMATION EVENTS (Gắn vào clip Attack_A và Attack_B)
  // =========================================================================

  public void EnableEnemyAttackHitbox()
  {
    if (weaponHitbox != null)
    {
      weaponHitbox.EnableHitbox(attackDamage, attackKnockback);
    }
  }

  public void DisableEnemyAttackHitbox()
  {
    if (weaponHitbox != null)
    {
      weaponHitbox.DisableHitbox();
    }
  }

  /// <summary>
  /// Gắn event này ở cuối clip Attack (khoảng 85% - 90%) để quái có thể di chuyển lại
  /// </summary>
  public void OnEnemyAttackEnd()
  {
    isAttacking = false;
    if (weaponHitbox != null)
    {
      weaponHitbox.DisableHitbox();
    }
  }

  // Vẽ bán kính quan sát trong cửa sổ Scene
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, chaseRange);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }
}