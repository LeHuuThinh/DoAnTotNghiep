using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu hình tùy chỉnh sinh tầng 2 (gác lửng Mezzanine) hiển thị trực quan trong Inspector
/// </summary>
[System.Serializable]
public class MezzanineSettings
{
  [Header("Bật/Tắt & Điều kiện")]
  [Tooltip("Bật/tắt sinh tầng 2 (gác lửng) cho dungeon")]
  public bool enableMezzanine = true;

  [Tooltip("Kích thước tối thiểu của phòng (cả chiều rộng và chiều cao tính theo tile) để đủ điều kiện sinh tầng 2")]
  public int minRoomSize = 10;

  [Header("Tỷ lệ Hình dạng Tầng 2 (Shape Probabilities)")]
  [Tooltip("Tỷ lệ sinh sàn chữ nhật (Rectangle - chiếm một nửa diện tích phòng)")]
  [Range(0f, 1f)] public float rectangleChance = 0.25f;

  [Tooltip("Tỷ lệ sinh sàn chữ L (L-Shape - ban công hành lang dài dọc theo vách)")]
  [Range(0f, 1f)] public float lShapeChance = 0.45f;

  [Tooltip("Tỷ lệ sinh sàn chữ U (U-Shape - catwalk bao quanh giếng trời thông tầng lớn)")]
  [Range(0f, 1f)] public float uShapeChance = 0.30f;

  [Header("Cột Đỡ & Cầu Thang")]
  [Tooltip("Khoảng cách tối thiểu giữa các cây cột đỡ (tính bằng số ô tile)")]
  [Range(4, 15)] public int minPillarDistance = 6;

  [Tooltip("Tỷ lệ ưu tiên chọn vị trí cầu thang đứng độc lập (freestanding) né cửa tường bên")]
  [Range(0f, 1f)] public float freestandingStairChance = 0.80f;
}

/// <summary>
/// Module phân tích đồ thị hầm ngục:
/// - Xác định cửa ra vào của từng phòng
/// - Chạy BFS từ Start Room để tính khoảng cách và chọn phòng xa nhất làm Boss Room
/// - Phân loại chức năng phòng (Start, Boss, Mezzanine, Treasure, Normal)
/// - Tính toán cấu trúc tầng 2 (Gác lửng, Cầu thang, Cột đỡ) cho các phòng đủ lớn
/// </summary>
public static class DungeonRoomAnalyzer
{
  public static void Analyze(DungeonData data, Transform startDoorMarker, float tileSize, int corridorThickness = 2, MezzanineSettings settings = null)
  {
    if (data == null || data.rooms == null || data.rooms.Count == 0) return;
    if (settings == null) settings = new MezzanineSettings();

    data.roomDataList.Clear();

    // 1. Tạo danh sách RoomData ban đầu
    for (int i = 0; i < data.rooms.Count; i++)
    {
      RoomData r = new RoomData
      {
        id = i,
        bounds = data.rooms[i],
        type = RoomType.Normal,
        ceilingHeight = 8f
      };
      data.roomDataList.Add(r);
    }

    // 2. Tìm cửa ra vào (Door Tiles) kết nối với hành lang
    FindDoorTiles(data);

    // 3. Xác định Start Room
    Vector2Int gridDoorPos = Vector2Int.zero;
    if (startDoorMarker != null)
    {
      gridDoorPos = new Vector2Int(Mathf.FloorToInt(startDoorMarker.position.x / tileSize), Mathf.FloorToInt(startDoorMarker.position.z / tileSize));
    }
    data.startRoom = FindStartRoom(data, gridDoorPos);
    if (data.startRoom != null)
    {
      data.startRoom.type = RoomType.Start;
      data.startRoom.ceilingHeight = 8f;
    }

    // 4. Xây dựng đồ thị kề giữa các phòng qua hành lang
    Dictionary<int, List<int>> adjacency = BuildRoomAdjacency(data);

    // 5. Chạy BFS tính khoảng cách và xác định Boss Room (phòng xa nhất)
    RunBFS(data, adjacency);

    // 6. Phân loại các phòng còn lại & Thiết lập Gác lửng tầng 2
    SetupRoomArchetypes(data, adjacency, tileSize, settings);
  }

