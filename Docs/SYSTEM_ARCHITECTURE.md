# Kiến Trúc Hệ Thống (System Architecture) - Infinite Labyrinth

> **Dự án:** Infinite Labyrinth (Game 3D Action RPG Roguelite - Đồ Án Tốt Nghiệp)  
> **Phong cách nghệ thuật:** Faceted Low-poly  
> **Engine:** Unity (Universal Render Pipeline - URP)  
> **Ngôn ngữ:** C#  

---

## 1. Vòng Lặp Trò Chơi Cốt Lõi (Core Game Loop & Day System)

mermaid
flowchart TD
    Spawn["Khởi Đầu: LandScene (Làng)<br/>Day N - Respawn tại Chuông Làng"] --> VillageActions["Chuẩn bị tại Làng:<br/>- Hồi máu tại Chuông<br/>- Rèn vũ khí (Blacksmith)<br/>- Mua đồ / Thuốc (Shop)"]
    VillageActions --> EnterPortal["Cổng Dịch Chuyển (Hold F)"]
    EnterPortal --> GenDungeon["DungeonScene: PCG Sinh Hầm Ngục<br/>(BSP + Poisson + BFS Boss Room)"]
    GenDungeon --> Explore["Khám phá: Đánh quái Skeleton,<br/>Leo tầng 2, Mở rương/thùng loot vàng & nguyên liệu"]
    
    Explore -->|Player Chết| DieState["Kết thúc Run: Qua Day N+1<br/>Respawn tại Chuông Trung Tâm Làng"]
    DieState --> Spawn
    
    Explore --> BossFight["Phòng Trùm: Đấu Boss Ice Golem / Red Ignis (ML-Agents AI)"]
    BossFight -->|Chiến Thắng| VictoryChoice{"Lựa chọn sau khi diệt Boss"}
    
    VictoryChoice -->|Về Làng| ReturnVillage["Về Làng cất đồ & Qua Day N+1"]
    ReturnVillage --> Spawn
    
    VictoryChoice -->|Đi tiếp| DescendNextFloor["Xuống Tầng Tiếp Theo (Floor 2)<br/>(Quái mạnh hơn, loot giá trị hơn)"]
    DescendNextFloor --> GenDungeon


---

## 2. Kiến Trúc Các Phân Hệ (System Modules)

### A. Phân Hệ Dungeon PCG (Procedural Content Generation)
Hầm ngục được sinh tự động hoàn toàn mỗi khi người chơi bước qua cổng:
1. **Phân chia không gian (BSPGenerator.cs, BSPNode.cs):** Thuật toán Binary Space Partitioning chia khối không gian, tạo các phòng và kết nối bằng mạng lưới hành lang. Tích hợp vùng đệm an toàn startRoomMinGapTiles ngăn phòng thường sinh quá sát phòng khởi đầu và vùng cấm địa Đại Lộ Boss.
2. **Phân tích hình học & Tìm phòng Boss (DungeonRoomAnalyzer.cs):**
   * Sử dụng thuật toán **BFS / Flood-Fill** từ phòng khởi đầu (startRoom) để đo độ dài đường đi ngắn nhất.
   * Phòng xa nhất và có không gian đủ lớn sẽ được chọn làm **Boss Room** (ceilingHeight = 16f, vòm trần đồ sộ chuẩn đấu trường).
   * Phân loại các phòng chức năng: Start Room, Boss Room, Mezzanine Room (Phòng gác lửng tầng 2), Treasure Room, Normal Room.
3. **Dựng hình học 3D (DungeonRenderer.cs):** 
   * Dựng sàn, tường, cửa mở, và hệ thống cầu thang 3D nối sàn tầng 1 lên gác lửng tầng 2.
   * Áp dụng Perlin Noise để tạo độ gồ ghề và biến thể hoa văn vật liệu cho sàn đá.
4. **Trang trí, Templates & Phân bổ đạo cụ (DungeonPropDecorator.cs & Sub-modules):**
   * Được thiết kế theo mô hình **Facade Orchestrator & Partial Modules** sạch sẽ:
     * DungeonPropDecorator.cs: Nhạc trưởng điều phối, lưu trữ serialized fields và API entry point DecorateDungeon(...).
     * DungeonPropDecorator.Templates.cs: 8 mẫu tường vỡ/chữ L/cổng vòm và 5 mẫu tiểu cảnh (trại, tiệc tàn, giả kim, kho đổ nát). Tự động đo Bounding Box, xoay 4 hướng, đảm bảo an toàn cửa/cầu thang.
     * DungeonPropDecorator.Lighting.cs: Đuốc sàn (Floor Torch) nâng offset Y 0.5m chống lún chân đế, đuốc tường và đuốc hành lang kiểm tra tường đặc chống lơ lửng.
     * DungeonPropDecorator.Mezzanine.cs: Trang trí gác lửng Tầng 2 khớp chuẩn cao độ 8m (cờ hiệu, khiên kiếm, bàn ghế ngắm cảnh lan can và rải rác sàn).
     * DungeonPropDecorator.Furniture.cs: Bố trí nội thất áp vách tường, bàn ghế tâm phòng, góc phòng và rải rác tự do.
     * DungeonPropDecorator.Spatial.cs: Thuật toán lưới, tính toán forbiddenTiles chống đè đồ, kiểm tra tường đặc và phân bố họ cờ hiệu.
     * DungeonStaticPropCombiner.cs: Module gộp toàn bộ đồ tĩnh trong mỗi phòng theo Material thành 1 Draw Call duy nhất, tắt Shadow Casting và lọc bỏ MeshCollider thừa cho cờ vải/thủy tinh.
5. **Tối ưu hóa ánh sáng & Hiệu ứng hạt (DungeonLightOptimizer.cs):**
   * **Culling kép (Dual Culling):** Bật/tắt đồng bộ cả Light (Point Light) và ParticleSystem (Lửa & Khói của đuốc) dựa trên cự ly tới Player. Khi ở xa, hạt bị dừng mô phỏng CPU và xóa quads GPU, loại bỏ hoàn toàn hiện tượng Overdraw xuyên tường.
   * **Hysteresis Buffer:** Độ đệm 6m chống nhấp nháy đèn (Flicker-free) ở ranh giới culling.
   * **Triệt tiêu Shadow Caster Pass:** Tự động phát hiện và vô hiệu hóa shadow pass của Directional Light cường độ 0 (tiết kiệm 4 cascades 2048x2048 vô ích).
   * **Duy trì FPS cực cao:** Đạt 110 - 120 FPS ngay cả trong hầm ngục quy mô lớn 300m x 322m với Far Clip Plane 200m.

---

### B. Phân Hệ Combat Souls-like (Player & Monsters)
* **Giao diện Người chơi Souls-like (PlayerHUD.cs):**
  * **Thanh Máu (Health Bar):** MainFill đỏ kèm **Ghost Bar** vàng nhạt tụt đuổi từ từ sau thời gian trễ ghostShrinkDelay = 0.5s khi dính đòn.
  * **Thanh Thể lực (Stamina Bar):** Màu cam/vàng, co giãn mượt mà khi Lăn né (Roll), Chạy nhanh (Sprint), Chém kiếm (Attack).
  * **Cụm 4 Ngôi Sao Năng Lượng (Spirit Stars):** Nạp đầy dần khi chém trúng quái hoặc parry chuẩn, tiêu hao khi kích hoạt chiêu E (Sóng kiếm - 60), Q (Nổ năng lượng - 90) hoặc R (Tuyệt kỹ Ult - 175).
* **Giao diện Kỹ năng & Cooldown (`SkillIconUI.cs`):**
  * Tự động quản lý 3 ô chiêu thức `SkillE`, `SkillQ`, `SkillR` trong cụm `SkillIconUI`.
  * **Ma trận phản hồi 2 tầng (Dual-State Feedback):**
    * `Corner` (Viền góc): Bật khi đủ Spirit, tắt khi thiếu Spirit để người chơi biết trước đã tích trữ đủ năng lượng hay chưa.
    * `Icon` Alpha: Đạt 1.0 (sáng rực) khi hội tụ đủ 2 điều kiện (Đủ Spirit & Không CD); mờ còn 0.1 khi đang CD hoặc thiếu tài nguyên.
    * `CDFill`: Lớp phủ tối đếm ngược thời gian hồi chiêu ($1.0 \to 0.0$). Tùy chọn hiển thị Text số giây CD.
* **Giao Diện Thanh Máu Quái Vật World Space Souls-like (EnemyHealthBarUI.cs):**
  * **Billboard 3D Thông Minh:** Tự động xoay mặt vuông góc 100% về phía Camera người chơi (CameraAligned), phẳng phiu và không bị méo lệch góc nhìn ở mọi hướng quan sát. Hỗ trợ tùy chọn khóa trục Y (lockYAxis).
  * **Khai Thác Sprite Grayscale & Hiệu Ứng Souls-like:**
    * Sử dụng Monster_Border.png và Monster_Fill.png (chuẩn kích thước 3331x400, tỉ lệ 8.33:1).
    * MainFill: Tô màu đỏ tươi Souls-like từ sprite grayscale qua Image.color.
    * Ghost Bar: Thanh bóng ma màu vàng nhạt tụt đuổi từ từ sau thời gian trễ ghostShrinkDelay = 0.45s khi quái trúng đòn.
  * **Tối Ưu Hóa Hiệu Năng Tuyệt Đối (Dungeon 200 Quái):**
    * Tự động tắt Canvas khi quái ở cự ly xa hoặc ngủ đông (DungeonEnemyOptimizer - 0 Draw Calls).
    * Ẩn thanh máu khi quái đầy 100% HP và chưa vào trạng thái chiến đấu; tự động Fade In mượt mà khi dính đòn hoặc phát hiện người chơi.
    * Tự động Fade Out và tắt hẳn khi quái tử trận (onDeath).
    * Tắt hoàn toàn GraphicRaycaster để loại bỏ 100% chi phí EventSystem Raycasting.
* **Thanh chịu đòn Poise (PlayerFeedback.cs, EnemyFeedback.cs, PlayerCombat.cs, IceGolemController.cs):**
  * **Người chơi & Quái thường:**
    * Cả người chơi và quái vật đều sở hữu thanh Poise ẩn (maxPoise = 100 cho Warrior/Minion, 50 cho Rogue/Mage).
    * **Cấu hình Sát thương Poise độc lập (PlayerCombat.cs):** Mỗi đòn đánh của Player sở hữu lượng sát thương Poise riêng đưa ra Inspector (attack1PoiseDamage = 25, attack2PoiseDamage = 25, attack3PoiseDamage = 55, chargeHit1PoiseDamage = 35, chargeHit2PoiseDamage = 80, counterAttackPoiseDamage = 120).
    * **Super Armor khi còn Poise:** Khi currentPoise > 0f, quái vật giữ vững thế đứng chịu đòn, nháy trắng nhận sát thương nhưng KHÔNG bị giật hoạt ảnh GetHit và không bị ngắt chiêu (Warrior/Minion đứng vững 2 hit đầu, Rogue/Mage đứng vững 1 hit đầu).
    * **Vỡ thế (Stagger / Poise Break):** Khi Poise tụt về 0 (currentPoise <= 0f), quái vật mới bị vỡ thế: ngắt đòn đánh đang thi triển, kích hoạt animation GetHit, bị đẩy lùi (knockback), và hồi phục lại maxPoise.
    * Tự động hồi phục sau một khoảng trễ không bị đánh (poiseRecoveryDelay = 2.5s).
  * **Trùm Ice Golem (Boss Poise = 500):** Sở hữu thanh Poise dày 500 điểm, **không tự hồi phục** trong suốt trận chiến.
    * Sát thương Poise trừ theo độ uy lực và Knockback của từng đòn đánh từ Player:
      + Đòn chém thường 1 & 2 (knockback = 0): trừ 35 Poise.
      + Đòn chém 3 & Charge 1 (knockback > 0): trừ 50 Poise.
      + Đòn chém gồng 2 & Skill E (knockback >= 1.0): trừ 70 Poise.
      + Đòn Counter & Phép nổ Q (knockback >= 1.8): trừ 100 Poise.
    * Khi Poise của Boss về 0: Kích hoạt trạng thái **Suy yếu (WeakenIG)** duy nhất 1 lần (Boss thực hiện động tác bước lùi Weaken hết trọn vẹn clip hoạt ảnh và lập tức tiếp tục chiến đấu ngay tại vị trí mới). Nếu trong lúc đang chạy Weaken mà máu Boss tụt xuống <= 50% HP thì lập tức hủy suy yếu và kích hoạt chuỗi Phase 2 cuồng nộ.
