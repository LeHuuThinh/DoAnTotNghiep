using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý hiệu ứng chém kiếm (Sword Slash Trail & Arc VFX) cho Player.
/// Cho phép cấu hình vị trí, góc xoay (Euler Angles) và tỷ lệ (Scale) riêng biệt
/// cho từng đòn chém trong combo mà không bị méo mó theo chuyển động cổ tay.
/// </summary>
public class PlayerSlashVFX : MonoBehaviour
{
  public enum SlashAttackType
  {
    Attack1,
    Attack2,
    Attack3,
    Charge1,
    Charge2,
    Counter
  }

  [System.Serializable]
  public class SlashVfxConfig
  {
    [Tooltip("Tên định danh đòn đánh")]
    public string attackName = "Attack";

    [Tooltip("Ghi đè Prefab riêng cho đòn này (nếu để trống sẽ dùng defaultSlashVfxPrefab)")]
    public GameObject customPrefab;

    [Tooltip("Độ lệch vị trí so với Player (mặc định ngang ngực/eo và nhô về phía trước)")]
    public Vector3 positionOffset = new Vector3(0.15f, 1.15f, 0.6f);

    [Tooltip("Góc xoay của mặt phẳng chém (Euler Angles)")]
    public Vector3 rotationOffset = new Vector3(-176.5f, -92.1f, -92.1f);

    [Tooltip("Kích thước đòn chém (Scale)")]
    public Vector3 scale = Vector3.one;

    [Tooltip("Thời gian tồn tại của VFX trước khi tự hủy (giây)")]
    [Range(0.2f, 2.0f)]
    public float lifetime = 0.8f;

    [Tooltip("Gắn VFX làm con của Player trong lúc chém (vệt di chuyển theo bước chân nhân vật)")]
    public bool parentToPlayer = false;
  }

  [Header("Default Prefab Settings")]
  [Tooltip("Prefab Slash VFX mặc định (Ví dụ: Slash VFX màu trắng)")]
  [SerializeField] private GameObject defaultSlashVfxPrefab;

  [Tooltip("Transform gốc làm tâm tham chiếu (mặc định là Player Root)")]
  [SerializeField] private Transform slashSpawnRoot;

  [Header("Combo Slash Configurations")]
  [Tooltip("Đòn 1: Chém vát chéo từ vai phải xuống hông trái")]
  [SerializeField]
  private SlashVfxConfig attack1Config = new SlashVfxConfig
  {
    attackName = "Attack 1 (Diagonal Down-Left)",
    positionOffset = new Vector3(0.18f, 1.18f, 0.62f),
    rotationOffset = new Vector3(-176.57f, -92.12f, -92.12f),
    scale = new Vector3(1.0f, 1.0f, 1.0f),
    lifetime = 0.8f,
    parentToPlayer = false
  };

  [Tooltip("Đòn 2: Chém vát ngược từ hông trái lên vai phải")]
  [SerializeField]
  private SlashVfxConfig attack2Config = new SlashVfxConfig
  {
    attackName = "Attack 2 (Slash Up-Right)",
    positionOffset = new Vector3(-0.12f, 1.15f, 0.62f),
    rotationOffset = new Vector3(-85.89f, -183.54f, 0.65f),
    scale = new Vector3(1.05f, 1.05f, 1.05f),
    lifetime = 0.8f,
    parentToPlayer = false
  };

  [Tooltip("Đòn 3: Chém quét ngang phẳng uy lực kết thúc combo")]
  [SerializeField]
  private SlashVfxConfig attack3Config = new SlashVfxConfig
  {
    attackName = "Attack 3 (Horizontal Sweep Finisher)",
    positionOffset = new Vector3(0.0f, 1.10f, 0.68f),
    rotationOffset = new Vector3(-176.57f, -92.12f, 0.09f),
    scale = new Vector3(1.25f, 1.25f, 1.25f),
    lifetime = 0.85f,
    parentToPlayer = false
  };

