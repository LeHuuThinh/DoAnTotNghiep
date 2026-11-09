using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý vật phẩm rơi trong thế giới 3D (Rương mini trunk_small_A xoay lơ lửng, đổi màu tia sáng theo độ hiếm)
  /// </summary>
  public class WorldItemPickup : MonoBehaviour
  {
    [Header("=== DỮ LIỆU VẬT PHẨM ===")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;

    [Header("=== THIẾT LẬP HIỂN THỊ & HIỆU ỨNG ===")]
    [Tooltip("Transform chứa model 3D (để xoay và nhấp nhô)")]
    [SerializeField] private Transform visualTransform;
    [Tooltip("Đèn hào quang phát sáng theo độ hiếm")]
    [SerializeField] private Light rarityLight;
    [Tooltip("Bán kính chiếu sáng của đèn (mét) - đã tinh chỉnh nhỏ gọn tinh tế")]
    [SerializeField] private float lightRange = 1.4f;
    [Tooltip("Cường độ phát sáng thông thường")]
    [SerializeField] private float lightIntensity = 1.5f;
    [Tooltip("Cường độ phát sáng cho đồ Legendary")]
    [SerializeField] private float legendaryLightIntensity = 2.2f;
    [Tooltip("Tốc độ xoay (độ / giây)")]
    [SerializeField] private float rotationSpeed = 60f;
    [Tooltip("Biên độ nhấp nhô bồng bềnh")]
    [SerializeField] private float bobAmplitude = 0.05f;
    [Tooltip("Tần số nhấp nhô")]
    [SerializeField] private float bobFrequency = 2.5f;
    [Tooltip("Tỉ lệ scale mô hình vũ khí khi rơi ra đất")]
    [SerializeField] private float weaponDropScale = 0.6f;

    [Header("=== TƯƠNG TÁC NHẶT ĐỒ ===")]
    [Tooltip("Bán kính người chơi có thể bấm F để nhặt")]
    [SerializeField] private float pickupRadius = 2.2f;
    [Tooltip("Bán kính hút tự động nếu là Tiền Vàng")]
    [SerializeField] private float magneticRadius = 3.8f;
    [Tooltip("Tốc độ bay hút về phía người chơi")]
    [SerializeField] private float magneticSpeed = 9f;

    // Sổ bộ các vật phẩm đang rơi hoạt động trong Scene (hỗ trợ PlayerInteraction quét danh sách tương tác dọc)
    private static readonly List<WorldItemPickup> activePickups = new List<WorldItemPickup>();
    public static IReadOnlyList<WorldItemPickup> ActivePickups => activePickups;

    // Static Events cho hệ thống HUD Pickup Prompt (Task RPG-03)
    public static event Action<WorldItemPickup> OnFocusPickup;
    public static event Action<WorldItemPickup> OnUnfocusPickup;

    // Public Getters
    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public bool IsPlayerInRange => isPlayerInRange;
    public float PickupRadius => pickupRadius;
    public bool IsLanded => isLanded;

    private Transform playerTransform;
    private bool isPlayerInRange = false;
    private bool isBeingMagnetized = false;
    private bool isLanded = false;
    private Vector3 baseVisualLocalPos;
    private float bobTimer = 0f;

    private void OnEnable()
    {
      if (!activePickups.Contains(this))
      {
        activePickups.Add(this);
      }
    }

    private void OnDisable()
    {
      activePickups.Remove(this);
      if (isPlayerInRange)
      {
        isPlayerInRange = false;
        OnUnfocusPickup?.Invoke(this);
      }
    }

    private void Awake()
    {
      // 1. Chống đẩy Player & chống giật Camera: Bắt buộc TẤT CẢ Collider (cả root và con) phải là Trigger!
      Collider[] allCols = GetComponentsInChildren<Collider>(true);
      foreach (var col in allCols)
      {
        col.isTrigger = true;
      }

      // 2. Chuyển toàn bộ sang Layer "Item" (Layer 11) hoặc fallback sang "Ignore Raycast" (Layer 2)
      int itemLayer = LayerMask.NameToLayer("Item");
      if (itemLayer < 0) itemLayer = LayerMask.NameToLayer("Ignore Raycast");

      if (itemLayer >= 0)
      {
        SetLayerRecursively(gameObject, itemLayer);

        // Vô hiệu hóa va chạm vật lý giữa Player và Item (chống húc đẩy Player)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
          Physics.IgnoreLayerCollision(playerLayer, itemLayer, true);
        }
      }

      if (visualTransform == null)
      {
        visualTransform = transform.Find("Model") ?? transform;
      }
      baseVisualLocalPos = visualTransform.localPosition;

      if (rarityLight == null)
      {
        rarityLight = GetComponentInChildren<Light>(true);
      }

      // Đảm bảo có trigger collider ở root nếu chưa có
      var rootCol = GetComponent<SphereCollider>();
      if (rootCol == null)
      {
        rootCol = gameObject.AddComponent<SphereCollider>();
        rootCol.isTrigger = true;
        rootCol.radius = pickupRadius;
      }

      // Vô hiệu hóa vật lý Rigidbody để chuyển động Parabol kinematically mượt mà 100%
      var rb = GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = true;
      }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
      obj.layer = layer;
      foreach (Transform child in obj.transform)
      {
        SetLayerRecursively(child.gameObject, layer);
      }
    }

    private void Start()
    {
      LocatePlayer();
      ApplyRarityVisuals();
      if (!isLanded && itemData != null)
      {
        isLanded = true;
      }
    }

    /// <summary>
    /// Khởi tạo vật phẩm đặt tĩnh (hoặc lơ lửng trên bệ/tế đàn) không cần nảy parabol
    /// </summary>
    public void InitializeStatic(ItemData data, int count = 1)
    {
      itemData = data;
      quantity = count;
      isLanded = true;
      ApplyRarityVisuals();
    }

    private void LocatePlayer()
    {
      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        playerTransform = playerObj.transform;
      }
    }

    /// <summary>
    /// Khởi tạo dữ liệu và kích hoạt hiệu ứng nảy búng vòng cung parabol (Arc Pop)
    /// </summary>
    public void Initialize(ItemData data, int count, Vector3 targetGroundPos, float arcHeight = 1.2f, float duration = 0.5f)
    {
      itemData = data;
      quantity = count;
      ApplyRarityVisuals();

      StartCoroutine(ArcPopRoutine(targetGroundPos, arcHeight, duration));
    }

    private void ApplyRarityVisuals()
    {
      if (itemData == null) return;

      Color rarityColor = itemData.Rarity.GetRarityColor();

      // Cập nhật màu và kích thước đèn hào quang theo phẩm cấp
      if (rarityLight == null)
      {
        rarityLight = GetComponentInChildren<Light>(true);
      }
      if (rarityLight == null)
      {
        GameObject glowObj = new GameObject("RarityGlow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        rarityLight = glowObj.AddComponent<Light>();
        rarityLight.type = LightType.Point;
        rarityLight.shadows = LightShadows.None;
      }

      if (rarityLight != null)
      {
        rarityLight.color = rarityColor;
        rarityLight.range = (itemData.Rarity >= ItemRarity.Epic) ? 1.8f : lightRange;
        float intensity = lightIntensity;
        switch (itemData.Rarity)
        {
          case ItemRarity.Common: intensity = 1.5f; break;
          case ItemRarity.Uncommon: intensity = 1.8f; break;
          case ItemRarity.Rare: intensity = 2.4f; break;
          case ItemRarity.Epic: intensity = 3.0f; break;
          case ItemRarity.Legendary: intensity = 3.8f; break;
        }
        rarityLight.intensity = intensity;
        rarityLight.enabled = true;
        if (!rarityLight.gameObject.activeSelf)
        {
          rarityLight.gameObject.SetActive(true);
        }
      }

      // Thiết lập mô hình 3D trực quan (hiển thị model kiếm nếu là vũ khí)
      SetupVisualModel();
    }

    private void SetupVisualModel()
    {
      if (itemData == null) return;

      if (visualTransform == null)
      {
        visualTransform = transform.Find("Model") ?? transform;
      }

      // 1. Nếu là Vũ khí và có WeaponVisualPrefab -> hiển thị chính model thanh kiếm 3D!
      if (itemData.WeaponVisualPrefab != null)
      {
        // Ẩn model rương mặc định (trunk_small_A)
        for (int i = 0; i < visualTransform.childCount; i++)
        {
          Transform child = visualTransform.GetChild(i);
          if (child.name != "SpawnedWeaponVisual")
          {
            child.gameObject.SetActive(false);
          }
        }

        // Tạo mô hình vũ khí nếu chưa có
        Transform existingWeapon = visualTransform.Find("SpawnedWeaponVisual");
        if (existingWeapon == null)
        {
          GameObject weaponObj = Instantiate(itemData.WeaponVisualPrefab, visualTransform);
          weaponObj.name = "SpawnedWeaponVisual";
          weaponObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
          // Xoay kiếm cắm nghiêng 40 độ tạo tư thế kiếm cắm đất xoay tròn
          weaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
          weaponObj.transform.localScale = Vector3.one * weaponDropScale;

          // Vô hiệu hóa Collider và Script trên vũ khí để không gây cản trở
          Collider[] cols = weaponObj.GetComponentsInChildren<Collider>(true);
          foreach (var c in cols) c.enabled = false;

          MonoBehaviour[] scripts = weaponObj.GetComponentsInChildren<MonoBehaviour>(true);
          foreach (var s in scripts) s.enabled = false;

          int itemLayer = LayerMask.NameToLayer("Item");
          if (itemLayer >= 0) SetLayerRecursively(weaponObj, itemLayer);
        }
        else
        {
          existingWeapon.gameObject.SetActive(true);
          existingWeapon.localScale = Vector3.one * weaponDropScale;
        }
      }
      else
      {
        // 2. Nếu là các vật phẩm khác (Bình máu, Giáp, Chìa khóa, Nguyên liệu...) -> hiển thị rương mini
        for (int i = 0; i < visualTransform.childCount; i++)
        {
          Transform child = visualTransform.GetChild(i);
          if (child.name == "SpawnedWeaponVisual")
          {
            child.gameObject.SetActive(false);
          }
          else
          {
            child.gameObject.SetActive(true);
          }
        }
      }
    }

    private IEnumerator ArcPopRoutine(Vector3 targetPos, float arcHeight, float duration)
    {
      isLanded = false;
      Vector3 startPos = transform.position;
      float timer = 0f;

      while (timer < duration)
      {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Nội suy vị trí ngang
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
        // Cộng thêm độ cao Parabol hình sin
        currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

        transform.position = currentPos;
        yield return null;
      }

      // Khảo sát kiểm tra mặt sàn thực tế tại điểm rơi để chống tuyệt đối hiện tượng lơ lửng trên không
      int groundLayerMask = (1 << LayerMask.NameToLayer("NavMessFloor")) | (1 << LayerMask.NameToLayer("Default"));
      int terrainLayer = LayerMask.NameToLayer("Terrain");
      if (terrainLayer >= 0) groundLayerMask |= (1 << terrainLayer);

      int playerLayer = LayerMask.NameToLayer("Player");
      int enemyLayer = LayerMask.NameToLayer("Enemy");
      int damageableLayer = LayerMask.NameToLayer("Damageable");
      int itemLayer = LayerMask.NameToLayer("Item");
      if (playerLayer >= 0) groundLayerMask &= ~(1 << playerLayer);
      if (enemyLayer >= 0) groundLayerMask &= ~(1 << enemyLayer);
      if (damageableLayer >= 0) groundLayerMask &= ~(1 << damageableLayer);
      if (itemLayer >= 0) groundLayerMask &= ~(1 << itemLayer);

      RaycastHit[] hits = Physics.RaycastAll(targetPos + Vector3.up * 0.6f, Vector3.down, 1.8f, groundLayerMask, QueryTriggerInteraction.Ignore);
      float closestFloorDist = float.MaxValue;
      float bestFloorY = targetPos.y;
      bool hasGround = false;

      for (int i = 0; i < hits.Length; i++)
      {
        var h = hits[i];
        if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
        if (h.collider.CompareTag("Player") || h.collider.CompareTag("Enemy")) continue;
        if (h.normal.y >= 0.55f && h.distance < closestFloorDist)
        {
          closestFloorDist = h.distance;
          bestFloorY = h.point.y;
          hasGround = true;
        }
      }

      if (hasGround)
      {
        targetPos.y = bestFloorY + 0.12f;
      }

      transform.position = targetPos;
      isLanded = true;
    }

    private void Update()
    {
      if (itemData == null) return;

      // Không cho phép tương tác nhặt đồ khi chưa tiếp đất xong
      if (!isLanded) return;

      // 1. Hiệu ứng xoay tròn và nhấp nhô lơ lửng khi đã tiếp đất
      if (visualTransform != null && isLanded && !isBeingMagnetized)
      {
        visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        bobTimer += Time.deltaTime * bobFrequency;
        Vector3 newPos = baseVisualLocalPos;
        newPos.y += Mathf.Sin(bobTimer) * bobAmplitude;
        visualTransform.localPosition = newPos;
      }

      if (playerTransform == null)
      {
        LocatePlayer();
        if (playerTransform == null) return;
      }

      float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

      // 2. Tự động hút nam châm (Magnetic Pickup) nếu là Tiền Vàng
      if (itemData.ItemType == ItemType.Currency && isLanded)
      {
        if (distToPlayer <= magneticRadius)
        {
          isBeingMagnetized = true;
          // Bay nhanh dần về ngực người chơi (+1.0m Y)
          Vector3 targetChest = playerTransform.position + Vector3.up * 1.0f;
          transform.position = Vector3.MoveTowards(transform.position, targetChest, magneticSpeed * Time.deltaTime);

          if (Vector3.Distance(transform.position, targetChest) < 0.35f)
          {
            CollectItem();
            return;
          }
        }
      }

      // Lưu ý: Việc bắt phím F và lựa chọn nhặt đơn lẻ được chuyển giao hoàn toàn
      // cho PlayerInteraction điều phối qua danh sách tương tác dọc cuộn chuột (Vertical Interaction List).
    }

    /// <summary>
    /// Kích hoạt nhặt vật phẩm vào kho đồ (được gọi từ PlayerInteraction khi đối tượng này đang được Focus/Selected)
    /// </summary>
    public bool Collect(GameObject player)
    {
      return CollectItem();
    }

    public bool CollectItem()
    {
      if (InventoryManager.HasInstance)
      {
        bool success = InventoryManager.Instance.AddItem(itemData, quantity, out int remainder);
        if (success)
        {
          if (remainder <= 0)
          {
            activePickups.Remove(this);
            if (isPlayerInRange)
            {
              isPlayerInRange = false;
              OnUnfocusPickup?.Invoke(this);
            }
            Destroy(gameObject);
            return true;
          }
          else
          {
            // Nếu túi đồ chỉ nhặt được một phần vì sắp đầy
            quantity = remainder;
            Debug.Log($"[Pickup] Túi đồ đã đầy, còn lại {remainder} {itemData.ItemName} chưa thể nhặt!");
            return true;
          }
        }
        else
        {
          Debug.LogWarning("[Pickup] Túi đồ đã đầy hoàn toàn, không thể nhặt thêm!");
          return false;
        }
      }
      else
      {
        Debug.LogWarning("[Pickup] Không tìm thấy InventoryManager trong Scene!");
        return false;
      }
    }

    private void OnDestroy()
    {
      activePickups.Remove(this);
      if (isPlayerInRange)
      {
        OnUnfocusPickup?.Invoke(this);
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, pickupRadius);
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, magneticRadius);
    }
#endif
  }
}
