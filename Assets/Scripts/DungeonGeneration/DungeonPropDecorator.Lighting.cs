using System.Collections.Generic;
using UnityEngine;

public partial class DungeonPropDecorator
{
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
  /// Cố gắng tìm vị trí và đặt 1 ngọn đuốc tường chiến lược gắn trên vách đá đặc
  /// </summary>
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
}
