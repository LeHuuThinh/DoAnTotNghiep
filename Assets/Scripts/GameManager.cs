using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trình Quản Lý Vòng Lặp Trò Chơi Toàn Cục (Task GM-01 & GM-02):
/// - Singleton DontDestroyOnLoad lưu trữ dữ liệu xuyên scene (CurrentDay, CurrentFloor).
/// - Quản lý vòng lặp Ngày & Tầng hầm ngục (Day Loop & Floor Progression).
/// - Xử lý Vòng lặp Tử trận & Hồi sinh tại Chuông Làng (Death & Respawn Loop - Task GM-02).
/// - Đồng bộ với LoadingScreenManager, InfoUIController, và DeadScreenUIController.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    private static bool isApplicationQuitting = false;

    public static bool IsApplicationQuitting => isApplicationQuitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isApplicationQuitting = false;
        instance = null;
    }

    public static GameManager Instance
    {
        get
        {
            if (isApplicationQuitting)
            {
                return instance;
            }

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
            instance = FindFirstObjectByType<GameManager>();
            return instance != null;
        }
    }

    [Header("=== TRẠNG THÁI TIẾN TRÌNH (CORE PROGRESSION) ===")]
    [Tooltip("Số ngày hiện tại của thế giới (Bắt đầu từ Day 1)")]
    [SerializeField] private int currentDay = 1;

    [Tooltip("Số tầng hầm ngục hiện tại (Bắt đầu từ Floor 1)")]
    [SerializeField] private int currentFloor = 1;

    [Header("=== CẤU HÌNH TÊN SCENE ===")]
    [SerializeField] private string townSceneName = "LandScene";
    [SerializeField] private string dungeonSceneName = "DungeonScene";

    [Header("=== DỮ LIỆU COMBAT LƯU TRỮ (PERSISTENT COMBAT STATS) ===")]
    [Tooltip("Lượng Spirit được lưu trữ xuyên scene")]
    [SerializeField] private float savedSpirit = 0f;
    [SerializeField] private bool hasSavedSpirit = false;

    [Tooltip("Lượng Máu được lưu trữ khi chuyển giữa các tầng Dungeon")]
    [SerializeField] private float savedHealth = -1f;
    [SerializeField] private bool hasSavedHealth = false;

    public float SavedSpirit => savedSpirit;
    public bool HasSavedSpirit => hasSavedSpirit;
    public float SavedHealth => savedHealth;
    public bool HasSavedHealth => hasSavedHealth;

    public void SavePlayerSpirit(float spirit)
    {
        savedSpirit = Mathf.Max(0f, spirit);
        hasSavedSpirit = true;
    }

    public void ResetPlayerSpirit()
    {
        savedSpirit = 0f;
        hasSavedSpirit = true;
    }

    public void SavePlayerHealth(float health)
    {
        savedHealth = health;
        hasSavedHealth = true;
    }

    public void ClearSavedHealth()
    {
        savedHealth = -1f;
        hasSavedHealth = false;
    }

    [Header("=== HỒI SINH TẠI CHUÔNG LÀNG (RESPAWN SETTINGS) ===")]
    [Tooltip("Tên GameObject của Chuông Làng trong LandScene")]
    [SerializeField] private string villageBellObjectName = "bell";

    [Tooltip("Khoảng cách đứng trước chuông khi hồi sinh (m)")]
    [SerializeField] private float respawnBellOffset = 2.5f;

    // Sự kiện toàn cục cho các hệ thống UI & Gameplay đăng ký
    public event Action<int> OnDayChanged;
    public event Action<int> OnFloorChanged;
    public event Action<string, int, int> OnGameStateChanged; // (sceneName, day, floor)

    public int CurrentDay => currentDay;
    public int CurrentFloor => currentFloor;
    public string TownSceneName => townSceneName;
    public string DungeonSceneName => dungeonSceneName;

    public bool IsInTown => SceneManager.GetActiveScene().name == townSceneName;
    public bool IsInDungeon => SceneManager.GetActiveScene().name == dungeonSceneName;

    private bool pendingRespawnAtBell = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeBeforeSceneLoad()
    {
        EnsureInstance();
    }

    /// <summary>
    /// Đảm bảo luôn tồn tại một GameManager duy nhất xuyên suốt vòng đời game.
    /// </summary>
    public static void EnsureInstance()
    {
        if (isApplicationQuitting) return;
        if (instance != null) return;

        instance = FindFirstObjectByType<GameManager>();
        if (instance != null)
        {
            DontDestroyOnLoad(instance.gameObject);
            return;
        }

        GameObject go = new GameObject("[GameManager]");
        instance = go.AddComponent<GameManager>();
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
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Đồng bộ số tầng từ LoadingScreenManager nếu có
        if (InfiniteLabyrinth.UI.LoadingScreenManager.Instance != null)
        {
            int loadingFloor = InfiniteLabyrinth.UI.LoadingScreenManager.Instance.CurrentFloor;
            if (scene.name == dungeonSceneName && loadingFloor > 0)
            {
                currentFloor = loadingFloor;
            }
        }

        // 2. Nếu tải LandScene (Làng), bảo lưu số tầng đang đi (không reset về 1)
        // Số tầng hiện tại được lưu và hiển thị liên tục ngay cả trong LandScene.

        // Khi vào Dungeon: Reset cờ để sẵn sàng tăng ngày cho lần về Làng tiếp theo
        if (scene.name == dungeonSceneName)
        {
            dayAdvancedForCurrentTrip = false;
        }

        // Tự động kiểm tra: Nếu trở về Làng từ Dungeon mà Ngày chưa được tăng, tự động tăng Ngày +1
        if (scene.name == townSceneName && (previousSceneName == dungeonSceneName || pendingRespawnAtBell))
        {
            if (!dayAdvancedForCurrentTrip)
            {
                AdvanceDay();
            }
        }

        previousSceneName = scene.name;

        OnGameStateChanged?.Invoke(scene.name, currentDay, currentFloor);
        Debug.Log($"<color=cyan>[GameManager]</color> 🌐 Nạp Scene: <b>{scene.name}</b> | Day: {currentDay} | Floor: {currentFloor}");

        // Đồng bộ ngay lập tức lên InfoUIController nếu có trong Scene
        InfiniteLabyrinth.UI.InfoUIController infoUI = FindFirstObjectByType<InfiniteLabyrinth.UI.InfoUIController>();
        if (infoUI != null)
        {
            infoUI.UpdateTopCornerDisplay();
        }

        // 3. Đảm bảo Chuông Làng có component tương tác hồi máu & spirit
        if (scene.name == townSceneName)
        {
            EnsureVillageBellInteractable();
        }

        // 4. Nếu đang có lệnh hồi sinh về Chuông Làng sau khi tử trận
        if (scene.name == townSceneName && pendingRespawnAtBell)
        {
            StartCoroutine(RespawnAtVillageRoutine());
        }
    }

    /// <summary>
    /// Kích hoạt chuỗi Hồi sinh tại Chuông Làng sau khi người chơi tử trận (Task GM-02):
    /// 1. Tăng ngày: CurrentDay += 1
    /// 2. Đặt lại tầng: CurrentFloor = 1
    /// 3. Chuyển cảnh về LandScene
    /// 4. Đặt vị trí tại Chuông Trung Tâm Làng và hồi phục 100% Máu & Thể lực
    /// </summary>
    public void RespawnAtVillage()
    {
        Debug.Log("<color=red>[GameManager]</color> 💀 Player tử trận! Đang hồi sinh về Chuông Làng...");
        AdvanceDay();
        // Item 2: Khi về làng thì không reset tầng, lưu số tầng đang đi
        pendingRespawnAtBell = true;

        if (InfiniteLabyrinth.UI.LoadingScreenManager.Instance != null)
        {
            InfiniteLabyrinth.UI.LoadingScreenManager.Instance.LoadScene(townSceneName, currentFloor);
        }
        else
        {
            SceneManager.LoadScene(townSceneName);
        }
    }

    /// <summary>
    /// Đảm bảo Chuông Làng Haven có component tương tác VillageBellInteractable để cầu nguyện hồi máu/spirit.
    /// </summary>
    public void EnsureVillageBellInteractable()
    {
        GameObject bellObj = GameObject.Find(villageBellObjectName);
        if (bellObj == null) bellObj = GameObject.Find("bell");
        if (bellObj != null)
        {
            if (bellObj.GetComponent<InfiniteLabyrinth.Interaction.VillageBellInteractable>() == null)
            {
                bellObj.AddComponent<InfiniteLabyrinth.Interaction.VillageBellInteractable>();
                Debug.Log("<color=green>[GameManager]</color> 🔔 Đã tự động gắn VillageBellInteractable vào Chuông Làng!");
            }
        }
    }

    private IEnumerator RespawnAtVillageRoutine()
    {
        // 1. Chờ LoadingScreenManager nạp xong hoàn toàn nếu có
        if (InfiniteLabyrinth.UI.LoadingScreenManager.Instance != null)
        {
            while (InfiniteLabyrinth.UI.LoadingScreenManager.Instance.IsLoading)
            {
                yield return null;
            }
        }

        yield return null;
        yield return new WaitForEndOfFrame();

        // 2. Tìm Player trong Scene Làng
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        // 3. Dịch chuyển Player đến cạnh Chuông Làng
        GameObject bellObj = GameObject.Find(villageBellObjectName);
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (bellObj != null)
        {
            spawnPos = bellObj.transform.position + Vector3.back * respawnBellOffset;
            spawnRot = Quaternion.LookRotation(Vector3.forward);
        }
        else if (player != null)
        {
            spawnPos = player.transform.position;
            spawnRot = player.transform.rotation;
        }

        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 8f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnPos + Vector3.up * 0.15f;
            player.transform.rotation = spawnRot;

            if (cc != null) cc.enabled = true;

            // 4. Hồi 100% Máu
            Health health = player.GetComponent<Health>();
            if (health != null)
            {
                health.RefillToFull();
            }

            // 5. Hồi 100% Thể lực (Stamina) và 100% Thần lực (Spirit) sau khi tử trận
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.RefillSpiritToFull();
                stats.RestoreStamina(9999f);
            }

            // 6. Khôi phục toàn bộ quyền điều khiển và Animator
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.enabled = true;
                pc.UnlockMovement();
            }

            PlayerCombat pcb = player.GetComponent<PlayerCombat>();
            if (pcb != null) pcb.enabled = true;

            TargetLockSystem tls = player.GetComponent<TargetLockSystem>();
            if (tls != null) tls.enabled = true;

            RHandAdjust rHand = player.GetComponentInChildren<RHandAdjust>();
            if (rHand != null) rHand.enabled = true;

            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Update(0f);
            }

            Debug.Log($"<color=green>[GameManager]</color> 🔔 Hồi sinh thành công tại Chuông Làng ({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1})! Máu & Thể lực đã hồi phục 100%.");
        }

        // 7. Khóa lại chuột cho góc nhìn thứ 3
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 8. Đảm bảo ẩn DeadScreenUI nếu có
        InfiniteLabyrinth.UI.DeadScreenUIController deadUI = FindFirstObjectByType<InfiniteLabyrinth.UI.DeadScreenUIController>();
        if (deadUI != null)
        {
            deadUI.HideImmediate();
        }

        // 9. Cập nhật HUD và kích hoạt biểu ngữ giới thiệu Làng
        InfiniteLabyrinth.UI.InfoUIController infoUI = FindFirstObjectByType<InfiniteLabyrinth.UI.InfoUIController>();
        if (infoUI != null)
        {
            infoUI.UpdateTopCornerDisplay();
            infoUI.TriggerCurrentAreaIntro();
        }

        pendingRespawnAtBell = false;
    }

    /// <summary>
    /// Chuyển sang ngày mới (CurrentDay += 1)
    /// </summary>
    private string previousSceneName = "";
    private bool dayAdvancedForCurrentTrip = false;

    public void AdvanceDay()
    {
        if (dayAdvancedForCurrentTrip) return;
        dayAdvancedForCurrentTrip = true;
        currentDay++;
        Debug.Log($"<color=yellow>[GameManager]</color> 🌅 Ngày mới bắt đầu: <b>Day {currentDay}</b> (Trở về Làng)");
        OnDayChanged?.Invoke(currentDay);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }

    /// <summary>
    /// Đặt ngày cụ thể (Dùng cho Save/Load hoặc Cheat/Test)
    /// </summary>
    public void SetDay(int day)
    {
        currentDay = Mathf.Max(1, day);
        OnDayChanged?.Invoke(currentDay);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }

    /// <summary>
    /// Tiến vào tầng kế tiếp (CurrentFloor += 1)
    /// </summary>
    public void AdvanceFloor()
    {
        currentFloor++;
        Debug.Log($"<color=orange>[GameManager]</color> 🏰 Tiến xuống tầng sâu hơn: <b>Floor {currentFloor}</b>");
        OnFloorChanged?.Invoke(currentFloor);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }

    /// <summary>
    /// Đặt tầng hầm ngục cụ thể
    /// </summary>
    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
        OnFloorChanged?.Invoke(currentFloor);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }

    /// <summary>
    /// Đặt lại tầng về 1 khi trở về làng an toàn
    /// </summary>
    public void ResetFloorToTown()
    {
        currentFloor = 1;
        OnFloorChanged?.Invoke(currentFloor);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }

    /// <summary>
    /// Đặt lại toàn bộ tiến trình về trạng thái ban đầu (Day 1, Floor 1)
    /// </summary>
    public void ResetProgress()
    {
        currentDay = 1;
        currentFloor = 1;
        OnDayChanged?.Invoke(currentDay);
        OnFloorChanged?.Invoke(currentFloor);
        OnGameStateChanged?.Invoke(SceneManager.GetActiveScene().name, currentDay, currentFloor);
    }
}
