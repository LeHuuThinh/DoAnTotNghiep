using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý Rương kho báu trong hầm ngục
  /// Cho phép người chơi lại gần bấm phím [F] để mở nắp và tung đồ ra đất
  /// </summary>
  public class ChestInteractable : MonoBehaviour
  {
    [Header("=== CẤU HÌNH MỞ RƯƠNG ===")]
    [Tooltip("Khớp nắp rương để xoay mở (nếu có, nếu không sẽ lắc nhẹ)")]
    [SerializeField] private Transform chestLid;
    [Tooltip("Góc mở nắp (thường là xoay quanh trục X hoặc Z -80 đến -110 độ)")]
    [SerializeField] private Vector3 openEulerOffset = new Vector3(-95f, 0f, 0f);
    [Tooltip("Tốc độ mở nắp")]
    [SerializeField] private float openSpeed = 3.5f;

    [Header("=== BÁN KÍNH TƯƠNG TÁC ===")]
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("=== HIỆU ỨNG VÀ ÂM THANH ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private GameObject openVfxPrefab;

    private bool isOpen = false;
    private bool isPlayerInRange = false;
    private Transform playerTransform;
    private LootDropper lootDropper;

    public bool IsOpen => isOpen;
    public bool IsPlayerInRange => isPlayerInRange;

    private void Awake()
    {
      lootDropper = GetComponent<LootDropper>();
      if (chestLid == null)
      {
        chestLid = transform.Find("Lid") ?? transform.Find("chest_lid") ?? transform.Find("top");
      }
    }

    private void Start()
    {
      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Update()
    {
      if (isOpen) return;

      if (playerTransform == null)
      {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        if (playerTransform == null) return;
      }

      float dist = Vector3.Distance(transform.position, playerTransform.position);

      if (dist <= interactionRadius)
      {
        isPlayerInRange = true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
          OpenChest();
        }
      }
      else
      {
        isPlayerInRange = false;
      }
    }

    public void OpenChest()
    {
      if (isOpen) return;
      isOpen = true;
      isPlayerInRange = false;

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
