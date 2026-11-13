using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Animations.SpringBones;

public static class SpringManagerHelper
{
    [MenuItem("CONTEXT/SpringManager/Làm Mới & Dọn Dẹp Xương (Fix Null Bones)")]
    public static void CleanSpringManager(MenuCommand command)
    {
        SpringManager manager = (SpringManager)command.context;
        if (manager == null) return;

        Undo.RecordObject(manager, "Clean Spring Bones");
        
        // Quét lại toàn bộ SpringBone thực sự còn tồn tại, loại bỏ hoàn toàn các phần tử Missing/Null
        manager.FindSpringBones(true);
        EditorUtility.SetDirty(manager);
        
        Debug.Log($"<color=green>[SpringManager]</color> Đã dọn sạch các phần tử Null! Số lượng xương hợp lệ hiện tại: {manager.springBones.Length}");
    }

    private const string RedIgnisPrefabPath = "Assets/Characters/Boss/IgnisDo/Boss_RedIgnis.prefab";
    private const string DarknessIgnisPrefabPath = "Assets/Characters/Boss/IgnisTim/Boss_DarknessIgnis.prefab";

    [MenuItem("Infinite Labyrinth/Boss/Red Ignis/Tự Động Sửa Lỗi Áo Choàng (Auto Fix Cape)")]
    public static void AutoFixRedIgnisCape()
    {
        AutoFixBossCloth<RedIgnisClothMotion>(
            RedIgnisPrefabPath,
            "Boss_RedIgnis",
            ConfigureBossObject,
            "AutoFix Cape",
            "14 SpringBones (chống lộn/xoắn áo khi vung tay) & 16 colliders (kèm 3 Back Panel)");
    }

    [MenuItem("Infinite Labyrinth/Boss/Darkness Ignis/Tự Động Sửa Lỗi Tóc & Áo Choàng (Auto Fix Cape & Hair)")]
    public static void AutoFixDarknessIgnisCloth()
    {
        AutoFixBossCloth<DarknessIgnisClothMotion>(
            DarknessIgnisPrefabPath,
            "Boss_DarknessIgnis",
            ConfigureDarknessIgnisObject,
            "Darkness Ignis Cloth",
            "11 SpringBones (Tóc Bone.003/010 + Áo Trái 30/33/36 + Giữa 18/21/27 + Phải 37/42/45) & 16 colliders (kèm 3 Back Panel)");
    }

