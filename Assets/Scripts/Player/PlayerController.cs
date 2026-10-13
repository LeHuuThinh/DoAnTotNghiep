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

    [Header("Run Settings")]
    [SerializeField] private float runActivationTime = 0.5f;
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Jump Settings (Fixed Trajectory)")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDelay = 0.15f;

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.6f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private TargetLockSystem targetLockSystem;

    // Components & States
    private CharacterController characterController;
    private float rightMouseHeldTime;
    private bool isRightMouseHeld;
    private bool runPersisted;
    private float verticalVelocity;

    // Roll States
    private bool isRolling;
    private float rollTimer;
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
    private bool IsRunning => (isRightMouseHeld || runPersisted) && !(targetLockSystem != null && targetLockSystem.IsLockedOn);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (targetLockSystem == null) targetLockSystem = GetComponent<TargetLockSystem>();

        if (thirdPersonCamera == null && Camera.main != null)
        {
            thirdPersonCamera = Camera.main.GetComponent<ThirdPersonCamera>();
        }
    }

    private void Update()
    {
        HandleRunInput();
        HandleMovement();
        UpdateAnimation();
    }

    private void HandleRunInput()
    {
        if (Mouse.current == null) return;
        bool rightMousePressed = Mouse.current.rightButton.isPressed;

        if (rightMousePressed)
        {
            if (!isRightMouseHeld)
            {
                isRightMouseHeld = true;
                rightMouseHeldTime = 0f;
            }
            rightMouseHeldTime += Time.deltaTime;
            if (rightMouseHeldTime >= runActivationTime) runPersisted = true;
            return;
        }

        if (isRightMouseHeld && rightMouseHeldTime < runActivationTime) runPersisted = false;
        isRightMouseHeld = false;
        rightMouseHeldTime = 0f;
    }

    private void HandleMovement()
    {
        // CHỐT CHẶN: Nếu CharacterController đã bị tắt (đã chết) thì dừng toàn bộ tính toán di chuyển
        if (characterController == null || !characterController.enabled) return;

        if (Keyboard.current == null) return;

        bool isGrounded = characterController.isGrounded;
        Vector2 input = Vector2.zero;

        if (canMove)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        if (input.sqrMagnitude <= 0.001f) runPersisted = false;

        bool isLocked = targetLockSystem != null && targetLockSystem.IsLockedOn;

        // TÍNH TOÁN HƯỚNG DI CHUYỂN
        Quaternion cameraRotation = thirdPersonCamera != null
            ? thirdPersonCamera.PlanarRotation
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Vector3 desiredMoveDirection = cameraRotation * new Vector3(input.x, 0f, input.y);

        // =====================================================================
        // 1. XỬ LÝ LĂN (ROLL) - Luôn lăn theo hướng bấm phím
        // =====================================================================
        if (canMove && !isRolling && isGrounded && Keyboard.current.shiftKey.wasPressedThisFrame && !isPreparingJump)
        {
            isRolling = true;
            rollTimer = rollDuration;
            animator.SetTrigger(RollHash);
            rollDirection = desiredMoveDirection.sqrMagnitude > 0.001f ? desiredMoveDirection.normalized : transform.forward;
        }

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f) isRolling = false;
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
        // 2. DI CHUYỂN BỘ & CHIẾN ĐẤU (TỰ DO vs STRAFE LOCK-ON)
        // =====================================================================
        if (isGrounded)
        {
            if (verticalVelocity < 0f && !isPreparingJump) verticalVelocity = -2f;

            if (!isPreparingJump)
            {
                if (isLocked)
                {
                    // --- CHẾ ĐỘ LOCK-ON (STRAFE) ---
                    // Thân người chơi luôn xoay mặt về phía quái
                    Vector3 toTarget = targetLockSystem.CurrentTarget.position - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(toTarget);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
                    }

                    // Di chuyển theo tốc độ Strafe
                    lockedAirVelocity = desiredMoveDirection * strafeSpeed;
                }
                else
                {
                    // --- CHẾ ĐỘ TỰ DO ---
                    if (desiredMoveDirection.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), rotationSpeed * Time.deltaTime);
                    }

                    float currentSpeed = IsRunning ? runSpeed : moveSpeed;
                    lockedAirVelocity = desiredMoveDirection * currentSpeed;
                }

                // LỆNH NHẢY
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
                // KHOM GỐI LẤY ĐÀ
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

        // Áp dụng lực vào Character Controller
        Vector3 finalVelocity = lockedAirVelocity;
        finalVelocity.y = verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
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
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        if (isLocked)
        {
            // Trong thế chiến đấu: Truyền trực tiếp trục X (Trái/Phải) và Y (Tiến/Lùi) vào 2D Blend Tree
            animator.SetFloat(StrafeXHash, input.x, speedDampTime, Time.deltaTime);
            animator.SetFloat(StrafeZHash, input.y, speedDampTime, Time.deltaTime);
        }
        else
        {
            // Trong chế độ tự do: Dùng Speed (0 = Idle, 0.5 = Walk, 1.0 = Run)
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