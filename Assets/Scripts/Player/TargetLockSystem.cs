using UnityEngine;
using UnityEngine.InputSystem;

public class TargetLockSystem : MonoBehaviour
{
  [Header("Detection Settings")]
  [SerializeField] private LayerMask enemyLayer;          // Chọn Layer "Enemy"
  [SerializeField] private LayerMask obstacleLayer;       // Chọn Layer "Default" / "Wall" để kiểm tra tường chắn
  [SerializeField] private float lockRange = 14f;          // Cự ly tối đa để bắt đầu khóa
  [SerializeField] private float breakLockRange = 18f;     // Cự ly quái chạy thoát sẽ tự bung khóa
  [SerializeField] private float viewAngleLimit = 75f;     // Góc quét hình nón phía trước camera

  [Header("Obstacle Occlusion")]
  [Tooltip("Thời gian quái nấp sau tường liên tục (giây) trước khi tự hủy khóa")]
  [SerializeField] private float wallLostGraceTime = 0.8f;
  private float wallOccludedTimer = 0f;

  [Header("Auto Switch Settings (Tự động đổi mục tiêu)")]
  [Tooltip("Tự động chuyển khóa sang quái tiếp theo khi quái hiện tại chết")]
  [SerializeField] private bool autoSwitchOnTargetDeath = true;
  [Tooltip("Bán kính tối đa quét tìm quái tiếp theo khi quái hiện tại chết")]
  [SerializeField] private float switchTargetRange = 16f;

  [Header("UI Reticle (Tùy chọn)")]
  [Tooltip("Kéo một icon/chấm tròn World Space UI vào đây để hiển thị tâm ngắm trên đầu quái")]
  [SerializeField] private GameObject lockOnReticle;
  [SerializeField] private Vector3 reticleOffset = new Vector3(0, 1.2f, 0);

  private Transform currentTarget;
  private Health targetHealth;
  private Health playerHealth;
  private Camera mainCamera;

  public bool IsLockedOn => currentTarget != null;
  public Transform CurrentTarget => currentTarget;

  private void Awake()
  {
    mainCamera = Camera.main;
    if (lockOnReticle != null) lockOnReticle.SetActive(false);

    playerHealth = GetComponent<Health>();
    if (playerHealth == null) playerHealth = GetComponentInParent<Health>();
  }

  private void OnEnable()
  {
    if (playerHealth != null) playerHealth.onDeath.AddListener(UnlockTarget);
  }

  private void OnDisable()
  {
    if (playerHealth != null) playerHealth.onDeath.RemoveListener(UnlockTarget);
    UnlockTarget();
  }

  private void Update()
  {
    if (playerHealth != null && playerHealth.IsDead)
    {
      if (IsLockedOn) UnlockTarget();
      return;
    }

    HandleLockInput();
    ValidateCurrentTarget();
    UpdateReticlePosition();
  }

  private void HandleLockInput()
  {
    if (playerHealth != null && playerHealth.IsDead) return;

    // Bấm Chuột giữa HOẶC phím Q để Bật/Tắt khóa mục tiêu
    bool togglePressed = false;
    if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame) togglePressed = true;

