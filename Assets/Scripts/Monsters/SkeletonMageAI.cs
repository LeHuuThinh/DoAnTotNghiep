using UnityEngine;
using UnityEngine.AI;

public class SkeletonMageAI : EnemyAIBase
{
  [Header("Mage Combat Settings (Pháp Sư)")]
  [Tooltip("Prefab vòng tròn cảnh báo và bộc phát phép (gắn script MageGroundSpell)")]
  [SerializeField] private GameObject groundSpellPrefab;
  [Tooltip("Vị trí đầu cây trượng (để đặt hào quang aura). Nếu để trống code tự tìm cây trượng trong người")]
  [SerializeField] private Transform staffTipPoint;
  [Tooltip("Hiệu ứng hào quang (Aura VFX) trên đầu trượng (ví dụ: Aura Burn, Aura Lightning)")]
  [SerializeField] private GameObject staffAuraVfx;
  [SerializeField] private float spellRadius = 2.8f;
  [SerializeField] private float spellChargeDuration = 1.3f;
  [SerializeField] private float spellDamage = 35f;
  [Tooltip("Layer vật cản kiểm tra tầm nhìn")]
  [SerializeField] private LayerMask obstacleLayerMask = 1; // Default

  [Header("Mage Kiting Settings (Thả Diều)")]
  [Tooltip("Cự ly tối thiểu: Nếu Player áp sát gần hơn mức này, Mage sẽ lùi lại giữ khoảng cách")]
  [SerializeField] private float minKiteDistance = 5.0f;
  [Tooltip("Tốc độ chạy lùi thả diều (m/s)")]
  [SerializeField] private float kiteSpeed = 2.8f;

  private bool isAttacking = false;

  public override EnemyCombatRole CombatRole => EnemyCombatRole.Ranged;
  public override bool IsAttacking => isAttacking;

  protected override void Awake()
  {
    base.Awake();

    // Mặc định cự ly đánh xa của Pháp sư
    if (preferredDistance < 5f) preferredDistance = 10.0f;
    if (attackRange < 6f) attackRange = 14.0f;
    if (combatStanceRange < 8f) combatStanceRange = 16.0f;
    if (chaseRange < 12f) chaseRange = 20.0f;
    if (attackCooldown < 2.0f) attackCooldown = 3.5f;

    // Tự tìm đầu trượng nếu chưa kéo tay
    if (staffTipPoint == null)
    {
      Transform staff = transform.Find("Skeleton_Staff");
      if (staff == null)
      {
        var allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
          if (t.name.Contains("Staff") || t.name.Contains("staff"))
          {
            staffTipPoint = t;
            break;
          }
        }
      }
      else
      {
        staffTipPoint = staff;
      }
    }

    // Mặc định tắt hào quang trượng khi chưa niệm phép
    if (staffAuraVfx != null)
    {
      staffAuraVfx.SetActive(false);
    }
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

    // 1. KITING: Nếu Player áp sát quá gần -> Mage lập tức lùi lại giữ cự ly
    if (distanceToPlayer <= minKiteDistance)
    {
      agent.isStopped = false;
      agent.speed = kiteSpeed;
      Vector3 awayDir = (transform.position - playerTransform.position).normalized;
      Vector3 targetPos = transform.position + awayDir * 4.0f;

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

    // 4. VỜN QUANH Ở CỰ LY XA
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
    DisableStaffAura();
    ReleaseToken();
  }

  // =========================================================================
  // ANIMATION EVENTS CHO PHÁP SƯ
  // =========================================================================

  /// <summary>
  /// Gọi tại đoạn Mage giơ trượng lên trời -> Bật aura trên đầu trượng
  /// </summary>
  public override void EnableStaffAura()
  {
    if (staffAuraVfx != null)
    {
      staffAuraVfx.SetActive(true);
    }
  }

  /// <summary>
  /// Tắt aura khi hạ trượng hoặc kết thúc cast
  /// </summary>
  public override void DisableStaffAura()
  {
    if (staffAuraVfx != null)
    {
      staffAuraVfx.SetActive(false);
    }
  }

  /// <summary>
  /// Gọi tại frame Mage trỏ trượng về phía trước hoặc kết thúc gồng -> Tạo vòng tròn ma pháp dưới chân Player
  /// </summary>
  public override void SpawnMageSpellEvent()
  {
    if (groundSpellPrefab == null || playerTransform == null) return;

    Vector3 targetPos = playerTransform.position;
    if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
    {
      targetPos = navHit.position;
    }

    GameObject spellObj = Instantiate(groundSpellPrefab, targetPos, Quaternion.identity);

    MageGroundSpell groundSpell = spellObj.GetComponent<MageGroundSpell>();
    if (groundSpell != null)
    {
      groundSpell.Initialize(transform.root, spellRadius, spellChargeDuration, spellDamage);
    }
  }

  public override void OnEnemyAttackEnd()
  {
    isAttacking = false;
    attackStateTimer = 0f;
    DisableStaffAura();
    currentState = AIState.Circling;
    ReleaseToken();
  }

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
        return false;
      }
    }
    return true;
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