  /// <summary>
  /// Tìm các ô cửa nằm trên mép phòng tiếp giáp với hành lang
  /// </summary>
  private static void FindDoorTiles(DungeonData data)
  {
    foreach (var room in data.roomDataList)
    {
      room.doorTiles.Clear();
      RectInt b = room.bounds;

      // Duyệt 4 cạnh của phòng
      for (int x = b.x; x < b.x + b.width; x++)
      {
        CheckDoorCandidate(data, room, x, b.y - 1, x, b.y);           // Cạnh Nam
        CheckDoorCandidate(data, room, x, b.y + b.height, x, b.y + b.height - 1); // Cạnh Bắc
      }

      for (int y = b.y; y < b.y + b.height; y++)
      {
        CheckDoorCandidate(data, room, b.x - 1, y, b.x, y);                   // Cạnh Tây
        CheckDoorCandidate(data, room, b.x + b.width, y, b.x + b.width - 1, y); // Cạnh Đông
      }
    }
  }

  private static void CheckDoorCandidate(DungeonData data, RoomData room, int neighborX, int neighborY, int innerX, int innerY)
  {
    if (!data.IsInBounds(neighborX, neighborY)) return;

    // Nếu ô lân cận nằm trong hành lang
    if (IsCorridorTile(data, neighborX, neighborY))
    {
      Vector2Int doorTile = new Vector2Int(innerX, innerY);
      if (!room.doorTiles.Contains(doorTile))
      {
        room.doorTiles.Add(doorTile);
      }
    }
  }

  private static bool IsCorridorTile(DungeonData data, int x, int y)
  {
    // Ô nằm trong bất kỳ căn phòng nào không được coi là ô hành lang
    if (data.IsInsideAnyRoom(x, y)) return false;

    foreach (var c in data.corridors)
    {
      if (x >= c.x && x < c.x + c.width && y >= c.y && y < c.y + c.height)
      {
        return true;
      }
    }
    return false;
  }

  private static RoomData FindStartRoom(DungeonData data, Vector2Int startPos)
  {
    RoomData closest = data.roomDataList[0];
    float minDst = float.MaxValue;

    foreach (var r in data.roomDataList)
    {
      float dst = Vector2.Distance(startPos, r.Center);
      if (dst < minDst)
      {
        minDst = dst;
        closest = r;
      }
    }
    return closest;
  }

  /// <summary>
  /// Xây dựng đồ thị kề giữa các phòng: 2 phòng được coi là kề nhau nếu có chung hành lang kết nối
  /// </summary>
  private static Dictionary<int, List<int>> BuildRoomAdjacency(DungeonData data)
  {
    Dictionary<int, List<int>> adj = new Dictionary<int, List<int>>();
    foreach (var r in data.roomDataList)
    {
      adj[r.id] = new List<int>();
    }

    // Với mỗi hành lang, xem nó chạm vào những phòng nào
    foreach (var corridor in data.corridors)
    {
      List<int> connectedRooms = new List<int>();
      foreach (var room in data.roomDataList)
      {
        if (CorridorTouchesRoom(corridor, room.bounds))
        {
          connectedRooms.Add(room.id);
        }
      }

      // Nối cạnh đồ thị giữa tất cả các phòng chạm chung hành lang này
      for (int i = 0; i < connectedRooms.Count; i++)
      {
        for (int j = i + 1; j < connectedRooms.Count; j++)
        {
          int idA = connectedRooms[i];
          int idB = connectedRooms[j];
          if (!adj[idA].Contains(idB)) adj[idA].Add(idB);
          if (!adj[idB].Contains(idA)) adj[idB].Add(idA);
        }
      }
    }

    return adj;
  }

  private static bool CorridorTouchesRoom(RectInt corridor, RectInt room)
  {
    // Mở rộng corridor thêm 1 tile để kiểm tra va chạm mép
    RectInt expanded = new RectInt(corridor.x - 1, corridor.y - 1, corridor.width + 2, corridor.height + 2);
    return expanded.Overlaps(room);
  }

