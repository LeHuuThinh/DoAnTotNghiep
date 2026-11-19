using UnityEngine;
using UnityEngine.SceneManagement;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.UI;

/// <summary>
/// Cổng dịch chuyển giữa các Scene (ví dụ: DungeonScene <-> LandScene)
/// Kế thừa IInteractable hỗ trợ đè giữ phím F với thanh HoldFill từ dưới lên trên InteractUI.
/// Tự động kết nối với LoadingScreenManager để chuyển cảnh và nạp PCG mượt mà.
/// </summary>
public class PortalController : MonoBehaviour, IInteractable
{
  [Header("=== CẤU HÌNH DỊCH CHUYỂN ===")]
  [Tooltip("Tên Scene đích muốn tải (ví dụ: DungeonScene hoặc LandScene)")]
  [SerializeField] private string targetSceneName = "LandScene";

  [Tooltip("Số tầng hầm ngục muốn tới (nếu chuyển vào DungeonScene)")]
  [SerializeField] private int targetFloor = 1;

  [Tooltip("Câu gợi ý hiển thị trên UI")]
  [SerializeField] private string interactionPrompt = "Vào Cổng Dịch Chuyển";

  [Tooltip("Tên đối tượng")]
  [SerializeField] private string targetName = "Cổng Không Gian";

  [Tooltip("Bật chế độ đè giữ phím")]
  [SerializeField] private bool requiresHold = true;

  [Tooltip("Thời gian cần đè giữ phím F (giây)")]
  [SerializeField] private float requiredHoldTime = 2.0f;

  [Tooltip("Bán kính có thể tương tác")]
  [SerializeField] private float interactionRadius = 4.5f;

  [Header("=== HIỆU ỨNG TÙY CHỌN ===")]
  [SerializeField] private AudioClip teleportSound;
  [SerializeField] private GameObject teleportVfxPrefab;

  [Header("=== ÂM THANH CỔNG (SPATIAL AURA LOOP) ===")]
  [Tooltip("Âm thanh vầng hào quang ma thuật của cổng (Loop 3D)")]
  [SerializeField] private AudioClip portalClip;

  [Tooltip("Cự ly tối thiểu âm thanh giữ nguyên âm lượng")]
  [SerializeField] private float minDistance = 2.0f;

  [Tooltip("Cự ly tối đa nghe thấy tiếng cổng")]
  [SerializeField] private float maxDistance = 16.0f;

  [Range(0f, 1f)]
  [SerializeField] private float volume = 0.85f;

  private AudioSource audioSource;

  [Header("=== GIAO DIỆN CỔNG THẮNG TRẬN (TASK D02-5D) ===")]
  [Tooltip("Nếu bật, tương tác cổng sẽ mở giao diện 2 cửa sổ VictoryPortalUI (Về làng / Đi tầng 2 / Ở lại) thay vì chuyển cảnh ngay")]
  [SerializeField] private bool openVictoryChoiceUI = false;

  private void Awake()
  {
    // Task 7: Thiết lập AudioSource 3D Spatial Loop cho Cổng
    SetupAudioSource();

    // Tự động kiểm tra và cấu hình scene đích phù hợp nếu bị trùng scene hiện tại
    string activeScene = SceneManager.GetActiveScene().name;
    if (string.IsNullOrEmpty(targetSceneName) || targetSceneName == activeScene)
    {
      if (activeScene == "LandScene")
      {
        targetSceneName = "DungeonScene";
      }
      else if (activeScene == "DungeonScene")
      {
        targetSceneName = "LandScene";
      }
    }
  }

  private void SetupAudioSource()
  {
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }

    audioSource.playOnAwake = false;
    audioSource.loop = true;
    audioSource.spatialBlend = 1.0f; // 100% 3D Spatial Audio
    audioSource.rolloffMode = AudioRolloffMode.Linear;
    audioSource.minDistance = minDistance; // 2.0f
    audioSource.maxDistance = maxDistance; // 16.0f
    audioSource.dopplerLevel = 0f;
  }

  private void Start()
  {
    PlayPortalAuraLoop();
  }

  private void Update()
  {
    // Đảm bảo kết nối âm thanh ngay khi AudioManager sẵn sàng nếu lúc Start chưa có clip
    if (audioSource != null && audioSource.clip == null && AudioManager.HasInstance && AudioManager.Instance.PortalClip != null)
    {
      PlayPortalAuraLoop();
    }
  }

  private void PlayPortalAuraLoop()
  {
    if (audioSource == null) return;

    AudioClip clip = portalClip;
    if (clip == null && AudioManager.HasInstance)
    {
      clip = AudioManager.Instance.PortalClip;
    }

    if (clip != null)
    {
      audioSource.clip = clip;
      float masterVol = AudioManager.HasInstance ? AudioManager.Instance.MasterVolume : 1f;
      float sfxVol = AudioManager.HasInstance ? AudioManager.Instance.SfxVolume : 1f;
      audioSource.volume = volume * sfxVol * masterVol;
      audioSource.Play();
    }
  }

  public void SetVictoryPortalMode(bool isVictoryPortal)
  {
    openVictoryChoiceUI = isVictoryPortal;
    if (isVictoryPortal)
    {
      interactionPrompt = "Enter the Portal";
      targetName = "VictoryPortal";
      requiresHold = true;
      requiredHoldTime = 2.0f;
    }
  }

  public bool IsVictoryPortal()
  {
    // Item 3: Chỉ mở UI lựa chọn khi openVictoryChoiceUI được bật hoặc là Cổng Boss sinh ra
    if (openVictoryChoiceUI) return true;
    if (gameObject.name.Contains("Victory") || (transform.root != null && transform.root.name.Contains("Victory"))) return true;
    return false;
  }

  // Triển khai IInteractable
  public string InteractionPrompt => interactionPrompt;
  public string TargetName => targetName;
  public bool RequiresHold => requiresHold;
  public float HoldDuration => requiresHold ? Mathf.Max(0.01f, requiredHoldTime) : 0f;
  public float InteractionRadius => interactionRadius;
  public Transform InteractionTransform => transform;

  private bool isTransitioning = false;

  private void OnEnable()
  {
    InteractableRegistry.Register(this);
    if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
    {
      audioSource.Play();
    }
  }

  private void OnDisable()
  {
    InteractableRegistry.Unregister(this);
  }

  public bool CanInteract(GameObject player)
  {
    return !isTransitioning && enabled && gameObject.activeInHierarchy;
  }

  public void OnInteract(GameObject player)
  {
    if (isTransitioning) return;

    // Nếu là Cổng Thắng Trận (Task D02-5D): Mở giao diện 2 cửa sổ thay vì chuyển cảnh tức thì
    if (openVictoryChoiceUI || IsVictoryPortal())
    {
      VictoryPortalUI ui = VictoryPortalUI.Instance;
      if (ui == null)
      {
        ui = FindFirstObjectByType<VictoryPortalUI>(FindObjectsInactive.Include);
      }

      if (ui != null)
      {
        Debug.Log("<color=gold>[PortalController]</color> 🌀 Người chơi đã đè giữ Cổng Chiến Thắng -> Mở giao diện VictoryPortalUI!");
        ui.Open();
        return;
      }
      else
      {
        Debug.LogError("[PortalController] ❌ Không tìm thấy VictoryPortalUI trong Scene!");
      }

      // Tuyệt đối không tự động nhảy về LandScene khi chưa chọn trên bảng
      return;
    }

    isTransitioning = true;

    // Task 7: Đã có 3D looping AudioSource trên Portal nên loại bỏ one-shot PlayPortalSound khi tương tác dịch chuyển

    if (teleportVfxPrefab != null)
    {
      Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);
    }

    // Khi trở về Làng (LandScene) từ Dungeon: Tăng ngày +1 và lưu lượng máu còn lại (không tự hồi đầy)
    if (targetSceneName == "LandScene")
    {
      if (GameManager.HasInstance)
      {
        GameManager.Instance.AdvanceDay();
      }

      if (player != null)
      {
        Health playerHp = player.GetComponent<Health>();
        if (playerHp != null && !playerHp.IsDead && GameManager.HasInstance)
        {
          GameManager.Instance.SavePlayerHealth(playerHp.CurrentHealth);
        }
      }
    }

    // Nếu đi từ Làng vào DungeonScene, nạp đúng số tầng đang đi được bảo lưu trong GameManager
    int floorToLoad = targetFloor;
    if (targetSceneName == "DungeonScene" && GameManager.HasInstance)
    {
      floorToLoad = GameManager.Instance.CurrentFloor;
    }

    // Chuyển Scene thông qua LoadingScreenManager (màn hình loading mượt mà kèm mẹo và ảnh nền)
    if (LoadingScreenManager.Instance != null)
    {
      LoadingScreenManager.Instance.LoadScene(targetSceneName, floorToLoad);
    }
    else
    {
      SceneManager.LoadScene(targetSceneName);
    }
  }

  // Tương thích ngược với các Trigger cũ nếu có
  private void OnTriggerEnter(Collider other) { }
  private void OnTriggerExit(Collider other) { }
}