* **Hệ thống Phản hồi Đòn đánh & Khựng Đòn (Hit Feedback & Hitlag - EnemyFeedback.cs, PlayerFeedback.cs, WeaponHitbox.cs, PlayerCombat.cs):**
  * **Hiệu ứng Hit VFX Tứa Máu & Mảnh Vụn (Directional Hit VFX):**
    * **Quái xương (Skeleton Monsters):** Khi bị Player chém trúng (combo 1, 2, 3, charge, counter, sóng kiếm E hay nổ Q), EnemyFeedback sinh ngay MobsGetHitVFX.prefab (màu xám vụn xương) tại điểm tiếp xúc hitPoint, góc tứa phun ngược chiều cú chém (-hitDirection) ra không khí.
    * **Người chơi (Player):** Khi bị quái chém, bắn tên hoặc phép thuật trúng đích, PlayerFeedback sinh ngay GetHitVFX.prefab (màu đỏ tứa máu) tại điểm dính đòn hitPoint. Khi Player **Parry thành công**, onCheckParryOrBlock chặn đứng sát thương trước khi trừ máu nên **không bị tứa máu đỏ**.
    * **Đòn kết liễu (Lethal Hit):** Luôn bảo đảm phát sinh Hit VFX ngay cả khi đòn đánh hạ gục mục tiêu.
    * **Công cụ Tự động hóa 1-Click:** CombatVfxSetupEditor.cs với MenuItem Infinite Labyrinth -> Combat -> 1. Cấu Hình Hit VFX Cho Player & Quái Vật (1-Click).
  * **Nháy trắng phát quang (Solid White Flash):** Sử dụng MaterialPropertyBlock ghi đè Texture2D.whiteTexture và kích hoạt _EMISSION (cường độ 2.5) trực tiếp trên GPU (0% clone material, giữ nguyên 100% SRP Batcher).
  * **Đòn kết liễu (Lethal Hit Sequence):** Khi dính đòn tử thủ, quái lập tức nháy trắng toàn thân và giữ màu trắng xuyên suốt thời gian hitlag kết liễu (lethalFlashDuration = 0.15s realtime), sau đó trả về màu texture ban đầu rồi mới kích hoạt animation ngã chết (Die).
  * **Hitlag Souls-like:** WeaponHitbox làm chậm thế giới xuống còn 5% (Time.timeScale = 0.05f) trong thời gian unscaled realtime. Toàn bộ giá trị hitlag từng đòn chém (attack1Hitlag: 0.035s, attack2Hitlag: 0.045s, attack3Hitlag: 0.08s, chargeHit1Hitlag: 0.05s, chargeHit2Hitlag: 0.095s, counterAttackHitlag: 0.11s) và đòn kết liễu (lethalHitlag: 0.18s) được đưa ra Inspector PlayerCombat.cs.
  * **Kiến Trúc Phân Tách Cấp Bậc Nhân Vật (Root Player vs Child MainModel Hierarchy):**
    * **Player (GameObject Gốc - Root):** Quản lý toàn bộ vật lý, điều khiển di chuyển, chỉ số tài nguyên và điều phối vệt kiếm slash: CharacterController, PlayerController, Health, PlayerStats, PlayerFeedback, TargetLockSystem, PlayerSlashVFX.
    * **MainModel (GameObject Con - Child):** Chứa 3D Mesh hình thể, bộ xương cử động, hệ thống tóc đuôi nảy lò xo (SpringManager), Animator và **PlayerCombat**.
    * **Nguyên lý Unity Animation Events:** Các sự kiện chém kiếm, mở/tắt hitbox và kích hoạt vệt slash được cài trực tiếp trên các mốc khung hình của animation clips. Unity chỉ truyền AnimationEvent đến các component nằm **trên cùng GameObject với Animator**. Do đó, PlayerCombat **bắt buộc phải nằm trên MainModel**, trong khi các script logic khác nằm ở Root Player và được liên kết tham chiếu hai chiều qua GetComponentInParent / Serialized fields.
  * **Hiệu ứng Vệt Chém Kiếm & Quỹ Đạo Đòn Đánh (Sword Slash Trail & Arc VFX - PlayerSlashVFX.cs, Slash VFX.prefab):**
    * **Kiến trúc Decoupled Arc Slash:** Không gắn dính vệt chém vào xương cổ tay hay thân kiếm (tránh hiện tượng xoay tròn méo mó theo cử động xương). VFX chém được điều phối bởi PlayerSlashVFX.cs, kích hoạt đúng frame vung kiếm mạnh nhất từ các Animation Events trong PlayerCombat.cs.
    * **Cấu hình Độc lập Theo 6 Đòn Đánh:** Mỗi nhát chém sở hữu tham số riêng: Position Offset (cao độ ngang ngực/eo), Rotation Offset (Euler Angles nghiêng chuẩn theo mặt phẳng chém), Scale kích thước, và Lifetime dọn dẹp bộ nhớ:
      + *Attack 1:* Chém vát chéo vai phải xuống hông trái (Rotation: -176.57°, -92.12°, -92.12°, Scale 1.0x).
      + *Attack 2:* Chém vát ngược từ hông trái lên vai phải (Rotation: -85.89°, -183.54°, 0.65°, Scale 1.05x).
      + *Attack 3:* Chém quét ngang phẳng uy lực kết thúc combo (Rotation: -176.57°, -92.12°, 0.09°, Scale 1.25x).
      + *Charge Hit 1:* Chém mở màn lấy đà gồng lực (Rotation: -176.57°, -92.12°, -45°, Scale 1.1x).
      + *Charge Hit 2:* Nhát bổ thẳng đứng từ trên cao cắm xuống đất (Rotation: -90°, -92.12°, -90°, Scale 1.35x).
      + *Counter Attack:* Cú chém phản đòn bùng nổ năng lượng diện rộng (Rotation: -176.57°, -92.12°, -15°, Scale 1.5x).
    * **Cơ Chế Tự Phục Hồi & Tự Động Hóa 1-Click:** PlayerCombat tự động tìm kiếm và gắn PlayerSlashVFX khi khởi chạy, tự động fallback nạp Resources/Slash VFX, Resources/E Slash VFX, Resources/Q Slash VFX nếu thiếu tham chiếu. Hỗ trợ cấu hình độc lập cho cả 6 đòn đánh cơ bản và 2 kỹ năng đặc biệt (E: Sóng kiếm khí, Q: Đập đất AOE). Hỗ trợ công cụ PlayerSlashVfxSetupEditor.cs với MenuItem Infinite Labyrinth -> Combat -> 2. Cấu Hình Slash, Impact & Parry VFX Cho Kiếm Player (1-Click) và Custom Editor với đầy đủ 8 nút Test trực quan trong Scene View.
  * **Hiệu ứng Tia Lửa Va Chạm Khi Chém Trúng Đích (Hit Impact Burst VFX - WeaponHitbox.cs, Impact VFX.prefab):**
    * **Tách rời Impact khỏi Slash Trail (Decoupled Impact):** Tắt Impact gắn cứng trong prefab đường kiếm để tránh nổ chớp sáng vô lý khi chém trượt trong không khí.
    * **Kích hoạt Đúng Điểm Chạm (Contact Point Burst):** WeaponHitbox.cs chỉ kích nổ Impact VFX khi va chạm với các mục tiêu thuộc Layer Enemy (9) hoặc Damageable (8) và có thành phần IDamageable/Health.
    * **Định vị & Hướng Tóe:** Sinh ngay tại hitPoint = other.ClosestPoint(transform.position), góc tứa hướng ngược chiều chém (-hitDirection) ra ngoài không khí với độ lệch hitImpactNormalOffset = 0.05m chống lẹm mesh. Kích hoạt đồng thời toàn bộ ParticleSystem con (kể cả root có playOnAwake = false).
* **Hệ thống Tài nguyên Player (PlayerStats.cs):**
  * **Máu (Health):** Gắn liền với Health.cs và interface IDamageable.cs.
  * **Thể lực (Stamina):** Max 100. Tiêu hao khi Lướt/Lăn né (Roll: 20), Chạy nhanh (Sprint: 15/s), Chém thường (15), Chém gồng (30), Giơ thế đỡ hụt (20). Tự động hồi phục sau 0.85s trễ.
  * **Năng lượng Chiêu thức (Spirit / Focus):** Max 200 (chia 4 sao trên HUD, mỗi sao 50 Spirit). **Không tự hồi**, chỉ tích lũy khi chém trúng quái (+12 thường, +30 gồng, +35 counter, +40 parry chuẩn). Tiêu hao cho kỹ năng:
    * **Phím E:** Sóng kiếm chém xa (`ProjectileSlash.cs`, tốn 50 Spirit - 1 sao).
    * **Phím Q:** Phép nổ năng lượng diện rộng (`AoeSpell.cs`, tốn 100 Spirit - 2 sao).
    * **Phím R:** Tuyệt kỹ Tối thượng Càn quét Khổng lồ (`Skill Ult.anim`, tốn 200 Spirit - 4 sao / Max thanh). Nhân vật tụ lực gồng kiếm (Hyper Armor + Invulnerable), chớp sáng ngang lưỡi kiếm (Sword Gleam Flare), đơ game (Freeze Frame / Hitlag 0.1s), Zoom Punch Camera nén lực, sau đó bung nhát chém ngang phẳng cực đại kèm Slow-motion 0.95s thực tế, vệt kiếm Ultra-wide Slash Trail scale 2.6x và Gigantic Cleave Hitbox quét diện rộng bán kính 5.5m hình quạt 240 độ (hỗ trợ Scene View Gizmos trực quan). Đồng bộ hoàn toàn hệ thống Slow-mo qua `ActiveSlowMoScale` và đa dạng phím tắt [T] / [-] / [=].
* **Cơ chế Đỡ đòn & Phản công (Parry & Counter - `PlayerCombat.cs` & `PlayerFeedback.cs`):**
  * Chuột phải (RMB) kích hoạt thế thủ với cửa sổ vàng `parryWindowDuration = 0.28s`.
  * **Hiệu Ứng Tóe Lửa Va Chạm Lưỡi Kiếm (`Parry VFX.prefab`):**
    * Khi đòn đánh của quái chạm vào Player trong cửa sổ Parry, `Health.onCheckParryOrBlock` kích hoạt `PlayerFeedback.ExecuteParrySuccess`.
    * **Định Vị Điểm Chạm Lưỡi Kiếm:** Không dùng tọa độ va chạm dưới thấp; định vị trực tiếp tại **tâm hình học giữa thân lưỡi kiếm** (`swordTransform.TransformPoint(box.center)` với local center Y = 0.81m) kèm tùy biến bù trừ `parryBladeOffset`, đảm bảo tia lửa luôn nổ đẹp mắt ngay trên thân kiếm ngang tầm mắt/ngực khi giơ thế đỡ.
    * **Phản Hồi Thị Giác & Xung Lực:** Khựng hình Hitlag 0.2s (`parryHitlagDuration`), Camera zoom cận cảnh kịch tính (`TriggerParryZoom`), tia lửa vàng/trắng tóe ngược chiều cú chém của quái, ngắt chiêu đòn đánh và làm choáng kẻ tấn công.
  * Parry trúng đòn: Hoàn trả thể lực, nạp mạnh Spirit (+40), làm choáng đối thủ và mở ra đòn Counter sát thương cực lớn.
* **Cơ chế Né đòn & Di chuyển (`PlayerController.cs` & `TargetLockSystem.cs`):**
  * Phím Shift: Nhấn nhả (< 0.25s) để Lăn né với 0.45s thời gian bất tử (I-frames); Giữ (> 0.25s) để Sprint.
  * **Hệ thống Khóa Mục Tiêu Souls-like (`TargetLockSystem.cs`):**
    * Bấm Chuột giữa (Middle Mouse) để bật/tắt khóa mục tiêu gần nhất. Nhân vật chuyển sang thế vờn/bước ngang (`strafeSpeed`), camera tập trung vào đối thủ (`ThirdPersonCamera.cs`).
    * **Chuyển mục tiêu bằng vẩy chuột (Mouse Flick Switch):** Khi đang lock, vẩy chuột sang trái hoặc phải (`delta.x` vượt `mouseFlickThreshold = 20`) để chuyển khóa sang con quái gần nhất ở phía tương ứng trên màn hình (Viewport Space). Tích hợp bộ lọc xung lực tự nhiên (`flickDecayRate = 120/s`) và hồi chiêu `switchCooldown = 0.25s` chống giật nhảy cóc.
    * **Bộ lọc chênh lệch cao độ tầng 2 (`maxLockElevationDifference = 4.2m`):** Triệt tiêu 100% hiện tượng camera ngửa lên trần gác lửng tầng 2 (cao 8m) khi quái tầng 1 chết hoặc khi bấm khóa. Tự ngắt khóa an toàn nếu khoảng cách độ cao vượt quá `breakLockElevationDifference = 6.5m`.
    * Tự động chuyển mượt sang quái tiếp theo khi quái hiện tại chết (`TrySwitchToNextTarget`).
  * Nhảy & Rơi (Jump, Fall, Air Control, phân biệt tiếp đất nhẹ và Hard Landing khựng chân khi rơi từ gác lửng).
* **Đạo diễn Lượt đánh Quái (AttackTokenDirector.cs):**
  * Áp dụng hệ thống Token Pool. Quái bao vây chỉ giữ cự ly vờn; chỉ quái nào xin được "Attack Token" từ Director mới được lao vào tấn công, chống spam hội đồng.

