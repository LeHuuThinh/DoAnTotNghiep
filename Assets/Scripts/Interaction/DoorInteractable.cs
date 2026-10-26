using System.Collections;
using UnityEngine;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Quản lý mở / đóng cửa có khớp quay (ví dụ: wall_doorway chứa cánh cửa wall_doorway_door)
  /// Kế thừa IInteractable, hỗ trợ đè giữ phím F (mặc định 1.0s) để xoay mở cánh cửa mượt mà.
  /// </summary>
  public class DoorInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH CÁNH CỬA ===")]
    [Tooltip("Transform của cánh cửa xoay mở (tự động tìm 'wall_doorway_door' nếu để trống)")]
    [SerializeField] private Transform doorTransform;

    [Tooltip("Góc xoay khi cửa ĐÓNG (mặc định 0, 0, 0)")]
    [SerializeField] private Vector3 closedEuler = Vector3.zero;

    [Tooltip("Góc xoay khi cửa MỞ (mặc định quay Y từ 0 đến -120 độ)")]
    [SerializeField] private Vector3 openedEuler = new Vector3(0f, -120f, 0f);

    [Tooltip("Thời gian cánh cửa xoay mở/đóng (giây)")]
    [SerializeField] private float rotationDuration = 1.6f;

    [Tooltip("Cho phép đè F lần nữa để đóng cửa lại")]
    [SerializeField] private bool canToggleClose = true;

    [Header("=== CẤU HÌNH TƯƠNG TÁC (IINTERACTABLE) ===")]
    [Tooltip("Câu nhắc khi cửa đang ĐÓNG")]
    [SerializeField] private string openPrompt = "Mở Cửa";

    [Tooltip("Câu nhắc khi cửa đang MỞ")]
    [SerializeField] private string closePrompt = "Đóng Cửa";

    [Tooltip("Tên đối tượng")]
    [SerializeField] private string targetName = "Cửa Hầm Ngục";

    [Tooltip("Có yêu cầu đè giữ phím không (true: Hold, false: Tap)")]
    [SerializeField] private bool requiresHold = true;

    [Tooltip("Thời gian cần đè giữ phím F (giây), mặc định 1.0s")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("Bán kính có thể tương tác")]
    [SerializeField] private float interactionRadius = 2.8f;

    [Header("=== VẬT CẢN & ÂM THANH ===")]
    [Tooltip("Collider chặn người chơi khi cửa đóng (tự động tắt khi mở cửa để đi qua)")]
    [SerializeField] private Collider doorObstacleCollider;

    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private bool isOpen = false;
    private bool isRotating = false;
    private Coroutine rotateCoroutine;

    public bool IsOpen => isOpen;
    public bool IsRotating => isRotating;

    // Triển khai IInteractable
    public string InteractionPrompt => isOpen ? closePrompt : openPrompt;
    public string TargetName => targetName;
    public bool RequiresHold => requiresHold;
    public float HoldDuration => requiresHold ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => doorTransform != null ? doorTransform : transform;

    private void Awake()
    {
      if (doorTransform == null)
      {
        doorTransform = transform.Find("wall_doorway_door")
                     ?? transform.Find("door")
                     ?? transform.Find("Door");
      }

      if (doorObstacleCollider == null)
      {
        doorObstacleCollider = GetComponent<Collider>();
      }
    }

    private void OnEnable()
    {
      InteractableRegistry.Register(this);
    }

    private void OnDisable()
    {
      InteractableRegistry.Unregister(this);
    }

    public bool CanInteract(GameObject player)
    {
      if (isRotating) return false;
      if (isOpen && !canToggleClose) return false;
      return enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      if (!CanInteract(player)) return;

      ToggleDoor();
    }

    public void ToggleDoor()
    {
      if (isRotating) return;

      isOpen = !isOpen;
      Vector3 targetEuler = isOpen ? openedEuler : closedEuler;

      if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
      rotateCoroutine = StartCoroutine(RotateDoorRoutine(targetEuler, isOpen));
    }

    private IEnumerator RotateDoorRoutine(Vector3 targetEuler, bool opening)
    {
      isRotating = true;

      // Âm thanh
      AudioClip clip = opening ? openSound : closeSound;
      if (clip != null)
      {
        AudioSource.PlayClipAtPoint(clip, transform.position);
      }

      // Khi bắt đầu mở, nếu có doorObstacleCollider thì tắt để người chơi đi qua
      if (opening && doorObstacleCollider != null)
      {
        doorObstacleCollider.enabled = false;
      }

      if (doorTransform != null)
      {
        Quaternion startRot = doorTransform.localRotation;
        Quaternion endRot = Quaternion.Euler(targetEuler);
        float timer = 0f;

        while (timer < rotationDuration)
        {
          timer += Time.deltaTime;
          float t = Mathf.Clamp01(timer / rotationDuration);
          float smoothT = Mathf.SmoothStep(0f, 1f, t);
          doorTransform.localRotation = Quaternion.Slerp(startRot, endRot, smoothT);
          yield return null;
        }

        doorTransform.localRotation = endRot;
      }

      // Khi đã đóng hoàn toàn, bật lại collider chặn đường
      if (!opening && doorObstacleCollider != null)
      {
        doorObstacleCollider.enabled = true;
      }

      isRotating = false;
      rotateCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Vector3 center = doorTransform != null ? doorTransform.position : transform.position;
      Gizmos.DrawWireSphere(center, interactionRadius);
    }
#endif
  }
}
