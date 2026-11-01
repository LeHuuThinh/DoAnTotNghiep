using System;
using UnityEditor;
using UnityEngine;

public class IgnisWeaponAligner : EditorWindow
{
    private GameObject bossObj;
    private Transform rightForeArm;
    private Transform rightHand;
    private Transform weaponSocket; // Hand_R_Weapon
    private Transform swordVisual;  // KiemSiuNhanDo
    private AnimationClip slashClip;

    private float previewTime = 0.5f; // Normalized 0..1
    private bool isPreviewing = false;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 initialLocalScale;

    // Detected sword blade axis
    private Vector3 bladeLocalAxis = Vector3.forward;

    [MenuItem("Tools/RedIgnis/0. Khôi Phục Dáng T-Pose & Kiếm Ban Đầu (Reset to T-Pose)")]
    public static void ResetToOriginalTPose()
    {
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }

        GameObject boss = Selection.activeGameObject;
        if (boss == null || !boss.name.Contains("Ignis"))
        {
            boss = GameObject.Find("Boss_RedIgnis");
        }

        if (boss != null)
        {
            // 1. Revert Prefab instance to original to get clean properties
            if (PrefabUtility.IsPartOfPrefabInstance(boss))
            {
                PrefabUtility.RevertPrefabInstance(boss, InteractionMode.AutomatedAction);
            }

            // 2. Rebind Animator and force canonical Humanoid T-Pose
            Animator anim = boss.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Tắt Animator tạm thời để Unity Edit Mode không tự chạy frame 0 của hoạt ảnh chém
                anim.enabled = false;
                anim.Rebind();

                if (anim.avatar != null && anim.avatar.isValid && anim.avatar.isHuman)
                {
                    HumanPoseHandler handler = new HumanPoseHandler(anim.avatar, anim.transform);
                    HumanPose pose = new HumanPose
                    {
                        bodyPosition = new Vector3(0, anim.transform.localPosition.y, 0),
                        bodyRotation = Quaternion.identity,
                        muscles = new float[HumanTrait.MuscleCount] // Tất cả muscles = 0 là dáng T-Pose chuẩn tuyệt đối!
                    };
                    handler.SetHumanPose(ref pose);
                }
            }

