using System;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Component phát hoạt ảnh từ chuỗi Sprite cắt từ Sprite Sheet (Hỗ trợ cả UI Image và 3D SpriteRenderer).
  /// Có thể kéo vào bất kỳ UI Canvas nào hoặc đặt trực tiếp vào Scene 3D.
  /// Hỗ trợ [ExecuteAlways] để xem trước hoạt ảnh ngay trong Editor mà không cần nhấn Play!
  /// </summary>
  [ExecuteAlways]
  [DisallowMultipleComponent]
  public class UISpriteSheetAnimator : MonoBehaviour
  {
    [Header("=== CẤU HÌNH KHUNG HÌNH (SPRITE FRAMES) ===")]
    [Tooltip("Danh sách các frame Sprite (cắt từ Sprite Sheet)")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Tốc độ khung hình trên giây (FPS). Ví dụ: 24, 30 hoặc 60")]
    [Range(1f, 120f)]
    [SerializeField] private float fps = 24f;

    [Tooltip("Tự động phát hoạt ảnh khi bật GameObject")]
    [SerializeField] private bool playOnAwake = true;

    [Tooltip("Lặp lại hoạt ảnh vô hạn khi hết chuỗi frame")]
    [SerializeField] private bool loop = true;

    [Tooltip("Dùng unscaledDeltaTime (để hoạt ảnh vẫn chạy mượt khi game pause hoặc Time.timeScale = 0)")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("=== XEM TRƯỚC TRONG EDITOR ===")]
    [Tooltip("Bật để xem hoạt ảnh chạy trực tiếp ngay trong Scene & Game View của Unity Editor mà không cần bấm Play!")]
    [SerializeField] private bool previewInEditor = true;

    [Header("=== TARGET COMPONENTS ===")]
    [Tooltip("Target UI Image (nếu component nằm trên UI Canvas)")]
    [SerializeField] private Image targetImage;

    [Tooltip("Target SpriteRenderer (nếu component nằm trên GameObject 3D trong Scene)")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    // Runtime state
    private int currentFrameIndex = 0;
    private float timer = 0f;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;
    public int FrameCount => frames != null ? frames.Length : 0;
    public int CurrentFrameIndex => currentFrameIndex;
    public float Fps { get => fps; set => fps = Mathf.Max(1f, value); }
    public Sprite[] Frames { get => frames; set => frames = value; }
    public bool PreviewInEditor { get => previewInEditor; set => previewInEditor = value; }

    private void Awake()
    {
      EnsureTargetComponents();
    }

    private void OnEnable()
    {
      EnsureTargetComponents();
      if (Application.isPlaying)
      {
        if (playOnAwake) Play();
      }
      else
      {
        if (previewInEditor) Play();
      }
    }

    private void EnsureTargetComponents()
    {
      if (targetImage == null) targetImage = GetComponent<Image>();
      if (targetSpriteRenderer == null) targetSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play()
    {
      isPlaying = true;
    }

    public void Pause()
    {
      isPlaying = false;
    }

    public void Stop()
    {
      isPlaying = false;
      currentFrameIndex = 0;
      timer = 0f;
      ApplyCurrentFrame();
    }

    public void SetFrame(int index)
    {
      if (frames == null || frames.Length == 0) return;
      currentFrameIndex = Mathf.Clamp(index, 0, frames.Length - 1);
      ApplyCurrentFrame();
    }

    private void Update()
    {
      if (!Application.isPlaying && !previewInEditor) return;
      if (!isPlaying || frames == null || frames.Length == 0) return;

      float dt = (useUnscaledTime || !Application.isPlaying) ? Time.unscaledDeltaTime : Time.deltaTime;
      timer += dt;
      float frameDuration = 1f / Mathf.Max(1f, fps);

      if (timer >= frameDuration)
      {
        int advance = Mathf.FloorToInt(timer / frameDuration);
        timer %= frameDuration;

        currentFrameIndex += advance;
        if (currentFrameIndex >= frames.Length)
        {
          if (loop)
          {
            currentFrameIndex %= frames.Length;
          }
          else
          {
            currentFrameIndex = frames.Length - 1;
            isPlaying = false;
          }
        }

        ApplyCurrentFrame();
      }
    }

    public void ApplyCurrentFrame()
    {
      if (frames == null || frames.Length == 0) return;
      Sprite sprite = frames[Mathf.Clamp(currentFrameIndex, 0, frames.Length - 1)];

      if (targetImage != null)
      {
        targetImage.sprite = sprite;
      }
      if (targetSpriteRenderer != null)
      {
        targetSpriteRenderer.sprite = sprite;
      }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      EnsureTargetComponents();
      ApplyCurrentFrame();
    }

    /// <summary>
    /// Nạp toàn bộ sub-sprites từ file ảnh Sprite Sheet theo thứ tự tự nhiên (0..N).
    /// </summary>
    public void LoadFramesFromAssetPath(string assetPath)
    {
      UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
      System.Collections.Generic.List<Sprite> list = new System.Collections.Generic.List<Sprite>();
      foreach (var a in assets)
      {
        if (a is Sprite s) list.Add(s);
      }

      if (list.Count == 0)
      {
        Debug.LogWarning($"[UISpriteSheetAnimator] Không tìm thấy Sprite nào tại: {assetPath}");
        return;
      }

      list.Sort((a, b) => ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name)));
      frames = list.ToArray();
      currentFrameIndex = 0;
      ApplyCurrentFrame();
      Debug.Log($"<color=green>[UISpriteSheetAnimator]</color> Đã nạp thành công <b>{frames.Length}</b> frames từ '{assetPath}'!");
    }

    private static int ExtractTrailingNumber(string name)
    {
      int lastUnderscore = name.LastIndexOf('_');
      if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int num))
      {
        return num;
      }
      return 0;
    }
#endif
  }
}
