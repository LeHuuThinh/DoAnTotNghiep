using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Shop;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ giao diện Cửa Hàng (Shop UI) 2 Panel:
  /// - Panel Trái: Danh sách mặt hàng niêm yết bán + Khung xem trước chi tiết & Nút Mua
  /// - Panel Phải: Túi đồ người chơi + Thao tác Bán đồ (+40% giá trị)
  /// </summary>
  public class ShopUIController : MonoBehaviour
  {
    public static ShopUIController Instance { get; private set; }
    public static bool IsOpen { get; private set; } = false;
    public static int LastClosedFrame { get; private set; } = -1;
    private int openedFrame = -1;

    public enum InventoryTab
    {
      All,
      Weapon,
      Armor,
      Item
    }

    [Header("=== GIAO DIỆN GỐC (ROOT CANVAS GROUP) ===")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image darkOverlay;

    [Header("=== CỬA SỔ TRÁI: TIỆM (SHOP PANEL) ===")]
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private TextMeshProUGUI shopTitleText;
    [SerializeField] private TextMeshProUGUI shopDescText;
    [SerializeField] private Transform goodsContent;
    [SerializeField] private ScrollRect goodsScrollRect;

    [Header("=== KHUNG CHI TIẾT MÓN HÀNG SHOP (ITEM DETAIL) ===")]
    [SerializeField] private RectTransform itemDetailPanel;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailRarityText;
    [SerializeField] private TextMeshProUGUI detailTypeText;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private Image detailRarityBg;
    [SerializeField] private Image detailRarityBar;
    [SerializeField] private TextMeshProUGUI detailStatsText;
    [SerializeField] private TextMeshProUGUI detailDescText;
    [SerializeField] private TextMeshProUGUI detailPriceText;
    [SerializeField] private Image detailGoldIcon;
    [SerializeField] private Button btnBuy;
    [SerializeField] private TextMeshProUGUI btnBuyText;
    [SerializeField] private GameObject btnBuyBorder;
    [SerializeField] private GameObject btnBuyCorner;

    [Header("=== CẤU TRÚC CHỈ SỐ MÓN HÀNG SHOP (ITEM STATS & ENCHANT PREVIEW) ===")]
    [SerializeField] private RectTransform itemStatsContainer;
    [SerializeField] private GameObject mainStatsRoot;
    [SerializeField] private TextMeshProUGUI mainStatLabelText;
    [SerializeField] private TextMeshProUGUI mainStatValueText;
    [SerializeField] private GameObject subStatsDivider;
    [SerializeField] private RectTransform subStatsLayoutRoot;
    [SerializeField] private List<SubStatRowUI> subStatRows = new List<SubStatRowUI>();

    [SerializeField] private RectTransform itemEnchantmentContainer;
    [SerializeField] private GameObject enchantmentDivider;
    [SerializeField] private RectTransform enchantmentLayoutRoot;
    [SerializeField] private List<EnchantRowUI> enchantRows = new List<EnchantRowUI>();

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

    [Header("=== BẢNG POPUP CHI TIẾT KHI CLICK Ô ĐỒ (BÁN) ===")]
    [SerializeField] private ItemDetailPopupUI itemDetailPopup;

    [Header("=== CẤU HÌNH DỮ LIỆU CỬA HÀNG HIỆN TẠI ===")]
    [SerializeField] private ShopData currentShopData;

    [Header("=== THỜI LƯỢNG MỞ / ĐÓNG ===")]
    [SerializeField] private float fadeDuration = 0.18f;

    // Danh sách các dòng mặt hàng đang hiển thị
    private List<ShopSlotUI> shopSlots = new List<ShopSlotUI>();
    private ShopSlotUI selectedShopSlot = null;
    private InventoryTab currentInventoryTab = InventoryTab.All;
    private PlayerController cachedPlayerController;
    private Coroutine transitionRoutine;

    // Màu hiển thị giá tiền
    private readonly Color normalPriceColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700
    private readonly Color redPriceColor = new Color(1f, 0.28f, 0.28f, 1f);   // #FF4747

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      // Đảm bảo Controller Kéo Thả luôn sẵn sàng dù mở Shop trước hay mở Túi đồ trước
      if (InventoryDragDropController.Instance == null)
      {
        var existing = FindFirstObjectByType<InventoryDragDropController>();
        if (existing == null)
        {
          gameObject.AddComponent<InventoryDragDropController>();
        }
      }

      AutoFindReferences();

      // Mặc định ẩn giao diện khi game bắt đầu
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
    }

    private void Update()
    {
      if (!IsOpen) return;

      if (Keyboard.current != null)
      {
        // 1. Phím F đóng Cửa Hàng (bỏ qua frame vừa mở để tránh đóng ngay lập tức)
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
          if (Time.frameCount != openedFrame)
          {
            CloseShop();
            return;
          }
        }

        // 2. Phím ESC đóng popup chi tiết trước (nếu đang mở) hoặc đóng Cửa Hàng
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
          if (itemDetailPopup != null && itemDetailPopup.gameObject.activeSelf)
          {
            itemDetailPopup.Hide();
          }
          else
          {
            CloseShop();
          }
        }
      }
    }

    /// <summary>
    /// Tự động tìm kiếm toàn bộ thành phần UI nếu chưa gán trong Inspector
    /// </summary>
    [ContextMenu("Auto Find References")]
    public void AutoFindReferences()
    {
      if (rootCanvasGroup == null) rootCanvasGroup = GetComponent<CanvasGroup>();
      if (darkOverlay == null) darkOverlay = transform.Find("DarkOverlay")?.GetComponent<Image>();

      // Shop Panel
      if (shopPanel == null) shopPanel = transform.Find("ShopPanel") as RectTransform;
      if (shopPanel != null)
      {
        Transform header = shopPanel.Find("HeaderRow");
        if (header != null)
        {
          if (shopTitleText == null) shopTitleText = header.Find("NameText")?.GetComponent<TextMeshProUGUI>();
          if (shopDescText == null) shopDescText = header.Find("SmallDescriptionText")?.GetComponent<TextMeshProUGUI>();
        }

        Transform scroll = shopPanel.Find("ItemSellScrollView");
        if (scroll != null)
        {
          if (goodsScrollRect == null) goodsScrollRect = scroll.GetComponent<ScrollRect>();
          if (goodsContent == null) goodsContent = scroll.Find("Viewport/Content");

          // Tăng độ nhạy cuộn chuột
          if (goodsScrollRect != null && goodsScrollRect.scrollSensitivity < 10f)
          {
            goodsScrollRect.scrollSensitivity = 20f;
          }
        }

        Transform detail = shopPanel.Find("ItemDetail");
        if (detail != null)
        {
          if (itemDetailPanel == null) itemDetailPanel = detail as RectTransform;
          if (detailNameText == null) detailNameText = detail.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();

          Transform typeRow = detail.Find("ItemTypeRow");
          if (typeRow != null)
          {
            if (detailRarityText == null) detailRarityText = typeRow.Find("ItemRarity")?.GetComponent<TextMeshProUGUI>();
            if (detailTypeText == null) detailTypeText = typeRow.Find("ItemType")?.GetComponent<TextMeshProUGUI>();
          }

          Transform iconTr = detail.Find("Icon");
          if (iconTr != null)
          {
            if (!iconTr.gameObject.activeSelf) iconTr.gameObject.SetActive(true);
            if (detailIconImage == null)
            {
              var imgTr = iconTr.Find("ItemIcon") ?? iconTr.Find("Icon");
              detailIconImage = imgTr?.GetComponent<Image>();
            }
            if (detailRarityBg == null) detailRarityBg = iconTr.Find("RarityBG")?.GetComponent<Image>();
            if (detailRarityBar == null) detailRarityBar = iconTr.Find("RarityBar")?.GetComponent<Image>();
          }

          if (detailStatsText == null) detailStatsText = detail.Find("ItemStatsText")?.GetComponent<TextMeshProUGUI>();
          if (detailDescText == null) detailDescText = detail.Find("ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();

          // MainStats & SubStatsLayout (Cấu trúc mới đồng bộ ItemDetail)
          if (itemStatsContainer == null) itemStatsContainer = detail.Find("ItemStatsText") as RectTransform;
          if (itemStatsContainer != null)
          {
            if (mainStatsRoot == null) mainStatsRoot = itemStatsContainer.Find("MainStats")?.gameObject;
            if (mainStatLabelText == null) mainStatLabelText = itemStatsContainer.Find("MainStats")?.GetComponent<TextMeshProUGUI>();
            if (mainStatValueText == null) mainStatValueText = itemStatsContainer.Find("MainStats/Value")?.GetComponent<TextMeshProUGUI>();
            if (subStatsDivider == null) subStatsDivider = detail.Find("Line (2)")?.gameObject ?? itemStatsContainer.Find("Line (2)")?.gameObject;
            if (subStatsLayoutRoot == null) subStatsLayoutRoot = itemStatsContainer.Find("SubStatsLayout") as RectTransform;

            if (subStatsLayoutRoot != null && (subStatRows == null || subStatRows.Count < 4))
            {
              subStatRows = new List<SubStatRowUI>();
              string[] subNames = new[] { "SubStats", "SubStats (1)", "SubStats (2)", "SubStats (3)" };
              for (int i = 0; i < subNames.Length; i++)
              {
                Transform t = subStatsLayoutRoot.Find(subNames[i]);
                if (t != null)
                {
                  subStatRows.Add(new SubStatRowUI
                  {
                    root = t.gameObject,
                    labelText = t.GetComponent<TextMeshProUGUI>(),
                    valueText = t.Find("Value")?.GetComponent<TextMeshProUGUI>()
                  });
                }
              }
            }
          }

          // ItemEnchantment & EnchantmentLayout (Cấu trúc mới đồng bộ ItemDetail)
          if (itemEnchantmentContainer == null) itemEnchantmentContainer = detail.Find("ItemEnchantment") as RectTransform;
          if (itemEnchantmentContainer != null)
          {
            if (enchantmentDivider == null) enchantmentDivider = detail.Find("Line (3)")?.gameObject ?? itemEnchantmentContainer.Find("Line (3)")?.gameObject;
            if (enchantmentLayoutRoot == null) enchantmentLayoutRoot = itemEnchantmentContainer.Find("EnchantmentLayout") as RectTransform;

            if (enchantmentLayoutRoot != null && (enchantRows == null || enchantRows.Count < 3))
            {
              enchantRows = new List<EnchantRowUI>();
              string[] enchNames = new[] { "Enchant", "Enchant (1)", "Enchant (2)" };
              for (int i = 0; i < enchNames.Length; i++)
              {
                Transform t = enchantmentLayoutRoot.Find(enchNames[i]);
                if (t != null)
                {
                  enchantRows.Add(new EnchantRowUI
                  {
                    root = t.gameObject,
                    iconActive = t.Find("IconActive")?.GetComponent<Image>(),
                    iconUnactive = t.Find("IconUnactive")?.GetComponent<Image>(),
                    text = t.Find("EnchantmentText")?.GetComponent<TextMeshProUGUI>()
                  });
                }
              }
            }
          }

          Transform footer = detail.Find("Footer");
          if (footer != null)
          {
            Transform priceTr = footer.Find("GoldPrice");
            if (priceTr != null)
            {
              if (detailPriceText == null) detailPriceText = priceTr.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
              if (detailGoldIcon == null) detailGoldIcon = priceTr.Find("GoldIcon")?.GetComponent<Image>();
            }

            Transform buyTr = footer.Find("BtnBuy");
            if (buyTr != null)
            {
              if (btnBuy == null) btnBuy = buyTr.GetComponent<Button>();
              if (btnBuyText == null) btnBuyText = buyTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
              if (btnBuyBorder == null) btnBuyBorder = buyTr.Find("Border")?.gameObject;
              if (btnBuyCorner == null) btnBuyCorner = buyTr.Find("Corner")?.gameObject;
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
        if (footer != null && playerGoldText == null)
        {
          playerGoldText = footer.Find("NumberText")?.GetComponent<TextMeshProUGUI>();
        }

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

      // Popup Bán
      if (itemDetailPopup == null)
      {
        itemDetailPopup = transform.Find("ItemDetailPopup")?.GetComponent<ItemDetailPopupUI>()
                       ?? GetComponentInChildren<ItemDetailPopupUI>(true);
      }
    }

    private void SetupEvents()
    {
      if (closeButton != null)
      {
        closeButton.onClick.AddListener(CloseShop);
      }

      if (btnBuy != null)
      {
        btnBuy.onClick.AddListener(HandleBuyClicked);
      }

      if (itemDetailPopup != null)
      {
        itemDetailPopup.SetShopSellMode(true);
        itemDetailPopup.OnSellClicked += HandleSellItemFromBag;
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

    private void HandleInventoryDataChanged()
    {
      if (IsOpen)
      {
        RefreshInventoryGrid();
        UpdatePlayerGold();
        if (selectedShopSlot != null && selectedShopSlot.Entry != null && InventoryManager.HasInstance)
        {
          UpdateBuyButtonState(selectedShopSlot.Entry, InventoryManager.Instance.CurrentGold);
        }
      }
    }

    private void HandleGoldChanged(int newGold)
    {
      if (IsOpen)
      {
        UpdatePlayerGold();
        if (selectedShopSlot != null && selectedShopSlot.Entry != null)
        {
          UpdateBuyButtonState(selectedShopSlot.Entry, newGold);
        }
      }
    }

    /// <summary>
    /// Đóng popup chi tiết túi đồ khi bắt đầu kéo thả
    /// </summary>
    public void CloseDetailPopup()
    {
      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
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
    // MỞ / ĐÓNG CỬA HÀNG (OPEN / CLOSE SHOP)
    // =========================================================================

    /// <summary>
    /// Mở Cửa Hàng với ShopData tương ứng
    /// </summary>
    public void OpenShop(ShopData shopData)
    {
      if (shopData == null) return;
      currentShopData = shopData;
      IsOpen = true;
      openedFrame = Time.frameCount;

      AutoFindReferences();
      CachePlayerController();

      // 1. Khóa di chuyển nhân vật và mở chuột
      if (cachedPlayerController != null)
      {
        cachedPlayerController.LockMovement();
      }
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      // 2. Cài đặt tiêu đề và mô tả cửa hàng
      if (shopTitleText != null) shopTitleText.text = currentShopData.ShopTitle;
      if (shopDescText != null) shopDescText.text = currentShopData.ShopDescription;

      // 3. Nạp danh mục mặt hàng bán
      PopulateGoodsList();

      // 4. Mặc định chọn ngay món đầu tiên
      SelectDefaultFirstSlot();

      // 5. Làm mới túi đồ người chơi bên phải
      currentInventoryTab = InventoryTab.All;
      UpdateTabHighlight();
      RefreshInventoryGrid();
      UpdatePlayerGold();

      // 6. Ẩn popup bán cũ nếu còn
      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
        itemDetailPopup.SetShopSellMode(true);
      }

      // 7. Hoạt ảnh Fade In mượt mà
      if (transitionRoutine != null) StopCoroutine(transitionRoutine);
      transitionRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    /// <summary>
    /// Đóng Cửa Hàng và khôi phục trạng thái điều khiển
    /// </summary>
    public void CloseShop()
    {
      if (!IsOpen) return;
      IsOpen = false;
      LastClosedFrame = Time.frameCount;

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }

      // Khôi phục điều khiển nhân vật và khóa chuột
      if (cachedPlayerController != null)
      {
        cachedPlayerController.UnlockMovement();
      }
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      // Hoạt ảnh Fade Out mượt mà
      if (transitionRoutine != null) StopCoroutine(transitionRoutine);
      transitionRoutine = StartCoroutine(FadeRoutine(0f, false));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha, bool enableBlocks)
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
        if (cachedPlayerController == null) cachedPlayerController = FindObjectOfType<PlayerController>();
      }
    }

    // =========================================================================
    // NẠP DANH SÁCH MẶT HÀNG & MẶC ĐỊNH CHỌN MÓN ĐẦU TIÊN
    // =========================================================================

    private void PopulateGoodsList()
    {
      if (goodsContent == null || currentShopData == null) return;

      int currentGold = InventoryManager.HasInstance ? InventoryManager.Instance.CurrentGold : 0;
      var existingRows = goodsContent.GetComponentsInChildren<ShopSlotUI>(true);
      if (existingRows == null || existingRows.Length == 0)
      {
        for (int i = 0; i < goodsContent.childCount; i++)
        {
          Transform child = goodsContent.GetChild(i);
          if (child.name.StartsWith("ShopItemRow"))
          {
            var slot = child.GetComponent<ShopSlotUI>() ?? child.gameObject.AddComponent<ShopSlotUI>();
            slot.AutoFindReferences();
          }
        }
        existingRows = goodsContent.GetComponentsInChildren<ShopSlotUI>(true);
      }

      shopSlots.Clear();
      shopSlots.AddRange(existingRows);

      int targetCount = currentShopData.Items.Count;

      // Đảm bảo đủ số lượng slot trong Content (tái sử dụng hoặc tạo mới từ mẫu có sẵn)
      while (shopSlots.Count < targetCount && shopSlots.Count > 0)
      {
        GameObject clone = Instantiate(shopSlots[0].gameObject, goodsContent);
        ShopSlotUI newSlot = clone.GetComponent<ShopSlotUI>();
        shopSlots.Add(newSlot);
      }

      for (int i = 0; i < shopSlots.Count; i++)
      {
        if (i < targetCount)
        {
          ShopItemEntry entry = currentShopData.Items[i];
          shopSlots[i].Setup(entry, i, currentGold);
          shopSlots[i].OnClicked -= HandleShopSlotClicked;
          shopSlots[i].OnClicked += HandleShopSlotClicked;
        }
        else
        {
          shopSlots[i].gameObject.SetActive(false);
        }
      }
    }

    /// <summary>
    /// Mặc định chọn ngay món đầu tiên khi mở Shop
    /// </summary>
    private void SelectDefaultFirstSlot()
    {
      if (shopSlots.Count > 0 && shopSlots[0].gameObject.activeSelf)
      {
        SelectShopSlot(shopSlots[0]);
      }
      else
      {
        ClearDetailPane();
      }
    }

    private void HandleShopSlotClicked(ShopSlotUI slot)
    {
      if (slot == null || slot.Entry == null) return;
      SelectShopSlot(slot);
    }

    private void SelectShopSlot(ShopSlotUI slot)
    {
      // Bỏ chọn slot cũ
      if (selectedShopSlot != null && selectedShopSlot != slot)
      {
        selectedShopSlot.SetSelected(false);
      }

      selectedShopSlot = slot;
      if (selectedShopSlot != null)
      {
        selectedShopSlot.SetSelected(true);
        UpdateDetailPane(selectedShopSlot.Entry);
      }
    }

    // =========================================================================
    // CẬP NHẬT KHUNG CHI TIẾT VẬT PHẨM & NÚT MUA
    // =========================================================================

    private void UpdateDetailPane(ShopItemEntry entry)
    {
      if (entry == null || entry.ItemData == null)
      {
        ClearDetailPane();
        return;
      }

      ItemData item = entry.ItemData;
      int currentGold = InventoryManager.HasInstance ? InventoryManager.Instance.CurrentGold : 0;

      // 1. Tên, phẩm cấp và loại
      if (detailNameText != null) detailNameText.text = item.Rarity.FormatWithGradient(item.ItemName);
      if (detailRarityText != null)
      {
        detailRarityText.text = item.Rarity.FormatWithGradient(item.Rarity.GetRarityName());
      }
      if (detailTypeText != null)
      {
        detailTypeText.text = GetItemTypeName(item.ItemType);
      }

      // 2. Kích hoạt và cập nhật Icon & Viền
      if (detailIconImage == null && itemDetailPanel != null)
      {
        Transform iconTr = itemDetailPanel.Find("Icon");
        if (iconTr != null)
        {
          var imgTr = iconTr.Find("ItemIcon") ?? iconTr.Find("Icon");
          detailIconImage = imgTr?.GetComponent<Image>();
        }
      }

      if (detailIconImage != null)
      {
        if (detailIconImage.transform.parent != null && !detailIconImage.transform.parent.gameObject.activeSelf)
        {
          detailIconImage.transform.parent.gameObject.SetActive(true);
        }
        detailIconImage.gameObject.SetActive(item.Icon != null);
        detailIconImage.sprite = item.Icon;
        detailIconImage.enabled = item.Icon != null;
        detailIconImage.color = Color.white;
      }

      Color rColor = item.Rarity.GetRarityColor();
      if (detailRarityBg != null)
      {
        detailRarityBg.gameObject.SetActive(true);
        detailRarityBg.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (detailRarityBar != null)
      {
        detailRarityBar.gameObject.SetActive(true);
        detailRarityBar.color = rColor;
      }

      // 3. Chỉ số & Mô tả (Cấu trúc mới đồng bộ ItemDetail: MainStats, SubStats, Enchantment Hints)
      DisplayItemDetailHints(item);

      // 4. Giá & Nút Mua (Đổi màu đỏ ở đây nếu thiếu tiền)
      UpdateBuyButtonState(entry, currentGold);
    }

    private void UpdateBuyButtonState(ShopItemEntry entry, int currentGold)
    {
      if (entry == null) return;

      int price = entry.BuyPrice;
      bool canAfford = currentGold >= price;
      bool hasStock = entry.IsInfiniteStock || entry.StockQuantity > 0;
      bool isInteractable = canAfford && hasStock;

      // Cập nhật text giá: Đủ tiền màu vàng mặc định, thiếu tiền màu ĐỎ
      if (detailPriceText != null)
      {
        detailPriceText.text = price.ToString("N0");
        detailPriceText.color = canAfford ? normalPriceColor : redPriceColor;
      }

      // Nút mua: interactable và bật/tắt viền trang trí
      if (btnBuy != null)
      {
        btnBuy.interactable = isInteractable;
      }

      // Tắt Border và Corner khi nút không thể bấm được
      if (btnBuyBorder != null) btnBuyBorder.SetActive(isInteractable);
      if (btnBuyCorner != null) btnBuyCorner.SetActive(isInteractable);

      if (btnBuyText != null)
      {
        if (!hasStock)
        {
          btnBuyText.text = "OUT OF STOCK";
          btnBuyText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }
        else if (!canAfford)
        {
          // Khi không đủ tiền: làm tối màu nút, tắt viền và giữ chữ BUY ngắn gọn
          btnBuyText.text = "BUY";
          btnBuyText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        }
        else
        {
          btnBuyText.text = "BUY";
          btnBuyText.color = Color.white;
        }
      }
    }

    private void ClearDetailPane()
    {
      if (detailNameText != null) detailNameText.text = "";
      if (detailRarityText != null) detailRarityText.text = "";
      if (detailTypeText != null) detailTypeText.text = "";
      if (detailIconImage != null) detailIconImage.enabled = false;
      if (detailStatsText != null) detailStatsText.text = "";
      if (detailDescText != null) detailDescText.text = "Select an item from the list.";

      // Ẩn các container chỉ số & phù phép
      if (itemStatsContainer != null) itemStatsContainer.gameObject.SetActive(false);
      if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
      if (subStatsDivider != null) subStatsDivider.SetActive(false);
      if (enchantmentDivider != null) enchantmentDivider.SetActive(false);

      if (detailPriceText != null)
      {
        detailPriceText.text = "0";
        detailPriceText.color = normalPriceColor;
      }
      if (btnBuy != null) btnBuy.interactable = false;
      if (btnBuyBorder != null) btnBuyBorder.SetActive(false);
      if (btnBuyCorner != null) btnBuyCorner.SetActive(false);
      if (btnBuyText != null)
      {
        btnBuyText.text = "BUY";
        btnBuyText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
      }
    }

    // =========================================================================
    // XỬ LÝ MUA HÀNG (HANDLE BUY)
    // =========================================================================

    private void HandleBuyClicked()
    {
      if (selectedShopSlot == null || selectedShopSlot.Entry == null) return;
      ShopItemEntry entry = selectedShopSlot.Entry;
      ItemData item = entry.ItemData;

      if (!InventoryManager.HasInstance) return;
      int currentGold = InventoryManager.Instance.CurrentGold;

      // 1. Kiểm tra đủ tiền
      if (currentGold < entry.BuyPrice)
      {
        return;
      }

      // 2. Thêm vật phẩm vào túi đồ (tự động roll dòng ngẫu nhiên và phù phép nếu là trang bị)
      EquipmentAffixData affix = null;
      if (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor)
      {
        affix = EquipmentAffixGenerator.Generate(item);
      }
      bool added = InventoryManager.Instance.AddItem(item, 1, out int remainder, notifyPickup: true, affixData: affix);
      if (!added || remainder > 0)
      {
        // Túi đồ đầy
        if (btnBuyText != null) btnBuyText.text = "BAG FULL!";
        return;
      }

      // 3. Trừ tiền vàng
      InventoryManager.Instance.SpendGold(entry.BuyPrice);

      // 4. Giảm tồn kho nếu không vô hạn
      if (!entry.IsInfiniteStock && entry.StockQuantity > 0)
      {
        entry.StockQuantity--;
      }

      // 5. Làm mới toàn bộ UI
      int newGold = InventoryManager.Instance.CurrentGold;
      selectedShopSlot.Setup(entry, selectedShopSlot.SlotIndex, newGold);
      selectedShopSlot.SetSelected(true);

      // Giá các ô trong list luôn giữ màu mặc định
      for (int i = 0; i < shopSlots.Count; i++)
      {
        if (shopSlots[i].gameObject.activeSelf)
        {
          shopSlots[i].UpdatePriceColor(newGold);
        }
      }

      UpdateBuyButtonState(entry, newGold);
      UpdatePlayerGold();
      RefreshInventoryGrid();
    }

    // =========================================================================
    // XỬ LÝ TÚI ĐỒ NGƯỜI CHƠI & BÁN ĐỒ (INVENTORY & SELLING)
    // =========================================================================

    public void SwitchInventoryTab(InventoryTab tab)
    {
      currentInventoryTab = tab;
      UpdateTabHighlight();
      RefreshInventoryGrid();
      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }
    }

    private void UpdateTabHighlight()
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
            bagSlots[i].SetItem(invSlots[i].ItemData, invSlots[i].Quantity, i);
          }
          else
          {
            bagSlots[i].ClearSlot(i);
          }
        }
      }
      else
      {
        var matchingSlots = new List<(ItemData item, int quantity, int originalIndex)>();

        for (int i = 0; i < invSlots.Count; i++)
        {
          if (invSlots[i].IsEmpty) continue;

          var item = invSlots[i].ItemData;
          bool isMatch = false;

          switch (currentInventoryTab)
          {
            case InventoryTab.Weapon:
              isMatch = (item.ItemType == ItemType.Weapon);
              break;
            case InventoryTab.Armor:
              isMatch = (item.ItemType == ItemType.Armor);
              break;
            case InventoryTab.Item:
              isMatch = (item.ItemType == ItemType.Consumable || item.ItemType == ItemType.Material);
              break;
          }

          if (isMatch)
          {
            matchingSlots.Add((item, invSlots[i].Quantity, i));
          }
        }

        for (int i = 0; i < bagSlots.Count; i++)
        {
          if (bagSlots[i] == null) continue;

          if (i < matchingSlots.Count)
          {
            var match = matchingSlots[i];
            bagSlots[i].SetItem(match.item, match.quantity, match.originalIndex);
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
        itemDetailPopup.SetShopSellMode(true);
        itemDetailPopup.ShowForBagSlot(slot);
      }
    }

    /// <summary>
    /// Xử lý bán vật phẩm từ túi đồ vào Shop
    /// </summary>
    private void HandleSellItemFromBag(InventorySlotUI slot, ItemData item)
    {
      if (slot == null || item == null || !InventoryManager.HasInstance) return;

      // Không cho phép bán vật phẩm nhiệm vụ
      if (item.Id == "item_key_dungeon") return;

      int sellPrice = item.SellPrice; // Đã là 40% giá trị gốc

      // 1. Cộng tiền vàng cho người chơi
      InventoryManager.Instance.AddGold(sellPrice);

      // 2. Trừ 1 số lượng vật phẩm trong ô
      bool removed = InventoryManager.Instance.RemoveItemAt(slot.ActualBagIndex, 1);

      // 3. Làm mới giao diện
      UpdatePlayerGold();
      RefreshInventoryGrid();

      int currentGold = InventoryManager.Instance.CurrentGold;

      // Cập nhật giá ô đang chọn trong ItemDetail (có thể chuyển từ đỏ sang vàng nếu vừa bán xong đủ tiền)
      if (selectedShopSlot != null && selectedShopSlot.Entry != null)
      {
        UpdateBuyButtonState(selectedShopSlot.Entry, currentGold);
      }

      // Nếu ô đồ đã hết sạch số lượng -> Đóng popup
      if (slot.IsEmpty || slot.CurrentQuantity <= 0)
      {
        if (itemDetailPopup != null) itemDetailPopup.Hide();
      }
      else
      {
        // Còn số lượng -> Cập nhật lại popup
        if (itemDetailPopup != null) itemDetailPopup.ShowForBagSlot(slot);
      }
    }

    private void UpdatePlayerGold()
    {
      if (playerGoldText != null && InventoryManager.HasInstance)
      {
        playerGoldText.text = InventoryManager.Instance.CurrentGold.ToString("N0");
      }
    }

    // =========================================================================
    // TIỆN ÍCH CHUYỂN ĐỔI CHỮ (HELPERS - ENGLISH)
    // =========================================================================

    private string GetItemTypeName(ItemType type)
    {
      switch (type)
      {
        case ItemType.Weapon: return "Weapon";
        case ItemType.Armor: return "Armor";
        case ItemType.Consumable: return "Consumable";
        case ItemType.Material: return "Material";
        case ItemType.Currency: return "Currency";
        default: return "Item";
      }
    }

    private string BuildStatsString(ItemData item)
    {
      if (item == null) return "";
      var sb = new System.Text.StringBuilder();

      if (item.BonusAttack > 0f) sb.AppendLine($"<color=#FF7777>Attack (ATK):</color> +{item.BonusAttack}");
      if (item.BonusDefense > 0f) sb.AppendLine($"<color=#77AAFF>Defense (DEF):</color> +{item.BonusDefense}");
      if (item.BonusHealth > 0f) sb.AppendLine($"<color=#77FF77>Max Health (HP):</color> +{item.BonusHealth}");
      if (item.BonusStamina > 0f) sb.AppendLine($"<color=#FFD700>Max Stamina:</color> +{item.BonusStamina}");
      if (item.HealAmount > 0f) sb.AppendLine($"<color=#77FF77>Health Recovery:</color> +{item.HealAmount} HP");

      return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Hiển thị thông số xem trước (Hint) cho món hàng:
    /// - Vũ khí: Attack min - max
    /// - Giáp: HP / DEF min-max / min-max
    /// - SubStats: Substat 1, 2, 3...
    /// - Enchant: Mốc phù phép thức tỉnh tiềm năng tại +1, +3, +5
    /// - Consumable / Material: hiển thị đúng loại và mô tả
    /// </summary>
    private void DisplayItemDetailHints(ItemData item)
    {
      if (item == null)
      {
        if (itemStatsContainer != null) itemStatsContainer.gameObject.SetActive(false);
        if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
        if (detailDescText != null) detailDescText.text = "";
        return;
      }

      bool isEquipment = item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor;

      if (isEquipment)
      {
        var (varMin, varMax) = GetRarityVariance(item.Rarity);

        // 1. DÒNG CHÍNH (MAIN STATS)
        if (itemStatsContainer != null) itemStatsContainer.gameObject.SetActive(true);
        if (mainStatsRoot != null) mainStatsRoot.SetActive(true);

        if (item.ItemType == ItemType.Weapon)
        {
          float baseAtk = item.BonusAttack > 0f ? item.BonusAttack : 10f;
          float minAtk = Mathf.Max(1f, Mathf.Round(baseAtk * varMin * 10f) / 10f);
          float maxAtk = Mathf.Max(1f, Mathf.Round(baseAtk * varMax * 10f) / 10f);

          if (mainStatLabelText != null) mainStatLabelText.text = "Attack";
          if (mainStatValueText != null) mainStatValueText.text = $"{minAtk:0.#} - {maxAtk:0.#}";
        }
        else // Armor
        {
          bool hasDef = item.BonusDefense > 0f;
          bool hasHp = item.BonusHealth > 0f;

          if (hasDef && hasHp)
          {
            float minDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varMin * 10f) / 10f);
            float maxDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varMax * 10f) / 10f);
            float minHp = Mathf.Max(5f, Mathf.Round(item.BonusHealth * varMin * 10f) / 10f);
            float maxHp = Mathf.Max(5f, Mathf.Round(item.BonusHealth * varMax * 10f) / 10f);

            if (mainStatLabelText != null) mainStatLabelText.text = "DEF or HP";
            if (mainStatValueText != null) mainStatValueText.text = $"{minDef:0.#}-{maxDef:0.#} | {minHp:0.#}-{maxHp:0.#}";
          }
          else if (hasDef)
          {
            float minDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varMin * 10f) / 10f);
            float maxDef = Mathf.Max(1f, Mathf.Round(item.BonusDefense * varMax * 10f) / 10f);

            if (mainStatLabelText != null) mainStatLabelText.text = "Defense";
            if (mainStatValueText != null) mainStatValueText.text = $"{minDef:0.#} - {maxDef:0.#}";
          }
          else
          {
            float baseHp = hasHp ? item.BonusHealth : 20f;
            float minHp = Mathf.Max(5f, Mathf.Round(baseHp * varMin * 10f) / 10f);
            float maxHp = Mathf.Max(5f, Mathf.Round(baseHp * varMax * 10f) / 10f);

            if (mainStatLabelText != null) mainStatLabelText.text = "Max Health";
            if (mainStatValueText != null) mainStatValueText.text = $"{minHp:0.#} - {maxHp:0.#}";
          }
        }

        // 2. DÒNG PHỤ (SUB-STATS)
        int maxSubCount = GetMaxSubStatCount(item.Rarity);
        if (maxSubCount > 0 && subStatRows != null && subStatRows.Count > 0)
        {
          if (subStatsDivider != null) subStatsDivider.SetActive(true);
          if (subStatsLayoutRoot != null) subStatsLayoutRoot.gameObject.SetActive(true);

          for (int i = 0; i < subStatRows.Count; i++)
          {
            if (subStatRows[i] == null || subStatRows[i].root == null) continue;

            if (i < maxSubCount)
            {
              subStatRows[i].root.SetActive(true);
              if (subStatRows[i].labelText != null)
              {
                subStatRows[i].labelText.text = $"Substat {i + 1}";
                subStatRows[i].labelText.enableWordWrapping = false;
              }
              if (subStatRows[i].valueText != null)
              {
                subStatRows[i].valueText.text = "<color=#90A4AE>Random</color>";
                subStatRows[i].valueText.enableWordWrapping = false;
              }
            }
            else
            {
              subStatRows[i].root.SetActive(false);
            }
          }
        }
        else
        {
          if (subStatsDivider != null) subStatsDivider.SetActive(false);
          if (subStatsLayoutRoot != null) subStatsLayoutRoot.gameObject.SetActive(false);
          for (int i = 0; i < subStatRows.Count; i++)
          {
            if (subStatRows[i]?.root != null) subStatRows[i].root.SetActive(false);
          }
        }

        // 3. MỐC PHÙ PHÉP THỨC TỈNH (ENCHANTMENT HINTS: +1, +3, +5)
        var milestones = GetEnchantMilestones(item.Rarity);
        int maxEnchCount = milestones.Count;
        if (maxEnchCount > 0 && enchantRows != null && enchantRows.Count > 0)
        {
          if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(true);
          if (enchantmentDivider != null) enchantmentDivider.SetActive(true);
          if (enchantmentLayoutRoot != null) enchantmentLayoutRoot.gameObject.SetActive(true);

          for (int i = 0; i < enchantRows.Count; i++)
          {
            if (enchantRows[i] == null || enchantRows[i].root == null) continue;

            if (i < maxEnchCount)
            {
              int msLevel = milestones[i];
              enchantRows[i].root.SetActive(true);
              if (enchantRows[i].iconActive != null) enchantRows[i].iconActive.gameObject.SetActive(false);
              if (enchantRows[i].iconUnactive != null) enchantRows[i].iconUnactive.gameObject.SetActive(true);

              if (enchantRows[i].text != null)
              {
                enchantRows[i].text.text = $"<color=#757575>[+{msLevel}] Random Enchantment</color>";
              }
            }
            else
            {
              enchantRows[i].root.SetActive(false);
            }
          }
        }
        else
        {
          if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
          if (enchantmentDivider != null) enchantmentDivider.SetActive(false);
          if (enchantmentLayoutRoot != null) enchantmentLayoutRoot.gameObject.SetActive(false);
          for (int i = 0; i < enchantRows.Count; i++)
          {
            if (enchantRows[i]?.root != null) enchantRows[i].root.SetActive(false);
          }
        }

        // 4. Mô tả trang bị (Lore description)
        if (detailDescText != null)
        {
          detailDescText.gameObject.SetActive(true);
          detailDescText.text = !string.IsNullOrEmpty(item.Description) ? item.Description : "";
        }
      }
      else
      {
        // === VẬT PHẨM THƯỜNG (Consumable / Material / Currency) ===
        // 1. Tắt khu vực Phù phép thức tỉnh (chỉ dành cho Trang bị)
        if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
        if (enchantmentDivider != null) enchantmentDivider.SetActive(false);
        if (enchantmentLayoutRoot != null) enchantmentLayoutRoot.gameObject.SetActive(false);

        // 2. Để trống dòng MainStats (theo yêu cầu: không dùng MainStats hiển thị công dụng)
        if (mainStatsRoot != null) mainStatsRoot.SetActive(false);
        if (mainStatLabelText != null) mainStatLabelText.text = "";
        if (mainStatValueText != null) mainStatValueText.text = "";

        // 3. Đưa mô tả công dụng / đặc tính của vật phẩm xuống khu vực SubStats
        var subList = new List<(string label, string val)>();

        if (item.ItemType == ItemType.Consumable)
        {
          if (item.HealAmount > 0f)
          {
            subList.Add(("HP Recovery", $"<color=#77FF77>+{item.HealAmount:0.#} HP</color>"));
            subList.Add(("Effect", "Instant Restore"));
          }
          else if (item.BonusDefense > 0f)
          {
            subList.Add(("Defense Boost", $"<color=#81D4FA>+{item.BonusDefense:0.#} DEF</color>"));
            subList.Add(("Duration", "30 Seconds"));
          }
          else if (item.BonusAttack > 0f)
          {
            subList.Add(("Attack Boost", $"<color=#FFB74D>+{item.BonusAttack:0.#} ATK</color>"));
            subList.Add(("Duration", "30 Seconds"));
          }
          else if (item.BonusStamina > 0f)
          {
            subList.Add(("Stamina Boost", $"<color=#81C784>+{item.BonusStamina:0.#} Max</color>"));
            subList.Add(("Effect", "Regen Boost"));
          }
          else if (item.BonusHealth > 0f)
          {
            subList.Add(("Health Boost", $"<color=#77FF77>+{item.BonusHealth:0.#} Max HP</color>"));
            subList.Add(("Effect", "Increases Max HP"));
          }
          else if (!string.IsNullOrEmpty(item.Id) && item.Id.Contains("spirit"))
          {
            subList.Add(("Spirit Recovery", "<color=#BA68C8>Instant Spirit</color>"));
            subList.Add(("Effect", "Replenish Skills"));
          }
          else
          {
            subList.Add(("Item Type", "Consumable"));
          }
        }
        else if (item.ItemType == ItemType.Material)
        {
          if (!string.IsNullOrEmpty(item.Id) && (item.Id.Contains("upgrade_stone") || item.Id.Contains("stone")))
          {
            subList.Add(("Usage", "<color=#FFE082>Gear Upgrade</color>"));
            subList.Add(("Target", "Haven Forge"));
            subList.Add(("Upgrade Range", "+1 to +5"));
          }
          else
          {
            subList.Add(("Usage", "<color=#FFE082>Crafting Material</color>"));
          }
        }
        else
        {
          subList.Add(("Item Type", item.ItemType.ToString()));
        }

        // Bổ sung thông tin Max Stack nếu còn dòng trống (định dạng gọn xN chuẩn RPG)
        int maxAvailableRows = subStatRows != null ? subStatRows.Count : 4;
        if (item.MaxStack > 1 && subList.Count < maxAvailableRows)
        {
          subList.Add(("Max Stack", $"x{item.MaxStack}"));
        }

        // 4. Kích hoạt và gán dữ liệu vào cụm SubStats
        if (itemStatsContainer != null) itemStatsContainer.gameObject.SetActive(true);
        if (subStatsDivider != null) subStatsDivider.SetActive(true);
        if (subStatsLayoutRoot != null) subStatsLayoutRoot.gameObject.SetActive(true);

        if (subStatRows != null && subStatRows.Count > 0)
        {
          for (int i = 0; i < subStatRows.Count; i++)
          {
            if (subStatRows[i] == null || subStatRows[i].root == null) continue;

            if (i < subList.Count)
            {
              subStatRows[i].root.SetActive(true);
              if (subStatRows[i].labelText != null)
              {
                subStatRows[i].labelText.text = subList[i].label;
                subStatRows[i].labelText.enableWordWrapping = false;
                subStatRows[i].labelText.enableAutoSizing = true;
                subStatRows[i].labelText.fontSizeMin = 12f;
                subStatRows[i].labelText.fontSizeMax = 20f;
              }
              if (subStatRows[i].valueText != null)
              {
                subStatRows[i].valueText.text = subList[i].val;
                subStatRows[i].valueText.enableWordWrapping = false;
                subStatRows[i].valueText.enableAutoSizing = true;
                subStatRows[i].valueText.fontSizeMin = 12f;
                subStatRows[i].valueText.fontSizeMax = 20f;
              }
            }
            else
            {
              subStatRows[i].root.SetActive(false);
            }
          }
        }

        // 5. Mô tả vật phẩm (Lore description)
        if (detailDescText != null)
        {
          detailDescText.gameObject.SetActive(true);
          detailDescText.text = !string.IsNullOrEmpty(item.Description) ? item.Description : "";
        }
      }

      // Legacy fallback nếu không có cấu trúc container mới
      if (itemStatsContainer == null && detailStatsText != null)
      {
        detailStatsText.text = BuildStatsString(item);
      }
    }

    private (float min, float max) GetRarityVariance(ItemRarity rarity)
    {
      switch (rarity)
      {
        case ItemRarity.Common: return (0.92f, 1.08f);
        case ItemRarity.Uncommon: return (0.94f, 1.12f);
        case ItemRarity.Rare: return (0.96f, 1.16f);
        case ItemRarity.Epic: return (1.00f, 1.20f);
        case ItemRarity.Legendary: return (1.05f, 1.25f);
        default: return (0.92f, 1.08f);
      }
    }

    private int GetMaxSubStatCount(ItemRarity rarity)
    {
      switch (rarity)
      {
        case ItemRarity.Common: return 1;
        case ItemRarity.Uncommon: return 2;
        case ItemRarity.Rare: return 2;
        case ItemRarity.Epic: return 3;
        case ItemRarity.Legendary: return 4;
        default: return 0;
      }
    }

    private List<int> GetEnchantMilestones(ItemRarity rarity)
    {
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
      return milestones;
    }
  }
}
