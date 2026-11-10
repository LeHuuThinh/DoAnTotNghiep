using UnityEngine;
using UnityEditor;

public static class InspectAndFixRedIgnis
{

[MenuItem("Infinite Labyrinth/Boss/Red Ignis/Inspect & Fix Hierarchy", false, 30)]
    public static void RunInspectionAndFix()
    {
        Debug.Log("<color=yellow>[Inspect RedIgnis]</color> === BẮT ĐẦU RÀ SOÁT RED IGNIS ===");

        GameObject boss = GameObject.Find("Boss_RedIgnis");
        if (boss == null)
        {
            var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go.name == "Boss_RedIgnis" && !EditorUtility.IsPersistent(go))
                {
                    boss = go;
                    break;
                }
            }
        }

        if (boss != null)
        {
            InspectGameObject(boss);
            FixBossHierarchy(boss);
        }
        else
        {
            Debug.LogWarning("[Inspect RedIgnis] Không tìm thấy Boss_RedIgnis trong Scene!");
        }

        // Cũng rà soát trên Prefab
        string prefabPath = "Assets/Characters/Boss/IgnisDo/Boss_RedIgnis.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot != null)
        {
            try
            {
                Debug.Log("<color=yellow>[Inspect RedIgnis Prefab]</color> Rà soát Prefab gốc:");
                InspectGameObject(prefabRoot);
                FixBossHierarchy(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void InspectGameObject(GameObject root)
    {
        Component[] comps = root.GetComponents<Component>();
        Debug.Log($"--- ROOT: {root.name} có {comps.Length} components ---");
        foreach (var c in comps)
        {
            if (c == null) { Debug.Log("  - Missing Component (Null Script)"); continue; }
            string detail = "";
            if (c is CharacterController cc)
            {
                detail = $" [center={cc.center}, height={cc.height}, radius={cc.radius}]";
            }
            else if (c is CapsuleCollider cap)
            {
                detail = $" [center={cap.center}, height={cap.height}, radius={cap.radius}]";
            }
            else if (c is BoxCollider box)
            {
                detail = $" [center={box.center}, size={box.size}]";
            }
            else if (c is Animator a)
            {
                detail = $" [controller={a.runtimeAnimatorController?.name}, avatar={a.avatar?.name}, enabled={a.enabled}]";
            }
            Debug.Log($"  - {c.GetType().Name}{detail}");
        }

        // Duyệt các con
        foreach (Transform child in root.transform)
        {
            Component[] childComps = child.GetComponents<Component>();
            Debug.Log($"  --- CHILD: {child.name} có {childComps.Length} components ---");
            foreach (var cc in childComps)
            {
                if (cc == null) continue;
                string d = "";
                if (cc is Animator a)
                {
                    d = $" [controller={a.runtimeAnimatorController?.name}, avatar={a.avatar?.name}, enabled={a.enabled}]";
                }
                else if (cc is Collider col)
                {
                    d = $" [Collider type: {col.GetType().Name}]";
                }
                Debug.Log($"     * {cc.GetType().Name}{d}");
            }
        }
    }

    private static void FixBossHierarchy(GameObject root)
    {
        bool changed = false;

        // 1. Kiểm tra Animator trên Root (Boss_RedIgnis) -> Gỡ bỏ vì Animator phải nằm ở child RedIgnis
        Animator rootAnim = root.GetComponent<Animator>();
        if (rootAnim != null)
        {
            Debug.Log("<color=orange>[Fix] Gỡ bỏ Animator thừa trên cha Boss_RedIgnis</color>");
            Object.DestroyImmediate(rootAnim, true);
            changed = true;
        }

        // 2. Kiểm tra Animator trên Child (RedIgnis)
        Transform redIgnisChild = root.transform.Find("RedIgnis");
        if (redIgnisChild != null)
        {
            Animator childAnim = redIgnisChild.GetComponent<Animator>();
            if (childAnim == null)
            {
                childAnim = redIgnisChild.gameObject.AddComponent<Animator>();
                changed = true;
            }

            // Gán Controller
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Characters/Boss/IgnisDo/Ignis_animator.controller");
            if (ctrl != null)
            {
                childAnim.runtimeAnimatorController = ctrl;
            }
            childAnim.enabled = true;
            childAnim.applyRootMotion = false;
            Debug.Log($"<color=green>[Fix] Animator trên child RedIgnis đã được kích hoạt: controller={childAnim.runtimeAnimatorController?.name}, avatar={childAnim.avatar?.name}</color>");
        }

        // 3. Rà soát Colliders: Giữ CharacterController cho di chuyển, loại bỏ collider trùng lặp nếu có
        CharacterController charCtrl = root.GetComponent<CharacterController>();
        CapsuleCollider capCol = root.GetComponent<CapsuleCollider>();
        BoxCollider boxCol = root.GetComponent<BoxCollider>();

        if (charCtrl != null && capCol != null)
        {
            Debug.Log("<color=orange>[Fix] Phát hiện cả CharacterController và CapsuleCollider trên cùng 1 Object! Gỡ bỏ CapsuleCollider để tránh lồng 2 vòng xanh.</color>");
            Object.DestroyImmediate(capCol, true);
            changed = true;
        }

        // Cập nhật RedIgnisController tham chiếu
        RedIgnisController ric = root.GetComponent<RedIgnisController>();
        if (ric != null)
        {
            EditorUtility.SetDirty(ric);
        }

        if (changed)
        {
            EditorUtility.SetDirty(root);
            Debug.Log("<color=green>[Fix] Đã sửa cấu trúc phân cấp Boss_RedIgnis thành công!</color>");
        }
    }
}