  /// <summary>
  /// Thuật toán BFS: Tính khoảng cách từ Start Room, tìm phòng xa nhất làm Boss Room
  /// </summary>
  private static void RunBFS(DungeonData data, Dictionary<int, List<int>> adjacency)
  {
    if (data.startRoom == null) return;

    Queue<int> queue = new Queue<int>();
    HashSet<int> visited = new HashSet<int>();

    data.startRoom.graphDistance = 0;
    queue.Enqueue(data.startRoom.id);
    visited.Add(data.startRoom.id);

    int maxDistance = 0;
    RoomData furthestRoom = data.startRoom;

    while (queue.Count > 0)
    {
      int currentId = queue.Dequeue();
      RoomData currentRoom = data.roomDataList[currentId];

      if (adjacency.ContainsKey(currentId))
      {
        foreach (int neighborId in adjacency[currentId])
        {
          if (!visited.Contains(neighborId))
          {
            visited.Add(neighborId);
            RoomData neighborRoom = data.roomDataList[neighborId];
            neighborRoom.graphDistance = currentRoom.graphDistance + 1;

            if (neighborRoom.graphDistance > maxDistance ||
               (neighborRoom.graphDistance == maxDistance && (neighborRoom.bounds.width * neighborRoom.bounds.height) > (furthestRoom.bounds.width * furthestRoom.bounds.height)))
            {
              maxDistance = neighborRoom.graphDistance;
              furthestRoom = neighborRoom;
            }

            queue.Enqueue(neighborId);
          }
        }
      }
    }

    // Gán Boss Room
    data.bossRoom = furthestRoom;
    data.bossRoom.type = RoomType.Boss;
    data.bossRoom.ceilingHeight = 16f; // Phòng Boss trần cao 16m cực kỳ thoáng đãng, hoành tráng
  }

  /// <summary>
  /// Phân loại các phòng còn lại (Mezzanine, Treasure, Normal) và thiết kế gác lửng tầng 2
  /// </summary>
  private static void SetupRoomArchetypes(DungeonData data, Dictionary<int, List<int>> adjacency, float tileSize, MezzanineSettings settings)
  {
    foreach (var room in data.roomDataList)
    {
      if (room == data.startRoom || room == data.bossRoom) continue;

      int connections = adjacency.ContainsKey(room.id) ? adjacency[room.id].Count : 0;
      int area = room.bounds.width * room.bounds.height;

      // 1. Nhánh cụt (chỉ có 1 cửa nối) -> Phòng Kho Báu (Treasure) hoặc Tế Đàn (Shrine)
      if (connections <= 1)
      {
        room.type = (Random.value > 0.35f) ? RoomType.Treasure : RoomType.Shrine;
        room.ceilingHeight = 8f; // Chiều cao tối thiểu 8m
        continue;
      }

      // 2. Phòng diện tích lớn đạt ngưỡng cấu hình minRoomSize -> Sinh Tầng 2 (Gác lửng Mezzanine)
      if (settings.enableMezzanine && room.bounds.width >= settings.minRoomSize && room.bounds.height >= settings.minRoomSize)
      {
        room.type = RoomType.Mezzanine;
        room.ceilingHeight = 16f; // Tổng trần cao 16m (Trệt 8m + Tầng 2 8m)
        room.hasMezzanine = true;
        room.mezzanineHeight = 8f; // Sàn tầng 2 nằm ở độ cao 8m

        CalculateMezzanineLayout(room, tileSize, settings);
        continue;
      }

      // 3. Phòng chiến đấu thông thường: Chiều cao chuẩn 8m
      room.type = RoomType.Normal;
      room.ceilingHeight = 8f;
    }
  }

