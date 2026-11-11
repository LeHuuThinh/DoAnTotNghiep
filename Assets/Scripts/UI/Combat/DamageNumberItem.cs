using System;
using TMPro;
using UnityEngine;

namespace InfiniteLabyrinth.UI.Combat
{
  /// <summary>
  /// Quản lý từng con số sát thương bay lên (Floating Damage Number Item)
  /// Sử dụng TextMeshPro 3D Mesh với hiệu ứng Pop & Physics Arc (búng nảy và rơi vồng cung)
  /// Hỗ trợ nạp Preset Gradient 4 góc (TMP_ColorGradient) và tự động Billboard hướng về Camera.
  /// </summary>
  [RequireComponent(typeof(TextMeshPro))]
  public class DamageNumberItem : MonoBehaviour
  {
    private TextMeshPro tmpText;
    private Camera targetCamera;
    private float timer = 0f;
    private float totalDuration = 0.75f;
    private float popDuration = 0.10f;
    private float baseScale = 1.0f;
    private float peakScale = 1.45f;
    private Vector3 currentVelocity;
    private Color mainColor;
    private Action<DamageNumberItem> onFinishedCallback;
    private bool isRunning = false;
    private DamageStyleSetting currentStyle;
    private SpriteRenderer critIconRenderer;
    private Color critIconColor = Color.white;
    private bool currentIsCrit = false;
    private DamageNumberType currentDamageType = DamageNumberType.Normal;
    private static Sprite s_CachedCritSprite;

    [Header("=== CẤU HÌNH ICON CHÍ MẠNG (CRIT ICON) ===")]
    [Tooltip("Bật/tắt hiển thị icon Crit bên cạnh số bạo kích. Nếu tắt, chỉ phóng to số")]
    [SerializeField] private bool showCritIcon = true;
    [Tooltip("Tỷ lệ thu nhỏ của Icon Crit (Mặc định 0.085 vừa vặn với cỡ chữ, không che số)")]
    [Range(0.02f, 0.4f)]
    [SerializeField] private float critIconScale = 0.085f;
    [Tooltip("Khoảng cách lệch sang trái so với mép con số")]
    [SerializeField] private float critIconOffsetLeft = 0.08f;

    [Header("=== CẤU HÌNH PHÓNG TO SỐ KHI CRIT ===")]
    [Tooltip("Hệ số phóng to số sát thương khi bạo kích (Mặc định 1.15x để số không bị quá to che khuất quái)")]
    [Range(1.0f, 1.6f)]
    [SerializeField] private float critScaleMultiplier = 1.15f;

    private Vector3 initialPrefabScale = Vector3.one;

    public bool ShowCritIcon
    {
      get => showCritIcon;
      set => showCritIcon = value;
    }

    public float CritIconScale
    {
      get => critIconScale;
      set => critIconScale = Mathf.Max(0.01f, value);
    }

    public float CritIconOffsetLeft
    {
      get => critIconOffsetLeft;
      set => critIconOffsetLeft = value;
    }

    public float CritScaleMultiplier
    {
      get => critScaleMultiplier;
      set => critScaleMultiplier = Mathf.Max(1.0f, value);
    }

    private void Awake()
    {
      initialPrefabScale = transform.localScale;
      EnsureTextMeshPro();
      EnsureCritIcon();
    }

    private void EnsureCritIcon()
    {
      if (critIconRenderer == null)
      {
        Transform child = transform.Find("CritIcon");
        if (child == null)
        {
          GameObject go = new GameObject("CritIcon");
          go.transform.SetParent(transform, false);
          child = go.transform;
        }
        critIconRenderer = child.GetComponent<SpriteRenderer>();
        if (critIconRenderer == null)
        {
          critIconRenderer = child.gameObject.AddComponent<SpriteRenderer>();
        }
      }

      if (critIconRenderer.sprite == null)
      {
        if (s_CachedCritSprite == null)
        {
          s_CachedCritSprite = Resources.Load<Sprite>("CritDmg");
        }
        critIconRenderer.sprite = s_CachedCritSprite;
      }

      if (tmpText != null && critIconRenderer != null)
      {
        critIconRenderer.sortingOrder = tmpText.sortingOrder + 1;
      }
    }

