using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Animations.SpringBones;
using UnityEngine;

/// <summary>
/// Shared Verlet cloth simulation, turn-whip damping, anatomical body barriers,
/// segment/floor anti-clipping, and stride-synced locomotion force provider
/// for Ignis-class Bosses (Red Ignis & Darkness Ignis).
/// </summary>
[DefaultExecutionOrder(-50)]
public abstract class IgnisClothMotionBase : ForceProvider
{
    private static readonly BindingFlags SpringFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo BoneAxisField = typeof(SpringBone).GetField("boneAxis", SpringFieldFlags);
    private static readonly FieldInfo SpringLengthField = typeof(SpringBone).GetField("springLength", SpringFieldFlags);
    private static readonly FieldInfo CurrTipPosField = typeof(SpringBone).GetField("currTipPos", SpringFieldFlags);
    private static readonly FieldInfo PrevTipPosField = typeof(SpringBone).GetField("prevTipPos", SpringFieldFlags);

    protected enum ClothRegion
    {
        HatTail,
        CapeLeft,
        CapeCenter,
        CapeRight
    }

    protected readonly struct ChainBoneSpec
    {
        public readonly string BoneName;
        public readonly ClothRegion Region;
        public readonly float ChainFraction;
        public readonly float StiffnessForce;
        public readonly float DragForce;
        public readonly Vector3 SpringForce;
        public readonly float Radius;
        public readonly float WindInfluence;
        public readonly float MaxSampleFactor;
        public readonly string PivotName;
        public readonly bool IsShoulderRoot;

        public ChainBoneSpec(
            string boneName,
            ClothRegion region,
            float chainFraction,
            float stiffnessForce,
            float dragForce,
            Vector3 springForce,
            float radius,
            float windInfluence,
            float maxSampleFactor,
            string pivotName,
            bool isShoulderRoot = false)
        {
            BoneName = boneName;
            Region = region;
            ChainFraction = chainFraction;
            StiffnessForce = stiffnessForce;
            DragForce = dragForce;
            SpringForce = springForce;
            Radius = radius;
            WindInfluence = windInfluence;
            MaxSampleFactor = maxSampleFactor;
            PivotName = pivotName;
            IsShoulderRoot = isShoulderRoot;
        }
    }

    protected readonly struct WalkStateSpec
    {
        public readonly int StateHash;
        public readonly float CyclesPerLoop;

        public WalkStateSpec(string stateName, float cyclesPerLoop = 1f)
        {
            StateHash = Animator.StringToHash(stateName);
            CyclesPerLoop = Mathf.Max(0.1f, cyclesPerLoop);
        }
    }

    protected sealed class ClothRigProfile
    {
        public string LogTag;
        public string[] NonSimulatedBoneNames;
        public ChainBoneSpec[] ChainSpecs;
        public WalkStateSpec[] WalkStates;
        public string[] BackAxisBoneNames;
        public string HeadAimChildName;
        public float HeadMidpointFactor;
        public float HeadUpOffset;
        public float HeadBackOffset;
        public float HeadRadius;
        public float NeckUpOffset;
        public float NeckRadius;
        public float UpperChestRadius;
        public float TorsoBackOffset;
        public float TorsoRadius;
        public float ArmFallbackHeight;
        public float ThighFallbackHeight;
        public float ShinFallbackHeight;
        public float HeadBackPanelUpOffset;
        public float PresetFootstepCycleOffset = 0.18f;
        public bool ApplyFootstepCycleOffsetOnUpdate = false;
        public float PresetStepLateralSwayStrength = 0.10f;
        public float PresetWingSpreadStrength = 1.65f;
        public float PresetHatTailForceScale = 0.22f;
    }

    private class BoneRuntimeMeta
    {
        public SpringBone Bone;
        public ClothRegion Region;
        public float ChainFraction;
        public float PhaseLag;
        public float BaseDragForce;
        public Vector3 BoneAxis;
        public float WorldLength;
        public float MaxSampleFactor;
        public Vector3 LastHeadPos;
        public bool HasLastHeadPos;
    }

    private static readonly string[] CapeColliderNames =
    {
        "CapeCol_Head",
        "CapeCol_Neck",
        "CapeCol_LeftShoulder",
        "CapeCol_RightShoulder",
        "CapeCol_UpperChest",
        "CapeCol_Torso",
        "CapeCol_Hips",
        "CapeCol_LeftArm",
        "CapeCol_RightArm",
        "CapeCol_LeftThigh",
        "CapeCol_RightThigh",
        "CapeCol_LeftShin",
        "CapeCol_RightShin",
        "CapeCol_HeadBackPanel",
        "CapeCol_TorsoBackPanel",
        "CapeCol_PelvisBackPanel"
    };

    [Header("=== MÔ PHỎNG VẬT LÝ (SIMULATION) ===")]
    [Tooltip("Tần số cập nhật vật lý SpringBone (Hz). Mặc định: 60. Cao hơn giúp mượt hơn nhưng tốn CPU hơn.")]
    [SerializeField] protected int targetSimulationFps = 60;
    [Tooltip("Giới hạn thời gian tối đa mỗi frame (giây) để tránh vải bị giật/nổ khi game tụt FPS. Mặc định: 0.05")]
    [SerializeField] protected float maxFrameDeltaTime = 0.05f;
    [Tooltip("Số bước chia nhỏ vật lý tối đa trong 1 frame khi FPS thấp. Mặc định: 3")]
    [SerializeField] protected int maxSubstepsPerFrame = 3;
    [Tooltip("Khoảng cách dịch chuyển đột ngột tối thiểu (mét) để tự reset dáng vải, tránh bị kéo giãn. Mặc định: 1.5")]
    [SerializeField] protected float teleportResetDistance = 1.5f;

    [Header("=== CHỐNG XUYÊN SÀN (FLOOR CLEARANCE) ===")]
    [Tooltip("Bật chống xuyên mặt sàn cho đuôi áo choàng (không cho vạt áo chìm xuống đất). Mặc định: Bật")]
    [SerializeField] protected bool enableGroundCollision = true;
    [Tooltip("Khoảng hở tối thiểu giữa vạt áo choàng và mặt sàn (mét). Mặc định: 0.04")]
    [SerializeField] protected float groundClearanceOffset = 0.04f;

    [Header("=== CHUYỂN ĐỘNG ÁO CHOÀNG & TÓC ===")]
    [Tooltip("Độ lệch pha nhịp bước chân khớp với clip Walk (0..1). Chỉnh để nhịp lắc áo khớp lúc bước chân. Mặc định: 0.18")]
    [SerializeField] protected float footstepCycleOffset = 0.18f;
    [Tooltip("Độ đung đưa ngang trái/phải của áo choàng và tóc theo từng bước đi. Mặc định: 0.10")]
    [SerializeField] protected float stepLateralSwayStrength = 0.10f;
    [Tooltip("Độ nhấp nhô lên/xuống theo nhịp bước chân. Để 0 để tránh hất vạt áo lên làm xuyên người. Mặc định: 0")]
    [SerializeField] protected float stepVerticalBounceStrength = 0.0f;
    [Tooltip("Độ trễ sóng gợn dọc từ trên xuống đuôi vải (radian). Lớn hơn tạo cảm giác uốn lượn rõ hơn. Mặc định: 1.35")]
    [SerializeField] protected float stepWaveLagRadians = 1.35f;
    [Tooltip("Lực gió nâng vạt áo bay bổng lên cao khi chạy nhanh. Để 0 để áo rủ tự nhiên, tránh bật ngược lên lưng/đầu. Mặc định: 0")]
    [SerializeField] protected float aerodynamicLiftStrength = 0.0f;
    [Tooltip("Độ xòe rộng thêm của 2 dải áo choàng trái/phải khi Boss chạy (tự giảm khi đi ngang để không văng loạn). Mặc định: 1.65")]
    [SerializeField] protected float wingSpreadStrength = 1.65f;
    [Tooltip("Độ phất nhẹ do gió thoảng khi Boss đứng yên (Idle). Nhỏ vừa phải giúp áo tự nhiên mà không rung giật. Mặc định: 0.04")]
    [SerializeField] protected float idleBreezeStrength = 0.04f;
    [Tooltip("Hệ số nhân lực gió/rung tác động lên đuôi tóc (0..1). Nhỏ hơn 1 giúp tóc rủ mềm và ít bay loạn hơn áo choàng. Mặc định: 0.22")]
    [SerializeField] protected float hatTailForceScale = 0.22f;
    [Tooltip("Lực kéo ổn định giữ áo choàng rủ xuống phía sau khi Boss rơi nhanh từ chiêu nhảy (JumpAttack). Mặc định: 0.65")]
    [SerializeField] protected float fallStabilizationStrength = 0.65f;

