using UnityEngine;

public class DungeonManager : MonoBehaviour
{
  public BSPGenerator generator;
  public DungeonRenderer dungeonRenderer;

  // Data chứa toàn bộ lưới sau khi render
  [System.NonSerialized]
  public DungeonData currentData;

  void Start()
  {
    GenerateWorld();
  }

  public void GenerateWorld()
  {
    // 1. Chạy toán học tạo dữ liệu
    currentData = generator.Generate();

    // 2. Vẽ 3D
    dungeonRenderer.BuildMesh(currentData);

    // 3. Tương lai bạn sẽ thêm:
    // propSpawner.SpawnDecorations(currentData);
    // entitySpawner.SpawnMobs(currentData);

    Debug.Log("Dungeon Generator Architecture Initialized!");
  }

  void OnDrawGizmos()
  {
    if (currentData == null || currentData.rootNode == null) return;

    float tSize = generator.tileSize;

    DrawNodeGizmo(currentData.rootNode, tSize);

    Gizmos.color = Color.magenta;
    foreach (RectInt corridor in currentData.corridors)
    {
      Vector3 corridorCenter = new Vector3((corridor.x + corridor.width / 2f) * tSize, 0, (corridor.y + corridor.height / 2f) * tSize);
      Vector3 corridorSize = new Vector3(corridor.width * tSize, 0.1f, corridor.height * tSize);
      Gizmos.DrawWireCube(corridorCenter, corridorSize);
    }

    if (generator.startDoorMarker != null)
    {
      Gizmos.color = Color.white;
      int gridX = Mathf.FloorToInt(generator.startDoorMarker.position.x / tSize);
      int gridZ = Mathf.FloorToInt(generator.startDoorMarker.position.z / tSize);
      Gizmos.DrawCube(new Vector3((gridX + 0.5f) * tSize, 0, (gridZ + 0.5f) * tSize), new Vector3(generator.corridorThickness * tSize, 1f, generator.corridorThickness * tSize));
    }

    // Hiển thị trực quan loại phòng và tầng 2 trong Scene View
    if (currentData.roomDataList != null)
    {
      foreach (var r in currentData.roomDataList)
      {
        Vector3 rCenter = new Vector3((r.bounds.x + r.bounds.width / 2f) * tSize, r.ceilingHeight / 2f, (r.bounds.y + r.bounds.height / 2f) * tSize);
        Vector3 rSize = new Vector3(r.bounds.width * tSize, r.ceilingHeight, r.bounds.height * tSize);

        switch (r.type)
        {
          case RoomType.Start:
            Gizmos.color = Color.cyan;
            break;
          case RoomType.Boss:
            Gizmos.color = Color.red;
            break;
          case RoomType.Mezzanine:
            Gizmos.color = new Color(0.2f, 0.8f, 1f);
            break;
          case RoomType.Treasure:
            Gizmos.color = Color.yellow;
            break;
          default:
            Gizmos.color = Color.gray;
            break;
        }

        Gizmos.DrawWireCube(rCenter, rSize);

        // Vẽ sàn gác lửng tầng 2
        if (r.hasMezzanine)
        {
          Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f); // Màu cam
          Vector3 mCenter = new Vector3((r.mezzanineBounds.x + r.mezzanineBounds.width / 2f) * tSize, r.mezzanineHeight, (r.mezzanineBounds.y + r.mezzanineBounds.height / 2f) * tSize);
          Vector3 mSize = new Vector3(r.mezzanineBounds.width * tSize, 0.4f, r.mezzanineBounds.height * tSize);
          Gizmos.DrawCube(mCenter, mSize);

          // Vẽ vị trí cầu thang
          Gizmos.color = Color.green;
          Gizmos.DrawSphere(r.stairsPosition + Vector3.up * 1f, 0.6f);
        }
      }
    }
  }

  void DrawNodeGizmo(BSPNode node, float tSize)
  {
    if (node == null) return;

    Gizmos.color = node.IsLeaf() ? Color.cyan : Color.green;
    Vector3 center = new Vector3((node.bounds.x + node.bounds.width / 2f) * tSize, 0, (node.bounds.y + node.bounds.height / 2f) * tSize);
    Vector3 size = new Vector3(node.bounds.width * tSize, 0.1f, node.bounds.height * tSize);
    Gizmos.DrawWireCube(center, size);

    if (node.IsLeaf() && node.roomBounds.width > 0)
    {
      Gizmos.color = Color.yellow;
      Vector3 roomCenter = new Vector3((node.roomBounds.x + node.roomBounds.width / 2f) * tSize, 0, (node.roomBounds.y + node.roomBounds.height / 2f) * tSize);
      Vector3 roomSize = new Vector3(node.roomBounds.width * tSize, 0.1f, node.roomBounds.height * tSize);
      Gizmos.DrawWireCube(roomCenter, roomSize);
    }

    if (node.leftChild != null) DrawNodeGizmo(node.leftChild, tSize);
    if (node.rightChild != null) DrawNodeGizmo(node.rightChild, tSize);
  }
}