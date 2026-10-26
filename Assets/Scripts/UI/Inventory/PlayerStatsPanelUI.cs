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
    [Header("=== TEXT HIỂN THỊ CHỈ SỐ ===")]
    [Tooltip("Text hiển thị chỉ số Tấn công (ATK)")]
    [SerializeField] private TextMeshProUGUI atkText;

    [Tooltip("Text hiển thị chỉ số Phòng thủ (DEF)")]
    [SerializeField] private TextMeshProUGUI defText;

    [Tooltip("Text hiển thị chỉ số Máu tối đa (Max HP)")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Tooltip("Text hiển thị chỉ số Thể lực tối đa (Max Stamina)")]
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

    private void Awake()
    {
      if (attributePointsButton != null)
      {
        attributePointsButton.onClick.AddListener(HandleAttributePointsClicked);
      }
    }

    private void OnEnable()
    {
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
    }

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
    }

    /// <summary>
    /// Làm mới và tính toán lại toàn bộ chỉ số nhân vật
    /// </summary>
    public void RefreshStats()
    {
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

      // 1. Tấn công
      if (atkText != null)
      {
        float totalAtk = baseAtk + totalBonusAtk;
        string bonusStr = totalBonusAtk > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusAtk})</color></size>" : "";
        atkText.text = $"Tấn công: <color=#FFE082>{totalAtk}</color>{bonusStr}";
      }

      // 2. Phòng thủ
      if (defText != null)
      {
        defText.text = $"Phòng thủ: <color=#81D4FA>{totalDef}</color>";
      }

      // 3. Máu tối đa
      if (hpText != null)
      {
        float totalHp = baseHp + totalBonusHp;
        string bonusStr = totalBonusHp > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusHp})</color></size>" : "";
        hpText.text = $"Máu tối đa: <color=#EF9A9A>{totalHp}</color>{bonusStr}";
      }

      // 4. Thể lực tối đa
      if (staminaText != null)
      {
        float totalStam = baseStamina + totalBonusStamina;
        string bonusStr = totalBonusStamina > 0f ? $" <size=80%><color=#A5D6A7>(+{totalBonusStamina})</color></size>" : "";
        staminaText.text = $"Thể lực: <color=#A5D6A7>{totalStam}</color>{bonusStr}";
      }

      UpdateAttributeButtonBadge();
    }

    private void UpdateAttributeButtonBadge()
    {
      if (attributeButtonText == null && attributePointsButton != null)
      {
        attributeButtonText = attributePointsButton.GetComponentInChildren<TextMeshProUGUI>();
      }

      if (attributeButtonText != null && PlayerProgression.HasInstance)
      {
        int unallocated = PlayerProgression.Instance.UnallocatedPoints;
        if (unallocated > 0)
        {
          attributeButtonText.text = $"Cộng Điểm <color=#FFE082><b>(+{unallocated})</b></color>";
        }
        else
        {
          attributeButtonText.text = "Cộng Điểm";
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
