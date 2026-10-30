using System.Collections;
using UnityEngine;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Đạo diễn Đấu Trường Boss (Arena Director) & Cutscene Mở Màn Điện Ảnh (Task D02-4):
  /// - Camera luôn chuyển động mượt mà (Cinematic Drift), không đứng yên.
  /// - Zoom gần Boss hơn tạo cảm giác áp đảo hoành tráng.
  /// - Khi Boss tiếp đất chuẩn bị phun băng: Offset Camera sang phải để đưa Boss sang bên TRÁI khung hình,
  ///   đồng thời Banner Tên xuất hiện ở bên PHẢI màn hình (không trùng màu/không che tia phun băng).
  /// - Toàn bộ thông số đưa ra Inspector để dễ dàng test & tinh chỉnh theo ý muốn.
  /// </summary>
  public class BossIntroDirector : MonoBehaviour
  {
    public static BossIntroDirector Instance { get; private set; }

    [Header("=== TRẠNG THÁI CUTSCENE ===")]
    [SerializeField] private bool isPlayingCutscene = false;
    [SerializeField] private bool hasPlayedIntro = false;

    [Header("=== PHA 1: GIẬT TỚI GIỮA PHÒNG & ZOOM TIẾN CHẬM (SLOW DRIFT) ===")]
    [Tooltip("Khoảng cách từ Camera giữa phòng tới điểm tiếp đất của Boss (nhỏ hơn = Zoom gần Boss hơn)")]
    [SerializeField] private float midCamDistance = 11.5f;
    [Tooltip("Độ cao Camera ở giữa phòng")]
    [SerializeField] private float midCamHeight = 2.4f;
    [Tooltip("Thời gian dừng/trôi ở giữa phòng (giây)")]
    [SerializeField] private float midRoomHoldDuration = 0.8f;
    [Tooltip("Tốc độ Camera trôi chậm (Slow Push-in) tiến về phía Boss ở giữa phòng (m/s)")]
    [SerializeField] private float midRoomDriftSpeed = 0.65f;

    [Header("=== PHA 2: LIA GÓC 3/4 LÊN BỤC ĐÁ & ZOOM GẦN ===")]
    [Tooltip("Thời gian camera lia theo đường cong sang mạn phải lên bục (giây)")]
    [SerializeField] private float panToRightDuration = 2.2f;
    [Tooltip("Khoảng cách dạt sang mạn phải (nhỏ hơn = Zoom gần Boss trên bục hơn)")]
    [SerializeField] private float flankDistance = 13.0f;
    [Tooltip("Độ cao Camera khi ở góc 3/4 nhìn lên bục")]
    [SerializeField] private float flankHeight = 5.2f;
    [Tooltip("Thời gian camera ngắm Boss trên bục chuẩn bị nhảy (giây)")]
    [SerializeField] private float bossJumpAnticipation = 0.5f;
    [Tooltip("Tốc độ Camera tiếp tục zoom/trôi chậm lại gần Boss trên bục trong lúc chờ nhảy (m/s)")]
    [SerializeField] private float anticipationDriftSpeed = 0.6f;

    [Header("=== PHA 3: BOSS NHẢY XUỐNG & RUNG MÀN HÌNH ===")]
    [Tooltip("Thời gian camera lia hạ góc chúc theo nhịp rơi của Boss (giây)")]
    [SerializeField] private float jumpCamFollowDuration = 1.25f;
    [Tooltip("Cường độ rung màn hình khi Boss tiếp đất")]
    [SerializeField] private float landingShakeIntensity = 0.45f;
    [Tooltip("Thời gian rung màn hình khi tiếp đất")]
    [SerializeField] private float landingShakeDuration = 0.55f;

    [Header("=== PHA 4: BỐ CỤC 1/3 (BOSS BÊN TRÁI - BANNER BÊN PHẢI) ===")]
    [Tooltip("Thời gian chuyển camera mượt mà sang vị trí lệch phải trước khi Boss phun băng (giây)")]
    [SerializeField] private float breathFramingTransitionDuration = 0.45f;
    [Tooltip("Độ lệch Camera sang phải (mét) để tạo góc nhìn nghiêng từ mạn phải")]
    [SerializeField] private float breathCamOffsetRight = 4.5f;
    [Tooltip("Độ lệch tâm ngắm sang phải (mét) để đưa Boss vào 1/3 bên TRÁI khung hình (Rule of Thirds), nhường khoảng trống bên PHẢI cho Banner Tên")]
    [SerializeField] private float breathLookOffsetRight = 2.6f;
    [Tooltip("Cự ly từ Camera tới Boss khi Boss phun băng (nhỏ hơn = Zoom cận cảnh Boss)")]
    [SerializeField] private float breathCamDistance = 8.5f;
    [Tooltip("Độ cao Camera khi Boss phun băng")]
    [SerializeField] private float breathCamHeight = 2.2f;
    [Tooltip("Tốc độ Camera trôi chậm (Slow Pan/Drift) trong suốt lúc Boss phun băng (m/s)")]
    [SerializeField] private float breathDriftSpeed = 0.4f;
    [Tooltip("Thời gian giữ khung cảnh phun băng và hiện Banner (giây)")]
    [SerializeField] private float breathHoldDuration = 2.8f;

    [Header("=== PHA 5: KÉO CAMERA VỀ LẠI PLAYER ===")]
    [Tooltip("Thời gian kéo camera mượt mà từ đấu trường quay về sau vai Player (giây)")]
    [SerializeField] private float returnToPlayerCamDuration = 1.6f;

    public bool IsPlayingCutscene => isPlayingCutscene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureDirectorInstance()
    {
      if (FindFirstObjectByType<BossIntroDirector>() == null)
      {
        GameObject obj = new GameObject("BossIntroDirector");
        obj.AddComponent<BossIntroDirector>();
        Debug.Log("<color=cyan>[BossIntroDirector]</color> 🎬 Tự động khởi tạo BossIntroDirector tại runtime.");
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
    }

    private void OnEnable()
    {
      BossArenaTrigger.OnPlayerEnteredArena -= HandlePlayerEnteredArena;
      BossArenaTrigger.OnPlayerEnteredArena += HandlePlayerEnteredArena;
    }

    private void OnDisable()
    {
      BossArenaTrigger.OnPlayerEnteredArena -= HandlePlayerEnteredArena;
    }

    private void HandlePlayerEnteredArena()
    {
      if (hasPlayedIntro || isPlayingCutscene) return;
      StartCoroutine(IntroCinematicRoutine());
    }

    private IEnumerator IntroCinematicRoutine()
    {
      isPlayingCutscene = true;
      hasPlayedIntro = true;

      Debug.Log("<color=cyan>[BossIntroDirector]</color> 🎬 KÍCH HOẠT CUTSCENE ĐẠO DIỄN BOSS ICE GOLEM (Task D02-4)...");

      // 1. TÌM VÀ KHÓA ĐIỀU KHIỂN PLAYER
      GameObject player = GameObject.FindGameObjectWithTag("Player");
      PlayerController playerCtrl = player != null ? player.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
      PlayerCombat playerCombat = player != null ? player.GetComponent<PlayerCombat>() : FindFirstObjectByType<PlayerCombat>();

      if (playerCtrl != null) playerCtrl.enabled = false;
      if (playerCombat != null) playerCombat.enabled = false;

      // 2. TÌM BOSS VÀ LIÊN KẾT MÁU VỚI HUD
      IceGolemController boss = FindFirstObjectByType<IceGolemController>();
      if (boss == null)
      {
        Debug.LogWarning("[BossIntroDirector] Chưa tìm thấy IceGolemController trong Scene! Đang chờ 0.5s...");
        yield return new WaitForSeconds(0.5f);
        boss = FindFirstObjectByType<IceGolemController>();
      }

      if (boss != null)
      {
        Health bossHealth = boss.GetComponent<Health>();
        if (bossHealth != null && BossHUDController.Instance != null)
        {
          BossHUDController.Instance.BindBossHealth(bossHealth);
        }
      }

      // 3. TẠM NGẮT THIRDPERSONCAMERA ĐỂ ĐẠO DIỄN CAMERA THỜI GIAN THỰC
      Camera mainCam = Camera.main;
      if (mainCam == null)
      {
        Debug.LogError("[BossIntroDirector] Không tìm thấy Camera.main!");
        yield break;
      }

      ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
      Transform camTrans = mainCam.transform;

      Vector3 initialPlayerCamPos = camTrans.position;
      Quaternion initialPlayerCamRot = camTrans.rotation;

      if (tpCam != null) tpCam.enabled = false;

      // Xác định các điểm neo không gian trong phòng Boss
      Vector3 bossStartPos = boss != null ? boss.transform.position : (player != null ? player.transform.position + Vector3.forward * 30f : Vector3.zero);

      // Hướng từ Boss nhìn vào trong sàn đấu trường (luôn hướng về phía Player, tuyệt đối không đâm vào tường Bắc)
      Vector3 dirToArena = Vector3.back;
      if (player != null)
      {
        Vector3 toPlayer = player.transform.position - bossStartPos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 1.0f) dirToArena = toPlayer.normalized;
      }

      // Ép Boss luôn quay mặt chuẩn xác về phía tâm đấu trường (hướng về phía Player)
      if (boss != null)
      {
        boss.transform.rotation = Quaternion.LookRotation(dirToArena);
      }

      // Điểm tiếp đất của Boss: Nằm ở mặt sàn (Y = 0) về phía tâm phòng (cách bục 15m về phía Player)
      Vector3 landingPos = bossStartPos + dirToArena * 15f;
      landingPos.y = 0f;

      // Vector chuẩn hóa từ điểm tiếp đất nhìn ngược lên Bục Đá
      Vector3 toPedestal = -dirToArena;
      Vector3 rightDir = Vector3.Cross(Vector3.up, toPedestal).normalized;

      // =========================================================================
      // PHA 1: CAMERA GIẬT TỚI GIỮA PHÒNG & CHUYỂN ĐỘNG TRÔI TIẾN CHẬM (SLOW DRIFT)
      // =========================================================================
      Vector3 midCamPos = landingPos - toPedestal * midCamDistance + Vector3.up * midCamHeight;
      Vector3 lookAtPedestal = bossStartPos + Vector3.up * 3.5f;
      Quaternion midCamRot = Quaternion.LookRotation((lookAtPedestal - midCamPos).normalized);

      camTrans.position = midCamPos;
      camTrans.rotation = midCamRot;

      if (BossHUDController.Instance != null)
      {
        BossHUDController.Instance.ShowHealthBarOnly();
      }

      // Camera luôn trôi nhẹ tiến lại gần bục trong suốt thời gian dừng
      float midTimer = 0f;
      while (midTimer < midRoomHoldDuration)
      {
        midTimer += Time.deltaTime;
        camTrans.position += toPedestal * (midRoomDriftSpeed * Time.deltaTime);
        camTrans.rotation = Quaternion.LookRotation(((bossStartPos + Vector3.up * 3.5f) - camTrans.position).normalized);
        yield return null;
      }

      // =========================================================================
      // PHA 2: LIA CAMERA THEO ĐƯỜNG CONG SANG MẠN PHẢI (GÓC 3/4) & TIẾP TỤC ZOOM GẦN
      // =========================================================================
      Vector3 startCurvePos = camTrans.position;
      Vector3 target34Pos = landingPos - toPedestal * 4.5f + rightDir * flankDistance + Vector3.up * flankHeight;

      float panTimer = 0f;
      while (panTimer < panToRightDuration)
      {
        panTimer += Time.deltaTime;
        float t = Mathf.Clamp01(panTimer / panToRightDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        // Đường cong uốn nhẹ ra phía ngoài bằng sin(t * pi)
        Vector3 curveOffset = rightDir * Mathf.Sin(t * Mathf.PI) * 3.5f;
        camTrans.position = Vector3.Lerp(startCurvePos, target34Pos, smoothT) + curveOffset;

        Vector3 currentLookTarget = Vector3.Lerp(lookAtPedestal, bossStartPos + Vector3.up * 2.8f, smoothT);
        camTrans.rotation = Quaternion.LookRotation((currentLookTarget - camTrans.position).normalized);

        yield return null;
      }

      camTrans.position = target34Pos;

      // Trong lúc Boss chuẩn bị nhảy: Camera tiếp tục zoom/trôi chậm lại gần Boss trên bục
      float antTimer = 0f;
      while (antTimer < bossJumpAnticipation)
      {
        antTimer += Time.deltaTime;
        camTrans.position += (bossStartPos + Vector3.up * 2.8f - camTrans.position).normalized * (anticipationDriftSpeed * Time.deltaTime);
        camTrans.rotation = Quaternion.LookRotation(((bossStartPos + Vector3.up * 2.8f) - camTrans.position).normalized);
        yield return null;
      }

      // =========================================================================
      // PHA 3: BOSS LAO XUỐNG ĐẤT (JUMPDOWN) - CAM LIA HẠ THEO
      // =========================================================================
      bool hasLanded = false;
      if (boss != null)
      {
        boss.PlayIntroJumpDownFromPedestal(landingPos, onLanded: () =>
        {
          hasLanded = true;
        });
      }

      float jumpTimer = 0f;
      while (jumpTimer < jumpCamFollowDuration && !hasLanded)
      {
        jumpTimer += Time.deltaTime;
        if (boss != null)
        {
          // Camera hạ góc chúc bám theo thân hình Boss đang rơi xuống
          Vector3 bossCurrentCenter = boss.transform.position + Vector3.up * 2.5f;
          camTrans.rotation = Quaternion.Slerp(camTrans.rotation, Quaternion.LookRotation((bossCurrentCenter - camTrans.position).normalized), Time.deltaTime * 12f);
        }
        yield return null;
      }

      // CHẤN ĐỘNG TIẾP ĐẤT: RUNG MÀN HÌNH CỰC MẠNH (CAMERA SHAKE)
      yield return StartCoroutine(CameraShakeRoutine(camTrans, landingShakeDuration, landingShakeIntensity));

      // =========================================================================
      // PHA 4: OFFSET CAMERA SANG PHẢI (BOSS BÊN TRÁI - BANNER BÊN PHẢI) & PHUN BĂNG
      // =========================================================================
      Vector3 preBreathCamPos = camTrans.position;
      Vector3 bossChestTarget = landingPos + Vector3.up * breathCamHeight;
      Vector3 framingLookTarget = bossChestTarget + rightDir * breathLookOffsetRight;

      // Vị trí Camera lệch sang phải breathCamOffsetRight và ngắm lệch sang phải -> Đưa Boss sang 1/3 BÊN TRÁI màn hình
      Vector3 targetBreathCamPos = landingPos - toPedestal * breathCamDistance + rightDir * breathCamOffsetRight + Vector3.up * breathCamHeight;
      Quaternion targetBreathCamRot = Quaternion.LookRotation((framingLookTarget - targetBreathCamPos).normalized);

      // Chuyển mượt mà sang vị trí lệch phải trước khi Boss phun băng
      float shiftTimer = 0f;
      while (shiftTimer < breathFramingTransitionDuration)
      {
        shiftTimer += Time.deltaTime;
        float t = Mathf.Clamp01(shiftTimer / breathFramingTransitionDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        camTrans.position = Vector3.Lerp(preBreathCamPos, targetBreathCamPos, smoothT);
        camTrans.rotation = Quaternion.Slerp(camTrans.rotation, targetBreathCamRot, smoothT);
        yield return null;
      }

      camTrans.position = targetBreathCamPos;
      camTrans.rotation = targetBreathCamRot;

      // Boss phun băng và Banner Tên xuất hiện ở bên PHẢI màn hình
      if (boss != null)
      {
        boss.PlayIntroBreath();
      }

      if (BossHUDController.Instance != null)
      {
        BossHUDController.Instance.PlayNameSequence();
      }

      // Trong suốt lúc Boss phun băng: Camera tiếp tục chuyển động trôi nhẹ (Cinematic Drift)
      float breathTimer = 0f;
      while (breathTimer < breathHoldDuration)
      {
        breathTimer += Time.deltaTime;
        // Trôi nhẹ sang phải và tiến nhẹ lại gần Boss
        camTrans.position += rightDir * (breathDriftSpeed * Time.deltaTime * 0.4f) + toPedestal * (breathDriftSpeed * Time.deltaTime * 0.6f);
        camTrans.rotation = Quaternion.LookRotation(((bossChestTarget + rightDir * breathLookOffsetRight) - camTrans.position).normalized);
        yield return null;
      }

      // =========================================================================
      // PHA 5: KÉO CAMERA TRƯỢT MƯỢT VỀ LẠI SAU VAI PLAYER & BẮT ĐẦU CHIẾN ĐẤU
      // =========================================================================
      Vector3 endArenaCamPos = camTrans.position;
      Quaternion endArenaCamRot = camTrans.rotation;

      float returnTimer = 0f;
      while (returnTimer < returnToPlayerCamDuration)
      {
        returnTimer += Time.deltaTime;
        float t = Mathf.Clamp01(returnTimer / returnToPlayerCamDuration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        Vector3 targetPlayerCamPos = initialPlayerCamPos;
        Quaternion targetPlayerCamRot = initialPlayerCamRot;

        if (player != null)
        {
          Vector3 playerLookTarget = player.transform.position + Vector3.up * 1.5f;
          targetPlayerCamPos = playerLookTarget - player.transform.forward * 4.5f + Vector3.up * 0.8f + player.transform.right * 0.85f;
          targetPlayerCamRot = Quaternion.LookRotation((playerLookTarget - targetPlayerCamPos).normalized);
        }

        camTrans.position = Vector3.Lerp(endArenaCamPos, targetPlayerCamPos, smoothT);
        camTrans.rotation = Quaternion.Slerp(endArenaCamRot, targetPlayerCamRot, smoothT);

        yield return null;
      }

      // 6. KHÔI PHỤC TOÀN BỘ ĐIỀU KHIỂN & KÍCH HOẠT COMBAT
      if (tpCam != null) tpCam.enabled = true;
      if (playerCtrl != null) playerCtrl.enabled = true;
      if (playerCombat != null) playerCombat.enabled = true;

      if (boss != null)
      {
        boss.SetStandbyOnPedestal(false);
      }

      isPlayingCutscene = false;
      Debug.Log("<color=green>[BossIntroDirector]</color> ⚔️ Cutscene kết thúc thành công! TRẬN ĐẤU BOSS BẮT ĐẦU!");
    }

    private IEnumerator CameraShakeRoutine(Transform camTrans, float duration, float intensity)
    {
      Vector3 originalPos = camTrans.position;
      float elapsed = 0f;

      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float decay = 1f - Mathf.Clamp01(elapsed / duration);

        float shakeX = Random.Range(-1f, 1f) * intensity * decay;
        float shakeY = Random.Range(-1f, 1f) * intensity * decay;
        float shakeZ = Random.Range(-1f, 1f) * intensity * decay * 0.5f;

        camTrans.position = originalPos + new Vector3(shakeX, shakeY, shakeZ);
        yield return null;
      }

      camTrans.position = originalPos;
    }
  }
}
