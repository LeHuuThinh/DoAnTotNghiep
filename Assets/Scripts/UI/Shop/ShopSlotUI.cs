using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Shop;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý hiển thị và tương tác của 1 dòng mặt hàng (ShopItemRow) trong Cửa Hàng
  /// </summary>
  public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
  {
    [Header("=== KHUNG TRANG TRÍ MẶT HÀNG (ROOT DECORATIONS) ===")]
    [Tooltip("Viền khung của toàn bộ dòng món đồ")]
    [SerializeField] private GameObject itemBorder;
    [Tooltip("Góc viền hoa văn của toàn bộ dòng món đồ")]
    [SerializeField] private GameObject itemCorner;

    [Header("=== KHUNG TRANG TRÍ ICON (ICON DECORATIONS) ===")]
    [Tooltip("Viền khung của ô Icon món đồ")]
    [SerializeField] private GameObject iconBorder;
    [Tooltip("Góc viền hoa văn của ô Icon món đồ")]
    [SerializeField] private GameObject iconCorner;

    [Header("=== CÁC THÀNH PHẦN HIỂN THỊ ===")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityBgImage;
    [SerializeField] private Image rarityBarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Image goldIcon;

    [Header("=== MÀU SẮC GIÁ VÀNG ===")]
    [SerializeField] private Color normalPriceColor = new Color(1f, 0.84f, 0f, 1f); // Vàng kim #FFD700

    private ShopItemEntry entry;
    private bool isSelected = false;
    private bool isHovered = false;
    private int slotIndex = -1;

    public ShopItemEntry Entry => entry;
    public bool IsSelected => isSelected;
    public int SlotIndex => slotIndex;

    public event Action<ShopSlotUI> OnClicked;

    private void Awake()
    {
      AutoFindReferences();
      UpdateVisualStates();
    }

    public void AutoFindReferences()
    {
      if (itemBorder == null) itemBorder = transform.Find("Border")?.gameObject;
      if (itemCorner == null) itemCorner = transform.Find("Corner")?.gameObject;

      Transform iconTr = transform.Find("ItemIcon");
      if (iconTr != null)
      {
        if (iconBorder == null) iconBorder = iconTr.Find("Border")?.gameObject;
        if (iconCorner == null) iconCorner = iconTr.Find("Corner")?.gameObject;
        if (iconImage == null)
        {
          var iconChild = iconTr.Find("Icon") ?? iconTr.Find("ItemIcon");
          iconImage = iconChild?.GetComponent<Image>();
        }
        if (rarityBgImage == null) rarityBgImage = iconTr.Find("RarityBG")?.GetComponent<Image>();
        if (rarityBarImage == null) rarityBarImage = iconTr.Find("RarityBar")?.GetComponent<Image>();
      }

      Transform infoTr = transform.Find("itemInfo");
      if (infoTr != null)
      {
        if (nameText == null) nameText = infoTr.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (descText == null) descText = infoTr.Find("SmallDescription")?.GetComponent<TextMeshProUGUI>();
      }

      Transform priceTr = transform.Find("Price");
      if (priceTr != null)
      {
        if (priceText == null) priceText = priceTr.Find("GoldValue")?.GetComponent<TextMeshProUGUI>();
        if (goldIcon == null)
        {
          goldIcon = priceTr.Find("GoldIcon")?.GetComponent<Image>() 
                  ?? priceTr.Find("GoidIcon")?.GetComponent<Image>();
        }
      }

      if (stockText == null) stockText = transform.Find("StockText")?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Gán dữ liệu mặt hàng vào ô
    /// </summary>
    public void Setup(ShopItemEntry itemEntry, int index, int currentGold)
    {
      AutoFindReferences();
      entry = itemEntry;
      slotIndex = index;
      isSelected = false;
      isHovered = false;

      if (entry == null || entry.ItemData == null)
      {
        gameObject.SetActive(false);
        return;
      }

      gameObject.SetActive(true);
      ItemData item = entry.ItemData;

      // 1. Kích hoạt GameObject Icon nếu bị ẩn và nạp ảnh
      Transform iconParent = transform.Find("ItemIcon");
      if (iconParent != null && !iconParent.gameObject.activeSelf)
      {
        iconParent.gameObject.SetActive(true);
      }

      if (iconImage == null && iconParent != null)
      {
        var iconChild = iconParent.Find("Icon") ?? iconParent.Find("ItemIcon");
        iconImage = iconChild?.GetComponent<Image>();
      }

      if (iconImage != null)
      {
        iconImage.gameObject.SetActive(item.Icon != null);
        iconImage.sprite = item.Icon;
        iconImage.enabled = item.Icon != null;
        iconImage.color = Color.white;
      }

      Color rColor = item.Rarity.GetRarityColor();
      if (rarityBgImage != null)
      {
        rarityBgImage.gameObject.SetActive(true);
        rarityBgImage.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (rarityBarImage != null)
      {
        rarityBarImage.gameObject.SetActive(true);
        rarityBarImage.color = rColor;
      }

      // 2. Tên & Mô tả ngắn (English)
      if (nameText != null)
      {
        nameText.text = item.Rarity.FormatWithGradient(item.ItemName);
      }

      if (descText != null)
      {
        descText.text = GetShortTypeOrDesc(item);
      }

      // 3. Giá tiền: Luôn giữ màu mặc định trong danh sách mặt hàng
      if (priceText != null)
      {
        priceText.text = entry.BuyPrice.ToString("N0");
        priceText.color = normalPriceColor;
      }

      // 4. Số lượng tồn kho: "Stock: <số>"
      if (stockText != null)
      {
        if (entry.IsInfiniteStock)
        {
          stockText.text = "Stock: MAX";
        }
        else
        {
          stockText.text = $"Stock: {entry.StockQuantity}";
        }
      }

      // 5. Cập nhật viền trang trí
      UpdateVisualStates();
    }

    /// <summary>
    /// Giữ màu giá mặc định cho ô trong danh sách
    /// </summary>
    public void UpdatePriceColor(int currentGold)
    {
      if (entry == null || priceText == null) return;
      priceText.text = entry.BuyPrice.ToString("N0");
      priceText.color = normalPriceColor; // Luôn màu mặc định
    }

    /// <summary>
    /// Đặt trạng thái được chọn (Selected) của món hàng
    /// </summary>
    public void SetSelected(bool selected)
    {
      isSelected = selected;
      UpdateVisualStates();
    }

    /// <summary>
    /// Cập nhật bật/tắt các khung trang trí theo đúng yêu cầu:
    /// - Mặc định (Unselected & Unhovered): Cả Border và Corner của cả 2 đều tắt.
    /// - Khi Hover (rê chuột qua): Border của cả Item và Icon bật lên; Corner tắt.
    /// - Khi Selected (được chọn): Cả Border và Corner của cả Item và Icon đều bật lên.
    /// </summary>
    private void UpdateVisualStates()
    {
      if (isSelected)
      {
        // Khi được chọn: Bật cả Border và Corner của cả Item lẫn Icon
        if (itemBorder != null) itemBorder.SetActive(true);
        if (itemCorner != null) itemCorner.SetActive(true);
        if (iconBorder != null) iconBorder.SetActive(true);
        if (iconCorner != null) iconCorner.SetActive(true);
      }
      else if (isHovered)
      {
        // Khi hover chuột qua: Chỉ bật Border của cả Item lẫn Icon, Corner vẫn tắt
        if (itemBorder != null) itemBorder.SetActive(true);
        if (itemCorner != null) itemCorner.SetActive(false);
        if (iconBorder != null) iconBorder.SetActive(true);
        if (iconCorner != null) iconCorner.SetActive(false);
      }
      else
      {
        // Mặc định: Tắt toàn bộ
        if (itemBorder != null) itemBorder.SetActive(false);
        if (itemCorner != null) itemCorner.SetActive(false);
        if (iconBorder != null) iconBorder.SetActive(false);
        if (iconCorner != null) iconCorner.SetActive(false);
      }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      isHovered = true;
      UpdateVisualStates();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      isHovered = false;
      UpdateVisualStates();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      OnClicked?.Invoke(this);
    }

    private string GetShortTypeOrDesc(ItemData item)
    {
      if (item == null) return "";
      switch (item.ItemType)
      {
        case ItemType.Weapon:
          return item.BonusAttack > 0 ? $"+{item.BonusAttack} Attack (ATK)" : "Melee Weapon";
        case ItemType.Armor:
          return item.BonusDefense > 0 ? $"+{item.BonusDefense} Defense (DEF)" : "Armor Gear";
        case ItemType.Consumable:
          return item.HealAmount > 0 ? $"+{item.HealAmount} HP Recovery" : "Survival Elixir";
        case ItemType.Material:
          return "Crafting Material";
        default:
          return item.Description.Length > 28 ? item.Description.Substring(0, 28) + "..." : item.Description;
      }
    }
  }
}
