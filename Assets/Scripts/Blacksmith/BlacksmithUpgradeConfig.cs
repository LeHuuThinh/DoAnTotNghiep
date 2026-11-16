using System;
using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Blacksmith
{
  /// <summary>
  /// Cấu hình công thức cường hóa trang bị (+1 đến +5) tại Lò Rèn Haven (Task RPG-08A & Nâng cấp Hệ thống Đập đồ):
  /// - Công thức chi phí linh hoạt dựa trên: Loại trang bị (ItemType) × Độ hiếm (Rarity) × Cấp cường hóa (UpgradeLevel)
  /// - Tự động liên kết nguyên liệu đặc thù theo lore và phẩm cấp (Xương, Huy hiệu, Bụi ma pháp, Mảnh cổ tự, Lõi Boss, v.v.)
  /// </summary>
  public static class BlacksmithUpgradeConfig
  {
    public const int MAX_UPGRADE_LEVEL = 5;

    // === CÔNG THỨC CƯỜNG HÓA: KẾT HỢP % VÀ CỘNG THẲNG (FLAT) ===
    // Dòng chính Vũ khí (ATK): +8% + 4 Flat ATK mỗi cấp
    public const float ATK_PERCENT_PER_LEVEL = 0.08f;
    public const float ATK_FLAT_PER_LEVEL = 4.0f;

    // Dòng chính Giáp (DEF): +8% + 2 Flat DEF mỗi cấp
    public const float DEF_PERCENT_PER_LEVEL = 0.08f;
    public const float DEF_FLAT_PER_LEVEL = 2.0f;

    // Dòng Máu (HP): +8% + 12 Flat HP mỗi cấp
    public const float HP_PERCENT_PER_LEVEL = 0.08f;
    public const float HP_FLAT_PER_LEVEL = 12.0f;

    // Dòng phụ (Crit, Hút máu, v.v.): Tăng nhẹ +3% mỗi cấp
    public const float SUBSTAT_PERCENT_PER_LEVEL = 0.03f;

    // ID nguyên liệu phổ quát
    public const string UPGRADE_STONE_ID = "mat_upgrade_stone";
    public const string UPGRADE_CRYSTAL_ID = "mat_upgrade_crystal";

    // ID nguyên liệu đặc thù theo lore
    public const string MAT_BONE_FRAGMENT = "mat_bone_fragment";
    public const string MAT_WARRIOR_INSIGNIA = "mat_warrior_insignia";
    public const string MAT_DARK_ARROWHEAD = "mat_dark_arrowhead";
    public const string MAT_ARCANE_DUST = "mat_arcane_dust";
    public const string MAT_SPELL_PARCHMENT = "mat_spell_parchment";
    public const string MAT_MONSTER_GEM = "mat_monster_gem";
    public const string MAT_BOSS_GEM = "mat_boss_gem";
    public const string MAT_ICE_RUNE = "mat_boss_ice_rune";
    public const string MAT_ICE_CORE = "mat_boss_ice_core";
    public const string MAT_FIRE_CREST = "mat_boss_fire_crest";
    public const string MAT_FIRE_CORE = "mat_boss_fire_core";
    public const string MAT_VOID_ESSENCE = "mat_boss_void_essence";
    public const string MAT_VOID_EYE = "mat_boss_void_eye";

    public struct UpgradeRequirement
    {
      public int TargetLevel;
      public int GoldCost;
      public int StoneCount;
      public int CrystalCount;
      public string SpecialMaterialId;
      public int SpecialMaterialCount;

      public UpgradeRequirement(int targetLevel, int gold, int stones, int crystals = 0, string specialMatId = null, int specialMatCount = 0)
      {
        TargetLevel = targetLevel;
        GoldCost = gold;
        StoneCount = stones;
        CrystalCount = crystals;
        SpecialMaterialId = specialMatId;
        SpecialMaterialCount = specialMatCount;
      }
    }

    // Chi phí cơ sở theo Level (+1 đến +5)
    // +1: 1 Đá, 0 Tinh Thể, 50 Vàng
    // +2: 2 Đá, 0 Tinh Thể, 100 Vàng
    // +3: 3 Đá, 0 Tinh Thể, 200 Vàng
    // +4: 4 Đá, 1 Tinh Thể, 400 Vàng
    // +5: 5 Đá, 2 Tinh Thể, 700 Vàng
    private static readonly int[] BaseStones = new int[] { 1, 2, 3, 4, 5 };
    private static readonly int[] BaseCrystals = new int[] { 0, 0, 0, 1, 2 };
    private static readonly int[] BaseGold = new int[] { 50, 100, 200, 400, 700 };

    /// <summary>
    /// Xác định nguyên liệu đặc thù theo lore và phẩm cấp trang bị
    /// </summary>
    public static (string matId, int count) ResolveSpecialMaterial(ItemData item, int targetLevel)
    {
      if (item == null) return (null, 0);

      string itemId = item.Id != null ? item.Id.ToLower() : "";
      string itemName = item.ItemName != null ? item.ItemName.ToLower() : "";
      ItemRarity rarity = item.Rarity;
      ItemType type = item.ItemType;

      switch (rarity)
      {
        case ItemRarity.Common:
          return (null, 0);

        case ItemRarity.Uncommon:
        {
          int count = targetLevel >= 4 ? 2 : 1;
          if (type == ItemType.Accessory) return (MAT_WARRIOR_INSIGNIA, 1);
          return (MAT_BONE_FRAGMENT, count);
        }

        case ItemRarity.Rare:
        {
          if (type == ItemType.Accessory)
          {
            return (MAT_MONSTER_GEM, 1);
          }
          int count = targetLevel >= 4 ? 2 : 1;
          if (type == ItemType.Armor) return (MAT_WARRIOR_INSIGNIA, count);
          return (MAT_ARCANE_DUST, count);
        }

        case ItemRarity.Epic:
        {
          int count = targetLevel >= 4 ? 2 : 1;
          if (type == ItemType.Accessory)
          {
            return (MAT_MONSTER_GEM, count);
          }

          // Nhận diện theo hệ thuộc tính của trang bị
          if (itemId.Contains("ice") || itemName.Contains("ice") || itemName.Contains("frost") || itemName.Contains("băng"))
            return (MAT_ICE_RUNE, count);
          if (itemId.Contains("fire") || itemName.Contains("fire") || itemName.Contains("flame") || itemName.Contains("hỏa"))
            return (MAT_FIRE_CREST, count);
          if (itemId.Contains("void") || itemName.Contains("hư không"))
            return (MAT_VOID_ESSENCE, count);

          return (type == ItemType.Weapon ? MAT_SPELL_PARCHMENT : MAT_ICE_RUNE, count);
        }

        case ItemRarity.Legendary:
        {
          // Cấp 1-2: Nguyên liệu tinh hoa
          if (targetLevel <= 2)
          {
            if (itemId.Contains("fire") || itemName.Contains("fire") || itemName.Contains("dragon") || itemName.Contains("hỏa"))
              return (MAT_FIRE_CREST, 1);
            if (itemId.Contains("ice") || itemName.Contains("frost") || itemName.Contains("băng"))
              return (MAT_ICE_RUNE, 1);
            if (itemId.Contains("void") || itemName.Contains("hư không"))
              return (MAT_VOID_ESSENCE, 1);
            return (MAT_SPELL_PARCHMENT, 1);
          }

          // Cấp 3-5: Lõi Boss tối thượng hoặc Boss Gem
          if (itemId.Contains("fire") || itemName.Contains("fire") || itemName.Contains("dragon") || itemName.Contains("hỏa"))
            return (MAT_FIRE_CORE, 1);
          if (itemId.Contains("ice") || itemName.Contains("frost") || itemName.Contains("băng"))
            return (MAT_ICE_CORE, 1);
          if (itemId.Contains("void") || itemName.Contains("hư không"))
            return (MAT_VOID_EYE, 1);

          return (MAT_BOSS_GEM, 1);
        }

        default:
          return (null, 0);
      }
    }

    /// <summary>
    /// Lấy yêu cầu nguyên liệu và vàng để nâng cấp từ currentLevel lên currentLevel + 1
    /// (Tự động biến đổi theo ItemType, Rarity và TargetLevel)
    /// </summary>
    public static bool GetRequirement(ItemData item, int currentLevel, out UpgradeRequirement req)
    {
      if (currentLevel < 0 || currentLevel >= MAX_UPGRADE_LEVEL)
      {
        req = default;
        return false;
      }

      int targetLevel = currentLevel + 1; // 1 đến 5
      int baseStone = BaseStones[currentLevel];
      int baseCrystal = BaseCrystals[currentLevel];
      int baseGold = BaseGold[currentLevel];

      if (item == null)
      {
        req = new UpgradeRequirement(targetLevel, baseGold, baseStone, baseCrystal);
        return true;
      }

      // 1. Hệ số theo Loại trang bị (ItemType)
      float stoneTypeMod = 1f;
      float goldTypeMod = 1f;
      switch (item.ItemType)
      {
        case ItemType.Armor:
          stoneTypeMod = 0.8f;
          goldTypeMod = 0.9f;
          break;
        case ItemType.Accessory:
          stoneTypeMod = 0.6f;
          goldTypeMod = 0.7f;
          break;
        case ItemType.Weapon:
        default:
          stoneTypeMod = 1.0f;
          goldTypeMod = 1.0f;
          break;
      }

      // 2. Hệ số theo Độ hiếm (Rarity)
      float stoneRarityMult = 1.0f;
      float crystalRarityMult = 1.0f;
      float goldRarityMult = 1.0f;

      switch (item.Rarity)
      {
        case ItemRarity.Common:
          stoneRarityMult = 1.0f;
          crystalRarityMult = 0f;
          goldRarityMult = 1.0f;
          break;
        case ItemRarity.Uncommon:
          stoneRarityMult = 1.0f;
          crystalRarityMult = 0f;
          goldRarityMult = 1.2f;
          break;
        case ItemRarity.Rare:
          stoneRarityMult = 1.5f;
          crystalRarityMult = (targetLevel >= 4) ? 1.0f : 0f;
          goldRarityMult = 1.5f;
          break;
        case ItemRarity.Epic:
          stoneRarityMult = 2.0f;
          crystalRarityMult = 1.0f;
          goldRarityMult = 2.0f;
          break;
        case ItemRarity.Legendary:
          stoneRarityMult = 2.5f;
          crystalRarityMult = (targetLevel >= 3) ? 1.5f : 0f;
          goldRarityMult = 2.8f;
          break;
      }

      // 3. Tính toán số lượng cuối
      int finalStones = Mathf.Max(1, Mathf.RoundToInt(baseStone * stoneRarityMult * stoneTypeMod));
      int finalCrystals = 0;
      if (baseCrystal > 0)
      {
        finalCrystals = Mathf.Max(1, Mathf.RoundToInt(baseCrystal * crystalRarityMult));
      }
      else if (item.Rarity == ItemRarity.Legendary && targetLevel >= 3)
      {
        // Cấp +3 của Legendary cần thêm 1 tinh thể
        finalCrystals = 1;
      }

      int finalGold = Mathf.RoundToInt(baseGold * goldRarityMult * goldTypeMod);

      // 4. Nguyên liệu đặc thù theo lore
      var (specialId, specialCount) = ResolveSpecialMaterial(item, targetLevel);

      req = new UpgradeRequirement(targetLevel, finalGold, finalStones, finalCrystals, specialId, specialCount);
      return true;
    }

    /// <summary>
    /// Overload tương thích ngược (khi không truyền item)
    /// </summary>
    public static bool GetRequirement(int currentLevel, out UpgradeRequirement req)
    {
      return GetRequirement(null, currentLevel, out req);
    }

    /// <summary>
    /// Tính toán sát thương sau khi cường hóa: BaseAttack * (1 + 0.08 * level) + (4 * level)
    /// </summary>
    public static float CalculateUpgradedAttack(float baseAttack, int level)
    {
      if (baseAttack <= 0f) return 0f;
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseAttack * (1f + ATK_PERCENT_PER_LEVEL * level) + (ATK_FLAT_PER_LEVEL * level);
    }

    /// <summary>
    /// Tính toán phòng thủ sau khi cường hóa: BaseDefense * (1 + 0.08 * level) + (2 * level)
    /// </summary>
    public static float CalculateUpgradedDefense(float baseDefense, int level)
    {
      if (baseDefense <= 0f) return 0f;
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseDefense * (1f + DEF_PERCENT_PER_LEVEL * level) + (DEF_FLAT_PER_LEVEL * level);
    }

    /// <summary>
    /// Tính toán máu tối đa sau khi cường hóa: BaseHealth * (1 + 0.08 * level) + (12 * level)
    /// </summary>
    public static float CalculateUpgradedHealth(float baseHealth, int level)
    {
      if (baseHealth <= 0f) return 0f;
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseHealth * (1f + HP_PERCENT_PER_LEVEL * level) + (HP_FLAT_PER_LEVEL * level);
    }

    /// <summary>
    /// Tính toán chỉ số dòng phụ sau khi cường hóa (tăng nhẹ +3% mỗi cấp)
    /// </summary>
    public static float CalculateUpgradedSubStat(float baseValue, int level)
    {
      if (baseValue <= 0f) return 0f;
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseValue * (1f + SUBSTAT_PERCENT_PER_LEVEL * level);
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ điều kiện (nguyên liệu + vàng) để nâng cấp món trang bị cụ thể không
    /// </summary>
    public static bool CanAffordUpgrade(ItemData item, int currentLevel, InventoryManager inv, out bool missingGold, out bool missingStone, out bool missingCrystal, out bool missingSpecialMat)
    {
      missingGold = false;
      missingStone = false;
      missingCrystal = false;
      missingSpecialMat = false;

      if (!GetRequirement(item, currentLevel, out var req)) return false;
      if (inv == null) return false;

      if (inv.CurrentGold < req.GoldCost) missingGold = true;
      if (inv.GetItemCount(UPGRADE_STONE_ID) < req.StoneCount) missingStone = true;
      if (req.CrystalCount > 0 && inv.GetItemCount(UPGRADE_CRYSTAL_ID) < req.CrystalCount) missingCrystal = true;
      if (!string.IsNullOrEmpty(req.SpecialMaterialId) && req.SpecialMaterialCount > 0)
      {
        if (inv.GetItemCount(req.SpecialMaterialId) < req.SpecialMaterialCount) missingSpecialMat = true;
      }

      return !missingGold && !missingStone && !missingCrystal && !missingSpecialMat;
    }

    /// <summary>
    /// Overload tương thích ngược
    /// </summary>
    public static bool CanAffordUpgrade(int currentLevel, InventoryManager inv, out bool missingGold, out bool missingStone, out bool missingCrystal)
    {
      return CanAffordUpgrade(null, currentLevel, inv, out missingGold, out missingStone, out missingCrystal, out _);
    }
  }
}
