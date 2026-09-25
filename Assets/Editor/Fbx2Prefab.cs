using UnityEngine;
using UnityEditor;
using System.IO;

public class Fbx2Prefab : EditorWindow
{
  // Bạn có thể đổi tên thư mục gom Prefab tại đây (Ví dụ: "Assets/MyDungeonPrefabs")
  private static string targetFolder = "Assets/DungeonPrefabs";

  [MenuItem("Tools/Convert Selected FBX to Prefabs Folder")]
  public static void ConvertFBXToPrefabs()
  {
    // 1. Kiểm tra xem thư mục đích đã tồn tại chưa, nếu chưa thì tự động tạo mới
    if (!AssetDatabase.IsValidFolder(targetFolder))
    {
      // Tách chữ "Assets" và tên thư mục con để dùng hàm CreateFolder
      string parentFolder = "Assets";
      string newFolderName = targetFolder.Replace("Assets/", "");
      AssetDatabase.CreateFolder(parentFolder, newFolderName);
    }

    // 2. Lấy danh sách các file đang được bạn bôi đen chọn trong ô Project
    Object[] selectedObjects = Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets);
    int count = 0;

    foreach (Object obj in selectedObjects)
    {
      string assetPath = AssetDatabase.GetAssetPath(obj);

      // Lọc ra đúng các file định dạng .fbx
      if (assetPath.ToLower().EndsWith(".fbx"))
      {
        // Tạo một bản sao tạm thời ảo ngay trên bộ nhớ (không làm rác Scene của bạn)
        GameObject fbxInstance = PrefabUtility.InstantiatePrefab(obj) as GameObject;
        if (fbxInstance != null)
        {
          // Lấy tên của file fbx (bỏ phần đuôi mở rộng)
          string fbxName = obj.name;

          // Thiết lập đường dẫn lưu file mới chạy thẳng vào thư mục đích chuyên biệt
          string prefabPath = $"{targetFolder}/{fbxName}.prefab";

          // Tiến hành lưu thành file Prefab chuẩn độc lập (.prefab xanh hoàn toàn)
          PrefabUtility.SaveAsPrefabAsset(fbxInstance, prefabPath);

          // Xóa bản sao tạm thời để giải phóng bộ nhớ
          DestroyImmediate(fbxInstance);
          count++;
        }
      }
    }

    // 3. Làm mới lại hệ thống dữ liệu của Unity để hiển thị file ngay lập tức
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log($"<color=green><b>Thành công!</b></color> Đã chuyển đổi {count} file FBX và gom tất cả vào thư mục: {targetFolder}");
  }
}
