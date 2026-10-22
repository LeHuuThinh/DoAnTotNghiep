# Nhật Ký Tiến Độ Dự Án (Progress Log) - Infinite Labyrinth

> **Cập nhật ngày:** 22/10/2026  
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
| **Boss** | Mô hình 3D & Bộ Animation Boss Ice Golem | ✅ Hoàn thành | `Boss_IceGolem.prefab`, Attack, Jump, Roar, Walk |
| **World** | Chuyển cảnh Làng -> Dungeon (Cổng dịch chuyển) | ✅ Hoàn thành | `PortalController.cs` (Đè phím F) |
| **World** | Môi trường Làng (`LandScene`) & NPC tuần tra | ✅ Hoàn thành | `LandScene.unity`, `NpcPatrol.cs` |

---

## 2. Các Tính Năng Đang Thiếu / Cần Thực Hiện Tiếp Theo (Theo Thứ Tự Ưu Tiên)

1. **[Ưu tiên 1 - Task D-02] Cơ chế Boss Arena tại Boss Room:**
   - Đặt `Boss_IceGolem.prefab` tại trung tâm phòng Boss (`data.bossRoom`).
   - Cơ chế Cửa chặn (Fog Gate / Iron Gate): Tự động đóng cửa khi Player bước vào, hạ gục Boss thì mở cửa và kích hoạt Portal chiến thắng.

2. **[Ưu tiên 2 - Task GM-01 -> GM-03] Hệ thống Day & Floor Loop (`DayManager` / `GameManager`):**
   - Quản lý biến tĩnh toàn cục `CurrentDay` (Day 1, Day 2...), `CurrentFloor` (Floor 1, Floor 2...).
   - Cơ chế Chết trong Dungeon -> Tăng Day + 1, Respawn tại Chuông Làng trong `LandScene`.
   - Cơ chế Thắng Boss -> Lựa chọn Về Làng (cất đồ, qua Day mới) hoặc Xuống Tầng Kế Tiếp (Floor + 1).

4. **[Ưu tiên 4 - Task RPG-01 -> RPG-03] Kinh Tế, Loot & Inventory:**
   - Đập thùng gỗ và tiêu diệt quái rơi Vàng / Nguyên liệu / Bình máu.
   - Giao diện Túi đồ (Inventory UI - phím Tab/I).
   - NPC Thợ rèn tại Làng (Blacksmith UI) tiêu Vàng và Đá cường hóa để nâng cấp sát thương kiếm.

5. **[Ưu tiên 5 - Task ML-01 -> ML-03] Trùm Học Máy (Boss ML-Agents):**
   - Tích hợp package `com.unity.ml-agents`.
   - Huấn luyện mô hình Reinforcement Learning cho Boss Ice Golem (PPO).
