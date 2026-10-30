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

- [x] **UI-02: Hệ Thống Thanh Máu Quái Vật World Space Souls-like (EnemyHealthBarUI.cs)**
  - **Billboard 3D Thông Minh:** Tự động xoay mặt vuông góc 100% về phía Camera người chơi (CameraAligned), phẳng phiu và không bị méo lệch góc nhìn ở mọi hướng quan sát. Hỗ trợ tùy chọn khóa trục Y (lockYAxis).
  - **Khai Thác Sprite Grayscale & Hiệu Ứng Souls-like:**
    - Sử dụng Monster_Border.png và Monster_Fill.png (chuẩn kích thước 3331x400, tỉ lệ 8.33:1).
    - MainFill: Tô màu đỏ tươi Souls-like từ sprite grayscale qua Image.color.
    - Ghost Bar: Thanh bóng ma màu vàng nhạt tụt đuổi từ từ sau thời gian trễ ghostShrinkDelay = 0.45s khi quái trúng đòn.
  - **Tối Ưu Hóa Hiệu Năng Tuyệt Đối (Dungeon 200 Quái):**
    - Tự động tắt Canvas khi quái ở cự ly xa hoặc ngủ đông (DungeonEnemyOptimizer - 0 Draw Calls).
    - Ẩn thanh máu khi quái đầy 100% HP và chưa vào trạng thái chiến đấu; tự động Fade In mượt mà khi dính đòn hoặc phát hiện người chơi.
    - Tự động Fade Out và tắt hẳn khi quái tử trận (onDeath).
    - Tắt hoàn toàn GraphicRaycaster để loại bỏ 100% chi phí EventSystem Raycasting.
  - **Tự Động Hóa 1-Click Unity Editor & Runtime:**
    - Tool EnemyHealthBarSetupEditor.cs với MenuItem: Infinite Labyrinth -> Monsters -> 1. Cấu Hình Toàn Diện Thanh Máu Cho Quái Vật (1-Click).
    - Tự động tạo Prefab EnemyHealthBar.prefab (trong cả Assets/Prefabs/RPG/ và Assets/Resources/) và gắn trực tiếp vào 4 Prefabs quái: SkeletonWarrior, SkeletonMinion, SkeletonRogue, SkeletonMage theo độ cao đầu tương ứng.
    - Tích hợp 2 tầng bảo vệ: Nướng sẵn vào Prefab quái và hỗ trợ Fallback Runtime EnsureHealthBar() trong EnemyAIBase.cs.

- [x] **UI-03: Màn hình Loading Screen chuyển cảnh Async & Hầm ngục PCG (Task LD-01 - LD-05)**
  - [x] **LD-01: Kiến trúc Dữ liệu `LoadingScreenData.cs` & Bộ Khởi Tạo Editor:**
    - Tạo ScriptableObject [`LoadingScreenData.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/UI/Loading/LoadingScreenData.cs) lưu trữ 21 mẹo tiếng Anh chia theo 5 nhóm chủ đề: `COMBAT`, `EXPLORATION`, `BOSS TACTICS`, `VILLAGE HUB`, `ROGUELITE`.
    - Hỗ trợ mảng ảnh nền hầm ngục và ngôi làng (cả `Texture2D` lẫn `Sprite`).
    - Tiêu đề phân vùng ngữ cảnh theo Scene (DungeonScene: *"INFINITE LABYRINTH - FLOOR {0}"*, LandScene: *"HAVEN VILLAGE"*).
    - Công cụ tự động hóa [`LoadingScreenSetupEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/LoadingScreenSetupEditor.cs) quét và nạp tự động 13 ảnh render hầm ngục từ `Assets/Prefabs/Dungeon/Samples/`.
  - [x] **LD-02: Dựng Prefab `LoadingScreenCanvas.prefab` & `LoadingScreenUI.cs`:**
    - Tạo controller [`LoadingScreenUI.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/UI/Loading/LoadingScreenUI.cs) điều khiển: Full-screen RawImage hầm ngục, Vignette tối tương phản cao, Header địa danh (`LocationTitle` + `LocationSubtitle` + `DecorativeLine`), Khung hiển thị Tip tiếng Anh (`TipCategory` + `TipContent`), Thanh tiến trình Souls-like (`Monster_Border` + `Monster_Fill`), Text % góc phải, và Icon `Exp_Circle` Spinner xoay liên tục (dùng `unscaledDeltaTime` chống đóng băng UI).
    - Tạo công cụ tự động hóa MenuItem: `Infinite Labyrinth -> UI -> 2. Tạo Prefab Loading Screen Canvas (1-Click)` trong [`LoadingScreenSetupEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/LoadingScreenSetupEditor.cs) tạo sẵn Prefab chuẩn Canvas Overlay (Sort Order 999) tại cả `Assets/Resources/LoadingScreenCanvas.prefab` và `Assets/Prefabs/RPG/LoadingScreenCanvas.prefab`.
    - Hỗ trợ API `Show(...)`, `SetProgress(...)`, `Hide(...)` kèm hiệu ứng Fade In/Out qua `CanvasGroup`.
  - [x] **LD-03: Bộ điều phối `LoadingScreenManager.cs` (Singleton / DontDestroyOnLoad):**
    - Tạo [`LoadingScreenManager.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/UI/Loading/LoadingScreenManager.cs) hoạt động theo mô hình Singleton bền vững xuyên cảnh (`DontDestroyOnLoad`), hỗ trợ cơ chế tự phục hồi `EnsureInstance()` tự nạp Canvas và Data từ `Resources/`.
    - Phân chia pha nạp bất đồng bộ: Pha 1 nạp Scene qua `SceneManager.LoadSceneAsync` (tiến trình ánh xạ `0% -> 40%`), Pha 2 chờ tạo lập thế giới / PCG (tiến trình ánh xạ `40% -> 100%`).
    - Cấu hình thời gian hiển thị tối thiểu `minLoadingTime = 1.6s` chống chớp nháy UI trên SSD tốc độ cao, đảm bảo người chơi luôn đọc trọn vẹn 1 mẹo hữu ích.
    - Cung cấp API `LoadScene(...)` và `LoadSceneWithStagedCallback(...)` sẵn sàng kết nối với `DungeonManager` và `PortalController`.
  - [x] **LD-04: Nâng cấp `DungeonManager.cs` sinh PCG theo từng chặng (Time-Sliced Staged Generation):**
    - Bổ sung `BuildMeshRoutine` trong [`DungeonRenderer.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonRenderer.cs) chia nhỏ việc dựng hình sàn, tường, trần, gác lửng tầng 2, cột đấu trường, props và NavMesh qua nhiều khung hình (`yield return null`).
    - Nâng cấp [`DungeonManager.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonManager.cs) với Coroutine `GenerateWorldRoutine(Action<float> onProgress)` báo cáo tiến trình theo 5 giai đoạn: Toán học BSP (12%) -> Dựng hình & Props & NavMesh (82%) -> Quái vật (92%) -> Tối ưu ngủ đông & Test quái (100%).
    - Đồng bộ `Start()`: Tự động phát hiện nếu `LoadingScreenManager.IsLoading` đang chạy thì nhường quyền điều phối cho LoadingScreen, bảo toàn khả năng play test trực tiếp độc lập khi nhấn Play trong Scene.
  - [x] **LD-05: Tích hợp vào `PortalController.cs` & các luồng chuyển cảnh:**
    - Cập nhật [`PortalController.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/PortalController.cs) chuyển đổi toàn bộ lệnh nạp scene đồng bộ sang `LoadingScreenManager.Instance.LoadScene(targetSceneName, targetFloor)` (hỗ trợ fallback an toàn).
    - Kết nối cả 2 chiều di chuyển: Từ Làng (`LandScene`) vào Hầm ngục (`DungeonScene`) và từ Hầm ngục trở về Làng. Hiển thị chuẩn xác ảnh nền hầm ngục, các mẹo tiếng Anh, số phần trăm và thanh tiến trình thời gian thực.
  - [x] **LD-06: Chạy Loading Screen trực tiếp trong DungeonScene & Admin Console & Tinh chỉnh Inspector:**
    - **Chạy trực tiếp từ Scene Dungeon (Cold Start Play):** Cấu hình `showLoadingScreenOnStart = true` trong [`DungeonManager.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonManager.cs). Khi ấn Play ngay trong `DungeonScene`, hệ thống kích hoạt `RunInSceneLoading` với cờ `instant: true` để che màn hình ngay từ Frame 0, khóa input người chơi và hiển thị mẹo/background ngẫu nhiên trong lúc PCG kiến tạo hầm ngục.
    - **Tích hợp Admin Console:** Nút "Regenerate Dungeon" (`~` / `F1`) kích hoạt `LoadingScreenManager.Instance.RunInSceneLoading(...)` chạy trơn tru mượt mà ngay trong scene.
    - **Tùy biến & Xem trước Inspector:** Cung cấp Custom Editor [`LoadingScreenUIEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/LoadingScreenUIEditor.cs) với các nút tiện ích: `👁️ Bật Xem Trước (Alpha = 1)`, `🙈 Ẩn Màn Hình (Alpha = 0)`, `🎲 Đổi Ngẫu Nhiên Mẹo & Ảnh Nền`, và thanh trượt `% Tiến Trình` thời gian thực trên Scene & Game view.
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

