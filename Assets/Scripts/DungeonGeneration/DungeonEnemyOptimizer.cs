using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ tối ưu hóa quái vật hầm ngục theo cự ly (AI & Animation LOD / Hibernation Optimizer).
/// Quản lý toàn bộ quái vật trong hầm ngục (~200 quái):
/// - Zone 1: Gần người chơi (< 40m): Thức tỉnh hoàn toàn, chạy AI, tìm đường NavMesh, tính toán Animator.
/// - Zone 2: Đệm chuyển tiếp (40m - 48m): Hysteresis Buffer 8m chống hiện tượng bật/tắt liên tục.
/// - Zone 3: Xa người chơi (> 48m): Ngủ đông (Sleep) - dừng NavMeshAgent, tắt tính toán Animator (CullCompletely), ngắt Update() AI -> 0% CPU!
/// Sử dụng cơ chế Time-Slicing (chia nhỏ danh sách kiểm tra thành nhiều batch) để triệt tiêu hoàn toàn CPU spike.
/// </summary>
public class DungeonEnemyOptimizer : MonoBehaviour
{
  public static DungeonEnemyOptimizer Instance { get; private set; }

  [Header("1. Cấu Hình Tầm Hoạt Động (Distance Settings)")]
  [Range(20f, 80f)]
  [Tooltip("Khoảng cách tối đa (mét) để quái vật thức tỉnh và hoạt động đầy đủ AI")]
  public float wakeDistance = 40f;

  [Range(3f, 15f)]
  [Tooltip("Độ đệm tắt chống nhấp nháy/giật trạng thái (mét): Thức ở wakeDistance, nhưng chỉ ngủ khi đi xa hơn wakeDistance + buffer")]
  public float cullingBufferDistance = 8f;

  [Range(0.05f, 0.5f)]
  [Tooltip("Tần suất quét cự ly (giây). 0.12s = ~8 lần/giây (siêu nhẹ, không tốn CPU)")]
  public float checkInterval = 0.12f;

  [Range(1, 8)]
  [Tooltip("Số lượt chia nhỏ danh sách (Time-Slicing) để phân tán tải CPU đều qua các frame")]
  public int timeSliceBatches = 4;

  [Header("2. Giám Sát Hiệu Năng Runtime")]
  [SerializeField] private int totalManagedEnemies = 0;
  [SerializeField] private int activeEnemiesCount = 0;
  [SerializeField] private int sleepingEnemiesCount = 0;

  private Transform playerTransform;
  private readonly List<EnemyAIBase> managedEnemies = new List<EnemyAIBase>();
  private float timer = 0f;
  private int currentBatchIndex = 0;

