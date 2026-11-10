#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using InfiniteLabyrinth.UI;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  [CustomEditor(typeof(InventoryUIController))]
  public class InventoryUIControllerEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      InventoryUIController controller = (InventoryUIController)target;

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("=== THỬ NGHIỆM HOẠT ẢNH TAB (PLAY MODE / PREVIEW) ===", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("🌟 Tất Cả (All)", GUILayout.Height(28)))
        {
          controller.SwitchTab(InventoryUIController.InventoryTab.All);
        }

        if (GUILayout.Button("⚔️ Vũ Khí (Weapon)", GUILayout.Height(28)))
        {
          controller.SwitchTab(InventoryUIController.InventoryTab.Weapon);
        }

        if (GUILayout.Button("🛡️ Giáp (Armor)", GUILayout.Height(28)))
        {
          controller.SwitchTab(InventoryUIController.InventoryTab.Armor);
        }

        if (GUILayout.Button("🧪 Vật Phẩm (Item)", GUILayout.Height(28)))
        {
          controller.SwitchTab(InventoryUIController.InventoryTab.Item);
        }
      }

      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("=== ĐIỀU KHIỂN & KIỂM TRA USABLE QUICK SLOT (BÌNH MÁU) ===", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope())
      {
        GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
        if (GUILayout.Button("⚡ Kết Nối UsableSlot & UsableSlotUI (1-Click)", GUILayout.Height(30)))
        {
          SetupUsableSlotSystem();
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
        if (GUILayout.Button("🧪 Chạy Thử Cooldown 2s", GUILayout.Height(30)))
        {
          if (InventoryManager.HasInstance)
          {
            InventoryManager.Instance.TriggerConsumableCooldown();
            Debug.Log("<color=yellow>[Inventory]</color> Đã kích hoạt chạy thử Shared Consumable Cooldown 2.0s!");
          }
          else
          {
            Debug.LogWarning("[Inventory] Cần vào Play Mode để chạy thử Cooldown của InventoryManager!");
          }
        }
        GUI.backgroundColor = Color.white;
      }

      EditorGUILayout.Space(8);
      if (GUILayout.Button("🔍 Tự Động Tìm & Gắn 4 Nút TabBar (Auto-Wire)", GUILayout.Height(30)))
      {
        Undo.RecordObject(controller, "Auto Wire Tab Buttons");
        controller.AutoFindTabs();
        controller.UpdateTabButtonsVisual(animate: false);
        EditorUtility.SetDirty(controller);
        Debug.Log("<color=green>[InventoryUIControllerEditor]</color> Đã tự động kết nối 4 nút TabBar thành công!");
      }
    }

    [MenuItem("Infinite Labyrinth/Inventory/1. Kiểm Tra & Cấu Hình TabBar Animation (1-Click)", priority = 100)]
    public static void SetupTabBarAnimation()
    {
      InventoryUIController controller = Object.FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
      if (controller == null)
      {
        string[] guids = AssetDatabase.FindAssets("Canvas t:Prefab");
        foreach (string guid in guids)
        {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          if (prefab != null)
          {
            controller = prefab.GetComponentInChildren<InventoryUIController>(true);
            if (controller != null) break;
          }
        }
      }

      if (controller == null)
      {
        Debug.LogWarning("[InventoryUIControllerEditor] Không tìm thấy InventoryUIController trong Scene hoặc Prefab!");
        return;
      }

      Undo.RecordObject(controller, "Setup TabBar Animation");
      controller.AutoFindTabs();
      controller.UpdateTabButtonsVisual(animate: false);
      EditorUtility.SetDirty(controller);

      if (PrefabUtility.IsPartOfAnyPrefab(controller))
      {
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
      }

      Debug.Log("<color=green>[InventoryUIControllerEditor]</color> Đã cấu hình và kết nối hoàn tất hiệu ứng phồng nút TabBar cho Inventory!");
    }

    [MenuItem("Infinite Labyrinth/Inventory/2. Cấu Hình & Kết Nối Usable Quick Slot (1-Click)", priority = 101)]
    public static void SetupUsableSlotSystem()
    {
      string[] guids = AssetDatabase.FindAssets("Canvas t:Prefab");
      int countUpdated = 0;

      foreach (string guid in guids)
      {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null) continue;

        bool isDirty = false;

        // 1. Kiểm tra UsableSlotUI trên HUD
        Transform usableSlotUIObj = prefabRoot.transform.Find("UsableSlotUI");
        if (usableSlotUIObj != null)
        {
          UsableSlotUI slotUI = usableSlotUIObj.GetComponent<UsableSlotUI>();
          if (slotUI == null)
          {
            slotUI = usableSlotUIObj.gameObject.AddComponent<UsableSlotUI>();
            isDirty = true;
          }
          slotUI.AutoFindReferences();
        }

        // 2. Kiểm tra UsableSlot ở LeftPanel
        Transform usableSlotObj = prefabRoot.transform.Find("InventoryUI/LeftPanel/UsableSlot");
        if (usableSlotObj != null)
        {
          EquipmentSlotUI eqSlot = usableSlotObj.GetComponent<EquipmentSlotUI>();
          if (eqSlot != null)
          {
            SerializedObject soEq = new SerializedObject(eqSlot);
            soEq.Update();
            SerializedProperty propType = soEq.FindProperty("slotType");
            if (propType != null && propType.enumValueIndex != (int)ItemType.Consumable)
            {
              propType.enumValueIndex = (int)ItemType.Consumable;
              soEq.ApplyModifiedProperties();
              isDirty = true;
            }
          }
        }

        // 3. Kết nối vào InventoryUIController
        InventoryUIController ctrl = prefabRoot.GetComponentInChildren<InventoryUIController>(true);
        if (ctrl != null)
        {
          SerializedObject soCtrl = new SerializedObject(ctrl);
          soCtrl.Update();

          SerializedProperty pUsable = soCtrl.FindProperty("usableSlot");
          SerializedProperty pHud = soCtrl.FindProperty("hudUsableSlot");

          if (usableSlotObj != null && pUsable != null && pUsable.objectReferenceValue == null)
          {
            pUsable.objectReferenceValue = usableSlotObj.GetComponent<EquipmentSlotUI>();
            isDirty = true;
          }

          if (usableSlotUIObj != null && pHud != null && pHud.objectReferenceValue == null)
          {
            pHud.objectReferenceValue = usableSlotUIObj.GetComponent<UsableSlotUI>();
            isDirty = true;
          }

          soCtrl.ApplyModifiedProperties();
        }

        if (isDirty)
        {
          PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
          countUpdated++;
          Debug.Log($"<color=green>[InventoryUIControllerEditor]</color> Đã tự động cấu hình và lưu Prefab tại {path}!");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
      }

      Debug.Log($"<color=green>[InventoryUIControllerEditor]</color> Đã kiểm tra và đồng bộ hoàn tất Usable Quick Slot cho {countUpdated} prefab!");
    }
  }
}
#endif
