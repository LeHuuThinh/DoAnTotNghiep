using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Điều khiển camera 3D xem trước nhân vật và cho phép xoay 360 độ bằng cách kéo chuột trên RawImage.
  /// Tích hợp hệ thống Smart Adaptive Studio Lighting (tự điều chỉnh theo Dungeon / LandScene)
  /// và Camera-Isolated Lighting Pass để tuyệt đối không làm ảnh hưởng tới ánh sáng ngoài thế giới game.
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

    [Tooltip("Cường độ sáng mặc định của đèn phụ")]
    [Range(0f, 4f)]
    [SerializeField] private float previewLightIntensity = 1.2f;

    [Tooltip("Bán kính chiếu sáng (chỉ chiếu trong cự ly gần nhân vật)")]
    [Range(1f, 8f)]
    [SerializeField] private float previewLightRange = 5.0f;

    [Tooltip("Màu ánh sáng phụ")]
    [SerializeField] private Color previewLightColor = new Color(1.0f, 0.96f, 0.90f);

    [Header("=== ÁNH SÁNG THÍCH ỨNG (SMART ADAPTIVE LIGHTING) ===")]
    [Tooltip("Tự động nhận diện ánh sáng môi trường để điều chỉnh đèn (Dungeon vs Land)")]
    [SerializeField] private bool autoAdaptiveIntensity = true;

    [Tooltip("Cường độ sáng khi ở trong Hầm ngục (DungeonScene hoặc môi trường tối không có mặt trời)")]
    [Range(0f, 4f)]
    [SerializeField] private float dungeonLightIntensity = 1.3f;

    [Tooltip("Cường độ sáng khi ở Ngoài Làng (LandScene hoặc môi trường sáng ban ngày đã có mặt trời)")]
    [Range(0f, 2f)]
    [SerializeField] private float landLightIntensity = 0.25f;

    [Tooltip("Độ lệch vị trí đèn phụ để tạo góc chiếu Key Light nghệ thuật 3/4 (thay vì chiếu thẳng tâm)")]
    [SerializeField] private Vector3 lightLocalOffset = new Vector3(0.55f, 0.40f, 0.15f);

    [Tooltip("Góc xoay của đèn phụ hướng nhẹ vào nhân vật")]
    [SerializeField] private Vector3 lightLocalEuler = new Vector3(6f, -12f, 0f);

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

    private void OnEnable()
    {
      // Đăng ký hook URP để cô lập ánh sáng preview: CHỈ bật khi previewCamera render
      RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
      RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
    }

    private void OnDisable()
    {
      RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
      RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;

      if (previewLight != null)
      {
        previewLight.enabled = false;
      }
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
        previewLight.type = previewLightType;
        previewLight.range = previewLightRange;
        previewLight.color = previewLightColor;
        previewLight.transform.localPosition = lightLocalOffset;
        previewLight.transform.localRotation = Quaternion.Euler(lightLocalEuler);

        if (previewLightType == LightType.Spot)
        {
          previewLight.spotAngle = 70f;
          previewLight.innerSpotAngle = 45f;
        }

        UpdateAdaptiveLighting();
      }

      if (isPreviewActive && cachedPlayerTransform != null && previewCamera != null)
      {
        UpdateCameraPosition();
      }
    }

    /// <summary>
    /// URP Hook: Khi một Camera bắt đầu quá trình Culling và Render
    /// </summary>
    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
      if (cam == previewCamera)
      {
        // CHỈ khi chính previewCamera vẽ thì mới kích hoạt đèn xem trước
        if (previewLight != null && isPreviewActive && usePreviewLight)
        {
          UpdateAdaptiveLighting();
          previewLight.enabled = true;
        }
      }
      else
      {
        // Khi MainCamera hoặc bất kỳ camera thế giới nào vẽ: TẮT HOÀN TOÀN đèn preview
        // để thế giới game ngoài đời tuyệt đối không bị tia sáng rọi vào người Player!
        if (previewLight != null && previewLight.enabled)
        {
          previewLight.enabled = false;
        }
      }
    }

    /// <summary>
    /// URP Hook: Sau khi Camera hoàn tất vẽ
    /// </summary>
    private void HandleEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
      if (cam == previewCamera)
      {
        if (previewLight != null)
        {
          previewLight.enabled = false;
        }
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

        int playerLayer = cachedPlayerTransform != null ? cachedPlayerTransform.gameObject.layer : LayerMask.NameToLayer("Player");
        if (playerLayer < 0) playerLayer = 3;

        // Chỉ soi Layer Player -> Dungeon và tường hầm ngục sẽ hoàn toàn tàng hình
        previewCamera.cullingMask = 1 << playerLayer;
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
        // Mặc định giữ đèn ở trạng thái tắt, chỉ bật khi previewCamera render
        previewLight.enabled = false;
        previewLight.type = previewLightType;

        int playerLayer = cachedPlayerTransform != null ? cachedPlayerTransform.gameObject.layer : LayerMask.NameToLayer("Player");
        if (playerLayer < 0) playerLayer = 3;
        previewLight.cullingMask = 1 << playerLayer; // Chỉ rọi sáng nhân vật (Layer Player)

        previewLight.range = previewLightRange;
        previewLight.color = previewLightColor;

        // Vị trí và góc chiếu Studio Key Light chếch 3/4
        previewLight.transform.localPosition = lightLocalOffset;
        previewLight.transform.localRotation = Quaternion.Euler(lightLocalEuler);

        if (previewLightType == LightType.Spot)
        {
          previewLight.spotAngle = 70f;
          previewLight.innerSpotAngle = 45f;
        }

        UpdateAdaptiveLighting();
      }
    }

    /// <summary>
    /// Tự động tính toán cường độ ánh sáng thích ứng theo môi trường (DungeonScene vs LandScene)
    /// </summary>
    private void UpdateAdaptiveLighting()
    {
      if (previewLight == null) return;

      float targetIntensity = previewLightIntensity;

      if (autoAdaptiveIntensity)
      {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isBrightScene = sceneName.IndexOf("Land", StringComparison.OrdinalIgnoreCase) >= 0;

        // Nếu scene hiện tại không chứa tên Land, kiểm tra xem có Directional Light nào sáng mạnh không
        if (!isBrightScene)
        {
          var sun = RenderSettings.sun;
          if (sun != null && sun.enabled && sun.intensity > 1.0f)
          {
            isBrightScene = true;
          }
        }

        targetIntensity = isBrightScene ? landLightIntensity : dungeonLightIntensity;
      }

      previewLight.intensity = targetIntensity;
    }

    /// <summary>
    /// Đảm bảo tất cả các Renderer của Player và Vũ khí (VisualHolder/Sword) đều thuộc Layer Player
    /// để Camera xem trước không bao giờ bị tình trạng thiếu kiếm hay thiếu phụ kiện
    /// </summary>
    private void EnsurePlayerAndWeaponLayers()
    {
      if (cachedPlayerTransform == null) return;

      int playerLayer = cachedPlayerTransform.gameObject.layer;
      if (playerLayer < 0) playerLayer = 3;

      if (previewCamera != null)
      {
        previewCamera.cullingMask = 1 << playerLayer;
      }
      if (previewLight != null)
      {
        previewLight.cullingMask = 1 << playerLayer;
      }

      // Quét các đối tượng con liên quan tới VisualHolder hoặc Sword để đồng bộ Layer
      foreach (var tr in cachedPlayerTransform.GetComponentsInChildren<Transform>(true))
      {
        if (tr.name == "VisualHolder" || tr.name == "Sword")
        {
          SetLayerRecursively(tr.gameObject, playerLayer);
        }
      }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
      if (obj == null) return;
      obj.layer = newLayer;
      for (int i = 0; i < obj.transform.childCount; i++)
      {
        SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
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
        EnsurePlayerAndWeaponLayers();
        DisableObstructingBackgroundRaycast();
        currentYawOffset = 0f; // Reset về hướng nhìn đối diện mặt người chơi
        UpdateAdaptiveLighting();
        UpdateCameraPosition();
      }
      else
      {
        if (previewLight != null)
        {
          previewLight.enabled = false;
        }
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
      RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
      RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;

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