  public int TotalManagedEnemies => totalManagedEnemies;
  public int ActiveEnemiesCount => activeEnemiesCount;
  public int SleepingEnemiesCount => sleepingEnemiesCount;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else if (Instance != this)
    {
      Destroy(this);
    }
  }

  private void Start()
  {
    FindPlayer();
  }

  private void Update()
  {
    if (managedEnemies.Count == 0) return;

    if (playerTransform == null)
    {
      FindPlayer();
      if (playerTransform == null) return;
    }

    timer += Time.deltaTime;
    float sliceInterval = checkInterval / Mathf.Max(1, timeSliceBatches);

    if (timer >= sliceInterval)
    {
      timer = 0f;
      ProcessCurrentBatch();
    }
  }

  /// <summary>
  /// Tự động dò tìm Player trong Scene
  /// </summary>
  private void FindPlayer()
  {
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerTransform = player.transform;
      return;
    }

    var playerCtrl = Object.FindFirstObjectByType<PlayerController>();
    if (playerCtrl != null)
    {
      playerTransform = playerCtrl.transform;
      return;
    }

    Camera mainCam = Camera.main;
    if (mainCam != null) playerTransform = mainCam.transform;
  }

  /// <summary>
  /// Nạp danh sách quái vật mới sinh từ Spawner
  /// </summary>
  public void RefreshEnemies(IEnumerable<GameObject> enemies)
  {
    managedEnemies.Clear();

    if (enemies != null)
    {
      foreach (var obj in enemies)
      {
        if (obj == null) continue;
        EnemyAIBase ai = obj.GetComponent<EnemyAIBase>();
        if (ai != null && !managedEnemies.Contains(ai))
        {
          managedEnemies.Add(ai);
        }
      }
    }

    totalManagedEnemies = managedEnemies.Count;

    // Quét ngay lập tức toàn bộ quái để đưa các quái ở xa vào trạng thái ngủ đông ngay từ đầu
    UpdateAllEnemiesImmediate();
  }

  /// <summary>
  /// Đăng ký thêm một quái vật đơn lẻ
  /// </summary>
  public void RegisterEnemy(EnemyAIBase enemy)
  {
    if (enemy != null && !managedEnemies.Contains(enemy))
    {
      managedEnemies.Add(enemy);
      totalManagedEnemies = managedEnemies.Count;
    }
  }

  /// <summary>
  /// Hủy đăng ký khi quái chết hoặc bị xóa
  /// </summary>
  public void UnregisterEnemy(EnemyAIBase enemy)
  {
    if (enemy != null)
    {
      managedEnemies.Remove(enemy);
      totalManagedEnemies = managedEnemies.Count;
    }
  }

  /// <summary>
  /// Xử lý một nhóm quái vật (Batch) theo phân chia Time-Slice
  /// </summary>
  private void ProcessCurrentBatch()
  {
    if (managedEnemies.Count == 0 || playerTransform == null) return;

    int total = managedEnemies.Count;
    int batchSize = Mathf.CeilToInt((float)total / Mathf.Max(1, timeSliceBatches));
    int startIndex = currentBatchIndex * batchSize;
    int endIndex = Mathf.Min(startIndex + batchSize, total);

    Vector3 pPos = playerTransform.position;
    float wakeDistSq = wakeDistance * wakeDistance;
    float sleepDistance = wakeDistance + cullingBufferDistance;
    float sleepDistSq = sleepDistance * sleepDistance;

    for (int i = startIndex; i < endIndex; i++)
    {
      EnemyAIBase enemy = managedEnemies[i];
      if (enemy == null) continue;

      Vector3 enemyPos = enemy.transform.position;
      float dX = pPos.x - enemyPos.x;
      float dZ = pPos.z - enemyPos.z;
      float sqrDist = dX * dX + dZ * dZ;

      // 1. Nếu quái đang ngủ và Player tiến vào phạm vi wakeDistance -> ĐÁNH THỨC
      if (enemy.IsSleeping && sqrDist <= wakeDistSq)
      {
        enemy.SetHibernation(false);
      }
      // 2. Nếu quái đang thức và Player đi ra xa hơn sleepDistance -> CHO NGỦ ĐÔNG
      else if (!enemy.IsSleeping && sqrDist > sleepDistSq)
      {
        enemy.SetHibernation(true);
      }
    }

    // Cập nhật thống kê runtime
    UpdateStatsCount();

    // Chuyển sang batch tiếp theo
    currentBatchIndex = (currentBatchIndex + 1) % Mathf.Max(1, timeSliceBatches);
  }

  /// <summary>
  /// Cập nhật ngay lập tức toàn bộ quái vật không chờ time-slice (dùng lúc vừa tạo hầm ngục)
  /// </summary>
  public void UpdateAllEnemiesImmediate()
  {
    if (playerTransform == null) FindPlayer();
    if (playerTransform == null || managedEnemies.Count == 0) return;

    Vector3 pPos = playerTransform.position;
    float wakeDistSq = wakeDistance * wakeDistance;

    for (int i = 0; i < managedEnemies.Count; i++)
    {
      EnemyAIBase enemy = managedEnemies[i];
      if (enemy == null) continue;

      Vector3 enemyPos = enemy.transform.position;
      float dX = pPos.x - enemyPos.x;
      float dZ = pPos.z - enemyPos.z;
      float sqrDist = dX * dX + dZ * dZ;

      // Quái ở gần thì thức, ở xa thì lập tức ngủ đông
      enemy.SetHibernation(sqrDist > wakeDistSq);
    }

    UpdateStatsCount();
  }

  private void UpdateStatsCount()
  {
    int active = 0;
    int sleeping = 0;

    for (int i = 0; i < managedEnemies.Count; i++)
    {
      if (managedEnemies[i] == null) continue;
      if (managedEnemies[i].IsSleeping) sleeping++;
      else active++;
    }

    activeEnemiesCount = active;
    sleepingEnemiesCount = sleeping;
  }

  /// <summary>
  /// Đánh thức tất cả quái vật (dùng cho debug hoặc đấu trường đặc biệt)
  /// </summary>
  public void ForceWakeAll()
  {
    foreach (var enemy in managedEnemies)
    {
      if (enemy != null) enemy.SetHibernation(false);
    }
    UpdateStatsCount();
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  private void OnDrawGizmosSelected()
  {
    if (playerTransform != null)
    {
      // Vòng tròn màu xanh lá: Phạm vi quái vật hoạt động đầy đủ
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(playerTransform.position, wakeDistance);

      // Vòng tròn màu vàng: Phạm vi ranh giới ngủ đông
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(playerTransform.position, wakeDistance + cullingBufferDistance);
    }
  }
}
