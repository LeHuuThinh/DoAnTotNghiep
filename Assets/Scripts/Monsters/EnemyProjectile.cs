using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
  [Header("Movement Settings")]
  [SerializeField] private float speed = 20f;
  [SerializeField] private float lifeTime = 5f;

  [Header("Combat Settings")]
  [SerializeField] private float damage = 15f;
  [SerializeField] private float knockback = 0.5f;
  [Tooltip("Layer mục tiêu (Bắt buộc chọn Layer 'Player' hoặc Layer chứa nhân vật người chơi)")]
  [SerializeField] private LayerMask targetLayers;
  [Tooltip("Layer vật cản (Ví dụ: Default, Wall, Environment) để mũi tên cắm vào tường")]
  [SerializeField] private LayerMask obstacleLayers;

  [Header("Hit Effects (Tùy chọn)")]
  [SerializeField] private GameObject hitVfxPrefab;
  [SerializeField] private float spawnGracePeriod = 0.05f;

  [Header("Visual Decoupling (Tùy chọn)")]
  [Tooltip("Kéo GameObject con chứa Model/Mesh vào đây (ví dụ: ArrowModel). Nếu để trống, code tự tìm con đầu tiên.")]
  [SerializeField] private Transform visualTransform;
  [Tooltip("Góc xoay bù trừ cho Visual con nếu cần tinh chỉnh thêm")]
  [SerializeField] private Vector3 visualRotationOffset = Vector3.zero;

  private Rigidbody rb;
  private Collider projCollider;
  private Transform attacker;
  private bool hasHit = false;
  private float aliveTimer = 0f;

  private void Awake()
  {
    rb = GetComponent<Rigidbody>();
    rb.isKinematic = true;
    rb.useGravity = false;

    projCollider = GetComponent<Collider>();
    projCollider.isTrigger = true;

    // Tự động tìm Visual/ArrowModel con nếu chưa gán thủ công
    if (visualTransform == null && transform.childCount > 0)
    {
      Transform foundChild = transform.Find("ArrowModel");
      if (foundChild == null) foundChild = transform.Find("Visual");
      visualTransform = foundChild != null ? foundChild : transform.GetChild(0);
    }

    if (visualTransform != null && visualRotationOffset != Vector3.zero)
    {
      visualTransform.localEulerAngles += visualRotationOffset;
    }

    // Đảm bảo tất cả ParticleSystem con (ví dụ: Flame Thrower) đều kích hoạt
    ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
    foreach (ParticleSystem p in particles)
    {
      if (!p.isPlaying) p.Play();
    }

    // Tự động hủy sau thời gian bay tối đa nếu không trúng gì
    Destroy(gameObject, lifeTime);
  }

  /// <summary>
  /// Khởi tạo và phóng mũi tên về hướng chỉ định
  /// </summary>
  public void Launch(Transform attackerRoot, Vector3 launchDirection, float customDamage = -1f, float customSpeed = -1f)
  {
    attacker = attackerRoot;

    if (customDamage > 0f) damage = customDamage;
    if (customSpeed > 0f) speed = customSpeed;

    // Bỏ qua va chạm với quái vật bắn ra mũi tên này
    if (attackerRoot != null)
    {
      Collider[] attackerColliders = attackerRoot.GetComponentsInChildren<Collider>();
      foreach (Collider col in attackerColliders)
      {
        if (projCollider != null && col != null)
        {
          Physics.IgnoreCollision(projCollider, col, true);
        }
      }
    }

    if (launchDirection.sqrMagnitude > 0.001f)
    {
      transform.rotation = Quaternion.LookRotation(launchDirection.normalized);
    }
  }

  private void Update()
  {
    if (hasHit) return;

    aliveTimer += Time.deltaTime;
    float moveDistance = speed * Time.deltaTime;
    Vector3 moveStep = transform.forward * moveDistance;

    // =========================================================================
    // QUÉT TIA DÒ ĐƯỜNG LIÊN TỤC CHỐNG XUYÊN TƯỜNG / VẬT CẢN & MỤC TIÊU Ở TỐC ĐỘ CAO
    // =========================================================================
    if (aliveTimer >= spawnGracePeriod)
    {
      LayerMask hitMask = obstacleLayers | targetLayers;
      if (Physics.Raycast(transform.position, transform.forward, out RaycastHit sweepHit, moveDistance + 0.15f, hitMask))
      {
        if (attacker == null || sweepHit.transform.root != attacker)
        {
          HandleHit(sweepHit.collider, sweepHit.point, sweepHit.normal);
          return;
        }
      }
    }

    transform.position += moveStep;
  }

  private void OnTriggerEnter(Collider other)
  {
    if (hasHit) return;
    if (attacker != null && other.transform.root == attacker) return;

    Vector3 hitPoint = GetClosestHitPoint(other);
    HandleHit(other, hitPoint, -transform.forward);
  }

  private void HandleHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
  {
    if (hasHit) return;
    if (attacker != null && other.transform.root == attacker) return;

    // 1. Trúng Player (hoặc mục tiêu nhận sát thương)
    if ((targetLayers.value & (1 << other.gameObject.layer)) != 0)
    {
      IDamageable damageable = other.GetComponentInParent<IDamageable>();
      if (damageable != null)
      {
        hasHit = true;
        
        // Gọi TakeDamage -> Truyền rõ kẻ tấn công để hệ thống Feedback, Parry & Aggro hoạt động chuẩn xác
        damageable.TakeDamage(damage, hitPoint, transform.forward, knockback, attacker);

        SpawnHitEffect(hitPoint, hitNormal.sqrMagnitude > 0.001f ? hitNormal : -transform.forward);
        Destroy(gameObject);
        return;
      }
    }

    // 2. Trúng tường hoặc vật thể cản đường
    if ((obstacleLayers.value & (1 << other.gameObject.layer)) != 0)
    {
      if (aliveTimer < spawnGracePeriod) return;

      hasHit = true;
      SpawnHitEffect(hitPoint, hitNormal.sqrMagnitude > 0.001f ? hitNormal : -transform.forward);
      Destroy(gameObject);
    }
  }

  private Vector3 GetClosestHitPoint(Collider other)
  {
    if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider)
    {
      return other.ClosestPoint(transform.position);
    }
    return other.bounds.ClosestPoint(transform.position);
  }

  private void SpawnHitEffect(Vector3 point, Vector3 normal)
  {
    if (hitVfxPrefab != null)
    {
      Vector3 spawnPos = point + normal * 0.05f;
      Quaternion spawnRot = Quaternion.LookRotation(normal);
      Instantiate(hitVfxPrefab, spawnPos, spawnRot);
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.yellow;
    Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
  }
#endif
}
