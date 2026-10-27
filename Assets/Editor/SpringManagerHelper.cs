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

    [MenuItem("Tools/RedIgnis/Tự Động Sửa Lỗi Áo Choàng (Auto Fix Cape)")]
    public static void AutoFixRedIgnisCape()
    {
        // 1. Tìm GameObject Boss_RedIgnis trong Scene hoặc Selection
        GameObject targetObj = Selection.activeGameObject;
        if (targetObj == null || targetObj.GetComponentInChildren<SpringManager>() == null)
        {
            targetObj = GameObject.Find("Boss_RedIgnis");
        }

        if (targetObj == null)
        {
            Debug.LogError("[AutoFix Cape] Không tìm thấy GameObject 'Boss_RedIgnis' trong Scene!");
            return;
        }

        SpringManager manager = targetObj.GetComponentInChildren<SpringManager>();
        if (manager == null)
        {
            Debug.LogError("[AutoFix Cape] Không tìm thấy SpringManager trên Boss_RedIgnis!");
            return;
        }

        Undo.RecordObject(manager, "Auto Fix RedIgnis Cape");

        // 2. Tìm và gỡ SpringBone trên 4 đốt ngọn cuối cùng (không có xương con):
        // Bone.013 (Nón trùm), Bone.025 (Áo choàng lưng), Bone.035 (Vai phải), Bone.037 (Vai trái)
        string[] endBoneNames = new string[] { "Bone.013", "Bone.025", "Bone.035", "Bone.037" };
        int removedCount = 0;

        Transform[] allTransforms = targetObj.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (endBoneNames.Contains(t.name))
            {
                SpringBone sb = t.GetComponent<SpringBone>();
                if (sb != null)
                {
                    Undo.DestroyObjectImmediate(sb);
                    removedCount++;
                    Debug.Log($"<color=cyan>[Đã gỡ SpringBone ngọn]</color> {t.name} (vì đốt ngọn không có xương con)");
                }
            }
        }

        // 3. Tự động quét và sắp xếp lại mảng springBones để sạch 100% phần tử null
        manager.FindSpringBones(true);

        // 4. Tối ưu cấu hình vật lý cho áo choàng bay tự nhiên:
        manager.enableAngleLimits = false;  // Tắt khóa góc xoay để áo choàng không bị cứng đơ
        manager.collideWithGround = false;  // Tắt va chạm sàn ảo để tránh giật
        manager.dynamicRatio = 0.9f;

        // Cân chỉnh lực cho các đốt xương còn lại
        if (manager.springBones != null)
        {
            foreach (var bone in manager.springBones)
            {
                if (bone == null) continue;
                Undo.RecordObject(bone, "Tune SpringBone Values");
                bone.stiffnessForce = Mathf.Clamp(bone.stiffnessForce, 40f, 80f);
                bone.dragForce = Mathf.Clamp(bone.dragForce, 0.35f, 0.5f);
                bone.springForce = new Vector3(0f, -0.02f, 0f);
                EditorUtility.SetDirty(bone);
            }
        }

        EditorUtility.SetDirty(manager);

        if (PrefabUtility.IsPartOfAnyPrefab(targetObj))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            GameObject rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObj);
            if (rootPrefab != null)
            {
                PrefabUtility.ApplyPrefabInstance(rootPrefab, InteractionMode.AutomatedAction);
            }
        }

        Debug.Log($"<color=green>[HOÀN TẤT THÀNH CÔNG!]</color> Đã gỡ {removedCount} SpringBone lỗi ở các đốt ngọn, dọn sạch Null trong SpringManager, và tối ưu thông số mềm mại cho áo choàng! Số lượng xương hợp lệ: {manager.springBones.Length}");
    }

    [MenuItem("Tools/RedIgnis/Chẩn Đoán Lún Đất (Diagnose Sinking)")]
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
