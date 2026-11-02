using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupRedIgnisAnimatorAndPrefabs : EditorWindow
{
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        if (!SessionState.GetBool("RedIgnisSetupRecreate_v6", false))
        {
            SessionState.SetBool("RedIgnisSetupRecreate_v6", true);
            EditorApplication.delayCall += () =>
            {
                ExecuteFullSetup();
            };
        }
    }

    [MenuItem("Boss/1-Click Setup Red Ignis (Controller, Animator, VFX)")]
    public static void ExecuteFullSetup()
    {
        Debug.Log("<color=cyan>[Red Ignis Setup]</color> === BẮT ĐẦU CẤU HÌNH TOÀN DIỆN RED IGNIS ===");

        // 1. Cấu hình Animator Controller
        SetupAnimatorController();

        // 2. Cấu hình Prefab đạn kiếm VFX (Ngang & Dọc 90 độ)
        SetupVfxPrefabs();

        // 3. Cấu hình Prefab Boss_RedIgnis và Scene Instance
        SetupBossPrefabsAndInstance();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[Red Ignis Setup] HOÀN TẤT 100%! Đã cấu hình xong: Animator Controller, VFX Prefabs, RedIgnisController, Health, Poise, Phím mũi tên & 4 Kỹ năng chém!</color>");
    }

    public static void SetupAnimatorController()
    {
        string controllerPath = "Assets/Characters/Boss/IgnisDo/Ignis_animator.controller";
        AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existingController != null && existingController.layers.Length > 0 && existingController.layers[0].stateMachine.states.Length >= 8)
        {
            Debug.Log("<color=cyan>[Red Ignis Setup] Ignis_animator.controller đã có đầy đủ states, giữ nguyên hoạt ảnh.</color>");
            return;
        }

        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[Red Ignis Setup] Không thể tạo {controllerPath}");
            return;
        }

        // Tải các Animation Clips
        string animDir = "Assets/Characters/Boss/IgnisDo/animation/Animation/";
        AnimationClip clipIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "IdleRI.anim");
        AnimationClip clipWalkFwd = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkT_RI.anim");
        AnimationClip clipWalkBack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkBackRI.anim");
        AnimationClip clipWalkLeft = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkLeftRI.anim");
        AnimationClip clipWalkRight = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkRightRI.anim");
        AnimationClip clipAttackSlash = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "AttackSlashRI.anim");
        AnimationClip clipHorizontalSlash = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "HorizontalSlashRI.anim");
        AnimationClip clipJumpAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "JumpAttackRI.anim");
        AnimationClip clipComboAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "ComboAttackRI.anim");
        AnimationClip clipDead = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "DeadRI.anim");

        // 1. Đảm bảo các Parameters
        EnsureParameter(controller, "MoveType", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveZ", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "AttackSlash", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "HorizontalSlash", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "JumpAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "ComboAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Dead", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Weaken", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Tạo các States
        AnimatorState stateIdle = rootStateMachine.AddState("IdleRI", new Vector3(300, 100, 0));
        stateIdle.motion = clipIdle;

        // ENTRY PHẢI VÀO IDLERI
        rootStateMachine.defaultState = stateIdle;

        AnimatorState stateWalkFwd = rootStateMachine.AddState("WalkT_RI", new Vector3(300, -20, 0));
        stateWalkFwd.motion = clipWalkFwd;

        AnimatorState stateWalkBack = rootStateMachine.AddState("WalkBackRI", new Vector3(300, 220, 0));
        stateWalkBack.motion = clipWalkBack;

        AnimatorState stateWalkLeft = rootStateMachine.AddState("WalkLeftRI", new Vector3(60, 100, 0));
        stateWalkLeft.motion = clipWalkLeft;

        AnimatorState stateWalkRight = rootStateMachine.AddState("WalkRightRI", new Vector3(540, 100, 0));
        stateWalkRight.motion = clipWalkRight;

        AnimatorState stateAttackSlash = rootStateMachine.AddState("AttackSlashRI", new Vector3(800, -60, 0));
        stateAttackSlash.motion = clipAttackSlash;

        AnimatorState stateHorizontalSlash = rootStateMachine.AddState("HorizontalSlashRI", new Vector3(800, 20, 0));
        stateHorizontalSlash.motion = clipHorizontalSlash;

        AnimatorState stateJumpAttack = rootStateMachine.AddState("JumpAttackRI", new Vector3(800, 100, 0));
        stateJumpAttack.motion = clipJumpAttack;

        AnimatorState stateComboAttack = rootStateMachine.AddState("ComboAttackRI", new Vector3(800, 180, 0));
        stateComboAttack.motion = clipComboAttack;

        AnimatorState stateDead = rootStateMachine.AddState("DeadRI", new Vector3(800, 260, 0));
        stateDead.motion = clipDead;

        // ========================================================
        // LOCOMOTION TRANSITIONS (Chuyển động mượt mà 0.15s, không chờ ExitTime)
        // MoveType: 0 = Idle, 1 = Tới, 2 = Lùi, 3 = Trái, 4 = Phải
        // ========================================================
        AddTransition(stateIdle, stateWalkFwd, "MoveType", AnimatorConditionMode.Equals, 1, 0.15f, false);
        AddTransition(stateWalkFwd, stateIdle, "MoveType", AnimatorConditionMode.Equals, 0, 0.15f, false);

        AddTransition(stateIdle, stateWalkBack, "MoveType", AnimatorConditionMode.Equals, 2, 0.15f, false);
        AddTransition(stateWalkBack, stateIdle, "MoveType", AnimatorConditionMode.Equals, 0, 0.15f, false);

        AddTransition(stateIdle, stateWalkLeft, "MoveType", AnimatorConditionMode.Equals, 3, 0.15f, false);
        AddTransition(stateWalkLeft, stateIdle, "MoveType", AnimatorConditionMode.Equals, 0, 0.15f, false);

        AddTransition(stateIdle, stateWalkRight, "MoveType", AnimatorConditionMode.Equals, 4, 0.15f, false);
        AddTransition(stateWalkRight, stateIdle, "MoveType", AnimatorConditionMode.Equals, 0, 0.15f, false);

        // Chuyển hướng trực tiếp giữa các hướng đi bộ
        AddTransition(stateWalkFwd, stateWalkBack, "MoveType", AnimatorConditionMode.Equals, 2, 0.15f, false);
        AddTransition(stateWalkFwd, stateWalkLeft, "MoveType", AnimatorConditionMode.Equals, 3, 0.15f, false);
        AddTransition(stateWalkFwd, stateWalkRight, "MoveType", AnimatorConditionMode.Equals, 4, 0.15f, false);

        AddTransition(stateWalkBack, stateWalkFwd, "MoveType", AnimatorConditionMode.Equals, 1, 0.15f, false);
        AddTransition(stateWalkBack, stateWalkLeft, "MoveType", AnimatorConditionMode.Equals, 3, 0.15f, false);
        AddTransition(stateWalkBack, stateWalkRight, "MoveType", AnimatorConditionMode.Equals, 4, 0.15f, false);

        AddTransition(stateWalkLeft, stateWalkFwd, "MoveType", AnimatorConditionMode.Equals, 1, 0.15f, false);
        AddTransition(stateWalkLeft, stateWalkBack, "MoveType", AnimatorConditionMode.Equals, 2, 0.15f, false);
        AddTransition(stateWalkLeft, stateWalkRight, "MoveType", AnimatorConditionMode.Equals, 4, 0.15f, false);

        AddTransition(stateWalkRight, stateWalkFwd, "MoveType", AnimatorConditionMode.Equals, 1, 0.15f, false);
        AddTransition(stateWalkRight, stateWalkBack, "MoveType", AnimatorConditionMode.Equals, 2, 0.15f, false);
        AddTransition(stateWalkRight, stateWalkLeft, "MoveType", AnimatorConditionMode.Equals, 3, 0.15f, false);

        // ========================================================
        // ATTACK & DEAD TRANSITIONS (Từ Any State và quay về IdleRI)
        // ========================================================
        AddAnyStateTrigger(rootStateMachine, stateAttackSlash, "AttackSlash", 0.08f);
        AddReturnToIdle(stateAttackSlash, stateIdle, 0.85f, 0.2f);

        AddAnyStateTrigger(rootStateMachine, stateHorizontalSlash, "HorizontalSlash", 0.08f);
        AddReturnToIdle(stateHorizontalSlash, stateIdle, 0.85f, 0.2f);

        AddAnyStateTrigger(rootStateMachine, stateJumpAttack, "JumpAttack", 0.08f);
        AddReturnToIdle(stateJumpAttack, stateIdle, 0.88f, 0.2f);

        AddAnyStateTrigger(rootStateMachine, stateComboAttack, "ComboAttack", 0.08f);
        AddReturnToIdle(stateComboAttack, stateIdle, 0.90f, 0.2f);

        AddAnyStateTrigger(rootStateMachine, stateDead, "Dead", 0.1f);

        EditorUtility.SetDirty(controller);
        Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình xong Animator Controller với Entry = IdleRI và đầy đủ transitions!</color>");
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static void AddTransition(AnimatorState src, AnimatorState dst, string paramName, AnimatorConditionMode mode, float threshold, float duration, bool hasExitTime)
    {
        var trans = src.AddTransition(dst);
        trans.hasExitTime = hasExitTime;
        trans.duration = duration;
        trans.AddCondition(mode, threshold, paramName);
    }

    private static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState dst, string triggerName, float duration)
    {
        var trans = sm.AddAnyStateTransition(dst);
        trans.hasExitTime = false;
        trans.duration = duration;
        trans.canTransitionToSelf = false;
        trans.AddCondition(AnimatorConditionMode.If, 0, triggerName);
    }

    private static void AddReturnToIdle(AnimatorState attackState, AnimatorState idleState, float exitTime, float duration)
    {
        var trans = attackState.AddTransition(idleState);
        trans.hasExitTime = true;
        trans.exitTime = exitTime;
        trans.duration = duration;
    }

    public static void SetupVfxPrefabs()
    {
        string vfxDir = "Assets/Characters/Boss/IgnisDo/VFX/";
        GameObject impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Slash_Impact.prefab");

        // 1. Setup Ignis_Slash_Horizontal.prefab (Chém ngang, tilt = 0)
        SetupSingleSlashPrefab(vfxDir + "Ignis_Slash_Horizontal.prefab", 0f, 50f, 18f, impactPrefab);

        // 2. Setup Ignis_Slash_Vertical.prefab (Chém dọc xoay 90 độ, tilt = 90)
        SetupSingleSlashPrefab(vfxDir + "Ignis_Slash_Vertical.prefab", 90f, 65f, 20f, impactPrefab);
    }

    private static void SetupSingleSlashPrefab(string prefabPath, float tiltAngle, float damage, float speed, GameObject impactVfx)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"[Red Ignis Setup] Không thể mở {prefabPath}");
            return;
        }

        try
        {
            // Gỡ bỏ script Demo cũ nếu có trên root và các con
            var allComponents = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in allComponents)
            {
                if (c != null && (c.GetType().Name == "Projectile" || c.GetType().Name == "SlashDecal"))
                {
                    DestroyImmediate(c, true);
                }
            }

            // Gán Rigidbody
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Gán BoxCollider
            BoxCollider col = root.GetComponent<BoxCollider>();
            if (col == null) col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0f, 0.5f);
            col.size = new Vector3(2.5f, 1.2f, 2.0f);

            // Gán RedIgnisSlashProjectile
            RedIgnisSlashProjectile proj = root.GetComponent<RedIgnisSlashProjectile>();
            if (proj == null) proj = root.AddComponent<RedIgnisSlashProjectile>();

            proj.Damage = damage;
            proj.Speed = speed;
            proj.SlashTiltAngle = tiltAngle;
            if (impactVfx != null) proj.HitVfxPrefab = impactVfx;

            // Đặt góc xoay mặc định của Prefab theo góc tilt
            if (Mathf.Abs(tiltAngle) > 0.1f)
            {
                root.transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
            }
            else
            {
                root.transform.localRotation = Quaternion.identity;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"<color=cyan>[Red Ignis Setup] Đã cập nhật Prefab {prefabPath} với góc tilt = {tiltAngle} độ!</color>");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void SetupBossPrefabsAndInstance()
    {
        string prefabPath = "Assets/Characters/Boss/IgnisDo/Boss_RedIgnis.prefab";
        string vfxDir = "Assets/Characters/Boss/IgnisDo/VFX/";

        GameObject horizSlash = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Slash_Horizontal.prefab");
        GameObject vertSlash = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Slash_Vertical.prefab");
        GameObject impactSlash = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Slash_Impact.prefab");
        GameObject jumpImpact = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Jump_Impact.prefab");
        GameObject comboSlash = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Ignis_Combo_Slash.prefab");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/Boss/IgnisDo/Ignis_animator.controller");

        // --- CẤU HÌNH PREFAB ---
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root != null)
        {
            try
            {
                ConfigureIgnisGameObject(root, controller, horizSlash, vertSlash, impactSlash, jumpImpact, comboSlash);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình Boss_RedIgnis.prefab thành công!</color>");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // --- CẤU HÌNH SCENE INSTANCE (NẾU CÓ TRONG SCENE HIỆN TẠI) ---
        GameObject sceneBoss = GameObject.Find("Boss_RedIgnis");
        if (sceneBoss != null)
        {
            ConfigureIgnisGameObject(sceneBoss, controller, horizSlash, vertSlash, impactSlash, jumpImpact, comboSlash);
            EditorUtility.SetDirty(sceneBoss);
            Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình Boss_RedIgnis trong Scene hiện tại thành công!</color>");
        }
    }

    private static void ConfigureIgnisGameObject(GameObject go, AnimatorController controller, 
        GameObject horizSlash, GameObject vertSlash, GameObject impactSlash, GameObject jumpImpact, GameObject comboSlash)
    {
        // Tag & Layer
        go.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) go.layer = enemyLayer;

        // 1. GỠ BỎ ANIMATOR THỪA TRÊN CHA BOSS_REDIGNIS (Tránh kẹt T-Pose do thiếu Avatar)
        Animator parentAnim = go.GetComponent<Animator>();
        if (parentAnim != null)
        {
            DestroyImmediate(parentAnim, true);
        }

        // 2. ĐẢM BẢO ANIMATOR NẰM TRÊN CON REDIGNIS (Nơi chứa xương Armature & Humanoid Avatar)
        Transform childRedIgnis = go.transform.Find("RedIgnis");
        if (childRedIgnis == null)
        {
            var childAnimCheck = go.GetComponentInChildren<Animator>();
            if (childAnimCheck != null && childAnimCheck.gameObject != go) childRedIgnis = childAnimCheck.transform;
        }

        if (childRedIgnis != null)
        {
            Animator childAnim = childRedIgnis.GetComponent<Animator>();
            if (childAnim == null) childAnim = childRedIgnis.gameObject.AddComponent<Animator>();
            if (controller != null) childAnim.runtimeAnimatorController = controller;
            childAnim.applyRootMotion = false;
            childAnim.enabled = true;
        }

        // 3. COLLIDERS: Gỡ bỏ CapsuleCollider / BoxCollider trùng lặp để tránh 2 vòng xanh lồng nhau
        CapsuleCollider extraCap = go.GetComponent<CapsuleCollider>();
        if (extraCap != null) DestroyImmediate(extraCap, true);

        BoxCollider extraBox = go.GetComponent<BoxCollider>();
        if (extraBox != null) DestroyImmediate(extraBox, true);

        // Giữ duy nhất 1 CharacterController chuẩn kích thước cho scale 2.0
        CharacterController cc = go.GetComponent<CharacterController>();
        if (cc == null) cc = go.AddComponent<CharacterController>();
        // Cân chỉnh center và height khớp chính xác với tỷ lệ scale 2.0 của Boss (World height: 2.9m, radius: 0.7m)
        cc.center = new Vector3(0f, 0.725f, 0f);
        cc.height = 1.45f;
        cc.radius = 0.35f;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.08f;

        // Cân chỉnh SlashSpawnPoint ngang tầm ngực/kiếm cho scale 2.0
        Transform spawnPoint = go.transform.Find("SlashSpawnPoint");
        if (spawnPoint != null)
        {
            spawnPoint.localPosition = new Vector3(0.08f, 0.75f, 0.70f);
            spawnPoint.localScale = Vector3.one;
        }

        // Health Component
        Health health = go.GetComponent<Health>();
        if (health == null) health = go.AddComponent<Health>();

        // Set MaxHealth = 1200 via reflection
        var maxHpF = typeof(Health).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (maxHpF != null) maxHpF.SetValue(health, 1200f);

        var curHpF = typeof(Health).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (curHpF != null) curHpF.SetValue(health, 1200f);

        var baseHpF = typeof(Health).GetField("baseMaxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (baseHpF != null) baseHpF.SetValue(health, 1200f);

        // RedIgnisController Component
        RedIgnisController bossCtrl = go.GetComponent<RedIgnisController>();
        if (bossCtrl == null) bossCtrl = go.AddComponent<RedIgnisController>();

        // Thiết lập Serialized fields trên RedIgnisController
        SerializedObject so = new SerializedObject(bossCtrl);
        so.Update();

        so.FindProperty("bossMaxHealth").floatValue = 1200f;
        so.FindProperty("maxPoise").floatValue = 400f;
        so.FindProperty("currentPoise").floatValue = 400f;
        so.FindProperty("moveSpeed").floatValue = 4.2f;
        so.FindProperty("strafeSpeed").floatValue = 3.2f;
        so.FindProperty("alwaysFacePlayer").boolValue = true;
        so.FindProperty("autoFindPlayer").boolValue = true;
        so.FindProperty("gravity").floatValue = -20f;
        so.FindProperty("groundedGravity").floatValue = -5f;

        // Kỹ năng 1: AttackSlashRI (Chém dọc)
        so.FindProperty("attackSlashDamage").floatValue = 65f;
        so.FindProperty("attackSlashCooldown").floatValue = 3.0f;
        so.FindProperty("attackSlashDelay").floatValue = 0.42f;
        so.FindProperty("attackSlashProjectileSpeed").floatValue = 22f;
        so.FindProperty("attackSlashTiltAngle").floatValue = 90f;
        so.FindProperty("attackSlashTotalDuration").floatValue = 1.5f;

        // Kỹ năng 2: HorizontalSlashRI (Chém ngang)
        so.FindProperty("horizontalSlashDamage").floatValue = 50f;
        so.FindProperty("horizontalSlashCooldown").floatValue = 3.5f;
        so.FindProperty("horizontalSlashDelay").floatValue = 0.40f;
        so.FindProperty("horizontalSlashProjectileSpeed").floatValue = 20f;
        so.FindProperty("horizontalSlashTiltAngle").floatValue = 0f;
        so.FindProperty("horizontalSlashTotalDuration").floatValue = 1.6f;

        // Kỹ năng 3: JumpAttackRI (Nhảy dậm đất)
        so.FindProperty("jumpAttackDamage").floatValue = 80f;
        so.FindProperty("jumpAttackCooldown").floatValue = 5.0f;
        so.FindProperty("jumpAttackWindupDelay").floatValue = 0.25f;
        so.FindProperty("jumpAttackLeapDuration").floatValue = 0.55f;
        so.FindProperty("jumpLeapDistance").floatValue = 7.5f;
        so.FindProperty("jumpAttackApexHeight").floatValue = 1.8f;
        so.FindProperty("jumpAttackRadius").floatValue = 4.0f;
        so.FindProperty("jumpAttackRecoveryDuration").floatValue = 0.85f;

        // Kỹ năng 4: ComboAttackRI (Chém liên hoàn 4 nhát)
        so.FindProperty("comboHitDamage").floatValue = 35f;
        so.FindProperty("comboAttackCooldown").floatValue = 6.0f;
        so.FindProperty("comboLungeDistance").floatValue = 0.75f;
        so.FindProperty("comboHitRadius").floatValue = 3.2f;
        so.FindProperty("comboRecoveryDuration").floatValue = 0.90f;

        // Kỹ năng 1: Attack Slash (Chém dọc)
        if (comboSlash != null) so.FindProperty("attackSlashSwingVfxPrefab").objectReferenceValue = comboSlash;
        if (vertSlash != null) so.FindProperty("verticalSlashPrefab").objectReferenceValue = vertSlash;
        if (impactSlash != null) so.FindProperty("attackSlashImpactVfxPrefab").objectReferenceValue = impactSlash;
        if (impactSlash != null) so.FindProperty("slashImpactVfxPrefab").objectReferenceValue = impactSlash;

        // Kỹ năng 2: Horizontal Slash (Chém ngang)
        if (comboSlash != null) so.FindProperty("horizontalSlashSwingVfxPrefab").objectReferenceValue = comboSlash;
        if (horizSlash != null) so.FindProperty("horizontalSlashPrefab").objectReferenceValue = horizSlash;
        if (impactSlash != null) so.FindProperty("horizontalSlashImpactVfxPrefab").objectReferenceValue = impactSlash;

        // Kỹ năng 3 & 4
        if (jumpImpact != null) so.FindProperty("jumpImpactVfxPrefab").objectReferenceValue = jumpImpact;
        if (comboSlash != null) so.FindProperty("comboSlashVfxPrefab").objectReferenceValue = comboSlash;

        // Tìm weapon socket
        Transform[] allChildren = go.GetComponentsInChildren<Transform>(true);
        foreach (var c in allChildren)
        {
            if (c.name == "Hand_R_Weapon")
            {
                so.FindProperty("weaponSocket").objectReferenceValue = c;
                break;
            }
        }

        so.ApplyModifiedProperties();
        bossCtrl.ApplyBossStats();
    }
}
