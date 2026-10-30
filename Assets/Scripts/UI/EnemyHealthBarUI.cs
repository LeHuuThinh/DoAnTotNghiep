using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteLabyrinth.UI
{
  public enum BillboardMode
  {
    CameraAligned,    // Vuông góc 100% với màn hình Camera (Chuẩn Souls-like, đẹp nhất)
    CameraLookAt,     // Hướng thẳng vào vị trí Camera
    PlayerPosition    // Hướng vào vị trí mô hình Player
  }

  /// <summary>
  /// Quản lý Thanh Máu World Space trên đầu quái vật hầm ngục:
  /// - Billboard xoay mượt mà theo góc nhìn Camera/Player.
  /// - Thanh máu chính (Main Fill) đỏ tươi Souls-like (tô màu từ sprite grayscale Monster_Fill).
  /// - Thanh bóng ma (Ghost Bar) màu vàng nhạt tụt chậm sau một khoảng trễ khi quái dính đòn.
  /// - Tự động ẩn khi 100% máu hoặc ngoài tầm mắt để tối ưu hóa hiệu năng và tránh rác màn hình.
  /// - Tắt Canvas khi quái ngủ đông (DungeonEnemyOptimizer) hoặc khi alpha = 0 để đạt 0 Draw Calls.
  /// </summary>
  public class EnemyHealthBarUI : MonoBehaviour
  {
    [Header("=== THAM CHIẾU UI COMPONENTS ===")]
    [Tooltip("Canvas World Space chứa thanh máu")]
    [SerializeField] private Canvas worldSpaceCanvas;
    [Tooltip("CanvasGroup điều khiển độ trong suốt Fade In / Out")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Ảnh viền ngoài trang trí (Monster_Border)")]
    [SerializeField] private Image borderImage;
    [Tooltip("Ảnh nền tối phía dưới thanh máu")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Thanh vàng tụt chậm Souls-like (Ghost Bar)")]
    [SerializeField] private Image ghostFillImage;
    [Tooltip("Thanh máu chính hiển thị tức thời (Main Fill)")]
    [SerializeField] private Image mainFillImage;

    [Header("=== CÀI ĐẶT MÀU SẮC ===")]
    [Tooltip("Màu thanh máu chính (được tint vào sprite grayscale)")]
    [SerializeField] private Color mainFillColor = new Color(0.85f, 0.16f, 0.16f, 1f);
    [Tooltip("Màu thanh Ghost Bar tụt chậm phía sau")]
    [SerializeField] private Color ghostFillColor = new Color(0.96f, 0.74f, 0.22f, 0.9f);
    [Tooltip("Màu nền tối phía sau thanh máu")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

    [Header("=== BILLBOARD & VỊ TRÍ ===")]
    [Tooltip("Chế độ xoay Billboard")]
    [SerializeField] private BillboardMode billboardMode = BillboardMode.CameraAligned;
    [Tooltip("Khóa trục Y để thanh máu luôn thẳng đứng không bị chúc lên/xuống")]
    [SerializeField] private bool lockYAxis = false;
    [Tooltip("Độ cao trên đầu quái vật (mét)")]
    [SerializeField] private float heightOffset = 2.15f;

    [Header("=== DIỄN HOẠT GHOST BAR ===")]
    [Tooltip("Thời gian trễ trước khi Ghost Bar bắt đầu tụt (giây)")]
    [SerializeField] private float ghostShrinkDelay = 0.45f;
    [Tooltip("Tốc độ tụt của Ghost Bar")]
    [SerializeField] private float ghostShrinkSpeed = 1.6f;

    [Header("=== HIỂN THỊ THÔNG MINH & TỐI ƯU HIỆU NĂNG ===")]
    [Tooltip("Ẩn thanh máu khi quái đang 100% máu và chưa vào chiến đấu")]
    [SerializeField] private bool hideWhenFullHealth = true;
    [Tooltip("Khoảng cách tối đa nhìn thấy thanh máu (mét)")]
    [SerializeField] private float maxVisibleDistance = 24f;
    [Tooltip("Tốc độ chuyển đổi Fade In / Fade Out")]
    [SerializeField] private float fadeSpeed = 6f;
    [Tooltip("Thời gian giữ thanh máu hiển thị sau khi quái nhận đòn gần nhất (giây)")]
    [SerializeField] private float visibleDurationAfterHit = 5f;

    // Runtime Cached References
    private Health targetHealth;
    private EnemyAIBase targetEnemyAI;
    private Transform cameraTransform;
    private Transform playerTransform;

    private float targetFill = 1f;
    private float ghostFill = 1f;
    private float ghostDelayTimer = 0f;
    private float lastHitTimer = 0f;
    private bool isDying = false;

    private void Awake()
    {
      ResolveReferences();
      ApplyColors();
    }

    private void Start()
    {
      CacheCameraAndPlayer();

      if (targetHealth != null)
      {
        float cur = targetHealth.CurrentHealth;
        float max = Mathf.Max(1f, targetHealth.MaxHealth);
        targetFill = cur / max;
        ghostFill = targetFill;

        if (mainFillImage != null) mainFillImage.fillAmount = targetFill;
        if (ghostFillImage != null) ghostFillImage.fillAmount = ghostFill;

        // Nếu mới khởi đầu và quái 100% máu -> ẩn thanh máu cho gọn
        if (hideWhenFullHealth && cur >= max)
        {
          if (canvasGroup != null) canvasGroup.alpha = 0f;
          SetCanvasActive(false);
        }
      }
    }

    private void OnEnable()
    {
      if (targetHealth == null)
      {
        targetHealth = GetComponentInParent<Health>();
      }

      if (targetHealth != null)
      {
        targetHealth.onHealthChanged.AddListener(OnHealthChanged);
        targetHealth.onDeath.AddListener(OnDeath);
      }
    }

    private void OnDisable()
    {
      if (targetHealth != null)
      {
        targetHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        targetHealth.onDeath.RemoveListener(OnDeath);
      }
    }

    private void LateUpdate()
    {
      if (isDying && canvasGroup != null && canvasGroup.alpha <= 0.01f)
      {
        SetCanvasActive(false);
        return;
      }

      // 1. Kiểm tra tối ưu ngủ đông (DungeonEnemyOptimizer)
      if (targetEnemyAI != null && targetEnemyAI.IsSleeping)
      {
        SetCanvasActive(false);
        return;
      }

      // 2. Kiểm tra khoảng cách tới Camera
      if (cameraTransform == null)
      {
        CacheCameraAndPlayer();
        if (cameraTransform == null) return;
      }

      float distToCamSqr = (transform.position - cameraTransform.position).sqrMagnitude;
      if (distToCamSqr > maxVisibleDistance * maxVisibleDistance)
      {
        SetCanvasActive(false);
        return;
      }

      // 3. Thực hiện xoay Billboard
      UpdateBillboardRotation();

      // 4. Cập nhật diễn hoạt tụt Ghost Bar
      UpdateGhostBar();

      // 5. Cập nhật độ trong suốt hiển thị thông minh (Fade In/Out)
      UpdateVisibility();
    }

    /// <summary>
    /// Xoay thanh máu đối diện trực tiếp với người chơi / Camera
    /// </summary>
    private void UpdateBillboardRotation()
    {
      switch (billboardMode)
      {
        case BillboardMode.CameraAligned:
          // Xoay tuyệt đối song song với mặt phẳng Camera (Phẳng phiu, chuẩn Souls-like)
          if (lockYAxis)
          {
            Vector3 camEuler = cameraTransform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
          }
          else
          {
            transform.rotation = cameraTransform.rotation;
          }
          break;

        case BillboardMode.CameraLookAt:
          // Hướng tâm nhìn thẳng vào tọa độ Camera
          Vector3 dirToCam = cameraTransform.position - transform.position;
          if (lockYAxis) dirToCam.y = 0f;
          if (dirToCam.sqrMagnitude > 0.001f)
          {
            transform.rotation = Quaternion.LookRotation(-dirToCam);
          }
          break;

        case BillboardMode.PlayerPosition:
          // Hướng tâm nhìn vào mô hình Player
          if (playerTransform != null)
          {
            Vector3 dirToPlayer = playerTransform.position - transform.position;
            if (lockYAxis) dirToPlayer.y = 0f;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
              transform.rotation = Quaternion.LookRotation(-dirToPlayer);
            }
          }
          else
          {
            transform.rotation = cameraTransform.rotation;
          }
          break;
      }
    }

    /// <summary>
    /// Diễn hoạt Ghost Bar co lại mượt mà sau thời gian trễ
    /// </summary>
    private void UpdateGhostBar()
    {
      if (ghostFill > targetFill)
      {
        if (ghostDelayTimer > 0f)
        {
          ghostDelayTimer -= Time.deltaTime;
        }
        else
        {
          ghostFill = Mathf.MoveTowards(ghostFill, targetFill, ghostShrinkSpeed * Time.deltaTime);
          if (ghostFillImage != null)
          {
            ghostFillImage.fillAmount = ghostFill;
          }
        }
      }
      else
      {
        ghostFill = targetFill;
        if (ghostFillImage != null)
        {
          ghostFillImage.fillAmount = ghostFill;
        }
      }
    }

    /// <summary>
    /// Điều khiển hiển thị Fade In / Fade Out thông minh
    /// </summary>
    private void UpdateVisibility()
    {
      if (canvasGroup == null) return;

      if (isDying)
      {
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
        return;
      }

      if (lastHitTimer > 0f)
      {
        lastHitTimer -= Time.deltaTime;
      }

      bool shouldShow = true;

      if (hideWhenFullHealth)
      {
        bool isFullHealth = (targetFill >= 0.999f);
        bool inCombat = (targetEnemyAI != null && 
          (targetEnemyAI.CurrentState == EnemyAIBase.AIState.Chase || 
           targetEnemyAI.CurrentState == EnemyAIBase.AIState.Circling || 
           targetEnemyAI.CurrentState == EnemyAIBase.AIState.Attack));

        bool recentlyDamaged = (lastHitTimer > 0f);

        // Ẩn khi còn nguyên 100% máu và chưa bị đánh trúng / chưa phát hiện Player
        if (isFullHealth && !inCombat && !recentlyDamaged)
        {
          shouldShow = false;
        }
      }

      float targetAlpha = shouldShow ? 1f : 0f;

      if (targetAlpha > 0f && !worldSpaceCanvas.enabled)
      {
        SetCanvasActive(true);
      }

      canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

      if (canvasGroup.alpha <= 0.001f && targetAlpha == 0f)
      {
        SetCanvasActive(false);
      }
    }

    private void OnHealthChanged(float current, float max)
    {
      max = Mathf.Max(1f, max);
      float newFill = Mathf.Clamp01(current / max);

      if (newFill < targetFill)
      {
        // Nhận sát thương -> Đặt lại thời gian trễ cho Ghost Bar
        ghostDelayTimer = ghostShrinkDelay;
        lastHitTimer = visibleDurationAfterHit;
      }
      else
      {
        // Hồi máu -> Tăng cả hai ngay lập tức
        ghostFill = newFill;
        if (ghostFillImage != null) ghostFillImage.fillAmount = ghostFill;
      }

      targetFill = newFill;
      if (mainFillImage != null)
      {
        mainFillImage.fillAmount = targetFill;
      }

      SetCanvasActive(true);
    }

    private void OnDeath()
    {
      isDying = true;
      targetFill = 0f;
      if (mainFillImage != null) mainFillImage.fillAmount = 0f;
    }

    private void SetCanvasActive(bool active)
    {
      if (worldSpaceCanvas != null && worldSpaceCanvas.enabled != active)
      {
        worldSpaceCanvas.enabled = active;
      }
    }

    private void ResolveReferences()
    {
      if (worldSpaceCanvas == null) worldSpaceCanvas = GetComponentInChildren<Canvas>(true);
      if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
      if (targetHealth == null) targetHealth = GetComponentInParent<Health>();
      if (targetEnemyAI == null) targetEnemyAI = GetComponentInParent<EnemyAIBase>();
    }

    private void CacheCameraAndPlayer()
    {
      if (cameraTransform == null && Camera.main != null)
      {
        cameraTransform = Camera.main.transform;
      }

      if (playerTransform == null)
      {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
          playerTransform = playerObj.transform;
        }
      }
    }

    public void ApplyColors()
    {
      if (mainFillImage != null) mainFillImage.color = mainFillColor;
      if (ghostFillImage != null) ghostFillImage.color = ghostFillColor;
      if (backgroundImage != null) backgroundImage.color = backgroundColor;
    }

    public void SetupCustomReferences(
      Canvas canvas, 
      CanvasGroup group, 
      Image border, 
      Image bg, 
      Image ghost, 
      Image main,
      float offset)
    {
      worldSpaceCanvas = canvas;
      canvasGroup = group;
      borderImage = border;
      backgroundImage = bg;
      ghostFillImage = ghost;
      mainFillImage = main;
      heightOffset = offset;
      ApplyColors();
    }
  }
}
