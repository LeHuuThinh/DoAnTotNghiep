#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace InfiniteLabyrinth.Editor
{
    /// <summary>
    /// Công cụ tự động hóa cấu hình và lưu vĩnh viễn toàn bộ Prefabs Trang trí hầm ngục vào DungeonScene.unity.
    /// Khắc phục triệt để lỗi khi Build Standalone (.exe) hầm ngục bị trống trơn do AssetDatabase không hoạt động ở runtime.
    /// </summary>
    [InitializeOnLoad]
    public class DungeonPropSetupEditor : IPreprocessBuildWithReport
    {
        private const string DUNGEON_SCENE_PATH = "Assets/Scenes/DungeonScene.unity";

        public int callbackOrder => 0;

        static DungeonPropSetupEditor()
        {
            EditorApplication.delayCall += AutoCheckAndConfigureDungeonProps;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("<color=cyan>[BuildPreprocess]</color> Đang tự động kiểm tra và khóa cứng Prefabs DungeonPropDecorator vào DungeonScene trước khi Build...");
            ConfigureAndSaveDungeonProps(silent: false);
        }

        private static void AutoCheckAndConfigureDungeonProps()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == DUNGEON_SCENE_PATH)
            {
                var decorator = Object.FindAnyObjectByType<DungeonPropDecorator>();
                if (decorator != null && (decorator.wallTorchPrefab == null || decorator.breakableBarrelPrefabs.Count == 0 || decorator.tablePrefabs.Count == 0))
                {
                    ConfigureAndSaveDungeonProps(silent: true);
                }
            }
        }

        [MenuItem("Infinite Labyrinth/PCG/1. Cấu Hình & Lưu Prefabs Trang Trí Hầm Ngục (1-Click)", false, 1)]
        public static void MenuConfigureAndSaveDungeonProps()
        {
            ConfigureAndSaveDungeonProps(silent: false);
        }

        public static bool ConfigureAndSaveDungeonProps(bool silent = false)
        {
            Scene originalScene = SceneManager.GetActiveScene();
            string originalScenePath = originalScene.path;

            bool isDungeonOpen = (originalScenePath == DUNGEON_SCENE_PATH);
            Scene dungeonScene;

            if (isDungeonOpen)
            {
                dungeonScene = originalScene;
            }
            else
            {
                if (originalScene.isDirty)
                {
                    EditorSceneManager.SaveScene(originalScene);
                }
                dungeonScene = EditorSceneManager.OpenScene(DUNGEON_SCENE_PATH, OpenSceneMode.Single);
            }

            var decorator = Object.FindAnyObjectByType<DungeonPropDecorator>();
            if (decorator == null)
            {
                Debug.LogError("<color=red>[DungeonPropSetupEditor]</color> Không tìm thấy component DungeonPropDecorator trong DungeonScene!");
                if (!isDungeonOpen && !string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
                return false;
            }

            // Nạp toàn bộ Prefabs từ Project
            decorator.LoadAllPrefabsFromProject();

            // Đánh dấu bẩn component và scene để Unity ghi đè file YAML trên ổ cứng
            EditorUtility.SetDirty(decorator);
            EditorSceneManager.MarkSceneDirty(dungeonScene);
            bool saved = EditorSceneManager.SaveScene(dungeonScene);

            if (!silent)
            {
                if (saved)
                {
                    Debug.Log($"<color=green>[DungeonPropSetupEditor]</color> ✅ ĐÃ LƯU THÀNH CÔNG TOÀN BỘ PREFABS VÀO SCENE: {DUNGEON_SCENE_PATH}!\n" +
                              $"• Đuốc & Ánh sáng: {(decorator.wallTorchPrefab != null ? "OK" : "Thiếu")}\n" +
                              $"• Bàn ghế nội thất: {decorator.tablePrefabs.Count} bàn, {decorator.chairPrefabs.Count} ghế\n" +
                              $"• Rương & Thùng gỗ: {decorator.breakableBarrelPrefabs.Count} thùng, Regular: {(decorator.regularChestPrefab != null ? "OK" : "Null")}\n" +
                              $"• Mẫu kiến trúc: {decorator.wallTemplatePrefabs.Count} Wall, {decorator.propTemplatePrefabs.Count} Prop\n" +
                              $"• Boss Arena: Pedestal: {(decorator.bossPedestalPrefab != null ? "OK" : "Null")}, Gate: {(decorator.bossGatePrefab != null ? "OK" : "Null")}");
                }
                else
                {
                    Debug.LogError("<color=red>[DungeonPropSetupEditor]</color> ❌ Không thể lưu Scene DungeonScene.unity!");
                }
            }

            // Trả lại Scene ban đầu nếu người dùng đang đứng ở scene khác
            if (!isDungeonOpen && !string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            return saved;
        }
    }
}
#endif
