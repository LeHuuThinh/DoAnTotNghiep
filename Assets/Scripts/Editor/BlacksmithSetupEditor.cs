using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Blacksmith;
using InfiniteLabyrinth.UI;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.EditorTools
{
  /// <summary>
  /// Công cụ tự động hóa 1-Click thiết lập hệ thống Lò Rèn Haven (Blacksmith System - Task RPG-08):
  /// 1. Khởi tạo 6 ScriptableObject CraftingRecipe (Thần binh từ Lõi Boss & Nguyên liệu quái)
  /// 2. Cấu hình component BlacksmithUIController & ForgeRecipeSlotUI trên BlackSmithCanvas
  /// 3. Gắn BlacksmithInteractable lên BlackSmithNPC trong LandScene
  /// </summary>
  public static class BlacksmithSetupEditor
  {
    private const string CRAFTING_FOLDER = "Assets/ScriptableObjects/Crafting";
    private const string ITEMS_FOLDER = "Assets/ScriptableObjects/Items";

    [MenuItem("Infinite Labyrinth/RPG/7. Thiết Lập Lò Rèn Haven (1-Click)", false, 107)]
    public static void SetupBlacksmithSystem()
    {
      Debug.Log("<b>[BlacksmithSetup]</b> Bắt đầu thiết lập Hệ thống Lò Rèn Làng Haven (RPG-08)...");

      // 1. Tạo thư mục và khởi tạo 6 công thức chế tạo thần binh
      EnsureFolder(CRAFTING_FOLDER);
      List<CraftingRecipe> recipes = CreateAllCraftingRecipes();

      // 2. Tìm BlackSmithCanvas trong Scene
      GameObject canvasObj = GameObject.Find("BlackSmithCanvas");
      if (canvasObj == null)
      {
        Debug.LogError("<b>[BlacksmithSetup]</b> Không tìm thấy 'BlackSmithCanvas' trong Scene hiện tại!");
        return;
      }

      Undo.RecordObject(canvasObj, "Setup BlackSmithCanvas");

      // Gắn BlacksmithUIController
      BlacksmithUIController controller = canvasObj.GetComponent<BlacksmithUIController>();
      if (controller == null)
      {
        controller = canvasObj.AddComponent<BlacksmithUIController>();
        Debug.Log("<b>[BlacksmithSetup]</b> Đã gắn BlacksmithUIController vào BlackSmithCanvas.");
      }

      controller.AutoFindReferences();

      // Cấu hình mẫu hàng trong ForgeScrollView
      Transform forgeContent = canvasObj.transform.Find("BlackSmithPanel/ForgePanel/ItemForgeScrollView/Viewport/Content");
      if (forgeContent != null)
      {
        for (int i = 0; i < forgeContent.childCount; i++)
        {
          Transform child = forgeContent.GetChild(i);
          // Gỡ ShopSlotUI nếu còn sót lại
          var oldShopSlot = child.GetComponent<ShopSlotUI>();
          if (oldShopSlot != null)
          {
            Undo.DestroyObjectImmediate(oldShopSlot);
          }

          var recipeSlot = child.GetComponent<ForgeRecipeSlotUI>();
          if (recipeSlot == null)
          {
            recipeSlot = child.gameObject.AddComponent<ForgeRecipeSlotUI>();
            Undo.RegisterCreatedObjectUndo(recipeSlot, "Add ForgeRecipeSlotUI");
          }
          recipeSlot.AutoFindReferences();
        }
      }

      // Cấu hình ô UpgradeSlot trong BeforeWindow (hỗ trợ kéo thả đe rèn)
      Transform beforeWindowTr = canvasObj.transform.Find("BlackSmithPanel/UpgradePanel/BeforeWindow");
      if (beforeWindowTr != null)
      {
        Transform slotTr = beforeWindowTr.Find("UpgradeSlot");
        if (slotTr != null)
        {
          var upSlotUI = slotTr.GetComponent<BlacksmithUpgradeSlotUI>();
          if (upSlotUI == null)
          {
            upSlotUI = slotTr.gameObject.AddComponent<BlacksmithUpgradeSlotUI>();
            Undo.RegisterCreatedObjectUndo(upSlotUI, "Add BlacksmithUpgradeSlotUI");
          }
          upSlotUI.AutoFindReferences();
        }
      }

      // Cấu hình ItemDetailPopup
      Transform popupTr = canvasObj.transform.Find("ItemDetailPopup");
      if (popupTr != null)
      {
        var popupUI = popupTr.GetComponent<ItemDetailPopupUI>();
        if (popupUI != null)
        {
          popupUI.SetBlacksmithMode(true);
        }
      }

      // Gán danh sách công thức vào controller bằng SerializedObject
      SerializedObject soController = new SerializedObject(controller);
      SerializedProperty spRecipes = soController.FindProperty("craftingRecipes");
      if (spRecipes != null)
      {
        spRecipes.ClearArray();
        for (int i = 0; i < recipes.Count; i++)
        {
          spRecipes.InsertArrayElementAtIndex(i);
          spRecipes.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
        }
        soController.ApplyModifiedProperties();
      }

      // 3. Tìm BlackSmithNPC trong Scene và gắn BlacksmithInteractable
      GameObject npcObj = GameObject.Find("BlackSmithNPC");
      if (npcObj == null)
      {
        // Thử tìm dưới cụm BlackSmith
        GameObject bsCluster = GameObject.Find("BlackSmith");
        if (bsCluster != null)
        {
          var trans = bsCluster.transform.Find("BlackSmithNPC");
          if (trans != null) npcObj = trans.gameObject;
        }
      }

      if (npcObj != null)
      {
        Undo.RecordObject(npcObj, "Setup BlacksmithInteractable");

        // Gỡ ShopInteractable cũ nếu có
        var oldShop = npcObj.GetComponent<ShopInteractable>();
        if (oldShop != null) Undo.DestroyObjectImmediate(oldShop);

        var interactable = npcObj.GetComponent<BlacksmithInteractable>();
        if (interactable == null)
        {
          interactable = npcObj.AddComponent<BlacksmithInteractable>();
          Undo.RegisterCreatedObjectUndo(interactable, "Add BlacksmithInteractable");
          Debug.Log("<b>[BlacksmithSetup]</b> Đã gắn BlacksmithInteractable vào BlackSmithNPC.");
        }

        interactable.Setup("[F] Forge & Upgrade Armaments", "Master Blacksmith", 3.2f);
      }
      else
      {
        Debug.LogWarning("<b>[BlacksmithSetup]</b> Không tìm thấy GameObject 'BlackSmithNPC' trong Scene!");
      }

      // Đánh dấu Scene đã thay đổi
      EditorSceneManager.MarkSceneDirty(canvasObj.scene);
      AssetDatabase.SaveAssets();

      Debug.Log("<color=#59F259><b>[BlacksmithSetup] HOÀN TẤT 100%!</b></color> Hệ thống Lò Rèn Haven (RPG-08) đã sẵn sàng.");
    }

    private static List<CraftingRecipe> CreateAllCraftingRecipes()
    {
      var list = new List<CraftingRecipe>();

      // 1. Cốt Ma Kiếm (Bone Greatsword)
      list.Add(GetOrCreateRecipe(
        "recipe_sword_bone",
        "Bone Greatsword",
        "Forged from dense monster skeletal fragments. Brutal heavy cleaving edge.",
        "Item_Sword_Bone",
        1,
        150,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Bone_Fragment", 5),
          ("Item_Mat_Warrior_Insignia", 1)
        }
      ));

      // 2. Hồ Ly Katana (Kitsune Blade)
      list.Add(GetOrCreateRecipe(
        "recipe_sword_katsunesi",
        "Kitsune Katana",
        "Elegantly folded spiritual steel enchanted with arcane dust and ancient parchment.",
        "Item_Sword_Katsunesi",
        1,
        250,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Bone_Fragment", 3),
          ("Item_Mat_Arcane_Dust", 2),
          ("Item_Mat_Spell_Parchment", 1)
        }
      ));

      // 3. Băng Nha Cực Kiếm (Frostfang Claymore)
      list.Add(GetOrCreateRecipe(
        "recipe_sword_ice",
        "Frostfang Claymore",
        "Legendary blade infused with the glacial core of the Ice Golem. Chills targets to the bone.",
        "Item_Sword_Ice",
        1,
        500,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Ice_Core", 1),
          ("Item_Mat_Ice_Rune", 2),
          ("Item_Stone_Upgrade", 4)
        }
      ));

      // 4. Hỏa Long Diệt Thế Kiếm (Draconic Flameblade)
      list.Add(GetOrCreateRecipe(
        "recipe_sword_dragonic",
        "Draconic Flameblade",
        "Peerless greatsword forged with the blazing flame core of Red Ignis. Burns with inextinguishable fury.",
        "Item_Sword_Dragonic",
        1,
        800,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Fire_Core", 1),
          ("Item_Mat_Fire_Crest", 2),
          ("Item_Mat_Flawless_Crystal", 2)
        }
      ));

      // 5. Giáp Xương Quái (Bone Carapace)
      list.Add(GetOrCreateRecipe(
        "recipe_armor_bone",
        "Bone Carapace",
        "Hardened skeletal armor reinforced with warrior insignias. Sturdy physical defense.",
        "Item_Armor_Bone_Carapace",
        1,
        200,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Bone_Fragment", 6),
          ("Item_Mat_Warrior_Insignia", 2)
        }
      ));

      // 6. Băng Giáp Cực Bắc (Frost Guardian Armor)
      list.Add(GetOrCreateRecipe(
        "recipe_armor_frost",
        "Frost Guardian Armor",
        "Divine mail crafted from ancient ice runes and flawless crystal. Exceptional defense against the elements.",
        "Item_Armor_Frost_Guardian",
        1,
        600,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Ice_Core", 1),
          ("Item_Mat_Ice_Rune", 2),
          ("Item_Mat_Flawless_Crystal", 1)
        }
      ));

      return list;
    }

    private static CraftingRecipe GetOrCreateRecipe(
      string recipeId,
      string recipeName,
      string desc,
      string resultItemAssetName,
      int resultCount,
      int goldCost,
      List<(string matItemName, int qty)> materials)
    {
      string assetPath = $"{CRAFTING_FOLDER}/{recipeId}.asset";
      CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(assetPath);
      bool isNew = false;

      if (recipe == null)
      {
        recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
        isNew = true;
      }

      ItemData resultItem = LoadItemAsset(resultItemAssetName);
      var matCosts = new List<CraftingMaterialCost>();

      foreach (var m in materials)
      {
        ItemData matItem = LoadItemAsset(m.matItemName);
        if (matItem != null)
        {
          matCosts.Add(new CraftingMaterialCost(matItem, m.qty));
        }
        else
        {
          Debug.LogWarning($"[BlacksmithSetup] Không tìm thấy nguyên liệu: {m.matItemName}");
        }
      }

      recipe.Setup(recipeId, recipeName, desc, resultItem, resultCount, goldCost, matCosts);

      if (isNew)
      {
        AssetDatabase.CreateAsset(recipe, assetPath);
      }
      else
      {
        EditorUtility.SetDirty(recipe);
      }

      return recipe;
    }

    private static ItemData LoadItemAsset(string assetName)
    {
      string path = $"{ITEMS_FOLDER}/{assetName}.asset";
      var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
      if (item != null) return item;

      string[] guids = AssetDatabase.FindAssets($"{assetName} t:ItemData");
      if (guids != null && guids.Length > 0)
      {
        string p = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ItemData>(p);
      }
      return null;
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
