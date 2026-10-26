using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Định danh vai trò và chức năng của từng căn phòng trong hầm ngục
/// </summary>
public enum RoomType
{
  Start,        // Phòng xuất phát an toàn
  Normal,       // Phòng chiến đấu tiêu chuẩn (sàn phẳng, trần 4m)
  Mezzanine,    // Phòng đại sảnh 2 tầng có gác lửng và cầu thang (trần 8m)
  Treasure,     // Phòng kho báu ở nhánh cụt
  Boss,         // Phòng trùm rộng lớn, trần cao 12m
  Shrine        // Phòng tế đàn / điện thờ ban phước
}

/// <summary>
/// Hướng của vách tường mà gác lửng tựa vào
/// </summary>
public enum MezzanineWallSide
{
  North,
  South,
  East,
  West
}

/// <summary>
/// <summary>
/// Hình dạng kiến trúc của tầng 2 (Gác lửng)
/// </summary>
public enum MezzanineShape
{
  Rectangle, // Hình chữ nhật truyền thống (nửa phòng)
  LShape,    // Hình chữ L chạy dọc 2 vách tường
  UShape     // Hình chữ U chạy dọc 3 vách tường (giếng trời trung tâm)
}

/// <summary>
/// Lưu trữ toàn bộ thông tin kiến trúc, cấu trúc tầng và dữ liệu gameplay của một căn phòng
/// </summary>
[System.Serializable]
public class RoomData
{
  [Header("Cơ bản")]
  public int id;
  public RectInt bounds;
  public RoomType type = RoomType.Normal;
  public float ceilingHeight = 4f;

  [Header("Cấu trúc Tầng 2 (Gác lửng - Mezzanine)")]
  public bool hasMezzanine = false;
  public MezzanineShape mezzanineShape = MezzanineShape.Rectangle;
  public RectInt mezzanineBounds;
  [System.NonSerialized]
  public HashSet<Vector2Int> mezzanineTiles = new HashSet<Vector2Int>();
  public float mezzanineHeight = 8f;
  public MezzanineWallSide mezzanineSide = MezzanineWallSide.North;

  [Header("Cầu thang 2 nhịp 4m")]
  public Vector3 stairsPosition; // Tọa độ chân thang tổng thể
  public Vector3 stairsLowerPosition; // Vị trí nhịp thang dưới (0 -> 4m)
  public Vector3 stairsUpperPosition; // Vị trí nhịp thang trên (4m -> 8m)
  public Quaternion stairsRotation = Quaternion.identity;
  public Vector2Int stairGridPos;
  [System.NonSerialized]
  public HashSet<Vector2Int> stairLandingTiles = new HashSet<Vector2Int>(); // Các ô đón cầu thang (không dựng lan can)

  [Header("Cột đỡ")]
  public List<Vector3> pillarPositions = new List<Vector3>();

  [Header("Cửa & Liên kết đồ thị")]
  public List<Vector2Int> doorTiles = new List<Vector2Int>();
  public int graphDistance = 0; // Khoảng cách từ Start Room qua BFS

  public Vector2Int Center => new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);

  public Vector3 WorldCenter(float tileSize)
  {
    return new Vector3((bounds.x + bounds.width / 2f) * tileSize, 0f, (bounds.y + bounds.height / 2f) * tileSize);
  }

  /// <summary>
  /// Kiểm tra tọa độ lưới có nằm trong vùng gác lửng tầng 2 không
  /// </summary>
  public bool IsInMezzanine(int gridX, int gridY)
  {
    if (!hasMezzanine) return false;
    if (mezzanineTiles != null && mezzanineTiles.Count > 0)
    {
      return mezzanineTiles.Contains(new Vector2Int(gridX, gridY));
    }
    return gridX >= mezzanineBounds.x && gridX < mezzanineBounds.x + mezzanineBounds.width &&
           gridY >= mezzanineBounds.y && gridY < mezzanineBounds.y + mezzanineBounds.height;
  }

  /// <summary>
  /// Kiểm tra ô có nằm sát cửa ra vào không (Clearance Zone để không bị chặn đường)
  /// </summary>
  public bool IsNearDoor(Vector2Int tile, int bufferRadius = 1)
  {
    foreach (var door in doorTiles)
    {
      if (Mathf.Abs(tile.x - door.x) <= bufferRadius && Mathf.Abs(tile.y - door.y) <= bufferRadius)
      {
        return true;
      }
    }
    return false;
  }
}
