using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý UI Thanh Máu Boss (BossHP) và Diễn Hoạt Tên Trùm Souls-like (Task D02-4):
  /// - Ban đầu ẩn hoàn toàn.
  /// - Khi cutscene bắt đầu: Hiện thanh máu Boss trên đỉnh màn hình với màu xanh băng tuyết (chưa có tên).
  /// - Khi Boss tiếp đất & chuẩn bị phun băng: Banner & Tên xuất hiện từ từ ở giữa màn hình.
  /// - Sau 1.2s: Banner mờ dần biến mất, dòng Tên vừa bay lên phía trên thanh máu vừa thu nhỏ lại vừa vặn.
  /// - Trong trận chiến: Cập nhật fillAmount theo máu Boss thời gian thực.
  /// </summary>
  public class BossHUDController : MonoBehaviour
  {
    public static BossHUDController Instance { get; private set; }

    [Header("=== THAM CHIẾU CÂY UI ENEMYCANVAS ===")]
    [Tooltip("Khung chứa toàn bộ thanh máu Boss (BossHP)")]
    [SerializeField] private RectTransform bossHPRoot;
    [Tooltip("Thanh Fill máu của Boss")]
    [SerializeField] private Image hpFillImage;
    [Tooltip("Vạch chia 50% máu")]
    [SerializeField] private GameObject divide50Marker;
    [Tooltip("Khung viền trang trí thanh máu")]
    [SerializeField] private Image hpBorderImage;

    [Header("=== BANNER & TÊN TRÙM ===")]
    [Tooltip("Nhóm chứa Banner và Tên ở giữa màn hình (IceGolemName)")]
    [SerializeField] private RectTransform nameGroupRoot;
    [Tooltip("Ảnh nền Banner trang trí phong cách Dark Souls")]
    [SerializeField] private Image bannerImage;
    [Tooltip("Text TextMeshPro hiển thị danh xưng & tên trùm")]
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Header("=== CÀI ĐẶT MÀU SẮC ===")]
    [Tooltip("Màu xanh băng giá cho thanh máu Boss Ice Golem")]
    [SerializeField] private Color frostHpColor = new Color(0.25f, 0.82f, 1.0f, 1.0f);

    [Header("=== CÀI ĐẶT BANNER TÊN XUẤT HIỆN BÊN PHẢI (RULE OF THIRDS) ===")]
    [Tooltip("Tọa độ xuất hiện ban đầu của Banner Tên ở nửa PHẢI màn hình (X dương = lệch phải; mặc định 420px lệch sang phải đối trọng với Boss bên trái)")]
    [SerializeField] private Vector2 bannerIntroAnchoredPos = new Vector2(420f, -540f);
    [Tooltip("Tỉ lệ kích thước của Banner khi xuất hiện ban đầu ở bên phải")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float bannerIntroScale = 0.95f;
    [Tooltip("Thời gian Banner mờ dần hiện rõ (Fade-in, giây)")]
    [SerializeField] private float bannerFadeInDuration = 0.6f;
    [Tooltip("Thời gian giữ Banner ở bên phải để người chơi đọc tên (giây)")]
    [SerializeField] private float bannerHoldDuration = 1.2f;
    [Tooltip("Thời gian Tên trùm bay từ bên phải lên neo vào thanh máu góc trên phải (giây)")]
    [SerializeField] private float nameTransitionDuration = 1.1f;

    [Header("=== ĐIỂM ĐẾN CỦA TÊN TRÙM TRÊN THANH MÁU (GÓC TRÊN PHẢI) ===")]
    [Tooltip("TÙY CHỌN: Kéo RectTransform slot trống trên thanh máu vào đây để tên bay chính xác vào đó. Nếu để trống, script sẽ tự lấy BossHP làm tâm đích")]
    [SerializeField] private RectTransform nameTargetSlot;
    [Tooltip("Tọa độ offset (X, Y) để bạn tự do căn chỉnh tên Boss nằm chuẩn xác trên thanh máu (Pixel)")]
    [SerializeField] private Vector2 nameOffsetOnBar = new Vector2(0f, 38f);
    [Tooltip("Tỉ lệ thu nhỏ (Scale) của Tên khi bay về thanh máu")]
    [Range(0.2f, 1.5f)]
    [SerializeField] private float targetNameScale = 0.55f;

    [Header("=== THỜI LƯỢNG THANH MÁU ===")]
    [SerializeField] private float barFadeInDuration = 0.8f;

    private CanvasGroup bossHPCanvasGroup;
    private CanvasGroup nameGroupCanvasGroup;
    private CanvasGroup bannerCanvasGroup;

    private Transform originalTextParent;
    private Vector3 originalTextLocalPos;
    private Vector3 originalTextLocalScale;

    private Health targetBossHealth;
    private bool isNameAnchoredOnBar = false;

    public bool IsNameAnchoredOnBar => isNameAnchoredOnBar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHUDControllerInstance()
    {
      if (FindFirstObjectByType<BossHUDController>() == null)
      {
        GameObject canvasObj = GameObject.Find("EnemyCanvas");
        if (canvasObj != null)
        {
          canvasObj.AddComponent<BossHUDController>();
          Debug.Log("<color=cyan>[BossHUDController]</color> 👑 Tự động gắn BossHUDController lên EnemyCanvas tại runtime.");
        }
      }
    }

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      AutoFindReferencesIfMissing();
      SetupCanvasGroups();
      InitializeState();
    }

    /// <summary>
    /// Tự động tìm kiếm các phần tử con theo đúng cấu trúc EnemyCanvas đã dựng sẵn
    /// </summary>
    private void AutoFindReferencesIfMissing()
    {
      if (bossHPRoot == null)
      {
        Transform hp = transform.Find("BossHP") ?? FindInParentsOrChildren(transform, "BossHP");
        if (hp != null) bossHPRoot = hp as RectTransform;
      }

      if (bossHPRoot != null)
      {
        if (hpFillImage == null)
        {
          Transform fill = bossHPRoot.Find("Fill");
          if (fill != null) hpFillImage = fill.GetComponent<Image>();
        }
        if (divide50Marker == null)
        {
          Transform div = bossHPRoot.Find("50%Divide");
          if (div != null) divide50Marker = div.gameObject;
        }
        if (hpBorderImage == null)
        {
          Transform border = bossHPRoot.Find("Border");
          if (border != null) hpBorderImage = border.GetComponent<Image>();
        }
      }

      if (nameGroupRoot == null)
      {
        Transform nGroup = transform.Find("IceGolemName") ?? FindInParentsOrChildren(transform, "IceGolemName");
        if (nGroup != null) nameGroupRoot = nGroup as RectTransform;
      }

      if (nameGroupRoot != null)
      {
        if (bannerImage == null)
        {
          Transform b = nameGroupRoot.Find("Banner");
          if (b != null) bannerImage = b.GetComponent<Image>();
        }
        if (bossNameText == null)
        {
          Transform t = nameGroupRoot.Find("Text");
          if (t != null) bossNameText = t.GetComponent<TextMeshProUGUI>();
        }
        if (bossNameText != null && originalTextParent == null)
        {
          originalTextParent = bossNameText.transform.parent;
          originalTextLocalPos = bossNameText.transform.localPosition;
          originalTextLocalScale = bossNameText.transform.localScale;
        }
      }
    }

    private Transform FindInParentsOrChildren(Transform root, string targetName)
    {
      Transform curr = root;
      while (curr != null)
      {
        Transform res = curr.Find(targetName);
        if (res != null) return res;
        curr = curr.parent;
      }

      // Quét các con
      foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
      {
        if (child.name == targetName) return child;
      }
      return null;
    }

    private void SetupCanvasGroups()
    {
      if (bossHPRoot != null)
      {
        bossHPCanvasGroup = bossHPRoot.GetComponent<CanvasGroup>();
        if (bossHPCanvasGroup == null) bossHPCanvasGroup = bossHPRoot.gameObject.AddComponent<CanvasGroup>();
      }

      if (nameGroupRoot != null)
      {
        nameGroupCanvasGroup = nameGroupRoot.GetComponent<CanvasGroup>();
        if (nameGroupCanvasGroup == null) nameGroupCanvasGroup = nameGroupRoot.gameObject.AddComponent<CanvasGroup>();
      }

      if (bannerImage != null)
      {
        bannerCanvasGroup = bannerImage.GetComponent<CanvasGroup>();
        if (bannerCanvasGroup == null) bannerCanvasGroup = bannerImage.gameObject.AddComponent<CanvasGroup>();
      }
    }

    private void InitializeState()
    {
      // Ẩn thanh máu ban đầu
      if (bossHPCanvasGroup != null)
      {
        bossHPCanvasGroup.alpha = 0f;
        bossHPRoot.gameObject.SetActive(false);
      }

      // Ẩn nhóm tên ban đầu
      if (nameGroupCanvasGroup != null)
      {
        nameGroupCanvasGroup.alpha = 0f;
        nameGroupRoot.gameObject.SetActive(false);
      }

      // Đổi thanh fill sang màu xanh băng rực sáng
      if (hpFillImage != null)
      {
        hpFillImage.color = frostHpColor;
        hpFillImage.fillAmount = 1.0f;
      }

      isNameAnchoredOnBar = false;
    }

    /// <summary>
    /// Kích hoạt thanh máu Boss (chưa có tên) xuất hiện trên đỉnh màn hình
    /// </summary>
    public void ShowHealthBarOnly()
    {
      if (bossHPRoot == null) return;
      bossHPRoot.gameObject.SetActive(true);
      StartCoroutine(FadeInHealthBarRoutine());
    }

    private IEnumerator FadeInHealthBarRoutine()
    {
      if (bossHPCanvasGroup == null) yield break;

      float timer = 0f;
      while (timer < barFadeInDuration)
      {
        timer += Time.deltaTime;
        bossHPCanvasGroup.alpha = Mathf.Clamp01(timer / barFadeInDuration);
        yield return null;
      }
      bossHPCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Bắt đầu chuỗi diễn hoạt Tên trùm: Hiện ở giữa màn hình -> Banner mờ đi -> Tên bay lên đỉnh thanh máu và thu nhỏ
    /// </summary>
    public void PlayNameSequence()
    {
      StartCoroutine(NameIntroSequenceRoutine());
    }

    private IEnumerator NameIntroSequenceRoutine()
    {
      if (nameGroupRoot == null || bossNameText == null) yield break;

      // Đặt Banner xuất hiện ở nửa bên PHẢI màn hình theo cài đặt Inspector
      nameGroupRoot.anchoredPosition = bannerIntroAnchoredPos;
      nameGroupRoot.localScale = Vector3.one * bannerIntroScale;
      nameGroupRoot.gameObject.SetActive(true);

      if (bannerImage != null) bannerImage.gameObject.SetActive(true);
      if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;

      // 1. Banner & Tên hiện từ từ lên ở giữa màn hình (Fade In)
      float timer = 0f;
      while (timer < bannerFadeInDuration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / bannerFadeInDuration);
        if (nameGroupCanvasGroup != null) nameGroupCanvasGroup.alpha = t;
        yield return null;
      }
      if (nameGroupCanvasGroup != null) nameGroupCanvasGroup.alpha = 1f;

      // 2. Giữ nguyên ở giữa màn hình để người chơi kịp đọc tên và cảm nhận sự uy nghiêm
      if (bannerHoldDuration > 0f)
      {
        yield return new WaitForSeconds(bannerHoldDuration);
      }

      // 3. Banner dần biến mất, Tên trùm bay chính xác về phía thanh máu (dù thanh máu ở bất kỳ vị trí nào)
      Transform textTransform = bossNameText.transform;
      Vector3 startWorldPos = textTransform.position;
      Vector3 startScale = textTransform.localScale;
      Vector3 endScale = new Vector3(targetNameScale, targetNameScale, targetNameScale);

      Canvas rootCanvas = GetComponentInParent<Canvas>();
      float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

      Transform targetDest = nameTargetSlot != null ? (Transform)nameTargetSlot : (Transform)bossHPRoot;

      timer = 0f;
      while (timer < nameTransitionDuration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / nameTransitionDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        // Banner mờ dần
        if (bannerCanvasGroup != null)
        {
          bannerCanvasGroup.alpha = 1f - smoothT;
        }

        // Tính đích đến động theo vị trí thực tế của thanh máu + offset
        Vector3 currentTargetWorldPos = targetDest != null
          ? targetDest.position + new Vector3(nameOffsetOnBar.x * scaleFactor, nameOffsetOnBar.y * scaleFactor, 0f)
          : startWorldPos;

        textTransform.position = Vector3.Lerp(startWorldPos, currentTargetWorldPos, smoothT);
        textTransform.localScale = Vector3.Lerp(startScale, endScale, smoothT);

        yield return null;
      }

      if (bannerCanvasGroup != null)
      {
        bannerCanvasGroup.alpha = 0f;
        if (bannerImage != null) bannerImage.gameObject.SetActive(false);
      }

      // Reparent Tên vào làm con của thanh máu để khi thanh máu di chuyển hay rung thì Tên luôn đi theo!
      if (targetDest != null)
      {
        textTransform.SetParent(targetDest, true);
      }

      textTransform.localScale = endScale;
      isNameAnchoredOnBar = true;

      Debug.Log($"<color=cyan>[BossHUDController]</color> 👑 Boss Title sequence complete! Tên trùm đã neo chuẩn xác trên thanh máu tại {textTransform.position}");
    }

    /// <summary>
    /// Liên kết thanh máu với Boss Health để cập nhật sát thương thời gian thực
    /// </summary>
    public void BindBossHealth(Health bossHealth)
    {
      if (targetBossHealth != null)
      {
        targetBossHealth.onHealthChanged.RemoveListener(OnBossHealthChanged);
        targetBossHealth.onDeath.RemoveListener(OnBossDefeated);
      }

      targetBossHealth = bossHealth;

      if (targetBossHealth != null)
      {
        targetBossHealth.onHealthChanged.AddListener(OnBossHealthChanged);
        targetBossHealth.onDeath.AddListener(OnBossDefeated);

        OnBossHealthChanged(targetBossHealth.CurrentHealth, targetBossHealth.MaxHealth);
        Debug.Log($"<color=cyan>[BossHUDController]</color> 🩸 Đã liên kết Boss Health ({targetBossHealth.CurrentHealth}/{targetBossHealth.MaxHealth} HP)");
      }
    }

    private void OnBossHealthChanged(float current, float max)
    {
      if (hpFillImage == null || max <= 0f) return;

      float ratio = Mathf.Clamp01(current / max);
      StopCoroutine("SmoothFillRoutine");
      StartCoroutine(SmoothFillRoutine(ratio));
    }

    private IEnumerator SmoothFillRoutine(float targetRatio)
    {
      float startRatio = hpFillImage.fillAmount;
      float timer = 0f;
      float duration = 0.25f;

      while (timer < duration)
      {
        timer += Time.deltaTime;
        hpFillImage.fillAmount = Mathf.Lerp(startRatio, targetRatio, timer / duration);
        yield return null;
      }

      hpFillImage.fillAmount = targetRatio;
    }

    private void OnBossDefeated()
    {
      StartCoroutine(FadeOutOnDeathRoutine());
    }

    private IEnumerator FadeOutOnDeathRoutine()
    {
      yield return new WaitForSeconds(1.5f);

      float timer = 0f;
      float duration = 1.5f;

      while (timer < duration)
      {
        timer += Time.deltaTime;
        float alpha = 1f - (timer / duration);
        if (bossHPCanvasGroup != null) bossHPCanvasGroup.alpha = alpha;
        if (nameGroupCanvasGroup != null) nameGroupCanvasGroup.alpha = alpha;
        yield return null;
      }

      if (bossHPRoot != null) bossHPRoot.gameObject.SetActive(false);
      if (nameGroupRoot != null) nameGroupRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
      if (targetBossHealth != null)
      {
        targetBossHealth.onHealthChanged.RemoveListener(OnBossHealthChanged);
        targetBossHealth.onDeath.RemoveListener(OnBossDefeated);
      }
    }
  }
}
