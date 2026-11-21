using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý giao diện Tạm dừng (PauseMenu) trong game.
  /// Bắt phím Esc để tạm dừng thế giới (Time.timeScale = 0), mở chuột,
  /// điều khiển Resume, Settings, SaveGame và Back to Main Menu.
  /// </summary>
  [DisallowMultipleComponent]
  public class PauseMenuController : MonoBehaviour
  {
    private static PauseMenuController instance;
    public static PauseMenuController Instance => instance;
    public static bool HasInstance => instance != null;

    [Header("=== PANELS ===")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private SettingsUIController settingUI;

    [Header("=== BUTTONS ===")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnSaveGame;
    [SerializeField] private Button btnBackToMenu;

    [Header("=== MAIN MENU SCENE NAME ===")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    public static bool IsPaused { get; private set; } = false;

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }
      instance = this;

      AutoFindReferences();
      RegisterEvents();
    }

    private void Start()
    {
      ResumeGame(true);
    }

    private void OnDestroy()
    {
      if (instance == this)
      {
        instance = null;
        IsPaused = false;
        // Bảo đảm không bao giờ để timeScale bị kẹt ở 0 khi chuyển scene
        Time.timeScale = 1f;
      }
    }

    private void Update()
    {
      if (UnityEngine.InputSystem.Keyboard.current == null) return;

      if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        HandleEscapeKey();
      }
    }

    public void AutoFindReferences()
    {
      if (pauseMenuUI == null)
      {
        Transform pUI = transform.Find("PauseMenuUI");
        if (pUI != null) pauseMenuUI = pUI.gameObject;
      }

      Transform settingT = transform.Find("SettingUI");
      if (settingT != null && settingUI == null)
      {
        settingUI = settingT.GetComponent<SettingsUIController>();
      }

      Transform btnContainer = transform.Find("PauseMenuUI/ButtonContainer");
      if (btnContainer != null)
      {
        if (btnResume == null) btnResume = btnContainer.Find("BtnResume")?.GetComponent<Button>();
        if (btnSettings == null) btnSettings = btnContainer.Find("BtnSettings")?.GetComponent<Button>();
        if (btnSaveGame == null) btnSaveGame = btnContainer.Find("BtnSaveGame")?.GetComponent<Button>();
        if (btnBackToMenu == null) btnBackToMenu = btnContainer.Find("BtnBackToMenu")?.GetComponent<Button>();
      }
    }

    private void RegisterEvents()
    {
      if (btnResume != null) btnResume.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        ResumeGame(true);
      });
      if (btnSettings != null) btnSettings.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        OnSettingsClick();
      });
      if (btnSaveGame != null) btnSaveGame.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        OnSaveGameClick();
      });
      if (btnBackToMenu != null) btnBackToMenu.onClick.AddListener(() => {
        AudioManager.Instance?.PlayUIInteract3();
        OnBackToMenuClick();
      });

      if (settingUI != null)
      {
        settingUI.OnClosed += () => {
          if (IsPaused && pauseMenuUI != null)
          {
            pauseMenuUI.SetActive(true);
          }
        };
      }
    }

    private void HandleEscapeKey()
    {
      // 1. Nếu có bất kỳ cửa sổ gameplay nào khác đang mở -> để cửa sổ đó tự xử lý phím Esc
      if (IsAnyOtherGameplayUIOpen())
      {
        return;
      }

      // 2. Nếu đang mở bảng Settings bên trong PauseMenu -> đóng Settings, quay lại PauseMenu
      if (settingUI != null && settingUI.IsOpen)
      {
        settingUI.Close();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        return;
      }

      // 3. Nếu đang tạm dừng -> Tiếp tục game
      if (IsPaused)
      {
        ResumeGame(true);
      }
      // 4. Nếu đang chơi bình thường -> Mở PauseMenu
      else
      {
        PauseGame();
      }
    }

    /// <summary>
    /// Kiểm tra xem có giao diện gameplay nào khác đang mở hay không để tránh xung đột phím Esc.
    /// </summary>
    private bool IsAnyOtherGameplayUIOpen()
    {
      if (ShopUIController.IsOpen) return true;
      if (BlacksmithUIController.IsOpen) return true;
      if (InventoryUIController.IsOpen) return true;
      if (VictoryPortalUI.IsOpen) return true;
      if (AdminCheatManager.IsOpen) return true;

      // Bản đồ
      if (DungeonMapManager.Instance != null && DungeonMapManager.Instance.IsOpen) return true;
      if (VillageMapManager.Instance != null && VillageMapManager.Instance.IsOpen) return true;

      return false;
    }

    public void PauseGame()
    {
      IsPaused = true;
      Time.timeScale = 0f;

      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
      if (settingUI != null) settingUI.Close();
    }

    private int lockCursorCountdown = 0;

    public void ResumeGame(bool lockCursor = true)
    {
      IsPaused = false;
      Time.timeScale = 1f;

      if (UnityEngine.EventSystems.EventSystem.current != null)
      {
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
      }

      if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
      if (settingUI != null) settingUI.Close();

      if (lockCursor)
      {
        LockCursor();
        lockCursorCountdown = 10;
      }
    }

    private void LateUpdate()
    {
      if (lockCursorCountdown > 0)
      {
        // Chờ người chơi nhả phím Esc ra hoàn toàn (đặc biệt trong Unity Editor)
        if (UnityEngine.InputSystem.Keyboard.current == null || !UnityEngine.InputSystem.Keyboard.current.escapeKey.isPressed)
        {
          if (!IsPaused && !IsAnyOtherGameplayUIOpen())
          {
            LockCursor();
          }
          lockCursorCountdown--;
        }
      }
    }

    public void LockCursor()
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }

    private void OnSettingsClick()
    {
      if (settingUI != null)
      {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        settingUI.Open();
      }
    }

    private void OnSaveGameClick()
    {
      // Phản hồi tạm thời trên nút (sẽ kết nối SaveManager ở bước kế tiếp)
      Debug.Log("[PauseMenuController] Tiến trình game sẵn sàng lưu vào savegame.json.");
      StartCoroutine(TemporaryButtonFeedback(btnSaveGame, "Ready to Save!"));
    }

    private void OnBackToMenuClick()
    {
      Time.timeScale = 1f;
      IsPaused = false;
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      string scene = !string.IsNullOrEmpty(mainMenuSceneName) ? mainMenuSceneName : "MainMenuScene";
      SceneManager.LoadScene(scene);
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
