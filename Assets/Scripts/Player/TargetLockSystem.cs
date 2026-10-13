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

  [Header("UI Reticle (Tùy chọn)")]
  [Tooltip("Kéo một icon/chấm tròn World Space UI vào đây để hiển thị tâm ngắm trên đầu quái")]
  [SerializeField] private GameObject lockOnReticle;
  [SerializeField] private Vector3 reticleOffset = new Vector3(0, 1.2f, 0);

  private Transform currentTarget;
  private Health targetHealth;
  private Camera mainCamera;

  public bool IsLockedOn => currentTarget != null;
  public Transform CurrentTarget => currentTarget;

  private void Awake()
  {
    mainCamera = Camera.main;
    if (lockOnReticle != null) lockOnReticle.SetActive(false);
  }

  private void Update()
  {
    HandleLockInput();
    ValidateCurrentTarget();
    UpdateReticlePosition();
  }

  private void HandleLockInput()
  {
    // Bấm Chuột giữa HOẶC phím Q để Bật/Tắt khóa mục tiêu
    bool togglePressed = false;
    if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame) togglePressed = true;
    if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) togglePressed = true;

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
    Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange, enemyLayer);
    Transform bestTarget = null;
    float bestScore = float.MaxValue;

    Transform camTransform = mainCamera != null ? mainCamera.transform : transform;

    foreach (Collider col in colliders)
    {
      Transform candidate = col.transform.root;

      // Kiểm tra máu: Quái chết thì bỏ qua
      Health candidateHealth = candidate.GetComponent<Health>();
      if (candidateHealth != null && candidateHealth.IsDead) continue;

      // Kiểm tra góc nhìn: Quái phải nằm trong tầm mắt phía trước camera
      Vector3 toCandidate = (candidate.position - camTransform.position).normalized;
      float angle = Vector3.Angle(camTransform.forward, toCandidate);
      if (angle > viewAngleLimit) continue;

      // Kiểm tra chướng ngại vật: Không khóa quái bị tường/vật thể che khuất
      Vector3 rayStart = transform.position + Vector3.up * 1.5f;
      Vector3 rayEnd = candidate.position + Vector3.up * 1.5f;
      if (Physics.Linecast(rayStart, rayEnd, obstacleLayer)) continue;

      // Ưu tiên con quái ở gần tâm màn hình và gần Player nhất
      float distance = Vector3.Distance(transform.position, candidate.position);
      float score = distance + angle * 0.2f;

      if (score < bestScore)
      {
        bestScore = score;
        bestTarget = candidate;
        targetHealth = candidateHealth;
      }
    }

    if (bestTarget != null)
    {
      currentTarget = bestTarget;
      if (lockOnReticle != null) lockOnReticle.SetActive(true);
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

    // Tự hủy khóa nếu: Quái bị xóa khỏi game, quái chết, hoặc chạy quá cự ly breakLockRange
    if (currentTarget == null || (targetHealth != null && targetHealth.IsDead))
    {
      UnlockTarget();
      return;
    }

    float distance = Vector3.Distance(transform.position, currentTarget.position);
    if (distance > breakLockRange)
    {
      UnlockTarget();
      return;
    }

    // Tự hủy khóa nếu quái nấp sau tường quá 1.5 giây
    Vector3 rayStart = transform.position + Vector3.up * 1.5f;
    Vector3 rayEnd = currentTarget.position + Vector3.up * 1.5f;
    if (Physics.Linecast(rayStart, rayEnd, obstacleLayer))
    {
      UnlockTarget();
    }
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