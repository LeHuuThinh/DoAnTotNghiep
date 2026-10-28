using UnityEngine;
using System.Collections.Generic;

// Chứa thông tin về bản vẽ hầm ngục sau khi thuật toán chạy xong
public class DungeonData
{
  public int mapWidth;
  public int mapHeight;
  public int startRoomHeight;

  public int[,] mapGrid;
  public List<RectInt> rooms = new List<RectInt>();
  public List<RectInt> corridors = new List<RectInt>();
  public BSPNode rootNode;

  [Header("Dữ liệu Phòng Chi Tiết (Kiến trúc & Gameplay)")]
  public List<RoomData> roomDataList = new List<RoomData>();
  public RoomData startRoom;
  public RoomData bossRoom;
  public RoomData bossPrevRoom;   // Phòng kề trước phòng Boss
  public RoomData keyRoom;        // Phòng tế đàn nhánh cụt chứa Chìa Khóa Hầm Ngục
  public bool hasSpawnedDungeonKey; // Cờ đánh dấu đã sinh chìa khóa hầm ngục
  public RectInt bossRoomBounds;   // Giới hạn căn phòng Boss Khổng Lồ do BSP tạo ra
  public RectInt bossPrevRoomBounds; // Giới hạn căn phòng kề nối vào cửa ngõ Đại Lộ Boss
  public RectInt bossCorridor;     // Hành lang nối trực tiếp vào phòng Boss (được mở rộng gấp đôi)
  public Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>(); // Đồ thị kề liên kết giữa các phòng qua hành lang

  // Kiểm tra an toàn xem tọa độ có lọt ra ngoài mảng không
  public bool IsInBounds(int x, int y)
  {
    if (mapGrid == null) return false;
    return x >= 0 && x < mapGrid.GetLength(0) && y >= 0 && y < mapGrid.GetLength(1);
  }

  /// <summary>
  /// Tìm căn phòng chứa tọa độ lưới (gridX, gridY)
  /// </summary>
  public RoomData GetRoomAt(int x, int y)
  {
    if (roomDataList == null) return null;
    foreach (var r in roomDataList)
    {
      if (x >= r.bounds.x && x < r.bounds.x + r.bounds.width &&
          y >= r.bounds.y && y < r.bounds.y + r.bounds.height)
      {
        return r;
      }
    }
    return null;
  }

  /// <summary>
  /// Kiểm tra xem tọa độ lưới (x, y) có nằm trong bất kỳ căn phòng nào không
  /// </summary>
  public bool IsInsideAnyRoom(int x, int y)
  {
    if (rooms != null)
    {
      for (int i = 0; i < rooms.Count; i++)
      {
        RectInt r = rooms[i];
        if (x >= r.x && x < r.x + r.width && y >= r.y && y < r.y + r.height)
          return true;
      }
    }
    return false;
  }
}
