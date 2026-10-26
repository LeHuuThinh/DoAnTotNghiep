using System.Collections.Generic;
using UnityEngine;

public partial class DungeonPropDecorator
{
  /// <summary>
  /// Thuật toán đánh dấu các ô cấm không được đặt đồ (lối đi từ cửa vào tâm, chân thang, bệ đón thang)
  /// </summary>
  private HashSet<Vector2Int> BuildForbiddenTiles(RoomData room, DungeonData data)
  {
    HashSet<Vector2Int> forbidden = new HashSet<Vector2Int>();

    if (room.doorTiles != null)
    {
      foreach (var d in room.doorTiles)
      {
        forbidden.Add(d);
        forbidden.Add(new Vector2Int(d.x + 1, d.y));
        forbidden.Add(new Vector2Int(d.x - 1, d.y));
        forbidden.Add(new Vector2Int(d.x, d.y + 1));
        forbidden.Add(new Vector2Int(d.x, d.y - 1));

        Vector2Int current = d;
        Vector2Int target = room.Center;
        int maxSteps = 30;
        while (current != target && maxSteps-- > 0)
        {
          forbidden.Add(current);
          if (current.x < target.x) current.x++;
          else if (current.x > target.x) current.x--;
          else if (current.y < target.y) current.y++;
          else if (current.y > target.y) current.y--;
        }
        forbidden.Add(target);
      }
    }

    if (room.hasMezzanine)
    {
      if (room.stairLandingTiles != null)
      {
        foreach (var landing in room.stairLandingTiles)
        {
          forbidden.Add(landing);
          forbidden.Add(new Vector2Int(landing.x + 1, landing.y));
          forbidden.Add(new Vector2Int(landing.x - 1, landing.y));
          forbidden.Add(new Vector2Int(landing.x, landing.y + 1));
          forbidden.Add(new Vector2Int(landing.x, landing.y - 1));
        }
      }

      int sx = room.stairGridPos.x;
      int sy = room.stairGridPos.y;
      for (int x = sx - 1; x <= sx + 2; x++)
      {
        for (int y = sy - 4; y <= sy + 4; y++)
        {
          forbidden.Add(new Vector2Int(x, y));
        }
      }
    }

    return forbidden;
  }

  /// <summary>
  /// Kiểm tra xem tại vị trí (x, y) theo hướng side có thực sự là một bức tường đá đặc không.
  /// Tuyệt đối ngăn chặn hiện tượng đuốc hoặc cờ bay lơ lửng ngay vòm cửa / lối đi hở.
  /// </summary>
  private bool HasSolidWall(int x, int y, int side, DungeonData data, RoomData room)
  {
    int nx = x;
    int ny = y;
    switch (side)
    {
      case 0: ny = y + 1; break; // North
      case 1: ny = y - 1; break; // South
      case 2: nx = x - 1; break; // West
      case 3: nx = x + 1; break; // East
    }

    if (!data.IsInBounds(nx, ny)) return true;
    if (data.mapGrid[nx, ny] != 0) return false;

    // Không gắn nếu ô ngay sát cửa ra vào
    if (room.doorTiles != null)
    {
      foreach (var d in room.doorTiles)
      {
        if (Mathf.Abs(x - d.x) + Mathf.Abs(y - d.y) <= 1) return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Kiểm tra ô có nằm sát bất kỳ cửa ra vào nào của hầm ngục không
  /// </summary>
  private bool IsNearAnyDoorway(int x, int y, DungeonData data)
  {
    if (data.roomDataList == null) return false;
    foreach (var r in data.roomDataList)
    {
      if (r.doorTiles != null)
      {
        foreach (var d in r.doorTiles)
        {
          if (Mathf.Abs(x - d.x) + Mathf.Abs(y - d.y) <= 1)
          {
            return true;
          }
        }
      }
    }
    return false;
  }

  /// <summary>
  /// Trộn ngẫu nhiên mảng
  /// </summary>
  private void ShuffleArray<T>(T[] array)
  {
    for (int i = 0; i < array.Length; i++)
    {
      int rnd = Random.Range(i, array.Length);
      T temp = array[i];
      array[i] = array[rnd];
      array[rnd] = temp;
    }
  }

  /// <summary>
  /// Chọn bộ cờ hiệu cùng tone màu cho một căn phòng
  /// </summary>
  private List<GameObject> GetRoomBannerFamily(int roomId)
  {
    if (bannerPrefabs == null || bannerPrefabs.Count == 0) return null;
    string[] colors = new string[] { "blue", "red", "green", "yellow", "brown", "white" };
    string chosenColor = colors[Mathf.Abs(roomId) % colors.Length];

    List<GameObject> matched = new List<GameObject>();
    foreach (var b in bannerPrefabs)
    {
      if (b.name.ToLower().Contains(chosenColor)) matched.Add(b);
    }
    return matched.Count > 0 ? matched : bannerPrefabs;
  }
}
