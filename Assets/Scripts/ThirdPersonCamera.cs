using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
  [Header("Target Tracking & Framing (Điểm Ngắm & Độ Cao)")]
  [Tooltip("Kéo Player (Transform gốc) vào đây")]
  [SerializeField] private Transform target;           // Kéo Player vào đây
  [Tooltip("Độ cao điểm ngắm ngang ngực & mặt Player (khuyên dùng 1.4 - 1.6m cho nhân vật cao ~2m). Giúp camera luôn nhìn ngang người, không bao giờ bị tụt xuống chân")]
  [SerializeField] private float cameraLookHeight = 1.5f;
  [Tooltip("Độ cao Camera ở cự ly bình thường (khuyên dùng 1.8 - 2.2m)")]
  [SerializeField] private float cameraHeight = 1.8f;
  [Tooltip("Độ lệch qua vai phải (Over-the-shoulder offset, khuyên dùng 0.7 - 0.9m)")]
  [SerializeField] private float cameraSideOffset = 0.85f;
  [Tooltip("Tốc độ bám theo Player lúc đi dạo tự do (khuyên dùng 0.04 - 0.08s)")]
  [SerializeField] private float freeCamFollowSmoothTime = 0.06f;

  [Header("Orbit & Distance (Khoảng Cách & Zoom Tự Do)")]
  [Tooltip("Khoảng cách mặc định giữa camera và Player (khuyên dùng 4.0 - 5.0m)")]
  [SerializeField] private float cameraDistance = 4.5f;
  [Tooltip("Cự ly zoom gần nhất bằng con lăn chuột (khuyên dùng 2.0 - 2.5m)")]
  [SerializeField] private float minZoomDistance = 2f;
  [Tooltip("Cự ly zoom xa nhất bằng con lăn chuột (khuyên dùng 6.0 - 8.0m)")]
  [SerializeField] private float maxZoomDistance = 6f;
  [Tooltip("Tốc độ zoom khi cuộn con lăn chuột")]
  [SerializeField] private float zoomSpeed = 30f;

  [Header("Sensitivity & Limits (Độ Nhạy & Giới Hạn Góc Xoay)")]
  [Tooltip("Độ nhạy lia chuột xoay góc nhìn")]
  [SerializeField] private float mouseSensitivity = 0.08f;
  [Tooltip("Góc chúc camera xuống tối đa khi lia chuột lên (độ, ví dụ -25)")]
  [SerializeField] private float minPitch = -25f;
  [Tooltip("Góc ngẩng camera lên cao tối đa khi lia chuột xuống (độ, ví dụ 65)")]
  [SerializeField] private float maxPitch = 65f;

  [Header("Lock-On Comfort & Framing (Khi Khóa Mục Tiêu Chiến Đấu)")]
  [Tooltip("Độ lệch qua vai phải khi khóa mục tiêu (khuyên dùng 0.7 - 0.9m)")]
  [SerializeField] private float lockOnSideOffset = 0.85f;
  [Tooltip("Độ cao riêng của camera khi Lock-on (khuyên dùng 2.4 - 2.7m để bao quát trận đấu)")]
  [SerializeField] private float lockOnCameraHeight = 2.6f;
  [Tooltip("Khoảng cách lùi xa hơn khi khóa để góc nhìn rộng, bao quát đấu trường (khuyên dùng 4.5 - 5.5m)")]
  [SerializeField] private float lockOnDistance = 5.2f;
  [Tooltip("Độ cao điểm ngắm trên người quái (khuyên dùng 1.2 - 1.4m)")]
  [SerializeField] private float lockOnEnemyLookHeight = 1.3f;

  [Header("Combat Responsiveness (Độ Nhạy Bám Sát Khi Đang Đánh Nhau)")]
  [Tooltip("Độ mượt xoay bám theo quái (0.18 - 0.22s giúp lia theo nhịp nhảy/lướt của địch)")]
  [SerializeField] private float lockOnRotationSmoothTime = 0.20f;
  [Tooltip("Độ mượt vị trí khi strafe vờn quái (0.14 - 0.16s giúp bám sát bước chân)")]
  [SerializeField] private float lockOnPositionSmoothTime = 0.15f;

  [Header("Transition Smoothing (Độ Mượt Chuyển Đổi Chế Độ)")]
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
  [Tooltip("Thời gian chuyển camera mượt mà khi đổi sang mục tiêu khác (giây)")]
  [SerializeField] private float targetSwitchSmoothDuration = 0.35f;

  [Header("Close-Up & Wall Framing (Cân Chỉnh Góc Nhìn Khi Sát Tường)")]
  [Tooltip("Độ nâng camera lên cao thêm khi bị tường ép sát (khuyên dùng 0.2 - 0.35m) để luôn nhìn ngang ngực & mặt, không bị tụt xuống chân")]
  [SerializeField] private float closeUpHeightBoost = 0.25f;
  [Tooltip("Giảm bớt góc chúc xuống khi camera áp sát Player (độ), giúp mặt và ngực nhân vật luôn ở giữa màn hình")]
  [SerializeField] private float closeUpPitchCompensation = 8f;

  [Header("Parry Camera Impact (Hiệu Ứng Zoom Khi Parry Thành Công)")]
  [Tooltip("Độ thu hẹp FOV khi Parry (giảm FOV giúp tạo cảm giác giật lực mạnh mẽ)")]
  [SerializeField] private float parryFovZoom = 8f;
  [Tooltip("Khoảng cách kéo camera áp sát mục tiêu khi Parry (mét)")]
  [SerializeField] private float parryDistanceZoom = 0.9f;
  [Tooltip("Thời gian camera lao vào (giây thực tế)")]
  [SerializeField] private float parryZoomInDuration = 0.05f;
  [Tooltip("Thời gian camera giữ ở điểm zoom gần nhất (giây thực tế)")]
  [SerializeField] private float parryZoomHoldDuration = 0.12f;
  [Tooltip("Thời gian camera trượt êm ái về lại vị trí cũ (giây thực tế)")]
  [SerializeField] private float parryZoomOutDuration = 0.25f;

  [Header("Camera Collision & Anti-Clipping (Chống Xuyên Tường & Sàn)")]
  [Tooltip("Các Layer môi trường vật lý (Tường, Sàn, Trần, Chướng ngại vật) mà Camera không được xuyên qua")]
  [SerializeField] private LayerMask collisionLayers;
  [Tooltip("Bán kính hình cầu để quét va chạm vật lý (SphereCast). Khuyên dùng 0.2 - 0.25m để camera không lọt khe hoặc xuyên góc tường")]
  [SerializeField] private float cameraCollisionRadius = 0.22f;
  [Tooltip("Khoảng đệm an toàn lùi ra khỏi mặt phẳng va chạm để tránh Near Clip Plane cắt vào tường")]
  [SerializeField] private float collisionBuffer = 0.12f;
  [Tooltip("Khoảng cách tối thiểu từ Camera tới nhân vật khi bị ép sát tường (khuyên dùng 0.5 - 0.8m)")]
  [SerializeField] private float minCameraDistance = 0.6f;
  [Tooltip("Tốc độ camera thu vào khi va vào tường (nhỏ = tức thì để không lộ mặt sau của mesh)")]
  [SerializeField] private float collisionPushSmoothTime = 0.03f;
  [Tooltip("Tốc độ camera nới ra xa lại khi rời khỏi tường (mượt mà, không bị giật nảy)")]
  [SerializeField] private float collisionReturnSmoothTime = 0.22f;
  [Tooltip("Tự động tối ưu Near Clip Plane của Camera để không cắt mặt phẳng khi đứng sát (0.1 - 0.15m)")]
  [SerializeField] private float cameraNearClipPlane = 0.12f;

  private TargetLockSystem lockSystem;
  private Camera cam;
  private float baseFov = 60f;
  private float currentParryDistanceOffset = 0f;
  private float currentParryFovOffset = 0f;
  private Coroutine parryZoomCoroutine;

  private float cameraYaw;
  private float cameraPitch = 15f;
  private float currentHeight;
  private float heightVelocity;

  // Quản lý va chạm camera
  private float currentCollisionDistance;
  private float collisionDistanceVelocity;
  private readonly RaycastHit[] sphereHits = new RaycastHit[8];
  private readonly RaycastHit[] rayHits = new RaycastHit[8];

  // Quản lý chuyển cảnh
  private bool wasLockedOn = false;
  private Transform previousLockedEnemy;
  private float enterTransitionTimer = 0f;
  private float exitTransitionTimer = 0f;

  // Vận tốc cho SmoothDamp
  private float yawVelocity;
  private float pitchVelocity;
  private Vector3 positionVelocity;

  public float CameraYaw => cameraYaw;
  public Quaternion PlanarRotation => Quaternion.Euler(0f, cameraYaw, 0f);

  private void Reset()
  {
    collisionLayers = LayerMask.GetMask("Default", "Terrain", "Obstacle", "NavMessFloor");
    if (collisionLayers.value == 0)
    {
      collisionLayers = ~LayerMask.GetMask("Player", "Enemy", "Ignore Raycast", "UI", "TransparentFX", "Item");
    }

    int itemLayerIndex = LayerMask.NameToLayer("Item");
    if (itemLayerIndex >= 0) collisionLayers &= ~(1 << itemLayerIndex);
    int ignoreRaycastIndex = LayerMask.NameToLayer("Ignore Raycast");
    if (ignoreRaycastIndex >= 0) collisionLayers &= ~(1 << ignoreRaycastIndex);
  }

  private void OnValidate()
  {
    if (cameraLookHeight < 0.5f && cameraLookHeight != 0f)
    {
      cameraLookHeight = 1.5f;
    }
    if (cameraHeight < 1.0f && cameraHeight != 0f)
    {
      cameraHeight = 1.8f;
    }
  }

  private void Awake()
  {
    // Tự động sửa thông số nếu Scene cũ bị lưu giá trị âm hoặc bất hợp lý
    if (cameraLookHeight < 0.5f)
    {
      cameraLookHeight = 1.5f;
    }
    if (cameraHeight < 1.0f)
    {
      cameraHeight = 1.8f;
    }
    if (lockOnSideOffset > 2.0f)
    {
      lockOnSideOffset = 0.85f;
    }

    currentHeight = cameraHeight;
    cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);
    currentCollisionDistance = cameraDistance;

    cam = GetComponent<Camera>();
    if (cam == null) cam = Camera.main;
    if (cam != null)
    {
      baseFov = cam.fieldOfView;
      if (cam.nearClipPlane > cameraNearClipPlane)
      {
        cam.nearClipPlane = cameraNearClipPlane;
      }
    }

    if (collisionLayers.value == 0)
    {
      collisionLayers = LayerMask.GetMask("Default", "Terrain", "Obstacle", "NavMessFloor");
      if (collisionLayers.value == 0)
      {
        collisionLayers = ~LayerMask.GetMask("Player", "Enemy", "Ignore Raycast", "UI", "TransparentFX", "Item");
      }
    }

    int itemLayerIndex = LayerMask.NameToLayer("Item");
    if (itemLayerIndex >= 0) collisionLayers &= ~(1 << itemLayerIndex);
    int ignoreRaycastIndex = LayerMask.NameToLayer("Ignore Raycast");
    if (ignoreRaycastIndex >= 0) collisionLayers &= ~(1 << ignoreRaycastIndex);

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

    // Cập nhật FOV theo hiệu ứng Zoom Punch
    if (cam != null)
    {
      cam.fieldOfView = Mathf.Max(20f, baseFov - currentParryFovOffset);
    }

    Transform currentLockedEnemy = lockSystem != null && lockSystem.IsLockedOn ? lockSystem.CurrentTarget : null;
    bool isCurrentlyLockedOn = currentLockedEnemy != null;

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
    else if (isCurrentlyLockedOn && wasLockedOn && currentLockedEnemy != previousLockedEnemy)
    {
      // VỪA TỰ ĐỘNG CHUYỂN MỤC TIÊU SANG QUÁI TIẾP THEO (ví dụ quái cũ chết):
      // Kích hoạt thời gian chuyển mượt mà tới mục tiêu mới mà không reset vận tốc để lia tự nhiên
      enterTransitionTimer = targetSwitchSmoothDuration;
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
    previousLockedEnemy = currentLockedEnemy;

    // Chuyển đổi mượt mà độ cao giữa 2 chế độ (1.4m <-> 2.7m)
    float targetHeight = isCurrentlyLockedOn ? lockOnCameraHeight : cameraHeight;
    currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, 0.25f, Mathf.Infinity, Time.unscaledDeltaTime);

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

    // Vị trí mong muốn của camera khi khóa (tính thêm độ giật gần của Parry Zoom)
    Vector3 targetPivot = target.position + Vector3.up * cameraLookHeight;
    float effectiveDistance = Mathf.Max(1.0f, lockOnDistance - currentParryDistanceOffset);
    Vector3 lockOnOffset = new Vector3(lockOnSideOffset, currentHeight - cameraLookHeight, -effectiveDistance);
    Vector3 desiredPosition = targetPivot + (enemyFacingRot * lockOnOffset);

    // Điểm gốc trên thân nhân vật (ngang ngực & mặt)
    Vector3 rayPivot = targetPivot;

    // Xử lý cản trở vật lý: Kéo camera vào trong nếu bị tường/sàn che khuất
    Vector3 resolvedPosition = ResolveCameraCollision(rayPivot, desiredPosition, out _);

    // Tính toán độ mượt: Mới vào thì chuyển động êm (0.35s), sau đó siết dần về nhạy bén (0.15s)
    float currentPosSmooth = lockOnPositionSmoothTime;
    float currentRotSmooth = lockOnRotationSmoothTime;

    if (enterTransitionTimer > 0f)
    {
      enterTransitionTimer -= Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(1f - (enterTransitionTimer / enterTransitionDuration));
      currentPosSmooth = Mathf.Lerp(enterPositionSmoothTime, lockOnPositionSmoothTime, t);
      currentRotSmooth = Mathf.Lerp(enterRotationSmoothTime, lockOnRotationSmoothTime, t);
    }

    // Di chuyển vị trí camera (dùng Time.unscaledDeltaTime để camera zoom punch tức thì trong lúc hitlag đóng băng thời gian)
    transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref positionVelocity, currentPosSmooth, Mathf.Infinity, Time.unscaledDeltaTime);

    // Lớp bảo vệ cứng (Hard Anti-Clip): Đảm bảo tuyệt đối camera không thể nằm trong hoặc sau vật thể ở bất kỳ frame nào
    EnforceHardAntiClip(rayPivot);

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
    cameraYaw = Mathf.SmoothDampAngle(cameraYaw, desiredEuler.y, ref yawVelocity, currentRotSmooth, Mathf.Infinity, Time.unscaledDeltaTime);
    cameraPitch = Mathf.SmoothDampAngle(cameraPitch, targetPitch, ref pitchVelocity, currentRotSmooth, Mathf.Infinity, Time.unscaledDeltaTime);

    transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
  }

  // =========================================================================
  // XỬ LÝ CAMERA TỰ DO BÌNH THƯỜNG (FREE LOOK)
  // =========================================================================
  private void UpdateFreeCamera()
  {
    Vector3 targetPosition = target.position + Vector3.up * cameraLookHeight;
    float effectiveDistance = Mathf.Max(minZoomDistance, cameraDistance - currentParryDistanceOffset);
    Vector3 localOffset = new Vector3(cameraSideOffset, currentHeight - cameraLookHeight, -effectiveDistance);

    Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    Vector3 desiredPosition = targetPosition + orbitRotation * localOffset;

    // Điểm gốc trên thân nhân vật (ngang ngực & mặt)
    Vector3 rayPivot = targetPosition;

    // Xử lý cản trở vật lý: Kéo camera vào trong nếu bị tường/sàn che khuất
    Vector3 resolvedPosition = ResolveCameraCollision(rayPivot, desiredPosition, out float closeRatio);

    // Khi áp sát tường, tự động bù bớt góc pitch để camera nhìn ngang mặt/thân thay vì chúc xuống chân
    float effectivePitch = cameraPitch;
    if (cameraPitch > 0f)
    {
      effectivePitch = Mathf.Max(0f, cameraPitch - closeUpPitchCompensation * closeRatio);
    }
    Quaternion effectiveRotation = Quaternion.Euler(effectivePitch, cameraYaw, 0f);

    // Trong giai đoạn thoát khóa mục tiêu: Hòa trộn mượt mà cả vị trí lẫn góc xoay
    if (exitTransitionTimer > 0f)
    {
      exitTransitionTimer -= Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(1f - (exitTransitionTimer / exitTransitionDuration));
      float currentPosSmooth = Mathf.Lerp(exitPositionSmoothTime, freeCamFollowSmoothTime, t);

      // Trượt vị trí mượt mà từ combat về freecam
      transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref positionVelocity, currentPosSmooth, Mathf.Infinity, Time.unscaledDeltaTime);

      // Xoay từ từ góc nhìn sang orbit tự do
      transform.rotation = Quaternion.Slerp(transform.rotation, effectiveRotation, 14f * Time.unscaledDeltaTime);
    }
    else
    {
      // Khi đã về hẳn Freecam: Bám theo êm ái và xoay góc nhìn tự do tức thì theo chuột
      transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref positionVelocity, freeCamFollowSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
      transform.rotation = effectiveRotation;
    }

    // Lớp bảo vệ cứng (Hard Anti-Clip): Đảm bảo tuyệt đối camera không thể nằm trong hoặc sau vật thể ở bất kỳ frame nào
    EnforceHardAntiClip(rayPivot);
  }

  // =========================================================================
  // XỬ LÝ VA CHẠM VẬT LÝ & CHỐNG XUYÊN TƯỜNG/SÀN (ANTI-CLIPPING)
  // =========================================================================
  private Vector3 ResolveCameraCollision(Vector3 rayPivot, Vector3 desiredPos, out float closeRatio)
  {
    Vector3 toDesired = desiredPos - rayPivot;
    float maxDist = toDesired.magnitude;
    if (maxDist < 0.001f)
    {
      closeRatio = 0f;
      return desiredPos;
    }

    Vector3 rayDir = toDesired / maxDist;
    float minSafeDist = maxDist;

    // 1. Quét hình cầu (SphereCast) để kiểm tra bề mặt tường, sàn và góc cạnh
    int sphereHitCount = Physics.SphereCastNonAlloc(
        rayPivot,
        cameraCollisionRadius,
        rayDir,
        sphereHits,
        maxDist,
        collisionLayers,
        QueryTriggerInteraction.Ignore
    );

    for (int i = 0; i < sphereHitCount; i++)
    {
      RaycastHit hit = sphereHits[i];
      if (hit.collider == null) continue;
      if (target != null && hit.transform.root == target.root) continue;

      float safeDist = hit.distance - collisionBuffer;
      if (safeDist < minSafeDist)
      {
        minSafeDist = safeDist;
      }
    }

    // 2. Dự phòng bằng Raycast (không bị trượt nếu rayPivot nằm sát mép vật thể)
    int rayHitCount = Physics.RaycastNonAlloc(
        rayPivot,
        rayDir,
        rayHits,
        maxDist,
        collisionLayers,
        QueryTriggerInteraction.Ignore
    );

    for (int i = 0; i < rayHitCount; i++)
    {
      RaycastHit hit = rayHits[i];
      if (hit.collider == null) continue;
      if (target != null && hit.transform.root == target.root) continue;

      float safeDist = hit.distance - collisionBuffer;
      if (safeDist < minSafeDist)
      {
        minSafeDist = safeDist;
      }
    }

    // Đảm bảo khoảng cách an toàn, không để nhỏ hơn giới hạn tối thiểu trừ khi bị ép sát tường
    float targetDist = Mathf.Clamp(minSafeDist, Mathf.Min(minCameraDistance, minSafeDist), maxDist);
    targetDist = Mathf.Max(0.1f, targetDist);

    // Mượt mà hóa cự ly: Thu vào nhanh (tránh lộ xuyên tường), nhả ra êm ái
    if (currentCollisionDistance <= 0.01f)
    {
      currentCollisionDistance = maxDist;
    }

    float smoothTime = (targetDist < currentCollisionDistance) ? collisionPushSmoothTime : collisionReturnSmoothTime;
    currentCollisionDistance = Mathf.SmoothDamp(
        currentCollisionDistance,
        targetDist,
        ref collisionDistanceVelocity,
        smoothTime,
        Mathf.Infinity,
        Time.unscaledDeltaTime
    );

    currentCollisionDistance = Mathf.Clamp(currentCollisionDistance, 0.1f, maxDist);

    // Tỷ lệ áp sát tường (0 = cự ly bình thường, 1 = bị ép sát Player)
    closeRatio = Mathf.Clamp01(1f - (currentCollisionDistance / Mathf.Max(minZoomDistance, 2.5f)));

    // Bù trừ độ cao khi sát tường để camera nhìn ngang ngực & mặt thay vì chúc xuống chân
    Vector3 closeUpOffset = Vector3.up * (closeUpHeightBoost * closeRatio);

    return rayPivot + rayDir * currentCollisionDistance + closeUpOffset;
  }

  private void EnforceHardAntiClip(Vector3 rayPivot)
  {
    Vector3 toCam = transform.position - rayPivot;
    float currentDist = toCam.magnitude;
    if (currentDist < 0.001f) return;
    Vector3 dir = toCam / currentDist;

    float closestBlock = currentDist;

    // 1. Kiểm tra tia trực tiếp
    int rayCount = Physics.RaycastNonAlloc(
        rayPivot,
        dir,
        rayHits,
        currentDist,
        collisionLayers,
        QueryTriggerInteraction.Ignore
    );

    for (int i = 0; i < rayCount; i++)
    {
      RaycastHit hit = rayHits[i];
      if (hit.collider == null) continue;
      if (target != null && hit.transform.root == target.root) continue;

      float safe = hit.distance - collisionBuffer;
      if (safe < closestBlock)
      {
        closestBlock = safe;
      }
    }

    // 2. Kiểm tra thể tích hình cầu để đảm bảo mép camera không chạm tường
    int sphereCount = Physics.SphereCastNonAlloc(
        rayPivot,
        cameraCollisionRadius * 0.8f,
        dir,
        sphereHits,
        currentDist,
        collisionLayers,
        QueryTriggerInteraction.Ignore
    );

    for (int i = 0; i < sphereCount; i++)
    {
      RaycastHit hit = sphereHits[i];
      if (hit.collider == null) continue;
      if (target != null && hit.transform.root == target.root) continue;

      float safe = hit.distance - collisionBuffer;
      if (safe < closestBlock)
      {
        closestBlock = safe;
      }
    }

    if (closestBlock < currentDist)
    {
      closestBlock = Mathf.Max(0.1f, closestBlock);
      transform.position = rayPivot + dir * closestBlock;
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    if (target == null) return;
    Vector3 rayPivot = target.position + Vector3.up * cameraLookHeight;
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(rayPivot, 0.1f);
    Gizmos.DrawLine(rayPivot, transform.position);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, cameraCollisionRadius);
  }
