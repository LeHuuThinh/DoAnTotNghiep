using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống điều phối vé tấn công (Attack Token Director) phong cách Souls-like.
/// Quản lý nhịp độ chiến đấu của toàn bộ quái vật:
/// - Giới hạn số lượng quái được phép tấn công đồng thời (tránh hội đồng, dồn dập).
/// - Tạo khoảng nghỉ (global cooldown) giữa các đòn đánh liên tiếp để người chơi có nhịp né/parry.
/// - Đóng vai trò Registry cho toàn bộ quái để các quái tự phát hiện và né lẫn nhau khi vờn quanh.
/// </summary>
public class AttackTokenDirector : MonoBehaviour
{
  private static AttackTokenDirector instance;
  private static bool isApplicationQuitting = false;

  /// <summary>
  /// Kiểm tra xem Instance có tồn tại và ứng dụng chưa tắt hay không (dùng an toàn trong OnDisable/OnDestroy)
  /// </summary>
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

  [Header("Token Settings (Hệ thống vé tấn công)")]
  [Tooltip("Số quái tối đa được phép lao vào tấn công cùng một thời điểm (Mặc định 1 để chuẩn Souls-like 1v1)")]
  [SerializeField] private int maxAttackTokens = 1;

  [Tooltip("Khoảng nghỉ tối thiểu giữa hai đòn tấn công bất kỳ của đám quái (giây)")]
  [SerializeField] private float minTimeBetweenAttacks = 0.8f;

  [Tooltip("Thời gian tối đa quái được giữ vé trước khi bị thu hồi (Failsafe tránh kẹt vé khi quái bị lạc đường)")]
  [SerializeField] private float tokenHoldTimeout = 4.0f;

  [Header("Debug Monitors")]
  [SerializeField] private int activeTokenCount = 0;
  [SerializeField] private int registeredEnemyCount = 0;

  // Danh sách các quái đang giữ vé tấn công
  private readonly HashSet<EnemyAI> tokenHolders = new HashSet<EnemyAI>();
  private readonly Dictionary<EnemyAI, float> tokenGrantTimes = new Dictionary<EnemyAI, float>();

  // Danh sách toàn bộ quái đang hoạt động trong scene
  private readonly List<EnemyAI> registeredEnemies = new List<EnemyAI>();

  private float nextAllowedAttackTime = 0f;

  public IReadOnlyList<EnemyAI> RegisteredEnemies => registeredEnemies;
  public int ActiveTokenCount => tokenHolders.Count;
  public int MaxAttackTokens => maxAttackTokens;

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
    // Failsafe: Thu hồi vé nếu quái cầm vé quá lâu mà chưa chém xong (ví dụ player chạy quá xa hoặc quái kẹt góc)
    if (tokenHolders.Count > 0)
    {
      List<EnemyAI> timedOutEnemies = null;
      float now = Time.time;

      foreach (var kvp in tokenGrantTimes)
      {
        if (now - kvp.Value >= tokenHoldTimeout)
        {
          if (timedOutEnemies == null) timedOutEnemies = new List<EnemyAI>();
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

    activeTokenCount = tokenHolders.Count;
    registeredEnemyCount = registeredEnemies.Count;
  }

  /// <summary>
  /// Đăng ký quái vật vào hệ thống quản lý khi bắt đầu kích hoạt
  /// </summary>
  public void RegisterEnemy(EnemyAI enemy)
  {
    if (enemy != null && !registeredEnemies.Contains(enemy))
    {
      registeredEnemies.Add(enemy);
    }
  }

  /// <summary>
  /// Hủy đăng ký quái vật khi chết hoặc vô hiệu hóa
  /// </summary>
  public void UnregisterEnemy(EnemyAI enemy)
  {
    if (enemy != null)
    {
      registeredEnemies.Remove(enemy);
      ReleaseAttackToken(enemy);
    }
  }

  /// <summary>
  /// Xin vé (Token) để thực hiện đòn tấn công. Trả về true nếu được cấp vé.
  /// </summary>
  public bool RequestAttackToken(EnemyAI enemy)
  {
    if (enemy == null) return false;

    // Nếu chính con quái này đã cầm vé rồi thì cho phép tiếp tục
    if (tokenHolders.Contains(enemy)) return true;

    // Đã hết vé khả dụng
    if (tokenHolders.Count >= maxAttackTokens) return false;

    // Đang trong thời gian nghỉ toàn cục giữa 2 đòn đánh
    if (Time.time < nextAllowedAttackTime) return false;

    // Cấp vé thành công
    tokenHolders.Add(enemy);
    tokenGrantTimes[enemy] = Time.time;
    return true;
  }

  /// <summary>
  /// Trả lại vé tấn công sau khi thực hiện xong đòn đánh hoặc bị gián đoạn (Parry/Stagger)
  /// </summary>
  public void ReleaseAttackToken(EnemyAI enemy)
  {
    if (enemy == null) return;

    if (tokenHolders.Remove(enemy))
    {
      tokenGrantTimes.Remove(enemy);
      nextAllowedAttackTime = Time.time + minTimeBetweenAttacks;
    }
  }

  /// <summary>
  /// Kiểm tra xem con quái này có đang cầm vé tấn công hay không
  /// </summary>
  public bool HasToken(EnemyAI enemy)
  {
    return enemy != null && tokenHolders.Contains(enemy);
  }
}
