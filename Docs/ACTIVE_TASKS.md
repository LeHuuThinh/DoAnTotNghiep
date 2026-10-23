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

- [x] **FIX-02: Cập nhật đường dẫn Prefabs & Triệt tiêu Warning / NullReferenceException**
  - Đồng bộ việc di chuyển thư mục `Dungeon` từ `Assets/Enviro Prefabs/Dungeon` sang `Assets/Prefabs/Dungeon`.
  - Bổ sung cơ chế `ResolveDungeonFolder` thông minh trong [`DungeonPropDecorator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.cs) và [`DungeonRenderer.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonRenderer.cs), tự động phát hiện đường dẫn mới và hỗ trợ fallback.
  - Triệt tiêu 6 warnings "Folder not found" và lỗi `NullReferenceException` của Unity UIElements ListView khi render Inspector.

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

## 🚩 GIAI ĐOẠN 3: Kinh Tế & Tiến Trình RPG (Loot, Inventory, Progression)

- [x] **RPG-01: Kiến trúc Dữ liệu Vật phẩm & Quản lý Kho đồ (`ItemData.cs`, `InventoryManager.cs`)**
  - Thiết kế `ItemData` (ScriptableObject): ID, Tên, Mô tả, Icon, Phân loại (`Weapon`, `Armor`, `Consumable`, `Material`, `Currency`), Độ hiếm (`Common`, `Rare`, `Epic`, `Legendary`), MaxStack, Chỉ số cộng thêm (`bonusAttack`, `bonusHealth`, `bonusStamina`, `bonusDefense`), Model 3D rớt đất (`worldModelPrefab`), Model cầm tay (`weaponVisualPrefab`).
  - Cấu trúc `InventorySlot` (Lưu `ItemData` + số lượng stack, hỗ trợ dồn ô và kiểm tra dung lượng).
  - Thiết kế `InventoryManager.cs`: Singleton/DontDestroyOnLoad quản lý 30 slot đồ, lượng Vàng, các ô trang bị (`equippedWeapon`, `equippedArmor`), và toàn bộ API `AddItem`, `RemoveItem`, `Equip`, `Unequip`, `UseItem`, `DropItem`, `AddGold`, `SpendGold`.
  - Kết nối trực tiếp chỉ số trang bị vào combat: Vũ khí tăng trực tiếp sát thương cho combo chém trong `PlayerCombat.cs`, Giáp giảm sát thương nhận vào trong `Health.cs`.
  - Tích hợp công cụ sinh dữ liệu tự động `ItemDatabaseGenerator.cs` trên Unity Menu (`Infinite Labyrinth -> RPG -> 1. Khởi Tạo Bộ Vật Phẩm Mẫu`) gồm 8 vật phẩm: *Kiếm Gỗ Tập Sự, Kiếm Hắc Thiết, Cốt Ma Kiếm, Hỏa Long Kiếm, Giáp Da Thám Hiểm, Bình Máu Nhỏ, Đá Hầm Ngục Cường Hóa, Đồng Tiền Vàng*.

- [x] **RPG-02: Rơi đồ thế giới từ Quái, Thùng gỗ & Rương (`LootDropper`, `BreakableProp`, `ChestInteractable`, `WorldItemPickup`)**
  - `LootData.cs`: Cấu hình danh sách tỉ lệ rơi đồ `LootEntry` (tỉ lệ dropChance, min/max quantity).
  - `WorldItemPickup.cs`: Thực thể vật phẩm 3D trong hầm ngục dùng chung model rương mini `trunk_small_A` thu nhỏ (scale 0.42x) cho Trang bị / Vật phẩm, hiệu ứng búng nảy parabol (Arc Pop) khi xuất hiện, xoay tròn lơ lửng bồng bềnh, PointLight hào quang đổi màu thời gian thực theo phẩm cấp Rarity (`itemData.Rarity.GetRarityColor()`).
  - **Hệ thống Mô hình Vàng phân cấp (Tiered Gold Visuals & Thresholds):** Tách riêng Vàng sử dụng 4 mô hình độc lập (`coin`, `coin_stack_small`, `coin_stack_medium`, `coin_stack_large`) kèm khả năng tùy biến ngưỡng Thresholds trên Inspector (`<10`: 1 đồng, `<35`: cụm nhỏ, `<75`: cụm vừa, `>=75`: đống vàng lớn cho Boss/Rương vàng).
  - Hỗ trợ cơ chế tự động bay hút (Magnetic Auto-pickup) cho Tiền Vàng khi người chơi lại gần trong bán kính 4.2m, và bấm phím `[F]` để nhặt đồ thường vào `InventoryManager`.
  - `BreakableProp.cs`: Kế thừa `IDamageable`, gắn vào các thùng gỗ (`Breakable_Barrel_Small`, `Breakable_Barrel_Large`), nhận sát thương từ `WeaponHitbox` vung kiếm, kích hoạt rơi loot và tự hủy.
  - `ChestInteractable.cs`: Gắn vào rương kho báu (`Interactive_Chest`, `Interactive_Chest_Gold`), người chơi lại gần bấm phím `[F]` để mở nắp lật ra sau và tung các món đồ ra xung quanh.
  - `LootDropper.cs`: Tích hợp trên Quái vật (`SkeletonWarrior`, `SkeletonMinion`, `SkeletonRogue`, `SkeletonMage`), Thùng gỗ và Rương.
  - Tích hợp 2 MenuItem tự động hóa trong Unity Editor:
    * `Infinite Labyrinth -> RPG -> 2. Tạo Prefab Rơi Đồ (WorldItemPickup Prefab)`
    * `Infinite Labyrinth -> RPG -> 3. Cấu Hình Tự Động Rơi Đồ (Setup Props & Monsters Loot)` để tự động cấu hình 100% Loot và 4 cấp độ Vàng cho toàn bộ Thùng, Rương và Quái vật.