  [Tooltip("Đòn Gồng 1: Nhát chém lấy đà mở màn của Charge Attack")]
  [SerializeField]
  private SlashVfxConfig chargeHit1Config = new SlashVfxConfig
  {
    attackName = "Charge Hit 1 (Windup Slash)",
    positionOffset = new Vector3(0.15f, 1.25f, 0.6f),
    rotationOffset = new Vector3(-176.57f, -92.12f, -45.0f),
    scale = new Vector3(1.1f, 1.1f, 1.1f),
    lifetime = 0.8f,
    parentToPlayer = false
  };

  [Tooltip("Đòn Gồng 2: Nhát bổ thẳng đứng từ trên trời cắm xuống đất")]
  [SerializeField]
  private SlashVfxConfig chargeHit2Config = new SlashVfxConfig
  {
    attackName = "Charge Hit 2 (Vertical Slam)",
    positionOffset = new Vector3(0.0f, 0.95f, 0.72f),
    rotationOffset = new Vector3(-90.0f, -92.12f, -90.0f),
    scale = new Vector3(1.35f, 1.35f, 1.35f),
    lifetime = 0.9f,
    parentToPlayer = false
  };

  [Tooltip("Đòn Phản Công (Counter Attack): Cú chém bùng nổ cực mạnh sau khi Parry chuẩn")]
  [SerializeField]
  private SlashVfxConfig counterAttackConfig = new SlashVfxConfig
  {
    attackName = "Counter Attack (Explosive Sweep)",
    positionOffset = new Vector3(0.0f, 1.15f, 0.7f),
    rotationOffset = new Vector3(-176.57f, -92.12f, -15.0f),
    scale = new Vector3(1.5f, 1.5f, 1.5f),
    lifetime = 1.0f,
    parentToPlayer = false
  };

  [Header("Gizmos Debug Settings")]
  [SerializeField] private bool showGizmosInEditor = true;
  [SerializeField] private SlashAttackType gizmoPreviewAttack = SlashAttackType.Attack1;
  [SerializeField] private Color gizmoColor = new Color(1f, 0.9f, 0.4f, 0.75f);

  [Header("Slow Motion Testing Settings")]
  [Tooltip("Kéo thanh trượt này trong lúc Play để làm chậm/tăng tốc độ toàn bộ game (0.05x - 1.0x)")]
  [Range(0.05f, 1.0f)]
  [SerializeField] private float gameSpeed = 1.0f;

  [Tooltip("Bật phím tắt Slow Motion: [ giảm tốc độ, ] tăng tốc độ, \\ về 1.0x, Giữ G hoặc T để slow-mo")]
  [SerializeField] private bool enableSlowMoHotkeys = true;
  [SerializeField] private float slowMoHoldScale = 0.15f;

  public static float ActiveSlowMoScale { get; set; } = 1.0f;
  public float GameSpeed => gameSpeed;
  private bool isHoldingSlowMo = false;

  private void Awake()
  {
    if (slashSpawnRoot == null)
    {
      slashSpawnRoot = transform.root;
    }

    if (defaultSlashVfxPrefab == null)
    {
      defaultSlashVfxPrefab = Resources.Load<GameObject>("Slash VFX");
    }

    ActiveSlowMoScale = gameSpeed;
    if (Application.isPlaying && gameSpeed < 0.99f)
    {
      Time.timeScale = gameSpeed;
    }
  }

  private void OnValidate()
  {
    if (Application.isPlaying && !isHoldingSlowMo)
    {
      ApplyTimeScale(gameSpeed);
    }
  }

