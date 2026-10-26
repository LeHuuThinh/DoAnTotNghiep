using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Phân loại vật phẩm
  /// </summary>
  public enum ItemType
  {
    Weapon,     // Vũ khí (Kiếm, rìu...)
    Armor,      // Giáp (Áo giáp, khiên...)
    Consumable, // Vật phẩm tiêu hao (Bình máu, bình thể lực...)
    Material,   // Nguyên liệu (Đá nâng cấp kiếm, xương quái, quặng...)
    Currency    // Tiền tệ (Đồng tiền vàng)
  }

  /// <summary>
  /// Phẩm cấp / Độ hiếm của vật phẩm
  /// </summary>
  public enum ItemRarity
  {
    Common,     // Thường (Trắng xám)
    Uncommon,   // Đặc Biệt / Tinh Xảo (Xanh lục)
    Rare,       // Hiếm (Xanh lam)
    Epic,       // Sử thi (Tím)
    Legendary   // Huyền thoại (Vàng cam rực rỡ)
  }

  /// <summary>
  /// Helper mở rộng cho Độ hiếm (Màu sắc, Tên hiển thị, Gradient)
  /// </summary>
  public static class ItemRarityExtensions
  {
    public static Color GetRarityColor(this ItemRarity rarity)
    {
      switch (rarity)
      {
        case ItemRarity.Common:
          return new Color(0.85f, 0.85f, 0.85f, 1f); // Trắng bạc
        case ItemRarity.Uncommon:
          return new Color(0.25f, 0.85f, 0.35f, 1f); // Xanh lục tươi (#40D959)
        case ItemRarity.Rare:
          return new Color(0.2f, 0.6f, 1.0f, 1f);    // Xanh lam sáng (#3399FF)
        case ItemRarity.Epic:
          return new Color(0.68f, 0.35f, 0.95f, 1f); // Tím ma pháp (#AE59F2)
        case ItemRarity.Legendary:
          return new Color(1.0f, 0.72f, 0.15f, 1f);  // Vàng cam hoàng gia (#FFB826)
        default:
          return Color.white;
      }
    }

    public static string GetRarityName(this ItemRarity rarity)
    {
      switch (rarity)
      {
        case ItemRarity.Common: return "Thường";
        case ItemRarity.Uncommon: return "Đặc Biệt";
        case ItemRarity.Rare: return "Hiếm";
        case ItemRarity.Epic: return "Sử Thi";
        case ItemRarity.Legendary: return "Huyền Thoại";
        default: return "Vô Danh";
      }
    }

    public static string GetHexColor(this ItemRarity rarity)
    {
      Color c = rarity.GetRarityColor();
      return ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>
    /// Bọc chuỗi trong tag Rich Text Gradient của TextMeshPro (<gradient="TxtGra_Rarity">text</gradient>)
    /// </summary>
    public static string FormatWithGradient(this ItemRarity rarity, string text)
    {
      return $"<gradient=\"TxtGra_{rarity}\">{text}</gradient>";
    }
  }
}
