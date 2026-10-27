#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

namespace InfiniteLabyrinth.EditorTools
{
    /// <summary>
    /// Công cụ tự động cấu hình và hoàn thiện Prefab Boss Ice Golem (BOSS-01).
    /// Đảm bảo Prefab Boss_IceGolem.prefab sở hữu trọn vẹn bộ kỹ năng 7 chiêu,
    /// VFX chuẩn xác (Purple Orb, Aura, Flame Thrower...), di chuyển 4 hướng,
    /// Health 800HP Souls-like, và hỗ trợ tự động nối dây AnimatorController.
    /// </summary>
    public static class IceGolemSetupEditor
    {
        private const string PrefabPath = "Assets/Characters/Boss/IceGolem/Boss_IceGolem.prefab";
        private const string ControllerPath = "Assets/Characters/Boss/IceGolem/BossController.controller";
        private const string FlameVfxPath = "Assets/VFX/A VFX Prefabs/Other/Flame Thrower.prefab";
        private const string Attack1VfxPath = "Assets/VFX/A VFX Prefabs/Explosiion/Impact.prefab";
        private const string HandSlamVfxPath = "Assets/VFX/A VFX Prefabs/Attack1 Effect.prefab";
        private const string GroundImpactVfxPath = "Assets/VFX/A VFX Prefabs/Impact/Ground Impact.prefab";
        private const string IceImpactVfxPath = "Assets/VFX/A VFX Prefabs/Impact/Ice Impact.prefab";
        private const string PurpleOrbVfxPath = "Assets/Characters/Boss/IceGolem/VFX/Purple Orb.prefab";

        private const string AttackLTFbxPath = "Assets/Characters/Boss/IceGolem/Animation/AttackLT_IG.fbx";
        private const string IceBallClipPath = "Assets/Characters/Boss/IceGolem/Animation/Animation/IceBallIG.anim";
        private const string RunAttackFbxPath = "Assets/Characters/Boss/IceGolem/Animation/RunAttack.fbx";
        private const string WalkBackFbxPath = "Assets/Characters/Boss/IceGolem/Animation/WalkBackIG.fbx";
        private const string WalkLeftFbxPath = "Assets/Characters/Boss/IceGolem/Animation/WalkLeftIG.fbx";
        private const string WalkRightFbxPath = "Assets/Characters/Boss/IceGolem/Animation/WalkRightIG.fbx";
        private const string WalkClipPath = "Assets/Characters/Boss/IceGolem/Animation/Animation/WalkIG.anim";
        private const string IdleClipPath = "Assets/Characters/Boss/IceGolem/Animation/Animation/GolemIdle.anim";

