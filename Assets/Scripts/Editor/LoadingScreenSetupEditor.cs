#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  public static class LoadingScreenSetupEditor
  {
    private const string ResourceDataPath = "Assets/Resources/LoadingScreenData.asset";
    private const string ResourcePrefabPath = "Assets/Resources/LoadingScreenCanvas.prefab";
    private const string RpgPrefabPath = "Assets/Prefabs/RPG/LoadingScreenCanvas.prefab";
    private const string PrefKey = "InfiniteLabyrinth_LoadingScreen_Initialized_v2";

    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string MonsterBorderPath = "Assets/UI RPG/GameUI/Monster_Border.png";
    private const string MonsterFillPath = "Assets/UI RPG/GameUI/Monster_Fill.png";
    private const string SpinnerSpritePath = "Assets/UI RPG/PlayerHUD 2/Exp_Circle.png";
    private const string UiLinePath = "Assets/UI RPG/GameUI/UI_Line.png";

    [InitializeOnLoadMethod]
    private static void AutoRunSetupOnce()
    {
      EditorApplication.delayCall += () =>
      {
        if (!EditorPrefs.GetBool(PrefKey, false) || !File.Exists(ResourcePrefabPath))
        {
          InitializeLoadingScreenData();
          CreateLoadingScreenPrefab();
          EditorPrefs.SetBool(PrefKey, true);
        }
      };
    }

    [MenuItem("Infinite Labyrinth/UI/1. Khởi Tạo Dữ Liệu Loading Screen (LoadingScreenData)", false, 20)]
    public static void InitializeLoadingScreenData()
    {
      if (!AssetDatabase.IsValidFolder("Assets/Resources"))
      {
        AssetDatabase.CreateFolder("Assets", "Resources");
      }

      LoadingScreenData data = AssetDatabase.LoadAssetAtPath<LoadingScreenData>(ResourceDataPath);
      bool isNew = false;

      if (data == null)
      {
        data = ScriptableObject.CreateInstance<LoadingScreenData>();
        AssetDatabase.CreateAsset(data, ResourceDataPath);
        isNew = true;
      }

      // 1. Nạp danh sách Tips tiếng Anh
      data.tips = GetDefaultEnglishTips();

      // 2. Nạp cấu hình Tiêu đề Phân Vùng (Locations)
      data.locations = new List<LoadingLocationInfo>
      {
        new LoadingLocationInfo("DungeonScene", "INFINITE LABYRINTH - FLOOR {0}", "The Forgotten Crypt"),
        new LoadingLocationInfo("LandScene", "HAVEN VILLAGE", "Sanctuary of the Wandering Knight")
      };

      // 3. Tự động quét và liên kết ảnh nền hầm ngục từ Assets/Prefabs/Dungeon/Samples/
      List<Texture2D> sampleTextures = new List<Texture2D>();
      string sampleFolder = "Assets/Prefabs/Dungeon/Samples";
      if (AssetDatabase.IsValidFolder(sampleFolder))
      {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sampleFolder });
        foreach (string guid in guids)
        {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
          if (tex != null && !sampleTextures.Contains(tex))
          {
            sampleTextures.Add(tex);
          }
        }
      }

      data.dungeonBackgroundTextures = sampleTextures.ToArray();

      EditorUtility.SetDirty(data);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>[LoadingScreen]</color> {(isNew ? "Đã tạo mới" : "Đã cập nhật")} LoadingScreenData tại '{ResourceDataPath}' " +
                $"với {data.tips.Count} Mẹo tiếng Anh và {data.dungeonBackgroundTextures.Length} Ảnh nền hầm ngục!");

      Selection.activeObject = data;
    }

    [MenuItem("Infinite Labyrinth/UI/2. Tạo Prefab Loading Screen Canvas (1-Click)", false, 21)]
    public static GameObject CreateLoadingScreenPrefab()
    {
      if (!AssetDatabase.IsValidFolder("Assets/Resources"))
      {
        AssetDatabase.CreateFolder("Assets", "Resources");
      }
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs/RPG"))
      {
        AssetDatabase.CreateFolder("Assets/Prefabs", "RPG");
      }

      TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
      Sprite borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonsterBorderPath);
      Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonsterFillPath);
      Sprite spinnerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpinnerSpritePath);
      Sprite lineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiLinePath);

      // 1. Root Canvas
      GameObject root = new GameObject("LoadingScreenCanvas");
      Canvas canvas = root.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = 999; // Trên cùng mọi UI khác

      CanvasScaler scaler = root.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920, 1080);
      scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
      scaler.matchWidthOrHeight = 0.5f;

      root.AddComponent<GraphicRaycaster>();

      CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;

      LoadingScreenUI ui = root.AddComponent<LoadingScreenUI>();

      // 2. Background (RawImage Full-screen)
      GameObject bgObj = new GameObject("Background");
      bgObj.transform.SetParent(root.transform, false);
      RectTransform bgRt = bgObj.AddComponent<RectTransform>();
      SetFullStretch(bgRt);
      RawImage bgRaw = bgObj.AddComponent<RawImage>();
      bgRaw.color = Color.white;
      bgRaw.raycastTarget = false;

      // 3. Vignette Overlay (Dark gradient ensuring text readability)
      GameObject vigObj = new GameObject("VignetteOverlay");
      vigObj.transform.SetParent(root.transform, false);
      RectTransform vigRt = vigObj.AddComponent<RectTransform>();
      SetFullStretch(vigRt);
      Image vigImg = vigObj.AddComponent<Image>();
      vigImg.color = new Color(0.03f, 0.03f, 0.05f, 0.65f);
      vigImg.raycastTarget = false;

      // 4. Top Header
      GameObject headerObj = new GameObject("TopHeader");
      headerObj.transform.SetParent(root.transform, false);
      RectTransform headerRt = headerObj.AddComponent<RectTransform>();
      headerRt.anchorMin = new Vector2(0.5f, 1f);
      headerRt.anchorMax = new Vector2(0.5f, 1f);
      headerRt.pivot = new Vector2(0.5f, 1f);
      headerRt.anchoredPosition = new Vector2(0, -40);
      headerRt.sizeDelta = new Vector2(1200, 120);

      // Title
      GameObject titleObj = new GameObject("LocationTitle");
      titleObj.transform.SetParent(headerObj.transform, false);
      RectTransform titleRt = titleObj.AddComponent<RectTransform>();
      titleRt.anchorMin = new Vector2(0f, 0.5f);
      titleRt.anchorMax = new Vector2(1f, 1f);
      titleRt.pivot = new Vector2(0.5f, 0.5f);
      titleRt.anchoredPosition = new Vector2(0, 15);
      titleRt.sizeDelta = new Vector2(1200, 45);
      TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
      if (font != null) titleTmp.font = font;
      titleTmp.text = "INFINITE LABYRINTH - FLOOR 1";
      titleTmp.fontSize = 32f;
      titleTmp.fontStyle = FontStyles.Bold;
      titleTmp.alignment = TextAlignmentOptions.Center;
      titleTmp.color = new Color(1f, 0.84f, 0.31f); // #FFD54F Gold
      titleTmp.raycastTarget = false;

      // Subtitle
      GameObject subObj = new GameObject("LocationSubtitle");
      subObj.transform.SetParent(headerObj.transform, false);
      RectTransform subRt = subObj.AddComponent<RectTransform>();
      subRt.anchorMin = new Vector2(0f, 0f);
      subRt.anchorMax = new Vector2(1f, 0.5f);
      subRt.pivot = new Vector2(0.5f, 0.5f);
      subRt.anchoredPosition = new Vector2(0, -10);
      subRt.sizeDelta = new Vector2(1200, 30);
      TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
      if (font != null) subTmp.font = font;
      subTmp.text = "The Forgotten Crypt";
      subTmp.fontSize = 17f;
      subTmp.fontStyle = FontStyles.Italic;
      subTmp.alignment = TextAlignmentOptions.Center;
      subTmp.color = new Color(0.72f, 0.78f, 0.82f); // #B0BEC5
      subTmp.raycastTarget = false;

      // Decorative Line
      GameObject lineObj = new GameObject("DecorativeLine");
      lineObj.transform.SetParent(headerObj.transform, false);
      RectTransform lineRt = lineObj.AddComponent<RectTransform>();
      lineRt.anchorMin = new Vector2(0.5f, 0f);
      lineRt.anchorMax = new Vector2(0.5f, 0f);
      lineRt.pivot = new Vector2(0.5f, 0.5f);
      lineRt.anchoredPosition = new Vector2(0, -25);
      lineRt.sizeDelta = new Vector2(500, 2);
      Image lineImg = lineObj.AddComponent<Image>();
      if (lineSprite != null) lineImg.sprite = lineSprite;
      lineImg.color = new Color(1f, 1f, 1f, 0.25f);
      lineImg.raycastTarget = false;

      // 5. Bottom Panel
      GameObject bottomObj = new GameObject("BottomPanel");
      bottomObj.transform.SetParent(root.transform, false);
      RectTransform bottomRt = bottomObj.AddComponent<RectTransform>();
      bottomRt.anchorMin = new Vector2(0.5f, 0f);
      bottomRt.anchorMax = new Vector2(0.5f, 0f);
      bottomRt.pivot = new Vector2(0.5f, 0f);
      bottomRt.anchoredPosition = new Vector2(0, 0);
      bottomRt.sizeDelta = new Vector2(1600, 220);

      // Tip Container
      GameObject tipObj = new GameObject("TipContainer");
      tipObj.transform.SetParent(bottomObj.transform, false);
      RectTransform tipRt = tipObj.AddComponent<RectTransform>();
      tipRt.anchorMin = new Vector2(0.5f, 0f);
      tipRt.anchorMax = new Vector2(0.5f, 0f);
      tipRt.pivot = new Vector2(0.5f, 0f);
      tipRt.anchoredPosition = new Vector2(0, 80);
      tipRt.sizeDelta = new Vector2(1300, 90);

      // Tip Category
      GameObject catObj = new GameObject("TipCategory");
      catObj.transform.SetParent(tipObj.transform, false);
      RectTransform catRt = catObj.AddComponent<RectTransform>();
      catRt.anchorMin = new Vector2(0f, 0.5f);
      catRt.anchorMax = new Vector2(1f, 1f);
      catRt.pivot = new Vector2(0.5f, 0.5f);
      catRt.anchoredPosition = new Vector2(0, 18);
      catRt.sizeDelta = new Vector2(1300, 26);
      TextMeshProUGUI catTmp = catObj.AddComponent<TextMeshProUGUI>();
      if (font != null) catTmp.font = font;
      catTmp.text = "[ COMBAT TIP ]";
      catTmp.fontSize = 15f;
      catTmp.fontStyle = FontStyles.Bold;
      catTmp.alignment = TextAlignmentOptions.Center;
      catTmp.color = new Color(1f, 0.65f, 0.15f); // #FFA726 Amber Orange
      catTmp.raycastTarget = false;

      // Tip Content
      GameObject bodyObj = new GameObject("TipContent");
      bodyObj.transform.SetParent(tipObj.transform, false);
      RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
      bodyRt.anchorMin = new Vector2(0f, 0f);
      bodyRt.anchorMax = new Vector2(1f, 0.5f);
      bodyRt.pivot = new Vector2(0.5f, 0.5f);
      bodyRt.anchoredPosition = new Vector2(0, -6);
      bodyRt.sizeDelta = new Vector2(1300, 48);
      TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
      if (font != null) bodyTmp.font = font;
      bodyTmp.text = "Time your Parry with [Right Mouse Button] right before an attack connects to stagger foes and recover Spirit.";
      bodyTmp.fontSize = 16f;
      bodyTmp.alignment = TextAlignmentOptions.Center;
      bodyTmp.color = new Color(0.94f, 0.95f, 0.96f); // #ECEFF1 Crisp Silver White
      bodyTmp.raycastTarget = false;

      // Progress Bar Container
      GameObject barObj = new GameObject("ProgressBarContainer");
      barObj.transform.SetParent(bottomObj.transform, false);
      RectTransform barRt = barObj.AddComponent<RectTransform>();
      barRt.anchorMin = new Vector2(0.5f, 0f);
      barRt.anchorMax = new Vector2(0.5f, 0f);
      barRt.pivot = new Vector2(0.5f, 0.5f);
      barRt.anchoredPosition = new Vector2(-60, 42); // Offset slightly left to make room for percent + spinner
      barRt.sizeDelta = new Vector2(1100, 22);

      // Bar Background
      GameObject barBgObj = new GameObject("BarBackground");
      barBgObj.transform.SetParent(barObj.transform, false);
      RectTransform barBgRt = barBgObj.AddComponent<RectTransform>();
      SetFullStretch(barBgRt);
      Image barBgImg = barBgObj.AddComponent<Image>();
      barBgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
      barBgImg.raycastTarget = false;

      // Bar Fill
      GameObject fillObj = new GameObject("BarFill");
      fillObj.transform.SetParent(barObj.transform, false);
      RectTransform fillRt = fillObj.AddComponent<RectTransform>();
      SetFullStretch(fillRt);
      Image fillImg = fillObj.AddComponent<Image>();
      if (fillSprite != null) fillImg.sprite = fillSprite;
      fillImg.type = Image.Type.Filled;
      fillImg.fillMethod = Image.FillMethod.Horizontal;
      fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
      fillImg.fillAmount = 0.65f;
      fillImg.color = new Color(0.96f, 0.62f, 0.04f); // #F59E0B Golden Amber Souls Fill
      fillImg.raycastTarget = false;

      // Bar Border
      GameObject borderObj = new GameObject("BarBorder");
      borderObj.transform.SetParent(barObj.transform, false);
      RectTransform borderRt = borderObj.AddComponent<RectTransform>();
      SetFullStretch(borderRt);
      Image borderImg = borderObj.AddComponent<Image>();
      if (borderSprite != null) borderImg.sprite = borderSprite;
      borderImg.color = new Color(0.78f, 0.78f, 0.82f, 0.9f);
      borderImg.raycastTarget = false;

      // Percent Text (Anchored right after progress bar)
      GameObject pctObj = new GameObject("PercentText");
      pctObj.transform.SetParent(bottomObj.transform, false);
      RectTransform pctRt = pctObj.AddComponent<RectTransform>();
      pctRt.anchorMin = new Vector2(0.5f, 0f);
      pctRt.anchorMax = new Vector2(0.5f, 0f);
      pctRt.pivot = new Vector2(0f, 0.5f);
      pctRt.anchoredPosition = new Vector2(510, 42);
      pctRt.sizeDelta = new Vector2(90, 30);
      TextMeshProUGUI pctTmp = pctObj.AddComponent<TextMeshProUGUI>();
      if (font != null) pctTmp.font = font;
      pctTmp.text = "65%";
      pctTmp.fontSize = 19f;
      pctTmp.fontStyle = FontStyles.Bold;
      pctTmp.alignment = TextAlignmentOptions.MidlineLeft;
      pctTmp.color = new Color(1f, 0.84f, 0.31f); // #FFD54F
      pctTmp.raycastTarget = false;

      // Spinner Container
      GameObject spinObj = new GameObject("SpinnerContainer");
      spinObj.transform.SetParent(bottomObj.transform, false);
      RectTransform spinRt = spinObj.AddComponent<RectTransform>();
      spinRt.anchorMin = new Vector2(0.5f, 0f);
      spinRt.anchorMax = new Vector2(0.5f, 0f);
      spinRt.pivot = new Vector2(0.5f, 0.5f);
      spinRt.anchoredPosition = new Vector2(620, 42);
      spinRt.sizeDelta = new Vector2(36, 36);

      GameObject spinIconObj = new GameObject("SpinnerIcon");
      spinIconObj.transform.SetParent(spinObj.transform, false);
      RectTransform spinIconRt = spinIconObj.AddComponent<RectTransform>();
      SetFullStretch(spinIconRt);
      Image spinImg = spinIconObj.AddComponent<Image>();
      if (spinnerSprite != null) spinImg.sprite = spinnerSprite;
      spinImg.color = new Color(1f, 0.84f, 0.31f, 0.9f);
      spinImg.raycastTarget = false;

      // 6. Link Serialized Properties in LoadingScreenUI
      SerializedObject so = new SerializedObject(ui);
      so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
      so.FindProperty("backgroundImage").objectReferenceValue = bgRaw;
      so.FindProperty("vignetteOverlay").objectReferenceValue = vigImg;
      so.FindProperty("locationTitleText").objectReferenceValue = titleTmp;
      so.FindProperty("locationSubtitleText").objectReferenceValue = subTmp;
      so.FindProperty("tipCategoryText").objectReferenceValue = catTmp;
      so.FindProperty("tipContentText").objectReferenceValue = bodyTmp;
      so.FindProperty("progressFillImage").objectReferenceValue = fillImg;
      so.FindProperty("percentText").objectReferenceValue = pctTmp;
      so.FindProperty("spinnerTransform").objectReferenceValue = spinIconRt;
      so.ApplyModifiedPropertiesWithoutUndo();

      // 7. Save as Prefab
      PrefabUtility.SaveAsPrefabAsset(root, ResourcePrefabPath);
      PrefabUtility.SaveAsPrefabAsset(root, RpgPrefabPath);
      Object.DestroyImmediate(root);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>[LoadingScreen]</color> 🚀 Đã tạo thành công Prefab LoadingScreenCanvas tại:\n" +
                $"1. {ResourcePrefabPath}\n2. {RpgPrefabPath}");

      GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefabPath);
      Selection.activeObject = createdPrefab;
      return createdPrefab;
    }

    private static void SetFullStretch(RectTransform rt)
    {
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;
    }

    private static List<LoadingTip> GetDefaultEnglishTips()
    {
      return new List<LoadingTip>
      {
        // === COMBAT ===
        new LoadingTip("COMBAT", "Time your Parry with [Right Mouse Button] right before an attack connects to stagger foes and recover Spirit."),
        new LoadingTip("COMBAT", "Rolling gives you Invulnerability Frames (I-Frames). Use it to evade wide slashes and crushing shockwaves."),
        new LoadingTip("COMBAT", "Heavy Skeleton Warriors boast thick Poise. Land a complete 3-hit combo to break their posture and interrupt their strikes."),
        new LoadingTip("COMBAT", "Release your Spirit Blade [E] to strike down archers and skeleton mages from a safe range."),
        new LoadingTip("COMBAT", "When surrounded by skeletal hordes, unleash the Area Burst [Q] to sweep the field clean."),
        new LoadingTip("COMBAT", "Keep an eye on your Stamina bar. Running out of breath leaves you defenseless against lethal counterattacks."),

        // === EXPLORATION ===
        new LoadingTip("EXPLORATION", "Shatter barrels and wooden crates with your weapon to find gold coins and vital recovery elixirs."),
        new LoadingTip("EXPLORATION", "Large labyrinth halls often feature second-floor mezzanines hiding valuable treasure chests."),
        new LoadingTip("EXPLORATION", "Ancient Shrines radiate a golden aura. Search them thoroughly to obtain the Dungeon Key."),
        new LoadingTip("EXPLORATION", "Gold coins magnetize towards you automatically when walking nearby."),
        new LoadingTip("EXPLORATION", "Wall torches and floor braziers reveal safe corridors and banish dark ambushes."),

        // === BOSS TACTICS ===
        new LoadingTip("BOSS TACTICS", "The Ice Golem is an unrelenting colossus. Step back when you see its fists raised high for a ground slam."),
        new LoadingTip("BOSS TACTICS", "When the Frost Juggernaut leaps into the air, time your jump or roll just as it impacts the arena floor."),
        new LoadingTip("BOSS TACTICS", "Take cover behind the four ancient colosseum pillars to block freezing breath and homing purple ice orbs."),
        new LoadingTip("BOSS TACTICS", "During the Ice Golem's intro jump-down, keep your distance to avoid taking heavy splash damage."),

        // === PROGRESSION & HUB ===
        new LoadingTip("VILLAGE HUB", "Resting at the Village Bell fully restores your Health and recharges your spirit for the journey ahead."),
        new LoadingTip("VILLAGE HUB", "Visit the Village Blacksmith to enhance your blades and boost your melee damage output."),
        new LoadingTip("PROGRESSION", "Press [Tab] at any time to inspect your inventory, equip armor, or allocate attribute points."),
        new LoadingTip("PROGRESSION", "Resetting attribute points costs gold at the Village, but allows you to experiment with new combat builds."),
        new LoadingTip("ROGUELITE", "If you fall in battle, you will awaken at the Village Bell with the rising of a new dawn."),
        new LoadingTip("ROGUELITE", "Deeper dungeon floors yield rarer equipment, greater heaps of gold, and tougher adversaries.")
      };
    }
  }
}
#endif
