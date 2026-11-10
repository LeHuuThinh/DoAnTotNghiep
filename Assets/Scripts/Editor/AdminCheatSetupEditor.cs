using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections.Generic;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// 1-Click Automated Setup Editor Tool for Admin Cheat Console.
  /// Generates the complete AdminCheatCanvas with:
  /// - Dynamic LandScene (Village Hub) & DungeonScene (PCG) Teleports & Scene Controls
  /// - Player Combat & Progression Cheats (God Mode, Inf Stamina, Inf Spirit, No Cooldown)
  /// - Monsters & 10,000 HP Training Dummy Controls
  /// - Full Creative Mode with Category Filter, Search Bar, and all 51 Game Items
  /// </summary>
  public static class AdminCheatSetupEditor
  {
    private const string PanelSpritePath = "Assets/UI RPG/GameUI/Panel_WithBorder.png";
    private const string PanelBorderThinPath = "Assets/UI RPG/GameUI/Panel_Border_Thin.png";
    private const string ButtonSpritePath = "Assets/UI RPG/GameUI/Button_WithBorder.png";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Infinite Labyrinth/Admin/1. Setup Admin Cheat Console (Hotkey ~)", false, 1)]
    public static void SetupAdminCheatSystem()
    {
      // 1. Ensure AdminCheatManager
      GameObject managerObj = EnsureAdminCheatManager();

      // 2. Setup / Update AdminCheatCanvas and UI Hierarchy
      GameObject canvasObj = SetupAdminCheatCanvas();

      // 3. Mark Scene Dirty
      if (managerObj != null) EditorUtility.SetDirty(managerObj);
      if (canvasObj != null) EditorUtility.SetDirty(canvasObj);

      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene()
      );

      EditorUtility.DisplayDialog(
        "Setup Completed Successfully!",
        "Admin Cheat Console is now configured!\n\n" +
        "• Press [~] (Tilde / Backquote), [F1], or [F2] during Play Mode to toggle.\n" +
        "• 5 Dynamic Tabs: Teleport (Scene-aware), World (Day & Cheats), Player (No Cooldown & Stats), Monsters (10k HP Dummy), Creative (All 51 Items)!\n" +
        "• Exported & Connected to Assets/Prefabs/Admin/AdminCheatCanvas.prefab.",
        "OK"
      );
    }

    private static GameObject EnsureAdminCheatManager()
    {
      var existing = Object.FindFirstObjectByType<AdminCheatManager>();
      GameObject obj = (existing != null) ? existing.gameObject : new GameObject("AdminCheatManager");

      AdminCheatManager manager = obj.GetComponent<AdminCheatManager>();
      if (manager == null) manager = obj.AddComponent<AdminCheatManager>();

      // Auto-assign Monster test prefabs
      manager.skeletonWarriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");
      manager.skeletonMinionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");
      manager.skeletonRoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");
      manager.skeletonMagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");

      // Auto-load all 51 Game Items into Creative Mode list
      manager.allGameItems = new List<ItemData>();
      string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects/Items" });
      foreach (var guid in guids)
      {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null && !manager.allGameItems.Contains(item))
        {
          manager.allGameItems.Add(item);
        }
      }
      manager.allGameItems.Sort((a, b) =>
      {
        int typeComp = a.ItemType.CompareTo(b.ItemType);
        if (typeComp != 0) return typeComp;
        int rareComp = b.Rarity.CompareTo(a.Rarity);
        if (rareComp != 0) return rareComp;
        return string.Compare(a.ItemName, b.ItemName, System.StringComparison.Ordinal);
      });

      Undo.RegisterCreatedObjectUndo(obj, "Create AdminCheatManager");
      return obj;
    }

    private static GameObject SetupAdminCheatCanvas()
    {
      EnsureEventSystem();

      // Find or create Canvas
      GameObject canvasObj = GameObject.Find("AdminCheatCanvas");
      if (canvasObj == null)
      {
        canvasObj = new GameObject("AdminCheatCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create AdminCheatCanvas");
      }

      // Clean old UI root to rebuild 100% cleanly
      Transform oldPanel = canvasObj.transform.Find("AdminCheatUI_Root");
      if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

      // Load graphic assets
      Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
      Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
      Sprite rowBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelBorderThinPath);
      TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

      // Attach AdminCheatUI component
      AdminCheatUI ui = canvasObj.GetComponent<AdminCheatUI>();
      if (ui == null) ui = canvasObj.AddComponent<AdminCheatUI>();

      SerializedObject so = new SerializedObject(ui);

      // =========================================================================
      // A. ROOT CONTAINER
      // =========================================================================
      GameObject rootContainer = new GameObject("AdminCheatUI_Root");
      rootContainer.transform.SetParent(canvasObj.transform, false);
      RectTransform rootRect = rootContainer.AddComponent<RectTransform>();
      rootRect.anchorMin = Vector2.zero;
      rootRect.anchorMax = Vector2.one;
      rootRect.sizeDelta = Vector2.zero;

      // =========================================================================
      // B. MINI DEBUG OVERLAY (TOP-RIGHT CORNER)
      // =========================================================================
      GameObject overlayObj = new GameObject("MiniDebugOverlay");
      overlayObj.transform.SetParent(rootContainer.transform, false);
      RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
      overlayRect.anchorMin = new Vector2(1f, 1f);
      overlayRect.anchorMax = new Vector2(1f, 1f);
      overlayRect.pivot = new Vector2(1f, 1f);
      overlayRect.anchoredPosition = new Vector2(-20f, -20f);
      overlayRect.sizeDelta = new Vector2(300f, 110f);

      Image overlayBg = overlayObj.AddComponent<Image>();
      if (rowBgSprite != null) overlayBg.sprite = rowBgSprite;
      overlayBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

      VerticalLayoutGroup overlayVGroup = overlayObj.AddComponent<VerticalLayoutGroup>();
      overlayVGroup.padding = new RectOffset(12, 12, 8, 8);
      overlayVGroup.spacing = 3f;
      overlayVGroup.childControlWidth = true;
      overlayVGroup.childControlHeight = false;

      so.FindProperty("debugOverlayRoot").objectReferenceValue = overlayObj;

      GameObject fpsTextObj = CreateText("FpsText", overlayObj.transform, "FPS: 60", 13f, font, Color.green);
      so.FindProperty("txtFps").objectReferenceValue = fpsTextObj.GetComponent<TextMeshProUGUI>();

      GameObject posTextObj = CreateText("PosText", overlayObj.transform, "Position: (0, 0, 0)", 12f, font, Color.white);
      so.FindProperty("txtPlayerPos").objectReferenceValue = posTextObj.GetComponent<TextMeshProUGUI>();

      GameObject roomTextObj = CreateText("RoomText", overlayObj.transform, "Room: #1 (Start)", 12f, font, Color.yellow);
      so.FindProperty("txtCurrentRoom").objectReferenceValue = roomTextObj.GetComponent<TextMeshProUGUI>();

      GameObject badgeTextObj = CreateText("BadgesText", overlayObj.transform, "", 12f, font, new Color(0.2f, 0.8f, 1f));
      so.FindProperty("txtActiveCheatsBadge").objectReferenceValue = badgeTextObj.GetComponent<TextMeshProUGUI>();

      // =========================================================================
      // C. MAIN ADMIN PANEL (FLOATING WINDOW)
      // =========================================================================
      GameObject panelObj = new GameObject("AdminCheatPanel");
      panelObj.transform.SetParent(rootContainer.transform, false);
      RectTransform panelRect = panelObj.AddComponent<RectTransform>();
      panelRect.anchorMin = new Vector2(0f, 0.5f);
      panelRect.anchorMax = new Vector2(0f, 0.5f);
      panelRect.pivot = new Vector2(0f, 0.5f);
      panelRect.anchoredPosition = new Vector2(25f, 0f);
      panelRect.sizeDelta = new Vector2(500f, 660f);

      CanvasGroup panelCg = panelObj.AddComponent<CanvasGroup>();
      so.FindProperty("mainPanelCanvasGroup").objectReferenceValue = panelCg;
      so.FindProperty("mainPanelRoot").objectReferenceValue = panelObj;
      so.FindProperty("rootCanvasGroup").objectReferenceValue = canvasObj.GetComponent<CanvasGroup>();

      Image panelBg = panelObj.AddComponent<Image>();
      if (panelSprite != null)
      {
        panelBg.sprite = panelSprite;
        panelBg.type = Image.Type.Sliced;
      }
      panelBg.color = new Color(0.09f, 0.08f, 0.13f, 0.98f);

      // 1. Header (Title + Scene Indicator + Close Button)
      GameObject headerObj = new GameObject("Header");
      headerObj.transform.SetParent(panelObj.transform, false);
      RectTransform headerRect = headerObj.AddComponent<RectTransform>();
      headerRect.anchorMin = new Vector2(0f, 1f);
      headerRect.anchorMax = new Vector2(1f, 1f);
      headerRect.pivot = new Vector2(0.5f, 1f);
      headerRect.anchoredPosition = new Vector2(0f, -10f);
      headerRect.sizeDelta = new Vector2(-28f, 44f);

      GameObject titleTextObj = CreateText("Title", headerObj.transform, "⚡ ADMIN CHEAT CONSOLE", 16f, font, new Color(1f, 0.88f, 0.45f), FontStyles.Bold);
      RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
      titleRect.anchorMin = new Vector2(0f, 0.5f);
      titleRect.anchorMax = new Vector2(0.85f, 1f);
      titleRect.offsetMin = Vector2.zero;
      titleRect.offsetMax = Vector2.zero;
      titleTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

      GameObject sceneIndObj = CreateText("SceneIndicator", headerObj.transform, "📍 Scene: Loading...", 11f, font, new Color(0.7f, 0.85f, 1f));
      RectTransform siRect = sceneIndObj.GetComponent<RectTransform>();
      siRect.anchorMin = new Vector2(0f, 0f);
      siRect.anchorMax = new Vector2(0.85f, 0.5f);
      siRect.offsetMin = Vector2.zero;
      siRect.offsetMax = Vector2.zero;
      sceneIndObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
      so.FindProperty("txtSceneIndicator").objectReferenceValue = sceneIndObj.GetComponent<TextMeshProUGUI>();

      GameObject closeBtnObj = CreateButton("BtnClose", headerObj.transform, new Vector2(30f, 30f), "✕", 15f, buttonSprite, font, new Color(0.7f, 0.2f, 0.2f));
      RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
      closeRect.anchorMin = new Vector2(1f, 0.5f);
      closeRect.anchorMax = new Vector2(1f, 0.5f);
      closeRect.pivot = new Vector2(1f, 0.5f);
      closeRect.anchoredPosition = Vector2.zero;
      so.FindProperty("btnClose").objectReferenceValue = closeBtnObj.GetComponent<Button>();

      // 2. Navigation Tabs (5 Tab Buttons)
      GameObject navObj = new GameObject("NavTabs");
      navObj.transform.SetParent(panelObj.transform, false);
      RectTransform navRect = navObj.AddComponent<RectTransform>();
      navRect.anchorMin = new Vector2(0f, 1f);
      navRect.anchorMax = new Vector2(1f, 1f);
      navRect.pivot = new Vector2(0.5f, 1f);
      navRect.anchoredPosition = new Vector2(0f, -58f);
      navRect.sizeDelta = new Vector2(-28f, 34f);

      HorizontalLayoutGroup navHGroup = navObj.AddComponent<HorizontalLayoutGroup>();
      navHGroup.spacing = 4f;
      navHGroup.childControlWidth = true;
      navHGroup.childControlHeight = true;

      var tabTeleportObj = CreateButton("TabTeleport", navObj.transform, Vector2.zero, "Teleport", 12f, buttonSprite, font);
      var tabWorldObj = CreateButton("TabWorld", navObj.transform, Vector2.zero, "World", 12f, buttonSprite, font);
      var tabPlayerObj = CreateButton("TabPlayer", navObj.transform, Vector2.zero, "Player", 12f, buttonSprite, font);
      var tabCombatObj = CreateButton("TabCombat", navObj.transform, Vector2.zero, "Monsters", 12f, buttonSprite, font);
      var tabCreativeObj = CreateButton("TabCreative", navObj.transform, Vector2.zero, "Creative", 12f, buttonSprite, font);

      so.FindProperty("btnTabTeleport").objectReferenceValue = tabTeleportObj.GetComponent<Button>();
      so.FindProperty("btnTabWorld").objectReferenceValue = tabWorldObj.GetComponent<Button>();
      so.FindProperty("btnTabPlayer").objectReferenceValue = tabPlayerObj.GetComponent<Button>();
      so.FindProperty("btnTabCombat").objectReferenceValue = tabCombatObj.GetComponent<Button>();
      so.FindProperty("btnTabCreative").objectReferenceValue = tabCreativeObj.GetComponent<Button>();

      // 3. Tab Content Container
      GameObject bodyContainer = new GameObject("TabContentContainer");
      bodyContainer.transform.SetParent(panelObj.transform, false);
      RectTransform bodyRect = bodyContainer.AddComponent<RectTransform>();
      bodyRect.anchorMin = new Vector2(0f, 0f);
      bodyRect.anchorMax = new Vector2(1f, 1f);
      bodyRect.offsetMin = new Vector2(16f, 48f);
      bodyRect.offsetMax = new Vector2(-16f, -98f);

      // =============================================================
      // TAB 1: TELEPORT (DYNAMIC LANDSCENE & DUNGEONSCENE)
      // =============================================================
      GameObject pTeleport = CreateTabContentPanel("Panel_Teleport", bodyContainer.transform);
      so.FindProperty("panelTeleport").objectReferenceValue = pTeleport;

      // 1.1 LandScene Teleports
      GameObject landTeleObj = CreateContainer("LandTeleportContainer", pTeleport.transform);
      so.FindProperty("landTeleportContainer").objectReferenceValue = landTeleObj;

      var bBell = CreateActionButton("BtnTeleBell", landTeleObj.transform, "🔔 Village Bell (Central Hub)", "Respawn altar & Haven village center", buttonSprite, font, new Color(0.2f, 0.45f, 0.35f));
      var bPortal = CreateActionButton("BtnTelePortal", landTeleObj.transform, "🚪 Dungeon Portal", "Ancient stone portal leading into Dungeon", buttonSprite, font, new Color(0.2f, 0.35f, 0.55f));
      var bEqShop = CreateActionButton("BtnTeleEqShop", landTeleObj.transform, "⚔️ Equipment & Armaments Shop", "Weapons & Armor merchant NPC", buttonSprite, font);
      var bSpShop = CreateActionButton("BtnTeleSpShop", landTeleObj.transform, "🧪 Supplies & Potions Shop", "Dungeon supplies & consumable potions NPC", buttonSprite, font);
      var bSmithy = CreateActionButton("BtnTeleSmithy", landTeleObj.transform, "🔨 Village Blacksmith", "Weapon upgrade & artifact forge NPC", buttonSprite, font, new Color(0.55f, 0.35f, 0.15f));
      var bWindmill = CreateActionButton("BtnTeleWindmill", landTeleObj.transform, "💨 Windmill Lookout", "Scenic high cliff overlook of the village", buttonSprite, font);
      var bFastDun1 = CreateActionButton("BtnFastTravelDungeon1", landTeleObj.transform, "🚀 FAST TRAVEL TO DUNGEON", "Instant scene jump into Dungeon Floor 1", buttonSprite, font, new Color(0.18f, 0.55f, 0.35f));

      so.FindProperty("btnTeleVillageBell").objectReferenceValue = bBell.GetComponent<Button>();
      so.FindProperty("btnTeleVillagePortal").objectReferenceValue = bPortal.GetComponent<Button>();
      so.FindProperty("btnTeleEquipShop").objectReferenceValue = bEqShop.GetComponent<Button>();
      so.FindProperty("btnTeleSuppliesShop").objectReferenceValue = bSpShop.GetComponent<Button>();
      so.FindProperty("btnTeleBlacksmith").objectReferenceValue = bSmithy.GetComponent<Button>();
      so.FindProperty("btnTeleWindmill").objectReferenceValue = bWindmill.GetComponent<Button>();
      so.FindProperty("btnFastTravelDungeon1").objectReferenceValue = bFastDun1.GetComponent<Button>();

      // 1.2 DungeonScene Teleports
      GameObject dunTeleObj = CreateContainer("DungeonTeleportContainer", pTeleport.transform);
      so.FindProperty("dungeonTeleportContainer").objectReferenceValue = dunTeleObj;

      var bDunStart = CreateActionButton("BtnTeleDunStart", dunTeleObj.transform, "📍 Start Room / Entrance", "Safely behind doorway in entrance lobby", buttonSprite, font);
      var bDunBoss = CreateActionButton("BtnTeleDunBoss", dunTeleObj.transform, "👑 Boss Grand Gate", "Before the avenue grand gate", buttonSprite, font, new Color(0.6f, 0.15f, 0.15f));
      var bDunArena = CreateActionButton("BtnTeleDunArena", dunTeleObj.transform, "⚔️ Inside Boss Arena (Center)", "Directly inside Ice Golem arena", buttonSprite, font, new Color(0.7f, 0.25f, 0.15f));
      var bDunKey = CreateActionButton("BtnTeleDunKey", dunTeleObj.transform, "🗝️ Key Shrine Room", "In front of Shrine alter for Boss Key", buttonSprite, font, new Color(0.2f, 0.45f, 0.55f));
      var bDunTreasure = CreateActionButton("BtnTeleDunTreasure", dunTeleObj.transform, "💰 Treasure Rooms (Cycle)", "Cycle through procedural treasure rooms", buttonSprite, font, new Color(0.55f, 0.45f, 0.15f));
      var bDunMez = CreateActionButton("BtnTeleDunMez", dunTeleObj.transform, "🏰 2nd Floor Mezzanine (8m)", "Elevated mezzanine floor / balcony stairs", buttonSprite, font, new Color(0.35f, 0.25f, 0.5f));
      var bFastVil1 = CreateActionButton("BtnFastTravelVillage1", dunTeleObj.transform, "🏡 RETURN TO HAVEN VILLAGE", "Exit dungeon & preserve all inventory loot", buttonSprite, font, new Color(0.2f, 0.5f, 0.25f));

      so.FindProperty("btnTeleDungeonStart").objectReferenceValue = bDunStart.GetComponent<Button>();
      so.FindProperty("btnTeleDungeonBoss").objectReferenceValue = bDunBoss.GetComponent<Button>();
      so.FindProperty("btnTeleDungeonArena").objectReferenceValue = bDunArena.GetComponent<Button>();
      so.FindProperty("btnTeleDungeonKey").objectReferenceValue = bDunKey.GetComponent<Button>();
      so.FindProperty("btnTeleDungeonTreasure").objectReferenceValue = bDunTreasure.GetComponent<Button>();
      so.FindProperty("btnTeleDungeonMezzanine").objectReferenceValue = bDunMez.GetComponent<Button>();
      so.FindProperty("btnFastTravelVillage1").objectReferenceValue = bFastVil1.GetComponent<Button>();

      // =============================================================
      // TAB 2: WORLD & SCENE CONTROLS
      // =============================================================
      GameObject pWorld = CreateTabContentPanel("Panel_World", bodyContainer.transform);
      so.FindProperty("panelWorld").objectReferenceValue = pWorld;

      // 2.1 LandScene World Controls
      GameObject landWorldObj = CreateContainer("LandWorldContainer", pWorld.transform);
      so.FindProperty("landWorldContainer").objectReferenceValue = landWorldObj;

      var bAdvDay = CreateActionButton("BtnAdvanceDay", landWorldObj.transform, "🌅 Advance To Next Day (Day +1)", "Advance core game loop & refresh village", buttonSprite, font, new Color(0.6f, 0.45f, 0.15f));
      var bResDay = CreateActionButton("BtnResetDay", landWorldObj.transform, "⏪ Reset To Day 1", "Reset day counter back to Day 1", buttonSprite, font);
      var bFastDun2 = CreateActionButton("BtnFastTravelDungeon2", landWorldObj.transform, "🚀 Fast Travel To Dungeon", "Instant scene transition to Dungeon Floor 1", buttonSprite, font, new Color(0.2f, 0.45f, 0.6f));

      so.FindProperty("btnAdvanceDay").objectReferenceValue = bAdvDay.GetComponent<Button>();
      so.FindProperty("btnResetDay").objectReferenceValue = bResDay.GetComponent<Button>();
      so.FindProperty("btnFastTravelDungeon2").objectReferenceValue = bFastDun2.GetComponent<Button>();

      // 2.2 DungeonScene World Controls
      GameObject dunWorldObj = CreateContainer("DungeonWorldContainer", pWorld.transform);
      so.FindProperty("dungeonWorldContainer").objectReferenceValue = dunWorldObj;

      var bRegen = CreateActionButton("BtnRegenerate", dunWorldObj.transform, "🔄 REGENERATE DUNGEON (New Seed)", "Wipe old map, generate new seed & rebake NavMesh", buttonSprite, font, new Color(0.18f, 0.55f, 0.35f));
      var bRevealMap = CreateActionButton("BtnRevealMap", dunWorldObj.transform, "🗺️ REVEAL ENTIRE DUNGEON MAP", "Clear 100% fog of war, show all rooms & corridors", buttonSprite, font, new Color(0.2f, 0.45f, 0.65f));
      var bUnlockGate = CreateActionButton("BtnUnlockGate", dunWorldObj.transform, "🗝️ Force Unlock Boss Gate", "Open Grand Gate directly without finding key", buttonSprite, font, new Color(0.55f, 0.45f, 0.15f));
      var bKillRad = CreateActionButton("BtnKillRadius", dunWorldObj.transform, "⚡ Kill Nearby Enemies (25m Radius)", "Eliminate all active monsters around player", buttonSprite, font, new Color(0.6f, 0.3f, 0.15f));
      var bKillAll = CreateActionButton("BtnKillAll", dunWorldObj.transform, "☠️ Kill All Enemies In Dungeon", "Wipe out all monsters across entire map", buttonSprite, font, new Color(0.65f, 0.15f, 0.15f));
      var bFastVil2 = CreateActionButton("BtnFastTravelVillage2", dunWorldObj.transform, "🏡 Return To Haven Village", "Exit dungeon & preserve all inventory loot", buttonSprite, font, new Color(0.2f, 0.5f, 0.25f));

      so.FindProperty("btnRegenerateDungeon").objectReferenceValue = bRegen.GetComponent<Button>();
      so.FindProperty("btnRevealMap").objectReferenceValue = bRevealMap.GetComponent<Button>();
      so.FindProperty("btnUnlockBossGate").objectReferenceValue = bUnlockGate.GetComponent<Button>();
      so.FindProperty("btnKillRadius").objectReferenceValue = bKillRad.GetComponent<Button>();
      so.FindProperty("btnKillAll").objectReferenceValue = bKillAll.GetComponent<Button>();
      so.FindProperty("btnFastTravelVillage2").objectReferenceValue = bFastVil2.GetComponent<Button>();

      // =============================================================
      // TAB 3: PLAYER CHEATS & RPG PROGRESSION
      // =============================================================
      GameObject pPlayer = CreateTabContentPanel("Panel_Player", bodyContainer.transform);
      so.FindProperty("panelPlayer").objectReferenceValue = pPlayer;

      var (bGod, txtGod) = CreateToggleActionButton("BtnGodMode", pPlayer.transform, "🛡️ God Mode (Invulnerable)", "[OFF]", buttonSprite, font);
      var (bStam, txtStam) = CreateToggleActionButton("BtnInfStamina", pPlayer.transform, "⚡ Infinite Stamina", "[OFF]", buttonSprite, font);
      var (bSpirit, txtSpirit) = CreateToggleActionButton("BtnInfSpirit", pPlayer.transform, "⭐ Infinite 4 Spirit Stars (200)", "[OFF]", buttonSprite, font);
      var (bNoCd, txtNoCd) = CreateToggleActionButton("BtnNoCooldown", pPlayer.transform, "⏱️ No Skill Cooldown (0s Delay)", "[OFF]", buttonSprite, font, new Color(0.5f, 0.2f, 0.4f));
      var bFullRestore = CreateActionButton("BtnFullRestore", pPlayer.transform, "❤️ Full Heal & Restore All", "Restore 100% Health, Stamina & 4 Spirit Stars", buttonSprite, font, new Color(0.2f, 0.5f, 0.25f));
      var (bSpeed, txtSpeed) = CreateToggleActionButton("BtnSuperSpeed", pPlayer.transform, "🏃 Move Speed Multiplier", "1.0x", buttonSprite, font);

      // Split Progression Rows (Gold / EXP / Points)
      var (bGold1k, bGold10k) = CreateSplitActionButtons("GoldRow", pPlayer.transform, "💰 +1,000 Gold", "💰 +10,000 Gold", buttonSprite, font);
      var (bExp500, bPts10) = CreateSplitActionButtons("ProgRow", pPlayer.transform, "📈 +500 EXP", "✨ +10 Stat Points", buttonSprite, font);

      so.FindProperty("btnToggleGodMode").objectReferenceValue = bGod.GetComponent<Button>();
      so.FindProperty("txtGodModeStatus").objectReferenceValue = txtGod;
      so.FindProperty("btnToggleInfStamina").objectReferenceValue = bStam.GetComponent<Button>();
      so.FindProperty("txtInfStaminaStatus").objectReferenceValue = txtStam;
      so.FindProperty("btnToggleInfSpirit").objectReferenceValue = bSpirit.GetComponent<Button>();
      so.FindProperty("txtInfSpiritStatus").objectReferenceValue = txtSpirit;
      so.FindProperty("btnToggleNoCooldown").objectReferenceValue = bNoCd.GetComponent<Button>();
      so.FindProperty("txtNoCooldownStatus").objectReferenceValue = txtNoCd;
      so.FindProperty("btnFullRestore").objectReferenceValue = bFullRestore.GetComponent<Button>();
      so.FindProperty("btnSuperSpeed").objectReferenceValue = bSpeed.GetComponent<Button>();
      so.FindProperty("txtSuperSpeed").objectReferenceValue = txtSpeed;
      so.FindProperty("btnAddGold1k").objectReferenceValue = bGold1k.GetComponent<Button>();
      so.FindProperty("btnAddGold10k").objectReferenceValue = bGold10k.GetComponent<Button>();
      so.FindProperty("btnAddExp500").objectReferenceValue = bExp500.GetComponent<Button>();
      so.FindProperty("btnAddPoints10").objectReferenceValue = bPts10.GetComponent<Button>();

      // =============================================================
      // TAB 4: MONSTERS & TRAINING DUMMIES
      // =============================================================
      GameObject pCombat = CreateTabContentPanel("Panel_Combat", bodyContainer.transform);
      so.FindProperty("panelCombat").objectReferenceValue = pCombat;

      // Dummy Target Section
      var bSpawnDummy = CreateActionButton("BtnSpawnDummy", pCombat.transform, "🎯 SPAWN 10,000 HP DUMMY", "Stationary, No AI, 0 DEF, Pure DMG Test", buttonSprite, font, new Color(0.6f, 0.2f, 0.15f));
      var (bHealDum, bClearDum) = CreateSplitActionButtons("DummyManageRow", pCombat.transform, "❤️ Heal Dummies (10k)", "🧹 Clear All Dummies", buttonSprite, font, new Color(0.2f, 0.45f, 0.25f), new Color(0.45f, 0.2f, 0.2f));

      var (bFreeze, txtFreeze) = CreateToggleActionButton("BtnFreezeAI", pCombat.transform, "❄️ Freeze Enemy AI (All Monsters)", "[ACTIVE]", buttonSprite, font, new Color(0.15f, 0.35f, 0.55f));
      var bSpawnW = CreateActionButton("BtnSpawnWarrior", pCombat.transform, "⚔️ Spawn Skeleton Warrior (Melee)", "Spawn 3.5m ahead on NavMesh", buttonSprite, font);
      var bSpawnM = CreateActionButton("BtnSpawnMinion", pCombat.transform, "💀 Spawn Skeleton Minion (Swarm)", "Spawn low-health flanker 3.5m ahead", buttonSprite, font);
      var bSpawnR = CreateActionButton("BtnSpawnRogue", pCombat.transform, "🏹 Spawn Skeleton Rogue (Archer)", "Spawn ranged sniper 3.5m ahead", buttonSprite, font);
      var bSpawnMg = CreateActionButton("BtnSpawnMage", pCombat.transform, "🔮 Spawn Skeleton Mage (Spellcaster)", "Spawn magic caster 3.5m ahead", buttonSprite, font);

      so.FindProperty("btnSpawnDummy").objectReferenceValue = bSpawnDummy.GetComponent<Button>();
      so.FindProperty("btnHealDummies").objectReferenceValue = bHealDum.GetComponent<Button>();
      so.FindProperty("btnClearDummies").objectReferenceValue = bClearDum.GetComponent<Button>();
      so.FindProperty("btnToggleFreezeAI").objectReferenceValue = bFreeze.GetComponent<Button>();
      so.FindProperty("txtFreezeAIStatus").objectReferenceValue = txtFreeze;
      so.FindProperty("btnSpawnWarrior").objectReferenceValue = bSpawnW.GetComponent<Button>();
      so.FindProperty("btnSpawnMinion").objectReferenceValue = bSpawnM.GetComponent<Button>();
      so.FindProperty("btnSpawnRogue").objectReferenceValue = bSpawnR.GetComponent<Button>();
      so.FindProperty("btnSpawnMage").objectReferenceValue = bSpawnMg.GetComponent<Button>();

      // =============================================================
      // TAB 5: CREATIVE MODE (ALL 51 GAME ITEMS)
      // =============================================================
      GameObject pCreative = new GameObject("Panel_Creative");
      pCreative.transform.SetParent(bodyContainer.transform, false);
      RectTransform crt = pCreative.AddComponent<RectTransform>();
      crt.anchorMin = Vector2.zero;
      crt.anchorMax = Vector2.one;
      crt.offsetMin = Vector2.zero;
      crt.offsetMax = Vector2.zero;
      so.FindProperty("panelCreative").objectReferenceValue = pCreative;

      // 5.1 Search Input Field
      GameObject searchObj = CreateInputField("SearchField", pCreative.transform, "🔍 Search 51 items...", buttonSprite, font);
      RectTransform sfRt = searchObj.GetComponent<RectTransform>();
      sfRt.anchorMin = new Vector2(0f, 1f);
      sfRt.anchorMax = new Vector2(1f, 1f);
      sfRt.pivot = new Vector2(0.5f, 1f);
      sfRt.anchoredPosition = new Vector2(0f, 0f);
      sfRt.sizeDelta = new Vector2(0f, 32f);
      so.FindProperty("searchField").objectReferenceValue = searchObj.GetComponent<TMP_InputField>();

      // 5.2 Category Filter Buttons Bar
      GameObject filterBarObj = new GameObject("FilterBar");
      filterBarObj.transform.SetParent(pCreative.transform, false);
      RectTransform fbRt = filterBarObj.AddComponent<RectTransform>();
      fbRt.anchorMin = new Vector2(0f, 1f);
      fbRt.anchorMax = new Vector2(1f, 1f);
      fbRt.pivot = new Vector2(0.5f, 1f);
      fbRt.anchoredPosition = new Vector2(0f, -36f);
      fbRt.sizeDelta = new Vector2(0f, 26f);

      HorizontalLayoutGroup fbHlg = filterBarObj.AddComponent<HorizontalLayoutGroup>();
      fbHlg.spacing = 3f;
      fbHlg.childControlWidth = true;
      fbHlg.childControlHeight = true;

      var bFAll = CreateButton("BtnFilterAll", filterBarObj.transform, Vector2.zero, "ALL", 10f, buttonSprite, font);
      var bFWpn = CreateButton("BtnFilterWpn", filterBarObj.transform, Vector2.zero, "WEAPON", 10f, buttonSprite, font);
      var bFArm = CreateButton("BtnFilterArm", filterBarObj.transform, Vector2.zero, "ARMOR", 10f, buttonSprite, font);
      var bFPot = CreateButton("BtnFilterPot", filterBarObj.transform, Vector2.zero, "POTION", 10f, buttonSprite, font);
      var bFMat = CreateButton("BtnFilterMat", filterBarObj.transform, Vector2.zero, "MATERIAL", 10f, buttonSprite, font);
      var bFMisc = CreateButton("BtnFilterMisc", filterBarObj.transform, Vector2.zero, "KEY/GOLD", 10f, buttonSprite, font);

      so.FindProperty("btnFilterAll").objectReferenceValue = bFAll.GetComponent<Button>();
      so.FindProperty("btnFilterWeapon").objectReferenceValue = bFWpn.GetComponent<Button>();
      so.FindProperty("btnFilterArmor").objectReferenceValue = bFArm.GetComponent<Button>();
      so.FindProperty("btnFilterConsumable").objectReferenceValue = bFPot.GetComponent<Button>();
      so.FindProperty("btnFilterMaterial").objectReferenceValue = bFMat.GetComponent<Button>();
      so.FindProperty("btnFilterMisc").objectReferenceValue = bFMisc.GetComponent<Button>();

      // 5.3 ScrollView for Items List
      GameObject scrollObj = new GameObject("CreativeScrollView");
      scrollObj.transform.SetParent(pCreative.transform, false);
      RectTransform sRt = scrollObj.AddComponent<RectTransform>();
      sRt.anchorMin = Vector2.zero;
      sRt.anchorMax = Vector2.one;
      sRt.offsetMin = new Vector2(0f, 0f);
      sRt.offsetMax = new Vector2(0f, -66f);

      ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
      scrollRect.horizontal = false;
      scrollRect.vertical = true;
      scrollRect.movementType = ScrollRect.MovementType.Clamped;
      scrollRect.scrollSensitivity = 25f;

      GameObject viewportObj = new GameObject("Viewport");
      viewportObj.transform.SetParent(scrollObj.transform, false);
      RectTransform vpRt = viewportObj.AddComponent<RectTransform>();
      vpRt.anchorMin = Vector2.zero;
      vpRt.anchorMax = Vector2.one;
      vpRt.offsetMin = Vector2.zero;
      vpRt.offsetMax = Vector2.zero;
      viewportObj.AddComponent<RectMask2D>();
      scrollRect.viewport = vpRt;

      GameObject contentObj = new GameObject("Content");
      contentObj.transform.SetParent(viewportObj.transform, false);
      RectTransform cRt = contentObj.AddComponent<RectTransform>();
      cRt.anchorMin = new Vector2(0f, 1f);
      cRt.anchorMax = new Vector2(1f, 1f);
      cRt.pivot = new Vector2(0.5f, 1f);
      cRt.anchoredPosition = Vector2.zero;
      cRt.sizeDelta = new Vector2(0f, 0f);

      VerticalLayoutGroup cvlg = contentObj.AddComponent<VerticalLayoutGroup>();
      cvlg.spacing = 3f;
      cvlg.childControlWidth = true;
      cvlg.childControlHeight = false;

      ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
      csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      scrollRect.content = cRt;
      so.FindProperty("creativeItemsScrollContent").objectReferenceValue = cRt;

      // 5.4 Item Row Template
      GameObject rowTemplate = CreateCreativeRowTemplate("CreativeRowTemplate", contentObj.transform, buttonSprite, font);
      so.FindProperty("creativeItemRowTemplate").objectReferenceValue = rowTemplate;

      // 4. Toast Notification Bar
      GameObject toastObj = new GameObject("ToastBar");
      toastObj.transform.SetParent(panelObj.transform, false);
      RectTransform toastRect = toastObj.AddComponent<RectTransform>();
      toastRect.anchorMin = new Vector2(0f, 0f);
      toastRect.anchorMax = new Vector2(1f, 0f);
      toastRect.pivot = new Vector2(0.5f, 0f);
      toastRect.anchoredPosition = new Vector2(0f, 8f);
      toastRect.sizeDelta = new Vector2(-28f, 32f);

      CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
      so.FindProperty("toastCanvasGroup").objectReferenceValue = toastCg;

      Image toastBg = toastObj.AddComponent<Image>();
      if (rowBgSprite != null) toastBg.sprite = rowBgSprite;
      toastBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

      GameObject toastTextObj = CreateText("ToastText", toastObj.transform, "Admin Console ready for testing!", 12f, font, new Color(0.2f, 1f, 0.6f));
      RectTransform ttRect = toastTextObj.GetComponent<RectTransform>();
      ttRect.anchorMin = Vector2.zero;
      ttRect.anchorMax = Vector2.one;
      ttRect.offsetMin = new Vector2(8f, 0f);
      ttRect.offsetMax = new Vector2(-8f, 0f);
      toastTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
      so.FindProperty("toastText").objectReferenceValue = toastTextObj.GetComponent<TextMeshProUGUI>();

      so.ApplyModifiedProperties();

      // Export as reusable Prefab
      string folder = "Assets/Prefabs/Admin";
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
      if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Admin");
      PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, $"{folder}/AdminCheatCanvas.prefab", InteractionMode.AutomatedAction);

      return canvasObj;
    }

    private static GameObject CreateContainer(string name, Transform parent)
    {
      GameObject obj = new GameObject(name);
      obj.transform.SetParent(parent, false);
      RectTransform rt = obj.AddComponent<RectTransform>();
      rt.sizeDelta = Vector2.zero;

      VerticalLayoutGroup vlg = obj.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = 5f;
      vlg.childControlWidth = true;
      vlg.childControlHeight = false;

      return obj;
    }

    private static GameObject CreateTabContentPanel(string name, Transform parent)
    {
      GameObject obj = new GameObject(name);
      obj.transform.SetParent(parent, false);
      RectTransform rt = obj.AddComponent<RectTransform>();
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;

      VerticalLayoutGroup vlg = obj.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = 5f;
      vlg.childControlWidth = true;
      vlg.childControlHeight = false;

      return obj;
    }

    private static GameObject CreateActionButton(string name, Transform parent, string title, string subtitle, Sprite btnSprite, TMP_FontAsset font, Color? bgColor = null)
    {
      GameObject btnObj = new GameObject(name);
      btnObj.transform.SetParent(parent, false);
      RectTransform rt = btnObj.AddComponent<RectTransform>();
      rt.sizeDelta = new Vector2(0f, 42f);

      Image img = btnObj.AddComponent<Image>();
      if (btnSprite != null)
      {
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
      }
      img.color = bgColor ?? new Color(0.20f, 0.20f, 0.26f, 0.95f);

      Button btn = btnObj.AddComponent<Button>();
      btn.targetGraphic = img;

      GameObject textContainer = new GameObject("TextContainer");
      textContainer.transform.SetParent(btnObj.transform, false);
      RectTransform tcRt = textContainer.AddComponent<RectTransform>();
      tcRt.anchorMin = Vector2.zero;
      tcRt.anchorMax = Vector2.one;
      tcRt.offsetMin = new Vector2(10f, 2f);
      tcRt.offsetMax = new Vector2(-10f, -2f);

      VerticalLayoutGroup vlg = textContainer.AddComponent<VerticalLayoutGroup>();
      vlg.childControlWidth = true;
      vlg.childControlHeight = true;
      vlg.spacing = 0f;

      GameObject titleObj = CreateText("Title", textContainer.transform, title, 13f, font, Color.white, FontStyles.Bold);
      titleObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

      if (!string.IsNullOrEmpty(subtitle))
      {
        GameObject subObj = CreateText("Subtitle", textContainer.transform, subtitle, 10f, font, new Color(0.7f, 0.7f, 0.75f));
        subObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
      }

      return btnObj;
    }

    private static (GameObject, TextMeshProUGUI) CreateToggleActionButton(string name, Transform parent, string title, string initialStatus, Sprite btnSprite, TMP_FontAsset font, Color? bgColor = null)
    {
      GameObject btnObj = new GameObject(name);
      btnObj.transform.SetParent(parent, false);
      RectTransform rt = btnObj.AddComponent<RectTransform>();
      rt.sizeDelta = new Vector2(0f, 40f);

      Image img = btnObj.AddComponent<Image>();
      if (btnSprite != null)
      {
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
      }
      img.color = bgColor ?? new Color(0.20f, 0.20f, 0.26f, 0.95f);

      Button btn = btnObj.AddComponent<Button>();
      btn.targetGraphic = img;

      GameObject labelObj = CreateText("Label", btnObj.transform, title, 13f, font, Color.white, FontStyles.Bold);
      RectTransform lblRt = labelObj.GetComponent<RectTransform>();
      lblRt.anchorMin = new Vector2(0f, 0f);
      lblRt.anchorMax = new Vector2(0.72f, 1f);
      lblRt.offsetMin = new Vector2(12f, 0f);
      lblRt.offsetMax = Vector2.zero;
      labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

      GameObject statusObj = CreateText("Status", btnObj.transform, initialStatus, 13f, font, Color.yellow, FontStyles.Bold);
      RectTransform stRt = statusObj.GetComponent<RectTransform>();
      stRt.anchorMin = new Vector2(0.72f, 0f);
      stRt.anchorMax = new Vector2(1f, 1f);
      stRt.offsetMin = Vector2.zero;
      stRt.offsetMax = new Vector2(-12f, 0f);
      TextMeshProUGUI stTmp = statusObj.GetComponent<TextMeshProUGUI>();
      stTmp.alignment = TextAlignmentOptions.MidlineRight;

      return (btnObj, stTmp);
    }

    private static (GameObject, GameObject) CreateSplitActionButtons(string name, Transform parent, string title1, string title2, Sprite btnSprite, TMP_FontAsset font, Color? color1 = null, Color? color2 = null)
    {
      GameObject rowObj = new GameObject(name);
      rowObj.transform.SetParent(parent, false);
      RectTransform rt = rowObj.AddComponent<RectTransform>();
      rt.sizeDelta = new Vector2(0f, 38f);

      HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
      hlg.spacing = 6f;
      hlg.childControlWidth = true;
      hlg.childControlHeight = true;

      var b1 = CreateButton("Btn1", rowObj.transform, Vector2.zero, title1, 12f, btnSprite, font, color1);
      var b2 = CreateButton("Btn2", rowObj.transform, Vector2.zero, title2, 12f, btnSprite, font, color2);

      return (b1, b2);
    }

    private static GameObject CreateInputField(string name, Transform parent, string placeholderText, Sprite bgSprite, TMP_FontAsset font)
    {
      GameObject ifObj = new GameObject(name);
      ifObj.transform.SetParent(parent, false);
      RectTransform rt = ifObj.AddComponent<RectTransform>();

      Image bg = ifObj.AddComponent<Image>();
      if (bgSprite != null)
      {
        bg.sprite = bgSprite;
        bg.type = Image.Type.Sliced;
      }
      bg.color = new Color(0.12f, 0.12f, 0.17f, 0.95f);

      TMP_InputField inputField = ifObj.AddComponent<TMP_InputField>();

      GameObject textArea = new GameObject("TextArea");
      textArea.transform.SetParent(ifObj.transform, false);
      RectTransform taRt = textArea.AddComponent<RectTransform>();
      taRt.anchorMin = Vector2.zero;
      taRt.anchorMax = Vector2.one;
      taRt.offsetMin = new Vector2(10f, 2f);
      taRt.offsetMax = new Vector2(-10f, -2f);
      textArea.AddComponent<RectMask2D>();

      GameObject phObj = CreateText("Placeholder", textArea.transform, placeholderText, 12f, font, new Color(0.6f, 0.6f, 0.65f), FontStyles.Italic);
      RectTransform phRt = phObj.GetComponent<RectTransform>();
      phRt.anchorMin = Vector2.zero;
      phRt.anchorMax = Vector2.one;
      phRt.offsetMin = Vector2.zero;
      phRt.offsetMax = Vector2.zero;
      TextMeshProUGUI phTmp = phObj.GetComponent<TextMeshProUGUI>();
      phTmp.alignment = TextAlignmentOptions.MidlineLeft;

      GameObject textObj = CreateText("Text", textArea.transform, "", 12f, font, Color.white);
      RectTransform tRt = textObj.GetComponent<RectTransform>();
      tRt.anchorMin = Vector2.zero;
      tRt.anchorMax = Vector2.one;
      tRt.offsetMin = Vector2.zero;
      tRt.offsetMax = Vector2.zero;
      TextMeshProUGUI tTmp = textObj.GetComponent<TextMeshProUGUI>();
      tTmp.alignment = TextAlignmentOptions.MidlineLeft;

      inputField.textViewport = taRt;
      inputField.textComponent = tTmp;
      inputField.placeholder = phTmp;

      return ifObj;
    }

    private static GameObject CreateCreativeRowTemplate(string name, Transform parent, Sprite btnSprite, TMP_FontAsset font)
    {
      GameObject rowObj = new GameObject(name);
      rowObj.transform.SetParent(parent, false);
      RectTransform rt = rowObj.AddComponent<RectTransform>();
      rt.sizeDelta = new Vector2(0f, 44f);

      Image bg = rowObj.AddComponent<Image>();
      if (btnSprite != null)
      {
        bg.sprite = btnSprite;
        bg.type = Image.Type.Sliced;
      }
      bg.color = new Color(0.16f, 0.16f, 0.22f, 0.95f);
      Button rowBtn = rowObj.AddComponent<Button>();
      rowBtn.targetGraphic = bg;

      // Icon & Rarity Border
      GameObject iconObj = new GameObject("Icon");
      iconObj.transform.SetParent(rowObj.transform, false);
      RectTransform iRt = iconObj.AddComponent<RectTransform>();
      iRt.anchorMin = new Vector2(0f, 0.5f);
      iRt.anchorMax = new Vector2(0f, 0.5f);
      iRt.pivot = new Vector2(0f, 0.5f);
      iRt.anchoredPosition = new Vector2(6f, 0f);
      iRt.sizeDelta = new Vector2(34f, 34f);
      Image iconImg = iconObj.AddComponent<Image>();
      iconImg.preserveAspect = true;

      GameObject rbObj = new GameObject("RarityBorder");
      rbObj.transform.SetParent(iconObj.transform, false);
      RectTransform rbRt = rbObj.AddComponent<RectTransform>();
      rbRt.anchorMin = Vector2.zero;
      rbRt.anchorMax = Vector2.one;
      rbRt.offsetMin = new Vector2(-2f, -2f);
      rbRt.offsetMax = new Vector2(2f, 2f);
      Image rbImg = rbObj.AddComponent<Image>();
      rbImg.color = new Color(1f, 1f, 1f, 0.35f);
      rbObj.transform.SetAsFirstSibling();

      // Title
      GameObject titleObj = CreateText("Title", rowObj.transform, "Item Name", 12f, font, Color.white, FontStyles.Bold);
      RectTransform titleRt = titleObj.GetComponent<RectTransform>();
      titleRt.anchorMin = new Vector2(0f, 0.5f);
      titleRt.anchorMax = new Vector2(1f, 1f);
      titleRt.offsetMin = new Vector2(48f, 0f);
      titleRt.offsetMax = new Vector2(-95f, -2f);
      titleObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

      // Subtitle
      GameObject subObj = CreateText("Subtitle", rowObj.transform, "Weapon | +15 ATK", 10f, font, new Color(0.7f, 0.7f, 0.75f));
      RectTransform subRt = subObj.GetComponent<RectTransform>();
      subRt.anchorMin = new Vector2(0f, 0f);
      subRt.anchorMax = new Vector2(1f, 0.5f);
      subRt.offsetMin = new Vector2(48f, 2f);
      subRt.offsetMax = new Vector2(-95f, 0f);
      subObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

      // Buttons (+1 and +Max)
      GameObject b1Obj = CreateButton("BtnPlusOne", rowObj.transform, new Vector2(38f, 28f), "+1", 11f, btnSprite, font, new Color(0.2f, 0.45f, 0.35f));
      RectTransform b1Rt = b1Obj.GetComponent<RectTransform>();
      b1Rt.anchorMin = new Vector2(1f, 0.5f);
      b1Rt.anchorMax = new Vector2(1f, 0.5f);
      b1Rt.pivot = new Vector2(1f, 0.5f);
      b1Rt.anchoredPosition = new Vector2(-50f, 0f);

      GameObject bMaxObj = CreateButton("BtnPlusMax", rowObj.transform, new Vector2(44f, 28f), "+Max", 11f, btnSprite, font, new Color(0.35f, 0.35f, 0.5f));
      RectTransform bMaxRt = bMaxObj.GetComponent<RectTransform>();
      bMaxRt.anchorMin = new Vector2(1f, 0.5f);
      bMaxRt.anchorMax = new Vector2(1f, 0.5f);
      bMaxRt.pivot = new Vector2(1f, 0.5f);
      bMaxRt.anchoredPosition = new Vector2(-4f, 0f);

      return rowObj;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 size, string text, float fontSize, Sprite btnSprite, TMP_FontAsset font, Color? tintColor = null)
    {
      GameObject btnObj = new GameObject(name);
      btnObj.transform.SetParent(parent, false);
      RectTransform rt = btnObj.AddComponent<RectTransform>();
      if (size != Vector2.zero) rt.sizeDelta = size;

      Image img = btnObj.AddComponent<Image>();
      if (btnSprite != null)
      {
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
      }
      img.color = tintColor ?? new Color(0.25f, 0.25f, 0.32f, 0.95f);

      Button btn = btnObj.AddComponent<Button>();
      btn.targetGraphic = img;

      GameObject textObj = CreateText("Text", btnObj.transform, text, fontSize, font, Color.white, FontStyles.Bold);
      RectTransform tRt = textObj.GetComponent<RectTransform>();
      tRt.anchorMin = Vector2.zero;
      tRt.anchorMax = Vector2.one;
      tRt.offsetMin = Vector2.zero;
      tRt.offsetMax = Vector2.zero;
      textObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

      return btnObj;
    }

    private static GameObject CreateText(string name, Transform parent, string content, float fontSize, TMP_FontAsset font, Color color, FontStyles style = FontStyles.Normal)
    {
      GameObject textObj = new GameObject(name);
      textObj.transform.SetParent(parent, false);
      TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
      if (font != null) tmp.font = font;
      tmp.text = content;
      tmp.fontSize = fontSize;
      tmp.color = color;
      tmp.fontStyle = style;
      return textObj;
    }

    private static void EnsureEventSystem()
    {
      if (EventSystem.current == null)
      {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing == null)
        {
          GameObject esObj = new GameObject("EventSystem");
          esObj.AddComponent<EventSystem>();
          esObj.AddComponent<InputSystemUIInputModule>();
        }
      }
    }
  }
}
