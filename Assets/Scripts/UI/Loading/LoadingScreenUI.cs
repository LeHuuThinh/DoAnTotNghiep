using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// View controller for the Loading Screen UI.
  /// Handles artwork display, tips, progress interpolation, spinner rotation, and smooth fade transitions.
  /// Fully localized in English.
  /// </summary>
  [RequireComponent(typeof(CanvasGroup))]
  public class LoadingScreenUI : MonoBehaviour
  {
    [Header("=== CANVAS & FADE TRANSITION ===")]
    [Tooltip("CanvasGroup controlling overall loading screen opacity")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Fade in duration in seconds")]
    [SerializeField] private float fadeInDuration = 0.35f;

    [Tooltip("Fade out duration in seconds")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("=== BACKGROUND ARTWORK ===")]
    [Tooltip("Full-screen RawImage for dungeon/village artwork")]
    [SerializeField] private RawImage backgroundImage;

    [Tooltip("Dark gradient/vignette overlay ensuring text readability")]
    [SerializeField] private Image vignetteOverlay;

    [Header("=== LOCATION & FLOOR HEADERS ===")]
    [Tooltip("Top header text (e.g., 'INFINITE LABYRINTH - FLOOR 1')")]
    [SerializeField] private TextMeshProUGUI locationTitleText;

    [Tooltip("Subtitle text (e.g., 'The Forgotten Crypt')")]
    [SerializeField] private TextMeshProUGUI locationSubtitleText;

    [Header("=== GAMEPLAY TIP UI ===")]
    [Tooltip("Tip category banner (e.g., '[ COMBAT TIP ]')")]
    [SerializeField] private TextMeshProUGUI tipCategoryText;

    [Tooltip("The tip explanation body text")]
    [SerializeField] private TextMeshProUGUI tipContentText;

    [Header("=== PROGRESS BAR & PERCENTAGE ===")]
    [Tooltip("Foreground fill image for the loading progress")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Optional glow particle or sprite at the leading edge of the progress bar")]
    [SerializeField] private Image progressGlowImage;

    [Tooltip("Percentage text display at bottom-right (e.g., '75%')")]
    [SerializeField] private TextMeshProUGUI percentText;

    [Tooltip("Speed at which progress bar visually catches up to target progress (per second)")]
    [SerializeField] private float progressLerpSpeed = 3.5f;

    [Header("=== ANIMATED SPINNER / SPRITE SHEET ===")]
    [Tooltip("Transform for the spinner icon")]
    [SerializeField] private RectTransform spinnerTransform;

    [Tooltip("Image component displaying the spinner icon or animation frames")]
    [SerializeField] private Image spinnerImage;

    [Tooltip("Bật xoay 360 độ liên tục (Nếu dùng chuỗi ảnh Sprite Sheet thì TẮT tính năng này)")]
    [SerializeField] private bool enableSpinnerRotation = false;

    [Tooltip("Tốc độ xoay (độ/giây, chỉ áp dụng khi enableSpinnerRotation = true)")]
    [SerializeField] private float spinnerRotateSpeed = -200f;

    [Header("=== SPRITE SHEET ANIMATION ===")]
    [Tooltip("Bật hoạt ảnh Sprite Sheet (chạy chuỗi frame liên tục)")]
    [SerializeField] private bool enableSpriteAnimation = true;

    [Tooltip("Tốc độ khung hình (FPS) cho hoạt ảnh Sprite (mặc định 24 FPS)")]
    [SerializeField] private float animationFps = 24f;

    [Tooltip("Danh sách các frame Sprite (cắt từ Sprite Sheet)")]
    [SerializeField] private Sprite[] animationFrames;

    [Header("=== EDITOR PREVIEW (INSPECTOR CONTROLS) ===")]
    [Tooltip("Enable to inspect and tweak the Loading Screen layout directly in Scene & Game view without entering Play mode!")]
    [SerializeField] private bool editorPreviewMode = false;

    [Range(0f, 1f)]
    [Tooltip("Test progress bar and percentage display in real-time in the Editor")]
    [SerializeField] private float previewProgress = 0.65f;

    // Runtime state
    private float targetProgress = 0f;
    private float displayedProgress = 0f;
    private Coroutine currentFadeRoutine;
    private int currentFrameIndex = 0;
    private float animationTimer = 0f;

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;
    public float DisplayedProgress => displayedProgress;
    public bool EditorPreviewMode => editorPreviewMode;
    public float PreviewProgress => previewProgress;
    public int AnimationFrameCount => animationFrames != null ? animationFrames.Length : 0;
    public float AnimationFps => animationFps;
    public bool EnableSpriteAnimation => enableSpriteAnimation;
    public bool EnableSpinnerRotation => enableSpinnerRotation;

    private void Awake()
    {
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
      if (spinnerImage == null && spinnerTransform != null) spinnerImage = spinnerTransform.GetComponent<Image>();
      if (!enableSpinnerRotation && spinnerTransform != null) spinnerTransform.localRotation = Quaternion.identity;
      
      editorPreviewMode = false;
      // Default to invisible and non-blocking in Play mode
      SetAlpha(0f);
      SetInteractable(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (Application.isPlaying) return;

      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

      if (editorPreviewMode)
      {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        ApplyProgressVisuals(previewProgress);
      }
      else
      {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
      }
    }

    [ContextMenu("👁️ Show Preview (Alpha = 1)")]
    public void ShowPreviewInEditor()
    {
      editorPreviewMode = true;
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup != null) canvasGroup.alpha = 1f;
      ApplyProgressVisuals(previewProgress);
      PopulateSamplePreviewContent();
    }

    [ContextMenu("🙈 Hide Preview (Alpha = 0)")]
    public void HidePreviewInEditor()
    {
      editorPreviewMode = false;
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    [ContextMenu("🎲 Randomize Preview Content")]
    public void RandomizePreviewInEditor()
    {
      editorPreviewMode = true;
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup != null) canvasGroup.alpha = 1f;
      PopulateSamplePreviewContent(true);
    }

    public void PopulateSamplePreviewContent(bool forceRandom = false)
    {
      LoadingScreenData data = Resources.Load<LoadingScreenData>("LoadingScreenData");
      if (data != null)
      {
        data.GetLocationHeader("DungeonScene", 1, out string title, out string subtitle);
        if (locationTitleText != null) locationTitleText.text = title;
        if (locationSubtitleText != null) locationSubtitleText.text = subtitle;

        LoadingTip tip = data.GetRandomTip();
        if (tip != null)
        {
          if (tipCategoryText != null) tipCategoryText.text = $"[ {tip.category.ToUpper()} ]";
          if (tipContentText != null) tipContentText.text = tip.content;
        }

        Texture bgTex = data.GetRandomBackground("DungeonScene");
        if (backgroundImage != null && bgTex != null)
        {
          backgroundImage.texture = bgTex;
          backgroundImage.color = Color.white;
        }

        if (enableSpriteAnimation && animationFrames != null && animationFrames.Length > 0)
        {
          if (spinnerImage == null && spinnerTransform != null) spinnerImage = spinnerTransform.GetComponent<Image>();
          if (spinnerImage != null && animationFrames[0] != null)
          {
            spinnerImage.sprite = animationFrames[0];
            spinnerImage.color = Color.white;
          }
        }
      }

      ApplyProgressVisuals(previewProgress);
      if (vignetteOverlay != null) vignetteOverlay.enabled = true;
    }

    public void SetPreviewProgressDirect(float val)
    {
      previewProgress = Mathf.Clamp01(val);
      ApplyProgressVisuals(previewProgress);
    }

    /// <summary>
    /// Nạp tự động toàn bộ Sprite cắt từ Sprite Sheet theo thứ tự số tự nhiên (1..N).
    /// </summary>
    public void LoadFramesFromSpriteSheet(string assetPath)
    {
      UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
      System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
      foreach (var obj in assets)
      {
        if (obj is Sprite s)
        {
          sprites.Add(s);
        }
      }

      if (sprites.Count == 0)
      {
        Debug.LogWarning($"[LoadingScreenUI] Không tìm thấy Sprite nào được cắt tại: {assetPath}");
        return;
      }

      // Sắp xếp tự nhiên theo số thứ tự cuối tên (vd: MemeSpriteSheet_0, MemeSpriteSheet_1, ... MemeSpriteSheet_191)
      sprites.Sort((a, b) => ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name)));

      animationFrames = sprites.ToArray();
      enableSpriteAnimation = true;
      enableSpinnerRotation = false;

      if (spinnerTransform != null)
      {
        spinnerTransform.localRotation = Quaternion.identity;
        if (spinnerImage == null) spinnerImage = spinnerTransform.GetComponent<Image>();
      }

      if (spinnerImage != null)
      {
        spinnerImage.sprite = animationFrames[0];
        spinnerImage.color = Color.white;
      }

      Debug.Log($"<color=green>[LoadingScreenUI]</color> 🎬 Đã nạp thành công {animationFrames.Length} frames từ '{assetPath}'!");
    }

    private static int ExtractTrailingNumber(string name)
    {
      int lastUnderscore = name.LastIndexOf('_');
      if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int num))
      {
        return num;
      }
      return 0;
    }