---

### C. Phân Hệ Quái Vật (Monsters) & Trùm Học Máy (ML-Agents Boss)
* **Bộ Sinh Quái Tự Động Theo Biệt Đội (ProceduralEnemySpawner.cs):**
  * **Cơ chế Cụm / Biệt Đội (Squad / Pack System):**
    * Không bó cứng quái theo tầng mà phối hợp linh hoạt Melee & Ranged theo 4 cấp đội hình:
      * Nhóm 4 quái: 2 Melee (1 Warrior + 1 Minion) + 2 Ranged (1 Rogue + 1 Mage).
      * Nhóm 3 quái: 2 Melee + 1 Ranged HOẶC 1 Melee + 2 Ranged.
      * Nhóm 2 quái: 2 Melee HOẶC 1 Melee + 1 Ranged.
      * Nhóm 1 quái lẻ: Random ngẫu nhiên 1 trong 4 loại quái.
  * **Phân bổ theo quy mô phòng (Dungeon 300x300m):**
    * Sàn trệt phòng thường: 1 – 2 nhóm; Sàn trệt đại sảnh lớn (>= 220 tiles): 2 – 3 nhóm.
    * Sàn tầng 2 gác lửng: 1 – 2 nhóm (tỷ lệ 2 nhóm là 25%).
    * Start Room & Boss Room: Miễn nhiễm hoàn toàn (0 quái).
    * Treasure & Shrine Room: 1 nhóm nhỏ (1–2 quái) canh giữ.
  * **Định vị Cụm (Spatial Clustering):** Mỗi nhóm chọn 1 Điểm Neo (Anchor Point), quái tụ tập bán kính 3m quanh tâm, các nhóm cách nhau tối thiểu 8.5m. Nắn chuẩn 100% lên bề mặt NavMesh và dùng Physics.CheckSphere chống kẹt vào đạo cụ tĩnh.
* **Bộ Tối Ưu Hóa Ngủ Đông Quái Vật (DungeonEnemyOptimizer.cs):**
  * **Cơ chế 3 Vùng LOD & Ngủ Đông (Hibernation):**
    * *Thức tỉnh hoàn toàn (< 40m):* Quái chạy đầy đủ AI, NavMeshAgent di chuyển và Animator xương.
    * *Vùng đệm Hysteresis (40m - 48m):* Độ đệm 8m chống giật hoặc bật/tắt liên tục khi người chơi đứng sát ngưỡng.
    * *Ngủ đông triệt để (> 48m):* Dừng NavMeshAgent (ngắt ObstacleAvoidanceType.NoObstacleAvoidance), đặt Animator.cullingMode = CullCompletely, ngắt lệnh Update() AI -> Giảm 0% chi phí CPU/GPU cho ~200 quái vật ở xa.
  * **Time-Slicing Batching (4 Batches, 0.12s):** Danh sách 200 quái được chia đều thành 4 nhóm để luân phiên tính khoảng cách tới người chơi, triệt tiêu hoàn toàn CPU Frame Spike.
  * **Đánh thức Tức thì (Instant Combat Wakeup):** Khi quái đang ngủ bị người chơi tấn công (bắn tên, phóng chưởng từ xa), hàm OnTakeDamageAggro lập tức đánh thức quái dậy phản xạ chiến đấu.
* **Quái thường (Bộ xương Skeleton):**
  1. SkeletonWarriorAI: Chiến binh cận chiến, biết leo cầu thang lên/xuống tầng 2 để truy sát người chơi.
  2. SkeletonMinion: Quái cận chiến số lượng đông, máu yếu nhưng đánh bọc lót.
  3. SkeletonRogue: Thích khách cơ động, bắn cung tầm xa (Skeleton_Arrow.prefab), biết giữ khoảng cách.
  4. SkeletonMage: Pháp sư bắn đạn ma pháp cầu đuổi (Light Star.prefab), thi triển vùng đất bốc cháy (MageGroundSpell.prefab) và cột sáng trừng phạt (LightPillarSkill.prefab).
