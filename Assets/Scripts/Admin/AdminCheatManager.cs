using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Progression;

/// <summary>
/// Central Developer Cheat & Admin Management Service.
/// Provides instant access to:
/// 1. Safe Teleport Matrix (Dynamic LandScene Village Hub & DungeonScene PCG Landmarks)
/// 2. Scene Controls & Progression (Day System, Fast Travel, Map Reveal, Boss Gate Unlock, ReGen Dungeon)
/// 3. Combat, Dummies & Monster Manipulation (10,000 HP Damage Dummy, Freeze AI, Kill Radius, 1v1 Spawner)
/// 4. Player God Mode & Cheats (God Mode, Inf Stamina, Inf Spirit, No Cooldown, Full Restore, Super Speed)
/// 5. Creative Mode (All 51 Game Items, Gold, EXP, Attribute Points)
/// </summary>
public class AdminCheatManager : MonoBehaviour
{
  public static AdminCheatManager Instance { get; private set; }

  /// <summary>
  /// Ensures an active instance of AdminCheatManager exists in the scene.
  /// Self-healing fallback if the canvas is dropped into any scene without the manager.
  /// </summary>
  public static AdminCheatManager EnsureInstance()
  {
    if (Instance != null) return Instance;

    var existing = FindFirstObjectByType<AdminCheatManager>();
    if (existing != null)
    {
      Instance = existing;
      return Instance;
    }

    GameObject obj = new GameObject("AdminCheatManager");
    Instance = obj.AddComponent<AdminCheatManager>();
    if (Application.isPlaying && obj.transform.parent == null)
    {
      DontDestroyOnLoad(obj);
    }
    return Instance;
  }

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
  /// Infinite Spirit flag. 4 Spirit Stars (200/200) remain full to spam skills E, Q and Ult R.
  /// </summary>
  public static bool IsInfiniteSpirit { get; set; } = false;

  /// <summary>
  /// No Cooldown flag. Skills E, Q, R and quick-slot items have 0 cooldown and can be used continuously.
  /// </summary>
  public static bool IsNoCooldown { get; set; } = false;

  /// <summary>
  /// Freeze Enemy AI flag. Turns all enemies into stationary training dummies.
  /// </summary>
  public static bool IsEnemyAIFrozen { get; set; } = false;

  [Header("Hotkey Settings")]
  [Tooltip("Listen for ~ (Backquote), F1, or F2 to toggle the Admin Console")]
  public bool enableHotkeys = true;

  [Header("All 51 Game Items Database (Creative Mode)")]
  public List<ItemData> allGameItems = new List<ItemData>();

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

  // Active spawned damage test dummies
  private readonly List<GameObject> activeDummies = new List<GameObject>();

  // Events for UI and Notification Feed
  public event Action<bool> OnMenuToggled;
  public event Action<string> OnCheatExecuted;

  private int currentTreasureIndex = 0;
  private int currentMezzanineIndex = 0;
  private readonly float[] speedOptions = new float[] { 1.0f, 1.8f, 2.5f, 3.5f };
  private int currentSpeedIndex = 0;

  public bool IsLandScene
  {
    get
    {
      string sceneName = SceneManager.GetActiveScene().name;
      return sceneName == "LandScene";
    }
  }

  public bool IsDungeonScene => !IsLandScene;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    if (Application.isPlaying && transform.parent == null)
    {
      DontDestroyOnLoad(gameObject);
    }
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
      // 1. Hotkey ~ (Backquote), F1, or F2 to toggle Console
      if (Keyboard.current.backquoteKey.wasPressedThisFrame ||
          Keyboard.current.f1Key.wasPressedThisFrame ||
          Keyboard.current.f2Key.wasPressedThisFrame)
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

  private static int lastToggleFrame = -1;

