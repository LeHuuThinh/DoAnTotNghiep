using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý hiển thị danh sách tương tác dọc hợp nhất (Unified Vertical Interaction List UI).
  /// - Hiển thị cả IInteractable (Cổng, Rương, Cửa, NPC) và WorldItemPickup (Đồ rơi).
  /// - Tự động xếp chồng dọc (Vertical Layout), hỗ trợ tối đa maxVisibleSlots mục.
  /// - Bật 2 Corner cho mục đang chọn (Selected), tắt 2 Corner cho các mục còn lại.
  /// - Tự động cuộn trượt danh sách và hiển thị gợi ý lăn chuột khi có từ 2 mục trở lên.
  /// - Sử dụng Object Pooling triệt tiêu 100% Garbage Collection.
  /// - Hỗ trợ cơ chế tự phục hồi (Self-Healing / EnsureInstance) khi chạy game.
  /// </summary>
  public class InteractionListUI : MonoBehaviour
  {
    public static InteractionListUI Instance { get; private set; }

    [Header("=== GIAO DIỆN MẪU (TEMPLATES) ===")]
    [Tooltip("Template UI cho đối tượng tương tác chung (Cổng, Rương, NPC...)")]
    [SerializeField] private InteractPromptUI interactTemplate;

    [Tooltip("Template UI cho vật phẩm nhặt đồ rơi dưới đất (Vũ khí, Giáp, Dược phẩm...)")]
    [SerializeField] private PickupPromptUI pickupTemplate;

    [Tooltip("Khung chứa danh sách các dòng (để trống nếu script gắn trực tiếp trên khung này)")]
    [SerializeField] private RectTransform containerRect;

    [Header("=== CẤU HÌNH HIỂN THỊ ===")]
    [Tooltip("Số lượng mục tối đa hiển thị cùng một lúc trên màn hình (Sliding Window)")]
    [SerializeField] private int maxVisibleSlots = 5;

    [Tooltip("Gợi ý cuộn chuột hiển thị khi có từ 2 mục trở lên")]
    [SerializeField] private GameObject scrollHintRoot;

    [Tooltip("CanvasGroup điều khiển độ mờ của toàn bộ danh sách")]
    [SerializeField] private CanvasGroup listCanvasGroup;

    // Public setters cho Editor tool hoặc Bootstrap
    public RectTransform ContainerRect { get => containerRect; set => containerRect = value; }
    public InteractPromptUI InteractTemplate { get => interactTemplate; set => interactTemplate = value; }
    public PickupPromptUI PickupTemplate { get => pickupTemplate; set => pickupTemplate = value; }
    public GameObject ScrollHintRoot { get => scrollHintRoot; set => scrollHintRoot = value; }
    public CanvasGroup ListCanvasGroup { get => listCanvasGroup; set => listCanvasGroup = value; }

    // Object Pools
    private readonly List<InteractPromptUI> interactPool = new List<InteractPromptUI>();
    private readonly List<PickupPromptUI> pickupPool = new List<PickupPromptUI>();

    // Mapping danh sách slot đang hiển thị theo target index thực tế
    private readonly Dictionary<int, Component> activeSlotsByTargetIndex = new Dictionary<int, Component>();
    private readonly List<Component> currentlyActiveSlots = new List<Component>();

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      if (containerRect == null) containerRect = transform as RectTransform;
      if (listCanvasGroup == null) listCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

      AutoFindTemplates();
      SetupLayoutGroup();

      // Ẩn 2 template gốc ban đầu để chỉ dùng làm mẫu nhân bản (Prototype)
      if (interactTemplate != null) interactTemplate.gameObject.SetActive(false);
      if (pickupTemplate != null) pickupTemplate.gameObject.SetActive(false);

      if (scrollHintRoot != null) scrollHintRoot.SetActive(false);

      if (listCanvasGroup != null)
      {
        listCanvasGroup.alpha = 0f;
        listCanvasGroup.interactable = false;
        listCanvasGroup.blocksRaycasts = false;
      }
    }

    private void AutoFindTemplates()
    {
      if (interactTemplate == null)
      {
        interactTemplate = transform.Find("InteractUI")?.GetComponent<InteractPromptUI>()
                        ?? transform.Find("InteractTemplate")?.GetComponent<InteractPromptUI>()
                        ?? GetComponentInChildren<InteractPromptUI>(true);
      }

      if (pickupTemplate == null)
      {
        pickupTemplate = transform.Find("PickupUI")?.GetComponent<PickupPromptUI>()
                      ?? transform.Find("PickupTemplate")?.GetComponent<PickupPromptUI>()
                      ?? GetComponentInChildren<PickupPromptUI>(true);
      }

      if (scrollHintRoot == null)
      {
        scrollHintRoot = transform.Find("ScrollHint")?.gameObject;
      }
    }

    private void SetupLayoutGroup()
    {
      if (containerRect == null) return;

      var vlg = containerRect.GetComponent<VerticalLayoutGroup>();
      if (vlg == null)
      {
        vlg = containerRect.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
      }
      // Nếu vlg đã tồn tại trong Inspector, tuyệt đối không ghi đè spacing của người dùng!

      var csf = containerRect.GetComponent<ContentSizeFitter>();
      if (csf == null)
      {
        csf = containerRect.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      }
    }

    private void OnEnable()
    {
      PlayerInteraction.OnTargetsUpdated += HandleTargetsUpdated;
      PlayerInteraction.OnTargetHoldProgressChanged += HandleHoldProgressChanged;
    }

    private void OnDisable()
    {
      PlayerInteraction.OnTargetsUpdated -= HandleTargetsUpdated;
      PlayerInteraction.OnTargetHoldProgressChanged -= HandleHoldProgressChanged;
      ClearActiveSlots();
    }

    private void HandleTargetsUpdated(IReadOnlyList<InteractionTarget> targets, int selectedIndex)
    {
      if (targets == null || targets.Count == 0)
      {
        ClearActiveSlots();
        if (scrollHintRoot != null) scrollHintRoot.SetActive(false);
        if (listCanvasGroup != null) listCanvasGroup.alpha = 0f;
        return;
      }

      if (listCanvasGroup != null)
      {
        listCanvasGroup.alpha = 1f;
      }

      // Xác định cửa sổ trượt hiển thị (Sliding Window)
      int totalCount = targets.Count;
      int visibleCount = Mathf.Min(totalCount, maxVisibleSlots);
      int startIndex = 0;

      if (totalCount > maxVisibleSlots)
      {
        startIndex = Mathf.Clamp(selectedIndex - maxVisibleSlots / 2, 0, totalCount - maxVisibleSlots);
      }

      int endIndex = startIndex + visibleCount;

      // Thu hồi toàn bộ slot cũ
      DeactivateAllSlots();
      activeSlotsByTargetIndex.Clear();

      Transform parentTr = containerRect != null ? containerRect : transform;

      // Sinh hoặc lấy slot từ pool cho các mục nằm trong cửa sổ hiển thị
      for (int i = startIndex; i < endIndex; i++)
      {
        var target = targets[i];
        bool isSelected = (i == selectedIndex);

        if (target.Type == InteractionTargetType.Interactable)
        {
          var slot = GetInteractSlot(parentTr);
          if (slot != null)
          {
            slot.gameObject.SetActive(true);
            slot.transform.SetAsLastSibling();
            slot.Bind(target.Interactable, isSelected);

            activeSlotsByTargetIndex[i] = slot;
            currentlyActiveSlots.Add(slot);
          }
        }
        else if (target.Type == InteractionTargetType.Pickup)
        {
          var slot = GetPickupSlot(parentTr);
          if (slot != null)
          {
            slot.gameObject.SetActive(true);
            slot.transform.SetAsLastSibling();
            slot.Bind(target.Pickup, isSelected);

            activeSlotsByTargetIndex[i] = slot;
            currentlyActiveSlots.Add(slot);
          }
        }
      }

      // Cập nhật gợi ý cuộn chuột ở cuối danh sách khi có từ 2 mục trở lên
      if (scrollHintRoot != null)
      {
        bool showHint = totalCount >= 2;
        scrollHintRoot.SetActive(showHint);
        if (showHint)
        {
          scrollHintRoot.transform.SetAsLastSibling();
        }
      }
    }

    private void HandleHoldProgressChanged(int targetIndex, float progress)
    {
      if (activeSlotsByTargetIndex.TryGetValue(targetIndex, out Component slotComp))
      {
        if (slotComp is InteractPromptUI interactSlot)
        {
          interactSlot.SetHoldProgress(progress);
        }
      }
    }

    private InteractPromptUI GetInteractSlot(Transform parent)
    {
      for (int i = 0; i < interactPool.Count; i++)
      {
        if (interactPool[i] != null && !interactPool[i].gameObject.activeSelf)
        {
          return interactPool[i];
        }
      }

      if (interactTemplate != null)
      {
        GameObject cloneObj = Instantiate(interactTemplate.gameObject, parent);
        cloneObj.name = $"InteractSlot_{interactPool.Count}";
        var slot = cloneObj.GetComponent<InteractPromptUI>();
        interactPool.Add(slot);
        return slot;
      }

      return null;
    }

    private PickupPromptUI GetPickupSlot(Transform parent)
    {
      for (int i = 0; i < pickupPool.Count; i++)
      {
        if (pickupPool[i] != null && !pickupPool[i].gameObject.activeSelf)
        {
          return pickupPool[i];
        }
      }

      if (pickupTemplate != null)
      {
        GameObject cloneObj = Instantiate(pickupTemplate.gameObject, parent);
        cloneObj.name = $"PickupSlot_{pickupPool.Count}";
        var slot = cloneObj.GetComponent<PickupPromptUI>();
        pickupPool.Add(slot);
        return slot;
      }

      return null;
    }

    private void DeactivateAllSlots()
    {
      for (int i = 0; i < currentlyActiveSlots.Count; i++)
      {
        if (currentlyActiveSlots[i] != null)
        {
          currentlyActiveSlots[i].gameObject.SetActive(false);
        }
      }
      currentlyActiveSlots.Clear();
    }

    private void ClearActiveSlots()
    {
      DeactivateAllSlots();
      activeSlotsByTargetIndex.Clear();
    }

    /// <summary>
    /// Cơ chế Tự Phục Hồi (Self-Healing): Tự động tìm hoặc dựng InteractionList tại runtime nếu chưa được cấu hình trong Canvas
    /// </summary>
    public static InteractionListUI EnsureInstance()
    {
      if (Instance != null) return Instance;

      var existing = Object.FindFirstObjectByType<InteractionListUI>();
      if (existing != null)
      {
        Instance = existing;
        return Instance;
      }

      // Tìm Canvas chính
      var canvas = GameObject.Find("Canvas");
      if (canvas == null) return null;

      Transform interactTr = canvas.transform.Find("InteractUI");
      Transform pickupTr = canvas.transform.Find("PickupUI");
      if (interactTr == null && pickupTr == null) return null;

      GameObject listObj = new GameObject("InteractionList");
      listObj.transform.SetParent(canvas.transform, false);

      RectTransform listRect = listObj.AddComponent<RectTransform>();
      listRect.anchorMin = new Vector2(0.5f, 0.5f);
      listRect.anchorMax = new Vector2(0.5f, 0.5f);
      listRect.pivot = new Vector2(0f, 0.5f);
      listRect.anchoredPosition = new Vector2(247f, 0f);
      listRect.sizeDelta = new Vector2(300f, 64f);

      var vlg = listObj.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = 2f;
      vlg.childAlignment = TextAnchor.MiddleLeft;
      vlg.childControlWidth = false;
      vlg.childControlHeight = false;
      vlg.childForceExpandWidth = false;
      vlg.childForceExpandHeight = false;

      var csf = listObj.AddComponent<ContentSizeFitter>();
      csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
      csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      var cg = listObj.AddComponent<CanvasGroup>();
      cg.alpha = 0f;
      cg.interactable = false;
      cg.blocksRaycasts = false;

      var listUI = listObj.AddComponent<InteractionListUI>();

      if (interactTr != null)
      {
        interactTr.SetParent(listObj.transform, false);
        interactTr.gameObject.SetActive(false);
      }
      if (pickupTr != null)
      {
        pickupTr.SetParent(listObj.transform, false);
        pickupTr.gameObject.SetActive(false);
      }

      // Tạo ScrollHint
      GameObject hintObj = new GameObject("ScrollHint");
      hintObj.transform.SetParent(listObj.transform, false);
      var hintRect = hintObj.AddComponent<RectTransform>();
      hintRect.sizeDelta = new Vector2(300f, 24f);

      var hintText = hintObj.AddComponent<TextMeshProUGUI>();
      hintText.text = "[ Cuộn chuột ↕ để chọn ]";
      hintText.fontSize = 13;
      hintText.alignment = TextAlignmentOptions.Center;
      hintText.color = new Color(1f, 0.88f, 0.54f, 0.85f);
      hintObj.SetActive(false);

      listUI.containerRect = listRect;
      listUI.interactTemplate = interactTr?.GetComponent<InteractPromptUI>();
      listUI.pickupTemplate = pickupTr?.GetComponent<PickupPromptUI>();
      listUI.scrollHintRoot = hintObj;
      listUI.listCanvasGroup = cg;

      Instance = listUI;
      Debug.Log("[InteractionListUI] Đã tự động khởi tạo danh sách tương tác dọc runtime Self-Healing.");
      return Instance;
    }
  }
}
