#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.EditorScripts
{
  [CustomEditor(typeof(BossVictoryDirector))]
  public class BossVictoryDirectorEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      BossVictoryDirector director = (BossVictoryDirector)target;

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("=== CÔNG CỤ ĐẠO DIỄN CHIẾN THẮNG (TASK D02-5B & D02-5C) ===", EditorStyles.boldLabel);

      if (GUILayout.Button("🔗 Tự Động Quét & Liên Kết UI BossVictoryBanner", GUILayout.Height(28)))
      {
        director.EnsureUIReferences();
        EditorUtility.SetDirty(director);
        Debug.Log("<color=green>[BossVictoryDirector]</color> ✅ Đã quét và liên kết thành công UI BossVictoryBanner từ EnemyCanvas!");
      }

      if (GUILayout.Button("🏛️ Nạp Prefab Rương Boss & Cổng Dịch Chuyển (Task D02-5C)", GUILayout.Height(28)))
      {
        director.EnsurePedestalPrefabs();
        GameObject ped = director.FindBossPedestal();
        if (ped != null)
        {
          Debug.Log($"<color=green>[BossVictoryDirector]</color> ✅ Đã tìm thấy Bục Đá {ped.name}!");
        }
        else
        {
          Debug.Log("<color=yellow>[BossVictoryDirector]</color> ℹ️ Chưa có Bục Đá trong Scene hiện tại (sẽ tự động sinh tại runtime bởi PCG).");
        }
        EditorUtility.SetDirty(director);
      }

      EditorGUILayout.Space(5);

      using (new EditorGUI.DisabledScope(!Application.isPlaying))
      {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🏆 Test Toàn Bộ Chuỗi (Banner + Bục 12m)", GUILayout.Height(34)))
        {
          director.TestVictorySequence();
        }

        if (GUILayout.Button("🔄 Reset", GUILayout.Height(34)))
        {
          director.ResetVictorySequence();
        }

        EditorGUILayout.EndHorizontal();
      }

      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox("Nhấn Play Mode (hoặc bấm phím K khi đấu Boss) để kiểm thử toàn bộ chuỗi: Hitlag kết liễu -> Camera góc 3/4 -> Biểu ngữ VICTORY ACHIEVED -> Lia Cam lên Bục Đá 12m sinh Rương Boss & Cổng Dịch Chuyển -> Trả Cam về Player.", MessageType.Info);
      }
    }
  }

  /// <summary>
  /// Tool Editor 1-click thiết lập toàn bộ hệ thống Đạo Diễn Chiến Thắng & Victory Banner (Task D02-5B & D02-5C)
  /// </summary>
  public static class BossVictorySetupEditor
  {
    [MenuItem("Infinite Labyrinth/Boss/8. Cấu Hình Đạo Diễn Chiến Thắng & Victory Banner (Task D02-5B)", false, 8)]
    public static void SetupBossVictory()
    {
      Debug.Log("<color=gold>=== BẮT ĐẦU CẤU HÌNH BOSS VICTORY DIRECTOR (TASK D02-5B) ===</color>");

      // 1. Kiểm tra hoặc tạo BossVictoryDirector trong Scene
      BossVictoryDirector director = Object.FindFirstObjectByType<BossVictoryDirector>();
      if (director == null)
      {
        GameObject directorObj = new GameObject("BossVictoryDirector");
        director = directorObj.AddComponent<BossVictoryDirector>();
        Undo.RegisterCreatedObjectUndo(directorObj, "Create BossVictoryDirector");
        Debug.Log("<color=green>[BossVictorySetup]</color> ✅ Đã tạo GameObject BossVictoryDirector trong Scene.");
      }
      else
      {
        Debug.Log("<color=yellow>[BossVictorySetup]</color> ℹ️ Scene đã có BossVictoryDirector.");
      }

      // 2. Tự động liên kết UI
      director.EnsureUIReferences();
      director.EnsurePedestalPrefabs();
      EditorUtility.SetDirty(director);

      Selection.activeGameObject = director.gameObject;
      Debug.Log("<color=green>[BossVictorySetup]</color> 🎉 Cấu hình Task D02-5B hoàn tất 100%! Đã chọn BossVictoryDirector trong Hierarchy.");
    }

    [MenuItem("Infinite Labyrinth/Boss/9. Cấu Hình Đạo Diễn Sinh Rương & Cổng Bục Đá (Task D02-5C)", false, 9)]
    public static void SetupPedestalRevealAndChestPortal()
    {
      Debug.Log("<color=gold>=== BẮT ĐẦU CẤU HÌNH ĐẠO DIỄN SINH RƯƠNG & CỔNG BỤC ĐÁ (TASK D02-5C) ===</color>");

      // 1. Cấu hình BossVictoryDirector trong Scene
      BossVictoryDirector director = Object.FindFirstObjectByType<BossVictoryDirector>();
      if (director == null)
      {
        GameObject directorObj = new GameObject("BossVictoryDirector");
        director = directorObj.AddComponent<BossVictoryDirector>();
        Undo.RegisterCreatedObjectUndo(directorObj, "Create BossVictoryDirector");
        Debug.Log("<color=green>[BossVictorySetup]</color> ✅ Đã tạo GameObject BossVictoryDirector trong Scene.");
      }

      director.EnsureUIReferences();
      director.EnsurePedestalPrefabs();

      // 2. Lưu lại assets
      EditorUtility.SetDirty(director);
      AssetDatabase.SaveAssets();
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(director.gameObject.scene);

      Selection.activeGameObject = director.gameObject;
      Debug.Log("<color=green>[BossVictorySetup]</color> 🎉 Cấu hình Task D02-5C hoàn tất 100%! Đạo diễn Bục Đá 12m, Rương Boss & Cổng Dịch Chuyển đã sẵn sàng.");
    }
  }
}
#endif