  private void Update()
  {
    if (!enableSlowMoHotkeys || Keyboard.current == null) return;

    // Phím [ : Giảm tốc độ (1.0 -> 0.5 -> 0.25 -> 0.1 -> 0.05)
    if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
    {
      StepDownTimeScale();
    }
    // Phím ] : Tăng tốc độ (0.05 -> 0.1 -> 0.25 -> 0.5 -> 1.0)
    else if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
    {
      StepUpTimeScale();
    }
    // Phím \ : Trả về 1.0x chuẩn
    else if (Keyboard.current.backslashKey.wasPressedThisFrame)
    {
      ApplyTimeScale(1.0f);
    }

    // Giữ phím G hoặc T: Chạy siêu chậm (0.15x) trong lúc giữ, nhả ra trả về tốc độ trước đó
    bool isHoldDown = (Keyboard.current.gKey != null && Keyboard.current.gKey.isPressed) ||
                      (Keyboard.current.tKey != null && Keyboard.current.tKey.isPressed);

    if (isHoldDown && !isHoldingSlowMo)
    {
      isHoldingSlowMo = true;
      Time.timeScale = slowMoHoldScale;
    }
    else if (!isHoldDown && isHoldingSlowMo)
    {
      isHoldingSlowMo = false;
      Time.timeScale = ActiveSlowMoScale;
    }
  }

  private void StepDownTimeScale()
  {
    float current = ActiveSlowMoScale;
    float next = current switch
    {
      > 0.5f => 0.5f,
      > 0.25f => 0.25f,
      > 0.1f => 0.1f,
      > 0.05f => 0.05f,
      _ => 0.05f
    };
    ApplyTimeScale(next);
  }

  private void StepUpTimeScale()
  {
    float current = ActiveSlowMoScale;
    float next = current switch
    {
      < 0.1f => 0.1f,
      < 0.25f => 0.25f,
      < 0.5f => 0.5f,
      _ => 1.0f
    };
    ApplyTimeScale(next);
  }

  /// <summary>
  /// Đổi tốc độ game trên instance cụ thể và đồng bộ thanh trượt Inspector
  /// </summary>
  public void ApplyTimeScale(float scale)
  {
    scale = Mathf.Clamp(scale, 0.05f, 2.0f);
    gameSpeed = scale;
    ActiveSlowMoScale = scale;
    Time.timeScale = scale;
    Debug.Log($"<color=yellow>[SlowMotion]</color> ⏱️ Tốc độ Game hiện tại: {scale:F2}x (Bấm [ / ] hoặc kéo Inspector)");
  }

  /// <summary>
  /// Đổi tốc độ game qua static API
  /// </summary>
  public static void SetTimeScale(float scale)
  {
    scale = Mathf.Clamp(scale, 0.05f, 2.0f);
    ActiveSlowMoScale = scale;
    Time.timeScale = scale;
    Debug.Log($"<color=yellow>[SlowMotion]</color> ⏱️ Tốc độ Game hiện tại: {scale:F2}x");
  }

  private void OnDisable()
  {
    Time.timeScale = 1.0f;
    ActiveSlowMoScale = 1.0f;
    gameSpeed = 1.0f;
#if UNITY_EDITOR
    StopActiveEditorPreview();
#endif
  }

#if UNITY_EDITOR
  private static GameObject activeEditorPreview;
  private static UnityEditor.EditorApplication.CallbackFunction editorUpdateLoop;

  public static void StopActiveEditorPreview()
  {
    if (editorUpdateLoop != null)
    {
      UnityEditor.EditorApplication.update -= editorUpdateLoop;
      editorUpdateLoop = null;
    }
    if (activeEditorPreview != null)
    {
      DestroyImmediate(activeEditorPreview);
      activeEditorPreview = null;
      UnityEditor.SceneView.RepaintAll();
    }
  }
#endif

  /// <summary>
  /// Kích hoạt Slash VFX tương ứng với loại đòn đánh.
  /// Được gọi từ các Animation Events trong PlayerCombat.
  /// </summary>
  public GameObject PlaySlash(SlashAttackType attackType)
  {
    SlashVfxConfig targetConfig = GetConfig(attackType);
    return PlaySlash(targetConfig);
  }

