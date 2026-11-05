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

  [Header("Elevation Settings (Giới hạn cao độ tầng/gác lửng)")]
  [Tooltip("Độ chênh lệch cao độ Y tối đa giữa Player và Quái để cho phép khóa (mét). Khuyên dùng 3.8 - 4.5m để chống ngửa camera lên trần gác lửng tầng 2 (cao 8m)")]
  [SerializeField] private float maxLockElevationDifference = 4.2f;
  [Tooltip("Độ chênh lệch cao độ Y tối đa trước khi tự động hủy khóa nếu Player/quái rơi xuống tầng khác (mét)")]
  [SerializeField] private float breakLockElevationDifference = 6.5f;

  [Header("Obstacle Occlusion")]
  [Tooltip("Thời gian quái nấp sau tường liên tục (giây) trước khi tự hủy khóa")]
  [SerializeField] private float wallLostGraceTime = 0.8f;
  private float wallOccludedTimer = 0f;

  [Header("Auto Switch Settings (Tự động đổi mục tiêu khi quái chết)")]
  [Tooltip("Tự động chuyển khóa sang quái tiếp theo khi quái hiện tại chết")]
  [SerializeField] private bool autoSwitchOnTargetDeath = true;
  [Tooltip("Bán kính tối đa quét tìm quái tiếp theo khi quái hiện tại chết")]
  [SerializeField] private float switchTargetRange = 16f;

  [Header("Manual Target Switch (Chuyển Mục Tiêu Bằng Chuột)")]
  [Tooltip("Cho phép vẩy chuột sang trái/phải để chuyển mục tiêu khi đang lock")]
  [SerializeField] private bool enableMouseFlickSwitch = true;
  [Tooltip("Ngưỡng xung lực vẩy chuột (Mouse delta X) để kích hoạt chuyển mục tiêu (khuyên dùng 18 - 25)")]
  [SerializeField] private float mouseFlickThreshold = 20f;
  [Tooltip("Thời gian hồi chiêu giữa 2 lần chuyển mục tiêu để chống trượt liên tiếp (giây)")]
  [SerializeField] private float switchCooldown = 0.25f;
  [Tooltip("Tốc độ suy hao xung lực chuột tích lũy mỗi giây (giúp lọc rê chuột chậm và bắt nhịp vẩy chuột nhanh)")]
  [SerializeField] private float flickDecayRate = 120f;
  [Tooltip("Độ lệch ngang Viewport tối thiểu để phân biệt quái bên trái/phải so với mục tiêu hiện tại")]
  [SerializeField] private float minViewportSeparation = 0.03f;

  [Header("Scroll Wheel Switch (Tùy chọn phụ)")]
  [Tooltip("Cho phép cuộn con lăn chuột lên/xuống để chuyển mục tiêu")]
  [SerializeField] private bool enableScrollWheelSwitch = false;
  [Tooltip("Ngưỡng cuộn con lăn chuột để nhận lệnh")]
  [SerializeField] private float scrollThreshold = 0.5f;

  [Header("UI Reticle (Tùy chọn)")]
  [Tooltip("Kéo một icon/chấm tròn World Space UI vào đây để hiển thị tâm ngắm trên đầu quái")]
  [SerializeField] private GameObject lockOnReticle;
  [SerializeField] private Vector3 reticleOffset = new Vector3(0, 1.2f, 0);

  private Transform currentTarget;
  private Health targetHealth;
  private Health playerHealth;
  private Camera mainCamera;

  // Quản lý chuyển mục tiêu thủ công bằng chuột
  private float accumulatedFlickX = 0f;
  private float switchCooldownTimer = 0f;

  public bool IsLockedOn => currentTarget != null;
  public Transform CurrentTarget => currentTarget;

  private void OnValidate()
  {
    if (breakLockRange <= lockRange)
    {
      breakLockRange = lockRange + 4f;
    }
    if (breakLockElevationDifference <= maxLockElevationDifference)
    {
      breakLockElevationDifference = maxLockElevationDifference + 2.5f;
    }
  }

  private void Awake()
  {
    if (breakLockRange <= lockRange)
    {
      breakLockRange = lockRange + 4f;
    }
    if (breakLockElevationDifference <= maxLockElevationDifference)
    {
      breakLockElevationDifference = maxLockElevationDifference + 2.5f;
    }

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

    if (switchCooldownTimer > 0f)
    {
      switchCooldownTimer -= Time.deltaTime;
    }

    HandleLockInput();
    HandleMouseSwitchInput();
    ValidateCurrentTarget();
    UpdateReticlePosition();
  }

  private void HandleLockInput()
  {
    if (playerHealth != null && playerHealth.IsDead) return;

    // Bấm Chuột giữa để Bật/Tắt khóa mục tiêu
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

  /// <summary>
  /// Lắng nghe cử chỉ chuột (vẩy chuột hoặc cuộn con lăn) để chuyển mục tiêu khi đang Lock
  /// </summary>
  private void HandleMouseSwitchInput()
  {
    if (!IsLockedOn)
    {
      accumulatedFlickX = 0f;
      return;
    }

    if (playerHealth != null && playerHealth.IsDead) return;

    if (switchCooldownTimer > 0f)
    {
      accumulatedFlickX = 0f;
      return;
    }

    if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
    {
      accumulatedFlickX = 0f;
      return;
    }

    // 1. Tùy chọn chuyển mục tiêu bằng con lăn chuột (Scroll Wheel)
    if (enableScrollWheelSwitch)
    {
      float scrollY = Mouse.current.scroll.ReadValue().y;
      if (Mathf.Abs(scrollY) > scrollThreshold)
      {
        bool toRight = scrollY < 0f; // Cuộn xuống = sang phải, cuộn lên = sang trái
        if (TrySwitchTargetDirection(toRight))
        {
          switchCooldownTimer = switchCooldown;
          accumulatedFlickX = 0f;
          return;
        }
      }
    }

    // 2. Chuyển mục tiêu bằng vẩy chuột (Mouse Flick)
    if (enableMouseFlickSwitch)
    {
      float deltaX = Mouse.current.delta.ReadValue().x;

      // Nếu đột ngột đổi chiều vẩy chuột thì reset xung lực cũ để bắt nhịp tức thì
      if ((accumulatedFlickX > 0f && deltaX < -5f) || (accumulatedFlickX < 0f && deltaX > 5f))
      {
        accumulatedFlickX = deltaX;
      }
      else
      {
        // Suy hao lực tích lũy về 0 theo thời gian
        accumulatedFlickX = Mathf.MoveTowards(accumulatedFlickX, 0f, flickDecayRate * Time.deltaTime);
        accumulatedFlickX += deltaX;
      }

      // Kiểm tra xem xung lực vẩy chuột đã vượt ngưỡng kích hoạt chưa
      if (Mathf.Abs(accumulatedFlickX) >= mouseFlickThreshold)
      {
        bool toRight = accumulatedFlickX > 0f;
        bool switched = TrySwitchTargetDirection(toRight);

        // Luôn giải phóng xung lực tích lũy sau cú vẩy
        accumulatedFlickX = 0f;

        if (switched)
        {
          switchCooldownTimer = switchCooldown;
        }
      }
    }
  }

  /// <summary>
  /// Chuyển mục tiêu sang quái gần nhất ở phía bên Phải (toRight = true) hoặc bên Trái (toRight = false) trên màn hình
  /// </summary>
  public bool TrySwitchTargetDirection(bool toRight)
  {
    if (currentTarget == null) return false;
    if (mainCamera == null) mainCamera = Camera.main;
    if (mainCamera == null) return false;

    // Vị trí Viewport của mục tiêu hiện tại trên màn hình
    Vector3 curVp = mainCamera.WorldToViewportPoint(currentTarget.position + Vector3.up * 1.2f);

    Collider[] colliders = Physics.OverlapSphere(transform.position, switchTargetRange, enemyLayer);
    Transform bestCandidate = null;
    float bestScore = float.MaxValue;

    foreach (Collider col in colliders)
    {
      Health candidateHealth = col.GetComponentInParent<Health>();
      if (candidateHealth == null) candidateHealth = col.GetComponentInChildren<Health>();

      Transform candidate = (candidateHealth != null) ? candidateHealth.transform : col.transform.root;

      // Bỏ qua mục tiêu hiện tại và candidate trùng lặp
      if (candidate == currentTarget || candidate == bestCandidate) continue;

      // Bỏ qua nếu quái đã chết
      if (candidateHealth != null && candidateHealth.IsDead) continue;

      // KIỂM TRA ĐỘ CAO (ELEVATION CHECK): Bỏ qua quái ở tầng khác (gác lửng tầng 2)
      float heightDelta = Mathf.Abs(candidate.position.y - transform.position.y);
      if (heightDelta > maxLockElevationDifference) continue;

      // Kiểm tra vị trí Viewport trên màn hình
      Vector3 candPos = candidate.position + Vector3.up * 1.2f;
      Vector3 candVp = mainCamera.WorldToViewportPoint(candPos);

      // Quái phải nằm ở phía trước Camera
      if (candVp.z <= 0.1f) continue;

      // Cho phép biên độ rộng hơn viền màn hình một chút ([-0.2, 1.2]) để bắt cả quái vừa lọt khỏi mép
      if (candVp.x < -0.2f || candVp.x > 1.2f || candVp.y < -0.2f || candVp.y > 1.2f) continue;

      // Kiểm tra hướng Trái / Phải
      float xDiff = toRight ? (candVp.x - curVp.x) : (curVp.x - candVp.x);
      if (xDiff < minViewportSeparation) continue; // Phải nằm hẳn về phía mong muốn

      // Kiểm tra tường chắn
      Vector3 rayStart = transform.position + Vector3.up * 1.3f;
      Vector3 rayEnd = candidate.position + Vector3.up * 1.3f;
      if (Physics.Linecast(rayStart, rayEnd, obstacleLayer, QueryTriggerInteraction.Ignore)) continue;

      // Chấm điểm: Ưu tiên quái ở gần hướng vẩy trên màn hình nhất (xDiff nhỏ nhất), sau đó xét cự ly 3D
      float distance = Vector3.Distance(transform.position, candidate.position);
      float score = xDiff * 15f + (distance / switchTargetRange) * 5f + heightDelta * 0.5f;

      if (score < bestScore)
      {
        bestScore = score;
        bestCandidate = candidate;
      }
    }

    if (bestCandidate != null)
    {
      SetTarget(bestCandidate);
      wallOccludedTimer = 0f;
      string dirStr = toRight ? "Phải ➔" : "⬅ Trái";
      Debug.Log($"<color=cyan>[Lock-On]</color> Vẩy chuột {dirStr}: Chuyển khóa sang: <b>{bestCandidate.name}</b>");
      return true;
    }

    return false;
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

      // KIỂM TRA ĐỘ CAO (ELEVATION CHECK): Bỏ qua quái ở tầng khác (gác lửng tầng 2 cách 8m)
      float heightDelta = Mathf.Abs(candidate.position.y - transform.position.y);
      if (heightDelta > maxLockElevationDifference) continue;

      // Kiểm tra góc nhìn: Nới lỏng góc nhìn khi chuyển mục tiêu (lên 85 độ) để bao quát tốt hơn
      Vector3 toCandidate = (candidate.position - camTransform.position).normalized;
      float angle = Vector3.Angle(camTransform.forward, toCandidate);
      float allowedAngle = (excludeTarget != null) ? Mathf.Min(viewAngleLimit + 15f, 85f) : viewAngleLimit;
      if (angle > allowedAngle) continue;

      // Kiểm tra chướng ngại vật: Không khóa quái bị tường che khuất
      Vector3 rayStart = transform.position + Vector3.up * 1.3f;
      Vector3 rayEnd = candidate.position + Vector3.up * 1.3f;
      if (Physics.Linecast(rayStart, rayEnd, obstacleLayer, QueryTriggerInteraction.Ignore)) continue;

      // Chấm điểm: Ưu tiên quái ở gần tâm màn hình, gần Player nhất và ít chênh lệch độ cao
      float distance = Vector3.Distance(transform.position, candidate.position);
      float score = distance + angle * 0.15f + heightDelta * 0.8f;

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

    // 2b. Quái hoặc Player rơi sang tầng khác (chênh lệch độ cao quá lớn) -> Hủy khóa
    float heightDelta = Mathf.Abs(currentTarget.position.y - transform.position.y);
    if (heightDelta > breakLockElevationDifference)
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
    Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
    Gizmos.DrawWireSphere(transform.position, switchTargetRange);

    // Vẽ hình khối giới hạn cao độ cho phép Lock-On
    Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
    Gizmos.DrawWireCube(transform.position, new Vector3(lockRange * 2f, maxLockElevationDifference * 2f, lockRange * 2f));
  }
}
