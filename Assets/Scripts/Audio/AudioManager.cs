using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InfiniteLabyrinth.Interaction;

/// <summary>
/// Hệ Thống Quản Lý Nhạc Nền & Âm Thanh Toàn Cục (Task POL-02):
/// - Singleton DontDestroyOnLoad tồn tại xuyên suốt vòng đời ứng dụng.
/// - Quản lý 3 kênh BGM riêng biệt (Explore, Combat, Boss) hỗ trợ Crossfade mượt mà.
/// - BGM Shuffle ngẫu nhiên không lặp bài cũ + khoảng lặng (Silence Gap 5-10s) chống mệt mỏi thính giác.
/// - Fade out giữa bài mượt mà khi kết thúc giao tranh hoặc hạ gục Boss.
/// - Cung cấp bộ Audio Pools (2D và 3D Spatial) cho Player, Quái Skeleton, Môi Trường và UI.
/// - Micro-pitch ngẫu nhiên cho từng nhát chém, bước chân để chống lặp âm thanh.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum BgmMode
    {
        None,
        Menu,
        Land,
        Dungeon,
        Combat,
        Boss
    }

    private static AudioManager instance;
    private static bool isApplicationQuitting = false;

    public static bool IsApplicationQuitting => isApplicationQuitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isApplicationQuitting = false;
        instance = null;
    }

    public static AudioManager Instance
    {
        get
        {
            if (isApplicationQuitting) return null;
            if (instance == null)
            {
                EnsureInstance();
            }
            return instance;
        }
    }

    public static bool HasInstance
    {
        get
        {
            if (isApplicationQuitting) return false;
            if (instance != null) return true;
            instance = FindFirstObjectByType<AudioManager>();
            return instance != null;
        }
    }

    [Header("=== THIẾT LẬP ÂM LƯỢNG (VOLUME SETTINGS) ===")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.5f;

    [Tooltip("Nếu tích chọn, khi Play game sẽ nạp lại âm lượng đã lưu từ PlayerPrefs. Nếu bỏ tích (mặc định), sẽ dùng trực tiếp chỉ số bạn thiết lập trong Inspector / Prefab.")]
    [SerializeField] private bool loadSavedSettingsOnStart = false;

    // Runtime effective volumes (được điều phối theo tỉ lệ thiết lập trong SettingUI)
    private float currentRuntimeMaster = 1.0f;
    private float currentRuntimeBgm = 0.1f;
    private float currentRuntimeVoice = 0.5f;
    private float currentRuntimeSfx = 0.5f;
    private float currentRuntimeUi = 0.5f;

    private float lastInspectorMasterVol = -1f;
    private float lastInspectorBgmVol = -1f;
    private float lastInspectorVoiceVol = -1f;
    private float lastInspectorSfxVol = -1f;
    private float lastInspectorUiVol = -1f;

    // =========================================================================
    // MA TRẬN CÂN BẰNG ÂM LƯỢNG CHI TIẾT (SOUND BALANCE MATRIX)
    // Tự do kéo chỉnh âm lượng từng âm thanh trong Inspector (kể cả lúc đang Play)
    // =========================================================================

    [Header("=== MA TRẬN CÂN BẰNG - COMBAT NGƯỜI CHƠI ===")]
    [Tooltip("Âm thanh đòn chém kiếm thường (Slash1..2)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerSlash = 1.0f;
    [Tooltip("Tiếng hô/thét chém kiếm ('huh', 'ha')")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerAttackVoice = 0.85f;
    [Tooltip("Tiếng rên đau khi dính đòn vỡ Poise (PlayerHurt1..5)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerHurtVoice = 1.0f;
    [Tooltip("Tiếng gạt đòn kim loại đanh thép (Parry)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerParry = 1.0f;
    [Tooltip("Tiếng bùng nổ xung lực phản đòn (Counter Attack)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerCounter = 1.0f;
    [Tooltip("Tiếng thở dốc khi cạn sạch thể lực (PlayerOutOfStamina)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerOutOfStamina = 0.9f;
    [Tooltip("Tiếng tít trầm cảnh báo khi cố hành động lúc hết stamina (Stamina Alert)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerStaminaAlert = 0.8f;
    [Tooltip("Tiếng ngã gục khi tử trận (Death)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerDeath = 1.0f;
    [Tooltip("Hiệu ứng âm thanh hồi máu / cầu nguyện chuông làng (Healing)")]
    [Range(0f, 2f)] [SerializeField] private float volPlayerHeal = 0.95f;

    [Header("=== MA TRẬN CÂN BẰNG - KỸ NĂNG NGƯỜI CHƠI (SKILLS) ===")]
    [Tooltip("Tụ kiếm khí Skill E (SmallCharge)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillECharge = 1.0f;
    [Tooltip("Gồng nén khí chấn địa Skill Q (MediumCharge)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillQCharge = 1.0f;
    [Tooltip("Tụ lực tuyệt kỹ tối thượng Skill R (BigCharge)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillRCharge = 1.0f;
    [Tooltip("Va chạm nổ kiếm khí Skill E (EImpact)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillEImpact = 1.0f;
    [Tooltip("Chấn địa nổ mặt đất Skill Q (QImpact)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillQImpact = 1.0f;
    [Tooltip("Nhát chém quét trúng kẻ địch Skill R (RImpact)")]
    [Range(0f, 2f)] [SerializeField] private float volSkillRImpact = 1.0f;

    [Header("=== MA TRẬN CÂN BẰNG - BƯỚC CHÂN & IDLE ===")]
    [Tooltip("Khoảng cách giữa 2 bước chân Player khi ĐI BỘ (giây)")]
    [Range(0.2f, 1.0f)] [SerializeField] private float playerWalkFootstepInterval = 0.44f;
    [Tooltip("Khoảng cách giữa 2 bước chân Player khi CHẠY (giây)")]
    [Range(0.15f, 0.8f)] [SerializeField] private float playerRunFootstepInterval = 0.30f;
    [Tooltip("Tỷ lệ âm lượng bước chân khi ĐI BỘ so với chạy (mặc định: 0.55 = 55%)")]
    [Range(0.2f, 1.0f)] [SerializeField] private float volPlayerWalkRatio = 0.55f;
    [Tooltip("Bước chân trên đất cát / cỏ (Làng Haven)")]
    [Range(0f, 2f)] [SerializeField] private float volFootstepsDirt = 0.6f;
    [Tooltip("Bước chân trên sàn đá (Hầm ngục)")]
    [Range(0f, 2f)] [SerializeField] private float volFootstepsRock = 0.6f;
    [Tooltip("Động tác ngó nghiêng / quan sát (Checking1..5)")]
    [Range(0f, 2f)] [SerializeField] private float volIdleChecking = 0.85f;
    [Tooltip("Động tác khởi động / bẻ khớp (Warmup1..6)")]
    [Range(0f, 2f)] [SerializeField] private float volIdleWarmup = 0.9f;
    [Tooltip("Tiếng thở khẽ / ngâm nga (Hums1..2)")]
    [Range(0f, 2f)] [SerializeField] private float volIdleHums = 0.8f;
    [Tooltip("Động tác vươn vai / ngáp (Streching)")]
    [Range(0f, 2f)] [SerializeField] private float volIdleStretching = 0.85f;

    [Header("=== MA TRẬN CÂN BẰNG - QUÁI VẬT SKELETON ===")]
    [Tooltip("Âm thanh bước chân quái vật trên sàn đá hầm ngục")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterFootstep = 0.75f;
    [Tooltip("Khoảng cách giữa 2 bước chân Quái vật khi ĐI BỘ / VỜN QUANH (giây)")]
    [Range(0.3f, 1.5f)] [SerializeField] private float monsterWalkFootstepInterval = 0.65f;
    [Tooltip("Khoảng cách giữa 2 bước chân Quái vật khi CHẠY / TRUY ĐUỔI (giây)")]
    [Range(0.2f, 1.0f)] [SerializeField] private float monsterRunFootstepInterval = 0.42f;
    [Tooltip("Quái cận chiến vung đao chém gió")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterMeleeAttack = 0.85f;
    [Tooltip("Đòn cận chiến chém trúng người chơi (da thịt)")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterMeleeHit = 0.9f;
    [Tooltip("Quái cung thủ bắn cung tên")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterArrowShoot = 0.85f;
    [Tooltip("Mũi tên găm trúng người chơi")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterArrowHit = 0.9f;
    [Tooltip("Pháp sư gồng niệm chú trượng")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterSpellCast = 0.85f;
    [Tooltip("Phép thuật nổ tung mặt đất")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterSpellImpact = 1.0f;
    [Tooltip("Quái dính sát thương (rung rắc xương)")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterGetHit = 0.85f;
    [Tooltip("Quái tử trận (sụp đổ vỡ xương)")]
    [Range(0f, 2f)] [SerializeField] private float volMonsterDeath = 1.0f;

    [Header("=== MA TRẬN CÂN BẰNG - MÔI TRƯỜNG & HẦM NGỤC ===")]
    [Tooltip("Thùng gỗ vỡ vụn")]
    [Range(0f, 2f)] [SerializeField] private float volBarrelBreak = 0.9f;
    [Tooltip("Chuông thánh Làng Haven (khi cầu nguyện)")]
    [Range(0f, 2f)] [SerializeField] private float volVillageBell = 1.0f;
    [Tooltip("Mở cửa hầm ngục")]
    [Range(0f, 2f)] [SerializeField] private float volDoorOpen = 0.85f;
    [Tooltip("Mở Đại Môn phong ấn Boss")]
    [Range(0f, 2f)] [SerializeField] private float volBossDoorOpen = 1.0f;
    [Tooltip("Mở rương báu")]
    [Range(0f, 2f)] [SerializeField] private float volChestOpen = 0.9f;
    [Tooltip("Hào quang rương báu chói lóa (Chest Aura)")]
    [Range(0f, 2f)] [SerializeField] private float volChestAura = 1.0f;
    [Tooltip("Nhặt đồng tiền vàng")]
    [Range(0f, 2f)] [SerializeField] private float volCoinPickup = 0.8f;
    [Tooltip("Cổng dịch chuyển ma thuật")]
    [Range(0f, 2f)] [SerializeField] private float volPortal = 0.9f;
    [Tooltip("Âm thanh ngọn đuốc cháy lách tách (Torch Loop)")]
    [Range(0f, 2f)] [SerializeField] private float volTorch = 0.85f;
    [Tooltip("Âm thanh rùng rợn hầm ngục")]
    [Range(0f, 2f)] [SerializeField] private float volAmbientStinger = 0.75f;

    [Header("=== MA TRẬN CÂN BẰNG - GIAO DIỆN & TƯƠNG TÁC (UI) ===")]
    [Tooltip("Đe rèn nâng cấp / rèn thành công")]
    [Range(0f, 2f)] [SerializeField] private float volUIAnvil = 1.0f;
    [Tooltip("Trang bị / tháo vũ khí, giáp trong kho đồ")]
    [Range(0f, 2f)] [SerializeField] private float volUIEquip = 0.85f;
    [Tooltip("Uống bình máu / dược phẩm")]
    [Range(0f, 2f)] [SerializeField] private float volUIPotion = 0.9f;
    [Tooltip("Giao dịch mua / bán tại Cửa Hàng")]
    [Range(0f, 2f)] [SerializeField] private float volUIShop = 0.85f;
    [Tooltip("Cảnh báo nút click bị khóa / không đủ điều kiện")]
    [Range(0f, 2f)] [SerializeField] private float volUIDisabled = 0.75f;
    [Tooltip("Nhặt vật phẩm dưới đất (phím F)")]
    [Range(0f, 2f)] [SerializeField] private float volUIItemPickup = 0.9f;
    [Tooltip("Mở túi đồ, shop, tab lò rèn, minimap (UIInteract 1)")]
    [Range(0f, 2f)] [SerializeField] private float volUIInteract = 0.85f;
    [Tooltip("Tương tác công tắc Toggle, nút 3D/2D, nhả Slider (UIInteract 2)")]
    [Range(0f, 2f)] [SerializeField] private float volUIInteract2 = 0.85f;
    [Tooltip("Click nút Menu, PauseMenu, chuyển Tab, đóng giao diện (UIInteract 3)")]
    [Range(0f, 2f)] [SerializeField] private float volUIInteract3 = 0.85f;

    [Header("=== DANH SÁCH NHẠC NỀN (BGM TRACKS) ===")]
    [Tooltip("Nhạc nền Làng Haven & MainMenu")]
    [SerializeField] private AudioClip[] landBgmTracks;

    [Tooltip("Nhạc nền Hầm ngục PCG")]
    [SerializeField] private AudioClip[] dungeonBgmTracks;

    [Tooltip("Nhạc chiến đấu khi quái vật phát hiện / truy đuổi người chơi")]
    [SerializeField] private AudioClip[] combatBgmTracks;

    [Tooltip("Nhạc đấu Boss (Ice Golem / Red Ignis)")]
    [SerializeField] private AudioClip[] bossBgmTracks;

    [Header("=== CẤU HÌNH NHỊP BGM (PACING & FADING) ===")]
    [Tooltip("Khoảng lặng tối thiểu giữa hai bài BGM khám phá (giây)")]
    [SerializeField] private float silenceGapMin = 5.0f;
    [Tooltip("Khoảng lặng tối đa giữa hai bài BGM khám phá (giây)")]
    [SerializeField] private float silenceGapMax = 10.0f;
    [Tooltip("Thời gian chờ sau khi con quái cuối cùng mất aggro trước khi fade out nhạc Combat (giây)")]
    [SerializeField] private float combatGracePeriod = 2.0f;
    [Tooltip("Thời lượng Fade in / Fade out BGM mặc định (giây)")]
    [SerializeField] private float defaultFadeDuration = 1.5f;

    [Header("=== ÂM THANH NGƯỜI CHƠI (PLAYER SFX) ===")]
    [Tooltip("2 âm thanh chém kiếm (Slash1..2)")]
    [SerializeField] private AudioClip[] slashClips;
    [Tooltip("4 âm thanh giọng nữ hô lúc chém ('huh', 'ha', 'ya')")]
    [SerializeField] private AudioClip[] attackVoiceClips;
    [Tooltip("5 âm thanh thở hắt / kêu đau khi dính đòn vỡ Poise")]
    [SerializeField] private AudioClip[] hurtVoiceClips;
    [Tooltip("Âm thanh gạt đòn kim loại đanh thép (Parry)")]
    [SerializeField] private AudioClip parryClip;
    [Tooltip("Âm thanh bùng nổ nhát chém phản đòn (Counter Attack)")]
    [SerializeField] private AudioClip counterAttackClip;
    [Tooltip("Âm thanh thở dốc khi cạn sạch thể lực")]
    [SerializeField] private AudioClip outOfStaminaClip;
    [Tooltip("Âm thanh tít trầm khi bấm hành động lúc hết thể lực hoặc click nút bị khóa")]
    [SerializeField] private AudioClip staminaEmptyAlertClip;
    [Tooltip("Âm thanh ngã gục khi tử trận (Death)")]
    [SerializeField] private AudioClip deathClip;
    [Tooltip("Âm thanh hiệu ứng hồi phục máu (Healing) khi uống bình máu hoặc cầu nguyện tại Chuông Làng")]
    [SerializeField] private AudioClip healClip;

    [Header("=== BƯỚC CHÂN NGƯỜI CHƠI (FOOTSTEPS) ===")]
    [Tooltip("Bước chân ĐI BỘ trên đất cát / cỏ (Làng Haven)")]
    [SerializeField] private AudioClip[] footstepsDirtWalk;
    [Tooltip("Bước chân CHẠY trên đất cát / cỏ (Làng Haven)")]
    [SerializeField] private AudioClip[] footstepsDirt;
    [Tooltip("Bước chân ĐI BỘ trên sàn đá hầm ngục (Dungeon)")]
    [SerializeField] private AudioClip[] footstepsRockWalk;
    [Tooltip("Bước chân CHẠY trên sàn đá hầm ngục (Dungeon)")]
    [SerializeField] private AudioClip[] footstepsRock;

    [Header("=== ÂM THANH KỸ NĂNG NGƯỜI CHƠI (PLAYER SKILLS) ===")]
    [Tooltip("Âm thanh gồng tụ lực Skill E (SmallCharge)")]
    [SerializeField] private AudioClip skillEChargeClip;
    [Tooltip("Âm thanh gồng tụ lực Skill Q (MediumCharge)")]
    [SerializeField] private AudioClip skillQChargeClip;
    [Tooltip("Âm thanh gồng tụ lực Skill R (BigCharge)")]
    [SerializeField] private AudioClip skillRChargeClip;

    [Tooltip("Âm thanh va chạm nổ kiếm khí Skill E (EImpact)")]
    [SerializeField] private AudioClip skillEImpactClip;
    [Tooltip("Âm thanh chấn động nổ mặt đất Skill Q (QImpact)")]
    [SerializeField] private AudioClip skillQImpactClip;
    [Tooltip("Âm thanh chém trúng quái vật uy lực của Skill R (RImpact)")]
    [SerializeField] private AudioClip skillRImpactClip;

    [Header("=== ÂM THANH ĐỘNG TÁC IDLE (PLAYER IDLE SFX) ===")]
    [Tooltip("5 âm thanh quan sát, ngó nghiêng (Checking1..5)")]
    [SerializeField] private AudioClip[] idleCheckingClips;
    [Tooltip("6 âm thanh khởi động, bẻ khớp (Warmup1..6)")]
    [SerializeField] private AudioClip[] idleWarmupClips;
    [Tooltip("2 âm thanh thở khẽ, ngâm nga (Hums1..2)")]
    [SerializeField] private AudioClip[] idleHumClips;
    [Tooltip("Âm thanh vươn vai, ngáp (Streching)")]
    [SerializeField] private AudioClip idleStretchingClip;

    [Header("=== ÂM THANH QUÁI VẬT SKELETON (MONSTER SFX) ===")]
    [Tooltip("Quái cận chiến chém đao")]
    [SerializeField] private AudioClip meleeAttackClip;
    [Tooltip("Đòn cận chiến chém trúng người chơi (da thịt)")]
    [SerializeField] private AudioClip meleeHitClip;
    [Tooltip("Quái cung thủ bắn tên")]
    [SerializeField] private AudioClip arrowShootClip;
    [Tooltip("Mũi tên găm trúng người chơi")]
    [SerializeField] private AudioClip arrowHitClip;
    [Tooltip("Pháp sư gồng niệm trượng")]
    [SerializeField] private AudioClip spellCastClip;
    [Tooltip("Phép thuật nổ tung (chạm đất hoặc trúng đích)")]
    [SerializeField] private AudioClip spellImpactClip;
    [Tooltip("Quái Skeleton dính sát thương (rung rắc xương)")]
    [SerializeField] private AudioClip skeletonGetHitClip;
    [Tooltip("Quái Skeleton tử trận (xương sập vỡ vụn)")]
    [SerializeField] private AudioClip skeletonDeathClip;
    [Tooltip("Âm thanh bước chân quái vật trên sàn đá hầm ngục (Footsteps Rock Walk)")]
    [SerializeField] private AudioClip[] monsterFootsteps;

    [Header("=== ÂM THANH MÔI TRƯỜNG & HẦM NGỤC (ENVIRONMENT SFX) ===")]
    [Tooltip("Âm thanh không khí hầm ngục rùng rợn")]
    [SerializeField] private AudioClip[] ambientClips;
    [Tooltip("Thùng gỗ vỡ vụn")]
    [SerializeField] private AudioClip[] barrelBreakClips;
    [Tooltip("Chuông thánh Làng Haven (khi cầu nguyện hồi máu / hồi sinh)")]
    [SerializeField] private AudioClip bellClip;
    [Tooltip("Mở cửa hầm ngục thông thường / phòng khởi đầu")]
    [SerializeField] private AudioClip doorOpenClip;
    [Tooltip("Mở Đại Môn phong ấn Đấu trường Boss")]
    [SerializeField] private AudioClip bossDoorOpenClip;
    [Tooltip("Mở rương báu")]
    [SerializeField] private AudioClip chestOpenClip;
    [Tooltip("Âm thanh vầng hào quang chói lóa khi mở rương (Chest Aura)")]
    [SerializeField] private AudioClip chestAuraClip;
    [Tooltip("Nhặt đồng tiền vàng")]
    [SerializeField] private AudioClip[] coinPickupClips;
    [Tooltip("Cổng dịch chuyển ma thuật Portal")]
    [SerializeField] private AudioClip portalClip;
    [Tooltip("Tiếng đuốc cháy lách tách (Loop)")]
    [SerializeField] private AudioClip torchClip;

    [Header("=== ÂM THANH GIAO DIỆN (UI SFX) ===")]
    [Tooltip("Tiếng đe rèn khi nâng cấp hoặc rèn thành công")]
    [SerializeField] private AudioClip anvilClip;
    [Tooltip("Trang bị vũ khí / giáp trụ trong kho đồ")]
    [SerializeField] private AudioClip equipClip;
    [Tooltip("Uống bình máu / dược phẩm")]
    [SerializeField] private AudioClip potionClip;
    [Tooltip("Giao dịch mua / bán tại Cửa Hàng")]
    [SerializeField] private AudioClip shopClip;
    [Tooltip("Âm thanh nhặt vật phẩm từ dưới đất")]
    [SerializeField] private AudioClip itemPickupClip;
    [Tooltip("Âm thanh mở giao diện hoặc tương tác UI chính (UIInteract 1)")]
    [SerializeField] private AudioClip uiInteractClip;
    [Tooltip("Âm thanh chuyển Toggle, nút 3D/2D, nhả Slider (UIInteract 2)")]
    [SerializeField] private AudioClip uiInteract2Clip;
    [Tooltip("Âm thanh click nút Menu, chuyển Tab, đóng giao diện (UIInteract 3)")]
    [SerializeField] private AudioClip uiInteract3Clip;

    // AudioSources phục vụ BGM
    private AudioSource exploreSource;
    private AudioSource combatSource;
    private AudioSource bossSource;

    // Pools cho SFX
    private readonly List<AudioSource> sfx2DPool = new List<AudioSource>();
    private readonly List<AudioSource> sfx3DPool = new List<AudioSource>();
    private const int POOL_SIZE_2D = 8;
    private const int POOL_SIZE_3D = 12;

    // Trạng thái BGM Runtime
    private BgmMode currentBgmMode = BgmMode.None;
    private BgmMode lastExploreMode = BgmMode.Land;
    private int lastExploreTrackIndex = -1;
    private int lastCombatTrackIndex = -1;
    private int lastBossTrackIndex = -1;

    private Coroutine exploreRoutine;
    private Coroutine combatGraceRoutine;
    private Coroutine returnToExploreRoutine;
    private Coroutine exploreFadeRoutine;
    private Coroutine combatFadeRoutine;
    private Coroutine bossFadeRoutine;
    private Coroutine dungeonAmbientRoutine;

    // Bộ đệm chống spam âm thanh
    private float lastOutOfStaminaTime = -99f;
    private float lastStaminaAlertTime = -99f;
    private float lastPlayerHurtTime = -99f;
    private float lastFootstepTime = -99f;

    // Public Getters & Setters
    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float VoiceVolume => voiceVolume;
    public float SfxVolume => sfxVolume;
    public float UiVolume => uiVolume;
    public BgmMode CurrentBgmMode => currentBgmMode;
    public AudioClip TorchClip => torchClip;
    public AudioClip PortalClip => portalClip;

    // Public Getters cho Ma Trận Cân Bằng Âm Lượng
    public float VolPlayerSlash => volPlayerSlash;
    public float VolPlayerAttackVoice => volPlayerAttackVoice;
    public float VolPlayerHurtVoice => volPlayerHurtVoice;
    public float VolPlayerParry => volPlayerParry;
    public float VolPlayerCounter => volPlayerCounter;
    public float VolPlayerOutOfStamina => volPlayerOutOfStamina;
    public float VolPlayerStaminaAlert => volPlayerStaminaAlert;
    public float VolPlayerDeath => volPlayerDeath;
    public float VolPlayerHeal => volPlayerHeal;

    public float VolSkillECharge => volSkillECharge;
    public float VolSkillQCharge => volSkillQCharge;
    public float VolSkillRCharge => volSkillRCharge;
    public float VolSkillEImpact => volSkillEImpact;
    public float VolSkillQImpact => volSkillQImpact;
    public float VolSkillRImpact => volSkillRImpact;

    public float VolFootstepsDirt => volFootstepsDirt;
    public float VolFootstepsRock => volFootstepsRock;
    public float VolPlayerWalkRatio => volPlayerWalkRatio;
    public float VolIdleChecking => volIdleChecking;
    public float VolIdleWarmup => volIdleWarmup;
    public float VolIdleHums => volIdleHums;
    public float VolIdleStretching => volIdleStretching;

    public float VolMonsterMeleeAttack => volMonsterMeleeAttack;
    public float VolMonsterMeleeHit => volMonsterMeleeHit;
    public float VolMonsterArrowShoot => volMonsterArrowShoot;
    public float VolMonsterArrowHit => volMonsterArrowHit;
    public float VolMonsterSpellCast => volMonsterSpellCast;
    public float VolMonsterSpellImpact => volMonsterSpellImpact;
    public float VolMonsterGetHit => volMonsterGetHit;
    public float VolMonsterDeath => volMonsterDeath;
    public float VolMonsterFootstep => volMonsterFootstep;
    public float PlayerWalkFootstepInterval => playerWalkFootstepInterval;
    public float PlayerRunFootstepInterval => playerRunFootstepInterval;
    public float MonsterWalkFootstepInterval => monsterWalkFootstepInterval;
    public float MonsterRunFootstepInterval => monsterRunFootstepInterval;

    public float VolBarrelBreak => volBarrelBreak;
    public float VolVillageBell => volVillageBell;
    public float VolDoorOpen => volDoorOpen;
    public float VolBossDoorOpen => volBossDoorOpen;
    public float VolChestOpen => volChestOpen;
    public float VolChestAura => volChestAura;
    public float VolCoinPickup => volCoinPickup;
    public float VolPortal => volPortal;
    public float VolTorch => volTorch;
    public float VolAmbientStinger => volAmbientStinger;

    public float VolUIAnvil => volUIAnvil;
    public float VolUIEquip => volUIEquip;
    public float VolUIPotion => volUIPotion;
    public float VolUIShop => volUIShop;
    public float VolUIDisabled => volUIDisabled;
    public float VolUIItemPickup => volUIItemPickup;
    public float VolUIInteract => volUIInteract;
    public float VolUIInteract2 => volUIInteract2;
    public float VolUIInteract3 => volUIInteract3;

    /// <summary>
    /// Khôi phục toàn bộ Ma trận cân bằng âm lượng (38 thanh trượt) về giá trị chuẩn mặc định.
    /// </summary>
    public void ResetSoundBalanceToDefaults()
    {
        volPlayerSlash = 1.0f;
        volPlayerAttackVoice = 0.85f;
        volPlayerHurtVoice = 1.0f;
        volPlayerParry = 1.0f;
        volPlayerCounter = 1.0f;
        volPlayerOutOfStamina = 0.9f;
        volPlayerStaminaAlert = 0.8f;
        volPlayerDeath = 1.0f;
        volPlayerHeal = 0.95f;

        volSkillECharge = 1.0f;
        volSkillQCharge = 1.0f;
        volSkillRCharge = 1.0f;
        volSkillEImpact = 1.0f;
        volSkillQImpact = 1.0f;
        volSkillRImpact = 1.0f;

        volFootstepsDirt = 0.6f;
        volFootstepsRock = 0.6f;
        volPlayerWalkRatio = 0.55f;
        playerWalkFootstepInterval = 0.44f;
        playerRunFootstepInterval = 0.30f;
        volIdleChecking = 0.85f;
        volIdleWarmup = 0.9f;
        volIdleHums = 0.8f;
        volIdleStretching = 0.85f;

        volMonsterMeleeAttack = 0.85f;
        volMonsterMeleeHit = 0.9f;
        volMonsterArrowShoot = 0.85f;
        volMonsterArrowHit = 0.9f;
        volMonsterSpellCast = 0.85f;
        volMonsterSpellImpact = 1.0f;
        volMonsterGetHit = 0.85f;
        volMonsterDeath = 1.0f;
        volMonsterFootstep = 0.75f;
        monsterWalkFootstepInterval = 0.65f;
        monsterRunFootstepInterval = 0.42f;

        volBarrelBreak = 0.9f;
        volVillageBell = 1.0f;
        volDoorOpen = 0.85f;
        volBossDoorOpen = 1.0f;
        volChestOpen = 0.9f;
        volChestAura = 1.0f;
        volCoinPickup = 0.8f;
        volPortal = 0.9f;
        volTorch = 0.85f;
        volAmbientStinger = 0.75f;

        volUIAnvil = 1.0f;
        volUIEquip = 0.85f;
        volUIPotion = 0.9f;
        volUIShop = 0.85f;
        volUIDisabled = 0.75f;
        volUIItemPickup = 0.9f;
        volUIInteract = 0.85f;
        volUIInteract2 = 0.85f;
        volUIInteract3 = 0.85f;
    }

    /// <summary>
    /// Sao chép cấu hình ma trận âm lượng từ một đối tượng AudioManager khác.
    /// </summary>
    public void CopyBalanceSettingsFrom(AudioManager other)
    {
        if (other == null) return;
        masterVolume = other.masterVolume;
        bgmVolume = other.bgmVolume;
        voiceVolume = other.voiceVolume;
        sfxVolume = other.sfxVolume;
        uiVolume = other.uiVolume;

        volPlayerSlash = other.volPlayerSlash;
        volPlayerAttackVoice = other.volPlayerAttackVoice;
        volPlayerHurtVoice = other.volPlayerHurtVoice;
        volPlayerParry = other.volPlayerParry;
        volPlayerCounter = other.volPlayerCounter;
        volPlayerOutOfStamina = other.volPlayerOutOfStamina;
        volPlayerStaminaAlert = other.volPlayerStaminaAlert;
        volPlayerDeath = other.volPlayerDeath;
        volPlayerHeal = other.volPlayerHeal;

        volSkillECharge = other.volSkillECharge;
        volSkillQCharge = other.volSkillQCharge;
        volSkillRCharge = other.volSkillRCharge;
        volSkillEImpact = other.volSkillEImpact;
        volSkillQImpact = other.volSkillQImpact;
        volSkillRImpact = other.volSkillRImpact;

        volFootstepsDirt = other.volFootstepsDirt;
        volFootstepsRock = other.volFootstepsRock;
        volPlayerWalkRatio = other.volPlayerWalkRatio;
        playerWalkFootstepInterval = other.playerWalkFootstepInterval;
        playerRunFootstepInterval = other.playerRunFootstepInterval;
        volIdleChecking = other.volIdleChecking;
        volIdleWarmup = other.volIdleWarmup;
        volIdleHums = other.volIdleHums;
        volIdleStretching = other.volIdleStretching;

        volMonsterMeleeAttack = other.volMonsterMeleeAttack;
        volMonsterMeleeHit = other.volMonsterMeleeHit;
        volMonsterArrowShoot = other.volMonsterArrowShoot;
        volMonsterArrowHit = other.volMonsterArrowHit;
        volMonsterSpellCast = other.volMonsterSpellCast;
        volMonsterSpellImpact = other.volMonsterSpellImpact;
        volMonsterGetHit = other.volMonsterGetHit;
        volMonsterDeath = other.volMonsterDeath;
        volMonsterFootstep = other.volMonsterFootstep;
        monsterWalkFootstepInterval = other.monsterWalkFootstepInterval;
        monsterRunFootstepInterval = other.monsterRunFootstepInterval;

        volBarrelBreak = other.volBarrelBreak;
        volVillageBell = other.volVillageBell;
        volDoorOpen = other.volDoorOpen;
        volBossDoorOpen = other.volBossDoorOpen;
        volChestOpen = other.volChestOpen;
        volChestAura = other.volChestAura;
        volCoinPickup = other.volCoinPickup;
        volPortal = other.volPortal;
        volTorch = other.volTorch;
        volAmbientStinger = other.volAmbientStinger;

        volUIAnvil = other.volUIAnvil;
        volUIEquip = other.volUIEquip;
        volUIPotion = other.volUIPotion;
        volUIShop = other.volUIShop;
        volUIDisabled = other.volUIDisabled;
        volUIItemPickup = other.volUIItemPickup;
        volUIInteract = other.volUIInteract;
        volUIInteract2 = other.volUIInteract2;
        volUIInteract3 = other.volUIInteract3;
    }

    /// <summary>
    /// Đảm bảo luôn tồn tại một AudioManager duy nhất trong game (ưu tiên bên trong [CORE_SYSTEMS]).
    /// </summary>
    public static void EnsureInstance()
    {
        if (isApplicationQuitting) return;
        if (instance != null) return;

        instance = FindFirstObjectByType<AudioManager>();
        if (instance != null)
        {
            if (instance.transform.parent == null)
            {
                DontDestroyOnLoad(instance.gameObject);
            }
            return;
        }

        // Ưu tiên tìm trong CoreSystems nếu có
        var core = InfiniteLabyrinth.Core.CoreSystems.Instance;
        if (core == null) core = FindFirstObjectByType<InfiniteLabyrinth.Core.CoreSystems>();
        if (core != null)
        {
            instance = core.GetComponentInChildren<AudioManager>();
            if (instance != null) return;
        }

        // Tự động tìm nạp Prefab CoreSystems từ Resources nếu scene hoàn toàn chưa có
        GameObject corePrefab = Resources.Load<GameObject>("CoreSystems");
        if (corePrefab != null)
        {
            GameObject coreObj = Instantiate(corePrefab);
            coreObj.name = "[CORE_SYSTEMS]";
            DontDestroyOnLoad(coreObj);
            instance = coreObj.GetComponentInChildren<AudioManager>();
            if (instance != null) return;
        }

        // Dự phòng tối hậu nếu không tìm thấy Prefab CoreSystems
        GameObject go = new GameObject("[AudioManager]");
        instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        LoadVolumeSettings();
        InitializeAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BossArenaTrigger.OnPlayerEnteredArena += OnBossArenaEntered;
        BossVictoryDirector.OnVictorySequenceStarted += OnBossVictorySequence;
        IceGolemController.OnBossDefeated += OnBossDefeated;
        RedIgnisController.OnBossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        BossArenaTrigger.OnPlayerEnteredArena -= OnBossArenaEntered;
        BossVictoryDirector.OnVictorySequenceStarted -= OnBossVictorySequence;
        IceGolemController.OnBossDefeated -= OnBossDefeated;
        RedIgnisController.OnBossDefeated -= OnBossDefeated;
    }

    private void OnBossArenaEntered()
    {
        StartBossMusic();
    }

    private void OnBossVictorySequence()
    {
        StopBossMusic(fadeDuration: 2.5f);
    }

    private void OnBossDefeated()
    {
        StopBossMusic(fadeDuration: 2.5f);
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void Start()
    {
        currentRuntimeMaster = masterVolume;
        currentRuntimeBgm = bgmVolume;
        currentRuntimeVoice = voiceVolume;
        currentRuntimeSfx = sfxVolume;
        currentRuntimeUi = uiVolume;

        if (InfiniteLabyrinth.UI.SettingsManager.HasInstance)
        {
            var d = InfiniteLabyrinth.UI.SettingsManager.Instance.CurrentData;
            ApplySettingsVolumes(d.masterVolume, d.musicVolume, d.voiceVolume, d.sfxVolume, d.uiVolume);
        }

        // Tự động nhận diện scene khởi động ban đầu
        HandleSceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        HandleSceneBgm(scene.name);
    }

    /// <summary>
    /// Nhận diện Scene để chuyển BGM Khám phá tương ứng
    /// </summary>
    private void HandleSceneBgm(string sceneName)
    {
        // 1. Dọn dẹp toàn bộ trạng thái quái aggro và các coroutine giao tranh cũ
        activeAggroEnemies.Clear();
        if (combatGraceRoutine != null) { StopCoroutine(combatGraceRoutine); combatGraceRoutine = null; }
        if (returnToExploreRoutine != null) { StopCoroutine(returnToExploreRoutine); returnToExploreRoutine = null; }

        // 2. Dập tắt ngay lập tức các kênh Combat và Boss nếu còn sót
        if (combatSource != null) { combatSource.Stop(); combatSource.volume = 0f; }
        if (bossSource != null) { bossSource.Stop(); bossSource.volume = 0f; }

        bool isMenu = sceneName.Contains("Menu");
        bool isDungeon = sceneName.Contains("Dungeon");
        BgmMode targetMode = isMenu ? BgmMode.Menu : (isDungeon ? BgmMode.Dungeon : BgmMode.Land);

        // Task 5: Không ngắt quãng hoặc phát lại exploreSource nếu BGM hiện tại đã đúng Scene (ví dụ Land)
        if (currentBgmMode == targetMode && (exploreSource != null && exploreSource.isPlaying || exploreRoutine != null))
        {
            return;
        }

        currentBgmMode = BgmMode.None;

        // Ngắt bài cũ đang phát dở trên exploreSource để nạp bài mới của Scene
        if (exploreSource != null) { exploreSource.Stop(); exploreSource.volume = 0f; }

        if (isMenu)
        {
            PlayMenuBGM();
        }
        else if (isDungeon)
        {
            // Task 6: Tạm hoãn phát Dungeon BGM ở mốc 40% nếu LoadingScreenManager đang chạy tiến trình tải cảnh,
            // để LoadingScreenManager tự kích hoạt tại mốc 90% khi PCG sắp hoàn thành.
            if (InfiniteLabyrinth.UI.LoadingScreenManager.HasInstance && InfiniteLabyrinth.UI.LoadingScreenManager.Instance.IsLoading)
            {
                return;
            }
            PlayDungeonBGM();
        }
        else
        {
            PlayLandBGM();
        }
    }

    // =========================================================================
    // KHỞI TẠO NGUỒN PHÁT (INITIALIZATION & POOLING)
    // =========================================================================

    private void InitializeAudioSources()
    {
        // 1. Tạo 3 AudioSource cho BGM
        exploreSource = CreateAudioSourceChild("BGM_Explore", is2D: true);
        combatSource = CreateAudioSourceChild("BGM_Combat", is2D: true);
        bossSource = CreateAudioSourceChild("BGM_Boss", is2D: true);

        // 2. Tạo Pool AudioSource 2D
        Transform pool2D = new GameObject("Pool_SFX_2D").transform;
        pool2D.SetParent(transform);
        for (int i = 0; i < POOL_SIZE_2D; i++)
        {
            AudioSource src = pool2D.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D Sound
            sfx2DPool.Add(src);
        }

        // 3. Tạo Pool AudioSource 3D
        Transform pool3D = new GameObject("Pool_SFX_3D").transform;
        pool3D.SetParent(transform);
        for (int i = 0; i < POOL_SIZE_3D; i++)
        {
            GameObject srcObj = new GameObject($"SFX_3D_Node_{i}");
            srcObj.transform.SetParent(pool3D);
            AudioSource src = srcObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // 100% 3D Sound
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2.0f;
            src.maxDistance = 25.0f;
            src.dopplerLevel = 0f;
            sfx3DPool.Add(src);
        }
    }

    private AudioSource CreateAudioSourceChild(string name, bool is2D)
    {
        Transform existing = transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(transform);

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = is2D ? 0f : 1f;
        src.volume = 0f;
        return src;
    }

    // =========================================================================
    // QUẢN LÝ ÂM LƯỢNG (VOLUME CONTROLS)
    // =========================================================================

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        currentRuntimeMaster = masterVolume;
        PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
        UpdateAllSourceVolumes();
    }

    public void SetBgmVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        currentRuntimeBgm = bgmVolume;
        PlayerPrefs.SetFloat("Audio_BgmVolume", bgmVolume);
        UpdateAllSourceVolumes();
    }

    public void SetVoiceVolume(float vol)
    {
        voiceVolume = Mathf.Clamp01(vol);
        currentRuntimeVoice = voiceVolume;
        PlayerPrefs.SetFloat("Audio_VoiceVolume", voiceVolume);
    }

    public void SetSfxVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        currentRuntimeSfx = sfxVolume;
        PlayerPrefs.SetFloat("Audio_SfxVolume", sfxVolume);
    }

    public void SetUiVolume(float vol)
    {
        uiVolume = Mathf.Clamp01(vol);
        currentRuntimeUi = uiVolume;
        PlayerPrefs.SetFloat("Audio_UiVolume", uiVolume);
    }

    // =========================================================================
    // ĐIỀU PHỐI ÂM LƯỢNG THEO SETTINGS UI (CALIBRATED SOUND MAPPING)
    // =========================================================================
    private const float DEFAULT_SETTING_MASTER = 1.0f; // Mặc định 10.0 trên Settings UI
    private const float DEFAULT_SETTING_SUB = 0.8f;    // Mặc định 8.0 trên Settings UI cho 4 kênh còn lại

    /// <summary>
    /// Đồng bộ âm lượng từ SettingsManager theo ma trận cân bằng chuẩn của AudioManager.
    /// Setting Master (1.0) gióng vào masterVolume (1.0).
    /// Setting BGM (0.8) gióng vào bgmVolume (0.1).
    /// Setting Voice, SFX, UI (0.8) gióng vào 0.5.
    /// </summary>
    public void ApplySettingsVolumes(float settingMaster, float settingBgm, float settingVoice, float settingSfx, float settingUi)
    {
        currentRuntimeMaster = Mathf.Clamp01(masterVolume * (settingMaster / DEFAULT_SETTING_MASTER));
        currentRuntimeBgm = Mathf.Clamp01(bgmVolume * (settingBgm / DEFAULT_SETTING_SUB));
        currentRuntimeVoice = Mathf.Clamp01(voiceVolume * (settingVoice / DEFAULT_SETTING_SUB));
        currentRuntimeSfx = Mathf.Clamp01(sfxVolume * (settingSfx / DEFAULT_SETTING_SUB));
        currentRuntimeUi = Mathf.Clamp01(uiVolume * (settingUi / DEFAULT_SETTING_SUB));

        UpdateAllSourceVolumes();
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);
        voiceVolume = Mathf.Clamp01(voiceVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        uiVolume = Mathf.Clamp01(uiVolume);

        if (Application.isPlaying)
        {
            UpdateAllSourceVolumes();
        }
    }

    private void Update()
    {
        // Cho phép kéo thanh trượt volume trong Inspector lúc Play Mode ăn ngay lập tức
        if (Mathf.Abs(masterVolume - lastInspectorMasterVol) > 0.005f || 
            Mathf.Abs(bgmVolume - lastInspectorBgmVol) > 0.005f ||
            Mathf.Abs(voiceVolume - lastInspectorVoiceVol) > 0.005f ||
            Mathf.Abs(sfxVolume - lastInspectorSfxVol) > 0.005f ||
            Mathf.Abs(uiVolume - lastInspectorUiVol) > 0.005f)
        {
            lastInspectorMasterVol = masterVolume;
            lastInspectorBgmVol = bgmVolume;
            lastInspectorVoiceVol = voiceVolume;
            lastInspectorSfxVol = sfxVolume;
            lastInspectorUiVol = uiVolume;

            if (InfiniteLabyrinth.UI.SettingsManager.HasInstance)
            {
                var d = InfiniteLabyrinth.UI.SettingsManager.Instance.CurrentData;
                ApplySettingsVolumes(d.masterVolume, d.musicVolume, d.voiceVolume, d.sfxVolume, d.uiVolume);
            }
            else
            {
                currentRuntimeMaster = masterVolume;
                currentRuntimeBgm = bgmVolume;
                currentRuntimeVoice = voiceVolume;
                currentRuntimeSfx = sfxVolume;
                currentRuntimeUi = uiVolume;
                UpdateAllSourceVolumes();
            }
        }
    }

    private void LoadVolumeSettings()
    {
        if (loadSavedSettingsOnStart)
        {
            if (PlayerPrefs.HasKey("Audio_MasterVolume"))
                masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", masterVolume);
            if (PlayerPrefs.HasKey("Audio_BgmVolume"))
                bgmVolume = PlayerPrefs.GetFloat("Audio_BgmVolume", bgmVolume);
            if (PlayerPrefs.HasKey("Audio_VoiceVolume"))
                voiceVolume = PlayerPrefs.GetFloat("Audio_VoiceVolume", voiceVolume);
            if (PlayerPrefs.HasKey("Audio_SfxVolume"))
                sfxVolume = PlayerPrefs.GetFloat("Audio_SfxVolume", sfxVolume);
            if (PlayerPrefs.HasKey("Audio_UiVolume"))
                uiVolume = PlayerPrefs.GetFloat("Audio_UiVolume", uiVolume);
        }

        lastInspectorMasterVol = masterVolume;
        lastInspectorBgmVol = bgmVolume;
        lastInspectorVoiceVol = voiceVolume;
        lastInspectorSfxVol = sfxVolume;
        lastInspectorUiVol = uiVolume;
    }

    private void UpdateAllSourceVolumes()
    {
        float targetBgmVol = currentRuntimeBgm * currentRuntimeMaster;
        if (currentBgmMode == BgmMode.Land || currentBgmMode == BgmMode.Dungeon || currentBgmMode == BgmMode.Menu)
        {
            if (exploreSource != null && exploreSource.isPlaying)
            {
                if (exploreFadeRoutine != null) { StopCoroutine(exploreFadeRoutine); exploreFadeRoutine = null; }
                exploreSource.volume = targetBgmVol;
            }
        }
        else if (currentBgmMode == BgmMode.Combat)
        {
            if (combatSource != null && combatSource.isPlaying)
            {
                if (combatFadeRoutine != null) { StopCoroutine(combatFadeRoutine); combatFadeRoutine = null; }
                combatSource.volume = targetBgmVol;
            }
        }
        else if (currentBgmMode == BgmMode.Boss)
        {
            if (bossSource != null && bossSource.isPlaying)
            {
                if (bossFadeRoutine != null) { StopCoroutine(bossFadeRoutine); bossFadeRoutine = null; }
                bossSource.volume = targetBgmVol;
            }
        }
    }

    // =========================================================================
    // HỆ THỐNG ĐIỀU PHỐI BGM (PLAYLIST SHUFFLE, CROSSFADE & FADEOUT)
    // =========================================================================

    /// <summary>
    /// Phát BGM Menu Chính (Shuffle bài từ landBgmTracks + 5-10s khoảng lặng)
    /// </summary>
    public void PlayMenuBGM()
    {
        lastExploreMode = BgmMode.Menu;
        if (currentBgmMode == BgmMode.Combat || currentBgmMode == BgmMode.Boss) return;
        StartExploreBgm(BgmMode.Menu, landBgmTracks);
    }

    /// <summary>
    /// Phát BGM Làng Haven & MainMenu (Shuffle bài + 5-10s khoảng lặng)
    /// </summary>
    public void PlayLandBGM()
    {
        lastExploreMode = BgmMode.Land;
        if (currentBgmMode == BgmMode.Combat || currentBgmMode == BgmMode.Boss) return;
        StartExploreBgm(BgmMode.Land, landBgmTracks);
    }

    /// <summary>
    /// Phát BGM Hầm Ngục PCG (Shuffle bài + 5-10s khoảng lặng)
    /// </summary>
    public void PlayDungeonBGM()
    {
        lastExploreMode = BgmMode.Dungeon;
        if (currentBgmMode == BgmMode.Combat || currentBgmMode == BgmMode.Boss) return;
        StartExploreBgm(BgmMode.Dungeon, dungeonBgmTracks);
    }

    private void StartExploreBgm(BgmMode mode, AudioClip[] playlist)
    {
        if (currentBgmMode == mode && exploreSource != null && exploreSource.isPlaying) return;

        currentBgmMode = mode;

        if (exploreRoutine != null) StopCoroutine(exploreRoutine);
        exploreRoutine = StartCoroutine(ExplorePlaylistLoopRoutine(playlist));

        if (mode == BgmMode.Dungeon)
        {
            if (dungeonAmbientRoutine != null) StopCoroutine(dungeonAmbientRoutine);
            dungeonAmbientRoutine = StartCoroutine(DungeonAmbientRoutine());
        }
        else
        {
            if (dungeonAmbientRoutine != null)
            {
                StopCoroutine(dungeonAmbientRoutine);
                dungeonAmbientRoutine = null;
            }
        }

        // Fade out các kênh còn lại
        FadeOut(combatSource, 1.0f, ref combatFadeRoutine);
        FadeOut(bossSource, 1.0f, ref bossFadeRoutine);
    }

    /// <summary>
    /// Vòng lặp phát nhạc ngẫu nhiên không lặp bài trước + khoảng lặng 5-10s
    /// </summary>
    private IEnumerator ExplorePlaylistLoopRoutine(AudioClip[] playlist)
    {
        if (playlist == null || playlist.Length == 0) yield break;

        while (currentBgmMode == BgmMode.Land || currentBgmMode == BgmMode.Dungeon || currentBgmMode == BgmMode.Menu)
        {
            // 1. Chọn bài ngẫu nhiên khác với bài vừa phát
            int nextIndex = GetRandomTrackIndex(playlist, lastExploreTrackIndex);
            lastExploreTrackIndex = nextIndex;
            AudioClip clip = playlist[nextIndex];

            if (clip != null)
            {
                exploreSource.clip = clip;
                FadeIn(exploreSource, currentRuntimeBgm * currentRuntimeMaster, defaultFadeDuration, ref exploreFadeRoutine);

                // Task 5: Cuối bài hát (2-3s cuối), thực hiện FadeOut êm ái trước khi bước vào khoảng lặng
                float elapsed = 0f;
                float duration = clip.length;
                float fadeOutDuration = Mathf.Min(2.5f, duration * 0.25f);
                float fadeOutStartTime = Mathf.Max(0f, duration - fadeOutDuration);
                bool endFadeStarted = false;

                while (elapsed < duration)
                {
                    if (currentBgmMode != BgmMode.Land && currentBgmMode != BgmMode.Dungeon && currentBgmMode != BgmMode.Menu)
                    {
                        // Đã chuyển sang Combat hoặc Boss -> dừng vòng lặp khám phá
                        yield break;
                    }

                    if (!endFadeStarted && elapsed >= fadeOutStartTime)
                    {
                        endFadeStarted = true;
                        FadeOut(exploreSource, fadeOutDuration, ref exploreFadeRoutine);
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (exploreSource != null)
                {
                    exploreSource.Stop();
                    exploreSource.volume = 0f;
                }
            }

            // 2. Khoảng lặng ngẫu nhiên 5 - 10s trước khi sang bài tiếp theo
            float silenceGap = Random.Range(silenceGapMin, silenceGapMax);
            float gapTimer = 0f;
            while (gapTimer < silenceGap)
            {
                if (currentBgmMode != BgmMode.Land && currentBgmMode != BgmMode.Dungeon && currentBgmMode != BgmMode.Menu) yield break;
                gapTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Vòng lặp phát âm thanh ambient rùng rợn ngẫu nhiên trong Hầm Ngục mỗi 25-45 giây
    /// </summary>
    private IEnumerator DungeonAmbientRoutine()
    {
        while (currentBgmMode == BgmMode.Dungeon)
        {
            float waitSeconds = Random.Range(25.0f, 45.0f);
            float timer = 0f;
            while (timer < waitSeconds)
            {
                if (currentBgmMode != BgmMode.Dungeon) yield break;
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (currentBgmMode != BgmMode.Dungeon) yield break;

            GameObject player = GameObject.FindWithTag("Player");
            Vector3 center = player != null ? player.transform.position : transform.position;
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(6.0f, 16.0f);
            Vector3 soundPos = center + new Vector3(offset.x, 1.2f, offset.y);

            PlayDungeonAmbientStinger(soundPos);
        }
    }

    // =========================================================================
    // THEO DÕI QUÁI AGGRO & ĐIỀU KHIỂN NHẠC CHIẾN ĐẤU (COMBAT BGM)
    // =========================================================================

    private readonly HashSet<EnemyAIBase> activeAggroEnemies = new HashSet<EnemyAIBase>();
    public int ActiveAggroCount => activeAggroEnemies.Count;

    /// <summary>
    /// Báo cáo trạng thái Aggro từ một con quái vật (EnemyAIBase).
    /// Khi con quái đầu tiên phát hiện người chơi -> Tự động bật Combat BGM.
    /// Khi tất cả quái chết hoặc bỏ aggro -> Đợi 2.0s Grace Period rồi Fade Out êm ái về BGM Khám phá.
    /// </summary>
    public void ReportEnemyAggro(EnemyAIBase enemy, bool isAggro)
    {
        if (enemy == null) return;

        if (isAggro)
        {
            if (!activeAggroEnemies.Contains(enemy))
            {
                activeAggroEnemies.Add(enemy);
                if (activeAggroEnemies.Count == 1)
                {
                    StartCombatMusic();
                }
            }
        }
        else
        {
            if (activeAggroEnemies.Contains(enemy))
            {
                activeAggroEnemies.Remove(enemy);
                if (activeAggroEnemies.Count == 0)
                {
                    StopCombatMusic(instant: false);
                }
            }
        }
    }

    /// <summary>
    /// Kích hoạt nhạc chiến đấu Combat BGM khi có quái agro Player
    /// </summary>
    public void StartCombatMusic()
    {
        if (currentBgmMode == BgmMode.Boss) return; // Boss BGM có độ ưu tiên cao nhất

        // Hủy bộ đếm Grace period và bộ đếm trở về nhạc khám phá nếu có quái agro lại
        if (combatGraceRoutine != null)
        {
            StopCoroutine(combatGraceRoutine);
            combatGraceRoutine = null;
        }
        if (returnToExploreRoutine != null)
        {
            StopCoroutine(returnToExploreRoutine);
            returnToExploreRoutine = null;
        }

        if (currentBgmMode == BgmMode.Combat && combatSource != null && combatSource.isPlaying) return;

        currentBgmMode = BgmMode.Combat;

        // Chọn bài Combat ngẫu nhiên
        if (combatBgmTracks != null && combatBgmTracks.Length > 0)
        {
            int nextIdx = GetRandomTrackIndex(combatBgmTracks, lastCombatTrackIndex);
            lastCombatTrackIndex = nextIdx;
            combatSource.clip = combatBgmTracks[nextIdx];
            combatSource.loop = true;
        }

        // 1. Fade out nhạc Explore nhanh chóng và dứt khoát (0.8s)
        FadeOut(exploreSource, 0.8f, ref exploreFadeRoutine);

        // 2. Fade In nhạc Combat mượt mà từ 0 (1.0s)
        FadeIn(combatSource, bgmVolume * masterVolume, 1.0f, ref combatFadeRoutine);
    }

    /// <summary>
    /// Ngừng nhạc chiến đấu khi hết quái agro (kèm khoảng đệm an toàn Grace Period)
    /// </summary>
    public void StopCombatMusic(bool instant = false)
    {
        if (currentBgmMode != BgmMode.Combat) return;

        if (instant)
        {
            EndCombatAndReturnToExplore();
            return;
        }

        if (combatGraceRoutine != null) StopCoroutine(combatGraceRoutine);
        combatGraceRoutine = StartCoroutine(CombatGraceRoutine());
    }

    private IEnumerator CombatGraceRoutine()
    {
        // Chờ 2 giây đề phòng quái tiếp theo bất ngờ tiếp cận
        float timer = 0f;
        while (timer < combatGracePeriod)
        {
            if (activeAggroEnemies.Count > 0)
            {
                combatGraceRoutine = null;
                yield break;
            }
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        EndCombatAndReturnToExplore();
        combatGraceRoutine = null;
    }

    private void EndCombatAndReturnToExplore()
    {
        if (currentBgmMode != BgmMode.Combat) return;

        // Đổi trạng thái ngay để tránh bị các hàm kiểm tra mode chặn
        currentBgmMode = BgmMode.None;

        if (returnToExploreRoutine != null) StopCoroutine(returnToExploreRoutine);
        returnToExploreRoutine = StartCoroutine(ReturnToExploreRoutine());
    }

    private IEnumerator ReturnToExploreRoutine()
    {
        // 1. Fade out nhạc Combat giữa bài (1.5s)
        FadeOut(combatSource, 1.5f, ref combatFadeRoutine);

        float fadeTimer = 0f;
        while (fadeTimer < 1.5f)
        {
            if (activeAggroEnemies.Count > 0)
            {
                returnToExploreRoutine = null;
                yield break;
            }
            fadeTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 2. Khoảng lặng nghỉ ngơi sau chiến đấu (Silence Gap ngẫu nhiên 3 - 5 giây)
        float breather = Random.Range(3.0f, 5.0f);
        float gapTimer = 0f;
        while (gapTimer < breather)
        {
            if (activeAggroEnemies.Count > 0)
            {
                returnToExploreRoutine = null;
                yield break;
            }
            gapTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        returnToExploreRoutine = null;

        // 3. Khôi phục lại nhạc thám hiểm của Scene hiện tại
        if (lastExploreMode == BgmMode.Dungeon)
        {
            PlayDungeonBGM();
        }
        else if (lastExploreMode == BgmMode.Menu)
        {
            PlayMenuBGM();
        }
        else
        {
            PlayLandBGM();
        }
    }

    /// <summary>
    /// Kích hoạt nhạc Boss khi mở cửa / bắt đầu trận đấu Boss
    /// </summary>
    public void StartBossMusic()
    {
        currentBgmMode = BgmMode.Boss;

        if (exploreRoutine != null) StopCoroutine(exploreRoutine);
        if (combatGraceRoutine != null) StopCoroutine(combatGraceRoutine);

        FadeOut(exploreSource, 0.8f, ref exploreFadeRoutine);
        FadeOut(combatSource, 0.8f, ref combatFadeRoutine);

        if (bossBgmTracks != null && bossBgmTracks.Length > 0)
        {
            int nextIdx = GetRandomTrackIndex(bossBgmTracks, lastBossTrackIndex);
            lastBossTrackIndex = nextIdx;
            bossSource.clip = bossBgmTracks[nextIdx];
            bossSource.loop = true;
            bossSource.Play();
        }

        FadeIn(bossSource, bgmVolume * masterVolume, 1.2f, ref bossFadeRoutine);
    }

    /// <summary>
    /// Fade out nhạc Boss khi Boss bị tiêu diệt
    /// </summary>
    public void StopBossMusic(float fadeDuration = 2.5f)
    {
        if (currentBgmMode != BgmMode.Boss) return;

        FadeOut(bossSource, fadeDuration, ref bossFadeRoutine);

        // Chờ fade xong trả về nhạc hầm ngục
        StartCoroutine(ReturnToDungeonAfterBossDefeat(fadeDuration));
    }

    private IEnumerator ReturnToDungeonAfterBossDefeat(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (currentBgmMode == BgmMode.Boss)
        {
            PlayDungeonBGM();
        }
    }

    private int GetRandomTrackIndex(AudioClip[] tracks, int lastIndex)
    {
        if (tracks == null || tracks.Length == 0) return -1;
        if (tracks.Length == 1) return 0;

        int newIdx = Random.Range(0, tracks.Length);
        if (newIdx == lastIndex)
        {
            newIdx = (newIdx + 1) % tracks.Length;
        }
        return newIdx;
    }

    // =========================================================================
    // HỆ THỐNG FADE IN / FADE OUT VOLUME CHUYÊN BIỆT
    // =========================================================================

    private void FadeIn(AudioSource src, float targetVol, float duration, ref Coroutine runningRoutine)
    {
        if (src == null) return;
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        src.volume = 0f;
        if (!src.isPlaying) src.Play();
        runningRoutine = StartCoroutine(FadeVolumeRoutine(src, targetVol, duration, stopWhenZero: false));
    }

    private void FadeOut(AudioSource src, float duration, ref Coroutine runningRoutine, System.Action onComplete = null)
    {
        if (src == null || !src.isPlaying)
        {
            if (src != null) { src.volume = 0f; src.Stop(); }
            runningRoutine = null;
            onComplete?.Invoke();
            return;
        }
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        runningRoutine = StartCoroutine(FadeVolumeRoutine(src, 0f, duration, stopWhenZero: true, onComplete));
    }

    private IEnumerator FadeVolumeRoutine(AudioSource src, float targetVol, float duration, bool stopWhenZero, System.Action onComplete = null)
    {
        if (src == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startVol = src.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (src != null)
            {
                src.volume = Mathf.Lerp(startVol, targetVol, t);
            }
            yield return null;
        }

        if (src != null)
        {
            src.volume = targetVol;
            if (stopWhenZero && targetVol <= 0.001f)
            {
                src.Stop();
            }
        }

        // Tự động giải phóng các biến fade routine tương ứng
        if (src == exploreSource) exploreFadeRoutine = null;
        else if (src == combatSource) combatFadeRoutine = null;
        else if (src == bossSource) bossFadeRoutine = null;

        onComplete?.Invoke();
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO PLAYER (COMBAT & REACTION)
    // =========================================================================

    /// <summary>
    /// Phát ngẫu nhiên 1 trong 4 tiếng chém kiếm với micro-pitch biến hóa
    /// </summary>
    public void PlayPlayerSlash()
    {
        AudioClip clip = GetRandomClip(slashClips);
        if (clip != null)
        {
            Play2D(clip, volume: volPlayerSlash, pitchMin: 0.94f, pitchMax: 1.06f);
        }
    }

    /// <summary>
    /// Phát tiếng giọng nữ ("huh", "ha") khi vung kiếm theo tỷ lệ (mặc định 40%)
    /// </summary>
    public void PlayPlayerAttackVoice(float chance = 0.40f)
    {
        if (Random.value > chance) return;

        AudioClip clip = GetRandomClip(attackVoiceClips);
        if (clip != null)
        {
            Play2D(clip, volume: volPlayerAttackVoice, pitchMin: 0.96f, pitchMax: 1.04f, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Xử lý âm thanh đòn Charge Attack: Nhát 1 có tỉ lệ kêu, nhát 2 chém không kêu giọng
    /// </summary>
    public void PlayPlayerChargeAttack(int hitIndex)
    {
        PlayPlayerSlash();

        if (hitIndex == 1)
        {
            PlayPlayerAttackVoice(0.50f);
        }
        // hitIndex == 2 -> Không phát tiếng giọng
    }

    /// <summary>
    /// Phát ngẫu nhiên 1 trong 5 tiếng kêu khi người chơi bị vỡ Poise (Stagger)
    /// </summary>
    public void PlayPlayerPoiseBreak()
    {
        if (Time.time < lastPlayerHurtTime + 0.35f) return;
        lastPlayerHurtTime = Time.time;

        AudioClip clip = GetRandomClip(hurtVoiceClips);
        if (clip != null)
        {
            Play2D(clip, volume: volPlayerHurtVoice, pitchMin: 0.95f, pitchMax: 1.05f, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh thở dốc khi cạn sạch thể lực Stamina
    /// </summary>
    public void PlayPlayerOutOfStamina()
    {
        if (Time.time < lastOutOfStaminaTime + 2.0f) return;
        lastOutOfStaminaTime = Time.time;

        if (outOfStaminaClip != null)
        {
            Play2D(outOfStaminaClip, volume: volPlayerOutOfStamina, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh tít trầm khi cố bấm hành động trong lúc hết thể lực
    /// </summary>
    public void PlayPlayerStaminaExhaustedAction()
    {
        if (Time.time < lastStaminaAlertTime + 0.25f) return;
        lastStaminaAlertTime = Time.time;

        if (staminaEmptyAlertClip != null)
        {
            Play2D(staminaEmptyAlertClip, volume: volPlayerStaminaAlert, pitchMin: 0.92f, pitchMax: 0.96f, category: AudioCategory.UI);
        }
    }

    /// <summary>
    /// Phát âm thanh tử trận khi máu về 0
    /// </summary>
    public void PlayPlayerDeath()
    {
        if (deathClip != null)
        {
            Play2D(deathClip, volume: volPlayerDeath, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh gạt đòn kim loại đanh thép (Parry)
    /// </summary>
    public void PlayPlayerParry()
    {
        if (parryClip != null)
        {
            Play2D(parryClip, volume: volPlayerParry, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh chém phản đòn bùng nổ xung lực (Counter Attack)
    /// </summary>
    public void PlayPlayerCounterAttack()
    {
        if (counterAttackClip != null)
        {
            Play2D(counterAttackClip, volume: volPlayerCounter);
        }
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng hồi phục máu (Healing) khi uống bình máu hoặc cầu nguyện tại Chuông Làng
    /// </summary>
    public void PlayHealing(Vector3? position = null, float volume = 0.95f)
    {
        if (healClip == null) return;
        float finalVol = volPlayerHeal * (volume / 0.95f);
        if (position.HasValue)
        {
            Play3D(healClip, position.Value, volume: finalVol, pitchMin: 0.98f, pitchMax: 1.02f, minDistance: 2f, maxDistance: 20f);
        }
        else
        {
            Play2D(healClip, volume: finalVol, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh bước chân của người chơi dựa trên môi trường và trạng thái Di chuyển (Đi bộ / Chạy)
    /// </summary>
    public void PlayPlayerFootstep(bool isDungeon, bool isRunning, float volumeMultiplier = 1.0f)
    {
        if (Time.time < lastFootstepTime + 0.12f) return;
        lastFootstepTime = Time.time;

        AudioClip[] array;
        if (isDungeon)
        {
            array = isRunning ? footstepsRock : (footstepsRockWalk != null && footstepsRockWalk.Length > 0 ? footstepsRockWalk : footstepsRock);
        }
        else
        {
            array = isRunning ? footstepsDirt : (footstepsDirtWalk != null && footstepsDirtWalk.Length > 0 ? footstepsDirtWalk : footstepsDirt);
        }

        float baseVol = isDungeon ? volFootstepsRock : volFootstepsDirt;
        float walkScale = isRunning ? 1.0f : volPlayerWalkRatio;
        float finalVol = baseVol * walkScale * volumeMultiplier;

        // Đi bộ: Âm trầm, êm dịu hơn (pitch 0.88 - 0.94). Chạy: Âm đanh giòn, nảy lực (pitch 0.98 - 1.06)
        float pMin = isRunning ? 0.98f : 0.88f;
        float pMax = isRunning ? 1.06f : 0.94f;

        AudioClip clip = GetRandomClip(array);
        if (clip != null)
        {
            Play2D(clip, volume: finalVol, pitchMin: pMin, pitchMax: pMax);
        }
    }

    /// <summary>
    /// Quá tải tương thích ngược cho PlayPlayerFootstep (ví dụ khi tiếp đất)
    /// </summary>
    public void PlayPlayerFootstep(bool isDungeon, float volume = 0.6f)
    {
        PlayPlayerFootstep(isDungeon, isRunning: true, volumeMultiplier: volume / 0.6f);
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX KỸ NĂNG NGƯỜI CHƠI (SKILLS - CHARGE & IMPACT)
    // =========================================================================

    /// <summary>
    /// Phát âm thanh gồng lực Skill E (SmallCharge)
    /// </summary>
    public void PlaySkillECharge()
    {
        if (skillEChargeClip != null)
        {
            Play2D(skillEChargeClip, volume: volSkillECharge, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh gồng lực Skill Q (MediumCharge)
    /// </summary>
    public void PlaySkillQCharge()
    {
        if (skillQChargeClip != null)
        {
            Play2D(skillQChargeClip, volume: volSkillQCharge, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh gồng lực Skill R (BigCharge)
    /// </summary>
    public void PlaySkillRCharge()
    {
        if (skillRChargeClip != null)
        {
            Play2D(skillRChargeClip, volume: volSkillRCharge, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh va chạm nổ kiếm khí Skill E (EImpact) tại vị trí va chạm (3D Spatial)
    /// </summary>
    public void PlaySkillEImpact(Vector3 position)
    {
        if (skillEImpactClip != null)
        {
            Play3D(skillEImpactClip, position, volume: volSkillEImpact, pitchMin: 0.95f, pitchMax: 1.05f, minDistance: 3f, maxDistance: 30f);
        }
    }

    /// <summary>
    /// Phát âm thanh chấn địa nổ mặt đất Skill Q (QImpact)
    /// </summary>
    public void PlaySkillQImpact(Vector3? position = null)
    {
        if (skillQImpactClip == null) return;
        if (position.HasValue)
        {
            Play3D(skillQImpactClip, position.Value, volume: volSkillQImpact, pitchMin: 0.96f, pitchMax: 1.04f, minDistance: 4f, maxDistance: 35f);
        }
        else
        {
            Play2D(skillQImpactClip, volume: volSkillQImpact, pitchMin: 0.96f, pitchMax: 1.04f);
        }
    }

    /// <summary>
    /// Phát âm thanh chém trúng quái vật uy lực của Skill R (RImpact)
    /// </summary>
    public void PlaySkillRImpact(Vector3? position = null)
    {
        if (skillRImpactClip == null) return;
        if (position.HasValue)
        {
            Play3D(skillRImpactClip, position.Value, volume: volSkillRImpact, pitchMin: 0.97f, pitchMax: 1.03f, minDistance: 4f, maxDistance: 35f);
        }
        else
        {
            Play2D(skillRImpactClip, volume: volSkillRImpact, pitchMin: 0.97f, pitchMax: 1.03f);
        }
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX ĐỘNG TÁC IDLE (PLAYER IDLE SFX)
    // =========================================================================

    /// <summary>
    /// Phát âm thanh ngó nghiêng / quan sát (Checking).
    /// index = 0 (ngẫu nhiên), hoặc 1..5 tương ứng Checking1..5
    /// </summary>
    public void PlayIdleChecking(int index = 0)
    {
        AudioClip clip = GetIndexedOrRandomClip(idleCheckingClips, index);
        if (clip != null)
        {
            Play2D(clip, volume: volIdleChecking, pitchMin: 0.96f, pitchMax: 1.04f, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh khởi động / bẻ khớp (Warmup).
    /// index = 0 (ngẫu nhiên), hoặc 1..6 tương ứng Warmup1..6
    /// </summary>
    public void PlayIdleWarmup(int index = 0)
    {
        AudioClip clip = GetIndexedOrRandomClip(idleWarmupClips, index);
        if (clip != null)
        {
            Play2D(clip, volume: volIdleWarmup, pitchMin: 0.96f, pitchMax: 1.04f, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh thở khẽ / ngâm nga (Hums).
    /// index = 0 (ngẫu nhiên), hoặc 1..2 tương ứng Hums1..2
    /// </summary>
    public void PlayIdleHums(int index = 0)
    {
        AudioClip clip = GetIndexedOrRandomClip(idleHumClips, index);
        if (clip != null)
        {
            Play2D(clip, volume: volIdleHums, pitchMin: 0.97f, pitchMax: 1.03f, category: AudioCategory.Voice);
        }
    }

    /// <summary>
    /// Phát âm thanh vươn vai / ngáp (Streching)
    /// </summary>
    public void PlayIdleStretching()
    {
        if (idleStretchingClip != null)
        {
            Play2D(idleStretchingClip, volume: volIdleStretching, pitchMin: 0.98f, pitchMax: 1.02f, category: AudioCategory.Voice);
        }
    }

    private AudioClip GetIndexedOrRandomClip(AudioClip[] array, int index)
    {
        if (array == null || array.Length == 0) return null;
        if (index >= 1 && index <= array.Length)
        {
            return array[index - 1];
        }
        return GetRandomClip(array);
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO QUÁI SKELETON (3D SPATIAL)
    // =========================================================================

    public void PlaySkeletonMeleeAttack(Vector3 pos) => Play3D(meleeAttackClip, pos, volume: volMonsterMeleeAttack, pitchMin: 0.95f, pitchMax: 1.05f);
    public void PlaySkeletonMeleeHit(Vector3 pos) => Play3D(meleeHitClip, pos, volume: volMonsterMeleeHit);
    public void PlaySkeletonArrowShoot(Vector3 pos) => Play3D(arrowShootClip, pos, volume: volMonsterArrowShoot, pitchMin: 0.95f, pitchMax: 1.05f);
    public void PlaySkeletonArrowHit(Vector3 pos) => Play3D(arrowHitClip, pos, volume: volMonsterArrowHit);
    public void PlaySkeletonSpellCast(Vector3 pos) => Play3D(spellCastClip, pos, volume: volMonsterSpellCast);
    public void PlaySkeletonSpellImpact(Vector3 pos) => Play3D(spellImpactClip, pos, volume: volMonsterSpellImpact, pitchMin: 0.93f, pitchMax: 1.07f);
    public void PlaySkeletonGetHit(Vector3 pos) => Play3D(skeletonGetHitClip, pos, volume: volMonsterGetHit, pitchMin: 0.93f, pitchMax: 1.07f);
    public void PlaySkeletonDeath(Vector3 pos) => Play3D(skeletonDeathClip, pos, volume: volMonsterDeath, pitchMin: 0.95f, pitchMax: 1.05f);

    /// <summary>
    /// Phát âm thanh bước chân của quái vật trên sàn đá hầm ngục (3D Spatial Audio)
    /// </summary>
    public void PlayMonsterFootstep(Vector3 pos, float volumeMultiplier = 1.0f)
    {
        if (monsterFootsteps == null || monsterFootsteps.Length == 0) return;
        AudioClip clip = GetRandomClip(monsterFootsteps);
        if (clip != null)
        {
            Play3D(clip, pos, volume: volMonsterFootstep * volumeMultiplier, pitchMin: 0.90f, pitchMax: 1.10f, minDistance: 2f, maxDistance: 22f);
        }
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO MÔI TRƯỜNG & HẦM NGỤC (ENVIRONMENT SFX)
    // =========================================================================

    public void PlayBarrelBreak(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(barrelBreakClips);
        if (clip != null) Play3D(clip, pos, volume: volBarrelBreak, pitchMin: 0.93f, pitchMax: 1.07f);
    }

    public void PlayVillageBell(Vector3 pos) => Play3D(bellClip, pos, volume: volVillageBell);
    public void PlayDoorOpen(Vector3 pos) => Play3D(doorOpenClip, pos, volume: volDoorOpen);
    public void PlayBossDoorOpen(Vector3 pos) => Play3D(bossDoorOpenClip, pos, volume: volBossDoorOpen);
    public void PlayChestOpen(Vector3 pos) => Play3D(chestOpenClip, pos, volume: volChestOpen);

    /// <summary>
    /// Phát âm thanh vầng hào quang chói lóa khi mở rương (Chest Aura) với độ lớn theo cấp bậc rương.
    /// volumeMultiplier: Boss = 1.0f, Rare = 0.70f, Normal = 0.35f
    /// </summary>
    public void PlayChestAura(Vector3 pos, float volumeMultiplier = 1.0f)
    {
        if (chestAuraClip == null) return;
        Play3D(chestAuraClip, pos, volume: Mathf.Clamp01(volChestAura * volumeMultiplier), pitchMin: 0.98f, pitchMax: 1.02f, minDistance: 3f, maxDistance: 30f);
    }

    public void PlayCoinPickup(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(coinPickupClips);
        if (clip != null) Play3D(clip, pos, volume: volCoinPickup, pitchMin: 0.95f, pitchMax: 1.08f);
    }

    public void PlayPortalSound(Vector3 pos) => Play3D(portalClip, pos, volume: volPortal);

    public void PlayDungeonAmbientStinger(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(ambientClips);
        if (clip != null) Play3D(clip, pos, volume: volAmbientStinger);
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO GIAO DIỆN (UI SFX)
    // =========================================================================

    public void PlayUIUpgradeAnvil() => Play2D(anvilClip, volume: volUIAnvil, category: AudioCategory.UI);
    public void PlayUIEquip() => Play2D(equipClip, volume: volUIEquip, category: AudioCategory.UI);
    public void PlayUIPotionUse() => Play2D(potionClip, volume: volUIPotion, category: AudioCategory.UI);
    public void PlayUIShopTransaction() => Play2D(shopClip, volume: volUIShop, category: AudioCategory.UI);
    public void PlayUIDisabledClick() => Play2D(staminaEmptyAlertClip, volume: volUIDisabled, pitchMin: 0.92f, pitchMax: 0.96f, category: AudioCategory.UI);
    public void PlayUIClick() => PlayUIInteract();
    public void PlayUIItemPickup() => Play2D(itemPickupClip, volume: volUIItemPickup, category: AudioCategory.UI);
    public void PlayUIInteract() => Play2D(uiInteractClip, volume: volUIInteract, category: AudioCategory.UI);
    public void PlayUIInteract1() => PlayUIInteract();
    public void PlayUIInteract2() => Play2D(uiInteract2Clip != null ? uiInteract2Clip : uiInteractClip, volume: volUIInteract2, category: AudioCategory.UI);
    public void PlayUIInteract3() => Play2D(uiInteract3Clip != null ? uiInteract3Clip : uiInteractClip, volume: volUIInteract3, category: AudioCategory.UI);

    // =========================================================================
    // TIỆN ÍCH CORE PHÁT 2D VÀ 3D (GENERIC AUDIO CORE)
    // =========================================================================

    public enum AudioCategory
    {
        SFX,
        Voice,
        UI
    }

    public void Play2D(AudioClip clip, float volume = 1.0f, float pitchMin = 1.0f, float pitchMax = 1.0f, AudioCategory category = AudioCategory.SFX)
    {
        if (clip == null) return;

        AudioSource src = GetAvailable2DSource();
        if (src == null) return;

        float catVol = category == AudioCategory.Voice ? currentRuntimeVoice : (category == AudioCategory.UI ? currentRuntimeUi : currentRuntimeSfx);
        src.clip = clip;
        src.volume = volume * catVol * currentRuntimeMaster;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.Play();
    }

    public void Play3D(AudioClip clip, Vector3 position, float volume = 1.0f, float pitchMin = 0.95f, float pitchMax = 1.05f, float minDistance = 2.0f, float maxDistance = 25.0f, AudioCategory category = AudioCategory.SFX)
    {
        if (clip == null) return;

        AudioSource src = GetAvailable3DSource();
        if (src == null) return;

        float catVol = category == AudioCategory.Voice ? currentRuntimeVoice : (category == AudioCategory.UI ? currentRuntimeUi : currentRuntimeSfx);
        src.transform.position = position;
        src.clip = clip;
        src.volume = volume * catVol * currentRuntimeMaster;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.Play();
    }

    private AudioSource GetAvailable2DSource()
    {
        for (int i = 0; i < sfx2DPool.Count; i++)
        {
            if (sfx2DPool[i] != null && !sfx2DPool[i].isPlaying)
            {
                return sfx2DPool[i];
            }
        }
        // Nếu tất cả đều bận, mượn nguồn đầu tiên
        return sfx2DPool.Count > 0 ? sfx2DPool[0] : null;
    }

    private AudioSource GetAvailable3DSource()
    {
        for (int i = 0; i < sfx3DPool.Count; i++)
        {
            if (sfx3DPool[i] != null && !sfx3DPool[i].isPlaying)
            {
                return sfx3DPool[i];
            }
        }
        // Nếu tất cả đều bận, mượn nguồn đầu tiên
        return sfx3DPool.Count > 0 ? sfx3DPool[0] : null;
    }

    private AudioClip GetRandomClip(AudioClip[] array)
    {
        if (array == null || array.Length == 0) return null;
        if (array.Length == 1) return array[0];
        return array[Random.Range(0, array.Length)];
    }
}