* **Bộ Đôi Trùm Hầm Ngục & Cặp Đấu Souls-like (Dual Boss Encounters):**
  1. **Trùm Băng: Ice Golem (Boss_IceGolem.prefab - The Frost Juggernaut):**
     * **Chỉ số sinh tồn & Bảng thông số Inspector (=== BANG THONG SO BOSS (CHINH TRUC TIEP TRONG UNITY INSPECTOR) ===):**
       * **Máu tối đa (bossMaxHealth = 1500 HP):** Máu trùm Souls-like dày dặn, tự động đồng bộ vào component Health khi khởi động game và trong Editor.
       * **Thanh chịu đòn Poise (maxPoise = 500 điểm):** Độc lập hoàn toàn với sát thương máu, không tự hồi phục trong trận chiến. Bị bào mòn theo độ uy lực và Knockback của từng đòn đánh từ Player (Chém thường: 35, Chém 3 / Charge 1: 50, Chém gồng 2 / Skill E: 70, Counter / Phép Q: 100). Cung cấp event onPoiseChanged(float current, float max) và alias onStaminaChanged cho UI kết nối.
       * **Cơ chế Suy yếu (WeakenIG):** Khi Poise về 0, kích hoạt bước lùi suy yếu duy nhất 1 lần trong trận đấu (hasTriggeredWeaken = true). Hoạt ảnh chạy trọn vẹn 100% độ dài clip theo đúng tốc độ người chơi cài đặt (kể cả khi tua chậm Speed = 0.5). Tự động chốt tọa độ gốc mới (CommitWeakenPosition) để không bị giật lùi về chỗ cũ; không đứng yên bất động sau clip (weakenFreezeAfterDuration = 0s), lập tức chuyển về Idle sẵn sàng tiếp tục chiến đấu.
       * **Phase 2 Cuồng Nộ (Enrage Sequence khi HP <= 50% / 750 HP):** Kích hoạt hoạt cảnh chao đảo 50%HP (4.2s), phát sóng chấn động đẩy lùi Player bán kính 5m (lực đẩy 4.5m) kèm âm thanh và VFX chấn động sàn, gầm thét phun lửa AttackRoaring, đợi 3.0s rồi phóng liên tiếp 3 quả cầu tím IceBallIG dồn dập. Nhịp độ giao tranh (combatPacing) tăng tốc từ 6-7s xuống 4-5s với tần suất tấn công hung hãn.
     * **Bộ chiêu thức hoàn chỉnh (7 Kỹ năng chiến đấu trên IceGolemController.cs):**
       * Attack1 *(Phím 1)*: Đấm thẳng cận chiến kết hợp hiệu ứng va đập Impact VFX (A VFX Prefabs/Explossion/Impact).
       * GumoGumo Slam *(Phím 2)*: Đấm liên hoàn xuống sàn rung đất 2 tay, tích hợp bộ thông số bắt điểm chạm đất thực tế (gumoVisualHeightOffset = 0.35m, gumoHandContactHeight = 0.55m, gumoHandMinHeight = 0.30m, gumoHandReleaseHeight = 0.95m) chống chìm chân vào sàn, bùng nổ hiệu ứng Attack1 Effect tại từng vị trí bàn tay tiếp đất.
       * FlameBreath *(Phím 3)*: Phun lửa hình nón (Flame Cone) tầm trung 15.5m đa tầng sát thương.
       * JumpAttack *(Phím 4)*: Lao bổ / vồ tới mục tiêu từ cự ly xa 20m, phát sinh sóng chấn động tỏa vòng tròn (Radial Shockwave).
       * RunAttack *(Phím 5)*: Chạy lao húc thẳng vào Player với vận tốc 12m/s và đẩy văng mạnh mục tiêu; hỗ trợ slot trống runAuraVfxPrefab để người dùng tự do kéo thả VFX hào quang xanh quanh thân.
       * AttackLT_IG *(Phím 6)*: Chuỗi combo 3 đòn đánh thường liên hoàn cực đẹp; hỗ trợ slot trống handAuraVfxPrefab để kéo thả VFX bao quanh 2 bàn tay.
       * IceBallIG *(Phím 7)*: Gồng tụ quả cầu ma lực tím Purple Orb lơ lửng chính giữa hai bàn tay trên trời (GetBetweenHandsPosition()), bám theo tay vươn cao và phình to dần từ 0.4 lên 2.2, sau đó bổ hai tay ném thẳng về phía Player với vận tốc 16m/s và phát nổ diện rộng khi va chạm.
       * JumpDown *(Mở màn duy nhất 1 lần khi phát hiện Player / One-Time Intro Cutscene)*: Đáp đất mở màn ấn tượng từ trên cao 12-16m kèm shockwave rền vang lan tỏa từ chân Boss. Tự động kích hoạt khi lần đầu phát hiện Player trong tầm 35m và khóa vĩnh viễn sau cú đáp đất đầu tiên (hasTriggeredJumpDown = true), không cho phép dùng đi dùng lại trong trận đấu (đã gỡ bỏ phím Space để tránh spam).
     * **Hệ thống Chiếu sáng Tự Thân & Đạo Diễn Cutscene Intro Bục Đá:**
     * **Cơ Chế Khựng Hoạt Ảnh Lấy Đà (Timing Mixup / Hold Wind-up - Kiểu 2 Souls-like):**
       * Tích hợp trực tiếp vào 5 kỹ năng: Attack 1 (đấm), GumoGumo Slam (đập đất), Flame Breath (ngửa cổ lấy hơi), Run Attack (vào thế phi nước đại), AttackLT_IG (vung combo 3 đòn). **Loại trừ hoàn toàn Chiêu 4 (JumpAttack) và Chiêu 7 (IceBall)**.
       * Bảng thông số Inspector trực quan: `enableTimingMixup` (bật/tắt), `timingMixupChance` (tỷ lệ kích hoạt, mặc định 1.0 = 100%), `windupWaitTime` (0.2s lên đỉnh lấy đà), `minHoldDuration` (0.5s) đến `maxHoldDuration` (0.8s), `holdAnimationSpeed` (0.02f nén cơ siêu chậm).
       * Đồng bộ tuyệt đối: Hitbox và timeline gây sát thương tự động lùi khớp với thời gian khựng, bảo đảm đòn giáng xuống chạm đích đúng frame tiếp xúc thực tế và trừng phạt triệt để nhịp lăn né hoảng loạn (Roll-Catching).
       * **Hào quang băng giá (Boss_Aura_Light):** Tự sinh point light xanh băng rực rỡ (bodyAuraColor, range 16m, intensity 3.0) bám theo ngực Boss cao 2.5m, giúp Boss nổi bật ấn tượng trong bóng tối hầm ngục.
       * **Chế độ Standby trên Bục Đá (SetStandbyOnPedestal):** Cố định Boss trên bục đá, đóng băng Rigidbody kinematic chờ Cutscene mở màn.
       * **Nhảy từ bục đá & Phun băng chào sân:** Hỗ trợ PlayIntroJumpDownFromPedestal (nhảy vòng cung từ bục xuống sàn đấu) và PlayIntroBreath (phun lửa nón chào sân).
     * **Hệ thống Di chuyển 4 hướng & Chuẩn Hóa Rig Humanoid:**
       * Chuẩn hóa 100% 14 State trong BossController.controller sang Humanoid Sub-asset trong file FBX (internalID: -203655887218126122), triệt tiêu hoàn toàn lỗi chìm nửa người xuống đất của các file animation Generic cũ.
       * Nhận diện 4 hướng di chuyển: Tiến (Walk), Lùi (WalkBackIG), Dạt Trái (WalkLeftIG), Dạt Phải (WalkRightIG) trong khi vẫn khóa mặt bám sát Player (strafeMode = true). Điều khiển thủ công qua **4 phím mũi tên (Arrow Keys)** tách biệt độc lập với phím W, A, S, D của Player.
     * **Khóa mục tiêu & Tự động hóa an toàn:**
       * Tự động phát hiện Player (autoFindPlayer), tự xoay mặt đối diện đối thủ (alwaysFacePlayer, facePlayerOnAttack, tốc độ facePlayerSpeed = 240 deg/s).
       * Cơ chế tự bảo vệ chống duplicate component trong Awake() và Menu dọn dẹp Scene Editor (Infinite Labyrinth -> Boss -> 3. Dọn Dẹp Scene & Khắc Phục Lỗi Ice Golem).
     * **Đạo Diễn Điện Ảnh Chiến Thắng, Biểu Ngữ "VICTORY ACHIEVED" & Bục Đá 12m (BossVictoryDirector.cs - Task D02-5B & D02-5C):**
       * Lắng nghe sự kiện IceGolemController.OnBossDefeated khi Boss cạn máu (hoặc phím K trong Play Mode).
       * Khựng hình Hitlag 0.22s (Time.timeScale = 0.05f) tôn vinh nhát chém kết liễu, tạm khóa điều khiển Player.
       * Camera Cinematic trượt mượt mà tới góc 3/4 mạn phải (cự ly 7.0m, cao 3.5m) ngắm Boss phát hoạt ảnh ngã gục Dead xuống sàn đấu trường kèm camera drift chậm.
       * Biểu ngữ hoàng kim Souls-like BossVictoryBanner trong EnemyCanvas bừng sáng giữa màn hình (Fade In, Breathing Scale, giữ sáng 2.2s rồi Fade Out).
       * **Đạo diễn Bục Đá 12m & Sinh Thưởng (Task D02-5C):** Ngay sau khi banner tắt, camera lia mượt mà lên đỉnh Bục Đá cao 12m (BossArena_Pedestal) ở vách Bắc; đồng loạt sinh Rương Boss (Interactive_Chest_Boss) và Cổng Dịch Chuyển (VictoryPortal) tại các socket [BossChest_Socket] và [Portal_Socket]; kích nổ cột sáng thần thánh (Point Light range 24m) và giữ camera ngắm bục 2.2s trước khi trượt êm ái quay về sau lưng Player và mở lại toàn bộ điều khiển.
       * **Vô hiệu hóa Reset Episode ML-Agents:** Khóa cờ respawnBossOnDeath = false trên IceGolemAgent.cs và Boss_IceGolem.prefab, bảo đảm Boss nằm gục vĩnh viễn trên sàn khi tử trận và không vô tình reset episode khiến Boss quay lại bục hay Player bị dịch chuyển về phòng khởi đầu.

  2. **Trùm Lửa: Red Ignis (Boss_RedIgnis.prefab - The Crimson Duelist / True Flame Ignis):**
     * **Định vị chiến đấu:** Kiếm sĩ Duelist tốc độ cao, xoay mặt theo Player mượt mà (`facePlayerSpeed = 110 deg/s` mặc định; tăng 1.5x khi lấy đà), chủ động áp sát và vờn đối thủ ở cự ly gần (`optimalDistanceMin..Max = 1.0 - 2.0m`), sở hữu thanh kiếm ma thuật rực lửa.
     * **Chỉ số sinh tồn & Bảng thông số Inspector (`RedIgnisController.cs` & `Boss_RedIgnis.prefab`):**
       * **Máu tối đa (`bossMaxHealth = 1200 HP`):** Máu chuẩn đồng bộ tự động vào component `Health` và khi hồi sinh hiệp đấu (`Revive()`).
       * **Giáp & Thanh chịu đòn Poise:** `bossDefense = 25`, `maxPoise = 400` (mặc định script; `Boss_RedIgnis.prefab` đặt `500`), độc lập với sát thương máu, không tự hồi phục trong trận. Bị bào mòn bởi các đòn đánh Player; khi Poise về 0 kích hoạt trạng thái suy yếu `Weaken` (`weakenFrontDuration` / `weakenBackDuration`) mở cửa sổ phản công.
       * **Vận tốc di chuyển tách hướng:** Tốc độ tiến tới `moveSpeed = 4.2m/s`, **tốc độ đi lùi riêng biệt `backwardSpeed = 1.8m/s`** (chậm hơn đi tới để Player dễ áp sát đổi đòn và không bị boss lùi câu giờ), tốc độ dạt ngang giảm còn `strafeSpeed = 1.9m/s` giúp dáng bước ngang đầm chắc và đồng bộ `WalkSpeed` theo tốc độ thực tế (`referenceSpeed`: tiến `3.8`, lùi `2.4`, ngang `2.2`, clamp `0.60..1.55`).
     * **Cơ Chế Gồng Kiếm Thức Tỉnh Phase 2 (`SwordCastingRI` tại 50% Máu):**
       * Khi máu Boss tụt xuống dưới 50% (`swordCastingHealthThreshold = 0.50f`), Boss tạm ngưng di chuyển và kích hoạt hoạt ảnh gồng kiếm `SwordCastingRI` (`swordCastingTotalDuration = 2.0s`, bật lửa ở `0.85s`).
       * Thắp sáng hiệu ứng ngọn lửa ma thuật dọc sống kiếm (`IgnisSwordFlame.cs`), bật cờ `IsPhase2` cho hệ thống AI (`RedIgnisAgent`), và cộng thêm vĩnh viễn `+10` sát thương (`swordEnrageBonusDamage = 10.0f`) cho toàn bộ chiêu thức đến hết trận.
     * **Bộ Chiêu Thức Hoàn Chỉnh & Thông Số Tầm Bay Inspector (`RedIgnisController.cs`):**
       * Skill 1: Attack Slash (Chém Dọc) *(Phím 1)*: Đòn vung kiếm chém dọc phóng đạn kiếm xoay 90° (`Ignis_Slash_Vertical.prefab`, tốc độ `22m/s`, script default damage `65` / Prefab `30`, delay `0.42s` / Prefab `1.4s`). Tầm bay tối đa chỉnh trực tiếp qua **`attackSlashMaxDistance` (mặc định `18.0m`)** và được thực thi bởi `RedIgnisSlashProjectile.cs` (`maxDistance`).
       * Skill 2: Horizontal Slash (Chém Ngang) *(Phím 2)*: Đòn chém quét ngang phóng sóng kiếm bay là mặt sàn (`Ignis_Slash_Horizontal.prefab`, tốc độ `20m/s`, script default damage `50` / Prefab `30`, delay `0.40s` / Prefab `1.2s`). Tầm bay tối đa chỉnh trực tiếp qua **`horizontalSlashMaxDistance` (mặc định `16.0m`)**.
       * Skill 3: Jump Attack (Nhảy Áp Sát Dậm Đất AoE) *(Phím 3)*: Boss nhún đà (`windup 0.25s`) rồi bay parabol (`apexHeight = 1.8m`) thẳng tới ngay trước mặt Player và giáng kiếm nổ AoE bán kính `4.5m` (script default damage `80` / Prefab `30`, cooldown `10s` / Prefab `5s`). Tích hợp đầy đủ 4 thông số Inspector: **`jumpLeapDistance` (`25.0m`)**, **`jumpStopOffset` (`1.3m` — đáp & đặt điểm nổ `slamPoint` ngay trước mặt Player)**, **`jumpLeapSpeed` (`12.0m/s`)**, và **`jumpAttackLeapDuration` (`0.85s` — thời gian bay tối thiểu giúp người chơi quan sát và né đòn)**.
       * Skill 4: Combo Attack (Chém 4 Nhát Liên Hoàn) *(Phím 4)*: Chuỗi combo 4 nhát (`ComboAttackRI.anim`, script default cooldown `12.0s` / Prefab `6.0s`, damage `35` / Prefab `15` mỗi nhát, `comboHitRadius` script `3.2m` / Prefab `2.0m`, `comboLungeDistance` script `0.55m` / Prefab `1.0m`), đồng bộ 4 nhịp `comboHitTimers = { 0.40s, 0.95s, 1.55s, 2.20s }`, sửa triệt để Root Yaw lệch `+57.3° -> 0.0°`.
     * **Cơ Chế Khựng Hoạt Ảnh Lấy Đà (Timing Mixup / Hold Wind-up - Kiểu 2 Souls-like):**
       * Tích hợp trực tiếp vào 3 kỹ năng: Skill 2 (Horizontal Slash - quét kiếm ngang), Skill 3 (Jump Attack - nhún chân gồng thế dưới sàn), Skill 4 (Combo Attack - vung nhát đầu tiên). **Loại trừ hoàn toàn Chiêu 1 (AttackSlash - chém dọc)** theo đúng yêu cầu thiết kế.
       * Bảng thông số Inspector trực quan (`RedIgnisController.cs`): `enableTimingMixup` (bật/tắt), `timingMixupChance` (tỷ lệ kích hoạt, mặc định 1.0 = 100%), `windupWaitTime` (0.2s lấy đà lên đỉnh thế), `minHoldDuration` (0.5s) đến `maxHoldDuration` (0.8s), `holdAnimationSpeed` (0.02f nén cơ siêu chậm).
       * Đồng bộ chính xác: Hitbox và timeline gây sát thương (kể cả nhát chém đầu combo và cú dậm đất) tự động lùi khớp với thời gian khựng, xoay mặt theo Player (`facePlayerSpeed * 1.5x`) để khóa góc đánh, trừng phạt phản xạ lăn né sớm (Panic Roll-Catching).
     * **Hệ thống Scene View Gizmos (`OnDrawGizmosSelected` & `DrawGroundCircle`):**
       * Hiển thị trực quan ngay trong Scene View khi chọn Boss: đường ngắm tới Player (màu hồng), tầm bay tối đa chiêu 1 & 2 (vàng cam / cam đỏ), đường nhảy tối đa + vòng nổ AoE JumpAttack (vàng) & dự đoán điểm đáp live vào Player (cam đỏ), cùng quãng lướt/hitbox 4 nhát ComboAttack (xanh cyan). Mọi vòng Gizmos tự động co giãn theo số chỉnh trong Inspector.
     * **Hệ Thống Vệt Kiếm Độc Quyền Trực Tiếp Trên Kiếm (IgnisSwordTrail.cs):**
       * Gắn trực tiếp trên prefab vũ khí Ignis_Sword_Flame.prefab tại 2 điểm neo mũi kiếm (`tipTransform`) và gốc kiếm (`baseTransform`).
       * Dựng Dynamic Ribbon Mesh mượt mà với thuật toán nội suy Catmull-Rom Spline (3 subdivisions), triệt tiêu hoàn toàn góc gãy khi vung kiếm tốc độ cao.
       * Bộ lọc khử giật rung lắc (`verticalJitterDamping` = 0.35f) và làm mượt chuyển động tay (motionSmoothing = 0.45f).
       * Dải màu Gradient đỏ thẫm pha viền đen khói (Dark Crimson to Deep Red/Black), tan nhanh trong 0.16s (`trailDuration` = 0.16s), hòa trộn Alpha Blended chuẩn URP.
       * Tự động bật ngay từ đầu nhịp vung ở cả 4 kỹ năng và tự động tắt khi chiêu kết thúc hoặc khi Boss bị suy yếu/chết.
     * **Quy Chuẩn Bộ Công Cụ Setup Editor Không Phá Hủy (Non-Destructive Tooling):**
       * Gỡ bỏ hoàn toàn [InitializeOnLoadMethod] trong SetupRedIgnisAnimatorAndPrefabs.cs và InspectAndFixRedIgnis.cs, loại bỏ hiện tượng tự chạy ngầm đè thông số mỗi khi Unity compile/reload domain.
       * Hàm SetDefaultFloatIfUnset bảo toàn 100% dữ liệu tùy chỉnh của người dùng trong Inspector (như `attackSlashDelay`, comboLungeDistance, v.v.), không bao giờ tự ý khôi phục về giá trị mặc định.
       * Sửa lỗi xung đột biến cục bộ CS0136, bảo đảm 0 lỗi biên dịch trong toàn bộ dự án.
     * **Hệ thống Vật lý Tóc Đuôi Mũ & Áo Choàng 3 Dải (`UnityChanSpringBone` + `RedIgnisClothMotion.cs`):**
       * Giữ cố định đốt gốc tóc đưa ra khỏi mũ (`Bone`) và 4 leaf bones (`Bone.013`, `Bone.037`, `Bone.025`, `Bone.035`); mô phỏng **14 `SpringBone` tắt dần tới hạn** cho Tóc (`Bone.002 -> Bone.004 -> Bone.006`: `stiffnessForce` `24→10`, `dragForce` `0.30→0.60`) và Áo choàng 3 dải Trái (`Bone.014 -> Bone.015 -> Bone.016 -> Bone.018`), Giữa (`Bone.021 -> Bone.022 -> Bone.023`), Phải (`Bone.026 -> Bone.028 -> Bone.030 -> Bone.034`).
       * Thông số rũ mượt chống xoắn vai/văng cánh (`gravity = (0, -22, 0)`, `friction = 1.0`, tắt `enableAngleLimits` và `enableLengthLimits`): 3 dải áo dùng `stiffnessForce` thấp (`5.0→1.5`, dải giữa `5.0→2.0`) và `dragForce` tăng dần về vạt (`0.25→0.70`) để miễn nhiễm với các pha xoay vai Mixamo mạnh mà không kéo chéo `lengthLimitTargets`.
       * Gắn **16 Collider giải phẫu** chống xuyên thân thể/tay/chân (4 Sphere: `CapeCol_Head`, `CapeCol_Neck`, `CapeCol_Left/RightShoulder`; 9 Capsule: `CapeCol_UpperChest`, `CapeCol_Torso`, `CapeCol_Hips`, `CapeCol_Left/RightArm`, `CapeCol_Left/RightThigh`, `CapeCol_Left/RightShin`; và 3 Back Panel: `CapeCol_HeadBackPanel`, `CapeCol_TorsoBackPanel`, `CapeCol_PelvisBackPanel` bù trừ scale qua `cachedBackPanels`) kết hợp giới hạn mặt sàn `groundClearanceOffset = 0.04m`.
       * **`RedIgnisClothMotion.cs` (`ForceProvider`, `[DefaultExecutionOrder(-50)]`):** Bước Verlet cố định `60Hz` (nhận `Time.timeScale`, tự reset pose khi dịch chuyển >= `1.5m` hoặc hồi sinh, tự phục hồi cấu trúc qua `NeedsRuntimeAutoHeal`); chặn lộn ngược lên vai/đầu (`ClampUpwardDrape`), kiểm tra va chạm đa điểm dọc thân xương & vạt lá (`EnforcePostStepSegmentCollisions`, `PropagateParentRotationToDescendants`, `ClampRelativeVerletSpeed` `0.060m`), tăng drag tạm thời khi xoay gắt/đi ngang/đáp đất (`ApplyDynamicTurnDamping`).
       * **Dáng áo choàng chữ A & Chú thích Inspector tiếng Việt:** Tạo độ xòe nhẹ tăng tiến ở vạt dưới 2 dải trái/phải (`capeHemFlare = 0.85f + 5.15f * chainT`), mở rộng thêm khi chạy thẳng (`wingSpreadStrength = 1.65f`, tự hãm khi đi ngang), và đồng bộ sóng bước chân theo `WalkSpeed` trên 4 state `Walk*RI`. Toàn bộ 4 nhóm (`Mô phỏng vật lý`, `Chống xuyên sàn`, `Chuyển động áo choàng & tóc`, `Hãm rung khi xoay & đi ngang`) và 17 thông số trên Inspector đều có `[Header]` và `[Tooltip]` tiếng Việt chi tiết.
       * Menu `Infinite Labyrinth -> Boss -> Red Ignis -> Tự Động Sửa Lỗi Áo Choàng (Auto Fix Cape)` (`SpringManagerHelper.cs`) tự động cấu hình trực tiếp lên `Boss_RedIgnis.prefab` lẫn instance trong Scene.
     * **Chuẩn Hóa Khung Xương Humanoid & Animation:**
       * Sửa lỗi gốc tự động map nhầm Hips -> Armature trong RedIgnis.fbx, trỏ chuẩn xác về mixamorig:Hips.
       * Chuẩn hóa hoạt ảnh bước đi Ignis_Walk.fbx với Based Upon = Feet / Center of Mass, nâng Offset cao độ chống hiện tượng lún đất hoặc co ngắn chiều cao thân thể.
     * **Vũ Khí Kiếm Siu Nhân Đỏ (KiemSiuNhanDo.fbx):**
       * Thiết lập Import Settings chuẩn vũ khí tĩnh: Animation Type: None (tab Rig), tắt Import Animation.
       * Gắn vào tay phải qua điểm neo RightHand_Socket trên xương mixamorig:RightHand, đồng bộ chuyển động 100% theo mọi cử động bàn tay.
       * **Giải quyết góc lệch xương cổ tay FBX gốc:** Xương mixamorig:RightHand trong file FBX gốc mang góc xoay nghỉ Euler(-1.1°, -45.8°, 2.7°). Tool biên tập IgnisWeaponAligner.cs tích hợp chuẩn hóa T-Pose bằng HumanPoseHandler (đặt 100% muscles = 0), cố định vị trí chuôi kiếm vào lòng bàn tay Hand_R_Weapon:
         + localPosition: (0.175, 0.094, -0.074)
         + localRotation: Euler(-0.953, -53.529, 83.786) (quaternion: x: -0.306, y: -0.330, z: 0.593, w: 0.667)
         + localScale: (0.8, 0.8, 1.0) (Bảo toàn 100% tỷ lệ kiếm mỹ thuật do người dùng cấu hình).
     * **Kiến Trúc Điều Khiển & Giao Tiếp Học Máy (Brain - Body):**
       * **Body (`RedIgnisController.cs`):** Quản lý 4 chiêu thức, hoạt ảnh, hệ thống hồi chiêu độc lập, thanh máu 1200 HP, Poise, trạng thái thức tỉnh kiếm lửa Phase 2 (`SwordCastingRI`), hồi sinh hiệp đấu (`Revive()`) và bộ phím nóng thử nghiệm thủ công (Phím 1..4 và phím Mũi Tên di chuyển 4 hướng).
       * **Brain (`RedIgnisAgent.cs`) & Đấu trường (`RedIgnisBattleArena.cs`):** Bộ não ML-Agents (27 Vector Observations, Action Space `[5, 5, 2]`), giao tiếp với Body qua `Move(h, v)`, `CastAttack(skillIndex)`, `CanCastAttack(skillIndex)`, `GetCooldownNormalized(skillIndex)`, `IsPhase2` và `GetSkillEffectiveMaxRange(skillIdx)`; phối hợp trọng tài `isManagedByArena` với `RedIgnisBattleArena` để tránh trùng lặp reset/thưởng kép khi train liên hoàn.

