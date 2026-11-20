#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using InfiniteLabyrinth.Core;

public static class CoreSystemsSetupEditor
{
    private const string PREFAB_DIR = "Assets/Prefabs/Core";
    private const string PREFAB_PATH = "Assets/Prefabs/Core/CoreSystems.prefab";
    private const string RESOURCES_PREFAB_PATH = "Assets/Resources/CoreSystems.prefab";

    private static readonly string[] LooseNamesToClean = new string[]
    {
        "[GameManager]", "GameManager",
        "InventoryManager", "[InventoryManager]",
        "[DamageNumberManager]", "DamageNumberManager",
        "AdminCheatManager", "[AdminCheatManager]",
        "PlayerProgression", "[PlayerProgression]",
        "[LoadingScreenManager]", "LoadingScreenManager",
        "LoadingScreenCanvas", "[LoadingScreenCanvas]",
        "[AudioManager]", "AudioManager"
    };

    private static readonly string[] CoreNames = new string[]
    {
        "[GameManager]",
        "InventoryManager",
        "[DamageNumberManager]",
        "AdminCheatManager",
        "PlayerProgression",
        "[LoadingScreenManager]",
        "LoadingScreenCanvas",
        "[AudioManager]"
    };

    // [MenuItem("Infinite Labyrinth/Core/1-Click Setup CoreSystems Prefab & Sync Scenes", false, 5)]
    public static void CreateAndSyncCoreSystems()
    {
        EnsureDirectoryExists(PREFAB_DIR);
        EnsureDirectoryExists("Assets/Resources");

        // 1. Mở LandScene làm chuẩn vì LandScene chứa đầy đủ setting nhất
        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        string landScenePath = "Assets/Scenes/LandScene.unity";
        string dungeonScenePath = "Assets/Scenes/DungeonScene.unity";

        if (EditorSceneManager.GetActiveScene().path != landScenePath)
        {
            EditorSceneManager.OpenScene(landScenePath, OpenSceneMode.Single);
        }

        Debug.Log("<color=cyan>[CoreSystemsSetup]</color> 📦 Bắt đầu thu thập các Manager từ LandScene...");

        // 2. Tìm hoặc tạo [CORE_SYSTEMS] gốc trong LandScene
        GameObject coreSystemsObj = GameObject.Find("[CORE_SYSTEMS]");
        if (coreSystemsObj == null)
        {
            coreSystemsObj = new GameObject("[CORE_SYSTEMS]");
        }
        coreSystemsObj.transform.position = Vector3.zero;

        CoreSystems coreComp = coreSystemsObj.GetComponent<CoreSystems>();
        if (coreComp == null) coreComp = coreSystemsObj.AddComponent<CoreSystems>();

        // 3. Gom các object con vào [CORE_SYSTEMS]
        foreach (string name in CoreNames)
        {
            GameObject obj = GameObject.Find(name);

            // Nếu obj đã nằm trong [CORE_SYSTEMS] rồi thì bỏ qua
            if (obj != null && obj.transform.parent == coreSystemsObj.transform)
            {
                continue;
            }

            // Nếu chưa có trong scene, nạp từ Prefab có sẵn trong Resources
            if (obj == null)
            {
                if (name == "[AudioManager]")
                {
                    GameObject audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Audio/AudioManager.prefab");
                    if (audioPrefab != null)
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(audioPrefab);
                        obj.name = "[AudioManager]";
                    }
                }
                else if (name == "LoadingScreenCanvas")
                {
                    GameObject canvasPrefab = Resources.Load<GameObject>("LoadingScreenCanvas");
                    if (canvasPrefab != null)
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
                        obj.name = "LoadingScreenCanvas";
                    }
                }
            }

            if (obj != null)
            {
                obj.transform.SetParent(coreSystemsObj.transform);
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                Debug.Log($"<color=green>[CoreSystemsSetup]</color> ✔ Đã gom {name} vào [CORE_SYSTEMS]");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[CoreSystemsSetup]</color> ⚠️ Không tìm thấy {name} trong Scene!");
            }
        }

        // Dọn dẹp bất kỳ object trùng lặp rời rạc nào còn sót lại ở ngoài root trong LandScene
        CleanLooseManagersInCurrentScene(coreSystemsObj);

        // Đưa [CORE_SYSTEMS] lên vị trí đầu tiên trong Hierarchy
        coreSystemsObj.transform.SetAsFirstSibling();

        // 4. Lưu thành Prefab tái sử dụng ở Assets/Prefabs/Core và Assets/Resources
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(coreSystemsObj, PREFAB_PATH, InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAsset(coreSystemsObj, RESOURCES_PREFAB_PATH);
        Debug.Log($"<color=green>[CoreSystemsSetup]</color> 💾 Đã lưu Prefab thành công tại:\n- {PREFAB_PATH}\n- {RESOURCES_PREFAB_PATH}");

        // Lưu LandScene
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[CoreSystemsSetup]</color> 💾 Đã lưu LandScene với Prefab [CORE_SYSTEMS] đồng bộ!");

        // 5. Mở DungeonScene và đồng bộ Prefab [CORE_SYSTEMS]
        if (File.Exists(dungeonScenePath))
        {
            EditorSceneManager.OpenScene(dungeonScenePath, OpenSceneMode.Single);
            Debug.Log("<color=cyan>[CoreSystemsSetup]</color> 🔄 Đang đồng bộ sang DungeonScene...");

            // Xóa các object cũ nằm rời rạc trong DungeonScene
            CleanLooseManagersInCurrentScene(null);

            // Xóa [CORE_SYSTEMS] cũ nếu có
            GameObject existingDungeonCore = GameObject.Find("[CORE_SYSTEMS]");
            if (existingDungeonCore != null)
            {
                Object.DestroyImmediate(existingDungeonCore);
            }

            // Đặt Prefab [CORE_SYSTEMS] mới vào DungeonScene
            if (prefabAsset != null)
            {
                GameObject dungeonCore = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                dungeonCore.name = "[CORE_SYSTEMS]";
                dungeonCore.transform.position = Vector3.zero;
                dungeonCore.transform.SetAsFirstSibling();
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[CoreSystemsSetup]</color> 💾 Đã đồng bộ và lưu DungeonScene thành công!");
        }

        // Trở về Scene ban đầu
        string returnScene = !string.IsNullOrEmpty(originalScenePath) ? originalScenePath : landScenePath;
        if (EditorSceneManager.GetActiveScene().path != returnScene)
        {
            EditorSceneManager.OpenScene(returnScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=gold><b>[CoreSystemsSetup] ✅ HOÀN TẤT ĐỒNG BỘ 100%!</b></color>\n" +
                  "Cả LandScene và DungeonScene hiện nay đều sử dụng chung duy nhất 1 Prefab [CORE_SYSTEMS]!\n" +
                  "Tất cả các setting của GameManager, Inventory, Audio, Progression, LoadingScreen, DamageNumber, Cheat đều là Single Source of Truth.");
    }

    private static void CleanLooseManagersInCurrentScene(GameObject excludeParent)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (excludeParent != null && root == excludeParent) continue;
            foreach (var looseName in LooseNamesToClean)
            {
                if (root.name == looseName)
                {
                    Debug.Log($"<color=yellow>[CoreSystemsSetup]</color> 🗑 Đã xóa object rời rạc thừa: {root.name}");
                    Object.DestroyImmediate(root);
                    break;
                }
            }
        }
    }

    private static void EnsureDirectoryExists(string relativePath)
    {
        if (!Directory.Exists(relativePath))
        {
            Directory.CreateDirectory(relativePath);
            AssetDatabase.Refresh();
        }
    }
}
#endif
