using System.Collections;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

/// <summary>
/// Đồng bộ hóa mô hình 3D của vũ khí cầm trên tay nhân vật khi người chơi
/// thay đổi trang bị trong Túi đồ (Inventory).
/// Tự động gắn vào Sword/VisualHolder để bảo toàn BoxCollider, Rigidbody và VFX.
/// </summary>
public class PlayerWeaponVisualSync : MonoBehaviour
{
  [Header("=== CẤU HÌNH VỊ TRÍ GẮN VŨ KHÍ ===")]
  [Tooltip("Transform chứa model kiếm (thường là Sword/VisualHolder trong tay phải)")]
  [SerializeField] private Transform visualHolder;

  [Tooltip("Prefab vũ khí mặc định (Kiếm gỗ) khi chưa trang bị vũ khí nào")]
  [SerializeField] private GameObject defaultWeaponPrefab;

  private GameObject currentVisualInstance;
  private bool isRegistered = false;

  private void Awake()
  {
    EnsureVisualHolder();
  }

  private void Start()
  {
    StartCoroutine(WaitForInventoryAndSyncRoutine());
  }

  private void OnEnable()
  {
    RegisterInventoryEvents();
  }

  private void OnDisable()
  {
    UnregisterInventoryEvents();
  }

  private void EnsureVisualHolder()
  {
    if (visualHolder != null) return;

    // 1. Tìm qua WeaponHitbox trên tay phải
    var hitbox = GetComponentInChildren<WeaponHitbox>(true);
    if (hitbox != null)
    {
      Transform vh = hitbox.transform.Find("VisualHolder");
      if (vh != null)
      {
        visualHolder = vh;
        return;
      }
      // Nếu Sword chưa có VisualHolder con, dùng chính Transform của hitbox
      visualHolder = hitbox.transform;
      return;
    }

    // 2. Fallback tìm đệ quy bất kỳ Transform tên "VisualHolder"
    foreach (var tr in GetComponentsInChildren<Transform>(true))
    {
      if (tr.name == "VisualHolder")
      {
        visualHolder = tr;
        return;
      }
    }

    // 3. Fallback tìm Transform tên "Sword"
    foreach (var tr in GetComponentsInChildren<Transform>(true))
    {
      if (tr.name == "Sword")
      {
        visualHolder = tr;
        return;
      }
    }

    Debug.LogWarning("[PlayerWeaponVisualSync] Không tìm thấy VisualHolder hoặc Sword trong phân cấp nhân vật!");
  }

  private IEnumerator WaitForInventoryAndSyncRoutine()
  {
    // Đợi InventoryManager sẵn sàng nếu nạp scene sau
    while (!InventoryManager.HasInstance)
    {
      yield return null;
    }

    RegisterInventoryEvents();

    if (InventoryManager.Instance.EquippedWeapon != null)
    {
      UpdateWeaponVisual(InventoryManager.Instance.EquippedWeapon);
    }
  }

  private void RegisterInventoryEvents()
  {
    if (isRegistered || !InventoryManager.HasInstance) return;

    InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
    isRegistered = true;
  }

  private void UnregisterInventoryEvents()
  {
    if (!isRegistered || !InventoryManager.HasInstance) return;

    InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
    isRegistered = false;
  }

  private void HandleEquipmentChanged(ItemType type, ItemData item)
  {
    if (type == ItemType.Weapon)
    {
      UpdateWeaponVisual(item);
    }
  }

  /// <summary>
  /// Cập nhật hiển thị model 3D kiếm trên tay nhân vật
  /// </summary>
  public void UpdateWeaponVisual(ItemData weaponItem)
  {
    EnsureVisualHolder();
    if (visualHolder == null) return;

    GameObject prefabToSpawn = null;
    if (weaponItem != null && weaponItem.WeaponVisualPrefab != null)
    {
      prefabToSpawn = weaponItem.WeaponVisualPrefab;
    }
    else if (defaultWeaponPrefab != null)
    {
      prefabToSpawn = defaultWeaponPrefab;
    }

    // Xóa model kiếm cũ bên trong visualHolder
    ClearCurrentVisual();

    if (prefabToSpawn != null)
    {
      currentVisualInstance = Instantiate(prefabToSpawn, visualHolder);
      currentVisualInstance.name = prefabToSpawn.name;
      currentVisualInstance.transform.localPosition = Vector3.zero;
      currentVisualInstance.transform.localRotation = Quaternion.identity;
      currentVisualInstance.transform.localScale = Vector3.one;

      // Vô hiệu hóa mọi Collider trong model con để không xung đột với BoxCollider Hitbox của Sword
      foreach (var col in currentVisualInstance.GetComponentsInChildren<Collider>(true))
      {
        col.enabled = false;
      }

      Debug.Log($"[PlayerWeaponVisualSync] Đã trang bị thành công mô hình: {prefabToSpawn.name}");
    }
  }

  private void ClearCurrentVisual()
  {
    if (visualHolder == null) return;

    // Xóa tất cả các object con trong visualHolder
    for (int i = visualHolder.childCount - 1; i >= 0; i--)
    {
      Transform child = visualHolder.GetChild(i);
      // Bỏ qua nếu có thành phần quan trọng (VFX, Hitbox...) vô tình nằm dưới
      if (child.GetComponent<WeaponHitbox>() != null || child.GetComponent<ParticleSystem>() != null)
      {
        continue;
      }

      if (Application.isPlaying)
      {
        Destroy(child.gameObject);
      }
      else
      {
        DestroyImmediate(child.gameObject);
      }
    }

    currentVisualInstance = null;
  }
}
