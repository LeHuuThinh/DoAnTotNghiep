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

  [Header("=== GIAO DIỆN CỔNG THẮNG TRẬN (TASK D02-5D) ===")]
  [Tooltip("Nếu bật, tương tác cổng sẽ mở giao diện 2 cửa sổ VictoryPortalUI (Về làng / Đi tầng 2 / Ở lại) thay vì chuyển cảnh ngay")]
  [SerializeField] private bool openVictoryChoiceUI = false;

  public void SetVictoryPortalMode(bool isVictoryPortal)
  {
    openVictoryChoiceUI = isVictoryPortal;
    if (isVictoryPortal)
    {
      interactionPrompt = "Vào Cổng Thắng Trận (Giữ F)";
      targetName = "Cổng Chiến Thắng";
      requiresHold = true;
      requiredHoldTime = 2.0f;
    }
  }

  public bool IsVictoryPortal()
  {
    if (openVictoryChoiceUI) return true;
    if (gameObject.name.Contains("Victory") || (transform.root != null && transform.root.name.Contains("Victory"))) return true;
    if (transform.parent != null && (transform.parent.name.Contains("Socket") || transform.parent.name.Contains("Pedestal") || transform.parent.name.Contains("Boss") || transform.parent.name.Contains("Victory"))) return true;
    if (SceneManager.GetActiveScene().name == "DungeonScene") return true;
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

    if (teleportSound != null)
    {
      AudioSource.PlayClipAtPoint(teleportSound, transform.position);
    }

    if (teleportVfxPrefab != null)
    {
      Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);
    }

    // Chuyển Scene thông qua LoadingScreenManager (màn hình loading mượt mà kèm mẹo và ảnh nền)
    if (LoadingScreenManager.Instance != null)
    {
      LoadingScreenManager.Instance.LoadScene(targetSceneName, targetFloor);
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