    [Header("=== HÃM RUNG KHI XOAY & ĐI NGANG ===")]
    [Tooltip("Ngưỡng tốc độ xoay người (độ/giây) bắt đầu kích hoạt lực hãm để tránh áo quất mạnh khi Boss quay đầu nhanh. Mặc định: 45")]
    [SerializeField] protected float turnDampingThresholdDegPerSec = 45f;
    [Tooltip("Độ cản (Drag) cộng thêm tối đa khi Boss xoay gấp, đi ngang hoặc tiếp đất mạnh để áo bớt văng loạn. Mặc định: 0.28")]
    [SerializeField] protected float maxExtraTurnDrag = 0.28f;

    private SpringManager springManager;
    private Animator anim;
    private CharacterController characterController;
    private readonly Dictionary<SpringBone, BoneRuntimeMeta> boneMetaMap = new Dictionary<SpringBone, BoneRuntimeMeta>();
    private readonly List<BoneRuntimeMeta> orderedBones = new List<BoneRuntimeMeta>(14);
    private readonly List<SpringPanelCollider> cachedBackPanels = new List<SpringPanelCollider>(3);

    private bool wasAutomaticUpdates = true;
    private float stepAccumulator;
    private Vector3 lastRootPosition;
    private Quaternion lastRootRotation;
    private bool hasLastRootPose;
    private float lastGroundedY;
    private float fallbackWalkPhase;
    private float smoothedExtraDrag;
    private float lastVerticalSpeed;

    // Frame-cached motion inputs sampled once per LateUpdate before substeps read GetForceOnBone
    private float frameHorizontalSpeed;
    private float frameLateralSpeed;
    private float frameVerticalSpeed;
    private float frameVerticalImpulse;
    private float frameWalkBlend;
    private float frameGaitAngle;
    private float frameIdleTime;
    private float frameForwardDot;

    protected abstract ClothRigProfile Profile { get; }

    protected abstract SpringManager RebuildThisRig();

    protected virtual void Awake()
    {
        CacheReferences();
        bool healed = false;
        if (springManager != null && NeedsRuntimeAutoHeal(springManager, Profile))
        {
            RebuildThisRig();
            healed = true;
        }

        RebuildRuntimeBoneMap();
        if (healed)
        {
            ResetClothPose();
        }

        lastRootPosition = transform.position;
        lastRootRotation = transform.rotation;
        lastGroundedY = transform.position.y;
        hasLastRootPose = true;
    }

    protected virtual void OnEnable()
    {
        CacheReferences();
        RebuildRuntimeBoneMap();

        if (Application.isPlaying && springManager != null)
        {
            wasAutomaticUpdates = springManager.automaticUpdates;
            springManager.automaticUpdates = false;
            springManager.simulationFrameRate = targetSimulationFps;
        }

        stepAccumulator = 0f;
        smoothedExtraDrag = 0f;
        lastVerticalSpeed = 0f;
        lastRootPosition = transform.position;
        lastRootRotation = transform.rotation;
        lastGroundedY = transform.position.y;
        hasLastRootPose = true;
        ClearTrackedHeadPositions();
    }

    protected virtual void OnDisable()
    {
        RestoreBaseDragForces();
        if (Application.isPlaying && springManager != null)
        {
            springManager.automaticUpdates = wasAutomaticUpdates;
        }
    }

    protected virtual void LateUpdate()
    {
        if (!Application.isPlaying || springManager == null)
        {
            return;
        }

        Vector3 currentPos = transform.position;
        Quaternion currentRot = transform.rotation;
        float rawDt = Time.deltaTime;

        if (!hasLastRootPose)
        {
            lastRootPosition = currentPos;
            lastRootRotation = currentRot;
            lastGroundedY = currentPos.y;
            hasLastRootPose = true;
        }

        Vector3 rootDelta = currentPos - lastRootPosition;
        if (rootDelta.sqrMagnitude > teleportResetDistance * teleportResetDistance)
        {
            lastRootPosition = currentPos;
            lastRootRotation = currentRot;
            lastGroundedY = currentPos.y;
            ResetClothPose();
            return;
        }

        float turnAngleDeg = Quaternion.Angle(lastRootRotation, currentRot);
        lastRootPosition = currentPos;
        lastRootRotation = currentRot;

        if (characterController == null || characterController.isGrounded)
        {
            lastGroundedY = currentPos.y;
        }

        // Handle floor clearance in our post-step pass so SpringBone.CheckForCollision() is never skipped.
        springManager.collideWithGround = false;
        springManager.groundHeight = lastGroundedY + groundClearanceOffset;
        springManager.simulationFrameRate = targetSimulationFps;

        if (rawDt <= 0f)
        {
            springManager.isPaused = true;
            springManager.UpdateDynamics();
            EnforcePostStepSegmentCollisions(syncVerletState: false);
            return;
        }

        float clampedDt = Mathf.Min(rawDt, maxFrameDeltaTime);
        SampleMotionSignals(rootDelta, turnAngleDeg, rawDt);
        ApplyDynamicTurnDamping();

        float fixedStep = 1f / Mathf.Max(30, targetSimulationFps);
        stepAccumulator += clampedDt;

        int stepsExecuted = 0;
        while (stepAccumulator >= fixedStep && stepsExecuted < maxSubstepsPerFrame)
        {
            springManager.isPaused = false;
            springManager.UpdateDynamics();
            EnforcePostStepSegmentCollisions(syncVerletState: true);
            stepAccumulator -= fixedStep;
            stepsExecuted++;
        }

        if (stepsExecuted == 0)
        {
            springManager.isPaused = true;
            springManager.UpdateDynamics();
            EnforcePostStepSegmentCollisions(syncVerletState: false);
        }
        else if (stepAccumulator >= fixedStep)
        {
            stepAccumulator = fixedStep * 0.5f;
        }
    }

    /// <summary>
    /// Resets SpringBone Verlet history to the current animated pose after teleports or Boss revive.
    /// </summary>
    public void ResetClothPose()
    {
        CacheReferences();
        stepAccumulator = 0f;
        smoothedExtraDrag = 0f;
        lastVerticalSpeed = 0f;
        RestoreBaseDragForces();
        ClearTrackedHeadPositions();
        lastRootPosition = transform.position;
        lastRootRotation = transform.rotation;
        lastGroundedY = transform.position.y;
        hasLastRootPose = true;

        if (springManager == null || springManager.springBones == null)
        {
            return;
        }

        foreach (SpringBone bone in springManager.springBones)
        {
            if (bone != null)
            {
                bone.Initialize(springManager);
            }
        }

        RefreshAllBoneGeometries();
    }

