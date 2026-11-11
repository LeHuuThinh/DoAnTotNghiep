using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Progression;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Manages real-time character stats display in the Inventory Left Panel.
  /// Supports Hold-to-Peek interaction:
  /// - Default view: MainStats (7 combat stats: ATK, DEF, HP, Stamina, Crit Rate, Crit DMG, Lifesteal).
  /// - Press and hold mouse on StatsPanel: switches to EffectStats (4 stats: Move Speed, Spirit Gain, Spirit Reduce, Stamina Reduce).
  /// - Release mouse or cursor exit: returns to MainStats.
  /// </summary>
  public class PlayerStatsPanelUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
  {
    [Header("=== STATS VIEW TOGGLE (MAIN & EFFECT STATS) ===")]
    [Tooltip("Root GameObject containing 7 primary combat stats (MainStats)")]
    [SerializeField] private GameObject mainStatsRoot;

    [Tooltip("Root GameObject containing 4 secondary effect stats (EffectStats)")]
    [SerializeField] private GameObject effectStatsRoot;

    [Tooltip("Text displaying interaction hint (e.g. '[Hold to show effect stats]')")]
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("=== MAIN STATS VALUES (7 COMBAT STATS) ===")]
    [Tooltip("Text displaying Attack value (ATK Value)")]
    [SerializeField] private TextMeshProUGUI atkValueText;

    [Tooltip("Text displaying Defense value (DEF Value)")]
    [SerializeField] private TextMeshProUGUI defValueText;

    [Tooltip("Text displaying Health value (HP Value: Current / Max)")]
    [SerializeField] private TextMeshProUGUI hpValueText;

    [Tooltip("Text displaying Stamina value (Stamina Value: Current / Max)")]
    [SerializeField] private TextMeshProUGUI staminaValueText;

    [Tooltip("Text displaying Critical Rate percentage (Crit Rate Value)")]
    [SerializeField] private TextMeshProUGUI critValueText;

    [Tooltip("Text displaying Critical Damage percentage (Crit DMG Value)")]
    [SerializeField] private TextMeshProUGUI critDmgValueText;

    [Tooltip("Text displaying Lifesteal percentage (Lifesteal Value)")]
    [SerializeField] private TextMeshProUGUI lifestealValueText;

    [Header("=== EFFECT STATS VALUES (4 EFFECT STATS) ===")]
    [Tooltip("Text displaying Movement Speed bonus percentage (MoveSpeed Value)")]
    [SerializeField] private TextMeshProUGUI moveSpeedValueText;

    [Tooltip("Text displaying Spirit Gain bonus percentage (SpiritGain Value)")]
    [SerializeField] private TextMeshProUGUI spiritGainValueText;

    [Tooltip("Text displaying Spirit Cost Reduction percentage (SpiritReduce Value)")]
    [SerializeField] private TextMeshProUGUI spiritReduceValueText;

    [Tooltip("Text displaying Stamina Cost Reduction percentage (StaminaReduce Value)")]
    [SerializeField] private TextMeshProUGUI staminaReduceValueText;

    [Header("=== FALLBACK LABELS OR CONTAINERS ===")]
    [Tooltip("Fallback container for Attack")]
    [SerializeField] private TextMeshProUGUI atkText;

    [Tooltip("Fallback container for Defense")]
    [SerializeField] private TextMeshProUGUI defText;

    [Tooltip("Fallback container for Health")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Tooltip("Fallback container for Stamina")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("=== ATTRIBUTE ALLOCATION BUTTON (RPG-05) ===")]
    [Tooltip("Button to open attribute allocation modal")]
    [SerializeField] private Button attributePointsButton;

    [Tooltip("Text on Attribute Points button to display (+X) available points")]
    [SerializeField] private TextMeshProUGUI attributeButtonText;

    [Tooltip("Attribute allocation modal reference")]
    [SerializeField] private AttributeAllocationUI attributeAllocationModal;

    public event Action OnAttributePointsClicked;

    // Runtime state
    private bool isHoldingEffectStats = false;
    public bool IsShowingEffectStats => isHoldingEffectStats;

    private Health cachedPlayerHealth;
    private PlayerStats cachedPlayerStats;
    private PlayerCombat cachedPlayerCombat;
    private string cachedAttributeButtonText;

    private void Reset()
    {
      AutoResolveValueTexts();
    }

    [ContextMenu("Auto Find References")]
    public void ContextMenuAutoFind()
    {
      AutoResolveValueTexts();
    }

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

    /// <summary>
    /// Automatically discovers and binds all UI Text and Root GameObjects if references are not set in Inspector
    /// </summary>
    public void AutoResolveValueTexts()
    {
      // 1. Resolve View Roots & Hint
      if (mainStatsRoot == null)
      {
        Transform tr = transform.Find("MainStats");
        if (tr != null) mainStatsRoot = tr.gameObject;
      }

      if (effectStatsRoot == null)
      {
        Transform tr = transform.Find("EffectStats");
        if (tr != null) effectStatsRoot = tr.gameObject;
      }

      if (hintText == null)
      {
        Transform tr = transform.Find("HintText");
        if (tr != null) hintText = tr.GetComponent<TextMeshProUGUI>();
      }

      // 2. Resolve MainStats Values
      if (atkValueText == null)
      {
        Transform tr = transform.Find("MainStats/AtkText/Value") ?? transform.Find("AtkText/Value") ?? (atkText != null ? atkText.transform.Find("Value") : null);
        if (tr != null) atkValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (defValueText == null)
      {
        Transform tr = transform.Find("MainStats/DefText/Value") ?? transform.Find("DefText/Value") ?? (defText != null ? defText.transform.Find("Value") : null);
        if (tr != null) defValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (hpValueText == null)
      {
        Transform tr = transform.Find("MainStats/HpText/Value") ?? transform.Find("HpText/Value") ?? (hpText != null ? hpText.transform.Find("Value") : null);
        if (tr != null) hpValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (staminaValueText == null)
      {
        Transform tr = transform.Find("MainStats/StaminaText/Value") ?? transform.Find("StaminaText/Value") ?? (staminaText != null ? staminaText.transform.Find("Value") : null);
        if (tr != null) staminaValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (critValueText == null)
      {
        Transform tr = transform.Find("MainStats/CritRateText/Value") ?? transform.Find("CritRateText/Value") ?? transform.Find("CritText/Value") ?? transform.Find("CritValue");
        if (tr != null) critValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (critDmgValueText == null)
      {
        Transform tr = transform.Find("MainStats/CritDameText/Value") ?? transform.Find("CritDameText/Value") ?? transform.Find("MainStats/CritDmgText/Value") ?? transform.Find("CritDmgText/Value") ?? transform.Find("CritDmgValue");
        if (tr != null) critDmgValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (lifestealValueText == null)
      {
        Transform tr = transform.Find("MainStats/LifeStealText/Value") ?? transform.Find("LifeStealText/Value") ?? transform.Find("MainStats/LifestealText/Value") ?? transform.Find("LifestealText/Value") ?? transform.Find("LifestealValue");
        if (tr != null) lifestealValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      // 3. Resolve EffectStats Values
      if (moveSpeedValueText == null)
      {
        Transform tr = transform.Find("EffectStats/MoveSpeedText/Value") ?? transform.Find("MoveSpeedText/Value");
        if (tr != null) moveSpeedValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (spiritGainValueText == null)
      {
        Transform tr = transform.Find("EffectStats/SpiritGainText/Value") ?? transform.Find("SpiritGainText/Value");
        if (tr != null) spiritGainValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (spiritReduceValueText == null)
      {
        Transform tr = transform.Find("EffectStats/SpiritReduceText/Value") ?? transform.Find("SpiritReduceText/Value");
        if (tr != null) spiritReduceValueText = tr.GetComponent<TextMeshProUGUI>();
      }

      if (staminaReduceValueText == null)
      {
        Transform tr = transform.Find("EffectStats/StaminaReduceText/Value") ?? transform.Find("StaminaReduceText/Value");
        if (tr != null) staminaReduceValueText = tr.GetComponent<TextMeshProUGUI>();
      }
    }

    private void OnEnable()
    {
      SetEffectStatsActive(false);
      AutoResolveValueTexts();
      CachePlayerReferences();
      RefreshStats();

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        InventoryManager.Instance.OnEquipmentUpgradeChanged += HandleEquipmentUpgradeChanged;
      }

      if (PlayerProgression.HasInstance)
      {
        PlayerProgression.Instance.OnStatsAllocated += RefreshStats;
        PlayerProgression.Instance.OnPointsChanged += HandlePointsChanged;
      }
    }

    private void OnDisable()
    {
      SetEffectStatsActive(false);

      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
        InventoryManager.Instance.OnEquipmentUpgradeChanged -= HandleEquipmentUpgradeChanged;
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

    // =========================================================================
    // HOLD-TO-PEEK POINTER EVENT HANDLERS
    // =========================================================================

    public void OnPointerDown(PointerEventData eventData)
    {
      SetEffectStatsActive(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
      SetEffectStatsActive(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      if (isHoldingEffectStats)
      {
        SetEffectStatsActive(false);
      }
    }

    private void SetEffectStatsActive(bool showEffect)
    {
      isHoldingEffectStats = showEffect;

      if (mainStatsRoot != null)
      {
        mainStatsRoot.SetActive(!showEffect);
      }

      if (effectStatsRoot != null)
      {
        effectStatsRoot.SetActive(showEffect);
      }

      if (hintText != null)
      {
        hintText.text = showEffect
          ? "<color=#FFE082>[Release to return]</color>"
          : "[Hold to show effect stats]";
      }
    }

    private void HandleHealthChanged(float cur, float max) => RefreshStats();
    private void HandleStaminaChanged(float cur, float max) => RefreshStats();

    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      RefreshStats();
    }

    private void HandleEquipmentUpgradeChanged(ItemType type, int level)
    {
      RefreshStats();
    }

    private void HandlePointsChanged(int points)
    {
      UpdateAttributeButtonBadge();
      if (attributePointsButton != null && points > 0 && gameObject.activeInHierarchy)
      {
        StartCoroutine(PunchButtonRoutine(attributePointsButton.transform, 1.12f, 0.12f));
      }
    }

    private System.Collections.IEnumerator PunchButtonRoutine(Transform tr, float scale, float duration)
    {
      if (tr == null) yield break;
      Vector3 startScale = Vector3.one * scale;
      float elapsed = 0f;
      tr.localScale = startScale;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = UITransitionHelper.EaseOutQuad(t);
        tr.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, e);
        yield return null;
      }
      tr.localScale = Vector3.one;
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
    /// Refreshes all 7 MainStats and 4 EffectStats values in real time
    /// </summary>
    public void RefreshStats()
    {
      AutoResolveValueTexts();
      CachePlayerReferences();

      // --- Base & Progression calculations ---
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

      float totalDef = cachedPlayerHealth != null ? cachedPlayerHealth.TotalDefense : (InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalDefenseBonus() : 0f);

      float equipHp = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalHealthBonus() : 0f;
      float progHp = PlayerProgression.HasInstance ? PlayerProgression.Instance.BonusHealth : 0f;
      float totalBonusHp = equipHp + progHp;

      float equipStamina = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalStaminaBonus() : 0f;
      float progStamina = PlayerProgression.HasInstance ? PlayerProgression.Instance.BonusStamina : 0f;
      float totalBonusStamina = equipStamina + progStamina;

      // =======================================================================
      // 1. MAIN STATS (7 COMBAT VALUES)
      // =======================================================================

      // Attack
      float totalAtk = baseAtk + totalBonusAtk;
      if (atkValueText != null)
      {
        atkValueText.text = $"<color=#FFE082>{totalAtk:0.#}</color>";
      }
      else if (atkText != null)
      {
        atkText.text = $"Attack: <color=#FFE082>{totalAtk:0.#}</color>";
      }

      // Defense
      if (defValueText != null)
      {
        defValueText.text = $"<color=#81D4FA>{totalDef:0.#}</color>";
      }
      else if (defText != null)
      {
        defText.text = $"Defense: <color=#81D4FA>{totalDef:0.#}</color>";
      }

      // Health
      float totalHp = baseHp + totalBonusHp;
      float curHp = cachedPlayerHealth != null ? cachedPlayerHealth.CurrentHealth : totalHp;
      if (hpValueText != null)
      {
        hpValueText.text = $"<color=#EF9A9A>{curHp:0.#} / {totalHp:0.#}</color>";
      }
      else if (hpText != null)
      {
        hpText.text = $"Health: <color=#EF9A9A>{curHp:0.#} / {totalHp:0.#}</color>";
      }

      // Stamina
      float totalStam = baseStamina + totalBonusStamina;
      float curStam = cachedPlayerStats != null ? cachedPlayerStats.CurrentStamina : totalStam;
      if (staminaValueText != null)
      {
        staminaValueText.text = $"<color=#A5D6A7>{curStam:0.#} / {totalStam:0.#}</color>";
      }
      else if (staminaText != null)
      {
        staminaText.text = $"Stamina: <color=#A5D6A7>{curStam:0.#} / {totalStam:0.#}</color>";
      }

      // Critical Rate & Critical Damage & Lifesteal
      float totalCritRate = cachedPlayerCombat != null ? cachedPlayerCombat.TotalCritRate : (10f + (InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalCritRateBonus() : 0f));
      float totalCritDmg = cachedPlayerCombat != null ? cachedPlayerCombat.TotalCritDamage : (150f + (InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalCritDamageBonus() : 0f));
      float totalLifesteal = cachedPlayerCombat != null ? cachedPlayerCombat.TotalLifesteal : (InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalLifestealBonus() : 0f);

      if (critValueText != null) critValueText.text = $"<color=#FFB74D>{totalCritRate:0.#}%</color>";
      if (critDmgValueText != null) critDmgValueText.text = $"<color=#FFA726>{totalCritDmg:0.#}%</color>";
      if (lifestealValueText != null) lifestealValueText.text = $"<color=#E57373>{totalLifesteal:0.#}%</color>";

      // =======================================================================
      // 2. EFFECT STATS (4 SECONDARY VALUES)
      // =======================================================================

      // Movement Speed Bonus
      float moveSpeedBonus = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalMoveSpeedBonus() : 0f;
      if (moveSpeedValueText != null)
      {
        moveSpeedValueText.text = $"<color=#FFF176>+{moveSpeedBonus:0.#}%</color>";
      }

      // Spirit Gain Bonus
      float spiritGainBonus = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalSpiritGainBonus() : 0f;
      if (spiritGainValueText != null)
      {
        spiritGainValueText.text = $"<color=#BA68C8>+{spiritGainBonus:0.#}%</color>";
      }

      // Spirit Cost Reduction
      float spiritCostReduction = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalSpiritCostReductionBonus() : 0f;
      if (spiritReduceValueText != null)
      {
        spiritReduceValueText.text = $"<color=#4DD0E1>-{spiritCostReduction:0.#}%</color>";
      }

      // Stamina Cost Reduction
      float staminaCostReduction = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalStaminaCostReductionBonus() : 0f;
      if (staminaReduceValueText != null)
      {
        staminaReduceValueText.text = $"<color=#81C784>-{staminaCostReduction:0.#}%</color>";
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
      if (attributePointsButton != null && gameObject.activeInHierarchy)
      {
        StartCoroutine(PunchButtonRoutine(attributePointsButton.transform, 0.94f, 0.08f));
      }

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
