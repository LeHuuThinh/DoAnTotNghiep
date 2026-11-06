#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
    /// <summary>
    /// Bộ công cụ 1-Click tự động hóa toàn bộ Vòng Lặp Trò Chơi (Tasks GM-01, GM-02, GM-03):
    /// - GM-01: InfoUI (TopCornerInfo Day/Floor HUD & MidScreenInfo Area Intro Banner)
    /// - GM-02: DeadScreenUI (Màn hình YOU DIED & Vòng lặp Hồi sinh tại Chuông Làng)
    /// - GM-03: VictoryPortalUI (Giao diện 2 cửa sổ Về Làng / Đi Tiếp Tầng Kế)
    /// </summary>
    public static class CoreLoopSetupEditor
    {
        [MenuItem("Infinite Labyrinth/Core Loop & UI/⚡ Cấu Hình Toàn Bộ Vòng Lặp (GM-01 -> GM-03) (1-Click)", false, 0)]
        [MenuItem("Tools/Infinite Labyrinth/Core Loop/⚡ Cấu Hình Toàn Bộ Vòng Lặp (GM-01 -> GM-03) (1-Click)", false, 0)]
        public static void SetupAllCoreLoop()
        {
            Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH TOÀN BỘ VÒNG LẶP TRÒ CHƠI (GM-01 -> GM-03) ===</color>");

            // 1. Cấu hình GM-01: InfoUI
            InfoUIControllerEditor.SetupInfoUIInScene();

            // 2. Cấu hình GM-02: DeadScreenUI
            DeadScreenUIControllerEditor.SetupDeadScreenUIInScene();

            // 3. Cấu hình GM-03: VictoryPortalUI
            VictoryPortalUIEditor.SetupVictoryPortalUIMenuItem();

            Debug.Log("<color=green><b>[Core Loop Setup]</b></color> 🎉 ĐÃ HOÀN TẤT CẤU HÌNH 100% VÒNG LẶP GAME (GM-01, GM-02, GM-03)!");
            EditorUtility.DisplayDialog("Thành Công Rực Rỡ", 
                "Đã cấu hình tự động toàn diện toàn bộ Vòng Lặp Trò Chơi (Task GM-01 -> GM-03) thành công!\n\n" +
                "1. GM-01: TopCornerInfo (Day/Floor) & MidScreenInfo (Area Banner)\n" +
                "2. GM-02: DeadScreenUI & Chuỗi Hồi Sinh tại Chuông Làng (Respawn Loop)\n" +
                "3. GM-03: VictoryPortalUI kết nối GameManager (Về Làng qua Day mới / Xuống Tầng Kế)\n\n" +
                "Mọi tham chiếu đã được liên kết và sẵn sàng trải nghiệm!", 
                "Bắt Đầu Chơi Ngay");
        }
    }
}
#endif
