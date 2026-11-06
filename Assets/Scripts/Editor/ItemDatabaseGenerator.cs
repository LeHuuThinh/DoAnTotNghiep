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
    private const string SwordsFolder = "Assets/Characters/Player/Swords";
    private const string WorldPickupPrefabPath = "Assets/Prefabs/RPG/WorldItemPickup.prefab";
    private const string GoldConfigPath = "Assets/Resources/GoldDropVisualConfig.asset";
    private const int DamageableLayer = 8; // Layer "Damageable" nhận sát thương từ kiếm

    [MenuItem("Infinite Labyrinth/RPG/1. Khởi Tạo Toàn Bộ Dữ Liệu Vật Phẩm (Full 49 Items Database)", false, 10)]
    public static void GenerateDefaultItems()
    {
      GenerateItems(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnceOnLoad()
    {
      EditorApplication.delayCall += () =>
      {
        EnsurePlayerWeaponVisualSync();
        string rustyAssetPath = $"{TargetFolder}/Item_Sword_Rusty_Iron.asset";
        if (!File.Exists($"{TargetFolder}/Item_Sword_Excalibur.asset") ||
            (File.Exists(rustyAssetPath) && !File.ReadAllText(rustyAssetPath).Contains("bffaf84d3633d0b40b74c2a435d0ec01")))
        {
          Debug.Log("[ItemDatabaseGenerator] Phát hiện cập nhật đường dẫn kiếm mới tại Player/Swords. Đang tự động cập nhật lại vật phẩm...");
          GenerateItems(false);
        }
      };
    }

    public static void GenerateItems(bool showDialog = true)
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

      // ==========================================
      // 1. TIỀN TỆ & CHÌA KHÓA (CURRENCY & KEYS)
      // ==========================================
      Sprite goldIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI RPG/PlayerHUD 2/Exp_Circle.png");
      CreateOrUpdateItem("Item_Gold_Coin", "currency_gold", "Gold Coin",
        "The standard currency circulating across Haven and beyond. Used for purchasing goods at the Village Shop and forging weapons at the Blacksmith.",
        goldIcon, ItemType.Currency, ItemRarity.Common, maxStack: 9999, goldValue: 1,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Key_Dungeon", "dungeon_key", "Dungeon Boss Key",
        "An ancient golden key resonating with sacred magic. Required to unlock the Grand Gate leading into the Boss Arena.",
        LoadItemIcon("DungeonKey.png"), ItemType.Key, ItemRarity.Rare, maxStack: 1, goldValue: 0,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // ==========================================
      // 2. VẬT PHẨM TIÊU HAO (CONSUMABLES - 6 ITEMS)
      // ==========================================
      CreateOrUpdateItem("Item_Potion_Health", "potion_health_small", "Small Health Potion",
        "Extracted from luminescent dungeon herbs. Instantly restores 40 Health points.",
        LoadItemIcon("HealthSmall.png"), ItemType.Consumable, ItemRarity.Common, maxStack: 10, goldValue: 25,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 40f,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Potion_Health_Large", "potion_health_large", "Large Health Potion",
        "A potent restorative draught brewed from ancient mystical roots. Instantly restores 100 Health points.",
        LoadItemIcon("HealthLarge.png"), ItemType.Consumable, ItemRarity.Rare, maxStack: 5, goldValue: 65,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 100f,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Potion_Stamina_Elixir", "potion_stamina_elixir", "Stamina Elixir",
        "An invigorating elixir that expands lungs and invigorates breath. Boosts Stamina and accelerates recovery.",
        LoadItemIcon("StaminaElixir.png"), ItemType.Consumable, ItemRarity.Uncommon, maxStack: 5, goldValue: 45,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 20f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Potion_Spirit_Nectar", "potion_spirit_nectar", "Spirit Nectar",
        "Concentrated spirit nectar drawn from subterranean crystals. Replenishes focus to unleash powerful special techniques.",
        LoadItemIcon("SpiritNectar.png"), ItemType.Consumable, ItemRarity.Rare, maxStack: 3, goldValue: 90,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Potion_Ironbark_Brew", "potion_ironbark_brew", "Ironbark Brew",
        "A thick decoction made from thousand-year ironbark. Hardens the skin like steel, granting +15 Defense.",
        LoadItemIcon("IronBarkBrew.png"), ItemType.Consumable, ItemRarity.Uncommon, maxStack: 5, goldValue: 50,
        bonusAtk: 0, bonusDef: 15f, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Potion_Berserker", "potion_berserker", "Berserker Draught",
        "Fierce draught that awakens raw combat instinct, granting +15 Attack during life-or-death battles.",
        LoadItemIcon("BerserkerPotion.png"), ItemType.Consumable, ItemRarity.Rare, maxStack: 3, goldValue: 85,
        bonusAtk: 15f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // ==========================================
      // 3. NGUYÊN LIỆU RÈN & CƯỜNG HÓA (MATERIALS - 13 ITEMS)
      // ==========================================
      CreateOrUpdateItem("Item_Stone_Upgrade", "mat_upgrade_stone", "Enhancement Stone",
        "Ancient ore infused with latent arcane energy. Used at the Forge to enhance weapons from +1 to +5.",
        LoadItemIcon("EnhanceStone.png"), ItemType.Material, ItemRarity.Rare, maxStack: 99, goldValue: 60,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Flawless_Crystal", "mat_upgrade_crystal", "Flawless Crystal",
        "An exceedingly rare pristine crystal. Required to ascend weapons to their ultimate +4 and +5 tiers at the Forge.",
        LoadItemIcon("Crystal.png"), ItemType.Material, ItemRarity.Epic, maxStack: 99, goldValue: 200,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Bone_Fragment", "mat_bone_fragment", "Bone Fragment",
        "Remnants of crushed Skeleton foes. Utilized as foundational crafting material for Bone weapons and armor.",
        LoadItemIcon("BoneFragment.png"), ItemType.Material, ItemRarity.Common, maxStack: 99, goldValue: 12,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Warrior_Insignia", "mat_warrior_insignia", "Warrior Crest",
        "A weathered metallic crest dropped by Skeleton Warriors. Used to forge iron armor or traded for a high price at the Shop.",
        LoadItemIcon("WarriorInsignia.png"), ItemType.Material, ItemRarity.Uncommon, maxStack: 99, goldValue: 28,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Dark_Arrowhead", "mat_dark_arrowhead", "Dark Arrowhead",
        "A poisoned arrowhead recovered from Skeleton Rogues. Imbued with shadowy toxins, ideal for crafting agile equipment.",
        LoadItemIcon("DarkArrow.png"), ItemType.Material, ItemRarity.Uncommon, maxStack: 99, goldValue: 35,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Arcane_Dust", "mat_arcane_dust", "Arcane Dust",
        "Glowing residue gathered from fallen Skeleton Mages. A vital component for imbuing weapons with mystical energy.",
        LoadItemIcon("ArcaneDust.png"), ItemType.Material, ItemRarity.Rare, maxStack: 99, goldValue: 55,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Spell_Parchment", "mat_spell_parchment", "Ancient Scroll",
        "An ancient scroll containing fragments of lost incantations. Used as an auxiliary catalyst for crafting Epic gear.",
        LoadItemIcon("AncientScroll.png"), ItemType.Material, ItemRarity.Epic, maxStack: 20, goldValue: 160,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Ice_Rune", "mat_boss_ice_rune", "Glacial Rune",
        "A glacial rune sealing the frost dominion of the Ice Golem. Essential for forging the Titan Heavy Cleaver and frost gear.",
        LoadItemIcon("IceRune.png"), ItemType.Material, ItemRarity.Epic, maxStack: 20, goldValue: 180,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Ice_Core", "mat_boss_ice_core", "Eternal Frost Core",
        "The perpetual frozen heart extracted from the Ice Golem. The primary component for forging the Frostfang Glacial Sword and Frostborne Platemail.",
        LoadItemIcon("IceCore.png"), ItemType.Material, ItemRarity.Legendary, maxStack: 10, goldValue: 450,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Fire_Crest", "mat_boss_fire_crest", "Blazing Crest",
        "A blazing crest radiating relentless heat, claimed from Red Ignis. Used to enhance fire attributes and craft flame armor.",
        LoadItemIcon("BlazingCrest.png"), ItemType.Material, ItemRarity.Epic, maxStack: 20, goldValue: 220,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Fire_Core", "mat_boss_fire_core", "True Flame Core",
        "A primal flame core burning at scorching temperatures, taken from Red Ignis. Essential for forging the legendary Dragonfire Apocalypse.",
        LoadItemIcon("FlameCore.png"), ItemType.Material, ItemRarity.Legendary, maxStack: 10, goldValue: 550,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Void_Essence", "mat_boss_void_essence", "Abyssal Essence",
        "Pitch-black essence dropped by Purple Ignis. Pulsates with boundless abyssal energy from another dimension.",
        LoadItemIcon("VoidEssence.png"), ItemType.Material, ItemRarity.Epic, maxStack: 20, goldValue: 260,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Mat_Void_Eye", "mat_boss_void_eye", "Void Singularity Eye",
        "A haunting cosmic eye obtained from Purple Ignis. Masterpiece material used to craft Excalibur Sovereign and the Voidwalker Mantle.",
        LoadItemIcon("VoidEye.png"), ItemType.Material, ItemRarity.Legendary, maxStack: 10, goldValue: 700,
        bonusAtk: 0, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // ==========================================
      // 4. GIÁP BẢO HỘ (ARMOR - 8 ITEMS)
      // ==========================================
      CreateOrUpdateItem("Item_Armor_Cloth_Novice", "armor_cloth_novice", "Novice Cloth Tunic",
        "A lightweight cloth tunic issued to new recruits. Grants rudimentary protection when stepping into the labyrinth (+2 DEF, +10 HP).",
        LoadItemIcon("NoviceArmor.png"), ItemType.Armor, ItemRarity.Common, maxStack: 1, goldValue: 20,
        bonusAtk: 0, bonusDef: 2f, bonusHp: 10f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Leather_Scout", "armor_leather_scout", "Scout Leather Armor",
        "Supple leather armor tailored for scouts. Balanced and flexible, protecting without hindering swift movement (+5 DEF, +25 HP, +10 Stamina).",
        LoadItemIcon("LeatherArmor.png"), ItemType.Armor, ItemRarity.Common, maxStack: 1, goldValue: 80,
        bonusAtk: 0, bonusDef: 5f, bonusHp: 25f, bonusStamina: 10f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Leather", "armor_leather_explorer", "Explorer Leather Vest",
        "Sturdy explorer leather vest crafted to withstand punishing dungeon expeditions (+9 DEF, +45 HP, +15 Stamina).",
        LoadItemIcon("ExplorerArmor.png"), ItemType.Armor, ItemRarity.Uncommon, maxStack: 1, goldValue: 160,
        bonusAtk: 0, bonusDef: 9f, bonusHp: 45f, bonusStamina: 15f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Iron_Cuirass", "armor_iron_cuirass", "Reinforced Iron Cuirass",
        "Hardened steel breastplate that significantly mitigates crushing blows from heavy monster attacks (+15 DEF, +70 HP).",
        LoadItemIcon("IronArmor.png"), ItemType.Armor, ItemRarity.Uncommon, maxStack: 1, goldValue: 280,
        bonusAtk: 0, bonusDef: 15f, bonusHp: 70f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Bone_Carapace", "armor_bone_carapace", "Bone Carapace Plate",
        "Carapace armor assembled from elite Skeleton bones, endowing the wearer with remarkable resilience (+22 DEF, +110 HP, +20 Stamina).",
        LoadItemIcon("BoneArmor.png"), ItemType.Armor, ItemRarity.Rare, maxStack: 1, goldValue: 420,
        bonusAtk: 0, bonusDef: 22f, bonusHp: 110f, bonusStamina: 20f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Frost_Guardian", "armor_frost_guardian", "Frostborne Platemail",
        "Glacial plate forged with the essence of the Ice Golem, radiating frost that dampens incoming kinetic shock (+32 DEF, +170 HP).",
        LoadItemIcon("FrostArmor.png"), ItemType.Armor, ItemRarity.Epic, maxStack: 1, goldValue: 650,
        bonusAtk: 0, bonusDef: 32f, bonusHp: 170f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Crimson_Scale", "armor_crimson_scale", "Crimson Dragon Scale",
        "Impenetrable battle armor reinforced with crimson dragon scales, offering absolute defense against lethal strikes (+45 DEF, +240 HP, +30 Stamina).",
        LoadItemIcon("CrimsonScale.png"), ItemType.Armor, ItemRarity.Legendary, maxStack: 1, goldValue: 950,
        bonusAtk: 0, bonusDef: 45f, bonusHp: 240f, bonusStamina: 30f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      CreateOrUpdateItem("Item_Armor_Void_Shroud", "armor_void_shroud", "Voidwalker Mantle",
        "Ethereal mantle woven from the deep cosmic void, warping mortal strikes away from the bearer (+58 DEF, +320 HP, +40 Stamina).",
        LoadItemIcon("VoidArmor.png"), ItemType.Armor, ItemRarity.Legendary, maxStack: 1, goldValue: 1400,
        bonusAtk: 0, bonusDef: 58f, bonusHp: 320f, bonusStamina: 40f, heal: 0,
        worldPrefab: worldPickup, visualPrefab: null);

      // ==========================================
      // 5. VŨ KHÍ CHIẾN ĐẤU (WEAPONS - 20 SWORDS)
      // ==========================================
      // Tier 1: Khởi Đầu & Cơ Bản (Common & Uncommon)
      CreateOrUpdateItem("Item_Sword_Wooden", "weapon_sword_wooden", "Apprentice Wooden Sword",
        "A simple practice wooden sword issued to recruits before venturing through the magical portal (+6 Attack).",
        LoadItemIcon("WoodenSword.png"), ItemType.Weapon, ItemRarity.Common, maxStack: 1, goldValue: 15,
        bonusAtk: 6f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/WoodenSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Rusty_Iron", "weapon_sword_rusty_iron", "Rusty Iron Blade",
        "A tarnished shortsword left discarded for years in the damp dungeon depths (+9 Attack).",
        LoadItemIcon("RustyIronBlade.png"), ItemType.Weapon, ItemRarity.Common, maxStack: 1, goldValue: 50,
        bonusAtk: 9f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/RustyIronSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Novice", "weapon_sword_novice", "Novice Knight Sword",
        "A standard knightly blade from Haven with a leather-wrapped hilt and a clean steel edge (+14 Attack).",
        LoadItemIcon("NoviceSword.png"), ItemType.Weapon, ItemRarity.Uncommon, maxStack: 1, goldValue: 110,
        bonusAtk: 14f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/NoviceSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Broad_Iron", "weapon_sword_broad_iron", "Broad Iron Blade",
        "A heavy broadsword designed for sweeping cleaves and subtle knockback force (+18 Attack).",
        LoadItemIcon("BroadIronBlade.png"), ItemType.Weapon, ItemRarity.Uncommon, maxStack: 1, goldValue: 175,
        bonusAtk: 18f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/IronBoardSword.prefab"));

      CreateOrUpdateItem("Item_Sword_DarkIron", "weapon_sword_darkiron", "Dark Iron Sword",
        "Smelted from subterranean dark ore, offering hardness and keenness far exceeding common iron (+22 Attack).",
        LoadItemIcon("DarkIronSword.png"), ItemType.Weapon, ItemRarity.Uncommon, maxStack: 1, goldValue: 240,
        bonusAtk: 22f, bonusDef: 0, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/DarkIronSword.prefab"));

      // Tier 2: Phiêu Lưu & Tinh Nhuệ (Rare)
      CreateOrUpdateItem("Item_Sword_Vanguard", "weapon_sword_vanguard", "Vanguard Broadsword",
        "A broadsword carried by frontline vanguards, bolstering both vitality and striking momentum (+27 Attack, +20 Max HP).",
        LoadItemIcon("VanguardSword.png"), ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 320,
        bonusAtk: 27f, bonusDef: 0, bonusHp: 20f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/VanguardSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Punk", "weapon_sword_punk", "Barbed Punk Blade",
        "A fierce jagged blade lined with vicious barbs, engineered for swift rending slashes (+31 Attack, +10 Stamina).",
        LoadItemIcon("PunkSword.png"), ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 380,
        bonusAtk: 31f, bonusDef: 0, bonusHp: 0, bonusStamina: 10f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/PunkSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Bone", "weapon_sword_bone", "Bonecleaver Blade",
        "Carved from dense Skeleton champion bones, heavily eroding the stance and poise of enemy targets (+34 Attack, +15 Stamina).",
        LoadItemIcon("BoneSword.png"), ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 430,
        bonusAtk: 34f, bonusDef: 0, bonusHp: 0, bonusStamina: 15f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/BoneSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Equilibrium", "weapon_sword_equilibrium", "Equilibrium Blade",
        "A twin-toned blade of light and dark, striking a flawless balance between offensive power and vitality (+37 Attack, +25 Max HP).",
        LoadItemIcon("EquilibriumBlade.png"), ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 480,
        bonusAtk: 37f, bonusDef: 0, bonusHp: 25f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/EquilibriumSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Dead", "weapon_sword_dead", "Deathbringer Sword",
        "A grim blade steeped in necromantic aura, siphoning lingering vigor from fallen foes (+40 Attack, +15 Max HP).",
        LoadItemIcon("DeadSword.png"), ItemType.Weapon, ItemRarity.Rare, maxStack: 1, goldValue: 520,
        bonusAtk: 40f, bonusDef: 0, bonusHp: 15f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/DeadSword.prefab"));

      // Tier 3: Ma Pháp & Tinh Hoa (Epic)
      CreateOrUpdateItem("Item_Sword_Emerald", "weapon_sword_emerald", "Emerald Jade Edge",
        "Sculpted from ancient jade, radiating a serene emerald sheen that wards its bearer (+45 Attack, +6 DEF).",
        LoadItemIcon("EmeraldSword.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 620,
        bonusAtk: 45f, bonusDef: 6f, bonusHp: 0, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/EmeraldSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Rose", "weapon_sword_rose", "Crimson Rose Rapier",
        "An ornate crimson rapier resembling a barbed rose blossom, striking with lethal speed and minimal effort (+49 Attack, +20 Stamina).",
        LoadItemIcon("RoseSword.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 680,
        bonusAtk: 49f, bonusDef: 0, bonusHp: 0, bonusStamina: 20f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/RoseSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Katsunesi", "weapon_sword_katsunesi", "Kitsune Spirit Katana",
        "A spirit katana housing the mystical soul of a kitsune, significantly amplifying sword waves and spells (+53 Attack, +30 Max HP).",
        LoadItemIcon("KatsunesiBlade.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 750,
        bonusAtk: 53f, bonusDef: 0, bonusHp: 30f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/KatsunesiSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Titan", "weapon_sword_titan", "Titan Heavy Cleaver",
        "A colossal cleaver that demands superhuman physique to wield, delivering devastating earth-shattering swings (+58 Attack, +45 Max HP).",
        LoadItemIcon("TitanSword.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 820,
        bonusAtk: 58f, bonusDef: 0, bonusHp: 45f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/TitanSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Demonic", "weapon_sword_demonic", "Nether Demonic Blade",
        "Forged from nether metals within the underworld abyss, emanating an overwhelming murderous aura (+63 Attack, +35 Max HP).",
        LoadItemIcon("DemonicSword.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 900,
        bonusAtk: 63f, bonusDef: 0, bonusHp: 35f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/DemonicSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Ice", "weapon_sword_ice", "Frostfang Glacial Sword",
        "An unmelting glacial blade carved from the Ice Golem, chilling the sinew and muscle of struck targets (+68 Attack, +8 DEF, +20 Stamina).",
        LoadItemIcon("IceSword.png"), ItemType.Weapon, ItemRarity.Epic, maxStack: 1, goldValue: 1000,
        bonusAtk: 68f, bonusDef: 8f, bonusHp: 0, bonusStamina: 20f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/IceSword.prefab"));

      // Tier 4: Thần Binh Tối Thượng (Legendary)
      CreateOrUpdateItem("Item_Sword_Sunblade", "weapon_sword_sunblade", "Radiant Sunblade",
        "A glorious sun-forged blade shining with blazing daylight, dispelling the dungeon gloom (+75 Attack, +30 Stamina).",
        LoadItemIcon("RadiantSunBlade.png"), ItemType.Weapon, ItemRarity.Legendary, maxStack: 1, goldValue: 1250,
        bonusAtk: 75f, bonusDef: 0, bonusHp: 0, bonusStamina: 30f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/RadiantSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Excalibur", "weapon_sword_excalibur", "Excalibur Sovereign",
        "The sovereign holy blade of ancient monarchs, commanding destiny and overwhelming battlefield authority (+84 Attack, +12 DEF, +60 Max HP).",
        LoadItemIcon("Excalibur.png"), ItemType.Weapon, ItemRarity.Legendary, maxStack: 1, goldValue: 1500,
        bonusAtk: 84f, bonusDef: 12f, bonusHp: 60f, bonusStamina: 0, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/ExcaliburSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Artifact", "weapon_sword_artifact", "Ancient Artifact Blade",
        "A relic excavated from a forgotten epoch, humming with incomprehensible primordial energy (+92 Attack, +70 Max HP, +35 Stamina).",
        LoadItemIcon("ArtifactSword.png"), ItemType.Weapon, ItemRarity.Legendary, maxStack: 1, goldValue: 1800,
        bonusAtk: 92f, bonusDef: 0, bonusHp: 70f, bonusStamina: 35f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/ArtifactSword.prefab"));

      CreateOrUpdateItem("Item_Sword_Dragonic", "weapon_sword_dragonic", "Dragonfire Apocalypse",
        "The ultimate apocalyptic blade harboring a dragon soul, consuming all adversaries in scorching inferno (+105 Attack, +80 Max HP, +40 Stamina).",
        LoadItemIcon("DragonicSword.png"), ItemType.Weapon, ItemRarity.Legendary, maxStack: 1, goldValue: 2200,
        bonusAtk: 105f, bonusDef: 0, bonusHp: 80f, bonusStamina: 40f, heal: 0,
        worldPrefab: worldPickup,
        visualPrefab: LoadWeaponPrefab($"{SwordsFolder}/DragonicSword.prefab"));

      EnsurePlayerWeaponVisualSync();

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      if (showDialog)
      {
        EditorUtility.DisplayDialog("Infinite Labyrinth", 
          "Đã khởi tạo và cập nhật thành công TOÀN BỘ 49 VẬT PHẨM:\n- 20 Vũ Khí (Đầy đủ 3D Model & Icon)\n- 8 Giáp Trụ (Thuộc tính & Icon)\n- 13 Nguyên Liệu Rèn & Lõi Boss\n- 6 Dược Phẩm Sinh Tồn\n- Tiền Vàng & Chìa Khóa Boss Hầm Ngục!\n\nToàn bộ dữ liệu nằm tại: Assets/ScriptableObjects/Items/", "OK");
      }
      Debug.Log("[ItemDatabaseGenerator] Đã tạo thành công toàn bộ 49 vật phẩm trong Assets/ScriptableObjects/Items/");
    }

    private static Sprite LoadItemIcon(string iconName)
    {
      string path = $"Assets/UI RPG/Icon/Items/{iconName}";
      Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
      if (sp == null)
      {
        Debug.LogWarning($"[ItemDatabaseGenerator] Không tìm thấy icon tại: {path}");
      }
      return sp;
    }

    private static GameObject LoadWeaponPrefab(string prefabPath)
    {
      GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      if (obj == null)
      {
        Debug.LogWarning($"[ItemDatabaseGenerator] Không tìm thấy model kiếm tại: {prefabPath}");
      }
      return obj;
    }

    [MenuItem("Infinite Labyrinth/RPG/2. Gắn Đồng Bộ Mô Hình Kiếm (PlayerWeaponVisualSync Cho Player)", false, 11)]
    public static void EnsurePlayerWeaponVisualSync()
    {
      string playerPrefabPath = "Assets/Characters/Player/Player.prefab";
      GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
      if (playerPrefab == null) return;

      using (var scope = new PrefabUtility.EditPrefabContentsScope(playerPrefabPath))
      {
        GameObject root = scope.prefabContentsRoot;
        var sync = root.GetComponent<PlayerWeaponVisualSync>();
        if (sync == null)
        {
          sync = root.AddComponent<PlayerWeaponVisualSync>();
        }

        SerializedObject so = new SerializedObject(sync);
        Transform vh = null;
        var hitbox = root.GetComponentInChildren<WeaponHitbox>(true);
        if (hitbox != null)
        {
          vh = hitbox.transform.Find("VisualHolder");
          if (vh == null) vh = hitbox.transform;
        }

        if (vh != null)
        {
          so.FindProperty("visualHolder").objectReferenceValue = vh;
        }

        GameObject woodenSword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Swords/WoodenSword.prefab");
        if (woodenSword != null)
        {
          so.FindProperty("defaultWeaponPrefab").objectReferenceValue = woodenSword;
        }

        so.ApplyModifiedProperties();
      }

      var inv = Object.FindAnyObjectByType<InventoryManager>();
      if (inv != null)
      {
        SerializedObject soInv = new SerializedObject(inv);
        ItemData starterSword = AssetDatabase.LoadAssetAtPath<ItemData>($"{TargetFolder}/Item_Sword_Wooden.asset");
        var pStarterWeapon = soInv.FindProperty("starterWeapon");
        var pStarterArmor = soInv.FindProperty("starterArmor");
        var pEquippedArmor = soInv.FindProperty("equippedArmor");
        if (pStarterWeapon != null && starterSword != null) pStarterWeapon.objectReferenceValue = starterSword;
        if (pStarterArmor != null) pStarterArmor.objectReferenceValue = null;
        if (pEquippedArmor != null) pEquippedArmor.objectReferenceValue = null;
        soInv.ApplyModifiedProperties();
        EditorUtility.SetDirty(inv);
      }

      Debug.Log("[ItemDatabaseGenerator] Đã gắn và cấu hình thành công PlayerWeaponVisualSync vào Player.prefab!");
    }

    [MenuItem("Infinite Labyrinth/RPG/3. Tạo Prefab Rơi Đồ (WorldItemPickup Prefab)", false, 12)]
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
      glow.range = 1.4f;
      glow.intensity = 1.5f;
      glow.shadows = LightShadows.None;
      glow.color = Color.white;

      GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, WorldPickupPrefabPath);
      GameObject.DestroyImmediate(root);

      EnsurePlayerWeaponVisualSync();

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
      glow.range = 1.4f;
      glow.intensity = 1.5f;
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

    [MenuItem("Infinite Labyrinth/RPG/4. Cấu Hình Tự Động Rơi Đồ (Setup Props & Monsters Loot)", false, 13)]
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

      EnsurePlayerWeaponVisualSync();

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      EditorUtility.DisplayDialog("Infinite Labyrinth", 
        "Đã tạo và gắn thành công cấu hình rơi đồ mặc định cho Thùng gỗ, Rương và Quái vật!", "OK");
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

    [MenuItem("Infinite Labyrinth/RPG/5. Tự Động Tạo Pickup UI & Notification Feed Trên Canvas", false, 14)]
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
