using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("=== TRANG BỊ MẶC ĐỊNH CHO TÂN BINH ===")]
    [Tooltip("Vật phẩm tiêu hao ban đầu (Tùy chọn)")]
    [SerializeField] private ItemData starterUsable;
    [SerializeField] private int starterUsableCount = 0;
    [Tooltip("Vũ khí mặc định ban đầu (Kiếm gỗ tập sự)")]
    [SerializeField] private ItemData starterWeapon;
    [Tooltip("Giáp mặc định ban đầu (Áo vải tân binh)")]
    [SerializeField] private ItemData starterArmor;

    [Header("=== DỮ LIỆU THỜI GIAN THỰC (READ-ONLY INSPECTOR) ===")]
    [SerializeField] private int currentGold = 0;
    [SerializeField] private ItemData equippedWeapon;
    [SerializeField] private ItemData equippedArmor;
    [SerializeField] private ItemData equippedUsable;
    [SerializeField] private int equippedUsableCount = 0;

    [Header("=== COOLDOWN DÙNG THUỐC (GLOBAL CONSUMABLE COOLDOWN) ===")]
    [Tooltip("Thời gian hồi chiêu dùng chung giữa các loại bình máu/thuốc (giây)")]
    [SerializeField] private float consumableGlobalCooldown = 2.0f;
    [SerializeField] private float consumableCooldownTimer = 0f;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // Events để UI và các hệ thống khác đăng ký lắng nghe
    public event Action OnInventoryChanged;
    public event Action<int> OnGoldChanged;
    public event Action<ItemType, ItemData> OnEquipmentChanged;
    public event Action<ItemData, int> OnItemAdded; // Phục vụ Notification Feed bên trái màn hình
    public event Action<ItemData, int> OnUsableChanged;
    public event Action<float, float> OnConsumableCooldownUpdated; // (currentTimer, maxCooldown)
    public event Action OnUsableUsed;

    // Properties
    public int CurrentGold => currentGold;
    public int MaxCapacity => maxCapacity;
    public IReadOnlyList<InventorySlot> Slots => slots;
    public ItemData EquippedWeapon => equippedWeapon;
    public ItemData EquippedArmor => equippedArmor;
    public ItemData EquippedUsable => equippedUsable;
    public int EquippedUsableCount => equippedUsableCount;
    public float ConsumableGlobalCooldown => consumableGlobalCooldown;
    public float ConsumableCooldownTimer => consumableCooldownTimer;
    public bool IsConsumableOnCooldown => consumableCooldownTimer > 0f;
    public float ConsumableCooldownNormalized => consumableGlobalCooldown > 0f ? Mathf.Clamp01(consumableCooldownTimer / consumableGlobalCooldown) : 0f;

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
      if (equippedWeapon != null)
      {
        OnEquipmentChanged?.Invoke(ItemType.Weapon, equippedWeapon);
      }
      if (equippedArmor != null)
      {
        OnEquipmentChanged?.Invoke(ItemType.Armor, equippedArmor);
      }
      if (equippedUsable != null && equippedUsableCount > 0)
      {
        OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
        OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
      }
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

      if (equippedWeapon == null && starterWeapon != null)
      {
        equippedWeapon = starterWeapon;
      }
      if (equippedArmor == null && starterArmor != null)
      {
        equippedArmor = starterArmor;
      }
      if (equippedUsable == null && starterUsable != null && starterUsableCount > 0)
      {
        equippedUsable = starterUsable;
        equippedUsableCount = starterUsableCount;
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

    private void Update()
    {
      UpdateConsumableCooldown();
      HandleQuickSlotInput();
    }

    private void UpdateConsumableCooldown()
    {
      if (consumableCooldownTimer > 0f)
      {
        consumableCooldownTimer -= Time.unscaledDeltaTime;
        if (consumableCooldownTimer < 0f)
        {
          consumableCooldownTimer = 0f;
        }
        OnConsumableCooldownUpdated?.Invoke(consumableCooldownTimer, consumableGlobalCooldown);
      }
    }

    private void HandleQuickSlotInput()
    {
      if (Keyboard.current == null) return;

      // Nhấn C để sử dụng bình thuốc đang trang bị
      if (Keyboard.current.cKey.wasPressedThisFrame)
      {
        // Tránh kích hoạt khi đang mở bảng Inventory UI
        if (UI.InventoryUIController.IsOpen) return;

        UseEquippedUsable();
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
    public bool AddItem(ItemData item, int quantity, out int remainder, bool notifyPickup = true, bool allowRefillEquippedUsable = true)
    {
      remainder = quantity;
      if (item == null || quantity <= 0) return false;

      // 1. Nếu là Tiền vàng -> Cộng thẳng vào ví tiền
      if (item.ItemType == ItemType.Currency)
      {
        int totalGoldEarned = item.GoldValue * quantity;
        AddGold(totalGoldEarned);
        remainder = 0;
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, quantity);
        }
        return true;
      }

      int originalQuantity = quantity;

      // 1.5. Nếu là vật phẩm Tiêu hao (Consumable) và đang được trang bị trên ô Usable -> Ưu tiên nạp đầy ô Usable trước
      if (allowRefillEquippedUsable && item.ItemType == ItemType.Consumable && equippedUsable != null && equippedUsable.Id == item.Id)
      {
        int maxStack = item.MaxStack > 0 ? item.MaxStack : 99;
        int spaceLeft = Mathf.Max(0, maxStack - equippedUsableCount);
        if (spaceLeft > 0)
        {
          int toAdd = Mathf.Min(remainder, spaceLeft);
          equippedUsableCount += toAdd;
          remainder -= toAdd;
          OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
          OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
        }
      }

      // 2. Nếu là vật phẩm có thể cộng dồn (Stackable) -> Ưu tiên lấp đầy các ô đã có trong túi
      if (item.IsStackable && remainder > 0)
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
        if (notifyPickup)
        {
          OnItemAdded?.Invoke(item, itemsAdded);
        }
        return true;
      }

      return false; // Túi đầy hoàn toàn
    }

    public bool AddItem(ItemData item, int quantity = 1, bool notifyPickup = true, bool allowRefillEquippedUsable = true)
    {
      return AddItem(item, quantity, out _, notifyPickup, allowRefillEquippedUsable);
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

    /// <summary>
    /// Kiểm tra người chơi có sở hữu vật phẩm theo ID không
    /// </summary>
    public bool HasItem(string itemId)
    {
      if (string.IsNullOrEmpty(itemId)) return false;
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData != null && slots[i].ItemData.Id.Equals(itemId, System.StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Đếm tổng số lượng một vật phẩm đang có trong túi theo ID
    /// </summary>
    public int GetItemCount(string itemId)
    {
      if (string.IsNullOrEmpty(itemId)) return 0;
      int count = 0;
      for (int i = 0; i < slots.Count; i++)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData != null && slots[i].ItemData.Id.Equals(itemId, System.StringComparison.OrdinalIgnoreCase))
        {
          count += slots[i].Quantity;
        }
      }
      return count;
    }

    /// <summary>
    /// Xóa vật phẩm theo ID khỏi túi đồ
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
      if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
      int totalOwned = GetItemCount(itemId);
      if (totalOwned < quantity) return false;

      int neededToRemove = quantity;
      for (int i = slots.Count - 1; i >= 0; i--)
      {
        if (!slots[i].IsEmpty && slots[i].ItemData != null && slots[i].ItemData.Id.Equals(itemId, System.StringComparison.OrdinalIgnoreCase))
        {
          int currentSlotQty = slots[i].Quantity;
          if (currentSlotQty <= neededToRemove)
          {
            neededToRemove -= currentSlotQty;
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

    // =========================================================================
    // 2. TRANG BỊ & THÁO ĐỒ (EQUIPMENT)
    // =========================================================================

    /// <summary>
    /// Hoán đổi vị trí hoặc gộp stack giữa 2 ô trong túi đồ (hỗ trợ kéo thả)
    /// </summary>
    public bool SwapOrMergeSlots(int fromIndex, int toIndex)
    {
      if (fromIndex < 0 || fromIndex >= slots.Count) return false;
      if (toIndex < 0 || toIndex >= slots.Count) return false;
      if (fromIndex == toIndex) return false;

      var slotA = slots[fromIndex];
      var slotB = slots[toIndex];

      if (slotA.IsEmpty) return false;

      // 1. Nếu ô B đang trống -> Chuyển toàn bộ sang B
      if (slotB.IsEmpty)
      {
        slotB.SetItem(slotA.ItemData, slotA.Quantity);
        slotA.Clear();
        OnInventoryChanged?.Invoke();
        return true;
      }

      // 2. Nếu ô B có cùng loại vật phẩm và cho phép xếp chồng -> Gộp stack
      if (slotA.ItemData.Id == slotB.ItemData.Id && slotA.ItemData.IsStackable)
      {
        int remainder = slotB.AddQuantity(slotA.Quantity);
        if (remainder > 0)
        {
          slotA.SetItem(slotA.ItemData, remainder);
        }
        else
        {
          slotA.Clear();
        }
        OnInventoryChanged?.Invoke();
        return true;
      }

      // 3. Nếu là 2 vật phẩm khác nhau (hoặc ô B đã đầy stack) -> Hoán đổi (Swap)
      ItemData tempItem = slotB.ItemData;
      int tempQty = slotB.Quantity;

      slotB.SetItem(slotA.ItemData, slotA.Quantity);
      slotA.SetItem(tempItem, tempQty);

      OnInventoryChanged?.Invoke();
      return true;
    }

    /// <summary>
    /// Trang bị một món đồ từ ô cụ thể trong túi đồ (nếu đã có trang bị cũ thì hoán đổi về đúng ô đó)
    /// </summary>
    public bool EquipFromSlot(int slotIndex)
    {
      if (slotIndex < 0 || slotIndex >= slots.Count) return false;
      var slot = slots[slotIndex];
      if (slot.IsEmpty) return false;

      ItemData item = slot.ItemData;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor && item.ItemType != ItemType.Consumable) return false;

      if (item.ItemType == ItemType.Weapon)
      {
        ItemData oldWeapon = equippedWeapon;
        equippedWeapon = item;
        slot.Clear();

        if (oldWeapon != null)
        {
          slot.SetItem(oldWeapon, 1);
        }

        OnEquipmentChanged?.Invoke(ItemType.Weapon, equippedWeapon);
      }
      else if (item.ItemType == ItemType.Armor)
      {
        ItemData oldArmor = equippedArmor;
        equippedArmor = item;
        slot.Clear();

        if (oldArmor != null)
        {
          slot.SetItem(oldArmor, 1);
        }

        OnEquipmentChanged?.Invoke(ItemType.Armor, equippedArmor);
      }
      else if (item.ItemType == ItemType.Consumable)
      {
        ItemData oldUsable = equippedUsable;
        int oldUsableCount = equippedUsableCount;

        equippedUsable = item;
        equippedUsableCount = slot.Quantity;
        slot.Clear();

        if (oldUsable != null && oldUsableCount > 0)
        {
          slot.SetItem(oldUsable, oldUsableCount);
        }

        OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
        OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
      }

      OnInventoryChanged?.Invoke();
      return true;
    }

    /// <summary>
    /// Tháo trang bị (Vũ khí / Giáp) và đặt vào một ô cụ thể trong túi đồ.
    /// Nếu ô đích có trang bị cùng loại -> Hoán đổi (trang bị món trong ô đó và đưa món cũ vào ô đó).
    /// </summary>
    public bool UnequipToSlot(ItemType type, int targetSlotIndex)
    {
      if (targetSlotIndex < 0 || targetSlotIndex >= slots.Count) return false;
      ItemData toUnequip = null;
      int toUnequipCount = 1;

      switch (type)
      {
        case ItemType.Weapon:
          toUnequip = equippedWeapon;
          break;
        case ItemType.Armor:
          toUnequip = equippedArmor;
          break;
        case ItemType.Consumable:
          toUnequip = equippedUsable;
          toUnequipCount = equippedUsableCount;
          break;
      }

      if (toUnequip == null || toUnequipCount <= 0) return false;

      var targetSlot = slots[targetSlotIndex];

      // 1. Ô đích đang trống -> Đặt đồ tháo vào ô đó
      if (targetSlot.IsEmpty)
      {
        targetSlot.SetItem(toUnequip, toUnequipCount);

        if (type == ItemType.Weapon)
        {
          equippedWeapon = null;
          OnEquipmentChanged?.Invoke(ItemType.Weapon, null);
        }
        else if (type == ItemType.Armor)
        {
          equippedArmor = null;
          OnEquipmentChanged?.Invoke(ItemType.Armor, null);
        }
        else if (type == ItemType.Consumable)
        {
          equippedUsable = null;
          equippedUsableCount = 0;
          OnUsableChanged?.Invoke(null, 0);
          OnEquipmentChanged?.Invoke(ItemType.Consumable, null);
        }

        OnInventoryChanged?.Invoke();
        return true;
      }

      // 2. Nếu là Consumable và cùng loại item -> Gộp stack
      if (type == ItemType.Consumable && targetSlot.ItemData != null && targetSlot.ItemData.Id == toUnequip.Id && toUnequip.IsStackable)
      {
        int remainder = targetSlot.AddQuantity(toUnequipCount);
        if (remainder > 0)
        {
          equippedUsableCount = remainder;
        }
        else
        {
          equippedUsable = null;
          equippedUsableCount = 0;
        }

        OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
        OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
        OnInventoryChanged?.Invoke();
        return true;
      }

      // 3. Ô đích có một trang bị cùng loại -> Hoán đổi trang bị!
      if (targetSlot.ItemData != null && targetSlot.ItemData.ItemType == type)
      {
        ItemData newEquip = targetSlot.ItemData;
        int newQty = targetSlot.Quantity;
        targetSlot.SetItem(toUnequip, toUnequipCount);

        if (type == ItemType.Weapon)
        {
          equippedWeapon = newEquip;
          OnEquipmentChanged?.Invoke(ItemType.Weapon, equippedWeapon);
        }
        else if (type == ItemType.Armor)
        {
          equippedArmor = newEquip;
          OnEquipmentChanged?.Invoke(ItemType.Armor, equippedArmor);
        }
        else if (type == ItemType.Consumable)
        {
          equippedUsable = newEquip;
          equippedUsableCount = newQty;
          OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
          OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
        }

        OnInventoryChanged?.Invoke();
        return true;
      }

      // 4. Ô đích chứa vật phẩm không phù hợp (Potion, Material...) -> Báo không thể hoán đổi vào ô đó
      Debug.LogWarning($"[Inventory] Không thể tháo {type} vào ô chứa {targetSlot.ItemData?.ItemName}!");
      return false;
    }

    /// <summary>
    /// Trang bị một món đồ từ túi đồ
    /// </summary>
    public bool Equip(ItemData item)
    {
      if (item == null) return false;
      if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor && item.ItemType != ItemType.Consumable) return false;

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

      return EquipFromSlot(slotIdx);
    }

    /// <summary>
    /// Tháo trang bị đưa về túi đồ
    /// </summary>
    public bool Unequip(ItemType type)
    {
      ItemData toUnequip = null;
      int toUnequipCount = 1;

      switch (type)
      {
        case ItemType.Weapon:
          toUnequip = equippedWeapon;
          break;
        case ItemType.Armor:
          toUnequip = equippedArmor;
          break;
        case ItemType.Consumable:
          toUnequip = equippedUsable;
          toUnequipCount = equippedUsableCount;
          break;
      }

      if (toUnequip == null || toUnequipCount <= 0) return false;

      // Thử thêm vào túi đồ (tự động gộp stack nếu là consumable stackable hoặc tìm ô trống)
      if (AddItem(toUnequip, toUnequipCount, out int remainder, notifyPickup: false, allowRefillEquippedUsable: false))
      {
        if (remainder <= 0)
        {
          if (type == ItemType.Weapon)
          {
            equippedWeapon = null;
            OnEquipmentChanged?.Invoke(ItemType.Weapon, null);
          }
          else if (type == ItemType.Armor)
          {
            equippedArmor = null;
            OnEquipmentChanged?.Invoke(ItemType.Armor, null);
          }
          else if (type == ItemType.Consumable)
          {
            equippedUsable = null;
            equippedUsableCount = 0;
            OnUsableChanged?.Invoke(null, 0);
            OnEquipmentChanged?.Invoke(ItemType.Consumable, null);
          }

          OnInventoryChanged?.Invoke();
          return true;
        }
        else
        {
          if (type == ItemType.Consumable)
          {
            equippedUsableCount = remainder;
            OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
            OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
          }
          Debug.LogWarning("[Inventory] Túi đồ không đủ chỗ chứa toàn bộ!");
          return false;
        }
      }

      Debug.LogWarning("[Inventory] Túi đồ đã đầy, không thể tháo trang bị!");
      return false;
    }

    // =========================================================================
    // 3. SỬ DỤNG VẬT PHẨM TIÊU HAO (USE CONSUMABLES)
    // =========================================================================

    public bool UseItem(ItemData item)
    {
      if (item == null || item.ItemType != ItemType.Consumable) return false;

      if (IsConsumableOnCooldown)
      {
        Debug.Log("[Inventory] Bình thuốc đang trong thời gian hồi chiêu!");
        return false;
      }

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
      TriggerConsumableCooldown();
      return true;
    }

    /// <summary>
    /// Sử dụng bình thuốc đang trang bị ở ô UsableSlot (qua phím tắt C hoặc click UI)
    /// </summary>
    public bool UseEquippedUsable()
    {
      if (equippedUsable == null || equippedUsableCount <= 0)
      {
        Debug.Log("[Inventory] Chưa trang bị bình thuốc vào ô UsableSlot!");
        return false;
      }

      if (IsConsumableOnCooldown)
      {
        Debug.Log("[Inventory] Bình thuốc đang trong thời gian hồi chiêu!");
        return false;
      }

      if (cachedPlayerHealth == null)
      {
        CachePlayerReferences();
      }

      if (equippedUsable.HealAmount > 0f && cachedPlayerHealth != null)
      {
        if (cachedPlayerHealth.CurrentHealth >= cachedPlayerHealth.MaxHealth)
        {
          Debug.Log("[Inventory] Máu của bạn đã đầy!");
          return false;
        }
        cachedPlayerHealth.Heal(equippedUsable.HealAmount);
        Debug.Log($"[Inventory] Đã dùng {equippedUsable.ItemName}, hồi phục {equippedUsable.HealAmount} HP!");
      }

      equippedUsableCount--;
      if (equippedUsableCount <= 0)
      {
        equippedUsable = null;
        equippedUsableCount = 0;
      }

      TriggerConsumableCooldown();
      OnUsableChanged?.Invoke(equippedUsable, equippedUsableCount);
      OnEquipmentChanged?.Invoke(ItemType.Consumable, equippedUsable);
      OnUsableUsed?.Invoke();
      return true;
    }

    public void TriggerConsumableCooldown()
    {
      consumableCooldownTimer = consumableGlobalCooldown;
      OnConsumableCooldownUpdated?.Invoke(consumableCooldownTimer, consumableGlobalCooldown);
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
      if (item == null || count <= 0) return;

      if (cachedPlayerTransform == null)
      {
        CachePlayerReferences();
      }

      Vector3 spawnOrigin = Vector3.zero;
      Vector3 forward = Vector3.forward;

      if (cachedPlayerTransform != null)
      {
        forward = cachedPlayerTransform.forward;
        // Xuất phát từ tầm ngực người chơi (1.0m) hơi chếch về trước (0.4m)
        spawnOrigin = cachedPlayerTransform.position + Vector3.up * 1.0f + forward * 0.4f;
      }
      else
      {
        spawnOrigin = transform.position + Vector3.up * 1.0f + Vector3.forward * 0.4f;
      }

      // Xác định Prefab rơi đồ: Ưu tiên WorldModelPrefab
      GameObject prefabToSpawn = item.WorldModelPrefab;
      if (prefabToSpawn == null)
      {
        prefabToSpawn = Resources.Load<GameObject>("WorldItemPickup");
      }
#if UNITY_EDITOR
      if (prefabToSpawn == null)
      {
        prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RPG/WorldItemPickup.prefab");
      }
#endif

      if (prefabToSpawn == null)
      {
        Debug.LogWarning($"[Inventory] Không tìm thấy prefab rơi đồ cho {item.ItemName}!");
        return;
      }

      // Tính điểm tiếp đất an toàn ở phía trước mặt Player (quét cản tường và sàn)
      Vector3 targetGroundPos = CalculateDropLandingPosition(spawnOrigin, forward);

      // Sinh GameObject tại điểm xuất phát
      GameObject dropObj = Instantiate(prefabToSpawn, spawnOrigin, Quaternion.identity);

      // Đảm bảo có component WorldItemPickup
      var pickup = dropObj.GetComponent<WorldItemPickup>();
      if (pickup == null)
      {
        pickup = dropObj.AddComponent<WorldItemPickup>();
      }

      // Khởi tạo quỹ đạo búng nảy Parabol, hào quang Rarity và tương tác nhặt đồ
      pickup.Initialize(item, count, targetGroundPos, arcHeight: 0.95f, duration: 0.45f);
    }

    /// <summary>
    /// Tính toán điểm tiếp đất an toàn ở phía trước mặt Player (quét cản tường và sàn)
    /// </summary>
    private Vector3 CalculateDropLandingPosition(Vector3 spawnOrigin, Vector3 forward)
    {
      Vector3 playerPos = cachedPlayerTransform != null ? cachedPlayerTransform.position : transform.position;
      float dropDist = 1.5f;

      // 1. Quét cản tường ngang bằng Raycast để không rơi xuyên qua hoặc kẹt vào trong tường
      int defaultLayer = LayerMask.NameToLayer("Default");
      int obstacleLayer = LayerMask.NameToLayer("Obstacle");
      int wallMask = (1 << defaultLayer);
      if (obstacleLayer >= 0) wallMask |= (1 << obstacleLayer);

      Vector3 rayStart = playerPos + Vector3.up * 0.6f;
      if (Physics.Raycast(rayStart, forward, out RaycastHit wallHit, dropDist + 0.35f, wallMask, QueryTriggerInteraction.Ignore))
      {
        dropDist = Mathf.Max(0.5f, wallHit.distance - 0.35f);
      }

      // Thêm độ lệch ngang nhẹ ngẫu nhiên (±0.15m) để nếu vứt liên tiếp nhiều món thì không đè khít lên nhau
      Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
      float sideOffset = UnityEngine.Random.Range(-0.15f, 0.15f);
      Vector3 targetPos = playerPos + forward * dropDist + right * sideOffset;

      // 2. Raycast kiểm tra mặt sàn thực tế
      int navFloorLayer = LayerMask.NameToLayer("NavMessFloor");
      int terrainLayer = LayerMask.NameToLayer("Terrain");
      int groundMask = (1 << defaultLayer);
      if (navFloorLayer >= 0) groundMask |= (1 << navFloorLayer);
      if (terrainLayer >= 0) groundMask |= (1 << terrainLayer);

      RaycastHit[] hits = Physics.RaycastAll(targetPos + Vector3.up * 1.5f, Vector3.down, 4.0f, groundMask, QueryTriggerInteraction.Ignore);
      float bestFloorY = playerPos.y;
      bool foundGround = false;
      float closestDist = float.MaxValue;

      for (int i = 0; i < hits.Length; i++)
      {
        var h = hits[i];
        if (h.collider == null) continue;
        if (h.collider.CompareTag("Player") || h.collider.CompareTag("Enemy")) continue;
        if (h.normal.y >= 0.55f && h.distance < closestDist)
        {
          closestDist = h.distance;
          bestFloorY = h.point.y;
          foundGround = true;
        }
      }

      if (foundGround)
      {
        targetPos.y = bestFloorY + 0.12f;
      }
      else
      {
        targetPos.y = playerPos.y + 0.12f;
      }

      return targetPos;
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
