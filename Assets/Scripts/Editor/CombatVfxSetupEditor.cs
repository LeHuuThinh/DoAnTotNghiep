#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tool Editor tự động cấu hình Prefab Hit VFX cho Player (màu đỏ) và Toàn bộ Quái vật (màu xám).
  /// </summary>
  public static class CombatVfxSetupEditor
  {
    private const string PLAYER_HIT_VFX_PATH = "Assets/Characters/Player/GetHitVFX.prefab";
    private const string MONSTER_HIT_VFX_PATH = "Assets/Characters/Monsters/MobsGetHitVFX.prefab";
    private const string PLAYER_PREFAB_PATH = "Assets/Characters/Player/Player.prefab";
    private const string PREF_KEY = "CombatVfx_AutoSetup_v1";

    private static readonly string[] MONSTER_PREFABS = new[]
    {
      "Assets/Characters/Monsters/SkeletonWarrior.prefab",
      "Assets/Characters/Monsters/SkeletonMinion.prefab",
      "Assets/Characters/Monsters/SkeletonRogue.prefab",
      "Assets/Characters/Monsters/SkeletonMage.prefab"
    };

    [InitializeOnLoadMethod]
    private static void AutoRunSetupOnce()
    {
      EditorApplication.delayCall += () =>
      {
        if (!EditorPrefs.GetBool(PREF_KEY, false))
        {
          Debug.Log("<color=cyan>[CombatVfxSetup]</color> 🚀 Tự động cấu hình Hit VFX lần đầu cho Player và Monsters...");
          SetupAllCombatHitVfx();
          EditorPrefs.SetBool(PREF_KEY, true);
        }
      };
    }

    [MenuItem("Infinite Labyrinth/Combat/1. Cấu Hình Hit VFX Cho Player & Quái Vật (1-Click)", false, 1)]
    public static void SetupAllCombatHitVfx()
    {
      Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH HIT VFX CHO PLAYER VÀ MONSTERS ===</color>");

      // 1. Tải Prefab VFX
      GameObject playerVfx = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_HIT_VFX_PATH);
      GameObject monsterVfx = AssetDatabase.LoadAssetAtPath<GameObject>(MONSTER_HIT_VFX_PATH);

      if (playerVfx == null)
      {
        Debug.LogError($"[CombatVfxSetup] ❌ Không tìm thấy Player Hit VFX tại: {PLAYER_HIT_VFX_PATH}");
        return;
      }

      if (monsterVfx == null)
      {
        Debug.LogError($"[CombatVfxSetup] ❌ Không tìm thấy Monster Hit VFX tại: {MONSTER_HIT_VFX_PATH}");
        return;
      }

      int successCount = 0;

      // 2. Cấu hình Player Prefab
      if (SetupPlayerPrefab(playerVfx))
      {
        successCount++;
      }

      // 3. Cấu hình 4 Quái vật Skeletons
      foreach (string monsterPath in MONSTER_PREFABS)
      {
        if (SetupMonsterPrefab(monsterPath, monsterVfx))
        {
          successCount++;
        }
      }

      // 4. Cấu hình Scene Instances nếu đang mở Scene
      SetupSceneInstances(playerVfx, monsterVfx);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>=== HOÀN TẤT CẤU HÌNH HIT VFX! ĐÃ CẬP NHẬT {successCount}/{1 + MONSTER_PREFABS.Length} PREFABS ===</color>");
    }

    private static bool SetupPlayerPrefab(GameObject vfxPrefab)
    {
      GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
      if (playerAsset == null)
      {
        Debug.LogWarning($"[CombatVfxSetup] ⚠️ Không tìm thấy Player prefab tại: {PLAYER_PREFAB_PATH}");
        return false;
      }

      using (var editScope = new PrefabUtility.EditPrefabContentsScope(PLAYER_PREFAB_PATH))
      {
        GameObject root = editScope.prefabContentsRoot;
        PlayerFeedback feedback = root.GetComponentInChildren<PlayerFeedback>();
        if (feedback == null)
        {
          feedback = root.AddComponent<PlayerFeedback>();
        }

        SerializedObject so = new SerializedObject(feedback);
        SerializedProperty prop = so.FindProperty("hitVfxPrefab");
        if (prop != null)
        {
          prop.objectReferenceValue = vfxPrefab;
          so.ApplyModifiedProperties();
          Debug.Log($"[CombatVfxSetup]  Đã gắn GetHitVFX (đỏ) vào Player.prefab");
          return true;
        }
      }

      return false;
    }

    private static bool SetupMonsterPrefab(string prefabPath, GameObject vfxPrefab)
    {
      GameObject monsterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      if (monsterAsset == null)
      {
        Debug.LogWarning($"[CombatVfxSetup] ⚠️ Không tìm thấy Monster prefab tại: {prefabPath}");
        return false;
      }

      using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
      {
        GameObject root = editScope.prefabContentsRoot;
        EnemyFeedback feedback = root.GetComponentInChildren<EnemyFeedback>();
        if (feedback == null)
        {
          feedback = root.AddComponent<EnemyFeedback>();
        }

        SerializedObject so = new SerializedObject(feedback);
        SerializedProperty prop = so.FindProperty("hitVfxPrefab");
        if (prop != null)
        {
          prop.objectReferenceValue = vfxPrefab;
          so.ApplyModifiedProperties();
          Debug.Log($"[CombatVfxSetup]  Đã gắn MobsGetHitVFX (xám) vào: {System.IO.Path.GetFileName(prefabPath)}");
          return true;
        }
      }

      return false;
    }

    private static void SetupSceneInstances(GameObject playerVfx, GameObject monsterVfx)
    {
      PlayerFeedback[] playerFeedbacks = Object.FindObjectsByType<PlayerFeedback>(FindObjectsSortMode.None);
      foreach (var pf in playerFeedbacks)
      {
        SerializedObject so = new SerializedObject(pf);
        SerializedProperty prop = so.FindProperty("hitVfxPrefab");
        if (prop != null && prop.objectReferenceValue == null)
        {
          prop.objectReferenceValue = playerVfx;
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(pf.gameObject);
          Debug.Log($"[CombatVfxSetup]  Đã gắn GetHitVFX (đỏ) vào Scene Player instance: {pf.gameObject.name}");
        }
      }

      EnemyFeedback[] enemyFeedbacks = Object.FindObjectsByType<EnemyFeedback>(FindObjectsSortMode.None);
      foreach (var ef in enemyFeedbacks)
      {
        SerializedObject so = new SerializedObject(ef);
        SerializedProperty prop = so.FindProperty("hitVfxPrefab");
        if (prop != null && prop.objectReferenceValue == null)
        {
          prop.objectReferenceValue = monsterVfx;
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(ef.gameObject);
          Debug.Log($"[CombatVfxSetup]  Đã gắn MobsGetHitVFX (xám) vào Scene Enemy instance: {ef.gameObject.name}");
        }
      }
    }
  }
}
#endif
