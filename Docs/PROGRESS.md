# Nhật Ký Tiến Độ Dự Án (Progress Log) - Infinite Labyrinth

> **Cập nhật ngày:** 26/10/2026  
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
| **Combat / UI** | PlayerHUD hiển thị đầy đủ 3 thanh tài nguyên Souls-like | ✅ Hoàn thành | `PlayerHUD.cs`: Máu (kèm Ghost Bar tụt trễ), Thể lực Stamina, Cụm 3 Sao Spirit nạp chiêu thức E/Q |
| **Combat** | Hệ thống Poise (Khựng đòn Souls-like) | ✅ Hoàn thành | Cài đặt trong `PlayerFeedback` & `EnemyFeedback` |
| **Combat** | Quản lý Thể lực (Stamina) & Tiêu hao | ✅ Hoàn thành | `PlayerStats.cs` (Roll, Sprint, Attack, Parry) |
| **Combat** | Năng lượng Chiêu thức (Spirit / Focus) | ✅ Hoàn thành | `PlayerStats.cs` (Tích lũy khi chém/parry, dùng E/Q) |
| **Combat** | Hệ thống Đỡ đòn & Phản công (Parry & Counter) | ✅ Hoàn thành | `PlayerCombat.cs` (Cửa sổ parry 0.28s) |
| **Combat** | Nhảy, Rơi tự do & Phân biệt Soft/Hard Landing | ✅ Hoàn thành | `PlayerController.cs` + Animator State Machine |
| **Combat** | Khóa mục tiêu (Target Lock Souls-like) | ✅ Hoàn thành | `TargetLockSystem.cs` vờn quái |
| **Monster AI** | Token Pool đạo diễn lượt đánh nhóm | ✅ Hoàn thành | `AttackTokenDirector.cs` |
| **Monster AI** | Quái cận chiến (Skeleton Warrior & Minion) | ✅ Hoàn thành | Prefab + AI truy vết leo cầu thang |
| **Monster AI** | Quái bắn cung tầm xa (Skeleton Rogue) | ✅ Hoàn thành | Prefab + Bắn tên `Skeleton_Arrow` |
| **Monster AI** | Quái pháp sư (Skeleton Mage) | ✅ Hoàn thành | Prefab + Đạn ma pháp, Vùng đất lửa, Cột sáng |
| **Monster PCG** | Bộ sinh quái tự động cho hầm ngục (`ProceduralEnemySpawner.cs`) | ✅ Hoàn thành | Sinh quái theo Biệt đội chuẩn RPG: Phòng nhỏ/vừa 1 nhóm T1 + 1 nhóm T2 (nếu có); Phòng lớn 1-2 nhóm T1 + 1-2 nhóm T2; Kho báu 1 nhóm nhỏ; Sinh quái cả phòng đầu vào (`data.startRoom`); Chỉ chừa trống Boss Room; Điểm neo R=3m cách nhau >=8.5m; Chống kẹt props & cửa |
| **Monster / Optimization** | Tối ưu hóa hiệu năng quái vật hầm ngục (`DungeonEnemyOptimizer.cs`) | ✅ Hoàn thành | AI, Animation, Mesh & Shadow Hibernation: Cự ly <40m thức tỉnh đầy đủ, >48m ngủ đông (0% CPU/GPU, tắt Animator.enabled, tắt Renderers, tắt Shadow, tắt NavMesh, tắt AI Update); Tối ưu `EnemyFeedback` bằng `MaterialPropertyBlock` không clone material; Time-slicing 4 batch; Thức tỉnh tức thì khi dính đòn |
| **Boss (Ice Golem)** | Trọn Bộ 7 Kỹ Năng, VFX Tùy Biến, Di Chuyển 4 Hướng & Khóa Mục Tiêu Thông Minh | ✅ Hoàn thành | Boss_IceGolem.prefab, IceGolemController.cs: 7 kỹ năng hoàn thiện: Attack1 (đấm thẳng + Impact), GumoGumo Slam (đấm sàn liên hoàn), FlameBreath (phun lửa nón), JumpAttack (nhảy vồ áp sát + shockwave), RunAttack (chạy húc tới phím 5 + slot VFX hào quang thân), AttackLT_IG (combo 3 đòn cận chiến phím 6 + slot VFX hào quang 2 tay), IceBallIG (gồng tụ cầu tím Purple Orb giữa 2 tay trên trời phím 7 + ném phát nổ); JumpDown mở màn duy nhất 1 lần khi phát hiện Player (khóa vĩnh viễn sau lần đầu, chống spam lại); Di chuyển 4 hướng thông minh (Tiến, Lùi, Dạt Trái, Dạt Phải) theo chuẩn 100% Humanoid Rig; Chống duplicate component & Tool tự động dọn Scene |
| **Boss ML-Agents (Ice Golem)** | Trí Tuệ Nhân Tạo PPO Souls-like, Bắt Roll-Catch, Phạt Uống Máu, Đấu Trường Hiệp Đấu Liên Hoàn & Bot Tự Động Cho Player (ML-01 & ML-02) | ✅ Hoàn thành | Packages/com.unity.ml-agents: 4.1.0 chuẩn Unity 6, IceGolemAgent.cs tích hợp Action Masking (WriteDiscreteActionMask) & Effective Range Filtering, Tối ưu nhịp độ tấn công cho PlayerBotAI, Run 3 đạt 12.955 steps (Reward +21.01) đóng gói IceGolemSoulsAI-12955.onnx, IceGolem_PPO.yaml |
| **Boss (Red Ignis)** | Vật Lý Áo Choàng, Khung Xương Humanoid & Vũ Khí | ✅ Hoàn thành | Boss_RedIgnis.prefab, RedIgnis.fbx: Khắc phục vật lý tà áo UnityChanSpringBone (dọn sạch null array 19 -> 12 xương, gỡ 4 xương ngọn lỗi, tool SpringManagerHelper.cs); Chuẩn hóa Rig Humanoid (Hips -> mixamorig:Hips); Tinh chỉnh Animation đi bộ Ignis_Walk (Based Upon = Feet / Center of Mass, nâng Offset chống lún/co ngắn chiều cao); Tích hợp Vũ khí Kiếm Siu Nhân Đỏ (RightHand_Socket, Animation Type: None) |
| **RPG / Progression** | Cấp độ, Kinh nghiệm & Cộng Điểm Tiềm Năng (RPG-05) | ✅ Hoàn thành | PlayerProgression.cs (Singleton/DontDestroyOnLoad, Level 1-50, EXP mượt, thưởng 3 điểm/cấp, tẩy điểm bằng Vàng); Vòng EXP chữ C trên PlayerHUD (ExpRingFill); Modal phân bổ điểm tiềm năng (AttributeAllocationUI.cs) mở từ nút BtnAttributePoints trên Inventory |
| **System / Interaction** | Hệ thống Tương Tác Đa Năng (SYS-01) | ✅ Hoàn thành | IInteractable.cs, InteractableRegistry.cs, PlayerInteraction.cs (quét đối tượng trước mặt, xử lý Tap/Hold phím F); InteractPromptUI.cs điều khiển InteractUI với thanh HoldFill dọc từ dưới lên; Nâng cấp PortalController.cs (Hold 2.0s chuyển cảnh); Nâng cấp ChestInteractable.cs (Hold 0.8s mở rương); GenericInteractable.cs sẵn sàng cho NPC/Thợ rèn |
| **World** | Chuyển cảnh Làng <-> Dungeon (Cổng dịch chuyển) | ✅ Hoàn thành | PortalController.cs (Hold 2.0s F, kết nối tự động với InteractUI) |
| **World** | Môi trường Làng (`LandScene`) & NPC tuần tra | ✅ Hoàn thành | `LandScene.unity`, `NpcPatrol.cs` |
| **RPG / Architecture** | Kiến trúc dữ liệu Vật phẩm & Kho đồ (RPG-01) | ✅ Hoàn thành | `ItemData.cs` (ScriptableObject), `InventorySlot.cs`, `InventoryManager.cs` (Singleton/DontDestroyOnLoad, 30 ô, Vàng, Equip/Unequip/Use/Drop, buff trực tiếp ATK vào `PlayerCombat.cs` và DEF vào `Health.cs`), bộ 8 vật phẩm mẫu qua `ItemDatabaseGenerator.cs` |
| **RPG / Drops** | Rơi đồ vật lý từ Quái, Thùng vỡ & Rương (RPG-02) | ✅ Hoàn thành | `LootDropper.cs` quỹ đạo búng vòng cung Arc Pop; `BreakableProp.cs` nhận sát thương vỡ thùng; `ChestInteractable.cs` rương lật nắp tung đồ; `WorldItemPickup.cs` mô hình 3D xoay lơ lửng hào quang Rarity; Hệ thống 4 mô hình Vàng phân cấp theo số lượng (`coin`, `coin_stack_small/medium/large`) |
| **RPG / Interaction** | UI Nhặt đồ & Feed thông báo mép trái (RPG-03) | ✅ Hoàn thành | `PickupPromptUI.cs` bảng nhặt đồ nổi (phím F, 2m, tên đổi màu Rarity, CanvasGroup fade); Hút Vàng tự động (Magnetic 4.2m bay vào ngực); `PickupNotificationFeed.cs` thông báo xếp tầng trượt mượt (Slide In/Up/Out) mép trái |
| **RPG / UI** | Giao diện Túi Đồ 2 Cửa Sổ (RPG-04) | ✅ Hoàn thành | `InventoryUIController.cs` (phím Tab/Esc, khóa chuột/di chuyển/camera); 3D Character Preview xoay 360 độ qua RenderTexture (`CharacterPreviewController.cs`); 2 ô trang bị Weapon & Armor (`EquipmentSlotUI.cs`); Bảng chỉ số thời gian thực (`PlayerStatsPanelUI.cs`); Lưới 30 ô phân loại 4 Tab (`InventorySlotUI.cs`); Popup chi tiết bám theo ô thông minh (`ItemDetailPopupUI.cs`) tự lật góc chống tràn màn hình |

