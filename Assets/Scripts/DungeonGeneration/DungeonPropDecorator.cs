using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống trang trí đạo cụ tĩnh, cụm kiến trúc tàn tích và chiếu sáng Dungeon (Bước 4 - Tối ưu hóa & Đa dạng hóa cao)
/// Điều phối toàn bộ quá trình trang trí qua các sub-modules partial classes:
/// - DungeonPropDecorator.Spatial.cs: Tính toán vùng cấm forbiddenTiles, tường đặc, xoay mảng, banner colors.
/// - DungeonPropDecorator.Templates.cs: Cụm kiến trúc tàn tích (8 Wall) & tiểu cảnh (5 Props), đo Bounding Box.
/// - DungeonPropDecorator.Lighting.cs: Đuốc tường, đuốc sàn, đuốc hành lang, quản lý culling.
/// - DungeonPropDecorator.Mezzanine.cs: Trang trí cấu trúc gác lửng tầng 2 (khớp chuẩn 8m).
/// - DungeonPropDecorator.Furniture.cs: Nội thất tường, bàn ghế trung tâm, góc phòng, rải rác sàn.
/// - DungeonStaticPropCombiner.cs: Gộp Mesh tĩnh theo Material và tối ưu hóa PhysX Colliders.
/// </summary>
public partial class DungeonPropDecorator : MonoBehaviour
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

  [Header("4. Cụm Kiến Trúc Tàn Tích & Tiểu Cảnh (Templates)")]
  [Tooltip("Bật/Tắt sinh các cụm tường tàn tích (Wall Templates: chữ L, cổng vòm, đống đổ nát, shrine)")]
  public bool enableWallTemplates = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ phòng đủ điều kiện được sinh cụm tường tàn tích")]
  public float wallTemplateChance = 0.85f;

  [Tooltip("Bật/Tắt sinh các cụm tiểu cảnh đạo cụ (Prop Templates: trại, tiệc tàn, giả kim, đồ bị cướp)")]
  public bool enablePropTemplates = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ phòng đủ điều kiện được sinh cụm tiểu cảnh đạo cụ")]
  public float propTemplateChance = 0.80f;

  [Header("5. Góc Phòng & Phế Tích (Corner & Rubble Density)")]
  [Tooltip("Bật/Tắt đồ trang trí tại các góc phòng")]
  public bool enableCornerProps = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện đồ trang trí tại 4 góc phòng (đá vụn hoặc thùng gỗ)")]
  public float cornerDecorChance = 0.85f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ ưu tiên sinh đống đá đổ vỡ (Rubble) so với thùng gỗ (Crates) ở các góc phòng")]
  public float rubbleRatio = 0.45f;

  [Header("6. Nội Thất Sát Tường & Bàn Ghế (Furniture Density)")]
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

  [Header("7. Rải Rác Đồ Vật Trên Sàn (Floor Scatter Props)")]
  [Tooltip("Bật/Tắt rải rác đồ vật ngẫu nhiên trên mặt sàn phòng (thùng gỗ, rương, keg, chai lọ, nến, đá vụn)")]
  public bool enableFloorScatter = true;

  [Range(0f, 0.5f)]
  [Tooltip("Mật độ đồ vật rải rác tự do trên sàn phòng")]
  public float floorScatterDensity = 0.15f;

  [Range(0f, 0.25f)]
  [Tooltip("Tỉ lệ xuất hiện đá vụn nhỏ trên sàn (đặt thấp ~0.05 để tránh các khối đá to choán chỗ)")]
  public float floorRubbleChance = 0.05f;

  [Header("8. Gác Lửng Tầng 2 & Hành Lang")]
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

  [Header("9. Tối Ưu Hiệu Năng (Performance Optimization)")]
  [Tooltip("Tự động gộp mesh toàn bộ đồ tĩnh trong mỗi phòng thành 1 Draw Call duy nhất")]
  public bool combineStaticProps = true;

  [Tooltip("Tự động tắt Point Light và Particle (khói, lửa) ở xa người chơi để đảm bảo FPS luôn cao (100-120+)")]
  public bool enableLightCulling = true;

  [Range(10f, 150f)]
  [Tooltip("Tầm kích hoạt ánh sáng (mét): Các ngọn đuốc/nến trong bán kính này tính từ Player sẽ bật sáng và phát khói/lửa, ở xa hơn sẽ tự tắt để tối ưu triệt để FPS. Với map 300m, khuyên dùng 70 - 90m để các phòng lân cận luôn sáng sẵn.")]
  public float lightCullingDistance = 80f;

  [Range(50f, 500f)]
  [Tooltip("Tự động đồng bộ tầm nhìn xa Camera Far Clip Plane (mét) cho hầm ngục. Với map 300m, 200m là lý tưởng để không thấy khoảng không.")]
  public float cameraFarClipPlane = 200f;

  [Header("Prefabs Mẫu Kiến Trúc & Tiểu Cảnh (Templates)")]
  public List<GameObject> wallTemplatePrefabs = new List<GameObject>();
  public List<GameObject> propTemplatePrefabs = new List<GameObject>();

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
  private Dictionary<GameObject, Bounds> templateBoundsCache = new Dictionary<GameObject, Bounds>();

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
      activeLightOptimizer.UpdateLightsImmediate(true);
    }
  }

  public void EnsurePrefabsLoaded()
  {
#if UNITY_EDITOR
    if (wallTorchPrefab == null || floorTorchPrefab == null || bannerPrefabs.Count == 0 || crateBoxPrefabs.Count == 0 || wallTemplatePrefabs.Count == 0 || propTemplatePrefabs.Count == 0)
    {
      LoadAllPrefabsFromProject();
    }
#endif
  }

