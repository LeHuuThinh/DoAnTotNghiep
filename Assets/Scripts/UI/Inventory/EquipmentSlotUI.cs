using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý ô trang bị (Vũ khí / Giáp) ở cửa sổ bên trái
  /// </summary>
  public class EquipmentSlotUI : MonoBehaviour
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

    [Tooltip("Viền góc trang trí khi đang chọn ô này")]
    [SerializeField] private Image selectHighlight;

    [Tooltip("Button để click mở popup chi tiết hoặc tháo đồ")]
    [SerializeField] private Button slotButton;

    public ItemType SlotType => slotType;
    public ItemData EquippedItem { get; private set; }
    public bool IsEmpty => EquippedItem == null;

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

      // Đảm bảo RarityBG tắt mặc định khi chưa có trang bị
      if (rarityBg != null && EquippedItem == null)
      {
        rarityBg.gameObject.SetActive(false);
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
      if (selectHighlight == null) selectHighlight = transform.Find("SelectHighlight")?.GetComponent<Image>();
    }

    /// <summary>
    /// Gán trang bị và cập nhật hiển thị
    /// </summary>
    public void SetEquippedItem(ItemData item)
    {
      EnsureInitialized();
      EquippedItem = item;

      if (item == null)
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

      // 3. Đổi màu RarityBG theo phẩm cấp
      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(true);
        rarityBg.color = item.Rarity.GetRarityColor();
      }
    }

    /// <summary>
    /// Đưa ô về trạng thái trống (ẩn RarityBG, hiện placeholder nếu có sprite)
    /// </summary>
    public void ClearSlot()
    {
      EnsureInitialized();
      EquippedItem = null;

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
  }
}
