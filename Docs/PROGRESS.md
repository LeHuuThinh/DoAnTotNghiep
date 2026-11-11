# Nhật Ký Tiến Độ Dự Án (Progress Log) - Infinite Labyrinth

> **Cập nhật ngày:** 06/11/2026  
> **Trạng thái đối chiếu:** Đã rà soát trực tiếp 100% mã nguồn C#, Prefabs, Scenes và Git Commits trên ổ đĩa.

---

## 1. Bảng Tổng Hợp Tính Năng Hiện Có (Verified Features)

| Phân hệ | Tính năng | Trạng thái | Ghi chú kỹ thuật |
| :--- | :--- | :---: | :--- |
| **PCG Dungeon** | Thuật toán BSP chia phòng & hành lang | ✅ Hoàn thành | `BSPGenerator.cs`, `BSPNode.cs` (Hỗ trợ map 150x150 tiles = 300x322m) |
| **PCG Dungeon** | BFS / Flood-Fill tìm phòng Boss xa nhất | ✅ Hoàn thành | `DungeonRoomAnalyzer.cs` (set trần cao 16m vòm đấu trường) |
| **PCG Dungeon** | Phòng Gác lửng (Mezzanine tầng 2) & Cầu thang 3D | ✅ Hoàn thành | Dựng Mesh 3D dốc nối sàn 1 và sàn 2, sàn dầm đà và lan can |
| **PCG Dungeon** | Rải đạo cụ ngẫu nhiên theo chủ đề phòng | ✅ Hoàn thành | `DungeonPropDecorator.cs` (6 themes: Barracks, Library, Ruins, GrandHall...) |
| **PCG Dungeon** | Cụm Kiến trúc tàn tích (8 mẫu) & Tiểu cảnh đạo cụ (5 mẫu) | ✅ Hoàn thành | Bounding Box footprint tự động, xoay 4 hướng, cấm đè props (`forbiddenTiles`) |
| **PCG Dungeon** | Trang trí Gác lửng Tầng 2 khớp cao độ 8m | ✅ Hoàn thành | Cờ hiệu banner cao thấp ngẫu nhiên, khiên kiếm, bàn ghế ngắm cảnh, rải rác sàn |
| **PCG Dungeon** | Đuốc sàn tự do (Floor Torch) & Đuốc tường an toàn | ✅ Hoàn thành | Nâng offset Y 0.5m chống lún, rải sàn 2 tầng, kiểm tra tường đặc triệt tiêu lơ lửng |
| **PCG Dungeon** | Gộp Mesh Props tĩnh theo Material (Mesh Combine) | ✅ Hoàn thành | 1 Draw Call duy nhất mỗi nhóm vật liệu, tắt Shadow Casting, lọc bỏ MeshCollider thừa |
| **PCG / Optimization** | Tối ưu hóa hiệu năng URP & Hệ thống Ánh sáng (110+ FPS) | ✅ Hoàn thành | `DungeonLightOptimizer.cs`: Cull kép Point Light & Particle khói/lửa; Triệt tiêu Shadow Caster Directional Light; Hysteresis Buffer 6m; Camera Far Clip 200m |
| **Combat / UI** | PlayerHUD hiển thị đầy đủ 3 thanh tài nguyên Souls-like | ✅ Hoàn thành | `PlayerHUD.cs`: Máu (kèm Ghost Bar tụt trễ), Thể lực Stamina, Cụm 4 Sao Spirit nạp chiêu thức E/Q/R |
| **Combat** | Hệ thống Poise (Khựng đòn Souls-like) | ✅ Hoàn thành | Cài đặt trong `PlayerFeedback` & `EnemyFeedback` |
| **Combat** | Quản lý Thể lực (Stamina) & Tiêu hao | ✅ Hoàn thành | `PlayerStats.cs` (Roll, Sprint, Attack, Parry) |
| **Combat** | Năng lượng Chiêu thức (Spirit / Focus) | ✅ Hoàn thành | `PlayerStats.cs` (Tích lũy khi chém/parry, dùng E/Q/R) |
| **Combat** | Hệ thống Đỡ đòn & Phản công (Parry & Counter) | ✅ Hoàn thành | `PlayerCombat.cs` (Cửa sổ parry 0.28s) |
| **Combat** | Nhảy, Rơi tự do & Phân biệt Soft/Hard Landing | ✅ Hoàn thành | `PlayerController.cs` + Animator State Machine |
| **Combat** | Khóa mục tiêu (Target Lock Souls-like) | ✅ Hoàn thành | `TargetLockSystem.cs` vờn quái |
| **Combat / Balance** | Chuẩn Hóa Công Thức Phòng Thủ ARPG & Scale Sát Thương Skill E, Q, Charge Attack | ✅ Hoàn thành | Health.cs (Công thức DR % chuẩn ARPG với K=100), EnemyAIBase.cs (Quái 5-15 DEF), IceGolemController.cs (35 DEF), RedIgnisController.cs (25 DEF), ProjectileSlash.cs (+1.4x ATK), AoeSpell.cs (+2.0x ATK), PlayerCombat.cs (Charge Attack Base 70) |
| **Combat / VFX** | Hệ Thống Số Sát Thương Bay Lên (Floating Damage Numbers / Pop & Physics Arc) | ✅ Hoàn thành | DamageNumberManager.cs (Object Pool 40 TextMeshPro 3D), DamageNumberItem.cs (Pop & Physics Arc, Billboard), Bảng màu đa dạng: Trắng (thường), Vàng (Gồng/Counter), Xanh (E), Cam (Q), Đỏ Crimson (R), Đỏ máu (Player dính đòn) |
| **Monster AI** | Token Pool đạo diễn lượt đánh nhóm | ✅ Hoàn thành | `AttackTokenDirector.cs` |
| **Monster AI** | Quái cận chiến (Skeleton Warrior & Minion) | ✅ Hoàn thành | Prefab + AI truy vết leo cầu thang |
| **Monster AI** | Quái bắn cung tầm xa (Skeleton Rogue) | ✅ Hoàn thành | Prefab + Bắn tên `Skeleton_Arrow` |
| **Monster AI** | Quái pháp sư (Skeleton Mage) | ✅ Hoàn thành | Prefab + Đạn ma pháp, Vùng đất lửa, Cột sáng |
| **Monster PCG** | Bộ sinh quái tự động cho hầm ngục (`ProceduralEnemySpawner.cs`) | ✅ Hoàn thành | Sinh quái theo Biệt đội chuẩn RPG: Phòng nhỏ/vừa 1 nhóm T1 + 1 nhóm T2 (nếu có); Phòng lớn 1-2 nhóm T1 + 1-2 nhóm T2; Kho báu 1 nhóm nhỏ; Sinh quái cả phòng đầu vào (`data.startRoom`); Chỉ chừa trống Boss Room; Điểm neo R=3m cách nhau >=8.5m; Chống kẹt props & cửa |
| **Monster / Optimization** | Tối ưu hóa hiệu năng quái vật hầm ngục (`DungeonEnemyOptimizer.cs`) | ✅ Hoàn thành | AI, Animation, Mesh & Shadow Hibernation: Cự ly <40m thức tỉnh đầy đủ, >48m ngủ đông (0% CPU/GPU, tắt Animator.enabled, tắt Renderers, tắt Shadow, tắt NavMesh, tắt AI Update); Tối ưu `EnemyFeedback` bằng `MaterialPropertyBlock` không clone material; Time-slicing 4 batch; Thức tỉnh tức thì khi dính đòn |
| **Boss (Ice Golem)** | Trọn Bộ 7 Kỹ Năng, 1500 HP, 500 Poise, WeakenIG, Phase 2 Cuồng Nộ 50% HP, Hào Quang Thân, Standby Bục Đá, JumpDown Không Giật Trần & Di Chuyển 4 Hướng | ✅ Hoàn thành | `Boss_IceGolem.prefab`, `IceGolemController.cs` (4.692 dòng thống nhất): 1500 HP, 500 Poise, WeakenIG, Phase 2 Cuồng nộ; Chuỗi JumpDown lao dốc chuẩn xác (nhún đà giữ nguyên độ cao bục, rơi gia tốc trọng lực $u^2$, tiếp đất frame 55 kích nổ sóng chấn động, triệt tiêu lỗi bay lên trần nhà); 5 nhịp GumoGumo dộng sàn chuẩn xác; 12 transitions 0.15s di chuyển 4 hướng không khựng; Bảo toàn 100% VFX Prefabs và Inspector bindings |
| **Boss ML-Agents (Ice Golem)** | Trí Tuệ Nhân Tạo PPO Souls-like, Bắt Roll-Catch, Phạt Uống Máu, Đấu Trường Hiệp Đấu Liên Hoàn & Bot Tự Động Cho Player (ML-01 & ML-02) | ✅ Hoàn thành | Packages/com.unity.ml-agents: 4.1.0 chuẩn Unity 6, IceGolemAgent.cs tích hợp Action Masking (WriteDiscreteActionMask) & Effective Range Filtering, Tối ưu nhịp độ tấn công cho PlayerBotAI, Run 3 đạt 12.955 steps (Reward +21.01) đóng gói IceGolemSoulsAI-12955.onnx, IceGolem_PPO.yaml |
| **Boss Sparring Partner** | Bot AI Tự Động Đối Luyện Cho Player (PlayerBotAI.cs) Đạt Chuẩn Souls-like | ✅ Hoàn thành | PlayerBotAI.cs: FSM 9 trạng thái; Đồng bộ isRolling hai chiều cho ML-Agents (sửa triệt để lỗi mất Vector Obs 19 & Obs 20 Roll-Catching); Sửa lỗi nuốt đòn 1 combo; Kích hoạt Counter Attack sau Parry (120 Poise); Skill E/Q trừ Spirit chuẩn; Cửa sổ sơ hở uống máu 1.2s cho Boss Heal-Punish; Trừng phạt Boss Weaken; Hỗ trợ cả Ice Golem & Red Ignis; Phím [B] bật/tắt & [[ / ]] chỉnh TimeScale |
| **Boss (Red Ignis)** | Vật Lý Áo Choàng, Khung Xương Humanoid, Vũ Khí, Controller Chiến Đấu, Animator 4 Hướng & Bộ VFX Riêng | ✅ Hoàn thành | Boss_RedIgnis.prefab, RedIgnisController.cs, Ignis_animator.controller: Hoàn thiện Monolithic Controller chuẩn mực tương tự IceGolemController; Máu 1200, Poise 400 kèm Weaken choáng vỡ thế 2.5s; Di chuyển 4 hướng mượt mà bằng phím mũi tên & API Move(h, v) cho AI; Trọn bộ 4 chiêu thức (Phím 1-4): AttackSlashRI (chém dọc), HorizontalSlashRI (chém ngang), JumpAttackRI (nhảy dậm nổ AoE), ComboAttackRI (chém 4 nhát liên hoàn); Hệ thống VFX đạn kiếm bay 90 độ (RedIgnisSlashProjectile.cs); Tool cân chỉnh kiếm IgnisWeaponAligner.cs |
| **Boss (Red Ignis Polish)** | Hệ Thống Vệt Kiếm IgnisSwordTrail, Chuẩn Hóa Combo 4 Nhát, Gồng Kiếm Lửa Phase 2 & Tối Ưu Tooling Phi Phá Hủy | ✅ Hoàn thành | IgnisSwordTrail.cs: Dynamic Ribbon Mesh mượt mà bằng Catmull-Rom Spline trực tiếp trên lưỡi kiếm, khử rung giật verticalJitterDamping = 0.35f, làm mượt chuyển động motionSmoothing = 0.45f, dải màu đỏ thẫm/đen khói tan biến nhanh 0.16s, tự kích hoạt khi xuất chiêu 1-4 và dập tắt khi trúng đòn suy yếu/tử trận; Sửa triệt để Root Motion Yaw lệch trái (+57.3° -> 0.0°) trong ComboAttackRI.anim, snap xoay mặt bám sát Player, bổ sung tham số comboLungeDistance (mặc định 0m chém tại chỗ không trôi giật vị trí), 4 nhịp hit timers {0.40s, 0.95s, 1.55s, 2.20s}; Gồng kiếm SwordCastingRI tại 50% HP kích hoạt IgnisSwordFlame.cs bốc cháy sống kiếm tăng vĩnh viễn +10 sát thương; Nâng cấp SetupRedIgnisAnimatorAndPrefabs.cs gỡ bỏ [InitializeOnLoadMethod], áp dụng SetDefaultFloatIfUnset bảo toàn 100% giá trị Inspector người dùng tùy chỉnh, triệt tiêu lỗi biên dịch CS0136. |
| **RPG / Progression** | Cấp độ, Kinh nghiệm & Cộng Điểm Tiềm Năng (RPG-05) | ✅ Hoàn thành | PlayerProgression.cs (Singleton/DontDestroyOnLoad, Level 1-50, EXP mượt, thưởng 3 điểm/cấp, tẩy điểm bằng Vàng); Vòng EXP chữ C trên PlayerHUD (ExpRingFill); Modal phân bổ điểm tiềm năng (AttributeAllocationUI.cs) |
| **System / Interaction** | Hệ thống Tương Tác Đa Năng (SYS-01) | ✅ Hoàn thành | IInteractable.cs, InteractableRegistry.cs, PlayerInteraction.cs (quét đối tượng trước mặt, xử lý Tap/Hold phím F); InteractPromptUI.cs điều khiển InteractUI với thanh HoldFill dọc từ dưới lên; Nâng cấp PortalController.cs (Hold 2.0s chuyển cảnh); Nâng cấp ChestInteractable.cs (Hold 0.8s mở rương); GenericInteractable.cs sẵn sàng cho NPC Shop/Thợ rèn |
| **World** | Chuyển cảnh Làng <-> Dungeon (Cổng dịch chuyển) | ✅ Hoàn thành | PortalController.cs (Hold 2.0s F, kết nối tự động với InteractUI) |
| **World** | Môi trường Làng (`LandScene`) & NPC tuần tra | ✅ Hoàn thành | `LandScene.unity`, `NpcPatrol.cs` |
| **Core Loop / UI** | Vòng lặp Ngày & Tầng (`GameManager.cs`, `InfoUIController.cs`) (GM-01) | ✅ Hoàn thành | Singleton DontDestroyOnLoad lưu trữ `CurrentDay` & `CurrentFloor`; Tự động cập nhật HUD `TopCornerInfo` (Day/Floor/Place); Biểu ngữ điện ảnh `MidScreenInfo` giới thiệu vùng đất (Fade In/Hold/Fade Out) Souls-like; Menu 1-Click tự động hóa cấu hình |
| **Core Loop / Death** | Cơ chế Tử trận & Hồi sinh tại Chuông Làng (`DeadScreenUIController.cs`, `GameManager.cs`) (GM-02) | ✅ Hoàn thành | Màn hình YOU DIED Souls-like; Chờ animation ngã gục 1.2s -> Mở khóa chuột -> Fade In; Nút RESPAWN: CurrentDay += 1, nạp LandScene, teleport chính xác cạnh Chuông Làng (`bell`), hồi 100% HP & Thể lực Stamina, khôi phục quyền điều khiển |
| **Core Loop / Victory** | Chiến Thắng Tầng & Lựa Chọn Đi Tiếp / Về Làng (`VictoryPortalUI.cs`, `GameManager.cs`) (GM-03) | ✅ Hoàn thành | Tích hợp biểu ngữ VICTORY ACHIEVED, rương Boss & cổng dịch chuyển bục đá 12m; UI 2 cửa sổ Hold-to-Confirm 2.0s; Về Làng: bảo toàn kho đồ, CurrentDay += 1, Floor = 1; Xuống Tầng Kế: targetFloor += 1 động, sinh Dungeon mới qua LoadingScreenManager |
| **RPG / Architecture** | Kiến trúc dữ liệu Vật phẩm & Kho đồ (RPG-01) | ✅ Hoàn thành | `ItemData.cs` (ScriptableObject), `InventorySlot.cs`, `InventoryManager.cs` (Singleton/DontDestroyOnLoad, 30 ô, Vàng, Equip/Unequip/Use/Drop, buff trực tiếp ATK vào `PlayerCombat.cs` và DEF vào `Health.cs`) |
| **RPG / Drops** | Rơi đồ vật lý từ Quái, Thùng vỡ & Rương (RPG-02) | ✅ Hoàn thành | `LootDropper.cs` quỹ đạo búng vòng cung Arc Pop; `BreakableProp.cs` nhận sát thương vỡ thùng; `ChestInteractable.cs` rương lật nắp tung đồ; `WorldItemPickup.cs` mô hình 3D xoay lơ lửng hào quang Rarity; Hệ thống 4 mô hình Vàng phân cấp theo số lượng |
| **RPG / Interaction** | UI Nhặt đồ & Feed thông báo mép trái (RPG-03) | ✅ Hoàn thành | `PickupPromptUI.cs` bảng nhặt đồ nổi (phím F, 2m, tên đổi màu Rarity, CanvasGroup fade); Hút Vàng tự động (Magnetic 4.2m bay vào ngực); `PickupNotificationFeed.cs` thông báo xếp tầng trượt mượt |
| **RPG / UI** | Giao diện Túi Đồ 2 Cửa Sổ (RPG-04) | ✅ Hoàn thành | `InventoryUIController.cs` (phím Tab/Esc, khóa chuột/di chuyển/camera); 3D Character Preview xoay 360 độ qua RenderTexture; 2 ô trang bị Weapon & Armor; Bảng chỉ số thời gian thực; Lưới 30 ô phân loại 4 Tab; Popup chi tiết bám theo ô thông minh |
| **RPG / Items** | Cơ Sở Dữ Liệu 51 Vật Phẩm & Đồng Bộ Visual Kiếm (RPG-06 & RPG-09) | ✅ Hoàn thành | `ItemDatabaseGenerator.cs` khởi tạo 51 ScriptableObjects (20 vũ khí, 8 giáp, 15 nguyên liệu, đá quý & lõi boss, 6 dược phẩm, vàng, chìa khóa); `PlayerWeaponVisualSync.cs` tự động đổi 3D model kiếm trên tay Player khi đổi trang bị trong Inventory |
| **RPG / Shop** | Hệ Thống Cửa Hàng Làng Haven (RPG-07) | ✅ Hoàn thành | `ShopData.cs`, `ShopUIController.cs`, `ShopInteractable.cs`: 2 NPC Shop Trang bị & Nhu yếu phẩm, bán đồ nhận 40% Vàng, khóa bán đồ đang mặc & chìa khóa |
| **RPG / Blacksmith** | Lò Rèn Haven & Hệ Thống Cường Hóa / Chế Tạo (RPG-08) | ✅ Hoàn thành | `BlacksmithUIController.cs`, `CraftingRecipe.cs`: Cường hóa +1 đến +5 (+15% ATK/cấp, badge vàng kim, hào quang vũ khí +5), chế tạo thần binh từ Lõi Boss |
| **RPG / Drops Balance** | Cân Bằng & Cập Nhật Ma Trận Rơi Đồ Toàn Diện (RPG-09) | ✅ Hoàn thành | `LootDropper.cs`, `LootTableSetupEditor.cs`: Đồng bộ 100% Ma trận rơi đồ cho 10 Prefabs (3 Thùng gỗ, 4 Quái Skeleton, 3 Cấp độ Rương); Bổ sung Monster Gem (20-25% từ quái) & Boss Gem (100% từ Boss) chuyên bán lấy Vàng |
| **PCG / Dungeon** | Phòng Tế Đàn & Chìa Khóa Hầm Ngục (D02-2) | ✅ Hoàn thành | Nhận diện `keyRoom` xa nhất nhánh cụt; sinh `wallset_shrine`; Chìa Khóa Hầm Ngục `DungeonKeyPickup` lơ lửng, phát quang Legendary vàng, nhặt F lưu `dungeon_key` vào `InventoryManager` |
| **PCG / Boss Arena** | Cơ Chế Boss Arena Toàn Diện (D-02 / D02-1 -> D02-5) | ✅ Hoàn thành | Phòng Boss 900 tiles trần 24m, Đại Lộ 40m, 4 Cột trụ né đòn, 3 Tầng ánh sáng; Đại Môn phong ấn (Hold F 2.5s) & Cổng sương mù Fog Gate; Đạo diễn Intro Cutscene 5 pha; Bục Đá 12m; Rương Boss bung tỏa quạt nón & Cổng chiến thắng 2 cửa sổ Hold-to-Confirm |