#if UNITY_EDITOR
  /// <summary>
  /// Tự động tìm đường dẫn thư mục dungeon, hỗ trợ cả Assets/Prefabs/Dungeon và Assets/Enviro Prefabs/Dungeon
  /// </summary>
  private static string ResolveDungeonFolder(string subPath)
  {
    string newPath = $"Assets/Prefabs/Dungeon/{subPath}";
    if (UnityEditor.AssetDatabase.IsValidFolder(newPath)) return newPath;

    string oldPath = $"Assets/Enviro Prefabs/Dungeon/{subPath}";
    if (UnityEditor.AssetDatabase.IsValidFolder(oldPath)) return oldPath;

    string folderName = System.IO.Path.GetFileName(subPath);
    string[] foundGuids = UnityEditor.AssetDatabase.FindAssets($"{folderName} t:Folder");
    foreach (string g in foundGuids)
    {
      string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
      if (p.EndsWith(subPath) && UnityEditor.AssetDatabase.IsValidFolder(p))
        return p;
    }

    return newPath;
  }

  [ContextMenu("Tải lại toàn bộ Prefabs Trang trí")]
  public void LoadAllPrefabsFromProject()
  {
    string folder = ResolveDungeonFolder("DungeonPrefabs");
    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { folder });

    if (guids == null || guids.Length == 0)
    {
      Debug.LogWarning($"<color=yellow>[DungeonPropDecorator]</color> Không tìm thấy prefabs nào trong thư mục: {folder}. Vui lòng kiểm tra lại đường dẫn Assets/Prefabs/Dungeon.");
      return;
    }

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

    // Tự động quét và nạp toàn bộ Templates từ 2 thư mục Walls và Props
    wallTemplatePrefabs.Clear();
    propTemplatePrefabs.Clear();

    string wallTemplatesFolder = ResolveDungeonFolder("Templates/Walls");
    if (UnityEditor.AssetDatabase.IsValidFolder(wallTemplatesFolder))
    {
      string[] wallGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { wallTemplatesFolder });
      foreach (string guid in wallGuids)
      {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) wallTemplatePrefabs.Add(prefab);
      }
    }

    string propTemplatesFolder = ResolveDungeonFolder("Templates/Props");
    if (UnityEditor.AssetDatabase.IsValidFolder(propTemplatesFolder))
    {
      string[] propGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { propTemplatesFolder });
      foreach (string guid in propGuids)
      {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) propTemplatePrefabs.Add(prefab);
      }
    }

    templateBoundsCache.Clear();
    Debug.Log($"<color=green>[DungeonPropDecorator]</color> Tải tự động thành công: <b>{guids.Length}</b> prefabs thường, <b>{wallTemplatePrefabs.Count}</b> wall templates, <b>{propTemplatePrefabs.Count}</b> prop templates từ '{folder}'.");
  }