  /// <summary>
  /// Tính toán khoảng cách tối thiểu từ một vùng lưới tới tất cả các cửa ra vào trong phòng
  /// </summary>
  private static int GetMinDoorDistance(RoomData room, RectInt footprint)
  {
    if (room.doorTiles == null || room.doorTiles.Count == 0) return 999;
    int minDst = int.MaxValue;
    foreach (var d in room.doorTiles)
    {
      if (footprint.Contains(d)) return 0;
      int dx = Mathf.Max(footprint.x - d.x, 0, d.x - (footprint.x + footprint.width - 1));
      int dy = Mathf.Max(footprint.y - d.y, 0, d.y - (footprint.y + footprint.height - 1));
      int dst = dx + dy;
      if (dst < minDst) minDst = dst;
    }
    return minDst;
  }

  private struct StairCandidate
  {
    public int x;
    public int landingY;
    public int dist;
    public bool isFreestanding;
    public int priority;
  }

  private static bool AddCandidateIfValid(RoomData room, RectInt b, int candX, int candLandingY, List<StairCandidate> candidates, int priority = 1)
  {
    if (candX < b.x || candX + 1 >= b.x + b.width) return false;

    if (room.mezzanineSide == MezzanineWallSide.North)
    {
      // 1. Phải có sàn đón trên gác lửng
      if (!room.mezzanineTiles.Contains(new Vector2Int(candX, candLandingY)) ||
          !room.mezzanineTiles.Contains(new Vector2Int(candX + 1, candLandingY)))
        return false;

      // 2. Chân thang (landingY - 4) và vùng bước vào (landingY - 5) phải nằm gọn trong phòng
      if (candLandingY - 4 < b.y) return false;

      // 3. Footprint kiểm tra khoảng cách đến các cửa trệt (chân thang + vùng bước vào)
      int checkMinY = Mathf.Max(b.y, candLandingY - 5);
      RectInt fp = new RectInt(candX, checkMinY, 2, candLandingY - checkMinY + 1);
      int dist = GetMinDoorDistance(room, fp);
      bool isFree = (candX > b.x && candX + 1 < b.x + b.width - 1);
      candidates.Add(new StairCandidate { x = candX, landingY = candLandingY, dist = dist, isFreestanding = isFree, priority = priority });
      return true;
    }
    else
    {
      // South: Landing tile trên gác lửng là (candLandingY - 1)
      if (!room.mezzanineTiles.Contains(new Vector2Int(candX, candLandingY - 1)) ||
          !room.mezzanineTiles.Contains(new Vector2Int(candX + 1, candLandingY - 1)))
        return false;

      // Chân thang (landingY + 3) và vùng bước vào (landingY + 4) phải nằm gọn trong phòng
      if (candLandingY + 3 >= b.y + b.height) return false;

      int checkMaxY = Mathf.Min(b.y + b.height - 1, candLandingY + 4);
      RectInt fp = new RectInt(candX, candLandingY - 1, 2, checkMaxY - (candLandingY - 1) + 1);
      int dist = GetMinDoorDistance(room, fp);
      bool isFree = (candX > b.x && candX + 1 < b.x + b.width - 1);
      candidates.Add(new StairCandidate { x = candX, landingY = candLandingY, dist = dist, isFreestanding = isFree, priority = priority });
      return true;
    }
  }

  private static StairCandidate PickBestStairCandidate(List<StairCandidate> candidates, float freestandingChance)
  {
    if (candidates == null || candidates.Count == 0) return default;

    int maxDist = -1;
    foreach (var c in candidates)
    {
      if (c.dist > maxDist) maxDist = c.dist;
    }

    // Khoảng cách an toàn tối thiểu với cửa là 3 ô tile (6m)
    int threshold = (maxDist >= 3) ? 3 : maxDist;
    List<StairCandidate> safeCandidates = new List<StairCandidate>();
    foreach (var c in candidates)
    {
      if (c.dist >= threshold) safeCandidates.Add(c);
    }

    // Ưu tiên các vị trí đứng độc lập (freestanding) không dính tường bên theo tỷ lệ freestandingChance
    List<StairCandidate> freestandingSafe = new List<StairCandidate>();
    foreach (var c in safeCandidates)
    {
      if (c.isFreestanding) freestandingSafe.Add(c);
    }

    if (freestandingSafe.Count > 0 && Random.value < freestandingChance)
    {
      return freestandingSafe[Random.Range(0, freestandingSafe.Count)];
    }

    return safeCandidates[Random.Range(0, safeCandidates.Count)];
  }

