using System;
using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Blacksmith
{
  /// <summary>
  /// Chi phí 1 nguyên liệu thành phần trong công thức rèn
  /// </summary>
  [Serializable]
  public class CraftingMaterialCost
  {
    [Tooltip("Vật phẩm nguyên liệu (ScriptableObject)")]
    [SerializeField] private ItemData materialItem;

    [Tooltip("Số lượng cần")]
    [SerializeField] private int quantity = 1;

    public ItemData MaterialItem => materialItem;
    public int Quantity => Mathf.Max(1, quantity);

    public CraftingMaterialCost() { }

    public CraftingMaterialCost(ItemData item, int count)
    {
      materialItem = item;
      quantity = count;
    }
  }

  /// <summary>
  /// ScriptableObject định nghĩa 1 công thức chế tạo thần binh (Crafting Recipe)
  /// </summary>
  [CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Infinite Labyrinth/Blacksmith/Crafting Recipe", order = 101)]
  public class CraftingRecipe : ScriptableObject
  {
    [Header("=== THÔNG TIN CÔNG THỨC ===")]
    [SerializeField] private string recipeId = "recipe_id";
    [SerializeField] private string recipeName = "Recipe Name";
    [TextArea(2, 4)]
    [SerializeField] private string description = "Recipe description";

    [Header("=== THÀNH PHẨM (RESULT ITEM) ===")]
    [SerializeField] private ItemData resultItem;
    [SerializeField] private int resultQuantity = 1;

    [Header("=== CHI PHÍ RÈN (COSTS) ===")]
    [SerializeField] private int goldCost = 100;
    [SerializeField] private List<CraftingMaterialCost> requiredMaterials = new List<CraftingMaterialCost>();

    public string RecipeId => recipeId;
    public string RecipeName => string.IsNullOrEmpty(recipeName) && resultItem != null ? resultItem.ItemName : recipeName;
    public string Description => string.IsNullOrEmpty(description) && resultItem != null ? resultItem.Description : description;
    public ItemData ResultItem => resultItem;
    public int ResultQuantity => Mathf.Max(1, resultQuantity);
    public int GoldCost => Mathf.Max(0, goldCost);
    public IReadOnlyList<CraftingMaterialCost> RequiredMaterials => requiredMaterials;

    public void Setup(string id, string name, string desc, ItemData result, int resultCount, int gold, List<CraftingMaterialCost> mats)
    {
      recipeId = id;
      recipeName = name;
      description = desc;
      resultItem = result;
      resultQuantity = resultCount;
      goldCost = gold;
      requiredMaterials = mats ?? new List<CraftingMaterialCost>();
    }
  }
}
