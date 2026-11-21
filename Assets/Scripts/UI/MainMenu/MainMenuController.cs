using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý toàn bộ giao diện và các luồng chuyển màn hình tại MainMenuScene.
  /// Điều khiển MainMenuUI, ContinueUI, NewgameUI, NewgameModeSelectedUI và kết nối SettingUI.
  /// </summary>
  [DisallowMultipleComponent]
  public class MainMenuController : MonoBehaviour
  {
    [Header("=== SUB PANELS & POPUPS ===")]
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject continueUI;
    [SerializeField] private GameObject newgameUI;
    [SerializeField] private GameObject newgameModeSelectedUI;
    [SerializeField] private SettingsUIController settingUI;

    [Header("=== MAIN MENU BUTTONS ===")]
    [SerializeField] private Button btnContinue;
    [SerializeField] private Button btnNewGame;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnCredits;
    [SerializeField] private Button btnQuit;

    [Header("=== CONTINUE UI CONTROLS ===")]
    [SerializeField] private Button btnContinueConfirm;
    [SerializeField] private Button btnContinueBack;
    [SerializeField] private TextMeshProUGUI dayCountText;
    [SerializeField] private TextMeshProUGUI floorCountText;
    [SerializeField] private TextMeshProUGUI levelCountText;
    [SerializeField] private TextMeshProUGUI goldCountText;

    [Header("=== NEW GAME UI CONTROLS ===")]
    [SerializeField] private Button btnNewGameConfirm;
    [SerializeField] private Button btnNewGameBack;

    [Header("=== MODE SELECT CONTROLS ===")]
    [SerializeField] private Button btnNewGameSingle;
    [SerializeField] private Button btnNewGameCoop;

    [Header("=== TARGET SCENES ===")]
    [SerializeField] private string targetGameplayScene = "LandScene";

    private void Awake()
    {
      AutoFindReferences();
      RegisterEvents();
    }

    private void Start()
    {
      // Đảm bảo mở chuột và nhịp thời gian bình thường khi ở Menu chính
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
      Time.timeScale = 1f;

      ShowOnlyPanel(mainMenuUI);

      if (settingUI != null)
      {
        settingUI.Close();
      }

      RefreshSavePreview();
    }

    private void Update()
    {
      // Xử lý phím ESC để lùi về Menu chính từ các popup
      if (UnityEngine.InputSystem.Keyboard.current != null &&
          UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        HandleEscapeKey();
      }
    }

    public void AutoFindReferences()
    {
      // 1. Panels
      if (mainMenuUI == null) mainMenuUI = transform.Find("MainMenuUI")?.gameObject;
      if (continueUI == null) continueUI = transform.Find("ContinueUI")?.gameObject;
      if (newgameUI == null) newgameUI = transform.Find("NewgameUI")?.gameObject;
      if (newgameModeSelectedUI == null) newgameModeSelectedUI = transform.Find("NewgameModeSelectedUI")?.gameObject;

      Transform settingT = transform.Find("SettingUI");
      if (settingT != null && settingUI == null)
      {
        settingUI = settingT.GetComponent<SettingsUIController>();
      }

      // 2. Main Menu Buttons
      Transform btnContainer = transform.Find("MainMenuUI/ButtonContainer");
      if (btnContainer != null)
      {
        if (btnContinue == null) btnContinue = btnContainer.Find("BtnContinue")?.GetComponent<Button>();
        if (btnNewGame == null) btnNewGame = btnContainer.Find("BtnNewGame")?.GetComponent<Button>();
        if (btnSettings == null) btnSettings = btnContainer.Find("BtnSettings")?.GetComponent<Button>();
        if (btnCredits == null) btnCredits = btnContainer.Find("BtnCredits")?.GetComponent<Button>();
        if (btnQuit == null) btnQuit = btnContainer.Find("BtnQuit")?.GetComponent<Button>();
      }

      // 3. Continue UI
      if (continueUI != null)
      {
        if (btnContinueConfirm == null) btnContinueConfirm = continueUI.transform.Find("BtnContinueConfirm")?.GetComponent<Button>();
        if (btnContinueBack == null) btnContinueBack = continueUI.transform.Find("BtnContinueBack")?.GetComponent<Button>();

        Transform saveInfo = continueUI.transform.Find("SaveInfo");
        if (saveInfo != null)
        {
          if (dayCountText == null) dayCountText = saveInfo.Find("DayCount")?.GetComponent<TextMeshProUGUI>();
          if (floorCountText == null) floorCountText = saveInfo.Find("FloorCount")?.GetComponent<TextMeshProUGUI>();
          if (levelCountText == null) levelCountText = saveInfo.Find("LevelCount")?.GetComponent<TextMeshProUGUI>();
          if (goldCountText == null) goldCountText = saveInfo.Find("GoidCount/Value")?.GetComponent<TextMeshProUGUI>();
        }
      }

      // 4. Newgame UI
      if (newgameUI != null)
      {
        if (btnNewGameConfirm == null) btnNewGameConfirm = newgameUI.transform.Find("BtnNewGameConfirm")?.GetComponent<Button>();
        if (btnNewGameBack == null) btnNewGameBack = newgameUI.transform.Find("BtnNewGameBack")?.GetComponent<Button>();
      }

      // 5. Mode Select UI
      if (newgameModeSelectedUI != null)
      {
        if (btnNewGameSingle == null) btnNewGameSingle = newgameModeSelectedUI.transform.Find("BtnNewGameSingle")?.GetComponent<Button>();
        if (btnNewGameCoop == null) btnNewGameCoop = newgameModeSelectedUI.transform.Find("BtnNewGameCoop")?.GetComponent<Button>();
      }
    }

    private void RegisterEvents()
    {
      // Main Buttons
      BindButton(btnContinue, OnContinueClick);
      BindButton(btnNewGame, OnNewGameClick);
      BindButton(btnSettings, OnSettingsClick);
      BindButton(btnCredits, OnCreditsClick);
      BindButton(btnQuit, OnQuitClick);

      // Continue UI Buttons
      BindButton(btnContinueBack, ReturnToMainMenu);
      BindButton(btnContinueConfirm, OnContinueConfirmClick);

      // Newgame UI Buttons
      BindButton(btnNewGameBack, ReturnToMainMenu);
      BindButton(btnNewGameConfirm, OnNewGameConfirmClick);

      // Mode Select Buttons
      BindButton(btnNewGameSingle, OnSingleplayerClick);
      BindButton(btnNewGameCoop, OnCoopClick);

      // Dark Overlay click to close popups
      AddOverlayCloseListener(continueUI, ReturnToMainMenu);
      AddOverlayCloseListener(newgameUI, ReturnToMainMenu);
      AddOverlayCloseListener(newgameModeSelectedUI, ReturnToMainMenu);
    }

    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
      if (btn == null) return;
      btn.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        action?.Invoke();
      });
    }

    private void AddOverlayCloseListener(GameObject panel, UnityEngine.Events.UnityAction action)
    {
      if (panel == null) return;
      Transform overlay = panel.transform.Find("DarkOverlay");
      if (overlay != null)
      {
        Button btn = overlay.GetComponent<Button>() ?? overlay.gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
          AudioManager.Instance?.PlayUIInteract3();
          action?.Invoke();
        });
      }
    }

    private void OnContinueClick()
    {
      RefreshSavePreview();
      ShowOnlyPanel(continueUI);
    }

    private void OnContinueConfirmClick()
    {
      // Nạp tiến trình và vào LandScene
      LoadGameplayScene();
    }

    private void OnNewGameClick()
    {
      // Mở popup xác nhận New Game (cảnh báo xóa save cũ)
      ShowOnlyPanel(newgameUI);
    }

    private void OnNewGameConfirmClick()
    {
      // Người chơi đồng ý -> Chuyển sang chọn chế độ (Single / Coop)
      ShowOnlyPanel(newgameModeSelectedUI);
    }

    private void OnSingleplayerClick()
    {
      // Bắt đầu chuyến phiêu lưu mới
      LoadGameplayScene();
    }

    private void OnCoopClick()
    {
      Debug.Log("[MainMenuController] Chế độ Coop đang được phát triển!");
      // Hiệu ứng phản hồi nhẹ
      StartCoroutine(TemporaryButtonFeedback(btnNewGameCoop, "Coming Soon!"));
    }

    private void OnSettingsClick()
    {
      if (settingUI != null)
      {
        settingUI.Open();
      }
    }

    private void OnCreditsClick()
    {
      Debug.Log("[MainMenuController] Infinite Labyrinth - Đồ Án Tốt Nghiệp 3D Action RPG Roguelite.");
      StartCoroutine(TemporaryButtonFeedback(btnCredits, "Infinite Labyrinth"));
    }

    private void OnQuitClick()
    {
      Debug.Log("[MainMenuController] Thoát trò chơi.");
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    private void HandleEscapeKey()
    {
      if (settingUI != null && settingUI.IsOpen)
      {
        settingUI.Close();
        return;
      }

      if (newgameModeSelectedUI != null && newgameModeSelectedUI.activeSelf)
      {
        ShowOnlyPanel(newgameUI);
        return;
      }

      if ((newgameUI != null && newgameUI.activeSelf) || (continueUI != null && continueUI.activeSelf))
      {
        ReturnToMainMenu();
      }
    }

    public void ReturnToMainMenu()
    {
      ShowOnlyPanel(mainMenuUI);
      if (settingUI != null) settingUI.Close();
    }

    private void ShowOnlyPanel(GameObject target)
    {
      if (continueUI != null) continueUI.SetActive(continueUI == target);
      if (newgameUI != null) newgameUI.SetActive(newgameUI == target);
      if (newgameModeSelectedUI != null) newgameModeSelectedUI.SetActive(newgameModeSelectedUI == target);

      // MainMenuUI luôn hiển thị phía sau các modal popup mờ
      if (mainMenuUI != null)
      {
        mainMenuUI.SetActive(true);
      }
    }

    /// <summary>
    /// Hiển thị tóm tắt thông tin save game lên khung ContinueUI.
    /// (Sẽ kết nối đầy đủ với SaveManager ở bước sau).
    /// </summary>
    private void RefreshSavePreview()
    {
      if (dayCountText != null) dayCountText.text = "Day 1";
      if (floorCountText != null) floorCountText.text = "Floor 1";
      if (levelCountText != null) levelCountText.text = "Level 1";
      if (goldCountText != null) goldCountText.text = "0000";
    }

    private void LoadGameplayScene()
    {
      string scene = !string.IsNullOrEmpty(targetGameplayScene) ? targetGameplayScene : "LandScene";

      if (LoadingScreenManager.HasInstance)
      {
        LoadingScreenManager.Instance.LoadScene(scene, 1);
      }
      else
      {
        SceneManager.LoadScene(scene);
      }
    }

    private IEnumerator TemporaryButtonFeedback(Button btn, string temporaryText)
    {
      if (btn == null) yield break;
      TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
      if (label == null) yield break;

      string original = label.text;
      label.text = $"<color=#FFDE6A>{temporaryText}</color>";
      yield return new WaitForSecondsRealtime(1.2f);
      label.text = original;
    }

    private void OnValidate()
    {
      AutoFindReferences();
    }
  }
}
