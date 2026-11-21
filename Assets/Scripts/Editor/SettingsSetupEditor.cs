using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Bộ công cụ 1-Click tự động hóa cấu hình Prefab SettingUI và gắn các component điều khiển.
  /// </summary>
  public static class SettingsSetupEditor
  {
    private const string SETTING_PREFAB_PATH = "Assets/UI RPG/SettingUI.prefab";

    [MenuItem("Infinite Labyrinth/UI/1. Cấu Hình SettingUI Prefab (1-Click)", priority = 100)]
    public static void SetupSettingUIPrefab()
    {
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SETTING_PREFAB_PATH);
      if (prefab == null)
      {
        Debug.LogError($"[SettingsSetupEditor] Không tìm thấy prefab tại đường dẫn: {SETTING_PREFAB_PATH}");
        return;
      }

      string path = AssetDatabase.GetAssetPath(prefab);
      GameObject root = PrefabUtility.LoadPrefabContents(path);

      try
      {
        // 1. Gắn SettingsUIController lên root
        SettingsUIController controller = root.GetComponent<SettingsUIController>();
        if (controller == null)
        {
          controller = root.AddComponent<SettingsUIController>();
          Debug.Log("[SettingsSetupEditor] Đã gắn component SettingsUIController lên SettingUI.");
        }

        // 2. Gắn ToggleSwitchUI lên 4 Toggles
        SetupToggle(root, "SettingsScrollView/Viewport/GraphicsUI/Vsync/ToggleVsync");
        SetupToggle(root, "SettingsScrollView/Viewport/Display/ScreenShake/ToggleVsync");
        SetupToggle(root, "SettingsScrollView/Viewport/Display/DmgNumbers/ToggleVsync");
        SetupToggle(root, "SettingsScrollView/Viewport/Controls/MouseInvertY/ToggleVsync");

        // 3. Gọi AutoFindReferences để nối dây 100% tự động
        controller.AutoFindReferences();

        // 4. Lưu lại Prefab
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log("<color=green>[SettingsSetupEditor] ✅ Đã cấu hình và lưu SettingUI.prefab thành công 100%!</color>");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[SettingsSetupEditor] Lỗi khi cấu hình SettingUI: {ex.Message}\n{ex.StackTrace}");
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static void SetupToggle(GameObject root, string path)
    {
      Transform t = root.transform.Find(path);
      if (t != null)
      {
        ToggleSwitchUI toggle = t.GetComponent<ToggleSwitchUI>();
        if (toggle == null)
        {
          toggle = t.gameObject.AddComponent<ToggleSwitchUI>();
        }
        toggle.AutoFindReferences();
        EditorUtility.SetDirty(toggle);
      }
      else
      {
        Debug.LogWarning($"[SettingsSetupEditor] Không tìm thấy Toggle tại đường dẫn: {path}");
      }
    }
  }
}
