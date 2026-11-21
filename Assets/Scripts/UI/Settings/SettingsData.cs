using System;
using UnityEngine;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Cấu trúc dữ liệu lưu trữ toàn bộ các tùy chọn cài đặt của trò chơi.
  /// Hỗ trợ serialize sang JSON để lưu trong PlayerPrefs hoặc file.
  /// </summary>
  [Serializable]
  public class SettingsData
  {
    [Header("=== GRAPHICS SETTINGS ===")]
    public int qualityLevel = 2;       // 0: Low, 1: Medium, 2: High, 3: Ultra (Mặc định High)
    public int shadowQuality = 2;      // 0: Off, 1: Low, 2: High (Mặc định High)
    public int antiAliasing = 1;       // 0: Off, 1: FXAA, 2: SMAA
    public int fpsLimitIndex = 1;      // 0: 30, 1: 60, 2: 120, 3: 144, 4: Unlimited (Mặc định 60)
    public bool vSync = false;

    [Header("=== AUDIO SETTINGS (0.0 - 1.0 INTERNALLY, 0.0 - 10.0 DISPLAY) ===")]
    public float masterVolume = 1.0f;  // 10.0 trên UI (Mặc định 1.0)
    public float musicVolume = 0.8f;   // 8.0 trên UI (Mặc định 0.8)
    public float voiceVolume = 0.8f;   // 8.0 trên UI (Mặc định 0.8)
    public float sfxVolume = 0.8f;     // 8.0 trên UI (Mặc định 0.8)
    public float uiVolume = 0.8f;      // 8.0 trên UI (Mặc định 0.8)

    [Header("=== DISPLAY SETTINGS ===")]
    public string resolutionString = "1920 x 1080";
    public int displayMode = 0;        // 0: Full Screen, 1: Windowed, 2: Borderless
    public float brightness = 1.0f;    // 0.0 - 2.0 (Mặc định 1.0)
    public bool screenShake = true;    // Rung lắc màn hình
    public bool damageNumbers = true;  // Hiển thị số sát thương
    public int language = 0;           // 0: English, 1: Vietnamese

    [Header("=== CONTROLS SETTINGS ===")]
    public float mouseSensitivity = 1.0f; // 0.1 - 3.0 (Mặc định 1.0)
    public bool mouseInvertY = false;

    /// <summary>
    /// Tạo bản sao dữ liệu cài đặt.
    /// </summary>
    public SettingsData Clone()
    {
      return (SettingsData)this.MemberwiseClone();
    }
  }
}