    public override Vector3 GetForceOnBone(SpringBone springBone)
    {
        if (springBone == null || !boneMetaMap.TryGetValue(springBone, out BoneRuntimeMeta meta))
        {
            return Vector3.zero;
        }

        float chainT = meta.ChainFraction;
        float regionScale = meta.Region == ClothRegion.HatTail ? hatTailForceScale : 1f;
        Vector3 totalForce = Vector3.zero;

        // 0. Root-anchored cloak memory keeps shoulder-attached strips draping behind torso
        // regardless of Mixamo shoulder twists (+/-170 deg) or overhead arm swings
        if (meta.Region != ClothRegion.HatTail)
        {
            float rootBiasWeight = 1.25f - 0.55f * chainT;
            float sideSign = meta.Region == ClothRegion.CapeLeft ? -1f : (meta.Region == ClothRegion.CapeRight ? 1f : 0f);
            float extraBackSweep = sideSign != 0f ? 0.45f * chainT : 0f;
            totalForce += Vector3.down * (1.80f * rootBiasWeight)
                        - transform.forward * (1.15f * rootBiasWeight + extraBackSweep);

            if (sideSign != 0f)
            {
                // Progressive A-line flare opens the lower cape hem gently without stiff shoulder wings
                float capeHemFlare = 0.85f + 5.15f * chainT;
                totalForce += transform.right * (sideSign * capeHemFlare);
            }
        }

        // 1. Subtle step-cadence lateral sway & trailing pulse (no upward kick)
        if (frameWalkBlend > 0.01f)
        {
            float waveAngle = frameGaitAngle - meta.PhaseLag;
            float lateralSway = Mathf.Sin(waveAngle) * stepLateralSwayStrength * chainT;
            float stepPulse = 0.5f + 0.5f * Mathf.Cos(waveAngle * 2f);
            float trailingPulse = stepPulse * 0.25f * chainT;

            // Gentle backward-downward drape keeps cloth clear of calves when backing up
            float backwardClearance = frameForwardDot < -0.2f ? (-frameForwardDot * 0.45f * chainT) : 0f;

            Vector3 stepForce =
                transform.right * lateralSway +
                transform.up * (stepVerticalBounceStrength * stepPulse * chainT) -
                transform.forward * (trailingPulse + backwardClearance);

            totalForce += stepForce * frameWalkBlend;
        }

        // 2. Gentle outward split-panel separation & optional lift during movement (damped during strafe)
        float speedFactor = Mathf.Clamp01(frameHorizontalSpeed / 4.5f);
        if (speedFactor > 0.01f)
        {
            if (aerodynamicLiftStrength != 0f)
            {
                totalForce += transform.up * (aerodynamicLiftStrength * speedFactor * chainT);
            }

            if (meta.Region == ClothRegion.CapeLeft || meta.Region == ClothRegion.CapeRight)
            {
                float sideSign = meta.Region == ClothRegion.CapeLeft ? -1f : 1f;
                float strafeRatio = Mathf.Clamp01(frameLateralSpeed / Mathf.Max(frameHorizontalSpeed, 0.2f));
                float flareMobilityScale = 1f - 0.65f * strafeRatio;
                float dynamicSpread = wingSpreadStrength * speedFactor * flareMobilityScale * chainT;
                totalForce += transform.right * (sideSign * dynamicSpread)
                            - transform.forward * (0.55f * speedFactor * chainT);
            }
        }

        // 3. Minimal slow idle heat breath behind armor (keeps drape resting downward)
        float idleWeight = 1f - frameWalkBlend;
        if (idleWeight > 0.02f && idleBreezeStrength > 0.001f)
        {
            float slowWave = Mathf.Sin(frameIdleTime * 1.15f - meta.PhaseLag * 0.7f);
            float microFlutter = Mathf.Cos(frameIdleTime * 1.95f - meta.PhaseLag);
            Vector3 idleForce =
                -transform.forward * ((0.55f + 0.45f * slowWave) * idleBreezeStrength) +
                transform.right * (microFlutter * 0.22f * idleBreezeStrength);

            totalForce += idleForce * (idleWeight * chainT);
        }

        // 4. Downward air-stream stabilization during high-speed descents
        if (frameVerticalSpeed < -1.5f)
        {
            float fallMagnitude = Mathf.Clamp(-frameVerticalSpeed - 1.5f, 0f, 10f);
            totalForce += (Vector3.down * fallStabilizationStrength - transform.forward * 0.35f) *
                          (fallMagnitude * chainT);
        }

        return totalForce * regionScale;
    }

    private void CacheReferences()
    {
        if (springManager == null)
        {
            springManager = GetComponentInChildren<SpringManager>(true);
        }

        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>(true);
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void SampleMotionSignals(Vector3 rootDelta, float turnAngleDeg, float rawDt)
    {
        Vector3 velocity = rootDelta / Mathf.Max(rawDt, 1e-5f);
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

        frameHorizontalSpeed = horizontalVel.magnitude;
        frameLateralSpeed = Mathf.Abs(Vector3.Dot(horizontalVel, transform.right));
        frameVerticalSpeed = velocity.y;
        frameVerticalImpulse = Mathf.Abs(frameVerticalSpeed - lastVerticalSpeed);
        lastVerticalSpeed = frameVerticalSpeed;

        frameForwardDot = frameHorizontalSpeed > 0.05f
            ? Vector3.Dot(horizontalVel / frameHorizontalSpeed, transform.forward)
            : 0f;
        frameIdleTime = Time.time;

        // Absorb snap-rotation, strafe whip, and jump-landing spikes smoothly via Verlet drag
        float angularSpeed = turnAngleDeg / Mathf.Max(rawDt, 1e-4f);
        float targetExtraDrag = 0f;
        if (angularSpeed > turnDampingThresholdDegPerSec)
        {
            float t = Mathf.Clamp01((angularSpeed - turnDampingThresholdDegPerSec) / 220f);
            targetExtraDrag = t * maxExtraTurnDrag;
        }

        if (frameLateralSpeed > 0.35f)
        {
            float strafeRatio = Mathf.Clamp01((frameLateralSpeed - 0.35f) / 2.2f);
            targetExtraDrag = Mathf.Max(targetExtraDrag, strafeRatio * (maxExtraTurnDrag * 0.55f));
        }

        if (frameVerticalSpeed < -2.5f || frameVerticalImpulse > 3.5f)
        {
            targetExtraDrag = Mathf.Max(targetExtraDrag, maxExtraTurnDrag * 0.80f);
        }

        float dragRate = targetExtraDrag > smoothedExtraDrag ? 22f : 6f;
        smoothedExtraDrag = Mathf.MoveTowards(smoothedExtraDrag, targetExtraDrag, rawDt * dragRate);

        bool inWalkState = false;
        float normalizedCycle = fallbackWalkPhase;
        WalkStateSpec[] walkStates = Profile.WalkStates;

        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (TryMatchWalkState(walkStates, stateInfo.shortNameHash, out float cyclesPerLoop))
            {
                inWalkState = true;
                normalizedCycle = Mathf.Repeat(stateInfo.normalizedTime * cyclesPerLoop, 1f);
                fallbackWalkPhase = normalizedCycle;
            }
            else if (anim.IsInTransition(0))
            {
                AnimatorStateInfo nextInfo = anim.GetNextAnimatorStateInfo(0);
                if (TryMatchWalkState(walkStates, nextInfo.shortNameHash, out float nextCycles))
                {
                    inWalkState = true;
                    normalizedCycle = Mathf.Repeat(nextInfo.normalizedTime * nextCycles, 1f);
                    fallbackWalkPhase = normalizedCycle;
                }
            }
        }

        if (!inWalkState && frameHorizontalSpeed > 0.45f)
        {
            fallbackWalkPhase = Mathf.Repeat(fallbackWalkPhase + rawDt * (frameHorizontalSpeed * 0.25f), 1f);
            normalizedCycle = fallbackWalkPhase;
        }

        float targetWalkBlend = inWalkState
            ? Mathf.Clamp01(Mathf.Max(0.35f, frameHorizontalSpeed / 2.6f))
            : Mathf.Clamp01((frameHorizontalSpeed - 0.5f) / 3.0f);

        frameWalkBlend = Mathf.MoveTowards(frameWalkBlend, targetWalkBlend, rawDt * 6f);
        frameGaitAngle = (normalizedCycle - footstepCycleOffset) * (Mathf.PI * 2f);
    }

    private static bool TryMatchWalkState(WalkStateSpec[] walkStates, int shortNameHash, out float cyclesPerLoop)
    {
        if (walkStates != null)
        {
            for (int i = 0; i < walkStates.Length; i++)
            {
                if (walkStates[i].StateHash == shortNameHash)
                {
                    cyclesPerLoop = walkStates[i].CyclesPerLoop;
                    return true;
                }
            }
        }

        cyclesPerLoop = 1f;
        return false;
    }

    private void ApplyDynamicTurnDamping()
    {
        if (boneMetaMap.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<SpringBone, BoneRuntimeMeta> kvp in boneMetaMap)
        {
            if (kvp.Key != null)
            {
                kvp.Key.dragForce = Mathf.Clamp(kvp.Value.BaseDragForce + smoothedExtraDrag, 0.05f, 0.55f);
            }
        }
    }

    private void ClearTrackedHeadPositions()
    {
        for (int i = 0; i < orderedBones.Count; i++)
        {
            orderedBones[i].HasLastHeadPos = false;
        }
    }

    private void RestoreBaseDragForces()
    {
        foreach (KeyValuePair<SpringBone, BoneRuntimeMeta> kvp in boneMetaMap)
        {
            if (kvp.Key != null)
            {
                kvp.Key.dragForce = kvp.Value.BaseDragForce;
            }
        }
    }

