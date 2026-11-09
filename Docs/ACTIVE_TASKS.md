# Danh Sách Nhiệm Vụ Phát Triển (Active Tasks Roadmap) - Infinite Labyrinth

> [!TIP]
> **Hướng dẫn gọi việc cho AI:** Mỗi khi mở một phiên chat mới, chỉ cần gõ đúng mã nhiệm vụ.  
> _Ví dụ:_ `"Đọc Docs/SYSTEM_ARCHITECTURE.md và Docs/ACTIVE_TASKS.md. Hãy cùng tôi làm Task RPG-07: Hệ thống Cửa hàng Làng Haven (Shop NPC)."`

---

## 📊 Bảng Tổng Quan Tiến Độ Dự Án (Project Progress Dashboard)

| Giai Đoạn | Tên Phân Hệ | Số Lượng Task | Trạng Thái | Tiến Độ (%) | Ghi Chú |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **Giai đoạn 1** | Hoàn Thiện Trải Nghiệm Hầm Ngục (PCG & Combat) | 18 / 18 | ✅ Hoàn thành | **100%** | PCG, Đồ họa, Combat Souls-like, Boss Arena D-02, ARPG Damage & Floating Numbers |
| **Giai đoạn 2** | Vòng Lặp Trò Chơi & Quản Lý Ngày (Core Loop) | 3 / 3 | ✅ Hoàn thành | **100%** | GameManager, Tử trận chuông làng, Thắng tầng qua cổng |
| **Giai đoạn 3** | Kinh Tế, Tiến Trình RPG & Ngôi Làng Haven | 9 / 10 | 🟡 **ĐANG THỰC HIỆN** | **90%** | **Xong Lò Rèn Haven RPG-08; Chuẩn bị làm Ma Trận Rơi Đồ RPG-09** |
| **Giai đoạn 4** | Điểm Nhấn Đồ Án - Cặp Trùm Souls-like & ML-Agents | 5 / 7 | 🟡 Đang hoàn thiện | **71%** | Xong Ice Golem, Red Ignis, ML Ice Golem; Chờ ML Red Ignis |
| **Giai đoạn 5** | Giao Diện Hoàn Chỉnh & Âm Thanh (UI & Polish) | 1 / 3 | ⚪ Chờ thực hiện | **33%** | Xong Bản đồ hầm ngục Fullscreen; Chờ Menu & Audio |
| **Giai đoạn 6** | Công Cụ Kiểm Thử & Quản Trị (Admin Debug) | 5 / 5 | ✅ Hoàn thành | **100%** | Admin Console (phím `~`), Master Dashboard (Ctrl+Alt+D), Teleport, God Mode, Cheats RPG |
| **Giai đoạn 7** | Tối Ưu Hệ Thống Inventory, UI & Vòng Đời Ứng Dụng | 3 / 3 | ✅ Hoàn thành | **100%** | UsableSlot, Drag & Drop, Punch scale, Sửa lỗi Scene Cleanup |

