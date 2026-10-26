using UnityEngine;
using UnityEngine.Events;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Component tương tác đa năng dùng cho bất kỳ đối tượng nào (NPC, Thợ rèn, Đòn bẩy, Bia đá...)
  /// Cho phép thiết lập câu lệnh, chế độ Hold/Tap và sự kiện OnInteract trong Inspector mà không cần viết code mới.
  /// </summary>
  public class GenericInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH TƯƠNG TÁC ===")]
    [Tooltip("Câu gợi ý hiển thị trên UI (ví dụ: 'Nói chuyện', 'Kéo đòn bẩy', 'Đọc văn bia')")]
    [SerializeField] private string interactionPrompt = "Tương tác";

    [Tooltip("Tên đối tượng (ví dụ: 'Thợ Rèn Brokk', 'Đòn Bẩy Cửa')")]
    [SerializeField] private string targetName = "";

    [Tooltip("Bật chế độ đè giữ phím (false = Tap bấm ăn ngay)")]
    [SerializeField] private bool requiresHold = false;

    [Tooltip("Thời gian cần giữ phím (giây), chỉ có tác dụng khi requiresHold = true")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("Bán kính có thể tương tác (mét)")]
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("=== SỰ KIỆN KHI TƯƠNG TÁC HOÀN THÀNH ===")]
    [SerializeField] private UnityEvent<GameObject> onInteractEvent;

    public string InteractionPrompt => interactionPrompt;
    public string TargetName => targetName;
    public bool RequiresHold => requiresHold;
    public float HoldDuration => requiresHold ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

    private bool isInteractable = true;

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
      return isInteractable && enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      if (!CanInteract(player)) return;
      onInteractEvent?.Invoke(player);
    }

    public void SetInteractable(bool state)
    {
      isInteractable = state;
    }
  }
}