    protected void RebuildRuntimeBoneMap()
    {
        boneMetaMap.Clear();
        orderedBones.Clear();
        cachedBackPanels.Clear();
        if (springManager == null)
        {
            return;
        }

        SpringPanelCollider[] foundPanels = transform.GetComponentsInChildren<SpringPanelCollider>(true);
        for (int i = 0; i < foundPanels.Length; i++)
        {
            SpringPanelCollider panel = foundPanels[i];
            if (panel != null && panel.name.StartsWith("CapeCol_", StringComparison.Ordinal))
            {
                cachedBackPanels.Add(panel);
            }
        }

        if (springManager.springBones == null || springManager.springBones.Length == 0)
        {
            springManager.FindSpringBones(true);
        }

        if (springManager.springBones == null)
        {
            return;
        }

        Dictionary<string, SpringBone> boneByName = new Dictionary<string, SpringBone>(springManager.springBones.Length);
        foreach (SpringBone bone in springManager.springBones)
        {
            if (bone != null && !boneByName.ContainsKey(bone.name))
            {
                boneByName[bone.name] = bone;
            }
        }

        // Preserve strict ChainSpecs parent-to-child order for topological segment resolution
        foreach (ChainBoneSpec spec in Profile.ChainSpecs)
        {
            if (!boneByName.TryGetValue(spec.BoneName, out SpringBone bone) || bone == null)
            {
                continue;
            }

            bone.dragForce = spec.DragForce;
            BoneRuntimeMeta meta = new BoneRuntimeMeta
            {
                Bone = bone,
                Region = spec.Region,
                ChainFraction = spec.ChainFraction,
                PhaseLag = spec.ChainFraction * stepWaveLagRadians,
                BaseDragForce = spec.DragForce,
                MaxSampleFactor = spec.MaxSampleFactor,
                HasLastHeadPos = false
            };
            RefreshBoneGeometry(meta);
            boneMetaMap[bone] = meta;
            orderedBones.Add(meta);
        }
    }

    private void RefreshAllBoneGeometries()
    {
        for (int i = 0; i < orderedBones.Count; i++)
        {
            RefreshBoneGeometry(orderedBones[i]);
        }
    }

    private static void RefreshBoneGeometry(BoneRuntimeMeta meta)
    {
        if (meta == null || meta.Bone == null)
        {
            return;
        }

        Vector3 cachedAxis = Vector3.zero;
        float cachedLength = 0f;

        if (BoneAxisField != null)
        {
            cachedAxis = (Vector3)BoneAxisField.GetValue(meta.Bone);
        }

        if (SpringLengthField != null)
        {
            cachedLength = (float)SpringLengthField.GetValue(meta.Bone);
        }

        if (cachedLength <= 0.001f || cachedAxis.sqrMagnitude < 0.1f)
        {
            Transform tf = meta.Bone.transform;
            if (TryGetAverageValidChildPosition(tf, out Vector3 childPos))
            {
                Vector3 worldVec = childPos - tf.position;
                cachedLength = worldVec.magnitude;
                Vector3 localVec = tf.InverseTransformPoint(childPos);
                cachedAxis = localVec.sqrMagnitude > 1e-8f ? localVec.normalized : Vector3.down;
            }
        }

        meta.BoneAxis = cachedAxis.sqrMagnitude > 1e-6f ? cachedAxis.normalized : Vector3.down;
        meta.WorldLength = cachedLength;
    }

    private static bool TryGetAverageValidChildPosition(Transform parent, out Vector3 avgPos)
    {
        avgPos = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.GetComponent<SpringBonePivot>() == null && !child.name.StartsWith("CapeCol_", StringComparison.Ordinal))
            {
                avgPos += child.position;
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return false;
        }