    private static void AutoFixBossCloth<TCloth>(
        string prefabPath,
        string sceneDefaultName,
        System.Func<GameObject, bool, SpringManager> configureFn,
        string logLabel,
        string detailSummary) where TCloth : IgnisClothMotionBase
    {
        int updatedTargets = 0;

        // 1. Always bake cleanly onto the Boss prefab asset
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot != null)
        {
            try
            {
                configureFn(prefabRoot, false);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                updatedTargets++;
                Debug.Log($"<color=green>[{logLabel}]</color> Đã cấu hình {detailSummary} trên Prefab: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        else
        {
            Debug.LogWarning($"[{logLabel}] Không tìm thấy Prefab tại '{prefabPath}'.");
        }

        // 2. Also update any scene instance (selected hierarchy subtree or matching default boss name)
        GameObject sceneObj = FindSceneBossTarget<TCloth>(sceneDefaultName);
        if (sceneObj != null)
        {
            configureFn(sceneObj, true);
            EditorUtility.SetDirty(sceneObj);
            if (!Application.isPlaying && sceneObj.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneObj.scene);
            }

            updatedTargets++;
            Debug.Log($"<color=green>[{logLabel}]</color> Đã đồng bộ Scene instance '{sceneObj.name}'.");
        }

        if (updatedTargets > 0)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static GameObject FindSceneBossTarget<TCloth>(string sceneDefaultName) where TCloth : IgnisClothMotionBase
    {
        Transform selected = Selection.activeTransform;
        if (selected != null && !PrefabUtility.IsPartOfPrefabAsset(selected.gameObject))
        {
            TCloth parentCloth = selected.GetComponentInParent<TCloth>(true);
            if (parentCloth != null && !PrefabUtility.IsPartOfPrefabAsset(parentCloth.gameObject))
            {
                return parentCloth.gameObject;
            }

            Transform cursor = selected;
            while (cursor != null)
            {
                if (cursor.name == sceneDefaultName)
                {
                    return cursor.gameObject;
                }

                cursor = cursor.parent;
            }
        }

        GameObject byName = GameObject.Find(sceneDefaultName);
        if (byName != null && !PrefabUtility.IsPartOfPrefabAsset(byName))
        {
            return byName;
        }

        TCloth inScene = Object.FindFirstObjectByType<TCloth>(FindObjectsInactive.Include);
        if (inScene != null && !PrefabUtility.IsPartOfPrefabAsset(inScene.gameObject))
        {
            return inScene.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Configures Red Ignis's hat tail, 3-strip cape, length-limit links, anatomical colliders, and RedIgnisClothMotion.
    /// </summary>
    public static SpringManager ConfigureBossObject(GameObject targetObj, bool useUndo = true)
    {
        return ConfigureBossClothInternal<RedIgnisClothMotion>(
            targetObj,
            useUndo,
            (root, record, destroy) => RedIgnisClothMotion.ConfigureClothRig(root, applyPresetValues: true, record, destroy));
    }

    /// <summary>
    /// Configures Darkness Ignis's hair tail, 3-strip cape, 16 anatomical colliders, and DarknessIgnisClothMotion.
    /// </summary>
    public static SpringManager ConfigureDarknessIgnisObject(GameObject targetObj, bool useUndo = true)
    {
        return ConfigureBossClothInternal<DarknessIgnisClothMotion>(
            targetObj,
            useUndo,
            (root, record, destroy) => DarknessIgnisClothMotion.ConfigureClothRig(root, applyPresetValues: true, record, destroy));
    }

    private static SpringManager ConfigureBossClothInternal<TCloth>(
        GameObject targetObj,
        bool useUndo,
        System.Func<GameObject, System.Action<UnityEngine.Object, string>, System.Action<UnityEngine.Object>, SpringManager> rigBuilder)
        where TCloth : IgnisClothMotionBase
    {
        if (targetObj == null)
        {
            return null;
        }

        System.Action<UnityEngine.Object, string> recordUndo = useUndo
            ? (obj, label) => Undo.RecordObject(obj, label)
            : null;
        System.Action<UnityEngine.Object> destroyComp = useUndo
            ? (obj) => Undo.DestroyObjectImmediate(obj)
            : (System.Action<UnityEngine.Object>)UnityEngine.Object.DestroyImmediate;

        SpringManager manager = rigBuilder(targetObj, recordUndo, destroyComp);

        if (manager != null)
        {
            EditorUtility.SetDirty(manager);
            if (manager.springBones != null)
            {
                foreach (SpringBone bone in manager.springBones)
                {
                    if (bone != null)
                    {
                        EditorUtility.SetDirty(bone);
                    }
                }
            }
        }

        TCloth clothMotion = targetObj.GetComponent<TCloth>();
        if (clothMotion != null)
        {
            EditorUtility.SetDirty(clothMotion);
        }

        return manager;
    }

    [MenuItem("Infinite Labyrinth/Boss/Red Ignis/Chẩn Đoán Lún Đất (Diagnose Sinking)")]
    public static void DiagnoseSinking()
    {
        GameObject ignis = GameObject.Find("Boss_RedIgnis");
        if (ignis == null)
        {
            return;
        }

        // 1. Force reimport model and walk animation with updated settings
        AssetDatabase.ImportAsset("Assets/Characters/Boss/IgnisDo/RedIgnis.fbx", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Characters/Boss/IgnisDo/animation/Ignis_Walk.fbx", ImportAssetOptions.ForceUpdate);
        Debug.Log("<color=yellow>[Diagnose]</color> Đã Force Reimport RedIgnis.fbx và Ignis_Walk.fbx!");

        Debug.Log($"=== CHẨN ĐOÁN BOSS_REDIGNIS ===\n" +
                  $"Boss_RedIgnis Pos World: {ignis.transform.position}, Local: {ignis.transform.localPosition}, Scale: {ignis.transform.localScale}");

        var comps = ignis.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c != null) Debug.Log($"[Root Component] {c.GetType().Name}");
        }

        Transform[] allChildren = ignis.GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t.name == "RedIgnis" || t.name == "Armature" || t.name.Contains("Hips"))
            {
                Debug.Log($"[Child: {t.name}] Pos World: {t.position}, Local: {t.localPosition}, LossyScale: {t.lossyScale}");
            }

            var anim = t.GetComponent<Animator>();
            if (anim != null)
            {
                Debug.Log($"[Animator on {t.name}] Avatar: {(anim.avatar != null ? anim.avatar.name : "NULL")}, " +
                          $"Controller: {(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL")}, " +
                          $"applyRootMotion: {anim.applyRootMotion}");
            }

            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[Rigidbody on {t.name}] useGravity: {rb.useGravity}, isKinematic: {rb.isKinematic}");
            }
        }
    }
}

