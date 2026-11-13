using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// ML-Agents Agent cho Boss Red Ignis (The Crimson Duelist - ML-03):
/// 1. Roll-Catching: Đọc vị nhịp lăn né của Player, phạt nặng khi đối thủ vừa dứt I-Frames.
/// 2. Heal-Punish: Trừng phạt tức thì bằng Jump Attack / Sóng kiếm khi Player lùi xa uống máu.
/// 3. Spacing & Duelist Strafing: Giữ cự ly vàng kiếm sĩ (2.5m - 6.5m) và dạt ngang vờn đối thủ.
/// 4. 4 Skill Range Management: Chém dọc (tầm trung), Chém ngang (quét rộng), Nhảy dậm AoE (áp sát), Combo 4 nhát (áp sát hủy diệt).
/// 5. Delayed Attack (Mixup): Đánh trễ 0.35s bắt bài panic roll.
/// 6. Hoàn toàn READ-ONLY đối với Player (bảo toàn 100% mã nguồn nhân vật).
/// </summary>
public class RedIgnisAgent : Agent
{
    [Header("=== THAM CHIẾU BOSS (BODY) ===")]
    [Tooltip("Controller điều khiển thể chất của Red Ignis")]
    [SerializeField] private RedIgnisController controller;
    [Tooltip("Thanh máu của Boss")]
    [SerializeField] private Health bossHealth;

