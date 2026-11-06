using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Bảng hiển thị thông báo tương tác phím [F] khi lại gần vật phẩm rơi trong thế giới 3D.
  /// Hiển thị Icon 2D, Tên món đồ đổi màu theo phẩm cấp (Rarity), và phím bấm tương tác.
  /// Tích hợp hoạt ảnh trượt nhẹ từ dưới lên (Souls-like Micro-Slide) và phản hồi đổi item mượt mà.
  /// </summary>
  public class PickupPromptUI : MonoBehaviour
  {
    [Header("=== GIAO DIỆN HIỂN THỊ ===")]
    [Tooltip("Khung chứa bảng tương tác (để trống nếu script gắn trực tiếp trên khung này)")]
    [SerializeField] private GameObject promptRoot;
    [Tooltip("Icon 2D của vật phẩm")]
    [SerializeField] private Image itemIcon;
    [Tooltip("Nền glow mờ theo phẩm cấp")]
    [SerializeField] private Image rarityBg;
    [Tooltip("Thanh bar dưới icon theo phẩm cấp")]
    [SerializeField] private Image rarityBar;
    [FormerlySerializedAs("rarityBorder")]
    [SerializeField] private Image legacyRarityBorder;
    [Tooltip("Tên vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [Tooltip("Gợi ý phím bấm và số lượng (ví dụ: [F] Nhặt x5)")]
    [SerializeField] private TextMeshProUGUI promptKeyText;
    [Tooltip("CanvasGroup điều khiển độ mờ")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== HOẠT ẢNH TRANSITION (SOULS-LIKE) ===")]
    [Tooltip("Thời lượng xuất hiện (giây)")]
    [SerializeField] private float showDuration = 0.14f;

    [Tooltip("Thời lượng biến mất (giây)")]
    [SerializeField] private float hideDuration = 0.09f;

    [Tooltip("Độ trượt từ dưới lên (pixel)")]
    [SerializeField] private float slideOffset = 14f;

    private readonly List<WorldItemPickup> activePickups = new List<WorldItemPickup>();
    private WorldItemPickup currentTarget;
    private WorldItemPickup previousTarget;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool hasCachedPosition = false;
    private Coroutine activeTransitionRoutine;
    private Coroutine itemSwitchRoutine;

    private void Awake()
    {
      if (rectTransform == null) rectTransform = transform as RectTransform;
      CacheOriginalPosition();

      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

      // Đảm bảo ẩn lúc đầu qua Alpha và tắt raycast
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;

      if (promptRoot != null && promptRoot != gameObject)
      {
        promptRoot.SetActive(false);
      }

      AutoFindReferences();
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
      if (itemIcon == null)
      {
        itemIcon = transform.Find("IconGroup/Icon")?.GetComponent<Image>()
                ?? transform.Find("Icon")?.GetComponent<Image>()
                ?? GetComponentInChildren<Image>();
      }

      if (rarityBg == null)
      {
        rarityBg = transform.Find("IconGroup/RarityBG")?.GetComponent<Image>()
                ?? transform.Find("RarityBG")?.GetComponent<Image>();
      }

      if (rarityBar == null)
      {
        rarityBar = transform.Find("IconGroup/RarityBar")?.GetComponent<Image>()
                 ?? transform.Find("RarityBar")?.GetComponent<Image>();
      }

      if (itemNameText == null)
      {
        itemNameText = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
      }

      if (promptKeyText == null)
      {
        promptKeyText = transform.Find("KeyPrompt")?.GetComponent<TextMeshProUGUI>()
                     ?? transform.Find("Key")?.GetComponent<TextMeshProUGUI>()
                     ?? transform.Find("Prompt")?.GetComponent<TextMeshProUGUI>()
                     ?? transform.Find("PickupText")?.GetComponent<TextMeshProUGUI>();
      }
    }

    private void OnEnable()
    {
      WorldItemPickup.OnFocusPickup += HandlePickupFocused;
      WorldItemPickup.OnUnfocusPickup += HandlePickupUnfocused;
    }

    private void OnDisable()
    {
      WorldItemPickup.OnFocusPickup -= HandlePickupFocused;
      WorldItemPickup.OnUnfocusPickup -= HandlePickupUnfocused;

      if (activeTransitionRoutine != null)
      {
        StopCoroutine(activeTransitionRoutine);
        activeTransitionRoutine = null;
      }

      if (itemSwitchRoutine != null)
      {
        StopCoroutine(itemSwitchRoutine);
        itemSwitchRoutine = null;
      }

      HideImmediate();
      activePickups.Clear();
      currentTarget = null;
      previousTarget = null;
    }

    private void HandlePickupFocused(WorldItemPickup pickup)
    {
      if (pickup == null || activePickups.Contains(pickup)) return;
      activePickups.Add(pickup);
      UpdateTargetPickup();
    }

    private void HandlePickupUnfocused(WorldItemPickup pickup)
    {
      if (pickup == null) return;
      activePickups.Remove(pickup);
      UpdateTargetPickup();
    }

    private void Update()
    {
      // Dọn dẹp danh sách các pickup đã bị nhặt hoặc destroy
      for (int i = activePickups.Count - 1; i >= 0; i--)
      {
        if (activePickups[i] == null)
        {
          activePickups.RemoveAt(i);
        }
      }

      UpdateTargetPickup();
    }

    private void UpdateTargetPickup()
    {
      if (activePickups.Count == 0)
      {
        if (previousTarget != null)
        {
          previousTarget = null;
          currentTarget = null;
          TriggerHide();
        }
        return;
      }

      // Ưu tiên chọn vật phẩm gần nhất với Player
      Transform playerTr = null;
      var playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj != null)
      {
        playerTr = playerObj.transform;
      }
      else if (Camera.main != null)
      {
        playerTr = Camera.main.transform;
      }
      else
      {
        playerTr = transform;
      }

      WorldItemPickup closest = null;
      float closestDist = float.MaxValue;

      for (int i = 0; i < activePickups.Count; i++)
      {
        var p = activePickups[i];
        if (p == null) continue;
        float d = Vector3.Distance(playerTr.position, p.transform.position);
        if (d < closestDist)
        {
          closestDist = d;
          closest = p;
        }
      }

      currentTarget = closest;

      if (currentTarget != null && currentTarget.ItemData != null)
      {
        if (previousTarget != currentTarget)
        {
          DisplayPickupInfo(currentTarget.ItemData, currentTarget.Quantity);

          bool isAlreadyVisible = canvasGroup != null && canvasGroup.alpha > 0.4f;
          if (isAlreadyVisible && previousTarget != null)
          {
            // Nếu đang mở sẵn mà đổi vật phẩm soi: Micro-punch nhẹ để báo hiệu vật phẩm đổi
            if (itemSwitchRoutine != null) StopCoroutine(itemSwitchRoutine);
            itemSwitchRoutine = StartCoroutine(PunchItemSwitchRoutine());
          }
          else
          {
            // Mở mới hoàn toàn
            TriggerShow();
          }

          previousTarget = currentTarget;
        }
      }
      else
      {
        if (previousTarget != null)
        {
          previousTarget = null;
          TriggerHide();
        }
      }
    }

    private void TriggerShow()
    {
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

    private void TriggerHide()
    {
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

    private IEnumerator PunchItemSwitchRoutine()
    {
      Transform textTr = itemNameText != null ? itemNameText.transform : null;
      Transform iconTr = itemIcon != null ? itemIcon.transform : null;

      Vector3 punchScale = Vector3.one * 1.15f;
      float duration = 0.10f;
      float elapsed = 0f;

      if (textTr != null) textTr.localScale = punchScale;
      if (iconTr != null) iconTr.localScale = punchScale;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseOutQuad(t);

        if (textTr != null) textTr.localScale = Vector3.LerpUnclamped(punchScale, Vector3.one, e);
        if (iconTr != null) iconTr.localScale = Vector3.LerpUnclamped(punchScale, Vector3.one, e);

        yield return null;
      }

      if (textTr != null) textTr.localScale = Vector3.one;
      if (iconTr != null) iconTr.localScale = Vector3.one;
      itemSwitchRoutine = null;
    }

    private void DisplayPickupInfo(ItemData item, int quantity)
    {
      Color rarityColor = item.Rarity.GetRarityColor();

      if (itemIcon != null)
      {
        bool hasIcon = (item != null && item.Icon != null);
        itemIcon.gameObject.SetActive(hasIcon);
        itemIcon.enabled = hasIcon;
        if (hasIcon)
        {
          itemIcon.sprite = item.Icon;
          itemIcon.color = Color.white;
        }
      }

      // Đổi màu nền glow mờ và thanh bar dưới icon theo phẩm cấp
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

      // Nếu có hệ thống hiển thị mới (rarityBg / rarityBar) thì trả legacyRarityBorder về màu chuẩn
      if (legacyRarityBorder != null)
      {
        if (rarityBg != null || rarityBar != null)
        {
          legacyRarityBorder.color = Color.white;
        }
        else
        {
          legacyRarityBorder.color = rarityColor;
        }
      }

      if (itemNameText != null)
      {
        itemNameText.color = Color.white;
        itemNameText.text = item.Rarity.FormatWithGradient(item.ItemName);
      }

      if (promptKeyText != null)
      {
        if (quantity > 1)
        {
          promptKeyText.text = $"Pick up x{quantity}";
        }
        else
        {
          promptKeyText.text = "Pick up";
        }
      }
    }
  }
}
