#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tool Editor 1-click thiết lập toàn bộ hệ thống Cutscene Intro và Boss HUD (Task D02-4)
  /// </summary>
  public static class BossIntroSetupEditor
  {
    [MenuItem("Infinite Labyrinth/Boss/7. Cấu Hình Cutscene Intro & Boss HUD (Task D02-4)", false, 7)]
    public static void SetupBossIntroAndHUD()
    {
      Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH BOSS INTRO & HUD (TASK D02-4) ===</color>");

      // 1. Kiểm tra EnemyCanvas và gắn BossHUDController
      GameObject enemyCanvas = GameObject.Find("EnemyCanvas");
      if (enemyCanvas != null)
      {
        BossHUDController hudCtrl = enemyCanvas.GetComponent<BossHUDController>();
        if (hudCtrl == null)
        {
          hudCtrl = enemyCanvas.AddComponent<BossHUDController>();
          EditorUtility.SetDirty(enemyCanvas);
          Debug.Log("<color=green>[SetupEditor]</color> ✅ Đã gắn component BossHUDController lên EnemyCanvas.");
        }
        else
        {
          Debug.Log("<color=yellow>[SetupEditor]</color> ℹ️ EnemyCanvas đã có BossHUDController.");
        }
      }
      else
      {
        Debug.LogWarning("[SetupEditor] ⚠️ Không tìm thấy EnemyCanvas trong Scene hiện tại!");
      }

      // 2. Kiểm tra hoặc tạo BossIntroDirector trong Scene
      BossIntroDirector director = Object.FindFirstObjectByType<BossIntroDirector>();
      if (director == null)
      {
        GameObject directorObj = new GameObject("BossIntroDirector");
        director = directorObj.AddComponent<BossIntroDirector>();
        Undo.RegisterCreatedObjectUndo(directorObj, "Create BossIntroDirector");
        Debug.Log("<color=green>[SetupEditor]</color> ✅ Đã tạo GameObject BossIntroDirector trong Scene.");
      }
      else
      {
        Debug.Log("<color=yellow>[SetupEditor]</color> ℹ️ Scene đã có BossIntroDirector.");
      }

      // 3. Kiểm tra Prefab BossPedestal
      GameObject pedestalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/BossPedestal.prefab");
      if (pedestalPrefab != null)
      {
        Debug.Log($"<color=green>[SetupEditor]</color> ✅ Đã xác nhận Prefab BossPedestal: {AssetDatabase.GetAssetPath(pedestalPrefab)}");
      }
      else
      {
        Debug.LogError("[SetupEditor] ❌ Không tìm thấy Prefab BossPedestal tại Assets/Prefabs/Dungeon/DungeonPrefabs/BossPedestal.prefab!");
      }

      // 4. Kiểm tra Prefab Boss_IceGolem
      GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Boss/IceGolem/Boss_IceGolem.prefab");
      if (bossPrefab != null)
      {
        Debug.Log($"<color=green>[SetupEditor]</color> ✅ Đã xác nhận Prefab Boss_IceGolem: {AssetDatabase.GetAssetPath(bossPrefab)}");
      }
      else
      {
        Debug.LogError("[SetupEditor] ❌ Không tìm thấy Prefab Boss_IceGolem tại Assets/Characters/Boss/IceGolem/Boss_IceGolem.prefab!");
      }

      // 5. Lưu Scene
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
      );

      Debug.Log("<color=cyan>=== HOÀN TẤT THIẾT LẬP TASK D02-4! SẴN SÀNG THỬ NGHIỆM PLAY MODE! ===</color>");
    }
  }
}
#endif
