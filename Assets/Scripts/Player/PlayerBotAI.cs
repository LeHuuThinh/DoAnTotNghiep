using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bot AI tự động điều khiển Player chiến đấu với Boss (Sparring Partner cho ML-Agents).
/// - Có công tắc phím nóng B (Toggle Bot Mode) để bật/tắt tức thì.
/// - TUYỆT ĐỐI KHÔNG SỬA CODE GỐC CỦA PLAYER: Sử dụng Reflection an toàn để kích hoạt
///   các kỹ năng chém (Combo 1-2-3), Parry, và Skill E/Q của Player.
/// - Tự xử lý Lăn né (Roll) với I-Frames 0.35s chuẩn Souls-like, đảm bảo tắt bất tử đúng hạn
///   để Boss có thể đánh trúng và Roll-Catch trừng phạt.
/// - Khi tắt (botEnabled = false), Bot hoàn toàn ngủ đông, trả lại 100% quyền điều khiển
///   bàn phím/chuột cho người chơi.
/// </summary>
public class PlayerBotAI : MonoBehaviour
{
    public enum BotState
    {
        Idle,           // Thăm dò / đứng giữ cự ly
        Approaching,    // Tiến áp sát Boss
        Circling,       // Đi vòng quanh Boss (Strafing / Spacing)
        Attacking,      // Tấn công cận chiến (Combo 1-2-3) hoặc Skill
        Retreating,     // Lùi lại giữ cự ly an toàn & hồi phục Stamina
        Dodging,        // Lăn né (Roll) khi Boss vung đòn
        Healing         // Lùi xa hồi phục máu (kích hoạt thử thách Heal-Punish cho Boss)
    }

    [Header("=== TRẠNG THÁI BOT ===")]
    [Tooltip("Bật/Tắt Bot tự động. Có thể bật/tắt bằng phím B khi đang chơi")]
    [SerializeField] private bool botEnabled = false;
    [SerializeField] private BotState currentState = BotState.Idle;

    [Header("=== CÀI ĐẶT CỰ LY CHIẾN ĐẤU (METERS) ===")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private float preferredMinDist = 3.2f;
    [SerializeField] private float preferredMaxDist = 5.5f;
    [SerializeField] private float retreatDistance = 4.5f;

    [Header("=== CÀI ĐẶT LĂN NÉ (ROLL SETTINGS) ===")]
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float rollIFrameDuration = 0.35f;
    [SerializeField] private float rollSpeed = 8.5f;

    [Header("=== THAM CHIẾU ===")]
    [SerializeField] private Transform bossTransform;
    [SerializeField] private IceGolemController bossController;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    // Reflection Method Infos (gọi hàm private không cần sửa script gốc)
    private MethodInfo normalAttackMethod;
    private MethodInfo advanceComboMethod;
    private MethodInfo chargeAttackMethod;
    private MethodInfo parryMethod;
    private MethodInfo skillEMethod;
    private MethodInfo skillQMethod;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int StrafeXHash = Animator.StringToHash("StrafeX");
    private static readonly int StrafeZHash = Animator.StringToHash("StrafeZ");
    private static readonly int RollHash = Animator.StringToHash("Roll");

    // Timers & Trạng thái nội bộ
    private float stateTimer = 0f;
    private float strafeDirection = 1f; // 1 = phải, -1 = trái
    private float nextActionDelay = 0f;
    private float verticalVelocity = 0f;
    private bool isComboing = false;
    private int currentComboStep = 0;
    private float rollCooldown = 0f;
    private float healCooldown = 0f;
    private bool wasBossBusy = false;
    private bool isBotRolling = false;
    private Coroutine rollRoutine = null;
    private GUIStyle cachedBoxStyle = null;

    public bool IsBotEnabled => botEnabled;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerHealth = GetComponent<Health>();
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat == null) playerCombat = GetComponentInChildren<PlayerCombat>();
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();

        // Cache Reflection Methods an toàn
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        if (playerCombat != null)
        {
            var combatType = playerCombat.GetType();
            normalAttackMethod = combatType.GetMethod("ExecuteNormalAttack", flags);
            advanceComboMethod = combatType.GetMethod("AdvanceCombo", flags);
            chargeAttackMethod = combatType.GetMethod("ExecuteChargeAttack", flags);
            parryMethod = combatType.GetMethod("ExecuteParry", flags);
            skillEMethod = combatType.GetMethod("ExecuteSkillE", flags);
            skillQMethod = combatType.GetMethod("ExecuteSkillQ", flags);
        }

