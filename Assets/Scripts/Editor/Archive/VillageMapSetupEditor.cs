using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.EditorTools
{
  /// <summary>
  /// Công cụ tự động hóa thiết lập Hệ Thống Bản Đồ Làng Haven (Village Map - LandScene - Task POL-04):
  /// 1. Tự động tìm hoặc tạo GameObject VillageMap dưới Canvas trong Scene
  /// 2. Gắn và cấu hình component VillageMapManager
  /// 3. Tự động nạp sprite VillageMap.png và Player_Icon.png
  /// 4. Thiết lập chuẩn các thông số (minZoom=1, maxZoom=3, rotatePlayerMarker=false, showLandmarkLabels=false)
  /// 5. Dựng sẵn toàn bộ hierarchy UI: Background Dimmer, Viewport Mask, MapContent, Landmarks, PlayerMarker
  /// </summary>
  public static class VillageMapSetupEditor
  {
    private const string VILLAGE_MAP_SPRITE_PATH = "Assets/UI RPG/GameImage/VillageMap.png";
    private const string PLAYER_ICON_PATH = "Assets/UI RPG/Icon/Map/Player_Icon.png";

    // [MenuItem("Infinite Labyrinth/RPG/8. Thiết Lập Bản Đồ Làng Haven (1-Click)", false, 108)]
    public static void SetupVillageMap()
    {
      Debug.Log("<b>[VillageMapSetup]</b> Bắt đầu thiết lập Bản Đồ Làng Haven...");

      // 1. Tìm Canvas chính trong Scene
      GameObject canvasObj = GameObject.Find("Canvas");
      if (canvasObj == null)
      {
        var c = Object.FindFirstObjectByType<Canvas>();
        if (c != null) canvasObj = c.gameObject;
      }

      if (canvasObj == null)
      {
        Debug.LogError("<b>[VillageMapSetup]</b> Không tìm thấy Canvas trong Scene hiện tại!");
        return;
      }

      Undo.RecordObject(canvasObj, "Setup VillageMap");

      // 2. Tìm hoặc tạo GameObject VillageMap
      Transform mapTr = canvasObj.transform.Find("VillageMap");
      GameObject mapObj;
      if (mapTr == null)
      {
        mapObj = new GameObject("VillageMap", typeof(RectTransform), typeof(CanvasGroup));
        mapObj.transform.SetParent(canvasObj.transform, false);
        Debug.Log("<b>[VillageMapSetup]</b> Đã tạo mới GameObject 'VillageMap'.");
      }
      else
      {
        mapObj = mapTr.gameObject;
      }

      Undo.RecordObject(mapObj, "Configure VillageMap");

      // Đưa xuống vị trí đầu tiên (dưới cùng của Canvas để không đè HUD/Inventory)
      mapObj.transform.SetAsFirstSibling();

      // Cấu hình RectTransform phủ toàn màn hình
      RectTransform rt = mapObj.GetComponent<RectTransform>();
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.anchoredPosition = Vector2.zero;
      rt.sizeDelta = Vector2.zero;
      rt.pivot = new Vector2(0.5f, 0.5f);

      // Cấu hình CanvasGroup
      CanvasGroup cg = mapObj.GetComponent<CanvasGroup>();
      if (cg == null) cg = mapObj.AddComponent<CanvasGroup>();
      cg.alpha = 0f;
      cg.interactable = false;
      cg.blocksRaycasts = false;

      // 3. Gắn và cấu hình VillageMapManager
      VillageMapManager mapManager = mapObj.GetComponent<VillageMapManager>();
      if (mapManager == null)
      {
        mapManager = mapObj.AddComponent<VillageMapManager>();
        Debug.Log("<b>[VillageMapSetup]</b> Đã gắn VillageMapManager vào VillageMap.");
      }

      // Nạp các Sprites
      mapManager.villageMapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VILLAGE_MAP_SPRITE_PATH);
      mapManager.playerIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PLAYER_ICON_PATH);

      // Cấu hình các thông số chuẩn xác theo yêu cầu
      mapManager.villageSceneName = "LandScene";
      mapManager.toggleKey = KeyCode.M;
      mapManager.freezePlayerWhenOpen = false;
      mapManager.unlockCursorWhenOpen = true;
      mapManager.centerOnPlayerOnOpen = true;
      mapManager.defaultZoomOnOpen = 1.5f;
      mapManager.backgroundDimAlpha = 0.85f;

      mapManager.mapImageSize = new Vector2(2624f, 1632f);
      mapManager.playerIconSize = 80f;
      mapManager.landmarkIconSize = 80f;
      mapManager.rotatePlayerMarker = false; // Yêu cầu 3: không xoay
      mapManager.showLandmarkLabels = false; // Yêu cầu 1: ẩn chữ

      mapManager.zoomSpeed = 0.15f;
      mapManager.minZoom = 1.0f; // Yêu cầu 2: min zoom 1.0
      mapManager.maxZoom = 3.0f; // Yêu cầu 2: max zoom 3.0

      // 4. Tự động dựng hierarchy con
      mapManager.BuildMapHierarchy();
      mapManager.ApplyLandmarkSettings();

      EditorUtility.SetDirty(mapObj);
      EditorUtility.SetDirty(mapManager);
      EditorSceneManager.MarkSceneDirty(mapObj.scene);

      Debug.Log("<color=#44FF44><b>[VillageMapSetup] THÀNH CÔNG 100%!</b></color> Đã thiết lập hoàn chỉnh Bản Đồ Làng Haven:");
      Debug.Log($"• Ảnh nền bản đồ: {VILLAGE_MAP_SPRITE_PATH} ({mapManager.villageMapSprite != null})");
      Debug.Log($"• Icon Player: {PLAYER_ICON_PATH} ({mapManager.playerIconSprite != null})");
      Debug.Log($"• Phím nóng: [M] để bật/tắt trong LandScene!");
    }
  }
}
