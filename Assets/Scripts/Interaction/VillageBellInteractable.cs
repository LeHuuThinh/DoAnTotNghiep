using UnityEngine;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Chuông Làng Haven (Village Bell):
  /// - Điểm tựa thiêng liêng nơi trung tâm Làng (Safe Sanctuary).
  /// - Khi tương tác (Giữ F): Cầu nguyện tại Chuông để hồi phục 100% Máu (Health), 100% Thần Lực (Spirit) và Thể Lực (Stamina).
  /// - Tự động đồng bộ với hệ thống tương tác IInteractable và InteractableRegistry.
  /// </summary>
  public class VillageBellInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH TƯƠNG TÁC ===")]
    [Tooltip("Câu gợi ý hiển thị trên UI")]
    [SerializeField] private string interactionPrompt = "Cầu Nguyện (Hồi Đầy Máu & Thần Lực)";

    [Tooltip("Tên đối tượng")]
    [SerializeField] private string targetName = "Chuông Thánh Haven";

    [Tooltip("Bật chế độ đè giữ phím F")]
    [SerializeField] private bool requiresHold = true;

    [Tooltip("Thời gian cần giữ phím (giây) để hoàn tất cầu nguyện")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("Bán kính có thể tương tác (m)")]
    [SerializeField] private float interactionRadius = 5.0f;

    [Header("=== HIỆU ỨNG ÂM THANH ===")]
    [SerializeField] private AudioClip praySound;

    public string InteractionPrompt => interactionPrompt;
    public string TargetName => targetName;
    public bool RequiresHold => requiresHold;
    public float HoldDuration => requiresHold ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

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
      return enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      if (player == null) return;

      // 1. Hồi 100% Máu (Health)
      Health health = player.GetComponent<Health>();
      if (health != null)
      {
        health.RefillToFull();
      }

      // 2. Hồi 100% Thần Lực (Spirit) & Thể Lực (Stamina)
      PlayerStats stats = player.GetComponent<PlayerStats>();
      if (stats != null)
      {
        stats.RefillSpiritToFull();
        stats.RestoreStamina(9999f);
      }

      // 3. Âm thanh chuông ngân thiêng liêng
      if (praySound != null)
      {
        AudioSource.PlayClipAtPoint(praySound, transform.position);
      }

      Debug.Log("<color=green>[VillageBell]</color> 🔔 <b>Bạn đã cầu nguyện trước Chuông Thánh Haven!</b> Toàn bộ Máu (100%), Thần Lực Spirit (100%) và Thể Lực đã được hồi phục viên mãn.");
    }
  }
}
