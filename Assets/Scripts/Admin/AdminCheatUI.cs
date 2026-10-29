using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Admin / Debug Cheat Console UI.
/// Features:
/// 1. Toggled smoothly via hotkey ~ (or F1), and closed via Esc or [✕] button.
/// 2. 5 Functional Tabs: Teleport, Dungeon, Player, Monsters, RPG & Loot.
/// 3. Tab buttons maintain crisp WHITE text when selected for high visibility.
/// 4. Mini Debug Overlay (FPS, Player Position, Current Room, Active Badges) toggles synchronously with the panel.
/// 5. Clean Toast Notification feed for real-time action feedback.
/// </summary>
public class AdminCheatUI : MonoBehaviour
{
  public static AdminCheatUI Instance { get; private set; }

  public enum CheatTab { Teleport, Dungeon, Player, Combat, RPG }

  [Header("Root Canvas Structure")]
  [SerializeField] private CanvasGroup mainPanelCanvasGroup;
  [SerializeField] private GameObject mainPanelRoot;
  [SerializeField] private Button btnClose;

  [Header("Navigation Tabs")]
  [SerializeField] private Button btnTabTeleport;
  [SerializeField] private Button btnTabDungeon;
  [SerializeField] private Button btnTabPlayer;
  [SerializeField] private Button btnTabCombat;
  [SerializeField] private Button btnTabRPG;

  [Header("Tab Content Panels")]
  [SerializeField] private GameObject panelTeleport;
  [SerializeField] private GameObject panelDungeon;
  [SerializeField] private GameObject panelPlayer;
  [SerializeField] private GameObject panelCombat;
  [SerializeField] private GameObject panelRPG;

  [Header("Buttons - Tab 1: Teleport")]
  [SerializeField] private Button btnTeleportStart;
  [SerializeField] private Button btnTeleportBoss;
  [SerializeField] private Button btnTeleportKey;
  [SerializeField] private Button btnTeleportTreasure;
  [SerializeField] private Button btnTeleportMezzanine;

  [Header("Buttons - Tab 2: Dungeon")]
  [SerializeField] private Button btnRegenerateDungeon;
  [SerializeField] private Button btnKillRadius;
  [SerializeField] private Button btnKillAll;

  [Header("Buttons - Tab 3: Player")]
  [SerializeField] private Button btnToggleGodMode;
  [SerializeField] private TextMeshProUGUI txtGodModeStatus;
  [SerializeField] private Button btnToggleInfStamina;
  [SerializeField] private TextMeshProUGUI txtInfStaminaStatus;
  [SerializeField] private Button btnToggleInfSpirit;
  [SerializeField] private TextMeshProUGUI txtInfSpiritStatus;
  [SerializeField] private Button btnFullRestore;
  [SerializeField] private Button btnSuperSpeed;
  [SerializeField] private TextMeshProUGUI txtSuperSpeed;

  [Header("Buttons - Tab 4: Monsters")]
  [SerializeField] private Button btnToggleFreezeAI;
  [SerializeField] private TextMeshProUGUI txtFreezeAIStatus;
  [SerializeField] private Button btnSpawnWarrior;
  [SerializeField] private Button btnSpawnMinion;
  [SerializeField] private Button btnSpawnRogue;
  [SerializeField] private Button btnSpawnMage;

  [Header("Buttons - Tab 5: RPG & Loot")]
  [SerializeField] private Button btnAddBossKey;
  [SerializeField] private Button btnAddGold1k;
  [SerializeField] private Button btnAddGold10k;
  [SerializeField] private Button btnAddExp500;
  [SerializeField] private Button btnAddPoints10;
  [SerializeField] private Button btnAddPotions;
  [SerializeField] private Button btnAddDragonicSword;

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

  // FPS calculation
  private float fpsTimer = 0f;
  private int frameCount = 0;
  private float currentFps = 60f;

  private void Awake()
  {
    Instance = this;
  }

