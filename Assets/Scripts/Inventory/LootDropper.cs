using System.Collections.Generic;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý việc tính toán tỷ lệ và thả vật phẩm rớt ra đất theo quỹ đạo hình vòng cung (Arc Pop)
  /// Có thể gắn lên Quái vật, Thùng gỗ, hoặc Rương kho báu
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

    [Header("=== TỰ ĐỘNG KÍCH HOẠT KHI CHẾT ===")]
    [Tooltip("Tự động đăng ký với Health.onDeath nếu có component Health trên đối tượng")]
    [SerializeField] private bool autoDropOnDeath = true;

    private static GoldDropVisualConfig cachedDefaultConfig;
    private Health cachedHealth;
    private bool hasDropped = false;

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
    /// Kích hoạt tính toán và tung các vật phẩm rơi ra đất
    /// </summary>
    public void DropLoot()
    {
      if (hasDropped) return;
      hasDropped = true;

      EnsureGoldConfigLoaded();

      // 1. Tính toán rơi Tiền Vàng theo phân cấp Thresholds
      if (goldItemData != null && Random.value <= goldDropChance)
      {
        int goldAmount = Random.Range(minGold, maxGold + 1);
        if (goldAmount > 0)
        {
          GameObject goldPrefabToUse = (goldVisualConfig != null) 
            ? goldVisualConfig.GetPrefabForAmount(goldAmount, worldPickupPrefab)
            : worldPickupPrefab;

          SpawnSinglePickup(goldItemData, goldAmount, goldPrefabToUse);
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
                SpawnSinglePickup(entry.item, count, worldPickupPrefab);
              }
            }
          }
        }
      }
    }

    private void SpawnSinglePickup(ItemData item, int count, GameObject prefabToSpawn)
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

      Vector3 spawnOrigin = transform.position + Vector3.up * 0.4f;

      // Tính toán điểm tiếp đất ngẫu nhiên theo hình tròn xung quanh
      float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
      float randomRadius = Random.Range(minDropRadius, maxDropRadius);
      Vector3 offset = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle)) * randomRadius;
      Vector3 targetGroundPos = transform.position + offset;

      // Đo chuẩn mặt sàn bằng Raycast để chống xuyên sàn
      if (Physics.Raycast(targetGroundPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4f))
      {
        targetGroundPos = hit.point + Vector3.up * 0.15f;
      }

      // Tạo thực thể và kích hoạt bay búng Parabol
      GameObject dropObj = Instantiate(prefabToSpawn, spawnOrigin, Quaternion.identity);
      var pickup = dropObj.GetComponent<WorldItemPickup>();
      if (pickup != null)
      {
        pickup.Initialize(item, count, targetGroundPos, arcHeight, arcDuration);
      }
    }
  }
}
