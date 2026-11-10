import os

# 1. Update WorldItemPickup.cs
w_path = r"Assets/Scripts/Inventory/WorldItemPickup.cs"
with open(w_path, "r", encoding="utf-8") as f:
    w_content = f.read()

# Replace Awake in WorldItemPickup.cs
target_w_awake = """      if (visualTransform == null)
      {
        visualTransform = transform.Find("Model") ?? transform;
      }
      baseVisualLocalPos = visualTransform.localPosition;

      if (rarityLight == null)
      {
        rarityLight = GetComponentInChildren<Light>();
      }"""

replacement_w_awake = """      if (visualTransform == null)
      {
        visualTransform = transform.Find("Model") ?? transform;
      }
      baseVisualLocalPos = visualTransform.localPosition;

      if (rarityLight == null)
      {
        rarityLight = GetComponentInChildren<Light>(true);
      }

      // Đảm bảo có trigger collider ở root nếu chưa có
      var rootCol = GetComponent<SphereCollider>();
      if (rootCol == null)
      {
        rootCol = gameObject.AddComponent<SphereCollider>();
        rootCol.isTrigger = true;
        rootCol.radius = pickupRadius;
      }

      // Vô hiệu hóa vật lý Rigidbody để chuyển động Parabol kinematically mượt mà 100%
      var rb = GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = true;
      }"""

assert target_w_awake.replace("\r\n", "\n") in w_content.replace("\r\n", "\n"), "target_w_awake not found"
w_norm = w_content.replace("\r\n", "\n").replace(target_w_awake.replace("\r\n", "\n"), replacement_w_awake.replace("\r\n", "\n"))

# Replace ApplyRarityVisuals in WorldItemPickup.cs
target_w_rarity = """    private void ApplyRarityVisuals()
    {
      if (itemData == null) return;

      Color rarityColor = itemData.Rarity.GetRarityColor();

      // Cập nhật màu và kích thước đèn hào quang theo phẩm cấp (thu nhỏ gọn gàng, không lóa mắt)
      if (rarityLight != null)
      {
        rarityLight.color = rarityColor;
        rarityLight.range = lightRange;
        rarityLight.intensity = (itemData.Rarity == ItemRarity.Legendary) ? legendaryLightIntensity : lightIntensity;
      }
    }"""

replacement_w_rarity = """    private void ApplyRarityVisuals()
    {
      if (itemData == null) return;

      Color rarityColor = itemData.Rarity.GetRarityColor();

      // Cập nhật màu và kích thước đèn hào quang theo phẩm cấp
      if (rarityLight == null)
      {
        rarityLight = GetComponentInChildren<Light>(true);
      }
      if (rarityLight == null)
      {
        GameObject glowObj = new GameObject("RarityGlow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        rarityLight = glowObj.AddComponent<Light>();
        rarityLight.type = LightType.Point;
        rarityLight.shadows = LightShadows.None;
      }

      if (rarityLight != null)
      {
        rarityLight.color = rarityColor;
        rarityLight.range = (itemData.Rarity >= ItemRarity.Epic) ? 1.8f : lightRange;
        float intensity = lightIntensity;
        switch (itemData.Rarity)
        {
          case ItemRarity.Common: intensity = 1.5f; break;
          case ItemRarity.Uncommon: intensity = 1.8f; break;
          case ItemRarity.Rare: intensity = 2.4f; break;
          case ItemRarity.Epic: intensity = 3.0f; break;
          case ItemRarity.Legendary: intensity = 3.8f; break;
        }
        rarityLight.intensity = intensity;
        rarityLight.enabled = true;
        if (!rarityLight.gameObject.activeSelf)
        {
          rarityLight.gameObject.SetActive(true);
        }
      }

      // Thiết lập mô hình 3D trực quan (hiển thị model kiếm nếu là vũ khí)
      SetupVisualModel();
    }

    private void SetupVisualModel()
    {
      if (itemData == null) return;

      if (visualTransform == null)
      {
        visualTransform = transform.Find("Model") ?? transform;
      }

      // 1. Nếu là Vũ khí và có WeaponVisualPrefab -> hiển thị chính model thanh kiếm 3D!
      if (itemData.WeaponVisualPrefab != null)
      {
        // Ẩn model rương mặc định (trunk_small_A)
        for (int i = 0; i < visualTransform.childCount; i++)
        {
          Transform child = visualTransform.GetChild(i);
          if (child.name != "SpawnedWeaponVisual")
          {
            child.gameObject.SetActive(false);
          }
        }

        // Tạo mô hình vũ khí nếu chưa có
        Transform existingWeapon = visualTransform.Find("SpawnedWeaponVisual");
        if (existingWeapon == null)
        {
          GameObject weaponObj = Instantiate(itemData.WeaponVisualPrefab, visualTransform);
          weaponObj.name = "SpawnedWeaponVisual";
          weaponObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
          // Xoay kiếm cắm nghiêng 40 độ tạo tư thế kiếm cắm đất xoay tròn
          weaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
          weaponObj.transform.localScale = Vector3.one * 0.8f;

          // Vô hiệu hóa Collider và Script trên vũ khí để không gây cản trở
          Collider[] cols = weaponObj.GetComponentsInChildren<Collider>(true);
          foreach (var c in cols) c.enabled = false;

          MonoBehaviour[] scripts = weaponObj.GetComponentsInChildren<MonoBehaviour>(true);
          foreach (var s in scripts) s.enabled = false;

          int itemLayer = LayerMask.NameToLayer("Item");
          if (itemLayer >= 0) SetLayerRecursively(weaponObj, itemLayer);
        }
        else
        {
          existingWeapon.gameObject.SetActive(true);
        }
      }
      else
      {
        // 2. Nếu là các vật phẩm khác (Bình máu, Giáp, Chìa khóa, Nguyên liệu...) -> hiển thị rương mini
        for (int i = 0; i < visualTransform.childCount; i++)
        {
          Transform child = visualTransform.GetChild(i);
          if (child.name == "SpawnedWeaponVisual")
          {
            child.gameObject.SetActive(false);
          }
          else
          {
            child.gameObject.SetActive(true);
          }
        }
      }
    }"""

