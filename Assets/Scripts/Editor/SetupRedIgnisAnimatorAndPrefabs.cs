using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupRedIgnisAnimatorAndPrefabs : EditorWindow
{
    // Đã gỡ bỏ [InitializeOnLoadMethod] để tránh tự động chạy ngầm đè lại các thông số delay/cooldown mà người dùng đã chỉnh trong Inspector.
    // Chỉ chạy khi người dùng chủ động click menu: Infinite Labyrinth -> Boss -> Red Ignis -> 1-Click Setup Red Ignis...
    [MenuItem("Infinite Labyrinth/Boss/Red Ignis/1-Click Setup Red Ignis (Controller, Animator, VFX)", false, 31)]
    public static void ExecuteFullSetup()
    {
        Debug.Log("<color=cyan>[Red Ignis Setup]</color> === BẮT ĐẦU CẤU HÌNH TOÀN DIỆN RED IGNIS ===");

        // 1. Cấu hình Animator Controller
        SetupAnimatorController();

        // 2. Cấu hình Prefab đạn kiếm VFX (Ngang & Dọc 90 độ)
        SetupVfxPrefabs();

        // 3. Cấu hình Prefab Lửa Kiếm (Ignis_Sword_Flame)
        CreateSwordFlamePrefab();

        // 4. Cấu hình Prefab Boss_RedIgnis và Scene Instance
        SetupBossPrefabsAndInstance();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[Red Ignis Setup] HOÀN TẤT 100%! Đã cấu hình xong: Animator Controller, VFX Prefabs, Lửa kiếm Ignis_Sword_Flame, RedIgnisController, Health, Poise, Phím mũi tên & 4 Kỹ năng chém!</color>");
    }

    public static void SetupAnimatorController()
    {
        string controllerPath = "Assets/Characters/Boss/IgnisDo/Ignis_animator.controller";
        string animDir = "Assets/Characters/Boss/IgnisDo/animation/Animation/";
        AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (existingController != null && existingController.layers.Length > 0)
        {
            AnimatorStateMachine sm = existingController.layers[0].stateMachine;
            bool hasSwordCasting = false;
            AnimatorState idleState = null;

            foreach (var childState in sm.states)
            {
                if (childState.state.name == "SwordCastingRI" || childState.state.name == "SwordCasting")
                {
                    hasSwordCasting = true;
                }
                if (childState.state.name == "IdleRI")
                {
                    idleState = childState.state;
                }
            }

            if (!hasSwordCasting)
            {
                EnsureParameter(existingController, "SwordCasting", AnimatorControllerParameterType.Trigger);
                AnimationClip addClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "SwordCastingRI.anim");
                if (addClip != null)
                {
                    AnimatorState addState = sm.AddState("SwordCastingRI", new Vector3(800, 340, 0));
                    addState.motion = addClip;
                    AddAnyStateTrigger(sm, addState, "SwordCasting", 0.08f);
                    if (idleState != null)
                    {
                        AddReturnToIdle(addState, idleState, 0.95f, 0.2f);
                    }
                    EditorUtility.SetDirty(existingController);
                    AssetDatabase.SaveAssets();
                    Debug.Log("<color=green>[Red Ignis Setup] Đã bổ sung thành công state SwordCastingRI vào Ignis_animator.controller!</color>");
                }
            }

            // Đảm bảo có đầy đủ 2 state Weaken (WeakenFrontRI quỳ -> WeakenBackRI đứng dậy)
            SetupWeakenAnimationAndStates(existingController);
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
        AnimationClip clipIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "IdleRI.anim");
        AnimationClip clipWalkFwd = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkT_RI.anim");
        AnimationClip clipWalkBack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkBackRI.anim");
        AnimationClip clipWalkLeft = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkLeftRI.anim");
        AnimationClip clipWalkRight = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WalkRightRI.anim");
        AnimationClip clipAttackSlash = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "AttackSlashRI.anim");
        AnimationClip clipHorizontalSlash = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "HorizontalSlashRI.anim");
        AnimationClip clipJumpAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "JumpAttackRI.anim");
        AnimationClip clipComboAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "ComboAttackRI.anim");
        AnimationClip clipSwordCasting = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "SwordCastingRI.anim");
        AnimationClip clipDead = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "DeadRI.anim");

        // 1. Đảm bảo các Parameters
        EnsureParameter(controller, "MoveType", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "WalkSpeed", AnimatorControllerParameterType.Float, 1f);
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveZ", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "AttackSlash", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "HorizontalSlash", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "JumpAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "ComboAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "SwordCasting", AnimatorControllerParameterType.Trigger);
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
        stateWalkFwd.speedParameterActive = true;
        stateWalkFwd.speedParameter = "WalkSpeed";

        AnimatorState stateWalkBack = rootStateMachine.AddState("WalkBackRI", new Vector3(300, 220, 0));
        stateWalkBack.motion = clipWalkBack;
        stateWalkBack.speedParameterActive = true;
        stateWalkBack.speedParameter = "WalkSpeed";

        AnimatorState stateWalkLeft = rootStateMachine.AddState("WalkLeftRI", new Vector3(60, 100, 0));
        stateWalkLeft.motion = clipWalkLeft;
        stateWalkLeft.speedParameterActive = true;
        stateWalkLeft.speedParameter = "WalkSpeed";

        AnimatorState stateWalkRight = rootStateMachine.AddState("WalkRightRI", new Vector3(540, 100, 0));
        stateWalkRight.motion = clipWalkRight;
        stateWalkRight.speedParameterActive = true;
        stateWalkRight.speedParameter = "WalkSpeed";

        AnimatorState stateAttackSlash = rootStateMachine.AddState("AttackSlashRI", new Vector3(800, -60, 0));
        stateAttackSlash.motion = clipAttackSlash;

        AnimatorState stateHorizontalSlash = rootStateMachine.AddState("HorizontalSlashRI", new Vector3(800, 20, 0));
        stateHorizontalSlash.motion = clipHorizontalSlash;

        AnimatorState stateJumpAttack = rootStateMachine.AddState("JumpAttackRI", new Vector3(800, 100, 0));
        stateJumpAttack.motion = clipJumpAttack;

        AnimatorState stateComboAttack = rootStateMachine.AddState("ComboAttackRI", new Vector3(800, 180, 0));
        stateComboAttack.motion = clipComboAttack;

        AnimatorState stateSwordCasting = rootStateMachine.AddState("SwordCastingRI", new Vector3(800, 260, 0));
        stateSwordCasting.motion = clipSwordCasting;

        AnimatorState stateDead = rootStateMachine.AddState("DeadRI", new Vector3(800, 340, 0));
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

        AddAnyStateTrigger(rootStateMachine, stateSwordCasting, "SwordCasting", 0.08f);
        AddReturnToIdle(stateSwordCasting, stateIdle, 0.95f, 0.2f);

        AddAnyStateTrigger(rootStateMachine, stateDead, "Dead", 0.1f);

        // ========================================================
        // WEAKEN TRANSITIONS (WeakenFrontRI -> WeakenBackRI -> IdleRI)
        // ========================================================
        AnimationClip clipWeakenFront = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WeakenFrontRI.anim");
        AnimationClip clipWeakenBack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WeakenBackRI.anim");

        AnimatorState stateWeakenFront = rootStateMachine.AddState("WeakenFrontRI", new Vector3(800, 420, 0));
        stateWeakenFront.motion = clipWeakenFront;

        AnimatorState stateWeakenBack = rootStateMachine.AddState("WeakenBackRI", new Vector3(800, 500, 0));
        stateWeakenBack.motion = clipWeakenBack;

        AddAnyStateTrigger(rootStateMachine, stateWeakenFront, "Weaken", 0.08f);
        AddExitTransition(stateWeakenFront, stateWeakenBack, 0.96f, 0.15f);
        AddExitTransition(stateWeakenBack, stateIdle, 0.95f, 0.2f);

        EditorUtility.SetDirty(controller);
        Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình xong Animator Controller với Entry = IdleRI và đầy đủ transitions!</color>");
    }

    [MenuItem("Infinite Labyrinth/Boss/Red Ignis/Phục Hồi WeakenFrontRI Gốc Từ FBX", false, 36)]
    public static void RestoreWeakenFrontFromFBX()
    {
        string fbxPath = "Assets/Characters/Boss/IgnisDo/animation/WeakenFrontRI.fbx";
        string animPath = "Assets/Characters/Boss/IgnisDo/animation/Animation/WeakenFrontRI.anim";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip srcClip = null;
        foreach (var a in assets)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
            {
                srcClip = c;
                break;
            }
        }
        if (srcClip != null)
        {
            AnimationClip copy = Object.Instantiate(srcClip);
            copy.name = "WeakenFrontRI";
            AssetDatabase.CreateAsset(copy, animPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>[Red Ignis] Đã phục hồi thành công WeakenFrontRI.anim nguyên bản từ file FBX!</b></color>");
        }
        else
        {
            Debug.LogError("[Red Ignis] Không tìm thấy clip trong WeakenFrontRI.fbx!");
        }
    }

    [MenuItem("Infinite Labyrinth/Boss/Red Ignis/Cấu Hình Hoạt Ảnh Weaken (Quỳ WeakenFrontRI -> Đứng Dậy WeakenBackRI)", false, 37)]
    public static void MenuSetupWeakenAnimationAndStates()
    {
        string controllerPath = "Assets/Characters/Boss/IgnisDo/Ignis_animator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[Red Ignis Setup] Không tìm thấy {controllerPath}");
            return;
        }
        SetupWeakenAnimationAndStates(controller);
    }

    public static void SetupWeakenAnimationAndStates(AnimatorController controller)
    {
        string animDir = "Assets/Characters/Boss/IgnisDo/animation/Animation/";
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        EnsureParameter(controller, "Weaken", AnimatorControllerParameterType.Trigger);

        AnimationClip clipFront = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WeakenFrontRI.anim");
        AnimationClip clipBack = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + "WeakenBackRI.anim");

        AnimatorState idleState = null;
        AnimatorState stateFront = null;
        AnimatorState stateBack = null;

        foreach (var childState in sm.states)
        {
            if (childState.state.name == "IdleRI") idleState = childState.state;
            if (childState.state.name == "WeakenFrontRI" || childState.state.name == "WeakenFront") stateFront = childState.state;
            if (childState.state.name == "WeakenBackRI" || childState.state.name == "WeakenBack") stateBack = childState.state;
        }

        // Tạo hoặc cập nhật state WeakenFrontRI (Quỳ xuống)
        if (stateFront == null)
        {
            stateFront = sm.AddState("WeakenFrontRI", new Vector3(800, 420, 0));
            AddAnyStateTrigger(sm, stateFront, "Weaken", 0.08f);
        }
        if (clipFront != null) stateFront.motion = clipFront;

        // Tạo hoặc cập nhật state WeakenBackRI (Đứng dậy)
        if (stateBack == null)
        {
            stateBack = sm.AddState("WeakenBackRI", new Vector3(800, 500, 0));
        }
        if (clipBack != null) stateBack.motion = clipBack;

        // Đảm bảo transition: WeakenFrontRI -> WeakenBackRI (Exit Time = 0.96, Duration = 0.15s)
        bool hasFrontToBack = false;
        foreach (var t in stateFront.transitions)
        {
            if (t.destinationState == stateBack)
            {
                hasFrontToBack = true;
                t.hasExitTime = true;
                t.exitTime = 0.96f;
                t.duration = 0.15f;
                break;
            }
        }
        if (!hasFrontToBack)
        {
            AddExitTransition(stateFront, stateBack, 0.96f, 0.15f);
        }

        // Đảm bảo transition: WeakenBackRI -> IdleRI (Exit Time = 0.95, Duration = 0.2s)
        if (idleState != null)
        {
            bool hasBackToIdle = false;
            foreach (var t in stateBack.transitions)
            {
                if (t.destinationState == idleState)
                {
                    hasBackToIdle = true;
                    t.hasExitTime = true;
                    t.exitTime = 0.95f;
                    t.duration = 0.2f;
                    break;
                }
            }
            if (!hasBackToIdle)
            {
                AddExitTransition(stateBack, idleState, 0.95f, 0.2f);
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>[Red Ignis Setup] HOÀN TẤT CẤU HÌNH WEAKEN! WeakenFrontRI (quỳ) -> WeakenBackRI (đứng dậy) -> IdleRI.</color>");
    }

    private static void AddExitTransition(AnimatorState src, AnimatorState dst, float exitTime, float duration)
    {
        var trans = src.AddTransition(dst);
        trans.hasExitTime = true;
        trans.exitTime = exitTime;
        trans.duration = duration;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float defaultFloat = 0f)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = type,
            defaultFloat = defaultFloat
        });
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

    public static GameObject CreateSwordFlamePrefab()
    {
        string vfxDir = "Assets/Characters/Boss/IgnisDo/VFX/";
        string prefabPath = vfxDir + "Ignis_Sword_Flame.prefab";

        string cfxrGfx = "Assets/VFX/Origin/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/";
        Material emberMat = AssetDatabase.LoadAssetAtPath<Material>(cfxrGfx + "cfxr ember blur hdr ab.mat");
        Material stretchFireMat = AssetDatabase.LoadAssetAtPath<Material>(cfxrGfx + "cfxr stretch fire crisp fade hdr ab.mat");
        Material flareAddMat = AssetDatabase.LoadAssetAtPath<Material>(cfxrGfx + "cfxr flare add.mat");
        Material fireGlowMat = AssetDatabase.LoadAssetAtPath<Material>(cfxrGfx + "cfxr fire full blurred fade hdr add.mat");
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Matthew Guz/Status and Auras FREE/Materials/Aura Burn/Smoke.mat");
        Material fallbackFireMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Matthew Guz/Status and Auras FREE/Materials/Aura Burn/Fire.mat");

        GameObject root = new GameObject("Ignis_Sword_Flame");
        IgnisSwordFlame flameCtrl = root.AddComponent<IgnisSwordFlame>();

        // TỌA ĐỘ VÀ KÍCH THƯỚC CHUẨN XÁC 100% THEO VERTICES THỰC TẾ CỦA KIEMSIUNHANDO.FBX:
        // - Chuôi & Tay cầm (Handle): Z = 0.00m -> 0.30m (Hoàn toàn sạch sẽ, lộ rõ bàn tay cầm kiếm)
        // - Quai kiếm (Crossguard): Z = 0.30m -> 0.36m (Bề ngang X = 0.27m)
        // - Lưỡi kiếm thực tế (Blade): Bắt đầu từ Z = 0.36m đến chóp mũi Z = 1.22m
        // -> Tâm đặt chính xác tại Z = 0.79m, chiều dài Z = 0.86m (Bao trùm trọn vẹn 100% lưỡi kiếm)
        // -> Bề ngang X = 0.24m (Phủ kín cả 2 cạnh kiếm sắc bén), bề dày Y = 0.08m
        Vector3 bladeCenter = new Vector3(0f, 0f, 0.79f);
        Vector3 bladeBoxScale = new Vector3(0.24f, 0.08f, 0.86f);

        // 1. Blade_Fire_Body (THÂN LỬA DÀY ĐẶC CUỒN CUỘN: Ôm trọn kín mít lưỡi kiếm bằng Additive UV Distortion)
        GameObject fireBodyObj = new GameObject("Blade_Fire_Body");
        fireBodyObj.transform.SetParent(root.transform, false);
        fireBodyObj.transform.localPosition = bladeCenter;
        fireBodyObj.transform.localRotation = Quaternion.identity;

        ParticleSystem psBody = fireBodyObj.AddComponent<ParticleSystem>();
        var bm = psBody.main;
        bm.duration = 1.0f;
        bm.loop = true;
        bm.playOnAwake = false;
        bm.simulationSpace = ParticleSystemSimulationSpace.Local;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.48f);
        bm.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f); // Lửa bốc cuồn cuộn nhẹ nhàng quanh thân
        bm.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.40f);  // Bản lửa to, dày dặn bọc kín kim loại
        bm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        bm.maxParticles = 300;

        var bEmission = psBody.emission;
        bEmission.enabled = true;
        bEmission.rateOverTime = 130f; // Mật độ dày đặc, không còn bị thưa

        var bShape = psBody.shape;
        bShape.enabled = true;
        bShape.shapeType = ParticleSystemShapeType.Box;
        bShape.scale = bladeBoxScale;
        bShape.randomDirectionAmount = 0.20f;

        var bSize = psBody.sizeOverLifetime;
        bSize.enabled = true;
        AnimationCurve curveBody = new AnimationCurve();
        curveBody.AddKey(0f, 0.75f);
        curveBody.AddKey(0.3f, 1.15f);
        curveBody.AddKey(1f, 0.25f);
        bSize.size = new ParticleSystem.MinMaxCurve(1f, curveBody);

        var bColor = psBody.colorOverLifetime;
        bColor.enabled = true;
        Gradient gradBody = new Gradient();
        gradBody.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), 
                new GradientColorKey(new Color(1f, 0.50f, 0.08f), 0.35f), 
                new GradientColorKey(new Color(0.9f, 0.18f, 0.02f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f), 
                new GradientAlphaKey(0.95f, 0.15f), 
                new GradientAlphaKey(0.85f, 0.7f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        bColor.color = new ParticleSystem.MinMaxGradient(gradBody);

        ParticleSystemRenderer rendBody = fireBodyObj.GetComponent<ParticleSystemRenderer>();
        rendBody.material = (fireGlowMat != null) ? fireGlowMat : fallbackFireMat;
        rendBody.renderMode = ParticleSystemRenderMode.Billboard;

        // 2. Blade_Fire_Tongues (LƯỠI LỬA SẮC NÉT: Liếm dọc 2 cạnh chém răng cưa của kiếm)
        GameObject fireTonguesObj = new GameObject("Blade_Fire_Tongues");
        fireTonguesObj.transform.SetParent(root.transform, false);
        fireTonguesObj.transform.localPosition = bladeCenter;
        fireTonguesObj.transform.localRotation = Quaternion.identity;

        ParticleSystem psTongues = fireTonguesObj.AddComponent<ParticleSystem>();
        var tm = psTongues.main;
        tm.duration = 1.0f;
        tm.loop = true;
        tm.playOnAwake = false;
        tm.simulationSpace = ParticleSystemSimulationSpace.Local;
        tm.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.38f);
        tm.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        tm.startSize = new ParticleSystem.MinMaxCurve(0.20f, 0.32f);
        tm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        tm.maxParticles = 200;

        var tEmission = psTongues.emission;
        tEmission.enabled = true;
        tEmission.rateOverTime = 75f;

        var tShape = psTongues.shape;
        tShape.enabled = true;
        tShape.shapeType = ParticleSystemShapeType.Box;
        tShape.scale = bladeBoxScale;

        ParticleSystemRenderer rendTongues = fireTonguesObj.GetComponent<ParticleSystemRenderer>();
        rendTongues.material = (stretchFireMat != null) ? stretchFireMat : fallbackFireMat;
        rendTongues.renderMode = ParticleSystemRenderMode.Billboard;

        // 3. Sparks_Burst (CƠN BÃO TIA LỬA BÙNG NỔ CỰC MẠNH: Dày đặc, tốc độ cao, kéo vệt chói sáng)
        GameObject sparksBurstObj = new GameObject("Sparks_Burst");
        sparksBurstObj.transform.SetParent(root.transform, false);
        sparksBurstObj.transform.localPosition = bladeCenter;
        sparksBurstObj.transform.localRotation = Quaternion.identity;

        ParticleSystem psSparks = sparksBurstObj.AddComponent<ParticleSystem>();
        var sm = psSparks.main;
        sm.duration = 1.0f;
        sm.loop = true;
        sm.playOnAwake = false;
        sm.simulationSpace = ParticleSystemSimulationSpace.Local;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.52f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.2f); // Lực bắn cực mạnh
        sm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
        sm.maxParticles = 400;

        var sEmission = psSparks.emission;
        sEmission.enabled = true;
        sEmission.rateOverTime = 160f; // Tăng gấp đôi mật độ tia lửa
        sEmission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 25, 35),
            new ParticleSystem.Burst(0.35f, 20, 30),
            new ParticleSystem.Burst(0.70f, 22, 32)
        });

        var sShape = psSparks.shape;
        sShape.enabled = true;
        sShape.shapeType = ParticleSystemShapeType.Box;
        sShape.scale = bladeBoxScale;
        sShape.randomDirectionAmount = 0.90f; // Bắn tỏa đa hướng mãnh liệt

        var sNoise = psSparks.noise;
        sNoise.enabled = true;
        sNoise.strength = 0.80f;
        sNoise.frequency = 1.6f;
        sNoise.scrollSpeed = 1.2f;
        sNoise.damping = true;

        var sColor = psSparks.colorOverLifetime;
        sColor.enabled = true;
        Gradient gradSparks = new Gradient();
        gradSparks.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.98f, 0.6f), 0f), 
                new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.55f), 
                new GradientColorKey(new Color(0.95f, 0.15f, 0.01f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.9f, 0.65f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        sColor.color = new ParticleSystem.MinMaxGradient(gradSparks);

        ParticleSystemRenderer rendSparks = sparksBurstObj.GetComponent<ParticleSystemRenderer>();
        rendSparks.material = (emberMat != null) ? emberMat : fallbackFireMat;
        rendSparks.renderMode = ParticleSystemRenderMode.Stretch;
        rendSparks.velocityScale = 0.08f;
        rendSparks.lengthScale = 2.6f;

        // 4. Flares_Burst (VỆT LỬA CHÓI ADDITIVE: Bùng phát năng lượng chói lòa)
        GameObject flaresObj = new GameObject("Flares_Burst");
        flaresObj.transform.SetParent(root.transform, false);
        flaresObj.transform.localPosition = bladeCenter;
        flaresObj.transform.localRotation = Quaternion.identity;

        ParticleSystem psFlares = flaresObj.AddComponent<ParticleSystem>();
        var fm = psFlares.main;
        fm.duration = 1.0f;
        fm.loop = true;
        fm.playOnAwake = false;
        fm.simulationSpace = ParticleSystemSimulationSpace.Local;
        fm.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
        fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
        fm.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
        fm.maxParticles = 180;

        var fEmission = psFlares.emission;
        fEmission.enabled = true;
        fEmission.rateOverTime = 60f;

        var fShape = psFlares.shape;
        fShape.enabled = true;
        fShape.shapeType = ParticleSystemShapeType.Box;
        fShape.scale = bladeBoxScale;
        fShape.randomDirectionAmount = 0.75f;

        ParticleSystemRenderer rendFlares = flaresObj.GetComponent<ParticleSystemRenderer>();
        rendFlares.material = (flareAddMat != null) ? flareAddMat : fallbackFireMat;
        rendFlares.renderMode = ParticleSystemRenderMode.Stretch;
        rendFlares.velocityScale = 0.05f;
        rendFlares.lengthScale = 2.0f;

        // 5. Blade_Smoke (KHÓI NHIỆT MỎNG: Thoát nhiệt dọc sống kiếm)
        GameObject bladeSmokeObj = new GameObject("Blade_Smoke");
        bladeSmokeObj.transform.SetParent(root.transform, false);
        bladeSmokeObj.transform.localPosition = bladeCenter;
        bladeSmokeObj.transform.localRotation = Quaternion.identity;

        ParticleSystem psSmoke = bladeSmokeObj.AddComponent<ParticleSystem>();
        var smokeMain = psSmoke.main;
        smokeMain.duration = 1.0f;
        smokeMain.loop = true;
        smokeMain.playOnAwake = false;
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
        smokeMain.maxParticles = 50;

        var smokeEmission = psSmoke.emission;
        smokeEmission.enabled = true;
        smokeEmission.rateOverTime = 18f;

        var smokeShape = psSmoke.shape;
        smokeShape.enabled = true;
        smokeShape.shapeType = ParticleSystemShapeType.Box;
        smokeShape.scale = bladeBoxScale;

        var smokeColor = psSmoke.colorOverLifetime;
        smokeColor.enabled = true;
        Gradient gradSmoke = new Gradient();
        gradSmoke.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.25f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        smokeColor.color = new ParticleSystem.MinMaxGradient(gradSmoke);

        ParticleSystemRenderer rendSmoke = bladeSmokeObj.GetComponent<ParticleSystemRenderer>();
        if (smokeMat != null) rendSmoke.material = smokeMat;
        rendSmoke.renderMode = ParticleSystemRenderMode.Billboard;

        // 6. Point Light (ÁNH LỬA CHIẾU SÁNG RỰC RỠ: Tỏa sáng lên kiếm và áo giáp)
        Light pointLight = fireBodyObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.52f, 0.12f);
        pointLight.range = 4.2f;
        pointLight.intensity = 3.0f;
        pointLight.shadows = LightShadows.None;

        // 7. Sword Weapon Trail (VỆT CHÉM ĐẠI KIẾM THEO CHUYỂN ĐỘNG THỰC TẾ)
        GameObject trailObj = new GameObject("Sword_Trail");
        trailObj.transform.SetParent(root.transform, false);
        trailObj.transform.localPosition = Vector3.zero;
        trailObj.transform.localRotation = Quaternion.identity;

        IgnisSwordTrail swordTrail = trailObj.AddComponent<IgnisSwordTrail>();
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/Boss/IgnisDo/VFX/Ignis_Sword_Trail.mat");
        if (trailMat == null) trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Eric VFX Studio/Resource/Materials/trail05.mat");

        SerializedObject soTrail = new SerializedObject(swordTrail);
        soTrail.Update();
        soTrail.FindProperty("baseOffset").vector3Value = new Vector3(0f, 0f, 0.35f);
        soTrail.FindProperty("tipOffset").vector3Value = new Vector3(0f, 0f, 1.45f);
        soTrail.FindProperty("trailDuration").floatValue = 0.16f; // Biến mất nhanh gọn trong 0.16s theo mẫu
        soTrail.FindProperty("minVertexDistance").floatValue = 0.02f;
        soTrail.FindProperty("motionSmoothing").floatValue = 0.45f; // Chống rung giật micro-jitter tay kiếm
        soTrail.FindProperty("verticalJitterDamping").floatValue = 0.35f; // Triệt tiêu nhấp nhô theo trục thẳng đứng
        soTrail.FindProperty("useSplineSmoothing").boolValue = true;
        soTrail.FindProperty("splineSubdivisions").intValue = 3;
        soTrail.FindProperty("minVelocityThreshold").floatValue = 0.35f;
        if (trailMat != null) soTrail.FindProperty("trailMaterial").objectReferenceValue = trailMat;
        soTrail.ApplyModifiedProperties();

        // Cập nhật IgnisSwordFlame
        SerializedObject soFlame = new SerializedObject(flameCtrl);
        soFlame.Update();
        var pMaxLight = soFlame.FindProperty("maxLightIntensity");
        if (pMaxLight != null) pMaxLight.floatValue = 3.0f;
        var pAutoIgnite = soFlame.FindProperty("autoIgniteOnStart");
        if (pAutoIgnite != null) pAutoIgnite.boolValue = false; // Kiếm thường trước 50% máu
        soFlame.ApplyModifiedProperties();

        flameCtrl.CacheComponents();

        // Lưu thành Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log($"<color=cyan>[Red Ignis Setup] Đã tạo thành công Prefab ngọn lửa kiếm chuẩn (Dày dặn, bùng nổ, ôm khít 100% lưỡi kiếm) tại: {prefabPath}</color>");
        return savedPrefab;
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
        GameObject slashRed = AssetDatabase.LoadAssetAtPath<GameObject>(vfxDir + "Slash_Red.prefab");
        if (slashRed == null) slashRed = comboSlash;
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/Boss/IgnisDo/Ignis_animator.controller");

        // --- CẤU HÌNH PREFAB ---
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root != null)
        {
            try
            {
                ConfigureIgnisGameObject(root, controller, horizSlash, vertSlash, impactSlash, jumpImpact, slashRed);
                SpringManagerHelper.ConfigureBossObject(root, useUndo: false);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình Boss_RedIgnis.prefab thành công với hiệu ứng Slash_Red!</color>");
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
            ConfigureIgnisGameObject(sceneBoss, controller, horizSlash, vertSlash, impactSlash, jumpImpact, slashRed);
            EditorUtility.SetDirty(sceneBoss);
            Debug.Log("<color=cyan>[Red Ignis Setup] Đã cấu hình Boss_RedIgnis trong Scene hiện tại thành công với hiệu ứng Slash_Red!</color>");
        }

        // Dọn dẹp Aura Burn hoặc Fire Flare thử nghiệm đặt tự do ở scene root nếu có
        GameObject looseAura = GameObject.Find("Aura Burn");
        if (looseAura != null && looseAura.transform.parent == null)
        {
            DestroyImmediate(looseAura);
            Debug.Log("<color=yellow>[Red Ignis Setup] Đã dọn dẹp Aura Burn tạm ở scene root.</color>");
        }
        GameObject looseFlare = GameObject.Find("Fire Flare");
        if (looseFlare != null && looseFlare.transform.parent == null)
        {
            DestroyImmediate(looseFlare);
            Debug.Log("<color=yellow>[Red Ignis Setup] Đã dọn dẹp Fire Flare tạm ở scene root.</color>");
        }
    }

    private static void ConfigureIgnisGameObject(GameObject go, AnimatorController controller, 
        GameObject horizSlash, GameObject vertSlash, GameObject impactSlash, GameObject jumpImpact, GameObject slashVfx)
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

        // Thiết lập Serialized fields trên RedIgnisController (Bảo toàn các giá trị người dùng đã tinh chỉnh)
        SerializedObject so = new SerializedObject(bossCtrl);
        so.Update();

        SetDefaultFloatIfUnset(so, "bossMaxHealth", 1200f);
        SetDefaultFloatIfUnset(so, "maxPoise", 400f);
        SetDefaultFloatIfUnset(so, "currentPoise", 400f);
        SetDefaultFloatIfUnset(so, "weakenFrontDuration", 4.27f);
        SetDefaultFloatIfUnset(so, "weakenBackDuration", 4.27f);
        SetDefaultFloatIfUnset(so, "moveSpeed", 4.2f);
        SetDefaultFloatIfUnset(so, "strafeSpeed", 1.9f);
        so.FindProperty("alwaysFacePlayer").boolValue = true;
        so.FindProperty("autoFindPlayer").boolValue = true;
        SetDefaultFloatIfUnset(so, "gravity", -20f);
        SetDefaultFloatIfUnset(so, "groundedGravity", -5f);

        // Kỹ năng 1: AttackSlashRI (Chém dọc) - Bảo toàn delay và stats người dùng chỉnh trong Inspector
        SetDefaultFloatIfUnset(so, "attackSlashDamage", 65f);
        SetDefaultFloatIfUnset(so, "attackSlashCooldown", 3.0f);
        SetDefaultFloatIfUnset(so, "attackSlashDelay", 0.42f);
        SetDefaultFloatIfUnset(so, "attackSlashProjectileSpeed", 22f);
        SetDefaultFloatIfUnset(so, "attackSlashTiltAngle", 90f);
        SetDefaultFloatIfUnset(so, "attackSlashTotalDuration", 1.5f);

        // Kỹ năng 2: HorizontalSlashRI (Chém ngang)
        SetDefaultFloatIfUnset(so, "horizontalSlashDamage", 50f);
        SetDefaultFloatIfUnset(so, "horizontalSlashCooldown", 3.5f);
        SetDefaultFloatIfUnset(so, "horizontalSlashDelay", 0.40f);
        SetDefaultFloatIfUnset(so, "horizontalSlashProjectileSpeed", 20f);
        SetDefaultFloatIfUnset(so, "horizontalSlashTiltAngle", 0f);
        SetDefaultFloatIfUnset(so, "horizontalSlashTotalDuration", 1.6f);

        // Kỹ năng 3: JumpAttackRI (Nhảy dậm đất)
        SetDefaultFloatIfUnset(so, "jumpAttackDamage", 80f);
        SetDefaultFloatIfUnset(so, "jumpAttackCooldown", 5.0f);
        SetDefaultFloatIfUnset(so, "jumpAttackWindupDelay", 0.25f);
        SetDefaultFloatIfUnset(so, "jumpAttackLeapDuration", 0.55f);
        SetDefaultFloatIfUnset(so, "jumpLeapDistance", 7.5f);
        SetDefaultFloatIfUnset(so, "jumpAttackApexHeight", 1.8f);
        SetDefaultFloatIfUnset(so, "jumpAttackRadius", 4.0f);
        SetDefaultFloatIfUnset(so, "jumpAttackRecoveryDuration", 0.85f);

        // Kỹ năng 4: ComboAttackRI (Chém liên hoàn 4 nhát)
        SetDefaultFloatIfUnset(so, "comboHitDamage", 35f);
        SetDefaultFloatIfUnset(so, "comboAttackCooldown", 6.0f);
        SetDefaultFloatIfUnset(so, "comboHitRadius", 3.2f);
        SetDefaultFloatIfUnset(so, "comboRecoveryDuration", 0.90f);

        // Kỹ năng 1: Attack Slash (Chém dọc - dùng IgnisSwordTrail trực tiếp trên kiếm, không dùng vệt spawn ngoài không gian)
        so.FindProperty("attackSlashSwingVfxPrefab").objectReferenceValue = null;
        if (vertSlash != null) so.FindProperty("verticalSlashPrefab").objectReferenceValue = vertSlash;
        if (impactSlash != null) so.FindProperty("attackSlashImpactVfxPrefab").objectReferenceValue = impactSlash;
        if (impactSlash != null) so.FindProperty("slashImpactVfxPrefab").objectReferenceValue = impactSlash;

        // Kỹ năng 2: Horizontal Slash (Chém ngang - dùng IgnisSwordTrail trực tiếp trên kiếm)
        so.FindProperty("horizontalSlashSwingVfxPrefab").objectReferenceValue = null;
        if (horizSlash != null) so.FindProperty("horizontalSlashPrefab").objectReferenceValue = horizSlash;
        if (impactSlash != null) so.FindProperty("horizontalSlashImpactVfxPrefab").objectReferenceValue = impactSlash;

        // Kỹ năng 3 & 4 (Nhảy dậm nổ & Chuỗi Combo)
        if (jumpImpact != null) so.FindProperty("jumpImpactVfxPrefab").objectReferenceValue = jumpImpact;
        so.FindProperty("comboSlashVfxPrefab").objectReferenceValue = null;

        // Tìm weapon socket
        Transform weaponSocketTransform = null;
        Transform[] allChildren = go.GetComponentsInChildren<Transform>(true);
        foreach (var c in allChildren)
        {
            if (c.name == "Hand_R_Weapon")
            {
                weaponSocketTransform = c;
                so.FindProperty("weaponSocket").objectReferenceValue = c;
                break;
            }
        }

        // Cấu hình Ignis_Sword_Flame trên kiếm
        if (weaponSocketTransform != null)
        {
            // Dọn dẹp Fire Flare hoặc Aura Burn thử nghiệm cũ nếu có trên tay
            for (int i = weaponSocketTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = weaponSocketTransform.GetChild(i);
                if (child.name == "Fire Flare" || child.name == "Aura Burn")
                {
                    DestroyImmediate(child.gameObject, true);
                }
            }

            // Dọn dẹp Ignis_Sword_Flame cũ nếu có để cập nhật bản mới không bầu chuôi
            for (int i = weaponSocketTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = weaponSocketTransform.GetChild(i);
                if (child.name == "Ignis_Sword_Flame")
                {
                    DestroyImmediate(child.gameObject, true);
                }
            }

            // Gắn Ignis_Sword_Flame mới
            Transform flameChild = null;
            GameObject swordFlamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Boss/IgnisDo/VFX/Ignis_Sword_Flame.prefab");

            if (swordFlamePrefab != null)
            {
                GameObject flameInstance = (GameObject)PrefabUtility.InstantiatePrefab(swordFlamePrefab, weaponSocketTransform);
                flameInstance.name = "Ignis_Sword_Flame";
                flameInstance.transform.localPosition = Vector3.zero;
                flameInstance.transform.localRotation = Quaternion.identity;
                flameInstance.transform.localScale = Vector3.one;
                flameChild = flameInstance.transform;
            }

            if (flameChild != null)
            {
                flameChild.localPosition = Vector3.zero;
                flameChild.localRotation = Quaternion.identity;
                flameChild.localScale = Vector3.one;

                IgnisSwordFlame flameComp = flameChild.GetComponent<IgnisSwordFlame>();
                if (flameComp != null)
                {
                    flameComp.CacheComponents();
                    flameComp.SetFlame(false, instant: true); // Trước 50% máu: Kiếm tắt lửa mặc định
                    so.FindProperty("weaponFlame").objectReferenceValue = flameComp;
                    so.FindProperty("autoIgniteSwordOnAttack").boolValue = false;
                    so.FindProperty("previewFlameAlwaysOn").boolValue = false;
                    so.FindProperty("swordCastingHealthThreshold").floatValue = 0.50f;
                    so.FindProperty("swordEnrageBonusDamage").floatValue = 10.0f;
                    so.FindProperty("swordCastingIgniteDelay").floatValue = 0.85f;
                    so.FindProperty("swordCastingTotalDuration").floatValue = 2.0f;
                }

                // Gán IgnisSwordTrail gắn trực tiếp trên kiếm
                IgnisSwordTrail trailComp = flameChild.GetComponentInChildren<IgnisSwordTrail>(true);
                if (trailComp != null)
                {
                    trailComp.SetTrailActive(false); // Mặc định tắt, chỉ bật khi xuất chiêu
                    so.FindProperty("weaponTrail").objectReferenceValue = trailComp;
                }
            }
        }

        so.ApplyModifiedProperties();
        bossCtrl.ApplyBossStats();
    }

    private static void SetDefaultFloatIfUnset(SerializedObject so, string propName, float defaultVal)
    {
        var prop = so.FindProperty(propName);
        if (prop != null && prop.floatValue <= 0.001f)
        {
            prop.floatValue = defaultVal;
        }
    }
}

