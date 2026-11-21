using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Điều khiển toàn bộ giao diện Cài đặt (SettingUI).
  /// Quản lý chuyển 4 Tab nội dung, cuộn ScrollRect, đồng bộ giá trị với SettingsManager,
  /// hiển thị thanh âm lượng theo thang điểm 0.0 - 10.0 theo yêu cầu thiết kế.
  /// </summary>
  [DisallowMultipleComponent]
  public class SettingsUIController : MonoBehaviour
  {
    public enum SettingsTab
    {
      Graphics = 0,
      Audio = 1,
      Display = 2,
      Controls = 3
    }

    [Header("=== TAB BUTTONS (CÁC NÚT DANH MỤC TRÁI) ===")]
    [SerializeField] private Button btnGraphic;
    [SerializeField] private Button btnAudio;
    [SerializeField] private Button btnDisplay;
    [SerializeField] private Button btnControls;

    [Header("=== TAB CONTENT SECTIONS (NỘI DUNG CUỘN) ===")]
    [SerializeField] private ScrollRect settingsScrollRect;
    [SerializeField] private RectTransform graphicsSection;
    [SerializeField] private RectTransform audioSection;
    [SerializeField] private RectTransform displaySection;
    [SerializeField] private RectTransform controlsSection;

    [Header("=== GRAPHICS CONTROLS ===")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown shadowDropdown;
    [SerializeField] private TMP_Dropdown aaDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [SerializeField] private ToggleSwitchUI vsyncToggle;

    [Header("=== AUDIO CONTROLS (HIỂN THỊ THANG ĐIỂM 0.0 - 10.0) ===")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private Slider voiceSlider;
    [SerializeField] private TextMeshProUGUI voiceValueText;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiValueText;

    [Header("=== DISPLAY CONTROLS ===")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessValueText;
    [SerializeField] private ToggleSwitchUI screenShakeToggle;
    [SerializeField] private ToggleSwitchUI dmgNumbersToggle;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("=== CONTROLS TAB ===")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;
    [SerializeField] private ToggleSwitchUI invertYToggle;

    [Header("=== FOOTER BUTTONS ===")]
    [SerializeField] private Button btnResetSettings;
    [SerializeField] private Button btnSaveSettings;
    [SerializeField] private TextMeshProUGUI saveButtonText;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnBack;

    [Header("=== TAB HIGHLIGHT COLORS ===")]
    [SerializeField] private Color activeTabColor = new Color(1.0f, 0.88f, 0.45f, 1.0f); // Vàng kim sáng
    [SerializeField] private Color inactiveTabColor = new Color(0.62f, 0.62f, 0.62f, 0.8f); // Xám mờ

    private SettingsTab currentTab = SettingsTab.Graphics;
    private Coroutine feedbackRoutine = null;
    private bool isInitializing = false;
    private SettingsData originalSnapshot = null;
    private bool isApplying = false;

    public bool IsOpen => gameObject.activeInHierarchy;
    public event Action OnClosed;

    private void Awake()
    {
      AutoFindReferences();
      RegisterEvents();
      RefreshAllUIFromManager();
    }

    private void Update()
    {
      if (UnityEngine.InputSystem.Keyboard.current != null &&
          UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame &&
          IsOpen)
      {
        Close();
      }
    }

    private void Start()
    {
      if (originalSnapshot == null && SettingsManager.Instance != null)
      {
        originalSnapshot = SettingsManager.Instance.CurrentData.Clone();
      }
      RefreshAllUIFromManager();
      SelectTab(SettingsTab.Graphics, false);
    }

    private void OnEnable()
    {
      if (!isApplying && SettingsManager.Instance != null)
      {
        originalSnapshot = SettingsManager.Instance.CurrentData.Clone();
      }
      RefreshAllUIFromManager();
      SelectTab(currentTab, false);
    }

    public void Open()
    {
      gameObject.SetActive(true);
      isApplying = false;
      if (SettingsManager.Instance != null)
      {
        originalSnapshot = SettingsManager.Instance.CurrentData.Clone();
      }
      RefreshAllUIFromManager();
      SelectTab(SettingsTab.Graphics, false);
    }

    public void Close()
    {
      // Nếu đóng giao diện mà KHÔNG bấm Apply -> hoàn trả toàn bộ về cấu hình đã lưu trước đó
      if (!isApplying && originalSnapshot != null && SettingsManager.Instance != null)
      {
        SettingsManager.Instance.RestoreData(originalSnapshot);
        SettingsManager.Instance.ApplyAllSettings();
      }

      isApplying = false;
      gameObject.SetActive(false);
      OnClosed?.Invoke();
    }

    /// <summary>
    /// Tự động dò tìm toàn bộ các component và GameObject con trong hierarchy của SettingUI.
    /// </summary>
    public void AutoFindReferences()
    {
      // 1. Tab buttons
      Transform typeContainer = transform.Find("SettingsTypeContainer");
      if (typeContainer != null)
      {
        if (btnGraphic == null) btnGraphic = typeContainer.Find("BtnGraphic")?.GetComponent<Button>();
        if (btnAudio == null) btnAudio = typeContainer.Find("BtnAudio")?.GetComponent<Button>();
        if (btnDisplay == null) btnDisplay = typeContainer.Find("BtnDisplay")?.GetComponent<Button>();
        if (btnControls == null) btnControls = typeContainer.Find("BtnControls")?.GetComponent<Button>();
      }

      // 2. ScrollRect & Sections
      if (settingsScrollRect == null)
      {
        settingsScrollRect = transform.Find("SettingsScrollView")?.GetComponent<ScrollRect>();
      }

      Transform viewport = transform.Find("SettingsScrollView/Viewport");
      if (viewport != null)
      {
        if (graphicsSection == null) graphicsSection = viewport.Find("GraphicsUI") as RectTransform;
        if (audioSection == null) audioSection = viewport.Find("Audio") as RectTransform;
        if (displaySection == null) displaySection = viewport.Find("Display") as RectTransform;
        if (controlsSection == null) controlsSection = viewport.Find("Controls") as RectTransform;
      }

      // 3. Graphics Controls
      if (graphicsSection != null)
      {
        if (qualityDropdown == null) qualityDropdown = graphicsSection.Find("GameQuality/Dropdown")?.GetComponent<TMP_Dropdown>();
        if (shadowDropdown == null) shadowDropdown = graphicsSection.Find("ShadowQuality/Dropdown")?.GetComponent<TMP_Dropdown>();
        if (aaDropdown == null) aaDropdown = graphicsSection.Find("AntiAliasing/Dropdown")?.GetComponent<TMP_Dropdown>();
        if (fpsDropdown == null) fpsDropdown = graphicsSection.Find("FPS/Dropdown")?.GetComponent<TMP_Dropdown>();
        if (vsyncToggle == null)
        {
          Transform vT = graphicsSection.Find("Vsync/ToggleVsync");
          if (vT != null) vsyncToggle = vT.GetComponent<ToggleSwitchUI>() ?? vT.gameObject.AddComponent<ToggleSwitchUI>();
        }
      }

      // 4. Audio Controls
      if (audioSection != null)
      {
        FindSliderAndText(audioSection, "MasterVolume", ref masterSlider, ref masterValueText);
        FindSliderAndText(audioSection, "Music", ref musicSlider, ref musicValueText);
        FindSliderAndText(audioSection, "CharacterVoice", ref voiceSlider, ref voiceValueText);
        FindSliderAndText(audioSection, "SFX", ref sfxSlider, ref sfxValueText);
        FindSliderAndText(audioSection, "UI Sound", ref uiSlider, ref uiValueText);
      }

      // 5. Display Controls
      if (displaySection != null)
      {
        if (resolutionDropdown == null) resolutionDropdown = displaySection.Find("Resolution/Dropdown")?.GetComponent<TMP_Dropdown>();
        if (displayModeDropdown == null) displayModeDropdown = displaySection.Find("DisplayMode/Dropdown")?.GetComponent<TMP_Dropdown>();
        FindSliderAndText(displaySection, "Brightness", ref brightnessSlider, ref brightnessValueText);

        if (screenShakeToggle == null)
        {
          Transform sT = displaySection.Find("ScreenShake/ToggleVsync");
          if (sT != null) screenShakeToggle = sT.GetComponent<ToggleSwitchUI>() ?? sT.gameObject.AddComponent<ToggleSwitchUI>();
        }

        if (dmgNumbersToggle == null)
        {
          Transform dT = displaySection.Find("DmgNumbers/ToggleVsync");
          if (dT != null) dmgNumbersToggle = dT.GetComponent<ToggleSwitchUI>() ?? dT.gameObject.AddComponent<ToggleSwitchUI>();
        }

        if (languageDropdown == null) languageDropdown = displaySection.Find("Language/Dropdown")?.GetComponent<TMP_Dropdown>();
      }

      // 6. Controls Tab
      if (controlsSection != null)
      {
        FindSliderAndText(controlsSection, "MouseSensitivity", ref sensitivitySlider, ref sensitivityValueText);

        if (invertYToggle == null)
        {
          Transform iT = controlsSection.Find("MouseInvertY/ToggleVsync");
          if (iT != null) invertYToggle = iT.GetComponent<ToggleSwitchUI>() ?? iT.gameObject.AddComponent<ToggleSwitchUI>();
        }
      }

      // 7. Footer
      Transform footer = transform.Find("Footer");
      if (footer != null)
      {
        if (btnResetSettings == null) btnResetSettings = footer.Find("BtnResetSettings")?.GetComponent<Button>();
        if (btnSaveSettings == null)
        {
          btnSaveSettings = footer.Find("BtnSaveSettings")?.GetComponent<Button>();
          if (btnSaveSettings != null) saveButtonText = btnSaveSettings.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (btnBack == null) btnBack = footer.Find("BtnBack")?.GetComponent<Button>();
      }
      if (btnBack == null) btnBack = transform.Find("BtnBack")?.GetComponent<Button>();

      // 8. Close Button
      if (btnClose == null)
      {
        btnClose = transform.Find("BtnClose")?.GetComponent<Button>() ??
                   transform.Find("Header/BtnClose")?.GetComponent<Button>() ??
                   transform.Find("Background/BtnClose")?.GetComponent<Button>();
      }
    }

    private void FindSliderAndText(Transform parent, string childName, ref Slider slider, ref TextMeshProUGUI text)
    {
      Transform c = parent.Find(childName);
      if (c != null)
      {
        if (slider == null) slider = c.Find("Slider")?.GetComponent<Slider>();
        if (text == null) text = c.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
      }
    }

    private void RegisterEvents()
    {
      // Tab switching
      if (btnGraphic != null) btnGraphic.onClick.AddListener(() => SelectTab(SettingsTab.Graphics, true));
      if (btnAudio != null) btnAudio.onClick.AddListener(() => SelectTab(SettingsTab.Audio, true));
      if (btnDisplay != null) btnDisplay.onClick.AddListener(() => SelectTab(SettingsTab.Display, true));
      if (btnControls != null) btnControls.onClick.AddListener(() => SelectTab(SettingsTab.Controls, true));

      // Sliders Audio (Live preview + Format 0.0 - 10.0)
      if (masterSlider != null) masterSlider.onValueChanged.AddListener(val => {
        UpdateAudioValueText(masterValueText, val);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.masterVolume = val;
          SettingsManager.Instance.ApplyAudio();
        }
      });

      if (musicSlider != null) musicSlider.onValueChanged.AddListener(val => {
        UpdateAudioValueText(musicValueText, val);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.musicVolume = val;
          SettingsManager.Instance.ApplyAudio();
        }
      });

      if (voiceSlider != null) voiceSlider.onValueChanged.AddListener(val => {
        UpdateAudioValueText(voiceValueText, val);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.voiceVolume = val;
          SettingsManager.Instance.ApplyAudio();
        }
      });

      if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(val => {
        UpdateAudioValueText(sfxValueText, val);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.sfxVolume = val;
          SettingsManager.Instance.ApplyAudio();
        }
      });

      if (uiSlider != null) uiSlider.onValueChanged.AddListener(val => {
        UpdateAudioValueText(uiValueText, val);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.uiVolume = val;
          SettingsManager.Instance.ApplyAudio();
        }
      });

      // Brightness Slider (Live preview)
      if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(val => {
        if (brightnessValueText != null) brightnessValueText.text = val.ToString("F1", CultureInfo.InvariantCulture);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.brightness = val;
          SettingsManager.Instance.ApplyBrightness();
        }
      });

      // Sensitivity Slider (Live preview)
      if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(val => {
        if (sensitivityValueText != null) sensitivityValueText.text = val.ToString("F1", CultureInfo.InvariantCulture);
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.mouseSensitivity = val;
        }
      });

      // Toggles (Live update)
      if (vsyncToggle != null) vsyncToggle.OnValueChanged += isOn => {
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.vSync = isOn;
          QualitySettings.vSyncCount = isOn ? 1 : 0;
        }
      };

      if (screenShakeToggle != null) screenShakeToggle.OnValueChanged += isOn => {
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.screenShake = isOn;
        }
      };

      if (dmgNumbersToggle != null) dmgNumbersToggle.OnValueChanged += isOn => {
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.damageNumbers = isOn;
        }
      };

      if (invertYToggle != null) invertYToggle.OnValueChanged += isOn => {
        if (!isInitializing && SettingsManager.HasInstance) {
          SettingsManager.Instance.CurrentData.mouseInvertY = isOn;
        }
      };

      // Footer
      if (btnSaveSettings != null) btnSaveSettings.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        OnApplySettingsClick();
      });
      if (btnResetSettings != null) btnResetSettings.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        OnResetDefaultClick();
      });
      if (btnClose != null) btnClose.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        Close();
      });
      if (btnBack != null) btnBack.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        Close();
      });

      // Lắng nghe sự kiện nhả tay (PointerUp) khi kéo xong các slider
      AttachSliderPointerUpAudio(masterSlider);
      AttachSliderPointerUpAudio(musicSlider);
      AttachSliderPointerUpAudio(voiceSlider);
      AttachSliderPointerUpAudio(sfxSlider);
      AttachSliderPointerUpAudio(uiSlider);
      AttachSliderPointerUpAudio(brightnessSlider);
      AttachSliderPointerUpAudio(sensitivitySlider);
    }

    private void AttachSliderPointerUpAudio(Slider slider)
    {
      if (slider == null) return;
      AddPointerUpListener(slider.gameObject);
      if (slider.handleRect != null)
      {
        AddPointerUpListener(slider.handleRect.gameObject);
      }
    }

    private void AddPointerUpListener(GameObject targetObj)
    {
      if (targetObj == null) return;
      var trigger = targetObj.GetComponent<EventTrigger>() ?? targetObj.AddComponent<EventTrigger>();
      EventTrigger.Entry entry = trigger.triggers.Find(e => e.eventID == EventTriggerType.PointerUp);
      if (entry == null)
      {
        entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        trigger.triggers.Add(entry);
      }
      entry.callback.RemoveAllListeners();
      entry.callback.AddListener((data) => {
        AudioManager.Instance?.PlayUIInteract2();
      });
    }

    /// <summary>
    /// Hiển thị giá trị âm thanh theo thang điểm 0.0 - 10.0 thay vì phần trăm theo yêu cầu.
    /// </summary>
    private void UpdateAudioValueText(TextMeshProUGUI text, float sliderValue)
    {
      if (text == null) return;
      float score = Mathf.Clamp(sliderValue * 10f, 0f, 10f);
      text.text = score.ToString("0.0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Chuyển đổi qua lại giữa 4 Tab cài đặt.
    /// </summary>
    public void SelectTab(SettingsTab tab, bool punchAnim)
    {
      if (punchAnim && tab != currentTab)
      {
        AudioManager.Instance?.PlayUIInteract3();
      }
      currentTab = tab;

      if (graphicsSection != null) graphicsSection.gameObject.SetActive(tab == SettingsTab.Graphics);
      if (audioSection != null) audioSection.gameObject.SetActive(tab == SettingsTab.Audio);
      if (displaySection != null) displaySection.gameObject.SetActive(tab == SettingsTab.Display);
      if (controlsSection != null) controlsSection.gameObject.SetActive(tab == SettingsTab.Controls);

      // Gán Content cho ScrollRect và đưa về đỉnh
      RectTransform activeRect = null;
      switch (tab)
      {
        case SettingsTab.Graphics: activeRect = graphicsSection; break;
        case SettingsTab.Audio: activeRect = audioSection; break;
        case SettingsTab.Display: activeRect = displaySection; break;
        case SettingsTab.Controls: activeRect = controlsSection; break;
      }

      if (settingsScrollRect != null)
      {
        if (activeRect != null) settingsScrollRect.content = activeRect;
        Canvas.ForceUpdateCanvases();
        settingsScrollRect.verticalNormalizedPosition = 1f; // Scroll lên đầu trang
      }

      // Cập nhật trạng thái Highlight của các nút Tab
      UpdateTabButtonVisual(btnGraphic, tab == SettingsTab.Graphics, punchAnim);
      UpdateTabButtonVisual(btnAudio, tab == SettingsTab.Audio, punchAnim);
      UpdateTabButtonVisual(btnDisplay, tab == SettingsTab.Display, punchAnim);
      UpdateTabButtonVisual(btnControls, tab == SettingsTab.Controls, punchAnim);
    }

    private void UpdateTabButtonVisual(Button btn, bool isSelected, bool punchAnim)
    {
      if (btn == null) return;

      Transform corner = btn.transform.Find("HoverCorner");
      if (corner != null) corner.gameObject.SetActive(isSelected);

      TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
      if (label != null)
      {
        label.color = isSelected ? activeTabColor : inactiveTabColor;
      }

      if (punchAnim && isSelected)
      {
        StopCoroutine("PunchScaleRoutine");
        StartCoroutine(PunchScaleRoutine(btn.transform));
      }
    }

    private IEnumerator PunchScaleRoutine(Transform target)
    {
      Vector3 original = Vector3.one;
      Vector3 punch = original * 1.08f;
      float elapsed = 0f;
      float duration = 0.14f;

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = elapsed / duration;
        target.localScale = Vector3.Lerp(original, punch, Mathf.Sin(t * Mathf.PI));
        yield return null;
      }
      target.localScale = original;
    }

    /// <summary>
    /// Đồng bộ toàn bộ dữ liệu từ SettingsManager vào các controls trên giao diện.
    /// </summary>
    public void RefreshAllUIFromManager()
    {
      SettingsData data = SettingsManager.Instance != null ? SettingsManager.Instance.CurrentData : new SettingsData();
      PopulateUIFromData(data);
    }

    private void PopulateUIFromData(SettingsData data)
    {
      if (data == null) return;
      isInitializing = true;

      // 1. Graphics
      if (qualityDropdown != null) qualityDropdown.value = Mathf.Clamp(data.qualityLevel, 0, qualityDropdown.options.Count - 1);
      if (shadowDropdown != null) shadowDropdown.value = Mathf.Clamp(data.shadowQuality, 0, shadowDropdown.options.Count - 1);
      if (aaDropdown != null) aaDropdown.value = Mathf.Clamp(data.antiAliasing, 0, aaDropdown.options.Count - 1);
      if (fpsDropdown != null) fpsDropdown.value = Mathf.Clamp(data.fpsLimitIndex, 0, fpsDropdown.options.Count - 1);
      if (vsyncToggle != null) vsyncToggle.SetIsOn(data.vSync, false);

      // 2. Audio (0.0 - 10.0 scale)
      if (masterSlider != null) { masterSlider.value = data.masterVolume; UpdateAudioValueText(masterValueText, data.masterVolume); }
      if (musicSlider != null) { musicSlider.value = data.musicVolume; UpdateAudioValueText(musicValueText, data.musicVolume); }
      if (voiceSlider != null) { voiceSlider.value = data.voiceVolume; UpdateAudioValueText(voiceValueText, data.voiceVolume); }
      if (sfxSlider != null) { sfxSlider.value = data.sfxVolume; UpdateAudioValueText(sfxValueText, data.sfxVolume); }
      if (uiSlider != null) { uiSlider.value = data.uiVolume; UpdateAudioValueText(uiValueText, data.uiVolume); }

      // 3. Display
      if (resolutionDropdown != null)
      {
        int matchIdx = FindDropdownIndexByText(resolutionDropdown, data.resolutionString);
        resolutionDropdown.value = matchIdx >= 0 ? matchIdx : 2; // Default 1920x1080
      }
      if (displayModeDropdown != null) displayModeDropdown.value = Mathf.Clamp(data.displayMode, 0, displayModeDropdown.options.Count - 1);
      if (brightnessSlider != null)
      {
        brightnessSlider.value = data.brightness;
        if (brightnessValueText != null) brightnessValueText.text = data.brightness.ToString("F1", CultureInfo.InvariantCulture);
      }
      if (screenShakeToggle != null) screenShakeToggle.SetIsOn(data.screenShake, false);
      if (dmgNumbersToggle != null) dmgNumbersToggle.SetIsOn(data.damageNumbers, false);
      if (languageDropdown != null) languageDropdown.value = Mathf.Clamp(data.language, 0, languageDropdown.options.Count - 1);

      // 4. Controls
      if (sensitivitySlider != null)
      {
        sensitivitySlider.value = data.mouseSensitivity;
        if (sensitivityValueText != null) sensitivityValueText.text = data.mouseSensitivity.ToString("F1", CultureInfo.InvariantCulture);
      }
      if (invertYToggle != null) invertYToggle.SetIsOn(data.mouseInvertY, false);

      isInitializing = false;
    }

    private int FindDropdownIndexByText(TMP_Dropdown dropdown, string search)
    {
      if (dropdown == null || string.IsNullOrEmpty(search)) return -1;
      string cleanSearch = search.Replace("'", "").Replace(" ", "").ToLower();
      for (int i = 0; i < dropdown.options.Count; i++)
      {
        string opt = dropdown.options[i].text.Replace("'", "").Replace(" ", "").ToLower();
        if (opt.Contains(cleanSearch) || cleanSearch.Contains(opt)) return i;
      }
      return -1;
    }

    private void OnApplySettingsClick()
    {
      if (SettingsManager.Instance == null) return;

      isApplying = true;
      SettingsData data = SettingsManager.Instance.CurrentData;

      // Graphics
      if (qualityDropdown != null) data.qualityLevel = qualityDropdown.value;
      if (shadowDropdown != null) data.shadowQuality = shadowDropdown.value;
      if (aaDropdown != null) data.antiAliasing = aaDropdown.value;
      if (fpsDropdown != null) data.fpsLimitIndex = fpsDropdown.value;
      if (vsyncToggle != null) data.vSync = vsyncToggle.IsOn;

      // Audio
      if (masterSlider != null) data.masterVolume = masterSlider.value;
      if (musicSlider != null) data.musicVolume = musicSlider.value;
      if (voiceSlider != null) data.voiceVolume = voiceSlider.value;
      if (sfxSlider != null) data.sfxVolume = sfxSlider.value;
      if (uiSlider != null) data.uiVolume = uiSlider.value;

      // Display
      if (resolutionDropdown != null && resolutionDropdown.value < resolutionDropdown.options.Count)
      {
        data.resolutionString = resolutionDropdown.options[resolutionDropdown.value].text;
      }
      if (displayModeDropdown != null) data.displayMode = displayModeDropdown.value;
      if (brightnessSlider != null) data.brightness = brightnessSlider.value;
      if (screenShakeToggle != null) data.screenShake = screenShakeToggle.IsOn;
      if (dmgNumbersToggle != null) data.damageNumbers = dmgNumbersToggle.IsOn;
      if (languageDropdown != null) data.language = languageDropdown.value;

      // Controls
      if (sensitivitySlider != null) data.mouseSensitivity = sensitivitySlider.value;
      if (invertYToggle != null) data.mouseInvertY = invertYToggle.IsOn;

      // Save & Apply
      SettingsManager.Instance.SaveSettings();
      SettingsManager.Instance.ApplyAllSettings();
      originalSnapshot = data.Clone();

      // Đóng SettingUI luôn theo yêu cầu
      Close();
    }

    private IEnumerator ShowApplyFeedbackRoutine()
    {
      if (saveButtonText == null) yield break;
      string original = saveButtonText.text;
      saveButtonText.text = "<color=#FFDE6A>Applied!</color>";

      if (btnSaveSettings != null)
      {
        StartCoroutine(PunchScaleRoutine(btnSaveSettings.transform));
      }

      yield return new WaitForSecondsRealtime(1.2f);
      saveButtonText.text = original;
      feedbackRoutine = null;
    }

    private void OnResetDefaultClick()
    {
      SettingsData defaultData = new SettingsData();
      PopulateUIFromData(defaultData);

      // Live preview các thông số mặc định nhưng chưa lưu vào PlayerPrefs
      if (SettingsManager.Instance != null)
      {
        SettingsData cur = SettingsManager.Instance.CurrentData;
        cur.masterVolume = defaultData.masterVolume;
        cur.musicVolume = defaultData.musicVolume;
        cur.voiceVolume = defaultData.voiceVolume;
        cur.sfxVolume = defaultData.sfxVolume;
        cur.uiVolume = defaultData.uiVolume;
        SettingsManager.Instance.ApplyAudio();

        cur.brightness = defaultData.brightness;
        SettingsManager.Instance.ApplyBrightness();
      }

      if (btnResetSettings != null)
      {
        StartCoroutine(PunchScaleRoutine(btnResetSettings.transform));
      }
    }

    private void OnValidate()
    {
      AutoFindReferences();
    }
  }
}