    private void EnsureTextMeshPro()
    {
      if (tmpText == null)
      {
        tmpText = GetComponent<TextMeshPro>();
        if (tmpText == null)
        {
          tmpText = gameObject.AddComponent<TextMeshPro>();
          tmpText.enableWordWrapping = false;
          tmpText.overflowMode = TextOverflowModes.Overflow;
          tmpText.sortingOrder = 2000;
        }
      }
      // Luôn đảm bảo alignment là Center để số mở rộng đều từ tâm, không bao giờ bị giật sang bên
      tmpText.alignment = TextAlignmentOptions.Center;
    }

    public void Setup(TMP_FontAsset font)
    {
      EnsureTextMeshPro();
      if (font != null)
      {
        tmpText.font = font;
      }
    }

    /// <summary>
    /// Kích hoạt số sát thương bay ra tại vị trí va chạm với cấu hình kích cỡ & màu sắc riêng biệt
    /// </summary>
    public void Spawn(float amount, Vector3 worldPosition, DamageNumberType type, DamageStyleSetting style, Camera cam, Action<DamageNumberItem> onFinished, bool isCrit = false)
    {
      EnsureTextMeshPro();
      EnsureCritIcon();
      targetCamera = cam != null ? cam : (Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());
      onFinishedCallback = onFinished;
      timer = 0f;
      isRunning = true;
      currentStyle = style;
      currentIsCrit = isCrit;
      currentDamageType = type;
      gameObject.SetActive(true);

      // 1. Định dạng chuỗi hiển thị
      int intAmount = Mathf.Max(1, Mathf.RoundToInt(amount));
      tmpText.text = intAmount.ToString();

      // 2. Cấu hình Kích thước & Thời lượng từ Style Setting
      if (style != null)
      {
        baseScale = style.baseScale;
        peakScale = style.peakScale;
        totalDuration = style.duration;

        // Áp dụng Gradient Preset nếu có, ngược lại dùng màu Fallback
        if (style.gradientPreset != null)
        {
          tmpText.enableVertexGradient = true;
          tmpText.colorGradientPreset = style.gradientPreset;
          mainColor = Color.white;
          tmpText.color = Color.white;
        }
        else
        {
          tmpText.enableVertexGradient = false;
          mainColor = style.fallbackColor;
          tmpText.color = mainColor;
        }
      }
      else
      {
        baseScale = 1.0f;
        peakScale = 1.45f;
        totalDuration = 0.75f;
        tmpText.enableVertexGradient = false;
        mainColor = Color.white;
        tmpText.color = mainColor;
      }

      // Xử lý hiển thị Icon bên cạnh con số khi Chí Mạng (Crit)
      if (isCrit)
      {
        baseScale *= critScaleMultiplier;
        peakScale *= critScaleMultiplier;

        if (showCritIcon && critIconRenderer != null)
        {
          if (s_CachedCritSprite == null) s_CachedCritSprite = Resources.Load<Sprite>("CritDmg");
          critIconRenderer.sprite = s_CachedCritSprite;

          // 1. Đổi màu icon Crit theo màu của sát thương (giống trước đây)
          if (style != null)
          {
            if (style.gradientPreset != null)
            {
              // Lấy màu đỉnh trên của Gradient 4 góc để icon rực rỡ đồng bộ với số sát thương
              critIconColor = style.gradientPreset.topLeft;
            }
            else if (style.fallbackColor != Color.clear)
            {
              critIconColor = style.fallbackColor;
            }
            else
            {
              critIconColor = Color.white;
            }
          }
          else
          {
            critIconColor = Color.white;
          }

          critIconRenderer.color = critIconColor;
          critIconRenderer.gameObject.SetActive(true);

          // 2. Định vị icon ngay sát mép trái của con số
          tmpText.ForceMeshUpdate();
          float textHalfWidth = Mathf.Max(0.12f, tmpText.bounds.extents.x);
          critIconRenderer.transform.localScale = Vector3.one * critIconScale;
          critIconRenderer.transform.localPosition = new Vector3(-textHalfWidth - critIconOffsetLeft, 0.02f, 0f);
        }
        else if (critIconRenderer != null)
        {
          critIconRenderer.gameObject.SetActive(false);
        }
      }
      else
      {
        // Các loại sát thương khác (kể cả Bleed) đều tắt icon, giữ số sát thương rõ ràng sạch sẽ
        if (critIconRenderer != null)
        {
          critIconRenderer.gameObject.SetActive(false);
        }
      }

      // 3. Vị trí xuất phát:
      // Offset lên cao (+0.65m) và kéo nhẹ về phía Camera (+0.35m) để số luôn nằm ở mặt tiền mục tiêu, không bị chìm trong mesh
      Vector3 camRight = targetCamera != null ? targetCamera.transform.right : Vector3.right;
      Vector3 dirToCam = targetCamera != null ? (targetCamera.transform.position - worldPosition).normalized : Vector3.back;
      Vector3 randomSpread = camRight * UnityEngine.Random.Range(-0.35f, 0.35f);
      
      transform.position = worldPosition + Vector3.up * 0.65f + dirToCam * 0.35f + randomSpread;

      // 4. Vận tốc búng vồng cung (Physics Arc): Búng vọt lên và tạt nhẹ sang bên
      float lateralSpeed = UnityEngine.Random.Range(-0.85f, 0.85f);
      float upwardSpeed = UnityEngine.Random.Range(3.2f, 4.4f);
      if (type == DamageNumberType.SkillR)
      {
        upwardSpeed = 5.2f; // Ult uy lực cực mạnh nên búng cao hơn
      }
      else if (type == DamageNumberType.Bleed)
      {
        upwardSpeed = UnityEngine.Random.Range(2.0f, 2.7f); // DoT rỉ máu búng nhẹ nhàng tại chỗ
        lateralSpeed = UnityEngine.Random.Range(-0.4f, 0.4f);
      }
      if (isCrit)
      {
        upwardSpeed += 0.8f; // Crit búng vọt cao hơn
      }
      currentVelocity = Vector3.up * upwardSpeed + camRight * lateralSpeed;

      // Scale ban đầu lúc vừa chạm (kết hợp với scale của Prefab nếu có)
      transform.localScale = initialPrefabScale * (baseScale * 0.45f);

      // Đặt lại alpha hiển thị
      tmpText.alpha = 1.0f;

      // Hướng mặt chữ về mắt nhìn camera ngay tức thì
      UpdateBillboard();
    }

