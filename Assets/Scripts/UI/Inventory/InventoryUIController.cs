using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ giao diện Túi đồ 2 cửa sổ (Dual-Pane Inventory Screen - Phím Tab)
  /// </summary>
  public class InventoryUIController : MonoBehaviour
  {
    public static InventoryUIController Instance { get; private set; }
    public static bool IsOpen { get; private set; } = false;

    public enum InventoryTab
    {
      All,
      Weapon,
      Armor,
      Item
    }

    [Header("=== GIAO DIỆN GỐC (ROOT CANVAS GROUP) ===")]
    [Tooltip("CanvasGroup của toàn bộ bảng Inventory để làm mờ / hiện")]
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Tooltip("Nút đóng [X] (Tùy chọn, có thể để trống vì dùng phím Tab / Esc)")]
    [SerializeField] private Button closeButton;

    [Header("=== CỬA SỔ TRÁI (LEFT PANE) ===")]
    [Tooltip("Controller hiển thị 3D Character Preview")]
    [SerializeField] private CharacterPreviewController characterPreview;

    [Tooltip("Toggle chuyển đổi xem trước 3D / 2D")]
    [SerializeField] private CharacterPreviewToggleUI previewModeToggle;

    [Tooltip("Ô trang bị Vũ khí")]
    [SerializeField] private EquipmentSlotUI weaponSlot;

    [Tooltip("Ô trang bị Giáp")]
    [SerializeField] private EquipmentSlotUI armorSlot;

    [Tooltip("Panel hiển thị chỉ số nhân vật thời gian thực")]
    [SerializeField] private PlayerStatsPanelUI statsPanel;

    [Header("=== CỬA SỔ PHẢI (RIGHT PANE) ===")]
    [Tooltip("Container chứa 30 ô túi đồ (có GridLayoutGroup)")]
    [SerializeField] private Transform slotsGridContainer;

    [Tooltip("Danh sách 30 ô túi đồ (Tự động tìm trong slotsGridContainer nếu để trống)")]
    [SerializeField] private List<InventorySlotUI> bagSlots = new List<InventorySlotUI>();

    [Tooltip("Text hiển thị số tiền vàng")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Tooltip("Popup hiển thị chi tiết vật phẩm và các nút thao tác")]
    [SerializeField] private ItemDetailPopupUI itemDetailPopup;

    [Header("=== CÁC NÚT TAB PHÂN LOẠI ===")]
    [SerializeField] private Button tabAllButton;
    [SerializeField] private Button tabWeaponButton;
    [SerializeField] private Button tabArmorButton;
    [SerializeField] private Button tabItemButton;

    [Header("=== TAB SELECT HIGHLIGHTS (VIỀN CHỌN DANH MỤC) ===")]
    [SerializeField] private GameObject tabAllHighlight;
    [SerializeField] private GameObject tabWeaponHighlight;
    [SerializeField] private GameObject tabArmorHighlight;
    [SerializeField] private GameObject tabItemHighlight;

    [Header("=== HOẠT ẢNH CHUYỂN CẢNH (SOULS-LIKE TRANSITIONS) ===")]
    [Tooltip("Panel bên trái chứa 3D Preview, Trang bị và Stats")]
    [SerializeField] private RectTransform leftPanel;

    [Tooltip("Panel bên phải chứa Lưới 30 ô đồ và các nút Tab")]
    [SerializeField] private RectTransform rightPanel;

    [Tooltip("Lớp phủ nền đen toàn màn hình")]
    [SerializeField] private Image darkOverlay;

    [Tooltip("Thời gian mở túi đồ (giây)")]
    [SerializeField] private float openDuration = 0.20f;

    [Tooltip("Thời gian đóng túi đồ (giây)")]
    [SerializeField] private float closeDuration = 0.12f;

    [Tooltip("Độ trượt ngang hai cánh từ mép vào (pixel)")]
    [SerializeField] private float slideOffset = 40f;

    private CanvasGroup leftCanvasGroup;
    private CanvasGroup rightCanvasGroup;
    private Vector2 leftOriginalPos;
    private Vector2 rightOriginalPos;
    private float overlayMaxAlpha = 0.7058824f;
    private bool hasCachedPositions = false;
    private Coroutine activeTransitionRoutine;

    private InventoryTab currentTab = InventoryTab.All;
    private PlayerController cachedPlayerController;

    private void Awake()
    {
      Instance = this;

      if (GetComponent<InventoryDragDropController>() == null)
      {
        gameObject.AddComponent<InventoryDragDropController>();
      }

      EnsureEventSystem();

      if (rootCanvasGroup == null)
      {
        rootCanvasGroup = GetComponent<CanvasGroup>();
      }

      if (previewModeToggle == null)
      {
        previewModeToggle = GetComponentInChildren<CharacterPreviewToggleUI>(true);
      }

      AutoFindPanels();
      CacheOriginalPositions();

      if (closeButton != null)
      {
        closeButton.onClick.AddListener(CloseInventory);
      }

      // Khởi tạo các nút tab và tìm highlight
      SetupTabButtons();

      // Tự động tìm 30 ô nếu chưa gán
      AutoFindSlots();

      // Đăng ký sự kiện từ popup
      if (itemDetailPopup != null)
      {
        itemDetailPopup.OnActionCompleted += HandleDetailActionCompleted;
      }

      // Đăng ký sự kiện click cho các ô trang bị
      if (weaponSlot != null)
      {
        weaponSlot.OnClicked += HandleEquipmentSlotClicked;
      }
      if (armorSlot != null)
      {
        armorSlot.OnClicked += HandleEquipmentSlotClicked;
      }
    }

    private void AutoFindPanels()
    {
      if (leftPanel == null) leftPanel = transform.Find("LeftPanel") as RectTransform;
      if (rightPanel == null) rightPanel = transform.Find("RightPanel") as RectTransform;
      if (darkOverlay == null) darkOverlay = transform.Find("DarkOverlay")?.GetComponent<Image>();
    }

    private void CacheOriginalPositions()
    {
      if (hasCachedPositions) return;

      AutoFindPanels();

      if (leftPanel != null)
      {
        leftOriginalPos = leftPanel.anchoredPosition;
        leftCanvasGroup = leftPanel.GetComponent<CanvasGroup>() ?? leftPanel.gameObject.AddComponent<CanvasGroup>();
      }

      if (rightPanel != null)
      {
        rightOriginalPos = rightPanel.anchoredPosition;
        rightCanvasGroup = rightPanel.GetComponent<CanvasGroup>() ?? rightPanel.gameObject.AddComponent<CanvasGroup>();
      }

      if (darkOverlay != null)
      {
        overlayMaxAlpha = darkOverlay.color.a > 0.05f ? darkOverlay.color.a : 0.7058824f;
      }

      hasCachedPositions = true;
    }

    private void Start()
    {
      CachePlayerController();

      // Đồng bộ dữ liệu sạch trước khi ẩn Canvas
      RefreshAll();

      // Lắng nghe thay đổi từ InventoryManager
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryDataChanged;
        InventoryManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
      }

      // Mặc định ẩn Inventory khi vào game
      ForceCloseImmediately();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnInventoryChanged -= HandleInventoryDataChanged;
        InventoryManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
      }

      IsOpen = false;
    }

    /// <summary>
    /// Đóng popup xem chi tiết vật phẩm nếu đang mở
    /// </summary>
    public void CloseDetailPopup()
    {
      if (itemDetailPopup != null && itemDetailPopup.gameObject.activeSelf)
      {
        itemDetailPopup.Hide();
      }
    }

    private void Update()
    {
      if (Keyboard.current == null) return;

      // 1. Phím Tab mở/đóng túi đồ
      if (Keyboard.current.tabKey.wasPressedThisFrame)
      {
        ToggleInventory();
      }
      // 2. Phím Esc đóng modal tiềm năng trước, hoặc popup chi tiết, hoặc đóng luôn Inventory
      else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        if (AttributeAllocationUI.Instance != null && AttributeAllocationUI.Instance.IsVisible)
        {
          AttributeAllocationUI.Instance.Hide();
        }
        else if (itemDetailPopup != null && itemDetailPopup.gameObject.activeSelf)
        {
          itemDetailPopup.Hide();
        }
        else
        {
          CloseInventory();
        }
      }
    }

    private void CachePlayerController()
    {
      if (cachedPlayerController != null) return;

      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        cachedPlayerController = playerObj.GetComponent<PlayerController>();
      }
      else
      {
        cachedPlayerController = FindObjectOfType<PlayerController>();
      }
    }

    private void AutoFindSlots()
    {
      if (bagSlots == null || bagSlots.Count == 0)
      {
        bagSlots = new List<InventorySlotUI>();
        if (slotsGridContainer != null)
        {
          var found = slotsGridContainer.GetComponentsInChildren<InventorySlotUI>(true);
          bagSlots.AddRange(found);
        }
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
      if (tabAllButton != null)
      {
        tabAllButton.onClick.AddListener(() => SwitchTab(InventoryTab.All));
        if (tabAllHighlight == null) tabAllHighlight = tabAllButton.transform.Find("SelectHighlight")?.gameObject;
      }

      if (tabWeaponButton != null)
      {
        tabWeaponButton.onClick.AddListener(() => SwitchTab(InventoryTab.Weapon));
        if (tabWeaponHighlight == null) tabWeaponHighlight = tabWeaponButton.transform.Find("SelectHighlight")?.gameObject;
      }

      if (tabArmorButton != null)
      {
        tabArmorButton.onClick.AddListener(() => SwitchTab(InventoryTab.Armor));
        if (tabArmorHighlight == null) tabArmorHighlight = tabArmorButton.transform.Find("SelectHighlight")?.gameObject;
      }

      if (tabItemButton != null)
      {
        tabItemButton.onClick.AddListener(() => SwitchTab(InventoryTab.Item));
        if (tabItemHighlight == null) tabItemHighlight = tabItemButton.transform.Find("SelectHighlight")?.gameObject;
      }
    }

    private void EnsureEventSystem()
    {
      if (EventSystem.current == null)
      {
        var existing = FindObjectOfType<EventSystem>();
        if (existing == null)
        {
          GameObject esObj = new GameObject("EventSystem");
          esObj.AddComponent<EventSystem>();
          esObj.AddComponent<InputSystemUIInputModule>();
          Debug.LogWarning("[Inventory] Scene thiếu EventSystem! Đã tự động tạo EventSystem + InputSystemUIInputModule.");
        }
      }

      if (EventSystem.current != null)
      {
        // Đặt ngưỡng phân biệt giữa Click và Drag (8px) giúp mở popup click rất nhạy và kéo thả mượt mà
        EventSystem.current.pixelDragThreshold = 8;
      }
    }

    public void ToggleInventory()
    {
      if (IsOpen)
      {
        CloseInventory();
      }
      else
      {
        OpenInventory();
      }
    }

    public void OpenInventory()
    {
      IsOpen = true;

      // 1. Reset về Tab Tất cả, làm mới dữ liệu và ẩn popup trước khi hiện UI để không bị nháy hình 1 frame
      currentTab = InventoryTab.All;
      UpdateTabButtonsVisual();
      RefreshAll();

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }

      // 2. Mở khóa con trỏ chuột
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      // 3. Khóa di chuyển nhân vật
      CachePlayerController();
      if (cachedPlayerController != null)
      {
        cachedPlayerController.LockMovement();
      }

      // 4. Kích hoạt camera xem trước (áp dụng theo chế độ 3D hoặc 2D)
      if (previewModeToggle != null)
      {
        previewModeToggle.ApplyCurrentModeToPreview();
      }
      else if (characterPreview != null)
      {
        characterPreview.EnablePreview(true);
      }

      // 5. Chạy hoạt ảnh mở hai cánh (Dual-Pane Entrance Transition)
      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
      }

      activeTransitionRoutine = StartCoroutine(AnimateOpenRoutine());
    }

    public void CloseInventory()
    {
      if (!IsOpen) return;
      IsOpen = false;

      // 1. Khóa tương tác UI ngay lập tức
      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
      }

      // 2. Đóng popup chi tiết tức thì
      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }

      // 3. Đóng modal phân bổ điểm tiềm năng nếu đang mở
      if (AttributeAllocationUI.Instance != null && AttributeAllocationUI.Instance.IsVisible)
      {
        AttributeAllocationUI.Instance.HideImmediate();
      }

      // 4. Chạy hoạt ảnh đóng hai cánh (Dual-Pane Exit Transition)
      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
      }

      activeTransitionRoutine = StartCoroutine(AnimateCloseRoutine());
    }

    private System.Collections.IEnumerator AnimateOpenRoutine()
    {
      CacheOriginalPositions();

      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;
      }
      else
      {
        gameObject.SetActive(true);
      }

      Vector2 leftStartPos = leftOriginalPos + new Vector2(-slideOffset, 0f);
      Vector2 rightStartPos = rightOriginalPos + new Vector2(slideOffset, 0f);
      Vector3 startScale = Vector3.one * 0.97f;

      if (leftPanel != null)
      {
        leftPanel.anchoredPosition = leftStartPos;
        leftPanel.localScale = startScale;
      }
      if (leftCanvasGroup != null) leftCanvasGroup.alpha = 0f;

      if (rightPanel != null)
      {
        rightPanel.anchoredPosition = rightStartPos;
        rightPanel.localScale = startScale;
      }
      if (rightCanvasGroup != null) rightCanvasGroup.alpha = 0f;

      if (darkOverlay != null)
      {
        Color c = darkOverlay.color;
        c.a = 0f;
        darkOverlay.color = c;
      }

      float elapsed = 0f;
      float staggerDelay = 0.02f;

      while (elapsed < openDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / openDuration);

        // 1. Lớp phủ nền mờ dần
        if (darkOverlay != null)
        {
          float eOverlay = UITransitionHelper.EaseOutQuad(t);
          Color c = darkOverlay.color;
          c.a = Mathf.Lerp(0f, overlayMaxAlpha, eOverlay);
          darkOverlay.color = c;
        }

        // 2. Cửa sổ trái (Left Panel) trượt từ mép trái vào tâm
        if (leftPanel != null)
        {
          float eLeft = UITransitionHelper.EaseOutCubic(t);
          leftPanel.anchoredPosition = Vector2.LerpUnclamped(leftStartPos, leftOriginalPos, eLeft);
          leftPanel.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eLeft);
          if (leftCanvasGroup != null) leftCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eLeft);
        }

        // 3. Cửa sổ phải (Right Panel) trượt từ mép phải vào tâm (trễ 0.02s)
        if (rightPanel != null)
        {
          float tRight = Mathf.Clamp01((elapsed - staggerDelay) / (openDuration - staggerDelay));
          float eRight = UITransitionHelper.EaseOutCubic(tRight);
          rightPanel.anchoredPosition = Vector2.LerpUnclamped(rightStartPos, rightOriginalPos, eRight);
          rightPanel.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eRight);
          if (rightCanvasGroup != null) rightCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eRight);
        }

        yield return null;
      }

      ResetPanelsToOriginal();
      activeTransitionRoutine = null;
    }

    private System.Collections.IEnumerator AnimateCloseRoutine()
    {
      CacheOriginalPositions();

      Vector2 leftTargetPos = leftOriginalPos + new Vector2(-slideOffset * 0.5f, 0f);
      Vector2 rightTargetPos = rightOriginalPos + new Vector2(slideOffset * 0.5f, 0f);
      Vector3 endScale = Vector3.one * 0.98f;

      float currentOverlayAlpha = darkOverlay != null ? darkOverlay.color.a : 0f;
      float elapsed = 0f;

      while (elapsed < closeDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / closeDuration);
        float e = UITransitionHelper.EaseInQuad(t);

        // 1. Lớp phủ nền mờ dần về 0
        if (darkOverlay != null)
        {
          Color c = darkOverlay.color;
          c.a = Mathf.Lerp(currentOverlayAlpha, 0f, e);
          darkOverlay.color = c;
        }

        // 2. Cánh trái trượt nhẹ ra ngoài
        if (leftPanel != null)
        {
          leftPanel.anchoredPosition = Vector2.LerpUnclamped(leftOriginalPos, leftTargetPos, e);
          leftPanel.localScale = Vector3.LerpUnclamped(Vector3.one, endScale, e);
          if (leftCanvasGroup != null) leftCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e);
        }

        // 3. Cánh phải trượt nhẹ ra ngoài
        if (rightPanel != null)
        {
          rightPanel.anchoredPosition = Vector2.LerpUnclamped(rightOriginalPos, rightTargetPos, e);
          rightPanel.localScale = Vector3.LerpUnclamped(Vector3.one, endScale, e);
          if (rightCanvasGroup != null) rightCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e);
        }

        yield return null;
      }

      // Ẩn Canvas gốc
      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.alpha = 0f;
      }
      else
      {
        gameObject.SetActive(false);
      }

      ResetPanelsToOriginal();

      // Khóa lại con trỏ chuột
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      // Mở khóa di chuyển nhân vật
      CachePlayerController();
      if (cachedPlayerController != null)
      {
        cachedPlayerController.UnlockMovement();
      }

      // Tắt camera xem trước 3D
      if (characterPreview != null)
      {
        characterPreview.EnablePreview(false);
      }

      activeTransitionRoutine = null;
    }

    private void ForceCloseImmediately()
    {
      IsOpen = false;

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
      }
      else
      {
        gameObject.SetActive(false);
      }

      ResetPanelsToOriginal();

      if (darkOverlay != null)
      {
        Color c = darkOverlay.color;
        c.a = 0f;
        darkOverlay.color = c;
      }

      if (characterPreview != null)
      {
        characterPreview.EnablePreview(false);
      }

      if (itemDetailPopup != null)
      {
        itemDetailPopup.HideImmediate();
      }

      if (AttributeAllocationUI.Instance != null)
      {
        AttributeAllocationUI.Instance.HideImmediate();
      }
    }

    private void ResetPanelsToOriginal()
    {
      if (hasCachedPositions)
      {
        if (leftPanel != null)
        {
          leftPanel.anchoredPosition = leftOriginalPos;
          leftPanel.localScale = Vector3.one;
        }
        if (leftCanvasGroup != null) leftCanvasGroup.alpha = 1f;

        if (rightPanel != null)
        {
          rightPanel.anchoredPosition = rightOriginalPos;
          rightPanel.localScale = Vector3.one;
        }
        if (rightCanvasGroup != null) rightCanvasGroup.alpha = 1f;

        if (darkOverlay != null && IsOpen)
        {
          Color c = darkOverlay.color;
          c.a = overlayMaxAlpha;
          darkOverlay.color = c;
        }
      }
    }

    public void SwitchTab(InventoryTab tab)
    {
      currentTab = tab;
      UpdateTabButtonsVisual();

      if (itemDetailPopup != null)
      {
        itemDetailPopup.Hide();
      }

      RefreshBagGrid();
    }

    private void UpdateTabButtonsVisual()
    {
      // Bật / Tắt viền SelectHighlight của từng Tab khi được chọn
      if (tabAllHighlight != null) tabAllHighlight.SetActive(currentTab == InventoryTab.All);
      if (tabWeaponHighlight != null) tabWeaponHighlight.SetActive(currentTab == InventoryTab.Weapon);
      if (tabArmorHighlight != null) tabArmorHighlight.SetActive(currentTab == InventoryTab.Armor);
      if (tabItemHighlight != null) tabItemHighlight.SetActive(currentTab == InventoryTab.Item);
    }

    /// <summary>
    /// Làm mới toàn bộ thông tin trên màn hình Túi đồ
    /// </summary>
    public void RefreshAll()
    {
      RefreshBagGrid();
      RefreshEquipment();
      RefreshStats();

      if (InventoryManager.HasInstance)
      {
        UpdateGoldDisplay(InventoryManager.Instance.CurrentGold);
      }
    }

    /// <summary>
    /// Làm mới 30 ô túi đồ theo Tab phân loại đang chọn
    /// </summary>
    private void RefreshBagGrid()
    {
      if (!InventoryManager.HasInstance || bagSlots == null) return;

      var invSlots = InventoryManager.Instance.Slots;

      if (currentTab == InventoryTab.All)
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
        // Thu thập các vật phẩm khớp với danh mục được chọn
        var matchingSlots = new List<(ItemData item, int quantity, int originalIndex)>();

        for (int i = 0; i < invSlots.Count; i++)
        {
          if (invSlots[i].IsEmpty) continue;

          var item = invSlots[i].ItemData;
          bool isMatch = false;

          switch (currentTab)
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

        // Đổ danh sách khớp vào các ô UI đầu tiên
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

    /// <summary>
    /// Làm mới 2 ô trang bị (Vũ khí & Giáp)
    /// </summary>
    private void RefreshEquipment()
    {
      if (!InventoryManager.HasInstance) return;

      if (weaponSlot != null)
      {
        weaponSlot.SetEquippedItem(InventoryManager.Instance.EquippedWeapon);
      }

      if (armorSlot != null)
      {
        armorSlot.SetEquippedItem(InventoryManager.Instance.EquippedArmor);
      }
    }

    /// <summary>
    /// Làm mới bảng chỉ số nhân vật
    /// </summary>
    private void RefreshStats()
    {
      if (statsPanel != null)
      {
        statsPanel.RefreshStats();
      }
    }

    /// <summary>
    /// Cập nhật hiển thị số tiền vàng
    /// </summary>
    private void UpdateGoldDisplay(int goldAmount)
    {
      if (goldText != null)
      {
        // Hiển thị số tiền vàng định dạng số có dấu phân cách nghìn
        goldText.text = goldAmount.ToString("N0");
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
        // Nếu click lại chính ô đang mở thì toggle đóng
        if (itemDetailPopup.CurrentBagSlot == slot && itemDetailPopup.gameObject.activeSelf)
        {
          itemDetailPopup.Hide();
        }
        else
        {
          itemDetailPopup.ShowForBagSlot(slot);
        }
      }
    }

    private void HandleEquipmentSlotClicked(EquipmentSlotUI eqSlot)
    {
      if (eqSlot == null || eqSlot.IsEmpty)
      {
        if (itemDetailPopup != null) itemDetailPopup.Hide();
        return;
      }

      if (itemDetailPopup != null)
      {
        // Nếu click lại chính ô đang mở thì toggle đóng
        if (itemDetailPopup.CurrentEquipSlot == eqSlot && itemDetailPopup.gameObject.activeSelf)
        {
          itemDetailPopup.Hide();
        }
        else
        {
          itemDetailPopup.ShowForEquipmentSlot(eqSlot);
        }
      }
    }

    private void HandleDetailActionCompleted()
    {
      RefreshAll();
    }

    private void HandleInventoryDataChanged()
    {
      if (IsOpen)
      {
        RefreshBagGrid();
      }
    }

    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      if (IsOpen)
      {
        RefreshEquipment();
        RefreshStats();
      }
    }
  }
}
