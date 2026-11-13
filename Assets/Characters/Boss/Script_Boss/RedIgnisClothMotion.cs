using System;
using Unity.Animations.SpringBones;
using UnityEngine;

/// <summary>
/// Synchronizes Boss Red Ignis's helmet hair tail (fixed root Bone, 3 draping child bones)
/// and 3-panel split cape with smooth critically-damped Verlet physics, turn-whip damping,
/// anatomical sphere/capsule + back-panel body barriers, segment/skirt anti-clipping,
/// and subtle locomotion cadence.
/// </summary>
[DefaultExecutionOrder(-50)]
public class RedIgnisClothMotion : IgnisClothMotionBase
{
    private static readonly ClothRigProfile RedProfile = new ClothRigProfile
    {
        LogTag = "[RedIgnisClothMotion]",
        // Remove SpringBone from root hair stem ("Bone" stays fixed jutting out of helmet)
        // and from all 4 terminal leaf endpoints (Bone.013, Bone.037, Bone.025, Bone.035).
        NonSimulatedBoneNames = new[]
        {
            "Bone",
            "Bone.013",
            "Bone.037",
            "Bone.025",
            "Bone.035"
        },
        // 14 SpringBones paired with gravity = -22 & friction = 1.0:
        //   Hat Tail (3): Bone.002 -> Bone.004 -> Bone.006 (-> leaf Bone.013)
        //   Cape Left (4): Bone.014 -> Bone.015 -> Bone.016 -> Bone.018 (-> leaf Bone.037)
        //   Cape Center (3): Bone.021 -> Bone.022 -> Bone.023 (-> leaf Bone.025)
        //   Cape Right (4): Bone.026 -> Bone.028 -> Bone.030 -> Bone.034 (-> leaf Bone.035)
        ChainSpecs = new[]
        {
            new ChainBoneSpec("Bone.002", ClothRegion.HatTail, 0.33f, 24f, 0.170f, Vector3.zero, 0.042f, 0.30f, 1.00f, "HairCone2"),
            new ChainBoneSpec("Bone.004", ClothRegion.HatTail, 0.67f, 16f, 0.140f, Vector3.zero, 0.042f, 0.45f, 1.00f, "HairCone3"),
            new ChainBoneSpec("Bone.006", ClothRegion.HatTail, 1.00f, 10f, 0.120f, Vector3.zero, 0.042f, 0.60f, 1.25f, "HairCone4"),

            new ChainBoneSpec("Bone.014", ClothRegion.CapeLeft, 0.25f, 5.0f, 0.220f, Vector3.zero, 0.028f, 0.25f, 1.00f, null, isShoulderRoot: true),
            new ChainBoneSpec("Bone.015", ClothRegion.CapeLeft, 0.50f, 3.5f, 0.190f, Vector3.zero, 0.032f, 0.40f, 1.00f, null),
            new ChainBoneSpec("Bone.016", ClothRegion.CapeLeft, 0.75f, 2.5f, 0.160f, Vector3.zero, 0.036f, 0.55f, 1.00f, null),
            new ChainBoneSpec("Bone.018", ClothRegion.CapeLeft, 1.00f, 1.5f, 0.140f, Vector3.zero, 0.036f, 0.70f, 1.45f, null),

            new ChainBoneSpec("Bone.021", ClothRegion.CapeCenter, 0.33f, 5.0f, 0.210f, Vector3.zero, 0.024f, 0.25f, 1.00f, null),
            new ChainBoneSpec("Bone.022", ClothRegion.CapeCenter, 0.67f, 3.5f, 0.180f, Vector3.zero, 0.032f, 0.45f, 1.00f, null),
            new ChainBoneSpec("Bone.023", ClothRegion.CapeCenter, 1.00f, 2.0f, 0.150f, Vector3.zero, 0.036f, 0.70f, 1.40f, null),

            new ChainBoneSpec("Bone.026", ClothRegion.CapeRight, 0.25f, 5.0f, 0.220f, Vector3.zero, 0.028f, 0.25f, 1.00f, null, isShoulderRoot: true),
            new ChainBoneSpec("Bone.028", ClothRegion.CapeRight, 0.50f, 3.5f, 0.190f, Vector3.zero, 0.032f, 0.40f, 1.00f, null),
            new ChainBoneSpec("Bone.030", ClothRegion.CapeRight, 0.75f, 2.5f, 0.160f, Vector3.zero, 0.036f, 0.55f, 1.00f, null),
            new ChainBoneSpec("Bone.034", ClothRegion.CapeRight, 1.00f, 1.5f, 0.140f, Vector3.zero, 0.036f, 0.70f, 1.40f, null)
        },
        WalkStates = new[]
        {
            new WalkStateSpec("WalkT_RI", 1f),
            new WalkStateSpec("WalkBackRI", 1f),
            new WalkStateSpec("WalkLeftRI", 1f),
            new WalkStateSpec("WalkRightRI", 1f)
        },
        BackAxisBoneNames = new[] { "Bone.021" },
        HeadAimChildName = "mixamorig:Head.001",
        HeadMidpointFactor = 0.55f,
        HeadUpOffset = 0f,
        HeadBackOffset = 0f,
        HeadRadius = 0.115f,
        NeckUpOffset = 0f,
        NeckRadius = 0.090f,
        UpperChestRadius = 0.102f,
        TorsoBackOffset = -0.005f,
        TorsoRadius = 0.102f,
        ArmFallbackHeight = 0.175f,
        ThighFallbackHeight = 0.42f,
        ShinFallbackHeight = 0.44f,
        HeadBackPanelUpOffset = 0.055f,
        PresetFootstepCycleOffset = 0.18f,
        ApplyFootstepCycleOffsetOnUpdate = false,
        PresetStepLateralSwayStrength = 0.10f,
        PresetWingSpreadStrength = 1.65f,
        PresetHatTailForceScale = 0.22f
    };

    protected override ClothRigProfile Profile => RedProfile;

    protected override SpringManager RebuildThisRig()
    {
        return ConfigureClothRig(gameObject, applyPresetValues: true);
    }

    [ContextMenu("Rebuild Cloth Rig & Reset Pose")]
    private void ContextRebuildClothRig()
    {
        ConfigureClothRig(gameObject, applyPresetValues: true);
        RebuildRuntimeBoneMap();
        ResetClothPose();
    }

    /// <summary>
    /// Configures Red Ignis's hat tail, 3-strip cape, 14 critically-damped SpringBones,
    /// and 16 anatomical colliders (spheres, horizontal/limb capsules, and 3 one-sided back panels).
    /// </summary>
    public static SpringManager ConfigureClothRig(
        GameObject bossRoot,
        bool applyPresetValues = true,
        Action<UnityEngine.Object, string> recordUndo = null,
        Action<UnityEngine.Object> destroyComponent = null)
    {
        return ConfigureClothRigFor<RedIgnisClothMotion>(
            bossRoot,
            RedProfile,
            applyPresetValues,
            recordUndo,
            destroyComponent);
    }
}
