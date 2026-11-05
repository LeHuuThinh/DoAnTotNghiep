using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteLabyrinth.UI
{
    /// <summary>
    /// Điều khiển Màn Hình Tử Trận & Vòng Lặp Hồi Sinh (Task GM-02):
    /// - Mặc định ẩn (alpha = 0, blocksRaycasts = false).
    /// - Tự động bắt sự kiện Player chết (Health.onDeath).
    /// - Chờ 1.2s hoạt cảnh ngã gục -> Mở khóa con trỏ chuột -> Fade In biểu ngữ "YOU DIED".
    /// - Bấm nút RESPAWN -> Kích hoạt vòng lặp hồi sinh tại Chuông Làng (GameManager.RespawnAtVillage).
    /// </summary>
    public class DeadScreenUIController : MonoBehaviour
    {
        public static DeadScreenUIController Instance { get; private set; }

        [Header("=== THAM CHIẾU GIAO DIỆN (UI REFERENCES) ===")]
        [SerializeField] private RectTransform deadScreenRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI youDiedText;
        [SerializeField] private Button btnRespawn;
        [SerializeField] private TextMeshProUGUI respawnButtonText;

        [Header("=== CẤU HÌNH HIỆU ỨNG (ANIMATION SETTINGS) ===")]
        [Tooltip("Thời gian chờ trước khi hiện màn hình (để hoạt cảnh ngã gục kịp diễn ra)")]
        [SerializeField] private float deathDelay = 1.2f;

        [Tooltip("Thời gian mờ dần hiện rõ màn hình YOU DIED (Fade In)")]
        [SerializeField] private float fadeInDuration = 1.0f;

        [Tooltip("Thời gian mờ dần biến mất khi hồi sinh (Fade Out)")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("=== HIỆU ỨNG ÂM THANH ===")]
        [SerializeField] private AudioClip youDiedSound;
        [SerializeField] private AudioClip respawnClickSound;
        [SerializeField] private AudioSource audioSource;

        private Health cachedPlayerHealth;
        private Coroutine fadeRoutine = null;
        private bool isDeadSequenceActive = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoWireOnSceneLoaded()
        {
            if (FindFirstObjectByType<DeadScreenUIController>() != null) return;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                Transform deadObj = FindRecursive(c.transform, "DeadScreenUI");
                if (deadObj != null)
                {
                    DeadScreenUIController ctrl = deadObj.gameObject.AddComponent<DeadScreenUIController>();
                    ctrl.EnsureReferences();
                    Debug.Log("<color=green>[DeadScreenUIController]</color> ⚡ Tự động gắn DeadScreenUIController vào DeadScreenUI khi Scene nạp!");
                    break;
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Cho phép giữ instance mới nhất
            }
            Instance = this;

            EnsureReferences();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (btnRespawn != null)
            {
                btnRespawn.onClick.RemoveAllListeners();
                btnRespawn.onClick.AddListener(HandleRespawnClicked);
            }
        }

        private void OnEnable()
        {
            HookPlayerHealth();
        }

        private void OnDisable()
        {
            UnhookPlayerHealth();
        }

        private void Start()
        {
            // Đảm bảo ẩn ngay từ frame đầu
            HideImmediate();

            if (cachedPlayerHealth == null)
            {
                HookPlayerHealth();
            }
        }

        /// <summary>
        /// Tìm và đăng ký lắng nghe sự kiện tử trận từ Player.
        /// </summary>
        public void HookPlayerHealth()
        {
            if (cachedPlayerHealth != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                cachedPlayerHealth = player.GetComponent<Health>();
                if (cachedPlayerHealth != null)
                {
                    cachedPlayerHealth.onDeath.RemoveListener(OnPlayerDeath);
                    cachedPlayerHealth.onDeath.AddListener(OnPlayerDeath);
                }
            }
        }

        public void UnhookPlayerHealth()
        {
            if (cachedPlayerHealth != null)
            {
                cachedPlayerHealth.onDeath.RemoveListener(OnPlayerDeath);
                cachedPlayerHealth = null;
            }
        }

        private void OnPlayerDeath()
        {
            if (isDeadSequenceActive) return;
            isDeadSequenceActive = true;

            StartCoroutine(PlayerDeathSequenceRoutine());
        }

        private IEnumerator PlayerDeathSequenceRoutine()
        {
            Debug.Log("<color=red>[DeadScreenUI]</color> 💀 Player tử trận! Đang chờ hoạt cảnh ngã gục...");

            // 1. Chờ hoạt cảnh ngã gục diễn ra
            yield return new WaitForSeconds(deathDelay);

            // 2. Mở khóa chuột để người chơi có thể bấm nút Respawn
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 3. Phát âm thanh You Died nếu có
            if (youDiedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(youDiedSound);
            }

            // 4. Fade In màn hình YOU DIED
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
        }

        /// <summary>
        /// Xử lý khi bấm nút RESPAWN
        /// </summary>
        private void HandleRespawnClicked()
        {
            Debug.Log("<color=yellow>[DeadScreenUI]</color> 🔄 Người chơi bấm RESPAWN!");

            if (respawnClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(respawnClickSound);
            }

            if (btnRespawn != null)
            {
                btnRespawn.interactable = false;
            }

            isDeadSequenceActive = false;

            // Kích hoạt hồi sinh xuyên scene tại Chuông Làng
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RespawnAtVillage();
            }
            else
            {
                // Fallback nếu thiếu GameManager
                UnityEngine.SceneManagement.SceneManager.LoadScene("LandScene");
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool enableInteraction)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float timer = 0f;

            if (enableInteraction)
            {
                canvasGroup.blocksRaycasts = true;
            }

            while (timer < fadeInDuration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = enableInteraction;
            canvasGroup.blocksRaycasts = enableInteraction;
            fadeRoutine = null;
        }

        /// <summary>
        /// Ẩn ngay lập tức màn hình tử trận.
        /// </summary>
        public void HideImmediate()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (btnRespawn != null)
            {
                btnRespawn.interactable = true;
            }

            isDeadSequenceActive = false;
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

            if (deadScreenRoot == null)
            {
                Transform t = FindRecursive(searchRoot, "DeadScreenUI");
                if (t != null) deadScreenRoot = t as RectTransform;
                else deadScreenRoot = transform as RectTransform;
            }

            if (deadScreenRoot != null)
            {
                if (canvasGroup == null)
                {
                    canvasGroup = deadScreenRoot.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = deadScreenRoot.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                if (youDiedText == null)
                {
                    Transform t = FindRecursive(deadScreenRoot, "Text");
                    if (t != null) youDiedText = t.GetComponent<TextMeshProUGUI>();
                }

                if (btnRespawn == null)
                {
                    Transform t = FindRecursive(deadScreenRoot, "BtnRespawn");
                    if (t != null)
                    {
                        btnRespawn = t.GetComponent<Button>();
                        Transform btnText = FindRecursive(t, "Text");
                        if (btnText != null) respawnButtonText = btnText.GetComponent<TextMeshProUGUI>();
                    }
                }

                if (audioSource == null)
                {
                    audioSource = deadScreenRoot.GetComponent<AudioSource>();
                }
            }
        }

        private static Transform FindRecursive(Transform parent, string childName)
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
            HideImmediate();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log("<color=green>[DeadScreenUIController]</color> ✅ Đã tự động liên kết tham chiếu DeadScreenUI!");
        }

        [ContextMenu("💀 Test Trigger Player Death (Play Mode)")]
        public void TestTriggerDeath()
        {
            OnPlayerDeath();
        }

        [ContextMenu("👁️ Test Show Dead Screen (Force Open)")]
        public void TestForceShow()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [ContextMenu("🙈 Test Hide Dead Screen")]
        public void TestHide()
        {
            HideImmediate();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
