# Danh Sách Nhiệm Vụ Phát Triển (Active Tasks Roadmap) - Infinite Labyrinth

> [!TIP]
> **Hướng dẫn gọi việc cho AI:** Mỗi khi mở một phiên chat mới, chỉ cần gõ đúng mã nhiệm vụ.  
> _Ví dụ:_ `"Đọc Docs/SYSTEM_ARCHITECTURE.md và Docs/ACTIVE_TASKS.md. Hãy cùng tôi làm Task D-01: Tự động sinh quái trong các phòng hầm ngục."*

---

## 🚩 GIAI ĐOẠN 1: Hoàn Thiện Trải Nghiệm Chơi Hầm Ngục (Dungeon Playability)

- [x] **CH-01: Commit Git Checkpoint hiện tại**
  - Đã lưu các commit: `e6ec005` (feat: Add template for generation and Game optimization), `1dff1c6` (feat: Add random props and lighting), `929ce8b` (feat: Add second floor generation).

- [x] **PCG-01: Hệ thống Templates kiến trúc tàn tích (8 Wall) & Tiểu cảnh đạo cụ (5 Props)**
  - Tự động tính toán Bounding Box footprint của Prefabs template (`GetPrefabLocalBounds`).
  - Xoay 4 hướng (0, 90, 180, 270) và kiểm tra an toàn đường đi (tránh cửa, hành lang, chân cầu thang).
  - Tự động đánh dấu diện tích chiếm dụng vào `forbiddenTiles` để props thường không bao giờ sinh đè lên.
  - Tách toàn bộ `Light` và `ParticleSystem` (khói, lửa) sang nhóm `Lights` để không bị xóa khi gộp mesh.

- [x] **PCG-02: Trang trí Gác lửng Tầng 2 & Hệ thống Đuốc sàn tự do**
  - Khớp chuẩn 100% cao độ tầng 2 (`mY = 8.0m`), triệt tiêu hoàn toàn lỗi đồ vật lơ lửng giữa 2 tầng.
  - Cờ hiệu treo tường ngẫu nhiên cao thấp (`minBannerHeight = 2.2m`, `maxBannerHeight = 3.2m`).
  - Đuốc sàn tự do trên cả sàn tầng 1 và tầng 2, bổ sung `floorTorchYOffset = 0.5f` chống lún chân đế.
  - Gộp Mesh tĩnh (Mesh Combine) theo từng nhóm Material thành 1 Draw Call duy nhất, lọc bỏ MeshCollider thừa cho cờ vải/thủy tinh.

- [x] **REF-01: Tái cấu trúc phân rã `DungeonPropDecorator.cs` (1.700 dòng -> Clean Sub-modules)**
  - Tách God Class `DungeonPropDecorator.cs` theo mô hình **Facade Orchestrator & Partial Classes + Utility**:
    + [`DungeonPropDecorator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.cs): Giữ 100% Inspector Fields, Awake/Update, API `DecorateDungeon(...)` và điều phối chính (437 dòng).
    + [`DungeonPropDecorator.Templates.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Templates.cs): Quản lý Templates tàn tích & tiểu cảnh, tính toán Bounding Box (337 dòng).
    + [`DungeonPropDecorator.Lighting.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Lighting.cs): Hệ thống đuốc sàn, đuốc tường, đuốc hành lang (221 dòng).
    + [`DungeonPropDecorator.Mezzanine.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Mezzanine.cs): Trang trí toàn bộ gác lửng tầng 2 (155 dòng).
    + [`DungeonPropDecorator.Furniture.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Furniture.cs): Đồ nội thất tường, bàn ghế tâm phòng, góc phòng, rải rác sàn (407 dòng).
    + [`DungeonPropDecorator.Spatial.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Spatial.cs): Thuật toán lưới, cấm đặt ô, tường đặc, họ cờ hiệu (148 dòng).
    + [`DungeonStaticPropCombiner.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonStaticPropCombiner.cs): Utility độc lập gộp Mesh và lọc Collider (79 dòng).
  - Đảm bảo 100% Zero Breaking Changes: Không mất dữ liệu Inspector, không ảnh hưởng `DungeonRenderer` và luồng sinh hầm ngục.

