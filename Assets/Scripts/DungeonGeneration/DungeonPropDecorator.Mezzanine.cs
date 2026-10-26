using System.Collections.Generic;
using UnityEngine;

public partial class DungeonPropDecorator
{
  /// <summary>
  /// Trang trí gác lửng tầng 2 (Mezzanine): cờ hiệu, khiên kiếm, đuốc tường, đuốc sàn, kệ sách, rương, bàn ghế ngắm cảnh lan can
  /// Khớp chuẩn cao độ mặt sàn tầng 2 (mY = room.mezzanineHeight = 8.0m)
  /// </summary>
  private void DecorateMezzanine(RoomData room, Transform staticPropsGroup, Transform lightsGroup, Transform interactablesGroup, HashSet<Vector2Int> forbiddenTiles, List<GameObject> roomBanners, RoomTheme theme, DungeonData data, float tileSize)
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

    // 2.5. RƯƠNG BÁU & CỤM THÙNG GỖ ĐẬP ĐƯỢC TRÊN GÁC LỬNG TẦNG 2
    if (enableLootAndBreakables && room.mezzanineTiles != null)
    {
      // A. Sinh Rương báu trên gác lửng (Tỉ lệ cao do người chơi leo lên khám phá)
      if (enableChests && (regularChestPrefab != null || goldChestPrefab != null) && Random.value < mezzanineChestChance)
      {
        List<Vector2Int> backTiles = new List<Vector2Int>();
        foreach (var t in room.mezzanineTiles)
        {
          if (forbiddenTiles.Contains(t)) continue;
          bool isBack = (room.mezzanineSide == MezzanineWallSide.North) ? (t.y == b.y + b.height - 1) : (t.y == b.y);
          if (isBack) backTiles.Add(t);
        }

        if (backTiles.Count > 0)
        {
          Vector2Int chestTile = backTiles[Random.Range(0, backTiles.Count)];
          float cOffset = Mathf.Max(0.1f, chestWallOffset);
          Vector3 pos = (room.mezzanineSide == MezzanineWallSide.North)
            ? new Vector3(chestTile.x * tileSize + halfTile, mY, (b.y + b.height) * tileSize - cOffset)
            : new Vector3(chestTile.x * tileSize + halfTile, mY, b.y * tileSize + cOffset);
          Quaternion rot = (room.mezzanineSide == MezzanineWallSide.North) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

          bool isGold = (Random.value < mezzanineGoldChestChance && goldChestPrefab != null);
          GameObject chestPrefab = (isGold && goldChestPrefab != null) ? goldChestPrefab : (regularChestPrefab ?? goldChestPrefab);
          if (chestPrefab != null)
          {
            GameObject chestObj = Instantiate(chestPrefab, pos, rot, interactablesGroup);
            EnsureNavMeshObstacleModifier(chestObj);
            forbiddenTiles.Add(chestTile);
          }
        }
      }

      // B. Sinh Thùng gỗ đập được trên gác lửng (1 - 2 cụm Solo hoặc Pair)
      if (enableFloorBarrels && breakableBarrelPrefabs.Count > 0)
      {
        int maxMezzBarrels = Random.Range(1, 3);
        int placedMezzBarrels = 0;
        foreach (var t in room.mezzanineTiles)
        {
          if (placedMezzBarrels >= maxMezzBarrels) break;
          if (forbiddenTiles.Contains(t)) continue;

          bool isBack = (room.mezzanineSide == MezzanineWallSide.North) ? (t.y == b.y + b.height - 1) : (t.y == b.y);
          if (isBack) continue; // Để lại sát tường cho kệ sách/rương

          if (Random.value < 0.35f)
          {
            Vector3 center = new Vector3(t.x * tileSize + halfTile, mY, t.y * tileSize + halfTile);
            float roll = Random.value;

            if (roll < 0.45f)
            {
              // Solo
              Vector3 jitter = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
              GameObject p = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
              GameObject bObj = Instantiate(p, center + jitter, Quaternion.Euler(0, Random.Range(0f, 360f), 0), interactablesGroup);
              EnsureNavMeshObstacleModifier(bObj);
            }
            else
            {
              // Pair
              float angle = Random.Range(0f, 360f);
              Quaternion crot = Quaternion.Euler(0, angle, 0);
              float spacing = Mathf.Max(0.3f, barrelPairSpacing);
              Vector3 pos1 = center + crot * new Vector3(-spacing, 0f, 0f);
              Vector3 pos2 = center + crot * new Vector3(spacing, 0f, 0f);

              GameObject p1 = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
              GameObject p2 = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];

              GameObject b1 = Instantiate(p1, pos1, Quaternion.Euler(0, Random.Range(0f, 360f), 0), interactablesGroup);
              GameObject b2 = Instantiate(p2, pos2, Quaternion.Euler(0, Random.Range(0f, 360f), 0), interactablesGroup);

              EnsureNavMeshObstacleModifier(b1);
              EnsureNavMeshObstacleModifier(b2);
            }

            forbiddenTiles.Add(t);
            placedMezzBarrels++;
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
}
