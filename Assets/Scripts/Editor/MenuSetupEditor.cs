using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Bộ công cụ 1-Click tự động hóa cấu hình và liên kết toàn bộ:
  /// 1. Cập nhật Build Settings (MainMenuScene Index 0, LandScene Index 1, DungeonScene Index 2).
  /// 2. Bổ sung nút Đóng [X] cho SettingUI.prefab.
  /// 3. Cấu hình PauseCanvas.prefab (gắn PauseMenuController, lồng SettingUI).
  /// 4. Cấu hình MainMenuScene.unity (gắn MainMenuController, lồng SettingUI).
  /// </summary>
  public static class MenuSetupEditor
  {
    private const string SCENE_MAIN_MENU = "Assets/Scenes/MainMenuScene.unity";
    private const string SCENE_LAND = "Assets/Scenes/LandScene.unity";
    private const string SCENE_DUNGEON = "Assets/Scenes/DungeonScene.unity";
    private const string PREFAB_PAUSE_CANVAS = "Assets/UI RPG/PauseCanvas.prefab";
    private const string PREFAB_SETTING_UI = "Assets/UI RPG/SettingUI.prefab";

    [MenuItem("Infinite Labyrinth/UI/2. Nối Dây Toàn Bộ Menus & Build Settings (1-Click)", priority = 101)]
    public static void SetupAllMenusAndBuildSettings()
    {
      Debug.Log("<color=cyan>=== BẮT ĐẦU CẤU HÌNH VÀ LIÊN KẾT TOÀN BỘ MENUS ===</color>");

      // 1. Cấu hình Build Settings
      SetupBuildSettings();

      // 2. Thêm nút Close [X] vào SettingUI.prefab
      EnsureCloseButtonOnSettingUI();

      // 3. Cấu hình PauseCanvas.prefab
      SetupPauseCanvasPrefab();

      // 4. Cấu hình MainMenuScene.unity
      SetupMainMenuScene();

      Debug.Log("<color=green>✅ HOÀN THÀNH 100%: MainMenu, PauseMenu và SettingUI đã được liên kết thông suốt!</color>");
    }

    public static void SetupBuildSettings()
    {
      List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
      {
        new EditorBuildSettingsScene(SCENE_MAIN_MENU, true),
        new EditorBuildSettingsScene(SCENE_LAND, true),
        new EditorBuildSettingsScene(SCENE_DUNGEON, true)
      };

      EditorBuildSettings.scenes = scenes.ToArray();
      Debug.Log($"[MenuSetupEditor] ✅ Đã cập nhật Build Settings: Index 0: MainMenuScene, Index 1: LandScene, Index 2: DungeonScene.");
    }

    public static void EnsureCloseButtonOnSettingUI()
    {
      string path = PREFAB_SETTING_UI;
      GameObject root = PrefabUtility.LoadPrefabContents(path);
      try
      {
        Transform btnClose = root.transform.Find("BtnClose");
        if (btnClose == null)
        {
          GameObject btnObj = new GameObject("BtnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
          btnObj.transform.SetParent(root.transform, false);

          RectTransform rt = btnObj.GetComponent<RectTransform>();
          rt.anchorMin = new Vector2(1, 1);
          rt.anchorMax = new Vector2(1, 1);
          rt.pivot = new Vector2(1, 1);
          rt.anchoredPosition = new Vector2(-28, -28);
          rt.sizeDelta = new Vector2(40, 40);

          Image img = btnObj.GetComponent<Image>();
          img.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

          // Tạo text X
          GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
          textObj.transform.SetParent(btnObj.transform, false);

          RectTransform textRt = textObj.GetComponent<RectTransform>();
          textRt.anchorMin = Vector2.zero;
          textRt.anchorMax = Vector2.one;
          textRt.sizeDelta = Vector2.zero;

          TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
          tmp.text = "✕";
          tmp.fontSize = 22;
          tmp.alignment = TextAlignmentOptions.Center;
          tmp.color = new Color(1.0f, 0.88f, 0.45f, 1.0f); // Vàng kim sáng

          Debug.Log("[MenuSetupEditor] Đã tạo nút BtnClose [✕] cho SettingUI.");
        }

        var controller = root.GetComponent<SettingsUIController>();
        if (controller != null) controller.AutoFindReferences();

        PrefabUtility.SaveAsPrefabAsset(root, path);
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    public static void SetupPauseCanvasPrefab()
    {
      string path = PREFAB_PAUSE_CANVAS;
      GameObject root = PrefabUtility.LoadPrefabContents(path);
      try
      {
        // 1. Gắn PauseMenuController lên PauseCanvas root
        PauseMenuController controller = root.GetComponent<PauseMenuController>();
        if (controller == null)
        {
          controller = root.AddComponent<PauseMenuController>();
        }

        // 2. Kiểm tra SettingUI con
        Transform settingChild = root.transform.Find("SettingUI");
        if (settingChild == null)
        {
          GameObject settingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_SETTING_UI);
          if (settingPrefab != null)
          {
            GameObject settingInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingPrefab, root.transform);
            settingInstance.name = "SettingUI";

            RectTransform rt = settingInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
              rt.anchorMin = Vector2.zero;
              rt.anchorMax = Vector2.one;
              rt.offsetMin = Vector2.zero;
              rt.offsetMax = Vector2.zero;
            }

            settingInstance.SetActive(false);
            Debug.Log("[MenuSetupEditor] Đã nhúng SettingUI vào PauseCanvas.prefab.");
          }
        }

        controller.AutoFindReferences();
        EditorUtility.SetDirty(controller);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log("[MenuSetupEditor] ✅ Đã cấu hình và lưu PauseCanvas.prefab thành công!");
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    public static void SetupMainMenuScene()
    {
      // Lưu scene hiện tại nếu có thay đổi
      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

      var scene = EditorSceneManager.OpenScene(SCENE_MAIN_MENU, OpenSceneMode.Single);
      try
      {
        GameObject canvasObj = GameObject.Find("MainMenuCanvas");
        if (canvasObj == null)
        {
          Debug.LogError("[MenuSetupEditor] Không tìm thấy MainMenuCanvas trong MainMenuScene!");
          return;
        }

        // 1. Gắn MainMenuController
        MainMenuController controller = canvasObj.GetComponent<MainMenuController>();
        if (controller == null)
        {
          controller = canvasObj.AddComponent<MainMenuController>();
        }

        // 2. Nhúng SettingUI nếu chưa có
        Transform settingChild = canvasObj.transform.Find("SettingUI");
        if (settingChild == null)
        {
          GameObject settingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_SETTING_UI);
          if (settingPrefab != null)
          {
            GameObject settingInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingPrefab, canvasObj.transform);
            settingInstance.name = "SettingUI";

            RectTransform rt = settingInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
              rt.anchorMin = Vector2.zero;
              rt.anchorMax = Vector2.one;
              rt.offsetMin = Vector2.zero;
              rt.offsetMax = Vector2.zero;
            }

            settingInstance.SetActive(false);
            Debug.Log("[MenuSetupEditor] Đã nhúng SettingUI vào MainMenuCanvas.");
          }
        }

        // 3. Tự động liên kết tham chiếu
        controller.AutoFindReferences();
        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MenuSetupEditor] ✅ Đã cấu hình và lưu MainMenuScene.unity thành công!");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[MenuSetupEditor] Lỗi cấu hình MainMenuScene: {ex.Message}");
      }
    }
  }
}