            // 3. Restore exact Hand_R_Weapon values
            Transform[] all = boss.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == "Hand_R_Weapon")
                {
                    Undo.RecordObject(t, "Restore Hand_R_Weapon");
                    t.localPosition = new Vector3(0.175f, 0.094f, -0.074f);
                    t.localRotation = new Quaternion(-0.30621836f, -0.33024502f, 0.5933961f, 0.6671204f);
                    t.localScale = new Vector3(0.8f, 0.8f, 1.0f);
                    EditorUtility.SetDirty(t);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boss.scene);
            SceneView.RepaintAll();
            Debug.Log("<color=green>[RedIgnis] THÀNH CÔNG!</color> Đã ép 100% về dáng T-Pose chuẩn tuyệt đối (Canonical T-Pose) và vị trí kiếm ban đầu!");
        }
    }

    [MenuItem("Tools/RedIgnis/Cân Chỉnh Kiếm Boss (Ignis Weapon Aligner)")]
    public static void ShowWindow()
    {
        var window = GetWindow<IgnisWeaponAligner>("Ignis Weapon Aligner");
        window.minSize = new Vector2(420, 620);
        window.FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        StopPreview();
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void FindReferences()
    {
        if (bossObj == null)
        {
            bossObj = Selection.activeGameObject;
            if (bossObj == null || !bossObj.name.Contains("Ignis"))
            {
                bossObj = GameObject.Find("Boss_RedIgnis");
            }
        }

        if (bossObj != null)
        {
            Transform[] all = bossObj.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name.Contains("RightForeArm")) rightForeArm = t;
                else if (t.name == "mixamorig:RightHand") rightHand = t;
                else if (t.name == "Hand_R_Weapon") weaponSocket = t;
                else if (t.name.Contains("KiemSiuNhanDo") || t.name.Contains("Sword")) swordVisual = t;
            }

            if (weaponSocket != null)
            {
                initialLocalPos = weaponSocket.localPosition;
                initialLocalRot = weaponSocket.localRotation;
                initialLocalScale = weaponSocket.localScale;
            }

            DetectBladeAxis();
        }

        if (slashClip == null)
        {
            // Load AttackSlashRI
            slashClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Characters/Boss/IgnisDo/animation/Animation/AttackSlashRI.anim");
            if (slashClip == null)
            {
                slashClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Characters/Boss/IgnisDo/animation/AttackSlashRI.fbx");
            }
        }
    }

    private void DetectBladeAxis()
    {
        if (swordVisual == null) return;
        MeshFilter mf = swordVisual.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            Vector3 size = b.size;

            // Longest dimension is blade axis
            if (size.x >= size.y && size.x >= size.z)
            {
                bladeLocalAxis = (b.max.x > Mathf.Abs(b.min.x)) ? Vector3.right : Vector3.left;
            }
            else if (size.y >= size.x && size.y >= size.z)
            {
                bladeLocalAxis = (b.max.y > Mathf.Abs(b.min.y)) ? Vector3.up : Vector3.down;
            }
            else
            {
                bladeLocalAxis = (b.max.z > Mathf.Abs(b.min.z)) ? Vector3.forward : Vector3.back;
            }
            Debug.Log($"<color=cyan>[KiemSiuNhanDo Mesh]</color> bounds center={b.center}, size={b.size}, min={b.min}, max={b.max}, bladeLocalAxis={bladeLocalAxis}");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("⚔️ BỘ CÂN CHỈNH KIẾM RED IGNIS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Xương cổ tay của RedIgnis vốn bị xoay vặn -45.8° quanh trục cẳng tay trong file FBX gốc.\n" +
            "Tool này giúp bạn tua tới dáng chém thực tế và nắn thẳng cây kiếm theo cánh tay chỉ với 1 cú click!",
            MessageType.Info);

        EditorGUILayout.Space(6);
        bossObj = (GameObject)EditorGUILayout.ObjectField("Boss Object", bossObj, typeof(GameObject), true);
        slashClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", slashClip, typeof(AnimationClip), false);

        if (bossObj == null || weaponSocket == null)
        {
            EditorGUILayout.HelpBox("Chưa tìm thấy Boss_RedIgnis hoặc Hand_R_Weapon trong Scene!", MessageType.Warning);
            if (GUILayout.Button("Tìm Lại Tham Chiếu (Refresh)", GUILayout.Height(30)))
            {
                FindReferences();
            }
            return;
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
        if (GUILayout.Button("🔄 1-CLICK: TRỞ VỀ DÁNG T-POSE & VỊ TRÍ KIẾM GỐC", GUILayout.Height(32)))
        {
            ResetToOriginalTPose();
            FindReferences();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f);
        if (GUILayout.Button("🚀 1-CLICK: TỰ ĐỘNG CẤU HÌNH RED IGNIS (CONTROLLER + ANIMATOR + VFX)", GUILayout.Height(34)))
        {
            SetupRedIgnisAnimatorAndPrefabs.ExecuteFullSetup();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("1. XEM TRƯỚC HOẠT ẢNH CHÉM (PREVIEW SLASH)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool prev = EditorGUILayout.Toggle("Bật Preview Hoạt Ảnh", isPreviewing);
        if (EditorGUI.EndChangeCheck())
        {
            if (prev) StartPreview();
            else StopPreview();
        }

        if (isPreviewing && slashClip != null)
        {
            EditorGUI.BeginChangeCheck();
            previewTime = EditorGUILayout.Slider("Frame / Thời Điểm Chém", previewTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                SamplePose();
            }

            float currentSec = previewTime * slashClip.length;
            int currentFrame = Mathf.RoundToInt(previewTime * slashClip.frameRate * slashClip.length);
            EditorGUILayout.LabelField($"Thời gian: {currentSec:F2}s / Khung hình: Frame {currentFrame} (Tổng {Mathf.RoundToInt(slashClip.frameRate * slashClip.length)} frames)", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Frame Đang Chém (Ảnh của bạn: ~0.45)"))
            {
                previewTime = 0.45f;
                SamplePose();
            }
            if (GUILayout.Button("Frame Bắt Đầu (0.1)"))
            {
                previewTime = 0.1f;
                SamplePose();
            }
            if (GUILayout.Button("Frame Vung Hết Cỡ (0.7)"))
            {
                previewTime = 0.7f;
                SamplePose();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("2. ĐO ĐẠC GÓC LỆCH SO VỚI CÁNH TAY", EditorStyles.boldLabel);
        
        float angleDiff = GetAngleDifference();
        GUI.color = angleDiff < 10f ? Color.green : (angleDiff < 30f ? Color.yellow : new Color(1f, 0.4f, 0.4f));
        EditorGUILayout.LabelField($"📐 Góc lệch hiện tại giữa Kiếm và Cẳng Tay: {angleDiff:F1}°", EditorStyles.boldLabel);
        GUI.color = Color.white;

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("🎯 1-CLICK: ĐƯA KIẾM VÀO TAY & NẮN THẲNG THEO CẲNG TAY", GUILayout.Height(38)))
        {
            AutoAlignBladeToForearm();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("✊ Hút Chuôi Kiếm Vào Giữa Nắm Đấm (Snap To Palm)", GUILayout.Height(28)))
        {
            SnapWeaponToPalm();
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("3. TINH CHỈNH GÓC XOAY (XOAY BÙ XƯƠNG CỔ TAY)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Xoay quanh trục cánh tay (Bù xoay vặn):", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ Xoay Trái 45°", GUILayout.Height(26))) RotateWeapon(0, -45f, 0);
        if (GUILayout.Button("◀ Trái 15°", GUILayout.Height(26))) RotateWeapon(0, -15f, 0);
        if (GUILayout.Button("Trái 5°", GUILayout.Height(26))) RotateWeapon(0, -5f, 0);
        if (GUILayout.Button("Phải 5°", GUILayout.Height(26))) RotateWeapon(0, 5f, 0);
        if (GUILayout.Button("Phải 15° ▶", GUILayout.Height(26))) RotateWeapon(0, 15f, 0);
        if (GUILayout.Button("Phải 45° ▶", GUILayout.Height(26))) RotateWeapon(0, 45f, 0);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▲ Ngẩng Kiếm 10°")) RotateWeapon(10f, 0, 0);
        if (GUILayout.Button("▼ Cắm Kiếm 10°")) RotateWeapon(-10f, 0, 0);
        if (GUILayout.Button("🔄 Lật Lưỡi Kiếm 90°")) RotateWeapon(0, 0, 90f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("4. THÔNG SỐ HIỆN TẠI TRÊN HAND_R_WEAPON", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Vector3 curPos = EditorGUILayout.Vector3Field("Local Position", weaponSocket.localPosition);
        Vector3 curEuler = EditorGUILayout.Vector3Field("Local Rotation (Euler)", weaponSocket.localEulerAngles);
        Vector3 curScale = EditorGUILayout.Vector3Field("Local Scale (Giữ nguyên của bạn)", weaponSocket.localScale);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(weaponSocket, "Manual Adjust Weapon");
            weaponSocket.localPosition = curPos;
            weaponSocket.localEulerAngles = curEuler;
            weaponSocket.localScale = curScale;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(14);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("💾 LƯU VÀO PREFAB (APPLY PREFAB)", GUILayout.Height(36)))
        {
            SaveToPrefab();
        }
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Phục Hồi Ban Đầu", GUILayout.Height(36)))
        {
            Undo.RecordObject(weaponSocket, "Reset Weapon Transform");
            weaponSocket.localPosition = initialLocalPos;
            weaponSocket.localRotation = initialLocalRot;
            weaponSocket.localScale = initialLocalScale;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void StartPreview()
    {
        isPreviewing = true;
        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
        }
        SamplePose();
    }

    private void StopPreview()
    {
        isPreviewing = false;
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }
        if (bossObj != null)
        {
            Animator anim = bossObj.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
                anim.Rebind();
                if (anim.avatar != null && anim.avatar.isValid && anim.avatar.isHuman)
                {
                    HumanPoseHandler handler = new HumanPoseHandler(anim.avatar, anim.transform);
                    HumanPose pose = new HumanPose
                    {
                        bodyPosition = new Vector3(0, anim.transform.localPosition.y, 0),
                        bodyRotation = Quaternion.identity,
                        muscles = new float[HumanTrait.MuscleCount]
                    };
                    handler.SetHumanPose(ref pose);
                }
            }
        }
        SceneView.RepaintAll();
    }

    private void SamplePose()
    {
        if (bossObj == null || slashClip == null) return;
        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
        }
        float sampleTime = previewTime * slashClip.length;
        AnimationMode.SampleAnimationClip(bossObj, slashClip, sampleTime);
        SceneView.RepaintAll();
    }

    private void RotateWeapon(float pitch, float yaw, float roll)
    {
        if (weaponSocket == null) return;
        Undo.RecordObject(weaponSocket, "Rotate Weapon Socket");
        weaponSocket.localRotation *= Quaternion.Euler(pitch, yaw, roll);
        EditorUtility.SetDirty(weaponSocket);
        SceneView.RepaintAll();
    }

    private float GetAngleDifference()
    {
        if (rightForeArm == null || rightHand == null || swordVisual == null) return 0f;

        Vector3 forearmDir = (rightHand.position - rightForeArm.position).normalized;
        Vector3 swordDir = GetSwordWorldDirection();

        return Vector3.Angle(forearmDir, swordDir);
    }

    private Vector3 GetSwordWorldDirection()
    {
        if (swordVisual == null) return Vector3.forward;
        return (swordVisual.rotation * bladeLocalAxis).normalized;
    }

    private void AutoAlignBladeToForearm()
    {
        if (weaponSocket == null || rightForeArm == null || rightHand == null || swordVisual == null) return;

        // Bắt buộc phải bật preview hoạt ảnh chém trước khi nắn thẳng
        if (!isPreviewing)
        {
            previewTime = 0.45f;
            StartPreview();
        }

        Undo.RecordObject(weaponSocket, "Auto Align Blade to Forearm");

        // 0. Đưa socket vào đúng tâm lòng bàn tay (Snap chuôi kiếm vào giữa nắm đấm)
        Transform middleFinger = rightHand.Find("mixamorig:RightHandMiddle1");
        if (middleFinger != null)
        {
            weaponSocket.position = (rightHand.position + middleFinger.position) * 0.5f;
        }
        else
        {
            weaponSocket.localPosition = new Vector3(0.01f, 0.085f, 0f);
        }

        // 1. Get forearm direction in world space
        Vector3 targetDir = (rightHand.position - rightForeArm.position).normalized;

        // 2. Current sword direction in world space
        Vector3 currentDir = GetSwordWorldDirection();

        // 3. Rotation needed to align currentDir to targetDir
        Quaternion deltaWorldRot = Quaternion.FromToRotation(currentDir, targetDir);

        // 4. Apply delta rotation to weaponSocket in world space
        weaponSocket.rotation = deltaWorldRot * weaponSocket.rotation;

        EditorUtility.SetDirty(weaponSocket);
        SceneView.RepaintAll();

        Debug.Log($"<color=green>[IgnisWeaponAligner]</color> Đã gắn chuôi kiếm vào lòng bàn tay và nắn thẳng theo cẳng tay! Góc lệch mới: {GetAngleDifference():F1}°");
    }

    private void SnapWeaponToPalm()
    {
        if (weaponSocket == null || rightHand == null) return;
        Undo.RecordObject(weaponSocket, "Snap Weapon to Palm");
        Transform middleFinger = rightHand.Find("mixamorig:RightHandMiddle1");
        if (middleFinger != null)
        {
            weaponSocket.position = (rightHand.position + middleFinger.position) * 0.5f;
        }
        else
        {
            weaponSocket.localPosition = new Vector3(0.01f, 0.085f, 0f);
        }
        EditorUtility.SetDirty(weaponSocket);
        SceneView.RepaintAll();
        Debug.Log("<color=green>[IgnisWeaponAligner]</color> Đã hút chuôi kiếm vào giữa nắm đấm!");
    }

    private void SaveToPrefab()
    {
        if (bossObj == null || weaponSocket == null) return;

        // CHỈ LƯU DUY NHẤT Hand_R_Weapon vào Prefab, TUYỆT ĐỐI KHÔNG LƯU BONES CỦA MODEL
        if (PrefabUtility.IsPartOfAnyPrefab(bossObj))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(weaponSocket);
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(bossObj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                PrefabUtility.ApplyObjectOverride(weaponSocket, assetPath, InteractionMode.AutomatedAction);
                Debug.Log("<color=green>[IgnisWeaponAligner] HOÀN TẤT!</color> Đã lưu thông số Hand_R_Weapon vào Prefab mà không ảnh hưởng tới khung xương!");
                return;
            }
        }

        // Direct prefab asset
        EditorUtility.SetDirty(weaponSocket);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>[IgnisWeaponAligner] HOÀN TẤT!</color> Đã lưu Asset!");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (bossObj == null || rightForeArm == null || rightHand == null || swordVisual == null) return;

        Vector3 armStart = rightForeArm.position;
        Vector3 armEnd = rightHand.position;
        Vector3 forearmDir = (armEnd - armStart).normalized;

        Vector3 swordStart = swordVisual.position;
        Vector3 swordDir = GetSwordWorldDirection();

        // Draw forearm guideline (Cyan)
        Handles.color = Color.cyan;
        Handles.DrawLine(armStart, armEnd, 3f);
        Handles.DrawDottedLine(armEnd, armEnd + forearmDir * 2.5f, 4f);
        Handles.Label(armEnd + forearmDir * 1.2f, "Hướng Cánh Tay", EditorStyles.boldLabel);

        // Draw sword blade guideline (Orange)
        Handles.color = new Color(1f, 0.5f, 0f);
        Handles.DrawLine(swordStart, swordStart + swordDir * 2.5f, 3.5f);
        Handles.Label(swordStart + swordDir * 1.5f, "Hướng Lưỡi Kiếm", EditorStyles.boldLabel);

        // Draw arc showing angle difference
        float angle = Vector3.Angle(forearmDir, swordDir);
        Vector3 cross = Vector3.Cross(forearmDir, swordDir);
        Handles.color = angle < 10f ? Color.green : Color.red;
        Handles.DrawWireArc(armEnd, cross.normalized, forearmDir, angle, 0.8f);
        Handles.Label(armEnd + cross.normalized * 0.2f, $"{angle:F1}°");
    }
}