  /// <summary>
  /// Tính toán vị trí mảng gác lửng, lan can, cột đỡ và cầu thang né cửa ra vào
  /// </summary>
  private static void CalculateMezzanineLayout(RoomData room, float tileSize, MezzanineSettings settings)
  {
    RectInt b = room.bounds;
    room.mezzanineTiles.Clear();
    room.stairLandingTiles.Clear();
    room.pillarPositions.Clear();

    // 1. Tìm phía tường xa cửa nhất để đặt khối gác lửng chính
    Vector2 doorCenter = Vector2.zero;
    if (room.doorTiles.Count > 0)
    {
      foreach (var d in room.doorTiles) doorCenter += new Vector2(d.x, d.y);
      doorCenter /= room.doorTiles.Count;
    }
    else
    {
      doorCenter = new Vector2(b.x + b.width / 2f, b.y);
    }

    // Mặc định đặt áp tường Bắc (nếu cửa ở Nam) hoặc tường Nam (nếu cửa ở Bắc)
    if (doorCenter.y < b.y + b.height / 2f)
    {
      room.mezzanineSide = MezzanineWallSide.North;
    }
    else
    {
      room.mezzanineSide = MezzanineWallSide.South;
    }

    // 2. Lựa chọn Hình dạng Kiến trúc tầng 2 theo tỷ lệ cấu hình trong Inspector
    float shapeRoll = Random.value;
    float totalChance = settings.rectangleChance + settings.lShapeChance + settings.uShapeChance;
    if (totalChance <= 0.001f) totalChance = 1f;
    float normRect = settings.rectangleChance / totalChance;
    float normL = settings.lShapeChance / totalChance;

    if (b.width >= settings.minRoomSize && b.height >= settings.minRoomSize)
    {
      if (shapeRoll < normRect) room.mezzanineShape = MezzanineShape.Rectangle;
      else if (shapeRoll < normRect + normL) room.mezzanineShape = MezzanineShape.LShape;
      else room.mezzanineShape = MezzanineShape.UShape;
    }
    else
    {
      room.mezzanineShape = MezzanineShape.Rectangle;
    }

    int landingY = 0;
    int stairGridX = b.x;

    // 3. Tính toán kích thước, sinh sàn tầng 2 và chọn vị trí cầu thang tối ưu (~50% diện tích phòng)
    if (room.mezzanineShape == MezzanineShape.Rectangle)
    {
      // SÀN CHỮ NHẬT: Chiếm đúng ~50% diện tích phòng (để lại 50% trần cao và sàn trệt)
      int maxSafeDepth = Mathf.Max(4, b.height - 5);
      int depth = Mathf.Clamp(Mathf.RoundToInt(b.height * 0.50f), 4, maxSafeDepth);
      landingY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - depth) : (b.y + depth);
      room.mezzanineBounds = new RectInt(b.x, (room.mezzanineSide == MezzanineWallSide.North) ? landingY : b.y, b.width, depth);

      for (int x = room.mezzanineBounds.x; x < room.mezzanineBounds.x + room.mezzanineBounds.width; x++)
        for (int y = room.mezzanineBounds.y; y < room.mezzanineBounds.y + room.mezzanineBounds.height; y++)
          room.mezzanineTiles.Add(new Vector2Int(x, y));

      List<StairCandidate> candidates = new List<StairCandidate>();
      for (int x = b.x; x <= b.x + b.width - 2; x++)
      {
        AddCandidateIfValid(room, b, x, landingY, candidates);
      }

      StairCandidate best = PickBestStairCandidate(candidates, settings.freestandingStairChance);
      stairGridX = (candidates.Count > 0) ? best.x : b.x;
      landingY = (candidates.Count > 0) ? best.landingY : landingY;
    }
    else if (room.mezzanineShape == MezzanineShape.LShape)
    {
      // SÀN CHỮ L:
      // 1. Thanh chính (Main Area) mở rộng sâu ~38% chiều cao phòng (rộng hơn vài lần so với trước)
      // 2. Nhánh vươn sâu ~70% phòng
      // => Tổng diện tích sàn chữ L chiếm ~48% - 50% diện tích phòng
      int barDepth = Mathf.Clamp(Mathf.RoundToInt(b.height * 0.38f), 4, b.height - 5);
      int maxArmLength = Mathf.Max(barDepth + 3, b.height - 5);
      int armLength = Mathf.Clamp(Mathf.RoundToInt(b.height * 0.70f), barDepth + 3, maxArmLength);
      int armWidth = Mathf.Clamp(Mathf.RoundToInt(b.width * 0.28f), 3, Mathf.Max(3, b.width / 2 - 1));

      bool chooseEast = Random.value > 0.5f;
      int armStartX = chooseEast ? (b.x + b.width - armWidth) : b.x;

      // Sinh sàn L-Shape: Thanh chính
      int barStartY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - barDepth) : b.y;
      for (int x = b.x; x < b.x + b.width; x++)
        for (int y = barStartY; y < barStartY + barDepth; y++)
          room.mezzanineTiles.Add(new Vector2Int(x, y));

