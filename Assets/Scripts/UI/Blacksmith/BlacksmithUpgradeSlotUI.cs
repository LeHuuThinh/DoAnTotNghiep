using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý tương tác và kéo thả cho ô Đe Rèn (UpgradeSlot trong BeforeWindow):
  /// 1. IDropHandler: Nhận vật phẩm kéo từ túi đồ thả vào đe rèn để cường hóa.
  /// 2. IBeginDragHandler, IDragHandler, IEndDragHandler: Cho phép nhấc trang bị trên đe rèn kéo thả ra lại túi đồ.
  /// 3. IPointerClickHandler: Click chuột trái để xem Preview chi tiết, click chuột phải để tháo đồ về túi đồ.
  /// </summary>
  public class BlacksmithUpgradeSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
  {
    [Header("=== THÀNH PHẦN UI ===")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image rarityBg;
    [SerializeField] private Image rarityBar;
    [SerializeField] private CanvasGroup slotCanvasGroup;

    public ItemData Item => BlacksmithUIController.Instance != null ? BlacksmithUIController.Instance.HeldUpgradeItem : null;
    public int UpgradeLevel => BlacksmithUIController.Instance != null ? BlacksmithUIController.Instance.HeldUpgradeLevel : 0;
    public bool IsEmpty => Item == null;

    public Sprite RarityBgSprite => rarityBg != null ? rarityBg.sprite : null;
    public Sprite RarityBarSprite => rarityBar != null ? rarityBar.sprite : null;

    private void Awake()
    {
      AutoFindReferences();
    }

    public void AutoFindReferences()
    {
      if (itemIcon == null) itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
      if (rarityBg == null) rarityBg = transform.Find("RarityBG")?.GetComponent<Image>();
      if (rarityBar == null) rarityBar = transform.Find("RarityBar")?.GetComponent<Image>();

      if (slotCanvasGroup == null)
      {
        slotCanvasGroup = GetComponent<CanvasGroup>();
        if (slotCanvasGroup == null)
        {
          slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
      }
    }

    /// <summary>
    /// Xử lý khi người chơi thả một vật phẩm kéo từ túi đồ vào ô Đe Rèn
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
      if (InventoryDragDropController.Instance == null || !InventoryDragDropController.Instance.IsDragging) return;

      var sourceBagSlot = InventoryDragDropController.Instance.SourceBagSlot;
      var draggedItem = InventoryDragDropController.Instance.DraggedItem;

      if (sourceBagSlot == null || draggedItem == null) return;

      // Chỉ chấp nhận Vũ khí (Weapon) hoặc Giáp (Armor)
      if (draggedItem.ItemType != ItemType.Weapon && draggedItem.ItemType != ItemType.Armor)
      {
        Debug.LogWarning($"[Blacksmith] Chỉ có thể đặt Vũ Khí hoặc Áo Giáp vào đe rèn, không nhận {draggedItem.ItemType}!");
        return;
      }

      int bagIndex = sourceBagSlot.ActualBagIndex;
      if (bagIndex < 0) return;

      // Nạp trang bị vào đe rèn
      if (BlacksmithUIController.Instance != null)
      {
        BlacksmithUIController.Instance.PutItemIntoUpgrade(bagIndex);
      }

      InventoryDragDropController.Instance.EndDrag(eventData);
    }

    /// <summary>
    /// Bắt đầu kéo trang bị từ đe rèn ra ngoài
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
      if (eventData.button != PointerEventData.InputButton.Left) return;
      if (IsEmpty || BlacksmithUIController.Instance == null) return;
      InventoryDragDropController.Instance?.StartDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
      InventoryDragDropController.Instance?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
      InventoryDragDropController.Instance?.EndDrag(eventData);
    }

    /// <summary>
    /// Click chuột vào ô đe rèn
    /// - Chuột phải: Tháo trang bị trên đe rèn trả lại túi đồ.
    /// - Chuột trái: Mở bảng xem trước chi tiết (ShowPreview).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
      if (IsEmpty || BlacksmithUIController.Instance == null) return;

      if (eventData.button == PointerEventData.InputButton.Right)
      {
        // Chuột phải: Lấy đồ ra túi
        BlacksmithUIController.Instance.TakeOutUpgradeItem();
      }
      else if (eventData.button == PointerEventData.InputButton.Left)
      {
        // Chuột trái: Mở xem thông tin
        BlacksmithUIController.Instance.ShowUpgradeSlotPreview();
      }
    }

    /// <summary>
    /// Làm mờ nhẹ khi đang kéo đi
    /// </summary>
    public void SetGhostFade(bool fade)
    {
      if (slotCanvasGroup != null)
      {
        slotCanvasGroup.alpha = fade ? 0.35f : 1f;
      }
    }
  }
}
