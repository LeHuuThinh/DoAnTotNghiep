using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Blacksmith;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ giao diện Lò Rèn Haven (Blacksmith UI) 2 Panel:
  /// - Panel Trái: Lò Rèn Haven với 2 Tab [ CƯỜNG HÓA (UPGRADE) | CHẾ TẠO THẦN BINH (FORGE) ]
  /// - Panel Phải: Túi đồ người chơi & Thao tác chọn trang bị
  /// </summary>
  public class BlacksmithUIController : MonoBehaviour
  {
    public static BlacksmithUIController Instance { get; private set; }
    public static bool IsOpen { get; private set; } = false;

    public enum BlacksmithTab
    {
      Upgrade,
      Forge
    }

    public enum InventoryTab
    {
      All,
      Weapon,
      Armor,
      Item
    }

    [Header("=== GIAO DIỆN GỐC (ROOT CANVAS GROUP) ===")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Image darkOverlay;
    [SerializeField] private Button closeButton;

    [Header("=== CỬA SỔ TRÁI: LÒ RÈN (BLACKSMITH PANEL) ===")]
    [SerializeField] private RectTransform blacksmithPanel;
    [SerializeField] private TextMeshProUGUI blacksmithTitleText;
    [SerializeField] private TextMeshProUGUI blacksmithDescText;

    [Header("=== TAB BAR LÒ RÈN ===")]
    [SerializeField] private Button btnTabUpgrade;
    [SerializeField] private Button btnTabForge;
    [SerializeField] private GameObject tabUpgradeHighlight;
    [SerializeField] private GameObject tabForgeHighlight;
    [SerializeField] private GameObject upgradePanelObj;
    [SerializeField] private GameObject forgePanelObj;

    [Header("=== TAB 1: CƯỜNG HÓA (UPGRADE PANEL) ===")]
    [SerializeField] private Transform beforeWindow;
    [SerializeField] private Image beforeIcon;
    [SerializeField] private Image beforeRarityBg;
    [SerializeField] private Image beforeRarityBar;
    [SerializeField] private TextMeshProUGUI beforeItemName;
    [SerializeField] private TextMeshProUGUI beforeUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI beforeItemStats;

    [SerializeField] private GameObject arrowObj;

    [SerializeField] private Transform afterWindow;
    [SerializeField] private Image afterIcon;
    [SerializeField] private Image afterRarityBg;
    [SerializeField] private Image afterRarityBar;
    [SerializeField] private TextMeshProUGUI afterItemName;
    [SerializeField] private TextMeshProUGUI afterUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI afterItemStats;

    [SerializeField] private TextMeshProUGUI materialsNeededText;
    [SerializeField] private Transform upgradeMaterialsContainer;
    [SerializeField] private TextMeshProUGUI upgradeGoldPriceText;
    [SerializeField] private Image upgradeGoldIcon;
    [SerializeField] private Button btnUpgrade;
    [SerializeField] private TextMeshProUGUI btnUpgradeText;
    [SerializeField] private GameObject btnUpgradeBorder;
    [SerializeField] private GameObject btnUpgradeCorner;

    [Header("=== TAB 2: CHẾ TẠO THẦN BINH (FORGE PANEL) ===")]
    [SerializeField] private ScrollRect forgeScrollRect;
    [SerializeField] private Transform forgeContent;
    [SerializeField] private Image forgeSlotIcon;
    [SerializeField] private Image forgeSlotRarityBg;
    [SerializeField] private Image forgeSlotRarityBar;
    [SerializeField] private TextMeshProUGUI forgeItemName;
    [SerializeField] private TextMeshProUGUI forgeItemStats;
    [SerializeField] private Transform forgeMaterialsContainer;
    [SerializeField] private TextMeshProUGUI forgeGoldPriceText;
    [SerializeField] private Image forgeGoldIcon;
    [SerializeField] private Button btnForge;
    [SerializeField] private TextMeshProUGUI btnForgeText;
    [SerializeField] private GameObject btnForgeBorder;
    [SerializeField] private GameObject btnForgeCorner;

    [Header("=== CỬA SỔ PHẢI: TÚI ĐỒ (INVENTORY PANEL) ===")]
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private Transform inventoryGridContainer;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private List<InventorySlotUI> bagSlots = new List<InventorySlotUI>();

    [Header("=== CÁC NÚT TAB TÚI ĐỒ ===")]
    [SerializeField] private Button tabAllButton;
    [SerializeField] private Button tabWeaponButton;
    [SerializeField] private Button tabArmorButton;
    [SerializeField] private Button tabItemButton;
    [SerializeField] private GameObject tabAllHighlight;
    [SerializeField] private GameObject tabWeaponHighlight;
    [SerializeField] private GameObject tabArmorHighlight;
    [SerializeField] private GameObject tabItemHighlight;

    [Header("=== BẢNG POPUP CHI TIẾT KHI CLICK Ô ĐỒ (PUT IN) ===")]
    [SerializeField] private ItemDetailPopupUI itemDetailPopup;

    [Header("=== DANH SÁCH CÔNG THỨC RÈN (CRAFTING RECIPES) ===")]
    [SerializeField] private List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

    [Header("=== THỜI LƯỢNG MỞ / ĐÓNG ===")]
    [SerializeField] private float fadeDuration = 0.18f;

    // Trạng thái nội bộ
    private BlacksmithTab currentBlacksmithTab = BlacksmithTab.Upgrade;
    private InventoryTab currentInventoryTab = InventoryTab.All;
    private PlayerController cachedPlayerController;
    private Coroutine transitionRoutine;

    // Item đang chọn để nâng cấp
    private int selectedBagSlotIndex = -1;
    private ItemData selectedUpgradeItem = null;
    private int selectedUpgradeLevel = 0;

    // Recipe đang chọn để rèn
    private List<ForgeRecipeSlotUI> recipeSlots = new List<ForgeRecipeSlotUI>();
    private ForgeRecipeSlotUI selectedRecipeSlot = null;

    // Màu hiển thị giá tiền
    private readonly Color normalPriceColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700
    private readonly Color redPriceColor = new Color(1f, 0.28f, 0.28f, 1f);   // #FF4747
    private readonly Color greenColor = new Color(0.35f, 0.95f, 0.35f, 1f);  // #59F259

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      if (InventoryDragDropController.Instance == null)
      {
        var existing = FindFirstObjectByType<InventoryDragDropController>();
        if (existing == null) gameObject.AddComponent<InventoryDragDropController>();
      }

      AutoFindReferences();

      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.blocksRaycasts = false;
        rootCanvasGroup.interactable = false;
      }
      gameObject.SetActive(true);
      IsOpen = false;
    }

    private void Start()
    {
      SetupEvents();
      AutoFindSlots();
      SetupTabButtons();
      LoadAllRecipesIfEmpty();
    }

    private void Update()
    {
      if (!IsOpen) return;

      if (Keyboard.current != null)
      {
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
        {
          CloseBlacksmith();
        }
      }
    }

    public void AutoFindReferences()
    {
      if (rootCanvasGroup == null) rootCanvasGroup = GetComponent<CanvasGroup>();
      if (darkOverlay == null) darkOverlay = transform.Find("DarkOverlay")?.GetComponent<Image>();

      // BlackSmithPanel
      if (blacksmithPanel == null) blacksmithPanel = transform.Find("BlackSmithPanel") as RectTransform;
      if (blacksmithPanel != null)
      {
        Transform header = blacksmithPanel.Find("HeaderRow");
        if (header != null)
        {
          if (blacksmithTitleText == null) blacksmithTitleText = header.Find("NameText")?.GetComponent<TextMeshProUGUI>();
          if (blacksmithDescText == null) blacksmithDescText = header.Find("SmallDescriptionText")?.GetComponent<TextMeshProUGUI>();
        }

        Transform tabBar = blacksmithPanel.Find("TabBar");
        if (tabBar != null)
        {
          if (btnTabUpgrade == null) btnTabUpgrade = tabBar.Find("BtnUpgrade")?.GetComponent<Button>();
          if (btnTabForge == null) btnTabForge = tabBar.Find("BtnForge")?.GetComponent<Button>();
          if (tabUpgradeHighlight == null && btnTabUpgrade != null) tabUpgradeHighlight = btnTabUpgrade.transform.Find("SelectHighlight")?.gameObject;
          if (tabForgeHighlight == null && btnTabForge != null) tabForgeHighlight = btnTabForge.transform.Find("SelectHighlight")?.gameObject;
        }

        // UpgradePanel
        Transform upTr = blacksmithPanel.Find("UpgradePanel");
        if (upTr != null)
        {
          upgradePanelObj = upTr.gameObject;
          beforeWindow = upTr.Find("BeforeWindow");
          if (beforeWindow != null)
          {
            Transform slot = beforeWindow.Find("UpgradeSlot");
            if (slot != null)
            {
              if (beforeIcon == null) beforeIcon = slot.Find("ItemIcon")?.GetComponent<Image>();
              if (beforeRarityBg == null) beforeRarityBg = slot.Find("RarityBG")?.GetComponent<Image>();
              if (beforeRarityBar == null) beforeRarityBar = slot.Find("RarityBar")?.GetComponent<Image>();
            }
            if (beforeItemName == null) beforeItemName = beforeWindow.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (beforeUpgradeLevelText == null) beforeUpgradeLevelText = beforeWindow.Find("UpgradeLevel")?.GetComponent<TextMeshProUGUI>();
            if (beforeItemStats == null) beforeItemStats = beforeWindow.Find("ItemStats")?.GetComponent<TextMeshProUGUI>();
          }

          if (arrowObj == null) arrowObj = upTr.Find("Arrow")?.gameObject;

          afterWindow = upTr.Find("AfterWindow");
          if (afterWindow != null)
          {
            Transform slot = afterWindow.Find("UpgradeSlot");
            if (slot != null)
            {
              if (afterIcon == null) afterIcon = slot.Find("ItemIcon")?.GetComponent<Image>();
              if (afterRarityBg == null) afterRarityBg = slot.Find("RarityBG")?.GetComponent<Image>();
              if (afterRarityBar == null) afterRarityBar = slot.Find("RarityBar")?.GetComponent<Image>();
            }
            if (afterItemName == null) afterItemName = afterWindow.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (afterUpgradeLevelText == null) afterUpgradeLevelText = afterWindow.Find("UpgradeLevel")?.GetComponent<TextMeshProUGUI>();
            if (afterItemStats == null) afterItemStats = afterWindow.Find("ItemStats")?.GetComponent<TextMeshProUGUI>();
          }

          if (materialsNeededText == null) materialsNeededText = upTr.Find("MaterialsNeededText")?.GetComponent<TextMeshProUGUI>();
          if (upgradeMaterialsContainer == null) upgradeMaterialsContainer = upTr.Find("MaterialsHorizontalVIew");

          Transform footer = upTr.Find("Footer");
          if (footer != null)
          {
            Transform gp = footer.Find("GoldPrice");
            if (gp != null)
            {
              if (upgradeGoldPriceText == null) upgradeGoldPriceText = gp.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
              if (upgradeGoldIcon == null) upgradeGoldIcon = gp.Find("GoldIcon")?.GetComponent<Image>();
            }
            Transform btnTr = footer.Find("BtnUpgrade");
            if (btnTr != null)
            {
              if (btnUpgrade == null) btnUpgrade = btnTr.GetComponent<Button>();
              if (btnUpgradeText == null) btnUpgradeText = btnTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
              if (btnUpgradeBorder == null) btnUpgradeBorder = btnTr.Find("Border")?.gameObject;
              if (btnUpgradeCorner == null) btnUpgradeCorner = btnTr.Find("Corner")?.gameObject;
            }
          }
        }

        // ForgePanel
        Transform fgTr = blacksmithPanel.Find("ForgePanel");
        if (fgTr != null)
        {
          forgePanelObj = fgTr.gameObject;
          Transform scroll = fgTr.Find("ItemForgeScrollView");
          if (scroll != null)
          {
            if (forgeScrollRect == null) forgeScrollRect = scroll.GetComponent<ScrollRect>();
            if (forgeContent == null) forgeContent = scroll.Find("Viewport/Content");
            if (forgeScrollRect != null && forgeScrollRect.scrollSensitivity < 10f) forgeScrollRect.scrollSensitivity = 20f;
          }

          Transform win = fgTr.Find("ItemForgeWindow");
          if (win != null)
          {
            Transform slot = win.Find("ForgeSlot");
            if (slot != null)
            {
              if (forgeSlotIcon == null) forgeSlotIcon = slot.Find("ItemIcon")?.GetComponent<Image>();
              if (forgeSlotRarityBg == null) forgeSlotRarityBg = slot.Find("RarityBG")?.GetComponent<Image>();
              if (forgeSlotRarityBar == null) forgeSlotRarityBar = slot.Find("RarityBar")?.GetComponent<Image>();
            }
            if (forgeItemName == null) forgeItemName = win.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (forgeItemStats == null) forgeItemStats = win.Find("ItemStats")?.GetComponent<TextMeshProUGUI>();
            if (forgeMaterialsContainer == null) forgeMaterialsContainer = win.Find("MaterialsHorizontalVIew");

            Transform footer = win.Find("Footer");
            if (footer != null)
            {
              Transform gp = footer.Find("GoldPrice");
              if (gp != null)
              {
                if (forgeGoldPriceText == null) forgeGoldPriceText = gp.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                if (forgeGoldIcon == null) forgeGoldIcon = gp.Find("GoldIcon")?.GetComponent<Image>();
              }
              Transform btnTr = footer.Find("BtnForge");
              if (btnTr != null)
              {
                if (btnForge == null) btnForge = btnTr.GetComponent<Button>();
                if (btnForgeText == null) btnForgeText = btnTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (btnForgeBorder == null) btnForgeBorder = btnTr.Find("Border")?.gameObject;
                if (btnForgeCorner == null) btnForgeCorner = btnTr.Find("Corner")?.gameObject;
              }
            }
          }
        }
      }

      // Inventory Panel
      if (inventoryPanel == null) inventoryPanel = transform.Find("InventoryPanel") as RectTransform;
      if (inventoryPanel != null)
      {
        if (inventoryGridContainer == null) inventoryGridContainer = inventoryPanel.Find("GridInventoryContainer");
        Transform footer = inventoryPanel.Find("FooterRow");
        if (footer != null && playerGoldText == null) playerGoldText = footer.Find("NumberText")?.GetComponent<TextMeshProUGUI>();

        Transform tabBar = inventoryPanel.Find("TabBar");
        if (tabBar != null)
        {
          if (tabAllButton == null) tabAllButton = tabBar.Find("BtnAll")?.GetComponent<Button>();
          if (tabWeaponButton == null) tabWeaponButton = tabBar.Find("BtnWeapon")?.GetComponent<Button>();
          if (tabArmorButton == null) tabArmorButton = tabBar.Find("BtnArmor")?.GetComponent<Button>();
          if (tabItemButton == null) tabItemButton = tabBar.Find("BtnItem")?.GetComponent<Button>();

          if (tabAllHighlight == null && tabAllButton != null) tabAllHighlight = tabAllButton.transform.Find("SelectHighlight")?.gameObject;
          if (tabWeaponHighlight == null && tabWeaponButton != null) tabWeaponHighlight = tabWeaponButton.transform.Find("SelectHighlight")?.gameObject;
          if (tabArmorHighlight == null && tabArmorButton != null) tabArmorHighlight = tabArmorButton.transform.Find("SelectHighlight")?.gameObject;
          if (tabItemHighlight == null && tabItemButton != null) tabItemHighlight = tabItemButton.transform.Find("SelectHighlight")?.gameObject;
        }
      }

      // Popup
      if (itemDetailPopup == null)
      {
        itemDetailPopup = transform.Find("ItemDetailPopup")?.GetComponent<ItemDetailPopupUI>()
                       ?? GetComponentInChildren<ItemDetailPopupUI>(true);
      }
    }

    private void SetupEvents()
    {
      if (closeButton != null) closeButton.onClick.AddListener(CloseBlacksmith);
      if (btnTabUpgrade != null) btnTabUpgrade.onClick.AddListener(() => SwitchBlacksmithTab(BlacksmithTab.Upgrade));
      if (btnTabForge != null) btnTabForge.onClick.AddListener(() => SwitchBlacksmithTab(BlacksmithTab.Forge));

      if (btnUpgrade != null) btnUpgrade.onClick.AddListener(HandleUpgradeClicked);
      if (btnForge != null) btnForge.onClick.AddListener(HandleForgeClicked);

      if (itemDetailPopup != null)
      {
        itemDetailPopup.SetBlacksmithMode(true);
        itemDetailPopup.OnPutInClicked += HandlePutInFromBag;
      }

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryDataChanged;
        InventoryManager.Instance.OnGoldChanged += HandleGoldChanged;
      }
    }

    private void OnDestroy()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnInventoryChanged -= HandleInventoryDataChanged;
        InventoryManager.Instance.OnGoldChanged -= HandleGoldChanged;
      }
    }

    private void AutoFindSlots()
    {
      if (inventoryGridContainer != null && (bagSlots == null || bagSlots.Count == 0))
      {
        bagSlots = new List<InventorySlotUI>(inventoryGridContainer.GetComponentsInChildren<InventorySlotUI>(true));
      }

      for (int i = 0; i < bagSlots.Count; i++)
      {
        if (bagSlots[i] != null)
        {
          bagSlots[i].OnClicked -= HandleBagSlotClicked;
          bagSlots[i].OnClicked += HandleBagSlotClicked;
        }
      }
    }

    private void SetupTabButtons()
    {
      if (tabAllButton != null) tabAllButton.onClick.AddListener(() => SwitchInventoryTab(InventoryTab.All));
      if (tabWeaponButton != null) tabWeaponButton.onClick.AddListener(() => SwitchInventoryTab(InventoryTab.Weapon));
      if (tabArmorButton != null) tabArmorButton.onClick.AddListener(() => SwitchInventoryTab(InventoryTab.Armor));
      if (tabItemButton != null) tabItemButton.onClick.AddListener(() => SwitchInventoryTab(InventoryTab.Item));
    }

    // =========================================================================
    // MỞ / ĐÓNG GIAO DIỆN LÒ RÈN (OPEN / CLOSE)
    // =========================================================================

    public void OpenBlacksmith()
    {
      IsOpen = true;
      AutoFindReferences();
      CachePlayerController();

      if (cachedPlayerController != null) cachedPlayerController.LockMovement();
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      // Tiêu đề & mô tả
      if (blacksmithTitleText != null) blacksmithTitleText.text = "HAVEN BLACKSMITH";
      if (blacksmithDescText != null) blacksmithDescText.text = "Enhance & forge legendary armaments with ancient cores and ore.";

      // Mặc định mở Tab Cường Hóa (Upgrade)
      SwitchBlacksmithTab(BlacksmithTab.Upgrade);

      // Làm mới kho đồ bên phải & tự động chọn vũ khí đầu tiên nếu có
      currentInventoryTab = InventoryTab.Weapon;
      UpdateInventoryTabHighlight();
      RefreshInventoryGrid();
      UpdatePlayerGold();

      AutoSelectFirstWeaponToUpgrade();

      // Cấu hình popup
      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
        itemDetailPopup.SetBlacksmithMode(true);
      }

      if (transitionRoutine != null) StopCoroutine(transitionRoutine);
      transitionRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    public void CloseBlacksmith()
    {
      if (!IsOpen) return;
      IsOpen = false;

      if (itemDetailPopup != null) itemDetailPopup.HideImmediate();
      if (cachedPlayerController != null) cachedPlayerController.UnlockMovement();
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      if (transitionRoutine != null) StopCoroutine(transitionRoutine);
      transitionRoutine = StartCoroutine(FadeRoutine(0f, false));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool enableBlocks)
    {
      if (rootCanvasGroup == null) yield break;
      float startAlpha = rootCanvasGroup.alpha;
      float elapsed = 0f;

      if (enableBlocks)
      {
        rootCanvasGroup.blocksRaycasts = true;
        rootCanvasGroup.interactable = true;
      }

      while (elapsed < fadeDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / fadeDuration);
        rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
        yield return null;
      }

      rootCanvasGroup.alpha = targetAlpha;
      if (!enableBlocks)
      {
        rootCanvasGroup.blocksRaycasts = false;
        rootCanvasGroup.interactable = false;
      }
      transitionRoutine = null;
    }

    private void CachePlayerController()
    {
      if (cachedPlayerController == null)
      {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) cachedPlayerController = playerObj.GetComponent<PlayerController>();
        if (cachedPlayerController == null) cachedPlayerController = FindFirstObjectByType<PlayerController>();
      }
    }

    // =========================================================================
    // CHUYỂN ĐỔI TAB LÒ RÈN: [ UPGRADE | FORGE ]
    // =========================================================================

    public void SwitchBlacksmithTab(BlacksmithTab tab)
    {
      currentBlacksmithTab = tab;
      if (tabUpgradeHighlight != null) tabUpgradeHighlight.SetActive(tab == BlacksmithTab.Upgrade);
      if (tabForgeHighlight != null) tabForgeHighlight.SetActive(tab == BlacksmithTab.Forge);

      if (upgradePanelObj != null) upgradePanelObj.SetActive(tab == BlacksmithTab.Upgrade);
      if (forgePanelObj != null) forgePanelObj.SetActive(tab == BlacksmithTab.Forge);

      if (itemDetailPopup != null) itemDetailPopup.HideImmediate();

      if (tab == BlacksmithTab.Upgrade)
      {
        UpdateUpgradeUI();
      }
      else
      {
        PopulateForgeRecipesList();
        SelectDefaultFirstRecipe();
      }
    }

    // =========================================================================
    // TAB 1: LOGIC CƯỜNG HÓA TRANG BỊ (+1 ĐẾN +5)
    // =========================================================================

    private void AutoSelectFirstWeaponToUpgrade()
    {
      if (!InventoryManager.HasInstance) return;

      // Ưu tiên vũ khí đang trang bị
      if (InventoryManager.Instance.EquippedWeapon != null)
      {
        selectedBagSlotIndex = -100; // Đánh dấu ô đang mặc (Equipped Weapon)
        selectedUpgradeItem = InventoryManager.Instance.EquippedWeapon;
        selectedUpgradeLevel = InventoryManager.Instance.EquippedWeaponUpgradeLevel;
        UpdateUpgradeUI();
        return;
      }

      // Hoặc tìm trang bị đầu tiên trong túi đồ
      var slots = InventoryManager.Instance.Slots;
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && (slots[i].ItemData.ItemType == ItemType.Weapon || slots[i].ItemData.ItemType == ItemType.Armor))
        {
          selectedBagSlotIndex = i;
          selectedUpgradeItem = slots[i].ItemData;
          selectedUpgradeLevel = slots[i].UpgradeLevel;
          UpdateUpgradeUI();
          return;
        }
      }

      // Không có trang bị nào
      selectedBagSlotIndex = -1;
      selectedUpgradeItem = null;
      selectedUpgradeLevel = 0;
      UpdateUpgradeUI();
    }

    public void SetItemToUpgrade(ItemData item, int upgradeLevel, int bagSlotIndex)
    {
      selectedUpgradeItem = item;
      selectedUpgradeLevel = Mathf.Clamp(upgradeLevel, 0, 5);
      selectedBagSlotIndex = bagSlotIndex;
      UpdateUpgradeUI();
    }

    private void UpdateUpgradeUI()
    {
      if (selectedUpgradeItem == null)
      {
        ClearUpgradeUI();
        return;
      }

      ItemData item = selectedUpgradeItem;
      int curLevel = selectedUpgradeLevel;
      bool isMaxLevel = curLevel >= BlacksmithUpgradeConfig.MAX_UPGRADE_LEVEL;

      // 1. Before Window
      if (beforeIcon != null)
      {
        beforeIcon.sprite = item.Icon;
        beforeIcon.enabled = item.Icon != null;
        beforeIcon.color = Color.white;
      }
      Color rColor = item.Rarity.GetRarityColor();
      if (beforeRarityBg != null)
      {
        beforeRarityBg.gameObject.SetActive(true);
        beforeRarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (beforeRarityBar != null)
      {
        beforeRarityBar.gameObject.SetActive(true);
        beforeRarityBar.color = rColor;
      }
      if (beforeItemName != null) beforeItemName.text = item.Rarity.FormatWithGradient(item.ItemName);
      if (beforeUpgradeLevelText != null) beforeUpgradeLevelText.text = curLevel > 0 ? $"+{curLevel}" : "+0";

      float currentAtk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(item.BonusAttack, curLevel);
      float currentDef = BlacksmithUpgradeConfig.CalculateUpgradedDefense(item.BonusDefense, curLevel);
      if (beforeItemStats != null)
      {
        if (item.ItemType == ItemType.Weapon)
          beforeItemStats.text = $"Attack (ATK): <color=#FFE082>{currentAtk:0.#}</color>";
        else if (item.ItemType == ItemType.Armor)
          beforeItemStats.text = $"Defense (DEF): <color=#81D4FA>{currentDef:0.#}</color>";
        else
          beforeItemStats.text = "";
      }

      // 2. After Window
      if (isMaxLevel)
      {
        if (afterItemName != null) afterItemName.text = "<color=#FFD700>MAX LEVEL</color>";
        if (afterUpgradeLevelText != null) afterUpgradeLevelText.text = "+5 (MAX)";
        if (afterItemStats != null) afterItemStats.text = "<color=#FFD700>Weapon at peak power!</color>";
        if (afterIcon != null)
        {
          afterIcon.sprite = item.Icon;
          afterIcon.enabled = item.Icon != null;
        }
        if (afterRarityBg != null) afterRarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
        if (afterRarityBar != null) afterRarityBar.color = rColor;

        if (materialsNeededText != null) materialsNeededText.text = "MAXIMUM ENHANCEMENT REACHED";
        ClearMaterialsContainer(upgradeMaterialsContainer);

        if (upgradeGoldPriceText != null)
        {
          upgradeGoldPriceText.text = "0";
          upgradeGoldPriceText.color = normalPriceColor;
        }

        if (btnUpgrade != null) btnUpgrade.interactable = false;
        if (btnUpgradeText != null)
        {
          btnUpgradeText.text = "MAX LEVEL";
          btnUpgradeText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }
        if (btnUpgradeBorder != null) btnUpgradeBorder.SetActive(false);
        if (btnUpgradeCorner != null) btnUpgradeCorner.SetActive(false);
        return;
      }

      int nextLevel = curLevel + 1;
      float nextAtk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(item.BonusAttack, nextLevel);
      float nextDef = BlacksmithUpgradeConfig.CalculateUpgradedDefense(item.BonusDefense, nextLevel);

      if (afterIcon != null)
      {
        afterIcon.sprite = item.Icon;
        afterIcon.enabled = item.Icon != null;
        afterIcon.color = Color.white;
      }
      if (afterRarityBg != null)
      {
        afterRarityBg.gameObject.SetActive(true);
        afterRarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (afterRarityBar != null)
      {
        afterRarityBar.gameObject.SetActive(true);
        afterRarityBar.color = rColor;
      }
      if (afterItemName != null) afterItemName.text = item.Rarity.FormatWithGradient(item.ItemName);
      if (afterUpgradeLevelText != null) afterUpgradeLevelText.text = $"+{nextLevel}";
      if (afterItemStats != null)
      {
        if (item.ItemType == ItemType.Weapon)
          afterItemStats.text = $"Attack (ATK): <color=#77FF77>{nextAtk:0.#} (+15%)</color>";
        else if (item.ItemType == ItemType.Armor)
          afterItemStats.text = $"Defense (DEF): <color=#77FF77>{nextDef:0.#} (+15%)</color>";
        else
          afterItemStats.text = "";
      }

      // 3. Materials Needed
      if (materialsNeededText != null) materialsNeededText.text = "Materials Needed";
      BlacksmithUpgradeConfig.GetRequirement(curLevel, out var req);

      PopulateUpgradeMaterials(req);

      // 4. Gold & Button State
      int currentGold = InventoryManager.HasInstance ? InventoryManager.Instance.CurrentGold : 0;
      bool hasEnoughGold = currentGold >= req.GoldCost;
      if (upgradeGoldPriceText != null)
      {
        upgradeGoldPriceText.text = req.GoldCost.ToString("N0");
        upgradeGoldPriceText.color = hasEnoughGold ? normalPriceColor : redPriceColor;
      }

      bool canAfford = BlacksmithUpgradeConfig.CanAffordUpgrade(curLevel, InventoryManager.Instance, out _, out _, out _);
      if (btnUpgrade != null) btnUpgrade.interactable = canAfford;
      if (btnUpgradeBorder != null) btnUpgradeBorder.SetActive(canAfford);
      if (btnUpgradeCorner != null) btnUpgradeCorner.SetActive(canAfford);
      if (btnUpgradeText != null)
      {
        btnUpgradeText.text = "UPGRADE";
        btnUpgradeText.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);
      }
    }

    private void PopulateUpgradeMaterials(BlacksmithUpgradeConfig.UpgradeRequirement req)
    {
      if (upgradeMaterialsContainer == null) return;

      int stoneOwned = InventoryManager.HasInstance ? InventoryManager.Instance.GetItemCount(BlacksmithUpgradeConfig.UPGRADE_STONE_ID) : 0;
      int crystalOwned = InventoryManager.HasInstance ? InventoryManager.Instance.GetItemCount(BlacksmithUpgradeConfig.UPGRADE_CRYSTAL_ID) : 0;

      // Danh sách các nguyên liệu cần hiển thị
      var list = new List<(string name, int owned, int needed, Sprite icon, ItemRarity rarity)>();

      ItemData stoneItem = FindItemAssetById(BlacksmithUpgradeConfig.UPGRADE_STONE_ID);
      list.Add((stoneItem != null ? stoneItem.ItemName : "Enhancement Stone", stoneOwned, req.StoneCount, stoneItem?.Icon, stoneItem != null ? stoneItem.Rarity : ItemRarity.Uncommon));

      if (req.CrystalCount > 0)
      {
        ItemData crystalItem = FindItemAssetById(BlacksmithUpgradeConfig.UPGRADE_CRYSTAL_ID);
        list.Add((crystalItem != null ? crystalItem.ItemName : "Flawless Crystal", crystalOwned, req.CrystalCount, crystalItem?.Icon, crystalItem != null ? crystalItem.Rarity : ItemRarity.Rare));
      }

      // Tái sử dụng hoặc ẩn bớt các GameObject con trong container
      int childCount = upgradeMaterialsContainer.childCount;
      for (int i = 0; i < childCount; i++)
      {
        Transform child = upgradeMaterialsContainer.GetChild(i);
        if (i < list.Count)
        {
          child.gameObject.SetActive(true);
          BindMaterialItemUI(child, list[i].name, list[i].owned, list[i].needed, list[i].icon, list[i].rarity);
        }
        else
        {
          child.gameObject.SetActive(false);
        }
      }

      // Nếu thiếu GameObject con thì nhân bản từ cái đầu tiên
      if (childCount > 0)
      {
        GameObject template = upgradeMaterialsContainer.GetChild(0).gameObject;
        for (int i = childCount; i < list.Count; i++)
        {
          GameObject clone = Instantiate(template, upgradeMaterialsContainer);
          clone.SetActive(true);
          BindMaterialItemUI(clone.transform, list[i].name, list[i].owned, list[i].needed, list[i].icon, list[i].rarity);
        }
      }
    }

    private void BindMaterialItemUI(Transform matTr, string name, int owned, int needed, Sprite icon, ItemRarity rarity)
    {
      if (matTr == null) return;
      var nameTmp = matTr.Find("MaterialName")?.GetComponent<TextMeshProUGUI>();
      var qtyTmp = matTr.Find("QuantityNeeded")?.GetComponent<TextMeshProUGUI>();
      var iconImg = matTr.Find("Icon/ItemIcon")?.GetComponent<Image>() ?? matTr.Find("ItemIcon")?.GetComponent<Image>();
      var bgImg = matTr.Find("Icon/RarityBG")?.GetComponent<Image>() ?? matTr.Find("RarityBG")?.GetComponent<Image>();
      var barImg = matTr.Find("Icon/RarityBar")?.GetComponent<Image>() ?? matTr.Find("RarityBar")?.GetComponent<Image>();

      if (nameTmp != null) nameTmp.text = name;
      if (qtyTmp != null)
      {
        bool enough = owned >= needed;
        qtyTmp.text = enough ? $"<color=#59F259>{owned}/{needed}</color>" : $"<color=#FF4747>{owned}/{needed}</color>";
      }
      if (iconImg != null)
      {
        iconImg.sprite = icon;
        iconImg.enabled = icon != null;
        iconImg.color = Color.white;
      }
      Color rColor = rarity.GetRarityColor();
      if (bgImg != null) bgImg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      if (barImg != null) barImg.color = rColor;
    }

    private void ClearMaterialsContainer(Transform container)
    {
      if (container == null) return;
      for (int i = 0; i < container.childCount; i++)
      {
        container.GetChild(i).gameObject.SetActive(false);
      }
    }

    private void ClearUpgradeUI()
    {
      if (beforeIcon != null) beforeIcon.enabled = false;
      if (beforeItemName != null) beforeItemName.text = "Select equipment";
      if (beforeUpgradeLevelText != null) beforeUpgradeLevelText.text = "";
      if (beforeItemStats != null) beforeItemStats.text = "Select a weapon or armor from your inventory.";

      if (afterIcon != null) afterIcon.enabled = false;
      if (afterItemName != null) afterItemName.text = "Next Tier";
      if (afterUpgradeLevelText != null) afterUpgradeLevelText.text = "";
      if (afterItemStats != null) afterItemStats.text = "";

      ClearMaterialsContainer(upgradeMaterialsContainer);
      if (upgradeGoldPriceText != null) upgradeGoldPriceText.text = "0";
      if (btnUpgrade != null) btnUpgrade.interactable = false;
      if (btnUpgradeBorder != null) btnUpgradeBorder.SetActive(false);
      if (btnUpgradeCorner != null) btnUpgradeCorner.SetActive(false);
    }

    private void HandleUpgradeClicked()
    {
      if (selectedUpgradeItem == null || !InventoryManager.HasInstance) return;
      if (selectedUpgradeLevel >= BlacksmithUpgradeConfig.MAX_UPGRADE_LEVEL) return;

      if (!BlacksmithUpgradeConfig.GetRequirement(selectedUpgradeLevel, out var req)) return;
      if (!BlacksmithUpgradeConfig.CanAffordUpgrade(selectedUpgradeLevel, InventoryManager.Instance, out _, out _, out _)) return;

      // 1. Trừ vàng
      InventoryManager.Instance.SpendGold(req.GoldCost);

      // 2. Trừ nguyên liệu
      InventoryManager.Instance.RemoveItem(BlacksmithUpgradeConfig.UPGRADE_STONE_ID, req.StoneCount);
      if (req.CrystalCount > 0)
      {
        InventoryManager.Instance.RemoveItem(BlacksmithUpgradeConfig.UPGRADE_CRYSTAL_ID, req.CrystalCount);
      }

      // 3. Tăng cấp cường hóa
      if (selectedBagSlotIndex == -100)
      {
        // Trang bị đang mặc
        InventoryManager.Instance.UpgradeEquipped(selectedUpgradeItem.ItemType);
        selectedUpgradeLevel = (selectedUpgradeItem.ItemType == ItemType.Weapon)
          ? InventoryManager.Instance.EquippedWeaponUpgradeLevel
          : InventoryManager.Instance.EquippedArmorUpgradeLevel;
      }
      else if (selectedBagSlotIndex >= 0)
      {
        // Ô trong túi đồ
        InventoryManager.Instance.UpgradeSlot(selectedBagSlotIndex);
        if (selectedBagSlotIndex < InventoryManager.Instance.Slots.Count)
        {
          selectedUpgradeLevel = InventoryManager.Instance.Slots[selectedBagSlotIndex].UpgradeLevel;
        }
      }

      // 4. Punch Scale Animation (Game Feel)
      if (afterWindow != null)
      {
        StartCoroutine(PunchRoutine(afterWindow, 1.1f, 0.15f));
      }

      // 5. Làm mới UI
      UpdateUpgradeUI();
      RefreshInventoryGrid();
      UpdatePlayerGold();
    }

    // =========================================================================
    // TAB 2: LOGIC CHẾ TẠO THẦN BINH (FORGE PANEL)
    // =========================================================================

    private void LoadAllRecipesIfEmpty()
    {
      if (craftingRecipes == null || craftingRecipes.Count == 0)
      {
        craftingRecipes = new List<CraftingRecipe>(Resources.LoadAll<CraftingRecipe>(""));
#if UNITY_EDITOR
        if (craftingRecipes.Count == 0)
        {
          string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CraftingRecipe");
          foreach (var g in guids)
          {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var r = UnityEditor.AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            if (r != null && !craftingRecipes.Contains(r)) craftingRecipes.Add(r);
          }
        }
#endif
      }
    }

    private void PopulateForgeRecipesList()
    {
      LoadAllRecipesIfEmpty();
      if (forgeContent == null || craftingRecipes == null) return;

      var existingRows = forgeContent.GetComponentsInChildren<ForgeRecipeSlotUI>(true);
      if (existingRows == null || existingRows.Length == 0)
      {
        // Tự động gắn ForgeRecipeSlotUI vào mẫu có sẵn
        for (int i = 0; i < forgeContent.childCount; i++)
        {
          Transform child = forgeContent.GetChild(i);
          var slot = child.GetComponent<ForgeRecipeSlotUI>() ?? child.gameObject.AddComponent<ForgeRecipeSlotUI>();
          slot.AutoFindReferences();
        }
        existingRows = forgeContent.GetComponentsInChildren<ForgeRecipeSlotUI>(true);
      }

      recipeSlots.Clear();
      recipeSlots.AddRange(existingRows);

      int targetCount = craftingRecipes.Count;
      while (recipeSlots.Count < targetCount && recipeSlots.Count > 0)
      {
        GameObject clone = Instantiate(recipeSlots[0].gameObject, forgeContent);
        ForgeRecipeSlotUI newSlot = clone.GetComponent<ForgeRecipeSlotUI>();
        recipeSlots.Add(newSlot);
      }

      for (int i = 0; i < recipeSlots.Count; i++)
      {
        if (i < targetCount)
        {
          CraftingRecipe r = craftingRecipes[i];
          recipeSlots[i].Setup(r, i);
          recipeSlots[i].OnClicked -= HandleRecipeSlotClicked;
          recipeSlots[i].OnClicked += HandleRecipeSlotClicked;
        }
        else
        {
          recipeSlots[i].gameObject.SetActive(false);
        }
      }
    }

    private void SelectDefaultFirstRecipe()
    {
      if (recipeSlots.Count > 0 && recipeSlots[0].gameObject.activeSelf)
      {
        SelectRecipeSlot(recipeSlots[0]);
      }
      else
      {
        ClearForgeWindow();
      }
    }

    private void HandleRecipeSlotClicked(ForgeRecipeSlotUI slot)
    {
      if (slot == null || slot.Recipe == null) return;
      SelectRecipeSlot(slot);
    }

    private void SelectRecipeSlot(ForgeRecipeSlotUI slot)
    {
      if (selectedRecipeSlot != null && selectedRecipeSlot != slot)
      {
        selectedRecipeSlot.SetSelected(false);
      }
      selectedRecipeSlot = slot;
      if (selectedRecipeSlot != null)
      {
        selectedRecipeSlot.SetSelected(true);
        UpdateForgeWindow(selectedRecipeSlot.Recipe);
      }
    }

    private void UpdateForgeWindow(CraftingRecipe recipe)
    {
      if (recipe == null || recipe.ResultItem == null)
      {
        ClearForgeWindow();
        return;
      }

      ItemData item = recipe.ResultItem;

      // 1. Forge Slot
      if (forgeSlotIcon != null)
      {
        forgeSlotIcon.sprite = item.Icon;
        forgeSlotIcon.enabled = item.Icon != null;
        forgeSlotIcon.color = Color.white;
      }
      Color rColor = item.Rarity.GetRarityColor();
      if (forgeSlotRarityBg != null)
      {
        forgeSlotRarityBg.gameObject.SetActive(true);
        forgeSlotRarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (forgeSlotRarityBar != null)
      {
        forgeSlotRarityBar.gameObject.SetActive(true);
        forgeSlotRarityBar.color = rColor;
      }

      // 2. Name & Stats
      if (forgeItemName != null) forgeItemName.text = item.Rarity.FormatWithGradient(recipe.RecipeName);
      if (forgeItemStats != null)
      {
        if (item.ItemType == ItemType.Weapon)
          forgeItemStats.text = $"Attack (ATK): <color=#FFE082>{item.BonusAttack}</color>";
        else if (item.ItemType == ItemType.Armor)
          forgeItemStats.text = $"Defense (DEF): <color=#81D4FA>{item.BonusDefense}</color>";
        else
          forgeItemStats.text = recipe.Description;
      }

      // 3. Materials Needed
      bool hasAllMaterials = true;
      if (forgeMaterialsContainer != null)
      {
        var reqMats = recipe.RequiredMaterials;
        int childCount = forgeMaterialsContainer.childCount;

        for (int i = 0; i < childCount; i++)
        {
          Transform child = forgeMaterialsContainer.GetChild(i);
          if (i < reqMats.Count)
          {
            child.gameObject.SetActive(true);
            var mCost = reqMats[i];
            int owned = InventoryManager.HasInstance && mCost.MaterialItem != null
              ? InventoryManager.Instance.GetItemCount(mCost.MaterialItem)
              : 0;
            if (owned < mCost.Quantity) hasAllMaterials = false;
            BindMaterialItemUI(child, mCost.MaterialItem?.ItemName ?? "Material", owned, mCost.Quantity, mCost.MaterialItem?.Icon, mCost.MaterialItem != null ? mCost.MaterialItem.Rarity : ItemRarity.Common);
          }
          else
          {
            child.gameObject.SetActive(false);
          }
        }
      }

      // 4. Gold & Forge Button
      int currentGold = InventoryManager.HasInstance ? InventoryManager.Instance.CurrentGold : 0;
      bool hasEnoughGold = currentGold >= recipe.GoldCost;
      if (forgeGoldPriceText != null)
      {
        forgeGoldPriceText.text = recipe.GoldCost.ToString("N0");
        forgeGoldPriceText.color = hasEnoughGold ? normalPriceColor : redPriceColor;
      }

      bool canForge = hasAllMaterials && hasEnoughGold;
      if (btnForge != null) btnForge.interactable = canForge;
      if (btnForgeBorder != null) btnForgeBorder.SetActive(canForge);
      if (btnForgeCorner != null) btnForgeCorner.SetActive(canForge);
      if (btnForgeText != null)
      {
        btnForgeText.text = "FORGE";
        btnForgeText.color = canForge ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);
      }
    }

    private void ClearForgeWindow()
    {
      if (forgeSlotIcon != null) forgeSlotIcon.enabled = false;
      if (forgeItemName != null) forgeItemName.text = "Select a Recipe";
      if (forgeItemStats != null) forgeItemStats.text = "Select a divine armament recipe from the list.";
      ClearMaterialsContainer(forgeMaterialsContainer);
      if (forgeGoldPriceText != null) forgeGoldPriceText.text = "0";
      if (btnForge != null) btnForge.interactable = false;
      if (btnForgeBorder != null) btnForgeBorder.SetActive(false);
      if (btnForgeCorner != null) btnForgeCorner.SetActive(false);
    }

    private void HandleForgeClicked()
    {
      if (selectedRecipeSlot == null || selectedRecipeSlot.Recipe == null || !InventoryManager.HasInstance) return;
      CraftingRecipe r = selectedRecipeSlot.Recipe;

      // 1. Kiểm tra vàng & nguyên liệu
      int curGold = InventoryManager.Instance.CurrentGold;
      if (curGold < r.GoldCost) return;

      foreach (var m in r.RequiredMaterials)
      {
        if (m.MaterialItem == null) continue;
        if (InventoryManager.Instance.GetItemCount(m.MaterialItem) < m.Quantity) return;
      }

      // 2. Kiểm tra slot trống túi đồ
      bool added = InventoryManager.Instance.AddItem(r.ResultItem, r.ResultQuantity, notifyPickup: true);
      if (!added)
      {
        if (btnForgeText != null) btnForgeText.text = "BAG FULL!";
        return;
      }

      // 3. Trừ vàng & trừ nguyên liệu
      InventoryManager.Instance.SpendGold(r.GoldCost);
      foreach (var m in r.RequiredMaterials)
      {
        if (m.MaterialItem != null)
        {
          InventoryManager.Instance.RemoveItem(m.MaterialItem, m.Quantity);
        }
      }

      // 4. Punch Animation
      if (btnForge != null)
      {
        StartCoroutine(PunchRoutine(btnForge.transform, 1.15f, 0.15f));
      }

      // 5. Làm mới UI
      UpdateForgeWindow(r);
      RefreshInventoryGrid();
      UpdatePlayerGold();
    }

    // =========================================================================
    // XỬ LÝ TÚI ĐỒ NGƯỜI CHƠI BÊN PHẢI (INVENTORY PANEL)
    // =========================================================================

    public void SwitchInventoryTab(InventoryTab tab)
    {
      currentInventoryTab = tab;
      UpdateInventoryTabHighlight();
      RefreshInventoryGrid();
      if (itemDetailPopup != null) itemDetailPopup.HideImmediate();
    }

    private void UpdateInventoryTabHighlight()
    {
      if (tabAllHighlight != null) tabAllHighlight.SetActive(currentInventoryTab == InventoryTab.All);
      if (tabWeaponHighlight != null) tabWeaponHighlight.SetActive(currentInventoryTab == InventoryTab.Weapon);
      if (tabArmorHighlight != null) tabArmorHighlight.SetActive(currentInventoryTab == InventoryTab.Armor);
      if (tabItemHighlight != null) tabItemHighlight.SetActive(currentInventoryTab == InventoryTab.Item);
    }

    private void RefreshInventoryGrid()
    {
      if (!InventoryManager.HasInstance || bagSlots == null) return;
      var invSlots = InventoryManager.Instance.Slots;

      if (currentInventoryTab == InventoryTab.All)
      {
        for (int i = 0; i < bagSlots.Count; i++)
        {
          if (bagSlots[i] == null) continue;
          if (i < invSlots.Count && !invSlots[i].IsEmpty)
          {
            bagSlots[i].SetItem(invSlots[i].ItemData, invSlots[i].Quantity, i, invSlots[i].UpgradeLevel);
          }
          else
          {
            bagSlots[i].ClearSlot(i);
          }
        }
      }
      else
      {
        var matchingSlots = new List<(ItemData item, int quantity, int originalIndex, int upgrade)>();
        for (int i = 0; i < invSlots.Count; i++)
        {
          if (invSlots[i].IsEmpty) continue;
          var item = invSlots[i].ItemData;
          bool isMatch = false;
          switch (currentInventoryTab)
          {
            case InventoryTab.Weapon: isMatch = (item.ItemType == ItemType.Weapon); break;
            case InventoryTab.Armor: isMatch = (item.ItemType == ItemType.Armor); break;
            case InventoryTab.Item: isMatch = (item.ItemType == ItemType.Consumable || item.ItemType == ItemType.Material); break;
          }
          if (isMatch)
          {
            matchingSlots.Add((item, invSlots[i].Quantity, i, invSlots[i].UpgradeLevel));
          }
        }

        for (int i = 0; i < bagSlots.Count; i++)
        {
          if (bagSlots[i] == null) continue;
          if (i < matchingSlots.Count)
          {
            var match = matchingSlots[i];
            bagSlots[i].SetItem(match.item, match.quantity, match.originalIndex, match.upgrade);
          }
          else
          {
            bagSlots[i].ClearSlot(-1);
          }
        }
      }
    }

    private void HandleBagSlotClicked(InventorySlotUI slot)
    {
      if (slot == null || slot.IsEmpty)
      {
        if (itemDetailPopup != null) itemDetailPopup.Hide();
        return;
      }

      if (itemDetailPopup != null)
      {
        itemDetailPopup.SetBlacksmithMode(true);
        itemDetailPopup.ShowForBagSlot(slot);
      }
    }

    private void HandlePutInFromBag(InventorySlotUI slot, ItemData item)
    {
      if (slot == null || item == null) return;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor) return;

      // Đặt món đồ vào ô BeforeWindow để cường hóa
      SetItemToUpgrade(item, slot.UpgradeLevel, slot.ActualBagIndex);

      // Tự động chuyển về tab Cường Hóa nếu đang ở tab Forge
      if (currentBlacksmithTab != BlacksmithTab.Upgrade)
      {
        SwitchBlacksmithTab(BlacksmithTab.Upgrade);
      }
    }

    private void HandleInventoryDataChanged()
    {
      if (!IsOpen) return;
      RefreshInventoryGrid();
      UpdatePlayerGold();
      if (currentBlacksmithTab == BlacksmithTab.Upgrade) UpdateUpgradeUI();
      else if (selectedRecipeSlot != null) UpdateForgeWindow(selectedRecipeSlot.Recipe);
    }

    private void HandleGoldChanged(int newGold)
    {
      if (!IsOpen) return;
      UpdatePlayerGold();
      if (currentBlacksmithTab == BlacksmithTab.Upgrade) UpdateUpgradeUI();
      else if (selectedRecipeSlot != null) UpdateForgeWindow(selectedRecipeSlot.Recipe);
    }

    private void UpdatePlayerGold()
    {
      if (playerGoldText != null && InventoryManager.HasInstance)
      {
        playerGoldText.text = InventoryManager.Instance.CurrentGold.ToString("N0");
      }
    }

    private ItemData FindItemAssetById(string id)
    {
      if (string.IsNullOrEmpty(id)) return null;
      var all = Resources.LoadAll<ItemData>("");
      foreach (var item in all)
      {
        if (item != null && item.Id == id) return item;
      }
#if UNITY_EDITOR
      string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
      foreach (var g in guids)
      {
        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        var it = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(p);
        if (it != null && it.Id == id) return it;
      }
#endif
      return null;
    }

    private IEnumerator PunchRoutine(Transform tr, float scale, float duration)
    {
      if (tr == null) yield break;
      Vector3 startScale = Vector3.one * scale;
      float elapsed = 0f;
      tr.localScale = startScale;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        tr.localScale = Vector3.Lerp(startScale, Vector3.one, t);
        yield return null;
      }
      tr.localScale = Vector3.one;
    }
  }
}