    [Header("=== THAM CHIẾU ĐỐI THỦ (PLAYER - CHỈ ĐỌC READ ONLY) ===")]
    [Tooltip("Transform của đối thủ Player")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Thanh máu của Player (chỉ đọc)")]
    [SerializeField] private Health playerHealth;
    [Tooltip("Bộ điều khiển di chuyển Player (chỉ đọc isRolling, isGrounded)")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("Bộ chiến đấu Player (chỉ đọc isAttacking, isBlockingOrParrying)")]
    [SerializeField] private PlayerCombat playerCombat;
    [Tooltip("Chỉ số thể lực Player (chỉ đọc stamina)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("=== THIẾT LẬP ĐẤU TRƯỜNG & SPACING ===")]
    [Tooltip("Tâm đấu trường (Arena Center)")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [Tooltip("Bán kính đấu trường (mét)")]
    [SerializeField] private float arenaRadius = 25f;
    [Tooltip("Cự ly tối ưu tối thiểu (mét)")]
    [SerializeField] private float optimalDistanceMin = 1.0f;
    [Tooltip("Cự ly tối ưu tối đa (mét)")]
    [SerializeField] private float optimalDistanceMax = 2.0f;
    [Tooltip("Cự ly tối đa cho phép Boss đi lùi (mét). Từ ngưỡng này trở lên sẽ khóa nút lùi, ưu tiên đứng thế hoặc đi vòng ngang")]
    [SerializeField] private float backwardLockDistance = 1.5f;
    [Tooltip("LayerMask của tường/chướng ngại vật")]
    [SerializeField] private LayerMask obstacleMask = 1; // Default layer

    [Header("=== REWARD SHAPING (SOULS-LIKE DUELIST) ===")]
    [Tooltip("Thưởng khi Roll-Catch thành công đối thủ (+1.5)")]
    [SerializeField] private float rollCatchReward = 1.5f;
    [Tooltip("Thưởng khi trừng phạt đối thủ đang hồi máu (+2.0)")]
    [SerializeField] private float healPunishReward = 2.0f;
    [Tooltip("Thưởng khi đánh trúng Player (+1.0)")]
    [SerializeField] private float hitPlayerReward = 1.0f;
    [Tooltip("Thưởng lớn khi hạ gục Player (+5.0)")]
    [SerializeField] private float defeatPlayerReward = 5.0f;
    [Tooltip("Phạt khi tung đòn vào I-Frames lăn né của Player (-0.5)")]
    [SerializeField] private float hitIFramePenalty = -0.5f;
    [Tooltip("Phạt khi tung đòn đánh trượt xa vô ích (-0.4)")]
    [SerializeField] private float whiffAttackPenalty = -0.4f;
    [Tooltip("Phạt khi bị Player đánh trúng (-0.8)")]
    [SerializeField] private float takeDamagePenalty = -0.8f;
    [Tooltip("Phạt khi Boss bị hạ gục (-3.0)")]
    [SerializeField] private float deathPenalty = -3.0f;

    [Header("=== CÀI ĐẶT HỒI SINH (RESPAWN) ===")]
    [Tooltip("Nếu false, khi Boss chết sẽ nằm gục luôn trên sàn, KHÔNG hồi sinh để tiếp tục train")]
    [SerializeField] private bool respawnBossOnDeath = false;

    [Header("=== LOCOMOTION SMOOTHING (CHỐNG GIẬT KHỰNG) ===")]
    [Tooltip("Thời gian tối thiểu duy trì một hướng di chuyển (giây)")]
    [SerializeField] private float movePersistenceTime = 0.60f;
    [Tooltip("Khoảng cách tối thiểu Boss phải đi ngang trước khi được phép đảo chiều (mét)")]
    [SerializeField] private float minStrafeDistance = 2.2f;
    [Tooltip("Thời gian tối thiểu duy trì hướng đi ngang trước khi đổi hướng (giây)")]
    [SerializeField] private float minStrafeDuration = 1.0f;

    private int currentCommittedMoveAction = 0;
    private float moveCommitTimer = 0f;
    private Vector2 currentAiMoveVector = Vector2.zero;

    private int activeStrafeDirection = 0; // 0 = Chưa khóa, 3 = Đi Trái, 4 = Đi Phải
    private Vector3 strafeStartPoint = Vector3.zero;
    private float strafeTimer = 0f;

    // Biến theo dõi trạng thái
    private bool wasPlayerRolling = false;
    private bool isPlayerInRollRecovery = false;
    private Coroutine rollRecoveryCoroutine = null;

    private float lastPlayerHealth = -1f;
    private bool isPlayerHealingWindow = false;
    private Coroutine healWindowCoroutine = null;

    private bool isDelayingAttack = false;
    private Coroutine delayedAttackCoroutine = null;

    private Vector3 initialBossPosition;
    private Quaternion initialBossRotation;
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    // Public properties để Battle Arena truy cập
    [Tooltip("Nếu có RedIgnisBattleArena trong Scene, Arena sẽ làm trọng tài điều phối hiệp đấu, tránh trùng lặp điểm và EndEpisode")]
    [SerializeField] private bool isManagedByArena = false;
    public void SetManagedByArena(bool managed) => isManagedByArena = managed;
    public bool IsManagedByArena => isManagedByArena;
    public void SetRespawnOnDeath(bool enable) => respawnBossOnDeath = enable;
    public RedIgnisController Controller => controller;
    public Health BossHealth => bossHealth;
    public Transform PlayerTarget => playerTransform;

    public override void Initialize()
    {
        initialBossPosition = transform.position;
        initialBossRotation = transform.rotation;

        if (FindFirstObjectByType<RedIgnisBattleArena>() != null)
        {
            isManagedByArena = true;
        }

        if (FindFirstObjectByType<DungeonManager>() != null)
        {
            respawnBossOnDeath = false;
        }

        if (arenaCenter == Vector3.zero)
        {
            arenaCenter = initialBossPosition;
        }

        if (controller == null) controller = GetComponent<RedIgnisController>();
        if (bossHealth == null) bossHealth = GetComponent<Health>();

        AutoDetectPlayerReferences();

        if (bossHealth != null)
        {
            bossHealth.onTakeDamage.AddListener(OnBossTakeDamage);
            bossHealth.onDeath.AddListener(OnBossDeath);
        }
    }

    private void AutoDetectPlayerReferences()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null && controller != null)
            {
                Transform t = controller.GetPlayerTarget();
                if (t != null) playerObj = t.gameObject;
            }
            if (playerObj == null) playerObj = GameObject.Find("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<Health>();
                playerController = playerObj.GetComponent<PlayerController>();
                playerCombat = playerObj.GetComponent<PlayerCombat>();
                if (playerCombat == null) playerCombat = playerObj.GetComponentInChildren<PlayerCombat>();
                playerStats = playerObj.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = playerObj.GetComponentInParent<PlayerStats>();
            }
        }

        if (playerHealth != null)
        {
            playerHealth.onTakeDamage.RemoveListener(OnPlayerTakeDamage);
            playerHealth.onTakeDamage.AddListener(OnPlayerTakeDamage);
            playerHealth.onDeath.RemoveListener(OnPlayerDeath);
            playerHealth.onDeath.AddListener(OnPlayerDeath);
            lastPlayerHealth = playerHealth.CurrentHealth;
        }

        if (playerTransform != null)
        {
            if (initialPlayerPosition == Vector3.zero)
            {
                initialPlayerPosition = playerTransform.position;
                initialPlayerRotation = playerTransform.rotation;
            }
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void RespawnAndResetEntities()
    {
        // 1. Hồi sinh Boss
        if (respawnBossOnDeath)
        {
            if (bossHealth != null)
            {
                bossHealth.Revive();
            }
            if (controller != null)
            {
                controller.Revive();
            }

            if (initialBossPosition != Vector3.zero)
            {
                transform.position = initialBossPosition;
                transform.rotation = initialBossRotation;
            }
        }

        // 2. Hồi sinh Player
        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (initialPlayerPosition != Vector3.zero)
            {
                playerTransform.position = initialPlayerPosition;
                playerTransform.rotation = initialPlayerRotation;
            }

            if (cc != null) cc.enabled = true;

            if (playerHealth != null)
            {
                playerHealth.Revive();
                playerHealth.SetInvulnerable(false);
            }

            if (playerStats != null)
            {
                playerStats.RestoreStamina(playerStats.MaxStamina);
                playerStats.RefillSpiritToFull(); // Hồi đầy 100% thanh năng lượng (Spirit) khi bắt đầu hiệp mới
            }

            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.UnlockMovement();
            }

            var lockSystem = playerTransform.GetComponent<TargetLockSystem>();
            if (lockSystem != null) lockSystem.enabled = true;

            Animator pAnim = playerTransform.GetComponentInChildren<Animator>();
            if (pAnim != null)
            {
                pAnim.ResetTrigger("Die");
                pAnim.ResetTrigger("GetHit");
                pAnim.Play("Locomotion", 0, 0f);
            }

            var botAI = playerTransform.GetComponent<PlayerBotAI>();
            if (botAI != null)
            {
                botAI.ResetBot();
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        AutoDetectPlayerReferences();
        RespawnAndResetEntities();

        if (controller != null)
        {
            controller.ResetAllCooldowns();
        }

        currentCommittedMoveAction = 0;
        moveCommitTimer = 0f;
        activeStrafeDirection = 0;
        strafeTimer = 0f;
        strafeStartPoint = Vector3.zero;
        wasPlayerRolling = false;
        isPlayerInRollRecovery = false;
        isPlayerHealingWindow = false;
        isDelayingAttack = false;

        if (rollRecoveryCoroutine != null) { StopCoroutine(rollRecoveryCoroutine); rollRecoveryCoroutine = null; }
        if (healWindowCoroutine != null) { StopCoroutine(healWindowCoroutine); healWindowCoroutine = null; }
        if (delayedAttackCoroutine != null) { StopCoroutine(delayedAttackCoroutine); delayedAttackCoroutine = null; }

        if (playerHealth != null)
        {
            lastPlayerHealth = playerHealth.CurrentHealth;
        }

        if (playerTransform != null)
        {
            if (initialPlayerPosition == Vector3.zero)
            {
                initialPlayerPosition = playerTransform.position;
                initialPlayerRotation = playerTransform.rotation;
            }
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            AutoDetectPlayerReferences();
            return;
        }

        // Duy trì liên tục lệnh di chuyển ở mỗi render frame
        if (!isDelayingAttack && controller != null && !controller.IsBusy() && !controller.IsDead)
        {
            float dist = (playerTransform != null) ? Vector3.Distance(transform.position, playerTransform.position) : 0f;
            if (dist > optimalDistanceMax && (currentAiMoveVector.x != 0f || currentAiMoveVector.y <= 0f))
            {
                currentAiMoveVector = new Vector2(0f, 1f);
            }
            else if (dist >= backwardLockDistance && currentAiMoveVector.y < 0f)
            {
                currentAiMoveVector.y = 0f;
            }
            else if (dist < optimalDistanceMin && currentAiMoveVector.y > 0f)
            {
                currentAiMoveVector.y = 0f;
            }
            controller.Move(currentAiMoveVector.x, currentAiMoveVector.y);
        }

        // 1. Tính vận tốc Player
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            playerVelocity = (playerTransform.position - lastPlayerPosition) / dt;
            lastPlayerPosition = playerTransform.position;
        }

        // 2. Theo dõi kết thúc Roll để mở cửa sổ Roll-Catching
        if (playerController != null)
        {
            bool currentRolling = playerController.IsRolling;
            if (wasPlayerRolling && !currentRolling)
            {
                if (rollRecoveryCoroutine != null) StopCoroutine(rollRecoveryCoroutine);
                rollRecoveryCoroutine = StartCoroutine(RollRecoveryRoutine(0.4f));
            }
            wasPlayerRolling = currentRolling;
        }

        // 3. Theo dõi hồi máu để mở cửa sổ Heal-Punish
        if (playerHealth != null)
        {
            if (lastPlayerHealth > 0f && playerHealth.CurrentHealth > lastPlayerHealth + 0.5f)
            {
                if (healWindowCoroutine != null) StopCoroutine(healWindowCoroutine);
                healWindowCoroutine = StartCoroutine(HealWindowRoutine(1.5f));
            }
            lastPlayerHealth = playerHealth.CurrentHealth;
        }
    }

    private IEnumerator RollRecoveryRoutine(float duration)
    {
        isPlayerInRollRecovery = true;
        yield return new WaitForSeconds(duration);
        isPlayerInRollRecovery = false;
        rollRecoveryCoroutine = null;
    }

    private IEnumerator HealWindowRoutine(float duration)
    {
        isPlayerHealingWindow = true;
        yield return new WaitForSeconds(duration);
        isPlayerHealingWindow = false;
        healWindowCoroutine = null;
    }

    /// <summary>
    /// Thu thập 27 Vector Observations môi trường
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerTransform == null)
        {
            for (int i = 0; i < 27; i++) sensor.AddObservation(0f);
            return;
        }

        // 1-2. Vị trí tương đối từ Boss tới Player
        Vector3 toPlayer = playerTransform.position - transform.position;
        sensor.AddObservation(Mathf.Clamp(toPlayer.x / arenaRadius, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toPlayer.z / arenaRadius, -1f, 1f));

        // 3. Khoảng cách thực tế tới Player
        float dist = toPlayer.magnitude;
        sensor.AddObservation(Mathf.Clamp01(dist / arenaRadius));

        // 4-5. Góc quay mặt của Boss so với hướng tới Player (Cos & Sin liên tục)
        Vector3 forward = transform.forward;
        Vector3 dirToPlayer = (dist > 0.001f) ? toPlayer / dist : forward;
        float cosAngle = Vector3.Dot(forward, dirToPlayer);
        float sinAngle = Vector3.Cross(forward, dirToPlayer).y;
        sensor.AddObservation(cosAngle);
        sensor.AddObservation(sinAngle);

        // 6. Máu hiện tại của Boss (chuẩn hóa 0..1)
        float bossHpNorm = (bossHealth != null && bossHealth.MaxHealth > 0f) ? (bossHealth.CurrentHealth / bossHealth.MaxHealth) : 1f;
        sensor.AddObservation(bossHpNorm);

        // 7. Boss đang bận (IsBusy)
        sensor.AddObservation((controller != null && controller.IsBusy()) ? 1f : 0f);

        // 8-11. Thời gian hồi chiêu của 4 kỹ năng (0..1)
        for (int i = 0; i < 4; i++)
        {
            float cd = (controller != null) ? controller.GetCooldownNormalized(i) : 0f;
            sensor.AddObservation(cd);
        }

        // 12. Trạng thái Phase 2 / Kiếm lửa thức tỉnh
        sensor.AddObservation((controller != null && controller.IsPhase2) ? 1f : 0f);

        // 13. Máu hiện tại của Player (0..1)
        float playerHpNorm = (playerHealth != null && playerHealth.MaxHealth > 0f) ? (playerHealth.CurrentHealth / playerHealth.MaxHealth) : 1f;
        sensor.AddObservation(playerHpNorm);

        // 14. Thể lực Player (0..1)
        float playerStaminaNorm = (playerStats != null && playerStats.MaxStamina > 0f) ? (playerStats.CurrentStamina / playerStats.MaxStamina) : 1f;
        sensor.AddObservation(playerStaminaNorm);

        // 15. Player đang tấn công
        sensor.AddObservation((playerCombat != null && playerCombat.IsAttacking) ? 1f : 0f);

        // 16. Player đang giơ thế đỡ / Parry
        sensor.AddObservation((playerCombat != null && playerCombat.IsBlockingOrParrying) ? 1f : 0f);

        // 17. Player đang lăn né trong I-Frames
        sensor.AddObservation((playerController != null && playerController.IsRolling) ? 1f : 0f);

        // 18. Player vừa dứt I-Frames (Cửa sổ Roll-Catch)
        sensor.AddObservation(isPlayerInRollRecovery ? 1f : 0f);

        // 19. Player đang chạm đất
        sensor.AddObservation((playerController != null && playerController.IsGrounded) ? 1f : 0f);

        // 20-21. Vận tốc tương đối dọc và ngang của Player
        float playerVelForward = Vector3.Dot(playerVelocity, forward);
        float playerVelRight = Vector3.Dot(playerVelocity, transform.right);
        sensor.AddObservation(Mathf.Clamp(playerVelForward / 10f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(playerVelRight / 10f, -1f, 1f));

        // 22-25. Cảm biến khoảng cách tới tường (Raycast 4 hướng)
        sensor.AddObservation(GetWallDistance(transform.forward));
        sensor.AddObservation(GetWallDistance(-transform.forward));
        sensor.AddObservation(GetWallDistance(-transform.right));
        sensor.AddObservation(GetWallDistance(transform.right));

        // 26-27. Vị trí Boss so với tâm đấu trường
        Vector3 toCenter = arenaCenter - transform.position;
        sensor.AddObservation(Mathf.Clamp(toCenter.x / arenaRadius, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toCenter.z / arenaRadius, -1f, 1f));
    }

    private float GetWallDistance(Vector3 direction)
    {
        float maxSensorDist = 12f;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, direction, out RaycastHit hit, maxSensorDist, obstacleMask))
        {
            return hit.distance / maxSensorDist;
        }
        return 1.0f;
    }

    /// <summary>
    /// Nhận quyết định hành động từ mạng nơ-ron:
    /// Branch 0: Di chuyển & Đi vòng (5 nhánh: 0=Đứng yên, 1=Tới, 2=Lùi, 3=Trái, 4=Phải)
    /// Branch 1: Kỹ năng (5 nhánh: 0=Không đánh, 1=AttackSlash, 2=HorizontalSlash, 3=JumpAttack, 4=ComboAttack)
    /// Branch 2: Timing Mixup (2 nhánh: 0=Đánh ngay, 1=Đánh trễ 0.35s bắt bài roll)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (controller == null || playerTransform == null || controller.IsDead || controller.IsWeakened) return;

        int moveAction = actions.DiscreteActions[0];
        int attackAction = actions.DiscreteActions[1];
        int timingAction = actions.DiscreteActions[2];

        // 1. Xử lý di chuyển
        int selectedMoveAction = moveAction;

        if (attackAction > 0)
        {
            currentCommittedMoveAction = 0;
            moveCommitTimer = 0f;
            selectedMoveAction = 0;
            activeStrafeDirection = 0;
            strafeTimer = 0f;
        }
        else
        {
            if (activeStrafeDirection == 3 || activeStrafeDirection == 4)
            {
                strafeTimer -= Time.fixedDeltaTime;
                float distTraveled = Vector3.Distance(transform.position, strafeStartPoint);
                Vector3 strafeWorldDir = (activeStrafeDirection == 3) ? -transform.right : transform.right;
                bool wallAhead = Physics.Raycast(transform.position + Vector3.up * 1.5f, strafeWorldDir, 2.0f, obstacleMask);

                if ((distTraveled >= minStrafeDistance && strafeTimer <= 0f) || wallAhead)
                {
                    activeStrafeDirection = 0;
                }
                else
                {
                    int oppositeDir = (activeStrafeDirection == 3) ? 4 : 3;
                    if (selectedMoveAction == oppositeDir)
                    {
                        selectedMoveAction = activeStrafeDirection;
                    }
                }
            }

            if (activeStrafeDirection == 0 && (selectedMoveAction == 3 || selectedMoveAction == 4))
            {
                activeStrafeDirection = selectedMoveAction;
                strafeStartPoint = transform.position;
                strafeTimer = minStrafeDuration;
            }

            if (moveCommitTimer > 0f)
            {
                moveCommitTimer -= Time.fixedDeltaTime;
                if (currentCommittedMoveAction != 0)
                {
                    selectedMoveAction = currentCommittedMoveAction;
                }
            }
            else
            {
                currentCommittedMoveAction = selectedMoveAction;
                moveCommitTimer = (selectedMoveAction != 0)
                    ? ((selectedMoveAction == 2) ? Mathf.Min(movePersistenceTime, 0.25f) : movePersistenceTime)
                    : 0f;
            }
        }

        // Kiểm soát Spacing: Khi ở xa (> optimalDistanceMax) buộc tiến tới; khóa lùi sớm khi >= backwardLockDistance
        float distToPlayer = (playerTransform != null) ? Vector3.Distance(transform.position, playerTransform.position) : 0f;
        if (distToPlayer > optimalDistanceMax && attackAction == 0)
        {
            if (selectedMoveAction == 0 || selectedMoveAction == 2 || selectedMoveAction == 3 || selectedMoveAction == 4)
            {
                selectedMoveAction = 1;
                currentCommittedMoveAction = 1;
            }
        }
        else if (distToPlayer >= backwardLockDistance && selectedMoveAction == 2)
        {
            selectedMoveAction = 0;
            currentCommittedMoveAction = 0;
            moveCommitTimer = 0f;
        }
        else if (distToPlayer < optimalDistanceMin && selectedMoveAction == 1)
        {
            selectedMoveAction = 0;
            currentCommittedMoveAction = 0;
            moveCommitTimer = 0f;
        }

        float horiz = 0f;
        float vert = 0f;
        switch (selectedMoveAction)
        {
            case 1: vert = 1f; break;  // Tới
            case 2: vert = -1f; break; // Lùi
            case 3: horiz = -1f; break; // Trái
            case 4: horiz = 1f; break;  // Phải
            default: break;
        }

        currentAiMoveVector = new Vector2(horiz, vert);

        if (!isDelayingAttack && !controller.IsBusy())
        {
            controller.Move(horiz, vert);
        }

        // Thưởng / Phạt Spacing:
        if (distToPlayer >= optimalDistanceMin && distToPlayer <= optimalDistanceMax)
        {
            AddReward(0.004f);
            if (moveAction == 3 || moveAction == 4)
            {
                AddReward(0.006f); // Thưởng đi vòng ngang vờn quanh đối thủ phong cách Duelist
            }
        }
        else if (distToPlayer < optimalDistanceMin && attackAction == 0)
        {
            AddReward(-0.004f); // Nhắc nhẹ khi đứng dính sát hơn cự ly tối thiểu
        }
        else if (distToPlayer > optimalDistanceMax)
        {
            if (moveAction == 1) AddReward(0.006f); // Thưởng tiến tới thu hẹp khoảng cách
            else if (moveAction == 2 || moveAction == 3 || moveAction == 4) AddReward(-0.005f);
        }

        // 2. Xử lý tấn công
        if (attackAction > 0 && !controller.IsBusy() && !isDelayingAttack)
        {
            int skillIndex = attackAction - 1; // 0..3
            if (controller.CanCastAttack(skillIndex))
            {
                if (playerController != null && playerController.IsRolling)
                {
                    AddReward(hitIFramePenalty); // Phạt đánh vào I-Frames
                }

                // Thưởng / Phạt theo cự ly hiệu dụng đồng bộ trực tiếp với thông số Inspector trên Controller:
                float skillMaxRange = GetSkillEffectiveMaxRange(skillIndex);
                if (skillIndex == 3) // Combo Attack (Cận chiến 4 nhát)
                {
                    if (distToPlayer <= skillMaxRange) AddReward(0.35f);
                    else AddReward(-0.8f); // Phạt nặng chém gió cận chiến ở cự ly xa
                }
                else if (skillIndex == 0 || skillIndex == 1) // Sóng kiếm dọc & ngang
                {
                    if (distToPlayer <= skillMaxRange) AddReward(0.35f);
                    else AddReward(-0.4f);
                }
                else if (skillIndex == 2) // Jump Attack (Nhảy dậm đất áp sát từ xa)
                {
                    if (distToPlayer >= 3.5f && distToPlayer <= skillMaxRange) AddReward(0.4f);
                    else if (distToPlayer < 3.0f) AddReward(-0.5f);
                }

                // Xử lý Timing: Đánh ngay hay giữ nhịp khựng lấy đà (Timing Mixup Type 2 trong Animation)
                bool forceMixup = (timingAction == 1);
                controller.CastAttack(skillIndex, forceMixup);
            }
        }
    }

    private IEnumerator DelayedAttackRoutine(int skillIndex, float delay)
    {
        isDelayingAttack = true;
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isDelayingAttack = false;
        delayedAttackCoroutine = null;

        if (controller != null && !controller.IsBusy() && controller.CanCastAttack(skillIndex))
        {
            controller.CastAttack(skillIndex);
        }
    }

    public void OnPlayerTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        float totalReward = hitPlayerReward;

        if (isPlayerInRollRecovery)
        {
            totalReward += rollCatchReward;
            Debug.Log("<color=green>[Red Ignis ML]</color> 🎯 ROLL-CATCH THÀNH CÔNG! Thưởng +" + rollCatchReward);
        }

        if (isPlayerHealingWindow)
        {
            totalReward += healPunishReward;
            Debug.Log("<color=yellow>[Red Ignis ML]</color> ⚡ HEAL-PUNISH THÀNH CÔNG! Thưởng +" + healPunishReward);
        }

        AddReward(totalReward);
    }

    private void OnPlayerDeath()
    {
        if (isManagedByArena) return; // Arena chịu trách nhiệm làm trọng tài điều phối hiệp đấu

        AddReward(defeatPlayerReward);
        Debug.Log("<color=cyan>[Red Ignis ML]</color> 🏆 BOSS CHIẾN THẮNG HIỆP ĐẤU! Thưởng +" + defeatPlayerReward);
        if (respawnBossOnDeath)
        {
            StartCoroutine(DelayedEpisodeReset(1.2f));
        }
    }

    private IEnumerator DelayedEpisodeReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndEpisode();
    }

    private void OnBossTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        AddReward(takeDamagePenalty);
    }

    private void OnBossDeath()
    {
        if (isManagedByArena) return; // Arena chịu trách nhiệm làm trọng tài điều phối hiệp đấu

        AddReward(deathPenalty);
        Debug.Log("<color=red>[Red Ignis ML]</color> 💀 Boss bị hạ gục! Phạt " + deathPenalty);
        if (respawnBossOnDeath)
        {
            StartCoroutine(DelayedEpisodeReset(1.2f));
        }
        else
        {
            enabled = false;
            var requester = GetComponent<DecisionRequester>();
            if (requester != null) requester.enabled = false;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        int move = 0;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.upArrowKey.isPressed) move = 1;
            else if (kb.downArrowKey.isPressed) move = 2;
            else if (kb.leftArrowKey.isPressed) move = 3;
            else if (kb.rightArrowKey.isPressed) move = 4;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) move = 1;
            else if (Input.GetKey(KeyCode.DownArrow)) move = 2;
            else if (Input.GetKey(KeyCode.LeftArrow)) move = 3;
            else if (Input.GetKey(KeyCode.RightArrow)) move = 4;
        }
        discreteActions[0] = move;

        int attack = 0;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.digit1Key.wasPressedThisFrame) attack = 1;
            else if (kb.digit2Key.wasPressedThisFrame) attack = 2;
            else if (kb.digit3Key.wasPressedThisFrame) attack = 3;
            else if (kb.digit4Key.wasPressedThisFrame) attack = 4;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) attack = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) attack = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) attack = 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) attack = 4;
        }
        discreteActions[1] = attack;

        int timing = 0;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed) timing = 1;
        }
        else
        {
            if (Input.GetKey(KeyCode.RightShift)) timing = 1;
        }
        discreteActions[2] = timing;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (controller == null || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Branch 0: Cự ly di chuyển
        if (dist > optimalDistanceMax)
        {
            actionMask.SetActionEnabled(0, 2, false); // Khóa lùi
            actionMask.SetActionEnabled(0, 3, false); // Khóa dạt trái
            actionMask.SetActionEnabled(0, 4, false); // Khóa dạt phải
            activeStrafeDirection = 0;
        }
        else
        {
            if (dist < optimalDistanceMin)
            {
                actionMask.SetActionEnabled(0, 1, false); // Khóa tiến
            }
            if (dist >= backwardLockDistance)
            {
                actionMask.SetActionEnabled(0, 2, false); // Khóa lùi sớm, ưu tiên đứng thế hoặc đi vòng ngang
            }

            if (activeStrafeDirection == 3) actionMask.SetActionEnabled(0, 4, false);
            else if (activeStrafeDirection == 4) actionMask.SetActionEnabled(0, 3, false);
        }

        if (controller.IsWeakened || controller.IsCastingSword)
        {
            for (int m = 1; m <= 4; m++) actionMask.SetActionEnabled(0, m, false);
            for (int i = 1; i <= 4; i++) actionMask.SetActionEnabled(1, i, false);
            return;
        }

        // Branch 1: Kỹ năng
        for (int skillIdx = 0; skillIdx < 4; skillIdx++)
        {
            int actionIndex = skillIdx + 1; // 1..4

            if (!controller.CanCastAttack(skillIdx))
            {
                actionMask.SetActionEnabled(1, actionIndex, false);
                continue;
            }

            float skillMaxRange = GetSkillEffectiveMaxRange(skillIdx);
            bool isRangeValid = true;
            switch (skillIdx)
            {
                case 0: // AttackSlash (Chém dọc: 0m - attackSlashMaxDistance)
                case 1: // HorizontalSlash (Chém ngang: 0m - horizontalSlashMaxDistance)
                case 3: // ComboAttack (Combo cận chiến: tối đa 3.8m)
                    if (dist > skillMaxRange) isRangeValid = false;
                    break;
                case 2: // JumpAttack (Nhảy dậm đất: 3.5m - jumpLeapDistance + jumpStopOffset)
                    if (dist < 3.5f || dist > skillMaxRange) isRangeValid = false;
                    break;
            }

            if (!isRangeValid)
            {
                actionMask.SetActionEnabled(1, actionIndex, false);
            }
        }
    }

    private float GetSkillEffectiveMaxRange(int skillIdx)
    {
        switch (skillIdx)
        {
            case 0:
                return (controller != null) ? Mathf.Max(0.5f, controller.AttackSlashMaxDistance) : 18.0f;
            case 1:
                return (controller != null) ? Mathf.Max(0.5f, controller.HorizontalSlashMaxDistance) : 16.0f;
            case 2:
                return (controller != null) ? Mathf.Max(3.5f, controller.JumpLeapDistance + controller.JumpStopOffset) : 26.3f;
            default:
                return 3.8f;
        }
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.onTakeDamage.RemoveListener(OnBossTakeDamage);
            bossHealth.onDeath.RemoveListener(OnBossDeath);
        }

        if (playerHealth != null)
        {
            playerHealth.onTakeDamage.RemoveListener(OnPlayerTakeDamage);
            playerHealth.onDeath.RemoveListener(OnPlayerDeath);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 groundCenter = transform.position + Vector3.up * 0.05f;

        // 1. Vòng tròn vùng giữ cự ly chiến đấu AI (1m - 1.5m - 2m)
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        RedIgnisController.DrawGroundCircle(groundCenter, optimalDistanceMin);

        Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.9f);
        RedIgnisController.DrawGroundCircle(groundCenter, backwardLockDistance);

        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.95f);
        RedIgnisController.DrawGroundCircle(groundCenter, optimalDistanceMax);

        // 2. Vòng tròn ranh giới đấu trường (Arena Radius)
        Vector3 center = (arenaCenter != Vector3.zero) ? arenaCenter : transform.position;
        center.y = groundCenter.y;
        Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.4f);
        RedIgnisController.DrawGroundCircle(center, arenaRadius, 64);

        // 3. 4 tia cảm biến tường xung quanh (12m theo đúng GetWallDistance)
        Vector3 sensorOrigin = transform.position + Vector3.up * 1.5f;
        Vector3[] sensorDirs = { transform.forward, -transform.forward, -transform.right, transform.right };
        float maxSensorDist = 12f;
        foreach (Vector3 dir in sensorDirs)
        {
            if (Physics.Raycast(sensorOrigin, dir, out RaycastHit hit, maxSensorDist, obstacleMask))
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
                Gizmos.DrawLine(sensorOrigin, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.2f);
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
                Gizmos.DrawRay(sensorOrigin, dir * maxSensorDist);
            }
        }
    }
}
