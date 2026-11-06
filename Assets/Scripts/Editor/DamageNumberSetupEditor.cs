#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using InfiniteLabyrinth.UI.Combat;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Công cụ Editor 1-Click: Tự động khởi tạo Prefab số sát thương và cấu hình cho DamageNumberManager
  /// </summary>
  public static class DamageNumberSetupEditor
  {
    [MenuItem("Infinite Labyrinth/Combat/3. Tạo Prefab Số Sát Thương (DamageNumberItem) (1-Click)", false, 30)]
    public static void SetupDamageNumbers()
    {
      // 1. Đảm bảo thư mục lưu Prefab tồn tại
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs/RPG"))
      {
        AssetDatabase.CreateFolder("Assets/Prefabs", "RPG");
      }

      string prefabPath = "Assets/Prefabs/RPG/DamageNumberItem.prefab";
      GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

      if (prefabObj == null)
      {
        GameObject tempGo = new GameObject("DamageNumberItem");
        TextMeshPro tmp = tempGo.AddComponent<TextMeshPro>();
        tmp.fontSize = 7.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.sortingOrder = 2000;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Color Gradient Presets/VecnaBold-4YY4 SDF");
        if (font != null)
        {
          tmp.font = font;
        }

        DamageNumberItem item = tempGo.AddComponent<DamageNumberItem>();
        prefabObj = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
        Object.DestroyImmediate(tempGo);
        Debug.Log($"<color=green>[DamageNumberSetupEditor]</color> ✅ Đã tạo thành công Prefab tại: {prefabPath}");
      }
      else
      {
        Debug.Log($"<color=cyan>[DamageNumberSetupEditor]</color> ℹ️ Prefab đã tồn tại tại: {prefabPath}");
      }

      // 2. Tìm Manager trong Scene (nếu có) và liên kết Prefab
      DamageNumberManager manager = Object.FindFirstObjectByType<DamageNumberManager>();
      if (manager != null && prefabObj != null)
      {
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty prop = so.FindProperty("damageNumberPrefab");
        if (prop != null)
        {
          prop.objectReferenceValue = prefabObj.GetComponent<DamageNumberItem>();
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(manager);
          Debug.Log("<color=green>[DamageNumberSetupEditor]</color> ✅ Đã gán Prefab vào [DamageNumberManager] trong Scene!");
        }
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      EditorUtility.DisplayDialog("Infinite Labyrinth", "Đã cấu hình hoàn tất Prefab Số Sát Thương (DamageNumberItem)!\n\nPrefab được lưu tại: Assets/Prefabs/RPG/DamageNumberItem.prefab", "OK");
    }
  }
}
#endif
