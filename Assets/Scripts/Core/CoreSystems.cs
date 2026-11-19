using UnityEngine;

namespace InfiniteLabyrinth.Core
{
    /// <summary>
    /// Prefab Tổng Quản lý Toàn Bộ Phân Hệ Cốt Lõi (Core Systems Architecture):
    /// - Gom cụm toàn bộ các Manager cốt lõi: GameManager, AudioManager, InventoryManager,
    ///   PlayerProgression, LoadingScreenManager, AdminCheatManager, DamageNumberManager.
    /// - Đảm bảo Single Source of Truth giữa các Scene (LandScene, DungeonScene, MainMenu).
    /// - Duy nhất 1 instance DontDestroyOnLoad tồn tại xuyên suốt vòng đời trò chơi.
    /// - Tự động nạp qua RuntimeInitializeOnLoadMethod nếu scene chưa có.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class CoreSystems : MonoBehaviour
    {
        public static CoreSystems Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Header("=== GHI CHÚ KIẾN TRÚC ===")]
        [SerializeField, TextArea(3, 6)]
        private string architecturalNote = 
            "Prefab Tổng [CORE_SYSTEMS]:\n" +
            "Chứa toàn bộ các hệ thống cốt lõi (GameManager, AudioManager, Inventory, Loading, Cheat, Progression, DamageNumber).\n" +
            "Đồng bộ 100% giữa LandScene và DungeonScene.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrap()
        {
            if (Instance != null) return;

            var existing = FindFirstObjectByType<CoreSystems>();
            if (existing != null)
            {
                Instance = existing;
                if (existing.transform.parent == null)
                {
                    DontDestroyOnLoad(existing.gameObject);
                }
                return;
            }

            // Tự động load từ Resources nếu có
            GameObject prefab = Resources.Load<GameObject>("CoreSystems");
            if (prefab != null)
            {
                GameObject instanceObj = Instantiate(prefab);
                instanceObj.name = "[CORE_SYSTEMS]";
                Instance = instanceObj.GetComponent<CoreSystems>();
                DontDestroyOnLoad(instanceObj);
                Debug.Log("<color=green>[CoreSystems]</color> 🌐 Tự động khởi tạo [CORE_SYSTEMS] từ Resources.Load!");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
