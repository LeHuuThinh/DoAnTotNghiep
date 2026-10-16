using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện HUD của Player (Thanh Máu Souls-like có Ghost bar, Thanh Thể Lực, 
/// Cụm Ngôi Sao Năng Lượng Kỹ Năng và Avatar Chân Dung).
/// Gắn script này trực tiếp lên GameObject "PlayerHUD" trong Canvas.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
  [Header("=== TARGET REFERENCES ===")]
  [Tooltip("Tham chiếu tới Health của Player. Nếu để trống code sẽ tự động tìm theo Tag 'Player'.")]
  [SerializeField] private Health playerHealth;
  [Tooltip("Tham chiếu tới PlayerStats của Player. Nếu để trống code sẽ tự động tìm theo Tag 'Player'.")]
  [SerializeField] private PlayerStats playerStats;

  [Header("=== HEALTH BAR ===")]
  [Tooltip("Thanh máu chính (MainFill - Màu đỏ tươi, Fill Method = Horizontal)")]
  [SerializeField] private Image healthMainFill;
  [Tooltip("Thanh máu bóng mờ Souls-like (GhostFill - Màu vàng nhạt tụt từ từ sau khi dính đòn)")]
  [SerializeField] private Image healthGhostFill;
  [Tooltip("Thời gian trễ (giây) trước khi thanh Ghost bắt đầu tụt sau khi mất máu")]
  [SerializeField] private float ghostShrinkDelay = 0.5f;
  [Tooltip("Tốc độ tụt của thanh Ghost bar (đơn vị fraction/giây)")]
  [SerializeField] private float ghostShrinkSpeed = 0.85f;

  [Header("=== STAMINA BAR ===")]
  [Tooltip("Thanh thể lực (Fill - Màu cam/vàng, Fill Method = Horizontal)")]
  [SerializeField] private Image staminaFill;
  [Tooltip("Tốc độ chuyển động mượt mà của thanh thể lực")]
  [SerializeField] private float staminaSmoothSpeed = 18f;

  [Header("=== SPIRIT STARS (HỆ THỐNG SAO KỸ NĂNG) ===")]
  [Tooltip("Mảng chứa các FillIcon của Star_0, Star_1, Star_2... (Tự động tìm nếu để trống)")]
  [SerializeField] private Image[] spiritStarFills;
  [Tooltip("Tốc độ nạp đầy mượt mà của từng ngôi sao")]
  [SerializeField] private float starFillSmoothSpeed = 10f;

  [Header("=== AVATAR (TÙY CHỌN) ===")]
  [SerializeField] private Image avatarPortrait;

  // Bộ đếm thời gian trễ cho Ghost bar
  private float ghostDelayTimer = 0f;
  private float targetHealthPct = 1f;
  private float targetStaminaPct = 1f;

  private void Reset()
  {
    AutoFindUIComponents();
  }

  private void Awake()
  {
    AutoFindUIComponents();
  }

  private void Start()
  {
    FindAndInitializePlayer();
  }

  private void OnEnable()
  {
    SubscribeEvents();
  }

  private void OnDisable()
  {
    UnsubscribeEvents();
  }

  private void Update()
  {
    // Failsafe: Nếu chưa tìm thấy Player (ví dụ Dungeon sinh sau), tìm lại định kỳ
    if (playerHealth == null || playerStats == null)
    {
      FindAndInitializePlayer();
    }

    UpdateHealthBarVisual();
    UpdateStaminaBarVisual();
    UpdateSpiritStarsVisual();
  }

  // =========================================================================
  // TỰ ĐỘNG DÒ TÌM COMPONENT THEO CẤU TRÚC HIERARCHY CỦA BẠN
  // =========================================================================
  [ContextMenu("Auto Find UI Components")]
  public void AutoFindUIComponents()
  {
    // 1. Tìm Health Bar
    if (healthMainFill == null)
    {
      Transform t = transform.Find("Bars_Container/HealthBar/MainFill");
      if (t != null) healthMainFill = t.GetComponent<Image>();
    }

    if (healthGhostFill == null)
    {
      Transform t = transform.Find("Bars_Container/HealthBar/GhostFill");
      if (t != null) healthGhostFill = t.GetComponent<Image>();
    }

    // 2. Tìm Stamina Bar
    if (staminaFill == null)
    {
      Transform t = transform.Find("Bars_Container/StaminaBar/Fill");
      if (t != null) staminaFill = t.GetComponent<Image>();
    }

    // 3. Tìm Avatar Portrait
    if (avatarPortrait == null)
    {
      Transform t = transform.Find("Avatar_Container/Avatar_Mask/Avatar_Portrait");
      if (t != null) avatarPortrait = t.GetComponent<Image>();
    }

    // 4. Tìm các FillIcon trong cụm Spirit_StarsGroup (Star_0, Star_1, Star_2...)
    if (spiritStarFills == null || spiritStarFills.Length == 0)
    {
      Transform starsGroup = transform.Find("Bars_Container/Spirit_StarsGroup");
      if (starsGroup != null)
      {
        List<Image> starList = new List<Image>();
        for (int i = 0; i < starsGroup.childCount; i++)
        {
          Transform starChild = starsGroup.GetChild(i);
          Transform fillIcon = starChild.Find("FillIcon");
          if (fillIcon != null)
          {
            Image img = fillIcon.GetComponent<Image>();
            if (img != null) starList.Add(img);
          }
        }

        if (starList.Count > 0)
        {
          spiritStarFills = starList.ToArray();
        }
      }
    }
  }

  // =========================================================================
  // LIÊN KẾT VỚI PLAYER
  // =========================================================================
  private void FindAndInitializePlayer()
  {
    if (playerHealth == null || playerStats == null)
    {
      GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj != null)
      {
        if (playerHealth == null) playerHealth = playerObj.GetComponentInChildren<Health>();
        if (playerStats == null) playerStats = playerObj.GetComponentInChildren<PlayerStats>();
      }
    }

    if (playerHealth != null && playerStats != null)
    {
      SubscribeEvents();
      // Khởi tạo các thanh về giá trị ban đầu ngay lập tức
      InitVisualsImmediate();
    }
  }

  private void SubscribeEvents()
  {
    if (playerHealth != null)
    {
      playerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
      playerHealth.onHealthChanged.AddListener(HandleHealthChanged);
    }

    if (playerStats != null)
    {
      playerStats.onStaminaChanged.RemoveListener(HandleStaminaChanged);
      playerStats.onStaminaChanged.AddListener(HandleStaminaChanged);

      playerStats.onSpiritChanged.RemoveListener(HandleSpiritChanged);
      playerStats.onSpiritChanged.AddListener(HandleSpiritChanged);
    }
  }

  private void UnsubscribeEvents()
  {
    if (playerHealth != null)
    {
      playerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
    }

    if (playerStats != null)
    {
      playerStats.onStaminaChanged.RemoveListener(HandleStaminaChanged);
      playerStats.onSpiritChanged.RemoveListener(HandleSpiritChanged);
    }
  }

  private void InitVisualsImmediate()
  {
    if (playerHealth != null && playerHealth.MaxHealth > 0f)
    {
      targetHealthPct = Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth);
      if (healthMainFill != null) healthMainFill.fillAmount = targetHealthPct;
      if (healthGhostFill != null) healthGhostFill.fillAmount = targetHealthPct;
    }

    if (playerStats != null && playerStats.MaxStamina > 0f)
    {
      targetStaminaPct = Mathf.Clamp01(playerStats.CurrentStamina / playerStats.MaxStamina);
      if (staminaFill != null) staminaFill.fillAmount = targetStaminaPct;
    }

    UpdateSpiritStarsVisual(instant: true);
  }

  // =========================================================================
  // XỬ LÝ THANH MÁU (SOULS-LIKE GHOST HEALTH BAR)
  // =========================================================================
  private void HandleHealthChanged(float current, float max)
  {
    if (max <= 0f) return;
    float newPct = Mathf.Clamp01(current / max);

    if (newPct < targetHealthPct)
    {
      // BỊ MẤT MÁU: Thanh chính tụt ngay lập tức, thanh Ghost bắt đầu đếm ngược thời gian trễ
      targetHealthPct = newPct;
      if (healthMainFill != null) healthMainFill.fillAmount = targetHealthPct;
      ghostDelayTimer = ghostShrinkDelay;
    }
    else
    {
      // HỒI MÁU: Cả 2 thanh cùng tăng ngay lập tức
      targetHealthPct = newPct;
      if (healthMainFill != null) healthMainFill.fillAmount = targetHealthPct;
      if (healthGhostFill != null) healthGhostFill.fillAmount = targetHealthPct;
      ghostDelayTimer = 0f;
    }
  }

  private void UpdateHealthBarVisual()
  {
    if (healthMainFill == null || healthGhostFill == null) return;

    // Sau khi hết thời gian trễ -> Thanh Ghost mượt mà tụt đuổi theo thanh chính
    if (ghostDelayTimer > 0f)
    {
      ghostDelayTimer -= Time.deltaTime;
    }
    else
    {
      if (healthGhostFill.fillAmount > healthMainFill.fillAmount)
      {
        healthGhostFill.fillAmount = Mathf.MoveTowards(
            healthGhostFill.fillAmount,
            healthMainFill.fillAmount,
            ghostShrinkSpeed * Time.deltaTime
        );
      }
      else
      {
        healthGhostFill.fillAmount = healthMainFill.fillAmount;
      }
    }
  }

  // =========================================================================
  // XỬ LÝ THANH THỂ LỰC (STAMINA)
  // =========================================================================
  private void HandleStaminaChanged(float current, float max)
  {
    if (max <= 0f) return;
    targetStaminaPct = Mathf.Clamp01(current / max);
  }

  private void UpdateStaminaBarVisual()
  {
    if (staminaFill == null) return;

    staminaFill.fillAmount = Mathf.MoveTowards(
        staminaFill.fillAmount,
        targetStaminaPct,
        staminaSmoothSpeed * Time.deltaTime
    );
  }

  // =========================================================================
  // XỬ LÝ CỤM NGÔI SAO NĂNG LƯỢNG KỸ NĂNG (SPIRIT STARS)
  // =========================================================================
  private void HandleSpiritChanged(float current, float max)
  {
    // Cập nhật trạng thái sao
  }

  private void UpdateSpiritStarsVisual(bool instant = false)
  {
    if (spiritStarFills == null || spiritStarFills.Length == 0 || playerStats == null) return;

    int starCount = spiritStarFills.Length; // Thường là 3 ngôi sao
    float maxSpirit = playerStats.MaxSpirit > 0f ? playerStats.MaxSpirit : 150f;
    float currentSpirit = Mathf.Clamp(playerStats.CurrentSpirit, 0f, maxSpirit);

    // Mỗi ngôi sao tương ứng với một phân đoạn năng lượng (Ví dụ: 150 / 3 = 50 điểm mỗi sao)
    float spiritPerStar = maxSpirit / starCount;

    for (int i = 0; i < starCount; i++)
    {
      Image starFill = spiritStarFills[i];
      if (starFill == null) continue;

      // Năng lượng rơi vào ngôi sao thứ i
      float starMinSpirit = i * spiritPerStar;
      float starMaxSpirit = (i + 1) * spiritPerStar;

      float starTargetFill = 0f;

      if (currentSpirit >= starMaxSpirit)
      {
        starTargetFill = 1f; // Đã tích đủ năng lượng cho ngôi sao này
      }
      else if (currentSpirit <= starMinSpirit)
      {
        starTargetFill = 0f; // Chưa tích lũy tới ngôi sao này
      }
      else
      {
        // Đang tích lũy dở dang
        starTargetFill = (currentSpirit - starMinSpirit) / spiritPerStar;
      }

      if (instant)
      {
        starFill.fillAmount = starTargetFill;
      }
      else
      {
        starFill.fillAmount = Mathf.MoveTowards(
            starFill.fillAmount,
            starTargetFill,
            starFillSmoothSpeed * Time.deltaTime
        );
      }
    }
  }
}
