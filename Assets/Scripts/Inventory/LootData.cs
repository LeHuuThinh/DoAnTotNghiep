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
    [Tooltip("Vật phẩm rơi")]
    public ItemData item;

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
  }
}
