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
    private const string CANVAS_PREFAB_PATH = "Assets/UI RPG/BlackSmithCanvas.prefab";

    // [MenuItem("Infinite Labyrinth/RPG/7. Thiết Lập Lò Rèn Haven (1-Click)", false, 107)]
    public static void SetupBlacksmithSystem()
    {
      Debug.Log("<b>[BlacksmithSetup]</b> Bắt đầu thiết lập Hệ thống Lò Rèn Làng Haven (Non-Destructive Tooling)...");

      // 1. Tạo thư mục và khởi tạo danh sách công thức chế tạo thần binh (Bảo toàn công thức đã có)
      EnsureFolder(CRAFTING_FOLDER);
      List<CraftingRecipe> recipes = CreateAllCraftingRecipes();

      // 2. Tìm hoặc đồng bộ BlackSmithCanvas
      GameObject canvasObj = GameObject.Find("BlackSmithCanvas");
      if (canvasObj != null)
      {
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

        // Cấu hình ô UpgradeSlot trong BeforeWindow
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

        // Gán danh sách công thức & cơ sở dữ liệu item vào controller bằng SerializedObject (NON-DESTRUCTIVE MERGE)
        SerializedObject soController = new SerializedObject(controller);
        MergeRecipesIntoSerializedProperty(soController.FindProperty("craftingRecipes"), recipes);
        MergeItemsIntoSerializedProperty(soController.FindProperty("itemDatabase"));
        soController.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(canvasObj.scene);
      }
      else
      {
        Debug.Log("<b>[BlacksmithSetup]</b> Không tìm thấy 'BlackSmithCanvas' trong Scene hiện tại, tiến hành đồng bộ trực tiếp vào Prefab Asset...");
      }

      // 3. Đồng bộ trực tiếp vào Prefab asset trên đĩa để đảm bảo Standalone Build luôn có đủ dữ liệu 100%
      GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(CANVAS_PREFAB_PATH);
      if (prefabRoot != null)
      {
        BlacksmithUIController prefabController = prefabRoot.GetComponent<BlacksmithUIController>();
        if (prefabController != null)
        {
          SerializedObject soPrefab = new SerializedObject(prefabController);
          MergeRecipesIntoSerializedProperty(soPrefab.FindProperty("craftingRecipes"), recipes);
          MergeItemsIntoSerializedProperty(soPrefab.FindProperty("itemDatabase"));
          soPrefab.ApplyModifiedProperties();
          EditorUtility.SetDirty(prefabRoot);
          Debug.Log($"<b>[BlacksmithSetup]</b> <color=cyan>Đã nạp cứng itemDatabase & craftingRecipes vào Prefab:</color> {CANVAS_PREFAB_PATH}");
        }
      }

      // 4. Tìm BlackSmithNPC trong Scene và gắn BlacksmithInteractable (NON-DESTRUCTIVE)
      GameObject npcObj = GameObject.Find("BlackSmithNPC");
      if (npcObj == null)
      {
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

        var oldShop = npcObj.GetComponent<ShopInteractable>();
        if (oldShop != null) Undo.DestroyObjectImmediate(oldShop);

        bool isNewInteractable = false;
        var interactable = npcObj.GetComponent<BlacksmithInteractable>();
        if (interactable == null)
        {
          interactable = npcObj.AddComponent<BlacksmithInteractable>();
          Undo.RegisterCreatedObjectUndo(interactable, "Add BlacksmithInteractable");
          isNewInteractable = true;
          Debug.Log("<b>[BlacksmithSetup]</b> Đã gắn BlacksmithInteractable vào BlackSmithNPC.");
        }

        // NON-DESTRUCTIVE: Chỉ khởi tạo giá trị mặc định nếu component mới được tạo
        // Nếu người dùng đã tùy chỉnh câu prompt hay bán kính trong Inspector, bảo tồn nguyên vẹn!
        if (isNewInteractable)
        {
          interactable.Setup("[F] Forge & Upgrade Armaments", "Master Blacksmith", 3.2f);
        }
      }
      else
      {
        Debug.LogWarning("<b>[BlacksmithSetup]</b> Không tìm thấy GameObject 'BlackSmithNPC' trong Scene hiện tại!");
      }

      AssetDatabase.SaveAssets();
      Debug.Log("<color=#59F259><b>[BlacksmithSetup] HOÀN TẤT 100%!</b></color> Hệ thống Lò Rèn Haven (Non-Destructive) đã sẵn sàng.");
    }

    private static void MergeRecipesIntoSerializedProperty(SerializedProperty spRecipes, List<CraftingRecipe> recipes)
    {
      if (spRecipes == null) return;
      var existing = new HashSet<CraftingRecipe>();
      for (int i = 0; i < spRecipes.arraySize; i++)
      {
        var r = spRecipes.GetArrayElementAtIndex(i).objectReferenceValue as CraftingRecipe;
        if (r != null) existing.Add(r);
      }

      foreach (var r in recipes)
      {
        if (r != null && !existing.Contains(r))
        {
          int idx = spRecipes.arraySize;
          spRecipes.InsertArrayElementAtIndex(idx);
          spRecipes.GetArrayElementAtIndex(idx).objectReferenceValue = r;
          existing.Add(r);
        }
      }
    }

    private static void MergeItemsIntoSerializedProperty(SerializedProperty spItemDb)
    {
      if (spItemDb == null) return;
      var existing = new HashSet<ItemData>();
      for (int i = 0; i < spItemDb.arraySize; i++)
      {
        var it = spItemDb.GetArrayElementAtIndex(i).objectReferenceValue as ItemData;
        if (it != null) existing.Add(it);
      }

      string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { ITEMS_FOLDER });
      foreach (var g in itemGuids)
      {
        string p = AssetDatabase.GUIDToAssetPath(g);
        var it = AssetDatabase.LoadAssetAtPath<ItemData>(p);
        if (it != null && !existing.Contains(it))
        {
          int idx = spItemDb.arraySize;
          spItemDb.InsertArrayElementAtIndex(idx);
          spItemDb.GetArrayElementAtIndex(idx).objectReferenceValue = it;
          existing.Add(it);
        }
      }
    }

    public static List<CraftingRecipe> CreateAllCraftingRecipes()
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

      // 7. Nhẫn Băng Vương (Frost Monarch Signet)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_frost_monarch",
        "Frost Monarch Signet",
        "Peerless ice signet that condenses frosty aura into a protective shield, drastically reducing skill stamina consumption (-12% Stamina Cost).",
        "Item_Acc_Frost_Monarch",
        1,
        450,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Ice_Core", 1),
          ("Item_Mat_Ice_Rune", 2),
          ("Item_Mat_Monster_Gem", 1)
        }
      ));

      // 8. Dây Chuyền Hư Không Nhãn (Void Eye Pendant)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_void_eye",
        "Void Eye Pendant",
        "A relic from the abyssal cosmic eye that gazes into the void. Catastrophically magnifies critical strike impact (+25% Critical Damage).",
        "Item_Acc_Void_Eye_Pendant",
        1,
        600,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Void_Essence", 4),
          ("Item_Mat_Void_Eye", 2),
          ("Item_Mat_Boss_Gem", 1)
        }
      ));

      // 9. Huy Hiệu Chiến Binh (Warrior Crest Insignia)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_warrior_crest",
        "Warrior Crest Insignia",
        "Honored badge of Haven frontline champions. Imbues the bearer with stalwart physical vigor.",
        "Item_Acc_Warrior_Crest",
        1,
        120,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Warrior_Insignia", 4),
          ("Item_Stone_Upgrade", 2)
        }
      ));

      // 10. Nhẫn Cuồng Nộ (Berserker Rage Ring)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_berserker",
        "Berserker Rage Ring",
        "Forged from hardened monster bones and glowing monster gems. Awakens latent bloodlust and lethal precision.",
        "Item_Acc_Berserker_Ring",
        1,
        220,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Bone_Fragment", 4),
          ("Item_Mat_Monster_Gem", 1),
          ("Item_Stone_Upgrade", 3)
        }
      ));

      // 11. Vòng Cổ Huyết Thạch (Bloodstone Choker)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_bloodstone",
        "Bloodstone Choker",
        "Mystical choker inlaid with pulsating monster gem and refined arcane dust. Feasts on the lifeforce of fallen foes.",
        "Item_Acc_Bloodstone_Choker",
        1,
        260,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Arcane_Dust", 2),
          ("Item_Mat_Monster_Gem", 1),
          ("Item_Stone_Upgrade", 3)
        }
      ));

      // 12. Hỏa Long Tâm Thạch (Crimson Dragon Heart)
      list.Add(GetOrCreateRecipe(
        "recipe_acc_dragon_heart",
        "Crimson Dragon Heart",
        "Supreme draconic relic pulsating with the molten heart of Red Ignis. Cataclysmically surges offensive might.",
        "Item_Acc_Crimson_Dragon_Heart",
        1,
        650,
        new List<(string matItemName, int qty)>
        {
          ("Item_Mat_Fire_Core", 1),
          ("Item_Mat_Fire_Crest", 2),
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

      // NON-DESTRUCTIVE: Nếu công thức đã tồn tại trên đĩa, bảo tồn 100% dữ liệu người dùng đã chỉnh sửa
      if (recipe != null)
      {
        return recipe;
      }

      recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
      bool isNew = true;

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
