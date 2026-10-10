using UnityEngine;
using System.Collections.Generic;

public class DungeonRenderer : MonoBehaviour
{
  [Header("Cài đặt 3D Prefabs")]
  // Đổi floorPrefab đơn lẻ thành mảng để kéo được nhiều variant
  public GameObject[] floorPrefabs;
  public GameObject wallPrefab;
  public GameObject cornerPrefab;
  public GameObject ceilingPrefab;
  public GameObject invisibleFloorPrefab;
  public Transform dungeonParent;

  [Header("Hình học Dungeon")]
  public Material dungeonMaterial;
  public float tileSize = 2f;
  public float ceilingHeight = 8f;
  public float wallHeight = 4f;

  [Header("Đa dạng Sàn (Floor Variation)")]
  [Tooltip("Càng nhỏ thì sàn cùng loại tạo thành mảng càng lớn. Càng lớn thì sàn trộn càng nhuyễn.")]
  public float floorNoiseScale = 0.2f;
  public void BuildMesh(DungeonData data)
  {
    if (dungeonParent == null) return;
    ClearGeneratedDungeon();

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

  void CreateCombinedAreaMesh(RectInt area, string type, DungeonData data)
  {
    List<CombineInstance> combineInstances = new List<CombineInstance>();

    // 1. Lưu sẵn danh sách Mesh của tất cả các loại sàn để tối ưu hiệu năng
    Mesh[] floorMeshes = new Mesh[floorPrefabs.Length];
    for (int i = 0; i < floorPrefabs.Length; i++)
    {
      floorMeshes[i] = GetMeshFromPrefab(floorPrefabs[i]);
    }

    Mesh wallMesh = GetMeshFromPrefab(wallPrefab);
    Mesh cornerMesh = GetMeshFromPrefab(cornerPrefab);
    Mesh ceilingMesh = GetMeshFromPrefab(ceilingPrefab);

    float halfTile = tileSize / 2f;

    // Tọa độ bù trừ để Perlin Noise không bị lặp đối xứng ở gốc tọa độ (0,0)
    float noiseOffsetX = 10000f;
    float noiseOffsetY = 10000f;

    for (int x = area.x; x < area.x + area.width; x++)
    {
      for (int y = area.y; y < area.y + area.height; y++)
      {
        if (!data.IsInBounds(x, y) || data.mapGrid[x, y] != 1) continue;

        float worldX = x * tileSize + halfTile;
        float worldZ = y * tileSize + halfTile;

        // --- SINH SÀN ĐA DẠNG BẰNG PERLIN NOISE ---
        if (floorPrefabs.Length > 0)
        {
          // Lấy giá trị Noise (từ 0.0 đến 1.0) dựa trên tọa độ X, Y của lưới
          float noise = Mathf.PerlinNoise((x + noiseOffsetX) * floorNoiseScale, (y + noiseOffsetY) * floorNoiseScale);

          // Chuyển giá trị Noise thành số thứ tự Index (từ 0 đến tổng số variant - 1)
          int floorIndex = Mathf.Clamp(Mathf.FloorToInt(noise * floorPrefabs.Length), 0, floorPrefabs.Length - 1);

          Mesh selectedFloorMesh = floorMeshes[floorIndex];
          if (selectedFloorMesh != null)
          {
            combineInstances.Add(CreateCombineInstance(floorPrefabs[floorIndex], selectedFloorMesh, new Vector3(worldX, 0, worldZ), Quaternion.identity));
          }
        }
        if (ceilingMesh != null)
          combineInstances.Add(CreateCombineInstanceScaled(ceilingPrefab, ceilingMesh, new Vector3(worldX, ceilingHeight, worldZ), Quaternion.identity, new Vector3(0.5f, 1f, 0.5f)));

        int wallLayers = Mathf.RoundToInt(ceilingHeight / wallHeight);
        for (int i = 0; i < wallLayers; i++)
        {
          float currentY = i * wallHeight;

          bool wLeft = IsWall(x - 1, y, data);
          bool wRight = IsWall(x + 1, y, data);
          bool wBottom = IsWall(x, y - 1, data);
          bool wTop = IsWall(x, y + 1, data);

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
            }
            if (wBottom)
            {
              if (!cBL && !cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX, currentY, worldZ - halfTile), Quaternion.identity, fullScale));
              else
              {
                if (!cBL) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX - quarterTile, currentY, worldZ - halfTile), Quaternion.identity, halfScale));
                if (!cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + quarterTile, currentY, worldZ - halfTile), Quaternion.identity, halfScale));
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
            }
            if (wRight)
            {
              if (!cTR && !cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ), Quaternion.Euler(0, -90, 0), fullScale));
              else
              {
                if (!cBR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ - quarterTile), Quaternion.Euler(0, -90, 0), halfScale));
                if (!cTR) combineInstances.Add(CreateCombineInstanceWall(wallPrefab, wallMesh, new Vector3(worldX + halfTile, currentY, worldZ + quarterTile), Quaternion.Euler(0, -90, 0), halfScale));
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
    }
  }

  bool IsWall(int x, int y, DungeonData data)
  {
    if (!data.IsInBounds(x, y)) return true;
    return data.mapGrid[x, y] == 0;
  }

  Mesh GetMeshFromPrefab(GameObject prefab)
  {
    if (prefab == null) return null;
    MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
    return mf != null ? mf.sharedMesh : null;
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

  void CreateInvisibleCollider(RectInt area)
  {
    float centerX = (area.x * tileSize) + ((area.width * tileSize) / 2f);
    float centerZ = (area.y * tileSize) + ((area.height * tileSize) / 2f);
    Vector3 centerPos = new Vector3(centerX, -0.2f, centerZ);

    GameObject invFloor = Instantiate(invisibleFloorPrefab, centerPos, Quaternion.identity, dungeonParent);
    invFloor.transform.localScale = new Vector3(area.width * tileSize, 0.2f, area.height * tileSize);
  }
}