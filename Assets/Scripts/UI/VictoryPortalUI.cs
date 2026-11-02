using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Giao diện Lựa chọn Cổng Dịch Chuyển 2 Cửa Sổ (Task D02-5D):
  /// - Xuất hiện khi tương tác với Cổng Dịch Chuyển Thắng Trận (Victory Portal trên bục 12m).
  /// - Cửa sổ Bên Trái: "RETURN TO HAVEN" - Giữ 2.0s để chuyển cảnh về Haven/LandScene.
  /// - Cửa sổ Bên Phải: "GO TO FLOOR 2" - Giữ 2.0s để tiến vào hầm ngục tầng kế tiếp.
  /// - Nút "Stay" & phím [ESC]: Đóng giao diện, khôi phục chuột và quyền điều khiển để gom nhặt loot.
  /// - Mask Stencil vát góc (Beveled Corners) chuẩn xác ôm khít Panel, khử 100% nháy trắng.
  /// </summary>
  public class VictoryPortalUI : MonoBehaviour
  {
    public static VictoryPortalUI Instance
    {
      get
      {
        if (instance == null)
        {
          instance = FindFirstObjectByType<VictoryPortalUI>(FindObjectsInactive.Include);
        }
        return instance;
      }
      private set => instance = value;
    }
    private static VictoryPortalUI instance;

    public static bool IsOpen => Instance != null && Instance.isOpen;

    public enum PortalChoice
    {
      ReturnToTown,
      NextFloor
    }

    [Header("=== THAM CHIẾU GIAO DIỆN (CANVAS GROUP) ===")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== CỬA SỔ BÊN TRÁI: VỀ LÀNG (HAVEN VILLAGE) ===")]
    [SerializeField] private RectTransform townPanel;
    [SerializeField] private Button btnReturnToTown;
    [SerializeField] private Image townHoldFill;
    [SerializeField] private TextMeshProUGUI townTitle;
    [SerializeField] private TextMeshProUGUI townButtonText;
    [SerializeField] private string townSceneName = "LandScene";

    [Header("=== CỬA SỔ BÊN PHẢI: TẦNG KẾ TIẾP (FLOOR 2) ===")]
    [SerializeField] private RectTransform nextFloorPanel;
    [SerializeField] private Button btnNextFloor;
    [SerializeField] private Image dungeonHoldFill;
    [SerializeField] private TextMeshProUGUI dungeonTitle;
    [SerializeField] private TextMeshProUGUI dungeonButtonText;
    [SerializeField] private string nextFloorSceneName = "DungeonScene";
    [SerializeField] private int nextFloorNumber = 2;

    [Header("=== NÚT Ở LẠI (STAY) ===")]
    [SerializeField] private Button btnStay;
    [SerializeField] private TextMeshProUGUI stayButtonText;

    [Header("=== CẤU HÌNH ĐÈ GIỮ (HOLD TO CONFIRM) ===")]
    [Tooltip("Thời gian đè giữ cần thiết để xác nhận chuyển cảnh (giây)")]
    [SerializeField] private float holdDuration = 2.0f;

    [Tooltip("Tốc độ thanh tụt lùi khi buông tay trước khi đầy")]
    [SerializeField] private float decayMultiplier = 2.5f;

    [Tooltip("Thời gian mờ dần hiện/ẩn giao diện (Fade In/Out)")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("=== HIỆU ỨNG ÂM THANH ===")]
    [SerializeField] private AudioClip holdTickSound;
    [SerializeField] private AudioClip confirmSound;
    [SerializeField] private AudioClip cancelSound;
    [SerializeField] private AudioSource audioSource;

    // Tiến trình đè giữ hiện tại (0.0 -> 1.0)
    private float townHoldProgress = 0f;
    private float dungeonHoldProgress = 0f;

    // Trạng thái giữ chuột qua EventSystem
    private bool isPointerHoldingTown = false;
    private bool isPointerHoldingDungeon = false;

    // Trạng thái hệ thống
    private bool isOpen = false;
    private bool isExecutingChoice = false;
    private Coroutine fadeRoutine = null;

    // Cache điều khiển người chơi
    private PlayerController cachedPlayerController;
    private Vector3 townPanelInitialScale = Vector3.one;
    private Vector3 nextFloorPanelInitialScale = Vector3.one;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      EnsureReferences();

      // Mặc định ẩn giao diện khi mới vào game
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
      }

      if (townPanel != null) townPanelInitialScale = townPanel.localScale;
      if (nextFloorPanel != null) nextFloorPanelInitialScale = nextFloorPanel.localScale;

      ResetFills();
    }

    private void Start()
    {
      SetupHoldTriggers();
    }

    /// <summary>
    /// Tự động dò tìm các GameObject con và cấu hình toàn bộ tương tác.
    /// </summary>
    public void EnsureReferences()
    {
      if (canvasGroup == null)
      {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
      }

      if (audioSource == null)
      {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
          audioSource = gameObject.AddComponent<AudioSource>();
          audioSource.playOnAwake = false;
        }
      }

      // Cấu hình Mask vát góc chuẩn xác & loại bỏ triệt để nháy trắng
      EnsureImageDisplayAndMasking();

      // 1. Tìm TownPanel và các thành phần
      if (townPanel == null)
      {
        Transform t = transform.Find("TownPanel");
        if (t != null) townPanel = t.GetComponent<RectTransform>();
      }

      if (townPanel != null)
      {
        if (btnReturnToTown == null)
        {
          Transform b = townPanel.Find("BtnReturnToTown");
          if (b != null) btnReturnToTown = b.GetComponent<Button>();
        }

        if (townTitle == null)
        {
          Transform tt = townPanel.Find("TownTittle");
          if (tt != null) townTitle = tt.GetComponent<TextMeshProUGUI>();
        }
      }

      if (btnReturnToTown != null)
      {
        // Đảm bảo BtnReturnToTown có Image trong suốt để nhận Raycast toàn bộ diện tích 431x527 px
        Image btnImg = btnReturnToTown.GetComponent<Image>();
        if (btnImg == null) btnImg = btnReturnToTown.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0f);
        btnImg.raycastTarget = true;
        btnReturnToTown.targetGraphic = btnImg;
        btnReturnToTown.transition = Selectable.Transition.None;

        if (townHoldFill == null)
        {
          Transform f = btnReturnToTown.transform.Find("HoldFill");
          if (f != null) townHoldFill = f.GetComponent<Image>();
        }

        if (townButtonText == null)
        {
          Transform txt = btnReturnToTown.transform.Find("Text");
          if (txt != null) townButtonText = txt.GetComponent<TextMeshProUGUI>();
        }
      }

      // 2. Tìm NextFloorPanel và các thành phần
      if (nextFloorPanel == null)
      {
        Transform nf = transform.Find("NextFloorPanel");
        if (nf != null) nextFloorPanel = nf.GetComponent<RectTransform>();
      }

      if (nextFloorPanel != null)
      {
        if (btnNextFloor == null)
        {
          Transform b = nextFloorPanel.Find("BtnNextFloor");
          if (b != null) btnNextFloor = b.GetComponent<Button>();
        }

        if (dungeonTitle == null)
        {
          Transform dt = nextFloorPanel.Find("DungeonTittle");
          if (dt != null) dungeonTitle = dt.GetComponent<TextMeshProUGUI>();
        }
      }

      if (btnNextFloor != null)
      {
        // Đảm bảo BtnNextFloor có Image trong suốt để nhận Raycast toàn bộ diện tích 430x530 px
        Image btnImg = btnNextFloor.GetComponent<Image>();
        if (btnImg == null) btnImg = btnNextFloor.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0f);
        btnImg.raycastTarget = true;
        btnNextFloor.targetGraphic = btnImg;
        btnNextFloor.transition = Selectable.Transition.None;

        if (dungeonHoldFill == null)
        {
          Transform f = btnNextFloor.transform.Find("HoldFill");
          if (f == null) f = btnNextFloor.transform.Find("HoidFill");
          if (f != null) dungeonHoldFill = f.GetComponent<Image>();
        }

        if (dungeonButtonText == null)
        {
          Transform txt = btnNextFloor.transform.Find("Text");
          if (txt != null) dungeonButtonText = txt.GetComponent<TextMeshProUGUI>();
        }
      }

      // 3. Tìm BtnStay
      if (btnStay == null)
      {
        Transform s = transform.Find("BtnStay");
        if (s != null) btnStay = s.GetComponent<Button>();
      }

      if (btnStay != null)
      {
        btnStay.onClick.RemoveListener(Close);
        btnStay.onClick.AddListener(Close);

        if (stayButtonText == null)
        {
          Transform txt = btnStay.transform.Find("Text");
          if (txt != null) stayButtonText = txt.GetComponent<TextMeshProUGUI>();
        }
      }
    }

    /// <summary>
    /// Kích hoạt Stencil Mask ôm khít Panel và vát góc (bevel corners) cho ảnh,
    /// đồng thời cấu hình showMaskGraphic = false để Unity dùng ColorMask = 0 không bao giờ nháy trắng.
    /// </summary>
    public void EnsureImageDisplayAndMasking()
    {
      // 1. Cấu hình Mask Stencil cho TownPanel & NextFloorPanel để vát góc và ôm khít khung
      foreach (var mask in GetComponentsInChildren<Mask>(true))
      {
        GameObject maskObj = mask.gameObject;

        // Gỡ bỏ RectMask2D nếu có để tránh xung đột component với Mask
        RectMask2D rectMask = maskObj.GetComponent<RectMask2D>();
        if (rectMask != null)
        {
          if (Application.isPlaying) Destroy(rectMask);
          else DestroyImmediate(rectMask);
        }

        // Bật Mask và tắt hiển thị graphic của Mask (showMaskGraphic = false)
        // để Unity chỉ ghi Stencil vát góc mà không vẽ bất kỳ pixel màu trắng nào ra Color Buffer
        mask.enabled = true;
        mask.showMaskGraphic = false;

        // Image của Mask dùng Button_Base.png để cắt góc vát (alpha = 1 để stencil ghi nhận đầy đủ)
        Image maskImg = maskObj.GetComponent<Image>();
        if (maskImg != null)
        {
          maskImg.enabled = true;
          maskImg.color = new Color(1f, 1f, 1f, 1f);
          maskImg.raycastTarget = false; // Chuột xuyên qua để chạm vào button/panel
        }
      }

      // 2. Khôi phục hiển thị rõ nét cho TownImg
      Transform townImgTrans = transform.Find("TownPanel/Mask/TownImg");
      if (townImgTrans != null)
      {
        townImgTrans.gameObject.SetActive(true);
        Image img = townImgTrans.GetComponent<Image>();
        if (img != null)
        {
          img.enabled = true;
          img.color = new Color(1f, 1f, 1f, 1f);
          img.raycastTarget = false;
        }
      }

      // 3. Khôi phục hiển thị rõ nét cho DungeonImg
      Transform dungeonImgTrans = transform.Find("NextFloorPanel/Mask/DungeonImg");
      if (dungeonImgTrans != null)
      {
        dungeonImgTrans.gameObject.SetActive(true);
        Image img = dungeonImgTrans.GetComponent<Image>();
        if (img != null)
        {
          img.enabled = true;
          img.color = new Color(1f, 1f, 1f, 1f);
          img.raycastTarget = false;
        }
      }
    }

    /// <summary>
    /// Gắn bộ lắng nghe sự kiện PointerDown/PointerUp vào các nút Hold-to-Confirm.
    /// </summary>
    public void SetupHoldTriggers()
    {
      // 1. Phía Làng (Town)
      if (btnReturnToTown != null)
      {
        AttachTrigger(btnReturnToTown.gameObject, () => isPointerHoldingTown = true, () => isPointerHoldingTown = false);
      }
      if (townPanel != null)
      {
        AttachTrigger(townPanel.gameObject, () => isPointerHoldingTown = true, () => isPointerHoldingTown = false);
      }

      // 2. Phía Hầm Ngục (Dungeon)
      if (btnNextFloor != null)
      {
        AttachTrigger(btnNextFloor.gameObject, () => isPointerHoldingDungeon = true, () => isPointerHoldingDungeon = false);
      }
      if (nextFloorPanel != null)
      {
        AttachTrigger(nextFloorPanel.gameObject, () => isPointerHoldingDungeon = true, () => isPointerHoldingDungeon = false);
      }
    }

    private void AttachTrigger(GameObject targetObj, Action onDown, Action onUp)
    {
      if (targetObj == null) return;
      VictoryPortalHoldTrigger trigger = targetObj.GetComponent<VictoryPortalHoldTrigger>();
      if (trigger == null) trigger = targetObj.AddComponent<VictoryPortalHoldTrigger>();
      trigger.onPointerDown = onDown;
      trigger.onPointerUp = onUp;
    }

    private void Update()
    {
      if (!isOpen || isExecutingChoice) return;

      // 1. Lắng nghe phím tắt ESC để đóng bảng (Stay)
      HandleCancelInput();

      // 2. Lắng nghe phím số / phím tắt đè giữ (Key 1 / Q = Làng, Key 2 / E = Tầng 2)
      bool isKeyHoldingTown = CheckKeyHoldingTown();
      bool isKeyHoldingDungeon = CheckKeyHoldingDungeon();

      // 3. Chuột đè giữ trực tiếp trên vùng RectTransform của Panel (Bảo đảm 100% không thể trượt)
      bool isMouseInTown = false;
      bool isMouseInDungeon = false;

#if ENABLE_INPUT_SYSTEM
      bool isMouseLeftPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
      Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
      bool isMouseLeftPressed = Input.GetMouseButton(0);
      Vector2 mousePos = Input.mousePosition;
#else
      bool isMouseLeftPressed = false;
      Vector2 mousePos = Vector2.zero;
#endif

      if (isMouseLeftPressed)
      {
        Camera uiCam = null;
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) uiCam = c.worldCamera;

        if ((townPanel != null && RectTransformUtility.RectangleContainsScreenPoint(townPanel, mousePos, uiCam)) ||
            (btnReturnToTown != null && RectTransformUtility.RectangleContainsScreenPoint(btnReturnToTown.GetComponent<RectTransform>(), mousePos, uiCam)))
        {
          isMouseInTown = true;
        }
        else if ((nextFloorPanel != null && RectTransformUtility.RectangleContainsScreenPoint(nextFloorPanel, mousePos, uiCam)) ||
                 (btnNextFloor != null && RectTransformUtility.RectangleContainsScreenPoint(btnNextFloor.GetComponent<RectTransform>(), mousePos, uiCam)))
        {
          isMouseInDungeon = true;
        }
      }
      else
      {
        // Khi chuột nhả ra thì lập tức reset pointer hold
        isPointerHoldingTown = false;
        isPointerHoldingDungeon = false;
      }

      // 4. Xử lý tiến trình đè giữ Về Làng (Town)
      bool holdingTown = (isMouseLeftPressed && isMouseInTown) || isPointerHoldingTown || isKeyHoldingTown;
      if (holdingTown)
      {
        townHoldProgress += Time.unscaledDeltaTime / Mathf.Max(0.1f, holdDuration);
        if (townHoldProgress >= 1f)
        {
          townHoldProgress = 1f;
          UpdateVisuals();
          ExecuteChoice(PortalChoice.ReturnToTown);
          return;
        }
      }
      else
      {
        if (townHoldProgress > 0f)
        {
          townHoldProgress = Mathf.Max(0f, townHoldProgress - (Time.unscaledDeltaTime * decayMultiplier / Mathf.Max(0.1f, holdDuration)));
        }
      }

      // 5. Xử lý tiến trình đè giữ Tầng Kế Tiếp (Dungeon)
      bool holdingDungeon = (isMouseLeftPressed && isMouseInDungeon) || isPointerHoldingDungeon || isKeyHoldingDungeon;
      if (holdingDungeon)
      {
        dungeonHoldProgress += Time.unscaledDeltaTime / Mathf.Max(0.1f, holdDuration);
        if (dungeonHoldProgress >= 1f)
        {
          dungeonHoldProgress = 1f;
          UpdateVisuals();
          ExecuteChoice(PortalChoice.NextFloor);
          return;
        }
      }
      else
      {
        if (dungeonHoldProgress > 0f)
        {
          dungeonHoldProgress = Mathf.Max(0f, dungeonHoldProgress - (Time.unscaledDeltaTime * decayMultiplier / Mathf.Max(0.1f, holdDuration)));
        }
      }

      // 6. Cập nhật giao diện thanh fill & độ nhún (juice scale)
      UpdateVisuals();
    }

    private bool CheckKeyHoldingTown()
    {
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null)
      {
        var kb = Keyboard.current;
        return kb.digit1Key.isPressed || kb.numpad1Key.isPressed || kb.qKey.isPressed;
      }
      return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
      return Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1) || Input.GetKey(KeyCode.Q);
