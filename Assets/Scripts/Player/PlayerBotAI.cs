using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bot AI tự động điều khiển Player chiến đấu với Boss (Sparring Partner cho ML-Agents).
/// - Có công tắc phím nóng B (Toggle Bot Mode) để bật/tắt tức thì.
/// - TUYỆT ĐỐI KHÔNG SỬA CODE GỐC CỦA PLAYER: Sử dụng Reflection an toàn để kích hoạt
///   các kỹ năng chém (Combo 1-2-3), Charge Attack, Parry, Counter Attack, và Skill E/Q của Player.
/// - Đồng bộ hóa isRolling với PlayerController qua Reflection để IceGolemAgent (ML-Agents)
///   nhận diện chính xác I-Frames và cửa sổ Roll-Catching (0.4s), phục vụ train thưởng/phạt.
/// - Tự động phát hiện cả Boss Ice Golem và Boss Red Ignis.
/// - Tích hợp cửa sổ sơ hở hồi máu (Heal-Punish window 1.2s) để Boss học cách trừng phạt khi Player uống máu.
/// - Tận dụng tối đa trạng thái Boss Suy Yếu (Weaken / Poise Break) để dồn sát thương trừng phạt.
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
        Healing,        // Lùi xa hồi phục máu (kích hoạt thử thách Heal-Punish cho Boss)
        Staggered,      // Bị trúng đòn khựng người
        PunishingWeaken // Boss bị vỡ thế / Weaken -> Lao vào xả chiêu dồn sát thương
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
    [SerializeField] private RedIgnisController ignisController;
    [SerializeField] private Health bossHealth;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerFeedback playerFeedback;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    // Reflection Method Infos (gọi hàm private không cần sửa script gốc)
    private MethodInfo normalAttackMethod;
    private MethodInfo advanceComboMethod;
    private MethodInfo chargeAttackMethod;
    private MethodInfo parryMethod;
    private MethodInfo counterAttackMethod;
    private MethodInfo skillEMethod;
    private MethodInfo skillQMethod;

    // Reflection Field Infos (đồng bộ trạng thái nội bộ an toàn)
    private FieldInfo comboStepField;
    private FieldInfo canCounterAttackField;
    private FieldInfo isRollingField;
    private FieldInfo skillETimerField;
    private FieldInfo skillQTimerField;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
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
    private bool isDrinkingPotion = false;
    private Coroutine rollRoutine = null;
    private Coroutine attackRoutine = null;
    private Coroutine healRoutine = null;
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
        playerFeedback = GetComponent<PlayerFeedback>();
        if (playerFeedback == null) playerFeedback = GetComponentInChildren<PlayerFeedback>();
        animator = GetComponentInChildren<Animator>();

        // Cache Reflection Methods & Fields trên PlayerCombat an toàn
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        if (playerCombat != null)
        {
            var combatType = playerCombat.GetType();
            normalAttackMethod = combatType.GetMethod("ExecuteNormalAttack", flags);
            advanceComboMethod = combatType.GetMethod("AdvanceCombo", flags);
            chargeAttackMethod = combatType.GetMethod("ExecuteChargeAttack", flags);
            parryMethod = combatType.GetMethod("ExecuteParry", flags);
            counterAttackMethod = combatType.GetMethod("ExecuteCounterAttack", flags);
            skillEMethod = combatType.GetMethod("ExecuteSkillE", flags);
            skillQMethod = combatType.GetMethod("ExecuteSkillQ", flags);

            comboStepField = combatType.GetField("comboStep", flags);
            canCounterAttackField = combatType.GetField("canCounterAttack", flags);
            skillETimerField = combatType.GetField("skillETimer", flags);
            skillQTimerField = combatType.GetField("skillQTimer", flags);
        }

        // Cache Reflection Field isRolling trên PlayerController để đồng bộ với ML-Agents (IceGolemAgent)
        if (playerController != null)
        {
            isRollingField = typeof(PlayerController).GetField("isRolling", flags);
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
        if (bossTransform == null || !bossTransform.gameObject.activeInHierarchy)
        {
            // 1. Tìm Ice Golem
            var golem = FindFirstObjectByType<IceGolemController>();
            if (golem != null)
            {
                bossController = golem;
                bossTransform = golem.transform;
                bossHealth = golem.GetComponent<Health>();
                return;
            }

            // 2. Tìm Red Ignis
            var ignis = FindFirstObjectByType<RedIgnisController>();
            if (ignis != null)
            {
                ignisController = ignis;
                bossTransform = ignis.transform;
                bossHealth = ignis.GetComponent<Health>();
                return;
            }

            // 3. Fallback tìm theo Tag Enemy (Quái và Boss đều dùng chung Tag "Enemy")
            GameObject enemyObj = null;
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            float minDistance = float.MaxValue;
            foreach (var e in allEnemies)
            {
                if (e == null || !e.activeInHierarchy) continue;
                Health h = e.GetComponent<Health>();
                if (h != null && h.IsDead) continue;

                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    enemyObj = e;
                }
            }

            // 4. Fallback tìm theo Tên GameObject nếu chưa kịp gán Tag Enemy
            if (enemyObj == null) enemyObj = GameObject.Find("Boss_DarkNight");
            if (enemyObj == null) enemyObj = GameObject.Find("Boss_RedIgnis");
            if (enemyObj == null) enemyObj = GameObject.Find("Boss_IceGolem");

            if (enemyObj != null)
            {
                bossTransform = enemyObj.transform;
                bossController = enemyObj.GetComponent<IceGolemController>();
                ignisController = enemyObj.GetComponent<RedIgnisController>();
                bossHealth = enemyObj.GetComponent<Health>();
            }
        }
    }

    private bool IsBossBusy()
    {
        if (bossController != null) return bossController.IsBusy();
        if (ignisController != null) return ignisController.IsBusy();
        return false;
    }

    private bool IsBossDead()
    {
        if (bossController != null && bossController.IsDead) return true;
        if (ignisController != null && ignisController.IsDead) return true;
        if (bossHealth != null && bossHealth.IsDead) return true;
        return false;
    }

    private bool IsBossWeakened()
    {
        if (bossController != null && bossController.IsWeakened) return true;
        if (ignisController != null && ignisController.IsWeakened) return true;
        return false;
    }

    private void Update()
    {
        // 1. Phím tắt B để Bật / Tắt Bot Mode tức thì
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBot(!botEnabled);
        }

        // Điều khiển tốc độ game bằng phím T (1x -> 2x -> 5x)
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (Time.timeScale <= 1.05f) Time.timeScale = 2.0f;
            else if (Time.timeScale <= 2.05f) Time.timeScale = 5.0f;
            else Time.timeScale = 1.0f;
            Debug.Log($"<color=yellow>[Player Bot AI]</color> ⏩ Tốc độ game Time.timeScale = {Time.timeScale:0.#}x (Phím T)");
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
        if (IsBossDead())
        {
            SetSpeedAnimation(0f);
            return;
        }

        // Giảm cooldowns
        if (rollCooldown > 0f) rollCooldown -= Time.deltaTime;
        if (healCooldown > 0f) healCooldown -= Time.deltaTime;
        if (nextActionDelay > 0f) nextActionDelay -= Time.deltaTime;
        stateTimer += Time.deltaTime;

        // 2. Nếu Player đang bị Choáng / Vỡ thế (Staggered) bởi đòn đánh Boss -> Nhường quyền vật lý cho PlayerFeedback
        if (playerFeedback != null && playerFeedback.IsStaggered)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isComboing = false;
            currentState = BotState.Staggered;
            return;
        }
        else if (currentState == BotState.Staggered)
        {
            currentState = BotState.Circling;
            stateTimer = 0f;
        }

        // 3. Nếu đang trong hoạt ảnh lăn né BotRollRoutine -> Không can thiệp di chuyển đè lên
        if (isBotRolling)
        {
            return;
        }

        // 3.5. Kiểm tra nếu vừa Parry thành công và có cơ hội Counter Attack -> Phản kích ngay!
        if (canCounterAttackField != null && playerCombat != null && (bool)canCounterAttackField.GetValue(playerCombat))
        {
            TriggerBotCounter();
            return;
        }

        float distToBoss = Vector3.Distance(transform.position, bossTransform.position);

        // 4. Tự động xoay mặt nhìn thẳng vào Boss
        Vector3 dirToBoss = (bossTransform.position - transform.position);
        dirToBoss.y = 0f;
        if (dirToBoss.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToBoss.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }

        // 5. Phản xạ Né đòn (Roll / Parry) khi Boss bắt đầu vung chiêu
        CheckBossAttackAndDodge(distToBoss);

        // 6. Kiểm tra trạng thái Suy yếu (Weaken / Poise Break) của Boss để dồn sát thương
        if (IsBossWeakened() && currentState != BotState.PunishingWeaken && !isDrinkingPotion)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isComboing = false;
            currentState = BotState.PunishingWeaken;
            stateTimer = 0f;
        }

        // 7. Máy trạng thái chiến đấu (FSM)
        UpdateFSM(distToBoss);

        // 8. Áp dụng trọng lực giữ chân Player bám sàn
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
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }

        isBotRolling = false;
        isComboing = false;
        isDrinkingPotion = false;
        rollCooldown = 0f;
        healCooldown = 0f;
        nextActionDelay = 0f;
        stateTimer = 0f;
        currentState = BotState.Approaching;

        if (playerCombat != null)
        {
            playerCombat.ForceResetCombat();
        }

        if (isRollingField != null && playerController != null)
        {
            isRollingField.SetValue(playerController, false);
        }

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
            if (!isBotOn)
            {
                playerController.UnlockMovement();
                if (isRollingField != null) isRollingField.SetValue(playerController, false);
            }
        }

        // Đảm bảo không bị kẹt bất tử
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        if (!isBotOn && playerCombat != null)
        {
            playerCombat.ForceResetCombat();
        }
    }

    private void CheckBossAttackAndDodge(float distToBoss)
    {
        if (isBotRolling || isDrinkingPotion) return;

        bool bossIsBusy = IsBossBusy();
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
                // 15% Tỉ lệ: Giơ kiếm Parry nếu đứng gần (< 4.5m)
                else if (rand < 0.85f && distToBoss < 4.5f)
                {
                    TriggerBotParry();
                    rollCooldown = 2.0f;
                    return;
                }
                // 15% Tỉ lệ: Cố tình không né hoặc lăn chậm (tạo cơ hội cho Boss bắt bài Roll-Catch / trúng đòn)
            }
        }
    }

    private void UpdateFSM(float distToBoss)
    {
        // Kiểm tra cơ hội hồi máu (Heal-Punish test khi máu thấp)
        if (playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth * 0.45f 
            && healCooldown <= 0f && currentState != BotState.Healing && !isDrinkingPotion && !isBotRolling)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isComboing = false;
            currentState = BotState.Healing;
            stateTimer = 0f;
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
                    if (distToBoss > 4.5f && distToBoss < 10f && Random.value < 0.03f)
                    {
                        TriggerBotSkillE();
                    }
                }
                break;

            case BotState.Circling:
                // Vờn quanh Boss (Strafing / Spacing)
                Vector3 strafeVec = (transform.right * strafeDirection).normalized;
                // Giữ cự ly 3.2m - 5.5m
                if (distToBoss < preferredMinDist) strafeVec += -transform.forward * 0.6f;
                else if (distToBoss > preferredMaxDist) strafeVec += transform.forward * 0.6f;

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
                if (!isComboing && attackRoutine == null && nextActionDelay <= 0f)
                {
                    attackRoutine = StartCoroutine(AttackComboRoutine());
                }
                break;

            case BotState.Retreating:
                // Lùi lại giữ cự ly an toàn để hồi phục Stamina
                MoveInDirection(-transform.forward, 3.4f);
                SetSpeedAnimation(0.5f);

                if (distToBoss >= retreatDistance || stateTimer > 1.2f)
                {
                    currentState = BotState.Circling;
                    stateTimer = 0f;
                }
                break;

            case BotState.Healing:
                // Lùi xa để uống máu
                if (distToBoss < 8.0f && !isDrinkingPotion)
                {
                    MoveInDirection(-transform.forward, 4.5f);
                    SetSpeedAnimation(0.8f);
                }
                else
                {
                    SetSpeedAnimation(0f);
                    if (!isDrinkingPotion && healRoutine == null)
                    {
                        healRoutine = StartCoroutine(DrinkPotionRoutine());
                    }
                }
                break;

            case BotState.PunishingWeaken:
                // Boss bị Suy Yếu (Weaken / Poise Break): Lao thẳng vào dồn sát thương cực đại!
                if (distToBoss > attackRange)
                {
                    MoveInDirection(transform.forward, 5.2f);
                    SetSpeedAnimation(1.0f);
                }
                else
                {
                    SetSpeedAnimation(0f);
                    if (!isComboing && attackRoutine == null)
                    {
                        attackRoutine = StartCoroutine(PunishWeakenRoutine());
                    }
                }

                if (!IsBossWeakened())
                {
                    currentState = BotState.Circling;
                    stateTimer = 0f;
                }
                break;

            case BotState.Staggered:
                // Đang bị Stagger: Đứng yên, đợi PlayerFeedback hoàn tất
                SetSpeedAnimation(0f);
                break;
        }
    }

    private IEnumerator AttackComboRoutine()
    {
        isComboing = true;
        currentComboStep = 1;

        // Đòn 1 (bắt buộc set comboStep = 1 trên PlayerCombat để Animator nhận diện)
        TriggerBotAttack();
        yield return new WaitForSeconds(0.45f);

        // Đòn 2 (85% tỉ lệ nối tiếp)
        if (Random.value < 0.85f && playerStats != null && playerStats.HasStamina(playerStats.LightAttackCost))
        {
            currentComboStep = 2;
            TriggerBotAdvanceCombo();
            yield return new WaitForSeconds(0.45f);

            // Đòn 3 kết thúc (70% tỉ lệ)
            if (Random.value < 0.70f && playerStats.HasStamina(playerStats.LightAttackCost))
            {
                currentComboStep = 3;
                TriggerBotAdvanceCombo();
                yield return new WaitForSeconds(0.65f);
            }
        }

        // Đôi khi kết thúc combo bằng Skill Q nếu nhiều Spirit
        if (playerStats != null && playerStats.HasSpirit(playerStats.SkillQCost) && Random.value < 0.35f)
        {
            TriggerBotSkillQ();
            yield return new WaitForSeconds(0.85f);
        }

        isComboing = false;
        attackRoutine = null;
        nextActionDelay = Random.Range(0.25f, 0.5f);
        currentState = BotState.Retreating; // Đánh xong lùi lại hồi thể lực
        stateTimer = 0f;
    }

    private IEnumerator PunishWeakenRoutine()
    {
        isComboing = true;

        // 1. Nếu có đủ 100 Spirit: Kích nổ Skill Q sát thương khổng lồ vào Boss đang suy yếu!
        if (playerStats != null && playerStats.HasSpirit(playerStats.SkillQCost))
        {
            TriggerBotSkillQ();
            yield return new WaitForSeconds(0.9f);
        }
        else
        {
            // 2. Chém trọn combo 3 đòn liên hoàn
            TriggerBotAttack();
            yield return new WaitForSeconds(0.45f);

            if (playerStats != null && playerStats.HasStamina(playerStats.LightAttackCost))
            {
                TriggerBotAdvanceCombo();
                yield return new WaitForSeconds(0.45f);

                if (playerStats.HasStamina(playerStats.LightAttackCost))
                {
                    TriggerBotAdvanceCombo();
                    yield return new WaitForSeconds(0.65f);
                }
            }
        }

        isComboing = false;
        attackRoutine = null;
        currentState = BotState.Circling;
        stateTimer = 0f;
    }

    private IEnumerator DrinkPotionRoutine()
    {
        isDrinkingPotion = true;
        SetSpeedAnimation(0f);

        // Hồi 35 HP (Kích thích Boss tung chiêu Heal-Punish)
        if (playerHealth != null)
        {
            playerHealth.Heal(35f);
            Debug.Log("<color=cyan>[Player Bot AI]</color> 🧪 Player vừa uống máu! (Kích hoạt cửa sổ Heal-Punish cho Boss)");
        }

        // Cửa sổ sơ hở (Heal Vulnerability Window - 1.2s):
        // Trong thời gian này, Player Bot đứng yên và KHÔNG ĐƯỢC PHÉP né đòn hay chạy trốn,
        // cho phép Boss AI (IceGolemAgent) tung chiêu JumpAttack/RunAttack trừng phạt và nhận điểm thưởng +2.0!
        float timer = 0f;
        while (timer < 1.2f)
        {
            timer += Time.deltaTime;
            // Nếu bị trúng đòn ngắt quãng (stagger) hoặc chết thì kết thúc sớm
            if ((playerFeedback != null && playerFeedback.IsStaggered) || (playerHealth != null && playerHealth.IsDead))
            {
                break;
            }
            yield return null;
        }

        healCooldown = 15f; // Không spam hồi máu liên tục
        isDrinkingPotion = false;
        healRoutine = null;
        currentState = BotState.Circling;
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
            // BẮT BUỘC đặt comboStep = 1 trước khi gọi ExecuteNormalAttack để Animator chuyển sang Attack 1!
            if (comboStepField != null)
            {
                comboStepField.SetValue(playerCombat, 1);
            }
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

    private void TriggerBotCounter()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        isComboing = false;

        if (counterAttackMethod != null && playerCombat != null)
        {
            counterAttackMethod.Invoke(playerCombat, null);
            Debug.Log("<color=yellow>[Player Bot AI]</color> ⚡ COUNTER ATTACK phản kích thành công sau Parry!");
        }

        currentState = BotState.Retreating;
        stateTimer = 0f;
        nextActionDelay = 0.6f;
    }

    private void TriggerBotSkillE()
    {
        if (playerCombat != null && skillEMethod != null && playerStats != null && playerStats.HasSpirit(playerStats.SkillECost))
        {
            // Kiểm tra Cooldown
            if (skillETimerField != null && (float)skillETimerField.GetValue(playerCombat) > 0f) return;

            // Tiêu hao Spirit đúng chuẩn
            playerStats.ConsumeSpirit(playerStats.SkillECost);
            skillEMethod.Invoke(playerCombat, null);
            Debug.Log("<color=cyan>[Player Bot AI]</color> 🌀 Phóng kiếm khí tầm xa (Skill E)!");
        }
    }

    private void TriggerBotSkillQ()
    {
        if (playerCombat != null && skillQMethod != null && playerStats != null && playerStats.HasSpirit(playerStats.SkillQCost))
        {
            // Kiểm tra Cooldown
            if (skillQTimerField != null && (float)skillQTimerField.GetValue(playerCombat) > 0f) return;

            // Tiêu hao Spirit đúng chuẩn
            playerStats.ConsumeSpirit(playerStats.SkillQCost);
            skillQMethod.Invoke(playerCombat, null);
            Debug.Log("<color=magenta>[Player Bot AI]</color> 💥 Đập nổ AOE giáng trần (Skill Q)!");
        }
    }

    private void TriggerBotRoll(Vector3 rollDir)
    {
        if (isBotRolling) return;

        // Ngắt combo đang chạy nếu có
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        isComboing = false;

        if (rollRoutine != null) StopCoroutine(rollRoutine);
        rollRoutine = StartCoroutine(BotRollRoutine(rollDir));
    }

    private IEnumerator BotRollRoutine(Vector3 rollDir)
    {
        isBotRolling = true;

        // Báo cho PlayerController biết đang Rolling để ML-Agents (IceGolemAgent) nhận diện chính xác
        if (isRollingField != null && playerController != null)
        {
            isRollingField.SetValue(playerController, true);
        }

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

            // Trọng lực khi lăn
            if (characterController != null && characterController.enabled)
            {
                if (characterController.isGrounded)
                {
                    if (verticalVelocity < 0f) verticalVelocity = -2f;
                }
                else
                {
                    verticalVelocity += Physics.gravity.y * 2.5f * Time.deltaTime;
                }

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

        // TẮT isRolling trên PlayerController để kích hoạt cửa sổ Roll-Catching (0.4s) trên IceGolemAgent!
        if (isRollingField != null && playerController != null)
        {
            isRollingField.SetValue(playerController, false);
        }

        isBotRolling = false;
        rollRoutine = null;
        currentState = BotState.Circling;
        stateTimer = 0f;
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
            GUI.Box(new Rect(20, 20, 440, 55), 
                $"🤖 [BOT PLAYER ĐANG TỰ ĐÁNH (SPARRING)]\n" +
                $"Trạng thái: {currentState} | [B]: TẮT BOT | [T]: Tốc Độ ({Time.timeScale:0.#}x)", cachedBoxStyle);
        }
        else
        {
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Box(new Rect(20, 20, 390, 36), 
                $"🎮 Thủ công | [B]: Bật Bot | [T]: Tốc Độ ({Time.timeScale:0.#}x)", cachedBoxStyle);
        }

        GUI.color = Color.white;
    }

    private void OnDisable()
    {
        if (rollRoutine != null) { StopCoroutine(rollRoutine); rollRoutine = null; }
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        if (healRoutine != null) { StopCoroutine(healRoutine); healRoutine = null; }

        if (isRollingField != null && playerController != null)
        {
            isRollingField.SetValue(playerController, false);
        }

        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.UnlockMovement();
        }

        if (playerCombat != null)
        {
            playerCombat.ForceResetCombat();
        }
    }
}