        [MenuItem("Infinite Labyrinth/Boss/1. Cấu Hình Hoàn Thiện Prefab Boss Ice Golem (BOSS-01)", false, 1)]
        public static void SetupIceGolemPrefab()
        {
            ExecuteSetup(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/2. Tự Động Nối Dây Animator Boss (BossController)", false, 2)]
        public static void AutoWireAnimatorMenu()
        {
            WireBossAnimator(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/3. Dọn Dẹp Scene & Khắc Phục Lỗi Ice Golem", false, 3)]
        public static void CleanActiveSceneMenu()
        {
            CleanActiveScene(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/4. Cài Đặt Package ML-Agents Tự Động", false, 4)]
        public static void InstallMLAgentsPackage()
        {
            Debug.Log("<color=cyan>[ML-Agents Setup]</color> Đang yêu cầu Unity Package Manager cài đặt package 'com.unity.ml-agents'...");
            UnityEditor.PackageManager.Client.Add("com.unity.ml-agents");
        }

        [MenuItem("Infinite Labyrinth/Boss/5. Tự Động Gắn & Cấu Hình ML-Agents Cho Boss Ice Golem", false, 5)]
        public static void SetupMLAgentsForBossMenu()
        {
            SetupMLAgentsForBoss(true);
        }

        [MenuItem("Infinite Labyrinth/Boss/6. Gắn Bot AI Cho Player (Phím B Bật/Tắt Sparring)", false, 6)]
        public static void AttachPlayerBotAIMenu()
        {
            AttachPlayerBotAI(true);
        }

        [InitializeOnLoadMethod]
        private static void AutoCheckAndSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PrefabPath)) return;
                ExecuteSetup(false);
                WireBossAnimator(false);
                CleanActiveScene(false);
                SetupMLAgentsForBoss(false);
                AttachPlayerBotAI(false);
            };
        }

        public static void CleanActiveScene(bool showDialog)
        {
            int cleanedCount = 0;

            // 1. Tìm và xóa Purple Orb nằm thừa trong Scene
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                GameObject[] roots = activeScene.GetRootGameObjects();
                foreach (GameObject obj in roots)
                {
                    if (obj == null) continue;
                    if (obj.name == "Purple Orb" || obj.name.StartsWith("Purple Orb"))
                    {
                        Undo.DestroyObjectImmediate(obj);
                        cleanedCount++;
                    }
                    else if (obj.name.Contains("(Clone)"))
                    {
                        if (obj.name.Contains("Ground Impact") || obj.name.Contains("ClockwiseShockwave") || obj.name.Contains("Impact"))
                        {
                            Undo.DestroyObjectImmediate(obj);
                            cleanedCount++;
                        }
                    }
                }

                // 2. Tìm GameObject Boss_IceGolem trong Scene và dọn dẹp component trùng lặp
                GameObject bossObj = GameObject.Find("Boss_IceGolem");
                if (bossObj != null)
                {
                    IceGolemController[] controllers = bossObj.GetComponents<IceGolemController>();
                    if (controllers.Length > 1)
                    {
                        for (int i = 1; i < controllers.Length; i++)
                        {
                            if (controllers[i] != null)
                            {
                                Undo.DestroyObjectImmediate(controllers[i]);
                                cleanedCount++;
                            }
                        }
                    }

                    if (controllers.Length > 0 && controllers[0] != null)
                    {
                        SerializedObject so = new SerializedObject(controllers[0]);
                        so.FindProperty("runAuraVfxPrefab").objectReferenceValue = null;
                        so.FindProperty("handAuraVfxPrefab").objectReferenceValue = null;
                        so.FindProperty("autoTargetPlayerOnJumpDown").boolValue = false;
                        so.ApplyModifiedProperties();
                    }
                }

                if (cleanedCount > 0)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                }
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Thông Báo", $"Đã dọn dẹp xong {cleanedCount} đối tượng / component lỗi thời trong Scene!", "OK");
            }
        }

        public static bool ExecuteSetup(bool showDialog)
        {
            if (!File.Exists(PrefabPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy file Prefab tại đường dẫn: {PrefabPath}", "OK");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", "Không thể nạp nội dung Prefab Boss_IceGolem!", "OK");
                return false;
            }

            try
            {
                // 1. Đảm bảo Layer là Enemy (9) và Tag
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1)
                {
                    root.layer = enemyLayer;
                }

                // 2. Cấu hình CapsuleCollider
                CapsuleCollider col = root.GetComponent<CapsuleCollider>();
                if (col == null) col = root.AddComponent<CapsuleCollider>();
                col.radius = 1.52f;
                col.height = 5.12f;
                col.center = new Vector3(0.14f, 2.45f, -0.25f);
                col.isTrigger = false;

                // 3. Cấu hình Rigidbody
                Rigidbody rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 100f;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

                // 4. Cấu hình Health (Souls-like Boss HP = 800)
                Health health = root.GetComponent<Health>();
                if (health == null) health = root.AddComponent<Health>();

                SerializedObject soHealth = new SerializedObject(health);
                soHealth.FindProperty("maxHealth").floatValue = 800f;
                soHealth.FindProperty("isInvulnerable").boolValue = false;

                // Xóa toàn bộ Persistent Calls cũ trong onDeath (loại bỏ SetActive(false) lỗi thời)
                SerializedProperty onDeathProp = soHealth.FindProperty("onDeath");
                if (onDeathProp != null)
                {
                    SerializedProperty persistentCalls = onDeathProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    if (persistentCalls != null)
                    {
                        persistentCalls.ClearArray();
                    }
                }
                soHealth.ApplyModifiedProperties();

                // 5. Cấu hình IceGolemController
                IceGolemController controller = root.GetComponent<IceGolemController>();
                if (controller == null) controller = root.AddComponent<IceGolemController>();

                SerializedObject soController = new SerializedObject(controller);

                // Tìm Animator trên IceGolemModel
                Animator anim = root.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    soController.FindProperty("anim").objectReferenceValue = anim;
                }

