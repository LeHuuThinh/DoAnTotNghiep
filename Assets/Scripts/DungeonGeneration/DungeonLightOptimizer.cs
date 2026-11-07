using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Bộ tối ưu hóa ánh sáng, bóng đổ và hiệu ứng hạt (Light, Shadow & Particle Culling Optimizer) cho Dungeon.
/// Tự động quản lý theo cơ chế 3 vùng cự ly (3-Tier Distance Optimization):
/// - Vùng 1: Ánh sáng BẬT + ĐỔ BÓNG BẬT (Soft Shadows) + Hạt lửa/khói BẬT.
///   (Bóng của Player, vũ khí kiếm, quái vật và các đồ tĩnh gộp mesh nhảy múa sống động theo ánh lửa).
/// - Vùng 2: Ánh sáng BẬT + ĐỔ BÓNG TẮT (None) + Hạt lửa/khói BẬT.
///   (Thấy ánh lửa le lói từ xa nhưng giải phóng 100% Shadow Atlas GPU, không tốn render pass).
/// - Vùng 3: TẮT TOÀN BỘ (Đèn tắt, Hạt dừng mô phỏng) để giải phóng CPU/GPU tối đa.
/// Tích hợp cơ chế giới hạn số lượng đèn đổ bóng gần nhất (Max Active Shadow Lights) để triệt tiêu 100% cảnh báo quá tải Shadow Atlas.
/// Hỗ trợ Shadow Culling cho cả Đồ Vật Tĩnh Đã Gộp Mesh (Static Props) khi người chơi ở gần phòng.
/// </summary>
public class DungeonLightOptimizer : MonoBehaviour
{
  [System.Serializable]
  public class ManagedLightSource
  {
    public Transform transform;
    public Light light;
    public ParticleSystem[] particles;
    public bool isEnabled;
    public bool isShadowEnabled;
    public LightShadows originalShadowType;
    public float currentDistSq;
  }

  [System.Serializable]
  public class ManagedStaticProp
  {
    public Transform transform;
    public MeshRenderer renderer;
    public Vector3 center;
    public bool isShadowEnabled;
  }

  [Header("=== 1. CẤU HÌNH TẦM KÍCH HOẠT ÁNH SÁNG & HIỆU ỨNG HẠT ===")]
  [Range(10f, 150f)]
  [Tooltip("Khoảng cách tối đa (mét) từ người chơi để một ngọn đèn và hiệu ứng lửa/khói được phép bật (với map 300m khuyên dùng 70 - 90m)")]
  public float maxLightDistance = 80f;

  [Range(2f, 20f)]
  [Tooltip("Độ đệm tắt đèn chống nhấp nháy (mét): Bật ở maxLightDistance, nhưng chỉ tắt khi người chơi đi xa hơn maxLightDistance + buffer")]
  public float cullingBufferDistance = 6f;

  [Tooltip("Tần suất kiểm tra khoảng cách (giây). 0.08s = ~12 lần/giây (cực nhạy, siêu nhẹ không tốn CPU)")]
  public float checkInterval = 0.08f;

  [Tooltip("Bật/Tắt tối ưu hóa cả hiệu ứng hạt ParticleSystem (khói, lửa) cùng với ánh sáng")]
  public bool cullParticles = true;

  [Header("=== 2. CẤU HÌNH TỐI ƯU HÓA BÓNG ĐỔ ĐUỐC (TORCH SHADOW CULLING) ===")]
  [Tooltip("Bật/Tắt tính năng tối ưu bóng đổ theo cự ly gần người chơi")]
  public bool enableShadowCulling = true;

  [Tooltip("Tự động kích hoạt bóng đổ cho các ngọn đuốc hầm ngục nếu đuốc chưa có shadow")]
  public bool autoEnableTorchShadows = true;

  [Tooltip("Loại bóng đổ mặc định cho đuốc")]
  public LightShadows defaultShadowType = LightShadows.Soft;

  [Range(0.1f, 1f)]
  [Tooltip("Độ đậm của bóng đổ")]
  public float defaultShadowStrength = 0.85f;

