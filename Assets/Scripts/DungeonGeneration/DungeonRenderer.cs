using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class DungeonRenderer : MonoBehaviour
{
  [Header("Cài đặt 3D Prefabs Cơ Bản")]
  public GameObject[] floorPrefabs;
  public GameObject wallPrefab;
  public GameObject cornerPrefab;
  public GameObject ceilingPrefab;
  public GameObject invisibleFloorPrefab;
  public Transform dungeonParent;

  [Header("Cài đặt Tầng 2 (Gác lửng Mezzanine)")]
  [Tooltip("Prefab sàn gỗ mặt trên và mặt dưới tầng 2")]
  public GameObject mezzanineFloorPrefab;
  [Tooltip("Prefab khối viền mép ngoài tầng 2 che khe hở")]
  public GameObject mezzanineEdgePrefab;
  [Tooltip("Prefab khối góc ngoài tầng 2")]
  public GameObject mezzanineCornerPrefab;
  [Tooltip("Prefab cột chống từ trệt lên tầng 2")]
  public GameObject pillarPrefab;
  [Tooltip("Prefab lan can mép tầng 2")]
  public GameObject barrierPrefab;
  [Tooltip("Prefab cầu thang dẫn từ trệt lên tầng 2")]
  public GameObject stairsPrefab;
  [Tooltip("Prefab bệ đá bịt kín khoảng trống dưới nhịp trên cầu thang (stairs_fill gồm 4 bức tường ghép sẵn)")]
  public GameObject stairsFillPrefab;
  [Tooltip("Bịt kín khoảng trống dưới nhịp trên cầu thang bằng bệ đá")]
  public bool encloseStairsBase = true;

  [Header("AI Navigation")]
  [Tooltip("Kéo component NavMeshSurface vào đây để tự động bake khi sinh xong map")]
  public NavMeshSurface navMeshSurface;

  // Danh sách các lộ trình NavMesh từ sàn trệt lên tầng 2 phục vụ kiểm định và vẽ Gizmos
  [System.NonSerialized]
  public List<Vector3[]> debugNavMeshPaths = new List<Vector3[]>();

  [Header("Hệ Thống Trang Trí Props & Chiếu Sáng (Bước 4)")]
  [Tooltip("Script quản lý trang trí hệ thống đuốc, cờ hiệu, nội thất và đồ trang trí")]
  public DungeonPropDecorator propDecorator;

  [Header("Hình học Dungeon")]
  public Material dungeonMaterial;
  public float tileSize = 2f;
  public float ceilingHeight = 8f; // Chiều cao mặc định nếu phòng không có cấu hình riêng
  public float wallHeight = 4f;

  [Header("Đa dạng Sàn (Floor Variation)")]
  [Tooltip("Càng nhỏ thì sàn cùng loại tạo thành mảng càng lớn. Càng lớn thì sàn trộn càng nhuyễn.")]
  public float floorNoiseScale = 0.2f;

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

  public void EnsurePrefabsAssigned()
  {
#if UNITY_EDITOR
    string folder = "Assets/Prefabs/Dungeon/DungeonPrefabs";
    if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
    {
      folder = "Assets/Enviro Prefabs/Dungeon/DungeonPrefabs";
    }

    if (mezzanineFloorPrefab == null)
    {
      mezzanineFloorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/floor_wood_small.prefab");
      if (mezzanineFloorPrefab == null)
        mezzanineFloorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/floor_wood_large.prefab");
    }
    if (mezzanineEdgePrefab == null)
      mezzanineEdgePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/floor_foundation_front.prefab");
    if (mezzanineCornerPrefab == null)
      mezzanineCornerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/floor_foundation_corner.prefab");
    if (pillarPrefab == null)
      pillarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/pillar.prefab");
    if (barrierPrefab == null)
    {
      barrierPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/barrier.prefab");
      if (barrierPrefab == null)
        barrierPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/barrier_half.prefab");
    }
    if (stairsPrefab == null)
      stairsPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/stairs.prefab");
    if (stairsFillPrefab == null)
      stairsFillPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/stairs_fill.prefab");
#endif
  }

  public void BuildMesh(DungeonData data)
  {
    if (dungeonParent == null) return;
    ClearGeneratedDungeon();
    EnsurePrefabsAssigned();

    // 1. Dựng sàn trệt, tường và trần theo chiều cao từng phòng
    foreach (RectInt room in data.rooms)
    {
      CreateCombinedAreaMesh(room, "Room", data);
      CreateInvisibleCollider(room);
    }
    foreach (RectInt corridor in data.corridors)
    {
      CreateCombinedAreaMesh(corridor, "Corridor", data);
      CreateInvisibleCollider(corridor);
    }

    // 2. Dựng cấu trúc gác lửng tầng 2 (Mezzanine) cho các phòng đủ điều kiện
    if (data.roomDataList != null)
    {
      foreach (var r in data.roomDataList)
      {
        if (r.hasMezzanine)
        {
          BuildMezzanine(r, data);
        }
      }
    }

    // 3. TRANG TRÍ PROPS VÀ HỆ THỐNG CHIẾU SÁNG (Sinh TRƯỚC khi Bake NavMesh để NavMesh tự động đục lỗ né vật cản)
    if (propDecorator == null) propDecorator = GetComponent<DungeonPropDecorator>();
    if (propDecorator == null) propDecorator = gameObject.AddComponent<DungeonPropDecorator>();
    propDecorator.DecorateDungeon(data, dungeonParent, tileSize);

    // 4. TỰ ĐỘNG BAKE NAVMESH VÀ KIỂM ĐỊNH KẾT NỐI TOÀN BỘ SÀN TRỆT, CẦU THANG VÀ TẦNG 2 (Sau khi đã có đầy đủ Props)
    BakeAndValidateNavMesh(data);
  }

  void ClearGeneratedDungeon()
  {
    for (int i = dungeonParent.childCount - 1; i >= 0; i--)
    {
      GameObject child = dungeonParent.GetChild(i).gameObject;
      if (Application.isPlaying) Destroy(child);
      else DestroyImmediate(child);
    }
  }

  float GetAreaCeilingHeight(RectInt area, string type, DungeonData data)
  {
    if (type == "Corridor") return 8f; // Hành lang tối thiểu cao 8m (2 tầng tường)
    RoomData r = data.GetRoomAt(area.x, area.y);
    if (r != null) return r.ceilingHeight;
    return ceilingHeight;
  }

  float GetTileCeilingHeight(int x, int y, DungeonData data)
  {
    RoomData r = data.GetRoomAt(x, y);
    if (r != null) return r.ceilingHeight;
    return 8f; // Mặc định nếu là hành lang (8m)
  }

  bool IsWallAtHeight(int x, int y, float currentY, DungeonData data)
  {
    if (!data.IsInBounds(x, y)) return true;
    if (data.mapGrid[x, y] == 0) return true;

    // Nếu ô lân cận là hành lang (trần 8m) nhưng phòng hiện tại cao 16m,
    // thì ở độ cao currentY >= 8m, ô lân cận được coi là TƯỜNG để bịt kín khoảng hở phía trên cửa!
    float neighborCeiling = GetTileCeilingHeight(x, y, data);
    return currentY >= neighborCeiling;
  }

  void CreateCombinedAreaMesh(RectInt area, string type, DungeonData data)
  {
    List<CombineInstance> combineInstances = new List<CombineInstance>();

    // Lưu sẵn danh sách Mesh của tất cả các loại sàn để tối ưu hiệu năng
    Mesh[] floorMeshes = new Mesh[floorPrefabs.Length];
    for (int i = 0; i < floorPrefabs.Length; i++)
    {
      floorMeshes[i] = GetMeshFromPrefab(floorPrefabs[i]);
    }

    Mesh wallMesh = GetMeshFromPrefab(wallPrefab);
    Mesh cornerMesh = GetMeshFromPrefab(cornerPrefab);
    Mesh ceilingMesh = GetMeshFromPrefab(ceilingPrefab);

    float halfTile = tileSize / 2f;
    float noiseOffsetX = 10000f;
    float noiseOffsetY = 10000f;

    float areaCeiling = GetAreaCeilingHeight(area, type, data);

    for (int x = area.x; x < area.x + area.width; x++)
    {
      for (int y = area.y; y < area.y + area.height; y++)
      {
        if (!data.IsInBounds(x, y) || data.mapGrid[x, y] != 1) continue;

        // Bỏ qua các ô hành lang nằm lấn vào bên trong phòng (ngăn trần hành lang và sàn trùng lặp bị sinh ra bên trong phòng)
        if (type == "Corridor" && data.IsInsideAnyRoom(x, y)) continue;

        float worldX = x * tileSize + halfTile;
        float worldZ = y * tileSize + halfTile;

        // --- 1. SINH SÀN TRỆT ĐA DẠNG BẰNG PERLIN NOISE ---
        if (floorPrefabs.Length > 0)
        {
          float noise = Mathf.PerlinNoise((x + noiseOffsetX) * floorNoiseScale, (y + noiseOffsetY) * floorNoiseScale);
          int floorIndex = Mathf.Clamp(Mathf.FloorToInt(noise * floorPrefabs.Length), 0, floorPrefabs.Length - 1);

          Mesh selectedFloorMesh = floorMeshes[floorIndex];
          if (selectedFloorMesh != null)
          {
            combineInstances.Add(CreateCombineInstance(floorPrefabs[floorIndex], selectedFloorMesh, new Vector3(worldX, 0, worldZ), Quaternion.identity));
          }
        }

        // --- 2. SINH TRẦN NHÀ THEO CHIỀU CAO CỦA PHÒNG/HÀNH LANG ---
        if (ceilingMesh != null)
        {
          combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX, areaCeiling, worldZ), Quaternion.identity, new Vector3(0.5f, 1f, 0.5f)));
        }

        // --- 3. SINH TƯỜNG THEO TỪNG TẦNG ĐỘ CAO (BỊT KÍN KHOẢNG HỞ TRÊN CỬA) ---
        int wallLayers = Mathf.RoundToInt(areaCeiling / wallHeight);
        for (int i = 0; i < wallLayers; i++)
        {
          float currentY = i * wallHeight;

          bool wLeft = IsWallAtHeight(x - 1, y, currentY, data);
          bool wRight = IsWallAtHeight(x + 1, y, currentY, data);
          bool wBottom = IsWallAtHeight(x, y - 1, currentY, data);
          bool wTop = IsWallAtHeight(x, y + 1, currentY, data);

          // Kiểm tra xem tầng ngay bên dưới có phải là khoảng mở (cửa vòm / lối đi) không
          bool belowOpenLeft = (i > 0) && !IsWallAtHeight(x - 1, y, currentY - wallHeight, data);
          bool belowOpenRight = (i > 0) && !IsWallAtHeight(x + 1, y, currentY - wallHeight, data);
          bool belowOpenBottom = (i > 0) && !IsWallAtHeight(x, y - 1, currentY - wallHeight, data);
          bool belowOpenTop = (i > 0) && !IsWallAtHeight(x, y + 1, currentY - wallHeight, data);

          bool cTL = wLeft && wTop;
          bool cTR = wRight && wTop;
          bool cBL = wLeft && wBottom;
          bool cBR = wRight && wBottom;

          if (cornerMesh != null)
          {
            if (cTL) combineInstances.Add(CreateCombineInstance(cornerPrefab, cornerMesh, new Vector3(worldX - halfTile, currentY, worldZ + halfTile), Quaternion.Euler(0, 90, 0)));
            if (cTR) combineInstances.Add(CreateCombineInstance(cornerPrefab, cornerMesh, new Vector3(worldX + halfTile, currentY, worldZ + halfTile), Quaternion.Euler(0, 180, 0)));
            if (cBR) combineInstances.Add(CreateCombineInstance(cornerPrefab, cornerMesh, new Vector3(worldX + halfTile, currentY, worldZ - halfTile), Quaternion.Euler(0, 270, 0)));
            if (cBL) combineInstances.Add(CreateCombineInstance(cornerPrefab, cornerMesh, new Vector3(worldX - halfTile, currentY, worldZ - halfTile), Quaternion.Euler(0, 0, 0)));
          }

          if (wallMesh != null)
          {
            float quarterTile = halfTile / 2f;
            Vector3 fullScale = Vector3.one;
            Vector3 halfScale = new Vector3(0.5f, 1f, 1f);

            if (wTop)
            {
              if (!cTL && !cTR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX, currentY, worldZ + halfTile), Quaternion.Euler(0, 180, 0), fullScale));
              else
              {
                if (!cTL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - quarterTile, currentY, worldZ + halfTile), Quaternion.Euler(0, 180, 0), halfScale));
                if (!cTR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + quarterTile, currentY, worldZ + halfTile), Quaternion.Euler(0, 180, 0), halfScale));
              }

              // Bịt kín đáy tường trên vòm cửa để tránh nhìn xuyên vào hư vô từ dưới lên
              if (belowOpenTop && ceilingMesh != null)
              {
                combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX, currentY, worldZ + halfTile), Quaternion.identity, new Vector3(0.5f, 1f, 0.25f)));
              }
            }
            if (wBottom)
            {
              if (!cBL && !cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX, currentY, worldZ - halfTile), Quaternion.identity, fullScale));
              else
              {
                if (!cBL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - quarterTile, currentY, worldZ - halfTile), Quaternion.identity, halfScale));
                if (!cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + quarterTile, currentY, worldZ - halfTile), Quaternion.identity, halfScale));
              }

              // Bịt kín đáy tường trên vòm cửa
              if (belowOpenBottom && ceilingMesh != null)
              {
                combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX, currentY, worldZ - halfTile), Quaternion.identity, new Vector3(0.5f, 1f, 0.25f)));
              }
            }
            if (wLeft)
            {
              if (!cTL && !cBL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - halfTile, currentY, worldZ), Quaternion.Euler(0, 90, 0), fullScale));
              else
              {
                if (!cBL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - halfTile, currentY, worldZ - quarterTile), Quaternion.Euler(0, 90, 0), halfScale));
                if (!cTL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - halfTile, currentY, worldZ + quarterTile), Quaternion.Euler(0, 90, 0), halfScale));
              }

              // Bịt kín đáy tường trên vòm cửa
              if (belowOpenLeft && ceilingMesh != null)
              {
                combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX - halfTile, currentY, worldZ), Quaternion.identity, new Vector3(0.25f, 1f, 0.5f)));
              }
            }
            if (wRight)
            {
              if (!cTR && !cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ), Quaternion.Euler(0, -90, 0), fullScale));
              else
              {
                if (!cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ - quarterTile), Quaternion.Euler(0, -90, 0), halfScale));
                if (!cTR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ + quarterTile), Quaternion.Euler(0, -90, 0), halfScale));
              }

              // Bịt kín đáy tường trên vòm cửa
              if (belowOpenRight && ceilingMesh != null)
              {
                combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX + halfTile, currentY, worldZ), Quaternion.identity, new Vector3(0.25f, 1f, 0.5f)));
              }
            }
          }
        }
      }
    }

    if (combineInstances.Count > 0)
    {
      Mesh combinedMesh = new Mesh();
      combinedMesh.name = type + "_CombinedMesh";
      combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

      GameObject areaObj = new GameObject(type + "_" + area.x + "_" + area.y);
      areaObj.transform.SetParent(dungeonParent, false);

      MeshFilter mf = areaObj.AddComponent<MeshFilter>();
      mf.sharedMesh = combinedMesh;

      MeshRenderer mr = areaObj.AddComponent<MeshRenderer>();
      mr.sharedMaterial = dungeonMaterial;

      MeshCollider mc = areaObj.AddComponent<MeshCollider>();
      mc.sharedMesh = combinedMesh;
      NavMeshModifier navMod = areaObj.AddComponent<NavMeshModifier>();
      navMod.ignoreFromBuild = true; // Bỏ qua trần nhà và hình học phòng visual khi bake NavMesh
    }
  }

  /// <summary>
  /// Dựng sàn gác lửng tầng 2 bằng kỹ thuật Sandwich:
  /// - Mặt trên: sàn gỗ Y = 4m
  /// - Mặt dưới: sàn gỗ Y = 3.65m lật úp ngược 180 độ
  /// - Mép hở: ốp khối viền floor_foundation_front
  /// - Dựng cột đỡ, lan can và cầu thang dẫn từ tầng 1 lên
  /// </summary>
  void BuildMezzanine(RoomData room, DungeonData data)
  {
    float mY = room.mezzanineHeight;           // 8.0m
    float halfTile = tileSize / 2f;

    GameObject mezzanineParent = new GameObject($"Mezzanine_Room_{room.id}");
    mezzanineParent.transform.SetParent(dungeonParent, false);
    mezzanineParent.layer = 10; // Đặt Layer 10 (NavMessFloor) để MeshCollider gộp của tầng 2 được NavMeshSurface nướng trực tiếp!

    List<CombineInstance> combineList = new List<CombineInstance>();

    Mesh woodMesh = GetMeshFromPrefab(mezzanineFloorPrefab);
    Mesh edgeMesh = GetMeshFromPrefab(mezzanineEdgePrefab);
    Mesh cornerMesh = GetMeshFromPrefab(mezzanineCornerPrefab);

    // Tự động tính toán scale sàn gỗ dựa trên kích thước mesh gốc (khớp với tileSize 2m)
    Vector3 woodScale = Vector3.one;
    if (woodMesh != null)
    {
      float nw = woodMesh.bounds.size.x;
      float nd = woodMesh.bounds.size.z;
      if (nw > 0.1f && nd > 0.1f)
      {
        woodScale = new Vector3(tileSize / nw, 1f, tileSize / nd);
      }
    }

    // TÍNH TOÁN KÍCH THƯỚC KHỐI VIỀN ĐÁ TỰ ĐỘNG THEO PREFAB
    // - Chiều cao beamY: Đáy đặt tại (mY - edgeHeight) để mặt trên phẳng khít với sàn tầng 2 (Y=8m)
    // - Sàn sandwich: Mặt dưới hạ xuống đúng beamY = 7.0m (vừa đủ 1 ô dày 1m), khớp khít với đáy ô viền và đỉnh cột!
    MeshFilter emf = (mezzanineEdgePrefab != null) ? mezzanineEdgePrefab.GetComponentInChildren<MeshFilter>() : null;
    float edgeScaleX = (emf != null) ? emf.transform.localScale.x : 0.5f;
    float edgeScaleY = (emf != null) ? emf.transform.localScale.y : 0.5f;
    float edgeNativeW = (edgeMesh != null && edgeMesh.bounds.size.x > 0.1f) ? edgeMesh.bounds.size.x : 2.0f;
    float edgeNativeH = (edgeMesh != null && edgeMesh.bounds.size.y > 0.1f) ? edgeMesh.bounds.size.y : 2.0f;

    float edgeWidth = edgeNativeW * edgeScaleX;   // Ví dụ 2.0 * 0.5 = 1.0m
    float edgeHeight = edgeNativeH * edgeScaleY; // Ví dụ 2.0 * 0.5 = 1.0m
    float beamY = mY - edgeHeight;               // 7.0m (Đáy khối viền và đỉnh cột đỡ)
    float mBottomY = beamY;                      // 7.0m (Mặt dưới sàn gỗ vừa đúng 1 ô dày, khớp với viền và cột)
    int subBlocks = (edgeWidth <= 1.1f) ? 2 : 1; // Rải 2 khối 1m để phủ kín cạnh ô tile 2m

    // 1. SÀN GỖ SANDWICH (Mặt trên Y=8m, Mặt dưới Y=7m lật úp - dày chuẩn 1 ô 1m)
    if (woodMesh != null)
    {
      foreach (var tile in room.mezzanineTiles)
      {
        float wx = tile.x * tileSize + halfTile;
        float wz = tile.y * tileSize + halfTile;

        // Mặt trên (Y=8.0m)
        combineList.Add(CreateCombineInstanceScaled(mezzanineFloorPrefab, woodMesh, new Vector3(wx, mY, wz), Quaternion.identity, woodScale));

        // Mặt dưới (Lật úp 180 độ nhìn từ dưới lên tại Y=7.0m)
        combineList.Add(CreateCombineInstanceScaled(mezzanineFloorPrefab, woodMesh, new Vector3(wx, mBottomY, wz), Quaternion.Euler(180, 0, 0), woodScale));
      }
    }

    Mesh bMesh = GetMeshFromPrefab(barrierPrefab);
    float barrierNativeW = (bMesh != null && bMesh.bounds.size.x > 0.1f) ? bMesh.bounds.size.x : tileSize;
    float barrierScaleX = tileSize / barrierNativeW;
    Vector3 barrierScale = new Vector3(barrierScaleX, 1f, 1f);

    List<CombineInstance> railingCombineList = new List<CombineInstance>();

    RectInt b = room.bounds;
    foreach (var tile in room.mezzanineTiles)
    {
      float wx = tile.x * tileSize + halfTile;
      float wz = tile.y * tileSize + halfTile;
      bool isStairLanding = room.stairLandingTiles.Contains(tile);

      // Chỉ coi là mép hở nẹp viền & lan can nếu mép đó nằm TRONG phòng và hướng xuống khoảng không tầng trệt.
      // TUYỆT ĐỐI KHÔNG sinh viền hay lan can nếu ô này nằm sát vách tường phòng!
      bool openS = (tile.y > b.y) && !room.IsInMezzanine(tile.x, tile.y - 1);
      bool openN = (tile.y < b.y + b.height - 1) && !room.IsInMezzanine(tile.x, tile.y + 1);
      bool openW = (tile.x > b.x) && !room.IsInMezzanine(tile.x - 1, tile.y);
      bool openE = (tile.x < b.x + b.width - 1) && !room.IsInMezzanine(tile.x + 1, tile.y);

      // Mép Nam
      if (openS)
      {
        bool skipForStairs = isStairLanding && (room.mezzanineSide == MezzanineWallSide.North);
        float edgeZ = tile.y * tileSize;
        if (!skipForStairs)
        {
          if (edgeMesh != null)
          {
            if (subBlocks == 2)
            {
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx - halfTile / 2f, beamY, edgeZ), Quaternion.Euler(0, 180, 0)));
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx + halfTile / 2f, beamY, edgeZ), Quaternion.Euler(0, 180, 0)));
            }
            else
            {
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx, beamY, edgeZ), Quaternion.Euler(0, 180, 0)));
            }
          }

          if (barrierPrefab != null && bMesh != null)
          {
            railingCombineList.Add(CreateCombineInstanceScaled(barrierPrefab, bMesh, new Vector3(wx, mY, edgeZ), Quaternion.Euler(0, 180, 0), barrierScale));
          }
        }
      }

      // Mép Bắc
      if (openN)
      {
        bool skipForStairs = isStairLanding && (room.mezzanineSide == MezzanineWallSide.South);
        float edgeZ = (tile.y + 1) * tileSize;
        if (!skipForStairs)
        {
          if (edgeMesh != null)
          {
            if (subBlocks == 2)
            {
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx - halfTile / 2f, beamY, edgeZ), Quaternion.identity));
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx + halfTile / 2f, beamY, edgeZ), Quaternion.identity));
            }
            else
            {
              combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(wx, beamY, edgeZ), Quaternion.identity));
            }
          }

          if (barrierPrefab != null && bMesh != null)
          {
            railingCombineList.Add(CreateCombineInstanceScaled(barrierPrefab, bMesh, new Vector3(wx, mY, edgeZ), Quaternion.identity, barrierScale));
          }
        }
      }

      // Mép Tây
      if (openW)
      {
        float edgeX = tile.x * tileSize;
        if (edgeMesh != null)
        {
          if (subBlocks == 2)
          {
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz - halfTile / 2f), Quaternion.Euler(0, -90, 0)));
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz + halfTile / 2f), Quaternion.Euler(0, -90, 0)));
          }
          else
          {
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz), Quaternion.Euler(0, -90, 0)));
          }
        }

        if (barrierPrefab != null && bMesh != null)
        {
          railingCombineList.Add(CreateCombineInstanceScaled(barrierPrefab, bMesh, new Vector3(edgeX, mY, wz), Quaternion.Euler(0, -90, 0), barrierScale));
        }
      }

      // Mép Đông
      if (openE)
      {
        float edgeX = (tile.x + 1) * tileSize;
        if (edgeMesh != null)
        {
          if (subBlocks == 2)
          {
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz - halfTile / 2f), Quaternion.Euler(0, 90, 0)));
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz + halfTile / 2f), Quaternion.Euler(0, 90, 0)));
          }
          else
          {
            combineList.Add(CreateCombineInstance(mezzanineEdgePrefab, edgeMesh, new Vector3(edgeX, beamY, wz), Quaternion.Euler(0, 90, 0)));
          }
        }

        if (barrierPrefab != null && bMesh != null)
        {
          railingCombineList.Add(CreateCombineInstanceScaled(barrierPrefab, bMesh, new Vector3(edgeX, mY, wz), Quaternion.Euler(0, 90, 0), barrierScale));
        }
      }
    }

    // 3. GỘP MESH SÀN TẦNG 2 VÀ KHUNG DẦM MÓNG
    if (combineList.Count > 0)
    {
      Mesh combinedMesh = new Mesh();
      combinedMesh.name = $"Mezzanine_CombinedMesh_{room.id}";
      combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

      MeshFilter mf = mezzanineParent.AddComponent<MeshFilter>();
      mf.sharedMesh = combinedMesh;

      MeshRenderer mr = mezzanineParent.AddComponent<MeshRenderer>();
      mr.sharedMaterial = dungeonMaterial;

      MeshCollider mc = mezzanineParent.AddComponent<MeshCollider>();
      mc.sharedMesh = combinedMesh;
    }

    // 4. GỘP MESH LAN CAN (RAILINGS) - 1 Mesh duy nhất toàn phòng, giảm hàng chục GameObject con
    if (railingCombineList.Count > 0)
    {
      GameObject railingsObj = new GameObject("Mezzanine_Railings");
      railingsObj.transform.SetParent(mezzanineParent.transform, false);

      Mesh combinedRailingMesh = new Mesh();
      combinedRailingMesh.name = $"Mezzanine_Railings_{room.id}";
      combinedRailingMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      combinedRailingMesh.CombineMeshes(railingCombineList.ToArray(), true, true);

      MeshFilter rmf = railingsObj.AddComponent<MeshFilter>();
      rmf.sharedMesh = combinedRailingMesh;

      MeshRenderer rmr = railingsObj.AddComponent<MeshRenderer>();
      Material bMat = GetMaterialFromPrefab(barrierPrefab) ?? dungeonMaterial;
      rmr.sharedMaterial = bMat;

      MeshCollider rmc = railingsObj.AddComponent<MeshCollider>();
      rmc.sharedMesh = combinedRailingMesh;
    }

    // 5. GỘP MESH CỘT ĐỠ CHỊU LỰC (PILLARS) - Vươn từ trệt (0m) lên đỡ đáy dầm đá (6m)
    if (pillarPrefab != null && room.pillarPositions != null && room.pillarPositions.Count > 0)
    {
      Mesh pMesh = GetMeshFromPrefab(pillarPrefab);
      float pillarScaleY = beamY / 4.0f; // Cột gốc cao 4m -> scale Y = 1.5 để chạm đáy dầm đá 6m
      Vector3 pillarScale = new Vector3(1f, pillarScaleY, 1f);

      if (pMesh != null)
      {
        List<CombineInstance> pillarCombineList = new List<CombineInstance>();
        foreach (var pPos in room.pillarPositions)
        {
          pillarCombineList.Add(CreateCombineInstanceScaled(pillarPrefab, pMesh, pPos, Quaternion.identity, pillarScale));
        }

        if (pillarCombineList.Count > 0)
        {
          GameObject pillarsObj = new GameObject("Mezzanine_Pillars");
          pillarsObj.transform.SetParent(mezzanineParent.transform, false);

          Mesh combinedPillarsMesh = new Mesh();
          combinedPillarsMesh.name = $"Mezzanine_Pillars_{room.id}";
          combinedPillarsMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
          combinedPillarsMesh.CombineMeshes(pillarCombineList.ToArray(), true, true);

          MeshFilter pmf = pillarsObj.AddComponent<MeshFilter>();
          pmf.sharedMesh = combinedPillarsMesh;

          MeshRenderer pmr = pillarsObj.AddComponent<MeshRenderer>();
          Material pMat = GetMaterialFromPrefab(pillarPrefab) ?? dungeonMaterial;
          pmr.sharedMaterial = pMat;

          MeshCollider pmc = pillarsObj.AddComponent<MeshCollider>();
          pmc.sharedMesh = combinedPillarsMesh;
        }
      }
      else
      {
        // Dự phòng nếu prefab cột không lấy được mesh filter
        foreach (var pPos in room.pillarPositions)
        {
          GameObject pillarObj = Instantiate(pillarPrefab, pPos, Quaternion.identity, mezzanineParent.transform);
          pillarObj.name = "Mezzanine_Pillar";
          pillarObj.transform.localScale = pillarScale;
        }
      }
    }

    // 5. DỰNG CẦU THANG 2 NHỊP 4M CHUẨN TỈ LỆ NGƯỜI VÀ BỆ ĐÁ STAIRS_FILL
    if (stairsPrefab != null)
    {
      MeshFilter smf = stairsPrefab.GetComponentInChildren<MeshFilter>();
      float targetWidth = 2f * tileSize; // Rộng 4m (2 ô tile)
      float meshWidth = (smf != null && smf.sharedMesh != null) ? smf.sharedMesh.bounds.size.x : 5.0f;
      float scaleX = targetWidth / meshWidth;

      // Scale Y=1.0 và Scale Z=1.0 GIỮ NGUYÊN BẢN GỐC để từng bậc thang cao chuẩn 0.35m và lan can cao 1.1m
      Vector3 flightScale = new Vector3(scaleX, 1.0f, 1.0f);

      // Nhịp dưới (0m -> 4m)
      Vector3 lowerPos = room.stairsLowerPosition + new Vector3(0, 0.025f, 0);
      GameObject lowerStairs = Instantiate(stairsPrefab, lowerPos, room.stairsRotation, mezzanineParent.transform);
      lowerStairs.name = "Mezzanine_Stairs_Lower";
      lowerStairs.transform.localScale = flightScale;

      // Nhịp trên (4m -> 8m) - Nâng nhẹ 0.05m để bậc trên cùng khớp phẳng khít tuyệt đối với mặt sàn gỗ (8.05m)
      Vector3 upperPos = room.stairsUpperPosition + new Vector3(0, 0.05f, 0);
      GameObject upperStairs = Instantiate(stairsPrefab, upperPos, room.stairsRotation, mezzanineParent.transform);
      upperStairs.name = "Mezzanine_Stairs_Upper";
      upperStairs.transform.localScale = flightScale;

      // Bệ đá stairs_fill bịt kín không gian dưới nhịp trên (dùng BoxCollider gốc đã chỉnh góc trên prefab của bạn)
      if (encloseStairsBase && stairsFillPrefab != null)
      {
        float stairCenterX = room.stairsPosition.x;
        float fillZ = (room.mezzanineSide == MezzanineWallSide.North) ? (room.stairsPosition.z - 2f) : (room.stairsPosition.z + 2f);
        Vector3 fillPos = new Vector3(stairCenterX, 0f, fillZ);
        GameObject fillObj = Instantiate(stairsFillPrefab, fillPos, room.stairsRotation, mezzanineParent.transform);
        fillObj.name = "Mezzanine_Stairs_Fill";
        // Sử dụng BoxCollider có sẵn trên Prefab do bạn căn chỉnh, TUYỆT ĐỐI không gắn NavMeshObstacle đè gây đứt quãng cầu thang!
      }

      // Tạo dốc va chạm MeshCollider liền mạch tuyệt đối (Continuous Ramp MeshCollider)
      // Nối từ mặt trệt (0m) lên đỉnh thang (8.05m) và phủ phẳng qua sàn đón (8.05m)
      // Loại bỏ hoàn toàn mọi gờ va chạm cạnh đứng, chạy hay đi bộ đều êm ru không bao giờ kẹt!
      CreateContinuousStairsRamp(room, mezzanineParent.transform, targetWidth);
    }
  }

  void CreateContinuousStairsRamp(RoomData room, Transform parent, float targetWidth)
  {
    GameObject rampObj = new GameObject("Stairs_ContinuousSlopeRamp");
    rampObj.transform.SetParent(parent, false);
    rampObj.layer = 10; // Ground / Floor layer (NavMessFloor)

    float halfW = targetWidth * 0.95f / 2f;
    float centerX = room.stairsPosition.x;
    float x0 = centerX - halfW;
    float x1 = centerX + halfW;
    float landingZ = room.stairsPosition.z;

    Mesh rampMesh = new Mesh();
    rampMesh.name = "ContinuousRampMesh";

    Vector3[] verts = new Vector3[6];
    int[] tris = new int[12];

    float rampRun = 8.3f; // Độ dài chân dốc 8.3m tạo độ dốc thoai thoải ~43.9 độ (< 45 độ chuẩn NavMesh)
    float rampTopY = room.mezzanineHeight; // 8.0m khớp phẳng khít tuyệt đối với mặt sàn gỗ tầng 2

    if (room.mezzanineSide == MezzanineWallSide.North)
    {
      // Thang leo từ Nam (landingZ - rampRun) lên Bắc (landingZ)
      verts[0] = new Vector3(x0, 0f, landingZ - rampRun);
      verts[1] = new Vector3(x1, 0f, landingZ - rampRun);
      verts[2] = new Vector3(x0, rampTopY, landingZ);
      verts[3] = new Vector3(x1, rampTopY, landingZ);
      verts[4] = new Vector3(x0, rampTopY, landingZ + tileSize);
      verts[5] = new Vector3(x1, rampTopY, landingZ + tileSize);

      // Quads hướng lên trên
      tris[0] = 0; tris[1] = 2; tris[2] = 1;
      tris[3] = 1; tris[4] = 2; tris[5] = 3;
      tris[6] = 2; tris[7] = 4; tris[8] = 3;
      tris[9] = 3; tris[10] = 4; tris[11] = 5;
    }
    else
    {
      // Thang leo từ Bắc (landingZ + rampRun) xuống Nam (landingZ)
      verts[0] = new Vector3(x0, 0f, landingZ + rampRun);
      verts[1] = new Vector3(x1, 0f, landingZ + rampRun);
      verts[2] = new Vector3(x0, rampTopY, landingZ);
      verts[3] = new Vector3(x1, rampTopY, landingZ);
      verts[4] = new Vector3(x0, rampTopY, landingZ - tileSize);
      verts[5] = new Vector3(x1, rampTopY, landingZ - tileSize);

      // Quads hướng lên trên
      tris[0] = 0; tris[1] = 1; tris[2] = 2;
      tris[3] = 1; tris[4] = 3; tris[5] = 2;
      tris[6] = 2; tris[7] = 3; tris[8] = 4;
      tris[9] = 3; tris[10] = 5; tris[11] = 4;
    }

    rampMesh.vertices = verts;
    rampMesh.triangles = tris;
    rampMesh.RecalculateNormals();
    rampMesh.RecalculateBounds();

    MeshCollider mc = rampObj.AddComponent<MeshCollider>();
    mc.sharedMesh = rampMesh;
  }

  void CreateInvisibleCollider(RectInt area)
  {
    if (invisibleFloorPrefab == null) return;
    float centerX = (area.x * tileSize) + ((area.width * tileSize) / 2f);
    float centerZ = (area.y * tileSize) + ((area.height * tileSize) / 2f);
    Vector3 centerPos = new Vector3(centerX, 0f, centerZ);

    GameObject invFloor = Instantiate(invisibleFloorPrefab, centerPos, Quaternion.identity, dungeonParent);
    invFloor.transform.localScale = new Vector3(area.width * tileSize, 0.2f, area.height * tileSize);
  }

  Mesh GetMeshFromPrefab(GameObject prefab)
  {
    if (prefab == null) return null;
    MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
    return mf != null ? mf.sharedMesh : null;
  }

  Material GetMaterialFromPrefab(GameObject prefab)
  {
    if (prefab == null) return null;
    MeshRenderer mr = prefab.GetComponentInChildren<MeshRenderer>();
    return mr != null ? mr.sharedMaterial : null;
  }

  CombineInstance CreateCombineInstance(GameObject prefab, Mesh mesh, Vector3 position, Quaternion rotation)
  {
    Matrix4x4 prefabMatrix = Matrix4x4.identity;
    if (prefab != null)
    {
      MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
      if (mf != null) prefabMatrix = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, mf.transform.localScale);
    }
    return new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, Vector3.one) * prefabMatrix };
  }

  CombineInstance CreateCombineInstanceWall(GameObject prefab, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scaleMultiplier)
  {
    Matrix4x4 prefabMatrix = Matrix4x4.identity;
    if (prefab != null)
    {
      MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
      if (mf != null)
      {
        Vector3 finalScale = Vector3.Scale(mf.transform.localScale, scaleMultiplier);
        Vector3 centerOffset = -mesh.bounds.center;
        centerOffset.y = 0;
        Matrix4x4 centering = Matrix4x4.Translate(centerOffset);
        Matrix4x4 scaleRot = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, finalScale);
        prefabMatrix = scaleRot * centering;
      }
    }
    return new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, Vector3.one) * prefabMatrix };
  }

  CombineInstance CreateCombineInstanceScaled(GameObject prefab, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scaleMultiplier)
  {
    Matrix4x4 prefabMatrix = Matrix4x4.identity;
    if (prefab != null)
    {
      MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
      if (mf != null)
      {
        Vector3 finalScale = Vector3.Scale(mf.transform.localScale, scaleMultiplier);
        prefabMatrix = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, finalScale);
      }
    }
    return new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, Vector3.one) * prefabMatrix };
  }

  void EnsureBarrierCollider(GameObject barrierObj, float width)
  {
    if (barrierObj == null) return;
    BoxCollider col = barrierObj.GetComponent<BoxCollider>();
    if (col == null)
    {
      col = barrierObj.AddComponent<BoxCollider>();
      col.size = new Vector3(width, 1.2f, 0.4f);
      col.center = new Vector3(0f, 0.6f, 0f);
    }
  }

  /// <summary>
  /// Tự động Bake NavMeshSurface toàn map và kiểm định tính liên thông đường đi (Pathfinding) giữa sàn trệt và tầng 2
  /// </summary>
  public void BakeAndValidateNavMesh(DungeonData data)
  {
    if (navMeshSurface == null)
    {
      navMeshSurface = dungeonParent != null ? dungeonParent.GetComponent<NavMeshSurface>() : null;
      if (navMeshSurface == null) navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
    }

    if (navMeshSurface == null)
    {
      Debug.LogWarning("<color=yellow>[NavMesh]</color> Không tìm thấy component NavMeshSurface để nướng hầm ngục!");
      return;
    }

    // Đảm bảo cấu hình thu thập chuẩn xác:
    // - Chỉ thu thập các GameObject con của dungeonParent (DungeonEnviroment)
    // - Chỉ thu thập các collider thuộc Layer 10 (NavMessFloor: sàn trệt, sàn tầng 2, cầu thang)
    navMeshSurface.collectObjects = CollectObjects.Children;
    navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
    navMeshSurface.layerMask = (1 << 10) | (1 << 0); // Layer 10: NavMessFloor, Layer 0: Props vật cản

    // Nướng NavMesh Surface đồng bộ
    navMeshSurface.BuildNavMesh();

    NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
    int vertCount = triangulation.vertices != null ? triangulation.vertices.Length : 0;
    int triCount = triangulation.indices != null ? triangulation.indices.Length / 3 : 0;

    Debug.Log($"<color=cyan>[NavMesh]</color> Bake thành công! Tổng số đỉnh: <b>{vertCount}</b>, số tam giác: <b>{triCount}</b>.");

    debugNavMeshPaths.Clear();

    // Kiểm tra tính liên thông đường đi (Pathfinding Validation) cho tất cả các phòng có tầng 2
    if (data != null && data.roomDataList != null)
    {
      int mezzanineCount = 0;
      int connectedCount = 0;

      foreach (var r in data.roomDataList)
      {
        if (r.hasMezzanine)
        {
          mezzanineCount++;

          // Điểm xuất phát A: Dưới sàn trệt ngay trước chân cầu thang (chân dốc 8.3m)
          Vector3 groundPos = (r.mezzanineSide == MezzanineWallSide.North)
            ? new Vector3(r.stairsPosition.x, 0.1f, r.stairsPosition.z - 9.0f)
            : new Vector3(r.stairsPosition.x, 0.1f, r.stairsPosition.z + 9.0f);

          // Điểm đích B: Trên sàn tầng 2 sau khi bước lên khỏi cầu thang
          Vector3 floor2Pos = (r.mezzanineSide == MezzanineWallSide.North)
            ? new Vector3(r.stairsPosition.x, r.mezzanineHeight + 0.1f, r.stairsPosition.z + tileSize)
            : new Vector3(r.stairsPosition.x, r.mezzanineHeight + 0.1f, r.stairsPosition.z - tileSize);

          NavMeshPath path = new NavMeshPath();
          bool hasSampleA = NavMesh.SamplePosition(groundPos, out NavMeshHit hitA, 3.0f, NavMesh.AllAreas);
          bool hasSampleB = NavMesh.SamplePosition(floor2Pos, out NavMeshHit hitB, 3.0f, NavMesh.AllAreas);

          if (hasSampleA && hasSampleB)
          {
            bool pathFound = NavMesh.CalculatePath(hitA.position, hitB.position, NavMesh.AllAreas, path);
            if (pathFound && path.status == NavMeshPathStatus.PathComplete)
            {
              connectedCount++;
              debugNavMeshPaths.Add(path.corners);

              float pathDist = 0f;
              for (int p = 1; p < path.corners.Length; p++)
              {
                pathDist += Vector3.Distance(path.corners[p - 1], path.corners[p]);
              }
              Debug.Log($"<color=green>[NavMesh Kết Nối OK]</color> Phòng {r.id}: Lộ trình liên tầng thông suốt! Dài: <b>{pathDist:F1}m</b> qua <b>{path.corners.Length}</b> mốc.");
            }
            else
            {
              Debug.LogWarning($"<color=orange>[NavMesh Warning]</color> Phòng {r.id}: Lộ trình leo thang chưa hoàn chỉnh (Status: {path.status}).");
            }
          }
          else
          {
            Debug.LogWarning($"<color=orange>[NavMesh Warning]</color> Phòng {r.id}: Không lấy được mẫu NavMesh ở chân thang ({hasSampleA}) hoặc tầng 2 ({hasSampleB}).");
          }
        }
      }

      if (mezzanineCount > 0)
      {
        Debug.Log($"<color=cyan>[NavMesh Tổng Kết]</color> Đã kiểm tra <b>{connectedCount}/{mezzanineCount}</b> phòng gác lửng kết nối leo thang lên tầng 2 thành công!");
      }
    }
  }
}
