using System.Collections.Generic;
using UnityEngine;

public partial class DungeonPropDecorator
{
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

  /// <summary>
  /// Bố trí đồ trang trí tại 4 góc phòng (đá vụn hoặc thùng gỗ)
  /// </summary>
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

      // Dịch tâm vật thể nhẹ nhàng 0.20m vào phía trong lòng phòng để các góc nhọn của đá/hòm không chạm vào vách đá
      float cornerInset = 0.20f;
      float posX = (c.x == b.x) ? (c.x * tileSize + halfTile + cornerInset) : (c.x * tileSize + halfTile - cornerInset);
      float posZ = (c.y == b.y) ? (c.y * tileSize + halfTile + cornerInset) : (c.y * tileSize + halfTile - cornerInset);
      Vector3 pos = new Vector3(posX, 0f, posZ);
      Quaternion rot = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

      bool pickRubble = (theme == RoomTheme.RuinsAbandoned) || (Random.value < rubbleRatio);

      if (pickRubble && rubblePrefabs.Count > 0)
      {
        GameObject rubble = rubblePrefabs[Random.Range(0, rubblePrefabs.Count)];
        GameObject rObj = Instantiate(rubble, pos, rot, staticPropsGroup);
        rObj.transform.localScale = Vector3.one * Random.Range(0.70f, 0.85f);
      }
      else if (crateBoxPrefabs.Count > 0)
      {
        GameObject crate = crateBoxPrefabs[Random.Range(0, crateBoxPrefabs.Count)];
        Instantiate(crate, pos, rot, staticPropsGroup);
      }
      forbiddenTiles.Add(c);
    }
  }

  /// <summary>
  /// Bố trí bàn lớn hoặc cụm trang trí tâm phòng cho phòng lớn
  /// </summary>
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

  /// <summary>
  /// Rải rác các cụm thùng gỗ đập được (Breakable Barrels) trên mặt sàn Tầng 1.
  /// Thùng có thể xuất hiện đơn lẻ (Solo), theo cặp (Pair) hoặc cụm ba (Trio) tự do trên sàn.
  /// Tuyệt đối không gộp Mesh và gắn NavMeshModifier để quái không đi xuyên qua.
  /// </summary>
  private void DecorateBreakableBarrels(RoomData room, Transform interactablesGroup, HashSet<Vector2Int> forbiddenTiles, float tileSize)
  {
    if (!enableFloorBarrels || breakableBarrelPrefabs == null || breakableBarrelPrefabs.Count == 0) return;

    int clusterCount = Random.Range(minBarrelClustersPerRoom, maxBarrelClustersPerRoom + 1);
    if (clusterCount <= 0) return;

    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;

    // Thu thập tất cả các ô sàn hợp lệ trên Tầng 1
    List<Vector2Int> candidateTiles = new List<Vector2Int>();
    for (int x = b.x + 1; x < b.x + b.width - 1; x++)
    {
      for (int y = b.y + 1; y < b.y + b.height - 1; y++)
      {
        Vector2Int tile = new Vector2Int(x, y);
        if (forbiddenTiles.Contains(tile)) continue;
        if (room.hasMezzanine && room.IsInMezzanine(x, y)) continue;
        candidateTiles.Add(tile);
      }
    }

    if (candidateTiles.Count == 0) return;
    ShuffleList(candidateTiles);

    int placedClusters = 0;
    foreach (var tile in candidateTiles)
    {
      if (placedClusters >= clusterCount) break;
      if (forbiddenTiles.Contains(tile)) continue;

      Vector3 center = new Vector3(tile.x * tileSize + halfTile, 0f, tile.y * tileSize + halfTile);
      float roll = Random.value;

      if (roll < barrelSoloChance)
      {
        // 1. Cụm Solo: 1 thùng đơn lẻ với độ lệch nhẹ
        Vector3 jitter = new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject prefab = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
        GameObject barrel = Instantiate(prefab, center + jitter, rot, interactablesGroup);
        EnsureNavMeshObstacleModifier(barrel);
      }
      else if (roll < barrelSoloChance + barrelPairChance)
      {
        // 2. Cụm Pair: 1 cặp 2 thùng đứng cạnh nhau (Khoảng cách cấu hình qua Inspector barrelPairSpacing)
        float clusterAngle = Random.Range(0f, 360f);
        Quaternion clusterRot = Quaternion.Euler(0f, clusterAngle, 0f);
        float spacing = Mathf.Max(0.3f, barrelPairSpacing);

        Vector3 pos1 = center + clusterRot * new Vector3(-spacing, 0f, 0f);
        Vector3 pos2 = center + clusterRot * new Vector3(spacing, 0f, 0f);

        GameObject p1 = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
        GameObject p2 = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];

        GameObject b1 = Instantiate(p1, pos1, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), interactablesGroup);
        GameObject b2 = Instantiate(p2, pos2, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), interactablesGroup);

        EnsureNavMeshObstacleModifier(b1);
        EnsureNavMeshObstacleModifier(b2);
      }
      else
      {
        // 3. Cụm Trio: 3 thùng xếp thành hình tam giác đều (Bán kính cấu hình qua Inspector barrelTrioRadius)
        float clusterAngle = Random.Range(0f, 360f);
        Quaternion clusterRot = Quaternion.Euler(0f, clusterAngle, 0f);
        float r = Mathf.Max(0.4f, barrelTrioRadius);
        float cos30 = 0.866f;
        float sin30 = 0.5f;

        Vector3[] offsets = new Vector3[]
        {
          new Vector3(0f, 0f, r),
          new Vector3(-r * cos30, 0f, -r * sin30),
          new Vector3(r * cos30, 0f, -r * sin30)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
          Vector3 pos = center + clusterRot * offsets[i];
          GameObject p = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
          GameObject barrelObj = Instantiate(p, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), interactablesGroup);
          EnsureNavMeshObstacleModifier(barrelObj);
        }
      }

      forbiddenTiles.Add(tile);
      placedClusters++;
    }
  }

  private struct ChestPlacementCandidate
  {
    public Vector2Int tile;
    public Vector3 worldPos;
    public Quaternion rotation;

    public ChestPlacementCandidate(Vector2Int tile, Vector3 worldPos, Quaternion rotation)
    {
      this.tile = tile;
      this.worldPos = worldPos;
      this.rotation = rotation;
    }
  }

  /// <summary>
  /// Bố trí rương báu (Interactive Chest) tại Tầng 1 (Ưu tiên góc phòng hoặc áp sát tường đặc)
  /// </summary>
  private void DecorateFloorChest(RoomData room, Transform interactablesGroup, HashSet<Vector2Int> forbiddenTiles, DungeonData data, float tileSize)
  {
    if (!enableChests || (regularChestPrefab == null && goldChestPrefab == null)) return;

    bool isTreasureRoom = (room.type == RoomType.Treasure);
    bool shouldSpawn = isTreasureRoom || (Random.value < floorChestChance);
    if (!shouldSpawn) return;

    float goldChance = isTreasureRoom ? treasureRoomGoldChestChance : goldChestChance;
    bool isGold = (Random.value < goldChance && goldChestPrefab != null);
    GameObject chestPrefab = (isGold && goldChestPrefab != null) ? goldChestPrefab : (regularChestPrefab ?? goldChestPrefab);
    if (chestPrefab == null) return;

    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;
    float cOffset = Mathf.Max(0.1f, chestWallOffset);

    // Ưu tiên 1: Đặt rương tại góc phòng (Cách đều 2 bức tường bằng cOffset, xoay chéo 45 độ hướng ra giữa phòng)
    ChestPlacementCandidate[] cornerList = new ChestPlacementCandidate[]
    {
      new ChestPlacementCandidate(
        new Vector2Int(b.x, b.y),
        new Vector3(b.x * tileSize + cOffset, 0f, b.y * tileSize + cOffset),
        Quaternion.Euler(0f, 45f, 0f)
      ),
      new ChestPlacementCandidate(
        new Vector2Int(b.x + b.width - 1, b.y),
        new Vector3((b.x + b.width) * tileSize - cOffset, 0f, b.y * tileSize + cOffset),
        Quaternion.Euler(0f, 315f, 0f)
      ),
      new ChestPlacementCandidate(
        new Vector2Int(b.x, b.y + b.height - 1),
        new Vector3(b.x * tileSize + cOffset, 0f, (b.y + b.height) * tileSize - cOffset),
        Quaternion.Euler(0f, 135f, 0f)
      ),
      new ChestPlacementCandidate(
        new Vector2Int(b.x + b.width - 1, b.y + b.height - 1),
        new Vector3((b.x + b.width) * tileSize - cOffset, 0f, (b.y + b.height) * tileSize - cOffset),
        Quaternion.Euler(0f, 225f, 0f)
      )
    };
    ShuffleArray(cornerList);

    foreach (var c in cornerList)
    {
      if (!forbiddenTiles.Contains(c.tile) && (!room.hasMezzanine || !room.IsInMezzanine(c.tile.x, c.tile.y)))
      {
        GameObject chestObj = Instantiate(chestPrefab, c.worldPos, c.rotation, interactablesGroup);
        EnsureNavMeshObstacleModifier(chestObj);
        forbiddenTiles.Add(c.tile);
        return;
      }
    }

    // Ưu tiên 2: Áp sát các vách tường đặc (Hỗ trợ đủ 4 bức tường: Nam, Bắc, Tây, Đông)
    List<ChestPlacementCandidate> wallCandidates = new List<ChestPlacementCandidate>();

    // Tường Nam
    for (int x = b.x + 2; x < b.x + b.width - 2; x++)
    {
      Vector2Int sTile = new Vector2Int(x, b.y);
      if (!forbiddenTiles.Contains(sTile) && HasSolidWall(x, b.y, 1, data, room))
      {
        wallCandidates.Add(new ChestPlacementCandidate(
          sTile,
          new Vector3(x * tileSize + halfTile, 0f, b.y * tileSize + cOffset),
          Quaternion.identity
        ));
      }
    }

    // Tường Bắc
    for (int x = b.x + 2; x < b.x + b.width - 2; x++)
    {
      Vector2Int nTile = new Vector2Int(x, b.y + b.height - 1);
      if (!forbiddenTiles.Contains(nTile) && HasSolidWall(x, b.y + b.height - 1, 0, data, room))
      {
        wallCandidates.Add(new ChestPlacementCandidate(
          nTile,
          new Vector3(x * tileSize + halfTile, 0f, (b.y + b.height) * tileSize - cOffset),
          Quaternion.Euler(0f, 180f, 0f)
        ));
      }
    }

    // Tường Tây
    for (int y = b.y + 2; y < b.y + b.height - 2; y++)
    {
      Vector2Int wTile = new Vector2Int(b.x, y);
      if (!forbiddenTiles.Contains(wTile) && HasSolidWall(b.x, y, 2, data, room))
      {
        wallCandidates.Add(new ChestPlacementCandidate(
          wTile,
          new Vector3(b.x * tileSize + cOffset, 0f, y * tileSize + halfTile),
          Quaternion.Euler(0f, 90f, 0f)
        ));
      }
    }

    // Tường Đông
    for (int y = b.y + 2; y < b.y + b.height - 2; y++)
    {
      Vector2Int eTile = new Vector2Int(b.x + b.width - 1, y);
      if (!forbiddenTiles.Contains(eTile) && HasSolidWall(b.x + b.width - 1, y, 3, data, room))
      {
        wallCandidates.Add(new ChestPlacementCandidate(
          eTile,
          new Vector3((b.x + b.width) * tileSize - cOffset, 0f, y * tileSize + halfTile),
          Quaternion.Euler(0f, 270f, 0f)
        ));
      }
    }

    if (wallCandidates.Count > 0)
    {
      ChestPlacementCandidate pick = wallCandidates[Random.Range(0, wallCandidates.Count)];
      GameObject chestObj = Instantiate(chestPrefab, pick.worldPos, pick.rotation, interactablesGroup);
      EnsureNavMeshObstacleModifier(chestObj);
      forbiddenTiles.Add(pick.tile);
      return;
    }

    // Ưu tiên 3: Ô sàn ngẫu nhiên bất kỳ
    for (int attempt = 0; attempt < 15; attempt++)
    {
      int rx = Random.Range(b.x + 1, b.x + b.width - 1);
      int ry = Random.Range(b.y + 1, b.y + b.height - 1);
      Vector2Int tile = new Vector2Int(rx, ry);
      if (!forbiddenTiles.Contains(tile) && (!room.hasMezzanine || !room.IsInMezzanine(rx, ry)))
      {
        Vector3 pos = new Vector3(rx * tileSize + halfTile, 0f, ry * tileSize + halfTile);
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);

        GameObject chestObj = Instantiate(chestPrefab, pos, rot, interactablesGroup);
        EnsureNavMeshObstacleModifier(chestObj);
        forbiddenTiles.Add(tile);
        return;
      }
    }
  }

  private void ShuffleList<T>(List<T> list)
  {
    for (int i = list.Count - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1);
      T temp = list[i];
      list[i] = list[j];
      list[j] = temp;
    }
  }
}