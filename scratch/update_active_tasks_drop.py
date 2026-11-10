import os

path = r"Docs/ACTIVE_TASKS.md"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target = """      - Cụm 4 nút hành động: `BtnEquip` và `BtnUnequip` xếp đè cùng 1 vị trí (Trang bị hiện khi xem đồ trong túi, Tháo ra hiện khi xem đồ đang mặc trên người); `BtnUse` cho bình máu; `BtnDrop` vứt đồ ra sàn đất."""

replacement = """      - Cụm 4 nút hành động: `BtnEquip` và `BtnUnequip` xếp đè cùng 1 vị trí (Trang bị hiện khi xem đồ trong túi, Tháo ra hiện khi xem đồ đang mặc trên người); `BtnUse` cho bình máu; `BtnDrop` vứt đồ ra sàn đất.
      - **Hoàn Thiện Hệ Thống Vứt Đồ 3D (`InventoryManager.DropItem` & `WorldItemPickup`):**
        * Quỹ đạo búng nảy Parabol (Arc Pop): Vật phẩm văng ra từ tầm ngực nhân vật, bay vòng cung 0.45s tiếp đất an toàn ở cự ly 1.5m phía trước.
        * Quét cản thông minh: Tự động raycast phát hiện tường chắn phía trước để co cự ly rơi an toàn (chống rơi xuyên/kẹt vào tường) và raycast mặt sàn thực tế (`NavMessFloor`, `Terrain`, `Default`) chống lơ lửng.
        * Mô hình 3D trực quan: Nếu là Vũ khí, tự động thay thế rương mini bằng chính mô hình 3D của thanh kiếm (`itemData.WeaponVisualPrefab`), xoay nghiêng 40 độ lơ lửng.
        * Hào quang Rarity đổi màu thời gian thực: Đèn PointLight phát sáng theo đúng màu độ hiếm (Common, Uncommon, Rare, Epic, Legendary) với cường độ tương ứng.
        * Tương tác nhặt lại: Khi tiếp đất, vật phẩm tự xoay tròn và nhấp nhô bồng bềnh; khi Player lại gần (<2.2m), HUD hiện prompt `[F] Nhặt [Item] (xCount)` và bấm F để nhặt lại kèm thông báo Pickup Feed."""

assert target.replace("\r\n", "\n") in content.replace("\r\n", "\n"), "target not found"
content_norm = content.replace("\r\n", "\n").replace(target.replace("\r\n", "\n"), replacement.replace("\r\n", "\n"))

with open(path, "w", encoding="utf-8") as f:
    f.write(content_norm)

print("ACTIVE_TASKS_DROP_UPDATED_SUCCESSFULLY")
