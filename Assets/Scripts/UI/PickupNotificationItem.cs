using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Đại diện cho 1 dòng thông báo nhặt đồ ở mép trái màn hình.
  /// Được quản lý 100% bởi VerticalLayoutGroup của Feed cha.
  /// Hiệu ứng Fade In mượt mà khi xuất hiện, giữ 2.5s, rồi Fade Out và tự hủy.
  /// </summary>
  public class PickupNotificationItem : MonoBehaviour
  {
    [Header("=== GIAO DIỆN ===")]
    [SerializeField] private Image itemIcon;
    [Tooltip("Nền glow mờ theo phẩm cấp")]
    [SerializeField] private Image rarityBg;
    [Tooltip("Thanh bar dưới icon theo phẩm cấp")]
    [SerializeField] private Image rarityBar;
    [FormerlySerializedAs("rarityBorder")]
    [SerializeField] private Image legacyRarityBorder;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;

    [Header("=== KÍCH THƯỚC (TÙY CHỈNH DÀI / RỘNG) ===")]
    [Tooltip("Chiều dài (Width) của dòng thông báo")]
    [SerializeField] private float itemWidth = 300f;
    [Tooltip("Chiều cao (Height) của dòng thông báo")]
    [SerializeField] private float itemHeight = 50f;

    private void Awake()
    {
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
      if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

      // 1. Đảm bảo Pos Z luôn bằng 0 để không bao giờ bị bay tít ra sau Canvas 3D
      Vector3 localPos = transform.localPosition;
      localPos.z = 0f;
      transform.localPosition = localPos;

      // 2. Áp dụng kích thước (ưu tiên thông số lớn hơn giữa Inspector và RectTransform)
      if (rectTransform != null)
      {
        if (rectTransform.sizeDelta.x > itemWidth) itemWidth = rectTransform.sizeDelta.x;
        if (rectTransform.sizeDelta.y > itemHeight) itemHeight = rectTransform.sizeDelta.y;
        rectTransform.sizeDelta = new Vector2(itemWidth, itemHeight);
      }

      // 3. Khai báo LayoutElement để VerticalLayoutGroup luôn cấp đủ Width x Height
      LayoutElement layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
      layout.minWidth = itemWidth;
      layout.preferredWidth = itemWidth;
      layout.minHeight = itemHeight;
      layout.preferredHeight = itemHeight;

      AutoFindReferences();
    }

    private void AutoFindReferences()
    {
      if (itemIcon == null)
      {
        itemIcon = transform.Find("IconGroup/Icon")?.GetComponent<Image>()
                ?? transform.Find("Icon")?.GetComponent<Image>();
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

      if (rarityBg == null && rarityBar == null && legacyRarityBorder == null)
      {
        legacyRarityBorder = transform.Find("IconGroup/Border")?.GetComponent<Image>()
                          ?? transform.Find("Border")?.GetComponent<Image>();
      }

      if (itemNameText == null)
      {
        itemNameText = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
      }

      if (quantityText == null)
      {
        quantityText = transform.Find("Count")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
      }
    }

    public void Setup(ItemData item, int count, float duration = 2.5f)
    {
      if (item == null) return;

      AutoFindReferences();

      Color rarityColor = item.Rarity.GetRarityColor();

      if (itemIcon != null)
      {
        bool hasIcon = (item.Icon != null);
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

      if (rarityBg == null && rarityBar == null && legacyRarityBorder != null)
      {
        legacyRarityBorder.color = rarityColor;
      }

      if (itemNameText != null)
      {
        itemNameText.color = Color.white;
        itemNameText.text = item.Rarity.FormatWithGradient(item.ItemName);
      }

      if (quantityText != null)
      {
        // Chỉ hiển thị số lượng +X, không kèm chữ Vàng vì ItemName đã hiển thị "Tiền Vàng"
        quantityText.text = $"+{count}";

        if (item.ItemType == ItemType.Currency)
        {
          quantityText.color = new Color(1.0f, 0.85f, 0.2f, 1f); // Màu vàng óng
        }
        else
        {
          quantityText.color = Color.white;
        }
      }

      StartCoroutine(AnimateRoutine(duration));
    }

    private IEnumerator AnimateRoutine(float displayDuration)
    {
      // 1. PHASE 1: FADE IN + ZOOM SCALE MƯỢT MÀ (SOULS-LIKE)
      canvasGroup.alpha = 0f;
      Vector3 startScale = Vector3.one * 0.92f;
      transform.localScale = startScale;

      float elapsed = 0f;
      float fadeInTime = 0.16f;

      while (elapsed < fadeInTime)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / fadeInTime);
        float e = UITransitionHelper.EaseOutQuad(t);

        canvasGroup.alpha = Mathf.Lerp(0f, 1f, e);
        transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, e);
        yield return null;
      }

      canvasGroup.alpha = 1f;
      transform.localScale = Vector3.one;

      // 2. PHASE 2: GIỮ NGUYÊN HIỂN THỊ
      yield return new WaitForSecondsRealtime(displayDuration);

      // 3. PHASE 3: FADE OUT MỜ DẦN BIẾN MẤT
      elapsed = 0f;
      float fadeOutTime = 0.22f;
      Vector3 endScale = Vector3.one * 0.95f;

      while (elapsed < fadeOutTime)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / fadeOutTime);
        float e = UITransitionHelper.EaseInQuad(t);

        canvasGroup.alpha = Mathf.Lerp(1f, 0f, e);
        transform.localScale = Vector3.LerpUnclamped(Vector3.one, endScale, e);
        yield return null;
      }

      // 4. TỰ HỦY
      Destroy(gameObject);
    }
  }
}