assert target_w_rarity.replace("\r\n", "\n") in w_norm, "target_w_rarity not found"
w_norm = w_norm.replace(target_w_rarity.replace("\r\n", "\n"), replacement_w_rarity.replace("\r\n", "\n"))

# In Update: check !isLanded and UI open check
target_w_update = """    private void Update()
    {
      if (itemData == null) return;

      // 1. Hiệu ứng xoay tròn và nhấp nhô lơ lửng khi đã tiếp đất"""

replacement_w_update = """    private void Update()
    {
      if (itemData == null) return;

      // Không cho phép tương tác nhặt đồ khi chưa tiếp đất xong
      if (!isLanded) return;

      // 1. Hiệu ứng xoay tròn và nhấp nhô lơ lửng khi đã tiếp đất"""

assert target_w_update.replace("\r\n", "\n") in w_norm, "target_w_update not found"
w_norm = w_norm.replace(target_w_update.replace("\r\n", "\n"), replacement_w_update.replace("\r\n", "\n"))

target_w_press_f = """        // Bấm phím F để nhặt
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
          CollectItem();
        }"""

replacement_w_press_f = """        // Bấm phím F để nhặt
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
          // Đảm bảo không nhặt khi đang mở bảng Inventory UI
          if (!InfiniteLabyrinth.UI.InventoryUIController.IsOpen)
          {
            CollectItem();
          }
        }"""

assert target_w_press_f.replace("\r\n", "\n") in w_norm, "target_w_press_f not found"
w_norm = w_norm.replace(target_w_press_f.replace("\r\n", "\n"), replacement_w_press_f.replace("\r\n", "\n"))

with open(w_path, "w", encoding="utf-8") as f:
    f.write(w_norm)

print("WORLD_ITEM_PICKUP_UPDATED_SUCCESSFULLY")

# 2. Update InventoryManager.cs
m_path = r"Assets/Scripts/Inventory/InventoryManager.cs"
with open(m_path, "r", encoding="utf-8") as f:
    m_content = f.read()

target_m_spawn = """    private void SpawnWorldDrop(ItemData item, int count)
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
    }"""

replacement_m_spawn = """    private void SpawnWorldDrop(ItemData item, int count)
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
    }"""

assert target_m_spawn.replace("\r\n", "\n") in m_content.replace("\r\n", "\n"), "target_m_spawn not found"
m_norm = m_content.replace("\r\n", "\n").replace(target_m_spawn.replace("\r\n", "\n"), replacement_m_spawn.replace("\r\n", "\n"))

with open(m_path, "w", encoding="utf-8") as f:
    f.write(m_norm)

print("INVENTORY_MANAGER_DROP_UPDATED_SUCCESSFULLY")
