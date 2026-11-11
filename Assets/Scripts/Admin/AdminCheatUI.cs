using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using InfiniteLabyrinth.Inventory;

/// <summary>
/// Controls the Admin / Debug Cheat Console UI.
/// Features:
/// 1. Toggled smoothly via hotkey ~ (or F1/F2), and closed via Esc or [X] button.
/// 2. 5 Functional Tabs: Teleport (Dynamic Scene), World (Scene Loop), Player (Combat & Stats), Monsters (Dummies & AI), Creative Mode (All 51 Items).
/// 3. Tab buttons maintain crisp WHITE text when selected for high visibility.
/// 4. Mini Debug Overlay (FPS, Player Position, Current Room, Active Badges) toggles synchronously with the panel.
/// 5. Clean Toast Notification feed for real-time action feedback.
/// </summary>
public class AdminCheatUI : MonoBehaviour
{
  public static AdminCheatUI Instance { get; private set; }

  public enum CheatTab { Teleport, World, Player, Combat, Creative }

  [Header("Root Canvas Structure")]
  [SerializeField] private CanvasGroup rootCanvasGroup;
  [SerializeField] private CanvasGroup mainPanelCanvasGroup;
  [SerializeField] private GameObject mainPanelRoot;
  [SerializeField] private Button btnClose;
  [SerializeField] private TextMeshProUGUI txtSceneIndicator;

  [Header("Navigation Tabs")]
  [SerializeField] private Button btnTabTeleport;
  [SerializeField] private Button btnTabWorld;
  [SerializeField] private Button btnTabPlayer;
  [SerializeField] private Button btnTabCombat;
  [SerializeField] private Button btnTabCreative;

  [Header("Tab Content Panels")]
  [SerializeField] private GameObject panelTeleport;
  [SerializeField] private GameObject panelWorld;
  [SerializeField] private GameObject panelPlayer;
  [SerializeField] private GameObject panelCombat;
  [SerializeField] private GameObject panelCreative;

  [Header("Tab 1: Teleport - Sub Containers")]
  [SerializeField] private GameObject landTeleportContainer;
  [SerializeField] private GameObject dungeonTeleportContainer;

  [Header("Buttons - Tab 1: LandScene Teleports")]
  [SerializeField] private Button btnTeleVillageBell;
  [SerializeField] private Button btnTeleVillagePortal;
  [SerializeField] private Button btnTeleEquipShop;
  [SerializeField] private Button btnTeleSuppliesShop;
  [SerializeField] private Button btnTeleBlacksmith;
  [SerializeField] private Button btnTeleWindmill;
  [SerializeField] private Button btnFastTravelDungeon1;

  [Header("Buttons - Tab 1: DungeonScene Teleports")]
  [SerializeField] private Button btnTeleDungeonStart;
  [SerializeField] private Button btnTeleDungeonBoss;
  [SerializeField] private Button btnTeleDungeonArena;
  [SerializeField] private Button btnTeleDungeonKey;
  [SerializeField] private Button btnTeleDungeonTreasure;
  [SerializeField] private Button btnTeleDungeonMezzanine;
  [SerializeField] private Button btnFastTravelVillage1;

  [Header("Tab 2: World - Sub Containers")]
  [SerializeField] private GameObject landWorldContainer;
  [SerializeField] private GameObject dungeonWorldContainer;

  [Header("Buttons - Tab 2: LandScene World")]
  [SerializeField] private Button btnAdvanceDay;
  [SerializeField] private Button btnResetDay;
  [SerializeField] private Button btnFastTravelDungeon2;

  [Header("Buttons - Tab 2: DungeonScene World")]
  [SerializeField] private Button btnRegenerateDungeon;
  [SerializeField] private Button btnRevealMap;
  [SerializeField] private Button btnUnlockBossGate;
  [SerializeField] private Button btnKillRadius;
  [SerializeField] private Button btnKillAll;
  [SerializeField] private Button btnFastTravelVillage2;

