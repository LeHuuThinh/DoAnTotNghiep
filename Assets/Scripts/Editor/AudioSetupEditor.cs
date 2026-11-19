#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bộ công cụ tự động hóa 1-Click thiết lập AudioManager và liên kết toàn bộ 40+ AudioClips từ Assets/Audio (Task POL-02).
/// </summary>
public static class AudioSetupEditor
{
    private const string PREFAB_PATH = "Assets/Prefabs/Audio/AudioManager.prefab";

    [MenuItem("Infinite Labyrinth/Audio/1. Thiết Lập AudioManager & Nối Âm Thanh (1-Click)", false, 30)]
    public static void SetupAudioManagerOneClick()
    {
        Debug.Log("<color=cyan>[AudioSetupEditor]</color> Bắt đầu thiết lập hệ thống AudioManager...");

        // 1. Đảm bảo thư mục tồn tại
        EnsureDirectoryExists("Assets/Prefabs/Audio");
        EnsureDirectoryExists("Assets/Resources");

        // 2. Tạo hoặc nạp GameObject tạm thời để cấu hình
        GameObject tempObj = new GameObject("[AudioManager]");
        AudioManager audioManager = tempObj.AddComponent<AudioManager>();
        SerializedObject so = new SerializedObject(audioManager);

        // Thiết lập âm lượng mặc định chuẩn
        so.FindProperty("masterVolume").floatValue = 1.0f;
        so.FindProperty("bgmVolume").floatValue = 0.1f;
        so.FindProperty("sfxVolume").floatValue = 0.5f;

        // 3. Liên kết BGM Tracks
        SetAudioClipArray(so, "landBgmTracks", new string[] {
            "Assets/Audio/BGM/LandBGM1.wav",
            "Assets/Audio/BGM/LandBGM2.wav",
            "Assets/Audio/BGM/LandBGM3.wav"
        });

        SetAudioClipArray(so, "dungeonBgmTracks", new string[] {
            "Assets/Audio/BGM/DungeonBGM1.wav",
            "Assets/Audio/BGM/DungeonBGM2.wav"
        });

        SetAudioClipArray(so, "combatBgmTracks", new string[] {
            "Assets/Audio/BGM/CombatBGM1.wav",
            "Assets/Audio/BGM/CombatBGM2.wav"
        });

        SetAudioClipArray(so, "bossBgmTracks", new string[] {
            "Assets/Audio/BGM/BossBGM.wav",
            "Assets/Audio/BGM/BossBGM1.wav",
            "Assets/Audio/BGM/BossBGM2.wav",
            "Assets/Audio/BGM/BossBGM3.wav"
        });

        // 4. Liên kết Player SFX (2 đòn chém cơ bản Slash1, Slash2)
        SetAudioClipArray(so, "slashClips", new string[] {
            "Assets/Audio/Player/CombatSound/Slash1.wav",
            "Assets/Audio/Player/CombatSound/Slash2.wav"
        });

        SetAudioClipArray(so, "attackVoiceClips", new string[] {
            "Assets/Audio/Player/PlayerSound/PlayerAttack1.wav",
            "Assets/Audio/Player/PlayerSound/PlayerAttack2.wav",
            "Assets/Audio/Player/PlayerSound/PlayerAttack3.wav",
            "Assets/Audio/Player/PlayerSound/PlayerAttack4.wav"
        });

        SetAudioClipArray(so, "hurtVoiceClips", new string[] {
            "Assets/Audio/Player/PlayerSound/PlayerHurt1.wav",
            "Assets/Audio/Player/PlayerSound/PlayerHurt2.wav",
            "Assets/Audio/Player/PlayerSound/PlayerHurt3.wav",
            "Assets/Audio/Player/PlayerSound/PlayerHurt4.wav",
            "Assets/Audio/Player/PlayerSound/PlayerHurt5.wav"
        });

        SetAudioClip(so, "parryClip", "Assets/Audio/Player/CombatSound/Parry.wav");
        SetAudioClip(so, "counterAttackClip", "Assets/Audio/Player/CombatSound/CounterAttack.wav");
        SetAudioClip(so, "outOfStaminaClip", "Assets/Audio/Player/PlayerSound/PlayerOutOfStamina.wav");
        SetAudioClip(so, "staminaEmptyAlertClip", "Assets/Audio/UI/PressDeep.wav");
        SetAudioClip(so, "deathClip", "Assets/Audio/Player/PlayerSound/PlayerDeath.wav");
        SetAudioClip(so, "healClip", "Assets/Audio/Player/CombatSound/Healing.wav");

        // Liên kết Skill SFX (Charge & Impact)
        SetAudioClip(so, "skillEChargeClip", "Assets/Audio/Player/PlayerSound/SmallCharge.wav");
        SetAudioClip(so, "skillQChargeClip", "Assets/Audio/Player/PlayerSound/MediumCharge.wav");
        SetAudioClip(so, "skillRChargeClip", "Assets/Audio/Player/PlayerSound/BigCharge.wav");
        SetAudioClip(so, "skillEImpactClip", "Assets/Audio/Player/CombatSound/EImpact.wav");
        SetAudioClip(so, "skillQImpactClip", "Assets/Audio/Player/CombatSound/QImpact.wav");
        SetAudioClip(so, "skillRImpactClip", "Assets/Audio/Player/CombatSound/RImpact.wav");

        // Liên kết Idle SFX
        SetAudioClipArray(so, "idleCheckingClips", new string[] {
            "Assets/Audio/Player/IdleSound/Checking1.wav",
            "Assets/Audio/Player/IdleSound/Checking2.wav",
            "Assets/Audio/Player/IdleSound/Checking3.wav",
            "Assets/Audio/Player/IdleSound/Checking4.wav",
            "Assets/Audio/Player/IdleSound/Checking5.wav"
        });

        SetAudioClipArray(so, "idleWarmupClips", new string[] {
            "Assets/Audio/Player/IdleSound/Warmup1.wav",
            "Assets/Audio/Player/IdleSound/Warmup2.wav",
            "Assets/Audio/Player/IdleSound/Warmup3.wav",
            "Assets/Audio/Player/IdleSound/Warmup4.wav",
            "Assets/Audio/Player/IdleSound/Warmup5.wav",
            "Assets/Audio/Player/IdleSound/Warmup6.wav"
        });

        SetAudioClipArray(so, "idleHumClips", new string[] {
            "Assets/Audio/Player/IdleSound/Hums1.mp3",
            "Assets/Audio/Player/IdleSound/Hums2.mp3"
        });

        SetAudioClip(so, "idleStretchingClip", "Assets/Audio/Player/IdleSound/Streching.mp3");

        // Footsteps
        SetAudioClipArray(so, "footstepsDirt", new string[] {
            "Assets/Audio/Footsteps - Essentials/Footsteps_DirtyGround/Footsteps_DirtyGround_Run/Footsteps_DirtyGround_Run_01.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_DirtyGround/Footsteps_DirtyGround_Run/Footsteps_DirtyGround_Run_02.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_DirtyGround/Footsteps_DirtyGround_Run/Footsteps_DirtyGround_Run_03.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_DirtyGround/Footsteps_DirtyGround_Run/Footsteps_DirtyGround_Run_04.wav"
        });

        SetAudioClipArray(so, "footstepsRock", new string[] {
            "Assets/Audio/Footsteps - Essentials/Footsteps_Rock/Footsteps_Rock_Run/Footsteps_Rock_Run_01.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_Rock/Footsteps_Rock_Run/Footsteps_Rock_Run_02.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_Rock/Footsteps_Rock_Run/Footsteps_Rock_Run_03.wav",
            "Assets/Audio/Footsteps - Essentials/Footsteps_Rock/Footsteps_Rock_Run/Footsteps_Rock_Run_04.wav"
        });

        // 5. Liên kết Monster SFX
        SetAudioClip(so, "meleeAttackClip", "Assets/Audio/Monster/SkeMeeleAttack.wav");
        SetAudioClip(so, "meleeHitClip", "Assets/Audio/Monster/SkeMeeleHit.wav");
        SetAudioClip(so, "arrowShootClip", "Assets/Audio/Monster/SkeArrowShoot.wav");
        SetAudioClip(so, "arrowHitClip", "Assets/Audio/Monster/SkeArrowHit.wav");
        SetAudioClip(so, "spellCastClip", "Assets/Audio/Monster/SkeSpellCast.wav");
        SetAudioClip(so, "spellImpactClip", "Assets/Audio/Monster/SkeSpellImpact.wav");
        SetAudioClip(so, "skeletonGetHitClip", "Assets/Audio/Monster/SkeletonGetHit.mp3");
        SetAudioClip(so, "skeletonDeathClip", "Assets/Audio/Monster/SkeletonDeath.mp3");

        // 6. Liên kết Environment SFX
        SetAudioClipArray(so, "ambientClips", new string[] {
            "Assets/Audio/Enviromemt/AmbientSound.wav",
            "Assets/Audio/Enviromemt/AmbientSound2.wav"
        });

        SetAudioClipArray(so, "barrelBreakClips", new string[] {
            "Assets/Audio/Enviromemt/BarrelBreak.wav",
            "Assets/Audio/Enviromemt/BarrelBreak2.wav"
        });

        SetAudioClip(so, "bellClip", "Assets/Audio/Enviromemt/BellSFX.wav");
        SetAudioClip(so, "doorOpenClip", "Assets/Audio/Enviromemt/DoorOpen.wav");
        SetAudioClip(so, "bossDoorOpenClip", "Assets/Audio/Enviromemt/BossDoorOpen.wav");
        SetAudioClip(so, "chestOpenClip", "Assets/Audio/Enviromemt/ChestOpen.wav");
        SetAudioClip(so, "chestAuraClip", "Assets/Audio/Enviromemt/ChestAura.wav");

        SetAudioClipArray(so, "coinPickupClips", new string[] {
            "Assets/Audio/Enviromemt/Coin.wav",
            "Assets/Audio/Enviromemt/Coin2.wav"
        });

        SetAudioClip(so, "portalClip", "Assets/Audio/Enviromemt/PortalSFX.wav");
        SetAudioClip(so, "torchClip", "Assets/Audio/Enviromemt/Torch.wav");

        // 7. Liên kết UI SFX
        SetAudioClip(so, "anvilClip", "Assets/Audio/UI/AnvilSFX.wav");
        SetAudioClip(so, "equipClip", "Assets/Audio/UI/Equip.wav");
        SetAudioClip(so, "potionClip", "Assets/Audio/UI/PotionUse.wav");
        SetAudioClip(so, "shopClip", "Assets/Audio/UI/ShopSFX.wav");
        SetAudioClip(so, "itemPickupClip", "Assets/Audio/UI/ItemPickup.wav");
        SetAudioClip(so, "uiInteractClip", "Assets/Audio/UI/UIInteract.wav");

        so.ApplyModifiedPropertiesWithoutUndo();

        // 8. Lưu thành Prefab tái sử dụng
        PrefabUtility.SaveAsPrefabAsset(tempObj, PREFAB_PATH);

        // 9. Đồng bộ vào CoreSystems.prefab (Resources và Prefabs) và Scene
        SyncToCoreSystemsPrefabs(audioManager);

        // Hủy object tạm
        Object.DestroyImmediate(tempObj);

        // 10. Tự động gắn SpatialTorchAudio lên các ngọn đuốc
        SetupTorchPrefabs();

        // 11. Tự động cấu hình AudioSource 3D Loop cho Portal Prefab
        SetupPortalPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[AudioSetupEditor]</color> Đã thiết lập hoàn tất và lưu Prefab tại:\n- {PREFAB_PATH}");
    }

