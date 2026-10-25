using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("State")]
    public bool canMove = true;

    [Header("Movement Speeds")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float strafeSpeed = 3.8f;      // Tốc độ khi bước ngang/lùi vờn quái
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Run / Sprint Settings (Shift Key)")]
    [Tooltip("Thời gian giữ Shift (giây) để bắt đầu chạy nước rút. Nhả ra trước mốc này sẽ là Lăn né (Roll).")]
    [SerializeField] private float sprintHoldThreshold = 0.25f;
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Jump Settings (Fixed Trajectory & Air Control)")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDelay = 0.15f;
    [Tooltip("Khả năng điều chỉnh hướng khi đang ở trên không (0 = quán tính tuyệt đối, 0.3 - 0.5 = cho phép lạng lách né bẫy/đáp sàn)")]
    [Range(0f, 1f)] [SerializeField] private float airControl = 0.35f;

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.6f;
    [Tooltip("Thời gian bất tử (i-frame) tính từ khi bắt đầu lăn né (giây, khuyên dùng 0.45s)")]
    [SerializeField] private float rollIFrameDuration = 0.45f;

    [Header("Ground Detection")]
    [Tooltip("LayerMask dùng để nhận diện mặt sàn và dự đoán tiếp đất (Anticipation)")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Landing Recovery Settings")]
    [Tooltip("Thời gian khựng chân khi nhảy thường (giây). Mặc định = 0 để khi bấm WASD nhân vật hủy Land và chạy tiếp cực kỳ mượt mà. Đặt > 0 (vd 0.35s) nếu muốn nhân vật bắt buộc phải quỵ gối Land xong mới được đi.")]
    [SerializeField] private float softLandLockDuration = 0f;

    [Tooltip("Thời gian khựng chân khi rơi từ trên cao/tầng 2 xuống (Hard Landing) để tạo sức nặng và chấn động chân thực (giây, khuyên dùng 0.35s).")]
    [SerializeField] private float hardLandLockDuration = 0.35f;

    private float landingLockTimer = 0f;

    [Header("Idle Flair Actions")]
    [SerializeField] private bool enableIdleActions = true;
    [Tooltip("Thời gian đứng yên tối thiểu trước khi kích hoạt hành động ngẫu nhiên (giây)")]
    [SerializeField] private float minIdleActionDelay = 6f;
    [Tooltip("Thời gian đứng yên tối đa trước khi kích hoạt hành động ngẫu nhiên (giây)")]
    [SerializeField] private float maxIdleActionDelay = 12f;
    [Tooltip("Tên các state hành động Idle trong Animator")]
    [SerializeField] private string[] idleActionStateNames = new string[] { "Idle Action 1", "Idle Action 2", "Idle Action 3" };

    private float idleTimer = 0f;
    private float nextIdleActionDelay = 8f;
    private bool isPlayingIdleAction = false;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private TargetLockSystem targetLockSystem;
    [SerializeField] private PlayerCombat playerCombat; // Tham chiếu tới PlayerCombat
    [SerializeField] private PlayerStats playerStats;   // Tham chiếu tới PlayerStats
    [SerializeField] private Health health;             // Tham chiếu tới Health để điều khiển I-Frames

    // Components & Physics States
    private CharacterController characterController;
    private float verticalVelocity;
    private bool wasGrounded = true;

    // Shift (Roll / Sprint) States
    private float shiftHoldTimer = 0f;
    private bool isShiftHeld = false;
    private bool isSprinting = false;

    // Roll Execution States
    private bool isRolling;
    private float rollTimer;
    private float rollIFrameTimer;
    private Vector3 rollDirection;

    // Jump States
    private Vector3 lockedAirVelocity;
    private Vector3 pendingJumpVelocity;
    private bool isPreparingJump;
    private float jumpTimer;
    private float jumpStartY;
    private bool isJumping;
    private float inAirTimer;

    // Animator Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsLockedOnHash = Animator.StringToHash("IsLockedOn");
    private static readonly int StrafeXHash = Animator.StringToHash("StrafeX");
    private static readonly int StrafeZHash = Animator.StringToHash("StrafeZ");

    public bool IsRolling => isRolling;
    public bool IsGrounded => characterController.isGrounded;
    public ThirdPersonCamera ThirdPersonCamera => thirdPersonCamera;
    private bool IsRunning => isSprinting && !(targetLockSystem != null && targetLockSystem.IsLockedOn);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (targetLockSystem == null) targetLockSystem = GetComponent<TargetLockSystem>();

        // Tự động tìm PlayerCombat ở cả Object cha lẫn Object con
        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
            if (playerCombat == null) playerCombat = GetComponentInChildren<PlayerCombat>();
        }

        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
            if (playerStats == null) playerStats = gameObject.AddComponent<PlayerStats>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null) health = GetComponentInParent<Health>();
        }

        if (thirdPersonCamera == null && Camera.main != null)
        {
            thirdPersonCamera = Camera.main.GetComponent<ThirdPersonCamera>();
        }

        nextIdleActionDelay = Random.Range(minIdleActionDelay, maxIdleActionDelay);
    }

    private void OnDisable()
    {
        if (health != null) health.SetInvulnerable(false);
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        HandleMovement();
        UpdateAnimation();
    }

    private void HandleMovement()
    {
        if (characterController == null || !characterController.enabled) return;
        if (Keyboard.current == null) return;

        // Đếm ngược thời gian khựng tiếp đất (nếu có Hard Landing từ tầng 2 hoặc softLandLockDuration > 0)
        if (landingLockTimer > 0f)
        {
            landingLockTimer -= Time.deltaTime;
            if (landingLockTimer <= 0f)
            {
                UnlockMovement();
            }
        }

        bool isPhysGrounded = characterController.isGrounded;
        bool isGrounded = isPhysGrounded;
        // Chỉ coi là tiếp đất thực sự khi: Chạm đất vật lý VÀ không đang trong giai đoạn nhún lấy đà nhảy VÀ không đang bay lên
        bool isTrulyGrounded = isPhysGrounded && !isPreparingJump && verticalVelocity <= 0.1f;
        bool justLanded = !wasGrounded && isTrulyGrounded;
        wasGrounded = isTrulyGrounded;

        if (isTrulyGrounded)
        {
            inAirTimer = 0f;
            isJumping = false;
        }

        if (justLanded && !isRolling && !isPreparingJump)
        {
            // Kiểm tra xem cú tiếp đất này là rơi từ trên cao (tầng 2 xuống) hay chỉ là nhảy thường trên mặt đất
            bool isHardFall = verticalVelocity < -11f || (isJumping && transform.position.y < jumpStartY - 2.5f);
            float recoveryTime = isHardFall ? hardLandLockDuration : softLandLockDuration;

            if (recoveryTime > 0.01f)
            {
                landingLockTimer = recoveryTime;
                LockMovement();
            }
            else
            {
                landingLockTimer = 0f;
                UnlockMovement();
            }
        }

        bool isBlocking = playerCombat != null && playerCombat.IsBlockingOrParrying;

        Vector2 input = Vector2.zero;

        // 1. NHẬN PHÍM WASD (Nhận cả khi đang Block để xác định hướng Lăn né chuẩn xác)
        if (canMove || isBlocking)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        bool isMoving = input.sqrMagnitude > 0.001f;
        bool isLocked = targetLockSystem != null && targetLockSystem.IsLockedOn;

        // Tính hướng di chuyển theo góc quay của Camera
        Quaternion cameraRotation = thirdPersonCamera != null
            ? thirdPersonCamera.PlanarRotation
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Vector3 desiredMoveDirection = cameraRotation * new Vector3(input.x, 0f, input.y);

        // Xử lý đếm giờ và kích hoạt / hủy Idle Actions
        HandleIdleActions(input, isMoving, isLocked, isBlocking);

        // =====================================================================
        // 2. XỬ LÝ PHÍM SHIFT: CHO PHÉP NHẤN KHI CANMOVE HOẶC KHI ĐANG BLOCK
        // =====================================================================
        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed;
        bool shiftDown = Keyboard.current.leftShiftKey.wasPressedThisFrame;
        bool shiftUp = Keyboard.current.leftShiftKey.wasReleasedThisFrame;

        // SỬA LỖI TẠI ĐÂY: Cho phép nhận Shift nếu canMove = true HOẶC đang Block!
        if (shiftDown && (canMove || isBlocking) && !isRolling)
        {
            isShiftHeld = true;
            shiftHoldTimer = 0f;
        }

        if (isShiftHeld && shiftPressed)
        {
            shiftHoldTimer += Time.deltaTime;

            // Chỉ chạy nhanh nếu không bị khóa mục tiêu (không ở trong CombatLocomotion), được phép di chuyển tự do (canMove) và có đủ thể lực
            if (!isLocked && shiftHoldTimer >= sprintHoldThreshold && isMoving && canMove)
            {
                if (playerStats == null || playerStats.HasStamina(1f))
                {
                    isSprinting = true;
                }
                else
                {
                    isSprinting = false;
                }
            }
            else if (isLocked)
            {
                isSprinting = false;
            }
        }

        // Tiêu hao Stamina liên tục khi đang chạy nhanh (chỉ khi thực sự chạy nhanh ngoài combat lock-on)
        if (IsRunning && isMoving && canMove)
        {
            if (playerStats != null)
            {
                if (!playerStats.ConsumeStaminaContinuous(playerStats.SprintCostPerSecond * Time.deltaTime))
                {
                    isSprinting = false; // Hết thể lực -> Tự động dừng chạy nhanh về đi bộ
                }
            }
        }

        if (shiftUp)
        {
            if (isShiftHeld && shiftHoldTimer < sprintHoldThreshold)
            {
                if ((canMove || isBlocking || landingLockTimer > 0f) && !isRolling && isGrounded && !isPreparingJump)
                {
                    // Kiểm tra thể lực trước khi cho phép Lăn né
                    if (playerStats != null && !playerStats.ConsumeStamina(playerStats.RollCost))
                    {
                        Debug.Log("<color=yellow>[Roll]</color> Hết thể lực, không thể lăn né!");
                    }
                    else
                    {
                        // NẾU ĐANG BLOCK MÀ LĂN NÉ -> DẬP TẮT NGAY TRẠNG THÁI BLOCK
                        if (isBlocking && playerCombat != null)
                        {
                            playerCombat.ForceResetCombat();
                        }

                        TriggerRoll(desiredMoveDirection);
                    }
                }
            }

            isShiftHeld = false;
            shiftHoldTimer = 0f;
            isSprinting = false;
        }

        if (!isMoving || !canMove || isLocked)
        {
            isSprinting = false;
        }

        // =====================================================================
        // 3. THỰC THI LĂN (ROLL) & I-FRAME BẤT TỬ
        // =====================================================================
        if (isRolling)
        {
            if (rollIFrameTimer > 0f)
            {
                rollIFrameTimer -= Time.deltaTime;
                if (rollIFrameTimer <= 0f && health != null)
                {
                    health.SetInvulnerable(false);
                }
            }

            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                isRolling = false;
                if (health != null) health.SetInvulnerable(false);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(rollDirection);
                verticalVelocity += gravity * Time.deltaTime;
                Vector3 rollVelocity = rollDirection * rollSpeed;
                rollVelocity.y = verticalVelocity;
                characterController.Move(rollVelocity * Time.deltaTime);
                return;
            }
        }

        // =====================================================================
        // 4. DI CHUYỂN BỘ & CHIẾN ĐẤU (CHỈ DI CHUYỂN KHI CANMOVE = TRUE)
        // =====================================================================
        if (isGrounded)
        {
            // Lực bám sàn -5f giúp CharacterController không bị trượt/chớp tắt isGrounded trên lan can và cầu thang
            if (verticalVelocity < 0f && !isPreparingJump) verticalVelocity = -5f;

            if (!isPreparingJump)
            {
                // Nếu canMove = false (đang vung kiếm hoặc đang Block) -> Không cho chân chạy
                Vector3 moveDir = canMove ? desiredMoveDirection : Vector3.zero;

                if (isLocked)
                {
                    Vector3 toTarget = targetLockSystem.CurrentTarget.position - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(toTarget);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
                    }

                    lockedAirVelocity = moveDir * strafeSpeed;
                }
                else
                {
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
                    }

                    float currentSpeed = IsRunning ? runSpeed : moveSpeed;
                    lockedAirVelocity = moveDir * currentSpeed;
                }

                // LỆNH NHẢY (SPACE)
                if (canMove && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    LockMovement();
                    isPreparingJump = true;
                    jumpTimer = jumpDelay;
                    animator.SetTrigger(JumpHash);
                    pendingJumpVelocity = lockedAirVelocity;
                    jumpStartY = transform.position.y;
                    isJumping = true;
                    inAirTimer = 0f;
                }
            }
            else
            {
                lockedAirVelocity = Vector3.zero;
                jumpTimer -= Time.deltaTime;
                if (jumpTimer <= 0f)
                {
                    isPreparingJump = false;
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    lockedAirVelocity = pendingJumpVelocity;
                }
            }
        }
        else
        {
            isPreparingJump = false;
            inAirTimer += Time.deltaTime;
            verticalVelocity += gravity * Time.deltaTime;

            // Cho phép điều chỉnh hướng nhẹ trên không (Air Control) giúp đáp sàn/cầu thang dễ dàng hơn
            if (airControl > 0f && desiredMoveDirection.sqrMagnitude > 0.001f)
            {
                float targetSpeed = IsRunning ? runSpeed : moveSpeed;
                Vector3 targetAirVel = desiredMoveDirection * targetSpeed;
                lockedAirVelocity = Vector3.Lerp(lockedAirVelocity, targetAirVel, airControl * 6f * Time.deltaTime);
            }
        }

        Vector3 finalVelocity = lockedAirVelocity;
        finalVelocity.y = verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
    }

    private void TriggerRoll(Vector3 desiredMoveDirection)
    {
        landingLockTimer = 0f;
        isRolling = true;
        rollTimer = rollDuration;
        rollIFrameTimer = rollIFrameDuration;
        if (health != null) health.SetInvulnerable(true);

        animator.SetTrigger(RollHash);
        rollDirection = desiredMoveDirection.sqrMagnitude > 0.001f ? desiredMoveDirection.normalized : transform.forward;
    }

    /// <summary>
    /// Dự đoán tiếp đất trước khi capsule va chạm thực tế (Anticipation) để Animator kích hoạt Land đón đầu mượt mà,
    /// triệt tiêu hoàn toàn cảm giác trễ 1-2 frame hay bị đơ dáng Fall trên mặt sàn.
    /// </summary>
    private bool CheckGroundAnticipation()
    {
        // Chỉ dự đoán khi đang rơi với vận tốc đủ lớn (verticalVelocity < -2.5f) và không đang lấy đà nhảy
        if (verticalVelocity >= -2.5f || isPreparingJump) return false;

        // Dự đoán khoảng cách tiếp đất tỷ lệ với tốc độ rơi (0.35m - 0.65m)
        float checkDist = Mathf.Clamp(-verticalVelocity * 0.05f, 0.35f, 0.65f);
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        float radius = characterController != null ? characterController.radius * 0.7f : 0.25f;

        LayerMask mask = groundLayers.value != 0 ? (LayerMask)(groundLayers.value & ~(1 << gameObject.layer)) : (LayerMask)~(1 << gameObject.layer);

        return Physics.SphereCast(origin, radius, Vector3.down, out _, checkDist + 0.2f, mask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateAnimation()
    {
        if (animator == null || !characterController.enabled) return;

        bool isPhysGrounded = characterController.isGrounded;
        bool approachingGround = CheckGroundAnticipation();

        // 1. MẶT ĐẤT ĐỐI VỚI ANIMATOR (IsGrounded):
        // - Khi đang nhún lấy đà nhảy (isPreparingJump) hoặc đang phóng lên không trung (isJumping && verticalVelocity > 0.1f):
        //   BẮT BUỘC gửi false để Animator KHÔNG bị ngắt ngang cú nhảy (không nhảy qua Land ngay khi vừa bấm Space)!
        // - Khi đang rơi xuống và sắp chạm đất (approachingGround) hoặc đã chạm đất (isPhysGrounded):
        //   Gửi true để kích hoạt Land chính xác ngay lúc chân chạm sàn, không bị trễ!
        bool isAnimGrounded = false;
        if (!isPreparingJump && !(isJumping && verticalVelocity > 0.1f))
        {
            isAnimGrounded = isPhysGrounded || approachingGround;
        }
        animator.SetBool(IsGroundedHash, isAnimGrounded);

        // 2. LOGIC KÍCH HOẠT FALL THÔNG MINH:
        // - Nhảy tại chỗ / nhảy ngang trên cùng mặt phẳng: isFalling = false -> Nhảy từ Jump đi thẳng tới Land!
        // - Nhảy từ tầng 2 xuống: rơi sâu hơn điểm xuất phát (> 1.2m) -> isFalling = true -> Kích hoạt Fall loop trên không!
        // - Rơi tự do khỏi mép tầng 2 (không bấm Space): rơi >= 0.18s VÀ verticalVelocity < -2.5f -> isFalling = true!
        // - Khi sắp chạm sàn hoặc đã chạm sàn (isAnimGrounded): dập tắt ngay isFalling = false để Animator đón vào Land!
        bool isFalling = false;
        if (!isAnimGrounded)
        {
            if (isJumping)
            {
                isFalling = (transform.position.y < jumpStartY - 1.2f);
            }
            else
            {
                isFalling = (inAirTimer >= 0.18f && verticalVelocity < -2.5f);
            }
        }
        animator.SetBool(IsFallingHash, isFalling);

        bool isLocked = targetLockSystem != null && targetLockSystem.IsLockedOn;
        animator.SetBool(IsLockedOnHash, isLocked);

        if (isRolling || !isPhysGrounded || isPreparingJump) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null && canMove)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        float inputMagnitude = Mathf.Clamp01(input.magnitude);
        float targetSpeed = 0f;
        if (inputMagnitude > 0.01f)
        {
            targetSpeed = IsRunning ? 1f : 0.5f;
        }

        if (isLocked)
        {
            animator.SetFloat(StrafeXHash, input.x, speedDampTime, Time.deltaTime);
            animator.SetFloat(StrafeZHash, input.y, speedDampTime, Time.deltaTime);
            animator.SetFloat(SpeedHash, targetSpeed, speedDampTime, Time.deltaTime);
        }
        else
        {

            // Chống hiện tượng dao động damping làm Speed bị trôi về số âm khi đứng yên
            if (targetSpeed == 0f && inputMagnitude < 0.01f)
            {
                float curSpeed = animator.GetFloat(SpeedHash);
                if (curSpeed < 0.05f)
                {
                    animator.SetFloat(SpeedHash, 0f);
                }
                else
                {
                    animator.SetFloat(SpeedHash, 0f, speedDampTime, Time.deltaTime);
                }
            }
            else
            {
                animator.SetFloat(SpeedHash, targetSpeed, speedDampTime, Time.deltaTime);
            }

            animator.SetBool(IsRunningHash, IsRunning && inputMagnitude > 0f);
        }
    }

    public void LockMovement() => canMove = false;
    public void UnlockMovement() => canMove = true;

    // =====================================================================
    // IDLE FLAIR ACTIONS (HÀNH ĐỘNG NGẪU NHIÊN KHI ĐỨNG IDLE LÂU)
    // =====================================================================
    private void HandleIdleActions(Vector2 input, bool isMoving, bool isLocked, bool isBlocking)
    {
        if (!enableIdleActions || animator == null) return;

        bool isAttacking = playerCombat != null && playerCombat.IsAttacking;
        bool isInventoryOpen = InfiniteLabyrinth.UI.InventoryUIController.IsOpen;

        // Cho phép đếm giờ Idle ngay cả khi canMove = false do đang mở Inventory UI
        bool isTrulyIdle = IsGrounded && !isMoving && (canMove || isInventoryOpen) && !isRolling 
                           && !isPreparingJump && !isJumping && !isLocked && !isBlocking && !isAttacking
                           && landingLockTimer <= 0f;

        // Nếu đang trong diễn hoạt Idle Action
        if (isPlayingIdleAction)
        {
            // Kiểm tra xem có bất kỳ input hoặc thay đổi trạng thái nào từ người chơi không
            bool hasMovementInput = isMoving || input.sqrMagnitude > 0.001f;
            bool hasActionStateChange = (!canMove && !isInventoryOpen) || isRolling || isPreparingJump || isJumping || isLocked || isBlocking || isAttacking;

            bool hasKeyInput = false;
            if (!isInventoryOpen && Keyboard.current != null)
            {
                hasKeyInput = Keyboard.current.spaceKey.wasPressedThisFrame ||
                              Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                              Keyboard.current.qKey.wasPressedThisFrame ||
                              Keyboard.current.eKey.wasPressedThisFrame;
            }

            // Chỉ nhận chuột để ngắt Idle Action khi KHÔNG mở Inventory (khi mở Inventory, chuột dùng để click UI và xoay model 3D)
            bool hasMouseInput = false;
            if (!isInventoryOpen && Mouse.current != null)
            {
                hasMouseInput = Mouse.current.leftButton.wasPressedThisFrame ||
                                Mouse.current.rightButton.wasPressedThisFrame ||
                                Mouse.current.middleButton.wasPressedThisFrame;
            }

            if (hasMovementInput || hasActionStateChange || hasKeyInput || hasMouseInput)
            {
                CancelIdleAction();
                return;
            }

            // Kiểm tra trạng thái Animator để biết khi nào clip kết thúc
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool backInLocomotion = stateInfo.IsName("Locomotion") && !animator.IsInTransition(0);
            bool animFinished = !animator.IsInTransition(0) && stateInfo.normalizedTime >= 0.98f;

            if (backInLocomotion || animFinished)
            {
                if (animFinished && !stateInfo.IsName("Locomotion"))
                {
                    animator.CrossFadeInFixedTime("Locomotion", 0.25f, 0);
                }
                isPlayingIdleAction = false;
                idleTimer = 0f;
                nextIdleActionDelay = Random.Range(minIdleActionDelay, maxIdleActionDelay);
            }
            return;
        }

        // Khi đứng yên hoàn toàn: đếm thời gian
        if (isTrulyIdle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= nextIdleActionDelay)
            {
                TriggerRandomIdleAction();
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void TriggerRandomIdleAction()
    {
        idleTimer = 0f;
        nextIdleActionDelay = Random.Range(minIdleActionDelay, maxIdleActionDelay);

        if (idleActionStateNames == null || idleActionStateNames.Length == 0) return;

        // Chọn ngẫu nhiên 1 hành động
        int randomIndex = Random.Range(0, idleActionStateNames.Length);
        string preferredName = idleActionStateNames[randomIndex];

        // Tìm đúng tên State trong Animator (hỗ trợ cả trường hợp có hoặc không có dấu cách)
        string resolvedName = ResolveIdleActionStateName(preferredName, randomIndex + 1);

        animator.CrossFadeInFixedTime(resolvedName, 0.25f, 0);
        isPlayingIdleAction = true;
    }

    private string ResolveIdleActionStateName(string preferredName, int actionNumber)
    {
        if (animator.HasState(0, Animator.StringToHash(preferredName)))
        {
            return preferredName;
        }

        // Các biến thể đặt tên phổ biến
        string[] candidates = new string[]
        {
            $"Idle Action {actionNumber}",
            $"IdleAction{actionNumber}",
            $"Idle Action{actionNumber}",
            "Idle Action" // fallback cho action 1
        };

        foreach (string candidate in candidates)
        {
            if (animator.HasState(0, Animator.StringToHash(candidate)))
            {
                return candidate;
            }
        }

        return preferredName;
    }

    public void CancelIdleAction()
    {
        if (!isPlayingIdleAction) return;

        isPlayingIdleAction = false;
        idleTimer = 0f;
        nextIdleActionDelay = Random.Range(minIdleActionDelay, maxIdleActionDelay);

        bool isInventoryOpen = InfiniteLabyrinth.UI.InventoryUIController.IsOpen;
        // Chỉ CrossFade về Locomotion nếu không đang bị khóa bởi Jump hay Roll
        if (animator != null && IsGrounded && (canMove || isInventoryOpen) && !isRolling && !isPreparingJump)
        {
            animator.CrossFadeInFixedTime("Locomotion", 0.15f, 0);
        }
    }
}