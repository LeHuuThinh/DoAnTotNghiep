using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Quản lý Rương kho báu trong hầm ngục theo 3 cấp độ (Thường, Hiếm, Boss).
  /// Kế thừa IInteractable hỗ trợ đè giữ phím [F] để mở nắp, kích nổ luồng tia sáng VFX theo màu tier và tung đồ ra đất.
  /// </summary>
  public class ChestInteractable : MonoBehaviour, IInteractable
  {
    [Header("=== CẤU HÌNH MỞ RƯƠNG ===")]
    [Tooltip("Khớp nắp rương để xoay mở")]
    [SerializeField] private Transform chestLid;
    [Tooltip("Góc mở nắp (thường xoay quanh trục X hoặc Z từ -80 đến -120 độ)")]
    [SerializeField] private Vector3 openEulerOffset = new Vector3(-95f, 0f, 0f);
    [Tooltip("Tốc độ mở nắp")]
    [SerializeField] private float openSpeed = 3.5f;

    [Header("=== CẤU HÌNH TƯƠNG TÁC (IINTERACTABLE) ===")]
    [Tooltip("Tên định danh của rương (VD: Rương Thường, Rương Hiếm, Rương Boss)")]
    [SerializeField] private string chestName = "Rương Kho Báu";
    [Tooltip("Câu gợi ý hiển thị trên giao diện (VD: Mở Rương)")]
    [SerializeField] private string interactionPrompt = "Mở Rương";
    [Tooltip("Có yêu cầu đè giữ phím không")]
    [SerializeField] private bool requiresHold = true;
    [Tooltip("Thời gian cần giữ phím (giây), ví dụ: 0.8s")]
    [SerializeField] private float holdDuration = 0.8f;
    [Tooltip("Bán kính có thể tương tác")]
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("=== HIỆU ỨNG VÀ ÂM THANH ===")]
    [SerializeField] private AudioClip openSound;
    [Tooltip("Đối tượng VFX mở rương gắn sẵn trong Prefab (con OpenVFX)")]
    [SerializeField] private GameObject openVfxObject;
    [Tooltip("Prefab VFX mở rương dự phòng (nếu không gắn sẵn con OpenVFX)")]
    [SerializeField] private GameObject openVfxPrefab;
    [Tooltip("Độ trễ kích nổ tia sáng khi nắp rương vừa bắt đầu hé mở (giây)")]
    [SerializeField] private float vfxDelay = 0.05f;
    [Tooltip("Thời gian tự tắt hiệu ứng sau khi mở (giây). Đặt <= 0 để giữ tia sáng phát quang vĩnh viễn.")]
    [SerializeField] private float vfxDuration = 5f;

    private bool isOpen = false;
    private LootDropper lootDropper;
    private Quaternion originalLidRot;
    private Dictionary<Light, float> lightBaseIntensities = new Dictionary<Light, float>();
    private Coroutine stopVfxCoroutine;

    public bool IsOpen => isOpen;

    // Triển khai IInteractable
    public string InteractionPrompt => interactionPrompt;
    public string TargetName => string.IsNullOrEmpty(chestName) ? "Rương Kho Báu" : chestName;
    public bool RequiresHold => requiresHold;
    public float HoldDuration => requiresHold ? Mathf.Max(0.01f, holdDuration) : 0f;
    public float InteractionRadius => interactionRadius;
    public Transform InteractionTransform => transform;

    private void Awake()
    {
      lootDropper = GetComponent<LootDropper>();
      if (chestLid == null)
      {
        chestLid = transform.Find("Lid") ?? transform.Find("chest_lid") ?? transform.Find("top");
      }
      if (chestLid != null)
      {
        originalLidRot = chestLid.localRotation;
      }

      // Tự động tìm OpenVFX nếu chưa kéo vào Inspector
      if (openVfxObject == null)
      {
        Transform vfxChild = transform.Find("OpenVFX") ?? transform.Find("open_vfx") ?? transform.Find("Chest_Open_VFX");
        if (vfxChild != null)
        {
          openVfxObject = vfxChild.gameObject;
        }
      }

      // Lưu trữ cường độ gốc của các đèn trong OpenVFX
      if (openVfxObject != null)
      {
        Light[] lights = openVfxObject.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
          if (l != null && !lightBaseIntensities.ContainsKey(l))
          {
            lightBaseIntensities[l] = l.intensity;
          }
        }

        // Đảm bảo ban đầu rương đóng thì tắt VFX hoàn toàn
        if (!isOpen)
        {
          openVfxObject.SetActive(false);
        }
      }
    }

    private void OnEnable()
    {
      InteractableRegistry.Register(this);
    }

    private void OnDisable()
    {
      InteractableRegistry.Unregister(this);
    }

    public bool CanInteract(GameObject player)
    {
      return !isOpen && enabled && gameObject.activeInHierarchy;
    }

    public void OnInteract(GameObject player)
    {
      OpenChest();
    }

    /// <summary>
    /// Kích hoạt mở nắp rương, bung tia sáng VFX và rơi chiến lợi phẩm
    /// </summary>
    public void OpenChest()
    {
      if (isOpen) return;
      isOpen = true;

      // 1. Chạy hiệu ứng xoay nắp rương
      if (chestLid != null)
      {
        StartCoroutine(OpenLidRoutine());
      }

      // 2. Kích hoạt hiệu ứng âm thanh
      if (openSound != null)
      {
        AudioSource.PlayClipAtPoint(openSound, transform.position);
      }

      // 3. Kích nổ tia sáng VFX đồng bộ khi nắp hé mở
      if (vfxDelay > 0f)
      {
        StartCoroutine(TriggerVfxWithDelayRoutine(vfxDelay));
      }
      else
      {
        TriggerOpenVfx();
      }

      // 4. Tung các món đồ chiến lợi phẩm ra xung quanh
      if (lootDropper != null)
      {
        lootDropper.DropLoot();
      }
    }

    private IEnumerator TriggerVfxWithDelayRoutine(float delay)
    {
      yield return new WaitForSeconds(delay);
      TriggerOpenVfx();
    }

    private void TriggerOpenVfx()
    {
      // A. Nếu có đối tượng OpenVFX gắn sẵn trong Prefab
      if (openVfxObject != null)
      {
        openVfxObject.SetActive(true);

        // Kích hoạt toàn bộ ParticleSystem con (Stars, Rays, Circles)
        ParticleSystem[] particles = openVfxObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
          if (ps == null) continue;
          ps.Clear();
          ps.Play(true);
        }

        // Kích hoạt Point Light và chớp sáng bùng nổ (Flash Flare)
        Light[] lights = openVfxObject.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
          if (l == null) continue;
          l.enabled = true;
          float baseIntensity = lightBaseIntensities.ContainsKey(l) ? lightBaseIntensities[l] : l.intensity;
          StartCoroutine(FlashLightRoutine(l, baseIntensity));
        }

        // Tự động tắt sau khoảng thời gian nếu vfxDuration > 0
        if (vfxDuration > 0f)
        {
          if (stopVfxCoroutine != null) StopCoroutine(stopVfxCoroutine);
          stopVfxCoroutine = StartCoroutine(StopVfxRoutine(vfxDuration));
        }
      }
      // B. Dự phòng nếu dùng OpenVFXPrefab bên ngoài
      else if (openVfxPrefab != null)
      {
        GameObject spawned = Instantiate(openVfxPrefab, transform.position + Vector3.up * 0.45f, transform.rotation);
        ParticleSystem[] psList = spawned.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in psList)
        {
          ps.Play(true);
        }
        if (vfxDuration > 0f)
        {
          Destroy(spawned, vfxDuration);
        }
      }
    }

    /// <summary>
    /// Hiệu ứng chớp sáng bùng nổ (Flash Flare) của Point Light khi nắp bật mở
    /// </summary>
    private IEnumerator FlashLightRoutine(Light lightComp, float baseIntensity)
    {
      if (lightComp == null) yield break;

      float duration = 0.4f;
      float elapsed = 0f;
      float peakIntensity = baseIntensity * 1.75f;

      while (elapsed < duration)
      {
        if (lightComp == null) yield break;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        if (t < 0.25f)
        {
          // Dâng vọt lên đỉnh trong 0.1s đầu
          lightComp.intensity = Mathf.Lerp(0f, peakIntensity, t / 0.25f);
        }
        else
        {
          // Dịu dần về cường độ sáng ổn định
          lightComp.intensity = Mathf.Lerp(peakIntensity, baseIntensity, (t - 0.25f) / 0.75f);
        }
        yield return null;
      }

      if (lightComp != null)
      {
        lightComp.intensity = baseIntensity;
      }
    }

    private IEnumerator StopVfxRoutine(float delay)
    {
      yield return new WaitForSeconds(delay);
      if (openVfxObject == null) yield break;

      // 1. Dừng phát thêm hạt (các hạt đang bay tiếp tục bay nốt rồi tan dần tự nhiên)
      ParticleSystem[] particles = openVfxObject.GetComponentsInChildren<ParticleSystem>();
      foreach (ParticleSystem ps in particles)
      {
        if (ps != null)
        {
          ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
      }

      // 2. Giảm dần ánh sáng Point Light trong 1.0s (fade out êm dịu)
      Light[] lights = openVfxObject.GetComponentsInChildren<Light>();
      float fadeDuration = 1.0f;
      float fadeElapsed = 0f;

      Dictionary<Light, float> currentIntensities = new Dictionary<Light, float>();
      foreach (Light l in lights)
      {
        if (l != null) currentIntensities[l] = l.intensity;
      }

      while (fadeElapsed < fadeDuration)
      {
        fadeElapsed += Time.deltaTime;
        float factor = Mathf.Clamp01(1f - (fadeElapsed / fadeDuration));
        foreach (Light l in lights)
        {
          if (l != null && currentIntensities.ContainsKey(l))
          {
            l.intensity = currentIntensities[l] * factor;
          }
        }
        yield return null;
      }

      foreach (Light l in lights)
      {
        if (l != null)
        {
          l.enabled = false;
          if (lightBaseIntensities.ContainsKey(l))
          {
            l.intensity = lightBaseIntensities[l];
          }
        }
      }

      // 3. Chờ thêm 1.0s để toàn bộ hạt sao, tia sáng tan hết trong không khí rồi mới tắt GameObject
      yield return new WaitForSeconds(1.0f);

      if (openVfxObject != null)
      {
        openVfxObject.SetActive(false);
      }
      stopVfxCoroutine = null;
    }

    private IEnumerator OpenLidRoutine()
    {
      Quaternion startRot = chestLid.localRotation;
      Quaternion targetRot = startRot * Quaternion.Euler(openEulerOffset);
      float progress = 0f;

      while (progress < 1f)
      {
        progress += Time.deltaTime * openSpeed;
        chestLid.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, progress));
        yield return null;
      }

      chestLid.localRotation = targetRot;
    }

    [ContextMenu("Test Open Chest")]
    public void TestOpenInEditor()
    {
      OpenChest();
    }

    [ContextMenu("Reset Chest (Close)")]
    public void ResetChestInEditor()
    {
      isOpen = false;
      if (stopVfxCoroutine != null)
      {
        StopCoroutine(stopVfxCoroutine);
        stopVfxCoroutine = null;
      }
      if (chestLid != null && originalLidRot != default)
      {
        chestLid.localRotation = originalLidRot;
      }
      if (openVfxObject != null)
      {
        // Phục hồi lại particle emission và xóa hạt cũ
        ParticleSystem[] particles = openVfxObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
          if (ps != null)
          {
            var emission = ps.emission;
            emission.enabled = true;
            ps.Clear();
          }
        }

        // Phục hồi lại ánh sáng gốc
        Light[] lights = openVfxObject.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
          if (l != null)
          {
            l.enabled = false;
            if (lightBaseIntensities.ContainsKey(l))
            {
              l.intensity = lightBaseIntensities[l];
            }
          }
        }

        openVfxObject.SetActive(false);
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
  }
}
