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

  [Range(0.1f, 5.0f)]
  [Tooltip("Khoảng cách từ lưng kệ/tủ/giường đến vách tường (Tăng lên nếu muốn đồ lùi sâu hơn vào trong phòng)")]
  public float furnitureWallOffset = 0.85f;

  [Range(0.1f, 1.0f)]
  [Tooltip("Khoảng cách đặt kệ sách/tủ sát tường (ăn theo khoảng cách đồ treo tường như đuốc, cờ, khiên - mặc định 0.3m)")]
  public float shelfWallOffset = 0.3f;

  [Range(0.1f, 5.0f)]
  [Tooltip("Khoảng cách từ lưng rương báu đến vách tường (Tăng lên để rương không chạm vào vách đá)")]
  public float chestWallOffset = 0.85f;

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

  [Header("10. Thùng Gỗ Đập Được & Rương Báu (Loot & Breakables)")]
  [Tooltip("Bật/Tắt sinh thùng gỗ đập được và rương báu trong toàn bộ hầm ngục")]
  public bool enableLootAndBreakables = true;

  [Header("10.1. Tỉ Lệ Sinh Trên Templates (Gắn Socket Sẵn)")]
  [Tooltip("Bật/Tắt sinh thùng và rương tại các Socket gắn sẵn trong Templates")]
  public bool enableTemplateLoot = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện thùng gỗ tại mỗi [Loot_Barrel_Socket] trong template")]
  public float templateBarrelSpawnChance = 0.75f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện rương tại mỗi [Loot_Chest_Socket] trong template")]
  public float templateChestSpawnChance = 0.65f;

  [Header("10.2. Thùng Gỗ Đập Được Rải Rác Trên Sàn (Tầng 1 & Tầng 2)")]
  [Tooltip("Bật/Tắt sinh cụm thùng gỗ tự do trên sàn phòng")]
  public bool enableFloorBarrels = true;

  [Range(0, 5)]
  [Tooltip("Số lượng cụm thùng gỗ tối thiểu mỗi phòng")]
  public int minBarrelClustersPerRoom = 1;

  [Range(1, 8)]
  [Tooltip("Số lượng cụm thùng gỗ tối đa mỗi phòng")]
  public int maxBarrelClustersPerRoom = 3;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ cụm sinh 1 thùng đơn lẻ (Solo)")]
  public float barrelSoloChance = 0.30f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ cụm sinh 1 cặp 2 thùng đứng cạnh nhau (Pair)")]
  public float barrelPairChance = 0.50f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ cụm sinh 3 thùng chụm lại tam giác (Trio)")]
  public float barrelTrioChance = 0.20f;

  [Range(0.3f, 3.0f)]
  [Tooltip("Khoảng cách lệch tâm của mỗi thùng trong cụm Cặp 2 thùng (Pair). Khoảng cách giữa 2 thùng = 2 * spacing. Mặc định 0.65m -> 2 thùng cách nhau 1.3m)")]
  public float barrelPairSpacing = 0.65f;

  [Range(0.4f, 3.0f)]
  [Tooltip("Bán kính cụm 3 thùng hình tam giác đều (Trio). Mặc định 0.70m giúp 3 thùng cách nhau ~1.2m thoáng đãng)")]
  public float barrelTrioRadius = 0.70f;

  [Header("10.3. Rương Báu Trong Phòng (Chests Tầng 1 & Tầng 2)")]
  [Tooltip("Bật/Tắt sinh rương báu trong phòng")]
  public bool enableChests = true;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện rương ở Tầng 1 cho mỗi phòng thông thường (Phòng Treasure luôn là 100%)")]
  public float floorChestChance = 0.25f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ xuất hiện rương trên Gác Lửng Tầng 2 (Mezzanine)")]
  public float mezzanineChestChance = 0.70f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ rương sinh ra là Rương Vàng (Interactive_Chest_Gold) so với Rương Thường")]
  public float goldChestChance = 0.15f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ rương sinh ra là Rương Vàng trong Phòng Kho Báu (Treasure Room)")]
  public float treasureRoomGoldChestChance = 0.80f;

  [Range(0f, 1f)]
  [Tooltip("Tỉ lệ rương sinh ra là Rương Vàng trên Gác Lửng Tầng 2")]
  public float mezzanineGoldChestChance = 0.25f;

  [Header("Prefabs Thùng Gỗ Đập Được & Rương Báu (Loot Prefabs)")]
  public List<GameObject> breakableBarrelPrefabs = new List<GameObject>();
  public GameObject regularChestPrefab;
  public GameObject goldChestPrefab;
  [Header("Prefabs Mẫu Kiến Trúc & Tiểu Cảnh (Templates)")]
  public List<GameObject> wallTemplatePrefabs = new List<GameObject>();
  public List<GameObject> propTemplatePrefabs = new List<GameObject>();
  [Header("Prefabs Chìa Khóa Hầm Ngục & Tế Đàn (Shrine Key PCG)")]
  public GameObject dungeonKeyPrefab;
  public InfiniteLabyrinth.Inventory.ItemData dungeonKeyItemData;
  public GameObject shrineTemplatePrefab;

  [Header("Prefabs Chiếu Sáng (Lighting)")]
  public GameObject wallTorchPrefab;       // torch_wall_lit.prefab
  public GameObject floorTorchPrefab;      // torch_lit.prefab
  public GameObject bossCauldronPrefab;    // Cauldron_Fire.prefab (Vạc lửa khổng lồ rực cháy)
  public GameObject bossGatePrefab;        // BossGate.prefab (Đại Môn Phong Ấn & Cổng Sương Mù)
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
    if (wallTorchPrefab == null || floorTorchPrefab == null || bannerPrefabs.Count == 0 || crateBoxPrefabs.Count == 0 || wallTemplatePrefabs.Count == 0 || propTemplatePrefabs.Count == 0 || breakableBarrelPrefabs.Count == 0 || regularChestPrefab == null)
    {
      LoadAllPrefabsFromProject();
    }
    // Lọc bỏ triệt để các prefab tường bị nạp nhầm vào kệ props
    shelfPrefabs.RemoveAll(p => p == null || p.name.StartsWith("wall_") || p.name.Contains("wall_"));
    if (bossGatePrefab == null)
    {
      bossGatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Interactable/BossGate.prefab");
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
    if (bossCauldronPrefab == null)
    {
      bossCauldronPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/Cauldron_Fire.prefab");
    }
    if (bossGatePrefab == null)
    {
      bossGatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Interactable/BossGate.prefab");
    }

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
      else if (name.Contains("table") && !name.Equals("table_small_decorated_B", System.StringComparison.OrdinalIgnoreCase)) tablePrefabs.Add(prefab);
      else if (name.Contains("chair") || name.Contains("stool")) chairPrefabs.Add(prefab);
      else if ((name.Contains("shelf") || name.Contains("shelves")) && !name.StartsWith("wall_") && !name.Contains("wall_")) shelfPrefabs.Add(prefab);
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

    // Tự động quét và nạp toàn bộ Thùng gỗ đập được và Rương báu từ Assets/Prefabs/RPG
    breakableBarrelPrefabs.Clear();
    regularChestPrefab = null;
    goldChestPrefab = null;

    string[] rpgGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Prefabs/RPG" });
    if (rpgGuids != null && rpgGuids.Length > 0)
    {
      foreach (string g in rpgGuids)
      {
        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        string n = System.IO.Path.GetFileNameWithoutExtension(p);
        GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if (go == null) continue;

        if (n.StartsWith("Breakable_Barrel"))
        {
          breakableBarrelPrefabs.Add(go);
        }
        else if (n == "Interactive_Chest")
        {
          regularChestPrefab = go;
        }
        else if (n == "Interactive_Chest_Gold")
        {
          goldChestPrefab = go;
        }
      }
    }

    // Nạp tự động Chìa Khóa Hầm Ngục & Tế Đàn
    if (dungeonKeyPrefab == null)
    {
      dungeonKeyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Interactable/DungeonKeyPickup.prefab");
    }
    if (dungeonKeyItemData == null)
    {
      dungeonKeyItemData = UnityEditor.AssetDatabase.LoadAssetAtPath<InfiniteLabyrinth.Inventory.ItemData>("Assets/ScriptableObjects/Items/Item_Key_Dungeon.asset");
    }
    if (shrineTemplatePrefab == null)
    {
      shrineTemplatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Templates/Walls/wallset_shrine.prefab");
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
    Light[] sceneLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
    // Nếu là phòng Boss: Chuyển sang quy trình trang trí Đấu Trường Boss riêng biệt (sàn sạch, đuốc cột, không rác)
    if (room == data.bossRoom || room.type == RoomType.Boss)
    {
      DecorateBossRoom(room, propsRoot, data, tileSize);
      return;
    }

    RoomTheme theme = (RoomTheme)(Mathf.Abs(room.id) % 6);
    if (room == data.startRoom) theme = RoomTheme.GrandHall;
    if (room == data.keyRoom) theme = RoomTheme.GrandHall;

    GameObject roomPropsObj = new GameObject($"Room_{room.id}_{theme}");
    roomPropsObj.transform.SetParent(propsRoot, false);

    Transform lightsGroup = new GameObject("Lights").transform;
    lightsGroup.SetParent(roomPropsObj.transform, false);

    GameObject tempStaticProps = new GameObject("Temp_StaticProps");
    tempStaticProps.transform.SetParent(roomPropsObj.transform, false);

    // Nhóm chứa toàn bộ đạo cụ tương tác (Thùng đập được, Rương báu) - TUYỆT ĐỐI KHÔNG GỘP MESH
    Transform interactablesGroup = new GameObject("Interactables").transform;
    interactablesGroup.SetParent(roomPropsObj.transform, false);

    HashSet<Vector2Int> forbiddenTiles = BuildForbiddenTiles(room, data);
    List<GameObject> roomBanners = GetRoomBannerFamily(room.id);

    // 1. ĐUỐC TƯỜNG (2 - 4 ĐUỐC CHIẾN LƯỢC, KIỂM TRA TƯỜNG ĐẶC) & CỜ / KHIÊN TREO TƯỜNG
    DecorateRoomWalls(room, lightsGroup, tempStaticProps.transform, forbiddenTiles, roomBanners, theme, data, tileSize);

    // 1.5. CỤM TƯỜNG TÀN TÍCH (WALL TEMPLATES) & TIỂU CẢNH ĐẠO CỤ (PROP TEMPLATES)
    // Đặt các cụm kiến trúc và tiểu cảnh trước, chiếm dụng các ô tương ứng vào forbiddenTiles
    DecorateRoomTemplates(room, tempStaticProps.transform, lightsGroup, interactablesGroup, forbiddenTiles, theme, data, tileSize);

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

    // 5.5. CỤM THÙNG GỖ ĐẬP ĐƯỢC TRÊN SÀN (TẦNG 1 - Tự do rải rác: solo, pair, trio)
    if (enableLootAndBreakables && enableFloorBarrels)
    {
      DecorateBreakableBarrels(room, interactablesGroup, forbiddenTiles, tileSize);
    }

    // 5.6. RƯƠNG BÁU TẦNG 1 (Áp sát tường hoặc góc phòng, Phòng Treasure luôn có rương)
    if (enableLootAndBreakables && enableChests)
    {
      DecorateFloorChest(room, interactablesGroup, forbiddenTiles, data, tileSize);
    }

    // 6. RẢI RÁC ĐỒ VẬT TRÊN SÀN PHÒNG (FLOOR SCATTER - Thùng, hộp, rương, keg, chai lọ, nến, đuốc sàn ngẫu nhiên)
    if (enableFloorScatter)
    {
      DecorateFloorScatter(room, tempStaticProps.transform, lightsGroup, forbiddenTiles, theme, tileSize);
    }

    // 7. TRANG TRÍ GÁC LỬNG TẦNG 2 (Khớp chuẩn cao độ 8m: Cờ, khiên, kệ sách, rương, bàn ngắm cảnh lan can, đuốc sàn)
    if (room.hasMezzanine && enableMezzanineDecor)
    {
      DecorateMezzanine(room, tempStaticProps.transform, lightsGroup, interactablesGroup, forbiddenTiles, roomBanners, theme, data, tileSize);
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

  /// <summary>
  /// Quy trình trang trí chuyên biệt cho Đấu Trường Boss:
  /// - Giữ 100% lòng sàn trung tâm sạch sẽ, bằng phẳng, không bàn ghế/thùng cản trở combat.
  /// - TẦNG 1: Giếng trời ánh trăng từ vòm trần 24m chiếu thẳng xuống tâm sàn đấu (Moonlight Spotlight).
  /// - TẦNG 2: 4 Vạc lửa Cauldron_Fire rực cháy vàng cam đặt tại chân 4 cây cột chống trời (đặt trên sàn đất).
  /// - Bố trí cờ hiệu và đuốc tường cao cấp dọc 4 bức tường cao 24m.
  /// - Bố trí 2 hàng đuốc đối xứng uy nghiêm dọc đại lộ hành lang dài 12m dẫn vào phòng Boss.
  /// </summary>
  private void DecorateBossRoom(RoomData room, Transform propsRoot, DungeonData data, float tileSize)
  {
    GameObject roomPropsObj = new GameObject($"BossArena_Room_{room.id}");
    roomPropsObj.transform.SetParent(propsRoot, false);

    Transform lightsGroup = new GameObject("Lights").transform;
    lightsGroup.SetParent(roomPropsObj.transform, false);

    GameObject tempStaticProps = new GameObject("Temp_StaticProps");
    tempStaticProps.transform.SetParent(roomPropsObj.transform, false);

    HashSet<Vector2Int> forbiddenTiles = BuildForbiddenTiles(room, data);

    // Đánh dấu khu vực 3x3 quanh 4 cây cột to vào forbiddenTiles để không đặt đồ đè lên
    if (room.pillarPositions != null)
    {
      foreach (var pPos in room.pillarPositions)
      {
        int px = Mathf.FloorToInt(pPos.x / tileSize);
        int py = Mathf.FloorToInt(pPos.z / tileSize);
        for (int dx = -1; dx <= 1; dx++)
        {
          for (int dy = -1; dy <= 1; dy++)
          {
            forbiddenTiles.Add(new Vector2Int(px + dx, py + dy));
          }
        }
      }
    }

    // 1. ĐUỐC TƯỜNG & CỜ HIỆU ĐẠI SẢNH DỌC 4 BỨC TƯỜNG CAO 24M
    List<GameObject> roomBanners = GetRoomBannerFamily(room.id);
    DecorateRoomWalls(room, lightsGroup, tempStaticProps.transform, forbiddenTiles, roomBanners, RoomTheme.GrandHall, data, tileSize);

    Vector3 roomCenter = room.WorldCenter(tileSize);

    // --- TẦNG 1 ÁNH SÁNG: GIẾNG TRỜI ÁNH TRĂNG TỪ VÒM TRẦN 24M (MOONLIGHT SPOTLIGHT) ---
    if (enableLighting)
    {
      GameObject skylightObj = new GameObject("BossArena_Moonlight");
      skylightObj.transform.SetParent(lightsGroup, false);
      skylightObj.transform.position = new Vector3(roomCenter.x, room.ceilingHeight - 0.5f, roomCenter.z);
      skylightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Chiếu thẳng xuống tâm sàn

      Light skyLight = skylightObj.AddComponent<Light>();
      skyLight.type = LightType.Spot;
      skyLight.spotAngle = 72f;
      skyLight.innerSpotAngle = 45f;
      skyLight.range = room.ceilingHeight + 8f; // 24m + 8m = 32m, rọi sáng toàn bộ vùng tâm đấu trường
      skyLight.color = new Color(0.68f, 0.84f, 1.0f); // Xanh ánh trăng dịu mát (Pale Moonlight Blue)
      skyLight.intensity = 5.5f;
      skyLight.shadows = LightShadows.Soft;
    }

    // --- TẦNG 2 ÁNH SÁNG: 4 VẠC LỬA KHỔNG LỒ RỰC CHÁY TẠI CHÂN 4 CỘT CHỐNG TRỜI ---
    GameObject cauldronPrefab = bossCauldronPrefab != null ? bossCauldronPrefab : floorTorchPrefab;
    if (enableLighting && cauldronPrefab != null && room.pillarPositions != null)
    {
      foreach (var pPos in room.pillarPositions)
      {
        Vector3 dirToCenter = (roomCenter - pPos).normalized;
        // Đặt vạc lửa ngay trước chân cột chống trời, lệch về phía tâm 2.8m, nằm trên sàn đất (Y = 0)
        Vector3 cauldronPos = pPos + dirToCenter * 2.8f;
        cauldronPos.y = 0f;

        GameObject cauldronObj = Instantiate(cauldronPrefab, cauldronPos, Quaternion.LookRotation(dirToCenter), lightsGroup);
        cauldronObj.name = "BossArena_Cauldron";

        // Tự động kiểm tra và cấu hình đèn Point Light rực sáng lửa vàng cam ấm áp
        Transform lightChild = cauldronObj.transform.Find("Light");
        Light cLight = lightChild != null ? lightChild.GetComponent<Light>() : null;
        if (cLight == null && lightChild != null) cLight = lightChild.gameObject.AddComponent<Light>();
        if (cLight == null) cLight = cauldronObj.GetComponentInChildren<Light>();
        if (cLight == null)
        {
          GameObject newLightObj = new GameObject("Light");
          newLightObj.transform.SetParent(cauldronObj.transform, false);
          newLightObj.transform.localPosition = new Vector3(0f, 3.3f, 0f);
          cLight = newLightObj.AddComponent<Light>();
        }

        if (cLight != null)
        {
          cLight.type = LightType.Point;
          cLight.color = new Color(1.0f, 0.58f, 0.16f); // Vàng cam ấm áp rực lửa (Warm Amber Fire)
          // Đấu trường 120m x 120m cần bán kính 35m và cường độ 18 để ngọn lửa rực sáng lung linh khắp sàn đấu
          if (cLight.range < 30f) cLight.range = 35.0f;
          if (cLight.intensity < 10f) cLight.intensity = 18.0f;
        }

        // Đảm bảo có CapsuleCollider ôm sát thân vạc
        BoxCollider oldBc = cauldronObj.GetComponent<BoxCollider>();
        if (oldBc != null) Destroy(oldBc);

        CapsuleCollider cc = cauldronObj.GetComponent<CapsuleCollider>();
        if (cc == null)
        {
          cc = cauldronObj.AddComponent<CapsuleCollider>();
          cc.radius = 1.25f;
          cc.height = 3.2f;
          cc.center = new Vector3(0f, 1.6f, 0f);
        }
        else
        {
          // Phòng ngừa trường hợp prefab chưa bọc parent mà để scale gốc to (ví dụ 200x)
          float scaleX = cauldronObj.transform.lossyScale.x;
          if (scaleX > 1.5f && cc.radius * scaleX > 5f)
          {
            cc.radius = 1.25f / scaleX;
            cc.height = 3.2f / scaleX;
            cc.center = new Vector3(0f, 1.6f / scaleX, 0f);
          }
        }

        EnsureNavMeshObstacleModifier(cauldronObj);
      }
    }

    // 2.5 TRANG TRÍ 2 HÀNG ĐUỐC DỌC ĐẠI LỘ HÀNH LANG DẪN VÀO PHÒNG BOSS
    DecorateBossAvenueCorridor(data, lightsGroup, tempStaticProps.transform, tileSize);

    // 2.6 DỰNG ĐẠI MÔN PHONG ẤN (BOSS GATE SCALE 2.5X) TẠI CỬA NGÕ PHÒNG BOSS (TASK D02-3)
    SpawnBossGate(data, room, roomPropsObj.transform, tileSize);

    // TUYỆT ĐỐI KHÔNG GỌI:
    // - DecorateRoomTemplates (không sinh tường vỡ, lều trại)
    // - DecorateRoomFurniture (không sinh bàn ghế, kệ tủ bừa bãi)
    // - DecorateRoomInteractables (không rải thùng gỗ lung tung giữa sàn đấu)
    // -> Giữ toàn bộ lòng sàn đấu trường sạch bóng, phẳng phiu 100% chuẩn Souls-like

    // 3. GỘP MESH ĐỒ TĨNH TRÊN TƯỜNG
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

    Debug.Log($"<color=cyan>[DungeonPropDecorator]</color> 🏛️ Đã trang trí hoàn tất Đấu Trường Boss (Room {room.id}, cao {room.ceilingHeight}m): Giếng trời ánh trăng, 4 vạc lửa cột chống trời, sàn sạch bóng.");
  }

  /// <summary>
  /// Bố trí 2 hàng đuốc đối xứng uy nghiêm dọc hai bên tường của đại lộ hành lang dẫn vào phòng Boss
  /// </summary>
  private void DecorateBossAvenueCorridor(DungeonData data, Transform lightsGroup, Transform staticPropsGroup, float tileSize)
  {
    if (wallTorchPrefab == null || !enableLighting) return;
    RectInt avenue = data.bossCorridor;
    if (avenue.width <= 0 || avenue.height <= 0) return;

    float halfTile = tileSize / 2f;
    int step = 4; // Cắm đuốc mỗi 4 tiles (8m) dọc hành lang

    if (avenue.height >= avenue.width)
    {
      // Đại lộ dọc: Đuốc cắm ở vách Tây (chiếu về Đông) và vách Đông (chiếu về Tây)
      int startY = avenue.y + 2;
      int endY = avenue.y + avenue.height - 2;

      for (int y = startY; y <= endY; y += step)
      {
        float worldZ = y * tileSize + halfTile;

        // Vách Tây: x = avenue.x
        float leftX = avenue.x * tileSize + 0.1f;
        Vector3 leftPos = new Vector3(leftX, 2.5f, worldZ);
        GameObject leftTorch = Instantiate(wallTorchPrefab, leftPos, Quaternion.Euler(0f, 90f, 0f), lightsGroup);
        leftTorch.name = "BossAvenue_Torch_West";

        // Vách Đông: x = avenue.x + avenue.width - 1
        float rightX = (avenue.x + avenue.width) * tileSize - 0.1f;
        Vector3 rightPos = new Vector3(rightX, 2.5f, worldZ);
        GameObject rightTorch = Instantiate(wallTorchPrefab, rightPos, Quaternion.Euler(0f, 270f, 0f), lightsGroup);
        rightTorch.name = "BossAvenue_Torch_East";
      }
    }
    else
    {
      // Đại lộ ngang: Đuốc cắm ở vách Nam và vách Bắc
      int startX = avenue.x + 2;
      int endX = avenue.x + avenue.width - 2;

      for (int x = startX; x <= endX; x += step)
      {
        float worldX = x * tileSize + halfTile;

        // Vách Nam
        float bottomZ = avenue.y * tileSize + 0.1f;
        Vector3 botPos = new Vector3(worldX, 2.5f, bottomZ);
        GameObject botTorch = Instantiate(wallTorchPrefab, botPos, Quaternion.Euler(0f, 0f, 0f), lightsGroup);
        botTorch.name = "BossAvenue_Torch_South";

        // Vách Bắc
        float topZ = (avenue.y + avenue.height) * tileSize - 0.1f;
        Vector3 topPos = new Vector3(worldX, 2.5f, topZ);
        GameObject topTorch = Instantiate(wallTorchPrefab, topPos, Quaternion.Euler(0f, 180f, 0f), lightsGroup);
        topTorch.name = "BossAvenue_Torch_North";
      }
    }
  }

  /// <summary>
  /// Đảm bảo một GameObject đạo cụ tương tác (Thùng đập được, Rương) được NavMesh nướng chính xác là vùng Not Walkable
  /// </summary>
  public static void EnsureNavMeshObstacleModifier(GameObject instance)
  {
    if (instance == null) return;
    Unity.AI.Navigation.NavMeshModifier mod = instance.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
    if (mod == null) mod = instance.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
    mod.overrideArea = true;
    mod.area = 1; // 1 = Not Walkable
    mod.applyToChildren = true;
  }

  /// <summary>
  /// Dựng Đại Môn Phong Ấn (Boss Gate scale 2.5x) tại cửa ngõ đại lộ hành lang nối vào Đấu Trường Boss.
  /// Tích hợp khóa mở bằng Chìa Khóa Hầm Ngục [Hold F 2.5s] và Cổng Sương Mù (Fog Gate) khóa chặt lối lui khi người chơi bước vào đấu trường.
  /// </summary>
  private void SpawnBossGate(DungeonData data, RoomData room, Transform roomPropsRoot, float tileSize)
  {
    if (data.bossCorridor.width <= 0 || data.bossCorridor.height <= 0) return;

    RectInt avenue = data.bossCorridor;

    // Vị trí ngưỡng cửa giữa Đại lộ hành lang và Đấu trường Boss:
    // Căn giữa theo trục X của đại lộ, Z nằm ngay tại mép Nam của phòng Boss (bounds.y)
    float gateX = (avenue.x + avenue.width / 2.0f) * tileSize;
    float gateZ = room.bounds.y * tileSize;
    Vector3 gatePos = new Vector3(gateX, 0f, gateZ);

    // Cổng quay mặt về hướng Nam (đón người chơi đi từ đại lộ hành lang tiến về phía Bắc)
    Quaternion gateRot = Quaternion.identity;

    if (bossGatePrefab == null)
    {
#if UNITY_EDITOR
      bossGatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Interactable/BossGate.prefab");
#endif
    }

    if (bossGatePrefab != null)
    {
      GameObject gateObj = Instantiate(bossGatePrefab, gatePos, gateRot, roomPropsRoot);
      gateObj.name = "BossArena_GrandGate";
      gateObj.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
      Debug.Log($"<color=cyan>[DungeonPropDecorator]</color> 🚪 Đã dựng Đại Môn Phong Ấn (Boss Gate) tại {gatePos} (Scale 2.5x, khóa bằng dungeon_key, có Fog Gate)");
    }
    else
    {
      BuildDynamicBossGate(gatePos, gateRot, roomPropsRoot);
    }
  }

  /// <summary>
  /// Khởi tạo động Đại Môn Boss dự phòng nếu prefab chưa được biên dịch kịp
  /// </summary>
  private void BuildDynamicBossGate(Vector3 gatePos, Quaternion gateRot, Transform parent)
  {
    Material textureMat = null;
    Material smokeMat = null;
    Mesh doorwayMesh = null;
    Mesh doorMesh = null;
    Mesh pillarMesh = null;
    GameObject torchPrefab = wallTorchPrefab;

#if UNITY_EDITOR
    textureMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Dungeon/DungeonPrefabs/texture.mat");
    smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Origin/Polygonal Particles/Assets/Materials/WhiteSmoke.mat")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Eric VFX Studio/Resource/Materials/FX_smoke_02.mat");
    GameObject doorwayPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/wall_doorway.prefab");
    GameObject pillarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/pillar.prefab");

    doorwayMesh = doorwayPrefab != null ? doorwayPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;
    if (doorwayMesh == null) doorwayMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Dungeon/Assets/obj/wall_doorway.obj");
    doorMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Dungeon/Assets/obj/wall_doorway_door.obj");
    pillarMesh = pillarPrefab != null ? pillarPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;
#endif

    GameObject root = new GameObject("BossArena_GrandGate");
    root.transform.SetParent(parent, false);
    root.transform.position = gatePos;
    root.transform.rotation = gateRot;
    root.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

    var navMod = root.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
    navMod.ignoreFromBuild = true;

    BoxCollider doorCol = root.AddComponent<BoxCollider>();
    doorCol.center = new Vector3(0f, 1.4f, 0f);
    doorCol.size = new Vector3(2.2f, 2.8f, 0.6f);

    UnityEngine.AI.NavMeshObstacle doorNav = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
    doorNav.center = new Vector3(0f, 1.4f, 0f);
    doorNav.size = new Vector3(2.2f, 2.8f, 0.6f);
    doorNav.carving = true;

    GameObject frameObj = new GameObject("Frame");
    frameObj.transform.SetParent(root.transform, false);
    if (doorwayMesh != null)
    {
      MeshFilter frameMf = frameObj.AddComponent<MeshFilter>();
      frameMf.sharedMesh = doorwayMesh;
      MeshRenderer frameMr = frameObj.AddComponent<MeshRenderer>();
      if (textureMat != null) frameMr.sharedMaterial = textureMat;
    }

    if (pillarMesh != null)
    {
      GameObject leftPillar = new GameObject("Left_Pillar");
      leftPillar.transform.SetParent(frameObj.transform, false);
      leftPillar.transform.localPosition = new Vector3(-2.1f, 0f, 0f);
      MeshFilter lpMf = leftPillar.AddComponent<MeshFilter>();
      lpMf.sharedMesh = pillarMesh;
      MeshRenderer lpMr = leftPillar.AddComponent<MeshRenderer>();
      if (textureMat != null) lpMr.sharedMaterial = textureMat;

      GameObject rightPillar = new GameObject("Right_Pillar");
      rightPillar.transform.SetParent(frameObj.transform, false);
      rightPillar.transform.localPosition = new Vector3(2.1f, 0f, 0f);
      MeshFilter rpMf = rightPillar.AddComponent<MeshFilter>();
      rpMf.sharedMesh = pillarMesh;
      MeshRenderer rpMr = rightPillar.AddComponent<MeshRenderer>();
      if (textureMat != null) rpMr.sharedMaterial = textureMat;

      if (torchPrefab != null)
      {
        GameObject leftTorch = Instantiate(torchPrefab, leftPillar.transform);
        leftTorch.name = "Pillar_Torch";
        leftTorch.transform.localPosition = new Vector3(0f, 2.2f, -0.45f);
        leftTorch.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        GameObject rightTorch = Instantiate(torchPrefab, rightPillar.transform);
        rightTorch.name = "Pillar_Torch";
        rightTorch.transform.localPosition = new Vector3(0f, 2.2f, -0.45f);
        rightTorch.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
      }
    }

    GameObject leftHinge = new GameObject("Left_Hinge");
    leftHinge.transform.SetParent(root.transform, false);
    leftHinge.transform.localPosition = new Vector3(-1.0f, 0f, 0f);

    GameObject leftDoor = new GameObject("Left_Door_Wing");
    leftDoor.transform.SetParent(leftHinge.transform, false);
    leftDoor.transform.localPosition = new Vector3(0.5f, 0f, 0f);
    leftDoor.transform.localScale = new Vector3(0.5f, 1f, 1f);
    if (doorMesh != null)
    {
      MeshFilter ldMf = leftDoor.AddComponent<MeshFilter>();
      ldMf.sharedMesh = doorMesh;
      MeshRenderer ldMr = leftDoor.AddComponent<MeshRenderer>();
      if (textureMat != null) ldMr.sharedMaterial = textureMat;
    }

    GameObject rightHinge = new GameObject("Right_Hinge");
    rightHinge.transform.SetParent(root.transform, false);
    rightHinge.transform.localPosition = new Vector3(1.0f, 0f, 0f);

    GameObject rightDoor = new GameObject("Right_Door_Wing");
    rightDoor.transform.SetParent(rightHinge.transform, false);
    rightDoor.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
    rightDoor.transform.localScale = new Vector3(-0.5f, 1f, 1f);
    if (doorMesh != null)
    {
      MeshFilter rdMf = rightDoor.AddComponent<MeshFilter>();
      rdMf.sharedMesh = doorMesh;
      MeshRenderer rdMr = rightDoor.AddComponent<MeshRenderer>();
      if (textureMat != null) rdMr.sharedMaterial = textureMat;
    }

    GameObject interactPt = new GameObject("InteractionPoint");
    interactPt.transform.SetParent(root.transform, false);
    interactPt.transform.localPosition = new Vector3(0f, 1.2f, -1.0f);

    GameObject triggerObj = new GameObject("ArenaTrigger");
    triggerObj.transform.SetParent(root.transform, false);
    triggerObj.transform.localPosition = new Vector3(0f, 1.5f, 2.6f);

    BoxCollider trigCol = triggerObj.AddComponent<BoxCollider>();
    trigCol.isTrigger = true;
    trigCol.center = Vector3.zero;
    trigCol.size = new Vector3(4.8f, 3.5f, 2.0f);

    var arenaTrig = triggerObj.AddComponent<InfiniteLabyrinth.Interaction.BossArenaTrigger>();

    var controller = root.AddComponent<InfiniteLabyrinth.Interaction.BossGateController>();
    controller.InitializeReferences(
      leftHinge.transform, rightHinge.transform,
      doorCol, doorNav,
      null, null, null,
      interactPt.transform
    );
    arenaTrig.SetGateController(controller);

#if UNITY_EDITOR
    string prefabPath = "Assets/Prefabs/Dungeon/Interactable/BossGate.prefab";
    if (!System.IO.File.Exists(prefabPath))
    {
      string dir = System.IO.Path.GetDirectoryName(prefabPath);
      if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
      UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
      bossGatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      Debug.Log($"<color=green>[DungeonPropDecorator]</color> 💾 Đã lưu BossGate.prefab tại {prefabPath}");
    }
#endif

    Debug.Log($"<color=cyan>[DungeonPropDecorator]</color> 🚪 Đã khởi tạo hoàn chỉnh Đại Môn Boss tại {gatePos} (Scale 2.5x)");
  }
}