using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private Image rarityBorder;
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

      if (rarityBorder == null)
      {
        rarityBorder = transform.Find("IconGroup/Border")?.GetComponent<Image>()
                    ?? transform.Find("IconGroup/RarityBG")?.GetComponent<Image>()
                    ?? transform.Find("RarityBar")?.GetComponent<Image>()
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
        itemIcon.sprite = item.Icon;
        itemIcon.enabled = (item.Icon != null);
      }

      if (rarityBorder != null)
      {
        rarityBorder.color = rarityColor;
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
      // 1. PHASE 1: FADE IN MƯỢT MÀ
      canvasGroup.alpha = 0f;
      float elapsed = 0f;
      float fadeInTime = 0.2f;

      while (elapsed < fadeInTime)
      {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
        yield return null;
      }

      canvasGroup.alpha = 1f;

      // 2. PHASE 2: GIỮ NGUYÊN HIỂN THỊ
      yield return new WaitForSecondsRealtime(displayDuration);

      // 3. PHASE 3: FADE OUT MỜ DẦN BIẾN MẤT
      elapsed = 0f;
      float fadeOutTime = 0.3f;

      while (elapsed < fadeOutTime)
      {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
        yield return null;
      }

      // 4. TỰ HỦY
      Destroy(gameObject);
    }
  }
}
