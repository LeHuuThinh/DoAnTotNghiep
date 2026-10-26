using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BoneForwardAxis
{
    AutoDetect = 0,
    ForwardZ = 1,
    BackNegativeZ = 2,
    UpY = 3,
    DownNegativeY = 4,
    RightX = 5,
    LeftNegativeX = 6,
    BossForward = 7
}

public class IceGolemController : MonoBehaviour
{
    [Header("=== THAM CHIẾU & DI CHUYỂN ===")]
    [Tooltip("Kéo đối tượng con IceGolemModel vào đây nếu script không tự tìm thấy")]
    public Animator anim;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 120f;
    public bool strafeMode = false;

    [Header("=== MỤC TIÊU ĐỐI THỦ (PLAYER TARGET TRACKING) ===")]
    [Tooltip("Transform của đối thủ Player. Nếu để trống, Boss sẽ TỰ ĐỘNG tìm Player trong Scene (theo Tag 'Player', component PlayerController, hoặc tên GameObject 'Player')")]
    [SerializeField] private Transform playerTarget;
    [Tooltip("Tự động tìm kiếm Player khi bắt đầu hoặc khi mục tiêu bị mất/chưa gán")]
    [SerializeField] private bool autoFindPlayer = true;
    [Tooltip("Boss luôn tự động quay mặt hướng về phía Player khi đứng yên (Idle) hoặc di chuyển")]
    [SerializeField] private bool alwaysFacePlayer = true;
    [Tooltip("Tốc độ xoay mặt hướng về Player khi đứng yên/di chuyển (độ/giây, mặc định 240)")]
    [SerializeField] private float facePlayerSpeed = 240f;
    [Tooltip("Tự động xoay người chuẩn xác hướng thẳng về Player ngay khi bấm tung bất kỳ chiêu nào (Attack 1, 2, 3, JumpAttack, JumpDown)")]
    [SerializeField] private bool facePlayerOnAttack = true;
    [Tooltip("Khoảng cách tối thiểu để xoay mặt (tránh giật hình khi Player đứng quá sát tâm Boss)")]
    [SerializeField] private float minFaceDistance = 0.5f;

