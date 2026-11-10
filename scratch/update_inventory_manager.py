import os

path = r"Assets/Scripts/Inventory/InventoryManager.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update AddItem
target_additem = """    public bool AddItem(ItemData item, int quantity, out int remainder, bool notifyPickup = true)
    {
      remainder = quantity;
      if (item == null || quantity <= 0) return false;

      // 1. Nếu là Tiền vàng -> Cộng thẳng vào ví tiền
      if (item.ItemType == ItemType.Currency)
      {
        int totalGoldEarned = item.GoldValue * quantity;
        AddGold(totalGoldEarned);
        remainder = 0;
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, quantity);
        }
        return true;
      }

      int originalQuantity = quantity;

      // 2. Nếu là vật phẩm có thể cộng dồn (Stackable) -> Ưu tiên lấp đầy các ô đã có
      if (item.IsStackable)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (!slots[i].IsEmpty && slots[i].ItemData.Id == item.Id)
          {
            remainder = slots[i].AddQuantity(remainder);
            if (remainder <= 0) break;
          }
        }
      }

      // 3. Nếu vẫn còn dư -> Tìm các ô trống đầu tiên để thêm mới
      if (remainder > 0)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (slots[i].IsEmpty)
          {
            int toPut = Mathf.Min(remainder, item.MaxStack);
            slots[i].SetItem(item, toPut);
            remainder -= toPut;
            if (remainder <= 0) break;
          }
        }
      }

      int itemsAdded = originalQuantity - remainder;
      if (itemsAdded > 0)
      {
        OnInventoryChanged?.Invoke();
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, itemsAdded);
        }
        return true;
      }

      return false; // Túi đầy hoàn toàn
    }

    public bool AddItem(ItemData item, int quantity = 1, bool notifyPickup = true)
    {
      return AddItem(item, quantity, out _, notifyPickup);
    }"""

replacement_additem = """    public bool AddItem(ItemData item, int quantity, out int remainder, bool notifyPickup = true, bool allowRefillEquippedUsable = true)
    {
      remainder = quantity;
      if (item == null || quantity <= 0) return false;

      // 1. Nếu là Tiền vàng -> Cộng thẳng vào ví tiền
      if (item.ItemType == ItemType.Currency)
      {
        int totalGoldEarned = item.GoldValue * quantity;
        AddGold(totalGoldEarned);
        remainder = 0;
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, quantity);
        }
        return true;
      }

      int originalQuantity = quantity;

      // 1.5. Nếu là vật phẩm Tiêu hao (Consumable) và đang được trang bị trên ô Usable -> Ưu tiên nạp đầy ô Usable trước
      if (allowRefillEquippedUsable && item.ItemType == ItemType.Consumable && equippedUsable != null && equippedUsable.Id == item.Id)
      {
        int maxStack = item.MaxStack > 0 ? item.MaxStack : 99;
        int spaceLeft = Mathf.Max(0, maxStack - equippedUsableCount);
        if (spaceLeft > 0)
        {
          int toAdd = Mathf.Min(remainder, spaceLeft);
          equippedUsableCount += toAdd;
          remainder -= toAdd;
          OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
          OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
        }
      }

      // 2. Nếu là vật phẩm có thể cộng dồn (Stackable) -> Ưu tiên lấp đầy các ô đã có trong túi
      if (item.IsStackable && remainder > 0)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (!slots[i].IsEmpty && slots[i].ItemData.Id == item.Id)
          {
            remainder = slots[i].AddQuantity(remainder);
            if (remainder <= 0) break;
          }
        }
      }

      // 3. Nếu vẫn còn dư -> Tìm các ô trống đầu tiên để thêm mới
      if (remainder > 0)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (slots[i].IsEmpty)
          {
            int toPut = Mathf.Min(remainder, item.MaxStack);
            slots[i].SetItem(item, toPut);
            remainder -= toPut;
            if (remainder <= 0) break;
          }
        }
      }

      int itemsAdded = originalQuantity - remainder;
      if (itemsAdded > 0)
      {
        OnInventoryChanged?.Invoke();
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, itemsAdded);
        }
        return true;
      }

      return false; // Túi đầy hoàn toàn
    }

    public bool AddItem(ItemData item, int quantity = 1, bool notifyPickup = true, bool allowRefillEquippedUsable = true)
    {
      return AddItem(item, quantity, out _, notifyPickup, allowRefillEquippedUsable);
    }"""

assert target_additem.replace("\r\n", "\n") in content.replace("\r\n", "\n"), "target_additem not found"

content_norm = content.replace("\r\n", "\n")
content_norm = content_norm.replace(target_additem.replace("\r\n", "\n"), replacement_additem.replace("\r\n", "\n"))

# 2. Update Unequip call
target_unequip = "if (AddItem(toUnequip, toUnequipCount, out int remainder, notifyPickup: false))"
replacement_unequip = "if (AddItem(toUnequip, toUnequipCount, out int remainder, notifyPickup: false, allowRefillEquippedUsable: false))"

assert target_unequip in content_norm, "target_unequip not found"
content_norm = content_norm.replace(target_unequip, replacement_unequip)

with open(path, "w", encoding="utf-8") as f:
    f.write(content_norm)

print("INVENTORY_MANAGER_UPDATED_SUCCESSFULLY")
