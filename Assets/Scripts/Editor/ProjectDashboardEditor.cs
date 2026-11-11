#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using InfiniteLabyrinth.EditorTools;
using InfiniteLabyrinth.EditorScripts;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
    /// <summary>
    /// Master Control Center (Dashboard) - Trung tâm điều phối và cấu hình 1-Click tập trung cho toàn bộ dự án Infinite Labyrinth.
    /// Giúp thanh menu Unity gọn gàng, chuyên nghiệp, gom toàn bộ công cụ phát triển vào một giao diện trực quan duy nhất.
    /// </summary>
    public class ProjectDashboardEditor : EditorWindow
    {
        private const string SCENE_LAND = "Assets/Scenes/LandScene.unity";
        private const string SCENE_DUNGEON = "Assets/Scenes/DungeonScene.unity";

        private int selectedTab = 0;
        private Vector2 scrollPos;

        private readonly string[] tabNames = new string[]
        {
            "⚔️ Combat & VFX",
            "👹 Boss Encounters",
            "🎒 RPG & Village",
            "🔁 Game Loop & UI",
            "⚡ Optimization"
        };

        [MenuItem("Infinite Labyrinth/🚀 Master Control Center (Dashboard) %&d", false, -100)]
        [MenuItem("Window/Infinite Labyrinth Dashboard", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectDashboardEditor>("Master Dashboard");
            window.minSize = new Vector2(640, 720);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSceneQuickBar();

            EditorGUILayout.Space(6);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(36));
            EditorGUILayout.Space(8);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0:
                    DrawCombatAndVfxTab();
                    break;
                case 1:
                    DrawBossEncountersTab();
                    break;
                case 2:
                    DrawRpgAndVillageTab();
                    break;
                case 3:
                    DrawGameLoopAndUiTab();
                    break;
                case 4:
                    DrawOptimizationTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        #region Headers and Banners

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.75f, 0.25f) }
            };

            GUIStyle subTitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            EditorGUILayout.LabelField("👑 INFINITE LABYRINTH - MASTER CONTROL CENTER", titleStyle);
            EditorGUILayout.LabelField("Bảng Điều Khiển & Cấu Hình 1-Click Toàn Diện Dự Án Roguelite ARPG", subTitleStyle);
            EditorGUILayout.Space(4);
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

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💡 <i>Gợi ý: Dùng phím tắt <b>Ctrl + Alt + D</b> để mở nhanh Dashboard này bất kỳ lúc nào.</i>", 
                new GUIStyle(EditorStyles.miniLabel) { richText = true, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 0: Combat & VFX

        private void DrawCombatAndVfxTab()
        {
            DrawSectionHeader("⚔️ HỆ THỐNG CHIẾN ĐẤU SOULS-LIKE & HIỆU ỨNG VFX");

            // Card 1: Slash VFX & Hit Feedback
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🗡️ Vệt Kiếm (Slash Trail), Va Chạm (Impact) & Gạt Đòn (Parry Clashing)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Decoupled Arc Slash độc lập cho 6 đòn đánh cơ bản và 2 kỹ năng E/Q. Impact va chạm tại điểm tiếp xúc thực tế.", MessageType.None);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⚡ Cấu Hình Slash, Impact & Parry Cho Kiếm", GUILayout.Height(32)))
            {
                PlayerSlashVfxSetupEditor.SetupPlayerSlashVfx();
            }
            if (GUILayout.Button("🩸 Cấu Hình Hit VFX Cho Player & Quái (1-Click)", GUILayout.Height(32)))
            {
                CombatVfxSetupEditor.SetupAllCombatHitVfx();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 2: Damage Numbers & Gradients
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💥 Số Sát Thương Bay Lên (Floating Damage Numbers)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Quỹ đạo búng nảy parabol, hiệu ứng phóng to Pop-scale, và 7 dải màu TextMeshPro Gradient độc lập cho từng loại đòn.", MessageType.None);

            if (GUILayout.Button("🎯 Tạo / Cập Nhật Prefab Số Sát Thương & 7 Dải Gradient", GUILayout.Height(32)))
            {
                DamageNumberSetupEditor.SetupDamageNumbers();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 3: Monster Health Bars & Skill HUD
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("👾 Thanh Máu Quái Vật World Space & Giao Diện Kỹ Năng", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Thanh máu Billboard 3D Souls-like cho 4 quái Skeleton (Ghost bar trễ 0.45s). Giao diện 3 ô kỹ năng E/Q/R và 4 Sao Spirit.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("❤️ Cấu Hình Thanh Máu 4 Quái Skeleton", GUILayout.Height(32)))
            {
                EnemyHealthBarSetupEditor.SetupAllEnemyHealthBars();
            }
            if (GUILayout.Button("⭐ Cấu Hình Skill Icon UI & Sao Năng Lượng", GUILayout.Height(32)))
            {
                SkillIconUISetupEditor.SetupSkillIconUI();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 1: Boss Encounters

        private void DrawBossEncountersTab()
        {
            DrawSectionHeader("👹 TRÙM HẦM NGỤC & HỌC MÁY TĂNG CƯỜNG (ML-AGENTS)");

            // Group 1: Ice Golem
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("❄️ TRÙM BĂNG: ICE GOLEM (The Frost Juggernaut)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Máu 1500, Poise 500, Suy yếu Weaken duy nhất 1 lần, Phase 2 Cuồng Nộ tại <= 50% HP (chao đảo, shockwave, 3 cầu tím ma thuật).", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Cấu Hình Prefab Boss Ice Golem", GUILayout.Height(28)))
            {
                IceGolemSetupEditor.SetupIceGolemPrefab();
            }
            if (GUILayout.Button("2. Tự Động Nối Dây Animator", GUILayout.Height(28)))
            {
                IceGolemSetupEditor.AutoWireAnimatorMenu();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Cấu Hình ML-Agents Học Máy", GUILayout.Height(28)))
            {
                IceGolemSetupEditor.SetupMLAgentsForBossMenu();
            }
            if (GUILayout.Button("4. Dọn Dẹp Scene & Khắc Phục Lỗi Golem", GUILayout.Height(28)))
            {
                IceGolemSetupEditor.CleanActiveSceneMenu();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Group 2: Red Ignis
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔥 TRÙM LỬA: RED IGNIS (The Crimson Duelist / True Flame Ignis)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Kiếm sĩ tốc độ cao, 2 Pha Tái Sinh khi chết, vật lý áo choàng SpringBone, vũ khí căn chuẩn lòng bàn tay.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. 1-Click Setup Red Ignis (Controller, Animator, VFX)", GUILayout.Height(28)))
            {
                SetupRedIgnisAnimatorAndPrefabs.ExecuteFullSetup();
            }
            if (GUILayout.Button("2. Cân Chỉnh Kiếm Boss (Ignis Aligner)", GUILayout.Height(28)))
            {
                IgnisWeaponAligner.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Tự Động Sửa Áo Choàng (Auto Fix Cape)", GUILayout.Height(28)))
            {
                SpringManagerHelper.AutoFixRedIgnisCape();
            }
            if (GUILayout.Button("4. Rà Soát Phân Cấp & Colliders", GUILayout.Height(28)))
            {
                InspectAndFixRedIgnis.RunInspectionAndFix();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("5. Cấu Hình ML-Agents (1-Click)", GUILayout.Height(28)))
            {
                RedIgnisSetupEditor.SetupMLAgentsForBoss(true);
            }
            if (GUILayout.Button("6. Thiết Lập Đấu Trường Arena", GUILayout.Height(28)))
            {
                RedIgnisSetupEditor.SetupBattleArena(true);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Group 3: Boss Arena Director (Task D-02)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎬 ĐẠO DIỄN ĐẤU TRƯỜNG & CUTSCENE (Task D-02 Boss Arena)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Cutscene Intro bục đá, Biểu ngữ 'VICTORY ACHIEVED' Souls-like, và đạo diễn lia cam lên Bục Đá 12m sinh rương & cổng dịch chuyển.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Intro Cutscene & Boss HUD", GUILayout.Height(28)))
            {
                BossIntroSetupEditor.SetupBossIntroAndHUD();
            }
            if (GUILayout.Button("Victory Banner (VICTORY ACHIEVED)", GUILayout.Height(28)))
            {
                BossVictorySetupEditor.SetupBossVictory();
            }
            if (GUILayout.Button("Sinh Rương & Cổng Bục Đá 12m", GUILayout.Height(28)))
            {
                BossVictorySetupEditor.SetupPedestalRevealAndChestPortal();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 2: RPG & Village

        private void DrawRpgAndVillageTab()
        {
            DrawSectionHeader("🎒 KINH TẾ, TIẾN TRÌNH RPG & NGÔI LÀNG HAVEN");

            // Card 1: 49 Items Database
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 Cơ Sở Dữ Liệu 49 Vật Phẩm (Items Database)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("20 Vũ khí, 8 Giáp trụ, 13 Nguyên liệu/Lõi Boss, 6 Dược phẩm, 2 Tiền & Khóa. Đồng bộ mô hình kiếm 3D lên tay nhân vật.", MessageType.None);

            if (GUILayout.Button("💎 1. Khởi Tạo / Cập Nhật Toàn Bộ 49 Items ScriptableObjects", GUILayout.Height(30)))
            {
                ItemDatabaseGenerator.GenerateDefaultItems();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2. Đồng Bộ Kiếm Cầm Tay (VisualSync)", GUILayout.Height(28)))
            {
                ItemDatabaseGenerator.EnsurePlayerWeaponVisualSync();
            }
            if (GUILayout.Button("3. Tạo Prefab Rơi Đồ (WorldItemPickup)", GUILayout.Height(28)))
            {
                ItemDatabaseGenerator.EnsureWorldItemPickupPrefab();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("4. Cấu Hình Rơi Đồ Thùng Gỗ & Quái", GUILayout.Height(28)))
            {
                ItemDatabaseGenerator.SetupPropsAndMonstersLoot();
            }
            if (GUILayout.Button("5. Cập Nhật Ma Trận Rơi Đồ (Loot Tables)", GUILayout.Height(28)))
            {
                LootTableSetupEditor.ConfigureAllLootTablesManual();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 2: Haven Shops (Task RPG-07)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏪 Hệ Thống Cửa Hàng Làng Haven (Shop NPC - Task RPG-07)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tách 2 cửa hàng độc lập: Tiệm Trang Bị & Rèn Binh (EquipmentShop) và Tiệm Dược Liệu & Tạp Hóa (SuppliesShop). Hỗ trợ mua và bán thu hồi 40% giá trị.", MessageType.None);

            if (GUILayout.Button("🛒 1-Click Thiết Lập Toàn Bộ Hệ Thống Shop Làng Haven", GUILayout.Height(32)))
            {
                ShopSetupEditor.SetupShopSystem();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 3: Haven Blacksmith (Task RPG-08)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚒️ Lò Rèn Haven & Hệ Thống Cường Hóa / Chế Tạo (Task RPG-08)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Cường hóa trang bị (+1 đến +5, +15% ATK/DEF mỗi cấp) và Chế tạo Thần binh từ Lõi Boss & Nguyên liệu quái. Giao diện 2 Panel trực quan đồng bộ túi đồ.", MessageType.None);

            if (GUILayout.Button("⚒️ 1-Click Thiết Lập Toàn Bộ Hệ Thống Lò Rèn Haven", GUILayout.Height(32)))
            {
                BlacksmithSetupEditor.SetupBlacksmithSystem();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 3: Progression, Interaction & Key
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📈 Tiến Trình Cấp Độ, Tương Tác & Chìa Khóa Hầm Ngục", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Vòng EXP Ring HUD, Bảng cộng điểm tiềm năng, Danh sách tương tác dọc cuộn chuột, và Chìa khóa Boss.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("EXP Ring & Bảng Điểm Tiềm Năng", GUILayout.Height(28)))
            {
                ProgressionSetupEditor.SetupProgressionAndUI();
            }
            if (GUILayout.Button("Danh Sách Tương Tác Dọc (Phím F)", GUILayout.Height(28)))
            {
                InteractionListSetupEditor.SetupVerticalInteractionList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Tạo Chìa Khóa Hầm Ngục (Boss Key)", GUILayout.Height(28)))
            {
                DungeonKeySetupEditor.EnsureDungeonKeyAssets();
            }
            if (GUILayout.Button("Cấu Hình Usable Quick Slot Túi Đồ", GUILayout.Height(28)))
            {
                InventoryUIControllerEditor.SetupUsableSlotSystem();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 3: Game Loop & UI

        private void DrawGameLoopAndUiTab()
        {
            DrawSectionHeader("🔁 VÒNG LẶP TRÒ CHƠI & MÀN HÌNH ĐIỆN ẢNH (CORE LOOP)");

            // Master Core Loop Button
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔄 Master Vòng Lặp Cốt Lõi (Tasks GM-01 -> GM-03)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Quản lý Ngày (Day System), Tử trận YOU DIED hồi sinh tại Chuông Làng, và Cổng Dịch Chuyển 2 lựa chọn (Về Làng / Xuống Tầng Kế).", MessageType.None);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("🚀 1-CLICK: CẤU HÌNH TOÀN BỘ VÒNG LẶP (GM-01 -> GM-03)", GUILayout.Height(36)))
            {
                CoreLoopSetupEditor.SetupAllCoreLoop();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("GM-01: InfoUI & Banner Vùng Đất", GUILayout.Height(26)))
            {
                InfoUIControllerEditor.SetupInfoUIInScene();
            }
            if (GUILayout.Button("GM-02: DeadScreenUI (YOU DIED)", GUILayout.Height(26)))
            {
                DeadScreenUIControllerEditor.SetupDeadScreenUIInScene();
            }
            if (GUILayout.Button("GM-03: VictoryPortalUI (2 Cửa Sổ)", GUILayout.Height(26)))
            {
                VictoryPortalUIEditor.SetupVictoryPortalUIMenuItem();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 2: Loading Screen & Character 2D/3D Toggle
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⏳ Màn Hình Loading Chuyển Cảnh & Toggle Xem Trước Túi Đồ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Loading bất đồng bộ kèm 21 mẹo Souls-like và hoạt ảnh Meme 192 frames. Toggle [ 3D | 2D ] tiết kiệm 100% GPU camera buồng soi khi ở chế độ 2D.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cấu Hình Loading Screen Canvas (1-Click)", GUILayout.Height(30)))
            {
                LoadingScreenSetupEditor.CreateLoadingScreenPrefab();
            }
            if (GUILayout.Button("Cấu Hình Toggle [ 3D | 2D ] Preview", GUILayout.Height(30)))
            {
                CharacterPreviewToggleEditor.MenuSetupToggle();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TAB 4: Optimization & Admin

        private void DrawOptimizationTab()
        {
            DrawSectionHeader("⚡ TỐI ƯU HÓA HIỆU NĂNG & CÔNG CỤ QUẢN TRỊ (ADMIN)");

            // Card 1: LandScene Optimizer
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🚀 Bộ Tối Ưu Hóa LandScene (Nâng FPS từ ~65 lên 110+ FPS)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Gộp 94 viên sỏi (Gravel) thành 1 Draw Call, Static 214 đối tượng làng, bật GPU Instancing Terrain, tắt bóng đổ nước phẳng, dọn trùng Volume Bloom, và Distance LOD ngủ đông cho 61 NPC.", MessageType.None);

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1.0f);
            if (GUILayout.Button("🚀 MỞ CỬA SỔ TỐI ƯU HÓA LANDSCENE (OPTIMIZER WINDOW)", GUILayout.Height(36)))
            {
                LandSceneOptimizerEditor.ShowWindow();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            if (GUILayout.Button("⚡ Chạy Nhanh Tối Ưu Hóa Toàn Bộ LandScene (1-Click)", GUILayout.Height(28)))
            {
                LandSceneOptimizerEditor.RunQuickOptimization();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Card 2: Admin Cheat Console
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛠️ Admin Cheat Console (Phím Tắt ~ hoặc F1)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Hệ thống cửa sổ lệnh điều khiển thời gian thực: Teleport (Làng, Hầm ngục, Boss), Bất tử (God Mode), Thêm Vàng (+1000g), Đầy Máu/Stamina/Spirit, Reset Ngày/Tầng.", MessageType.None);

            if (GUILayout.Button("🔧 1-Click Cấu Hình Admin Cheat Console Trong Scene", GUILayout.Height(32)))
            {
                AdminCheatSetupEditor.SetupAdminCheatSystem();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        private void DrawSectionHeader(string title)
        {
            GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.4f, 0.9f, 0.6f) }
            };

            EditorGUILayout.LabelField(title, sectionStyle);
            EditorGUILayout.Space(2);
        }

        private void SwitchScene(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cảnh Báo", "Không thể đổi Scene khi đang chạy Play Mode. Hãy dừng game trước!", "Đã hiểu");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        #endregion
    }
}
#endif