- [x] **VFX-01: Hệ thống Hit VFX Phản Hồi Đòn Đánh Cho Player & Quái Vật (`MobsGetHitVFX` & `GetHitVFX`)**
  - **Cơ chế Decoupled Body Reaction:**
    + **Quái xương (Skeleton Monsters):** Khi bị Player chém trúng (combo 1, 2, 3, charge, counter, skill E, Q), [`EnemyFeedback.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Monsters/EnemyFeedback.cs) tự động sinh `MobsGetHitVFX.prefab` (màu xám vụn xương) tại tọa độ va chạm `hitPoint`, hướng tứa ngược chiều cú chém (`-hitDirection`) ra không khí.
    + **Người chơi (Player):** Khi dính đòn từ quái vật (kiếm, tên bắn, phép ma pháp, chấn động boss), [`PlayerFeedback.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerFeedback.cs) tự động sinh `GetHitVFX.prefab` (màu đỏ tứa máu) tại `hitPoint`. Khi Player **Parry thành công**, sát thương bị chặn đứng trước khi trừ máu nên **không bị tứa máu đỏ**.
    + **Bảo toàn Đòn kết liễu (Lethal Hit):** Kích hoạt `SpawnHitVfx` trước khi kiểm tra tử vong, bảo đảm phát sinh hiệu ứng tứa tia VFX ngay cả trong nhát chém hạ gục mục tiêu.
    + **Công cụ Tự động hóa Unity Editor 1-Click:** [`CombatVfxSetupEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/CombatVfxSetupEditor.cs) với MenuItem `Infinite Labyrinth -> Combat -> 1. Cấu Hình Hit VFX Cho Player & Quái Vật (1-Click)`, nướng trực tiếp tham chiếu vào `Player.prefab` và 4 Prefabs quái: `SkeletonWarrior`, `SkeletonMinion`, `SkeletonRogue`, `SkeletonMage`.

- [x] **VFX-02: Hệ thống Sword Slash Trail & Arc VFX Cho Player (`PlayerSlashVFX` & `Slash VFX.prefab`)**
  - **Kiến trúc Decoupled Arc Slash:** Không gắn dính vệt chém vào xương kiếm hay bàn tay. Thay vào đó, vệt chém được quản lý bởi [`PlayerSlashVFX.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerSlashVFX.cs) sinh ra đúng thời điểm vàng (Keyframe) từ các Animation Events trong [`PlayerCombat.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerCombat.cs).
  - **Cấu hình Độc lập Theo 6 Đòn Đánh (Position, Rotation & Scale):**
    + *Attack 1:* Chém vát chéo vai phải xuống hông trái (Rotation: `-176.57°, -92.12°, -92.12°`, Scale 1.0x).
    + *Attack 2:* Chém vát ngược từ hông trái lên vai phải (Rotation: `-85.89°, -183.54°, 0.65°`, Scale 1.05x).
    + *Attack 3:* Chém quét ngang phẳng uy lực kết thúc combo (Rotation: `-176.57°, -92.12°, 0.09°`, Scale 1.25x).
    + *Charge Hit 1:* Chém mở màn lấy đà gồng lực (Rotation: `-176.57°, -92.12°, -45°`, Scale 1.1x).
    + *Charge Hit 2:* Nhát bổ thẳng đứng từ trên cao cắm xuống đất (Rotation: `-90°, -92.12°, -90°`, Scale 1.35x).
    + *Counter Attack:* Cú chém phản đòn bùng nổ năng lượng diện rộng (Rotation: `-176.57°, -92.12°, -15°`, Scale 1.5x).
  - **Bảo vệ 2 Tầng & Tự Phục Hồi (Self-Healing Runtime):**
    + [`PlayerCombat.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerCombat.cs) tự động tìm kiếm tham chiếu `PlayerSlashVFX` ở parent (GameObject gốc `Player`), triệt tiêu việc tự ý sinh component thừa lên con `MainModel`.
    + [`PlayerSlashVFX.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerSlashVFX.cs) tự động tìm nạp fallback `Resources/Slash VFX` nếu inspector chưa gán prefab.
    + Công cụ [`PlayerSlashVfxSetupEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/PlayerSlashVfxSetupEditor.cs) tự động dọn dẹp các component `PlayerSlashVFX` bị trùng trên con `MainModel` và trỏ tham chiếu chuẩn vào root `Player`.
  - **Hiệu ứng Va Chạm Tóe Lửa Khi Trúng Đích (Hit Impact Burst VFX - `WeaponHitbox.cs`, `Impact VFX.prefab`):**
    + Tách rời `Impact` khỏi `Slash VFX.prefab` (chống nổ chớp sáng vô lý khi chém hụt ra không khí).
    + [`WeaponHitbox.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/WeaponHitbox.cs) tự động kích nổ `Impact VFX` ngay tại `hitPoint` (điểm tiếp xúc va chạm thực tế) khi chém trúng đối tượng có HP (Layer `Enemy` & `Damageable`, có `IDamageable`/`Health`).
    + Tự động kích hoạt đồng loạt mọi `ParticleSystem` con (kể cả root có `playOnAwake = false`), góc tứa ngược chiều chém (`-hitDirection`).
    + Bổ sung nút bấm `[ 💥 Test Thử Hit Impact VFX ]` ngay trên Custom Inspector [`PlayerSlashVFXEditor`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/PlayerSlashVfxSetupEditor.cs).
  - **Công Cụ Tự Động Hóa Unity Editor 1-Click & Custom Inspector:**
    + [`PlayerSlashVfxSetupEditor.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Editor/PlayerSlashVfxSetupEditor.cs) với MenuItem `Infinite Labyrinth -> Combat -> 2. Cấu Hình Slash Trail & Hit Impact VFX Cho Kiếm Player (1-Click)`.
    + Custom Inspector cho `PlayerSlashVFX` với 6 nút Test chém trực quan và Gizmos vẽ trước mặt phẳng chém trong Scene View.

