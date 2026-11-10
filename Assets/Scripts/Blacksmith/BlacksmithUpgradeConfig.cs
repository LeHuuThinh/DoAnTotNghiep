using System;
using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Blacksmith
{
  /// <summary>
  /// Cấu hình công thức cường hóa trang bị (+1 đến +5) tại Lò Rèn Haven (Task RPG-08A)
  /// </summary>
  public static class BlacksmithUpgradeConfig
  {
    public const int MAX_UPGRADE_LEVEL = 5;
    public const float STAT_BONUS_PER_LEVEL = 0.15f; // +15% mỗi cấp

    public const string UPGRADE_STONE_ID = "mat_upgrade_stone";
    public const string UPGRADE_CRYSTAL_ID = "mat_upgrade_crystal";

    public struct UpgradeRequirement
    {
      public int TargetLevel;
      public int GoldCost;
      public int StoneCount;
      public int CrystalCount;

      public UpgradeRequirement(int targetLevel, int gold, int stones, int crystals = 0)
      {
        TargetLevel = targetLevel;
        GoldCost = gold;
        StoneCount = stones;
        CrystalCount = crystals;
      }
    }

    private static readonly UpgradeRequirement[] Requirements = new UpgradeRequirement[]
    {
      new UpgradeRequirement(1, 50,  1, 0), // +1: 1 Đá + 50 Vàng
      new UpgradeRequirement(2, 100, 2, 0), // +2: 2 Đá + 100 Vàng
      new UpgradeRequirement(3, 200, 3, 0), // +3: 3 Đá + 200 Vàng
      new UpgradeRequirement(4, 400, 4, 1), // +4: 1 Tinh Thể + 4 Đá + 400 Vàng
      new UpgradeRequirement(5, 700, 6, 2)  // +5: 2 Tinh Thể + 6 Đá + 700 Vàng
    };

    /// <summary>
    /// Lấy yêu cầu nguyên liệu và vàng để nâng cấp từ currentLevel lên currentLevel + 1
    /// </summary>
    public static bool GetRequirement(int currentLevel, out UpgradeRequirement req)
    {
      if (currentLevel >= 0 && currentLevel < MAX_UPGRADE_LEVEL)
      {
        req = Requirements[currentLevel];
        return true;
      }
      req = default;
      return false;
    }

    /// <summary>
    /// Tính toán sát thương sau khi cường hóa: BaseAttack * (1 + 0.15 * level)
    /// </summary>
    public static float CalculateUpgradedAttack(float baseAttack, int level)
    {
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseAttack * (1f + STAT_BONUS_PER_LEVEL * level);
    }

    /// <summary>
    /// Tính toán phòng thủ sau khi cường hóa: BaseDefense * (1 + 0.15 * level)
    /// </summary>
    public static float CalculateUpgradedDefense(float baseDefense, int level)
    {
      level = Mathf.Clamp(level, 0, MAX_UPGRADE_LEVEL);
      return baseDefense * (1f + STAT_BONUS_PER_LEVEL * level);
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ điều kiện (nguyên liệu + vàng) để nâng cấp không
    /// </summary>
    public static bool CanAffordUpgrade(int currentLevel, InventoryManager inv, out bool missingGold, out bool missingStone, out bool missingCrystal)
    {
      missingGold = false;
      missingStone = false;
      missingCrystal = false;

      if (!GetRequirement(currentLevel, out var req)) return false;
      if (inv == null) return false;

      if (inv.CurrentGold < req.GoldCost) missingGold = true;
      if (inv.GetItemCount(UPGRADE_STONE_ID) < req.StoneCount) missingStone = true;
      if (req.CrystalCount > 0 && inv.GetItemCount(UPGRADE_CRYSTAL_ID) < req.CrystalCount) missingCrystal = true;

      return !missingGold && !missingStone && !missingCrystal;
    }
  }
}
