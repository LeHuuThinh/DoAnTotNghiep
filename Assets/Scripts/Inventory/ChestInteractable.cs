using System.Collections;
using UnityEngine;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý Rương kho báu trong hầm ngục
  /// Kế thừa IInteractable hỗ trợ đè giữ nhẹ phím [F] (hoặc tap) để mở nắp và tung đồ ra đất
  /// </summary>
  public class ChestInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH MỞ RƯƠNG ===")]
    [Tooltip("Khớp nắp rương để xoay mở (nếu có, nếu không sẽ lắc nhẹ)")]
    [SerializeField] private Transform chestLid;
    [Tooltip("Góc mở nắp (thường là xoay quanh trục X hoặc Z -80 đến -110 độ)")]
    [SerializeField] private Vector3 openEulerOffset = new Vector3(-95f, 0f, 0f);
    [Tooltip("Tốc độ mở nắp")]
    [SerializeField] private float openSpeed = 3.5f;

    [Header("=== CẤU HÌNH TƯƠNG TÁC (IINTERACTABLE) ===")]
    [Tooltip("Câu gợi ý hiển thị trên giao diện")]
    [SerializeField] private string interactionPrompt = "Mở Rương";
    [Tooltip("Có yêu cầu đè giữ phím không")]
    [SerializeField] private bool requiresHold = true;
    [Tooltip("Thời gian cần giữ phím (giây), ví dụ: 0.8s")]
    [SerializeField] private float holdDuration = 0.8f;
    [Tooltip("Bán kính có thể tương tác")]
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("=== HIỆU ỨNG VÀ ÂM THANH ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private GameObject openVfxPrefab;

    private bool isOpen = false;
    private LootDropper lootDropper;

    public bool IsOpen => isOpen;

    // Triển khai IInteractable
    public string InteractionPrompt => interactionPrompt;
    public string TargetName => "Rương Kho Báu";
    public bool RequiresHold => requiresHold;
    public float HoldDuration => requiresHold ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

    private void Awake()
    {
      lootDropper = GetComponent<LootDropper>();
      if (chestLid == null)
      {
        chestLid = transform.Find("Lid") ?? transform.Find("chest_lid") ?? transform.Find("top");
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
      return !isOpen && enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      OpenChest();
    }

    public void OpenChest()
    {
      if (isOpen) return;
      isOpen = true;

      // 1. Chạy hiệu ứng xoay nắp rương
      if (chestLid != null)
      {
        StartCoroutine(OpenLidRoutine());
      }

      // 2. Kích hoạt hiệu ứng âm thanh & hạt ánh sáng
      if (openSound != null)
      {
        AudioSource.PlayClipAtPoint(openSound, transform.position);
      }
      if (openVfxPrefab != null)
      {
        Instantiate(openVfxPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);
      }

      // 3. Tung các món đồ ra xung quanh
      if (lootDropper != null)
      {
        lootDropper.DropLoot();
      }
    }

    private IEnumerator OpenLidRoutine()
    {
      Quaternion startRot = chestLid.localRotation;
      Quaternion targetRot = startRot * Quaternion.Euler(openEulerOffset);
      float progress = 0f;

      while (progress < 1f)
      {
        progress += Time.deltaTime * openSpeed;
        chestLid.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, progress));
        yield return null;
      }

      chestLid.localRotation = targetRot;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
  }
}