#endif

    private void Update()
    {
      // 1. Continuous spinner rotation (chỉ chạy khi được bật)
      if (enableSpinnerRotation && spinnerTransform != null && IsVisible)
      {
        spinnerTransform.Rotate(0f, 0f, spinnerRotateSpeed * Time.unscaledDeltaTime);
      }

      // 2. Sprite Sheet Frame Animation using unscaledDeltaTime (mượt mà cả khi scene đang load)
      if (enableSpriteAnimation && animationFrames != null && animationFrames.Length > 0 && IsVisible)
      {
        animationTimer += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(1f, animationFps);

        if (animationTimer >= frameDuration)
        {
          int framesToAdvance = Mathf.FloorToInt(animationTimer / frameDuration);
          animationTimer %= frameDuration;
          currentFrameIndex = (currentFrameIndex + framesToAdvance) % animationFrames.Length;

          if (spinnerImage != null && animationFrames[currentFrameIndex] != null)
          {
            spinnerImage.sprite = animationFrames[currentFrameIndex];
          }
        }
      }

      // 3. Smooth progress bar interpolation
      if (displayedProgress < targetProgress)
      {
        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, progressLerpSpeed * Time.unscaledDeltaTime);
        ApplyProgressVisuals(displayedProgress);
      }
    }

    /// <summary>
    /// Displays the loading screen with fresh random tip, artwork, and header.
    /// Supports instant reveal (alpha = 1) for cold scene starts.
    /// </summary>
    public void Show(string sceneName, int floorNumber, LoadingScreenData data, Action onFadeInComplete = null, bool instant = false)
    {
      // 1. Reset progress & animation
      targetProgress = 0f;
      displayedProgress = 0f;
      ApplyProgressVisuals(0f);

      currentFrameIndex = 0;
      animationTimer = 0f;
      if (enableSpriteAnimation && animationFrames != null && animationFrames.Length > 0)
      {
        if (spinnerImage == null && spinnerTransform != null) spinnerImage = spinnerTransform.GetComponent<Image>();
        if (spinnerImage != null && animationFrames[0] != null)
        {
          spinnerImage.sprite = animationFrames[0];
          spinnerImage.color = Color.white;
        }
      }
      if (!enableSpinnerRotation && spinnerTransform != null)
      {
        spinnerTransform.localRotation = Quaternion.identity;
      }

      // 2. Populate Header
      if (data != null)
      {
        data.GetLocationHeader(sceneName, floorNumber, out string title, out string subtitle);
        if (locationTitleText != null) locationTitleText.text = title;
        if (locationSubtitleText != null) locationSubtitleText.text = subtitle;

        // 3. Populate Tip
        LoadingTip tip = data.GetRandomTip();
        if (tip != null)
        {
          if (tipCategoryText != null)
          {
            tipCategoryText.text = $"[ {tip.category.ToUpper()} ]";
          }
          if (tipContentText != null)
          {
            tipContentText.text = tip.content;
          }
        }

        // 4. Populate Background Artwork
        Texture bgTex = data.GetRandomBackground(sceneName);
        if (backgroundImage != null)
        {
          if (bgTex != null)
          {
            backgroundImage.texture = bgTex;
            backgroundImage.color = Color.white;
          }
          else
          {
            // Fallback dark tint if no artwork available
            backgroundImage.color = new Color(0.08f, 0.08f, 0.12f, 1f);
          }
        }
      }
      else
      {
        if (locationTitleText != null) locationTitleText.text = "LOADING...";
        if (locationSubtitleText != null) locationSubtitleText.text = "Please wait";
        if (tipCategoryText != null) tipCategoryText.text = "[ TIPS ]";
        if (tipContentText != null) tipContentText.text = "Prepare yourself for the labyrinth ahead.";
      }

      if (vignetteOverlay != null)
      {
        vignetteOverlay.enabled = true;
      }

      if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);

      if (instant)
      {
        SetAlpha(1f);
        SetInteractable(true);
        currentFadeRoutine = null;
        onFadeInComplete?.Invoke();
      }
      else
      {
        currentFadeRoutine = StartCoroutine(FadeRoutine(1f, fadeInDuration, onFadeInComplete));
      }
    }

    /// <summary>
    /// Sets the target progress (clamped between 0 and 1).
    /// </summary>
    public void SetProgress(float progress)
    {
      targetProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Instantly forces the progress to a specific value without lerping.
    /// </summary>
    public void SetProgressImmediate(float progress)
    {
      targetProgress = Mathf.Clamp01(progress);
      displayedProgress = targetProgress;
      ApplyProgressVisuals(displayedProgress);
    }

    /// <summary>
    /// Fades out the loading screen and disables raycasts.
    /// </summary>
    public void Hide(Action onFadeOutComplete = null)
    {
      if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
      currentFadeRoutine = StartCoroutine(FadeRoutine(0f, fadeOutDuration, () =>
      {
        SetInteractable(false);
        onFadeOutComplete?.Invoke();
      }));
    }

    private void ApplyProgressVisuals(float progress)
    {
      if (progressFillImage != null)
      {
        progressFillImage.fillAmount = progress;
      }

      if (percentText != null)
      {
        percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
      }

      if (progressGlowImage != null)
      {
        // Glow stays active and fades slightly at 0
        progressGlowImage.enabled = progress > 0.01f;
      }
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

      SetInteractable(targetAlpha > 0.01f);

      float startAlpha = canvasGroup.alpha;
      float elapsed = 0f;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        // Smooth step easing
        canvasGroup.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
        yield return null;
      }

      canvasGroup.alpha = targetAlpha;
      currentFadeRoutine = null;
      onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
      if (canvasGroup != null) canvasGroup.alpha = alpha;
    }

    private void SetInteractable(bool active)
    {
      if (canvasGroup != null)
      {
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;
      }
    }
  }
}
