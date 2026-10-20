# Danh Sách Nhiệm Vụ Phát Triển (Active Tasks Roadmap) - Infinite Labyrinth

> [!TIP]
> **Hướng dẫn gọi việc cho AI:** Mỗi khi mở một phiên chat mới, chỉ cần gõ đúng mã nhiệm vụ.  
> *Ví dụ:* `"Đọc Docs/SYSTEM_ARCHITECTURE.md và Docs/ACTIVE_TASKS.md. Hãy cùng tôi làm Task D-01: Tự động sinh quái trong các phòng hầm ngục."*

---

## 🚩 GIAI ĐOẠN 1: Hoàn Thiện Trải Nghiệm Chơi Hầm Ngục (Dungeon Playability)

- [ ] **CH-01: Commit Git Checkpoint hiện tại**
  - Lưu toàn bộ thay đổi vừa xong (Jump/Fall animation, Light optimizer, Decorator).
  - Lệnh: `git add . && git commit -m "feat: jump fall animation and dungeon lighting"`

- [ ] **D-01: Bộ sinh quái tự động cho hầm ngục (`ProceduralEnemySpawner.cs`)**
  - Dựa vào danh sách `RoomData` từ `DungeonManager`:
    * Phòng thường (Normal): Sinh ngẫu nhiên 2–3 Skeleton Minion / Warrior.
    * Phòng lớn / Phòng gác lửng (Mezzanine): Sinh thêm Skeleton Rogue hoặc Mage trên tầng 2.
    * Đảm bảo quái sinh đúng trên NavMesh (dùng `NavMesh.SamplePosition`).

- [ ] **UI-01: Cập nhật `PlayerHUD` hiển thị đầy đủ 3 thanh tài nguyên**
  - Hiện tại `PlayerHUD` mới có thanh Máu (HP).
  - Bổ sung thanh **Thể lực (Stamina)** (xanh lá/vàng) và thanh **Năng lượng Chiêu thức (Spirit)** (xanh dương/tím).
  - Lắng nghe trực tiếp `onStaminaChanged` và `onSpiritChanged` từ `PlayerStats.cs`.

- [ ] **D-02: Cơ chế Boss Arena tại Boss Room**
  - Đặt Boss `Boss_IceGolem.prefab` tại trung tâm `data.bossRoom`.
  - Cửa chặn phòng (Fog Gate / Iron Gate): Khi người chơi bước vào phòng Boss, cửa tự động đóng lại; hạ gục Boss thì mở cửa và kích hoạt Cổng dịch chuyển.

---

## 🚩 GIAI ĐOẠN 2: Vòng Lặp Trò Chơi & Quản Lý Ngày (Core Game Loop & Day System)

- [ ] **GM-01: Quản lý vòng lặp ngày & tầng (`DayManager.cs` / `GameManager.cs`)**
  - Lưu trữ biến tĩnh `CurrentDay` (bắt đầu từ Day 1) và `CurrentFloor` (Tầng 1).
  - Sử dụng cơ chế DontDestroyOnLoad để giữ dữ liệu xuyên cảnh.

- [ ] **GM-02: Cơ chế Tử trận & Hồi sinh tại Chuông Làng (Death & Respawn Loop)**
  - Khi Máu người chơi = 0 trong `DungeonScene`:
    * Hiển thị màn hình You Died ngắn gọn.
    * Tăng `CurrentDay += 1`.
    * Load lại `LandScene` và đặt vị trí người chơi ngay cạnh **Chuông Trung Tâm Làng**.
    * Hồi phục 100% Máu/Stamina.

- [ ] **GM-03: Cơ chế Chiến thắng Tầng & Lựa chọn Đi tiếp / Về Làng**
  - Khi Boss chết: Hiện UI thông báo chiến thắng.
  - Người chơi có 2 lựa chọn:
    1. **Về Làng:** Load về `LandScene`, giữ lại toàn bộ đồ và chuyển sang Day tiếp theo.
    2. **Xuống Tầng Kế Tiếp (Floor + 1):** Load lại `DungeonScene` với seed mới, tăng chỉ số quái và tỷ lệ rớt đồ.

---

## 🚩 GIAI ĐOẠN 3: Kinh Tế & Tiến Trình RPG (Loot, Inventory, Crafting)

- [ ] **RPG-01: Rơi đồ khi đập thùng gỗ & giết quái (Loot Drop System)**
  - Thùng gỗ và Quái khi chết có xác suất rơi ra: Đồng tiền Vàng (Gold Coins), Đá hầm ngục (Ore), Xương quái (Bones), Bình máu (Potion).
  - Người chơi lại gần tự động hút hoặc bấm E/F để nhặt.

- [ ] **RPG-02: Hệ thống Túi Đồ (Inventory System & UI)**
  - Thiết kế cấu trúc dữ liệu `ItemData` (ScriptableObject) và `InventoryManager`.
  - UI phím `Tab` hoặc `I` mở túi đồ hiển thị danh sách vật phẩm, số lượng Vàng, phím tắt dùng bình máu nhanh.

- [ ] **RPG-03: Thợ Rèn (Blacksmith) & Nâng Cấp Vũ Khí tại Làng**
  - NPC Thợ rèn tại `LandScene`: Khi tương tác (bấm F) mở UI rèn.
  - Công thức: Vũ khí hiện tại + [X] Vàng + [Y] Đá cường hóa -> Tăng sát thương kiếm.

- [ ] **RPG-04: Cấp độ & Cộng Điểm Tiềm Năng (Leveling & Attributes)**
  - Đánh quái nhận EXP. Đủ EXP thì Lên cấp.
  - Mỗi cấp cho 2–3 điểm tiềm năng để cộng vào: Máu tối đa (Max HP), Sức tấn công (Attack Damage), Thể lực (Max Stamina).

---

## 🚩 GIAI ĐOẠN 4: Điểm Nhấn Đồ Án - Trùm Học Máy (Boss ML-Agents)

- [ ] **ML-01: Tích hợp Unity ML-Agents Package**
  - Cài đặt package `com.unity.ml-agents` qua Unity Package Manager.
  - Chuẩn bị môi trường Python (PyTorch + mlagents) để phục vụ huấn luyện.

- [ ] **ML-02: Thiết kế `IceGolemAgent.cs`**
  - **Observations:** Vị trí tương đối so với Player, góc quay, khoảng cách, trạng thái hành động của Player (đang chém, đang roll, đang parry).
  - **Actions (Discrete/Continuous):** Quyết định tung đòn tấn công thường, dậm đất (JumpAttack), gầm (Roar), hoặc di chuyển bao vây.
  - **Reward Signal:**
    * `+1.0` khi đánh trúng Player; `+2.0` khi hạ gục Player.
    * `-0.5` khi đánh trượt; `-1.0` khi bị Player Parry thành công.
    * `+0.01` khi giữ khoảng cách combat tối ưu.

- [ ] **ML-03: Huấn luyện & Đóng gói Model ONNX**
  - Dựng sàn đấu arena phụ trong scene `IceGolem.unity` để huấn luyện tự động với bot đối thủ hoặc player mô phỏng.
  - Xuất file `.onnx` và nhúng vào `Model` của `Behavior Parameters` trong Unity để chạy thời gian thực (Inference) không cần cài Python.

---

## 🚩 GIAI ĐOẠN 5: Giao Diện Hoàn Chỉnh & Âm Thanh (UI & Polish)

- [ ] **POL-01: Hệ thống Menu** (MainMenu, Settings, PauseMenu).
- [ ] **POL-02: Âm thanh (SFX & BGM)** (Nhạc nền Làng yên bình, Nhạc nền Hầm ngục rùng rợn, Nhạc đấu Boss dồn dập; Tiếng va chạm kim loại, tiếng quái gầm).
- [ ] **POL-03: Minimap / La bàn chỉ đường** định hướng trong Dungeon.
