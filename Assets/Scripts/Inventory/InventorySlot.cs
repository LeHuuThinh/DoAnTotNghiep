using System;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Đại diện cho 1 ô chứa trong túi đồ
  /// </summary>
  [Serializable]
  public class InventorySlot
  {
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity;

    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public bool IsEmpty => itemData == null || quantity <= 0;

    public InventorySlot()
    {
      itemData = null;
      quantity = 0;
    }

    public InventorySlot(ItemData item, int count)
    {
      itemData = item;
      quantity = count;
    }

    public void SetItem(ItemData item, int count)
    {
      itemData = item;
      quantity = count;
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
    }
  }
}
