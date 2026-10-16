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

  [Header("Circling Diversity & Spacing (Đa dạng hóa vờn quanh)")]
  [Tooltip("Độ lệch ngẫu nhiên khoảng cách vờn quanh (ví dụ: ±0.8m giúp mỗi quái đứng một cự ly khác nhau)")]
  [SerializeField] private float distanceVariance = 0.8f;
  [Tooltip("Bán kính né nhau giữa các quái để tránh vây cùng một chỗ")]
  [SerializeField] private float crowdRepulsionRadius = 2.4f;
  [Tooltip("Lực đẩy dạt sang hai bên khi gặp quái khác cùng đường")]
  [SerializeField] private float crowdRepulsionForce = 1.5f;

  [Header("Movement Speeds")]
  [Tooltip("Tốc độ đi bộ khi đi tuần (khuyên dùng 1.5 - 2.0m/s)")]
  [SerializeField] private float walkSpeed = 1.8f;
  [Tooltip("Tốc độ bước ngang vờn quanh (khuyên dùng 1.8 - 2.2m/s)")]
  [SerializeField] private float strafeSpeed = 2.0f;
  [Tooltip("Tốc độ khi quái bước lùi lại 1 bước trong cận chiến (khuyên dùng 1.0 - 1.2m/s)")]
  [SerializeField] private float retreatSpeed = 1.1f;
  [Tooltip("Tốc độ quái chủ động lùi về lại cự ly preferredDistance sau khi chém (khuyên dùng 1.6 - 2.0m/s)")]
  [SerializeField] private float postAttackRetreatSpeed = 1.8f;
  [Tooltip("Tốc độ chạy đuổi theo hoặc lao vào tấn công")]
  [SerializeField] private float runSpeed = 4.2f;
  [Tooltip("Tốc độ xoay mặt theo Player")]
  [SerializeField] private float rotationSpeed = 9f;

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
  [Tooltip("Tỷ lệ % lùi 1 bước rồi đứng thủ thế khi Player áp sát (0 - 100)")]
  [Range(0, 100)] [SerializeField] private int stepBackChance = 30;
  [Tooltip("Tự động chém trả nếu Player lao vào tầm đánh khi quái đã hồi chiêu xong")]
  [SerializeField] private bool counterAttackIfReady = true;

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

  // Biến đa dạng hóa & Token
  private float individualPreferredDistance;
  private float distanceReshuffleTimer = 0f;
  private bool hasAttackToken = false;

  // Trạng thái lùi tái lập cự ly sau khi chém
  private bool isPostAttackRetreating = false;
  private float postAttackRetreatTimeout = 0f;

  // Phản ứng khi Player áp sát cận chiến
  private enum CloseReaction { None, StandGuard, Sidestep, StepBackOnce }
  private CloseReaction currentReaction = CloseReaction.None;
  private float reactionTimer = 0f;
  private float reactionCooldownTimer = 0f;
  private Vector3 stepBackTargetPosition;
  private bool isStepBackDone = false;

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

    // Khởi tạo hướng lượn ngẫu nhiên cho từng quái (50% trái, 50% phải)
    strafeDirection = Random.value > 0.5f ? 1 : -1;
    directionChangeInterval = Random.Range(2.0f, 4.0f);

    // Cá nhân hóa cự ly vờn quanh riêng cho từng con quái (không con nào đứng trùng cự ly con nào)
    individualPreferredDistance = preferredDistance + Random.Range(-distanceVariance, distanceVariance);

    // Đăng ký vào hệ thống quản lý Token
    if (AttackTokenDirector.Instance != null)
    {
      AttackTokenDirector.Instance.RegisterEnemy(this);
    }

    currentState = AIState.Patrol;
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(OnTakeDamageAggro);
    health.onDeath.AddListener(OnDeath);
    if (AttackTokenDirector.Instance != null)
    {
      AttackTokenDirector.Instance.RegisterEnemy(this);
    }
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(OnTakeDamageAggro);
    health.onDeath.RemoveListener(OnDeath);
    ReleaseToken();
    if (AttackTokenDirector.HasInstance)
    {
      AttackTokenDirector.Instance.UnregisterEnemy(this);
    }
  }

  private void OnDeath()
  {
    isPostAttackRetreating = false;
    ReleaseToken();
    if (AttackTokenDirector.HasInstance)
    {
      AttackTokenDirector.Instance.UnregisterEnemy(this);
    }
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
    if (hasAttackToken && distanceToPlayer <= attackRange)
    {
      currentState = AIState.Attack;
    }
    else if (distanceToPlayer <= combatStanceRange)
    {
      // Vào cự ly gần -> Bật tư thế chiến đấu (Vờn quanh / Giữ cự ly phân tán)
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
    isPostAttackRetreating = false;
    agent.isStopped = false;
    agent.speed = runSpeed;
    agent.SetDestination(playerTransform.position);
  }

  private void HandleCircling(float distanceToPlayer)
  {
    // 1. Luôn nhìn thẳng vào người chơi
    RotateTowards(playerTransform.position);

    // 2. GIAI ĐOẠN LÙI TÁI LẬP CỰ LY SAU KHI CHÉM (Post-Attack Retreat):
    // Sau khi vừa hoàn thành đòn chém, quái ưu tiên lùi lại để tái lập khoảng cách preferredDistance.
    // Trong lúc đang lùi này, tạm thời KHÔNG kích hoạt phản ứng ngẫu nhiên để tránh quái bị khựng lại đứng thủ thế ngay trước mặt Player.
    if (isPostAttackRetreating)
    {
      postAttackRetreatTimeout -= Time.deltaTime;
      bool isAllyAttackingNow = AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.ActiveTokenCount > 0;
      float targetPreferredDist = isAllyAttackingNow ? individualPreferredDistance + 0.6f : individualPreferredDistance;

      // Kiểm tra xem đã lùi đủ xa về preferredDistance chưa (hoặc kẹt góc tường / hết timeout)
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

    // 3. PHẢN CÔNG TỨC THÌ: Nếu Player chủ động áp sát vào tầm đánh và quái đã hồi chiêu xong -> Vung kiếm chém ngay!
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

    // 4. NẾU ĐANG CẦM VÉ TẤN CÔNG: Lao thẳng vào cự ly chém!
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

    // 5. ĐỊNH KỲ ĐỔI HƯỚNG BƯỚC NGANG & CỰ LY (giữ nhịp chiến đấu sống động)
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

    // 6. PHẢN ỨNG NGẪU NHIÊN KHI PLAYER CHỦ ĐỘNG ÁP SÁT CẬN CHIẾN
    // Chỉ kích hoạt khi quái ĐÃ LÙI XONG về cự ly vờn quanh, mà Player lại chủ động áp sát vào
    if (distanceToPlayer <= closeCombatTriggerRange)
    {
      HandleCloseCombatReaction(distanceToPlayer);
      return;
    }

    // Nếu Player đã rời khỏi cự ly áp sát cận chiến -> Reset trạng thái phản ứng
    currentReaction = CloseReaction.None;
    reactionTimer = 0f;

    // 7. VỜN QUANH BÌNH THƯỜNG Ở CỰ LY TRUNG BÌNH (Tách quái đồng minh & giữ cự ly)
    agent.isStopped = false;

    // Nếu có quái đồng minh đang chém, quái chờ tự động dạt ra xa thêm 0.6m để nhường sân khấu 1v1
    bool isAllyAttacking = AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.ActiveTokenCount > 0;
    float currentPreferredDist = isAllyAttacking ? individualPreferredDistance + 0.6f : individualPreferredDistance;

    Vector3 directionToMonster = (transform.position - playerTransform.position).normalized;
    Vector3 tangentDirection = Vector3.Cross(Vector3.up, directionToMonster) * strafeDirection;

    // Né quái đồng minh để không đứng chùm một chỗ
    Vector3 separationOffset = CalculateSeparationOffset(directionToMonster, ref tangentDirection);

    Vector3 targetOrbitPosition;

    if (distanceToPlayer < currentPreferredDist - 0.4f)
    {
      // Nếu hơi gần hơn cự ly lý tưởng -> Lùi lại với tốc độ CHẬM để Player có thể bắt kịp
      agent.speed = retreatSpeed;
      targetOrbitPosition = playerTransform.position + (directionToMonster + separationOffset * 0.5f).normalized * currentPreferredDist;
    }
    else
    {
      // Dạt ngang vòng quanh Player ở tốc độ strafe
      agent.speed = strafeSpeed;
      Vector3 blendedDirection = (directionToMonster + tangentDirection * 0.7f + separationOffset).normalized;
      targetOrbitPosition = playerTransform.position + blendedDirection * currentPreferredDist;
    }

    // Đặt điểm đến trên NavMesh
    if (NavMesh.SamplePosition(targetOrbitPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
    {
      agent.SetDestination(hit.position);
    }

    // 7. Sau khi vờn đủ thời gian và hết hồi chiêu -> Xin vé (Token) để chủ động tấn công
    circleTimer += Time.deltaTime;
    if (circleTimer >= circleDuration && Time.time >= lastAttackTime + attackCooldown)
    {
      if (AttackTokenDirector.Instance != null && AttackTokenDirector.Instance.RequestAttackToken(this))
      {
        hasAttackToken = true;
      }
    }
  }

  /// <summary>
  /// Xử lý các phản ứng ngẫu nhiên khi Player chủ động lao vào áp sát cận chiến
  /// (Đứng thủ thế / Dạt ngang / Lùi 1 bước rồi dừng thủ thế)
  /// </summary>
  private void HandleCloseCombatReaction(float distanceToPlayer)
  {
    reactionCooldownTimer -= Time.deltaTime;

    // Nếu chưa chọn phản ứng nào hoặc phản ứng cũ đã hết hạn -> Roll ngẫu nhiên phản ứng mới
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
        // Trong khoảng dừng quyết định: Đứng thủ thế
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        return;
      }
    }

    // THỰC THI PHẢN ỨNG ĐƯỢC CHỌN
    switch (currentReaction)
    {
      case CloseReaction.StandGuard:
        // ĐỨNG THỦ THẾ: Dừng lại, nhìn thẳng vào Player, sẵn sàng đọ kiếm hoặc cho Player cơ hội chém
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        break;

      case CloseReaction.Sidestep:
        // DẠT NGANG: Bước dạt sang trái hoặc phải quanh Player, không lùi ra xa
        agent.isStopped = false;
        agent.speed = strafeSpeed;
        Vector3 dirToMonster = (transform.position - playerTransform.position).normalized;
        Vector3 tangent = Vector3.Cross(Vector3.up, dirToMonster) * strafeDirection;
        Vector3 sidestepPos = playerTransform.position + (dirToMonster + tangent * 0.7f).normalized * distanceToPlayer;
        if (NavMesh.SamplePosition(sidestepPos, out NavMeshHit sideHit, 1.5f, NavMesh.AllAreas))
        {
          agent.SetDestination(sideHit.position);
        }
        break;

      case CloseReaction.StepBackOnce:
        // LÙI 1 BƯỚC RỒI ĐỨNG THỦ THẾ:
        if (!isStepBackDone)
        {
          agent.isStopped = false;
          agent.speed = retreatSpeed; // LÙI CHẬM
          agent.SetDestination(stepBackTargetPosition);

          float distToStepTarget = Vector3.Distance(transform.position, stepBackTargetPosition);
          if (!agent.pathPending && (distToStepTarget <= 0.35f || agent.remainingDistance <= agent.stoppingDistance + 0.2f))
          {
            isStepBackDone = true;
          }
        }
        else
        {
          // Đã lùi xong 1 bước -> Dừng chân và chuyển sang đứng thủ thế, không lùi nữa!
          agent.isStopped = true;
          agent.velocity = Vector3.zero;
        }
        break;
    }

    // Đếm ngược thời gian phản ứng
    reactionTimer -= Time.deltaTime;
    if (reactionTimer <= 0f)
    {
      currentReaction = CloseReaction.None;
      reactionCooldownTimer = 0.5f; // Khoảng dừng ngắn trước khi đổi phản ứng mới
    }
  }

  /// <summary>
  /// Tính toán lực đẩy tách quái đồng minh để không bu cùng một hướng
  /// </summary>
  private Vector3 CalculateSeparationOffset(Vector3 directionToMonster, ref Vector3 tangentDirection)
  {
    Vector3 separationOffset = Vector3.zero;
    if (AttackTokenDirector.Instance != null)
    {
      var allEnemies = AttackTokenDirector.Instance.RegisteredEnemies;
      for (int i = 0; i < allEnemies.Count; i++)
      {
        EnemyAI other = allEnemies[i];
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

  private void HandleAttack(float distanceToPlayer)
  {
    agent.isStopped = true;
    RotateTowards(playerTransform.position);

    if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
    {
      ExecuteAttack();
    }
    else if (distanceToPlayer > attackRange)
    {
      // Player đã lùi xa khỏi tầm đánh -> Quay về Circling để tiếp cận lại
      currentState = AIState.Circling;
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
    isPostAttackRetreating = false;
    currentReaction = CloseReaction.None;
    reactionTimer = 0f;
    ReleaseToken();
    if (weaponHitbox != null)
    {
      weaponHitbox.DisableHitbox();
    }
  }

  public void RevokeAttackToken()
  {
    hasAttackToken = false;
  }

  private void ReleaseToken()
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
    isPostAttackRetreating = false;
    if (knockback > 0f)
    {
      InterruptAttack();
    }

    currentReaction = CloseReaction.None;
    reactionTimer = 0f;

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
    Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Cam: Vùng phản ứng cận chiến
    Gizmos.DrawWireSphere(transform.position, closeCombatTriggerRange);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, combatStanceRange);
  }
#endif
}