---

## 2. Các Tính Năng Đang Thiếu / Cần Thực Hiện Tiếp Theo (Theo Thứ Tự Ưu Tiên)

1. **[ƯU TIÊN SỐ 1 HIỆN TẠI - Task RPG-06] Thợ Rèn (Blacksmith) & Cường Hóa Trang Bị tại Làng:**
   - Tương tác với NPC Thợ Rèn tại `LandScene` (bấm phím F mở UI Rèn).
   - Công thức cường hóa: Vũ khí hiện tại + [X] Vàng + [Y] Đá cường hóa kiếm -> Nâng cấp bậc kiếm (+1, +2...), tăng mạnh sát thương chém và đổi màu hiệu ứng vung kiếm.

2. **[Ưu tiên 2 - Task RPG-06] Thợ Rèn (Blacksmith) & Cường Hóa Trang Bị:**
   - Tương tác với NPC Thợ Rèn tại `LandScene` (bấm phím F mở UI Rèn đồ).
   - Tiêu tốn Vàng và Đá cường hóa để nâng cấp bậc kiếm (+1, +2...), tăng mạnh sát thương chém và hiệu ứng vung kiếm.

3. **[Ưu tiên 3 - Task D-02] Cơ chế Boss Arena tại Boss Room:**
   - Đặt `Boss_IceGolem.prefab` tại trung tâm phòng Boss (`data.bossRoom`).
   - Cơ chế Cửa chặn (Fog Gate / Iron Gate): Tự động đóng cửa khi Player bước vào, hạ gục Boss thì mở cửa và kích hoạt Portal chiến thắng.