  [Header("Buttons - Tab 3: Player")]
  [SerializeField] private Button btnToggleGodMode;
  [SerializeField] private TextMeshProUGUI txtGodModeStatus;
  [SerializeField] private Button btnToggleInfStamina;
  [SerializeField] private TextMeshProUGUI txtInfStaminaStatus;
  [SerializeField] private Button btnToggleInfSpirit;
  [SerializeField] private TextMeshProUGUI txtInfSpiritStatus;
  [SerializeField] private Button btnToggleNoCooldown;
  [SerializeField] private TextMeshProUGUI txtNoCooldownStatus;
  [SerializeField] private Button btnFullRestore;
  [SerializeField] private Button btnSuperSpeed;
  [SerializeField] private TextMeshProUGUI txtSuperSpeed;
  [SerializeField] private Button btnAddGold1k;
  [SerializeField] private Button btnAddGold10k;
  [SerializeField] private Button btnAddExp500;
  [SerializeField] private Button btnAddPoints10;

  [Header("Buttons - Tab 4: Monsters & Dummies")]
  [SerializeField] private Button btnSpawnDummy;
  [SerializeField] private Button btnHealDummies;
  [SerializeField] private Button btnClearDummies;
  [SerializeField] private Button btnToggleFreezeAI;
  [SerializeField] private TextMeshProUGUI txtFreezeAIStatus;
  [SerializeField] private Button btnSpawnWarrior;
  [SerializeField] private Button btnSpawnMinion;
  [SerializeField] private Button btnSpawnRogue;
  [SerializeField] private Button btnSpawnMage;

  [Header("Tab 5: Creative Mode (All 51 Game Items)")]
  [SerializeField] private TMP_InputField searchField;
  [SerializeField] private Button btnFilterAll;
  [SerializeField] private Button btnFilterWeapon;
  [SerializeField] private Button btnFilterArmor;
  [SerializeField] private Button btnFilterConsumable;
  [SerializeField] private Button btnFilterMaterial;
  [SerializeField] private Button btnFilterMisc;
  [SerializeField] private Transform creativeItemsScrollContent;
  [SerializeField] private GameObject creativeItemRowTemplate;

  [Header("Toast Notification")]
  [SerializeField] private TextMeshProUGUI toastText;
  [SerializeField] private CanvasGroup toastCanvasGroup;

  [Header("Mini Debug Overlay (Top-Right Screen)")]
  [SerializeField] private GameObject debugOverlayRoot;
  [SerializeField] private TextMeshProUGUI txtFps;
  [SerializeField] private TextMeshProUGUI txtPlayerPos;
  [SerializeField] private TextMeshProUGUI txtCurrentRoom;
  [SerializeField] private TextMeshProUGUI txtActiveCheatsBadge;

  private CheatTab currentTab = CheatTab.Teleport;
  private Coroutine toastCoroutine;

  // Creative Mode Items cache
  private class CreativeItemEntry
  {
    public GameObject rowObj;
    public ItemData item;
  }
  private readonly List<CreativeItemEntry> creativeEntries = new List<CreativeItemEntry>();
  private ItemType? currentFilterType = null;
  private string currentSearchKeyword = "";

  // FPS calculation
  private float fpsTimer = 0f;
  private int frameCount = 0;
  private float currentFps = 60f;

  private void Awake()
  {
    Instance = this;
    if (rootCanvasGroup == null)
    {
      rootCanvasGroup = GetComponent<CanvasGroup>();
    }
    AdminCheatManager.EnsureInstance();
  }

  private void Start()
  {
    var mgr = AdminCheatManager.EnsureInstance();
    SetupListeners();
    BuildCreativeItemCards();
    SwitchTab(CheatTab.Teleport);
    UpdateAllCheatButtonsVisual();

    if (mgr != null)
    {
      mgr.OnMenuToggled += HandleMenuToggled;
      mgr.OnCheatExecuted += ShowToast;
    }

    SetPanelVisible(false);
  }

