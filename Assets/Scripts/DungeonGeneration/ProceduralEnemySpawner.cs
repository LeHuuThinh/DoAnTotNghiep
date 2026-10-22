using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bộ sinh quái tự động cho hầm ngục theo mô hình Biệt Đội / Cụm (Squad / Pack-Based Spawning System - Task D-01 Nâng Cấp)
/// - Phòng thường: 1 - 2 cụm quái dưới đất; phòng đại sảnh lớn (kích thước lớn): 2 - 3 cụm.
/// - Phòng gác lửng (Mezzanine): 1 - 2 cụm dưới đất và 1 - 2 cụm trên tầng 2 (tỷ lệ tầng 2 xuất hiện 2 nhóm là thấp hơn).
/// - Quái cận chiến và tầm xa không bị bó cứng theo tầng mà phân bổ linh hoạt theo đội hình:
///     + Nhóm 4 quái: Đảm bảo 2 Melee (1 Warrior + 1 Minion) và 2 Ranged (1 Rogue + 1 Mage).
///     + Nhóm 3 quái: 2 Melee + 1 Ranged HOẶC 1 Melee + 2 Ranged.
///     + Nhóm 2 quái: 2 Melee HOẶC 1 Melee + 1 Ranged.
///     + Nhóm 1 quái lẻ: Random ngẫu nhiên 1 trong 4 loại quái.
/// - Mỗi cụm quái tụ tập quanh một Điểm Neo (Anchor Point) tự nhiên, các cụm giãn cách nhau để tạo nhiều điểm đụng độ.
/// - Tuyệt đối không sinh quái tại Start Room và Boss Room.
/// - Dò tìm chuẩn xác trên NavMesh (NavMesh.SamplePosition) và chống sinh đè lên đạo cụ tĩnh.
/// </summary>
public class ProceduralEnemySpawner : MonoBehaviour
{
  public enum SquadSize
  {
    Solo = 1,
    Duo = 2,
    Trio = 3,
    Quad = 4
  }

  [Header("1. Quái Cận Chiến (Melee Prefabs)")]
  [Tooltip("Chiến binh xương mang khiên kiếm (máu cao, biết parry và counter)")]
  public GameObject skeletonWarriorPrefab;
  [Tooltip("Bộ xương lính quèn cơ động (số lượng đông, áp sát nhanh)")]
  public GameObject skeletonMinionPrefab;

  [Header("2. Quái Tầm Xa (Ranged Prefabs)")]
  [Tooltip("Thích khách xạ thủ bắn cung tầm xa (giữ cự ly, thả diều kiting)")]
  public GameObject skeletonRoguePrefab;
  [Tooltip("Pháp sư gọi bão lửa và cột sáng trừng phạt")]
  public GameObject skeletonMagePrefab;

  [Header("3. Cấu Hình Sinh Quái Theo Cụm (Squad Settings)")]
  [Tooltip("Bật/Tắt tự động sinh quái vật khi hầm ngục khởi tạo")]
  public bool enableSpawning = true;

  [Header("Quy Mô Phòng & Số Cụm Quái (Squad Counts Per Room)")]
  [Tooltip("Ngưỡng diện tích (số ô tile) để phòng được coi là Đại sảnh lớn")]
  public int largeRoomTileThreshold = 220;

  [Range(1, 3)]
  [Tooltip("Số nhóm quái tối thiểu ở sàn trệt phòng thường (nhỏ/vừa: đúng 1 nhóm)")]
  public int minGroundSquadsStandard = 1;
  [Range(1, 3)]
  [Tooltip("Số nhóm quái tối đa ở sàn trệt phòng thường (nhỏ/vừa: đúng 1 nhóm)")]
  public int maxGroundSquadsStandard = 1;

  [Range(1, 3)]
  [Tooltip("Số nhóm quái tối thiểu ở sàn trệt phòng lớn")]
  public int minGroundSquadsLarge = 1;
  [Range(1, 4)]
  [Tooltip("Số nhóm quái tối đa ở sàn trệt phòng lớn")]
  public int maxGroundSquadsLarge = 2;

