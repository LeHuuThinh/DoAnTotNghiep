using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý ô phím tắt Bình máu / Tiêu hao (UsableSlotUI) hiển thị cố định ở góc dưới bên trái màn hình HUD.
  /// Hỗ trợ phím tắt C và click chuột trực tiếp để sử dụng bình thuốc đã trang bị kèm vòng xoay hồi chiêu CDFill.
  /// </summary>
  public class UsableSlotUI : MonoBehaviour, IPointerClickHandler
  {
    [Header("=== THÀNH PHẦN UI (HUD QUICK SLOT) ===")]
    [Tooltip("Icon hiển thị vật phẩm đang trang bị")]
    [SerializeField] private Image icon;

    [Tooltip("Icon placeholder hiển thị khi ô trống")]
    [SerializeField] private Image placeholder;

    [Tooltip("Nền RarityBG đổi màu theo phẩm cấp")]
    [SerializeField] private Image rarityBg;

    [Tooltip("Thanh RarityBar dưới icon")]
    [SerializeField] private Image rarityBar;

    [Tooltip("Họa tiết góc viền (Corner)")]
    [SerializeField] private Image corner;

    [Tooltip("Vòng xoay hồi chiêu CDFill (Radial 360)")]
    [SerializeField] private Image cdFill;

    [Tooltip("Text số lượng bình thuốc dồn")]
    [SerializeField] private TextMeshProUGUI stackCountText;

    [Tooltip("Icon phím tắt C")]
    [SerializeField] private Image keyIcon;

    [Header("=== HIỆU ỨNG NHẤN NẢY (POP PUNCH ANIMATION) ===")]
    [Tooltip("Độ phóng to khi nhấn dùng")]
    [SerializeField] private float punchScale = 1.15f;

    [Tooltip("Thời gian hoạt ảnh nảy")]
    [SerializeField] private float punchDuration = 0.15f;

    private bool isInitialized = false;
    private Coroutine activePunchRoutine;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
      EnsureInitialized();
    }

    private void Start()
    {
      EnsureInitialized();

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnUsableChanged += HandleUsableChanged;
        InventoryManager.Instance.OnConsumableCooldownUpdated += HandleCooldownUpdated;
        InventoryManager.Instance.OnUsableUsed += PlayPunchAnimation;
        HandleUsableChanged(InventoryManager.Instance.EquippedUsable, InventoryManager.Instance.EquippedUsableCount);
      }
    }

    private void OnDestroy()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnUsableChanged -= HandleUsableChanged;
        InventoryManager.Instance.OnConsumableCooldownUpdated -= HandleCooldownUpdated;
        InventoryManager.Instance.OnUsableUsed -= PlayPunchAnimation;
      }
    }

    public void EnsureInitialized()
    {
      if (isInitialized) return;
      isInitialized = true;

      originalScale = transform.localScale;
      AutoFindReferences();
    }

    public void AutoFindReferences()
    {
      Transform bg = transform.Find("UsableBG");
      if (bg == null) bg = transform;

      if (icon == null) icon = bg.Find("Icon")?.GetComponent<Image>();
      if (placeholder == null) placeholder = bg.Find("PlaceHolder")?.GetComponent<Image>();
      if (rarityBg == null) rarityBg = bg.Find("RarityBG")?.GetComponent<Image>();
      if (rarityBar == null) rarityBar = bg.Find("RarityBar")?.GetComponent<Image>();
      if (corner == null) corner = bg.Find("Corner")?.GetComponent<Image>();
      if (cdFill == null) cdFill = bg.Find("CDFill")?.GetComponent<Image>();
      if (stackCountText == null) stackCountText = bg.Find("StackCountText")?.GetComponent<TextMeshProUGUI>();
      if (keyIcon == null) keyIcon = bg.Find("KeyIcon")?.GetComponent<Image>();

      if (cdFill != null)
      {
        cdFill.raycastTarget = false;
      }
    }

    private void Update()
    {
      if (!InventoryManager.HasInstance) return;

      float cdNorm = InventoryManager.Instance.ConsumableCooldownNormalized;
      if (cdFill != null)
      {
        bool onCd = cdNorm > 0.001f && InventoryManager.Instance.EquippedUsable != null;
        cdFill.gameObject.SetActive(onCd);
        if (onCd)
        {
          cdFill.fillAmount = cdNorm;
        }
      }
    }

    public void HandleUsableChanged(ItemData item, int count)
    {
      EnsureInitialized();

      if (item == null || count <= 0)
      {
        if (icon != null) icon.gameObject.SetActive(false);
        if (placeholder != null) placeholder.gameObject.SetActive(true);
        if (rarityBg != null) rarityBg.gameObject.SetActive(false);
        if (rarityBar != null) rarityBar.gameObject.SetActive(false);
        if (stackCountText != null) stackCountText.gameObject.SetActive(false);
        if (cdFill != null) cdFill.gameObject.SetActive(false);
        return;
      }

      // 1. Icon
      if (icon != null)
      {
        icon.gameObject.SetActive(true);
        icon.sprite = item.Icon;
        icon.color = Color.white;
      }

      // 2. Placeholder
      if (placeholder != null)
      {
        placeholder.gameObject.SetActive(false);
      }

      // 3. Rarity
      Color c = item.Rarity.GetRarityColor();
      if (rarityBg != null)
      {
        rarityBg.gameObject.SetActive(true);
        rarityBg.color = c;
      }

      if (rarityBar != null)
      {
        rarityBar.gameObject.SetActive(true);
        rarityBar.color = c;
      }

      // 4. Số lượng dồn
      if (stackCountText != null)
      {
        stackCountText.gameObject.SetActive(count > 1);
        stackCountText.text = count.ToString();
      }
    }

    public void HandleCooldownUpdated(float currentTimer, float maxCooldown)
    {
      if (cdFill != null)
      {
        float norm = maxCooldown > 0f ? Mathf.Clamp01(currentTimer / maxCooldown) : 0f;
        bool onCd = norm > 0.001f && InventoryManager.Instance != null && InventoryManager.Instance.EquippedUsable != null;
        cdFill.gameObject.SetActive(onCd);
        if (onCd)
        {
          cdFill.fillAmount = norm;
        }
      }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (eventData.button == PointerEventData.InputButton.Left)
      {
        TriggerUse();
      }
    }

    /// <summary>
    /// Kích hoạt sử dụng bình thuốc và chạy hoạt ảnh nảy
    /// </summary>
    public void TriggerUse()
    {
      if (!InventoryManager.HasInstance) return;
      InventoryManager.Instance.UseEquippedUsable();
    }

    public void PlayPunchAnimation()
    {
      if (!gameObject.activeInHierarchy) return;

      if (activePunchRoutine != null)
      {
        StopCoroutine(activePunchRoutine);
      }
      activePunchRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
      float elapsed = 0f;
      float halfDuration = punchDuration * 0.5f;

      // 1. Phóng to
      while (elapsed < halfDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / halfDuration);
        transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, UITransitionHelper.EaseOutQuad(t));
        yield return null;
      }

      // 2. Co về bình thường
      elapsed = 0f;
      while (elapsed < halfDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / halfDuration);
        transform.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, UITransitionHelper.EaseOutQuad(t));
        yield return null;
      }

      transform.localScale = originalScale;
      activePunchRoutine = null;
    }
  }
}
