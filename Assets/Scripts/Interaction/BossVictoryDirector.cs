using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Đạo diễn Điện Ảnh Chiến Thắng Boss & Biểu Ngữ "VICTORY ACHIEVED" (Task D02-5B):
  /// - Lắng nghe sự kiện kết liễu IceGolemController.OnBossDefeated khi Boss cạn máu.
  /// - Hitlag khựng hình 0.22s tôn vinh đòn chém kết liễu.
  /// - Khóa điều khiển Player, chuyển Camera mượt mà tới góc 3/4 mạn phải trên đầu Boss (cự ly 7m, cao 3.5m).
  /// - Boss phát hoạt ảnh ngã gục (Dead) rền vang sàn đấu trường, thanh máu Boss mờ dần.
  /// - Biểu ngữ hoàng kim "VICTORY ACHIEVED" bừng sáng giữa màn hình với âm thanh chuông ngân / hợp xướng.
  /// - Giữ sáng 2.2s rồi mờ dần; khôi phục camera và điều khiển Player (sẵn sàng bàn giao cho Task D02-5C).
  /// </summary>
  public class BossVictoryDirector : MonoBehaviour
  {
    public static BossVictoryDirector Instance { get; private set; }

    // Sự kiện toàn cục báo hiệu các mốc của màn chiến thắng
    public static event System.Action OnVictorySequenceStarted;
    public static event System.Action OnVictoryBannerShown;
    public static event System.Action OnVictoryBannerComplete;
    public static event System.Action OnPedestalRevealStarted;
    public static event System.Action OnPedestalEntitiesSpawned;
    public static event System.Action OnVictorySequenceComplete;

    [Header("=== TRẠNG THÁI ===")]
    [SerializeField] private bool isVictorySequenceActive = false;
    [SerializeField] private bool hasTriggeredVictory = false;

    [Header("=== THAM CHIẾU GIAO DIỆN CHIẾN THẮNG (ENEMYCANVAS) ===")]
    [Tooltip("Khung gốc BossVictoryBanner trong EnemyCanvas")]
    [SerializeField] private RectTransform victoryBannerRoot;
    [Tooltip("CanvasGroup điều khiển Alpha của biểu ngữ chiến thắng")]
    [SerializeField] private CanvasGroup victoryBannerCanvasGroup;
    [Tooltip("Ảnh nền Banner trang trí phong cách Dark Souls")]
    [SerializeField] private Image bannerImage;
    [Tooltip("Dòng chữ TextMeshPro VICTORY ACHIEVED")]
    [SerializeField] private TextMeshProUGUI victoryText;

    [Header("=== PHA 1: HITLAG KẾT LIỄU & KHÓA PLAYER ===")]
    [Tooltip("Thời gian khựng hình Hitlag (giây realtime)")]
    [SerializeField] private float hitlagDuration = 0.22f;
    [Tooltip("Tốc độ thời gian trong lúc hitlag (0.05 = làm chậm 95%)")]
    [SerializeField] private float hitlagTimeScale = 0.05f;

    [Header("=== PHA 2: CAMERA ĐẠO DIỄN GÓC 3/4 NGẮM BOSS NGÃ GỤC ===")]
    [Tooltip("Thời gian Camera lia mượt mà tới góc nhìn 3/4 trên đầu Boss (giây)")]
    [SerializeField] private float panToBossDuration = 1.35f;
    [Tooltip("Cự ly từ Camera tới Boss (mét)")]
    [SerializeField] private float bossCamDistance = 7.0f;
    [Tooltip("Độ cao Camera so với sàn (mét)")]
    [SerializeField] private float bossCamHeight = 3.5f;
    [Tooltip("Góc lệch sang mạn phải so với hướng mặt của Boss (độ)")]
    [SerializeField] private float bossCamAngleOffset = 45f;
    [Tooltip("Tốc độ Camera trôi chậm (Slow Cinematic Drift) trong lúc Boss ngã gục (m/s)")]
    [SerializeField] private float cinematicDriftSpeed = 0.18f;
    [Tooltip("Thời gian chờ sau khi Boss ngã trước khi biểu ngữ bừng sáng (giây)")]
    [SerializeField] private float delayBeforeBanner = 0.9f;

    [Header("=== PHA 3: BIỂU NGỮ HOÀNG KIM 'VICTORY ACHIEVED' ===")]
    [Tooltip("Thời gian biểu ngữ mờ dần hiện rõ (Fade In, giây)")]
    [SerializeField] private float bannerFadeInDuration = 0.7f;
    [Tooltip("Tỉ lệ scale ban đầu khi bắt đầu xuất hiện (phóng nhẹ từ 0.88 lên 1.0)")]
    [SerializeField] private float bannerStartScale = 0.88f;
    [Tooltip("Thời gian giữ biểu ngữ rực sáng giữa màn hình (giây)")]
    [SerializeField] private float bannerHoldDuration = 2.2f;
    [Tooltip("Thời gian biểu ngữ mờ dần biến mất (Fade Out, giây)")]
    [SerializeField] private float bannerFadeOutDuration = 1.0f;
    [Tooltip("Âm thanh hợp xướng / chuông ngân chiến thắng (Victory Fanfare)")]
    [SerializeField] private AudioClip victorySound;

    [Header("=== PHA 4: ĐẠO DIỄN LIA CAM LÊN BỤC 12M & SINH RƯƠNG BOSS + CỔNG (TASK D02-5C) ===")]
    [Tooltip("Bật cutscene lia camera lên Bục Đá để chiêm ngưỡng Rương Boss & Cổng Dịch Chuyển")]
    [SerializeField] private bool enablePedestalReveal = true;
    [Tooltip("Thời gian camera lia mượt mà từ góc Boss tới góc ngắm Bục Đá 12m (giây)")]
    [SerializeField] private float panToPedestalDuration = 1.6f;
    [Tooltip("Thời gian giữ camera ngắm Bục Đá khi Rương và Cổng bừng sáng xuất hiện (giây)")]
    [SerializeField] private float pedestalHoldDuration = 2.2f;
    [Tooltip("Offset đặt Camera so với tâm mặt Bục Đá (Local: X lệch phải, Y độ cao, Z lùi về phía Nam ngắm lên bục)")]
    [SerializeField] private Vector3 pedestalCamLocalOffset = new Vector3(3.5f, 2.8f, -14.0f);
    [Tooltip("Offset điểm nhìn của Camera so với tâm mặt Bục Đá")]
    [SerializeField] private Vector3 pedestalLookLocalOffset = new Vector3(0f, 1.2f, 0.5f);
    [Tooltip("Prefab Rương Boss (Interactive_Chest_Boss)")]
    [SerializeField] private GameObject bossChestPrefab;
    [Tooltip("Prefab Cổng Dịch Chuyển Chiến Thắng (portal.prefab)")]
    [SerializeField] private GameObject victoryPortalPrefab;
    [Tooltip("Hiệu ứng VFX ánh sáng bừng sáng giáng thế trên bục")]
    [SerializeField] private GameObject spawnBurstVfxPrefab;
    [Tooltip("Âm thanh thần thánh khi Rương & Cổng giáng thế")]
    [SerializeField] private AudioClip pedestalSpawnSound;

    [Header("=== PHA 5: KHÔI PHỤC CAMERA & ĐIỀU KHIỂN PLAYER ===")]
    [Tooltip("Thời gian camera lia mượt mà quay về sau lưng Player (giây)")]
    [SerializeField] private float returnToPlayerDuration = 1.5f;
    [Tooltip("Tự động khôi phục camera & player sau khi chuỗi chiến thắng hoàn tất")]
    [SerializeField] private bool autoReturnToPlayer = true;

    // Tham chiếu runtime của rương và cổng đã sinh
    private GameObject spawnedBossChestInstance;
    private GameObject spawnedVictoryPortalInstance;

    public bool IsVictorySequenceActive => isVictorySequenceActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureDirectorInstance()
    {
      if (FindFirstObjectByType<BossVictoryDirector>() == null)
      {
        GameObject obj = new GameObject("BossVictoryDirector");
        obj.AddComponent<BossVictoryDirector>();
        Debug.Log("<color=gold>[BossVictoryDirector]</color> 🏆 Tự động khởi tạo BossVictoryDirector tại runtime.");
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

      EnsureUIReferences();
      EnsurePedestalPrefabs();
    }

    public void EnsurePedestalPrefabs()
    {
#if UNITY_EDITOR
      if (bossChestPrefab == null)
      {
        bossChestPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RPG/Interactive_Chest_Boss.prefab");
      }
      if (victoryPortalPrefab == null)
      {
        victoryPortalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Portal/portal.prefab");
      }
#endif
    }

    private void OnEnable()
    {
      IceGolemController.OnBossDefeated -= HandleBossDefeated;
      IceGolemController.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
      IceGolemController.OnBossDefeated -= HandleBossDefeated;
    }

    /// <summary>
    /// Tự động quét và liên kết cây UI BossVictoryBanner từ EnemyCanvas
    /// </summary>
    public void EnsureUIReferences()
    {
      if (victoryBannerRoot == null)
      {
        // 1. Tìm trong EnemyCanvas
        GameObject canvasObj = GameObject.Find("EnemyCanvas");
        if (canvasObj != null)
        {
          Transform bannerTr = canvasObj.transform.Find("BossVictoryBanner");
          if (bannerTr != null)
          {
            victoryBannerRoot = bannerTr.GetComponent<RectTransform>();
          }
        }

        // 2. Fallback tìm theo tên trên toàn Scene
        if (victoryBannerRoot == null)
        {
          GameObject bannerObj = GameObject.Find("BossVictoryBanner");
          if (bannerObj != null)
          {
            victoryBannerRoot = bannerObj.GetComponent<RectTransform>();
          }
        }
      }

      if (victoryBannerRoot != null)
      {
        // Tự động gắn CanvasGroup nếu chưa có
        if (victoryBannerCanvasGroup == null)
        {
          victoryBannerCanvasGroup = victoryBannerRoot.GetComponent<CanvasGroup>();
          if (victoryBannerCanvasGroup == null)
          {
            victoryBannerCanvasGroup = victoryBannerRoot.gameObject.AddComponent<CanvasGroup>();
          }
        }

        // Tìm con Image (Banner)
        if (bannerImage == null)
        {
          Transform bTr = victoryBannerRoot.Find("Banner");
          if (bTr != null) bannerImage = bTr.GetComponent<Image>();
        }

        // Tìm con Text (TextMeshProUGUI)
        if (victoryText == null)
        {
          Transform tTr = victoryBannerRoot.Find("Text");
          if (tTr != null) victoryText = tTr.GetComponent<TextMeshProUGUI>();
        }

        // Đảm bảo Text luôn nằm TRÊN Banner (không bị Banner che khuất)
        if (victoryText != null && bannerImage != null)
        {
          victoryText.transform.SetAsLastSibling();
        }

        // Ẩn ban đầu khi vào game
        if (victoryBannerCanvasGroup != null)
        {
          victoryBannerCanvasGroup.alpha = 0f;
        }
        victoryBannerRoot.gameObject.SetActive(false);
      }
    }

    private void HandleBossDefeated()
    {
      if (hasTriggeredVictory || isVictorySequenceActive) return;
      StartCoroutine(VictorySequenceRoutine());
    }

    /// <summary>
    /// Coroutine điều phối toàn bộ chuỗi điện ảnh chiến thắng Souls-like
    /// </summary>
    private IEnumerator VictorySequenceRoutine()
    {
      isVictorySequenceActive = true;
      hasTriggeredVictory = true;

      Debug.Log("<color=gold>[BossVictoryDirector]</color> 🏆 KÍCH HOẠT ĐẠO DIỄN CHIẾN THẮNG BOSS (Task D02-5B)...");
      OnVictorySequenceStarted?.Invoke();

      // =========================================================================
      // PHA 1: HITLAG KẾT LIỄU (SOULS-LIKE LETHAL FREEZE)
      // =========================================================================
      if (hitlagDuration > 0f)
      {
        Time.timeScale = hitlagTimeScale;
        yield return new WaitForSecondsRealtime(hitlagDuration);
        Time.timeScale = 1f;
      }

      // =========================================================================
      // PHA 2: TẠM KHÓA PLAYER & CHUYỂN CAMERA TỚI GÓC 3/4 NGẮM BOSS
      // =========================================================================
      GameObject player = GameObject.FindGameObjectWithTag("Player");
      PlayerController playerCtrl = player != null ? player.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
      PlayerCombat playerCombat = player != null ? player.GetComponent<PlayerCombat>() : FindFirstObjectByType<PlayerCombat>();

      if (playerCtrl != null) playerCtrl.enabled = false;
      if (playerCombat != null) playerCombat.enabled = false;

      Camera mainCam = Camera.main;
      if (mainCam == null)
      {
        Debug.LogWarning("[BossVictoryDirector] Không tìm thấy Camera.main! Bỏ qua đạo diễn Camera.");
      }

      ThirdPersonCamera tpCam = mainCam != null ? mainCam.GetComponent<ThirdPersonCamera>() : null;
      Transform camTrans = mainCam != null ? mainCam.transform : null;

      Vector3 initialCamPos = camTrans != null ? camTrans.position : Vector3.zero;
      Quaternion initialCamRot = camTrans != null ? camTrans.rotation : Quaternion.identity;

      if (tpCam != null) tpCam.enabled = false;

      // Tìm vị trí Boss
      IceGolemController boss = FindFirstObjectByType<IceGolemController>();
      Vector3 bossPos = boss != null ? boss.transform.position : (player != null ? player.transform.position + Vector3.forward * 10f : Vector3.zero);
      Vector3 bossFwd = boss != null ? boss.transform.forward : Vector3.forward;
      Vector3 bossRight = boss != null ? boss.transform.right : Vector3.right;

      // Tính toán góc 3/4 mạn phải trên đầu Boss
      Vector3 flankDir = Quaternion.Euler(0f, bossCamAngleOffset, 0f) * bossFwd;
      Vector3 targetCamPos = bossPos + flankDir * bossCamDistance + Vector3.up * bossCamHeight;
      Vector3 lookTarget = bossPos + Vector3.up * 1.5f;
      Quaternion targetCamRot = Quaternion.LookRotation((lookTarget - targetCamPos).normalized);

      // Lia camera mượt mà tới góc 3/4
      if (camTrans != null)
      {
        float panTimer = 0f;
        while (panTimer < panToBossDuration)
        {
          panTimer += Time.deltaTime;
          float t = Mathf.Clamp01(panTimer / panToBossDuration);
          float smoothT = Mathf.SmoothStep(0f, 1f, t);

          camTrans.position = Vector3.Lerp(initialCamPos, targetCamPos, smoothT);
          camTrans.rotation = Quaternion.Slerp(initialCamRot, targetCamRot, smoothT);
          yield return null;
        }

        camTrans.position = targetCamPos;
        camTrans.rotation = targetCamRot;
      }

      // Trôi chậm (Cinematic Drift) trong lúc Boss ngã xuống và chờ biểu ngữ
      float driftTimer = 0f;
      while (driftTimer < delayBeforeBanner)
      {
        driftTimer += Time.deltaTime;
        if (camTrans != null)
        {
          camTrans.position += bossRight * (cinematicDriftSpeed * Time.deltaTime);
          camTrans.rotation = Quaternion.LookRotation((lookTarget - camTrans.position).normalized);
        }
        yield return null;
      }

      // =========================================================================
      // PHA 3: HIỂN THỊ BIỂU NGỮ "VICTORY ACHIEVED" HOÀNG KIM
      // =========================================================================
      EnsureUIReferences();

      if (victoryBannerRoot != null && victoryBannerCanvasGroup != null)
      {
        victoryBannerRoot.gameObject.SetActive(true);
        if (victoryText != null) victoryText.transform.SetAsLastSibling();

        // Âm thanh chiến thắng
        if (victorySound != null)
        {
          AudioSource.PlayClipAtPoint(victorySound, mainCam != null ? mainCam.transform.position : transform.position);
        }

        OnVictoryBannerShown?.Invoke();

        // Diễn hoạt Fade In & Phóng nhẹ (Scale 0.88 -> 1.0)
        Vector3 baseScale = Vector3.one;
        Vector3 startScale = baseScale * bannerStartScale;
        float inTimer = 0f;

        while (inTimer < bannerFadeInDuration)
        {
          inTimer += Time.deltaTime;
          float t = Mathf.Clamp01(inTimer / bannerFadeInDuration);
          float smoothT = Mathf.SmoothStep(0f, 1f, t);

          victoryBannerCanvasGroup.alpha = smoothT;
          victoryBannerRoot.localScale = Vector3.Lerp(startScale, baseScale, smoothT);

          // Camera tiếp tục trôi chậm
          if (camTrans != null)
          {
            camTrans.position += bossRight * (cinematicDriftSpeed * Time.deltaTime * 0.5f);
            camTrans.rotation = Quaternion.LookRotation((lookTarget - camTrans.position).normalized);
          }
          yield return null;
        }

        victoryBannerCanvasGroup.alpha = 1f;
        victoryBannerRoot.localScale = baseScale;

        // Giữ sáng rực rỡ giữa màn hình
        float holdTimer = 0f;
        while (holdTimer < bannerHoldDuration)
        {
          holdTimer += Time.deltaTime;
          // Hiệu ứng thở nhẹ (Subtle Breathing Scale 1.0 -> 1.02)
          float breathT = Mathf.Sin((holdTimer / bannerHoldDuration) * Mathf.PI);
          victoryBannerRoot.localScale = Vector3.Lerp(baseScale, baseScale * 1.025f, breathT);

          if (camTrans != null)
          {
            camTrans.position += bossRight * (cinematicDriftSpeed * Time.deltaTime * 0.4f);
            camTrans.rotation = Quaternion.LookRotation((lookTarget - camTrans.position).normalized);
          }
          yield return null;
        }

        // Diễn hoạt Fade Out (1.0 -> 0)
        float outTimer = 0f;
        while (outTimer < bannerFadeOutDuration)
        {
          outTimer += Time.deltaTime;
          float t = Mathf.Clamp01(outTimer / bannerFadeOutDuration);
          float smoothT = Mathf.SmoothStep(0f, 1f, t);

          victoryBannerCanvasGroup.alpha = 1f - smoothT;

          if (camTrans != null)
          {
            camTrans.position += bossRight * (cinematicDriftSpeed * Time.deltaTime * 0.3f);
            camTrans.rotation = Quaternion.LookRotation((lookTarget - camTrans.position).normalized);
          }
          yield return null;
        }

        victoryBannerCanvasGroup.alpha = 0f;
        victoryBannerRoot.gameObject.SetActive(false);
      }

      OnVictoryBannerComplete?.Invoke();
      Debug.Log("<color=gold>[BossVictoryDirector]</color> 🏆 Biểu ngữ VICTORY ACHIEVED hoàn tất!");

      // =========================================================================
      // PHA 4: LIA CAMERA LÊN BỤC ĐÁ 12M & SINH RƯƠNG BOSS + CỔNG DỊCH CHUYỂN (TASK D02-5C)
      // =========================================================================
      if (enablePedestalReveal)
      {
        OnPedestalRevealStarted?.Invoke();
        Debug.Log("<color=gold>[BossVictoryDirector]</color> 🏛️ Bắt đầu lia Camera lên Bục Đá 12m (Task D02-5C)...");

        GameObject pedestalObj = FindBossPedestal();
        if (pedestalObj != null)
        {
          Transform chestSocket = FindSocketRecursive(pedestalObj.transform, "[BossChest_Socket]");
          Transform portalSocket = FindSocketRecursive(pedestalObj.transform, "[Portal_Socket]");

          Vector3 chestPos = chestSocket != null 
            ? chestSocket.position 
            : pedestalObj.transform.TransformPoint(new Vector3(0f, 12.05f, 3.95f));
          Vector3 portalPos = portalSocket != null 
            ? portalSocket.position 
            : pedestalObj.transform.TransformPoint(new Vector3(0f, 12.05f, 9.0f));

          Quaternion chestRot = pedestalObj.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
          Quaternion portalRot = pedestalObj.transform.rotation * Quaternion.Euler(0f, 180f, 0f);

          Vector3 platformCenter = (chestPos + portalPos) * 0.5f;

          // Tính tọa độ Camera ngắm Bục Đá
          Vector3 targetPedestalCamPos = platformCenter + pedestalObj.transform.TransformDirection(pedestalCamLocalOffset);
          Vector3 pedestalLookTarget = platformCenter + pedestalObj.transform.TransformDirection(pedestalLookLocalOffset);
          Quaternion targetPedestalCamRot = Quaternion.LookRotation((pedestalLookTarget - targetPedestalCamPos).normalized);

          // 1. Lia camera mượt mà từ góc Boss tới góc ngắm Bục Đá
          if (camTrans != null)
          {
            Vector3 startCamPos = camTrans.position;
            Quaternion startCamRot = camTrans.rotation;

            float panPedTimer = 0f;
            while (panPedTimer < panToPedestalDuration)
            {
              panPedTimer += Time.deltaTime;
              float t = Mathf.Clamp01(panPedTimer / panToPedestalDuration);
              float smoothT = Mathf.SmoothStep(0f, 1f, t);

              camTrans.position = Vector3.Lerp(startCamPos, targetPedestalCamPos, smoothT);
              camTrans.rotation = Quaternion.Slerp(startCamRot, targetPedestalCamRot, smoothT);
              yield return null;
            }

            camTrans.position = targetPedestalCamPos;
            camTrans.rotation = targetPedestalCamRot;
          }

          // 2. KHOẢNH KHẮC GIÁNG THẾ: SINH RƯƠNG BOSS VÀ CỔNG DỊCH CHUYỂN
          EnsurePedestalPrefabs();

          if (bossChestPrefab != null && spawnedBossChestInstance == null)
          {
            spawnedBossChestInstance = Instantiate(bossChestPrefab, chestPos, chestRot, pedestalObj.transform);
            spawnedBossChestInstance.name = "Interactive_Chest_Boss";
          }

          if (victoryPortalPrefab != null && spawnedVictoryPortalInstance == null)
          {
            spawnedVictoryPortalInstance = Instantiate(victoryPortalPrefab, portalPos, portalRot, pedestalObj.transform);
            spawnedVictoryPortalInstance.name = "VictoryPortal";
          }

          // Hiệu ứng ánh sáng thần thánh bừng nở trên bục đá
          StartCoroutine(PedestalSpawnFlareRoutine(platformCenter + Vector3.up * 1.5f));

          // Âm thanh thần thánh giáng thế
          if (pedestalSpawnSound != null)
          {
            AudioSource.PlayClipAtPoint(pedestalSpawnSound, mainCam != null ? mainCam.transform.position : chestPos);
          }

          OnPedestalEntitiesSpawned?.Invoke();
          Debug.Log("<color=gold>[BossVictoryDirector]</color> 👑 Rương Boss & Cổng Dịch Chuyển đã bừng sáng giáng thế trên Bục Đá 12m!");

          // 3. Giữ camera ngắm bục với chuyển động trôi chậm nghệ thuật (Cinematic Drift)
          float pedHoldTimer = 0f;
          while (pedHoldTimer < pedestalHoldDuration)
          {
            pedHoldTimer += Time.deltaTime;
            if (camTrans != null)
            {
              camTrans.position += pedestalObj.transform.right * (cinematicDriftSpeed * Time.deltaTime * 0.45f);
              camTrans.rotation = Quaternion.LookRotation((pedestalLookTarget - camTrans.position).normalized);
            }
            yield return null;
          }
        }
        else
        {
          Debug.LogWarning("[BossVictoryDirector] Không tìm thấy Bục Đá BossArena_Pedestal! Bỏ qua đạo diễn Bục Đá.");
        }
      }

      // =========================================================================
      // PHA 5: KHÔI PHỤC CAMERA VỀ PLAYER & TRẢ LẠI QUYỀN ĐIỀU KHIỂN
      // =========================================================================
      if (autoReturnToPlayer && camTrans != null)
      {
        Vector3 curCamPos = camTrans.position;
        Quaternion curCamRot = camTrans.rotation;

        float retTimer = 0f;
        while (retTimer < returnToPlayerDuration)
        {
          retTimer += Time.deltaTime;
          float t = Mathf.Clamp01(retTimer / returnToPlayerDuration);
          float smoothT = Mathf.SmoothStep(0f, 1f, t);

          Vector3 targetPlayerCamPos = initialCamPos;
          Quaternion targetPlayerCamRot = initialCamRot;

          if (player != null)
          {
            Vector3 playerTarget = player.transform.position + Vector3.up * 1.5f;
            targetPlayerCamPos = playerTarget - player.transform.forward * 4.5f + Vector3.up * 0.8f + player.transform.right * 0.85f;
            targetPlayerCamRot = Quaternion.LookRotation((playerTarget - targetPlayerCamPos).normalized);
          }

          camTrans.position = Vector3.Lerp(curCamPos, targetPlayerCamPos, smoothT);
          camTrans.rotation = Quaternion.Slerp(curCamRot, targetPlayerCamRot, smoothT);
          yield return null;
        }

        // Mở lại toàn bộ điều khiển
        if (tpCam != null) tpCam.enabled = true;
        if (playerCtrl != null) playerCtrl.enabled = true;
        if (playerCombat != null) playerCombat.enabled = true;
      }

      OnVictorySequenceComplete?.Invoke();
      isVictorySequenceActive = false;
      Debug.Log("<color=gold>[BossVictoryDirector]</color> 🏆 Chuỗi chiến thắng hoàn tất 100%! Player sẵn sàng tiến lên bục 12m nhận thưởng.");
    }

    private IEnumerator PedestalSpawnFlareRoutine(Vector3 center)
    {
      if (spawnBurstVfxPrefab != null)
      {
        Instantiate(spawnBurstVfxPrefab, center, Quaternion.identity);
      }

      // Tạo một điểm sáng Point Light thần thánh bùng nổ rồi mờ dần
      GameObject flareObj = new GameObject("PedestalVictoryLightFlare");
      flareObj.transform.position = center;
      Light flareLight = flareObj.AddComponent<Light>();
      flareLight.type = LightType.Point;
      flareLight.range = 24f;
      flareLight.color = new Color(1.0f, 0.84f, 0.28f); // Golden amber
      flareLight.intensity = 8.5f;

      float elapsed = 0f;
      float flareDuration = 1.8f;
      while (elapsed < flareDuration)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flareDuration);
        flareLight.intensity = Mathf.Lerp(8.5f, 0f, t * t);
        yield return null;
      }

      Destroy(flareObj);
    }

    /// <summary>
    /// Tìm Bục Đá BossArena_Pedestal trên toàn scene
    /// </summary>
    public GameObject FindBossPedestal()
    {
      GameObject ped = GameObject.Find("BossArena_Pedestal");
      if (ped != null) return ped;

      ped = GameObject.Find("BossPedestal");
      if (ped != null) return ped;

      GameObject[] allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
      foreach (var root in allRoots)
      {
        Transform t = FindSocketRecursive(root.transform, "BossArena_Pedestal");
        if (t != null) return t.gameObject;
        t = FindSocketRecursive(root.transform, "BossPedestal");
        if (t != null) return t.gameObject;
      }
      return null;
    }

    /// <summary>
    /// Tìm socket hoặc GameObject con đệ quy theo tên
    /// </summary>
    public static Transform FindSocketRecursive(Transform parent, string socketName)
    {
      if (parent.name.Contains(socketName)) return parent;
      for (int i = 0; i < parent.childCount; i++)
      {
        Transform res = FindSocketRecursive(parent.GetChild(i), socketName);
        if (res != null) return res;
      }
      return null;
    }

    [ContextMenu("Test Victory Sequence (Play Mode)")]
    public void TestVictorySequence()
    {
      if (Application.isPlaying)
      {
        hasTriggeredVictory = false;
        isVictorySequenceActive = false;
        StartCoroutine(VictorySequenceRoutine());
      }
      else
      {
        Debug.LogWarning("[BossVictoryDirector] Chỉ có thể Test trong Play Mode!");
      }
    }

    [ContextMenu("Reset Victory Sequence")]
    public void ResetVictorySequence()
    {
      StopAllCoroutines();
      isVictorySequenceActive = false;
      hasTriggeredVictory = false;
      Time.timeScale = 1f;

      if (victoryBannerCanvasGroup != null)
      {
        victoryBannerCanvasGroup.alpha = 0f;
      }
      if (victoryBannerRoot != null)
      {
        victoryBannerRoot.gameObject.SetActive(false);
      }

      if (spawnedBossChestInstance != null)
      {
        Destroy(spawnedBossChestInstance);
        spawnedBossChestInstance = null;
      }
      if (spawnedVictoryPortalInstance != null)
      {
        Destroy(spawnedVictoryPortalInstance);
        spawnedVictoryPortalInstance = null;
      }

      Camera mainCam = Camera.main;
      if (mainCam != null)
      {
        ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
        if (tpCam != null) tpCam.enabled = true;
      }

      GameObject player = GameObject.FindGameObjectWithTag("Player");
      if (player != null)
      {
        PlayerController pc = player.GetComponent<PlayerController>();
        PlayerCombat pcb = player.GetComponent<PlayerCombat>();
        if (pc != null) pc.enabled = true;
        if (pcb != null) pcb.enabled = true;
      }
    }
  }
}
