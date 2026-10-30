using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý việc tính toán tỷ lệ và thả vật phẩm rớt ra đất theo quỹ đạo hình vòng cung (Arc Pop)
  /// Có thể gắn lên Quái vật, Thùng gỗ, hoặc Rương kho báu
  /// Tích hợp thuật toán quét cản thông minh: Chống lọt vào tường, chống lơ lửng trên không và quạt đồ phía trước cho Rương.
  /// </summary>
  public class LootDropper : MonoBehaviour
  {
    [Header("=== PREFAB RƠI ĐỒ CHUNG (ITEMS / GEAR) ===")]
    [Tooltip("Prefab rương nhỏ WorldItemPickup mang model trunk_small_A cho vật phẩm/trang bị")]
    [SerializeField] private GameObject worldPickupPrefab;

    [Header("=== CẤU HÌNH MÔ HÌNH VÀNG DÙNG CHUNG (GLOBAL CONFIG) ===")]
    [Tooltip("File cấu hình hiển thị Vàng dùng chung (Để trống sẽ tự động dùng file GoldDropVisualConfig trong Resources)")]
    [SerializeField] private GoldDropVisualConfig goldVisualConfig;

    [Header("=== BẢNG RƠI ĐỒ (LOOT TABLE) ===")]
    [Tooltip("Danh sách các vật phẩm có thể rơi ra cùng tỉ lệ %")]
    [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();

    [Header("=== RƠI TIỀN VÀNG (GOLD) ===")]
    [Tooltip("Vật phẩm Tiền Vàng")]
    [SerializeField] private ItemData goldItemData;
    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ rơi vàng")]
    [SerializeField] private float goldDropChance = 0.85f;
    [Tooltip("Lượng vàng rơi tối thiểu")]
    [SerializeField] private int minGold = 5;
    [Tooltip("Lượng vàng rơi tối đa")]
    [SerializeField] private int maxGold = 25;

    [Header("=== HIỆU ỨNG VĂNG ĐỒ (ARC POP SETTINGS) ===")]
    [Tooltip("Bán kính văng ngẫu nhiên xung quanh (mét)")]
    [SerializeField] private float minDropRadius = 0.8f;
    [SerializeField] private float maxDropRadius = 2.0f;
    [Tooltip("Độ cao búng nảy hình vòng cung")]
    [SerializeField] private float arcHeight = 1.3f;
    [Tooltip("Thời gian bay nảy tiếp đất (giây)")]
    [SerializeField] private float arcDuration = 0.5f;
    [Range(30f, 360f)]
    [Tooltip("Góc hình nón văng đồ (360 độ = tỏa tròn, ví dụ 130 độ = quạt phía trước cho Rương)")]
    [SerializeField] private float dropConeAngle = 360f;
    [Tooltip("Ưu tiên rơi đồ theo hướng mặt trước (transform.forward) - Rương sẽ tự động kích hoạt")]
    [SerializeField] private bool useForwardCone = false;
    [Tooltip("Khoảng cách tối thiểu giữ an toàn với tường để chống rơi kẹt vào trong tường (mét)")]
    [SerializeField] private float wallSafetyPadding = 0.35f;

    [Header("=== TỰ ĐỘNG KÍCH HOẠT KHI CHẾT ===")]
    [Tooltip("Tự động đăng ký với Health.onDeath nếu có component Health trên đối tượng")]
    [SerializeField] private bool autoDropOnDeath = true;

    private static GoldDropVisualConfig cachedDefaultConfig;
    private Health cachedHealth;
    private bool hasDropped = false;

    private struct DropRequest
    {
      public ItemData Item;
      public int Count;
      public GameObject Prefab;
    }

    private void Awake()
    {
      if (autoDropOnDeath)
      {
        cachedHealth = GetComponent<Health>();
        if (cachedHealth != null)
        {
          cachedHealth.onDeath.AddListener(HandleDeathDrop);
        }
      }

      EnsureGoldConfigLoaded();
    }

    private void EnsureGoldConfigLoaded()
    {
      if (goldVisualConfig == null)
      {
        if (cachedDefaultConfig == null)
        {
          cachedDefaultConfig = Resources.Load<GoldDropVisualConfig>("GoldDropVisualConfig");
        }
        goldVisualConfig = cachedDefaultConfig;
      }
    }

    private void OnDestroy()
    {
      if (cachedHealth != null)
      {
        cachedHealth.onDeath.RemoveListener(HandleDeathDrop);
      }
    }

    private void HandleDeathDrop()
    {
      DropLoot();
    }

    /// <summary>
    /// Kích hoạt tính toán và tung các vật phẩm rơi ra đất với thuật toán chống rơi vào tường & chống lơ lửng
    /// </summary>
    public void DropLoot()
    {
      if (hasDropped) return;
      hasDropped = true;

      EnsureGoldConfigLoaded();

      List<DropRequest> requests = new List<DropRequest>();

      // 1. Tính toán rơi Tiền Vàng theo phân cấp Thresholds
      if (goldItemData != null && Random.value <= goldDropChance)
      {
        int goldAmount = Random.Range(minGold, maxGold + 1);
        if (goldAmount > 0)
        {
          GameObject goldPrefabToUse = (goldVisualConfig != null) 
            ? goldVisualConfig.GetPrefabForAmount(goldAmount, worldPickupPrefab)
            : worldPickupPrefab;

          requests.Add(new DropRequest { Item = goldItemData, Count = goldAmount, Prefab = goldPrefabToUse });
        }
      }

      // 2. Tính toán rơi từng vật phẩm trong bảng Loot (dùng chung rương nhỏ WorldItemPickup)
      if (lootTable != null)
      {
        for (int i = 0; i < lootTable.Count; i++)
        {
          var entry = lootTable[i];
          if (entry != null && entry.item != null)
          {
            if (Random.value <= entry.dropChance)
            {
              int count = entry.EvaluateQuantity();
              if (count > 0)
              {
                requests.Add(new DropRequest { Item = entry.item, Count = count, Prefab = worldPickupPrefab });
              }
            }
          }
        }
      }

      if (requests.Count == 0) return;

      bool isChest = useForwardCone || (GetComponent<ChestInteractable>() != null);

      // Phân bổ rơi từng vật phẩm theo góc quạt để không đè dính chùm
      for (int i = 0; i < requests.Count; i++)
      {
        SpawnSinglePickup(requests[i].Item, requests[i].Count, requests[i].Prefab, i, requests.Count, isChest);
      }
    }

    private void SpawnSinglePickup(ItemData item, int count, GameObject prefabToSpawn, int itemIndex, int totalItems, bool isChest)
    {
      if (prefabToSpawn == null)
      {
        prefabToSpawn = worldPickupPrefab;
      }

      if (prefabToSpawn == null)
      {
        Debug.LogWarning($"[LootDropper] Không có prefab nào được gán để rơi {item?.ItemName} trên {gameObject.name}!");
        return;
      }

      // Điểm xuất phát của hiệu ứng Parabol (với Rương xuất phát từ nắp rương chếch về trước)
      Vector3 spawnOrigin = isChest 
        ? transform.position + Vector3.up * 0.45f + transform.forward * 0.25f 
        : transform.position + Vector3.up * 0.4f;

      // Tính toán điểm tiếp đất an toàn tuyệt đối
      Vector3 targetGroundPos = CalculateSafeLandingPosition(spawnOrigin, itemIndex, totalItems, isChest);

      // Tạo thực thể và kích hoạt bay búng Parabol
      GameObject dropObj = Instantiate(prefabToSpawn, spawnOrigin, Quaternion.identity);
      var pickup = dropObj.GetComponent<WorldItemPickup>();
      if (pickup != null)
      {
        pickup.Initialize(item, count, targetGroundPos, arcHeight, arcDuration);
      }
    }

    /// <summary>
    /// Thuật toán đa lớp tìm điểm tiếp đất an toàn:
    /// - Quét va chạm ngang để chống lọt vào tường hoặc bay qua tường
    /// - Đảm bảo quạt góc thông thoáng (Rương rơi ra phía trước, quái/thùng rơi vào hướng mở)
    /// - Khảo sát sàn nhà thực tế (bỏ qua Player, quái vật, rương, props) để triệt tiêu lỗi lơ lửng
    /// </summary>
    private Vector3 CalculateSafeLandingPosition(Vector3 spawnOrigin, int itemIndex, int totalItems, bool isChest)
    {
      // 1. Khởi tạo LayerMask
      int defaultLayer = LayerMask.NameToLayer("Default");
      int obstacleLayer = LayerMask.NameToLayer("Obstacle");
      int navFloorLayer = LayerMask.NameToLayer("NavMessFloor");
      int terrainLayer = LayerMask.NameToLayer("Terrain");
      int playerLayer = LayerMask.NameToLayer("Player");
      int enemyLayer = LayerMask.NameToLayer("Enemy");
      int damageableLayer = LayerMask.NameToLayer("Damageable");
      int itemLayer = LayerMask.NameToLayer("Item");

      int wallLayerMask = 0;
      if (defaultLayer >= 0) wallLayerMask |= (1 << defaultLayer);
      if (obstacleLayer >= 0) wallLayerMask |= (1 << obstacleLayer);

      int groundLayerMask = 0;
      if (defaultLayer >= 0) groundLayerMask |= (1 << defaultLayer);
      if (navFloorLayer >= 0) groundLayerMask |= (1 << navFloorLayer);
      if (terrainLayer >= 0) groundLayerMask |= (1 << terrainLayer);

      if (playerLayer >= 0) groundLayerMask &= ~(1 << playerLayer);
      if (enemyLayer >= 0) groundLayerMask &= ~(1 << enemyLayer);
      if (damageableLayer >= 0) groundLayerMask &= ~(1 << damageableLayer);
      if (itemLayer >= 0) groundLayerMask &= ~(1 << itemLayer);

      // 2. Xác định hướng góc quạt (Rương luôn ưu tiên rơi ra trước mặt, tránh bức tường sau lưng)
      float baseAngle;
      float coneSpan = dropConeAngle;

      if (isChest)
      {
        baseAngle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        coneSpan = Mathf.Min(coneSpan, 130f);
      }
      else if (useForwardCone)
      {
        baseAngle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
      }
      else
      {
        baseAngle = Random.Range(0f, 360f);
      }

      // Phân bổ góc khi có nhiều vật phẩm cùng rơi một lúc để tạo hình cánh quạt đẹp mắt
      float targetAngle;
      if (totalItems > 1)
      {
        float step = coneSpan / (totalItems + 1);
        float offsetAngle = -coneSpan * 0.5f + step * (itemIndex + 1);
        targetAngle = baseAngle + offsetAngle + Random.Range(-5f, 5f);
      }
      else
      {
        targetAngle = baseAngle + Random.Range(-coneSpan * 0.45f, coneSpan * 0.45f);
      }

      float rad = targetAngle * Mathf.Deg2Rad;
      Vector3 candidateDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
      float chosenDist = Random.Range(minDropRadius, maxDropRadius);

      float maxTestDist = maxDropRadius + 0.6f;
      float clearance = maxTestDist;

      // Đo cự ly tới tường gần nhất theo hướng ứng viên bằng Raycast ngang tại độ cao spawnOrigin
      if (Physics.Raycast(spawnOrigin, candidateDir, out RaycastHit wallHit, maxTestDist, wallLayerMask, QueryTriggerInteraction.Ignore))
      {
        clearance = wallHit.distance;
      }

      // Nếu hướng chính bị tường cản quá gần (< minDropRadius) và không phải rương ép góc:
      // Quét 8 hướng xung quanh để tự động chọn hướng thông thoáng nhất (hướng xa tường nhất)
      if (clearance < minDropRadius && !isChest)
      {
        float bestClearance = clearance;
        Vector3 bestDir = candidateDir;

        for (int i = 0; i < 8; i++)
        {
          float testAngle = (targetAngle + i * 45f) * Mathf.Deg2Rad;
          Vector3 testDir = new Vector3(Mathf.Sin(testAngle), 0f, Mathf.Cos(testAngle)).normalized;
          float testClearance = maxTestDist;

          if (Physics.Raycast(spawnOrigin, testDir, out RaycastHit testHit, maxTestDist, wallLayerMask, QueryTriggerInteraction.Ignore))
          {
            testClearance = testHit.distance;
          }

          if (testClearance > bestClearance)
          {
            bestClearance = testClearance;
            bestDir = testDir;
          }
        }

        candidateDir = bestDir;
        clearance = bestClearance;
      }

      // Giới hạn bán kính rơi đồ: Luôn cách tường ít nhất wallSafetyPadding (0.35m) để chống lọt tường
      float safeMaxRadius = Mathf.Max(0.25f, clearance - wallSafetyPadding);
      float safeDropRadius = Mathf.Clamp(chosenDist, 0.25f, safeMaxRadius);

      Vector3 targetGroundPos = transform.position + candidateDir * safeDropRadius;

      // 3. Kiểm tra trên NavMesh (nếu có): Đảm bảo tọa độ thuộc phạm vi sàn đi lại được của phòng
      if (NavMesh.SamplePosition(targetGroundPos, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
      {
        // Kiểm tra không bị tường chắn ngang giữa điểm sinh và điểm NavMesh
        if (!Physics.Linecast(spawnOrigin, navHit.position + Vector3.up * 0.3f, wallLayerMask, QueryTriggerInteraction.Ignore))
        {
          targetGroundPos = navHit.position;
        }
      }

      // 4. Khảo sát chính xác cao độ mặt sàn bằng RaycastAll (loại bỏ Player, quái vật, rương, thùng)
      Vector3 rayStart = new Vector3(targetGroundPos.x, spawnOrigin.y + 1.2f, targetGroundPos.z);
      RaycastHit[] groundHits = Physics.RaycastAll(rayStart, Vector3.down, 15f, groundLayerMask, QueryTriggerInteraction.Ignore);

      float bestGroundY = transform.position.y;
      bool foundValidGround = false;
      float closestDist = float.MaxValue;

      Collider[] myColliders = GetComponentsInChildren<Collider>(true);

      for (int i = 0; i < groundHits.Length; i++)
      {
        var gh = groundHits[i];
        if (gh.collider == null) continue;

        // Bỏ qua toàn bộ collider của chính đối tượng rơi (rương, quái vật, thùng)
        bool isOwnCollider = false;
        for (int c = 0; c < myColliders.Length; c++)
        {
          if (myColliders[c] == gh.collider)
          {
            isOwnCollider = true;
            break;
          }
        }
        if (isOwnCollider) continue;

        // Bỏ qua người chơi hoặc quái vật lân cận
        if (gh.collider.gameObject.layer == playerLayer || gh.collider.gameObject.layer == enemyLayer) continue;
        if (gh.collider.CompareTag("Player") || gh.collider.CompareTag("Enemy")) continue;
        if (gh.collider.GetComponentInParent<EnemyAIBase>() != null) continue;
        if (gh.collider.GetComponentInParent<Health>() != null) continue;

        // Bỏ qua rương, thùng gỗ hoặc đồ loot khác
        if (gh.collider.GetComponentInParent<ChestInteractable>() != null) continue;
        if (gh.collider.GetComponentInParent<BreakableProp>() != null) continue;
        if (gh.collider.GetComponentInParent<WorldItemPickup>() != null) continue;

        // Bắt buộc phải là mặt phẳng tương đối bằng phẳng (normal.y >= 0.55f) - sàn hoặc bậc cầu thang
        if (gh.normal.y >= 0.55f)
        {
          if (gh.distance < closestDist)
          {
            closestDist = gh.distance;
            bestGroundY = gh.point.y;
            foundValidGround = true;
          }
        }
      }

      if (foundValidGround)
      {
        targetGroundPos.y = bestGroundY + 0.12f;
      }
      else
      {
        targetGroundPos.y = transform.position.y + 0.12f;
      }

      return targetGroundPos;
    }
  }
}
