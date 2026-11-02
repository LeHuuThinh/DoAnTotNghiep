using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  [CustomEditor(typeof(VictoryPortalUI))]
  public class VictoryPortalUIEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      VictoryPortalUI ui = (VictoryPortalUI)target;

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("=== CÔNG CỤ TỰ ĐỘNG THIẾT LẬP (TASK D02-5D) ===", EditorStyles.boldLabel);

      if (GUILayout.Button("🔄 Tự Động Kết Nối & Cấu Hình Mask Vát Góc (Auto-Wire)", GUILayout.Height(32)))
      {
        Undo.RecordObject(ui.gameObject, "Auto Wire VictoryPortalUI");
        SetupGameObject(ui.gameObject);
        EditorUtility.SetDirty(ui.gameObject);
        Debug.Log("<color=green>[VictoryPortalUIEditor]</color> ✅ Đã cấu hình thành công Mask vát góc & toàn bộ tham chiếu cho VictoryPortalUI!");
      }

      EditorGUILayout.Space(6);
      EditorGUILayout.LabelField("=== KIỂM THỬ RUNTIME ===", EditorStyles.boldLabel);

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("▶️ Mở Giao Diện (Test Open)", GUILayout.Height(26)))
      {
        if (Application.isPlaying)
        {
          ui.Open();
        }
        else
        {
          CanvasGroup cg = ui.GetComponent<CanvasGroup>();
          if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        }
      }

      if (GUILayout.Button("⏹️ Đóng Giao Diện (Test Close)", GUILayout.Height(26)))
      {
        if (Application.isPlaying)
        {
          ui.Close();
        }
        else
        {
          CanvasGroup cg = ui.GetComponent<CanvasGroup>();
          if (cg != null) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
        }
      }
      EditorGUILayout.EndHorizontal();
    }

    [MenuItem("Infinite Labyrinth/UI/Thiết Lập Victory Portal Choice UI (Task D02-5D)", false, 20)]
    [MenuItem("Tools/Infinite Labyrinth/Setup Victory Portal UI")]
    public static void SetupVictoryPortalUIMenuItem()
    {
      // 1. Kiểm tra trong Canvas.prefab
      string prefabPath = "Assets/UI RPG/Canvas.prefab";
      GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
      if (prefabRoot != null)
      {
        Transform vpTrans = prefabRoot.transform.Find("VictoryPortalUI");
        if (vpTrans != null)
        {
          SetupGameObject(vpTrans.gameObject);
          PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
          Debug.Log("<color=green>[VictoryPortalUIEditor]</color> ✅ Đã cấu hình và lưu thành công VictoryPortalUI trong Canvas.prefab!");
        }
        PrefabUtility.UnloadPrefabContents(prefabRoot);
      }

      // 2. Kiểm tra trong Scene đang mở
      VictoryPortalUI ui = Object.FindFirstObjectByType<VictoryPortalUI>(FindObjectsInactive.Include);
      if (ui != null)
      {
        SetupGameObject(ui.gameObject);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Debug.Log("<color=green>[VictoryPortalUIEditor]</color> ✅ Đã thiết lập thành công VictoryPortalUI trong Scene!");
      }
      else
      {
        Debug.Log("<color=yellow>[VictoryPortalUIEditor]</color> ℹ️ VictoryPortalUI đã được cập nhật trực tiếp vào Canvas.prefab.");
      }
    }

    public static void SetupGameObject(GameObject rootObj)
    {
      Undo.RecordObject(rootObj, "Setup VictoryPortalUI");

      CanvasGroup cg = rootObj.GetComponent<CanvasGroup>();
      if (cg == null) cg = rootObj.AddComponent<CanvasGroup>();
      cg.alpha = 0f;
      cg.blocksRaycasts = false;
      cg.interactable = false;

      VictoryPortalUI ui = rootObj.GetComponent<VictoryPortalUI>();
      if (ui == null) ui = rootObj.AddComponent<VictoryPortalUI>();

      // Đổi tên typo HoidFill -> HoldFill nếu có
      Transform nextFloorBtn = rootObj.transform.Find("NextFloorPanel/BtnNextFloor");
      if (nextFloorBtn != null)
      {
        Transform hoidFill = nextFloorBtn.Find("HoidFill");
        if (hoidFill != null)
        {
          hoidFill.name = "HoldFill";
        }
      }

      // 1. Cấu hình Mask Stencil vát góc (Beveled Corners) chuẩn xác ôm khít Panel
      foreach (var mask in rootObj.GetComponentsInChildren<Mask>(true))
      {
        GameObject maskObj = mask.gameObject;

        // Xóa RectMask2D nếu có để không xung đột
        RectMask2D rectMask = maskObj.GetComponent<RectMask2D>();
        if (rectMask != null)
        {
          if (Application.isPlaying) Object.Destroy(rectMask);
          else Object.DestroyImmediate(rectMask);
        }

        // Bật Mask và tắt showMaskGraphic để Unity dùng ColorMask = 0 (chỉ ghi Stencil vát góc, không vẽ pixel trắng)
        mask.enabled = true;
        mask.showMaskGraphic = false;

        Image maskImg = maskObj.GetComponent<Image>();
        if (maskImg != null)
        {
          maskImg.enabled = true;
          maskImg.color = Color.white;
          maskImg.raycastTarget = false;
        }
      }

      // 2. Bảo đảm hình ảnh TownImg hiển thị sắc nét 100%
      Transform townImgTrans = rootObj.transform.Find("TownPanel/Mask/TownImg");
      if (townImgTrans != null)
      {
        townImgTrans.gameObject.SetActive(true);
        Image img = townImgTrans.GetComponent<Image>();
        if (img != null)
        {
          img.enabled = true;
          img.color = new Color(1f, 1f, 1f, 1f);
          img.raycastTarget = false;
        }
      }

      // 3. Bảo đảm hình ảnh DungeonImg hiển thị sắc nét 100%
      Transform dungeonImgTrans = rootObj.transform.Find("NextFloorPanel/Mask/DungeonImg");
      if (dungeonImgTrans != null)
      {
        dungeonImgTrans.gameObject.SetActive(true);
        Image img = dungeonImgTrans.GetComponent<Image>();
        if (img != null)
        {
          img.enabled = true;
          img.color = new Color(1f, 1f, 1f, 1f);
          img.raycastTarget = false;
        }
      }

      // 4. Cấu hình Nút Về Làng (BtnReturnToTown) phủ toàn bộ diện tích panel để đè giữ
      Transform townBtnTrans = rootObj.transform.Find("TownPanel/BtnReturnToTown");
      if (townBtnTrans != null)
      {
        Button btn = townBtnTrans.GetComponent<Button>();
        if (btn == null) btn = townBtnTrans.gameObject.AddComponent<Button>();

        Image btnImg = townBtnTrans.GetComponent<Image>();
        if (btnImg == null) btnImg = townBtnTrans.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0f);
        btnImg.raycastTarget = true;
        btn.targetGraphic = btnImg;
        btn.transition = Selectable.Transition.None;

        Transform townFill = townBtnTrans.Find("HoldFill");
        if (townFill != null)
        {
          Image fillImg = townFill.GetComponent<Image>();
          if (fillImg != null)
          {
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
          }
        }
      }

      // 5. Cấu hình Nút Tầng Kế Tiếp (BtnNextFloor) phủ toàn bộ diện tích panel để đè giữ
      if (nextFloorBtn != null)
      {
        Button btn = nextFloorBtn.GetComponent<Button>();
        if (btn == null) btn = nextFloorBtn.gameObject.AddComponent<Button>();

        Image btnImg = nextFloorBtn.GetComponent<Image>();
        if (btnImg == null) btnImg = nextFloorBtn.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0f);
        btnImg.raycastTarget = true;
        btn.targetGraphic = btnImg;
        btn.transition = Selectable.Transition.None;

        Transform nextFill = nextFloorBtn.Find("HoldFill");
        if (nextFill != null)
        {
          Image fillImg = nextFill.GetComponent<Image>();
          if (fillImg != null)
          {
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
          }
        }
      }

      // 6. Cấu hình Nút Ở Lại (BtnStay)
      Transform stayBtnTrans = rootObj.transform.Find("BtnStay");
      if (stayBtnTrans != null)
      {
        Button stayBtn = stayBtnTrans.GetComponent<Button>();
        if (stayBtn != null)
        {
          stayBtn.onClick.RemoveListener(ui.Close);
          stayBtn.onClick.AddListener(ui.Close);
        }
      }

      ui.EnsureReferences();
      EditorUtility.SetDirty(rootObj);
    }
  }
}
