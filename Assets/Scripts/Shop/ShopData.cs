using System;
using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Shop
{
  /// <summary>
  /// Định nghĩa 1 mặt hàng bày bán trong Cửa Hàng
  /// </summary>
  [Serializable]
  public class ShopItemEntry
  {
    [Tooltip("Dữ liệu vật phẩm (ItemData ScriptableObject)")]
    [SerializeField] private ItemData itemData;

    [Tooltip("Giá mua bằng Tiền Vàng")]
    [SerializeField] private int buyPrice = 50;

    [Tooltip("Số lượng tồn kho (-1 là vô hạn / không giới hạn)")]
    [SerializeField] private int stockQuantity = -1;

    public ItemData ItemData => itemData;
    public int BuyPrice => buyPrice > 0 ? buyPrice : (itemData != null ? itemData.GoldValue : 10);
    public int StockQuantity { get => stockQuantity; set => stockQuantity = value; }
    public bool IsInfiniteStock => stockQuantity < 0;

    public ShopItemEntry() { }

    public ShopItemEntry(ItemData item, int price, int stock = -1)
    {
      itemData = item;
      buyPrice = price;
      stockQuantity = stock;
    }
  }

  /// <summary>
  /// ScriptableObject lưu trữ cấu hình và danh mục mặt hàng của 1 Cửa Hàng
  /// </summary>
  [CreateAssetMenu(fileName = "New_ShopData", menuName = "Infinite Labyrinth/RPG/Shop Data")]
  public class ShopData : ScriptableObject
  {
    [Header("=== THÔNG TIN CỬA HÀNG ===")]
    [Tooltip("Mã định danh cửa hàng (VD: equipment_shop, supplies_shop)")]
    [SerializeField] private string shopId = "equipment_shop";

    [Tooltip("Tiêu đề hiển thị trên Header của Shop (VD: EQUIPMENT & ARMORY)")]
    [SerializeField] private string shopTitle = "EQUIPMENT & ARMORY";

    [TextArea(2, 4)]
    [Tooltip("Mô tả ngắn của cửa hàng hiển thị dưới tiêu đề")]
    [SerializeField] private string shopDescription = "Basic weapons and sturdy armor for adventurers.";

    [Header("=== DANH MỤC MẶT HÀNG BÀY BÁN ===")]
    [SerializeField] private List<ShopItemEntry> items = new List<ShopItemEntry>();

    public string ShopId => shopId;
    public string ShopTitle => shopTitle;
    public string ShopDescription => shopDescription;
    public IReadOnlyList<ShopItemEntry> Items => items;

    public void SetItems(List<ShopItemEntry> newItems)
    {
      items = newItems;
    }
  }
}