  [Header("Tầng 2 Gác Lửng (Mezzanine Floor)")]
  [Range(1, 2)]
  [Tooltip("Số nhóm quái tối thiểu trên tầng 2 phòng thường (nhỏ/vừa: đúng 1 nhóm)")]
  public int minMezzanineSquadsStandard = 1;
  [Range(1, 2)]
  [Tooltip("Số nhóm quái tối đa trên tầng 2 phòng thường (nhỏ/vừa: đúng 1 nhóm)")]
  public int maxMezzanineSquadsStandard = 1;

  [Range(1, 3)]
  [Tooltip("Số nhóm quái tối thiểu trên tầng 2 phòng lớn")]
  public int minMezzanineSquadsLarge = 1;
  [Range(1, 3)]
  [Tooltip("Số nhóm quái tối đa trên tầng 2 phòng lớn")]
  public int maxMezzanineSquadsLarge = 2;

  [Header("Tỷ Lệ Kích Thước Nhóm (Squad Size Chances)")]
  [Range(0.05f, 0.5f)]
  [Tooltip("Tỷ lệ sinh nhóm 1 quái lẻ (Solo Scout)")]
  public float chanceSoloSquad = 0.15f;
  [Range(0.1f, 0.6f)]
  [Tooltip("Tỷ lệ sinh nhóm 2 quái (Duo Patrol)")]
  public float chanceDuoSquad = 0.40f;
  [Range(0.1f, 0.6f)]
  [Tooltip("Tỷ lệ sinh nhóm 3 quái (Trio Squad)")]
  public float chanceTrioSquad = 0.30f;
  [Range(0.05f, 0.4f)]
  [Tooltip("Tỷ lệ sinh nhóm 4 quái (Quad Fireteam)")]
  public float chanceQuadSquad = 0.15f;

  [Header("Phòng Kho Báu & Điện Thờ (Treasure / Shrine)")]
  [Tooltip("Bật sinh quái bảo vệ phòng kho báu / đền thờ")]
  public bool spawnGuardsInSpecialRooms = true;

  [Header("4. Định Vị & Giãn Cách Không Gian (Spatial Clustering)")]
  [Range(1.5f, 5.0f)]
  [Tooltip("Bán kính tụ tập của các thành viên trong cùng một nhóm (mét quanh tâm nhóm)")]
  public float squadClusterRadius = 3.0f;

  [Range(1.0f, 3.0f)]
  [Tooltip("Khoảng cách tối thiểu giữa 2 quái trong cùng một nhóm (tránh dính người)")]
  public float minDistanceBetweenMembers = 1.7f;

  [Range(5.0f, 18.0f)]
  [Tooltip("Khoảng cách tối thiểu giữa tâm 2 nhóm quái khác nhau trong cùng một phòng (tránh dồn cụm)")]
  public float minDistanceBetweenSquads = 8.5f;

  [Range(1, 4)]
  [Tooltip("Số ô an toàn cách cửa ra vào")]
  public int doorClearanceTiles = 2;

  [Range(1, 3)]
  [Tooltip("Khoảng cách đệm an toàn tính từ chân tường")]
  public int wallPaddingTiles = 1;

  [Range(1.0f, 5.0f)]
  [Tooltip("Bán kính dò tìm mặt phẳng NavMesh (NavMesh.SamplePosition)")]
  public float navMeshSampleRadius = 2.5f;

  [Tooltip("Layer vật cản (Props tĩnh, thùng, bàn ghế, tường) để chống sinh đè")]
  public LayerMask obstacleCheckMask = 1; // Default Layer

  [Header("5. Quản Lý Hierarchy")]
  [Tooltip("Transform chứa toàn bộ quái vật sinh ra (tự động tạo nếu để trống)")]
  public Transform enemyParentContainer;

