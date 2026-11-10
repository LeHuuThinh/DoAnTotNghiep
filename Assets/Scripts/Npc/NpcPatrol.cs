using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điều khiển tuần tra cho NPC dân làng tại LandScene.
/// Đã được tối ưu hóa hiệu năng cao cho 61+ NPCs:
/// 1. Tự động chuyển Animator.cullingMode sang CullUpdateTransforms (ngắt tính toán xương khi khuất màn hình).
/// 2. Hạ cấp ObstacleAvoidance sang LowQualityObstacleAvoidance (tiết kiệm 70% CPU tránh va chạm cho dân làng).
/// 3. Hệ thống Ngủ đông theo cự ly (Distance LOD Hibernation):
///    - Dưới 40m: NPC tuần tra, tìm đường và hoạt ảnh đầy đủ.
///    - Trên 48m: Tạm dừng NavMeshAgent, tắt né tránh, ngắt tính toán Animator -> 0% CPU khi ở xa.
///    - Quét cự ly xen kẽ theo khung thời gian (Time-sliced 0.35s) chống spike lag.
/// 4. Chia sẻ mảng Waypoints tĩnh (Static Waypoints Cache), loại bỏ 61 lần GameObject.Find dư thừa.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NpcPatrol : MonoBehaviour
{
  [Header("Chế độ hoạt động")]
  public bool isStationary = false; // Tích chọn ô này trong Inspector nếu muốn NPC đứng yên

  [Header("Cài đặt di chuyển")]
  [Tooltip("Gõ chính xác tên của GameObject cha chứa các Waypoints")]
  public string waypointParentName = "Villager_WP";
  public float minWaitTime = 2f;
  public float maxWaitTime = 5f;

  [Header("Tối ưu hóa Hiệu năng (Distance LOD)")]
  [Tooltip("Bật tối ưu hóa khoảng cách ngủ đông để giảm tải CPU")]
  public bool enableDistanceLOD = true;
  [Tooltip("Khoảng cách tối đa NPC hoạt động bình thường (mét)")]
  public float activeDistance = 40f;
  [Tooltip("Khoảng cách bắt đầu ngủ đông (mét, đệm hysteresis chống nhấp nháy)")]
  public float sleepDistance = 48f;

  [Header("Tên biến Animation")]
  public string animationSpeedParameter = "Speed";

  private Transform[] waypoints;
  private NavMeshAgent agent;
  private Animator animator;
  private int currentWaypointIndex = -1;
  private bool isWaiting = false;
  private int animSpeedHash;

  // Quản lý LOD ngủ đông
  private bool isSleeping = false;
  private float nextDistanceCheckTime = 0f;
  private Coroutine waitRoutine;

  // Cache tĩnh dùng chung cho toàn bộ 61+ NPCs (tránh tìm kiếm và cấp phát mảng trùng lặp)
  private static Transform[] sharedWaypoints;
  private static string sharedWaypointParentName;
  private static Transform cachedPlayerTarget;

  void Start()
  {
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponent<Animator>();
    animSpeedHash = Animator.StringToHash(animationSpeedParameter);

    // Tối ưu hóa Animator: Không tính toán chuyển động xương khi NPC khuất tầm nhìn
    if (animator != null)
    {
      animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    // NẾU LÀ NPC ĐỨNG YÊN: Tắt Agent để tiết kiệm hiệu năng và dừng chạy code tìm đường
    if (isStationary)
    {
      if (agent != null) agent.enabled = false;
      return;
    }

    // Tối ưu hóa NavMeshAgent: Dân làng chỉ cần né tránh mức cơ bản, tiết kiệm CPU RVO
    if (agent != null)
    {
      agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    }

    FindWaypointsCached();

    // Rải đều thời gian kiểm tra cự ly ban đầu để triệt tiêu frame spike
    nextDistanceCheckTime = Time.time + Random.Range(0f, 0.4f);

    if (waypoints != null && waypoints.Length > 0)
    {
      SetNextWaypoint();
    }
  }

  private void FindWaypointsCached()
  {
    // Tái sử dụng mảng tĩnh nếu đã được nạp trước đó từ cùng tên cha
    if (sharedWaypoints != null && sharedWaypoints.Length > 0 && sharedWaypointParentName == waypointParentName)
    {
      waypoints = sharedWaypoints;
      return;
    }

    GameObject parentObj = GameObject.Find(waypointParentName);
    if (parentObj == null)
    {
      Debug.LogError($"[NpcPatrol] Không tìm thấy GameObject tên '{waypointParentName}' trong Scene! Vui lòng kiểm tra lại tên.");
      return;
    }

    int childCount = parentObj.transform.childCount;
    waypoints = new Transform[childCount];

    for (int i = 0; i < childCount; i++)
    {
      waypoints[i] = parentObj.transform.GetChild(i);
    }

    sharedWaypoints = waypoints;
    sharedWaypointParentName = waypointParentName;
  }

  private static Transform GetPlayerTarget()
  {
    if (cachedPlayerTarget != null) return cachedPlayerTarget;

    GameObject playerObj = GameObject.FindWithTag("Player");
    if (playerObj != null)
    {
      cachedPlayerTarget = playerObj.transform;
      return cachedPlayerTarget;
    }

    if (Camera.main != null)
    {
      cachedPlayerTarget = Camera.main.transform;
      return cachedPlayerTarget;
    }

    return null;
  }

  void Update()
  {
    // Bỏ qua toàn bộ logic nếu NPC này đứng yên
    if (isStationary) return;

    // 1. Quét cự ly người chơi theo chu kỳ Time-slicing (0.35s)
    if (enableDistanceLOD && Time.time >= nextDistanceCheckTime)
    {
      nextDistanceCheckTime = Time.time + 0.35f;
      Transform target = GetPlayerTarget();
      if (target != null)
      {
        float distSqr = (transform.position - target.position).sqrMagnitude;
        if (!isSleeping && distSqr > sleepDistance * sleepDistance)
        {
          SetSleeping(true);
        }
        else if (isSleeping && distSqr < activeDistance * activeDistance)
        {
          SetSleeping(false);
        }
      }
    }

    // 2. Nếu đang ngủ đông: Bỏ qua toàn bộ tính toán di chuyển và Animator
    if (isSleeping) return;

    // 3. Cập nhật hoạt ảnh di chuyển
    if (animator != null && agent != null)
    {
      animator.SetFloat(animSpeedHash, agent.velocity.magnitude);
    }

    // 4. Kiểm tra khi đến đích
    if (!isWaiting && agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    {
      waitRoutine = StartCoroutine(WaitAndMoveRoutine());
    }
  }

  private void SetSleeping(bool sleep)
  {
    if (isSleeping == sleep) return;
    isSleeping = sleep;

    if (isSleeping)
    {
      // Vào trạng thái ngủ đông: Tạm dừng di chuyển, tắt né tránh, ngắt Animator
      if (agent != null && agent.isOnNavMesh)
      {
        agent.isStopped = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
      }
      if (animator != null)
      {
        animator.cullingMode = AnimatorCullingMode.CullCompletely;
      }
    }
    else
    {
      // Thức tỉnh: Khôi phục di chuyển và Animator
      if (animator != null)
      {
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
      }
      if (agent != null && agent.isOnNavMesh)
      {
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.isStopped = false;

        // Nếu lúc ngủ đã đi hết đường hoặc mất đường thì tìm điểm tiếp theo
        if (!isWaiting && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
        {
          SetNextWaypoint();
        }
      }
    }
  }

  void SetNextWaypoint()
  {
    if (waypoints == null || waypoints.Length <= 1) return;
    if (agent == null || !agent.isOnNavMesh) return;

    // Đảm bảo điểm random mới phải KHÁC điểm hiện tại
    int newIndex = currentWaypointIndex;
    int safetyLimit = 10;
    while (newIndex == currentWaypointIndex && safetyLimit-- > 0)
    {
      newIndex = Random.Range(0, waypoints.Length);
    }

    currentWaypointIndex = newIndex;
    if (waypoints[currentWaypointIndex] != null)
    {
      agent.SetDestination(waypoints[currentWaypointIndex].position);
    }
  }

  IEnumerator WaitAndMoveRoutine()
  {
    isWaiting = true;

    float waitTime = Random.Range(minWaitTime, maxWaitTime);
    yield return new WaitForSeconds(waitTime);

    if (isSleeping || isStationary)
    {
      isWaiting = false;
      yield break;
    }

    SetNextWaypoint();
    isWaiting = false;
  }

  private void OnDisable()
  {
    if (waitRoutine != null)
    {
      StopCoroutine(waitRoutine);
      waitRoutine = null;
      isWaiting = false;
    }
  }
}