  private void OnDestroy()
  {
    if (AdminCheatManager.Instance != null)
    {
      AdminCheatManager.Instance.OnMenuToggled -= HandleMenuToggled;
      AdminCheatManager.Instance.OnCheatExecuted -= ShowToast;
    }
  }

  private void Update()
  {
    if (Keyboard.current != null)
    {
      if (Keyboard.current.backquoteKey.wasPressedThisFrame ||
          Keyboard.current.f1Key.wasPressedThisFrame ||
          Keyboard.current.f2Key.wasPressedThisFrame)
      {
        var mgr = AdminCheatManager.EnsureInstance();
        if (mgr != null) mgr.ToggleMenu();
      }
      else if (AdminCheatManager.IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        var mgr = AdminCheatManager.EnsureInstance();
        if (mgr != null) mgr.CloseMenu();
      }
    }

    if (debugOverlayRoot != null && debugOverlayRoot.activeInHierarchy)
    {
      UpdateDebugOverlay();
    }
  }

  #region Button Wiring & Event Listeners

  private void SetupListeners()
  {
    if (btnClose != null) btnClose.onClick.AddListener(() => AdminCheatManager.Instance?.CloseMenu());

    // Navigation Tabs
    if (btnTabTeleport != null) btnTabTeleport.onClick.AddListener(() => SwitchTab(CheatTab.Teleport));
    if (btnTabWorld != null) btnTabWorld.onClick.AddListener(() => SwitchTab(CheatTab.World));
    if (btnTabPlayer != null) btnTabPlayer.onClick.AddListener(() => SwitchTab(CheatTab.Player));
    if (btnTabCombat != null) btnTabCombat.onClick.AddListener(() => SwitchTab(CheatTab.Combat));
    if (btnTabCreative != null) btnTabCreative.onClick.AddListener(() => SwitchTab(CheatTab.Creative));

    // Tab 1: LandScene Teleports
    if (btnTeleVillageBell != null) btnTeleVillageBell.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToVillageBell());
    if (btnTeleVillagePortal != null) btnTeleVillagePortal.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToVillagePortal());
    if (btnTeleEquipShop != null) btnTeleEquipShop.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToEquipmentShop());
    if (btnTeleSuppliesShop != null) btnTeleSuppliesShop.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToSuppliesShop());
    if (btnTeleBlacksmith != null) btnTeleBlacksmith.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToBlacksmith());
    if (btnTeleWindmill != null) btnTeleWindmill.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToWindmill());
    if (btnFastTravelDungeon1 != null) btnFastTravelDungeon1.onClick.AddListener(() => AdminCheatManager.Instance?.FastTravelToDungeon());

    // Tab 1: DungeonScene Teleports
    if (btnTeleDungeonStart != null) btnTeleDungeonStart.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToStartRoom());
    if (btnTeleDungeonBoss != null) btnTeleDungeonBoss.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToBossRoom());
    if (btnTeleDungeonArena != null) btnTeleDungeonArena.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportInsideBossArena());
    if (btnTeleDungeonKey != null) btnTeleDungeonKey.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToKeyRoom());
    if (btnTeleDungeonTreasure != null) btnTeleDungeonTreasure.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToTreasureRoom());
    if (btnTeleDungeonMezzanine != null) btnTeleDungeonMezzanine.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToMezzanine());
    if (btnFastTravelVillage1 != null) btnFastTravelVillage1.onClick.AddListener(() => AdminCheatManager.Instance?.FastTravelToVillage());

    // Tab 2: LandScene World
    if (btnAdvanceDay != null) btnAdvanceDay.onClick.AddListener(() => AdminCheatManager.Instance?.AdvanceDay());
    if (btnResetDay != null) btnResetDay.onClick.AddListener(() => AdminCheatManager.Instance?.ResetDayTo1());
    if (btnFastTravelDungeon2 != null) btnFastTravelDungeon2.onClick.AddListener(() => AdminCheatManager.Instance?.FastTravelToDungeon());

    // Tab 2: DungeonScene World
    if (btnRegenerateDungeon != null) btnRegenerateDungeon.onClick.AddListener(() => AdminCheatManager.Instance?.RegenerateDungeon());
    if (btnRevealMap != null) btnRevealMap.onClick.AddListener(() => AdminCheatManager.Instance?.RevealDungeonMap());
    if (btnUnlockBossGate != null) btnUnlockBossGate.onClick.AddListener(() => AdminCheatManager.Instance?.UnlockBossGate());
    if (btnKillRadius != null) btnKillRadius.onClick.AddListener(() => AdminCheatManager.Instance?.KillEnemiesInRadius(25f));
    if (btnKillAll != null) btnKillAll.onClick.AddListener(() => AdminCheatManager.Instance?.KillAllEnemies());
    if (btnFastTravelVillage2 != null) btnFastTravelVillage2.onClick.AddListener(() => AdminCheatManager.Instance?.FastTravelToVillage());

    // Tab 3: Player
    if (btnToggleGodMode != null) btnToggleGodMode.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleGodMode();
      UpdateAllCheatButtonsVisual();
    });

    if (btnToggleInfStamina != null) btnToggleInfStamina.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleInfiniteStamina();
      UpdateAllCheatButtonsVisual();
    });

    if (btnToggleInfSpirit != null) btnToggleInfSpirit.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleInfiniteSpirit();
      UpdateAllCheatButtonsVisual();
    });

    if (btnToggleNoCooldown != null) btnToggleNoCooldown.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleNoCooldown();
      UpdateAllCheatButtonsVisual();
    });

    if (btnFullRestore != null) btnFullRestore.onClick.AddListener(() => AdminCheatManager.Instance?.FullHealAndRestore());

    if (btnSuperSpeed != null) btnSuperSpeed.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.CycleSuperSpeed();
      UpdateAllCheatButtonsVisual();
    });

    if (btnAddGold1k != null) btnAddGold1k.onClick.AddListener(() => AdminCheatManager.Instance?.AddGold(1000));
    if (btnAddGold10k != null) btnAddGold10k.onClick.AddListener(() => AdminCheatManager.Instance?.AddGold(10000));
    if (btnAddExp500 != null) btnAddExp500.onClick.AddListener(() => AdminCheatManager.Instance?.AddExp(500));
    if (btnAddPoints10 != null) btnAddPoints10.onClick.AddListener(() => AdminCheatManager.Instance?.AddAttributePoints(10));

    // Tab 4: Monsters & Dummies
    if (btnSpawnDummy != null) btnSpawnDummy.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnDamageDummy(10000f));
    if (btnHealDummies != null) btnHealDummies.onClick.AddListener(() => AdminCheatManager.Instance?.HealAllDummies(10000f));
    if (btnClearDummies != null) btnClearDummies.onClick.AddListener(() => AdminCheatManager.Instance?.ClearAllDummies());

    if (btnToggleFreezeAI != null) btnToggleFreezeAI.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleFreezeEnemies();
      UpdateAllCheatButtonsVisual();
    });

    if (btnSpawnWarrior != null) btnSpawnWarrior.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Warrior));
    if (btnSpawnMinion != null) btnSpawnMinion.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Minion));
    if (btnSpawnRogue != null) btnSpawnRogue.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Rogue));
    if (btnSpawnMage != null) btnSpawnMage.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Mage));

    // Tab 5: Creative Mode Filters
    if (searchField != null)
    {
      searchField.onValueChanged.AddListener((val) =>
      {
        currentSearchKeyword = val.Trim().ToLower();
        FilterCreativeItems();
      });
    }

    if (btnFilterAll != null) btnFilterAll.onClick.AddListener(() => SetCreativeCategory(null));
    if (btnFilterWeapon != null) btnFilterWeapon.onClick.AddListener(() => SetCreativeCategory(ItemType.Weapon));
    if (btnFilterArmor != null) btnFilterArmor.onClick.AddListener(() => SetCreativeCategory(ItemType.Armor));
    if (btnFilterConsumable != null) btnFilterConsumable.onClick.AddListener(() => SetCreativeCategory(ItemType.Consumable));
    if (btnFilterMaterial != null) btnFilterMaterial.onClick.AddListener(() => SetCreativeCategory(ItemType.Material));
    if (btnFilterMisc != null) btnFilterMisc.onClick.AddListener(() => SetCreativeCategory(ItemType.Key));
  }

  #endregion

  #region Tab Switching & Scene Layout Sync

  public void SwitchTab(CheatTab tab)
  {
    currentTab = tab;

    if (panelTeleport != null) panelTeleport.SetActive(tab == CheatTab.Teleport);
    if (panelWorld != null) panelWorld.SetActive(tab == CheatTab.World);
    if (panelPlayer != null) panelPlayer.SetActive(tab == CheatTab.Player);
    if (panelCombat != null) panelCombat.SetActive(tab == CheatTab.Combat);
    if (panelCreative != null) panelCreative.SetActive(tab == CheatTab.Creative);

    UpdateTabButtonStyles();
    UpdateSceneContainers();
  }

  private void UpdateSceneContainers()
  {
    bool isLand = (AdminCheatManager.Instance != null && AdminCheatManager.Instance.IsLandScene);

    if (txtSceneIndicator != null)
    {
      txtSceneIndicator.text = isLand
        ? "Scene: <color=#81C784>Haven Village Hub (LandScene)</color>"
        : "Scene: <color=#64B5F6>Procedural Dungeon (DungeonScene)</color>";
    }

    if (landTeleportContainer != null) landTeleportContainer.SetActive(isLand);
    if (dungeonTeleportContainer != null) dungeonTeleportContainer.SetActive(!isLand);

    if (landWorldContainer != null) landWorldContainer.SetActive(isLand);
    if (dungeonWorldContainer != null) dungeonWorldContainer.SetActive(!isLand);
  }

  private void UpdateTabButtonStyles()
  {
    SetTabButtonColor(btnTabTeleport, currentTab == CheatTab.Teleport);
    SetTabButtonColor(btnTabWorld, currentTab == CheatTab.World);
    SetTabButtonColor(btnTabPlayer, currentTab == CheatTab.Player);
    SetTabButtonColor(btnTabCombat, currentTab == CheatTab.Combat);
    SetTabButtonColor(btnTabCreative, currentTab == CheatTab.Creative);
  }

  private void SetTabButtonColor(Button btn, bool isSelected)
  {
    if (btn == null) return;

    Image img = btn.GetComponent<Image>();
    if (img != null)
    {
      img.color = isSelected ? new Color(0.85f, 0.55f, 0.15f, 1.0f) : new Color(0.20f, 0.20f, 0.25f, 0.9f);
    }

    TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
    if (txt != null)
    {
      txt.color = Color.white;
      txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
    }
  }

  public void UpdateAllCheatButtonsVisual()
  {
    // 1. God Mode
    if (txtGodModeStatus != null)
    {
      bool god = AdminCheatManager.IsGodMode;
      txtGodModeStatus.text = god ? "<color=#00FF88><b>[ON]</b></color>" : "<color=#888888>[OFF]</color>";
    }

    // 2. Inf Stamina
    if (txtInfStaminaStatus != null)
    {
      bool stam = AdminCheatManager.IsInfiniteStamina;
      txtInfStaminaStatus.text = stam ? "<color=#00FF88><b>[ON]</b></color>" : "<color=#888888>[OFF]</color>";
    }

    // 3. Inf Spirit
    if (txtInfSpiritStatus != null)
    {
      bool spirit = AdminCheatManager.IsInfiniteSpirit;
      txtInfSpiritStatus.text = spirit ? "<color=#00FF88><b>[ON]</b></color>" : "<color=#888888>[OFF]</color>";
    }

    // 4. No Cooldown
    if (txtNoCooldownStatus != null)
    {
      bool noCd = AdminCheatManager.IsNoCooldown;
      txtNoCooldownStatus.text = noCd ? "<color=#00FF88><b>[ON]</b></color>" : "<color=#888888>[OFF]</color>";
    }

    // 5. Freeze AI
    if (txtFreezeAIStatus != null)
    {
      bool freeze = AdminCheatManager.IsEnemyAIFrozen;
      txtFreezeAIStatus.text = freeze ? "<color=#4FC3F7><b>[FROZEN]</b></color>" : "<color=#888888>[ACTIVE]</color>";
    }

    // 6. Speed Multiplier
    if (txtSuperSpeed != null && AdminCheatManager.Instance != null)
    {
      float spd = AdminCheatManager.Instance.GetCurrentSpeedMultiplier();
      txtSuperSpeed.text = $"Move Speed: <color=#FFE082><b>{spd:F1}x</b></color>";
    }
  }

  #endregion

  #region Creative Mode Item List & Filtering

  private void BuildCreativeItemCards()
  {
    if (creativeItemsScrollContent == null || creativeItemRowTemplate == null) return;

    creativeItemRowTemplate.SetActive(false);
    creativeEntries.Clear();

    var mgr = AdminCheatManager.EnsureInstance();
    if (mgr == null) return;
    mgr.EnsureItemAndEnemyPrefabs();

    var items = mgr.allGameItems;
    if (items == null || items.Count == 0) return;

    for (int i = 0; i < items.Count; i++)
    {
      ItemData item = items[i];
      if (item == null) continue;

      GameObject row = Instantiate(creativeItemRowTemplate, creativeItemsScrollContent);
      row.name = $"ItemRow_{item.Id}";
      row.SetActive(true);

      // Icon & Border
      Transform iconTf = row.transform.Find("Icon");
      if (iconTf != null)
      {
        Image iconImg = iconTf.GetComponent<Image>();
        if (iconImg != null)
        {
          iconImg.sprite = item.Icon;
          iconImg.color = Color.white;
        }
      }

      Transform rarityBorderTf = row.transform.Find("RarityBorder");
      if (rarityBorderTf != null)
      {
        Image rbImg = rarityBorderTf.GetComponent<Image>();
        if (rbImg != null) rbImg.color = item.Rarity.GetRarityColor();
      }

      // Title & Subtitle
      Transform titleTf = row.transform.Find("Title");
      if (titleTf != null)
      {
        TextMeshProUGUI titleTxt = titleTf.GetComponent<TextMeshProUGUI>();
        if (titleTxt != null)
        {
          string hex = ColorUtility.ToHtmlStringRGB(item.Rarity.GetRarityColor());
          titleTxt.text = $"<color=#{hex}><b>{item.ItemName}</b></color>";
        }
      }

      Transform subTf = row.transform.Find("Subtitle");
      if (subTf != null)
      {
        TextMeshProUGUI subTxt = subTf.GetComponent<TextMeshProUGUI>();
        if (subTxt != null)
        {
          subTxt.text = FormatItemSubtitle(item);
        }
      }

      // Action Buttons (+1 & +Max)
      Transform btnOneTf = row.transform.Find("BtnPlusOne");
      if (btnOneTf != null)
      {
        Button b1 = btnOneTf.GetComponent<Button>();
        if (b1 != null) b1.onClick.AddListener(() => AdminCheatManager.Instance?.GiveItem(item, 1));
      }

      Transform btnMaxTf = row.transform.Find("BtnPlusMax");
      if (btnMaxTf != null)
      {
        Button bMax = btnMaxTf.GetComponent<Button>();
        if (bMax != null) bMax.onClick.AddListener(() => AdminCheatManager.Instance?.GiveItem(item, item.MaxStack));
      }

      // Clicking row directly also adds 1 item
      Button rowBtn = row.GetComponent<Button>();
      if (rowBtn != null)
      {
        rowBtn.onClick.AddListener(() => AdminCheatManager.Instance?.GiveItem(item, 1));
      }

      creativeEntries.Add(new CreativeItemEntry { rowObj = row, item = item });
    }
  }

  private string FormatItemSubtitle(ItemData item)
  {
    if (item.ItemType == ItemType.Weapon)
      return $"Weapon | <color=#FF8A65>+{item.BonusAttack:F0} ATK</color>";
    if (item.ItemType == ItemType.Armor)
      return $"Armor | <color=#81D4FA>+{item.BonusDefense:F0} DEF</color>";
    if (item.ItemType == ItemType.Consumable)
      return (item.HealAmount > 0f) ? $"Potion | <color=#A5D6A7>Heals {item.HealAmount:F0} HP</color>" : "Consumable Buff";
    if (item.ItemType == ItemType.Material)
      return $"Material | Max Stack: {item.MaxStack}";
    if (item.ItemType == ItemType.Key)
      return "Quest Key / Dungeon Pass";
    if (item.ItemType == ItemType.Currency)
      return $"Currency | Value: {item.GoldValue} Gold";
    return $"{item.ItemType} | {item.Rarity}";
  }

  private void SetCreativeCategory(ItemType? type)
  {
    currentFilterType = type;
    FilterCreativeItems();
  }

  private void FilterCreativeItems()
  {
    for (int i = 0; i < creativeEntries.Count; i++)
    {
      var entry = creativeEntries[i];
      if (entry == null || entry.rowObj == null || entry.item == null) continue;

      bool matchCategory = true;
      if (currentFilterType.HasValue)
      {
        if (currentFilterType.Value == ItemType.Key)
        {
          matchCategory = (entry.item.ItemType == ItemType.Key || entry.item.ItemType == ItemType.Currency);
        }
        else
        {
          matchCategory = (entry.item.ItemType == currentFilterType.Value);
        }
      }

      bool matchSearch = true;
      if (!string.IsNullOrEmpty(currentSearchKeyword))
      {
        string name = entry.item.ItemName.ToLower();
        string id = entry.item.Id.ToLower();
        matchSearch = name.Contains(currentSearchKeyword) || id.Contains(currentSearchKeyword);
      }

      entry.rowObj.SetActive(matchCategory && matchSearch);
    }
  }

  #endregion

  #region Open/Close Sync & Toast Feed

  private void HandleMenuToggled(bool isOpen)
  {
    SetPanelVisible(isOpen);
    if (isOpen)
    {
      UpdateSceneContainers();
      UpdateAllCheatButtonsVisual();
    }
  }

  private void SetPanelVisible(bool visible)
  {
    if (rootCanvasGroup == null)
    {
      rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    if (rootCanvasGroup != null)
    {
      rootCanvasGroup.alpha = visible ? 1f : 0f;
      rootCanvasGroup.interactable = visible;
      rootCanvasGroup.blocksRaycasts = visible;
    }

    if (mainPanelCanvasGroup != null && mainPanelCanvasGroup != rootCanvasGroup)
    {
      mainPanelCanvasGroup.alpha = visible ? 1f : 0f;
      mainPanelCanvasGroup.interactable = visible;
      mainPanelCanvasGroup.blocksRaycasts = visible;
    }
    else if (mainPanelRoot != null && mainPanelCanvasGroup == null)
    {
      mainPanelRoot.SetActive(visible);
    }

    if (debugOverlayRoot != null)
    {
      debugOverlayRoot.SetActive(visible);
    }
  }

  public void ShowToast(string message)
  {
    if (toastText == null) return;

    toastText.text = message;

    if (toastCoroutine != null)
    {
      StopCoroutine(toastCoroutine);
    }
    toastCoroutine = StartCoroutine(ToastRoutine());
  }

  private IEnumerator ToastRoutine()
  {
    if (toastCanvasGroup != null)
    {
      toastCanvasGroup.alpha = 1f;
      yield return new WaitForSecondsRealtime(2.8f);

      float t = 1f;
      while (t > 0f)
      {
        t -= Time.unscaledDeltaTime * 2.5f;
        toastCanvasGroup.alpha = Mathf.Clamp01(t);
        yield return null;
      }
      toastCanvasGroup.alpha = 0f;
    }
    else
    {
      toastText.gameObject.SetActive(true);
      yield return new WaitForSecondsRealtime(2.8f);
      toastText.gameObject.SetActive(false);
    }
  }

  #endregion

  #region Mini Debug Overlay (FPS, Coordinates, Room Info)

  private void UpdateDebugOverlay()
  {
    // 1. Calculate FPS
    fpsTimer += Time.unscaledDeltaTime;
    frameCount++;
    if (fpsTimer >= 0.5f)
    {
      currentFps = frameCount / fpsTimer;
      fpsTimer = 0f;
      frameCount = 0;

      if (txtFps != null)
      {
        Color fpsColor = (currentFps >= 60f) ? new Color(0.2f, 1f, 0.4f) : ((currentFps >= 30f) ? Color.yellow : Color.red);
        string colHex = ColorUtility.ToHtmlStringRGB(fpsColor);
        txtFps.text = $"FPS: <color=#{colHex}><b>{Mathf.RoundToInt(currentFps)}</b></color>";
      }
    }

    // 2. Player Coordinates & Current Room
    var player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      Vector3 pos = player.transform.position;
      if (txtPlayerPos != null)
      {
        txtPlayerPos.text = $"Position: <color=#81D4FA>({pos.x:F1}, {pos.y:F1}, {pos.z:F1})</color>";
      }

      if (txtCurrentRoom != null)
      {
        if (DungeonManager.Instance != null && DungeonManager.Instance.currentData != null)
        {
          float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;
          int gridX = Mathf.FloorToInt(pos.x / tSize);
          int gridZ = Mathf.FloorToInt(pos.z / tSize);
          RoomData room = DungeonManager.Instance.currentData.GetRoomAt(gridX, gridZ);

          if (room != null)
          {
            txtCurrentRoom.text = $"Room: <color=#FFE082>#{room.id} ({room.type})</color>";
          }
          else
          {
            txtCurrentRoom.text = "Room: <color=#888888>Corridor / Entrance</color>";
          }
        }
        else
        {
          txtCurrentRoom.text = "Location: <color=#81C784>Haven Village Hub (Peaceful)</color>";
        }
      }
    }

    // 3. Active Cheat Badges
    if (txtActiveCheatsBadge != null)
    {
      List<string> badges = new List<string>();
      if (AdminCheatManager.IsGodMode) badges.Add("<color=#00FF88>[GOD]</color>");
      if (AdminCheatManager.IsInfiniteStamina) badges.Add("<color=#FFD54F>[INF STAM]</color>");
      if (AdminCheatManager.IsInfiniteSpirit) badges.Add("<color=#4DD0E1>[INF SPIRIT]</color>");
      if (AdminCheatManager.IsNoCooldown) badges.Add("<color=#FF80AB>[NO CD]</color>");
      if (AdminCheatManager.IsEnemyAIFrozen) badges.Add("<color=#4FC3F7>[FROZEN AI]</color>");

      float spd = (AdminCheatManager.Instance != null) ? AdminCheatManager.Instance.GetCurrentSpeedMultiplier() : 1f;
      if (spd > 1.05f) badges.Add($"<color=#FF8A65>[SPD {spd:F1}x]</color>");

      txtActiveCheatsBadge.text = badges.Count > 0 ? string.Join(" ", badges) : "";
    }
  }

  #endregion
}
