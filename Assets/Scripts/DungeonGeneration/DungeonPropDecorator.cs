using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống trang trí đạo cụ tĩnh và chiếu sáng Dungeon (Bước 4 - Tối ưu hóa & Đa dạng hóa cao)
/// - Căn chỉnh offset riêng cho đồ treo tường (wallMountOffset) và nội thất áp tường (furnitureWallOffset).
/// - Khắc phục triệt để đuốc sàn lún 1/2 bằng floorTorchYOffset (do pivot ở giữa thân).
/// - Tắt mặc định đuốc sàn ở cửa để không bao giờ bị xếp hàng cản lối đi (chỉ rải tự nhiên khắp phòng và chân thang).
/// - Khớp chuẩn 100% cao độ Tầng 2 (mY = room.mezzanineHeight = 8.0m) triệt tiêu hoàn toàn hiện tượng props bay lơ lửng ở 4m.
/// - Đa dạng hóa tuyệt đối toàn bộ danh mục Props (hơn 150 prefabs: hòm, thùng, keg, rương, chai lọ, đĩa chén, nến sàn, bàn ghế).
/// - Tùy chỉnh trực tiếp tầm kích hoạt ánh sáng (lightCullingDistance) trên Inspector.
/// - Gộp Mesh (Mesh Combine) toàn bộ đồ tĩnh trong mỗi phòng thành 1 Draw Call duy nhất.
/// </summary>
public class DungeonPropDecorator : MonoBehaviour
{
  public enum RoomTheme
  {
    Barracks,        // Trại lính / Phòng nghỉ ngơi (Giường, bàn nhỏ, ghế, khiên kiếm)
    ArmoryStorage,   // Kho vũ khí & quân nhu (Nhiều thùng gỗ, hộp, rương, keg)
    LibraryStudy,    // Thư viện & Bàn đọc (Kệ sách lớn, bàn dài, nến cháy, ghế)
    RuinsAbandoned,  // Phế tích hoang tàn (Đá vụn rubble, bàn vỡ, khiên gãy, nến tàn)
    GrandHall,       // Đại sảnh / Phòng nghi lễ (Bàn tiệc dài, cờ banner lộng lẫy, đuốc)
    DiningQuarters   // Phòng ăn (Bàn ăn, ghế, đĩa thức ăn, chai lọ)
  }

  [Header("1. Vị Trí Gắn Tường & Áp Tường (Wall Offsets)")]
  [Range(0.1f, 0.6f)]
  [Tooltip("Khoảng cách đẩy đồ treo tường (đuốc, cờ, khiên) ra khỏi tâm tường để không bị lún vào trong vách đá")]
  public float wallMountOffset = 0.3f;

  [Range(0.1f, 0.8f)]
  [Tooltip("Khoảng cách từ lưng kệ/tủ/giường đến vách tường (giúp đồ gỗ áp sát tường tự nhiên, không nhô ra quá xa)")]
  public float furnitureWallOffset = 0.35f;

  [Header("2. Hệ Thống Chiếu Sáng (Lighting)")]
  [Tooltip("Bật/Tắt sinh đuốc trong toàn hầm ngục")]
  public bool enableLighting = true;

  [Tooltip("Bật/Tắt sinh đuốc đứng sàn (Floor Torches)")]
  public bool enableFloorTorches = true;

  [Range(0f, 1.5f)]
  [Tooltip("Độ nâng trục Y cho đuốc sàn (khắc phục hiện tượng pivot ở giữa khiến chân đế lún 1/2 xuống sàn)")]
  public float floorTorchYOffset = 0.5f;

  [Tooltip("Bật/Tắt đặt đuốc sàn cố định ở 2 bên cửa ra vào (mặc định TẮT để không xếp hàng cản trở lối đi)")]
  public bool placeDoorwayFloorTorches = false;

  [Range(1, 6)]
  [Tooltip("Số đuốc đứng sàn cố định tối đa mỗi phòng (ở chân cầu thang)")]
  public int maxFloorTorchesPerRoom = 2;

  [Tooltip("Bật/Tắt rải đuốc sàn ngẫu nhiên trong phòng (tầng 1 & tầng 2)")]
  public bool enableRandomFloorTorches = true;

  [Range(0f, 0.15f)]
  [Tooltip("Tỉ lệ xuất hiện đuốc sàn ngẫu nhiên trên sàn tầng 1 và tầng 2 khi khám phá")]
  public float randomFloorTorchChance = 0.04f;

  [Range(1, 8)]
  [Tooltip("Số lượng đuốc sàn rải rác tối đa trong mỗi phòng")]
  public int maxRandomFloorTorches = 3;

  [Range(1, 8)]
  [Tooltip("Số ngọn đuốc tường tối đa trong một phòng")]
  public int maxTorchesPerRoom = 4;

  [Range(3f, 15f)]
  [Tooltip("Tầm chiếu sáng của đuốc (mét)")]
  public float torchRange = 6.5f;

  [Range(1f, 10f)]
  [Tooltip("Cường độ sáng của đuốc")]
  public float torchIntensity = 4.5f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ thắp nến trên mặt bàn")]
  public float candleDensity = 0.75f;

  [Header("3. Đồ Treo Tường & Độ Cao Cờ (Wall Decor & Banners)")]
  [Tooltip("Bật/Tắt cờ hiệu treo tường")]
  public bool enableBanners = true;

  [Range(0f, 1f)]
  [Tooltip("Mật độ xuất hiện cờ hiệu trên các mảng tường trống")]
  public float bannerDensity = 0.65f;

  [Range(1.8f, 3.0f)]
  [Tooltip("Độ cao tối thiểu của cờ hiệu")]
  public float minBannerHeight = 2.2f;

  [Range(2.5f, 4.0f)]
  [Tooltip("Độ cao tối đa của cờ hiệu (tạo sự ngẫu nhiên cao thấp sinh động)")]
  public float maxBannerHeight = 3.2f;

  [Tooltip("Bật/Tắt khiên kiếm trang trí trên tường")]
  public bool enableWallWeapons = true;

  [Range(0f, 1f)]
  [Tooltip("Mật độ xuất hiện khiên kiếm trên tường")]
  public float wallWeaponDensity = 0.5f;

  [Header("4. Góc Phòng & Phế Tích (Corner & Rubble Density)")]
  [Tooltip("Bật/Tắt đồ trang trí tại các góc phòng")]
  public bool enableCornerProps = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện đồ trang trí tại 4 góc phòng (đá vụn hoặc thùng gỗ)")]
  public float cornerDecorChance = 0.85f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ ưu tiên sinh đống đá đổ vỡ (Rubble) so với thùng gỗ (Crates) ở các góc phòng")]
  public float rubbleRatio = 0.45f;

  [Header("5. Nội Thất Sát Tường & Bàn Ghế (Furniture Density)")]
  [Tooltip("Bật/Tắt đồ nội thất sát tường (giường, kệ sách, rương, bàn nhỏ)")]
  public bool enableWallFurniture = true;

  [Range(0f, 1f)]
  [Tooltip("Mật độ đồ nội thất sát các vách tường trống")]
  public float wallFurnitureChance = 0.65f;

  [Tooltip("Bật/Tắt bàn ghế trung tâm cho các phòng lớn")]
  public bool enableCenterTables = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ phòng lớn có bàn ghế ở giữa phòng")]
  public float centerTableChance = 0.85f;

