#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

namespace InfiniteLabyrinth.EditorTools
{
    /// <summary>
    /// Công cụ 1-Click Unity Editor cấu hình ML-Agents cho Boss Red Ignis (ML-03).
    /// Tự động gắn và cấu hình:
    /// - RedIgnisAgent (Học máy tăng cường Souls-like Duelist)
    /// - BehaviorParameters (Behavior: RedIgnisSoulsAI, Vector Obs: 27, Discrete Actions: [5, 5, 2])
    /// - DecisionRequester (Decision Period: 5, TakeActionsBetweenDecisions: true)
    /// - Thiết lập Đấu trường tập luyện RedIgnisBattleArena
    /// </summary>
    public static class RedIgnisSetupEditor
    {
        private const string PrefabPath = "Assets/Characters/Boss/IgnisDo/Boss_RedIgnis.prefab";
        private const string PlayerPrefabPath = "Assets/Characters/Player/Player.prefab";

        [MenuItem("Infinite Labyrinth/Boss/Red Ignis/1. Cấu Hình ML-Agents Cho Boss Red Ignis (1-Click)", false, 11)]
        public static void SetupMLAgentsForBossMenu()
        {
            SetupMLAgentsForBoss(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/Red Ignis/2. Thiết Lập Đấu Trường Tập Luyện (RedIgnisBattleArena)", false, 12)]
        public static void SetupBattleArenaMenu()
        {
            SetupBattleArena(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/Red Ignis/3. Gắn Bot AI Cho Player (Phím B Bật/Tắt Sparring)", false, 13)]
        public static void AttachPlayerBotAIMenu()
        {
            AttachPlayerBotAI(true);
        }

        /// <summary>
        /// Cấu hình ML-Agents trên Prefab Boss_RedIgnis và Scene Instance nếu có
        /// </summary>
        public static void SetupMLAgentsForBoss(bool showDialog)
        {
            if (!File.Exists(PrefabPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy Prefab tại: {PrefabPath}", "OK");
                return;
            }

            // 1. Cấu hình trên Prefab
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefab != null)
            {
                ConfigureBossMLAgentsComponents(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            // 2. Cấu hình trên Scene Instance nếu có trong Scene hiện tại
            GameObject sceneBoss = GameObject.Find("Boss_RedIgnis");
            if (sceneBoss != null)
            {
                ConfigureBossMLAgentsComponents(sceneBoss);
            }

            Debug.Log("<color=green>[Red Ignis ML Setup] Đã cấu hình thành công ML-Agents cho Boss Red Ignis!</color>");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Cấu Hình ML-Agents Hoàn Tất (Red Ignis)",
                    "Đã cấu hình thành công ML-Agents cho Boss Red Ignis:\n\n" +
                    "- Component RedIgnisAgent (Souls-like Duelist Reinforcement Learning)\n" +
                    "- Component BehaviorParameters (Behavior: RedIgnisSoulsAI, Vector Obs: 27, Discrete Actions: [5, 5, 2])\n" +
                    "- Component DecisionRequester (Decision Period: 5, TakeActions: true)\n\n" +
                    "Prefab đã được lưu tại: " + PrefabPath,
                    "Tuyệt vời!"
                );
            }
        }

        private static void ConfigureBossMLAgentsComponents(GameObject go)
        {
            // Dọn dẹp các BehaviorParameters trùng lặp cũ
            BehaviorParameters[] allParams = go.GetComponents<BehaviorParameters>();
            for (int i = 1; i < allParams.Length; i++)
            {
                if (allParams[i] != null) Undo.DestroyObjectImmediate(allParams[i]);
            }

            // Dọn dẹp các component Agent cũ nếu không phải RedIgnisAgent
            Agent[] oldAgents = go.GetComponents<Agent>();
            foreach (var a in oldAgents)
            {
                if (a != null && !(a is RedIgnisAgent))
                {
                    Undo.DestroyObjectImmediate(a);
                }
            }

            // 1. RedIgnisAgent
            var agent = go.GetComponent<RedIgnisAgent>();
            if (agent == null) agent = Undo.AddComponent<RedIgnisAgent>(go);

            // 2. BehaviorParameters
            var behaviorParams = go.GetComponent<BehaviorParameters>();
            if (behaviorParams == null) behaviorParams = Undo.AddComponent<BehaviorParameters>(go);

            behaviorParams.BehaviorName = "RedIgnisSoulsAI";
            behaviorParams.BrainParameters.VectorObservationSize = 27;
            behaviorParams.BrainParameters.NumStackedVectorObservations = 1;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(new int[] { 5, 5, 2 });

            // 3. DecisionRequester
            var requester = go.GetComponent<DecisionRequester>();
            if (requester == null) requester = Undo.AddComponent<DecisionRequester>(go);
            requester.DecisionPeriod = 5;
            requester.TakeActionsBetweenDecisions = true;

            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// Tạo hoặc cập nhật GameObject RedIgnis_BattleArena trong Scene hiện tại
        /// </summary>
        public static void SetupBattleArena(bool showDialog)
        {
            RedIgnisBattleArena arena = Object.FindFirstObjectByType<RedIgnisBattleArena>();
            if (arena == null)
            {
                GameObject arenaGo = new GameObject("RedIgnis_BattleArena");
                Undo.RegisterCreatedObjectUndo(arenaGo, "Create RedIgnis_BattleArena");
                arena = arenaGo.AddComponent<RedIgnisBattleArena>();
            }

            // Tự động căn chỉnh vị trí của BattleArena vào chính giữa sàn đấu (giữa Boss và Player)
            GameObject boss = GameObject.Find("Boss_RedIgnis");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (boss != null && player != null)
            {
                arena.transform.position = (boss.transform.position + player.transform.position) * 0.5f;
            }
            else if (boss != null)
            {
                arena.transform.position = boss.transform.position;
            }
            else if (player != null)
            {
                arena.transform.position = player.transform.position;
            }

            EditorUtility.SetDirty(arena.gameObject);
            Debug.Log("<color=green>[Red Ignis Arena] Đã thiết lập GameObject RedIgnis_BattleArena trong Scene thành công!</color>");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Thiết Lập Đấu Trường Thành Công",
                    "Đã thiết lập RedIgnisBattleArena trong Scene hiện tại!\n\n" +
                    "- Tự động quản lý hiệp đấu vô tận (Endless Rounds).\n" +
                    "- Khi Player <= 5HP -> Boss thắng hiệp (+5.0 thưởng ML).\n" +
                    "- Khi Boss <= 0HP -> Player thắng hiệp (-3.0 phạt ML).\n" +
                    "- Tự động hồi phục 100% máu và reset cự ly 12m cho hiệp mới.",
                    "OK"
                );
            }
        }

        /// <summary>
        /// Gắn PlayerBotAI lên Player để làm Sparring Partner tự động
        /// </summary>
        public static void AttachPlayerBotAI(bool showDialog)
        {
            int attachedCount = 0;

            if (File.Exists(PlayerPrefabPath))
            {
                GameObject pPrefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                if (pPrefab != null)
                {
                    var bot = pPrefab.GetComponent<PlayerBotAI>();
                    if (bot == null)
                    {
                        Undo.AddComponent<PlayerBotAI>(pPrefab);
                        attachedCount++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(pPrefab, PlayerPrefabPath);
                    PrefabUtility.UnloadPrefabContents(pPrefab);
                }
            }

            GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
            if (scenePlayer == null) scenePlayer = GameObject.Find("Player");

            if (scenePlayer != null)
            {
                var sBot = scenePlayer.GetComponent<PlayerBotAI>();
                if (sBot == null)
                {
                    sBot = Undo.AddComponent<PlayerBotAI>(scenePlayer);
                    EditorUtility.SetDirty(scenePlayer);
                    attachedCount++;
                }
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Cấu Hình Player Bot AI Thành Công",
                    "Đã gắn thành công component PlayerBotAI cho Red Ignis Sparring:\n\n" +
                    "- Phím [B]: Bật / Tắt chế độ Bot tự động chiến đấu.\n" +
                    "- Phím [T]: Chuyển đổi tốc độ game (1x -> 2x -> 5x) để train nhanh gấp bội.\n" +
                    "- Tự động né chiêu kiếm bay, vờn cự ly và kích hoạt cửa sổ uống máu cho Boss học.",
                    "OK"
                );
            }
        }
    }
}
#endif