#endif

  /// <summary>
  /// Khởi tạo và phân bổ toàn bộ đạo cụ trang trí, cụm templates và nguồn sáng cho hầm ngục
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

    // 3. GẮN BỘ TỐI ƯU HÓA ÁNH SÁNG VÀ HIỆU ỨNG HẠT (LIGHT & PARTICLE CULLER)
    if (enableLightCulling)
    {
      activeLightOptimizer = propsRoot.AddComponent<DungeonLightOptimizer>();
      activeLightOptimizer.maxLightDistance = lightCullingDistance;
      activeLightOptimizer.cullParticles = true;
      activeLightOptimizer.RefreshLights();
    }

    // 4. TỰ ĐỘNG TẮT SHADOW CASTER CHO DIRECTIONAL LIGHT CƯỜNG ĐỘ 0 ĐỂ GIẢI PHÓNG 4 SHADOW PASSES
    Light[] sceneLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
    foreach (var l in sceneLights)
    {
      if (l != null && l.type == LightType.Directional && l.intensity <= 0.01f)
      {
        l.shadows = LightShadows.None;
        l.enabled = false;
        Debug.Log("<color=green>[DungeonPropDecorator]</color> Đã vô hiệu hóa Shadow Caster Pass cho Directional Light cường độ 0 (giải phóng 4-6ms GPU).");
      }
    }

    // 5. ĐỒNG BỘ TẦM NHÌN XA CỦA CAMERA CHÍNH (FAR CLIP PLANE)
    if (cameraFarClipPlane > 10f && Camera.main != null)
    {
      Camera.main.farClipPlane = cameraFarClipPlane;
      Debug.Log($"<color=green>[DungeonPropDecorator]</color> Đã đồng bộ Camera Far Clip Plane: {cameraFarClipPlane}m");
    }

    Debug.Log("<color=cyan>[DungeonPropDecorator]</color> Hoàn tất Bước 4: Trang trí Props tĩnh, Templates & Hệ thống chiếu sáng tùy chỉnh!");
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

    // 1.5. CỤM TƯỜNG TÀN TÍCH (WALL TEMPLATES) & TIỂU CẢNH ĐẠO CỤ (PROP TEMPLATES)
    // Đặt các cụm kiến trúc và tiểu cảnh trước, chiếm dụng các ô tương ứng vào forbiddenTiles
    DecorateRoomTemplates(room, tempStaticProps.transform, lightsGroup, forbiddenTiles, theme, data, tileSize);

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

    // 8. GỘP MESH TOÀN BỘ PROPS TĨNH VÀ TEMPLATES TRONG PHÒNG THÀNH 1 DRAW CALL DUY NHẤT
    if (combineStaticProps)
    {
      DungeonStaticPropCombiner.CombineStaticPropsInRoom(tempStaticProps, roomPropsObj.transform, room.id);
    }
    else
    {
      tempStaticProps.name = "StaticProps";
      Unity.AI.Navigation.NavMeshModifier navMod = tempStaticProps.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
      navMod.overrideArea = true;
      navMod.area = 1; // 1 = Not Walkable
      navMod.applyToChildren = true;
    }
  }
}
