using System;
using System.Text;
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
    [Header("=== THÀNH PHẦN HIỂN THỊ ===")]
    [Tooltip("Tên vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Text hiển thị độ hiếm (Common, Rare, Epic, Legendary)")]
    [SerializeField] private TextMeshProUGUI itemRarityText;

    [Tooltip("Text hiển thị phân loại (Vũ khí, Giáp, Tiêu hao...)")]
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

    [Tooltip("Danh sách các chỉ số cộng thêm (Sát thương, giáp, máu, hồi phục...)")]
    [SerializeField] private TextMeshProUGUI itemStatsText;

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
        // Loại bỏ triệt để HandleDrop nếu nút bị duplicate từ BtnDrop trong Unity Inspector
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
      if (itemStatsText == null) itemStatsText = transform.Find("ItemStatsText")?.GetComponent<TextMeshProUGUI>();
      if (itemDescriptionText == null) itemDescriptionText = transform.Find("ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();

      // Sửa lỗi gán nhầm reference khi người chơi nhân bản/đổi tên nút trong Editor
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

      // Đang mờ dần hoặc chưa hiển thị
      if (popupCanvasGroup != null && popupCanvasGroup.alpha < 0.05f) return;

      // Bỏ qua nếu không có phím chuột trái vừa nhấn trong frame này
      if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

      // Bỏ qua frame vừa mở popup để tránh đóng ngay lập tức
      if (Time.frameCount == lastOpenedFrame) return;

      // Bỏ qua nếu đang kéo thả vật phẩm trong kho đồ
      if (InventoryDragDropController.Instance != null && InventoryDragDropController.Instance.IsDragging) return;

      Vector2 mousePos = Mouse.current.position.ReadValue();

      // 1. Kiểm tra xem chuột có nằm trong khung của ItemDetailPopup hay không
      if (popupRectTransform == null) popupRectTransform = transform as RectTransform;

      Canvas rootCanvas = GetComponentInParent<Canvas>();
      Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

      if (popupRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(popupRectTransform, mousePos, uiCam))
      {
        // Chuột nằm trong khung chi tiết -> Cho phép thao tác các nút bên trong bình thường
        return;
      }

      // 2. Chuột nằm ngoài khung popup: Kiểm tra xem có click trúng ô đồ nào không
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

          // Nếu click trúng bất kỳ ô đồ nào (túi đồ, trang bị hoặc ô ForgeSlot) -> Để sự kiện Click của ô đó tự xử lý (chuyển item hoặc toggle)
          if (hitObj.GetComponentInParent<InventorySlotUI>() != null ||
              hitObj.GetComponentInParent<EquipmentSlotUI>() != null ||
              hitObj.name == "ForgeSlot" || hitObj.transform.parent?.name == "ForgeSlot")
          {
            return;
          }
        }
      }

      // 3. Chuột bấm ra ngoài khung popup và không bấm vào ô đồ nào -> Tự động đóng popup
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

      // Bỏ chọn ô cũ nếu có
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

      // Bỏ chọn ô cũ nếu có
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

      // Nếu đang mở đúng item preview này -> Toggle đóng popup
      if (gameObject.activeSelf && popupCanvasGroup != null && popupCanvasGroup.alpha > 0.1f && isPreviewMode && currentItem == item)
      {
        Hide();
        return;
      }

      // Bỏ chọn các ô cũ
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

      // Nếu đang mở sẵn và cùng Pivot -> Trượt mượt mà (Smooth Slide) sang ô mới
      if (isAlreadyShowing && popupRect.pivot == targetPivot)
      {
        activeTransitionRoutine = StartCoroutine(SlideToSlotRoutine(targetPos, slideDuration));
      }
      else
      {
        // Mở mới hoặc đổi hướng pivot -> Pop mở nhanh dứt khoát
        popupRect.pivot = targetPivot;
        popupRect.anchoredPosition = targetPos;
        activeTransitionRoutine = StartCoroutine(AnimateOpenRoutine(openDuration));
      }
    }

    /// <summary>
    /// Tự động tính toán vị trí bám theo ô đồ
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

      Vector3 slotWorldCenter = slotRect.position;
      Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, slotWorldCenter);

      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, cam, out targetPos))
      {
        return false;
      }

      float popupWidth = popupRect.rect.width > 0f ? popupRect.rect.width : 280f;
      float popupHeight = popupRect.rect.height > 0f ? popupRect.rect.height : 380f;

      float pivotX = 0f;
      float pivotY = 1f;

      if (screenPos.x + popupWidth > Screen.width - 20f)
      {
        pivotX = 1f;
      }

      if (screenPos.y - popupHeight < 20f)
      {
        pivotY = 0f;
      }

      targetPivot = new Vector2(pivotX, pivotY);
      return true;
    }

    private System.Collections.IEnumerator SlideToSlotRoutine(Vector2 targetPos, float duration)
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

    private System.Collections.IEnumerator AnimateOpenRoutine(float duration)
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

    private System.Collections.IEnumerator AnimateCloseRoutine(float duration)
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
      string hexColor = ColorUtility.ToHtmlStringRGB(rarityColor);
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

      // 6. Thống kê chỉ số (Stats)
      if (itemStatsText != null)
      {
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
        itemStatsText.text = BuildStatsString(currentItem, upgradeLvl, affix);
      }

      // 7. Cấu hình trạng thái 4 nút hành động
      ConfigureActionButtons();
    }

    private void ConfigureActionButtons()
    {
      if (isPreviewMode)
      {
        // Chế độ Xem Trước (Preview): Ẩn toàn bộ nút tương tác phía dưới
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
        // Chế độ Bán trong Cửa Hàng: Ẩn toàn bộ nút Equip/Unequip/Use/Drop, chỉ hiện duy nhất nút Bán
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        if (btnUse != null) btnUse.gameObject.SetActive(false);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false);
        if (btnPutIn != null) btnPutIn.gameObject.SetActive(false);

        if (btnSell != null)
        {
          btnSell.gameObject.SetActive(true);
          // Không thể bán vật phẩm nhiệm vụ (như Chìa Khóa Boss)
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
        // Chế độ Lò Rèn: Chỉ hiện duy nhất nút Bỏ vào rèn (Put in)
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
        // Đang xem đồ trên người (từ WeaponSlot, ArmorSlot hoặc UsableSlot)
        if (btnEquip != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(true); // Nút Tháo ra hiện lên
        if (btnUse != null) btnUse.gameObject.SetActive(currentItem != null && currentItem.ItemType == ItemType.Consumable);
        if (btnDrop != null) btnDrop.gameObject.SetActive(false); // Không thể vứt đồ đang mặc
      }
      else
      {
        // Đang xem đồ trong túi
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);

        // Nút Vứt bỏ luôn khả dụng cho vật phẩm trong túi
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

      // 1. DÒNG CHÍNH (MAIN STATS)
      float baseAtk = (affix != null && affix.rolledAttack > 0f) ? affix.rolledAttack : item.BonusAttack;
      if (baseAtk > 0f)
      {
        float atk = BlacksmithUpgradeConfig.CalculateUpgradedAttack(baseAtk, upgradeLevel);
        string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
        sb.AppendLine($"<color=#FFD54F>+ {atk:0.#} Attack Damage</color>{upgStr}");
      }

      float baseDef = (affix != null && affix.rolledDefense > 0f) ? affix.rolledDefense : item.BonusDefense;
      if (baseDef > 0f)
      {
        float def = BlacksmithUpgradeConfig.CalculateUpgradedDefense(baseDef, upgradeLevel);
        string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
        sb.AppendLine($"<color=#81D4FA>+ {def:0.#} Defense</color>{upgStr}");
      }

      float baseHp = (affix != null && affix.rolledHealth > 0f) ? affix.rolledHealth : item.BonusHealth;
      if (baseHp > 0f)
      {
        float hp = BlacksmithUpgradeConfig.CalculateUpgradedHealth(baseHp, upgradeLevel);
        string upgStr = upgradeLevel > 0 ? $" <size=85%><color=#77FF77>(+{upgradeLevel})</color></size>" : "";
        sb.AppendLine($"<color=#EF5350>+ {hp:0.#} Max Health</color>{upgStr}");
      }

      if (item.BonusStamina > 0f)
      {
        sb.AppendLine($"<color=#66BB6A>+ {item.BonusStamina:0.#} Max Stamina</color>");
      }

      // 2. DÒNG PHỤ THẦN BINH (SUB-STATS)
      if (affix != null && affix.subStats.Count > 0)
      {
        sb.AppendLine("\n<color=#B0BEC5>─── DÒNG PHỤ THẦN BINH ───</color>");
        for (int i = 0; i < affix.subStats.Count; i++)
        {
          var sub = affix.subStats[i];
          float upVal = BlacksmithUpgradeConfig.CalculateUpgradedSubStat(sub.value, upgradeLevel);
          sb.AppendLine(new SubStatEntry(sub.type, upVal).GetFormattedString());
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

      // 3. PHÙ PHÉP THỨC TỈNH (ENCHANTMENTS)
      if (affix != null && affix.enchantments.Count > 0)
      {
        sb.AppendLine("\n<color=#CE93D8>─── PHÙ PHÉP THỨC TỈNH ───</color>");
        for (int i = 0; i < affix.enchantments.Count; i++)
        {
          var ench = affix.enchantments[i];
          bool isUnlocked = upgradeLevel >= ench.milestoneLevel;
          if (isUnlocked)
          {
            sb.AppendLine($"<color=#E040FB>✦ [+{ench.milestoneLevel}] {ench.title}:</color> <color=#EEEEEE>{ench.description}</color>");
          }
          else
          {
            sb.AppendLine($"<color=#757575>✧ [+{ench.milestoneLevel}] {ench.title} (Khóa - Cần đạt +{ench.milestoneLevel})</color>");
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
}
