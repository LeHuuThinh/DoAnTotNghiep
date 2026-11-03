using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện hiển thị 3 kỹ năng (Skill E, Q, R) trong cụm SkillIconUI.
/// Điều khiển Cooldown (CDFill), Độ mờ Icon (Alpha 1.0 vs 0.1) và Viền góc (Corner) theo năng lượng Spirit thời gian thực.
/// </summary>
public class SkillIconUI : MonoBehaviour
{
  [System.Serializable]
  public class SkillSlot
  {
    public string slotName = "Skill";
    [Tooltip("Hình ảnh Icon chiêu thức chính")]
    public Image iconImage;
    [Tooltip("Viền góc Corner (Chỉ sáng khi đủ Spirit)")]
    public GameObject cornerObject;
    [Tooltip("Lớp phủ Cooldown (Image Type = Filled)")]
    public Image cdFillImage;
    [Tooltip("Text đếm ngược số giây CD (Tùy chọn, để trống nếu không dùng)")]
    public TextMeshProUGUI cdText;
  }

  [Header("=== TARGET REFERENCES ===")]
  [Tooltip("Tham chiếu PlayerStats. Tự động tìm nếu để trống.")]
  [SerializeField] private PlayerStats playerStats;
  [Tooltip("Tham chiếu PlayerCombat. Tự động tìm nếu để trống.")]
  [SerializeField] private PlayerCombat playerCombat;

  [Header("=== SKILL SLOTS ===")]
  [SerializeField] private SkillSlot slotE = new SkillSlot { slotName = "Skill E (Slash)" };
  [SerializeField] private SkillSlot slotQ = new SkillSlot { slotName = "Skill Q (AOE Slam)" };
  [SerializeField] private SkillSlot slotR = new SkillSlot { slotName = "Skill R (Ultimate)" };

  [Header("=== VISUAL SETTINGS ===")]
  [Tooltip("Độ trong suốt của Icon khi ĐỦ Spirit và KHÔNG dính CD (Sẵn sàng xuất chiêu)")]
  [Range(0f, 1f)]
  [SerializeField] private float readyIconAlpha = 1.0f;

  [Tooltip("Độ trong suốt của Icon khi THIẾU Spirit hoặc ĐANG dính CD (Không thể xuất chiêu)")]
  [Range(0f, 1f)]
  [SerializeField] private float disabledIconAlpha = 0.1f;

  private void Reset()
  {
    AutoFindComponents();
  }

  private void Awake()
  {
    AutoFindComponents();
  }

  private void Start()
  {
    FindPlayerReferences();
  }

  private void Update()
  {
    if (playerStats == null || playerCombat == null)
    {
      FindPlayerReferences();
      if (playerStats == null || playerCombat == null) return;
    }

    float currentSpirit = playerStats.CurrentSpirit;

    // 1. Cập nhật Skill E
    UpdateSlot(slotE, currentSpirit, playerStats.SkillECost, playerCombat.SkillETimer, playerCombat.SkillECooldown);

    // 2. Cập nhật Skill Q
    UpdateSlot(slotQ, currentSpirit, playerStats.SkillQCost, playerCombat.SkillQTimer, playerCombat.SkillQCooldown);

    // 3. Cập nhật Skill R
    UpdateSlot(slotR, currentSpirit, playerStats.SkillRCost, playerCombat.SkillRTimer, playerCombat.SkillRCooldown);
  }

