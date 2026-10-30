using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Progression;

/// <summary>
/// Central Developer Cheat & Admin Management Service.
/// Provides instant access to:
/// 1. Safe Teleport Matrix (Start Entrance, Boss Arena Gate, Key Shrine, Treasure Rooms, Mezzanine 8m)
/// 2. Real-time Procedural Dungeon ReGeneration in Play Mode
/// 3. Combat & Monster Manipulation (Kill Radius, Kill All, Freeze AI Dummies, 1v1 Spawner)
/// 4. Player God Mode & Cheats (Invulnerability, Infinite Stamina, Infinite Spirit, Full Restore, Super Speed)
/// 5. RPG Progression & Economy (Boss Key, Gold, EXP, Attribute Points, Potions, Legendary Weapons)
/// </summary>
public class AdminCheatManager : MonoBehaviour
{
  public static AdminCheatManager Instance { get; private set; }

  /// <summary>
  /// Static flag indicating whether the Cheat Console is open.
  /// Used by ThirdPersonCamera and PlayerController to release cursor and lock movement.
  /// </summary>
  public static bool IsOpen { get; set; } = false;

  /// <summary>
  /// God Mode flag. Player takes 0 damage from all damage sources when enabled.
  /// </summary>
  public static bool IsGodMode { get; set; } = false;

  /// <summary>
  /// Infinite Stamina flag. Sprinting and rolling do not consume stamina.
  /// </summary>
  public static bool IsInfiniteStamina { get; set; } = false;

  /// <summary>
  /// Infinite Spirit flag. 3 Spirit Stars (150/150) remain full to spam skills E and Q.
  /// </summary>
  public static bool IsInfiniteSpirit { get; set; } = false;

  /// <summary>
  /// Freeze Enemy AI flag. Turns all enemies into stationary training dummies.
  /// </summary>
  public static bool IsEnemyAIFrozen { get; set; } = false;

  [Header("Hotkey Settings")]
  [Tooltip("Listen for ~ (Backquote) or F1 to toggle the Admin Console")]
  public bool enableHotkeys = true;

  [Header("Item Data References (Auto-loaded from ScriptableObjects/Items)")]
  public ItemData dungeonKeyItem;
  public ItemData healthPotionItem;
  public ItemData dragonicSwordItem;
  public ItemData leatherArmorItem;
  public ItemData upgradeStoneItem;

  [Header("Monster Test Prefabs")]
  public GameObject skeletonWarriorPrefab;
  public GameObject skeletonMinionPrefab;
  public GameObject skeletonRoguePrefab;
  public GameObject skeletonMagePrefab;

  [Header("Runtime Cached References")]
  [SerializeField] private PlayerController cachedPlayerController;
  [SerializeField] private CharacterController cachedCharacterController;
  [SerializeField] private Health cachedPlayerHealth;
  [SerializeField] private PlayerStats cachedPlayerStats;
  [SerializeField] private Transform cachedPlayerTransform;

  // Events for UI and Notification Feed
  public event Action<bool> OnMenuToggled;
  public event Action<string> OnCheatExecuted;

