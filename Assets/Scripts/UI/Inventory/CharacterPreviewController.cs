using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Điều khiển camera 3D xem trước nhân vật và cho phép xoay 360 độ bằng cách kéo chuột trên RawImage
  /// </summary>
  public class CharacterPreviewController : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler
  {
    [Header("=== UI REFERENCES ===")]
    [Tooltip("RawImage hiển thị hình ảnh từ RenderTexture")]
    [SerializeField] private RawImage previewRawImage;

    [Tooltip("RenderTexture dùng cho Camera xem trước (Tự động sinh nếu để trống)")]
    [SerializeField] private RenderTexture previewRenderTexture;

    [Header("=== CAMERA SETTINGS (TÙY CHỈNH KHOẢNG CÁCH & GÓC NHÌN) ===")]
    [Tooltip("Camera chuyên dụng để soi nhân vật (Tự động tạo nếu để trống)")]
    [SerializeField] private Camera previewCamera;

    [Tooltip("Khoảng cách từ camera tới nhân vật (Tăng lên để thấy trọn vẹn cả đầu và chân)")]
    [Range(1.5f, 6.0f)]
    [SerializeField] private float previewDistance = 3.4f;

    [Tooltip("Độ cao tâm ngắm của camera (tính từ chân người chơi lên)")]
    [Range(0.2f, 2.0f)]
    [SerializeField] private float targetHeightOffset = 0.95f;

    [Tooltip("Góc nghiêng từ trên xuống (Pitch)")]
    [Range(-20f, 30f)]
    [SerializeField] private float pitchAngle = 2f;

    [Tooltip("Góc nhìn FOV của camera xem trước")]
    [Range(20f, 60f)]
    [SerializeField] private float fieldOfView = 35f;

    [Tooltip("Tốc độ xoay nhân vật khi kéo chuột trái")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float dragSensitivity = 0.6f;

    [Header("=== MÀU NỀN KHUNG SOI ===")]
    [Tooltip("Màu nền buồng soi nhân vật")]
    [SerializeField] private Color backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);

    [Header("=== ÁNH SÁNG PHỤ TRỢ (PREVIEW LIGHT) ===")]
    [Tooltip("Bật đèn phụ trợ dịu nhẹ gắn kèm camera")]
    [SerializeField] private bool usePreviewLight = true;

    [Tooltip("Loại đèn chiếu phụ (Spot hoặc Point để TUYỆT ĐỐI KHÔNG làm sáng phòng hầm ngục)")]
    [SerializeField] private LightType previewLightType = LightType.Spot;

    [Tooltip("Cường độ sáng đèn phụ")]
    [Range(0f, 4f)]
    [SerializeField] private float previewLightIntensity = 1.2f;

    [Tooltip("Bán kính chiếu sáng (chỉ chiếu trong cự ly gần nhân vật)")]
    [Range(1f, 8f)]
    [SerializeField] private float previewLightRange = 5.0f;

    [Tooltip("Màu ánh sáng phụ")]
    [SerializeField] private Color previewLightColor = new Color(1.0f, 0.96f, 0.90f);

    private Transform cachedPlayerTransform;
    private Light previewLight;
    private float currentYawOffset = 0f;
    private bool isPreviewActive = false;

    private void Awake()
    {
      if (previewRawImage == null)
      {
        previewRawImage = GetComponent<RawImage>();
      }

      // Đảm bảo RawImage luôn nhận được sự kiện chuột
      if (previewRawImage != null)
      {
        previewRawImage.raycastTarget = true;
      }

      // Tự động tắt Raycast Target trên PreviewBG nếu nó nằm đè phía trên làm chặn chuột
      DisableObstructingBackgroundRaycast();

      // Nâng khoảng cách mặc định nếu inspector cũ còn lưu giá trị quá gần (2.4f)
      if (previewDistance < 2.8f)
      {
        previewDistance = 3.4f;
      }

      EnsureRenderTexture();
      EnsurePreviewCamera();
    }

    private void Start()
    {
      CachePlayer();
      // Mặc định tắt camera khi chưa mở Túi đồ
      EnablePreview(false);
    }

    private void LateUpdate()
    {
      if (!isPreviewActive || cachedPlayerTransform == null || previewCamera == null) return;
      UpdateCameraPosition();
    }

    private void OnValidate()
    {
      if (previewCamera != null)
      {
        previewCamera.fieldOfView = fieldOfView;
        previewCamera.backgroundColor = backgroundColor;
      }

      if (previewLight != null)
      {
        previewLight.enabled = usePreviewLight;
        previewLight.type = previewLightType;
        previewLight.intensity = previewLightIntensity;
        previewLight.range = previewLightRange;
        previewLight.color = previewLightColor;
        if (previewLightType == LightType.Spot)
        {
          previewLight.spotAngle = 65f;
          previewLight.innerSpotAngle = 40f;
        }
      }

      if (isPreviewActive && cachedPlayerTransform != null && previewCamera != null)
      {
        UpdateCameraPosition();
      }
    }

    /// <summary>
    /// Kiểm tra nếu có PreviewBG nằm cùng cha hoặc con thì tắt raycastTarget để không chặn sự kiện Drag
    /// </summary>
    private void DisableObstructingBackgroundRaycast()
    {
      if (transform.parent != null)
      {
        var bg = transform.parent.Find("PreviewBG")?.GetComponent<Graphic>();
        if (bg != null && bg.raycastTarget)
        {
          bg.raycastTarget = false;
        }
      }
    }

    private void CachePlayer()
    {
      if (cachedPlayerTransform != null) return;

      var playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        cachedPlayerTransform = playerObj.transform;
      }
      else
      {
        var playerCtrl = FindObjectOfType<PlayerController>();
        if (playerCtrl != null)
        {
          cachedPlayerTransform = playerCtrl.transform;
        }
      }
    }

    private void EnsureRenderTexture()
    {
      if (previewRenderTexture == null)
      {
        previewRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
          name = "CharacterPreviewRT_Auto",
          antiAliasing = 2,
          filterMode = FilterMode.Bilinear
        };
      }

      if (previewRawImage != null)
      {
        previewRawImage.texture = previewRenderTexture;
      }
    }

    private void EnsurePreviewCamera()
    {
      if (previewCamera == null)
      {
        // Kiểm tra xem đã có sẵn trong scene chưa
        var existingObj = GameObject.Find("CharacterPreviewCamera");
        if (existingObj != null)
        {
          previewCamera = existingObj.GetComponent<Camera>();
        }
        else
        {
          // Tự động tạo Camera xem trước chuyên dụng
          GameObject camObj = new GameObject("CharacterPreviewCamera");
          previewCamera = camObj.AddComponent<Camera>();
          previewCamera.nearClipPlane = 0.1f;
          previewCamera.farClipPlane = 20f;
        }
      }

      if (previewCamera != null)
      {
        previewCamera.fieldOfView = fieldOfView;
        // Chỉ soi Layer 3 (Player) -> Dungeon và tường hầm ngục sẽ hoàn toàn tàng hình
        previewCamera.cullingMask = 1 << 3;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.targetTexture = previewRenderTexture;

        // Cấu hình nguồn sáng phụ trợ cho camera
        SetupPreviewLight();
      }
    }

    private void SetupPreviewLight()
    {
      if (previewCamera == null) return;

      // Tìm đèn cũ nếu có
      if (previewLight == null)
      {
        var existingLight = previewCamera.GetComponentInChildren<Light>();
        if (existingLight != null)
        {
          previewLight = existingLight;
        }
        else
        {
          GameObject lightObj = new GameObject("CharacterPreviewLight");
          lightObj.transform.SetParent(previewCamera.transform, false);
          previewLight = lightObj.AddComponent<Light>();
        }
      }

      if (previewLight != null)
      {
        previewLight.enabled = usePreviewLight;
        // QUAN TRỌNG: TUYỆT ĐỐI không dùng Directional Light vì sẽ làm sáng rực cả hầm ngục
        previewLight.type = previewLightType;
        previewLight.cullingMask = 1 << 3; // Chỉ rọi sáng nhân vật (Layer 3)
        previewLight.intensity = previewLightIntensity;
        previewLight.range = previewLightRange;
        previewLight.color = previewLightColor;
        if (previewLightType == LightType.Spot)
        {
          previewLight.spotAngle = 65f;
          previewLight.innerSpotAngle = 40f;
        }
      }
    }

    /// <summary>
    /// Bật / Tắt hệ thống Camera xem trước khi mở / đóng túi đồ
    /// </summary>
    public void EnablePreview(bool enable)
    {
      isPreviewActive = enable;

      if (previewCamera != null)
      {
        previewCamera.gameObject.SetActive(enable);
      }

      if (enable)
      {
        CachePlayer();
        DisableObstructingBackgroundRaycast();
        currentYawOffset = 0f; // Reset về hướng nhìn đối diện mặt người chơi
        UpdateCameraPosition();
      }
    }

    /// <summary>
    /// Tính toán vị trí và góc nhìn của Camera quay quanh nhân vật
    /// </summary>
    private void UpdateCameraPosition()
    {
      if (cachedPlayerTransform == null || previewCamera == null) return;

      Vector3 targetCenter = cachedPlayerTransform.position + Vector3.up * targetHeightOffset;

      // Góc đối diện trước mặt nhân vật = player.eulerAngles.y + 180 độ
      float currentYaw = cachedPlayerTransform.eulerAngles.y + 180f + currentYawOffset;
      Quaternion rotation = Quaternion.Euler(pitchAngle, currentYaw, 0f);

      Vector3 camPos = targetCenter + rotation * new Vector3(0f, 0f, -previewDistance);

      previewCamera.transform.position = camPos;
      previewCamera.transform.LookAt(targetCenter);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
      // Bắt buộc cần có để EventSystem xác định GameObject này là mục tiêu nhận sự kiện Drag
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
      // Kéo chuột trái sang ngang để xoay nhân vật
      currentYawOffset -= eventData.delta.x * dragSensitivity;
      UpdateCameraPosition();
    }

    private void OnDestroy()
    {
      if (previewCamera != null && previewCamera.gameObject != null)
      {
        // Dọn dẹp camera động nếu được tạo trong runtime
        if (previewCamera.gameObject.name == "CharacterPreviewCamera")
        {
          Destroy(previewCamera.gameObject);
        }
      }
    }
  }
}