    private static void SyncToCoreSystemsPrefabs(AudioManager source)
    {
        string[] corePaths = new string[]
        {
            "Assets/Prefabs/Core/CoreSystems.prefab",
            "Assets/Resources/CoreSystems.prefab"
        };

        foreach (string corePath in corePaths)
        {
            if (!File.Exists(corePath)) continue;

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(corePath))
            {
                GameObject root = editScope.prefabContentsRoot;
                AudioManager target = root.GetComponentInChildren<AudioManager>();
                if (target != null)
                {
                    EditorUtility.CopySerialized(source, target);
                    Debug.Log($"<color=green>[AudioSetupEditor]</color> Đã đồng bộ AudioManager vào: {corePath}");
                }
            }
        }

        // Đồng bộ AudioManager trong Scene đang mở nếu có
        AudioManager sceneManager = Object.FindFirstObjectByType<AudioManager>();
        if (sceneManager != null)
        {
            EditorUtility.CopySerialized(source, sceneManager);
            EditorUtility.SetDirty(sceneManager);
            Debug.Log("<color=green>[AudioSetupEditor]</color> Đã đồng bộ AudioManager trong Scene đang mở.");
        }
    }

    private static AudioClip LoadAudioClipFlexible(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip != null) return clip;

        string dir = Path.GetDirectoryName(path);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
        string[] extensions = new string[] { ".wav", ".mp3", ".ogg" };

        foreach (string ext in extensions)
        {
            string candidate = Path.Combine(dir, nameWithoutExt + ext).Replace("\\", "/");
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(candidate);
            if (clip != null) return clip;
        }

        return null;
    }

    private static void SetAudioClip(SerializedObject so, string propertyName, string assetPath)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[AudioSetupEditor] Không tìm thấy property: {propertyName}");
            return;
        }

        AudioClip clip = LoadAudioClipFlexible(assetPath);
        if (clip != null)
        {
            prop.objectReferenceValue = clip;
        }
        else
        {
            Debug.LogWarning($"[AudioSetupEditor] Không tìm thấy file âm thanh tại: {assetPath}");
        }
    }

    private static void SetAudioClipArray(SerializedObject so, string propertyName, string[] assetPaths)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || !prop.isArray)
        {
            Debug.LogWarning($"[AudioSetupEditor] Không tìm thấy array property: {propertyName}");
            return;
        }

        prop.ClearArray();
        int added = 0;
        for (int i = 0; i < assetPaths.Length; i++)
        {
            AudioClip clip = LoadAudioClipFlexible(assetPaths[i]);
            if (clip != null)
            {
                prop.InsertArrayElementAtIndex(added);
                prop.GetArrayElementAtIndex(added).objectReferenceValue = clip;
                added++;
            }
            else
            {
                Debug.LogWarning($"[AudioSetupEditor] Không tìm thấy file âm thanh tại: {assetPaths[i]}");
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

    private static void SetupTorchPrefabs()
    {
        string[] torchPaths = new string[]
        {
            "Assets/Prefabs/Dungeon/DungeonPrefabs/torch_lit.prefab",
            "Assets/Prefabs/Dungeon/DungeonPrefabs/torch_wall_lit.prefab"
        };

        foreach (string path in torchPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(assetPath))
            {
                GameObject root = editScope.prefabContentsRoot;
                if (root.GetComponent<InfiniteLabyrinth.Audio.SpatialTorchAudio>() == null)
                {
                    root.AddComponent<InfiniteLabyrinth.Audio.SpatialTorchAudio>();
                }
            }
        }
    }

    private static void SetupPortalPrefab()
    {
        string portalPath = "Assets/Prefabs/Portal/Portal.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(portalPath);
        if (prefab == null) return;

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(portalPath))
        {
            GameObject root = editScope.prefabContentsRoot;
            AudioSource src = root.GetComponent<AudioSource>();
            if (src == null)
            {
                src = root.AddComponent<AudioSource>();
            }
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 1.0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2.0f;
            src.maxDistance = 16.0f;
            src.dopplerLevel = 0f;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Enviromemt/PortalSFX.wav");
            if (clip != null)
            {
                src.clip = clip;
            }
        }
    }
}
#endif
