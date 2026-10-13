using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Đã sửa Namespace cho Coroutine

[RequireComponent(typeof(NavMeshAgent))] // Đảm bảo luôn có component này
public class NpcPatrol : MonoBehaviour
{
  [Header("Chế độ hoạt động")]
  public bool isStationary = false; // Tích chọn ô này trong Inspector nếu muốn NPC đứng yên

  [Header("Cài đặt di chuyển")]
  [Tooltip("Gõ chính xác tên của GameObject cha chứa các Waypoints")]
  public string waypointParentName = "Villager_WP"; // Tên mặc định của bạn
  public float minWaitTime = 2f;
  public float maxWaitTime = 5f;

  private Transform[] waypoints;

  [Header("Tên biến Animation")]
  public string animationSpeedParameter = "Speed";

  private NavMeshAgent agent;
  private Animator animator;
  private int currentWaypointIndex = -1; // Để -1 để lần đầu random không bị trùng
  private bool isWaiting = false;

  // Biến lưu mã Hash để tối ưu hiệu năng Animator
  private int animSpeedHash;

  void Start()
  {
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponent<Animator>();
    animSpeedHash = Animator.StringToHash(animationSpeedParameter);

    // NẾU LÀ NPC ĐỨNG YÊN: Tắt Agent để tiết kiệm hiệu năng và dừng chạy code tìm đường
    if (isStationary)
    {
      if (agent != null) agent.enabled = false;
      return;
    }

    FindWaypoints();

    if (waypoints != null && waypoints.Length > 0)
    {
      SetNextWaypoint();
    }
  }

  private void FindWaypoints()
  {
    // 1. Tìm GameObject cha trong Scene theo tên
    GameObject parentObj = GameObject.Find(waypointParentName);

    if (parentObj == null)
    {
      Debug.LogError($"Không tìm thấy GameObject tên '{waypointParentName}' trong Scene! Vui lòng kiểm tra lại tên.");
      return;
    }

    // 2. Lấy số lượng Waypoint con bên trong (không tính chính object cha)
    int childCount = parentObj.transform.childCount;
    waypoints = new Transform[childCount];

    // 3. Đổ tất cả Transform của các con vào mảng
    for (int i = 0; i < childCount; i++)
    {
      waypoints[i] = parentObj.transform.GetChild(i);
    }

    // Tùy chọn: In ra console để kiểm tra xem đã nhận đủ chưa
    // Debug.Log($"{gameObject.name} đã tải thành công {waypoints.Length} Waypoints từ {waypointParentName}.");
  }

  void Update()
  {
    // Bỏ qua toàn bộ logic tìm đường bên dưới nếu NPC này đứng yên
    if (isStationary) return;

    if (animator != null)
    {
      animator.SetFloat(animSpeedHash, agent.velocity.magnitude);
    }

    if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    {
      StartCoroutine(WaitAndMoveRoutine());
    }
  }

  void SetNextWaypoint()
  {
    if (waypoints.Length <= 1) return;

    // Đảm bảo điểm random mới phải KHÁC điểm hiện tại
    int newIndex = currentWaypointIndex;
    while (newIndex == currentWaypointIndex)
    {
      newIndex = Random.Range(0, waypoints.Length);
    }

    currentWaypointIndex = newIndex;
    agent.SetDestination(waypoints[currentWaypointIndex].position);
  }

  IEnumerator WaitAndMoveRoutine()
  {
    isWaiting = true;

    float waitTime = Random.Range(minWaitTime, maxWaitTime);
    yield return new WaitForSeconds(waitTime);

    SetNextWaypoint();
    isWaiting = false;
  }
}