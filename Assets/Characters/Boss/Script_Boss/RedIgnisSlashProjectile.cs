using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RedIgnisSlashProjectile : MonoBehaviour
{
    [Header("=== THÔNG SỐ BAY (MOVEMENT) ===")]
    [Tooltip("Tốc độ bay của đường chém (mét/giây)")]
    [SerializeField] private float speed = 20f;
    [Tooltip("Khoảng cách bay tối đa của đường chém (mét). <= 0 là không giới hạn khoảng cách")]
    [SerializeField] private float maxDistance = 0f;
    [Tooltip("Thời gian tồn tại tối đa (giây)")]
    [SerializeField] private float lifeTime = 3f;

    [Header("=== GÓC NGHIÊNG ĐƯỜNG KIẾM (VISUAL TILT) ===")]
    [Tooltip("Góc xoay visual quanh trục Z: 0 độ = chém ngang (Horizontal), 90 độ = chém dọc (Vertical AttackSlash)")]
    [SerializeField] private float slashTiltAngle = 0f;
    [Tooltip("Transform chứa visual/particle/mesh của đường chém")]
    [SerializeField] private Transform visualTransform;

    [Header("=== SÁT THƯƠNG & VA CHẠM ===")]
    [Tooltip("Sát thương khi chém trúng Player")]
    [SerializeField] private float damage = 55f;
    [Tooltip("Lực đẩy lùi đối thủ")]
    [SerializeField] private float knockback = 2.0f;
    [Tooltip("Layer mục tiêu bị sát thương (Mặc định: Player, Damageable)")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Layer chướng ngại vật cản đường (Mặc định: Default, Terrain, Obstacle)")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("=== HIỆU ỨNG VA CHẠM (HIT VFX) ===")]
    [Tooltip("Prefab hiệu ứng bốc khói/lửa khi đạn va chạm")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("=== AN TOÀN SPAWN ===")]
    [SerializeField] private float spawnGracePeriod = 0.06f;

    private Rigidbody rb;
    private BoxCollider boxCollider;
    private Transform attacker;
    private bool hasHit = false;
    private float aliveTimer = 0f;
    private float traveledDistance = 0f;

    public float Damage { get => damage; set => damage = value; }
    public float Speed { get => speed; set => speed = value; }
    public float MaxDistance { get => maxDistance; set => maxDistance = value; }
    public float SlashTiltAngle { get => slashTiltAngle; set => slashTiltAngle = value; }
    public GameObject HitVfxPrefab { get => hitVfxPrefab; set => hitVfxPrefab = value; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        EnsureDefaultLayers();
        Destroy(gameObject, lifeTime);
    }

    private void EnsureDefaultLayers()
    {
        if (targetLayers.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int damageableLayer = LayerMask.NameToLayer("Damageable");
            int mask = 0;
            if (playerLayer != -1) mask |= (1 << playerLayer);
            if (damageableLayer != -1) mask |= (1 << damageableLayer);
            targetLayers = mask;
        }

        if (obstacleLayers.value == 0)
        {
            int defLayer = LayerMask.NameToLayer("Default");
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            int mask = 0;
            if (defLayer != -1) mask |= (1 << defLayer);
            if (terrainLayer != -1) mask |= (1 << terrainLayer);
            if (obstacleLayer != -1) mask |= (1 << obstacleLayer);
            obstacleLayers = mask;
        }
    }

    /// <summary>
    /// Bắn đường kiếm bay thẳng về phía trước theo launchDirection với tốc độ, góc nghiêng và tầm bay tùy chỉnh
    /// </summary>
    public void Launch(Transform attackerRoot, Vector3 launchDirection, float speedOverride = -1f, float tiltAngleOverride = -999f, float maxDistanceOverride = -1f)
    {
        attacker = attackerRoot;
        traveledDistance = 0f;

        if (boxCollider != null && attackerRoot != null)
        {
            Collider[] attackerColliders = attackerRoot.GetComponentsInChildren<Collider>();
            foreach (var col in attackerColliders)
            {
                if (col != null)
                {
                    Physics.IgnoreCollision(boxCollider, col, true);
                }
            }
        }

        launchDirection.y = 0f;
        if (launchDirection.sqrMagnitude < 0.001f)
        {
            launchDirection = transform.forward;
        }
        launchDirection.Normalize();

        if (speedOverride > 0f)
        {
            speed = speedOverride;
        }

        if (tiltAngleOverride > -900f)
        {
            slashTiltAngle = tiltAngleOverride;
        }

        if (maxDistanceOverride >= 0f)
        {
            maxDistance = maxDistanceOverride;
        }

        // Xoay TOÀN BỘ Game Object theo hướng bắn kết hợp góc nghiêng (Roll quanh trục Z - trục bay).
        // Nhờ đó TẤT CẢ Particle Systems con, vệt chém, mesh và BoxCollider đều nghiêng đồng bộ chính xác!
        transform.rotation = Quaternion.LookRotation(launchDirection) * Quaternion.Euler(0f, 0f, slashTiltAngle);

        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localRotation = Quaternion.identity;
        }

        // Kích hoạt tất cả Particle Systems sau khi đã xoay chuẩn xác hướng và góc nghiêng
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            if (ps != null)
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    private void Update()
    {
        if (hasHit) return;

        aliveTimer += Time.deltaTime;
        float moveDistance = speed * Time.deltaTime;
        if (maxDistance > 0f)
        {
            float remaining = maxDistance - traveledDistance;
            if (remaining <= 0.001f)
            {
                Destroy(gameObject);
                return;
            }
            moveDistance = Mathf.Min(moveDistance, remaining);
        }

        Vector3 moveStep = transform.forward * moveDistance;

        // Dò tia trước mặt chống xuyên thấu (Raycast sweep)
        if (aliveTimer >= spawnGracePeriod)
        {
            // Quét mục tiêu địch (Player)
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit playerHit, moveDistance + 0.3f, targetLayers))
            {
                if (attacker == null || playerHit.transform.root != attacker)
                {
                    OnHitTarget(playerHit.collider, playerHit.point, playerHit.normal);
                    return;
                }
            }

            // Quét chướng ngại vật (Tường/Vật cản)
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, moveDistance + 0.25f, obstacleLayers))
            {
                if (Vector3.Angle(wallHit.normal, Vector3.up) >= 30f)
                {
                    hasHit = true;
                    transform.position = wallHit.point;
                    SpawnHitVfx(wallHit.point, wallHit.normal);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += moveStep;
        traveledDistance += moveDistance;

        if (maxDistance > 0f && traveledDistance >= maxDistance - 0.001f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (attacker != null && other.transform.root == attacker) return;

        if ((targetLayers.value & (1 << other.gameObject.layer)) != 0 || other.CompareTag("Player"))
        {
            Vector3 hitPoint = GetSafeHitPoint(other);
            OnHitTarget(other, hitPoint, -transform.forward);
            return;
        }

        if ((obstacleLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            if (aliveTimer < spawnGracePeriod) return;

            // Bỏ qua nếu chạm sàn
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit floorHit, 1.8f, obstacleLayers))
            {
                if (floorHit.collider == other && Vector3.Angle(floorHit.normal, Vector3.up) < 30f)
                {
                    return;
                }
            }

            hasHit = true;
            Vector3 hitPoint = GetSafeHitPoint(other);
            SpawnHitVfx(hitPoint, -transform.forward);
            Destroy(gameObject);
        }
    }

    private void OnHitTarget(Collider targetCol, Vector3 hitPoint, Vector3 hitNormal)
    {
        hasHit = true;

        Health targetHealth = targetCol.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, hitPoint, transform.forward, knockback, attacker);
        }
        else
        {
            IDamageable damageable = targetCol.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, hitPoint, transform.forward, knockback);
            }
        }

        SpawnHitVfx(hitPoint, hitNormal);
        Destroy(gameObject);
    }

    private Vector3 GetSafeHitPoint(Collider other)
    {
        if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider)
        {
            return other.ClosestPoint(transform.position);
        }

        if (other is MeshCollider meshCol && meshCol.convex)
        {
            return other.ClosestPoint(transform.position);
        }

        return other.bounds.ClosestPoint(transform.position);
    }

    private void SpawnHitVfx(Vector3 point, Vector3 normal)
    {
        if (hitVfxPrefab != null)
        {
            Vector3 spawnPos = point + normal * 0.05f;
            Quaternion spawnRot = (normal != Vector3.zero) ? Quaternion.LookRotation(normal) : transform.rotation;
            Instantiate(hitVfxPrefab, spawnPos, spawnRot);
        }
    }
}
