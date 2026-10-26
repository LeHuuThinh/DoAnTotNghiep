using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.EventSystems;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý hiển thị và tương tác của 1 ô trong lưới túi đồ 30 ô
  /// </summary>
  public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    [Header("=== THÀNH PHẦN UI ===")]
    [Tooltip("Icon hiển thị ảnh đại diện của vật phẩm")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Ảnh nền trắng RarityBG dùng để tô màu theo phẩm cấp (Common, Rare, Epic, Legendary)")]
    [FormerlySerializedAs("rarityBorder")]
    [SerializeField] private Image rarityBg;

    [Tooltip("Viền góc trang trí hiển thị khi ô này được chọn")]
    [SerializeField] private Image selectHighlight;

    [Tooltip("Text hiển thị số lượng (chỉ hiện khi Stack > 1)")]
    [SerializeField] private TextMeshProUGUI stackCountText;

    [Tooltip("Button của ô này để nhận sự kiện Click chuột")]
    [SerializeField] private Button slotButton;

    public int ActualBagIndex { get; private set; } = -1;
    public ItemData CurrentItem { get; private set; }
    public int CurrentQuantity { get; private set; }
    public bool IsEmpty => CurrentItem == null;

    public event Action<InventorySlotUI> OnClicked;

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

      // Đảm bảo RarityBG tắt mặc định khi chưa có item
      if (rarityBg != null && CurrentItem == null)
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
      if (rarityBg == null) rarityBg = transform.Find("RarityBG")?.GetComponent<Image>();
      if (selectHighlight == null) selectHighlight = transform.Find("SelectHighlight")?.GetComponent<Image>();
      if (stackCountText == null) stackCountText = transform.Find("StackCountText")?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Gán vật phẩm và cập nhật hiển thị lên ô UI
    /// </summary>
    public void SetItem(ItemData item, int quantity, int bagIndex)
    {
      EnsureInitialized();
      CurrentItem = item;
      CurrentQuantity = quantity;
      ActualBagIndex = bagIndex;

      if (item == null || quantity <= 0)
      {
        ClearSlot(bagIndex);
        return;
      }

      // 1. Icon vật phẩm (nếu chưa có sprite thì ẩn để không hiện ô vuông trắng)
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

      // 2. Nền màu theo độ hiếm (RarityBG)
      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(true);
        rarityBg.color = item.Rarity.GetRarityColor();
      }

      // 3. Số lượng dồn
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

    /// <summary>
    /// Làm sạch ô khi không có vật phẩm
    /// </summary>
    public void ClearSlot(int bagIndex = -1)
    {
      EnsureInitialized();
      CurrentItem = null;
      CurrentQuantity = 0;
      ActualBagIndex = bagIndex;

      if (itemIcon != null)
      {
        itemIcon.gameObject.SetActive(false);
        itemIcon.sprite = null;
      }

      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(false);
      }

      if (stackCountText != null)
      {
        stackCountText.gameObject.SetActive(false);
      }

      SetSelected(false);
    }

    /// <summary>
    /// Bật / Tắt viền trang trí góc khi được click chọn
    /// </summary>
    public void SetSelected(bool isSelected)
    {
      EnsureInitialized();
      if (selectHighlight != null)
      {
        selectHighlight.gameObject.SetActive(isSelected);
      }
    }

    private void HandleClick()
    {
      OnClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
  }
}
