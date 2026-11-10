import os

path = r"Docs/ACTIVE_TASKS.md"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target = """- [x] **UI-06: Hoàn thiện Ô Phím Tắt Tiêu Hao UsableSlotUI (HUD) & UsableSlot (LeftPanel) kèm Cooldown Toàn Cục 2.0s**
  - **Kiến trúc Ô Trang Bị Tiêu Hao (`UsableSlot` & `UsableSlotUI`):**
    - Cung cấp ô `UsableSlot` ở cửa sổ trái `LeftPanel` (`EquipmentSlotUI`, `ItemType.Consumable`) cho phép người chơi trang bị bình máu/thuốc trực tiếp từ túi đồ (hỗ trợ kéo thả linh hoạt, gộp stack và swap giữa các loại thuốc).
    - Cung cấp ô phím tắt `UsableSlotUI` ở góc dưới bên trái màn hình HUD hiển thị icon bình thuốc, số lượng stack (`StackCountText`), phím nóng [C], và nền độ hiếm theo phẩm cấp `RarityBG`/`RarityBar`.
    - Người chơi có thể bấm phím `[C]` bất kỳ lúc nào trong quá trình khám phá/chiến đấu (hoặc click chuột trực tiếp vào `UsableSlotUI`) để hồi máu ngay lập tức mà không cần mở túi đồ.
    - Hỗ trợ hoạt ảnh nhún nảy (Tactile Pop / Punch Animation) `punchScale = 1.15f` khi bấm dùng thuốc."""

replacement = """- [x] **UI-06: Hoàn thiện Ô Phím Tắt Tiêu Hao UsableSlotUI (HUD) & UsableSlot (LeftPanel) kèm Cooldown Toàn Cục 2.0s**
  - **Kiến trúc Ô Trang Bị Tiêu Hao (`UsableSlot` & `UsableSlotUI`):**
    - Cung cấp ô `UsableSlot` ở cửa sổ trái `LeftPanel` (`EquipmentSlotUI`, `ItemType.Consumable`) cho phép người chơi trang bị bình máu/thuốc trực tiếp từ túi đồ (hỗ trợ kéo thả linh hoạt, gộp stack và swap giữa các loại thuốc).
    - Cung cấp ô phím tắt `UsableSlotUI` ở góc dưới bên trái màn hình HUD hiển thị icon bình thuốc, số lượng stack (`StackCountText`), phím nóng [C], và nền độ hiếm theo phẩm cấp `RarityBG`/`RarityBar`.
    - Người chơi có thể bấm phím `[C]` bất kỳ lúc nào trong quá trình khám phá/chiến đấu (hoặc click chuột trực tiếp vào `UsableSlotUI`) để hồi máu ngay lập tức mà không cần mở túi đồ.
    - Hỗ trợ hoạt ảnh nhún nảy (Tactile Pop / Punch Animation) `punchScale = 1.15f` đồng bộ cả khi bấm phím `[C]` lẫn khi click chuột trực tiếp (`InventoryManager.OnUsableUsed`).
  - **Sửa Lỗi Thông Báo Nhặt Đồ Khi Tháo Trang Bị (Silent Unequip):**
    - Bổ sung cờ `notifyPickup = false` khi `Unequip()` trong `InventoryManager.cs`. Khắc phục triệt để lỗi tháo vũ khí, giáp, hoặc bình thuốc từ ô trang bị sang túi đồ bị kích hoạt nhầm bảng thông báo nhặt đồ mới (`PickupNotificationFeed`) bên góc trái màn hình."""

content = content.replace(target.replace("\r\n", "\n"), replacement.replace("\r\n", "\n"))
content = content.replace(target, replacement)

target_skill = """- [x] **UI-SKILL-01: Hệ Thống Giao Diện Kỹ Năng & Hồi Chiêu (SkillIconUI Cooldown & Spirit Feedback)**
  - **Cấu trúc phân cấp UI:** Tích hợp trực tiếp cụm `SkillIconUI` trong Canvas gồm 3 ô kỹ năng `SkillE` (size 100x100), `SkillQ` (size 100x100), `SkillR` (size 150x150). Mỗi ô gồm 4 thành phần: `Icon` (ảnh chiêu), `Corner` (hoa văn viền góc), `CDFill` (ảnh Type = Filled đếm ngược), `KeyIcon` (badge chữ E/Q/R)."""

replacement_skill = """- [x] **UI-SKILL-01: Hệ Thống Giao Diện Kỹ Năng & Hồi Chiêu (SkillIconUI Cooldown & Spirit Feedback)**
  - **Cấu trúc phân cấp UI & Hiệu Ứng Nảy (Pop Punch Animation):** Tích hợp trực tiếp cụm `SkillIconUI` trong Canvas gồm 3 ô kỹ năng `SkillE` (size 100x100), `SkillQ` (size 100x100), `SkillR` (size 150x150). Tích hợp hoạt ảnh nảy phồng (`punchScale = 1.15f`, `punchDuration = 0.15f`) khi thi triển qua phím tắt E/Q/R hoặc click chuột trực tiếp lên icon trên màn hình HUD."""

content = content.replace(target_skill.replace("\r\n", "\n"), replacement_skill.replace("\r\n", "\n"))
content = content.replace(target_skill, replacement_skill)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("SUCCESS")
