#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfiniteLabyrinth.Editor
{
    /// <summary>
    /// Master Control Center (Dashboard) - Bảng điều khiển dự án Infinite Labyrinth.
    /// Thiết kế chuẩn Non-Destructive: Điều hướng Scene nhanh, truy cập công cụ trực quan an toàn,
    /// bảo vệ 100% dữ liệu Inspector do người dùng cân chỉnh mà không có bất kỳ nút 1-Click nào gây reset dữ liệu.
    /// </summary>
    public class ProjectDashboardEditor : EditorWindow
    {
        private const string SCENE_LAND = "Assets/Scenes/LandScene.unity";
        private const string SCENE_DUNGEON = "Assets/Scenes/DungeonScene.unity";

        private int selectedTab = 0;
        private Vector2 scrollPos;

        private readonly string[] tabNames = new string[]
        {
            "🌿 Scene & Điều Hướng",
            "⚔️ Combat & VFX",
            "👹 Boss Encounters",
            "🎒 RPG & Village",
            "⚡ Optimization & Build"
        };

        [MenuItem("Infinite Labyrinth/🚀 Master Control Center (Dashboard) %&d", false, 1)]
        [MenuItem("Window/Infinite Labyrinth Dashboard", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectDashboardEditor>("Master Dashboard");
            window.minSize = new Vector2(640, 680);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSceneQuickBar();

            EditorGUILayout.Space(6);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(34));
            EditorGUILayout.Space(8);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0:
                    DrawSceneNavigationTab();
                    break;
                case 1:
                    DrawCombatTab();
                    break;
                case 2:
                    DrawBossTab();
                    break;
                case 3:
                    DrawRpgTab();
                    break;
                case 4:
                    DrawOptimizationAndBuildTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        #region Headers and Navigation Bar

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.75f, 0.25f) }
            };

            GUIStyle subTitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            EditorGUILayout.LabelField("👑 INFINITE LABYRINTH - MASTER DASHBOARD", titleStyle);
            EditorGUILayout.LabelField("Bảng Điều Khiển An Toàn & Điều Hướng Dự Án Roguelite ARPG", subTitleStyle);
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            // Safety Warning Banner
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = new Color(0.4f, 1f, 0.5f);
            EditorGUILayout.LabelField("🛡️ CHẾ ĐỘ AN TOÀN (NON-DESTRUCTIVE INSPECTOR PROTECTION)", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.LabelField(
                "Toàn bộ công cụ 1-Click Setup đã được lưu trữ (Archived). Mọi thông số (Tọa độ UI, Damage, Cooldown, Offsets...)\n" +
                "do bạn cân chỉnh bằng tay trong Inspector được bảo vệ an toàn 100%, tuyệt đối không bị reset hay ghi đè.",
                EditorStyles.wordWrappedMiniLabel
            );
            EditorGUILayout.EndVertical();
        }

        private void DrawSceneQuickBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            Scene activeScene = SceneManager.GetActiveScene();
            string sceneName = string.IsNullOrEmpty(activeScene.name) ? "Chưa lưu" : activeScene.name;
            GUILayout.Label($"📍 Scene Hiện Tại: <b>{sceneName}</b>", new GUIStyle(EditorStyles.label) { richText = true });

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🌿 Mở LandScene", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                SwitchScene(SCENE_LAND);
            }

            if (GUILayout.Button("🏰 Mở DungeonScene", EditorStyles.toolbarButton, GUILayout.Width(125)))
            {
                SwitchScene(SCENE_DUNGEON);
            }

            if (GUILayout.Button(EditorApplication.isPlaying ? "⏹️ Dừng Game" : "▶️ Play Game", EditorStyles.toolbarButton, GUILayout.Width(95)))
            {
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "💡 <i>Mẹo: Mở nhanh Dashboard bất kỳ lúc nào bằng phím tắt <b>Ctrl + Alt + D</b> hoặc menu <b>Infinite Labyrinth -> Master Control Center</b>.</i>",
                new GUIStyle(EditorStyles.miniLabel) { richText = true, alignment = TextAnchor.MiddleCenter }
            );
            EditorGUILayout.EndVertical();
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(4);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.3f, 0.85f, 1f) }
            };
            EditorGUILayout.LabelField(title, headerStyle);
            EditorGUILayout.Space(2);
        }

        #endregion

        #region TAB 0: Scene & Điều Hướng

        private void DrawSceneNavigationTab()
        {
            DrawSectionHeader("🌿 QUẢN LÝ VÀ CHUYỂN ĐỔI SCENE NHANH");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏡 LandScene (Làng Haven - Safe Hub)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Nơi người chơi hồi sinh tại Chuông Làng, ghé Tiệm Trang Bị, Tiệm Dược Liệu, Lò Rèn và bước qua Cổng Hầm Ngục.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("➡️ Mở LandScene (Assets/Scenes/LandScene.unity)", GUILayout.Height(30)))
            {
                SwitchScene(SCENE_LAND);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏰 DungeonScene (Hầm Ngục PCG & Đấu Trường Boss)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Không gian hầm ngục sinh ngẫu nhiên theo thuật toán BSP, chứa 200+ quái Skeleton, rương báu, bẫy và đấu trường cặp Boss.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("➡️ Mở DungeonScene (Assets/Scenes/DungeonScene.unity)", GUILayout.Height(30)))
            {
                SwitchScene(SCENE_DUNGEON);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 Truy Cập Nhanh Prefabs Cốt Lõi", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player Prefab"))
            {
                PingAsset("Assets/Characters/Player/Player.prefab");
            }
            if (GUILayout.Button("Canvas UI Prefab"))
            {
                PingAsset("Assets/UI RPG/Canvas.prefab");
            }
            if (GUILayout.Button("CoreSystems Prefab"))
            {
                PingAsset("Assets/Prefabs/Core/CoreSystems.prefab");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 1: Combat & VFX

        private void DrawCombatTab()
        {
            DrawSectionHeader("⚔️ HỆ THỐNG CHIẾN ĐẤU & HIỆU ỨNG VFX (SOULS-LIKE)");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🗡️ Vệt Kiếm (Slash VFX), Va Chạm (Impact) & Gạt Đòn (Parry)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Trạng thái: ✅ Đã hoàn thiện & cấu hình chuẩn xác vào Player.prefab.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn PlayerSlashVFX Script / Prefab"))
            {
                PingAsset("Assets/Characters/Player/Player.prefab");
            }
            if (GUILayout.Button("Chọn Slash VFX Prefab"))
            {
                PingAsset("Assets/Characters/Player/Skill/Slash VFX.prefab");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🩸 Hiệu Ứng Hit VFX & Số Sát Thương (Damage Numbers)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Trạng thái: ✅ Hit VFX quái/player và bộ 7 preset màu Gradient DamageNumberItem đã nạp vào Resources.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn DamageNumberItem Prefab"))
            {
                PingAsset("Assets/Resources/DamageNumberItem.prefab");
            }
            if (GUILayout.Button("Chọn MobsGetHitVFX Prefab"))
            {
                PingAsset("Assets/Prefabs/VFX/MobsGetHitVFX.prefab");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 2: Boss Encounters

        private void DrawBossTab()
        {
            DrawSectionHeader("👹 BỘ ĐÔI TRÙM HẦM NGỤC (DUAL BOSS ENCOUNTERS)");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("❄️ Trùm Băng: Ice Golem (The Frost Juggernaut)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("HP: 1500 | Poise: 500 | 7 Kỹ năng Souls-like | Phase 2 Enrage 50% HP.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Chọn Prefab Boss_IceGolem"))
            {
                PingAsset("Assets/Prefabs/Boss/Boss_IceGolem.prefab");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔥 Trùm Lửa: Red Ignis (The Crimson Duelist)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("HP: 1200 | Poise: 400 | Kiếm Sĩ Duelist | Timing Mixup Wind-up | Phase 2 gồng kiếm.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn Prefab Boss_RedIgnis"))
            {
                PingAsset("Assets/Prefabs/Boss/Boss_RedIgnis.prefab");
            }
            if (GUILayout.Button("⚔️ Mở Cân Chỉnh Kiếm Ignis (Aligner)"))
            {
                IgnisWeaponAligner.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 3: RPG & Village

        private void DrawRpgTab()
        {
            DrawSectionHeader("🎒 KHO ĐỒ, CỬA HÀNG & LÒ RÈN LÀNG HAVEN");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛒 Hệ Thống Cửa Hàng (Shop NPCs)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tiệm Trang Bị & Tiệm Dược Liệu tại LandScene. Tỷ lệ bán lại 40% giá trị gốc.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn EquipmentShopData"))
            {
                PingAsset("Assets/ScriptableObjects/Shop/EquipmentShopData.asset");
            }
            if (GUILayout.Button("Chọn SuppliesShopData"))
            {
                PingAsset("Assets/ScriptableObjects/Shop/SuppliesShopData.asset");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔨 Lò Rèn Haven (Blacksmith NPC)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Nâng cấp trang bị (+1 đến +5) và chế tạo thần binh từ Lõi Boss.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Mở Thư Mục Công Thức Rèn (Crafting Recipes)"))
            {
                PingAsset("Assets/ScriptableObjects/Crafting");
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 4: Optimization & Build

        private void DrawOptimizationAndBuildTab()
        {
            DrawSectionHeader("⚡ TỐI ƯU HÓA HIỆU NĂNG & KIỂM TRA BIÊN DỊCH");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏡 Tối Ưu Hóa LandScene (Village Hub)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mesh Combining (94 viên sỏi -> 1 Mesh), Static flags, LOD và tối ưu 61 NPC.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("⚡ Mở Cửa Sổ Tối Ưu LandScene (Optimizer Window)", GUILayout.Height(30)))
            {
                LandSceneOptimizerEditor.ShowWindow();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛠️ Kiểm Tra Biên Dịch Player Build (Compile Test)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Kiểm tra nhanh xem toàn bộ mã nguồn C# có thể build thành công sang file .exe mà không bị lỗi thiếu Editor assembly.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("🛠️ Chạy Kiểm Tra Biên Dịch Build Player", GUILayout.Height(30)))
            {
                BuildTestHelper.TestCompilePlayerScripts();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        private void SwitchScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private void PingAsset(string path)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            else
            {
                Debug.LogWarning($"[Dashboard] Không tìm thấy tài nguyên tại: {path}");
            }
        }

        #endregion
    }
}
#endif
