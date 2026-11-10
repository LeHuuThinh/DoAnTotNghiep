using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace InfiniteLabyrinth.Editor
{
    public class LandSceneOptimizerEditor : EditorWindow
    {
        private const string SCENE_NAME = "LandScene";

        [MenuItem("Infinite Labyrinth/Optimization/1. Tối Ưu Hóa LandScene (1-Click)", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<LandSceneOptimizerEditor>("LandScene Optimizer");
            window.minSize = new Vector2(440, 580);
        }

        [MenuItem("Infinite Labyrinth/Optimization/2. Chạy Nhanh Tối Ưu Hóa LandScene (1-Click)", false, 11)]
        public static void RunQuickOptimization()
        {
            EnsureLandSceneLoaded();
            ExecuteAllOptimizations();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🚀 BỘ TỐI ƯU HÓA HIỆU NĂNG LANDSCENE", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Công cụ tự động tối ưu hóa toàn diện LandScene:\n" +
                "1. Terrain: Bật GPU Instancing, hạ Tree Distance từ 5km về 200m, chỉnh Pixel Error 10.\n" +
                "2. Gravel (94 viên sỏi): Gộp thành 1 Draw Call duy nhất bằng CombineMeshes.\n" +
                "3. Village (214 props): Đánh dấu Static (Batching & Occlusion), tắt bóng đổ cho đạo cụ nhỏ.\n" +
                "4. Water & Volume: Tắt bóng nước phẳng, tắt GameObject Bloom trùng lặp.\n" +
                "5. NPC (61 dân làng): Đã tích hợp Distance LOD Culling (ngủ đông ngoài 40m) & Animator Culling.\n" +
                "Kỳ vọng: Tăng FPS từ ~65 FPS lên 100 - 120+ FPS!",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🚀 1-CLICK: TỐI ƯU HÓA TOÀN BỘ LANDSCENE", GUILayout.Height(45)))
            {
                if (EnsureLandSceneLoaded())
                {
                    ExecuteAllOptimizations();
                }
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("--- CÁC TÙY CHỌN RIÊNG LẺ ---", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Tối ưu Terrain & Mặt Nước (Water)", GUILayout.Height(30)))
            {
                if (EnsureLandSceneLoaded())
                {
                    OptimizeTerrainAndWater();
                    MarkSceneDirtyAndSave();
                }
            }

            if (GUILayout.Button("2. Gộp Mesh Sỏi Đá (Gravel - 94 -> 1 Draw Call)", GUILayout.Height(30)))
            {
                if (EnsureLandSceneLoaded())
                {
                    CombineGravel();
                    MarkSceneDirtyAndSave();
                }
            }

            if (GUILayout.Button("3. Đánh Dấu Static & Tối Ưu Bóng Đổ Village (214 props)", GUILayout.Height(30)))
            {
                if (EnsureLandSceneLoaded())
                {
                    OptimizeVillageStaticAndShadows();
                    MarkSceneDirtyAndSave();
                }
            }

            if (GUILayout.Button("4. Dọn Dẹp Volume & Bloom Trùng Lặp", GUILayout.Height(30)))
            {
                if (EnsureLandSceneLoaded())
                {
                    CleanupVolumes();
                    MarkSceneDirtyAndSave();
                }
            }

            if (GUILayout.Button("5. Cân Bằng Dân Số NPC (Giữ 24 NPC đại diện, ẩn 37 NPC dư)", GUILayout.Height(30)))
            {
                if (EnsureLandSceneLoaded())
                {
                    OptimizeNpcPopulation(24);
                    MarkSceneDirtyAndSave();
                }
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("--- HOÀN TÁC & KHÔI PHỤC ---", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↩️ Khôi Phục Sỏi Gốc", GUILayout.Height(28)))
            {
                if (EnsureLandSceneLoaded())
                {
                    RevertGravel();
                    MarkSceneDirtyAndSave();
                }
            }

            if (GUILayout.Button("↩️ Bật Lại Toàn Bộ 61 NPC", GUILayout.Height(28)))
            {
                if (EnsureLandSceneLoaded())
                {
                    RestoreAllNpcs();
                    MarkSceneDirtyAndSave();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static bool EnsureLandSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != SCENE_NAME)
            {
                string scenePath = "Assets/Scenes/LandScene.unity";
                if (EditorUtility.DisplayDialog("Mở LandScene",
                    $"Scene hiện tại là '{activeScene.name}'. Bạn có muốn mở '{SCENE_NAME}' để tiến hành tối ưu không?",
                    "Mở LandScene", "Hủy"))
                {
                    EditorSceneManager.OpenScene(scenePath);
                    return true;
                }
                return false;
            }
            return true;
        }

        public static void ExecuteAllOptimizations()
        {
            int terrainCount = OptimizeTerrainAndWater();
            int gravelCount = CombineGravel();
            int villageCount = OptimizeVillageStaticAndShadows();
            int volumeCount = CleanupVolumes();

            MarkSceneDirtyAndSave();

            EditorUtility.DisplayDialog("Tối Ưu Hóa Hoàn Tất!",
                $"Đã hoàn tất tối ưu hóa LandScene thành công:\n\n" +
                $"• Terrain: Bật Draw Instanced, Tree Distance 200m, Shadow Off ({terrainCount} terrain)\n" +
                $"• Gravel: Gộp {gravelCount} viên sỏi thành 1 Mesh (tiết kiệm {gravelCount - 1} Draw Calls)\n" +
                $"• Village: Đánh dấu Static cho {villageCount} vật thể, lọc bóng đổ cho đạo cụ nhỏ\n" +
                $"• Water: Tắt bóng đổ phẳng\n" +
                $"• Volume: Tắt Bloom trùng lặp ({volumeCount} volume)\n" +
                $"• NPC: Đã kích hoạt Distance LOD Culling (ngủ đông ngoài 40m) & Animator Culling trong NpcPatrol.cs\n\n" +
                $"Hãy bấm Play để kiểm tra FPS cải thiện!",
                "Tuyệt vời");
        }

        private static int OptimizeTerrainAndWater()
        {
            int count = 0;
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                Undo.RecordObject(terrain, "Optimize Terrain");
                terrain.drawInstanced = true;
                terrain.treeDistance = 200f;
                terrain.treeBillboardDistance = 120f;
                terrain.detailObjectDistance = 80f;
                terrain.heightmapPixelError = 10f;
                terrain.shadowCastingMode = ShadowCastingMode.Off;
                count++;
                Debug.Log("[LandSceneOptimizer] Đã tối ưu hóa thông số Terrain: DrawInstanced=True, TreeDist=200, PixelError=10, Shadows=Off.");
            }

            GameObject waterObj = GameObject.Find("Water");
            if (waterObj != null)
            {
                MeshRenderer mr = waterObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Undo.RecordObject(mr, "Optimize Water Shadows");
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    Debug.Log("[LandSceneOptimizer] Đã tắt Shadow Casting cho mặt nước (Water).");
                }
            }

            return count;
        }

        private static int CombineGravel()
        {
            GameObject gravelRoot = GameObject.Find("Gravel");
            if (gravelRoot == null)
            {
                Debug.LogWarning("[LandSceneOptimizer] Không tìm thấy GameObject 'Gravel' trong Scene.");
                return 0;
            }

            Transform combinedChild = gravelRoot.transform.Find("Gravel_Combined");
            if (combinedChild != null)
            {
                Undo.DestroyObjectImmediate(combinedChild.gameObject);
            }

            Transform backupChild = gravelRoot.transform.Find("Gravel_Original_Backup");
            if (backupChild == null)
            {
                GameObject backupObj = new GameObject("Gravel_Original_Backup");
                Undo.RegisterCreatedObjectUndo(backupObj, "Create Gravel Backup");
                backupChild = backupObj.transform;
                backupChild.SetParent(gravelRoot.transform, false);
            }

            List<MeshFilter> filtersToCombine = new List<MeshFilter>();
            Material sharedMaterial = null;

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < gravelRoot.transform.childCount; i++)
            {
                Transform child = gravelRoot.transform.GetChild(i);
                if (child == backupChild || child.name.StartsWith("Gravel_Combined")) continue;
                childrenToMove.Add(child);
            }

            foreach (var child in childrenToMove)
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mf != null && mf.sharedMesh != null && mr != null && mr.sharedMaterial != null)
                {
                    filtersToCombine.Add(mf);
                    if (sharedMaterial == null) sharedMaterial = mr.sharedMaterial;
                }
            }

            int count = filtersToCombine.Count;
            if (count == 0)
            {
                Debug.Log("[LandSceneOptimizer] Không tìm thấy MeshFilter nào cần gộp trong Gravel (có thể đã gộp trước đó).");
                return 0;
            }

            List<CombineInstance> combines = new List<CombineInstance>();
            foreach (var mf in filtersToCombine)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                ci.transform = gravelRoot.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                combines.Add(ci);
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "Gravel_Combined_Mesh";
            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combines.ToArray(), true, true);

            string assetFolder = "Assets/Scenes/LandScene";
            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "LandScene");
            }
            string assetPath = $"{assetFolder}/Gravel_Combined_Mesh.asset";
            AssetDatabase.CreateAsset(combinedMesh, assetPath);
            AssetDatabase.SaveAssets();

            GameObject combinedObj = new GameObject("Gravel_Combined");
            Undo.RegisterCreatedObjectUndo(combinedObj, "Create Combined Gravel");
            combinedObj.transform.SetParent(gravelRoot.transform, false);

            MeshFilter cmf = combinedObj.AddComponent<MeshFilter>();
            cmf.sharedMesh = combinedMesh;

            MeshRenderer cmr = combinedObj.AddComponent<MeshRenderer>();
            cmr.sharedMaterial = sharedMaterial;
            cmr.shadowCastingMode = ShadowCastingMode.Off;
            cmr.receiveShadows = true;

            GameObjectUtility.SetStaticEditorFlags(combinedObj,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

            foreach (var child in childrenToMove)
            {
                Undo.SetTransformParent(child, backupChild, "Move to Gravel Backup");
            }
            backupChild.gameObject.SetActive(false);

            Debug.Log($"[LandSceneOptimizer] Đã gộp thành công {count} viên sỏi vào 'Gravel_Combined' (1 Draw Call duy nhất, Mesh lưu tại {assetPath}).");
            return count;
        }

        private static void RevertGravel()
        {
            GameObject gravelRoot = GameObject.Find("Gravel");
            if (gravelRoot == null) return;

            Transform combinedChild = gravelRoot.transform.Find("Gravel_Combined");
            if (combinedChild != null)
            {
                Undo.DestroyObjectImmediate(combinedChild.gameObject);
            }

            Transform backupChild = gravelRoot.transform.Find("Gravel_Original_Backup");
            if (backupChild != null)
            {
                backupChild.gameObject.SetActive(true);
                List<Transform> restored = new List<Transform>();
                for (int i = 0; i < backupChild.childCount; i++)
                {
                    restored.Add(backupChild.GetChild(i));
                }
                foreach (var t in restored)
                {
                    Undo.SetTransformParent(t, gravelRoot.transform, "Restore Gravel Child");
                }
                Undo.DestroyObjectImmediate(backupChild.gameObject);
                Debug.Log($"[LandSceneOptimizer] Đã khôi phục {restored.Count} viên sỏi gốc.");
            }
        }

        private static int OptimizeVillageStaticAndShadows()
        {
            GameObject villageRoot = GameObject.Find("Village");
            if (villageRoot == null)
            {
                Debug.LogWarning("[LandSceneOptimizer] Không tìm thấy GameObject 'Village' trong Scene.");
                return 0;
            }

            int optimizedCount = 0;
            MeshRenderer[] renderers = villageRoot.GetComponentsInChildren<MeshRenderer>(true);

            StaticEditorFlags staticFlags = StaticEditorFlags.BatchingStatic |
                                            StaticEditorFlags.OccludeeStatic |
                                            StaticEditorFlags.OccluderStatic |
                                            StaticEditorFlags.OffMeshLinkGeneration;

            foreach (var mr in renderers)
            {
                GameObject go = mr.gameObject;
                Undo.RecordObject(go, "Set Static Flags");
                GameObjectUtility.SetStaticEditorFlags(go, staticFlags);

                string lowerName = go.name.ToLower();
                bool isMajorBuilding = lowerName.Contains("house") ||
                                       lowerName.Contains("windmill") ||
                                       lowerName.Contains("smithy") ||
                                       lowerName.Contains("shop") ||
                                       lowerName.Contains("tower") ||
                                       lowerName.Contains("sawmill") ||
                                       lowerName.Contains("grain") ||
                                       lowerName.Contains("bridge") ||
                                       lowerName.Contains("well");

                Undo.RecordObject(mr, "Optimize Prop Shadow");
                if (!isMajorBuilding)
                {
                    // Tắt bóng đổ cho các vật thể nhỏ (sỏi, hoa, bụi cỏ, chum vại, hàng rào, thùng nhỏ, cọc gỗ)
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    // Giữ bóng đổ cho nhà cửa công trình lớn
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }

                optimizedCount++;
            }

            // Đánh dấu cả Village root là Static
            GameObjectUtility.SetStaticEditorFlags(villageRoot, staticFlags);

            Debug.Log($"[LandSceneOptimizer] Đã đánh dấu Static cho toàn bộ cụm Village ({optimizedCount} MeshRenderers), bật bóng đổ cho công trình lớn và tắt bóng đổ cho đồ vật nhỏ.");
            return optimizedCount;
        }

        private static int CleanupVolumes()
        {
            int count = 0;
            GameObject bloomObj = GameObject.Find("Bloom");
            if (bloomObj != null)
            {
                Undo.RecordObject(bloomObj, "Disable Redundant Bloom");
                bloomObj.SetActive(false);
                count++;
                Debug.Log("[LandSceneOptimizer] Đã vô hiệu hóa GameObject 'Bloom' trùng lặp.");
            }
            return count;
        }

        private static int OptimizeNpcPopulation(int targetActiveCount = 24)
        {
            GameObject npcRoot = GameObject.Find("NPC");
            if (npcRoot == null) return 0;

            int total = npcRoot.transform.childCount;
            if (total == 0) return 0;

            int step = Mathf.Max(1, total / targetActiveCount);
            int activeKept = 0;
            int deactivated = 0;

            for (int i = 0; i < total; i++)
            {
                Transform child = npcRoot.transform.GetChild(i);
                Undo.RecordObject(child.gameObject, "Optimize NPC Population");

                // Giữ lại số lượng NPC rải đều
                bool shouldKeep = (activeKept < targetActiveCount) && (i % step == 0 || (total - i) <= (targetActiveCount - activeKept));
                if (shouldKeep)
                {
                    child.gameObject.SetActive(true);
                    activeKept++;
                }
                else
                {
                    child.gameObject.SetActive(false);
                    deactivated++;
                }
            }

            Debug.Log($"[LandSceneOptimizer] Đã cân bằng dân số NPC: Giữ {activeKept} NPC hoạt động, tạm ẩn {deactivated} NPC.");
            return deactivated;
        }

        private static int RestoreAllNpcs()
        {
            GameObject npcRoot = GameObject.Find("NPC");
            if (npcRoot == null) return 0;

            int count = 0;
            for (int i = 0; i < npcRoot.transform.childCount; i++)
            {
                Transform child = npcRoot.transform.GetChild(i);
                Undo.RecordObject(child.gameObject, "Restore All NPCs");
                child.gameObject.SetActive(true);
                count++;
            }

            Debug.Log($"[LandSceneOptimizer] Đã kích hoạt lại toàn bộ {count} NPC.");
            return count;
        }

        private static void MarkSceneDirtyAndSave()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[LandSceneOptimizer] Đã lưu Scene '{activeScene.name}' thành công!");
        }
    }
}
