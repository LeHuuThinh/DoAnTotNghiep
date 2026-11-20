#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
    /// <summary>
    /// B? công c? 1-Click t? d?ng hóa toàn b? Vòng L?p Trò Choi (Tasks GM-01, GM-02, GM-03):
    /// - GM-01: InfoUI (TopCornerInfo Day/Floor HUD & MidScreenInfo Area Intro Banner)
    /// - GM-02: DeadScreenUI (Màn hình YOU DIED & Vòng l?p H?i sinh t?i Chuông Làng)
    /// - GM-03: VictoryPortalUI (Giao di?n 2 c?a s? V? Làng / Ði Ti?p T?ng K?)
    /// </summary>
    public static class CoreLoopSetupEditor
    {
        // [MenuItem("Infinite Labyrinth/Core Loop & UI/? C?u Hình Toàn B? Vòng L?p (GM-01 -> GM-03) (1-Click)", false, 0)]
        public static void SetupAllCoreLoop()
        {
            Debug.Log("<color=cyan>=== B?T Ð?U C?U HÌNH TOÀN B? VÒNG L?P TRÒ CHOI (GM-01 -> GM-03) ===</color>");

            // 1. C?u hình GM-01: InfoUI
            InfoUIControllerEditor.SetupInfoUIInScene();

            // 2. C?u hình GM-02: DeadScreenUI
            DeadScreenUIControllerEditor.SetupDeadScreenUIInScene();

            // 3. C?u hình GM-03: VictoryPortalUI
            VictoryPortalUIEditor.SetupVictoryPortalUIMenuItem();

            Debug.Log("<color=green><b>[Core Loop Setup]</b></color> ?? ÐÃ HOÀN T?T C?U HÌNH 100% VÒNG L?P GAME (GM-01, GM-02, GM-03)!");
            EditorUtility.DisplayDialog("Thành Công R?c R?", 
                "Ðã c?u hình t? d?ng toàn di?n toàn b? Vòng L?p Trò Choi (Task GM-01 -> GM-03) thành công!\n\n" +
                "1. GM-01: TopCornerInfo (Day/Floor) & MidScreenInfo (Area Banner)\n" +
                "2. GM-02: DeadScreenUI & Chu?i H?i Sinh t?i Chuông Làng (Respawn Loop)\n" +
                "3. GM-03: VictoryPortalUI k?t n?i GameManager (V? Làng qua Day m?i / Xu?ng T?ng K?)\n\n" +
                "M?i tham chi?u dã du?c liên k?t và s?n sàng tr?i nghi?m!", 
                "B?t Ð?u Choi Ngay");
        }
    }
}
#endif

