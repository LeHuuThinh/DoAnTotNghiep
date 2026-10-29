using UnityEngine;

public class BSPGenerator : MonoBehaviour
{
  [Header("Cài đặt Lưới Map")]
  public int mapWidth = 50;
  public int mapHeight = 50;
  public int startRoomHeight = 15;

  [Header("Cài đặt Cắt BSP")]
  public int minNodeSize = 11;
  public float cutChance = 0.95f;

  [Header("Cài đặt Phòng Thường")]
  public int minRoomSize = 6;
  public int maxNormalRoomSize = 16;
  public int roomPadding = 1;

  [Header("Cài đặt Hành lang")]
  public int corridorThickness = 2;

  [Header("Cài đặt Phòng Chức Năng (Treasure / Shrine)")]
  [Tooltip("Tỷ lệ các phòng ngách cụt (Dead-Ends) trở thành phòng kho báu (Mặc định: 0.75 = 75%)")]
  [Range(0.2f, 1.0f)] public float deadEndTreasureChance = 0.75f;

  [Tooltip("Số lượng phòng kho báu (Treasure Room) tối thiểu cần có nếu có đủ ngách cụt (Mặc định: 3)")]
  [Range(1, 6)] public int minTreasureRooms = 3;

  [Header("Cài đặt Đấu Trường Boss (Boss Arena)")]
  [Tooltip("Kích thước tối thiểu cho Node chứa phòng Boss (tile) để ngăn cắt nhỏ")]
  public int bossNodeMinSize = 48;

  [Tooltip("Kích thước tối thiểu cho phòng Boss (tile), gấp 3 - 5 lần diện tích phòng thường")]
  public int bossRoomMinSize = 26;

  [Tooltip("Kích thước tối đa cho phòng Boss (tile)")]
  public int bossRoomMaxSize = 30;

  [Tooltip("Độ dày hành lang dẫn vào phòng Boss (tile), mặc định gấp đôi hành lang thường")]
  public int bossCorridorThickness = 6;

  [Tooltip("Chiều dài tối thiểu của đại lộ hành lang dẫn vào phòng Boss (tile), mặc định 20 tiles = 40m")]
  public int minBossCorridorLength = 20;

  [Header("Cài đặt Cửa Khởi Đầu")]
  public Transform startDoorMarker;
  public float tileSize = 2f;

  [Header("Cài đặt Tầng 2 (Gác Lửng Mezzanine)")]
  public MezzanineSettings mezzanineSettings = new MezzanineSettings();

