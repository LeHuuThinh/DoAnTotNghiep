using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// 1-Click Automated Setup Editor Tool for Admin Cheat Console.
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
        "• Press [~] (Tilde / Backquote) or [F1] during Play Mode to toggle the console.\n" +
        "• The Debug Info overlay (top-right) opens and closes simultaneously with the panel.\n" +
        "• Features 5 Tabs: Teleport, Dungeon, Player, Monsters, RPG & Loot in English!",
        "OK"
      );
    }

    private static GameObject EnsureAdminCheatManager()
    {
      var existing = Object.FindFirstObjectByType<AdminCheatManager>();
      GameObject obj = (existing != null) ? existing.gameObject : new GameObject("AdminCheatManager");

      AdminCheatManager manager = obj.GetComponent<AdminCheatManager>();
      if (manager == null) manager = obj.AddComponent<AdminCheatManager>();

      // Auto-assign ScriptableObject item assets
      manager.dungeonKeyItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Key_Dungeon.asset");
      manager.healthPotionItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Potion_Health.asset");
      manager.dragonicSwordItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Sword_Dragonic.asset");
      manager.leatherArmorItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Armor_Leather.asset");
      manager.upgradeStoneItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Stone_Upgrade.asset");

      // Auto-assign Monster test prefabs
      manager.skeletonWarriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");
      manager.skeletonMinionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");
      manager.skeletonRoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");
      manager.skeletonMagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");

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
        canvas.sortingOrder = 150; // Render on top of everything

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
      overlayRect.sizeDelta = new Vector2(290f, 105f);

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
      panelRect.sizeDelta = new Vector2(460f, 620f);

      CanvasGroup panelCg = panelObj.AddComponent<CanvasGroup>();
      so.FindProperty("mainPanelCanvasGroup").objectReferenceValue = panelCg;
      so.FindProperty("mainPanelRoot").objectReferenceValue = panelObj;

      Image panelBg = panelObj.AddComponent<Image>();
      if (panelSprite != null)
      {
        panelBg.sprite = panelSprite;
        panelBg.type = Image.Type.Sliced;
      }
      panelBg.color = new Color(0.09f, 0.08f, 0.13f, 0.98f);

      // 1. Header (Title + Close Button)
      GameObject headerObj = new GameObject("Header");
      headerObj.transform.SetParent(panelObj.transform, false);
      RectTransform headerRect = headerObj.AddComponent<RectTransform>();
      headerRect.anchorMin = new Vector2(0f, 1f);
      headerRect.anchorMax = new Vector2(1f, 1f);
      headerRect.pivot = new Vector2(0.5f, 1f);
      headerRect.anchoredPosition = new Vector2(0f, -12f);
      headerRect.sizeDelta = new Vector2(-28f, 40f);

      GameObject titleTextObj = CreateText("Title", headerObj.transform, "⚡ ADMIN CHEAT CONSOLE", 17f, font, new Color(1f, 0.88f, 0.45f), FontStyles.Bold);
      RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
      titleRect.anchorMin = new Vector2(0f, 0f);
      titleRect.anchorMax = new Vector2(0.85f, 1f);
      titleRect.offsetMin = Vector2.zero;
      titleRect.offsetMax = Vector2.zero;
      titleTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

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
      navRect.anchoredPosition = new Vector2(0f, -56f);
      navRect.sizeDelta = new Vector2(-28f, 34f);

      HorizontalLayoutGroup navHGroup = navObj.AddComponent<HorizontalLayoutGroup>();
      navHGroup.spacing = 4f;
      navHGroup.childControlWidth = true;
      navHGroup.childControlHeight = true;

      var tabTeleportObj = CreateButton("TabTeleport", navObj.transform, Vector2.zero, "Teleport", 12f, buttonSprite, font);
      var tabDungeonObj = CreateButton("TabDungeon", navObj.transform, Vector2.zero, "Dungeon", 12f, buttonSprite, font);
      var tabPlayerObj = CreateButton("TabPlayer", navObj.transform, Vector2.zero, "Player", 12f, buttonSprite, font);
      var tabCombatObj = CreateButton("TabCombat", navObj.transform, Vector2.zero, "Monsters", 12f, buttonSprite, font);
      var tabRpgObj = CreateButton("TabRPG", navObj.transform, Vector2.zero, "RPG / Loot", 12f, buttonSprite, font);

      so.FindProperty("btnTabTeleport").objectReferenceValue = tabTeleportObj.GetComponent<Button>();
      so.FindProperty("btnTabDungeon").objectReferenceValue = tabDungeonObj.GetComponent<Button>();
      so.FindProperty("btnTabPlayer").objectReferenceValue = tabPlayerObj.GetComponent<Button>();
      so.FindProperty("btnTabCombat").objectReferenceValue = tabCombatObj.GetComponent<Button>();
      so.FindProperty("btnTabRPG").objectReferenceValue = tabRpgObj.GetComponent<Button>();

      // 3. Tab Content Container
      GameObject bodyContainer = new GameObject("TabContentContainer");
      bodyContainer.transform.SetParent(panelObj.transform, false);
      RectTransform bodyRect = bodyContainer.AddComponent<RectTransform>();
      bodyRect.anchorMin = new Vector2(0f, 0f);
      bodyRect.anchorMax = new Vector2(1f, 1f);
      bodyRect.offsetMin = new Vector2(16f, 50f);
      bodyRect.offsetMax = new Vector2(-16f, -96f);

      // -------------------------------------------------------------
      // TAB 1: TELEPORT
      // -------------------------------------------------------------
      GameObject pTeleport = CreateTabContentPanel("Panel_Teleport", bodyContainer.transform);
      so.FindProperty("panelTeleport").objectReferenceValue = pTeleport;

      var bTeleStart = CreateActionButton("BtnTeleStart", pTeleport.transform, "📍 Start Room / Entrance", "Teleport safely behind doorway in entrance lobby", buttonSprite, font);
      var bTeleBoss = CreateActionButton("BtnTeleBoss", pTeleport.transform, "👑 Boss Arena (Ice Golem Gate)", "Teleport before Grand Gate on the Avenue", buttonSprite, font, new Color(0.6f, 0.15f, 0.15f));
      var bTeleKey = CreateActionButton("BtnTeleKey", pTeleport.transform, "🗝️ Key Shrine Room", "Teleport in front of Shrine alter for Boss Key", buttonSprite, font, new Color(0.2f, 0.45f, 0.55f));
      var bTeleTreasure = CreateActionButton("BtnTeleTreasure", pTeleport.transform, "💰 Treasure Rooms (Cycle)", "Cycle through procedural treasure rooms", buttonSprite, font, new Color(0.55f, 0.45f, 0.15f));
      var bTeleMez = CreateActionButton("BtnTeleMez", pTeleport.transform, "🏰 2nd Floor Mezzanine (8m Height)", "Teleport to elevated mezzanine floor / stairs", buttonSprite, font, new Color(0.35f, 0.25f, 0.5f));

      so.FindProperty("btnTeleportStart").objectReferenceValue = bTeleStart.GetComponent<Button>();
      so.FindProperty("btnTeleportBoss").objectReferenceValue = bTeleBoss.GetComponent<Button>();
      so.FindProperty("btnTeleportKey").objectReferenceValue = bTeleKey.GetComponent<Button>();
      so.FindProperty("btnTeleportTreasure").objectReferenceValue = bTeleTreasure.GetComponent<Button>();
      so.FindProperty("btnTeleportMezzanine").objectReferenceValue = bTeleMez.GetComponent<Button>();

      // -------------------------------------------------------------
      // TAB 2: DUNGEON
      // -------------------------------------------------------------
      GameObject pDungeon = CreateTabContentPanel("Panel_Dungeon", bodyContainer.transform);
      so.FindProperty("panelDungeon").objectReferenceValue = pDungeon;

      var bRegen = CreateActionButton("BtnRegenerate", pDungeon.transform, "🔄 REGENERATE DUNGEON (New Seed)", "Wipe old map, generate new seed & rebake NavMesh at runtime", buttonSprite, font, new Color(0.18f, 0.55f, 0.35f));
      var bKillRad = CreateActionButton("BtnKillRadius", pDungeon.transform, "⚡ Kill Nearby Enemies (25m Radius)", "Eliminate all active monsters around player", buttonSprite, font, new Color(0.6f, 0.3f, 0.15f));
      var bKillAll = CreateActionButton("BtnKillAll", pDungeon.transform, "☠️ Kill All Enemies In Dungeon", "Wipe out all monsters across entire map", buttonSprite, font, new Color(0.65f, 0.15f, 0.15f));

      so.FindProperty("btnRegenerateDungeon").objectReferenceValue = bRegen.GetComponent<Button>();
      so.FindProperty("btnKillRadius").objectReferenceValue = bKillRad.GetComponent<Button>();
      so.FindProperty("btnKillAll").objectReferenceValue = bKillAll.GetComponent<Button>();

      // -------------------------------------------------------------
      // TAB 3: PLAYER
      // -------------------------------------------------------------
      GameObject pPlayer = CreateTabContentPanel("Panel_Player", bodyContainer.transform);
      so.FindProperty("panelPlayer").objectReferenceValue = pPlayer;

      var (bGod, txtGod) = CreateToggleActionButton("BtnGodMode", pPlayer.transform, "🛡️ God Mode (Invulnerable)", "[OFF]", buttonSprite, font);
      var (bStam, txtStam) = CreateToggleActionButton("BtnInfStamina", pPlayer.transform, "⚡ Infinite Stamina", "[OFF]", buttonSprite, font);
      var (bSpirit, txtSpirit) = CreateToggleActionButton("BtnInfSpirit", pPlayer.transform, "⭐ Infinite 3 Spirit Stars (150)", "[OFF]", buttonSprite, font);
      var bFullRestore = CreateActionButton("BtnFullRestore", pPlayer.transform, "❤️ Full Heal & Restore", "Restore 100% Health, Stamina & 3 Spirit Stars", buttonSprite, font, new Color(0.2f, 0.5f, 0.25f));
      var (bSpeed, txtSpeed) = CreateToggleActionButton("BtnSuperSpeed", pPlayer.transform, "🏃 Move Speed Multiplier", "1.0x", buttonSprite, font);

      so.FindProperty("btnToggleGodMode").objectReferenceValue = bGod.GetComponent<Button>();
      so.FindProperty("txtGodModeStatus").objectReferenceValue = txtGod;
      so.FindProperty("btnToggleInfStamina").objectReferenceValue = bStam.GetComponent<Button>();
      so.FindProperty("txtInfStaminaStatus").objectReferenceValue = txtStam;
      so.FindProperty("btnToggleInfSpirit").objectReferenceValue = bSpirit.GetComponent<Button>();
      so.FindProperty("txtInfSpiritStatus").objectReferenceValue = txtSpirit;
      so.FindProperty("btnFullRestore").objectReferenceValue = bFullRestore.GetComponent<Button>();
      so.FindProperty("btnSuperSpeed").objectReferenceValue = bSpeed.GetComponent<Button>();
      so.FindProperty("txtSuperSpeed").objectReferenceValue = txtSpeed;

      // -------------------------------------------------------------
      // TAB 4: MONSTERS
      // -------------------------------------------------------------
      GameObject pCombat = CreateTabContentPanel("Panel_Combat", bodyContainer.transform);
      so.FindProperty("panelCombat").objectReferenceValue = pCombat;

      var (bFreeze, txtFreeze) = CreateToggleActionButton("BtnFreezeAI", pCombat.transform, "❄️ Freeze Enemy AI (Training Dummy)", "[ACTIVE]", buttonSprite, font, new Color(0.15f, 0.35f, 0.55f));
      var bSpawnW = CreateActionButton("BtnSpawnWarrior", pCombat.transform, "⚔️ Spawn Skeleton Warrior (Melee)", "Spawn 3.5m ahead on NavMesh", buttonSprite, font);
      var bSpawnM = CreateActionButton("BtnSpawnMinion", pCombat.transform, "💀 Spawn Skeleton Minion (Swarm)", "Spawn low-health flanker 3.5m ahead", buttonSprite, font);
      var bSpawnR = CreateActionButton("BtnSpawnRogue", pCombat.transform, "🏹 Spawn Skeleton Rogue (Archer)", "Spawn ranged sniper 3.5m ahead", buttonSprite, font);
      var bSpawnMg = CreateActionButton("BtnSpawnMage", pCombat.transform, "🔮 Spawn Skeleton Mage (Spellcaster)", "Spawn magic caster 3.5m ahead", buttonSprite, font);

      so.FindProperty("btnToggleFreezeAI").objectReferenceValue = bFreeze.GetComponent<Button>();
      so.FindProperty("txtFreezeAIStatus").objectReferenceValue = txtFreeze;
      so.FindProperty("btnSpawnWarrior").objectReferenceValue = bSpawnW.GetComponent<Button>();
      so.FindProperty("btnSpawnMinion").objectReferenceValue = bSpawnM.GetComponent<Button>();
      so.FindProperty("btnSpawnRogue").objectReferenceValue = bSpawnR.GetComponent<Button>();
      so.FindProperty("btnSpawnMage").objectReferenceValue = bSpawnMg.GetComponent<Button>();

      // -------------------------------------------------------------
      // TAB 5: RPG & LOOT
      // -------------------------------------------------------------
      GameObject pRpg = CreateTabContentPanel("Panel_RPG", bodyContainer.transform);
      so.FindProperty("panelRPG").objectReferenceValue = pRpg;

      var bAddKey = CreateActionButton("BtnAddKey", pRpg.transform, "🗝️ Add 1 Boss Key (Dungeon Key)", "Add key directly to inventory to open Boss Gate", buttonSprite, font, new Color(0.55f, 0.45f, 0.15f));
      var bAddG1k = CreateActionButton("BtnAddGold1k", pRpg.transform, "💰 Add +1,000 Gold", "Add gold currency to pouch", buttonSprite, font);
      var bAddG10k = CreateActionButton("BtnAddGold10k", pRpg.transform, "💰 Add +10,000 Gold", "Add large gold amount for upgrades", buttonSprite, font);
      var bAddExp = CreateActionButton("BtnAddExp", pRpg.transform, "📈 Add +500 EXP (Level Up)", "Test EXP ring progression and level badge", buttonSprite, font);
      var bAddPts = CreateActionButton("BtnAddPoints", pRpg.transform, "✨ Add +10 Attribute Points", "Gain stat points for ATK/HP/Stamina", buttonSprite, font);
      var bAddPot = CreateActionButton("BtnAddPotions", pRpg.transform, "🧪 Add 5 Health Potions", "Add consumable healing potions to bag", buttonSprite, font);
      var bAddSwd = CreateActionButton("BtnAddSword", pRpg.transform, "🔥 Add Dragonic Sword (Legendary)", "Obtain mythical fire weapon", buttonSprite, font, new Color(0.6f, 0.2f, 0.15f));

      so.FindProperty("btnAddBossKey").objectReferenceValue = bAddKey.GetComponent<Button>();
      so.FindProperty("btnAddGold1k").objectReferenceValue = bAddG1k.GetComponent<Button>();
      so.FindProperty("btnAddGold10k").objectReferenceValue = bAddG10k.GetComponent<Button>();
      so.FindProperty("btnAddExp500").objectReferenceValue = bAddExp.GetComponent<Button>();
      so.FindProperty("btnAddPoints10").objectReferenceValue = bAddPts.GetComponent<Button>();
      so.FindProperty("btnAddPotions").objectReferenceValue = bAddPot.GetComponent<Button>();
      so.FindProperty("btnAddDragonicSword").objectReferenceValue = bAddSwd.GetComponent<Button>();

      // 4. Toast Notification Bar
      GameObject toastObj = new GameObject("ToastBar");
      toastObj.transform.SetParent(panelObj.transform, false);
      RectTransform toastRect = toastObj.AddComponent<RectTransform>();
      toastRect.anchorMin = new Vector2(0f, 0f);
      toastRect.anchorMax = new Vector2(1f, 0f);
      toastRect.pivot = new Vector2(0.5f, 0f);
      toastRect.anchoredPosition = new Vector2(0f, 10f);
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
      vlg.spacing = 6f;
      vlg.childControlWidth = true;
      vlg.childControlHeight = false;

      return obj;
    }

    private static GameObject CreateActionButton(string name, Transform parent, string title, string subtitle, Sprite btnSprite, TMP_FontAsset font, Color? bgColor = null)
    {
      GameObject btnObj = new GameObject(name);
      btnObj.transform.SetParent(parent, false);
      RectTransform rt = btnObj.AddComponent<RectTransform>();
      rt.sizeDelta = new Vector2(0f, 44f);

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
