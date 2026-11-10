using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý ô trang bị (Vũ khí / Giáp) ở cửa sổ bên trái
  /// </summary>
  public class EquipmentSlotUI : MonoBehaviour,
                                 IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
  {
    [Header("=== CẤU HÌNH LOẠI Ô ===")]
    [Tooltip("Loại trang bị của ô này (Weapon hoặc Armor)")]
    [SerializeField] private ItemType slotType = ItemType.Weapon;

    [Header("=== THÀNH PHẦN UI ===")]
    [Tooltip("Icon hiển thị trang bị đang mặc")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Icon mờ placeholder hiển thị khi chưa có đồ")]
    [SerializeField] private Image placeholderIcon;

    [Tooltip("Nền trắng RarityBG đổi màu theo phẩm cấp của món đang trang bị")]
    [FormerlySerializedAs("rarityBorder")]
    [SerializeField] private Image rarityBg;

    [Tooltip("Thanh bar dưới icon đổi màu theo phẩm cấp của trang bị")]
    [SerializeField] private Image rarityBar;

    [Tooltip("Viền góc trang trí khi đang chọn ô này")]
    [SerializeField] private Image selectHighlight;

    [Tooltip("Button để click mở popup chi tiết hoặc tháo đồ")]
    [SerializeField] private Button slotButton;

    [Tooltip("Ảnh hiển thị tiến trình hồi chiêu CDFill")]
    [SerializeField] private Image cdFillImage;

    [Tooltip("Text hiển thị số lượng vật phẩm (cho ô tiêu hao UsableSlot)")]
    [SerializeField] private TMPro.TextMeshProUGUI stackCountText;

    public int EquippedQuantity { get; private set; } = 1;

    public ItemType SlotType => slotType;
    public ItemData EquippedItem { get; private set; }
    public bool IsEmpty => EquippedItem == null;

    public Sprite RarityBgSprite => rarityBg != null ? rarityBg.sprite : null;
    public Sprite RarityBarSprite => rarityBar != null ? rarityBar.sprite : null;
    public Sprite BorderSprite => transform.Find("Border")?.GetComponent<Image>()?.sprite;

    public event Action<EquipmentSlotUI> OnClicked;

    private bool isInitialized = false;

    private void Awake()
    {
      EnsureInitialized();
    }

    private void EnsureInitialized()
    {
      if (isInitialized) return;
      isInitialized = true;

      AutoFindReferences();

      if (slotButton != null)
      {
        slotButton.onClick.AddListener(HandleClick);

        // Đảm bảo base Image luôn là màu trắng để Button Color Tint (Normal, Hover, Pressed) hiển thị đúng 100%
        if (slotButton.targetGraphic != null)
        {
          slotButton.targetGraphic.color = Color.white;
        }
      }

      // Đảm bảo RarityBG & RarityBar tắt mặc định khi chưa có trang bị
      if (rarityBg != null && EquippedItem == null)
      {
        rarityBg.gameObject.SetActive(false);
      }

      if (rarityBar != null && EquippedItem == null)
      {
        rarityBar.gameObject.SetActive(false);
      }

      if (selectHighlight != null)
      {
        selectHighlight.gameObject.SetActive(false);
      }
    }

    private void AutoFindReferences()
    {
      if (slotButton == null) slotButton = GetComponent<Button>();
      if (itemIcon == null) itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
      if (placeholderIcon == null) placeholderIcon = transform.Find("PlaceHolderIcon")?.GetComponent<Image>();
      if (rarityBg == null) rarityBg = transform.Find("RarityBG")?.GetComponent<Image>();
      if (rarityBar == null) rarityBar = transform.Find("RarityBar")?.GetComponent<Image>();
      if (selectHighlight == null) selectHighlight = transform.Find("SelectHighlight")?.GetComponent<Image>();
      if (cdFillImage == null) cdFillImage = transform.Find("CDFill")?.GetComponent<Image>();
      if (cdFillImage != null) cdFillImage.raycastTarget = false;
      if (stackCountText == null) stackCountText = transform.Find("StackCountText")?.GetComponent<TMPro.TextMeshProUGUI>();

      if (gameObject.name.IndexOf("usable", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        slotType = ItemType.Consumable;
      }
    }

    /// <summary>
    /// Gán trang bị và cập nhật hiển thị
    /// </summary>
    public void SetEquippedItem(ItemData item)
    {
      SetEquippedItem(item, 1);
    }

    public void SetEquippedItem(ItemData item, int quantity)
    {
      EnsureInitialized();
      EquippedItem = item;
      EquippedQuantity = quantity;

      if (item == null || quantity <= 0)
      {
        ClearSlot();
        return;
      }

      // 1. Icon trang bị
      if (itemIcon != null)
      {
        if (item.Icon != null)
        {
          itemIcon.gameObject.SetActive(true);
          itemIcon.enabled = true;
          itemIcon.sprite = item.Icon;
          itemIcon.color = Color.white;
        }
        else
        {
          itemIcon.gameObject.SetActive(false);
        }
      }

      // 2. Ẩn placeholder khi đã có đồ
      if (placeholderIcon != null)
      {
        placeholderIcon.gameObject.SetActive(false);
      }

      Color rarityColor = item.Rarity.GetRarityColor();

      // 3. Đổi màu RarityBG (glow mờ) và RarityBar (thanh bar dưới icon) theo phẩm cấp
      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(true);
        rarityBg.color = rarityColor;
      }

      if (rarityBar != null)
      {
        rarityBar.gameObject.SetActive(true);
        rarityBar.color = rarityColor;
      }

      // 4. Số lượng dồn (cho UsableSlot)
      if (stackCountText != null)
      {
        if (quantity > 1)
        {
          stackCountText.gameObject.SetActive(true);
          stackCountText.text = quantity.ToString();
        }
        else
        {
          stackCountText.gameObject.SetActive(false);
        }
      }
    }

    public void SetCooldownNormalized(float normalized)
    {
      if (cdFillImage != null)
      {
        bool show = slotType == ItemType.Consumable && EquippedItem != null && normalized > 0.001f;
        cdFillImage.gameObject.SetActive(show);
        if (show)
        {
          cdFillImage.fillAmount = normalized;
        }
      }
    }

    /// <summary>
    /// Đưa ô về trạng thái trống (ẩn RarityBG, RarityBar, hiện placeholder nếu có sprite)
    /// </summary>
    public void ClearSlot()
    {
      EnsureInitialized();
      EquippedItem = null;
      EquippedQuantity = 0;

      if (stackCountText != null)
      {
        stackCountText.gameObject.SetActive(false);
      }

      if (cdFillImage != null)
      {
        cdFillImage.gameObject.SetActive(false);
      }

      if (itemIcon != null)
      {
        itemIcon.gameObject.SetActive(false);
        itemIcon.sprite = null;
      }

      if (placeholderIcon != null)
      {
        // Chỉ hiện nếu đã được gán sprite icon mờ, tránh hiện ô vuông trắng
        placeholderIcon.gameObject.SetActive(placeholderIcon.sprite != null);
      }

      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(false);
      }

      if (rarityBar != null)
      {
        rarityBar.gameObject.SetActive(false);
      }

      SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
      if (selectHighlight != null)
      {
        selectHighlight.gameObject.SetActive(isSelected);
      }
    }

    private void HandleClick()
    {
      OnClicked?.Invoke(this);
    }

    /// <summary>
    /// Làm mờ icon khi đang bị nhấc kéo đi
    /// </summary>
    public void SetGhostFade(bool faded)
    {
      if (itemIcon != null)
      {
        Color c = itemIcon.color;
        c.a = faded ? 0.35f : 1.0f;
        itemIcon.color = c;
      }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      if (IsEmpty) return;
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

    public void OnDrop(PointerEventData eventData)
    {
      InventoryDragDropController.Instance?.DropOnEquipSlot(this);
    }
  }
}