---

### D. Kiến Trúc AI Souls-like & Học Máy Tăng Cường (Boss ML-Agents Architecture)

#### 1. Triết Lý Thiết Kế Hành Vi Trùm Souls-like (Souls-like Combat Dynamics)
Để tạo ra trải nghiệm giác đấu nghẹt thở chuẩn phong cách Souls-like (*Elden Ring, Dark Souls, Sekiro*), AI không được phép là "bot xả chiêu vô tội vạ" mà phải tuân thủ nghiêm ngặt 5 nguyên lý:

mermaid
flowchart TD
    subgraph SoulsCombatLoop ["Vòng Lặp Chiến Đấu Souls-like"]
        Stalking["1. Thăm Dò & Vờn (Pacing / Strafing)<br/>Dạt vòng quanh khóa mục tiêu, giữ cự ly 4-7m"] --> DetectAction{"Phát hiện hành động Player"}
        
        DetectAction -->|Player Uống Máu / Dùng Thuốc| HealPunish["2. Trừng Phạt Hồi Máu (Heal-Punish)<br/>Lao vồ / Chém lướt lập tức ngắt chiêu"]
        DetectAction -->|Player Lăn Né Hoảng Loạn| RollCatch["3. Bắt Nhịp Lộn (Roll-Catching)<br/>Giữ đòn gồng delay, chém trúng khi hết I-frame"]
        DetectAction -->|Player Giơ Khiên Đỡ Đòn| FlankBreak["4. Luồn Sườn & Phá Thủ (Flank & Guard Break)<br/>Dạt ra góc chết sau lưng / Dậm đất phá Poise"]
        DetectAction -->|Player Thăm Dò / Đứng Yên| TelegraphAttack["5. Ra Chiêu Báo Hiệu (Telegraphed Attack)<br/>Wind-up rõ ràng -> Tung đòn -> Recovery hở sườn"]
    end
    
    TelegraphAttack --> PunishWindow["Cửa Sổ Trừng Phạt (Player Punish Window)<br/>Boss khựng 0.8s - 1.5s cho Player phản công"]
    PunishWindow --> Stalking


1. **Báo Hiệu Đòn Đánh (Tell & Telegraphing):** Mỗi chiêu thức chia làm 3 pha rõ rệt:
   * *Pha Gồng (Wind-up):* Giơ cao tay / ngửa cổ lấy đà rõ ràng, kèm âm thanh và VFX tụ lực để người chơi kịp phán đoán né hoặc Parry.
   * *Pha Tung Đòn (Active/Release):* Chém chớp nhoáng hoặc dậm sàn uy lực, gây sát thương.
   * *Pha Hồi Chiêu Khựng Đòn (Recovery Window):* Sau khi ra đòn, Boss có 0.8s - 1.5s thở/thu thế (Punish Window) để người chơi giỏi có cơ hội lao vào phản kích trừng phạt.
2. **Nhịp Thở & Vờn Đối Thủ (Pacing & Circling/Strafing):** Boss không cắm đầu chạy thẳng. Boss biết **bước dạt ngang (Strafe Left / Right)** vòng quanh người chơi ở cự ly trung bình, quan sát thể lực và thế thủ của người chơi để tạo áp lực tâm lý như một kẻ săn mồi.
3. **Bắt Nhịp Lộn Hoảng Loạn (Roll-Catching):** Khi Player hoảng loạn bấm Lăn né liên tục (Panic Roll), AI học được cách **giữ đòn trễ nhịp (Delayed Attack / Tracking Swing)** để vung đòn trúng ngay khoảnh khắc Player vừa chạm đất hết thời gian bất tử (I-frames).
4. **Trừng Phạt Khi Dùng Bình Máu (Heal-Punish / Input Reading):** Khi phát hiện Player lùi xa uống máu (isHealing = true), Boss lập tức kích hoạt chiêu áp sát nhanh (JumpAttack của Golem hoặc Lướt kiếm của Ignis) để trừng phạt sơ hở.
5. **Phân Định Cơ Chế Giai Đoạn (Phase 2 Cuồng Nộ & Kiếm Lửa Thức Tỉnh tại 50% HP):**
   * **Ice Golem (Phase 2 Cuồng Nộ - Enrage Sequence tại 50% HP):** Trận đánh chia làm 2 giai đoạn: Giai đoạn 1 (HP > 50%) rình rập, đánh đòn chuẩn nhịp; Giai đoạn 2 (HP <= 50% / 750 HP) bùng nổ chuỗi chuyển pha Cuồng nộ (chao đảo 50%HP, sóng đẩy lùi người chơi, gầm thét phun lửa AttackRoaring, xả 3 cầu tím ma thuật dồn dập, và rút ngắn nhịp thở combat pacing từ 6-7s xuống 4-5s tạo sức ép áp đảo). Đồng thời sở hữu thanh Poise (500 điểm) độc lập gây trạng thái Suy yếu WeakenIG 1 lần duy nhất.
   * **Red Ignis (Thức Tỉnh Kiếm Lửa Phase 2 tại 50% HP):** Khi máu tụt xuống dưới `50%` (`600 HP`), Boss kích hoạt động tác gồng kiếm `SwordCastingRI` (2.0s), thắp sáng ngọn lửa trên lưỡi kiếm (`IgnisSwordFlame.cs`), bật cờ `IsPhase2` truyền cho mạng nơ-ron `RedIgnisAgent` và gia tăng vĩnh viễn `+10` sát thương (`swordEnrageBonusDamage`) cho toàn bộ chiêu thức về sau.

---

#### 2. Thiết Kế Mạng Nơ-ron ML-Agents (Reinforcement Learning with PPO)

* **Không Gian Quan Sát (Observation Vector - 29 floats chuẩn hóa trong IceGolemAgent.cs):**
  | Nhóm dữ liệu | Số chiều | Ý nghĩa chi tiết |
  | :--- | :---: | :--- |
  | **Vị trí tương đối tới Player** | 2 | Tọa độ phẳng chuẩn hóa tới Player (toPlayer.x / arenaRadius, toPlayer.z / arenaRadius) |
  | **Khoảng cách tới Player** | 1 | Khoảng cách thực tế chuẩn hóa (dist / arenaRadius) |
  | **Góc quay đối diện** | 2 | Góc quay mặt Boss so với hướng Player dạng Cos & Sin liên tục (cosAngle, sinAngle) |
  | **Sinh mệnh Boss** | 1 | Tỷ lệ máu hiện tại của Boss (bossHealth / 1500f) |
  | **Trạng thái bận của Boss** | 1 | Boss đang bận ra đòn / nhảy (controller.IsBusy() ? 1f : 0f) |
  | **Thời gian hồi chiêu 7 Kỹ năng** | 7 | Tỷ lệ hồi chiêu của 7 kỹ năng chiến đấu (GetCooldownNormalized(0..6)) |
  | **Sinh mệnh Player** | 1 | Tỷ lệ máu hiện tại của Player (playerHealth.CurrentHealth / playerHealth.MaxHealth) |
  | **Thể lực Player** | 1 | Tỷ lệ Stamina của Player (playerStats.CurrentStamina / playerStats.MaxStamina) |
  | **Trạng thái chiến đấu Player** | 5 | IsAttacking, IsBlockingOrParrying, IsRolling (I-frame), isPlayerInRollRecovery (Cơ hội Roll-Catch), IsGrounded |
  | **Vận tốc di chuyển Player** | 2 | Vận tốc tương đối dọc và ngang của Player theo góc nhìn Boss (Mathf.Clamp(vel / 10f, -1f, 1f)) |
  | **Cảm biến tường đấu trường** | 4 | Raycast khoảng cách tới tường 4 hướng: Trước, Sau, Trái, Phải |
  | **Vị trí Boss so với tâm sàn** | 2 | Tọa độ phẳng của Boss so với tâm đấu trường (toCenter.x / arenaRadius, toCenter.z / arenaRadius) |

