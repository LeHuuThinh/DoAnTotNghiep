# Quy Tắc Cốt Lõi Khi Lập Trình Unity (Infinite Labyrinth)

> [!IMPORTANT]
> Đây là các nguyên tắc phát triển bắt buộc dành cho AI Agent trong dự án **Infinite Labyrinth (Unity 3D Action RPG - Đồ Án Tốt Nghiệp)**. Mọi thay đổi mã nguồn phải tuân thủ nghiêm ngặt để đảm bảo an toàn cho Prefab, Scene, và kiến trúc hệ thống.

---

## 1. Bảo Vệ Tham Chiếu Inspector & Serialized Fields

- **TUYỆT ĐỐI KHÔNG** tự ý đổi tên hoặc xóa các trường `[SerializeField] private` hay `public` của MonoBehaviour.
  - Đổi tên biến sẽ làm mất liên kết (Missing / None) trong Prefab (`Player.prefab`, `Boss_IceGolem.prefab`, `Skeleton*.prefab`) và Scene (`LandScene.unity`, `DungeonScene.unity`).
  - Nếu bắt buộc phải đổi tên: Phải kèm attribute `[UnityEngine.Serialization.FormerlySerializedAs("tenBienCu")]`.

---

## 2. Quy Định Thao Tác Với Scene, Prefab & Unity Editor

- **CẤM TUYỆT ĐỐI sửa file nhị phân / YAML:** KHÔNG BAO GIỜ được dùng công cụ sửa file (`edit_file`, `write_file`) trên các file `.unity`, `.prefab`, `.asset`, `.mat`, `.controller` trong bất kỳ tình huống nào (kể cả khi người dùng yêu cầu gán component / reference).
- **CẤM TUYỆT ĐỐI tự ý viết script tự động hóa Editor (`[MenuItem]`, Python, v.v.):** 
  - KHÔNG TỰ TIỆN tạo các script Editor (thêm nút trên thanh Menu Bar/taskbar) hoặc script automation để tự động gán component / setup Scene.
  - Chỉ được viết script Editor khi người dùng YÊU CẦU ĐÍCH DANH bằng câu lệnh: *"Hãy viết Editor script để làm việc này"*.
- **BẮT BUỘC HƯỚNG DẪN THAO TÁC BẰNG TAY (UI Checklist):**
  - Mọi thao tác cấu hình Scene, Prefab, gán Component, kéo thả Reference hoặc tinh chỉnh Inspector, AI **PHẢI** xuất ra hướng dẫn từng bước rõ ràng để người dùng tự thao tác trong giao diện Unity Editor.
  - Định dạng hướng dẫn chuẩn:
    1. **Bước 1 (Vị trí):** Mở Scene / Prefab nào?
    2. **Bước 2 (Đối tượng):** Chọn GameObject nào trong cửa sổ Hierarchy?
    3. **Bước 3 (Thao tác):** Kéo thả component / asset nào vào trường (field) nào trong Inspector? Giá trị số/enum cần chỉnh là gì?
- **Đảm bảo giữ nguyên các file `.meta`:** Luôn giữ nguyên hoặc tạo đúng các file `.meta` tương ứng khi thêm mới/đổi tên file C#.

---

## 3. Tiêu Chuẩn Kiến Trúc C# Cho Infinite Labyrinth

- **Input System:** Dự án sử dụng **Unity New Input System** (`UnityEngine.InputSystem`). Sử dụng các API chuẩn như `Keyboard.current`, `Mouse.current`, hoặc `PlayerInput` Actions thay cho `Input.GetKeyDown` cũ.
- **Player & Combat:**
  - `PlayerController`: Phụ trách di chuyển (CharacterController), Jump/Fall, Roll, Air Control, vờn quái khi Target Lock.
  - `PlayerCombat`: Phụ trách combo kiếm, thời điểm Parry/Block, hitbox vũ khí, và kích hoạt kỹ năng.
  - `PlayerStats`: Quản lý tài nguyên **Stamina** (Thể lực) và **Spirit** (Năng lượng chiêu). Mọi thay đổi tài nguyên **PHẢI** phát sinh thông qua các phương thức chuẩn (`ConsumeStamina`, `GainSpirit`) và bắn `UnityEvent` (`onStaminaChanged`, `onSpiritChanged`).
  - `PlayerFeedback` & `EnemyFeedback`: Đã cài đặt cơ chế **Souls-like Poise** (thanh chịu đòn, khựng đòn khi hết Poise) và rung chấn/flash.
- **Monster & Boss AI:**
  - Quái thường (Skeleton) kế thừa từ `EnemyAIBase` và nhận lệnh tấn công thông qua `AttackTokenDirector` (Token pool tránh đánh hội đồng).
  - Boss (Ice Golem): Có kiến trúc sẵn sàng cho **ML-Agents** (Reinforcement Learning) kết hợp với FSM/Behavior dự phòng.
- **Dungeon Generation (PCG):**
  - Giữ vững cấu trúc phân tách nhiệm vụ: `BSPGenerator` (Toán học BSP) -> `DungeonRoomAnalyzer` (BFS tìm Boss room) -> `DungeonRenderer` (Dựng Mesh 3D) -> `DungeonPropDecorator` (Poisson Disk Sampler) -> `DungeonLightOptimizer` (Tối ưu URP Lights).

---

## 4. UI & Game State Pattern

- **UI Decoupling (Observer Pattern):** UI (`PlayerHUD`, v.v.) chỉ đăng ký nhận sự kiện (UnityEvent / C# Action) từ các Stats component. **TUYỆT ĐỐI KHÔNG** viết logic `Update()` liên tục query biến từ player trong các script UI.
- **Game Progression & Day System:** Dữ liệu xuyên suốt giữa `LandScene` và `DungeonScene` (Vàng, Trang bị, Ngày, Chỉ số) phải được quản lý tập trung qua GameManager/SaveData (DontDestroyOnLoad hoặc ScriptableObject), không phụ thuộc vào việc load scene.

---

## 5. Quy Trình Làm Việc Của Agent (Agent Best Practices)

1. **Luôn `read_file` trước khi sửa:** Kiểm tra mã nguồn thật trên ổ đĩa, không bao giờ đoán mò code từ lịch sử chat.
2. **Khu biệt phạm vi (Feature-Scoped):** Chỉ can thiệp đúng file của tính năng được giao trong `Docs/ACTIVE_TASKS.md`.
3. **Giữ nguyên Comment & Tooltip tiếng Việt:** Duy trì phong cách chú thích tiếng Việt chi tiết có sẵn trong codebase.
4. **Ưu tiên checklist UI hơn script tự động:** Với mọi cài đặt Scene / Prefab / Inspector, luôn ưu tiên cung cấp checklist các bước thao tác rõ ràng để người dùng thao tác an toàn trên giao diện Unity.
