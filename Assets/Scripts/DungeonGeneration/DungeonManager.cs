using UnityEngine;

public class DungeonManager : MonoBehaviour
{
  public BSPGenerator generator;
  public DungeonRenderer dungeonRenderer;

  [Header("Hệ Thống Sinh Quái Hầm Ngục (Task D-01)")]
  [Tooltip("Bộ sinh quái tự động cho các phòng")]
  public ProceduralEnemySpawner enemySpawner;
  [Tooltip("Bộ tối ưu hóa ngủ đông và culling cho quái vật (Tiered AI/Animation LOD)")]
  public DungeonEnemyOptimizer enemyOptimizer;

  [Header("Thử Nghiệm AI & Quái Vật")]
  [Tooltip("Kéo Prefab SkeletonWarrior vào đây để sinh thử 1 quái trên tầng 2 kiểm tra AI leo thang")]
  public GameObject testEnemyPrefab;
  [Tooltip("Bật để tự động sinh 1 quái vật trên tầng 2 để kiểm tra xem quái có biết leo cầu thang xuống đuổi player không")]
  public bool spawnTestEnemyOnMezzanine = false;

  public static DungeonManager Instance { get; private set; }
  public event System.Action<DungeonData> OnDungeonGenerated;

  // Data chứa toàn bộ lưới sau khi render
  [System.NonSerialized]
  public DungeonData currentData;

  void Awake()
  {
    Instance = this;
  }

  void Start()
  {
    GenerateWorld();
  }

  public void GenerateWorld()
  {
    // 1. Chạy toán học tạo dữ liệu
    currentData = generator.Generate();

    // 2. Vẽ 3D & nướng NavMesh & trang trí props
    dungeonRenderer.BuildMesh(currentData);

    // 3. Tự động sinh quái vật trong hầm ngục (Task D-01)
    if (enemySpawner == null) enemySpawner = GetComponent<ProceduralEnemySpawner>();
    if (enemySpawner == null) enemySpawner = gameObject.AddComponent<ProceduralEnemySpawner>();
    enemySpawner.SpawnEnemies(currentData, generator.tileSize);

    // 4. Kích hoạt bộ tối ưu hóa ngủ đông quái vật (AI / Animation LOD & Sleep)
    if (enemyOptimizer == null) enemyOptimizer = GetComponent<DungeonEnemyOptimizer>();
    if (enemyOptimizer == null) enemyOptimizer = gameObject.AddComponent<DungeonEnemyOptimizer>();
    enemyOptimizer.RefreshEnemies(enemySpawner.activeEnemies);

    // 5. Sinh quái thử nghiệm kiểm tra tính năng AI leo cầu thang (nếu bật)
    SpawnTestEnemies();

    Debug.Log("Dungeon Generator Architecture Initialized!");
    OnDungeonGenerated?.Invoke(currentData);
  }

  private void SpawnTestEnemies()
  {
    if (testEnemyPrefab == null || !spawnTestEnemyOnMezzanine || currentData == null || currentData.roomDataList == null) return;

    foreach (var r in currentData.roomDataList)
    {
      if (r.hasMezzanine)
      {
        float tSize = generator.tileSize;
        // Đặt quái trên sàn tầng 2 ở vị trí xa cầu thang
        Vector3 spawnPos = (r.mezzanineSide == MezzanineWallSide.North)
          ? new Vector3(r.stairsPosition.x, r.mezzanineHeight + 0.1f, r.stairsPosition.z + tSize * 2f)
          : new Vector3(r.stairsPosition.x, r.mezzanineHeight + 0.1f, r.stairsPosition.z - tSize * 2f);

        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
          GameObject enemy = Instantiate(testEnemyPrefab, hit.position, Quaternion.identity);
          enemy.name = $"Test_Skeleton_Room_{r.id}";
          Debug.Log($"<color=cyan>[Test AI]</color> Đã sinh quái thử nghiệm tại phòng {r.id} (Tầng 2) tại tọa độ: {hit.position}");
        }
        break; // Chỉ sinh 1 con ở phòng gác lửng đầu tiên để test
      }
    }
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

        // Vẽ 4 cột đỡ phòng Boss
        if (r.type == RoomType.Boss && r.pillarPositions != null)
        {
          Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
          foreach (var pPos in r.pillarPositions)
          {
            Gizmos.DrawWireCube(pPos + Vector3.up * (r.ceilingHeight / 2f), new Vector3(2.5f * tSize, r.ceilingHeight, 2.5f * tSize));
          }
        }

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

    // Hiển thị trực quan đường đi NavMesh leo cầu thang lên tầng 2 (màu xanh lá neon)
    if (dungeonRenderer != null && dungeonRenderer.debugNavMeshPaths != null)
    {
      Gizmos.color = Color.green;
      foreach (var pathCorners in dungeonRenderer.debugNavMeshPaths)
      {
        if (pathCorners != null && pathCorners.Length > 1)
        {
          for (int i = 1; i < pathCorners.Length; i++)
          {
            Gizmos.DrawLine(pathCorners[i - 1] + Vector3.up * 0.2f, pathCorners[i] + Vector3.up * 0.2f);
            Gizmos.DrawSphere(pathCorners[i] + Vector3.up * 0.2f, 0.2f);
          }
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