  // Thống kê runtime
  [System.NonSerialized]
  public List<GameObject> activeEnemies = new List<GameObject>();
  private readonly List<Vector3> debugMeleePoints = new List<Vector3>();
  private readonly List<Vector3> debugRangedPoints = new List<Vector3>();
  private readonly List<Vector3> debugSquadAnchors = new List<Vector3>();

  private void Awake()
  {
    EnsurePrefabsAssigned();
  }

#if UNITY_EDITOR
  private void OnValidate()
  {
    EnsurePrefabsAssigned();
  }
#endif

  /// <summary>
  /// Tự động dò tìm Prefabs quái nếu chưa được kéo vào Inspector
  /// </summary>
  public void EnsurePrefabsAssigned()
  {
#if UNITY_EDITOR
    if (skeletonWarriorPrefab == null)
      skeletonWarriorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");

    if (skeletonMinionPrefab == null)
      skeletonMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");

    if (skeletonRoguePrefab == null)
      skeletonRoguePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");

    if (skeletonMagePrefab == null)
      skeletonMagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");
#endif
  }

  /// <summary>
  /// Dọn dẹp toàn bộ quái vật đã sinh trước đó
  /// </summary>
  public void ClearEnemies()
  {
    for (int i = activeEnemies.Count - 1; i >= 0; i--)
    {
      if (activeEnemies[i] != null)
      {
        if (Application.isPlaying) Destroy(activeEnemies[i]);
        else DestroyImmediate(activeEnemies[i]);
      }
    }

    if (enemyParentContainer != null)
    {
      for (int i = enemyParentContainer.childCount - 1; i >= 0; i--)
      {
        GameObject child = enemyParentContainer.GetChild(i).gameObject;
        if (Application.isPlaying) Destroy(child);
        else DestroyImmediate(child);
      }
    }

    activeEnemies.Clear();
    debugMeleePoints.Clear();
    debugRangedPoints.Clear();
    debugSquadAnchors.Clear();
  }

