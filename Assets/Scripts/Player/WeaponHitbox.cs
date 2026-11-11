using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
  [Header("Hitbox Settings")]
  [Tooltip("Chỉ gây sát thương lên các Layer được chọn (ví dụ: Enemy, Breakable)")]
  [SerializeField] private LayerMask targetLayers;
  public LayerMask TargetLayers => targetLayers;

  [Header("Hit Impact VFX Settings")]
  [Tooltip("Hiệu ứng va chạm (Impact burst) phát sinh ngay tại điểm tiếp xúc khi chém trúng mục tiêu có HP (Layer Enemy / Damageable)")]
  [SerializeField] private GameObject hitImpactVfxPrefab;
  [Tooltip("Thời gian tự hủy instance Impact VFX (giây)")]
  [SerializeField] private float hitImpactDestroyDelay = 1.5f;
  [Tooltip("Độ lệch vị trí tứa ra ngoài bề mặt (m) chống lẹm vào mesh mục tiêu")]
  [SerializeField] private float hitImpactNormalOffset = 0.05f;
  [Tooltip("Tự động chỉnh tỉ lệ phóng to/thu nhỏ của Impact VFX")]
  [SerializeField] private float hitImpactScaleMultiplier = 1.0f;

  [Header("Hitlag Settings")]
  [Tooltip("Tốc độ thời gian khi khựng (Time.timeScale). 0.05 = làm chậm 95%")]
  [Range(0f, 0.2f)]
  [SerializeField] private float hitlagTimeScale = 0.05f;

  [Tooltip("Thời gian hitlag mặc định cho đòn kết liễu (giây) nếu không được chỉ định riêng")]
  [SerializeField] private float defaultLethalHitlag = 0.18f;

  private Collider hitCollider;
  private float currentDamage = 10f;
  private float currentKnockback = 0f;
  private float currentHitlag = 0f; // Biến lưu thời gian hitlag của nhát chém
  private float currentLethalHitlag = 0.18f; // Biến lưu thời gian hitlag khi kết liễu
  private float currentPoiseDamage = -1f; // Biến lưu sát thương Poise (làm mất thăng bằng) của nhát chém
  private float currentCritRate = 0f;
  private float currentCritDamageMultiplier = 1.5f;
  private float currentLifestealPercent = 0f;
  private InfiniteLabyrinth.UI.Combat.DamageNumberType currentDamageType = InfiniteLabyrinth.UI.Combat.DamageNumberType.Normal;
  private Transform attacker;

  // Danh sách lưu các mục tiêu đã trúng đòn trong 1 lần vung kiếm (chống lặp dame)
  private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
  private static Coroutine activeHitlagCoroutine;
  private static WeaponHitbox activeRunner;
  private static float activeHitlagEndTime;
  public static bool IsHitlagActive => activeHitlagCoroutine != null;

  // Event thông báo chém trúng mục tiêu (dùng cho tích nộ/spirit, lifesteal...)
  public event System.Action<IDamageable, float> OnHitTarget;

  private void Awake()
  {
    hitCollider = GetComponent<Collider>();
    hitCollider.isTrigger = true;
    hitCollider.enabled = false; // Mặc định tắt khi chưa chém
  }

  public void Initialize(Transform attackerTransform)
  {
    attacker = attackerTransform;
  }

  /// <summary>
  /// Bật hitbox khi kiếm bắt đầu vung trúng đích.
  /// hitlag = 0f làm mặc định để quái vật dùng chung không bị ảnh hưởng.
  /// lethalHitlag = -1f để tự động kế thừa hoặc dùng defaultLethalHitlag.
  /// poiseDamage = -1f để dùng mặc định nếu không truyền từ đòn đánh.
  /// critRate, critDamageMultiplier, lifestealPercent phục vụ hệ thống chỉ số mở rộng của Player.
  /// </summary>
  public void EnableHitbox(
    float damage, 
    float knockback, 
    float hitlag = 0f, 
    float lethalHitlag = -1f, 
    float poiseDamage = -1f, 
    InfiniteLabyrinth.UI.Combat.DamageNumberType damageType = InfiniteLabyrinth.UI.Combat.DamageNumberType.Normal,
    float critRate = 0f,
    float critDamageMultiplier = 1.5f,
    float lifestealPercent = 0f)
  {
    currentDamage = damage;
    currentKnockback = knockback;
    currentHitlag = hitlag;
    currentPoiseDamage = poiseDamage;
    currentDamageType = damageType;
    currentCritRate = critRate;
    currentCritDamageMultiplier = critDamageMultiplier > 0f ? critDamageMultiplier : 1.5f;
    currentLifestealPercent = lifestealPercent;

    if (lethalHitlag >= 0f)
    {
      currentLethalHitlag = lethalHitlag;
    }
    else
    {
      currentLethalHitlag = hitlag > 0f ? defaultLethalHitlag : 0f;
    }
    hitTargets.Clear();
    if (hitCollider != null) hitCollider.enabled = true;
  }

  /// <summary>
  /// Tắt hitbox khi kiếm đã chém qua đích
  /// </summary>
  public void DisableHitbox()
  {
    if (hitCollider != null) hitCollider.enabled = false;
    hitTargets.Clear();
  }

  private void OnTriggerEnter(Collider other)
  {
    // Bỏ qua nếu va chạm trúng chính bản thân người vung kiếm
    if (attacker != null && other.transform.root == attacker) return;

    // 1. Kiểm tra layer có nằm trong danh sách được phép chém không
    if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

    // 2. Tìm IDamageable trên đối tượng va chạm (hoặc cha của nó)
    IDamageable damageable = other.GetComponentInParent<IDamageable>();

    if (damageable != null && !hitTargets.Contains(damageable))
    {
      hitTargets.Add(damageable); // Đánh dấu đã chém trúng, không chém lần 2 trong nhát này

      // Lấy điểm tiếp xúc gần nhất và hướng đánh
      Vector3 hitPoint = other.ClosestPoint(transform.position);
      Vector3 hitDirection = attacker != null ? attacker.forward : transform.forward;

      // 1. KÍCH HOẠT IMPACT VFX NGAY TẠI ĐIỂM TIẾP XÚC KHI CHÉM TRÚNG MỤC TIÊU CÓ HP
      SpawnHitImpactVfx(hitPoint, hitDirection);

      // Kiểm tra trạng thái sống trước khi trừ máu
      Health targetHealth = damageable as Health ?? (damageable as Component)?.GetComponentInParent<Health>();
      bool wasAlive = targetHealth == null || !targetHealth.IsDead;

      // Truyền sát thương Poise tùy chỉnh của nhát chém tới mục tiêu
      if (targetHealth != null)
      {
        targetHealth.SetIncomingPoiseDamage(currentPoiseDamage);
      }

      // Tính toán Chí Mạng (Critical Hit)
      bool isCrit = currentCritRate > 0f && UnityEngine.Random.Range(0f, 100f) < currentCritRate;
      float finalDamage = isCrit ? (currentDamage * currentCritDamageMultiplier) : currentDamage;

      // Gây sát thương (kèm tham chiếu attacker và cờ isCrit)
      damageable.TakeDamage(finalDamage, hitPoint, hitDirection, currentKnockback, attacker, currentDamageType, isCrit);
      OnHitTarget?.Invoke(damageable, finalDamage);

      // Kích hoạt Phù Phép khi Bạo Kích (Bleed & Frenzy)
      if (isCrit && attacker != null)
      {
        if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
            InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.BleedingCrit))
        {
          if (targetHealth != null && !targetHealth.IsDead)
          {
            targetHealth.ApplyBleed(finalDamage * 0.3f, 3f, 3, attacker);
          }
        }

        if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
            InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.CritFrenzy))
        {
          var pc = attacker.GetComponent<PlayerController>();
          if (pc != null) pc.TriggerTemporarySpeedBoost(15f, 3.5f);
        }
      }

      // Xử lý Hút Máu (Lifesteal)
      if (currentLifestealPercent > 0f && attacker != null)
      {
        Health attackerHealth = attacker.GetComponent<Health>();
        if (attackerHealth != null && !attackerHealth.IsDead)
        {
          float healAmount = Mathf.Min(finalDamage * (currentLifestealPercent / 100f), 25f);
          if (healAmount > 0.5f)
          {
            attackerHealth.Heal(healAmount);
          }
        }
      }

      // Nhận diện đòn đánh có phải là đòn kết liễu hạ gục mục tiêu không
      bool isLethalHit = wasAlive && targetHealth != null && targetHealth.IsDead;

      // 3. KÍCH HOẠT HITLAG NẾU ĐÒN ĐÁNH CÓ CÀI ĐẶT THỜI GIAN KHỰNG
      float hitlagToApply = isLethalHit
        ? (currentLethalHitlag > 0f ? currentLethalHitlag : defaultLethalHitlag)
        : currentHitlag;

      if (hitlagToApply > 0f)
      {
        TriggerHitlag(hitlagToApply);
      }
    }
  }

  private void TriggerHitlag(float duration)
  {
    float expectedEndTime = Time.unscaledTime + duration;
    // Không cho hitlag ngắn đè lên hitlag dài (ví dụ đòn kết liễu) đang chạy
    if (activeHitlagCoroutine != null && expectedEndTime < activeHitlagEndTime)
    {
      return;
    }

    if (activeHitlagCoroutine != null && activeRunner != null)
    {
      activeRunner.StopCoroutine(activeHitlagCoroutine);
      float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
    }

    activeRunner = this;
    activeHitlagEndTime = expectedEndTime;
    activeHitlagCoroutine = StartCoroutine(HitlagRoutine(duration));
  }

  private IEnumerator HitlagRoutine(float duration)
  {
    Time.timeScale = hitlagTimeScale; // Làm chậm thế giới xuống còn 5% để tạo độ khựng đanh
    Time.fixedDeltaTime = 0.02f * Time.timeScale;
    yield return new WaitForSecondsRealtime(duration);
    float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
    Time.timeScale = restoreScale;    // Trả lại tốc độ slow-mo hiện tại hoặc tốc độ gốc
    Time.fixedDeltaTime = 0.02f * restoreScale;
    activeHitlagCoroutine = null;
    activeRunner = null;
  }

  /// <summary>
  /// Sinh hiệu ứng va chạm Impact VFX ngay tại điểm tiếp xúc thực tế khi chém trúng mục tiêu có HP.
  /// </summary>
  public void SpawnHitImpactVfx(Vector3 hitPoint, Vector3 hitDirection)
  {
    GameObject prefabToSpawn = hitImpactVfxPrefab;
    if (prefabToSpawn == null)
    {
      // Fallback 1: Tự động nạp từ thư mục Resources/Impact VFX
      prefabToSpawn = Resources.Load<GameObject>("Impact VFX");
      if (prefabToSpawn != null)
      {
        hitImpactVfxPrefab = prefabToSpawn;
      }
    }

    if (prefabToSpawn == null) return;

    Vector3 spawnPos = hitPoint;
    if (spawnPos == Vector3.zero)
    {
      spawnPos = transform.position;
    }

    // Hướng tứa: Bắn ngược chiều cú chém ra ngoài không khí
    Vector3 spawnDir = -hitDirection;
    if (spawnDir.sqrMagnitude < 0.001f)
    {
      spawnDir = transform.forward;
    }
    spawnDir.Normalize();

    spawnPos += spawnDir * hitImpactNormalOffset;
    Quaternion spawnRot = Quaternion.LookRotation(spawnDir);

    GameObject vfxInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
    if (Mathf.Abs(hitImpactScaleMultiplier - 1.0f) > 0.01f)
    {
      vfxInstance.transform.localScale = vfxInstance.transform.localScale * hitImpactScaleMultiplier;
    }

    // Đảm bảo toàn bộ ParticleSystem con (kể cả root có playOnAwake = false) đều phát nổ ngay lập tức
    ParticleSystem rootPs = vfxInstance.GetComponent<ParticleSystem>();
    if (rootPs != null)
    {
      rootPs.Play(true);
    }
    else
    {
      foreach (ParticleSystem ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
      {
        ps.Play();
      }
    }

    if (hitImpactDestroyDelay > 0f)
    {
      Destroy(vfxInstance, hitImpactDestroyDelay);
    }
  }

  private void OnDisable()
  {
    // Failsafe: Đảm bảo thời gian không bao giờ bị kẹt lại nếu kiếm bị tắt đột ngột
    if (activeRunner == this && activeHitlagCoroutine != null)
    {
      float restoreScale = PlayerCombat.GetActiveCombatTimeScale();
      Time.timeScale = restoreScale;
      Time.fixedDeltaTime = 0.02f * restoreScale;
      activeHitlagCoroutine = null;
      activeRunner = null;
    }
  }
}