  public void ToggleMenu()
  {
    if (Time.frameCount == lastToggleFrame) return;
    lastToggleFrame = Time.frameCount;

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

  #region Auto References & Database Population

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
    // 1. Load monster test prefabs
    if (skeletonWarriorPrefab == null)
      skeletonWarriorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");
    if (skeletonMinionPrefab == null)
      skeletonMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");
    if (skeletonRoguePrefab == null)
      skeletonRoguePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");
    if (skeletonMagePrefab == null)
      skeletonMagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");

    // 2. Load all 51 items into Creative Mode database
    if (allGameItems == null || allGameItems.Count == 0)
    {
      allGameItems = new List<ItemData>();
      string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects/Items" });
      foreach (var guid in guids)
      {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null && !allGameItems.Contains(item))
        {
          allGameItems.Add(item);
        }
      }
      allGameItems.Sort((a, b) =>
      {
        int typeComp = a.ItemType.CompareTo(b.ItemType);
        if (typeComp != 0) return typeComp;
        int rareComp = b.Rarity.CompareTo(a.Rarity);
        if (rareComp != 0) return rareComp;
        return string.Compare(a.ItemName, b.ItemName, StringComparison.Ordinal);
      });
    }
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

  #region 1. SAFE TELEPORT MATRIX (LANDSCENE & DUNGEONSCENE)

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

  // ==========================================
  // A. LANDSCENE (VILLAGE HAVEN) TELEPORTS
  // ==========================================

  public bool TeleportToVillageBell()
  {
    GameObject bellObj = GameObject.Find("bell") ?? GameObject.Find("Bell");
    if (bellObj != null)
    {
      return TeleportPlayer(bellObj.transform.position + Vector3.back * 2.5f, "Village Bell (Central Respawn)");
    }
    NotifyWarning("Village Bell not found in current scene!");
    return false;
  }

  public bool TeleportToVillagePortal()
  {
    GameObject portalObj = GameObject.Find("Portal") ?? GameObject.Find("portal");
    if (portalObj == null)
    {
      var pc = FindFirstObjectByType<PortalController>();
      if (pc != null) portalObj = pc.gameObject;
    }
    if (portalObj != null)
    {
      return TeleportPlayer(portalObj.transform.position + Vector3.back * 3.5f, "Village Dungeon Portal");
    }
    NotifyWarning("Village Portal not found in current scene!");
    return false;
  }

  public bool TeleportToEquipmentShop()
  {
    GameObject shopObj = GameObject.Find("EquipmentShop") ?? GameObject.Find("weapons_shop");
    if (shopObj != null)
    {
      return TeleportPlayer(shopObj.transform.position + Vector3.back * 3.5f, "Equipment & Armaments Shop");
    }
    NotifyWarning("Equipment Shop not found in current scene!");
    return false;
  }

  public bool TeleportToSuppliesShop()
  {
    GameObject shopObj = GameObject.Find("SuppliesShop") ?? GameObject.Find("potion_shop");
    if (shopObj != null)
    {
      return TeleportPlayer(shopObj.transform.position + Vector3.back * 3.5f, "Supplies & Potions Shop");
    }
    NotifyWarning("Supplies Shop not found in current scene!");
    return false;
  }

  public bool TeleportToBlacksmith()
  {
    GameObject smithyObj = GameObject.Find("BlackSmith") ?? GameObject.Find("smithy");
    if (smithyObj != null)
    {
      return TeleportPlayer(smithyObj.transform.position + Vector3.forward * 4.5f, "Village Blacksmith (Forge & Upgrade)");
    }
    NotifyWarning("Blacksmith not found in current scene!");
    return false;
  }

  public bool TeleportToWindmill()
  {
    GameObject windmillObj = GameObject.Find("windmill") ?? GameObject.Find("Windmill");
    if (windmillObj != null)
    {
      return TeleportPlayer(windmillObj.transform.position + Vector3.up * 7.5f + Vector3.forward * 3.5f, "Windmill Lookout (Scenic)");
    }
    NotifyWarning("Windmill not found in current scene!");
    return false;
  }

  // ==========================================
  // B. DUNGEONSCENE PCG TELEPORTS
  // ==========================================

