using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

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
    public static event System.Action OnBossDefeated;
    [Header("=== THAM CHIẾU & DI CHUYỂN ===")]
    [Tooltip("Kéo đối tượng con IceGolemModel vào đây nếu script không tự tìm thấy")]
    public Animator anim;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 120f;
    public bool strafeMode = true;

    [Header("=== TẦNG 3 ÁNH SÁNG: HÀO QUANG BĂNG GIÁ (BOSS BODY LIGHT) ===")]
    [Tooltip("Bật đèn hào quang xanh băng tự thân bám theo thân Boss Ice Golem trong bóng tối")]
    [SerializeField] private bool enableBodyAuraLight = true;
    [SerializeField] private Color bodyAuraColor = new Color(0.25f, 0.75f, 1.0f);
    [SerializeField] private float bodyAuraRange = 16f;
    [SerializeField] private float bodyAuraIntensity = 3.0f;

    [Header("=== THỜI GIAN HỒI CHIÊU (COOLDOWNS) ===")]
    [Tooltip("Thời gian hồi chiêu Attack 1 (đấm thường)")]
    [SerializeField] private float attack1Cooldown = 1.8f;
    [Tooltip("Thời gian hồi chiêu GumoGumo (đấm đất liên hoàn)")]
    [SerializeField] private float gumoGumoCooldown = 10.0f;
    [Tooltip("Thời gian hồi chiêu Flame Breath (thổi lửa)")]
    [SerializeField] private float flameBreathCooldown = 14.0f;
    [Tooltip("Thời gian hồi chiêu Jump Attack (nhảy bổ vồ tới)")]
    [SerializeField] private float jumpAttackCooldown = 12.0f;
    [Tooltip("Thời gian hồi chiêu Run Attack (chạy húc tới - Nút 5)")]
    [SerializeField] private float runAttackCooldown = 9.0f;
    [Tooltip("Thời gian hồi chiêu AttackLT (combo 3 đòn - Nút 6)")]
    [SerializeField] private float attackLTCooldown = 5.0f;
    [Tooltip("Thời gian hồi chiêu Ice Ball (ném cầu tím - Nút 7)")]
    [SerializeField] private float iceBallCooldown = 14.0f;
    [Tooltip("Khoảng cách giãn cách tối thiểu giữa 2 kỹ năng lớn liên tiếp (giây) - Tránh spam chiêu")]
    [SerializeField] private float specialSkillGlobalCooldown = 3.0f;

    [Header("=== CÀI ĐẶT RUN ATTACK (NÚT 5 - CHẠY HÚC & VÒNG SÁNG TOẢ RA) ===")]
    [Tooltip("Tốc độ chạy lao về phía trước khi RunAttack (mét/giây)")]
    [SerializeField] private float runAttackSpeed = 12.0f;
    [Tooltip("Thời gian duy trì cú chạy húc (giây, khớp với hoạt ảnh 1.3s)")]
    [SerializeField] private float runAttackDuration = 1.25f;
    [Tooltip("Sát thương khi húc trúng Player")]
    [SerializeField] private float runAttackDamage = 35f;
    [Tooltip("Lực đẩy văng Player khi bị húc trúng")]
    [SerializeField] private float runAttackKnockback = 5.0f;
    [Tooltip("Bán kính vùng quét va chạm khi đang chạy húc (mét)")]
    [SerializeField] private float runAttackHitRadius = 2.2f;
    [Tooltip("TỰ CHỌN: Kéo thả Prefab VFX màu xanh dương bao quanh thân Boss khi chạy húc. Để trống = Không sinh hiệu ứng/vệt xanh")]
    [SerializeField] private GameObject runAuraVfxPrefab;

    [Header("=== CÀI ĐẶT ATTACK LT (NÚT 6 - 3 ĐÒN LIÊN HOÀN) ===")]
    [Tooltip("TỰ CHỌN: Kéo thả Prefab VFX bao quanh 2 bàn tay khi đánh 3 đòn liên tiếp. Để trống = Không sinh hiệu ứng/vòng khói")]
    [SerializeField] private GameObject handAuraVfxPrefab;
    [Tooltip("Sát thương đòn đánh thứ 1 và 2")]
    [SerializeField] private float attackLTDamage = 20f;
    [Tooltip("Sát thương đòn dứt điểm thứ 3")]
    [SerializeField] private float attackLTFinalDamage = 30f;
    [Tooltip("Lực đẩy lùi của đòn 1 và 2")]
    [SerializeField] private float attackLTKnockback = 1.8f;
    [Tooltip("Lực đẩy lùi của đòn thứ 3")]
    [SerializeField] private float attackLTFinalKnockback = 3.5f;
    [Tooltip("Thời điểm trúng đòn 1 (giây từ đầu animation)")]
    [SerializeField] private float attackLTHitTime1 = 0.8f;
    [Tooltip("Thời điểm trúng đòn 2 (giây)")]
    [SerializeField] private float attackLTHitTime2 = 1.7f;
    [Tooltip("Thời điểm trúng đòn 3 dứt điểm (giây)")]
    [SerializeField] private float attackLTHitTime3 = 2.6f;
    [Tooltip("Thời lượng tổng của chiêu thức (giây, animation 3.5s)")]
    [SerializeField] private float attackLTTotalDuration = 3.5f;

    [Header("=== CÀI ĐẶT ICE BALL (NÚT 7 - GỒNG CHIÊU & QUĂNG CẦU PURPLE ORB) ===")]
    [Tooltip("Prefab quả cầu năng lượng Purple Orb (Assets/Characters/Boss/IceGolem/VFX/Purple Orb.prefab)")]
    [SerializeField] private GameObject iceBallPrefab;
    [Tooltip("Thời gian gồng tụ lực trước khi quăng cầu (giây từ đầu animation)")]
    [SerializeField] private float iceBallCastDelay = 1.15f;
    [Tooltip("Thời lượng tổng của animation IceBallIG (giây, animation ~3.0s)")]
    [SerializeField] private float iceBallTotalDuration = 3.0f;
    [Tooltip("Tốc độ bay của quả cầu (mét/giây)")]
    [SerializeField] private float iceBallSpeed = 16.0f;
    [Tooltip("Sát thương khi quả cầu phát nổ trúng mục tiêu")]
    [SerializeField] private float iceBallDamage = 35.0f;
    [Tooltip("Bán kính nổ sát thương của quả cầu (mét)")]
    [SerializeField] private float iceBallExplosionRadius = 3.5f;
    [Tooltip("Lực đẩy văng khi dính vụ nổ cầu")]
    [SerializeField] private float iceBallKnockback = 2.5f;
    [Tooltip("Thời gian tồn tại tối đa của quả cầu (giây)")]
    [SerializeField] private float iceBallLifeTime = 4.0f;
    [Tooltip("Prefab hiệu ứng khi quả cầu phát nổ (nếu để trống tự động dùng Impact VFX)")]
    [SerializeField] private GameObject iceBallExplodeVfxPrefab;

    [Header("=== BANG THONG SO BOSS (CHINH TRUC TIEP TRONG UNITY INSPECTOR) ===")]
    [Tooltip("Mau toi da cua Boss (Tu dong dong bo vao Health component khi chay game va trong Editor)")]
    [SerializeField] private float bossMaxHealth = 1500f;
    [Tooltip("Thanh chiu don Poise toi da cua Boss (500) - Tieu hao khi bi danh theo do uy luc don danh, ve 0 se gay suy yeu WeakenIG")]
    [SerializeField] private float maxPoise = 500f;
    [Tooltip("Diem Poise chiu don hien tai cua Boss")]
    [SerializeField] private float currentPoise = 500f;
    [Tooltip("Thoi gian dung yen bat dong SAU KHI ket thuc clip WeakenIG (giay)")]
    [SerializeField] private float weakenFreezeAfterDuration = 0.0f;
    [Tooltip("Bu dap do cao nang model khi chao dao 50% HP (m) - Chong lun chan xuong san")]
    [SerializeField] private float phase2StaggerVisualHeightOffset = 0.0f;
    [Tooltip("Bu dap do cao nang model khi suy yeu Weaken (m) - Chong lun chan xuong san")]
    [SerializeField] private float weakenVisualHeightOffset = 0.0f;
    [Tooltip("Thoi luong cua clip WeakenIG (giay)")]
    [SerializeField] private float weakenAnimDuration = 3.6f;
    [Tooltip("Ten State hoat canh Weaken trong Animator")]
    [SerializeField] private string weakenStateName = "WeakenIG";

    [Header("=== EVENTS SỰ KIỆN BOSS ===")]
    public UnityEvent<float, float> onPoiseChanged; // (currentPoise, maxPoise) cho UI kết nối
    public UnityEvent<float, float> onStaminaChanged; // Alias tương thích ngược

    private bool isWeakened = false;
    private bool hasTriggeredWeaken = false;
    private Coroutine weakenCoroutine = null;

    public float BossMaxHealth => bossMaxHealth;
    public float CurrentPoise => currentPoise;
    public float MaxPoise => maxPoise;
    public float CurrentStamina => currentPoise;
    public float MaxStamina => maxPoise;
    public bool IsWeakened => isWeakened;
    public bool HasTriggeredWeaken => hasTriggeredWeaken;

    [Header("=== PHẢN HỒI NHẬN SÁT THƯƠNG & CHỚP ĐỎ GPU ===")]
    [Tooltip("Thời gian nháy đỏ mỗi khi dính đòn (giây)")]
    [SerializeField] private float hitFlashDuration = 0.12f;
    [Tooltip("Màu sắc nháy khi trúng đòn")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

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

    [Header("=== CÀI ĐẶT JUMP DOWN (ĐÁP XUỐNG MỞ MÀN DUY NHẤT 1 LẦN) ===")]
    [Tooltip("Tự động kích hoạt cú nhảy đáp xuống khi vừa bắt đầu game (Start) nếu Player đã có mặt")]
    [SerializeField] private bool jumpDownOnStart = true;
    [Tooltip("Tự động kích hoạt cú nhảy đáp xuống khi lần đầu tiên phát hiện Player xuất hiện trong tầm nhìn")]
    [SerializeField] private bool triggerJumpDownOnDetectPlayer = true;
    [Tooltip("Khoảng cách tối đa để phát hiện Player và kích hoạt cú nhảy đáp đất mở màn (mét)")]
    [SerializeField] private float playerDetectionRange = 35f;
    [Tooltip("Khoảng cách bay tới (mét) khi JumpDown")]
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
    [Tooltip("Tự động tìm Player làm mục tiêu tiếp đất (mặc định false để Boss chỉ rơi xuống tại vị trí của Boss và tỏa sóng lan ra)")]
    [SerializeField] private bool autoTargetPlayerOnJumpDown = false;
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
    [SerializeField] private float jumpAttackDistance = 20f;
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
    [SerializeField] private float jumpAttackShockwaveDelay = 0.23f;
    [Tooltip("ĐỘ RỘNG tối đa của vòng xoáy khi JumpAttack đập đất (mét)")]
    [SerializeField] private float jumpAttackShockwaveRadius = 18f;
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
    [Tooltip("THỜI ĐIỂM ĐÁNH TRÚNG / ĐỘ TRỄ VA CHẠM (giây từ đầu animation): Chỉnh để khớp lúc cánh tay vung tới mục tiêu (mặc định 1.23s chuẩn hoạt ảnh)")]
    [SerializeField] private float attack1HitDelay = 1.23f;
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
    [Tooltip("ĐỘ TO NHỎ / KÍCH THƯỚC của hiệu ứng Attack1 Effect khi đập đất (Scale, mặc định 2.0 tạo chấn động lớn)")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float gumoSlamVfxScale = 2.0f;
    [Tooltip("BÁN KÍNH SÁT THƯƠNG diện rộng của mỗi cú đập tay xuống sàn (mét) - TỰ DO ĐIỀU CHỈNH TẠI ĐÂY")]
    [SerializeField] private float gumoShockwaveRadius = 12.0f;
    [Tooltip("Sát thương mỗi cú đập / vòng sóng lan")]
    [SerializeField] private float gumoSlamDamage = 18f;
    [Tooltip("Lực đẩy nhẹ mục tiêu dính sóng")]
    [SerializeField] private float gumoSlamKnockback = 1.2f;

    [Header("GumoGumo Hand-To-Ground Contact Settings (Bắt Điểm Chạm Đất Bàn Tay - Chiêu 2)")]
    [Tooltip("Nâng độ cao model Boss khi thực hiện chiêu 2 (m) - Giúp bù đắp hoạt ảnh để chân không bị lún xuống sàn (mặc định 0.35m)")]
    [SerializeField] private float gumoVisualHeightOffset = 0.35f;
    [Tooltip("Độ cao bàn tay so với mặt sàn để bắt đầu vào vùng sẵn sàng va chạm (m)")]
    [SerializeField] private float gumoHandContactHeight = 0.55f;
    [Tooltip("Độ cao bàn tay chạm sát sàn coi như chạm đất ngay lập tức (m)")]
    [SerializeField] private float gumoHandMinHeight = 0.30f;
    [Tooltip("Độ cao bàn tay cần nhấc lên để nạp lại cú đập tiếp theo (m)")]
    [SerializeField] private float gumoHandReleaseHeight = 0.95f;
    [Tooltip("Độ lệch tiến về phía trước theo hướng mặt của bàn tay để đặt hiệu ứng nổ ngay tâm nắm đấm (m)")]
    [SerializeField] private float gumoHandForwardOffset = 0.20f;

    [Header("GumoGumo Timing (Dự phòng thời gian)")]
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
    [SerializeField] private float flameVisualLength = 3.58f;
    [Tooltip("HỆ SỐ ĐỘ RỘNG TIA LỬA VFX: Kéo tăng (> 1.0) để ngọn lửa toả rộng sang 2 bên và cuộn dày hơn")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float flameVisualWidth = 2.9f;
    [Tooltip("HỆ SỐ ĐỘ DÀY / MẬT ĐỘ HẠT LỬA (DENSITY): Tăng số này (> 1.0) để sinh nhiều hạt hơn mỗi giây, giúp ngọn lửa cuộn đặc kịt và không bị thưa khi kéo dài")]
    [Range(0.5f, 5.0f)]
    [SerializeField] private float flameDensity = 1.9f;

    [Header("Flame Hitbox & Damage (Tầm Quét Sát Thương Hình Nón)")]
    [Tooltip("TẦM XA sát thương của ngọn lửa (mét) - Tự do điều chỉnh theo độ dài của tia lửa")]
    [SerializeField] private float flameRange = 15.5f;
    [Tooltip("GÓC QUÉT HÌNH NÓN của ngọn lửa (độ) - Tự do điều chỉnh theo độ rộng của tia lửa (ví dụ: 45 - 65 độ)")]
    [SerializeField] private float flameConeAngle = 49f;
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
    [Tooltip("Bật/Tắt hiển thị vòng xoáy LineRenderer trên sàn (Mặc định TẮT để không làm bẩn sàn đấu)")]
    [SerializeField] private bool enableShockwaveRingVisual = false;
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

    [Header("=== GIAI ĐOẠN 2 CUỒNG NỘ (50% HP) & NHỊP THỞ COMBAT PACING ===")]
    [Tooltip("Khoảng nghỉ ngẫu nhiên tối thiểu giữa các chiêu ở Phase 1 (Máu > 50%): 6.0 giây")]
    [SerializeField] private float phase1PacingMin = 6.0f;
    [Tooltip("Khoảng nghỉ ngẫu nhiên tối đa giữa các chiêu ở Phase 1 (Máu > 50%): 7.0 giây")]
    [SerializeField] private float phase1PacingMax = 7.0f;
    [Tooltip("Khoảng nghỉ ngẫu nhiên tối thiểu giữa các chiêu ở Phase 2 (Máu <= 50% Cuồng Nộ): 4.0 giây")]
    [SerializeField] private float phase2PacingMin = 4.0f;
    [Tooltip("Khoảng nghỉ ngẫu nhiên tối đa giữa các chiêu ở Phase 2 (Máu <= 50% Cuồng Nộ): 5.0 giây")]
    [SerializeField] private float phase2PacingMax = 5.0f;
    [Tooltip("Tên State hoạt cảnh chao đảo khi chạm 50% HP")]
    [SerializeField] private string phase2StaggerStateName = "50%HP";
    [Tooltip("Thời lượng hoạt cảnh chao đảo 50% HP (giây) trước khi hất văng và phun lửa")]
    [SerializeField] private float phase2StaggerDuration = 4.2f;
    [Tooltip("Bán kính kiểm tra Player ở gần để hất văng khi dứt chao đảo (mét)")]
    [SerializeField] private float phase2PushRadius = 5.0f;
    [Tooltip("Cự ly đẩy lùi Player ra xa (mét) để vào đúng tầm phun lửa")]
    [SerializeField] private float phase2PushDistance = 4.5f;
    [Tooltip("Thời gian thực hiện cú đẩy lùi Player (giây)")]
    [SerializeField] private float phase2PushDuration = 0.25f;
    [Tooltip("TỰ CHỌN: Prefab VFX sóng chấn động khi hất văng Player (Để trống = Tự dùng VFX Gumo/Jump)")]
    [SerializeField] private GameObject phase2ShockwaveVfxPrefab;
    [Tooltip("TỰ CHỌN: Âm thanh gầm phát lực khi hất văng Player")]
    [SerializeField] private AudioClip phase2PushSfx;
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
    public bool IsDead => isDead;
    private bool isAttack1Active = false;
    private bool isJumpingDown = false;
    private bool isJumpingAttack = false;
    private bool isGumoGumoAttacking = false;
    private bool isFlameBreathing = false;
    private bool isRunAttacking = false;
    private bool isAttackLTActive = false;
    private bool isIceBallCasting = false;
    // Trạng thái Giai đoạn 2 & Chuyển pha
    private bool isPhase2 = false;
    private bool isPhase2Triggered = false;
    private bool isPhase2Transitioning = false;
    private float nextAttackAllowedTime = 0f;

    public bool IsPhase2 => isPhase2;
    public bool IsPhase2Transitioning => isPhase2Transitioning;
    public float NextAttackAllowedTime => nextAttackAllowedTime;
    public bool IsGumoGumoAttacking => isGumoGumoAttacking;
    public Transform TargetPlayer => playerTarget;
    public Vector3 GetAccurateGroundPosition(Vector3 searchPos) => GetGroundPosition(searchPos);

    private float lastLeftSlamTime = -1f;
    private float lastRightSlamTime = -1f;

    private bool isInitialStartJumpDown = true;
    private bool hasTriggeredJumpDown = false;
    private bool isCinematicStandby = false;
    public bool IsCinematicStandby => isCinematicStandby;
    private Rigidbody rb;
    private Collider bossCollider;
    private Health health;
    private Renderer[] bossRenderers;
    private Coroutine flashCoroutine;
    private static MaterialPropertyBlock flashMpb;
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    // Timestamps theo dõi thời điểm kích hoạt chiêu (Cooldown Tracking)
    private float lastAttack1Time = -999f;
    private float lastGumoGumoTime = -999f;
    private float lastFlameBreathTime = -999f;
    private float lastJumpAttackTime = -999f;
    private float lastRunAttackTime = -999f;
    private float lastAttackLTTime = -999f;
    private float lastIceBallTime = -999f;
    private float lastSpecialSkillTime = -999f;

    private GameObject activeLeftHandAura;
    private GameObject activeRightHandAura;
    private GameObject activeFlameInstance;
    private GameObject activeRunAuraInstance;
    private GameObject activeIceBallChargeInstance;
    private int cachedBoneAxisIndex = -1;
    private float cachedInitialMouthHeightOffset = -1f;

    // Cache Animator Hashes để tối ưu hiệu năng
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveTypeHash = Animator.StringToHash("MoveType");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int GumoGumoHash = Animator.StringToHash("AttackGumoGumo");
    private static readonly int RoaringHash = Animator.StringToHash("AttackRoaring");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
    private static readonly int JumpDownHash = Animator.StringToHash("JumpDown");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    // Hashes cho 3 kỹ năng mới (hỗ trợ cả tên có hoặc không có _IG)
    private static readonly int RunAttackHash = Animator.StringToHash("RunAttack");
    private static readonly int AttackLTHash = Animator.StringToHash("AttackLT");
    private static readonly int AttackLTIGHash = Animator.StringToHash("AttackLT_IG");
    private static readonly int IceBallHash = Animator.StringToHash("IceBall");
    private static readonly int IceBallIGHash = Animator.StringToHash("IceBallIG");
    private static readonly int WeakenHash = Animator.StringToHash("Weaken");

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

        // Dong bo ngay trong Unity Editor khi nguoi dung chinh sua thong so tren Inspector!
        SyncHealthToComponent();
    }

    /// <summary>
    /// Dong bo truc tiep thong so bossMaxHealth vao Health component
    /// </summary>
    public void ApplyBossStats()
    {
        SyncHealthToComponent();
        currentPoise = maxPoise;
        hasTriggeredWeaken = false;
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
            if (maxHpField != null)
            {
                maxHpField.SetValue(health, bossMaxHealth);
            }

            var curHpField = typeof(Health).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (curHpField != null)
            {
                curHpField.SetValue(health, bossMaxHealth);
            }

            var baseHpField = typeof(Health).GetField("baseMaxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (baseHpField != null)
            {
                baseHpField.SetValue(health, bossMaxHealth);
            }

            health.onHealthChanged?.Invoke(bossMaxHealth, bossMaxHealth);
        }
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
        if (anim != null) anim.transform.localPosition = Vector3.zero;
        if (health != null)
        {
            health.onTakeDamage.RemoveListener(HandleOnTakeDamage);
            health.onDeath.RemoveListener(Die);
        }
        ResetFlashColors();
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
        // TỰ BẢO VỆ: Chống nhân đôi script nếu vô tình add nhiều lần trên GameObject
        IceGolemController[] existingControllers = GetComponents<IceGolemController>();
        if (existingControllers.Length > 1 && existingControllers[0] != this)
        {
            Debug.LogWarning($"[IceGolemController] Phát hiện component thừa trên {gameObject.name}! Tự hủy instance này để tránh nhân đôi chiêu thức.");
            Destroy(this);
            return;
        }

        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();
        ApplyCombatPacing();
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        bossRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyBossStats();

        // Tự động tìm & xác thực xương bàn tay và miệng
        FindBones();
        ValidateMouthBone();
        ValidateHandBones();
        SetupHandHitboxes();

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
        ApplyBossStats();
        if (enableBodyAuraLight)
        {
            EnsureBodyAuraLight();
        }

        if (isCinematicStandby)
        {
            if (rb != null) rb.isKinematic = true;
            return;
        }

        // Tự tìm Player làm mục tiêu nếu chưa gán
        if (playerTarget == null && autoFindPlayer)
        {
            GetPlayerTarget();
        }

        if (jumpAttackTarget == null && autoTargetPlayer) jumpAttackTarget = playerTarget;
        if (jumpDownTarget == null && autoTargetPlayerOnJumpDown) jumpDownTarget = playerTarget;

        // Nhảy vào sân mở màn trận đấu nếu Player đã xuất hiện sẵn trong tầm phát hiện
        if (jumpDownOnStart && !hasTriggeredJumpDown)
        {
            Transform target = GetPlayerTarget();
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist <= playerDetectionRange)
                {
                    PlayJumpDown();
                }
            }
            else
            {
                PlayJumpDown();
            }
        }
    }

    void Update()
    {
        if (isDead) return;

        if (isCinematicStandby)
        {
            if (anim != null) anim.SetFloat(SpeedHash, 0f);
            return;
        }

        // Luôn bảo đảm có tham chiếu tới đối thủ Player
        if (playerTarget == null)
        {
            GetPlayerTarget();
        }

        // Tự động kích hoạt cú nhảy giáng trần mở màn DUY NHẤT 1 LẦN khi phát hiện Player bước vào phòng hầm ngục
        if (!hasTriggeredJumpDown && triggerJumpDownOnDetectPlayer)
        {
            Transform target = GetPlayerTarget();
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist <= playerDetectionRange)
                {
                    PlayJumpDown();
                }
            }
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
        if (anim == null || isDead || isWeakened) return true;
        if (isAttack1Active || isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing 
            || isRunAttacking || isAttackLTActive || isIceBallCasting) return true;

        // 1. Kiểm tra nếu Animator đang trong quá trình chuyển cảnh (Transition)
        if (anim.IsInTransition(0))
        {
            AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(0);
            if (!IsLocomotionState(nextState))
            {
                return true;
            }
        }

        // 2. Kiểm tra trạng thái hiện tại trên Layer 0
        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        return !IsLocomotionState(currentState);
    }

    private bool IsLocomotionState(AnimatorStateInfo state)
    {
        return state.IsName("Idle") 
            || state.IsName("Walk") 
            || state.IsName("WalkBackIG") 
            || state.IsName("WalkLeftIG") 
            || state.IsName("WalkRightIG");
    }

    /// <summary>
    /// Đọc phím điều khiển từ Input System mới (Hỗ trợ phím 1-7 và di chuyển 4 hướng)
    /// </summary>
    private void HandleNewInputSystem()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. Di chuyển bằng 4 phím mũi tên (Arrow Keys: Up, Down, Left, Right) - Tách biệt hoàn toàn với phím W, A, S, D của Player
        float horizontal = 0f;
        float vertical = 0f;

        bool hasArrowInput = keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
                             keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;
        if (hasArrowInput)
        {
            if (keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.rightArrowKey.isPressed) horizontal += 1f;

            Move(horizontal, vertical);
        }

        // 2. Tung chiêu khi không bận (Hỗ trợ cả Hàng phím số lẫn Bàn phím số Numpad)
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) CastAttack(0); // Attack 1
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) CastAttack(1); // GumoGumo Slam
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) CastAttack(2); // Flame Breath
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) CastAttack(3); // Jump Attack
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) CastAttack(4); // Run Attack (Nút 5)
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) CastAttack(5); // AttackLT_IG (Nút 6)
        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) CastAttack(6); // IceBallIG (Nút 7)

        // 3. Phím thử nghiệm: Chết (K). (Động tác JumpDown chỉ kích hoạt 1 lần duy nhất khi thấy Player, không được dùng đi dùng lại)
        if (keyboard.kKey.wasPressedThisFrame) Die();
    }

    // ==========================================
    // PHẢN HỒI NHẬN SÁT THƯƠNG & CHỚP ĐỎ GPU
    // ==========================================

    private void HandleOnTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockback)
    {
        if (isDead) return;

        // 1. Tinh sat thuong Poise theo gia tri Knockback cua don danh (chuan theo co che Poise trong PlayerFeedback & EnemyFeedback)
        // - Heavy/Counter (knockback >= 1.8f): Counter Attack (2.0f), Skill Q AoeSpell (2.5f) -> 100 Poise
        // - Medium-Heavy (knockback >= 1.0f): Charge Attack Hit 2 (1.2f), Skill E ProjectileSlash (1.0f) -> 70 Poise
        // - Medium (knockback > 0f): Attack 3 (0.8f), Charge Attack Hit 1 (0.6f) -> 50 Poise
        // - Light (knockback == 0f): Attack 1 & Attack 2 -> 35 Poise
        float poiseDmg = 35f;
        if (knockback >= 1.8f) poiseDmg = 100f;
        else if (knockback >= 1.0f) poiseDmg = 70f;
        else if (knockback > 0f) poiseDmg = 50f;

        TakePoiseDamage(poiseDmg);

        // 2. Neu dang trong trang thai Weaken ma mau tut xuong <= 50% HP:
        // Huy trang thai Weaken/dung yen va kich hoat ngay chuoi Phase 2!
        if (isWeakened && health != null && health.CurrentHealth <= health.MaxHealth * 0.5f)
        {
            CancelWeakenForPhase2();
        }
        else if (!isWeakened)
        {
            CheckPhase2Transition();
        }

        FlashHitFeedback();
    }

    /// <summary>
    /// Kích hoạt hiệu ứng chớp đỏ GPU khi trúng đòn (không clone Material, bảo toàn 100% SRP Batcher)
    /// </summary>
    public void FlashHitFeedback()
    {
        if (bossRenderers == null || bossRenderers.Length == 0) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashHitRoutine());
    }

    private System.Collections.IEnumerator FlashHitRoutine()
    {
        if (flashMpb == null) flashMpb = new MaterialPropertyBlock();
        flashMpb.SetColor(BaseColorProp, hitFlashColor);
        flashMpb.SetColor(ColorProp, hitFlashColor);

        for (int i = 0; i < bossRenderers.Length; i++)
        {
            if (bossRenderers[i] != null && bossRenderers[i].enabled)
            {
                bossRenderers[i].SetPropertyBlock(flashMpb);
            }
        }

        yield return new WaitForSeconds(hitFlashDuration);

        ResetFlashColors();
        flashCoroutine = null;
    }

    private void ResetFlashColors()
    {
        if (bossRenderers == null) return;
        for (int i = 0; i < bossRenderers.Length; i++)
        {
            if (bossRenderers[i] != null)
            {
                bossRenderers[i].SetPropertyBlock(null);
            }
        }
    }

    /// <summary>
    /// Nhận sát thương Poise (thanh chịu đòn) từ các đòn đánh của Player theo kiến trúc Souls-like.
    /// Khi Poise tụt về 0 (<= 0.01f), Boss sẽ kích hoạt trạng thái suy yếu WeakenIG (duy nhất 1 lần).
    /// </summary>
    public void TakePoiseDamage(float poiseDamage)
    {
        if (isDead || hasTriggeredWeaken || poiseDamage <= 0f) return;

        currentPoise = Mathf.Max(0f, currentPoise - poiseDamage);
        onPoiseChanged?.Invoke(currentPoise, maxPoise);
        onStaminaChanged?.Invoke(currentPoise, maxPoise);

        if (currentPoise <= 0.01f)
        {
            TriggerWeaken();
        }
    }

    /// <summary>
    /// Các hàm tương thích ngược và alias phụ trợ
    /// </summary>
    public void TakeStaminaDamage(float amount) => TakePoiseDamage(amount);
    public void DeductPoise(float amount) => TakePoiseDamage(amount);
    public void DeductStamina(float amount) => TakePoiseDamage(amount);

    public void TriggerWeaken()
    {
        if (hasTriggeredWeaken || isDead || isPhase2Transitioning) return;
        hasTriggeredWeaken = true;
        if (weakenCoroutine != null) StopCoroutine(weakenCoroutine);
        weakenCoroutine = StartCoroutine(WeakenRoutine());
    }

    private void CancelWeakenForPhase2()
    {
        if (weakenCoroutine != null)
        {
            StopCoroutine(weakenCoroutine);
            weakenCoroutine = null;
        }
        isWeakened = false;
        if (anim != null)
        {
            anim.speed = 1f;
            anim.transform.localPosition = Vector3.zero;
        }
        CheckPhase2Transition();
    }

    private void CommitWeakenPosition(Transform modelTransform, Vector3 originalModelLocalPos)
    {
        if (modelTransform == null) return;

        // 1. Lay vi tri the gioi thuc te ma model/hips da buoc lui toi
        Vector3 targetWorldPos = modelTransform.position;

        // Kiem tra du phong neu root motion tac dong len xuong Hips thay vi modelTransform
        Transform hips = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
        if (hips == null && anim != null) hips = anim.transform.Find("mixamorig:Hips");
        if (hips != null && Vector3.Distance(new Vector3(modelTransform.position.x, 0f, modelTransform.position.z), new Vector3(transform.position.x, 0f, transform.position.z)) < 0.1f)
        {
            float hipsDist = Vector3.Distance(new Vector3(hips.position.x, 0f, hips.position.z), new Vector3(transform.position.x, 0f, transform.position.z));
            if (hipsDist > 0.1f)
            {
                targetWorldPos = new Vector3(hips.position.x, transform.position.y, hips.position.z);
            }
        }

        // 2. Bat mat san an toan tai vi tri moi de khong bi lo lung hay lun
        Vector3 groundPos = GetGroundPosition(targetWorldPos);
        Vector3 finalRootPos = new Vector3(targetWorldPos.x, groundPos.y, targetWorldPos.z);

        // 3. Cap nhat vi tri moi vao ROOT transform cua Boss (keo theo Collider, Rigidbody va AI)
        transform.position = finalRootPos;

        // 4. Dat lai localPosition cua model ve goc de model nam khop 100% voi Collider tai vi tri moi
        modelTransform.localPosition = originalModelLocalPos + Vector3.up * weakenVisualHeightOffset;

        // 5. Triet tieu van toc vat ly
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private System.Collections.IEnumerator WeakenRoutine()
    {
        isWeakened = true;

        // Dung cac don danh dang thi trien do dang
        CancelActiveAttackCoroutines();

        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // Triet tieu van toc vat ly de khong bi truot
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        bool prevRootMotion = anim != null && anim.applyRootMotion;
        if (anim != null)
        {
            anim.applyRootMotion = true;
        }

        Transform modelTransform = anim != null ? anim.transform : null;
        Vector3 originalModelLocalPos = modelTransform != null ? modelTransform.localPosition : Vector3.zero;

        if (modelTransform != null && weakenVisualHeightOffset != 0f)
        {
            modelTransform.localPosition = originalModelLocalPos + Vector3.up * weakenVisualHeightOffset;
        }

        // Xoa sach trigger ton dong truoc khi kich hoat de khong bao gio bi lap lai
        SafeResetTrigger(WeakenHash);

        // 1. Kich hoat hoat canh WeakenIG truc tiep bang CrossFadeInFixedTime
        int weakenHash = Animator.StringToHash(weakenStateName);
        if (anim != null)
        {
            anim.SetFloat(SpeedHash, 0f);
            if (anim.HasState(0, weakenHash))
            {
                anim.CrossFadeInFixedTime(weakenStateName, 0.15f);
            }
            else
            {
                anim.Play(weakenStateName, 0, 0f);
            }
        }

        // Xoa ngay trigger Weaken de AnyState khong bao gio nhan trigger lan nua
        SafeResetTrigger(WeakenHash);

        // 2. Cho Animator vao state WeakenIG
        yield return null;
        float enterTimeout = 0.6f;
        while (enterTimeout > 0f && anim != null)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == weakenHash || info.IsName(weakenStateName))
            {
                break;
            }
            enterTimeout -= Time.deltaTime;
            yield return null;
        }

        // 3. Cho dong tac WeakenIG chay HET TOAN BO do dai clip theo dung toc do ban tua cham (speed = 0.5)
        float maxTimeout = 20f; // Timeout an toan phong ngua
        while (maxTimeout > 0f && !isDead)
        {
            // Neu bi danh tut xuong <= 50% HP trong luc dang Weaken thi lap tuc chot vi tri va chuyen Phase 2
            if (health != null && health.CurrentHealth <= health.MaxHealth * 0.5f)
            {
                CommitWeakenPosition(modelTransform, originalModelLocalPos);
                SafeResetTrigger(WeakenHash);
                if (anim != null)
                {
                    anim.applyRootMotion = prevRootMotion;
                }
                CancelWeakenForPhase2();
                yield break;
            }

            if (anim != null)
            {
                AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == weakenHash || info.IsName(weakenStateName))
                {
                    // Khi clip da chay gan het (normalizedTime >= 0.95f) hoac Animator dang transition sang Idle
                    if (info.normalizedTime >= 0.95f || anim.IsInTransition(0))
                    {
                        break;
                    }
                }
                else if (enterTimeout <= 0f)
                {
                    // Da thoat sang state Idle
                    break;
                }
            }
            else
            {
                break;
            }

            maxTimeout -= Time.deltaTime;
            yield return null;
        }

        if (isDead)
        {
            CommitWeakenPosition(modelTransform, originalModelLocalPos);
            SafeResetTrigger(WeakenHash);
            if (anim != null) anim.applyRootMotion = prevRootMotion;
            isWeakened = false;
            yield break;
        }

        // 4. CHỐT VỊ TRÍ LÙI: Chuyen toan bo toa do buoc lui vao ROOT transform cua Boss (chong giat nguoc)
        CommitWeakenPosition(modelTransform, originalModelLocalPos);

        // Xoa trigger de tuyet doi khong the lap lai
        SafeResetTrigger(WeakenHash);

        // 5. KET THUC WEAKEN: Khoi phuc root motion goc, chuyen sang Idle va cho Boss tiep tuc danh NGAY LAP TUC!
        if (anim != null)
        {
            anim.applyRootMotion = prevRootMotion;
            AnimatorStateInfo currentInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.shortNameHash == weakenHash || currentInfo.IsName(weakenStateName))
            {
                anim.CrossFadeInFixedTime("Idle", 0.25f);
            }
        }

        if (modelTransform != null)
        {
            modelTransform.localPosition = originalModelLocalPos;
        }

        isWeakened = false;
        weakenCoroutine = null;

        if (isDead) yield break;

        // Neu sau khi het Weaken ma mau da <= 50% HP thi kich hoat Phase 2 ngay
        if (health != null && health.CurrentHealth <= health.MaxHealth * 0.5f)
        {
            CheckPhase2Transition();
        }
        else
        {
            ApplyCombatPacing();
        }
    }

    private void CheckPhase2Transition()
    {
        if (isPhase2Triggered || isDead || isPhase2Transitioning) return;
        if (health != null && health.CurrentHealth <= health.MaxHealth * 0.5f)
        {
            StartCoroutine(Phase2TransitionRoutine());
        }
    }

    private IEnumerator Phase2TransitionRoutine()
    {
        isPhase2Triggered = true;
        isPhase2Transitioning = true;

        // Dừng và dọn dẹp các đòn đánh đang thi triển dở dang
        CancelActiveAttackCoroutines();

        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // 1. Kích hoạt hoạt cảnh chao đảo 50%HP
        Transform staggerModelTf = anim != null ? anim.transform : null;
        Vector3 origStaggerLocalPos = staggerModelTf != null ? staggerModelTf.localPosition : Vector3.zero;

        // Bu dap do cao model de triet tieu viec chan bi lun xuong dat khi chao dao 50% HP
        if (staggerModelTf != null && phase2StaggerVisualHeightOffset != 0f)
        {
            staggerModelTf.localPosition = origStaggerLocalPos + Vector3.up * phase2StaggerVisualHeightOffset;
        }

        if (anim != null)
        {
            anim.SetFloat(SpeedHash, 0f);
            anim.SetTrigger("50%HP");
            int staggerHash = Animator.StringToHash(phase2StaggerStateName);
            if (anim.HasState(0, staggerHash))
            {
                anim.CrossFadeInFixedTime(phase2StaggerStateName, 0.15f);
            }
            else
            {
                anim.Play(phase2StaggerStateName, 0, 0f);
            }
        }

        // Chờ hết thời lượng hoạt ảnh chao đảo (có kiểm tra hoàn tất normalizedTime)
        float staggerTimer = 0f;
        yield return new WaitForSeconds(0.25f);
        staggerTimer += 0.25f;

        while (staggerTimer < phase2StaggerDuration && !isDead)
        {
            if (anim != null)
            {
                AnimatorStateInfo stInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stInfo.IsName(phase2StaggerStateName) && stInfo.normalizedTime >= 0.95f)
                {
                    break;
                }
            }
            staggerTimer += Time.deltaTime;
            yield return null;
        }

        // Tra lai do cao goc cua model sau khi dut dong tac chao dao 50% HP
        if (staggerModelTf != null)
        {
            staggerModelTf.localPosition = origStaggerLocalPos;
        }

        if (isDead)
        {
            isPhase2Transitioning = false;
            yield break;
        }

        // 2. Dứt động tác 50%HP: Tự động hất văng Player ra xa 1 khoảng nếu đang ở gần (< 5m)
        Transform playerTf = GetPlayerTarget();
        if (playerTf != null)
        {
            float dist = Vector3.Distance(transform.position, playerTf.position);
            if (dist < phase2PushRadius)
            {
                Vector3 pushDir = (playerTf.position - transform.position);
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = transform.forward;
                else pushDir.Normalize();

                StartCoroutine(PushPlayerBackRoutine(playerTf, pushDir, phase2PushDistance, phase2PushDuration));

                // Sóng chấn động và âm thanh phát lực hất văng
                SpawnPhase2PushVfx(transform.position + Vector3.up * 0.5f);
                if (phase2PushSfx != null)
                {
                    AudioSource.PlayClipAtPoint(phase2PushSfx, transform.position);
                }
            }
        }

        // 3. Đánh dấu chính thức kích hoạt Phase 2 (Mở khóa Skill 3 & Skill 7)
        isPhase2 = true;
        isPhase2Transitioning = false;

        // Xóa trigger chao đảo để tránh kích hoạt lại
        if (anim != null)
        {
            anim.ResetTrigger(phase2StaggerStateName);
        }

        // Đợi cú hất văng kết thúc để Player lùi về đúng cự ly phun lửa
        yield return new WaitForSeconds(phase2PushDuration);

        if (isDead)
        {
            isPhase2Transitioning = false;
            yield break;
        }

        // 4. Bước 1: Tung ngay Kỹ năng Roaring (Phun lửa Flame Breath)
        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }
        TriggerAttackCooldown(2);
        yield return StartCoroutine(FlameBreathRoutine());

        if (isDead)
        {
            isPhase2Transitioning = false;
            yield break;
        }

        // 5. Bước 2: Sau Roaring thì chờ đúng 4 giây (giữ cự ly, nhìn Player)
        float wait4s = 3.0f; // Da doi thanh cho dung 3s theo yeu cau
        while (wait4s > 0f && !isDead)
        {
            wait4s -= Time.deltaTime;
            Transform target = GetPlayerTarget();
            if (target != null)
            {
                SmoothFaceTarget(target, 120f);
            }
            yield return null;
        }

        if (isDead)
        {
            isPhase2Transitioning = false;
            yield break;
        }

        // 6. Bước 3: Sau 4s, thực hiện kỹ năng 7 ném cầu (Ice Ball - bắn 3 quả cầu băng)
        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }
        TriggerAttackCooldown(6);
        yield return StartCoroutine(IceBallRoutine());

        // 7. Bước 4: Hoàn tất chuỗi mở màn Phase 2! Trả quyền để Boss tiếp tục tự tính toán sử dụng skill hợp lý
        isPhase2Transitioning = false;
        ApplyCombatPacing();
    }

    private void CancelActiveAttackCoroutines()
    {
        isAttack1Active = false;
        isGumoGumoAttacking = false;
        isFlameBreathing = false;
        isJumpingAttack = false;
        isRunAttacking = false;
        isAttackLTActive = false;
        isIceBallCasting = false;

        if (activeFlameInstance != null)
        {
            Destroy(activeFlameInstance);
            activeFlameInstance = null;
        }
        if (activeIceBallChargeInstance != null)
        {
            Destroy(activeIceBallChargeInstance);
            activeIceBallChargeInstance = null;
        }

        DisableHandAuras();
        DisableRunAura();
    }

    private IEnumerator PushPlayerBackRoutine(Transform playerTf, Vector3 pushDir, float distance, float duration)
    {
        if (playerTf == null) yield break;
        CharacterController cc = playerTf.GetComponent<CharacterController>();
        float elapsed = 0f;
        float speed = distance / Mathf.Max(duration, 0.05f);

        while (elapsed < duration && playerTf != null)
        {
            float step = speed * Time.deltaTime;
            if (cc != null && cc.enabled)
            {
                cc.Move(pushDir * step);
            }
            else
            {
                playerTf.position += pushDir * step;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SpawnPhase2PushVfx(Vector3 position)
    {
        GameObject vfxPrefab = phase2ShockwaveVfxPrefab != null 
            ? phase2ShockwaveVfxPrefab 
            : (gumoShockwaveVfx != null ? gumoShockwaveVfx : jumpAttackShockwaveVfx);
            
        if (vfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(vfxPrefab, position, Quaternion.identity);
            vfxInstance.transform.localScale = Vector3.one * 1.5f;
            Destroy(vfxInstance, 3.0f);
        }
    }

    public void ApplyCombatPacing()
    {
        float delay = isPhase2 ? Random.Range(phase2PacingMin, phase2PacingMax) : Random.Range(phase1PacingMin, phase1PacingMax);
        nextAttackAllowedTime = Time.time + delay;
    }

        public bool CanCastAttack(int attackIndex)
    {
        if (IsBusy() || isPhase2Transitioning) return false;

        // Kỹ năng 3 (Flame Breath - index 2) và Kỹ năng 7 (Ice Ball - index 6) bị KHÓA ở Phase 1:
        if (!isPhase2 && (attackIndex == 2 || attackIndex == 6))
        {
            return false;
        }

        // Kiểm tra nhịp nghỉ Combat Pacing (6-7s ở Phase 1, 4-5s ở Phase 2):
        if (Time.time < nextAttackAllowedTime)
        {
            return false;
        }

        // Nếu là chiêu lớn (1: GumoGumo, 2: Flame, 3: JumpAttack, 4: RunAttack, 6: IceBall), phải cách chiêu lớn trước ít nhất specialSkillGlobalCooldown
        bool isSpecial = (attackIndex == 1 || attackIndex == 2 || attackIndex == 3 || attackIndex == 4 || attackIndex == 6);
        if (isSpecial && Time.time < lastSpecialSkillTime + specialSkillGlobalCooldown)
        {
            return false;
        }

        switch (attackIndex)
        {
            case 0: return Time.time >= lastAttack1Time + attack1Cooldown;
            case 1: return Time.time >= lastGumoGumoTime + gumoGumoCooldown;
            case 2: return Time.time >= lastFlameBreathTime + flameBreathCooldown;
            case 3: return Time.time >= lastJumpAttackTime + jumpAttackCooldown;
            case 4: return Time.time >= lastRunAttackTime + runAttackCooldown;
            case 5: return Time.time >= lastAttackLTTime + attackLTCooldown;
            case 6: return Time.time >= lastIceBallTime + iceBallCooldown;
            default: return false;
        }
    }

    /// <summary>
    /// Trả về tỷ lệ hồi chiêu chuẩn hóa từ 0.0 (đã sẵn sàng) đến 1.0 (vừa dùng xong) - Dành riêng cho ML-Agents
    /// </summary>
    public float GetCooldownNormalized(int attackIndex)
    {
        float remaining = GetRemainingCooldown(attackIndex);
        float maxCd = GetMaxCooldown(attackIndex);
        if (maxCd <= 0.0001f) return 0f;
        return Mathf.Clamp01(remaining / maxCd);
    }

    /// <summary>
    /// Trả về số giây còn lại trước khi chiêu hồi xong
    /// </summary>
    public float GetRemainingCooldown(int attackIndex)
    {
        float lastTime;
        float maxCd;
        switch (attackIndex)
        {
            case 0: lastTime = lastAttack1Time; maxCd = attack1Cooldown; break;
            case 1: lastTime = lastGumoGumoTime; maxCd = gumoGumoCooldown; break;
            case 2: lastTime = lastFlameBreathTime; maxCd = flameBreathCooldown; break;
            case 3: lastTime = lastJumpAttackTime; maxCd = jumpAttackCooldown; break;
            case 4: lastTime = lastRunAttackTime; maxCd = runAttackCooldown; break;
            case 5: lastTime = lastAttackLTTime; maxCd = attackLTCooldown; break;
            case 6: lastTime = lastIceBallTime; maxCd = iceBallCooldown; break;
            default: return 0f;
        }
        return Mathf.Max(0f, (lastTime + maxCd) - Time.time);
    }

    public float GetMaxCooldown(int attackIndex)
    {
        switch (attackIndex)
        {
            case 0: return attack1Cooldown;
            case 1: return gumoGumoCooldown;
            case 2: return flameBreathCooldown;
            case 3: return jumpAttackCooldown;
            case 4: return runAttackCooldown;
            case 5: return attackLTCooldown;
            case 6: return iceBallCooldown;
            default: return 1f;
        }
    }

    private void TriggerAttackCooldown(int attackIndex)
    {
        bool isSpecial = (attackIndex == 1 || attackIndex == 2 || attackIndex == 3 || attackIndex == 4 || attackIndex == 6);
        if (isSpecial)
        {
            lastSpecialSkillTime = Time.time;
        }

        switch (attackIndex)
        {
            case 0: lastAttack1Time = Time.time; break;
            case 1: lastGumoGumoTime = Time.time; break;
            case 2: lastFlameBreathTime = Time.time; break;
            case 3: lastJumpAttackTime = Time.time; break;
            case 4: lastRunAttackTime = Time.time; break;
            case 5: lastAttackLTTime = Time.time; break;
            case 6: lastIceBallTime = Time.time; break;
        }
    }

    /// <summary>
    /// Đặt lại toàn bộ thời gian hồi chiêu về 0 (sẵn sàng sử dụng ngay) - Dùng khi bắt đầu Episode mới trong ML-Agents
    /// </summary>
    public void ResetAllCooldowns()
    {
        lastAttack1Time = -999f;
        lastGumoGumoTime = -999f;
        lastFlameBreathTime = -999f;
        lastJumpAttackTime = -999f;
        lastRunAttackTime = -999f;
        lastAttackLTTime = -999f;
        lastIceBallTime = -999f;
        lastSpecialSkillTime = -999f;
    }

    // ==========================================
    // CÁC HÀM CÔNG KHAI (DÙNG CHO PHÍM HOẶC ML-AGENTS)
    // ==========================================

    /// <summary>
    /// Di chuyển 4 hướng linh hoạt (Tới: Walk, Lùi: WalkBackIG, Trái: WalkLeftIG, Phải: WalkRightIG)
    /// </summary>
    public void Move(float horizontal, float vertical)
    {
        if (IsBusy()) return;

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        float inputMag = inputDir.magnitude;
        int moveType = 0; // 0 = Idle, 1 = Forward, 2 = Backward, 3 = Left, 4 = Right

        if (inputMag >= 0.1f)
        {
            inputDir.Normalize();

            if (strafeMode)
            {
                Transform target = GetPlayerTarget();
                if (target != null)
                {
                    SmoothFaceTarget(target, facePlayerSpeed * 1.5f);
                }

                Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                transform.position += moveDir * moveSpeed * Time.deltaTime;

                // Xác định hướng di chuyển chính để kích hoạt animation tương ứng
                if (Mathf.Abs(vertical) >= Mathf.Abs(horizontal))
                {
                    moveType = (vertical > 0f) ? 1 : 2; // 1 = Walk tới, 2 = WalkBackIG lùi
                }
                else
                {
                    moveType = (horizontal > 0f) ? 4 : 3; // 4 = WalkRightIG phải, 3 = WalkLeftIG trái
                }
            }
            else
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, TurnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                moveType = 1;
            }

            anim.SetFloat(SpeedHash, inputMag);
            anim.SetInteger(MoveTypeHash, moveType);
            anim.SetFloat(MoveXHash, horizontal);
            anim.SetFloat(MoveZHash, vertical);
        }
        else
        {
            anim.SetFloat(SpeedHash, 0f);
            anim.SetInteger(MoveTypeHash, 0); // 0 = Idle
            anim.SetFloat(MoveXHash, 0f);
            anim.SetFloat(MoveZHash, 0f);
        }
    }

    public void CastAttack(int attackIndex)
    {
        if (IsBusy()) return;
        if (!CanCastAttack(attackIndex)) return;

        switch (attackIndex)
        {
            case 0: PlayAttack1(); break;
            case 1: PlayGumoGumoAttack(); break;
            case 2: PlayFlameBreathAttack(); break;
            case 3: PlayJumpAttack(); break;
            case 4: PlayRunAttack(); break;
            case 5: PlayAttackLT(); break;
            case 6: PlayIceBall(); break;
        }
    }

    // =========================================================================
    // COROUTINE ATTACK 1 (BASIC ATTACK - CÚ ĐÁNH CƠ BẢN VÀ HIỆU ỨNG IMPACT)
    // =========================================================================

    public void PlayAttack1()
    {
        if (isAttack1Active || isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing) return;
        TriggerAttackCooldown(0);
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
        ApplyCombatPacing();
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
        // JumpDown chỉ được phép kích hoạt 1 lần duy nhất khi mới bắt đầu thấy Player, tuyệt đối không dùng đi dùng lại
        if (hasTriggeredJumpDown) return;
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        hasTriggeredJumpDown = true;
        StartCoroutine(JumpDownRoutine());
    }

    public void PlayJumpAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(3);
        StartCoroutine(JumpAttackRoutine());
    }

    public void PlayGumoGumoAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(1);
        StartCoroutine(GumoGumoRoutine());
    }

    public void PlayFlameBreathAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(2);
        StartCoroutine(FlameBreathRoutine());
    }

    public void PlayRunAttack()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(4);
        StartCoroutine(RunAttackRoutine());
    }

    public void PlayAttackLT()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(5);
        StartCoroutine(AttackLTRoutine());
    }

    public void PlayIceBall()
    {
        if (isJumpingDown || isJumpingAttack || isGumoGumoAttacking || isFlameBreathing || isRunAttacking || isAttackLTActive || isIceBallCasting) return;
        TriggerAttackCooldown(6);
        StartCoroutine(IceBallRoutine());
    }

    private void SafeResetTrigger(int paramHash)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.nameHash == paramHash)
            {
                anim.ResetTrigger(paramHash);
                return;
            }
        }
    }

    private void SafeResetTrigger(string paramName)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.name.Equals(paramName, System.StringComparison.OrdinalIgnoreCase))
            {
                anim.ResetTrigger(p.nameHash);
                return;
            }
        }
    }

    private void SafeSetTrigger(int paramHash)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.nameHash == paramHash)
            {
                anim.SetTrigger(paramHash);
                return;
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isWeakened = false;
        if (weakenCoroutine != null)
        {
            StopCoroutine(weakenCoroutine);
            weakenCoroutine = null;
        }
        if (anim != null) anim.speed = 1f;
        OnBossDefeated?.Invoke();

        if (activeFlameInstance != null)
        {
            Destroy(activeFlameInstance);
            activeFlameInstance = null;
        }

        if (activeIceBallChargeInstance != null)
        {
            Destroy(activeIceBallChargeInstance);
            activeIceBallChargeInstance = null;
        }

        DisableHandAuras();
        DisableRunAura();
        StopAllCoroutines();
        ResetFlashColors();

        // Tắt Collider để người chơi không tiếp tục chém trúng xác
        if (bossCollider != null) bossCollider.enabled = false;
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (anim != null)
        {
            anim.SetFloat(SpeedHash, 0f);
            SafeResetTrigger(DeadHash);
            SafeResetTrigger(WeakenHash);
            anim.speed = 1f;
            anim.CrossFadeInFixedTime("Dead", 0.15f);
        }
    }

    /// <summary>
    /// Hồi sinh Boss để bắt đầu hiệp đấu mới (Reset trạng thái chết, bật lại Collider, reset Animation và Cooldowns)
    /// </summary>
    public void Revive()
    {
        isDead = false;
        isPhase2 = false;
        ApplyBossStats();
        isWeakened = false;
        if (weakenCoroutine != null)
        {
            StopCoroutine(weakenCoroutine);
            weakenCoroutine = null;
        }
        isPhase2Triggered = false;
        isPhase2Transitioning = false;
        ApplyCombatPacing();
        isAttack1Active = false;
        isJumpingDown = false;
        isJumpingAttack = false;
        isGumoGumoAttacking = false;
        isFlameBreathing = false;
        isRunAttacking = false;
        isAttackLTActive = false;
        isIceBallCasting = false;

        if (activeFlameInstance != null)
        {
            Destroy(activeFlameInstance);
            activeFlameInstance = null;
        }

        if (activeIceBallChargeInstance != null)
        {
            Destroy(activeIceBallChargeInstance);
            activeIceBallChargeInstance = null;
        }

        DisableHandAuras();
        DisableRunAura();
        ResetFlashColors();
        StopAllCoroutines();

        if (bossCollider != null) bossCollider.enabled = true;
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.speed = 1f;
            anim.applyRootMotion = false;
            SafeResetTrigger(DeadHash);
            SafeResetTrigger(RoaringHash);
            SafeResetTrigger(JumpAttackHash);
            SafeResetTrigger(GumoGumoHash);
            SafeResetTrigger(RunAttackHash);
            SafeResetTrigger(AttackLTIGHash);
            SafeResetTrigger(IceBallIGHash);
            SafeResetTrigger(WeakenHash);
            SafeResetTrigger(phase2StaggerStateName);
            anim.Play("Idle", 0, 0f);
            anim.SetFloat(SpeedHash, 0f);
        }

        ResetAllCooldowns();
        CleanupResidualShockwaves();
    }

    /// <summary>
    /// Dọn sạch mọi đối tượng vòng xoáy LineRenderer còn tồn đọng trên sàn đấu
    /// </summary>
    public void CleanupResidualShockwaves()
    {
        GameObject[] oldRings = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in oldRings)
        {
            if (obj != null && obj.name.StartsWith("ClockwiseShockwaveRing"))
            {
                Destroy(obj);
            }
        }
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

        // 1. Kích hoạt animation AttackRoaring (dùng CrossFade và reset sạch trigger để không bao giờ bị phát lặp lại lần 2)
        if (anim != null)
        {
            anim.ResetTrigger(phase2StaggerStateName);
            anim.ResetTrigger(RoaringHash);
            anim.CrossFadeInFixedTime("AttackRoaring", 0.15f);
        }

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

        // Triệt tiêu trigger Roaring để ngăn chặn việc bị kích hoạt lặp lại lần 2
        if (anim != null)
        {
            anim.ResetTrigger(RoaringHash);
        }

        isFlameBreathing = false;
        ApplyCombatPacing();
    }

    // =========================================================================
    // COROUTINE RUN ATTACK (NÚT 5 - CHẠY LAO TỚI HÚC VÀ VÒNG SÁNG TOẢ RA)
    // =========================================================================

    private IEnumerator RunAttackRoutine()
    {
        isRunAttacking = true;

        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // Kích hoạt trigger animation RunAttack
        anim.SetTrigger(RunAttackHash);

        // Kích hoạt vòng sáng/hào quang bao quanh Boss khi chạy
        EnableRunAura();

        float timer = 0f;
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        while (timer < runAttackDuration)
        {
            timer += Time.deltaTime;

            // Xoay nhẹ bám theo mục tiêu trong nửa đầu cú chạy để tăng độ chuẩn xác
            if (timer < runAttackDuration * 0.5f)
            {
                Transform target = GetPlayerTarget();
                if (target != null)
                {
                    SmoothFaceTarget(target, facePlayerSpeed * 0.8f);
                }
            }

            // Di chuyển lao nhanh về phía trước
            transform.position += transform.forward * runAttackSpeed * Time.deltaTime;

            // Quét va chạm húc trúng người chơi
            PerformRunAttackHitCheck(hitDamageables);

            yield return null;
        }

        DisableRunAura();

        // Chờ animation RunAttack chuyển hoàn tất về thế Idle (kèm timeout chống kẹt)
        float runSafetyTimeout = 1.0f;
        while (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("RunAttack") && runSafetyTimeout > 0f)
        {
            runSafetyTimeout -= Time.deltaTime;
            yield return null;
        }

        isRunAttacking = false;
        ApplyCombatPacing();
    }

    private void PerformRunAttackHitCheck(HashSet<IDamageable> hitDamageables)
    {
        Vector3 checkCenter = transform.position + transform.forward * 1.6f + Vector3.up * 1.3f;
        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hits = Physics.OverlapSphere(checkCenter, runAttackHitRadius, queryMask);

        foreach (Collider col in hits)
        {
            if (col.transform.root == transform.root) continue;

            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null && playerTarget != null && col.transform.root == playerTarget.root)
            {
                dmg = playerTarget.GetComponentInChildren<IDamageable>();
            }

            if (dmg != null && hitDamageables.Add(dmg))
            {
                Vector3 hitPoint = col.ClosestPoint(checkCenter);
                Vector3 knockDir = (transform.forward + Vector3.up * 0.25f).normalized;

                dmg.TakeDamage(runAttackDamage, hitPoint, knockDir, runAttackKnockback, transform);

                // Sinh hiệu ứng va chạm Impact
                SpawnAttack1ImpactVfx(hitPoint, Quaternion.LookRotation(-transform.forward, Vector3.up));

                // Rung màn hình mạnh
                StartCoroutine(TriggerCameraShake(0.35f, 0.25f));
            }
        }
    }

    private void EnableRunAura()
    {
        if (runAuraVfxPrefab != null && activeRunAuraInstance == null)
        {
            activeRunAuraInstance = Instantiate(runAuraVfxPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
            ParticleSystem[] psList = activeRunAuraInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in psList) ps.Play();
        }
    }

    private void DisableRunAura()
    {
        if (activeRunAuraInstance != null)
        {
            Destroy(activeRunAuraInstance);
            activeRunAuraInstance = null;
        }
    }

    // =========================================================================
    // COROUTINE ATTACK LT (NÚT 6 - 3 ĐÒN LIÊN TIẾP & HÀO QUANG BAO QUANH TAY)
    // =========================================================================

    private IEnumerator AttackLTRoutine()
    {
        isAttackLTActive = true;

        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // Trigger animation AttackLT_IG
        anim.SetTrigger(AttackLTIGHash);

        // Bật hiệu ứng hào quang bao quanh 2 bàn tay trong suốt chuỗi 3 đòn
        EnableHandAuras();

        float timer = 0f;
        bool hit1Done = false;
        bool hit2Done = false;
        bool hit3Done = false;

        while (timer < attackLTTotalDuration)
        {
            timer += Time.deltaTime;

            // Xoay nhẹ theo Player giữa các nhát chém
            if (timer < attackLTHitTime3)
            {
                Transform target = GetPlayerTarget();
                if (target != null)
                {
                    SmoothFaceTarget(target, facePlayerSpeed * 0.6f);
                }
            }

            // Đòn 1 (vung tay nhát 1)
            if (!hit1Done && timer >= attackLTHitTime1)
            {
                hit1Done = true;
                PerformSingleMeleeHit(attackLTDamage, attackLTKnockback, 0.15f);
            }

            // Đòn 2 (vung tay nhát 2)
            if (!hit2Done && timer >= attackLTHitTime2)
            {
                hit2Done = true;
                PerformSingleMeleeHit(attackLTDamage, attackLTKnockback, 0.15f);
            }

            // Đòn 3 (dứt điểm cực mạnh vung 2 tay)
            if (!hit3Done && timer >= attackLTHitTime3)
            {
                hit3Done = true;
                PerformSingleMeleeHit(attackLTFinalDamage, attackLTFinalKnockback, 0.3f);
            }

            yield return null;
        }

        DisableHandAuras();

        // Chờ animation AttackLT chuyển hoàn tất về thế Idle (kèm timeout chống kẹt)
        float ltSafetyTimeout = 1.0f;
        while (anim != null && (anim.GetCurrentAnimatorStateInfo(0).IsName("AttackLT_IG") || anim.GetCurrentAnimatorStateInfo(0).IsName("AttackLT")) && ltSafetyTimeout > 0f)
        {
            ltSafetyTimeout -= Time.deltaTime;
            yield return null;
        }

        isAttackLTActive = false;
        ApplyCombatPacing();
    }

    private void PerformSingleMeleeHit(float damage, float knockback, float cameraShakeMag)
    {
        Vector3 hitCenter = transform.position + transform.forward * attack1HitDistance + Vector3.up * attack1HitHeight;
        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hits = Physics.OverlapSphere(hitCenter, attack1HitRadius + 0.4f, queryMask);
        bool hasHit = false;
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        foreach (Collider col in hits)
        {
            if (col.transform.root == transform.root) continue;

            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null)
            {
                dmg = col.GetComponentInChildren<IDamageable>();
            }
            if (dmg == null && playerTarget != null && col.transform.root == playerTarget.root)
            {
                dmg = playerTarget.GetComponentInChildren<IDamageable>();
            }

            if (dmg != null && hitDamageables.Add(dmg))
            {
                hasHit = true;
                Vector3 hitPoint = col.ClosestPoint(hitCenter);
                Vector3 knockDir = (col.transform.position - transform.position).normalized;
                if (knockDir.sqrMagnitude < 0.001f) knockDir = transform.forward;
                dmg.TakeDamage(damage, hitPoint, knockDir, knockback, transform);

                SpawnAttack1ImpactVfx(hitPoint, Quaternion.identity);
            }
        }

        if (hasHit && cameraShakeMag > 0f)
        {
            StartCoroutine(TriggerCameraShake(0.18f, cameraShakeMag));
        }
    }

    // =========================================================================
    // COROUTINE ICE BALL (NÚT 7 - GỒNG CHIÊU & QUĂNG CẦU NĂNG LƯỢNG PURPLE ORB)
    // =========================================================================

    private IEnumerator IceBallRoutine()
    {
        isIceBallCasting = true;

        int totalCasts = isPhase2 ? 3 : 1;

        for (int castIndex = 0; castIndex < totalCasts; castIndex++)
        {
            if (isDead) break;

        if (facePlayerOnAttack)
        {
            SnapFaceTarget();
        }

        // Trigger animation IceBallIG
        anim.SetTrigger(IceBallIGHash);

        // Tự động kiểm tra liên kết Prefab Purple Orb nếu chưa gán
        if (iceBallPrefab == null)
        {
#if UNITY_EDITOR
            AutoAssignVfx();
#endif
        }

        // Pha 1: Tụ năng lượng tại GIỮA HAI BÀN TAY KHI ĐANG TRÊN TRỜI (kèm 2 luồng năng lượng ở 2 tay)
        GameObject leftHandCharge = null;
        GameObject rightHandCharge = null;

        if (iceBallPrefab != null && activeIceBallChargeInstance == null)
        {
            Vector3 centerPos = GetBetweenHandsPosition();
            activeIceBallChargeInstance = Instantiate(iceBallPrefab, centerPos, Quaternion.identity);
            activeIceBallChargeInstance.transform.localScale = Vector3.one * 0.4f;

            if (leftHandBone != null)
            {
                leftHandCharge = Instantiate(iceBallPrefab, leftHandBone.position, Quaternion.identity, leftHandBone);
                leftHandCharge.transform.localScale = Vector3.one * 0.35f;
            }
            if (rightHandBone != null)
            {
                rightHandCharge = Instantiate(iceBallPrefab, rightHandBone.position, Quaternion.identity, rightHandBone);
                rightHandCharge.transform.localScale = Vector3.one * 0.35f;
            }
        }

        float timer = 0f;
        while (timer < iceBallCastDelay)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / iceBallCastDelay);

            // Cập nhật vị trí quả cầu lớn liên tục bám theo trung điểm giữa 2 bàn tay trên cao
            if (activeIceBallChargeInstance != null)
            {
                activeIceBallChargeInstance.transform.position = GetBetweenHandsPosition();
                float currentScale = Mathf.Lerp(0.4f, 2.2f, progress);
                activeIceBallChargeInstance.transform.localScale = Vector3.one * currentScale;
            }

            // Xoay hướng về Player trong lúc gồng
            Transform target = GetPlayerTarget();
            if (target != null)
            {
                SmoothFaceTarget(target, facePlayerSpeed);
            }

            yield return null;
        }

        // Pha 2: Quăng quả cầu bay từ giữa 2 tay về phía đối thủ
        Vector3 launchOrigin = GetBetweenHandsPosition();
        Vector3 launchDir = transform.forward;

        Transform finalTarget = GetPlayerTarget();
        if (finalTarget != null)
        {
            Vector3 toPlayer = (finalTarget.position + Vector3.up * 1.0f) - launchOrigin;
            if (toPlayer.sqrMagnitude > 0.1f)
            {
                launchDir = toPlayer.normalized;
            }
        }

        // Hủy các quả cầu tích tụ trên tay
        if (leftHandCharge != null) Destroy(leftHandCharge);
        if (rightHandCharge != null) Destroy(rightHandCharge);
        if (activeIceBallChargeInstance != null)
        {
            Destroy(activeIceBallChargeInstance);
            activeIceBallChargeInstance = null;
        }

        // Sinh quả cầu bay độc lập trong Scene
        if (iceBallPrefab != null)
        {
            GameObject flyingOrb = Instantiate(iceBallPrefab, launchOrigin, Quaternion.LookRotation(launchDir, Vector3.up));
            flyingOrb.transform.localScale = Vector3.one * 2.0f;
            StartCoroutine(IceBallFlightRoutine(flyingOrb, launchDir));
        }

        // Chờ animation hoàn tất
        while (timer < iceBallTotalDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Chờ animation IceBall chuyển hoàn tất về thế Idle (kèm timeout chống kẹt)
        float iceBallSafetyTimeout = 1.0f;
        while (anim != null && (anim.GetCurrentAnimatorStateInfo(0).IsName("IceBallIG") || anim.GetCurrentAnimatorStateInfo(0).IsName("IceBall")) && iceBallSafetyTimeout > 0f)
        {
            iceBallSafetyTimeout -= Time.deltaTime;
            yield return null;
        }

            if (castIndex < totalCasts - 1)
            {
                yield return new WaitForSeconds(0.4f);
            }
        }

        isIceBallCasting = false;
        ApplyCombatPacing();
    }

    private IEnumerator IceBallFlightRoutine(GameObject orb, Vector3 direction)
    {
        float aliveTime = 0f;
        bool hasExploded = false;
        Vector3 currentPos = orb.transform.position;
        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));

        while (aliveTime < iceBallLifeTime && !hasExploded && orb != null)
        {
            aliveTime += Time.deltaTime;
            Vector3 nextPos = currentPos + direction * (iceBallSpeed * Time.deltaTime);

            // Kiểm tra va chạm bằng SphereCast
            if (Physics.SphereCast(currentPos, 0.6f, direction, out RaycastHit hit, (nextPos - currentPos).magnitude, queryMask | obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.root != transform.root)
                {
                    ExplodeIceBall(orb, hit.point);
                    hasExploded = true;
                    break;
                }
            }

            currentPos = nextPos;
            if (orb != null) orb.transform.position = currentPos;

            yield return null;
        }

        if (!hasExploded && orb != null)
        {
            ExplodeIceBall(orb, currentPos);
        }
    }

    private void ExplodeIceBall(GameObject orb, Vector3 explodePos)
    {
        // Hiệu ứng nổ
        GameObject vfxToSpawn = iceBallExplodeVfxPrefab != null ? iceBallExplodeVfxPrefab : attack1ImpactVfxPrefab;
        if (vfxToSpawn != null)
        {
            GameObject exp = Instantiate(vfxToSpawn, explodePos, Quaternion.identity);
            exp.transform.localScale = Vector3.one * 2.2f;
            Destroy(exp, 3f);
        }

        // Gây sát thương diện rộng
        LayerMask queryMask = targetLayers.value != 0 ? targetLayers : (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        Collider[] hits = Physics.OverlapSphere(explodePos, iceBallExplosionRadius, queryMask);
        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        foreach (Collider col in hits)
        {
            if (col.transform.root == transform.root) continue;

            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null)
            {
                dmg = col.GetComponentInChildren<IDamageable>();
            }
            if (dmg == null && playerTarget != null && col.transform.root == playerTarget.root)
            {
                dmg = playerTarget.GetComponentInChildren<IDamageable>();
            }

            if (dmg != null && hitDamageables.Add(dmg))
            {
                Vector3 hitPoint = col.ClosestPoint(explodePos);
                Vector3 knockDir = (col.transform.position - explodePos).normalized;
                if (knockDir.sqrMagnitude < 0.001f) knockDir = (transform.forward + Vector3.up * 0.25f).normalized;
                dmg.TakeDamage(iceBallDamage, hitPoint, knockDir, iceBallKnockback, transform);
            }
        }

        StartCoroutine(TriggerCameraShake(0.25f, 0.2f));

        if (orb != null)
        {
            ParticleSystem[] psList = orb.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in psList) p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(orb, 0.5f);
        }
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

    private void SetupHandHitboxes()
    {
        FindBones();
        ValidateHandBones();
        if (leftHandBone != null && leftHandBone.GetComponent<BossHandHitbox>() == null)
        {
            var hb = leftHandBone.gameObject.AddComponent<BossHandHitbox>();
            hb.Initialize(this, 0);
        }
        if (rightHandBone != null && rightHandBone.GetComponent<BossHandHitbox>() == null)
        {
            var hb = rightHandBone.gameObject.AddComponent<BossHandHitbox>();
            hb.Initialize(this, 1);
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

        // Chuẩn hóa hướng phun lửa trên mặt phẳng ngang XZ để triệt tiêu chênh lệch độ cao giữa miệng Boss và Player
        Vector3 dirFlat = new Vector3(direction.x, 0f, direction.z).normalized;
        if (dirFlat.sqrMagnitude < 0.001f) dirFlat = transform.forward;

        foreach (Collider col in hitColliders)
        {
            if (col.transform.root == transform.root) continue;

            Vector3 targetCenter = col.bounds.center;
            Vector3 toTarget = targetCenter - origin;

            // 1. Kiểm tra độ cao tương đối (Player không nhảy vút lên trời quá cao và không rơi tụt xuống vực)
            float deltaY = targetCenter.y - origin.y;
            if (deltaY > 2.0f || deltaY < -5.0f) continue;

            // 2. Tính khoảng cách và góc quét trên mặt phẳng ngang (XZ plane)
            Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            float flatDist = toTargetFlat.magnitude;

            if (flatDist > flameRange) continue;

            // Nếu đứng ngay sát dưới chân/miệng Boss (< 1.2m), mặc định trúng lửa
            if (flatDist >= 1.2f)
            {
                float horizontalAngle = Vector3.Angle(dirFlat, toTargetFlat.normalized);
                if (horizontalAngle > flameConeAngle * 0.5f)
                {
                    continue;
                }
            }

            // 3. Kiểm tra vật cản che chắn (tường / cột đá) - Bỏ qua sàn đấu và chính collider của Boss/Player
            float rayDist = toTarget.magnitude;
            if (rayDist > 0.5f)
            {
                RaycastHit[] hits = Physics.RaycastAll(origin, toTarget.normalized, rayDist - 0.2f, obstacleLayer, QueryTriggerInteraction.Ignore);
                bool isBlocked = false;
                foreach (var h in hits)
                {
                    if (h.collider.transform.root == transform.root || h.collider.transform.root == col.transform.root) continue;
                    // Bỏ qua nếu chạm vào sàn đấu (mặt sàn có normal hướng lên: normal.y > 0.5f)
                    if (h.normal.y > 0.5f) continue;

                    isBlocked = true;
                    break;
                }

                if (isBlocked) continue;
            }

            // 4. Tìm kiếm IDamageable chắc chắn trên mọi phân cấp collider
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                damageable = col.GetComponentInChildren<IDamageable>();
            }
            if (damageable == null && playerTarget != null && (col.transform.root == playerTarget.root || col.CompareTag("Player")))
            {
                damageable = playerTarget.GetComponentInChildren<IDamageable>();
            }

            if (damageable != null && hitDamageables.Add(damageable))
            {
                Vector3 hitPoint = col.ClosestPoint(origin);
                Vector3 knockDir = toTargetFlat.sqrMagnitude > 0.01f ? toTargetFlat.normalized : dirFlat;

                // Gây sát thương theo từng tick kèm hất nhẹ
                damageable.TakeDamage(flameDamagePerTick, hitPoint, knockDir, flameKnockback, transform);

                // Hất nhẹ Player ra xa theo phương ngang (CharacterController)
                CharacterController targetCC = col.GetComponentInParent<CharacterController>();
                if (targetCC != null && targetCC.enabled)
                {
                    targetCC.Move(knockDir * (flameKnockback * 2.0f * Time.deltaTime));
                }

                if (flameHitVfxPrefab != null)
                {
                    Instantiate(flameHitVfxPrefab, hitPoint, Quaternion.identity);
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

        if (leftHandBone == null || rightHandBone == null)
        {
            FindBones();
        }

        Transform modelTransform = anim != null ? anim.transform : null;
        Vector3 originalModelLocalPos = modelTransform != null ? modelTransform.localPosition : Vector3.zero;

        // Bù đắp độ cao model để triệt tiêu việc chân và thân bị lún xuống đất
        if (modelTransform != null && gumoVisualHeightOffset != 0f)
        {
            modelTransform.localPosition = originalModelLocalPos + Vector3.up * gumoVisualHeightOffset;
        }

        // Chờ Animator bắt đầu vào state AttackGumoGumo (tối đa 0.4s)
        float enterWait = 0.4f;
        while (enterWait > 0f && anim != null)
        {
            var cur = anim.GetCurrentAnimatorStateInfo(0);
            var nxt = anim.GetNextAnimatorStateInfo(0);
            if (cur.IsName("AttackGumoGumo") || nxt.IsName("AttackGumoGumo"))
            {
                break;
            }
            enterWait -= Time.deltaTime;
            yield return null;
        }

        // Hệ thống theo dõi chuyển động thực tế của 2 bàn tay theo thời gian thực (đáp ứng 100% mọi tốc độ animation: 0.5x, 0.7x, 1.0x, 1.5x...)
        bool leftArmed = true;
        bool rightArmed = true;
        float prevLeftY = leftHandBone != null ? leftHandBone.position.y : transform.position.y + 2f;
        float prevRightY = rightHandBone != null ? rightHandBone.position.y : transform.position.y + 2f;
        bool leftWasDescending = false;
        bool rightWasDescending = false;

        float attackDurationTimer = 0f;
        float maxSafetyDuration = 12.0f; // Bảo hiểm chống kẹt trạng thái

        while (anim != null && attackDurationTimer < maxSafetyDuration)
        {
            attackDurationTimer += Time.deltaTime;
            var curState = anim.GetCurrentAnimatorStateInfo(0);
            var nextState = anim.GetNextAnimatorStateInfo(0);

            bool inGumo = curState.IsName("AttackGumoGumo") || nextState.IsName("AttackGumoGumo");
            if (!inGumo && attackDurationTimer > 0.35f)
            {
                break; // Animation kết thúc hoặc đã chuyển sang trạng thái kế tiếp
            }

            if (leftHandBone == null || rightHandBone == null)
            {
                FindBones();
            }

            // ==========================================
            // 1. THEO DÕI BÀN TAY TRÁI (LEFT HAND CONTACT)
            // ==========================================
            if (leftHandBone != null)
            {
                float handY = leftHandBone.position.y;
                Vector3 slamOrigin = leftHandBone.position + transform.forward * gumoHandForwardOffset;
                Vector3 groundPos = GetGroundPosition(slamOrigin);
                float relativeY = handY - groundPos.y;

                // Nạp lại khi bàn tay được nhấc lên cao
                if (!leftArmed && relativeY > gumoHandReleaseHeight)
                {
                    leftArmed = true;
                    leftWasDescending = false;
                }

                if (leftArmed)
                {
                    if (handY < prevLeftY - 0.001f)
                    {
                        leftWasDescending = true;
                    }

                    // Chỉ kích hoạt khi tay đã đi xuống sát mặt đất
                    if (relativeY <= gumoHandContactHeight)
                    {
                        // Chạm đất khi:
                        // a) Tay chạm đáy và bắt đầu nảy ngược lên (chuyển động đi lên thực sự)
                        // b) HOẶC tay đã chạm sát sàn đấu (<= gumoHandMinHeight)
                        bool hitBottom = leftWasDescending && (handY > prevLeftY + 0.002f);
                        bool hitFloor = relativeY <= gumoHandMinHeight;

                        if (hitBottom || hitFloor)
                        {
                            TriggerHandSlam(0, groundPos);
                            leftArmed = false;
                            leftWasDescending = false;
                        }
                    }
                }
                prevLeftY = handY;
            }

            // ==========================================
            // 2. THEO DÕI BÀN TAY PHẢI (RIGHT HAND CONTACT)
            // ==========================================
            if (rightHandBone != null)
            {
                float handY = rightHandBone.position.y;
                Vector3 slamOrigin = rightHandBone.position + transform.forward * gumoHandForwardOffset;
                Vector3 groundPos = GetGroundPosition(slamOrigin);
                float relativeY = handY - groundPos.y;

                // Nạp lại khi bàn tay được nhấc lên cao
                if (!rightArmed && relativeY > gumoHandReleaseHeight)
                {
                    rightArmed = true;
                    rightWasDescending = false;
                }

                if (rightArmed)
                {
                    if (handY < prevRightY - 0.001f)
                    {
                        rightWasDescending = true;
                    }

                    if (relativeY <= gumoHandContactHeight)
                    {
                        bool hitBottom = rightWasDescending && (handY > prevRightY + 0.002f);
                        bool hitFloor = relativeY <= gumoHandMinHeight;

                        if (hitBottom || hitFloor)
                        {
                            TriggerHandSlam(1, groundPos);
                            rightArmed = false;
                            rightWasDescending = false;
                        }
                    }
                }
                prevRightY = handY;
            }

            yield return null;
        }

        DisableHandAuras();

        while (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("AttackGumoGumo"))
        {
            yield return null;
        }

        // Trả lại vị trí gốc của model sau khi kết thúc đòn đập đất
        if (modelTransform != null)
        {
            modelTransform.localPosition = originalModelLocalPos;
        }

        isGumoGumoAttacking = false;
        ApplyCombatPacing();
    }

    public void TriggerHandSlam(int handIndex, Vector3? overrideGroundPos = null)
    {
        // 0. Debounce chống trùng lặp nếu cả Trigger Collider và Bone Tracking cùng bắt được điểm chạm
        float now = Time.time;
        if (handIndex == 0)
        {
            if (now - lastLeftSlamTime < 0.15f) return;
            lastLeftSlamTime = now;
        }
        else
        {
            if (now - lastRightSlamTime < 0.15f) return;
            lastRightSlamTime = now;
        }

        Transform handBone = (handIndex == 0) ? leftHandBone : rightHandBone;
        Vector3 slamGroundPos;

        if (overrideGroundPos.HasValue)
        {
            slamGroundPos = overrideGroundPos.Value;
        }
        else
        {
            Vector3 slamOrigin;
            if (handBone != null)
            {
                slamOrigin = handBone.position + transform.forward * gumoHandForwardOffset;
            }
            else
            {
                float sideOffset = (handIndex == 0) ? -0.9f : 0.9f;
                slamOrigin = transform.position + transform.forward * 1.6f + transform.right * sideOffset;
            }
            slamGroundPos = GetGroundPosition(slamOrigin);
        }

        // 1. Rung chấn nhẹ theo từng nhịp đập
        StartCoroutine(TriggerCameraShake(0.12f, 0.12f));

        // 2. Tự động kiểm tra liên kết Attack1 Effect nếu chưa gán
        if (handSlamImpactVfx == null)
        {
#if UNITY_EDITOR
            AutoAssignVfx();
#endif
        }

        // 3. Sinh hiệu ứng đập đất Attack1 Effect tại vị trí bàn tay tiếp đất (sát mặt đất)
        if (handSlamImpactVfx != null)
        {
            // Nhích 0.02m so với mặt sàn để triệt tiêu hiện tượng z-fighting (nhấp nháy vân)
            Vector3 vfxSpawnPos = slamGroundPos + Vector3.up * 0.02f;
            GameObject slamObj = Instantiate(handSlamImpactVfx, vfxSpawnPos, Quaternion.identity);
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

        GameObject waveRoot = null;
        Material lineMat = null;
        LineRenderer outerLine = null;
        LineRenderer[] swirlLines = null;
        int armCount = 4;
        int pointsPerArm = 16;

        if (enableShockwaveRingVisual)
        {
            waveRoot = new GameObject("ClockwiseShockwaveRing");
            waveRoot.transform.position = center + Vector3.up * 0.04f;
            Destroy(waveRoot, duration + 0.2f); // Tự hủy chắc chắn kể cả khi coroutine bị dừng

            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            lineMat = new Material(lineShader);

            GameObject outerObj = new GameObject("OuterRing");
            outerObj.transform.SetParent(waveRoot.transform, false);
            outerLine = outerObj.AddComponent<LineRenderer>();
            SetupLineRenderer(outerLine, lineMat, ringColor, shockwaveLineWidth, 40, true);

            swirlLines = new LineRenderer[armCount];
            for (int k = 0; k < armCount; k++)
            {
                GameObject armObj = new GameObject($"SwirlArm_{k}");
                armObj.transform.SetParent(waveRoot.transform, false);
                swirlLines[k] = armObj.AddComponent<LineRenderer>();
                SetupLineRenderer(swirlLines[k], lineMat, ringColor, shockwaveLineWidth * 0.8f, pointsPerArm, false);
            }
        }

        HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

        float timer = 0f;
        float currentRotationY = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float currentRadius = Mathf.Lerp(0.2f, maxRadius, t);

            if (waveRoot != null && outerLine != null && swirlLines != null)
            {
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

        if (lineMat != null) Destroy(lineMat);
        if (waveRoot != null) Destroy(waveRoot);
    }

    private bool CheckCanDodgeByJump(Collider targetCol, Vector3 waveCenter)
    {
        float footY = targetCol.bounds.min.y;
        float heightAboveWave = footY - waveCenter.y;

        PlayerController pc = targetCol.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            // Nếu là PlayerController, chỉ né đòn khi đang nhảy trên không (!IsGrounded)
            // VÀ độ cao bàn chân nhấc khỏi mặt đất/sóng ít nhất 0.25m để tránh vi rung bề mặt
            return !pc.IsGrounded && heightAboveWave > 0.25f;
        }

        return heightAboveWave > jumpDodgeHeightThreshold;
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

        // Xoay mặt hướng về Player khi phát hiện
        Transform initTarget = jumpDownTarget != null ? jumpDownTarget : (autoTargetPlayerOnJumpDown ? GetPlayerTarget() : null);
        if (initTarget == null) initTarget = GetPlayerTarget();
        if (initTarget != null)
        {
            Vector3 toInit = initTarget.position - transform.position;
            toInit.y = 0f;
            if (toInit.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(toInit.normalized);
            }
        }

        // Điểm tiếp đất là vị trí sàn tại chỗ của Boss trong phòng đấu
        Vector3 targetLandingPos = GetGroundPosition(transform.position);
        // Boss xuất phát từ trên cao lao dốc xuống sàn đấu
        Vector3 startPos = targetLandingPos - transform.forward * jumpDownDistance + Vector3.up * jumpDownHeight;
        transform.position = startPos;

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
        ApplyCombatPacing();
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
        // Bắt đầu quét từ phía trên sàn nhưng dưới tầm ngực/vai boss để tránh va chạm cơ thể boss
        float startY = transform.position.y + 1.8f;
        Vector3 rayStart = new Vector3(searchPos.x, startY, searchPos.z);

        // Quét xuyên qua tất cả colliders để tìm chính xác bề mặt sàn, lọc bỏ toàn bộ Boss và Player
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        float floorY = transform.position.y; // Mặc định là cao độ chân Boss trên sàn

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (playerTarget != null && hit.transform.root == playerTarget.root) continue;

            // Bề mặt sàn phải hướng lên (normal.y > 0.4f) để không bắt nhầm tường phẳng thẳng đứng
            if (hit.normal.y > 0.4f && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                floorY = hit.point.y;
            }
        }

        return new Vector3(searchPos.x, floorY, searchPos.z);
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

    /// <summary>
    /// Lấy vị trí lơ lửng chính giữa hai bàn tay trên trời khi Boss giơ 2 tay gồng tụ lực quả cầu Ice Ball
    /// </summary>
    public Vector3 GetBetweenHandsPosition()
    {
        if (leftHandBone != null && rightHandBone != null)
        {
            return (leftHandBone.position + rightHandBone.position) * 0.5f + Vector3.up * 0.35f;
        }
        else if (rightHandBone != null)
        {
            return rightHandBone.position + Vector3.up * 0.5f;
        }
        else if (leftHandBone != null)
        {
            return leftHandBone.position + Vector3.up * 0.5f;
        }
        return transform.position + Vector3.up * 4.2f + transform.forward * 0.8f;
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
        if (jumpDownImpactVfx == null)
        {
            jumpDownImpactVfx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Impact/Ground Impact.prefab");
        }
        if (jumpAttackImpactVfx == null)
        {
            jumpAttackImpactVfx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/A VFX Prefabs/Impact/Ice Impact.prefab");
        }
        if (iceBallPrefab == null)
        {
            iceBallPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Boss/IceGolem/VFX/Purple Orb.prefab");
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
        Vector3 fDirFlat = new Vector3(fDir.x, 0f, fDir.z).normalized;
        if (fDirFlat.sqrMagnitude < 0.001f) fDirFlat = transform.forward;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.95f);
        Gizmos.DrawRay(fOrigin, fDir * flameRange);
        Gizmos.DrawWireSphere(fOrigin, 0.15f);

        float halfAng = flameConeAngle * 0.5f;
        Quaternion lRot = Quaternion.AngleAxis(-halfAng, Vector3.up);
        Quaternion rRot = Quaternion.AngleAxis(halfAng, Vector3.up);

        Gizmos.color = new Color(1f, 0.45f, 0f, 0.5f);
        Gizmos.DrawRay(fOrigin, (lRot * fDirFlat) * flameRange);
        Gizmos.DrawRay(fOrigin, (rRot * fDirFlat) * flameRange);

        // Vẽ thêm rẻ quạt trên mặt đất để dễ quan sát vùng sát thương thực tế
        Vector3 floorOrigin = new Vector3(fOrigin.x, transform.position.y + 0.1f, fOrigin.z);
        Gizmos.color = new Color(1f, 0.2f, 0f, 0.6f);
        Gizmos.DrawRay(floorOrigin, (lRot * fDirFlat) * flameRange);
        Gizmos.DrawRay(floorOrigin, (rRot * fDirFlat) * flameRange);
        Gizmos.DrawRay(floorOrigin, fDirFlat * flameRange);

        float endRadius = flameRange * Mathf.Tan(halfAng * Mathf.Deg2Rad);
        Gizmos.DrawWireSphere(floorOrigin + fDirFlat * flameRange, endRadius);
    }

    private void EnsureBodyAuraLight()
    {
        Transform auraTrans = transform.Find("Boss_Aura_Light");
        if (auraTrans == null)
        {
            GameObject auraObj = new GameObject("Boss_Aura_Light");
            auraObj.transform.SetParent(transform, false);
            auraObj.transform.localPosition = new Vector3(0f, 2.5f, 0f); // Ngang ngực Boss cao 5m

            Light auraLight = auraObj.AddComponent<Light>();
            auraLight.type = LightType.Point;
            auraLight.color = bodyAuraColor;
            auraLight.range = bodyAuraRange;
            auraLight.intensity = bodyAuraIntensity;
            auraLight.shadows = LightShadows.None;
        }
    }

    // =========================================================================
    // HỆ THỐNG ĐẠO DIỄN CUTSCENE INTRO & BỤC ĐÁ (TASK D02-4)
    // =========================================================================

    /// <summary>
    /// Đặt Boss vào chế độ đứng yên trên bục đá chờ Cutscene mở màn
    /// </summary>
    public void SetStandbyOnPedestal(bool standby)
    {
        isCinematicStandby = standby;
        if (standby)
        {
            hasTriggeredJumpDown = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            if (anim != null) anim.SetFloat(SpeedHash, 0f);
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    /// <summary>
    /// Kích hoạt cú nhảy JumpDown từ trên bục đá xuống sàn đấu trường (Task D02-4)
    /// </summary>
    public void PlayIntroJumpDownFromPedestal(Vector3 targetLandingPos, System.Action onLanded = null)
    {
        if (isJumpingDown) return;
        hasTriggeredJumpDown = true;
        StartCoroutine(PedestalJumpDownRoutine(targetLandingPos, onLanded));
    }

    private IEnumerator PedestalJumpDownRoutine(Vector3 targetLandingPos, System.Action onLanded)
    {
        isJumpingDown = true;

        bool originalKinematic = rb != null && rb.isKinematic;
        bool originalGravity = rb != null && rb.useGravity;
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPos = transform.position;

        // Xoay mặt nhìn về phía điểm tiếp đất
        Vector3 toLanding = targetLandingPos - startPos;
        toLanding.y = 0f;
        if (toLanding.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(toLanding.normalized);
        }

        anim.SetTrigger(JumpDownHash);

        float fallTimer = 0f;
        float duration = Mathf.Max(1.15f, jumpDownFallDuration);
        while (fallTimer < duration)
        {
            fallTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fallTimer / duration);

            Vector3 currentPos = Vector3.Lerp(startPos, targetLandingPos, t);
            float curveT = jumpDownFallCurve != null ? jumpDownFallCurve.Evaluate(t) : (t * t);
            float currentY = Mathf.Lerp(startPos.y, targetLandingPos.y, curveT);

            // Cung nhảy nhẹ nhún lên 1.2m
            currentY += Mathf.Sin(t * Mathf.PI) * 1.2f;

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
        onLanded?.Invoke();
    }

    /// <summary>
    /// Kích hoạt đòn phun băng chào sân trong Cutscene mở màn
    /// </summary>
    public void PlayIntroBreath(System.Action onFinished = null)
    {
        StartCoroutine(IntroBreathRoutine(onFinished));
    }

    private IEnumerator IntroBreathRoutine(System.Action onFinished)
    {
        yield return StartCoroutine(FlameBreathRoutine());
        onFinished?.Invoke();
    }

}