- [ ] **D-02: Cơ chế Boss Arena tại Boss Room** *(ƯU TIÊN SỐ 1 HIỆN TẠI)*
  - [x] **D02-1: Sinh Phòng Boss Khổng Lồ (x3-x5), Đại Lộ Hành Lang Dài 40m Uy Nghi, 4 Cột Trụ & 3 Tầng Ánh Sáng (PCG Geometry & Lighting):**
    - Cấp phát phân vùng lá BSP lớn chuyên biệt (bossNodeMinSize = 48, bossRoomMinSize = 26, bossRoomMaxSize = 30) tại điểm neo xa nhất so với cửa khởi đầu (bossTargetPoint), neo phòng Boss sát biên Bắc (trên cùng) để chừa trọn vẹn khoảng không phía Nam làm Đại Lộ. Diện tích phòng Boss đạt 676 - 900 tiles (52m - 60m), gấp 3 - 5 lần phòng thường (~200 tiles) với vòm trần đồ sộ cao 24m chuẩn thánh đường Souls-like.
    - Kiến tạo Đại Lộ Hành Lang uy nghi thẳng tắp dẫn vào phòng Boss: Rộng 12m (6 tiles), Dài 40m (20 tiles) độc lập, kết nối cửa ngõ phía Nam với phòng kế cận (bossPrevRoom) và nướng lại vào mapGrid.
    - Dựng 4 cây cột đá cổ đồ sộ chịu lực tại 4 góc phòng Boss (vươn cao 24m chạm trần, scale 2.5x), gắn MeshCollider và NavMeshModifier(area = 1 - Not Walkable) làm điểm núp chiến thuật né đòn IceBall và FlameBreath.
    - Dọn sạch 100% bàn ghế, rương đồ, thùng gỗ bừa bãi ở lòng sàn đấu trường; bố trí hệ thống 3 Tầng Ánh Sáng nghệ thuật (Atmospheric Souls Lighting):
      + Tầng 1: Giếng trời ánh trăng (Pale Moonlight Spotlight) rọi thẳng từ trần 23.5m xuống tâm sàn đấu.
      + Tầng 2: 4 Vạc lửa khổng lồ rực cháy (Cauldron_Fire.prefab) đặt trang trọng trên mặt sàn đất (Y = 0) tại chân 4 cột chống trời, tỏa ánh lửa vàng cam ấm áp (Point Light range 24m, intensity 3.8f).
      + Tầng 3: Vòng hào quang băng xanh tự thân (Body Aura Light) trên Boss Ice Golem.
      + Hàng đuốc Đại Lộ: 2 hàng đuốc đối xứng uy nghi cắm dọc hai vách tường của đại lộ 40m mỗi 4 tiles.
  - [x] **D02-2: Cơ Chế Tế Đàn & Sinh Chìa Khóa Hầm Ngục (Shrine Key PCG):**
    - Nhận diện phòng nhánh cụt keyRoom (graphDistance xa nhất trong các nhánh cụt connections <= 1, diện tích 1 tầng độc lập hasMezzanine = false, trần cao 8m chuẩn nơi tôn nghiêm).
    - Ép buộc sinh wallset_shrine.prefab tại vị trí trung tâm hướng mặt về phía lối vào cửa.
    - Quét socket [Key_Socket] trên bàn nhỏ tế đàn và sinh Chìa Khóa Hầm Ngục (DungeonKeyPickup.prefab) lơ lửng, phát quang ánh vàng Legendary (range = 3.5m, intensity = 3.5f) kèm hiệu ứng xoay tròn nhấp nhô.
    - Nhặt chìa khóa (phím F) lưu thẳng vào InventoryManager (ItemData: dungeon_key), kích hoạt popup thông báo nhặt đồ.
  - [x] **D02-3: Đại Môn Phong Ấn (Boss Door) & Cổng Sương Mù (Fog Gate Lock-in):**
    - Dựng cửa lớn scale 2.5x tại lối vào phòng Boss, yêu cầu Chìa Khóa Hầm Ngục và giữ F (2.5s) để mở.
    - Tự động đóng cửa và kích hoạt rào chắn sương mù khi người chơi bước vào đấu trường, khóa chặt đường lui.
  - [x] **D02-4: Đạo Diễn Đấu Trường (Arena Director), Boss JumpDown Giáng Trần & Thanh Máu Souls HUD:**
    - **Sinh Bục Đá & Boss PCG (`DungeonPropDecorator.cs`):** Tự động sinh `BossPedestal.prefab` tại vách đối diện cửa vào (vách Bắc phòng Boss, Y = 0), gắn `NavMeshModifier(area = 1 - Not Walkable)`. Quét socket `[Boss_Socket]` trên đỉnh bục cao 12m và tự động sinh `Boss_IceGolem.prefab` tại đây ở chế độ ngủ đông đứng yên `SetStandbyOnPedestal(true)`.
    - **Giao Diện Thanh Máu & Diễn Hoạt Tên Trùm (`BossHUDController.cs`):**
      + Quản lý cụm `BossHP` ở đỉnh màn hình: Đổi thanh `Fill` sang màu xanh băng tuyết (`frostHpColor`), liên kết cập nhật máu thời gian thực theo `Health.cs` của Boss.
      + Diễn hoạt typography Souls-like đỉnh cao: Banner và Tên trùm xuất hiện hoành tráng ở giữa màn hình (`IceGolemName`), giữ 1.2s để người chơi đọc tên, sau đó Banner mờ dần biến mất trong khi dòng chữ Tên vừa bay lên phía trên thanh máu vừa thu nhỏ lại vừa vặn.
    - **Bộ Đạo Diễn Cutscene Điện Ảnh 5 Pha (`BossIntroDirector.cs`):**
      + Lắng nghe sự kiện `BossArenaTrigger.OnPlayerEnteredArena` khi Player bước qua Đại Môn.
      + Khóa tạm thời Input Player (`PlayerController`, `PlayerCombat`).
      + Pha 1: Camera cắt giật tới giữa phòng đấu trường, hiện thanh máu Boss màu xanh băng (chưa có tên).
      + Pha 2: Camera lia mượt mà theo đường cong sang mạn phải (Right Flank 3/4 Low-Angle Heroic View) ngước nhìn lên đỉnh bục đá 12m nơi Boss đứng.
      + Pha 3: Boss phóng mình nhảy `JumpDown` từ bục 12m giáng trần (camera lia hạ theo cú rơi) -> Tiếp đất rền vang kèm vụ nổ va chạm và Rung Màn Hình (Screen Shake) cực mạnh.
      + Pha 4: Boss ngửa cổ thi triển đòn phun băng chào sân (`PlayIntroBreath`), cùng lúc kích hoạt hoạt ảnh Banner mờ dần và Tên trùm bay lên neo vững trên thanh máu.
      + Pha 5: Kéo camera trượt êm ái (EaseInOut) từ đấu trường quay về sau vai Player, mở lại toàn bộ quyền điều khiển và kích hoạt Boss vào trạng thái chiến đấu.
    - **Tự Động Hóa 1-Click Unity Editor & Runtime:** Tích hợp menu `Infinite Labyrinth -> Boss -> 7. Cấu Hình Cutscene Intro & Boss HUD (Task D02-4)` và cơ chế `[RuntimeInitializeOnLoadMethod]` tự động khởi chạy mượt mà ngay khi bấm Play Mode.
  - [ ] **D02-5: Hoàn Tất Chiến Thắng (Victory Fanfare), Rơi Thưởng Đại Lượng & Cổng Dịch Chuyển Tầng Kế:**
    - Hiển thị biểu ngữ 'VICTORY ACHIEVED', nổ tung vàng/đồ quý, mở lại cửa và kích hoạt Cổng Dịch Chuyển lựa chọn Về Làng hoặc Xuống Tầng 2.

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

## 🚩 GIAI ĐOẠN 4: Điểm Nhấn Đồ Án - Cặp Trùm Souls-like & Trùm Học Máy (Boss ML-Agents)