#endif

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
      if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
      {
        return;
      }

      if (InfiniteLabyrinth.UI.InventoryUIController.IsOpen)
      {
        return;
      }

      if (AdminCheatManager.IsOpen)
      {
        return;
      }

      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }
  }

  // =========================================================================
  // HIỆU ỨNG ZOOM PUNCH KHI PARRY THÀNH CÔNG
  // =========================================================================
  public void TriggerParryZoom()
  {
    if (cam == null)
    {
      cam = GetComponent<Camera>();
      if (cam == null) cam = Camera.main;
      if (cam != null && Mathf.Approximately(currentParryFovOffset, 0f))
      {
        baseFov = cam.fieldOfView;
      }
    }

    if (parryZoomCoroutine != null)
    {
      StopCoroutine(parryZoomCoroutine);
    }
    parryZoomCoroutine = StartCoroutine(ParryZoomRoutine());
  }

  private IEnumerator ParryZoomRoutine()
  {
    float elapsed = 0f;
    float startFovOffset = currentParryFovOffset;
    float startDistOffset = currentParryDistanceOffset;

    // 1. Zoom in nhanh tạo cảm giác giật lực tức thì (Punch In)
    while (elapsed < parryZoomInDuration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / parryZoomInDuration);
      float curve = Mathf.SmoothStep(0f, 1f, t);
      currentParryFovOffset = Mathf.Lerp(startFovOffset, parryFovZoom, curve);
      currentParryDistanceOffset = Mathf.Lerp(startDistOffset, parryDistanceZoom, curve);
      yield return null;
    }

    currentParryFovOffset = parryFovZoom;
    currentParryDistanceOffset = parryDistanceZoom;

    // 2. Giữ khung hình cận cảnh trong suốt thời gian hitlag đóng băng (Hold)
    if (parryZoomHoldDuration > 0f)
    {
      yield return new WaitForSecondsRealtime(parryZoomHoldDuration);
    }

    // 3. Trượt êm ái về lại góc nhìn và cự ly ban đầu (Ease Out / Return)
    elapsed = 0f;
    while (elapsed < parryZoomOutDuration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / parryZoomOutDuration);
      float curve = Mathf.SmoothStep(0f, 1f, t);
      currentParryFovOffset = Mathf.Lerp(parryFovZoom, 0f, curve);
      currentParryDistanceOffset = Mathf.Lerp(parryDistanceZoom, 0f, curve);
      yield return null;
    }

    currentParryFovOffset = 0f;
    currentParryDistanceOffset = 0f;
    if (cam != null) cam.fieldOfView = baseFov;
    parryZoomCoroutine = null;
  }

  private void OnDisable()
  {
    if (parryZoomCoroutine != null)
    {
      StopCoroutine(parryZoomCoroutine);
      parryZoomCoroutine = null;
    }
    currentParryFovOffset = 0f;
    currentParryDistanceOffset = 0f;
    if (cam != null) cam.fieldOfView = baseFov;
  }
}