  [Range(10f, 60f)]
  [Tooltip("Khoảng cách tối đa (mét) từ người chơi để đèn ĐƯỢC PHÉP ĐỔ BÓNG (Khuyên dùng 25 - 36m)")]
  public float maxShadowDistance = 36f;

  [Range(1f, 10f)]
  [Tooltip("Độ đệm tắt bóng đổ chống nhấp nháy ở ranh giới (mét)")]
  public float shadowBufferDistance = 5f;

  [Range(1, 8)]
  [Tooltip("Số lượng ngọn đuốc tối đa ĐƯỢC PHÉP ĐỔ BÓNG ĐỒNG THỜI (ưu tiên các ngọn đuốc gần Player nhất để triệt tiêu cảnh báo Shadow Atlas)")]
  public int maxActiveShadowLights = 4;

  [Header("=== 3. CẤU HÌNH BÓNG ĐỔ ĐỒ VẬT TĨNH GỘP MESH (STATIC PROPS SHADOW CULLING) ===")]
  [Tooltip("Tự động bật bóng đổ cho các cụm đồ vật tĩnh đã gộp mesh (bàn, ghế, tủ, cột, tượng...) ở gần Player")]
  public bool enableStaticPropsShadowCulling = true;

  [Range(10f, 80f)]
  [Tooltip("Khoảng cách tối đa (mét) để đồ vật tĩnh gộp mesh được phép đổ bóng (Khuyên dùng 35 - 45m)")]
  public float maxPropShadowDistance = 38f;

  [Range(2f, 15f)]
  [Tooltip("Độ đệm tắt bóng đồ tĩnh chống nhấp nháy ở ranh giới (mét)")]
  public float propShadowBufferDistance = 6f;

  private Transform playerTransform;
  private List<ManagedLightSource> allSources = new List<ManagedLightSource>();
  private List<ManagedStaticProp> allStaticProps = new List<ManagedStaticProp>();
  private List<ManagedLightSource> shadowCandidates = new List<ManagedLightSource>();
  private float timer = 0f;

  void Start()
  {
    FindPlayer();
    RefreshLights();
    RefreshStaticProps();
  }

  public void RefreshLights()
  {
    allSources.Clear();

    // 1. Quét tất cả component Light
    Light[] lights = GetComponentsInChildren<Light>(true);
    HashSet<ParticleSystem> registeredParticles = new HashSet<ParticleSystem>();

    for (int i = 0; i < lights.Length; i++)
    {
      Light l = lights[i];
      if (l == null) continue;

      // Bỏ qua đèn thuộc đối tượng tương tác (Rương báu, item pickup) để tránh bật/tắt nhầm
      if (l.GetComponentInParent<InfiniteLabyrinth.Inventory.ChestInteractable>() != null ||
          l.GetComponentInParent<InfiniteLabyrinth.Inventory.WorldItemPickup>() != null)
      {
        continue;
      }

      // Tìm gốc ngọn đuốc / đèn (thường là parent hoặc grandparent dưới nhóm Lights)
      Transform root = l.transform;
      while (root.parent != null &&
             root.parent != transform &&
             root.parent.name != "Lights" &&
             root.parent.name != "Corridor_Lights")
      {
        root = root.parent;
      }

      ParticleSystem[] pList = root.GetComponentsInChildren<ParticleSystem>(true);
      for (int p = 0; p < pList.Length; p++)
      {
        registeredParticles.Add(pList[p]);
      }

      // Nhận diện và thiết lập loại bóng đổ ban đầu
      LightShadows targetShadow = l.shadows;
      if (autoEnableTorchShadows && targetShadow == LightShadows.None)
      {
        targetShadow = defaultShadowType;
        l.shadowStrength = defaultShadowStrength;
        l.shadowBias = 0.05f;
        l.shadowNormalBias = 0.4f;
      }

      ManagedLightSource src = new ManagedLightSource
      {
        transform = l.transform,
        light = l,
        particles = pList,
        isEnabled = true,
        isShadowEnabled = (targetShadow != LightShadows.None),
        originalShadowType = targetShadow
      };
      allSources.Add(src);
    }

    // 2. Quét bổ sung các ParticleSystem độc lập nếu có (chưa nằm trong cụm Light nào)
    ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);
    for (int i = 0; i < allParticles.Length; i++)
    {
      ParticleSystem ps = allParticles[i];
      if (ps == null || registeredParticles.Contains(ps)) continue;

      // Bỏ qua hạt thuộc đối tượng tương tác (Rương báu, item pickup)
      if (ps.GetComponentInParent<InfiniteLabyrinth.Inventory.ChestInteractable>() != null ||
          ps.GetComponentInParent<InfiniteLabyrinth.Inventory.WorldItemPickup>() != null)
      {
        continue;
      }

      ManagedLightSource src = new ManagedLightSource
      {
        transform = ps.transform,
        light = null,
        particles = new ParticleSystem[] { ps },
        isEnabled = true,
        isShadowEnabled = false,
        originalShadowType = LightShadows.None
      };
      allSources.Add(src);
      registeredParticles.Add(ps);
    }

