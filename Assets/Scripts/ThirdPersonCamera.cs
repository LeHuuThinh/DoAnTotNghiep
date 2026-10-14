using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
  [Header("Target Tracking")]
  [SerializeField] private Transform target;           // Kéo Player vào đây
  [SerializeField] private float cameraLookHeight = 1.3f;
  [SerializeField] private float cameraHeight = 2.2f;
  [SerializeField] private float cameraSideOffset = 0.85f;
  [SerializeField] private float freeCamFollowSmoothTime = 0.06f; // Tốc độ bám theo Player lúc đi dạo tự do

  [Header("Orbit & Distance (Free Look)")]
  [SerializeField] private float cameraDistance = 5f;
  [SerializeField] private float minZoomDistance = 2f;
  [SerializeField] private float maxZoomDistance = 8f;
  [SerializeField] private float zoomSpeed = 2f;

  [Header("Sensitivity & Limits")]
  [SerializeField] private float mouseSensitivity = 0.12f;
  [SerializeField] private float minPitch = -25f;
  [SerializeField] private float maxPitch = 65f;

  [Header("Lock-On Comfort & Framing")]
  [Tooltip("Độ lệch qua vai phải khi khóa mục tiêu")]
  [SerializeField] private float lockOnSideOffset = 0.85f;
  [Tooltip("Độ cao riêng của camera khi Lock-on (khuyên dùng 2.5 - 2.7)")]
  [SerializeField] private float lockOnCameraHeight = 2.7f;
  [Tooltip("Khoảng cách lùi xa hơn khi khóa để góc nhìn rộng, đỡ ngợp")]
  [SerializeField] private float lockOnDistance = 5.8f;
  [Tooltip("Độ cao điểm ngắm trên người quái")]
  [SerializeField] private float lockOnEnemyLookHeight = 1.2f;

  [Header("Combat Responsiveness (Độ nhạy khi ĐANG chiến đấu)")]
  [Tooltip("Độ mượt xoay khi ĐANG chiến đấu (0.2s giúp bám sát mục tiêu)")]
  [SerializeField] private float lockOnRotationSmoothTime = 0.20f;
  [Tooltip("Độ mượt vị trí khi ĐANG chiến đấu (0.14 - 0.16s giúp bám sát nhịp strafe)")]
  [SerializeField] private float lockOnPositionSmoothTime = 0.15f;

  [Header("Transition Smoothing (Độ mượt khi Chuyển Chế Độ)")]
  [Tooltip("Thời gian chuyển mượt khi BẮT ĐẦU khóa mục tiêu (giây)")]
  [SerializeField] private float enterTransitionDuration = 0.35f;
  [Tooltip("Độ êm vị trí khi mới bấm khóa (0.35s giúp lia vào êm ái)")]
  [SerializeField] private float enterPositionSmoothTime = 0.35f;
  [Tooltip("Độ êm góc xoay khi mới bấm khóa (0.35s giúp không bị lia góc nhìn quá gắt)")]
  [SerializeField] private float enterRotationSmoothTime = 0.35f;

  [Tooltip("Thời gian chuyển mượt khi THOÁT khóa mục tiêu về Freecam (giây)")]
  [SerializeField] private float exitTransitionDuration = 0.35f;
  [Tooltip("Độ êm vị trí khi thoát khóa (0.30s giúp trượt về êm ái)")]
  [SerializeField] private float exitPositionSmoothTime = 0.30f;

  private TargetLockSystem lockSystem;
  private float cameraYaw;
  private float cameraPitch = 15f;
  private float currentHeight;
  private float heightVelocity;

  // Quản lý chuyển cảnh
  private bool wasLockedOn = false;
  private float enterTransitionTimer = 0f;
  private float exitTransitionTimer = 0f;

  // Vận tốc cho SmoothDamp
  private float yawVelocity;
  private float pitchVelocity;
  private Vector3 positionVelocity;

  public float CameraYaw => cameraYaw;
  public Quaternion PlanarRotation => Quaternion.Euler(0f, cameraYaw, 0f);

  private void Awake()
  {
    currentHeight = cameraHeight;
    cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);

    if (target != null)
    {
      cameraYaw = target.eulerAngles.y;
      lockSystem = target.GetComponent<TargetLockSystem>();
      if (lockSystem == null) lockSystem = target.GetComponentInChildren<TargetLockSystem>();
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }

  private void Update()
  {
    HandleCursorLock();

    // Chỉ nhận chuột xoay camera tự do khi KHÔNG ở trong trạng thái khóa mục tiêu
    if (lockSystem == null || !lockSystem.IsLockedOn)
    {
      HandleFreeCameraInput();
    }
  }

  private void LateUpdate()
  {
    if (target == null) return;

    bool isCurrentlyLockedOn = lockSystem != null && lockSystem.IsLockedOn && lockSystem.CurrentTarget != null;

    // =========================================================================
    // 1. PHÁT HIỆN THỜI ĐIỂM CHUYỂN TRẠNG THÁI (EDGE DETECTION)
    // =========================================================================
    if (isCurrentlyLockedOn && !wasLockedOn)
    {
      // VỪA BẬT LOCK-ON: Bắt đầu chuyển cảnh êm ái vào combat
      enterTransitionTimer = enterTransitionDuration;
      positionVelocity = Vector3.zero;
      yawVelocity = 0f;
      pitchVelocity = 0f;
    }
    else if (!isCurrentlyLockedOn && wasLockedOn)
    {
      // VỪA THOÁT LOCK-ON: Bắt đầu chuyển cảnh êm ái về lại Freecam
      exitTransitionTimer = exitTransitionDuration;
      positionVelocity = Vector3.zero;

      // Đồng bộ góc nhìn Freecam khớp 100% với góc camera hiện tại để triệt tiêu cú giật góc quay
      cameraYaw = transform.eulerAngles.y;
      float currentP = transform.eulerAngles.x;
      if (currentP > 180f) currentP -= 360f;
      cameraPitch = Mathf.Clamp(currentP, minPitch, maxPitch);

      yawVelocity = 0f;
      pitchVelocity = 0f;
    }

    wasLockedOn = isCurrentlyLockedOn;

    // Chuyển đổi mượt mà độ cao giữa 2 chế độ (1.4m <-> 2.7m)
    float targetHeight = isCurrentlyLockedOn ? lockOnCameraHeight : cameraHeight;
    currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, 0.25f);

    // =========================================================================
    // 2. THỰC THI CHẾ ĐỘ CAMERA TƯƠNG ỨNG
    // =========================================================================
    if (isCurrentlyLockedOn)
    {
      UpdateLockOnCamera();
    }
    else
    {
      UpdateFreeCamera();
    }
  }

  // =========================================================================
  // XỬ LÝ CAMERA KHI KHÓA MỤC TIÊU (LOCK-ON)
  // =========================================================================
  private void UpdateLockOnCamera()
  {
    Transform enemy = lockSystem.CurrentTarget;
    if (enemy == null) return;

    // Hướng phẳng từ Player tới Quái
    Vector3 playerToEnemy = enemy.position - target.position;
    playerToEnemy.y = 0f;
    if (playerToEnemy.sqrMagnitude < 0.001f) playerToEnemy = target.forward;

    Quaternion enemyFacingRot = Quaternion.LookRotation(playerToEnemy);

    // Vị trí mong muốn của camera khi khóa
    Vector3 targetPivot = target.position + Vector3.up * cameraLookHeight;
    Vector3 lockOnOffset = new Vector3(lockOnSideOffset, currentHeight - cameraLookHeight, -lockOnDistance);
    Vector3 desiredPosition = targetPivot + (enemyFacingRot * lockOnOffset);

    // Tính toán độ mượt: Mới vào thì chuyển động êm (0.35s), sau đó siết dần về nhạy bén (0.15s)
    float currentPosSmooth = lockOnPositionSmoothTime;
    float currentRotSmooth = lockOnRotationSmoothTime;

    if (enterTransitionTimer > 0f)
    {
      enterTransitionTimer -= Time.deltaTime;
      float t = Mathf.Clamp01(1f - (enterTransitionTimer / enterTransitionDuration));
      currentPosSmooth = Mathf.Lerp(enterPositionSmoothTime, lockOnPositionSmoothTime, t);
      currentRotSmooth = Mathf.Lerp(enterRotationSmoothTime, lockOnRotationSmoothTime, t);
    }

    // Di chuyển vị trí camera
    transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, currentPosSmooth);

    // Tính điểm ngắm (Framing 65% nghiêng về phía quái)
    Vector3 aimTarget = Vector3.Lerp(
        target.position + Vector3.up * cameraLookHeight,
        enemy.position + Vector3.up * lockOnEnemyLookHeight,
        0.65f
    );

    Vector3 lookDirection = (aimTarget - transform.position).normalized;
    Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
    Vector3 desiredEuler = desiredRotation.eulerAngles;

    float targetPitch = desiredEuler.x;
    if (targetPitch > 180f) targetPitch -= 360f;
    targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

    // Xoay góc nhìn mượt mà
    cameraYaw = Mathf.SmoothDampAngle(cameraYaw, desiredEuler.y, ref yawVelocity, currentRotSmooth);
    cameraPitch = Mathf.SmoothDampAngle(cameraPitch, targetPitch, ref pitchVelocity, currentRotSmooth);

    transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
  }

  // =========================================================================
  // XỬ LÝ CAMERA TỰ DO BÌNH THƯỜNG (FREE LOOK)
  // =========================================================================
  private void UpdateFreeCamera()
  {
    Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    Vector3 targetPosition = target.position + Vector3.up * cameraLookHeight;
    Vector3 localOffset = new Vector3(cameraSideOffset, currentHeight - cameraLookHeight, -cameraDistance);
    Vector3 desiredPosition = targetPosition + orbitRotation * localOffset;

    // Trong giai đoạn thoát khóa mục tiêu: Hòa trộn mượt mà cả vị trí lẫn góc xoay
    if (exitTransitionTimer > 0f)
    {
      exitTransitionTimer -= Time.deltaTime;
      float t = Mathf.Clamp01(1f - (exitTransitionTimer / exitTransitionDuration));
      float currentPosSmooth = Mathf.Lerp(exitPositionSmoothTime, freeCamFollowSmoothTime, t);

      // Trượt vị trí mượt mà từ combat về freecam
      transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, currentPosSmooth);

      // Xoay từ từ góc nhìn sang orbit tự do
      transform.rotation = Quaternion.Slerp(transform.rotation, orbitRotation, 14f * Time.deltaTime);
    }
    else
    {
      // Khi đã về hẳn Freecam: Bám theo êm ái và xoay góc nhìn tự do tức thì theo chuột
      transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, freeCamFollowSmoothTime);
      transform.rotation = orbitRotation;
    }
  }

  private void HandleFreeCameraInput()
  {
    if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
    cameraYaw += mouseDelta.x * mouseSensitivity;
    cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);

    float scrollDelta = Mouse.current.scroll.ReadValue().y;
    cameraDistance = Mathf.Clamp(cameraDistance - scrollDelta * zoomSpeed * 0.01f, minZoomDistance, maxZoomDistance);
  }

  private void HandleCursorLock()
  {
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    {
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
    }

    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }
  }
}