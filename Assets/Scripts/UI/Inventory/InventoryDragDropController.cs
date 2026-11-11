using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ cơ chế Kéo & Thả (Drag and Drop) trong hệ thống Túi đồ (Inventory):
  /// 1. Kéo thả giữa 2 ô trong túi (BagSlot -> BagSlot): Hoán đổi (Swap) hoặc Gộp dồn (Merge Stack).
  /// 2. Kéo trang bị vào ô trang bị (BagSlot -> EquipSlot): Đeo ngay lập tức; nếu đã có đồ cũ thì hoán đổi về đúng ô túi đó.
  /// 3. Kéo trang bị về túi (EquipSlot -> BagSlot): Tháo trang bị vào đúng ô túi đích; nếu ô túi có trang bị cùng loại thì hoán đổi.
  /// 4. Ghost Icon đi theo chuột với hiệu ứng Rarity Glow + Bar và nghiêng nhẹ tạo phản hồi xúc giác ARPG chuẩn mực.
  /// </summary>
  public class InventoryDragDropController : MonoBehaviour
  {
    public static InventoryDragDropController Instance { get; private set; }

    [Header("=== GIAO DIỆN GHOST KÉO THẢ ===")]
    [Tooltip("Kích thước của icon bóng ma đang kéo (pixel)")]
    [SerializeField] private Vector2 ghostSize = new Vector2(75f, 75f);

    [Tooltip("Độ mờ của icon bóng ma")]
    [Range(0.2f, 1f)]
    [SerializeField] private float ghostAlpha = 0.88f;

    [Tooltip("Góc nghiêng nhẹ khi đang cầm kéo (độ)")]
    [SerializeField] private float ghostTiltAngle = -4f;

    [Tooltip("Tỷ lệ phóng to nhẹ khi nhấc item lên")]
    [SerializeField] private float ghostScale = 1.08f;

    // Trạng thái Drag
    public bool IsDragging { get; private set; } = false;
    public InventorySlotUI SourceBagSlot { get; private set; }
    public EquipmentSlotUI SourceEquipSlot { get; private set; }
    public BlacksmithUpgradeSlotUI SourceUpgradeSlot { get; private set; }
    public ItemData DraggedItem { get; private set; }
    public int DraggedQuantity { get; private set; }

    // Thành phần Ghost Object
    private Canvas rootCanvas;
    private GameObject ghostRoot;
    private RectTransform ghostRect;
    private CanvasGroup ghostCanvasGroup;
    private Image ghostBg;
    private Image ghostBar;
    private Image ghostBorder;
    private Image ghostIcon;
    private TextMeshProUGUI ghostCountText;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(this);
        return;
      }
      Instance = this;

      FindRootCanvas();
      CreateGhostObject();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }

      if (ghostRoot != null)
      {
        Destroy(ghostRoot);
      }
    }

    private void FindRootCanvas()
    {
      if (rootCanvas == null)
      {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
          rootCanvas = rootCanvas.rootCanvas;
        }
        else
        {
          rootCanvas = Object.FindFirstObjectByType<Canvas>();
        }
      }
    }

    /// <summary>
    /// Tự động khởi tạo Ghost UI Object nằm ở tầng cao nhất của Canvas
    /// </summary>
    private void CreateGhostObject()
    {
      if (ghostRoot != null || rootCanvas == null) return;

      ghostRoot = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(CanvasGroup));
      ghostRoot.transform.SetParent(rootCanvas.transform, false);
      ghostRoot.transform.SetAsLastSibling(); // Luôn đè lên trên tất cả UI panels

      ghostRect = ghostRoot.GetComponent<RectTransform>();
      ghostRect.sizeDelta = ghostSize;
      ghostRect.pivot = new Vector2(0.5f, 0.5f);
      ghostRect.localRotation = Quaternion.Euler(0f, 0f, ghostTiltAngle);
      ghostRect.localScale = Vector3.one * ghostScale;

      ghostCanvasGroup = ghostRoot.GetComponent<CanvasGroup>();
      ghostCanvasGroup.alpha = ghostAlpha;
      ghostCanvasGroup.blocksRaycasts = false; // Bắt buộc false để Raycast xuyên qua trúng Slot đích
      ghostCanvasGroup.interactable = false;

      // 1. RarityBG (glow mờ)
      GameObject bgObj = new GameObject("Ghost_RarityBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      bgObj.transform.SetParent(ghostRoot.transform, false);
      RectTransform bgRt = bgObj.GetComponent<RectTransform>();
      bgRt.anchorMin = Vector2.zero;
      bgRt.anchorMax = Vector2.one;
      bgRt.sizeDelta = Vector2.zero;
      ghostBg = bgObj.GetComponent<Image>();
      ghostBg.raycastTarget = false;

      // 2. RarityBar (thanh bar dưới icon)
      GameObject barObj = new GameObject("Ghost_RarityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      barObj.transform.SetParent(ghostRoot.transform, false);
      RectTransform barRt = barObj.GetComponent<RectTransform>();
      barRt.anchorMin = Vector2.zero;
      barRt.anchorMax = Vector2.one;
      barRt.sizeDelta = Vector2.zero;
      ghostBar = barObj.GetComponent<Image>();
      ghostBar.raycastTarget = false;

      // 3. Border khung ô
      GameObject borderObj = new GameObject("Ghost_Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      borderObj.transform.SetParent(ghostRoot.transform, false);
      RectTransform borderRt = borderObj.GetComponent<RectTransform>();
      borderRt.anchorMin = Vector2.zero;
      borderRt.anchorMax = Vector2.one;
      borderRt.sizeDelta = Vector2.zero;
      ghostBorder = borderObj.GetComponent<Image>();
      ghostBorder.raycastTarget = false;

      // 4. Icon vật phẩm
      GameObject iconObj = new GameObject("Ghost_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      iconObj.transform.SetParent(ghostRoot.transform, false);
      RectTransform iconRt = iconObj.GetComponent<RectTransform>();
      iconRt.anchorMin = new Vector2(0.12f, 0.12f);
      iconRt.anchorMax = new Vector2(0.88f, 0.88f);
      iconRt.sizeDelta = Vector2.zero;
      ghostIcon = iconObj.GetComponent<Image>();
      ghostIcon.raycastTarget = false;
      ghostIcon.preserveAspect = true;

      // 5. Stack Count Text
      GameObject countObj = new GameObject("Ghost_CountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
      countObj.transform.SetParent(ghostRoot.transform, false);
      RectTransform countRt = countObj.GetComponent<RectTransform>();
      countRt.anchorMin = new Vector2(0f, 0f);
      countRt.anchorMax = new Vector2(1f, 0.4f);
      countRt.pivot = new Vector2(1f, 0f);
      countRt.anchoredPosition = new Vector2(-4f, 4f);
      countRt.sizeDelta = Vector2.zero;
      ghostCountText = countObj.GetComponent<TextMeshProUGUI>();
      ghostCountText.raycastTarget = false;
      ghostCountText.alignment = TextAlignmentOptions.BottomRight;
      ghostCountText.fontSize = 14f;
      ghostCountText.fontStyle = FontStyles.Bold;
      ghostCountText.color = Color.white;

      ghostRoot.SetActive(false);
    }

    /// <summary>
    /// Bắt đầu kéo từ một ô trong Túi đồ (InventorySlotUI)
    /// </summary>
    public void StartDrag(InventorySlotUI bagSlot, PointerEventData eventData)
    {
      if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
      if (bagSlot == null || bagSlot.IsEmpty) return;

      // Đảm bảo Ghost luôn nằm trên đúng Canvas của ô đang được kéo (ShopCanvas hoặc Main Canvas)
      Canvas slotCanvas = bagSlot.GetComponentInParent<Canvas>();
      if (slotCanvas != null)
      {
        rootCanvas = slotCanvas.rootCanvas;
        if (ghostRoot != null && ghostRoot.transform.parent != rootCanvas.transform)
        {
          ghostRoot.transform.SetParent(rootCanvas.transform, false);
        }
      }

      IsDragging = true;
      SourceBagSlot = bagSlot;
      SourceEquipSlot = null;
      DraggedItem = bagSlot.CurrentItem;
      DraggedQuantity = bagSlot.CurrentQuantity;

      // Đóng popup chi tiết nếu đang mở để tránh che khuất tầm nhìn kéo thả
      if (InventoryUIController.Instance != null)
      {
        InventoryUIController.Instance.CloseDetailPopup();
      }
      if (ShopUIController.Instance != null && ShopUIController.IsOpen)
      {
        ShopUIController.Instance.CloseDetailPopup();
      }

      // Làm mờ icon ô gốc
      bagSlot.SetGhostFade(true);

      // Thiết lập visual cho Ghost
      SetupGhostVisual(bagSlot.RarityBgSprite, bagSlot.RarityBarSprite, bagSlot.BorderSprite);
      UpdateGhostPosition(eventData);

      if (ghostRoot != null)
      {
        ghostRoot.transform.SetAsLastSibling();
        ghostRoot.SetActive(true);
      }
    }

    /// <summary>
    /// Bắt đầu kéo từ một ô Trang bị (EquipmentSlotUI)
    /// </summary>
    public void StartDrag(EquipmentSlotUI equipSlot, PointerEventData eventData)
    {
      if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
      if (equipSlot == null || equipSlot.IsEmpty) return;

      Canvas slotCanvas = equipSlot.GetComponentInParent<Canvas>();
      if (slotCanvas != null)
      {
        rootCanvas = slotCanvas.rootCanvas;
        if (ghostRoot != null && ghostRoot.transform.parent != rootCanvas.transform)
        {
          ghostRoot.transform.SetParent(rootCanvas.transform, false);
        }
      }

      IsDragging = true;
      SourceBagSlot = null;
      SourceEquipSlot = equipSlot;
      DraggedItem = equipSlot.EquippedItem;
      DraggedQuantity = 1;

      if (InventoryUIController.Instance != null)
      {
        InventoryUIController.Instance.CloseDetailPopup();
      }
      if (ShopUIController.Instance != null && ShopUIController.IsOpen)
      {
        ShopUIController.Instance.CloseDetailPopup();
      }

      equipSlot.SetGhostFade(true);

      SetupGhostVisual(equipSlot.RarityBgSprite, equipSlot.RarityBarSprite, equipSlot.BorderSprite);
      UpdateGhostPosition(eventData);

      if (ghostRoot != null)
      {
        ghostRoot.transform.SetAsLastSibling();
        ghostRoot.SetActive(true);
      }
    }

    /// <summary>
    /// Bắt đầu kéo từ ô Đe Rèn (BlacksmithUpgradeSlotUI)
    /// </summary>
    public void StartDrag(BlacksmithUpgradeSlotUI upgradeSlot, PointerEventData eventData)
    {
      if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
      if (upgradeSlot == null || upgradeSlot.IsEmpty) return;

      Canvas slotCanvas = upgradeSlot.GetComponentInParent<Canvas>();
      if (slotCanvas != null)
      {
        rootCanvas = slotCanvas.rootCanvas;
        if (ghostRoot != null && ghostRoot.transform.parent != rootCanvas.transform)
        {
          ghostRoot.transform.SetParent(rootCanvas.transform, false);
        }
      }

      IsDragging = true;
      SourceBagSlot = null;
      SourceEquipSlot = null;
      SourceUpgradeSlot = upgradeSlot;
      DraggedItem = upgradeSlot.Item;
      DraggedQuantity = 1;

      if (InventoryUIController.Instance != null)
      {
        InventoryUIController.Instance.CloseDetailPopup();
      }
      if (ShopUIController.Instance != null && ShopUIController.IsOpen)
      {
        ShopUIController.Instance.CloseDetailPopup();
      }

      upgradeSlot.SetGhostFade(true);

      SetupGhostVisual(upgradeSlot.RarityBgSprite, upgradeSlot.RarityBarSprite, null);
      UpdateGhostPosition(eventData);

      if (ghostRoot != null)
      {
        ghostRoot.transform.SetAsLastSibling();
        ghostRoot.SetActive(true);
      }
    }

    private void SetupGhostVisual(Sprite bgSprite, Sprite barSprite, Sprite borderSprite)
    {
      if (ghostRoot == null)
      {
        FindRootCanvas();
        CreateGhostObject();
      }
      if (ghostRoot == null || DraggedItem == null) return;

      Color rarityColor = DraggedItem.Rarity.GetRarityColor();

      // Nền Glow
      if (ghostBg != null)
      {
        if (bgSprite != null) ghostBg.sprite = bgSprite;
        ghostBg.color = rarityColor;
        ghostBg.gameObject.SetActive(true);
      }

      // Thanh Bar
      if (ghostBar != null)
      {
        if (barSprite != null) ghostBar.sprite = barSprite;
        ghostBar.color = rarityColor;
        ghostBar.gameObject.SetActive(true);
      }

      // Viền Frame
      if (ghostBorder != null)
      {
        if (borderSprite != null) ghostBorder.sprite = borderSprite;
        ghostBorder.color = Color.white;
        ghostBorder.gameObject.SetActive(borderSprite != null);
      }

      // Icon chính
      if (ghostIcon != null)
      {
        ghostIcon.sprite = DraggedItem.Icon;
        ghostIcon.color = Color.white;
        ghostIcon.gameObject.SetActive(DraggedItem.Icon != null);
      }

      // Text số lượng
      if (ghostCountText != null)
      {
        if (DraggedQuantity > 1)
        {
          ghostCountText.text = DraggedQuantity.ToString();
          ghostCountText.gameObject.SetActive(true);
        }
        else
        {
          ghostCountText.gameObject.SetActive(false);
        }
      }
    }

    /// <summary>
    /// Di chuyển Ghost Icon theo chuột
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
      if (!IsDragging || ghostRoot == null) return;
      UpdateGhostPosition(eventData);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
      if (ghostRect == null) return;

      if (rootCanvas != null)
      {
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPoint))
        {
          ghostRect.anchoredPosition = localPoint;
          return;
        }
      }

      ghostRect.position = eventData.position;
    }

    /// <summary>
    /// Kết thúc kéo thả (hủy ghost và phục hồi độ mờ ô gốc)
    /// </summary>
    public void EndDrag(PointerEventData eventData)
    {
      if (!IsDragging) return;

      // Phục hồi độ mờ cho ô xuất phát
      if (SourceBagSlot != null)
      {
        SourceBagSlot.SetGhostFade(false);
      }
      if (SourceEquipSlot != null)
      {
        SourceEquipSlot.SetGhostFade(false);
      }
      if (SourceUpgradeSlot != null)
      {
        SourceUpgradeSlot.SetGhostFade(false);
      }

      if (ghostRoot != null)
      {
        ghostRoot.SetActive(false);
      }

      IsDragging = false;
      SourceBagSlot = null;
      SourceEquipSlot = null;
      SourceUpgradeSlot = null;
      DraggedItem = null;
      DraggedQuantity = 0;
    }

    /// <summary>
    /// Xử lý khi thả vào 1 ô trong Túi đồ (InventorySlotUI)
    /// </summary>
    public void DropOnBagSlot(InventorySlotUI targetBagSlot)
    {
      if (!IsDragging || targetBagSlot == null || !InventoryManager.HasInstance) return;

      // 1. Kéo từ BagSlot -> BagSlot
      if (SourceBagSlot != null)
      {
        if (targetBagSlot == SourceBagSlot)
        {
          // Thả vào chính ô vừa kéo đi -> Không làm gì
          return;
        }

        int fromIdx = SourceBagSlot.ActualBagIndex;
        int toIdx = targetBagSlot.ActualBagIndex;

        // Nếu ô đích có index -1 (ví dụ đang ở Tab lọc danh mục mà thả vào ô trống)
        if (toIdx == -1)
        {
          toIdx = FindFirstEmptyBagIndex();
        }

        if (fromIdx >= 0 && toIdx >= 0)
        {
          InventoryManager.Instance.SwapOrMergeSlots(fromIdx, toIdx);
        }
      }
      // 2. Kéo từ EquipmentSlot -> BagSlot (Tháo đồ hoặc Hoán đổi)
      else if (SourceEquipSlot != null)
      {
        int toIdx = targetBagSlot.ActualBagIndex;
        if (toIdx == -1)
        {
          toIdx = FindFirstEmptyBagIndex();
        }

        if (toIdx >= 0)
        {
          InventoryManager.Instance.UnequipToSlot(SourceEquipSlot.SlotType, toIdx);
        }
      }
      // 3. Kéo từ UpgradeSlot -> BagSlot (Tháo trang bị từ đe rèn thả vào ô túi đồ đích)
      else if (SourceUpgradeSlot != null)
      {
        int toIdx = targetBagSlot.ActualBagIndex;
        if (toIdx == -1)
        {
          toIdx = FindFirstEmptyBagIndex();
        }

        if (BlacksmithUIController.Instance != null)
        {
          BlacksmithUIController.Instance.TakeOutUpgradeItem(toIdx);
        }
      }
    }

    /// <summary>
    /// Xử lý khi thả vào 1 ô Trang bị (EquipmentSlotUI - Weapon hoặc Armor)
    /// </summary>
    public void DropOnEquipSlot(EquipmentSlotUI targetEquipSlot)
    {
      if (!IsDragging || targetEquipSlot == null || !InventoryManager.HasInstance) return;

      // 1. Kéo từ BagSlot -> EquipSlot (Đeo đồ / Hoán đổi đồ cũ)
      if (SourceBagSlot != null && DraggedItem != null)
      {
        // Kiểm tra đúng loại trang bị (Vũ khí vào WeaponSlot, Giáp vào ArmorSlot)
        if (DraggedItem.ItemType == targetEquipSlot.SlotType)
        {
          int bagIdx = SourceBagSlot.ActualBagIndex;
          if (bagIdx >= 0)
          {
            InventoryManager.Instance.EquipFromSlot(bagIdx);
          }
        }
        else
        {
          Debug.LogWarning($"[Inventory] Không thể trang bị {DraggedItem.ItemName} ({DraggedItem.ItemType}) vào ô {targetEquipSlot.SlotType}!");
        }
      }
      // 2. Kéo từ EquipSlot -> EquipSlot
      else if (SourceEquipSlot != null)
      {
        // Kéo vũ khí vào ô vũ khí hoặc giáp vào ô giáp -> Không làm gì
      }
    }

    private int FindFirstEmptyBagIndex()
    {
      if (!InventoryManager.HasInstance) return -1;
      var slots = InventoryManager.Instance.Slots;
      for (int i = 0; i < slots.Count; i++)
      {
        if (slots[i].IsEmpty) return i;
      }
      return -1;
    }
  }
}
