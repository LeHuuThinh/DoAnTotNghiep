using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Module gộp Mesh tĩnh và tối ưu hóa Draw Call cho đạo cụ hầm ngục.
/// Gom các MeshFilter theo Material, tạo 32-bit mesh (1 Draw Call duy nhất per material),
/// tắt shadow casting và lọc bỏ MeshCollider cho cờ vải/thủy tinh để tối ưu PhysX CPU.
/// </summary>
public static class DungeonStaticPropCombiner
{
  /// <summary>
  /// Gộp toàn bộ Mesh của đồ tĩnh và templates trong phòng theo Material để giảm triệt để Draw Call (1 Draw Call duy nhất)
  /// </summary>
  public static void CombineStaticPropsInRoom(GameObject tempRoot, Transform targetParent, int roomId)
  {
    MeshFilter[] filters = tempRoot.GetComponentsInChildren<MeshFilter>();
    if (filters == null || filters.Length == 0)
    {
      if (Application.isPlaying) Object.Destroy(tempRoot);
      else Object.DestroyImmediate(tempRoot);
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
      cmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Đồ tĩnh hầm ngục không cần tạo bóng thêm

      // Chỉ tạo MeshCollider cho các nhóm vật liệu kiến trúc, nội thất gỗ/đá; bỏ qua cờ vải, chai lọ, đĩa chén và đá vụn nhỏ để giảm tải PhysX và không làm vấp chân người chơi
      string matName = (mat != null) ? mat.name.ToLower() : "";
      bool isNonCollidingMat = matName.Contains("banner") || matName.Contains("cloth") || matName.Contains("flag") || matName.Contains("glass")
          || matName.Contains("rubble") || matName.Contains("plate") || matName.Contains("dish") || matName.Contains("bottle");
      if (!isNonCollidingMat)
      {
        MeshCollider cmc = combinedObj.AddComponent<MeshCollider>();
        cmc.sharedMesh = combinedMesh;

        // Đánh dấu toàn bộ vật thể props có collider là Not Walkable để NavMeshSurface tự động đục lỗ, ngăn quái đi xuyên qua
        Unity.AI.Navigation.NavMeshModifier navMod = combinedObj.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
        navMod.overrideArea = true;
        navMod.area = 1; // 1 = Not Walkable
      }
    }

    if (Application.isPlaying) Object.Destroy(tempRoot);
    else Object.DestroyImmediate(tempRoot);
  }
}
