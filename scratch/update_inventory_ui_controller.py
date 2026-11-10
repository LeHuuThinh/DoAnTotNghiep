import os

path = r"Assets/Scripts/UI/Inventory/InventoryUIController.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target_start = """        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
      }"""
replacement_start = """        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        InventoryManager.Instance.OnUsableChanged += HandleUsableChanged;
      }"""

assert target_start.replace("\r\n", "\n") in content.replace("\r\n", "\n"), "target_start not found"
content_norm = content.replace("\r\n", "\n").replace(target_start.replace("\r\n", "\n"), replacement_start.replace("\r\n", "\n"))

target_destroy = """        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
      }"""
replacement_destroy = """        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
        InventoryManager.Instance.OnUsableChanged -= HandleUsableChanged;
      }"""

assert target_destroy.replace("\r\n", "\n") in content_norm, "target_destroy not found"
content_norm = content_norm.replace(target_destroy.replace("\r\n", "\n"), replacement_destroy.replace("\r\n", "\n"))

target_handler = """    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      if (IsOpen)
      {
        RefreshEquipment();
        RefreshStats();
      }
    }"""

replacement_handler = """    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      if (IsOpen)
      {
        RefreshEquipment();
        RefreshStats();
      }
    }

    private void HandleUsableChanged(ItemData item, int count)
    {
      if (IsOpen)
      {
        RefreshEquipment();
      }
    }"""

assert target_handler.replace("\r\n", "\n") in content_norm, "target_handler not found"
content_norm = content_norm.replace(target_handler.replace("\r\n", "\n"), replacement_handler.replace("\r\n", "\n"))

with open(path, "w", encoding="utf-8") as f:
    f.write(content_norm)

print("INVENTORY_UI_CONTROLLER_UPDATED_SUCCESSFULLY")
