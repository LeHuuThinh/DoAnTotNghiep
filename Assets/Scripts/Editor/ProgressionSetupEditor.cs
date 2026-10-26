#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Progression;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.EditorTools
{
  /// <summary>
  /// Công cụ tự động thiết lập và liên kết toàn bộ hệ thống Cấp độ, EXP Ring và Modal Cộng Điểm Tiềm Năng (RPG-05).
  /// </summary>
  public static class ProgressionSetupEditor
  {
    private const string PanelSpritePath = "Assets/UI RPG/GameUI/Panel_WithBorder.png";
    private const string PanelBorderThinPath = "Assets/UI RPG/GameUI/Panel_Border_Thin.png";
    private const string ButtonSpritePath = "Assets/UI RPG/GameUI/Button_WithBorder.png";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Infinite Labyrinth/RPG/5. Tự Động Thiết Lập EXP Ring & Bảng Điểm Tiềm Năng Trên Canvas", false, 14)]
    public static void SetupProgressionAndUI()
    {
      // 1. Đảm bảo GameObject PlayerProgression có mặt trong Scene
      GameObject progObj = EnsurePlayerProgressionObject();

      // 2. Thiết lập PlayerHUD: ExpRingFill (0.19 - 0.58) và LevelText
      bool hudSuccess = SetupPlayerHUDExp();

      // 3. Thiết lập Modal AttributeAllocationUI trong Canvas
      GameObject modalObj = SetupAttributeAllocationModal();

      // 4. Kết nối Modal vào PlayerStatsPanelUI
      LinkModalToStatsPanel(modalObj);

      // Lưu Scene
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene()
      );

      string msg = "Hoàn tất cài đặt Task RPG-05:\n\n" +
                   "1. PlayerProgression: Đã sẵn sàng quản lý Level, EXP, và Điểm Tiềm Năng.\n" +
                   $"2. PlayerHUD: {(hudSuccess ? "Đã liên kết ExpRingFill (0.19 - 0.58) và LevelText" : "Đã cập nhật cấu hình fill")}.\n" +
                   "3. Modal Phân Bổ Điểm Tiềm Năng: Đã dựng hoàn chỉnh trên Canvas với nút Xác Nhận, Hủy và Tẩy Điểm (100 Vàng).\n" +
                   "4. Kết nối nút [Cộng Điểm] trong Cửa sổ Trái của Inventory Screen.\n\n" +
                   "Bạn có thể Play game để kiểm tra ngay!";

      EditorUtility.DisplayDialog("Infinite Labyrinth - RPG-05 Hoàn Tất", msg, "Tuyệt vời!");
      Debug.Log("[ProgressionSetupEditor] Đã hoàn tất cài đặt toàn bộ hệ thống RPG-05 thành công.");
    }

    private static GameObject EnsurePlayerProgressionObject()
    {
      PlayerProgression existing = Object.FindFirstObjectByType<PlayerProgression>(FindObjectsInactive.Include);
      if (existing != null)
      {
        return existing.gameObject;
      }

      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RPG/PlayerProgression.prefab");
      GameObject obj;
      if (prefab != null)
      {
        obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
      }
      else
      {
        obj = new GameObject("PlayerProgression");
        obj.AddComponent<PlayerProgression>();
      }

      EditorUtility.SetDirty(obj);
      return obj;
    }

    private static bool SetupPlayerHUDExp()
    {
      PlayerHUD hud = Object.FindFirstObjectByType<PlayerHUD>();
      if (hud == null) return false;

      SerializedObject so = new SerializedObject(hud);

      // Tìm ExpRingFill
      Transform expFillTr = hud.transform.Find("AvatarContainer/ExpRingMask/ExpRingFill");
      if (expFillTr == null) expFillTr = hud.transform.Find("Avatar_Container/ExpRingMask/ExpRingFill");
      if (expFillTr == null) expFillTr = hud.transform.Find("AvatarContainer/ExpRingFill");

      if (expFillTr != null)
      {
        Image fillImg = expFillTr.GetComponent<Image>();
        if (fillImg != null)
        {
          so.FindProperty("expRingFill").objectReferenceValue = fillImg;
        }
      }

      // Cài đặt thông số fill chữ C: 0.19 - 0.58
      so.FindProperty("minExpFill").floatValue = 0.19f;
      so.FindProperty("maxExpFill").floatValue = 0.58f;

      // Tìm LevelText
      Transform levelTextTr = hud.transform.Find("AvatarContainer/LevelBadge/LevelText");
      if (levelTextTr == null) levelTextTr = hud.transform.Find("Avatar_Container/LevelBadge/LevelText");

      if (levelTextTr != null)
      {
        TextMeshProUGUI tmp = levelTextTr.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
          so.FindProperty("levelText").objectReferenceValue = tmp;
        }
      }

      so.ApplyModifiedProperties();
      EditorUtility.SetDirty(hud);
      return true;
    }

    private static GameObject SetupAttributeAllocationModal()
    {
      // Tìm InventoryScreen hoặc Canvas
      Transform parentContainer = null;
      var invCtrl = Object.FindFirstObjectByType<InventoryUIController>();
      if (invCtrl != null)
      {
        parentContainer = invCtrl.transform;
      }
      else
      {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) parentContainer = canvas.transform;
      }

      if (parentContainer == null)
      {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        parentContainer = canvasObj.transform;
      }

      Transform existingModal = parentContainer.Find("AttributeUI");
      if (existingModal == null) existingModal = parentContainer.Find("AttributeAllocationModal");
      if (existingModal == null)
      {
        var allModals = Object.FindObjectsByType<AttributeAllocationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allModals.Length > 0) existingModal = allModals[0].transform;
      }

      if (existingModal != null)
      {
        AttributeAllocationUI ui = existingModal.GetComponent<AttributeAllocationUI>();
        if (ui != null)
        {
          ui.AutoFindUIComponents();

          SerializedObject existingSo = new SerializedObject(ui);
          existingSo.FindProperty("modalRoot").objectReferenceValue = existingModal.gameObject;

          CanvasGroup existingCg = existingModal.GetComponent<CanvasGroup>();
          if (existingCg != null) existingSo.FindProperty("modalCanvasGroup").objectReferenceValue = existingCg;

          // Available Points
          Transform ptsTr = existingModal.Find("Available Points/Points");
          if (ptsTr == null) ptsTr = existingModal.Find("Available Points/Text");
          if (ptsTr != null) existingSo.FindProperty("availablePointsText").objectReferenceValue = ptsTr.GetComponent<TextMeshProUGUI>();

          // Status Msg
          Transform statusTr = existingModal.Find("AttributePanel/StatusMsg/Text");
          if (statusTr != null) existingSo.FindProperty("statusMessageText").objectReferenceValue = statusTr.GetComponent<TextMeshProUGUI>();

          // Footer
          Transform confTr = existingModal.Find("FooterRow/BtnConfirm");
          if (confTr != null) existingSo.FindProperty("confirmButton").objectReferenceValue = confTr.GetComponent<Button>();

          Transform cancelTr = existingModal.Find("FooterRow/BtnCancel");
          if (cancelTr != null) existingSo.FindProperty("cancelButton").objectReferenceValue = cancelTr.GetComponent<Button>();

          Transform resetTr = existingModal.Find("FooterRow/BtnResetPoints");
          if (resetTr == null) resetTr = existingModal.Find("FooterRow/BtnResetStats");
          if (resetTr != null) existingSo.FindProperty("resetStatsButton").objectReferenceValue = resetTr.GetComponent<Button>();

          Transform resetPriceTr = existingModal.Find("FooterRow/ResetPrice/Text");
          if (resetPriceTr != null) existingSo.FindProperty("resetPriceText").objectReferenceValue = resetPriceTr.GetComponent<TextMeshProUGUI>();

          if (resetTr != null) existingSo.FindProperty("resetStatsButtonText").objectReferenceValue = resetTr.GetComponentInChildren<TextMeshProUGUI>();

          // Attack
          LinkRowProperties(existingSo, existingModal, "AttributePanel/RowContainer/Attack", "atk");
          // Health
          LinkRowProperties(existingSo, existingModal, "AttributePanel/RowContainer/Health", "hp");
          // Stamina
          LinkRowProperties(existingSo, existingModal, "AttributePanel/RowContainer/Stamina", "stamina");

          existingSo.ApplyModifiedProperties();
          EditorUtility.SetDirty(ui);
        }
        EditorUtility.SetDirty(existingModal.gameObject);
        return existingModal.gameObject;
      }

      // Tải tài nguyên đồ họa
      Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
      Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
      Sprite rowBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelBorderThinPath);
      TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

      // 1. Root Modal
      GameObject modalRoot = new GameObject("AttributeAllocationModal");
      modalRoot.transform.SetParent(parentContainer, false);
      RectTransform rootRect = modalRoot.AddComponent<RectTransform>();
      rootRect.anchorMin = new Vector2(0.5f, 0.5f);
      rootRect.anchorMax = new Vector2(0.5f, 0.5f);
      rootRect.pivot = new Vector2(0.5f, 0.5f);
      rootRect.anchoredPosition = new Vector2(0f, 0f);
      rootRect.sizeDelta = new Vector2(400f, 490f);

      CanvasGroup cg = modalRoot.AddComponent<CanvasGroup>();
      Image rootBg = modalRoot.AddComponent<Image>();
      if (panelSprite != null)
      {
        rootBg.sprite = panelSprite;
        rootBg.type = Image.Type.Sliced;
      }
      rootBg.color = new Color(0.10f, 0.09f, 0.13f, 0.97f);

      // 2. Header
      GameObject titleObj = new GameObject("TitleText");
      titleObj.transform.SetParent(modalRoot.transform, false);
      RectTransform titleRect = titleObj.AddComponent<RectTransform>();
      titleRect.anchorMin = new Vector2(0f, 1f);
      titleRect.anchorMax = new Vector2(1f, 1f);
      titleRect.pivot = new Vector2(0.5f, 1f);
      titleRect.anchoredPosition = new Vector2(0f, -16f);
      titleRect.sizeDelta = new Vector2(-40f, 32f);
      TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
      if (font != null) titleText.font = font;
      titleText.text = "ATTRIBUTE POINTS";
      titleText.fontSize = 20f;
      titleText.fontStyle = FontStyles.Bold;
      titleText.color = new Color(1f, 0.88f, 0.51f);
      titleText.alignment = TextAlignmentOptions.Center;

      // Nút Close [X]
      GameObject closeBtnObj = CreateButton("BtnClose", modalRoot.transform, new Vector2(30f, 30f), "X", 14f, buttonSprite, font);
      RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
      closeRect.anchorMin = new Vector2(1f, 1f);
      closeRect.anchorMax = new Vector2(1f, 1f);
      closeRect.pivot = new Vector2(1f, 1f);
      closeRect.anchoredPosition = new Vector2(-12f, -12f);
      Button closeBtn = closeBtnObj.GetComponent<Button>();

      // 3. Points Banner
      GameObject pointsObj = new GameObject("AvailablePointsText");
      pointsObj.transform.SetParent(modalRoot.transform, false);
      RectTransform pointsRect = pointsObj.AddComponent<RectTransform>();
      pointsRect.anchorMin = new Vector2(0f, 1f);
      pointsRect.anchorMax = new Vector2(1f, 1f);
      pointsRect.pivot = new Vector2(0.5f, 1f);
      pointsRect.anchoredPosition = new Vector2(0f, -54f);
      pointsRect.sizeDelta = new Vector2(-40f, 28f);
      TextMeshProUGUI pointsText = pointsObj.AddComponent<TextMeshProUGUI>();
      if (font != null) pointsText.font = font;
      pointsText.text = "Available Points: <color=#FFE082><b>0</b></color>";
      pointsText.fontSize = 15f;
      pointsText.alignment = TextAlignmentOptions.Center;

      // 4. Rows Container
      GameObject rowsContainer = new GameObject("RowsContainer");
      rowsContainer.transform.SetParent(modalRoot.transform, false);
      RectTransform rowsRect = rowsContainer.AddComponent<RectTransform>();
      rowsRect.anchorMin = new Vector2(0f, 1f);
      rowsRect.anchorMax = new Vector2(1f, 1f);
      rowsRect.pivot = new Vector2(0.5f, 1f);
      rowsRect.anchoredPosition = new Vector2(0f, -92f);
      rowsRect.sizeDelta = new Vector2(-36f, 230f);

      VerticalLayoutGroup vlg = rowsContainer.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = 10f;
      vlg.childControlWidth = true;
      vlg.childControlHeight = false;
      vlg.childForceExpandWidth = true;
      vlg.childForceExpandHeight = false;

      // Hàng 1: ATK
      var atkRow = CreateStatRow("Row_ATK", rowsContainer.transform, "Attack Damage", new Color(1f, 0.54f, 0.50f), rowBgSprite, buttonSprite, font);

      // Hàng 2: HP
      var hpRow = CreateStatRow("Row_HP", rowsContainer.transform, "Max Health", new Color(0.94f, 0.33f, 0.31f), rowBgSprite, buttonSprite, font);

      // Hàng 3: Stamina
      var stamRow = CreateStatRow("Row_Stamina", rowsContainer.transform, "Max Stamina", new Color(0.40f, 0.73f, 0.42f), rowBgSprite, buttonSprite, font);

      // 5. Status Feedback Text
      GameObject statusObj = new GameObject("StatusMessageText");
      statusObj.transform.SetParent(modalRoot.transform, false);
      RectTransform statusRect = statusObj.AddComponent<RectTransform>();
      statusRect.anchorMin = new Vector2(0f, 0f);
      statusRect.anchorMax = new Vector2(1f, 0f);
      statusRect.pivot = new Vector2(0.5f, 0f);
      statusRect.anchoredPosition = new Vector2(0f, 108f);
      statusRect.sizeDelta = new Vector2(-40f, 24f);
      TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
      if (font != null) statusText.font = font;
      statusText.text = "";
      statusText.fontSize = 13f;
      statusText.alignment = TextAlignmentOptions.Center;

      // 6. Action Footer
      GameObject footerObj = new GameObject("Footer");
      footerObj.transform.SetParent(modalRoot.transform, false);
      RectTransform footerRect = footerObj.AddComponent<RectTransform>();
      footerRect.anchorMin = new Vector2(0f, 0f);
      footerRect.anchorMax = new Vector2(1f, 0f);
      footerRect.pivot = new Vector2(0.5f, 0f);
      footerRect.anchoredPosition = new Vector2(0f, 16f);
      footerRect.sizeDelta = new Vector2(-36f, 86f);

      // Hàng nút trên: Confirm & Cancel
      GameObject confirmBtnObj = CreateButton("BtnConfirm", footerObj.transform, new Vector2(174f, 38f), "Confirm", 14f, buttonSprite, font, new Color(0.20f, 0.55f, 0.25f));
      RectTransform confirmRect = confirmBtnObj.GetComponent<RectTransform>();
      confirmRect.anchorMin = new Vector2(0f, 1f);
      confirmRect.anchorMax = new Vector2(0f, 1f);
      confirmRect.pivot = new Vector2(0f, 1f);
      confirmRect.anchoredPosition = new Vector2(0f, 0f);

      GameObject cancelBtnObj = CreateButton("BtnCancel", footerObj.transform, new Vector2(174f, 38f), "Cancel", 14f, buttonSprite, font, new Color(0.35f, 0.35f, 0.40f));
      RectTransform cancelRect = cancelBtnObj.GetComponent<RectTransform>();
      cancelRect.anchorMin = new Vector2(1f, 1f);
      cancelRect.anchorMax = new Vector2(1f, 1f);
      cancelRect.pivot = new Vector2(1f, 1f);
      cancelRect.anchoredPosition = new Vector2(0f, 0f);

      // Hàng nút dưới: Reset All
      GameObject resetBtnObj = CreateButton("BtnResetStats", footerObj.transform, new Vector2(358f, 38f), "Reset All", 14f, buttonSprite, font, new Color(0.65f, 0.20f, 0.20f));
      RectTransform resetRect = resetBtnObj.GetComponent<RectTransform>();
      resetRect.anchorMin = new Vector2(0.5f, 0f);
      resetRect.anchorMax = new Vector2(0.5f, 0f);
      resetRect.pivot = new Vector2(0.5f, 0f);
      resetRect.anchoredPosition = new Vector2(0f, 0f);

      // Gắn Component AttributeAllocationUI và ánh xạ SerializedObject
      AttributeAllocationUI uiComp = modalRoot.AddComponent<AttributeAllocationUI>();
      SerializedObject so = new SerializedObject(uiComp);

      so.FindProperty("modalCanvasGroup").objectReferenceValue = cg;
      so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
      so.FindProperty("closeButton").objectReferenceValue = closeBtn;
      so.FindProperty("availablePointsText").objectReferenceValue = pointsText;

      // ATK
      so.FindProperty("atkIcon").objectReferenceValue = atkRow.icon;
      so.FindProperty("atkValueText").objectReferenceValue = atkRow.valueText;
      so.FindProperty("atkPointsAddedText").objectReferenceValue = atkRow.addedText;
      so.FindProperty("atkMinusButton").objectReferenceValue = atkRow.minusBtn;
      so.FindProperty("atkPlusButton").objectReferenceValue = atkRow.plusBtn;

      // HP
      so.FindProperty("hpIcon").objectReferenceValue = hpRow.icon;
      so.FindProperty("hpValueText").objectReferenceValue = hpRow.valueText;
      so.FindProperty("hpPointsAddedText").objectReferenceValue = hpRow.addedText;
      so.FindProperty("hpMinusButton").objectReferenceValue = hpRow.minusBtn;
      so.FindProperty("hpPlusButton").objectReferenceValue = hpRow.plusBtn;

      // Stamina
      so.FindProperty("staminaIcon").objectReferenceValue = stamRow.icon;
      so.FindProperty("staminaValueText").objectReferenceValue = stamRow.valueText;
      so.FindProperty("staminaPointsAddedText").objectReferenceValue = stamRow.addedText;
      so.FindProperty("staminaMinusButton").objectReferenceValue = stamRow.minusBtn;
      so.FindProperty("staminaPlusButton").objectReferenceValue = stamRow.plusBtn;

      // Footer
      so.FindProperty("confirmButton").objectReferenceValue = confirmBtnObj.GetComponent<Button>();
      so.FindProperty("cancelButton").objectReferenceValue = cancelBtnObj.GetComponent<Button>();
      so.FindProperty("resetStatsButton").objectReferenceValue = resetBtnObj.GetComponent<Button>();
      so.FindProperty("resetStatsButtonText").objectReferenceValue = resetBtnObj.GetComponentInChildren<TextMeshProUGUI>();
      so.FindProperty("statusMessageText").objectReferenceValue = statusText;

      so.ApplyModifiedProperties();
      EditorUtility.SetDirty(modalRoot);

      return modalRoot;
    }

    private struct StatRowElements
    {
      public Image icon;
      public TextMeshProUGUI valueText;
      public TextMeshProUGUI addedText;
      public Button minusBtn;
      public Button plusBtn;
    }

    private static StatRowElements CreateStatRow(string name, Transform parent, string title, Color iconColor, Sprite rowBgSprite, Sprite buttonSprite, TMP_FontAsset font)
    {
      StatRowElements elements = new StatRowElements();

      GameObject rowObj = new GameObject(name);
      rowObj.transform.SetParent(parent, false);
      RectTransform rowRect = rowObj.AddComponent<RectTransform>();
      rowRect.sizeDelta = new Vector2(0f, 64f);

      Image rowBg = rowObj.AddComponent<Image>();
      if (rowBgSprite != null)
      {
        rowBg.sprite = rowBgSprite;
        rowBg.type = Image.Type.Sliced;
      }
      rowBg.color = new Color(0.16f, 0.14f, 0.20f, 0.85f);

      // Icon Placeholder
      GameObject iconObj = new GameObject("IconPlaceholder");
      iconObj.transform.SetParent(rowObj.transform, false);
      RectTransform iconRect = iconObj.AddComponent<RectTransform>();
      iconRect.anchorMin = new Vector2(0f, 0.5f);
      iconRect.anchorMax = new Vector2(0f, 0.5f);
      iconRect.pivot = new Vector2(0f, 0.5f);
      iconRect.anchoredPosition = new Vector2(10f, 0f);
      iconRect.sizeDelta = new Vector2(40f, 40f);
      elements.icon = iconObj.AddComponent<Image>();
      elements.icon.color = iconColor;

      // Tên chỉ số
      GameObject labelObj = new GameObject("Label");
      labelObj.transform.SetParent(rowObj.transform, false);
      RectTransform labelRect = labelObj.AddComponent<RectTransform>();
      labelRect.anchorMin = new Vector2(0f, 0.5f);
      labelRect.anchorMax = new Vector2(0f, 0.5f);
      labelRect.pivot = new Vector2(0f, 0.5f);
      labelRect.anchoredPosition = new Vector2(58f, 10f);
      labelRect.sizeDelta = new Vector2(110f, 22f);
      TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
      if (font != null) labelText.font = font;
      labelText.text = title;
      labelText.fontSize = 14f;
      labelText.fontStyle = FontStyles.Bold;
      labelText.alignment = TextAlignmentOptions.MidlineLeft;

      // Giá trị hiện tại / xem trước
      GameObject valObj = new GameObject("ValueText");
      valObj.transform.SetParent(rowObj.transform, false);
      RectTransform valRect = valObj.AddComponent<RectTransform>();
      valRect.anchorMin = new Vector2(0f, 0.5f);
      valRect.anchorMax = new Vector2(0f, 0.5f);
      valRect.pivot = new Vector2(0f, 0.5f);
      valRect.anchoredPosition = new Vector2(58f, -12f);
      valRect.sizeDelta = new Vector2(160f, 22f);
      elements.valueText = valObj.AddComponent<TextMeshProUGUI>();
      if (font != null) elements.valueText.font = font;
      elements.valueText.text = "0";
      elements.valueText.fontSize = 13f;
      elements.valueText.color = new Color(0.9f, 0.9f, 0.9f);
      elements.valueText.alignment = TextAlignmentOptions.MidlineLeft;

      // Cụm nút: [-]  +0  [+]
      GameObject minusObj = CreateButton("BtnMinus", rowObj.transform, new Vector2(30f, 30f), "-", 16f, buttonSprite, font);
      RectTransform minusRect = minusObj.GetComponent<RectTransform>();
      minusRect.anchorMin = new Vector2(1f, 0.5f);
      minusRect.anchorMax = new Vector2(1f, 0.5f);
      minusRect.pivot = new Vector2(1f, 0.5f);
      minusRect.anchoredPosition = new Vector2(-88f, 0f);
      elements.minusBtn = minusObj.GetComponent<Button>();

      GameObject addedObj = new GameObject("AddedPointsText");
      addedObj.transform.SetParent(rowObj.transform, false);
      RectTransform addedRect = addedObj.AddComponent<RectTransform>();
      addedRect.anchorMin = new Vector2(1f, 0.5f);
      addedRect.anchorMax = new Vector2(1f, 0.5f);
      addedRect.pivot = new Vector2(0.5f, 0.5f);
      addedRect.anchoredPosition = new Vector2(-60f, 0f);
      addedRect.sizeDelta = new Vector2(40f, 30f);
      elements.addedText = addedObj.AddComponent<TextMeshProUGUI>();
      if (font != null) elements.addedText.font = font;
      elements.addedText.text = "0";
      elements.addedText.fontSize = 15f;
      elements.addedText.fontStyle = FontStyles.Bold;
      elements.addedText.color = new Color(0.65f, 0.84f, 0.65f);
      elements.addedText.alignment = TextAlignmentOptions.Center;

      GameObject plusObj = CreateButton("BtnPlus", rowObj.transform, new Vector2(30f, 30f), "+", 16f, buttonSprite, font);
      RectTransform plusRect = plusObj.GetComponent<RectTransform>();
      plusRect.anchorMin = new Vector2(1f, 0.5f);
      plusRect.anchorMax = new Vector2(1f, 0.5f);
      plusRect.pivot = new Vector2(1f, 0.5f);
      plusRect.anchoredPosition = new Vector2(-10f, 0f);
      elements.plusBtn = plusObj.GetComponent<Button>();

      return elements;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 size, string text, float fontSize, Sprite btnSprite, TMP_FontAsset font, Color? tintColor = null)
    {
      GameObject btnObj = new GameObject(name);
      btnObj.transform.SetParent(parent, false);
      RectTransform rt = btnObj.AddComponent<RectTransform>();
      rt.sizeDelta = size;

      Image img = btnObj.AddComponent<Image>();
      if (btnSprite != null)
      {
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
      }
      img.color = tintColor ?? new Color(0.24f, 0.22f, 0.30f);

      Button btn = btnObj.AddComponent<Button>();
      ColorBlock cb = btn.colors;
      cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
      cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
      Color baseColor = tintColor ?? new Color(0.24f, 0.22f, 0.30f);
      cb.disabledColor = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, 0.45f);
      btn.colors = cb;

      GameObject textObj = new GameObject("Text");
      textObj.transform.SetParent(btnObj.transform, false);
      RectTransform trt = textObj.AddComponent<RectTransform>();
      trt.anchorMin = Vector2.zero;
      trt.anchorMax = Vector2.one;
      trt.offsetMin = Vector2.zero;
      trt.offsetMax = Vector2.zero;

      TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
      if (font != null) tmp.font = font;
      tmp.text = text;
      tmp.fontSize = fontSize;
      tmp.fontStyle = FontStyles.Bold;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = Color.white;

      return btnObj;
    }

    private static void LinkModalToStatsPanel(GameObject modalObj)
    {
      if (modalObj == null) return;
      var statsPanel = Object.FindFirstObjectByType<PlayerStatsPanelUI>();
      if (statsPanel == null) return;

      var uiComp = modalObj.GetComponent<AttributeAllocationUI>();
      if (uiComp == null) return;

      SerializedObject so = new SerializedObject(statsPanel);
      var prop = so.FindProperty("attributeAllocationModal");
      if (prop != null)
      {
        prop.objectReferenceValue = uiComp;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(statsPanel);
      }
    }

    private static void LinkRowProperties(SerializedObject so, Transform modal, string rowPath, string prefix)
    {
      Transform row = modal.Find(rowPath);
      if (row == null) return;

      Transform iconTr = row.Find("Icon");
      if (iconTr == null) iconTr = row.Find("IconPlaceholder");
      if (iconTr != null) so.FindProperty(prefix + "Icon").objectReferenceValue = iconTr.GetComponent<Image>();

      Transform valTr = row.Find("PreviewValue");
      if (valTr == null) valTr = row.Find("ValueText");
      if (valTr != null) so.FindProperty(prefix + "ValueText").objectReferenceValue = valTr.GetComponent<TextMeshProUGUI>();

      Transform addedTr = row.Find("PointAddedValue");
      if (addedTr == null) addedTr = row.Find("AddedPointsText");
      if (addedTr != null) so.FindProperty(prefix + "PointsAddedText").objectReferenceValue = addedTr.GetComponent<TextMeshProUGUI>();

      Transform minusTr = row.Find("BtnMinus");
      if (minusTr != null) so.FindProperty(prefix + "MinusButton").objectReferenceValue = minusTr.GetComponent<Button>();

      Transform plusTr = row.Find("BtnPlus");
      if (plusTr != null) so.FindProperty(prefix + "PlusButton").objectReferenceValue = plusTr.GetComponent<Button>();
    }
  }
}
#endif
