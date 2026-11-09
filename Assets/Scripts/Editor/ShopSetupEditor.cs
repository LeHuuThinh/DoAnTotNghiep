using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Shop;
using InfiniteLabyrinth.UI;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.EditorTools
{
  /// <summary>
  /// Công cụ tự động hóa 1-Click thiết lập hệ thống Cửa Hàng Làng Haven (Shop System - Task RPG-07):
  /// 1. Khởi tạo 2 ScriptableObject: EquipmentShopData & SuppliesShopData
  /// 2. Cấu hình component ShopUIController & ShopSlotUI trên ShopCanvas trong Scene
  /// 3. Cân chỉnh vị trí InventoryPanel tránh đè mép, nâng scroll sensitivity
  /// 4. Gắn ShopInteractable lên 2 NPC EquipmentShopNPC và SuppliesShopNPC trong LandScene
  /// </summary>
  public static class ShopSetupEditor
  {
    private const string SHOP_FOLDER = "Assets/ScriptableObjects/Shop";
    private const string EQUIPMENT_SHOP_PATH = "Assets/ScriptableObjects/Shop/EquipmentShopData.asset";
    private const string SUPPLIES_SHOP_PATH = "Assets/ScriptableObjects/Shop/SuppliesShopData.asset";
    private const string ITEMS_FOLDER = "Assets/ScriptableObjects/Items";

    [MenuItem("Infinite Labyrinth/RPG/6. Thiết Lập Shop Làng Haven (1-Click)", false, 106)]
    public static void SetupShopSystem()
    {
      Debug.Log("<b>[ShopSetup]</b> Bắt đầu thiết lập Hệ thống Cửa Hàng Làng Haven...");

      // 1. Tạo hoặc nạp 2 file ShopData
      EnsureFolder(SHOP_FOLDER);
      ShopData equipShopData = SetupEquipmentShopData();
      ShopData suppliesShopData = SetupSuppliesShopData();

      // 2. Cấu hình ShopCanvas trong Scene
      GameObject shopCanvasObj = GameObject.Find("ShopCanvas");
      if (shopCanvasObj == null)
      {
        Debug.LogError("<b>[ShopSetup]</b> Không tìm thấy 'ShopCanvas' trong Scene hiện tại!");
        return;
      }

      Undo.RecordObject(shopCanvasObj, "Setup ShopCanvas");

      // Gắn ShopUIController
      ShopUIController shopController = shopCanvasObj.GetComponent<ShopUIController>();
      if (shopController == null)
      {
        shopController = shopCanvasObj.AddComponent<ShopUIController>();
        Debug.Log("<b>[ShopSetup]</b> Đã gắn ShopUIController vào ShopCanvas.");
      }

      // Cân chỉnh khoảng cách giữa 2 panel (tránh đè mép 5px)
      Transform invPanel = shopCanvasObj.transform.Find("InventoryPanel");
      if (invPanel is RectTransform invRect)
      {
        Undo.RecordObject(invRect, "Adjust InventoryPanel PosX");
        Vector2 pos = invRect.anchoredPosition;
        if (pos.x < -430f)
        {
          pos.x = -400f; // Nhích sang phải để tạo khoảng hở ~50px thoáng đãng
          invRect.anchoredPosition = pos;
          Debug.Log("<b>[ShopSetup]</b> Đã chỉnh AnchoredPosition.x của InventoryPanel từ -458 sang -400 (hở 50px).");
        }
      }

      // Tăng Scroll Sensitivity và đổi tên typo GoidIcon
      Transform shopPanel = shopCanvasObj.transform.Find("ShopPanel");
      if (shopPanel != null)
      {
        Transform scrollViewTr = shopPanel.Find("ItemSellScrollView");
        if (scrollViewTr != null)
        {
          ScrollRect scrollRect = scrollViewTr.GetComponent<ScrollRect>();
          if (scrollRect != null)
          {
            Undo.RecordObject(scrollRect, "Set Scroll Sensitivity");
            scrollRect.scrollSensitivity = 20f;
            Debug.Log("<b>[ShopSetup]</b> Đã nâng ScrollSensitivity lên 20 cho cuộn chuột mượt mà.");
          }

          Transform contentTr = scrollViewTr.Find("Viewport/Content");
          if (contentTr != null)
          {
            // Gắn ShopSlotUI vào các dòng ShopItemRow mẫu hiện có
            var rows = contentTr.GetComponentsInChildren<Transform>(true);
            foreach (var r in rows)
            {
              if (r != contentTr && r.name.StartsWith("ShopItemRow"))
              {
                // Sửa typo GoidIcon -> GoldIcon
                Transform goidIcon = r.Find("Price/GoidIcon");
                if (goidIcon != null)
                {
                  goidIcon.name = "GoldIcon";
                }

                // Kích hoạt GameObject Icon nếu bị ẩn
                Transform itemIconTr = r.Find("ItemIcon");
                if (itemIconTr != null)
                {
                  itemIconTr.gameObject.SetActive(true);
                  Transform iconChild = itemIconTr.Find("Icon") ?? itemIconTr.Find("ItemIcon");
                  if (iconChild != null)
                  {
                    iconChild.gameObject.SetActive(true);
                    EditorUtility.SetDirty(iconChild.gameObject);
                  }
                }

                ShopSlotUI slot = r.GetComponent<ShopSlotUI>();
                if (slot == null)
                {
                  slot = r.gameObject.AddComponent<ShopSlotUI>();
                }
                slot.AutoFindReferences();
                EditorUtility.SetDirty(r.gameObject);
              }
            }
          }
        }

        // Kích hoạt icon trong ItemDetail nếu bị ẩn
        Transform detailTr = shopPanel.Find("ItemDetail");
        if (detailTr != null)
        {
          Transform iconTr = detailTr.Find("Icon");
          if (iconTr != null)
          {
            iconTr.gameObject.SetActive(true);
            Transform itemIcon = iconTr.Find("ItemIcon") ?? iconTr.Find("Icon");
            if (itemIcon != null)
            {
              itemIcon.gameObject.SetActive(true);
              EditorUtility.SetDirty(itemIcon.gameObject);
            }
          }
        }
      }

      // Tự động tìm kiếm toàn bộ tham chiếu cho ShopUIController
      shopController.AutoFindReferences();

      // Cập nhật và liên kết đúng nút BtnSell cho ItemDetailPopupUI (tránh nhầm BtnDrop)
      var popupUI = shopCanvasObj.GetComponentInChildren<ItemDetailPopupUI>(true);
      if (popupUI != null)
      {
        var so = new SerializedObject(popupUI);
        var dropProp = so.FindProperty("btnDrop");
        var sellProp = so.FindProperty("btnSell");
        if (dropProp != null && dropProp.objectReferenceValue != null && dropProp.objectReferenceValue.name == "BtnSell")
        {
          if (sellProp != null) sellProp.objectReferenceValue = dropProp.objectReferenceValue;
          dropProp.objectReferenceValue = null;
          so.ApplyModifiedProperties();
        }
      }

      EditorUtility.SetDirty(shopCanvasObj);

      // 3. Gắn ShopInteractable lên 2 NPC trong Làng (English prompts & names)
      SetupNPC("EquipmentShopNPC", equipShopData, "Browse Equipment", "Equipment Merchant");
      SetupNPC("SuppliesShopNPC", suppliesShopData, "Browse Supplies", "Village Alchemist");

      // Đánh dấu Scene đã sửa đổi để lưu
      EditorSceneManager.MarkSceneDirty(shopCanvasObj.scene);

      Debug.Log("<color=#44FF44><b>[ShopSetup] THÀNH CÔNG 100%!</b></color> Hệ thống Cửa Hàng đã được thiết lập hoàn chỉnh:");
      Debug.Log("- EquipmentShopData: " + EQUIPMENT_SHOP_PATH);
      Debug.Log("- SuppliesShopData: " + SUPPLIES_SHOP_PATH);
      Debug.Log("- ShopCanvas: Đã cấu hình ShopUIController & ShopSlotUI, cân chỉnh PosX=-400, scrollSensitivity=20.");
      Debug.Log("- 2 NPC Shop: Đã gắn ShopInteractable sẵn sàng tương tác phím F!");
    }

    private static ShopData SetupEquipmentShopData()
    {
      ShopData data = AssetDatabase.LoadAssetAtPath<ShopData>(EQUIPMENT_SHOP_PATH);
      if (data == null)
      {
        data = ScriptableObject.CreateInstance<ShopData>();
        AssetDatabase.CreateAsset(data, EQUIPMENT_SHOP_PATH);
      }

      var so = new SerializedObject(data);
      so.FindProperty("shopId").stringValue = "equipment_shop";
      so.FindProperty("shopTitle").stringValue = "EQUIPMENT & ARMORY";
      so.FindProperty("shopDescription").stringValue = "Basic weapons and sturdy armor for adventurers.";
      so.ApplyModifiedProperties();

      var items = new List<ShopItemEntry>();
      AddItem(items, "Item_Sword_Novice", 60, -1);
      AddItem(items, "Item_Sword_Rusty_Iron", 40, -1);
      AddItem(items, "Item_Sword_Broad_Iron", 100, -1);
      AddItem(items, "Item_Sword_Vanguard", 180, -1);
      AddItem(items, "Item_Sword_DarkIron", 250, -1);
      AddItem(items, "Item_Armor_Cloth_Novice", 50, -1);
      AddItem(items, "Item_Armor_Leather_Scout", 100, -1);
      AddItem(items, "Item_Armor_Leather", 180, -1);
      AddItem(items, "Item_Armor_Iron_Cuirass", 300, -1);

      data.SetItems(items);
      EditorUtility.SetDirty(data);
      AssetDatabase.SaveAssets();
      return data;
    }

    private static ShopData SetupSuppliesShopData()
    {
      ShopData data = AssetDatabase.LoadAssetAtPath<ShopData>(SUPPLIES_SHOP_PATH);
      if (data == null)
      {
        data = ScriptableObject.CreateInstance<ShopData>();
        AssetDatabase.CreateAsset(data, SUPPLIES_SHOP_PATH);
      }

      // Đổi tiêu đề (English)
      var so = new SerializedObject(data);
      so.FindProperty("shopId").stringValue = "supplies_shop";
      so.FindProperty("shopTitle").stringValue = "ALCHEMIST & SUPPLIES";
      so.FindProperty("shopDescription").stringValue = "Healing draughts, stamina elixirs, and raw crafting materials.";
      so.ApplyModifiedProperties();

      var items = new List<ShopItemEntry>();
      AddItem(items, "Item_Potion_Health", 20, -1);
      AddItem(items, "Item_Potion_Health_Large", 60, -1);
      AddItem(items, "Item_Potion_Stamina_Elixir", 35, -1);
      AddItem(items, "Item_Potion_Spirit_Nectar", 45, -1);
      AddItem(items, "Item_Potion_Ironbark_Brew", 50, 10);
      AddItem(items, "Item_Potion_Berserker", 60, 10);
      AddItem(items, "Item_Stone_Upgrade", 50, 15);

      data.SetItems(items);
      EditorUtility.SetDirty(data);
      AssetDatabase.SaveAssets();
      return data;
    }

    private static void AddItem(List<ShopItemEntry> list, string itemName, int price, int stock)
    {
      string path = $"{ITEMS_FOLDER}/{itemName}.asset";
      ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
      if (item != null)
      {
        list.Add(new ShopItemEntry(item, price, stock));
      }
      else
      {
        Debug.LogWarning($"[ShopSetup] Không tìm thấy ItemData: {path}");
      }
    }

    private static void SetupNPC(string npcName, ShopData shopData, string prompt, string targetName)
    {
      GameObject npcObj = GameObject.Find(npcName);
      if (npcObj == null)
      {
        // Thử tìm trong các đối tượng con
        var allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
          if (obj.name == npcName && obj.scene.isLoaded)
          {
            npcObj = obj;
            break;
          }
        }
      }

      if (npcObj != null)
      {
        Undo.RecordObject(npcObj, "Setup Shop NPC");

        // Đảm bảo có Collider để PlayerInteraction phát hiện được
        Collider col = npcObj.GetComponent<Collider>();
        if (col == null)
        {
          CapsuleCollider cap = npcObj.AddComponent<CapsuleCollider>();
          cap.height = 1.8f;
          cap.center = new Vector3(0f, 0.9f, 0f);
          cap.radius = 0.45f;
          cap.isTrigger = true;
          Debug.Log($"<b>[ShopSetup]</b> Đã thêm CapsuleCollider (Trigger) cho {npcName}.");
        }

        // Gắn ShopInteractable
        ShopInteractable interactable = npcObj.GetComponent<ShopInteractable>();
        if (interactable == null)
        {
          interactable = npcObj.AddComponent<ShopInteractable>();
        }
        interactable.SetShopData(shopData, prompt, targetName);
        EditorUtility.SetDirty(npcObj);
        Debug.Log($"<b>[ShopSetup]</b> Đã cài đặt ShopInteractable cho {npcName} ({targetName}).");
      }
      else
      {
        Debug.LogWarning($"[ShopSetup] Không tìm thấy NPC '{npcName}' trong Scene!");
      }
    }

    private static void EnsureFolder(string path)
    {
      if (!AssetDatabase.IsValidFolder(path))
      {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
          string next = current + "/" + parts[i];
          if (!AssetDatabase.IsValidFolder(next))
          {
            AssetDatabase.CreateFolder(current, parts[i]);
          }
          current = next;
        }
      }
    }
  }
}
