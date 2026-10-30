using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Progression;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Hiển thị bảng chỉ số thời gian thực của nhân vật ở góc dưới cửa sổ bên trái
  /// </summary>
  public class PlayerStatsPanelUI : MonoBehaviour
  {
    [Header("=== TEXT HIỂN THỊ CHỈ SỐ (VALUE RIÊNG) ===")]
    [Tooltip("Text hiển thị giá trị số Tấn công (ATK Value)")]
    [SerializeField] private TextMeshProUGUI atkValueText;

    [Tooltip("Text hiển thị giá trị số Phòng thủ (DEF Value)")]
    [SerializeField] private TextMeshProUGUI defValueText;

    [Tooltip("Text hiển thị giá trị Máu (HP Value: Hiện tại / Tối đa)")]
    [SerializeField] private TextMeshProUGUI hpValueText;

    [Tooltip("Text hiển thị giá trị Thể lực (Stamina Value: Hiện tại / Tối đa)")]
    [SerializeField] private TextMeshProUGUI staminaValueText;

    [Header("=== LABEL HOẶC CONTAINER GỐC ===")]
    [Tooltip("Text hiển thị nhãn Tấn công (hoặc container AtkText)")]
    [SerializeField] private TextMeshProUGUI atkText;

    [Tooltip("Text hiển thị nhãn Phòng thủ (hoặc container DefText)")]
    [SerializeField] private TextMeshProUGUI defText;

    [Tooltip("Text hiển thị nhãn Máu (hoặc container HpText)")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Tooltip("Text hiển thị nhãn Thể lực (hoặc container StaminaText)")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("=== NÚT CỘNG ĐIỂM TIỀM NĂNG (RPG-05) ===")]
    [Tooltip("Nút mở modal phân bổ điểm thuộc tính")]
    [SerializeField] private Button attributePointsButton;

    [Tooltip("Text trên nút Cộng Điểm để hiển thị (+X) điểm khả dụng")]
    [SerializeField] private TextMeshProUGUI attributeButtonText;

    [Tooltip("Modal phân bổ điểm tiềm năng")]
    [SerializeField] private AttributeAllocationUI attributeAllocationModal;

    public event Action OnAttributePointsClicked;

    private Health cachedPlayerHealth;
    private PlayerStats cachedPlayerStats;
    private PlayerCombat cachedPlayerCombat;
    private string cachedAttributeButtonText;

    private void Awake()
    {
      AutoResolveValueTexts();

      if (attributePointsButton != null)
      {
        attributePointsButton.onClick.AddListener(HandleAttributePointsClicked);
        if (attributeButtonText == null)
        {
          attributeButtonText = attributePointsButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (attributeButtonText != null)
        {
          cachedAttributeButtonText = attributeButtonText.text;
        }
      }
    }

    private void AutoResolveValueTexts()
    {
      if (atkValueText == null && atkText != null)
      {
        Transform tr = atkText.transform.Find("Value");
        if (tr != null) atkValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (defValueText == null && defText != null)
      {
        Transform tr = defText.transform.Find("Value");
        if (tr != null) defValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (hpValueText == null && hpText != null)
      {
        Transform tr = hpText.transform.Find("Value");
        if (tr != null) hpValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (staminaValueText == null && staminaText != null)
      {
        Transform tr = staminaText.transform.Find("Value");
        if (tr != null) staminaValueText = tr.GetComponent<TextMeshProUGUI>();
      }
    }

    private void OnEnable()
    {
      AutoResolveValueTexts();
      CachePlayerReferences();
      RefreshStats();

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
      }

      if (PlayerProgression.HasInstance)
      {
        PlayerProgression.Instance.OnStatsAllocated += RefreshStats;
        PlayerProgression.Instance.OnPointsChanged += HandlePointsChanged;
      }
    }

    private void OnDisable()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
      }

      if (PlayerProgression.HasInstance)
      {
        PlayerProgression.Instance.OnStatsAllocated -= RefreshStats;
        PlayerProgression.Instance.OnPointsChanged -= HandlePointsChanged;
      }

      if (cachedPlayerHealth != null)
      {
        cachedPlayerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
      }

      if (cachedPlayerStats != null)
      {
        cachedPlayerStats.onStaminaChanged.RemoveListener(HandleStaminaChanged);
      }
    }

    private void HandleHealthChanged(float cur, float max) => RefreshStats();
    private void HandleStaminaChanged(float cur, float max) => RefreshStats();

    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      RefreshStats();
    }

    private void HandlePointsChanged(int points)
    {
      UpdateAttributeButtonBadge();
    }

    private void CachePlayerReferences()
    {
      if (cachedPlayerHealth != null && cachedPlayerStats != null && cachedPlayerCombat != null) return;

      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        cachedPlayerHealth = playerObj.GetComponent<Health>();
        cachedPlayerStats = playerObj.GetComponent<PlayerStats>();
        cachedPlayerCombat = playerObj.GetComponent<PlayerCombat>();
      }
      else
      {
        var playerCtrl = FindFirstObjectByType<PlayerController>();
        if (playerCtrl != null)
        {
          cachedPlayerHealth = playerCtrl.GetComponent<Health>();
          cachedPlayerStats = playerCtrl.GetComponent<PlayerStats>();
          cachedPlayerCombat = playerCtrl.GetComponent<PlayerCombat>();
        }
      }

      if (cachedPlayerHealth != null)
      {
        cachedPlayerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
        cachedPlayerHealth.onHealthChanged.AddListener(HandleHealthChanged);
      }

      if (cachedPlayerStats != null)
      {
        cachedPlayerStats.onStaminaChanged.RemoveListener(HandleStaminaChanged);
        cachedPlayerStats.onStaminaChanged.AddListener(HandleStaminaChanged);
      }
    }

    /// <summary>
    /// Làm mới và tính toán lại toàn bộ chỉ số nhân vật
    /// </summary>
    public void RefreshStats()
    {
      AutoResolveValueTexts();
      CachePlayerReferences();

      float baseAtk = 15f;
      if (cachedPlayerCombat != null)
      {
        baseAtk = cachedPlayerCombat.BaseAttackDamage;
      }

      float baseHp = 100f;
      if (cachedPlayerHealth != null)
      {
        baseHp = cachedPlayerHealth.BaseMaxHealth;
      }

      float baseStamina = 100f;
      if (cachedPlayerStats != null)
      {
        baseStamina = cachedPlayerStats.BaseMaxStamina;
      }

      float equipAtk = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalAttackBonus() : 0f;
      float progAtk = PlayerProgression.HasInstance ? PlayerProgression.Instance.BonusAttack : 0f;
      float totalBonusAtk = equipAtk + progAtk;

      float totalDef = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalDefenseBonus() : 0f;

      float equipHp = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalHealthBonus() : 0f;
      float progHp = PlayerProgression.HasInstance ? PlayerProgression.Instance.BonusHealth : 0f;
      float totalBonusHp = equipHp + progHp;

      float equipStamina = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalStaminaBonus() : 0f;
      float progStamina = PlayerProgression.HasInstance ? PlayerProgression.Instance.BonusStamina : 0f;
      float totalBonusStamina = equipStamina + progStamina;

      // 1. Tấn công (Attack - chỉ hiển thị số)
      float totalAtk = baseAtk + totalBonusAtk;
      string atkBonusStr = totalBonusAtk > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusAtk:0.#})</color></size>" : "";
      if (atkValueText != null)
      {
        atkValueText.text = $"<color=#FFE082>{totalAtk:0.#}</color>{atkBonusStr}";
      }
      else if (atkText != null)
      {
        atkText.text = $"Attack: <color=#FFE082>{totalAtk:0.#}</color>{atkBonusStr}";
      }

      // 2. Phòng thủ (Defense - chỉ hiển thị số)
      if (defValueText != null)
      {
        defValueText.text = $"<color=#81D4FA>{totalDef:0.#}</color>";
      }
      else if (defText != null)
      {
        defText.text = $"Defense: <color=#81D4FA>{totalDef:0.#}</color>";
      }

      // 3. Máu (Health: Hiện tại / Tối đa)
      float totalHp = baseHp + totalBonusHp;
      float curHp = cachedPlayerHealth != null ? cachedPlayerHealth.CurrentHealth : totalHp;
      string hpBonusStr = totalBonusHp > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusHp:0.#})</color></size>" : "";
      if (hpValueText != null)
      {
        hpValueText.text = $"<color=#EF9A9A>{curHp:0.#} / {totalHp:0.#}</color>{hpBonusStr}";
      }
      else if (hpText != null)
      {
        hpText.text = $"Health: <color=#EF9A9A>{curHp:0.#} / {totalHp:0.#}</color>{hpBonusStr}";
      }

      // 4. Thể lực (Stamina: Hiện tại / Tối đa)
      float totalStam = baseStamina + totalBonusStamina;
      float curStam = cachedPlayerStats != null ? cachedPlayerStats.CurrentStamina : totalStam;
      string stamBonusStr = totalBonusStamina > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusStamina:0.#})</color></size>" : "";
      if (staminaValueText != null)
      {
        staminaValueText.text = $"<color=#A5D6A7>{curStam:0.#} / {totalStam:0.#}</color>{stamBonusStr}";
      }
      else if (staminaText != null)
      {
        staminaText.text = $"Stamina: <color=#A5D6A7>{curStam:0.#} / {totalStam:0.#}</color>{stamBonusStr}";
      }

      UpdateAttributeButtonBadge();
    }

    private void UpdateAttributeButtonBadge()
    {
      if (attributeButtonText == null && attributePointsButton != null)
      {
        attributeButtonText = attributePointsButton.GetComponentInChildren<TextMeshProUGUI>();
        if (attributeButtonText != null && string.IsNullOrEmpty(cachedAttributeButtonText))
        {
          cachedAttributeButtonText = attributeButtonText.text;
        }
      }

      if (attributeButtonText != null && PlayerProgression.HasInstance)
      {
        string baseText = !string.IsNullOrEmpty(cachedAttributeButtonText) ? cachedAttributeButtonText : "Attribute Points";
        int unallocated = PlayerProgression.Instance.UnallocatedPoints;
        if (unallocated > 0)
        {
          attributeButtonText.text = $"{baseText} <color=#FFE082><b>(+{unallocated})</b></color>";
        }
        else
        {
          attributeButtonText.text = baseText;
        }
      }
    }

    private void HandleAttributePointsClicked()
    {
      OnAttributePointsClicked?.Invoke();

      if (attributeAllocationModal != null)
      {
        attributeAllocationModal.Toggle();
      }
      else if (AttributeAllocationUI.Instance != null)
      {
        AttributeAllocationUI.Instance.Toggle();
      }
    }
  }
}
