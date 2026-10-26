using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý toàn bộ hệ thống Túi đồ (Inventory), Tiền tệ (Gold) và Trang bị (Equipment)
  /// </summary>
  public class InventoryManager : MonoBehaviour
  {
    public static InventoryManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("=== CẤU HÌNH KHO ĐỒ ===")]
    [Tooltip("Số lượng ô chứa tối đa của túi đồ")]
    [SerializeField] private int maxCapacity = 30;
    [Tooltip("Số vàng ban đầu")]
    [SerializeField] private int initialGold = 50;

    [Header("=== DỮ LIỆU THỜI GIAN THỰC (READ-ONLY INSPECTOR) ===")]
    [SerializeField] private int currentGold = 0;
    [SerializeField] private ItemData equippedWeapon;
    [SerializeField] private ItemData equippedArmor;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // Events để UI và các hệ thống khác đăng ký lắng nghe
    public event Action OnInventoryChanged;
    public event Action<int> OnGoldChanged;
    public event Action<ItemType, ItemData> OnEquipmentChanged;
    public event Action<ItemData, int> OnItemAdded; // Phục vụ Notification Feed bên trái màn hình

    // Properties
    public int CurrentGold => currentGold;
    public int MaxCapacity => maxCapacity;
    public IReadOnlyList<InventorySlot> Slots => slots;
    public ItemData EquippedWeapon => equippedWeapon;
    public ItemData EquippedArmor => equippedArmor;

    private Health cachedPlayerHealth;
    private Transform cachedPlayerTransform;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);

      InitializeInventory();
    }

    private void Start()
    {
      CachePlayerReferences();
    }

    private void InitializeInventory()
    {
      currentGold = initialGold;
      if (slots == null || slots.Count == 0)
      {
        slots = new List<InventorySlot>(maxCapacity);
        for (int i = 0; i < maxCapacity; i++)
        {
          slots.Add(new InventorySlot());
        }
      }
    }

    private void CachePlayerReferences()
    {
      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        cachedPlayerTransform = playerObj.transform;
        cachedPlayerHealth = playerObj.GetComponent<Health>();
      }
      else
      {
        var playerCtrl = FindObjectOfType<PlayerController>();
        if (playerCtrl != null)
        {
          cachedPlayerTransform = playerCtrl.transform;
          cachedPlayerHealth = playerCtrl.GetComponent<Health>();
        }
      }
    }

    // =========================================================================
    // 1. THÊM / BỚT VẬT PHẨM (ADD / REMOVE ITEMS)
    // =========================================================================

    /// <summary>
    /// Thêm vật phẩm vào túi đồ
    /// </summary>
    /// <param name="item">Vật phẩm</param>
    /// <param name="quantity">Số lượng cần thêm</param>
    /// <param name="remainder">Số lượng còn dư chưa nhặt được nếu túi đầy</param>
    /// <returns>True nếu nhặt được ít nhất 1 cái, False nếu hoàn toàn không chứa được</returns>
    public bool AddItem(ItemData item, int quantity, out int remainder)
    {
      remainder = quantity;
      if (item == null || quantity <= 0) return false;

      // 1. Nếu là Tiền vàng -> Cộng thẳng vào ví tiền
      if (item.ItemType == ItemType.Currency)
      {
        int totalGoldEarned = item.GoldValue * quantity;
        AddGold(totalGoldEarned);
        remainder = 0;
        OnItemAdded?.Invoke(item, quantity);
        return true;
      }

      int originalQuantity = quantity;

      // 2. Nếu là vật phẩm có thể cộng dồn (Stackable) -> Ưu tiên lấp đầy các ô đã có
      if (item.IsStackable)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (!slots[i].IsEmpty && slots[i].ItemData.Id == item.Id)
          {
            remainder = slots[i].AddQuantity(remainder);
            if (remainder <= 0) break;
          }
        }
      }

      // 3. Nếu vẫn còn dư -> Tìm các ô trống đầu tiên để thêm mới
      if (remainder > 0)
      {
        for (int i = 0; i < slots.Count; i++)
        {
          if (slots[i].IsEmpty)
          {
            int toPut = Mathf.Min(remainder, item.MaxStack);
            slots[i].SetItem(item, toPut);
            remainder -= toPut;
            if (remainder <= 0) break;
          }
        }
      }

      int itemsAdded = originalQuantity - remainder;
      if (itemsAdded > 0)
      {
        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(item, itemsAdded);
        return true;
      }

      return false; // Túi đầy hoàn toàn
    }

    public bool AddItem(ItemData item, int quantity = 1)
    {
      return AddItem(item, quantity, out _);
    }

    /// <summary>
    /// Xóa một lượng vật phẩm khỏi túi đồ theo ItemData
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
      if (item == null || quantity <= 0) return false;

      // Kiểm tra xem có đủ số lượng trong túi không
      int totalOwned = GetItemCount(item);
      if (totalOwned < quantity) return false;

      int neededToRemove = quantity;
      for (int i = slots.Count - 1; i >= 0; i--)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData.Id == item.Id)
        {
          if (slots[i].Quantity <= neededToRemove)
          {
            neededToRemove -= slots[i].Quantity;
            slots[i].Clear();
          }
          else
          {
            slots[i].RemoveQuantity(neededToRemove);
            neededToRemove = 0;
          }

          if (neededToRemove <= 0) break;
        }
      }

      OnInventoryChanged?.Invoke();
      return true;
    }

    /// <summary>
    /// Xóa vật phẩm tại 1 ô cụ thể
    /// </summary>
    public bool RemoveItemAt(int slotIndex, int quantity = 1)
    {
      if (slotIndex < 0 || slotIndex >= slots.Count) return false;
      if (slots[slotIndex].IsEmpty) return false;

      bool success = slots[slotIndex].RemoveQuantity(quantity);
      if (success)
      {
        OnInventoryChanged?.Invoke();
      }
      return success;
    }

    /// <summary>
    /// Đếm tổng số lượng một vật phẩm đang có trong túi
    /// </summary>
    public int GetItemCount(ItemData item)
    {
      if (item == null) return 0;
      int count = 0;
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData.Id == item.Id)
        {
          count += slots[i].Quantity;
        }
      }
      return count;
    }

    // =========================================================================
    // 2. TRANG BỊ & THÁO ĐỒ (EQUIPMENT)
    // =========================================================================

    /// <summary>
    /// Trang bị một món đồ từ túi đồ
    /// </summary>
    public bool Equip(ItemData item)
    {
      if (item == null) return false;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor) return false;

      // Tìm vị trí món đồ trong túi
      int slotIdx = -1;
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData == item)
        {
          slotIdx = i;
          break;
        }
      }

      if (slotIdx == -1) return false;

      // Xử lý đổi trang bị
      if (item.ItemType == ItemType.Weapon)
      {
        ItemData oldWeapon = equippedWeapon;
        equippedWeapon = item;
        slots[slotIdx].Clear();

        if (oldWeapon != null)
        {
          slots[slotIdx].SetItem(oldWeapon, 1);
        }

        OnEquipmentChanged?.Invoke(ItemType.Weapon, equippedWeapon);
      }
      else if (item.ItemType == ItemType.Armor)
      {
        ItemData oldArmor = equippedArmor;
        equippedArmor = item;
        slots[slotIdx].Clear();

        if (oldArmor != null)
        {
          slots[slotIdx].SetItem(oldArmor, 1);
        }

        OnEquipmentChanged?.Invoke(ItemType.Armor, equippedArmor);
      }

      OnInventoryChanged?.Invoke();
      return true;
    }

    /// <summary>
    /// Tháo trang bị đưa về túi đồ
    /// </summary>
    public bool Unequip(ItemType type)
    {
      ItemData toUnequip = (type == ItemType.Weapon) ? equippedWeapon : equippedArmor;
      if (toUnequip == null) return false;

      // Kiểm tra túi đồ còn chỗ trống không
      int emptyIdx = slots.FindIndex(s => s.IsEmpty);
      if (emptyIdx == -1)
      {
        Debug.LogWarning("[Inventory] Túi đồ đã đầy, không thể tháo trang bị!");
        return false;
      }

      slots[emptyIdx].SetItem(toUnequip, 1);

      if (type == ItemType.Weapon)
      {
        equippedWeapon = null;
        OnEquipmentChanged?.Invoke(ItemType.Weapon, null);
      }
      else
      {
        equippedArmor = null;
        OnEquipmentChanged?.Invoke(ItemType.Armor, null);
      }

      OnInventoryChanged?.Invoke();
      return true;
    }

    // =========================================================================
    // 3. SỬ DỤNG VẬT PHẨM TIÊU HAO (USE CONSUMABLES)
    // =========================================================================

    public bool UseItem(ItemData item)
    {
      if (item == null || item.ItemType != ItemType.Consumable) return false;

      if (cachedPlayerHealth == null)
      {
        CachePlayerReferences();
      }

      // Hồi máu nếu vật phẩm có chỉ số hồi máu
      if (item.HealAmount > 0f)
      {
        if (cachedPlayerHealth != null)
        {
          if (cachedPlayerHealth.CurrentHealth >= cachedPlayerHealth.MaxHealth)
          {
            Debug.Log("[Inventory] Máu của bạn đã đầy!");
            return false;
          }
          cachedPlayerHealth.Heal(item.HealAmount);
          Debug.Log($"[Inventory] Đã dùng {item.ItemName}, hồi phục {item.HealAmount} HP!");
        }
      }

      // Trừ 1 vật phẩm khỏi túi đồ
      RemoveItem(item, 1);
      return true;
    }

    // =========================================================================
    // 4. VỨT ĐỒ RA THẾ GIỚI (DROP ITEM)
    // =========================================================================

    public bool DropItem(int slotIndex, int count = 1)
    {
      if (slotIndex < 0 || slotIndex >= slots.Count) return false;
      if (slots[slotIndex].IsEmpty) return false;

      ItemData item = slots[slotIndex].ItemData;
      int dropCount = Mathf.Min(count, slots[slotIndex].Quantity);

      // Trừ số lượng trong túi
      slots[slotIndex].RemoveQuantity(dropCount);
      OnInventoryChanged?.Invoke();

      // Sinh model rơi ra đất trước mặt nhân vật nếu có prefab
      SpawnWorldDrop(item, dropCount);
      return true;
    }

    private void SpawnWorldDrop(ItemData item, int count)
    {
      if (cachedPlayerTransform == null)
      {
        CachePlayerReferences();
      }

      Vector3 spawnPos = Vector3.zero;
      Vector3 forward = Vector3.forward;

      if (cachedPlayerTransform != null)
      {
        forward = cachedPlayerTransform.forward;
        spawnPos = cachedPlayerTransform.position + forward * 1.3f + Vector3.up * 0.5f;
      }

      if (item.WorldModelPrefab != null)
      {
        GameObject dropObj = Instantiate(item.WorldModelPrefab, spawnPos, Quaternion.identity);
        // Lưu ý: Component WorldItemPickup sẽ được gán và khởi tạo tại Task RPG-02
        var rb = dropObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
          rb.AddForce((forward + Vector3.up * 0.8f).normalized * 3.5f, ForceMode.Impulse);
        }
      }
      else
      {
        Debug.LogWarning($"[Inventory] Vật phẩm {item.ItemName} chưa gán WorldModelPrefab để rơi ra đất!");
      }
    }

    // =========================================================================
    // 5. TIỀN TỆ (GOLD)
    // =========================================================================

    public void AddGold(int amount)
    {
      if (amount <= 0) return;
      currentGold += amount;
      OnGoldChanged?.Invoke(currentGold);
    }

    public bool SpendGold(int amount)
    {
      if (amount <= 0) return true;
      if (currentGold < amount) return false;

      currentGold -= amount;
      OnGoldChanged?.Invoke(currentGold);
      return true;
    }

    // =========================================================================
    // 6. TÍNH TOÁN CHỈ SỐ TỔNG (TOTAL BONUS STATS)
    // =========================================================================

    public float GetTotalAttackBonus()
    {
      float bonus = 0f;
      if (equippedWeapon != null) bonus += equippedWeapon.BonusAttack;
      if (equippedArmor != null) bonus += equippedArmor.BonusAttack;
      return bonus;
    }

    public float GetTotalDefenseBonus()
    {
      float bonus = 0f;
      if (equippedArmor != null) bonus += equippedArmor.BonusDefense;
      if (equippedWeapon != null) bonus += equippedWeapon.BonusDefense;
      return bonus;
    }

    public float GetTotalHealthBonus()
    {
      float bonus = 0f;
      if (equippedArmor != null) bonus += equippedArmor.BonusHealth;
      if (equippedWeapon != null) bonus += equippedWeapon.BonusHealth;
      return bonus;
    }

    public float GetTotalStaminaBonus()
    {
      float bonus = 0f;
      if (equippedArmor != null) bonus += equippedArmor.BonusStamina;
      if (equippedWeapon != null) bonus += equippedWeapon.BonusStamina;
      return bonus;
    }

    public List<InventorySlot> GetSlotsByCategory(ItemType category)
    {
      var result = new List<InventorySlot>();
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData.ItemType == category)
        {
          result.Add(slots[i]);
        }
      }
      return result;
    }
  }
}
