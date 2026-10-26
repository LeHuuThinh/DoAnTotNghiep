using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bộ tối ưu hóa ánh sáng và hiệu ứng hạt (Light & Particle Culling Optimizer) cho Dungeon.
/// Tự động bật/tắt (Enable/Disable) component Point Light và ParticleSystem (khói, lửa) dựa trên khoảng cách tới người chơi.
/// Giữ nguyên 100% hình ảnh 3D của chân đuốc và giá đỡ, chỉ tắt tính toán chiếu sáng GPU và mô phỏng hạt CPU khi ở xa.
/// Hỗ trợ cơ chế Hysteresis Buffer chống giật/nhấp nháy đèn (Flicker & Pop-in free) cho hầm ngục quy mô lớn.
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
  }

  [Header("Cấu hình Tầm Kích Hoạt Ánh Sáng")]
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

  private Transform playerTransform;
  private List<ManagedLightSource> allSources = new List<ManagedLightSource>();
  private float timer = 0f;

  void Start()
  {
    FindPlayer();
    RefreshLights();
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

      ManagedLightSource src = new ManagedLightSource
      {
        transform = l.transform,
        light = l,
        particles = pList,
        isEnabled = true
      };
      allSources.Add(src);
    }

    // 2. Quét bổ sung các ParticleSystem độc lập nếu có (chưa nằm trong cụm Light nào)
    ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);
    for (int i = 0; i < allParticles.Length; i++)
    {
      ParticleSystem ps = allParticles[i];
      if (ps == null || registeredParticles.Contains(ps)) continue;

      ManagedLightSource src = new ManagedLightSource
      {
        transform = ps.transform,
        light = null,
        particles = new ParticleSystem[] { ps },
        isEnabled = true
      };
      allSources.Add(src);
      registeredParticles.Add(ps);
    }

    UpdateLightsImmediate(true);
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

    var playerCtrl = Object.FindFirstObjectByType<PlayerController>();
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

    if (allSources == null || allSources.Count == 0) return;

    Vector3 pPos = playerTransform.position;
    float onDistSq = maxLightDistance * maxLightDistance;
    float offDist = maxLightDistance + cullingBufferDistance;
    float offDistSq = offDist * offDist;

    for (int i = 0; i < allSources.Count; i++)
    {
      ManagedLightSource src = allSources[i];
      if (src == null || src.transform == null) continue;

      float distSq = (src.transform.position - pPos).sqrMagnitude;
      
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
    }
  }
}
