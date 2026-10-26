using System.Collections.Generic;
using UnityEngine;

public partial class DungeonPropDecorator
{
  /// <summary>
  /// Bố trí các cụm tường tàn tích (Wall Templates) và tiểu cảnh đạo cụ (Prop Templates) vào các phòng đủ lớn.
  /// Tự động chiếm dụng toàn bộ diện tích footprint vào forbiddenTiles để tuyệt đối không bị đồ khác đè lên.
  /// </summary>
  private void DecorateRoomTemplates(RoomData room, Transform staticPropsGroup, Transform lightsGroup, Transform interactablesGroup, HashSet<Vector2Int> forbiddenTiles, RoomTheme theme, DungeonData data, float tileSize)
  {
    RectInt b = room.bounds;
    // Phòng quá nhỏ (< 7x7) không nên nhét cụm kiến trúc lớn để tránh cản đường
    if (b.width < 7 || b.height < 7) return;

    int maxWallTemplates = 0;
    int maxPropTemplates = 0;

    if (b.width >= 12 && b.height >= 12)
    {
      maxWallTemplates = 2;
      maxPropTemplates = 1;
    }
    else if (b.width >= 9 || b.height >= 9)
    {
      maxWallTemplates = 1;
      maxPropTemplates = 1;
    }
    else
    {
      // Phòng vừa (7-8 tiles)
      maxWallTemplates = (Random.value < 0.5f) ? 1 : 0;
      maxPropTemplates = (maxWallTemplates == 0) ? 1 : 0;
    }

    // 1. SINH CỤM TƯỜNG TÀN TÍCH (WALL TEMPLATES)
    if (enableWallTemplates && maxWallTemplates > 0 && wallTemplatePrefabs.Count > 0 && Random.value < wallTemplateChance)
    {
      int placed = 0;
      List<GameObject> pool = GetThemedWallTemplates(theme);
      for (int attempt = 0; attempt < 20 && placed < maxWallTemplates; attempt++)
      {
        GameObject prefab = pool[Random.Range(0, pool.Count)];
        if (TryPlaceTemplate(prefab, room, staticPropsGroup, lightsGroup, interactablesGroup, forbiddenTiles, tileSize))
        {
          placed++;
        }
      }
    }

    // 2. SINH CỤM TIỂU CẢNH ĐẠO CỤ (PROP TEMPLATES)
    if (enablePropTemplates && maxPropTemplates > 0 && propTemplatePrefabs.Count > 0 && Random.value < propTemplateChance)
    {
      int placed = 0;
      List<GameObject> pool = GetThemedPropTemplates(theme);
      for (int attempt = 0; attempt < 20 && placed < maxPropTemplates; attempt++)
      {
        GameObject prefab = pool[Random.Range(0, pool.Count)];
        if (TryPlaceTemplate(prefab, room, staticPropsGroup, lightsGroup, interactablesGroup, forbiddenTiles, tileSize))
        {
          placed++;
        }
      }
    }
  }