  /// <summary>
  /// Cập nhật trạng thái hiển thị của một ô chiêu thức theo đúng ma trận logic:
  /// - Đủ Spirit & Không CD  -> Icon Alpha 1.0 | Corner BẬT | CDFill = 0
  /// - Đủ Spirit & Đang CD   -> Icon Alpha 0.1 | Corner BẬT | CDFill chạy (1 -> 0)
  /// - Thiếu Spirit & Không CD -> Icon Alpha 0.1 | Corner TẮT | CDFill = 0
  /// - Thiếu Spirit & Đang CD  -> Icon Alpha 0.1 | Corner TẮT | CDFill chạy (1 -> 0)
  /// </summary>
  private void UpdateSlot(SkillSlot slot, float currentSpirit, float skillCost, float cdTimer, float maxCd)
  {
    if (slot == null) return;

    bool hasSpirit = currentSpirit >= skillCost;
    bool isOnCooldown = cdTimer > 0.001f && maxCd > 0f;
    bool canCast = hasSpirit && !isOnCooldown;

    // 1. Corner: Phụ thuộc DUY NHẤT vào việc có đủ Spirit hay không
    if (slot.cornerObject != null)
    {
      if (slot.cornerObject.activeSelf != hasSpirit)
      {
        slot.cornerObject.SetActive(hasSpirit);
      }
    }

    // 2. Icon Alpha: 1.0 khi có thể thi triển ngay (Đủ Spirit & Hết CD), 0.1 cho mọi trường hợp còn lại
    if (slot.iconImage != null)
    {
      Color c = slot.iconImage.color;
      float targetAlpha = canCast ? readyIconAlpha : disabledIconAlpha;
      if (!Mathf.Approximately(c.a, targetAlpha))
      {
        c.a = targetAlpha;
        slot.iconImage.color = c;
      }
    }

    // 3. CDFill: Minh họa thanh thời gian hồi chiêu
    if (slot.cdFillImage != null)
    {
      if (isOnCooldown)
      {
        if (!slot.cdFillImage.gameObject.activeSelf)
        {
          slot.cdFillImage.gameObject.SetActive(true);
        }
        float fill = Mathf.Clamp01(cdTimer / maxCd);
        slot.cdFillImage.fillAmount = fill;
      }
      else
      {
        if (slot.cdFillImage.fillAmount > 0f)
        {
          slot.cdFillImage.fillAmount = 0f;
        }
        if (slot.cdFillImage.gameObject.activeSelf)
        {
          slot.cdFillImage.gameObject.SetActive(false);
        }
      }
    }

    // 4. CD Text đếm ngược (Tùy chọn)
    if (slot.cdText != null)
    {
      if (isOnCooldown)
      {
        if (!slot.cdText.gameObject.activeSelf) slot.cdText.gameObject.SetActive(true);
        slot.cdText.text = cdTimer >= 1.0f ? $"{Mathf.CeilToInt(cdTimer)}" : $"{cdTimer:0.0}";
      }
      else
      {
        if (slot.cdText.gameObject.activeSelf) slot.cdText.gameObject.SetActive(false);
      }
    }
  }

  /// <summary>
  /// Tự động tìm kiếm các GameObject Icon, Corner, CDFill theo đúng phân cấp trong Canvas
  /// </summary>
  [ContextMenu("Auto Find UI Components")]
  public void AutoFindComponents()
  {
    BindSlot(transform.Find("SkillE"), ref slotE, "Skill E (Slash)");
    BindSlot(transform.Find("SkillQ"), ref slotQ, "Skill Q (AOE Slam)");
    BindSlot(transform.Find("SkillR"), ref slotR, "Skill R (Ultimate)");
  }

  private void BindSlot(Transform root, ref SkillSlot slot, string defaultName)
  {
    if (root == null) return;
    if (slot == null) slot = new SkillSlot();
    slot.slotName = defaultName;

    if (slot.iconImage == null)
    {
      Transform iconT = root.Find("Icon");
      if (iconT != null) slot.iconImage = iconT.GetComponent<Image>();
    }

    if (slot.cornerObject == null)
    {
      Transform cornerT = root.Find("Corner");
      if (cornerT != null) slot.cornerObject = cornerT.gameObject;
    }

    if (slot.cdFillImage == null)
    {
      Transform cdT = root.Find("CDFill");
      if (cdT != null) slot.cdFillImage = cdT.GetComponent<Image>();
    }

    if (slot.cdText == null)
    {
      Transform textT = root.Find("CDText");
      if (textT != null) slot.cdText = textT.GetComponent<TextMeshProUGUI>();
    }
  }

  private void FindPlayerReferences()
  {
    if (playerStats == null)
    {
      GameObject player = GameObject.FindWithTag("Player");
      if (player != null)
      {
        playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = player.GetComponentInChildren<PlayerStats>();
      }
      if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
    }

    if (playerCombat == null)
    {
      GameObject player = GameObject.FindWithTag("Player");
      if (player != null)
      {
        playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat == null) playerCombat = player.GetComponentInChildren<PlayerCombat>();
      }
      if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
    }
  }
}
