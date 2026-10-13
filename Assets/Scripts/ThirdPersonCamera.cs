using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
  [Header("Target Tracking")]
  [SerializeField] private Transform target;           // Kéo Player vào đây
  [SerializeField] private float cameraFollowSpeed = 12f;
  [SerializeField] private float cameraLookHeight = 1.3f;
  [SerializeField] private float cameraHeight = 2.2f;
  [SerializeField] private float cameraSideOffset = 0.85f;

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
  [Tooltip("Thời gian giảm chấn xoay (0.2 - 0.25s giúp triệt tiêu rung giật khi Strafe)")]
  [SerializeField] private float lockOnRotationSmoothTime = 0.22f;
  [Tooltip("Thời gian giảm chấn vị trí di chuyển của camera")]
  [SerializeField] private float lockOnPositionSmoothTime = 0.15f;

  private TargetLockSystem lockSystem;
  private float cameraYaw;
  private float cameraPitch = 15f;
  private float currentHeight;
  private float heightVelocity;

  // Các biến vận tốc phục vụ SmoothDamp (Chống chóng mặt)
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
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }

  private void Update()
  {
    HandleCursorLock();

    if (lockSystem == null || !lockSystem.IsLockedOn)
    {
      HandleFreeCameraInput();
    }
  }

  private void LateUpdate()
  {
    if (target == null) return;

    // Tự động chuyển đổi mượt mà độ cao giữa 2 chế độ (1.4m <-> 2.6m)
    float targetHeight = (lockSystem != null && lockSystem.IsLockedOn) ? lockOnCameraHeight : cameraHeight;
    currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, 0.2f);

    if (lockSystem != null && lockSystem.IsLockedOn)
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

    // 1. Tính hướng phẳng từ Player tới Quái
    Vector3 playerToEnemy = enemy.position - target.position;
    playerToEnemy.y = 0f;
    if (playerToEnemy.sqrMagnitude < 0.001f) playerToEnemy = target.forward;

    Quaternion enemyFacingRot = Quaternion.LookRotation(playerToEnemy);

    // 2. Đặt vị trí Camera: Lùi ra sau, lệch hẳn qua vai phải của Player
    Vector3 targetPivot = target.position + Vector3.up * cameraLookHeight;
    Vector3 lockOnOffset = new Vector3(lockOnSideOffset, currentHeight - cameraLookHeight, -lockOnDistance);
    Vector3 desiredPosition = targetPivot + (enemyFacingRot * lockOnOffset);

    // Lướt vị trí mượt mà bằng SmoothDamp thay vì Lerp
    transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, lockOnPositionSmoothTime);

    // 3. Tính điểm ngắm (Framing): Ngắm vào điểm 65% nghiêng về phía quái
    // Giúp quái nằm ngay giữa/phải khung hình, Player nằm gọn gàng bên góc dưới trái
    Vector3 aimTarget = Vector3.Lerp(
        target.position + Vector3.up * cameraLookHeight,
        enemy.position + Vector3.up * lockOnEnemyLookHeight,
        0.65f
    );

    Vector3 lookDirection = (aimTarget - transform.position).normalized;
    Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);

    Vector3 desiredEuler = desiredRotation.eulerAngles;

    // 4. Khử rung xoay góc nhìn bằng SmoothDampAngle
    cameraYaw = Mathf.SmoothDampAngle(cameraYaw, desiredEuler.y, ref yawVelocity, lockOnRotationSmoothTime);
    cameraPitch = Mathf.SmoothDampAngle(cameraPitch, desiredEuler.x, ref pitchVelocity, lockOnRotationSmoothTime);

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

    transform.position = Vector3.Lerp(transform.position, desiredPosition, cameraFollowSpeed * Time.deltaTime);
    transform.rotation = orbitRotation;
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