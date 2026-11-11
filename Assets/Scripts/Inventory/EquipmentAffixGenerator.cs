using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Thuật toán sinh chỉ số ngẫu nhiên (Procedural Roll) cho Trang bị khi Rơi ra đất, Mua shop, hoặc Chế tạo.
  /// Phân bổ số lượng dòng phụ và các mốc phù phép theo Phẩm cấp (Rarity).
  /// </summary>
  public static class EquipmentAffixGenerator
  {
    private static readonly SubStatType[] WeaponSubStatPool = new[]
    {
      SubStatType.CritRate,
      SubStatType.CritDamage,
      SubStatType.Lifesteal,
      SubStatType.SpiritGainBonus,
      SubStatType.StaminaCostReduction,
      SubStatType.SpiritCostReduction,
      SubStatType.AttackBonus
    };

    private static readonly SubStatType[] ArmorSubStatPool = new[]
    {
      SubStatType.DefenseBonus,
      SubStatType.HealthBonus,
      SubStatType.MaxStamina,
      SubStatType.MoveSpeedBonus,
      SubStatType.StaminaCostReduction,
      SubStatType.SpiritCostReduction,
      SubStatType.Lifesteal
    };

    private struct EnchantmentMeta
    {
      public EnchantmentType type;
      public string title;
      public string description;

      public EnchantmentMeta(EnchantmentType type, string title, string description)
      {
        this.type = type;
        this.title = title;
        this.description = description;
      }
    }

    private static readonly EnchantmentMeta[] AllEnchantments = new[]
    {
      new EnchantmentMeta(EnchantmentType.BleedingCrit, "Xuất Huyết Bạo Kích", "Đòn đánh Bạo Kích khiến kẻ địch chịu thêm 30% sát thương Chảy Máu trong 3 giây."),
      new EnchantmentMeta(EnchantmentType.SkillECostReduction, "Tật Phong Trảm", "Giảm thêm 30% Spirit tiêu hao khi thi triển Kiếm Khí (Skill E)."),
      new EnchantmentMeta(EnchantmentType.SkillQCostReduction, "Cuồng Lôi Bão", "Giảm thêm 30% Spirit tiêu hao khi thi triển Lốc Xoáy (Skill Q)."),
      new EnchantmentMeta(EnchantmentType.SkillRCostReduction, "Diệt Thế Thần Uy", "Giảm thêm 25% Spirit tiêu hao khi thi triển Trảm Kích Ultimate (Skill R)."),
      new EnchantmentMeta(EnchantmentType.LowHealthBloodthirst, "Khát Máu Cuồng Nộ", "Tăng thêm +8% Hút Máu khi lượng HP người chơi dưới 50%."),
      new EnchantmentMeta(EnchantmentType.TitanCharge, "Kình Lực Titan", "Đòn gồng (Charge Attack) gây thêm +25% Sát thương."),
      new EnchantmentMeta(EnchantmentType.ParryStaminaRestore, "Bất Động Như Sơn", "Phản đòn Parry thành công hồi phục ngay 25 Thể Lực (Stamina)."),
      new EnchantmentMeta(EnchantmentType.SprintWind, "Phong Hành Bộ", "Sau khi Lướt hoặc Lăn né, tăng 20% Tốc độ di chuyển trong 2.5 giây."),
      new EnchantmentMeta(EnchantmentType.CritFrenzy, "Cuồng Trảm", "Đòn đánh Bạo Kích tăng 15% Tốc độ di chuyển trong 3.5 giây."),
      new EnchantmentMeta(EnchantmentType.SoulSiphon, "Thu Hồn Quyết", "Tiêu diệt kẻ địch hồi phục ngay 15 Spirit và 15 HP.")
    };

    /// <summary>
    /// Tạo ra một thực thể AffixData với chỉ số chính được roll trong khoảng, các dòng phụ và ô phù phép theo Rarity
    /// </summary>
    public static EquipmentAffixData Generate(ItemData item, int initialUpgradeLevel = 0)
    {
      if (item == null) return null;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor) return null;

      var affix = new EquipmentAffixData();
      ItemRarity rarity = item.Rarity;

      // 1. ROLL DÒNG CHÍNH (MAIN STAT)
      float varianceMin = 0.92f;
      float varianceMax = 1.08f;
      switch (rarity)
      {
        case ItemRarity.Common:
          varianceMin = 0.92f; varianceMax = 1.08f;
          break;
        case ItemRarity.Uncommon:
          varianceMin = 0.94f; varianceMax = 1.12f;
          break;
        case ItemRarity.Rare:
          varianceMin = 0.96f; varianceMax = 1.16f;
          break;
        case ItemRarity.Epic:
          varianceMin = 1.00f; varianceMax = 1.20f;
          break;
        case ItemRarity.Legendary:
          varianceMin = 1.05f; varianceMax = 1.25f;
          break;
      }

      float mainVariance = UnityEngine.Random.Range(varianceMin, varianceMax);

      if (item.ItemType == ItemType.Weapon)
      {
        float baseAtk = item.BonusAttack > 0f ? item.BonusAttack : 10f;
        affix.rolledAttack = Mathf.Max(1f, Mathf.Round(baseAtk * mainVariance * 10f) / 10f);
      }
      else if (item.ItemType == ItemType.Armor)
      {
        if (item.BonusDefense > 0f)
        {
          affix.rolledDefense = Mathf.Max(1f, Mathf.Round(item.BonusDefense * mainVariance * 10f) / 10f);
        }
        if (item.BonusHealth > 0f)
        {
          affix.rolledHealth = Mathf.Max(5f, Mathf.Round(item.BonusHealth * mainVariance * 10f) / 10f);
        }
      }

      // 2. ROLL DÒNG PHỤ (SUB-STATS)
      int subStatCount = 0;
      switch (rarity)
      {
        case ItemRarity.Common:
          subStatCount = UnityEngine.Random.value < 0.5f ? 1 : 0;
          break;
        case ItemRarity.Uncommon:
          subStatCount = UnityEngine.Random.value < 0.5f ? 2 : 1;
          break;
        case ItemRarity.Rare:
          subStatCount = 2;
          break;
        case ItemRarity.Epic:
          subStatCount = 3;
          break;
        case ItemRarity.Legendary:
          subStatCount = 4;
          break;
      }

      SubStatType[] pool = (item.ItemType == ItemType.Weapon) ? WeaponSubStatPool : ArmorSubStatPool;
      var availableTypes = new List<SubStatType>(pool);

      for (int i = 0; i < subStatCount && availableTypes.Count > 0; i++)
      {
        int pickIndex = UnityEngine.Random.Range(0, availableTypes.Count);
        SubStatType pickedType = availableTypes[pickIndex];
        availableTypes.RemoveAt(pickIndex);

        float val = RollSubStatValue(pickedType, rarity);
        affix.subStats.Add(new SubStatEntry(pickedType, val));
      }

      // 3. THIẾT LẬP CÁC Ô PHÙ PHÉP (ENCHANTMENTS) THEO MỐC +1, +3, +5
      var milestoneLevels = new List<int>();
      if (rarity == ItemRarity.Common || rarity == ItemRarity.Uncommon)
      {
        milestoneLevels.Add(1);
      }
      else if (rarity == ItemRarity.Rare)
      {
        milestoneLevels.Add(1);
        milestoneLevels.Add(3);
      }
      else // Epic & Legendary
      {
        milestoneLevels.Add(1);
        milestoneLevels.Add(3);
        milestoneLevels.Add(5);
      }

      var availableEnchants = new List<EnchantmentMeta>(AllEnchantments);
      for (int i = 0; i < milestoneLevels.Count && availableEnchants.Count > 0; i++)
      {
        int pick = UnityEngine.Random.Range(0, availableEnchants.Count);
        EnchantmentMeta meta = availableEnchants[pick];
        availableEnchants.RemoveAt(pick);

        int milestone = milestoneLevels[i];
        bool unlocked = initialUpgradeLevel >= milestone;
        affix.enchantments.Add(new EnchantmentEntry(milestone, meta.type, meta.title, meta.description, 0f, unlocked));
      }

      return affix;
    }

    private static float RollSubStatValue(SubStatType type, ItemRarity rarity)
    {
      float qualityRatio = (int)rarity / 4f; // 0.0 (Common) đến 1.0 (Legendary)

      switch (type)
      {
        case SubStatType.CritRate:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(1.5f, 3.0f), UnityEngine.Random.Range(3.5f, 6.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.CritDamage:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(8.0f, 15.0f), UnityEngine.Random.Range(16.0f, 25.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.Lifesteal:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(1.0f, 2.5f), UnityEngine.Random.Range(2.5f, 4.5f), qualityRatio) * 10f) / 10f;
        case SubStatType.SpiritCostReduction:
        case SubStatType.StaminaCostReduction:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(4.0f, 8.0f), UnityEngine.Random.Range(8.0f, 15.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.SpiritGainBonus:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(5.0f, 12.0f), UnityEngine.Random.Range(12.0f, 22.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.MoveSpeedBonus:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(2.5f, 5.0f), UnityEngine.Random.Range(5.0f, 9.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.AttackBonus:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(2.0f, 4.0f), UnityEngine.Random.Range(5.0f, 10.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.DefenseBonus:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(2.0f, 4.0f), UnityEngine.Random.Range(4.0f, 8.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.HealthBonus:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(15.0f, 30.0f), UnityEngine.Random.Range(35.0f, 65.0f), qualityRatio) * 10f) / 10f;
        case SubStatType.MaxStamina:
          return Mathf.Round(Mathf.Lerp(UnityEngine.Random.Range(8.0f, 15.0f), UnityEngine.Random.Range(15.0f, 25.0f), qualityRatio) * 10f) / 10f;
        default:
          return 5f;
      }
    }
  }
}
