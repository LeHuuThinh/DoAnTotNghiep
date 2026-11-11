using System;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Đại diện cho 1 ô chứa trong túi đồ (kèm dữ liệu động cấp cường hóa và dòng chỉ số/phù phép ngẫu nhiên)
  /// </summary>
  [Serializable]
  public class InventorySlot
  {
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity;
    [SerializeField] private int upgradeLevel = 0; // Cấp cường hóa (+0 đến +5)
    [SerializeField] private EquipmentAffixData affixData; // Dữ liệu động: dòng chính roll, dòng phụ và phù phép

    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public int UpgradeLevel => upgradeLevel;
    public EquipmentAffixData AffixData => affixData;
    public bool IsEmpty => itemData == null || quantity <= 0;

    public InventorySlot()
    {
      itemData = null;
      quantity = 0;
      upgradeLevel = 0;
      affixData = null;
    }

    public InventorySlot(ItemData item, int count, int upgrade = 0, EquipmentAffixData affix = null)
    {
      itemData = item;
      quantity = count;
      upgradeLevel = Mathf.Clamp(upgrade, 0, 5);
      affixData = affix;
      EnsureAffixData();
    }

    public void SetItem(ItemData item, int count, int upgrade = 0, EquipmentAffixData affix = null)
    {
      itemData = item;
      quantity = count;
      upgradeLevel = Mathf.Clamp(upgrade, 0, 5);
      affixData = affix;
      EnsureAffixData();
    }

    public void SetAffixData(EquipmentAffixData affix)
    {
      affixData = affix;
      if (affixData != null)
      {
        affixData.UpdateEnchantmentUnlocks(upgradeLevel);
      }
    }

    public EquipmentAffixData EnsureAffixData()
    {
      if (itemData != null && (itemData.ItemType == ItemType.Weapon || itemData.ItemType == ItemType.Armor))
      {
        if (affixData == null)
        {
          affixData = EquipmentAffixGenerator.Generate(itemData, upgradeLevel);
        }
        else
        {
          affixData.UpdateEnchantmentUnlocks(upgradeLevel);
        }
      }
      return affixData;
    }

    public void SetUpgradeLevel(int level)
    {
      upgradeLevel = Mathf.Clamp(level, 0, 5);
      if (affixData != null)
      {
        affixData.UpdateEnchantmentUnlocks(upgradeLevel);
      }
    }

    public int AddQuantity(int amount)
    {
      if (itemData == null) return amount;

      int max = itemData.MaxStack;
      int spaceLeft = max - quantity;
      int toAdd = Mathf.Min(spaceLeft, amount);

      quantity += toAdd;
      return amount - toAdd; // Trả về số lượng còn thừa chưa cho vào được
    }

    public bool RemoveQuantity(int amount)
    {
      if (quantity < amount) return false;

      quantity -= amount;
      if (quantity <= 0)
      {
        Clear();
      }
      return true;
    }

    public void Clear()
    {
      itemData = null;
      quantity = 0;
      upgradeLevel = 0;
      affixData = null;
    }
  }
}
