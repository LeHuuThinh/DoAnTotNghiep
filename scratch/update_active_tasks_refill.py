import os

path = r"Docs/ACTIVE_TASKS.md"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target = """  - **Sửa Lỗi Thông Báo Nhặt Đồ Khi Tháo Trang Bị (Silent Unequip):**
    - Bổ sung cờ `notifyPickup = false` khi `Unequip()` trong `InventoryManager.cs`. Khắc phục triệt để lỗi tháo vũ khí, giáp, hoặc bình thuốc từ ô trang bị sang túi đồ bị kích hoạt nhầm bảng thông báo nhặt đồ mới (`PickupNotificationFeed`) bên góc trái màn hình."""

replacement = """  - **Tự Động Nạp Đầy Ô Thuốc Đang Trang Bị Khi Nhặt Thêm (Auto-Refill Usable Slot on Pickup):**
    - Khi người chơi nhặt thêm bình thuốc từ sàn ngục/rương báu, nếu trùng khớp ID với bình thuốc đang trang bị trong ô `UsableSlot` (`equippedUsable.Id == item.Id`), hệ thống sẽ **ưu tiên cộng dồn nạp đầy ô Usable trước** cho đến khi đạt giới hạn `MaxStack` (ví dụ 10/10).
    - Lượng thuốc còn dư (nếu nhặt vượt quá MaxStack) sẽ tự động chảy tiếp vào các ô túi đồ `Inventory`.
    - Ô `UsableSlotUI` ngoài màn hình HUD và `UsableSlot` trong Inventory tự động cập nhật số lượng dồn thời gian thực.
    - An toàn tuyệt đối: Không tự ý gán thuốc mới vào ô Usable khi ô này đang trống; ngắt tự nạp khi tháo trang bị (`allowRefillEquippedUsable = false` trong `Unequip`).
  - **Sửa Lỗi Thông Báo Nhặt Đồ Khi Tháo Trang Bị (Silent Unequip):**
    - Bổ sung cờ `notifyPickup = false` khi `Unequip()` trong `InventoryManager.cs`. Khắc phục triệt để lỗi tháo vũ khí, giáp, hoặc bình thuốc từ ô trang bị sang túi đồ bị kích hoạt nhầm bảng thông báo nhặt đồ mới (`PickupNotificationFeed`) bên góc trái màn hình."""

assert target.replace("\r\n", "\n") in content.replace("\r\n", "\n"), "target not found"
content_norm = content.replace("\r\n", "\n").replace(target.replace("\r\n", "\n"), replacement.replace("\r\n", "\n"))

with open(path, "w", encoding="utf-8") as f:
    f.write(content_norm)

print("ACTIVE_TASKS_UPDATED_SUCCESSFULLY")
