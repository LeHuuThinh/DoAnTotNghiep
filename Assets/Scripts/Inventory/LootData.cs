using System;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Cấu hình một mục rơi đồ trong bảng chiến lợi phẩm
  /// </summary>
  [Serializable]
  public class LootEntry
  {
    [Tooltip("Vật phẩm rơi (nếu không dùng danh sách ngẫu nhiên randomItemPool)")]
    public ItemData item;

    [Tooltip("Danh sách vật phẩm ngẫu nhiên để chọn 1 (nếu có phần tử, sẽ random chọn 1 món trong danh sách này khi trúng tỉ lệ)")]
    public System.Collections.Generic.List<ItemData> randomItemPool = new System.Collections.Generic.List<ItemData>();

    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ rơi (1 = 100%, 0.5 = 50%, 0.1 = 10%)")]
    public float dropChance = 1.0f;

    [Tooltip("Số lượng rơi tối thiểu")]
    public int minQuantity = 1;

    [Tooltip("Số lượng rơi tối đa")]
    public int maxQuantity = 1;

    public int EvaluateQuantity()
    {
      if (minQuantity >= maxQuantity) return minQuantity;
      return UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
    }

    /// <summary>
    /// Lấy vật phẩm rơi thực tế: nếu có randomItemPool thì bốc ngẫu nhiên 1 món, ngược lại trả về item cố định
    /// </summary>
    public ItemData ResolveItem()
    {
      if (randomItemPool != null && randomItemPool.Count > 0)
      {
        var validPool = randomItemPool.FindAll(x => x != null);
        if (validPool.Count > 0)
        {
          int index = UnityEngine.Random.Range(0, validPool.Count);
          return validPool[index];
        }
      }
      return item;
    }
  }
}