        // Đảm bảo không bị kẹt bất tử
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }
    }

    private void Start()
    {
        FindBossReferences();
        ApplyBotState(botEnabled);
    }

    private void FindBossReferences()
    {
        if (bossTransform == null)
        {
            var golem = FindFirstObjectByType<IceGolemController>();
            if (golem != null)
            {
                bossController = golem;
                bossTransform = golem.transform;
            }
            else
            {
                GameObject bossObj = GameObject.Find("Boss_IceGolem");
                if (bossObj != null)
                {
                    bossTransform = bossObj.transform;
                    bossController = bossObj.GetComponent<IceGolemController>();
                }
            }
        }
    }

    private void Update()
    {
        // 1. Phím tắt B để Bật / Tắt Bot Mode tức thì
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBot(!botEnabled);
        }

        // Nếu Bot đang tắt -> Ngủ đông hoàn toàn, nhường 100% quyền cho người chơi
        if (!botEnabled) return;

        if (bossTransform == null || !bossTransform.gameObject.activeInHierarchy)
        {
            FindBossReferences();
            return;
        }

        // Nếu Player chết -> Dừng FSM
        if (playerHealth != null && playerHealth.IsDead) return;

        // Nếu Boss đã chết -> Dừng chém để chuẩn bị hiệp mới
        if ((bossController != null && bossController.IsDead) || (bossTransform.GetComponent<Health>() != null && bossTransform.GetComponent<Health>().IsDead))
        {
            SetSpeedAnimation(0f);
            return;
        }

        // Giảm cooldowns
        if (rollCooldown > 0f) rollCooldown -= Time.deltaTime;
        if (healCooldown > 0f) healCooldown -= Time.deltaTime;
        if (nextActionDelay > 0f) nextActionDelay -= Time.deltaTime;
        stateTimer += Time.deltaTime;

        // Nếu đang trong hoạt ảnh lăn né BotRollRoutine -> Không can thiệp di chuyển đè lên
        if (isBotRolling)
        {
            return;
        }

        float distToBoss = Vector3.Distance(transform.position, bossTransform.position);

        // 2. Tự động xoay mặt nhìn thẳng vào Boss
        Vector3 dirToBoss = (bossTransform.position - transform.position);
        dirToBoss.y = 0f;
        if (dirToBoss.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToBoss.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }

        // 3. Phản xạ Né đòn (Roll / Parry) khi Boss bắt đầu vung chiêu
        CheckBossAttackAndDodge(distToBoss);

        // 4. Máy trạng thái chiến đấu (FSM)
        UpdateFSM(distToBoss);

        // 5. Áp dụng trọng lực giữ chân Player bám sàn
        ApplyGravity();
    }

    public void ToggleBot(bool enable)
    {
        botEnabled = enable;
        ApplyBotState(botEnabled);

        if (botEnabled)
        {
            ResetBot();
            Debug.Log("<color=green>[Player Bot AI]</color> 🟢 ĐÃ BẬT BOT TỰ ĐỘNG! (Nhấn phím B để tắt)");
        }
        else
        {
            if (playerHealth != null) playerHealth.SetInvulnerable(false);
            Debug.Log("<color=yellow>[Player Bot AI]</color> 🟡 ĐÃ TẮT BOT! Trả lại quyền điều khiển thủ công.");
        }
    }

    public void ResetBot()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }
        isBotRolling = false;
        isComboing = false;
        rollCooldown = 0f;
        healCooldown = 0f;
        nextActionDelay = 0f;
        stateTimer = 0f;
        currentState = BotState.Approaching;

        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }
    }

    private void ApplyBotState(bool isBotOn)
    {
        // Khi Bot chạy: tạm ngắt script đọc phím gốc để không bị nhiễu phím
        if (playerController != null)
        {
            playerController.enabled = !isBotOn;
            if (!isBotOn) playerController.UnlockMovement();
        }

        // Đảm bảo không bị kẹt bất tử
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }
    }

    private void CheckBossAttackAndDodge(float distToBoss)
    {
        if (bossController == null || isBotRolling) return;

        bool bossIsBusy = bossController.IsBusy();
        bool bossJustAttacked = !wasBossBusy && bossIsBusy;
        wasBossBusy = bossIsBusy;

        // Nếu Boss vừa bắt đầu tung chiêu và Player đang ở cự ly nguy hiểm (< 12m)
        if (bossJustAttacked && distToBoss < 12f && rollCooldown <= 0f)
        {
            if (playerStats != null && playerStats.HasStamina(20f))
            {
                float rand = Random.value;
                // 70% Tỉ lệ: Lăn né sang bên hoặc lùi lại
                if (rand < 0.70f)
                {
                    Vector3 dodgeDir = (Random.value > 0.5f) ? -transform.right : transform.right;
                    if (Random.value > 0.6f) dodgeDir = -transform.forward; // Lăn lùi
                    TriggerBotRoll(dodgeDir);
                    rollCooldown = Random.Range(1.5f, 2.8f);
                    return;
                }
                // 15% Tỉ lệ: Giơ kiếm Parry nếu đứng gần (< 4m)
                else if (rand < 0.85f && distToBoss < 4.0f)
                {
                    TriggerBotParry();
                    rollCooldown = 2.0f;
                    return;
                }
                // 15% Tỉ lệ: Cố tình không né hoặc lăn chậm (tạo cơ hội cho Boss bắt bài Roll-Catch)
            }
        }
    }

    private void UpdateFSM(float distToBoss)
    {
        // Kiểm tra cơ hội hồi máu (Heal-Punish test)
        if (playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth * 0.45f && healCooldown <= 0f)
        {
            currentState = BotState.Healing;
        }

        switch (currentState)
        {
            case BotState.Idle:
                SetSpeedAnimation(0f);
                if (stateTimer > 0.4f)
                {
                    currentState = (distToBoss > attackRange) ? BotState.Approaching : BotState.Attacking;
                    stateTimer = 0f;
                }
                break;

            case BotState.Approaching:
                if (distToBoss <= attackRange)
                {
                    // Đã vào tầm kiếm -> Lập tức vung kiếm chém combo!
                    currentState = BotState.Attacking;
                    stateTimer = 0f;
                }
                else
                {
                    // Di chuyển dứt khoát áp sát vào tầm chém của Boss
                    float approachSpeed = (distToBoss > 8.0f) ? 4.8f : 3.8f;
                    float animSpeed = (distToBoss > 8.0f) ? 1.0f : 0.7f;
                    MoveInDirection(transform.forward, approachSpeed);
                    SetSpeedAnimation(animSpeed);

                    // Thỉnh thoảng bắn Kiếm khí Skill E tầm xa
                    if (distToBoss > 4.5f && distToBoss < 9f && Random.value < 0.02f)
                    {
                        TriggerBotSkillE();
                    }
                }
                break;

            case BotState.Circling:
                // Vờn quanh Boss (Strafing / Spacing)
                Vector3 strafeVec = (transform.right * strafeDirection).normalized;
                // Giữ cự ly 3.2m - 5.0m
                if (distToBoss < preferredMinDist) strafeVec += -transform.forward * 0.5f;
                else if (distToBoss > preferredMaxDist) strafeVec += transform.forward * 0.5f;

                MoveInDirection(strafeVec.normalized, 2.8f);
                SetSpeedAnimation(0.5f);

                // Thời gian vờn quanh hợp lý (1.5s - 2.8s)
                if (stateTimer > Random.Range(1.5f, 2.8f))
                {
                    strafeDirection *= -1f; // Đổi hướng lượn dạt
                    // 80% Tỉ lệ: Mở đợt tấn công áp sát chém liên hoàn!
                    // 20% Tỉ lệ: Lùi lại hồi phục thêm
                    currentState = (Random.value < 0.80f) ? BotState.Approaching : BotState.Retreating;
                    stateTimer = 0f;
                }
                break;

            case BotState.Attacking:
                SetSpeedAnimation(0f);
                if (!isComboing && nextActionDelay <= 0f)
                {
                    StartCoroutine(AttackComboRoutine());
                }
                break;

            case BotState.Retreating:
                // Lùi lại giữ cự ly an toàn để hồi phục Stamina
                MoveInDirection(-transform.forward, 3.4f);
                SetSpeedAnimation(0.5f);

                if (distToBoss >= retreatDistance || stateTimer > 1.0f)
                {
                    currentState = BotState.Circling;
                    stateTimer = 0f;
                }
                break;

            case BotState.Healing:
                // Lùi xa để uống máu
                if (distToBoss < 8.0f)
                {
                    MoveInDirection(-transform.forward, 4.5f);
                    SetSpeedAnimation(0.8f);
                }
                else
                {
                    SetSpeedAnimation(0f);
                    if (stateTimer > 0.6f)
                    {
                        // Hồi 35 HP (Kích thích Boss tung chiêu Heal-Punish)
                        if (playerHealth != null)
                        {
                            playerHealth.Heal(35f);
                            Debug.Log("<color=cyan>[Player Bot]</color> 🧪 Player vừa uống máu! (Thử thách Heal-Punish cho Boss)");
                        }
                        healCooldown = 15f; // Không spam hồi máu liên tục
                        currentState = BotState.Circling;
                        stateTimer = 0f;
                    }
                }
                break;
        }
    }

    private IEnumerator AttackComboRoutine()
    {
        isComboing = true;
        currentComboStep = 1;

        // Đòn 1
        TriggerBotAttack();
        yield return new WaitForSeconds(0.45f);

        // Đòn 2 (85% tỉ lệ nối tiếp)
        if (Random.value < 0.85f && playerStats != null && playerStats.HasStamina(15f))
        {
            currentComboStep = 2;
            TriggerBotAdvanceCombo();
            yield return new WaitForSeconds(0.45f);

            // Đòn 3 kết thúc (70% tỉ lệ)
            if (Random.value < 0.70f && playerStats.HasStamina(15f))
            {
                currentComboStep = 3;
                TriggerBotAdvanceCombo();
                yield return new WaitForSeconds(0.55f);
            }
        }

        isComboing = false;
        nextActionDelay = Random.Range(0.25f, 0.5f);
        currentState = BotState.Retreating; // Đánh xong lùi lại hồi thể lực
        stateTimer = 0f;
    }

    private void MoveInDirection(Vector3 direction, float speed)
    {
        if (characterController == null || !characterController.enabled) return;
        Vector3 moveVel = direction.normalized * speed;
        characterController.Move(moveVel * Time.deltaTime);
    }

    private void SetSpeedAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (characterController == null || !characterController.enabled) return;

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * 2.5f * Time.deltaTime;
        }

        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    // ==========================================
    // KÍCH HOẠT HÀNH ĐỘNG QUA REFLECTION AN TOÀN
    // ==========================================

    private void TriggerBotAttack()
    {
        if (playerCombat != null && normalAttackMethod != null)
        {
            normalAttackMethod.Invoke(playerCombat, null);
        }
    }

    private void TriggerBotAdvanceCombo()
    {
        if (playerCombat != null && advanceComboMethod != null)
        {
            advanceComboMethod.Invoke(playerCombat, null);
        }
    }

    private void TriggerBotParry()
    {
        if (playerCombat != null && parryMethod != null)
        {
            parryMethod.Invoke(playerCombat, null);
        }
    }

    private void TriggerBotSkillE()
    {
        if (playerCombat != null && skillEMethod != null && playerStats != null && playerStats.HasSpirit(50f))
        {
            skillEMethod.Invoke(playerCombat, null);
        }
    }

    private void TriggerBotRoll(Vector3 rollDir)
    {
        if (isBotRolling) return;
        if (rollRoutine != null) StopCoroutine(rollRoutine);
        rollRoutine = StartCoroutine(BotRollRoutine(rollDir));
    }

    private IEnumerator BotRollRoutine(Vector3 rollDir)
    {
        isBotRolling = true;

        // 1. Tiêu hao thể lực Stamina né đòn
        if (playerStats != null)
        {
            playerStats.ConsumeStamina(20f);
        }

        // 2. Kích hoạt hoạt ảnh lăn né (Roll)
        if (animator != null)
        {
            animator.SetTrigger(RollHash);
        }

        // 3. Kích hoạt I-Frames bất tử
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(true);
        }

        Vector3 moveDir = rollDir.sqrMagnitude > 0.01f ? rollDir.normalized : transform.forward;
        float elapsed = 0f;
        bool iFrameActive = true;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            // Hết 0.35s I-Frame -> Tắt bất tử ngay lập tức để Boss có thể Roll-Catch trừng phạt!
            if (iFrameActive && elapsed >= rollIFrameDuration)
            {
                iFrameActive = false;
                if (playerHealth != null)
                {
                    playerHealth.SetInvulnerable(false);
                }
            }

            // Lướt nhân vật theo hướng né đòn
            if (characterController != null && characterController.enabled)
            {
                Vector3 move = moveDir * rollSpeed + new Vector3(0f, verticalVelocity, 0f);
                characterController.Move(move * Time.deltaTime);
            }

            yield return null;
        }

        // Đảm bảo 100% sau khi hoàn tất lăn, bất tử luôn được tắt
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        isBotRolling = false;
        rollRoutine = null;
    }

    // ==========================================
    // GIAO DIỆN HIỂN THỊ (ON-SCREEN HUD OVERLAY)
    // ==========================================

    private void OnGUI()
    {
        // Hiển thị Banner trạng thái Bot ở góc trên bên trái (Cache GUIStyle để chống tràn Windows Handles)
        if (cachedBoxStyle == null)
        {
            cachedBoxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        if (botEnabled)
        {
            GUI.color = new Color(0.2f, 1f, 0.4f, 0.95f);
            GUI.Box(new Rect(20, 20, 420, 55), 
                $"🤖 [BOT PLAYER ĐANG TỰ ĐÁNH (SPARRING)]\n" +
                $"Trạng thái: {currentState} | [B]: TẮT BOT | [T]: Tốc Độ ({Time.timeScale:0.#}x)", cachedBoxStyle);
        }
        else
        {
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Box(new Rect(20, 20, 380, 36), 
                $"🎮 Thủ công | [B]: Bật Bot | [T]: Tốc Độ ({Time.timeScale:0.#}x)", cachedBoxStyle);
        }

        GUI.color = Color.white;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.UnlockMovement();
        }
    }
}
