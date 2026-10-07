using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
  [Header("Cài đặt Lưới Map")]
  public int mapWidth = 100;
  public int mapHeight = 100;

  public int startRoomHeight = 30; // Giả sử phòng khởi đầu của bạn dài 30 ô dọc


  [Header("Cài đặt Cắt BSP")]
  public int minNodeSize = 15; // Kích thước Node nhỏ nhất không bị cắt tiếp

  public float cutChance = 0.95f; // Xác suất ngẫu nhiên dừng cắt (để tạo ra các Node lớn)

  [Header("Cài đặt Phòng")]
  public int minRoomSize = 12; // Kích thước tối thiểu của một căn phòng
  public int roomPadding = 2;  // Khoảng cách từ mép phòng đến ranh giới Node (để tường không dính liền nhau)

  [Header("Cài đặt Hành lang")]
  public int corridorThickness = 4; // Độ rộng hành lang (tính bằng số ô)

  [Header("Cài đặt Cửa Khởi Đầu")]
  // Xóa biến Vector2Int cũ đi, thay bằng Transform
  public Transform startDoorMarker;

  [Header("Cài đặt 3D Prefabs")]
  public GameObject visualTilePrefab;     // Kéo prefab gạch đá (đã xóa component Collider)
  public GameObject invisibleFloorPrefab; // Kéo khối Cube (Có Box Collider, đã xóa Mesh Renderer)
  public Transform dungeonParent;

  // Danh sách lưu các hình chữ nhật đại diện cho hành lang
  private List<RectInt> corridors = new List<RectInt>();

  // Mảng 2D lưu trữ dữ liệu bản đồ: 0 = Tường/Khoảng không, 1 = Sàn nhà
  private int[,] mapGrid;

  // Node cha bao trùm toàn bộ Dungeon
  private BSPNode rootNode;

  // Danh sách các phòng (để sau này rải quái/đồ vật)
  private List<RectInt> rooms = new List<RectInt>();

  void Start()
  {
    GenerateDungeon();
  }

  void GenerateDungeon()
  {
    // Khởi tạo mảng lưu dữ liệu (Cộng thêm offset để chứa cả map và phòng ngoài)
    // Nếu mapWidth = 200, mapHeight = 200, ta nên tạo mảng to hơn một chút nếu muốn gộp chung
    // Tuy nhiên, ở bước này bạn cứ giữ nguyên, ta chỉ cần dời tọa độ lưới đi


    // Cập nhật mảng grid bao trùm cả chiều cao của phòng gốc
    mapGrid = new int[mapWidth, mapHeight + startRoomHeight];

    // Khởi tạo Node gốc của BSP bắt đầu từ Y = 30 thay vì Y = 0
    // Trục X bắt đầu từ 0, trải dài 200 ô.
    rootNode = new BSPNode(new RectInt(0, startRoomHeight, mapWidth, mapHeight));

    SplitNode(rootNode);

    rooms.Clear(); // Xóa danh sách phòng cũ (nếu có) trước khi tạo mới
    CreateRooms(rootNode);

    corridors.Clear();
    CreateCorridors(rootNode);

    ConnectStartRoom();

    // 2 BƯỚC MỚI THÊM VÀO
    BakeGrid();
    InstantiateDungeon();

    // Đóng băng và gộp toàn bộ GameObjects con bên trong DungeonParent để tối ưu Draw Calls
    if (dungeonParent != null)
    {
      StaticBatchingUtility.Combine(dungeonParent.gameObject);
    }

    Debug.Log("Đã tạo xong khung BSP nhích lên trên!");
  }

  // Hàm đệ quy cắt Node (Phác thảo)
  bool SplitNode(BSPNode node)
  {
    // 1. Dừng đệ quy nếu node này đã bị cắt
    if (node.leftChild != null || node.rightChild != null)
      return false;

    // 2. Quyết định hướng cắt (Ngang hoặc Dọc)
    // Ưu tiên cắt ngang nếu node quá cao, và ưu tiên cắt dọc nếu node quá rộng
    bool splitHorizontally = Random.value > 0.5f;
    if (node.bounds.width > node.bounds.height && node.bounds.width / (float)node.bounds.height >= 1.25f)
      splitHorizontally = false;
    else if (node.bounds.height > node.bounds.width && node.bounds.height / (float)node.bounds.width >= 1.25f)
      splitHorizontally = true;

    // 3. Tính toán không gian hợp lệ để cắt
    int max = (splitHorizontally ? node.bounds.height : node.bounds.width) - minNodeSize;

    // ĐIỀU KIỆN 1: Bắt buộc dừng nếu không đủ chỗ cho 2 node
    if (max <= minNodeSize)
      return false;

    // ĐIỀU KIỆN 2: Ngẫu nhiên dừng cắt (25% cơ hội dừng) để tạo ra các Node lớn
    if (Random.value > cutChance)
      return false;

    // 4. Chọn ngẫu nhiên một điểm cắt nằm giữa khoảng an toàn
    int split = Random.Range(minNodeSize, max);

    // 5. Tạo 2 Node con dựa trên điểm cắt
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

    // 6. Tiếp tục đệ quy cắt nhỏ 2 Node con vừa tạo
    SplitNode(node.leftChild);
    SplitNode(node.rightChild);

    return true;
  }

  void CreateRooms(BSPNode node)
  {
    if (node == null) return;

    // Nếu là Node lá (Node cuối cùng không bị cắt nữa), ta sẽ khoét phòng ở đây
    if (node.IsLeaf())
    {
      // Tính toán kích thước phòng ngẫu nhiên (đảm bảo không vượt quá Node và có trừ hao Padding)
      int roomWidth = Random.Range(minRoomSize, node.bounds.width - (roomPadding * 2));
      int roomHeight = Random.Range(minRoomSize, node.bounds.height - (roomPadding * 2));

      // Tính toán tọa độ X, Y ngẫu nhiên để đặt phòng lọt lòng bên trong Node
      int roomX = node.bounds.x + Random.Range(roomPadding, node.bounds.width - roomWidth - roomPadding);
      int roomY = node.bounds.y + Random.Range(roomPadding, node.bounds.height - roomHeight - roomPadding);

      // Gán dữ liệu vào biến roomBounds của Node
      node.roomBounds = new RectInt(roomX, roomY, roomWidth, roomHeight);

      // Lưu vào danh sách tổng để sau này dễ rải quái
      rooms.Add(node.roomBounds);
    }
    else
    {
      // Nếu Node này đã bị cắt, đệ quy yêu cầu các Node con tự tạo phòng
      CreateRooms(node.leftChild);
      CreateRooms(node.rightChild);
    }
  }

  // Hàm đệ quy tìm tâm của một căn phòng bất kỳ trong Node
  Vector2Int GetNodeCenter(BSPNode node)
  {
    if (node.IsLeaf())
    {
      // Trả về tâm của căn phòng vàng
      return new Vector2Int(
          node.roomBounds.x + node.roomBounds.width / 2,
          node.roomBounds.y + node.roomBounds.height / 2
      );
    }
    // Nếu là Node cha, cứ đi sâu vào nhánh trái cho đến khi chạm đáy (Node lá)
    return GetNodeCenter(node.leftChild);
  }

  // Hàm tạo một hình chữ nhật hành lang và thêm vào danh sách
  void CreateCorridorSegment(int x1, int y1, int x2, int y2)
  {
    int startX = Mathf.Min(x1, x2);
    int startY = Mathf.Min(y1, y2);

    // Cộng thêm thickness để đảm bảo độ rộng
    int width = Mathf.Abs(x1 - x2) + corridorThickness;
    int height = Mathf.Abs(y1 - y2) + corridorThickness;

    // Căn giữa độ dày của hành lang dựa theo hướng
    if (x1 == x2) // Nối dọc
      startX -= corridorThickness / 2;
    else if (y1 == y2) // Nối ngang
      startY -= corridorThickness / 2;

    corridors.Add(new RectInt(startX, startY, width, height));
  }

  void CreateCorridors(BSPNode node)
  {
    if (node == null || node.IsLeaf()) return;

    // 1. Yêu cầu các Node con tự nối nội bộ của chúng trước
    CreateCorridors(node.leftChild);
    CreateCorridors(node.rightChild);

    // 2. Lấy tọa độ tâm của hai khu vực trái và phải
    Vector2Int leftCenter = GetNodeCenter(node.leftChild);
    Vector2Int rightCenter = GetNodeCenter(node.rightChild);

    // 3. Nối 2 tâm bằng hành lang chữ L (ngẫu nhiên bẻ ngang trước hay dọc trước)
    if (Random.value > 0.5f)
    {
      // Nối ngang trước, dọc sau (Góc bẻ tại điểm: rightCenter.x, leftCenter.y)
      CreateCorridorSegment(leftCenter.x, leftCenter.y, rightCenter.x, leftCenter.y);
      CreateCorridorSegment(rightCenter.x, leftCenter.y, rightCenter.x, rightCenter.y);
    }
    else
    {
      // Nối dọc trước, ngang sau (Góc bẻ tại điểm: leftCenter.x, rightCenter.y)
      CreateCorridorSegment(leftCenter.x, leftCenter.y, leftCenter.x, rightCenter.y);
      CreateCorridorSegment(leftCenter.x, rightCenter.y, rightCenter.x, rightCenter.y);
    }
  }

  void ConnectStartRoom()
  {
    if (rooms.Count == 0 || startDoorMarker == null) return;

    // Tự động ép kiểu tọa độ 3D thành tọa độ Grid nguyên (int)
    Vector2Int gridDoorPos = new Vector2Int(
        Mathf.RoundToInt(startDoorMarker.position.x),
        Mathf.RoundToInt(startDoorMarker.position.z)
    );

    RectInt closestRoom = rooms[0];
    float minDistance = float.MaxValue;
    Vector2Int closestCenter = Vector2Int.zero;

    // Quét tìm phòng gần nhất
    foreach (RectInt room in rooms)
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

    // Vẽ hành lang bằng tọa độ đã làm tròn
    CreateCorridorSegment(gridDoorPos.x, gridDoorPos.y, gridDoorPos.x, closestCenter.y);
    CreateCorridorSegment(gridDoorPos.x, closestCenter.y, closestCenter.x, closestCenter.y);
  }

  void BakeGrid()
  {
    // Quét tất cả các phòng và điền số 1 vào mảng
    foreach (RectInt room in rooms)
    {
      for (int x = room.x; x < room.x + room.width; x++)
      {
        for (int y = room.y; y < room.y + room.height; y++)
        {
          if (IsInBounds(x, y)) mapGrid[x, y] = 1;
        }
      }
    }

    // Quét tất cả các hành lang và điền số 1 vào mảng
    foreach (RectInt corridor in corridors)
    {
      for (int x = corridor.x; x < corridor.x + corridor.width; x++)
      {
        for (int y = corridor.y; y < corridor.y + corridor.height; y++)
        {
          if (IsInBounds(x, y)) mapGrid[x, y] = 1;
        }
      }
    }
  }

  // Hàm kiểm tra an toàn, tránh lỗi tràn mảng (Index Out Of Range)
  bool IsInBounds(int x, int y)
  {
    return x >= 0 && x < mapGrid.GetLength(0) && y >= 0 && y < mapGrid.GetLength(1);
  }

  void InstantiateDungeon()
  {
    // BƯỚC 1: Rải gạch trang trí (Visual) với bước nhảy 2
    for (int x = 0; x < mapGrid.GetLength(0); x += 2)
    {
      for (int y = 0; y < mapGrid.GetLength(1); y += 2)
      {
        if (mapGrid[x, y] == 1)
        {
          // Nếu Pivot của viên gạch nằm ở Tâm (Center), bạn phải cộng thêm 0.5f 
          // để viên gạch 2x2 nằm chính giữa 4 ô lưới 1x1
          Vector3 position = new Vector3(x + 0.5f, 0, y + 0.5f);

          // Nếu Pivot nằm ở góc (Corner), bạn chỉ cần dùng (x, 0, y)

          Instantiate(visualTilePrefab, position, Quaternion.identity, dungeonParent);
        }
      }
    }

    // BƯỚC 2: Sinh sàn ẩn tàng hình (Physics) bao trùm theo khối
    foreach (RectInt room in rooms)
    {
      CreateInvisibleCollider(room);
    }

    foreach (RectInt corridor in corridors)
    {
      CreateInvisibleCollider(corridor);
    }
  }

  void CreateInvisibleCollider(RectInt area)
  {
    // Tính toán điểm chính giữa của căn phòng/hành lang
    // (Trừ đi 1 rồi chia 2 vì tọa độ x,y đang là tâm của viên gạch, không phải mép ngoài)
    float centerX = area.x + (area.width - 1) / 2f;
    float centerZ = area.y + (area.height - 1) / 2f;

    // Đặt Y âm một chút (ví dụ -0.1f hoặc -0.2f) để bề mặt BoxCollider nằm ngay dưới các phiến đá
    Vector3 centerPos = new Vector3(centerX, -0.2f, centerZ);

    GameObject invFloor = Instantiate(invisibleFloorPrefab, centerPos, Quaternion.identity, dungeonParent);

    // Kéo giãn khối Cube: Width và Height tương ứng số ô, trục Y để dẹt (0.2)
    invFloor.transform.localScale = new Vector3(area.width, 0.2f, area.height);
  }

  void OnDrawGizmos()
  {
    if (rootNode == null) return;

    // Vẽ lưới khung của các Node và các phòng (phòng vàng) bằng đệ quy
    DrawNodeGizmo(rootNode);

    // Vẽ các hành lang màu đỏ tía (Magenta)
    Gizmos.color = Color.magenta;
    foreach (RectInt corridor in corridors)
    {
      Vector3 corridorCenter = new Vector3(corridor.x + corridor.width / 2f, 0, corridor.y + corridor.height / 2f);
      Vector3 corridorSize = new Vector3(corridor.width, 0.1f, corridor.height);
      Gizmos.DrawWireCube(corridorCenter, corridorSize);
    }

    // Vẽ cục mốc màu trắng tại vị trí Cửa khởi đầu (dựa vào transform marker)
    if (startDoorMarker != null)
    {
      Gizmos.color = Color.white;

      // Làm tròn tọa độ 3D của marker sang tọa độ nguyên để vẽ khít lưới
      int gridX = Mathf.RoundToInt(startDoorMarker.position.x);
      int gridZ = Mathf.RoundToInt(startDoorMarker.position.z);

      Gizmos.DrawCube(new Vector3(gridX, 0, gridZ), new Vector3(corridorThickness, 1f, corridorThickness));
    }
  }

  void DrawNodeGizmo(BSPNode node)
  {
    if (node == null) return;

    // 1. Vẽ ranh giới Node (Viền Cyan)
    Gizmos.color = node.IsLeaf() ? Color.cyan : Color.green;
    Vector3 center = new Vector3(node.bounds.x + node.bounds.width / 2f, 0, node.bounds.y + node.bounds.height / 2f);
    Vector3 size = new Vector3(node.bounds.width, 0.1f, node.bounds.height);
    Gizmos.DrawWireCube(center, size);

    // 2. Vẽ CĂN PHÒNG THỰC TẾ lọt lòng bên trong (Viền Vàng)
    if (node.IsLeaf() && node.roomBounds.width > 0)
    {
      Gizmos.color = Color.yellow;
      Vector3 roomCenter = new Vector3(node.roomBounds.x + node.roomBounds.width / 2f, 0, node.roomBounds.y + node.roomBounds.height / 2f);
      Vector3 roomSize = new Vector3(node.roomBounds.width, 0.1f, node.roomBounds.height);

      // Vẽ khối lập phương dạng dây thép màu vàng
      Gizmos.DrawWireCube(roomCenter, roomSize);
    }

    if (node.leftChild != null) DrawNodeGizmo(node.leftChild);
    if (node.rightChild != null) DrawNodeGizmo(node.rightChild);
  }
}