> **🔥 TRỌNG TÂM HIỆN TẠI (CURRENT FOCUS):**  
> 1. **`Task RPG-08`:** Lò rèn Haven & Hệ thống Cường hóa / Chế tạo Thần binh (Blacksmith NPC) - ✅ **Hoàn thành**.  
> 2. **`Task RPG-09`:** Cân bằng Ma trận rơi đồ toàn diện (Drop Tables từ Quái, Rương, Thùng).

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
    + [`DungeonPropDecorator.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.cs): Giữ 100% Inspector Fields, Awake/Update, API `DecorateDungeon(...)` và điều phối chính (437 dòng).
    + [`DungeonPropDecorator.Templates.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Templates.cs): Quản lý Templates tàn tích & tiểu cảnh, tính toán Bounding Box (337 dòng).
    + [`DungeonPropDecorator.Lighting.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Lighting.cs): Hệ thống đuốc sàn, đuốc tường, đuốc hành lang (221 dòng).
    + [`DungeonPropDecorator.Mezzanine.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Mezzanine.cs): Trang trí toàn bộ gác lửng tầng 2 (155 dòng).
    + [`DungeonPropDecorator.Furniture.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Furniture.cs): Đồ nội thất tường, bàn ghế tâm phòng, góc phòng, rải rác sàn (407 dòng).
    + [`DungeonPropDecorator.Spatial.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.Spatial.cs): Thuật toán lưới, cấm đặt ô, tường đặc, họ cờ hiệu (148 dòng).
    + [`DungeonStaticPropCombiner.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonStaticPropCombiner.cs): Utility độc lập gộp Mesh và lọc Collider (79 dòng).
  - Đảm bảo 100% Zero Breaking Changes: Không mất dữ liệu Inspector, không ảnh hưởng `DungeonRenderer` và luồng sinh hầm ngục.

- [x] **FIX-01: Khắc phục quái vật đi xuyên qua Props (Đồng bộ Bake NavMesh sau Props)**
  - Đổi thứ tự trong `DungeonRenderer.BuildMesh`: Sinh và gộp mesh Props hoàn tất trước, sau đó mới tiến hành `BakeAndValidateNavMesh`.
  - Gắn `NavMeshModifier(ignoreFromBuild = true)` lên `areaObj` để NavMeshSurface không bake lên trần hay tường visual.
  - Gắn `NavMeshModifier(overrideArea = true, area = 1 - Not Walkable)` lên toàn bộ `combinedObj` có MeshCollider trong `DungeonStaticPropCombiner.cs`.
  - Mở rộng `navMeshSurface.layerMask = (1 << 10) | (1 << 0)`: Thu thập cả sàn (Layer 10) và props vật cản (Layer 0), tự động đục thủng NavMesh tại footprint của props, quái vật (NavMeshAgent) tự động đi vòng né tránh.

- [x] **FIX-02: Cập nhật đường dẫn Prefabs & Triệt tiêu Warning / NullReferenceException**
  - Đồng bộ việc di chuyển thư mục `Dungeon` từ `Assets/Enviro Prefabs/Dungeon` sang `Assets/Prefabs/Dungeon`.
  - Bổ sung cơ chế `ResolveDungeonFolder` thông minh trong [`DungeonPropDecorator.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonPropDecorator.cs) và [`DungeonRenderer.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonRenderer.cs), tự động phát hiện đường dẫn mới và hỗ trợ fallback.
  - Triệt tiêu 6 warnings "Folder not found" và lỗi `NullReferenceException` của Unity UIElements ListView khi render Inspector.

- [x] **FIX-03: Khắc phục phòng đầu tiên sinh quá sát Phòng Khởi Đầu (Vùng đệm an toàn Start Room Min Gap)**
  - Bảo vệ từ gốc phân chia không gian BSP: Đưa `startRoomMinGapTiles` vào thiết lập `minDungeonY = Mathf.Max(startRoomHeight, doorGridY) + Mathf.Max(2, startRoomMinGapTiles)` cho `mainDungeonRoot`. Tuyệt đối không một phòng nào trong toàn bộ hầm ngục có thể sinh trong vùng đệm an toàn trước cửa phòng khởi đầu.
  - Đồng bộ liên kết phòng: Lưu `data.startRoomBounds = closestRoom` trong `ConnectStartRoom` và cập nhật `DungeonRoomAnalyzer` ưu tiên gán `data.startRoom` theo `data.startRoomBounds`, đảm bảo phòng kết nối thực tế đồng bộ 100% với phòng bắt đầu của BFS đồ thị và Mini-map.
  - Hành lang dẫn lối an toàn: Tự động dẫn đoạn hành lang thẳng tối thiểu ra khỏi cửa (`minTurnY = gridDoorPos.y + corridorThickness + 1`) trước khi rẽ Z, loại bỏ hoàn toàn nguy cơ tường hành lang gãy góc đè lên ngàm nối cửa khởi đầu.

- [x] **PCG-03: Hệ thống sinh Thùng Gỗ Đập Được & Rương Báu (Interactive Chests & Breakable Barrels)**
  - Tự động nạp các Prefabs từ `Assets/Prefabs/RPG/` (`Breakable_Barrel_Small/Medium/Large`, `Interactive_Chest`, `Interactive_Chest_Gold`).
  - Khai thác Template Sockets: Nhận diện toàn bộ các socket user gắn sẵn (`[Loot_Barrel_Socket]`, `[Loot_Chest_Socket]`), roll xúc xắc theo tỉ lệ Inspector và sinh thùng/rương chính xác tại tọa độ socket.
  - Phân bổ Tầng 1 (Sàn chính): Cụm thùng rải rác tự do (Solo, Pair, Trio), Rương xuất hiện theo tỉ lệ hoặc cố định trong phòng Kho báu (Treasure Room).
  - Phân bổ Tầng 2 (Gác lửng Mezzanine): Tỉ lệ xuất hiện rương cao ở bức tường sau gác lửng và rải rác các cụm thùng gỗ trên sàn gác lửng.
  - Đưa toàn bộ thùng gỗ và rương báu vào nhóm riêng `Interactables`, **TUYỆT ĐỐI KHÔNG GỘP MESH**, giữ nguyên `IDamageable`, `BreakableProp`, `ChestInteractable`, `LootDropper` và animations.
  - Gắn `NavMeshModifier(area = 1 - Not Walkable)` và mở rộng `layerMask` của `NavMeshSurface` gồm cả Layer 8 (Damageable) và Layer 7 (Obstacle), đảm bảo NavMesh bake quanh thùng/rương, quái không đi xuyên qua.

- [x] **OPT-01: Tối ưu hóa hiệu năng URP & Hệ thống Ánh sáng (Phục hồi 40 FPS -> 110+ FPS)**
  - Triệt tiêu Shadow Caster Pass của Directional Light cường độ 0 (giải phóng 4 cascades 2048x2048 vô ích, tiết kiệm 4-6ms GPU).
  - Viết lại `DungeonLightOptimizer.cs`: Culling kép cả `Light` lẫn `ParticleSystem` (khói & lửa của đuốc) khi ở xa, giải phóng hơn 160 emitter hạt.
  - Bổ sung cơ chế **Hysteresis Buffer (đệm 6m)** chống nhấp nháy đèn ở ranh giới.
  - Mở rộng thanh trượt `Light Culling Distance` (10m - 150m, mặc định 80m) và đồng bộ `Camera Far Clip Plane` (200m) phù hợp hoàn hảo với hầm ngục quy mô lớn 300m x 322m.

- [x] **OPT-02: Tối ưu hóa hiệu năng LandScene (Ngôi Làng Haven Village Hub - Nâng FPS từ ~65 lên 110+ FPS)**
  - Tối ưu hóa Địa hình: Kích hoạt GPU Instancing cho bề mặt địa hình (`drawInstanced = true`), cắt giảm cự ly render cây cối xuống `treeDistance = 200m` và billboard xuống 120m, nâng `heightmapPixelError = 10`, tắt `shadowCastingMode = Off` cho Terrain.
  - Mặt Nước & Post Processing: Tắt Cast Shadows cho mặt nước, dọn GameObject Bloom trùng lặp, tắt `highQualityFiltering` trên URP Bloom.
  - Bộ Công Cụ Tự Động Hóa 1-Click (`LandSceneOptimizerEditor.cs`): Gộp 94 viên sỏi rời rạc thành 1 Mesh 32-bit duy nhất (`Gravel_Combined_Mesh.asset`), giảm 93 Draw Calls; Đánh dấu Static cho 214 đối tượng làng; Tắt bóng đổ cho đạo cụ nhỏ; Tối ưu hóa toàn diện 61 NPC (`NpcPatrol.cs`: `CullUpdateTransforms`, `LowQualityObstacleAvoidance`, Distance LOD Hibernation 40m-48m, Time-slicing 0.35s, bộ nhớ đệm sharedWaypoints).
  - Tích hợp Tự Phục Hồi Admin Console (`AdminCheatConsole Self-Healing`): Tự động gọi `EnsureInstance()`, hỗ trợ phím nóng dự phòng `~`, `F1`, `F2`, chống bấm đúp cùng frame, thiết lập tọa độ dịch chuyển riêng cho LandScene.

- [x] **UI-01: Cập nhật `PlayerHUD` hiển thị đầy đủ 3 thanh tài nguyên Souls-like**
  - Thanh Máu (Health Bar): MainFill màu đỏ tươi kèm Ghost Bar màu vàng nhạt tụt đuổi từ từ sau thời gian trễ khi dính đòn.
  - Thanh Thể lực (Stamina Bar): Màu vàng/cam, co giãn mượt mà khi Lăn né (Roll), Chạy nhanh (Sprint), Chém kiếm (Attack).
  - Cụm Sao Năng lượng Chiêu thức (Spirit Stars): 4 ngôi sao nạp đầy khi chém trúng địch / parry, tiêu hao khi dùng kỹ năng E, Q, R.

- [x] **UI-02: Hệ Thống Thanh Máu Quái Vật World Space Souls-like (`EnemyHealthBarUI.cs`)**
  - Billboard 3D Thông Minh: Tự động xoay mặt vuông góc 100% về phía Camera người chơi, phẳng phiu không méo lệch.
  - Khai thác Sprite Grayscale & Hiệu Ứng Souls-like: `Monster_Border.png` và `Monster_Fill.png`, MainFill tô đỏ tươi qua `Image.color`, Ghost Bar vàng nhạt tụt đuổi sau 0.45s trễ.
  - Tối Ưu Hóa Hiệu Năng: Tắt Canvas khi quái ở xa hoặc ngủ đông (0 Draw Calls), ẩn khi 100% HP, Fade In khi dính đòn, Fade Out khi tử trận, tắt 100% `GraphicRaycaster`.
  - Tool 1-Click: `EnemyHealthBarSetupEditor.cs` tự động tạo `EnemyHealthBar.prefab` và gắn vào 4 quái Skeleton.

- [x] **UI-03: Màn hình Loading Screen chuyển cảnh Async & Hầm ngục PCG (Task LD-01 - LD-09)**
  - LD-01: `LoadingScreenData.cs` lưu trữ 21 mẹo tiếng Anh theo 5 nhóm chủ đề, mảng ảnh nền hầm ngục/làng, tự động nạp ảnh render.
  - LD-02: Prefab `LoadingScreenCanvas.prefab` & controller `LoadingScreenUI.cs` điều khiển Full-screen RawImage, Vignette, Header địa danh, Tip content, thanh tiến trình Souls-like và spinner.
  - LD-03: Singleton DontDestroyOnLoad `LoadingScreenManager.cs`, phân chia 2 pha nạp (Pha 1 nạp Scene 0-40%, Pha 2 PCG 40-100%), thời gian tối thiểu `minLoadingTime = 1.6s`.\n  - LD-04: Nâng cấp `DungeonManager.cs` và `DungeonRenderer.cs` sinh PCG theo từng chặng đa khung hình (`BuildMeshRoutine`, `GenerateWorldRoutine`).
  - LD-05: Tích hợp `PortalController.cs` nạp bất đồng bộ 2 chiều Làng <-> Hầm ngục.
  - LD-06: Chạy Loading Screen trực tiếp trong DungeonScene khi Cold Start, tích hợp Admin Console và Custom Inspector `LoadingScreenUIEditor.cs`.
  - LD-07 & LD-08 & LD-09: Hoạt ảnh Sprite Sheet 192 frame bằng `Time.unscaledDeltaTime`, tối ưu Time-Slicing Coroutine trang trí Props và sinh quái, component đa năng `UISpriteSheetAnimator.cs` và 2 prefabs UI/World Space.

- [x] **UI-04: Toggle Switch chuyển đổi xem trước 3D / 2D trong Inventory (`CharacterPreviewToggleUI.cs`)**
  - Giao diện Toggle Switch [ 3D | 2D ] với con trỏ trượt mượt mà `SlidingHandle` (SmoothStep 0.15s), đổi màu vàng kim/xám mờ, lưu `PlayerPrefs`.
  - Tối ưu hóa GPU: Chế độ 2D tắt hoàn toàn `previewCamera` và `previewLight` (0% chi phí render buồng soi 3D), kích hoạt `Character2DBox` chạy hoạt ảnh meme 190 frame. Chế độ 3D bật lại camera soi nhân vật.
  - Tích hợp 1-Click Editor `CharacterPreviewToggleEditor.cs`.

- [x] **D-01: Bộ sinh quái tự động cho hầm ngục theo Biệt Đội / Cụm (`ProceduralEnemySpawner.cs`)**
  - Tổ hợp Biệt Đội chuẩn RPG (Nhóm 4 quái, Nhóm 3 quái, Nhóm 2 quái, Nhóm 1 quái lẻ) kết hợp Spatial Clustering (Neo bán kính 3m, cách nhau >= 8.5m, bám sàn NavMesh).
  - Phân bổ theo quy mô phòng: Sàn trệt phòng nhỏ/vừa 1 nhóm, gác lửng tầng 2 1 nhóm, đại sảnh lớn 1-2 nhóm, kho báu 1 nhóm nhỏ, phòng đầu tiên vào hầm ngục có quái ngay, chỉ chừa trống Boss Room.
  - Tối ưu hóa quái vật hầm ngục (`DungeonEnemyOptimizer.cs`): Cơ chế 3 vùng cự ly (Thức tỉnh < 40m, Đệm 40-48m, Ngủ đông > 48m ngắt Animator, Renderers, NavMesh, Shadows); Nháy trắng GPU qua `MaterialPropertyBlock`; Chuỗi tử trận lethal hit sequence; Time-slicing 4 batch; Thức tỉnh tức thì khi dính đòn tầm xa.

- [x] **VFX-01: Hệ thống Hit VFX Phản Hồi Đòn Đánh Cho Player & Quái Vật (`MobsGetHitVFX` & `GetHitVFX`)**
  - Quái xương dính đòn sinh `MobsGetHitVFX.prefab` (vụn xương xám) tại `hitPoint`, tứa ngược chiều chém. Player dính đòn sinh `GetHitVFX.prefab` (máu đỏ). Parry thành công không tứa máu.
  - Bảo toàn phát sinh Hit VFX cả trong đòn kết liễu hạ gục quái. Tool 1-Click `CombatVfxSetupEditor.cs`.

- [x] **VFX-02: Hệ thống Sword Slash Trail & Arc VFX Cho Player (`PlayerSlashVFX` & `Slash VFX.prefab`)**
  - Kiến trúc Decoupled Arc Slash độc lập theo 6 đòn đánh cơ bản (Attack 1-3, Charge 1-2, Counter) và 2 kỹ năng đặc biệt (E sóng kiếm, Q nổ đất).
  - Tách rời Hit Impact khỏi Slash Trail: `WeaponHitbox.cs` chỉ kích nổ `Impact VFX` tại đúng điểm tiếp xúc va chạm khi chém trúng quái vật có HP.
  - Hiệu ứng gạt đòn Parry Clashing Spark: Nổ tia lửa chính xác tại tâm giữa lưỡi kiếm (`swordTransform.TransformPoint(box.center)`), kết hợp Hitlag 0.2s, zoom camera và choáng kẻ tấn công.
  - Custom Inspector `PlayerSlashVFXEditor` với đầy đủ nút test trực quan và gizmos Scene View.

- [x] **SKILL-01: Tuyệt Kỹ Tối Thượng Phím R (Ultimate Colossal Slash & Impact System)**
  - Tốn 200 Spirit (4 sao), hồi chiêu 12s, Hyper Armor & Bất tử trong chuỗi thi triển.
  - Chuỗi điện ảnh 4 pha: Tụ lực gồng kiếm (0-1.48s) -> Đỉnh điểm chớp sáng + Freeze frame 0.1s + Camera zoom punch (1.48s) -> Bung chém ngang cực đại kèm Slow-motion 0.95s, Gigantic Cleave Hitbox bán kính 5.5m quét quạt 240 độ (1.62s) -> Thu chiêu hồi vị (2.55s).
  - Khắc phục triệt để hiện tượng lệch `Time.timeScale` bằng `ActiveSlowMoScale`, đa dạng hóa phím tắt chỉnh tốc độ `T`, `[`, `]`, `\\`, `0`, giữ `G` slow-mo.

- [x] **UI-SKILL-01: Giao Diện Kỹ Năng & Hồi Chiêu (`SkillIconUI` Cooldown & Spirit Feedback)**
  - Quản lý 3 ô kỹ năng E, Q, R trên HUD với ma trận 2 tầng: Corner viền góc sáng khi đủ Spirit; CDFill quét đếm ngược; Icon alpha mờ/sáng báo trạng thái sẵn sàng.

- [x] **COMBAT-HITLAG-01 & COMBAT-SLOWMO-01: Hệ Thống Hitlag Va Chạm & Bảo Vệ Nhịp Slow-Motion**
  - Bổ sung Hitlag chuẩn game hành động cho bộ 3 chiêu: E (0.18s), Q (0.20s), R (0.22s).
  - Bảo vệ nhịp Slow-mo Skill Ult không bị đè bởi Hitlag/đòn kết liễu: Đóng băng bộ đếm slow-mo khi khựng và tự động khôi phục về tốc độ slow-mo thay vì ép về 1.0x.

- [x] **COMBAT-02: Nâng Cấp Khóa Mục Tiêu Chuẩn Souls-like & Bộ Lọc Cao Độ Tầng 2 (`TargetLockSystem.cs`)**
  - Bộ lọc chênh lệch cao độ tầng 2 (`maxLockElevationDifference = 4.2m`): Triệt tiêu lỗi camera ngửa 65 độ lên gác lửng tầng 2 khi quái tầng 1 chết.
  - Chuyển mục tiêu bằng vẩy chuột dứt khoát (`mouseFlickThreshold = 20`, Screen Viewport Space), hồi chiêu `switchCooldown = 0.25s`, lia cam mượt mà qua `ThirdPersonCamera.cs`.

- [x] **COMBAT-DMG-01: Chuẩn Hóa Công Thức Phòng Thủ ARPG & Scale Sát Thương Skill E, Q, Charge Attack**
  - Nâng cấp `Health.cs` sang công thức giảm trừ % ARPG chuẩn: $\text{Damage Nhận} = \text{Damage Gốc} \times \frac{100}{100 + \text{DEF}}$ (khắc phục lỗi trừ phẳng flat gây bất tử hoặc one-hit).
  - Đồng bộ chỉ số Defense cho Quái thường (`EnemyAIBase.cs`: Warrior 15, Rogue 10, Mage 5) và Boss (`IceGolemController`: 35 DEF, `RedIgnisController`: 25 DEF).
  - Scale sát thương kỹ năng theo ATK nhân vật: Skill E ($+1.4\times\text{ATK}$), Skill Q ($+2.0\times\text{ATK}$), Charge Attack (Base 70 Sát thương).

- [x] **COMBAT-DMG-02: Hệ Thống Số Sát Thương Bay Lên (Floating Damage Numbers / Pop & Physics Arc, Gradient TextMeshPro)**
  - Quản lý Object Pool 40 TextMeshPro 3D Mesh (`DamageNumberManager.cs` & `DamageNumberItem.cs`), Billboard tự động hướng mặt về camera.
  - Quỹ đạo búng nảy vồng cung (Physics Arc): Đòn đánh búng vọt lên cao kèm trọng lực rơi tự do, độ phóng to đanh thép (Pop scale ở 0.10s đầu) và mờ dần (Alpha fade out).
  - Bộ 7 dải màu Gradient TextMeshPro 4 góc chuẩn xác trong `Assets/Resources/Color Gradient Presets/`:
    + Combo 1-3: Trắng bạc ánh khói (`Damage_Normal`)
    + Charge Attack: Vàng kim hổ phách (`Damage_Charge`)
    + Counter Attack: Vàng cam nảy lửa (`Damage_Counter`)
    + Skill E: Lam ngọc nguyệt quang (`Damage_SkillE`)
    + Skill Q: Cam đất nung chấn địa (`Damage_SkillQ`)
    + Skill R: Đỏ Huyết Tinh Crimson uy lực tột đỉnh (`Damage_SkillR`)
    + Quái đánh Player: Đỏ tươi nguy hiểm (`Damage_PlayerHit`), hiển thị số thuần túy không dấu trừ.
  - Bảng tùy chỉnh kích thước (Base Scale, Peak Scale, Duration) và độ dày Outline (`outlineWidth = 0.1f`) trực tiếp trên Inspector với `OnValidate()`.
  - Tool 1-Click Unity Editor tạo Prefab: `Infinite Labyrinth -> Combat -> 3. Tạo Prefab Số Sát Thương (DamageNumberItem) (1-Click)`.

- [x] **D-02: Cơ chế Boss Arena Toàn Diện tại Boss Room (D02-1 -> D02-5)**
  - [x] **D02-1:** Sinh phòng Boss khổng lồ (676-900 tiles, vòm trần 24m), Đại Lộ Hành Lang 40m uy nghi rộng 12m, 4 Cột Trụ đá chịu lực khổng lồ né đòn, hệ thống 3 Tầng Ánh Sáng Souls-like (Giếng trời ánh trăng, 4 Vạc lửa rực cháy chân cột, Hào quang thân Boss, 2 hàng đuốc Đại Lộ).
  - [x] **D02-2:** Phòng Tế Đàn nhánh cụt (`wallset_shrine`), Chìa Khóa Hầm Ngục (`DungeonKeyPickup.prefab`) phát quang Legendary xoay lơ lửng, nhặt F lưu vào `InventoryManager`.
  - [x] **D02-3:** Đại Môn Phong Ấn scale 2.5x yêu cầu Chìa Khóa (giữ F 2.5s mở), Cổng Sương Mù (Fog Gate Lock-in) khóa chặt đấu trường khi bước vào.
  - [x] **D02-4:** Đạo diễn Đấu Trường, sinh Bục Đá cao 12m ở vách Bắc, Boss đứng Standby, Cutscene Intro 5 pha điện ảnh, typography Souls-like bay lên neo thanh máu màu xanh băng; Khắc phục triệt để quỹ đạo JumpDown giáng trần 2 pha lao dốc gia tốc $u^2$ rơi đúng frame 55 (1.80s) kích nổ shockwave; 5 nhịp đập sàn GumoGumo dộng đất; Locomotion mượt mà 4 hướng.
  - [x] **D02-5A:** Hệ thống 3 Cấp Độ Rương (`Interactive_Chest` Xanh lá, `Interactive_Chest_Rare` Tím, `Interactive_Chest_Boss` Vàng kim) & VFX Tia Sáng đa tầng khi mở nắp.
  - [x] **D02-5B:** Đạo diễn Chiến Thắng Boss & Biểu Ngữ hoàng kim Souls-like "VICTORY ACHIEVED" trong `EnemyCanvas`, lia cam 3/4 mạn phải ngắm Boss gục ngã, Victory Fanfare.
  - [x] **D02-5C:** Sinh Rương Boss & Cổng Dịch Chuyển trên Bục Đá 12m sau khi hạ Boss; Lia cam tôn vinh lên đài cao 12m; Cột sáng thần thánh giáng thế; Cơn mưa chiến lợi phẩm quạt hình nón (150-300 Vàng, Đá, Trang bị); Vô hiệu hóa reset episode ML-Agents khi Boss chết; Khóa cứng `applyRootMotion = false` và khóa `localPosition.x/z = 0` chống trôi model so với Hitbox.
  - [x] **D02-5D:** Giao diện Lựa chọn Cổng Dịch Chuyển 2 Cửa Sổ (`VictoryPortalUI`): Bên trái về Làng Haven (Hold 2.0s), Bên phải xuống Tầng 2 (Hold 2.0s), Nút Stay / Phím ESC ở lại nhặt rương tự do.

---

## 🚩 GIAI ĐOẠN 2: Vòng Lặp Trò Chơi & Quản Lý Ngày (Core Game Loop & Day System)

- [x] **GM-01: Quản lý vòng lặp ngày & tầng (`GameManager.cs`, `InfoUIController.cs`)**
  - Singleton `GameManager.cs` (`DontDestroyOnLoad`) quản lý `CurrentDay`, `CurrentFloor`, các events `OnDayChanged`, `OnFloorChanged`, `OnGameStateChanged`.
  - Giao diện thông tin `InfoUIController.cs`: `TopCornerInfo` góc trên-phải (Day N, Floor X, Vị trí hiện tại); Biểu ngữ điện ảnh Souls-like `MidScreenInfo` giới thiệu vùng đất mới khi vừa bước qua cổng (Fade In/Hold 2.2s/Fade Out).
  - Tự động hóa 1-Click Unity Menu và runtime auto-wire.

- [x] **GM-02: Cơ chế Tử trận & Hồi sinh tại Chuông Làng (Death & Respawn Loop)**
  - Màn hình tử trận `DeadScreenUIController.cs`: Lắng nghe `Health.onDeath`, chờ 1.2s hoạt cảnh ngã gục, mở chuột, Fade In biểu ngữ `YOU DIED` và nút `BtnRespawn`.
  - Vòng lặp hồi sinh `GameManager.RespawnAtVillage()`: Tăng `CurrentDay += 1`, đặt lại `CurrentFloor = 1`, nạp `LandScene`, teleport Player về **Chuông Trung Tâm Làng** (`bell`) trên NavMesh an toàn, hồi 100% HP và Thể lực, khôi phục điều khiển và phát biểu ngữ Làng.

- [x] **GM-03: Cơ chế Chiến thắng Tầng & Lựa chọn Đi tiếp / Về Làng (`VictoryPortalUI.cs`, `GameManager.cs`)**
  - Lựa chọn 1 (Trở về Làng): Bảo toàn 100% kho đồ, `AdvanceDay()` (`CurrentDay += 1`), `ResetFloorToTown()` (`CurrentFloor = 1`), nạp `LandScene`.
  - Lựa chọn 2 (Tiến vào Tầng kế): Tính `targetFloor = CurrentFloor + 1`, cập nhật tiến trình `SetFloor(targetFloor)`, tải `DungeonScene` với seed mới và độ sâu mới qua `LoadingScreenManager`.
  - Master 1-Click Menu cấu hình toàn bộ vòng lặp GM-01 đến GM-03.

---

## 🚩 GIAI ĐOẠN 3: Kinh Tế, Tiến Trình RPG & Ngôi Làng Haven (Inventory, Progression, Shop & Blacksmith)

> **Trạng thái phân hệ:** Đã hoàn thiện toàn bộ nền tảng Kho đồ, Rơi đồ, Giao diện 2 cửa sổ, Cấp độ tiềm năng, Tương tác đa năng và Cơ sở dữ liệu 49 Items. Hiện tại đang triển khai cặp đôi trung tâm kinh tế Làng Haven: **Cửa Hàng (Shop)** và **Lò Rèn (Blacksmith)**.

- [x] **RPG-01: Kiến trúc Dữ liệu Vật phẩm & Quản lý Kho đồ (`ItemData.cs`, `InventoryManager.cs`)**
  - ScriptableObject `ItemData`: ID, Tên, Mô tả, Icon, Phân loại, Độ hiếm, MaxStack, Chỉ số cộng thêm (ATK, DEF, HP, Stamina), Model 3D rớt đất, Model cầm tay.
  - Cấu trúc `InventorySlot` lưu trữ stack đồ; Singleton `InventoryManager.cs` quản lý 30 slot đồ, Tiền Vàng, ô trang bị vũ khí/giáp, toàn bộ API Add, Remove, Equip, Unequip, Use, Drop, AddGold, SpendGold.
  - Buff trực tiếp: Vũ khí tăng ATK cho combo trong `PlayerCombat.cs`, Giáp giảm sát thương trong `Health.cs`.

- [x] **RPG-02: Rơi đồ thế giới từ Quái, Thùng gỗ & Rương (`LootDropper`, `BreakableProp`, `ChestInteractable`, `WorldItemPickup`)**
  - `WorldItemPickup.cs`: Thực thể 3D rương mini xoay lơ lửng, hiệu ứng búng nảy parabol Arc Pop, PointLight hào quang đổi màu theo Rarity.
  - Mô hình Vàng phân cấp 4 bậc (`coin`, `coin_stack_small`, `coin_stack_medium`, `coin_stack_large`) theo ngưỡng số lượng; Hút vàng tự động (Magnetic Auto-pickup 4.2m).
  - `BreakableProp.cs` nhận sát thương vỡ thùng văng loot; `ChestInteractable.cs` lật nắp mở tung đồ; `LootDropper.cs` thuật toán đa lớp chống rơi lọt tường và bám sát mặt sàn NavMesh.

- [x] **RPG-03: Tương tác Nhặt đồ (Pickup Prompt UI & Feed Thông Báo Mép Trái)**
  - `PickupPromptUI.cs`: Bảng nổi mép dưới tâm màn hình khi lại gần 2m (Icon, Tên đổi màu Rarity, phím `[F] Nhặt`).
  - `PickupNotificationFeed.cs` & `PickupNotificationItem.cs`: Danh sách thông báo trượt mượt mà (Slide In/Up/Out) mép trái màn hình, lưu giữ 2.5s, hàng đợi tối đa 5 thông báo.

- [x] **RPG-04: Giao diện Túi Đồ 2 Cửa Sổ (Dual-Pane Inventory Screen - Phím Tab)**
  - Mở/đóng bằng `Tab` / `Esc`, khóa chuột/di chuyển/camera, chống xung đột New Input System.
  - Cửa sổ Trái (Character & Stats): 3D Character Preview soi nhân vật xoay 360 độ qua RenderTexture (tắt hoàn toàn camera khi đóng túi để tiết kiệm 100% GPU); 2 Ô trang bị chuyên dụng Weapon & Armor; Bảng chỉ số thời gian thực (ATK, DEF, Max HP, Stamina); Nút mở Modal Điểm tiềm năng.
  - Cửa sổ Phải (Kho đồ & Thao tác): 4 Tab phân loại (All, Weapon, Armor, Item); Lưới 30 ô túi đồ viền đổi màu Rarity; Hiển thị tiền vàng; Bảng chi tiết vật phẩm bám theo ô thông minh tự lật góc chống tràn màn hình kèm 4 nút hành động (Trang bị/Tháo, Dùng, Vứt).
  - Tích hợp đóng Popup khi click ra ngoài (`Click Outside to Close`) và cơ chế Kéo & Thả đồ ARPG (`InventoryDragDropController.cs`).

- [x] **RPG-05: Cấp độ, Kinh nghiệm & Cộng Điểm Tiềm Năng (`PlayerProgression.cs`, `AttributeAllocationUI.cs`)**
  - Singleton `PlayerProgression.cs`: Cấp độ 1-50, EXP mượt mà, thưởng 3 điểm tiềm năng/cấp, cộng chỉ số trực tiếp vào ATK, HP, Stamina.
  - Thanh EXP vòng tròn chữ C (`ExpRingFill`) trên PlayerHUD ánh xạ chuẩn fill `0.19 - 0.58`, hiển thị cấp độ trên huy hiệu.
  - Giao diện modal `AttributeAllocationUI.cs`: Tăng giảm điểm xem trước chỉ số thời gian thực, Nút Tẩy Điểm (Reset Stats) tiêu tốn Vàng.

- [x] **SYS-01: Hệ Thống Tương Tác Đa Năng Universal Interaction System (`IInteractable`, `PlayerInteraction`, `InteractPromptUI`)**
  - Interface `IInteractable` & `InteractableRegistry`: Chuẩn hóa đối tượng tương tác (câu lệnh Prompt, Tap hoặc Hold, thời lượng giữ, bán kính).
  - `PlayerInteraction` trên Player: Quét mục tiêu hình nón, xử lý Tap/Hold New Input System, hủy khi mở túi đồ.
  - `InteractPromptUI`: Thanh `HoldFill` dâng dọc từ dưới lên khi đè giữ phím F. Tái cấu trúc `PortalController` (Hold 2.0s) và `ChestInteractable` (Hold 0.8s).
  - Component `GenericInteractable.cs`: Có sẵn `UnityEvent<GameObject>` sẵn sàng gắn vào bất kỳ NPC Shop, Thợ Rèn, Chuông Làng mà không cần viết lại mã tương tác.

- [x] **SYS-02: Hệ Thống Danh Sách Tương Tác Dọc & Cuộn Chuột Tập Trung (Vertical Interaction List & Mouse Scroll Selection)**
  - Hợp nhất toàn bộ `IInteractable` (Cổng, Rương, Cửa, NPC) và `WorldItemPickup` (Vật phẩm rơi) vào kiến trúc quản lý tập trung qua [`InteractionTarget.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Interaction/InteractionTarget.cs).
  - Gỡ bỏ logic tự bắt phím F riêng lẻ trong từng [`WorldItemPickup.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Inventory/WorldItemPickup.cs) (chống triệt để hiện tượng bấm 1 lần nhặt hết toàn bộ đồ trong tầm).
  - [`PlayerInteraction.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerInteraction.cs) điều hướng danh sách bằng con lăn chuột (`Mouse Scroll Wheel`) hoặc phím Mũi Tên, hỗ trợ cửa sổ trượt (Sliding Window tối đa 5 dòng) mượt mà.
  - Phản hồi Corner kép: Mục đang chọn (Selected) tự động bật 2 Corner (Background Corner & KeyIcon Corner) và sáng rực (`alpha = 1.0f`); các mục còn lại tắt 2 Corner và làm mờ nhẹ (`alpha = 0.6f`).
  - Phím F (Tap hoặc Hold) chỉ tác động duy nhất lên mục đang được chọn. Nhặt xong món đó danh sách tự động co lại và chuyển sang món kế tiếp.
  - Bộ công cụ 1-Click Unity Editor ([`InteractionListSetupEditor.cs`](file:///c:/2.tapcodedienloan/Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/InteractionListSetupEditor.cs)) và cơ chế tự phục hồi Runtime (`InteractionListUI.EnsureInstance()`).

- [x] **RPG-06: Cơ Sở Dữ Liệu 49 Vật Phẩm & Đồng Bộ Mô Hình Vũ Khí Nhân Vật (49 Items Database & Visual Sync)**
  - Khởi tạo trọn bộ 49 ScriptableObjects Items chuẩn hóa tiếng Anh:
    + 20 Vũ Khí chiến đấu (Kiếm Gỗ, Thiết Kiếm, Tân Binh, Bản Rộng, Hắc Thiết, Tiên Phong, Gai Thép, Cốt Ma Kiếm, Âm Dương, Tử Thần, Bích Ngọc, Huyết Hồng, Hồ Ly Katana, Titan Cleaver, Minh Phủ Hắc Kiếm, Băng Nha Cực Kiếm, Thái Dương Thánh Kiếm, Excalibur Sovereign, Cổ Kiếm Thất Truyền, Hỏa Long Diệt Thế Kiếm).
    + 8 Giáp Trụ bảo hộ (Áo Vải Tân Binh, Giáp Da Trinh Sát, Giáp Da Thám Hiểm, Thiết Giáp Cường Hóa, Giáp Xương Quái, Băng Giáp Cực Bắc, Hỏa Long Chiến Giáp, Hư Không Thần Giáp).
    + 13 Nguyên Liệu chế tác & Lõi Boss (Đá Cường Hóa, Tinh Thể Hoàn Mỹ, Mảnh Xương Quái, Huy Hiệu Chiến Binh, Mũi Tên Đen, Bụi Ma Pháp, Giấy Da Cổ, Mảnh Cổ Tự Băng, Trái Tim Băng Ice Core, Huy Hiệu Liệt Hỏa, Lõi Lửa Flame Core, Tinh Chất Vực Thẳm, Con Mắt Hư Không Void Eye).
    + 6 Dược Phẩm Sinh Tồn (Bình Máu Nhỏ 40 HP, Bình Máu Lớn 100 HP, Tiên Dược Thể Lực, Mật Năng Lượng Spirit, Thuốc Da Sắt +15 DEF, Rượu Cuồng Nộ +15 ATK).
    + 2 Vật phẩm nhiệm vụ & Tiền tệ: Đồng Tiền Vàng (Gold Coin) & Chìa Khóa Boss Hầm Ngục (Dungeon Boss Key).
  - Đồng bộ hóa mô hình kiếm cầm tay (`PlayerWeaponVisualSync.cs`): Gắn trên Player, tự động tìm socket `WeaponSocket`, thay đổi model 3D mượt mà khi đổi kiếm trong túi đồ, vô hiệu hóa collider con tránh lỗi hitbox/VFX chém.
  - Công cụ 1-Click tạo bộ dữ liệu 49 items và gắn visual sync cho Player.

- [x] **RPG-07: Hệ Thống Cửa Hàng Làng Haven (Shop NPCs - Tách 2 Cửa Hàng: Trang Bị & Nhu Yếu Phẩm)** *(ƯU TIÊN SỐ 1 HIỆN TẠI)*
  - [x] **RPG-07A: Kiến trúc Dữ liệu Cửa hàng Đa Năng (`ShopData.cs`, `ShopManager.cs`):**
    - ScriptableObject `ShopData.cs`: Danh sách mặt hàng niêm yết bán (`ItemData`, `buyPrice`, `stockQuantity` hoặc vô hạn `-1`).
    - Khởi tạo 2 Data Asset chuyên biệt:
      * `EquipmentShopData.asset`: Vũ khí tân binh, giáp da, thiết giáp, khiên cơ bản.
      * `SuppliesShopData.asset`: Bình máu nhỏ/lớn, tiên dược thể lực, mật năng lượng, đá cường hóa cơ bản.
    - Singleton / Service `ShopManager.cs` (liên kết `InventoryManager`): Xử lý giao dịch Mua (kiểm tra tiền vàng, kiểm tra slot trống túi đồ, trừ vàng, thêm đồ) và Bán (thu hồi item từ túi, cộng vàng = **40% `GoldValue`** theo `ItemData.SellPrice`).
  - [x] **RPG-07B: Giao Diện Người Dùng Shop UI 2 Panel Đồng Bộ (`ShopUIController.cs`, `ShopSlotUI.cs`):**
    - Dựng Canvas UI phong cách Dark RPG Souls-like gồm 2 Panel lớn:
      * **Panel Trái (Hàng của Shop - Chia làm 2 nửa):**
        + Nửa trái: Danh sách mặt hàng đang bày bán (ScrollView danh sách kèm Icon, Tên, Rarity, Giá niêm yết).
        + Nửa phải: Khung chi tiết của item đang chọn trong Shop (Icon lớn, Tên đổi màu Rarity, Thông số Stats, Mô tả) + Bộ chọn số lượng `[-] [N] [+]` cho vật phẩm cộng dồn + Hiển thị Giá Vàng (Đủ tiền: màu vàng/trắng mặc định; Không đủ tiền: đổi sang **màu đỏ** và **tối/vô hiệu hóa nút Mua**) + Nút **"MUA (BUY)"**.
      * **Panel Phải (Túi đồ Player & Thao tác Bán):**
        + Kế thừa chuẩn giao diện Túi đồ từ `InventoryUIController` (Tabs phân loại, Lưới 30 ô đồ, hiển thị số dư Vàng người chơi thời gian thực).
        + Khi click vào 1 item trong túi: Hiện `ItemDetailPopupUI`, ẩn toàn bộ nút Equip/Unequip/Use/Drop, **chỉ hiện nút "BÁN (SELL)"** kèm số tiền vàng nhận được (+40%). Hỗ trợ bộ chọn số lượng khi bán stack vật phẩm.
        + Player tự tay chọn bán từng món/nguyên liệu (không dùng nút bán tự động để tránh bán nhầm).
        + Khóa bảo vệ an toàn (Sell Protection): Trang bị đang mặc trên người hoặc vật phẩm nhiệm vụ (`Dungeon Boss Key`) tuyệt đối không cho phép bán (làm mờ hoặc ẩn).
    - Quản lý đóng/mở mượt mà qua CanvasGroup, phím `Esc` để thoát, tự động khóa camera và di chuyển nhân vật khi đang mở Shop.
  - [x] **RPG-07C: Thiết Lập 2 NPC Thương Nhân Tại Làng Haven (`LandScene`):**
    - Gắn `GenericInteractable.cs` vào 2 NPC tại cụm `EquipmentShop` và `SuppliesShop`:
      * `EquipmentShop`: Gợi ý `"[F] Mua Trang Bị"`, truyền `EquipmentShopData` mở UI Shop tiêu đề *"TIỆM TRANG BỊ & RÈN BINH"*.
      * `SuppliesShop`: Gợi ý `"[F] Mua Nhu Yếu Phẩm"`, truyền `SuppliesShopData` mở UI Shop tiêu đề *"TIỆM DƯỢC LIỆU & TẠP HÓA"*.
    - Góc máy camera trò chuyện (Dialogue Framing) lia nhẹ tạo chiều sâu điện ảnh.
  - [x] **RPG-07D: Công Cụ Tự Động Hóa 1-Click Unity Editor & Kiểm Thử Toàn Diện:**
    - Tool `ShopSetupEditor.cs` với MenuItem: `Infinite Labyrinth -> RPG -> 6. Thiết Lập Shop Làng Haven (1-Click)` tự động liên kết Prefab UI, ShopData và 2 NPC Shop.

- [x] **RPG-08: Lò Rèn Haven & Hệ Thống Cường Hóa / Chế Tạo Thần Binh (Blacksmith NPC - Upgrade & Crafting)**
  - [x] **RPG-08A: Kiến trúc Dữ liệu Cường Hóa (+1 đến +5) & Công Thức Chế Tạo (`InventorySlot.cs`, `CraftingRecipe.cs`):**
    - Mở rộng `InventorySlot.cs` và `InventoryManager.cs` lưu trữ `upgradeLevel` (0 đến 5) độc lập cho từng instance trang bị (không ghi đè ScriptableObject gốc).
    - Cường hóa Vũ khí (+1 đến +5): Mỗi cấp tăng **+15% Sát thương cơ bản (ATK)** của vũ khí; Giáp tăng +15% DEF.
    - Hiển thị Badge cấp độ `+1`..`+5` màu vàng kim ở góc icon ô đồ trong kho đồ (`InventorySlotUI.cs`).
    - Công thức nâng cấp:
      * `+1`: 1 Đá Cường Hóa (`mat_upgrade_stone`) + 50 Vàng.
      * `+2`: 2 Đá Cường Hóa + 100 Vàng.
      * `+3`: 3 Đá Cường Hóa + 200 Vàng.
      * `+4`: 1 Tinh Thể Hoàn Mỹ (`mat_upgrade_crystal`) + 4 Đá Cường Hóa + 400 Vàng.
      * `+5 (MAX)`: 2 Tinh Thể Hoàn Mỹ + 6 Đá Cường Hóa + 700 Vàng.
    - ScriptableObject `CraftingRecipe.cs` cho hệ thống Chế tạo Thần Binh từ Lõi Boss & Nguyên Liệu Quái (Cốt Ma Kiếm, Hồ Ly Katana, Băng Nha Cực Kiếm, Hỏa Long Diệt Thế Kiếm, Giáp Xương Quái, Băng Giáp Cực Bắc).
  - [x] **RPG-08B: Giao Diện Lò Rèn UI 2 Panel [NÂNG CẤP | RÈN] (`BlacksmithUIController.cs`):**
    - Dựng Canvas UI phong cách Lò Rèn Dark Fantasy gồm 2 Panel lớn:
      * **Panel Trái (Lò Rèn Haven):**
        + 2 Tab chuyển đổi trên đầu: **[ NÂNG CẤP (UPGRADE) | RÈN (CRAFT) ]**.
        + **Cửa sổ Nâng cấp (Upgrade Window - Chia làm 2 nửa so sánh):**
          * Nửa trái: Item gốc & thông số hiện tại (Icon, Tên, Cấp `+0`, ATK gốc).
          * Nửa phải: Item preview cấp kế tiếp & thông số dự kiến tăng (Icon, Tên, Cấp `+1`, ATK tăng xanh lá `+15%`).
          * Trạng thái Cấp Tối Đa: Khi vật phẩm đạt `+5`, hiển thị nhãn vàng kim **"CẤP TỐI ĐA (MAX LEVEL)"**, ẩn nguyên liệu và vô hiệu hóa nút nâng cấp.
          * Phía dưới: Danh sách nguyên liệu cần (Đá cường hóa, Tinh thể) hiển thị `Đang có / Cần` (Màu xanh đủ, màu đỏ thiếu) + Chi phí Vàng + Nút **"NÂNG CẤP (UPGRADE)"**.
        + **Cửa sổ Rèn (Crafting Window):**
          * Danh sách các công thức rèn trang bị thần binh từ Lõi Boss (Ice Golem Core, Flame Core).
          * Bảng chi tiết: Nguyên liệu thành phần cần thiết (`Đang có / Cần`) + Chi phí Vàng + Nút **"RÈN (FORGE)"**.
      * **Panel Phải (Túi đồ Player & Chọn Trang Bị):**
        + Hiển thị túi đồ người chơi (Tabs All, Weapon, Armor, Item; 30 ô đồ; Vàng thời gian thực).
        + Khi click vào 1 trang bị: `ItemDetailPopupUI` chỉ hiện duy nhất nút **"Bỏ Vào Nâng Cấp" (Put in)** đưa thẳng vào slot nâng cấp.
  - [x] **RPG-08C: Thiết Lập NPC Thợ Rèn Tại Lò Rèn Làng Haven (`LandScene`):**
    - Gắn `BlacksmithInteractable.cs` vào NPC tại cụm `BlackSmith`: Gợi ý `"[F] Forge & Upgrade Armaments"`, mở giao diện Lò Rèn.
    - Tự động khóa di chuyển, mở chuột và đóng bằng ESC/Tab mượt mà qua CanvasGroup.
  - [x] **RPG-08D: Game Feel Hoành Tráng, Hào Quang +5 & Tool 1-Click Unity Editor:**
    - Hiệu ứng nháy sáng phồng nhẹ (Punch Scale) khi nâng cấp và rèn thành công.
    - Khi trang bị đạt cấp `+5`, kích hoạt hiệu ứng hào quang vàng kim phát sáng quanh lưỡi kiếm trong game (`PlayerWeaponVisualSync.cs`).
    - Tool `BlacksmithSetupEditor.cs` với MenuItem: `Infinite Labyrinth -> RPG -> 7. Thiết Lập Lò Rèn Haven (1-Click)` và tích hợp vào Master Dashboard (Ctrl+Alt+D).

- [ ] **RPG-09: Cân Bằng & Cập Nhật Ma Trận Rơi Đồ Toàn Diện (Detailed Drop Tables)**
  - Cập nhật tỷ lệ rơi đồ chuẩn xác theo tài liệu `ITEM_SYSTEM_DESIGN.md` vào `LootDropper.cs`, `ChestInteractable.cs`, `BreakableProp.cs`, và `EnemyAIBase.cs`:
    + 3 Thùng gỗ đập được: Thùng nhỏ (Vàng, Bình máu nhỏ), Thùng vừa (Vàng, Máu, Thể lực, Đá cường hóa), Thùng lớn (Vàng lớn, Máu lớn, Đá cường hóa, Thiết kiếm rỉ).
    + 4 Quái Skeleton: Minion (Mảnh xương, Vàng), Warrior (Mảnh xương, Huy hiệu chiến binh, Đá cường hóa), Rogue (Mũi tên đen, Bình máu, Tiên dược thể lực), Mage (Bụi ma pháp, Đá cường hóa, Giấy da cổ).
    + 3 Cấp độ Rương: Rương thường (Vàng, Máu nhỏ, Đá cường hóa, Đồ Uncommon), Rương hiếm (Vàng nhiều, Máu lớn, Đá cường hóa, Vũ khí Rare, Giáp sắt), Rương Boss (180-350 Vàng, Lõi Boss 100%, Mảnh cổ tự, Đá & Tinh thể, Đồ Epic/Legendary cực phẩm).
  - Tích hợp công cụ 1-Click cập nhật toàn bộ Loot Tables trên scene và prefabs.

---

## 🚩 GIAI ĐOẠN 4: Điểm Nhấn Đồ Án - Cặp Trùm Souls-like & Trùm Học Máy (Boss ML-Agents)

- [x] **BOSS-01: Hoàn thiện Bộ Kỹ Năng, VFX & Động Cơ Di Chuyển Trùm Ice Golem (`Boss_IceGolem.prefab`)**
  - **Thông số sinh tồn Inspector:** Máu `bossMaxHealth = 1500 HP`, Thanh chịu đòn `maxPoise = 500`, Suy yếu `WeakenIG` 1 lần duy nhất, Phase 2 Cuồng Nộ tại `<= 50% HP` (chao đảo, sóng đẩy văng 5m, gầm thét phun lửa, phóng liên tiếp 3 cầu tím ma thuật, rút ngắn nhịp thở giao tranh từ 6-7s xuống 4-5s).
  - **Hệ thống Chiếu sáng Tự Thân & Standby Bục Đá:** Đèn hào quang băng xanh tự thân (`Boss_Aura_Light` range 16m), cố định Standby trên đỉnh bục đá 12m chờ người chơi mở cửa.
  - **Chuỗi Mở Màn JumpDown Lao Dốc Chuẩn Xác:** Quét tầm nhìn toàn phòng (100-300m); Bật nhún đà giữ nguyên độ cao bục đá (0.4s đầu) rồi phóng mình lao dốc cắm xuống sàn theo gia tốc rơi trọng lực $u^2$; Tiếp đất chính xác ở frame 55 (1.80s) kích nổ shockwave và rung chấn màn hình, khóa vĩnh viễn chiêu sau lần đầu tiếp đất.
  - **Trọn bộ 7 chiêu thức chiến đấu:** `Attack1` (đấm thẳng cận chiến), `GumoGumo Slam` (5 nhịp dộng sàn chính xác: 1.00s Phải, 1.67s Trái, 2.50s Phải, 3.20s Trái, 3.85s Hai tay kết liễu kèm `Attack1 Effect`), `FlameBreath` (phun lửa nón 15.5m), `JumpAttack` (lao bổ 20m), `RunAttack` (chạy húc 12m/s), `AttackLT_IG` (combo 3 đòn liên hoàn), `IceBallIG` (tụ cầu tím ma lực giữa hai tay trên cao phóng xa 16m nổ diện rộng).
  - **Động cơ Di Chuyển Mượt Mà 4 Hướng & Chiến Thuật Spacing (Approach on Distance):**
    + Bổ sung 12 transitions 2 chiều giữa 4 trạng thái đi bộ (`Walk`, `WalkBackIG`, `WalkLeftIG`, `WalkRightIG`) trong Animator Controller, triệt tiêu khựng bước chân.
    + Khi ở xa Player (> 8.0m): Khóa đi ngang và lùi, ép Boss tiến thẳng rút ngắn khoảng cách.
    + Khi vào cự ly chuẩn (3.5m - 8.0m): Mở khóa di chuyển ngang vờn quanh đối thủ chuẩn phong cách Souls-like.
    + Khi áp sát quá gần (< 3.0m): Khóa tiến, lùi lại hoặc ra đòn cận chiến.
  - **Bảo Toàn Monolithic Controller Thống Nhất (`IceGolemController.cs`):** Duy trì cấu trúc file nguyên khối ổn định tuyệt đối 100% Serialization trong Unity Inspector, bảo toàn toàn bộ VFX Prefabs và tham chiếu xương.
  - Menu 1-Click Unity Editor: Cấu hình Prefab Boss, Nối dây Animator và Dọn dẹp Scene.

- [x] **BOSS-02: Chuẩn Hóa Khung Xương, Vật Lý Áo Choàng & Vũ Khí Trùm Red Ignis (`Boss_RedIgnis.prefab`)**
  - Khắc phục triệt để lỗi vật lý tà áo `UnityChanSpringBone`: Dọn sạch mảng null (12 xương hợp lệ), gỡ bỏ 4 xương ngọn leaf bones lỗi, vô hiệu hóa khóa góc xoay và sàn ảo.
  - Khắc phục lỗi lún đất & co ngắn chiều cao: Ánh xạ chuẩn xương `Hips -> mixamorig:Hips` trong `RedIgnis.fbx`, chuẩn hóa hoạt ảnh bước đi `Ignis_Walk.fbx` với `Based Upon = Feet / Center of Mass` và bù trừ cao độ Offset Y.
  - Tích hợp Vũ khí Kiếm Siu Nhân Đỏ (`KiemSiuNhanDo.fbx`): Import Settings chuẩn vật thể tĩnh, gắn vào lòng bàn tay phải qua `RightHand_Socket`.
  - Tool cân chỉnh kiếm & chuẩn hóa T-Pose (`IgnisWeaponAligner.cs`): Giải quyết góc lệch -45.8 độ xương cổ tay, chuẩn hóa T-Pose qua `HumanPoseHandler`, cố định vị trí socket `Hand_R_Weapon` (Scale 0.8x, 0.8x, 1.0x).

- [x] **BOSS-03: Bộ Điều Khiển Chiến Đấu Trùm Red Ignis (`RedIgnisController.cs`, `Ignis_animator.controller` & Bộ VFX Độc Lập)**
  - Bảng thông số sinh tồn: Máu `bossMaxHealth = 1200`, Poise `maxPoise = 400` kèm trạng thái choáng vỡ thế `Weaken` (2.5s).
  - Động cơ di chuyển 4 hướng mượt mà qua phím Mũi Tên và API `Move(h, v)` cho AI, tự xoay mặt theo Player `280°/s`.
  - Trọn bộ 4 kỹ năng chiến đấu (Phím 1-4): `AttackSlashRI` (chém dọc phóng đạn kiếm 90 độ), `HorizontalSlashRI` (chém ngang phóng đạn kiếm bay ngang), `JumpAttackRI` (nhảy tới dậm đất nổ lan AOE), `ComboAttackRI` (chém liên hoàn 4 nhát lướt kiếm).
  - Hệ thống VFX độc lập `IgnisDo/VFX/`: Script đạn `RedIgnisSlashProjectile.cs` quét raycast liên tục chống xuyên tường, không can thiệp thư mục VFX gốc.
  - Animator Controller chuẩn mực đặt `IdleRI` làm Entry; Tool 1-Click cấu hình Prefab và Animator.

- [x] **ML-01: Tích hợp Unity ML-Agents Package & Môi Trường Huấn Luyện**
  - Tích hợp gói chính thức `com.unity.ml-agents: 4.1.0` (khớp Unity 6 và Unity AI Inference 2.6.1).
  - Cấu hình môi trường Python 3.10 với `mlagents: 1.1.0`, `torch: 2.14.1`, và `setuptools: 69.5.1`.
  - File cấu hình huấn luyện PPO: `MLAgents/Config/IceGolem_PPO.yaml` (batch_size: 256, buffer_size: 2048, hidden_units: 256, num_layers: 3).

- [x] **ML-02: Thiết kế Agent Trùm Băng Khống Chế & Souls-like (`IceGolemAgent.cs` & `IceGolemBattleArena.cs`)**
  - Kiến trúc Brain - Body tách biệt; Read-Only 100% đối với thông tin Player, bảo toàn tuyệt đối mã nguồn nhân vật.
  - Vector Observations 29 chiều toàn diện: Cự ly, hướng, góc quay, máu Boss, máu & thể lực Player, trạng thái Roll I-Frame, Roll-Recovery, Cooldown 7 chiêu, 4 tia raycast cản tường.
  - Action Space: 3 Discrete Branches (Branch 0: Di chuyển & Vờn quanh 5 lựa chọn; Branch 1: Kỹ năng chiến đấu 8 lựa chọn; Branch 2: Timing Mixup 2 lựa chọn hoãn đòn bắt bài roll).
  - Souls-like Reward Shaping: Thưởng Roll-Catching (+1.5), Thưởng Heal-Punish (+2.0), Thưởng hạ gục (+5.0), Phạt đánh vào I-Frame (-0.5), Phạt chém trượt (-0.4), Phạt bị parry (-1.5).
  - Đấu trường hiệp đấu liên hoàn không Game Over (`IceGolemBattleArena.cs`): Tự hồi phục và reset cự ly khi máu <= 5 HP để tập luyện không cần reload scene.
  - Tích hợp Sparring Partner Bot AI cho Player (`PlayerBotAI.cs` phím B bật/tắt) phục vụ tự luyện; Đóng gói model huấn luyện Run 3: `IceGolemSoulsAI-12955.onnx`.

- [ ] **ML-03: Thiết kế Agent Kiếm Thuật Cơ Động & Tái Sinh Đổi Dạng 2 Pha (`RedIgnisAgent.cs`)**
  - Cơ Chế Tái Sinh Đổi Diện Mạo (Phase 2 Rebirth & Appearance Transformation): Khi máu Phase 1 = 0, Boss quỵ ngã -> bùng nổ sóng nhiệt đẩy lùi Player -> hồi 100% máu và hoán đổi diện mạo mới (Flaming Wings / Dark Flame Aura, kiếm rực lửa) mở khóa bộ não Phase 2.
  - Observations 27 chiều bổ sung cờ `isPhase2`; Actions linh hoạt giữa rình rập vờn quanh (Phase 1) và cuồng bạo lướt vệt lửa sàn, bão lửa AOE (Phase 2).
  - Reward Shaping phong cách Duelist: Thưởng áp sát phá thủ, thưởng bắt roll, phạt vung kiếm trượt cự ly.

- [ ] **ML-04: Huấn Luyện Đa Sàn Đấu & Đóng Gói Model ONNX Thời Gian Thực**
  - Dựng 8 đến 16 sàn đấu Arena chạy song song trong Scene huấn luyện để tăng tốc độ hội tụ gấp 10 lần (`--time-scale=10`).
  - Huấn luyện PPO qua `mlagents-learn` với file cấu hình `IceGolem_PPO.yaml` và `RedIgnis_PPO.yaml`.
  - Đóng gói file `.onnx` xuất xưởng, nạp vào `Behavior Parameters` (chế độ `Inference Only`) chạy mượt mà 100% không phụ thuộc Python.

---

## 🚩 GIAI ĐOẠN 5: Giao Diện Hoàn Chỉnh & Âm Thanh (UI & Polish)

- [ ] **POL-01: Hệ thống Menu** (MainMenu, Settings đồ họa & âm lượng, PauseMenu phím Esc).
- [ ] **POL-02: Âm thanh (SFX & BGM)** (Nhạc nền Làng Haven yên bình, Nhạc hầm ngục u tối, Nhạc Boss rực lửa; Tiếng va chạm vũ khí, tiếng đe búa thợ rèn, tiếng bước chân, tiếng quái gầm).
- [x] **POL-03: Bản đồ Hầm ngục Toàn màn hình & Khám phá Sương mù (`DungeonMapManager.cs`)**
  - Bật/tắt bằng phím nóng `M`, hỗ trợ phím `Esc` để đóng nhanh. Bản đồ nằm ở đáy Canvas bên dưới HUD.
  - Tự động đặt zoom 2.0x và căn giữa vị trí người chơi khi mở, hỗ trợ lăn chuột zoom (1.0x - 3.5x) và kéo chuột Pan.
  - Dựng hình Phòng & Hành lang chuẩn BSP: Ghép nối góc L, ngã 3 chữ T và ngã 4 bằng 9-slice; Thu nhẹ đầu hành lang `corridorLengthTrim` kết hợp đệm đáy `FloorBase` xóa sổ 100% mấu nhô thừa; Đại Lộ Boss 40m nối liền lạc không đứt gãy.
  - Cơ chế Sương mù Fog of War: 3 trạng thái Chưa khám phá, Lân cận và Đã khám phá sáng rực vàng gold; Luôn hiển thị vị trí Icon Boss, Icon Key Tế đàn và Icon Phòng Kho Báu.
  - Cân bằng Phòng Kho Báu: 75% phòng ngách cụt thành kho báu (3-4 phòng/dungeon), luôn có ít nhất 4 quái tinh nhuệ canh giữ.

---

## 🚩 GIAI ĐOẠN 6: Công Cụ Kiểm Thử & Quản Trị Viên (Admin & Debug Tooling)

- [x] **ADM-01: Lập trình Bộ Não Điều Hành Cheat, Safe Teleport & ReGenerate Hầm Ngục (`AdminCheatManager.cs`)**
  - Singleton `AdminCheatManager.Instance` hỗ trợ toàn cục, tự phục hồi xuyên cảnh (`EnsureInstance()`).
  - Ma trận Dịch chuyển tức thời an toàn (Safe Teleport Matrix): Teleport về Start Room, Đấu trường Boss, Key Shrine, Phòng Kho Báu (xoay vòng), Gác lửng tầng 2; Fallback thông minh sang địa danh Làng Haven khi ở `LandScene`.
  - Tái sinh Hầm ngục tức thời trong Play Mode (`RegenerateDungeon`) dọn sạch map cũ sinh seed mới; Diệt quái bán kính 25m hoặc toàn bản đồ.

- [x] **ADM-02: Bộ Cheats Chiến Đấu, Sinh Tồn & Tiến Trình RPG**
  - Player God Mode (bất tử 100% trong `Health.cs`), Vô hạn Thể lực Stamina, Vô hạn Spirit nạp 4 sao kỹ năng, Hồi phục toàn diện Máu/Stamina/Spirit trong 1 click.
  - Tăng tốc độ di chuyển siêu tốc (1.0x -> 1.8x -> 2.5x -> 3.5x); Đóng băng AI quái vật thành bia tập đánh; Sinh quái thử nghiệm 1v1.
  - Bơm Vàng (+1.000 / +10.000), Bơm EXP (+500), Bơm Điểm Tiềm Năng (+10), Thêm Chìa Khóa Boss, Bình Máu, Hỏa Long Kiếm Legendary.

- [x] **ADM-03: Giao Diện Người Dùng Admin Console UI (`AdminCheatUI.cs`) & Bật/Tắt Phím `~` / `F1` / `F2`**
  - Chuẩn hóa 100% giao diện tiếng Anh, phong cách Dark RPG bán trong suốt (460x620px) với 5 Tab: Teleport, Dungeon, Player, Monsters, RPG & Loot.
  - Mở/đóng đồng bộ với Mini Debug Overlay góc trên-phải (FPS, tọa độ, location, cheat badges), Toast Notifications phản hồi tức thì.
  - Tọa độ an toàn: Dịch chuyển lùi trước cửa phòng khởi đầu 4.5m và đứng trước Đại Môn Boss 5.5m nhìn thẳng cổng vòm.

- [x] **ADM-04: Công Cụ Tự Động Hóa 1-Click Unity Editor & Đóng Gói Prefab**
  - MenuItem: `Infinite Labyrinth -> Admin -> 1. Setup Admin Cheat Console (Hotkey ~)`.
  - Tự động dựng toàn bộ Canvas UI, cài đặt EventSystem, nối dây 100% nút bấm và xuất thành Prefab tái sử dụng: `Assets/Prefabs/Admin/AdminCheatCanvas.prefab`.

- [x] **DEV-01: Tái Cấu Trúc Toàn Diện Hệ Thống Editor & Master Control Center (`ProjectDashboardEditor.cs`)**
  - Gom toàn bộ công cụ phát triển, setup 1-click vào một cửa sổ trung tâm duy nhất: `ProjectDashboardEditor` (phím tắt `Ctrl + Alt + D` hoặc menu `Infinite Labyrinth -> 🚀 Master Control Center (Dashboard)`).
  - Phân chia 5 Tab trực quan: Combat & VFX, Boss Encounters (Ice Golem & Red Ignis), RPG & Haven Hub, Game Loop & UI, và Optimization & Admin.
  - Tích hợp thanh Scene Quick Switch chuyển đổi nhanh giữa `LandScene` và `DungeonScene`.
  - Triệt tiêu 100% menu rác, trùng lặp trên thanh công cụ Unity: Xóa bỏ menu tách rời `Boss/`, xóa bỏ menu `Tools/Infinite Labyrinth` và `Tools/RedIgnis`.
  - Hợp nhất cấu trúc thư mục: Xóa bỏ `Assets/Editor/` cũ, chuyển các script Red Ignis về `Assets/Scripts/Editor/`, chuyển các script sửa lỗi một lần (one-off) đã hoàn thành vào `Assets/Scripts/Editor/Archive/`.

---

## 🚩 GIAI ĐOẠN 7: Tối Ưu Hệ Thống Inventory, UI & Vòng Đời Ứng Dụng (Lifecycle & Cleanup)

- [x] **INV-01: Hoạt ảnh chọn Tab phân loại & Phím tắt kỹ năng / UsableSlot**
  - Hiệu ứng nảy phồng (Punch scale animation) khi chuyển tab đồ trong Inventory (TabBar).
  - Phím tắt C dùng bình máu trên màn hình có hoạt ảnh nảy phồng đồng bộ chuột và phím bấm.
  - Áp dụng hoạt ảnh nảy phồng cho 3 icon kỹ năng góc dưới phải (`SkillIconUI`).

- [x] **INV-02: Ô trang bị dược phẩm UsableSlot & Tự động cộng dồn (Auto-Refill On Pickup)**
  - Thêm ô trang bị UsableSlot ở LeftPanel và slot ngoài màn hình (`UsableSlotUI`) có cooldown 2s chung (CDFill).
  - Tự động cộng dồn số lượng dược phẩm nhặt được vào ô UsableSlot đang trang bị trước khi tràn vào túi đồ 30 ô.
  - Sửa lỗi khi tháo đồ (Unequip) không còn kích hoạt nhầm thông báo nhặt đồ (PickupNotification).
  - Nâng cấp cơ chế vứt đồ (Drop): vật phẩm rơi theo quỹ đạo cung parabol, visual 3D vũ khí tương ứng (tỉ lệ scale gọn gàng 0.6x = 75% kích thước ban đầu qua thuộc tính `weaponDropScale`) và ánh sáng glow theo phẩm cấp (Rarity Point Light).
  - Tự động đóng popup thông tin chi tiết (`ItemDetailPopupUI`) khi click chuột ra ngoài khung.

- [x] **FIX-04: Khắc phục lỗi Scene Cleanup Error khi dừng Play Mode (`[LoadingScreenManager]` & `LoadingScreenCanvas`)**
  - Bổ sung cờ `isApplicationQuitting` và bắt sự kiện `Application.quitting` sớm trong `LoadingScreenManager.cs`.
  - Thêm thuộc tính kiểm tra an toàn `HasInstance => !isApplicationQuitting && instance != null`.
  - Trong `LoadingScreenManager.Instance`, `EnsureInstance()` và `EnsureDependencies()`: chặn lập tức việc sinh GameObject nếu `isApplicationQuitting == true`.
  - Trong `InfoUIController.cs`: Thay thế lệnh gọi `.Instance != null` bằng `.HasInstance` trong `OnDisable()` và `Start()`, triệt tiêu hoàn toàn việc sinh GameObject ma khi dừng Play Mode.

- [x] **UI-07: Đồng bộ CanvasGroup cho AdminCheatCanvas & Rarity Visuals cho ItemDetailPopupUI**
  - **AdminCheatUI CanvasGroup Control:** Tự động phát hiện và đồng bộ điều khiển thuộc tính alpha, interactable, blocksRaycasts của rootCanvasGroup khi mở/đóng bằng phím ~ hoặc nút [✕]. Khắc phục triệt để hiện tượng gán CanvasGroup(alpha = 0) lên AdminCheatCanvas khiến menu bị tàng hình khi bật.
  - **ItemDetailPopup Rarity Visuals:** Tự động kết nối RarityBG, RarityBar và container Icon trong ItemDetailPopupUI.cs. Đồng bộ hiển thị dải màu phẩm cấp thời gian thực theo Rarity của vật phẩm đang xem; tự động vô hiệu hóa (SetActive(false)) icon và rarity khi chưa có vật phẩm hoặc khi đóng popup, tránh hiển thị ô vuông trắng rỗng trong Scene.
