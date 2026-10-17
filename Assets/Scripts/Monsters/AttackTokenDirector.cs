using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống điều phối vé tấn công (Attack Token Director) đa luồng (Dual-Pool Token).
/// Tách riêng vé Cận chiến (Melee) và vé Tầm xa (Ranged):
/// - Quái cận chiến (Warrior, Minion) dùng chung bể vé Cận chiến (Melee Tokens).
/// - Quái tầm xa (Rogue, Mage) dùng chung bể vé Tầm xa (Ranged Tokens).
/// Cho phép 1 quái cận chiến áp sát VÀ 1 quái tầm xa bắn yểm trợ cùng lúc mà không làm loạn nhịp!
/// </summary>
public class AttackTokenDirector : MonoBehaviour
{
  private static AttackTokenDirector instance;
  private static bool isApplicationQuitting = false;

  public static bool HasInstance => instance != null && !isApplicationQuitting;

  public static AttackTokenDirector Instance
  {
    get
    {
      if (isApplicationQuitting) return null;

      if (instance == null)
      {
        instance = FindAnyObjectByType<AttackTokenDirector>();
        if (instance == null && !isApplicationQuitting && Application.isPlaying)
        {
          GameObject go = new GameObject("AttackTokenDirector");
          instance = go.AddComponent<AttackTokenDirector>();
        }
      }
      return instance;
    }
  }

  [Header("Melee Token Pool (Bể Vé Cận Chiến - Warrior, Minion)")]
  [Tooltip("Số quái cận chiến tối đa được phép tấn công cùng lúc (Mặc định 1 để giữ nhịp 1v1 chuẩn Souls-like)")]
  [SerializeField] private int maxMeleeTokens = 1;
  [Tooltip("Khoảng nghỉ tối thiểu giữa hai đòn chém cận chiến bất kỳ (giây)")]
  [SerializeField] private float minTimeBetweenMeleeAttacks = 0.8f;

  [Header("Ranged Token Pool (Bể Vé Tầm Xa - Rogue, Mage)")]
  [Tooltip("Số quái tầm xa tối đa được phép bắn tên/phóng phép cùng lúc")]
  [SerializeField] private int maxRangedTokens = 1;
  [Tooltip("Khoảng nghỉ tối thiểu giữa hai phát bắn tầm xa bất kỳ (giây)")]
  [SerializeField] private float minTimeBetweenRangedAttacks = 1.2f;

  [Header("General Settings")]
  [Tooltip("Thời gian tối đa quái được giữ vé trước khi bị thu hồi (Failsafe tránh kẹt vé khi quái lạc đường)")]
  [SerializeField] private float tokenHoldTimeout = 4.0f;

  [Header("Debug Monitors")]
  [SerializeField] private int activeMeleeTokens = 0;
  [SerializeField] private int activeRangedTokens = 0;
  [SerializeField] private int registeredEnemyCount = 0;

  // Danh sách các quái đang giữ vé tấn công
  private readonly HashSet<EnemyAIBase> meleeTokenHolders = new HashSet<EnemyAIBase>();
  private readonly HashSet<EnemyAIBase> rangedTokenHolders = new HashSet<EnemyAIBase>();
  private readonly Dictionary<EnemyAIBase, float> tokenGrantTimes = new Dictionary<EnemyAIBase, float>();

  // Danh sách toàn bộ quái đang hoạt động trong scene
  private readonly List<EnemyAIBase> registeredEnemies = new List<EnemyAIBase>();

  private float nextAllowedMeleeAttackTime = 0f;
  private float nextAllowedRangedAttackTime = 0f;

  public IReadOnlyList<EnemyAIBase> RegisteredEnemies => registeredEnemies;
  public int ActiveMeleeTokens => meleeTokenHolders.Count;
  public int ActiveRangedTokens => rangedTokenHolders.Count;
  public int ActiveTokenCount => meleeTokenHolders.Count + rangedTokenHolders.Count;
  public int MaxMeleeTokens => maxMeleeTokens;
  public int MaxRangedTokens => maxRangedTokens;

  private void Awake()
  {
    isApplicationQuitting = false;

    if (instance == null)
    {
      instance = this;
    }
    else if (instance != this)
    {
      Destroy(gameObject);
    }
  }

  private void OnApplicationQuit()
  {
    isApplicationQuitting = true;
  }