  /// <summary>
  /// Thử nghiệm tìm vị trí và góc xoay thích hợp để đặt 1 cụm Template.
  /// Kiểm tra 100% diện tích footprint không va chạm cửa, đường đi chính, cầu thang, tường bao và mezzanine.
  /// </summary>
  private bool TryPlaceTemplate(GameObject prefab, RoomData room, Transform staticPropsGroup, Transform lightsGroup, Transform interactablesGroup, HashSet<Vector2Int> forbiddenTiles, float tileSize)
  {
    if (prefab == null) return false;

    Bounds localBounds = GetPrefabLocalBounds(prefab);
    RectInt b = room.bounds;
    float halfTile = tileSize / 2f;

    // Các hướng xoay chuẩn 90 độ
    int[] angles = new int[] { 0, 90, 180, 270 };
    ShuffleArray(angles);

    for (int a = 0; a < angles.Length; a++)
    {
      int angle = angles[a];
      Quaternion rot = Quaternion.Euler(0, angle, 0);

      Vector3 ext = localBounds.extents;
      Vector3 cOffset = localBounds.center;
      Vector3 rotCenterOffset = rot * cOffset;

      // Kích thước phủ rộng theo trục X và Z sau khi xoay (thêm đệm 0.25m an toàn)
      bool swapXZ = (angle == 90 || angle == 270);
      float rotatedHalfSpanX = (swapXZ ? ext.z : ext.x) + 0.25f;
      float rotatedHalfSpanZ = (swapXZ ? ext.x : ext.z) + 0.25f;

      int tileRadiusX = Mathf.CeilToInt(rotatedHalfSpanX / tileSize);
      int tileRadiusZ = Mathf.CeilToInt(rotatedHalfSpanZ / tileSize);

      int minCx = b.x + 1 + tileRadiusX;
      int maxCx = b.x + b.width - 2 - tileRadiusX;
      int minCy = b.y + 1 + tileRadiusZ;
      int maxCy = b.y + b.height - 2 - tileRadiusZ;

      if (minCx > maxCx || minCy > maxCy)
      {
        continue; // Template quá lớn so với phòng ở góc xoay này
      }

      // Thử một số tọa độ tâm ngẫu nhiên
      for (int attempt = 0; attempt < 10; attempt++)
      {
        int cx = Random.Range(minCx, maxCx + 1);
        int cy = Random.Range(minCy, maxCy + 1);

        Vector3 worldCenter = new Vector3(cx * tileSize + halfTile, 0f, cy * tileSize + halfTile);

        // Tính khoảng tile thực tế bị chiếm dụng bởi bounding box
        float minWx = worldCenter.x + rotCenterOffset.x - rotatedHalfSpanX;
        float maxWx = worldCenter.x + rotCenterOffset.x + rotatedHalfSpanX;
        float minWz = worldCenter.z + rotCenterOffset.z - rotatedHalfSpanZ;
        float maxWz = worldCenter.z + rotCenterOffset.z + rotatedHalfSpanZ;

        int tMinX = Mathf.FloorToInt(minWx / tileSize);
        int tMaxX = Mathf.FloorToInt(maxWx / tileSize);
        int tMinY = Mathf.FloorToInt(minWz / tileSize);
        int tMaxY = Mathf.FloorToInt(maxWz / tileSize);

        bool canPlace = true;
        for (int tx = tMinX; tx <= tMaxX; tx++)
        {
          for (int ty = tMinY; ty <= tMaxY; ty++)
          {
            Vector2Int tile = new Vector2Int(tx, ty);

            // Phải nằm hoàn toàn bên trong phòng (không chạm tường bao)
            if (tx <= b.x || tx >= b.x + b.width - 1 || ty <= b.y || ty >= b.y + b.height - 1)
            {
              canPlace = false;
              break;
            }

            // Không được trùng vào lối đi cửa, đường đi chính, cầu thang, hoặc template khác
            if (forbiddenTiles.Contains(tile))
            {
              canPlace = false;
              break;
            }

            // Không đặt dưới sàn tầng lửng
            if (room.hasMezzanine && room.IsInMezzanine(tx, ty))
            {
              canPlace = false;
              break;
            }
          }
          if (!canPlace) break;
        }

        if (canPlace)
        {
          // ĐẶT CỤM TEMPLATE VÀO PHÒNG
          GameObject instance = Instantiate(prefab, worldCenter, rot, staticPropsGroup);

          // Tách toàn bộ các cụm Đuốc / Nến / Particle System (Khói, Lửa) sang lightsGroup
          // để không bị xóa sổ khi gộp mesh phòng!
          HashSet<Transform> dynamicRoots = new HashSet<Transform>();

          // 1. Tìm tất cả ParticleSystem (khói, lửa, vfx)
          foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>(true))
          {
            Transform t = ps.transform;
            while (t.parent != null && t.parent != instance.transform)
            {
              t = t.parent;
            }
            if (t != instance.transform)
            {
              dynamicRoots.Add(t);
            }
          }

          // 2. Tìm tất cả Light (Point Light)
          foreach (Light l in instance.GetComponentsInChildren<Light>(true))
          {
            Transform t = l.transform;
            while (t.parent != null && t.parent != instance.transform)
            {
              t = t.parent;
            }
            if (t != instance.transform)
            {
              dynamicRoots.Add(t);
            }
          }

          // 3. Chuyển toàn bộ các cây đuốc / ngọn lửa hoàn chỉnh sang lightsGroup
          foreach (Transform dynRoot in dynamicRoots)
          {
            dynRoot.SetParent(lightsGroup, true);

            // Kích hoạt phát khói và lửa cho tất cả ParticleSystem con
            ParticleSystem[] childPS = dynRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in childPS)
            {
              if (!ps.isPlaying) ps.Play();
            }
          }

          // 4. QUÉT VÀ KÍCH HOẠT CÁC SOCKET LOOT (Thùng gỗ đập được & Rương báu)
          List<Transform> barrelSockets = new List<Transform>();
          List<Transform> chestSockets = new List<Transform>();

          foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
          {
            if (child == null) continue;
            string cName = child.name;
            if (cName.IndexOf("Loot_Barrel_Socket", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                cName.IndexOf("Barrel_Socket", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                cName.IndexOf("Socket_Barrel", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
              barrelSockets.Add(child);
            }
            else if (cName.IndexOf("Loot_Chest_Socket", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cName.IndexOf("Chest_Socket", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cName.IndexOf("Socket_Chest", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
              chestSockets.Add(child);
            }
          }

          // A. Xử lý các Socket Thùng gỗ
          foreach (Transform socket in barrelSockets)
          {
            if (enableLootAndBreakables && enableTemplateLoot && breakableBarrelPrefabs.Count > 0 && Random.value < templateBarrelSpawnChance)
            {
              GameObject barrelPrefab = breakableBarrelPrefabs[Random.Range(0, breakableBarrelPrefabs.Count)];
              GameObject barrelObj = Instantiate(barrelPrefab, socket.position, socket.rotation, interactablesGroup);
              EnsureNavMeshObstacleModifier(barrelObj);

              int sx = Mathf.FloorToInt(socket.position.x / tileSize);
              int sy = Mathf.FloorToInt(socket.position.z / tileSize);
              forbiddenTiles.Add(new Vector2Int(sx, sy));
            }
            socket.gameObject.SetActive(false);
          }

          // B. Xử lý các Socket Rương báu
          foreach (Transform socket in chestSockets)
          {
            if (enableLootAndBreakables && enableTemplateLoot && (regularChestPrefab != null || goldChestPrefab != null) && Random.value < templateChestSpawnChance)
            {
              bool isGold = (Random.value < goldChestChance && goldChestPrefab != null);
              GameObject chestPrefab = (isGold && goldChestPrefab != null) ? goldChestPrefab : (regularChestPrefab ?? goldChestPrefab);
              if (chestPrefab != null)
              {
                GameObject chestObj = Instantiate(chestPrefab, socket.position, socket.rotation, interactablesGroup);
                EnsureNavMeshObstacleModifier(chestObj);

                int sx = Mathf.FloorToInt(socket.position.x / tileSize);
                int sy = Mathf.FloorToInt(socket.position.z / tileSize);
                forbiddenTiles.Add(new Vector2Int(sx, sy));
              }
            }
            socket.gameObject.SetActive(false);
          }

          // ĐÁNH DẤU TOÀN BỘ CÁC Ô ĐÃ CHIẾM DỤNG VÀO FORBIDDENTILES ĐỂ KHÔNG BỊ PROPS KHÁC ĐÈ LÊN
          for (int tx = tMinX; tx <= tMaxX; tx++)
          {
            for (int ty = tMinY; ty <= tMaxY; ty++)
            {
              forbiddenTiles.Add(new Vector2Int(tx, ty));
            }
          }

          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Tính toán chính xác Bounding Box cục bộ của Prefab Template dựa trên toàn bộ MeshFilter con
  /// </summary>
  private Bounds GetPrefabLocalBounds(GameObject prefab)
  {
    if (templateBoundsCache.TryGetValue(prefab, out Bounds cached))
    {
      return cached;
    }

    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
    bool hasBounds = false;

    MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
    foreach (var mf in filters)
    {
      if (mf.sharedMesh == null) continue;
      Bounds mb = mf.sharedMesh.bounds;
      Transform t = mf.transform;

      Matrix4x4 m = prefab.transform.worldToLocalMatrix * t.localToWorldMatrix;

      Vector3 min = mb.min;
      Vector3 max = mb.max;
      Vector3[] corners = new Vector3[8]
      {
        m.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)),
        m.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)),
        m.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)),
        m.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)),
        m.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)),
        m.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)),
        m.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)),
        m.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z))
      };

      foreach (var c in corners)
      {
        if (!hasBounds)
        {
          bounds = new Bounds(c, Vector3.zero);
          hasBounds = true;
        }
        else
        {
          bounds.Encapsulate(c);
        }
      }
    }

    if (!hasBounds)
    {
      bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
    }

    templateBoundsCache[prefab] = bounds;
    return bounds;
  }

  private List<GameObject> GetThemedWallTemplates(RoomTheme theme)
  {
    if (wallTemplatePrefabs == null || wallTemplatePrefabs.Count == 0) return wallTemplatePrefabs;

    List<GameObject> filtered = new List<GameObject>();
    switch (theme)
    {
      case RoomTheme.RuinsAbandoned:
        filtered.AddRange(wallTemplatePrefabs.FindAll(p => p.name.Contains("ruins") || p.name.Contains("L")));
        break;
      case RoomTheme.GrandHall:
        filtered.AddRange(wallTemplatePrefabs.FindAll(p => p.name.Contains("shrine") || p.name.Contains("gate")));
        break;
      case RoomTheme.ArmoryStorage:
        filtered.AddRange(wallTemplatePrefabs.FindAll(p => p.name.Contains("storage") || p.name.Contains("L2")));
        break;
      default:
        filtered.AddRange(wallTemplatePrefabs.FindAll(p => p.name.Contains("gate") || p.name.Contains("L")));
        break;
    }

    return (filtered.Count > 0) ? filtered : wallTemplatePrefabs;
  }

  private List<GameObject> GetThemedPropTemplates(RoomTheme theme)
  {
    if (propTemplatePrefabs == null || propTemplatePrefabs.Count == 0) return propTemplatePrefabs;

    List<GameObject> filtered = new List<GameObject>();
    switch (theme)
    {
      case RoomTheme.LibraryStudy:
        filtered.AddRange(propTemplatePrefabs.FindAll(p => p.name.Contains("alchemist")));
        break;
      case RoomTheme.Barracks:
        filtered.AddRange(propTemplatePrefabs.FindAll(p => p.name.Contains("camp")));
        break;
      case RoomTheme.ArmoryStorage:
        filtered.AddRange(propTemplatePrefabs.FindAll(p => p.name.Contains("looted")));
        break;
      case RoomTheme.DiningQuarters:
      case RoomTheme.GrandHall:
        filtered.AddRange(propTemplatePrefabs.FindAll(p => p.name.Contains("banquet")));
        break;
      case RoomTheme.RuinsAbandoned:
        filtered.AddRange(propTemplatePrefabs.FindAll(p => p.name.Contains("collapsed") || p.name.Contains("looted")));
        break;
    }

    return (filtered.Count > 0) ? filtered : propTemplatePrefabs;
  }
}