* **Không Gian Hành Động (Action Space - Discrete Branches):**
  * **Branch 0 - Di chuyển & Vờn đối thủ (5 lựa chọn):**
    * 0: Đứng thủ thế / Thăm dò (Idle)
    * 1: Tiến thẳng áp sát (Move Forward)
    * 2: Lùi lại giữ cự ly an toàn (Move Backward)
    * 3: Dạt trái vòng quanh Player (Strafe Left)
    * 4: Dạt phải vòng quanh Player (Strafe Right)
  * **Branch 1 - Kỹ năng chiến đấu (8 lựa chọn):**
    * 0: Không ra chiêu (Chờ thời cơ hoặc giữ nhịp thở)
    * 1: Đòn đánh thường cận chiến (Attack 1)
    * 2: Đấm sàn rung đất liên hoàn 2 tay (GumoGumo Slam)
    * 3: Phun lửa hình nón tầm trung (Flame Breath)
    * 4: Nhảy vồ áp sát tầm xa 20m kèm shockwave tròn (Jump Attack)
    * 5: Chạy lao húc thẳng vận tốc 12m/s (Run Attack)
    * 6: Combo 3 đòn liên hoàn tăng dần sát thương (AttackLT_IG)
    * 7: Tụ quả cầu ma lực tím ném xa 16m phát nổ (IceBallIG)
  * **Branch 2 - Tùy biến Nhịp đòn (2 lựa chọn):**
    * 0: Đánh ngay với tốc độ chuẩn (Immediate timing)
    * 1: Đánh trễ 0.35s để bắt bài roll hoảng loạn (Delayed Attack / Roll-catcher)

* **Thiết Lập Hàm Thưởng / Phạt (Reward Function):**
  | Hành vi | Điểm thưởng/phạt | Mục đích huấn luyện |
  | :--- | :---: | :--- |
  | **Đánh trúng Player (khi không bị né/parry)** | +1.0 * (damage / 20f) | Khuyến khích tấn công chính xác |
  | **Trừng phạt thành công khi Player uống máu** | +2.0 | Tạo phản xạ Heal-Punish kinh điển Souls-like |
  | **Bắt trúng đòn khi Player vừa hết Roll I-frame** | +1.5 | Huấn luyện kỹ năng Roll-Catching |
  | **Hạ gục Player (Player HP = 0)** | +5.0 ➔ EndEpisode() | Mục tiêu tối thượng của trận đấu |
  | **Duy trì khoảng cách combat lý tưởng (3m - 6m)** | +0.001 per step | Khuyến khích vờn quanh, không chạy ra góc lag |
  | **Tung chiêu trượt vào không khí** | -0.3 | Tránh spam chiêu vô tội vạ |
  | **Tung chiêu khi Player đang trong I-frame né đòn** | -0.5 | Dạy AI không đánh vào lúc đối thủ bất tử |
  | **Bị Player Parry thành công (Bị choáng)** | -1.5 | Dạy AI biết sợ thế thủ và không ra đòn dễ đoán |
  | **Bị Player chém trúng** | -0.4 * (damage / 20f) | Dạy AI biết né tránh, dạt sườn giữ khoảng cách |
  | **Boss tử trận (Boss HP = 0)** | -5.0 ➔ EndEpisode() | Trừng phạt thất bại |
  | **Phạt thời gian tồn tại (Existence Penalty)** | -0.0005 per step | Ép Boss chủ động kết liễu trận, chống cù nhây |

* **Quy Trình Đào Tạo Đa Sàn Đấu (Multi-Arena Parallel Training):**
  * Trong Scene huấn luyện, nhân bản **8 hoặc 16 sàn đấu Arena** chạy song song.
  * Huấn luyện thông qua `mlagents-learn` với thuật toán PPO (`IceGolem_PPO.yaml` và `RedIgnis_PPO.yaml`) trên môi trường Python ảo (venv), xuất ra file **.onnx** (`IceGolemSoulsAI-12955.onnx`, `RedIgnis_Run03/RedIgnisSoulsAI-33411.onnx`).
  * Nạp file .onnx vào component Behavior Parameters (chế độ Inference Only) để chạy mượt mà 100% thời gian thực trong game cuối cùng mà người chơi không cần cài Python hay PyTorch.

* **Kiến Trúc ML-Agents Riêng Cho Kiếm Sĩ Red Ignis (`RedIgnisAgent.cs`, `RedIgnisBattleArena.cs` & `RedIgnis_PPO.yaml`):**
  * **Vector Observations (27 floats):** Gồm tọa độ phẳng tới Player (2), cự ly (1), góc mặt cos/sin (2), máu Boss (1), `IsBusy` (1), cooldown 4 kỹ năng (4), cờ `IsPhase2` (1), máu & Stamina Player (2), 5 trạng thái Player (`IsAttacking`, `IsBlockingOrParrying`, `IsRolling`, `isPlayerInRollRecovery` 0.4s, `IsGrounded`), vận tốc tương đối Player (2), 4 tia raycast tường tầm `12m` (4) và tọa độ tới tâm Arena (2).
  * **Action Space (`[5, 5, 2]`):** Branch 0 di chuyển 5 lựa chọn (tích hợp `movePersistenceTime = 0.6s`, `minStrafeDistance = 2.2m`), Branch 1 chọn 4 kỹ năng chiến đấu (Skill 0..3), Branch 2 Timing Mixup (0 = đánh ngay, 1 = hoãn `0.35s` bắt bài roll).
  * **Chống Đi Lùi Câu Giờ & Đồng Bộ Tầm Chiêu:** Giữ cự ly áp sát `optimalDistanceMin = 1.0m` – `optimalDistanceMax = 2.0m`, khóa lùi từ ngưỡng `backwardLockDistance = 1.5m` ở cả `WriteDiscreteActionMask`, `OnActionReceived` và `Update`, phối hợp `backwardSpeed = 1.8m/s` trên Controller. Hàm `GetSkillEffectiveMaxRange(skillIdx)` đọc trực tiếp `AttackSlashMaxDistance`, `HorizontalSlashMaxDistance`, `JumpLeapDistance + JumpStopOffset` (cận dưới `3.5m`) và `3.8m` (Combo) để khóa và thưởng chiêu khớp tuyệt đối với thông số Inspector.
  * **Điều Phối Đấu Trường Hiệp Đấu Liên Hoàn (`RedIgnisBattleArena.cs`):** Gắn cờ `isManagedByArena = true` để Arena làm trọng tài duy nhất cho EndEpisode/Reward kết thúc hiệp, tránh reset kép với Agent; hồi đúng `BossMaxHealth` (`1200 HP`) thay vì cố định sai, hồi 100% HP/Stamina/Spirit Player và reset toàn bộ cooldown/animation Boss.
  * **Gizmos Debug AI (`OnDrawGizmosSelected`):** Vẽ vòng `optimalDistanceMin` (`1m` đỏ), `backwardLockDistance` (`1.5m` vàng), `optimalDistanceMax` (`2m` xanh lá), `arenaRadius` (`25m` xanh dương) và 4 tia raycast cảm biến tường (`12m`) trong Scene View.

* **Bot Đối Luyện Tự Động Cho Player (PlayerBotAI.cs - Sparring Partner):**
  * **Vai trò:** Cung cấp đối thủ giả lập có trí khôn và phản xạ chuẩn Souls-like để huấn luyện mạng nơ-ron Boss AI tự động mà không cần người chơi phải ngồi bấm máy thủ công hàng giờ.
  * **Zero Code Modification (Bảo toàn 100% mã nguồn Player):** Khai thác Reflection an toàn để kích hoạt toàn bộ kho chiêu thức nội bộ (ExecuteNormalAttack, AdvanceCombo, ExecuteChargeAttack, ExecuteParry, ExecuteCounterAttack, ExecuteSkillE, ExecuteSkillQ).
  * **Đồng Bộ Hóa Vector Observation ML-Agents (Roll-Catching Bridge):** Đồng bộ cờ isRolling hai chiều sang PlayerController qua Reflection khi bắt đầu và kết thúc lăn né. Nhờ đó, IceGolemAgent đọc được chính xác Vector Observation 19 (IsRolling = 1) và mở cửa sổ isPlayerInRollRecovery (Observation 20 = 1, kéo dài 0.4s), kích hoạt chuẩn xác hàm thưởng rollCatchReward (+1.5) và phạt hitIFramePenalty (-0.5).
  * **Máy Trạng Thái Chiến Đấu Toàn Diện (9 Trạng Thái FSM):**
    * *Idle & Circling:* Vờn quanh Boss ở cự ly vàng (3.2m - 5.5m), đảo chiều dạt ngang nhịp nhàng.
    * *Approaching & Attacking:* Áp sát và thi triển combo 3 nhát chém hoàn chỉnh (gán comboStep = 1 trước khi tung đòn 1 để Animator nhận diện chính xác ComboStep == 1, không bị nuốt đòn).
    * *Parry & Counter:* Giơ kiếm đón đòn khi Boss ra chiêu cận chiến; ngay khi Parry thành công, lập tức kích hoạt ExecuteCounterAttack gây 120 Poise Damage và đẩy lùi Boss.
    * *Skill E & Skill Q:* Phóng kiếm khí tầm xa (50 Spirit) và đập nổ AOE giáng trần (100 Spirit) khi tích lũy đủ năng lượng từ combo (tiêu hao Spirit chuẩn mực).
    * *Cửa Sổ Sơ Hở Hồi Máu (Heal-Punish Window - 1.2s):* Lùi xa uống máu (DrinkPotionRoutine) kèm độ trễ 1.2s đứng yên không né đòn, tạo điều kiện cho Boss kích hoạt đòn áp sát và nhận thưởng healPunishReward (+2.0).
    * *Trừng Phạt Boss Suy Yếu (PunishingWeaken):* Nhận diện khi Boss bị vỡ thế (IsWeakened == true), bot lập tức lao vào xả toàn bộ sát thương.
    * *Tương Thích Stagger:* Tạm ngưng can thiệp vật lý khi Player bị Boss đánh choáng (PlayerFeedback.IsStaggered), bảo toàn hiệu ứng đẩy lùi (Knockback).
  * **Hỗ Trợ Đa Boss:** Tự động phát hiện và thích ứng linh hoạt với cả Boss Băng (IceGolemController.cs) và Boss Lửa (RedIgnisController.cs).
  * **Phím Tắt Tiện Ích:** Phím [B] bật/tắt Bot tức thì; phím [T] điều chỉnh tốc độ game (1x -> 2x -> 5x) tăng tốc độ huấn luyện.

---

### D. Phân Hệ Tiến Trình RPG (RPG Progression & Economy)
1. **Hệ Thống Rơi Đồ Thế Giới (Loot Drop & World Drops):**
   * **Nguồn rơi đồ:** Quái vật chết (Health.onDeath), Thùng gỗ/hòm đập vỡ (BreakableProp.cs nhận đòn qua IDamageable), Rương kho báu mở (ChestInteractable.cs).
   * **Hệ Thống 3 Cấp Độ Rương & VFX Tia Sáng Mở Nắp (Chest Tiers & Opening Light Rays):**
     * **Rương Thường (`Interactive_Chest.prefab`):** Model rương gỗ cổ điển, phát chùm tia sáng xanh lá (`#22C55E`).
     * **Rương Hiếm (`Interactive_Chest_Rare.prefab`):** Model viền vàng nạm ngọc, phát chùm tia sáng tím thần bí (`#A855F7`).
     * **Rương Boss (`Interactive_Chest_Boss.prefab`):** Model rương hoàng kim đại thánh đường (scale 1.35x), phát chùm tia sáng vàng kim rực rỡ (`#F59E0B`).
     * **Cơ chế tia sáng đa tầng (`OpenVFX`):** Gồm `Point Light` (Flash Flare bùng nổ khi hé nắp qua `FlashLightRoutine`), `Rays` (luồng tia sáng nón vươn cao), `Stars` (hạt sao lấp lánh) và `Circles` (vòng hào quang). Tự động kích hoạt đồng bộ khi nắp rương hé mở (`vfxDelay = 0.05s`) qua `ChestInteractable.cs`.
   * **Hiệu ứng vật lý văng đồ (Arc Pop Physics):** Vật phẩm búng nảy theo hình vòng cung parabol nhẹ khi xuất hiện (`LootDropper.cs`), tránh đè dính chùm.
   * **Vật phẩm thế giới (`WorldItemPickup.cs`):** Model 3D thu nhỏ, xoay lơ lửng, tia sáng hào quang nhẹ (Loot Beam/Glow) tương ứng theo độ hiếm.
    * **Hệ Thống Danh Sách Tương Tác Dọc & Cuộn Chuột Tập Trung (PlayerInteraction.cs, InteractionListUI.cs, InteractionTarget.cs):**
      * Hợp nhất toàn bộ đối tượng tương tác IInteractable (Cổng dịch chuyển, Rương báu, Cửa, NPC) và vật phẩm rơi WorldItemPickup thành 1 danh sách dọc duy nhất bên phải tâm màn hình (x: 247, y: 0), triệt tiêu 100% lỗi chồng đè 2 UI.
      * **Điều hướng cuộn chuột (Mouse Scroll Wheel) & Phím Mũi Tên:** Người chơi lăn con lăn chuột để chọn đối tượng cần tương tác/nhặt; danh sách hỗ trợ cửa sổ trượt (Sliding Window) hiển thị tối đa 5 dòng mượt mà.
      * **Phản hồi Corner Kép (Dual Corner Active State):** Mục đang được chọn (Selected) tự động bật sáng 2 Corner (Góc viền nền Background & Góc viền KeyIcon phím F) và hiển thị sáng rõ (lpha = 1.0f); các mục còn lại tắt 2 Corner và làm mờ nhẹ (lpha = 0.6f).
      * **Nhặt đồ có chủ đích (Single Target Action):** Gỡ bỏ hoàn toàn việc bắt phím F riêng lẻ trong từng instance rơi đất, loại bỏ triệt để lỗi "bấm 1 lần nhặt hết toàn bộ đồ trong tầm". Bấm phím F chỉ nhặt hoặc tương tác đúng mục đang chọn; nhặt xong danh sách tự co lại và chọn tiếp mục kế tiếp.
      * **Tiền Vàng:** Duy trì cơ chế tự động hút nam châm (Magnetic Auto-pickup 3.8m), không làm rác danh sách phím F.
   * **Thông báo nhặt đồ (`PickupNotificationFeed.cs`):** Xếp chồng mép trái màn hình, trượt mượt lên từ dưới (smooth slide up), mờ dần sau 2.5s.
