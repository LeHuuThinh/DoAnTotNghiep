using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class RedIgnisController : MonoBehaviour
{
    public static event System.Action OnBossDefeated;

    [Header("=== BẢNG THÔNG SỐ BOSS (RED IGNIS) ===")]
    [Tooltip("Máu tối đa của Red Ignis (Mặc định: 1200)")]
    [SerializeField] private float bossMaxHealth = 1200f;
    [Tooltip("Chỉ số Giáp/Phòng thủ cơ bản của Red Ignis (Giảm sát thương % theo chuẩn ARPG: Damage * 100 / (100 + Def))")]
    [SerializeField] private float bossDefense = 25f;
    public float BossDefense => bossDefense;
    [Tooltip("Thanh sức bền (Poise) tối đa của Red Ignis (Mặc định: 400). Khi cạn về 0 sẽ gây suy yếu (Weaken/Stagger)")]
    [SerializeField] private float maxPoise = 400f;
    [Tooltip("Điểm Poise chịu đòn hiện tại")]
    [SerializeField] private float currentPoise = 400f;
    [Tooltip("Thời gian bị choáng / suy yếu khi hết poise (giây)")]
    [SerializeField] private float weakenDuration = 2.5f;

    [Header("=== EVENTS SỰ KIỆN BOSS (UI BARS) ===")]
    public UnityEvent<float, float> onPoiseChanged; // (currentPoise, maxPoise)
    public UnityEvent<float, float> onStaminaChanged; // Alias tương thích ngược
    public UnityEvent<float, float> onHealthChanged; // (currentHealth, maxHealth)

    [Header("=== DI CHUYỂN & LOCOMOTION ENGINE ===")]
    [Tooltip("Tốc độ đi bộ tới / lùi (m/s)")]
    [SerializeField] private float moveSpeed = 4.2f;
    [Tooltip("Tốc độ di chuyển ngang (strafe) trái / phải (m/s)")]
    [SerializeField] private float strafeSpeed = 3.2f;
    [Tooltip("Gia tốc tăng tốc khi bắt đầu di chuyển")]
    [SerializeField] private float moveAcceleration = 9.0f;
    [Tooltip("Gia tốc giảm tốc khi dừng")]
    [SerializeField] private float moveDeceleration = 12.0f;
    [Tooltip("Thời gian làm mượt thông số Animator (DampTime)")]
    [SerializeField] private float locomotionDampTime = 0.1f;
    [Tooltip("Luôn tự động xoay mặt hướng về phía Player khi rảnh rỗi hoặc di chuyển")]
    [SerializeField] private bool alwaysFacePlayer = true;
    [Tooltip("Tốc độ xoay mặt theo Player (độ/giây)")]
    [SerializeField] private float facePlayerSpeed = 280f;
    [Tooltip("Gia tốc trọng lực rơi (m/s²). Mặc định: -20")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Vận tốc ép bám sàn khi tiếp đất (m/s). Mặc định: -5")]
    [SerializeField] private float groundedGravity = -5f;
    private float verticalVelocity = -5f;

    [Header("=== MỤC TIÊU ĐỐI THỦ (PLAYER TARGET) ===")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("=== WEAPON & PROJECTILE SPAWN POINTS ===")]
    [Tooltip("Socket kiếm trên tay phải (Hand_R_Weapon)")]
    [SerializeField] private Transform weaponSocket;
    [Tooltip("Điểm xuất phát của đường kiếm bay (nếu để trống tự tạo phía trước ngực/kiếm)")]
    [SerializeField] private Transform slashSpawnPoint;

    [Space(10)]
    [Header("=== KỸ NĂNG 1: ATTACK SLASH (CHÉM DỌC - PHÍM 1) ===")]
    [Tooltip("Prefab hiệu ứng vung kiếm dọc trên tay (Ignis_Combo_Slash)")]
    [SerializeField] private GameObject attackSlashSwingVfxPrefab;
    [Tooltip("Prefab sóng kiếm bay dọc (Ignis_Slash_Vertical)")]
    [SerializeField] private GameObject verticalSlashPrefab;
    [Tooltip("Prefab hiệu ứng va chạm của sóng kiếm dọc (Ignis_Slash_Impact)")]
    [SerializeField] private GameObject attackSlashImpactVfxPrefab;
    [HideInInspector] [SerializeField] private GameObject slashImpactVfxPrefab; // Giữ để tương thích dữ liệu cũ
    [Tooltip("Sát thương sóng kiếm dọc")]
    [SerializeField] private float attackSlashDamage = 65f;
    [Tooltip("Thời gian hồi chiêu (giây)")]
    [SerializeField] private float attackSlashCooldown = 3.0f;
    [Tooltip("Độ trễ vung kiếm trước khi phóng sóng kiếm (giây). Mặc định: 0.42s")]
    [SerializeField] private float attackSlashDelay = 0.42f;
    [Tooltip("Tốc độ bay của sóng kiếm dọc (m/s)")]
    [SerializeField] private float attackSlashProjectileSpeed = 22f;
    [Tooltip("Góc nghiêng sóng kiếm (độ): 90 = chém dọc đứng (có thể chỉnh 90 hoặc -90, 0 = ngang)")]
    [SerializeField] private float attackSlashTiltAngle = 90f;
    [Tooltip("Bù độ cao xuất phát của sóng kiếm dọc (mét: + là nâng cao lên, - là hạ thấp xuống)")]
    [SerializeField] private float attackSlashHeightOffset = 0f;
    [Tooltip("Tổng thời gian thực hiện chiêu/khóa hành động (giây)")]
    [SerializeField] private float attackSlashTotalDuration = 1.5f;

    [Space(10)]
    [Header("=== KỸ NĂNG 2: HORIZONTAL SLASH (CHÉM NGANG - PHÍM 2) ===")]
    [Tooltip("Prefab hiệu ứng vung kiếm ngang trên tay (Ignis_Combo_Slash)")]
    [SerializeField] private GameObject horizontalSlashSwingVfxPrefab;
    [Tooltip("Prefab sóng kiếm bay ngang (Ignis_Slash_Horizontal)")]
    [SerializeField] private GameObject horizontalSlashPrefab;
    [Tooltip("Prefab hiệu ứng va chạm của sóng kiếm ngang (Ignis_Slash_Impact)")]
    [SerializeField] private GameObject horizontalSlashImpactVfxPrefab;
    [Tooltip("Sát thương sóng kiếm ngang")]
    [SerializeField] private float horizontalSlashDamage = 50f;
    [Tooltip("Thời gian hồi chiêu (giây)")]
    [SerializeField] private float horizontalSlashCooldown = 3.5f;
    [Tooltip("Độ trễ vung kiếm trước khi phóng sóng kiếm (giây). Mặc định: 0.40s")]
    [SerializeField] private float horizontalSlashDelay = 0.40f;
    [Tooltip("Tốc độ bay của sóng kiếm ngang (m/s)")]
    [SerializeField] private float horizontalSlashProjectileSpeed = 20f;
    [Tooltip("Góc nghiêng sóng kiếm (độ): 0 = chém ngang")]
    [SerializeField] private float horizontalSlashTiltAngle = 0f;
    [Tooltip("Bù độ cao xuất phát của sóng kiếm ngang (mét: + là nâng cao lên, - là hạ thấp xuống)")]
    [SerializeField] private float horizontalSlashHeightOffset = 0f;
    [Tooltip("Tổng thời gian thực hiện chiêu/khóa hành động (giây)")]
    [SerializeField] private float horizontalSlashTotalDuration = 1.6f;

    [Space(10)]
    [Header("=== KỸ NĂNG 3: JUMP ATTACK (NHẢY CHÉM DẬM ĐẤT - PHÍM 3) ===")]
    [Tooltip("Prefab hiệu ứng nổ lan dậm đất (Ignis_Jump_Impact)")]
    [SerializeField] private GameObject jumpImpactVfxPrefab;
    [Tooltip("Sát thương nổ lan khi dậm đất")]
    [SerializeField] private float jumpAttackDamage = 80f;
    [Tooltip("Thời gian hồi chiêu (giây)")]
    [SerializeField] private float jumpAttackCooldown = 5.0f;
    [Tooltip("Độ trễ nhún người trước khi bật nhảy (giây). Mặc định: 0.25s")]
    [SerializeField] private float jumpAttackWindupDelay = 0.25f;
    [Tooltip("Thời gian bay trên không lướt tới điểm rơi (giây). Mặc định: 0.55s")]
    [SerializeField] private float jumpAttackLeapDuration = 0.55f;
    [Tooltip("Cự ly nhảy tới tối đa (m). Đã cân chỉnh cho boss scale 2.0")]
    [SerializeField] private float jumpLeapDistance = 7.5f;
    [Tooltip("Độ cao nhấc bổng nhảy parabol (m). Đã cân chỉnh cho boss scale 2.0")]
    [SerializeField] private float jumpAttackApexHeight = 1.8f;
    [Tooltip("Bán kính nổ sát thương AoE khi dậm đất (m)")]
    [SerializeField] private float jumpAttackRadius = 4.0f;
    [Tooltip("Bù độ cao điểm nổ dậm đất (mét: + nâng cao hơn mặt sàn)")]
    [SerializeField] private float jumpImpactHeightOffset = 0f;
    [Tooltip("Thời gian hồi phục sau khi tiếp đất trước khi hành động tiếp (giây)")]
    [SerializeField] private float jumpAttackRecoveryDuration = 0.85f;

    [Space(10)]
    [Header("=== KỸ NĂNG 4: COMBO ATTACK (CHÉM LIÊN HOÀN 4 NHÁT - PHÍM 4) ===")]
    [Tooltip("Prefab hiệu ứng vệt chém khi đánh combo (Ignis_Combo_Slash)")]
    [SerializeField] private GameObject comboSlashVfxPrefab;
    [Tooltip("Sát thương mỗi nhát chém")]
    [SerializeField] private float comboHitDamage = 35f;
    [Tooltip("Thời gian hồi chiêu (giây)")]
    [SerializeField] private float comboAttackCooldown = 6.0f;
    [Tooltip("Mốc thời gian (giây) của từng nhát chém 1, 2, 3, 4 trong clip animation")]
    [SerializeField] private float[] comboHitTimers = new float[] { 0.40f, 0.95f, 1.55f, 2.20f };
    [Tooltip("Khoảng cách nhích lướt tới ở mỗi nhát chém (m). Đã cân chỉnh cho boss scale 2.0")]
    [SerializeField] private float comboLungeDistance = 0.75f;
    [Tooltip("Bán kính chém trúng quét hình nón trước mặt (m)")]
    [SerializeField] private float comboHitRadius = 3.2f;
    [Tooltip("Bù độ cao vệt chém combo (mét: + nâng cao lên, - hạ thấp xuống)")]
    [SerializeField] private float comboVfxHeightOffset = 0f;
    [Tooltip("Thời gian hồi phục sau khi kết thúc 4 nhát chém (giây)")]
    [SerializeField] private float comboRecoveryDuration = 0.90f;

    [Header("=== PHẢN HỒI NHẬN ĐÒN & CHỚP ĐỎ ===")]
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    // Private runtime state
    private Animator anim;
    private CharacterController characterController;
    private Health health;
    private Renderer[] allRenderers;
    private List<Material> flashMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();
    private Coroutine flashCoroutine;

    private Vector2 desiredMoveInput = Vector2.zero;
    private Vector2 currentSmoothMoveInput = Vector2.zero;
    private float currentSmoothSpeed = 0f;
    private int currentActiveMoveType = 0;
    private float lastMoveCommandTime = -1f;
    private bool hasManualArrowInput = false;

    private bool isAttacking = false;
    private bool isWeakened = false;
    private bool isDead = false;
    private Coroutine currentAttackRoutine = null;
    private Coroutine weakenRoutine = null;

    private float[] skillCooldownTimers = new float[4];
    private float[] skillCooldownMax = new float[4];

    // Animator Hashes
    private static readonly int MoveTypeHash = Animator.StringToHash("MoveType");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int AttackSlashHash = Animator.StringToHash("AttackSlash");
    private static readonly int HorizontalSlashHash = Animator.StringToHash("HorizontalSlash");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
    private static readonly int ComboAttackHash = Animator.StringToHash("ComboAttack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int WeakenHash = Animator.StringToHash("Weaken");

    // Public properties
    public float BossMaxHealth => bossMaxHealth;
    public float CurrentPoise => currentPoise;
    public float MaxPoise => maxPoise;
    public float CurrentStamina => currentPoise;
    public float MaxStamina => maxPoise;
    public bool IsWeakened => isWeakened;
    public bool IsDead => isDead;

    private void Awake()
    {
        // 1. Tìm đúng Animator chứa Humanoid Avatar trên GameObject con RedIgnis
        Animator childAnim = null;
        Animator[] allAnims = GetComponentsInChildren<Animator>(true);
        foreach (var a in allAnims)
        {
            if (a.gameObject != gameObject && a.isHuman)
            {
                childAnim = a;
                break;
            }
            if (a.gameObject != gameObject && childAnim == null)
            {
                childAnim = a;
            }
        }

        // 2. Nếu trên root cha Boss_RedIgnis có Animator thừa (không có Avatar), gỡ bỏ ngay để không bị kẹt T-Pose
        Animator rootAnim = GetComponent<Animator>();
        if (rootAnim != null && rootAnim != childAnim)
        {
            Destroy(rootAnim);
        }

        anim = (childAnim != null) ? childAnim : rootAnim;
        if (anim != null)
        {
            anim.enabled = true;
            anim.applyRootMotion = false;
        }

        characterController = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();

        SyncCooldownMax();
        CacheRenderers();
        AutoFindWeaponSocket();
        EnsureSpawnPoint();
        AutoCalibrateColliderForScale();
        ApplyBossStats();
    }

    private void OnValidate()
    {
        if (attackSlashImpactVfxPrefab == null && slashImpactVfxPrefab != null) attackSlashImpactVfxPrefab = slashImpactVfxPrefab;
        if (horizontalSlashImpactVfxPrefab == null && slashImpactVfxPrefab != null) horizontalSlashImpactVfxPrefab = slashImpactVfxPrefab;
        AutoFindWeaponSocket();
        EnsureSpawnPoint();
        AutoCalibrateColliderForScale();
        SyncHealthToComponent();
        SyncCooldownMax();
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null) health = GetComponentInParent<Health>();
        }

        if (health != null)
        {
            health.onTakeDamage.AddListener(HandleOnTakeDamage);
            health.onDeath.AddListener(Die);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onTakeDamage.RemoveListener(HandleOnTakeDamage);
            health.onDeath.RemoveListener(Die);
        }
    }

    private void Start()
    {
        ApplyBossStats();
        if (autoFindPlayer && playerTarget == null)
        {
            FindPlayerTarget();
        }
    }

    private void CacheRenderers()
    {
        allRenderers = GetComponentsInChildren<Renderer>(true);
        flashMaterials.Clear();
        originalColors.Clear();

        foreach (var r in allRenderers)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            foreach (var mat in r.materials)
            {
                if (mat != null && mat.HasProperty("_BaseColor"))
                {
                    flashMaterials.Add(mat);
                    originalColors.Add(mat.GetColor("_BaseColor"));
                }
                else if (mat != null && mat.HasProperty("_Color"))
                {
                    flashMaterials.Add(mat);
                    originalColors.Add(mat.GetColor("_Color"));
                }
            }
        }
    }

    private void AutoFindWeaponSocket()
    {
        if (weaponSocket == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child.name == "Hand_R_Weapon")
                {
                    weaponSocket = child;
                    break;
                }
            }
        }
    }

    private void EnsureSpawnPoint()
    {
        if (slashSpawnPoint == null)
        {
            Transform existing = transform.Find("SlashSpawnPoint");
            if (existing != null)
            {
                slashSpawnPoint = existing;
            }
            else
            {
                GameObject spawnGo = new GameObject("SlashSpawnPoint");
                spawnGo.transform.SetParent(transform);
                spawnGo.transform.localPosition = new Vector3(0.08f, 0.75f, 0.70f);
                spawnGo.transform.localRotation = Quaternion.identity;
                spawnGo.transform.localScale = Vector3.one;
                slashSpawnPoint = spawnGo.transform;
            }
        }

        if (slashSpawnPoint != null)
        {
            // Chuẩn hóa scale = 1 tránh thừa hưởng scale dị dạng từ các lần cấu hình cũ
            slashSpawnPoint.localScale = Vector3.one;
            // Nếu vị trí cũ quá cao (như 1.35) trên scale 2.0 -> tự hạ xuống tầm ngực/kiếm 0.75m
            if (slashSpawnPoint.localPosition.y > 1.0f)
            {
                slashSpawnPoint.localPosition = new Vector3(0.08f, 0.75f, 0.70f);
            }
        }
    }

    [ContextMenu("Cân Chỉnh Kích Thước Theo Scale")]
    public void AutoCalibrateColliderForScale()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            float s = transform.localScale.y;
            if (s > 0.05f)
            {
                // Chiều cao mong muốn trong World Space là ~2.9m, bán kính ~0.70m
                characterController.height = 2.9f / s;
                characterController.center = new Vector3(0f, 1.45f / s, 0f);
                characterController.radius = 0.35f; // Bán kính world = 0.35 * s = 0.70m
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.08f;
            }
        }
    }

    private void SyncCooldownMax()
    {
        if (skillCooldownMax == null || skillCooldownMax.Length != 4)
        {
            skillCooldownMax = new float[4];
        }
        skillCooldownMax[0] = attackSlashCooldown;
        skillCooldownMax[1] = horizontalSlashCooldown;
        skillCooldownMax[2] = jumpAttackCooldown;
        skillCooldownMax[3] = comboAttackCooldown;
    }

    public void ApplyBossStats()
    {
        SyncHealthToComponent();
        currentPoise = maxPoise;
        isWeakened = false;
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);
    }

    private void SyncHealthToComponent()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null) health = GetComponentInParent<Health>();
        }

        if (health != null && bossMaxHealth > 0f)
        {
            var maxHpField = typeof(Health).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (maxHpField != null) maxHpField.SetValue(health, bossMaxHealth);

            var curHpField = typeof(Health).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (curHpField != null) curHpField.SetValue(health, bossMaxHealth);

            var baseHpField = typeof(Health).GetField("baseMaxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (baseHpField != null) baseHpField.SetValue(health, bossMaxHealth);

            health.SetBaseDefense(bossDefense);
            health.onHealthChanged?.Invoke(bossMaxHealth, bossMaxHealth);
            onHealthChanged?.Invoke(bossMaxHealth, bossMaxHealth);
        }
    }

    private void Update()
    {
        if (isDead) return;

        UpdateCooldowns(Time.deltaTime);
        HandleKeyboardInput();
        UpdateLocomotion(Time.deltaTime);
    }

    private void UpdateCooldowns(float dt)
    {
        for (int i = 0; i < skillCooldownTimers.Length; i++)
        {
            if (skillCooldownTimers[i] > 0f)
            {
                skillCooldownTimers[i] = Mathf.Max(0f, skillCooldownTimers[i] - dt);
            }
        }
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. PHÍM MŨI TÊN DI CHUYỂN (ARROW KEYS)
        float horizontal = 0f;
        float vertical = 0f;

        hasManualArrowInput = keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
                              keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;

        if (hasManualArrowInput)
        {
            if (keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.rightArrowKey.isPressed) horizontal += 1f;

            Move(horizontal, vertical);
        }

        // 2. PHÍM SỐ TẤN CÔNG (1, 2, 3, 4 HOẶC NUMPAD 1, 2, 3, 4)
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) CastAttack(0); // AttackSlashRI
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) CastAttack(1); // HorizontalSlashRI
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) CastAttack(2); // JumpAttackRI
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) CastAttack(3); // ComboAttackRI

        // 3. Phím thử nghiệm: Chết (K)
        if (keyboard.kKey.wasPressedThisFrame) Die();
    }

    /// <summary>
    /// Nhận lệnh di chuyển từ Bàn phím hoặc AI Agent (ML-Agents)
    /// </summary>
    public void Move(float horizontal, float vertical)
    {
        if (IsBusy())
        {
            desiredMoveInput = Vector2.zero;
            return;
        }

        desiredMoveInput = new Vector2(horizontal, vertical);
        if (desiredMoveInput.sqrMagnitude > 1f)
        {
            desiredMoveInput.Normalize();
        }
        lastMoveCommandTime = Time.time;
    }

    private void UpdateLocomotion(float dt)
    {
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
            if (anim == null) return;
        }

        if (IsBusy())
        {
            desiredMoveInput = Vector2.zero;
            currentSmoothMoveInput = Vector2.zero;
            currentSmoothSpeed = 0f;
            currentActiveMoveType = 0;
            anim.SetFloat(SpeedHash, 0f, locomotionDampTime, dt);
            anim.SetInteger(MoveTypeHash, 0);
            anim.SetFloat(MoveXHash, 0f, locomotionDampTime, dt);
            anim.SetFloat(MoveZHash, 0f, locomotionDampTime, dt);

            // Duy trì trọng lực và chống kẹt trên đầu Player ngay cả khi đang bận/hồi chiêu
            ApplyMovementAndGravity(Vector3.zero, dt);
            return;
        }

        // Tự động dừng nếu quá 0.45s không có lệnh di chuyển từ AI hay bàn phím
        if (!hasManualArrowInput && (Time.time - lastMoveCommandTime > 0.45f))
        {
            desiredMoveInput = Vector2.zero;
        }

        // Gia tốc làm mượt đầu vào di chuyển
        float accel = (desiredMoveInput.sqrMagnitude > 0.01f) ? moveAcceleration : moveDeceleration;
        currentSmoothMoveInput = Vector2.MoveTowards(currentSmoothMoveInput, desiredMoveInput, accel * dt);
        currentSmoothSpeed = currentSmoothMoveInput.magnitude;

        // Xoay mặt mượt mà về phía Player
        if (alwaysFacePlayer)
        {
            Transform target = GetPlayerTarget();
            if (target != null)
            {
                SmoothFaceTarget(target, facePlayerSpeed);
            }
        }

        // Tính toán vận tốc di chuyển ngang
        Vector3 horizontalMove = Vector3.zero;
        if (currentSmoothSpeed > 0.02f)
        {
            Vector3 forwardMove = transform.forward * currentSmoothMoveInput.y * moveSpeed;
            Vector3 strafeMove = transform.right * currentSmoothMoveInput.x * strafeSpeed;
            horizontalMove = (forwardMove + strafeMove);
        }

        // Áp dụng di chuyển ngang + Trọng lực liên tục + Chống kẹt trên đầu Player
        ApplyMovementAndGravity(horizontalMove, dt);

        // Cập nhật Animator mượt mà bằng DampTime
        anim.SetFloat(SpeedHash, currentSmoothSpeed, locomotionDampTime, dt);
        anim.SetFloat(MoveXHash, currentSmoothMoveInput.x, locomotionDampTime, dt);
        anim.SetFloat(MoveZHash, currentSmoothMoveInput.y, locomotionDampTime, dt);

        // Xác định MoveType (1=Walk Forward, 2=Walk Back, 3=Walk Left, 4=Walk Right, 0=Idle)
        int newMoveType = currentActiveMoveType;
        if (currentSmoothSpeed >= 0.12f)
        {
            if (Mathf.Abs(currentSmoothMoveInput.y) >= Mathf.Abs(currentSmoothMoveInput.x) * 0.85f)
            {
                newMoveType = (currentSmoothMoveInput.y > 0f) ? 1 : 2;
            }
            else
            {
                newMoveType = (currentSmoothMoveInput.x > 0f) ? 4 : 3;
            }
        }
        else if (currentSmoothSpeed < 0.05f)
        {
            newMoveType = 0;
        }

        if (newMoveType != currentActiveMoveType)
        {
            currentActiveMoveType = newMoveType;
            anim.SetInteger(MoveTypeHash, currentActiveMoveType);
        }
    }

    private void ApplyMovementAndGravity(Vector3 horizontalVelocity, float dt)
    {
        if (characterController != null && characterController.enabled)
        {
            if (characterController.isGrounded)
            {
                verticalVelocity = groundedGravity;
            }
            else
            {
                verticalVelocity += gravity * dt;
                if (verticalVelocity < -35f) verticalVelocity = -35f;
            }

            // Chống kẹt trên đầu Player: Nếu Boss nằm ngay phía trên Player (bán kính ngang < 1.3m)
            Transform pTarget = GetPlayerTarget();
            if (pTarget != null)
            {
                Vector3 toBoss = transform.position - pTarget.position;
                float yDiff = toBoss.y;
                toBoss.y = 0f;
                float horizDist = toBoss.magnitude;

                if (horizDist < 1.3f)
                {
                    Vector3 slideDir = (horizDist > 0.05f) ? toBoss.normalized : -transform.forward;
                    // Nếu Boss đang ở trên đầu Player (y cao hơn), trượt mạnh ra ngoài để rơi xuống đất ngay
                    float pushSpeed = (yDiff > 0.3f) ? 6.0f : 2.0f;
                    horizontalVelocity += slideDir * pushSpeed;
                }
            }

            Vector3 totalMove = horizontalVelocity;
            totalMove.y = verticalVelocity;
            characterController.Move(totalMove * dt);
        }
        else
        {
            Vector3 nextPos = transform.position + horizontalVelocity * dt;
            Vector3 groundPos = GetGroundPosition(nextPos);
            nextPos.y = groundPos.y;
            transform.position = nextPos;
        }
    }

    public void CastAttack(int attackIndex)
    {
        if (IsBusy()) return;
        if (!CanCastAttack(attackIndex)) return;

        SyncCooldownMax();
        skillCooldownTimers[attackIndex] = skillCooldownMax[attackIndex];

        switch (attackIndex)
        {
            case 0:
                currentAttackRoutine = StartCoroutine(AttackSlashRoutine());
                break;
            case 1:
                currentAttackRoutine = StartCoroutine(HorizontalSlashRoutine());
                break;
            case 2:
                currentAttackRoutine = StartCoroutine(JumpAttackRoutine());
                break;
            case 3:
                currentAttackRoutine = StartCoroutine(ComboAttackRoutine());
                break;
        }
    }

    // ==========================================
    // KỸ NĂNG 1: ATTACK SLASH RI (CHÉM DỌC + VFX BAY DỌC)
    // ==========================================
    private IEnumerator AttackSlashRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if (anim != null) anim.SetTrigger(AttackSlashHash);

        // Xoay nhẹ hướng về player trong giai đoạn lấy đà theo attackSlashDelay
        Transform target = GetPlayerTarget();
        float elapsed = 0f;
        while (elapsed < attackSlashDelay)
        {
            elapsed += Time.deltaTime;
            if (target != null) SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
            yield return null;
        }

        // 1. Hiệu ứng vệt chém lửa vung kiếm trên tay (xoay dọc 90 độ chém xuống từ trên cao)
        SpawnSwordSwingArc(attackSlashSwingVfxPrefab, 90f, attackSlashHeightOffset);

        // 2. Phóng đường kiếm bay dọc theo thông số tùy chỉnh trong Inspector
        GameObject impactToUse = (attackSlashImpactVfxPrefab != null) ? attackSlashImpactVfxPrefab : slashImpactVfxPrefab;
        SpawnFlyingSlash(verticalSlashPrefab, attackSlashTiltAngle, attackSlashDamage, attackSlashProjectileSpeed, impactToUse, attackSlashHeightOffset);

        // Chờ kết thúc động tác vung kiếm
        float remainingDuration = Mathf.Max(0.1f, attackSlashTotalDuration - attackSlashDelay);
        yield return new WaitForSeconds(remainingDuration);

        isAttacking = false;
        currentAttackRoutine = null;
    }

    // ==========================================
    // KỸ NĂNG 2: HORIZONTAL SLASH RI (CHÉM NGANG + VFX BAY NGANG)
    // ==========================================
    private IEnumerator HorizontalSlashRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if (anim != null) anim.SetTrigger(HorizontalSlashHash);

        Transform target = GetPlayerTarget();
        float elapsed = 0f;
        while (elapsed < horizontalSlashDelay)
        {
            elapsed += Time.deltaTime;
            if (target != null) SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
            yield return null;
        }

        // 1. Hiệu ứng vệt chém lửa vung kiếm trên tay (quét ngang 0 độ)
        SpawnSwordSwingArc(horizontalSlashSwingVfxPrefab, 0f, horizontalSlashHeightOffset);

        // 2. Phóng đường kiếm bay ngang theo thông số tùy chỉnh trong Inspector
        GameObject impactToUse = (horizontalSlashImpactVfxPrefab != null) ? horizontalSlashImpactVfxPrefab : slashImpactVfxPrefab;
        SpawnFlyingSlash(horizontalSlashPrefab, horizontalSlashTiltAngle, horizontalSlashDamage, horizontalSlashProjectileSpeed, impactToUse, horizontalSlashHeightOffset);

        float remainingDuration = Mathf.Max(0.1f, horizontalSlashTotalDuration - horizontalSlashDelay);
        yield return new WaitForSeconds(remainingDuration);

        isAttacking = false;
        currentAttackRoutine = null;
    }

    private void SpawnSwordSwingArc(GameObject swingPrefab, float tiltAngle, float heightOffset = 0f)
    {
        if (swingPrefab == null) return;

        Vector3 spawnPos = (slashSpawnPoint != null) 
            ? slashSpawnPoint.position + Vector3.up * heightOffset 
            : transform.position + transform.forward * 1.2f + Vector3.up * (1.4f + heightOffset);

        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f, 0f, tiltAngle);
        GameObject arc = Instantiate(swingPrefab, spawnPos, spawnRot);
        Destroy(arc, 1.5f);
    }

    private void SpawnFlyingSlash(GameObject projectilePrefab, float tiltAngle, float slashDmg, float projSpeed, GameObject impactPrefab, float heightOffset = 0f)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = (slashSpawnPoint != null) 
            ? slashSpawnPoint.position 
            : transform.position + transform.forward * 1.4f + Vector3.up * 1.5f;
        spawnPos += Vector3.up * heightOffset;
        Vector3 fireDirection = transform.forward;

        Transform target = GetPlayerTarget();
        if (target != null)
        {
            Vector3 toTarget = (target.position - spawnPos);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.1f)
            {
                fireDirection = toTarget.normalized;
            }
        }

        Quaternion spawnRot = Quaternion.LookRotation(fireDirection) * Quaternion.Euler(0f, 0f, tiltAngle);
        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        RedIgnisSlashProjectile slash = proj.GetComponent<RedIgnisSlashProjectile>();
        if (slash != null)
        {
            slash.Damage = slashDmg;
            if (impactPrefab != null) slash.HitVfxPrefab = impactPrefab;
            slash.Launch(transform, fireDirection, projSpeed, tiltAngle);
        }
    }

    // ==========================================
    // KỸ NĂNG 3: JUMP ATTACK RI (NHẢY TỚI DẬM ĐẤT CHÉM NGANG)
    // ==========================================
    private IEnumerator JumpAttackRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if (anim != null) anim.SetTrigger(JumpAttackHash);

        Transform target = GetPlayerTarget();
        Vector3 startPos = transform.position;
        Vector3 targetLandingPos = startPos + transform.forward * jumpLeapDistance;

        if (target != null)
        {
            Vector3 toTarget = (target.position - startPos);
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            // Dừng trước Player một cự ly an toàn (1.5m) để quét chém đại kiếm, tuyệt đối không nhảy cắm vào tâm hoặc dẫm lên đầu Player!
            float stopOffset = 1.5f;
            float leapDist = (dist > stopOffset) ? Mathf.Min(dist - stopOffset, jumpLeapDistance) : 0f;
            targetLandingPos = startPos + toTarget.normalized * leapDist;
            SmoothFaceTarget(target, 500f);
        }

        // Lấy cao độ mặt sàn thực tế tại điểm rơi và điểm xuất phát
        targetLandingPos = GetGroundPosition(targetLandingPos);
        startPos = GetGroundPosition(startPos);

        // Giai đoạn nhún người chuẩn bị nhảy
        yield return new WaitForSeconds(jumpAttackWindupDelay);

        // Tạm thời tắt CharacterController để chuyển động parabol bay mượt mà, không va cấn Player trên không
        if (characterController != null) characterController.enabled = false;

        // Giai đoạn bay trên không lướt tới điểm rơi theo jumpAttackLeapDuration
        float leapDuration = Mathf.Max(0.1f, jumpAttackLeapDuration);
        float leapTimer = 0f;
        while (leapTimer < leapDuration)
        {
            leapTimer += Time.deltaTime;
            float t = Mathf.Clamp01(leapTimer / leapDuration);
            Vector3 currentPos = Vector3.Lerp(startPos, targetLandingPos, t);

            // Đường cong nhảy parabol nhấc bổng theo jumpAttackApexHeight
            float arcHeight = Mathf.Sin(t * Mathf.PI) * jumpAttackApexHeight;
            currentPos.y = Mathf.Lerp(startPos.y, targetLandingPos.y, t) + arcHeight;

            transform.position = currentPos;
            yield return null;
        }

        // Chạm đất dậm kiếm: Đặt đúng vị trí mặt sàn và bật lại CharacterController
        transform.position = targetLandingPos;
        if (characterController != null)
        {
            characterController.enabled = true;
            characterController.Move(Vector3.down * 0.15f);
        }
        verticalVelocity = groundedGravity;

        // Spawn VFX dậm đất nổ lan
        if (jumpImpactVfxPrefab != null)
        {
            Vector3 slamPoint = transform.position + transform.forward * 1.2f + Vector3.up * jumpImpactHeightOffset;
            Instantiate(jumpImpactVfxPrefab, slamPoint, Quaternion.identity);
        }

        // Gây sát thương AoE diện rộng
        DealAreaDamage(transform.position + transform.forward * 1.0f, jumpAttackRadius, jumpAttackDamage, 2.5f);

        yield return new WaitForSeconds(jumpAttackRecoveryDuration);

        isAttacking = false;
        currentAttackRoutine = null;
    }

    // ==========================================
    // KỸ NĂNG 4: COMBO ATTACK RI (CHÉM LIÊN TỤC 4 NHÁT)
    // ==========================================
    private IEnumerator ComboAttackRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if (anim != null) anim.SetTrigger(ComboAttackHash);

        if (comboHitTimers == null || comboHitTimers.Length == 0)
        {
            comboHitTimers = new float[] { 0.40f, 0.95f, 1.55f, 2.20f };
        }

        float lastTime = 0f;
        for (int hit = 0; hit < comboHitTimers.Length; hit++)
        {
            float targetTime = comboHitTimers[hit];
            float waitDelta = Mathf.Max(0.01f, targetTime - lastTime);
            lastTime = targetTime;
            yield return new WaitForSeconds(waitDelta);

            // Nhích nhẹ tới trước ở mỗi nhát chém theo comboLungeDistance
            Vector3 lungePos = transform.position + transform.forward * comboLungeDistance;
            transform.position = GetGroundPosition(lungePos);

            Transform target = GetPlayerTarget();
            if (target != null) SmoothFaceTarget(target, 320f);

            // Gây sát thương nhát chém hình nón trước mặt
            DealAreaDamage(transform.position + transform.forward * 1.2f + Vector3.up * 1.0f, comboHitRadius, comboHitDamage, 1.2f);

            // VFX vệt chém
            if (comboSlashVfxPrefab != null)
            {
                Vector3 slashPos = (slashSpawnPoint != null) 
                    ? slashSpawnPoint.position + Vector3.up * comboVfxHeightOffset 
                    : transform.position + transform.forward * 1.2f + Vector3.up * (1.4f + comboVfxHeightOffset);
                GameObject comboObj = Instantiate(comboSlashVfxPrefab, slashPos, transform.rotation);
                Destroy(comboObj, 1.5f);
            }
        }

        yield return new WaitForSeconds(comboRecoveryDuration);

        isAttacking = false;
        currentAttackRoutine = null;
    }

    private void DealAreaDamage(Vector3 center, float radius, float dmg, float knock)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        HashSet<Transform> hitRoots = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.root == transform) continue;
            Transform root = hit.transform.root;
            if (hitRoots.Contains(root)) continue;

            if (hit.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                hitRoots.Add(root);
                Health h = hit.GetComponentInParent<Health>();
                if (h != null)
                {
                    Vector3 knockDir = (hit.transform.position - transform.position).normalized;
                    knockDir.y = 0.2f;
                    h.TakeDamage(dmg, hit.ClosestPoint(center), knockDir, knock, transform);
                }
            }
        }
    }

    // ==========================================
    // POISE & WEAKEN (SỨC BỀN & CHOÁNG)
    // ==========================================
    public void TakePoiseDamage(float poiseDamage)
    {
        if (isDead) return;

        currentPoise = Mathf.Max(0f, currentPoise - poiseDamage);
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);

        if (currentPoise <= 0.01f && !isWeakened)
        {
            if (weakenRoutine != null) StopCoroutine(weakenRoutine);
            weakenRoutine = StartCoroutine(WeakenRoutine());
        }
    }

    public void TakeStaminaDamage(float amount) => TakePoiseDamage(amount);
    public void DeductPoise(float amount) => TakePoiseDamage(amount);
    public void DeductStamina(float amount) => TakePoiseDamage(amount);

    private IEnumerator WeakenRoutine()
    {
        isWeakened = true;
        isAttacking = false;
        if (currentAttackRoutine != null)
        {
            StopCoroutine(currentAttackRoutine);
            currentAttackRoutine = null;
        }

        if (anim != null)
        {
            anim.SetTrigger(WeakenHash);
            anim.SetFloat(SpeedHash, 0f);
            anim.SetInteger(MoveTypeHash, 0);
        }

        yield return new WaitForSeconds(weakenDuration);

        currentPoise = maxPoise;
        isWeakened = false;
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);
        weakenRoutine = null;
    }

    // ==========================================
    // SÁT THƯƠNG & CHỚP ĐỎ
    // ==========================================
    private void HandleOnTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        if (isDead) return;
        TakePoiseDamage(20f); // Mặc định mỗi đòn đánh làm hao 20 poise nếu không chỉ định
        TriggerHitFlash();
    }

    private void TriggerHitFlash()
    {
        if (flashMaterials.Count == 0) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < flashMaterials.Count; i++)
        {
            var mat = flashMaterials[i];
            if (mat == null) continue;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hitFlashColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", hitFlashColor);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < flashMaterials.Count; i++)
        {
            var mat = flashMaterials[i];
            if (mat == null) continue;
            Color orig = originalColors[i];
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", orig);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", orig);
        }
        flashCoroutine = null;
    }

    // ==========================================
    // CHẾT (DIE)
    // ==========================================
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        isWeakened = false;

        if (currentAttackRoutine != null) StopCoroutine(currentAttackRoutine);
        if (weakenRoutine != null) StopCoroutine(weakenRoutine);

        if (anim != null) anim.SetTrigger(DeadHash);

        if (characterController != null) characterController.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        OnBossDefeated?.Invoke();
    }

    // ==========================================
    // TIỆN ÍCH & TRỢ GIÚP
    // ==========================================
    public bool IsBusy()
    {
        return isAttacking || isWeakened || isDead;
    }

    public bool CanCastAttack(int index)
    {
        if (index < 0 || index >= skillCooldownTimers.Length) return false;
        return skillCooldownTimers[index] <= 0.01f;
    }

    public float GetCooldownNormalized(int index)
    {
        if (index < 0 || index >= skillCooldownTimers.Length) return 0f;
        if (skillCooldownMax[index] <= 0f) return 0f;
        return Mathf.Clamp01(skillCooldownTimers[index] / skillCooldownMax[index]);
    }

    public Transform GetPlayerTarget()
    {
        if (playerTarget == null && autoFindPlayer)
        {
            FindPlayerTarget();
        }
        return playerTarget;
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            return;
        }

        var playerCc = FindFirstObjectByType<CharacterController>();
        if (playerCc != null && playerCc.transform != transform)
        {
            playerTarget = playerCc.transform;
            return;
        }

        GameObject byName = GameObject.Find("Player");
        if (byName != null)
        {
            playerTarget = byName.transform;
        }
    }

    private void SmoothFaceTarget(Transform target, float rotSpeed)
    {
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetGroundPosition(Vector3 pos)
    {
        Transform pTarget = GetPlayerTarget();
        // Bắt đầu dò từ vị trí cao hơn 3.5m để bắt sàn, bỏ qua Boss và Player
        RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 3.5f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        float bestFloorY = pos.y;
        float minDiff = float.MaxValue;
        bool found = false;

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue; // Bỏ qua Boss
            if (pTarget != null && hit.transform.root == pTarget.root) continue; // Bỏ qua Player
            if (hit.collider.isTrigger) continue;

            // Mặt sàn hợp lệ phải có normal hướng lên (tránh va vào vách tường thẳng đứng)
            if (hit.normal.y > 0.45f)
            {
                float diff = Mathf.Abs(hit.point.y - pos.y);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestFloorY = hit.point.y;
                    found = true;
                }
            }
        }

        if (found)
        {
            return new Vector3(pos.x, bestFloorY, pos.z);
        }

        return pos;
    }
}
