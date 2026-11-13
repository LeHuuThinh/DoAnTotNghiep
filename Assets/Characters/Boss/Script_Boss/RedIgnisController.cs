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
    [Tooltip("Thời gian dự phòng cho động tác quỳ WeakenFrontRI nếu không có Animator (giây). Khi có Animator, hệ thống tự động bám theo clip.")]
    [SerializeField] private float weakenFrontDuration = 4.27f;
    [Tooltip("Thời gian dự phòng cho động tác đứng dậy WeakenBackRI nếu không có Animator (giây). Khi có Animator, hệ thống tự động bám theo clip.")]
    [SerializeField] private float weakenBackDuration = 3.63f;

    [Header("=== EVENTS SỰ KIỆN BOSS (UI BARS) ===")]
    public UnityEvent<float, float> onPoiseChanged; // (currentPoise, maxPoise)
    public UnityEvent<float, float> onStaminaChanged; // Alias tương thích ngược
    public UnityEvent<float, float> onHealthChanged; // (currentHealth, maxHealth)

    [Header("=== DI CHUYỂN & LOCOMOTION ENGINE ===")]
    [Tooltip("Tốc độ đi bộ tới (m/s)")]
    [SerializeField] private float moveSpeed = 4.2f;
    [Tooltip("Tốc độ đi bộ lùi (m/s) - Chậm hơn đi tới để Player dễ áp sát đổi đòn")]
    [SerializeField] private float backwardSpeed = 1.8f;
    [Tooltip("Tốc độ di chuyển ngang (strafe) trái / phải (m/s)")]
    [SerializeField] private float strafeSpeed = 1.9f;
    [Tooltip("Gia tốc tăng tốc khi bắt đầu di chuyển")]
    [SerializeField] private float moveAcceleration = 9.0f;
    [Tooltip("Gia tốc giảm tốc khi dừng")]
    [SerializeField] private float moveDeceleration = 12.0f;
    [Tooltip("Thời gian làm mượt thông số Animator (DampTime)")]
    [SerializeField] private float locomotionDampTime = 0.1f;
    [Tooltip("Luôn tự động xoay mặt hướng về phía Player khi rảnh rỗi hoặc di chuyển")]
    [SerializeField] private bool alwaysFacePlayer = true;
    [Tooltip("Tốc độ xoay mặt theo Player (độ/giây)")]
    [SerializeField] private float facePlayerSpeed = 110f;
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

    [Header("=== HIỆU ỨNG LỬA KIẾM (SWORD FLAME AURA) ===")]
    [Tooltip("Hiệu ứng lửa bao quanh thanh kiếm (Ignis_Sword_Flame)")]
    [SerializeField] private IgnisSwordFlame weaponFlame;
    [Tooltip("Hiệu ứng vệt chém kéo dài từ thân đại kiếm (IgnisSwordTrail) - chỉ kích hoạt khi xuất chiêu, tắt khi đứng yên/di chuyển")]
    [SerializeField] private IgnisSwordTrail weaponTrail;
    [Tooltip("Tự động bốc cháy kiếm khi ra chiêu (1, 2, 3, 4) và tắt khi xong chiêu")]
    [SerializeField] private bool autoIgniteSwordOnAttack = false;
    [Tooltip("Bật lửa kiếm liên tục để xem trước hiệu ứng ngay cả khi đứng yên / đi bộ (không làm gì)")]
    [SerializeField] private bool previewFlameAlwaysOn = false;

    [Space(10)]
    [Header("=== GIAI ĐOẠN 50% MÁU & NIỆM KIẾM LỬA (SWORD CASTING) ===")]
    [Tooltip("Ngưỡng máu kích hoạt động tác SwordCastingRI (0.5 = 50% máu)")]
    [SerializeField] private float swordCastingHealthThreshold = 0.50f;
    [Tooltip("Lượng sát thương cộng thêm vĩnh viễn cho tất cả đòn đánh sau khi niệm kiếm")]
    [SerializeField] private float swordEnrageBonusDamage = 10.0f;
    [Tooltip("Thời gian chờ trước khi bốc lửa trong động tác SwordCastingRI (giây)")]
    [SerializeField] private float swordCastingIgniteDelay = 0.85f;
    [Tooltip("Tổng thời gian diễn ra động tác SwordCastingRI (giây)")]
    [SerializeField] private float swordCastingTotalDuration = 2.0f;

    private bool hasTriggeredSwordCasting = false;
    private bool isCastingSword = false;
    private bool isSwordBuffActive = false;
    private Coroutine swordCastingRoutine = null;

    private static readonly int SwordCastingHash = Animator.StringToHash("SwordCasting");

    [Space(10)]
    [Header("=== TIMING MIXUP / KHỰNG HOẠT ẢNH LẤY ĐÀ (HOLD WIND-UP) ===")]
    [Tooltip("Bật/tắt cơ chế giữ nhịp đòn lấy đà (Timing Mixup / Hold Wind-up) - Áp dụng cho chiêu 2, 3, 4 (loại trừ chiêu 1)")]
    [SerializeField] private bool enableTimingMixup = true;
    [Tooltip("Tỷ lệ kích hoạt giữ nhịp đòn lấy đà (0.5 = 50% ngẫu nhiên lúc có lúc không, 1.0 = luôn luôn giữ nhịp, 0 = tắt)")]
    [Range(0f, 1f)]
    [SerializeField] private float timingMixupChance = 0.5f;
    [Tooltip("Thời gian hoạt ảnh chạy tự nhiên trước khi khựng giữ nhịp (giây, mặc định 0.20s để vung kiếm lên đỉnh)")]
    [SerializeField] private float windupWaitTime = 0.20f;
    [Tooltip("Thời gian dừng/khựng tối thiểu (giây) - Mặc định 0.5s")]
    [SerializeField] private float minHoldDuration = 0.5f;
    [Tooltip("Thời gian dừng/khựng tối đa (giây) - Mặc định 0.8s")]
    [SerializeField] private float maxHoldDuration = 0.8f;
    [Tooltip("Tốc độ hoạt ảnh trong lúc khựng (0 = đóng băng 100%, 0.02 = nén cơ siêu chậm)")]
    [Range(0f, 0.2f)]
    [SerializeField] private float holdAnimationSpeed = 0.02f;

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
    [SerializeField] private float attackSlashCooldown = 6.0f;
    [Tooltip("Độ trễ vung kiếm trước khi phóng sóng kiếm (giây). Mặc định: 0.42s")]
    [SerializeField] private float attackSlashDelay = 0.42f;
    [Tooltip("Tốc độ bay của sóng kiếm dọc (m/s)")]
    [SerializeField] private float attackSlashProjectileSpeed = 22f;
    [Tooltip("Khoảng cách bay tối đa của sóng kiếm dọc (m)")]
    [SerializeField] private float attackSlashMaxDistance = 18.0f;
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
    [SerializeField] private float horizontalSlashCooldown = 7.0f;
    [Tooltip("Độ trễ vung kiếm trước khi phóng sóng kiếm (giây). Mặc định: 0.40s")]
    [SerializeField] private float horizontalSlashDelay = 0.40f;
    [Tooltip("Tốc độ bay của sóng kiếm ngang (m/s)")]
    [SerializeField] private float horizontalSlashProjectileSpeed = 20f;
    [Tooltip("Khoảng cách bay tối đa của sóng kiếm ngang (m)")]
    [SerializeField] private float horizontalSlashMaxDistance = 16.0f;
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
    [SerializeField] private float jumpAttackCooldown = 10.0f;
    [Tooltip("Độ trễ nhún người trước khi bật nhảy (giây). Mặc định: 0.25s")]
    [SerializeField] private float jumpAttackWindupDelay = 0.25f;
    [Tooltip("Thời gian bay tối thiểu trên không (giây). Mặc định: 0.85s")]
    [SerializeField] private float jumpAttackLeapDuration = 0.85f;
    [Tooltip("Tốc độ bay khi nhảy tới áp sát Player (m/s). Số nhỏ hơn giúp Boss bay chậm, dễ quan sát và né")]
    [SerializeField] private float jumpLeapSpeed = 12.0f;
    [Tooltip("Khoảng cách nhảy tới tối đa (m)")]
    [SerializeField] private float jumpLeapDistance = 25.0f;
    [Tooltip("Khoảng dừng ngay trước mặt Player khi tiếp đất (m)")]
    [SerializeField] private float jumpStopOffset = 1.3f;
    [Tooltip("Độ cao nhấc bổng nhảy parabol (m). Đã cân chỉnh cho boss scale 2.0")]
    [SerializeField] private float jumpAttackApexHeight = 1.8f;
    [Tooltip("Bán kính nổ sát thương AoE khi dậm đất (m)")]
    [SerializeField] private float jumpAttackRadius = 4.5f;
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
    [SerializeField] private float comboAttackCooldown = 12.0f;
    [Tooltip("Mốc thời gian (giây) của từng nhát chém 1, 2, 3, 4 trong clip animation")]
    [SerializeField] private float[] comboHitTimers = new float[] { 0.40f, 0.95f, 1.55f, 2.20f };
    [Tooltip("Khoảng cách nhích lướt tới ở mỗi nhát chém (m).")]
    [SerializeField] private float comboLungeDistance = 0.55f;
    [Tooltip("Bán kính chém trúng quét hình nón trước mặt (m)")]
    [SerializeField] private float comboHitRadius = 3.2f;
    [Tooltip("Bù độ cao vệt chém combo (mét: + nâng cao lên, - hạ thấp xuống)")]
    [SerializeField] private float comboVfxHeightOffset = 0f;
    [Tooltip("Thời gian hồi phục sau khi kết thúc 4 nhát chém (giây)")]
    [SerializeField] private float comboRecoveryDuration = 0.90f;

    [Space(10)]
    [Header("=== NHỊP NGHỈ CHIẾN ĐẤU (COMBAT PACING - TRÁNH SPAM CHIÊU) ===")]
    [Tooltip("Khoảng thời gian giãn cách tối thiểu giữa 2 lần tung chiêu liên tiếp (giây) - Ép Boss lùi lại vờn cự ly")]
    [SerializeField] private float attackPacingInterval = 5.0f;

    private float nextAttackAllowedTime = 0f;
    private int lastUsedSkillIndex = -1; // Chống lặp chiêu: Lưu chiêu vừa sử dụng để không dùng 2 lần liên tiếp

    [Header("=== PHẢN HỒI NHẬN ĐÒN & CHỚP ĐỎ ===")]
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    // Private runtime state
    private Animator anim;
    private CharacterController characterController;
    private Health health;
    private RedIgnisClothMotion clothMotion;
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
    private static readonly int WalkSpeedHash = Animator.StringToHash("WalkSpeed");
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
    public bool IsSwordBuffActive => isSwordBuffActive;
    public bool IsCastingSword => isCastingSword;
    public float SwordEnrageBonusDamage => swordEnrageBonusDamage;
    public float AttackSlashMaxDistance => attackSlashMaxDistance;
    public float HorizontalSlashMaxDistance => horizontalSlashMaxDistance;
    public float JumpLeapDistance => jumpLeapDistance;
    public float JumpStopOffset => jumpStopOffset;

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
        clothMotion = GetComponentInChildren<RedIgnisClothMotion>(true);
        if (health == null) health = GetComponentInParent<Health>();

        SyncCooldownMax();
        CacheRenderers();
        AutoFindWeaponSocket();
        AutoFindWeaponFlame();
        AutoFindWeaponTrail();
        if (weaponFlame != null)
        {
            if (previewFlameAlwaysOn || isSwordBuffActive) weaponFlame.Ignite();
            else weaponFlame.Extinguish(instant: true);
        }
        if (weaponTrail != null) weaponTrail.SetTrailActive(false);
        EnsureSpawnPoint();
        AutoCalibrateColliderForScale();
        ApplyBossStats();
    }

    private void OnValidate()
    {
        if (attackSlashImpactVfxPrefab == null && slashImpactVfxPrefab != null) attackSlashImpactVfxPrefab = slashImpactVfxPrefab;
        if (horizontalSlashImpactVfxPrefab == null && slashImpactVfxPrefab != null) horizontalSlashImpactVfxPrefab = slashImpactVfxPrefab;
        AutoFindWeaponSocket();
        AutoFindWeaponFlame();
        AutoFindWeaponTrail();
        if (weaponFlame != null)
        {
            if (previewFlameAlwaysOn || isSwordBuffActive) weaponFlame.Ignite();
            else weaponFlame.Extinguish(instant: true);
        }
        if (weaponTrail != null) weaponTrail.SetTrailActive(false);
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
            health.onHealthChanged.AddListener(HandleOnHealthChanged);
            health.onDeath.AddListener(Die);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onTakeDamage.RemoveListener(HandleOnTakeDamage);
            health.onHealthChanged.RemoveListener(HandleOnHealthChanged);
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

    private void AutoFindWeaponFlame()
    {
        if (weaponFlame == null)
        {
            weaponFlame = GetComponentInChildren<IgnisSwordFlame>(true);
        }
    }

    private void AutoFindWeaponTrail()
    {
        if (weaponTrail == null && weaponFlame != null)
        {
            weaponTrail = weaponFlame.GetComponentInChildren<IgnisSwordTrail>(true);
        }
        if (weaponTrail == null && weaponSocket != null)
        {
            weaponTrail = weaponSocket.GetComponentInChildren<IgnisSwordTrail>(true);
        }
        if (weaponTrail == null)
        {
            weaponTrail = GetComponentInChildren<IgnisSwordTrail>(true);
        }
    }

    /// <summary>
    /// Kích hoạt vệt chém kiếm (IgnisSwordTrail) bám theo lưỡi đại kiếm trong khoảng thời gian duration rồi tự tắt
    /// </summary>
    public void TriggerSwordTrail(float duration)
    {
        if (weaponTrail != null)
        {
            weaponTrail.PlayTrailFor(duration);
        }
    }

    /// <summary>
    /// Bật hoặc tắt vệt kiếm trực tiếp
    /// </summary>
    public void SetSwordTrailActive(bool active)
    {
        if (weaponTrail != null)
        {
            weaponTrail.SetTrailActive(active);
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

        // Tự động kiểm tra ngưỡng 50% máu để kích hoạt SwordCastingRI thức tỉnh kiếm lửa
        if (!hasTriggeredSwordCasting && health != null)
        {
            if (health.CurrentHealth <= health.MaxHealth * swordCastingHealthThreshold)
            {
                TriggerSwordCasting();
            }
        }

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

        // 3. Phím thử nghiệm: Trừ 50% máu để kích hoạt động tác SwordCastingRI (Phím H)
        if (keyboard.hKey.wasPressedThisFrame)
        {
            if (health != null)
            {
                float targetHp = health.MaxHealth * swordCastingHealthThreshold;
                float dmgToDeal = health.CurrentHealth - targetHp;
                if (dmgToDeal > 0f)
                {
                    health.TakeDamage(dmgToDeal, transform.position, Vector3.zero);
                    Debug.Log($"<color=yellow>[Test] Đã trừ máu về 50% ({targetHp}/{health.MaxHealth}) để kích hoạt SwordCastingRI!</color>");
                }
                else
                {
                    TriggerSwordCasting();
                }
            }
            else
            {
                TriggerSwordCasting();
            }
        }

        // 4. Phím thử nghiệm: Kích hoạt trực tiếp SwordCastingRI (Phím T)
        if (keyboard.tKey.wasPressedThisFrame)
        {
            TriggerSwordCasting();
        }

        // 5. Phím thử nghiệm: Chết (K)
        if (keyboard.kKey.wasPressedThisFrame) Die();

        // 6. Phím thử nghiệm: Trừ hết Poise để kiểm tra chuỗi Weaken (Phím J)
        if (keyboard.jKey.wasPressedThisFrame)
        {
            TakePoiseDamage(currentPoise > 0f ? currentPoise : maxPoise);
            Debug.Log("<color=yellow>[Test] Đã trừ cạn Poise để kích hoạt Weaken (Quỳ -> Đứng dậy)!</color>");
        }
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
            anim.SetFloat(WalkSpeedHash, 1f);
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
            float verticalSpeed = (currentSmoothMoveInput.y >= 0f) ? moveSpeed : backwardSpeed;
            Vector3 forwardMove = transform.forward * currentSmoothMoveInput.y * verticalSpeed;
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

        // Đồng bộ nhịp bước chân Animator với tốc độ dịch chuyển thực tế theo từng hướng
        float targetWalkSpeed = 1f;
        if (currentActiveMoveType != 0)
        {
            float horizontalSpeed = new Vector2(horizontalMove.x, horizontalMove.z).magnitude;
            float referenceSpeed = currentActiveMoveType switch
            {
                1 => 3.8f, // WalkT_RI
                2 => 2.4f, // WalkBackRI
                _ => 2.2f  // WalkLeftRI / WalkRightRI
            };
            targetWalkSpeed = Mathf.Clamp(horizontalSpeed / referenceSpeed, 0.60f, 1.55f);
        }
        anim.SetFloat(WalkSpeedHash, targetWalkSpeed, locomotionDampTime, dt);
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

    private bool? pendingForceTimingMixup = null;

    public void CastAttack(int attackIndex)
    {
        CastAttack(attackIndex, null);
    }

    public void CastAttack(int attackIndex, bool? forceTimingMixup)
    {
        if (IsBusy()) return;
        if (!CanCastAttack(attackIndex)) return;

        pendingForceTimingMixup = forceTimingMixup;

        lastUsedSkillIndex = attackIndex; // Ghi nhớ chiêu vừa dùng để khóa không cho dùng lặp lại liên tiếp
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

        pendingForceTimingMixup = null;
    }

    /// <summary>
    /// Kiểm tra xem chiêu thức hiện tại có kích hoạt Timing Mixup (giữ nhịp đòn lấy đà) hay không.
    /// Loại trừ hoàn toàn Chiêu 1 (AttackSlash - index 0).
    /// </summary>
    public bool ShouldApplyTimingMixup(int attackIndex)
    {
        // Loại trừ hoàn toàn Chiêu 1 (Attack Slash - index 0) theo yêu cầu!
        if (attackIndex == 0) return false;
        if (!enableTimingMixup) return false;
        if (timingMixupChance <= 0f) return false;

        // Nếu Agent chọn đánh thường tức thì (forceMixup = false)
        if (pendingForceTimingMixup.HasValue && !pendingForceTimingMixup.Value)
        {
            return false;
        }

        // Random ngẫu nhiên lúc có lúc không theo tỷ lệ cài đặt Inspector (mặc định 0.5 = 50%)
        return Random.value <= timingMixupChance;
    }

    /// <summary>
    /// Sinh thời gian khựng giữ nhịp ngẫu nhiên trong khoảng [minHoldDuration, maxHoldDuration] (mặc định 0.5s - 0.8s)
    /// </summary>
    public float GetRandomHoldDuration(int attackIndex)
    {
        if (!ShouldApplyTimingMixup(attackIndex)) return 0f;
        return Random.Range(minHoldDuration, maxHoldDuration);
    }

    /// <summary>
    /// Coroutine hỗ trợ giữ nhịp lấy đà: chạy tự nhiên windupWaitTime rồi khựng lại trong holdTime, sau đó trả tốc độ về 1.0f.
    /// </summary>
    private IEnumerator HoldWindupRoutine(float holdTime)
    {
        if (holdTime <= 0f) yield break;

        // 1. Chờ hoạt ảnh chạy tự nhiên để vung kiếm lên đỉnh lấy đà
        if (windupWaitTime > 0f)
        {
            yield return new WaitForSeconds(windupWaitTime);
        }

        try
        {
            // 2. Khựng lại giữ thế (đóng băng hoặc chạy siêu chậm)
            if (anim != null) anim.speed = holdAnimationSpeed;

            float timer = 0f;
            while (timer < holdTime)
            {
                timer += Time.deltaTime;
                Transform t = GetPlayerTarget();
                if (t != null)
                {
                    SmoothFaceTarget(t, facePlayerSpeed * 1.5f);
                }
                yield return null;
            }
        }
        finally
        {
            // 3. Luôn bảo đảm trả lại tốc độ 1.0f bình thường
            if (anim != null) anim.speed = 1.0f;
        }
    }

    // ==========================================
    // KỸ NĂNG 1: ATTACK SLASH RI (CHÉM DỌC + VFX BAY DỌC)
    // ==========================================
    private IEnumerator AttackSlashRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if ((isSwordBuffActive || previewFlameAlwaysOn) && weaponFlame != null) weaponFlame.Ignite();
        if (anim != null) anim.SetTrigger(AttackSlashHash);

        // 1. NGAY TỪ ĐẦU CHIÊU: BẬT VỆT KIẾM TỨC THÌ (lấy đà & vung kiếm đều có vệt)
        SetSwordTrailActive(true);

        // Xoay nhẹ hướng về player trong giai đoạn lấy đà theo attackSlashDelay
        Transform target = GetPlayerTarget();
        float elapsed = 0f;
        while (elapsed < attackSlashDelay)
        {
            elapsed += Time.deltaTime;
            if (target != null) SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
            yield return null;
        }

        // 2. Phóng đường kiếm bay dọc (+10 sát thương nếu đã thức tỉnh kiếm lửa sau 50% máu)
        GameObject impactToUse = (attackSlashImpactVfxPrefab != null) ? attackSlashImpactVfxPrefab : slashImpactVfxPrefab;
        float finalDmg = attackSlashDamage + (isSwordBuffActive ? swordEnrageBonusDamage : 0f);
        SpawnFlyingSlash(verticalSlashPrefab, attackSlashTiltAngle, finalDmg, attackSlashProjectileSpeed, impactToUse, attackSlashHeightOffset, attackSlashMaxDistance);

        // Giai đoạn vung kiếm bổ chạm đất
        float activeSwingDuration = 0.45f;
        yield return new WaitForSeconds(activeSwingDuration);

        // Dứt đòn bổ: Tắt vệt kiếm (vệt chém tan biến tự nhiên trong 0.16s)
        SetSwordTrailActive(false);

        float remainingDuration = Mathf.Max(0.05f, attackSlashTotalDuration - attackSlashDelay - activeSwingDuration);
        yield return new WaitForSeconds(remainingDuration);

        FinishAttack();
    }

    // ==========================================
    // KỸ NĂNG 2: HORIZONTAL SLASH RI (CHÉM NGANG + VFX BAY NGANG)
    // ==========================================
    private IEnumerator HorizontalSlashRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if ((isSwordBuffActive || previewFlameAlwaysOn) && weaponFlame != null) weaponFlame.Ignite();
        if (anim != null) anim.SetTrigger(HorizontalSlashHash);

        // 1. NGAY TỪ ĐẦU CHIÊU: BẬT VỆT KIẾM TỨC THÌ
        SetSwordTrailActive(true);

        // Timing Mixup (Khựng lấy đà 0.5s - 0.8s cho Chiêu 2)
        float holdTime = GetRandomHoldDuration(1);
        if (holdTime > 0f)
        {
            yield return StartCoroutine(HoldWindupRoutine(holdTime));
        }

        Transform target = GetPlayerTarget();
        float elapsed = 0f;
        float remainingDelay = Mathf.Max(0f, horizontalSlashDelay - (holdTime > 0f ? windupWaitTime : 0f));
        while (elapsed < remainingDelay)
        {
            elapsed += Time.deltaTime;
            if (target != null) SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
            yield return null;
        }

        // 2. Phóng đường kiếm bay ngang (+10 sát thương nếu đã thức tỉnh kiếm lửa sau 50% máu)
        GameObject impactToUse = (horizontalSlashImpactVfxPrefab != null) ? horizontalSlashImpactVfxPrefab : slashImpactVfxPrefab;
        float finalDmg = horizontalSlashDamage + (isSwordBuffActive ? swordEnrageBonusDamage : 0f);
        SpawnFlyingSlash(horizontalSlashPrefab, horizontalSlashTiltAngle, finalDmg, horizontalSlashProjectileSpeed, impactToUse, horizontalSlashHeightOffset, horizontalSlashMaxDistance);

        float activeSwingDuration = 0.45f;
        yield return new WaitForSeconds(activeSwingDuration);

        // Dứt đòn quét ngang: Tắt vệt kiếm
        SetSwordTrailActive(false);

        float remainingDuration = Mathf.Max(0.05f, horizontalSlashTotalDuration - horizontalSlashDelay - activeSwingDuration);
        yield return new WaitForSeconds(remainingDuration);

        FinishAttack();
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

    private void SpawnFlyingSlash(GameObject projectilePrefab, float tiltAngle, float slashDmg, float projSpeed, GameObject impactPrefab, float heightOffset = 0f, float maxDistance = 0f)
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
            slash.Launch(transform, fireDirection, projSpeed, tiltAngle, maxDistance);
        }
    }

    private float ComputeJumpDistanceToTarget(float targetDist)
    {
        float stopOffset = Mathf.Max(0.5f, jumpStopOffset);
        float maxLeap = Mathf.Max(0f, jumpLeapDistance);
        return (targetDist > stopOffset) ? Mathf.Min(targetDist - stopOffset, maxLeap) : 0f;
    }

    // ==========================================
    // KỸ NĂNG 3: JUMP ATTACK RI (NHẢY TỚI DẬM ĐẤT CHÉM NGANG)
    // ==========================================
    private IEnumerator JumpAttackRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if ((isSwordBuffActive || previewFlameAlwaysOn) && weaponFlame != null) weaponFlame.Ignite();
        if (anim != null) anim.SetTrigger(JumpAttackHash);

        // 1. NGAY TỪ ĐẦU CHIÊU NHẢY: BẬT VỆT KIẾM TỨC THÌ
        SetSwordTrailActive(true);

        // Timing Mixup (Khựng thế nhún chân trên sàn 0.5s - 0.8s cho Chiêu 3 trước khi bật nhảy)
        float jumpHold = GetRandomHoldDuration(2);
        if (jumpHold > 0f)
        {
            yield return StartCoroutine(HoldWindupRoutine(jumpHold));
        }

        // Giai đoạn nhún người lấy đà (Windup): Boss liên tục xoay mặt theo sát hướng Player
        float windupTimer = 0f;
        float remainingWindup = Mathf.Max(0f, jumpAttackWindupDelay - (jumpHold > 0f ? windupWaitTime : 0f));
        while (windupTimer < remainingWindup)
        {
            windupTimer += Time.deltaTime;
            Transform p = GetPlayerTarget();
            if (p != null) SmoothFaceTarget(p, 500f);
            yield return null;
        }

        // 2. KHOẢNH KHẮC BẬT NHẢY RỜI SÀN:
        // Đo lường chính xác vị trí thời gian thực của Player để bay thẳng tới ngay trước mặt theo jumpStopOffset
        float stopOffset = Mathf.Max(0.5f, jumpStopOffset);
        float maxLeap = Mathf.Max(0f, jumpLeapDistance);
        Vector3 startPos = transform.position;
        Vector3 targetLandingPos = startPos + transform.forward * maxLeap;

        Transform liveTarget = GetPlayerTarget();
        if (liveTarget != null)
        {
            Vector3 toTarget = (liveTarget.position - startPos);
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            float leapDist = ComputeJumpDistanceToTarget(dist);
            if (dist > 0.05f)
            {
                targetLandingPos = startPos + toTarget.normalized * leapDist;
                SmoothFaceTarget(liveTarget, 1000f);
            }
        }

        // Lấy cao độ mặt sàn thực tế tại điểm rơi và điểm xuất phát
        targetLandingPos = GetGroundPosition(targetLandingPos);
        startPos = GetGroundPosition(startPos);

        // Tạm thời tắt CharacterController để chuyển động parabol bay mượt mà, không va cấn Player trên không
        if (characterController != null) characterController.enabled = false;

        // Tính thời gian bay theo tốc độ jumpLeapSpeed (m/s) và tối thiểu jumpAttackLeapDuration để bay đầm hơn, dễ quan sát né
        float horizontalFlightDist = Vector2.Distance(
            new Vector2(startPos.x, startPos.z),
            new Vector2(targetLandingPos.x, targetLandingPos.z));
        float speedDuration = (jumpLeapSpeed > 0.1f) ? (horizontalFlightDist / jumpLeapSpeed) : jumpAttackLeapDuration;
        float leapDuration = Mathf.Max(0.1f, jumpAttackLeapDuration, speedDuration);
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

        // Điểm nổ dậm kiếm nằm ngay trước mặt Boss theo jumpStopOffset (ngay vị trí lưỡi kiếm cắm xuống mặt Player)
        Vector3 slamPoint = transform.position + transform.forward * stopOffset + Vector3.up * jumpImpactHeightOffset;

        // Spawn VFX dậm đất nổ lan
        if (jumpImpactVfxPrefab != null)
        {
            Instantiate(jumpImpactVfxPrefab, slamPoint, Quaternion.identity);
        }

        // Gây sát thương AoE diện rộng (+10 sát thương nếu đã thức tỉnh kiếm lửa sau 50% máu) và đẩy lùi (Knockback 4.0m)
        float finalJumpDmg = jumpAttackDamage + (isSwordBuffActive ? swordEnrageBonusDamage : 0f);
        DealAreaDamage(slamPoint, jumpAttackRadius, finalJumpDmg, 4.0f);

        // Sau khi hoàn thành cú dậm chém đất: Tắt vệt kiếm (vệt tự động fade tan mượt)
        SetSwordTrailActive(false);

        yield return new WaitForSeconds(jumpAttackRecoveryDuration);

        FinishAttack();
    }

    // ==========================================
    // KỸ NĂNG 4: COMBO ATTACK RI (CHÉM LIÊN TỤC 4 NHÁT)
    // ==========================================
    private IEnumerator ComboAttackRoutine()
    {
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        if ((isSwordBuffActive || previewFlameAlwaysOn) && weaponFlame != null) weaponFlame.Ignite();

        // 1. NGAY LẬP TỨC XOAY THẲNG MẶT VỀ PHÍA PLAYER
        Transform target = GetPlayerTarget();
        if (target != null)
        {
            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(toTarget);
            }
        }

        if (anim != null) anim.SetTrigger(ComboAttackHash);

        // 2. NGAY TỪ ĐẦU CHUỖI COMBO: BẬT VỆT KIẾM TỨC THÌ CHO TOÀN BỘ CHUỖI VUNG CHÉM
        SetSwordTrailActive(true);

        // Timing Mixup (Khựng nhát chém đầu tiên 0.5s - 0.8s cho Chiêu 4)
        float comboHold = GetRandomHoldDuration(3);
        if (comboHold > 0f)
        {
            yield return StartCoroutine(HoldWindupRoutine(comboHold));
        }

        if (comboHitTimers == null || comboHitTimers.Length == 0)
        {
            comboHitTimers = new float[] { 0.40f, 0.95f, 1.55f, 2.20f };
        }

        float lungePerHit = (comboLungeDistance > 0.01f) ? comboLungeDistance : 0.55f;

        float lastTime = 0f;
        for (int hit = 0; hit < comboHitTimers.Length; hit++)
        {
            float targetTime = comboHitTimers[hit];
            float waitDelta = (hit == 0 && comboHold > 0f)
                ? Mathf.Max(0.01f, targetTime - windupWaitTime)
                : Mathf.Max(0.01f, targetTime - lastTime);
            lastTime = targetTime;

            // Vừa xoay mặt bám theo Player vừa bước lướt tới trước mượt mà theo từng nhát chém
            float elapsed = 0f;
            float lungeSpeed = lungePerHit / waitDelta;
            while (elapsed < waitDelta)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                Transform p = GetPlayerTarget();
                if (p != null) SmoothFaceTarget(p, 180f);

                // Di chuyển vị trí thật của Boss tới trước bằng CharacterController (không xuyên tường/người)
                Vector3 moveVelocity = transform.forward * lungeSpeed;
                moveVelocity.y = groundedGravity;
                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(moveVelocity * dt);
                }
                else
                {
                    transform.position = GetGroundPosition(transform.position + moveVelocity * dt);
                }

                yield return null;
            }

            // Gây sát thương nhát chém quét trước mặt (+10 sát thương nếu đã thức tỉnh kiếm lửa sau 50% máu)
            float finalComboDmg = comboHitDamage + (isSwordBuffActive ? swordEnrageBonusDamage : 0f);
            DealAreaDamage(transform.position + transform.forward * 1.5f + Vector3.up * 1.0f, comboHitRadius, finalComboDmg, 1.2f);
        }

        // Dứt toàn bộ 4 nhát chém combo: Tắt vệt kiếm
        SetSwordTrailActive(false);

        // Chờ hoàn tất trọn vẹn clip ComboAttackRI (3.63s) để chuyển tiếp mượt mà vào IdleRI mà không bị giật
        float remainingDuration = Mathf.Max(comboRecoveryDuration, 3.633f - lastTime);
        yield return new WaitForSeconds(remainingDuration);

        FinishAttack();
    }

    /// <summary>
    /// Kết thúc chuỗi đòn tấn công và kích hoạt Nhịp nghỉ Combat Pacing (3.5s)
    /// </summary>
    private void FinishAttack()
    {
        isAttacking = false;
        if (!isSwordBuffActive && !previewFlameAlwaysOn && weaponFlame != null) weaponFlame.Extinguish();
        currentAttackRoutine = null;
        nextAttackAllowedTime = Time.time + attackPacingInterval;
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

            bool isPlayer = hit.CompareTag("Player") ||
                            (root != null && root.CompareTag("Player")) ||
                            hit.GetComponentInParent<PlayerController>() != null ||
                            (LayerMask.NameToLayer("Player") != -1 && (hit.gameObject.layer == LayerMask.NameToLayer("Player") || (root != null && root.gameObject.layer == LayerMask.NameToLayer("Player"))));

            if (isPlayer)
            {
                hitRoots.Add(root);
                Health h = hit.GetComponentInParent<Health>();
                if (h == null && root != null) h = root.GetComponentInChildren<Health>();

                if (h != null)
                {
                    Vector3 knockDir = (hit.transform.position - transform.position);
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude < 0.001f) knockDir = transform.forward;
                    knockDir.Normalize();

                    h.TakeDamage(dmg, hit.ClosestPoint(center), knockDir, knock, transform);
                    Debug.Log($"<color=orange>[Red Ignis Damage]</color> Trúng đòn gây {dmg} sát thương{(isSwordBuffActive ? " (+10 Buff Lửa)" : "")}!");
                }
            }
        }
    }

    // ==========================================
    // POISE & WEAKEN (SỨC BỀN & CHOÁNG)
    // ==========================================
    public void TakePoiseDamage(float poiseDamage)
    {
        if (isDead || isCastingSword) return;

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
        SetSwordTrailActive(false);
        if (!isSwordBuffActive && weaponFlame != null) weaponFlame.Extinguish(instant: true);
        if (currentAttackRoutine != null)
        {
            StopCoroutine(currentAttackRoutine);
            currentAttackRoutine = null;
        }

        int frontHash = Animator.StringToHash("WeakenFrontRI");
        int backHash = Animator.StringToHash("WeakenBackRI");

        if (anim != null)
        {
            anim.ResetTrigger(WeakenHash);
            anim.SetTrigger(WeakenHash);
            anim.SetFloat(SpeedHash, 0f);
            anim.SetInteger(MoveTypeHash, 0);
        }

        Debug.Log("<color=yellow><b>[Red Ignis] POISE VỀ 0! VỠ THẾ (STAGGER)! Đang quỳ xuống (WeakenFrontRI) rồi đứng dậy (WeakenBackRI) tự động...</b></color>");

        // Khóa hoàn toàn di chuyển và trượt chân trong thời gian suy yếu
        desiredMoveInput = Vector2.zero;
        currentSmoothMoveInput = Vector2.zero;
        currentSmoothSpeed = 0f;

        if (anim != null)
        {
            // 1. Chờ Animator bắt đầu vào state WeakenFrontRI (tối đa 0.6s)
            float enterTimeout = 0.6f;
            while (enterTimeout > 0f && !isDead)
            {
                AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == frontHash || info.IsName("WeakenFrontRI"))
                {
                    break;
                }
                enterTimeout -= Time.deltaTime;
                Vector3 gPos = GetGroundPosition(transform.position);
                transform.position = new Vector3(transform.position.x, gPos.y, transform.position.z);
                yield return null;
            }

            // Xóa ngay trigger Weaken để AnyState không bao giờ bị kích hoạt lặp
            anim.ResetTrigger(WeakenHash);

            // 2. Chờ Animator hoàn tất WeakenFrontRI và CHUYỂN SANG WeakenBackRI (tối đa 6.0s)
            float frontTimeout = 6.0f;
            while (frontTimeout > 0f && !isDead)
            {
                Vector3 gPos = GetGroundPosition(transform.position);
                transform.position = new Vector3(transform.position.x, gPos.y, transform.position.z);

                AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
                // Đã chính thức vào WeakenBackRI
                if (info.shortNameHash == backHash || info.IsName("WeakenBackRI"))
                {
                    break;
                }

                frontTimeout -= Time.deltaTime;
                yield return null;
            }

            // 3. Khi đã ở trong WeakenBackRI, chờ cho đến khi động tác đứng dậy hoàn tất (tối đa 6.0s)
            float backTimeout = 6.0f;
            float minStandUpTime = 0.3f; // Chờ ít nhất 0.3s để tránh thoát nhầm khi đang transition
            while (backTimeout > 0f && !isDead)
            {
                Vector3 gPos = GetGroundPosition(transform.position);
                transform.position = new Vector3(transform.position.x, gPos.y, transform.position.z);

                minStandUpTime -= Time.deltaTime;
                AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

                if (info.shortNameHash == backHash || info.IsName("WeakenBackRI"))
                {
                    // Khi Boss đã đứng thẳng lên (normalizedTime >= 0.70f), bắt đầu xoay mặt mượt mà về phía Player
                    // để khi kết thúc động tác và vào IdleRI hoàn toàn không bị giật xoay đột ngột
                    if (info.normalizedTime >= 0.70f && alwaysFacePlayer)
                    {
                        Transform target = GetPlayerTarget();
                        if (target != null)
                        {
                            SmoothFaceTarget(target, facePlayerSpeed * 0.75f);
                        }
                    }

                    // Khi WeakenBackRI đã hoàn tất 100% động tác đứng dậy
                    if (info.normalizedTime >= 1.0f)
                    {
                        break;
                    }
                }
                else if (minStandUpTime <= 0f)
                {
                    // Đã thoát hẳn sang state khác (ví dụ IdleRI) sau khi đứng dậy
                    break;
                }

                backTimeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Dự phòng khi không có Animator
            float totalDuration = weakenFrontDuration + weakenBackDuration;
            float timer = 0f;
            while (timer < totalDuration && !isDead)
            {
                timer += Time.deltaTime;
                Vector3 gPos = GetGroundPosition(transform.position);
                transform.position = new Vector3(transform.position.x, gPos.y, transform.position.z);
                yield return null;
            }
        }

        currentPoise = maxPoise;
        isWeakened = false;
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);
        weakenRoutine = null;
        Debug.Log("<color=green>[Red Ignis] Đã hoàn tất Weaken và đứng dậy! Hồi đầy Poise (" + maxPoise + "/" + maxPoise + "), sẵn sàng tiếp tục chiến đấu ngay lập tức.</color>");
    }

    // ==========================================
    // GIAI ĐOẠN 50% MÁU: SWORD CASTING RI (NIỆM KIẾM LỬA & BUFF SÁT THƯƠNG)
    // ==========================================
    private void HandleOnHealthChanged(float cur, float max)
    {
        if (isDead || hasTriggeredSwordCasting) return;
        if (max > 0f && (cur / max) <= swordCastingHealthThreshold)
        {
            TriggerSwordCasting();
        }
    }

    /// <summary>
    /// Kích hoạt động tác niệm kiếm (SwordCastingRI) khi boss về dưới 50% máu:
    /// Boss đứng vào tư thế niệm kiếm, thanh kiếm bốc lửa rực rỡ và nhận buff +10 sát thương vĩnh viễn.
    /// </summary>
    public void TriggerSwordCasting()
    {
        if (hasTriggeredSwordCasting || isDead) return;
        hasTriggeredSwordCasting = true;

        if (swordCastingRoutine != null) StopCoroutine(swordCastingRoutine);
        swordCastingRoutine = StartCoroutine(SwordCastingRoutine());
    }

    private IEnumerator SwordCastingRoutine()
    {
        isCastingSword = true;
        isAttacking = true;
        desiredMoveInput = Vector2.zero;
        currentSmoothMoveInput = Vector2.zero;
        currentSmoothSpeed = 0f;
        currentActiveMoveType = 0;

        // Dừng đòn tấn công hoặc trạng thái suy yếu nếu đang diễn ra
        if (currentAttackRoutine != null)
        {
            StopCoroutine(currentAttackRoutine);
            currentAttackRoutine = null;
        }
        if (weakenRoutine != null)
        {
            StopCoroutine(weakenRoutine);
            weakenRoutine = null;
            isWeakened = false;
        }

        // Hồi phục lại toàn bộ Poise
        currentPoise = maxPoise;
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);

        // Kích hoạt hoạt ảnh SwordCastingRI
        if (anim != null)
        {
            anim.SetFloat(SpeedHash, 0f);
            anim.SetInteger(MoveTypeHash, 0);
            anim.SetTrigger(SwordCastingHash);
        }

        Debug.Log("<color=red><b>[Red Ignis] MÁU DƯỚI 50%! THI TRIỂN ĐỘNG TÁC SwordCastingRI - THỨC TỈNH NGỌN LỬA KIẾM!</b></color>");

        // Hướng mặt nhẹ về phía Player khi bắt đầu niệm kiếm
        Transform target = GetPlayerTarget();
        if (target != null)
        {
            SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
        }

        // Chờ đến thời điểm bốc cháy ngọn lửa
        float elapsed = 0f;
        while (elapsed < swordCastingIgniteDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // BỐC CHÁY NGỌN LỬA VÀ KÍCH HOẠT BUFF +10 SÁT THƯƠNG VĨNH VIỄN
        isSwordBuffActive = true;
        if (weaponFlame != null)
        {
            weaponFlame.Ignite();
            Debug.Log("<color=orange><b>[Red Ignis] THANH KIẾM BỐC LỬA CUỒN CUỘN! Từ nay TẤT CẢ các đợt gây sát thương của Boss tăng thêm +10 sát thương!</b></color>");
        }

        // Chờ hoàn tất thời lượng động tác
        float remaining = Mathf.Max(0.1f, swordCastingTotalDuration - elapsed);
        yield return new WaitForSeconds(remaining);

        isCastingSword = false;
        isAttacking = false;
        swordCastingRoutine = null;
        Debug.Log("<color=green>[Red Ignis] Đã hoàn tất SwordCastingRI! Tiếp tục chiến đấu với thanh kiếm rực lửa và +10 sát thương.</color>");
    }

    // ==========================================
    // SÁT THƯƠNG & CHỚP ĐỎ
    // ==========================================
    private void HandleOnTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        if (isDead) return;

        // 1. Tính sát thương Poise theo lực đòn đánh của Player (chuẩn combat Souls-like)
        // - Heavy / Counter (knockback >= 1.8f): Counter Attack (2.0f), Phép nổ Q AoeSpell (2.5f) -> 100 Poise
        // - Medium-Heavy (knockback >= 1.0f): Charge Hit 2 (1.2f), Sóng kiếm E (1.0f) -> 70 Poise
        // - Medium (knockback > 0f): Attack 3 (0.8f), Charge Hit 1 (0.6f) -> 50 Poise
        // - Light (knockback == 0f): Attack 1 & Attack 2 -> 35 Poise
        float poiseDmg = 35f;
        if (knockback >= 1.8f) poiseDmg = 100f;
        else if (knockback >= 1.0f) poiseDmg = 70f;
        else if (knockback > 0f) poiseDmg = 50f;

        TakePoiseDamage(poiseDmg);
        TriggerHitFlash();

        if (!hasTriggeredSwordCasting && health != null)
        {
            if (health.CurrentHealth <= health.MaxHealth * swordCastingHealthThreshold)
            {
                TriggerSwordCasting();
            }
        }
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
        isCastingSword = false;
        if (swordCastingRoutine != null) StopCoroutine(swordCastingRoutine);
        if (weaponFlame != null) weaponFlame.Extinguish(instant: true);
        SetSwordTrailActive(false);
        if (weaponTrail != null) weaponTrail.ClearTrail();

        if (currentAttackRoutine != null) StopCoroutine(currentAttackRoutine);
        if (weakenRoutine != null) StopCoroutine(weakenRoutine);

        if (anim != null) anim.SetTrigger(DeadHash);

        if (characterController != null) characterController.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        OnBossDefeated?.Invoke();
    }

    /// <summary>
    /// Hồi sinh Boss để bắt đầu hiệp đấu mới (Reset trạng thái chết, bật lại Collider/CharacterController, reset Animation và Cooldowns)
    /// </summary>
    public void Revive()
    {
        isDead = false;
        isAttacking = false;
        isWeakened = false;
        isCastingSword = false;
        hasTriggeredSwordCasting = false;
        isSwordBuffActive = false;

        if (currentAttackRoutine != null)
        {
            StopCoroutine(currentAttackRoutine);
            currentAttackRoutine = null;
        }
        if (weakenRoutine != null)
        {
            StopCoroutine(weakenRoutine);
            weakenRoutine = null;
        }
        if (swordCastingRoutine != null)
        {
            StopCoroutine(swordCastingRoutine);
            swordCastingRoutine = null;
        }

        if (weaponFlame != null) weaponFlame.Extinguish(instant: true);
        SetSwordTrailActive(false);
        if (weaponTrail != null) weaponTrail.ClearTrail();

        // Khôi phục Collider & CharacterController
        if (characterController != null) characterController.enabled = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // Reset Animator
        if (anim != null)
        {
            anim.ResetTrigger(DeadHash);
            anim.ResetTrigger(WeakenHash);
            anim.ResetTrigger(SwordCastingHash);
            anim.ResetTrigger(AttackSlashHash);
            anim.ResetTrigger(HorizontalSlashHash);
            anim.ResetTrigger(JumpAttackHash);
            anim.ResetTrigger(ComboAttackHash);
            anim.SetFloat(SpeedHash, 0f);
            anim.SetFloat(WalkSpeedHash, 1f);
            anim.SetInteger(MoveTypeHash, 0);
            anim.Play("IdleRI", 0, 0f);
        }

        // Reset Verlet cloth history để tránh giật bung áo khi hồi sinh / dịch chuyển
        clothMotion?.ResetClothPose();

        // Hồi phục Máu và Poise
        ApplyBossStats();
        if (health != null) health.Revive(bossMaxHealth);

        ResetAllCooldowns();
        CleanupResidualProjectiles();
    }

    /// <summary>
    /// Đặt lại toàn bộ thời gian hồi chiêu và nhịp nghỉ về 0 cho hiệp đấu mới
    /// </summary>
    public void ResetAllCooldowns()
    {
        if (skillCooldownTimers != null)
        {
            for (int i = 0; i < skillCooldownTimers.Length; i++)
            {
                skillCooldownTimers[i] = 0f;
            }
        }
        nextAttackAllowedTime = 0f;
        lastUsedSkillIndex = -1; // Reset trạng thái chống lặp chiêu cho hiệp mới
    }

    /// <summary>
    /// Dọn sạch các projectile kiếm bay còn tồn đọng trên sàn đấu
    /// </summary>
    public void CleanupResidualProjectiles()
    {
        RedIgnisSlashProjectile[] projectiles = FindObjectsByType<RedIgnisSlashProjectile>(FindObjectsSortMode.None);
        foreach (var p in projectiles)
        {
            if (p != null) Destroy(p.gameObject);
        }
    }

    public bool IsPhase2 => isSwordBuffActive;

    // ==========================================
    // TIỆN ÍCH & TRỢ GIÚP
    // ==========================================
    public bool IsBusy()
    {
        return isAttacking || isWeakened || isDead || isCastingSword;
    }

    public bool CanCastAttack(int index)
    {
        if (index < 0 || index >= skillCooldownTimers.Length) return false;
        // 1. Kiểm tra nhịp nghỉ Combat Pacing (5.0s giữa các lần tung chiêu)
        if (Time.time < nextAttackAllowedTime) return false;
        // 2. Chống lặp chiêu: Tuyệt đối không được dùng cùng 1 chiêu 2 lần liên tiếp
        if (index == lastUsedSkillIndex) return false;
        // 3. Kiểm tra Cooldown riêng của chiêu
        return skillCooldownTimers[index] <= 0.01f;
    }

    public float GetCooldownNormalized(int index)
    {
        if (index < 0 || index >= skillCooldownTimers.Length) return 0f;
        // Nếu vừa dùng chiêu này -> Bị khóa chống lặp chiêu liên tiếp (trả về 1.0f cho Observation)
        if (index == lastUsedSkillIndex) return 1.0f;
        if (skillCooldownMax[index] <= 0f) return 0f;
        float skillCdNorm = Mathf.Clamp01(skillCooldownTimers[index] / skillCooldownMax[index]);
        float pacingRemaining = Mathf.Max(0f, nextAttackAllowedTime - Time.time);
        float pacingNorm = (attackPacingInterval > 0f) ? Mathf.Clamp01(pacingRemaining / attackPacingInterval) : 0f;
        return Mathf.Max(skillCdNorm, pacingNorm);
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

    // ==========================================
    // GIZMOS HIỂN THỊ PHẠM VI SKILL (SCENE VIEW)
    // ==========================================

    /// <summary>
    /// Vẽ vòng tròn phẳng trên mặt đất XZ trong Scene View để dễ quan sát phạm vi chiêu và cự ly AI.
    /// </summary>
    public static void DrawGroundCircle(Vector3 center, float radius, int segments = 48)
    {
        if (radius <= 0.01f || segments < 3) return;
        float step = (Mathf.PI * 2f) / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawFlyingSlashGizmo(Vector3 baseSpawnPos, Vector3 fireDir, float heightOffset, float maxRange, float widthSpan, Color color)
    {
        Vector3 spawnPos = baseSpawnPos + Vector3.up * heightOffset;
        Vector3 endPos = spawnPos + fireDir * maxRange;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(spawnPos, 0.18f);
        Gizmos.DrawLine(spawnPos, endPos);
        Gizmos.DrawWireSphere(endPos, 0.35f);

        if (widthSpan > 0.01f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, fireDir).normalized * (widthSpan * 0.5f);
            Gizmos.DrawLine(spawnPos - right, spawnPos + right);
            Gizmos.DrawLine(endPos - right, endPos + right);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position;
        Vector3 groundCenter = start + Vector3.up * 0.08f;

        // 0. Đường ngắm khóa mục tiêu Player (Màu Tím Hồng / Magenta - chỉ đọc an toàn trong Edit Mode)
        Transform target = playerTarget;
        if (target == null && autoFindPlayer)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) target = pObj.transform;
        }

        if (target != null)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.8f, 0.9f);
            Gizmos.DrawLine(start + Vector3.up * 1.5f, target.position + Vector3.up * 1.0f);
            Gizmos.DrawWireSphere(target.position + Vector3.up * 1.0f, 0.45f);
        }

        // 1. Kỹ năng 1 & 2: Sóng kiếm dọc (Vàng Cam) & Sóng kiếm ngang (Cam Đỏ)
        Vector3 baseSpawnPos = (slashSpawnPoint != null)
            ? slashSpawnPoint.position
            : start + transform.forward * 1.4f + Vector3.up * 1.5f;

        Vector3 fireDir = transform.forward;
        if (target != null)
        {
            Vector3 toTarget = target.position - baseSpawnPos;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.05f) fireDir = toTarget.normalized;
        }

        DrawFlyingSlashGizmo(baseSpawnPos, fireDir, attackSlashHeightOffset, Mathf.Max(0f, attackSlashMaxDistance), 0f, new Color(1f, 0.55f, 0f, 0.9f));
        DrawFlyingSlashGizmo(baseSpawnPos, fireDir, horizontalSlashHeightOffset, Mathf.Max(0f, horizontalSlashMaxDistance), 2.5f, new Color(1f, 0.25f, 0.1f, 0.9f));

        // 2. Kỹ năng 3: JumpAttack (Đường nhảy Vàng & Vòng nổ AoE Đỏ)
        float maxLeap = Mathf.Max(0f, jumpLeapDistance);
        float stopOffset = Mathf.Max(0.5f, jumpStopOffset);

        Gizmos.color = Color.yellow;
        Vector3 jumpLandingMax = start + transform.forward * maxLeap;
        Gizmos.DrawLine(groundCenter, jumpLandingMax + Vector3.up * 0.08f);
        Gizmos.DrawWireSphere(jumpLandingMax, 0.45f);

        Vector3 slamMax = jumpLandingMax + transform.forward * stopOffset + Vector3.up * jumpImpactHeightOffset;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(slamMax, jumpAttackRadius);
        DrawGroundCircle(new Vector3(slamMax.x, groundCenter.y, slamMax.z), jumpAttackRadius);

        if (target != null)
        {
            Vector3 toTarget = target.position - start;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            Vector3 dir = (dist > 0.05f) ? toTarget / dist : transform.forward;
            float leapDist = ComputeJumpDistanceToTarget(dist);
            Vector3 liveLanding = start + dir * leapDist;
            Vector3 liveSlam = liveLanding + dir * stopOffset + Vector3.up * jumpImpactHeightOffset;

            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.85f);
            Gizmos.DrawLine(start + Vector3.up * 0.15f, liveLanding + Vector3.up * 0.15f);
            Gizmos.DrawWireSphere(liveSlam, jumpAttackRadius);
            DrawGroundCircle(new Vector3(liveSlam.x, groundCenter.y, liveSlam.z), jumpAttackRadius);
        }

        // 3. Kỹ năng 4: ComboAttack 4 nhát (Quãng lướt & Hitbox màu Xanh Cyan)
        float lungePerHit = (comboLungeDistance > 0.01f) ? comboLungeDistance : 0.55f;
        int hitCount = (comboHitTimers != null && comboHitTimers.Length > 0) ? comboHitTimers.Length : 4;
        Vector3 comboTotalEnd = start + transform.forward * (lungePerHit * hitCount);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(groundCenter, comboTotalEnd + Vector3.up * 0.08f);

        Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.45f);
        for (int i = 0; i < hitCount; i++)
        {
            Vector3 hitCenter = start + transform.forward * (lungePerHit * (i + 1) + 1.5f) + Vector3.up * 1.0f;
            Gizmos.DrawWireSphere(hitCenter, comboHitRadius);
        }
    }
}