  public DungeonData Generate()
  {
    DungeonData data = new DungeonData
    {
      mapWidth = this.mapWidth,
      mapHeight = this.mapHeight,
      startRoomHeight = this.startRoomHeight,
      mapGrid = new int[mapWidth, mapHeight + startRoomHeight],
      rootNode = new BSPNode(new RectInt(0, startRoomHeight, mapWidth, mapHeight))
    };

    // 1. TÍNH TOÁN VÙNG ĐẤU TRƯỜNG BOSS & ĐẠI LỘ UY NGHIÊM (NEO TẠI ĐỈNH BẮC BẢN ĐỒ)
    int bRoomW = Mathf.Clamp(bossRoomMaxSize, 26, 30);
    int bRoomH = Mathf.Clamp(bossRoomMaxSize, 26, 30);
    // Canh giữa theo trục X, neo sát biên Bắc (cách mép 2 ô để tạo tường bao quanh)
    int bRoomX = (mapWidth - bRoomW) / 2;
    int bRoomY = startRoomHeight + mapHeight - bRoomH - 2;

    int aveLen = Mathf.Clamp(minBossCorridorLength, 16, 22);
    int aveThick = Mathf.Max(corridorThickness * 2, bossCorridorThickness);
    int aveX = bRoomX + (bRoomW - aveThick) / 2;
    int aveY = bRoomY - aveLen;

    // 2. KHỞI TẠO CHÍNH XÁC PHÒNG BOSS KHỔNG LỒ & ĐẠI LỘ BOSS
    RectInt bossRoomRect = new RectInt(bRoomX, bRoomY, bRoomW, bRoomH);
    data.bossRoomBounds = bossRoomRect;
    data.rooms.Add(bossRoomRect);

    // Cắm ngập 2 ô (4m) vào lòng phòng Boss để triệt tiêu hoàn toàn khe hở viền trên Minimap
    int avenueBossOverlap = 2;
    RectInt bossAvenue = new RectInt(aveX, aveY, aveThick, aveLen + avenueBossOverlap);
    data.bossCorridor = bossAvenue;
    data.corridors.Add(bossAvenue);

    Debug.Log($"<color=cyan>[BSPGenerator]</color> 🏛️ Đã kiến tạo Đấu Trường Boss: X={bRoomX}, Y={bRoomY}, {bRoomW}x{bRoomH} tiles | Đại Lộ: X={aveX}, Y={aveY}, Rộng {aveThick * tileSize}m, Dài {aveLen * tileSize}m");

    // 3. PHÂN CHIA BSP KHU VỰC HẦM NGỤC CHÍNH (MAIN DUNGEON)
    // Toàn bộ hầm ngục thông thường sinh trong vùng Y từ startRoomHeight đến aveY
    // Đảm bảo 100% không một phòng thường nào có thể chạm hoặc đè lên phòng Boss hay Đại Lộ!
    int mainDungeonHeight = aveY - startRoomHeight;
    BSPNode mainDungeonRoot = new BSPNode(new RectInt(0, startRoomHeight, mapWidth, mainDungeonHeight));

    BSPNode bossZoneNode = new BSPNode(new RectInt(0, aveY, mapWidth, (startRoomHeight + mapHeight) - aveY));
    bossZoneNode.isBossNode = true;
    bossZoneNode.roomBounds = bossRoomRect;

    data.rootNode.leftChild = mainDungeonRoot;
    data.rootNode.rightChild = bossZoneNode;

    // Phân chia và sinh các phòng thường trong hầm ngục chính
    SplitMainDungeon(mainDungeonRoot);
    CreateRooms(mainDungeonRoot, data);
    CreateCorridors(mainDungeonRoot, data);

    // 4. KẾT NỐI ĐẠI LỘ BOSS VỚI PHÒNG THƯỜNG TRONG HẦM NGỤC CHÍNH
    ConnectGrandBossAvenue(data, aveX, aveY, aveThick, aveLen);

    // 5. KẾT NỐI CỬA KHỞI ĐẦU VÀ NƯỚNG LƯỚI
    ConnectStartRoom(data);
    BakeGrid(data);

    // 6. PHÂN TÍCH ĐỒ THỊ PHÒNG, GẮN GÁC LỬNG & THIẾT LẬP CỘT ĐẤU TRƯỜNG
    DungeonRoomAnalyzer.Analyze(data, startDoorMarker, tileSize, corridorThickness, mezzanineSettings, minTreasureRooms, deadEndTreasureChance);

    return data;
  }

