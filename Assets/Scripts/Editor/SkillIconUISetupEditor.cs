#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InfiniteLabyrinth.Editor
{
  public static class SkillIconUISetupEditor
  {
    private const string CANVAS_PREFAB_PATH = "Assets/UI RPG/Canvas.prefab";

    [InitializeOnLoadMethod]
    private static void AutoRunSetupOnce()
    {
      EditorApplication.delayCall += () =>
      {
        if (!EditorPrefs.GetBool("SkillIconUI_AutoSetup_v1", false))
        {
          SetupSkillIconUI();
          EditorPrefs.SetBool("SkillIconUI_AutoSetup_v1", true);
        }
      };
    }

    [MenuItem("Infinite Labyrinth/UI/1. Cấu hình Skill Icon UI (1-Click)", false, 10)]
    public static void SetupSkillIconUI()
    {
      GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CANVAS_PREFAB_PATH);
      if (canvasPrefab == null)
      {
        Debug.LogWarning($"[SkillIconUI Setup] Không tìm thấy Canvas prefab tại: {CANVAS_PREFAB_PATH}");
        return;
      }

      string assetPath = AssetDatabase.GetAssetPath(canvasPrefab);
      GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

      try
      {
        Transform skillIconT = prefabRoot.transform.Find("SkillIconUI");
        if (skillIconT == null)
        {
          Debug.LogWarning("[SkillIconUI Setup] Không tìm thấy GameObject 'SkillIconUI' trong Canvas!");
          return;
        }

        SkillIconUI script = skillIconT.GetComponent<SkillIconUI>();
        if (script == null)
        {
          script = skillIconT.gameObject.AddComponent<SkillIconUI>();
        }

        script.AutoFindComponents();
        EditorUtility.SetDirty(prefabRoot);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        Debug.Log("<color=green>[SkillIconUI Setup]</color> ✅ Đã gắn và tự động liên kết thành công SkillIconUI vào Canvas.prefab!");
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"[SkillIconUI Setup] Lỗi: {ex.Message}");
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(prefabRoot);
      }
    }
  }
}
#endif
