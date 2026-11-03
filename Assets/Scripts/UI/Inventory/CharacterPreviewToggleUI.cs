using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Điều khiển cụm nút Toggle chuyển đổi giữa xem nhân vật 3D (Model Preview) và 2D (Splash Art / Meme Sprite).
  /// Quản lý con trỏ trượt SlidingHandle, đổi màu text tab, và tối ưu hiệu năng (tắt 3D Camera khi xem 2D).
  /// </summary>
  [DisallowMultipleComponent]
  public class CharacterPreviewToggleUI : MonoBehaviour
  {
    public enum PreviewMode
    {
      Mode3D = 0,
      Mode2D = 1
    }

    [Header("=== TOGGLE CONTROLS ===")]
    [Tooltip("Con trỏ trượt di chuyển qua lại giữa 3D và 2D")]
    [SerializeField] private RectTransform slidingHandle;

    [Tooltip("Nút bấm chọn chế độ 3D")]
    [SerializeField] private Button btn3D;

    [Tooltip("Nút bấm chọn chế độ 2D")]
    [SerializeField] private Button btn2D;

    [Tooltip("Nhãn chữ Text 3D")]
    [SerializeField] private TextMeshProUGUI label3D;

    [Tooltip("Nhãn chữ Text 2D")]
    [SerializeField] private TextMeshProUGUI label2D;

    [Header("=== PREVIEW TARGETS ===")]
    [Tooltip("Box hiển thị Model 3D (RawImage + CharacterPreviewController)")]
    [SerializeField] private GameObject characterPreviewBox;

    [Tooltip("Controller quản lý Camera 3D xem trước nhân vật")]
    [SerializeField] private CharacterPreviewController characterPreviewController;

    [Tooltip("Box hiển thị ảnh 2D (Image + UISpriteSheetAnimator)")]
    [SerializeField] private GameObject character2DBox;

    [Tooltip("Animator phát ảnh sprite sheet 2D")]
    [SerializeField] private UISpriteSheetAnimator character2DAnimator;

    [Tooltip("Image hiển thị ảnh 2D")]
    [SerializeField] private Image character2DImage;

    [Header("=== SLIDING ANIMATION SETTINGS ===")]
    [Tooltip("Tọa độ X của SlidingHandle khi ở vị trí 3D (bên trái)")]
    [SerializeField] private float handlePosX_3D = -26f;

    [Tooltip("Tọa độ X của SlidingHandle khi ở vị trí 2D (bên phải)")]
    [SerializeField] private float handlePosX_2D = 28f;

    [Tooltip("Thời gian trượt của SlidingHandle (giây)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float slideDuration = 0.15f;

    [Header("=== LABEL COLORS ===")]
    [Tooltip("Màu chữ khi được kích hoạt (Vàng kim sáng đặc trưng của game)")]
    [SerializeField] private Color activeLabelColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);

    [Tooltip("Màu chữ khi không kích hoạt (Xám mờ)")]
    [SerializeField] private Color inactiveLabelColor = new Color(0.60f, 0.60f, 0.60f, 0.75f);

    [Header("=== PERSISTENCE ===")]
    [Tooltip("Ghi nhớ lựa chọn của người chơi khi mở lại túi đồ")]
    [SerializeField] private bool rememberPlayerChoice = true;

    private const string PREF_KEY = "InfiniteLabyrinth_PreviewMode";
    private PreviewMode currentMode = PreviewMode.Mode3D;
    private Coroutine activeSlideRoutine = null;

    public PreviewMode CurrentMode => currentMode;
    public bool Is2DMode => currentMode == PreviewMode.Mode2D;
    public bool Is3DMode => currentMode == PreviewMode.Mode3D;

    private void Awake()
    {
      AutoFindReferences();

      // Đăng ký sự kiện click nút
      if (btn3D != null)
      {
        btn3D.onClick.RemoveListener(OnBtn3DClicked);
        btn3D.onClick.AddListener(OnBtn3DClicked);
      }

      if (btn2D != null)
      {
        btn2D.onClick.RemoveListener(OnBtn2DClicked);
        btn2D.onClick.AddListener(OnBtn2DClicked);
      }

      // Đảm bảo Image 2D giữ đúng tỷ lệ góc (Preserve Aspect)
      if (character2DImage != null)
      {
        character2DImage.preserveAspect = true;
      }

      // Đọc lựa chọn đã lưu (hoặc mặc định 3D)
      if (rememberPlayerChoice && PlayerPrefs.HasKey(PREF_KEY))
      {
        currentMode = (PreviewMode)PlayerPrefs.GetInt(PREF_KEY, (int)PreviewMode.Mode3D);
      }
      else
      {
        currentMode = PreviewMode.Mode3D;
      }
    }

    private void Start()
    {
      // Áp dụng ngay lập tức (không chạy hiệu ứng trượt ở lần đầu khởi động)
      ApplyMode(currentMode, animate: false);
    }

    private void OnEnable()
    {
      // Mỗi khi panel Inventory được bật lên, đồng bộ lại camera và vị trí handle
      ApplyCurrentModeToPreview();
      UpdateHandlePositionImmediate();
      UpdateLabelColors();
    }

    /// <summary>
    /// Tự động tìm kiếm các GameObject và Component liên quan nếu để trống trong Inspector
    /// </summary>
    public void AutoFindReferences()
    {
      if (slidingHandle == null)
      {
        Transform handleT = transform.Find("SlidingHandle");
        if (handleT != null) slidingHandle = handleT.GetComponent<RectTransform>();
      }

      if (btn3D == null)
      {
        Transform btnT = transform.Find("Btn3D");
        if (btnT != null) btn3D = btnT.GetComponent<Button>();
      }

      if (btn2D == null)
      {
        Transform btnT = transform.Find("Btn2D");
        if (btnT != null) btn2D = btnT.GetComponent<Button>();
      }

      if (label3D == null && btn3D != null)
      {
        label3D = btn3D.GetComponentInChildren<TextMeshProUGUI>(true);
      }

      if (label2D == null && btn2D != null)
      {
        label2D = btn2D.GetComponentInChildren<TextMeshProUGUI>(true);
      }

      // Tự tìm CharacterPreviewBox và Character2DBox trong PreviewMask
      Transform current = transform.parent;
      while (current != null && (characterPreviewBox == null || character2DBox == null))
      {
        Transform previewMask = current.Find("ModelPreview/PreviewMask");
        if (previewMask == null) previewMask = current.Find("PreviewMask");

        if (previewMask != null)
        {
          if (characterPreviewBox == null)
          {
            Transform box3D = previewMask.Find("CharacterPreviewBox");
            if (box3D != null) characterPreviewBox = box3D.gameObject;
          }

          if (character2DBox == null)
          {
            Transform box2D = previewMask.Find("Character2DBox");
            if (box2D != null) character2DBox = box2D.gameObject;
          }
        }

        current = current.parent;
      }

      // Nếu vẫn chưa tìm thấy thì tìm theo root
      if (characterPreviewBox == null || character2DBox == null)
      {
        Transform root = transform.root;
        if (characterPreviewBox == null)
        {
          CharacterPreviewController ctrl = root.GetComponentInChildren<CharacterPreviewController>(true);
          if (ctrl != null) characterPreviewBox = ctrl.gameObject;
        }

        if (character2DBox == null)
        {
          foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
          {
            if (t.name == "Character2DBox")
            {
              character2DBox = t.gameObject;
              break;
            }
          }
        }
      }

      if (characterPreviewController == null && characterPreviewBox != null)
      {
        characterPreviewController = characterPreviewBox.GetComponent<CharacterPreviewController>();
      }

      if (character2DBox != null)
      {
        if (character2DAnimator == null)
        {
          character2DAnimator = character2DBox.GetComponent<UISpriteSheetAnimator>();
        }
        if (character2DImage == null)
        {
          character2DImage = character2DBox.GetComponent<Image>();
        }
      }

      // Cập nhật vị trí X từ vị trí thực tế của nút nếu có
      if (btn3D != null)
      {
        RectTransform rt3D = btn3D.GetComponent<RectTransform>();
        if (rt3D != null) handlePosX_3D = rt3D.anchoredPosition.x;
      }

      if (btn2D != null)
      {
        RectTransform rt2D = btn2D.GetComponent<RectTransform>();
        if (rt2D != null) handlePosX_2D = rt2D.anchoredPosition.x;
      }
    }

    private void OnBtn3DClicked()
    {
      SetMode(PreviewMode.Mode3D, animate: true);
    }

    private void OnBtn2DClicked()
    {
      SetMode(PreviewMode.Mode2D, animate: true);
    }

    public void SwitchTo3D()
    {
      SetMode(PreviewMode.Mode3D, animate: true);
    }

    public void SwitchTo2D()
    {
      SetMode(PreviewMode.Mode2D, animate: true);
    }

    public void ToggleMode()
    {
      SetMode(currentMode == PreviewMode.Mode3D ? PreviewMode.Mode2D : PreviewMode.Mode3D, animate: true);
    }

    public void SetMode(PreviewMode newMode, bool animate = true)
    {
      currentMode = newMode;

      if (rememberPlayerChoice)
      {
        PlayerPrefs.SetInt(PREF_KEY, (int)currentMode);
        PlayerPrefs.Save();
      }

      ApplyMode(currentMode, animate);
    }

    private void ApplyMode(PreviewMode mode, bool animate)
    {
      float targetX = (mode == PreviewMode.Mode3D) ? handlePosX_3D : handlePosX_2D;

      if (animate && gameObject.activeInHierarchy)
      {
        if (activeSlideRoutine != null) StopCoroutine(activeSlideRoutine);
        activeSlideRoutine = StartCoroutine(AnimateSlideHandle(targetX));
      }
      else
      {
        if (slidingHandle != null)
        {
          Vector2 pos = slidingHandle.anchoredPosition;
          pos.x = targetX;
          slidingHandle.anchoredPosition = pos;
        }
      }

      UpdateLabelColors();
      ApplyCurrentModeToPreview();
    }

    /// <summary>
    /// Đồng bộ trạng thái hiển thị giữa buồng xem 3D và 2D
    /// </summary>
    public void ApplyCurrentModeToPreview()
    {
      bool is3D = (currentMode == PreviewMode.Mode3D);

      // 1. Quản lý buồng xem 3D
      if (characterPreviewBox != null)
      {
        characterPreviewBox.SetActive(is3D);
      }

      if (characterPreviewController != null)
      {
        // Khi ở chế độ 2D: TẮT hoàn toàn Camera 3D để giải phóng 100% chi phí Render của GPU!
        // Khi ở chế độ 3D: Chỉ bật khi Inventory đang mở
        bool shouldCameraBeActive = is3D && InventoryUIController.IsOpen;
        characterPreviewController.EnablePreview(shouldCameraBeActive);
      }

      // 2. Quản lý buồng xem 2D
      if (character2DBox != null)
      {
        character2DBox.SetActive(!is3D);

        if (!is3D && character2DAnimator != null)
        {
          character2DAnimator.Play();
        }
      }
    }

    private void UpdateLabelColors()
    {
      bool is3D = (currentMode == PreviewMode.Mode3D);

      if (label3D != null)
      {
        label3D.color = is3D ? activeLabelColor : inactiveLabelColor;
      }

      if (label2D != null)
      {
        label2D.color = !is3D ? activeLabelColor : inactiveLabelColor;
      }
    }

    private void UpdateHandlePositionImmediate()
    {
      if (slidingHandle == null) return;
      float targetX = (currentMode == PreviewMode.Mode3D) ? handlePosX_3D : handlePosX_2D;
      Vector2 pos = slidingHandle.anchoredPosition;
      pos.x = targetX;
      slidingHandle.anchoredPosition = pos;
    }

    private IEnumerator AnimateSlideHandle(float targetX)
    {
      if (slidingHandle == null) yield break;

      float startX = slidingHandle.anchoredPosition.x;
      float elapsed = 0f;
      float duration = Mathf.Max(0.01f, slideDuration);
      Vector2 pos = slidingHandle.anchoredPosition;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        pos.x = Mathf.Lerp(startX, targetX, smoothT);
        slidingHandle.anchoredPosition = pos;
        yield return null;
      }

      pos.x = targetX;
      slidingHandle.anchoredPosition = pos;
      activeSlideRoutine = null;
    }
  }
}