  private void Start()
  {
    SetupListeners();
    SwitchTab(CheatTab.Teleport);
    UpdateAllCheatButtonsVisual();

    if (AdminCheatManager.Instance != null)
    {
      AdminCheatManager.Instance.OnMenuToggled += HandleMenuToggled;
      AdminCheatManager.Instance.OnCheatExecuted += ShowToast;
    }

    // Both Admin Panel and Debug Overlay are closed/hidden by default on start
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
    // Update Debug Overlay only when it is active/visible
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
    if (btnTabDungeon != null) btnTabDungeon.onClick.AddListener(() => SwitchTab(CheatTab.Dungeon));
    if (btnTabPlayer != null) btnTabPlayer.onClick.AddListener(() => SwitchTab(CheatTab.Player));
    if (btnTabCombat != null) btnTabCombat.onClick.AddListener(() => SwitchTab(CheatTab.Combat));
    if (btnTabRPG != null) btnTabRPG.onClick.AddListener(() => SwitchTab(CheatTab.RPG));

    // Tab 1: Teleport
    if (btnTeleportStart != null) btnTeleportStart.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToStartRoom());
    if (btnTeleportBoss != null) btnTeleportBoss.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToBossRoom());
    if (btnTeleportKey != null) btnTeleportKey.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToKeyRoom());
    if (btnTeleportTreasure != null) btnTeleportTreasure.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToTreasureRoom());
    if (btnTeleportMezzanine != null) btnTeleportMezzanine.onClick.AddListener(() => AdminCheatManager.Instance?.TeleportToMezzanine());

    // Tab 2: Dungeon
    if (btnRegenerateDungeon != null) btnRegenerateDungeon.onClick.AddListener(() => AdminCheatManager.Instance?.RegenerateDungeon());
    if (btnKillRadius != null) btnKillRadius.onClick.AddListener(() => AdminCheatManager.Instance?.KillEnemiesInRadius(25f));
    if (btnKillAll != null) btnKillAll.onClick.AddListener(() => AdminCheatManager.Instance?.KillAllEnemies());

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

    if (btnFullRestore != null) btnFullRestore.onClick.AddListener(() => AdminCheatManager.Instance?.FullHealAndRestore());

    if (btnSuperSpeed != null) btnSuperSpeed.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.CycleSuperSpeed();
      UpdateAllCheatButtonsVisual();
    });

    // Tab 4: Monsters
    if (btnToggleFreezeAI != null) btnToggleFreezeAI.onClick.AddListener(() =>
    {
      AdminCheatManager.Instance?.ToggleFreezeEnemies();
      UpdateAllCheatButtonsVisual();
    });

    if (btnSpawnWarrior != null) btnSpawnWarrior.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Warrior));
    if (btnSpawnMinion != null) btnSpawnMinion.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Minion));
    if (btnSpawnRogue != null) btnSpawnRogue.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Rogue));
    if (btnSpawnMage != null) btnSpawnMage.onClick.AddListener(() => AdminCheatManager.Instance?.SpawnTestEnemy(AdminCheatManager.TestEnemyType.Mage));

    // Tab 5: RPG
    if (btnAddBossKey != null) btnAddBossKey.onClick.AddListener(() => AdminCheatManager.Instance?.AddBossKey());
    if (btnAddGold1k != null) btnAddGold1k.onClick.AddListener(() => AdminCheatManager.Instance?.AddGold(1000));
    if (btnAddGold10k != null) btnAddGold10k.onClick.AddListener(() => AdminCheatManager.Instance?.AddGold(10000));
    if (btnAddExp500 != null) btnAddExp500.onClick.AddListener(() => AdminCheatManager.Instance?.AddExp(500));
    if (btnAddPoints10 != null) btnAddPoints10.onClick.AddListener(() => AdminCheatManager.Instance?.AddAttributePoints(10));
    if (btnAddPotions != null) btnAddPotions.onClick.AddListener(() => AdminCheatManager.Instance?.AddHealthPotions(5));
    if (btnAddDragonicSword != null) btnAddDragonicSword.onClick.AddListener(() => AdminCheatManager.Instance?.AddLegendarySword());
  }

  #endregion

  #region Tab Switching & Visual State

  public void SwitchTab(CheatTab tab)
  {
    currentTab = tab;

    if (panelTeleport != null) panelTeleport.SetActive(tab == CheatTab.Teleport);
    if (panelDungeon != null) panelDungeon.SetActive(tab == CheatTab.Dungeon);
    if (panelPlayer != null) panelPlayer.SetActive(tab == CheatTab.Player);
    if (panelCombat != null) panelCombat.SetActive(tab == CheatTab.Combat);
    if (panelRPG != null) panelRPG.SetActive(tab == CheatTab.RPG);

    UpdateTabButtonStyles();
  }

  private void UpdateTabButtonStyles()
  {
    SetTabButtonColor(btnTabTeleport, currentTab == CheatTab.Teleport);
    SetTabButtonColor(btnTabDungeon, currentTab == CheatTab.Dungeon);
    SetTabButtonColor(btnTabPlayer, currentTab == CheatTab.Player);
    SetTabButtonColor(btnTabCombat, currentTab == CheatTab.Combat);
    SetTabButtonColor(btnTabRPG, currentTab == CheatTab.RPG);
  }

  private void SetTabButtonColor(Button btn, bool isSelected)
  {
    if (btn == null) return;

    Image img = btn.GetComponent<Image>();
    if (img != null)
    {
      // Highlight with bright golden amber when selected, or dark metallic slate when unselected
      img.color = isSelected ? new Color(0.85f, 0.55f, 0.15f, 1.0f) : new Color(0.20f, 0.20f, 0.25f, 0.9f);
    }

    // Always maintain crisp WHITE text for high visibility (as requested)
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

    // 4. Freeze AI
    if (txtFreezeAIStatus != null)
    {
      bool freeze = AdminCheatManager.IsEnemyAIFrozen;
      txtFreezeAIStatus.text = freeze ? "<color=#4FC3F7><b>[FROZEN]</b></color>" : "<color=#888888>[ACTIVE]</color>";
    }

    // 5. Speed Multiplier
    if (txtSuperSpeed != null && AdminCheatManager.Instance != null)
    {
      float spd = AdminCheatManager.Instance.GetCurrentSpeedMultiplier();
      txtSuperSpeed.text = $"Move Speed: <color=#FFE082><b>{spd:F1}x</b></color>";
    }
  }

  #endregion

  #region Open/Close Sync & Toast Feed

  private void HandleMenuToggled(bool isOpen)
  {
    SetPanelVisible(isOpen);
    if (isOpen)
    {
      UpdateAllCheatButtonsVisual();
    }
  }

  /// <summary>
  /// Toggles both the Main Admin Panel and the Mini Debug Overlay synchronously.
  /// </summary>
  private void SetPanelVisible(bool visible)
  {
    // 1. Main Admin Panel
    if (mainPanelCanvasGroup != null)
    {
      mainPanelCanvasGroup.alpha = visible ? 1f : 0f;
      mainPanelCanvasGroup.interactable = visible;
      mainPanelCanvasGroup.blocksRaycasts = visible;
    }
    else if (mainPanelRoot != null)
    {
      mainPanelRoot.SetActive(visible);
    }

    // 2. Mini Debug Overlay (Opens and closes simultaneously with the panel)
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

      if (txtCurrentRoom != null && DungeonManager.Instance != null && DungeonManager.Instance.currentData != null)
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
    }

    // 3. Active Cheat Badges
    if (txtActiveCheatsBadge != null)
    {
      List<string> badges = new List<string>();
      if (AdminCheatManager.IsGodMode) badges.Add("<color=#00FF88>[GOD]</color>");
      if (AdminCheatManager.IsInfiniteStamina) badges.Add("<color=#FFD54F>[INF STAM]</color>");
      if (AdminCheatManager.IsInfiniteSpirit) badges.Add("<color=#4DD0E1>[INF SPIRIT]</color>");
      if (AdminCheatManager.IsEnemyAIFrozen) badges.Add("<color=#4FC3F7>[FROZEN AI]</color>");

      float spd = (AdminCheatManager.Instance != null) ? AdminCheatManager.Instance.GetCurrentSpeedMultiplier() : 1f;
      if (spd > 1.05f) badges.Add($"<color=#FF8A65>[SPD {spd:F1}x]</color>");

      txtActiveCheatsBadge.text = badges.Count > 0 ? string.Join(" ", badges) : "";
    }
  }

  #endregion
}
