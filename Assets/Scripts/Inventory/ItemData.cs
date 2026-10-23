using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  [CreateAssetMenu(fileName = "New_Item", menuName = "Infinite Labyrinth/RPG/Item Data")]
  public class ItemData : ScriptableObject
  {
    [Header("=== THÔNG TIN CƠ BẢN ===")]
    [Tooltip("Mã định danh duy nhất (VD: weapon_sword_iron)")]
    [SerializeField] private string id = "item_id";
    [Tooltip("Tên vật phẩm hiển thị trong Game")]
    [SerializeField] private string itemName = "Tên Vật Phẩm";
    [TextArea(2, 5)]
    [Tooltip("Mô tả chi tiết hoặc cốt truyện của vật phẩm")]
    [SerializeField] private string description = "Mô tả vật phẩm...";
    [Tooltip("Icon 2D hiển thị trong Túi đồ và Pickup UI")]
    [SerializeField] private Sprite icon;
    [Tooltip("Phân loại vật phẩm")]
    [SerializeField] private ItemType itemType = ItemType.Consumable;
    [Tooltip("Phẩm cấp / Độ hiếm")]
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;

    [Header("=== CHỨA ĐỒ (STACKING) & KINH TẾ ===")]
    [Tooltip("Số lượng tối đa trong 1 ô (Vũ khí/Giáp thường là 1, Thuốc/NL là 99)")]
    [SerializeField] private int maxStack = 1;
    [Tooltip("Giá trị vàng (Dùng để bán vào shop hoặc số vàng nhận được nếu là Currency)")]
    [SerializeField] private int goldValue = 10;

    [Header("=== CHỈ SỐ BUFF (CHO VŨ KHÍ & GIÁP) ===")]
    [Tooltip("Sát thương cộng thêm khi trang bị (cho Vũ khí)")]
    [SerializeField] private float bonusAttack = 0f;
    [Tooltip("Điểm phòng thủ / giảm sát thương (cho Giáp)")]
    [SerializeField] private float bonusDefense = 0f;
    [Tooltip("Máu tối đa cộng thêm")]
    [SerializeField] private float bonusHealth = 0f;
    [Tooltip("Thể lực tối đa cộng thêm")]
    [SerializeField] private float bonusStamina = 0f;

    [Header("=== TIÊU HAO (CHO BÌNH MÁU / THUỐC) ===")]
    [Tooltip("Lượng máu hồi phục ngay lập tức khi dùng")]
    [SerializeField] private float healAmount = 0f;

    [Header("=== MODEL 3D HIỂN THỊ ===")]
    [Tooltip("Prefab model 3D thu nhỏ rơi xuống đất trong hầm ngục")]
    [SerializeField] private GameObject worldModelPrefab;
    [Tooltip("Prefab hiển thị gắn vào tay người chơi khi trang bị (cho Vũ khí)")]
    [SerializeField] private GameObject weaponVisualPrefab;

    // Public Getters
    public string Id => id;
    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public ItemRarity Rarity => rarity;
    public int MaxStack => Mathf.Max(1, maxStack);
    public bool IsStackable => maxStack > 1;
    public int GoldValue => goldValue;

    public float BonusAttack => bonusAttack;
    public float BonusDefense => bonusDefense;
    public float BonusHealth => bonusHealth;
    public float BonusStamina => bonusStamina;
    public float HealAmount => healAmount;

    public GameObject WorldModelPrefab => worldModelPrefab;
    public GameObject WeaponVisualPrefab => weaponVisualPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (string.IsNullOrEmpty(id))
      {
        id = name.ToLower().Replace(" ", "_");
      }
      if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
      {
        maxStack = 1; // Trang bị không cộng dồn ô
      }
    }
#endif
  }
}