- [x] **RPG-03: Tương tác Nhặt đồ (Pickup Prompt UI & Feed Thông Báo Mép Trái)**
  - `PickupPromptUI.cs`: Khi Player bước vào phạm vi nhặt (2m) của bất kỳ item nào trên sàn, hiển thị bảng nổi ở cạnh dưới tâm màn hình gồm: Icon 2D, Tên vật phẩm đổi màu theo phẩm cấp Rarity (`Common` trắng, `Rare` lục, `Epic` tím, `Legendary` cam), và phím bấm `[F] Nhặt (xSố lượng)`. Hiệu ứng mờ dần mượt mà (Fade In/Out qua CanvasGroup).
  - Cơ chế hút Vàng tự động (Magnetic Auto-pickup): Vàng tự động bay về phía ngực Player và cộng trực tiếp vào túi khi lại gần trong bán kính 4.2m (không cần bấm F).
  - `PickupNotificationFeed.cs` & `PickupNotificationItem.cs`: Danh sách thông báo xếp tầng góc mép trái màn hình. Khi nhặt vật phẩm / nhận vàng, dòng thông báo trượt mượt mà (Slide In từ trái qua), giữ hiển thị trong 2.5s rồi trôi nhẹ lên trên và mờ dần (Slide Up + Fade Out). Tự động dọn dẹp hàng đợi tối đa 5 thông báo.
  - Tích hợp MenuItem tự động hóa: `Infinite Labyrinth -> RPG -> 4. Tự Động Tạo Pickup UI & Notification Feed Trên Canvas` tự động dựng prefab dòng thông báo `PickupNotificationItem.prefab` và gắn sẵn toàn bộ các UI này lên Game Canvas.

- [ ] **RPG-04: Giao diện Túi Đồ 2 Cửa Sổ (Dual-Pane Inventory Screen - Phím Tab / I)**
  - Bật/tắt bằng phím `Tab` hoặc `I`, tự động giải phóng con trỏ chuột (`Cursor.lockState = None`) và ngắt tạm thời điều khiển di chuyển/tấn công.
  - Tận dụng bộ sprite UI có sẵn trong `Assets/UI RPG` (`Panel_WithBorder`, `Slot_WithBorder_Base`, `Button_Base`...).
  - **Cửa sổ Trái (Character & Stats):**
    - Khung giữa: Model nhân vật 3D thời gian thực qua Camera Sub-Preview + `RenderTexture` (cho phép xoay ngắm bằng chuột).
    - Ô trang bị: Ô Vũ khí (Weapon Slot) & Ô Giáp (Armor Slot) hiển thị trang bị đang mặc.
    - Bảng chỉ số người chơi bên dưới: Sát thương (ATK), Máu (HP), Thể lực (Stamina), Phòng thủ (DEF).
    - Nút góc dưới phải: Mở Modal **Cộng Điểm Tiềm Năng** (có badge nhấp nháy khi còn điểm chưa cộng).
  - **Cửa sổ Phải (Túi đồ & Tương tác):**
    - Thanh Tab phân loại bên trên: `Tất cả` | `Vũ khí` | `Giáp` | `Vật phẩm (Thuốc & NL)`.
    - Lưới ô chứa đồ (Inventory Grid Slots): Hiển thị icon item, viền màu rarity, số lượng stack (`x5`).
    - Thanh hiển thị Vàng ở cạnh dưới cùng.
    - Cửa sổ/Popup Thông tin Vật phẩm khi click vào 1 slot: Tên, Icon lớn, Mô tả, Stats cộng thêm + 2 nút: **[Trang bị / Sử dụng]** và **[Bỏ (Drop)]**.

- [ ] **RPG-05: Cấp độ, Kinh nghiệm & Cộng Điểm Tiềm Năng (`PlayerProgression.cs`)**
  - Tiêu diệt quái vật nhận EXP -> Cập nhật thanh EXP trên HUD (`ExpBar_Fill.png`).
  - Đủ EXP -> Lên cấp! Thưởng 2–3 Điểm Tiềm Năng (Attribute Points).
  - Modal Phân phối Điểm Tiềm Năng (mở từ nút cửa sổ trái):
    - Các hàng: `Sức Tấn Công (ATK)`, `Máu Tối Đa (Max HP)`, `Thể Lực (Max Stamina)`.
    - Nút `+` và `-` tạm thời để tính toán trước chỉ số (Preview).
    - Nút "Xác Nhận": Chính thức trừ điểm tiềm năng và áp dụng vĩnh viễn vào `Health.cs`, `PlayerStats.cs`, `PlayerCombat.cs`.

- [ ] **RPG-06: Thợ Rèn (Blacksmith) & Cường Hóa Trang Bị tại Làng**
  - Tương tác với NPC Thợ Rèn tại `LandScene` (bấm phím F mở UI Rèn).
  - Công thức cường hóa: Vũ khí hiện tại + [X] Vàng + [Y] Đá cường hóa kiếm -> Nâng cấp bậc kiếm (+1, +2...), tăng mạnh sát thương chém và đổi màu hiệu ứng vung kiếm.

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
