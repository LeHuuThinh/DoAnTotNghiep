using System;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Progression
{
  /// <summary>
  /// Quản lý Cấp độ (Level), Kinh nghiệm (EXP) và Hệ thống Phân bổ Điểm Tiềm Năng (Attribute Points).
  /// Có hỗ trợ Singleton / DontDestroyOnLoad để giữ nguyên tiến trình người chơi qua các Scene.
  /// </summary>
  public class PlayerProgression : MonoBehaviour
  {
    private static PlayerProgression instance;
    public static PlayerProgression Instance
    {
      get
      {
        if (instance == null)
        {
          instance = FindFirstObjectByType<PlayerProgression>();
          if (instance == null)
          {
            GameObject obj = new GameObject("PlayerProgression");
            instance = obj.AddComponent<PlayerProgression>();
            DontDestroyOnLoad(obj);
          }
        }
        return instance;
      }
    }
    public static bool HasInstance => instance != null || FindFirstObjectByType<PlayerProgression>() != null;

    [Header("=== CẤP ĐỘ & KINH NGHIỆM ===")]
    [Tooltip("Cấp độ hiện tại của nhân vật")]
    [SerializeField] private int currentLevel = 1;

    [Tooltip("Cấp độ tối đa có thể đạt được")]
    [SerializeField] private int maxLevel = 50;

    [Tooltip("Lượng EXP hiện tại đang tích lũy")]
    [SerializeField] private int currentExp = 0;

    [Tooltip("Lượng EXP cơ bản cần để lên Cấp 2")]
    [SerializeField] private int baseExpToLevel2 = 100;

    [Tooltip("Hệ số tăng trưởng EXP theo cấp độ: ExpCần = BaseExp * (Level ^ ExpGrowthExponent)")]
    [SerializeField] private float expGrowthExponent = 1.35f;

    [Header("=== ĐIỂM TIỀM NĂNG (ATTRIBUTE POINTS) ===")]
    [Tooltip("Số điểm tiềm năng thưởng cho người chơi mỗi khi Lên Cấp")]
    [SerializeField] private int pointsPerLevel = 3;

    [Tooltip("Số điểm tiềm năng chưa dùng (Unallocated Points)")]
    [SerializeField] private int unallocatedPoints = 0;

    [Header("=== ĐIỂM ĐÃ PHÂN BỔ (ALLOCATED POINTS) ===")]
    [Tooltip("Số điểm đã cộng vào Sức Tấn Công")]
    [SerializeField] private int allocatedAttackPoints = 0;

    [Tooltip("Số điểm đã cộng vào Máu Tối Đa")]
    [SerializeField] private int allocatedHealthPoints = 0;

    [Tooltip("Số điểm đã cộng vào Thể Lực Tối Đa")]
    [SerializeField] private int allocatedStaminaPoints = 0;

    [Header("=== HIỆU QUẢ MỖI ĐIỂM TIỀM NĂNG (SCALING) ===")]
    [Tooltip("Mỗi điểm Tấn công tăng thêm bao nhiêu sát thương")]
    [SerializeField] private float attackPerPoint = 2f;

    [Tooltip("Mỗi điểm Máu tăng thêm bao nhiêu Max HP")]
    [SerializeField] private float healthPerPoint = 15f;

    [Tooltip("Mỗi điểm Thể lực tăng thêm bao nhiêu Max Stamina")]
    [SerializeField] private float staminaPerPoint = 10f;

    [Header("=== TẨY ĐIỂM TIỀM NĂNG (RESET STATS) ===")]
    [Tooltip("Chi phí Tiền Vàng cần để Tẩy toàn bộ điểm tiềm năng")]
    [SerializeField] private int resetCostGold = 100;

    // Events để UI và các hệ thống khác đăng ký lắng nghe
    public event Action<int, int> OnExpChanged;      // (currentExp, expToNextLevel)
    public event Action<int> OnLevelUp;              // (newLevel)
    public event Action<int> OnPointsChanged;        // (unallocatedPoints)
    public event Action OnStatsAllocated;            // Khi xác nhận cộng điểm hoặc reset

    // Public Getters
    public int CurrentLevel => currentLevel;
    public int MaxLevel => maxLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => GetExpRequiredForLevel(currentLevel);
    public float ExpNormalized => (ExpToNextLevel > 0) ? Mathf.Clamp01((float)currentExp / ExpToNextLevel) : 0f;

    public int PointsPerLevel => pointsPerLevel;
    public int UnallocatedPoints => unallocatedPoints;
    public int AllocatedAttackPoints => allocatedAttackPoints;
    public int AllocatedHealthPoints => allocatedHealthPoints;
    public int AllocatedStaminaPoints => allocatedStaminaPoints;
    public int TotalAllocatedPoints => allocatedAttackPoints + allocatedHealthPoints + allocatedStaminaPoints;

    public float AttackPerPoint => attackPerPoint;
    public float HealthPerPoint => healthPerPoint;
    public float StaminaPerPoint => staminaPerPoint;
    public int ResetCostGold => resetCostGold;

    // Bonus Stats từ điểm tiềm năng
    public float BonusAttack => allocatedAttackPoints * attackPerPoint;
    public float BonusHealth => allocatedHealthPoints * healthPerPoint;
    public float BonusStamina => allocatedStaminaPoints * staminaPerPoint;

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
      // Đồng bộ chỉ số lên Player khi bắt đầu game
      ApplyStatsToPlayer();

      // Thông báo cập nhật ban đầu
      OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
      OnPointsChanged?.Invoke(unallocatedPoints);
      OnLevelUp?.Invoke(currentLevel);

      // Lắng nghe đổi trang bị để đồng bộ lại Máu/Thể lực
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
      }
    }

    private void OnDestroy()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
      }
    }

    private void HandleEquipmentChanged(ItemType type, ItemData item)
    {
      ApplyStatsToPlayer();
    }

    /// <summary>
    /// Tính lượng EXP cần để vượt qua một cấp độ cụ thể
    /// </summary>
    public int GetExpRequiredForLevel(int level)
    {
      if (level >= maxLevel) return int.MaxValue;
      if (level <= 1) return baseExpToLevel2;

      // Công thức luỹ thừa mượt mà: baseExp * (level ^ exponent)
      float required = baseExpToLevel2 * Mathf.Pow(level, expGrowthExponent);
      return Mathf.RoundToInt(required);
    }

    /// <summary>
    /// Thêm EXP khi tiêu diệt quái vật hoặc hoàn thành mục tiêu
    /// </summary>
    public void GainExp(int amount)
    {
      if (amount <= 0 || currentLevel >= maxLevel) return;

      currentExp += amount;
      bool leveledUp = false;

      while (currentLevel < maxLevel && currentExp >= ExpToNextLevel)
      {
        currentExp -= ExpToNextLevel;
        currentLevel++;
        unallocatedPoints += pointsPerLevel;
        leveledUp = true;

        Debug.Log($"<color=#FFE082>[CHÚC MỪNG] Player LÊN CẤP {currentLevel}! Nhận +{pointsPerLevel} Điểm Tiềm Năng!</color>");
        OnLevelUp?.Invoke(currentLevel);
      }

      if (currentLevel >= maxLevel)
      {
        currentExp = 0;
      }

      OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
      if (leveledUp)
      {
        OnPointsChanged?.Invoke(unallocatedPoints);
        ApplyStatsToPlayer();
      }
    }

    /// <summary>
    /// Phân bổ điểm tiềm năng vào các thuộc tính
    /// </summary>
    public bool AllocatePoints(int addAtk, int addHp, int addStam)
    {
      if (addAtk < 0 || addHp < 0 || addStam < 0) return false;
      int totalAdd = addAtk + addHp + addStam;

      if (totalAdd <= 0 || totalAdd > unallocatedPoints)
      {
        Debug.LogWarning("[PlayerProgression] Số điểm muốn cộng không hợp lệ hoặc vượt quá điểm khả dụng!");
        return false;
      }

      unallocatedPoints -= totalAdd;
      allocatedAttackPoints += addAtk;
      allocatedHealthPoints += addHp;
      allocatedStaminaPoints += addStam;

      ApplyStatsToPlayer();

      OnPointsChanged?.Invoke(unallocatedPoints);
      OnStatsAllocated?.Invoke();

      Debug.Log($"[PlayerProgression] Đã phân bổ điểm thành công: +{addAtk} ATK, +{addHp} HP, +{addStam} Stamina. Điểm còn lại: {unallocatedPoints}");
      return true;
    }

    /// <summary>
    /// Kiểm tra xem người chơi có đủ điều kiện để Tẩy Điểm không
    /// </summary>
    public bool CanResetStats(out string reason)
    {
      if (TotalAllocatedPoints <= 0)
      {
        reason = "No allocated attribute points to reset!";
        return false;
      }

      if (InventoryManager.HasInstance && InventoryManager.Instance.CurrentGold < resetCostGold)
      {
        reason = $"Not enough Gold! Requires {resetCostGold} Gold (Current: {InventoryManager.Instance.CurrentGold} Gold).";
        return false;
      }

      reason = string.Empty;
      return true;
    }

    /// <summary>
    /// Tẩy toàn bộ điểm tiềm năng: Trừ X Vàng, hoàn lại 100% điểm về Chưa Phân Bổ, xóa bonus
    /// </summary>
    public bool ResetStats()
    {
      if (!CanResetStats(out string reason))
      {
        Debug.LogWarning($"[PlayerProgression] Không thể tẩy điểm: {reason}");
        return false;
      }

      // Trừ vàng
      if (InventoryManager.HasInstance && resetCostGold > 0)
      {
        if (!InventoryManager.Instance.SpendGold(resetCostGold))
        {
          return false;
        }
      }

      // Hoàn lại điểm
      int pointsToRefund = TotalAllocatedPoints;
      unallocatedPoints += pointsToRefund;
      allocatedAttackPoints = 0;
      allocatedHealthPoints = 0;
      allocatedStaminaPoints = 0;

      ApplyStatsToPlayer();

      OnPointsChanged?.Invoke(unallocatedPoints);
      OnStatsAllocated?.Invoke();

      Debug.Log($"<color=#A5D6A7>[PlayerProgression] Tẩy điểm thành công! Đã hoàn trả {pointsToRefund} điểm tiềm năng (Tiêu hao: {resetCostGold} Vàng).</color>");
      return true;
    }

    /// <summary>
    /// Đồng bộ chỉ số bonus từ điểm tiềm năng vào Health, PlayerStats, PlayerCombat
    /// </summary>
    public void ApplyStatsToPlayer()
    {
      GameObject playerObj = GameObject.FindWithTag("Player");
      if (playerObj == null)
      {
        var playerCtrl = FindFirstObjectByType<PlayerController>();
        if (playerCtrl != null) playerObj = playerCtrl.gameObject;
      }

      if (playerObj == null) return;

      // 1. Áp dụng Máu
      Health health = playerObj.GetComponent<Health>();
      if (health != null)
      {
        float equipHp = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalHealthBonus() : 0f;
        health.SetBonusMaxHealth(BonusHealth, equipHp);
      }

      // 2. Áp dụng Thể lực
      PlayerStats stats = playerObj.GetComponent<PlayerStats>();
      if (stats != null)
      {
        float equipStam = InventoryManager.HasInstance ? InventoryManager.Instance.GetTotalStaminaBonus() : 0f;
        stats.SetBonusMaxStamina(BonusStamina, equipStam);
      }
    }

    // =========================================================================
    // DEBUG CONTEXT MENU CHO DEVELOPER TRONG UNITY EDITOR
    // =========================================================================
    [ContextMenu("Debug: Add 50 EXP")]
    private void DebugAdd50Exp() => GainExp(50);

    [ContextMenu("Debug: Add 150 EXP")]
    private void DebugAdd150Exp() => GainExp(150);

    [ContextMenu("Debug: Level Up")]
    private void DebugLevelUp() => GainExp(ExpToNextLevel - currentExp);

    [ContextMenu("Debug: Add 5 Points")]
    private void DebugAdd5Points()
    {
      unallocatedPoints += 5;
      OnPointsChanged?.Invoke(unallocatedPoints);
    }

    [ContextMenu("Debug: Reset Stats (Free)")]
    private void DebugResetFree()
    {
      int points = TotalAllocatedPoints;
      unallocatedPoints += points;
      allocatedAttackPoints = 0;
      allocatedHealthPoints = 0;
      allocatedStaminaPoints = 0;
      ApplyStatsToPlayer();
      OnPointsChanged?.Invoke(unallocatedPoints);
      OnStatsAllocated?.Invoke();
    }
  }
}
