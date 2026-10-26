using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý bảng hiển thị chi tiết vật phẩm bám theo ô đồ được chọn và các nút hành động (Equip / Unequip / Use / Drop)
  /// </summary>
  public class ItemDetailPopupUI : MonoBehaviour
  {
    [Header("=== THÀNH PHẦN HIỂN THỊ ===")]
    [Tooltip("Tên vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Text hiển thị độ hiếm (Common, Rare, Epic, Legendary)")]
    [SerializeField] private TextMeshProUGUI itemRarityText;

    [Tooltip("Text hiển thị phân loại (Vũ khí, Giáp, Tiêu hao...)")]
    [SerializeField] private TextMeshProUGUI itemTypeText;

    [Tooltip("Icon lớn xem trước")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Mô tả / Cốt truyện vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Tooltip("Danh sách các chỉ số cộng thêm (Sát thương, giáp, máu, hồi phục...)")]
    [SerializeField] private TextMeshProUGUI itemStatsText;

    [Header("=== CÁC NÚT HÀNH ĐỘNG ===")]
    [Tooltip("Nút Trang bị")]
    [SerializeField] private Button btnEquip;

    [Tooltip("Nút Tháo ra (nằm đè cùng vị trí với BtnEquip)")]
    [SerializeField] private Button btnUnequip;

    [Tooltip("Nút Sử dụng (cho bình máu / vật phẩm tiêu hao)")]
    [SerializeField] private Button btnUse;

    [Tooltip("Nút Vứt bỏ vật phẩm ra sàn")]
    [SerializeField] private Button btnDrop;

    // Trạng thái hiện tại
    private InventorySlotUI currentBagSlot;
    private EquipmentSlotUI currentEquipSlot;
    private ItemData currentItem;
    private bool isEquippedTarget = false;
    private bool isInitialized = false;

    public InventorySlotUI CurrentBagSlot => currentBagSlot;
    public EquipmentSlotUI CurrentEquipSlot => currentEquipSlot;

    // Callback thông báo lại cho InventoryUIController
    public event Action OnActionCompleted;

    private void Awake()
    {
      EnsureInitialized();
    }

    private void EnsureInitialized()
    {
      if (isInitialized) return;
      isInitialized = true;

      AutoFindReferences();

      if (btnEquip != null)
      {
        btnEquip.onClick.AddListener(HandleEquip);
      }

      if (btnUnequip != null)
      {
        btnUnequip.onClick.AddListener(HandleUnequip);
      }

      if (btnUse != null)
      {
        btnUse.onClick.AddListener(HandleUse);
      }

      if (btnDrop != null)
      {
        btnDrop.onClick.AddListener(HandleDrop);
      }
    }

    private void AutoFindReferences()
    {
      if (itemNameText == null) itemNameText = transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
      if (itemRarityText == null) itemRarityText = transform.Find("ItemTypeRow/ItemRarity")?.GetComponent<TextMeshProUGUI>();
      if (itemTypeText == null) itemTypeText = transform.Find("ItemTypeRow/ItemType")?.GetComponent<TextMeshProUGUI>();
      if (itemIcon == null) itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
      if (itemStatsText == null) itemStatsText = transform.Find("ItemStatsText")?.GetComponent<TextMeshProUGUI>();
      if (itemDescriptionText == null) itemDescriptionText = transform.Find("ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();

      if (btnEquip == null) btnEquip = transform.Find("ActionButton/BtnEquip")?.GetComponent<Button>();
      if (btnUnequip == null) btnUnequip = transform.Find("ActionButton/BtnUnequip")?.GetComponent<Button>();
      if (btnUse == null) btnUse = transform.Find("ActionButton/BtnUse")?.GetComponent<Button>();
      if (btnDrop == null) btnDrop = transform.Find("ActionButton/BtnDrop")?.GetComponent<Button>();
    }

    /// <summary>
    /// Hiển thị popup cho 1 ô đồ trong túi và đặt vị trí bám theo ô đó
    /// </summary>
    public void ShowForBagSlot(InventorySlotUI slot)
    {
      EnsureInitialized();

      if (slot == null || slot.IsEmpty)
      {
        Hide();
        return;
      }

      // Bỏ chọn ô cũ nếu có
      if (currentBagSlot != null) currentBagSlot.SetSelected(false);
      if (currentEquipSlot != null) currentEquipSlot.SetSelected(false);

      currentBagSlot = slot;
      currentEquipSlot = null;
      currentItem = slot.CurrentItem;
      isEquippedTarget = false;

      currentBagSlot.SetSelected(true);
      gameObject.SetActive(true);
      UpdateUIContent();
      PositionNearSlot(slot.transform as RectTransform);
    }

    /// <summary>
    /// Hiển thị popup cho 1 ô đang trang bị trên người (Weapon/Armor) và đặt vị trí bám theo ô đó
    /// </summary>
    public void ShowForEquipmentSlot(EquipmentSlotUI eqSlot)
    {
      EnsureInitialized();

      if (eqSlot == null || eqSlot.IsEmpty)
      {
        Hide();
        return;
      }

      // Bỏ chọn ô cũ nếu có
      if (currentBagSlot != null) currentBagSlot.SetSelected(false);
      if (currentEquipSlot != null) currentEquipSlot.SetSelected(false);

      currentEquipSlot = eqSlot;
      currentBagSlot = null;
      currentItem = eqSlot.EquippedItem;
      isEquippedTarget = true;

      currentEquipSlot.SetSelected(true);
      gameObject.SetActive(true);
      UpdateUIContent();
      PositionNearSlot(eqSlot.transform as RectTransform);
    }

    /// <summary>
    /// Tự động định vị Popup theo ô đồ: Góc trên trái nằm ở tâm ô đồ, 
    /// nếu tràn cạnh dưới hoặc cạnh phải màn hình thì tự động đảo góc (pivot) sang dưới hoặc trái
    /// </summary>
    private void PositionNearSlot(RectTransform slotRect)
    {
      if (slotRect == null) return;

      RectTransform popupRect = transform as RectTransform;
      RectTransform parentRect = transform.parent as RectTransform;
      if (popupRect == null || parentRect == null) return;

      Canvas canvas = GetComponentInParent<Canvas>();
      Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

      // 1. Lấy vị trí tâm của ô đồ trên màn hình (Screen Space)
      Vector3 slotWorldCenter = slotRect.position;
      Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, slotWorldCenter);

      // 2. Chuyển sang toạ độ local của parentRect (InventoryUI)
      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, cam, out Vector2 localPoint))
      {
        return;
      }

      // 3. Kích thước popup
      float popupWidth = popupRect.rect.width > 0f ? popupRect.rect.width : 280f;
      float popupHeight = popupRect.rect.height > 0f ? popupRect.rect.height : 380f;

      // 4. Quyết định Pivot:
      // Mặc định mở sang phải (pivot.x = 0) và xuống dưới (pivot.y = 1) -> Góc trên trái nằm ở giữa ô đồ
      float pivotX = 0f;
      float pivotY = 1f;

      // Nếu không đủ chỗ bên phải màn hình -> mở sang trái (pivot.x = 1)
      if (screenPos.x + popupWidth > Screen.width - 20f)
      {
        pivotX = 1f;
      }

      // Nếu không đủ chỗ bên dưới màn hình -> mở lên trên (pivot.y = 0)
      if (screenPos.y - popupHeight < 20f)
      {
        pivotY = 0f;
      }

      popupRect.pivot = new Vector2(pivotX, pivotY);
      popupRect.anchoredPosition = localPoint;
    }

    private void UpdateUIContent()
    {
      if (currentItem == null)
      {
        Hide();
        return;
      }

      // 1. Tên vật phẩm với gradient phẩm cấp
      Color rarityColor = currentItem.Rarity.GetRarityColor();
      string hexColor = ColorUtility.ToHtmlStringRGB(rarityColor);
      if (itemNameText != null)
      {
        itemNameText.color = Color.white;
        itemNameText.text = currentItem.Rarity.FormatWithGradient(currentItem.ItemName);
      }

      // 2. Độ hiếm (ItemRarity text)
      if (itemRarityText != null)
      {
        itemRarityText.color = Color.white;
        itemRarityText.text = currentItem.Rarity.FormatWithGradient(currentItem.Rarity.GetRarityName());
      }

      // 3. Phân loại (ItemType text)
      if (itemTypeText != null)
      {
        itemTypeText.text = GetItemTypeName(currentItem.ItemType);
      }

      // 4. Icon
      if (itemIcon != null)
      {
        if (currentItem.Icon != null)
        {
          itemIcon.gameObject.SetActive(true);
          itemIcon.sprite = currentItem.Icon;
          itemIcon.color = Color.white;
        }
        else
        {
          itemIcon.gameObject.SetActive(false);
        }
      }

      // 5. Mô tả
      if (itemDescriptionText != null)
      {
        itemDescriptionText.text = currentItem.Description;
      }

      // 6. Thống kê chỉ số (Stats)
      if (itemStatsText != null)
      {
        itemStatsText.text = BuildStatsString(currentItem);
      }

      // 7. Cấu hình trạng thái 4 nút hành động
      ConfigureActionButtons();
    }

    private void ConfigureActionButtons()
    {
      if (isEquippedTarget)
      {
        // Đang xem đồ trên người (từ WeaponSlot hoặc ArmorSlot)
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(true); // Nút Tháo ra hiện lên
        if (btnUse != null) btnUse.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false); // Không thể vứt đồ đang mặc
      }
      else
      {
        // Đang xem đồ trong túi
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);

        // Nút Vứt bỏ luôn khả dụng cho vật phẩm trong túi
        if (btnDrop != null) btnDrop.gameObject.SetActive(true);

        switch (currentItem.ItemType)
        {
          case ItemType.Weapon:
          case ItemType.Armor:
            if (btnEquip != null) btnEquip.gameObject.SetActive(true);
            if (btnUse != null) btnUse.gameObject.SetActive(false);
            break;

          case ItemType.Consumable:
            if (btnEquip != null) btnEquip.gameObject.SetActive(false);
            if (btnUse != null) btnUse.gameObject.SetActive(true);
            break;

          case ItemType.Material:
          case ItemType.Currency:
          default:
            if (btnEquip != null) btnEquip.gameObject.SetActive(false);
            if (btnUse != null) btnUse.gameObject.SetActive(false);
            break;
        }
      }
    }

    private string BuildStatsString(ItemData item)
    {
      var sb = new StringBuilder();

      if (item.BonusAttack > 0f)
      {
        sb.AppendLine($"<color=#FFD54F>+ {item.BonusAttack} Attack Damage</color>");
      }

      if (item.BonusDefense > 0f)
      {
        sb.AppendLine($"<color=#81D4FA>+ {item.BonusDefense} Defense</color>");
      }

      if (item.BonusHealth > 0f)
      {
        sb.AppendLine($"<color=#EF5350>+ {item.BonusHealth} Max Health</color>");
      }

      if (item.BonusStamina > 0f)
      {
        sb.AppendLine($"<color=#66BB6A>+ {item.BonusStamina} Max Stamina</color>");
      }

      if (item.HealAmount > 0f)
      {
        sb.AppendLine($"<color=#81C784>Restores {item.HealAmount} HP Instantly</color>");
      }

      if (item.GoldValue > 0)
      {
        sb.AppendLine($"<color=#FFE082>Value: {item.GoldValue} Gold</color>");
      }

      return sb.Length > 0 ? sb.ToString().TrimEnd() : "No special attributes.";
    }

    private string GetItemTypeName(ItemType type)
    {
      switch (type)
      {
        case ItemType.Weapon: return "Weapon";
        case ItemType.Armor: return "Armor";
        case ItemType.Consumable: return "Consumable";
        case ItemType.Material: return "Material";
        case ItemType.Currency: return "Currency";
        default: return "Item";
      }
    }

    private void HandleEquip()
    {
      if (currentItem == null || currentBagSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.Equip(currentItem);
      if (success)
      {
        Hide();
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleUnequip()
    {
      if (currentItem == null || currentEquipSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.Unequip(currentEquipSlot.SlotType);
      if (success)
      {
        Hide();
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleUse()
    {
      if (currentItem == null || currentBagSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.UseItem(currentItem);
      if (success)
      {
        if (currentBagSlot.CurrentQuantity <= 1)
        {
          Hide();
        }
        else
        {
          UpdateUIContent();
        }
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleDrop()
    {
      if (currentItem == null || currentBagSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.DropItem(currentBagSlot.ActualBagIndex, 1);
      if (success)
      {
        if (currentBagSlot.CurrentQuantity <= 1)
        {
          Hide();
        }
        else
        {
          UpdateUIContent();
        }
        OnActionCompleted?.Invoke();
      }
    }

    /// <summary>
    /// Đóng popup và hủy viền chọn ô
    /// </summary>
    public void Hide()
    {
      if (currentBagSlot != null)
      {
        currentBagSlot.SetSelected(false);
        currentBagSlot = null;
      }

      if (currentEquipSlot != null)
      {
        currentEquipSlot.SetSelected(false);
        currentEquipSlot = null;
      }

      currentItem = null;
      gameObject.SetActive(false);
    }
  }
}
