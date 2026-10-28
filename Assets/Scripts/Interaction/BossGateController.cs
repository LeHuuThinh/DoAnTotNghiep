using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Interaction
{
  public enum BossGateState
  {
    Locked,       // Cửa đóng, yêu cầu chìa khóa để mở
    Opening,      // Cửa đang trong diễn hoạt mở
    Opened,       // Cửa mở toang, người chơi có thể vào
    LockedArena   // Người chơi đã vào đấu trường; cửa đóng sập và khóa chặt collider cho tới khi hạ gục Boss
  }

  /// <summary>
  /// Quản lý Đại Môn Phong Ấn phòng Boss (Boss Gate) & Cơ chế Lock-in đấu trường:
  /// - Ban đầu khóa, yêu cầu Chìa Khóa Hầm Ngục ("dungeon_key") và đè giữ F (2.5s) để mở.
  /// - Khi người chơi bước vào đấu trường, cửa tự động đóng sập và bật Collider ngăn người chơi bỏ chạy.
  /// - Khi Boss bị tiêu diệt (IceGolemController.OnBossDefeated), cửa tự động mở lại cho người chơi.
  /// </summary>
  public class BossGateController : MonoBehaviour, IInteractable
  {
    public static BossGateController Instance { get; private set; }

    [Header("=== TRẠNG THÁI CỔNG ===")]
    [SerializeField] private BossGateState currentState = BossGateState.Locked;

    [Header("=== CÁNH CỬA ĐÔI (DOUBLE DOORS) ===")]
    [Tooltip("Khớp xoay cánh cửa trái")]
    [SerializeField] private Transform leftDoorHinge;
    [Tooltip("Khớp xoay cánh cửa phải")]
    [SerializeField] private Transform rightDoorHinge;

    [SerializeField] private Vector3 leftClosedEuler = Vector3.zero;
    [SerializeField] private Vector3 leftOpenedEuler = new Vector3(0f, -105f, 0f);

    [SerializeField] private Vector3 rightClosedEuler = Vector3.zero;
    [SerializeField] private Vector3 rightOpenedEuler = new Vector3(0f, 105f, 0f);

    [Tooltip("Thời gian cửa mở (giây)")]
    [SerializeField] private float openDuration = 2.4f;

    [Tooltip("Thời gian cửa đóng sập khi vào arena (giây)")]
    [SerializeField] private float closeDuration = 1.0f;

    [Header("=== VẬT CẢN VÀ NAVMESH ĐẠI MÔN ===")]
    [Tooltip("Collider cản bước người chơi khi cửa đóng")]
    [SerializeField] private Collider doorObstacleCollider;
    [Tooltip("NavMeshObstacle đục lỗ động khi cửa đóng, xóa lỗ khi cửa mở")]
    [SerializeField] private NavMeshObstacle doorNavObstacle;

    [Header("=== CỔNG SƯƠNG MÙ (ĐÃ TẮT THEO YÊU CẦU) ===")]
    [SerializeField] private GameObject fogGateRoot;
    [SerializeField] private Collider fogBlockerCollider;
    [SerializeField] private NavMeshObstacle fogNavObstacle;

    [Header("=== CẤU HÌNH TƯƠNG TÁC (IINTERACTABLE) ===")]
    [SerializeField] private string requiredKeyId = "dungeon_key";
    [SerializeField] private string promptLocked = "Requires Dungeon Boss Key";
    [SerializeField] private string promptCanUnlock = "Unlock Grand Gate [Hold F]";
    [SerializeField] private string targetName = "Grand Boss Gate";
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float interactionRadius = 6.0f;
    [SerializeField] private Transform interactionTransform;

    [Header("=== ÂM THANH ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    public static event Action OnBossGateOpened;
    public static event Action OnBossArenaLocked;

    public BossGateState CurrentState => currentState;
    public bool IsOpened => currentState == BossGateState.Opened;
    public bool IsArenaLocked => currentState == BossGateState.LockedArena;

    public void InitializeReferences(
      Transform leftHinge, Transform rightHinge,
      Collider doorCol, NavMeshObstacle doorNav,
      GameObject fogRoot, Collider fogCol, NavMeshObstacle fogNav,
      Transform interactPt)
    {
      leftDoorHinge = leftHinge;
      rightDoorHinge = rightHinge;
      doorObstacleCollider = doorCol;
      doorNavObstacle = doorNav;
      fogGateRoot = fogRoot;
      fogBlockerCollider = fogCol;
      fogNavObstacle = fogNav;
      interactionTransform = interactPt;
    }

    // Triển khai IInteractable
    public string InteractionPrompt => HasKey() ? promptCanUnlock : promptLocked;
    public string TargetName => targetName;
    public bool RequiresHold => HasKey();
    public float HoldDuration => HasKey() ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => interactionTransform != null ? interactionTransform : transform;

    private bool isTransitioning = false;
    private Coroutine doorCoroutine;

    private void Awake()
    {
      Instance = this;

      if (doorObstacleCollider == null)
      {
        doorObstacleCollider = GetComponent<Collider>();
      }

      if (doorNavObstacle == null)
      {
        doorNavObstacle = GetComponent<NavMeshObstacle>();
      }

      // Đảm bảo sương mù hoàn toàn tắt theo yêu cầu
      if (fogGateRoot != null) fogGateRoot.SetActive(false);
      if (fogBlockerCollider != null) fogBlockerCollider.enabled = false;
      if (fogNavObstacle != null) fogNavObstacle.enabled = false;

      // Đảm bảo cửa ở trạng thái đóng ban đầu
      if (currentState == BossGateState.Locked)
      {
        if (leftDoorHinge != null) leftDoorHinge.localRotation = Quaternion.Euler(leftClosedEuler);
        if (rightDoorHinge != null) rightDoorHinge.localRotation = Quaternion.Euler(rightClosedEuler);
        if (doorObstacleCollider != null) doorObstacleCollider.enabled = true;
        if (doorNavObstacle != null)
        {
          doorNavObstacle.enabled = true;
          doorNavObstacle.carving = true;
        }
      }
    }

    private void OnEnable()
    {
      if (currentState == BossGateState.Locked)
      {
        InteractableRegistry.Register(this);
      }

      // Lắng nghe sự kiện tiêu diệt Boss để tự động mở lại cửa
      IceGolemController.OnBossDefeated -= HandleBossDefeated;
      IceGolemController.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
      InteractableRegistry.Unregister(this);
      IceGolemController.OnBossDefeated -= HandleBossDefeated;
    }

    public bool HasKey()
    {
      return InventoryManager.HasInstance && InventoryManager.Instance.HasItem(requiredKeyId);
    }

    public bool CanInteract(GameObject player)
    {
      if (currentState != BossGateState.Locked) return false;
      if (isTransitioning) return false;
      return enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      if (!CanInteract(player)) return;

      if (!HasKey())
      {
        Debug.Log($"<color=yellow>[BossGate]</color> 🔒 The Grand Gate remains firmly locked. Requires key: '{requiredKeyId}'.");
        return;
      }

      UnlockAndOpenGate();
    }

    public void UnlockAndOpenGate()
    {
      if (currentState != BossGateState.Locked || isTransitioning) return;

      // Tiêu hao chìa khóa hầm ngục
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.RemoveItem(requiredKeyId, 1);
        Debug.Log($"<color=green>[BossGate]</color> 🗝️ Dungeon Boss Key ('{requiredKeyId}') consumed! Opening Grand Gate...");
      }

      // Hủy đăng ký tương tác
      InteractableRegistry.Unregister(this);

      if (doorCoroutine != null) StopCoroutine(doorCoroutine);
      doorCoroutine = StartCoroutine(OpenGateRoutine());
    }

    private IEnumerator OpenGateRoutine()
    {
      isTransitioning = true;
      currentState = BossGateState.Opening;

      if (openSound != null)
      {
        AudioSource.PlayClipAtPoint(openSound, transform.position);
      }

      // Tắt collider và nav obstacle để người chơi có thể bước qua
      if (doorObstacleCollider != null)
      {
        doorObstacleCollider.enabled = false;
      }

      if (doorNavObstacle != null)
      {
        doorNavObstacle.enabled = false;
      }

      Quaternion leftStart = leftDoorHinge != null ? leftDoorHinge.localRotation : Quaternion.identity;
      Quaternion leftEnd = Quaternion.Euler(leftOpenedEuler);

      Quaternion rightStart = rightDoorHinge != null ? rightDoorHinge.localRotation : Quaternion.identity;
      Quaternion rightEnd = Quaternion.Euler(rightOpenedEuler);

      float timer = 0f;
      while (timer < openDuration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / openDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        if (leftDoorHinge != null) leftDoorHinge.localRotation = Quaternion.Slerp(leftStart, leftEnd, smoothT);
        if (rightDoorHinge != null) rightDoorHinge.localRotation = Quaternion.Slerp(rightStart, rightEnd, smoothT);

        yield return null;
      }

      if (leftDoorHinge != null) leftDoorHinge.localRotation = leftEnd;
      if (rightDoorHinge != null) rightDoorHinge.localRotation = rightEnd;

      currentState = BossGateState.Opened;
      isTransitioning = false;
      doorCoroutine = null;

      Debug.Log("<color=cyan>[BossGate]</color> 🚪 Grand Gate fully opened! Path into Boss Arena is clear.");
      OnBossGateOpened?.Invoke();
    }

    /// <summary>
    /// Kích hoạt cơ chế khóa chặt đấu trường khi người chơi bước vào (Boss Arena Lock-in).
    /// </summary>
    public void LockArena()
    {
      if (currentState == BossGateState.LockedArena) return;

      currentState = BossGateState.LockedArena;
      InteractableRegistry.Unregister(this);

      if (doorCoroutine != null) StopCoroutine(doorCoroutine);
      doorCoroutine = StartCoroutine(LockArenaRoutine());
    }

    private IEnumerator LockArenaRoutine()
    {
      isTransitioning = true;

      if (closeSound != null)
      {
        AudioSource.PlayClipAtPoint(closeSound, transform.position);
      }

      // Đóng sập 2 cánh cửa lại
      Quaternion leftStart = leftDoorHinge != null ? leftDoorHinge.localRotation : Quaternion.identity;
      Quaternion leftEnd = Quaternion.Euler(leftClosedEuler);

      Quaternion rightStart = rightDoorHinge != null ? rightDoorHinge.localRotation : Quaternion.identity;
      Quaternion rightEnd = Quaternion.Euler(rightClosedEuler);

      float timer = 0f;
      while (timer < closeDuration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / closeDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        if (leftDoorHinge != null) leftDoorHinge.localRotation = Quaternion.Slerp(leftStart, leftEnd, smoothT);
        if (rightDoorHinge != null) rightDoorHinge.localRotation = Quaternion.Slerp(rightStart, rightEnd, smoothT);

        yield return null;
      }

      if (leftDoorHinge != null) leftDoorHinge.localRotation = leftEnd;
      if (rightDoorHinge != null) rightDoorHinge.localRotation = rightEnd;

      // Khóa collider cửa và đục NavMeshObstacle để ngăn người chơi và quái đi xuyên qua
      if (doorObstacleCollider != null)
      {
        doorObstacleCollider.enabled = true;
      }

      if (doorNavObstacle != null)
      {
        doorNavObstacle.enabled = true;
        doorNavObstacle.carving = true;
      }

      isTransitioning = false;
      doorCoroutine = null;

      Debug.Log("<color=cyan>[BossGate]</color> 🚪 LOCK-IN ENGAGED! Cửa đại môn đã đóng sập và khóa chặt. Không thể bỏ chạy cho tới khi Boss bị tiêu diệt!");
      OnBossArenaLocked?.Invoke();
    }

    private void HandleBossDefeated()
    {
      OpenGateOnBossDefeated();
    }

    /// <summary>
    /// Tự động mở lại cửa khi Boss bị tiêu diệt
    /// </summary>
    public void OpenGateOnBossDefeated()
    {
      Debug.Log("<color=green>[BossGate]</color> 🏆 Boss đã bị tiêu diệt! Tự động mở lại Đại Môn cho người chơi.");
      if (doorCoroutine != null) StopCoroutine(doorCoroutine);
      doorCoroutine = StartCoroutine(OpenGateAfterVictoryRoutine());
    }

    private IEnumerator OpenGateAfterVictoryRoutine()
    {
      isTransitioning = true;
      currentState = BossGateState.Opened;

      if (openSound != null)
      {
        AudioSource.PlayClipAtPoint(openSound, transform.position);
      }

      // Tắt collider và NavMeshObstacle để mở lối đi thông suốt
      if (doorObstacleCollider != null)
      {
        doorObstacleCollider.enabled = false;
      }

      if (doorNavObstacle != null)
      {
        doorNavObstacle.enabled = false;
      }

      Quaternion leftStart = leftDoorHinge != null ? leftDoorHinge.localRotation : Quaternion.identity;
      Quaternion leftEnd = Quaternion.Euler(leftOpenedEuler);

      Quaternion rightStart = rightDoorHinge != null ? rightDoorHinge.localRotation : Quaternion.identity;
      Quaternion rightEnd = Quaternion.Euler(rightOpenedEuler);

      float timer = 0f;
      while (timer < openDuration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / openDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        if (leftDoorHinge != null) leftDoorHinge.localRotation = Quaternion.Slerp(leftStart, leftEnd, smoothT);
        if (rightDoorHinge != null) rightDoorHinge.localRotation = Quaternion.Slerp(rightStart, rightEnd, smoothT);

        yield return null;
      }

      if (leftDoorHinge != null) leftDoorHinge.localRotation = leftEnd;
      if (rightDoorHinge != null) rightDoorHinge.localRotation = rightEnd;

      isTransitioning = false;
      doorCoroutine = null;

      Debug.Log("<color=cyan>[BossGate]</color> 🚪 Đại Môn đã mở toang trở lại sau chiến thắng! Lối ra vào hoàn toàn thông suốt.");
      OnBossGateOpened?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Vector3 center = interactionTransform != null ? interactionTransform.position : transform.position;
      Gizmos.DrawWireSphere(center, interactionRadius);
    }
#endif
  }
}