  private int currentTreasureIndex = 0;
  private int currentMezzanineIndex = 0;
  private readonly float[] speedOptions = new float[] { 1.0f, 1.8f, 2.5f, 3.5f };
  private int currentSpeedIndex = 0;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    EnsurePlayerReferences();
    EnsureItemAndEnemyPrefabs();
  }

  private void Start()
  {
    EnsurePlayerReferences();
    EnsureItemAndEnemyPrefabs();
  }

  private void Update()
  {
    // Continuously maintain resource caps if cheats are active
    if (IsInfiniteStamina && cachedPlayerStats != null)
    {
      cachedPlayerStats.RestoreStamina(cachedPlayerStats.MaxStamina);
    }
    if (IsInfiniteSpirit && cachedPlayerStats != null)
    {
      cachedPlayerStats.GainSpirit(cachedPlayerStats.MaxSpirit);
    }

    if (!enableHotkeys) return;

    if (Keyboard.current != null)
    {
      // 1. Hotkey ~ (Backquote) or F1 to toggle Console
      if (Keyboard.current.backquoteKey.wasPressedThisFrame || Keyboard.current.f1Key.wasPressedThisFrame)
      {
        ToggleMenu();
      }
      // 2. Escape closes the menu if open
      else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        CloseMenu();
      }
    }
  }

  #region Menu Toggle & Cursor Management

  public void ToggleMenu()
  {
    if (IsOpen) CloseMenu();
    else OpenMenu();
  }

  public void OpenMenu()
  {
    IsOpen = true;
    EnsurePlayerReferences();

    // Release mouse cursor
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    // Temporarily lock movement so clicking UI won't swing sword or roll
    if (cachedPlayerController != null)
    {
      cachedPlayerController.LockMovement();
    }

    OnMenuToggled?.Invoke(true);
  }

  public void CloseMenu()
  {
    IsOpen = false;

    // Lock cursor back to gameplay
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    // Restore player movement
    if (cachedPlayerController != null)
    {
      cachedPlayerController.UnlockMovement();
    }

    OnMenuToggled?.Invoke(false);
  }

  #endregion

  #region Auto References

  public bool EnsurePlayerReferences()
  {
    if (cachedPlayerTransform != null && cachedPlayerController != null && cachedCharacterController != null && cachedPlayerStats != null)
      return true;

    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj == null)
    {
      var ctrl = FindFirstObjectByType<PlayerController>();
      if (ctrl != null) playerObj = ctrl.gameObject;
    }

    if (playerObj != null)
    {
      cachedPlayerTransform = playerObj.transform;
      cachedPlayerController = playerObj.GetComponent<PlayerController>();
      cachedCharacterController = playerObj.GetComponent<CharacterController>();
      cachedPlayerHealth = playerObj.GetComponent<Health>();
      cachedPlayerStats = playerObj.GetComponent<PlayerStats>();
      return true;
    }

    return false;
  }

  public void EnsureItemAndEnemyPrefabs()
  {
#if UNITY_EDITOR
    if (dungeonKeyItem == null)
      dungeonKeyItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Key_Dungeon.asset");
    if (healthPotionItem == null)
      healthPotionItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Potion_Health.asset");
    if (dragonicSwordItem == null)
      dragonicSwordItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Sword_Dragonic.asset");
    if (leatherArmorItem == null)
      leatherArmorItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Armor_Leather.asset");
    if (upgradeStoneItem == null)
      upgradeStoneItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Item_Stone_Upgrade.asset");

    if (skeletonWarriorPrefab == null)
      skeletonWarriorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");
    if (skeletonMinionPrefab == null)
      skeletonMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");
    if (skeletonRoguePrefab == null)
      skeletonRoguePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");
    if (skeletonMagePrefab == null)
      skeletonMagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");
#endif

    // Fallback to ProceduralEnemySpawner if available
    if (DungeonManager.Instance != null && DungeonManager.Instance.enemySpawner != null)
    {
      var spawner = DungeonManager.Instance.enemySpawner;
      if (skeletonWarriorPrefab == null) skeletonWarriorPrefab = spawner.skeletonWarriorPrefab;
      if (skeletonMinionPrefab == null) skeletonMinionPrefab = spawner.skeletonMinionPrefab;
      if (skeletonRoguePrefab == null) skeletonRoguePrefab = spawner.skeletonRoguePrefab;
      if (skeletonMagePrefab == null) skeletonMagePrefab = spawner.skeletonMagePrefab;
    }
  }

  #endregion

  #region 1. SAFE TELEPORT MATRIX

  /// <summary>
  /// Teleports player safely: samples closest NavMesh floor point, temporarily disables CharacterController, and applies rotation.
  /// </summary>
  public bool TeleportPlayer(Vector3 targetWorldPosition, string locationName = "Designated Location", Quaternion? targetRotation = null)
  {
    if (!EnsurePlayerReferences())
    {
      NotifyWarning("Player not found in Scene!");
      return false;
    }

    Vector3 finalPos = targetWorldPosition;
    if (NavMesh.SamplePosition(targetWorldPosition, out NavMeshHit hit, 8f, NavMesh.AllAreas))
    {
      finalPos = hit.position;
    }

    if (cachedCharacterController != null)
    {
      cachedCharacterController.enabled = false;
    }

    cachedPlayerTransform.position = finalPos + Vector3.up * 0.15f;
    if (targetRotation.HasValue)
    {
      cachedPlayerTransform.rotation = targetRotation.Value;
    }

    if (cachedCharacterController != null)
    {
      cachedCharacterController.enabled = true;
    }

    NotifySuccess($"[Teleport] Teleported to: {locationName} ({finalPos.x:F1}, {finalPos.y:F1}, {finalPos.z:F1})");
    return true;
  }

  /// <summary>
  /// Teleports player slightly behind the Start Doorway (inside the entrance lobby, facing through the door into the dungeon).
  /// </summary>
  public bool TeleportToStartRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Dungeon data not initialized!");
      return false;
    }

    // 1. If startDoorMarker is available, position player 4.5m behind the doorway facing into the door
    if (DungeonManager.Instance.generator != null && DungeonManager.Instance.generator.startDoorMarker != null)
    {
      Transform marker = DungeonManager.Instance.generator.startDoorMarker;
      Vector3 spawnBehindDoor = marker.position - marker.forward * 4.5f;
      return TeleportPlayer(spawnBehindDoor, "Start Room Entrance (Behind Door)", marker.rotation);
    }

    // 2. Fallback to startRoom bounds
    var data = DungeonManager.Instance.currentData;
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;
    if (data.startRoom != null)
    {
      Vector3 entrancePos = new Vector3(
        (data.startRoom.bounds.x + data.startRoom.bounds.width / 2f) * tSize,
        0.5f,
        (data.startRoom.bounds.y + 1.5f) * tSize
      );
      return TeleportPlayer(entrancePos, $"Start Room #{data.startRoom.id} (Entrance)", Quaternion.identity);
    }

    NotifyWarning("Could not determine Start Room location!");
    return false;
  }

  /// <summary>
  /// Teleports player slightly behind the Boss Gate (5.5m South on the Avenue, facing North towards the Grand Gate).
  /// </summary>
  public bool TeleportToBossRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Dungeon data not initialized!");
      return false;
    }

    var data = DungeonManager.Instance.currentData;
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;

    if (data.bossRoom != null)
    {
      // Boss Grand Gate is at South edge of boss arena:
      // X = avenue center, Z = bossRoom.bounds.y * tileSize.
      float gateX = (data.bossCorridor.width > 0)
        ? (data.bossCorridor.x + data.bossCorridor.width / 2f) * tSize
        : (data.bossRoom.bounds.x + data.bossRoom.bounds.width / 2f) * tSize;
      float gateZ = data.bossRoom.bounds.y * tSize;

      // Position 5.5m behind the gate on the Avenue, facing North towards the gate
      Vector3 spawnBehindGate = new Vector3(gateX, 0.5f, gateZ - 5.5f);
      return TeleportPlayer(spawnBehindGate, $"Boss Arena (Before Grand Gate)", Quaternion.identity);
    }

    NotifyWarning("Boss Room not found in current dungeon!");
    return false;
  }

  /// <summary>
  /// Teleports player right in front of the Key Shrine in the dead-end shrine room.
  /// </summary>
  public bool TeleportToKeyRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Dungeon data not initialized!");
      return false;
    }

    var data = DungeonManager.Instance.currentData;
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;

    RoomData keyRoom = data.keyRoom;
    if (keyRoom == null && data.roomDataList != null)
    {
      keyRoom = data.roomDataList.Find(r => r.type == RoomType.Shrine);
    }

    if (keyRoom != null)
    {
      Vector3 center = new Vector3(
        (keyRoom.bounds.x + keyRoom.bounds.width / 2f) * tSize,
        0.5f,
        (keyRoom.bounds.y + keyRoom.bounds.height / 2f) * tSize
      );
      return TeleportPlayer(center, $"Key Shrine Room #{keyRoom.id}");
    }

    NotifyWarning("Key Shrine Room not found in dungeon!");
    return false;
  }

  /// <summary>
  /// Cycles teleportation through each procedural Treasure Room.
  /// </summary>
  public bool TeleportToTreasureRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Dungeon data not initialized!");
      return false;
    }

    var data = DungeonManager.Instance.currentData;
    if (data.roomDataList == null || data.roomDataList.Count == 0)
    {
      NotifyWarning("Room data list is empty!");
      return false;
    }

    List<RoomData> treasureRooms = data.roomDataList.FindAll(r => r.type == RoomType.Treasure);
    if (treasureRooms.Count == 0)
    {
      NotifyWarning("No Treasure Rooms found in current layout!");
      return false;
    }

    currentTreasureIndex = (currentTreasureIndex + 1) % treasureRooms.Count;
    RoomData target = treasureRooms[currentTreasureIndex];
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;

    Vector3 center = new Vector3(
      (target.bounds.x + target.bounds.width / 2f) * tSize,
      0.5f,
      (target.bounds.y + target.bounds.height / 2f) * tSize
    );

    return TeleportPlayer(center, $"Treasure Room #{target.id} ({currentTreasureIndex + 1}/{treasureRooms.Count})");
  }

  /// <summary>
  /// Cycles teleportation onto the 2nd Floor Mezzanines (height 8m).
  /// </summary>
  public bool TeleportToMezzanine()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Dungeon data not initialized!");
      return false;
    }

    var data = DungeonManager.Instance.currentData;
    if (data.roomDataList == null || data.roomDataList.Count == 0)
    {
      NotifyWarning("Room data list is empty!");
      return false;
    }

    List<RoomData> mezRooms = data.roomDataList.FindAll(r => r.hasMezzanine);
    if (mezRooms.Count == 0)
    {
      NotifyWarning("No Mezzanine rooms found in current layout!");
      return false;
    }

    currentMezzanineIndex = (currentMezzanineIndex + 1) % mezRooms.Count;
    RoomData target = mezRooms[currentMezzanineIndex];
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;

    Vector3 mezPos = new Vector3(
      (target.mezzanineBounds.x + target.mezzanineBounds.width / 2f) * tSize,
      target.mezzanineHeight + 0.3f,
      (target.mezzanineBounds.y + target.mezzanineBounds.height / 2f) * tSize
    );

    return TeleportPlayer(mezPos, $"2nd Floor Mezzanine Room #{target.id} ({currentMezzanineIndex + 1}/{mezRooms.Count}) - Height {target.mezzanineHeight}m");
  }

  #endregion

  #region 2. RUNTIME DUNGEON REGENERATION

  public bool RegenerateDungeon(bool keepSeed = false)
  {
    if (DungeonManager.Instance == null)
    {
      NotifyWarning("DungeonManager not found in Scene!");
      return false;
    }

    // Đóng Admin Console để người chơi chiêm ngưỡng trọn vẹn màn hình Loading
    if (IsOpen)
    {
      CloseMenu();
    }

    NotifySuccess("Regenerating Dungeon with Loading Screen... Please wait!");

    // Ưu tiên nạp qua LoadingScreenManager với hiệu ứng chuyển cảnh mượt mà
    if (InfiniteLabyrinth.UI.LoadingScreenManager.Instance != null)
    {
      InfiniteLabyrinth.UI.LoadingScreenManager.Instance.RunInSceneLoading(
        "DungeonScene",
        1,
        (reportProgress) => RegenerateDungeonRoutine(keepSeed, reportProgress),
        () => NotifySuccess("[Success] New dungeon generated & NavMesh baked successfully!")
      );
      return true;
    }

    // Fallback đồng bộ nếu không có LoadingScreenManager
    ExecuteSyncRegeneration(keepSeed);
    return true;
  }

  private System.Collections.IEnumerator RegenerateDungeonRoutine(bool keepSeed, Action<float> reportProgress)
  {
    // 1. Di chuyển Player an toàn ra khu vực chờ trước khi xóa Mesh cũ
    if (DungeonManager.Instance.generator != null && DungeonManager.Instance.generator.startDoorMarker != null)
    {
      TeleportPlayer(DungeonManager.Instance.generator.startDoorMarker.position, "Holding Area");
    }

    // 2. Dọn sạch quái vật cũ
    if (DungeonManager.Instance.enemySpawner != null)
    {
      DungeonManager.Instance.enemySpawner.ClearEnemies();
    }

    // 3. Đổi Seed ngẫu nhiên nếu không giữ nguyên seed
    if (!keepSeed)
    {
      UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
    }

    reportProgress?.Invoke(0.08f);
    yield return null;

    // 4. Sinh hầm ngục theo từng chặng thông qua GenerateWorldRoutine
    yield return StartCoroutine(DungeonManager.Instance.GenerateWorldRoutine(p =>
    {
      reportProgress?.Invoke(Mathf.Lerp(0.08f, 0.95f, p));
    }));

    // 5. Làm mới bộ tối ưu ánh sáng
    var lightOptimizer = FindFirstObjectByType<DungeonLightOptimizer>();
    if (lightOptimizer != null)
    {
      lightOptimizer.RefreshLights();
    }

    // 6. Đưa Player trở lại phòng xuất phát an toàn
    TeleportToStartRoom();

    reportProgress?.Invoke(1.0f);
    yield return null;
  }

  private void ExecuteSyncRegeneration(bool keepSeed)
  {
    if (DungeonManager.Instance.generator != null && DungeonManager.Instance.generator.startDoorMarker != null)
    {
      TeleportPlayer(DungeonManager.Instance.generator.startDoorMarker.position, "Holding Area");
    }

    if (DungeonManager.Instance.enemySpawner != null)
    {
      DungeonManager.Instance.enemySpawner.ClearEnemies();
    }

    if (!keepSeed)
    {
      UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
    }

    DungeonManager.Instance.GenerateWorld();

    var lightOptimizer = FindFirstObjectByType<DungeonLightOptimizer>();
    if (lightOptimizer != null)
    {
      lightOptimizer.RefreshLights();
    }

    TeleportToStartRoom();
    NotifySuccess("[Success] New dungeon generated & NavMesh baked successfully!");
  }

  #endregion

  #region 3. COMBAT, FREEZE AI & MONSTER SPAWNING

  public int KillEnemiesInRadius(float radius = 25f)
  {
    if (!EnsurePlayerReferences())
    {
      NotifyWarning("Player not found!");
      return 0;
    }

    Vector3 center = cachedPlayerTransform.position;
    int killedCount = 0;

    Health[] allHealths = FindObjectsByType<Health>(FindObjectsSortMode.None);
    for (int i = 0; i < allHealths.Length; i++)
    {
      Health h = allHealths[i];
      if (h == null || h.IsDead) continue;

      if (h.gameObject.CompareTag("Player") || h.transform == cachedPlayerTransform) continue;

      float dist = Vector3.Distance(center, h.transform.position);
      if (dist <= radius)
      {
        h.TakeDamage(999999f, h.transform.position, Vector3.up, 0f, cachedPlayerTransform);
        killedCount++;
      }
    }

    NotifySuccess($"[Combat] Eliminated {killedCount} enemies within {radius}m radius!");
    return killedCount;
  }

  public int KillAllEnemies()
  {
    EnsurePlayerReferences();
    int killedCount = 0;

    Health[] allHealths = FindObjectsByType<Health>(FindObjectsSortMode.None);
    for (int i = 0; i < allHealths.Length; i++)
    {
      Health h = allHealths[i];
      if (h == null || h.IsDead) continue;

      if (h.gameObject.CompareTag("Player") || (cachedPlayerTransform != null && h.transform == cachedPlayerTransform))
        continue;

      h.TakeDamage(999999f, h.transform.position, Vector3.up, 0f, cachedPlayerTransform);
      killedCount++;
    }

    NotifySuccess($"[Combat] Wiped out all {killedCount} enemies across entire dungeon!");
    return killedCount;
  }

  public bool ToggleFreezeEnemies()
  {
    IsEnemyAIFrozen = !IsEnemyAIFrozen;

    // 1. Normal monsters (EnemyAIBase)
    EnemyAIBase[] enemies = FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None);
    for (int i = 0; i < enemies.Length; i++)
    {
      if (enemies[i] == null) continue;
      enemies[i].enabled = !IsEnemyAIFrozen;

      var agent = enemies[i].GetComponent<NavMeshAgent>();
      if (agent != null && agent.isOnNavMesh)
      {
        agent.isStopped = IsEnemyAIFrozen;
      }
    }

    // 2. Boss Ice Golem (IceGolemController)
    var bossCtrl = FindFirstObjectByType<IceGolemController>();
    if (bossCtrl != null)
    {
      bossCtrl.enabled = !IsEnemyAIFrozen;
      var bossAgent = bossCtrl.GetComponent<NavMeshAgent>();
      if (bossAgent != null && bossAgent.isOnNavMesh)
      {
        bossAgent.isStopped = IsEnemyAIFrozen;
      }
    }

    NotifySuccess(IsEnemyAIFrozen
      ? "❄️ [Enemy AI] FROZEN! Enemies are stationary training dummies."
      : "⚔️ [Enemy AI] RESUMED normal combat AI.");
    return IsEnemyAIFrozen;
  }

  public enum TestEnemyType { Warrior, Minion, Rogue, Mage }

  public GameObject SpawnTestEnemy(TestEnemyType type)
  {
    if (!EnsurePlayerReferences())
    {
      NotifyWarning("Player not found!");
      return null;
    }

    EnsureItemAndEnemyPrefabs();

    GameObject prefabToSpawn = null;
    switch (type)
    {
      case TestEnemyType.Warrior: prefabToSpawn = skeletonWarriorPrefab; break;
      case TestEnemyType.Minion: prefabToSpawn = skeletonMinionPrefab; break;
      case TestEnemyType.Rogue: prefabToSpawn = skeletonRoguePrefab; break;
      case TestEnemyType.Mage: prefabToSpawn = skeletonMagePrefab; break;
    }

    if (prefabToSpawn == null)
    {
      NotifyWarning($"Prefab not assigned for monster type: {type}!");
      return null;
    }

    Vector3 spawnPos = cachedPlayerTransform.position + cachedPlayerTransform.forward * 3.5f;
    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
    {
      spawnPos = hit.position;
    }

    Quaternion rot = Quaternion.LookRotation(-cachedPlayerTransform.forward);
    GameObject enemy = Instantiate(prefabToSpawn, spawnPos, rot);
    enemy.name = $"Test_{type}_{Time.frameCount}";

    if (DungeonManager.Instance != null && DungeonManager.Instance.enemySpawner != null)
    {
      DungeonManager.Instance.enemySpawner.activeEnemies.Add(enemy);
    }

    if (IsEnemyAIFrozen)
    {
      var ai = enemy.GetComponent<EnemyAIBase>();
      if (ai != null) ai.enabled = false;
      var agent = enemy.GetComponent<NavMeshAgent>();
      if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
    }

    NotifySuccess($"[Spawner] Spawned 1 {type} at {spawnPos:F1}!");
    return enemy;
  }

  #endregion

  #region 4. PLAYER CHEATS & RESOURCE RESTORATION

  public bool ToggleGodMode()
  {
    IsGodMode = !IsGodMode;
    EnsurePlayerReferences();

    if (IsGodMode && cachedPlayerHealth != null)
    {
      cachedPlayerHealth.Heal(cachedPlayerHealth.MaxHealth);
    }

    NotifySuccess(IsGodMode
      ? "🛡️ [God Mode] ENABLED! Immune to all damage."
      : "🛡️ [God Mode] DISABLED. Normal damage taken.");
    return IsGodMode;
  }

  public bool ToggleInfiniteStamina()
  {
    IsInfiniteStamina = !IsInfiniteStamina;
    EnsurePlayerReferences();

    if (IsInfiniteStamina && cachedPlayerStats != null)
    {
      cachedPlayerStats.RestoreStamina(cachedPlayerStats.MaxStamina);
    }

    NotifySuccess(IsInfiniteStamina
      ? "⚡ [Stamina] INFINITE STAMINA ENABLED!"
      : "⚡ [Stamina] Normal stamina consumption restored.");
    return IsInfiniteStamina;
  }

  public bool ToggleInfiniteSpirit()
  {
    IsInfiniteSpirit = !IsInfiniteSpirit;
    EnsurePlayerReferences();

    if (IsInfiniteSpirit && cachedPlayerStats != null)
    {
      cachedPlayerStats.GainSpirit(cachedPlayerStats.MaxSpirit);
    }

    NotifySuccess(IsInfiniteSpirit
      ? "⭐ [Spirit] INFINITE 3 SPIRIT STARS ENABLED! (150/150)"
      : "⭐ [Spirit] Normal spirit accumulation restored.");
    return IsInfiniteSpirit;
  }

  public void FullHealAndRestore()
  {
    EnsurePlayerReferences();

    if (cachedPlayerHealth != null)
    {
      cachedPlayerHealth.Heal(cachedPlayerHealth.MaxHealth);
    }

    if (cachedPlayerStats != null)
    {
      cachedPlayerStats.RestoreStamina(cachedPlayerStats.MaxStamina);
      cachedPlayerStats.GainSpirit(cachedPlayerStats.MaxSpirit);
    }

    NotifySuccess("❤️ [Restored] Full Health, Stamina and 3 Spirit Stars restored!");
  }

  public float CycleSuperSpeed()
  {
    EnsurePlayerReferences();
    if (cachedPlayerController == null) return 1.0f;

    currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
    float nextSpeed = speedOptions[currentSpeedIndex];
    cachedPlayerController.SpeedMultiplier = nextSpeed;

    NotifySuccess($"🏃 [Speed] Move speed multiplier set to: {nextSpeed:F1}x!");
    return nextSpeed;
  }

  public float GetCurrentSpeedMultiplier()
  {
    return (cachedPlayerController != null) ? cachedPlayerController.SpeedMultiplier : 1.0f;
  }

  #endregion

  #region 5. RPG PROGRESSION & INVENTORY CHEATS

  public bool AddBossKey()
  {
    EnsureItemAndEnemyPrefabs();
    if (dungeonKeyItem == null)
    {
      NotifyWarning("Item_Key_Dungeon.asset not found!");
      return false;
    }

    if (!InventoryManager.HasInstance)
    {
      NotifyWarning("InventoryManager not initialized!");
      return false;
    }

    bool success = InventoryManager.Instance.AddItem(dungeonKeyItem, 1);
    if (success)
    {
      NotifySuccess("🗝️ [Inventory] Added 1 Dungeon Boss Key to inventory!");
    }
    else
    {
      NotifyWarning("Inventory is full, could not add Boss Key!");
    }
    return success;
  }

  public void AddGold(int amount = 1000)
  {
    if (!InventoryManager.HasInstance)
    {
      NotifyWarning("InventoryManager not initialized!");
      return;
    }

    InventoryManager.Instance.AddGold(amount);
    NotifySuccess($"💰 [Economy] Added +{amount:N0} Gold! Total: {InventoryManager.Instance.CurrentGold:N0} Gold");
  }

  public void AddExp(int amount = 500)
  {
    if (PlayerProgression.Instance == null)
    {
      NotifyWarning("PlayerProgression not initialized!");
      return;
    }

    PlayerProgression.Instance.GainExp(amount);
    NotifySuccess($"📈 [Progression] Added +{amount} EXP! Current Level: {PlayerProgression.Instance.CurrentLevel}");
  }

  public void AddAttributePoints(int points = 10)
  {
    if (PlayerProgression.Instance == null)
    {
      NotifyWarning("PlayerProgression not initialized!");
      return;
    }

    PlayerProgression.Instance.AddUnallocatedPoints(points);
    NotifySuccess($"✨ [Attributes] Gained +{points} Attribute Points! Available: {PlayerProgression.Instance.UnallocatedPoints}");
  }

  public bool AddHealthPotions(int count = 5)
  {
    EnsureItemAndEnemyPrefabs();
    if (healthPotionItem == null || !InventoryManager.HasInstance) return false;

    bool res = InventoryManager.Instance.AddItem(healthPotionItem, count);
    if (res) NotifySuccess($"🧪 [Inventory] Added {count} Health Potions to inventory!");
    return res;
  }

  public bool AddLegendarySword()
  {
    EnsureItemAndEnemyPrefabs();
    if (dragonicSwordItem == null || !InventoryManager.HasInstance) return false;

    bool res = InventoryManager.Instance.AddItem(dragonicSwordItem, 1);
    if (res) NotifySuccess("🔥 [Inventory] Added Dragonic Sword (Legendary) to inventory!");
    return res;
  }

  public bool AddUpgradeStones(int count = 10)
  {
    EnsureItemAndEnemyPrefabs();
    if (upgradeStoneItem == null || !InventoryManager.HasInstance) return false;

    bool res = InventoryManager.Instance.AddItem(upgradeStoneItem, count);
    if (res) NotifySuccess($"💎 [Inventory] Added {count} Upgrade Stones to inventory!");
    return res;
  }

  #endregion

  #region Feedback Notifications

  private void NotifySuccess(string message)
  {
    Debug.Log($"<color=#00FF88>[Admin Manager]</color> {message}");
    OnCheatExecuted?.Invoke(message);
  }

  private void NotifyWarning(string message)
  {
    Debug.LogWarning($"<color=#FFAA00>[Admin Manager]</color> {message}");
    OnCheatExecuted?.Invoke($"⚠️ {message}");
  }

  #endregion
}