  /// <summary>
  /// Sinh hiệu ứng chém theo thông số SlashVfxConfig cụ thể.
  /// Hỗ trợ cả Play Mode lẫn Edit Mode (xem trước trực tiếp trong Scene View mà không cần Play).
  /// </summary>
  public GameObject PlaySlash(SlashVfxConfig config)
  {
    if (config == null) return null;

    GameObject prefabToSpawn = config.customPrefab != null ? config.customPrefab : defaultSlashVfxPrefab;
    if (prefabToSpawn == null)
    {
      prefabToSpawn = Resources.Load<GameObject>("Slash VFX");
#if UNITY_EDITOR
      if (prefabToSpawn == null)
      {
        prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Skill/Slash VFX.prefab");
      }
      if (prefabToSpawn == null)
      {
        prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/C Sword Slash/Slash VFX.prefab");
      }
#endif
    }

    if (prefabToSpawn == null)
    {
      Debug.LogWarning("[PlayerSlashVFX] Chưa gán defaultSlashVfxPrefab hoặc customPrefab!");
      return null;
    }

    Transform root = slashSpawnRoot != null ? slashSpawnRoot : transform.root;

    // Tính toán tọa độ và góc xoay dựa trên hướng nhìn của Player tại thời điểm vung kiếm
    Vector3 spawnPosition = root.TransformPoint(config.positionOffset);
    Quaternion spawnRotation = root.rotation * Quaternion.Euler(config.rotationOffset);

    // Sinh đối tượng
    GameObject vfxInstance;
    if (config.parentToPlayer)
    {
      vfxInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, root);
    }
    else
    {
      vfxInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
    }

    // Áp dụng tỷ lệ scale tương ứng
    Vector3 originalScale = prefabToSpawn.transform.localScale;
    vfxInstance.transform.localScale = Vector3.Scale(originalScale, config.scale);

    float duration = config.lifetime > 0f ? config.lifetime : 0.8f;

#if UNITY_EDITOR
    // XỬ LÝ ĐẶC BIỆT CHO EDIT MODE (KHÔNG CẦN BẤM PLAY VẪN XEM ĐƯỢC VFX TRONG SCENE VIEW)
    if (!Application.isPlaying)
    {
      StopActiveEditorPreview();

      activeEditorPreview = vfxInstance;
      vfxInstance.hideFlags = HideFlags.DontSave;

      ParticleSystem rootPs = vfxInstance.GetComponent<ParticleSystem>();
      ParticleSystem[] allPs = vfxInstance.GetComponentsInChildren<ParticleSystem>();

      foreach (ParticleSystem ps in allPs)
      {
        if (ps != null)
        {
          ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
          ps.Clear(true);
        }
      }

      double startTime = UnityEditor.EditorApplication.timeSinceStartup;
      double lastTime = startTime;

      editorUpdateLoop = () =>
      {
        if (vfxInstance == null)
        {
          StopActiveEditorPreview();
          return;
        }

        double now = UnityEditor.EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastTime);
        lastTime = now;

        if (dt > 0f)
        {
          if (rootPs != null)
          {
            rootPs.Simulate(dt, true, false, false);
          }
          else
          {
            foreach (ParticleSystem ps in allPs)
            {
              if (ps != null) ps.Simulate(dt, false, false, false);
            }
          }
          UnityEditor.SceneView.RepaintAll();
        }

        if (now - startTime >= duration)
        {
          StopActiveEditorPreview();
        }
      };

      UnityEditor.EditorApplication.update += editorUpdateLoop;
      return vfxInstance;
    }