    RefreshStaticProps();
    UpdateLightsImmediate(true);
  }

  /// <summary>
  /// Quét toàn bộ các GameObject đồ vật tĩnh đã gộp mesh (StaticProps) trong hầm ngục
  /// </summary>
  public void RefreshStaticProps()
  {
    allStaticProps.Clear();

    MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
    for (int i = 0; i < renderers.Length; i++)
    {
      MeshRenderer mr = renderers[i];
      if (mr == null) continue;

      // Chỉ thu thập các mesh gộp đồ tĩnh (StaticProps) do DungeonStaticPropCombiner tạo ra
      if (mr.gameObject.name.IndexOf("StaticProps", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        Vector3 cPos = mr.bounds.size.sqrMagnitude > 0.01f ? mr.bounds.center : mr.transform.position;
        allStaticProps.Add(new ManagedStaticProp
        {
          transform = mr.transform,
          renderer = mr,
          center = cPos,
          isShadowEnabled = false
        });

        // Mặc định ban đầu tắt bóng đổ cho đồ tĩnh, sẽ được bật khi Player lại gần
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      }
    }
  }

  void Update()
  {
    timer += Time.deltaTime;
    if (timer >= checkInterval)
    {
      timer = 0f;
      UpdateLightsImmediate(false);
    }
  }

  private void FindPlayer()
  {
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerTransform = player.transform;
      return;
    }

    player = GameObject.Find("Player");
    if (player != null)
    {
      playerTransform = player.transform;
      return;
    }

    var playerCtrl = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    if (playerCtrl != null)
    {
      playerTransform = playerCtrl.transform;
      return;
    }

    Camera mainCam = Camera.main;
    if (mainCam != null) playerTransform = mainCam.transform;
  }

  public void UpdateLightsImmediate(bool forceUpdate = false)
  {
    if (playerTransform == null)
    {
      FindPlayer();
      if (playerTransform == null) return;
    }

    Vector3 pPos = playerTransform.position;

    // Ngưỡng cự ly Bật/Tắt Toàn Bộ Đèn & Hạt
    float onDistSq = maxLightDistance * maxLightDistance;
    float offDist = maxLightDistance + cullingBufferDistance;
    float offDistSq = offDist * offDist;

    // Ngưỡng cự ly Bật/Tắt Đổ Bóng Đuốc (Shadow Culling)
    float shadowOnDistSq = maxShadowDistance * maxShadowDistance;
    float shadowOffDist = maxShadowDistance + shadowBufferDistance;
    float shadowOffDistSq = shadowOffDist * shadowOffDist;

    shadowCandidates.Clear();

    // 1. KIỂM TRA BẬT/TẮT ÁNH SÁNG & THU THẬP ĐÈN ĐỔ BÓNG TIỀM NĂNG
    for (int i = 0; i < allSources.Count; i++)
    {
      ManagedLightSource src = allSources[i];
      if (src == null || src.transform == null) continue;

      float distSq = (src.transform.position - pPos).sqrMagnitude;
      src.currentDistSq = distSq;

      bool shouldEnable;
      if (forceUpdate)
      {
        shouldEnable = distSq <= onDistSq;
      }
      else if (src.isEnabled)
      {
        shouldEnable = distSq <= offDistSq;
      }
      else
      {
        shouldEnable = distSq <= onDistSq;
      }

      if (forceUpdate || shouldEnable != src.isEnabled)
      {
        src.isEnabled = shouldEnable;

        // Bật/tắt tính toán ánh sáng GPU
        if (src.light != null && src.light.enabled != shouldEnable)
        {
          src.light.enabled = shouldEnable;
        }

        // Bật/tắt mô phỏng hạt CPU & vẽ quads GPU
        if (cullParticles && src.particles != null)
        {
          for (int p = 0; p < src.particles.Length; p++)
          {
            ParticleSystem ps = src.particles[p];
            if (ps == null) continue;

            var emission = ps.emission;
            if (emission.enabled != shouldEnable)
            {
              emission.enabled = shouldEnable;
            }

            if (shouldEnable)
            {
              if (!ps.isPlaying) ps.Play();
            }
            else
            {
              if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
          }
        }
      }

      // Thu thập các đèn đang bật và có hỗ trợ shadow nằm trong cự ly cho phép
      if (src.isEnabled && src.light != null && src.originalShadowType != LightShadows.None)
      {
        float checkDist = src.isShadowEnabled ? shadowOffDistSq : shadowOnDistSq;
        if (distSq <= checkDist)
        {
          shadowCandidates.Add(src);
        }
        else
        {
          if (src.isShadowEnabled)
          {
            src.isShadowEnabled = false;
            src.light.shadows = LightShadows.None;
          }
        }
      }
      else if (!src.isEnabled && src.light != null && src.isShadowEnabled)
      {
        src.isShadowEnabled = false;
        src.light.shadows = LightShadows.None;
      }
    }

    // 2. GIỚI HẠN SỐ LƯỢNG ĐÈN ĐỔ BÓNG ĐỒNG THỜI (ƯU TIÊN CÁC ĐÈN GẦN PLAYER NHẤT)
    if (enableShadowCulling)
    {
      if (shadowCandidates.Count > 1)
      {
        // Sắp xếp các đèn gần người chơi nhất lên đầu danh sách
        shadowCandidates.Sort((a, b) => a.currentDistSq.CompareTo(b.currentDistSq));
      }

      int allowedCount = Mathf.Max(1, maxActiveShadowLights);
      for (int i = 0; i < shadowCandidates.Count; i++)
      {
        ManagedLightSource src = shadowCandidates[i];
        bool shouldShadow = (i < allowedCount);

        if (src.isShadowEnabled != shouldShadow || forceUpdate)
        {
          src.isShadowEnabled = shouldShadow;
          src.light.shadows = shouldShadow ? src.originalShadowType : LightShadows.None;
        }
      }
    }

    // 3. KIỂM TRA BẬT/TẮT BÓNG ĐỔ CHO ĐỒ VẬT TĨNH GỘP MESH (STATIC PROPS)
    if (enableStaticPropsShadowCulling && allStaticProps.Count > 0)
    {
      float propOnDistSq = maxPropShadowDistance * maxPropShadowDistance;
      float propOffDist = maxPropShadowDistance + propShadowBufferDistance;
      float propOffDistSq = propOffDist * propOffDist;

      for (int i = 0; i < allStaticProps.Count; i++)
      {
        ManagedStaticProp prop = allStaticProps[i];
        if (prop == null || prop.renderer == null) continue;

        float distSq = (prop.center - pPos).sqrMagnitude;
        bool shouldCastShadow;

        if (forceUpdate)
        {
          shouldCastShadow = distSq <= propOnDistSq;
        }
        else if (prop.isShadowEnabled)
        {
          shouldCastShadow = distSq <= propOffDistSq;
        }
        else
        {
          shouldCastShadow = distSq <= propOnDistSq;
        }

        if (forceUpdate || shouldCastShadow != prop.isShadowEnabled)
        {
          prop.isShadowEnabled = shouldCastShadow;
          prop.renderer.shadowCastingMode = shouldCastShadow
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
      }
    }
  }
}