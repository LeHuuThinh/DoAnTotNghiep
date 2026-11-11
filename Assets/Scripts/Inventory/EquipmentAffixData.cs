using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Các loại dòng chỉ số phụ ngẫu nhiên (Sub-stats) có thể xuất hiện trên trang bị
  /// </summary>
  public enum SubStatType
  {
    AttackBonus,          // + Flat ATK
    DefenseBonus,         // + Flat DEF
    HealthBonus,          // + Flat HP
    MaxStamina,           // + Flat Stamina
    CritRate,             // + % Tỷ lệ bạo kích
    CritDamage,           // + % Sát thương bạo kích
    Lifesteal,            // + % Hút máu
    SpiritCostReduction,  // - % Tiêu hao Thần Lực (Spirit)
    StaminaCostReduction, // - % Tiêu hao Thể Lực (Stamina)
    SpiritGainBonus,      // + % Tăng Spirit nhận được
    MoveSpeedBonus        // + % Tốc độ di chuyển
  }

  /// <summary>
  /// Đại diện cho 1 dòng chỉ số phụ ngẫu nhiên
  /// </summary>
  [Serializable]
  public struct SubStatEntry
  {
    public SubStatType type;
    public float value;

    public SubStatEntry(SubStatType type, float value)
    {
      this.type = type;
      this.value = value;
    }

    public string GetFormattedString()
    {
      switch (type)
      {
        case SubStatType.AttackBonus:
          return $"<color=#FFD54F>+ {value:0.#} Attack Damage</color>";
        case SubStatType.DefenseBonus:
          return $"<color=#81D4FA>+ {value:0.#} Defense</color>";
        case SubStatType.HealthBonus:
          return $"<color=#EF5350>+ {value:0.#} Max Health</color>";
        case SubStatType.MaxStamina:
          return $"<color=#66BB6A>+ {value:0.#} Max Stamina</color>";
        case SubStatType.CritRate:
          return $"<color=#FFB74D>+ {value:0.#}% Critical Rate</color>";
        case SubStatType.CritDamage:
          return $"<color=#FFA726>+ {value:0.#}% Critical Damage</color>";
        case SubStatType.Lifesteal:
          return $"<color=#E57373>+ {value:0.#}% Lifesteal</color>";
        case SubStatType.SpiritCostReduction:
          return $"<color=#4DD0E1>- {value:0.#}% Spirit Cost</color>";
        case SubStatType.StaminaCostReduction:
          return $"<color=#81C784>- {value:0.#}% Stamina Cost</color>";
        case SubStatType.SpiritGainBonus:
          return $"<color=#BA68C8>+ {value:0.#}% Spirit Gain</color>";
        case SubStatType.MoveSpeedBonus:
          return $"<color=#FFF176>+ {value:0.#}% Movement Speed</color>";
        default:
          return $"+ {value:0.#} {type}";
      }
    }
  }

  /// <summary>
  /// Danh mục 10 loại Phù Phép đặc biệt (Enchantments) thức tỉnh tại mốc +1, +3, +5
  /// </summary>
  public enum EnchantmentType
  {
    None = 0,
    BleedingCrit,           // Huyết Nhẫn: Đòn bạo kích gây Xuất Huyết 30% dmg qua 3s
    SkillECostReduction,    // Tật Phong Trảm: Giảm 30% Spirit riêng cho Kiếm Khí E
    SkillQCostReduction,    // Cuồng Lôi Bão: Giảm 30% Spirit riêng cho Lốc Xoáy Q
    SkillRCostReduction,    // Diệt Thế Thần Uy: Giảm 25% Spirit riêng cho Trảm Kích R
    LowHealthBloodthirst,   // Khát Máu Cuồng Nộ: Tăng +8% Hút Máu khi dưới 50% HP
    TitanCharge,            // Kình Lực Titan: Tăng +25% Sát thương đòn Gồng (Charge)
    ParryStaminaRestore,    // Bất Động Như Sơn: Phản đòn Parry thành công hồi ngay 25 Thể Lực
    SprintWind,             // Phong Hành Bộ: Sau khi Lướt/Lăn né tăng 20% Tốc chạy trong 2.5s
    CritFrenzy,             // Cuồng Trảm: Đòn bạo kích tăng 15% Tốc đánh trong 3.5s
    SoulSiphon              // Thu Hồn Quyết: Tiêu diệt kẻ địch hồi 15 Spirit & 15 HP
  }

  /// <summary>
  /// Đại diện cho 1 ô Phù Phép trên trang bị (mở tại mốc +1, +3 hoặc +5)
  /// </summary>
  [Serializable]
  public class EnchantmentEntry
  {
    public int milestoneLevel; // 1, 3, hoặc 5
    public EnchantmentType enchantType;
    public string title;
    public string description;
    public float value;
    public bool isUnlocked;

    public EnchantmentEntry() { }

    public EnchantmentEntry(int milestone, EnchantmentType type, string title, string description, float value = 0f, bool unlocked = false)
    {
      this.milestoneLevel = milestone;
      this.enchantType = type;
      this.title = title;
      this.description = description;
      this.value = value;
      this.isUnlocked = unlocked;
    }

    public EnchantmentEntry Clone()
    {
      return new EnchantmentEntry(milestoneLevel, enchantType, title, description, value, isUnlocked);
    }
  }

  /// <summary>
  /// Dữ liệu động (Instance Data) của một trang bị cụ thể trong túi hoặc trên người.
  /// Lưu trữ chỉ số chính đã roll, danh sách dòng phụ và các ô phù phép thức tỉnh.
  /// </summary>
  [Serializable]
  public class EquipmentAffixData
  {
    public float rolledAttack = 0f;
    public float rolledDefense = 0f;
    public float rolledHealth = 0f;

    public List<SubStatEntry> subStats = new List<SubStatEntry>();
    public List<EnchantmentEntry> enchantments = new List<EnchantmentEntry>();

    public float GetSubStatValue(SubStatType type, int upgradeLevel = 0)
    {
      float total = 0f;
      for (int i = 0; i < subStats.Count; i++)
      {
        if (subStats[i].type == type)
        {
          float val = subStats[i].value;
          if (upgradeLevel > 0)
          {
            val = InfiniteLabyrinth.Blacksmith.BlacksmithUpgradeConfig.CalculateUpgradedSubStat(val, upgradeLevel);
          }
          total += val;
        }
      }
      return total;
    }

    public bool HasEnchantment(EnchantmentType type)
    {
      for (int i = 0; i < enchantments.Count; i++)
      {
        if (enchantments[i].enchantType == type && enchantments[i].isUnlocked)
        {
          return true;
        }
      }
      return false;
    }

    public void UpdateEnchantmentUnlocks(int currentUpgradeLevel)
    {
      for (int i = 0; i < enchantments.Count; i++)
      {
        if (currentUpgradeLevel >= enchantments[i].milestoneLevel)
        {
          enchantments[i].isUnlocked = true;
        }
      }
    }

    public EquipmentAffixData Clone()
    {
      var clone = new EquipmentAffixData
      {
        rolledAttack = this.rolledAttack,
        rolledDefense = this.rolledDefense,
        rolledHealth = this.rolledHealth,
        subStats = new List<SubStatEntry>(this.subStats)
      };

      clone.enchantments = new List<EnchantmentEntry>();
      for (int i = 0; i < this.enchantments.Count; i++)
      {
        clone.enchantments.Add(this.enchantments[i].Clone());
      }

      return clone;
    }
  }
}
