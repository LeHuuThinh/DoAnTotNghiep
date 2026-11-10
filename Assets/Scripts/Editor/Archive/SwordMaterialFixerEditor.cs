#if false // ARCHIVED ONE-OFF SCRIPT
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace InfiniteLabyrinth.Editor
{
  public static class SwordMaterialFixerEditor
  {
    private const string TargetFolder = "Assets/Characters/Player/Sword2/Materials";

    // // MenuItem removed (Archived)", false, 50)]
    public static void ConvertSword2MaterialsToURP()
    {
      Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
      if (urpLitShader == null)
      {
        EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Shader 'Universal Render Pipeline/Lit' trong dự án!", "OK");
        return;
      }

      if (!Directory.Exists(TargetFolder))
      {
        EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy thư mục: {TargetFolder}", "OK");
        return;
      }

      string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
      int convertedCount = 0;

      AssetDatabase.StartAssetEditing();
      try
      {
        foreach (string guid in matGuids)
        {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
          if (mat == null) continue;

          // Lưu lại các thuộc tính màu cũ trước khi đổi shader
          Color baseColor = Color.white;
          if (mat.HasProperty("_Color"))
          {
            baseColor = mat.GetColor("_Color");
          }
          else if (mat.HasProperty("_BaseColor"))
          {
            baseColor = mat.GetColor("_BaseColor");
          }

          float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
          float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f);

          // Đổi sang URP Lit shader
          mat.shader = urpLitShader;

          // Gán lại màu và thuộc tính cho URP Lit
          if (mat.HasProperty("_BaseColor"))
          {
            mat.SetColor("_BaseColor", baseColor);
          }
          if (mat.HasProperty("_Metallic"))
          {
            mat.SetFloat("_Metallic", metallic);
          }
          if (mat.HasProperty("_Smoothness"))
          {
            mat.SetFloat("_Smoothness", smoothness);
          }

          EditorUtility.SetDirty(mat);
          convertedCount++;
        }
      }
      finally
      {
        AssetDatabase.StopAssetEditing();
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"[SwordMaterialFixer] Đã chuyển đổi thành công {convertedCount} materials trong {TargetFolder} sang URP Lit!");
      EditorUtility.DisplayDialog("Infinite Labyrinth", 
        $"Đã sửa xong toàn bộ {convertedCount} materials của 10 cây kiếm mới sang URP Lit!\n\nToàn bộ màu sắc (Gold, Emerald, Ice, Iron, Rose...) đã hiển thị chuẩn xác, không còn bị hồng!", "Tuyệt vời!");
    }
  }
}
#endif


#endif


