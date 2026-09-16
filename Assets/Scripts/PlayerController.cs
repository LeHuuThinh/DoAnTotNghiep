using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float fallMultiplier = 2.5f; // Nhân trọng lực khi rơi
    [SerializeField] private float lowJumpMultiplier = 2f; // Nhân trọng lực khi nhả phím nhảy sớm
    [SerializeField] private float apexHangTimeMultiplier = 0.5f; // Giảm trọng lực ở đỉnh
    [SerializeField] private float apexVelocityThreshold = 1f; // Ngưỡng vận tốc để kích hoạt Hang Time

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
    private float verticalVelocity;
    private float cameraYaw;
    private float cameraPitch = 15f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            cameraYaw = transform.eulerAngles.y;
        }

        cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCameraInput();
        HandleMovement();

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

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // 1. Lấy góc xoay tổng của camera dựa trên chuột
        Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

        // 2. Điểm gốc trên người nhân vật (điểm Camera sẽ xoay quanh)
        Vector3 targetPosition = transform.position + Vector3.up * cameraLookHeight;

        // 3. Gộp tất cả các khoảng cách (Side, Height, Distance) vào MỘT vector cục bộ.
        // Khi nhân với orbitRotation, camera sẽ giữ nguyên form này dù bạn nhìn lên/xuống/trái/phải.
        Vector3 localOffset = new Vector3(cameraSideOffset, cameraHeight - cameraLookHeight, -cameraDistance);

        // 4. Tính toán vị trí cuối cùng
        Vector3 desiredPosition = targetPosition + orbitRotation * localOffset;

        // 5. Di chuyển camera mượt mà
        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            desiredPosition,
            cameraFollowSpeed * Time.deltaTime);

        // 6. QUAN TRỌNG: Gán thẳng góc nhìn thay vì dùng LookAt().
        // Việc này đảm bảo camera luôn nhìn song song phía trước, tạo đúng cảm giác nhìn qua vai.
        cameraTransform.rotation = orbitRotation;
    }

    private void HandleCameraInput()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        cameraYaw += mouseDelta.x * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);

        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        cameraDistance = Mathf.Clamp(
            cameraDistance - scrollDelta * zoomSpeed * 0.01f,
            minZoomDistance,
            maxZoomDistance);
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 moveDirection = cameraRotation * new Vector3(input.x, 0f, input.y);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                12f * Time.deltaTime);
        }

        // --- XỬ LÝ CHẠM ĐẤT ---
        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Ép nhẹ xuống đất để leo dốc mượt
        }

        // --- KÍCH HOẠT NHẢY ---
        // Công thức vật lý kinh điển để đạt đúng chiều cao mong muốn: v = căn bậc 2 của (h * -2 * g)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- XỬ LÝ TRỌNG LỰC "GAME FEEL" ---
        float gravityMultiplier = 1f;

        if (verticalVelocity < 0f)
        {
            // 1. Fall Multiplier: Càng rơi càng chịu trọng lực mạnh -> Cú chạm đất có lực hơn
            gravityMultiplier = fallMultiplier;
        }
        else if (verticalVelocity > 0f && !Keyboard.current.spaceKey.isPressed)
        {
            // 2. Low Jump: Nếu đang bay lên mà người chơi thả tay khỏi phím Space -> Kéo rơi xuống sớm
            gravityMultiplier = lowJumpMultiplier;
        }
        else if (Mathf.Abs(verticalVelocity) < apexVelocityThreshold && !isGrounded)
        {
            // 3. Hang Time: Khi đang ở lơ lửng quanh đỉnh cú nhảy -> Trọng lực yếu đi để tạo độ "lỳ"
            gravityMultiplier = apexHangTimeMultiplier;
        }

        // Áp dụng trọng lực đã tinh chỉnh vào vận tốc trục Y
        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;

        // --- ÁP DỤNG VÀO CONTROLLER ---
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }
}