  /// <summary>
  /// Điểm vào chính: Tự động phân bổ các cụm quái cho toàn bộ hầm ngục
  /// </summary>
  public void SpawnEnemies(DungeonData data, float tileSize)
  {
    if (!enableSpawning)
    {
      Debug.Log("<color=yellow>[Enemy Spawner]</color> Tính năng sinh quái tự động đang TẮT trong Inspector.");
      return;
    }

    if (data == null || data.roomDataList == null || data.roomDataList.Count == 0)
    {
      Debug.LogWarning("<color=red>[Enemy Spawner]</color> Không có dữ liệu RoomData để sinh quái!");
      return;
    }

    EnsurePrefabsAssigned();
    ClearEnemies();

    if (enemyParentContainer == null)
    {
      GameObject containerObj = GameObject.Find("DungeonEnemies");
      if (containerObj == null)
      {
        containerObj = new GameObject("DungeonEnemies");
      }
      enemyParentContainer = containerObj.transform;
    }

    int totalMelee = 0;
    int totalRanged = 0;
    int totalSquads = 0;
    int roomsPopulated = 0;

    foreach (var room in data.roomDataList)
    {
      // 1. Chỉ bỏ qua phòng Boss (dành riêng cho Boss Arena Ice Golem ở Task D-02)
      // Phòng bắt đầu (data.startRoom / RoomType.Start) vẫn sinh quái bình thường để người chơi vừa bước vào hầm ngục là có quái giao chiến ngay!
      if (room == data.bossRoom || room.type == RoomType.Boss) continue;

      int roomSpawnedBefore = activeEnemies.Count;
      int roomAreaTiles = room.bounds.width * room.bounds.height;
      bool isLargeRoom = roomAreaTiles >= largeRoomTileThreshold || (room.bounds.width >= 16 && room.bounds.height >= 16);

      // Danh sách vị trí các quái đã sinh trong phòng này (để kiểm tra khoảng cách)
      List<Vector3> roomSpawnedPositions = new List<Vector3>();
      List<Vector3> roomSquadAnchors = new List<Vector3>();

      // 2. PHÒNG KHO BÁU & ĐIỆN THỜ (Treasure / Shrine): 1 nhóm nhỏ (1 - 2 quái) làm lính gác
      if (room.type == RoomType.Treasure || room.type == RoomType.Shrine)
      {
        if (spawnGuardsInSpecialRooms)
        {
          SquadSize specialSize = (Random.value < 0.65f) ? SquadSize.Duo : SquadSize.Solo;
          int spawned = SpawnSquad(room, false, specialSize, tileSize, roomSpawnedPositions, roomSquadAnchors, ref totalMelee, ref totalRanged, "GuardSquad");
          if (spawned > 0) totalSquads++;
        }
      }
      else
      {
        // 3. CÁC PHÒNG CHIẾN ĐẤU (Bao gồm phòng nối với phòng khởi đầu, phòng thường, phòng lớn, phòng gác lửng):
        // 3A. Sàn trệt Tầng 1:
        // - Phòng nhỏ & vừa: đúng 1 nhóm
        // - Phòng lớn: 1 - 2 nhóm
        int groundSquadCount = isLargeRoom 
          ? Random.Range(minGroundSquadsLarge, maxGroundSquadsLarge + 1)
          : Random.Range(minGroundSquadsStandard, maxGroundSquadsStandard + 1);

        for (int s = 0; s < groundSquadCount; s++)
        {
          SquadSize size = RollSquadSize();
          int spawned = SpawnSquad(room, false, size, tileSize, roomSpawnedPositions, roomSquadAnchors, ref totalMelee, ref totalRanged, $"Ground_Squad_{s + 1}");
          if (spawned > 0) totalSquads++;
        }

        // 3B. Tầng 2 Gác Lửng (Nếu phòng có gác lửng):
        // - Phòng nhỏ & vừa: đúng 1 nhóm
        // - Phòng lớn: 1 - 2 nhóm
        if (room.hasMezzanine || room.type == RoomType.Mezzanine)
        {
          int mezzanineSquadCount = isLargeRoom
            ? Random.Range(minMezzanineSquadsLarge, maxMezzanineSquadsLarge + 1)
            : Random.Range(minMezzanineSquadsStandard, maxMezzanineSquadsStandard + 1);

          for (int s = 0; s < mezzanineSquadCount; s++)
          {
            SquadSize size = RollSquadSize();
            int spawned = SpawnSquad(room, true, size, tileSize, roomSpawnedPositions, roomSquadAnchors, ref totalMelee, ref totalRanged, $"Mezzanine_Squad_{s + 1}");
            if (spawned > 0) totalSquads++;
          }
        }
      }

      if (activeEnemies.Count > roomSpawnedBefore)
      {
        roomsPopulated++;
      }
    }

    Debug.Log($"<color=green>[Enemy Spawner - Biệt Đội]</color> Hoàn tất sinh quái! Đã bố trí <b>{activeEnemies.Count}</b> quái vật (<b>{totalMelee}</b> Cận chiến, <b>{totalRanged}</b> Tầm xa) chia làm <b>{totalSquads}</b> cụm biệt đội trên <b>{roomsPopulated}</b> căn phòng.");
  }

  /// <summary>
  /// Quyết định kích thước nhóm quái ngẫu nhiên dựa trên phân phối trọng số
  /// </summary>
  private SquadSize RollSquadSize()
  {
    float roll = Random.value;
    float cumulative = chanceSoloSquad;
    if (roll < cumulative) return SquadSize.Solo;

    cumulative += chanceDuoSquad;
    if (roll < cumulative) return SquadSize.Duo;

    cumulative += chanceTrioSquad;
    if (roll < cumulative) return SquadSize.Trio;

    return SquadSize.Quad;
  }

