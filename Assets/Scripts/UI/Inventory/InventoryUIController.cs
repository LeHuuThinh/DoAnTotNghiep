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

    private InventoryTab currentTab = InventoryTab.All;
    private PlayerController cachedPlayerController;

    private void Awake()
    {
      EnsureEventSystem();

      if (rootCanvasGroup == null)
      {
        rootCanvasGroup = GetComponent<CanvasGroup>();
      }

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
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnInventoryChanged -= HandleInventoryDataChanged;
        InventoryManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
      }

      IsOpen = false;
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
        itemDetailPopup.Hide();
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

      // 4. Kích hoạt camera xem trước 3D
      if (characterPreview != null)
      {
        characterPreview.EnablePreview(true);
      }

      // 5. Hiện Canvas sau khi tất cả thành phần UI đã được đồng bộ chuẩn xác
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
    }

    public void CloseInventory()
    {
      IsOpen = false;

      // 1. Ẩn Canvas
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

      // 2. Khóa lại con trỏ chuột
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      // 3. Mở khóa di chuyển nhân vật
      CachePlayerController();
      if (cachedPlayerController != null)
      {
        cachedPlayerController.UnlockMovement();
      }

      // 4. Tắt camera xem trước 3D để tiết kiệm 100% tài nguyên GPU
      if (characterPreview != null)
      {
        characterPreview.EnablePreview(false);
      }

      // 5. Đóng popup chi tiết
      if (itemDetailPopup != null)
      {
        itemDetailPopup.Hide();
      }

      // 6. Đóng modal phân bổ điểm tiềm năng nếu đang mở
      if (AttributeAllocationUI.Instance != null && AttributeAllocationUI.Instance.IsVisible)
      {
        AttributeAllocationUI.Instance.Hide();
      }
    }

    private void ForceCloseImmediately()
    {
      IsOpen = false;

      if (rootCanvasGroup != null)
      {
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
      }

      if (characterPreview != null)
      {
        characterPreview.EnablePreview(false);
      }

      if (itemDetailPopup != null)
      {
        itemDetailPopup.Hide();
      }

      if (AttributeAllocationUI.Instance != null)
      {
        AttributeAllocationUI.Instance.HideImmediate();
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
