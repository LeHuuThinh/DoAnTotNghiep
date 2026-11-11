using UnityEngine;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Shop;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Component gắn vào các NPC Cửa Hàng (EquipmentShopNPC, SuppliesShopNPC)
  /// Cho phép người chơi nhấn F lại gần để mở giao diện Cửa Hàng tương ứng
  /// </summary>
  public class ShopInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH CỬA HÀNG ===")]
    [Tooltip("Dữ liệu cửa hàng (EquipmentShopData hoặc SuppliesShopData)")]
    [SerializeField] private ShopData shopData;

    [Tooltip("Câu nhắc hiển thị khi lại gần (ví dụ: 'Browse Equipment', 'Browse Supplies')")]
    [SerializeField] private string interactionPrompt = "Browse Goods";

    [Tooltip("Tên thương nhân hiển thị")]
    [SerializeField] private string targetName = "Merchant";

    [Tooltip("Bán kính có thể tương tác (mét)")]
    [SerializeField] private float interactionRadius = 2.8f;

    public string InteractionPrompt => interactionPrompt;
    public string TargetName => targetName;
    public bool RequiresHold => false; // Tap phím F ăn ngay
    public float HoldDuration => 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

    public ShopData ShopData => shopData;

    public void SetShopData(ShopData data, string prompt, string name)
    {
      shopData = data;
      interactionPrompt = prompt;
      targetName = name;
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
      if (!enabled || !gameObject.activeInHierarchy || shopData == null) return false;
      // Không cho phép tương tác nếu Shop, Blacksmith hoặc Túi đồ đang mở sẵn
      if (ShopUIController.IsOpen || BlacksmithUIController.IsOpen || InventoryUIController.IsOpen) return false;
      return true;
    }

    public void OnInteract(GameObject player)
    {
      if (!CanInteract(player)) return;

      if (ShopUIController.Instance != null)
      {
        ShopUIController.Instance.OpenShop(shopData);
      }
      else
      {
        Debug.LogWarning("[ShopInteractable] Không tìm thấy ShopUIController trong Scene!");
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
  }
}