  [Header("6. Rải Rác Đồ Vật Trên Sàn (Floor Scatter Props)")]
  [Tooltip("Bật/Tắt rải rác đồ vật ngẫu nhiên trên mặt sàn phòng (thùng gỗ, rương, keg, chai lọ, nến, đá vụn)")]
  public bool enableFloorScatter = true;

  [Range(0f, 0.5f)]
  [Tooltip("Mật độ đồ vật rải rác tự do trên sàn phòng")]
  public float floorScatterDensity = 0.15f;

  [Range(0f, 0.25f)]
  [Tooltip("Tỉ lệ xuất hiện đá vụn nhỏ trên sàn (đặt thấp ~0.05 để tránh các khối đá to choán chỗ)")]
  public float floorRubbleChance = 0.05f;

  [Header("7. Gác Lửng Tầng 2 & Hành Lang")]
  [Tooltip("Bật/Tắt trang trí tầng 2 (gác lửng)")]
  public bool enableMezzanineDecor = true;

  [Range(4, 24)]
  [Tooltip("Số lượng đồ nội thất và rải rác tối đa trên sàn gác lửng tầng 2")]
  public int maxMezzanineProps = 12;

  [Tooltip("Bật/Tắt cờ hiệu và khiên kiếm trên vách tường tầng 2")]
  public bool enableMezzanineWallDecor = true;

  [Tooltip("Bật/Tắt đuốc dẫn lối dọc hành lang")]
  public bool enableCorridorLighting = true;

  [Range(4, 16)]
  [Tooltip("Khoảng cách giữa các ngọn đuốc hành lang (số ô tile)")]
  public int corridorTorchSpacing = 8;

  [Header("8. Tối Ưu Hiệu Năng (Performance Optimization)")]
  [Tooltip("Tự động gộp mesh toàn bộ đồ tĩnh trong mỗi phòng thành 1 Draw Call duy nhất")]
  public bool combineStaticProps = true;

  [Tooltip("Tự động tắt Point Light ở xa người chơi để đảm bảo FPS luôn cao")]
  public bool enableLightCulling = true;

  [Range(10f, 80f)]
  [Tooltip("Tầm kích hoạt ánh sáng (mét): Các ngọn đuốc/nến trong bán kính này tính từ Player sẽ bật sáng, ở xa hơn sẽ tự tắt để tối ưu FPS")]
  public float lightCullingDistance = 28f;

  [Header("Prefabs Chiếu Sáng (Lighting)")]
  public GameObject wallTorchPrefab;       // torch_wall_lit.prefab
  public GameObject floorTorchPrefab;      // torch_lit.prefab
  public List<GameObject> candlePrefabs = new List<GameObject>();

  [Header("Prefabs Đồ Treo Tường (Wall Decor)")]
  public List<GameObject> bannerPrefabs = new List<GameObject>();
  public List<GameObject> wallWeaponPrefabs = new List<GameObject>();

  [Header("Prefabs Đồ Góc & Rác Sàn (Corner & Rubble)")]
  public List<GameObject> rubblePrefabs = new List<GameObject>();
  public List<GameObject> crateBoxPrefabs = new List<GameObject>();

  [Header("Prefabs Nội Thất & Bàn Ghế (Furniture)")]
  public List<GameObject> tablePrefabs = new List<GameObject>();
  public List<GameObject> chairPrefabs = new List<GameObject>();
  public List<GameObject> shelfPrefabs = new List<GameObject>();
  public List<GameObject> bedPrefabs = new List<GameObject>();
  public List<GameObject> tabletopPrefabs = new List<GameObject>();

  private DungeonLightOptimizer activeLightOptimizer;

  private void Awake()
  {
    EnsurePrefabsLoaded();
  }

  private void Update()
  {
    // Đồng bộ trực tiếp khoảng cách culling khi kéo slider trong Inspector lúc Play mode
    if (activeLightOptimizer != null && Mathf.Abs(activeLightOptimizer.maxLightDistance - lightCullingDistance) > 0.05f)
    {
      activeLightOptimizer.maxLightDistance = lightCullingDistance;
      activeLightOptimizer.UpdateLightsImmediate();
    }
  }

  public void EnsurePrefabsLoaded()
  {
#if UNITY_EDITOR
    if (wallTorchPrefab == null || floorTorchPrefab == null || bannerPrefabs.Count == 0 || crateBoxPrefabs.Count == 0)
    {
      LoadAllPrefabsFromProject();
    }
#endif
  }

#if UNITY_EDITOR
  [ContextMenu("Tải lại toàn bộ Prefabs Trang trí")]
  public void LoadAllPrefabsFromProject()
  {
    string folder = "Assets/Enviro Prefabs/Dungeon/DungeonPrefabs";
    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { folder });

    wallTorchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/torch_wall_lit.prefab");
    floorTorchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/torch_lit.prefab");

    candlePrefabs.Clear();
    bannerPrefabs.Clear();
    wallWeaponPrefabs.Clear();
    rubblePrefabs.Clear();
    crateBoxPrefabs.Clear();
    tablePrefabs.Clear();
    chairPrefabs.Clear();
    shelfPrefabs.Clear();
    bedPrefabs.Clear();
    tabletopPrefabs.Clear();

    foreach (string guid in guids)
    {
      string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
      string name = System.IO.Path.GetFileNameWithoutExtension(path);
      GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (prefab == null) continue;

      if (name.Contains("candle")) candlePrefabs.Add(prefab);
      else if (name.Contains("banner") && !name.Contains("shield")) bannerPrefabs.Add(prefab);
      else if (name.Contains("sword") || name.Contains("shield")) wallWeaponPrefabs.Add(prefab);
      else if (name.Contains("rubble")) rubblePrefabs.Add(prefab);
      else if (name.Contains("box") || name.Contains("crate") || name.Contains("barrel_small_stack") || name.Contains("keg") || name.Contains("trunk")) crateBoxPrefabs.Add(prefab);
      else if (name.Contains("table")) tablePrefabs.Add(prefab);
      else if (name.Contains("chair") || name.Contains("stool")) chairPrefabs.Add(prefab);
      else if (name.Contains("shelf") || name.Contains("shelves")) shelfPrefabs.Add(prefab);
      else if (name.Contains("bed")) bedPrefabs.Add(prefab);
      else if (name.Contains("bottle") || name.Contains("plate")) tabletopPrefabs.Add(prefab);
    }

    Debug.Log($"<color=green>[DungeonPropDecorator]</color> Tải tự động thành công: <b>{guids.Length}</b> prefabs từ thư mục DungeonPrefabs.");
  }
