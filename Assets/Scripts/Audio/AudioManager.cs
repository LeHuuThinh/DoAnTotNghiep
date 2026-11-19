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
    [SerializeField] private float sfxVolume = 0.5f;

    [Tooltip("Nếu tích chọn, khi Play game sẽ nạp lại âm lượng đã lưu từ PlayerPrefs. Nếu bỏ tích (mặc định), sẽ dùng trực tiếp chỉ số bạn thiết lập trong Inspector / Prefab.")]
    [SerializeField] private bool loadSavedSettingsOnStart = false;

    private float lastInspectorMasterVol = -1f;
    private float lastInspectorBgmVol = -1f;
    private float lastInspectorSfxVol = -1f;

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
    [Tooltip("Bước chân trên đất cát / cỏ (Làng Haven)")]
    [SerializeField] private AudioClip[] footstepsDirt;
    [Tooltip("Bước chân trên sàn đá hầm ngục (Dungeon)")]
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
    [Tooltip("Âm thanh mở giao diện hoặc tương tác UI")]
    [SerializeField] private AudioClip uiInteractClip;

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
    public float SfxVolume => sfxVolume;
    public BgmMode CurrentBgmMode => currentBgmMode;
    public AudioClip TorchClip => torchClip;
    public AudioClip PortalClip => portalClip;

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

        bool isDungeon = sceneName.Contains("Dungeon");
        BgmMode targetMode = isDungeon ? BgmMode.Dungeon : BgmMode.Land;

        // Task 5: Không ngắt quãng hoặc phát lại exploreSource nếu BGM hiện tại đã đúng Scene (ví dụ Land)
        if (currentBgmMode == targetMode && (exploreSource != null && exploreSource.isPlaying || exploreRoutine != null))
        {
            return;
        }

        currentBgmMode = BgmMode.None;

        // Ngắt bài cũ đang phát dở trên exploreSource để nạp bài mới của Scene
        if (exploreSource != null) { exploreSource.Stop(); exploreSource.volume = 0f; }

        if (isDungeon)
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
        PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
        UpdateAllSourceVolumes();
    }

    public void SetBgmVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("Audio_BgmVolume", bgmVolume);
        UpdateAllSourceVolumes();
    }

    public void SetSfxVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("Audio_SfxVolume", sfxVolume);
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

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
            Mathf.Abs(sfxVolume - lastInspectorSfxVol) > 0.005f)
        {
            lastInspectorMasterVol = masterVolume;
            lastInspectorBgmVol = bgmVolume;
            lastInspectorSfxVol = sfxVolume;
            UpdateAllSourceVolumes();
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
            if (PlayerPrefs.HasKey("Audio_SfxVolume"))
                sfxVolume = PlayerPrefs.GetFloat("Audio_SfxVolume", sfxVolume);
        }

        lastInspectorMasterVol = masterVolume;
        lastInspectorBgmVol = bgmVolume;
        lastInspectorSfxVol = sfxVolume;
    }

    private void UpdateAllSourceVolumes()
    {
        float targetBgmVol = bgmVolume * masterVolume;
        if (currentBgmMode == BgmMode.Land || currentBgmMode == BgmMode.Dungeon)
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

        while (currentBgmMode == BgmMode.Land || currentBgmMode == BgmMode.Dungeon)
        {
            // 1. Chọn bài ngẫu nhiên khác với bài vừa phát
            int nextIndex = GetRandomTrackIndex(playlist, lastExploreTrackIndex);
            lastExploreTrackIndex = nextIndex;
            AudioClip clip = playlist[nextIndex];

            if (clip != null)
            {
                exploreSource.clip = clip;
                FadeIn(exploreSource, bgmVolume * masterVolume, defaultFadeDuration, ref exploreFadeRoutine);

                // Task 5: Cuối bài hát (2-3s cuối), thực hiện FadeOut êm ái trước khi bước vào khoảng lặng
                float elapsed = 0f;
                float duration = clip.length;
                float fadeOutDuration = Mathf.Min(2.5f, duration * 0.25f);
                float fadeOutStartTime = Mathf.Max(0f, duration - fadeOutDuration);
                bool endFadeStarted = false;

                while (elapsed < duration)
                {
                    if (currentBgmMode != BgmMode.Land && currentBgmMode != BgmMode.Dungeon)
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
                if (currentBgmMode != BgmMode.Land && currentBgmMode != BgmMode.Dungeon) yield break;
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
            Play2D(clip, volume: 1.0f, pitchMin: 0.94f, pitchMax: 1.06f);
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
            Play2D(clip, volume: 0.85f, pitchMin: 0.96f, pitchMax: 1.04f);
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
            Play2D(clip, volume: 1.0f, pitchMin: 0.95f, pitchMax: 1.05f);
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
            Play2D(outOfStaminaClip, volume: 0.9f);
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
            Play2D(staminaEmptyAlertClip, volume: 0.8f, pitchMin: 0.92f, pitchMax: 0.96f);
        }
    }

    /// <summary>
    /// Phát âm thanh tử trận khi máu về 0
    /// </summary>
    public void PlayPlayerDeath()
    {
        if (deathClip != null)
        {
            Play2D(deathClip, volume: 1.0f);
        }
    }

    /// <summary>
    /// Phát âm thanh gạt đòn kim loại đanh thép (Parry)
    /// </summary>
    public void PlayPlayerParry()
    {
        if (parryClip != null)
        {
            Play2D(parryClip, volume: 1.0f, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh chém phản đòn bùng nổ xung lực (Counter Attack)
    /// </summary>
    public void PlayPlayerCounterAttack()
    {
        if (counterAttackClip != null)
        {
            Play2D(counterAttackClip, volume: 1.0f);
        }
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng hồi phục máu (Healing) khi uống bình máu hoặc cầu nguyện tại Chuông Làng
    /// </summary>
    public void PlayHealing(Vector3? position = null, float volume = 0.95f)
    {
        if (healClip == null) return;
        if (position.HasValue)
        {
            Play3D(healClip, position.Value, volume: volume, pitchMin: 0.98f, pitchMax: 1.02f, minDistance: 2f, maxDistance: 20f);
        }
        else
        {
            Play2D(healClip, volume: volume, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh bước chân của người chơi dựa trên môi trường
    /// </summary>
    public void PlayPlayerFootstep(bool isDungeon, float volume = 0.6f)
    {
        if (Time.time < lastFootstepTime + 0.28f) return;
        lastFootstepTime = Time.time;

        AudioClip[] array = isDungeon ? footstepsRock : footstepsDirt;
        AudioClip clip = GetRandomClip(array);
        if (clip != null)
        {
            Play2D(clip, volume: volume, pitchMin: 0.92f, pitchMax: 1.08f);
        }
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
            Play2D(skillEChargeClip, volume: 1.0f, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh gồng lực Skill Q (MediumCharge)
    /// </summary>
    public void PlaySkillQCharge()
    {
        if (skillQChargeClip != null)
        {
            Play2D(skillQChargeClip, volume: 1.0f, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh gồng lực Skill R (BigCharge)
    /// </summary>
    public void PlaySkillRCharge()
    {
        if (skillRChargeClip != null)
        {
            Play2D(skillRChargeClip, volume: 1.0f, pitchMin: 0.98f, pitchMax: 1.02f);
        }
    }

    /// <summary>
    /// Phát âm thanh va chạm nổ kiếm khí Skill E (EImpact) tại vị trí va chạm (3D Spatial)
    /// </summary>
    public void PlaySkillEImpact(Vector3 position)
    {
        if (skillEImpactClip != null)
        {
            Play3D(skillEImpactClip, position, volume: 1.0f, pitchMin: 0.95f, pitchMax: 1.05f, minDistance: 3f, maxDistance: 30f);
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
            Play3D(skillQImpactClip, position.Value, volume: 1.0f, pitchMin: 0.96f, pitchMax: 1.04f, minDistance: 4f, maxDistance: 35f);
        }
        else
        {
            Play2D(skillQImpactClip, volume: 1.0f, pitchMin: 0.96f, pitchMax: 1.04f);
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
            Play3D(skillRImpactClip, position.Value, volume: 1.0f, pitchMin: 0.97f, pitchMax: 1.03f, minDistance: 4f, maxDistance: 35f);
        }
        else
        {
            Play2D(skillRImpactClip, volume: 1.0f, pitchMin: 0.97f, pitchMax: 1.03f);
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
            Play2D(clip, volume: 0.85f, pitchMin: 0.96f, pitchMax: 1.04f);
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
            Play2D(clip, volume: 0.9f, pitchMin: 0.96f, pitchMax: 1.04f);
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
            Play2D(clip, volume: 0.8f, pitchMin: 0.97f, pitchMax: 1.03f);
        }
    }

    /// <summary>
    /// Phát âm thanh vươn vai / ngáp (Streching)
    /// </summary>
    public void PlayIdleStretching()
    {
        if (idleStretchingClip != null)
        {
            Play2D(idleStretchingClip, volume: 0.85f, pitchMin: 0.98f, pitchMax: 1.02f);
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

    public void PlaySkeletonMeleeAttack(Vector3 pos) => Play3D(meleeAttackClip, pos, volume: 0.85f, pitchMin: 0.95f, pitchMax: 1.05f);
    public void PlaySkeletonMeleeHit(Vector3 pos) => Play3D(meleeHitClip, pos, volume: 0.9f);
    public void PlaySkeletonArrowShoot(Vector3 pos) => Play3D(arrowShootClip, pos, volume: 0.85f, pitchMin: 0.95f, pitchMax: 1.05f);
    public void PlaySkeletonArrowHit(Vector3 pos) => Play3D(arrowHitClip, pos, volume: 0.9f);
    public void PlaySkeletonSpellCast(Vector3 pos) => Play3D(spellCastClip, pos, volume: 0.85f);
    public void PlaySkeletonSpellImpact(Vector3 pos) => Play3D(spellImpactClip, pos, volume: 1.0f, pitchMin: 0.93f, pitchMax: 1.07f);
    public void PlaySkeletonGetHit(Vector3 pos) => Play3D(skeletonGetHitClip, pos, volume: 0.85f, pitchMin: 0.93f, pitchMax: 1.07f);
    public void PlaySkeletonDeath(Vector3 pos) => Play3D(skeletonDeathClip, pos, volume: 1.0f, pitchMin: 0.95f, pitchMax: 1.05f);

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO MÔI TRƯỜNG & HẦM NGỤC (ENVIRONMENT SFX)
    // =========================================================================

    public void PlayBarrelBreak(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(barrelBreakClips);
        if (clip != null) Play3D(clip, pos, volume: 0.9f, pitchMin: 0.93f, pitchMax: 1.07f);
    }

    public void PlayVillageBell(Vector3 pos) => Play3D(bellClip, pos, volume: 1.0f);
    public void PlayDoorOpen(Vector3 pos) => Play3D(doorOpenClip, pos, volume: 0.85f);
    public void PlayBossDoorOpen(Vector3 pos) => Play3D(bossDoorOpenClip, pos, volume: 1.0f);
    public void PlayChestOpen(Vector3 pos) => Play3D(chestOpenClip, pos, volume: 0.9f);

    /// <summary>
    /// Phát âm thanh vầng hào quang chói lóa khi mở rương (Chest Aura) với độ lớn theo cấp bậc rương.
    /// volumeMultiplier: Boss = 1.0f, Rare = 0.70f, Normal = 0.35f
    /// </summary>
    public void PlayChestAura(Vector3 pos, float volumeMultiplier = 1.0f)
    {
        if (chestAuraClip == null) return;
        Play3D(chestAuraClip, pos, volume: Mathf.Clamp01(1.0f * volumeMultiplier), pitchMin: 0.98f, pitchMax: 1.02f, minDistance: 3f, maxDistance: 30f);
    }

    public void PlayCoinPickup(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(coinPickupClips);
        if (clip != null) Play3D(clip, pos, volume: 0.8f, pitchMin: 0.95f, pitchMax: 1.08f);
    }

    public void PlayPortalSound(Vector3 pos) => Play3D(portalClip, pos, volume: 0.9f);

    public void PlayDungeonAmbientStinger(Vector3 pos)
    {
        AudioClip clip = GetRandomClip(ambientClips);
        if (clip != null) Play3D(clip, pos, volume: 0.75f);
    }

    // =========================================================================
    // CÁC HÀM PHÁT SFX CHO GIAO DIỆN (UI SFX)
    // =========================================================================

    public void PlayUIUpgradeAnvil() => Play2D(anvilClip, volume: 1.0f);
    public void PlayUIEquip() => Play2D(equipClip, volume: 0.85f);
    public void PlayUIPotionUse() => Play2D(potionClip, volume: 0.9f);
    public void PlayUIShopTransaction() => Play2D(shopClip, volume: 0.85f);
    public void PlayUIDisabledClick() => Play2D(staminaEmptyAlertClip, volume: 0.75f, pitchMin: 0.92f, pitchMax: 0.96f);
    public void PlayUIClick() => PlayUIInteract();
    public void PlayUIItemPickup() => Play2D(itemPickupClip, volume: 0.9f);
    public void PlayUIInteract() => Play2D(uiInteractClip, volume: 0.85f);

    // =========================================================================
    // TIỆN ÍCH CORE PHÁT 2D VÀ 3D (GENERIC AUDIO CORE)
    // =========================================================================

    public void Play2D(AudioClip clip, float volume = 1.0f, float pitchMin = 1.0f, float pitchMax = 1.0f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailable2DSource();
        if (src == null) return;

        src.clip = clip;
        src.volume = volume * sfxVolume * masterVolume;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.Play();
    }

    public void Play3D(AudioClip clip, Vector3 position, float volume = 1.0f, float pitchMin = 0.95f, float pitchMax = 1.05f, float minDistance = 2.0f, float maxDistance = 25.0f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailable3DSource();
        if (src == null) return;

        src.transform.position = position;
        src.clip = clip;
        src.volume = volume * sfxVolume * masterVolume;
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
