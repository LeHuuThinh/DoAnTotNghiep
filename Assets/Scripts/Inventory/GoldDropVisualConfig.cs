using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Cấu hình dùng chung toàn cục cho mô hình hiển thị Tiền Vàng rơi ra thế giới.
  /// Chỉ cần chỉnh sửa 1 file ScriptableObject này, toàn bộ quái vật, thùng gỗ và rương trong game sẽ tự động cập nhật!
  /// </summary>
  [CreateAssetMenu(fileName = "GoldDropVisualConfig", menuName = "Infinite Labyrinth/RPG/Gold Drop Visual Config")]
  public class GoldDropVisualConfig : ScriptableObject
  {
    [Header("=== PREFABS HIỂN THỊ (4 CẤP ĐỘ) ===")]
    [Tooltip("Mô hình 1 đồng tiền vàng lẻ")]
    public GameObject singleCoinPrefab;
    [Tooltip("Mô hình cụm vàng nhỏ xếp chồng")]
    public GameObject smallStackPrefab;
    [Tooltip("Mô hình cụm vàng vừa")]
    public GameObject mediumStackPrefab;
    [Tooltip("Mô hình đống vàng to tướng rực rỡ (cho Rương Vàng / Boss)")]
    public GameObject largeStackPrefab;

    [Header("=== NGƯỠNG SỐ LƯỢNG (GOLD THRESHOLDS) ===")]
    [Tooltip("Dưới mức này hiển thị 1 đồng xu lẻ (Mặc định: 10)")]
    public int smallThreshold = 10;

    [Tooltip("Từ smallThreshold đến dưới mức này hiển thị cụm nhỏ (Mặc định: 35)")]
    public int mediumThreshold = 35;

    [Tooltip("Từ mediumThreshold đến dưới mức này hiển thị cụm vừa. Từ mức này trở lên hiển thị đống vàng lớn (Mặc định: 75)")]
    public int largeThreshold = 75;

    /// <summary>
    /// Lấy Prefab phù hợp tương ứng với số lượng vàng
    /// </summary>
    public GameObject GetPrefabForAmount(int amount, GameObject fallbackPrefab = null)
    {
      if (amount < smallThreshold && singleCoinPrefab != null)
        return singleCoinPrefab;

      if (amount < mediumThreshold && smallStackPrefab != null)
        return smallStackPrefab;

      if (amount < largeThreshold && mediumStackPrefab != null)
        return mediumStackPrefab;

      if (largeStackPrefab != null)
        return largeStackPrefab;

      return fallbackPrefab;
    }
  }
}