    private void Update()
    {
      if (!isRunning) return;

      timer += Time.deltaTime;

      // 1. Cập nhật vị trí rơi vồng cung (Trọng lực gia tốc)
      currentVelocity.y -= 8.5f * Time.deltaTime;
      transform.position += currentVelocity * Time.deltaTime;

      // 2. Cập nhật Scale (Pha 1 Pop đanh ở 0.10s đầu, sau đó co về Base Scale)
      float currentScale;
      if (timer < popDuration)
      {
        float popT = timer / popDuration;
        currentScale = Mathf.Lerp(baseScale * 0.45f, peakScale, popT);
      }
      else if (timer < popDuration + 0.12f)
      {
        float settleT = (timer - popDuration) / 0.12f;
        currentScale = Mathf.Lerp(peakScale, baseScale, settleT);
      }
      else
      {
        float fadeT = Mathf.Clamp01((timer - (popDuration + 0.12f)) / (totalDuration - (popDuration + 0.12f)));
        currentScale = Mathf.Lerp(baseScale, baseScale * 0.85f, fadeT);
      }
      transform.localScale = initialPrefabScale * currentScale;

      // 3. Cập nhật Alpha (Mờ dần ở 35% thời lượng cuối)
      float alpha = 1.0f;
      float fadeStartTime = totalDuration * 0.65f;
      if (timer > fadeStartTime)
      {
        alpha = Mathf.Clamp01(1.0f - (timer - fadeStartTime) / (totalDuration - fadeStartTime));
      }
      // tmpText.alpha tự động nhân mờ đồng đều cho cả Gradient 4 góc lẫn màu phẳng
      tmpText.alpha = alpha;

      // Đồng bộ độ mờ alpha cho icon bên cạnh số (Crit)
      if (currentIsCrit && showCritIcon && critIconRenderer != null && critIconRenderer.gameObject.activeSelf)
      {
        critIconRenderer.color = new Color(critIconColor.r, critIconColor.g, critIconColor.b, alpha);
      }

      // 4. Luôn quay mặt chữ về mắt nhìn camera (Billboard)
      UpdateBillboard();

      // 5. Kết thúc vòng đời và tự động hoàn trả về Object Pool
      if (timer >= totalDuration)
      {
        isRunning = false;
        if (critIconRenderer != null) critIconRenderer.gameObject.SetActive(false);
        gameObject.SetActive(false);
        onFinishedCallback?.Invoke(this);
      }
    }

    private void UpdateBillboard()
    {
      if (targetCamera == null) targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
      if (targetCamera != null)
      {
        transform.rotation = targetCamera.transform.rotation;
      }
    }
  }
}
