using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Component điều khiển công tắc chuyển đổi ON / OFF với con trỏ trượt SlidingHandle.
  /// Tương thích với các Toggle trong SettingUI (Vsync, ScreenShake, DmgNumbers, MouseInvertY).
  /// </summary>
  [DisallowMultipleComponent]
  public class ToggleSwitchUI : MonoBehaviour
  {
    [Header("=== CONTROLS ===")]
    [SerializeField] private RectTransform slidingHandle;
    [SerializeField] private Button btnOn;
    [SerializeField] private Button btnOff;
    [SerializeField] private TextMeshProUGUI labelOn;
    [SerializeField] private TextMeshProUGUI labelOff;

    [Header("=== ANIMATION SETTINGS ===")]
    [Tooltip("Thời gian trượt của SlidingHandle (giây)")]
    [Range(0.05f, 0.3f)]
    [SerializeField] private float slideDuration = 0.12f;

    [Header("=== LABEL COLORS ===")]
    [SerializeField] private Color activeColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);   // Vàng kim sáng
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.55f, 0.55f, 0.8f); // Xám mờ

    [Header("=== STATE ===")]
    [SerializeField] private bool isOn = false;
    private Coroutine slideRoutine = null;

    public bool IsOn
    {
      get => isOn;
      set => SetIsOn(value, false);
    }

    public event Action<bool> OnValueChanged;

    private void Awake()
    {
      AutoFindReferences();

      if (btnOn != null)
      {
        btnOn.onClick.RemoveListener(OnBtnOnClick);
        btnOn.onClick.AddListener(OnBtnOnClick);
      }

      if (btnOff != null)
      {
        btnOff.onClick.RemoveListener(OnBtnOffClick);
        btnOff.onClick.AddListener(OnBtnOffClick);
      }

      // Cho phép bấm trực tiếp vào khung nền để đảo trạng thái
      Button selfBtn = GetComponent<Button>();
      if (selfBtn != null)
      {
        selfBtn.onClick.RemoveListener(OnToggleClick);
        selfBtn.onClick.AddListener(OnToggleClick);
      }
    }

    private void Start()
    {
      // Khởi tạo vị trí ban đầu không dùng animation
      SetIsOn(isOn, false);
    }

    private void OnEnable()
    {
      // Khởi tạo vị trí ban đầu không dùng animation khi bật lại panel
      SetIsOn(isOn, false);
    }

    public void AutoFindReferences()
    {
      if (slidingHandle == null)
      {
        Transform handleT = transform.Find("SlidingHandle");
        if (handleT != null) slidingHandle = handleT as RectTransform;
      }

      if (btnOn == null)
      {
        btnOn = transform.Find("BtnOn")?.GetComponent<Button>();
      }

      if (btnOff == null)
      {
        btnOff = transform.Find("BtnOff")?.GetComponent<Button>();
      }

      if (labelOn == null && btnOn != null)
      {
        labelOn = btnOn.GetComponentInChildren<TextMeshProUGUI>();
      }

      if (labelOff == null && btnOff != null)
      {
        labelOff = btnOff.GetComponentInChildren<TextMeshProUGUI>();
      }
    }

    private void OnBtnOnClick()
    {
      if (!isOn)
      {
        AudioManager.Instance?.PlayUIInteract2();
        SetIsOn(true, true);
        OnValueChanged?.Invoke(true);
      }
    }

    private void OnBtnOffClick()
    {
      if (isOn)
      {
        AudioManager.Instance?.PlayUIInteract2();
        SetIsOn(false, true);
        OnValueChanged?.Invoke(false);
      }
    }

    private void OnToggleClick()
    {
      AudioManager.Instance?.PlayUIInteract2();
      SetIsOn(!isOn, true);
      OnValueChanged?.Invoke(isOn);
    }

    /// <summary>
    /// Thiết lập trạng thái ON / OFF của toggle.
    /// </summary>
    public void SetIsOn(bool value, bool animate = true)
    {
      isOn = value;

      if (labelOn != null) labelOn.color = isOn ? activeColor : inactiveColor;
      if (labelOff != null) labelOff.color = !isOn ? activeColor : inactiveColor;

      float targetX = GetTargetPosX(isOn);

      if (slidingHandle != null)
      {
        if (slideRoutine != null)
        {
          StopCoroutine(slideRoutine);
          slideRoutine = null;
        }

        if (animate && gameObject.activeInHierarchy)
        {
          slideRoutine = StartCoroutine(SlideRoutine(targetX));
        }
        else
        {
          Vector2 pos = slidingHandle.anchoredPosition;
          pos.x = targetX;
          slidingHandle.anchoredPosition = pos;
        }
      }
    }

    private float GetTargetPosX(bool stateOn)
    {
      Transform targetBtn = stateOn ? (btnOn != null ? btnOn.transform : null) : (btnOff != null ? btnOff.transform : null);
      if (targetBtn != null && slidingHandle != null && slidingHandle.parent != null)
      {
        RectTransform parentRT = slidingHandle.parent as RectTransform;
        if (parentRT != null)
        {
          float anchorOffset = parentRT.rect.width * (slidingHandle.anchorMin.x - parentRT.pivot.x);
          return targetBtn.localPosition.x - anchorOffset;
        }
      }

      return stateOn ? 155.27f : 53.5f;
    }

    private IEnumerator SlideRoutine(float targetX)
    {
      if (slidingHandle == null) yield break;

      float startX = slidingHandle.anchoredPosition.x;
      float elapsed = 0f;
      float duration = Mathf.Max(0.04f, slideDuration);

      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        // Smoothstep interpolation
        float smoothT = t * t * (3f - 2f * t);

        Vector2 pos = slidingHandle.anchoredPosition;
        pos.x = Mathf.Lerp(startX, targetX, smoothT);
        slidingHandle.anchoredPosition = pos;

        yield return null;
      }

      Vector2 finalPos = slidingHandle.anchoredPosition;
      finalPos.x = targetX;
      slidingHandle.anchoredPosition = finalPos;
      slideRoutine = null;
    }

    private void OnDisable()
    {
      if (slideRoutine != null)
      {
        StopCoroutine(slideRoutine);
        slideRoutine = null;
      }
    }

    private void OnValidate()
    {
      AutoFindReferences();
    }
  }
}
