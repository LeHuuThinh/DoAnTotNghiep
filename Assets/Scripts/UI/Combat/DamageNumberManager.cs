using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace InfiniteLabyrinth.UI.Combat
{
  /// <summary>
  /// Cấu hình tùy chỉnh kích cỡ, thời lượng và dải màu Gradient cho từng loại đòn đánh
  /// </summary>
  [System.Serializable]
  public class DamageStyleSetting
  {
    [Tooltip("Tên loại đòn đánh nhận biết trên Inspector")]
    public string styleName = "Damage Style";

    [Tooltip("Tỷ lệ kích thước cơ bản của số (Base Scale). Mặc định 1.0x")]
    [Range(0.4f, 3.5f)]
    public float baseScale = 1.0f;

    [Tooltip("Tỷ lệ búng nảy cực đại khi vừa trúng mục tiêu (Peak Pop Scale)")]
    [Range(0.6f, 5.0f)]
    public float peakScale = 1.45f;

    [Tooltip("Thời gian tồn tại của số sát thương (giây)")]
    [Range(0.3f, 2.5f)]
    public float duration = 0.75f;

    [Tooltip("Preset dải màu Gradient 4 góc (Tự động nạp từ Resources/Color Gradient Presets/ nếu để trống)")]
    public TMP_ColorGradient gradientPreset;

    [Tooltip("Màu sắc đơn sắc dùng khi không tìm thấy Gradient Preset")]
    public Color fallbackColor = Color.white;
  }

  /// <summary>
  /// Quản lý Hệ thống Số Sát Thương Bay Lên (Floating Damage Numbers)
  /// Sử dụng Object Pooling TextMeshPro 3D Mesh (Zero Garbage Collection, 120 FPS).
  /// Cho phép tùy chỉnh kích thước, nhịp búng và gán dải màu Gradient độc lập cho từng chiêu thức.
  /// </summary>
  public class DamageNumberManager : MonoBehaviour
  {
    private static DamageNumberManager instance;
    public static DamageNumberManager Instance
    {
      get
      {
        if (instance == null)
        {
          instance = FindFirstObjectByType<DamageNumberManager>();
          if (instance == null)
          {
            GameObject go = new GameObject("[DamageNumberManager]");
            instance = go.AddComponent<DamageNumberManager>();
            DontDestroyOnLoad(go);
          }
        }
        return instance;
      }
    }
    public static bool HasInstance => instance != null || FindFirstObjectByType<DamageNumberManager>() != null;

    [Header("=== PREFAB SỐ SÁT THƯƠNG (TÙY CHỌN) ===")]
    [Tooltip("Kéo Prefab DamageNumberItem vào đây nếu muốn tùy biến trực quan. Nếu để trống, hệ thống sẽ tự động tạo TextMeshPro 3D bằng code.")]
    [SerializeField] private DamageNumberItem damageNumberPrefab;

    [Header("=== CẤU HÌNH POOL & PHÔNG CHỮ ===")]
    [Tooltip("Số lượng số sát thương khởi tạo sẵn trong Pool (mặc định 40)")]
    [SerializeField] private int initialPoolSize = 40;

    [Tooltip("Font chữ TextMeshPro (Tự động tải VecnaBold của dự án nếu để trống)")]
    [SerializeField] private TMP_FontAsset fontAsset;

    [Tooltip("Cỡ chữ hiển thị trong không gian 3D thế giới (World Space). Thay đổi số này sẽ tự động cập nhật ngay lập tức cho toàn bộ số sát thương (kể cả dùng Prefab)")]
    [SerializeField] private float fontSize = 4.5f;

    [Tooltip("Độ dày đường viền đen của chữ (Outline Width). Mặc định 0.1")]
    [Range(0f, 0.5f)]
    [SerializeField] private float outlineWidth = 0.1f;

    [Header("=== CẤU HÌNH ICON CHÍ MẠNG (CRIT ICON) ===")]
    [Tooltip("Bật/tắt hiển thị icon Crit bên cạnh số bạo kích trên toàn bộ Pool. Nếu tắt, chỉ phóng to số")]
    [SerializeField] private bool showCritIcon = true;

    [Tooltip("Tỷ lệ thu nhỏ của Icon Crit (Mặc định 0.085 vừa vặn với cỡ chữ, không che số)")]
    [Range(0.02f, 0.4f)]
    [SerializeField] private float critIconScale = 0.085f;

    [Tooltip("Khoảng cách lệch sang trái của Icon Crit so với mép con số")]
    [Range(0.01f, 0.3f)]
    [SerializeField] private float critIconOffsetLeft = 0.08f;

    [Tooltip("Hệ số phóng to số sát thương khi bạo kích (Mặc định 1.15x để số không bị quá to che khuất quái)")]
    [Range(1.0f, 1.6f)]
    [SerializeField] private float critScaleMultiplier = 1.15f;

    [Header("=== BẢNG TÙY CHỈNH KÍCH THƯỚC & GRADIENT TỪNG LOẠI ĐÒN ===")]
    [SerializeField] private DamageStyleSetting normalAttackStyle = new DamageStyleSetting 
    { 
      styleName = "1. Đòn Đánh Thường (Combo 1, 2, 3)",
      baseScale = 1.0f, 
      peakScale = 1.45f, 
      duration = 0.70f, 
      fallbackColor = Color.white 
    };

    [SerializeField] private DamageStyleSetting chargeAttackStyle = new DamageStyleSetting 
    { 
      styleName = "2. Đòn Gồng Lực (Charge Attack)",
      baseScale = 1.30f, 
      peakScale = 1.75f, 
      duration = 0.75f, 
      fallbackColor = new Color(1.0f, 0.75f, 0.08f) 
    };

    [SerializeField] private DamageStyleSetting counterAttackStyle = new DamageStyleSetting 
    { 
      styleName = "3. Đòn Phản Công Sau Parry (Counter)",
      baseScale = 1.40f, 
      peakScale = 1.90f, 
      duration = 0.80f, 
      fallbackColor = new Color(1.0f, 0.60f, 0.0f) 
    };

    [SerializeField] private DamageStyleSetting skillEStyle = new DamageStyleSetting 
    { 
      styleName = "4. Kỹ Năng E (Moon Slash - Kiếm Khí)",
      baseScale = 1.35f, 
      peakScale = 1.80f, 
      duration = 0.75f, 
      fallbackColor = new Color(0.22f, 0.85f, 1.0f) 
    };

    [SerializeField] private DamageStyleSetting skillQStyle = new DamageStyleSetting 
    { 
      styleName = "5. Kỹ Năng Q (Ground AOE - Chấn Địa)",
      baseScale = 1.40f, 
      peakScale = 1.85f, 
      duration = 0.80f, 
      fallbackColor = new Color(1.0f, 0.45f, 0.15f) 
    };

    [SerializeField] private DamageStyleSetting skillRStyle = new DamageStyleSetting 
    { 
      styleName = "6. Tuyệt Kỹ R (Ultimate Colossal Slash - Đỏ Huyết)",
      baseScale = 1.80f, 
      peakScale = 2.45f, 
      duration = 0.95f, 
      fallbackColor = new Color(1.0f, 0.12f, 0.16f) 
    };

    [SerializeField] private DamageStyleSetting playerHitStyle = new DamageStyleSetting 
    { 
      styleName = "7. Quái Đánh Trúng Player (Player Damaged)",
      baseScale = 1.25f, 
      peakScale = 1.55f, 
      duration = 0.75f, 
      fallbackColor = new Color(1.0f, 0.22f, 0.22f) 
    };

    [SerializeField] private DamageStyleSetting bleedStyle = new DamageStyleSetting 
    { 
      styleName = "8. Sát Thương Xuất Huyết (Bleed DoT)",
      baseScale = 0.85f, 
      peakScale = 1.15f, 
      duration = 0.65f, 
      fallbackColor = new Color(0.85f, 0.12f, 0.12f) 
    };

    private readonly Queue<DamageNumberItem> availablePool = new Queue<DamageNumberItem>();
    private readonly List<DamageNumberItem> activeItems = new List<DamageNumberItem>();
    private Transform poolContainer;
    private Camera cachedMainCamera;
    private Material damageNumberMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
      if (instance == null)
      {
        var dummy = Instance;
      }
    }

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);

      InitializePool();
    }

    public float CritIconOffsetLeft
    {
      get => critIconOffsetLeft;
      set => critIconOffsetLeft = value;
    }

    private void OnValidate()
    {
      if (damageNumberMaterial != null)
      {
        damageNumberMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
      }
      foreach (var it in availablePool)
      {
        if (it != null)
        {
          it.ShowCritIcon = showCritIcon;
          it.CritIconScale = critIconScale;
          it.CritIconOffsetLeft = critIconOffsetLeft;
          it.CritScaleMultiplier = critScaleMultiplier;
          var tmp = it.GetComponent<TextMeshPro>();
          if (tmp != null)
          {
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
          }
        }
      }
      foreach (var it in activeItems)
      {
        if (it != null)
        {
          it.ShowCritIcon = showCritIcon;
          it.CritIconScale = critIconScale;
          it.CritIconOffsetLeft = critIconOffsetLeft;
          it.CritScaleMultiplier = critScaleMultiplier;
          var tmp = it.GetComponent<TextMeshPro>();
          if (tmp != null)
          {
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
          }
        }
      }
    }

    private void InitializePool()
    {
      // Dọn dẹp pool cũ nếu có tàn dư từ scene save
      Transform existing = transform.Find("DamageNumberPool");
      if (existing != null)
      {
        Destroy(existing.gameObject);
      }

      GameObject containerGo = new GameObject("DamageNumberPool");
      containerGo.transform.SetParent(transform);
      poolContainer = containerGo.transform;

      // 1. Tự động nạp Font VecnaBold của dự án nếu chưa gán (hoặc lấy từ Prefab nếu có)
      if (fontAsset == null)
      {
        if (damageNumberPrefab != null)
        {
          TextMeshPro prefabTmp = damageNumberPrefab.GetComponent<TextMeshPro>();
          if (prefabTmp != null && prefabTmp.font != null)
          {
            fontAsset = prefabTmp.font;
          }
        }
        if (fontAsset == null)
        {
          fontAsset = Resources.Load<TMP_FontAsset>("Color Gradient Presets/VecnaBold-4YY4 SDF");
        }
        if (fontAsset == null)
        {
          fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (fontAsset == null)
        {
          fontAsset = TMP_Settings.defaultFontAsset;
        }
      }

      // 2. Tự động nạp Preset Gradient từ thư mục Resources/Color Gradient Presets/ nếu để trống
      LoadDefaultGradients();

      // 3. Tạo một Material dùng chung (Shared Material) có Outline đen dày và ZTest Always
      if (fontAsset != null && fontAsset.material != null)
      {
        damageNumberMaterial = new Material(fontAsset.material);
        damageNumberMaterial.name = "TMP_DamageNumber_Material";
        damageNumberMaterial.EnableKeyword("OUTLINE_ON");
        damageNumberMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.02f, 0.02f, 0.02f, 0.98f));
        damageNumberMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);

        // ZTest Always: Luôn vẽ đè lên trên thân quái vật và môi trường
        Shader overlayShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");
        if (overlayShader != null)
        {
          damageNumberMaterial.shader = overlayShader;
        }
        else
        {
          damageNumberMaterial.SetInt(Shader.PropertyToID("unity_GUIZTestMode"), (int)UnityEngine.Rendering.CompareFunction.Always);
        }
        damageNumberMaterial.renderQueue = 3000;
      }

      // 4. Khởi tạo sẵn các item trong Pool
      for (int i = 0; i < initialPoolSize; i++)
      {
        CreateNewPoolItem();
      }

      Debug.Log($"<color=green>[DamageNumberManager]</color> ✅ Đã khởi tạo hoàn tất Pool {initialPoolSize} số sát thương (Gradient Ready)!");
    }

    private void LoadDefaultGradients()
    {
      if (normalAttackStyle.gradientPreset == null)
        normalAttackStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_Normal");
      if (chargeAttackStyle.gradientPreset == null)
        chargeAttackStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_Charge");
      if (counterAttackStyle.gradientPreset == null)
        counterAttackStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_Counter");
      if (skillEStyle.gradientPreset == null)
        skillEStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_SkillE");
      if (skillQStyle.gradientPreset == null)
        skillQStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_SkillQ");
      if (skillRStyle.gradientPreset == null)
        skillRStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_SkillR");
      if (playerHitStyle.gradientPreset == null)
        playerHitStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_PlayerHit");
      if (bleedStyle.gradientPreset == null)
        bleedStyle.gradientPreset = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Damage_Bleed");
    }

    private DamageNumberItem CreateNewPoolItem()
    {
      DamageNumberItem item;
      GameObject itemGo;

      if (damageNumberPrefab != null)
      {
        item = Instantiate(damageNumberPrefab, poolContainer);
        itemGo = item.gameObject;
        itemGo.name = $"DamageNumber_{availablePool.Count + activeItems.Count}";

        TextMeshPro tmp = itemGo.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
          if (damageNumberMaterial != null)
          {
            tmp.fontSharedMaterial = damageNumberMaterial;
          }
          tmp.fontSize = fontSize;
          tmp.alignment = TextAlignmentOptions.Center;
        }
      }
      else
      {
        itemGo = new GameObject($"DamageNumber_{availablePool.Count + activeItems.Count}");
        itemGo.transform.SetParent(poolContainer);

        TextMeshPro tmp = itemGo.AddComponent<TextMeshPro>();
        tmp.fontSize = fontSize;
        if (fontAsset != null) tmp.font = fontAsset;
        if (damageNumberMaterial != null) tmp.fontSharedMaterial = damageNumberMaterial;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.sortingOrder = 2000;

        item = itemGo.AddComponent<DamageNumberItem>();
        item.Setup(fontAsset);
      }

      item.ShowCritIcon = showCritIcon;
      item.CritIconScale = critIconScale;
      item.CritIconOffsetLeft = critIconOffsetLeft;
      item.CritScaleMultiplier = critScaleMultiplier;

      itemGo.SetActive(false);
      availablePool.Enqueue(item);
      return item;
    }

    public DamageStyleSetting GetStyle(DamageNumberType type)
    {
      switch (type)
      {
        case DamageNumberType.Normal: return normalAttackStyle;
        case DamageNumberType.Charge: return chargeAttackStyle;
        case DamageNumberType.Counter: return counterAttackStyle;
        case DamageNumberType.SkillE: return skillEStyle;
        case DamageNumberType.SkillQ: return skillQStyle;
        case DamageNumberType.SkillR: return skillRStyle;
        case DamageNumberType.PlayerHit: return playerHitStyle;
        case DamageNumberType.Bleed: return bleedStyle;
        default: return normalAttackStyle;
      }
    }

    /// <summary>
    /// Kích hoạt số sát thương bay lên tại vị trí va chạm thế giới (World Position)
    /// </summary>
    public void SpawnDamage(float damage, Vector3 worldPosition, DamageNumberType type = DamageNumberType.Normal, bool isCrit = false)
    {
      if (damage <= 0f) return;

      if (cachedMainCamera == null)
      {
        cachedMainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
      }

      DamageStyleSetting style = GetStyle(type);

      DamageNumberItem item;
      if (availablePool.Count > 0)
      {
        item = availablePool.Dequeue();
      }
      else if (activeItems.Count > 0)
      {
        // Khi xảy ra bão sát thương diện rộng cực lớn làm cạn pool, tái chế số cũ nhất
        item = activeItems[0];
        activeItems.RemoveAt(0);
      }
      else
      {
        item = CreateNewPoolItem();
        availablePool.Dequeue();
      }

      activeItems.Add(item);
      item.Spawn(damage, worldPosition, type, style, cachedMainCamera, ReturnToPool, isCrit);
    }

    private void ReturnToPool(DamageNumberItem item)
    {
      if (item == null) return;
      activeItems.Remove(item);
      if (!availablePool.Contains(item))
      {
        availablePool.Enqueue(item);
      }
    }
  }
}