  bool SplitMainDungeon(BSPNode node)
  {
    if (node.leftChild != null || node.rightChild != null) return false;

    bool splitHorizontally = Random.value > 0.5f;
    if (node.bounds.width > node.bounds.height && node.bounds.width / (float)node.bounds.height >= 1.25f) splitHorizontally = false;
    else if (node.bounds.height > node.bounds.width && node.bounds.height / (float)node.bounds.width >= 1.25f) splitHorizontally = true;

    int max = (splitHorizontally ? node.bounds.height : node.bounds.width) - minNodeSize;
    if (max <= minNodeSize) return false;

    if (Random.value > cutChance) return false;

    int split = Random.Range(minNodeSize, max);
    if (splitHorizontally)
    {
      node.leftChild = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, node.bounds.width, split));
      node.rightChild = new BSPNode(new RectInt(node.bounds.x, node.bounds.y + split, node.bounds.width, node.bounds.height - split));
    }
    else
    {
      node.leftChild = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, split, node.bounds.height));
      node.rightChild = new BSPNode(new RectInt(node.bounds.x + split, node.bounds.y, node.bounds.width - split, node.bounds.height));
    }

    SplitMainDungeon(node.leftChild);
    SplitMainDungeon(node.rightChild);
    return true;
  }

  void CreateRooms(BSPNode node, DungeonData data)
  {
    if (node == null) return;
    if (node.IsLeaf())
    {
      if (node.isBossNode) return; // Phòng Boss đã được khởi tạo trực tiếp

      int maxW = Mathf.Min(node.bounds.width - (roomPadding * 2), maxNormalRoomSize);
      int maxH = Mathf.Min(node.bounds.height - (roomPadding * 2), maxNormalRoomSize);
      int minW = Mathf.Min(minRoomSize, maxW);
      int minH = Mathf.Min(minRoomSize, maxH);

      int roomWidth = Random.Range(minW, maxW + 1);
      int roomHeight = Random.Range(minH, maxH + 1);
      int roomX = node.bounds.x + Random.Range(roomPadding, Mathf.Max(roomPadding + 1, node.bounds.width - roomWidth - roomPadding + 1));
      int roomY = node.bounds.y + Random.Range(roomPadding, Mathf.Max(roomPadding + 1, node.bounds.height - roomHeight - roomPadding + 1));

      node.roomBounds = new RectInt(roomX, roomY, roomWidth, roomHeight);
      data.rooms.Add(node.roomBounds);
    }
    else
    {
      CreateRooms(node.leftChild, data);
      CreateRooms(node.rightChild, data);
    }
  }

  // ──────────────────────────────────────────────────────────────────────────────
  // Hệ thống nối hành lang thông minh: kết nối phòng GẦN BIÊN NHẤT của 2 node anh em
  // thay vì nối tâm node đệ quy luôn trả về góc xa nhất → tránh hành lang ứ lộn xộn
  // ──────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Thu thập toàn bộ phòng lá (leaf rooms) trong cây con của node này
  /// </summary>
  void CollectLeafRooms(BSPNode node, System.Collections.Generic.List<RectInt> result)
  {
    if (node == null) return;
    if (node.IsLeaf())
    {
      if (node.roomBounds.width > 0) result.Add(node.roomBounds);
      return;
    }
    CollectLeafRooms(node.leftChild, result);
    CollectLeafRooms(node.rightChild, result);
  }

  /// <summary>
  /// Tìm phòng gần đường biên cắt BSP nhất trong node này.
  /// splitIsVertical = true  → biên là đường dọc X = splitVal → tìm phòng gần nhất theo trục X
  /// splitIsVertical = false → biên là đường ngang Y = splitVal → tìm phòng gần nhất theo trục Y
  /// fromRight = true → node nằm bên phải/trên biên, lấy mép phòng gần biên nhất từ phía đó
  /// </summary>
  RectInt FindClosestRoomToSplit(BSPNode node, bool splitIsVertical, int splitVal, bool fromRight)
  {
    var rooms = new System.Collections.Generic.List<RectInt>();
    CollectLeafRooms(node, rooms);
    if (rooms.Count == 0) return default;

    RectInt best = rooms[0];
    int bestDist = int.MaxValue;

    foreach (var r in rooms)
    {
      // Khoảng cách từ mép phòng gần biên đến đường biên cắt
      int edgePos = splitIsVertical
        ? (fromRight ? r.x : r.x + r.width - 1)
        : (fromRight ? r.y : r.y + r.height - 1);
      int dist = Mathf.Abs(edgePos - splitVal);
      if (dist < bestDist) { bestDist = dist; best = r; }
    }
    return best;
  }

  /// <summary>
  /// Tính điểm kết nối trên mép phòng (gần biên cắt BSP nhất) với jitter nhỏ để tạo
  /// sự tự nhiên. jitter ∈ [-jitterRange, +jitterRange] theo trục vuông góc với hành lang.
  /// </summary>
  Vector2Int GetConnectionPoint(RectInt room, bool splitIsVertical, bool fromRight, int jitterRange = 0)
  {
    if (splitIsVertical)
    {
      // Hành lang nằm ngang → lấy tọa độ X mép phòng, Y ngẫu nhiên trong thân phòng
      int edgeX = fromRight ? room.x : room.x + room.width - 1;
      int centerY = room.y + room.height / 2;
      int jitter = (jitterRange > 0) ? Random.Range(-jitterRange, jitterRange + 1) : 0;
      int clampedY = Mathf.Clamp(centerY + jitter, room.y + 1, room.y + room.height - 2);
      return new Vector2Int(edgeX, clampedY);
    }
    else
    {
      // Hành lang nằm dọc → lấy tọa độ Y mép phòng, X ngẫu nhiên trong thân phòng
      int edgeY = fromRight ? room.y : room.y + room.height - 1;
      int centerX = room.x + room.width / 2;
      int jitter = (jitterRange > 0) ? Random.Range(-jitterRange, jitterRange + 1) : 0;
      int clampedX = Mathf.Clamp(centerX + jitter, room.x + 1, room.x + room.width - 2);
      return new Vector2Int(clampedX, edgeY);
    }
  }

  void CreateCorridorSegment(int x1, int y1, int x2, int y2, DungeonData data)
  {
    CreateCorridorSegment(x1, y1, x2, y2, data, corridorThickness);
  }

  void CreateCorridorSegment(int x1, int y1, int x2, int y2, DungeonData data, int thickness)
  {
    int minX = Mathf.Min(x1, x2);
    int maxX = Mathf.Max(x1, x2);
    int minY = Mathf.Min(y1, y2);
    int maxY = Mathf.Max(y1, y2);

    int startX, startY, width, height;

    if (x1 == x2) // Đoạn hành lang dọc
    {
      startX = x1 - thickness / 2;
      startY = minY - thickness / 2;
      width = thickness;
      height = (maxY - minY) + thickness;
    }
    else // Đoạn hành lang ngang
    {
      startX = minX - thickness / 2;
      startY = y1 - thickness / 2;
      width = (maxX - minX) + thickness;
      height = thickness;
    }

    data.corridors.Add(new RectInt(startX, startY, width, height));
  }

  void CreateCorridors(BSPNode node, DungeonData data)
  {
    if (node == null || node.IsLeaf()) return;
    CreateCorridors(node.leftChild, data);
    CreateCorridors(node.rightChild, data);

    // Xác định đường biên cắt BSP giữa 2 node anh em
    // → cắt dọc (vertical split): leftChild.bounds.xMax == rightChild.bounds.x
    // → cắt ngang (horizontal split): leftChild.bounds.yMax == rightChild.bounds.y
    bool splitIsVertical = (node.leftChild.bounds.xMax == node.rightChild.bounds.x);
    bool splitIsHorizontal = (node.leftChild.bounds.yMax == node.rightChild.bounds.y);

    // Safety: nếu không xác định được kiểu cắt, bỏ qua node này (tránh lỗi runtime)
    if (!splitIsVertical && !splitIsHorizontal) return;

    int splitVal = splitIsVertical ? node.leftChild.bounds.xMax : node.leftChild.bounds.yMax;

    // Tìm phòng GẦN BIÊN NHẤT trong mỗi nhánh (không đệ quy về góc xa nhất nữa)
    // fromRight=false: node bên trái/dưới → muốn mép phòng GẦN BIÊN từ phía phải/trên
    // fromRight=true: node bên phải/trên → muốn mép phòng GẦN BIÊN từ phía trái/dưới
    RectInt leftRoom  = FindClosestRoomToSplit(node.leftChild,  splitIsVertical, splitVal, fromRight: false);
    RectInt rightRoom = FindClosestRoomToSplit(node.rightChild, splitIsVertical, splitVal, fromRight: true);

    if (leftRoom.width == 0 || rightRoom.width == 0) return;

    if (splitIsVertical)
    {
      // Vách cắt dọc X = splitVal: leftRoom ở bên trái, rightRoom ở bên phải (hành lang chính nằm ngang)
      // 1. Kiểm tra vùng giao nhau theo trục Y:
      int overlapMinY = Mathf.Max(leftRoom.y + 1, rightRoom.y + 1);
      int overlapMaxY = Mathf.Min(leftRoom.y + leftRoom.height - 2, rightRoom.y + rightRoom.height - 2);

      if (overlapMinY <= overlapMaxY)
      {
        // Có vùng giao thoa theo trục Y -> Ép thẳng hàng 100%!
        // Tuyệt đối không sinh đoạn gãy 1 ô gây chồng đè khối nối L
        int commonY = (overlapMinY + overlapMaxY) / 2;
        int edgeL = leftRoom.x + leftRoom.width - 1;
        int edgeR = rightRoom.x;

        CreateCorridorSegment(edgeL, commonY, edgeR, commonY, data);
      }
      else
      {
        // Hai phòng không giao nhau Y -> Bắt buộc phải rẽ chữ L (Z-shape qua splitVal)
        // Ép khoảng cách giữa 2 điểm uốn tối thiểu >= corridorThickness + 2 để 2 khối nối L tách biệt hoàn toàn
        int minSep = corridorThickness + 2;
        int ptLy = Mathf.Clamp(leftRoom.y + leftRoom.height / 2, leftRoom.y + 1, leftRoom.y + leftRoom.height - 2);
        int ptRy = Mathf.Clamp(rightRoom.y + rightRoom.height / 2, rightRoom.y + 1, rightRoom.y + rightRoom.height - 2);

        if (Mathf.Abs(ptLy - ptRy) < minSep)
        {
          if (ptLy < ptRy)
          {
            ptLy = leftRoom.y + 1;
            ptRy = rightRoom.y + rightRoom.height - 2;
          }
          else
          {
            ptLy = leftRoom.y + leftRoom.height - 2;
            ptRy = rightRoom.y + 1;
          }
        }

        int edgeL = leftRoom.x + leftRoom.width - 1;
        int edgeR = rightRoom.x;

        CreateCorridorSegment(edgeL, ptLy, splitVal, ptLy, data);
        CreateCorridorSegment(splitVal, ptLy, splitVal, ptRy, data);
        CreateCorridorSegment(splitVal, ptRy, edgeR, ptRy, data);
      }
    }
    else
    {
      // Vách cắt ngang Y = splitVal: leftRoom ở bên dưới, rightRoom ở bên trên (hành lang chính nằm dọc)
      // 1. Kiểm tra vùng giao nhau theo trục X:
      int overlapMinX = Mathf.Max(leftRoom.x + 1, rightRoom.x + 1);
      int overlapMaxX = Mathf.Min(leftRoom.x + leftRoom.width - 2, rightRoom.x + rightRoom.width - 2);

      if (overlapMinX <= overlapMaxX)
      {
        // Có vùng giao thoa theo trục X -> Ép thẳng cột 100%!
        // Tuyệt đối không sinh đoạn gãy 1 ô gây chồng đè khối nối L
        int commonX = (overlapMinX + overlapMaxX) / 2;
        int edgeBottom = leftRoom.y + leftRoom.height - 1;
        int edgeTop = rightRoom.y;

        CreateCorridorSegment(commonX, edgeBottom, commonX, edgeTop, data);
      }
      else
      {
        // Hai phòng không giao nhau X -> Bắt buộc phải rẽ chữ L (Z-shape qua splitVal)
        // Ép khoảng cách giữa 2 điểm uốn tối thiểu >= corridorThickness + 2 để 2 khối nối L tách biệt hoàn toàn
        int minSep = corridorThickness + 2;
        int ptLx = Mathf.Clamp(leftRoom.x + leftRoom.width / 2, leftRoom.x + 1, leftRoom.x + leftRoom.width - 2);
        int ptRx = Mathf.Clamp(rightRoom.x + rightRoom.width / 2, rightRoom.x + 1, rightRoom.x + rightRoom.width - 2);

        if (Mathf.Abs(ptLx - ptRx) < minSep)
        {
          if (ptLx < ptRx)
          {
            ptLx = leftRoom.x + 1;
            ptRx = rightRoom.x + rightRoom.width - 2;
          }
          else
          {
            ptLx = leftRoom.x + leftRoom.width - 2;
            ptRx = rightRoom.x + 1;
          }
        }

        int edgeBottom = leftRoom.y + leftRoom.height - 1;
        int edgeTop = rightRoom.y;

        CreateCorridorSegment(ptLx, edgeBottom, ptLx, splitVal, data);
        CreateCorridorSegment(ptLx, splitVal, ptRx, splitVal, data);
        CreateCorridorSegment(ptRx, splitVal, ptRx, edgeTop, data);
      }
    }
  }

  void ConnectStartRoom(DungeonData data)
  {
    if (data.rooms.Count == 0 || startDoorMarker == null) return;
    Vector2Int gridDoorPos = new Vector2Int(Mathf.FloorToInt(startDoorMarker.position.x / tileSize), Mathf.FloorToInt(startDoorMarker.position.z / tileSize));
    RectInt closestRoom = data.rooms[0];
    float minDistance = float.MaxValue;

    foreach (RectInt room in data.rooms)
    {
      Vector2Int roomCenter = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
      float distance = Vector2.Distance(gridDoorPos, roomCenter);
      if (distance < minDistance)
      {
        minDistance = distance;
        closestRoom = room;
      }
    }

    // Điểm cắm vào mép dưới phòng gần nhất (Y = closestRoom.y), TUYỆT ĐỐI không đi sâu vào lòng phòng!
    int halfThick = corridorThickness / 2;
    int targetX = Mathf.Clamp(gridDoorPos.x, closestRoom.x + halfThick + 1, closestRoom.x + closestRoom.width - halfThick - 1);
    int targetY = closestRoom.y;

    if (gridDoorPos.x == targetX)
    {
      CreateCorridorSegment(gridDoorPos.x, gridDoorPos.y, targetX, targetY, data);
    }
    else
    {
      // Nếu lệch X, rẽ chữ L bên ngoài phòng trước khi cắm vào mép dưới
      int midY = (gridDoorPos.y + targetY) / 2;
      CreateCorridorSegment(gridDoorPos.x, gridDoorPos.y, gridDoorPos.x, midY, data);
      CreateCorridorSegment(gridDoorPos.x, midY, targetX, midY, data);
      CreateCorridorSegment(targetX, midY, targetX, targetY, data);
    }
  }

  void ConnectGrandBossAvenue(DungeonData data, int aveX, int aveY, int aveThick, int aveLen)
  {
    int mouthX = aveX + aveThick / 2;
    Vector2Int mouthCenter = new Vector2Int(mouthX, aveY);

    RectInt bestRoom = default;
    float bestScore = float.MaxValue;

    foreach (var r in data.rooms)
    {
      if (r.Equals(data.bossRoomBounds)) continue;
      Vector2 rCenter = new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);

      // Khoảng cách từ miệng đại lộ tới tâm phòng
      float dist = Vector2.Distance(mouthCenter, rCenter);
      // Ưu tiên các phòng nằm sát đường biên aveY (đỉnh của phòng gần aveY)
      float yGap = Mathf.Max(0, aveY - (r.y + r.height));
      float score = dist + yGap * 2.0f;

      if (score < bestScore)
      {
        bestScore = score;
        bestRoom = r;
      }
    }

    if (bestRoom.width > 0)
    {
      data.bossPrevRoomBounds = bestRoom;
      Vector2Int bestCenter = new Vector2Int(bestRoom.x + bestRoom.width / 2, bestRoom.y + bestRoom.height / 2);

      // Kết nối trực tiếp từ phòng kề bestRoom vào thẳng tâm miệng Đại Lộ Boss (mouthX, aveY)
      // Nếu phòng kề bao trùm cột mouthX -> cắm thẳng 1 đoạn dọc duy nhất, ngập sâu 2 ô vào Đại Lộ (aveY + 2)
      if (mouthX >= bestRoom.x + 1 && mouthX <= bestRoom.x + bestRoom.width - 2)
      {
        int startY = bestRoom.y + bestRoom.height - 1;
        CreateCorridorSegment(mouthX, startY, mouthX, aveY + 2, data, corridorThickness);
      }
      else
      {
        // Nếu lệch cột X: Rẽ 3 đoạn vuông góc chuẩn chỉnh từ mép trên phòng lên miệng Đại Lộ
        int bestRoomTop = bestRoom.y + bestRoom.height - 1;
        int turnY = (bestRoomTop + aveY) / 2;
        turnY = Mathf.Clamp(turnY, bestRoomTop + 1, aveY - 2);

        int startX = Mathf.Clamp(bestCenter.x, bestRoom.x + 1, bestRoom.x + bestRoom.width - 2);

        CreateCorridorSegment(startX, bestRoomTop, startX, turnY, data, corridorThickness);
        CreateCorridorSegment(startX, turnY, mouthX, turnY, data, corridorThickness);
        CreateCorridorSegment(mouthX, turnY, mouthX, aveY + 2, data, corridorThickness);
      }

      Debug.Log($"<color=cyan>[BSPGenerator]</color> 🏛️ Đã kết nối Đại Lộ Boss (Dài {aveLen * tileSize}m) vào phòng kề: X={bestRoom.x}, Y={bestRoom.y}");
    }
  }

  void BakeGrid(DungeonData data)
  {
    foreach (RectInt room in data.rooms)
    {
      for (int x = room.x; x < room.x + room.width; x++)
        for (int y = room.y; y < room.y + room.height; y++)
          if (data.IsInBounds(x, y)) data.mapGrid[x, y] = 1;
    }
    foreach (RectInt corridor in data.corridors)
    {
      for (int x = corridor.x; x < corridor.x + corridor.width; x++)
        for (int y = corridor.y; y < corridor.y + corridor.height; y++)
          if (data.IsInBounds(x, y)) data.mapGrid[x, y] = 1;
    }

    if (startDoorMarker != null)
    {
      int doorX = Mathf.FloorToInt(startDoorMarker.position.x / tileSize);
      int doorZ = Mathf.FloorToInt(startDoorMarker.position.z / tileSize);
      int startX = doorX - (corridorThickness / 2);
      int endX = startX + corridorThickness - 1;

      for (int x = startX; x <= endX; x++)
        for (int y = doorZ - 5; y <= doorZ; y++)
          if (data.IsInBounds(x, y)) data.mapGrid[x, y] = 2; // Khóa ngàm nối
    }
  }
}
