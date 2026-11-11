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
    public static int LastClosedFrame { get; private set; } = -1;
    private int openedFrame = -1;

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

    [Header("=== Ô ĐE RÈN (UPGRADE SLOT INTERACTION) ===")]
    [SerializeField] private BlacksmithUpgradeSlotUI upgradeSlotUI;

    [SerializeField] private Transform afterWindow;
    [SerializeField] private Image afterIcon;
    [SerializeField] private Image afterRarityBg;
    [SerializeField] private Image afterRarityBar;
    [SerializeField] private TextMeshProUGUI afterItemName;
    [SerializeField] private TextMeshProUGUI afterUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI afterItemStats;

    [Header("=== CẤU TRÚC CHI TIẾT BEFORE / AFTER (CHUẨN ITEM DETAILS) ===")]
    [SerializeField] private BlacksmithUpgradeWindowUI beforeWindowUI = new BlacksmithUpgradeWindowUI();
    [SerializeField] private BlacksmithUpgradeWindowUI afterWindowUI = new BlacksmithUpgradeWindowUI();
    [SerializeField] private BlacksmithUpgradeWindowUI forgeWindowUI = new BlacksmithUpgradeWindowUI();

    [Header("=== TOOLTIP GIẢI THÍCH (ENCHANT HINT) ===")]
    [SerializeField] private RectTransform enchantHintRect;
    [SerializeField] private CanvasGroup enchantHintCanvasGroup;
    [SerializeField] private TextMeshProUGUI enchantHintText;
    private Coroutine hintFadeRoutine;

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
    [SerializeField] private Button btnForgeSlot;
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

    // Item đang chọn để nâng cấp (tạm giữ trên đe rèn)
    private int selectedBagSlotIndex = -1;
    private ItemData selectedUpgradeItem = null;
    private int selectedUpgradeLevel = 0;

    private ItemData heldUpgradeItem = null;
    private int heldUpgradeLevel = 0;
    private EquipmentAffixData heldUpgradeAffixData = null;
    private int originalBagIndex = -1;

    public ItemData HeldUpgradeItem => heldUpgradeItem;
    public int HeldUpgradeLevel => heldUpgradeLevel;
    public EquipmentAffixData HeldUpgradeAffixData => heldUpgradeAffixData;
    public bool HasUpgradeItem => heldUpgradeItem != null;

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

      if (InventoryDragDropController.Instance == null || !InventoryDragDropController.Instance.gameObject.activeInHierarchy)
      {
        var existing = FindFirstObjectByType<InventoryDragDropController>();
        if (existing == null || !existing.gameObject.activeInHierarchy)
        {
          gameObject.AddComponent<InventoryDragDropController>();
        }
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
        // 1. Phím F đóng Lò Rèn (bỏ qua frame vừa mở để tránh đóng ngay lập tức)
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
          if (Time.frameCount != openedFrame)
          {
            CloseBlacksmith();
            return;
          }
        }

        // 2. Phím ESC đóng popup chi tiết trước (nếu đang mở) hoặc đóng Lò Rèn
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
          if (itemDetailPopup != null && itemDetailPopup.gameObject.activeSelf)
          {
            itemDetailPopup.Hide();
          }
          else
          {
            CloseBlacksmith();
          }
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
              if (upgradeSlotUI == null) upgradeSlotUI = slot.GetComponent<BlacksmithUpgradeSlotUI>();
              if (upgradeSlotUI == null) upgradeSlotUI = slot.gameObject.AddComponent<BlacksmithUpgradeSlotUI>();
              upgradeSlotUI.AutoFindReferences();
            }
            beforeWindowUI.AutoFind(beforeWindow);
            if (beforeIcon == null) beforeIcon = beforeWindowUI.itemIcon;
            if (beforeRarityBg == null) beforeRarityBg = beforeWindowUI.rarityBg;
            if (beforeRarityBar == null) beforeRarityBar = beforeWindowUI.rarityBar;
            if (beforeItemName == null) beforeItemName = beforeWindowUI.itemName;
            if (beforeUpgradeLevelText == null) beforeUpgradeLevelText = beforeWindowUI.upgradeLevelText;
            if (beforeItemStats == null) beforeItemStats = beforeWindowUI.fallbackItemStats;
            SetupEnchantRowHoverTriggers(beforeWindowUI);
          }

          if (arrowObj == null) arrowObj = upTr.Find("Arrow")?.gameObject;

          afterWindow = upTr.Find("AfterWindow");
          if (afterWindow != null)
          {
            afterWindowUI.AutoFind(afterWindow);
            if (afterIcon == null) afterIcon = afterWindowUI.itemIcon;
            if (afterRarityBg == null) afterRarityBg = afterWindowUI.rarityBg;
            if (afterRarityBar == null) afterRarityBar = afterWindowUI.rarityBar;
            if (afterItemName == null) afterItemName = afterWindowUI.itemName;
            if (afterUpgradeLevelText == null) afterUpgradeLevelText = afterWindowUI.upgradeLevelText;
            if (afterItemStats == null) afterItemStats = afterWindowUI.fallbackItemStats;
            SetupEnchantRowHoverTriggers(afterWindowUI);
          }

          // Tìm hoặc nhân bản EnchantHint cho Lò Rèn (đặt dưới BlackSmithPanel để dùng chung cho cả Upgrade và Forge)
          if (enchantHintRect == null)
          {
            Transform hTr = blacksmithPanel?.Find("EnchantHint")
                         ?? upgradePanelObj?.transform.Find("EnchantHint")
                         ?? beforeWindow?.Find("EnchantHint")
                         ?? afterWindow?.Find("EnchantHint")
                         ?? transform.Find("EnchantHint");
            if (hTr != null)
            {
              enchantHintRect = hTr as RectTransform;
              enchantHintCanvasGroup = hTr.GetComponent<CanvasGroup>();
              enchantHintText = hTr.Find("Text")?.GetComponent<TextMeshProUGUI>() ?? hTr.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            else if (itemDetailPopup != null)
            {
              Transform templateHint = itemDetailPopup.transform.Find("EnchantHint");
              if (templateHint != null)
              {
                Transform pTarget = blacksmithPanel != null ? blacksmithPanel.transform : transform;
                GameObject clone = Instantiate(templateHint.gameObject, pTarget);
                clone.name = "EnchantHint";
                enchantHintRect = clone.GetComponent<RectTransform>();
                enchantHintCanvasGroup = clone.GetComponent<CanvasGroup>();
                enchantHintText = clone.GetComponentInChildren<TextMeshProUGUI>(true);
              }
            }
          }
          if (enchantHintRect != null && blacksmithPanel != null && enchantHintRect.parent != blacksmithPanel.transform)
          {
            enchantHintRect.SetParent(blacksmithPanel.transform, false);
            enchantHintRect.SetAsLastSibling();
          }
          if (enchantHintCanvasGroup != null)
          {
            enchantHintCanvasGroup.alpha = 0f;
            enchantHintCanvasGroup.blocksRaycasts = false;
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
            forgeWindowUI.AutoFind(win);
            if (btnForgeSlot == null) btnForgeSlot = win.Find("ForgeSlot")?.GetComponent<Button>();
            if (forgeSlotIcon == null) forgeSlotIcon = forgeWindowUI.itemIcon;
            if (forgeSlotRarityBg == null) forgeSlotRarityBg = forgeWindowUI.rarityBg;
            if (forgeSlotRarityBar == null) forgeSlotRarityBar = forgeWindowUI.rarityBar;
            if (forgeItemName == null) forgeItemName = forgeWindowUI.itemName;
            if (forgeItemStats == null) forgeItemStats = forgeWindowUI.fallbackItemStats;
            if (forgeMaterialsContainer == null) forgeMaterialsContainer = win.Find("MaterialsHorizontalVIew");
            SetupEnchantRowHoverTriggers(forgeWindowUI);

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
      if (btnForgeSlot != null) btnForgeSlot.onClick.AddListener(HandleForgeSlotClicked);

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

      if (bagSlots != null)
      {
        for (int i = 0; i < bagSlots.Count; i++)
        {
          if (bagSlots[i] != null)
          {
            bagSlots[i].OnClicked -= HandleBagSlotClicked;
            bagSlots[i].OnRightClicked -= HandleBagSlotRightClicked;
          }
        }
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
          bagSlots[i].OnRightClicked -= HandleBagSlotRightClicked;
          bagSlots[i].OnRightClicked += HandleBagSlotRightClicked;
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
      openedFrame = Time.frameCount;
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

      // Làm mới kho đồ bên phải & đặt ô cường hóa về trạng thái chờ
      currentInventoryTab = InventoryTab.All;
      UpdateInventoryTabHighlight();
      RefreshInventoryGrid();
      UpdatePlayerGold();

      ResetUpgradeSlot();

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

      // Tự động hoàn trả trang bị nếu còn nằm trên đe rèn
      if (heldUpgradeItem != null)
      {
        TakeOutUpgradeItem();
      }

      IsOpen = false;
      LastClosedFrame = Time.frameCount;

      HideEnchantHintImmediate();
      if (itemDetailPopup != null) itemDetailPopup.HideImmediate();
      HideEnchantHintImmediate();
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
      HideEnchantHintImmediate();

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

    private void ResetUpgradeSlot()
    {
      heldUpgradeItem = null;
      heldUpgradeLevel = 0;
      heldUpgradeAffixData = null;
      originalBagIndex = -1;

      selectedBagSlotIndex = -1;
      selectedUpgradeItem = null;
      selectedUpgradeLevel = 0;
      ClearUpgradeUI();
    }

    /// <summary>
    /// Nạp một trang bị từ túi đồ vào đe rèn để cường hóa (tạm thời nhấc khỏi túi đồ)
    /// </summary>
    public void PutItemIntoUpgrade(int bagSlotIndex)
    {
      if (!InventoryManager.HasInstance || bagSlotIndex < 0 || bagSlotIndex >= InventoryManager.Instance.Slots.Count) return;

      var slot = InventoryManager.Instance.Slots[bagSlotIndex];
      if (slot.IsEmpty) return;

      ItemData item = slot.ItemData;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor) return;

      // Nếu trước đó đang có trang bị khác trên đe rèn -> Hoàn trả lại túi trước!
      if (heldUpgradeItem != null)
      {
        TakeOutUpgradeItem();
      }

      // 1. Lưu lại trang bị trên đe rèn
      heldUpgradeItem = item;
      heldUpgradeLevel = slot.UpgradeLevel;
      heldUpgradeAffixData = slot.AffixData ?? slot.EnsureAffixData();
      originalBagIndex = bagSlotIndex;

      // 2. Nhấc ra khỏi túi đồ của người chơi
      InventoryManager.Instance.ClearSlot(bagSlotIndex);

      // 3. Đồng bộ trạng thái UI đe rèn
      selectedUpgradeItem = heldUpgradeItem;
      selectedUpgradeLevel = heldUpgradeLevel;
      selectedBagSlotIndex = originalBagIndex;

      UpdateUpgradeUI();
      RefreshInventoryGrid();

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }
    }

    /// <summary>
    /// Tháo trang bị từ đe rèn trả lại túi đồ (vào ô chỉ định hoặc ô ban đầu)
    /// </summary>
    public void TakeOutUpgradeItem(int targetBagIndex = -1)
    {
      if (heldUpgradeItem == null || !InventoryManager.HasInstance) return;

      var invSlots = InventoryManager.Instance.Slots;
      int destIndex = -1;

      // Ưu tiên 1: Ô đích người chơi kéo thả vào (nếu đang trống)
      if (targetBagIndex >= 0 && targetBagIndex < invSlots.Count && invSlots[targetBagIndex].IsEmpty)
      {
        destIndex = targetBagIndex;
      }
      // Ưu tiên 2: Ô gốc ban đầu trước khi nhấc ra (nếu đang trống)
      else if (originalBagIndex >= 0 && originalBagIndex < invSlots.Count && invSlots[originalBagIndex].IsEmpty)
      {
        destIndex = originalBagIndex;
      }
      // Ưu tiên 3: Tìm ô trống đầu tiên trong túi
      else
      {
        for (int i = 0; i < invSlots.Count; i++)
        {
          if (invSlots[i].IsEmpty)
          {
            destIndex = i;
            break;
          }
        }
      }

      if (destIndex >= 0)
      {
        invSlots[destIndex].SetItem(heldUpgradeItem, 1, heldUpgradeLevel, heldUpgradeAffixData);
        InventoryManager.Instance.NotifyInventoryChanged();
      }
      else
      {
        // Dự phòng túi đầy
        InventoryManager.Instance.AddItem(heldUpgradeItem, 1, upgradeLevel: heldUpgradeLevel, affixData: heldUpgradeAffixData);
      }

      // Xóa sạch trạng thái đe rèn
      heldUpgradeItem = null;
      heldUpgradeLevel = 0;
      heldUpgradeAffixData = null;
      originalBagIndex = -1;

      selectedUpgradeItem = null;
      selectedUpgradeLevel = 0;
      selectedBagSlotIndex = -1;

      ClearUpgradeUI();
      RefreshInventoryGrid();
      UpdatePlayerGold();

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }
    }

    /// <summary>
    /// Xem trước thông tin chi tiết trang bị đang đặt trên đe rèn
    /// </summary>
    public void ShowUpgradeSlotPreview()
    {
      if (heldUpgradeItem == null || itemDetailPopup == null) return;
      RectTransform targetRect = upgradeSlotUI != null ? upgradeSlotUI.transform as RectTransform : (beforeWindow as RectTransform);
      itemDetailPopup.ShowPreview(heldUpgradeItem, targetRect, heldUpgradeAffixData);
    }

    public void SetItemToUpgrade(ItemData item, int upgradeLevel, int bagSlotIndex)
    {
      PutItemIntoUpgrade(bagSlotIndex);
    }

    private string BuildUpgradeStatsText(ItemData item, int level, bool showBonusDiff, EquipmentAffixData affix = null)
    {
      if (item == null) return "";
      var sb = new System.Text.StringBuilder();

      // Sát thương (Attack)
      float baseAtk = (affix != null && affix.rolledAttack > 0f) ? affix.rolledAttack : item.BonusAttack;
      if (baseAtk > 0f)
      {
        float val = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, level);
        if (showBonusDiff && level > 0)
        {
          float prevVal = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, level - 1);
          float diff = val - prevVal;
          sb.AppendLine($"Attack (ATK): <color=#77FF77>{val:0.#} (+{diff:0.#})</color>");
        }
        else
        {
          sb.AppendLine($"Attack (ATK): <color=#FFE082>{val:0.#}</color>");
        }
      }

      // Phòng thủ (Defense)
      float baseDef = (item.ItemType == ItemType.Armor && affix != null)
        ? affix.rolledDefense
        : item.BonusDefense;
      if (baseDef > 0f)
      {
        float val = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, level);
        if (showBonusDiff && level > 0)
        {
          float prevVal = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, level - 1);
          float diff = val - prevVal;
          sb.AppendLine($"Defense (DEF): <color=#77FF77>{val:0.#} (+{diff:0.#})</color>");
        }
        else
        {
          sb.AppendLine($"Defense (DEF): <color=#81D4FA>{val:0.#}</color>");
        }
      }

      // Máu tối đa (Max Health)
      float baseHp = (item.ItemType == ItemType.Armor && affix != null)
        ? affix.rolledHealth
        : item.BonusHealth;
      if (baseHp > 0f)
      {
        float val = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, level);
        if (showBonusDiff && level > 0)
        {
          float prevVal = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, level - 1);
          float diff = val - prevVal;
          sb.AppendLine($"Max Health: <color=#77FF77>+{val:0.#} (+{diff:0.#})</color>");
        }
        else
        {
          sb.AppendLine($"Max Health: <color=#EF5350>+{val:0.#}</color>");
        }
      }

      // Thể lực tối đa (Max Stamina)
      if (item.BonusStamina > 0f)
      {
        sb.AppendLine($"Max Stamina: <color=#66BB6A>+{item.BonusStamina:0.#}</color>");
      }

      // DÒNG PHỤ THẦN BINH (SUB-STATS) TỪ AFFIXDATA
      if (affix != null && affix.subStats.Count > 0)
      {
        sb.AppendLine("\n<color=#B0BEC5>── Dòng Phụ ──</color>");
        for (int i = 0; i < affix.subStats.Count; i++)
        {
          var sub = affix.subStats[i];
          float upVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, level);
          sb.AppendLine(new SubStatEntry(sub.type, upVal).GetFormattedString());
        }
      }
      else
      {
        // Chỉ số mở rộng (Crit, Hút máu, Tiện ích)
        if (item.BonusCritRate > 0f)
        {
          float val = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritRate, level);
          sb.AppendLine($"Critical Rate: <color=#FFB74D>+{val:0.#}%</color>");
        }

        if (item.BonusCritDamage > 0f)
        {
          float val = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritDamage, level);
          sb.AppendLine($"Critical DMG: <color=#FFA726>+{val:0.#}%</color>");
        }

        if (item.BonusLifesteal > 0f)
        {
          float val = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusLifesteal, level);
          sb.AppendLine($"Lifesteal: <color=#E57373>+{val:0.#}%</color>");
        }

        if (item.SpiritCostReduction > 0f)
        {
          sb.AppendLine($"Spirit Cost: <color=#4DD0E1>-{item.SpiritCostReduction:0.#}%</color>");
        }

        if (item.StaminaCostReduction > 0f)
        {
          sb.AppendLine($"Stamina Cost: <color=#81C784>-{item.StaminaCostReduction:0.#}%</color>");
        }

        if (item.SpiritGainBonus > 0f)
        {
          sb.AppendLine($"Spirit Gain: <color=#BA68C8>+{item.SpiritGainBonus:0.#}%</color>");
        }

        if (item.MoveSpeedBonus > 0f)
        {
          sb.AppendLine($"Move Speed: <color=#FFF176>+{item.MoveSpeedBonus:0.#}%</color>");
        }
      }

      // PHÙ PHÉP THỨC TỈNH (ENCHANTMENTS)
      if (affix != null && affix.enchantments.Count > 0)
      {
        sb.AppendLine("\n<color=#CE93D8>── Phù Phép ──</color>");
        for (int i = 0; i < affix.enchantments.Count; i++)
        {
          var ench = affix.enchantments[i];
          bool isUnlockedAtThisTier = level >= ench.milestoneLevel;
          if (isUnlockedAtThisTier)
          {
            sb.AppendLine($"<color=#E040FB>[+{ench.milestoneLevel}] {ench.title}</color>");
          }
          else
          {
            if (showBonusDiff && level == ench.milestoneLevel)
            {
              sb.AppendLine($"<color=#FFD54F>[Thức tỉnh +{ench.milestoneLevel}]: {ench.title}!</color>");
            }
            else
            {
              sb.AppendLine($"<color=#757575>[+{ench.milestoneLevel}] Khóa (+{ench.milestoneLevel})</color>");
            }
          }
        }
      }

      if (sb.Length == 0)
      {
        sb.Append("<color=#B0BEC5>No bonus attributes</color>");
      }

      return sb.ToString().TrimEnd();
    }

    private string BuildForgeStatsText(ItemData item)
    {
      if (item == null) return "";
      var sb = new System.Text.StringBuilder();

      if (item.BonusAttack > 0f)
        sb.AppendLine($"Attack (ATK): <color=#FFE082>+{item.BonusAttack:0.#}</color>");
      if (item.BonusDefense > 0f)
        sb.AppendLine($"Defense (DEF): <color=#81D4FA>+{item.BonusDefense:0.#}</color>");
      if (item.BonusHealth > 0f)
        sb.AppendLine($"Max Health: <color=#EF5350>+{item.BonusHealth:0.#}</color>");
      if (item.BonusStamina > 0f)
        sb.AppendLine($"Max Stamina: <color=#66BB6A>+{item.BonusStamina:0.#}</color>");

      if (sb.Length == 0)
      {
        sb.Append("<color=#B0BEC5>No bonus attributes</color>");
      }

      return sb.ToString().TrimEnd();
    }

    private void SafeSetIcon(Image img, Sprite icon)
    {
      if (img == null) return;
      bool hasIcon = icon != null;
      img.gameObject.SetActive(hasIcon);
      if (hasIcon && img.transform.parent != null && img.transform.parent.name.Contains("Icon"))
      {
        img.transform.parent.gameObject.SetActive(true);
      }
      img.sprite = icon;
      img.enabled = hasIcon;
      img.color = Color.white;
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
      SafeSetIcon(beforeWindowUI.itemIcon, item.Icon);
      Color rColor = item.Rarity.GetRarityColor();
      if (beforeWindowUI.rarityBg != null)
      {
        beforeWindowUI.rarityBg.gameObject.SetActive(true);
        beforeWindowUI.rarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (beforeWindowUI.rarityBar != null)
      {
        beforeWindowUI.rarityBar.gameObject.SetActive(true);
        beforeWindowUI.rarityBar.color = rColor;
      }

      PopulateWindowStats(beforeWindowUI, item, curLevel, curLevel, false, heldUpgradeAffixData);

      // 2. After Window
      SafeSetIcon(afterWindowUI.itemIcon, item.Icon);
      if (afterWindowUI.rarityBg != null)
      {
        afterWindowUI.rarityBg.gameObject.SetActive(true);
        afterWindowUI.rarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (afterWindowUI.rarityBar != null)
      {
        afterWindowUI.rarityBar.gameObject.SetActive(true);
        afterWindowUI.rarityBar.color = rColor;
      }

      if (isMaxLevel)
      {
        PopulateWindowStats(afterWindowUI, item, curLevel, curLevel, true, heldUpgradeAffixData);

        if (materialsNeededText != null) materialsNeededText.text = "Materials Needed";
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
      PopulateWindowStats(afterWindowUI, item, curLevel, nextLevel, true, heldUpgradeAffixData);

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
      SafeSetIcon(iconImg, icon);
      Color rColor = rarity.GetRarityColor();
      if (bgImg != null)
      {
        bgImg.gameObject.SetActive(true);
        bgImg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (barImg != null)
      {
        barImg.gameObject.SetActive(true);
        barImg.color = rColor;
      }
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
      ClearWindowUI(beforeWindowUI, true);
      ClearWindowUI(afterWindowUI, false);

      ClearMaterialsContainer(upgradeMaterialsContainer);
      if (materialsNeededText != null) materialsNeededText.text = "Materials Needed";
      if (upgradeGoldPriceText != null)
      {
        upgradeGoldPriceText.text = "0";
        upgradeGoldPriceText.color = normalPriceColor;
      }
      if (btnUpgrade != null) btnUpgrade.interactable = false;
      if (btnUpgradeText != null)
      {
        btnUpgradeText.text = "ENHANCE";
        btnUpgradeText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
      }
      if (btnUpgradeBorder != null) btnUpgradeBorder.SetActive(false);
      if (btnUpgradeCorner != null) btnUpgradeCorner.SetActive(false);

      HideEnchantHintImmediate();
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

      // 3. Tăng cấp cường hóa của món đồ trên đe rèn
      heldUpgradeLevel++;
      selectedUpgradeLevel = heldUpgradeLevel;
      int newlyAwakenedIndex = -1;
      if (heldUpgradeAffixData != null)
      {
        heldUpgradeAffixData.UpdateEnchantmentUnlocks(heldUpgradeLevel);
        var newlyAwakened = heldUpgradeAffixData.enchantments.Find(e => e.milestoneLevel == heldUpgradeLevel);
        if (newlyAwakened != null)
        {
          newlyAwakenedIndex = heldUpgradeAffixData.enchantments.FindIndex(e => e.milestoneLevel == heldUpgradeLevel);
          Debug.Log($"<color=magenta>[Blacksmith]</color> [THỨC TỈNH PHÙ PHÉP MỐC +{heldUpgradeLevel}]: {newlyAwakened.title}!");
        }
      }

      // 4. Hoạt ảnh thức tỉnh phù phép & Punch Scale Animation
      if (afterWindow != null)
      {
        StartCoroutine(PunchRoutine(afterWindow, 1.1f, 0.15f));
      }
      if (beforeWindow != null)
      {
        StartCoroutine(PunchRoutine(beforeWindow, 1.05f, 0.15f));
      }

      // 5. Làm mới UI
      UpdateUpgradeUI();

      if (newlyAwakenedIndex >= 0 && newlyAwakenedIndex < beforeWindowUI.enchantRows.Count)
      {
        StartCoroutine(AwakenFlashRoutine(beforeWindowUI.enchantRows[newlyAwakenedIndex]));
      }

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
      SafeSetIcon(forgeSlotIcon, item.Icon);
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
      PopulateForgePreviewWindow(forgeWindowUI, item);
      if (forgeItemStats != null)
      {
        forgeItemStats.text = BuildForgeStatsText(item);
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
      if (forgeSlotIcon != null)
      {
        forgeSlotIcon.gameObject.SetActive(false);
        forgeSlotIcon.enabled = false;
      }
      if (forgeSlotRarityBg != null) forgeSlotRarityBg.gameObject.SetActive(false);
      if (forgeSlotRarityBar != null) forgeSlotRarityBar.gameObject.SetActive(false);
      ClearWindowUI(forgeWindowUI, true);
      if (forgeItemName != null) forgeItemName.text = "Select a Recipe";
      if (forgeItemStats != null) forgeItemStats.text = "Select a divine armament recipe from the list.";
      ClearMaterialsContainer(forgeMaterialsContainer);
      if (forgeGoldPriceText != null) forgeGoldPriceText.text = "0";
      if (btnForge != null) btnForge.interactable = false;
      if (btnForgeBorder != null) btnForgeBorder.SetActive(false);
      if (btnForgeCorner != null) btnForgeCorner.SetActive(false);
      if (btnForgeText != null)
      {
        btnForgeText.text = "FORGE";
        btnForgeText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
      }
      HideEnchantHintImmediate();
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

    private void HandleForgeSlotClicked()
    {
      if (selectedRecipeSlot == null || selectedRecipeSlot.Recipe == null) return;
      ItemData item = selectedRecipeSlot.Recipe.ResultItem;
      if (item == null) return;

      if (itemDetailPopup != null)
      {
        RectTransform targetRect = btnForgeSlot != null ? btnForgeSlot.transform as RectTransform : null;
        itemDetailPopup.ShowPreview(item, targetRect);
      }
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
      HideEnchantHintImmediate();
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

    /// <summary>
    /// Nhấp chuột phải vào ô đồ trong túi bên phải khi đang mở Lò Rèn:
    /// Nhanh chóng nạp trang bị (Vũ khí / Giáp) vào đe rèn để cường hóa (tự động hoàn trả trang bị cũ nếu có)
    /// </summary>
    private void HandleBagSlotRightClicked(InventorySlotUI slot)
    {
      if (slot == null || slot.IsEmpty || slot.CurrentItem == null) return;

      ItemData item = slot.CurrentItem;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor)
      {
        Debug.Log($"[Blacksmith] Vật phẩm {item.ItemName} không phải Vũ khí hay Giáp, không thể cường hóa!");
        return;
      }

      int bagIndex = slot.ActualBagIndex;
      if (bagIndex < 0) return;

      // Nạp trang bị vào đe rèn
      PutItemIntoUpgrade(bagIndex);

      // Tự động chuyển về tab Cường Hóa nếu đang ở tab Forge
      if (currentBlacksmithTab != BlacksmithTab.Upgrade)
      {
        SwitchBlacksmithTab(BlacksmithTab.Upgrade);
      }

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }
    }

    private void HandlePutInFromBag(InventorySlotUI slot, ItemData item)
    {
      if (slot == null || item == null) return;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor) return;

      // Nạp món đồ vào đe rèn để cường hóa (tạm thời nhấc khỏi túi)
      PutItemIntoUpgrade(slot.ActualBagIndex);

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

    // =========================================================================
    // HỆ THỐNG HIỂN THỊ BEFORE / AFTER WINDOWS & ENCHANT HINT (TASK RPG-11)
    // =========================================================================

    private void PopulateWindowStats(BlacksmithUpgradeWindowUI winUI, ItemData item, int curLevel, int targetLevel, bool isAfter, EquipmentAffixData affix)
    {
      if (winUI == null || item == null) return;

      bool isMaxLevel = isAfter && curLevel >= BlacksmithUpgradeConfig.MAX_UPGRADE_LEVEL;

      // 1. Upgrade Level indicator (+1, +3, +5... hoặc MAX)
      if (winUI.upgradeLevelText != null)
      {
        winUI.upgradeLevelText.gameObject.SetActive(true);
        if (isMaxLevel)
        {
          winUI.upgradeLevelText.text = "<color=#FFD700>MAX</color>";
        }
        else if (targetLevel > 0)
        {
          winUI.upgradeLevelText.text = $"+{targetLevel}";
        }
        else
        {
          winUI.upgradeLevelText.text = "+0";
        }
      }

      // 2. Tên vật phẩm (giữ nguyên tên cùng gradient như before)
      if (winUI.itemName != null)
      {
        winUI.itemName.text = item.Rarity.FormatWithGradient(item.ItemName);
      }

      // 3. Chỉ số chính (Main Stats)
      if (winUI.mainStatsRoot != null)
      {
        winUI.mainStatsRoot.SetActive(true);

        if (item.ItemType == ItemType.Weapon)
        {
          float baseAtk = (affix != null && affix.rolledAttack > 0f) ? affix.rolledAttack : item.BonusAttack;
          if (baseAtk <= 0f) baseAtk = 10f;
          float curAtk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, curLevel);
          float targetAtk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, targetLevel);
          float diffAtk = targetAtk - curAtk;

          if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Attack";
          if (winUI.mainStatValue != null)
          {
            if (isAfter && !isMaxLevel && diffAtk > 0.05f)
              winUI.mainStatValue.text = $"{targetAtk:0.#} <color=#55FF55>(+{diffAtk:0.#})</color>";
            else
              winUI.mainStatValue.text = $"{targetAtk:0.#}";
          }
        }
        else // Armor
        {
          bool isDefense = (affix != null) ? (affix.rolledDefense > 0f) : (item.BonusDefense > 0f);

          if (isDefense)
          {
            float baseDef = (affix != null && affix.rolledDefense > 0f) ? affix.rolledDefense : item.BonusDefense;
            float curDef = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, curLevel);
            float targetDef = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, targetLevel);
            float diffDef = targetDef - curDef;

            if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Defense";
            if (winUI.mainStatValue != null)
            {
              if (isAfter && !isMaxLevel && diffDef > 0.05f)
                winUI.mainStatValue.text = $"{targetDef:0.#} <color=#55FF55>(+{diffDef:0.#})</color>";
              else
                winUI.mainStatValue.text = $"{targetDef:0.#}";
            }
          }
          else
          {
            float baseHp = (affix != null && affix.rolledHealth > 0f) ? affix.rolledHealth : item.BonusHealth;
            float curHp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, curLevel);
            float targetHp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, targetLevel);
            float diffHp = targetHp - curHp;

            if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Max Health";
            if (winUI.mainStatValue != null)
            {
              if (isAfter && !isMaxLevel && diffHp > 0.05f)
                winUI.mainStatValue.text = $"{targetHp:0.#} <color=#55FF55>(+{diffHp:0.#})</color>";
              else
                winUI.mainStatValue.text = $"{targetHp:0.#}";
            }
          }
        }
      }

      // 4. Các dòng chỉ số phụ (Sub Stats - tối đa 4, 100% Procedural Affixes)
      var subList = new List<(string name, string valStr)>();

      if (affix != null && affix.subStats != null && affix.subStats.Count > 0)
      {
        for (int i = 0; i < affix.subStats.Count && subList.Count < 4; i++)
        {
          var sub = affix.subStats[i];
          float curVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, curLevel);
          float targetVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, targetLevel);
          float diffVal = targetVal - curVal;
          var (sName, sValStr) = ItemDetailPopupUI.FormatSubStat(sub.type, targetVal);
          bool isPercent = sub.type != SubStatType.AttackBonus &&
                           sub.type != SubStatType.DefenseBonus &&
                           sub.type != SubStatType.HealthBonus &&
                           sub.type != SubStatType.MaxStamina;
          string diffUnit = isPercent ? "%" : "";
          if (isAfter && !isMaxLevel && diffVal > 0.05f)
          {
            sValStr = $"{sValStr} <color=#55FF55>(+{diffVal:0.#}{diffUnit})</color>";
          }
          subList.Add((sName, sValStr));
        }
      }

      if (affix == null || affix.subStats == null || affix.subStats.Count == 0)
      {
        // Static fallback nếu không có procedural affixes
        if (item.BonusCritRate > 0f && subList.Count < 4)
        {
          float curVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritRate, curLevel);
          float targetVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritRate, targetLevel);
          float diffVal = targetVal - curVal;
          string valStr = isAfter && !isMaxLevel && diffVal > 0.05f ? $"+ {targetVal:0.#}% <color=#55FF55>(+{diffVal:0.#}%)</color>" : $"+ {targetVal:0.#}%";
          subList.Add(("Critical Rate", valStr));
        }
        if (item.BonusCritDamage > 0f && subList.Count < 4)
        {
          float curVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritDamage, curLevel);
          float targetVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritDamage, targetLevel);
          float diffVal = targetVal - curVal;
          string valStr = isAfter && !isMaxLevel && diffVal > 0.05f ? $"+ {targetVal:0.#}% <color=#55FF55>(+{diffVal:0.#}%)</color>" : $"+ {targetVal:0.#}%";
          subList.Add(("Critical Damage", valStr));
        }
        if (item.BonusLifesteal > 0f && subList.Count < 4)
        {
          float curVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusLifesteal, curLevel);
          float targetVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusLifesteal, targetLevel);
          float diffVal = targetVal - curVal;
          string valStr = isAfter && !isMaxLevel && diffVal > 0.05f ? $"+ {targetVal:0.#}% <color=#55FF55>(+{diffVal:0.#}%)</color>" : $"+ {targetVal:0.#}%";
          subList.Add(("Lifesteal", valStr));
        }
        if (item.SpiritCostReduction > 0f && subList.Count < 4) subList.Add(("Spirit Cost", $"- {item.SpiritCostReduction:0.#}%"));
        if (item.StaminaCostReduction > 0f && subList.Count < 4) subList.Add(("Stamina Cost", $"- {item.StaminaCostReduction:0.#}%"));
        if (item.SpiritGainBonus > 0f && subList.Count < 4) subList.Add(("Spirit Gain", $"+ {item.SpiritGainBonus:0.#}%"));
        if (item.MoveSpeedBonus > 0f && subList.Count < 4) subList.Add(("Movement Speed", $"+ {item.MoveSpeedBonus:0.#}%"));
      }

      int activeSubCount = Mathf.Min(subList.Count, 4);
      if (activeSubCount > 0)
      {
        if (winUI.subStatsDivider != null) winUI.subStatsDivider.SetActive(true);
        if (winUI.subStatsLayoutRoot != null) winUI.subStatsLayoutRoot.gameObject.SetActive(true);

        for (int i = 0; i < winUI.subStatRows.Count; i++)
        {
          if (winUI.subStatRows[i] == null || winUI.subStatRows[i].root == null) continue;
          if (i < activeSubCount)
          {
            winUI.subStatRows[i].root.SetActive(true);
            if (winUI.subStatRows[i].labelText != null) winUI.subStatRows[i].labelText.text = subList[i].name;
            if (winUI.subStatRows[i].valueText != null) winUI.subStatRows[i].valueText.text = subList[i].valStr;
          }
          else
          {
            winUI.subStatRows[i].root.SetActive(false);
          }
        }
      }
      else
      {
        if (winUI.subStatsDivider != null) winUI.subStatsDivider.SetActive(false);
        if (winUI.subStatsLayoutRoot != null) winUI.subStatsLayoutRoot.gameObject.SetActive(false);
        for (int i = 0; i < winUI.subStatRows.Count; i++)
        {
          if (winUI.subStatRows[i]?.root != null) winUI.subStatRows[i].root.SetActive(false);
        }
      }

      // 5. Phù phép thức tỉnh (Enchantments - tối đa 3)
      var enchantList = (affix != null && affix.enchantments != null) ? affix.enchantments : null;
      int activeEnchCount = (enchantList != null) ? Mathf.Min(enchantList.Count, 3) : 0;

      if (activeEnchCount > 0)
      {
        if (winUI.itemEnchantmentContainer != null) winUI.itemEnchantmentContainer.gameObject.SetActive(true);
        if (winUI.enchantmentDivider != null) winUI.enchantmentDivider.SetActive(true);
        if (winUI.enchantmentLayoutRoot != null) winUI.enchantmentLayoutRoot.gameObject.SetActive(true);

        for (int i = 0; i < winUI.enchantRows.Count; i++)
        {
          if (winUI.enchantRows[i] == null || winUI.enchantRows[i].root == null) continue;
          if (i < activeEnchCount)
          {
            var ench = enchantList[i];
            bool curActive = curLevel >= ench.milestoneLevel;
            bool targetActive = isMaxLevel ? true : (targetLevel >= ench.milestoneLevel);
            bool willAwaken = isAfter && !isMaxLevel && (targetLevel >= ench.milestoneLevel) && !curActive;

            winUI.activeEnchantmentsCache[i] = ench;
            winUI.enchantRows[i].root.SetActive(true);

            if (winUI.enchantRows[i].iconActive != null) winUI.enchantRows[i].iconActive.gameObject.SetActive(targetActive);
            if (winUI.enchantRows[i].iconUnactive != null) winUI.enchantRows[i].iconUnactive.gameObject.SetActive(!targetActive);

            var (eTitle, _) = ItemDetailPopupUI.GetEnchantmentEnglishInfo(ench.enchantType, ench.title, ench.description);

            if (winUI.enchantRows[i].text != null)
            {
              if (willAwaken)
              {
                winUI.enchantRows[i].text.text = $"<color=#FFD700><b>[+{ench.milestoneLevel}] {eTitle}</b></color>";
              }
              else if (targetActive)
              {
                winUI.enchantRows[i].text.text = $"<color=#E040FB>[+{ench.milestoneLevel}] {eTitle}</color>";
              }
              else
              {
                string tag = isAfter ? $"Requires +{ench.milestoneLevel}" : "Locked";
                winUI.enchantRows[i].text.text = $"<color=#757575>[+{ench.milestoneLevel}] {eTitle} <size=80%>({tag})</size></color>";
              }
            }
          }
          else
          {
            winUI.enchantRows[i].root.SetActive(false);
            winUI.activeEnchantmentsCache[i] = null;
          }
        }
      }
      else
      {
        if (winUI.itemEnchantmentContainer != null) winUI.itemEnchantmentContainer.gameObject.SetActive(false);
        if (winUI.enchantmentDivider != null) winUI.enchantmentDivider.SetActive(false);
        if (winUI.enchantmentLayoutRoot != null) winUI.enchantmentLayoutRoot.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++) winUI.activeEnchantmentsCache[i] = null;
        for (int i = 0; i < winUI.enchantRows.Count; i++)
        {
          if (winUI.enchantRows[i]?.root != null) winUI.enchantRows[i].root.SetActive(false);
        }
      }

      // 6. Fallback legacy text
      if (winUI.fallbackItemStats != null)
      {
        winUI.fallbackItemStats.text = isMaxLevel
          ? "<color=#FFD700>Weapon at peak power (MAX Tier)!</color>"
          : BuildUpgradeStatsText(item, targetLevel, isAfter, affix);
      }
    }

    /// <summary>
    /// Hiển thị thông số gợi ý (Range x - y, Substats có thể có, Enchantments mốc thức tỉnh) trong cửa sổ Rèn (Forge)
    /// </summary>
    private void PopulateForgePreviewWindow(BlacksmithUpgradeWindowUI winUI, ItemData item)
    {
      if (winUI == null || item == null) return;

      ItemRarity rarity = item.Rarity;
      float varianceMin = 0.92f;
      float varianceMax = 1.08f;
      switch (rarity)
      {
        case ItemRarity.Common:
          varianceMin = 0.92f; varianceMax = 1.08f;
          break;
        case ItemRarity.Uncommon:
          varianceMin = 0.94f; varianceMax = 1.12f;
          break;
        case ItemRarity.Rare:
          varianceMin = 0.96f; varianceMax = 1.16f;
          break;
        case ItemRarity.Epic:
          varianceMin = 1.00f; varianceMax = 1.20f;
          break;
        case ItemRarity.Legendary:
          varianceMin = 1.05f; varianceMax = 1.25f;
          break;
      }

      // 1. Level Text (Cửa sổ rèn không cần số cấp)
      if (winUI.upgradeLevelText != null)
      {
        winUI.upgradeLevelText.gameObject.SetActive(false);
      }

      // 2. Chỉ số chính (Main Stats: Attack x - y hoặc DEF / HP x-y / x-y)
      if (winUI.mainStatsRoot != null)
      {
        winUI.mainStatsRoot.SetActive(true);

        if (item.ItemType == ItemType.Weapon)
        {
          float baseAtk = item.BonusAttack > 0f ? item.BonusAttack : 10f;
          float minAtk = Mathf.Max(1f, Mathf.Round(baseAtk * varianceMin * 10f) / 10f);
          float maxAtk = Mathf.Max(1f, Mathf.Round(baseAtk * varianceMax * 10f) / 10f);

          if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Attack";
          if (winUI.mainStatValue != null) winUI.mainStatValue.text = $"{minAtk:0.#} - {maxAtk:0.#}";
        }
        else // Armor
        {
          bool hasDef = item.BonusDefense > 0f;
          bool hasHp = item.BonusHealth > 0f;

          if (hasDef && hasHp)
          {
            float minDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varianceMin * 10f) / 10f);
            float maxDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varianceMax * 10f) / 10f);
            float minHp = Mathf.Max(5f, Mathf.Round(item.BonusHealth * varianceMin * 10f) / 10f);
            float maxHp = Mathf.Max(5f, Mathf.Round(item.BonusHealth * varianceMax * 10f) / 10f);

            if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "DEF or HP";
            if (winUI.mainStatValue != null) winUI.mainStatValue.text = $"{minDef:0.#}-{maxDef:0.#} | {minHp:0.#}-{maxHp:0.#}";
          }
          else if (hasDef)
          {
            float minDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varianceMin * 10f) / 10f);
            float maxDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varianceMax * 10f) / 10f);

            if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Defense";
            if (winUI.mainStatValue != null) winUI.mainStatValue.text = $"{minDef:0.#} - {maxDef:0.#}";
          }
          else
          {
            float baseHp = hasHp ? item.BonusHealth : 20f;
            float minHp = Mathf.Max(5f, Mathf.Round(baseHp * varianceMin * 10f) / 10f);
            float maxHp = Mathf.Max(5f, Mathf.Round(baseHp * varianceMax * 10f) / 10f);

            if (winUI.mainStatLabel != null) winUI.mainStatLabel.text = "Max Health";
            if (winUI.mainStatValue != null) winUI.mainStatValue.text = $"{minHp:0.#} - {maxHp:0.#}";
          }
        }
      }

      // 3. Substats có thể có theo Rarity (Substat 1, 2, 3...)
      int maxSubCount = 0;
      switch (rarity)
      {
        case ItemRarity.Common: maxSubCount = 1; break;
        case ItemRarity.Uncommon: maxSubCount = 2; break;
        case ItemRarity.Rare: maxSubCount = 2; break;
        case ItemRarity.Epic: maxSubCount = 3; break;
        case ItemRarity.Legendary: maxSubCount = 4; break;
      }

      if (maxSubCount > 0)
      {
        if (winUI.subStatsDivider != null) winUI.subStatsDivider.SetActive(true);
        if (winUI.subStatsLayoutRoot != null) winUI.subStatsLayoutRoot.gameObject.SetActive(true);

        for (int i = 0; i < winUI.subStatRows.Count; i++)
        {
          if (winUI.subStatRows[i] == null || winUI.subStatRows[i].root == null) continue;
          if (i < maxSubCount)
          {
            winUI.subStatRows[i].root.SetActive(true);
            if (winUI.subStatRows[i].labelText != null) winUI.subStatRows[i].labelText.text = $"Substat {i + 1}";
            if (winUI.subStatRows[i].valueText != null) winUI.subStatRows[i].valueText.text = "<color=#90A4AE>Random</color>";
          }
          else
          {
            winUI.subStatRows[i].root.SetActive(false);
          }
        }
      }
      else
      {
        if (winUI.subStatsDivider != null) winUI.subStatsDivider.SetActive(false);
        if (winUI.subStatsLayoutRoot != null) winUI.subStatsLayoutRoot.gameObject.SetActive(false);
        for (int i = 0; i < winUI.subStatRows.Count; i++)
        {
          if (winUI.subStatRows[i]?.root != null) winUI.subStatRows[i].root.SetActive(false);
        }
      }

      // 4. Các ô Phù Phép (Enchantments) theo Rarity
      var milestones = new List<int>();
      if (rarity == ItemRarity.Common || rarity == ItemRarity.Uncommon)
      {
        milestones.Add(1);
      }
      else if (rarity == ItemRarity.Rare)
      {
        milestones.Add(1);
        milestones.Add(3);
      }
      else // Epic & Legendary
      {
        milestones.Add(1);
        milestones.Add(3);
        milestones.Add(5);
      }

      int maxEnchCount = milestones.Count;
      if (maxEnchCount > 0)
      {
        if (winUI.itemEnchantmentContainer != null) winUI.itemEnchantmentContainer.gameObject.SetActive(true);
        if (winUI.enchantmentDivider != null) winUI.enchantmentDivider.SetActive(true);
        if (winUI.enchantmentLayoutRoot != null) winUI.enchantmentLayoutRoot.gameObject.SetActive(true);

        for (int i = 0; i < winUI.enchantRows.Count; i++)
        {
          if (winUI.enchantRows[i] == null || winUI.enchantRows[i].root == null) continue;
          if (i < maxEnchCount)
          {
            int msLevel = milestones[i];
            winUI.enchantRows[i].root.SetActive(true);
            if (winUI.enchantRows[i].iconActive != null) winUI.enchantRows[i].iconActive.gameObject.SetActive(false);
            if (winUI.enchantRows[i].iconUnactive != null) winUI.enchantRows[i].iconUnactive.gameObject.SetActive(true);
            if (winUI.enchantRows[i].text != null)
            {
              winUI.enchantRows[i].text.text = $"<color=#757575>[+{msLevel}] Random Enchantment</color>";
            }

            // Gắn cache để hover vẫn hiện EnchantHint giải thích
            winUI.activeEnchantmentsCache[i] = new EnchantmentEntry
            {
              enchantType = EnchantmentType.None,
              milestoneLevel = msLevel,
              title = "Random Enchantment",
              description = $"Unlocks a random passive perk upon enhancing this armament to +{msLevel} at the Blacksmith."
            };
          }
          else
          {
            winUI.enchantRows[i].root.SetActive(false);
            winUI.activeEnchantmentsCache[i] = null;
          }
        }
      }
      else
      {
        if (winUI.itemEnchantmentContainer != null) winUI.itemEnchantmentContainer.gameObject.SetActive(false);
        if (winUI.enchantmentDivider != null) winUI.enchantmentDivider.SetActive(false);
        if (winUI.enchantmentLayoutRoot != null) winUI.enchantmentLayoutRoot.gameObject.SetActive(false);
        for (int i = 0; i < winUI.enchantRows.Count; i++)
        {
          if (winUI.enchantRows[i]?.root != null) winUI.enchantRows[i].root.SetActive(false);
          winUI.activeEnchantmentsCache[i] = null;
        }
      }
    }

    private void ClearWindowUI(BlacksmithUpgradeWindowUI winUI, bool isBefore)
    {
      if (winUI == null) return;

      if (winUI.itemIcon != null)
      {
        winUI.itemIcon.gameObject.SetActive(false);
        winUI.itemIcon.enabled = false;
      }
      if (winUI.rarityBg != null) winUI.rarityBg.gameObject.SetActive(false);
      if (winUI.rarityBar != null) winUI.rarityBar.gameObject.SetActive(false);

      if (winUI.itemName != null) winUI.itemName.text = isBefore ? "Select equipment" : "Next Tier";
      if (winUI.upgradeLevelText != null) winUI.upgradeLevelText.text = "";

      if (winUI.mainStatsRoot != null) winUI.mainStatsRoot.SetActive(false);
      if (winUI.subStatsDivider != null) winUI.subStatsDivider.SetActive(false);
      if (winUI.subStatsLayoutRoot != null) winUI.subStatsLayoutRoot.gameObject.SetActive(false);
      if (winUI.itemEnchantmentContainer != null) winUI.itemEnchantmentContainer.gameObject.SetActive(false);
      if (winUI.enchantmentDivider != null) winUI.enchantmentDivider.SetActive(false);
      if (winUI.enchantmentLayoutRoot != null) winUI.enchantmentLayoutRoot.gameObject.SetActive(false);

      for (int i = 0; i < winUI.subStatRows.Count; i++)
      {
        if (winUI.subStatRows[i]?.root != null) winUI.subStatRows[i].root.SetActive(false);
      }
      for (int i = 0; i < winUI.enchantRows.Count; i++)
      {
        if (winUI.enchantRows[i]?.root != null) winUI.enchantRows[i].root.SetActive(false);
      }
      for (int i = 0; i < 3; i++) winUI.activeEnchantmentsCache[i] = null;

      if (winUI.fallbackItemStats != null)
      {
        winUI.fallbackItemStats.text = isBefore
          ? "<color=#90A4AE>Select a weapon or armor from your bag & click \"Put in\".</color>"
          : "<color=#90A4AE>View stats increase and materials needed.</color>";
      }
    }

    private void SetupEnchantRowHoverTriggers(BlacksmithUpgradeWindowUI winUI)
    {
      if (winUI == null || winUI.enchantRows == null) return;

      for (int i = 0; i < winUI.enchantRows.Count; i++)
      {
        var row = winUI.enchantRows[i];
        if (row == null || row.root == null) continue;

        var graphic = row.root.GetComponent<Graphic>();
        if (graphic == null)
        {
          var img = row.root.AddComponent<Image>();
          img.color = new Color(0f, 0f, 0f, 0f);
          img.raycastTarget = true;
        }
        else
        {
          graphic.raycastTarget = true;
        }

        var trigger = row.root.GetComponent<EnchantRowHoverTrigger>() ?? row.root.AddComponent<EnchantRowHoverTrigger>();
        int rowIndex = i;
        trigger.rowIndex = rowIndex;
        trigger.onHoverEnter = (rIdx) => ShowEnchantHint(winUI, rIdx);
        trigger.onHoverExit = (rIdx) => HideEnchantHint();
        row.hoverTrigger = trigger;
      }
    }

    private void ShowEnchantHint(BlacksmithUpgradeWindowUI winUI, int rowIndex)
    {
      if (winUI == null || rowIndex < 0 || rowIndex >= 3) return;
      EnchantmentEntry ench = winUI.activeEnchantmentsCache[rowIndex];
      if (ench == null || enchantHintCanvasGroup == null || enchantHintText == null) return;

      var (eTitle, eDesc) = ItemDetailPopupUI.GetEnchantmentEnglishInfo(ench.enchantType, ench.title, ench.description);

      // Định dạng chuẩn "Tên: Mô tả" (không hiện [+5] hay [Requires+5])
      string hintText = ench.isUnlocked
        ? $"<color=#E040FB>{eTitle}:</color> <color=#EEEEEE>{eDesc}</color>"
        : $"<color=#B0BEC5>{eTitle}:</color> <color=#9E9E9E>{eDesc}</color>";

      enchantHintText.text = hintText;
      enchantHintText.ForceMeshUpdate();

      float preferredW = enchantHintText.preferredWidth;
      float targetW = Mathf.Clamp(preferredW + 28f, 160f, 360f);
      float preferredH = enchantHintText.GetPreferredValues(hintText, targetW - 20f, 0f).y;
      float targetH = Mathf.Max(36f, preferredH + 14f);

      if (enchantHintRect != null)
      {
        if (!enchantHintRect.gameObject.activeSelf)
        {
          enchantHintRect.gameObject.SetActive(true);
        }

        enchantHintRect.SetAsLastSibling();
        enchantHintRect.sizeDelta = new Vector2(targetW, targetH);
        enchantHintRect.pivot = new Vector2(0f, 1f); // Top-Left

        RectTransform parentRect = enchantHintRect.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mouseScreenPos, uiCam, out Vector2 mouseLocalPos);

        float targetY = mouseLocalPos.y;
        if (rowIndex < winUI.enchantRows.Count && winUI.enchantRows[rowIndex]?.root != null)
        {
          Vector3 rowWorldCenter = winUI.enchantRows[rowIndex].root.transform.position;
          Vector3 rowLocalPos = parentRect.InverseTransformPoint(rowWorldCenter);
          RectTransform rRect = winUI.enchantRows[rowIndex].root.transform as RectTransform;
          float rowHalfH = (rRect != null) ? rRect.rect.height * 0.5f : 20f;
          targetY = rowLocalPos.y + rowHalfH;
        }

        float targetX = mouseLocalPos.x;
        float halfParentW = parentRect != null ? parentRect.rect.width * 0.5f : 500f;
        float halfParentH = parentRect != null ? parentRect.rect.height * 0.5f : 400f;

        if (targetX + targetW > halfParentW - 10f)
        {
          targetX = halfParentW - 10f - targetW;
        }
        if (targetX < -halfParentW + 10f)
        {
          targetX = -halfParentW + 10f;
        }

        if (targetY - targetH < -halfParentH + 10f)
        {
          targetY = -halfParentH + 10f + targetH;
        }
        if (targetY > halfParentH - 10f)
        {
          targetY = halfParentH - 10f;
        }

        enchantHintRect.anchoredPosition = new Vector2(targetX, targetY);
      }

      if (hintFadeRoutine != null) StopCoroutine(hintFadeRoutine);
      hintFadeRoutine = StartCoroutine(FadeHintRoutine(1f, 0.08f));
    }

    private void HideEnchantHint()
    {
      if (enchantHintCanvasGroup == null) return;
      if (hintFadeRoutine != null) StopCoroutine(hintFadeRoutine);
      hintFadeRoutine = StartCoroutine(FadeHintRoutine(0f, 0.06f));
    }

    private void HideEnchantHintImmediate()
    {
      if (hintFadeRoutine != null)
      {
        StopCoroutine(hintFadeRoutine);
        hintFadeRoutine = null;
      }
      if (enchantHintCanvasGroup != null)
      {
        enchantHintCanvasGroup.alpha = 0f;
        enchantHintCanvasGroup.blocksRaycasts = false;
      }
      if (enchantHintRect != null)
      {
        enchantHintRect.gameObject.SetActive(false);
      }
    }

    private IEnumerator FadeHintRoutine(float targetAlpha, float duration)
    {
      if (enchantHintCanvasGroup == null) yield break;
      float startAlpha = enchantHintCanvasGroup.alpha;
      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        enchantHintCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
        yield return null;
      }
      enchantHintCanvasGroup.alpha = targetAlpha;
      if (targetAlpha <= 0.01f && enchantHintRect != null)
      {
        enchantHintRect.gameObject.SetActive(false);
      }
    }

    private IEnumerator AwakenFlashRoutine(EnchantRowUI row)
    {
      if (row == null || row.root == null) yield break;

      Transform tr = row.root.transform;
      Vector3 originalScale = Vector3.one;

      float duration = 0.5f;
      float elapsed = 0f;
      tr.localScale = Vector3.one * 1.35f;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        tr.localScale = Vector3.Lerp(Vector3.one * 1.35f, originalScale, t);

        if (row.iconActive != null)
        {
          float pingPong = Mathf.PingPong(elapsed * 8f, 1f);
          row.iconActive.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.2f), pingPong);
        }
        yield return null;
      }

      tr.localScale = originalScale;
      if (row.iconActive != null) row.iconActive.color = Color.white;
    }
  }

  /// <summary>
  /// Cấu trúc hiển thị thông số chi tiết (MainStats, SubStats, Enchantments) cho một cửa sổ Before/After trong Lò Rèn
  /// </summary>
  [Serializable]
  public class BlacksmithUpgradeWindowUI
  {
    public Transform windowRoot;
    public Image itemIcon;
    public Image rarityBg;
    public Image rarityBar;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI upgradeLevelText;

    // Cấu trúc MainStats (ItemStatsText/MainStats)
    public GameObject mainStatsRoot;
    public TextMeshProUGUI mainStatLabel;
    public TextMeshProUGUI mainStatValue;

    // Cấu trúc SubStats (ItemStatsText/SubStatsLayout)
    public GameObject subStatsDivider; // Line (2)
    public RectTransform subStatsLayoutRoot;
    public List<SubStatRowUI> subStatRows = new List<SubStatRowUI>();

    // Cấu trúc Phù Phép (ItemEnchantment / EnchantmentLayout)
    public RectTransform itemEnchantmentContainer;
    public GameObject enchantmentDivider; // Line (3)
    public RectTransform enchantmentLayoutRoot;
    public List<EnchantRowUI> enchantRows = new List<EnchantRowUI>();

    // Fallback cũ nếu có
    public TextMeshProUGUI fallbackItemStats;

    [NonSerialized] public EnchantmentEntry[] activeEnchantmentsCache = new EnchantmentEntry[3];

    public void AutoFind(Transform root)
    {
      windowRoot = root;
      if (windowRoot == null) return;

      Transform slot = windowRoot.Find("UpgradeSlot") ?? windowRoot.Find("ForgeSlot");
      if (slot != null)
      {
        if (itemIcon == null) itemIcon = slot.Find("ItemIcon")?.GetComponent<Image>();
        if (rarityBg == null) rarityBg = slot.Find("RarityBG")?.GetComponent<Image>();
        if (rarityBar == null) rarityBar = slot.Find("RarityBar")?.GetComponent<Image>();
      }

      if (itemName == null) itemName = windowRoot.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
      if (upgradeLevelText == null) upgradeLevelText = windowRoot.Find("UpgradeLevel")?.GetComponent<TextMeshProUGUI>();
      if (fallbackItemStats == null) fallbackItemStats = windowRoot.Find("ItemStats")?.GetComponent<TextMeshProUGUI>();

      // MainStats: tìm trong ItemStatsText hoặc trực tiếp dưới windowRoot
      Transform statsTextContainer = windowRoot.Find("ItemStatsText");
      Transform msSearchRoot = statsTextContainer != null ? statsTextContainer : windowRoot;

      Transform msTr = msSearchRoot.Find("MainStats");
      if (msTr != null)
      {
        mainStatsRoot = msTr.gameObject;
        if (mainStatLabel == null) mainStatLabel = msTr.Find("Text")?.GetComponent<TextMeshProUGUI>() ?? msTr.GetComponent<TextMeshProUGUI>();
        if (mainStatValue == null) mainStatValue = msTr.Find("Value")?.GetComponent<TextMeshProUGUI>();
      }

      // SubStats
      if (subStatsDivider == null)
      {
        subStatsDivider = msSearchRoot.Find("Line (2)")?.gameObject ?? windowRoot.Find("Line (2)")?.gameObject;
      }

      Transform subLayoutTr = msSearchRoot.Find("SubStatsLayout") ?? windowRoot.Find("SubStatsLayout");
      if (subLayoutTr != null)
      {
        subStatsLayoutRoot = subLayoutTr as RectTransform;
        if (subStatRows == null) subStatRows = new List<SubStatRowUI>();
        subStatRows.Clear();

        string[] subNames = new string[] { "SubStats", "SubStats (1)", "SubStats (2)", "SubStats (3)" };
        for (int i = 0; i < subNames.Length; i++)
        {
          Transform t = subLayoutTr.Find(subNames[i]);
          if (t == null && i < subLayoutTr.childCount) t = subLayoutTr.GetChild(i);
          if (t != null)
          {
            var valTmp = t.Find("Value")?.GetComponent<TextMeshProUGUI>();
            var lblTmp = t.Find("Text")?.GetComponent<TextMeshProUGUI>() ?? t.GetComponent<TextMeshProUGUI>();
            if (lblTmp == valTmp && lblTmp != null)
            {
              var allTmps = t.GetComponentsInChildren<TextMeshProUGUI>(true);
              foreach (var tmp in allTmps)
              {
                if (tmp != valTmp) { lblTmp = tmp; break; }
              }
            }
            var row = new SubStatRowUI
            {
              root = t.gameObject,
              labelText = lblTmp,
              valueText = valTmp
            };
            subStatRows.Add(row);
          }
        }
      }

      // Enchantments
      Transform enchContainer = windowRoot.Find("ItemEnchantment") ?? windowRoot.Find("ItemEnchantmentContainer");
      if (enchContainer != null) itemEnchantmentContainer = enchContainer as RectTransform;

      Transform enchSearchRoot = itemEnchantmentContainer != null ? itemEnchantmentContainer.transform : windowRoot;
      if (enchantmentDivider == null)
      {
        enchantmentDivider = enchSearchRoot.Find("Line (3)")?.gameObject ?? windowRoot.Find("Line (3)")?.gameObject;
      }

      Transform enchLayoutTr = enchSearchRoot.Find("EnchantmentLayout") ?? windowRoot.Find("EnchantmentLayout");
      if (enchLayoutTr != null)
      {
        enchantmentLayoutRoot = enchLayoutTr as RectTransform;
        if (enchantRows == null) enchantRows = new List<EnchantRowUI>();
        enchantRows.Clear();

        string[] enchNames = new string[] { "Enchant", "Enchant (1)", "Enchant (2)" };
        for (int i = 0; i < enchNames.Length; i++)
        {
          Transform t = enchLayoutTr.Find(enchNames[i]);
          if (t == null && i < enchLayoutTr.childCount) t = enchLayoutTr.GetChild(i);
          if (t != null)
          {
            var row = new EnchantRowUI
            {
              root = t.gameObject,
              iconActive = t.Find("IconActive")?.GetComponent<Image>(),
              iconUnactive = t.Find("IconUnactive")?.GetComponent<Image>(),
              text = t.Find("EnchantmentText")?.GetComponent<TextMeshProUGUI>() ?? t.Find("Text")?.GetComponent<TextMeshProUGUI>()
            };
            enchantRows.Add(row);
          }
        }
      }
    }
  }
}