  private void OnDestroy()
  {
    if (instance == this)
    {
      instance = null;
    }
  }

#if UNITY_EDITOR
  [UnityEditor.MenuItem("Tools/Combat/Create Attack Token Director")]
  public static void CreateDirectorInScene()
  {
    AttackTokenDirector existing = FindAnyObjectByType<AttackTokenDirector>();
    if (existing != null)
    {
      UnityEditor.Selection.activeGameObject = existing.gameObject;
      Debug.Log("<color=yellow>[AttackTokenDirector]</color> Đã tồn tại trong Scene: " + existing.gameObject.name);
      return;
    }

    GameObject go = new GameObject("AttackTokenDirector");
    AttackTokenDirector director = go.AddComponent<AttackTokenDirector>();
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create AttackTokenDirector");
    UnityEditor.Selection.activeGameObject = go;
    Debug.Log("<color=green>[AttackTokenDirector]</color> Đã tạo GameObject AttackTokenDirector trong Scene! Bạn có thể tùy chỉnh thông số trong Inspector và lưu Scene (Ctrl + S).");
  }
#endif

  private void Update()
  {
    // Failsafe: Thu hồi vé nếu quái cầm vé quá lâu mà chưa tấn công xong
    if (tokenGrantTimes.Count > 0)
    {
      List<EnemyAIBase> timedOutEnemies = null;
      float now = Time.time;

      foreach (var kvp in tokenGrantTimes)
      {
        if (now - kvp.Value >= tokenHoldTimeout)
        {
          if (timedOutEnemies == null) timedOutEnemies = new List<EnemyAIBase>();
          timedOutEnemies.Add(kvp.Key);
        }
      }

      if (timedOutEnemies != null)
      {
        foreach (var enemy in timedOutEnemies)
        {
          if (enemy != null)
          {
            enemy.RevokeAttackToken();
          }
          ReleaseAttackToken(enemy);
        }
      }
    }

    activeMeleeTokens = meleeTokenHolders.Count;
    activeRangedTokens = rangedTokenHolders.Count;
    registeredEnemyCount = registeredEnemies.Count;
  }

  public void RegisterEnemy(EnemyAIBase enemy)
  {
    if (enemy != null && !registeredEnemies.Contains(enemy))
    {
      registeredEnemies.Add(enemy);
    }
  }

  public void UnregisterEnemy(EnemyAIBase enemy)
  {
    if (enemy != null)
    {
      registeredEnemies.Remove(enemy);
      ReleaseAttackToken(enemy);
    }
  }

  /// <summary>
  /// Xin vé tấn công tùy theo Class/Role của quái (Cận chiến hoặc Đánh xa)
  /// </summary>
  public bool RequestAttackToken(EnemyAIBase enemy)
  {
    if (enemy == null) return false;

    if (enemy.CombatRole == EnemyCombatRole.Melee)
    {
      // Nếu đã giữ vé cận chiến rồi -> Cho phép tiếp tục
      if (meleeTokenHolders.Contains(enemy)) return true;

      // Hết vé cận chiến
      if (meleeTokenHolders.Count >= maxMeleeTokens) return false;

      // Đang trong cooldown nghỉ giữa 2 đòn cận chiến
      if (Time.time < nextAllowedMeleeAttackTime) return false;

      meleeTokenHolders.Add(enemy);
      tokenGrantTimes[enemy] = Time.time;
      return true;
    }
    else
    {
      // Quái tầm xa (Ranged)
      if (rangedTokenHolders.Contains(enemy)) return true;

      // Hết vé tầm xa
      if (rangedTokenHolders.Count >= maxRangedTokens) return false;

      // Đang trong cooldown nghỉ giữa 2 phát bắn
      if (Time.time < nextAllowedRangedAttackTime) return false;

      rangedTokenHolders.Add(enemy);
      tokenGrantTimes[enemy] = Time.time;
      return true;
    }
  }

  /// <summary>
  /// Trả lại vé tấn công sau khi hoàn tất đòn đánh hoặc bị ngắt chiêu (Parry/Stagger)
  /// </summary>
  public void ReleaseAttackToken(EnemyAIBase enemy)
  {
    if (enemy == null) return;

    if (enemy.CombatRole == EnemyCombatRole.Melee)
    {
      if (meleeTokenHolders.Remove(enemy))
      {
        tokenGrantTimes.Remove(enemy);
        nextAllowedMeleeAttackTime = Time.time + minTimeBetweenMeleeAttacks;
      }
    }
    else
    {
      if (rangedTokenHolders.Remove(enemy))
      {
        tokenGrantTimes.Remove(enemy);
        nextAllowedRangedAttackTime = Time.time + minTimeBetweenRangedAttacks;
      }
    }
  }

  public bool HasToken(EnemyAIBase enemy)
  {
    if (enemy == null) return false;
    return meleeTokenHolders.Contains(enemy) || rangedTokenHolders.Contains(enemy);
  }
}