- [x] **FIX-01: Khắc phục quái vật đi xuyên qua Props (Đồng bộ Bake NavMesh sau Props)**
  - **Nguyên nhân:** Trước đây hàm `BakeAndValidateNavMesh` chạy trước `propDecorator.DecorateDungeon`, đồng thời `layerMask` của `NavMeshSurface` chỉ thu thập Layer 10 (Sàn phẳng `InvisFloorPrefab`), bỏ qua hoàn toàn các Collider của Props và Wall Templates ở Layer 0.
  - **Khắc phục triệt để:**
    + Đổi thứ tự trong `DungeonRenderer.BuildMesh`: Sinh và gộp mesh Props hoàn tất trước, sau đó mới tiến hành `BakeAndValidateNavMesh`.
    + Gắn `NavMeshModifier(ignoreFromBuild = true)` lên `areaObj` để NavMeshSurface không bake lên trần hay tường visual.
    + Gắn `NavMeshModifier(overrideArea = true, area = 1 - Not Walkable)` lên toàn bộ `combinedObj` có MeshCollider trong `DungeonStaticPropCombiner.cs`.
    + Mở rộng `navMeshSurface.layerMask = (1 << 10) | (1 << 0)`: Thu thập cả sàn (Layer 10) và props vật cản (Layer 0), tự động đục thủng NavMesh tại footprint của props, quái vật (NavMeshAgent) tự động đi vòng né tránh.

- [x] **OPT-01: Tối ưu hóa hiệu năng URP & Hệ thống Ánh sáng (Phục hồi 40 FPS -> 110+ FPS)**
  - Triệt tiêu Shadow Caster Pass của Directional Light cường độ 0 (giải phóng 4 cascades 2048x2048 vô ích, tiết kiệm 4-6ms GPU).
  - Viết lại `DungeonLightOptimizer.cs`: Culling kép cả `Light` lẫn `ParticleSystem` (khói & lửa của đuốc) khi ở xa, giải phóng hơn 160 emitter hạt.
  - Bổ sung cơ chế **Hysteresis Buffer (đệm 6m)** chống nhấp nháy đèn ở ranh giới.
  - Mở rộng thanh trượt `Light Culling Distance` (10m - 150m, mặc định 80m) và đồng bộ `Camera Far Clip Plane` (200m) phù hợp hoàn hảo với hầm ngục quy mô lớn 300m x 322m.

- [x] **UI-01: Cập nhật `PlayerHUD` hiển thị đầy đủ 3 thanh tài nguyên Souls-like**
  - **Thanh Máu (Health Bar):** MainFill màu đỏ tươi kèm **Ghost Bar** màu vàng nhạt tụt đuổi từ từ sau thời gian trễ khi dính đòn.
  - **Thanh Thể lực (Stamina Bar):** Màu vàng/cam, co giãn mượt mà khi Lăn né (Roll), Chạy nhanh (Sprint), Chém kiếm (Attack).
  - **Cụm Sao Năng lượng Chiêu thức (Spirit Stars):** 3 ngôi sao nạp đầy khi chém trúng địch / parry, tiêu hao khi dùng kỹ năng E (Sóng kiếm) và Q (Nổ năng lượng).
  - Đã liên kết và hoạt động hoàn hảo trong `DungeonScene.unity`.

- [x] **D-01: Bộ sinh quái tự động cho hầm ngục theo Biệt Đội / Cụm (`ProceduralEnemySpawner.cs`)**
  - Hệ thống sinh quái theo Biệt Đội (Squad / Pack System) kết hợp định vị không gian (Spatial Clustering):
    - **Tổ hợp Biệt Đội chuẩn RPG:**
      + Nhóm 4 quái: 2 Melee (1 Warrior + 1 Minion) + 2 Ranged (1 Rogue + 1 Mage).
      + Nhóm 3 quái: 2 Melee + 1 Ranged HOẶC 1 Melee + 2 Ranged.
      + Nhóm 2 quái: 2 Melee HOẶC 1 Melee + 1 Ranged.
      + Nhóm 1 quái lẻ: Random ngẫu nhiên 1 trong 4 loại.
    - **Phân bổ theo quy mô phòng (Dungeon 300x300m, tile size = 2):**
      + Sàn trệt phòng nhỏ và vừa (< 220 tiles): Đúng 1 nhóm quái.
      + Sàn tầng 2 gác lửng phòng nhỏ/vừa (nếu có): Đúng 1 nhóm quái.
      + Sàn trệt phòng đại sảnh lớn (>= 220 tiles): 1 – 2 nhóm quái.
      + Sàn tầng 2 gác lửng phòng lớn (nếu có): 1 – 2 nhóm quái.
      + Phòng kho báu / Điện thờ (Treasure / Shrine): 1 nhóm nhỏ (1–2 quái) canh gác.
      + Phòng đầu tiên nối với sảnh lối vào (`data.startRoom`): Sinh quái bình thường để bước vào hầm ngục là có quái chiến đấu ngay, không bị trống trải.
      + Chỉ bỏ qua duy nhất Boss Room (dành riêng cho Boss Arena D-02).
    - **Định vị & An toàn:** Mỗi nhóm chọn 1 Điểm Neo (Anchor Point), các thành viên tụ quanh tâm bán kính 3m, các cụm nhóm cách nhau tối thiểu 8.5m để tạo nhiều điểm đụng độ riêng biệt. Bám sàn NavMesh và chống sinh đè lên đạo cụ tĩnh.
    - Sửa triệt để cơ chế Target Lock System nhận diện chính xác quái vật tự sinh (bắt qua `Health.GetComponentInParent` thay vì `transform.root`).
  - **Tối ưu hóa hiệu năng quái vật hầm ngục (`DungeonEnemyOptimizer.cs` + `EnemyAIBase.cs` + `EnemyFeedback.cs`):**
    - **Cơ chế 3 Vùng Cự Ly (Distance-based Hibernation):**
      + Vùng Thức tỉnh (< 40m): Kích hoạt toàn bộ AI, NavMeshAgent, Animator tính toán chuyển động và bật lại toàn bộ Renderers.
      + Vùng Đệm Hysteresis (40m - 48m, buffer 8m): Chống hiện tượng bật/tắt chập chờn khi người chơi đứng ở ranh giới.
      + Vùng Ngủ đông (> 48m): Ngắt `Update()` AI, dừng `NavMeshAgent` (tắt Obstacle Avoidance), tắt `animator.enabled = false` (ngắt hoàn toàn tính toán ma trận xương), tắt toàn bộ `renderer.enabled = false` (triệt tiêu 100% Draw Calls và Dynamic Shadow Passes cho quái ở xa).
    - **Tối ưu Material bằng `MaterialPropertyBlock`:** Chớp đỏ trúng đòn trên GPU không clone material, bảo toàn 100% SRP Batcher & GPU Instancing.
    - **Time-Slicing Batching (4 Batches, 0.12s):** Chia nhỏ quái thành 4 nhóm để quét cự ly luân phiên, triệt tiêu hoàn toàn CPU micro-stutter.
    - **Combat Awakening:** Nếu quái đang ngủ bị trúng đòn tầm xa hoặc dính sát thương (`OnTakeDamageAggro`), quái sẽ lập tức thức tỉnh 100% phản xạ chiến đấu.
  - Đã gắn component và liên kết hoàn chỉnh trong `DungeonScene.unity`.