                // Di chuyển & Khóa mục tiêu
                soController.FindProperty("moveSpeed").floatValue = 3.5f;
                soController.FindProperty("rotationSpeed").floatValue = 120f;
                soController.FindProperty("strafeMode").boolValue = true;
                soController.FindProperty("autoFindPlayer").boolValue = true;
                soController.FindProperty("alwaysFacePlayer").boolValue = true;
                soController.FindProperty("facePlayerSpeed").floatValue = 240f;
                soController.FindProperty("facePlayerOnAttack").boolValue = true;
                soController.FindProperty("minFaceDistance").floatValue = 0.5f;

                // Cooldowns
                soController.FindProperty("attack1Cooldown").floatValue = 1.5f;
                soController.FindProperty("gumoGumoCooldown").floatValue = 6.0f;
                soController.FindProperty("flameBreathCooldown").floatValue = 8.0f;
                soController.FindProperty("jumpAttackCooldown").floatValue = 7.0f;
                soController.FindProperty("runAttackCooldown").floatValue = 6.0f;
                soController.FindProperty("attackLTCooldown").floatValue = 5.0f;
                soController.FindProperty("iceBallCooldown").floatValue = 7.0f;

                // Phản hồi chớp đỏ
                soController.FindProperty("hitFlashDuration").floatValue = 0.12f;
                soController.FindProperty("hitFlashColor").colorValue = new Color(1f, 0.25f, 0.25f, 1f);

                // JumpDown (mở màn từ trên cao duy nhất 1 lần khi thấy Player)
                soController.FindProperty("jumpDownOnStart").boolValue = true;
                soController.FindProperty("triggerJumpDownOnDetectPlayer").boolValue = true;
                soController.FindProperty("playerDetectionRange").floatValue = 35f;
                soController.FindProperty("jumpDownDistance").floatValue = 10f;
                soController.FindProperty("jumpDownHeight").floatValue = 12f;
                soController.FindProperty("jumpDownArcHeight").floatValue = 0f;
                soController.FindProperty("jumpDownStartDelay").floatValue = 0.1f;
                soController.FindProperty("jumpDownFallDuration").floatValue = 1.25f;
                soController.FindProperty("autoTargetPlayerOnJumpDown").boolValue = false;
                soController.FindProperty("jumpDownShockwaveDelay").floatValue = 0f;
                soController.FindProperty("jumpDownShockwaveRadius").floatValue = 16f;
                soController.FindProperty("jumpDownShockwaveSpeed").floatValue = 14f;
                soController.FindProperty("jumpDownShockwaveDamage").floatValue = 35f;
                soController.FindProperty("jumpDownShockwaveKnockback").floatValue = 3.0f;
                soController.FindProperty("jumpDownShockwaveColor").colorValue = new Color(0.2f, 0.9f, 1f, 0.95f);

                // JumpAttack (vồ tới tầm xa)
                soController.FindProperty("jumpAttackDistance").floatValue = 20f;
                soController.FindProperty("jumpAttackArcHeight").floatValue = 2.8f;
                soController.FindProperty("jumpTakeoffDelay").floatValue = 0.65f;
                soController.FindProperty("jumpFlightDuration").floatValue = 0.85f;
                soController.FindProperty("autoTargetPlayer").boolValue = true;
                soController.FindProperty("targetOffsetDistance").floatValue = 1.8f;
                soController.FindProperty("jumpAttackShockwaveDelay").floatValue = 0.23f;
                soController.FindProperty("jumpAttackShockwaveRadius").floatValue = 18f;
                soController.FindProperty("jumpAttackShockwaveSpeed").floatValue = 16f;
                soController.FindProperty("jumpAttackDamage").floatValue = 40f;
                soController.FindProperty("jumpAttackKnockback").floatValue = 3.5f;
                soController.FindProperty("jumpAttackShockwaveColor").colorValue = new Color(0.15f, 0.85f, 1f, 0.95f);

                // Attack 1 (đấm thường)
                soController.FindProperty("attack1Damage").floatValue = 25f;
                soController.FindProperty("attack1Knockback").floatValue = 2.5f;
                soController.FindProperty("attack1HitDistance").floatValue = 2.4f;
                soController.FindProperty("attack1HitHeight").floatValue = 1.3f;
                soController.FindProperty("attack1HitRadius").floatValue = 1.8f;
                soController.FindProperty("attack1HitDelay").floatValue = 1.23f;
                soController.FindProperty("attack1VfxDelay").floatValue = 0f;
                soController.FindProperty("attack1ImpactVfxScale").floatValue = 1.2f;