2. **Hệ Thống Túi Đồ 2 Cửa Sổ (Dual-Pane Inventory Screen - Phím Tab):**
   * **Kiến trúc điều phối chính (InventoryUIController.cs):** Quản lý đóng/mở mượt mà bằng CanvasGroup (phím Tab hoặc Esc), tự động giải phóng con trỏ chuột (Cursor.lockState = None), khóa di chuyển người chơi và đóng băng xoay camera / combat. Tự động kiểm tra và khởi tạo EventSystem + InputSystemUIInputModule tại runtime nếu scene bị thiếu.
   * **Cửa sổ Trái (Character & Stats):**
     * **3D Character Preview (`CharacterPreviewController.cs` & `PlayerWeaponVisualSync.cs`):** Camera phụ chuyên dụng soi nhân vật qua `RenderTexture` (512x512). Thiết lập `cullingMask = 1 << 3` (chỉ soi Layer 3: Player) giúp loại bỏ 100% tường, sàn, quái hầm ngục khỏi khung xem trước. Tự động đồng bộ đệ quy Layer 3 cho mọi vũ khí kiếm khi đổi trang bị (`SetLayerRecursively` & `EnsurePlayerAndWeaponLayers`). Tích hợp **Smart Adaptive Studio Lighting** tự động phân biệt cường độ sáng giữa hầm ngục (`dungeonLightIntensity = 1.3f`) và làng ngoài trời (`landLightIntensity = 0.25f`), thiết lập góc chiếu Studio Key Light 3/4 nghệ thuật và sử dụng hook URP `RenderPipelineManager.beginCameraRendering` để cô lập hoàn toàn đèn xem trước (không bao giờ rọi vào Player ở ngoài thế giới). Hỗ trợ sự kiện chuột `IDragHandler` cho phép nhấp giữ chuột trái và kéo ngang trên khung để xoay nhân vật 360 độ. Tự động tắt Camera khi đóng túi hoặc chuyển sang chế độ 2D để tiết kiệm 100% tài nguyên GPU trong lúc chơi game.
     * **Ô trang bị (`EquipmentSlotUI.cs`):** Quản lý 3 ô chuyên dụng: Ô Vũ khí (`WeaponSlot`), Ô Giáp (`ArmorSlot`) và Ô Tiêu Hao / Bình Máu (`UsableSlot`). Tự động ẩn placeholder khi đã mặc đồ, đổi icon và tô màu nền trắng `RarityBG` theo phẩm cấp món đồ. Hỗ trợ hiển thị số lượng dồn stack và vòng xoay hồi chiêu `CDFill` (Radial 360) cho ô `UsableSlot`.
     * **Ô Phím Tắt Tiêu Hao Ngoài Màn Hình HUD (`UsableSlotUI.cs`):** Cố định góc dưới-trái màn hình HUD, hiển thị icon bình thuốc đã trang bị, số lượng stack, phím tắt [C] và vòng xoay tiến trình hồi chiêu `CDFill`. Hỗ trợ nhấn phím [C] hoặc click chuột trực tiếp để hồi máu ngay lập tức kèm hoạt ảnh nhún nảy (Punchy Pop 1.15x).
     * **Thời Gian Hồi Chiêu Dùng Chung 2.0s (Global Shared Consumable Cooldown):** Thuốc dùng từ phím C, click HUD hoặc bấm `[Use]` trong Popup đều kích hoạt thời gian hồi chiêu 2.0s chung, đồng bộ vòng xoay `CDFill` trên HUD, LeftPanel và toàn bộ các ô chứa bình thuốc trong lưới 30 ô, đồng thời tạm khóa nút `[Use]` để chống spam bất tử.
     * **Bảng chỉ số thời gian thực (`PlayerStatsPanelUI.cs`):** Tự động liên kết và đọc chỉ số từ `PlayerCombat.cs`, `Health.cs`, `PlayerStats.cs` và `InventoryManager.cs`. Hiển thị: Sát thương tấn công (ATK + Bonus), Phòng thủ (DEF), Máu tối đa (Max HP + Bonus), Thể lực tối đa (Stamina + Bonus). Cập nhật tức thì ngay khi đổi trang bị.
     * **Nút Cộng Điểm Tiềm Năng (`BtnAttributePoints`):** Mở Modal phân bổ thuộc tính (sẵn sàng kết nối Task RPG-05).
   * **Cửa sổ Phải (Kho Đồ & Thao Tác):**
     * **Thanh Tab phân loại 4 danh mục (TabBar):** BtnAll (Tất cả), BtnWeapon (Vũ khí), BtnArmor (Giáp), BtnItem (Vật phẩm tiêu hao & nguyên liệu).
       * *Phồng kích thước nhận diện (Selected Scale):* Nút tab đang được chọn luôn duy trì kích thước phồng lớn hơn nhẹ (	abSelectedScale = 1.08f) và được đưa lên lớp hiển thị trên cùng (SetAsLastSibling()) giúp người chơi nhận biết danh mục trực quan tức thì.
       * *Hoạt ảnh Pop/Punch chuyển Tab:* Khi bấm chuyển đổi qua lại, nút mới nảy phồng mạnh lên mức (	abPopPunchScale = 1.15f) rồi co về 1.08f bằng UITransitionHelper.EaseOutQuad qua Time.unscaledDeltaTime (đạt 60 FPS mượt mà kể cả khi pause game), nút cũ co mượt mà về 	abNormalScale = 1.0f.
       * Tự động lọc lưới và điều khiển viền góc SelectHighlight tương ứng, giữ nguyên độc lập bảng màu Color Tint trên Button của Unity.
     * **Lưới 30 ô túi đồ (`InventorySlotUI.cs`):** GridLayoutGroup 5x6. Hiển thị Icon (tự động ẩn an toàn nếu chưa có sprite), nền trắng `RarityBG` đổi màu theo phẩm cấp Rarity, số lượng dồn stack (`xCount`), viền trang trí góc `SelectHighlight` khi được chọn.
     * **Hiển thị Tiền Vàng:** Cập nhật số dư vàng thời gian thực qua event `OnGoldChanged`.
     * **Bảng Chi Tiết Vật Phẩm Động (`ItemDetailPopupUI.cs`):**
       * Tự động tính toán toạ độ và bám theo ô đồ vừa click (`PositionNearSlot`): Mặc định đặt góc trên-trái của Popup nằm đúng tâm ô đồ; nếu ô ở hàng dưới cùng thì tự động đảo pivot thành góc dưới-trái để trồi lên trên; nếu ô ở sát mép phải thì tự động mở sang trái, đảm bảo 100% không tràn khỏi màn hình. Click lại chính ô đó sẽ tự động đóng Popup.
       * Tách riêng Text độ hiếm `ItemRarity` (tô màu theo phẩm cấp) và phân loại `ItemType`.
       * Cụm 4 nút hành động: `BtnEquip` và `BtnUnequip` xếp đè cùng 1 vị trí (Trang bị hiện khi xem đồ trong túi, Tháo ra hiện khi xem đồ đang mặc); `BtnUse` cho vật phẩm tiêu hao (bình máu); `BtnDrop` vứt đồ rơi ra sàn thế giới.
3. **Kinh Nghiệm, Cấp Độ & Điểm Tiềm Năng (`PlayerProgression.cs`, `AttributeAllocationUI.cs`):**
   * **EXP & Cấp độ:** Tiêu diệt quái vật (`EnemyAIBase.cs`) nhận EXP. Cập nhật thanh EXP dạng vòng tròn chữ C (`ExpRingFill`) trên `PlayerHUD.cs` với ánh xạ fill từ `0.19` đến `0.58`. Hiển thị Cấp độ trên huy hiệu `LevelText`.
   * **Điểm tiềm năng (Attribute Points):** Lên cấp thưởng điểm (mặc định 3 điểm / cấp). Mở modal phân bổ từ nút `BtnAttributePoints` ở cửa sổ trái Inventory (hiển thị badge `+X` điểm khả dụng).
   * **Phân bổ 3 chỉ số:** Sức Tấn Công (ATK), Máu Tối Đa (Max HP), Thể Lực Tối Đa (Max Stamina) với chế độ xem trước thay đổi chỉ số thời gian thực (`Current -> Preview (+Bonus)`).
   * **Đồng bộ Combat & Sinh tồn:** Bấm "Xác Nhận" áp dụng trực tiếp vào `Health.cs` (`BaseMaxHealth` + `SetBonusMaxHealth`), `PlayerStats.cs` (`BaseMaxStamina` + `SetBonusMaxStamina`) và `PlayerCombat.cs` (`TotalAttackBonus` cho toàn bộ các đòn đánh).
   * **Tẩy Điểm Tiềm Năng (Reset Stats):** Nút Reset tiêu tốn X Vàng (tùy biến Inspector, mặc định 100 Vàng) qua `InventoryManager.cs`. Hoàn lại 100% điểm tiềm năng và xóa bỏ toàn bộ bonus đã cộng. Kiểm tra chặt chẽ số dư Vàng và khóa nút nếu không đủ.
