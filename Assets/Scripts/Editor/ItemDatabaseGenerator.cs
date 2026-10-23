#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  public static class ItemDatabaseGenerator
  {
    private const string TargetFolder = "Assets/ScriptableObjects/Items";
    private const string PrefabFolder = "Assets/Prefabs/RPG";
    private const string WorldPickupPrefabPath = "Assets/Prefabs/RPG/WorldItemPickup.prefab";
    private const string GoldConfigPath = "Assets/Resources/GoldDropVisualConfig.asset";
    private const int DamageableLayer = 8; // Layer "Damageable" nhận sát thương từ kiếm

    [MenuItem("Infinite Labyrinth/RPG/1. Khởi Tạo Bộ Vật Phẩm Mẫu (Default Items)", false, 10)]
    public static void GenerateDefaultItems()
    {
      if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
      {
        AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
      }
      if (!AssetDatabase.IsValidFolder(TargetFolder))
      {
        AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");
      }

      // Tạo prefab WorldItemPickup trước nếu chưa có
      GameObject worldPickup = EnsureWorldItemPickupPrefab();

      // Swords Visual Prefabs
      GameObject woodenSwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Swords/WoodenSword.prefab");
      GameObject darkIronSwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Swords/DarkIronSword.prefab");
      GameObject boneSwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Swords/BoneSword.prefab");
      GameObject dragonicSwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Swords/DragonicSword.prefab");
      GameObject fallbackSword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/NPC/KayKit_Adventurers_2.0_FREE/Assets/fbx/sword_1handed.fbx");

      // Icons
      Sprite starIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/Spirit_Filled.png");
      Sprite coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/Exp_Circle.png");
      Sprite potionIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/HPBar_Fill.png");
      Sprite shieldIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/Avatar_Frame.png");
      Sprite swordIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/Bar_Corner.png");

      // 1. Tiền Vàng
      CreateOrUpdateItem("Item_Gold_Coin", "currency_gold", "Đồng Tiền Vàng",
        "Đơn vị tiền tệ lưu hành khắp vương quốc. Dùng để nâng cấp vũ khí tại Thợ Rèn hoặc mua nhu yếu phẩm.",
        coinIcon, ItemType.Currency, ItemRarity.Common, maxStack: 9999, goldValue: 1,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // 2. Bình Máu Nhỏ
      CreateOrUpdateItem("Item_Potion_Health", "potion_health_small", "Bình Máu Nhỏ",
        "Chiết xuất từ thảo mộc phát sáng trong hầm ngục. Hồi phục ngay lập tức 35 điểm Máu.",
        potionIcon, ItemType.Consumable, ItemRarity.Common, maxStack: 10, goldValue: 20,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 35f,
        worldPrefab: worldPickup, visualPrefab: null);

      // 3. Đá Nâng Cấp Kiếm
      CreateOrUpdateItem("Item_Stone_Upgrade", "mat_upgrade_stone", "Đá Hầm Ngục Cường Hóa",
        "Khoáng thạch cổ xưa tỏa ra ánh sáng ma thuật. Nguyên liệu chính để Thợ Rèn trui rèn và gia tăng sát thương vũ khí.",
        starIcon, ItemType.Material, ItemRarity.Rare, maxStack: 99, goldValue: 50,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // 4. Giáp Da Chiến Binh
      CreateOrUpdateItem("Item_Armor_Leather", "armor_warrior_leather", "Giáp Da Thám Hiểm",
        "Bộ giáp da dẻo dai giúp giảm sát thương nhận vào (+6 DEF) và tăng sức bền (+25 Max HP).",
        shieldIcon, ItemType.Armor, ItemRarity.Common, maxStack: 1, goldValue: 60,
        bonusAtk: 0, bonusDef: 6f, bonusHp: 25f, bonusStamina: 15f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // 5. Kiếm Gỗ Tập Sự (Common)
      CreateOrUpdateItem("Item_Sword_Wooden", "weapon_sword_wooden", "Kiếm Gỗ Tập Sự",
        "Thanh kiếm gỗ dành cho các tân binh mới bước chân vào mê cung (+6 Sát thương).",
        swordIcon, ItemType.Weapon, ItemRarity.Common, maxStack: 1, goldValue: 15,
        bonusAtk: 6f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: woodenSwordPrefab != null ? woodenSwordPrefab : fallbackSword);

      // 6. Kiếm Hắc Thiết (Common)
      CreateOrUpdateItem("Item_Sword_DarkIron", "weapon_sword_darkiron", "Kiếm Hắc Thiết",
        "Được đúc từ quặng sắt đen kiên cố dưới đáy hầm ngục (+16 Sát thương).",
        swordIcon, ItemType.Weapon, ItemRarity.Common, maxStack: 1, goldValue: 75,
        bonusAtk: 16f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: darkIronSwordPrefab != null ? darkIronSwordPrefab : fallbackSword);

      // 7. Kiếm Xương Quỷ (Rare)
      CreateOrUpdateItem("Item_Sword_Bone", "weapon_sword_bone", "Cốt Ma Kiếm",
        "Rèn từ xương sọ của Chiến Binh Bộ Xương cổ xưa (+28 Sát thương, +10 Thể lực).",
        swordIcon, ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 180,
        bonusAtk: 28f, bonusDef: 0, bonusHp: 0, bonusStamina: 10f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: boneSwordPrefab != null ? boneSwordPrefab : fallbackSword);

      // 8. Hỏa Long Thần Kiếm (Legendary)
      CreateOrUpdateItem("Item_Sword_Dragonic", "weapon_sword_dragonic", "Hỏa Long Kiếm",
        "Thần khí mang linh hồn của Rồng Lửa Viễn Cổ (+55 Sát thương, +50 Máu tối đa, +25 Thể lực).",
        swordIcon, ItemType.Weapon, ItemRarity.Legendary, maxStack: 1, goldValue: 500,
        bonusAtk: 55f, bonusDef: 0, bonusHp: 50f, bonusStamina: 25f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: dragonicSwordPrefab != null ? dragonicSwordPrefab : fallbackSword);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      EditorUtility.DisplayDialog("Infinite Labyrinth", 
        "Đã tạo và gắn thành công:\n1. Bảng PickupPromptUI (Phím [F] nổi khi lại gần item)\n2. Bảng PickupNotificationFeed (Thông báo trượt mép trái màn hình)!\n\nBạn có thể Play game để trải nghiệm ngay!", "OK");
      Debug.Log("[ItemDatabaseGenerator] Đã tạo thành công bộ vật phẩm mẫu trong Assets/ScriptableObjects/Items/");
    }

    [MenuItem("Infinite Labyrinth/RPG/2. Tạo Prefab Rơi Đồ (WorldItemPickup Prefab)", false, 11)]
    public static GameObject EnsureWorldItemPickupPrefab()
    {
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
      {
        AssetDatabase.CreateFolder("Assets", "Prefabs");
      }
      if (!AssetDatabase.IsValidFolder(PrefabFolder))
      {
        AssetDatabase.CreateFolder("Assets/Prefabs", "RPG");
      }

      GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPickupPrefabPath);
      if (existingPrefab != null)
      {
        return existingPrefab;
      }

      GameObject root = new GameObject("WorldItemPickup");
      root.layer = 0;
      var pickupComp = root.AddComponent<WorldItemPickup>();

      SphereCollider col = root.AddComponent<SphereCollider>();
      col.isTrigger = true;
      col.radius = 2.2f;

      GameObject trunkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/trunk_small_A.prefab");
      GameObject visualObj = new GameObject("Model");
      visualObj.transform.SetParent(root.transform, false);

      if (trunkPrefab != null)
      {
        GameObject trunkInstance = (GameObject)PrefabUtility.InstantiatePrefab(trunkPrefab, visualObj.transform);
        trunkInstance.transform.localPosition = Vector3.zero;
        trunkInstance.transform.localRotation = Quaternion.identity;
        trunkInstance.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
      }

      GameObject lightObj = new GameObject("RarityGlow");
      lightObj.transform.SetParent(root.transform, false);
      lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

      Light glow = lightObj.AddComponent<Light>();
      glow.type = LightType.Point;
      glow.range = 3.2f;
      glow.intensity = 3.0f;
      glow.shadows = LightShadows.None;
      glow.color = Color.white;

      GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, WorldPickupPrefabPath);
      GameObject.DestroyImmediate(root);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"[ItemDatabaseGenerator] Đã tạo thành công WorldItemPickup Prefab tại: {WorldPickupPrefabPath}");
      return savedPrefab;
    }

    public static GameObject EnsureGoldPickupPrefab(string prefabName, string sourceModelPath, float scale = 0.9f)
    {
      string savePath = $"{PrefabFolder}/{prefabName}.prefab";
      // Xóa prefab cũ nếu cần cập nhật cấu hình Layer và Trigger
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);

      GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
      if (sourceModel == null)
      {
        // Fallback đường dẫn mới Prefabs/Dungeon
        string altPath = sourceModelPath.Replace("Enviro Prefabs", "Prefabs");
        sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(altPath);
      }
      if (sourceModel == null) return existing;

      int itemLayer = LayerMask.NameToLayer("Item");
      if (itemLayer < 0) itemLayer = LayerMask.NameToLayer("Ignore Raycast");

      GameObject root = new GameObject(prefabName);
      if (itemLayer >= 0) root.layer = itemLayer;

      var pickupComp = root.AddComponent<WorldItemPickup>();
      SerializedObject so = new SerializedObject(pickupComp);
      so.FindProperty("magneticRadius").floatValue = 4.2f;
      so.FindProperty("magneticSpeed").floatValue = 11.0f;
      so.FindProperty("rotationSpeed").floatValue = 90.0f;
      so.ApplyModifiedProperties();

      SphereCollider col = root.AddComponent<SphereCollider>();
      col.isTrigger = true;
      col.radius = 2.2f;

      GameObject visualObj = new GameObject("Model");
      visualObj.transform.SetParent(root.transform, false);

      GameObject coinInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel, visualObj.transform);
      coinInstance.transform.localPosition = Vector3.zero;
      coinInstance.transform.localRotation = Quaternion.identity;
      coinInstance.transform.localScale = Vector3.one * scale;

      // TRIỆT TIÊU LỖI: Chuyển toàn bộ Collider trong model con sang Trigger
      foreach (var c in root.GetComponentsInChildren<Collider>(true))
      {
        c.isTrigger = true;
      }
      if (itemLayer >= 0) SetLayerRecursively(root, itemLayer);

      GameObject lightObj = new GameObject("GoldGlow");
      lightObj.transform.SetParent(root.transform, false);
      lightObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);

      Light glow = lightObj.AddComponent<Light>();
      glow.type = LightType.Point;
      glow.range = 2.8f;
      glow.intensity = 3.2f;
      glow.shadows = LightShadows.None;
      glow.color = new Color(1.0f, 0.82f, 0.12f, 1f);

      GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, savePath);
      GameObject.DestroyImmediate(root);
      return saved;
    }

    /// <summary>
    /// Tạo hoặc lấy file ScriptableObject cấu hình Vàng dùng chung (Global Config)
    /// </summary>
    public static GoldDropVisualConfig EnsureGoldDropVisualConfig(GameObject gSingle, GameObject gSmall, GameObject gMed, GameObject gLarge)
    {
      if (!AssetDatabase.IsValidFolder("Assets/Resources"))
      {
        AssetDatabase.CreateFolder("Assets", "Resources");
      }

      GoldDropVisualConfig config = AssetDatabase.LoadAssetAtPath<GoldDropVisualConfig>(GoldConfigPath);
      if (config == null)
      {
        config = ScriptableObject.CreateInstance<GoldDropVisualConfig>();
        AssetDatabase.CreateAsset(config, GoldConfigPath);
      }

      SerializedObject so = new SerializedObject(config);
      so.FindProperty("singleCoinPrefab").objectReferenceValue = gSingle;
      so.FindProperty("smallStackPrefab").objectReferenceValue = gSmall;
      so.FindProperty("mediumStackPrefab").objectReferenceValue = gMed;
      so.FindProperty("largeStackPrefab").objectReferenceValue = gLarge;
      so.FindProperty("smallThreshold").intValue = 10;
      so.FindProperty("mediumThreshold").intValue = 35;
      so.FindProperty("largeThreshold").intValue = 75;
      so.ApplyModifiedProperties();

      EditorUtility.SetDirty(config);
      return config;
    }

    [MenuItem("Infinite Labyrinth/RPG/3. Cấu Hình Tự Động Rơi Đồ (Setup Props & Monsters Loot)", false, 12)]
    public static void SetupPropsAndMonstersLoot()
    {
      GameObject worldPickup = EnsureWorldItemPickupPrefab();

      // 4 Cấp độ Mô Hình Vàng
      GameObject goldSingle = EnsureGoldPickupPrefab("WorldPickup_Gold_Single", "Assets/Prefabs/Dungeon/Interactable/coin.prefab", 1.0f);
      GameObject goldSmall = EnsureGoldPickupPrefab("WorldPickup_Gold_Small", "Assets/Prefabs/Dungeon/Interactable/coin_stack_small.prefab", 0.9f);
      GameObject goldMedium = EnsureGoldPickupPrefab("WorldPickup_Gold_Medium", "Assets/Prefabs/Dungeon/Interactable/coin_stack_medium.prefab", 0.9f);
      GameObject goldLarge = EnsureGoldPickupPrefab("WorldPickup_Gold_Large", "Assets/Prefabs/Dungeon/Interactable/coin_stack_large.prefab", 0.95f);

      // Tạo File Cấu Hình Dùng Chung Toàn Cục (GoldDropVisualConfig.asset)
      GoldDropVisualConfig goldConfig = EnsureGoldDropVisualConfig(goldSingle, goldSmall, goldMedium, goldLarge);

      ItemData gold = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Gold_Coin.asset");
      ItemData potion = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Potion_Health.asset");
      ItemData stone = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Stone_Upgrade.asset");
      ItemData swordWooden = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_Wooden.asset");
      ItemData swordDarkIron = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_DarkIron.asset");
      ItemData swordBone = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_Bone.asset");
      ItemData armor = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Armor_Leather.asset");

      if (gold == null)
      {
        GenerateDefaultItems();
        gold = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Gold_Coin.asset");
        potion = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Potion_Health.asset");
        stone = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Stone_Upgrade.asset");
        swordWooden = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_Wooden.asset");
        swordDarkIron = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_DarkIron.asset");
        swordBone = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_Bone.asset");
        armor = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Armor_Leather.asset");
      }

      // 1. Tạo Prefab Thùng Gỗ đập vỡ chuẩn (Wrapper Pattern)
      CreateBreakableBarrelPrefab("Breakable_Barrel_Small", "Assets/Prefabs/Dungeon/Interactable/barrel_small.prefab",
        worldPickup, goldConfig, gold, potion, stone, 0.45f, 0.85f);

      CreateBreakableBarrelPrefab("Breakable_Barrel_Large", "Assets/Prefabs/Dungeon/Interactable/barrel_large.prefab",
        worldPickup, goldConfig, gold, potion, stone, 0.65f, 1.25f);

      // 2. Tạo Prefab Rương Tương Tác chuẩn
      CreateInteractiveChestPrefab("Interactive_Chest", "Assets/Prefabs/Dungeon/Interactable/chest.prefab",
        worldPickup, goldConfig, gold, potion, stone, swordDarkIron, armor, isGoldChest: false);

      CreateInteractiveChestPrefab("Interactive_Chest_Gold", "Assets/Prefabs/Dungeon/Interactable/chest_gold.prefab",
        worldPickup, goldConfig, gold, potion, stone, swordBone, armor, isGoldChest: true);

      // 3. Cấu hình Quái vật
      SetupMonsterLoot("Assets/Characters/Monsters/SkeletonWarrior.prefab", worldPickup, goldConfig, gold, potion, stone, swordDarkIron, 0.15f);
      SetupMonsterLoot("Assets/Characters/Monsters/SkeletonMinion.prefab", worldPickup, goldConfig, gold, potion, stone, swordWooden, 0.08f);
      SetupMonsterLoot("Assets/Characters/Monsters/SkeletonRogue.prefab", worldPickup, goldConfig, gold, potion, stone, armor, 0.12f);
      SetupMonsterLoot("Assets/Characters/Monsters/SkeletonMage.prefab", worldPickup, goldConfig, gold, potion, stone, swordBone, 0.18f);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      EditorUtility.DisplayDialog("Infinite Labyrinth", 
        "Đã tạo và gắn thành công:\n1. Bảng PickupPromptUI (Phím [F] nổi khi lại gần item)\n2. Bảng PickupNotificationFeed (Thông báo trượt mép trái màn hình)!\n\nBạn có thể Play game để trải nghiệm ngay!", "OK");
      Debug.Log("[ItemDatabaseGenerator] Hoàn tất thiết lập GoldDropVisualConfig dùng chung toàn cục.");
    }

    private static void CreateBreakableBarrelPrefab(string prefabName, string sourceModelPath,
      GameObject worldPickup, GoldDropVisualConfig goldConfig,
      ItemData gold, ItemData potion, ItemData stone, float colRadius, float colHeight)
    {
      string savePath = $"{PrefabFolder}/{prefabName}.prefab";
      GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
      if (sourceModel == null) return;

      GameObject root = new GameObject(prefabName);
      root.layer = DamageableLayer;

      CapsuleCollider col = root.AddComponent<CapsuleCollider>();
      col.radius = colRadius;
      col.height = colHeight;
      col.center = new Vector3(0f, colHeight * 0.5f, 0f);

      root.AddComponent<BreakableProp>();

      var dropper = root.AddComponent<LootDropper>();
      SerializedObject so = new SerializedObject(dropper);
      so.FindProperty("worldPickupPrefab").objectReferenceValue = worldPickup;
      so.FindProperty("goldVisualConfig").objectReferenceValue = goldConfig;

      so.FindProperty("goldItemData").objectReferenceValue = gold;
      so.FindProperty("goldDropChance").floatValue = 0.85f;
      so.FindProperty("minGold").intValue = 4;
      so.FindProperty("maxGold").intValue = 20;

      SerializedProperty table = so.FindProperty("lootTable");
      table.ClearArray();
      if (potion != null) AddLootEntry(table, potion, 0.35f, 1, 1);
      if (stone != null) AddLootEntry(table, stone, 0.25f, 1, 2);
      so.ApplyModifiedProperties();

      GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel, root.transform);
      visual.name = "Model";
      visual.transform.localPosition = Vector3.zero;
      visual.transform.localRotation = Quaternion.identity;

      SetLayerRecursively(root, DamageableLayer);
      PrefabUtility.SaveAsPrefabAsset(root, savePath);
      GameObject.DestroyImmediate(root);
    }

    private static void CreateInteractiveChestPrefab(string prefabName, string sourceModelPath,
      GameObject worldPickup, GoldDropVisualConfig goldConfig,
      ItemData gold, ItemData potion, ItemData stone, ItemData weapon, ItemData armor, bool isGoldChest)
    {
      string savePath = $"{PrefabFolder}/{prefabName}.prefab";
      GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
      if (sourceModel == null) return;

      GameObject root = new GameObject(prefabName);
      root.layer = 0;

      BoxCollider col = root.AddComponent<BoxCollider>();
      col.size = new Vector3(1.2f, 0.9f, 0.8f);
      col.center = new Vector3(0f, 0.45f, 0f);

      root.AddComponent<ChestInteractable>();

      var dropper = root.AddComponent<LootDropper>();
      SerializedObject so = new SerializedObject(dropper);
      so.FindProperty("worldPickupPrefab").objectReferenceValue = worldPickup;
      so.FindProperty("goldVisualConfig").objectReferenceValue = goldConfig;

      so.FindProperty("goldItemData").objectReferenceValue = gold;
      so.FindProperty("goldDropChance").floatValue = 1.0f;
      so.FindProperty("minGold").intValue = isGoldChest ? 50 : 25;
      so.FindProperty("maxGold").intValue = isGoldChest ? 120 : 60;

      SerializedProperty table = so.FindProperty("lootTable");
      table.ClearArray();
      if (potion != null) AddLootEntry(table, potion, 0.75f, 1, 2);
      if (stone != null) AddLootEntry(table, stone, isGoldChest ? 0.8f : 0.4f, 1, 3);
      if (weapon != null) AddLootEntry(table, weapon, isGoldChest ? 0.65f : 0.35f, 1, 1);
      if (armor != null) AddLootEntry(table, armor, isGoldChest ? 0.5f : 0.25f, 1, 1);
      so.ApplyModifiedProperties();

      GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel, root.transform);
      visual.name = "Model";
      visual.transform.localPosition = Vector3.zero;
      visual.transform.localRotation = Quaternion.identity;

      PrefabUtility.SaveAsPrefabAsset(root, savePath);
      GameObject.DestroyImmediate(root);
    }

    private static void SetupMonsterLoot(string path, GameObject worldPickup, GoldDropVisualConfig goldConfig,
      ItemData gold, ItemData potion, ItemData stone, ItemData gear, float gearChance)
    {
      GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (monsterPrefab == null) return;

      using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
      {
        GameObject root = scope.prefabContentsRoot;

        var dropper = root.GetComponent<LootDropper>() ?? root.AddComponent<LootDropper>();
        SerializedObject so = new SerializedObject(dropper);
        so.FindProperty("worldPickupPrefab").objectReferenceValue = worldPickup;
        so.FindProperty("goldVisualConfig").objectReferenceValue = goldConfig;

        so.FindProperty("goldItemData").objectReferenceValue = gold;
        so.FindProperty("goldDropChance").floatValue = 0.75f;
        so.FindProperty("minGold").intValue = 5;
        so.FindProperty("maxGold").intValue = 25;

        SerializedProperty table = so.FindProperty("lootTable");
        table.ClearArray();
        if (potion != null) AddLootEntry(table, potion, 0.30f, 1, 1);
        if (stone != null) AddLootEntry(table, stone, 0.25f, 1, 2);
        if (gear != null) AddLootEntry(table, gear, gearChance, 1, 1);

        so.ApplyModifiedProperties();
      }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
      obj.layer = layer;
      foreach (Transform child in obj.transform)
      {
        SetLayerRecursively(child.gameObject, layer);
      }
    }

    private static void AddLootEntry(SerializedProperty list, ItemData item, float chance, int min, int max)
    {
      int index = list.arraySize;
      list.InsertArrayElementAtIndex(index);
      SerializedProperty elem = list.GetArrayElementAtIndex(index);

      elem.FindPropertyRelative("item").objectReferenceValue = item;
      elem.FindPropertyRelative("dropChance").floatValue = chance;
      elem.FindPropertyRelative("minQuantity").intValue = min;
      elem.FindPropertyRelative("maxQuantity").intValue = max;
    }

    private static void CreateOrUpdateItem(string assetName, string id, string itemName, string desc,
      Sprite icon, ItemType type, ItemRarity rarity, int maxStack, int goldValue,
      float bonusAtk, float bonusDef, float bonusHp, float bonusStamina, float heal,
      GameObject worldPrefab, GameObject visualPrefab)
    {
      string assetPath = $"{TargetFolder}/{assetName}.asset";
      ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

      if (item == null)
      {
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, assetPath);
      }

      SerializedObject so = new SerializedObject(item);
      so.FindProperty("id").stringValue = id;
      so.FindProperty("itemName").stringValue = itemName;
      so.FindProperty("description").stringValue = desc;
      so.FindProperty("icon").objectReferenceValue = icon;
      so.FindProperty("itemType").enumValueIndex = (int)type;
      so.FindProperty("rarity").enumValueIndex = (int)rarity;
      so.FindProperty("maxStack").intValue = maxStack;
      so.FindProperty("goldValue").intValue = goldValue;
      so.FindProperty("bonusAttack").floatValue = bonusAtk;
      so.FindProperty("bonusDefense").floatValue = bonusDef;
      so.FindProperty("bonusHealth").floatValue = bonusHp;
      so.FindProperty("bonusStamina").floatValue = bonusStamina;
      so.FindProperty("healAmount").floatValue = heal;
      so.FindProperty("worldModelPrefab").objectReferenceValue = worldPrefab;
      so.FindProperty("weaponVisualPrefab").objectReferenceValue = visualPrefab;

      so.ApplyModifiedProperties();
      EditorUtility.SetDirty(item);
    }
    private const string NotiItemPrefabPath = "Assets/Prefabs/RPG/PickupNotificationItem.prefab";

    [MenuItem("Infinite Labyrinth/RPG/4. Tự Động Tạo Pickup UI & Notification Feed Trên Canvas", false, 13)]
    public static void SetupPickupUIAndNotificationFeed()
    {
      // 1. Tạo Prefab cho từng dòng thông báo
      GameObject notiPrefab = EnsurePickupNotificationItemPrefab();

      // 2. Tìm Canvas trong Scene
      Canvas canvas = Object.FindAnyObjectByType<Canvas>();
      if (canvas == null)
      {
        GameObject canvasObj = new GameObject("GameCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
      }

      // 3. Tạo hoặc lấy PickupPromptUI trên Canvas
      Transform promptTr = canvas.transform.Find("PickupPromptUI");
      GameObject promptObj;
      if (promptTr == null)
      {
        promptObj = CreatePickupPromptUIObject(canvas.transform);
      }
      else
      {
        promptObj = promptTr.gameObject;
      }

      // 4. Tạo hoặc lấy PickupNotificationFeed trên Canvas
      Transform feedTr = canvas.transform.Find("PickupNotificationFeed");
      GameObject feedObj;
      if (feedTr == null)
      {
        feedObj = CreateNotificationFeedObject(canvas.transform, notiPrefab);
      }
      else
      {
        feedObj = feedTr.gameObject;
        var feedComp = feedObj.GetComponent<InfiniteLabyrinth.UI.PickupNotificationFeed>();
        if (feedComp != null)
        {
          SerializedObject so = new SerializedObject(feedComp);
          so.FindProperty("notificationPrefab").objectReferenceValue = notiPrefab;
          so.ApplyModifiedProperties();
        }
      }

      EditorUtility.SetDirty(canvas.gameObject);
      EditorUtility.DisplayDialog("Infinite Labyrinth", 
        "Đã tạo và gắn thành công:\n1. Bảng PickupPromptUI (Phím [F] nổi khi lại gần item)\n2. Bảng PickupNotificationFeed (Thông báo trượt mép trái màn hình)!\n\nBạn có thể Play game để trải nghiệm ngay!", "OK");
      Debug.Log("[ItemDatabaseGenerator] Hoàn tất cài đặt PickupPromptUI và PickupNotificationFeed lên Canvas.");
    }

    public static GameObject EnsurePickupNotificationItemPrefab()
    {
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
      if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "RPG");

      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NotiItemPrefabPath);
      if (existing != null) return existing;

      Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/GameUI/Panel_Border_Thin.png");
      TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

      GameObject root = new GameObject("PickupNotificationItem");
      RectTransform rootRect = root.AddComponent<RectTransform>();
      rootRect.sizeDelta = new Vector2(260f, 44f);
      var cg = root.AddComponent<CanvasGroup>();

      // Nền
      var bgImg = root.AddComponent<UnityEngine.UI.Image>();
      bgImg.sprite = bgSprite;
      bgImg.type = UnityEngine.UI.Image.Type.Sliced;
      bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);

      // Thanh màu viền độ hiếm bên trái
      GameObject rarityBar = new GameObject("RarityBar");
      rarityBar.transform.SetParent(root.transform, false);
      RectTransform barRect = rarityBar.AddComponent<RectTransform>();
      barRect.anchorMin = new Vector2(0f, 0f);
      barRect.anchorMax = new Vector2(0f, 1f);
      barRect.pivot = new Vector2(0f, 0.5f);
      barRect.anchoredPosition = new Vector2(2f, 0f);
      barRect.sizeDelta = new Vector2(4f, -4f);
      var barImg = rarityBar.AddComponent<UnityEngine.UI.Image>();
      barImg.color = Color.white;

      // Icon
      GameObject iconObj = new GameObject("Icon");
      iconObj.transform.SetParent(root.transform, false);
      RectTransform iconRect = iconObj.AddComponent<RectTransform>();
      iconRect.anchorMin = new Vector2(0f, 0.5f);
      iconRect.anchorMax = new Vector2(0f, 0.5f);
      iconRect.pivot = new Vector2(0.5f, 0.5f);
      iconRect.anchoredPosition = new Vector2(26f, 0f);
      iconRect.sizeDelta = new Vector2(32f, 32f);
      var iconImg = iconObj.AddComponent<UnityEngine.UI.Image>();

      // Tên item
      GameObject nameObj = new GameObject("ItemName");
      nameObj.transform.SetParent(root.transform, false);
      RectTransform nameRect = nameObj.AddComponent<RectTransform>();
      nameRect.anchorMin = new Vector2(0f, 0f);
      nameRect.anchorMax = new Vector2(1f, 1f);
      nameRect.offsetMin = new Vector2(48f, 0f);
      nameRect.offsetMax = new Vector2(-65f, 0f);
      var nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
      if (font != null) nameText.font = font;
      nameText.fontSize = 15f;
      nameText.fontStyle = TMPro.FontStyles.Bold;
      nameText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
      nameText.text = "Tên Vật Phẩm";

      // Số lượng
      GameObject countObj = new GameObject("Count");
      countObj.transform.SetParent(root.transform, false);
      RectTransform countRect = countObj.AddComponent<RectTransform>();
      countRect.anchorMin = new Vector2(1f, 0f);
      countRect.anchorMax = new Vector2(1f, 1f);
      countRect.pivot = new Vector2(1f, 0.5f);
      countRect.anchoredPosition = new Vector2(-10f, 0f);
      countRect.sizeDelta = new Vector2(60f, 0f);
      var countText = countObj.AddComponent<TMPro.TextMeshProUGUI>();
      if (font != null) countText.font = font;
      countText.fontSize = 14f;
      countText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
      countText.text = "+1";

      var notiComp = root.AddComponent<InfiniteLabyrinth.UI.PickupNotificationItem>();
      SerializedObject so = new SerializedObject(notiComp);
      so.FindProperty("itemIcon").objectReferenceValue = iconImg;
      so.FindProperty("rarityBorder").objectReferenceValue = barImg;
      so.FindProperty("itemNameText").objectReferenceValue = nameText;
      so.FindProperty("quantityText").objectReferenceValue = countText;
      so.FindProperty("canvasGroup").objectReferenceValue = cg;
      so.FindProperty("rectTransform").objectReferenceValue = rootRect;
      so.ApplyModifiedProperties();

      GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, NotiItemPrefabPath);
      GameObject.DestroyImmediate(root);
      return saved;
    }

    private static GameObject CreatePickupPromptUIObject(Transform canvasParent)
    {
      Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/GameUI/Panel_Border_Thin.png");
      Sprite slotBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/GameUI/Slot_Border.png");
      TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

      GameObject root = new GameObject("PickupPromptUI");
      root.transform.SetParent(canvasParent, false);
      RectTransform rootRect = root.AddComponent<RectTransform>();
      rootRect.anchorMin = new Vector2(0.5f, 0.22f);
      rootRect.anchorMax = new Vector2(0.5f, 0.22f);
      rootRect.pivot = new Vector2(0.5f, 0.5f);
      rootRect.anchoredPosition = Vector2.zero;
      rootRect.sizeDelta = new Vector2(270f, 54f);

      var bg = root.AddComponent<UnityEngine.UI.Image>();
      bg.sprite = panelSprite;
      bg.type = UnityEngine.UI.Image.Type.Sliced;
      bg.color = new Color(0.06f, 0.06f, 0.06f, 0.88f);
      var cg = root.AddComponent<CanvasGroup>();

      // Icon
      GameObject iconObj = new GameObject("Icon");
      iconObj.transform.SetParent(root.transform, false);
      RectTransform iconRect = iconObj.AddComponent<RectTransform>();
      iconRect.anchorMin = new Vector2(0f, 0.5f);
      iconRect.anchorMax = new Vector2(0f, 0.5f);
      iconRect.anchoredPosition = new Vector2(28f, 0f);
      iconRect.sizeDelta = new Vector2(38f, 38f);
      var iconImg = iconObj.AddComponent<UnityEngine.UI.Image>();

      // Viền Rarity
      GameObject borderObj = new GameObject("Border");
      borderObj.transform.SetParent(iconObj.transform, false);
      RectTransform borderRect = borderObj.AddComponent<RectTransform>();
      borderRect.anchorMin = Vector2.zero;
      borderRect.anchorMax = Vector2.one;
      borderRect.sizeDelta = Vector2.zero;
      var borderImg = borderObj.AddComponent<UnityEngine.UI.Image>();
      borderImg.sprite = slotBorder;
      borderImg.type = UnityEngine.UI.Image.Type.Sliced;

      // Text Tên Item
      GameObject nameObj = new GameObject("ItemName");
      nameObj.transform.SetParent(root.transform, false);
      RectTransform nameRect = nameObj.AddComponent<RectTransform>();
      nameRect.anchorMin = new Vector2(0f, 0.5f);
      nameRect.anchorMax = new Vector2(1f, 1f);
      nameRect.offsetMin = new Vector2(56f, 0f);
      nameRect.offsetMax = new Vector2(-10f, -4f);
      var nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
      if (font != null) nameText.font = font;
      nameText.fontSize = 15f;
      nameText.fontStyle = TMPro.FontStyles.Bold;
      nameText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
      nameText.text = "Tên Vật Phẩm";

      // Text Phím F
      GameObject keyObj = new GameObject("KeyPrompt");
      keyObj.transform.SetParent(root.transform, false);
      RectTransform keyRect = keyObj.AddComponent<RectTransform>();
      keyRect.anchorMin = new Vector2(0f, 0f);
      keyRect.anchorMax = new Vector2(1f, 0.5f);
      keyRect.offsetMin = new Vector2(56f, 4f);
      keyRect.offsetMax = new Vector2(-10f, 0f);
      var keyText = keyObj.AddComponent<TMPro.TextMeshProUGUI>();
      if (font != null) keyText.font = font;
      keyText.fontSize = 13f;
      keyText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
      keyText.text = "[<color=#FFCC00>F</color>] Nhặt";

      var promptComp = root.AddComponent<InfiniteLabyrinth.UI.PickupPromptUI>();
      SerializedObject so = new SerializedObject(promptComp);
      so.FindProperty("promptRoot").objectReferenceValue = root;
      so.FindProperty("itemIcon").objectReferenceValue = iconImg;
      so.FindProperty("rarityBorder").objectReferenceValue = borderImg;
      so.FindProperty("itemNameText").objectReferenceValue = nameText;
      so.FindProperty("promptKeyText").objectReferenceValue = keyText;
      so.FindProperty("canvasGroup").objectReferenceValue = cg;
      so.ApplyModifiedProperties();

      return root;
    }

    private static GameObject CreateNotificationFeedObject(Transform canvasParent, GameObject notiPrefab)
    {
      GameObject root = new GameObject("PickupNotificationFeed");
      root.transform.SetParent(canvasParent, false);
      RectTransform rootRect = root.AddComponent<RectTransform>();
      rootRect.anchorMin = new Vector2(0f, 0.45f);
      rootRect.anchorMax = new Vector2(0f, 0.45f);
      rootRect.pivot = new Vector2(0f, 0f);
      rootRect.anchoredPosition = new Vector2(30f, -60f);
      rootRect.sizeDelta = new Vector2(280f, 260f);

      var vlg = root.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
      vlg.spacing = 8f;
      vlg.childAlignment = TextAnchor.LowerLeft;
      vlg.childControlWidth = true;
      vlg.childControlHeight = false;
      vlg.childForceExpandWidth = false;
      vlg.childForceExpandHeight = false;

      var feedComp = root.AddComponent<InfiniteLabyrinth.UI.PickupNotificationFeed>();
      SerializedObject so = new SerializedObject(feedComp);
      so.FindProperty("notificationPrefab").objectReferenceValue = notiPrefab;
      so.FindProperty("maxVisibleItems").intValue = 5;
      so.FindProperty("itemDisplayDuration").floatValue = 2.5f;
      so.ApplyModifiedProperties();

      return root;
    }

  }
}
#endif