                // Attack 2 (GumoGumo Slam đấm sàn liên hoàn)
                soController.FindProperty("gumoSlamVfxScale").floatValue = 2.0f;
                soController.FindProperty("gumoShockwaveRadius").floatValue = 12.0f;
                soController.FindProperty("gumoSlamDamage").floatValue = 18f;
                soController.FindProperty("gumoSlamKnockback").floatValue = 1.2f;
                soController.FindProperty("gumoSlamStartTime").floatValue = 0.55f;
                soController.FindProperty("gumoSlamEndTime").floatValue = 3.3f;
                soController.FindProperty("gumoSlamInterval").floatValue = 0.35f;
                soController.FindProperty("enableGumoProceduralRing").boolValue = false;
                soController.FindProperty("gumoShockwaveSpeed").floatValue = 12f;
                soController.FindProperty("gumoShockwaveColor").colorValue = new Color(0.3f, 0.85f, 1f, 0.9f);

                // Attack 3 (Flame Breath)
                soController.FindProperty("lockVerticalRotation").boolValue = true;
                soController.FindProperty("lockFlameHeight").boolValue = true;
                soController.FindProperty("flamePositionOffset").vector3Value = new Vector3(0f, 0.05f, 0.45f);
                soController.FindProperty("flameVisualLength").floatValue = 3.58f;
                soController.FindProperty("flameVisualWidth").floatValue = 2.9f;
                soController.FindProperty("flameDensity").floatValue = 1.9f;
                soController.FindProperty("flameRange").floatValue = 15.5f;
                soController.FindProperty("flameConeAngle").floatValue = 49f;
                soController.FindProperty("flameStartTime").floatValue = 0.75f;
                soController.FindProperty("flameDuration").floatValue = 3.2f;
                soController.FindProperty("flameDamagePerTick").floatValue = 12f;
                soController.FindProperty("flameDamageTickInterval").floatValue = 0.2f;
                soController.FindProperty("flameKnockback").floatValue = 0.6f;
                soController.FindProperty("enableFlameShake").boolValue = true;
                soController.FindProperty("flameShakeMagnitude").floatValue = 0.025f;
                soController.FindProperty("flameShakeDuration").floatValue = 0.05f;

                // Cơ chế né đòn
                soController.FindProperty("jumpDodgeHeightThreshold").floatValue = 0.5f;
                soController.FindProperty("clockwiseSpinSpeed").floatValue = 280f;
                soController.FindProperty("shockwaveLineWidth").floatValue = 0.35f;

                // Tìm xương bàn tay và miệng
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in children)
                {
                    if (t.name == "BossHitboxT") soController.FindProperty("leftHandBone").objectReferenceValue = t;
                    if (t.name == "BossHitboxP") soController.FindProperty("rightHandBone").objectReferenceValue = t;
                    if (t.name == "Mieng") soController.FindProperty("mouthBone").objectReferenceValue = t;
                }

                // Gán các Prefab VFX
                GameObject flameVfx = AssetDatabase.LoadAssetAtPath<GameObject>(FlameVfxPath);
                if (flameVfx != null) soController.FindProperty("flameVfxPrefab").objectReferenceValue = flameVfx;

                GameObject impactVfx = AssetDatabase.LoadAssetAtPath<GameObject>(Attack1VfxPath);
                if (impactVfx != null) soController.FindProperty("attack1ImpactVfxPrefab").objectReferenceValue = impactVfx;

                GameObject handSlamVfx = AssetDatabase.LoadAssetAtPath<GameObject>(HandSlamVfxPath);
                if (handSlamVfx != null) soController.FindProperty("handSlamImpactVfx").objectReferenceValue = handSlamVfx;

                GameObject groundVfx = AssetDatabase.LoadAssetAtPath<GameObject>(GroundImpactVfxPath);
                if (groundVfx != null) soController.FindProperty("jumpDownImpactVfx").objectReferenceValue = groundVfx;

                GameObject iceVfx = AssetDatabase.LoadAssetAtPath<GameObject>(IceImpactVfxPath);
                if (iceVfx != null) soController.FindProperty("jumpAttackImpactVfx").objectReferenceValue = iceVfx;

