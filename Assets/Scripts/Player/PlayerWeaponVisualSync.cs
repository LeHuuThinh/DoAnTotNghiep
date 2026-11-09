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
  private GameObject currentAuraInstance;
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
    if (visualHolder != null)
    {
      SetLayerRecursively(visualHolder.gameObject, visualHolder.gameObject.layer);
      return;
    }

    // 1. Tìm qua WeaponHitbox trên tay phải
    var hitbox = GetComponentInChildren<WeaponHitbox>(true);
    if (hitbox != null)
    {
      Transform vh = hitbox.transform.Find("VisualHolder");
      if (vh != null)
      {
        visualHolder = vh;
        SetLayerRecursively(visualHolder.gameObject, visualHolder.gameObject.layer);
        return;
      }
      // Nếu Sword chưa có VisualHolder con, dùng chính Transform của hitbox
      visualHolder = hitbox.transform;
      SetLayerRecursively(visualHolder.gameObject, visualHolder.gameObject.layer);
      return;
    }

    // 2. Fallback tìm đệ quy bất kỳ Transform tên "VisualHolder"
    foreach (var tr in GetComponentsInChildren<Transform>(true))
    {
      if (tr.name == "VisualHolder")
      {
        visualHolder = tr;
        SetLayerRecursively(visualHolder.gameObject, visualHolder.gameObject.layer);
        return;
      }
    }

    // 3. Fallback tìm Transform tên "Sword"
    foreach (var tr in GetComponentsInChildren<Transform>(true))
    {
      if (tr.name == "Sword")
      {
        visualHolder = tr;
        SetLayerRecursively(visualHolder.gameObject, visualHolder.gameObject.layer);
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

    // Luôn luôn đồng bộ hiển thị ngay cả khi ô vũ khí đang trống (để dọn sạch model mặc định)
    UpdateWeaponVisual(InventoryManager.Instance.EquippedWeapon);
  }

  private void RegisterInventoryEvents()
  {
    if (isRegistered || !InventoryManager.HasInstance) return;

    InventoryManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
    InventoryManager.Instance.OnEquipmentUpgradeChanged += HandleEquipmentUpgradeChanged;
    isRegistered = true;
  }

  private void UnregisterInventoryEvents()
  {
    if (!isRegistered || !InventoryManager.HasInstance) return;

    InventoryManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
    InventoryManager.Instance.OnEquipmentUpgradeChanged -= HandleEquipmentUpgradeChanged;
    isRegistered = false;
  }

  private void HandleEquipmentChanged(ItemType type, ItemData item)
  {
    if (type == ItemType.Weapon)
    {
      UpdateWeaponVisual(item);
      UpdateMaxUpgradeAura();
    }
  }

  private void HandleEquipmentUpgradeChanged(ItemType type, int level)
  {
    if (type == ItemType.Weapon)
    {
      UpdateMaxUpgradeAura();
    }
  }

  private void UpdateMaxUpgradeAura()
  {
    bool isMax = InventoryManager.HasInstance && InventoryManager.Instance.EquippedWeapon != null && InventoryManager.Instance.EquippedWeaponUpgradeLevel >= 5;
    if (currentAuraInstance != null)
    {
      currentAuraInstance.SetActive(isMax);
    }
    else if (isMax && visualHolder != null)
    {
      GameObject auraObj = new GameObject("[+5_MaxTier_BladeAura]");
      auraObj.transform.SetParent(visualHolder, false);
      auraObj.transform.localPosition = new Vector3(0, 0.45f, 0);

      var light = auraObj.AddComponent<Light>();
      light.type = LightType.Point;
      light.color = new Color(1f, 0.85f, 0.4f, 1f); // Vàng kim ấm áp
      light.range = 1.8f;
      light.intensity = 1.5f;
      light.shadows = LightShadows.None;

      currentAuraInstance = auraObj;
    }
  }

  /// <summary>
  /// Cập nhật hiển thị model 3D kiếm trên tay nhân vật
  /// </summary>
  public void UpdateWeaponVisual(ItemData weaponItem)
  {
    EnsureVisualHolder();
    if (visualHolder == null) return;

    // Xóa model kiếm cũ bên trong visualHolder
    ClearCurrentVisual();

    var hitbox = GetComponentInChildren<WeaponHitbox>(true);

    if (weaponItem == null)
    {
      // 1. KHI THÁO VŨ KHÍ (Ô KIẾM TRỐNG):
      visualHolder.gameObject.SetActive(false);

      if (hitbox != null)
      {
        hitbox.DisableHitbox();
        hitbox.enabled = false;
      }

      Debug.Log("[PlayerWeaponVisualSync] Đã gỡ vũ khí: Ô kiếm trống, tắt mô hình trên tay.");
      return;
    }

    // 2. KHI CÓ VŨ KHÍ TRANG BỊ:
    visualHolder.gameObject.SetActive(true);
    if (hitbox != null)
    {
      hitbox.enabled = true;
    }

    GameObject prefabToSpawn = null;
    if (weaponItem.WeaponVisualPrefab != null)
    {
      prefabToSpawn = weaponItem.WeaponVisualPrefab;
    }
    else if (defaultWeaponPrefab != null)
    {
      prefabToSpawn = defaultWeaponPrefab;
    }

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

      // Đảm bảo model kiếm kế thừa Layer của visualHolder (Layer 3: Player)
      // để Camera xem trước (CharacterPreviewCamera) và ánh sáng có thể soi thấy
      SetLayerRecursively(currentVisualInstance, visualHolder.gameObject.layer);

      Debug.Log($"[PlayerWeaponVisualSync] Đã trang bị thành công mô hình: {prefabToSpawn.name}");
    }
  }

  /// <summary>
  /// Gán layer đệ quy cho GameObject và tất cả các đối tượng con
  /// </summary>
  private void SetLayerRecursively(GameObject obj, int newLayer)
  {
    if (obj == null) return;
    obj.layer = newLayer;
    for (int i = 0; i < obj.transform.childCount; i++)
    {
      SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
    }
  }

  private void ClearCurrentVisual()
  {
    if (visualHolder == null) return;

    // Xóa tất cả các object con trong visualHolder
    for (int i = visualHolder.childCount - 1; i >= 0; i--)
    {
      Transform child = visualHolder.GetChild(i);
      // Bỏ qua nếu có thành phần quan trọng (VFX, Hitbox, Aura...) vô tình nằm dưới
      if (child.GetComponent<WeaponHitbox>() != null || child.GetComponent<ParticleSystem>() != null || child.name == "[+5_MaxTier_BladeAura]")
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
