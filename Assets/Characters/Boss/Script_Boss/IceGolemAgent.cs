using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// ML-Agents Agent cho Boss Ice Golem (BOSS-01) với hành vi Souls-like đỉnh cao:
/// 1. Roll-Catching: Bắt bài né tránh, phạt nặng khi đối thủ kết thúc I-Frames.
/// 2. Heal-Punish: Trừng phạt ngay lập tức khi Player cố tình lùi lại uống máu / hồi phục.
/// 3. Spacing & Strafing: Duy trì cự ly vàng (3.5m - 8m) và đi vòng (Circling) quanh đối thủ.
/// 4. 7 Skill Management: Chọn đúng chiêu thức theo khoảng cách thực tế.
/// 5. Delayed Attack (Mixup): Đánh trễ nhịp để bẫy Player lăn sớm.
/// 6. Hoàn toàn READ-ONLY với Player (tuyệt đối không can thiệp script Player).
/// </summary>
public class IceGolemAgent : Agent
{
    [Header("=== THAM CHIẾU BOSS (BODY) ===")]
    [Tooltip("Controller điều khiển thể chất của Boss")]
    [SerializeField] private IceGolemController controller;
    [Tooltip("Thanh máu của Boss")]
    [SerializeField] private Health bossHealth;

    [Header("=== THAM CHIẾU ĐỐI THỦ (PLAYER - CHỈ ĐỌC READ ONLY) ===")]
    [Tooltip("Transform của đối thủ Player")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Thanh máu của Player (chỉ đọc)")]
    [SerializeField] private Health playerHealth;
    [Tooltip("Bộ điều khiển di chuyển Player (chỉ đọc isRolling, isGrounded)")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("Bộ chiến đấu Player (chỉ đọc isAttacking, isBlocking)")]
    [SerializeField] private PlayerCombat playerCombat;
    [Tooltip("Chỉ số thể lực Player (chỉ đọc stamina)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("=== THIẾT LẬP ĐẤU TRƯỜNG & SPACING ===")]
    [Tooltip("Tâm đấu trường (Arena Center)")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [Tooltip("Bán kính đấu trường (mét)")]
    [SerializeField] private float arenaRadius = 25f;
    [Tooltip("Cự ly tối ưu tối thiểu (mét)")]
    [SerializeField] private float optimalDistanceMin = 3.5f;
    [Tooltip("Cự ly tối ưu tối đa (mét)")]
    [SerializeField] private float optimalDistanceMax = 8.0f;
    [Tooltip("LayerMask của tường/chướng ngại vật")]
    [SerializeField] private LayerMask obstacleMask = 1; // Default layer

    [Header("=== REWARD SHAPING (SOULS-LIKE) ===")]
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

    [Header("=== ĐIỀU KHIỂN TỐC ĐỘ GAME (TIME SCALE) ===")]
    [Tooltip("Khóa tốc độ game ở mức 1x bình thường để người chơi kịp quan sát / tự đánh")]
    [SerializeField] private bool lockNormalSpeed = true;

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

    // Public properties để Arena Manager truy cập
    public IceGolemController Controller => controller;
    public Health BossHealth => bossHealth;
    public Transform PlayerTarget => playerTransform;

    public override void Initialize()
    {
        initialBossPosition = transform.position;
        initialBossRotation = transform.rotation;
        if (arenaCenter == Vector3.zero)
        {
            arenaCenter = initialBossPosition;
        }

        // Tự động tìm Controller và Health của Boss nếu chưa gán
        if (controller == null) controller = GetComponent<IceGolemController>();
        if (bossHealth == null) bossHealth = GetComponent<Health>();

        // Tự động tìm Player nếu chưa gán
        AutoDetectPlayerReferences();

        // Đăng ký sự kiện sát thương Boss
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

    /// <summary>
    /// Hồi sinh Boss và Player, đưa cả hai về vị trí xuất phát khi bắt đầu Episode/Hiệp đấu mới
    /// </summary>
    private void RespawnAndResetEntities()
    {
        // 1. Hồi sinh Boss
        if (bossHealth != null)
        {
            bossHealth.Revive();
        }
        if (controller != null)
        {
            controller.Revive();
            controller.CleanupResidualShockwaves();
        }

        if (initialBossPosition != Vector3.zero)
        {
            transform.position = initialBossPosition;
            transform.rotation = initialBossRotation;
        }

        // 2. Hồi sinh Player
        if (playerTransform != null)
        {
            // Tạm tắt CharacterController trước khi set transform.position để Unity không chặn việc di chuyển
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (initialPlayerPosition != Vector3.zero)
            {
                playerTransform.position = initialPlayerPosition;
                playerTransform.rotation = initialPlayerRotation;
            }

            if (cc != null) cc.enabled = true;

            // Hồi phục Máu Player & tắt bất tử
            if (playerHealth != null)
            {
                playerHealth.Revive();
                playerHealth.SetInvulnerable(false);
            }

            // Hồi phục Thể lực Player
            if (playerStats != null)
            {
                playerStats.RestoreStamina(playerStats.MaxStamina);
            }

            // Bật lại các script của Player nếu đã từng bị tắt khi chết
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.UnlockMovement();
            }

            var lockSystem = playerTransform.GetComponent<TargetLockSystem>();
            if (lockSystem != null) lockSystem.enabled = true;

            var rHand = playerTransform.GetComponentInChildren<RHandAdjust>();
            if (rHand != null) rHand.enabled = true;

            // Đưa Animator Player về Locomotion
            Animator pAnim = playerTransform.GetComponentInChildren<Animator>();
            if (pAnim != null)
            {
                pAnim.ResetTrigger("Die");
                pAnim.ResetTrigger("GetHit");
                pAnim.Play("Locomotion", 0, 0f);
            }

            // Báo cho PlayerBotAI reset trạng thái
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

        // Reset hồi chiêu của Boss
        if (controller != null)
        {
            controller.ResetAllCooldowns();
        }

        // Reset các cờ theo dõi
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
        // 0. Điều khiển tốc độ game: Bấm phím T để chuyển đổi giữa 1x (thực tế) và 5x (tăng tốc training)
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            lockNormalSpeed = !lockNormalSpeed;
            Time.timeScale = lockNormalSpeed ? 1.0f : 5.0f;
            Debug.Log($"<color=yellow>[Tốc Độ Game]</color> Đã đổi Time.timeScale = {Time.timeScale}x (Phím T)");
        }

        if (lockNormalSpeed && Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
        }

        if (playerTransform == null)
        {
            AutoDetectPlayerReferences();
            return;
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
                // Player vừa kết thúc I-Frames -> Mở cửa sổ bắt bài (Roll-Catch Window: 0.4s)
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
                // Máu Player tăng lên -> Vừa uống máu / dùng bình dược
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
    /// Thu thập 29 thông tin môi trường quan sát (Vector Observations)
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerTransform == null)
        {
            for (int i = 0; i < 29; i++) sensor.AddObservation(0f);
            return;
        }

        // 1-2. Vị trí tương đối từ Boss tới Player (chuẩn hóa theo bán kính đấu trường)
        Vector3 toPlayer = playerTransform.position - transform.position;
        sensor.AddObservation(Mathf.Clamp(toPlayer.x / arenaRadius, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toPlayer.z / arenaRadius, -1f, 1f));

        // 3. Khoảng cách thực tế tới Player (chuẩn hóa 0..1)
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

        // 7. Boss đang bận tung chiêu / nhảy (IsBusy)
        sensor.AddObservation((controller != null && controller.IsBusy()) ? 1f : 0f);

        // 8-14. Thời gian hồi chiêu của 7 kỹ năng (chuẩn hóa 0..1: 0 = sẵn sàng, 1 = đang hồi)
        for (int i = 0; i < 7; i++)
        {
            float cd = (controller != null) ? controller.GetCooldownNormalized(i) : 0f;
            sensor.AddObservation(cd);
        }

        // 15. Máu hiện tại của Player (chuẩn hóa 0..1)
        float playerHpNorm = (playerHealth != null && playerHealth.MaxHealth > 0f) ? (playerHealth.CurrentHealth / playerHealth.MaxHealth) : 1f;
        sensor.AddObservation(playerHpNorm);

        // 16. Thể lực hiện tại của Player (chuẩn hóa 0..1)
        float playerStaminaNorm = (playerStats != null && playerStats.MaxStamina > 0f) ? (playerStats.CurrentStamina / playerStats.MaxStamina) : 1f;
        sensor.AddObservation(playerStaminaNorm);

        // 17. Player đang chém / tấn công (IsAttacking)
        sensor.AddObservation((playerCombat != null && playerCombat.IsAttacking) ? 1f : 0f);

        // 18. Player đang giơ khiên đỡ / Parry (IsBlockingOrParrying)
        sensor.AddObservation((playerCombat != null && playerCombat.IsBlockingOrParrying) ? 1f : 0f);

        // 19. Player đang lăn né trong I-Frames (IsRolling)
        sensor.AddObservation((playerController != null && playerController.IsRolling) ? 1f : 0f);

        // 20. Player vừa kết thúc I-Frames lăn né (Cơ hội Roll-Catch vàng)
        sensor.AddObservation(isPlayerInRollRecovery ? 1f : 0f);

        // 21. Player đang chạm đất (IsGrounded)
        sensor.AddObservation((playerController != null && playerController.IsGrounded) ? 1f : 0f);

        // 22-23. Vận tốc di chuyển tương đối của Player (dọc và ngang theo góc nhìn Boss)
        float playerVelForward = Vector3.Dot(playerVelocity, forward);
        float playerVelRight = Vector3.Dot(playerVelocity, transform.right);
        sensor.AddObservation(Mathf.Clamp(playerVelForward / 10f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(playerVelRight / 10f, -1f, 1f));

        // 24-27. Cảm biến khoảng cách tới tường đấu trường (4 tia Raycast: Trước, Sau, Trái, Phải)
        sensor.AddObservation(GetWallDistance(transform.forward));
        sensor.AddObservation(GetWallDistance(-transform.forward));
        sensor.AddObservation(GetWallDistance(-transform.right));
        sensor.AddObservation(GetWallDistance(transform.right));

        // 28-29. Vị trí của Boss so với tâm đấu trường (chuẩn hóa -1..1)
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
        return 1.0f; // An toàn, xa tường
    }

    /// <summary>
    /// Tiếp nhận quyết định hành động từ Mạng nơ-ron PPO
    /// Branch 0: Di chuyển & Đi vòng (5 nhánh: 0=Đứng yên, 1=Tới, 2=Lùi, 3=Trái, 4=Phải)
    /// Branch 1: Kỹ năng chiến đấu (8 nhánh: 0=Không ra chiêu, 1..7 tương ứng Chiêu 1..7)
    /// Branch 2: Đánh trễ / Timing Mixup (2 nhánh: 0=Đánh ngay, 1=Đánh trễ 0.35s để bắt bài roll)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (controller == null || playerTransform == null) return;

        int moveAction = actions.DiscreteActions[0];
        int attackAction = actions.DiscreteActions[1];
        int timingAction = actions.DiscreteActions[2];

        // 1. Xử lý di chuyển
        float horiz = 0f;
        float vert = 0f;
        switch (moveAction)
        {
            case 1: vert = 1f; break;  // Tới
            case 2: vert = -1f; break; // Lùi
            case 3: horiz = -1f; break; // Đi vòng sang trái
            case 4: horiz = 1f; break;  // Đi vòng sang phải
            default: break;            // Đứng yên
        }

        if (!isDelayingAttack && !controller.IsBusy())
        {
            controller.Move(horiz, vert);
        }

        // Thưởng phạt về Cự ly và Di chuyển (Spacing & Circling):
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // A. Cự ly lý tưởng (3.5m - 8.0m): Thưởng giữ cự ly + Thưởng đi vòng quanh vờn đối thủ
        if (distToPlayer >= optimalDistanceMin && distToPlayer <= optimalDistanceMax)
        {
            AddReward(0.004f);
            // Nếu đang đi vòng quanh (Strafing Left/Right) ở cự ly đẹp -> Thưởng thêm để tạo phong cách Souls-like
            if (moveAction == 3 || moveAction == 4)
            {
                AddReward(0.006f);
            }
        }
        // B. Bị dính quá sát (< 2.2m) mà không ra chiêu chém:
        else if (distToPlayer < 2.2f && attackAction == 0)
        {
            AddReward(-0.006f); // Phạt lười lùi khi bị áp sát
            // Nhưng nếu chủ động chọn Lùi (MoveAction == 2) để tạo khoảng cách -> Thưởng bù để khuyến khích lùi
            if (moveAction == 2)
            {
                AddReward(0.008f);
            }
        }
        // C. Ở quá xa (> 14m) mà đứng yên hoặc lùi thêm: Phạt nhẹ vì trốn chạy
        else if (distToPlayer > 14f && (moveAction == 0 || moveAction == 2))
        {
            AddReward(-0.004f);
        }

        // Phạt nhẹ nếu dí sát tường đấu trường
        if (GetWallDistance(-transform.forward) < 0.15f || GetWallDistance(transform.forward) < 0.15f)
        {
            AddReward(-0.005f);
        }

        // 2. Xử lý tấn công
        if (attackAction > 0 && !controller.IsBusy() && !isDelayingAttack)
        {
            int skillIndex = attackAction - 1; // 0..6
            if (controller.CanCastAttack(skillIndex))
            {
                // Kiểm tra tung đòn vào lúc Player đang lăn né (I-Frames) -> Phạt vì chọn thời điểm tồi
                if (playerController != null && playerController.IsRolling)
                {
                    AddReward(hitIFramePenalty);
                }

                // Thưởng / Phạt theo Cự ly hiệu dụng của từng chiêu (Effective Range):
                if (skillIndex == 0 || skillIndex == 5) // Chiêu 1 & Chiêu 6 (Combo cận chiến)
                {
                    if (distToPlayer <= 3.8f)
                    {
                        AddReward(0.35f); // Thưởng ra đòn cận chiến chuẩn cự ly!
                    }
                    else
                    {
                        AddReward(-0.8f); // Phạt nặng chém gió vô ích khi ở xa!
                    }
                }
                else if (skillIndex == 1) // Chiêu 2 (GumoGumo - đấm đất)
                {
                    if (distToPlayer <= 5.5f) AddReward(0.3f);
                    else AddReward(-0.6f);
                }
                else if (skillIndex == 2) // Chiêu 3 (FlameBreath - phun lửa)
                {
                    if (distToPlayer >= 3.5f && distToPlayer <= 14.0f) AddReward(0.35f);
                    else AddReward(-0.5f);
                }
                else if (skillIndex == 3 || skillIndex == 4 || skillIndex == 6) // Chiêu 4 (Jump), 5 (Run), 7 (IceBall)
                {
                    if (distToPlayer >= 5.0f && distToPlayer <= 18.0f) AddReward(0.4f);
                    else if (distToPlayer < 4.0f) AddReward(-0.6f); // Phạt lãng phí chiêu tầm xa ở cự ly áp sát
                }

                // Xử lý Timing: Đánh ngay hay đánh trễ nhịp (Delayed Attack)
                if (timingAction == 1)
                {
                    if (delayedAttackCoroutine != null) StopCoroutine(delayedAttackCoroutine);
                    delayedAttackCoroutine = StartCoroutine(DelayedAttackRoutine(skillIndex, 0.35f));
                }
                else
                {
                    controller.CastAttack(skillIndex);
                }
            }
        }
    }

    private IEnumerator DelayedAttackRoutine(int skillIndex, float delay)
    {
        isDelayingAttack = true;
        // Trong lúc hoãn đòn, Boss vẫn xoay người ghim chặt Player
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.deltaTime;
            if (playerTransform != null && controller != null)
            {
                controller.SmoothFaceTarget(playerTransform, 300f);
            }
            yield return null;
        }

        isDelayingAttack = false;
        delayedAttackCoroutine = null;

        if (controller != null && !controller.IsBusy() && controller.CanCastAttack(skillIndex))
        {
            controller.CastAttack(skillIndex);
        }
    }

    /// <summary>
    /// Xử lý phản hồi khi Player bị trúng đòn (Tính điểm thưởng Souls-like)
    /// </summary>
    public void OnPlayerTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        float totalReward = hitPlayerReward; // Base +1.0

        // 1. Roll-Catching: Trúng đòn khi Player vừa dứt Roll
        if (isPlayerInRollRecovery)
        {
            totalReward += rollCatchReward; // +1.5 -> Tổng +2.5
            Debug.Log("<color=green>[ML-Agents]</color> 🎯 ROLL-CATCH THÀNH CÔNG! Thưởng +" + rollCatchReward);
        }

        // 2. Heal-Punish: Trúng đòn khi Player đang cố uống máu
        if (isPlayerHealingWindow)
        {
            totalReward += healPunishReward; // +2.0 -> Tổng +3.0
            Debug.Log("<color=yellow>[ML-Agents]</color> ⚡ HEAL-PUNISH THÀNH CÔNG! Thưởng +" + healPunishReward);
        }

        AddReward(totalReward);
    }

    private void OnPlayerDeath()
    {
        AddReward(defeatPlayerReward); // +5.0
        Debug.Log("<color=cyan>[ML-Agents]</color> 🏆 BOSS CHIẾN THẮNG HIỆP ĐẤU! Thưởng +" + defeatPlayerReward);
        EndEpisode();
    }

    private void OnBossTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        AddReward(takeDamagePenalty); // -0.8
    }

    private void OnBossDeath()
    {
        AddReward(deathPenalty); // -3.0
        Debug.Log("<color=red>[ML-Agents]</color> 💀 Boss bị hạ gục! Phạt " + deathPenalty);
        EndEpisode();
    }

    /// <summary>
    /// Điều khiển thủ công Boss để thử nghiệm (Heuristic Mode) bằng 4 phím Mũi Tên và phím 1..7
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        // 1. Phím mũi tên điều khiển di chuyển (Branch 0)
        int move = 0;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.upArrowKey.isPressed) move = 1;      // Tới
            else if (kb.downArrowKey.isPressed) move = 2; // Lùi
            else if (kb.leftArrowKey.isPressed) move = 3; // Trái
            else if (kb.rightArrowKey.isPressed) move = 4; // Phải
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) move = 1;
            else if (Input.GetKey(KeyCode.DownArrow)) move = 2;
            else if (Input.GetKey(KeyCode.LeftArrow)) move = 3;
            else if (Input.GetKey(KeyCode.RightArrow)) move = 4;
        }
        discreteActions[0] = move;

        // 2. Phím số 1..7 tung chiêu (Branch 1)
        int attack = 0;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.digit1Key.wasPressedThisFrame) attack = 1;
            else if (kb.digit2Key.wasPressedThisFrame) attack = 2;
            else if (kb.digit3Key.wasPressedThisFrame) attack = 3;
            else if (kb.digit4Key.wasPressedThisFrame) attack = 4;
            else if (kb.digit5Key.wasPressedThisFrame) attack = 5;
            else if (kb.digit6Key.wasPressedThisFrame) attack = 6;
            else if (kb.digit7Key.wasPressedThisFrame) attack = 7;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) attack = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) attack = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) attack = 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) attack = 4;
            else if (Input.GetKeyDown(KeyCode.Alpha5)) attack = 5;
            else if (Input.GetKeyDown(KeyCode.Alpha6)) attack = 6;
            else if (Input.GetKeyDown(KeyCode.Alpha7)) attack = 7;
        }
        discreteActions[1] = attack;

        // 3. Phím giữ nhịp (Branch 2): Nếu giữ phím Right Shift -> Đánh trễ
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

    /// <summary>
    /// Khóa (Mask) các hành động không hợp lệ theo Cự ly thực tế và Cooldown.
    /// Triệt tiêu 100% hiện tượng tung chiêu cận chiến (như Chiêu 6) khi Player ở xa,
    /// buộc mạng nơ-ron phải di chuyển áp sát mới được mở khóa chiêu!
    /// </summary>
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (controller == null || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Branch 1: Kỹ năng chiến đấu (0=Không đánh, 1..7 tương ứng Chiêu 1..7)
        for (int skillIdx = 0; skillIdx < 7; skillIdx++)
        {
            int actionIndex = skillIdx + 1; // 1..7

            // 1. Kiểm tra Cooldown & Global Cooldown 3s giữa các chiêu lớn
            if (!controller.CanCastAttack(skillIdx))
            {
                actionMask.SetActionEnabled(1, actionIndex, false);
                continue;
            }

            // 2. Kiểm tra Cự ly hiệu dụng (Effective Range Masking)
            bool isRangeValid = true;
            switch (skillIdx)
            {
                case 0: // Chiêu 1 (Attack 1): Đấm thẳng cận chiến (tối đa 3.5m)
                    if (dist > 3.5f) isRangeValid = false;
                    break;

                case 1: // Chiêu 2 (GumoGumo): Đấm đất liên hoàn (tối đa 5.5m)
                    if (dist > 5.5f) isRangeValid = false;
                    break;

                case 2: // Chiêu 3 (FlameBreath): Phun lửa nón (tầm 3.2m - 14.5m)
                    if (dist < 3.0f || dist > 14.5f) isRangeValid = false;
                    break;

                case 3: // Chiêu 4 (JumpAttack): Nhảy vồ áp sát (tầm 5.5m - 18.0m)
                    if (dist < 5.0f || dist > 18.0f) isRangeValid = false;
                    break;

                case 4: // Chiêu 5 (RunAttack): Chạy lao húc tới (tầm 4.0m - 12.0m)
                    if (dist < 3.8f || dist > 12.0f) isRangeValid = false;
                    break;

                case 5: // Chiêu 6 (AttackLT): Combo 3 nhát chém cận chiến (tối đa 4.0m)
                    if (dist > 4.0f) isRangeValid = false;
                    break;

                case 6: // Chiêu 7 (IceBall): Ném cầu tím tầm xa (tầm 5.5m - 22.0m)
                    if (dist < 5.0f || dist > 22.0f) isRangeValid = false;
                    break;
            }

            if (!isRangeValid)
            {
                actionMask.SetActionEnabled(1, actionIndex, false);
            }
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
}
