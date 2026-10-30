using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Bảng hiển thị thông báo tương tác phím [F] cho Portal, Rương, NPC...
  /// Hỗ trợ thanh Fill dọc (Vertical Fill) từ dưới lên khi đè giữ phím.
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

    [Header("=== HIỆU ỨNG ===")]
    [SerializeField] private float fadeSpeed = 14f;

    private float targetAlpha = 0f;
    private IInteractable currentTarget;

    private void Awake()
    {
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

    private void AutoFindReferences()
    {
      if (promptRoot == null) promptRoot = gameObject;
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

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

      targetAlpha = 0f;
      if (canvasGroup != null) canvasGroup.alpha = 0f;
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

        targetAlpha = 1f;
      }
      else
      {
        targetAlpha = 0f;
        if (holdFillImage != null)
        {
          holdFillImage.fillAmount = 0f;
        }
      }
    }

    private void HandleProgressChanged(IInteractable interactable, float progress)
    {
      if (holdFillImage != null)
      {
        holdFillImage.fillAmount = progress;
      }
    }

    private void Update()
    {
      if (canvasGroup != null)
      {
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        if (promptRoot != null && promptRoot != gameObject)
        {
          if (targetAlpha > 0f && !promptRoot.activeSelf) promptRoot.SetActive(true);
          else if (targetAlpha == 0f && canvasGroup.alpha <= 0.01f && promptRoot.activeSelf) promptRoot.SetActive(false);
        }
      }
    }
  }
}
