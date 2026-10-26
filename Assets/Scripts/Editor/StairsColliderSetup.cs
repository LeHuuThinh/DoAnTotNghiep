using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class StairsColliderSetup
{
    private const string WEDGE_MESH_PATH = "Assets/Prefabs/Dungeon/DungeonPrefabs/Stairs_Wedge_Mesh.asset";
    private const string STAIRS_PREFAB_PATH = "Assets/Prefabs/Dungeon/DungeonPrefabs/stairs.prefab";

    static StairsColliderSetup()
    {
        EditorApplication.delayCall += EnsureStairsCollider;
    }

    [MenuItem("Infinite Labyrinth/Cầu Thang/1. Tạo Mesh & Gán Collider Tam Giác Vào stairs.prefab")]
    public static void EnsureStairsCollider()
    {
        // 1. Tạo Mesh lăng trụ tam giác vuông chuẩn khớp 100% với model stairs.fbx
        Mesh wedgeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(WEDGE_MESH_PATH);
        if (wedgeMesh == null)
        {
            wedgeMesh = CreateStairsWedgeMesh();
            AssetDatabase.CreateAsset(wedgeMesh, WEDGE_MESH_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>[StairsColliderSetup]</color> Đã tạo file Mesh tam giác tại: {WEDGE_MESH_PATH}");
        }
        else
        {
            UpdateMeshGeometry(wedgeMesh);
            EditorUtility.SetDirty(wedgeMesh);
            AssetDatabase.SaveAssets();
        }

        // 2. Nạp stairs.prefab và gắn MeshCollider
        if (!File.Exists(STAIRS_PREFAB_PATH))
        {
            Debug.LogWarning($"<color=yellow>[StairsColliderSetup]</color> Không tìm thấy prefab: {STAIRS_PREFAB_PATH}");
            return;
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(STAIRS_PREFAB_PATH);
        if (prefabContents != null)
        {
            try
            {
                MeshCollider mc = prefabContents.GetComponent<MeshCollider>();
                if (mc == null) mc = prefabContents.AddComponent<MeshCollider>();

                mc.sharedMesh = wedgeMesh;
                mc.convex = true; // Solid convex 3D wedge volume (không bao giờ bị lọt/xuyên qua)

                PrefabUtility.SaveAsPrefabAsset(prefabContents, STAIRS_PREFAB_PATH);
                Debug.Log("<color=green>[StairsColliderSetup]</color> Đã gán thành công MeshCollider tam giác vào stairs.prefab!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }
    }

    public static void UpdateMeshGeometry(Mesh mesh)
    {
        mesh.Clear();

        // Kích thước chuẩn theo model stairs.fbx gốc: Rộng X: [-2.5, 2.5] (5m), Dài Z: [0, 4] (4m), Cao Y: [0, 4] (4m)
        // Chân thang ở Z = 4.0m (Y = 0m). Đỉnh thang ở Z = 0.0m (Y = 4.0m).
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-2.5f, 0.0f, 4.0f), // 0: Chân thang trái (Foot Left)
            new Vector3( 2.5f, 0.0f, 4.0f), // 1: Chân thang phải (Foot Right)
            new Vector3(-2.5f, 0.0f, 0.0f), // 2: Đáy sau trái (Back Bottom Left)
            new Vector3( 2.5f, 0.0f, 0.0f), // 3: Đáy sau phải (Back Bottom Right)
            new Vector3(-2.5f, 4.0f, 0.0f), // 4: Đỉnh sau trái (Back Top Left)
            new Vector3( 2.5f, 4.0f, 0.0f)  // 5: Đỉnh sau phải (Back Top Right)
        };

        int[] triangles = new int[]
        {
            // 1. Mặt dốc nghiêng (từ chân Z=4 lên đỉnh Z=0) - normal hướng lên và ra ngoài (+Y, +Z)
            0, 5, 4,
            0, 1, 5,

            // 2. Mặt đáy phẳng sàn (Y=0) - normal hướng xuống (-Y)
            0, 3, 1,
            0, 2, 3,

            // 3. Mặt đứng phía sau (Z=0) - normal hướng ra sau (-Z)
            2, 4, 5,
            2, 5, 3,

            // 4. Mặt tam giác bên hông trái (X=-2.5) - normal hướng sang trái (-X)
            0, 4, 2,

            // 5. Mặt tam giác bên hông phải (X=+2.5) - normal hướng sang phải (+X)
            1, 3, 5
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public static Mesh CreateStairsWedgeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Stairs_Wedge_Collider";
        UpdateMeshGeometry(mesh);
        return mesh;
    }

    /// <summary>
    /// Vẽ Gizmo hiển thị khung dây màu xanh lục trong Scene view cho các cầu thang có gắn collider này,
    /// giúp người dùng nhìn thấy rõ 100% hình khối collider có khớp với bậc thang hay không.
    /// </summary>
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    private static void DrawStairsColliderGizmo(MeshCollider mc, GizmoType gizmoType)
    {
        if (mc == null || mc.sharedMesh == null) return;
        if (!mc.sharedMesh.name.Contains("Stairs_Wedge") && !mc.sharedMesh.name.Contains("stairs_wedge")) return;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.85f);
        Gizmos.matrix = mc.transform.localToWorldMatrix;
        Gizmos.DrawWireMesh(mc.sharedMesh);

        // Vẽ một lớp bán trong suốt nhẹ để dễ nhìn mặt dốc
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawMesh(mc.sharedMesh);
    }
}
