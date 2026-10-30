#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tool Editor tự động tạo Prefab Thanh Máu World Space và gắn vào toàn bộ Prefab quái vật hầm ngục.
  /// </summary>
  public static class EnemyHealthBarSetupEditor
  {
    private const string BORDER_SPRITE_PATH = "Assets/UI RPG/GameUI/Monster_Border.png";
    private const string FILL_SPRITE_PATH = "Assets/UI RPG/GameUI/Monster_Fill.png";
    private const string HEALTHBAR_PREFAB_PATH = "Assets/Prefabs/RPG/EnemyHealthBar.prefab";
    private const string RESOURCES_HEALTHBAR_PATH = "Assets/Resources/EnemyHealthBar.prefab";
    private const string PREF_KEY = "EnemyHealthBar_AutoSetup_v1";

    private static readonly (string path, float heightOffset)[] MONSTER_PREFABS = new[]
    {
      ("Assets/Characters/Monsters/SkeletonWarrior.prefab", 2.15f),
      ("Assets/Characters/Monsters/SkeletonMinion.prefab", 1.95f),
      ("Assets/Characters/Monsters/SkeletonRogue.prefab", 2.15f),
      ("Assets/Characters/Monsters/SkeletonMage.prefab", 2.25f)
    };

    [InitializeOnLoadMethod]
    private static void AutoRunSetupOnce()
    {
      EditorApplication.delayCall += () =>
      {
        if (!EditorPrefs.GetBool(PREF_KEY, false))
        {
          Debug.Log("<color=cyan>[EnemyHealthBarSetup]</color> 🚀 Tự động khởi chạy cấu hình Thanh máu Quái vật lần đầu...");
          SetupAllEnemyHealthBars();
          EditorPrefs.SetBool(PREF_KEY, true);
        }
      };
    }

    [MenuItem("Infinite Labyrinth/Monsters/1. Cấu Hình Toàn Diện Thanh Máu Cho Quái Vật (1-Click)", false, 1)]
    public static void SetupAllEnemyHealthBars()
    {
      Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH THANH MÁU WORLD SPACE CHO QUÁI VẬT ===</color>");

      // 1. Kiểm tra và tải Sprites
      Sprite borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BORDER_SPRITE_PATH);
      Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FILL_SPRITE_PATH);

      if (borderSprite == null || fillSprite == null)
      {
        Debug.LogError($"[EnemyHealthBarSetup] ❌ Không tìm thấy sprite UI tại:\n- {BORDER_SPRITE_PATH}\n- {FILL_SPRITE_PATH}");
        return;
      }

      // 2. Tạo hoặc Cập nhật Prefab EnemyHealthBar
      GameObject healthBarPrefab = CreateOrUpdateHealthBarPrefab(borderSprite, fillSprite);
      if (healthBarPrefab == null)
      {
        Debug.LogError("[EnemyHealthBarSetup] ❌ Không thể tạo Prefab EnemyHealthBar!");
        return;
      }

      // 3. Gắn vào từng Prefab quái vật
      int successCount = 0;
      foreach (var (prefabPath, heightOffset) in MONSTER_PREFABS)
      {
        if (AttachHealthBarToMonsterPrefab(prefabPath, healthBarPrefab, heightOffset))
        {
          successCount++;
        }
      }

      // 4. Quét cả các quái vật đang có sẵn trong Scene hiện tại (nếu có quái đặt thủ công)
      int sceneCount = SetupSceneMonsters(healthBarPrefab);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>=== HOÀN TẤT THIẾT LẬP THANH MÁU QUÁI VẬT! ===</color>\n" +
                $"✅ Đã cấu hình {successCount}/{MONSTER_PREFABS.Length} Prefabs quái hầm ngục.\n" +
                $"✅ Đã đồng bộ {sceneCount} quái vật trong Scene hiện tại.\n" +
                $"👑 Prefab Thanh Máu: {HEALTHBAR_PREFAB_PATH}\n" +
                $"👑 Resources Fallback: {RESOURCES_HEALTHBAR_PATH}");
    }

    /// <summary>
    /// Xây dựng Prefab EnemyHealthBar chuẩn với World Space Canvas, Ghost Bar và Monster Border/Fill
    /// </summary>
    public static GameObject CreateOrUpdateHealthBarPrefab(Sprite borderSprite, Sprite fillSprite)
    {
      GameObject root = new GameObject("EnemyHealthBar");

      try
      {
        // 1. Cấu hình RectTransform Root
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(250f, 30f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.localScale = new Vector3(0.0045f, 0.0045f, 0.0045f);
        rootRect.localPosition = new Vector3(0f, 2.15f, 0f);

        // 2. Canvas World Space
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        // 3. CanvasGroup
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // 4. Background (Nền tối)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        SetStretch(bgRect);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = fillSprite;
        bgImg.type = Image.Type.Filled;
        bgImg.fillMethod = Image.FillMethod.Horizontal;
        bgImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        bgImg.fillAmount = 1f;
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        bgImg.raycastTarget = false;

        // 5. GhostFill (Thanh bóng ma màu vàng Souls-like)
        GameObject ghostObj = new GameObject("GhostFill");
        ghostObj.transform.SetParent(root.transform, false);
        RectTransform ghostRect = ghostObj.AddComponent<RectTransform>();
        SetStretch(ghostRect);
        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite = fillSprite;
        ghostImg.type = Image.Type.Filled;
        ghostImg.fillMethod = Image.FillMethod.Horizontal;
        ghostImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        ghostImg.fillAmount = 1f;
        ghostImg.color = new Color(0.96f, 0.74f, 0.22f, 0.9f);
        ghostImg.raycastTarget = false;

        // 6. MainFill (Thanh máu chính màu đỏ tươi Souls-like)
        GameObject mainObj = new GameObject("MainFill");
        mainObj.transform.SetParent(root.transform, false);
        RectTransform mainRect = mainObj.AddComponent<RectTransform>();
        SetStretch(mainRect);
        Image mainImg = mainObj.AddComponent<Image>();
        mainImg.sprite = fillSprite;
        mainImg.type = Image.Type.Filled;
        mainImg.fillMethod = Image.FillMethod.Horizontal;
        mainImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        mainImg.fillAmount = 1f;
        mainImg.color = new Color(0.85f, 0.16f, 0.16f, 1f);
        mainImg.raycastTarget = false;

        // 7. Border (Khung viền hoa văn Monster_Border)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(root.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        SetStretch(borderRect);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = borderSprite;
        borderImg.type = Image.Type.Simple;
        borderImg.color = Color.white;
        borderImg.raycastTarget = false;

        // 8. Gắn Controller
        EnemyHealthBarUI healthBarUI = root.AddComponent<EnemyHealthBarUI>();
        healthBarUI.SetupCustomReferences(canvas, group, borderImg, bgImg, ghostImg, mainImg, 2.15f);

        // Lưu thành Prefab vào Assets/Prefabs/RPG/
        string dirRpg = System.IO.Path.GetDirectoryName(HEALTHBAR_PREFAB_PATH);
        if (!System.IO.Directory.Exists(dirRpg)) System.IO.Directory.CreateDirectory(dirRpg);
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, HEALTHBAR_PREFAB_PATH);

        // Lưu bản sao vào Assets/Resources/ để quái có thể fallback runtime nếu cần
        string dirRes = System.IO.Path.GetDirectoryName(RESOURCES_HEALTHBAR_PATH);
        if (!System.IO.Directory.Exists(dirRes)) System.IO.Directory.CreateDirectory(dirRes);
        PrefabUtility.SaveAsPrefabAsset(root, RESOURCES_HEALTHBAR_PATH);

        Debug.Log($"<color=green>[EnemyHealthBarSetup]</color> ✅ Đã tạo/cập nhật Prefab:\n- {HEALTHBAR_PREFAB_PATH}\n- {RESOURCES_HEALTHBAR_PATH}");
        return savedPrefab;
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    /// <summary>
    /// Gắn Prefab Thanh Máu vào Prefab Quái Vật
    /// </summary>
    private static bool AttachHealthBarToMonsterPrefab(string monsterPrefabPath, GameObject healthBarPrefab, float heightOffset)
    {
      if (!System.IO.File.Exists(monsterPrefabPath))
      {
        Debug.LogWarning($"[EnemyHealthBarSetup] ⚠️ Không tìm thấy quái prefab: {monsterPrefabPath}");
        return false;
      }

      GameObject monsterRoot = PrefabUtility.LoadPrefabContents(monsterPrefabPath);
      try
      {
        // 1. Kiểm tra nếu đã có thanh máu cũ
        Transform existingBar = monsterRoot.transform.Find("EnemyHealthBar");
        if (existingBar != null)
        {
          Object.DestroyImmediate(existingBar.gameObject);
        }

        // 2. Instantiate thanh máu vào quái
        GameObject barInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, monsterRoot.transform);
        barInstance.name = "EnemyHealthBar";

        // 3. Tinh chỉnh RectTransform theo độ cao quái
        RectTransform barRect = barInstance.GetComponent<RectTransform>();
        if (barRect != null)
        {
          barRect.localPosition = new Vector3(0f, heightOffset, 0f);
          barRect.localRotation = Quaternion.identity;
          barRect.localScale = new Vector3(0.0045f, 0.0045f, 0.0045f);
        }

        // 4. Đồng bộ tham chiếu Health và EnemyAIBase trên UI
        EnemyHealthBarUI ui = barInstance.GetComponent<EnemyHealthBarUI>();
        if (ui != null)
        {
          SerializedObject so = new SerializedObject(ui);
          so.FindProperty("heightOffset").floatValue = heightOffset;
          so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Lưu lại Prefab
        PrefabUtility.SaveAsPrefabAsset(monsterRoot, monsterPrefabPath);
        Debug.Log($"<color=green>[EnemyHealthBarSetup]</color> ✅ Gắn thanh máu thành công cho {System.IO.Path.GetFileNameWithoutExtension(monsterPrefabPath)} (Offset: {heightOffset}m)");
        return true;
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(monsterRoot);
      }
    }

    /// <summary>
    /// Cấu hình cho quái vật đang có sẵn trong Scene (nếu có)
    /// </summary>
    private static int SetupSceneMonsters(GameObject healthBarPrefab)
    {
      EnemyAIBase[] sceneEnemies = Object.FindObjectsByType<EnemyAIBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      int count = 0;

      foreach (var enemy in sceneEnemies)
      {
        Transform existingBar = enemy.transform.Find("EnemyHealthBar");
        if (existingBar == null)
        {
          GameObject barObj = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, enemy.transform);
          barObj.name = "EnemyHealthBar";

          RectTransform rect = barObj.GetComponent<RectTransform>();
          if (rect != null)
          {
            float offset = 2.15f;
            if (enemy.name.Contains("Minion")) offset = 1.95f;
            else if (enemy.name.Contains("Mage")) offset = 2.25f;

            rect.localPosition = new Vector3(0f, offset, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(0.0045f, 0.0045f, 0.0045f);
          }

          EditorUtility.SetDirty(enemy.gameObject);
          count++;
        }
      }

      return count;
    }

    private static void SetStretch(RectTransform rt)
    {
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;
      rt.pivot = new Vector2(0.5f, 0.5f);
    }
  }
}
#endif