- [x] **BOSS-01: Hoàn thiện Bộ Kỹ Năng & VFX Trùm Ice Golem (`Boss_IceGolem.prefab`)**
  - **Đồng bộ trực tiếp lên Prefab gốc (`Boss_IceGolem.prefab`):**
    + Gắn component `IceGolemController` với cấu hình tham chiếu xương tay (`leftHandBone: BossHitboxT`, `rightHandBone: BossHitboxP`), miệng (`mouthBone: Mieng`), Animator, và Tag `Enemy`.
    + Cấu hình Máu Boss: `Health.maxHealth = 800 HP` chuẩn trùm Souls-like (loại bỏ Persistent Call gọi `SetActive(false)` lỗi thời).
    + Cơ chế tự bảo vệ trong `Awake()`: Tự động phát hiện và hủy mọi instance `IceGolemController` bị gắn trùng trên cùng GameObject để chống nhân đôi chiêu thức.
  - **Chuẩn Hóa 100% Hoạt Ảnh Humanoid Rig & Khắc Phục Triệt Để Lỗi Chôn Chân:**
    + Khắc phục triệt để lỗi hoạt ảnh Generic (`AttackLT_IG.anim`, `WalkBackIG.anim`, `WalkLeftIG.anim`, `WalkRightIG.anim`) làm tụt xương `mixamorig:Hips` xuống Y = 0.88m khiến Boss cao 5.12m bị chìm nửa người xuống đất và đóng băng di chuyển.
    + Trỏ toàn bộ các State trong `BossController.controller` sang sub-asset Humanoid chuẩn bên trong các file FBX (`AttackLT_IG.fbx`, `WalkBackIG.fbx`, `WalkLeftIG.fbx`, `WalkRightIG.fbx`), đưa toàn bộ 14 State trong Animator Controller thành 100% Humanoid chuẩn xác.
  - **Tích hợp trọn bộ 7 chiêu thức chiến đấu & Tùy biến VFX Prefabs chuyên biệt:**
    + `Attack1` *(Phím 1)*: Đấm thẳng cận chiến (`attack1Damage = 25`, `hitDelay = 1.23s`, Scale 1.2x) kết hợp `Impact.prefab`.
    + `GumoGumo Slam` *(Phím 2)*: Đấm sàn rung đất liên hoàn 2 tay (`gumoSlamDamage = 18`, bán kính 12m, Scale 2.0x, nhịp 0.35s) kết hợp `Attack1 Effect.prefab` bùng nổ tại từng vị trí bàn tay tiếp đất.
    + `FlameBreath` *(Phím 3)*: Phun lửa nón tầm trung 15.5m (`flameConeAngle = 49°`, `flameDamagePerTick = 12`, mật độ hạt 1.9x, visual dài 3.58x, rộng 2.9x, thời lượng 3.2s) kết hợp `Flame Thrower.prefab`, khóa xoay dọc và độ cao ngọn lửa.
    + `JumpAttack` *(Phím 4)*: Lao bổ áp sát tầm xa 20m (`jumpAttackDamage = 40`, đỉnh cung 2.8m, delay đập đất 0.23s, bán kính 18m) kết hợp `Ice Impact.prefab` và Vòng xoáy xung kích phát sáng xoay kim đồng hồ.
    + `RunAttack` *(Phím 5)*: Boss lao nhanh về phía trước húc thẳng vào Player (`runAttackSpeed = 12m/s`, `runAttackDamage = 35`, knockback `5.0`), hỗ trợ slot `Run Aura Vfx Prefab` trống (`None`) để người dùng tự do gán hiệu ứng hào quang bao quanh Boss khi chạy, loại bỏ hoàn toàn vệt xanh lá kéo dài cũ.
    + `AttackLT_IG` *(Phím 6)*: Chuỗi combo 3 đòn đánh thường liên hoàn cực đẹp (`AttackLT_IG.anim`), gây sát thương tăng dần (20, 20, 30 HP) kèm rung chấn camera nhẹ, hỗ trợ slot `Hand Aura Vfx Prefab` trống (`None`) trên Inspector để người dùng tự do kéo thả VFX bao quanh 2 bàn tay, loại bỏ hoàn toàn khói và vòng tròn xanh lỗi.
    + `IceBallIG` *(Phím 7)*: Boss giơ hai tay lên cao tụ quả cầu năng lượng `Purple Orb.prefab` **lơ lửng chính giữa hai bàn tay trên trời** (`GetBetweenHandsPosition()`) kết hợp 2 quả cầu nhỏ ở 2 lòng bàn tay phát sáng; quả cầu bám theo nhịp tay vươn lên cao và phình to dần từ 0.4 lên 2.2 trong 1.15s tụ lực, sau đó Boss bổ hai tay phóng quả cầu bay thẳng tới Player với vận tốc 16m/s và phát nổ diện rộng khi va chạm (35 HP + rung màn hình).
    + `JumpDown` *(Mở màn duy nhất 1 lần khi phát hiện Player / One-Time Intro Cutscene)*: Cú đáp đất giáng trần rền vang từ trên cao (`jumpDownDamage = 35`, rơi 1.25s, bán kính 16m) kết hợp `Ground Impact.prefab` và Vòng sóng xung kích lan tỏa từ chân Boss. Tự động kích hoạt khi Boss lần đầu tiên phát hiện Player bước vào phòng hầm ngục (`playerDetectionRange = 35m`). Sau khi tiếp đất thành công, cờ `hasTriggeredJumpDown = true` khóa vĩnh viễn chiêu thức này trong toàn bộ trận đấu, không thể dùng đi dùng lại (đã loại bỏ phím Space để tránh spam lại). Mọi cú nhảy áp sát trong giao tranh được chuyển giao hoàn toàn cho `JumpAttack` (Phím 4) có cooldown chuẩn Souls-like.
    + `Di chuyển 4 hướng thông minh`: Tự động nhận diện hướng di chuyển `MoveType` (0 = Idle, 1 = Tiến `Walk`, 2 = Lùi `WalkBackIG`, 3 = Trái `WalkLeftIG`, 4 = Phải `WalkRightIG`) kết hợp cơ chế vờn quanh khóa mặt bám sát Player (`strafeMode = true`). Hỗ trợ điều khiển thủ công bằng **4 phím mũi tên (Arrow Keys: Up, Down, Left, Right)** tách biệt 100% với phím W, A, S, D của Player.
  - **Phản Hồi Combat Souls-like & Chuẩn Bị Cho ML-Agents (ML-02):**
    + *Chớp đỏ GPU khi trúng kiếm:* Tự động liên kết `Health.onTakeDamage` và điều khiển toàn bộ MeshRenderers/SkinnedMeshRenderers qua `MaterialPropertyBlock` (thời gian 0.12s, màu đỏ tươi), bảo toàn 100% SRP Batcher.
    + *Xử lý tử trận chuẩn xác:* Tự động liên kết `Health.onDeath` -> `Die()` kích hoạt trigger animation `Dead`, vô hiệu hóa Collider và Rigidbody để ngưng nhận đòn, dọn dẹp effect lửa/aura.
    + *Hệ thống Thanh chịu đòn Poise (500) & Suy yếu (WeakenIG):* Boss sở hữu 500 điểm Poise, bị bào mòn theo độ uy lực đòn đánh của Player (35 - 100 Poise). Khi Poise = 0 kích hoạt WeakenIG bước lùi hết trọn vẹn clip hoạt ảnh duy nhất 1 lần và lập tức tiếp tục chiến đấu tại vị trí mới; nếu máu tụt <= 50% HP hủy ngay để chuyển Phase 2 cuồng nộ.
    + *Hệ thống Cooldowns & API cho AI / ML-Agents:* Bổ sung thời gian hồi chiêu cho 7 kỹ năng (`attack1: 1.5s`, `gumo: 6.0s`, `flame: 8.0s`, `jump: 7.0s`, `run: 6.0s`, `attackLT: 5.0s`, `iceBall: 7.0s`) cùng các API `CanCastAttack`, `GetCooldownNormalized`, `GetRemainingCooldown` phục vụ trực tiếp vector quan sát cho Agent ML-02.
    + *Khóa mục tiêu & Di chuyển thông minh:* `autoFindPlayer = true`, `alwaysFacePlayer = true`, `facePlayerSpeed = 240 deg/s`, `facePlayerOnAttack = true`, `strafeMode = true`.
  - **Công Cụ Tự Động Hóa Unity Editor:** Tích hợp tool `IceGolemSetupEditor.cs` tại 3 menu:
    * `Infinite Labyrinth -> Boss -> 1. Cấu Hình Hoàn Thiện Prefab Boss Ice Golem (BOSS-01)`
    * `Infinite Labyrinth -> Boss -> 2. Tự Động Nối Dây Animator Boss (BossController)` hỗ trợ nối dây tự động 1 click toàn bộ 4 hướng di chuyển và 3 kỹ năng mới vào Animator.
    * `Infinite Labyrinth -> Boss -> 3. Dọn Dẹp Scene & Khắc Phục Lỗi Ice Golem` tự động quét sạch các GameObject lỗi thời (như Purple Orb lơ lửng), clone thừa, và dọn dẹp component trùng lặp trong Scene.

