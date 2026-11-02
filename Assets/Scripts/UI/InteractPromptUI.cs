using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Bảng hiển thị thông báo tương tác phím [F] cho Portal, Rương, NPC...
  /// Hỗ trợ thanh Fill dọc (Vertical Fill), hiệu ứng trượt nhẹ từ dưới lên (Souls-like Micro-Slide)
  /// và phản hồi phím có lực khi đè giữ.
  /// </summary>
  public class InteractPromptUI : MonoBehaviour
  {
    [Header("=== GIAO DIỆN HIỂN THỊ ===")]
    [Tooltip("Khung gốc bảng tương tác (mặc định lấy chính GameObject này)")]
    [SerializeField] private GameObject promptRoot;

    [Tooltip("CanvasGroup điều khiển độ mờ Fade In / Out")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Image dạng Filled (Vertical, Bottom) bên trong KeyIcon để hiển thị tiến độ đè giữ phím F")]
    [SerializeField] private Image holdFillImage;

    [Tooltip("Text hiển thị câu lệnh tương tác (ví dụ: 'Vào Hầm Ngục', 'Mở Rương')")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("Khung icon phím F để tạo hiệu ứng lún phím có lực")]
    [SerializeField] private RectTransform keyIconRect;

    [Header("=== HOẠT ẢNH TRANSITION (SOULS-LIKE) ===")]
    [Tooltip("Thời lượng xuất hiện (giây)")]
    [SerializeField] private float showDuration = 0.14f;

    [Tooltip("Thời lượng biến mất (giây)")]
    [SerializeField] private float hideDuration = 0.09f;

    [Tooltip("Độ trượt từ dưới lên (pixel)")]
    [SerializeField] private float slideOffset = 14f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool hasCachedPosition = false;
    private Coroutine activeTransitionRoutine;
    private Coroutine keyIconRoutine;
    private IInteractable currentTarget;

    private void Awake()
    {
      if (rectTransform == null) rectTransform = transform as RectTransform;
      CacheOriginalPosition();
      AutoFindReferences();

      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
      }

      if (holdFillImage != null)
      {
        holdFillImage.fillAmount = 0f;
      }
    }

    private void CacheOriginalPosition()
    {
      if (hasCachedPosition) return;
      if (rectTransform == null) rectTransform = transform as RectTransform;
      if (rectTransform != null)
      {
        originalAnchoredPosition = rectTransform.anchoredPosition;
        hasCachedPosition = true;
      }
    }

    private void AutoFindReferences()
    {
      if (promptRoot == null) promptRoot = gameObject;
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
      if (rectTransform == null) rectTransform = transform as RectTransform;

      if (keyIconRect == null)
      {
        keyIconRect = transform.Find("KeyIcon") as RectTransform;
      }

      if (holdFillImage == null)
      {
        holdFillImage = transform.Find("KeyIcon/HoldFill")?.GetComponent<Image>()
                     ?? transform.Find("HoldFill")?.GetComponent<Image>();
      }

      if (promptText == null)
      {
        promptText = transform.Find("PickupText")?.GetComponent<TextMeshProUGUI>()
                  ?? transform.Find("Text")?.GetComponent<TextMeshProUGUI>()
                  ?? GetComponentInChildren<TextMeshProUGUI>();
      }
    }

    private void OnEnable()
    {
      PlayerInteraction.OnInteractFocusChanged += HandleFocusChanged;
      PlayerInteraction.OnInteractProgressChanged += HandleProgressChanged;
    }

    private void OnDisable()
    {
      PlayerInteraction.OnInteractFocusChanged -= HandleFocusChanged;
      PlayerInteraction.OnInteractProgressChanged -= HandleProgressChanged;

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      if (keyIconRoutine != null)
      {
        StopCoroutine(keyIconRoutine);
        keyIconRoutine = null;
      }

      HideImmediate();
      if (holdFillImage != null) holdFillImage.fillAmount = 0f;
      currentTarget = null;
    }

    private void HandleFocusChanged(IInteractable interactable)
    {
      currentTarget = interactable;

      if (interactable != null)
      {
        if (promptText != null)
        {
          promptText.text = interactable.InteractionPrompt;
        }

        if (holdFillImage != null)
        {
          holdFillImage.fillAmount = 0f;
        }

        if (keyIconRect != null)
        {
          keyIconRect.localScale = Vector3.one;
        }

        if (promptRoot != null && promptRoot != gameObject && !promptRoot.activeSelf)
        {
          promptRoot.SetActive(true);
        }

        if (activeTransitionRoutine != null)
        {
          StopCoroutine(activeTransitionRoutine);
        }
        activeTransitionRoutine = StartCoroutine(AnimateShowRoutine());
      }
      else
      {
        if (holdFillImage != null)
        {
          holdFillImage.fillAmount = 0f;
        }

        if (activeTransitionRoutine != null)
        {
          StopCoroutine(activeTransitionRoutine);
        }

        if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.05f)
        {
          activeTransitionRoutine = StartCoroutine(AnimateHideRoutine());
        }
        else
        {
          HideImmediate();
        }
      }
    }

    private void HandleProgressChanged(IInteractable interactable, float progress)
    {
      if (holdFillImage != null)
      {
        holdFillImage.fillAmount = progress;
      }

      if (keyIconRect != null)
      {
        if (progress > 0.02f && progress < 1f)
        {
          // Phím [F] lún nhẹ khi đè giữ
          keyIconRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.92f, progress * 2f);
        }
        else if (progress >= 1f)
        {
          // Hoàn thành tương tác: Nảy nhẹ
          if (keyIconRoutine != null) StopCoroutine(keyIconRoutine);
          keyIconRoutine = StartCoroutine(PunchKeyIconRoutine(1.20f, 0.12f));
        }
        else
        {
          keyIconRect.localScale = Vector3.one;
        }
      }
    }

    private IEnumerator AnimateShowRoutine()
    {
      CacheOriginalPosition();

      Vector2 startPos = originalAnchoredPosition + new Vector2(0f, -slideOffset);
      Vector3 startScale = Vector3.one * 0.95f;

      if (rectTransform != null)
      {
        rectTransform.anchoredPosition = startPos;
        rectTransform.localScale = startScale;
      }

      if (canvasGroup != null) canvasGroup.alpha = 0f;

      float elapsed = 0f;
      while (elapsed < showDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / showDuration);
        float e = UITransitionHelper.EaseOutQuad(t);

        if (rectTransform != null)
        {
          rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, originalAnchoredPosition, e);
          rectTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, e);
        }

        if (canvasGroup != null)
        {
          canvasGroup.alpha = Mathf.Lerp(0f, 1f, e);
        }

        yield return null;
      }

      if (rectTransform != null)
      {
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = Vector3.one;
      }
      if (canvasGroup != null) canvasGroup.alpha = 1f;

      activeTransitionRoutine = null;
    }

    private IEnumerator AnimateHideRoutine()
    {
      CacheOriginalPosition();

      Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : originalAnchoredPosition;
      Vector2 targetPos = originalAnchoredPosition + new Vector2(0f, -slideOffset * 0.6f);
      Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
      Vector3 endScale = Vector3.one * 0.96f;
      float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

      float elapsed = 0f;
      while (elapsed < hideDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / hideDuration);
        float e = UITransitionHelper.EaseInQuad(t);

        if (rectTransform != null)
        {
          rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, e);
          rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
        }

        if (canvasGroup != null)
        {
          canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, e);
        }

        yield return null;
      }

      HideImmediate();
      activeTransitionRoutine = null;
    }

    private void HideImmediate()
    {
      if (canvasGroup != null) canvasGroup.alpha = 0f;
      if (rectTransform != null && hasCachedPosition)
      {
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = Vector3.one;
      }
      if (promptRoot != null && promptRoot != gameObject)
      {
        promptRoot.SetActive(false);
      }
    }

    private IEnumerator PunchKeyIconRoutine(float scale, float duration)
    {
      if (keyIconRect == null) yield break;
      Vector3 startScale = Vector3.one * scale;
      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseOutQuad(t);
        keyIconRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, e);
        yield return null;
      }
      keyIconRect.localScale = Vector3.one;
      keyIconRoutine = null;
    }
  }
}
