using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("State")]
    public bool canMove = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -20f;

    [Header("Run")]
    [SerializeField] private float runActivationTime = 0.5f;
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private Animator animator;

    [Header("Jump (Fixed Trajectory)")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDelay = 0.15f; // Thời gian chờ khom gối lấy đà (giây)

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.6f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 8f;
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 2.2f;
    [SerializeField] private float cameraSideOffset = 0.8f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraLookHeight = 1.2f;

    private CharacterController characterController;
    private float rightMouseHeldTime;
    private bool isRightMouseHeld;
    private bool runPersisted;
    private float verticalVelocity;
    private float cameraYaw;
    private float cameraPitch = 15f;

    private bool isRolling;
    private float rollTimer;
    private Vector3 rollDirection;

    private Vector3 lockedAirVelocity;
    private Vector3 pendingJumpVelocity; // Đà bay sẽ được bung ra sau khi lấy đà xong
    private bool isPreparingJump; // Biến đánh dấu đang khom người
    private float jumpTimer;      // Bộ đếm lùi

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null) cameraYaw = transform.eulerAngles.y;

        cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCameraInput();
        HandleRunInput();
        HandleMovement();
        UpdateAnimation();

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

    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 targetPosition = transform.position + Vector3.up * cameraLookHeight;
        Vector3 localOffset = new Vector3(cameraSideOffset, cameraHeight - cameraLookHeight, -cameraDistance);
        Vector3 desiredPosition = targetPosition + orbitRotation * localOffset;

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, cameraFollowSpeed * Time.deltaTime);
        cameraTransform.rotation = orbitRotation;
    }

    private void HandleCameraInput()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        cameraYaw += mouseDelta.x * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);

        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        cameraDistance = Mathf.Clamp(cameraDistance - scrollDelta * zoomSpeed * 0.01f, minZoomDistance, maxZoomDistance);
    }

    public void LockMovement() { canMove = false; }
    public void UnlockMovement() { canMove = true; }

    private void HandleMovement()
    {
        if (Keyboard.current == null) return;

        bool isGrounded = characterController.isGrounded;

        Vector2 input = Vector2.zero;

        // 1. CHỈ NHẬN INPUT NẾU ĐƯỢC PHÉP DI CHUYỂN
        if (canMove)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        if (input.sqrMagnitude <= 0.001f) runPersisted = false;

        Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 desiredMoveDirection = cameraRotation * new Vector3(input.x, 0f, input.y);

        // --- XỬ LÝ ROLL ---
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

        // --- XỬ LÝ ĐI BỘ & NHẢY CÓ TRỄ ---
        if (isGrounded)
        {
            if (verticalVelocity < 0f && !isPreparingJump) verticalVelocity = -2f;

            float currentSpeed = IsRunning ? runSpeed : moveSpeed;

            if (!isPreparingJump)
            {
                // 1. Chỉ xoay người và cập nhật vận tốc khi KHÔNG khom gối lấy đà
                if (desiredMoveDirection.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), 12f * Time.deltaTime);
                }
                lockedAirVelocity = desiredMoveDirection * currentSpeed;

                // 2. NHẬN LỆNH NHẢY
                if (canMove && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    LockMovement(); // Khóa input WASD
                    isPreparingJump = true;
                    jumpTimer = jumpDelay;
                    animator.SetTrigger(JumpHash);

                    // Lưu lại hướng và tốc độ chạy hiện tại để dùng khi thực sự bay lên
                    pendingJumpVelocity = lockedAirVelocity;
                }
            }
            else
            {
                // 3. ĐANG LẤY ĐÀ: Ép lực đẩy ngang về 0 để nhân vật đứng yên gồng sức
                lockedAirVelocity = Vector3.zero;

                // 4. ĐẾM LÙI
                jumpTimer -= Time.deltaTime;
                if (jumpTimer <= 0f)
                {
                    isPreparingJump = false;
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // Nảy trục Y

                    // Trả lại đà đã lưu để bay lướt về phía trước trên không trung
                    lockedAirVelocity = pendingJumpVelocity;
                }
            }
        }
        else
        {
            isPreparingJump = false; // Đề phòng rơi tự do khỏi mép vực lúc đang lấy đà
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Áp dụng lực vào Character Controller
        Vector3 finalVelocity = lockedAirVelocity;
        finalVelocity.y = verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isGrounded = characterController.isGrounded;
        animator.SetBool(IsGroundedHash, isGrounded);

        if (isRolling || !isGrounded || isPreparingJump) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        float inputMagnitude = Mathf.Clamp01(input.magnitude);
        float targetAnimationSpeed = 0f;

        // QUY ĐỔI TRẠNG THÁI RA SỐ FLOAT
        if (inputMagnitude > 0.01f)
        {
            // Nếu có đi bộ, gán là 0.5. Nếu có thêm chạy, gán là 1.0.
            targetAnimationSpeed = IsRunning ? 1f : 0.5f;
        }

        // Gửi thẳng giá trị này vào Animator
        animator.SetFloat(SpeedHash, targetAnimationSpeed, speedDampTime, Time.deltaTime);

        // (Tùy chọn) Giữ lại biến IsRunning nếu sau này bạn cần dùng cho các Transition khác
        animator.SetBool(IsRunningHash, IsRunning && inputMagnitude > 0f);
    }

    private bool IsRunning => isRightMouseHeld || runPersisted;
}