  public bool TeleportToStartRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      return TeleportToVillageBell();
    }

    if (DungeonManager.Instance.generator != null && DungeonManager.Instance.generator.startDoorMarker != null)
    {
      Transform marker = DungeonManager.Instance.generator.startDoorMarker;
      Vector3 spawnBehindDoor = marker.position - marker.forward * 4.5f;
      return TeleportPlayer(spawnBehindDoor, "Start Room Entrance (Behind Door)", marker.rotation);
    }

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

  public bool TeleportToBossRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      return TeleportToVillagePortal();
    }

    var data = DungeonManager.Instance.currentData;
    float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;

    if (data.bossRoom != null)
    {
      float gateX = (data.bossCorridor.width > 0)
        ? (data.bossCorridor.x + data.bossCorridor.width / 2f) * tSize
        : (data.bossRoom.bounds.x + data.bossRoom.bounds.width / 2f) * tSize;
      float gateZ = data.bossRoom.bounds.y * tSize;

      Vector3 spawnBehindGate = new Vector3(gateX, 0.5f, gateZ - 5.5f);
      return TeleportPlayer(spawnBehindGate, "Boss Arena (Before Grand Gate)", Quaternion.identity);
    }

    NotifyWarning("Boss Room not found in current dungeon!");
    return false;
  }

  public bool TeleportInsideBossArena()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      NotifyWarning("Boss Room not found (Dungeon data not initialized)!");
      return false;
    }

    var data = DungeonManager.Instance.currentData;
    if (data.bossRoom != null)
    {
      float tSize = (DungeonManager.Instance.generator != null) ? DungeonManager.Instance.generator.tileSize : 2f;
      Vector3 center = new Vector3(
        (data.bossRoom.bounds.x + data.bossRoom.bounds.width / 2f) * tSize,
        0.5f,
        (data.bossRoom.bounds.y + data.bossRoom.bounds.height / 2f) * tSize
      );
      return TeleportPlayer(center, "Inside Boss Arena (Center)");
    }

    NotifyWarning("Boss Room not found in current dungeon!");
    return false;
  }

  public bool TeleportToKeyRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      return TeleportToEquipmentShop();
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

  public bool TeleportToTreasureRoom()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      return TeleportToBlacksmith();
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

  public bool TeleportToMezzanine()
  {
    if (DungeonManager.Instance == null || DungeonManager.Instance.currentData == null)
    {
      return TeleportToWindmill();
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

  #region 2. SCENE CONTROLS & PROGRESSION CHEATS

  public void FastTravelToDungeon()
  {
    if (IsOpen) CloseMenu();
    NotifySuccess("Traveling to Dungeon Floor 1...");
    if (InfiniteLabyrinth.UI.LoadingScreenManager.HasInstance)
    {
      InfiniteLabyrinth.UI.LoadingScreenManager.Instance.LoadScene("DungeonScene", 1);
    }
    else
    {
      SceneManager.LoadScene("DungeonScene");
    }
  }

  public void FastTravelToVillage()
  {
    if (IsOpen) CloseMenu();
    NotifySuccess("Returning to Haven Village...");
    if (GameManager.HasInstance)
    {
      GameManager.Instance.RespawnAtVillage();
    }
    else if (InfiniteLabyrinth.UI.LoadingScreenManager.HasInstance)
    {
      InfiniteLabyrinth.UI.LoadingScreenManager.Instance.LoadScene("LandScene", 1);
    }
    else
    {
      SceneManager.LoadScene("LandScene");
    }
  }

  public void AdvanceDay()
  {
    if (GameManager.HasInstance)
    {
      GameManager.Instance.AdvanceDay();
      NotifySuccess($"[Day System] Advanced to Day {GameManager.Instance.CurrentDay}!");
    }
    else
    {
      NotifyWarning("GameManager not found in scene!");
    }
  }

  public void ResetDayTo1()
  {
    if (GameManager.HasInstance)
    {
      GameManager.Instance.SetDay(1);
      NotifySuccess("[Day System] Reset Day counter back to Day 1!");
    }
    else
    {
      NotifyWarning("GameManager not found in scene!");
    }
  }

  public void RevealDungeonMap()
  {
    if (InfiniteLabyrinth.UI.DungeonMapManager.Instance != null)
    {
      InfiniteLabyrinth.UI.DungeonMapManager.Instance.RevealAll();
      NotifySuccess("[Map] Entire Dungeon map revealed! Fog of war cleared.");
    }
    else
    {
      NotifyWarning("DungeonMapManager not found in scene!");
    }
  }

  public void UnlockBossGate()
  {
    if (InfiniteLabyrinth.Interaction.BossGateController.Instance != null)
    {
      InfiniteLabyrinth.Interaction.BossGateController.Instance.ForceOpenGate();
      NotifySuccess("[Boss Gate] Force unlocked Grand Gate into Boss Arena!");
    }
    else
    {
      // Fallback: Add Boss Key to inventory
      ItemData keyItem = allGameItems.Find(i => i.Id == "dungeon_key" || i.ItemType == ItemType.Key);
      if (keyItem != null && InventoryManager.HasInstance)
      {
        InventoryManager.Instance.AddItem(keyItem, 1);
        NotifySuccess("[Boss Gate] Added Boss Key to inventory!");
      }
      else
      {
        NotifyWarning("BossGateController not found in current scene!");
      }
    }
  }

  public bool RegenerateDungeon(bool keepSeed = false)
  {
    if (DungeonManager.Instance == null)
    {
      NotifyWarning("DungeonManager not found in Scene!");
      return false;
    }

    if (IsOpen) CloseMenu();
    NotifySuccess("Regenerating Dungeon with Loading Screen... Please wait!");

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

    ExecuteSyncRegeneration(keepSeed);
    return true;
  }

  private IEnumerator RegenerateDungeonRoutine(bool keepSeed, Action<float> reportProgress)
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

    reportProgress?.Invoke(0.08f);
    yield return null;

    yield return StartCoroutine(DungeonManager.Instance.GenerateWorldRoutine(p =>
    {
      reportProgress?.Invoke(Mathf.Lerp(0.08f, 0.95f, p));
    }));

    var lightOptimizer = FindFirstObjectByType<DungeonLightOptimizer>();
    if (lightOptimizer != null)
    {
      lightOptimizer.RefreshLights();
    }

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

  #region 3. COMBAT, DUMMY TARGET & MONSTER CHEATS

  /// <summary>
  /// Spawns a stationary, zero-AI, 10,000 HP training dummy right in front of the player.
  /// Perfect for combo testing, DPS calculation, and damage number inspection.
  /// </summary>
  public GameObject SpawnDamageDummy(float maxHp = 10000f)
  {
    if (!EnsurePlayerReferences())
    {
      NotifyWarning("Player not found!");
      return null;
    }

    EnsureItemAndEnemyPrefabs();
    GameObject prefabToSpawn = skeletonMinionPrefab != null ? skeletonMinionPrefab : skeletonWarriorPrefab;
    if (prefabToSpawn == null)
    {
      NotifyWarning("Monster prefab not assigned!");
      return null;
    }

    Vector3 spawnPos = cachedPlayerTransform.position + cachedPlayerTransform.forward * 3.5f;
    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
    {
      spawnPos = hit.position;
    }

    Quaternion rot = Quaternion.LookRotation(-cachedPlayerTransform.forward);
    GameObject dummy = Instantiate(prefabToSpawn, spawnPos, rot);
    dummy.name = $"Dummy_Target_10k_{Time.frameCount}";

    // 1. Disable EnemyAIBase so it has zero behavior
    var ai = dummy.GetComponent<EnemyAIBase>();
    if (ai != null) ai.enabled = false;

    // 2. Stop and freeze NavMeshAgent
    var agent = dummy.GetComponent<NavMeshAgent>();
    if (agent != null && agent.isOnNavMesh)
    {
      agent.isStopped = true;
      agent.updatePosition = false;
      agent.updateRotation = false;
    }

    // 3. Configure Health to 10,000 HP with 0 DEF for clean testing
    var health = dummy.GetComponent<Health>();
    if (health != null)
    {
      health.SetBaseDefense(0f);
      health.SetCustomHealth(maxHp);
    }

    if (DungeonManager.Instance != null && DungeonManager.Instance.enemySpawner != null)
    {
      DungeonManager.Instance.enemySpawner.activeEnemies.Add(dummy);
    }

    activeDummies.Add(dummy);
    NotifySuccess($"[Dummy] Spawned {maxHp:N0} HP Training Dummy! (No AI, 0 DEF, Pure DMG Test)");
    return dummy;
  }

  public void ClearAllDummies()
  {
    int count = 0;
    for (int i = activeDummies.Count - 1; i >= 0; i--)
    {
      if (activeDummies[i] != null)
      {
        Destroy(activeDummies[i]);
        count++;
      }
    }
    activeDummies.Clear();
    NotifySuccess($"[Dummy] Cleared {count} Training Dummies!");
  }

  public void HealAllDummies(float hp = 10000f)
  {
    int count = 0;
    for (int i = 0; i < activeDummies.Count; i++)
    {
      if (activeDummies[i] != null)
      {
        var h = activeDummies[i].GetComponent<Health>();
        if (h != null)
        {
          h.SetCustomHealth(hp);
          count++;
        }
      }
    }
    NotifySuccess($"[Dummy] Restored {count} Dummies back to {hp:N0} HP!");
  }

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
      ? "[Enemy AI] FROZEN! Enemies are stationary training dummies."
      : "[Enemy AI] RESUMED normal combat AI.");
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
      ? "[God Mode] ENABLED! Immune to all damage."
      : "[God Mode] DISABLED. Normal damage taken.");
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
      ? "[Stamina] INFINITE STAMINA ENABLED!"
      : "[Stamina] Normal stamina consumption restored.");
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
      ? "[Spirit] INFINITE 4 SPIRIT STARS ENABLED! (200/200)"
      : "[Spirit] Normal spirit accumulation restored.");
    return IsInfiniteSpirit;
  }

  public bool ToggleNoCooldown()
  {
    IsNoCooldown = !IsNoCooldown;
    NotifySuccess(IsNoCooldown
      ? "[No Cooldown] NO COOLDOWN ENABLED! Skills E, Q, R & Usables spam ready."
      : "[No Cooldown] Normal cooldowns restored.");
    return IsNoCooldown;
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

    NotifySuccess("[Restored] Full Health, Stamina and 4 Spirit Stars restored!");
  }

  public float CycleSuperSpeed()
  {
    EnsurePlayerReferences();
    if (cachedPlayerController == null) return 1.0f;

    currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
    float nextSpeed = speedOptions[currentSpeedIndex];
    cachedPlayerController.SpeedMultiplier = nextSpeed;

    NotifySuccess($"[Speed] Move speed multiplier set to: {nextSpeed:F1}x!");
    return nextSpeed;
  }

  public float GetCurrentSpeedMultiplier()
  {
    return (cachedPlayerController != null) ? cachedPlayerController.SpeedMultiplier : 1.0f;
  }

  #endregion

  #region 5. CREATIVE MODE & PROGRESSION CHEATS

  public bool GiveItem(ItemData item, int count = 1)
  {
    if (item == null) return false;
    if (!InventoryManager.HasInstance)
    {
      NotifyWarning("InventoryManager not initialized!");
      return false;
    }

    bool res = InventoryManager.Instance.AddItem(item, count);
    if (res)
    {
      NotifySuccess($"[Creative] Added {item.ItemName} x{count} to Inventory!");
    }
    else
    {
      NotifyWarning("Inventory is full, could not add item!");
    }
    return res;
  }

  public void AddGold(int amount = 1000)
  {
    if (!InventoryManager.HasInstance)
    {
      NotifyWarning("InventoryManager not initialized!");
      return;
    }

    InventoryManager.Instance.AddGold(amount);
    NotifySuccess($"[Economy] Added +{amount:N0} Gold! Total: {InventoryManager.Instance.CurrentGold:N0} Gold");
  }

  public void AddExp(int amount = 500)
  {
    if (PlayerProgression.Instance == null)
    {
      NotifyWarning("PlayerProgression not initialized!");
      return;
    }

    PlayerProgression.Instance.GainExp(amount);
    NotifySuccess($"[Progression] Added +{amount} EXP! Current Level: {PlayerProgression.Instance.CurrentLevel}");
  }

  public void AddAttributePoints(int points = 10)
  {
    if (PlayerProgression.Instance == null)
    {
      NotifyWarning("PlayerProgression not initialized!");
      return;
    }

    PlayerProgression.Instance.AddUnallocatedPoints(points);
    NotifySuccess($"[Attributes] Gained +{points} Attribute Points! Available: {PlayerProgression.Instance.UnallocatedPoints}");
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
    OnCheatExecuted?.Invoke($"[Notice] {message}");
  }

  #endregion
}