- [ ] **D-02: Cơ chế Boss Arena tại Boss Room** *(ƯU TIÊN SỐ 1 HIỆN TẠI)*
  - Đặt Boss `Boss_IceGolem.prefab` tại trung tâm `data.bossRoom`.
  - Cửa chặn phòng (Fog Gate / Iron Gate): Khi người chơi bước vào phòng Boss, cửa tự động đóng lại; hạ gục Boss thì mở cửa và kích hoạt Cổng dịch chuyển chiến thắng.

---

## 🚩 GIAI ĐOẠN 2: Vòng Lặp Trò Chơi & Quản Lý Ngày (Core Game Loop & Day System)

- [ ] **GM-01: Quản lý vòng lặp ngày & tầng (`DayManager.cs` / `GameManager.cs`)**
  - Lưu trữ biến tĩnh `CurrentDay` (bắt đầu từ Day 1) và `CurrentFloor` (Tầng 1).
  - Sử dụng cơ chế DontDestroyOnLoad để giữ dữ liệu xuyên cảnh.

- [ ] **GM-02: Cơ chế Tử trận & Hồi sinh tại Chuông Làng (Death & Respawn Loop)**
  - Khi Máu người chơi = 0 trong `DungeonScene`:
    - Hiển thị màn hình You Died ngắn gọn.
    - Tăng `CurrentDay += 1`.
    - Load lại `LandScene` và đặt vị trí người chơi ngay cạnh **Chuông Trung Tâm Làng**.
    - Hồi phục 100% Máu/Stamina.

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
    - `+1.0` khi đánh trúng Player; `+2.0` khi hạ gục Player.
    - `-0.5` khi đánh trượt; `-1.0` khi bị Player Parry thành công.
    - `+0.01` khi giữ khoảng cách combat tối ưu.

- [ ] **ML-03: Huấn luyện & Đóng gói Model ONNX**
  - Dựng sàn đấu arena phụ trong scene `IceGolem.unity` để huấn luyện tự động với bot đối thủ hoặc player mô phỏng.
  - Xuất file `.onnx` và nhúng vào `Model` của `Behavior Parameters` trong Unity để chạy thời gian thực (Inference) không cần cài Python.

---

## 🚩 GIAI ĐOẠN 5: Giao Diện Hoàn Chỉnh & Âm Thanh (UI & Polish)

- [ ] **POL-01: Hệ thống Menu** (MainMenu, Settings, PauseMenu).
- [ ] **POL-02: Âm thanh (SFX & BGM)** (Nhạc nền Làng yên bình, Nhạc nền Hầm ngục rùng rợn, Nhạc đấu Boss dồn dập; Tiếng va chạm kim loại, tiếng quái gầm).
- [ ] **POL-03: Minimap / La bàn chỉ đường** định hướng trong Dungeon.