      // Nhánh hành lang chữ L vươn dài
      int armMinY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - armLength) : b.y;
      int armMaxY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height) : (b.y + armLength);
      for (int x = armStartX; x < armStartX + armWidth; x++)
        for (int y = armMinY; y < armMaxY; y++)
          room.mezzanineTiles.Add(new Vector2Int(x, y));

      int armTipLandingY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - armLength) : (b.y + armLength);
      int mainFrontLandingY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - barDepth) : (b.y + barDepth);

      List<StairCandidate> candidates = new List<StairCandidate>();

      // Vị trí 1: Đầu mút nhánh chữ L (Arm Tip)
      for (int x = armStartX; x <= armStartX + armWidth - 2; x++)
      {
        AddCandidateIfValid(room, b, x, armTipLandingY, candidates, priority: 2);
      }

      // Vị trí 2 & 3: Tại chỗ gấp khúc (Bend) và dọc theo mép thanh chính (Main Area)
      if (chooseEast)
      {
        for (int x = b.x; x <= armStartX - 2; x++)
        {
          AddCandidateIfValid(room, b, x, mainFrontLandingY, candidates, priority: 1);
        }
      }
      else
      {
        for (int x = armStartX + armWidth; x <= b.x + b.width - 2; x++)
        {
          AddCandidateIfValid(room, b, x, mainFrontLandingY, candidates, priority: 1);
        }
      }

      StairCandidate best = PickBestStairCandidate(candidates, settings.freestandingStairChance);
      stairGridX = (candidates.Count > 0) ? best.x : armStartX;
      landingY = (candidates.Count > 0) ? best.landingY : armTipLandingY;
    }
    else if (room.mezzanineShape == MezzanineShape.UShape)
    {
      // SÀN CHỮ U:
      // 1. Thanh chính (Main Area) mở rộng sâu ~34% chiều cao phòng (rộng hơn vài lần so với trước)
      // 2. 2 Nhánh hai bên vươn sâu ~70% phòng bao quanh giếng trời thông tầng thoáng đãng
      // => Tổng diện tích sàn chữ U chiếm ~48% - 50% diện tích phòng
      int barDepth = Mathf.Clamp(Mathf.RoundToInt(b.height * 0.34f), 4, b.height - 5);
      int maxArmLength = Mathf.Max(barDepth + 3, b.height - 5);
      int armLength = Mathf.Clamp(Mathf.RoundToInt(b.height * 0.70f), barDepth + 3, maxArmLength);
      int armWidth = Mathf.Clamp(Mathf.RoundToInt(b.width * 0.22f), 3, Mathf.Max(3, (b.width - 4) / 2));

      // Sinh sàn U-Shape: Thanh chính
      int barStartY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - barDepth) : b.y;
      for (int x = b.x; x < b.x + b.width; x++)
        for (int y = barStartY; y < barStartY + barDepth; y++)
          room.mezzanineTiles.Add(new Vector2Int(x, y));

      // 2 Nhánh hai bên (West arm & East arm)
      int armMinY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - armLength) : b.y;
      int armMaxY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height) : (b.y + armLength);
      for (int y = armMinY; y < armMaxY; y++)
      {
        for (int x = b.x; x < b.x + armWidth; x++) room.mezzanineTiles.Add(new Vector2Int(x, y));
        for (int x = b.x + b.width - armWidth; x < b.x + b.width; x++) room.mezzanineTiles.Add(new Vector2Int(x, y));
      }

      int armTipLandingY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - armLength) : (b.y + armLength);
      int mainFrontLandingY = (room.mezzanineSide == MezzanineWallSide.North) ? (b.y + b.height - barDepth) : (b.y + barDepth);

      List<StairCandidate> candidates = new List<StairCandidate>();

      // Vị trí 1: Đầu mút nhánh Tây
      for (int x = b.x; x <= b.x + armWidth - 2; x++)
      {
        AddCandidateIfValid(room, b, x, armTipLandingY, candidates, priority: 2);
      }

      // Vị trí 2: Đầu mút nhánh Đông
      for (int x = b.x + b.width - armWidth; x <= b.x + b.width - 2; x++)
      {
        AddCandidateIfValid(room, b, x, armTipLandingY, candidates, priority: 2);
      }

      // Vị trí 3: Dọc mép thanh chính giếng trời & chỗ gấp khúc (giữa nhánh Tây và nhánh Đông)
      for (int x = b.x + armWidth; x <= b.x + b.width - armWidth - 2; x++)
      {
        AddCandidateIfValid(room, b, x, mainFrontLandingY, candidates, priority: 1);
      }

      StairCandidate best = PickBestStairCandidate(candidates, settings.freestandingStairChance);
      stairGridX = (candidates.Count > 0) ? best.x : b.x;
      landingY = (candidates.Count > 0) ? best.landingY : armTipLandingY;
    }

    // Cập nhật bounding box bao phủ toàn bộ các ô sàn gác lửng đã sinh
    int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
    foreach (var tile in room.mezzanineTiles)
    {
      if (tile.x < minX) minX = tile.x;
      if (tile.x > maxX) maxX = tile.x;
      if (tile.y < minY) minY = tile.y;
      if (tile.y > maxY) maxY = tile.y;
    }
    if (minX <= maxX && minY <= maxY)
    {
      room.mezzanineBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // 4. TUYỆT ĐỐI ĐẢM BẢO KHÔNG CÓ Ô SÀN TẦNG 2 NÀO ĐÈ LÊN KHOẢNG KHÔNG CẦU THANG (4 Ô THANG)
    int sClearMinY = (room.mezzanineSide == MezzanineWallSide.North) ? (landingY - 4) : landingY;
    int sClearMaxY = (room.mezzanineSide == MezzanineWallSide.North) ? (landingY - 1) : (landingY + 3);
    for (int x = stairGridX; x < stairGridX + 2; x++)
    {
      for (int y = sClearMinY; y <= sClearMaxY; y++)
      {
        room.mezzanineTiles.Remove(new Vector2Int(x, y));
      }
    }

    // Đánh dấu 2 ô đón cầu thang trên gác lửng (LUÔN CÓ SÀN ĐỂ BƯỚC LÊN & TỰ ĐỘNG BỎ LAN CAN LỐI VÀO)
    int landingCheckY = (room.mezzanineSide == MezzanineWallSide.North) ? landingY : (landingY - 1);
    room.mezzanineTiles.Add(new Vector2Int(stairGridX, landingCheckY));
    room.mezzanineTiles.Add(new Vector2Int(stairGridX + 1, landingCheckY));
    room.stairLandingTiles.Add(new Vector2Int(stairGridX, landingCheckY));
    room.stairLandingTiles.Add(new Vector2Int(stairGridX + 1, landingCheckY));

    // 5. Thiết lập Cầu thang 2 nhịp 4m chuẩn tỉ lệ người (Total 8m)
    float halfTile = tileSize / 2f;
    float stairCenterX = (stairGridX + 1) * tileSize;
    room.stairGridPos = new Vector2Int(stairGridX, landingY);

    if (room.mezzanineSide == MezzanineWallSide.North)
    {
      float landingZ = landingY * tileSize;
      room.stairsRotation = Quaternion.Euler(0, 180, 0); // Hướng leo từ Nam lên Bắc
      // Nhịp trên (4m -> 8m)
      room.stairsUpperPosition = new Vector3(stairCenterX, 4f, landingZ);
      // Nhịp dưới (0m -> 4m)
      room.stairsLowerPosition = new Vector3(stairCenterX, 0f, landingZ - 4f);
      room.stairsPosition = new Vector3(stairCenterX, 0f, landingZ);
    }
    else
    {
      float landingZ = landingY * tileSize;
      room.stairsRotation = Quaternion.Euler(0, 0, 0); // Hướng leo từ Bắc xuống Nam
      // Nhịp trên (4m -> 8m)
      room.stairsUpperPosition = new Vector3(stairCenterX, 4f, landingZ);
      // Nhịp dưới (0m -> 4m)
      room.stairsLowerPosition = new Vector3(stairCenterX, 0f, landingZ + 4f);
      room.stairsPosition = new Vector3(stairCenterX, 0f, landingZ);
    }

    // 6. Tính toán vị trí các cột chống đỡ (Pillars) thụt vào trong sàn với khoảng cách tối thiểu nghiêm ngặt
    HashSet<Vector2Int> openEdgeTiles = new HashSet<Vector2Int>();
    foreach (var tile in room.mezzanineTiles)
    {
      bool openS = (tile.y > b.y) && !room.mezzanineTiles.Contains(new Vector2Int(tile.x, tile.y - 1));
      bool openN = (tile.y < b.y + b.height - 1) && !room.mezzanineTiles.Contains(new Vector2Int(tile.x, tile.y + 1));
      bool openW = (tile.x > b.x) && !room.mezzanineTiles.Contains(new Vector2Int(tile.x - 1, tile.y));
      bool openE = (tile.x < b.x + b.width - 1) && !room.mezzanineTiles.Contains(new Vector2Int(tile.x + 1, tile.y));

      if (openS || openN || openW || openE)
      {
        openEdgeTiles.Add(tile);
      }
    }

    float minPillarDist = settings.minPillarDistance * tileSize;
    float minPillarDistSqr = minPillarDist * minPillarDist;

    int stairMinY = (room.mezzanineSide == MezzanineWallSide.North) ? (landingY - 4) : (landingY - 1);
    int stairMaxY = (room.mezzanineSide == MezzanineWallSide.North) ? landingY : (landingY + 3);

    foreach (var tile in openEdgeTiles)
    {
      // Cách xa cầu thang ít nhất 2 ô cả theo trục X lẫn trục Y
      bool nearStairs = (tile.x >= stairGridX - 2 && tile.x <= stairGridX + 3 && tile.y >= stairMinY - 2 && tile.y <= stairMaxY + 2);
      if (nearStairs) continue;

      Vector3 pos = new Vector3(tile.x * tileSize + halfTile, 0f, tile.y * tileSize + halfTile);

      bool tooClose = false;
      foreach (var placed in room.pillarPositions)
      {
        if ((placed - pos).sqrMagnitude < minPillarDistSqr)
        {
          tooClose = true;
          break;
        }
      }

      if (!tooClose)
      {
        room.pillarPositions.Add(pos);
      }
    }

    if (room.pillarPositions.Count == 0)
    {
      int safePillarX = (stairGridX == b.x) ? (b.x + b.width - 3) : (b.x + 2);
      room.pillarPositions.Add(new Vector3(safePillarX * tileSize + halfTile, 0f, landingY * tileSize + (room.mezzanineSide == MezzanineWallSide.North ? halfTile : -halfTile)));
    }
  }
}
