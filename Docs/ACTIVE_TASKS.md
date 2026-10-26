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

- [x] **PCG-03: Hệ thống sinh Thùng Gỗ Đập Được & Rương Báu (Interactive Chests & Breakable Barrels)**
  - Tự động nạp các Prefabs từ Assets/Prefabs/RPG/ (Breakable_Barrel_Small/Medium/Large, Interactive_Chest, Interactive_Chest_Gold).
  - **Khai thác Template Sockets:** Nhận diện toàn bộ các socket user gắn sẵn ([Loot_Barrel_Socket], [Loot_Chest_Socket]), roll xúc xắc theo tỉ lệ Inspector và sinh thùng/rương chính xác tại tọa độ socket.
  - **Phân bổ Tầng 1 (Sàn chính):**
    + Cụm thùng gỗ rải rác tự do trên toàn bộ mặt sàn (không chỉ ở góc hay tường) theo các dạng: Solo (1 thùng), Pair (cặp 2 thùng), Trio (tam giác 3 thùng).
    + Rương báu xuất hiện theo tỉ lệ hoặc cố định 100% trong phòng Kho báu (Treasure Room) với tỉ lệ rương vàng cao.
  - **Phân bổ Tầng 2 (Gác lửng Mezzanine):** Tỉ lệ xuất hiện rương cao ở bức tường sau gác lửng và rải rác các cụm thùng gỗ trên sàn gác lửng.
  - **Phòng Khởi Đầu & Phòng Tiếp Giáp:** Phòng khởi đầu tĩnh (pre-built) giữ nguyên an toàn; phòng đầu tiên nối với sảnh lối vào được trang trí thùng và rương như một phòng bình thường.
  - **Bảo Vệ Tính Tương Tác & NavMesh:**
    + Toàn bộ thùng gỗ đập được và rương báu được đưa vào nhóm riêng Interactables, **TUYỆT ĐỐI KHÔNG GỘP MESH**, giữ nguyên IDamageable, BreakableProp, ChestInteractable, LootDropper và animations.
    + Gắn NavMeshModifier(area = 1 - Not Walkable) và mở rộng layerMask của NavMeshSurface gồm cả Layer 8 (Damageable) và Layer 7 (Obstacle), đảm bảo NavMesh bake quanh thùng/rương, quái không đi xuyên qua.
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
    - **Tối ưu Material & Visual Feedback bằng `MaterialPropertyBlock`:**
      + Nháy trắng đanh thép (`solidWhiteFlash = true`, override `Texture2D.whiteTexture` và kích hoạt phát quang `_EMISSION` với `emissionIntensity = 2.5f`) không clone Material, bảo toàn 100% SRP Batcher & GPU Instancing.
      + **Quy trình Đòn Kết Liễu (Lethal Hit Flash & Death Routine):** Khi quái cạn máu, thực thi `DeathRoutine`: Quái lập tức nháy trắng toàn thân và giữ màu trắng xuyên suốt thời gian hitlag kết liễu (`WaitForSecondsRealtime(lethalFlashDuration)`). Sau khi hết thời gian nháy, quái trả về màu và texture bình thường rồi mới kích hoạt animation ngã chết (`animator.SetTrigger("Die")`).
      + **Hệ thống Hitlag Linh hoạt (Combat Hitlag System):** Tích hợp hitlag kết liễu (`lethalHitlag = 0.18s`), tự động ưu tiên đòn kết liễu khi chém trúng nhiều quái; đồng thời đưa toàn bộ giá trị hitlag của các đòn đánh (`attack1Hitlag`, `attack2Hitlag`, `attack3Hitlag`, `chargeHit1Hitlag`, `chargeHit2Hitlag`, `counterAttackHitlag`, `lethalHitlag`) ra Inspector [`PlayerCombat.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerCombat.cs) để tinh chỉnh trực tiếp.
      + **Hệ thống Sát thương Poise Chuẩn Souls-like (Poise Damage Tuning):** Tách biệt hoàn toàn Poise damage khỏi knockback distance; cấu hình chi tiết `attack1PoiseDamage = 25`, `attack2PoiseDamage = 25`, `attack3PoiseDamage = 55`, `chargeHit2PoiseDamage = 80`, `counterAttackPoiseDamage = 120`. Đồng bộ `maxPoise` quái cận chiến = 100 (đứng vững 2 hit đầu, vỡ thế ở hit 3), quái tầm xa = 50 (đứng vững hit 1, vỡ thế ở hit 2).
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
  - `LootDropper.cs`: Tích hợp trên Quái vật (`SkeletonWarrior`, `SkeletonMinion`, `SkeletonRogue`, `SkeletonMage`), Thùng gỗ và Rương. Thuật toán đa lớp chống rơi lọt vào tường (quét cản 8 hướng, quạt góc phía trước cho Rương, nắn tọa độ lên NavMesh) và triệt tiêu lỗi lơ lửng (loại trừ collider nhân vật/quái/rương, đo chuẩn cao độ mặt sàn +0.12m).
  - Tinh chỉnh hào quang độ hiếm (`WorldItemPickup.cs`): Thu nhỏ bán kính PointLight (`lightRange = 1.4f`, `lightIntensity = 1.5f`) giúp ánh sáng tập trung quanh vật phẩm, không bị chói lóa.
  - Tích hợp 2 MenuItem tự động hóa trong Unity Editor:
    * `Infinite Labyrinth -> RPG -> 2. Tạo Prefab Rơi Đồ (WorldItemPickup Prefab)`
    * `Infinite Labyrinth -> RPG -> 3. Cấu Hình Tự Động Rơi Đồ (Setup Props & Monsters Loot)` để tự động cấu hình 100% Loot và 4 cấp độ Vàng cho toàn bộ Thùng, Rương và Quái vật.

- [x] **RPG-03: Tương tác Nhặt đồ (Pickup Prompt UI & Feed Thông Báo Mép Trái)**
  - `PickupPromptUI.cs`: Khi Player bước vào phạm vi nhặt (2m) của bất kỳ item nào trên sàn, hiển thị bảng nổi ở cạnh dưới tâm màn hình gồm: Icon 2D, Tên vật phẩm đổi màu theo phẩm cấp Rarity (`Common` trắng, `Rare` lục, `Epic` tím, `Legendary` cam), và phím bấm `[F] Nhặt (xSố lượng)`. Hiệu ứng mờ dần mượt mà (Fade In/Out qua CanvasGroup).
  - Cơ chế hút Vàng tự động (Magnetic Auto-pickup): Vàng tự động bay về phía ngực Player và cộng trực tiếp vào túi khi lại gần trong bán kính 4.2m (không cần bấm F).
  - `PickupNotificationFeed.cs` & `PickupNotificationItem.cs`: Danh sách thông báo xếp tầng góc mép trái màn hình. Khi nhặt vật phẩm / nhận vàng, dòng thông báo trượt mượt mà (Slide In từ trái qua), giữ hiển thị trong 2.5s rồi trôi nhẹ lên trên và mờ dần (Slide Up + Fade Out). Tự động dọn dẹp hàng đợi tối đa 5 thông báo.
  - Tích hợp MenuItem tự động hóa: `Infinite Labyrinth -> RPG -> 4. Tự Động Tạo Pickup UI & Notification Feed Trên Canvas` tự động dựng prefab dòng thông báo `PickupNotificationItem.prefab` và gắn sẵn toàn bộ các UI này lên Game Canvas.

- [x] **RPG-04: Giao diện Túi Đồ 2 Cửa Sổ (Dual-Pane Inventory Screen - Phím Tab)**
  - Đóng / mở nhanh bằng phím `Tab` hoặc phím `Esc`, tự động giải phóng con trỏ chuột (`Cursor.lockState = None`), tạm dừng di chuyển người chơi và đóng băng xoay camera / combat trong lúc mở túi.
  - Tối ưu hóa tránh xung đột: Khắc phục triệt để lỗi tự động khóa lại chuột trong `ThirdPersonCamera.cs` khi click giao diện UI, bổ sung hàm `EnsureEventSystem()` tự động tạo `EventSystem` + `InputSystemUIInputModule` chuẩn New Input System nếu scene bị thiếu.
  - **Cửa sổ Trái (LeftPane - Character & Stats):**
    - `CharacterPreviewController.cs`: Soi nhân vật 3D thời gian thực qua Camera Sub-Preview + `RenderTexture` (512x512). Camera chuyên dụng chỉ soi **Layer 3: Player** (tường, quái và hầm ngục hoàn toàn tàng hình), có đèn định hướng dịu đi kèm. Hỗ trợ **nhấp giữ chuột trái và kéo ngang (Drag) trên khung hình để xoay nhân vật 360 độ**. Tự động tắt camera khi đóng túi để tiết kiệm 100% GPU khi chơi.
    - 2 Ô trang bị chuyên dụng (`EquipmentSlotUI.cs`): Ô Vũ khí (`WeaponSlot`) & Ô Giáp (`ArmorSlot`). Tự động hiển thị placeholder khi rỗng, đổi icon và tô màu nền `RarityBG` theo phẩm cấp món đồ khi trang bị.
    - Bảng chỉ số thời gian thực (`PlayerStatsPanelUI.cs`): Tự động tính toán và hiển thị Tấn công (ATK + Bonus), Phòng thủ (DEF), Máu tối đa (Max HP + Bonus), Thể lực tối đa (Stamina + Bonus). Cập nhật tức thì khi thay đổi trang bị.
    - Nút `BtnAttributePoints` ("Cộng Điểm Tiềm Năng") chuẩn bị sẵn sàng kết nối sang Task RPG-05.
  - **Cửa sổ Phải (RightPane - Túi đồ & Tương tác):**
    - Thanh Tab phân loại 4 danh mục: `BtnAll` (Tất cả), `BtnWeapon` (Vũ khí), `BtnArmor` (Giáp), `BtnItem` (Vật phẩm tiêu hao & nguyên liệu). Tự động lọc lưới 30 ô và bật/tắt viền góc `SelectHighlight` tương ứng, tách biệt hoàn toàn không đè màu `Color Tint` của Button.
    - Lưới 30 ô túi đồ (`InventorySlotUI.cs`): Hiển thị icon vật phẩm (tự động ẩn an toàn nếu chưa có sprite), nền trắng `RarityBG` tô màu theo phẩm cấp Rarity, số lượng dồn stack (`xCount`), viền trang trí góc `SelectHighlight` khi được chọn.
    - Hiển thị tiền vàng ở cạnh dưới (`NumberText` định dạng số có dấu phẩy phân cách).
    - **Bảng Chi Tiết Vật Phẩm Động (`ItemDetailPopupUI.cs`):**
      - Tự động định vị bám theo ô đồ vừa click (`PositionNearSlot`): Mặc định góc trên-trái của Popup nằm đúng tâm ô đồ; nếu ô ở hàng dưới cùng thì tự động lật thành góc dưới-trái để trồi lên trên; nếu ô ở sát mép phải thì tự động mở sang trái, đảm bảo 100% không bao giờ bị tràn khỏi màn hình.
      - Click lại chính ô đang chọn -> tự động đóng Popup (Toggle thông minh).
      - Tách riêng Text độ hiếm `ItemRarity` (tô màu theo phẩm cấp) và phân loại `ItemType`.
      - Cụm 4 nút hành động: `BtnEquip` và `BtnUnequip` xếp đè cùng 1 vị trí (Trang bị hiện khi xem đồ trong túi, Tháo ra hiện khi xem đồ đang mặc trên người); `BtnUse` cho bình máu; `BtnDrop` vứt đồ ra sàn đất.

- [x] **RPG-05: Cấp độ, Kinh nghiệm & Cộng Điểm Tiềm Năng (`PlayerProgression.cs`, `AttributeAllocationUI.cs`)**
  - **Kiến trúc Cốt Lõi `PlayerProgression.cs` (Singleton / DontDestroyOnLoad):**
    - Quản lý Level (mặc định 1, max 50), Kinh nghiệm EXP hiện tại và EXP cần lên cấp theo công thức tăng trưởng mượt mà: `baseExp * (level ^ expGrowthExponent)`.
    - Thưởng Điểm tiềm năng (mặc định 3 điểm / cấp).
    - Cấu hình chỉ số cộng thêm mỗi điểm: `attackPerPoint = 2f`, `healthPerPoint = 15f`, `staminaPerPoint = 10f`.
    - Tự động đồng bộ và áp dụng chỉ số trực tiếp vào `Health.cs` (`BaseMaxHealth` + `SetBonusMaxHealth`), `PlayerStats.cs` (`BaseMaxStamina` + `SetBonusMaxStamina`) và `PlayerCombat.cs` (`TotalAttackBonus` cho toàn bộ combo chém, charge attack, counter attack).
    - Quái vật chết (`EnemyAIBase.cs`) tự động thưởng EXP cho Player (`expReward = 25` EXP).
  - **Thanh EXP Dạng Vòng Tròn Chữ C (`ExpRingFill` trên PlayerHUD):**
    - Ánh xạ chính xác tỷ lệ EXP từ 0% đến 100% tương ứng dải `0.19f - 0.58f` trong `Image.fillAmount` theo đúng sprite chữ C đã test.
    - Chạy mượt mà qua nội suy `Mathf.MoveTowards`.
    - Tự động hiển thị và cập nhật cấp độ lên `LevelText` trên Avatar Badge.
  - **Giao Diện Phân Bổ Điểm Tiềm Năng (`AttributeAllocationUI.cs`):**
    - Mở / đóng mượt mà từ nút `BtnAttributePoints` ("Cộng Điểm") ở cửa sổ trái của Inventory Screen (hoặc phím `Esc`). Nút tự động hiển thị số điểm khả dụng dạng huy hiệu `(+X)`.
    - 3 hàng thuộc tính: `Sức Tấn Công (ATK)`, `Máu Tối Đa (HP)`, `Thể Lực (Stamina)` gồm Icon placeholder, giá trị hiện tại và xem trước trực tiếp khi bấm `[+]` và `[-]` (`15 -> 18 (+3)`).
    - Nút "Xác Nhận": Chính thức trừ điểm khả dụng và áp dụng vĩnh viễn vào nhân vật.
    - Nút "Hủy Bỏ": Hoàn trả các điểm vừa chọn thử.
    - **Nút Tẩy Điểm (Reset Stats):** Tiêu tốn X Vàng (mặc định `100 Vàng`, tùy chỉnh trong Inspector). Kiểm tra điều kiện vàng và điểm đã cộng: nếu đủ vàng thì trừ vàng, hoàn lại 100% điểm tiềm năng và xóa sạch chỉ số cộng thêm; nếu không đủ vàng hiển thị cảnh báo đỏ và khóa nút.
  - **Công Cụ Tự Động Hóa Unity Editor (`ProgressionSetupEditor.cs`):**
    - Thêm MenuItem: `Infinite Labyrinth -> RPG -> 5. Tự Động Thiết Lập EXP Ring & Bảng Điểm Tiềm Năng Trên Canvas` giúp tự động dựng trọn gói cây UI Modal trên Canvas, liên kết ExpRingFill và LevelText trên PlayerHUD chỉ với 1 click chuột.

- [x] **SYS-01: Hệ Thống Tương Tác Đa Năng Universal Interaction System (`IInteractable`, `PlayerInteraction`, `InteractPromptUI`)**
  - **Interface `IInteractable` & `InteractableRegistry`:** Chuẩn hóa toàn bộ đối tượng tương tác trong game. Hỗ trợ tùy chỉnh câu lệnh gợi ý `InteractionPrompt`, bật/tắt `RequiresHold` (Hold hoặc Tap), thời lượng giữ `HoldDuration` và bán kính `InteractionRadius`.
  - **Component `PlayerInteraction` (trên Player):** Quét đối tượng tương tác gần nhất trước mặt theo góc nón và cự ly; tự động hủy khi mở Túi đồ (`InventoryUIController.IsOpen`); xử lý bấm Tap (F) hoặc đè Hold (F) mượt mà với New Input System. Tự động kiểm tra và thêm vào Player nếu thiếu.
  - **Giao diện `InteractPromptUI` (trên Canvas `InteractUI`):** Hiển thị câu lệnh hành động và điều khiển thanh `HoldFill` (Image Type: Filled, Method: Vertical, Origin: Bottom) dâng từ dưới lên khi đè giữ phím F. Tự động ẩn hiện Fade In/Out qua `CanvasGroup`.
  - **Tái cấu trúc `PortalController.cs`:** Kế thừa `IInteractable`, thời lượng đè phím 2.0s với thanh fill dọc trước khi chuyển cảnh giữa `DungeonScene` và `LandScene`.
  - **Tái cấu trúc `ChestInteractable.cs`:** Kế thừa `IInteractable`, chuyển sang chế độ giữ nhẹ 0.8s (tùy chỉnh được trong Inspector) để mở nắp rương và tung đồ ra đất.
  - **Component `GenericInteractable.cs`:** Thành phần đa năng có sẵn `UnityEvent<GameObject>` sẵn sàng gắn vào bất kỳ NPC, Đòn bẩy, Thợ rèn hay Bia đá nào mà không cần viết code mới.

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
