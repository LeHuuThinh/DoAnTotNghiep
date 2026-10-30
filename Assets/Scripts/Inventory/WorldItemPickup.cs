using System;
using System.Collections;
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

    [Header("=== TƯƠNG TÁC NHẶT ĐỒ ===")]
    [Tooltip("Bán kính người chơi có thể bấm F để nhặt")]
    [SerializeField] private float pickupRadius = 2.2f;
    [Tooltip("Bán kính hút tự động nếu là Tiền Vàng")]
    [SerializeField] private float magneticRadius = 3.8f;
    [Tooltip("Tốc độ bay hút về phía người chơi")]
    [SerializeField] private float magneticSpeed = 9f;

    // Static Events cho hệ thống HUD Pickup Prompt (Task RPG-03)
    public static event Action<WorldItemPickup> OnFocusPickup;
    public static event Action<WorldItemPickup> OnUnfocusPickup;

    // Public Getters
    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public bool IsPlayerInRange => isPlayerInRange;

    private Transform playerTransform;
    private bool isPlayerInRange = false;
    private bool isBeingMagnetized = false;
    private bool isLanded = false;
    private Vector3 baseVisualLocalPos;
    private float bobTimer = 0f;

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
        rarityLight = GetComponentInChildren<Light>();
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

      // Cập nhật màu và kích thước đèn hào quang theo phẩm cấp (thu nhỏ gọn gàng, không lóa mắt)
      if (rarityLight != null)
      {
        rarityLight.color = rarityColor;
        rarityLight.range = lightRange;
        rarityLight.intensity = (itemData.Rarity == ItemRarity.Legendary) ? legendaryLightIntensity : lightIntensity;
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

      // 3. Tương tác nhặt bằng phím [F] cho trang bị / vật phẩm thường
      if (distToPlayer <= pickupRadius)
      {
        if (!isPlayerInRange)
        {
          isPlayerInRange = true;
          OnFocusPickup?.Invoke(this);
        }

        // Bấm phím F để nhặt
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
          CollectItem();
        }
      }
      else
      {
        if (isPlayerInRange)
        {
          isPlayerInRange = false;
          OnUnfocusPickup?.Invoke(this);
        }
      }
    }

    private void CollectItem()
    {
      if (InventoryManager.HasInstance)
      {
        bool success = InventoryManager.Instance.AddItem(itemData, quantity, out int remainder);
        if (success)
        {
          if (remainder <= 0)
          {
            if (isPlayerInRange)
            {
              OnUnfocusPickup?.Invoke(this);
            }
            Destroy(gameObject);
          }
          else
          {
            // Nếu túi đồ chỉ nhặt được một phần vì sắp đầy
            quantity = remainder;
            Debug.Log($"[Pickup] Túi đồ đã đầy, còn lại {remainder} {itemData.ItemName} chưa thể nhặt!");
          }
        }
        else
        {
          Debug.LogWarning("[Pickup] Túi đồ đã đầy hoàn toàn, không thể nhặt thêm!");
        }
      }
      else
      {
        Debug.LogWarning("[Pickup] Không tìm thấy InventoryManager trong Scene!");
      }
    }

    private void OnDestroy()
    {
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