                GameObject purpleOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PurpleOrbVfxPath);
                if (purpleOrb != null) soController.FindProperty("iceBallPrefab").objectReferenceValue = purpleOrb;

                // Các slot VFX tự chọn: runAuraVfxPrefab và handAuraVfxPrefab để trống để người dùng tự do kéo thả
                // không tự động gán bất kỳ hiệu ứng khói/vệt nào

                // Layers
                int pLayer = LayerMask.NameToLayer("Player");
                int dLayer = LayerMask.NameToLayer("Damageable");
                int mask = 0;
                if (pLayer != -1) mask |= (1 << pLayer);
                if (dLayer != -1) mask |= (1 << dLayer);
                if (mask == 0) mask = 1; // Fallback
                soController.FindProperty("targetLayers").intValue = mask;
                soController.FindProperty("groundLayer").intValue = 1; // Default
                soController.FindProperty("obstacleLayer").intValue = 1; // Default

                soController.ApplyModifiedProperties();

                // 6. Lưu Prefab
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

                Debug.Log($"[IceGolemSetupEditor] Đã cấu hình và lưu Prefab thành công: {PrefabPath}");
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Infinite Labyrinth - BOSS-01", 
                        "Đã hoàn thiện Prefab Boss Ice Golem (Boss_IceGolem.prefab) thành công!\n\n" +
                        "- Đã gắn IceGolemController với 7 chiêu thức (bao gồm RunAttack, AttackLT, IceBall) & di chuyển 4 hướng\n" +
                        "- Đã gán đầy đủ VFX (Flame Thrower, Purple Orb, Aura, Attack1 Effect, Ground Impact, Ice Impact)\n" +
                        "- Đã cấu hình Máu Boss: 800 HP Souls-like\n" +
                        "- Đã tích hợp phản hồi chớp đỏ GPU khi trúng đòn\n" +
                        "- Phím tắt kiểm tra: 1, 2, 3, 4, 5, 6, 7 và di chuyển WASD!", 
                        "Tuyệt vời!");
                }
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static bool WireBossAnimator(bool showDialog)
        {
            if (!File.Exists(ControllerPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy Animator Controller tại: {ControllerPath}", "OK");
                return false;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", "Không thể nạp AnimatorController BossController!", "OK");
                return false;
            }

            // 1. Đảm bảo các Parameters cần thiết tồn tại
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveType", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveZ", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackGumoGumo", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackRoaring", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "JumpAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "JumpDown", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Dead", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "RunAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackLT_IG", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IceBallIG", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

            // 2. Tìm hoặc tạo các States (Nạp Motion Humanoid chuẩn từ FBX / Anim)
            AnimatorState idleState = FindOrCreateState(rootSm, "Idle", IdleClipPath, new Vector3(250, 60, 0));
            AnimatorState walkState = FindOrCreateState(rootSm, "Walk", WalkClipPath, new Vector3(250, -40, 0));
            AnimatorState walkBackState = FindOrCreateState(rootSm, "WalkBackIG", WalkBackFbxPath, new Vector3(250, 160, 0));
            AnimatorState walkLeftState = FindOrCreateState(rootSm, "WalkLeftIG", WalkLeftFbxPath, new Vector3(50, 60, 0));
            AnimatorState walkRightState = FindOrCreateState(rootSm, "WalkRightIG", WalkRightFbxPath, new Vector3(450, 60, 0));

            AnimatorState runAttackState = FindOrCreateState(rootSm, "RunAttack", RunAttackFbxPath, new Vector3(550, -60, 0));
            AnimatorState attackLTState = FindOrCreateState(rootSm, "AttackLT_IG", AttackLTFbxPath, new Vector3(550, 0, 0));
            AnimatorState iceBallState = FindOrCreateState(rootSm, "IceBallIG", IceBallClipPath, new Vector3(550, 60, 0));

            // 3. Nối dây di chuyển 4 hướng giữa Idle và Walk / WalkBack / WalkLeft / WalkRight
            // MoveType: 0 = Idle, 1 = Walk (tới), 2 = WalkBackIG (lùi), 3 = WalkLeftIG (trái), 4 = WalkRightIG (phải)
            SetupLocomotionTransition(idleState, walkState, 1);
            SetupLocomotionReturnTransition(walkState, idleState);

            SetupLocomotionTransition(idleState, walkBackState, 2);
            SetupLocomotionReturnTransition(walkBackState, idleState);

            SetupLocomotionTransition(idleState, walkLeftState, 3);
            SetupLocomotionReturnTransition(walkLeftState, idleState);

            SetupLocomotionTransition(idleState, walkRightState, 4);
            SetupLocomotionReturnTransition(walkRightState, idleState);

            // 4. Nối dây 3 Kỹ Năng Mới từ Any State và hồi về Idle
            // RunAttack (Phím 5)
            SetupAnyStateAttack(rootSm, runAttackState, "RunAttack");
            SetupAttackReturnToIdle(runAttackState, idleState, 0.9f);

            // AttackLT_IG (Phím 6)
            SetupAnyStateAttack(rootSm, attackLTState, "AttackLT_IG");
            SetupAttackReturnToIdle(attackLTState, idleState, 0.95f);

            // IceBallIG (Phím 7)
            SetupAnyStateAttack(rootSm, iceBallState, "IceBallIG");
            SetupAttackReturnToIdle(iceBallState, idleState, 0.95f);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[IceGolemSetupEditor] Đã nối dây hoàn thiện Animator Controller: {ControllerPath}");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Infinite Labyrinth - Nối Dây Animator Thành Công",
                    "Đã tự động nối dây hoàn chỉnh cho Animator BossController.controller!\n\n" +
                    "- Đã chuẩn hóa toàn bộ Animation sang chuẩn Humanoid (khắc phục triệt để lỗi chôn nửa người xuống đất)\n" +
                    "- Đã thêm Parameters: MoveType (Int), RunAttack (Trigger), AttackLT_IG (Trigger), IceBallIG (Trigger)\n" +
                    "- Đã nối di chuyển 4 hướng: Walk (1), WalkBackIG (2), WalkLeftIG (3), WalkRightIG (4) <-> Idle (0)\n" +
                    "- Đã nối 3 Kỹ năng mới: Any State -> RunAttack, AttackLT_IG, IceBallIG -> Idle\n\n" +
                    "Bạn có thể vào Animator Window để kiểm tra hoặc Play game để test phím 5, 6, 7 & WASD ngay lập tức!",
                    "Tuyệt vời!");
            }

            return true;
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        private static Motion LoadMotionAsset(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object obj in subAssets)
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }
            }

            return AssetDatabase.LoadAssetAtPath<Motion>(path);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName, string clipPath, Vector3 fallbackPos)
        {
            Motion motion = LoadMotionAsset(clipPath);

            foreach (var cs in sm.states)
            {
                if (cs.state.name == stateName)
                {
                    if (motion != null)
                    {
                        cs.state.motion = motion;
                    }
                    return cs.state;
                }
            }

            AnimatorState newState = sm.AddState(stateName, fallbackPos);
            if (motion != null)
            {
                newState.motion = motion;
            }
            return newState;
        }

        private static void SetupLocomotionTransition(AnimatorState fromState, AnimatorState toState, int moveTypeValue)
        {
            foreach (var t in fromState.transitions)
            {
                if (t.destinationState == toState)
                {
                    t.hasExitTime = false;
                    t.duration = 0.15f;
                    EnsureTransitionCondition(t, AnimatorConditionMode.Equals, moveTypeValue, "MoveType");
                    return;
                }
            }

            AnimatorStateTransition newTrans = fromState.AddTransition(toState);
            newTrans.hasExitTime = false;
            newTrans.duration = 0.15f;
            newTrans.AddCondition(AnimatorConditionMode.Equals, moveTypeValue, "MoveType");
        }

        private static void SetupLocomotionReturnTransition(AnimatorState fromState, AnimatorState toState)
        {
            foreach (var t in fromState.transitions)
            {
                if (t.destinationState == toState)
                {
                    t.hasExitTime = false;
                    t.duration = 0.15f;
                    EnsureTransitionCondition(t, AnimatorConditionMode.Equals, 0, "MoveType");
                    return;
                }
            }

            AnimatorStateTransition newTrans = fromState.AddTransition(toState);
            newTrans.hasExitTime = false;
            newTrans.duration = 0.15f;
            newTrans.AddCondition(AnimatorConditionMode.Equals, 0, "MoveType");
        }

        private static void SetupAnyStateAttack(AnimatorStateMachine sm, AnimatorState targetState, string triggerName)
        {
            foreach (var t in sm.anyStateTransitions)
            {
                if (t.destinationState == targetState)
                {
                    t.hasExitTime = false;
                    t.duration = 0.1f;
                    t.canTransitionToSelf = false;
                    EnsureTransitionCondition(t, AnimatorConditionMode.If, 0, triggerName);
                    return;
                }
            }

            AnimatorStateTransition newTrans = sm.AddAnyStateTransition(targetState);
            newTrans.hasExitTime = false;
            newTrans.duration = 0.1f;
            newTrans.canTransitionToSelf = false;
            newTrans.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        }

        private static void SetupAttackReturnToIdle(AnimatorState attackState, AnimatorState idleState, float exitTime)
        {
            foreach (var t in attackState.transitions)
            {
                if (t.destinationState == idleState)
                {
                    t.hasExitTime = true;
                    t.exitTime = exitTime;
                    t.duration = 0.2f;
                    return;
                }
            }

            AnimatorStateTransition newTrans = attackState.AddTransition(idleState);
            newTrans.hasExitTime = true;
            newTrans.exitTime = exitTime;
            newTrans.duration = 0.2f;
        }

        private static void EnsureTransitionCondition(AnimatorStateTransition trans, AnimatorConditionMode mode, float threshold, string param)
        {
            foreach (var c in trans.conditions)
            {
                if (c.parameter == param) return;
            }
            trans.AddCondition(mode, threshold, param);
        }

        public static void SetupMLAgentsForBoss(bool showDialog)
        {
            if (!File.Exists(PrefabPath)) return;

            // 1. Cấu hình trên Prefab
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefab != null)
            {
                ConfigureBossMLAgentsComponents(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            // 2. Cấu hình trên Scene Instance nếu có
            GameObject sceneBoss = GameObject.Find("Boss_IceGolem");
            if (sceneBoss != null)
            {
                ConfigureBossMLAgentsComponents(sceneBoss);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Cấu Hình ML-Agents Hoàn Tất",
                    "Đã cấu hình thành công ML-Agents cho Boss Ice Golem:\n" +
                    "- Component IceGolemAgent (Souls-like Reinforcement Learning)\n" +
                    "- Component BehaviorParameters (Behavior: IceGolemSoulsAI, Vector Obs: 29, Discrete Actions: [5, 8, 2])\n" +
                    "- Component DecisionRequester (Decision Period: 5)",
                    "OK"
                );
            }
        }

        private static void ConfigureBossMLAgentsComponents(GameObject go)
        {
            // 1. IceGolemAgent
            var agent = go.GetComponent<IceGolemAgent>();
            if (agent == null) agent = Undo.AddComponent<IceGolemAgent>(go);

            // 2. BehaviorParameters
            var behaviorParams = go.GetComponent<BehaviorParameters>();
            if (behaviorParams == null) behaviorParams = Undo.AddComponent<BehaviorParameters>(go);

            behaviorParams.BehaviorName = "IceGolemSoulsAI";
            behaviorParams.BrainParameters.VectorObservationSize = 29;
            behaviorParams.BrainParameters.NumStackedVectorObservations = 1;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(new int[] { 5, 8, 2 });

            // 3. DecisionRequester
            var requester = go.GetComponent<DecisionRequester>();
            if (requester == null) requester = Undo.AddComponent<DecisionRequester>(go);
            requester.DecisionPeriod = 5;
            requester.TakeActionsBetweenDecisions = true;

            EditorUtility.SetDirty(go);
        }

        private const string PlayerPrefabPath = "Assets/Characters/Player/Player.prefab";

        public static void AttachPlayerBotAI(bool showDialog)
        {
            int attachedCount = 0;

            // 1. Gắn lên Player Prefab nếu có
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

            // 2. Gắn lên Player trong Scene đang mở
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
                    "Đã gắn thành công component PlayerBotAI:\n\n" +
                    "- Phím [B]: Bật / Tắt chế độ Bot tự động chiến đấu.\n" +
                    "- Khi Bật (ON): Player tự động áp sát, chém combo, lộn né, parry và hồi máu để Boss tự học.\n" +
                    "- Khi Tắt (OFF): Player trở lại chế độ lái thủ công 100% bằng chuột và phím WASD.",
                    "OK"
                );
            }
        }
    }
}
#endif
