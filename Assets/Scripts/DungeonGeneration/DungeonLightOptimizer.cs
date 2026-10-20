using UnityEngine;

/// <summary>
/// Bộ tối ưu hóa ánh sáng thời gian thực (Light Culling Optimizer) cho Dungeon.
/// Tự động bật/tắt (Enable/Disable) component Point Light dựa trên khoảng cách tới người chơi.
/// Giữ nguyên 100% hình ảnh 3D của ngọn đuốc, chỉ tắt tính toán chiếu sáng GPU khi ở xa.
/// Giúp duy trì FPS cực cao (60 - 120+ FPS) ngay cả khi dungeon có hàng chục ngọn đuốc.
/// </summary>
public class DungeonLightOptimizer : MonoBehaviour
{
  [Header("Cấu hình Tầm Bật Đèn")]
  [Tooltip("Khoảng cách tối đa (mét) từ người chơi để một ngọn đèn được phép bật tính toán ánh sáng")]
  public float maxLightDistance = 28f;

  [Tooltip("Tần suất kiểm tra khoảng cách (giây). 0.2s = 5 lần/giây (siêu nhẹ, không tốn CPU)")]
  public float checkInterval = 0.2f;

  private Transform playerTransform;
  private Light[] allLights;
  private float timer = 0f;

  void Start()
  {
    FindPlayer();
    RefreshLights();
  }

  public void RefreshLights()
  {
    allLights = GetComponentsInChildren<Light>(true);
    UpdateLightsImmediate();
  }

  void Update()
  {
    timer += Time.deltaTime;
    if (timer >= checkInterval)
    {
      timer = 0f;
      UpdateLightsImmediate();
    }
  }

  private void FindPlayer()
  {
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerTransform = player.transform;
    }
    else
    {
      Camera mainCam = Camera.main;
      if (mainCam != null) playerTransform = mainCam.transform;
    }
  }

  public void UpdateLightsImmediate()
  {
    if (playerTransform == null)
    {
      FindPlayer();
      if (playerTransform == null) return;
    }

    if (allLights == null || allLights.Length == 0) return;

    Vector3 pPos = playerTransform.position;
    float maxDistSq = maxLightDistance * maxLightDistance;

    for (int i = 0; i < allLights.Length; i++)
    {
      Light l = allLights[i];
      if (l == null) continue;

      float distSq = (l.transform.position - pPos).sqrMagnitude;
      bool shouldEnable = distSq <= maxDistSq;

      if (l.enabled != shouldEnable)
      {
        l.enabled = shouldEnable;
      }
    }
  }
}
