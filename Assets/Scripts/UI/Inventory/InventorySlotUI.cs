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
                                 IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
                                 IPointerClickHandler
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

    [Tooltip("Badge hiển thị cấp cường hóa (+1 đến +5) màu vàng kim")]
    [SerializeField] private TextMeshProUGUI upgradeBadgeText;

    [Tooltip("Button của ô này để nhận sự kiện Click chuột")]
    [SerializeField] private Button slotButton;

    [Tooltip("Ảnh hiển thị tiến trình hồi chiêu CDFill")]
    [SerializeField] private Image cdFillImage;

    public int ActualBagIndex { get; private set; } = -1;
    public ItemData CurrentItem { get; private set; }
    public int CurrentQuantity { get; private set; }
    public int UpgradeLevel { get; private set; } = 0;
    public EquipmentAffixData AffixData => (InventoryManager.HasInstance && ActualBagIndex >= 0 && ActualBagIndex < InventoryManager.Instance.Slots.Count) ? InventoryManager.Instance.Slots[ActualBagIndex].AffixData : null;
    public bool IsEmpty => CurrentItem == null;

    public Sprite RarityBgSprite => rarityBg != null ? rarityBg.sprite : null;
    public Sprite RarityBarSprite => rarityBar != null ? rarityBar.sprite : null;
    public Sprite BorderSprite => transform.Find("Border")?.GetComponent<Image>()?.sprite;

    public event Action<InventorySlotUI> OnClicked;
    public event Action<InventorySlotUI> OnRightClicked;

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
      if (upgradeBadgeText == null) upgradeBadgeText = transform.Find("UpgradeBadgeText")?.GetComponent<TextMeshProUGUI>();
      if (upgradeBadgeText == null)
      {
        // Tự động khởi tạo badge cấp độ ở góc trên-phải ô nếu chưa có sẵn
        GameObject badgeObj = new GameObject("UpgradeBadgeText");
        badgeObj.transform.SetParent(transform, false);
        var rt = badgeObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, -4f);
        rt.sizeDelta = new Vector2(40f, 22f);

        upgradeBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
        upgradeBadgeText.fontSize = 17f;
        upgradeBadgeText.fontStyle = FontStyles.Bold;
        upgradeBadgeText.alignment = TextAlignmentOptions.TopLeft;
        upgradeBadgeText.color = new Color(1f, 0.84f, 0f, 1f); // #FFD700 Gold
        upgradeBadgeText.raycastTarget = false;
        badgeObj.SetActive(false);
      }
      if (cdFillImage == null) cdFillImage = transform.Find("CDFill")?.GetComponent<Image>();
      if (cdFillImage != null) cdFillImage.raycastTarget = false;
    }

    /// <summary>
    /// Gán vật phẩm và cập nhật hiển thị lên ô UI (kèm cấp cường hóa)
    /// </summary>
    public void SetItem(ItemData item, int quantity, int bagIndex, int upgradeLevel = 0)
    {
      EnsureInitialized();
      CurrentItem = item;
      CurrentQuantity = quantity;
      ActualBagIndex = bagIndex;
      UpgradeLevel = Mathf.Clamp(upgradeLevel, 0, 5);

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

      // 4. Badge cấp cường hóa (+1 đến +5)
      if (upgradeBadgeText != null)
      {
        if (UpgradeLevel > 0 && (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor))
        {
          upgradeBadgeText.gameObject.SetActive(true);
          upgradeBadgeText.text = $"+{UpgradeLevel}";
          upgradeBadgeText.color = new Color(1f, 0.84f, 0f, 1f); // Vàng kim #FFD700
        }
        else
        {
          upgradeBadgeText.gameObject.SetActive(false);
        }
      }
    }

    /// <summary>
    /// Gán vật phẩm tương thích ngược (không truyền upgradeLevel)
    /// </summary>
    public void SetItem(ItemData item, int quantity, int bagIndex)
    {
      int currentUpgrade = 0;
      if (InventoryManager.HasInstance && bagIndex >= 0 && bagIndex < InventoryManager.Instance.Slots.Count)
      {
        currentUpgrade = InventoryManager.Instance.Slots[bagIndex].UpgradeLevel;
      }
      SetItem(item, quantity, bagIndex, currentUpgrade);
    }

    /// <summary>
    /// Làm sạch ô khi không có vật phẩm
    /// </summary>
    public void ClearSlot(int bagIndex = -1)
    {
      EnsureInitialized();
      CurrentItem = null;
      CurrentQuantity = 0;
      UpgradeLevel = 0;
      ActualBagIndex = bagIndex;

      if (upgradeBadgeText != null)
      {
        upgradeBadgeText.gameObject.SetActive(false);
      }

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

    public void OnPointerClick(PointerEventData eventData)
    {
      if (eventData.button == PointerEventData.InputButton.Right)
      {
        OnRightClicked?.Invoke(this);
      }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      if (eventData.button != PointerEventData.InputButton.Left) return;
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