  /// <summary>
  /// Xây dựng danh sách Prefabs cho một nhóm quái theo đúng tiêu chí thiết kế:
  /// - 4 quái: 2 Melee (1 Warrior + 1 Minion) + 2 Ranged (1 Rogue + 1 Mage).
  /// - 3 quái: 2 Melee + 1 Ranged HOẶC 1 Melee + 2 Ranged.
  /// - 2 quái: 2 Melee HOẶC 1 Melee + 1 Ranged.
  /// - 1 quái: Ngẫu nhiên 1 trong 4 loại.
  /// </summary>
  private List<GameObject> BuildSquadComposition(SquadSize size, out int meleeInSquad, out int rangedInSquad)
  {
    List<GameObject> roster = new List<GameObject>();
    meleeInSquad = 0;
    rangedInSquad = 0;

    switch (size)
    {
      case SquadSize.Quad: // 4 quái: 2 Melee (1 Warrior, 1 Minion) + 2 Ranged (1 Rogue, 1 Mage)
        AddMember(roster, skeletonWarriorPrefab, ref meleeInSquad);
        AddMember(roster, skeletonMinionPrefab, ref meleeInSquad);
        AddMember(roster, skeletonRoguePrefab, ref rangedInSquad);
        AddMember(roster, skeletonMagePrefab, ref rangedInSquad);
        break;

      case SquadSize.Trio: // 3 quái: 1 Melee 2 Ranged HOẶC 2 Melee 1 Ranged
        if (Random.value < 0.5f)
        {
          // 2 Melee + 1 Ranged
          AddMember(roster, skeletonWarriorPrefab, ref meleeInSquad);
          AddMember(roster, skeletonMinionPrefab, ref meleeInSquad);
          GameObject ranged = (Random.value < 0.5f) ? skeletonRoguePrefab : skeletonMagePrefab;
          AddMember(roster, ranged, ref rangedInSquad);
        }
        else
        {
          // 1 Melee + 2 Ranged
          GameObject melee = (Random.value < 0.5f) ? skeletonWarriorPrefab : skeletonMinionPrefab;
          AddMember(roster, melee, ref meleeInSquad);
          AddMember(roster, skeletonRoguePrefab, ref rangedInSquad);
          AddMember(roster, skeletonMagePrefab, ref rangedInSquad);
        }
        break;

      case SquadSize.Duo: // 2 quái: 2 Melee HOẶC 1 Melee 1 Ranged
        if (Random.value < 0.5f)
        {
          // 2 Melee
          AddMember(roster, skeletonWarriorPrefab, ref meleeInSquad);
          AddMember(roster, skeletonMinionPrefab, ref meleeInSquad);
        }
        else
        {
          // 1 Melee + 1 Ranged
          GameObject melee = (Random.value < 0.5f) ? skeletonWarriorPrefab : skeletonMinionPrefab;
          GameObject ranged = (Random.value < 0.5f) ? skeletonRoguePrefab : skeletonMagePrefab;
          AddMember(roster, melee, ref meleeInSquad);
          AddMember(roster, ranged, ref rangedInSquad);
        }
        break;

      case SquadSize.Solo: // 1 quái: Random ngẫu nhiên bất kỳ
      default:
        float r = Random.value;
        if (r < 0.35f) AddMember(roster, skeletonMinionPrefab, ref meleeInSquad);
        else if (r < 0.60f) AddMember(roster, skeletonWarriorPrefab, ref meleeInSquad);
        else if (r < 0.80f) AddMember(roster, skeletonRoguePrefab, ref rangedInSquad);
        else AddMember(roster, skeletonMagePrefab, ref rangedInSquad);
        break;
    }

    return roster;
  }

  private void AddMember(List<GameObject> roster, GameObject prefab, ref int counter)
  {
    if (prefab != null)
    {
      roster.Add(prefab);
      counter++;
    }
  }

