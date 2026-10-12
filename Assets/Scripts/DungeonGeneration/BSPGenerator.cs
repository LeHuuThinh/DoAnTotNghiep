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

  [Header("Cài đặt Phòng")]
  public int minRoomSize = 6;
  public int roomPadding = 1;

  [Header("Cài đặt Hành lang")]
  public int corridorThickness = 2;

  [Header("Cài đặt Cửa Khởi Đầu")]
  public Transform startDoorMarker;
  public float tileSize = 2f;

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

    SplitNode(data.rootNode);
    CreateRooms(data.rootNode, data);
    CreateCorridors(data.rootNode, data);
    ConnectStartRoom(data);
    BakeGrid(data);

    return data;
  }

  bool SplitNode(BSPNode node)
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
    SplitNode(node.leftChild);
    SplitNode(node.rightChild);
    return true;
  }

  void CreateRooms(BSPNode node, DungeonData data)
  {
    if (node == null) return;
    if (node.IsLeaf())
    {
      int roomWidth = Random.Range(minRoomSize, node.bounds.width - (roomPadding * 2));
      int roomHeight = Random.Range(minRoomSize, node.bounds.height - (roomPadding * 2));
      int roomX = node.bounds.x + Random.Range(roomPadding, node.bounds.width - roomWidth - roomPadding);
      int roomY = node.bounds.y + Random.Range(roomPadding, node.bounds.height - roomHeight - roomPadding);

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
    return GetNodeCenter(node.leftChild);
  }

  void CreateCorridorSegment(int x1, int y1, int x2, int y2, DungeonData data)
  {
    int startX = Mathf.Min(x1, x2);
    int startY = Mathf.Min(y1, y2);
    int width = Mathf.Abs(x1 - x2) + corridorThickness;
    int height = Mathf.Abs(y1 - y2) + corridorThickness;

    if (x1 == x2) startX -= corridorThickness / 2;
    else if (y1 == y2) startY -= corridorThickness / 2;

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