- [x] **BOSS-02: Chuẩn Hóa Khung Xương, Vật Lý Áo Choàng & Vũ Khí Trùm Red Ignis (`Boss_RedIgnis.prefab`)**
  - Khắc phục triệt để lỗi vật lý tà áo `UnityChanSpringBone`: Dọn sạch mảng null (19 -> 12 xương), gỡ bỏ 4 xương ngọn leaf bones lỗi, vô hiệu hóa khóa góc xoay và sàn ảo ảo giác, viết tool tự động `SpringManagerHelper.cs`.
  - Khắc phục lỗi lún đất & co ngắn chiều cao: Ánh xạ chuẩn xương `Hips -> mixamorig:Hips` trong `RedIgnis.fbx`, chuẩn hóa hoạt ảnh bước đi `Ignis_Walk.fbx` với `Based Upon = Feet / Center of Mass` và bù trừ cao độ Offset Y.
  - Tích hợp Vũ khí Kiếm Siu Nhân Đỏ (`KiemSiuNhanDo.fbx`): Cài đặt Import Settings chuẩn vật thể tĩnh (`Animation Type: None`), gắn vào lòng bàn tay phải qua `RightHand_Socket` trên xương `mixamorig:RightHand`.

- [x] **ML-01: Tích hợp Unity ML-Agents Package & Môi Trường Huấn Luyện**
  - Đã tích hợp gói chính thức com.unity.ml-agents: 4.1.0 (khớp chuẩn Unity 6 và Unity AI Inference 2.6.1) vào thư mục Packages/com.unity.ml-agents.
  - Cấu hình môi trường Python 3.10 thành công với mlagents: 1.1.0, PyTorch 	orch: 2.14.1, và setuptools: 69.5.1. Kiểm tra lệnh mlagents-learn --help hoạt động hoàn hảo.
  - Chuẩn bị file cấu hình huấn luyện PPO: [MLAgents/Config/IceGolem_PPO.yaml](file:///c:/Users/Admin/Documents/DoAnTotNghiep/DoAnTotNghiep/MLAgents/Config/IceGolem_PPO.yaml) (Behavior: IceGolemSoulsAI, batch_size: 256, buffer_size: 2048, hidden_units: 256, num_layers: 3).

- [x] **ML-02: Thiết kế Agent Trùm Băng Khống Chế & Souls-like (IceGolemAgent.cs & IceGolemBattleArena.cs)**
  - **Kiến trúc Brain - Body tách biệt:**
    + Body: [IceGolemController.cs](file:///c:/Users/Admin/Documents/DoAnTotNghiep/DoAnTotNghiep/Assets/Characters/Boss/Script_Boss/IceGolemController.cs) quản lý 7 chiêu thức, VFX, hoạt ảnh, di chuyển 4 hướng qua Move(horiz, vert) và CastAttack(skillIndex), bổ sung ResetAllCooldowns().
    + Brain: [IceGolemAgent.cs](file:///c:/Users/Admin/Documents/DoAnTotNghiep/DoAnTotNghiep/Assets/Characters/Boss/Script_Boss/IceGolemAgent.cs) kế thừa Unity.MLAgents.Agent.
  - **TUYỆT ĐỐI KHÔNG SỬA BẤT KỲ SCRIPT NÀO CỦA PLAYER:**
    + Toàn bộ thông tin đối thủ Player (Health, PlayerController, PlayerCombat, PlayerStats) chỉ được đọc qua các public properties có sẵn dạng **Read-Only 100%**.
  - **Vector Observations (29 chiều toàn diện):**
    + Vị trí tương đối tới Player (X, Z chuẩn hóa), Cự ly thực tế tới Player.
    + Góc quay mặt Boss so với Player (Cos & Sin liên tục).
    + Máu hiện tại của Boss và trạng thái bận IsBusy.
    + Thời gian hồi chiêu chuẩn hóa của cả 7 chiêu thức (0 = sẵn sàng, 1 = đang hồi).
    + Máu và Thể lực (Stamina) hiện tại của Player.
    + Trạng thái Player: IsAttacking, IsBlockingOrParrying, IsRolling (I-Frames), IsInRollRecovery (cửa sổ Roll-Catch 0.4s), IsGrounded.
    + Vận tốc di chuyển tương đối của Player (dọc & ngang theo góc nhìn Boss).
    + 4 tia cảm biến Raycast khoảng cách tới tường đấu trường (Trước, Sau, Trái, Phải).
    + Vị trí Boss so với tâm đấu trường (Arena Center).
  - **Action Space (3 Discrete Branches linh hoạt):**
    + Nhánh 0 - Di chuyển & Vờn quanh (5 lựa chọn): 0 = Đứng yên, 1 = Tiến áp sát, 2 = Lùi giữ khoảng cách, 3 = Dạt trái (Circling), 4 = Dạt phải (Circling).
    + Nhánh 1 - Ra đòn chiến đấu (8 lựa chọn): 0 = Quan sát/Không đánh, 1 = Attack 1 (đấm cận chiến), 2 = GumoGumo (đấm đất liên hoàn), 3 = Flame Breath (phun lửa nón), 4 = Jump Attack (vồ áp sát), 5 = Run Attack (chạy húc 12m/s), 6 = Attack LT (combo 3 đòn), 7 = Ice Ball (ném cầu tím tầm xa).
    + Nhánh 2 - Điều tiết nhịp độ (Timing Mixup - 2 lựa chọn): 0 = Ra đòn lập tức, 1 = Hoãn đòn 0.35s (Delayed Attack - bẫy roll panic của Player).
  - **Souls-like Reward Shaping:**
    + +1.5 Thưởng Roll-Catching khi đánh trúng Player ngay lúc vừa kết thúc I-Frames lăn né.
    + +2.0 Thưởng Heal-Punish khi trừng phạt Player đang cố uống máu/hồi phục.
    + +1.0 Thưởng khi gây sát thương lên Player; +5.0 Thưởng lớn khi hạ gục Player.
    + -0.5 Phạt khi tung đòn vào lúc Player đang lăn né (I-Frames); -0.4 Phạt đánh trượt xa (Whiffing).
    + -0.8 Phạt khi bị Player chém trúng; -3.0 Phạt khi Boss bị hạ gục.
    + +0.002/step Khuyến khích giữ cự ly lý tưởng (3.5m - 8m) và vờn quanh (Circling).
  - **Cơ Chế Đấu Hiệp Liên Hoàn Không Game Over (IceGolemBattleArena.cs):**
    + Dành riêng cho trải nghiệm Người Chơi tự tay cầm kiếm đánh với Boss.
    + Khi Player tụt máu <= 5 HP, đấu trường chặn lại, trao chiến thắng cho Boss, tự động hồi đầy máu cho cả 2 và xếp lại vị trí cách nhau 12m để bắt đầu hiệp mới mượt mà.
    + Khi Boss cạn máu, tự động hồi máu cho Boss và bắt đầu hiệp tiếp. Không bao giờ làm chết kẹt Player hay cần reload Scene.
  - **Công Cụ Tự Động Hóa Unity Editor:**
    + Thêm menu Infinite Labyrinth -> Boss -> 5. Tự Động Gắn & Cấu Hình ML-Agents Cho Boss Ice Golem trong [IceGolemSetupEditor.cs](file:///c:/Users/Admin/Documents/DoAnTotNghiep/DoAnTotNghiep/Assets/Scripts/Editor/IceGolemSetupEditor.cs).
    + Tự động gắn và cấu hình IceGolemAgent, BehaviorParameters (29 obs, [5, 8, 2] actions), và DecisionRequester (Period = 5) trên cả Prefab lẫn Scene.
    - **Tích hợp Bot Tự Động Chiến Đấu (PlayerBotAI.cs) Phục Vụ Tự Luyện ML-Agents:**
    + Viết script [PlayerBotAI.cs](file:///c:/Users/Admin/Documents/DoAnTotNghiep/DoAnTotNghiep/Assets/Scripts/Player/PlayerBotAI.cs): Bot tự động chiến đấu (Sparring Partner) với Boss thông qua FSM đa dạng (tiến áp sát, chém combo 1-2-3, lùi giữ cự ly hồi thể lực, lăn né khi Boss vung đòn, giơ kiếm parry, và lùi xa uống máu để kích hoạt thử thách Heal-Punish cho Boss).
    + Cơ chế công tắc phím nóng B (Toggle Bot Mode): Nhấn phím [B] để Bật/Tắt tức thì. Khi tắt, bot ngủ đông 100%, trả lại quyền điều khiển bàn phím/chuột cho người chơi.
    + Bảo toàn 100% code gốc Player: Sử dụng Reflection an toàn để gọi các hàm chiến đấu mà không sửa dù chỉ 1 dòng code trong PlayerController.cs hay PlayerCombat.cs.
    + Tích hợp menu Editor Infinite Labyrinth -> Boss -> 6. Gắn Bot AI Cho Player (Phím B Bật/Tắt Sparring) tự động gắn vào cả Prefab lẫn Scene.
  - **Chế Độ Điều Khiển Thủ Công (Heuristic Mode):**
    + Người dùng có thể tự lái Boss bằng 4 phím Mũi Tên và phím 1..7 (giữ Right Shift để đánh trễ) bất kỳ lúc nào để thử nghiệm phản xạ và hành vi.

- [ ] **ML-03: Thiết kế Agent Kiếm Thuật Cơ Động & Tái Sinh Đổi Dạng 2 Pha (`RedIgnisAgent.cs`)**
  - **Cơ Chế Tái Sinh Đổi Diện Mạo (Phase 2 Rebirth & Appearance Transformation):**
    - Khi thanh máu Phase 1 chạm 0 -> Kích hoạt animation quỵ ngã, bật cờ bất tử tạm thời (`invulnerable = true`), phát nổ sóng nhiệt đẩy lùi Player.
    - **Lột xác diện mạo mới:** Hoán đổi sang Model/Mesh thức tỉnh (hoặc kích hoạt hào quang lửa bùng cháy, cánh lửa, kiếm rực lửa), hồi đầy 100% máu và mở khóa danh hiệu Phase 2.
  - **Observations (27 chiều):** Bổ sung biến đầu vào `isPhase2` (0 hoặc 1) để mạng nơ-ron nhận thức hình thái hiện tại.
  - **Actions & Phong cách chiến đấu 2 Pha:**
    - *Phase 1 (The Crimson Duelist):* Vờn quanh cự ly 4-6m, lướt chém đơn, combo 3 nhát, rình sơ hở.
    - *Phase 2 (True Flame Ignis):* Tốc độ di chuyển tăng 30%, mở khóa chiêu lướt chém để lại vệt lửa bốc cháy mặt sàn (Fire Trail), combo cuồng bạo liên tục phá thủ, bắt nhịp lăn (Roll-Catch) cực gắt.
  - **Reward Function:** Thưởng dồn ép phá vỡ thế thủ, thưởng đánh trúng bằng vệt lửa diện rộng, thưởng bắt roll, phạt chém hụt.

- [ ] **ML-04: Huấn Luyện Đa Sàn Đấu & Đóng Gói Model ONNX Thời Gian Thực**
  - Dựng 8 đến 16 sàn đấu Arena chạy song song trong Scene huấn luyện để tăng tốc độ hội tụ gấp 10 lần (`--time-scale=10`).
  - Huấn luyện PPO qua `mlagents-learn` với file cấu hình `icegolem_config.yaml` và `redignis_config.yaml`.
  - Đóng gói file `.onnx` xuất xưởng, nạp vào `Behavior Parameters` (chế độ `Inference Only`) chạy mượt mà 100% không phụ thuộc Python.

---

## 🚩 GIAI ĐOẠN 5: Giao Diện Hoàn Chỉnh & Âm Thanh (UI & Polish)

- [ ] **POL-01: Hệ thống Menu** (MainMenu, Settings, PauseMenu).
- [ ] **POL-02: Âm thanh (SFX & BGM)** (Nhạc nền Làng yên bình, Nhạc nền Hầm ngục rùng rợn, Nhạc đấu Boss dồn dập; Tiếng va chạm kim loại, tiếng quái gầm).
- [x] **POL-03: Bản đồ Hầm ngục Toàn màn hình & Khám phá Sương mù (Fullscreen Procedural Dungeon Map)**
  - **Kiến trúc & Điều khiển (`DungeonMapManager.cs`):**
    + Bật/tắt bằng phím nóng `M`, hỗ trợ phím `Esc` để đóng nhanh. Bản đồ nằm ở đáy Canvas (`SetAsFirstSibling()`) bên dưới toàn bộ HUD/UI khác.
    + Khi mở bản đồ, tự động đặt mức zoom mặc định là 2.0x (`defaultZoomOnOpen = 2.0f`) và căn giữa ngay vị trí người chơi (`centerOnPlayerOnOpen = true`), người chơi có thể tự do phóng to/thu nhỏ bằng con lăn chuột trong khoảng `1.0x` (`minZoom`) đến `3.5x` (`maxZoom`) và kéo chuột trái (Pan). Tự động giữ ổn định tâm nhìn của camera map khi zoom (`panOffset *= newZoom / currentZoom`).
    + Hiển thị vị trí người chơi thời gian thực (`PlayerMarker`) cố định hướng thẳng đứng mặc định theo yêu cầu.
  - **Dựng hình Phòng & Hành lang chuẩn BSP:**
    + Dựng chính xác tỷ lệ và tọa độ theo kết quả phân hoạch BSP (`DungeonData`), bảo toàn hình dạng phòng chữ nhật góc vát sắc nét (`roomBorderMultiplier = 16`).
    + Tự động ghép nối các mối nối **góc vuông chữ L**, **ngã ba chữ T** và **ngã tư 4 hướng** (`Corridor_Border_L/T`, `Corridor_Fill_L/T`) xoay tự động theo 4 hướng, áp dụng chuẩn **9-slice** (`border = 120`) đồng bộ hoàn hảo với hành lang thẳng.
    + Tích hợp cơ chế thu nhẹ chiều dài đầu hành lang (`corridorLengthTrim = 6f`, 3px mỗi đầu) kết hợp lớp đệm đáy `FloorBase` đồng bộ màu khám phá (`theme.fillColor`), xóa sổ 100% các đường viền vàng cắt ngang ngã rẽ và các mấu nhô thừa.
    + Sửa thuật toán `ConnectStartRoom` trong [`BSPGenerator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/BSPGenerator.cs) cắm chuẩn xác vào mép cửa dưới phòng khởi đầu (`closestRoom.y`), triệt tiêu hoàn toàn bóng hành lang đâm xuyên vào lòng sàn phòng.
    + Căn tâm đoạn hành lang trong [`BSPGenerator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/BSPGenerator.cs) và triệt tiêu `corridorOverlapExtension` dư thừa, loại bỏ hoàn toàn các mấu nhô thừa thành dấu `+`.
    + Thu hẹp khoảng cách gom cụm điểm giao (`<= 0.5f`) và hạ ngưỡng nhánh (`> 0`), đảm bảo 100% khúc cua L và ngã ba/tư đều được sinh block nối chính xác, không bị nuốt block hoặc nhô viền đen.
    + Bổ sung nhóm cài đặt Inspector tùy chỉnh trực tiếp cho mối nối (`junctionSizeOffset`, `enableJunctionFloorBase`, `junctionFloorBasePadding`) với `junctionFloorBasePadding = -3f` thu gọn mép nền lùi sâu vào sau viền vàng, ngăn chặn triệt để hiện tượng phình to nền đen ra ngoài.
    + Tách riêng kích thước icon Boss (`bossIconSize`, Range 16-96px, mặc định 48px) độc lập với các icon phòng thông thường (`roomIconSize = 36px`), cập nhật trực tiếp theo thời gian thực trong Play Mode qua `OnValidate()`.
    + Khắc phục triệt để hiện tượng đứt gãy hành lang dẫn vào phòng Boss trong [`BSPGenerator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/BSPGenerator.cs) (`ConnectGrandBossAvenue`): Loại bỏ khối `vestibule` 6x2 thừa thãi; thiết lập hệ thống nối chuẩn mực cắm sâu 2 ô vào đáy đại lộ (`aveY + 2`), đồng thời kéo dài đại lộ cắm ngập 2 ô vào lòng phòng Boss (`aveLen + 2`) và bỏ `corridorLengthTrim` cho đại lộ trong [`DungeonMapManager.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/UI/DungeonMap/DungeonMapManager.cs), giúp đại lộ rộng 12m kết nối liền lạc 100% không một khe hở.
    + Khắc phục triệt để lỗi hành lang gãy khúc 1 ô làm 2 khối nối L chồng chéo lên nhau (`CreateCorridors` trong [`BSPGenerator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/BSPGenerator.cs)): Tự động kiểm tra vùng giao thoa của 2 phòng để ép thẳng hàng 100% (Snap to Straight) thành một đoạn thẳng duy nhất không có mối nối thừa; trường hợp bắt buộc phải rẽ chữ L (không giao nhau), thuật toán tự động ép cự ly uốn tối thiểu `>= corridorThickness + 2` ô (>= 4 ô = 8m) để 2 khối nối L luôn cách biệt rõ ràng, không bao giờ bị đè dính lên nhau.
  - **Cơ chế Khám phá & Sương mù (Fog of War):**
    + Trạng thái 3 cấp độ: *Chưa khám phá* (Unexplored - mờ tối), *Lân cận* (Adjacent - biết có phòng, sáng vừa), *Đã khám phá* (Explored - sáng rực vàng gold).
    + Kích hoạt tự động Phòng khởi đầu (`StarterRoom`) và các hành lang dẫn vào phòng đầu tiên khi bắt đầu màn chơi.
    + Khi người chơi bước vào phòng mới, kích hoạt sáng bừng phòng hiện tại kèm highlighter, mở sáng toàn bộ chuỗi hành lang liên hoàn (L-shape, Z-shape) và đánh dấu mờ các phòng lân cận qua đồ thị kề BFS (`adjacency`).
    + Luôn hiển thị vị trí các Icon chức năng quan trọng: Phòng Boss (`Boss_Icon`), Phòng Chìa Khóa Tế Đàn duy nhất (`Key_Icon`), Phòng Kho Báu (`Treasure_Icon`).
    + **Hệ thống Phân Bổ Phòng Kho Báu Cân Bằng (`deadEndTreasureChance = 0.75f`, `minTreasureRooms = 3`):** Cấu hình trực tiếp trên Inspector [`BSPGenerator.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/BSPGenerator.cs) và thuật toán phân loại trong [`DungeonRoomAnalyzer.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/DungeonRoomAnalyzer.cs): Thiết lập tỷ lệ **75% cho các phòng ngách cụt (Dead-Ends)** trở thành Phòng Kho Báu (vẫn giữ mục tiêu tối thiểu 3 phòng nếu đủ ngách cụt). Loại bỏ hoàn toàn việc biến các phòng hành lang thông thường thành kho báu, giúp mật độ phòng kho báu luôn hài hòa (3 - 4 phòng).
    + **Thử Thách Lính Gác Phòng Kho Báu (`treasureGuardSquadSize = SquadSize.Quad`):** Trong [`ProceduralEnemySpawner.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/DungeonGeneration/ProceduralEnemySpawner.cs), phòng kho báu luôn chắc chắn sinh **ít nhất 4 quái tinh nhuệ** (1 Warrior, 1 Minion, 1 Rogue, 1 Mage) canh giữ rương báu (kèm thêm lính bắn tỉa nếu phòng có gác lửng tầng 2), gia tăng độ kịch tính và thử thách trước khi người chơi mở hòm báu.
    + Toàn bộ bảng màu hiển thị có thể tùy chỉnh trực tiếp trên Inspector và cập nhật ngay trong Play Mode.

---

## 🚩 GIAI ĐOẠN 6: Công Cụ Kiểm Thử & Quản Trị Viên (Admin & Debug Tooling)

- [x] **ADM-01: Lập trình Bộ Não Điều Hành Cheat, Safe Teleport & ReGenerate Hầm Ngục (`AdminCheatManager.cs`)**
  - Quản lý Singleton `AdminCheatManager.Instance` hỗ trợ toàn cục trong game.
  - Ma trận Dịch chuyển tức thời an toàn (Safe Teleport Matrix): Nắn chuẩn mặt sàn NavMesh và tạm ngắt `CharacterController` chống giật lag/rơi khỏi map:
    + Teleport về Phòng Khởi Đầu (`TeleportToStartRoom`).
    + Teleport về Đấu Trường Boss (`TeleportToBossRoom`).
    + Teleport về Phòng Chìa Khóa / Tế Đàn (`TeleportToKeyRoom`).
    + Teleport xoay vòng các Phòng Kho Báu (`TeleportToTreasureRoom`).
    + Teleport lên Gác Lửng Tầng 2 (`TeleportToMezzanine`).
  - Cơ chế Tái sinh Hầm ngục tức thời trong Play Mode (`RegenerateDungeon`): Tự động dọn quái cũ, dọn sạch props và geometry cũ, sinh seed mới, nướng lại NavMesh, đưa Player về Start an toàn không cần dừng Play Mode.
  - Trừng phạt & Tiêu diệt quái vật: Tiêu diệt quái trong bán kính xung quanh Player (`KillEnemiesInRadius`) và diệt sạch toàn bộ hầm ngục (`KillAllEnemies`).
  - Tích hợp kiểm tra `AdminCheatManager.IsOpen` vào [`ThirdPersonCamera.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/ThirdPersonCamera.cs) chống xung đột khóa chuột khi thao tác.

- [x] **ADM-02: Bộ Cheats Chiến Đấu, Sinh Tồn & Tiến Trình RPG (Combat, God Mode & RPG Progression)**
  - **Player God Mode (Bất Tử 100%):** Tích hợp cờ tĩnh `AdminCheatManager.IsGodMode` trực tiếp vào [`Health.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Health.cs), miễn nhiễm toàn bộ sát thương đòn đánh và tự động nạp đầy thanh máu.
  - **Vô Hạn Thể Lực (Infinite Stamina):** Tích hợp cờ `AdminCheatManager.IsInfiniteStamina` vào [`PlayerStats.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerStats.cs) (`HasStamina`, `ConsumeStamina`, `ConsumeStaminaContinuous`), lăn né và chạy nước rút không giới hạn.
  - **Vô Hạn 3 Sao Kỹ Năng (Infinite Spirit Stars):** Tích hợp cờ `AdminCheatManager.IsInfiniteSpirit` nạp đầy 150/150 Spirit trên [`PlayerStats.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerStats.cs), sẵn sàng spam liên tục chiêu E (Sóng kiếm) và Q (Nổ năng lượng).
  - **Hồi Phục Toàn Diện (`FullHealAndRestore`):** Khôi phục tức thì 100% Máu, 100% Thể lực và 3 Sao Năng Lượng trong 1 nút bấm.
  - **Tăng Tốc Di Chuyển Siêu Tốc (`CycleSuperSpeed`):** Bổ sung thuộc tính `SpeedMultiplier` trên [`PlayerController.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerController.cs) cho phép luân chuyển 4 mức tốc độ: 1.0x -> 1.8x -> 2.5x -> 3.5x.
  - **Đóng Băng AI Quái Vật (`ToggleFreezeEnemies`):** Vô hiệu hóa AI `EnemyAIBase` và dừng `NavMeshAgent` toàn bộ quái vật lẫn Boss Ice Golem, biến quái thành bao cát tập luyện tĩnh để kiểm tra combo kiếm 3 đòn, đòn chém gồng, parry timing, vỡ thế poise và nháy trắng GPU.
  - **Sinh Quái Thử Nghiệm 1v1 (`SpawnTestEnemy`):** Tự động sinh quái (Warrior, Minion, Rogue, Mage) cách Player 3.5m trên sàn NavMesh.
  - **Bộ Cheats Kinh Tế & Tiến Trình RPG:**
    + Thêm Chìa Khóa Hầm Ngục (`AddBossKey`): Bỏ thẳng `Item_Key_Dungeon` vào [`InventoryManager.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Inventory/InventoryManager.cs) để mở Đại Môn Boss mà không cần tìm nhặt.
    + Thêm Vàng (`AddGold`): Thêm +1.000 hoặc +10.000 Vàng để test Thợ rèn và tẩy điểm.
    + Thêm EXP (`AddExp`): Thêm +500 EXP vào [`PlayerProgression.cs`](file:///c:/2.tapcodedienloan/!%20Đồ%20án%20tốt%20nghiệp/Infinite%20Labyrinth/Assets/Scripts/Player/PlayerProgression.cs) để test lên cấp và xem vòng tròn chữ C `ExpRingFill`.
    + Thêm Điểm Tiềm Năng (`AddAttributePoints`): Thêm +10 điểm tiềm năng chưa phân bổ để test bảng modal thuộc tính.
    + Thêm Bình Máu (`AddHealthPotions`) & Hỏa Long Kiếm Legendary (`AddLegendarySword`).

- [x] **ADM-03: Giao Diện Người Dùng Admin Console UI (`AdminCheatUI.cs`) & Bật/Tắt Phím `~` (Tilde / Backquote)**
  - Chuẩn hóa 100% giao diện sang Tiếng Anh (English UI): Tabs, Buttons, Subtitles, Badges, Toast & Log Messages.
  - Bật/tắt mở đóng đồng bộ bằng phím `~` (Tilde / Backquote) hoặc phím `F1`, hỗ trợ phím `Esc` hoặc nút [✕] để đóng nhanh. Loại bỏ nút nổi góc trái để màn hình chơi game luôn gọn gàng.
  - Đồng bộ đóng/mở Cửa sổ Debug thông tin góc trên-phải (`MiniDebugOverlay`) cùng lúc với bảng Admin Console (khi ấn `~` cả 2 cùng hiện, khi đóng cả 2 cùng ẩn).
  - Tối ưu màu sắc Thanh điều hướng Navbar: Các nút tab khi được chọn (`isSelected`) luôn giữ màu chữ trắng tinh (`Color.white`) trên nền vàng hổ phách nổi bật, đảm bảo độ tương phản cao và cực kỳ dễ đọc.
  - Tối ưu tọa độ Dịch chuyển tức thời an toàn (Safe Offset Teleport):
    + Phòng Khởi Đầu: Dịch chuyển lùi về phía sau cửa 4.5m trong sảnh đón (`startDoorMarker.position - forward * 4.5f`), xoay mặt nhìn thẳng vào cửa hầm ngục.
    + Đấu Trường Boss: Dịch chuyển đứng trước Đại Môn Boss 5.5m trên Đại Lộ (`gateZ - 5.5f`), xoay mặt nhìn thẳng vào cổng vòm uy nghi để sẵn sàng giữ F mở cửa bằng chìa khóa.
  - Thiết kế bảng điều khiển phong cách Dark RPG bán trong suốt (460x620px), phân loại 5 tab chức năng:
    + **Tab 1: Teleport:** Start Room, Boss Arena, Key Shrine, Treasure Rooms, Mezzanine 8m.
    + **Tab 2: Dungeon:** Regenerate Dungeon (New Seed), Kill Nearby (25m), Kill All Enemies.
    + **Tab 3: Player:** God Mode (Invulnerable), Infinite Stamina, Infinite Spirit, Full Heal & Restore, Move Speed Multiplier (1.0x - 3.5x).
    + **Tab 4: Monsters:** Freeze Enemy AI (Training Dummy), Spawn Skeleton Warrior, Minion, Rogue, Mage.
    + **Tab 5: RPG & Loot:** Add 1 Boss Key, Add +1,000 / +10,000 Gold, Add +500 EXP, Add +10 Attribute Points, Add 5 Health Potions, Add Dragonic Sword.
  - Tự động cập nhật nhãn trạng thái ON/OFF thời gian thực (`[ON]`, `[OFF]`, `[FROZEN]`, `[ACTIVE]`).
  - Thanh thông báo Toast Notification (`toastText`) hiển thị phản hồi tức thì bằng tiếng Anh mỗi khi thực thi cheat.

- [x] **ADM-04: Công Cụ Tự Động Hóa 1-Click Unity Editor (`AdminCheatSetupEditor.cs`) & Kiểm Thử Toàn Diện**
  - Tích hợp MenuItem trên thanh công cụ Unity: `Infinite Labyrinth -> Admin -> 1. Setup Admin Cheat Console (Hotkey ~)`.
  - Tự động tạo hoặc kiểm tra `AdminCheatManager` trong Scene, nạp đầy đủ 5 ScriptableObjects Item mẫu và 4 Prefabs quái.
  - Tự động dựng toàn bộ cây Canvas UI (`AdminCheatCanvas`, `AdminCheatPanel`), cài đặt EventSystem, nối dây 100% các nút bấm và nhãn TextMeshProUGUI.
  - Tự động đóng gói và xuất thành Prefab tái sử dụng: `Assets/Prefabs/Admin/AdminCheatCanvas.prefab`.