  /// <summary>
  /// Sinh một cụm quái (Squad) quây quần quanh một Điểm Neo (Anchor Point)
  /// </summary>
  private int SpawnSquad(
    RoomData room, 
    bool onMezzanine, 
    SquadSize size, 
    float tileSize, 
    List<Vector3> roomSpawnedPositions, 
    List<Vector3> roomSquadAnchors, 
    ref int totalMelee, 
    ref int totalRanged, 
    string squadLabel)
  {
    // 1. Tìm Điểm Neo (Anchor Point) cho cả nhóm
    if (!TryFindSquadAnchor(room, onMezzanine, tileSize, roomSquadAnchors, out Vector3 anchorPos))
    {
      return 0; // Không tìm được vị trí đặt cụm thỏa mãn khoảng cách
    }

    roomSquadAnchors.Add(anchorPos);
    debugSquadAnchors.Add(anchorPos);

    // 2. Xây dựng danh sách thành viên biệt đội
    List<GameObject> squadRoster = BuildSquadComposition(size, out int meleeCount, out int rangedCount);
    if (squadRoster.Count == 0) return 0;

    float expectedElevation = onMezzanine ? room.mezzanineHeight : 0.1f;
    float maxElevationDelta = onMezzanine ? 1.5f : 2.0f;
    int successfullySpawned = 0;

    // 3. Sinh từng thành viên quây quanh tâm cụm (bán kính squadClusterRadius)
    for (int i = 0; i < squadRoster.Count; i++)
    {
      GameObject prefab = squadRoster[i];
      if (prefab == null) continue;

      bool memberPlaced = false;
      int memberAttempts = 20;

      while (memberAttempts-- > 0 && !memberPlaced)
      {
        // Vị trí offset ngẫu nhiên quanh tâm cụm (bán kính 0m đến squadClusterRadius)
        Vector2 randomOffset2D = Random.insideUnitCircle * squadClusterRadius;
        Vector3 candidatePos = anchorPos + new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);

        if (TryFindNavMeshPosition(candidatePos, expectedElevation, maxElevationDelta, out Vector3 validPos))
        {
          // Kiểm tra không đè lên quái khác & không bị kẹt trong props
          if (IsPositionSafeForMember(validPos, roomSpawnedPositions))
          {
            // Hướng nhìn tự nhiên: Quái đứng quay mặt ra phía ngoài tâm cụm để cảnh giới
            Vector3 lookDir = (validPos - anchorPos);
            lookDir.y = 0f;
            Quaternion memberRotation = (lookDir.sqrMagnitude > 0.05f) 
              ? Quaternion.LookRotation(lookDir) 
              : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            string enemyName = $"Enemy_R{room.id}_{squadLabel}_{prefab.name}_{i + 1}";
            GameObject enemy = InstantiateEnemy(prefab, validPos, memberRotation, enemyName);

            roomSpawnedPositions.Add(validPos);
            if (prefab == skeletonWarriorPrefab || prefab == skeletonMinionPrefab)
            {
              debugMeleePoints.Add(validPos);
              totalMelee++;
            }
            else
            {
              debugRangedPoints.Add(validPos);
              totalRanged++;
            }

            successfullySpawned++;
            memberPlaced = true;
          }
        }
      }
    }

