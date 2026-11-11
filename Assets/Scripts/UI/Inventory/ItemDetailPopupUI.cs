using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Blacksmith;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý bảng hiển thị chi tiết vật phẩm bám theo ô đồ được chọn và các nút hành động (Equip / Unequip / Use / Drop)
  /// </summary>
  public class ItemDetailPopupUI : MonoBehaviour
  {
    [Header("=== THÀNH PHẦN HIỂN THỊ CƠ BẢN ===")]
    [Tooltip("Tên vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Text hiển thị cấp độ cường hóa ở góc trên (+1, +3, +5...)")]
    [SerializeField] private TextMeshProUGUI upgradeLevelText;

    [Tooltip("Text hiển thị độ hiếm (Common, Rare, Epic, Legendary)")]
    [SerializeField] private TextMeshProUGUI itemRarityText;

    [Tooltip("Text hiển thị phân loại (Weapon, Armor, Consumable...)")]
    [SerializeField] private TextMeshProUGUI itemTypeText;

    [Tooltip("Icon lớn xem trước")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Ảnh nền trắng RarityBG dùng để tô màu theo phẩm cấp (Common, Rare, Epic, Legendary)")]
    [SerializeField] private Image rarityBg;

    [Tooltip("Thanh bar dưới icon đổi màu theo phẩm cấp của vật phẩm")]
    [SerializeField] private Image rarityBar;

    [Tooltip("GameObject container Icon chứa ItemIcon, RarityBG, RarityBar")]
    [SerializeField] private GameObject iconContainer;

    [Tooltip("Mô tả / Cốt truyện vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Tooltip("Legacy fallback stats text (giữ lại để tương thích các inspector cũ nếu có)")]
    [SerializeField] private TextMeshProUGUI itemStatsText;

    [Header("=== CẤU TRÚC CHỈ SỐ MỚI (MAIN & SUB-STATS) ===")]
    [Tooltip("Container tổng của ItemStatsText")]
    [SerializeField] private RectTransform itemStatsTextContainer;

    [Tooltip("GameObject root của MainStats")]
    [SerializeField] private GameObject mainStatsRoot;

    [Tooltip("Text tên chỉ số chính (Attack Damage, Defense, Max Health...)")]
    [SerializeField] private TextMeshProUGUI mainStatLabelText;

    [Tooltip("Text giá trị chỉ số chính")]
    [SerializeField] private TextMeshProUGUI mainStatValueText;

    [Tooltip("Đường kẻ phân cách Line (2) giữa MainStats và SubStats")]
    [SerializeField] private GameObject subStatsDivider;

    [Tooltip("Root của SubStatsLayout")]
    [SerializeField] private RectTransform subStatsLayoutRoot;

    [Tooltip("Danh sách các dòng chỉ số phụ SubStats (tối đa 4)")]
    [SerializeField] private List<SubStatRowUI> subStatRows = new List<SubStatRowUI>();

    [Header("=== CẤU TRÚC PHÙ PHÉP (ENCHANTMENTS) ===")]
    [Tooltip("Container tổng của ItemEnchantment")]
    [SerializeField] private RectTransform itemEnchantmentContainer;

    [Tooltip("Đường kẻ phân cách Line (3) của ItemEnchantment")]
    [SerializeField] private GameObject enchantmentDivider;

    [Tooltip("Root của EnchantmentLayout")]
    [SerializeField] private RectTransform enchantmentLayoutRoot;

    [Tooltip("Danh sách các dòng Enchantment (tối đa 3)")]
    [SerializeField] private List<EnchantRowUI> enchantRows = new List<EnchantRowUI>();

    [Header("=== TOOLTIP GIẢI THÍCH (ENCHANT HINT) ===")]
    [Tooltip("RectTransform của EnchantHint")]
    [SerializeField] private RectTransform enchantHintRect;

    [Tooltip("CanvasGroup của EnchantHint")]
    [SerializeField] private CanvasGroup enchantHintCanvasGroup;

    [Tooltip("Text hiển thị nội dung giải thích trong EnchantHint")]
    [SerializeField] private TextMeshProUGUI enchantHintText;

    [Header("=== CÁC NÚT HÀNH ĐỘNG ===")]
    [Tooltip("Nút Trang bị")]
    [SerializeField] private Button btnEquip;

    [Tooltip("Nút Tháo ra (nằm đè cùng vị trí với BtnEquip)")]
    [SerializeField] private Button btnUnequip;

    [Tooltip("Nút Sử dụng (cho bình máu / vật phẩm tiêu hao)")]
    [SerializeField] private Button btnUse;

    [Tooltip("Nút Vứt bỏ vật phẩm ra sàn")]
    [SerializeField] private Button btnDrop;

    [Tooltip("Nút Bán vật phẩm trong Cửa Hàng")]
    [SerializeField] private Button btnSell;

    [Tooltip("Nút Bỏ vào rèn/cường hóa trong Lò Rèn (Blacksmith)")]
    [SerializeField] private Button btnPutIn;

    [Header("=== HIỆU ỨNG HOẠT ẢNH POPUP (SOULS-LIKE) ===")]
    [Tooltip("Thời lượng mở popup (giây)")]
    [SerializeField] private float openDuration = 0.10f;

    [Tooltip("Thời lượng trượt giữa các ô đồ khi đang mở sẵn (giây)")]
    [SerializeField] private float slideDuration = 0.08f;

    [Tooltip("Thời lượng đóng popup (giây)")]
    [SerializeField] private float closeDuration = 0.07f;

    // Trạng thái hiện tại
    private InventorySlotUI currentBagSlot;
    private EquipmentSlotUI currentEquipSlot;
    private ItemData currentItem;
    private bool isEquippedTarget = false;
    private bool isInitialized = false;

    private CanvasGroup popupCanvasGroup;
    private Coroutine activeTransitionRoutine;
    private RectTransform popupRectTransform;
    private int lastOpenedFrame = -1;

    // Cache các EnchantmentEntry đang hiển thị để hiển thị Hint khi hover
    private readonly EnchantmentEntry[] activeEnchantmentsCache = new EnchantmentEntry[3];
    private Coroutine hintFadeRoutine;

    public InventorySlotUI CurrentBagSlot => currentBagSlot;
    public EquipmentSlotUI CurrentEquipSlot => currentEquipSlot;

    // Chế độ Cửa Hàng (Shop Sell Mode)
    private bool isShopSellMode = false;
    public bool IsShopSellMode => isShopSellMode;
    public void SetShopSellMode(bool enable) { isShopSellMode = enable; isBlacksmithMode = false; }

    // Chế độ Lò Rèn (Blacksmith Mode)
    private bool isBlacksmithMode = false;
    public bool IsBlacksmithMode => isBlacksmithMode;
    public void SetBlacksmithMode(bool enable) { isBlacksmithMode = enable; isShopSellMode = false; isPreviewMode = false; }

    // Chế độ Xem Trước Chỉ Đọc (Preview Mode: ẩn toàn bộ nút tương tác)
    private bool isPreviewMode = false;
    public bool IsPreviewMode => isPreviewMode;
    public void SetPreviewMode(bool enable) { isPreviewMode = enable; }

    // Callback thông báo lại cho InventoryUIController
    public event Action OnActionCompleted;
    public event Action<InventorySlotUI, ItemData> OnSellClicked;
    public event Action<InventorySlotUI, ItemData> OnPutInClicked;

    private void Awake()
    {
      EnsureInitialized();
    }

    private void EnsureInitialized()
    {
      if (isInitialized) return;
      isInitialized = true;

      AutoFindReferences();
      if (itemIcon != null) itemIcon.gameObject.SetActive(false);
      if (rarityBg != null) rarityBg.gameObject.SetActive(false);
      if (rarityBar != null) rarityBar.gameObject.SetActive(false);
      if (iconContainer != null) iconContainer.SetActive(false);
      HideEnchantHintImmediate();

      if (btnEquip != null)
      {
        btnEquip.onClick.RemoveListener(HandleEquip);
        btnEquip.onClick.AddListener(HandleEquip);
      }

      if (btnUnequip != null)
      {
        btnUnequip.onClick.RemoveListener(HandleUnequip);
        btnUnequip.onClick.AddListener(HandleUnequip);
      }

      if (btnUse != null)
      {
        btnUse.onClick.RemoveListener(HandleUse);
        btnUse.onClick.AddListener(HandleUse);
      }

      if (btnDrop != null)
      {
        btnDrop.onClick.RemoveListener(HandleDrop);
        btnDrop.onClick.AddListener(HandleDrop);
      }

      if (btnSell != null)
      {
        btnSell.onClick.RemoveListener(HandleDrop);
        btnSell.onClick.RemoveListener(HandleSell);
        btnSell.onClick.AddListener(HandleSell);
      }

      if (btnPutIn != null)
      {
        btnPutIn.onClick.RemoveListener(HandlePutIn);
        btnPutIn.onClick.AddListener(HandlePutIn);
      }
    }

    private void AutoFindReferences()
    {
      if (itemNameText == null) itemNameText = transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
      if (upgradeLevelText == null) upgradeLevelText = transform.Find("UpgradeLevel")?.GetComponent<TextMeshProUGUI>();
      if (itemRarityText == null) itemRarityText = transform.Find("ItemTypeRow/ItemRarity")?.GetComponent<TextMeshProUGUI>();
      if (itemTypeText == null) itemTypeText = transform.Find("ItemTypeRow/ItemType")?.GetComponent<TextMeshProUGUI>();
      if (iconContainer == null) iconContainer = transform.Find("Icon")?.gameObject;

      if (itemIcon == null)
      {
        itemIcon = transform.Find("Icon/ItemIcon")?.GetComponent<Image>() ?? transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon == null)
        {
          foreach (var img in GetComponentsInChildren<Image>(true))
          {
            if (img.gameObject.name == "ItemIcon") { itemIcon = img; break; }
          }
        }
      }

      if (rarityBg == null)
      {
        rarityBg = transform.Find("Icon/RarityBG")?.GetComponent<Image>() ?? transform.Find("RarityBG")?.GetComponent<Image>();
        if (rarityBg == null)
        {
          foreach (var img in GetComponentsInChildren<Image>(true))
          {
            if (img.gameObject.name == "RarityBG") { rarityBg = img; break; }
          }
        }
      }

      if (rarityBar == null)
      {
        rarityBar = transform.Find("Icon/RarityBar")?.GetComponent<Image>() ?? transform.Find("RarityBar")?.GetComponent<Image>();
        if (rarityBar == null)
        {
          foreach (var img in GetComponentsInChildren<Image>(true))
          {
            if (img.gameObject.name == "RarityBar") { rarityBar = img; break; }
          }
        }
      }

      if (itemDescriptionText == null) itemDescriptionText = transform.Find("ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();
      if (itemStatsText == null) itemStatsText = transform.Find("ItemStatsText")?.GetComponent<TextMeshProUGUI>();

      // Cụm MainStats & SubStatsLayout
      if (itemStatsTextContainer == null) itemStatsTextContainer = transform.Find("ItemStatsText") as RectTransform;
      if (mainStatsRoot == null) mainStatsRoot = transform.Find("ItemStatsText/MainStats")?.gameObject;
      if (mainStatLabelText == null) mainStatLabelText = transform.Find("ItemStatsText/MainStats")?.GetComponent<TextMeshProUGUI>();
      if (mainStatValueText == null) mainStatValueText = transform.Find("ItemStatsText/MainStats/Value")?.GetComponent<TextMeshProUGUI>();
      if (subStatsDivider == null) subStatsDivider = transform.Find("ItemStatsText/Line (2)")?.gameObject;
      if (subStatsLayoutRoot == null) subStatsLayoutRoot = transform.Find("ItemStatsText/SubStatsLayout") as RectTransform;

      if (subStatRows == null || subStatRows.Count < 4)
      {
        subStatRows = new List<SubStatRowUI>();
        string[] subNames = new[] { "SubStats", "SubStats (1)", "SubStats (2)", "SubStats (3)" };
        for (int i = 0; i < subNames.Length; i++)
        {
          Transform t = transform.Find($"ItemStatsText/SubStatsLayout/{subNames[i]}");
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

      // Cụm ItemEnchantment
      if (itemEnchantmentContainer == null) itemEnchantmentContainer = transform.Find("ItemEnchantment") as RectTransform;
      if (enchantmentDivider == null) enchantmentDivider = transform.Find("ItemEnchantment/Line (3)")?.gameObject;
      if (enchantmentLayoutRoot == null) enchantmentLayoutRoot = transform.Find("ItemEnchantment/EnchantmentLayout") as RectTransform;

      if (enchantRows == null || enchantRows.Count < 3)
      {
        enchantRows = new List<EnchantRowUI>();
        string[] enchNames = new[] { "Enchant", "Enchant (1)", "Enchant (2)" };
        for (int i = 0; i < enchNames.Length; i++)
        {
          Transform t = transform.Find($"ItemEnchantment/EnchantmentLayout/{enchNames[i]}");
          if (t != null)
          {
            var row = new EnchantRowUI
            {
              root = t.gameObject,
              iconActive = t.Find("IconActive")?.GetComponent<Image>(),
              iconUnactive = t.Find("IconUnactive")?.GetComponent<Image>(),
              text = t.Find("EnchantmentText")?.GetComponent<TextMeshProUGUI>()
            };

            var trigger = t.GetComponent<EnchantRowHoverTrigger>();
            if (trigger == null) trigger = t.gameObject.AddComponent<EnchantRowHoverTrigger>();
            trigger.rowIndex = i;
            trigger.onHoverEnter = HandleEnchantRowHoverEnter;
            trigger.onHoverExit = HandleEnchantRowHoverExit;
            row.hoverTrigger = trigger;

            enchantRows.Add(row);
          }
        }
      }
      else
      {
        for (int i = 0; i < enchantRows.Count; i++)
        {
          if (enchantRows[i]?.root != null)
          {
            var trigger = enchantRows[i].root.GetComponent<EnchantRowHoverTrigger>();
            if (trigger == null) trigger = enchantRows[i].root.AddComponent<EnchantRowHoverTrigger>();
            trigger.rowIndex = i;
            trigger.onHoverEnter = HandleEnchantRowHoverEnter;
            trigger.onHoverExit = HandleEnchantRowHoverExit;
            enchantRows[i].hoverTrigger = trigger;
          }
        }
      }

      // Cụm EnchantHint
      if (enchantHintRect == null) enchantHintRect = transform.Find("EnchantHint") as RectTransform;
      if (enchantHintCanvasGroup == null) enchantHintCanvasGroup = transform.Find("EnchantHint")?.GetComponent<CanvasGroup>();
      if (enchantHintText == null) enchantHintText = transform.Find("EnchantHint/Text")?.GetComponent<TextMeshProUGUI>();

      if (enchantHintCanvasGroup != null)
      {
        enchantHintCanvasGroup.alpha = 0f;
        enchantHintCanvasGroup.blocksRaycasts = false;
        enchantHintCanvasGroup.interactable = false;
      }

      // Action Buttons
      if (btnDrop != null && btnDrop.gameObject.name == "BtnSell")
      {
        if (btnSell == null) btnSell = btnDrop;
        btnDrop = null;
      }
      if (btnSell != null && btnSell.gameObject.name == "BtnDrop")
      {
        if (btnDrop == null) btnDrop = btnSell;
        btnSell = null;
      }

      if (btnEquip == null) btnEquip = transform.Find("ActionButton/BtnEquip")?.GetComponent<Button>();
      if (btnUnequip == null) btnUnequip = transform.Find("ActionButton/BtnUnequip")?.GetComponent<Button>();
      if (btnUse == null) btnUse = transform.Find("ActionButton/BtnUse")?.GetComponent<Button>();
      if (btnDrop == null) btnDrop = transform.Find("ActionButton/BtnDrop")?.GetComponent<Button>();
      if (btnSell == null) btnSell = transform.Find("ActionButton/BtnSell")?.GetComponent<Button>() ?? transform.Find("BtnSell")?.GetComponent<Button>();
      if (btnPutIn == null) btnPutIn = transform.Find("ActionButton/BtnPutIn")?.GetComponent<Button>() ?? transform.Find("BtnPutIn")?.GetComponent<Button>();

      if (popupCanvasGroup == null)
      {
        popupCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
      }
      if (popupRectTransform == null)
      {
        popupRectTransform = transform as RectTransform;
      }
    }

    private void OnDisable()
    {
      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }
      HideEnchantHintImmediate();
    }

    private void Update()
    {
      if (btnUse != null && btnUse.gameObject.activeSelf && InventoryManager.HasInstance)
      {
        btnUse.interactable = !InventoryManager.Instance.IsConsumableOnCooldown;
      }

      CheckClickOutside();
    }

    /// <summary>
    /// Đóng popup khi click chuột ra bên ngoài khung chi tiết (trừ trường hợp click vào ô đồ khác)
    /// </summary>
    private void CheckClickOutside()
    {
      if (!gameObject.activeSelf) return;

      if (popupCanvasGroup != null && popupCanvasGroup.alpha < 0.05f) return;
      if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
      if (Time.frameCount == lastOpenedFrame) return;
      if (InventoryDragDropController.Instance != null && InventoryDragDropController.Instance.IsDragging) return;

      Vector2 mousePos = Mouse.current.position.ReadValue();
      if (popupRectTransform == null) popupRectTransform = transform as RectTransform;

      Canvas rootCanvas = GetComponentInParent<Canvas>();
      Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

      if (popupRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(popupRectTransform, mousePos, uiCam))
      {
        return;
      }

      if (EventSystem.current != null)
      {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
          position = mousePos
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
          GameObject hitObj = raycastResults[i].gameObject;
          if (hitObj == null) continue;

          if (hitObj.GetComponentInParent<InventorySlotUI>() != null ||
              hitObj.GetComponentInParent<EquipmentSlotUI>() != null ||
              hitObj.name == "ForgeSlot" || hitObj.transform.parent?.name == "ForgeSlot")
          {
            return;
          }
        }
      }

      Hide();
    }

    /// <summary>
    /// Hiển thị popup cho 1 ô đồ trong túi và đặt vị trí bám theo ô đó
    /// </summary>
    public void ShowForBagSlot(InventorySlotUI slot)
    {
      EnsureInitialized();

      if (slot == null || slot.IsEmpty)
      {
        Hide();
        return;
      }

      if (currentBagSlot != null) currentBagSlot.SetSelected(false);
      if (currentEquipSlot != null) currentEquipSlot.SetSelected(false);

      currentBagSlot = slot;
      currentEquipSlot = null;
      currentItem = slot.CurrentItem;
      isEquippedTarget = false;
      isPreviewMode = false;

      currentBagSlot.SetSelected(true);
      UpdateUIContent();
      lastOpenedFrame = Time.frameCount;
      ShowNearSlotAnimated(slot.transform as RectTransform);
    }

    /// <summary>
    /// Hiển thị popup cho 1 ô đang trang bị trên người (Weapon/Armor) và đặt vị trí bám theo ô đó
    /// </summary>
    public void ShowForEquipmentSlot(EquipmentSlotUI eqSlot)
    {
      EnsureInitialized();

      if (eqSlot == null || eqSlot.IsEmpty)
      {
        Hide();
        return;
      }

      if (currentBagSlot != null) currentBagSlot.SetSelected(false);
      if (currentEquipSlot != null) currentEquipSlot.SetSelected(false);

      currentEquipSlot = eqSlot;
      currentBagSlot = null;
      currentItem = eqSlot.EquippedItem;
      isEquippedTarget = true;
      isPreviewMode = false;

      currentEquipSlot.SetSelected(true);
      UpdateUIContent();
      lastOpenedFrame = Time.frameCount;
      ShowNearSlotAnimated(eqSlot.transform as RectTransform);
    }

    private EquipmentAffixData previewAffix = null;

    /// <summary>
    /// Hiển thị popup xem trước thông tin vật phẩm (chế độ chỉ đọc, ẩn toàn bộ nút tương tác phía dưới)
    /// </summary>
    public void ShowPreview(ItemData item, RectTransform slotRect, EquipmentAffixData affix = null)
    {
      EnsureInitialized();

      if (item == null)
      {
        Hide();
        return;
      }

      if (gameObject.activeSelf && popupCanvasGroup != null && popupCanvasGroup.alpha > 0.1f && isPreviewMode && currentItem == item)
      {
        Hide();
        return;
      }

      if (currentBagSlot != null) currentBagSlot.SetSelected(false);
      if (currentEquipSlot != null) currentEquipSlot.SetSelected(false);

      currentBagSlot = null;
      currentEquipSlot = null;
      currentItem = item;
      previewAffix = affix;
      isEquippedTarget = false;
      isPreviewMode = true;

      UpdateUIContent();
      lastOpenedFrame = Time.frameCount;

      if (slotRect != null)
      {
        ShowNearSlotAnimated(slotRect);
      }
    }

    /// <summary>
    /// Hiển thị và định vị popup với hoạt ảnh Souls-like (Trượt mượt nếu đang mở sẵn, Pop card nếu mở mới)
    /// </summary>
    private void ShowNearSlotAnimated(RectTransform slotRect)
    {
      if (slotRect == null) return;

      if (!CalculatePositionNearSlot(slotRect, out Vector2 targetPivot, out Vector2 targetPos))
      {
        return;
      }

      RectTransform popupRect = transform as RectTransform;
      if (popupRect == null) return;

      bool isAlreadyShowing = gameObject.activeSelf && (popupCanvasGroup != null && popupCanvasGroup.alpha > 0.1f);

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      gameObject.SetActive(true);
      if (popupCanvasGroup != null)
      {
        popupCanvasGroup.blocksRaycasts = true;
        popupCanvasGroup.interactable = true;
      }

      if (isAlreadyShowing && popupRect.pivot == targetPivot)
      {
        activeTransitionRoutine = StartCoroutine(SlideToSlotRoutine(targetPos, slideDuration));
      }
      else
      {
        popupRect.pivot = targetPivot;
        popupRect.anchoredPosition = targetPos;
        activeTransitionRoutine = StartCoroutine(AnimateOpenRoutine(openDuration));
      }
    }

    /// <summary>
    /// Tự động tính toán vị trí bám theo ô đồ thông minh (Hàng trên hiện từ trên xuống, hàng dưới hiện từ dưới lên, hàng giữa căn giữa màn hình)
    /// </summary>
    private bool CalculatePositionNearSlot(RectTransform slotRect, out Vector2 targetPivot, out Vector2 targetPos)
    {
      targetPivot = new Vector2(0f, 1f);
      targetPos = Vector2.zero;

      if (slotRect == null) return false;

      RectTransform popupRect = transform as RectTransform;
      RectTransform parentRect = transform.parent as RectTransform;
      if (popupRect == null || parentRect == null) return false;

      Canvas canvas = GetComponentInParent<Canvas>();
      Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

      // 1. Lấy khung viền ô đồ trên màn hình (Screen Space)
      Vector3[] slotCorners = new Vector3[4];
      slotRect.GetWorldCorners(slotCorners);

      Vector2 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[0]);
      Vector2 screenTopLeft = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[1]);
      Vector2 screenTopRight = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[2]);
      Vector2 screenBottomRight = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[3]);

      float slotScreenLeft = screenBottomLeft.x;
      float slotScreenRight = screenBottomRight.x;
      float slotScreenTop = screenTopLeft.y;
      float slotScreenBottom = screenBottomLeft.y;
      float slotScreenCenterY = (slotScreenTop + slotScreenBottom) * 0.5f;

      float popupWidth = popupRect.rect.width > 0f ? popupRect.rect.width : 370f;
      float popupHeight = popupRect.rect.height > 0f ? popupRect.rect.height : 550f;

      float padding = 15f;
      float screenW = Screen.width;
      float screenH = Screen.height;

      // 2. TÍNH TOÁN TRỤC HOÀNH (X-AXIS):
      // Ưu tiên hiển thị sang bên phải ô đồ
      float targetScreenX;
      float pivotX;

      if (slotScreenRight + 10f + popupWidth <= screenW - padding)
      {
        pivotX = 0f;
        targetScreenX = slotScreenRight + 10f;
      }
      else if (slotScreenLeft - 10f - popupWidth >= padding)
      {
        pivotX = 1f;
        targetScreenX = slotScreenLeft - 10f;
      }
      else
      {
        if (slotScreenLeft > screenW - slotScreenRight)
        {
          pivotX = 1f;
          targetScreenX = Mathf.Max(padding + popupWidth, slotScreenLeft - 10f);
        }
        else
        {
          pivotX = 0f;
          targetScreenX = Mathf.Min(screenW - padding - popupWidth, slotScreenRight + 10f);
        }
      }

      // 3. TÍNH TOÁN TRỤC TUNG (Y-AXIS) THEO 3 TRƯỜNG HỢP:
      float targetScreenY;
      float pivotY;

      bool canFitDown = (slotScreenTop - popupHeight >= padding);
      bool canFitUp = (slotScreenBottom + popupHeight <= screenH - padding);

      if (canFitDown && (!canFitUp || slotScreenCenterY > screenH * 0.55f))
      {
        // TRƯỜNG HỢP 1: Ô ở hàng trên -> Mở từ trên xuống dưới
        pivotY = 1f;
        targetScreenY = Mathf.Min(slotScreenTop, screenH - padding);
      }
      else if (canFitUp && (!canFitDown || slotScreenCenterY < screenH * 0.45f))
      {
        // TRƯỜNG HỢP 2: Ô ở hàng dưới -> Mở từ dưới lên trên
        pivotY = 0f;
        targetScreenY = Mathf.Max(slotScreenBottom, padding);
      }
      else
      {
        // TRƯỜNG HỢP 3: Ô ở hàng giữa (hoặc popup quá dài không đủ mở thuần trên/dưới)
        // -> Căn giữa và clamp an toàn trong màn hình
        pivotY = 0.5f;

        float minCenterY = padding + (popupHeight * 0.5f);
        float maxCenterY = screenH - padding - (popupHeight * 0.5f);

        if (minCenterY <= maxCenterY)
        {
          targetScreenY = Mathf.Clamp(slotScreenCenterY, minCenterY, maxCenterY);
        }
        else
        {
          targetScreenY = screenH * 0.5f;
        }
      }

      targetPivot = new Vector2(pivotX, pivotY);

      // 4. Chuyển đổi tọa độ màn hình sang Local Point trong parentRect
      Vector2 targetScreenPoint = new Vector2(targetScreenX, targetScreenY);
      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPoint, cam, out targetPos))
      {
        return false;
      }

      return true;
    }

    private IEnumerator SlideToSlotRoutine(Vector2 targetPos, float duration)
    {
      RectTransform popupRect = transform as RectTransform;
      Vector2 startPos = popupRect.anchoredPosition;
      Vector3 startScale = Vector3.one * 0.96f;
      Vector3 endScale = Vector3.one;

      if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseOutQuad(t);

        popupRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, e);
        popupRect.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
        yield return null;
      }

      popupRect.anchoredPosition = targetPos;
      popupRect.localScale = Vector3.one;
      activeTransitionRoutine = null;
    }

    private IEnumerator AnimateOpenRoutine(float duration)
    {
      RectTransform popupRect = transform as RectTransform;
      Vector3 startScale = Vector3.one * 0.92f;
      Vector3 endScale = Vector3.one;

      if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
      popupRect.localScale = startScale;

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseOutCubic(t);

        popupRect.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
        if (popupCanvasGroup != null)
        {
          popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, e);
        }
        yield return null;
      }

      popupRect.localScale = Vector3.one;
      if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;
      activeTransitionRoutine = null;
    }

    private IEnumerator AnimateCloseRoutine(float duration)
    {
      RectTransform popupRect = transform as RectTransform;
      Vector3 startScale = popupRect != null ? popupRect.localScale : Vector3.one;
      Vector3 endScale = Vector3.one * 0.95f;
      float startAlpha = popupCanvasGroup != null ? popupCanvasGroup.alpha : 1f;

      if (popupCanvasGroup != null)
      {
        popupCanvasGroup.blocksRaycasts = false;
        popupCanvasGroup.interactable = false;
      }

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseInQuad(t);

        if (popupRect != null)
        {
          popupRect.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
        }
        if (popupCanvasGroup != null)
        {
          popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, e);
        }
        yield return null;
      }

      if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
      if (popupRect != null) popupRect.localScale = Vector3.one;
      if (itemIcon != null) itemIcon.gameObject.SetActive(false);
      if (rarityBg != null) rarityBg.gameObject.SetActive(false);
      if (rarityBar != null) rarityBar.gameObject.SetActive(false);
      if (iconContainer != null) iconContainer.SetActive(false);
      HideEnchantHintImmediate();
      gameObject.SetActive(false);
      activeTransitionRoutine = null;
    }

    private void UpdateUIContent()
    {
      if (currentItem == null)
      {
        Hide();
        return;
      }

      // 1. Tên vật phẩm với gradient phẩm cấp
      Color rarityColor = currentItem.Rarity.GetRarityColor();
      if (itemNameText != null)
      {
        itemNameText.color = Color.white;
        itemNameText.text = currentItem.Rarity.FormatWithGradient(currentItem.ItemName);
      }

      // 2. Độ hiếm (ItemRarity text)
      if (itemRarityText != null)
      {
        itemRarityText.color = Color.white;
        itemRarityText.text = currentItem.Rarity.FormatWithGradient(currentItem.Rarity.GetRarityName());
      }

      // 3. Phân loại (ItemType text)
      if (itemTypeText != null)
      {
        itemTypeText.text = GetItemTypeName(currentItem.ItemType);
      }

      // 4. Icon & Rarity Visuals
      if (iconContainer != null)
      {
        iconContainer.SetActive(true);
      }

      if (itemIcon != null)
      {
        if (currentItem.Icon != null)
        {
          itemIcon.gameObject.SetActive(true);
          itemIcon.enabled = true;
          itemIcon.sprite = currentItem.Icon;
          itemIcon.color = Color.white;
        }
        else
        {
          itemIcon.gameObject.SetActive(false);
        }
      }

      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(true);
        rarityBg.color = rarityColor;
      }

      if (rarityBar != null)
      {
        rarityBar.gameObject.SetActive(true);
        rarityBar.color = rarityColor;
      }

      // 5. Mô tả
      if (itemDescriptionText != null)
      {
        itemDescriptionText.text = currentItem.Description;
      }

      // 6. Thống kê chỉ số (MainStats, SubStatsLayout, ItemEnchantment & Dynamic Height)
      int upgradeLvl = 0;
      EquipmentAffixData affix = previewAffix;
      if (currentBagSlot != null)
      {
        upgradeLvl = currentBagSlot.UpgradeLevel;
        affix = currentBagSlot.AffixData;
      }
      else if (currentEquipSlot != null)
      {
        upgradeLvl = currentEquipSlot.UpgradeLevel;
        affix = currentEquipSlot.AffixData;
      }

      PopulateStatsAndEnchantments(currentItem, upgradeLvl, affix);

      // Legacy fallback
      if (itemStatsText != null && itemStatsText.gameObject.activeSelf)
      {
        itemStatsText.text = BuildStatsString(currentItem, upgradeLvl, affix);
      }

      // 7. Cấu hình trạng thái các nút hành động
      ConfigureActionButtons();
    }

    /// <summary>
    /// Điền dữ liệu vào MainStats, SubStatsLayout (1-4 dòng) và ItemEnchantment (1-3 dòng), đồng thời co giãn chiều cao popup
    /// </summary>
    private void PopulateStatsAndEnchantments(ItemData item, int upgradeLevel, EquipmentAffixData affix)
    {
      HideEnchantHintImmediate();

      // 1. Upgrade Level indicator ở góc trên bên phải (+1, +3, +5...)
      if (upgradeLevelText != null)
      {
        if (upgradeLevel > 0)
        {
          upgradeLevelText.gameObject.SetActive(true);
          upgradeLevelText.text = $"+ {upgradeLevel}";
        }
        else
        {
          upgradeLevelText.gameObject.SetActive(false);
        }
      }

      bool isEquipment = item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor;

      if (!isEquipment)
      {
        // === VẬT PHẨM THƯỜNG (Consumable / Material / Currency) ===
        // 1. Tắt hoàn toàn cửa sổ Enchantment
        if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++) activeEnchantmentsCache[i] = null;
        for (int i = 0; i < enchantRows.Count; i++)
        {
          if (enchantRows[i]?.root != null) enchantRows[i].root.SetActive(false);
        }

        // 2. Để trống dòng MainStats (theo yêu cầu: không dùng MainStats hiển thị công dụng/mô tả/value)
        if (mainStatsRoot != null) mainStatsRoot.SetActive(false);
        if (mainStatLabelText != null) mainStatLabelText.text = "";
        if (mainStatValueText != null) mainStatValueText.text = "";

        // 3. Đưa mô tả công dụng / đặc tính của vật phẩm xuống khu vực SubStats
        var suppliesSubList = new List<(string label, string val)>();

        if (item.ItemType == ItemType.Consumable)
        {
          if (item.HealAmount > 0f)
          {
            suppliesSubList.Add(("HP Recovery", $"<color=#77FF77>+{item.HealAmount:0.#} HP</color>"));
            suppliesSubList.Add(("Effect", "Instant Restore"));
          }
          else if (item.BonusDefense > 0f)
          {
            suppliesSubList.Add(("Defense Boost", $"<color=#81D4FA>+{item.BonusDefense:0.#} DEF</color>"));
            suppliesSubList.Add(("Duration", "30 Seconds"));
          }
          else if (item.BonusAttack > 0f)
          {
            suppliesSubList.Add(("Attack Boost", $"<color=#FFB74D>+{item.BonusAttack:0.#} ATK</color>"));
            suppliesSubList.Add(("Duration", "30 Seconds"));
          }
          else if (item.BonusStamina > 0f)
          {
            suppliesSubList.Add(("Stamina Boost", $"<color=#81C784>+{item.BonusStamina:0.#} Max</color>"));
            suppliesSubList.Add(("Effect", "Regen Boost"));
          }
          else if (item.BonusHealth > 0f)
          {
            suppliesSubList.Add(("Health Boost", $"<color=#77FF77>+{item.BonusHealth:0.#} Max HP</color>"));
            suppliesSubList.Add(("Effect", "Increases Max HP"));
          }
          else if (!string.IsNullOrEmpty(item.Id) && item.Id.Contains("spirit"))
          {
            suppliesSubList.Add(("Spirit Recovery", "<color=#BA68C8>Instant Spirit</color>"));
            suppliesSubList.Add(("Effect", "Replenish Skills"));
          }
          else
          {
            suppliesSubList.Add(("Item Type", "Consumable"));
          }
        }
        else if (item.ItemType == ItemType.Material)
        {
          if (!string.IsNullOrEmpty(item.Id) && (item.Id.Contains("upgrade_stone") || item.Id.Contains("stone")))
          {
            suppliesSubList.Add(("Usage", "<color=#FFE082>Gear Upgrade</color>"));
            suppliesSubList.Add(("Target", "Haven Forge"));
            suppliesSubList.Add(("Upgrade Range", "+1 to +5"));
          }
          else
          {
            suppliesSubList.Add(("Usage", "<color=#FFE082>Crafting Material</color>"));
          }

          // Hiển thị giá trị vàng cơ bản nếu có
          if (item.GoldValue > 0 && suppliesSubList.Count < 3)
          {
            suppliesSubList.Add(("Base Value", $"{item.GoldValue} Gold"));
          }
        }
        else // Currency hoặc loại khác
        {
          if (item.GoldValue > 0)
          {
            suppliesSubList.Add(("Value", $"{item.GoldValue} Gold"));
          }
          else
          {
            suppliesSubList.Add(("Item Type", item.ItemType.ToString()));
          }
        }

        // Bổ sung thông tin Max Stack nếu còn dòng trống (định dạng gọn xN chuẩn RPG)
        int maxAvailableRows = subStatRows != null ? subStatRows.Count : 4;
        if (item.MaxStack > 1 && suppliesSubList.Count < maxAvailableRows)
        {
          suppliesSubList.Add(("Max Stack", $"x{item.MaxStack}"));
        }

        int activeSuppliesCount = Mathf.Min(suppliesSubList.Count, 4);
        if (activeSuppliesCount > 0)
        {
          if (subStatsDivider != null) subStatsDivider.SetActive(true);
          if (subStatsLayoutRoot != null)
          {
            subStatsLayoutRoot.gameObject.SetActive(true);
            subStatsLayoutRoot.pivot = new Vector2(0.5f, 1f);
            subStatsLayoutRoot.anchorMin = new Vector2(0.5f, 1f);
            subStatsLayoutRoot.anchorMax = new Vector2(0.5f, 1f);
            subStatsLayoutRoot.anchoredPosition = new Vector2(0f, -46f);
            subStatsLayoutRoot.sizeDelta = new Vector2(304.6f, activeSuppliesCount * 25f);
          }

          for (int i = 0; i < subStatRows.Count; i++)
          {
            if (subStatRows[i] == null || subStatRows[i].root == null) continue;

            if (i < activeSuppliesCount)
            {
              subStatRows[i].root.SetActive(true);
              if (subStatRows[i].labelText != null)
              {
                subStatRows[i].labelText.text = suppliesSubList[i].label;
                subStatRows[i].labelText.enableWordWrapping = false;
                subStatRows[i].labelText.enableAutoSizing = true;
                subStatRows[i].labelText.fontSizeMin = 12f;
                subStatRows[i].labelText.fontSizeMax = 20f;
              }
              if (subStatRows[i].valueText != null)
              {
                subStatRows[i].valueText.text = suppliesSubList[i].val;
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
        else
        {
          if (subStatsDivider != null) subStatsDivider.SetActive(false);
          if (subStatsLayoutRoot != null) subStatsLayoutRoot.gameObject.SetActive(false);
          for (int i = 0; i < subStatRows.Count; i++)
          {
            if (subStatRows[i]?.root != null) subStatRows[i].root.SetActive(false);
          }
        }

        // 4. Kéo dãn chiều cao linh hoạt (Dynamic Height Scaling)
        float suppliesStatsHeight = 42f;
        if (activeSuppliesCount > 0)
        {
          suppliesStatsHeight = 46f + (activeSuppliesCount * 25f) + 8f;
        }

        if (itemStatsTextContainer != null)
        {
          itemStatsTextContainer.gameObject.SetActive(true);
          itemStatsTextContainer.pivot = new Vector2(0.5f, 1f);
          itemStatsTextContainer.anchorMin = new Vector2(0.5f, 1f);
          itemStatsTextContainer.anchorMax = new Vector2(0.5f, 1f);
          itemStatsTextContainer.sizeDelta = new Vector2(304.6f, suppliesStatsHeight);
          itemStatsTextContainer.anchoredPosition = new Vector2(0f, -283.4f);
        }

        float suppliesStatsBottomY = -283.4f - suppliesStatsHeight;
        float contentBottomY = suppliesStatsBottomY;
        float totalHeight = Mathf.Abs(contentBottomY) + 195f;
        if (popupRectTransform != null)
        {
          popupRectTransform.sizeDelta = new Vector2(370f, Mathf.Clamp(totalHeight, 460f, 820f));
        }
        return;
      }

      // === TRANG BỊ (Weapon hoặc Armor) ===
      if (itemStatsTextContainer != null) itemStatsTextContainer.gameObject.SetActive(true);
      if (mainStatsRoot != null) mainStatsRoot.SetActive(true);

      // 1. CHỈ SỐ CHÍNH (MAIN STATS)
      if (item.ItemType == ItemType.Weapon)
      {
        float baseAtk = (affix != null && affix.rolledAttack > 0f) ? affix.rolledAttack : item.BonusAttack;
        if (baseAtk <= 0f) baseAtk = 10f;
        float finalAtk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, upgradeLevel);

        if (mainStatLabelText != null) mainStatLabelText.text = "Attack Damage";
        if (mainStatValueText != null) mainStatValueText.text = finalAtk.ToString("0.#");
      }
      else // Armor
      {
        // Giáp có 1 dòng chính duy nhất: Hoặc Defense, hoặc Max Health
        bool isDefense = (affix != null) ? (affix.rolledDefense > 0f) : (item.BonusDefense > 0f);

        if (isDefense)
        {
          float baseDef = (affix != null && affix.rolledDefense > 0f) ? affix.rolledDefense : item.BonusDefense;
          float finalDef = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, upgradeLevel);
          if (mainStatLabelText != null) mainStatLabelText.text = "Defense";
          if (mainStatValueText != null) mainStatValueText.text = finalDef.ToString("0.#");
        }
        else
        {
          float baseHp = (affix != null && affix.rolledHealth > 0f) ? affix.rolledHealth : item.BonusHealth;
          float finalHp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, upgradeLevel);
          if (mainStatLabelText != null) mainStatLabelText.text = "Max Health";
          if (mainStatValueText != null) mainStatValueText.text = finalHp.ToString("0.#");
        }
      }

      // 2. DÒNG PHỤ (SUB-STATS - tối đa 4, 100% Procedural Affixes)
      var subList = new List<(string name, string valStr)>();

      if (affix != null && affix.subStats != null && affix.subStats.Count > 0)
      {
        for (int i = 0; i < affix.subStats.Count && subList.Count < 4; i++)
        {
          var sub = affix.subStats[i];
          float upVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, upgradeLevel);
          var (sName, sVal) = FormatSubStat(sub.type, upVal);
          subList.Add((sName, sVal));
        }
      }

      if (affix == null || affix.subStats == null || affix.subStats.Count == 0)
      {
        // Fallback static bonuses trên ItemData nếu không có affix
        if (item.BonusCritRate > 0f && subList.Count < 4)
        {
          float cr = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritRate, upgradeLevel);
          subList.Add(("Critical Rate", $"+ {cr:0.#}%"));
        }
        if (item.BonusCritDamage > 0f && subList.Count < 4)
        {
          float cd = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritDamage, upgradeLevel);
          subList.Add(("Critical Damage", $"+ {cd:0.#}%"));
        }
        if (item.BonusLifesteal > 0f && subList.Count < 4)
        {
          float ls = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusLifesteal, upgradeLevel);
          subList.Add(("Lifesteal", $"+ {ls:0.#}%"));
        }
        if (item.SpiritCostReduction > 0f && subList.Count < 4)
        {
          subList.Add(("Spirit Cost", $"- {item.SpiritCostReduction:0.#}%"));
        }
        if (item.StaminaCostReduction > 0f && subList.Count < 4)
        {
          subList.Add(("Stamina Cost", $"- {item.StaminaCostReduction:0.#}%"));
        }
        if (item.SpiritGainBonus > 0f && subList.Count < 4)
        {
          subList.Add(("Spirit Gain", $"+ {item.SpiritGainBonus:0.#}%"));
        }
        if (item.MoveSpeedBonus > 0f && subList.Count < 4)
        {
          subList.Add(("Movement Speed", $"+ {item.MoveSpeedBonus:0.#}%"));
        }
      }

      int activeSubCount = Mathf.Min(subList.Count, 4);
      if (activeSubCount > 0)
      {
        if (subStatsDivider != null) subStatsDivider.SetActive(true);
        if (subStatsLayoutRoot != null)
        {
          subStatsLayoutRoot.gameObject.SetActive(true);
          subStatsLayoutRoot.pivot = new Vector2(0.5f, 1f);
          subStatsLayoutRoot.anchorMin = new Vector2(0.5f, 1f);
          subStatsLayoutRoot.anchorMax = new Vector2(0.5f, 1f);
          subStatsLayoutRoot.anchoredPosition = new Vector2(0f, -46f);
          subStatsLayoutRoot.sizeDelta = new Vector2(304.6f, activeSubCount * 25f);
        }

        for (int i = 0; i < subStatRows.Count; i++)
        {
          if (subStatRows[i] == null || subStatRows[i].root == null) continue;
          if (i < activeSubCount)
          {
            subStatRows[i].root.SetActive(true);
            if (subStatRows[i].labelText != null)
            {
              subStatRows[i].labelText.text = subList[i].name;
              subStatRows[i].labelText.enableWordWrapping = false;
            }
            if (subStatRows[i].valueText != null)
            {
              subStatRows[i].valueText.text = subList[i].valStr;
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

      // Chiều cao ItemStatsText
      float statsHeight = 42f;
      if (activeSubCount > 0)
      {
        statsHeight = 46f + (activeSubCount * 25f) + 8f;
      }
      if (itemStatsTextContainer != null)
      {
        itemStatsTextContainer.pivot = new Vector2(0.5f, 1f);
        itemStatsTextContainer.anchorMin = new Vector2(0.5f, 1f);
        itemStatsTextContainer.anchorMax = new Vector2(0.5f, 1f);
        itemStatsTextContainer.sizeDelta = new Vector2(304.6f, statsHeight);
        itemStatsTextContainer.anchoredPosition = new Vector2(0f, -283.4f);
      }
      float statsBottomY = -283.4f - statsHeight;

      // 3. PHÙ PHÉP THỨC TỈNH (ENCHANTMENTS - tối đa 3)
      var enchantList = (affix != null && affix.enchantments != null) ? affix.enchantments : null;
      int activeEnchCount = (enchantList != null) ? Mathf.Min(enchantList.Count, 3) : 0;

      if (activeEnchCount > 0)
      {
        if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(true);
        if (enchantmentDivider != null) enchantmentDivider.SetActive(true);
        if (enchantmentLayoutRoot != null)
        {
          enchantmentLayoutRoot.gameObject.SetActive(true);
          enchantmentLayoutRoot.pivot = new Vector2(0.5f, 1f);
          enchantmentLayoutRoot.anchorMin = new Vector2(0.5f, 1f);
          enchantmentLayoutRoot.anchorMax = new Vector2(0.5f, 1f);
          enchantmentLayoutRoot.anchoredPosition = new Vector2(0f, -14f);
          enchantmentLayoutRoot.sizeDelta = new Vector2(304.6f, activeEnchCount * 45f);
        }

        for (int i = 0; i < enchantRows.Count; i++)
        {
          if (enchantRows[i] == null || enchantRows[i].root == null) continue;
          if (i < activeEnchCount)
          {
            var ench = enchantList[i];
            bool isUnlocked = upgradeLevel >= ench.milestoneLevel;
            ench.isUnlocked = isUnlocked;
            activeEnchantmentsCache[i] = ench;

            enchantRows[i].root.SetActive(true);
            if (enchantRows[i].iconActive != null) enchantRows[i].iconActive.gameObject.SetActive(isUnlocked);
            if (enchantRows[i].iconUnactive != null) enchantRows[i].iconUnactive.gameObject.SetActive(!isUnlocked);

            var (eTitle, _) = GetEnchantmentEnglishInfo(ench.enchantType, ench.title, ench.description);
            if (enchantRows[i].text != null)
            {
              if (isUnlocked)
              {
                enchantRows[i].text.text = $"<color=#E040FB>[+{ench.milestoneLevel}] {eTitle}</color>";
              }
              else
              {
                enchantRows[i].text.text = $"<color=#757575>[+{ench.milestoneLevel}] {eTitle} <size=80%>(Locked)</size></color>";
              }
            }
          }
          else
          {
            enchantRows[i].root.SetActive(false);
            activeEnchantmentsCache[i] = null;
          }
        }

        float enchHeight = 14f + (activeEnchCount * 45f) + 8f;
        float enchTopY = statsBottomY - 6f;
        if (itemEnchantmentContainer != null)
        {
          itemEnchantmentContainer.pivot = new Vector2(0.5f, 1f);
          itemEnchantmentContainer.anchorMin = new Vector2(0.5f, 1f);
          itemEnchantmentContainer.anchorMax = new Vector2(0.5f, 1f);
          itemEnchantmentContainer.sizeDelta = new Vector2(304.6f, enchHeight);
          itemEnchantmentContainer.anchoredPosition = new Vector2(0f, enchTopY);
        }
        float contentBottomY = enchTopY - enchHeight;

        // Tính tổng chiều cao popup cho trang bị có enchant
        float totalHeight = Mathf.Abs(contentBottomY) + 195f;
        if (popupRectTransform != null)
        {
          popupRectTransform.sizeDelta = new Vector2(370f, Mathf.Clamp(totalHeight, 460f, 820f));
        }
      }
      else
      {
        if (itemEnchantmentContainer != null) itemEnchantmentContainer.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++) activeEnchantmentsCache[i] = null;
        for (int i = 0; i < enchantRows.Count; i++)
        {
          if (enchantRows[i]?.root != null) enchantRows[i].root.SetActive(false);
        }

        float contentBottomY = statsBottomY;
        float totalHeight = Mathf.Abs(contentBottomY) + 195f;
        if (popupRectTransform != null)
        {
          popupRectTransform.sizeDelta = new Vector2(370f, Mathf.Clamp(totalHeight, 460f, 820f));
        }
      }
    }

    #region Enchant Hover & Hint Tooltip Logic

    private void HandleEnchantRowHoverEnter(int rowIndex)
    {
      if (rowIndex < 0 || rowIndex >= activeEnchantmentsCache.Length) return;
      var ench = activeEnchantmentsCache[rowIndex];
      if (ench == null) return;

      ShowEnchantHint(ench, rowIndex);
    }

    private void HandleEnchantRowHoverExit(int rowIndex)
    {
      HideEnchantHint();
    }

    private void ShowEnchantHint(EnchantmentEntry ench, int rowIndex = -1)
    {
      if (ench == null || enchantHintCanvasGroup == null || enchantHintText == null) return;

      var (eTitle, eDesc) = GetEnchantmentEnglishInfo(ench.enchantType, ench.title, ench.description);

      // Định dạng ngắn gọn chuẩn yêu cầu: "Tên: Mô tả" (không hiện [+5] hay [Requires+5])
      string hintText = ench.isUnlocked
        ? $"<color=#E040FB>{eTitle}:</color> <color=#EEEEEE>{eDesc}</color>"
        : $"<color=#B0BEC5>{eTitle}:</color> <color=#9E9E9E>{eDesc}</color>";

      enchantHintText.text = hintText;
      enchantHintText.ForceMeshUpdate();

      // Tự động co giãn chiều rộng và chiều cao phù hợp với text
      float preferredW = enchantHintText.preferredWidth;
      float targetW = Mathf.Clamp(preferredW + 28f, 160f, 340f);
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

        // Neo ở giữa để tính tọa độ local từ tâm popup (0, 0)
        enchantHintRect.anchorMin = new Vector2(0.5f, 0.5f);
        enchantHintRect.anchorMax = new Vector2(0.5f, 0.5f);
        enchantHintRect.pivot = new Vector2(0f, 1f); // Góc trái trên (Top-Left)

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRectTransform, mouseScreenPos, uiCam, out Vector2 mouseLocalPos);

        // Lấy tọa độ Y của dòng đang hover để căn chỉnh chuẩn xác theo dòng
        float targetY = mouseLocalPos.y;
        if (rowIndex >= 0 && rowIndex < enchantRows.Count && enchantRows[rowIndex]?.root != null)
        {
          Vector3 rowWorldCenter = enchantRows[rowIndex].root.transform.position;
          Vector3 rowLocalPos = popupRectTransform.InverseTransformPoint(rowWorldCenter);
          RectTransform rRect = enchantRows[rowIndex].root.transform as RectTransform;
          float rowHalfH = (rRect != null) ? rRect.rect.height * 0.5f : 22.5f;
          targetY = rowLocalPos.y + rowHalfH; // Đỉnh hint khớp đỉnh dòng
        }

        // Đặt góc trái (Top-Left) của Hint theo chuột/dòng hover
        float targetX = mouseLocalPos.x;
        float halfPopupW = popupRectTransform.rect.width * 0.5f;

        // Nếu mở sang phải bị tràn mép popup -> mở sang trái hoặc lùi lại
        if (targetX + targetW > halfPopupW - 12f)
        {
          targetX = halfPopupW - 12f - targetW;
        }
        if (targetX < -halfPopupW + 12f)
        {
          targetX = -halfPopupW + 12f;
        }

        // Chống tràn mép dưới / mép trên popup
        float halfPopupH = popupRectTransform.rect.height * 0.5f;
        if (targetY - targetH < -halfPopupH + 12f)
        {
          targetY = -halfPopupH + 12f + targetH;
        }
        if (targetY > halfPopupH - 12f)
        {
          targetY = halfPopupH - 12f;
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
        enchantHintCanvasGroup.interactable = false;
      }
      if (enchantHintRect != null)
      {
        enchantHintRect.gameObject.SetActive(false);
      }
    }

    private IEnumerator FadeHintRoutine(float targetAlpha, float duration)
    {
      float startAlpha = enchantHintCanvasGroup != null ? enchantHintCanvasGroup.alpha : 0f;
      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        if (enchantHintCanvasGroup != null)
        {
          enchantHintCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
        }
        yield return null;
      }
      if (enchantHintCanvasGroup != null)
      {
        enchantHintCanvasGroup.alpha = targetAlpha;
      }
      if (targetAlpha <= 0.01f && enchantHintRect != null)
      {
        enchantHintRect.gameObject.SetActive(false);
      }
      hintFadeRoutine = null;
    }

    #endregion

    #region English Formatting Helpers

    public static (string name, string valStr) FormatSubStat(SubStatType type, float val)
    {
      switch (type)
      {
        case SubStatType.AttackBonus:
          return ("Attack Damage", $"+ {val:0.#}");
        case SubStatType.DefenseBonus:
          return ("Defense", $"+ {val:0.#}");
        case SubStatType.HealthBonus:
          return ("Max Health", $"+ {val:0.#}");
        case SubStatType.MaxStamina:
          return ("Max Stamina", $"+ {val:0.#}");
        case SubStatType.CritRate:
          return ("Critical Rate", $"+ {val:0.#}%");
        case SubStatType.CritDamage:
          return ("Critical Damage", $"+ {val:0.#}%");
        case SubStatType.Lifesteal:
          return ("Lifesteal", $"+ {val:0.#}%");
        case SubStatType.SpiritCostReduction:
          return ("Spirit Cost", $"- {val:0.#}%");
        case SubStatType.StaminaCostReduction:
          return ("Stamina Cost", $"- {val:0.#}%");
        case SubStatType.SpiritGainBonus:
          return ("Spirit Gain", $"+ {val:0.#}%");
        case SubStatType.MoveSpeedBonus:
          return ("Movement Speed", $"+ {val:0.#}%");
        default:
          return (type.ToString(), $"+ {val:0.#}");
      }
    }

    public static (string title, string description) GetEnchantmentEnglishInfo(EnchantmentType type, string defaultTitle, string defaultDesc)
    {
      switch (type)
      {
        case EnchantmentType.BleedingCrit:
          return ("Bleeding Crit", "Critical hits inflict Bleed, dealing 30% bonus damage over 3s.");
        case EnchantmentType.SkillECostReduction:
          return ("Gale Blade", "Reduces Spirit cost of Blade Slash (Skill E) by 30%.");
        case EnchantmentType.SkillQCostReduction:
          return ("Thunder Cyclone", "Reduces Spirit cost of Cyclone (Skill Q) by 30%.");
        case EnchantmentType.SkillRCostReduction:
          return ("Wrath Slash", "Reduces Spirit cost of Ultimate Slash (Skill R) by 25%.");
        case EnchantmentType.LowHealthBloodthirst:
          return ("Blood Frenzy", "Increases Lifesteal by +8% when HP drops below 50%.");
        case EnchantmentType.TitanCharge:
          return ("Titan Force", "Charge attacks deal +25% bonus damage.");
        case EnchantmentType.ParryStaminaRestore:
          return ("Steadfast Guard", "Successful Parries instantly restore 25 Stamina.");
        case EnchantmentType.SprintWind:
          return ("Windstride", "Dashing or rolling grants +20% Movement Speed for 2.5s.");
        case EnchantmentType.CritFrenzy:
          return ("Berserk Surge", "Critical hits grant +15% Movement Speed for 3.5s.");
        case EnchantmentType.SoulSiphon:
          return ("Soul Feast", "Killing an enemy restores 15 Spirit and 15 HP.");
        default:
          return (!string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Enchantment",
                  !string.IsNullOrEmpty(defaultDesc) ? defaultDesc : "Special combat effect.");
      }
    }

    #endregion

    private void ConfigureActionButtons()
    {
      if (isPreviewMode)
      {
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        if (btnUse != null) btnUse.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false);
        if (btnSell != null) btnSell.gameObject.SetActive(false);
        if (btnPutIn != null) btnPutIn.gameObject.SetActive(false);
        return;
      }

      if (isShopSellMode)
      {
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        if (btnUse != null) btnUse.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false);
        if (btnPutIn != null) btnPutIn.gameObject.SetActive(false);

        if (btnSell != null)
        {
          btnSell.gameObject.SetActive(true);
          bool canSell = currentItem != null && currentItem.Id != "item_key_dungeon";
          btnSell.interactable = canSell;

          var labelText = btnSell.GetComponentInChildren<TextMeshProUGUI>();
          if (labelText != null)
          {
            labelText.text = "Sell";
          }
        }
        return;
      }

      if (isBlacksmithMode)
      {
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        if (btnUse != null) btnUse.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false);
        if (btnSell != null) btnSell.gameObject.SetActive(false);

        if (btnPutIn != null)
        {
          btnPutIn.gameObject.SetActive(true);
          bool canUpgrade = currentItem != null && (currentItem.ItemType == ItemType.Weapon || currentItem.ItemType == ItemType.Armor);
          btnPutIn.interactable = canUpgrade;

          var labelText = btnPutIn.GetComponentInChildren<TextMeshProUGUI>();
          if (labelText != null)
          {
            labelText.text = canUpgrade ? "Put in" : "Cannot Upgrade";
          }
        }
        return;
      }

      if (btnSell != null) btnSell.gameObject.SetActive(false);
      if (btnPutIn != null) btnPutIn.gameObject.SetActive(false);

      if (isEquippedTarget)
      {
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(true);
        if (btnUse != null) btnUse.gameObject.SetActive(currentItem != null && currentItem.ItemType == ItemType.Consumable);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false);
      }
      else
      {
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(true);

        switch (currentItem.ItemType)
        {
          case ItemType.Weapon:
          case ItemType.Armor:
            if (btnEquip != null) btnEquip.gameObject.SetActive(true);
            if (btnUse != null) btnUse.gameObject.SetActive(false);
            break;

          case ItemType.Consumable:
            if (btnEquip != null) btnEquip.gameObject.SetActive(true);
            if (btnUse != null) btnUse.gameObject.SetActive(true);
            break;

          case ItemType.Material:
          case ItemType.Currency:
          default:
            if (btnEquip != null) btnEquip.gameObject.SetActive(false);
            if (btnUse != null) btnUse.gameObject.SetActive(false);
            break;
        }
      }
    }

    private string BuildStatsString(ItemData item, int upgradeLevel = 0, EquipmentAffixData affix = null)
    {
      var sb = new StringBuilder();

      float baseAtk = (affix != null && affix.rolledAttack > 0f) ? affix.rolledAttack : item.BonusAttack;
      if (baseAtk > 0f)
      {
        float atk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, upgradeLevel);
        string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
        sb.AppendLine($"<color=#FFD54F>+ {atk:0.#} Attack Damage</color>{upgStr}");
      }

      if (item.ItemType == ItemType.Armor)
      {
        bool isDefense = (affix != null) ? (affix.rolledDefense > 0f) : (item.BonusDefense > 0f);
        if (isDefense)
        {
          float baseDef = (affix != null && affix.rolledDefense > 0f) ? affix.rolledDefense : item.BonusDefense;
          float def = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, upgradeLevel);
          string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
          sb.AppendLine($"<color=#81D4FA>+ {def:0.#} Defense</color>{upgStr}");
        }
        else
        {
          float baseHp = (affix != null && affix.rolledHealth > 0f) ? affix.rolledHealth : item.BonusHealth;
          float hp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, upgradeLevel);
          string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
          sb.AppendLine($"<color=#EF5350>+ {hp:0.#} Max Health</color>{upgStr}");
        }
      }
      else
      {
        float baseDef = item.BonusDefense;
        if (baseDef > 0f)
        {
          float def = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, upgradeLevel);
          string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
          sb.AppendLine($"<color=#81D4FA>+ {def:0.#} Defense</color>{upgStr}");
        }

        float baseHp = item.BonusHealth;
        if (baseHp > 0f)
        {
          float hp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, upgradeLevel);
          string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
          sb.AppendLine($"<color=#EF5350>+ {hp:0.#} Max Health</color>{upgStr}");
        }
      }

      if (item.BonusStamina > 0f)
      {
        sb.AppendLine($"<color=#66BB6A>+ {item.BonusStamina:0.#} Max Stamina</color>");
      }

      if (affix != null && affix.subStats.Count > 0)
      {
        sb.AppendLine("\n<color=#B0BEC5>─── SUB-STATS ───</color>");
        for (int i = 0; i < affix.subStats.Count; i++)
        {
          var sub = affix.subStats[i];
          float upVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, upgradeLevel);
          var (sName, sVal) = FormatSubStat(sub.type, upVal);
          sb.AppendLine($"<color=#FFB74D>{sName}:</color> {sVal}");
        }
      }
      else
      {
        if (item.BonusCritRate > 0f)
        {
          float cr = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritRate, upgradeLevel);
          sb.AppendLine($"<color=#FFB74D>+ {cr:0.#}% Critical Rate</color>");
        }

        if (item.BonusCritDamage > 0f)
        {
          float cd = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusCritDamage, upgradeLevel);
          sb.AppendLine($"<color=#FFA726>+ {cd:0.#}% Critical Damage</color>");
        }

        if (item.BonusLifesteal > 0f)
        {
          float ls = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(item.BonusLifesteal, upgradeLevel);
          sb.AppendLine($"<color=#E57373>+ {ls:0.#}% Lifesteal</color>");
        }

        if (item.SpiritCostReduction > 0f)
        {
          sb.AppendLine($"<color=#4DD0E1>- {item.SpiritCostReduction:0.#}% Spirit Cost</color>");
        }

        if (item.StaminaCostReduction > 0f)
        {
          sb.AppendLine($"<color=#81C784>- {item.StaminaCostReduction:0.#}% Stamina Cost</color>");
        }

        if (item.SpiritGainBonus > 0f)
        {
          sb.AppendLine($"<color=#BA68C8>+ {item.SpiritGainBonus:0.#}% Spirit Gain</color>");
        }

        if (item.MoveSpeedBonus > 0f)
        {
          sb.AppendLine($"<color=#FFF176>+ {item.MoveSpeedBonus:0.#}% Movement Speed</color>");
        }
      }

      if (affix != null && affix.enchantments.Count > 0)
      {
        sb.AppendLine("\n<color=#CE93D8>─── ENCHANTMENTS ───</color>");
        for (int i = 0; i < affix.enchantments.Count; i++)
        {
          var ench = affix.enchantments[i];
          bool isUnlocked = upgradeLevel >= ench.milestoneLevel;
          var (eTitle, eDesc) = GetEnchantmentEnglishInfo(ench.enchantType, ench.title, ench.description);
          if (isUnlocked)
          {
            sb.AppendLine($"<color=#E040FB>[+{ench.milestoneLevel}] {eTitle}:</color> <color=#EEEEEE>{eDesc}</color>");
          }
          else
          {
            sb.AppendLine($"<color=#757575>[+{ench.milestoneLevel}] {eTitle} (Requires +{ench.milestoneLevel})</color>");
          }
        }
      }

      if (item.HealAmount > 0f)
      {
        sb.AppendLine($"<color=#81C784>Restores {item.HealAmount} HP Instantly</color>");
      }

      if (item.GoldValue > 0)
      {
        sb.AppendLine($"<color=#FFE082>Value: {item.GoldValue} Gold</color>");
      }

      return sb.Length > 0 ? sb.ToString().TrimEnd() : "No special attributes.";
    }

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

    private void HandleEquip()
    {
      if (currentItem == null || currentBagSlot == null || !InventoryManager.HasInstance) return;

      bool success = false;
      if (currentBagSlot.ActualBagIndex >= 0)
      {
        success = InventoryManager.Instance.EquipFromSlot(currentBagSlot.ActualBagIndex);
      }
      else
      {
        success = InventoryManager.Instance.Equip(currentItem);
      }

      if (success)
      {
        Hide();
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleUnequip()
    {
      if (currentItem == null || currentEquipSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.Unequip(currentEquipSlot.SlotType);
      if (success)
      {
        Hide();
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleUse()
    {
      if (currentItem == null || !InventoryManager.HasInstance) return;

      if (isEquippedTarget)
      {
        bool success = InventoryManager.Instance.UseEquippedUsable();
        if (success)
        {
          if (InventoryManager.Instance.EquippedUsable == null)
          {
            Hide();
          }
          else
          {
            UpdateUIContent();
          }
          OnActionCompleted?.Invoke();
        }
      }
      else
      {
        if (currentBagSlot == null) return;
        bool success = InventoryManager.Instance.UseItem(currentItem);
        if (success)
        {
          if (currentBagSlot == null || currentBagSlot.IsEmpty || currentBagSlot.CurrentQuantity <= 0)
          {
            Hide();
          }
          else
          {
            UpdateUIContent();
          }
          OnActionCompleted?.Invoke();
        }
      }
    }

    private void HandleDrop()
    {
      if (currentItem == null || currentBagSlot == null || !InventoryManager.HasInstance) return;

      bool success = InventoryManager.Instance.DropItem(currentBagSlot.ActualBagIndex, 1);
      if (success)
      {
        if (currentBagSlot == null || currentBagSlot.IsEmpty || currentBagSlot.CurrentQuantity <= 0)
        {
          Hide();
        }
        else
        {
          UpdateUIContent();
        }
        OnActionCompleted?.Invoke();
      }
    }

    private void HandleSell()
    {
      if (currentItem == null || currentBagSlot == null) return;
      OnSellClicked?.Invoke(currentBagSlot, currentItem);
    }

    private void HandlePutIn()
    {
      if (currentItem == null || currentBagSlot == null) return;
      OnPutInClicked?.Invoke(currentBagSlot, currentItem);
      Hide();
    }

    /// <summary>
    /// Đóng popup với hoạt ảnh mờ dần và co nhẹ
    /// </summary>
    public void Hide()
    {
      HideEnchantHintImmediate();

      if (currentBagSlot != null)
      {
        currentBagSlot.SetSelected(false);
        currentBagSlot = null;
      }

      if (currentEquipSlot != null)
      {
        currentEquipSlot.SetSelected(false);
        currentEquipSlot = null;
      }

      currentItem = null;
      isPreviewMode = false;

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      if (gameObject.activeSelf && popupCanvasGroup != null && popupCanvasGroup.alpha > 0.05f)
      {
        activeTransitionRoutine = StartCoroutine(AnimateCloseRoutine(closeDuration));
      }
      else
      {
        HideImmediate();
      }
    }

    /// <summary>
    /// Đóng popup tức thì không qua hoạt ảnh (dùng khi đóng túi đồ hoặc chuyển cảnh)
    /// </summary>
    public void HideImmediate()
    {
      HideEnchantHintImmediate();

      if (currentBagSlot != null)
      {
        currentBagSlot.SetSelected(false);
        currentBagSlot = null;
      }

      if (currentEquipSlot != null)
      {
        currentEquipSlot.SetSelected(false);
        currentEquipSlot = null;
      }

      currentItem = null;
      isPreviewMode = false;

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      if (popupCanvasGroup != null)
      {
        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.blocksRaycasts = false;
        popupCanvasGroup.interactable = false;
      }

      RectTransform popupRect = transform as RectTransform;
      if (popupRect != null)
      {
        popupRect.localScale = Vector3.one;
      }

      if (itemIcon != null) itemIcon.gameObject.SetActive(false);
      if (rarityBg != null) rarityBg.gameObject.SetActive(false);
      if (rarityBar != null) rarityBar.gameObject.SetActive(false);
      if (iconContainer != null) iconContainer.SetActive(false);

      gameObject.SetActive(false);
    }
  }

  [Serializable]
  public class SubStatRowUI
  {
    public GameObject root;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;
  }

  [Serializable]
  public class EnchantRowUI
  {
    public GameObject root;
    public Image iconActive;
    public Image iconUnactive;
    public TextMeshProUGUI text;
    [NonSerialized] public EnchantRowHoverTrigger hoverTrigger;
  }

  /// <summary>
  /// Bắt sự kiện hover chuột trên từng dòng Enchant để kích hoạt / ẩn EnchantHint
  /// </summary>
  public class EnchantRowHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    public Action<int> onHoverEnter;
    public Action<int> onHoverExit;
    public int rowIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
      onHoverEnter?.Invoke(rowIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      onHoverExit?.Invoke(rowIndex);
    }
  }
}
