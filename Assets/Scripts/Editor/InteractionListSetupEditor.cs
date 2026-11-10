using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Công cụ tự động hóa 1-Click nâng cấp giao diện tương tác sang Danh Sách Dọc (Vertical Interaction List)
  /// - Gom InteractUI và PickupUI vào chung container InteractionList tại {x: 247, y: 0}
  /// - Gắn VerticalLayoutGroup & ContentSizeFitter
  /// - Gắn InteractionListUI điều phối
  /// - Bổ sung ScrollHint [Lăn chuột ↕ để chọn]
  /// </summary>
  public static class InteractionListSetupEditor
  {
    private const string CanvasPrefabPath = "Assets/UI RPG/Canvas.prefab";

    [MenuItem("Infinite Labyrinth/UI/1. Cấu Hình Danh Sách Tương Tác Dọc (Vertical Interaction List) (1-Click)", priority = 100)]
    public static void SetupVerticalInteractionList()
    {
      GameObject canvasRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
      if (canvasRoot == null)
      {
        Debug.LogError($"[InteractionListSetupEditor] Không thể mở Prefab tại đường dẫn: {CanvasPrefabPath}");
        return;
      }

      try
      {
        Transform canvasTr = canvasRoot.transform;

        // 1. Tìm InteractUI và PickupUI hiện tại
        Transform interactTr = canvasTr.Find("InteractUI") ?? canvasTr.Find("InteractionList/InteractUI");
        Transform pickupTr = canvasTr.Find("PickupUI") ?? canvasTr.Find("InteractionList/PickupUI");

        if (interactTr == null || pickupTr == null)
        {
          Debug.LogWarning("[InteractionListSetupEditor] Không tìm thấy đầy đủ InteractUI và PickupUI trên Canvas!");
        }

        // 2. Tìm hoặc tạo InteractionList container
        Transform listTr = canvasTr.Find("InteractionList");
        GameObject listObj;
        if (listTr == null)
        {
          listObj = new GameObject("InteractionList");
          listObj.transform.SetParent(canvasTr, false);
          listTr = listObj.transform;
        }
        else
        {
          listObj = listTr.gameObject;
        }

        // 3. Cấu hình RectTransform cho InteractionList
        RectTransform listRect = listObj.GetComponent<RectTransform>() ?? listObj.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0f, 0.5f);
        listRect.anchoredPosition = new Vector2(247f, 0f);
        listRect.sizeDelta = new Vector2(300f, 64f);

        // 4. Cấu hình VerticalLayoutGroup & ContentSizeFitter (bảo toàn spacing người dùng đã cài nếu có)
        var vlg = listObj.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
          vlg = listObj.AddComponent<VerticalLayoutGroup>();
          vlg.spacing = 2f;
          vlg.childAlignment = TextAnchor.MiddleLeft;
          vlg.childControlWidth = false;
          vlg.childControlHeight = false;
          vlg.childForceExpandWidth = false;
          vlg.childForceExpandHeight = false;
        }

        var csf = listObj.GetComponent<ContentSizeFitter>() ?? listObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 5. Cấu hình CanvasGroup cho container
        var cg = listObj.GetComponent<CanvasGroup>() ?? listObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 6. Cấu hình InteractionListUI
        var listUI = listObj.GetComponent<InteractionListUI>() ?? listObj.AddComponent<InteractionListUI>();

        // 7. Di chuyển InteractUI và PickupUI vào làm con của InteractionList
        if (interactTr != null && interactTr.parent != listTr)
        {
          interactTr.SetParent(listTr, false);
        }
        if (pickupTr != null && pickupTr.parent != listTr)
        {
          pickupTr.SetParent(listTr, false);
        }

        // Ẩn 2 template ban đầu
        if (interactTr != null) interactTr.gameObject.SetActive(false);
        if (pickupTr != null) pickupTr.gameObject.SetActive(false);

        // 8. Tạo hoặc cấu hình ScrollHint Text
        Transform hintTr = listTr.Find("ScrollHint");
        GameObject hintObj;
        if (hintTr == null)
        {
          hintObj = new GameObject("ScrollHint");
          hintObj.transform.SetParent(listTr, false);
          hintTr = hintObj.transform;

          var hintRect = hintObj.AddComponent<RectTransform>();
          hintRect.sizeDelta = new Vector2(300f, 24f);

          var hintText = hintObj.AddComponent<TextMeshProUGUI>();
          hintText.text = "[ Cuộn chuột ↕ để chọn ]";
          hintText.fontSize = 13;
          hintText.alignment = TextAlignmentOptions.Center;
          hintText.color = new Color(1f, 0.88f, 0.54f, 0.85f); // Vàng kim nhạt

          // Tìm font asset tương đồng nếu có
          var refText = interactTr?.GetComponentInChildren<TextMeshProUGUI>();
          if (refText != null && refText.font != null)
          {
            hintText.font = refText.font;
          }
        }
        else
        {
          hintObj = hintTr.gameObject;
        }

        hintObj.SetActive(false);

        // 9. Nối dây SerializedObject cho InteractionListUI
        SerializedObject so = new SerializedObject(listUI);
        if (interactTr != null)
        {
          so.FindProperty("interactTemplate").objectReferenceValue = interactTr.GetComponent<InteractPromptUI>();
        }
        if (pickupTr != null)
        {
          so.FindProperty("pickupTemplate").objectReferenceValue = pickupTr.GetComponent<PickupPromptUI>();
        }
        so.FindProperty("containerRect").objectReferenceValue = listRect;
        so.FindProperty("scrollHintRoot").objectReferenceValue = hintObj;
        so.FindProperty("listCanvasGroup").objectReferenceValue = cg;
        so.FindProperty("maxVisibleSlots").intValue = 5;
        so.ApplyModifiedProperties();

        // 10. Lưu Prefab lại
        PrefabUtility.SaveAsPrefabAsset(canvasRoot, CanvasPrefabPath);
        Debug.Log("[InteractionListSetupEditor] Đã cấu hình thành công Danh Sách Tương Tác Dọc (Vertical Interaction List) vào Canvas.prefab!");
        EditorUtility.DisplayDialog("Infinite Labyrinth", 
          "Cấu hình Danh Sách Tương Tác Dọc thành công!\n\n" +
          "1. Hợp nhất InteractUI & PickupUI vào InteractionList.\n" +
          "2. Đã bật điều khiển Corner: Mục đang chọn bật 2 Corner, các mục khác tắt.\n" +
          "3. Đã thêm cơ chế lăn chuột (Scroll Wheel) và phím Mũi Tên.\n" +
          "4. Bấm F chỉ nhặt hoặc tương tác đúng mục được chọn!", "Tuyệt vời");
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(canvasRoot);
      }
    }
  }
}