    [Header("=== CÀI ĐẶT JUMP DOWN (ĐÁP XUỐNG ĐẦU GAME) ===")]
    [Tooltip("Tự động kích hoạt cú nhảy đáp xuống khi vừa bắt đầu game (Start)")]
    [SerializeField] private bool jumpDownOnStart = true;
    [Tooltip("Khoảng cách bay tới (mét) khi JumpDown (hoạt động giống như JumpAttack)")]
    [SerializeField] private float jumpDownDistance = 10f;
    [Tooltip("Độ cao xuất phát trên không của Boss trước khi lao dốc xuống (mét)")]
    [SerializeField] private float jumpDownHeight = 12f;
    [Tooltip("Độ cao vòng cung bổ sung nếu có (mét)")]
    [SerializeField] private float jumpDownArcHeight = 0f;
    [Tooltip("Khoảng trễ trước khi bắt đầu rơi (giây)")]
    [SerializeField] private float jumpDownStartDelay = 0.1f;
    [Tooltip("Thời gian rơi từ trên cao xuống chạm đất (giây, khớp với lúc hai tay/chân đập sàn trong animation)")]
    [SerializeField] private float jumpDownFallDuration = 0.95f;
    [Tooltip("Mục tiêu để Boss đáp tới (Player). Nếu để trống thì bay thẳng theo hướng mặt")]
    [SerializeField] private Transform jumpDownTarget;
    [Tooltip("Tự động tìm Player làm mục tiêu tiếp đất")]
    [SerializeField] private bool autoTargetPlayerOnJumpDown = true;
    [Tooltip("Đường cong gia tốc rơi (EaseIn giúp rơi tăng tốc dần tạo cảm giác nặng nề)")]
    [SerializeField] private AnimationCurve jumpDownFallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("JumpDown Shockwave Ring (Vòng Xoáy Tiếp Đất)")]
    [Tooltip("ĐỘ TRỄ của vòng xoáy xuất hiện & gây sát thương sau khi tiếp đất (giây). Chỉnh số này để vòng sóng phát nổ chuẩn xác đúng khoảnh khắc hai tay/chân giáng xuống sàn trong animation (ví dụ: 0 = nổ ngay khi chạm đất, 0.15 = chờ 0.15s sau khi chạm đất mới nổ sóng)")]
    [SerializeField] private float jumpDownShockwaveDelay = 0f;
    [Tooltip("ĐỘ RỘNG tối đa của vòng xoáy khi JumpDown tiếp đất (mét)")]
    [SerializeField] private float jumpDownShockwaveRadius = 12f;
    [Tooltip("TỐC ĐỘ LAN RA của vòng xoáy JumpDown (mét/giây) - Chỉnh số này để lan nhanh hay chậm")]
    [SerializeField] private float jumpDownShockwaveSpeed = 14f;
    [Tooltip("Sát thương của vòng xoáy JumpDown")]
    [SerializeField] private float jumpDownShockwaveDamage = 35f;
    [Tooltip("Lực đẩy văng của vòng xoáy JumpDown")]
    [SerializeField] private float jumpDownShockwaveKnockback = 3.0f;
    [Tooltip("Màu sắc vòng xoáy JumpDown")]
    [SerializeField] private Color jumpDownShockwaveColor = new Color(0.2f, 0.9f, 1f, 0.95f);

    [Header("=== CÀI ĐẶT JUMP ATTACK (NHẢY TẤN CÔNG BAY TỚI) ===")]
    [Tooltip("Khoảng cách bay tới tối đa khi JumpAttack (mét)")]
    [SerializeField] private float jumpAttackDistance = 9f;
    [Tooltip("Độ cao đỉnh cung nhảy (vòng cung parabol trên không, mét)")]
    [SerializeField] private float jumpAttackArcHeight = 2.8f;
    [Tooltip("Thời gian nhún chân lấy đà trước khi chân rời đất (giây, khớp với animation)")]
    [SerializeField] private float jumpTakeoffDelay = 0.65f;
    [Tooltip("Thời gian bay trên không trung từ lúc cất cánh đến khi đập xuống sàn (giây)")]
    [SerializeField] private float jumpFlightDuration = 0.85f;
    [Tooltip("Mục tiêu để Boss vồ tới (Player). Nếu để trống hoặc ngoài tầm thì vồ thẳng theo hướng mặt")]
    [SerializeField] private Transform jumpAttackTarget;
    [Tooltip("Nếu có mục tiêu trong tầm thì ưu tiên nhảy về phía mục tiêu")]
    [SerializeField] private bool autoTargetPlayer = true;
    [Tooltip("Khoảng cách dừng trước mục tiêu (để không bị nhảy chồng xuyên qua người Player)")]
    [SerializeField] private float targetOffsetDistance = 1.8f;

    [Header("JumpAttack Shockwave Ring (Vòng Xoáy Giáng Đòn)")]
    [Tooltip("ĐỘ TRỄ của vòng xoáy xuất hiện & gây sát thương sau khi JumpAttack tiếp đất (giây). Đặt = 0 nếu muốn nổ ngay khi chạm đất")]
    [SerializeField] private float jumpAttackShockwaveDelay = 0f;
    [Tooltip("ĐỘ RỘNG tối đa của vòng xoáy khi JumpAttack đập đất (mét)")]
    [SerializeField] private float jumpAttackShockwaveRadius = 9f;
    [Tooltip("TỐC ĐỘ LAN RA của vòng xoáy JumpAttack (mét/giây) - Chỉnh số này để lan nhanh hay chậm")]
    [SerializeField] private float jumpAttackShockwaveSpeed = 16f;
    [Tooltip("Lượng sát thương gây ra khi vòng xoáy quét trúng")]
    [SerializeField] private float jumpAttackDamage = 40f;
    [Tooltip("Lực đẩy văng mục tiêu khi trúng đòn")]
    [SerializeField] private float jumpAttackKnockback = 3.5f;
    [Tooltip("Màu sắc vòng xoáy JumpAttack")]
    [SerializeField] private Color jumpAttackShockwaveColor = new Color(0.15f, 0.85f, 1f, 0.95f);

    [Header("=== CÀI ĐẶT ATTACK 1 - CÚ ĐÁNH CƠ BẢN (BASIC ATTACK) ===")]
    [Tooltip("Sát thương cú đánh thường của Boss")]
    [SerializeField] private float attack1Damage = 25f;
    [Tooltip("Lực đẩy lùi mục tiêu khi trúng đòn")]
    [SerializeField] private float attack1Knockback = 2.5f;
    [Tooltip("Khoảng cách vùng đánh vươn ra phía trước mặt Boss (mét)")]
    [SerializeField] private float attack1HitDistance = 2.4f;
    [Tooltip("Độ cao vùng va chạm cú đánh tính từ chân Boss (mét)")]
    [SerializeField] private float attack1HitHeight = 1.3f;
    [Tooltip("BÁN KÍNH vùng đánh trúng (mét)")]
    [SerializeField] private float attack1HitRadius = 1.8f;
    [Tooltip("THỜI ĐIỂM ĐÁNH TRÚNG / ĐỘ TRỄ VA CHẠM (giây từ đầu animation): Chỉnh để khớp lúc cánh tay vung tới mục tiêu (ví dụ 0.65s)")]
    [SerializeField] private float attack1HitDelay = 0.65f;
    [Tooltip("ĐỘ TRỄ HIỆU ỨNG (giây): Thời gian chờ sau khi đánh trúng thì hiệu ứng Impact mới xuất hiện (0 = nổ ngay tức thì khi trúng đòn)")]
    [SerializeField] private float attack1VfxDelay = 0f;
    [Tooltip("ĐỘ TO NHỎ / KÍCH THƯỚC của hiệu ứng Impact (Scale, mặc định 1.0, kéo tăng để hiệu ứng nổ to hơn)")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float attack1ImpactVfxScale = 1.0f;
    [Tooltip("Prefab hiệu ứng Impact khi đánh trúng Player (Tự động gán từ Assets/VFX/A VFX Prefabs/Explosiion/Impact.prefab)")]
    [SerializeField] private GameObject attack1ImpactVfxPrefab;
    [Tooltip("Âm thanh va đập khi đánh trúng mục tiêu (tùy chọn)")]
    [SerializeField] private AudioClip attack1HitSfx;

    [Header("=== CÀI ĐẶT ATTACK 2 - GUMOGUMO (ĐÁNH LIÊN TỤC XUỐNG ĐẤT) ===")]
    [Tooltip("Transform xương bàn tay trái (để trống code tự động tìm theo Hitbox/Bone)")]
    [SerializeField] private Transform leftHandBone;
    [Tooltip("Transform xương bàn tay phải (để trống code tự động tìm theo Hitbox/Bone)")]
    [SerializeField] private Transform rightHandBone;

    [Header("GumoGumo Slam Settings (Đập Đất Liên Hoàn - Attack1 Effect)")]
    [Tooltip("Prefab hiệu ứng va đập đấm đất chiêu 2 (Tự động gán 'Assets/VFX/A VFX Prefabs/Attack1 Effect.prefab')")]
    [SerializeField] private GameObject handSlamImpactVfx;
    [Tooltip("ĐỘ TO NHỎ / KÍCH THƯỚC của hiệu ứng Attack1 Effect khi đập đất (Scale, mặc định 1.0)")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float gumoSlamVfxScale = 1.0f;
    [Tooltip("BÁN KÍNH SÁT THƯƠNG diện rộng của mỗi cú đập tay xuống sàn (mét) - TỰ DO ĐIỀU CHỈNH TẠI ĐÂY")]
    [SerializeField] private float gumoShockwaveRadius = 7.0f;
    [Tooltip("Sát thương mỗi cú đập / vòng sóng lan")]
    [SerializeField] private float gumoSlamDamage = 18f;
    [Tooltip("Lực đẩy nhẹ mục tiêu dính sóng")]
    [SerializeField] private float gumoSlamKnockback = 1.2f;

    [Header("GumoGumo Timing (Nhịp Đập Tự Động)")]
    [Tooltip("Thời điểm bắt đầu đập tay xuống đất (giây từ đầu animation)")]
    [SerializeField] private float gumoSlamStartTime = 0.55f;
    [Tooltip("Thời điểm kết thúc đập tay (giây)")]
    [SerializeField] private float gumoSlamEndTime = 3.3f;
    [Tooltip("Khoảng cách giữa các cú đập luân phiên hai tay (giây)")]
    [SerializeField] private float gumoSlamInterval = 0.35f;

    [Header("GumoGumo Shockwave Ring (Tùy Chọn Vòng Sóng Xoáy Cũ)")]
    [Tooltip("Bật/tắt thêm vòng sóng xoáy phát sáng cũ (Mặc định = false để sử dụng trọn vẹn hiệu ứng Attack1 Effect như yêu cầu)")]
    [SerializeField] private bool enableGumoProceduralRing = false;
    [Tooltip("TỐC ĐỘ LAN RA của vòng sóng nếu bật (mét/giây)")]
    [SerializeField] private float gumoShockwaveSpeed = 12f;
    [Tooltip("Màu sắc vòng sóng xung kích GumoGumo")]
    [SerializeField] private Color gumoShockwaveColor = new Color(0.3f, 0.85f, 1f, 0.9f);

    [Header("=== CÀI ĐẶT ATTACK 3 - FLAME BREATH (THỔI LỬA TỪ MẶT) ===")]
    [Tooltip("Transform xương miệng/đầu (nếu kéo nhầm từ Project Window hoặc để trống, script sẽ tự động tìm đúng xương Mieng trên Scene)")]
    [SerializeField] private Transform mouthBone;
    [Tooltip("Trục hướng ra phía trước mặt của xương miệng. Mặc định 'AutoDetect' sẽ tự động dò tìm trục quay ra trước mặt")]
    [SerializeField] private BoneForwardAxis mouthForwardAxis = BoneForwardAxis.AutoDetect;
    [Tooltip("KHÓA HƯỚNG CHỈ QUÉT NGANG (TRÁI/PHẢI): Bật mục này để ngọn lửa chỉ xoay qua lại theo mặt, triệt tiêu hoàn toàn hiện tượng ngóc đầu lên xuống")]
    [SerializeField] private bool lockVerticalRotation = true;
    [Tooltip("KHÓA ĐỘ CAO ĐIỂM PHUN (Y): Giữ tia lửa ở độ cao ổn định ngang người chơi, không bị nhấp nhô lên xuống theo nhịp thở")]
    [SerializeField] private bool lockFlameHeight = true;
    [Tooltip("Độ lệch vị trí tia lửa so với miệng [X: Sang Phải (+), Y: Lên Trên (+), Z: Ra Trước (+)] tính theo mét")]
    [SerializeField] private Vector3 flamePositionOffset = new Vector3(0f, 0.05f, 0.45f);
    [Tooltip("Độ lệch góc xoay Euler (độ) để căn chỉnh tia lửa (X: Cố định góc chúc/ngửa, Y: Trái/Phải, Z: Nghiêng)")]
    [SerializeField] private Vector3 flameRotationOffset = Vector3.zero;

    [Header("Flame Visual Scale (Độ Dài & Độ Rộng Tia Lửa VFX)")]
    [Tooltip("HỆ SỐ ĐỘ DÀI TIA LỬA VFX: Kéo tăng (> 1.0) để tia lửa phóng xa và vươn dài hơn, giảm (< 1.0) để ngắn lại")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float flameVisualLength = 1.0f;
    [Tooltip("HỆ SỐ ĐỘ RỘNG TIA LỬA VFX: Kéo tăng (> 1.0) để ngọn lửa toả rộng sang 2 bên và cuộn dày hơn")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float flameVisualWidth = 1.0f;
    [Tooltip("HỆ SỐ ĐỘ DÀY / MẬT ĐỘ HẠT LỬA (DENSITY): Tăng số này (> 1.0) để sinh nhiều hạt hơn mỗi giây, giúp ngọn lửa cuộn đặc kịt và không bị thưa khi kéo dài")]
    [Range(0.5f, 5.0f)]
    [SerializeField] private float flameDensity = 1.8f;

    [Header("Flame Hitbox & Damage (Tầm Quét Sát Thương Hình Nón)")]
    [Tooltip("TẦM XA sát thương của ngọn lửa (mét) - Tự do điều chỉnh theo độ dài của tia lửa")]
    [SerializeField] private float flameRange = 10.0f;
    [Tooltip("GÓC QUÉT HÌNH NÓN của ngọn lửa (độ) - Tự do điều chỉnh theo độ rộng của tia lửa (ví dụ: 45 - 65 độ)")]
    [SerializeField] private float flameConeAngle = 55f;
    [Tooltip("Thời gian lấy hơi trước khi ngọn lửa bắt đầu phun ra (giây từ đầu animation)")]
    [SerializeField] private float flameStartTime = 0.75f;
    [Tooltip("Thời gian duy trì ngọn lửa phun ra từ miệng (giây)")]
    [SerializeField] private float flameDuration = 3.2f;
    [Tooltip("Sát thương gây ra cho mỗi nhịp tick của ngọn lửa")]
    [SerializeField] private float flameDamagePerTick = 12f;
    [Tooltip("Khoảng cách giữa các nhịp gây sát thương (giây, 0.2s = 5 lần/giây)")]
    [SerializeField] private float flameDamageTickInterval = 0.2f;
    [Tooltip("Lực đẩy lùi nhẹ khi bị lửa thiêu")]
    [SerializeField] private float flameKnockback = 0.6f;

    [Header("Flame Screen Shake (Giảm / Điều Chỉnh Rung Màn Hình)")]
    [Tooltip("Bật/Tắt rung màn hình khi thổi lửa")]
    [SerializeField] private bool enableFlameShake = true;
    [Tooltip("CƯỜNG ĐỘ RUNG MÀN HÌNH - Kéo thanh trượt để giảm độ rung cho êm dịu (Ví dụ: 0.02 - 0.03), hoặc kéo về 0 để TẮT HẲN RUNG")]
    [Range(0f, 0.15f)]
    [SerializeField] private float flameShakeMagnitude = 0.025f;
    [Tooltip("Thời gian mỗi nhịp rung chấn (giây)")]
    [SerializeField] private float flameShakeDuration = 0.05f;

    [Header("=== CƠ CHẾ NHẢY NÉ ĐÒN & XOÁY KIM ĐỒNG HỒ ===")]
    [Tooltip("Độ cao tối thiểu (mét) so với mặt đất để tính là Player đã nhảy lên né đòn thành công")]
    [SerializeField] private float jumpDodgeHeightThreshold = 0.5f;
    [Tooltip("Tốc độ tự xoay theo chiều kim đồng hồ của vòng xoáy (độ/giây)")]
    [SerializeField] private float clockwiseSpinSpeed = 280f;
    [Tooltip("Độ dày nét viền của vòng sóng phát sáng")]
    [SerializeField] private float shockwaveLineWidth = 0.35f;

    [Header("=== VFX PREFABS (MỤC THÊM HIỆU ỨNG TỰ CHỌN SAU NÀY) ===")]
    [Tooltip("Prefab hiệu ứng phun lửa (Mặc định tự động gán 'Flame Thrower.prefab')")]
    [SerializeField] private GameObject flameVfxPrefab;
    [Tooltip("Prefab hiệu ứng khi lửa thiêu trúng mục tiêu (tùy chọn)")]
    [SerializeField] private GameObject flameHitVfxPrefab;
    [Tooltip("Prefab hiệu ứng vòng xoáy tiếp đất của JumpDown (nếu có kéo vào đây)")]
    [SerializeField] private GameObject jumpDownShockwaveVfx;
    [Tooltip("Prefab hiệu ứng vòng xoáy tiếp đất của JumpAttack (nếu có kéo vào đây)")]
    [SerializeField] private GameObject jumpAttackShockwaveVfx;
    [Tooltip("Prefab hiệu ứng vòng sóng chấn của Động tác 2 GumoGumo (nếu có kéo vào đây)")]
    [SerializeField] private GameObject gumoShockwaveVfx;
    [Tooltip("Prefab hiệu ứng hào quang quanh bàn tay khi đập chiêu 2")]
    [SerializeField] private GameObject handAuraVfxPrefab;
    [Tooltip("Prefab hiệu ứng va đập khi JumpDown tiếp đất")]
    [SerializeField] private GameObject jumpDownImpactVfx;
    [Tooltip("Prefab hiệu ứng va đập khi JumpAttack đập đất")]
    [SerializeField] private GameObject jumpAttackImpactVfx;

    [Header("SFX Feedback")]
    [SerializeField] private AudioClip flameBreathSfx;
    [SerializeField] private AudioClip jumpDownImpactSfx;
    [SerializeField] private AudioClip jumpAttackImpactSfx;
    [SerializeField] private AudioClip handSlamImpactSfx;

    [Header("=== PHÁT HIỆN SÀN & VẬT CẢN (GROUND & OBSTACLES) ===")]
    [Tooltip("Layer mục tiêu nhận sát thương (thường là Player)")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Layer mặt sàn để Boss luôn đáp chuẩn xác trên mặt đất")]
    [SerializeField] private LayerMask groundLayer = 1; // Default
    [Tooltip("Layer tường/vật cản để tránh nhảy xuyên tường")]
    [SerializeField] private LayerMask obstacleLayer = 1; // Default

    private float turnSmoothVelocity;
    private const float TurnSmoothTime = 0.1f;
    private bool isDead = false;
    private bool isAttack1Active = false;
    private bool isJumpingDown = false;
    private bool isJumpingAttack = false;
    private bool isGumoGumoAttacking = false;
    private bool isFlameBreathing = false;
    private bool isInitialStartJumpDown = true;
    private Rigidbody rb;

    private GameObject activeLeftHandAura;
    private GameObject activeRightHandAura;
    private GameObject activeFlameInstance;
    private int cachedBoneAxisIndex = -1;
    private float cachedInitialMouthHeightOffset = -1f;

    // Cache Animator Hashes để tối ưu hiệu năng
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int GumoGumoHash = Animator.StringToHash("AttackGumoGumo");
    private static readonly int RoaringHash = Animator.StringToHash("AttackRoaring");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
    private static readonly int JumpDownHash = Animator.StringToHash("JumpDown");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private void Reset()
    {
#if UNITY_EDITOR
        AutoAssignVfx();
#endif
        ValidateMouthBone();
        ValidateHandBones();
        cachedInitialMouthHeightOffset = -1f;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        AutoAssignVfx();
#endif
        ValidateMouthBone();
        ValidateHandBones();
        EnsureTargetLayersIncludePlayer();
        cachedBoneAxisIndex = -1;
        cachedInitialMouthHeightOffset = -1f;
    }

    private void OnDestroy()
    {
        if (activeFlameInstance != null)
        {
            Destroy(activeFlameInstance);
        }
    }

    void Awake()
    {
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        rb = GetComponent<Rigidbody>();

        // Tự động tìm & xác thực xương bàn tay và miệng
        FindBones();
        ValidateMouthBone();
        ValidateHandBones();

#if UNITY_EDITOR
        AutoAssignVfx();
#endif

        // Loại trừ layer của bản thân Boss khỏi groundLayer để Raycast không bắn trúng chính mình
        groundLayer &= ~(1 << gameObject.layer);

        // Bảo đảm targetLayers tự động bao gồm Layer Player
        EnsureTargetLayersIncludePlayer();

        // Tự động nhận diện đối thủ Player trong Scene
        GetPlayerTarget();
    }

    void Start()
    {
        // Tự tìm Player làm mục tiêu nếu chưa gán
        if (playerTarget == null && autoFindPlayer)
        {
            GetPlayerTarget();
        }

        if (jumpAttackTarget == null && autoTargetPlayer) jumpAttackTarget = playerTarget;
        if (jumpDownTarget == null && autoTargetPlayerOnJumpDown) jumpDownTarget = playerTarget;

        // Nhảy vào sân mở màn trận đấu
        if (jumpDownOnStart)
        {
            PlayJumpDown();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Luôn bảo đảm có tham chiếu tới đối thủ Player
        if (playerTarget == null)
        {
            GetPlayerTarget();
        }

        // KIỂM TRA KHÓA: Nếu đang bận thực hiện bất kỳ hoạt cảnh nào -> khóa toàn bộ thao tác
        if (IsBusy())
        {
            // Triệt tiêu tốc độ di chuyển trong Animator để tránh giật chân
            anim.SetFloat(SpeedHash, 0f);
            return;
        }

        // Luôn tự động xoay mặt hướng về phía Player khi rảnh rỗi (Idle)
        HandleFacingPlayer();

        HandleNewInputSystem();
    }

    /// <summary>
    /// Bảo đảm targetLayers luôn bao hàm Layer của Player và Damageable để gây sát thương chính xác
    /// </summary>
    private void EnsureTargetLayersIncludePlayer()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            targetLayers |= (1 << playerLayer);
        }

        int damageableLayer = LayerMask.NameToLayer("Damageable");
        if (damageableLayer != -1)
        {
            targetLayers |= (1 << damageableLayer);
        }

        if (targetLayers.value == 0)
        {
            targetLayers = (playerLayer != -1) ? (1 << playerLayer) : ~0;
        }
    }

    /// <summary>
    /// Tự động tìm kiếm và trả về đối thủ Player trong Scene (qua Tag 'Player', PlayerController hoặc tên 'Player')
    /// </summary>
    public Transform GetPlayerTarget()
    {
        if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
        {
            return playerTarget;
        }

        if (!autoFindPlayer) return playerTarget;

        // 1. Tìm theo Tag Player
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerTarget = pObj.transform;
            EnsureTargetLayersIncludePlayer();
            return playerTarget;
        }

        // 2. Tìm theo PlayerController
#if UNITY_2023_1_OR_NEWER
        PlayerController pc = FindAnyObjectByType<PlayerController>();
#else
        PlayerController pc = FindObjectOfType<PlayerController>();
#endif
        if (pc != null)
        {
            playerTarget = pc.transform;
            EnsureTargetLayersIncludePlayer();
            return playerTarget;
        }

        // 3. Tìm theo tên GameObject 'Player'
        GameObject byName = GameObject.Find("Player");
        if (byName != null)
        {
            playerTarget = byName.transform;
            EnsureTargetLayersIncludePlayer();
            return playerTarget;
        }

        return playerTarget;
    }

    /// <summary>
    /// Xoay mượt mà Boss hướng về phía mục tiêu trên mặt phẳng nằm ngang
    /// </summary>
    public void SmoothFaceTarget(Transform target, float speed)
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > (minFaceDistance * minFaceDistance))
        {
            Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Xoay người dứt khoát thẳng về phía đối thủ ngay tức thì (dùng trước khi tung chiêu)
    /// </summary>
    public void SnapFaceTarget(Transform target = null)
    {
        if (target == null) target = GetPlayerTarget();
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > (minFaceDistance * minFaceDistance))
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized);
        }
    }

    /// <summary>
    /// Xử lý việc luôn hướng mặt về Player khi không bấm di chuyển
    /// </summary>
    private void HandleFacingPlayer()
    {
        if (!alwaysFacePlayer) return;

        Transform target = GetPlayerTarget();
        if (target == null) return;

        var keyboard = Keyboard.current;
        bool isMovingInput = keyboard != null && (
            keyboard.wKey.isPressed || keyboard.sKey.isPressed ||
            keyboard.aKey.isPressed || keyboard.dKey.isPressed ||
            keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
            keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed
        );

        if (!isMovingInput)
        {
            SmoothFaceTarget(target, facePlayerSpeed);
        }
    }

    /// <summary>
    /// Kiểm tra xem Boss có đang bận thực hiện động tác nào không
    /// </summary>
    public bool IsBusy()
    {
        if (anim == null || isDead) return true;
        if (isAttack1Active || isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return true;

        // 1. Kiểm tra nếu Animator đang trong quá trình chuyển cảnh (Transition)
        if (anim.IsInTransition(0))
        {
            AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(0);
            if (!nextState.IsName("Idle") && !nextState.IsName("Walk"))
            {
                return true;
            }
        }

        // 2. Kiểm tra trạng thái hiện tại trên Layer 0
        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        bool isFree = currentState.IsName("Idle") || currentState.IsName("Walk");

        return !isFree;
    }

    /// <summary>
    /// Đọc phím điều khiển từ Input System mới
    /// </summary>
    private void HandleNewInputSystem()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. Di chuyển khi không bận
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;

        Move(horizontal, vertical);

        // 2. Tung chiêu khi không bận
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) CastAttack(0);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) CastAttack(1);
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) CastAttack(2);
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) CastAttack(3);

        // 3. Phím diễn lại tiếp đất (Space) và Phím chết (K)
        if (keyboard.spaceKey.wasPressedThisFrame) PlayJumpDown();
        if (keyboard.kKey.wasPressedThisFrame) Die();
    }

    // ==========================================
    // CÁC HÀM CÔNG KHAI (DÙNG CHO PHÍM HOẶC ML-AGENTS)
    // ==========================================

    public void Move(float horizontal, float vertical)
    {
        if (IsBusy()) return;

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            if (strafeMode)
            {
                Transform target = GetPlayerTarget();
                if (target != null)
                {
                    SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
                }
                Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
            else
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, TurnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            }

            anim.SetFloat(SpeedHash, 1f);
        }
        else
        {
            anim.SetFloat(SpeedHash, 0f);
        }
    }

    public void CastAttack(int attackIndex)
    {
        if (IsBusy()) return;

        switch (attackIndex)
        {
            case 0: PlayAttack1(); break;
            case 1: PlayGumoGumoAttack(); break;
            case 2: PlayFlameBreathAttack(); break;
            case 3: PlayJumpAttack(); break;
        }
    }

    // =========================================================================
    // COROUTINE ATTACK 1 (BASIC ATTACK - CÚ ĐÁNH CƠ BẢN VÀ HIỆU ỨNG IMPACT)
    // =========================================================================

    public void PlayAttack1()
    {
        if (isAttack1Active || isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        StartCoroutine(Attack1Routine());
    }

    private IEnumerator Attack1Routine()
    {
        isAttack1Active = true;

        // Tự động xoay người đối diện chuẩn xác về phía đối thủ trước khi vung nắm đấm
        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        anim.SetTrigger(AttackHash);

        // 1. Chờ đến đúng thời điểm cánh tay vung tới mục tiêu (Hit Delay)
        if (attack1HitDelay > 0f)
        {
            yield return new WaitForSeconds(attack1HitDelay);
        }

        // 2. Thực hiện quét va chạm và gây sát thương / sinh hiệu ứng Impact
        PerformAttack1HitCheck();

        // 3. Chờ cho animation Attack chuyển hoàn tất về Idle
        while (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            yield return null;
        }

        isAttack1Active = false;
    }

    private void PerformAttack1HitCheck()
    {
        Transform target = GetPlayerTarget();
        Vector3 hitDirection = transform.forward;
        float effectiveDist = attack1HitDistance;

        // Nếu có mục tiêu Player trong tầm, căn chỉnh tâm đánh thẳng vào vị trí Player
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist > 0.1f)
            {
                hitDirection = toTarget.normalized;
                effectiveDist = Mathf.Clamp(dist, 1.2f, attack1HitDistance + 0.6f);
            }
        }

        Vector3 hitCenter = transform.position + hitDirection * effectiveDist + Vector3.up * attack1HitHeight;

        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hitColliders = Physics.OverlapSphere(hitCenter, attack1HitRadius, queryMask);
        bool hasHitTarget = false;
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        foreach (Collider col in hitColliders)
        {
            if (col.transform.root == transform.root) continue;

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null && target != null && col.transform.root == target.root)
            {
                damageable = target.GetComponentInChildren<IDamageable>();
            }

            if (damageable != null && hitDamageables.Add(damageable))
            {
                hasHitTarget = true;
                Vector3 hitPoint = col.ClosestPoint(hitCenter);
                Vector3 hitDir = (hitPoint - transform.position).normalized;
                if (hitDir.sqrMagnitude < 0.001f) hitDir = transform.forward;

                damageable.TakeDamage(attack1Damage, hitPoint, hitDir, attack1Knockback, transform);

                // Kích hoạt hiệu ứng Impact tại vị trí trúng đòn
                SpawnAttack1ImpactVfx(hitPoint, Quaternion.LookRotation(-hitDir, Vector3.up));

                if (attack1HitSfx != null)
                {
                    AudioSource.PlayClipAtPoint(attack1HitSfx, hitPoint);
                }
            }
        }

        if (hasHitTarget)
        {
            StartCoroutine(TriggerCameraShake(0.18f, 0.15f));
        }
    }

    private void SpawnAttack1ImpactVfx(Vector3 position, Quaternion rotation)
    {
        if (attack1ImpactVfxPrefab == null)
        {
#if UNITY_EDITOR
            AutoAssignVfx();
#endif
        }

        if (attack1ImpactVfxPrefab != null)
        {
            if (attack1VfxDelay > 0f)
            {
                StartCoroutine(DelayedSpawnAttack1Vfx(position, rotation, attack1VfxDelay));
            }
            else
            {
                InstantiateAttack1Vfx(position, rotation);
            }
        }
    }

    private IEnumerator DelayedSpawnAttack1Vfx(Vector3 position, Quaternion rotation, float delay)
    {
        yield return new WaitForSeconds(delay);
        InstantiateAttack1Vfx(position, rotation);
    }

    private void InstantiateAttack1Vfx(Vector3 position, Quaternion rotation)
    {
        if (attack1ImpactVfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(attack1ImpactVfxPrefab, position, rotation);
        vfxInstance.transform.localScale = Vector3.one * attack1ImpactVfxScale;

        ParticleSystem[] psList = vfxInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in psList)
        {
            var main = ps.main;
            main.startSizeMultiplier *= attack1ImpactVfxScale;
            ps.Play();
        }

        Destroy(vfxInstance, 2.5f);
    }

    public void PlayJumpDown()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        StartCoroutine(JumpDownRoutine());
    }

    public void PlayJumpAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        StartCoroutine(JumpAttackRoutine());
    }

    public void PlayGumoGumoAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        StartCoroutine(GumoGumoRoutine());
    }

    public void PlayFlameBreathAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        StartCoroutine(FlameBreathRoutine());
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        anim.SetTrigger(DeadHash);
    }

    // =========================================================================
    // COROUTINE ATTACK 3 (FLAME BREATH - THỔI LỬA TỪ MẶT DI CHUYỂN THEO XƯƠNG)
    // =========================================================================

    private IEnumerator FlameBreathRoutine()
    {
        isFlameBreathing = true;

        // Tự động xoay người đối diện chuẩn xác về phía đối thủ trước khi gầm và phun lửa
        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // 1. Kích hoạt trigger animation AttackRoaring
        anim.SetTrigger(RoaringHash);

        // 2. Chờ giai đoạn Boss ngửa người lấy hơi trước khi thổi lửa
        float prepTimer = 0f;
        while (prepTimer < flameStartTime)
        {
            prepTimer += Time.deltaTime;
            yield return null;
        }

        // 3. Khởi tạo hiệu ứng ngọn lửa Flame Thrower
        if (flameVfxPrefab == null)
        {
#if UNITY_EDITOR
            AutoAssignVfx();
#endif
        }

        if (mouthBone != null)
        {
            cachedInitialMouthHeightOffset = mouthBone.position.y - transform.position.y;
        }

        if (flameVfxPrefab != null)
        {
            Vector3 startPos = GetMouthPosition();
            Vector3 startDir = GetFlameDirection();
            Quaternion startRot = (startDir.sqrMagnitude > 0.001f) 
                ? Quaternion.LookRotation(startDir, Vector3.up) 
                : transform.rotation;

            // Khởi tạo instance độc lập trong World Space để không bị phụ thuộc vào scale của xương FBX
            activeFlameInstance = Instantiate(flameVfxPrefab, startPos, startRot);

            // Điều chỉnh ĐỘ DÀI & ĐỘ RỘNG của hiệu ứng tia lửa VFX
            ApplyFlameDimensions(activeFlameInstance);

            ParticleSystem[] psList = activeFlameInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in psList)
            {
                p.Play();
            }
        }
        else
        {
            Debug.LogWarning("[IceGolemController] Chưa gán Prefab Flame Thrower vào ô 'Flame Vfx Prefab' trên Inspector!");
        }

        if (flameBreathSfx != null)
        {
            AudioSource.PlayClipAtPoint(flameBreathSfx, GetMouthPosition());
        }

        // 4. Vòng lặp duy trì ngọn lửa: bám theo chuyển động quay đầu của miệng & quét sát thương
        float activeTimer = 0f;
        float damageTickTimer = 0f;

        while (activeTimer < flameDuration)
        {
            activeTimer += Time.deltaTime;
            damageTickTimer += Time.deltaTime;

            // Cập nhật vị trí và hướng của ngọn lửa theo thời gian thực khi đầu/miệng lắc lư
            if (activeFlameInstance != null)
            {
                Vector3 currentPos = GetMouthPosition();
                Vector3 currentDir = GetFlameDirection();

                activeFlameInstance.transform.position = currentPos;
                if (currentDir.sqrMagnitude > 0.001f)
                {
                    activeFlameInstance.transform.rotation = Quaternion.LookRotation(currentDir, Vector3.up);
                }
            }

            // Rung chấn màn hình (nếu được bật và cường độ > 0)
            if (enableFlameShake && flameShakeMagnitude > 0f)
            {
                StartCoroutine(TriggerCameraShake(flameShakeDuration, flameShakeMagnitude));
            }

            if (damageTickTimer >= flameDamageTickInterval)
            {
                damageTickTimer = 0f;
                PerformFlameConeDamageCheck();
            }

            yield return null;
        }

        // 5. Kết thúc phun lửa: Dừng phát hạt và dọn dẹp instance
        if (activeFlameInstance != null)
        {
            ParticleSystem[] psList = activeFlameInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in psList)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            Destroy(activeFlameInstance, 2.0f);
            activeFlameInstance = null;
        }

        // Chờ animation Roaring hoàn tất thu người về thế Idle
        while (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("AttackRoaring"))
        {
            yield return null;
        }

        isFlameBreathing = false;
    }

    /// <summary>
    /// Điều chỉnh Độ Dài, Độ Rộng và Độ Dày (Mật Độ Hạt) cho hiệu ứng tia lửa VFX Flame Thrower
    /// </summary>
    private void ApplyFlameDimensions(GameObject flameObj)
    {
        if (flameObj == null) return;

        // 1. Co giãn kích thước Transform (Trục Z là độ dài tầm phóng, Trục X & Y là độ rộng/dày)
        flameObj.transform.localScale = new Vector3(flameVisualWidth, flameVisualWidth, flameVisualLength);

        // 2. Tinh chỉnh trực tiếp các ParticleSystem bên trong
        ParticleSystem[] psList = flameObj.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in psList)
        {
            var main = ps.main;
            // Tăng tốc độ bay của hạt theo flameVisualLength để tia lửa vươn xa hơn
            main.startSpeedMultiplier *= flameVisualLength;
            // Phóng to kích thước từng cụm hạt lửa theo flameVisualWidth để lửa dày hơn
            main.startSizeMultiplier *= flameVisualWidth;

            // Tự động nâng giới hạn số lượng hạt tối đa (Max Particles) để không bị nghẽn ở mức 30 hạt
            int baseMax = Mathf.Max(main.maxParticles, 100);
            main.maxParticles = Mathf.RoundToInt(baseMax * Mathf.Max(flameVisualLength, 1f) * flameDensity);

            // Mở rộng góc toả nón theo flameVisualWidth
            var shape = ps.shape;
            if (shape.enabled)
            {
                shape.angle = Mathf.Clamp(shape.angle * flameVisualWidth, 1f, 85f);
            }

            // Tăng tốc độ phun hạt (Emission Rate Over Time) để lấp đầy khoảng trống khi tia lửa bị kéo dài ra
            var emission = ps.emission;
            if (emission.enabled)
            {
                emission.rateOverTimeMultiplier *= (Mathf.Max(flameVisualLength, 1f) * flameDensity);
            }
        }

        // 3. Mở rộng bán kính chiếu sáng của Point Light theo độ dài ngọn lửa
        Light flameLight = flameObj.GetComponentInChildren<Light>();
        if (flameLight != null)
        {
            flameLight.range *= flameVisualLength;
        }
    }

    /// <summary>
    /// Lấy vị trí miệng phun lửa thực tế trong không gian World
    /// </summary>
    public Vector3 GetMouthPosition()
    {
        ValidateMouthBone();

        if (mouthBone != null)
        {
            Vector3 basePos = mouthBone.position;

            // Khóa độ cao cố định so với sàn đấu, triệt tiêu hoàn toàn nhịp nhấp nhô lên xuống của xương
            if (lockFlameHeight)
            {
                if (cachedInitialMouthHeightOffset <= 0f)
                {
                    cachedInitialMouthHeightOffset = mouthBone.position.y - transform.position.y;
                }
                basePos.y = transform.position.y + cachedInitialMouthHeightOffset;
            }

            Vector3 fDir = GetFlameDirection();

            Vector3 fRight = Vector3.Cross(Vector3.up, fDir).normalized;
            if (fRight.sqrMagnitude < 0.001f) fRight = transform.right;
            Vector3 fUp = Vector3.Cross(fDir, fRight).normalized;

            return basePos 
                + fDir * flamePositionOffset.z 
                + fUp * flamePositionOffset.y 
                + fRight * flamePositionOffset.x;
        }

        return transform.position + Vector3.up * 2.2f + transform.forward * 0.6f;
    }

    /// <summary>
    /// Lấy hướng phun lửa chuẩn xác theo góc quay thời gian thực của xương mặt/miệng
    /// </summary>
    public Vector3 GetFlameDirection()
    {
        ValidateMouthBone();

        Vector3 baseDir;
        if (mouthBone != null)
        {
            baseDir = GetBoneMouthFacingDirection(mouthBone);
        }
        else
        {
            baseDir = transform.forward;
        }

        if (baseDir.sqrMagnitude < 0.0001f)
        {
            baseDir = transform.forward;
        }

        // Triệt tiêu thành phần trục Y để tia lửa CHỈ quét qua lại Trái / Phải theo mặt, không ngóc lên ngóc xuống
        if (lockVerticalRotation)
        {
            baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.0001f)
            {
                baseDir = transform.forward;
                baseDir.y = 0f;
            }
            baseDir.Normalize();
        }

        if (flameRotationOffset != Vector3.zero)
        {
            Quaternion rotOffset = Quaternion.Euler(flameRotationOffset);
            Quaternion lookRot = Quaternion.LookRotation(baseDir, Vector3.up);
            baseDir = (lookRot * rotOffset) * Vector3.forward;
        }

        return baseDir.normalized;
    }

    private Vector3 GetBoneMouthFacingDirection(Transform bone)
    {
        switch (mouthForwardAxis)
        {
            case BoneForwardAxis.ForwardZ: return bone.forward;
            case BoneForwardAxis.BackNegativeZ: return -bone.forward;
            case BoneForwardAxis.UpY: return bone.up;
            case BoneForwardAxis.DownNegativeY: return -bone.up;
            case BoneForwardAxis.RightX: return bone.right;
            case BoneForwardAxis.LeftNegativeX: return -bone.right;
            case BoneForwardAxis.BossForward: return transform.forward;
            case BoneForwardAxis.AutoDetect:
            default:
                return GetAutoDetectedBoneForward(bone);
        }
    }

    private Vector3 GetAutoDetectedBoneForward(Transform bone)
    {
        Vector3[] axes = new Vector3[]
        {
            bone.forward,   // 0: +Z
            -bone.forward,  // 1: -Z
            bone.up,        // 2: +Y
            -bone.up,       // 3: -Y
            bone.right,     // 4: +X
            -bone.right     // 5: -X
        };

        if (cachedBoneAxisIndex >= 0 && cachedBoneAxisIndex < axes.Length)
        {
            return axes[cachedBoneAxisIndex];
        }

        Vector3 bossFwd = transform.forward;
        float bestDot = -999f;
        int bestIdx = 0;

        for (int i = 0; i < axes.Length; i++)
        {
            float dot = Vector3.Dot(axes[i], bossFwd);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestIdx = i;
            }
        }

        cachedBoneAxisIndex = bestIdx;
        return axes[bestIdx];
    }

    private void ValidateMouthBone()
    {
        bool isInvalid = mouthBone == null 
            || !mouthBone.gameObject.scene.IsValid() 
            || !mouthBone.IsChildOf(transform);

        if (isInvalid)
        {
            Transform found = FindBoneByName("Mieng");
            if (found == null && anim != null && anim.isHuman)
            {
                found = anim.GetBoneTransform(HumanBodyBones.Head);
            }
            if (found == null)
            {
                found = FindBoneByName("Head") ?? FindBoneByName("Mouth") ?? FindBoneByName("Co") ?? FindBoneByName("Bone.026");
            }

            if (found != null)
            {
                mouthBone = found;
                cachedBoneAxisIndex = -1;
            }
        }
    }

    private void ValidateHandBones()
    {
        if (leftHandBone == null || !leftHandBone.gameObject.scene.IsValid() || !leftHandBone.IsChildOf(transform))
        {
            leftHandBone = FindBoneByName("BossHitboxT") ?? FindBoneByName("Hand_L") ?? FindBoneByName("LeftHand");
        }
        if (rightHandBone == null || !rightHandBone.gameObject.scene.IsValid() || !rightHandBone.IsChildOf(transform))
        {
            rightHandBone = FindBoneByName("BossHitboxP") ?? FindBoneByName("Hand_R") ?? FindBoneByName("RightHand");
        }
    }

    private Transform FindBoneByName(string targetName)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t == transform) continue;
            if (t.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
        }
        foreach (var t in allChildren)
        {
            if (t == transform) continue;
            if (t.name.ToLower().Contains(targetName.ToLower()))
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// Quét sát thương hình nón theo vị trí và hướng nhìn thời gian thực của xương mặt/miệng
    /// </summary>
    private void PerformFlameConeDamageCheck()
    {
        Vector3 origin = GetMouthPosition();
        Vector3 direction = GetFlameDirection();

        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hitColliders = Physics.OverlapSphere(origin, flameRange, queryMask);
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        foreach (Collider col in hitColliders)
        {
            if (col.transform.root == transform.root) continue;

            Vector3 targetCenter = col.bounds.center;
            Vector3 toTarget = targetCenter - origin;
            float dist = toTarget.magnitude;

            if (dist > flameRange || dist < 0.01f) continue;

            // Kiểm tra góc lệch so với hướng miệng đang quay
            float angle = Vector3.Angle(direction, toTarget);
            if (angle <= flameConeAngle * 0.5f)
            {
                // Kiểm tra vật cản che chắn (tường / cột đá)
                if (Physics.Raycast(origin, toTarget.normalized, dist, obstacleLayer))
                {
                    continue;
                }

                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null && playerTarget != null && col.transform.root == playerTarget.root)
                {
                    damageable = playerTarget.GetComponentInChildren<IDamageable>();
                }

                if (damageable != null && hitDamageables.Add(damageable))
                {
                    Vector3 hitPoint = col.ClosestPoint(origin);
                    damageable.TakeDamage(flameDamagePerTick, hitPoint, toTarget.normalized, flameKnockback, transform);

                    if (flameHitVfxPrefab != null)
                    {
                        Instantiate(flameHitVfxPrefab, hitPoint, Quaternion.identity);
                    }
                }
            }
        }
    }


    // ==========================================
    // COROUTINE ATTACK 2 (GUMOGUMO - ĐẬP LIÊN TỤC)
    // ==========================================

    private IEnumerator GumoGumoRoutine()
    {
        isGumoGumoAttacking = true;

        // Tự động xoay người đối diện chuẩn xác về phía đối thủ trước khi đập đất liên hoàn
        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        anim.SetTrigger(GumoGumoHash);
        EnableHandAuras();

        float currentTimer = 0f;
        while (currentTimer < gumoSlamStartTime)
        {
            currentTimer += Time.deltaTime;
            yield return null;
        }

        int currentHandIndex = 0; // 0 = Trái, 1 = Phải
        float nextSlamTime = currentTimer;

        while (currentTimer < gumoSlamEndTime)
        {
            currentTimer += Time.deltaTime;

            if (currentTimer >= nextSlamTime)
            {
                TriggerHandSlam(currentHandIndex);
                currentHandIndex = (currentHandIndex == 0) ? 1 : 0;
                nextSlamTime = currentTimer + gumoSlamInterval;
            }

            yield return null;
        }

        DisableHandAuras();

        while (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("AttackGumoGumo"))
        {
            yield return null;
        }

        isGumoGumoAttacking = false;
    }

    public void TriggerHandSlam(int handIndex)
    {
        Transform handBone = (handIndex == 0) ? leftHandBone : rightHandBone;
        Vector3 slamOrigin;

        if (handBone != null)
        {
            slamOrigin = handBone.position;
        }
        else
        {
            float sideOffset = (handIndex == 0) ? -0.9f : 0.9f;
            slamOrigin = transform.position + transform.forward * 1.6f + transform.right * sideOffset;
        }

        Vector3 slamGroundPos = GetGroundPosition(slamOrigin);

        // 1. Rung chấn nhẹ theo từng nhịp đập
        StartCoroutine(TriggerCameraShake(0.12f, 0.12f));

        // 2. Tự động kiểm tra liên kết Attack1 Effect nếu chưa gán
        if (handSlamImpactVfx == null)
        {
#if UNITY_EDITOR
            AutoAssignVfx();
#endif
        }

        // 3. Sinh hiệu ứng đập đất Attack1 Effect tại vị trí bàn tay tiếp đất
        if (handSlamImpactVfx != null)
        {
            GameObject slamObj = Instantiate(handSlamImpactVfx, slamGroundPos, Quaternion.identity);
            slamObj.transform.localScale = Vector3.one * gumoSlamVfxScale;

            ParticleSystem[] psList = slamObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in psList)
            {
                var main = ps.main;
                main.loop = false; // Tắt loop để nổ đúng 1 lần cho từng cú đập
                ps.Play();
            }

            Destroy(slamObj, 2.5f);
        }

        // 4. Âm thanh đập đất
        if (handSlamImpactSfx != null)
        {
            AudioSource.PlayClipAtPoint(handSlamImpactSfx, slamGroundPos);
        }

        // 5. Gây sát thương diện rộng xung quanh vị trí đập tay (vẫn có thể nhảy né đòn)
        PerformHandSlamDamage(slamGroundPos);

        // 6. Tùy chọn sinh thêm vòng sóng xoáy cũ (nếu người dùng bật)
        if (enableGumoProceduralRing)
        {
            SpawnClockwiseShockwave(
                slamGroundPos,
                gumoShockwaveRadius,
                gumoShockwaveSpeed,
                0f, // Sát thương đã được xử lý bởi PerformHandSlamDamage
                gumoSlamKnockback,
                gumoShockwaveColor,
                gumoShockwaveVfx
            );
        }
    }

    private void PerformHandSlamDamage(Vector3 slamGroundPos)
    {
        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hitColliders = Physics.OverlapSphere(slamGroundPos, gumoShockwaveRadius, queryMask);
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        foreach (Collider col in hitColliders)
        {
            if (col.transform.root == transform.root) continue;

            // Kiểm tra xem mục tiêu có đang nhảy né đòn trên không không
            if (CheckCanDodgeByJump(col, slamGroundPos))
            {
                continue;
            }

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null && playerTarget != null && col.transform.root == playerTarget.root)
            {
                damageable = playerTarget.GetComponentInChildren<IDamageable>();
            }

            if (damageable != null && hitDamageables.Add(damageable))
            {
                Vector3 hitPoint = col.ClosestPoint(slamGroundPos);
                Vector3 toTarget = (hitPoint - slamGroundPos).normalized;
                if (toTarget.sqrMagnitude < 0.001f) toTarget = transform.forward;

                damageable.TakeDamage(gumoSlamDamage, hitPoint, toTarget, gumoSlamKnockback, transform);
            }
        }
    }

    public void OnGumoGumoLeftHandSlam() => TriggerHandSlam(0);
    public void OnGumoGumoRightHandSlam() => TriggerHandSlam(1);

    // =========================================================================
    // HỆ THỐNG VÒNG SÓNG XOÁY THEO CHIỀU KIM ĐỒNG HỒ (CLOCKWISE SHOCKWAVE RING)
    // =========================================================================

    public void SpawnClockwiseShockwave(
        Vector3 center,
        float maxRadius,
        float expandSpeed,
        float damage,
        float knockback,
        Color ringColor,
        GameObject customVfxPrefab)
    {
        StartCoroutine(ExpandingClockwiseShockwaveRoutine(
            center,
            maxRadius,
            expandSpeed,
            damage,
            knockback,
            ringColor,
            customVfxPrefab
        ));
    }

    private IEnumerator ExpandingClockwiseShockwaveRoutine(
        Vector3 center,
        float maxRadius,
        float expandSpeed,
        float damage,
        float knockback,
        Color ringColor,
        GameObject customVfxPrefab)
    {
        if (customVfxPrefab != null)
        {
            GameObject customVfx = Instantiate(customVfxPrefab, center, Quaternion.identity);
            float safeLifetime = (maxRadius / Mathf.Max(expandSpeed, 1f)) + 1.5f;
            Destroy(customVfx, safeLifetime);
        }

        float speed = Mathf.Max(expandSpeed, 0.5f);
        float duration = maxRadius / speed;

        GameObject waveRoot = new GameObject("ClockwiseShockwaveRing");
        waveRoot.transform.position = center + Vector3.up * 0.04f;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null) lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        Material lineMat = new Material(lineShader);

        GameObject outerObj = new GameObject("OuterRing");
        outerObj.transform.SetParent(waveRoot.transform, false);
        LineRenderer outerLine = outerObj.AddComponent<LineRenderer>();
        SetupLineRenderer(outerLine, lineMat, ringColor, shockwaveLineWidth, 40, true);

        int armCount = 4;
        LineRenderer[] swirlLines = new LineRenderer[armCount];
        int pointsPerArm = 16;
        for (int k = 0; k < armCount; k++)
        {
            GameObject armObj = new GameObject($"SwirlArm_{k}");
            armObj.transform.SetParent(waveRoot.transform, false);
            swirlLines[k] = armObj.AddComponent<LineRenderer>();
            SetupLineRenderer(swirlLines[k], lineMat, ringColor, shockwaveLineWidth * 0.8f, pointsPerArm, false);
        }

        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        float timer = 0f;
        float currentRotationY = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float currentRadius = Mathf.Lerp(0.2f, maxRadius, t);

            currentRotationY += clockwiseSpinSpeed * Time.deltaTime;
            waveRoot.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);

            float angleStep = 360f / 40;
            for (int i = 0; i < 40; i++)
            {
                float rad = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * currentRadius;
                float z = Mathf.Sin(rad) * currentRadius;
                outerLine.SetPosition(i, new Vector3(x, 0f, z));
            }

            float baseAngleOffset = 360f / armCount;
            for (int k = 0; k < armCount; k++)
            {
                float armBaseAngle = k * baseAngleOffset;
                for (int p = 0; p < pointsPerArm; p++)
                {
                    float fraction = (float)p / (pointsPerArm - 1);
                    float r = fraction * currentRadius;
                    float swirlRad = (armBaseAngle + fraction * 75f) * Mathf.Deg2Rad;
                    float sx = Mathf.Cos(swirlRad) * r;
                    float sz = -Mathf.Sin(swirlRad) * r;
                    swirlLines[k].SetPosition(p, new Vector3(sx, 0f, sz));
                }
            }

            Color fadeColor = ringColor;
            fadeColor.a = Mathf.Lerp(ringColor.a, 0f, t * t);
            outerLine.startColor = fadeColor;
            outerLine.endColor = fadeColor;

            for (int k = 0; k < armCount; k++)
            {
                swirlLines[k].startColor = fadeColor;
                Color tipColor = fadeColor;
                tipColor.a *= 0.25f;
                swirlLines[k].endColor = tipColor;
            }

            if (damage > 0f)
            {
                LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
                Collider[] hits = Physics.OverlapSphere(center, currentRadius, queryMask);
                foreach (Collider col in hits)
                {
                    if (col.transform.root == transform.root) continue;

                    IDamageable dmg = col.GetComponentInParent<IDamageable>();
                    if (dmg == null && playerTarget != null && col.transform.root == playerTarget.root)
                    {
                        dmg = playerTarget.GetComponentInChildren<IDamageable>();
                    }

                    if (dmg != null && !hitDamageables.Contains(dmg))
                    {
                        if (CheckCanDodgeByJump(col, center))
                        {
                            continue;
                        }

                        hitDamageables.Add(dmg);
                        Vector3 hitPoint = col.ClosestPoint(center);
                        Vector3 knockDir = (col.transform.position - center).normalized;
                        knockDir.y = 0.25f;
                        dmg.TakeDamage(damage, hitPoint, knockDir, knockback, transform);
                    }
                }
            }

            yield return null;
        }

        Destroy(lineMat);
        Destroy(waveRoot);
    }

    private bool CheckCanDodgeByJump(Collider targetCol, Vector3 waveCenter)
    {
        PlayerController pc = targetCol.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            if (!pc.IsGrounded)
            {
                return true;
            }
        }

        float footY = targetCol.bounds.min.y;
        float heightAboveFloor = footY - waveCenter.y;
        if (heightAboveFloor > jumpDodgeHeightThreshold)
        {
            return true;
        }

        return false;
    }

    private void SetupLineRenderer(LineRenderer line, Material mat, Color col, float width, int count, bool loop)
    {
        line.useWorldSpace = false;
        line.loop = loop;
        line.positionCount = count;
        line.startWidth = width;
        line.endWidth = width;
        line.material = mat;
        line.startColor = col;
        line.endColor = col;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    // ==========================================
    // HIỆU ỨNG HÀO QUANG BÀN TAY
    // ==========================================

    private void EnableHandAuras()
    {
        if (handAuraVfxPrefab == null) return;

        if (leftHandBone != null && activeLeftHandAura == null)
        {
            activeLeftHandAura = Instantiate(handAuraVfxPrefab, leftHandBone);
            activeLeftHandAura.transform.localPosition = Vector3.zero;
            activeLeftHandAura.transform.localRotation = Quaternion.identity;
        }

        if (rightHandBone != null && activeRightHandAura == null)
        {
            activeRightHandAura = Instantiate(handAuraVfxPrefab, rightHandBone);
            activeRightHandAura.transform.localPosition = Vector3.zero;
            activeRightHandAura.transform.localRotation = Quaternion.identity;
        }
    }

    private void DisableHandAuras()
    {
        if (activeLeftHandAura != null)
        {
            Destroy(activeLeftHandAura);
            activeLeftHandAura = null;
        }

        if (activeRightHandAura != null)
        {
            Destroy(activeRightHandAura);
            activeRightHandAura = null;
        }
    }

    // ==========================================
    // COROUTINES XỬ LÝ JUMP DOWN & JUMP ATTACK
    // ==========================================

    private IEnumerator JumpDownRoutine()
    {
        isJumpingDown = true;

        if (jumpDownStartDelay > 0f)
        {
            yield return new WaitForSeconds(jumpDownStartDelay);
        }

        bool originalKinematic = rb != null && rb.isKinematic;
        bool originalGravity = rb != null && rb.useGravity;
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPos;
        Vector3 targetLandingPos;

        if (isInitialStartJumpDown)
        {
            Transform initTarget = jumpDownTarget != null ? jumpDownTarget : (autoTargetPlayerOnJumpDown ? GetPlayerTarget() : null);
            if (initTarget != null)
            {
                Vector3 toInit = initTarget.position - transform.position;
                toInit.y = 0f;
                if (toInit.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(toInit.normalized);
                }
            }

            targetLandingPos = GetGroundPosition(transform.position);
            startPos = targetLandingPos - transform.forward * jumpDownDistance + Vector3.up * jumpDownHeight;
            transform.position = startPos;
            isInitialStartJumpDown = false;
        }
        else
        {
            startPos = transform.position;

            Transform target = jumpDownTarget != null ? jumpDownTarget : (autoTargetPlayerOnJumpDown ? GetPlayerTarget() : null);

            if (target != null)
            {
                Vector3 toTarget = target.position - startPos;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(toTarget.normalized);
                }

                float desiredDist = Mathf.Min(dist - targetOffsetDistance, jumpDownDistance);
                desiredDist = Mathf.Max(desiredDist, 1.5f);
                targetLandingPos = startPos + transform.forward * desiredDist;
            }
            else
            {
                targetLandingPos = startPos + transform.forward * jumpDownDistance;
            }

            Vector3 flightDir = (targetLandingPos - startPos);
            float flightDist = flightDir.magnitude;
            if (flightDist > 0.05f)
            {
                flightDir.Normalize();
                if (Physics.Raycast(startPos + Vector3.up * 1.5f, flightDir, out RaycastHit wallHit, flightDist, obstacleLayer))
                {
                    targetLandingPos = wallHit.point - flightDir * 0.8f;
                }
            }

            targetLandingPos = GetGroundPosition(targetLandingPos);
            startPos = startPos + Vector3.up * jumpDownHeight;
        }

        anim.SetTrigger(JumpDownHash);

        float fallTimer = 0f;
        while (fallTimer < jumpDownFallDuration)
        {
            fallTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fallTimer / jumpDownFallDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, targetLandingPos, t);
            float curveT = jumpDownFallCurve != null ? jumpDownFallCurve.Evaluate(t) : (t * t);
            float currentY = Mathf.Lerp(startPos.y, targetLandingPos.y, curveT);

            if (jumpDownArcHeight > 0f)
            {
                currentY += Mathf.Sin(t * Mathf.PI) * jumpDownArcHeight;
            }

            currentPos.y = currentY;
            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetLandingPos;

        if (rb != null)
        {
            rb.isKinematic = originalKinematic;
            rb.useGravity = originalGravity;
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }

        OnJumpDownImpact(targetLandingPos);

        isJumpingDown = false;
    }

    private IEnumerator JumpAttackRoutine()
    {
        isJumpingAttack = true;

        anim.SetTrigger(JumpAttackHash);

        yield return new WaitForSeconds(jumpTakeoffDelay);

        bool originalKinematic = rb != null && rb.isKinematic;
        bool originalGravity = rb != null && rb.useGravity;
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPos = transform.position;
        Vector3 targetLandingPos;

        Transform target = jumpAttackTarget != null ? jumpAttackTarget : (autoTargetPlayer ? GetPlayerTarget() : null);

        if (target != null)
        {
            Vector3 toTarget = target.position - startPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(toTarget.normalized);
            }

            float desiredDist = Mathf.Min(dist - targetOffsetDistance, jumpAttackDistance);
            desiredDist = Mathf.Max(desiredDist, 1.5f);

            targetLandingPos = startPos + transform.forward * desiredDist;
        }
        else
        {
            targetLandingPos = startPos + transform.forward * jumpAttackDistance;
        }

        Vector3 flightDir = (targetLandingPos - startPos);
        float flightDist = flightDir.magnitude;
        if (flightDist > 0.05f)
        {
            flightDir.Normalize();
            if (Physics.Raycast(startPos + Vector3.up * 1.5f, flightDir, out RaycastHit wallHit, flightDist, obstacleLayer))
            {
                targetLandingPos = wallHit.point - flightDir * 0.8f;
            }
        }

        targetLandingPos = GetGroundPosition(targetLandingPos);

        float flightTimer = 0f;
        while (flightTimer < jumpFlightDuration)
        {
            flightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(flightTimer / jumpFlightDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, targetLandingPos, t);
            float arcY = Mathf.Sin(t * Mathf.PI) * jumpAttackArcHeight;
            currentPos.y += arcY;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetLandingPos;

        if (rb != null)
        {
            rb.isKinematic = originalKinematic;
            rb.useGravity = originalGravity;
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }

        OnJumpAttackImpact(targetLandingPos);

        isJumpingAttack = false;
    }

    private void OnJumpDownImpact(Vector3 landingPos)
    {
        if (jumpDownShockwaveDelay > 0f)
        {
            StartCoroutine(DelayedJumpDownShockwave(landingPos, jumpDownShockwaveDelay));
        }
        else
        {
            TriggerJumpDownImpactImmediate(landingPos);
        }
    }

    private IEnumerator DelayedJumpDownShockwave(Vector3 landingPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerJumpDownImpactImmediate(landingPos);
    }

    private void TriggerJumpDownImpactImmediate(Vector3 landingPos)
    {
        if (jumpDownImpactVfx != null)
        {
            Instantiate(jumpDownImpactVfx, landingPos, Quaternion.identity);
        }

        if (jumpDownImpactSfx != null)
        {
            AudioSource.PlayClipAtPoint(jumpDownImpactSfx, landingPos);
        }

        StartCoroutine(TriggerCameraShake(0.35f, 0.25f));

        SpawnClockwiseShockwave(
            landingPos,
            jumpDownShockwaveRadius,
            jumpDownShockwaveSpeed,
            jumpDownShockwaveDamage,
            jumpDownShockwaveKnockback,
            jumpDownShockwaveColor,
            jumpDownShockwaveVfx
        );
    }

    private void OnJumpAttackImpact(Vector3 landingPos)
    {
        if (jumpAttackShockwaveDelay > 0f)
        {
            StartCoroutine(DelayedJumpAttackShockwave(landingPos, jumpAttackShockwaveDelay));
        }
        else
        {
            TriggerJumpAttackImpactImmediate(landingPos);
        }
    }

    private IEnumerator DelayedJumpAttackShockwave(Vector3 landingPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerJumpAttackImpactImmediate(landingPos);
    }

    private void TriggerJumpAttackImpactImmediate(Vector3 landingPos)
    {
        if (jumpAttackImpactVfx != null)
        {
            Instantiate(jumpAttackImpactVfx, landingPos, Quaternion.identity);
        }

        if (jumpAttackImpactSfx != null)
        {
            AudioSource.PlayClipAtPoint(jumpAttackImpactSfx, landingPos);
        }

        StartCoroutine(TriggerCameraShake(0.45f, 0.35f));

        SpawnClockwiseShockwave(
            landingPos,
            jumpAttackShockwaveRadius,
            jumpAttackShockwaveSpeed,
            jumpAttackDamage,
            jumpAttackKnockback,
            jumpAttackShockwaveColor,
            jumpAttackShockwaveVfx
        );
    }

    private IEnumerator TriggerCameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Transform camTransform = cam.transform;
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
    }

    private Vector3 GetGroundPosition(Vector3 searchPos)
    {
        if (Physics.Raycast(searchPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 50f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return searchPos;
    }

    private void FindBones()
    {
        if (anim != null && anim.isHuman)
        {
            if (leftHandBone == null) leftHandBone = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            if (rightHandBone == null) rightHandBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (mouthBone == null) mouthBone = anim.GetBoneTransform(HumanBodyBones.Head);
        }

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            string lowerName = t.name.ToLower();
            if (leftHandBone == null && (t.name == "BossHitboxT" || lowerName.Contains("hand_l") || lowerName.Contains("lefthand") || lowerName.Contains("wrist_l")))
            {
                leftHandBone = t;
            }
            if (rightHandBone == null && (t.name == "BossHitboxP" || lowerName.Contains("hand_r") || lowerName.Contains("righthand") || lowerName.Contains("wrist_r")))
            {
                rightHandBone = t;
            }
            if (mouthBone == null && (t.name == "Mieng" || lowerName.Contains("head") || lowerName.Contains("mouth") || lowerName.Contains("face")))
            {
                mouthBone = t;
            }
        }
    }

    private void AutoAssignVfx()
    {
#if UNITY_EDITOR
        if (flameVfxPrefab == null)
        {
            flameVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Other/Flame Thrower.prefab");
        }
        if (attack1ImpactVfxPrefab == null)
        {
            attack1ImpactVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Explosiion/Impact.prefab");
        }
        if (handSlamImpactVfx == null)
        {
            handSlamImpactVfx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Attack1 Effect.prefab");
        }
#endif
    }

    // ==========================================
    // GIZMOS TRỰC QUAN HÓA TRONG SCENE VIEW
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position;

        // 0. Đường ngắm khóa mục tiêu Player (Màu Tím Hồng / Magenta)
        Transform target = playerTarget != null ? playerTarget : (autoFindPlayer ? GetPlayerTarget() : null);
        if (target != null)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.8f, 0.9f);
            Gizmos.DrawLine(start + Vector3.up * 1.5f, target.position + Vector3.up * 1.0f);
            Gizmos.DrawWireSphere(target.position + Vector3.up * 1.0f, 0.45f);
        }

        // 1. Vùng va chạm Attack 1 (Màu Cam Đỏ)
        Vector3 hitDir1 = transform.forward;
        float hitDist1 = attack1HitDistance;
        if (target != null)
        {
            Vector3 toTarget = target.position - start;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.05f)
            {
                hitDir1 = toTarget.normalized;
                hitDist1 = Mathf.Clamp(toTarget.magnitude, 1.2f, attack1HitDistance + 0.6f);
            }
        }
        Vector3 hit1Center = start + hitDir1 * hitDist1 + Vector3.up * attack1HitHeight;
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(hit1Center, attack1HitRadius);
        Gizmos.DrawLine(start + Vector3.up * attack1HitHeight, hit1Center);

        // 1. Cung nhảy và vòng xoáy JumpAttack (Vàng / Đỏ)
        Gizmos.color = Color.yellow;
        Vector3 attackTarget = start + transform.forward * jumpAttackDistance;
        Gizmos.DrawLine(start, attackTarget);
        Gizmos.DrawWireSphere(attackTarget, 0.5f);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(attackTarget, jumpAttackShockwaveRadius);

        // 2. Khoảng cách bay và vòng xoáy JumpDown (Cyan)
        Gizmos.color = Color.cyan;
        Vector3 jumpDownTargetPos = start + transform.forward * jumpDownDistance;
        Gizmos.DrawLine(start, jumpDownTargetPos);
        Gizmos.DrawWireSphere(jumpDownTargetPos, 0.6f);

        Vector3 jumpDownAir = start - transform.forward * jumpDownDistance + Vector3.up * jumpDownHeight;
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawLine(jumpDownAir, start);
        Gizmos.DrawWireSphere(jumpDownAir, 0.8f);

        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(jumpDownTargetPos, jumpDownShockwaveRadius);

        // 3. Vòng xoáy GumoGumo (Xanh Băng)
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.5f);
        Vector3 gumoCenter = start + transform.forward * 1.5f;
        Gizmos.DrawWireSphere(gumoCenter, gumoShockwaveRadius);

        // 4. Hình nón phun lửa của Attack 3 (Màu Cam / Đỏ nổi bật)
        Vector3 fOrigin = GetMouthPosition();
        Vector3 fDir = GetFlameDirection();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.95f);
        Gizmos.DrawRay(fOrigin, fDir * flameRange);
        Gizmos.DrawWireSphere(fOrigin, 0.15f);

        float halfAng = flameConeAngle * 0.5f;
        Vector3 fRight = Vector3.Cross(Vector3.up, fDir).normalized;
        if (fRight.sqrMagnitude < 0.001f) fRight = transform.right;
        Vector3 fUp = Vector3.Cross(fDir, fRight).normalized;

        Quaternion lRot = Quaternion.AngleAxis(-halfAng, fUp);
        Quaternion rRot = Quaternion.AngleAxis(halfAng, fUp);
        Quaternion uRot = Quaternion.AngleAxis(-halfAng, fRight);
        Quaternion dRot = Quaternion.AngleAxis(halfAng, fRight);

        Gizmos.color = new Color(1f, 0.45f, 0f, 0.4f);
        Gizmos.DrawRay(fOrigin, (lRot * fDir) * flameRange);
        Gizmos.DrawRay(fOrigin, (rRot * fDir) * flameRange);
        Gizmos.DrawRay(fOrigin, (uRot * fDir) * flameRange);
        Gizmos.DrawRay(fOrigin, (dRot * fDir) * flameRange);

        float endRadius = flameRange * Mathf.Tan(halfAng * Mathf.Deg2Rad);
        Gizmos.DrawWireSphere(fOrigin + fDir * flameRange, endRadius);
    }
}
