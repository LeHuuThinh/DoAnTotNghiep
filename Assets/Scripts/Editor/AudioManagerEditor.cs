#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector nâng cao cho AudioManager - Cung cấp Giao diện Bảng Cân Bằng Âm Lượng (Sound Balance Matrix)
/// Hỗ trợ Live Tuning thời gian thực trong lúc Play Mode và nút 1-Click lưu trực tiếp vào Prefabs.
/// </summary>
[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    // Trạng thái gập mở các nhóm Foldout
    private static bool foldoutMaster = true;
    private static bool foldoutCombat = true;
    private static bool foldoutSkills = true;
    private static bool foldoutIdle = true;
    private static bool foldoutMonsters = true;
    private static bool foldoutEnv = true;
    private static bool foldoutUI = true;
    private static bool foldoutClips = false;

    // Serialized Properties
    private SerializedProperty masterVolumeProp;
    private SerializedProperty bgmVolumeProp;
    private SerializedProperty voiceVolumeProp;
    private SerializedProperty sfxVolumeProp;
    private SerializedProperty uiVolumeProp;
    private SerializedProperty loadSavedSettingsProp;

    private void OnEnable()
    {
        masterVolumeProp = serializedObject.FindProperty("masterVolume");
        bgmVolumeProp = serializedObject.FindProperty("bgmVolume");
        voiceVolumeProp = serializedObject.FindProperty("voiceVolume");
        sfxVolumeProp = serializedObject.FindProperty("sfxVolume");
        uiVolumeProp = serializedObject.FindProperty("uiVolume");
        loadSavedSettingsProp = serializedObject.FindProperty("loadSavedSettingsOnStart");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        AudioManager am = (AudioManager)target;

        // Header Title
        EditorGUILayout.Space(6);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🎵 BẢNG ĐIỀU KHIỂN & CÂN BẰNG ÂM LƯỢNG", headerStyle);
        EditorGUILayout.LabelField("Infinite Labyrinth - Sound Balance Matrix", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(8);

        // Banner thông báo trong Play Mode
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("🎮 ĐANG PLAY MODE (LIVE TUNING):\nBạn có thể vừa chơi vừa kéo thanh trượt để nghe âm lượng thay đổi tức thì!\nNhấn nút 'Lưu Thiết Lập Vào Prefabs' bên dưới để lưu vĩnh viễn các giá trị bạn vừa chỉnh.", MessageType.Info);
            
            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f);
            if (GUILayout.Button("💾 LƯU THIẾT LẬP ÂM LƯỢNG VÀO PREFABS", GUILayout.Height(34)))
            {
                SaveRuntimeVolumesToPrefabs(am);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(8);
        }

        // =====================================================================
        // 1. ÂM LƯỢNG CHUNG (GLOBAL VOLUMES)
        // =====================================================================
        DrawSectionHeader(ref foldoutMaster, "🔊 1. ÂM LƯỢNG TỔNG & NHẠC NỀN (MASTER & BGM)", new Color(0.2f, 0.6f, 0.9f));
        if (foldoutMaster)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(masterVolumeProp, new GUIContent("Master Volume", "Âm lượng tổng của toàn bộ trò chơi"));
            EditorGUILayout.PropertyField(bgmVolumeProp, new GUIContent("BGM Volume", "Âm lượng nhạc nền thám hiểm, chiến đấu và Boss"));
            EditorGUILayout.PropertyField(voiceVolumeProp, new GUIContent("Voice Volume", "Âm lượng giọng nhân vật (hô chém, rên đau, thở dốc, idle)"));
            EditorGUILayout.PropertyField(sfxVolumeProp, new GUIContent("SFX Volume", "Âm lượng hiệu ứng âm thanh chung"));
            EditorGUILayout.PropertyField(uiVolumeProp, new GUIContent("UI Volume", "Âm lượng giao diện, tương tác UI, đe rèn, nhặt đồ"));
            EditorGUILayout.PropertyField(loadSavedSettingsProp, new GUIContent("Load Saved Settings", "Tự động tải lại âm lượng đã lưu từ PlayerPrefs"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 2. COMBAT NGƯỜI CHƠI (PLAYER COMBAT)
        // =====================================================================
        DrawSectionHeader(ref foldoutCombat, "⚔️ 2. COMBAT NGƯỜI CHƠI (PLAYER SFX)", new Color(0.85f, 0.35f, 0.35f));
        if (foldoutCombat)
        {
            EditorGUI.indentLevel++;
            DrawVolumeSlider("volPlayerSlash", "Đòn Chém Kiếm (Slash 1..2)");
            DrawVolumeSlider("volPlayerAttackVoice", "Tiếng Hô Chém ('huh', 'ha')");
            DrawVolumeSlider("volPlayerHurtVoice", "Tiếng Rên Đau Vỡ Poise (Hurt 1..5)");
            DrawVolumeSlider("volPlayerParry", "Gạt Đòn Kim Loại (Parry)");
            DrawVolumeSlider("volPlayerCounter", "Bùng Nổ Phản Đòn (Counter)");
            DrawVolumeSlider("volPlayerOutOfStamina", "Thở Dốc Cạn Thể Lực");
            DrawVolumeSlider("volPlayerStaminaAlert", "Tít Trầm Hết Stamina");
            DrawVolumeSlider("volPlayerDeath", "Ngã Gục Tử Trận (Death)");
            DrawVolumeSlider("volPlayerHeal", "Hiệu Ứng Hồi Máu (Healing)");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 3. KỸ NĂNG NGƯỜI CHƠI (PLAYER SKILLS)
        // =====================================================================
        DrawSectionHeader(ref foldoutSkills, "⚡ 3. KỸ NĂNG NGƯỜI CHƠI (SKILLS SFX)", new Color(0.9f, 0.75f, 0.2f));
        if (foldoutSkills)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Gồng Tụ Lực (Charge SFX)", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volSkillECharge", "Skill E Gồng Kiếm Khí (Small)");
            DrawVolumeSlider("volSkillQCharge", "Skill Q Gồng Chấn Địa (Medium)");
            DrawVolumeSlider("volSkillRCharge", "Skill R Gồng Tối Thượng (Big)");

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Va Chạm & Sát Thương (Impact SFX)", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volSkillEImpact", "Skill E Va Chạm Kiếm Khí (3D)");
            DrawVolumeSlider("volSkillQImpact", "Skill Q Nổ Chấn Địa Mặt Đất");
            DrawVolumeSlider("volSkillRImpact", "Skill R Chém Quét Uy Lực");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 4. BƯỚC CHÂN & IDLE (FOOTSTEPS & IDLE)
        // =====================================================================
        DrawSectionHeader(ref foldoutIdle, "👟 4. BƯỚC CHÂN & ĐỘNG TÁC IDLE", new Color(0.35f, 0.75f, 0.65f));
        if (foldoutIdle)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Nhịp Độ Bước Chân Player (Cadence)", EditorStyles.miniBoldLabel);
            DrawIntervalSlider("playerWalkFootstepInterval", "Nhịp Bước Đi Bộ (Walk Interval)", 0.2f, 1.0f);
            DrawIntervalSlider("playerRunFootstepInterval", "Nhịp Bước Chạy (Run Interval)", 0.15f, 0.8f);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Âm Lượng Bước Chân Player", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volFootstepsDirt", "Bước Chân Đất Cỏ (Làng Haven)");
            DrawVolumeSlider("volFootstepsRock", "Bước Chân Sàn Đá (Hầm Ngục)");
            DrawIntervalSlider("volPlayerWalkRatio", "Tỷ Lệ Âm Đi Bộ / Chạy (Walk Ratio)", 0.2f, 1.0f);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Động Tác Idle (Idle Reactions)", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volIdleChecking", "Ngó Nghiêng (Checking 1..5)");
            DrawVolumeSlider("volIdleWarmup", "Khởi Động Bẻ Khớp (Warmup 1..6)");
            DrawVolumeSlider("volIdleHums", "Thở Khẽ / Ngâm Nga (Hums 1..2)");
            DrawVolumeSlider("volIdleStretching", "Vươn Vai / Ngáp (Stretching)");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 5. QUÁI VẬT SKELETON (MONSTER SFX)
        // =====================================================================
        DrawSectionHeader(ref foldoutMonsters, "💀 5. QUÁI VẬT SKELETON (MONSTER SFX)", new Color(0.8f, 0.45f, 0.85f));
        if (foldoutMonsters)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Bước Chân Quái Vật (Monster Footsteps)", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volMonsterFootstep", "Bước Chân Quái Trên Đá (Footstep)");
            DrawIntervalSlider("monsterWalkFootstepInterval", "Nhịp Bước Quái Đi Bộ / Vờn (giây)", 0.3f, 1.5f);
            DrawIntervalSlider("monsterRunFootstepInterval", "Nhịp Bước Quái Chạy / Truy Đuổi (giây)", 0.2f, 1.0f);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Hành Động & Tấn Công", EditorStyles.miniBoldLabel);
            DrawVolumeSlider("volMonsterMeleeAttack", "Quái Chém Gió (Melee Attack)");
            DrawVolumeSlider("volMonsterMeleeHit", "Đòn Chém Trúng Da Thịt");
            DrawVolumeSlider("volMonsterArrowShoot", "Cung Thủ Bắn Tên (Shoot)");
            DrawVolumeSlider("volMonsterArrowHit", "Mũi Tên Găm Trúng Đích (Hit)");
            DrawVolumeSlider("volMonsterSpellCast", "Pháp Sư Gồng Niệm Trượng");
            DrawVolumeSlider("volMonsterSpellImpact", "Phép Thuật Nổ Tung");
            DrawVolumeSlider("volMonsterGetHit", "Dính Sát Thương (Rắc Xương)");
            DrawVolumeSlider("volMonsterDeath", "Tử Trận Sụp Đổ Xương");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 6. MÔI TRƯỜNG & HẦM NGỤC (ENVIRONMENT SFX)
        // =====================================================================
        DrawSectionHeader(ref foldoutEnv, "🏰 6. MÔI TRƯỜNG & HẦM NGỤC (ENVIRONMENT SFX)", new Color(0.7f, 0.6f, 0.4f));
        if (foldoutEnv)
        {
            EditorGUI.indentLevel++;
            DrawVolumeSlider("volBarrelBreak", "Thùng Gỗ Vỡ Vụn");
            DrawVolumeSlider("volVillageBell", "Chuông Thánh Làng Haven");
            DrawVolumeSlider("volDoorOpen", "Mở Cửa Hầm Ngục");
            DrawVolumeSlider("volBossDoorOpen", "Mở Đại Môn Đấu Trường Boss");
            DrawVolumeSlider("volChestOpen", "Mở Rương Báu (Chest Open)");
            DrawVolumeSlider("volChestAura", "Hào Quang Rương Chói Lóa (Aura)");
            DrawVolumeSlider("volCoinPickup", "Nhặt Đồng Tiền Vàng");
            DrawVolumeSlider("volPortal", "Cổng Dịch Chuyển Ma Thuật (Portal Loop)");
            DrawVolumeSlider("volTorch", "Lửa Đuốc Cháy Lách Tách (Torch Loop)");
            DrawVolumeSlider("volAmbientStinger", "Âm Hưởng Rùng Rợn Hầm Ngục");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // 7. GIAO DIỆN & TƯƠNG TÁC (UI SFX)
        // =====================================================================
        DrawSectionHeader(ref foldoutUI, "🖱️ 7. GIAO DIỆN & TƯƠNG TÁC (UI SFX)", new Color(0.4f, 0.65f, 0.85f));
        if (foldoutUI)
        {
            EditorGUI.indentLevel++;
            DrawVolumeSlider("volUIAnvil", "Đe Rèn Lò Rèn (Upgrade/Craft)");
            DrawVolumeSlider("volUIEquip", "Mặc / Tháo Trang Bị (Equip)");
            DrawVolumeSlider("volUIPotion", "Uống Bình Dược Phẩm (Potion)");
            DrawVolumeSlider("volUIShop", "Giao Dịch Cửa Hàng (Shop)");
            DrawVolumeSlider("volUIDisabled", "Click Nút Bị Khóa / Disable");
            DrawVolumeSlider("volUIItemPickup", "Nhặt Đồ Dưới Đất (Phím F)");
            DrawVolumeSlider("volUIInteract", "Mở Giao Diện (UIInteract 1)");
            DrawVolumeSlider("volUIInteract2", "Toggle / 3D-2D / Thả Slider (UIInteract 2)");
            DrawVolumeSlider("volUIInteract3", "Menu / Tab / Đóng UI (UIInteract 3)");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // =====================================================================
        // CÔNG CỤ RESET VỀ MẶC ĐỊNH
        // =====================================================================
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Khôi Phục Toàn Bộ Về Mặc Định", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc chắn muốn đặt lại toàn bộ thanh trượt cân bằng âm lượng về giá trị mặc định chuẩn không?", "Đồng ý", "Hủy"))
            {
                Undo.RecordObject(am, "Reset Audio Balance Defaults");
                am.ResetSoundBalanceToDefaults();
                EditorUtility.SetDirty(am);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        // =====================================================================
        // 8. DANH SÁCH AUDIO CLIPS & PREFABS (ẨN MẶC ĐỊNH CHO GỌN)
        // =====================================================================
        DrawSectionHeader(ref foldoutClips, "📁 DANH SÁCH AUDIO CLIPS GỐC (ASSETS MAPPING)", new Color(0.5f, 0.5f, 0.5f));
        if (foldoutClips)
        {
            EditorGUI.indentLevel++;
            DrawDefaultInspectorExceptVolumes();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSectionHeader(ref bool foldout, string title, Color accentColor)
    {
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 11
        };

        Rect rect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(accentColor.r, accentColor.g, accentColor.b, 0.15f));
        foldout = EditorGUI.Foldout(rect, foldout, title, true, foldoutStyle);
    }

    private void DrawVolumeSlider(string propertyName, string label)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.Slider(prop, 0f, 2f, new GUIContent(label));
        }
        else
        {
            EditorGUILayout.LabelField(label, $"[Property '{propertyName}' không tồn tại]");
        }
    }

    private void DrawIntervalSlider(string propertyName, string label, float min, float max)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.Slider(prop, min, max, new GUIContent(label));
        }
        else
        {
            EditorGUILayout.LabelField(label, $"[Property '{propertyName}' không tồn tại]");
        }
    }

    private void DrawDefaultInspectorExceptVolumes()
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            // Bỏ qua script reference và các thuộc tính volume đã được vẽ ở trên
            if (prop.name == "m_Script" || 
                prop.name == "masterVolume" || 
                prop.name == "bgmVolume" || 
                prop.name == "voiceVolume" || 
                prop.name == "sfxVolume" || 
                prop.name == "uiVolume" || 
                prop.name == "loadSavedSettingsOnStart" || 
                prop.name.StartsWith("vol") ||
                prop.name.StartsWith("player") ||
                prop.name.StartsWith("monster"))
            {
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }
    }

    /// <summary>
    /// Lưu các giá trị âm lượng đang điều chỉnh ở Runtime vào các file Prefab của dự án
    /// </summary>
    private static void SaveRuntimeVolumesToPrefabs(AudioManager runtimeAM)
    {
        string[] targetPaths = new string[]
        {
            "Assets/Prefabs/Audio/AudioManager.prefab",
            "Assets/Prefabs/Core/CoreSystems.prefab",
            "Assets/Resources/CoreSystems.prefab"
        };

        int savedCount = 0;
        foreach (string path in targetPaths)
        {
            if (!File.Exists(path)) continue;

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = editScope.prefabContentsRoot;
                AudioManager targetAM = root.GetComponentInChildren<AudioManager>();
                if (targetAM != null)
                {
                    Undo.RecordObject(targetAM, "Save Runtime Audio Volumes");
                    targetAM.CopyBalanceSettingsFrom(runtimeAM);
                    EditorUtility.SetDirty(targetAM);
                    savedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[AudioManagerEditor]</color> Đã lưu thành công thiết lập âm lượng vào {savedCount} Prefabs!");
        EditorUtility.DisplayDialog("Lưu Thành Công", $"Đã lưu toàn bộ thiết lập âm lượng hiện tại vào {savedCount} Prefab của dự án!\nKhi thoát Play Mode, các thiết lập mới vẫn sẽ được bảo toàn nguyên vẹn.", "Tuyệt vời");
    }
}
#endif
