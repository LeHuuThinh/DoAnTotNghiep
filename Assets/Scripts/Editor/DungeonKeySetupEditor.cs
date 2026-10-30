using System.IO;
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Editor
{
  [InitializeOnLoad]
  public static class DungeonKeySetupEditor
  {
    private const string ItemFolder = "Assets/ScriptableObjects/Items";
    private const string KeyItemPath = "Assets/ScriptableObjects/Items/Item_Key_Dungeon.asset";
    private const string KeyPrefabPath = "Assets/Prefabs/Dungeon/Interactable/DungeonKeyPickup.prefab";
    private const string SourceKeyModelPath = "Assets/Prefabs/Dungeon/Interactable/key.prefab";
    private const string IconPath = "Assets/UI RPG/PlayerHUD 2/Spirit_Filled.png";

    static DungeonKeySetupEditor()
    {
      EditorApplication.delayCall += EnsureDungeonKeyAssets;
    }

    [MenuItem("Infinite Labyrinth/RPG/Tạo Chìa Khóa Hầm Ngục (Dungeon Key Setup)", false, 15)]
    public static void EnsureDungeonKeyAssets()
    {
      if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
      {
        AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
      }
      if (!AssetDatabase.IsValidFolder(ItemFolder))
      {
        AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");
      }

      // 1. TẠO HOẶC CẬP NHẬT ITEMDATA CHO CHÌA KHÓA HẦM NGỤC (ENGLISH STRINGS)
      ItemData keyItem = AssetDatabase.LoadAssetAtPath<ItemData>(KeyItemPath);
      bool isNewItem = false;
      if (keyItem == null)
      {
        keyItem = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(keyItem, KeyItemPath);
        isNewItem = true;
      }

      SerializedObject soItem = new SerializedObject(keyItem);
      soItem.FindProperty("id").stringValue = "dungeon_key";
      soItem.FindProperty("itemName").stringValue = "Dungeon Boss Key";
      soItem.FindProperty("description").stringValue = "An ancient golden key resonating with sacred magic. Required to unlock the Grand Gate leading into the Boss Arena.";
      
      Sprite keyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
      if (keyIcon != null)
      {
        soItem.FindProperty("icon").objectReferenceValue = keyIcon;
      }

      soItem.FindProperty("itemType").enumValueIndex = (int)ItemType.Key;
      soItem.FindProperty("rarity").enumValueIndex = (int)ItemRarity.Legendary;
      soItem.FindProperty("maxStack").intValue = 1;
      soItem.FindProperty("goldValue").intValue = 0;
      soItem.FindProperty("bonusAttack").floatValue = 0f;
      soItem.FindProperty("bonusDefense").floatValue = 0f;
      soItem.FindProperty("bonusHealth").floatValue = 0f;
      soItem.FindProperty("bonusStamina").floatValue = 0f;
      soItem.FindProperty("healAmount").floatValue = 0f;

      // 2. TẠO HOẶC CẬP NHẬT PREFAB WORLD PICKUP CHO CHÌA KHÓA (DUNGEONKEYPICKUP)
      GameObject keyPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KeyPrefabPath);
      if (keyPickupPrefab == null)
      {
        keyPickupPrefab = CreateDungeonKeyPickupPrefab(keyItem);
      }

      if (keyPickupPrefab != null)
      {
        soItem.FindProperty("worldModelPrefab").objectReferenceValue = keyPickupPrefab;
      }

      soItem.ApplyModifiedProperties();
      AssetDatabase.SaveAssets();

      if (isNewItem)
      {
        Debug.Log("<color=green>[DungeonKeySetupEditor]</color> 🗝️ Created ItemData (Dungeon Boss Key) and DungeonKeyPickup.prefab successfully!");
      }
    }

    private static GameObject CreateDungeonKeyPickupPrefab(ItemData keyItem)
    {
      if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Dungeon/Interactable"))
      {
        Directory.CreateDirectory("Assets/Prefabs/Dungeon/Interactable");
        AssetDatabase.Refresh();
      }

      GameObject root = new GameObject("DungeonKeyPickup");
      root.layer = LayerMask.NameToLayer("Item");
      if (root.layer < 0) root.layer = LayerMask.NameToLayer("Ignore Raycast");
      if (root.layer < 0) root.layer = 0;

      // SphereCollider Trigger nhặt đồ
      SphereCollider col = root.AddComponent<SphereCollider>();
      col.isTrigger = true;
      col.radius = 2.8f;

      // Model con chứa Mesh chìa khóa
      GameObject visualObj = new GameObject("Model");
      visualObj.transform.SetParent(root.transform, false);
      visualObj.transform.localPosition = Vector3.zero;
      visualObj.transform.localRotation = Quaternion.Euler(25f, 0f, 0f); // Nghiêng nhẹ 25 độ cho đẹp mắt
      visualObj.transform.localScale = Vector3.one; // Tỉ lệ chuẩn 1 1 1 cho chìa khóa hầm ngục

      GameObject sourceKey = AssetDatabase.LoadAssetAtPath<GameObject>(SourceKeyModelPath);
      if (sourceKey != null)
      {
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourceKey, visualObj.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // Đảm bảo tất cả collider con là trigger
        foreach (var c in modelInstance.GetComponentsInChildren<Collider>(true))
        {
          c.isTrigger = true;
        }
      }

      // Đèn hào quang ánh vàng hoàng gia rực rỡ (Legendary Amber Gold)
      GameObject lightObj = new GameObject("RarityGlow");
      lightObj.transform.SetParent(root.transform, false);
      lightObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);

      Light glow = lightObj.AddComponent<Light>();
      glow.type = LightType.Point;
      glow.range = 3.5f;
      glow.intensity = 3.5f;
      glow.shadows = LightShadows.None;
      glow.color = new Color(1.0f, 0.85f, 0.25f, 1.0f); // Ánh vàng Legendary

      // Gắn WorldItemPickup script
      var pickupComp = root.AddComponent<WorldItemPickup>();
      SerializedObject soPickup = new SerializedObject(pickupComp);
      soPickup.FindProperty("itemData").objectReferenceValue = keyItem;
      soPickup.FindProperty("quantity").intValue = 1;
      soPickup.FindProperty("visualTransform").objectReferenceValue = visualObj.transform;
      soPickup.FindProperty("rarityLight").objectReferenceValue = glow;
      soPickup.FindProperty("lightRange").floatValue = 3.5f;
      soPickup.FindProperty("lightIntensity").floatValue = 2.5f;
      soPickup.FindProperty("legendaryLightIntensity").floatValue = 3.5f;
      soPickup.FindProperty("rotationSpeed").floatValue = 60f;
      soPickup.FindProperty("bobAmplitude").floatValue = 0.08f;
      soPickup.FindProperty("bobFrequency").floatValue = 2.2f;
      soPickup.FindProperty("pickupRadius").floatValue = 2.8f;
      soPickup.ApplyModifiedProperties();

      GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, KeyPrefabPath);
      Object.DestroyImmediate(root);
      return savedPrefab;
    }
  }
}
