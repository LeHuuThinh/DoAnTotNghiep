using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private TargetLockSystem targetLockSystem;
  [SerializeField] private PlayerCombat playerCombat;
  [SerializeField] private ThirdPersonCamera thirdPersonCamera;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 35f;

  [Header("Parry Feedback Settings")]
  [Tooltip("VFX tia lửa tóe ra khi gạt đòn (Parry) thành công ngay tại điểm chạm trên lưỡi kiếm")]
  [SerializeField] private GameObject parryVfxPrefab;
  [Tooltip("Thời gian tự hủy instance Parry VFX (giây)")]
  [SerializeField] private float parryVfxDestroyDelay = 1.5f;
  [Tooltip("Tỉ lệ phóng to/thu nhỏ của Parry VFX")]
  [SerializeField] private float parryVfxScale = 1.0f;
  [Tooltip("Độ lệch vị trí dọc theo chiều dài lưỡi kiếm (m) tính từ giữa lưỡi kiếm. Dương = tiến về đầu mũi kiếm, Âm = lùi về chuôi kiếm.")]
  [SerializeField] private float parryBladeOffset = 0f;
  [Tooltip("Âm thanh keng kim loại khi gạt kiếm thành công (tùy chọn)")]
  [SerializeField] private AudioClip parrySfx;
  [Tooltip("Thời gian hitlag khựng hình khi Parry thành công (giây thực tế)")]
  [SerializeField] private float parryHitlagDuration = 0.2f;
  [Tooltip("Tốc độ thời gian khi khựng hitlag (0.05 = làm chậm 95%)")]
  [SerializeField] private float parryTimeScale = 0.05f;

  [Header("Hit VFX Feedback")]
  [Tooltip("Prefab VFX tứa ra tại điểm dính đòn (Mặc định: GetHitVFX màu đỏ cho người chơi)")]
  [SerializeField] private GameObject hitVfxPrefab;
  [Tooltip("Thời gian tự hủy instance VFX (giây) đề phòng nếu VFX không tự hủy")]
  [SerializeField] private float vfxDestroyDelay = 2.0f;

  [Header("Healing Feedback Settings")]
  [Tooltip("Prefab VFX hiệu ứng hồi máu (Healing.prefab trong Characters/Player/PlayerVFX hoặc Resources)")]
  [SerializeField] private GameObject healingVfxPrefab;
  [Tooltip("Thời gian hiển thị VFX hồi máu trước khi tắt hiệu ứng (giây)")]
  [SerializeField] private float healingVfxDuration = 1.0f;
  [Tooltip("Độ lệch vị trí VFX so với gốc tọa độ người chơi (Mặc định Y=0.9m ngang hông để effect bao trọn phần thân)")]
  [SerializeField] private Vector3 healingVfxOffset = new Vector3(0f, 0.9f, 0f);

  [Header("Stagger / Knockback")]
  [SerializeField] private float staggerDuration = 0.5f;
  [SerializeField] private float knockbackDuration = 0.15f;
  [Tooltip("Thời gian bất tử (I-frames) khi bị vỡ Poise / Stagger để chống bị quái hội đồng ép góc chết sốc")]
  [SerializeField] private float staggerIFrameDuration = 0.35f;

  public bool HasHyperArmor { get; set; } = false;
  public bool IsParrying { get; set; } = false;

  // Cổng kiểm tra xem người chơi có đang bị choáng/vỡ thế hay không
  public bool IsStaggered => staggerCoroutine != null;

  private Health health;
  private PlayerController playerController;
  private CharacterController characterController;

  private float currentPoise;
  private float lastHitTime;
  private Coroutine staggerCoroutine;
  private Coroutine knockbackCoroutine;
  private Coroutine hitStopCoroutine;
  public static bool IsAnyHitlagActive { get; private set; }

  private static readonly int GetHitHash = Animator.StringToHash("GetHit");
  private static readonly int DieHash = Animator.StringToHash("Die");

  private void Awake()
  {
    health = GetComponent<Health>();
    playerController = GetComponent<PlayerController>();
    characterController = GetComponent<CharacterController>();

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (targetLockSystem == null) targetLockSystem = GetComponent<TargetLockSystem>();

    // Tự động tìm kiếm linh hoạt ở cả Object cha lẫn Object con
    if (playerCombat == null)
    {
      playerCombat = GetComponent<PlayerCombat>();
      if (playerCombat == null) playerCombat = GetComponentInChildren<PlayerCombat>();
      if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
    }

    if (thirdPersonCamera == null)
    {
      if (playerController != null && playerController.ThirdPersonCamera != null)
      {
        thirdPersonCamera = playerController.ThirdPersonCamera;
      }
      else
      {
        thirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();
      }
    }

#if UNITY_EDITOR
    if (hitVfxPrefab == null)
    {
      hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/GetHitVFX.prefab");
    }
#endif

    currentPoise = maxPoise;
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(HandleTakeDamage);
    health.onDeath.AddListener(HandleDeath);
    if (health != null) health.onCheckParryOrBlock = TryParry;
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(HandleTakeDamage);
    health.onDeath.RemoveListener(HandleDeath);
    if (health != null && health.onCheckParryOrBlock == TryParry) health.onCheckParryOrBlock = null;

    if (staggerCoroutine != null)
    {
      StopCoroutine(staggerCoroutine);
      staggerCoroutine = null;
      if (health != null) health.SetInvulnerable(false);
    }

    if (hitStopCoroutine != null)
    {
      float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
      hitStopCoroutine = null;
      IsAnyHitlagActive = false;
    }
  }

  /// <summary>
  /// Can thiệp ngay TRƯỚC KHI Health bị trừ máu: Nếu người chơi đang trong thế Parry thì gạt đòn thành công!
  /// </summary>
  private bool TryParry(Transform attacker, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    if (IsParrying)
    {
      ExecuteParrySuccess(attacker, hitPoint, hitDirection);
      return true; // Gạt đòn thành công -> Bỏ qua 100% sát thương, KHÔNG TRỪ MÁU!
    }
    return false;
  }

  private void Update()
  {
    if (currentPoise < maxPoise && Time.time >= lastHitTime + poiseRecoveryDelay)
    {
      currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
    }
  }

  private void HandleTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    // Luôn sinh hiệu ứng tứa máu đỏ (GetHitVFX) tại điểm dính đòn (kể cả khi đòn này kết liễu người chơi)
    SpawnHitVfx(hitPoint, hitDirection);

    if (health.IsDead || health.CurrentHealth <= 0f) return;

    lastHitTime = Time.time;

    // 1. TÍNH POISE DAMAGE BÌNH THƯỜNG
    float poiseDamageTaken = knockbackDistance > 0.8f ? 70f : 35f;
    currentPoise -= poiseDamageTaken;

    if (HasHyperArmor) return;

    // 2. BỊ VỠ THẾ
    if (currentPoise <= 0f || knockbackDistance >= 1.2f)
    {
      currentPoise = maxPoise;
      animator.SetTrigger(GetHitHash);

      if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
      staggerCoroutine = StartCoroutine(StaggerRoutine());

      if (knockbackDistance > 0f)
      {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
      }
    }
  }

  /// <summary>
  /// Sinh hiệu ứng VFX tứa máu đỏ (GetHitVFX) tại điểm trúng đòn
  /// </summary>
  private void SpawnHitVfx(Vector3 hitPoint, Vector3 hitDirection)
  {
    if (hitVfxPrefab == null) return;

    // Failsafe nếu điểm trúng đòn không xác định
    Vector3 spawnPos = hitPoint;
    if (spawnPos == Vector3.zero)
    {
      spawnPos = transform.position + Vector3.up * 1.0f;
    }

    // Hướng tứa: Bắn ngược chiều cú đánh ra ngoài không khí
    Vector3 spawnDir = -hitDirection;
    if (spawnDir.sqrMagnitude < 0.001f)
    {
      spawnDir = -transform.forward;
    }
    spawnDir.Normalize();

    // Dịch nhẹ điểm spawn ra ngoài bề mặt 0.05m để không bị lẹm vào bên trong mesh
    spawnPos += spawnDir * 0.05f;

    Quaternion spawnRot = Quaternion.LookRotation(spawnDir);
    GameObject vfxInstance = Instantiate(hitVfxPrefab, spawnPos, spawnRot);
    if (vfxDestroyDelay > 0f)
    {
      Destroy(vfxInstance, vfxDestroyDelay);
    }
  }

  public void ExecuteParrySuccess(Transform incomingAttacker = null, Vector3 hitPoint = default, Vector3 hitDirection = default)
  {
    Debug.Log("<color=yellow>[PARRY THÀNH CÔNG!] Keng!</color>");

    // 1. Khựng hình hitlag 0.2s tạo độ đanh và cảm giác lực
    TriggerHitlag(parryHitlagDuration, parryTimeScale);

    // 2. Kích hoạt camera zoom tới và trả về mượt mà
    if (thirdPersonCamera != null)
    {
      thirdPersonCamera.TriggerParryZoom();
    }
    else
    {
      thirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();
      if (thirdPersonCamera != null) thirdPersonCamera.TriggerParryZoom();
    }

    // 3. XÁC ĐỊNH CHÍNH XÁC CON QUÁI THỰC SỰ VUNG KIẾM BỊ PARRY
    // Ưu tiên cao nhất: Lấy trực tiếp attacker từ đòn đánh vừa bị gạt phăng
    Transform parriedEnemy = incomingAttacker;

    // Ưu tiên 2: Lấy từ LastAttacker trên Health
    if (parriedEnemy == null && health != null && health.LastAttacker != null)
    {
      parriedEnemy = health.LastAttacker;
    }

    // Ưu tiên 2 (Failsafe): Quét các quái xung quanh NHƯNG BẮT BUỘC ĐANG TẤN CÔNG (IsAttacking == true)
    if (parriedEnemy == null)
    {
      Collider[] cols = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 3.0f);
      foreach (Collider c in cols)
      {
        EnemyAIBase ai = c.GetComponentInParent<EnemyAIBase>();
        if (ai != null && ai.IsAttacking)
        {
          parriedEnemy = ai.transform.root;
          break;
        }
      }
    }

    // Ưu tiên 3: Nếu vẫn chưa thấy, kiểm tra quái đang lock-on (nhưng cũng phải đang vung kiếm IsAttacking)
    if (parriedEnemy == null && targetLockSystem != null && targetLockSystem.IsLockedOn)
    {
      EnemyAIBase lockAI = targetLockSystem.CurrentTarget.GetComponentInParent<EnemyAIBase>();
      if (lockAI != null && lockAI.IsAttacking)
      {
        parriedEnemy = lockAI.transform.root;
      }
    }

    // Ép CHỈ CON QUÁI BỊ PARRY bị ngắt chiêu và vỡ thế hoàn toàn
    if (parriedEnemy != null)
    {
      EnemyAIBase enemyAI = parriedEnemy.GetComponentInChildren<EnemyAIBase>();
      if (enemyAI != null)
      {
        enemyAI.InterruptAttack();
      }

      Animator enemyAnim = parriedEnemy.GetComponentInChildren<Animator>();
      if (enemyAnim != null)
      {
        enemyAnim.SetInteger("HitType", 1);
        enemyAnim.SetTrigger("GetHit");
      }
    }

    // 4. KÍCH HOẠT PARRY VFX NGAY TẠI GIỮA LƯỠI KIẾM CỦA PLAYER
    SpawnParryVfx(parriedEnemy);

    // 5. Báo cho PlayerCombat mở cửa sổ Counter Attack
    if (playerCombat != null)
    {
      playerCombat.OnParrySuccess();
    }
    else
    {
      Debug.LogError("Chưa tìm thấy PlayerCombat trên Player!");
    }
  }

  /// <summary>
  /// Sinh hiệu ứng tia lửa Parry VFX ngay tại vị trí GIỮA LƯỠI KIẾM của Player khi gạt đòn thành công.
  /// </summary>
  public void SpawnParryVfx(Transform attacker = null)
  {
    if (parryVfxPrefab == null)
    {
      // Fallback: Nạp từ thư mục Resources
      parryVfxPrefab = Resources.Load<GameObject>("Parry VFX");
      if (parryVfxPrefab == null)
      {
        parryVfxPrefab = Resources.Load<GameObject>("Impact VFX");
      }
      if (parryVfxPrefab == null) return;
    }

    Vector3 spawnPos = Vector3.zero;

    // 1. Tìm thanh kiếm và Collider lưỡi kiếm của Player
    Collider swordCollider = null;
    Transform swordTransform = null;

    if (playerCombat != null && playerCombat.WeaponHitbox != null)
    {
      swordCollider = playerCombat.WeaponHitbox.GetComponent<Collider>();
      swordTransform = playerCombat.WeaponHitbox.transform;
    }
    else
    {
      WeaponHitbox hitbox = GetComponentInChildren<WeaponHitbox>(true);
      if (hitbox != null)
      {
        swordCollider = hitbox.GetComponent<Collider>();
        swordTransform = hitbox.transform;
      }
    }

    // 2. Định vị vị trí chuẩn xác: GIỮA LƯỠI KIẾM (Center of Sword Blade)
    if (swordCollider is BoxCollider box && swordTransform != null)
    {
      // Sử dụng trực tiếp tâm hình học của BoxCollider lưỡi kiếm (local center = 0.81m)
      spawnPos = swordTransform.TransformPoint(box.center);
      if (parryBladeOffset != 0f)
      {
        spawnPos += swordTransform.up * parryBladeOffset;
      }
    }
    else if (swordCollider != null)
    {
      spawnPos = swordCollider.bounds.center;
      if (swordTransform != null && parryBladeOffset != 0f)
      {
        spawnPos += swordTransform.up * parryBladeOffset;
      }
    }
    else if (swordTransform != null)
    {
      // Vị trí giữa thân kiếm nhô lên phía trên chuôi kiếm (local Y ~ 0.81m)
      spawnPos = swordTransform.position + swordTransform.up * (0.81f + parryBladeOffset);
    }
    else
    {
      // Failsafe tổng thể: Vị trí giơ kiếm đón đỡ ngang ngực/mắt phía trước người chơi
      spawnPos = transform.position + transform.forward * 0.65f + Vector3.up * 1.35f;
    }

    // 3. Tính toán góc quay của tia lửa tóe ra (bắn văng về phía trước đối thủ)
    Vector3 sparkDirection = Vector3.zero;
    if (attacker != null)
    {
      sparkDirection = (attacker.position - transform.position);
      sparkDirection.y = 0f;
    }
    else
    {
      sparkDirection = transform.forward;
    }

    if (sparkDirection.sqrMagnitude < 0.001f)
    {
      sparkDirection = transform.forward;
    }
    sparkDirection.Normalize();
    sparkDirection.y = 0.15f; // Chếch nhẹ lên trên tạo độ văng tóe sống động
    sparkDirection.Normalize();

    Quaternion spawnRot = Quaternion.LookRotation(sparkDirection);

    // 4. Sinh instance VFX và kích hoạt toàn bộ ParticleSystem con
    GameObject vfxInstance = Instantiate(parryVfxPrefab, spawnPos, spawnRot);
    if (Mathf.Abs(parryVfxScale - 1.0f) > 0.01f)
    {
      vfxInstance.transform.localScale = vfxInstance.transform.localScale * parryVfxScale;
    }

    ParticleSystem rootPs = vfxInstance.GetComponent<ParticleSystem>();
    if (rootPs != null)
    {
      rootPs.Play(true);
    }
    else
    {
      foreach (ParticleSystem ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
      {
        ps.Play(true);
      }
    }

    if (parrySfx != null)
    {
      AudioSource.PlayClipAtPoint(parrySfx, spawnPos);
    }

    if (parryVfxDestroyDelay > 0f)
    {
      Destroy(vfxInstance, parryVfxDestroyDelay);
    }
  }

  /// <summary>
  /// Overload tương thích ngược
  /// </summary>
  public void SpawnParryVfx(Vector3 hitPoint, Vector3 hitDirection, Transform attacker = null)
  {
    SpawnParryVfx(attacker);
  }

  [ContextMenu("🛡️ Test Parry VFX")]
  public void TestParryVfx()
  {
    SpawnParryVfx(null);
  }

  public void TriggerHitlag(float duration, float scale = 0.05f)
  {
    if (hitStopCoroutine != null)
    {
      StopCoroutine(hitStopCoroutine);
      float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
      IsAnyHitlagActive = false;
    }
    hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, scale));
  }

  private IEnumerator HitStopRoutine(float duration, float scale)
  {
    IsAnyHitlagActive = true;
    Time.timeScale = scale;
    Time.fixedDeltaTime = 0.02f * scale;
    yield return new WaitForSecondsRealtime(duration);
    float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
    Time.timeScale = restoreScale;
    Time.fixedDeltaTime = 0.02f * restoreScale;
    hitStopCoroutine = null;
    IsAnyHitlagActive = false;
  }

  private IEnumerator StaggerRoutine()
  {
    playerController.LockMovement();

    // Bật I-frames cứu sinh khi vỡ Poise
    if (health != null && staggerIFrameDuration > 0f)
    {
      health.SetInvulnerable(true);
    }

    float elapsed = 0f;
    bool iFrameActive = staggerIFrameDuration > 0f;

    while (elapsed < staggerDuration)
    {
      elapsed += Time.deltaTime;
      if (iFrameActive && elapsed >= staggerIFrameDuration)
      {
        iFrameActive = false;
        if (health != null) health.SetInvulnerable(false);
      }
      yield return null;
    }

    if (health != null) health.SetInvulnerable(false);
    playerController.UnlockMovement();
    staggerCoroutine = null;
  }

  private IEnumerator ApplyKnockback(Vector3 direction, float distance)
  {
    direction.y = 0f;
    direction.Normalize();

    float timer = 0f;
    float speed = distance / knockbackDuration;

    while (timer < knockbackDuration)
    {
      timer += Time.deltaTime;
      characterController.Move(direction * (speed * Time.deltaTime));
      yield return null;
    }

    knockbackCoroutine = null;
  }

  private void HandleDeath()
  {
    if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
    if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
    if (health != null) health.SetInvulnerable(false);

    // Xóa ngay lập tức trigger GetHit để không bị tranh chấp hoặc đè lên hoạt ảnh chết
    animator.ResetTrigger(GetHitHash);
    animator.SetTrigger(DieHash);
    animator.CrossFadeInFixedTime("Death", 0.1f, 0);

    playerController.LockMovement();
    playerController.enabled = false;
    characterController.enabled = false;

    if (targetLockSystem != null)
    {
      targetLockSystem.UnlockTarget();
      targetLockSystem.enabled = false;
    }

    RHandAdjust rHand = GetComponentInChildren<RHandAdjust>();
    if (rHand != null) rHand.enabled = false;

    Debug.Log("<color=red>[YOU DIED]</color>");
  }

  // =========================================================================
  // HEALING VFX FEEDBACK (UỐNG MÁU & CẦU NGUYỆN CHUÔNG LÀNG)
  // =========================================================================

  private Coroutine healingVfxCoroutine;
  private GameObject currentHealingVfxInstance;

  /// <summary>
  /// Kích hoạt hiệu ứng VFX hồi máu (Healing VFX) ngay tại thân người chơi trong khoảng thời gian chỉ định (mặc định 1s).
  /// Dành cho uống bình máu hoặc cầu nguyện tại Chuông Làng.
  /// </summary>
  public void PlayHealingVfx()
  {
    if (healingVfxPrefab == null)
    {
      healingVfxPrefab = Resources.Load<GameObject>("Healing");
    }

    if (healingVfxPrefab == null)
    {
      Debug.LogWarning("[PlayerFeedback] Không tìm thấy healingVfxPrefab hoặc Resources/Healing!");
      return;
    }

    if (healingVfxCoroutine != null)
    {
      StopCoroutine(healingVfxCoroutine);
      if (currentHealingVfxInstance != null)
      {
        Destroy(currentHealingVfxInstance);
        currentHealingVfxInstance = null;
      }
    }

    healingVfxCoroutine = StartCoroutine(HealingVfxRoutine());
  }

  private IEnumerator HealingVfxRoutine()
  {
    currentHealingVfxInstance = Instantiate(healingVfxPrefab, transform);
    currentHealingVfxInstance.transform.localPosition = healingVfxOffset;
    currentHealingVfxInstance.transform.localRotation = healingVfxPrefab.transform.localRotation;
    currentHealingVfxInstance.transform.localScale = healingVfxPrefab.transform.localScale;

    // Kích hoạt toàn bộ ParticleSystem con
    ParticleSystem[] particles = currentHealingVfxInstance.GetComponentsInChildren<ParticleSystem>();
    foreach (var ps in particles)
    {
      ps.Play();
    }

    yield return new WaitForSeconds(healingVfxDuration);

    // Dừng phát hạt mới để các hạt hiện có tan tự nhiên
    if (particles != null)
    {
      foreach (var ps in particles)
      {
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
      }
    }

    yield return new WaitForSeconds(0.4f);

    if (currentHealingVfxInstance != null)
    {
      Destroy(currentHealingVfxInstance);
      currentHealingVfxInstance = null;
    }

    healingVfxCoroutine = null;
  }

  public void EnableHyperArmor() => HasHyperArmor = true;
  public void DisableHyperArmor() => HasHyperArmor = false;
}