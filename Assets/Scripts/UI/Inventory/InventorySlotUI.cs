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
  public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                 IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
  {
    [Header("=== THÀNH PHẦN UI ===")]
    [Tooltip("Icon hiển thị ảnh đại diện của vật phẩm")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Ảnh nền trắng RarityBG dùng để tô màu theo phẩm cấp (Common, Rare, Epic, Legendary)")]
    [FormerlySerializedAs("rarityBorder")]
    [SerializeField] private Image rarityBg;

    [Tooltip("Thanh bar dưới icon đổi màu theo phẩm cấp của vật phẩm")]
    [SerializeField] private Image rarityBar;

    [Tooltip("Viền góc trang trí hiển thị khi ô này được chọn")]
    [SerializeField] private Image selectHighlight;

    [Tooltip("Text hiển thị số lượng (chỉ hiện khi Stack > 1)")]
    [SerializeField] private TextMeshProUGUI stackCountText;

    [Tooltip("Button của ô này để nhận sự kiện Click chuột")]
    [SerializeField] private Button slotButton;

    [Tooltip("Ảnh hiển thị tiến trình hồi chiêu CDFill")]
    [SerializeField] private Image cdFillImage;

    public int ActualBagIndex { get; private set; } = -1;
    public ItemData CurrentItem { get; private set; }
    public int CurrentQuantity { get; private set; }
    public bool IsEmpty => CurrentItem == null;

    public Sprite RarityBgSprite => rarityBg != null ? rarityBg.sprite : null;
    public Sprite RarityBarSprite => rarityBar != null ? rarityBar.sprite : null;
    public Sprite BorderSprite => transform.Find("Border")?.GetComponent<Image>()?.sprite;

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

      // Đảm bảo RarityBG & RarityBar tắt mặc định khi chưa có item
      if (rarityBg != null && CurrentItem == null)
      {
        rarityBg.gameObject.SetActive(false);
      }

      if (rarityBar != null && CurrentItem == null)
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
      if (rarityBg == null) rarityBg = transform.Find("RarityBG")?.GetComponent<Image>();
      if (rarityBar == null) rarityBar = transform.Find("RarityBar")?.GetComponent<Image>();
      if (selectHighlight == null) selectHighlight = transform.Find("SelectHighlight")?.GetComponent<Image>();
      if (stackCountText == null) stackCountText = transform.Find("StackCountText")?.GetComponent<TextMeshProUGUI>();
      if (cdFillImage == null) cdFillImage = transform.Find("CDFill")?.GetComponent<Image>();
      if (cdFillImage != null) cdFillImage.raycastTarget = false;
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
          itemIcon.enabled = true;
          itemIcon.sprite = item.Icon;
          itemIcon.color = Color.white;
        }
        else
        {
          itemIcon.gameObject.SetActive(false);
        }
      }

      Color rarityColor = item.Rarity.GetRarityColor();

      // 2. Nền glow mờ (RarityBG) và thanh bar dưới icon (RarityBar)
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

      if (rarityBar != null)
      {
        rarityBar.gameObject.SetActive(false);
      }

      if (stackCountText != null)
      {
        stackCountText.gameObject.SetActive(false);
      }

      if (cdFillImage != null)
      {
        cdFillImage.gameObject.SetActive(false);
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

    /// <summary>
    /// Làm mờ icon khi đang bị nhấc kéo đi
    /// </summary>
    public void SetCooldownNormalized(float normalized)
    {
      if (cdFillImage != null)
      {
        bool show = CurrentItem != null && CurrentItem.ItemType == ItemType.Consumable && normalized > 0.001f;
        cdFillImage.gameObject.SetActive(show);
        if (show)
        {
          cdFillImage.fillAmount = normalized;
        }
      }
    }

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
      InventoryDragDropController.Instance?.DropOnBagSlot(this);
    }
  }
}