4. **[Ưu tiên 4 - Task GM-01 -> GM-03] Hệ thống Day & Floor Loop (`DayManager` / `GameManager`):**
   - Quản lý biến tĩnh toàn cục `CurrentDay` (Day 1, Day 2...), `CurrentFloor` (Floor 1, Floor 2...).
   - Cơ chế Chết trong Dungeon -> Tăng Day + 1, Respawn tại Chuông Làng trong `LandScene`.
   - Cơ chế Thắng Boss -> Lựa chọn Về Làng (cất đồ, qua Day mới) hoặc Xuống Tầng Kế Tiếp (Floor + 1).

5. **[Ưu tiên 5 - Task ML-01 -> ML-04] Trùm Học Máy Souls-like (Boss ML-Agents: Ice Golem & Red Ignis):**
   - Tích hợp package com.unity.ml-agents (Unity) & kết nối môi trường Python ml-agents: 1.1.0 (PyTorch).
   - Thiết kế IceGolemAgent.cs (Boss Juggernaut: đòn nặng, chấn động diện rộng, nhảy bổ áp sát, phun lửa, dạt né).
   - Thiết kế RedIgnisAgent.cs (Boss Duelist: kiếm thuật tốc độ, combo lướt kiếm, chém phản xạ, dạt né vòng quanh).
   - Xây dựng hành vi Souls-like qua RL: Báo hiệu đòn (Telegraphing), Trừng phạt lộn hoảng loạn (Roll-Catching), Trừng phạt uống máu (Heal-Punish), Thăm dò cự ly & Bọc lót (Circling/Strafing), Phân định cơ chế: Ice Golem (Đơn pha - Single Phase) và Red Ignis (2 Pha - Tái sinh khi chết kèm Lột xác Diện mạo mới - Phase 2 Rebirth & Appearance Transformation).
   - Huấn luyện PPO tự động trên đa sàn đấu (Multi-Arena) và đóng gói model .onnx nhúng vào game thời gian thực.
