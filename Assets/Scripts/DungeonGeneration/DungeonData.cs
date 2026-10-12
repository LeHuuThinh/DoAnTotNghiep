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

  // Kiểm tra an toàn xem tọa độ có lọt ra ngoài mảng không
  public bool IsInBounds(int x, int y)
  {
    if (mapGrid == null) return false;
    return x >= 0 && x < mapGrid.GetLength(0) && y >= 0 && y < mapGrid.GetLength(1);
  }
}