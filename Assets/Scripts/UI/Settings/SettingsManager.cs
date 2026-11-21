using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ thiết lập hệ thống trò chơi (Graphics, Audio, Display, Controls).
  /// Singleton DontDestroyOnLoad, lưu trữ cấu hình qua PlayerPrefs JSON, tự động áp dụng khi chuyển Scene.
  /// </summary>
  public class SettingsManager : MonoBehaviour
  {
    private static SettingsManager instance;
    private static bool isApplicationQuitting = false;

    public static SettingsManager Instance
    {
      get
      {
        if (isApplicationQuitting) return null;
        if (instance == null)
        {
          EnsureInstance();
        }
        return instance;
      }
    }

    public static bool HasInstance
    {
      get
      {
        if (isApplicationQuitting) return false;
        return Instance != null;
      }
    }

    private const string SETTINGS_PREF_KEY = "InfiniteLabyrinth_Settings_v1";

    [Header("=== CURRENT SETTINGS DATA ===")]
    [SerializeField] private SettingsData currentData = new SettingsData();

    public SettingsData CurrentData => currentData;

    // Events thông báo khi có thay đổi thiết lập
    public event Action OnSettingsChanged;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;
    public event Action<float> OnVoiceVolumeChanged;
    public event Action<float> OnUiVolumeChanged;
    public event Action<float> OnBrightnessChanged;
    public event Action<bool> OnScreenShakeToggled;
    public event Action<bool> OnDamageNumbersToggled;
    public event Action<int> OnLanguageChanged;

    // Fast-access properties cho các hệ thống Gameplay
    public bool ScreenShakeEnabled => currentData.screenShake;
    public bool DamageNumbersEnabled => currentData.damageNumbers;
    public float MouseSensitivity => currentData.mouseSensitivity;
    public bool MouseInvertY => currentData.mouseInvertY;
    public float MasterVolume => currentData.masterVolume;
    public float MusicVolume => currentData.musicVolume;
    public float SfxVolume => currentData.sfxVolume;
    public float VoiceVolume => currentData.voiceVolume;
    public float UiVolume => currentData.uiVolume;
    public float Brightness => currentData.brightness;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeBeforeSceneLoad()
    {
      EnsureInstance();
    }

    public static void EnsureInstance()
    {
      if (isApplicationQuitting) return;
      if (instance != null) return;

      instance = FindFirstObjectByType<SettingsManager>();
      if (instance != null)
      {
        if (Application.isPlaying) DontDestroyOnLoad(instance.gameObject);
        return;
      }

      GameObject go = new GameObject("[SettingsManager]");
      instance = go.AddComponent<SettingsManager>();
      if (Application.isPlaying) DontDestroyOnLoad(go);
    }

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      if (Application.isPlaying) DontDestroyOnLoad(gameObject);

      LoadSettings();
      ApplyAllSettings();

      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
      isApplicationQuitting = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      // Tự động áp dụng lại độ sáng và camera cho scene mới nạp
      ApplyBrightness();
      ApplyControls();
    }

    /// <summary>
    /// Nạp cấu hình đã lưu từ PlayerPrefs. Nếu chưa có, giữ nguyên giá trị mặc định.
    /// </summary>
    public void LoadSettings()
    {
      if (PlayerPrefs.HasKey(SETTINGS_PREF_KEY))
      {
        try
        {
          string json = PlayerPrefs.GetString(SETTINGS_PREF_KEY);
          currentData = JsonUtility.FromJson<SettingsData>(json);
          if (currentData == null) currentData = new SettingsData();
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[SettingsManager] Lỗi nạp Settings JSON: {ex.Message}. Khôi phục mặc định.");
          currentData = new SettingsData();
        }
      }
      else
      {
        currentData = new SettingsData();
      }
    }

    /// <summary>
    /// Lưu cấu hình hiện tại vào PlayerPrefs.
    /// </summary>
    public void SaveSettings()
    {
      try
      {
        string json = JsonUtility.ToJson(currentData, true);
        PlayerPrefs.SetString(SETTINGS_PREF_KEY, json);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
      }
      catch (Exception ex)
      {
        Debug.LogError($"[SettingsManager] Không thể lưu Settings: {ex.Message}");
      }
    }

    /// <summary>
    /// Khôi phục toàn bộ cài đặt về mặc định của nhà phát triển.
    /// </summary>
    public void ResetToDefaults()
    {
      currentData = new SettingsData();
      SaveSettings();
      ApplyAllSettings();
    }

    /// <summary>
    /// Áp dụng toàn bộ thiết lập vào engine.
    /// </summary>
    public void ApplyAllSettings()
    {
      ApplyGraphics();
      ApplyAudio();
      ApplyDisplay();
      ApplyControls();
      OnSettingsChanged?.Invoke();
    }

    public void ApplyGraphics()
    {
      // 1. Quality Level
      QualitySettings.SetQualityLevel(Mathf.Clamp(currentData.qualityLevel, 0, 3), true);

      // 2. Shadows
      switch (currentData.shadowQuality)
      {
        case 0:
          QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
          break;
        case 1:
          QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
          break;
        case 2:
        default:
          QualitySettings.shadows = UnityEngine.ShadowQuality.All;
          break;
      }

      // 3. Anti-Aliasing
      switch (currentData.antiAliasing)
      {
        case 0: QualitySettings.antiAliasing = 0; break;
        case 1: QualitySettings.antiAliasing = 2; break; // FXAA / 2x
        case 2: QualitySettings.antiAliasing = 4; break; // SMAA / 4x
        default: QualitySettings.antiAliasing = 2; break;
      }

      // 4. FPS Limit
      int targetFps = 60;
      switch (currentData.fpsLimitIndex)
      {
        case 0: targetFps = 30; break;
        case 1: targetFps = 60; break;
        case 2: targetFps = 120; break;
        case 3: targetFps = 144; break;
        case 4: targetFps = -1; break; // Unlimited
        default: targetFps = 60; break;
      }
      Application.targetFrameRate = targetFps;

      // 5. V-Sync (Lưu ý: vSync > 0 sẽ ghi đè targetFrameRate)
      QualitySettings.vSyncCount = currentData.vSync ? 1 : 0;
    }

    public void ApplyAudio()
    {
      // Master Volume điều khiển AudioListener toàn cục
      AudioListener.volume = Mathf.Clamp01(currentData.masterVolume);

      // Cập nhật ma trận âm lượng calibrated trong AudioManager nếu có
      if (AudioManager.HasInstance)
      {
        AudioManager.Instance.ApplySettingsVolumes(
          currentData.masterVolume,
          currentData.musicVolume,
          currentData.voiceVolume,
          currentData.sfxVolume,
          currentData.uiVolume
        );
      }

      OnMasterVolumeChanged?.Invoke(currentData.masterVolume);
      OnMusicVolumeChanged?.Invoke(currentData.musicVolume);
      OnSfxVolumeChanged?.Invoke(currentData.sfxVolume);
      OnVoiceVolumeChanged?.Invoke(currentData.voiceVolume);
      OnUiVolumeChanged?.Invoke(currentData.uiVolume);
    }

    /// <summary>
    /// Khôi phục dữ liệu cấu hình từ một bản sao snapshot (khi người chơi hủy / thoát mà không Apply).
    /// </summary>
    public void RestoreData(SettingsData snapshot)
    {
      if (snapshot != null)
      {
        currentData = snapshot.Clone();
        ApplyAllSettings();
      }
    }

    public void ApplyDisplay()
    {
      // 1. Resolution & Display Mode
      ParseAndApplyResolution(currentData.resolutionString, currentData.displayMode);

      // 2. Brightness
      ApplyBrightness();

      // 3. Toggles
      OnScreenShakeToggled?.Invoke(currentData.screenShake);
      OnDamageNumbersToggled?.Invoke(currentData.damageNumbers);

      // 4. Language
      OnLanguageChanged?.Invoke(currentData.language);
    }

    public void ApplyBrightness()
    {
      float b = Mathf.Clamp(currentData.brightness, 0f, 2f);
      OnBrightnessChanged?.Invoke(b);

      string activeScene = SceneManager.GetActiveScene().name;
      if (!string.IsNullOrEmpty(activeScene) && activeScene.Contains("Dungeon"))
      {
        // DungeonScene: Điều khiển trực tiếp màu Ambient Color trong Environment Lighting
        // Baseline mặc định b = 1.0f tương ứng với RGB(0.23, 0.23, 0.23)
        // b = 0.0f -> RGB(0.05, 0.05, 0.05) (tối huyền bí)
        // b = 2.0f -> RGB(0.46, 0.46, 0.46) (sáng rõ các ngóc ngách và lối đi)
        float gray = b <= 1.0f ? Mathf.Lerp(0.05f, 0.23f, b) : Mathf.Lerp(0.23f, 0.46f, b - 1.0f);
        Color dungeonAmbient = new Color(gray, gray, gray, 1.0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = dungeonAmbient;
        RenderSettings.ambientSkyColor = dungeonAmbient;
      }
      else
      {
        // LandScene / MainMenuScene: Giữ nguyên màu sắc môi trường tự nhiên ban ngày.
        // Hỗ trợ nếu Scene có ColorAdjustments (postExposure)
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (var vol in volumes)
        {
          if (vol != null && vol.profile != null)
          {
            if (vol.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
              float exposureOffset = (b - 1.0f) * 1.5f;
              colorAdjustments.postExposure.Override(exposureOffset);
            }
          }
        }
      }
    }

    public void ApplyControls()
    {
      // ThirdPersonCamera sẽ tự đọc MouseSensitivity và MouseInvertY từ SettingsManager
    }

    private void ParseAndApplyResolution(string resString, int displayModeIndex)
    {
      FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
      switch (displayModeIndex)
      {
        case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
        case 1: mode = FullScreenMode.Windowed; break;
        case 2: mode = FullScreenMode.FullScreenWindow; break;
      }

      int width = Screen.width;
      int height = Screen.height;

      if (!string.IsNullOrEmpty(resString))
      {
        // Làm sạch chuỗi, ví dụ: "'1280 x 720 '" -> "1280 x 720"
        string clean = resString.Replace("'", "").Trim();
        string[] parts = clean.Split('x');
        if (parts.Length == 2)
        {
          if (int.TryParse(parts[0].Trim(), out int w) && int.TryParse(parts[1].Trim(), out int h))
          {
            width = w;
            height = h;
          }
        }
      }

      if (width > 0 && height > 0)
      {
        Screen.SetResolution(width, height, mode);
      }
    }
  }
}
