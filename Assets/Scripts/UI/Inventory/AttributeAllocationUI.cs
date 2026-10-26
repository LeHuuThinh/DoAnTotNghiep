using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Progression;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý Modal giao diện Phân Bổ Điểm Tiềm Năng (RPG-05).
  /// Cho phép người chơi tăng/giảm điểm xem trước (Preview), Xác nhận lưu điểm vĩnh viễn,
  /// hoặc Tẩy điểm tiềm năng với chi phí X Vàng (cấu hình trong PlayerProgression Inspector).
  /// </summary>
  public class AttributeAllocationUI : MonoBehaviour
  {
    public static AttributeAllocationUI Instance { get; private set; }

    [Header("=== GIAO DIỆN CHÍNH (MODAL ROOT) ===")]
    [Tooltip("Panel gốc hoặc CanvasGroup của Modal")]
    [SerializeField] private CanvasGroup modalCanvasGroup;
    [Tooltip("GameObject gốc của Modal (dùng để SetActive nếu không dùng CanvasGroup)")]
    [SerializeField] private GameObject modalRoot;
    [Tooltip("Nút đóng [X] ở góc trên-phải")]
    [SerializeField] private Button closeButton;

    [Header("=== HIỂN THỊ ĐIỂM KHẢ DỤNG ===")]
    [Tooltip("Text hiển thị số điểm chưa dùng")]
    [SerializeField] private TextMeshProUGUI availablePointsText;

    [Header("=== HÀNG 1: SỨC TẤN CÔNG (ATK) ===")]
    [Tooltip("Placeholder Icon cho Sức Tấn Công")]
    [SerializeField] private Image atkIcon;
    [Tooltip("Text hiển thị chỉ số ATK hiện tại và xem trước")]
    [SerializeField] private TextMeshProUGUI atkValueText;
    [Tooltip("Text hiển thị số điểm đang cộng tạm (+X)")]
    [SerializeField] private TextMeshProUGUI atkPointsAddedText;
    [Tooltip("Nút giảm điểm tạm thời [-]")]
    [SerializeField] private Button atkMinusButton;
    [Tooltip("Nút tăng điểm tạm thời [+]")]
    [SerializeField] private Button atkPlusButton;

    [Header("=== HÀNG 2: MÁU TỐI ĐA (HP) ===")]
    [Tooltip("Placeholder Icon cho Máu Tối Đa")]
    [SerializeField] private Image hpIcon;
    [Tooltip("Text hiển thị chỉ số HP hiện tại và xem trước")]
    [SerializeField] private TextMeshProUGUI hpValueText;
    [Tooltip("Text hiển thị số điểm đang cộng tạm (+X)")]
    [SerializeField] private TextMeshProUGUI hpPointsAddedText;
    [Tooltip("Nút giảm điểm tạm thời [-]")]
    [SerializeField] private Button hpMinusButton;
    [Tooltip("Nút tăng điểm tạm thời [+]")]
    [SerializeField] private Button hpPlusButton;

    [Header("=== HÀNG 3: THỂ LỰC TỐI ĐA (STAMINA) ===")]
    [Tooltip("Placeholder Icon cho Thể Lực Tối Đa")]
    [SerializeField] private Image staminaIcon;
    [Tooltip("Text hiển thị chỉ số Stamina hiện tại và xem trước")]
    [SerializeField] private TextMeshProUGUI staminaValueText;
    [Tooltip("Text hiển thị số điểm đang cộng tạm (+X)")]
    [SerializeField] private TextMeshProUGUI staminaPointsAddedText;
    [Tooltip("Nút giảm điểm tạm thời [-]")]
    [SerializeField] private Button staminaMinusButton;
    [Tooltip("Nút tăng điểm tạm thời [+]")]
    [SerializeField] private Button staminaPlusButton;

    [Header("=== CÁC NÚT THAO TÁC (FOOTER) ===")]
    [Tooltip("Nút Xác Nhận chính thức lưu điểm")]
    [SerializeField] private Button confirmButton;
    [Tooltip("Nút Hủy Bỏ các điểm vừa chọn")]
    [SerializeField] private Button cancelButton;
    [Tooltip("Nút Tẩy Điểm Tiềm Năng (Tốn X Vàng)")]
    [SerializeField] private Button resetStatsButton;
    [Tooltip("Text hiển thị trên nút Tẩy Điểm (Ví dụ: 'Reset All')")]
    [SerializeField] private TextMeshProUGUI resetStatsButtonText;
    [Tooltip("Text hiển thị giá vàng để tẩy điểm (ResetPrice/Text)")]
    [SerializeField] private TextMeshProUGUI resetPriceText;
    [Tooltip("Text hiển thị thông báo trạng thái / phản hồi (Ví dụ: 'Không đủ vàng!')")]
    [SerializeField] private TextMeshProUGUI statusMessageText;

    // Điểm cộng tạm thời trong phiên mở bảng
    private int tempAtk = 0;
    private int tempHp = 0;
    private int tempStamina = 0;

    private int TotalTempAdded => tempAtk + tempHp + tempStamina;
    private int RemainingPoints
    {
      get
      {
        if (!PlayerProgression.HasInstance) return 0;
        return Mathf.Max(0, PlayerProgression.Instance.UnallocatedPoints - TotalTempAdded);
      }
    }

    public bool IsVisible => (modalCanvasGroup != null && modalCanvasGroup.alpha > 0.05f) ||
                             (modalRoot != null && modalRoot.activeSelf);

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      if (modalRoot == null)
      {
        modalRoot = gameObject;
      }

      if (modalCanvasGroup == null)
      {
        modalCanvasGroup = GetComponent<CanvasGroup>();
      }

      // Tự động dò tìm các GameObject và Component theo cấu trúc Canvas
      if (confirmButton == null || atkPlusButton == null)
      {
        AutoFindUIComponents();
      }

      SetupButtonListeners();
    }

    [ContextMenu("Auto Find UI Components")]
    public void AutoFindUIComponents()
    {
      if (modalRoot == null) modalRoot = gameObject;
      if (modalCanvasGroup == null) modalCanvasGroup = GetComponent<CanvasGroup>();

      // Available Points
      if (availablePointsText == null)
      {
        Transform t = transform.Find("Available Points/Points");
        if (t == null) t = transform.Find("Available Points/Text");
        if (t == null) t = transform.Find("AvailablePointsText");
        if (t != null) availablePointsText = t.GetComponent<TextMeshProUGUI>();
      }

      // Status Msg
      if (statusMessageText == null)
      {
        Transform t = transform.Find("AttributePanel/StatusMsg/Text");
        if (t == null) t = transform.Find("StatusMessageText");
        if (t != null) statusMessageText = t.GetComponent<TextMeshProUGUI>();
      }

      // Footer Buttons
      if (confirmButton == null)
      {
        Transform t = transform.Find("FooterRow/BtnConfirm");
        if (t == null) t = transform.Find("Footer/BtnConfirm");
        if (t != null) confirmButton = t.GetComponent<Button>();
      }

      if (cancelButton == null)
      {
        Transform t = transform.Find("FooterRow/BtnCancel");
        if (t == null) t = transform.Find("Footer/BtnCancel");
        if (t != null) cancelButton = t.GetComponent<Button>();
      }

      if (resetStatsButton == null)
      {
        Transform t = transform.Find("FooterRow/BtnResetPoints");
        if (t == null) t = transform.Find("FooterRow/BtnResetStats");
        if (t == null) t = transform.Find("Footer/BtnResetStats");
        if (t != null) resetStatsButton = t.GetComponent<Button>();
      }

      if (resetPriceText == null)
      {
        Transform t = transform.Find("FooterRow/ResetPrice/Text");
        if (t != null) resetPriceText = t.GetComponent<TextMeshProUGUI>();
      }

      if (resetStatsButtonText == null && resetStatsButton != null)
      {
        resetStatsButtonText = resetStatsButton.GetComponentInChildren<TextMeshProUGUI>();
      }

      // Hàng Attack
      AutoFindRow("AttributePanel/RowContainer/Attack", ref atkIcon, ref atkValueText, ref atkPointsAddedText, ref atkMinusButton, ref atkPlusButton);
      if (atkValueText == null) AutoFindRow("RowsContainer/Row_ATK", ref atkIcon, ref atkValueText, ref atkPointsAddedText, ref atkMinusButton, ref atkPlusButton);

      // Hàng Health
      AutoFindRow("AttributePanel/RowContainer/Health", ref hpIcon, ref hpValueText, ref hpPointsAddedText, ref hpMinusButton, ref hpPlusButton);
      if (hpValueText == null) AutoFindRow("RowsContainer/Row_HP", ref hpIcon, ref hpValueText, ref hpPointsAddedText, ref hpMinusButton, ref hpPlusButton);

      // Hàng Stamina
      AutoFindRow("AttributePanel/RowContainer/Stamina", ref staminaIcon, ref staminaValueText, ref staminaPointsAddedText, ref staminaMinusButton, ref staminaPlusButton);
      if (staminaValueText == null) AutoFindRow("RowsContainer/Row_Stamina", ref staminaIcon, ref staminaValueText, ref staminaPointsAddedText, ref staminaMinusButton, ref staminaPlusButton);
    }

    private void AutoFindRow(string rowPath, ref Image icon, ref TextMeshProUGUI valText, ref TextMeshProUGUI addedText, ref Button minusBtn, ref Button plusBtn)
    {
      Transform row = transform.Find(rowPath);
      if (row == null) return;

      if (icon == null)
      {
        Transform t = row.Find("Icon");
        if (t == null) t = row.Find("IconPlaceholder");
        if (t != null) icon = t.GetComponent<Image>();
      }
      if (valText == null)
      {
        Transform t = row.Find("PreviewValue");
        if (t == null) t = row.Find("ValueText");
        if (t != null) valText = t.GetComponent<TextMeshProUGUI>();
      }
      if (addedText == null)
      {
        Transform t = row.Find("PointAddedValue");
        if (t == null) t = row.Find("AddedPointsText");
        if (t != null) addedText = t.GetComponent<TextMeshProUGUI>();
      }
      if (minusBtn == null)
      {
        Transform t = row.Find("BtnMinus");
        if (t != null) minusBtn = t.GetComponent<Button>();
      }
      if (plusBtn == null)
      {
        Transform t = row.Find("BtnPlus");
        if (t != null) plusBtn = t.GetComponent<Button>();
      }
    }

    private void Start()
    {
      // Mặc định ẩn modal khi vào game
      HideImmediate();

      if (PlayerProgression.HasInstance)
      {
        PlayerProgression.Instance.OnPointsChanged += HandlePointsChanged;
        PlayerProgression.Instance.OnStatsAllocated += HandleStatsAllocated;
      }

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnGoldChanged += HandleGoldChanged;
      }
    }

    private void OnDestroy()
    {
      if (PlayerProgression.HasInstance)
      {
        PlayerProgression.Instance.OnPointsChanged -= HandlePointsChanged;
        PlayerProgression.Instance.OnStatsAllocated -= HandleStatsAllocated;
      }

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnGoldChanged -= HandleGoldChanged;
      }
    }

    private void HandlePointsChanged(int points)
    {
      if (IsVisible) RefreshUI();
    }

    private void HandleStatsAllocated()
    {
      if (IsVisible) RefreshUI();
    }

    private void HandleGoldChanged(int newGold)
    {
      if (IsVisible) RefreshResetButtonState();
    }

    private void SetupButtonListeners()
    {
      if (closeButton != null) closeButton.onClick.AddListener(Hide);
      if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancelClicked);
      if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirmClicked);
      if (resetStatsButton != null) resetStatsButton.onClick.AddListener(HandleResetStatsClicked);

      if (atkPlusButton != null) atkPlusButton.onClick.AddListener(() => ChangePoint(ref tempAtk, 1));
      if (atkMinusButton != null) atkMinusButton.onClick.AddListener(() => ChangePoint(ref tempAtk, -1));

      if (hpPlusButton != null) hpPlusButton.onClick.AddListener(() => ChangePoint(ref tempHp, 1));
      if (hpMinusButton != null) hpMinusButton.onClick.AddListener(() => ChangePoint(ref tempHp, -1));

      if (staminaPlusButton != null) staminaPlusButton.onClick.AddListener(() => ChangePoint(ref tempStamina, 1));
      if (staminaMinusButton != null) staminaMinusButton.onClick.AddListener(() => ChangePoint(ref tempStamina, -1));
    }

    private void ChangePoint(ref int pointVar, int delta)
    {
      if (delta > 0 && RemainingPoints <= 0) return;
      if (delta < 0 && pointVar <= 0) return;

      pointVar += delta;
      if (statusMessageText != null) statusMessageText.text = "";
      RefreshUI();
    }

    /// <summary>
    /// Mở Modal phân bổ điểm tiềm năng
    /// </summary>
    public void Show()
    {
      tempAtk = 0;
      tempHp = 0;
      tempStamina = 0;

      if (statusMessageText != null) statusMessageText.text = "";

      if (modalRoot != null) modalRoot.SetActive(true);

      if (modalCanvasGroup != null)
      {
        modalCanvasGroup.alpha = 1f;
        modalCanvasGroup.blocksRaycasts = true;
        modalCanvasGroup.interactable = true;
      }

      RefreshUI();
    }

    /// <summary>
    /// Đóng Modal
    /// </summary>
    public void Hide()
    {
      tempAtk = 0;
      tempHp = 0;
      tempStamina = 0;

      if (modalCanvasGroup != null)
      {
        modalCanvasGroup.alpha = 0f;
        modalCanvasGroup.blocksRaycasts = false;
        modalCanvasGroup.interactable = false;
      }

      if (modalRoot != null && modalCanvasGroup == null)
      {
        modalRoot.SetActive(false);
      }
    }

    public void HideImmediate()
    {
      tempAtk = 0;
      tempHp = 0;
      tempStamina = 0;

      if (modalCanvasGroup != null)
      {
        modalCanvasGroup.alpha = 0f;
        modalCanvasGroup.blocksRaycasts = false;
        modalCanvasGroup.interactable = false;
      }
      else if (modalRoot != null)
      {
        modalRoot.SetActive(false);
      }
    }

    public void Toggle()
    {
      if (IsVisible) Hide();
      else Show();
    }

    private void HandleCancelClicked()
    {
      if (TotalTempAdded > 0)
      {
        tempAtk = 0;
        tempHp = 0;
        tempStamina = 0;
        if (statusMessageText != null) statusMessageText.text = "<color=#B0BEC5>Đã hủy bỏ điểm cộng tạm thời.</color>";
        RefreshUI();
      }
      else
      {
        Hide();
      }
    }

    private void HandleConfirmClicked()
    {
      if (TotalTempAdded <= 0 || !PlayerProgression.HasInstance) return;

      int addedTotal = TotalTempAdded;
      bool success = PlayerProgression.Instance.AllocatePoints(tempAtk, tempHp, tempStamina);
      if (success)
      {
        tempAtk = 0;
        tempHp = 0;
        tempStamina = 0;
        if (statusMessageText != null)
        {
          statusMessageText.text = $"<color=#A5D6A7>✓ Đã phân bổ thành công +{addedTotal} điểm tiềm năng!</color>";
        }
        RefreshUI();
      }
      else
      {
        if (statusMessageText != null)
        {
          statusMessageText.text = "<color=#EF9A9A>✗ Phân bổ điểm không thành công, vui lòng kiểm tra lại!</color>";
        }
      }
    }

    private void HandleResetStatsClicked()
    {
      if (!PlayerProgression.HasInstance) return;

      if (!PlayerProgression.Instance.CanResetStats(out string reason))
      {
        if (statusMessageText != null)
        {
          statusMessageText.text = $"<color=#EF9A9A>✗ {reason}</color>";
        }
        return;
      }

      int pointsRefunded = PlayerProgression.Instance.TotalAllocatedPoints;
      bool success = PlayerProgression.Instance.ResetStats();
      if (success)
      {
        tempAtk = 0;
        tempHp = 0;
        tempStamina = 0;
        if (statusMessageText != null)
        {
          statusMessageText.text = $"<color=#A5D6A7>✓ Tẩy điểm thành công! Đã hoàn trả {pointsRefunded} điểm tiềm năng.</color>";
        }
        RefreshUI();
      }
    }

    /// <summary>
    /// Làm mới toàn bộ thông số trên giao diện
    /// </summary>
    public void RefreshUI()
    {
      var prog = PlayerProgression.Instance;
      if (prog == null) return;

      // 1. Điểm khả dụng
      if (availablePointsText != null)
      {
        int rem = RemainingPoints;
        if (availablePointsText.gameObject.name.Equals("Points", StringComparison.OrdinalIgnoreCase))
        {
          availablePointsText.text = rem.ToString();
        }
        else
        {
          string colorTag = rem > 0 ? "<color=#FFE082>" : "<color=#B0BEC5>";
          availablePointsText.text = $"Điểm khả dụng: {colorTag}<b>{rem}</b></color>";
        }
      }

      // Đọc chỉ số gốc của Player
      float currentBaseAtk = 15f;
      float currentBaseHp = 100f;
      float currentBaseStamina = 100f;

      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        var combat = playerObj.GetComponent<PlayerCombat>();
        if (combat != null) currentBaseAtk = combat.BaseAttackDamage;

        var health = playerObj.GetComponent<Health>();
        if (health != null) currentBaseHp = health.MaxHealth;

        var stats = playerObj.GetComponent<PlayerStats>();
        if (stats != null) currentBaseStamina = stats.MaxStamina;
      }

      // 2. Hàng Tấn công (ATK)
      if (atkPointsAddedText != null)
      {
        atkPointsAddedText.text = tempAtk > 0 ? $"+{tempAtk}" : "0";
      }
      if (atkValueText != null)
      {
        float bonusPerPoint = prog.AttackPerPoint;
        float currentTotalAtk = currentBaseAtk;
        float previewBonus = tempAtk * bonusPerPoint;
        if (tempAtk > 0)
        {
          atkValueText.text = $"{currentTotalAtk} <color=#A5D6A7>→ {currentTotalAtk + previewBonus:0.#} (+{previewBonus:0.#})</color>";
        }
        else
        {
          atkValueText.text = $"{currentTotalAtk:0.#}";
        }
      }
      if (atkMinusButton != null) atkMinusButton.interactable = tempAtk > 0;
      if (atkPlusButton != null) atkPlusButton.interactable = RemainingPoints > 0;

      // 3. Hàng Máu tối đa (HP)
      if (hpPointsAddedText != null)
      {
        hpPointsAddedText.text = tempHp > 0 ? $"+{tempHp}" : "0";
      }
      if (hpValueText != null)
      {
        float bonusPerPoint = prog.HealthPerPoint;
        float currentTotalHp = currentBaseHp;
        float previewBonus = tempHp * bonusPerPoint;
        if (tempHp > 0)
        {
          hpValueText.text = $"{currentTotalHp:0.#} <color=#A5D6A7>→ {currentTotalHp + previewBonus:0.#} (+{previewBonus:0.#})</color>";
        }
        else
        {
          hpValueText.text = $"{currentTotalHp:0.#}";
        }
      }
      if (hpMinusButton != null) hpMinusButton.interactable = tempHp > 0;
      if (hpPlusButton != null) hpPlusButton.interactable = RemainingPoints > 0;

      // 4. Hàng Thể lực tối đa (Stamina)
      if (staminaPointsAddedText != null)
      {
        staminaPointsAddedText.text = tempStamina > 0 ? $"+{tempStamina}" : "0";
      }
      if (staminaValueText != null)
      {
        float bonusPerPoint = prog.StaminaPerPoint;
        float currentTotalStam = currentBaseStamina;
        float previewBonus = tempStamina * bonusPerPoint;
        if (tempStamina > 0)
        {
          staminaValueText.text = $"{currentTotalStam:0.#} <color=#A5D6A7>→ {currentTotalStam + previewBonus:0.#} (+{previewBonus:0.#})</color>";
        }
        else
        {
          staminaValueText.text = $"{currentTotalStam:0.#}";
        }
      }
      if (staminaMinusButton != null) staminaMinusButton.interactable = tempStamina > 0;
      if (staminaPlusButton != null) staminaPlusButton.interactable = RemainingPoints > 0;

      // 5. Nút Xác Nhận & Hủy
      if (confirmButton != null) confirmButton.interactable = TotalTempAdded > 0;

      // 6. Nút Tẩy Điểm (Reset Stats)
      RefreshResetButtonState();
    }

    private void RefreshResetButtonState()
    {
      var prog = PlayerProgression.Instance;
      if (prog == null) return;

      int cost = prog.ResetCostGold;

      // Cập nhật text giá vàng nếu có ô ResetPrice riêng biệt
      if (resetPriceText != null)
      {
        resetPriceText.text = cost.ToString("N0");
      }

      if (resetStatsButtonText != null)
      {
        if (resetPriceText != null)
        {
          resetStatsButtonText.text = "Reset All";
        }
        else
        {
          resetStatsButtonText.text = $"Tẩy Điểm ({cost} Vàng)";
        }
      }

      if (resetStatsButton != null)
      {
        bool hasAllocated = prog.TotalAllocatedPoints > 0;
        bool hasEnoughGold = !InventoryManager.HasInstance || InventoryManager.Instance.CurrentGold >= cost;
        resetStatsButton.interactable = hasAllocated && hasEnoughGold;
      }
    }
  }
}