#else
      return false;
#endif
    }

    private bool CheckKeyHoldingDungeon()
    {
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null)
      {
        var kb = Keyboard.current;
        return kb.digit2Key.isPressed || kb.numpad2Key.isPressed || kb.eKey.isPressed;
      }
      return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
      return Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2) || Input.GetKey(KeyCode.E);
#else
      return false;
#endif
    }

    private void HandleCancelInput()
    {
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null)
      {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
          Close();
        }
      }
#elif ENABLE_LEGACY_INPUT_MANAGER
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        Close();
      }
#endif
    }

    private void UpdateVisuals()
    {
      if (townHoldFill != null)
      {
        townHoldFill.fillAmount = townHoldProgress;
      }

      if (dungeonHoldFill != null)
      {
        dungeonHoldFill.fillAmount = dungeonHoldProgress;
      }

      // Hiệu ứng phồng nhẹ khi đang đè giữ (Juicy Pulse Feedback)
      if (townPanel != null)
      {
        float targetTownScale = Mathf.Lerp(1.0f, 1.035f, townHoldProgress);
        townPanel.localScale = townPanelInitialScale * targetTownScale;
      }

      if (nextFloorPanel != null)
      {
        float targetDungeonScale = Mathf.Lerp(1.0f, 1.035f, dungeonHoldProgress);
        nextFloorPanel.localScale = nextFloorPanelInitialScale * targetDungeonScale;
      }
    }

    private void ResetFills()
    {
      townHoldProgress = 0f;
      dungeonHoldProgress = 0f;
      isPointerHoldingTown = false;
      isPointerHoldingDungeon = false;

      if (townHoldFill != null) townHoldFill.fillAmount = 0f;
      if (dungeonHoldFill != null) dungeonHoldFill.fillAmount = 0f;

      if (townPanel != null) townPanel.localScale = townPanelInitialScale;
      if (nextFloorPanel != null) nextFloorPanel.localScale = nextFloorPanelInitialScale;
    }

    /// <summary>
    /// Mở giao diện Lựa chọn Cổng Dịch Chuyển (Được gọi từ PortalController khi tương tác).
    /// </summary>
    public void Open()
    {
      if (isOpen) return;

      EnsureReferences();
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
      }

      gameObject.SetActive(true);
      SetupHoldTriggers();
      ResetFills();

      isOpen = true;
      isExecutingChoice = false;

      // 1. Mở khóa con trỏ chuột
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      // 2. Khóa di chuyển người chơi
      if (cachedPlayerController == null)
      {
        cachedPlayerController = FindFirstObjectByType<PlayerController>();
      }
      if (cachedPlayerController != null)
      {
        cachedPlayerController.LockMovement();
      }

      // 3. Hiện mượt mà qua CanvasGroup Fade Routine
      if (fadeRoutine != null) StopCoroutine(fadeRoutine);
      fadeRoutine = StartCoroutine(FadeRoutine(1f, true));

      Debug.Log("<color=gold>[VictoryPortalUI]</color> 🌀 Đã mở giao diện Lựa Chọn Cổng Thắng Trận (D02-5D).");
    }

    /// <summary>
    /// Đóng giao diện và ở lại hầm ngục (Được gọi khi bấm BtnStay hoặc [ESC]).
    /// </summary>
    public void Close()
    {
      if (!isOpen || isExecutingChoice) return;

      isOpen = false;
      ResetFills();

      if (cancelSound != null && audioSource != null)
      {
        audioSource.PlayOneShot(cancelSound);
      }

      // 1. Khóa lại con trỏ chuột
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      // 2. Mở khóa di chuyển người chơi
      if (cachedPlayerController != null)
      {
        cachedPlayerController.UnlockMovement();
      }

      // 3. Mờ dần biến mất
      if (fadeRoutine != null) StopCoroutine(fadeRoutine);
      fadeRoutine = StartCoroutine(FadeRoutine(0f, false));

      Debug.Log("<color=gold>[VictoryPortalUI]</color> 🚪 Người chơi chọn 'STAY' - Đã đóng giao diện để tiếp tục gom loot.");
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool enableInteraction)
    {
      if (canvasGroup == null) yield break;

      if (targetAlpha > 0f)
      {
        // Khi mở: Đảm bảo alpha = 0 hoàn toàn trong 1 frame đầu
        // để Unity CanvasRenderer và Stencil Mask hoàn tất nạp vật liệu ColorMask 0
        // Loại bỏ 100% hiện tượng nháy trắng 1 frame!
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        yield return null;
      }

      canvasGroup.interactable = enableInteraction;
      canvasGroup.blocksRaycasts = enableInteraction;

      float startAlpha = canvasGroup.alpha;
      float timer = 0f;

      while (timer < fadeDuration)
      {
        timer += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
        yield return null;
      }

      canvasGroup.alpha = targetAlpha;
      canvasGroup.interactable = enableInteraction;
      canvasGroup.blocksRaycasts = enableInteraction;
    }

    /// <summary>
    /// Kích hoạt lựa chọn khi thanh HoldFill đầy 100%.
    /// </summary>
    private void ExecuteChoice(PortalChoice choice)
    {
      if (isExecutingChoice) return;
      isExecutingChoice = true;

      // Khóa tương tác tránh spam đè giữ kép
      if (canvasGroup != null)
      {
        canvasGroup.interactable = false;
      }

      if (confirmSound != null && audioSource != null)
      {
        audioSource.PlayOneShot(confirmSound);
      }

      switch (choice)
      {
        case PortalChoice.ReturnToTown:
          Debug.Log($"<color=lime>[VictoryPortalUI]</color> 🏡 Xác nhận TRỞ VỀ LÀNG! Đang chuyển cảnh tới '{townSceneName}'...");
          if (LoadingScreenManager.Instance != null)
          {
            LoadingScreenManager.Instance.LoadScene(townSceneName, 1);
          }
          else
          {
            SceneManager.LoadScene(townSceneName);
          }
          break;

        case PortalChoice.NextFloor:
          Debug.Log($"<color=cyan>[VictoryPortalUI]</color> ⚔️ Xác nhận TIẾN VÀO TẦNG {nextFloorNumber}! Đang nạp '{nextFloorSceneName}'...");
          if (LoadingScreenManager.Instance != null)
          {
            LoadingScreenManager.Instance.LoadScene(nextFloorSceneName, nextFloorNumber);
          }
          else
          {
            SceneManager.LoadScene(nextFloorSceneName);
          }
          break;
      }
    }
  }

  /// <summary>
  /// Lắng nghe các sự kiện chuột PointerDown, PointerUp cho nút Hold-to-Confirm.
  /// </summary>
  public class VictoryPortalHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
  {
    public Action onPointerDown;
    public Action onPointerUp;

    public void OnPointerDown(PointerEventData eventData)
    {
      if (eventData.button == PointerEventData.InputButton.Left)
      {
        onPointerDown?.Invoke();
      }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
      if (eventData.button == PointerEventData.InputButton.Left)
      {
        onPointerUp?.Invoke();
      }
    }
  }
}
