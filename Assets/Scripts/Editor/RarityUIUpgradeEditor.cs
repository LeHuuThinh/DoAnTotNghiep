#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tool 1-Click tự động kiểm tra, thiết lập và liên kết tham chiếu hệ thống hiển thị Rarity mới:
  /// 1. Nền glow mờ (RarityBG - Slot_Rarity_2.png)
  /// 2. Thanh bar dưới icon (RarityBar - Slot_Rarity_3.png)
  /// Áp dụng đồng bộ cho: 30 ô InventorySlot, 2 ô Trang bị (Weapon & Armor), PickupPromptUI, và PickupNotificationItem.
  /// </summary>
  public class RarityUIUpgradeEditor : UnityEditor.Editor
  {
    private const string CANVAS_PREFAB_PATH = "Assets/UI RPG/Canvas.prefab";
    private const string PICKUP_NOTI_PREFAB_PATH = "Assets/Prefabs/RPG/PickupNotificationItem.prefab";
    private const string GLOW_SPRITE_GUID = "7877af98133b6624d84c06b911add1e9"; // Slot_Rarity_2.png
    private const string BAR_SPRITE_GUID = "34f4258c291138f4d89a5cd3ffdf4e58";  // Slot_Rarity_3.png

    [MenuItem("Infinite Labyrinth/UI/8. Cấu Hình Rarity Mới (Glow + Bar) Cho Canvas & Prefabs (1-Click)", false, 8)]
    [MenuItem("Infinite Labyrinth/RPG/8. Cấu Hình Rarity Mới (Glow + Bar) (1-Click)", false, 8)]
    public static void UpgradeAllRarityUI()
    {
      Debug.Log("<color=cyan>[RarityUIUpgradeEditor]</color> Bắt đầu kiểm tra và nâng cấp hệ thống hiển thị Rarity mới...");

      Sprite glowSprite = LoadSpriteByGuid(GLOW_SPRITE_GUID);
      Sprite barSprite = LoadSpriteByGuid(BAR_SPRITE_GUID);

      int slotsUpdated = 0;
      int prefabsUpdated = 0;

      // 1. Nâng cấp PickupNotificationItem.prefab
      GameObject notiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PICKUP_NOTI_PREFAB_PATH);
      if (notiPrefab != null)
      {
        GameObject instance = PrefabUtility.InstantiatePrefab(notiPrefab) as GameObject;
        if (instance != null)
        {
          bool changed = SetupNotificationItem(instance, glowSprite, barSprite);
          if (changed)
          {
            PrefabUtility.SaveAsPrefabAsset(instance, PICKUP_NOTI_PREFAB_PATH);
            prefabsUpdated++;
            Debug.Log("<color=green>[RarityUIUpgradeEditor]</color> ✅ Đã cấu hình PickupNotificationItem.prefab!");
          }
          DestroyImmediate(instance);
        }
      }

      // 2. Nâng cấp Canvas.prefab
      GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CANVAS_PREFAB_PATH);
      if (canvasPrefab != null)
      {
        GameObject instance = PrefabUtility.InstantiatePrefab(canvasPrefab) as GameObject;
        if (instance != null)
        {
          bool changed = false;

          // A. 30 ô InventorySlot
          InventorySlotUI[] invSlots = instance.GetComponentsInChildren<InventorySlotUI>(true);
          foreach (var slot in invSlots)
          {
            if (SetupInventorySlot(slot.gameObject, glowSprite, barSprite))
            {
              changed = true;
              slotsUpdated++;
            }
          }

          // B. Ô EquipmentSlot (WeaponSlot, ArmorSlot)
          EquipmentSlotUI[] equipSlots = instance.GetComponentsInChildren<EquipmentSlotUI>(true);
          foreach (var slot in equipSlots)
          {
            if (SetupEquipmentSlot(slot.gameObject, glowSprite, barSprite))
            {
              changed = true;
              slotsUpdated++;
            }
          }

          // C. PickupPromptUI
          PickupPromptUI promptUI = instance.GetComponentInChildren<PickupPromptUI>(true);
          if (promptUI != null)
          {
            if (SetupPickupPrompt(promptUI.gameObject, glowSprite, barSprite))
            {
              changed = true;
            }
          }

          if (changed)
          {
            PrefabUtility.SaveAsPrefabAsset(instance, CANVAS_PREFAB_PATH);
            prefabsUpdated++;
            Debug.Log("<color=green>[RarityUIUpgradeEditor]</color> ✅ Đã cấu hình Canvas.prefab!");
          }
          DestroyImmediate(instance);
        }
      }

      // 3. Nâng cấp Canvas trong Scene đang mở (nếu có)
      Canvas sceneCanvas = Object.FindFirstObjectByType<Canvas>();
      if (sceneCanvas != null)
      {
        Undo.RecordObject(sceneCanvas, "Upgrade Rarity UI in Scene");

        InventorySlotUI[] invSlots = sceneCanvas.GetComponentsInChildren<InventorySlotUI>(true);
        foreach (var slot in invSlots)
        {
          SetupInventorySlot(slot.gameObject, glowSprite, barSprite);
        }

        EquipmentSlotUI[] equipSlots = sceneCanvas.GetComponentsInChildren<EquipmentSlotUI>(true);
        foreach (var slot in equipSlots)
        {
          SetupEquipmentSlot(slot.gameObject, glowSprite, barSprite);
        }

        PickupPromptUI promptUI = sceneCanvas.GetComponentInChildren<PickupPromptUI>(true);
        if (promptUI != null)
        {
          SetupPickupPrompt(promptUI.gameObject, glowSprite, barSprite);
        }

        EditorUtility.SetDirty(sceneCanvas);
        Debug.Log("<color=green>[RarityUIUpgradeEditor]</color> ✅ Đã đồng bộ Canvas trong Scene hiện tại!");
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>[RarityUIUpgradeEditor]</color> 🎉 HOÀN THÀNH! Đã cấu hình {slotsUpdated} ô slot và {prefabsUpdated} prefabs.");
      EditorUtility.DisplayDialog("Thành Công", $"Đã cấu hình hệ thống hiển thị Rarity mới (Glow + Bar) hoàn chỉnh!\n- Prefabs đã nâng cấp: {prefabsUpdated}\n- Tổng số Slot đã đồng bộ: {slotsUpdated}", "Tuyệt Vời!");
    }

    private static Sprite LoadSpriteByGuid(string guid)
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      if (string.IsNullOrEmpty(path)) return null;
      return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool SetupInventorySlot(GameObject slotObj, Sprite glowSprite, Sprite barSprite)
    {
      bool modified = false;

      // 1. Kiểm tra / gắn RarityBG (glow mờ)
      Transform bgTr = slotObj.transform.Find("RarityBG");
      if (bgTr != null)
      {
        Image bgImg = bgTr.GetComponent<Image>();
        if (bgImg != null && glowSprite != null && bgImg.sprite != glowSprite)
        {
          bgImg.sprite = glowSprite;
          modified = true;
        }
      }

      // 2. Kiểm tra / tạo RarityBar (thanh bar dưới icon)
      Transform barTr = slotObj.transform.Find("RarityBar");
      if (barTr == null)
      {
        GameObject barObj = new GameObject("RarityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barTr = barObj.transform;
        barTr.SetParent(slotObj.transform, false);

        RectTransform rt = barTr as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Đặt ngay sau RarityBG
        if (bgTr != null)
        {
          barTr.SetSiblingIndex(bgTr.GetSiblingIndex() + 1);
        }

        Image barImg = barObj.GetComponent<Image>();
        barImg.sprite = barSprite;
        barImg.color = Color.white;
        barImg.raycastTarget = false;

        barObj.SetActive(false);
        modified = true;
      }
      else
      {
        Image barImg = barTr.GetComponent<Image>();
        if (barImg != null && barSprite != null && barImg.sprite != barSprite)
        {
          barImg.sprite = barSprite;
          modified = true;
        }
      }

      // Wire serialized properties vào InventorySlotUI
      InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
      if (slotUI != null)
      {
        SerializedObject so = new SerializedObject(slotUI);
        SerializedProperty pBg = so.FindProperty("rarityBg");
        SerializedProperty pBar = so.FindProperty("rarityBar");

        if (bgTr != null && pBg != null && pBg.objectReferenceValue == null)
        {
          pBg.objectReferenceValue = bgTr.GetComponent<Image>();
          modified = true;
        }

        if (barTr != null && pBar != null && pBar.objectReferenceValue == null)
        {
          pBar.objectReferenceValue = barTr.GetComponent<Image>();
          modified = true;
        }

        if (modified)
        {
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(slotUI);
        }
      }

      return modified;
    }

    private static bool SetupEquipmentSlot(GameObject slotObj, Sprite glowSprite, Sprite barSprite)
    {
      bool modified = false;

      Transform bgTr = slotObj.transform.Find("RarityBG");
      if (bgTr != null)
      {
        Image bgImg = bgTr.GetComponent<Image>();
        if (bgImg != null && glowSprite != null && bgImg.sprite != glowSprite)
        {
          bgImg.sprite = glowSprite;
          modified = true;
        }
      }

      Transform barTr = slotObj.transform.Find("RarityBar");
      if (barTr == null)
      {
        GameObject barObj = new GameObject("RarityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barTr = barObj.transform;
        barTr.SetParent(slotObj.transform, false);

        RectTransform rt = barTr as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (bgTr != null)
        {
          barTr.SetSiblingIndex(bgTr.GetSiblingIndex() + 1);
        }

        Image barImg = barObj.GetComponent<Image>();
        barImg.sprite = barSprite;
        barImg.color = Color.white;
        barImg.raycastTarget = false;

        barObj.SetActive(false);
        modified = true;
      }
      else
      {
        Image barImg = barTr.GetComponent<Image>();
        if (barImg != null && barSprite != null && barImg.sprite != barSprite)
        {
          barImg.sprite = barSprite;
          modified = true;
        }
      }

      EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();
      if (slotUI != null)
      {
        SerializedObject so = new SerializedObject(slotUI);
        SerializedProperty pBg = so.FindProperty("rarityBg");
        SerializedProperty pBar = so.FindProperty("rarityBar");

        if (bgTr != null && pBg != null && pBg.objectReferenceValue == null)
        {
          pBg.objectReferenceValue = bgTr.GetComponent<Image>();
          modified = true;
        }

        if (barTr != null && pBar != null && pBar.objectReferenceValue == null)
        {
          pBar.objectReferenceValue = barTr.GetComponent<Image>();
          modified = true;
        }

        if (modified)
        {
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(slotUI);
        }
      }

      return modified;
    }

    private static bool SetupPickupPrompt(GameObject promptObj, Sprite glowSprite, Sprite barSprite)
    {
      bool modified = false;

      Transform iconGroup = promptObj.transform.Find("IconGroup");
      if (iconGroup == null) return false;

      // 1. RarityBG
      Transform bgTr = iconGroup.Find("RarityBG");
      if (bgTr != null)
      {
        Image bgImg = bgTr.GetComponent<Image>();
        if (bgImg != null && glowSprite != null && bgImg.sprite != glowSprite)
        {
          bgImg.sprite = glowSprite;
          modified = true;
        }
      }

      // 2. RarityBar
      Transform barTr = iconGroup.Find("RarityBar");
      if (barTr == null)
      {
        GameObject barObj = new GameObject("RarityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barTr = barObj.transform;
        barTr.SetParent(iconGroup, false);

        RectTransform rt = barTr as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (bgTr != null)
        {
          barTr.SetSiblingIndex(bgTr.GetSiblingIndex() + 1);
        }

        Image barImg = barObj.GetComponent<Image>();
        barImg.sprite = barSprite;
        barImg.color = Color.white;
        barImg.raycastTarget = false;

        barObj.SetActive(true);
        modified = true;
      }
      else
      {
        Image barImg = barTr.GetComponent<Image>();
        if (barImg != null && barSprite != null && barImg.sprite != barSprite)
        {
          barImg.sprite = barSprite;
          modified = true;
        }
      }

      // 3. Đảm bảo Icon GameObject luôn active để hiển thị ảnh icon
      Transform iconTr = iconGroup.Find("Icon");
      if (iconTr != null && !iconTr.gameObject.activeSelf)
      {
        iconTr.gameObject.SetActive(true);
        modified = true;
      }

      // Wire PickupPromptUI
      PickupPromptUI promptUI = promptObj.GetComponent<PickupPromptUI>();
      if (promptUI != null)
      {
        SerializedObject so = new SerializedObject(promptUI);
        SerializedProperty pIcon = so.FindProperty("itemIcon");
        SerializedProperty pBg = so.FindProperty("rarityBg");
        SerializedProperty pBar = so.FindProperty("rarityBar");
        SerializedProperty pLegacy = so.FindProperty("legacyRarityBorder");

        if (iconTr != null && pIcon != null && pIcon.objectReferenceValue == null)
        {
          pIcon.objectReferenceValue = iconTr.GetComponent<Image>();
          modified = true;
        }

        if (bgTr != null && pBg != null && pBg.objectReferenceValue == null)
        {
          pBg.objectReferenceValue = bgTr.GetComponent<Image>();
          modified = true;
        }

        if (barTr != null && pBar != null && pBar.objectReferenceValue == null)
        {
          pBar.objectReferenceValue = barTr.GetComponent<Image>();
          modified = true;
        }

        // Đảm bảo Background không bị gán nhầm làm border tô màu solid
        if (pLegacy != null && pLegacy.objectReferenceValue != null)
        {
          Image legacyImg = pLegacy.objectReferenceValue as Image;
          if (legacyImg != null)
          {
            legacyImg.color = Color.white;
          }
        }

        if (modified)
        {
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(promptUI);
        }
      }

      return modified;
    }

    private static bool SetupNotificationItem(GameObject notiObj, Sprite glowSprite, Sprite barSprite)
    {
      bool modified = false;

      Transform iconGroup = notiObj.transform.Find("IconGroup");
      if (iconGroup == null) return false;

      Transform bgTr = iconGroup.Find("RarityBG");
      if (bgTr != null)
      {
        Image bgImg = bgTr.GetComponent<Image>();
        if (bgImg != null && glowSprite != null && bgImg.sprite != glowSprite)
        {
          bgImg.sprite = glowSprite;
          modified = true;
        }
      }

      Transform barTr = iconGroup.Find("RarityBar");
      if (barTr == null)
      {
        GameObject barObj = new GameObject("RarityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barTr = barObj.transform;
        barTr.SetParent(iconGroup, false);

        RectTransform rt = barTr as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (bgTr != null)
        {
          barTr.SetSiblingIndex(bgTr.GetSiblingIndex() + 1);
        }

        Image barImg = barObj.GetComponent<Image>();
        barImg.sprite = barSprite;
        barImg.color = Color.white;
        barImg.raycastTarget = false;

        barObj.SetActive(true);
        modified = true;
      }
      else
      {
        Image barImg = barTr.GetComponent<Image>();
        if (barImg != null && barSprite != null && barImg.sprite != barSprite)
        {
          barImg.sprite = barSprite;
          modified = true;
        }
      }

      // 3. Đảm bảo Icon GameObject luôn active để hiển thị ảnh icon
      Transform iconTr = iconGroup.Find("Icon");
      if (iconTr != null && !iconTr.gameObject.activeSelf)
      {
        iconTr.gameObject.SetActive(true);
        modified = true;
      }

      PickupNotificationItem notiUI = notiObj.GetComponent<PickupNotificationItem>();
      if (notiUI != null)
      {
        SerializedObject so = new SerializedObject(notiUI);
        SerializedProperty pIcon = so.FindProperty("itemIcon");
        SerializedProperty pBg = so.FindProperty("rarityBg");
        SerializedProperty pBar = so.FindProperty("rarityBar");

        if (iconTr != null && pIcon != null && pIcon.objectReferenceValue == null)
        {
          pIcon.objectReferenceValue = iconTr.GetComponent<Image>();
          modified = true;
        }

        if (bgTr != null && pBg != null && pBg.objectReferenceValue == null)
        {
          pBg.objectReferenceValue = bgTr.GetComponent<Image>();
          modified = true;
        }

        if (barTr != null && pBar != null && pBar.objectReferenceValue == null)
        {
          pBar.objectReferenceValue = barTr.GetComponent<Image>();
          modified = true;
        }

        if (modified)
        {
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(notiUI);
        }
      }

      return modified;
    }
  }
}
#endif
