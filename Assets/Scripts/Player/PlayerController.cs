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

    [Header("Jump Settings (Fixed Trajectory)")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDelay = 0.15f;

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.6f;
    [Tooltip("Thời gian bất tử (i-frame) tính từ khi bắt đầu lăn né (giây, khuyên dùng 0.45s)")]
    [SerializeField] private float rollIFrameDuration = 0.45f;

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

    // Animator Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
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
    }

    private void OnDisable()
    {
        if (health != null) health.SetInvulnerable(false);
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimation();
    }

    private void HandleMovement()
    {
        if (characterController == null || !characterController.enabled) return;
        if (Keyboard.current == null) return;

        bool isGrounded = characterController.isGrounded;
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

            // Chỉ chạy nhanh nếu được phép di chuyển tự do (canMove) và có đủ thể lực
            if (shiftHoldTimer >= sprintHoldThreshold && isMoving && canMove)
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
        }

        // Tiêu hao Stamina liên tục khi đang chạy nhanh
        if (isSprinting && isMoving && canMove)
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
                if ((canMove || isBlocking) && !isRolling && isGrounded && !isPreparingJump)
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

        if (!isMoving || !canMove)
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
            if (verticalVelocity < 0f && !isPreparingJump) verticalVelocity = -2f;

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
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalVelocity = lockedAirVelocity;
        finalVelocity.y = verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
    }

    private void TriggerRoll(Vector3 desiredMoveDirection)
    {
        isRolling = true;
        rollTimer = rollDuration;
        rollIFrameTimer = rollIFrameDuration;
        if (health != null) health.SetInvulnerable(true);

        animator.SetTrigger(RollHash);
        rollDirection = desiredMoveDirection.sqrMagnitude > 0.001f ? desiredMoveDirection.normalized : transform.forward;
    }

    private void UpdateAnimation()
    {
        if (animator == null || !characterController.enabled) return;

        bool isGrounded = characterController.isGrounded;
        animator.SetBool(IsGroundedHash, isGrounded);

        bool isLocked = targetLockSystem != null && targetLockSystem.IsLockedOn;
        animator.SetBool(IsLockedOnHash, isLocked);

        if (isRolling || !isGrounded || isPreparingJump) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null && canMove)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        if (isLocked)
        {
            animator.SetFloat(StrafeXHash, input.x, speedDampTime, Time.deltaTime);
            animator.SetFloat(StrafeZHash, input.y, speedDampTime, Time.deltaTime);
        }
        else
        {
            float inputMagnitude = Mathf.Clamp01(input.magnitude);
            float targetSpeed = 0f;
            if (inputMagnitude > 0.01f)
            {
                targetSpeed = IsRunning ? 1f : 0.5f;
            }

            animator.SetFloat(SpeedHash, targetSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(IsRunningHash, IsRunning && inputMagnitude > 0f);
        }
    }

    public void LockMovement() => canMove = false;
    public void UnlockMovement() => canMove = true;
}