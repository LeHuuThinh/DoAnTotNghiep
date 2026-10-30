using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Trạng thái khám phá của từng phần tử trên bản đồ
  /// </summary>
  public enum MapDiscoveryState
  {
    Unexplored, // Chưa khám phá (tối màu / sương mù)
    Adjacent,   // Lân cận (biết có phòng, sáng mờ)
    Explored    // Đã khám phá (sáng bừng)
  }

  /// <summary>
  /// Cặp màu sắc (Nền sàn & Khung viền) cho từng trạng thái hiển thị
  /// </summary>
  [System.Serializable]
  public class ElementColorTheme
  {
    [Tooltip("Màu của lòng sàn (Fill)")]
    public Color fillColor;
    [Tooltip("Màu của khung viền (Border)")]
    public Color borderColor;

    public ElementColorTheme(Color fill, Color border)
    {
      fillColor = fill;
      borderColor = border;
    }
  }

  /// <summary>
  /// Quản lý toàn bộ hệ thống Bản Đồ Hầm Ngục (Dungeon Map - Task POL-03):
  /// - Hiển thị toàn màn hình, nằm dưới tất cả các UI khác trên Canvas
  /// - In chính xác cấu trúc phòng và hành lang theo kết quả thuật toán BSP
  /// - Tự động tính toán tỷ lệ để phòng luôn là HÌNH CHỮ NHẬT vát góc sắc nét (không bị bẹp thành hình thoi)
  /// - Đảm bảo hành lang hiển thị rõ ràng, nối khớp nối vào các phòng
  /// - Cơ chế sương mù khám phá (Fog of War)
  /// - Hiển thị Icon phòng (Boss, Key, Treasure, Start) luôn nhìn thấy
  /// - Hiển thị vị trí và hướng nhìn của Player thời gian thực
  /// - Hỗ trợ kéo chuột (Pan) và phóng to/thu nhỏ (Zoom)
  /// - Hỗ trợ tùy chỉnh trực tiếp bảng màu hiển thị ngay trên Inspector
  /// </summary>
  [RequireComponent(typeof(CanvasGroup))]
  public class DungeonMapManager : MonoBehaviour
  {
    public static DungeonMapManager Instance { get; private set; }

    [Header("=== CÀI ĐẶT BẬT/TẮT & GIAO DIỆN ===")]
    [Tooltip("Phím nóng bật/tắt bản đồ")]
    public KeyCode toggleKey = KeyCode.M;

    [Tooltip("Khóa di chuyển nhân vật khi mở bản đồ? (Mặc định: false - vừa chạy vừa xem map như ARPG)")]
    public bool freezePlayerWhenOpen = false;

    [Tooltip("Mở con trỏ chuột khi xem bản đồ để kéo/zoom?")]
    public bool unlockCursorWhenOpen = true;

    [Tooltip("Căn giữa vào vị trí Người Chơi khi mở bản đồ? (Mặc định: true)")]
    public bool centerOnPlayerOnOpen = true;

    [Tooltip("Độ zoom mặc định khi mở bản đồ (Mặc định: 2.0)")]
    [Range(0.5f, 5.0f)] public float defaultZoomOnOpen = 2.0f;

    [Tooltip("Tự động co giãn toàn cảnh hầm ngục khi mở bản đồ? (Chỉ áp dụng nếu centerOnPlayerOnOpen = false)")]
    public bool autoFitScreenOnOpen = false;

    [Tooltip("Độ mờ của lớp nền tối phủ toàn màn hình")]
    [Range(0f, 1f)] public float backgroundDimAlpha = 0.75f;

    [Header("=== TÀI NGUYÊN SPRITES 9-SLICE (UI RPG/GameUI) ===")]
    [Tooltip("Sprite viền phòng vát góc trắng (Room_Border)")]
    public Sprite roomBorderSprite;
    [Tooltip("Sprite ruột phòng vát góc trắng (Room_Fill)")]
    public Sprite roomFillSprite;
    [Tooltip("Sprite viền hành lang trắng (Corridor_Border)")]
    public Sprite corridorBorderSprite;
    [Tooltip("Sprite ruột hành lang trắng (Corridor_Fill)")]
    public Sprite corridorFillSprite;

    [Header("=== TÀI NGUYÊN SPRITES MỐI NỐI CHỮ L & T (UI RPG/MapUI) ===")]
    [Tooltip("Sprite viền góc vuông chữ L (Corridor_Border_L)")]
    public Sprite corridorBorderLSprite;
    [Tooltip("Sprite ruột góc vuông chữ L (Corridor_Fill_L)")]
    public Sprite corridorFillLSprite;
    [Tooltip("Sprite viền ngã ba chữ T (Corridor_Border_T)")]
    public Sprite corridorBorderTSprite;
    [Tooltip("Sprite ruột ngã ba chữ T (Corridor_Fill_T)")]
    public Sprite corridorFillTSprite;

    [Header("=== CÀI ĐẶT MỐI NỐI HÀNH LANG (L & T JUNCTIONS) ===")]
    [Tooltip("Kích thước bù trừ của toàn bộ khối mối nối (pixels, âm = thu nhỏ, dương = phóng to)")]
    [Range(-10f, 10f)] public float junctionSizeOffset = 0f;

    [Tooltip("Bật lớp nền đáy FloorBase để xóa viền cắt bên dưới ngã rẽ")]
    public bool enableJunctionFloorBase = true;

    [Tooltip("Độ co/giãn lề của nền sau FloorBase (pixels: âm = thụt vào trong để không lồi ra ngoài, dương = nở to ra)")]
    [Range(-10f, 6f)] public float junctionFloorBasePadding = -3f;

    [Header("=== TÀI NGUYÊN ICONS (UI RPG/Icon) ===")]
    [Tooltip("Icon vị trí Người Chơi (Player_Icon)")]
    public Sprite playerIconSprite;
    [Tooltip("Icon Phòng Trùm (Boss_Icon)")]
    public Sprite bossIconSprite;
    [Tooltip("Icon Phòng Chìa Khóa / Tế Đàn (Key_Icon)")]
    public Sprite keyIconSprite;
    [Tooltip("Icon Phòng Kho Báu (Treasure_Icon)")]
    public Sprite treasureIconSprite;
    [Tooltip("Icon Phòng Khởi Đầu (Tùy chọn)")]
    public Sprite startRoomIconSprite;

    [Header("=== MÀU SẮC PHÒNG (ROOM COLORS) ===")]
    [Tooltip("Phòng chưa khám phá (hiện mờ tối để thấy sơ đồ)")]
    public ElementColorTheme roomUnexplored = new ElementColorTheme(
      new Color(0.08f, 0.09f, 0.11f, 0.85f),
      new Color(0.35f, 0.35f, 0.38f, 0.85f)
    );

    [Tooltip("Phòng lân cận (khi phòng kề bên đã khám phá)")]
    public ElementColorTheme roomAdjacent = new ElementColorTheme(
      new Color(0.10f, 0.11f, 0.13f, 0.90f),
      new Color(0.55f, 0.50f, 0.42f, 0.90f)
    );

    [Tooltip("Phòng đã khám phá (sáng bừng rực rỡ vàng ánh kim)")]
    public ElementColorTheme roomExplored = new ElementColorTheme(
      new Color(0.12f, 0.13f, 0.16f, 1.00f),
      new Color(0.92f, 0.75f, 0.38f, 1.00f) // Vàng ánh kim
    );

    [Tooltip("Phòng người chơi đang đứng hiện tại (Highlighter)")]
    public ElementColorTheme roomCurrent = new ElementColorTheme(
      new Color(0.16f, 0.18f, 0.22f, 1.00f),
      new Color(1.00f, 0.90f, 0.45f, 1.00f) // Vàng sáng
    );

    [Header("=== MÀU SẮC HÀNH LANG (CORRIDOR COLORS) ===")]
    [Tooltip("Hành lang chưa khám phá (hiện mờ tối để thấy lối đi)")]
    public ElementColorTheme corridorUnexplored = new ElementColorTheme(
      new Color(0.08f, 0.09f, 0.11f, 0.85f),
      new Color(0.35f, 0.35f, 0.38f, 0.85f)
    );

    [Tooltip("Hành lang lân cận (nối với phòng đã khám phá)")]
    public ElementColorTheme corridorAdjacent = new ElementColorTheme(
      new Color(0.10f, 0.11f, 0.13f, 0.90f),
      new Color(0.55f, 0.50f, 0.42f, 0.90f)
    );

    [Tooltip("Hành lang đã khám phá")]
    public ElementColorTheme corridorExplored = new ElementColorTheme(
      new Color(0.12f, 0.13f, 0.16f, 1.00f),
      new Color(0.92f, 0.75f, 0.38f, 1.00f) // Vàng gold
    );

    [Header("=== ĐỘ DÀY VIỀN & HÌNH DẠNG (FIX LỖI HÌNH THOI & HÀNH LANG) ===")]
    [Tooltip("Độ co viền 9-slice phòng: Càng cao góc vát càng nhỏ, phòng càng ra hình chữ nhật rõ nét (Mặc định: 16)")]
    [Range(5f, 30f)] public float roomBorderMultiplier = 16f;

    [Tooltip("Độ co viền hành lang: Càng cao đường kẻ càng mảnh và sắc nét (Mặc định: 16)")]
    [Range(5f, 30f)] public float corridorBorderMultiplier = 16f;

    [Tooltip("Độ dày hiển thị tối thiểu của hành lang trên UI (pixels) để luôn nhìn rõ lối đi")]
    [Range(16f, 40f)] public float minCorridorThickness = 22f;

    [Tooltip("Độ dài bù trừ hai đầu hành lang để cắm sâu vào phòng, triệt tiêu khe hở")]
    [Range(0f, 10f)] public float corridorOverlapExtension = 0f;

    [Tooltip("Độ co nhẹ chiều dài hành lang để giấu kín đầu mấu vào bên trong block nối L/T và phòng (Mặc định: 6)")]
    [Range(0f, 12f)] public float corridorLengthTrim = 6f;

    [Tooltip("Xoay icon người chơi theo hướng nhìn? (Mặc định: false - giữ nguyên góc mặc định theo yêu cầu)")]
    public bool rotatePlayerMarker = false;

    [Header("=== THÔNG SỐ TỶ LỆ & ĐIỀU KHIỂN (MAP SCALE & PAN) ===")]
    [Tooltip("Kích thước UI (pixels) cho mỗi ô lưới tile của BSP")]
    [Range(4f, 20f)] public float pixelsPerTile = 10f;

    [Tooltip("Kích thước icon phòng thông thường (Chìa khóa, Kho báu) (pixels)")]
    [Range(16f, 64f)] public float roomIconSize = 36f;

    [Tooltip("Kích thước riêng cho Icon Phòng Trùm / Boss (pixels)")]
    [Range(16f, 96f)] public float bossIconSize = 48f;

    [Tooltip("Kích thước icon người chơi (pixels)")]
    public float playerIconSize = 28f;

    [Tooltip("Tốc độ phóng to thu nhỏ bằng con lăn chuột")]
    public float zoomSpeed = 0.15f;
    [Tooltip("Độ zoom nhỏ nhất (cho người chơi tự do thu nhỏ, mặc định: 1.0)")]
    public float minZoom = 1.0f;
    [Tooltip("Độ zoom lớn nhất (cho người chơi tự do phóng to, mặc định: 3.5)")]
    public float maxZoom = 3.5f;

    [Header("=== PHÒNG KHỞI ĐẦU DỰNG SẴN (STARTER ROOM) ===")]
    [Tooltip("Tự động hiển thị và kích hoạt phòng khởi đầu dựng sẵn")]
    public bool includeStarterRoom = true;
    [Tooltip("Kích thước lưới của phòng khởi đầu (Width, Height tính theo tile)")]
    public Vector2Int starterRoomTileSize = new Vector2Int(14, 12);

    // --- CÁC THÀNH PHẦN NỘI BỘ ---
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private RectTransform mapViewport;
    private RectTransform mapContent;
    private RectTransform playerMarkerTransform;
    private Image playerMarkerImage;

    private Transform cachedPlayerTransform;
    private PlayerController cachedPlayerController;
    private DungeonData activeDungeonData;
    private float worldTileSize = 2f;
    private Vector2 gridCenter;
    private Vector2 dungeonBoundingCenter;
    private Vector2 dungeonBoundingSize;

    private float currentZoom = 1f;
    private Vector2 panOffset = Vector2.zero;
    private bool isDragging = false;
    private Vector2 lastMousePosition;

    private bool isMapOpen = false;
    private bool isInitialized = false;

    // Quản lý các Node phòng và hành lang
    private class RoomNodeUI
    {
      public int id;
      public RectInt bounds;
      public RoomType type;
      public MapDiscoveryState state;
      public RectTransform root;
      public Image fillImage;
      public Image borderImage;
      public Image iconImage;
    }

    private class CorridorNodeUI
    {
      public int id;
      public RectInt bounds;
      public bool isVertical;
      public MapDiscoveryState state;
      public RectTransform root;
      public Image fillImage;
      public Image borderImage;
      public List<int> connectedRoomIds = new List<int>();
    }

    private class JunctionNodeUI
    {
      public int id;
      public Vector2 gridCenter;
      public float baseThickness;
      public MapDiscoveryState state;
      public RectTransform root;
      public RectTransform floorBaseRt;
      public Image floorBaseImage;
      public Image fillImage;
      public Image borderImage;
      public List<int> connectedRoomIds = new List<int>();
    }

    private Dictionary<int, RoomNodeUI> roomNodes = new Dictionary<int, RoomNodeUI>();
    private List<CorridorNodeUI> corridorNodes = new List<CorridorNodeUI>();
    private List<JunctionNodeUI> junctionNodes = new List<JunctionNodeUI>();
    private RectInt starterRoomBounds;
    private const int STARTER_ROOM_ID = -1;
    private int currentRoomId = -999;

    public bool IsOpen => isMapOpen;

    private void Awake()
    {
      Instance = this;

      // Đảm bảo Map UI luôn nằm dưới tất cả các UI khác trên Canvas
      transform.SetAsFirstSibling();

      canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

      // Mặc định ẩn map khi mới khởi động
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;
      isMapOpen = false;
    }

    private void Start()
    {
      // 1. Kết nối với DungeonManager
      if (DungeonManager.Instance != null)
      {
        DungeonManager.Instance.OnDungeonGenerated += HandleDungeonGenerated;
        if (DungeonManager.Instance.currentData != null)
        {
          HandleDungeonGenerated(DungeonManager.Instance.currentData);
        }
      }

      // 2. Tự động tìm Player
      FindPlayer();
    }

    private void OnDestroy()
    {
      if (DungeonManager.Instance != null)
      {
        DungeonManager.Instance.OnDungeonGenerated -= HandleDungeonGenerated;
      }
    }

    private void Update()
    {
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

      // 3. Cập nhật vị trí Player và Fog of War thời gian thực
      UpdatePlayerAndFogOfWar();

      // 4. Xử lý Pan và Zoom khi Map đang mở
      if (isMapOpen)
      {
        HandlePanAndZoom();
      }
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
        var pc = FindObjectOfType<PlayerController>();
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

      // Cập nhật ngay vị trí Player trước khi định vị bản đồ
      UpdatePlayerAndFogOfWar();

      // Mặc định mở ngay tại vị trí Player với độ zoom defaultZoomOnOpen (2.0)
      if (centerOnPlayerOnOpen)
      {
        SetZoom(defaultZoomOnOpen);
        CenterMapOnPlayer();
      }
      else if (autoFitScreenOnOpen)
      {
        FitMapToScreen();
      }
      else
      {
        SetZoom(defaultZoomOnOpen);
        CenterMapOnPlayer();
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

      // Khóa lại con trỏ chuột cho góc nhìn 3D nếu túi đồ không mở
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
    /// Xây dựng toàn bộ sơ đồ phòng và hành lang ngay khi Dungeon được sinh xong
    /// </summary>
    public void HandleDungeonGenerated(DungeonData data)
    {
      if (data == null) return;
      activeDungeonData = data;
      worldTileSize = (DungeonManager.Instance != null && DungeonManager.Instance.generator != null)
        ? DungeonManager.Instance.generator.tileSize
        : 2f;

      BuildMapHierarchy();
      BuildDungeonElements(data);
      isInitialized = true;

      // Khởi tạo trạng thái phòng khởi đầu
      InitStarterRoomDiscovery();

      // Cập nhật hiển thị màu sắc ban đầu
      RefreshAllVisuals();
    }

    /// <summary>
    /// Tự động dựng khung cấu trúc UI Canvas (Viewport, Content, PlayerMarker, Background) nếu chưa có
    /// </summary>
    private void BuildMapHierarchy()
    {
      // 1. Background Dimmer (Phủ tối toàn màn hình)
      var bgObj = transform.Find("MapBackground");
      if (bgObj == null)
      {
        GameObject bg = new GameObject("MapBackground", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        backgroundImage = bg.GetComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.04f, 0.06f, backgroundDimAlpha);
        backgroundImage.raycastTarget = true; // Bắt sự kiện kéo chuột trên nền

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
      }
      else
      {
        backgroundImage = bgObj.GetComponent<Image>();
      }

      // 2. Viewport (Vùng chứa bản đồ)
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

      // 3. Content Container (Vật chứa có thể Pan và Zoom)
      var contentObj = mapViewport.Find("MapContent");
      if (contentObj == null)
      {
        GameObject content = new GameObject("MapContent", typeof(RectTransform));
        content.transform.SetParent(mapViewport, false);
        mapContent = content.GetComponent<RectTransform>();
        mapContent.anchorMin = new Vector2(0.5f, 0.5f);
        mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.pivot = new Vector2(0.5f, 0.5f);
        mapContent.sizeDelta = Vector2.zero;
      }
      else
      {
        mapContent = contentObj.GetComponent<RectTransform>();
      }

      // 4. Player Marker (Con trỏ Người Chơi)
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
      }
    }

    /// <summary>
    /// Sinh các UI Room và UI Corridor trên Canvas chính xác theo kích thước của BSP
    /// </summary>
    private void BuildDungeonElements(DungeonData data)
    {
      // Xóa các phần tử cũ nếu có
      foreach (Transform child in mapContent)
      {
        if (child == playerMarkerTransform) continue;
        Destroy(child.gameObject);
      }
      roomNodes.Clear();
      corridorNodes.Clear();
      junctionNodes.Clear();

      int totalGridW = data.mapWidth;
      int totalGridH = data.mapHeight + data.startRoomHeight;
      gridCenter = new Vector2(totalGridW / 2f, totalGridH / 2f);

      // Tính Bounding Box bao trùm toàn bộ hầm ngục để auto-fit
      int minGX = int.MaxValue, maxGX = int.MinValue;
      int minGY = int.MaxValue, maxGY = int.MinValue;

      // 1. TÍNH TOÁN VỊ TRÍ PHÒNG KHỞI ĐẦU DỰNG SẴN (STARTER ROOM)
      if (includeStarterRoom)
      {
        Transform doorMarker = (DungeonManager.Instance != null && DungeonManager.Instance.generator != null)
          ? DungeonManager.Instance.generator.startDoorMarker
          : null;

        int doorGridX = totalGridW / 2;
        int doorGridY = data.startRoomHeight;
        if (doorMarker != null)
        {
          doorGridX = Mathf.FloorToInt(doorMarker.position.x / worldTileSize);
          doorGridY = Mathf.FloorToInt(doorMarker.position.z / worldTileSize);
        }

        int sW = starterRoomTileSize.x;
        int sH = starterRoomTileSize.y;
        int sX = doorGridX - sW / 2;
        int sY = Mathf.Max(0, doorGridY - sH);
        starterRoomBounds = new RectInt(sX, sY, sW, sH);

        minGX = Mathf.Min(minGX, sX);
        maxGX = Mathf.Max(maxGX, sX + sW);
        minGY = Mathf.Min(minGY, sY);
        maxGY = Mathf.Max(maxGY, sY + sH);
      }

      // 2. DỰNG CÁC ĐOẠN HÀNH LANG THẲNG (LỚP DƯỚI CÙNG)
      int corridorIdx = 0;
      foreach (var c in data.corridors)
      {
        CreateCorridorUI(corridorIdx++, c, data);

        minGX = Mathf.Min(minGX, c.x);
        maxGX = Mathf.Max(maxGX, c.x + c.width);
        minGY = Mathf.Min(minGY, c.y);
        maxGY = Mathf.Max(maxGY, c.y + c.height);
      }

      // Đưa hành lang Boss (Đại Lộ) lên trên cùng trong danh sách hành lang để che kín đầu cắm của hành lang dẫn vào
      if (data.bossCorridor.width > 0)
      {
        var bossCorrNode = corridorNodes.Find(cn => cn.bounds.Equals(data.bossCorridor));
        if (bossCorrNode != null && bossCorrNode.root != null)
        {
          bossCorrNode.root.SetAsLastSibling();
        }
      }

      // 3. LAN TRUYỀN LIÊN KẾT PHÒNG QUA CHUỖI HÀNH LANG LIÊN HOÀN
      PropagateCorridorConnections();

      // 4. DỰNG CÁC MỐI NỐI CHỮ L & NGÃ BA CHỮ T (LỚP GIỮA - ĐÈ LÊN ĐẦU HÀNH LANG THẲNG)
      BuildCorridorJunctions(data);

      // 5. DỰNG PHÒNG KHỞI ĐẦU (LỚP TRÊN - CHE MỐI NỐI NỐI VÀO PHÒNG)
      if (includeStarterRoom)
      {
        CreateRoomUI(STARTER_ROOM_ID, starterRoomBounds, RoomType.Start, startRoomIconSprite);
      }

      // 6. DỰNG CÁC PHÒNG CHÍNH CỦA BSP (LỚP TRÊN - CHE MỐI NỐI HÀNH LANG CẮM VÀO PHÒNG)
      foreach (var r in data.roomDataList)
      {
        Sprite icon = null;
        // Chi duy nhat can phong duoc chon lam data.keyRoom moi hien thi Key_Icon!
        if (data.keyRoom != null && r.id == data.keyRoom.id)
        {
          icon = keyIconSprite;
        }
        else
        {
          switch (r.type)
          {
            case RoomType.Boss:
              icon = bossIconSprite;
              break;
            case RoomType.Treasure:
              icon = treasureIconSprite;
              break;
            case RoomType.Start:
              icon = startRoomIconSprite;
              break;
          }
        }

        CreateRoomUI(r.id, r.bounds, r.type, icon);

        minGX = Mathf.Min(minGX, r.bounds.x);
        maxGX = Mathf.Max(maxGX, r.bounds.x + r.bounds.width);
        minGY = Mathf.Min(minGY, r.bounds.y);
        maxGY = Mathf.Max(maxGY, r.bounds.y + r.bounds.height);
      }

      // Lưu trữ Bounding Center & Size cho tính năng Auto-Fit
      if (minGX != int.MaxValue)
      {
        dungeonBoundingCenter = new Vector2((minGX + maxGX) / 2f, (minGY + maxGY) / 2f);
        dungeonBoundingSize = new Vector2(maxGX - minGX, maxGY - minGY);
      }
      else
      {
        dungeonBoundingCenter = gridCenter;
        dungeonBoundingSize = new Vector2(totalGridW, totalGridH);
      }

      // Đưa Player Marker lên trên cùng của MapContent
      if (playerMarkerTransform != null)
      {
        playerMarkerTransform.SetAsLastSibling();
      }
    }

    /// <summary>
    /// Tạo UI cho một căn phòng: Luôn giữ nguyên chuẩn tỷ lệ Rectangular từ bounds của BSP
    /// </summary>
    private void CreateRoomUI(int id, RectInt bounds, RoomType type, Sprite iconSprite)
    {
      GameObject roomObj = new GameObject($"Room_{id}_{type}", typeof(RectTransform));
      roomObj.transform.SetParent(mapContent, false);

      RectTransform rt = roomObj.GetComponent<RectTransform>();
      Vector2 uiPos = GridToUIPosition(bounds.center);
      Vector2 uiSize = new Vector2(bounds.width * pixelsPerTile, bounds.height * pixelsPerTile);

      rt.anchoredPosition = uiPos;
      rt.sizeDelta = uiSize;

      // 1. Layer Nền (Fill)
      GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
      fillObj.transform.SetParent(roomObj.transform, false);
      RectTransform fillRt = fillObj.GetComponent<RectTransform>();
      fillRt.anchorMin = Vector2.zero;
      fillRt.anchorMax = Vector2.one;
      fillRt.sizeDelta = Vector2.zero;
      Image fillImg = fillObj.GetComponent<Image>();
      fillImg.sprite = roomFillSprite;
      fillImg.type = Image.Type.Sliced;
      fillImg.pixelsPerUnitMultiplier = roomBorderMultiplier;
      fillImg.raycastTarget = false;

      // 2. Layer Khung Viền (Border)
      GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
      borderObj.transform.SetParent(roomObj.transform, false);
      RectTransform borderRt = borderObj.GetComponent<RectTransform>();
      borderRt.anchorMin = Vector2.zero;
      borderRt.anchorMax = Vector2.one;
      borderRt.sizeDelta = Vector2.zero;
      Image borderImg = borderObj.GetComponent<Image>();
      borderImg.sprite = roomBorderSprite;
      borderImg.type = Image.Type.Sliced;
      borderImg.pixelsPerUnitMultiplier = roomBorderMultiplier;
      borderImg.raycastTarget = false;

      // 3. Layer Icon Chức Năng (Luôn luôn hiển thị theo yêu cầu)
      Image iconImg = null;
      if (iconSprite != null)
      {
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(roomObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        float currentIconSize = (type == RoomType.Boss) ? bossIconSize : roomIconSize;
        iconRt.sizeDelta = new Vector2(currentIconSize, currentIconSize);
        iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = Color.white; // Luôn hiển thị rõ nét
        iconImg.raycastTarget = false;
      }

      RoomNodeUI node = new RoomNodeUI
      {
        id = id,
        bounds = bounds,
        type = type,
        state = MapDiscoveryState.Unexplored,
        root = rt,
        fillImage = fillImg,
        borderImage = borderImg,
        iconImage = iconImg
      };

      roomNodes[id] = node;
    }

    /// <summary>
    /// Tạo UI cho một đoạn hành lang: Đảm bảo độ dày tối thiểu và viền 2 bên sườn sắc nét
    /// </summary>
    private void CreateCorridorUI(int id, RectInt bounds, DungeonData data)
    {
      GameObject corObj = new GameObject($"Corridor_{id}", typeof(RectTransform));
      corObj.transform.SetParent(mapContent, false);

      RectTransform rt = corObj.GetComponent<RectTransform>();
      Vector2 uiPos = GridToUIPosition(bounds.center);

      // Nhận diện hành lang Dọc hay Ngang
      bool isVertical = bounds.height > bounds.width;

      // Tính chiều dài và chiều dày thực tế trên UI
      float rawLength = (isVertical ? bounds.height : bounds.width) * pixelsPerTile;
      float rawThickness = (isVertical ? bounds.width : bounds.height) * pixelsPerTile;

      // Đảm bảo hành lang đạt độ dày tối thiểu minCorridorThickness để hiển thị rõ 2 đường viền
      float displayThickness = Mathf.Max(rawThickness, minCorridorThickness);
      // Chiều dài hiển thị: Co nhẹ corridorLengthTrim (mỗi đầu 3px) để đầu hành lang luôn giấu kín hoàn hảo bên trong block nối L/T và phòng
      // Riêng Đại Lộ Boss (bossCorridor) là đại lộ khổng lồ cắm trực tiếp vào Đấu trường Boss, không co ngắn để triệt tiêu hoàn toàn khe hở!
      bool isBossAvenue = (data != null && bounds.Equals(data.bossCorridor));
      float lengthTrim = isBossAvenue ? 0f : corridorLengthTrim;
      float displayLength = Mathf.Max(displayThickness, rawLength - lengthTrim);

      rt.anchoredPosition = uiPos;

      // Xoay 90 độ cho hành lang dọc để 2 đường viền chạy dọc hai bên sườn
      if (isVertical)
      {
        rt.sizeDelta = new Vector2(displayLength, displayThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, 90f);
      }
      else
      {
        rt.sizeDelta = new Vector2(displayLength, displayThickness);
        rt.localRotation = Quaternion.identity;
      }

      // 1. Layer Nền (Fill)
      GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
      fillObj.transform.SetParent(corObj.transform, false);
      RectTransform fillRt = fillObj.GetComponent<RectTransform>();
      fillRt.anchorMin = Vector2.zero;
      fillRt.anchorMax = Vector2.one;
      fillRt.sizeDelta = Vector2.zero;
      Image fillImg = fillObj.GetComponent<Image>();
      fillImg.sprite = corridorFillSprite;
      fillImg.type = Image.Type.Sliced;
      fillImg.pixelsPerUnitMultiplier = corridorBorderMultiplier;
      fillImg.raycastTarget = false;

      // 2. Layer Viền (Border)
      GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
      borderObj.transform.SetParent(corObj.transform, false);
      RectTransform borderRt = borderObj.GetComponent<RectTransform>();
      borderRt.anchorMin = Vector2.zero;
      borderRt.anchorMax = Vector2.one;
      borderRt.sizeDelta = Vector2.zero;
      Image borderImg = borderObj.GetComponent<Image>();
      borderImg.sprite = corridorBorderSprite;
      borderImg.type = Image.Type.Sliced;
      borderImg.pixelsPerUnitMultiplier = corridorBorderMultiplier;
      borderImg.raycastTarget = false;

      CorridorNodeUI cNode = new CorridorNodeUI
      {
        id = id,
        bounds = bounds,
        isVertical = isVertical,
        state = MapDiscoveryState.Unexplored,
        root = rt,
        fillImage = fillImg,
        borderImage = borderImg
      };

      // Tìm các phòng mà hành lang này tiếp xúc
      RectInt exp = new RectInt(bounds.x - 1, bounds.y - 1, bounds.width + 2, bounds.height + 2);
      foreach (var r in data.roomDataList)
      {
        if (exp.Overlaps(r.bounds))
        {
          cNode.connectedRoomIds.Add(r.id);
        }
      }
      if (includeStarterRoom && exp.Overlaps(starterRoomBounds))
      {
        cNode.connectedRoomIds.Add(STARTER_ROOM_ID);
      }

      corridorNodes.Add(cNode);
    }

    /// <summary>
    /// Lan truyen lien ket phong qua cac doan hanh lang giao nhau.
    /// Giup cac doan hanh lang 3 doan (Z-shape/dogleg qua split line) hoac nhieu doan
    /// cung nhan dien duoc cac can phong o 2 dau de sang len dong bo!
    /// </summary>
    private void PropagateCorridorConnections()
    {
      bool changed = true;
      int iterations = 0;
      while (changed && iterations < 8)
      {
        changed = false;
        iterations++;

        for (int i = 0; i < corridorNodes.Count; i++)
        {
          RectInt expI = new RectInt(corridorNodes[i].bounds.x - 1, corridorNodes[i].bounds.y - 1, corridorNodes[i].bounds.width + 2, corridorNodes[i].bounds.height + 2);

          for (int j = i + 1; j < corridorNodes.Count; j++)
          {
            if (expI.Overlaps(corridorNodes[j].bounds))
            {
              // Chia se phong lien ket giua 2 doan hanh lang giao nhau
              foreach (int rId in corridorNodes[i].connectedRoomIds)
              {
                if (!corridorNodes[j].connectedRoomIds.Contains(rId))
                {
                  corridorNodes[j].connectedRoomIds.Add(rId);
                  changed = true;
                }
              }

              foreach (int rId in corridorNodes[j].connectedRoomIds)
              {
                if (!corridorNodes[i].connectedRoomIds.Contains(rId))
                {
                  corridorNodes[i].connectedRoomIds.Add(rId);
                  changed = true;
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Tìm tất cả các giao điểm và chỗ ngoặt giữa các đoạn hành lang để đặt sprite góc vuông L hoặc ngã ba T
    /// </summary>
    private void BuildCorridorJunctions(DungeonData data)
    {
      if (corridorBorderLSprite == null && corridorBorderTSprite == null) return;
      if (data == null || data.corridors == null || data.corridors.Count < 2) return;

      // 1. Phân loại hành lang ngang và hành lang dọc
      List<CorridorNodeUI> horizNodes = new List<CorridorNodeUI>();
      List<CorridorNodeUI> vertNodes = new List<CorridorNodeUI>();
      foreach (var c in corridorNodes)
      {
        if (c.isVertical) vertNodes.Add(c);
        else horizNodes.Add(c);
      }

      // 2. Tìm tất cả các giao điểm giữa hành lang ngang và hành lang dọc
      List<RawJunctionData> rawList = new List<RawJunctionData>();

      foreach (var h in horizNodes)
      {
        foreach (var v in vertNodes)
        {
          int x1 = Mathf.Max(h.bounds.x, v.bounds.x);
          int x2 = Mathf.Min(h.bounds.xMax, v.bounds.xMax);
          int y1 = Mathf.Max(h.bounds.y, v.bounds.y);
          int y2 = Mathf.Min(h.bounds.yMax, v.bounds.yMax);

          if (x1 < x2 && y1 < y2)
          {
            // Kiểm tra các nhánh thực sự vươn ra khỏi giao điểm (> 0 vì BSP đã căn chuẩn)
            bool hasL = (x1 - h.bounds.x) > 0;
            bool hasR = (h.bounds.xMax - x2) > 0;
            bool hasD = (y1 - v.bounds.y) > 0;
            bool hasU = (v.bounds.yMax - y2) > 0;

            float cx = (x1 + x2) / 2f;
            float cy = (y1 + y2) / 2f;
            float rawThick = Mathf.Min(h.bounds.height, v.bounds.width) * pixelsPerTile;
            float displayThick = Mathf.Max(rawThick, minCorridorThickness);

            RawJunctionData rj = new RawJunctionData
            {
              center = new Vector2(cx, cy),
              hasLeft = hasL,
              hasRight = hasR,
              hasDown = hasD,
              hasUp = hasU,
              thickness = displayThick
            };
            foreach (var rId in h.connectedRoomIds) rj.connectedRooms.Add(rId);
            foreach (var rId in v.connectedRoomIds) rj.connectedRooms.Add(rId);

            rawList.Add(rj);
          }
        }
      }

      // 3. Gom cụm các giao điểm trùng vị trí (<= 0.5 tile để tránh gộp nhầm 2 góc cua L gần nhau)
      List<RawJunctionData> mergedList = new List<RawJunctionData>();
      foreach (var rj in rawList)
      {
        RawJunctionData existing = null;
        foreach (var m in mergedList)
        {
          if (Vector2.Distance(rj.center, m.center) <= 0.5f)
          {
            existing = m;
            break;
          }
        }

        if (existing != null)
        {
          existing.center = (existing.center + rj.center) / 2f;
          existing.hasLeft |= rj.hasLeft;
          existing.hasRight |= rj.hasRight;
          existing.hasDown |= rj.hasDown;
          existing.hasUp |= rj.hasUp;
          existing.thickness = Mathf.Max(existing.thickness, rj.thickness);
          foreach (var rId in rj.connectedRooms) existing.connectedRooms.Add(rId);
        }
        else
        {
          mergedList.Add(rj);
        }
      }

      // 4. Tạo UI element cho từng mối nối
      int junctionIdx = 0;
      foreach (var j in mergedList)
      {
        int branchCount = (j.hasLeft ? 1 : 0) + (j.hasRight ? 1 : 0) + (j.hasDown ? 1 : 0) + (j.hasUp ? 1 : 0);

        Sprite borderSprite = null;
        Sprite fillSprite = null;
        float angle = 0f;

        if (branchCount == 2)
        {
          // Chữ L: Sprite gốc mở sang Trái & Dưới (Tường ở Trên & Phải)
          borderSprite = corridorBorderLSprite;
          fillSprite = corridorFillLSprite;

          if (j.hasLeft && j.hasDown) angle = 0f;
          else if (j.hasDown && j.hasRight) angle = 90f;
          else if (j.hasRight && j.hasUp) angle = 180f;
          else if (j.hasLeft && j.hasUp) angle = 270f;
          else continue; // Hai nhánh đối đỉnh thẳng hàng, không phải góc vuông
        }
        else if (branchCount == 3)
        {
          // Chữ T: Sprite gốc mở sang Trái, Phải, Dưới (Tường ở Trên)
          borderSprite = corridorBorderTSprite;
          fillSprite = corridorFillTSprite;

          if (j.hasLeft && j.hasRight && j.hasDown) angle = 0f;
          else if (j.hasDown && j.hasUp && j.hasRight) angle = 90f;
          else if (j.hasLeft && j.hasRight && j.hasUp) angle = 180f;
          else if (j.hasDown && j.hasUp && j.hasLeft) angle = 270f;
        }
        else if (branchCount == 4)
        {
          // Ngã tư thông thoáng 4 hướng: Chỉ cần Fill che phủ sàn, không có viền bao
          borderSprite = null;
          fillSprite = corridorFillTSprite != null ? corridorFillTSprite : corridorFillLSprite;
          angle = 0f;
        }
        else
        {
          // 1 nhánh cụt, bỏ qua
          continue;
        }

        if (borderSprite == null && fillSprite == null) continue;

        CreateJunctionUI(junctionIdx++, j.center, j.thickness, angle, borderSprite, fillSprite, j.connectedRooms);
      }
    }

    private class RawJunctionData
    {
      public Vector2 center;
      public bool hasLeft;
      public bool hasRight;
      public bool hasDown;
      public bool hasUp;
      public float thickness;
      public HashSet<int> connectedRooms = new HashSet<int>();
    }

    /// <summary>
    /// Tạo UI cho một mối nối chữ L hoặc chữ T: Dùng chế độ 9-slice với cùng corridorBorderMultiplier
    /// để khớp hoàn hảo từng pixel với các đoạn hành lang thẳng kề bên!
    /// </summary>
    private void CreateJunctionUI(int id, Vector2 gridCenterPos, float thickness, float rotationAngle, Sprite borderSprite, Sprite fillSprite, HashSet<int> connectedRooms)
    {
      GameObject jObj = new GameObject($"Junction_{id}", typeof(RectTransform));
      jObj.transform.SetParent(mapContent, false);

      RectTransform rt = jObj.GetComponent<RectTransform>();
      Vector2 uiPos = GridToUIPosition(gridCenterPos);
      rt.anchoredPosition = uiPos;
      float actualThickness = Mathf.Max(6f, thickness + junctionSizeOffset);
      rt.sizeDelta = new Vector2(actualThickness, actualThickness);
      rt.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);

      // 0. Layer Đáy Sàn Liền Mạch (FloorBase) - Xóa sổ 100% mọi đường viền hành lang thẳng bên dưới đâm vào ngã rẽ
      GameObject baseObj = new GameObject("FloorBase", typeof(RectTransform), typeof(Image));
      baseObj.transform.SetParent(jObj.transform, false);
      RectTransform baseRt = baseObj.GetComponent<RectTransform>();
      baseRt.anchorMin = Vector2.zero;
      baseRt.anchorMax = Vector2.one;
      baseRt.sizeDelta = new Vector2(junctionFloorBasePadding * 2f, junctionFloorBasePadding * 2f);
      Image baseImg = baseObj.GetComponent<Image>();
      baseImg.enabled = enableJunctionFloorBase;
      baseImg.color = corridorUnexplored.fillColor;
      baseImg.raycastTarget = false;

      // 1. Layer Nền (Fill) - Chuẩn 9-Slice đồng bộ với hành lang thẳng
      Image fillImg = null;
      if (fillSprite != null)
      {
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(jObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillImg = fillObj.GetComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        fillImg.raycastTarget = false;
      }

      // 2. Layer Viền (Border) - Chuẩn 9-Slice đồng bộ với hành lang thẳng
      Image borderImg = null;
      if (borderSprite != null)
      {
        GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(jObj.transform, false);
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;
        borderImg = borderObj.GetComponent<Image>();
        borderImg.sprite = borderSprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        borderImg.raycastTarget = false;
      }

      JunctionNodeUI jNode = new JunctionNodeUI
      {
        id = id,
        gridCenter = gridCenterPos,
        baseThickness = thickness,
        state = MapDiscoveryState.Unexplored,
        root = rt,
        floorBaseRt = baseRt,
        floorBaseImage = baseImg,
        fillImage = fillImg,
        borderImage = borderImg,
        connectedRoomIds = new List<int>(connectedRooms)
      };

      junctionNodes.Add(jNode);
    }

    /// <summary>
    /// Kích hoạt phòng khởi đầu sáng rõ ngay từ đầu
    /// </summary>
    private void InitStarterRoomDiscovery()
    {
      if (includeStarterRoom && roomNodes.ContainsKey(STARTER_ROOM_ID))
      {
        roomNodes[STARTER_ROOM_ID].state = MapDiscoveryState.Explored;

        // Kích hoạt các hành lang nối với StarterRoom và phòng đầu tiên
        foreach (var c in corridorNodes)
        {
          if (c.connectedRoomIds.Contains(STARTER_ROOM_ID))
          {
            c.state = MapDiscoveryState.Explored;
            foreach (var rId in c.connectedRoomIds)
            {
              if (rId != STARTER_ROOM_ID && roomNodes.ContainsKey(rId) && roomNodes[rId].state == MapDiscoveryState.Unexplored)
              {
                roomNodes[rId].state = MapDiscoveryState.Adjacent;
              }
            }
          }
        }

        // Kích hoạt các mối nối L và T gắn với StarterRoom
        foreach (var j in junctionNodes)
        {
          if (j.connectedRoomIds.Contains(STARTER_ROOM_ID))
          {
            j.state = MapDiscoveryState.Explored;
          }
        }
      }
      else if (activeDungeonData != null && activeDungeonData.startRoom != null && roomNodes.ContainsKey(activeDungeonData.startRoom.id))
      {
        RevealRoom(activeDungeonData.startRoom.id);
      }
    }

    /// <summary>
    /// Cập nhật vị trí Player và kích hoạt sương mù khi bước vào phòng mới
    /// </summary>
    private void UpdatePlayerAndFogOfWar()
    {
      FindPlayer();
      if (cachedPlayerTransform == null || activeDungeonData == null) return;

      Vector3 pos = cachedPlayerTransform.position;
      float gx = pos.x / worldTileSize;
      float gy = pos.z / worldTileSize;
      int gridX = Mathf.FloorToInt(gx);
      int gridY = Mathf.FloorToInt(gy);

      // 1. Cập nhật Player Marker trên UI
      if (playerMarkerTransform != null)
      {
        Vector2 markerPos = GridToUIPosition(new Vector2(gx, gy));
        playerMarkerTransform.anchoredPosition = markerPos;
        if (rotatePlayerMarker)
        {
          playerMarkerTransform.localEulerAngles = new Vector3(0f, 0f, -cachedPlayerTransform.eulerAngles.y);
        }
        else
        {
          playerMarkerTransform.localEulerAngles = Vector3.zero;
        }
      }

      // 2. Xác định phòng hiện tại của người chơi
      int newRoomId = -999;
      if (includeStarterRoom && starterRoomBounds.Contains(new Vector2Int(gridX, gridY)))
      {
        newRoomId = STARTER_ROOM_ID;
      }
      else
      {
        RoomData r = activeDungeonData.GetRoomAt(gridX, gridY);
        if (r != null)
        {
          newRoomId = r.id;
        }
      }

      // 3. Nếu người chơi bước sang phòng mới
      if (newRoomId != -999 && newRoomId != currentRoomId)
      {
        currentRoomId = newRoomId;
        RevealRoom(currentRoomId);
        RefreshAllVisuals();
      }
    }

    /// <summary>
    /// Kích hoạt một phòng sang trạng thái Explored, các phòng lân cận sang Adjacent, và hành lang sáng lên
    /// </summary>
    public void RevealRoom(int roomId)
    {
      if (!roomNodes.ContainsKey(roomId)) return;

      // 1. Phòng hiện tại sáng rõ (Explored)
      roomNodes[roomId].state = MapDiscoveryState.Explored;

      // 2. Kích hoạt các hành lang nối với phòng này
      foreach (var c in corridorNodes)
      {
        if (c.connectedRoomIds.Contains(roomId))
        {
          c.state = MapDiscoveryState.Explored;

          foreach (var otherId in c.connectedRoomIds)
          {
            if (otherId != roomId && roomNodes.ContainsKey(otherId))
            {
              if (roomNodes[otherId].state == MapDiscoveryState.Unexplored)
              {
                roomNodes[otherId].state = MapDiscoveryState.Adjacent;
              }
            }
          }
        }
      }

      // Kích hoạt các mối nối L và T nối với phòng này
      foreach (var j in junctionNodes)
      {
        if (j.connectedRoomIds.Contains(roomId))
        {
          j.state = MapDiscoveryState.Explored;
        }
      }

      // 3. Kiểm tra qua đồ thị kề BFS adjacency của DungeonData
      if (activeDungeonData != null && activeDungeonData.adjacency != null && activeDungeonData.adjacency.ContainsKey(roomId))
      {
        foreach (int neighborId in activeDungeonData.adjacency[roomId])
        {
          if (roomNodes.ContainsKey(neighborId) && roomNodes[neighborId].state == MapDiscoveryState.Unexplored)
          {
            roomNodes[neighborId].state = MapDiscoveryState.Adjacent;
          }
        }
      }
    }

    /// <summary>
    /// Cập nhật màu sắc cho tất cả các phòng và hành lang theo đúng Inspector Theme
    /// </summary>
    public void RefreshAllVisuals()
    {
      // 1. Cập nhật phòng
      foreach (var kvp in roomNodes)
      {
        RoomNodeUI node = kvp.Value;
        bool isCurrent = (node.id == currentRoomId);

        ElementColorTheme theme;
        if (isCurrent)
        {
          theme = roomCurrent;
        }
        else
        {
          switch (node.state)
          {
            case MapDiscoveryState.Explored:
              theme = roomExplored;
              break;
            case MapDiscoveryState.Adjacent:
              theme = roomAdjacent;
              break;
            default:
              theme = roomUnexplored;
              break;
          }
        }

        if (node.fillImage != null)
        {
          node.fillImage.color = theme.fillColor;
          node.fillImage.pixelsPerUnitMultiplier = roomBorderMultiplier;
        }
        if (node.borderImage != null)
        {
          node.borderImage.color = theme.borderColor;
          node.borderImage.pixelsPerUnitMultiplier = roomBorderMultiplier;
        }
        if (node.iconImage != null)
        {
          float curIconSize = (node.type == RoomType.Boss) ? bossIconSize : roomIconSize;
          node.iconImage.rectTransform.sizeDelta = new Vector2(curIconSize, curIconSize);
        }
      }

      // 2. Cập nhật hành lang
      foreach (var c in corridorNodes)
      {
        ElementColorTheme theme;
        switch (c.state)
        {
          case MapDiscoveryState.Explored:
            theme = corridorExplored;
            break;
          case MapDiscoveryState.Adjacent:
            theme = corridorAdjacent;
            break;
          default:
            theme = corridorUnexplored;
            break;
        }

        if (c.fillImage != null)
        {
          c.fillImage.color = theme.fillColor;
          c.fillImage.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        }
        if (c.borderImage != null)
        {
          c.borderImage.color = theme.borderColor;
          c.borderImage.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        }
      }

      // 3. Cập nhật mối nối hành lang chữ L và chữ T
      foreach (var j in junctionNodes)
      {
        ElementColorTheme theme;
        switch (j.state)
        {
          case MapDiscoveryState.Explored:
            theme = corridorExplored;
            break;
          case MapDiscoveryState.Adjacent:
            theme = corridorAdjacent;
            break;
          default:
            theme = corridorUnexplored;
            break;
        }

        float actualThickness = Mathf.Max(6f, j.baseThickness + junctionSizeOffset);
        if (j.root != null)
        {
          j.root.sizeDelta = new Vector2(actualThickness, actualThickness);
        }
        if (j.floorBaseRt != null)
        {
          j.floorBaseRt.sizeDelta = new Vector2(junctionFloorBasePadding * 2f, junctionFloorBasePadding * 2f);
        }
        if (j.floorBaseImage != null)
        {
          j.floorBaseImage.enabled = enableJunctionFloorBase;
          j.floorBaseImage.color = theme.fillColor;
        }
        if (j.fillImage != null)
        {
          j.fillImage.color = theme.fillColor;
          j.fillImage.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        }
        if (j.borderImage != null)
        {
          j.borderImage.color = theme.borderColor;
          j.borderImage.pixelsPerUnitMultiplier = corridorBorderMultiplier;
        }
      }

      // 4. Cập nhật độ mờ nền
      if (backgroundImage != null)
      {
        backgroundImage.color = new Color(0.04f, 0.04f, 0.06f, backgroundDimAlpha);
      }
    }

    /// <summary>
    /// Điều khiển Kéo Chuột (Pan) và Lăn Chuột (Zoom)
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
        mapContent.anchoredPosition = panOffset;
      }

      // 2. Xử lý Zoom (Con lăn chuột - giữ tâm nhìn ổn định khi zoom)
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
          mapContent.anchoredPosition = panOffset;
        }
      }
    }

    /// <summary>
    /// Tự động tính toán tỷ lệ phóng và căn giữa để toàn bộ hầm ngục vừa vặn màn hình
    /// </summary>
    public void FitMapToScreen()
    {
      if (mapContent == null || mapViewport == null) return;

      Vector2 viewportSize = mapViewport.rect.size;
      if (viewportSize.x <= 10f || viewportSize.y <= 10f)
      {
        viewportSize = new Vector2(Screen.width, Screen.height);
      }

      float targetW = dungeonBoundingSize.x * pixelsPerTile;
      float targetH = dungeonBoundingSize.y * pixelsPerTile;

      if (targetW <= 0f || targetH <= 0f) return;

      // Để chừa lề 15% màn hình để map nằm thoáng đẹp
      float fitX = (viewportSize.x * 0.85f) / targetW;
      float fitY = (viewportSize.y * 0.85f) / targetH;
      currentZoom = Mathf.Clamp(Mathf.Min(fitX, fitY), minZoom, maxZoom);

      mapContent.localScale = Vector3.one * currentZoom;

      // Đặt tâm mapContent khớp với tâm của Dungeon Bounding Box
      Vector2 centerUIPos = GridToUIPosition(dungeonBoundingCenter);
      panOffset = -centerUIPos * currentZoom;
      mapContent.anchoredPosition = panOffset;
    }

    /// <summary>
    /// Đặt độ zoom bản đồ, tự động giới hạn trong khoảng [minZoom, maxZoom]
    /// </summary>
    public void SetZoom(float targetZoom)
    {
      currentZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
      if (mapContent != null)
      {
        mapContent.localScale = Vector3.one * currentZoom;
      }
    }

    /// <summary>
    /// Căn giữa bản đồ vào vị trí Player
    /// </summary>
    public void CenterMapOnPlayer()
    {
      if (mapContent == null) return;

      FindPlayer();
      if (cachedPlayerTransform != null)
      {
        Vector3 pos = cachedPlayerTransform.position;
        float gx = pos.x / worldTileSize;
        float gy = pos.z / worldTileSize;
        Vector2 playerUIPos = GridToUIPosition(new Vector2(gx, gy));

        if (playerMarkerTransform != null)
        {
          playerMarkerTransform.anchoredPosition = playerUIPos;
        }

        panOffset = -playerUIPos * currentZoom;
        mapContent.anchoredPosition = panOffset;
        return;
      }

      if (playerMarkerTransform != null)
      {
        Vector2 playerUIPos = playerMarkerTransform.anchoredPosition;
        panOffset = -playerUIPos * currentZoom;
        mapContent.anchoredPosition = panOffset;
      }
    }

    private Vector2 GridToUIPosition(Vector2 gridPos)
    {
      float x = (gridPos.x - gridCenter.x) * pixelsPerTile;
      float y = (gridPos.y - gridCenter.y) * pixelsPerTile;
      return new Vector2(x, y);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (minZoom > maxZoom) minZoom = maxZoom;
      defaultZoomOnOpen = Mathf.Clamp(defaultZoomOnOpen, minZoom, maxZoom);

      // Cho phép chỉnh sửa màu sắc và độ dày viền trong Play Mode và cập nhật trực tiếp
      if (Application.isPlaying && isInitialized)
      {
        RefreshAllVisuals();
      }
    }
#endif
  }
}