---

## 2. Các Tính Năng Đang Thiếu / Cần Thực Hiện Tiếp Theo (Theo Thứ Tự Ưu Tiên)

1. **[ĐÃ HOÀN THÀNH - Task RPG-07] Cửa Hàng Làng Haven (Tách 2 Shop: Trang Bị & Nhu Yếu Phẩm)**
2. **[ĐÃ HOÀN THÀNH - Task RPG-08] Lò Rèn Haven & Hệ Thống Cường Hóa / Chế Tạo (Blacksmith NPC)**
3. **[ĐÃ HOÀN THÀNH - Task RPG-09] Cân Bằng & Cập Nhật Ma Trận Rơi Đồ Toàn Diện (Detailed Drop Tables & Gems)**
4. **[Ưu tiên 1 - Task ML-03 & ML-04] Trí Tuệ Nhân Tạo Red Ignis & Đóng Gói Huấn Luyện:**
   - Thiết kế `RedIgnisAgent.cs` (Boss Duelist: kiếm thuật tốc độ, 2 Pha tái sinh đổi diện mạo mới).
   - Huấn luyện PPO đa sàn đấu (Multi-Arena) và xuất file `.onnx` nhúng vào game thời gian thực.
5. **[Ưu tiên 2 - Task POL-01 & POL-02] Hệ Thống Menu & Âm Thanh Hoàn Chỉnh:**
   - Menu chính (MainMenu), Cài đặt (Settings), Menu tạm dừng (PauseMenu).
   - Hệ thống SFX va chạm kiếm, đe búa thợ rèn, quái gầm và BGM Làng / Hầm ngục / Đấu Boss.
   - Huấn luyện PPO đa sàn đấu (Multi-Arena) và xuất file `.onnx` nhúng vào game thời gian thực.

5. **[Ưu tiên 5 - Task POL-01 & POL-02] Hệ Thống Menu & Âm Thanh Hoàn Chỉnh:**
   - Menu chính (MainMenu), Cài đặt (Settings), Menu tạm dừng (PauseMenu).
   - Hệ thống SFX va chạm kiếm, đe búa thợ rèn, quái gầm và BGM Làng / Hầm ngục / Đấu Boss.
