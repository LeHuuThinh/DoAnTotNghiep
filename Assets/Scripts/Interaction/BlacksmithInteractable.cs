using UnityEngine;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Component gắn vào NPC Thợ Rèn (BlackSmithNPC) tại Lò Rèn Haven (Task RPG-08C)
  /// Cho phép người chơi nhấn F lại gần để mở giao diện Lò Rèn (Cường hóa & Chế tạo)
  /// </summary>
  public class BlacksmithInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH TƯƠNG TÁC THỢ RÈN ===")]
    [Tooltip("Câu nhắc hiển thị khi lại gần")]
    [SerializeField] private string interactionPrompt = "Forge & Upgrade Armaments";

    [Tooltip("Tên thợ rèn hiển thị")]
    [SerializeField] private string targetName = "Master Blacksmith";

    [Tooltip("Bán kính có thể tương tác (mét)")]
    [SerializeField] private float interactionRadius = 3.2f;

    public string InteractionPrompt => interactionPrompt;
    public string TargetName => targetName;
    public bool RequiresHold => false; // Tap phím F ăn ngay
    public float HoldDuration => 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

    public void Setup(string prompt, string name, float radius)
    {
      interactionPrompt = prompt;
      targetName = name;
      interactionRadius = radius;
    }

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
      if (!enabled || !gameObject.activeInHierarchy) return false;
      // Không cho phép tương tác nếu Blacksmith, Shop hoặc Túi đồ đang mở sẵn
      if (BlacksmithUIController.IsOpen || ShopUIController.IsOpen || InventoryUIController.IsOpen) return false;
      return true;
    }

    public void OnInteract(GameObject player)
    {
      if (!CanInteract(player)) return;

      if (BlacksmithUIController.Instance != null)
      {
        BlacksmithUIController.Instance.OpenBlacksmith();
      }
      else
      {
        Debug.LogWarning("[BlacksmithInteractable] Không tìm thấy BlacksmithUIController trong Scene!");
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = new Color(1f, 0.5f, 0.1f, 1f); // Màu cam rực lửa lò rèn
      Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
  }
}
