using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace InfiniteLabyrinth.UI
{
    /// <summary>
    /// Điều khiển hiển thị thông tin Thế giới & Vòng lặp Ngày/Tầng (Task GM-01):
    /// 1. TopCornerInfo (Góc trên phải): Hiển thị Day, Floor, Current Place ổn định suốt trận.
    /// 2. MidScreenInfo (Giữa màn hình): Hiệu ứng Cinematic Area Intro Banner (Fade In / Hold / Fade Out)
    ///    giới thiệu khu vực vừa đặt chân đến (Haven Village hoặc Infinite Labyrinth Floor X).
    /// </summary>
    public class InfoUIController : MonoBehaviour
    {
        public static InfoUIController Instance { get; private set; }

        [Header("=== TOP CORNER INFO (HUD GÓC TRÊN PHẢI) ===")]
        [SerializeField] private RectTransform topCornerRoot;
        [SerializeField] private TextMeshProUGUI dayCountText;
        [SerializeField] private TextMeshProUGUI dungeonFloorText;
        [SerializeField] private TextMeshProUGUI currentPlaceText;

        [Header("=== MID SCREEN INFO (CINEMATIC AREA INTRO BANNER) ===")]
        [SerializeField] private RectTransform midScreenRoot;
        [SerializeField] private CanvasGroup midCanvasGroup;
        [SerializeField] private TextMeshProUGUI midMainText;
        [SerializeField] private TextMeshProUGUI midSideText;

        [Header("=== CẤU HÌNH HIỆU ỨNG (ANIMATION SETTINGS) ===")]
        [Tooltip("Thời gian mờ dần hiện rõ biểu ngữ (Fade In)")]
        [SerializeField] private float fadeInDuration = 0.85f;

        [Tooltip("Thời gian giữ sáng rõ giữa màn hình (Hold Display)")]
        [SerializeField] private float holdDuration = 2.2f;

        [Tooltip("Thời gian mờ dần biến mất (Fade Out)")]
        [SerializeField] private float fadeOutDuration = 1.0f;

        [Tooltip("Tỷ lệ scale ban đầu lúc mới hiện (0.94 -> 1.0 tạo cảm giác dãn nở điện ảnh)")]
        [SerializeField] private float startScale = 0.94f;

        [Tooltip("Tự động phát biểu ngữ giới thiệu khi vừa vào Scene / tầng mới")]
        [SerializeField] private bool playIntroOnSceneEnter = true;

        [Header("=== CẤU HÌNH TÊN VÙNG ĐẤT (LOCALIZATION / LABELS) ===")]
        [SerializeField] private string townPlaceName = "Haven Village";
        [SerializeField] private string townIntroTitle = "HAVEN VILLAGE";
        [SerializeField] private string townIntroSubtitleFormat = "Day {0} — Safe Sanctuary";

        [SerializeField] private string dungeonPlaceName = "Infinite Labyrinth";
        [SerializeField] private string dungeonIntroTitle = "INFINITE LABYRINTH";
        [SerializeField] private string dungeonIntroSubtitleFormat = "Floor {0} — The Frostbound Crypt";

        private Coroutine introBannerRoutine = null;
        private Vector3 midInitialScale = Vector3.one;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoWireOnSceneLoaded()
        {
            if (FindFirstObjectByType<InfoUIController>() != null) return;

            // Tìm Canvas chính để gắn controller nếu chưa có
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                Transform topCorner = FindRecursive(c.transform, "TopCornerInfo");
                if (topCorner != null)
                {
                    InfoUIController ctrl = topCorner.gameObject.AddComponent<InfoUIController>();
                    ctrl.EnsureReferences();
                    ctrl.UpdateTopCornerDisplay();
                    Debug.Log("<color=green>[InfoUIController]</color> ⚡ Tự động gắn InfoUIController vào TopCornerInfo khi Scene nạp!");
                    break;
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Cho phép instance trong scene hoạt động
            }
            Instance = this;

            EnsureReferences();

            if (midCanvasGroup != null)
            {
                midCanvasGroup.alpha = 0f;
                midCanvasGroup.interactable = false;
                midCanvasGroup.blocksRaycasts = false;
            }

            if (midScreenRoot != null)
            {
                midInitialScale = midScreenRoot.localScale;
            }
        }

        private void OnEnable()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.OnDayChanged += HandleDayChanged;
                GameManager.Instance.OnFloorChanged += HandleFloorChanged;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.OnLoadCompleted += HandleLoadingCompleted;
            }
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.OnDayChanged -= HandleDayChanged;
                GameManager.Instance.OnFloorChanged -= HandleFloorChanged;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (LoadingScreenManager.HasInstance)
            {
                LoadingScreenManager.Instance.OnLoadCompleted -= HandleLoadingCompleted;
            }
        }

        private void Start()
        {
            UpdateTopCornerDisplay();

            // Nếu không có màn hình loading đang che, phát intro banner sau một nhịp chờ ngắn
            if (playIntroOnSceneEnter)
            {
                if (!LoadingScreenManager.HasInstance || !LoadingScreenManager.Instance.IsLoading)
                {
                    StartCoroutine(DelayedSceneStartIntro(0.45f));
                }
            }
        }

        private IEnumerator DelayedSceneStartIntro(float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerCurrentAreaIntro();
        }

        private void HandleLoadingCompleted(string loadedSceneName)
        {
            UpdateTopCornerDisplay();

            if (playIntroOnSceneEnter)
            {
                TriggerCurrentAreaIntro();
            }
        }

        private void HandleDayChanged(int newDay)
        {
            UpdateTopCornerDisplay();
        }

        private void HandleFloorChanged(int newFloor)
        {
            UpdateTopCornerDisplay();
        }

        private void HandleGameStateChanged(string sceneName, int day, int floor)
        {
            UpdateTopCornerDisplay();
        }

        /// <summary>
        /// Cập nhật nội dung hiển thị cho HUD góc trên bên phải (TopCornerInfo).
        /// </summary>
        public void UpdateTopCornerDisplay()
        {
            int day = GameManager.HasInstance ? GameManager.Instance.CurrentDay : 1;
            int floor = GameManager.HasInstance ? GameManager.Instance.CurrentFloor : 1;
            bool isInTown = GameManager.HasInstance ? GameManager.Instance.IsInTown : (SceneManager.GetActiveScene().name == "LandScene");

            if (dayCountText != null)
            {
                dayCountText.text = $"Day {day}";
            }

            if (isInTown)
            {
                if (currentPlaceText != null)
                {
                    currentPlaceText.text = townPlaceName;
                }
            }
            else
            {
                if (currentPlaceText != null)
                {
                    currentPlaceText.text = dungeonPlaceName;
                }
            }

            // Item 2: Hiển thị số tầng đang đi ngay cả ở trong LandScene giống DungeonScene
            if (dungeonFloorText != null)
            {
                dungeonFloorText.gameObject.SetActive(true);
                dungeonFloorText.text = $"Floor {floor}";
            }
        }

        /// <summary>
        /// Kích hoạt biểu ngữ giới thiệu khu vực dựa trên Scene và trạng thái hiện tại.
        /// </summary>
        public void TriggerCurrentAreaIntro()
        {
            int day = GameManager.HasInstance ? GameManager.Instance.CurrentDay : 1;
            int floor = GameManager.HasInstance ? GameManager.Instance.CurrentFloor : 1;
            bool isInTown = GameManager.HasInstance ? GameManager.Instance.IsInTown : (SceneManager.GetActiveScene().name == "LandScene");

            if (isInTown)
            {
                string subtitle = string.Format(townIntroSubtitleFormat, day);
                PlayAreaIntro(townIntroTitle, subtitle);
            }
            else
            {
                string subtitle = string.Format(dungeonIntroSubtitleFormat, floor);
                PlayAreaIntro(dungeonIntroTitle, subtitle);
            }
        }

        /// <summary>
        /// Phát hoạt cảnh biểu ngữ giới thiệu khu vực ở giữa màn hình (Fade In -> Hold -> Fade Out).
        /// </summary>
        public void PlayAreaIntro(string title, string subtitle)
        {
            if (introBannerRoutine != null)
            {
                StopCoroutine(introBannerRoutine);
            }
            introBannerRoutine = StartCoroutine(AreaIntroBannerRoutine(title, subtitle));
        }

        private IEnumerator AreaIntroBannerRoutine(string title, string subtitle)
        {
            if (midCanvasGroup == null) yield break;

            if (midMainText != null) midMainText.text = title;
            if (midSideText != null) midSideText.text = subtitle;

            midCanvasGroup.interactable = false;
            midCanvasGroup.blocksRaycasts = false;

            // 1. PHASE 1: FADE IN & CINEMATIC EXPANSION
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                // Đường cong mượt mà Ease Out
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

                midCanvasGroup.alpha = smoothT;

                if (midScreenRoot != null)
                {
                    float currentScale = Mathf.Lerp(startScale, 1.0f, smoothT);
                    midScreenRoot.localScale = midInitialScale * currentScale;
                }

                yield return null;
            }

            midCanvasGroup.alpha = 1f;
            if (midScreenRoot != null) midScreenRoot.localScale = midInitialScale;

            // 2. PHASE 2: HOLD DISPLAY
            yield return new WaitForSecondsRealtime(holdDuration);

            // 3. PHASE 3: FADE OUT
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                midCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            midCanvasGroup.alpha = 0f;
            introBannerRoutine = null;
        }

        /// <summary>
        /// Tự động dò tìm các GameObject và gán tham chiếu thông minh.
        /// </summary>
        public void EnsureReferences()
        {
            Transform searchRoot = transform;
            if (transform.parent != null && transform.name != "Canvas")
            {
                Canvas rootCanvas = GetComponentInParent<Canvas>();
                if (rootCanvas != null) searchRoot = rootCanvas.transform;
            }

            // 1. Dò tìm TopCornerInfo
            if (topCornerRoot == null)
            {
                Transform t = FindRecursive(searchRoot, "TopCornerInfo");
                if (t != null) topCornerRoot = t as RectTransform;
            }

            if (topCornerRoot != null)
            {
                if (dayCountText == null)
                {
                    Transform t = FindRecursive(topCornerRoot, "DayCount");
                    if (t != null) dayCountText = t.GetComponent<TextMeshProUGUI>();
                }
                if (dungeonFloorText == null)
                {
                    Transform t = FindRecursive(topCornerRoot, "DungeonFloor");
                    if (t != null) dungeonFloorText = t.GetComponent<TextMeshProUGUI>();
                }
                if (currentPlaceText == null)
                {
                    Transform t = FindRecursive(topCornerRoot, "CurrentPlace");
                    if (t != null) currentPlaceText = t.GetComponent<TextMeshProUGUI>();
                }
            }

            // 2. Dò tìm MidScreenInfo
            if (midScreenRoot == null)
            {
                Transform t = FindRecursive(searchRoot, "MidScreenInfo");
                if (t != null) midScreenRoot = t as RectTransform;
            }

            if (midScreenRoot != null)
            {
                if (midCanvasGroup == null)
                {
                    midCanvasGroup = midScreenRoot.GetComponent<CanvasGroup>();
                    if (midCanvasGroup == null)
                    {
                        midCanvasGroup = midScreenRoot.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                if (midMainText == null)
                {
                    Transform t = FindRecursive(midScreenRoot, "MainText");
                    if (t != null) midMainText = t.GetComponent<TextMeshProUGUI>();
                }
                if (midSideText == null)
                {
                    Transform t = FindRecursive(midScreenRoot, "SideText");
                    if (t != null) midSideText = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public static Transform FindRecursive(Transform parent, string childName)
        {
            if (parent.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform res = FindRecursive(parent.GetChild(i), childName);
                if (res != null) return res;
            }
            return null;
        }

        [ContextMenu("⚡ Auto-Wire References")]
        public void ContextAutoWire()
        {
            EnsureReferences();
            UpdateTopCornerDisplay();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log("<color=green>[InfoUIController]</color> ✅ Đã tự động liên kết toàn bộ tham chiếu TopCornerInfo & MidScreenInfo!");
        }

        [ContextMenu("▶ Test Area Intro Banner")]
        public void TestAreaIntroBanner()
        {
            TriggerCurrentAreaIntro();
        }
    }
}