4. **Hub Ngôi Làng & Thợ Rèn (`LandScene`):**
   * **Chuông Làng Trung Tâm (Village Bell):** Điểm linh hồn hồi phục 100% máu, điểm Respawn sau khi tử trận.
   * **Thợ rèn Haven & Hệ Thống Cường Hóa / Chế Tạo Thần Binh (`BlacksmithUIController.cs`, `CraftingRecipe.cs`, `BlacksmithUpgradeConfig.cs` - Task RPG-08):**
     * **Kiến trúc Cường hóa Lai (Hybrid Scaling: Flat + %) & Chỉ Số Động:**
        + Khắc phục triệt để lỗi "vũ khí yếu cấp thấp nâng cấp không bõ": Áp dụng công thức lai giữa phần trăm và giá trị cố định:
          - ATK = baseAttack * (1 + 0.08 * level) + (4 * level)
          - DEF = baseDefense * (1 + 0.08 * level) + (2 * level)
          - HP = baseHealth * (1 + 0.08 * level) + (12 * level)
          - Dòng phụ: Tăng nhẹ +3% / cấp.
        + **7 Chỉ số mở rộng:** CritRate, CritDamage, Lifesteal (cap 25 HP), SpiritCostReduction (giảm tiêu hao Spirit chiêu E/Q/R), StaminaCostReduction (giảm hao thể lực Dash/Roll/Attack), SpiritGainBonus (tăng Spirit hồi khi đánh trúng hoặc Parry), MoveSpeedBonus.
        + **Dữ Liệu Động Instance Data & Procedural Roll (EquipmentAffixData.cs, EquipmentAffixGenerator.cs):** Mỗi trang bị rơi ra đất, mua shop hoặc rèn đều roll dòng chính (+-8%~25%), dòng phụ (0-4 dòng theo Rarity) và các ô phù phép độc lập.
        + **Hệ thống Phù Phép Thức Tỉnh Tại Mốc +1, +3, +5 (Enchantment Awakening):**
          - Mở 1 ô (+1) cho Common/Uncommon, 2 ô (+1, +3) cho Rare, 3 ô (+1, +3, +5) cho Epic/Legendary.
          - 10 Phù phép chuyên sâu: BleedingCrit (gây chảy máu rút 30% dmg qua 3s khi bạo kích), SkillECostReduction (-30% Spirit chiêu E), SkillQCostReduction (-30% Spirit chiêu Q), SkillRCostReduction (-25% Spirit chiêu R), LowHealthBloodthirst (+8% hút máu khi dưới 50% HP), TitanCharge (+25% sát thương đòn gồng), ParryStaminaRestore (hồi 25 thể lực khi parry thành công), SprintWind (+20% tốc chạy sau khi lăn né), CritFrenzy (+15% tốc chạy khi chém crit), SoulSiphon (tiêu diệt mục tiêu hồi 15 Spirit & 15 HP).
     * **Badge Cấp Độ & Hào Quang Thần Binh:** Tự động hiển thị Badge `+1`..`+5` màu vàng kim `#FFD700` ở góc trên ô đồ (`InventorySlotUI.cs`). Khi vũ khí đạt cấp tối đa `+5`, kích hoạt hào quang phát sáng vàng kim quanh lưỡi kiếm trong game (`PlayerWeaponVisualSync.cs`).
     * **Công thức nâng cấp theo bậc:**
       + Cấp `+1`: 1 Đá Cường Hóa (`mat_upgrade_stone`) + 50 Vàng.
       + Cấp `+2`: 2 Đá Cường Hóa + 100 Vàng.
       + Cấp `+3`: 3 Đá Cường Hóa + 200 Vàng.
       + Cấp `+4`: 1 Tinh Thể Hoàn Mỹ (`mat_upgrade_crystal`) + 4 Đá Cường Hóa + 400 Vàng.
       + Cấp `+5 (MAX)`: 2 Tinh Thể Hoàn Mỹ + 6 Đá Cường Hóa + 700 Vàng.
     * **Hệ thống Chế tạo Thần Binh (Crafting Recipes):** 6 công thức rèn trang bị cực phẩm từ Lõi Boss & Nguyên liệu quái:
       + *Cốt Ma Kiếm:* 5 Mảnh Xương Quái + 1 Huy Hiệu Chiến Binh + 150 Vàng.
       + *Hồ Ly Katana:* 3 Mảnh Xương Quái + 2 Bụi Ma Pháp + 1 Giấy Da Cổ + 250 Vàng.
       + *Băng Nha Cực Kiếm:* 1 Trái Tim Băng Ice Core + 2 Mảnh Cổ Tự Băng + 4 Đá Cường Hóa + 500 Vàng.
       + *Hỏa Long Diệt Thế Kiếm:* 1 Lõi Lửa Flame Core + 2 Huy Hiệu Liệt Hỏa + 2 Tinh Thể Hoàn Mỹ + 800 Vàng.
       + *Giáp Xương Quái:* 6 Mảnh Xương Quái + 2 Huy Hiệu Chiến Binh + 200 Vàng.
       + *Băng Giáp Cực Bắc:* 1 Trái Tim Băng Ice Core + 2 Mảnh Cổ Tự Băng + 1 Tinh Thể Hoàn Mỹ + 600 Vàng.
     * **Giao Diện 2 Panel Đồng Bộ:** Panel Trái gồm 2 Tab chuyển đổi [ NÂNG CẤP | RÈN ] (cửa sổ so sánh Before/After, container nguyên liệu, hiển thị giá vàng thời gian thực). Panel Phải đồng bộ 30 ô túi đồ và Popup thao tác với nút `[Put in]` độc quyền.
     * **NPC Thợ Rèn & Công Cụ 1-Click:** `BlacksmithInteractable.cs` gắn trên `BlackSmithNPC` (bán kính 3.2m, phím F); Tool `BlacksmithSetupEditor.cs` với MenuItem `Infinite Labyrinth -> RPG -> 7. Thiết Lập Lò Rèn Haven (1-Click)` và tích hợp Master Dashboard.
   * **Cổng dịch chuyển (`PortalController.cs`):** Giữ phím F để bước vào Dungeon thông qua màn hình Loading Screen bất đồng bộ.
   * **Tối Ưu Hóa Hiệu Năng Ngôi Làng (NpcPatrol.cs & LandSceneOptimizerEditor.cs):**
     - **Distance LOD Culling & Hibernation cho 61+ NPCs:** Tự động đưa NPC ở xa (> 48m) vào chế độ ngủ đông (dừng NavMeshAgent, tắt né tránh, ngắt tính toán Animator qua CullCompletely), giải phóng 100% CPU. Khi người chơi lại gần (< 40m), NPC tự động thức tỉnh tuần tra mượt mà. Quét cự ly Time-sliced 0.35s rải đều các frame.
     - **Animator Culling (CullUpdateTransforms):** Dừng tính toán biến dạng xương khi NPC khuất tầm nhìn camera.
     - **Hạ cấp Tránh va chạm (LowQualityObstacleAvoidance):** Cắt giảm 70% chi phí CPU tính toán RVO cho dân làng đi tuần.
     - **Bộ nhớ đệm tĩnh Waypoints:** Dùng chung mảng sharedWaypoints loại bỏ 61 lần quét GameObject.Find trùng lặp.
     - **Gộp Mesh Sỏi Đá (Gravel Combine):** Gộp 94 viên sỏi thành 1 Draw Call duy nhất bằng CombineMeshes 32-bit.
     - **Static Batching & Lọc Bóng Đổ:** Đánh dấu Static cho 214 đối tượng kiến trúc; tắt đổ bóng cho tiểu cảnh nhỏ (sỏi, bụi hoa, hàng rào, chum vại) và chỉ giữ bóng đổ cho công trình lớn.
     - **Terrain GPU Instancing:** Bật drawInstanced = true, khống chế 	reeDistance = 200m đồng bộ camera far clip 150m.

---

### E. Phân Hệ Màn Hình Tải Bất Đồng Bộ & Sinh Hầm Ngục Theo Chặng (Asynchronous Loading Screen & Time-Sliced PCG)
* **Bộ Điều Phối Toàn Cục (LoadingScreenManager.cs - Singleton & DontDestroyOnLoad):**
  * Tự động khởi tạo và nạp tài nguyên từ Resources/ (EnsureInstance()) mà không phụ thuộc vào thiết lập thủ công trong Scene.
  * **Pha 1 (0% -> 40%):** Tải Scene từ đĩa vào RAM bất đồng bộ qua SceneManager.LoadSceneAsync(allowSceneActivation = false) cho đến khi đạt 90% dữ liệu gốc.
  * **Pha 2 (40% -> 100%):** Tự động phát hiện DungeonManager và liên kết với Coroutine GenerateWorldRoutine sinh PCG theo từng chặng.
  * **Bảo vệ Trải nghiệm & Chống Chớp Nháy (minLoadingTime = 1.6s):** Đảm bảo giữ màn hình tối thiểu 1.6 giây để người chơi kịp đọc trọn vẹn mẹo chơi, đồng thời chờ thanh tiến trình dâng đủ 100% trước khi Fade Out mượt mà đưa nhân vật vào thế giới.
* **Giao Diện Màn Hình Tải (LoadingScreenUI.cs, LoadingScreenData.cs, LoadingScreenCanvas.prefab):**
  * **Khung Ảnh Nền Ngẫu Nhiên (RawImage Full-screen):** Nhận ngẫu nhiên 1 trong 13 ảnh render 3D hầm ngục mẫu kết hợp lớp phủ Vignette tối tương phản cao.
  * **Tiêu Đề Địa Danh (Header):** Tiêu đề chính vàng kim (LocationTitle) và phụ đề bạc (LocationSubtitle) đổi linh hoạt theo ngữ cảnh (ví dụ: *"INFINITE LABYRINTH - FLOOR 1"* hoặc *"HAVEN VILLAGE"*).
  * **Mẹo Sinh Tồn Ngẫu Nhiên (Gameplay Tips):** Hệ thống 21 mẹo tiếng Anh được phân loại theo 5 nhóm: COMBAT, EXPLORATION, BOSS TACTICS, VILLAGE HUB, ROGUELITE.
  * **Thanh Tiến Trình Souls-like:** Sử dụng cặp sprite Monster_Border.png + Monster_Fill.png, nạp màu hổ phách vàng rực, nội suy mượt mà qua Mathf.MoveTowards.
  * **Phần Trăm & Ấn Ký Spinner:** Hiển thị số % ở góc phải thanh loading và ấn ký vòng tròn xoay tròn liên tục theo Time.unscaledDeltaTime.
* **Sinh Hầm Ngục Theo Chặng (Time-Sliced PCG - DungeonManager.cs, DungeonRenderer.cs):**
  * Chia nhỏ việc dựng lưới 3D, gác lửng tầng 2, props và bake NavMesh thành các giai đoạn qua nhiều khung hình (yield return null), triệt tiêu 100% hiện tượng khựng đơ khung hình khi tạo lập hầm ngục quy mô lớn.

---

### F. Bảng Điều Khiển Quản Trị & Thử Nghiệm (Developer Admin Cheat Console)
* **Dịch Vụ Quản Trị Trung Tâm (AdminCheatManager.cs - Self-Healing Singleton & DontDestroyOnLoad):**
  * Tự động khởi tạo và phục hồi (EnsureInstance()) khi kéo Prefab AdminCheatCanvas.prefab vào bất kỳ Scene nào (LandScene, DungeonScene...) mà không cần kéo Manager thủ công.
  * Hỗ trợ 3 phím nóng mở Console: Phím **~** (Backquote), **F1** hoặc **F2** (chống xung đột bộ gõ tiếng Việt Unikey/EVKey), và phím **Esc** / nút **✕** để đóng nhanh.
  * Tích hợp cơ chế khóa con trỏ chuột (CursorLockMode.None / Locked) và tạm dừng hành động nhân vật khi mở bảng điều khiển. Chống bấm đúp frame (lastToggleFrame).
  * **Ma Trận Dịch Chuyển Linh Hoạt Thích Ứng Scene (Dynamic Safe Teleport Matrix):**
    * Trong LandScene: Hiển thị và dịch chuyển chuẩn xác tới 6 địa danh làng Haven: Chuông Làng (Village Bell), Cổng Hầm Ngục (Village Portal), Tiệm Binh Khí (Equipment Shop), Tiệm Dược Liệu (Supplies Shop), Lò Rèn Haven (Blacksmith), Cối Xay Gió (Windmill Lookout); Nút Fast Travel vào Dungeon Tầng 1.
    * Trong DungeonScene: Dịch chuyển đến Start Entrance, Cổng Đại Lộ Boss, Tâm Đấu Trường Boss (Arena Center), Key Shrine, Phòng Kho Báu (xoay vòng), Gác lửng Tầng 2 (cao độ 8m); Nút Về Làng Haven bảo toàn kho đồ.
  * **Bộ Công Cụ Thử Nghiệm Toàn Diện:**
    * Quản lý Vòng lặp Ngày & Bản đồ: AdvanceDay (Day +1), ResetDayTo1, Mở sáng 100% bản đồ hầm ngục (RevealAll), Mở toang Đại Môn Boss (ForceOpenGate), Tái sinh hầm ngục thời gian thực (RegenerateDungeon).
    * **Bia Tập Đánh Thử Sát Thương (Damage Dummy 10,000 HP):** Sinh quái đứng yên, tắt AI, khóa NavMesh, cài đặt 10.000 HP & 0 DEF để kiểm tra combo đòn đánh, sát thương vũ khí và số Floating Damage Numbers; Nút hồi máu dummy và xóa sạch dummy.
    * Đóng băng AI quái vật (FreezeEnemies) và sinh 4 quái Skeleton thường.
    * God Mode, Vô hạn Thể lực (Stamina), Vô hạn Spirit (4 sao), **No Cooldown Cheat (0s hồi chiêu)** cho kỹ năng E, Q, R và bình máu, Siêu tốc độ (1.0x - 3.5x).
    * Bơm Vàng, EXP, Điểm Tiềm Năng, Bình Máu, Vũ khí Huyền thoại.
* **Giao Diện Điều Khiển (AdminCheatUI.cs & AdminCheatCanvas.prefab):**
  * 5 Tab chức năng: Teleport (Scene-aware), World (Day & Cheats), Player (Stats & No CD), Monsters (10k Dummy & AI), Creative (Kho Sáng Tạo 51 Items).
  * **Cửa Sổ Sáng Tạo (Creative Mode - Toàn bộ 51 Items Game):** Thanh tìm kiếm thời gian thực (`TMP_InputField`), bộ nút lọc phân loại nhanh (`ALL`, `WEAPON`, `ARMOR`, `POTION`, `MATERIAL`, `KEY/GOLD`), danh sách ScrollView hiển thị toàn bộ 51 items (bao gồm cả `MonsterGem` và `BossGem`) kèm Icon, Tên theo màu Rarity, chỉ số, nút `[ +1 ]` và `[ +Max ]` nhận đồ thẳng vào túi.
  * Mini Debug Overlay góc trên-phải: Hiển thị FPS thời gian thực, tọa độ Player (X, Y, Z), phòng hiện tại / vị trí làng, và huy hiệu trạng thái Cheat ([GOD], [INF STAM], [INF SPIRIT], [NO CD], [FROZEN AI], [SPD]).
  * Hệ thống Toast Notification mượt mà phản hồi tức thì mỗi khi thực thi lệnh.

---

## 3. Quản Lý Trạng Thái Toàn Cục (Game Architecture Patterns)
- **Persist Data:** Sử dụng GameManager (DontDestroyOnLoad) hoặc ScriptableObjects để lưu trữ trạng thái giữa các lần chuyển Scene:
  * CurrentDay: Số ngày hiện tại (Day 1, Day 2...).
  * CurrentFloor: Tầng hầm ngục hiện tại (Floor 1, Floor 2...).
  * PlayerSaveData: Lượng Vàng, EXP, Level, Chỉ số nâng cấp, Kho đồ Inventory.
- **Scene Routing:**
  * LandScene -> DungeonScene (qua Portal).
  * DungeonScene (thắng Boss) -> UI popup chọn: Về Làng (LandScene) hoặc Đi tiếp (DungeonScene tầng kế).
  * DungeonScene (thua cuộc) -> Tự động chuyển về LandScene tại vị trí Chuông Làng.

