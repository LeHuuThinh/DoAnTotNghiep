using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ProjectileSlash : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Combat Settings")]
    [SerializeField] private float damage = 45f;
    [SerializeField] private float knockback = 1.2f;
    [SerializeField] private LayerMask targetLayers;   // Chọn "Damageable", "Enemy"
    [SerializeField] private LayerMask obstacleLayers; // Chọn "Default"

    [Header("Visual vs Hitbox Decoupling")]
    [Tooltip("Kéo GameObject con chứa Particle/Mesh vào đây. Nếu để trống, code tự tìm Object con đầu tiên để nghiêng VFX.")]
    [SerializeField] private Transform visualTransform;
    [Tooltip("Góc nghiêng visual theo đường kiếm chéo (35 đến 45 độ)")]
    [SerializeField] private float slashVisualTiltAngle = 40f;

    [Header("Hit Effect (Tùy chọn)")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("Hitlag Settings")]
    [Tooltip("Thời gian khựng đơ game (giây) khi sóng kiếm chém trúng quái vật (0.15s - 0.2s tạo độ đanh xé toạc mục tiêu)")]
    [SerializeField] private float hitlagDuration = 0.18f;
    [Range(0.01f, 0.2f)]
    [SerializeField] private float hitlagTimeScale = 0.05f;

    [Header("Spawn Safety")]
    [SerializeField] private float spawnGracePeriod = 0.08f;

    private Rigidbody rb;
    private Transform attacker;
    private BoxCollider boxCollider;
    private bool hasHit = false;
    private float aliveTimer = 0f;
    private float bonusDamage = 0f;
    private float critRate = 0f;
    private float critDamageMultiplier = 1.5f;
    private float lifestealPercent = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        // Tự tìm Object con chứa hình ảnh nếu chưa kéo tay
        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }

        // Kích hoạt tất cả Particle System con
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in particles)
        {
            p.Play();
        }

        Destroy(gameObject, lifeTime);
    }

    public void Launch(Transform attackerRoot, Vector3 launchDirection, float additionalBonusDamage = 0f, float critRate = 0f, float critDamageMultiplier = 1.5f, float lifesteal = 0f)
    {
        attacker = attackerRoot;
        bonusDamage = additionalBonusDamage;
        this.critRate = critRate;
        this.critDamageMultiplier = critDamageMultiplier > 0f ? critDamageMultiplier : 1.5f;
        this.lifestealPercent = lifesteal;

        Collider[] attackerColliders = attackerRoot.GetComponentsInChildren<Collider>();
        foreach (Collider col in attackerColliders)
        {
            Physics.IgnoreCollision(boxCollider, col, true);
        }

        launchDirection.y = 0f;
        launchDirection.Normalize();

        // 1. ROOT & HITBOX: Luôn giữ phẳng nằm ngang, mặt hướng về phía trước
        transform.rotation = Quaternion.LookRotation(launchDirection);

        // 2. VISUAL (EFFECT): Chỉ nghiêng riêng phần hình ảnh con để đánh lừa thị giác!
        if (visualTransform != null)
        {
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, slashVisualTiltAngle);
        }
    }

    private void Update()
    {
        if (hasHit) return;

        aliveTimer += Time.deltaTime;
        float moveDistance = speed * Time.deltaTime;
        Vector3 moveStep = transform.forward * moveDistance;

        // =====================================================================
        // QUÉT TIA DÒ ĐƯỜNG CHỐNG XUYÊN TƯỜNG (CONTINUOUS SWEEP)
        // =====================================================================
        if (aliveTimer >= spawnGracePeriod)
        {
            // Bắn một tia ngắn đón đầu phía trước đúng bằng quãng đường chuẩn bị di chuyển
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, moveDistance + 0.25f, obstacleLayers))
            {
                // Bỏ qua mặt sàn phẳng (chỉ nổ khi đâm vào vách tường đứng)
                if (Vector3.Angle(wallHit.normal, Vector3.up) >= 30f)
                {
                    hasHit = true;
                    // Hãm phanh đặt đạn ngay tại mặt trước bức tường
                    transform.position = wallHit.point;
                    SpawnHitVfx(wallHit.point, wallHit.normal);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Di chuyển tiếp nếu đường đi thông thoáng
        transform.position += moveStep;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (attacker != null && other.transform.root == attacker) return;

        // 1. Va chạm Quái vật (Hitbox phẳng quét trúng mọi kẻ địch từ lùn đến cao)
        if ((targetLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                hasHit = true;
                Vector3 hitPoint = GetSafeHitPoint(other);
                bool isCrit = critRate > 0f && UnityEngine.Random.Range(0f, 100f) < critRate;
                float rawDamage = Mathf.Max(1f, damage + bonusDamage);
                float finalDamage = isCrit ? (rawDamage * critDamageMultiplier) : rawDamage;
                damageable.TakeDamage(finalDamage, hitPoint, transform.forward, knockback, attacker, InfiniteLabyrinth.UI.Combat.DamageNumberType.SkillE, isCrit);

                if (isCrit && attacker != null)
                {
                    if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
                        InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.BleedingCrit))
                    {
                        Health targetHealth = damageable as Health ?? (damageable as Component)?.GetComponentInParent<Health>();
                        if (targetHealth != null && !targetHealth.IsDead)
                        {
                            targetHealth.ApplyBleed(finalDamage * 0.3f, 3f, 3, attacker);
                        }
                    }

                    if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
                        InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.CritFrenzy))
                    {
                        var pcCombat = attacker.GetComponentInChildren<PlayerCombat>() ?? attacker.GetComponent<PlayerCombat>();
                        if (pcCombat != null) pcCombat.TriggerAttackSpeedBoost(15f, 3.5f);
                    }
                }

                if (lifestealPercent > 0f && attacker != null)
                {
                    Health attackerHealth = attacker.GetComponent<Health>();
                    if (attackerHealth != null && !attackerHealth.IsDead)
                    {
                        float healAmount = Mathf.Min(finalDamage * (lifestealPercent / 100f), 25f);
                        if (healAmount > 0.5f) attackerHealth.Heal(healAmount);
                    }
                }

                TriggerPlayerHitlag();

                SpawnHitVfx(hitPoint, -transform.forward);
                Destroy(gameObject);
                return;
            }
        }

        // 2. Failsafe cho chướng ngại vật
        if ((obstacleLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            if (aliveTimer < spawnGracePeriod) return;

            // Kiểm tra tránh nổ do sàn
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
            // Đặt hiệu ứng nhô nhẹ ra khỏi mặt tường (0.05m) và xoay hướng bung ra ngoài
            Vector3 spawnPos = point + normal * 0.05f;
            Quaternion spawnRot = Quaternion.LookRotation(normal);
            Instantiate(hitVfxPrefab, spawnPos, spawnRot);
        }
    }

    private void TriggerPlayerHitlag()
    {
        if (hitlagDuration <= 0f) return;
        if (attacker != null)
        {
            PlayerFeedback feedback = attacker.GetComponent<PlayerFeedback>() ?? attacker.GetComponentInChildren<PlayerFeedback>();
            if (feedback != null)
            {
                feedback.TriggerHitlag(hitlagDuration, hitlagTimeScale);
                return;
            }
        }
        PlayerFeedback anyFeedback = FindAnyObjectByType<PlayerFeedback>();
        anyFeedback?.TriggerHitlag(hitlagDuration, hitlagTimeScale);
    }
}
