using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Blacksmith;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý hiển thị và tương tác của 1 dòng công thức chế tạo (Recipe Row) trong lò rèn
  /// </summary>
  public class ForgeRecipeSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
  {
    [Header("=== KHUNG TRANG TRÍ DÒNG (ROW DECORATIONS) ===")]
    [SerializeField] private GameObject itemBorder;
    [SerializeField] private GameObject itemCorner;

    [Header("=== KHUNG TRANG TRÍ ICON (ICON DECORATIONS) ===")]
    [SerializeField] private GameObject iconBorder;
    [SerializeField] private GameObject iconCorner;

    [Header("=== CÁC THÀNH PHẦN HIỂN THỊ ===")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityBgImage;
    [SerializeField] private Image rarityBarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;

    private CraftingRecipe recipe;
    private bool isSelected = false;
    private bool isHovered = false;
    private int slotIndex = -1;

    public CraftingRecipe Recipe => recipe;
    public bool IsSelected => isSelected;
    public int SlotIndex => slotIndex;

    public event Action<ForgeRecipeSlotUI> OnClicked;

    private void Awake()
    {
      AutoFindReferences();
      UpdateVisualStates();
    }

    public void AutoFindReferences()
    {
      if (itemBorder == null) itemBorder = transform.Find("Border")?.gameObject;
      if (itemCorner == null) itemCorner = transform.Find("Corner")?.gameObject;

      Transform iconTr = transform.Find("ItemIcon");
      if (iconTr != null)
      {
        if (iconBorder == null) iconBorder = iconTr.Find("Border")?.gameObject;
        if (iconCorner == null) iconCorner = iconTr.Find("Corner")?.gameObject;
        if (iconImage == null)
        {
          var iconChild = iconTr.Find("Icon") ?? iconTr.Find("ItemIcon");
          iconImage = iconChild?.GetComponent<Image>();
        }
        if (rarityBgImage == null) rarityBgImage = iconTr.Find("RarityBG")?.GetComponent<Image>();
        if (rarityBarImage == null) rarityBarImage = iconTr.Find("RarityBar")?.GetComponent<Image>();
      }

      Transform infoTr = transform.Find("itemInfo");
      if (infoTr != null)
      {
        if (nameText == null) nameText = infoTr.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (descText == null) descText = infoTr.Find("SmallDescription")?.GetComponent<TextMeshProUGUI>();
      }
    }

    /// <summary>
    /// Gán công thức rèn vào dòng
    /// </summary>
    public void Setup(CraftingRecipe recipeData, int index)
    {
      AutoFindReferences();
      recipe = recipeData;
      slotIndex = index;

      if (recipe == null || recipe.ResultItem == null)
      {
        gameObject.SetActive(false);
        return;
      }

      gameObject.SetActive(true);
      ItemData item = recipe.ResultItem;

      // 1. Icon & Phẩm cấp
      SafeSetIcon(iconImage, item.Icon);

      Color rColor = item.Rarity.GetRarityColor();
      if (rarityBgImage != null)
      {
        rarityBgImage.gameObject.SetActive(true);
        rarityBgImage.color = new Color(rColor.r, rColor.g, rColor.b, 0.25f);
      }
      if (rarityBarImage != null)
      {
        rarityBarImage.gameObject.SetActive(true);
        rarityBarImage.color = rColor;
      }

      // 2. Tên & Mô tả (giới hạn tối đa 2 dòng kèm ellipsis)
      if (nameText != null)
      {
        nameText.text = item.Rarity.FormatWithGradient(recipe.RecipeName);
      }

      if (descText != null)
      {
        descText.enableWordWrapping = true;
        descText.maxVisibleLines = 2;
        descText.overflowMode = TextOverflowModes.Ellipsis;
        descText.text = TruncateDescription(recipe.Description, 72);
      }

      UpdateVisualStates();
    }

    private void SafeSetIcon(Image img, Sprite icon)
    {
      if (img == null) return;
      bool hasIcon = icon != null;
      img.gameObject.SetActive(hasIcon);
      if (img.transform.parent != null && img.transform.parent.name.Contains("Icon"))
      {
        img.transform.parent.gameObject.SetActive(true);
      }
      img.sprite = icon;
      img.enabled = hasIcon;
      img.color = Color.white;
    }

    private string TruncateDescription(string desc, int maxChars = 72)
    {
      if (string.IsNullOrEmpty(desc)) return "";
      desc = desc.Trim();
      if (desc.Length <= maxChars) return desc;
      return desc.Substring(0, maxChars).TrimEnd() + "...";
    }

    public void SetSelected(bool selected)
    {
      isSelected = selected;
      UpdateVisualStates();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      isHovered = true;
      UpdateVisualStates();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      isHovered = false;
      UpdateVisualStates();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (eventData.button == PointerEventData.InputButton.Left)
      {
        OnClicked?.Invoke(this);
      }
    }

    private void UpdateVisualStates()
    {
      bool highlight = isSelected || isHovered;
      if (itemBorder != null) itemBorder.SetActive(highlight);
      if (itemCorner != null) itemCorner.SetActive(highlight);
      if (iconBorder != null) iconBorder.SetActive(highlight);
      if (iconCorner != null) iconCorner.SetActive(highlight);
    }
  }
}
