#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tool Editor tự động cấu hình Sword Slash VFX (Vệt chém hình cung) cho Player.
  /// </summary>
  public static class PlayerSlashVfxSetupEditor
  {
    private const string PLAYER_PREFAB_PATH = "Assets/Characters/Player/Player.prefab";
    private const string PRIMARY_SLASH_VFX_PATH = "Assets/Characters/Player/Skill/Slash VFX.prefab";
    private const string RESOURCES_SLASH_VFX_PATH = "Assets/Resources/Slash VFX.prefab";
    private const string FALLBACK_SLASH_VFX_PATH = "Assets/VFX/C Sword Slash/Slash VFX.prefab";

    private const string PRIMARY_IMPACT_VFX_PATH = "Assets/Characters/Player/Skill/Impact VFX.prefab";
    private const string RESOURCES_IMPACT_VFX_PATH = "Assets/Resources/Impact VFX.prefab";
    private const string SOURCE_IMPACT_VFX_PATH = "Assets/VFX/C Sword Slash/Impact.prefab";

    private const string PREF_KEY = "PlayerSlashVfx_AutoSetup_v2";

    [InitializeOnLoadMethod]
    private static void AutoRunSetupOnce()
    {
      EditorApplication.delayCall += () =>
      {
        if (!EditorPrefs.GetBool(PREF_KEY, false))
        {
          Debug.Log("<color=cyan>[PlayerSlashVfxSetup]</color> 🚀 Tự động cấu hình Slash Trail & Hit Impact VFX cho Player...");
          SetupPlayerSlashVfx();
          EditorPrefs.SetBool(PREF_KEY, true);
        }
      };
    }

    [MenuItem("Infinite Labyrinth/Combat/2. Cấu Hình Slash Trail & Hit Impact VFX Cho Kiếm Player (1-Click)", false, 2)]
    public static void SetupPlayerSlashVfx()
    {
      Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH SWORD SLASH TRAIL & HIT IMPACT CHO PLAYER ===</color>");

      // 1. Tìm Prefab Slash VFX
      GameObject slashVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PRIMARY_SLASH_VFX_PATH);
      if (slashVfxPrefab == null)
      {
        slashVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESOURCES_SLASH_VFX_PATH);
      }
      if (slashVfxPrefab == null)
      {
        slashVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FALLBACK_SLASH_VFX_PATH);
      }

      if (slashVfxPrefab == null)
      {
        Debug.LogError($"[PlayerSlashVfxSetup] ❌ Không tìm thấy Prefab Slash VFX tại: {PRIMARY_SLASH_VFX_PATH} hoặc {FALLBACK_SLASH_VFX_PATH}");
        return;
      }

      // 2. Tìm Prefab Hit Impact VFX
      GameObject impactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PRIMARY_IMPACT_VFX_PATH);
      if (impactVfxPrefab == null)
      {
        impactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESOURCES_IMPACT_VFX_PATH);
      }
      if (impactVfxPrefab == null)
      {
        impactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_IMPACT_VFX_PATH);
      }

      Debug.Log($"<color=green>[PlayerSlashVfxSetup] ✔️ Đã tìm thấy Prefab Slash VFX: {slashVfxPrefab.name}</color>");
      if (impactVfxPrefab != null)
      {
        Debug.Log($"<color=green>[PlayerSlashVfxSetup] ✔️ Đã tìm thấy Prefab Hit Impact VFX: {impactVfxPrefab.name}</color>");
      }

      // 3. Cấu hình trên Player Prefab
      bool prefabSuccess = SetupPlayerPrefab(slashVfxPrefab, impactVfxPrefab);

      // 4. Cấu hình Scene Instance nếu có trong Scene đang mở
      SetupSceneInstances(slashVfxPrefab, impactVfxPrefab);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      if (prefabSuccess)
      {
        Debug.Log("<color=green>=== HOÀN TẤT! ĐÃ GẮN PLAYER SLASH & HIT IMPACT VFX THÀNH CÔNG VÀO PLAYER PREFAB ===</color>");
      }
      else
      {
        Debug.LogWarning("[PlayerSlashVfxSetup] ⚠️ Không cập nhật được Player Prefab.");
      }
    }

    private static bool SetupPlayerPrefab(GameObject slashVfxPrefab, GameObject impactVfxPrefab)
    {
      GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
      if (playerAsset == null)
      {
        Debug.LogError($"[PlayerSlashVfxSetup] ❌ Không tìm thấy Player prefab tại: {PLAYER_PREFAB_PATH}");
        return false;
      }

      using (var editScope = new PrefabUtility.EditPrefabContentsScope(PLAYER_PREFAB_PATH))
      {
        GameObject root = editScope.prefabContentsRoot;

        // Đảm bảo có PlayerSlashVFX ở root Player
        PlayerSlashVFX slashVFX = root.GetComponent<PlayerSlashVFX>();
        if (slashVFX == null)
        {
          slashVFX = root.AddComponent<PlayerSlashVFX>();
          Debug.Log("[PlayerSlashVfxSetup] + Đã thêm component PlayerSlashVFX vào Player.prefab (Root)");
        }

        // Tự động dọn dẹp PlayerSlashVFX nếu vô tình bị gắn vào con (ví dụ MainModel)
        foreach (PlayerSlashVFX childSlash in root.GetComponentsInChildren<PlayerSlashVFX>(true))
        {
          if (childSlash.gameObject != root)
          {
            Debug.Log($"[PlayerSlashVfxSetup] 🧹 Dọn dẹp script trùng lặp: Đã gỡ PlayerSlashVFX khỏi con '{childSlash.gameObject.name}'");
            Object.DestroyImmediate(childSlash, true);
          }
        }

        // Gán defaultSlashVfxPrefab
        SerializedObject soSlash = new SerializedObject(slashVFX);
        SerializedProperty propDefaultPrefab = soSlash.FindProperty("defaultSlashVfxPrefab");
        if (propDefaultPrefab != null)
        {
          propDefaultPrefab.objectReferenceValue = slashVfxPrefab;
        }

        SerializedProperty propRoot = soSlash.FindProperty("slashSpawnRoot");
        if (propRoot != null && propRoot.objectReferenceValue == null)
        {
          propRoot.objectReferenceValue = root.transform;
        }
        soSlash.ApplyModifiedPropertiesWithoutUndo();

        // Cấu hình Hit Impact VFX cho WeaponHitbox trên kiếm
        WeaponHitbox weaponHitbox = root.GetComponentInChildren<WeaponHitbox>(true);
        if (weaponHitbox != null && impactVfxPrefab != null)
        {
          SerializedObject soWeapon = new SerializedObject(weaponHitbox);
          SerializedProperty propImpact = soWeapon.FindProperty("hitImpactVfxPrefab");
          if (propImpact != null)
          {
            propImpact.objectReferenceValue = impactVfxPrefab;
            soWeapon.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[PlayerSlashVfxSetup] + Đã cấu hình Hit Impact VFX cho WeaponHitbox trên Player.prefab");
          }
        }

        // Đồng bộ liên kết vào PlayerCombat (nằm trên con MainModel của Player)
        PlayerCombat combat = root.GetComponentInChildren<PlayerCombat>(true);
        if (combat != null)
        {
          SerializedObject soCombat = new SerializedObject(combat);
          SerializedProperty propSlashRef = soCombat.FindProperty("playerSlashVFX");
          if (propSlashRef != null)
          {
            propSlashRef.objectReferenceValue = slashVFX;
            soCombat.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[PlayerSlashVfxSetup] + Đã liên kết PlayerSlashVFX vào PlayerCombat (ở MainModel)");
          }
        }

        EditorUtility.SetDirty(root);
      }

      return true;
    }

    private static void SetupSceneInstances(GameObject slashVfxPrefab, GameObject impactVfxPrefab)
    {
      PlayerCombat[] scenePlayers = Object.FindObjectsByType<PlayerCombat>(FindObjectsSortMode.None);
      if (scenePlayers == null || scenePlayers.Length == 0) return;

      foreach (PlayerCombat combat in scenePlayers)
      {
        // PlayerCombat nằm trên con MainModel, nên root thực tế là combat.transform.root
        Transform rootTransform = combat.transform.root;
        GameObject rootGo = rootTransform != null ? rootTransform.gameObject : combat.gameObject;

        // Nếu MainModel có gắn nhầm PlayerSlashVFX thì dọn dẹp ngay
        if (combat.gameObject != rootGo)
        {
          PlayerSlashVFX childSlash = combat.GetComponent<PlayerSlashVFX>();
          if (childSlash != null)
          {
            Debug.Log($"[PlayerSlashVfxSetup] 🧹 Dọn dẹp script trùng lặp: Đã gỡ PlayerSlashVFX khỏi '{combat.gameObject.name}'");
            Object.DestroyImmediate(childSlash);
          }
        }

        // Đảm bảo PlayerSlashVFX nằm trên root Player
        PlayerSlashVFX slashVFX = rootGo.GetComponent<PlayerSlashVFX>();
        if (slashVFX == null)
        {
          slashVFX = rootGo.AddComponent<PlayerSlashVFX>();
          Debug.Log($"[PlayerSlashVfxSetup] + Đã thêm PlayerSlashVFX cho Scene root: {rootGo.name}");
        }

        SerializedObject soSlash = new SerializedObject(slashVFX);
        SerializedProperty propDefaultPrefab = soSlash.FindProperty("defaultSlashVfxPrefab");
        if (propDefaultPrefab != null && propDefaultPrefab.objectReferenceValue == null)
        {
          propDefaultPrefab.objectReferenceValue = slashVfxPrefab;
        }

        SerializedProperty propRoot = soSlash.FindProperty("slashSpawnRoot");
        if (propRoot != null && propRoot.objectReferenceValue == null)
        {
          propRoot.objectReferenceValue = rootGo.transform;
        }
        soSlash.ApplyModifiedPropertiesWithoutUndo();

        // Cấu hình WeaponHitbox trong Scene
        WeaponHitbox sceneWeapon = rootGo.GetComponentInChildren<WeaponHitbox>(true);
        if (sceneWeapon != null && impactVfxPrefab != null)
        {
          SerializedObject soWeapon = new SerializedObject(sceneWeapon);
          SerializedProperty propImpact = soWeapon.FindProperty("hitImpactVfxPrefab");
          if (propImpact != null)
          {
            propImpact.objectReferenceValue = impactVfxPrefab;
            soWeapon.ApplyModifiedPropertiesWithoutUndo();
          }
        }

        // Gán tham chiếu playerSlashVFX trên PlayerCombat trỏ về root PlayerSlashVFX
        SerializedObject soCombat = new SerializedObject(combat);
        SerializedProperty propSlashRef = soCombat.FindProperty("playerSlashVFX");
        if (propSlashRef != null)
        {
          propSlashRef.objectReferenceValue = slashVFX;
          soCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(combat);
        EditorUtility.SetDirty(rootGo);
      }
    }
  }

  /// <summary>
  /// Custom Editor giao diện đẹp cho PlayerSlashVFX với các nút bấm Test đòn chém trực quan.
  /// </summary>
  [CustomEditor(typeof(PlayerSlashVFX))]
  public class PlayerSlashVFXEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      PlayerSlashVFX slashScript = (PlayerSlashVFX)target;
      if (slashScript == null) return;

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("⏱️ ĐIỀU CHỈNH TỐC ĐỘ GAME (SLOW MOTION TOOL)", EditorStyles.boldLabel);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      float currentScale = slashScript.GameSpeed;
      EditorGUILayout.LabelField($"Tốc độ hiện tại: {currentScale:F2}x {(currentScale < 0.99f ? "(Đang Slow-Mo)" : "(Bình thường)")}", EditorStyles.miniBoldLabel);

      EditorGUI.BeginChangeCheck();
      float sliderScale = EditorGUILayout.Slider("Tốc độ (TimeScale)", currentScale, 0.05f, 1.0f);
      if (EditorGUI.EndChangeCheck())
      {
        slashScript.ApplyTimeScale(sliderScale);
        EditorUtility.SetDirty(slashScript);
      }

      EditorGUILayout.Space(4);
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("0.1x (Siêu Chậm)"))
      {
        slashScript.ApplyTimeScale(0.1f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("0.25x (Chậm)"))
      {
        slashScript.ApplyTimeScale(0.25f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("0.5x (Vừa)"))
      {
        slashScript.ApplyTimeScale(0.5f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("1.0x (Chuẩn)"))
      {
        slashScript.ApplyTimeScale(1.0f);
        EditorUtility.SetDirty(slashScript);
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.HelpBox("Phím tắt trong Game:\n• [ : Giảm tốc độ\n• ] : Tăng tốc độ\n• \\ : Reset 1.0x chuẩn\n• Giữ G hoặc T : Bật Slow-Mo tức thì (0.15x)", MessageType.Info);
      EditorGUILayout.EndVertical();

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("⚔️ THỬ NGHIỆM ĐÒN CHÉM (TEST SLASH VFX)", EditorStyles.boldLabel);

      if (slashScript.DefaultPrefab == null)
      {
        EditorGUILayout.HelpBox("Chưa gán 'defaultSlashVfxPrefab'. Hãy kéo Prefab 'Slash VFX' vào ô trên!", MessageType.Warning);
      }

      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      if (GUILayout.Button("⚡ Test Attack 1 (Chém chéo phải xuống trái)", GUILayout.Height(28)))
      {
        slashScript.TestAttack1();
      }

      if (GUILayout.Button("⚡ Test Attack 2 (Chém vát trái lên phải)", GUILayout.Height(28)))
      {
        slashScript.TestAttack2();
      }

      if (GUILayout.Button("⚡ Test Attack 3 (Chém quét ngang phẳng kết combo)", GUILayout.Height(28)))
      {
        slashScript.TestAttack3();
      }

      EditorGUILayout.Space(4);

      if (GUILayout.Button("💥 Test Charge Hit 1 (Chém mở màn)", GUILayout.Height(26)))
      {
        slashScript.TestCharge1();
      }

      if (GUILayout.Button("💥 Test Charge Hit 2 (Bổ dọc từ trên xuống)", GUILayout.Height(26)))
      {
        slashScript.TestCharge2();
      }

      EditorGUILayout.Space(4);

      GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
      if (GUILayout.Button("🔥 Test Counter Attack (Phản công uy lực)", GUILayout.Height(30)))
      {
        slashScript.TestCounter();
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.Space(6);
      GUI.backgroundColor = new Color(1f, 0.45f, 0.2f);
      if (GUILayout.Button("💥 Test Thử Hit Impact VFX (Chém Trúng Đích)", GUILayout.Height(30)))
      {
        slashScript.TestImpact();
      }
      GUI.backgroundColor = Color.white;

      if (!Application.isPlaying)
      {
        EditorGUILayout.Space(6);
        if (GUILayout.Button("⏹️ Dọn Dẹp / Tắt Preview Ngay", GUILayout.Height(24)))
        {
          PlayerSlashVFX.StopActiveEditorPreview();
        }
      }

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