        avgPos /= validCount;
        return true;
    }

    private void EnforcePostStepSegmentCollisions(bool syncVerletState)
    {
        if (orderedBones.Count == 0 || springManager == null)
        {
            return;
        }

        float groundPlaneY = springManager.groundHeight;
        bool checkBodies = springManager.enableCollision;

        for (int i = 0; i < orderedBones.Count; i++)
        {
            BoneRuntimeMeta meta = orderedBones[i];
            SpringBone bone = meta.Bone;
            if (bone == null || !bone.enabled)
            {
                continue;
            }

            if (meta.WorldLength <= 0.001f || meta.BoneAxis.sqrMagnitude < 0.1f)
            {
                RefreshBoneGeometry(meta);
                if (meta.WorldLength <= 0.001f || meta.BoneAxis.sqrMagnitude < 0.1f)
                {
                    continue;
                }
            }

            Transform boneTf = bone.transform;
            Vector3 headPos = boneTf.position;
            Vector3 initialDir = (boneTf.rotation * meta.BoneAxis).normalized;
            Vector3 initialTip = headPos + initialDir * meta.WorldLength;
            Vector3 resolvedTip = initialTip;
            Vector3 accumulatedNormal = Vector3.zero;
            bool hasSkirt = meta.MaxSampleFactor > 1.05f;
            float midSkirt = hasSkirt ? 0.5f * (1.00f + meta.MaxSampleFactor) : 1.00f;
            float boneRadiusModel = Mathf.Max(0.005f, bone.radius);
            float boneScale = Mathf.Max(0.01f, boneTf.lossyScale.x);

            for (int iter = 0; iter < 2; iter++)
            {
                ClampUpwardDrape(headPos, meta.WorldLength, meta, ref resolvedTip);

                if (checkBodies)
                {
                    ResolveSampleFactor(bone, headPos, 0.55f, boneRadiusModel, boneScale, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);
                    ResolveSampleFactor(bone, headPos, 0.80f, boneRadiusModel, boneScale, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);
                    ResolveSampleFactor(bone, headPos, 1.00f, boneRadiusModel, boneScale, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);

                    if (hasSkirt)
                    {
                        ResolveSampleFactor(bone, headPos, midSkirt, boneRadiusModel, boneScale, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);
                        ResolveSampleFactor(bone, headPos, meta.MaxSampleFactor, boneRadiusModel, boneScale, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);
                    }
                }

                if (enableGroundCollision)
                {
                    ResolveGroundFloor(headPos, meta.MaxSampleFactor, boneRadiusModel * boneScale, groundPlaneY, meta.WorldLength, ref resolvedTip, ref accumulatedNormal);
                }
            }

            bool rotated = (resolvedTip - initialTip).sqrMagnitude > 1e-8f;
            if (rotated)
            {
                Vector3 desiredDir = (resolvedTip - headPos).normalized;
                Quaternion deltaRot = Quaternion.FromToRotation(initialDir, desiredDir);
                boneTf.rotation = deltaRot * boneTf.rotation;

                if (syncVerletState)
                {
                    SyncVerletTipAfterProjection(bone, resolvedTip, initialTip, accumulatedNormal);
                    PropagateParentRotationToDescendants(i, headPos, deltaRot);
                }
            }

            if (syncVerletState)
            {
                ClampRelativeVerletSpeed(meta, headPos, 0.060f);
                meta.LastHeadPos = headPos;
                meta.HasLastHeadPos = true;
            }
        }
    }

    private void ClampUpwardDrape(Vector3 headPos, float worldLength, BoneRuntimeMeta meta, ref Vector3 tipPos)
    {
        Vector3 vec = tipPos - headPos;
        float len = vec.magnitude;
        if (len <= 1e-5f)
        {
            return;
        }

        Vector3 dir = vec / len;
        float maxUpDot = meta.Region == ClothRegion.HatTail
            ? (meta.ChainFraction <= 0.40f ? 0.25f : 0.12f)
            : (meta.ChainFraction <= 0.40f ? -0.10f : 0.06f);

        if (dir.y <= maxUpDot)
        {
            return;
        }

        Vector3 horiz = new Vector3(dir.x, 0f, dir.z);
        if (horiz.sqrMagnitude < 1e-6f)
        {
            horiz = -transform.forward;
        }
        else
        {
            horiz.Normalize();
        }

        float horizScale = Mathf.Sqrt(Mathf.Max(0f, 1f - maxUpDot * maxUpDot));
        Vector3 clampedDir = horiz * horizScale + Vector3.up * maxUpDot;
        tipPos = headPos + clampedDir * worldLength;
    }

    private void PropagateParentRotationToDescendants(int parentIndex, Vector3 pivot, Quaternion deltaRot)
    {
        ClothRegion parentRegion = orderedBones[parentIndex].Region;
        for (int j = parentIndex + 1; j < orderedBones.Count; j++)
        {
            BoneRuntimeMeta childMeta = orderedBones[j];
            if (childMeta.Region != parentRegion || childMeta.Bone == null)
            {
                break;
            }

            if (CurrTipPosField != null)
            {
                Vector3 cur = (Vector3)CurrTipPosField.GetValue(childMeta.Bone);
                CurrTipPosField.SetValue(childMeta.Bone, PivotRotate(cur, pivot, deltaRot));
            }

            if (PrevTipPosField != null)
            {
                Vector3 prev = (Vector3)PrevTipPosField.GetValue(childMeta.Bone);
                PrevTipPosField.SetValue(childMeta.Bone, PivotRotate(prev, pivot, deltaRot));
            }

            if (childMeta.HasLastHeadPos)
            {
                childMeta.LastHeadPos = PivotRotate(childMeta.LastHeadPos, pivot, deltaRot);
            }
        }
    }

    private static Vector3 PivotRotate(Vector3 point, Vector3 pivot, Quaternion rot)
    {
        return pivot + rot * (point - pivot);
    }

    private static void ClampRelativeVerletSpeed(BoneRuntimeMeta meta, Vector3 headPos, float maxRelStepDist)
    {
        if (CurrTipPosField == null || PrevTipPosField == null || !meta.HasLastHeadPos || meta.Bone == null)
        {
            return;
        }

        Vector3 currTip = (Vector3)CurrTipPosField.GetValue(meta.Bone);
        Vector3 prevTip = (Vector3)PrevTipPosField.GetValue(meta.Bone);
        Vector3 headMove = headPos - meta.LastHeadPos;
        Vector3 relVel = (currTip - prevTip) - headMove;

        if (relVel.sqrMagnitude > maxRelStepDist * maxRelStepDist)
        {
            Vector3 clampedStep = headMove + relVel.normalized * maxRelStepDist;
            PrevTipPosField.SetValue(meta.Bone, currTip - clampedStep);
        }
    }

    private void ResolveSampleFactor(
        SpringBone bone,
        Vector3 headPos,
        float factor,
        float boneRadiusModel,
        float boneScale,
        float boneWorldLength,
        ref Vector3 tipPos,
        ref Vector3 accumulatedNormal)
    {
        Vector3 fallbackBack = -transform.forward;
        float invFactor = 1f / Mathf.Max(factor, 0.55f);

        // 1. Spheres
        if (bone.sphereColliders != null)
        {
            for (int i = 0; i < bone.sphereColliders.Length; i++)
            {
                SpringSphereCollider sphere = bone.sphereColliders[i];
                if (sphere == null || !sphere.enabled || (sphere.linkedRenderer != null && !sphere.linkedRenderer.enabled))
                {
                    continue;
                }

                Vector3 samplePos = Vector3.LerpUnclamped(headPos, tipPos, factor);
                Transform colTf = sphere.transform;
                float colScale = Mathf.Max(0.01f, colTf.lossyScale.x);
                float localRadius = boneRadiusModel * (boneScale / colScale);
                float combined = sphere.radius + localRadius;

                Vector3 localSample = colTf.InverseTransformPoint(samplePos);
                float distSqr = localSample.sqrMagnitude;
                if (distSqr < combined * combined)
                {
                    Vector3 localDir = distSqr > 1e-8f
                        ? localSample / Mathf.Sqrt(distSqr)
                        : colTf.InverseTransformDirection(fallbackBack).normalized;
                    Vector3 worldPushed = colTf.TransformPoint(localDir * combined);
                    Vector3 delta = worldPushed - samplePos;
                    ApplyTipPush(headPos, delta, delta.normalized, invFactor, boneWorldLength, ref tipPos, ref accumulatedNormal);
                }
            }
        }

        // 2. Capsules
        if (bone.capsuleColliders != null)
        {
            for (int i = 0; i < bone.capsuleColliders.Length; i++)
            {
                SpringCapsuleCollider capsule = bone.capsuleColliders[i];
                if (capsule == null || !capsule.enabled || capsule.radius <= 0.0001f ||
                    (capsule.linkedRenderer != null && !capsule.linkedRenderer.enabled))
                {
                    continue;
                }

                Vector3 samplePos = Vector3.LerpUnclamped(headPos, tipPos, factor);
                Transform colTf = capsule.transform;
                float colScale = Mathf.Max(0.01f, colTf.lossyScale.x);
                float localRadius = boneRadiusModel * (boneScale / colScale);
                float combined = capsule.radius + localRadius;

                Vector3 localSample = colTf.InverseTransformPoint(samplePos);
                float clampedY = Mathf.Clamp(localSample.y, 0f, capsule.height);
                Vector3 axisPoint = new Vector3(0f, clampedY, 0f);
                Vector3 radial = localSample - axisPoint;
                float distSqr = radial.sqrMagnitude;
                if (distSqr < combined * combined)
                {
                    Vector3 localDir = distSqr > 1e-8f
                        ? radial / Mathf.Sqrt(distSqr)
                        : Vector3.ProjectOnPlane(colTf.InverseTransformDirection(fallbackBack), Vector3.up).normalized;
                    if (localDir.sqrMagnitude < 1e-6f)
                    {
                        localDir = colTf.InverseTransformDirection(fallbackBack).normalized;
                    }

                    Vector3 worldPushed = colTf.TransformPoint(axisPoint + localDir * combined);
                    Vector3 delta = worldPushed - samplePos;
                    ApplyTipPush(headPos, delta, delta.normalized, invFactor, boneWorldLength, ref tipPos, ref accumulatedNormal);
                }
            }
        }

        // 3. One-sided back-shield panels evaluated with exact scale compensation (avoiding package scale bug)
        for (int i = 0; i < cachedBackPanels.Count; i++)
        {
            SpringPanelCollider panel = cachedBackPanels[i];
            if (panel == null || !panel.enabled || (panel.linkedRenderer != null && !panel.linkedRenderer.enabled))
            {
                continue;
            }

            Vector3 samplePos = Vector3.LerpUnclamped(headPos, tipPos, factor);
            Transform colTf = panel.transform;
            float colScale = Mathf.Max(0.01f, colTf.lossyScale.x);
            float localRadius = boneRadiusModel * (boneScale / colScale);
            float hw = panel.width * 0.5f;
            float hh = panel.height * 0.5f;

            Vector3 localSample = colTf.InverseTransformPoint(samplePos);
            if (Mathf.Abs(localSample.x) < hw + localRadius &&
                Mathf.Abs(localSample.y) < hh + localRadius &&
                localSample.z < localRadius)
            {
                bool insideRect = Mathf.Abs(localSample.x) <= hw && Mathf.Abs(localSample.y) <= hh;
                Vector3 pushedLocal;
                if (!insideRect)
                {
                    if (localSample.z <= -0.08f)
                    {
                        continue;
                    }

                    Vector3 corner = new Vector3(
                        Mathf.Clamp(localSample.x, -hw, hw),
                        Mathf.Clamp(localSample.y, -hh, hh),
                        0f);
                    Vector3 diff = localSample - corner;
                    pushedLocal = diff.sqrMagnitude > 1e-8f
                        ? corner + diff.normalized * localRadius
                        : new Vector3(localSample.x, localSample.y, localRadius);
                }
                else
                {
                    pushedLocal = new Vector3(localSample.x, localSample.y, localRadius);
                }

                Vector3 worldPushed = colTf.TransformPoint(pushedLocal);
                ApplyTipPush(headPos, worldPushed - samplePos, colTf.forward, invFactor, boneWorldLength, ref tipPos, ref accumulatedNormal);
            }
        }
    }

    private static void ResolveGroundFloor(
        Vector3 headPos,
        float maxFactor,
        float worldRadius,
        float groundHeight,
        float boneWorldLength,
        ref Vector3 tipPos,
        ref Vector3 accumulatedNormal)
    {
        float factor = maxFactor > 1.05f ? maxFactor : 1.00f;
        Vector3 lowestPos = Vector3.LerpUnclamped(headPos, tipPos, factor);
        float minY = groundHeight + worldRadius;
        if (lowestPos.y < minY)
        {
            Vector3 lifted = new Vector3(lowestPos.x, minY, lowestPos.z);
            ApplyTipPush(headPos, lifted - lowestPos, Vector3.up, 1f / Mathf.Max(factor, 0.5f), boneWorldLength, ref tipPos, ref accumulatedNormal);
        }
    }

    private static void ApplyTipPush(
        Vector3 headPos,
        Vector3 pushDelta,
        Vector3 pushNormal,
        float invFactor,
        float boneWorldLength,
        ref Vector3 tipPos,
        ref Vector3 accumulatedNormal)
    {
        accumulatedNormal += pushNormal;
        Vector3 candidate = tipPos + pushDelta * invFactor - headPos;
        if (candidate.sqrMagnitude > 1e-8f)
        {
            tipPos = headPos + candidate.normalized * boneWorldLength;
        }
    }

    private static void SyncVerletTipAfterProjection(
        SpringBone bone,
        Vector3 resolvedTip,
        Vector3 initialTip,
        Vector3 pushNormal)
    {
        CurrTipPosField?.SetValue(bone, resolvedTip);
        if (PrevTipPosField != null)
        {
            Vector3 prevTip = (Vector3)PrevTipPosField.GetValue(bone);
            Vector3 stepVel = resolvedTip - prevTip;
            Vector3 normal = pushNormal.sqrMagnitude > 1e-6f
                ? pushNormal.normalized
                : (resolvedTip - initialTip).normalized;
            float inward = Vector3.Dot(stepVel, normal);
            if (inward < 0f)
            {
                Vector3 tangentVel = stepVel - inward * normal;
                Vector3 dampedVel = tangentVel * 0.7f;
                PrevTipPosField.SetValue(bone, resolvedTip - dampedVel);
            }
        }
    }

    private static bool NeedsRuntimeAutoHeal(SpringManager manager, ClothRigProfile profile)
    {
        if (manager == null || manager.springBones == null)
        {
            return true;
        }

        if (manager.friction < 0.98f || manager.enableLengthLimits)
        {
            return true;
        }

        Dictionary<string, SpringBone> boneByName = new Dictionary<string, SpringBone>();
        foreach (SpringBone bone in manager.springBones)
        {
            if (bone != null && !boneByName.ContainsKey(bone.name))
            {
                boneByName[bone.name] = bone;
            }
        }

        foreach (string nonSimName in profile.NonSimulatedBoneNames)
        {
            if (boneByName.ContainsKey(nonSimName))
            {
                return true;
            }
        }

        foreach (ChainBoneSpec spec in profile.ChainSpecs)
        {
            if (!boneByName.TryGetValue(spec.BoneName, out SpringBone sb) ||
                (sb.panelColliders != null && sb.panelColliders.Length > 0) ||
                (sb.lengthLimitTargets != null && sb.lengthLimitTargets.Length > 0) ||
                sb.stiffnessForce > spec.StiffnessForce + 1f ||
                Mathf.Abs(sb.radius - spec.Radius) > 0.004f)
            {
                return true;
            }
        }

        Transform[] allTransforms = manager.GetComponentsInChildren<Transform>(true);
        HashSet<string> transformNames = new HashSet<string>();
        foreach (Transform t in allTransforms)
        {
            if (t != null)
            {
                transformNames.Add(t.name);
            }
        }

        foreach (string colName in CapeColliderNames)
        {
            if (!transformNames.Contains(colName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Shared configuration entry point used by both Editor auto-fix and runtime auto-heal.
    /// Removes non-simulated leaf/stem SpringBones, sets critically-damped SpringBones,
    /// and creates/updates all 16 anatomical colliders plus component presets.
    /// </summary>
    protected static SpringManager ConfigureClothRigFor<TCloth>(
        GameObject bossRoot,
        ClothRigProfile profile,
        bool applyPresetValues = true,
        Action<UnityEngine.Object, string> recordUndo = null,
        Action<UnityEngine.Object> destroyComponent = null) where TCloth : IgnisClothMotionBase
    {
        if (bossRoot == null || profile == null)
        {
            return null;
        }

        void Record(UnityEngine.Object obj, string label)
        {
            if (obj != null && recordUndo != null)
            {
                recordUndo(obj, label);
            }
        }

        void DestroyObj(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (destroyComponent != null)
            {
                destroyComponent(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        // Remove any mismatched IgnisClothMotionBase variant on this boss root so two ForceProviders never fight.
        foreach (IgnisClothMotionBase existingMotion in bossRoot.GetComponents<IgnisClothMotionBase>())
        {
            if (existingMotion != null && !(existingMotion is TCloth))
            {
                DestroyObj(existingMotion);
            }
        }

        SpringManager manager = bossRoot.GetComponentInChildren<SpringManager>(true);
        if (manager == null)
        {
            manager = bossRoot.AddComponent<SpringManager>();
            Record(manager, "Add SpringManager");
        }

        Dictionary<string, Transform> boneMap = BuildTransformLookup(bossRoot.transform);
        Renderer linkedRenderer = bossRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (linkedRenderer == null)
        {
            linkedRenderer = bossRoot.GetComponentInChildren<Renderer>(true);
        }

        // Derive stable anatomical axes directly from skeleton landmarks
        Transform rootTf = bossRoot.transform;
        Vector3 worldUp = rootTf.up;
        if (boneMap.TryGetValue("mixamorig:Hips", out Transform refHips) &&
            boneMap.TryGetValue("mixamorig:Neck", out Transform refNeck) &&
            (refNeck.position - refHips.position).sqrMagnitude > 1e-4f)
        {
            worldUp = (refNeck.position - refHips.position).normalized;
        }

        Vector3 worldLeft = rootTf.right;
        if (boneMap.TryGetValue("mixamorig:LeftShoulder", out Transform refLeftSh) &&
            boneMap.TryGetValue("mixamorig:RightShoulder", out Transform refRightSh))
        {
            Vector3 span = Vector3.ProjectOnPlane(refLeftSh.position - refRightSh.position, worldUp);
            if (span.sqrMagnitude > 1e-4f)
            {
                worldLeft = span.normalized;
            }
        }

        Vector3 worldBack = -rootTf.forward;
        if (profile.BackAxisBoneNames != null && boneMap.TryGetValue("mixamorig:Spine2", out Transform refSpine2))
        {
            foreach (string backBoneName in profile.BackAxisBoneNames)
            {
                if (boneMap.TryGetValue(backBoneName, out Transform refCapeCenter))
                {
                    Vector3 backVec = Vector3.ProjectOnPlane(refCapeCenter.position - refSpine2.position, worldUp);
                    if (backVec.sqrMagnitude > 1e-4f)
                    {
                        worldBack = backVec.normalized;
                        break;
                    }
                }
            }
        }

        // 1. Remove SpringBone from fixed hair stem and all terminal leaf endpoints
        foreach (string nonSimName in profile.NonSimulatedBoneNames)
        {
            if (boneMap.TryGetValue(nonSimName, out Transform nonSimTf))
            {
                SpringBone[] staleBones = nonSimTf.GetComponents<SpringBone>();
                foreach (SpringBone staleBone in staleBones)
                {
                    DestroyObj(staleBone);
                }
            }
        }

        // 2. Build/update 16 anatomical collision volumes (spheres, torso/shoulder/limb capsules, back shield panels)
        SpringSphereCollider colHead = EnsureSphereCollider(
            boneMap, "mixamorig:Head", profile.HeadAimChildName, "CapeCol_Head",
            profile.HeadMidpointFactor, Vector3.zero, profile.HeadRadius,
            worldUp, worldBack, profile.HeadUpOffset, profile.HeadBackOffset, linkedRenderer, Record, DestroyObj);

        SpringSphereCollider colNeck = EnsureSphereCollider(
            boneMap, "mixamorig:Neck", "mixamorig:Head", "CapeCol_Neck",
            0.50f, Vector3.zero, profile.NeckRadius,
            worldUp, worldBack, profile.NeckUpOffset, 0f, linkedRenderer, Record, DestroyObj);

        SpringSphereCollider colLeftShoulder = EnsureSphereCollider(
            boneMap, "mixamorig:LeftShoulder", "mixamorig:LeftArm", "CapeCol_LeftShoulder",
            0.62f, Vector3.zero, 0.096f, worldUp, worldBack, 0.015f, 0f, linkedRenderer, Record, DestroyObj);

        SpringSphereCollider colRightShoulder = EnsureSphereCollider(
            boneMap, "mixamorig:RightShoulder", "mixamorig:RightArm", "CapeCol_RightShoulder",
            0.62f, Vector3.zero, 0.096f, worldUp, worldBack, 0.015f, 0f, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colUpperChest = EnsureHorizontalCapsuleCollider(
            boneMap, "mixamorig:Spine2", "mixamorig:Neck", "CapeCol_UpperChest",
            0.45f, 0f, 0f, 0.078f, profile.UpperChestRadius, worldUp, worldLeft, worldBack, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colTorso = EnsureHorizontalCapsuleCollider(
            boneMap, "mixamorig:Spine1", "mixamorig:Spine2", "CapeCol_Torso",
            0.35f, 0f, profile.TorsoBackOffset, 0.055f, profile.TorsoRadius, worldUp, worldLeft, worldBack, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colHips = EnsureHorizontalCapsuleCollider(
            boneMap, "mixamorig:Hips", null, "CapeCol_Hips",
            0f, 0.010f, -0.002f, 0.060f, 0.095f, worldUp, worldLeft, worldBack, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colLeftArm = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:LeftArm", "mixamorig:LeftForeArm", "CapeCol_LeftArm",
            0.080f, profile.ArmFallbackHeight, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colRightArm = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:RightArm", "mixamorig:RightForeArm", "CapeCol_RightArm",
            0.080f, profile.ArmFallbackHeight, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colLeftThigh = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", "CapeCol_LeftThigh",
            0.096f, profile.ThighFallbackHeight, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colRightThigh = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:RightUpLeg", "mixamorig:RightLeg", "CapeCol_RightThigh",
            0.096f, profile.ThighFallbackHeight, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colLeftShin = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:LeftLeg", "mixamorig:LeftFoot", "CapeCol_LeftShin",
            0.085f, profile.ShinFallbackHeight, linkedRenderer, Record, DestroyObj);

        SpringCapsuleCollider colRightShin = EnsureLimbCapsuleCollider(
            boneMap, "mixamorig:RightLeg", "mixamorig:RightFoot", "CapeCol_RightShin",
            0.085f, profile.ShinFallbackHeight, linkedRenderer, Record, DestroyObj);

        // Keep back-shield SpringPanelColliders in the hierarchy for cachedBackPanels
        // (left unassigned on SpringBone.panelColliders so Unity package's unscaled length projection never fires).
        EnsurePanelCollider(
            boneMap, "mixamorig:Head", "CapeCol_HeadBackPanel",
            profile.HeadBackPanelUpOffset, 0.025f, 0.26f, 0.42f, worldUp, worldBack, linkedRenderer, Record, DestroyObj);

        EnsurePanelCollider(
            boneMap, "mixamorig:Spine1", "CapeCol_TorsoBackPanel",
            0.070f, 0.030f, 0.36f, 0.42f, worldUp, worldBack, linkedRenderer, Record, DestroyObj);

        EnsurePanelCollider(
            boneMap, "mixamorig:Hips", "CapeCol_PelvisBackPanel",
            -0.120f, 0.028f, 0.34f, 0.44f, worldUp, worldBack, linkedRenderer, Record, DestroyObj);

        SpringSphereCollider[] hatSpheres = CompactArray(colHead, colNeck, colLeftShoulder, colRightShoulder);
        SpringCapsuleCollider[] hatCapsules = CompactArray(colUpperChest, colTorso, colHips, colLeftArm, colRightArm);

        SpringSphereCollider[] capeSpheres = CompactArray(colHead, colNeck, colLeftShoulder, colRightShoulder);
        SpringCapsuleCollider[] capeRootCapsules = CompactArray(
            colUpperChest, colTorso, colHips,
            colLeftThigh, colRightThigh, colLeftShin, colRightShin);
        SpringCapsuleCollider[] capeFlowCapsules = CompactArray(
            colUpperChest, colTorso, colHips,
            colLeftArm, colRightArm,
            colLeftThigh, colRightThigh, colLeftShin, colRightShin);

        // 3. Configure simulated SpringBones for smooth silk drape without shoulder-whip coupling
        foreach (ChainBoneSpec spec in profile.ChainSpecs)
        {
            if (!boneMap.TryGetValue(spec.BoneName, out Transform boneTf))
            {
                Debug.LogWarning($"{profile.LogTag} Bone '{spec.BoneName}' not found on {bossRoot.name}.");
                continue;
            }

            SpringBone springBone = boneTf.GetComponent<SpringBone>();
            if (springBone == null)
            {
                springBone = boneTf.gameObject.AddComponent<SpringBone>();
                Record(springBone, "Add SpringBone");
            }
            else
            {
                Record(springBone, "Configure SpringBone");
            }

            if (applyPresetValues)
            {
                springBone.stiffnessForce = spec.StiffnessForce;
                springBone.dragForce = spec.DragForce;
                springBone.springForce = spec.SpringForce;
                springBone.radius = spec.Radius;
                springBone.windInfluence = spec.WindInfluence;
            }

            if (!string.IsNullOrEmpty(spec.PivotName) && springBone.pivotNode == null)
            {
                if (boneMap.TryGetValue(spec.PivotName, out Transform pivotTf))
                {
                    springBone.pivotNode = pivotTf;
                }
            }

            springBone.lengthLimitTargets = Array.Empty<Transform>();
            springBone.panelColliders = Array.Empty<SpringPanelCollider>();

            if (spec.Region == ClothRegion.HatTail)
            {
                springBone.sphereColliders = hatSpheres;
                springBone.capsuleColliders = hatCapsules;
            }
            else
            {
                springBone.sphereColliders = capeSpheres;
                springBone.capsuleColliders = spec.IsShoulderRoot ? capeRootCapsules : capeFlowCapsules;
            }
        }

        // 4. Configure SpringManager global simulation parameters & sanitize bone collider arrays
        Record(manager, "Configure SpringManager");
        if (applyPresetValues)
        {
            manager.simulationFrameRate = 60;
            manager.dynamicRatio = 1.0f;
            manager.gravity = new Vector3(0f, -22f, 0f);
            manager.bounce = 0f;
            manager.friction = 1.0f;
            manager.enableAngleLimits = false;
            manager.enableCollision = true;
            manager.enableLengthLimits = false;
            manager.collideWithGround = false;
            manager.groundHeight = 0f;
        }

        manager.FindSpringBones(true);

        if (manager.springBones != null)
        {
            foreach (SpringBone sb in manager.springBones)
            {
                if (sb == null) continue;
                sb.sphereColliders ??= Array.Empty<SpringSphereCollider>();
                sb.capsuleColliders ??= Array.Empty<SpringCapsuleCollider>();
                sb.panelColliders ??= Array.Empty<SpringPanelCollider>();
                sb.lengthLimitTargets ??= Array.Empty<Transform>();
            }
        }

        manager.CleanUpBoneColliders();
        manager.UpdateBoneIsAnimatedStates(Array.Empty<string>());

        TCloth cloth = bossRoot.GetComponent<TCloth>();
        bool isNewComponent = cloth == null;
        if (isNewComponent)
        {
            cloth = bossRoot.AddComponent<TCloth>();
            Record(cloth, "Add " + typeof(TCloth).Name);
            cloth.footstepCycleOffset = profile.PresetFootstepCycleOffset;
            ApplyMotionPresets(cloth, profile, applyFootstepCycleOffset: true);
        }
        else if (applyPresetValues)
        {
            Record(cloth, "Apply " + typeof(TCloth).Name + " Presets");
            ApplyMotionPresets(cloth, profile, applyFootstepCycleOffset: profile.ApplyFootstepCycleOffsetOnUpdate);
        }

        return manager;
    }

    private static void ApplyMotionPresets(IgnisClothMotionBase cloth, ClothRigProfile profile, bool applyFootstepCycleOffset)
    {
        if (applyFootstepCycleOffset)
        {
            cloth.footstepCycleOffset = profile.PresetFootstepCycleOffset;
        }

        cloth.stepLateralSwayStrength = profile.PresetStepLateralSwayStrength;
        cloth.stepVerticalBounceStrength = 0f;
        cloth.stepWaveLagRadians = 1.35f;
        cloth.aerodynamicLiftStrength = 0f;
        cloth.wingSpreadStrength = profile.PresetWingSpreadStrength;
        cloth.idleBreezeStrength = 0.04f;
        cloth.hatTailForceScale = profile.PresetHatTailForceScale;
        cloth.fallStabilizationStrength = 0.65f;
        cloth.turnDampingThresholdDegPerSec = 45f;
        cloth.maxExtraTurnDrag = 0.28f;
    }

    private static Dictionary<string, Transform> BuildTransformLookup(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> map = new Dictionary<string, Transform>(transforms.Length);
        foreach (Transform t in transforms)
        {
            if (t != null && !map.ContainsKey(t.name))
            {
                map[t.name] = t;
            }
        }

        return map;
    }

    private static Transform EnsureColliderChildTransform(
        Transform parentBone,
        string childName,
        Action<UnityEngine.Object, string> recordUndo)
    {
        Transform child = parentBone.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            child = go.transform;
            child.SetParent(parentBone, false);
            recordUndo?.Invoke(go, "Create Cloth Collider");
        }
        else
        {
            recordUndo?.Invoke(child, "Update Cloth Collider Transform");
        }

        child.localScale = Vector3.one;
        return child;
    }

    private static SpringSphereCollider EnsureSphereCollider(
        Dictionary<string, Transform> boneMap,
        string parentBoneName,
        string aimChildName,
        string colliderName,
        float midpointFactor,
        Vector3 fallbackLocalPos,
        float radius,
        Vector3 worldUp,
        Vector3 worldBack,
        float upOffset,
        float backOffset,
        Renderer linkedRenderer,
        Action<UnityEngine.Object, string> recordUndo,
        Action<UnityEngine.Object> destroyObj)
    {
        if (!boneMap.TryGetValue(parentBoneName, out Transform parentBone))
        {
            return null;
        }

        Transform colTf = EnsureColliderChildTransform(parentBone, colliderName, recordUndo);

        Vector3 localPos = fallbackLocalPos;
        if (!string.IsNullOrEmpty(aimChildName) && boneMap.TryGetValue(aimChildName, out Transform childBone))
        {
            localPos = parentBone.InverseTransformPoint(childBone.position) * midpointFactor;
        }

        localPos += ApplyDirectionalOffsets(parentBone, worldUp, upOffset, worldBack, backOffset);
        colTf.localPosition = localPos;
        colTf.localRotation = Quaternion.identity;

        SpringSphereCollider sphere = EnsureColliderComponent<SpringSphereCollider>(
            colTf, "SpringSphereCollider", recordUndo, destroyObj);
        sphere.radius = radius;
        sphere.linkedRenderer = linkedRenderer;
        return sphere;
    }

    private static SpringCapsuleCollider EnsureHorizontalCapsuleCollider(
        Dictionary<string, Transform> boneMap,
        string parentBoneName,
        string upperAimBoneName,
        string colliderName,
        float aimFactor,
        float upOffset,
        float backOffset,
        float halfSpan,
        float radius,
        Vector3 worldUp,
        Vector3 worldLeft,
        Vector3 worldBack,
        Renderer linkedRenderer,
        Action<UnityEngine.Object, string> recordUndo,
        Action<UnityEngine.Object> destroyObj)
    {
        if (!boneMap.TryGetValue(parentBoneName, out Transform parentBone))
        {
            return null;
        }

        Transform colTf = EnsureColliderChildTransform(parentBone, colliderName, recordUndo);

        Vector3 centerLocal = Vector3.zero;
        if (!string.IsNullOrEmpty(upperAimBoneName) && boneMap.TryGetValue(upperAimBoneName, out Transform upperBone))
        {
            centerLocal = parentBone.InverseTransformPoint(upperBone.position) * aimFactor;
        }

        centerLocal += ApplyDirectionalOffsets(parentBone, worldUp, upOffset, worldBack, backOffset);

        Vector3 axisLocal = SafeInverseDirection(parentBone, worldLeft, Vector3.right);
        colTf.localPosition = centerLocal - axisLocal * halfSpan;
        colTf.localRotation = Quaternion.FromToRotation(Vector3.up, axisLocal);

        SpringCapsuleCollider capsule = EnsureColliderComponent<SpringCapsuleCollider>(
            colTf, "SpringCapsuleCollider", recordUndo, destroyObj);
        capsule.radius = radius;
        capsule.height = Mathf.Max(0.02f, halfSpan * 2f);
        capsule.linkedRenderer = linkedRenderer;
        return capsule;
    }

    private static SpringCapsuleCollider EnsureLimbCapsuleCollider(
        Dictionary<string, Transform> boneMap,
        string startBoneName,
        string endBoneName,
        string colliderName,
        float radius,
        float fallbackHeight,
        Renderer linkedRenderer,
        Action<UnityEngine.Object, string> recordUndo,
        Action<UnityEngine.Object> destroyObj)
    {
        if (!boneMap.TryGetValue(startBoneName, out Transform startBone))
        {
            return null;
        }

        Transform colTf = EnsureColliderChildTransform(startBone, colliderName, recordUndo);

        Vector3 endLocal = new Vector3(0f, fallbackHeight, 0f);
        if (boneMap.TryGetValue(endBoneName, out Transform endBone))
        {
            endLocal = startBone.InverseTransformPoint(endBone.position);
        }

        float height = endLocal.magnitude > 0.05f ? endLocal.magnitude : fallbackHeight;
        Vector3 dir = endLocal.sqrMagnitude > 1e-5f ? endLocal.normalized : Vector3.up;

        colTf.localPosition = Vector3.zero;
        colTf.localRotation = Quaternion.FromToRotation(Vector3.up, dir);

        SpringCapsuleCollider capsule = EnsureColliderComponent<SpringCapsuleCollider>(
            colTf, "SpringCapsuleCollider", recordUndo, destroyObj);
        capsule.radius = radius;
        capsule.height = height;
        capsule.linkedRenderer = linkedRenderer;
        return capsule;
    }

    private static SpringPanelCollider EnsurePanelCollider(
        Dictionary<string, Transform> boneMap,
        string parentBoneName,
        string colliderName,
        float upOffset,
        float backOffset,
        float width,
        float height,
        Vector3 worldUp,
        Vector3 worldBack,
        Renderer linkedRenderer,
        Action<UnityEngine.Object, string> recordUndo,
        Action<UnityEngine.Object> destroyObj)
    {
        if (!boneMap.TryGetValue(parentBoneName, out Transform parentBone))
        {
            return null;
        }

        Transform colTf = EnsureColliderChildTransform(parentBone, colliderName, recordUndo);

        Vector3 localUp = SafeInverseDirection(parentBone, worldUp, Vector3.up);
        Vector3 localBack = SafeInverseDirection(parentBone, worldBack, Vector3.back);

        colTf.localPosition = localUp * upOffset + localBack * backOffset;
        colTf.localRotation = Quaternion.LookRotation(localBack, localUp);

        SpringPanelCollider panel = EnsureColliderComponent<SpringPanelCollider>(
            colTf, "SpringPanelCollider", recordUndo, destroyObj);
        panel.width = width;
        panel.height = height;
        panel.linkedRenderer = linkedRenderer;
        return panel;
    }

    private static Vector3 SafeInverseDirection(Transform space, Vector3 worldDir, Vector3 fallback)
    {
        Vector3 localDir = space.InverseTransformDirection(worldDir);
        return localDir.sqrMagnitude > 1e-6f ? localDir.normalized : fallback;
    }

    private static Vector3 ApplyDirectionalOffsets(
        Transform space,
        Vector3 worldUp,
        float upOffset,
        Vector3 worldBack,
        float backOffset)
    {
        Vector3 offset = Vector3.zero;
        if (Mathf.Abs(upOffset) > 1e-5f)
        {
            offset += SafeInverseDirection(space, worldUp, Vector3.up) * upOffset;
        }

        if (Mathf.Abs(backOffset) > 1e-5f)
        {
            offset += SafeInverseDirection(space, worldBack, Vector3.back) * backOffset;
        }

        return offset;
    }

    private static T EnsureColliderComponent<T>(
        Transform colTf,
        string label,
        Action<UnityEngine.Object, string> recordUndo,
        Action<UnityEngine.Object> destroyObj) where T : Component
    {
        // Strip incompatible spring-collider components when a helper object changes shape
        RemoveMismatchedColliders<SpringSphereCollider>(colTf, typeof(T), destroyObj);
        RemoveMismatchedColliders<SpringCapsuleCollider>(colTf, typeof(T), destroyObj);
        RemoveMismatchedColliders<SpringPanelCollider>(colTf, typeof(T), destroyObj);

        T component = colTf.GetComponent<T>();
        if (component == null)
        {
            component = colTf.gameObject.AddComponent<T>();
            recordUndo?.Invoke(component, "Add " + label);
        }
        else
        {
            recordUndo?.Invoke(component, "Update " + label);
        }

        return component;
    }

    private static void RemoveMismatchedColliders<TExisting>(
        Transform colTf,
        Type targetType,
        Action<UnityEngine.Object> destroyObj) where TExisting : Component
    {
        if (typeof(TExisting) == targetType || destroyObj == null)
        {
            return;
        }

        foreach (TExisting existing in colTf.GetComponents<TExisting>())
        {
            destroyObj(existing);
        }
    }

    private static T[] CompactArray<T>(params T[] items) where T : UnityEngine.Object
    {
        List<T> result = new List<T>(items.Length);
        foreach (T item in items)
        {
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }
}
