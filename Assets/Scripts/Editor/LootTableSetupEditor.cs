#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Tự động thiết lập ma trận rơi đồ toàn diện (Drop Tables) cho 4 Quái Skeleton, 3 Loại Thùng Gỗ và 3 Cấp Độ Rương
  /// theo đúng bản thiết kế chuẩn trong ITEM_SYSTEM_DESIGN.md.
  /// </summary>
  public static class LootTableSetupEditor
  {
    private const string ItemsFolder = "Assets/ScriptableObjects/Items";
    private const string WorldPickupPrefabPath = "Assets/Prefabs/RPG/WorldItemPickup.prefab";
    private const string GoldConfigPath = "Assets/Resources/GoldDropVisualConfig.asset";

    // Prefabs Quái Skeleton
    private const string MinionPrefabPath = "Assets/Characters/Monsters/SkeletonMinion.prefab";
    private const string WarriorPrefabPath = "Assets/Characters/Monsters/SkeletonWarrior.prefab";
    private const string RoguePrefabPath = "Assets/Characters/Monsters/SkeletonRogue.prefab";
    private const string MagePrefabPath = "Assets/Characters/Monsters/SkeletonMage.prefab";

    // Prefabs Thùng Gỗ
    private const string BarrelSmallPrefabPath = "Assets/Prefabs/RPG/Breakable_Barrel_Small.prefab";
    private const string BarrelMediumPrefabPath = "Assets/Prefabs/RPG/Breakable_Barrel_Medium.prefab";
    private const string BarrelLargePrefabPath = "Assets/Prefabs/RPG/Breakable_Barrel_Large.prefab";

    // Prefabs Rương Kho Báu
    private const string ChestNormalPrefabPath = "Assets/Prefabs/RPG/Interactive_Chest.prefab";
    private const string ChestRarePrefabPath = "Assets/Prefabs/RPG/Interactive_Chest_Rare.prefab";
    private const string ChestBossPrefabPath = "Assets/Prefabs/RPG/Interactive_Chest_Boss.prefab";

    [MenuItem("Infinite Labyrinth/RPG/2. Cấu Hình Ma Trận Rơi Đồ (Full Loot Drop Tables)", false, 20)]
    public static void ConfigureAllLootTablesManual()
    {
      ConfigureAllLootTables(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnceOnLoad()
    {
      EditorApplication.delayCall += () =>
      {
        if (File.Exists(MinionPrefabPath))
        {
          string minionText = File.ReadAllText(MinionPrefabPath);
          // b09c634f9d2eec04692a983380c66236 là GUID của Item_Mat_Bone_Fragment.asset
          if (!minionText.Contains("b09c634f9d2eec04692a983380c66236"))
          {
            Debug.Log("[LootTableSetupEditor] Phát hiện cấu hình rơi đồ cũ. Đang tự động cập nhật Ma Trận Rơi Đồ chuẩn...");
            ConfigureAllLootTables(false);
          }
        }
      };
    }

    public static void ConfigureAllLootTables(bool showDialog = true)
    {
      GameObject worldPickup = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPickupPrefabPath);
      GoldDropVisualConfig goldConfig = AssetDatabase.LoadAssetAtPath<GoldDropVisualConfig>(GoldConfigPath);
      ItemData goldItem = LoadItem("Item_Gold_Coin.asset");

      if (worldPickup == null || goldItem == null)
      {
        Debug.LogError("[LootTableSetupEditor] Thiếu WorldItemPickup hoặc Item_Gold_Coin. Hãy chạy tạo database vật phẩm trước!");
        return;
      }

      int updatedCount = 0;

      // ==========================================
      // 1. CẤU HÌNH 3 THÙNG GỖ ĐẬP ĐƯỢC
      // ==========================================
      // A. Thùng Nhỏ (Breakable_Barrel_Small)
      // 70% Vàng (2-6), 25% Bình Máu Nhỏ
      updatedCount += ConfigurePrefab(BarrelSmallPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 0.70f, minGold: 2, maxGold: 6,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Potion_Health.asset"), 0.25f, 1, 1)
        });

      // B. Thùng Vừa (Breakable_Barrel_Medium)
      // 85% Vàng (8-18), 35% Bình Máu Nhỏ HOẶC Tiên Dược Thể Lực, 15% Đá Cường Hóa
      updatedCount += ConfigurePrefab(BarrelMediumPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 0.85f, minGold: 8, maxGold: 18,
        entries: new List<LootEntry>
        {
          CreatePoolEntry(new List<ItemData> { LoadItem("Item_Potion_Health.asset"), LoadItem("Item_Potion_Stamina_Elixir.asset") }, 0.35f, 1, 1),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 0.15f, 1, 1)
        });

      // C. Thùng Lớn (Breakable_Barrel_Large)
      // 100% Vàng (18-35), 40% Bình Máu Lớn, 25% 1-2 Đá Cường Hóa, 5% Thiết Kiếm Rỉ Sét
      updatedCount += ConfigurePrefab(BarrelLargePrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 18, maxGold: 35,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Potion_Health_Large.asset"), 0.40f, 1, 1),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 0.25f, 1, 2),
          CreateEntry(LoadItem("Item_Sword_Rusty_Iron.asset"), 0.05f, 1, 1)
        });

      // ==========================================
      // 2. CẤU HÌNH 4 QUÁI SKELETON
      // ==========================================
      // A. Skeleton Minion
      // 100% Vàng (3-8), 30% Mảnh Xương Quái
      updatedCount += ConfigurePrefab(MinionPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 3, maxGold: 8,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Mat_Bone_Fragment.asset"), 0.30f, 1, 1)
        });

      // B. Skeleton Warrior
      // 100% Vàng (8-16), 45% 1-2 Mảnh Xương, 20% Huy Hiệu Chiến Binh, 10% Đá Cường Hóa
      updatedCount += ConfigurePrefab(WarriorPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 8, maxGold: 16,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Mat_Bone_Fragment.asset"), 0.45f, 1, 2),
          CreateEntry(LoadItem("Item_Mat_Warrior_Insignia.asset"), 0.20f, 1, 1),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 0.10f, 1, 1)
        });

      // C. Skeleton Rogue
      // 100% Vàng (10-20), 40% 1-2 Mũi Tên Đen, 20% Bình Máu Nhỏ, 12% Tiên Dược Thể Lực
      updatedCount += ConfigurePrefab(RoguePrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 10, maxGold: 20,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Mat_Dark_Arrowhead.asset"), 0.40f, 1, 2),
          CreateEntry(LoadItem("Item_Potion_Health.asset"), 0.20f, 1, 1),
          CreateEntry(LoadItem("Item_Potion_Stamina_Elixir.asset"), 0.12f, 1, 1)
        });

      // D. Skeleton Mage
      // 100% Vàng (16-32), 50% 1-2 Bụi Tinh Hoa Ma Pháp, 25% Đá Cường Hóa, 8% Giấy Da Cổ Ma Thuật
      updatedCount += ConfigurePrefab(MagePrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 16, maxGold: 32,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Mat_Arcane_Dust.asset"), 0.50f, 1, 2),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 0.25f, 1, 1),
          CreateEntry(LoadItem("Item_Mat_Spell_Parchment.asset"), 0.08f, 1, 1)
        });

      // ==========================================
      // 3. CẤU HÌNH 3 CẤP ĐỘ RƯƠNG KHO BÁU
      // ==========================================
      // A. Rương Thường (Interactive_Chest)
      // 100% Vàng (30-75), 100% 1-2 Bình Máu Nhỏ, 40% 1 Đá Cường Hóa, 25% Trang bị Uncommon ngẫu nhiên
      updatedCount += ConfigurePrefab(ChestNormalPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 30, maxGold: 75,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Potion_Health.asset"), 1.0f, 1, 2),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 0.40f, 1, 1),
          CreatePoolEntry(new List<ItemData>
          {
            LoadItem("Item_Sword_Novice.asset"),
            LoadItem("Item_Sword_Broad_Iron.asset"),
            LoadItem("Item_Armor_Leather.asset"),
            LoadItem("Item_Armor_Cloth_Novice.asset")
          }, 0.25f, 1, 1)
        });

      // B. Rương Hiếm (Interactive_Chest_Rare)
      // 100% Vàng (80-180), 100% 2-3 Bình Máu Lớn, 100% 2-3 Đá Cường Hóa, 50% Vũ khí Rare ngẫu nhiên, 30% Thiết Giáp Sắt
      updatedCount += ConfigurePrefab(ChestRarePrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 80, maxGold: 180,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Potion_Health_Large.asset"), 1.0f, 2, 3),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 1.0f, 2, 3),
          CreatePoolEntry(new List<ItemData>
          {
            LoadItem("Item_Sword_Punk.asset"),
            LoadItem("Item_Sword_Bone.asset"),
            LoadItem("Item_Sword_Dead.asset"),
            LoadItem("Item_Sword_Equilibrium.asset")
          }, 0.50f, 1, 1),
          CreateEntry(LoadItem("Item_Armor_Iron_Cuirass.asset"), 0.30f, 1, 1)
        });

      // C. Rương Boss Bục Đá 12m (Interactive_Chest_Boss)
      // 100% Vàng (180-350), 100% Trái Tim Băng Ice Core, 100% 2-3 Mảnh Cổ Tự Băng, 100% 3-5 Đá Cường Hóa,
      // 100% 1 Tinh Thể Hoàn Mỹ, 100% 3 Bình Máu Lớn, 100% 2 Mật Năng Lượng Spirit, 65% Trang bị Epic/Legendary
      updatedCount += ConfigurePrefab(ChestBossPrefabPath, worldPickup, goldConfig, goldItem,
        goldChance: 1.0f, minGold: 180, maxGold: 350,
        entries: new List<LootEntry>
        {
          CreateEntry(LoadItem("Item_Mat_Ice_Core.asset"), 1.0f, 1, 1),
          CreateEntry(LoadItem("Item_Mat_Ice_Rune.asset"), 1.0f, 2, 3),
          CreateEntry(LoadItem("Item_Stone_Upgrade.asset"), 1.0f, 3, 5),
          CreateEntry(LoadItem("Item_Mat_Flawless_Crystal.asset"), 1.0f, 1, 1),
          CreateEntry(LoadItem("Item_Potion_Health_Large.asset"), 1.0f, 3, 3),
          CreateEntry(LoadItem("Item_Potion_Spirit_Nectar.asset"), 1.0f, 2, 2),
          CreatePoolEntry(new List<ItemData>
          {
            LoadItem("Item_Sword_Demonic.asset"),
            LoadItem("Item_Sword_Titan.asset"),
            LoadItem("Item_Sword_Artifact.asset"),
            LoadItem("Item_Sword_Ice.asset"),
            LoadItem("Item_Armor_Frost_Guardian.asset")
          }, 0.65f, 1, 1)
        });

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"[LootTableSetupEditor] Đã cấu hình và lưu thành công Ma Trận Rơi Đồ cho {updatedCount}/10 Prefabs (Quái vật, Thùng gỗ, Rương báu)!");
      if (showDialog)
      {
        EditorUtility.DisplayDialog("Infinite Labyrinth - RPG",
          $"Đã cấu hình thành công Ma Trận Rơi Đồ cho toàn bộ {updatedCount} Prefabs:\n" +
          "- 3 Thùng gỗ (Small, Medium, Large)\n" +
          "- 4 Quái Skeleton (Minion, Warrior, Rogue, Mage)\n" +
          "- 3 Cấp độ Rương (Thường, Hiếm, Boss 12m)\n\nTất cả dữ liệu đã được lưu trực tiếp vào Prefabs!", "OK");
      }
    }

    private static int ConfigurePrefab(string prefabPath, GameObject worldPickup, GoldDropVisualConfig goldConfig,
      ItemData goldItem, float goldChance, int minGold, int maxGold, List<LootEntry> entries)
    {
      if (!File.Exists(prefabPath))
      {
        Debug.LogWarning($"[LootTableSetupEditor] Không tìm thấy Prefab tại: {prefabPath}");
        return 0;
      }

      GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
      if (contents == null) return 0;

      try
      {
        LootDropper dropper = contents.GetComponent<LootDropper>();
        if (dropper == null)
        {
          dropper = contents.AddComponent<LootDropper>();
        }

        dropper.SetPrefabs(worldPickup, goldConfig);
        dropper.ConfigureGold(goldItem, goldChance, minGold, maxGold);
        dropper.SetLootTable(entries);

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        return 1;
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"[LootTableSetupEditor] Lỗi khi cấu hình Prefab {prefabPath}: {ex.Message}");
        return 0;
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(contents);
      }
    }

    private static LootEntry CreateEntry(ItemData item, float chance, int minQty, int maxQty)
    {
      return new LootEntry
      {
        item = item,
        dropChance = chance,
        minQuantity = minQty,
        maxQuantity = maxQty,
        randomItemPool = new List<ItemData>()
      };
    }

    private static LootEntry CreatePoolEntry(List<ItemData> pool, float chance, int minQty, int maxQty)
    {
      var entry = new LootEntry
      {
        item = pool != null && pool.Count > 0 ? pool[0] : null,
        dropChance = chance,
        minQuantity = minQty,
        maxQuantity = maxQty,
        randomItemPool = pool != null ? new List<ItemData>(pool) : new List<ItemData>()
      };
      return entry;
    }

    private static ItemData LoadItem(string fileName)
    {
      string fullPath = $"{ItemsFolder}/{fileName}";
      var item = AssetDatabase.LoadAssetAtPath<ItemData>(fullPath);
      if (item == null)
      {
        Debug.LogWarning($"[LootTableSetupEditor] Không tìm thấy ItemData tại: {fullPath}");
      }
      return item;
    }
  }
}
#endif