    return successfullySpawned;
  }

  /// <summary>
  /// Tìm kiếm Điểm Neo (Anchor Point) cho một nhóm quái mới trong phòng
  /// </summary>
  private bool TryFindSquadAnchor(RoomData room, bool onMezzanine, float tileSize, List<Vector3> existingAnchors, out Vector3 anchorPos)
  {
    anchorPos = Vector3.zero;

    List<Vector2Int> candidates = onMezzanine 
      ? GetMezzanineCandidateTiles(room) 
      : GetGroundCandidateTiles(room, room.hasMezzanine);

    if (candidates.Count == 0) return false;

    ShuffleList(candidates);
    float expectedElevation = onMezzanine ? room.mezzanineHeight : 0.1f;
    float maxElevationDelta = onMezzanine ? 1.5f : 2.0f;

    // Khoảng cách giãn cách giữa các nhóm trên gác lửng có thể linh hoạt hơn do diện tích hẹp
    float requiredSquadDistance = onMezzanine ? (minDistanceBetweenSquads * 0.75f) : minDistanceBetweenSquads;

    foreach (var tile in candidates)
    {
      Vector3 worldOrigin = new Vector3((tile.x + 0.5f) * tileSize, expectedElevation + 0.1f, (tile.y + 0.5f) * tileSize);

      if (TryFindNavMeshPosition(worldOrigin, expectedElevation, maxElevationDelta, out Vector3 sampledPos))
      {
        // Kiểm tra vật cản tĩnh tại tâm cụm
        if (Physics.CheckSphere(sampledPos + Vector3.up * 0.8f, 0.45f, obstacleCheckMask, QueryTriggerInteraction.Ignore))
        {
          continue;
        }

        // Kiểm tra khoảng cách với tâm các cụm khác trong cùng phòng
        bool tooCloseToAnotherSquad = false;
        foreach (var existing in existingAnchors)
        {
          if (Vector3.Distance(sampledPos, existing) < requiredSquadDistance)
          {
            tooCloseToAnotherSquad = true;
            break;
          }
        }

        if (!tooCloseToAnotherSquad)
        {
          anchorPos = sampledPos;
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Thu thập danh sách ô sàn trệt hợp lệ (cách vách tường, tránh cửa, tránh chân cầu thang)
  /// </summary>
  private List<Vector2Int> GetGroundCandidateTiles(RoomData room, bool excludeMezzanine)
  {
    List<Vector2Int> tiles = new List<Vector2Int>();
    RectInt b = room.bounds;

    int minX = b.x + wallPaddingTiles;
    int maxX = b.x + b.width - wallPaddingTiles;
    int minY = b.y + wallPaddingTiles;
    int maxY = b.y + b.height - wallPaddingTiles;

    for (int x = minX; x < maxX; x++)
    {
      for (int y = minY; y < maxY; y++)
      {
        Vector2Int tile = new Vector2Int(x, y);

        // Tránh cửa ra vào
        if (room.IsNearDoor(tile, doorClearanceTiles)) continue;

        // Nếu phòng có gác lửng và yêu cầu loại trừ sàn tầng 2 (để quái cận chiến sinh ở khoảng thông tầng)
        if (excludeMezzanine && room.hasMezzanine)
        {
          if (room.IsInMezzanine(x, y)) continue;

          // Tránh khu vực chân cầu thang
          if (Mathf.Abs(x - room.stairGridPos.x) <= 2 && Mathf.Abs(y - room.stairGridPos.y) <= 3)
          {
            continue;
          }
        }

        tiles.Add(tile);
      }
    }

    return tiles;
  }

  /// <summary>
  /// Thu thập danh sách ô sàn gác lửng tầng 2 (tránh ô đón thang landing tiles)
  /// </summary>
  private List<Vector2Int> GetMezzanineCandidateTiles(RoomData room)
  {
    List<Vector2Int> tiles = new List<Vector2Int>();
    if (!room.hasMezzanine) return tiles;

    if (room.mezzanineTiles != null && room.mezzanineTiles.Count > 0)
    {
      foreach (var tile in room.mezzanineTiles)
      {
        // Tránh ô đón thang trên tầng 2 để quái không cản trở lối bước lên
        if (room.stairLandingTiles != null && room.stairLandingTiles.Contains(tile)) continue;
        if (Mathf.Abs(tile.x - room.stairGridPos.x) <= 1 && Mathf.Abs(tile.y - room.stairGridPos.y) <= 1) continue;

        tiles.Add(tile);
      }
    }
    else
    {
      RectInt mb = room.mezzanineBounds;
      for (int x = mb.x; x < mb.x + mb.width; x++)
      {
        for (int y = mb.y; y < mb.y + mb.height; y++)
        {
          Vector2Int tile = new Vector2Int(x, y);
          if (Mathf.Abs(tile.x - room.stairGridPos.x) <= 1 && Mathf.Abs(tile.y - room.stairGridPos.y) <= 1) continue;
          tiles.Add(tile);
        }
      }
    }

    return tiles;
  }

  /// <summary>
  /// Dò tìm tọa độ hợp lệ trên NavMesh và kiểm tra chênh lệch cao độ chuẩn tầng
  /// </summary>
  private bool TryFindNavMeshPosition(Vector3 origin, float expectedElevation, float maxElevationDelta, out Vector3 validPos)
  {
    validPos = Vector3.zero;

    if (NavMesh.SamplePosition(origin, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
    {
      if (Mathf.Abs(hit.position.y - expectedElevation) <= maxElevationDelta)
      {
        validPos = hit.position;
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Kiểm tra xem vị trí có an toàn cho từng thành viên:
  /// - Không đè lên các quái khác đã sinh (minDistanceBetweenMembers)
  /// - Không bị kẹt bên trong props/đạo cụ tĩnh (Physics.CheckSphere)
  /// </summary>
  private bool IsPositionSafeForMember(Vector3 candidatePos, List<Vector3> existingPositions)
  {
    // 1. Kiểm tra khoảng cách với các quái khác
    foreach (var existing in existingPositions)
    {
      if (Vector3.Distance(candidatePos, existing) < minDistanceBetweenMembers)
      {
        return false;
      }
    }

    // 2. Kiểm tra va chạm với đạo cụ kiến trúc / props tĩnh
    Vector3 checkOrigin = candidatePos + Vector3.up * 0.8f;
    if (Physics.CheckSphere(checkOrigin, 0.45f, obstacleCheckMask, QueryTriggerInteraction.Ignore))
    {
      return false;
    }

    return true;
  }

  /// <summary>
  /// Khởi tạo quái vật và nạp vào hierarchy
  /// </summary>
  private GameObject InstantiateEnemy(GameObject prefab, Vector3 position, Quaternion rotation, string enemyName)
  {
    // Sinh quái trực tiếp ở Root Scene (giống CombatTester) để transform.root chính là con quái
    // Đảm bảo tương thích 100% với TargetLockSystem, WeaponHitbox, Linecast tầm nhìn
    GameObject enemy = Instantiate(prefab, position, rotation);
    enemy.name = enemyName;

    NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
    if (agent != null)
    {
      agent.Warp(position);
    }

    activeEnemies.Add(enemy);
    return enemy;
  }

  private void ShuffleList<T>(List<T> list)
  {
    for (int i = list.Count - 1; i > 0; i--)
    {
      int randomIndex = Random.Range(0, i + 1);
      (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
    }
  }

  private void OnDrawGizmosSelected()
  {
    // Vẽ tâm các cụm quái (Vòng tròn màu tím)
    Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.8f);
    foreach (var anchor in debugSquadAnchors)
    {
      Gizmos.DrawWireSphere(anchor + Vector3.up * 0.3f, squadClusterRadius);
      Gizmos.DrawCube(anchor + Vector3.up * 0.3f, new Vector3(0.5f, 0.5f, 0.5f));
    }

    // Vẽ vị trí quái cận chiến (Màu xanh dương)
    Gizmos.color = Color.cyan;
    foreach (var p in debugMeleePoints)
    {
      Gizmos.DrawWireSphere(p + Vector3.up * 0.9f, 0.45f);
    }

    // Vẽ vị trí quái tầm xa (Màu vàng/cam)
    Gizmos.color = new Color(1f, 0.6f, 0f);
    foreach (var p in debugRangedPoints)
    {
      Gizmos.DrawWireSphere(p + Vector3.up * 0.9f, 0.55f);
    }
  }
}
