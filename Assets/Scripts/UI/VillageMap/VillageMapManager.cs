using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ hệ thống Bản Đồ Làng Haven (Village Map - LandScene - Task POL-04):
  /// - Kích hoạt bật/tắt bằng phím M (và Esc để đóng), đồng bộ trải nghiệm với DungeonMapManager
  /// - Hiển thị ảnh chụp trực giao toàn cảnh ngôi làng từ trên xuống (VillageMap.png)
  /// - Tôn trọng 100% vị trí các Icon địa danh (Bell, 2 Shop, Blacksmith, Portal) do người dùng căn chỉnh trong Canvas
  /// - Giới hạn kéo Pan tuyệt đối trong phạm vi ảnh bản đồ (không để lộ phần nền đen bên ngoài)
  /// - Cập nhật vị trí Người Chơi thời gian thực với độ chính xác tuyệt đối qua ma trận Affine + RBF Residuals
  /// - Giữ cố định hướng của PlayerMarker (rotatePlayerMarker = false) như hầm ngục
  /// - Tự động ẩn các nhãn Text (showLandmarkLabels = false) để giao diện thoáng đẹp, dễ nhìn
  /// </summary>
  [RequireComponent(typeof(CanvasGroup))]
  public class VillageMapManager : MonoBehaviour
  {
    public static VillageMapManager Instance { get; private set; }

    [Header("=== CÀI ĐẶT BẬT/TẮT & GIAO DIỆN ===")]
    [Tooltip("Tên Scene ngôi làng (chỉ nhận phím M khi đang ở scene này)")]
    public string villageSceneName = "LandScene";

    [Tooltip("Phím nóng bật/tắt bản đồ")]
    public KeyCode toggleKey = KeyCode.M;

    [Tooltip("Khóa di chuyển nhân vật khi mở bản đồ? (Mặc định: false - vừa chạy vừa xem map như ARPG)")]
    public bool freezePlayerWhenOpen = false;

    [Tooltip("Mở con trỏ chuột khi xem bản đồ để kéo/zoom?")]
    public bool unlockCursorWhenOpen = true;

    [Tooltip("Căn giữa vào vị trí Người Chơi khi mở bản đồ?")]
    public bool centerOnPlayerOnOpen = true;

    [Tooltip("Độ zoom mặc định khi mở bản đồ (Mặc định: 1.5)")]
    [Range(1.0f, 4.0f)] public float defaultZoomOnOpen = 1.5f;

    [Tooltip("Độ zoom nhỏ nhất (Mặc định: 1.0 để luôn vừa khít ảnh không lộ nền đen)")]
    [Range(0.5f, 2.0f)] public float minZoom = 1.0f;

    [Tooltip("Độ zoom lớn nhất (Mặc định: 3.0)")]
    [Range(1.5f, 5.0f)] public float maxZoom = 3.0f;

    [Tooltip("Tốc độ phóng to thu nhỏ bằng con lăn chuột")]
    public float zoomSpeed = 0.15f;

    [Tooltip("Độ mờ của lớp nền tối phủ toàn màn hình")]
    [Range(0f, 1f)] public float backgroundDimAlpha = 0.85f;

    [Header("=== TÀI NGUYÊN HÌNH ẢNH (UI RPG) ===")]
    [Tooltip("Sprite toàn cảnh ngôi làng nhìn từ trên xuống (VillageMap.png)")]
    public Sprite villageMapSprite;

    [Tooltip("Sprite con trỏ Người Chơi (Player_Icon.png)")]
    public Sprite playerIconSprite;

    [Header("=== KÍCH THƯỚC BẢN ĐỒ & HIỂN THỊ ===")]
    [Tooltip("Kích thước hiển thị của ảnh bản đồ ngôi làng trên UI (pixels)")]
    public Vector2 mapImageSize = new Vector2(2624f, 1632f);

    [Tooltip("Kích thước icon Người Chơi (pixels)")]
    [Range(24f, 120f)] public float playerIconSize = 80f;

    [Tooltip("Kích thước icon các địa danh (pixels)")]
    [Range(24f, 120f)] public float landmarkIconSize = 80f;

    [Tooltip("Xoay icon người chơi theo hướng nhìn nhân vật? (Mặc định: false - giữ thẳng đứng cố định như Dungeon)")]
    public bool rotatePlayerMarker = false;

    [Tooltip("Hiển thị nhãn chữ bên dưới các icon địa danh? (Mặc định: false - ẩn chữ cho thoáng đẹp theo yêu cầu)")]
    public bool showLandmarkLabels = false;

    // --- CÁC THÀNH PHẦN NỘI BỘ ---
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private RectTransform mapViewport;
    private RectTransform mapContent;
    private Image villageImage;
    private RectTransform playerMarkerTransform;
    private Image playerMarkerImage;
    private Transform landmarksContainer;

    private Transform cachedPlayerTransform;
    private PlayerController cachedPlayerController;

    private float currentZoom = 1.5f;
    private Vector2 panOffset = Vector2.zero;
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private bool isMapOpen = false;
    private bool isHierarchyBuilt = false;

    // 5 Mốc chuẩn (World 3D -> UI Position do người dùng căn chỉnh trên ảnh VillageMap.png)
    private struct LandmarkAnchor
    {
      public string name;
      public Vector2 worldPos; // (X, Z) trong 3D
      public Vector2 uiPos;    // (u, v) trên UI
      public Vector2 residual; // Sai số so với ma trận Affine nền
    }

    // Hệ số Affine nền: u = a*x + b*z + c, v = d*x + e*z + f
    private const float AFFINE_A = 4.1503f;
    private const float AFFINE_B = -6.3074f;
    private const float AFFINE_C = 454.2664f;
    private const float AFFINE_D = 11.2052f;
    private const float AFFINE_E = 1.7694f;
    private const float AFFINE_F = -2346.5724f;

    private static readonly LandmarkAnchor[] LandmarkAnchors = new LandmarkAnchor[]
    {
      new LandmarkAnchor { name = "Bell",       worldPos = new Vector2(186.89f, 127.47f), uiPos = new Vector2(427.0f, -2.0f) },
      new LandmarkAnchor { name = "Equip",      worldPos = new Vector2(183.30f, 114.10f), uiPos = new Vector2(488.0f, -107.0f) },
      new LandmarkAnchor { name = "Supplies",   worldPos = new Vector2(149.71f, 144.10f), uiPos = new Vector2(174.0f, -408.0f) },
      new LandmarkAnchor { name = "Blacksmith", worldPos = new Vector2(210.05f, 119.76f), uiPos = new Vector2(559.0f, 184.0f) },
      new LandmarkAnchor { name = "Portal",     worldPos = new Vector2(235.17f, 98.45f),  uiPos = new Vector2(820.0f, 483.0f) }
    };

    private static bool anchorsInitialized = false;

    public bool IsOpen => isMapOpen;

    private void Awake()
    {
      Instance = this;

      // Đảm bảo Map UI luôn nằm dưới các menu khác (Inventory, HUD, Dialogue)
      transform.SetAsFirstSibling();

      canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

      // Mặc định ẩn map khi khởi động
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;
      isMapOpen = false;

      InitAnchorResiduals();
      AutoLoadSpritesIfMissing();
    }

    private void Start()
    {
      FindPlayer();
      BuildMapHierarchy();
      ApplyLandmarkSettings();
      CloseMap();
    }

    private void Update()
    {
      // Chỉ hoạt động nếu đang ở LandScene (hoặc SceneName khớp)
      string currentScene = SceneManager.GetActiveScene().name;
      if (!string.IsNullOrEmpty(villageSceneName) && currentScene != villageSceneName)
      {
        if (isMapOpen) CloseMap();
        return;
      }

      // 1. Bắt phím mở/đóng Map (phím M)
      bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
      {
        togglePressed = true;
      }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
      if (Input.GetKeyDown(toggleKey))
      {
        togglePressed = true;
      }
#endif

      if (togglePressed)
      {
        ToggleMap();
      }

      // 2. Phím Esc đóng map nếu đang mở
      bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
      if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
#endif

      if (isMapOpen && escPressed)
      {
        CloseMap();
      }

      // 3. Cập nhật vị trí Người Chơi thời gian thực
      UpdatePlayerMarker();

      // 4. Xử lý Pan và Zoom khi Map đang mở
      if (isMapOpen)
      {
        HandlePanAndZoom();
      }
    }

    private static void InitAnchorResiduals()
    {
      if (anchorsInitialized) return;
      for (int i = 0; i < LandmarkAnchors.Length; i++)
      {
        float wx = LandmarkAnchors[i].worldPos.x;
        float wz = LandmarkAnchors[i].worldPos.y;
        float baseU = AFFINE_A * wx + AFFINE_B * wz + AFFINE_C;
        float baseV = AFFINE_D * wx + AFFINE_E * wz + AFFINE_F;
        LandmarkAnchors[i].residual = new Vector2(LandmarkAnchors[i].uiPos.x - baseU, LandmarkAnchors[i].uiPos.y - baseV);
      }
      anchorsInitialized = true;
    }

    private void FindPlayer()
    {
      if (cachedPlayerTransform != null) return;

      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        cachedPlayerTransform = playerObj.transform;
        cachedPlayerController = playerObj.GetComponent<PlayerController>();
      }
      else
      {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
          cachedPlayerTransform = pc.transform;
          cachedPlayerController = pc;
        }
      }
    }

    public void ToggleMap()
    {
      if (isMapOpen) CloseMap();
      else OpenMap();
    }

    public void OpenMap()
    {
      isMapOpen = true;
      AudioManager.Instance?.PlayUIInteract();

      // Đưa map xuống đáy Canvas để nằm dưới các UI khác
      transform.SetAsFirstSibling();

      if (canvasGroup != null)
      {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
      }

      // Khóa di chuyển nhân vật nếu được bật
      if (freezePlayerWhenOpen)
      {
        FindPlayer();
        if (cachedPlayerController != null) cachedPlayerController.LockMovement();
      }

      // Mở khóa chuột để kéo và zoom bản đồ
      if (unlockCursorWhenOpen)
      {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
      }

      // Cập nhật ngay vị trí Người Chơi
      UpdatePlayerMarker();

      // Căn giữa vào vị trí Người Chơi và giới hạn biên bức ảnh
      SetZoom(defaultZoomOnOpen);
      if (centerOnPlayerOnOpen)
      {
        CenterMapOnPlayer();
      }
      else
      {
        ClampPanOffset();
      }
    }

    public void CloseMap()
    {
      isMapOpen = false;

      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
      }

      // Mở lại di chuyển nhân vật
      if (freezePlayerWhenOpen)
      {
        FindPlayer();
        if (cachedPlayerController != null) cachedPlayerController.UnlockMovement();
      }

      // Khóa lại con trỏ chuột nếu túi đồ không mở
      if (unlockCursorWhenOpen)
      {
        if (!InventoryUIController.IsOpen)
        {
          Cursor.lockState = CursorLockMode.Locked;
          Cursor.visible = false;
        }
      }
    }

    /// <summary>
    /// Chuyển đổi tọa độ 3D trong thế giới Làng sang tọa độ 2D UI trên ảnh bản đồ:
    /// Kết hợp ma trận Affine toàn cục và hàm RBF nội suy sai số, triệt tiêu 100% sai lệch tại 5 địa danh!
    /// </summary>
    public Vector2 WorldToMapPosition(Vector3 worldPos)
    {
      InitAnchorResiduals();

      float wx = worldPos.x;
      float wz = worldPos.z;

      float baseU = AFFINE_A * wx + AFFINE_B * wz + AFFINE_C;
      float baseV = AFFINE_D * wx + AFFINE_E * wz + AFFINE_F;

      float totalWeight = 0f;
      float corrU = 0f;
      float corrV = 0f;

      for (int i = 0; i < LandmarkAnchors.Length; i++)
      {
        float dx = wx - LandmarkAnchors[i].worldPos.x;
        float dz = wz - LandmarkAnchors[i].worldPos.y;
        float distSq = dx * dx + dz * dz;

        // Nếu trùng sát điểm neo (< 0.1m) trả về chính xác tọa độ UI đã đặt
        if (distSq < 0.01f)
        {
          return LandmarkAnchors[i].uiPos;
        }

        float weight = 1f / (distSq + 4.0f); // Bán kính làm mượt 2m
        totalWeight += weight;
        corrU += weight * LandmarkAnchors[i].residual.x;
        corrV += weight * LandmarkAnchors[i].residual.y;
      }

      if (totalWeight > 0.0001f)
      {
        baseU += corrU / totalWeight;
        baseV += corrV / totalWeight;
      }

      return new Vector2(baseU, baseV);
    }

    /// <summary>
    /// Cập nhật vị trí Người Chơi thời gian thực trên bản đồ
    /// </summary>
    private void UpdatePlayerMarker()
    {
      FindPlayer();
      if (cachedPlayerTransform == null || playerMarkerTransform == null) return;

      Vector2 mapPos = WorldToMapPosition(cachedPlayerTransform.position);
      playerMarkerTransform.anchoredPosition = mapPos;

      // Yêu cầu 3: Icon Player giữ thẳng đứng cố định như trong Dungeon, không xoay tròn
      if (rotatePlayerMarker)
      {
        float yaw = cachedPlayerTransform.eulerAngles.y;
        playerMarkerTransform.localEulerAngles = new Vector3(0f, 0f, -yaw);
      }
      else
      {
        playerMarkerTransform.localEulerAngles = Vector3.zero;
      }

      // Đưa Player Marker lên trên cùng
      playerMarkerTransform.SetAsLastSibling();
    }

    /// <summary>
    /// Căn giữa bản đồ vào vị trí Player và giới hạn trong biên ảnh
    /// </summary>
    public void CenterMapOnPlayer()
    {
      if (mapContent == null) return;

      FindPlayer();
      Vector2 targetUIPos = Vector2.zero;
      if (cachedPlayerTransform != null)
      {
        targetUIPos = WorldToMapPosition(cachedPlayerTransform.position);
      }
      else if (playerMarkerTransform != null)
      {
        targetUIPos = playerMarkerTransform.anchoredPosition;
      }

      panOffset = -targetUIPos * currentZoom;
      ClampPanOffset();
    }

    /// <summary>
    /// Đặt độ zoom bản đồ và clamp pan
    /// </summary>
    public void SetZoom(float targetZoom)
    {
      currentZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
      if (mapContent != null)
      {
        mapContent.localScale = Vector3.one * currentZoom;
      }
      ClampPanOffset();
    }

    /// <summary>
    /// Yêu cầu 2: Giới hạn panOffset để 4 mép ảnh VillageImage KHÔNG BAO GIỜ co vào trong khung nhìn Viewport
    /// </summary>
    private void ClampPanOffset()
    {
      if (mapContent == null) return;

      Vector2 viewportSize = (mapViewport != null) ? mapViewport.rect.size : new Vector2(Screen.width, Screen.height);
      if (viewportSize.x <= 10f || viewportSize.y <= 10f)
      {
        viewportSize = new Vector2(Screen.width, Screen.height);
      }

      // Kích thước hiệu dụng của ảnh bản đồ theo độ zoom hiện tại
      float contentWidth = mapImageSize.x * currentZoom;
      float contentHeight = mapImageSize.y * currentZoom;

      // Trục X: nếu ảnh rộng hơn viewport thì giới hạn maxPan, nếu nhỏ hơn thì neo tại tâm (0)
      if (contentWidth > viewportSize.x)
      {
        float maxPanX = (contentWidth - viewportSize.x) * 0.5f;
        panOffset.x = Mathf.Clamp(panOffset.x, -maxPanX, maxPanX);
      }
      else
      {
        panOffset.x = 0f;
      }

      // Trục Y: nếu ảnh cao hơn viewport thì giới hạn maxPan, nếu nhỏ hơn thì neo tại tâm (0)
      if (contentHeight > viewportSize.y)
      {
        float maxPanY = (contentHeight - viewportSize.y) * 0.5f;
        panOffset.y = Mathf.Clamp(panOffset.y, -maxPanY, maxPanY);
      }
      else
      {
        panOffset.y = 0f;
      }

      mapContent.anchoredPosition = panOffset;
    }

    /// <summary>
    /// Điều khiển Pan (kéo chuột trái) và Zoom (lăn chuột) kèm Clamping chống lộ nền
    /// </summary>
    private void HandlePanAndZoom()
    {
      if (mapContent == null) return;

      Vector2 mousePos = Vector2.zero;
      bool mouseLeftDown = false;
      bool mouseLeftHeld = false;
      bool mouseLeftUp = false;
      float scrollDelta = 0f;

#if ENABLE_INPUT_SYSTEM
      if (Mouse.current != null)
      {
        mousePos = Mouse.current.position.ReadValue();
        mouseLeftDown = Mouse.current.leftButton.wasPressedThisFrame;
        mouseLeftHeld = Mouse.current.leftButton.isPressed;
        mouseLeftUp = Mouse.current.leftButton.wasReleasedThisFrame;
        scrollDelta = Mouse.current.scroll.ReadValue().y;
      }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
      mousePos = Input.mousePosition;
      if (Input.GetMouseButtonDown(0)) mouseLeftDown = true;
      if (Input.GetMouseButton(0)) mouseLeftHeld = true;
      if (Input.GetMouseButtonUp(0)) mouseLeftUp = true;
      scrollDelta = Input.mouseScrollDelta.y;
#endif

      // 1. Xử lý Pan (Kéo chuột trái)
      if (mouseLeftDown)
      {
        isDragging = true;
        lastMousePosition = mousePos;
      }
      else if (mouseLeftUp)
      {
        isDragging = false;
      }

      if (isDragging && mouseLeftHeld)
      {
        Vector2 delta = mousePos - lastMousePosition;
        panOffset += delta;
        lastMousePosition = mousePos;
        ClampPanOffset();
      }

      // 2. Xử lý Zoom (Con lăn chuột)
      if (Mathf.Abs(scrollDelta) > 0.01f)
      {
        float zoomChange = (scrollDelta > 0f) ? zoomSpeed : -zoomSpeed;
        float newZoom = Mathf.Clamp(currentZoom + zoomChange, minZoom, maxZoom);
        if (Mathf.Abs(newZoom - currentZoom) > 0.001f)
        {
          float zoomFactor = newZoom / currentZoom;
          currentZoom = newZoom;
          mapContent.localScale = Vector3.one * currentZoom;
          panOffset *= zoomFactor;
          ClampPanOffset();
        }
      }
    }

    /// <summary>
    /// Tự động dựng khung cấu trúc UI Canvas nếu chưa có
    /// </summary>
    public void BuildMapHierarchy()
    {
      if (isHierarchyBuilt && mapContent != null && villageImage != null) return;

      // 1. Background Dimmer (Phủ tối toàn màn hình)
      var bgObj = transform.Find("MapBackground");
      if (bgObj == null)
      {
        GameObject bg = new GameObject("MapBackground", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        backgroundImage = bg.GetComponent<Image>();
        backgroundImage.color = new Color(0.03f, 0.03f, 0.05f, backgroundDimAlpha);
        backgroundImage.raycastTarget = true;

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
      }
      else
      {
        backgroundImage = bgObj.GetComponent<Image>();
      }

      // 2. Viewport (Cửa sổ hiển thị bản đồ)
      var vpObj = transform.Find("MapViewport");
      if (vpObj == null)
      {
        GameObject vp = new GameObject("MapViewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(transform, false);
        mapViewport = vp.GetComponent<RectTransform>();
        mapViewport.anchorMin = Vector2.zero;
        mapViewport.anchorMax = Vector2.one;
        mapViewport.sizeDelta = Vector2.zero;
      }
      else
      {
        mapViewport = vpObj.GetComponent<RectTransform>();
      }

      // 3. MapContent (Vật chứa nhận Pan & Zoom)
      var contentObj = mapViewport.Find("MapContent");
      if (contentObj == null)
      {
        GameObject content = new GameObject("MapContent", typeof(RectTransform));
        content.transform.SetParent(mapViewport, false);
        mapContent = content.GetComponent<RectTransform>();
        mapContent.anchorMin = new Vector2(0.5f, 0.5f);
        mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.pivot = new Vector2(0.5f, 0.5f);
        mapContent.sizeDelta = mapImageSize;
      }
      else
      {
        mapContent = contentObj.GetComponent<RectTransform>();
        mapContent.sizeDelta = mapImageSize;
      }

      // 4. VillageImage (Ảnh toàn cảnh ngôi làng)
      var imgObj = mapContent.Find("VillageImage");
      if (imgObj == null)
      {
        GameObject img = new GameObject("VillageImage", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(mapContent, false);
        villageImage = img.GetComponent<Image>();
        villageImage.sprite = villageMapSprite;
        villageImage.type = Image.Type.Simple;
        villageImage.preserveAspect = true;
        villageImage.raycastTarget = false;

        RectTransform imgRt = img.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.5f);
        imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = Vector2.zero;
        imgRt.sizeDelta = mapImageSize;
      }
      else
      {
        villageImage = imgObj.GetComponent<Image>();
      }

      // 5. Landmarks Container
      var lmContainerObj = mapContent.Find("LandmarksContainer");
      if (lmContainerObj == null)
      {
        GameObject lmC = new GameObject("LandmarksContainer", typeof(RectTransform));
        lmC.transform.SetParent(mapContent, false);
        landmarksContainer = lmC.transform;
      }
      else
      {
        landmarksContainer = lmContainerObj;
      }

      // 6. Player Marker (Con trỏ Người Chơi)
      var playerObj = mapContent.Find("PlayerMarker");
      if (playerObj == null)
      {
        GameObject pm = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        pm.transform.SetParent(mapContent, false);
        playerMarkerTransform = pm.GetComponent<RectTransform>();
        playerMarkerTransform.sizeDelta = new Vector2(playerIconSize, playerIconSize);
        playerMarkerImage = pm.GetComponent<Image>();
        playerMarkerImage.sprite = playerIconSprite;
        playerMarkerImage.raycastTarget = false;
      }
      else
      {
        playerMarkerTransform = playerObj.GetComponent<RectTransform>();
        playerMarkerImage = playerObj.GetComponent<Image>();
        playerMarkerTransform.sizeDelta = new Vector2(playerIconSize, playerIconSize);
      }

      isHierarchyBuilt = true;
    }

    /// <summary>
    /// Yêu cầu 1: Giữ nguyên 100% vị trí các Landmark do người dùng đã chỉnh,
    /// ẩn nhãn Text nếu showLandmarkLabels = false
    /// </summary>
    public void ApplyLandmarkSettings()
    {
      if (landmarksContainer == null)
      {
        landmarksContainer = transform.Find("MapViewport/MapContent/LandmarksContainer");
      }
      if (landmarksContainer == null) return;

      // Duyệt qua tất cả các Landmark hiện có: TÔN TRỌNG VỊ TRÍ NGƯỜI DÙNG ĐÃ CHỈNH
      foreach (Transform landmark in landmarksContainer)
      {
        // 1. Tự động ẩn nhãn Text nếu showLandmarkLabels = false
        var label = landmark.Find("Label");
        if (label != null)
        {
          label.gameObject.SetActive(showLandmarkLabels);
        }

        // Tự động kiểm tra Text component
        var texts = landmark.GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
          if (t.transform != landmark)
          {
            t.gameObject.SetActive(showLandmarkLabels);
          }
        }
      }
    }

    public void AutoLoadSpritesIfMissing()
    {
#if UNITY_EDITOR
      if (villageMapSprite == null)
        villageMapSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/GameImage/VillageMap.png");
      if (playerIconSprite == null)
        playerIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/Icon/Map/Player_Icon.png");
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (minZoom > maxZoom) minZoom = maxZoom;
      defaultZoomOnOpen = Mathf.Clamp(defaultZoomOnOpen, minZoom, maxZoom);

      if (Application.isPlaying && isHierarchyBuilt)
      {
        ApplyLandmarkSettings();
        ClampPanOffset();
        UpdatePlayerMarker();
      }
    }
#endif
  }
}
