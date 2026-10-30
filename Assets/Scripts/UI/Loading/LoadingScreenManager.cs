using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Central singleton orchestrator for scene transitions and asynchronous loading.
  /// Persists across scenes (DontDestroyOnLoad), coordinates background scene loads,
  /// maps progress phases (0% -> 40% scene load, 40% -> 100% PCG / world generation),
  /// and guarantees minimum display time for tips readability.
  /// </summary>
  public class LoadingScreenManager : MonoBehaviour
  {
    private const string DefaultCanvasPrefabPath = "LoadingScreenCanvas";
    private const string DefaultDataPath = "LoadingScreenData";

    private static LoadingScreenManager instance;

    [Header("=== CONFIGURATION ===")]
    [Tooltip("Reference to the LoadingScreenData ScriptableObject")]
    [SerializeField] private LoadingScreenData screenData;

    [Tooltip("Reference to the LoadingScreenUI view controller")]
    [SerializeField] private LoadingScreenUI screenUI;

    [Tooltip("Minimum loading time in seconds to guarantee tip readability on fast NVMe SSDs")]
    [SerializeField] private float minLoadingTime = 1.6f;

    [Header("=== RUNTIME STATE ===")]
    [SerializeField] private bool isLoading = false;
    [SerializeField] private int currentFloor = 1;

    // Events for other systems to listen to
    public event Action<string> OnLoadStarted;
    public event Action<string> OnSceneLoaded;
    public event Action<string> OnLoadCompleted;

    public static LoadingScreenManager Instance
    {
      get
      {
        if (instance == null)
        {
          EnsureInstance();
        }
        return instance;
      }
    }

    public bool IsLoading => isLoading;
    public int CurrentFloor => currentFloor;

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);

      EnsureDependencies();
    }

    /// <summary>
    /// Guarantees that an instance of LoadingScreenManager and its UI Canvas exist in the runtime.
    /// Auto-creates them from Resources if not already present.
    /// </summary>
    public static void EnsureInstance()
    {
      if (instance != null) return;

      instance = FindAnyObjectByType<LoadingScreenManager>();
      if (instance != null)
      {
        DontDestroyOnLoad(instance.gameObject);
        instance.EnsureDependencies();
        return;
      }

      // Auto-create persistent GameObject
      GameObject managerObj = new GameObject("[LoadingScreenManager]");
      instance = managerObj.AddComponent<LoadingScreenManager>();
      DontDestroyOnLoad(managerObj);
      instance.EnsureDependencies();
    }

    private void EnsureDependencies()
    {
      // 1. Ensure Data
      if (screenData == null)
      {
        screenData = Resources.Load<LoadingScreenData>(DefaultDataPath);
      }

      // 2. Ensure UI Canvas
      if (screenUI == null)
      {
        screenUI = FindAnyObjectByType<LoadingScreenUI>();

        if (screenUI == null)
        {
          GameObject canvasPrefab = Resources.Load<GameObject>(DefaultCanvasPrefabPath);
          if (canvasPrefab != null)
          {
            GameObject canvasObj = Instantiate(canvasPrefab);
            canvasObj.name = "LoadingScreenCanvas";
            screenUI = canvasObj.GetComponent<LoadingScreenUI>();
            DontDestroyOnLoad(canvasObj);
          }
          else
          {
            Debug.LogWarning("[LoadingScreenManager] Could not find 'LoadingScreenCanvas' in Resources!");
          }
        }
        else
        {
          DontDestroyOnLoad(screenUI.gameObject);
        }
      }
    }

    /// <summary>
    /// Loads the target scene with standard asynchronous transition and tip display.
    /// </summary>
    public void LoadScene(string sceneName, int targetFloor = 1)
    {
      LoadSceneWithStagedCallback(sceneName, targetFloor, null);
    }

    /// <summary>
    /// Chạy hiệu ứng màn hình Loading Screen cho các tác vụ ngay trong Scene mà không đổi Scene
    /// (ví dụ: Tạo lại hầm ngục trong Admin Console hoặc bấm Play trực tiếp trong DungeonScene).
    /// </summary>
    public void RunInSceneLoading(string locationSceneName, int floorNumber, Func<Action<float>, IEnumerator> taskRoutine, Action onCompleted = null, bool instant = false)
    {
      if (isLoading)
      {
        Debug.LogWarning("[LoadingScreenManager] Đang bận một tiến trình tải khác.");
        return;
      }

      EnsureDependencies();
      StartCoroutine(InSceneLoadingRoutine(locationSceneName, floorNumber, taskRoutine, onCompleted, instant));
    }

    private IEnumerator InSceneLoadingRoutine(string locationSceneName, int floorNumber, Func<Action<float>, IEnumerator> taskRoutine, Action onCompleted, bool instant)
    {
      isLoading = true;
      currentFloor = floorNumber;
      OnLoadStarted?.Invoke(locationSceneName);

      LockPlayerInput(true);

      // 1. Hiện UI Loading Screen với mẹo và ảnh nền mới (hỗ trợ instant khi cold start)
      bool fadeInDone = false;
      if (screenUI != null)
      {
        screenUI.Show(locationSceneName, floorNumber, screenData, () => fadeInDone = true, instant);
      }
      else
      {
        fadeInDone = true;
      }

      while (!fadeInDone)
      {
        yield return null;
      }

      float transitionStartTime = Time.unscaledTime;

      // 2. Chạy tác vụ sinh hầm ngục trong Scene (tiến trình 0% -> 100%)
      if (taskRoutine != null)
      {
        Action<float> reportProgress = (p) =>
        {
          float normalized = Mathf.Clamp01(p);
          if (screenUI != null) screenUI.SetProgress(normalized);
        };

        yield return StartCoroutine(taskRoutine(reportProgress));
      }
      else
      {
        float timer = 0f;
        while (timer < 1f)
        {
          timer += Time.unscaledDeltaTime * 2f;
          if (screenUI != null) screenUI.SetProgress(timer);
          yield return null;
        }
      }

      // 3. Đảm bảo thời gian đọc mẹo tối thiểu
      float elapsedTime = Time.unscaledTime - transitionStartTime;
      if (elapsedTime < minLoadingTime)
      {
        yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
      }

      // 4. Đảm bảo thanh loading trực quan đạt 100%
      if (screenUI != null)
      {
        screenUI.SetProgress(1.0f);
        while (screenUI.DisplayedProgress < 0.98f)
        {
          yield return null;
        }
        yield return new WaitForSecondsRealtime(0.2f);
      }

      // 5. Fade out và trả quyền điều khiển cho Player
      if (screenUI != null)
      {
        screenUI.Hide(() =>
        {
          isLoading = false;
          LockPlayerInput(false);
          onCompleted?.Invoke();
          OnLoadCompleted?.Invoke(locationSceneName);
        });
      }
      else
      {
        isLoading = false;
        LockPlayerInput(false);
        onCompleted?.Invoke();
        OnLoadCompleted?.Invoke(locationSceneName);
      }
    }

    private void LockPlayerInput(bool locked)
    {
      PlayerController pc = FindAnyObjectByType<PlayerController>();
      if (pc != null)
      {
        if (locked) pc.LockMovement();
        else pc.UnlockMovement();
      }
    }

    /// <summary>
    /// Loads the target scene and coordinates with a staged generator (e.g., PCG Dungeon)
    /// to report granular progress between 40% and 100%.
    /// </summary>
    public void LoadSceneWithStagedCallback(string sceneName, int targetFloor, Func<Action<float>, IEnumerator> stagedRoutine)
    {
      if (isLoading)
      {
        Debug.LogWarning($"[LoadingScreenManager] Already loading a scene. Ignored request for: {sceneName}");
        return;
      }

      EnsureDependencies();
      StartCoroutine(TransitionRoutine(sceneName, targetFloor, stagedRoutine));
    }

    private IEnumerator TransitionRoutine(string sceneName, int targetFloor, Func<Action<float>, IEnumerator> stagedRoutine)
    {
      isLoading = true;
      currentFloor = targetFloor;
      OnLoadStarted?.Invoke(sceneName);

      LockPlayerInput(true);

      // 1. Show Loading Screen UI with fresh randomized tip and background
      bool fadeInDone = false;
      if (screenUI != null)
      {
        screenUI.Show(sceneName, targetFloor, screenData, () => fadeInDone = true);
      }
      else
      {
        fadeInDone = true;
      }

      // Wait for fade in
      while (!fadeInDone)
      {
        yield return null;
      }

      float transitionStartTime = Time.unscaledTime;

      // 2. Phase 1: Asynchronous Scene Loading (0% -> 40%)
      AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
      if (asyncOp == null)
      {
        Debug.LogError($"[LoadingScreenManager] Failed to load scene: '{sceneName}'. Check Build Settings!");
        isLoading = false;
        LockPlayerInput(false);
        yield break;
      }

      asyncOp.allowSceneActivation = false;

      while (asyncOp.progress < 0.9f)
      {
        float sceneProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
        float visualProgress = Mathf.Lerp(0.0f, 0.4f, sceneProgress);

        if (screenUI != null)
        {
          screenUI.SetProgress(visualProgress);
        }

        yield return null;
      }

      // Scene is ready in memory
      if (screenUI != null)
      {
        screenUI.SetProgress(0.4f);
      }

      // 3. Activate the new scene
      asyncOp.allowSceneActivation = true;

      while (!asyncOp.isDone)
      {
        yield return null;
      }

      OnSceneLoaded?.Invoke(sceneName);

      // Wait a frame for Awake/Start scripts in the new scene to initialize
      yield return null;

      // Đảm bảo Player trong Scene mới cũng bị khóa điều khiển trong khi đang tạo thế giới PCG
      LockPlayerInput(true);

      // 4. Phase 2: Generation / World Setup (40% -> 100%)
      if (stagedRoutine != null)
      {
        // Custom staged routine (e.g. PCG Dungeon Generation)
        Action<float> reportProgress = (stageProgress) =>
        {
          float normalized = Mathf.Clamp01(stageProgress);
          float combined = Mathf.Lerp(0.4f, 1.0f, normalized);
          if (screenUI != null)
          {
            screenUI.SetProgress(combined);
          }
        };

        yield return StartCoroutine(stagedRoutine(reportProgress));
      }
      else
      {
        // Auto-detect DungeonManager in the newly loaded scene
        DungeonManager dungeonMgr = FindAnyObjectByType<DungeonManager>();
        if (dungeonMgr != null)
        {
          Action<float> reportProgress = (stageProgress) =>
          {
            float normalized = Mathf.Clamp01(stageProgress);
            float combined = Mathf.Lerp(0.4f, 1.0f, normalized);
            if (screenUI != null)
            {
              screenUI.SetProgress(combined);
            }
          };

          yield return StartCoroutine(dungeonMgr.GenerateWorldRoutine(reportProgress));
        }
        else
        {
          // Smooth linear progression for non-PCG scenes (e.g., LandScene)
          float progressTimer = 0f;
          float progressDuration = 0.6f;

          while (progressTimer < progressDuration)
          {
            progressTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(progressTimer / progressDuration);
            float combined = Mathf.Lerp(0.4f, 1.0f, t);

            if (screenUI != null)
            {
              screenUI.SetProgress(combined);
            }

            yield return null;
          }
        }
      }

      // 5. Ensure minimum display time for readability
      float elapsedTime = Time.unscaledTime - transitionStartTime;
      if (elapsedTime < minLoadingTime)
      {
        yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
      }

      // 6. Guarantee visual 100% completion before hiding
      if (screenUI != null)
      {
        screenUI.SetProgress(1.0f);
        while (screenUI.DisplayedProgress < 0.98f)
        {
          yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
      }

      // 7. Fade Out and return controls to player
      if (screenUI != null)
      {
        screenUI.Hide(() =>
        {
          isLoading = false;
          LockPlayerInput(false);
          OnLoadCompleted?.Invoke(sceneName);
        });
      }
      else
      {
        isLoading = false;
        LockPlayerInput(false);
        OnLoadCompleted?.Invoke(sceneName);
      }
    }
  }
}