#endif

  /// <summary>
  /// Khởi tạo và phân bổ toàn bộ đạo cụ trang trí và nguồn sáng cho hầm ngục
  /// </summary>
  public void DecorateDungeon(DungeonData data, Transform dungeonParent, float tileSize)
  {
    if (data == null || dungeonParent == null) return;
    EnsurePrefabsLoaded();

    GameObject propsRoot = new GameObject("Dungeon_Props");
    propsRoot.transform.SetParent(dungeonParent, false);

    // 1. TRANG TRÍ TỪNG CĂN PHÒNG THEO CHỦ ĐỀ
    if (data.roomDataList != null)
    {
      foreach (var room in data.roomDataList)
      {
        DecorateRoom(room, propsRoot.transform, data, tileSize);
      }
    }

    // 2. CHIẾU SÁNG DỌC CÁC HÀNH LANG (CORRIDORS - Kiểm tra vách tường đặc triệt tiêu đuốc lơ lửng)
    if (enableLighting && enableCorridorLighting)
    {
      DecorateCorridors(data, propsRoot.transform, tileSize);
    }

    // 3. GẮN BỘ TỐI ƯU HÓA ÁNH SÁNG DỰA TRÊN KHOẢNG CÁCH (LIGHT CULLER)
    if (enableLightCulling)
    {
      activeLightOptimizer = propsRoot.AddComponent<DungeonLightOptimizer>();
      activeLightOptimizer.maxLightDistance = lightCullingDistance;
      activeLightOptimizer.RefreshLights();
    }

    Debug.Log("<color=cyan>[DungeonPropDecorator]</color> Hoàn tất Bước 4: Trang trí Props tĩnh & Hệ thống chiếu sáng tùy chỉnh!");
  }

  private void DecorateRoom(RoomData room, Transform propsRoot, DungeonData data, float tileSize)
  {
    RoomTheme theme = (RoomTheme)(Mathf.Abs(room.id) % 6);
    if (room == data.startRoom) theme = RoomTheme.GrandHall;
    else if (room == data.bossRoom) theme = RoomTheme.GrandHall;

    GameObject roomPropsObj = new GameObject($"Room_{room.id}_{theme}");
    roomPropsObj.transform.SetParent(propsRoot, false);

    Transform lightsGroup = new GameObject("Lights").transform;
    lightsGroup.SetParent(roomPropsObj.transform, false);

    GameObject tempStaticProps = new GameObject("Temp_StaticProps");
    tempStaticProps.transform.SetParent(roomPropsObj.transform, false);

    HashSet<Vector2Int> forbiddenTiles = BuildForbiddenTiles(room, data);
    List<GameObject> roomBanners = GetRoomBannerFamily(room.id);

    // 1. ĐUỐC TƯỜNG (2 - 4 ĐUỐC CHIẾN LƯỢC, KIỂM TRA TƯỜNG ĐẶC) & CỜ / KHIÊN TREO TƯỜNG
    DecorateRoomWalls(room, lightsGroup, tempStaticProps.transform, forbiddenTiles, roomBanners, theme, data, tileSize);

    // 2. ĐUỐC ĐỨNG TRÊN SÀN (FLOOR TORCHES - Chân cầu thang, không chắn cửa)
    if (enableLighting && enableFloorTorches)
    {
      DecorateFloorTorches(room, lightsGroup, forbiddenTiles, theme, data, tileSize);
    }

    // 3. ĐỒ TRANG TRÍ 4 GÓC PHÒNG (CORNER DECOR)
    if (enableCornerProps)
    {
      DecorateRoomCorners(room, tempStaticProps.transform, forbiddenTiles, theme, tileSize);
    }

    // 4. NỘI THẤT ÁP SÁT TƯỜNG (WALL FURNITURE - Áp sát tự nhiên bằng furnitureWallOffset)
    if (enableWallFurniture)
    {
      DecorateWallAdjacentTiles(room, tempStaticProps.transform, forbiddenTiles, theme, data, tileSize);
    }

    // 5. BÀN LỚN HOẶC TRANG TRÍ TÂM PHÒNG (CENTER DECOR CHO PHÒNG LỚN)
    if (enableCenterTables)
    {
      DecorateRoomCenter(room, tempStaticProps.transform, lightsGroup, forbiddenTiles, theme, tileSize);
    }

    // 6. RẢI RÁC ĐỒ VẬT TRÊN SÀN PHÒNG (FLOOR SCATTER - Thùng, hộp, rương, keg, chai lọ, nến, đuốc sàn ngẫu nhiên)
    if (enableFloorScatter)
    {
      DecorateFloorScatter(room, tempStaticProps.transform, lightsGroup, forbiddenTiles, theme, tileSize);
    }

    // 7. TRANG TRÍ GÁC LỬNG TẦNG 2 (Khớp chuẩn cao độ 8m: Cờ, khiên, kệ sách, rương, bàn ngắm cảnh lan can, đuốc sàn)
    if (room.hasMezzanine && enableMezzanineDecor)
    {
      DecorateMezzanine(room, tempStaticProps.transform, lightsGroup, forbiddenTiles, roomBanners, theme, data, tileSize);
    }

    // 8. GỘP MESH TOÀN BỘ PROPS TĨNH TRONG PHÒNG THÀNH 1 DRAW CALL DUY NHẤT
    if (combineStaticProps)
    {
      CombineStaticPropsInRoom(tempStaticProps, roomPropsObj.transform, room.id);
    }
    else
    {
      tempStaticProps.name = "StaticProps";
    }
  }

  /// <summary>
  /// Khởi tạo đuốc đứng sàn kèm nâng offset trục Y để không bị lún 1/2 thân do pivot ở giữa
  /// </summary>
  private GameObject SpawnFloorTorch(Vector3 basePos, Quaternion rot, Transform parent)
  {
    if (floorTorchPrefab == null) return null;
    Vector3 spawnPos = new Vector3(basePos.x, basePos.y + floorTorchYOffset, basePos.z);
    GameObject torch = Instantiate(floorTorchPrefab, spawnPos, rot, parent);
    Light l = torch.GetComponentInChildren<Light>();
    if (l != null)
    {
      l.range = torchRange;
      l.intensity = torchIntensity;
    }
    return torch;
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
  /// Bố trí đuốc sàn (torch_lit) tại chân cầu thang và tùy chọn 2 bên mép cửa (mặc định không chắn cửa)
  /// </summary>
  private void DecorateFloorTorches(RoomData room, Transform lightsGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, DungeonData data, float tileSize)
  {
    if (floorTorchPrefab == null) return;
    float halfTile = tileSize / 2f;
    int spawnedCount = 0;

    // 1. Chỉ đặt ở cửa ra vào nếu người dùng bật tùy chọn này (mặc định TẮT để không xếp hàng cản lối đi)
    if (placeDoorwayFloorTorches && room.doorTiles != null)
    {
      foreach (var door in room.doorTiles)
      {
        if (spawnedCount >= maxFloorTorchesPerRoom) break;

        // Chỉ tìm các ô dọc theo vách tường hai bên cánh cửa, KHÔNG lấy ô hướng vào phòng hay hành lang
        Vector2Int[] candidates = (door.x == room.bounds.x || door.x == room.bounds.x + room.bounds.width - 1)
          ? new Vector2Int[] { new Vector2Int(door.x, door.y + 1), new Vector2Int(door.x, door.y - 1) }
          : new Vector2Int[] { new Vector2Int(door.x + 1, door.y), new Vector2Int(door.x - 1, door.y) };

        foreach (var c in candidates)
        {
          if (spawnedCount >= maxFloorTorchesPerRoom) break;

          if (c.x > room.bounds.x && c.x < room.bounds.x + room.bounds.width - 1 &&
              c.y > room.bounds.y && c.y < room.bounds.y + room.bounds.height - 1 &&
              !forbiddenTiles.Contains(c))
          {
            Vector3 pos = new Vector3(c.x * tileSize + halfTile, 0f, c.y * tileSize + halfTile);
            SpawnFloorTorch(pos, Quaternion.identity, lightsGroup);
            forbiddenTiles.Add(c);
            spawnedCount++;
          }
        }
      }
    }

    // 2. Đặt đuốc ở lối vào chân cầu thang tầng lửng
    if (room.hasMezzanine && spawnedCount < maxFloorTorchesPerRoom)
    {
      Vector2Int stairBase = room.stairGridPos;
      Vector2Int sideTorchTile = (room.mezzanineSide == MezzanineWallSide.North)
        ? new Vector2Int(stairBase.x + 3, stairBase.y - 1)
        : new Vector2Int(stairBase.x + 3, stairBase.y + 1);

      if (sideTorchTile.x > room.bounds.x && sideTorchTile.x < room.bounds.x + room.bounds.width - 1 &&
          sideTorchTile.y > room.bounds.y && sideTorchTile.y < room.bounds.y + room.bounds.height - 1 &&
          !forbiddenTiles.Contains(sideTorchTile))
      {
        Vector3 pos = new Vector3(sideTorchTile.x * tileSize + halfTile, 0f, sideTorchTile.y * tileSize + halfTile);
        SpawnFloorTorch(pos, Quaternion.identity, lightsGroup);
        forbiddenTiles.Add(sideTorchTile);
        spawnedCount++;
      }
    }
  }

  /// <summary>
  /// Rải rác tự do các đồ vật trên mặt sàn phòng (đuốc sàn ngẫu nhiên, thùng gỗ, rương, chai lọ, nến; đá vụn nhỏ hạn chế)
  /// </summary>
  private void DecorateFloorScatter(RoomData room, Transform staticPropsGroup, Transform lightsGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, float tileSize)
  {
    if (!enableFloorScatter || floorScatterDensity <= 0.001f) return;

    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;
    int spawnedFloorTorches = 0;

    for (int x = b.x + 1; x < b.x + b.width - 1; x++)
    {
      for (int y = b.y + 1; y < b.y + b.height - 1; y++)
      {
        Vector2Int tile = new Vector2Int(x, y);
        if (forbiddenTiles.Contains(tile)) continue;

        if (Random.value < floorScatterDensity)
        {
          float jitterX = Random.Range(-0.25f, 0.25f);
          float jitterZ = Random.Range(-0.25f, 0.25f);
          Vector3 pos = new Vector3(x * tileSize + halfTile + jitterX, 0f, y * tileSize + halfTile + jitterZ);
          Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

          SpawnRandomFloorProp(theme, pos, rot, staticPropsGroup, lightsGroup, ref spawnedFloorTorches, maxRandomFloorTorches);
          forbiddenTiles.Add(tile);
        }
      }
    }
  }

  /// <summary>
  /// Hàm sinh prop ngẫu nhiên đa dạng phong phú cho cả Tầng 1 và Tầng 2
  /// </summary>
  private void SpawnRandomFloorProp(RoomTheme theme, Vector3 pos, Quaternion rot, Transform staticPropsGroup, Transform lightsGroup, ref int spawnedFloorTorches, int maxFloorTorches)
  {
    // 1. ĐUỐC SÀN RẢI RÁC TỰ DO (TẦNG 1 & TẦNG 2)
    if (enableLighting && enableRandomFloorTorches && spawnedFloorTorches < maxFloorTorches && Random.value < randomFloorTorchChance && floorTorchPrefab != null)
    {
      SpawnFloorTorch(pos, Quaternion.identity, lightsGroup);
      spawnedFloorTorches++;
      return;
    }

    // 2. ĐÁ VỤN NHỎ (RUBBLE_HALF) - CHỈ XUẤT HIỆN THEO TỈ LỆ NHỎ ĐÃ CẤU HÌNH (tránh các khối đá to)
    bool spawnRubble = (theme == RoomTheme.RuinsAbandoned && Random.value < 0.12f) || (Random.value < floorRubbleChance);
    if (spawnRubble && rubblePrefabs.Count > 0)
    {
      GameObject rubble = rubblePrefabs.Find(r => r.name.Contains("half")) ?? rubblePrefabs[0];
      GameObject rObj = Instantiate(rubble, pos, rot, staticPropsGroup);
      rObj.transform.localScale = Vector3.one * Random.Range(0.65f, 0.85f);
      return;
    }

    // 3. PHÂN BỔ ĐA DẠNG CÁC NHÓM ĐẠO CỤ (PROPS)
    float roll = Random.value;

    if (roll < 0.35f && crateBoxPrefabs.Count > 0)
    {
      // 35%: Hòm, thùng, rương, keg, kiện hàng (ngẫu nhiên đồng đều trong 16 loại)
      GameObject box = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
      Instantiate(box, pos, rot, staticPropsGroup);
    }
    else if (roll < 0.60f && tabletopPrefabs.Count > 0)
    {
      // 25%: Chai lọ, đĩa chén, thức ăn rơi rớt trên sàn (ngẫu nhiên trong 13 loại)
      GameObject item = tabletopPrefabs[Random.Range(0, tabletopPrefabs.Count)];
      Instantiate(item, pos, rot, staticPropsGroup);
    }
    else if (roll < 0.75f && candlePrefabs.Count > 0)
    {
      // 15%: Nến sàn (nến đơn, cụm 3 cây, nến chảy sáp, nến thắp sáng)
      GameObject candle = candlePrefabs[Random.Range(0, candlePrefabs.Count)];
      Transform parentGroup = (candle.GetComponentInChildren<Light>() != null) ? lightsGroup : staticPropsGroup;
      Instantiate(candle, pos, rot, parentGroup);
    }
    else if (roll < 0.90f && chairPrefabs.Count > 0)
    {
      // 15%: Ghế tựa hoặc ghế đẩu (ngẫu nhiên)
      GameObject chair = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
      Instantiate(chair, pos, rot, staticPropsGroup);
    }
    else if (tablePrefabs.Count > 0)
    {
      // 10%: Bàn nhỏ hoặc bàn gãy
      List<GameObject> smallTables = tablePrefabs.FindAll(t => t.name.Contains("small") || t.name.Contains("broken"));
      GameObject smTable = (smallTables.Count > 0) ? smallTables[Random.Range(0, smallTables.Count)] : tablePrefabs[Random.Range(0, tablePrefabs.Count)];
      Instantiate(smTable, pos, rot, staticPropsGroup);
    }
    else if (crateBoxPrefabs.Count > 0)
    {
      GameObject fallback = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
      Instantiate(fallback, pos, rot, staticPropsGroup);
    }
  }

  /// <summary>
  /// Bố trí đuốc tường & cờ/khiên treo tường (với độ cao cờ ngẫu nhiên minBannerHeight - maxBannerHeight)
  /// </summary>
  private void DecorateRoomWalls(RoomData room, Transform lightsGroup, Transform staticPropsGroup, HashSet<Vector2Int> forbiddenTiles, List<GameObject> roomBanners, RoomTheme theme, DungeonData data, float tileSize)
  {
    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;
    float wallTorchHeight = 2.2f;

    bool isLargeRoom = (b.width >= 8 || b.height >= 8 || theme == RoomTheme.GrandHall);

    // 1. ĐẶT ĐUỐC CHIẾN LƯỢC (Đảm bảo có tường đặc phía sau):
    if (enableLighting)
    {
      if (maxTorchesPerRoom >= 1)
        TryPlaceSingleWallTorch(new Vector2Int(b.x + b.width / 2, b.y + b.height - 1), 0, b, lightsGroup, forbiddenTiles, data, room, tileSize, wallTorchHeight);

      if (maxTorchesPerRoom >= 2)
        TryPlaceSingleWallTorch(new Vector2Int(b.x + b.width / 2, b.y), 1, b, lightsGroup, forbiddenTiles, data, room, tileSize, wallTorchHeight);

      if (isLargeRoom && maxTorchesPerRoom >= 3)
      {
        TryPlaceSingleWallTorch(new Vector2Int(b.x, b.y + b.height / 2), 2, b, lightsGroup, forbiddenTiles, data, room, tileSize, wallTorchHeight);
        if (maxTorchesPerRoom >= 4)
          TryPlaceSingleWallTorch(new Vector2Int(b.x + b.width - 1, b.y + b.height / 2), 3, b, lightsGroup, forbiddenTiles, data, room, tileSize, wallTorchHeight);
      }
    }

    // 2. CỜ HIỆU VÀ KHIÊN KIẾM TREO TƯỜNG (Chỉ gắn trên tường đặc, độ cao ngẫu nhiên)
    // Tường Bắc (North):
    for (int x = b.x + 2; x < b.x + b.width - 2; x += 2)
    {
      Vector2Int tile = new Vector2Int(x, b.y + b.height - 1);
      if (forbiddenTiles.Contains(tile) || !HasSolidWall(x, b.y + b.height - 1, 0, data, room)) continue;

      if (enableBanners && roomBanners != null && roomBanners.Count > 0 && Random.value < bannerDensity)
      {
        GameObject banner = roomBanners[Random.Range(0, roomBanners.Count)];
        float bannerY = Random.Range(minBannerHeight, maxBannerHeight);
        Vector3 pos = new Vector3(x * tileSize + halfTile, bannerY, (b.y + b.height) * tileSize - wallMountOffset);
        Instantiate(banner, pos, Quaternion.Euler(0, 180, 0), staticPropsGroup);
      }
    }

    // Tường Nam (South):
    for (int x = b.x + 2; x < b.x + b.width - 2; x += 2)
    {
      Vector2Int tile = new Vector2Int(x, b.y);
      if (forbiddenTiles.Contains(tile) || !HasSolidWall(x, b.y, 1, data, room)) continue;

      if (enableWallWeapons && wallWeaponPrefabs != null && wallWeaponPrefabs.Count > 0 && Random.value < wallWeaponDensity)
      {
        GameObject weapon = wallWeaponPrefabs[Random.Range(0, wallWeaponPrefabs.Count)];
        Vector3 pos = new Vector3(x * tileSize + halfTile, wallTorchHeight, b.y * tileSize + wallMountOffset);
        Instantiate(weapon, pos, Quaternion.identity, staticPropsGroup);
      }
    }

    // Tường Tây (West):
    for (int y = b.y + 2; y < b.y + b.height - 2; y += 2)
    {
      Vector2Int tile = new Vector2Int(b.x, y);
      if (forbiddenTiles.Contains(tile) || !HasSolidWall(b.x, y, 2, data, room)) continue;

      if (enableBanners && roomBanners != null && roomBanners.Count > 0 && Random.value < bannerDensity)
      {
        GameObject banner = roomBanners[Random.Range(0, roomBanners.Count)];
        float bannerY = Random.Range(minBannerHeight, maxBannerHeight);
        Vector3 pos = new Vector3(b.x * tileSize + wallMountOffset, bannerY, y * tileSize + halfTile);
        Instantiate(banner, pos, Quaternion.Euler(0, 90, 0), staticPropsGroup);
      }
    }

    // Tường Đông (East):
    for (int y = b.y + 2; y < b.y + b.height - 2; y += 2)
    {
      Vector2Int tile = new Vector2Int(b.x + b.width - 1, y);
      if (forbiddenTiles.Contains(tile) || !HasSolidWall(b.x + b.width - 1, y, 3, data, room)) continue;

      if (enableWallWeapons && wallWeaponPrefabs != null && wallWeaponPrefabs.Count > 0 && Random.value < wallWeaponDensity)
      {
        GameObject weapon = wallWeaponPrefabs[Random.Range(0, wallWeaponPrefabs.Count)];
        Vector3 pos = new Vector3((b.x + b.width) * tileSize - wallMountOffset, wallTorchHeight, y * tileSize + halfTile);
        Instantiate(weapon, pos, Quaternion.Euler(0, 270, 0), staticPropsGroup);
      }
    }
  }

  private void TryPlaceSingleWallTorch(Vector2Int targetTile, int wallSide, RectInt b, Transform lightsGroup, HashSet<Vector2Int> forbiddenTiles, DungeonData data, RoomData room, float tileSize, float yHeight)
  {
    if (wallTorchPrefab == null) return;
    float halfTile = tileSize / 2f;

    Vector2Int chosenTile = targetTile;
    bool foundSolid = false;

    int[] offsets = new int[] { 0, -1, 1, -2, 2 };
    foreach (int off in offsets)
    {
      Vector2Int candidate = targetTile;
      if (wallSide == 0 || wallSide == 1) candidate.x += off;
      else candidate.y += off;

      if (candidate.x > b.x && candidate.x < b.x + b.width - 1 &&
          candidate.y >= b.y && candidate.y < b.y + b.height)
      {
        if (!forbiddenTiles.Contains(candidate) && HasSolidWall(candidate.x, candidate.y, wallSide, data, room))
        {
          chosenTile = candidate;
          foundSolid = true;
          break;
        }
      }
    }

    if (!foundSolid) return;

    Vector3 pos = Vector3.zero;
    Quaternion rot = Quaternion.identity;

    switch (wallSide)
    {
      case 0: // North
        pos = new Vector3(chosenTile.x * tileSize + halfTile, yHeight, (b.y + b.height) * tileSize - wallMountOffset);
        rot = Quaternion.Euler(0, 180, 0);
        break;
      case 1: // South
        pos = new Vector3(chosenTile.x * tileSize + halfTile, yHeight, b.y * tileSize + wallMountOffset);
        rot = Quaternion.identity;
        break;
      case 2: // West
        pos = new Vector3(b.x * tileSize + wallMountOffset, yHeight, chosenTile.y * tileSize + halfTile);
        rot = Quaternion.Euler(0, 90, 0);
        break;
      case 3: // East
        pos = new Vector3((b.x + b.width) * tileSize - wallMountOffset, yHeight, chosenTile.y * tileSize + halfTile);
        rot = Quaternion.Euler(0, 270, 0);
        break;
    }

    GameObject torch = Instantiate(wallTorchPrefab, pos, rot, lightsGroup);
    Light l = torch.GetComponentInChildren<Light>();
    if (l != null)
    {
      l.range = torchRange;
      l.intensity = torchIntensity;
    }

    forbiddenTiles.Add(chosenTile);
  }

  /// <summary>
  /// Bố trí nội thất áp sát các vách tường trống bằng furnitureWallOffset (giường, kệ sách, rương, keg)
  /// </summary>
  private void DecorateWallAdjacentTiles(RoomData room, Transform staticPropsGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, DungeonData data, float tileSize)
  {
    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;

    // Sát tường Nam
    for (int x = b.x + 2; x < b.x + b.width - 2; x += 3)
    {
      Vector2Int tile = new Vector2Int(x, b.y);
      if (!forbiddenTiles.Contains(tile) && HasSolidWall(x, b.y, 1, data, room) && Random.value < wallFurnitureChance)
      {
        Vector3 pos = new Vector3(x * tileSize + halfTile, 0f, b.y * tileSize + furnitureWallOffset);
        SpawnThemeFurniture(theme, pos, Quaternion.identity, staticPropsGroup);
        forbiddenTiles.Add(tile);
      }
    }

    // Sát tường Bắc
    for (int x = b.x + 2; x < b.x + b.width - 2; x += 3)
    {
      Vector2Int tile = new Vector2Int(x, b.y + b.height - 1);
      if (!forbiddenTiles.Contains(tile) && HasSolidWall(x, b.y + b.height - 1, 0, data, room) && Random.value < wallFurnitureChance)
      {
        Vector3 pos = new Vector3(x * tileSize + halfTile, 0f, (b.y + b.height) * tileSize - furnitureWallOffset);
        SpawnThemeFurniture(theme, pos, Quaternion.Euler(0, 180, 0), staticPropsGroup);
        forbiddenTiles.Add(tile);
      }
    }

    // Sát tường Tây
    for (int y = b.y + 2; y < b.y + b.height - 2; y += 3)
    {
      Vector2Int tile = new Vector2Int(b.x, y);
      if (!forbiddenTiles.Contains(tile) && HasSolidWall(b.x, y, 2, data, room) && Random.value < wallFurnitureChance)
      {
        Vector3 pos = new Vector3(b.x * tileSize + furnitureWallOffset, 0f, y * tileSize + halfTile);
        SpawnThemeFurniture(theme, pos, Quaternion.Euler(0, 90, 0), staticPropsGroup);
        forbiddenTiles.Add(tile);
      }
    }

    // Sát tường Đông
    for (int y = b.y + 2; y < b.y + b.height - 2; y += 3)
    {
      Vector2Int tile = new Vector2Int(b.x + b.width - 1, y);
      if (!forbiddenTiles.Contains(tile) && HasSolidWall(b.x + b.width - 1, y, 3, data, room) && Random.value < wallFurnitureChance)
      {
        Vector3 pos = new Vector3((b.x + b.width) * tileSize - furnitureWallOffset, 0f, y * tileSize + halfTile);
        SpawnThemeFurniture(theme, pos, Quaternion.Euler(0, 270, 0), staticPropsGroup);
        forbiddenTiles.Add(tile);
      }
    }
  }

  private void DecorateRoomCorners(RoomData room, Transform staticPropsGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, float tileSize)
  {
    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;

    Vector2Int[] corners = new Vector2Int[]
    {
      new Vector2Int(b.x, b.y),
      new Vector2Int(b.x + b.width - 1, b.y),
      new Vector2Int(b.x, b.y + b.height - 1),
      new Vector2Int(b.x + b.width - 1, b.y + b.height - 1)
    };

    foreach (var c in corners)
    {
      if (forbiddenTiles.Contains(c) || Random.value > cornerDecorChance) continue;

      Vector3 pos = new Vector3(c.x * tileSize + halfTile, 0f, c.y * tileSize + halfTile);
      Quaternion rot = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

      bool pickRubble = (theme == RoomTheme.RuinsAbandoned) || (Random.value < rubbleRatio);

      if (pickRubble && rubblePrefabs.Count > 0)
      {
        GameObject rubble = rubblePrefabs[Random.Range(0, rubblePrefabs.Count)];
        Instantiate(rubble, pos, rot, staticPropsGroup);
      }
      else if (crateBoxPrefabs.Count > 0)
      {
        GameObject crate = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
        Instantiate(crate, pos, rot, staticPropsGroup);
      }
      forbiddenTiles.Add(c);
    }
  }

  private void SpawnThemeFurniture(RoomTheme theme, Vector3 pos, Quaternion rot, Transform parent)
  {
    switch (theme)
    {
      case RoomTheme.Barracks:
        if (bedPrefabs.Count > 0)
        {
          GameObject bed = bedPrefabs[Random.Range(0, bedPrefabs.Count)];
          Instantiate(bed, pos, rot, parent);
        }
        else if (crateBoxPrefabs.Count > 0)
        {
          Instantiate(crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)], pos, rot, parent);
        }
        break;

      case RoomTheme.LibraryStudy:
        if (shelfPrefabs.Count > 0)
        {
          GameObject shelf = shelfPrefabs[Random.Range(0, shelfPrefabs.Count)];
          Instantiate(shelf, pos, rot, parent);
        }
        else if (tablePrefabs.Count > 0)
        {
          Instantiate(tablePrefabs[Random.Range(0, tablePrefabs.Count)], pos, rot, parent);
        }
        break;

      case RoomTheme.ArmoryStorage:
        if (crateBoxPrefabs.Count > 0)
        {
          GameObject box = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
          Instantiate(box, pos, rot, parent);
        }
        break;

      default:
        float r = Random.value;
        if (r < 0.4f && shelfPrefabs.Count > 0)
        {
          Instantiate(shelfPrefabs[Random.Range(0, shelfPrefabs.Count)], pos, rot, parent);
        }
        else if (r < 0.7f && crateBoxPrefabs.Count > 0)
        {
          Instantiate(crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)], pos, rot, parent);
        }
        else if (tablePrefabs.Count > 0)
        {
          List<GameObject> smTables = tablePrefabs.FindAll(t => t.name.Contains("small") || t.name.Contains("medium"));
          GameObject smTable = (smTables.Count > 0) ? smTables[Random.Range(0, smTables.Count)] : tablePrefabs[Random.Range(0, tablePrefabs.Count)];
          Instantiate(smTable, pos, rot, parent);
        }
        break;
    }
  }

  private void DecorateRoomCenter(RoomData room, Transform staticPropsGroup, Transform lightsGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, float tileSize)
  {
    RectInt b = room.bounds;
    if (b.width < 6 || b.height < 6) return;
    if (Random.value > centerTableChance) return;

    Vector2Int c = room.Center;
    if (forbiddenTiles.Contains(c) || forbiddenTiles.Contains(new Vector2Int(c.x + 1, c.y)) || forbiddenTiles.Contains(new Vector2Int(c.x - 1, c.y)))
    {
      return;
    }

    float halfTile = tileSize / 2f;
    Vector3 centerPos = new Vector3(c.x * tileSize + halfTile, 0f, c.y * tileSize + halfTile);
    Quaternion tableRot = (b.width >= b.height) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;

    GameObject centerTable = null;
    if (theme == RoomTheme.RuinsAbandoned)
    {
      List<GameObject> brokenTables = tablePrefabs.FindAll(t => t.name.Contains("broken"));
      centerTable = (brokenTables.Count > 0) ? brokenTables[Random.Range(0, brokenTables.Count)] : tablePrefabs[Random.Range(0, tablePrefabs.Count)];
    }
    else if (theme == RoomTheme.GrandHall || theme == RoomTheme.DiningQuarters)
    {
      List<GameObject> banquetTables = tablePrefabs.FindAll(t => t.name.Contains("tablecloth") || t.name.Contains("long"));
      centerTable = (banquetTables.Count > 0) ? banquetTables[Random.Range(0, banquetTables.Count)] : tablePrefabs[Random.Range(0, tablePrefabs.Count)];
    }
    else
    {
      List<GameObject> medTables = tablePrefabs.FindAll(t => t.name.Contains("medium") || t.name.Contains("small"));
      centerTable = (medTables.Count > 0) ? medTables[Random.Range(0, medTables.Count)] : tablePrefabs[Random.Range(0, tablePrefabs.Count)];
    }

    if (centerTable != null)
    {
      Instantiate(centerTable, centerPos, tableRot, staticPropsGroup);
      forbiddenTiles.Add(c);
      forbiddenTiles.Add(new Vector2Int(c.x + 1, c.y));
      forbiddenTiles.Add(new Vector2Int(c.x - 1, c.y));

      // Đặt nến thắp sáng trên mặt bàn
      if (candlePrefabs.Count > 0 && Random.value < candleDensity)
      {
        GameObject candle = candlePrefabs[Random.Range(0, candlePrefabs.Count)];
        Transform parentGroup = (candle.GetComponentInChildren<Light>() != null) ? lightsGroup : staticPropsGroup;
        Instantiate(candle, centerPos + new Vector3(0, 0.85f, 0), Quaternion.identity, parentGroup);
      }

      // Kê ghế 2 bên bàn
      if (chairPrefabs.Count > 0)
      {
        Vector3 chairOffset1 = tableRot * new Vector3(0, 0, 1.1f);
        Vector3 chairOffset2 = tableRot * new Vector3(0, 0, -1.1f);

        GameObject c1 = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
        GameObject c2 = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
        Instantiate(c1, centerPos + chairOffset1, tableRot * Quaternion.Euler(0, 180, 0), staticPropsGroup);
        Instantiate(c2, centerPos + chairOffset2, tableRot, staticPropsGroup);
      }
    }
  }

  /// <summary>
  /// Trang trí gác lửng tầng 2 (Mezzanine): cờ hiệu, khiên kiếm, đuốc tường, đuốc sàn, kệ sách, rương, bàn ghế ngắm cảnh lan can
  /// Khớp chuẩn cao độ mặt sàn tầng 2 (mY = room.mezzanineHeight = 8.0m)
  /// </summary>
  private void DecorateMezzanine(RoomData room, Transform staticPropsGroup, Transform lightsGroup, HashSet<Vector2Int> forbiddenTiles, List<GameObject> roomBanners, RoomTheme theme, DungeonData data, float tileSize)
  {
    float mY = (room.mezzanineHeight > 0.1f) ? room.mezzanineHeight : 8.0f; // Cao độ thực tế của sàn tầng 2
    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;

    // 1. ĐUỐC TƯỜNG TẦNG 2
    if (enableLighting && wallTorchPrefab != null)
    {
      Vector3 tPos = (room.mezzanineSide == MezzanineWallSide.North)
        ? new Vector3((b.x + b.width / 2) * tileSize + halfTile, mY + 2.2f, (b.y + b.height) * tileSize - wallMountOffset)
        : new Vector3((b.x + b.width / 2) * tileSize + halfTile, mY + 2.2f, b.y * tileSize + wallMountOffset);
      Quaternion tRot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

      GameObject torch = Instantiate(wallTorchPrefab, tPos, tRot, lightsGroup);
      Light l = torch.GetComponentInChildren<Light>();
      if (l != null)
      {
        l.range = torchRange;
        l.intensity = torchIntensity;
      }
    }

    // 2. CỜ HIỆU & KHIÊN KIẾM TREO TƯỜNG TẦNG 2
    if (enableMezzanineWallDecor)
    {
      // Treo cờ dọc bức tường sau tầng 2
      for (int x = b.x + 2; x < b.x + b.width - 2; x += 2)
      {
        Vector2Int tile = (room.mezzanineSide == MezzanineWallSide.North)
          ? new Vector2Int(x, b.y + b.height - 1)
          : new Vector2Int(x, b.y);

        if (room.IsInMezzanine(tile.x, tile.y) && !forbiddenTiles.Contains(tile))
        {
          if (roomBanners != null && roomBanners.Count > 0 && Random.value < bannerDensity)
          {
            GameObject banner = roomBanners[Random.Range(0, roomBanners.Count)];
            float bannerY = mY + Random.Range(minBannerHeight, maxBannerHeight);
            Vector3 pos = (room.mezzanineSide == MezzanineWallSide.North)
              ? new Vector3(x * tileSize + halfTile, bannerY, (b.y + b.height) * tileSize - wallMountOffset)
              : new Vector3(x * tileSize + halfTile, bannerY, b.y * tileSize + wallMountOffset);
            Quaternion rot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            Instantiate(banner, pos, rot, staticPropsGroup);
          }
          else if (wallWeaponPrefabs != null && wallWeaponPrefabs.Count > 0 && Random.value < wallWeaponDensity)
          {
            GameObject weapon = wallWeaponPrefabs[Random.Range(0, wallWeaponPrefabs.Count)];
            Vector3 pos = (room.mezzanineSide == MezzanineWallSide.North)
              ? new Vector3(x * tileSize + halfTile, mY + 2.2f, (b.y + b.height) * tileSize - wallMountOffset)
              : new Vector3(x * tileSize + halfTile, mY + 2.2f, b.y * tileSize + wallMountOffset);
            Quaternion rot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            Instantiate(weapon, pos, rot, staticPropsGroup);
          }
        }
      }
    }

    // 3. NỘI THẤT ÁP TƯỜNG & KHU VỰC NGẮM CẢNH GẦN LAN CAN & RẢI RÁC SÀN TẦNG 2
    if (room.mezzanineTiles != null)
    {
      int placedFurniture = 0;
      int maxFurniture = Mathf.Max(2, maxMezzanineProps / 3);
      int placedScatter = 0;
      int maxScatter = maxMezzanineProps;
      int mezzanineFloorTorches = 0;
      int maxMezzanineFloorTorches = 2;

      // Sắp xếp các ô gác lửng để bố trí đồ đạc hợp lý
      foreach (var tile in room.mezzanineTiles)
      {
        if (forbiddenTiles.Contains(tile)) continue;
        if (placedFurniture + placedScatter >= maxMezzanineProps) break;

        bool isBackWall = (room.mezzanineSide == MezzanineWallSide.North)
          ? (tile.y == b.y + b.height - 1)
          : (tile.y == b.y);

        // A. Ô sát tường sau: Kê kệ sách hoặc rương/keg áp sát vách (ngẫu nhiên đa dạng)
        if (isBackWall && placedFurniture < maxFurniture)
        {
          Vector3 pos = (room.mezzanineSide == MezzanineWallSide.North)
            ? new Vector3(tile.x * tileSize + halfTile, mY, (b.y + b.height) * tileSize - furnitureWallOffset)
            : new Vector3(tile.x * tileSize + halfTile, mY, b.y * tileSize + furnitureWallOffset);
          Quaternion rot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

          if (placedFurniture % 2 == 0 && shelfPrefabs.Count > 0)
          {
            GameObject shelf = shelfPrefabs[Random.Range(0, shelfPrefabs.Count)];
            Instantiate(shelf, pos, rot, staticPropsGroup);
            placedFurniture++;
            forbiddenTiles.Add(tile);
          }
          else if (crateBoxPrefabs.Count > 0)
          {
            GameObject container = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
            Instantiate(container, pos, rot, staticPropsGroup);
            placedFurniture++;
            forbiddenTiles.Add(tile);
          }
        }
        // B. Ô gần lan can: Kê bàn nhỏ hoặc ghế nhìn xuống đại sảnh
        else if (!isBackWall && placedFurniture < maxFurniture && (tile.x % 3 == 0))
        {
          Vector3 pos = new Vector3(tile.x * tileSize + halfTile, mY, tile.y * tileSize + halfTile);

          if (placedFurniture % 2 == 0 && tablePrefabs.Count > 0)
          {
            List<GameObject> smTables = tablePrefabs.FindAll(t => t.name.Contains("small") || t.name.Contains("medium"));
            GameObject smTable = (smTables.Count > 0) ? smTables[Random.Range(0, smTables.Count)] : tablePrefabs[0];
            Instantiate(smTable, pos, Quaternion.identity, staticPropsGroup);

            if (candlePrefabs.Count > 0)
            {
              GameObject candle = candlePrefabs[Random.Range(0, candlePrefabs.Count)];
              Transform parentGroup = (candle.GetComponentInChildren<Light>() != null) ? lightsGroup : staticPropsGroup;
              Instantiate(candle, pos + new Vector3(0, 0.8f, 0), Quaternion.identity, parentGroup);
            }
            placedFurniture++;
            forbiddenTiles.Add(tile);
          }
          else if (chairPrefabs.Count > 0)
          {
            GameObject chair = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
            Quaternion rot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            Instantiate(chair, pos, rot, staticPropsGroup);
            placedFurniture++;
            forbiddenTiles.Add(tile);
          }
        }
        // C. Rải rác đồ vật nhỏ trên sàn tầng 2 (đuốc sàn, thùng, rương, chai lọ, nến)
        else if (enableFloorScatter && placedScatter < maxScatter && Random.value < floorScatterDensity * 1.5f)
        {
          float jitterX = Random.Range(-0.2f, 0.2f);
          float jitterZ = Random.Range(-0.2f, 0.2f);
          Vector3 scatterPos = new Vector3(tile.x * tileSize + halfTile + jitterX, mY, tile.y * tileSize + halfTile + jitterZ);
          Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

          SpawnRandomFloorProp(theme, scatterPos, rot, staticPropsGroup, lightsGroup, ref mezzanineFloorTorches, maxMezzanineFloorTorches);
          placedScatter++;
          forbiddenTiles.Add(tile);
        }
      }
    }
  }

  /// <summary>
  /// Chiếu sáng dọc theo các lối đi hành lang kết nối giữa các phòng (Kiểm tra tường đặc 2 bên để không bao giờ lơ lửng)
  /// </summary>
  private void DecorateCorridors(DungeonData data, Transform propsRoot, float tileSize)
  {
    if (wallTorchPrefab == null || data.corridors == null) return;

    Transform corridorLights = new GameObject("Corridor_Lights").transform;
    corridorLights.SetParent(propsRoot, false);

    float halfTile = tileSize / 2f;
    float torchY = 2.2f;

    foreach (var corr in data.corridors)
    {
      if (corr.width >= corr.height) // Hành lang ngang (chạy dọc X)
      {
        for (int x = corr.x + 3; x < corr.x + corr.width - 3; x += corridorTorchSpacing)
        {
          if (data.IsInsideAnyRoom(x, corr.y)) continue;
          if (IsNearAnyDoorway(x, corr.y, data)) continue;

          // Kiểm tra xem phía Bắc hoặc phía Nam có tường đặc không
          bool hasNorthWall = !data.IsInBounds(x, corr.y + corr.height) || data.mapGrid[x, corr.y + corr.height] == 0;
          bool hasSouthWall = !data.IsInBounds(x, corr.y - 1) || data.mapGrid[x, corr.y - 1] == 0;

          if (hasNorthWall)
          {
            Vector3 pos = new Vector3(x * tileSize + halfTile, torchY, (corr.y + corr.height) * tileSize - wallMountOffset);
            SpawnCorridorTorch(pos, Quaternion.Euler(0, 180, 0), corridorLights);
          }
          else if (hasSouthWall)
          {
            Vector3 pos = new Vector3(x * tileSize + halfTile, torchY, corr.y * tileSize + wallMountOffset);
            SpawnCorridorTorch(pos, Quaternion.identity, corridorLights);
          }
        }
      }
      else // Hành lang dọc (chạy dọc Y)
      {
        for (int y = corr.y + 3; y < corr.y + corr.height - 3; y += corridorTorchSpacing)
        {
          if (data.IsInsideAnyRoom(corr.x, y)) continue;
          if (IsNearAnyDoorway(corr.x, y, data)) continue;

          // Kiểm tra xem phía Tây hoặc phía Đông có tường đặc không
          bool hasWestWall = !data.IsInBounds(corr.x - 1, y) || data.mapGrid[corr.x - 1, y] == 0;
          bool hasEastWall = !data.IsInBounds(corr.x + corr.width, y) || data.mapGrid[corr.x + corr.width, y] == 0;

          if (hasWestWall)
          {
            Vector3 pos = new Vector3(corr.x * tileSize + wallMountOffset, torchY, y * tileSize + halfTile);
            SpawnCorridorTorch(pos, Quaternion.Euler(0, 90, 0), corridorLights);
          }
          else if (hasEastWall)
          {
            Vector3 pos = new Vector3((corr.x + corr.width) * tileSize - wallMountOffset, torchY, y * tileSize + halfTile);
            SpawnCorridorTorch(pos, Quaternion.Euler(0, 270, 0), corridorLights);
          }
        }
      }
    }
  }

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

  private void SpawnCorridorTorch(Vector3 pos, Quaternion rot, Transform parent)
  {
    GameObject torch = Instantiate(wallTorchPrefab, pos, rot, parent);
    Light l = torch.GetComponentInChildren<Light>();
    if (l != null)
    {
      l.range = torchRange;
      l.intensity = torchIntensity;
    }
  }

  /// <summary>
  /// Gộp toàn bộ Mesh của đồ tĩnh trong phòng theo Material để giảm triệt để Draw Call (1 Draw Call duy nhất)
  /// </summary>
  private void CombineStaticPropsInRoom(GameObject tempRoot, Transform targetParent, int roomId)
  {
    MeshFilter[] filters = tempRoot.GetComponentsInChildren<MeshFilter>();
    if (filters == null || filters.Length == 0)
    {
      DestroyImmediate(tempRoot);
      return;
    }

    Dictionary<Material, List<CombineInstance>> materialToCombines = new Dictionary<Material, List<CombineInstance>>();

    foreach (var mf in filters)
    {
      if (mf.sharedMesh == null) continue;
      MeshRenderer mr = mf.GetComponent<MeshRenderer>();
      if (mr == null || mr.sharedMaterial == null) continue;

      Material mat = mr.sharedMaterial;
      if (!materialToCombines.ContainsKey(mat))
      {
        materialToCombines[mat] = new List<CombineInstance>();
      }

      CombineInstance ci = new CombineInstance();
      ci.mesh = mf.sharedMesh;
      ci.transform = targetParent.worldToLocalMatrix * mf.transform.localToWorldMatrix;
      materialToCombines[mat].Add(ci);
    }

    int groupIdx = 0;
    foreach (var kvp in materialToCombines)
    {
      Material mat = kvp.Key;
      List<CombineInstance> combines = kvp.Value;
      if (combines.Count == 0) continue;

      GameObject combinedObj = new GameObject($"Room_{roomId}_StaticProps_{groupIdx++}");
      combinedObj.transform.SetParent(targetParent, false);

      Mesh combinedMesh = new Mesh();
      combinedMesh.name = $"StaticProps_Mesh_Room_{roomId}_{groupIdx}";
      combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      combinedMesh.CombineMeshes(combines.ToArray(), true, true);

      MeshFilter cmf = combinedObj.AddComponent<MeshFilter>();
      cmf.sharedMesh = combinedMesh;

      MeshRenderer cmr = combinedObj.AddComponent<MeshRenderer>();
      cmr.sharedMaterial = mat;

      MeshCollider cmc = combinedObj.AddComponent<MeshCollider>();
      cmc.sharedMesh = combinedMesh;
    }

    DestroyImmediate(tempRoot);
  }

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
