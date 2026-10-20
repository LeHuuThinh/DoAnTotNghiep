# Nhật Ký Tiến Độ Dự Án (Progress Log) - Infinite Labyrinth

> **Cập nhật ngày:** 20/10/2026  
> **Trạng thái đối chiếu:** Đã rà soát trực tiếp 100% mã nguồn C#, Prefabs và Scenes trên ổ đĩa.

---

## 1. Bảng Tổng Hợp Tính Năng Hiện Có (Verified Features)

| Phân hệ | Tính năng | Trạng thái | Ghi chú kỹ thuật |
| :--- | :--- | :---: | :--- |
| **PCG Dungeon** | Thuật toán BSP chia phòng & hành lang | ✅ Hoàn thành | `BSPGenerator.cs`, `BSPNode.cs` |
| **PCG Dungeon** | BFS / Flood-Fill tìm phòng Boss xa nhất | ✅ Hoàn thành | `DungeonRoomAnalyzer.cs` (set trần cao 16m) |
| **PCG Dungeon** | Phòng Gác lửng (Mezzanine tầng 2) & Cầu thang 3D | ✅ Hoàn thành | Dựng Mesh 3D dốc nối sàn 1 và sàn 2 |
| **PCG Dungeon** | Rải đạo cụ ngẫu nhiên (Poisson Disk Sampling) | ✅ Hoàn thành | `DungeonPropDecorator.cs` |
| **PCG Dungeon** | Tối ưu hóa ánh sáng URP Point Lights | ✅ Hoàn thành | `DungeonLightOptimizer.cs` |
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
| **Boss** | Mô hình 3D & Bộ Animation Boss Ice Golem | ✅ Hoàn thành | `Boss_IceGolem.prefab`, Attack, Jump, Roar, Walk |
| **World** | Chuyển cảnh Làng -> Dungeon (Cổng dịch chuyển) | ✅ Hoàn thành | `PortalController.cs` (Đè phím F) |
| **World** | Môi trường Làng (`LandScene`) & NPC tuần tra | ✅ Hoàn thành | `LandScene.unity`, `NpcPatrol.cs` |

---

## 2. Các Tính Năng Đang Thiếu / Cần Thực Hiện Tiếp Theo

- [ ] **Procedural Monster Spawner:** Quái hiện tại mới chỉ spawn thử 1 con ở phòng gác lửng (`spawnTestEnemyOnMezzanine`), chưa có script tự rải quái vào các phòng theo tỷ lệ và diện tích.
- [ ] **UI HUD Binding:** `PlayerHUD.cs` hiện chỉ có thanh máu, chưa hiển thị thanh **Stamina** (xanh lá/vàng) và thanh **Spirit** (xanh dương/tím).
- [ ] **Hệ thống Day & Floor Loop (`DayManager` / `GameManager`):** Chưa có script điều phối ngày (`Day 1`, `Day 2`), đếm tầng (`Floor 1`, `Floor 2`), và cơ chế chết thì respawn về Chuông Làng.
- [ ] **Loot & Economy:** Chưa có hệ thống rơi Vàng/Nguyên liệu khi chém vỡ thùng gỗ hoặc tiêu diệt quái.
- [ ] **Inventory & Crafting / Blacksmith:** Chưa có UI túi đồ, kho trang bị và bàn rèn vũ khí.
- [ ] **ML-Agents Boss:** Chưa cài đặt package `com.unity.ml-agents`, chưa có script `IceGolemAgent.cs` để train model Reinforcement Learning.