#endif

    // Kích hoạt tất cả ParticleSystem con (trong Play Mode)
    ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
    foreach (ParticleSystem ps in particleSystems)
    {
      ps.Play(true);
    }

    // Tự động dọn dẹp sau khi diễn hoạt hoàn tất trong Play Mode
    Destroy(vfxInstance, duration);

    return vfxInstance;
  }

  /// <summary>
  /// Lấy cấu hình của từng loại đòn đánh
  /// </summary>
  public SlashVfxConfig GetConfig(SlashAttackType type)
  {
    return type switch
    {
      SlashAttackType.Attack1 => attack1Config,
      SlashAttackType.Attack2 => attack2Config,
      SlashAttackType.Attack3 => attack3Config,
      SlashAttackType.Charge1 => chargeHit1Config,
      SlashAttackType.Charge2 => chargeHit2Config,
      SlashAttackType.Counter => counterAttackConfig,
      _ => attack1Config
    };
  }

  // =========================================================================
  // CÁC HÀM TIỆN ÍCH DÀNH CHO INSPECTOR & EDITOR CONTEXT MENU
  // =========================================================================

  [ContextMenu("⚡ Test Attack 1 Slash")]
  public void TestAttack1() => PlaySlash(SlashAttackType.Attack1);

  [ContextMenu("⚡ Test Attack 2 Slash")]
  public void TestAttack2() => PlaySlash(SlashAttackType.Attack2);

  [ContextMenu("⚡ Test Attack 3 Slash")]
  public void TestAttack3() => PlaySlash(SlashAttackType.Attack3);

  [ContextMenu("⚡ Test Charge Hit 1 Slash")]
  public void TestCharge1() => PlaySlash(SlashAttackType.Charge1);

  [ContextMenu("⚡ Test Charge Hit 2 Slash")]
  public void TestCharge2() => PlaySlash(SlashAttackType.Charge2);

  [ContextMenu("⚡ Test Counter Attack Slash")]
  public void TestCounter() => PlaySlash(SlashAttackType.Counter);

  [ContextMenu("💥 Test Hit Impact VFX")]
  public void TestImpact()
  {
    WeaponHitbox hitbox = GetComponentInChildren<WeaponHitbox>(true);
    if (hitbox == null && transform.root != null)
    {
      hitbox = transform.root.GetComponentInChildren<WeaponHitbox>(true);
    }

    Transform root = slashSpawnRoot != null ? slashSpawnRoot : transform.root;
    Vector3 testPos = root != null ? root.position + root.forward * 1.2f + Vector3.up * 1.15f : transform.position + Vector3.forward * 1.2f + Vector3.up * 1.15f;
    Vector3 testDir = root != null ? root.forward : transform.forward;

    if (hitbox != null)
    {
      hitbox.SpawnHitImpactVfx(testPos, testDir);
    }
    else
    {
      GameObject impactPrefab = Resources.Load<GameObject>("Impact VFX");
      if (impactPrefab != null)
      {
        GameObject inst = Instantiate(impactPrefab, testPos, Quaternion.LookRotation(-testDir));
        Destroy(inst, 1.5f);
      }
    }
  }

  /// <summary>
  /// Thiết lập Prefab mặc định từ code hoặc Editor Setup
  /// </summary>
  public void SetDefaultPrefab(GameObject prefab)
  {
    defaultSlashVfxPrefab = prefab;
  }

  public GameObject DefaultPrefab => defaultSlashVfxPrefab;

  private void OnDrawGizmosSelected()
  {
    if (!showGizmosInEditor) return;

    Transform root = slashSpawnRoot != null ? slashSpawnRoot : transform.root;
    if (root == null) return;

    SlashVfxConfig previewConfig = GetConfig(gizmoPreviewAttack);
    if (previewConfig == null) return;

    Vector3 centerPos = root.TransformPoint(previewConfig.positionOffset);
    Quaternion rot = root.rotation * Quaternion.Euler(previewConfig.rotationOffset);

    Gizmos.color = gizmoColor;
    Gizmos.DrawWireSphere(centerPos, 0.1f * previewConfig.scale.x);

    // Vẽ mặt phẳng vệt chém (hình quạt mô phỏng)
    Matrix4x4 oldMatrix = Gizmos.matrix;
    Gizmos.matrix = Matrix4x4.TRS(centerPos, rot, previewConfig.scale);

    Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.4f);
    Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.2f, 0.1f, 1.2f));

    Gizmos.color = Color.cyan;
    Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.8f);

    Gizmos.matrix = oldMatrix;
  }
}