    if (togglePressed)
    {
      if (IsLockedOn)
      {
        UnlockTarget();
      }
      else
      {
        LockClosestTarget();
      }
    }
  }

  private void LockClosestTarget()
  {
    if (playerHealth != null && playerHealth.IsDead) return;

    Transform bestTarget = FindBestTarget();
    if (bestTarget != null)
    {
      SetTarget(bestTarget);
    }
  }

  /// <summary>
  /// Tìm con quái hợp lệ tối ưu nhất để khóa (ưu tiên góc nhìn và cự ly gần)
  /// </summary>
  private Transform FindBestTarget(Transform excludeTarget = null, float searchRadius = -1f)
  {
    float radius = searchRadius > 0f ? searchRadius : lockRange;
    Collider[] colliders = Physics.OverlapSphere(transform.position, radius, enemyLayer);
    Transform bestTarget = null;
    float bestScore = float.MaxValue;

    Transform camTransform = mainCamera != null ? mainCamera.transform : transform;

    foreach (Collider col in colliders)
    {
      // Ưu tiên tìm component Health trên chính collider hoặc các object cha của nó
      Health candidateHealth = col.GetComponentInParent<Health>();
      if (candidateHealth == null) candidateHealth = col.GetComponentInChildren<Health>();

      // Lấy đúng Transform của con quái (GameObject chứa Health), tránh lấy nhầm container cha nếu quái được nhóm
      Transform candidate = (candidateHealth != null) ? candidateHealth.transform : col.transform.root;

      // Bỏ qua mục tiêu bị loại trừ (ví dụ quái vừa bị tiêu diệt)
      if (excludeTarget != null && candidate == excludeTarget) continue;

      // Bỏ qua nếu là cùng 1 candidate qua nhiều collider con
      if (candidate == bestTarget) continue;

      // Kiểm tra máu: Quái chết thì bỏ qua
      if (candidateHealth != null && candidateHealth.IsDead) continue;

      // Kiểm tra góc nhìn: Nới lỏng góc nhìn khi chuyển mục tiêu (lên 85 độ) để bao quát tốt hơn
      Vector3 toCandidate = (candidate.position - camTransform.position).normalized;
      float angle = Vector3.Angle(camTransform.forward, toCandidate);
      float allowedAngle = (excludeTarget != null) ? Mathf.Min(viewAngleLimit + 15f, 85f) : viewAngleLimit;
      if (angle > allowedAngle) continue;

      // Kiểm tra chướng ngại vật: Không khóa quái bị tường che khuất
      Vector3 rayStart = transform.position + Vector3.up * 1.3f;
      Vector3 rayEnd = candidate.position + Vector3.up * 1.3f;
      if (Physics.Linecast(rayStart, rayEnd, obstacleLayer, QueryTriggerInteraction.Ignore)) continue;

      // Chấm điểm: Ưu tiên quái ở gần tâm màn hình và gần Player nhất
      float distance = Vector3.Distance(transform.position, candidate.position);
      float score = distance + angle * 0.15f;

      if (score < bestScore)
      {
        bestScore = score;
        bestTarget = candidate;
      }
    }

    return bestTarget;
  }

  /// <summary>
  /// Tự động chuyển khóa sang con quái tiếp theo khi quái hiện tại chết
  /// </summary>
  public bool TrySwitchToNextTarget()
  {
    if (playerHealth != null && playerHealth.IsDead)
    {
      UnlockTarget();
      return false;
    }

    Transform oldTarget = currentTarget;
    Transform nextTarget = FindBestTarget(excludeTarget: oldTarget, searchRadius: switchTargetRange);

    if (nextTarget != null)
    {
      SetTarget(nextTarget);
      wallOccludedTimer = 0f;
      Debug.Log($"<color=cyan>[Lock-On]</color> Quái cũ đã chết. Tự động chuyển khóa mượt sang: {nextTarget.name}");
      return true;
    }
    else
    {
      // Không còn con quái nào khác xung quanh -> Hủy khóa về lại Freecam
      UnlockTarget();
      wallOccludedTimer = 0f;
      return false;
    }
  }

  public void SetTarget(Transform newTarget)
  {
    if (newTarget != null && playerHealth != null && playerHealth.IsDead)
    {
      UnlockTarget();
      return;
    }

    currentTarget = newTarget;
    if (newTarget != null)
    {
      targetHealth = newTarget.GetComponent<Health>();
      if (targetHealth == null) targetHealth = newTarget.GetComponentInChildren<Health>();

      if (lockOnReticle != null)
      {
        lockOnReticle.SetActive(true);
        UpdateReticlePosition();
      }
    }
    else
    {
      targetHealth = null;
      if (lockOnReticle != null) lockOnReticle.SetActive(false);
    }
  }

  public void UnlockTarget()
  {
    currentTarget = null;
    targetHealth = null;
    if (lockOnReticle != null) lockOnReticle.SetActive(false);
  }

  private void ValidateCurrentTarget()
  {
    if (!IsLockedOn) return;

    // 1. Quái bị xóa khỏi scene hoặc quái đã chết -> Tự động chuyển mục tiêu tiếp theo!
    if (currentTarget == null || (targetHealth != null && targetHealth.IsDead))
    {
      if (autoSwitchOnTargetDeath)
      {
        TrySwitchToNextTarget();
      }
      else
      {
        UnlockTarget();
      }
      wallOccludedTimer = 0f;
      return;
    }

    // 2. Quái chạy quá xa -> Hủy khóa
    float distance = Vector3.Distance(transform.position, currentTarget.position);
    if (distance > breakLockRange)
    {
      UnlockTarget();
      wallOccludedTimer = 0f;
      return;
    }

    // 3. KIỂM TRA TƯỜNG CHẮN AN TOÀN (BỎ QUA TRIGGER VÀ HIỆU ỨNG CHIÊU THỨC)
    Vector3 rayStart = transform.position + Vector3.up * 1.3f;
    Vector3 rayEnd = currentTarget.position + Vector3.up * 1.3f;

    // Thêm QueryTriggerInteraction.Ignore để tia quét KHÔNG BỊ CẢN bởi kiếm khí/hitbox
    if (Physics.Linecast(rayStart, rayEnd, out RaycastHit hit, obstacleLayer, QueryTriggerInteraction.Ignore))
    {
      // Nếu vật cản không phải là bản thân Player và cũng không phải con Quái đang khóa
      bool isPlayer = hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root;
      bool isTarget = hit.transform == currentTarget || hit.transform.IsChildOf(currentTarget) || hit.transform.root == currentTarget.root;

      if (!isPlayer && !isTarget)
      {
        wallOccludedTimer += Time.deltaTime;
        if (wallOccludedTimer >= wallLostGraceTime)
        {
          UnlockTarget();
          wallOccludedTimer = 0f;
        }
        return;
      }
    }

    // Nếu tầm nhìn thông thoáng, reset bộ đếm thời gian
    wallOccludedTimer = 0f;
  }

  private void UpdateReticlePosition()
  {
    if (lockOnReticle != null && IsLockedOn)
    {
      lockOnReticle.transform.position = currentTarget.position + reticleOffset;
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(transform.position, lockRange);
  }
}
