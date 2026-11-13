using System;
using Unity.Animations.SpringBones;
using UnityEngine;

/// <summary>
/// Synchronizes Boss Darkness Ignis's helmet hair tail and 3-panel split cape with smooth
/// critically-damped Verlet physics, turn-whip damping, 16 anatomical colliders calibrated to
/// IgnisTim's skeleton, segment/floor anti-clipping, and 2-stride WalkDN locomotion cadence.
/// </summary>
[DefaultExecutionOrder(-50)]
public class DarknessIgnisClothMotion : IgnisClothMotionBase
{
    private static readonly ClothRigProfile DarknessProfile = new ClothRigProfile
    {
        LogTag = "[DarknessIgnisClothMotion]",
        // Keep root helmet stem ("Bone") fixed over the spiked collar and strip terminal leaf tips
        // (Hair: Bone.015, Cape Left: Bone.038, Cape Center: Bone.028, Cape Right: Bone.048).
        NonSimulatedBoneNames = new[]
        {
            "Bone",
            "Bone.015",
            "Bone.038",
            "Bone.028",
            "Bone.048"
        },
        // 11 SpringBones paired with gravity = -22 & friction = 1.0:
        //   Hair Tail (2):  mixamorig:Head -> Bone (fixed) -> Bone.003 -> Bone.010 (-> leaf Bone.015)
        //   Cape Left (3):  mixamorig:LeftShoulder -> Bone.030 -> Bone.033 -> Bone.036 (-> leaf Bone.038)
        //   Cape Center (3): mixamorig:Spine2 -> Bone.018 -> Bone.021 -> Bone.027 (-> leaf Bone.028)
        //   Cape Right (3): mixamorig:RightShoulder -> Bone.037 -> Bone.042 -> Bone.045 (-> leaf Bone.048)
        ChainSpecs = new[]
        {
            new ChainBoneSpec("Bone.003", ClothRegion.HatTail, 0.50f, 20.0f, 0.160f, Vector3.zero, 0.026f, 0.38f, 1.00f, null),
            new ChainBoneSpec("Bone.010", ClothRegion.HatTail, 1.00f, 11.0f, 0.125f, Vector3.zero, 0.030f, 0.60f, 1.30f, null),

            new ChainBoneSpec("Bone.030", ClothRegion.CapeLeft, 0.33f, 5.0f, 0.215f, Vector3.zero, 0.028f, 0.28f, 1.00f, null, isShoulderRoot: true),
            new ChainBoneSpec("Bone.033", ClothRegion.CapeLeft, 0.67f, 3.5f, 0.180f, Vector3.zero, 0.032f, 0.48f, 1.00f, null),
            new ChainBoneSpec("Bone.036", ClothRegion.CapeLeft, 1.00f, 1.8f, 0.145f, Vector3.zero, 0.036f, 0.70f, 1.40f, null),

            new ChainBoneSpec("Bone.018", ClothRegion.CapeCenter, 0.33f, 5.0f, 0.210f, Vector3.zero, 0.024f, 0.25f, 1.00f, null),
            new ChainBoneSpec("Bone.021", ClothRegion.CapeCenter, 0.67f, 3.5f, 0.180f, Vector3.zero, 0.032f, 0.45f, 1.00f, null),
            new ChainBoneSpec("Bone.027", ClothRegion.CapeCenter, 1.00f, 2.0f, 0.150f, Vector3.zero, 0.036f, 0.70f, 1.40f, null),

            new ChainBoneSpec("Bone.037", ClothRegion.CapeRight, 0.33f, 5.0f, 0.215f, Vector3.zero, 0.028f, 0.28f, 1.00f, null, isShoulderRoot: true),
            new ChainBoneSpec("Bone.042", ClothRegion.CapeRight, 0.67f, 3.5f, 0.180f, Vector3.zero, 0.032f, 0.48f, 1.00f, null),
            new ChainBoneSpec("Bone.045", ClothRegion.CapeRight, 1.00f, 1.8f, 0.145f, Vector3.zero, 0.036f, 0.70f, 1.40f, null)
        },
        WalkStates = new[]
        {
            // WalkDN.anim spans 2.800s and contains 2 complete R-L stride cycles per clip loop
            new WalkStateSpec("WalkDN", 2f),
            new WalkStateSpec("WalkForwardDN", 1f),
            new WalkStateSpec("WalkBackDN", 1f),
            new WalkStateSpec("WalkLeftDN", 1f),
            new WalkStateSpec("WalkRightDN", 1f),
            new WalkStateSpec("Walk", 1f),
            new WalkStateSpec("RunDN", 1f)
        },
        BackAxisBoneNames = new[] { "Bone.021", "Bone.018" },
        // IgnisTim has no mixamorig:Head.001, so offset upward/back inside the helmet skull
        HeadAimChildName = null,
        HeadMidpointFactor = 0f,
        HeadUpOffset = 0.105f,
        HeadBackOffset = -0.010f,
        HeadRadius = 0.115f,
        NeckUpOffset = 0.035f,
        NeckRadius = 0.092f,
        UpperChestRadius = 0.100f,
        TorsoBackOffset = -0.010f,
        TorsoRadius = 0.092f,
        ArmFallbackHeight = 0.199f,
        ThighFallbackHeight = 0.437f,
        ShinFallbackHeight = 0.424f,
        HeadBackPanelUpOffset = 0.065f,
        PresetFootstepCycleOffset = 0.20f,
        ApplyFootstepCycleOffsetOnUpdate = true,
        PresetStepLateralSwayStrength = 0.11f,
        PresetWingSpreadStrength = 1.65f,
        PresetHatTailForceScale = 0.28f
    };

    protected override ClothRigProfile Profile => DarknessProfile;

    private void Reset()
    {
        footstepCycleOffset = DarknessProfile.PresetFootstepCycleOffset;
        stepLateralSwayStrength = DarknessProfile.PresetStepLateralSwayStrength;
        wingSpreadStrength = DarknessProfile.PresetWingSpreadStrength;
        hatTailForceScale = DarknessProfile.PresetHatTailForceScale;
    }

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
    /// Configures Darkness Ignis's hair tail (Bone.003, Bone.010) and 3-strip cape
    /// (Left: 30/33/36, Center: 18/21/27, Right: 37/42/45) with 11 critically-damped
    /// SpringBones and 16 anatomical colliders tailored to IgnisTim.fbx.
    /// </summary>
    public static SpringManager ConfigureClothRig(
        GameObject bossRoot,
        bool applyPresetValues = true,
        Action<UnityEngine.Object, string> recordUndo = null,
        Action<UnityEngine.Object> destroyComponent = null)
    {
        return ConfigureClothRigFor<DarknessIgnisClothMotion>(
            bossRoot,
            DarknessProfile,
            applyPresetValues,
            recordUndo,
            destroyComponent);
    }
}
