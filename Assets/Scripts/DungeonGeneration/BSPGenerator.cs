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

    RectInt bossAvenue = new RectInt(aveX, aveY, aveThick, aveLen);
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
    DungeonRoomAnalyzer.Analyze(data, startDoorMarker, tileSize, corridorThickness, mezzanineSettings);

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

  Vector2Int GetNodeCenter(BSPNode node)
  {
    if (node.IsLeaf()) return new Vector2Int(node.roomBounds.x + node.roomBounds.width / 2, node.roomBounds.y + node.roomBounds.height / 2);
    if (node.leftChild != null) return GetNodeCenter(node.leftChild);
    if (node.rightChild != null) return GetNodeCenter(node.rightChild);
    return Vector2Int.zero;
  }

  void CreateCorridorSegment(int x1, int y1, int x2, int y2, DungeonData data)
  {
    CreateCorridorSegment(x1, y1, x2, y2, data, corridorThickness);
  }

  void CreateCorridorSegment(int x1, int y1, int x2, int y2, DungeonData data, int thickness)
  {
    int startX = Mathf.Min(x1, x2);
    int startY = Mathf.Min(y1, y2);
    int width = Mathf.Abs(x1 - x2) + thickness;
    int height = Mathf.Abs(y1 - y2) + thickness;

    if (x1 == x2) startX -= thickness / 2;
    else if (y1 == y2) startY -= thickness / 2;

    data.corridors.Add(new RectInt(startX, startY, width, height));
  }

  void CreateCorridors(BSPNode node, DungeonData data)
  {
    if (node == null || node.IsLeaf()) return;
    CreateCorridors(node.leftChild, data);
    CreateCorridors(node.rightChild, data);

    Vector2Int leftCenter = GetNodeCenter(node.leftChild);
    Vector2Int rightCenter = GetNodeCenter(node.rightChild);

    if (Random.value > 0.5f)
    {
      CreateCorridorSegment(leftCenter.x, leftCenter.y, rightCenter.x, leftCenter.y, data);
      CreateCorridorSegment(rightCenter.x, leftCenter.y, rightCenter.x, rightCenter.y, data);
    }
    else
    {
      CreateCorridorSegment(leftCenter.x, leftCenter.y, leftCenter.x, rightCenter.y, data);
      CreateCorridorSegment(leftCenter.x, rightCenter.y, rightCenter.x, rightCenter.y, data);
    }
  }

  void ConnectStartRoom(DungeonData data)
  {
    if (data.rooms.Count == 0 || startDoorMarker == null) return;
    Vector2Int gridDoorPos = new Vector2Int(Mathf.FloorToInt(startDoorMarker.position.x / tileSize), Mathf.FloorToInt(startDoorMarker.position.z / tileSize));
    RectInt closestRoom = data.rooms[0];
    float minDistance = float.MaxValue;
    Vector2Int closestCenter = Vector2Int.zero;

    foreach (RectInt room in data.rooms)
    {
      Vector2Int roomCenter = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
      float distance = Vector2.Distance(gridDoorPos, roomCenter);
      if (distance < minDistance)
      {
        minDistance = distance;
        closestRoom = room;
        closestCenter = roomCenter;
      }
    }
    CreateCorridorSegment(gridDoorPos.x, gridDoorPos.y, gridDoorPos.x, closestCenter.y, data);
    CreateCorridorSegment(gridDoorPos.x, closestCenter.y, closestCenter.x, closestCenter.y, data);
  }

  void ConnectGrandBossAvenue(DungeonData data, int aveX, int aveY, int aveThick, int aveLen)
  {
    Vector2Int mouthCenter = new Vector2Int(aveX + aveThick / 2, aveY);

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

      // 1. Tiền sảnh vòm (Entrance Vestibule) mở rộng toàn bộ bề ngang aveThick tại miệng đại lộ (Y = aveY - 2 đến aveY)
      // Giúp 100% các ô ở mép Nam đại lộ aveY đều có sàn bên dưới (mapGrid = 1), tuyệt đối không sinh tường đá bịt ngang lối vào!
      int vestH = 2;
      int vestY = aveY - vestH;
      RectInt vestibule = new RectInt(aveX, vestY, aveThick, vestH);
      data.corridors.Add(vestibule);

      // 2. Nối từ phòng kề bestRoom lên đáy tiền sảnh vestY tại cột mouthCenter.x
      // Đoạn 1: Đi từ tâm phòng bestCenter.x, bestCenter.y lên tới hàng vestY (tại cột bestCenter.x)
      // Đoạn 2: Đi ngang từ bestCenter.x sang mouthCenter.x (tại hàng vestY)
      CreateCorridorSegment(bestCenter.x, bestCenter.y, bestCenter.x, vestY, data, corridorThickness);
      CreateCorridorSegment(bestCenter.x, vestY, mouthCenter.x, vestY, data, corridorThickness);

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
