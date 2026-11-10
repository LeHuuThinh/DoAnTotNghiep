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

      // 3. Chỉ số & Mô tả (English)
      if (detailStatsText != null) detailStatsText.text = BuildStatsString(item);
      if (detailDescText != null) detailDescText.text = item.Description;

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
  }
}
