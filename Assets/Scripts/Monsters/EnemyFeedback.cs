using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyFeedback : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private Collider enemyCollider;
  [Tooltip("Để trống thì code tự tìm tất cả bộ phận con. Hoặc bạn có thể tự kéo các bộ phận vào đây.")]
  [SerializeField] private Renderer[] bodyRenderers;

  [Header("Flash Feedback")]
  [Tooltip("Thời gian nháy khi dính đòn (giây). 0.08s - 0.1s cho cảm giác chém trúng rất đã tay")]
  [SerializeField] private float flashDuration = 0.08f;

  [Tooltip("Thời gian quái giữ màu nháy trắng khi dính đòn kết liễu (giây). Giữ trắng xuyên suốt hitlag kết liễu để tạo điểm nhấn thị giác cực mạnh")]
  [SerializeField] private float lethalFlashDuration = 0.15f;

  [Tooltip("Màu nháy khi dính đòn (Mặc định: Trắng Color.white)")]
  [ColorUsage(true, true)]
  [SerializeField] private Color flashColor = Color.white;

  [Tooltip("Cường độ phát quang (Emission) giúp nháy trắng rực rỡ và nổi bật ngay cả trong góc tối hầm ngục")]
  [Range(0f, 5f)]
  [SerializeField] private float emissionIntensity = 2.5f;

  [Tooltip("Ghi đè texture thành màu trắng thuần khiết khi nháy (tránh bị chìm vào vân texture xỉn màu)")]
  [SerializeField] private bool solidWhiteFlash = true;

  [Header("Hit VFX Feedback")]
  [Tooltip("Prefab VFX tứa ra tại điểm trúng đòn (Mặc định: MobsGetHitVFX màu xám cho quái xương)")]
  [SerializeField] private GameObject hitVfxPrefab;
  [Tooltip("Thời gian tự hủy instance VFX (giây) đề phòng nếu VFX không tự hủy")]
  [SerializeField] private float vfxDestroyDelay = 2.0f;

  [Header("Death Settings")]
  [Tooltip("Thời gian chờ xóa GameObject sau khi quái chết (giây)")]
  [SerializeField] private float corpseDestroyDelay = 5f;

  [Header("Souls-like Poise Settings")]
  [SerializeField] private float maxPoise = 100f;
  [SerializeField] private float poiseRecoveryDelay = 2.5f;
  [SerializeField] private float poiseRecoveryRate = 40f;

  [Header("Knockback Settings")]
  [SerializeField] private float knockbackDuration = 0.15f;

  // Sử dụng MaterialPropertyBlock để chớp trắng trực tiếp trên GPU: Không clone Material, 100% tương thích SRP Batcher & GPU Instancing!
  private static MaterialPropertyBlock flashPropertyBlock;
  private static readonly int BaseMapProp = Shader.PropertyToID("_BaseMap");
  private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
  private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
  private static readonly int ColorProp = Shader.PropertyToID("_Color");
  private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

  private Health health;
  private EnemyAIBase enemyAI;
  private float currentPoise;
  private float lastHitTime;
  private Coroutine knockbackCoroutine;
  private Coroutine flashCoroutine;

  private static readonly int GetHitTriggerHash = Animator.StringToHash("GetHit");
  private static readonly int HitTypeHash = Animator.StringToHash("HitType");
  private static readonly int DieTriggerHash = Animator.StringToHash("Die");
  private static readonly int DeathTypeHash = Animator.StringToHash("DeathType");

  private void Awake()
  {
    health = GetComponent<Health>();
    enemyAI = GetComponent<EnemyAIBase>();

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (enemyCollider == null) enemyCollider = GetComponent<Collider>();

    currentPoise = maxPoise;

    // Tự tìm tất cả SkinnedMeshRenderer và MeshRenderer ở mọi object con nếu chưa kéo tay
    if (bodyRenderers == null || bodyRenderers.Length == 0)
    {
      bodyRenderers = GetComponentsInChildren<Renderer>(true);
    }

    // Kích hoạt từ khóa _EMISSION trên Shared Material để shader URP Lit xử lý phát quang từ MaterialPropertyBlock
    if (bodyRenderers != null)
    {
      for (int i = 0; i < bodyRenderers.Length; i++)
      {
        if (bodyRenderers[i] == null) continue;
        Material[] mats = bodyRenderers[i].sharedMaterials;
        if (mats != null)
        {
          for (int m = 0; m < mats.Length; m++)
          {
            if (mats[m] != null)
            {
              mats[m].EnableKeyword("_EMISSION");
            }
          }
        }
      }
    }

#if UNITY_EDITOR
    if (hitVfxPrefab == null)
    {
      hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/MobsGetHitVFX.prefab");
    }
#endif
  }

  private void OnEnable()
  {
    health.onTakeDamage.AddListener(HandleTakeDamage);
    health.onDeath.AddListener(HandleDeath);
    if (currentPoise <= 0f && (health == null || !health.IsDead))
    {
      currentPoise = maxPoise;
    }
  }

  private void OnDisable()
  {
    health.onTakeDamage.RemoveListener(HandleTakeDamage);
    health.onDeath.RemoveListener(HandleDeath);
    ResetColors();
  }

  private void Update()
  {
    if ((health != null && health.IsDead) || (enemyAI != null && enemyAI.IsSleeping)) return;

    if (currentPoise < maxPoise && Time.time >= lastHitTime + poiseRecoveryDelay)
    {
      currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
    }
  }

  private void HandleTakeDamage(Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance)
  {
    // Luôn sinh hiệu ứng tứa xám (MobsGetHitVFX) tại điểm chém (kể cả khi đòn này kết liễu quái)
    SpawnHitVfx(hitPoint, hitDirection);

    if (health.IsDead || health.CurrentHealth <= 0f) return;

    lastHitTime = Time.time;

    // 1. Tính lượng Poise bị trừ: Ưu tiên lấy trực tiếp từ cấu hình đòn đánh của Player (Health.LastPoiseDamage)
    float poiseDamageTaken = (health != null && health.LastPoiseDamage > 0f)
      ? health.LastPoiseDamage
      : (knockbackDistance >= 1.5f ? 100f : (knockbackDistance >= 0.8f ? 55f : 25f));

    currentPoise -= poiseDamageTaken;

    // 2. Luôn nháy trắng tất cả bộ phận khi trúng kiếm (Visual Feedback đanh thép)
    FlashDamageFeedback();

    // 3. Cơ chế Souls-like Poise: Khi còn Poise (currentPoise > 0f), quái giữ vững thế đứng (Super Armor)
    // CHỈ nháy trắng chịu đòn, TUYỆT ĐỐI KHÔNG bị giật hoạt ảnh GetHit hay bị ngắt hành động!
    if (currentPoise > 0f)
    {
      return;
    }

    // 4. VỠ THẾ (Poise Broken / Stagger): Reset lại Poise và kích hoạt Animation GetHit
    currentPoise = maxPoise;

    // Ngắt đòn đánh của quái nếu đang vung kiếm
    if (enemyAI != null && enemyAI.IsAttacking)
    {
      enemyAI.InterruptAttack();
    }

    int randomHit = Random.Range(0, 2);
    animator.SetInteger(HitTypeHash, randomHit);
    animator.SetTrigger(GetHitTriggerHash);

    if (knockbackDistance > 0f)
    {
      if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
      knockbackCoroutine = StartCoroutine(ApplyKnockback(hitDirection, knockbackDistance));
    }
  }

  /// <summary>
  /// Sinh hiệu ứng VFX tứa mảnh vụn xương (MobsGetHitVFX) tại điểm chém
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

    // Hướng tứa: Bắn ngược chiều cú chém ra ngoài không khí
    Vector3 spawnDir = -hitDirection;
    if (spawnDir.sqrMagnitude < 0.001f)
    {
      spawnDir = -transform.forward;
    }
    spawnDir.Normalize();

    // Dịch nhẹ điểm spawn ra ngoài bề mặt 0.05m để không bị lẹm vào trong mesh
    spawnPos += spawnDir * 0.05f;

    Quaternion spawnRot = Quaternion.LookRotation(spawnDir);
    GameObject vfxInstance = Instantiate(hitVfxPrefab, spawnPos, spawnRot);
    if (vfxDestroyDelay > 0f)
    {
      Destroy(vfxInstance, vfxDestroyDelay);
    }
  }

  // =========================================================================
  // XỬ LÝ NHÁY TRẮNG TRỰC TIẾP QUA MATERIAL PROPERTY BLOCK
  // =========================================================================

  private void FlashDamageFeedback()
  {
    if (bodyRenderers == null || bodyRenderers.Length == 0) return;

    if (flashCoroutine != null) StopCoroutine(flashCoroutine);
    flashCoroutine = StartCoroutine(FlashDamageRoutine());
  }

  private IEnumerator FlashDamageRoutine()
  {
    ApplyFlashColors();
    yield return new WaitForSecondsRealtime(flashDuration);
    ResetColors();
    flashCoroutine = null;
  }

  private void ApplyFlashColors()
  {
    if (bodyRenderers == null || bodyRenderers.Length == 0) return;

    if (flashPropertyBlock == null)
    {
      flashPropertyBlock = new MaterialPropertyBlock();
    }

    // 1. Ghi đè texture thành màu trắng thuần (Texture2D.whiteTexture)
    // Giúp loại bỏ texture xỉn màu gốc, đưa mô hình về màu trắng tinh khiết
    if (solidWhiteFlash)
    {
      flashPropertyBlock.SetTexture(BaseMapProp, Texture2D.whiteTexture);
      flashPropertyBlock.SetTexture(MainTexProp, Texture2D.whiteTexture);
    }

    // 2. Thiết lập tông màu Base Color
    flashPropertyBlock.SetColor(BaseColorProp, flashColor);
    flashPropertyBlock.SetColor(ColorProp, flashColor);

    // 3. Tăng cường độ phát sáng (Emission) để nháy chói lóa ngay cả khi quái đứng trong bóng tối hầm ngục
    if (emissionIntensity > 0f)
    {
      Color emissionColor = flashColor * emissionIntensity;
      flashPropertyBlock.SetColor(EmissionColorProp, emissionColor);
    }

    for (int i = 0; i < bodyRenderers.Length; i++)
    {
      if (bodyRenderers[i] != null && bodyRenderers[i].enabled)
      {
        bodyRenderers[i].SetPropertyBlock(flashPropertyBlock);
      }
    }
  }

  private void ResetColors()
  {
    if (bodyRenderers == null) return;
    for (int i = 0; i < bodyRenderers.Length; i++)
    {
      if (bodyRenderers[i] != null)
      {
        bodyRenderers[i].SetPropertyBlock(null);
      }
    }
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
      transform.position += direction * (speed * Time.deltaTime);
      yield return null;
    }

    knockbackCoroutine = null;
  }

  // =========================================================================
  // XỬ LÝ CHẾT (Đòn kết liễu: Nháy trắng -> Trả về màu bình thường -> Death)
  // =========================================================================

  private void HandleDeath()
  {
    if (knockbackCoroutine != null)
    {
      StopCoroutine(knockbackCoroutine);
      knockbackCoroutine = null;
    }
    if (flashCoroutine != null)
    {
      StopCoroutine(flashCoroutine);
      flashCoroutine = null;
    }

    StartCoroutine(DeathRoutine());
  }

  private IEnumerator DeathRoutine()
  {
    // 1. Tắt va chạm ngay lập tức để không cản trở di chuyển của Player và không nhận thêm đòn đánh thừa
    if (enemyCollider != null) enemyCollider.enabled = false;

    // 2. Nháy trắng toàn thân khi dính đòn kết liễu
    ApplyFlashColors();

    // 3. Giữ màu trắng trong suốt thời gian hitlag kết liễu (dùng unscaled realtime vì game đang khựng ở timeScale = 0.05)
    yield return new WaitForSecondsRealtime(lethalFlashDuration);

    // 4. Trả lại màu và texture ban đầu cho quái vật
    ResetColors();

    // 5. Sau khi hết nháy và đã về màu bình thường mới kích hoạt animation ngã chết
    if (animator != null)
    {
      int randomDeath = Random.Range(0, 2);
      animator.SetInteger(DeathTypeHash, randomDeath);
      animator.SetTrigger(DieTriggerHash);
    }

    // 6. Xóa xác quái vật sau thời gian chờ
    Destroy(gameObject, corpseDestroyDelay);
  }
}
