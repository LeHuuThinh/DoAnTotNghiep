using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
  [Header("Hitbox Settings")]
  [Tooltip("Chỉ gây sát thương lên các Layer được chọn (ví dụ: Enemy, Breakable)")]
  [SerializeField] private LayerMask targetLayers;

  private Collider hitCollider;
  private float currentDamage = 10f;
  private float currentKnockback = 0f;
  private float currentHitlag = 0f; // Biến lưu thời gian hitlag của nhát chém
  private Transform attacker;

  // Danh sách lưu các mục tiêu đã trúng đòn trong 1 lần vung kiếm (chống lặp dame)
  private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
  private static Coroutine activeHitlagCoroutine;

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
  /// </summary>
  public void EnableHitbox(float damage, float knockback, float hitlag = 0f)
  {
    currentDamage = damage;
    currentKnockback = knockback;
    currentHitlag = hitlag;
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

      // Gây sát thương
      damageable.TakeDamage(currentDamage, hitPoint, hitDirection, currentKnockback);

      // 3. KÍCH HOẠT HITLAG NẾU ĐÒN ĐÁNH CÓ CÀI ĐẶT THỜI GIAN KHỰNG
      if (currentHitlag > 0f)
      {
        TriggerHitlag(currentHitlag);
      }
    }
  }

  private void TriggerHitlag(float duration)
  {
    if (activeHitlagCoroutine != null)
    {
      StopCoroutine(activeHitlagCoroutine);
      Time.timeScale = 1f;
    }

    activeHitlagCoroutine = StartCoroutine(HitlagRoutine(duration));
  }

  private IEnumerator HitlagRoutine(float duration)
  {
    Time.timeScale = 0.05f; // Làm chậm thế giới xuống còn 5% để tạo độ khựng đanh
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1f;    // Trả lại tốc độ bình thường
    activeHitlagCoroutine = null;
  }

  private void OnDisable()
  {
    // Failsafe: Đảm bảo thời gian không bao giờ bị kẹt lại 0.05 nếu kiếm bị tắt đột ngột
    if (activeHitlagCoroutine != null)
    {
      Time.timeScale = 1f;
      activeHitlagCoroutine = null;
    }
  }
}